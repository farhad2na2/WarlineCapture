using System;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    // Called by the existing expanded fact-projection owner after authoritative
    // clock and native combat/lifecycle updates. Never creates a second clock,
    // authors footprints, feeds hidden observations to ARIA, or owns result UI.
    public static class SkirmishNativeObjectiveTick
    {
        public static bool TryTick(EntityManager em,Entity session,out string error)
        {
            error="Actual native non-BA objective owner is unavailable.";
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||
                !em.HasComponent<SkirmishObjectiveStateComponent>(session))return false;
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(!SkirmishMissionOrchestrationKernel.ShouldTickNativeObjective(attempt.IsLegacy!=0,attempt.ObjectiveKind,attempt.Phase,false))
            {error=null;return true;}
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            if(objective.Terminal!=0){error=null;return true;}
            if(objective.Kind!=attempt.ObjectiveKind)return Fail(em,session,out error,"Objective/session kind changed.");
            if(!em.HasComponent<SkirmishNativeObjectiveRecord>(session))return Fail(em,session,out error,error);
            var owner=em.GetComponentObject<SkirmishNativeObjectiveRecord>(session);
            if(owner?.Collector==null||owner.Areas==null||owner.Areas.ObjectiveKind!=attempt.ObjectiveKind)return Fail(em,session,out error,error);
            try
            {
                bool surrender=false;
                using(var matches=em.CreateEntityQuery(typeof(SkirmishMatchState)))
                {
                    if(matches.CalculateEntityCount()!=1)return Fail(em,session,out error,"Exact native match surrender owner missing/ambiguous.");
                    surrender=matches.GetSingleton<SkirmishMatchState>().SurrenderRequested!=0;
                }
                if(surrender)
                {
                    if(!SkirmishMissionOrchestrationKernel.TryDispatch(attempt.ObjectiveKind,true,
                        ref owner.Frontline,ref owner.Breakthrough,ref owner.Convoy,
                        default,default,default,out _,out error))return Fail(em,session,out error,error);
                    Publish(em,session,attempt.ObjectiveKind,owner);error=null;return true;
                }
                if(!owner.Collector.TryCollect(em,session,owner.Clock,out var facts,out error))return Fail(em,session,out error,error);
                var frontline=owner.Frontline;var breakthrough=owner.Breakthrough;var convoy=owner.Convoy;
                if(!SkirmishMissionOrchestrationKernel.TryDispatch(attempt.ObjectiveKind,false,
                    ref frontline,ref breakthrough,ref convoy,facts.Frontline,facts.Breakthrough,facts.Convoy,
                    out var departing,out error))return Fail(em,session,out error,error);
                if(!owner.Collector.TryCommitCollectedTombstones(facts,out error))return Fail(em,session,out error,error);
                owner.Frontline=frontline;owner.Breakthrough=breakthrough;owner.Convoy=convoy;owner.Clock=facts.NextClock;
                foreach(string id in departing)
                    if(!SkirmishNativeObjectiveDeparture.TryRemoveSuccessfulOriginal(em,session,id,out error))return Fail(em,session,out error,error);
                // All native removals finish before the single existing Outcome
                // owner sees a terminal result or a checkpoint can be captured.
                Publish(em,session,attempt.ObjectiveKind,owner);error=null;return true;
            }
            catch(Exception failure)when(failure is ArgumentException||failure is OverflowException||failure is InvalidOperationException)
            {error=failure.Message;return Fail(em,session,out error,error);}
        }

        private static void Publish(EntityManager em,Entity session,SkirmishObjectiveKind kind,SkirmishNativeObjectiveRecord owner)
        {
            if(!SkirmishMissionOrchestrationKernel.TryGetOutcome(kind,owner.Frontline,owner.Breakthrough,owner.Convoy,
                out var outcome,out var reason))return;
            if(outcome==SkirmishOutcomeKind.None)return;
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            objective.Outcome=outcome;objective.Reason=reason;objective.Terminal=1;
            objective.State=outcome==SkirmishOutcomeKind.Victory?SkirmishObjectiveStateKind.TerminalVictory:
                outcome==SkirmishOutcomeKind.Defeat?SkirmishObjectiveStateKind.TerminalDefeat:SkirmishObjectiveStateKind.TerminalDraw;
            em.SetComponentData(session,objective);
        }
        private static bool Fail(EntityManager em,Entity session,out string error,string reason)
        {
            error=string.IsNullOrWhiteSpace(reason)?"Native objective tick failed without a detailed reason.":reason;
            var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            objective.Outcome=SkirmishOutcomeKind.TechnicalFailure;objective.Reason=SkirmishEndReasonKind.ContentFailure;
            objective.Terminal=1;objective.State=SkirmishObjectiveStateKind.TechnicalFailure;em.SetComponentData(session,objective);return false;
        }
    }
}
