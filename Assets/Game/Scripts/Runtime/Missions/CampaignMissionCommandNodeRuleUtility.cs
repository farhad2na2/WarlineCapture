using Game.Components;
using Game.Missions.Contracts;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class CampaignMissionCommandNodeRuleUtility
    {
        public const int OriginalCount = 23, MilitaryCount = 10, HoldMilliseconds = 6000, DeadlineMilliseconds = 900000;
        public static bool Matches(in CampaignMissionCommandNodeState s, in CampaignMissionRuntimeComponent r) => s.Initialized != 0 && s.SessionToken.Equals(r.SessionToken) && s.AttemptOrdinal == r.AttemptOrdinal && s.SourceVersion == r.SourceVersion;
        public static int Stage(in CampaignMissionCommandNodeState s) => s.ExteriorCleared == 0 ? 1 : s.ClinicIsolated == 0 || s.UtilityIsolated == 0 ? 2 : s.NodeDisabled == 0 ? 3 : s.Breached == 0 ? 4 : s.MilitaryCleared == 0 ? 5 : s.AuditPreserved == 0 ? 6 : s.AuditReleased == 0 ? 7 : 8;
        public static int AdvanceHold(int current, int delta, bool qualified) => !qualified ? 0 : math.min(HoldMilliseconds, current + math.min(HoldMilliseconds, math.max(0, delta)));
        public static bool CanRelease(in CampaignMissionCommandNodeState s) => s.AuditPreserved != 0 && s.ReleaseOrdered != 0 && s.MilitaryCleared != 0 && s.Breached != 0;
        public static bool VictoryQualified(in CampaignMissionCommandNodeState s, in CampaignMissionAttemptFactsComponent f) => s.Ready != 0 && s.Failure == CommandNodeFailure.None && s.ClinicIsolated != 0 && s.UtilityIsolated != 0 && s.NodeDisabled != 0 && s.Breached != 0 && s.QassemDefeated != 0 && s.MilitaryCleared != 0 && s.AuditPreserved != 0 && s.AuditReleased != 0 && s.ReleaseOrdered != 0 && s.SpecialistsSafe != 0 && f.HostileTotalCount == MilitaryCount && f.HostileDefeatedCount == MilitaryCount && f.CivilianTotalCount == 4 && f.CivilianLossCount == 0;
        public static bool TryAdvance(in CampaignMissionRuntimeComponent r, in CampaignMissionAttemptFactsComponent f, in CampaignMissionCommandNodeState s, bool opening, out CampaignMissionRuntimeComponent next)
        {
            next = r; if (!Matches(in s, in r) || r.Outcome != MissionOutcomeKind.None) return false;
            if (s.Failure != CommandNodeFailure.None) { next.Phase = MissionPhaseKind.Result; next.Outcome = MissionOutcomeKind.Defeat; next.ReturnDestination = MissionReturnDestinationKind.CampaignOperations; }
            else if (r.Phase == MissionPhaseKind.FindSquad && s.Ready != 0 && opening) next.Phase = MissionPhaseKind.Engage;
            else if (r.Phase == MissionPhaseKind.Engage && VictoryQualified(in s, in f)) { next.Phase = MissionPhaseKind.Result; next.Outcome = MissionOutcomeKind.Victory; next.ReturnDestination = MissionReturnDestinationKind.CampaignOperations; }
            else return false;
            next.Version = r.Version == uint.MaxValue ? 1 : r.Version + 1; return true;
        }
    }
}
