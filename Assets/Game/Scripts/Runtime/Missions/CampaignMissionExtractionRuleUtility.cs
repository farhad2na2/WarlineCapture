using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    internal static class CampaignMissionExtractionRuleUtility
    {
        internal static bool IsFailure(in CampaignMissionAttemptFactsComponent facts) =>
            facts.ExtractionAircraftLost != 0 || facts.ExtractionCarrierLost != 0 || facts.CivilianLossCount > 0 ||
            facts.HostileRosterIntegrityFault != 0 || facts.ExtractionTimedOut != 0 ||
            facts.CommandSquadSpawned != 0 && facts.CommandSquadAlive == 0;

        internal static bool IsVictory(in CampaignMissionAttemptFactsComponent facts, int required) =>
            !IsFailure(in facts) && required > 0 && facts.ExtractionPassengerTotal == required &&
            facts.ExtractionCarrierLegCount == required && facts.ExtractionPassengersDelivered == required && facts.ExtractionDeparted != 0;

        internal static bool TryAdvance(in CampaignMissionRuntimeComponent current, in CampaignMissionAttemptFactsComponent facts,
            bool ready, bool openingComplete, int required, out CampaignMissionRuntimeComponent next)
        {
            next = current;
            if (current.Outcome != MissionOutcomeKind.None || current.Phase >= MissionPhaseKind.Result) return false;
            var phase = current.Phase; var outcome = MissionOutcomeKind.None;
            if (phase == MissionPhaseKind.Preparing && (current.ReadyReadiness & current.RequiredReadiness) == current.RequiredReadiness)
                phase = MissionPhaseKind.InteractiveBrief;
            else if (phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0)
                phase = MissionPhaseKind.FindSquad;
            else if (phase >= MissionPhaseKind.FindSquad && (ready || facts.HostileRosterIntegrityFault != 0) && IsFailure(in facts)) outcome = MissionOutcomeKind.Defeat;
            else if (phase == MissionPhaseKind.FindSquad && ready && openingComplete) phase = MissionPhaseKind.Engage;
            else if (phase == MissionPhaseKind.Engage && IsVictory(in facts, required))
            {
                if (facts.FinalePresentationRequired != 0 && facts.FinalePresentationComplete == 0) phase = MissionPhaseKind.SecureCorridor;
                else outcome = MissionOutcomeKind.Victory;
            }
            else if (phase == MissionPhaseKind.SecureCorridor && IsVictory(in facts, required) && facts.FinalePresentationComplete != 0)
                outcome = MissionOutcomeKind.Victory;
            if (outcome != MissionOutcomeKind.None) phase = MissionPhaseKind.Result;
            return phase != current.Phase && CampaignMissionRuntimeSystem.TryTransition(in current, phase, outcome,
                outcome == MissionOutcomeKind.None ? MissionReturnDestinationKind.None : MissionReturnDestinationKind.CampaignOperations, out next);
        }
    }
}
