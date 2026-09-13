using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    internal static class CampaignMissionBreachRuleUtility
    {
        internal static bool IsFailure(in CampaignMissionAttemptFactsComponent facts) =>
            facts.CommandSquadAlive==0 || facts.HostileRosterIntegrityFault!=0 || facts.BreachTimedOut!=0;
        internal static bool IsVictory(in CampaignMissionAttemptFactsComponent facts) =>
            !IsFailure(in facts) && facts.BreachGateDestroyed!=0 && facts.BreachCoreDestroyed!=0 && facts.BreachArchiveSecured!=0;
        internal static bool TryAdvance(in CampaignMissionRuntimeComponent current,in CampaignMissionAttemptFactsComponent facts,
            bool ready,bool openingComplete,out CampaignMissionRuntimeComponent next)
        {
            next=current;
            if(current.Outcome!=MissionOutcomeKind.None || current.Phase>=MissionPhaseKind.Result) return false;
            var phase=current.Phase; var outcome=MissionOutcomeKind.None;
            if(phase==MissionPhaseKind.Preparing && (current.ReadyReadiness&current.RequiredReadiness)==current.RequiredReadiness)
                phase=MissionPhaseKind.InteractiveBrief;
            else if(phase==MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted!=0) phase=MissionPhaseKind.FindSquad;
            else if(phase>=MissionPhaseKind.FindSquad && (ready || facts.HostileRosterIntegrityFault!=0) && IsFailure(in facts)) outcome=MissionOutcomeKind.Defeat;
            else if(phase==MissionPhaseKind.FindSquad && ready && openingComplete) phase=MissionPhaseKind.Engage;
            else if(phase==MissionPhaseKind.Engage && IsVictory(in facts))
            {
                if(facts.FinalePresentationRequired!=0 && facts.FinalePresentationComplete==0) phase=MissionPhaseKind.SecureCorridor;
                else outcome=MissionOutcomeKind.Victory;
            }
            else if(phase==MissionPhaseKind.SecureCorridor && IsVictory(in facts) && facts.FinalePresentationComplete!=0) outcome=MissionOutcomeKind.Victory;
            if(outcome!=MissionOutcomeKind.None) phase=MissionPhaseKind.Result;
            return phase!=current.Phase && CampaignMissionRuntimeSystem.TryTransition(in current,phase,outcome,
                outcome==MissionOutcomeKind.None?MissionReturnDestinationKind.None:MissionReturnDestinationKind.CampaignOperations,out next);
        }
    }
}
