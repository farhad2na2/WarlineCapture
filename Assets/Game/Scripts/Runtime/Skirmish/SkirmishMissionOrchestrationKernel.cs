using System;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    public enum SkirmishResultFreezeDecision : byte { None, ProjectExisting, CreateFrozen }

    // The native owner supplies collected facts and commits tombstones before publishing
    // staged reducer states. This kernel never owns ECS removal, result UI or another clock.
    public static class SkirmishMissionOrchestrationKernel
    {
        public static bool ShouldRefreshFacts(bool legacy, SkirmishSessionPhase phase) =>
            !legacy && phase != SkirmishSessionPhase.Finished && phase != SkirmishSessionPhase.Failed && phase != SkirmishSessionPhase.Cleaning;

        public static void ProjectClock(bool legacy, SkirmishSessionPhase phase, bool paused, float delta,
            int setupDeadline, ref float elapsed, ref int deadline, ref byte playing, ref byte clockPaused)
        {
            if (legacy) return;
            bool isPlaying = phase == SkirmishSessionPhase.Playing;
            bool terminal = !ShouldRefreshFacts(false, phase);
            playing = (byte)(isPlaying ? 1 : 0);
            clockPaused = (byte)(paused || terminal ? 1 : 0);
            if (deadline <= 0 && setupDeadline > 0) deadline = setupDeadline;
            if (isPlaying && !paused) elapsed += delta;
        }

        public static bool TryProjectCollectorClock(double last, double fractional, float elapsed, byte playing, byte paused,
            out double nextLast, out double nextFractional, out long delta, out string error)
        {
            nextLast=last;nextFractional=fractional;delta=0;error="Invalid/regressing authoritative objective clock.";
            double now=elapsed;
            if(playing>1||paused>1||double.IsNaN(now)||double.IsInfinity(now)||now<0||double.IsNaN(last)||
                double.IsInfinity(last)||last<0||now<last||
                double.IsNaN(fractional)||double.IsInfinity(fractional)||fractional<0||fractional>=1)return false;
            if((playing==0||paused!=0)&&now!=last)
            {error="Paused/nonplaying authoritative clock advanced.";return false;}
            double total=(now-last)*TimeSpan.TicksPerSecond+fractional;
            if(total>=long.MaxValue)return false;
            delta=(long)Math.Floor(total);nextLast=now;nextFractional=total-delta;error=null;return true;
        }

        public static bool ShouldTickNativeObjective(bool legacy, SkirmishObjectiveKind kind, SkirmishSessionPhase phase, bool terminal) =>
            !legacy && kind != SkirmishObjectiveKind.BaseAssault && phase == SkirmishSessionPhase.Playing && !terminal;

        public static bool TryDispatch(SkirmishObjectiveKind kind, bool surrender,
            ref SkirmishFrontlineState frontline, ref SkirmishBreakthroughState breakthrough, ref SkirmishConvoyState convoy,
            in SkirmishFrontlineTickFacts frontlineFacts, in SkirmishBreakthroughTickFacts breakthroughFacts, in SkirmishConvoyTickFacts convoyFacts,
            out string[] departing, out string error)
        {
            departing = Array.Empty<string>(); error = null;
            if (kind == SkirmishObjectiveKind.FrontlineControl)
            {
                if (surrender) SkirmishFrontlineReducer.TrySurrender(ref frontline, true);
                else SkirmishFrontlineReducer.Step(ref frontline, frontlineFacts);
            }
            else if (kind == SkirmishObjectiveKind.Breakthrough)
            {
                if (surrender) SkirmishBreakthroughReducer.TrySurrender(ref breakthrough, true);
                else departing = SkirmishBreakthroughReducer.Step(ref breakthrough, breakthroughFacts);
            }
            else if (kind == SkirmishObjectiveKind.ConvoyEscort)
            {
                if (surrender) SkirmishConvoyReducer.TrySurrender(ref convoy, true);
                else departing = SkirmishConvoyReducer.Step(ref convoy, convoyFacts);
            }
            else { error = "Unsupported objective kind."; return false; }
            return true;
        }

        public static bool TryGetOutcome(SkirmishObjectiveKind kind, in SkirmishFrontlineState frontline,
            in SkirmishBreakthroughState breakthrough, in SkirmishConvoyState convoy,
            out SkirmishOutcomeKind outcome, out SkirmishEndReasonKind reason)
        {
            outcome = SkirmishOutcomeKind.None; reason = SkirmishEndReasonKind.None;
            if (kind == SkirmishObjectiveKind.FrontlineControl) { outcome = frontline.Outcome; reason = frontline.Reason; }
            else if (kind == SkirmishObjectiveKind.Breakthrough) { outcome = breakthrough.Outcome; reason = breakthrough.Reason; }
            else if (kind == SkirmishObjectiveKind.ConvoyEscort) { outcome = convoy.Outcome; reason = convoy.Reason; }
            else return false;
            return true;
        }

        public static SkirmishResultFreezeDecision DecideFreeze(bool legacy, bool terminal, bool alreadyFrozen) =>
            legacy || !terminal ? SkirmishResultFreezeDecision.None :
            alreadyFrozen ? SkirmishResultFreezeDecision.ProjectExisting : SkirmishResultFreezeDecision.CreateFrozen;

        public static bool TryEvaluateBaseAssault(
            in SkirmishBaseAssaultFacts facts,
            out SkirmishOutcomeKind outcome,
            out SkirmishEndReasonKind reason)
        {
            outcome = SkirmishOutcomeKind.None;
            reason = SkirmishEndReasonKind.None;
            if (!facts.Playing)
                return false;

            if (facts.Surrender)
            {
                outcome = SkirmishOutcomeKind.Defeat;
                reason = SkirmishEndReasonKind.Surrender;
                return true;
            }

            if (!facts.PlayerDesignatedAlive && !facts.EnemyDesignatedAlive)
            {
                outcome = SkirmishOutcomeKind.Draw;
                reason = SkirmishEndReasonKind.BothBasesDestroyed;
                return true;
            }

            if (!facts.EnemyDesignatedAlive)
            {
                outcome = SkirmishOutcomeKind.Victory;
                reason = SkirmishEndReasonKind.MainBaseDestroyed;
                return true;
            }

            if (!facts.PlayerDesignatedAlive)
            {
                outcome = SkirmishOutcomeKind.Defeat;
                reason = SkirmishEndReasonKind.MainBaseDestroyed;
                return true;
            }

            if (facts.Paused)
                return false;

            if (facts.DeadlineSeconds > 0 && facts.ElapsedSeconds >= facts.DeadlineSeconds)
            {
                outcome = SkirmishOutcomeKind.Draw;
                reason = SkirmishEndReasonKind.TimeLimit;
                return true;
            }

            _ = facts.ReplacementBarracksPresent;
            _ = facts.FieldArmyWiped;
            return false;
        }

    }
}
