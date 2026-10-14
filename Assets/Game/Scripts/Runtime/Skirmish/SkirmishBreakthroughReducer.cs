using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    public enum SkirmishBreakthroughSubjectStatus : byte { Alive = 0, Dead = 1, Evacuated = 2 }

    [Serializable]
    public struct SkirmishBreakthroughSubjectState
    {
        public string StableOriginalId;
        public SkirmishBreakthroughSubjectStatus Status;
        public long ExitDwellTicks;
    }
    [Serializable]
    public struct SkirmishBreakthroughCorridorState
    {
        public string StableCorridorId;
        public long HoldTicks;
    }
    [Serializable]
    public struct SkirmishBreakthroughState
    {
        public SkirmishBreakthroughSubjectState[] OriginalSubjects;
        public SkirmishBreakthroughCorridorState CorridorA, CorridorB;
        public bool ExitOpen;
        public long ElapsedTicks, DeadlineTicks;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public bool Terminal => Outcome != SkirmishOutcomeKind.None;
    }
    public struct SkirmishBreakthroughCorridorFacts
    {
        // Native collector excludes vehicles/passengers and uses living dismounted
        // combat infantry, including support infantry that are not designated.
        public int PlayerEligibleInfantry, EnemyEligibleInfantry;
    }
    public struct SkirmishBreakthroughSubjectFacts
    {
        public string StableOriginalId;
        public bool AuthoritativeLifeKnown, Alive, Dismounted, InsideExit;
    }
    public struct SkirmishBreakthroughTickFacts
    {
        public bool Playing, Paused;
        public long SimulationDeltaTicks;
        public SkirmishBreakthroughCorridorFacts CorridorA, CorridorB;
        public SkirmishBreakthroughSubjectFacts[] OriginalSubjects;
        // Main-base facts intentionally do not participate in the BT outcome.
    }

    /// <summary>Pure BT rules; no actor spawning/removal, capacity or world occupancy access.</summary>
    public static class SkirmishBreakthroughReducer
    {
        public const int OriginalCount = 12, RequiredEvacuated = 8;
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;
        public const long CorridorHoldTicks = 20 * TicksPerSecond;
        public const long ExitDwellTicks = 3 * TicksPerSecond;

        public static SkirmishBreakthroughState Create(SkirmishSizeId size, string corridorA, string corridorB,
            IReadOnlyList<string> originalRifleIds)
        {
            if (string.IsNullOrWhiteSpace(corridorA) || string.IsNullOrWhiteSpace(corridorB) || corridorA == corridorB ||
                originalRifleIds == null || originalRifleIds.Count != OriginalCount)
                throw new ArgumentException("Two distinct authored corridors and exactly 12 original rifle IDs required.");
            var subjects = new SkirmishBreakthroughSubjectState[OriginalCount];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < subjects.Length; i++)
            {
                string id = originalRifleIds[i];
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new ArgumentException("Original rifle IDs must be distinct.");
                subjects[i] = new SkirmishBreakthroughSubjectState { StableOriginalId = id };
            }
            int deadline;
            switch (size)
            {
                case SkirmishSizeId.Standard: deadline = 1080; break;
                case SkirmishSizeId.War: deadline = 1500; break;
                case SkirmishSizeId.LargeWar: deadline = 1800; break;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
            return new SkirmishBreakthroughState
            {
                OriginalSubjects = subjects, DeadlineTicks = deadline * TicksPerSecond,
                CorridorA = new SkirmishBreakthroughCorridorState { StableCorridorId = corridorA },
                CorridorB = new SkirmishBreakthroughCorridorState { StableCorridorId = corridorB }
            };
        }

        public static bool TrySurrender(ref SkirmishBreakthroughState state, bool playing)
        {
            if (!playing || state.Terminal) return false;
            state.Outcome = SkirmishOutcomeKind.Defeat; state.Reason = SkirmishEndReasonKind.Surrender;
            return true;
        }

        // Newly evacuated IDs are transition events for the native lifecycle owner.
        // Repeated observations never emit a second removal/capacity release event.
        public static string[] Step(ref SkirmishBreakthroughState state, in SkirmishBreakthroughTickFacts facts)
        {
            if (state.Terminal) return Array.Empty<string>();
            var byId = Validate(state, facts);
            if (!facts.Playing || facts.Paused) return Array.Empty<string>();
            var next = state;
            next.OriginalSubjects = (SkirmishBreakthroughSubjectState[])state.OriginalSubjects.Clone();
            long delta = facts.SimulationDeltaTicks;
            next.ElapsedTicks = checked(next.ElapsedTicks + delta);
            long unlockA = AdvanceCorridor(ref next.CorridorA, facts.CorridorA, delta);
            long unlockB = AdvanceCorridor(ref next.CorridorB, facts.CorridorB, delta);
            bool opens = (unlockA != long.MaxValue && unlockA <= delta) || (unlockB != long.MaxValue && unlockB <= delta);
            long dwellDelta = state.ExitOpen ? delta : opens ? delta - Math.Min(unlockA, unlockB) : 0;
            next.ExitOpen |= opens;
            var evacuated = new List<string>();
            int evacuatedCount = 0, aliveCount = 0;
            for (int i = 0; i < next.OriginalSubjects.Length; i++)
            {
                var subject = next.OriginalSubjects[i];
                if (subject.Status == SkirmishBreakthroughSubjectStatus.Alive)
                {
                    var observed = byId[subject.StableOriginalId];
                    // Damage/passenger facts are collected before credit. A death on
                    // the third dwell second prevents evacuation, not a delayed death.
                    if (!observed.Alive) { subject.Status = SkirmishBreakthroughSubjectStatus.Dead; subject.ExitDwellTicks = 0; }
                    else if (!next.ExitOpen || !observed.Dismounted || !observed.InsideExit) subject.ExitDwellTicks = 0;
                    else
                    {
                        long dwell = checked(subject.ExitDwellTicks + dwellDelta);
                        if (dwell >= ExitDwellTicks)
                        {
                            subject.Status = SkirmishBreakthroughSubjectStatus.Evacuated;
                            subject.ExitDwellTicks = 0;
                            evacuated.Add(subject.StableOriginalId);
                        }
                        else subject.ExitDwellTicks = dwell;
                    }
                }
                // Dead/evacuated stable tombstones cannot be revived by a later
                // actor observation, replacement recruit or duplicate exit event.
                if (subject.Status == SkirmishBreakthroughSubjectStatus.Alive) aliveCount++;
                if (subject.Status == SkirmishBreakthroughSubjectStatus.Evacuated) evacuatedCount++;
                next.OriginalSubjects[i] = subject;
            }
            if (evacuatedCount >= RequiredEvacuated)
            { next.Outcome = SkirmishOutcomeKind.Victory; next.Reason = SkirmishEndReasonKind.BreakthroughEvacuated; }
            else if (evacuatedCount + aliveCount < RequiredEvacuated)
            { next.Outcome = SkirmishOutcomeKind.Defeat; next.Reason = SkirmishEndReasonKind.BreakthroughImpossible; }
            else if (next.ElapsedTicks >= next.DeadlineTicks)
            { next.Outcome = SkirmishOutcomeKind.Defeat; next.Reason = SkirmishEndReasonKind.ObjectiveDeadline; }
            state = next;
            return evacuated.ToArray();
        }

        // Returns the elapsed part of this tick at which this corridor opens.
        // Long.MaxValue means no opening. Exact-final-tick unlock gives zero
        // retroactive exit dwell; only time while open may earn evacuation.
        private static long AdvanceCorridor(ref SkirmishBreakthroughCorridorState corridor,
            in SkirmishBreakthroughCorridorFacts facts, long delta)
        {
            if (facts.PlayerEligibleInfantry == 0 || facts.EnemyEligibleInfantry > 0)
            { corridor.HoldTicks = 0; return long.MaxValue; }
            long needed = CorridorHoldTicks - corridor.HoldTicks;
            corridor.HoldTicks = delta >= needed ? CorridorHoldTicks : corridor.HoldTicks + delta;
            return delta >= needed ? needed : long.MaxValue;
        }

        private static Dictionary<string, SkirmishBreakthroughSubjectFacts> Validate(in SkirmishBreakthroughState state,
            in SkirmishBreakthroughTickFacts facts)
        {
            if (state.OriginalSubjects == null || state.OriginalSubjects.Length != OriginalCount || facts.OriginalSubjects == null ||
                facts.OriginalSubjects.Length != OriginalCount || state.ElapsedTicks < 0 || state.DeadlineTicks <= 0 || facts.SimulationDeltaTicks < 0)
                throw new ArgumentException("Invalid original subject set or simulation clock.");
            ValidateCorridor(state.CorridorA, facts.CorridorA); ValidateCorridor(state.CorridorB, facts.CorridorB);
            if (state.CorridorA.StableCorridorId == state.CorridorB.StableCorridorId) throw new ArgumentException("Duplicate corridor identity.");
            var byId = new Dictionary<string, SkirmishBreakthroughSubjectFacts>(StringComparer.Ordinal);
            foreach (var observed in facts.OriginalSubjects)
            {
                if (string.IsNullOrWhiteSpace(observed.StableOriginalId) || byId.ContainsKey(observed.StableOriginalId))
                    throw new ArgumentException("Duplicate/missing original subject observation.");
                byId.Add(observed.StableOriginalId, observed);
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var subject in state.OriginalSubjects)
            {
                if (string.IsNullOrWhiteSpace(subject.StableOriginalId) || !ids.Add(subject.StableOriginalId) ||
                    !byId.TryGetValue(subject.StableOriginalId, out var observed) ||
                    (byte)subject.Status > (byte)SkirmishBreakthroughSubjectStatus.Evacuated ||
                    subject.ExitDwellTicks < 0 || subject.ExitDwellTicks >= ExitDwellTicks ||
                    (subject.Status != SkirmishBreakthroughSubjectStatus.Alive && subject.ExitDwellTicks != 0) ||
                    (!state.ExitOpen && subject.ExitDwellTicks != 0) ||
                    (subject.Status == SkirmishBreakthroughSubjectStatus.Alive && !observed.AuthoritativeLifeKnown))
                    throw new ArgumentException("Invalid designated identity/tombstone/life/dwell state.");
            }
            return byId;
        }
        private static void ValidateCorridor(in SkirmishBreakthroughCorridorState state, in SkirmishBreakthroughCorridorFacts facts)
        {
            if (string.IsNullOrWhiteSpace(state.StableCorridorId) || state.HoldTicks < 0 || state.HoldTicks > CorridorHoldTicks ||
                facts.PlayerEligibleInfantry < 0 || facts.EnemyEligibleInfantry < 0)
                throw new ArgumentException("Invalid corridor state/eligible counts.");
        }
    }
}
