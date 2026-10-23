using System;
using Game.Components;
using Game.Skirmish.Contracts;
using Game.Runtime;
namespace Game.Tests.Editor
{
    internal static class SkirmishMissionOrchestrationOriginalOracle
    {
        public static bool TryEvaluate(
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

        public static bool TryProjectClock(in SkirmishObjectiveCollectorClock cursor,in SkirmishObjectiveClockComponent clock,
            out SkirmishObjectiveCollectorClock next,out long delta,out string error)
        {
            next=cursor;delta=0;error="Invalid/regressing authoritative objective clock.";
            double now=clock.ElapsedSeconds;
            if(clock.Playing>1||clock.Paused>1||double.IsNaN(now)||double.IsInfinity(now)||now<0||double.IsNaN(cursor.LastAuthoritativeSeconds)||
                double.IsInfinity(cursor.LastAuthoritativeSeconds)||cursor.LastAuthoritativeSeconds<0||now<cursor.LastAuthoritativeSeconds||
                double.IsNaN(cursor.FractionalTicks)||double.IsInfinity(cursor.FractionalTicks)||cursor.FractionalTicks<0||cursor.FractionalTicks>=1)return false;
            if((clock.Playing==0||clock.Paused!=0)&&now!=cursor.LastAuthoritativeSeconds)
            {error="Paused/nonplaying authoritative clock advanced.";return false;}
            double total=(now-cursor.LastAuthoritativeSeconds)*TimeSpan.TicksPerSecond+cursor.FractionalTicks;
            if(total>=long.MaxValue)return false;
            delta=(long)Math.Floor(total);next.LastAuthoritativeSeconds=now;next.FractionalTicks=total-delta;error=null;return true;
        }

    }
}
