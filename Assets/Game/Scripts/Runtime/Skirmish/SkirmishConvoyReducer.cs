using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    public enum SkirmishConvoySubjectStatus : byte { Alive = 0, Dead = 1, Delivered = 2 }

    [Serializable]
    public struct SkirmishConvoySubjectState
    {
        public string StableOriginalId;
        public SkirmishConvoySubjectStatus Status;
        public long DestinationDwellTicks;
    }
    [Serializable]
    public struct SkirmishConvoyState
    {
        public SkirmishConvoySubjectState[] OriginalTrucks;
        public string StableDestinationId;
        public long ElapsedTicks, DeadlineTicks;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public bool Terminal => Outcome != SkirmishOutcomeKind.None;
    }
    public struct SkirmishConvoySubjectFacts
    {
        public string StableOriginalId;
        public bool AuthoritativeLifeKnown, Alive, InsideDestination;
    }
    public struct SkirmishConvoyTickFacts
    {
        public bool Playing, Paused;
        public long SimulationDeltaTicks;
        public SkirmishConvoySubjectFacts[] OriginalTrucks;
        // No main-base, ordinary logistics truck, movement route or stock fact
        // participates in the CE terminal predicate.
    }

    /// <summary>Pure CE delivery rules, without spawning/removal, repair or economy access.</summary>
    public static class SkirmishConvoyReducer
    {
        public const int OriginalCount = 3, RequiredDelivered = 2;
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;
        public const long DestinationDwellTicks = 5 * TicksPerSecond;

        // Fixed stable roles map to the three actual original objective truck IDs
        // at native startup. They are separate from ordinary logistics identities.
        public static SkirmishConvoyState Create(SkirmishSizeId size, string destinationId,
            IReadOnlyList<string> originalTruckIds)
        {
            if (string.IsNullOrWhiteSpace(destinationId) || originalTruckIds == null || originalTruckIds.Count != OriginalCount)
                throw new ArgumentException("Destination and exactly three original objective truck IDs required.");
            var trucks = new SkirmishConvoySubjectState[OriginalCount];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < trucks.Length; i++)
            {
                string id = originalTruckIds[i];
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw new ArgumentException("Original objective truck IDs must be distinct.");
                trucks[i] = new SkirmishConvoySubjectState { StableOriginalId = id };
            }
            int deadline;
            switch (size)
            {
                case SkirmishSizeId.Standard: deadline = 1080; break;
                case SkirmishSizeId.War: deadline = 1500; break;
                case SkirmishSizeId.LargeWar: deadline = 1800; break;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
            return new SkirmishConvoyState { OriginalTrucks = trucks, StableDestinationId = destinationId,
                DeadlineTicks = deadline * TicksPerSecond };
        }

        public static bool TrySurrender(ref SkirmishConvoyState state, bool playing)
        {
            if (!playing || state.Terminal) return false;
            state.Outcome = SkirmishOutcomeKind.Defeat; state.Reason = SkirmishEndReasonKind.Surrender;
            return true;
        }

        // Transition events are consumed once by the native lifecycle owner to
        // remove commands/targeting and retain tombstones, without loss/resources.
        public static string[] Step(ref SkirmishConvoyState state, in SkirmishConvoyTickFacts facts)
        {
            if (state.Terminal) return Array.Empty<string>();
            var byId = Validate(state, facts);
            if (!facts.Playing || facts.Paused) return Array.Empty<string>();
            var next = state;
            next.OriginalTrucks = (SkirmishConvoySubjectState[])state.OriginalTrucks.Clone();
            next.ElapsedTicks = checked(next.ElapsedTicks + facts.SimulationDeltaTicks);
            var delivered = new List<string>();
            int deliveredCount = 0, liveCount = 0;
            for (int i = 0; i < next.OriginalTrucks.Length; i++)
            {
                var truck = next.OriginalTrucks[i];
                if (truck.Status == SkirmishConvoySubjectStatus.Alive)
                {
                    var observed = byId[truck.StableOriginalId];
                    // Shared damage/passenger playback must precede facts. Death
                    // on the fifth dwell second prevents delivery.
                    if (!observed.Alive) { truck.Status = SkirmishConvoySubjectStatus.Dead; truck.DestinationDwellTicks = 0; }
                    else if (!observed.InsideDestination) truck.DestinationDwellTicks = 0;
                    else
                    {
                        long dwell = checked(truck.DestinationDwellTicks + facts.SimulationDeltaTicks);
                        if (dwell >= DestinationDwellTicks)
                        {
                            truck.Status = SkirmishConvoySubjectStatus.Delivered;
                            truck.DestinationDwellTicks = 0;
                            delivered.Add(truck.StableOriginalId);
                        }
                        else truck.DestinationDwellTicks = dwell;
                    }
                }
                // Tombstones cannot be resurrected, re-recruited or delivered twice.
                if (truck.Status == SkirmishConvoySubjectStatus.Alive) liveCount++;
                if (truck.Status == SkirmishConvoySubjectStatus.Delivered) deliveredCount++;
                next.OriginalTrucks[i] = truck;
            }
            if (deliveredCount >= RequiredDelivered)
            { next.Outcome = SkirmishOutcomeKind.Victory; next.Reason = SkirmishEndReasonKind.ConvoyDelivered; }
            else if (deliveredCount + liveCount < RequiredDelivered)
            { next.Outcome = SkirmishOutcomeKind.Defeat; next.Reason = SkirmishEndReasonKind.ConvoyImpossible; }
            else if (next.ElapsedTicks >= next.DeadlineTicks)
            { next.Outcome = SkirmishOutcomeKind.Defeat; next.Reason = SkirmishEndReasonKind.ObjectiveDeadline; }
            state = next;
            return delivered.ToArray();
        }

        private static Dictionary<string, SkirmishConvoySubjectFacts> Validate(in SkirmishConvoyState state,
            in SkirmishConvoyTickFacts facts)
        {
            if (state.OriginalTrucks == null || state.OriginalTrucks.Length != OriginalCount || facts.OriginalTrucks == null ||
                facts.OriginalTrucks.Length != OriginalCount || string.IsNullOrWhiteSpace(state.StableDestinationId) ||
                state.ElapsedTicks < 0 || state.DeadlineTicks <= 0 || facts.SimulationDeltaTicks < 0)
                throw new ArgumentException("Invalid original convoy, destination or simulation clock.");
            var byId = new Dictionary<string, SkirmishConvoySubjectFacts>(StringComparer.Ordinal);
            foreach (var observed in facts.OriginalTrucks)
            {
                if (string.IsNullOrWhiteSpace(observed.StableOriginalId) || byId.ContainsKey(observed.StableOriginalId))
                    throw new ArgumentException("Duplicate/missing objective truck observation.");
                byId.Add(observed.StableOriginalId, observed);
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var truck in state.OriginalTrucks)
            {
                if (string.IsNullOrWhiteSpace(truck.StableOriginalId) || !ids.Add(truck.StableOriginalId) ||
                    !byId.TryGetValue(truck.StableOriginalId, out var observed) ||
                    (byte)truck.Status > (byte)SkirmishConvoySubjectStatus.Delivered ||
                    truck.DestinationDwellTicks < 0 || truck.DestinationDwellTicks >= DestinationDwellTicks ||
                    (truck.Status != SkirmishConvoySubjectStatus.Alive && truck.DestinationDwellTicks != 0) ||
                    (truck.Status == SkirmishConvoySubjectStatus.Alive && !observed.AuthoritativeLifeKnown))
                    throw new ArgumentException("Invalid original truck identity/tombstone/life/dwell state.");
            }
            return byId;
        }
    }
}
