using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    public static class CampaignMissionArmorBreakRuleUtility
    {
        public const int HoldMilliseconds = 6000, DeadlineMilliseconds = 900000;
        public static bool Matches(in CampaignMissionArmorBreakState mission, in CampaignMissionRuntimeComponent runtime) =>
            mission.Initialized != 0 && mission.SessionToken.Equals(runtime.SessionToken) && mission.AttemptOrdinal == runtime.AttemptOrdinal && mission.SourceVersion == runtime.SourceVersion;
        public static int Stage(in CampaignMissionArmorBreakState mission) => mission.CoverageReady == 0 ? 1 : mission.AirCleared == 0 ? 2 :
            mission.BatteryDisabled == 0 || mission.LauncherShots == 0 ? 3 : mission.AircraftUsed == 0 || mission.ArmoredThreatsCleared == 0 ? 4 : mission.ArmorApproached == 0 ? 5 : mission.CommandDisabled == 0 ? 6 : 7;
        public static int AdvanceRecovery(int current, int delta, bool qualified) => !qualified ? 0 :
            current >= HoldMilliseconds - System.Math.Max(0, delta) ? HoldMilliseconds : current + System.Math.Max(0, delta);
        public static bool VictoryQualified(in CampaignMissionArmorBreakState mission, in CampaignMissionAttemptFactsComponent facts) =>
            mission.Ready != 0 && mission.Failure == ArmorBreakFailure.None && mission.CoverageReady != 0 && mission.AirCleared != 0 &&
            mission.LauncherShots > 0 && mission.BatteryDisabled != 0 && mission.AircraftUsed != 0 && mission.ArmoredThreatsCleared != 0 && mission.ArmorApproached != 0 &&
            mission.CommandDisabled != 0 && mission.AuthorityRecovered != 0 && mission.ReliefLost == 0 && facts.HostileTotalCount == 12 && facts.HostileDefeatedCount == 12;
        public static bool TryAdvance(in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in CampaignMissionArmorBreakState mission, bool opening, out CampaignMissionRuntimeComponent next)
        {
            next = runtime;
            if (!Matches(in mission, in runtime) || runtime.Outcome != MissionOutcomeKind.None || runtime.Phase >= MissionPhaseKind.Result) return false;
            var phase = runtime.Phase;
            var outcome = MissionOutcomeKind.None;
            if (phase == MissionPhaseKind.Preparing && (runtime.ReadyReadiness & runtime.RequiredReadiness) == runtime.RequiredReadiness) phase = MissionPhaseKind.InteractiveBrief;
            else if (phase == MissionPhaseKind.InteractiveBrief && facts.InteractiveBriefCompleted != 0) phase = MissionPhaseKind.FindSquad;
            else if (phase >= MissionPhaseKind.FindSquad && mission.Failure != ArmorBreakFailure.None) { phase = MissionPhaseKind.Result; outcome = MissionOutcomeKind.Defeat; }
            else if (phase == MissionPhaseKind.FindSquad && mission.Ready != 0 && opening) phase = MissionPhaseKind.Engage;
            else if (phase == MissionPhaseKind.Engage && VictoryQualified(in mission, in facts)) { phase = MissionPhaseKind.Result; outcome = MissionOutcomeKind.Victory; }
            return phase != runtime.Phase && CampaignMissionRuntimeSystem.TryTransition(in runtime, phase, outcome,
                outcome == MissionOutcomeKind.None ? MissionReturnDestinationKind.None : MissionReturnDestinationKind.CampaignOperations, out next);
        }
    }
}
