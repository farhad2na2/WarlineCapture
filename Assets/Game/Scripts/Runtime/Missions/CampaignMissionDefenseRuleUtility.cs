using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    internal static class CampaignMissionDefenseRuleUtility
    {
        internal static bool IsFailure(in CampaignMissionAttemptFactsComponent facts) =>
            facts.ForwardPostDestroyed != 0 || facts.CoreBreached != 0 || facts.HostileRosterIntegrityFault != 0;

        internal static bool IsVictory(in CampaignMissionAttemptFactsComponent facts) =>
            !IsFailure(in facts) && facts.ForwardPostBound != 0 && facts.DefenseWaveActivated != 0 &&
            facts.HostileTotalCount > 0 && facts.HostileDefeatedCount == facts.HostileTotalCount;

        internal static bool TryAdvance(in CampaignMissionRuntimeComponent current,
            in CampaignMissionAttemptFactsComponent facts, bool controlsReady, out CampaignMissionRuntimeComponent next)
        {
            next = current;
            if (current.Outcome != MissionOutcomeKind.None || current.Phase >= MissionPhaseKind.Result) return false;
            MissionPhaseKind phase = current.Phase;
            MissionOutcomeKind outcome = MissionOutcomeKind.None;
            MissionReturnDestinationKind destination = MissionReturnDestinationKind.None;
            if (phase == MissionPhaseKind.Preparing &&
                (current.ReadyReadiness & current.RequiredReadiness) == current.RequiredReadiness)
                phase = MissionPhaseKind.InteractiveBrief;
            else if (phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0)
                phase = MissionPhaseKind.FindSquad;
            else if (phase is >= MissionPhaseKind.FindSquad and <= MissionPhaseKind.SecureCorridor && IsFailure(in facts))
                outcome = MissionOutcomeKind.Defeat;
            else if (phase == MissionPhaseKind.FindSquad && controlsReady && facts.ForwardPostBound != 0)
                phase = MissionPhaseKind.Engage;
            else if (phase == MissionPhaseKind.Engage && IsVictory(in facts))
            {
                if (facts.FinalePresentationRequired != 0 && facts.FinalePresentationComplete == 0)
                    phase = MissionPhaseKind.SecureCorridor;
                else outcome = MissionOutcomeKind.Victory;
            }
            else if (phase == MissionPhaseKind.SecureCorridor && IsVictory(in facts) && facts.FinalePresentationComplete != 0)
                outcome = MissionOutcomeKind.Victory;
            if (outcome != MissionOutcomeKind.None)
            {
                phase = MissionPhaseKind.Result;
                destination = MissionReturnDestinationKind.CampaignOperations;
            }
            return phase != current.Phase && CampaignMissionRuntimeSystem.TryTransition(
                in current, phase, outcome, destination, out next);
        }
    }
}
