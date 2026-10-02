using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    public static class CampaignMissionCitywideAlertRuleUtility
    {
        public const int HoldMilliseconds = 6000, ObstructionGraceMilliseconds = 60000, DeadlineMilliseconds = 900000;
        public const int MilitaryCount = 11, OriginalCount = 27, WarningLeadMilliseconds = 130000;
        public static bool Matches(in CampaignMissionCitywideAlertState mission, in CampaignMissionRuntimeComponent runtime) =>
            mission.Initialized != 0 && mission.SessionToken.Equals(runtime.SessionToken) && mission.AttemptOrdinal == runtime.AttemptOrdinal && mission.SourceVersion == runtime.SourceVersion;
        public static int Stage(in CampaignMissionCitywideAlertState mission) => mission.ReinforcementProduced == 0 ? 1 : mission.CoverageReady == 0 ? 2 :
            mission.ClinicThreatsCleared == 0 ? 3 : mission.ClinicRecovered == 0 ? 4 : mission.UtilityThreatsCleared == 0 || mission.AirCleared == 0 ? 5 :
            mission.UtilityRecovered == 0 ? 6 : mission.ReinforcementReady == 0 ? 7 : 8;
        public static int AdvanceHold(int current, int delta, bool qualified) => !qualified ? 0 :
            current >= HoldMilliseconds - System.Math.Max(0, delta) ? HoldMilliseconds : current + System.Math.Max(0, delta);
        public static bool VictoryQualified(in CampaignMissionCitywideAlertState mission, in CampaignMissionAttemptFactsComponent facts) =>
            mission.Ready != 0 && mission.Failure == CitywideAlertFailure.None && mission.CoverageReady != 0 && mission.ClinicRecovered != 0 &&
            mission.UtilityRecovered != 0 && mission.ReinforcementReady != 0 && mission.MilitaryCleared != 0 &&
            mission.StableMilliseconds >= HoldMilliseconds && facts.HostileTotalCount == MilitaryCount && facts.HostileDefeatedCount == MilitaryCount &&
            facts.CivilianTotalCount == 4 && facts.CivilianLossCount == 0;
        public static bool TryAdvance(in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in CampaignMissionCitywideAlertState mission, bool opening, out CampaignMissionRuntimeComponent next)
        {
            next = runtime;
            if (!Matches(in mission, in runtime) || runtime.Outcome != MissionOutcomeKind.None || runtime.Phase >= MissionPhaseKind.Result) return false;
            var phase = runtime.Phase;var outcome = MissionOutcomeKind.None;
            if (phase >= MissionPhaseKind.FindSquad && mission.Failure != CitywideAlertFailure.None) { phase = MissionPhaseKind.Result;outcome = MissionOutcomeKind.Defeat; }
            else if (phase == MissionPhaseKind.FindSquad && mission.Ready != 0 && opening) phase = MissionPhaseKind.Engage;
            else if (phase == MissionPhaseKind.Engage && VictoryQualified(in mission,in facts)) { phase = MissionPhaseKind.Result;outcome = MissionOutcomeKind.Victory; }
            return phase != runtime.Phase && CampaignMissionRuntimeSystem.TryTransition(in runtime,phase,outcome,
                outcome == MissionOutcomeKind.None ? MissionReturnDestinationKind.None : MissionReturnDestinationKind.CampaignOperations,out next);
        }
    }
}
