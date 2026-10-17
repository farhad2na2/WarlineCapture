using System;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Pure objective state only. Native ECS occupancy/eligibility, clocks, planner,
    // HUD, checkpoint and the single outcome owner still need explicit adapters.
    public enum SkirmishFrontlineSide : byte { Neutral = 0, Player = 1, Enemy = 2 }
    public enum SkirmishFrontlineCapturePhase : byte { None = 0, Neutralizing = 1, Capturing = 2 }

    [Serializable]
    public struct SkirmishFrontlineZoneState
    {
        public string StableZoneId;
        public SkirmishFrontlineSide Owner;
        public SkirmishFrontlineSide CapturingSide;
        public SkirmishFrontlineCapturePhase Phase;
        public long ProgressTicks;
        public long EmptyGraceTicks;
        public bool Contested;
    }

    [Serializable]
    public struct SkirmishFrontlineState
    {
        public SkirmishFrontlineZoneState ZoneA, ZoneB, ZoneC;
        public int PlayerTickets, EnemyTickets;
        public long PlayerDrainRemainderTicks, EnemyDrainRemainderTicks;
        public long ElapsedTicks, DeadlineTicks;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public bool Terminal => Outcome != SkirmishOutcomeKind.None;
    }

    public struct SkirmishFrontlineZoneFacts
    {
        // The native fact collector must count only living, dismounted combat infantry
        // in the authored footprint. Vehicles/air/passengers/pilots/civilians are excluded.
        public int PlayerEligibleInfantry, EnemyEligibleInfantry;
    }

    public struct SkirmishFrontlineTickFacts
    {
        public SkirmishFrontlineZoneFacts ZoneA, ZoneB, ZoneC;
        public bool Playing, Paused;
        public bool OriginalBaseFactsPresent;
        public bool PlayerOriginalBaseAlive, EnemyOriginalBaseAlive;
        // TimeSpan tick units are a representation, not a gameplay speed/tick-rate rule.
        // Native adapters convert simulation time with a persisted fractional remainder.
        public long SimulationDeltaTicks;
    }

    public static class SkirmishFrontlineReducer
    {
        public const long TicksPerSecond = TimeSpan.TicksPerSecond;
        public const long CaptureTicks = 8 * TicksPerSecond;
        public const long EmptyGraceTicks = 10 * TicksPerSecond;
        public const int InitialTickets = 500;

        public static SkirmishFrontlineState Create(SkirmishSizeId size, string zoneA, string zoneB, string zoneC)
        {
            if (string.IsNullOrWhiteSpace(zoneA) || string.IsNullOrWhiteSpace(zoneB) || string.IsNullOrWhiteSpace(zoneC) ||
                zoneA == zoneB || zoneA == zoneC || zoneB == zoneC)
                throw new ArgumentException("Three distinct authored stable zone IDs are required.");
            int seconds;
            switch (size)
            {
                case SkirmishSizeId.Standard: seconds = 1200; break;
                case SkirmishSizeId.War: seconds = 1500; break;
                case SkirmishSizeId.LargeWar: seconds = 1800; break;
                default: throw new ArgumentOutOfRangeException(nameof(size));
            }
            return new SkirmishFrontlineState
            {
                ZoneA = new SkirmishFrontlineZoneState { StableZoneId = zoneA },
                ZoneB = new SkirmishFrontlineZoneState { StableZoneId = zoneB },
                ZoneC = new SkirmishFrontlineZoneState { StableZoneId = zoneC },
                PlayerTickets = InitialTickets, EnemyTickets = InitialTickets, DeadlineTicks = seconds * TicksPerSecond
            };
        }

        // Call at the accepted input boundary before the next simulation tick.
        // Pause does not turn an already accepted Playing surrender into a combat event.
        public static bool TrySurrender(ref SkirmishFrontlineState state, bool playing)
        {
            if (!playing || state.Terminal) return false;
            state.Outcome = SkirmishOutcomeKind.Defeat;
            state.Reason = SkirmishEndReasonKind.Surrender;
            return true;
        }

        public static void Step(ref SkirmishFrontlineState state, in SkirmishFrontlineTickFacts facts)
        {
            if (state.Terminal) return;
            Validate(state, facts);
            if (!facts.Playing || facts.Paused) return;
            // Stage everything; invalid/overflowing input cannot partly mutate persisted state.
            var next = state;
            long delta = facts.SimulationDeltaTicks;
            next.ElapsedTicks = checked(next.ElapsedTicks + delta);
            AdvanceZone(ref next.ZoneA, facts.ZoneA, delta);
            AdvanceZone(ref next.ZoneB, facts.ZoneB, delta);
            AdvanceZone(ref next.ZoneC, facts.ZoneC, delta);

            int playerOwned = Owned(next, SkirmishFrontlineSide.Player);
            int enemyOwned = Owned(next, SkirmishFrontlineSide.Enemy);
            // Count ownership after every zone transition. Contested owned zones count.
            // Three zones drains at the same 1 ticket/s rate as two.
            if (playerOwned >= 2) Drain(ref next.EnemyTickets, ref next.EnemyDrainRemainderTicks, delta);
            if (enemyOwned >= 2) Drain(ref next.PlayerTickets, ref next.PlayerDrainRemainderTicks, delta);

            bool win = next.EnemyTickets == 0 || !facts.EnemyOriginalBaseAlive;
            bool loss = next.PlayerTickets == 0 || !facts.PlayerOriginalBaseAlive;
            if (win && loss)
            {
                next.Outcome = SkirmishOutcomeKind.Draw;
                next.Reason = !facts.PlayerOriginalBaseAlive && !facts.EnemyOriginalBaseAlive
                    ? SkirmishEndReasonKind.BothBasesDestroyed : SkirmishEndReasonKind.TicketsDepleted;
            }
            else if (win)
            {
                next.Outcome = SkirmishOutcomeKind.Victory;
                next.Reason = !facts.EnemyOriginalBaseAlive ? SkirmishEndReasonKind.MainBaseDestroyed : SkirmishEndReasonKind.TicketsDepleted;
            }
            else if (loss)
            {
                next.Outcome = SkirmishOutcomeKind.Defeat;
                next.Reason = !facts.PlayerOriginalBaseAlive ? SkirmishEndReasonKind.MainBaseDestroyed : SkirmishEndReasonKind.TicketsDepleted;
            }
            else if (next.ElapsedTicks >= next.DeadlineTicks)
            {
                next.Outcome = next.PlayerTickets == next.EnemyTickets ? SkirmishOutcomeKind.Draw :
                    next.PlayerTickets > next.EnemyTickets ? SkirmishOutcomeKind.Victory : SkirmishOutcomeKind.Defeat;
                next.Reason = SkirmishEndReasonKind.TicketsAtDeadline;
            }
            state = next;
        }

        private static void AdvanceZone(ref SkirmishFrontlineZoneState zone, in SkirmishFrontlineZoneFacts facts, long delta)
        {
            bool player = facts.PlayerEligibleInfantry > 0, enemy = facts.EnemyEligibleInfantry > 0;
            zone.Contested = player && enemy;
            if (zone.Contested) { zone.EmptyGraceTicks = 0; return; }
            if (!player && !enemy)
            {
                long empty = checked(zone.EmptyGraceTicks + delta);
                long decay = Math.Max(0, empty - EmptyGraceTicks);
                zone.EmptyGraceTicks = Math.Min(EmptyGraceTicks, empty);
                zone.ProgressTicks = Math.Max(0, zone.ProgressTicks - decay);
                if (zone.ProgressTicks == 0) ResetWork(ref zone);
                return;
            }
            zone.EmptyGraceTicks = 0;
            var side = player ? SkirmishFrontlineSide.Player : SkirmishFrontlineSide.Enemy;
            if (zone.Owner == side) { ResetWork(ref zone); return; }
            if (zone.CapturingSide != side)
            {
                ResetWork(ref zone);
                zone.CapturingSide = side;
            }
            zone.Phase = zone.Owner == SkirmishFrontlineSide.Neutral
                ? SkirmishFrontlineCapturePhase.Capturing : SkirmishFrontlineCapturePhase.Neutralizing;
            long work = checked(zone.ProgressTicks + delta);
            if (work < CaptureTicks) { zone.ProgressTicks = work; return; }
            work -= CaptureTicks;
            if (zone.Owner != SkirmishFrontlineSide.Neutral)
            {
                zone.Owner = SkirmishFrontlineSide.Neutral;
                zone.Phase = SkirmishFrontlineCapturePhase.Capturing;
                if (work < CaptureTicks) { zone.ProgressTicks = work; return; }
            }
            zone.Owner = side;
            ResetWork(ref zone);
        }

        private static void ResetWork(ref SkirmishFrontlineZoneState zone)
        {
            zone.ProgressTicks = 0;
            zone.CapturingSide = SkirmishFrontlineSide.Neutral;
            zone.Phase = SkirmishFrontlineCapturePhase.None;
        }
        private static void Drain(ref int tickets, ref long remainder, long delta)
        {
            long total = checked(remainder + delta);
            long spent = total / TicksPerSecond;
            remainder = total % TicksPerSecond;
            tickets = spent >= tickets ? 0 : tickets - (int)spent;
        }
        private static int Owned(in SkirmishFrontlineState state, SkirmishFrontlineSide side) =>
            (state.ZoneA.Owner == side ? 1 : 0) + (state.ZoneB.Owner == side ? 1 : 0) + (state.ZoneC.Owner == side ? 1 : 0);
        private static void Validate(in SkirmishFrontlineState state, in SkirmishFrontlineTickFacts facts)
        {
            if ((facts.Playing && !facts.OriginalBaseFactsPresent) || facts.SimulationDeltaTicks < 0 || state.ElapsedTicks < 0 || state.DeadlineTicks <= 0 ||
                state.PlayerTickets < 0 || state.PlayerTickets > InitialTickets || state.EnemyTickets < 0 || state.EnemyTickets > InitialTickets ||
                state.PlayerDrainRemainderTicks < 0 || state.PlayerDrainRemainderTicks >= TicksPerSecond ||
                state.EnemyDrainRemainderTicks < 0 || state.EnemyDrainRemainderTicks >= TicksPerSecond)
                throw new ArgumentException("Invalid Frontline clock/ticket state.");
            ValidateZone(state.ZoneA, facts.ZoneA); ValidateZone(state.ZoneB, facts.ZoneB); ValidateZone(state.ZoneC, facts.ZoneC);
            if (state.ZoneA.StableZoneId == state.ZoneB.StableZoneId || state.ZoneA.StableZoneId == state.ZoneC.StableZoneId ||
                state.ZoneB.StableZoneId == state.ZoneC.StableZoneId) throw new ArgumentException("Duplicate Frontline zone identity.");
        }
        private static void ValidateZone(in SkirmishFrontlineZoneState zone, in SkirmishFrontlineZoneFacts facts)
        {
            if (string.IsNullOrWhiteSpace(zone.StableZoneId) || (byte)zone.Owner > (byte)SkirmishFrontlineSide.Enemy ||
                (byte)zone.CapturingSide > (byte)SkirmishFrontlineSide.Enemy || (byte)zone.Phase > (byte)SkirmishFrontlineCapturePhase.Capturing ||
                zone.ProgressTicks < 0 || zone.ProgressTicks >= CaptureTicks || zone.EmptyGraceTicks < 0 || zone.EmptyGraceTicks > EmptyGraceTicks ||
                facts.PlayerEligibleInfantry < 0 || facts.EnemyEligibleInfantry < 0)
                throw new ArgumentException("Invalid Frontline zone/count state.");
            bool idle = zone.Phase == SkirmishFrontlineCapturePhase.None;
            if ((idle && (zone.ProgressTicks != 0 || zone.CapturingSide != SkirmishFrontlineSide.Neutral)) ||
                (!idle && (zone.CapturingSide == SkirmishFrontlineSide.Neutral || zone.CapturingSide == zone.Owner)) ||
                (zone.Phase == SkirmishFrontlineCapturePhase.Neutralizing && zone.Owner == SkirmishFrontlineSide.Neutral) ||
                (zone.Phase == SkirmishFrontlineCapturePhase.Capturing && zone.Owner != SkirmishFrontlineSide.Neutral))
                throw new ArgumentException("Inconsistent Frontline capture phase/owner state.");
        }
    }
}
