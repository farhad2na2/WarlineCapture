using Game.Components;
using Game.Missions.Contracts;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class CampaignMissionTrustUnderFireRuleUtility
    {
        public const int HoldMilliseconds = 6000, DeadlineMilliseconds = 900000, MilitaryCount = 9, OriginalCount = 26;
        public static bool Matches(in CampaignMissionTrustUnderFireState mission, in CampaignMissionRuntimeComponent runtime) =>
            mission.Initialized != 0 && mission.SessionToken.Equals(runtime.SessionToken) && mission.AttemptOrdinal == runtime.AttemptOrdinal && mission.SourceVersion == runtime.SourceVersion;
        public static int Stage(in CampaignMissionTrustUnderFireState mission) => mission.NorthCleared == 0 ? 1 : mission.NorthArrived == 0 ? 2 :
            mission.SouthCleared == 0 ? 3 : mission.SouthArrived == 0 ? 4 : mission.RelayApproached == 0 && mission.RelayCleared == 0 ? 5 : mission.RelayCleared == 0 ? 6 : mission.RelayVerified == 0 ? 7 : 8;
        public static bool ShouldFailEscortLoss(int initializedOriginals, int livingEscorts, int militaryCount, int defeated) =>
            initializedOriginals == OriginalCount && livingEscorts == 0 && militaryCount == MilitaryCount && defeated < militaryCount;
        // Incomplete transit must remain on this bridge lane: west entry, deck, east exit.
        public static byte AdvancePassage(byte phase, float3 position, float3 crossing)
        {
            if (phase >= 3) return 3;
            float2 offset = position.xz - crossing.xz;
            if (!math.all(math.isfinite(offset)) || math.abs(offset.y) > 8 || math.abs(offset.x) > 38) return 0;
            if (phase == 0 && offset.x <= -26) return 1;
            if (phase == 1 && math.abs(offset.x) <= 8) return 2;
            if (phase == 2 && offset.x >= 26) return 3;
            return phase;
        }
        public static float3 PassageWaypoint(byte phase, float3 crossing) => crossing + new float3(phase == 0 ? -30 : phase == 1 ? 0 : 30, 0, 0);
        public static int AdvanceHold(int current, int delta, bool qualified) => !qualified ? 0 :
            current >= HoldMilliseconds - System.Math.Max(0, delta) ? HoldMilliseconds : current + System.Math.Max(0, delta);
        public static bool VictoryQualified(in CampaignMissionTrustUnderFireState mission, in CampaignMissionAttemptFactsComponent facts) =>
            mission.Ready != 0 && mission.Failure == TrustUnderFireFailure.None && mission.NorthCrossed != 0 && mission.SouthCrossed != 0 &&
            mission.NorthArrived != 0 && mission.SouthArrived != 0 && mission.RelayVerified != 0 && mission.MilitaryCleared != 0 &&
            mission.StableMilliseconds >= HoldMilliseconds && facts.HostileTotalCount == MilitaryCount && facts.HostileDefeatedCount == MilitaryCount &&
            facts.CivilianTotalCount == 4 && facts.CivilianLossCount == 0;
        public static bool TryAdvance(in CampaignMissionRuntimeComponent runtime, in CampaignMissionAttemptFactsComponent facts,
            in CampaignMissionTrustUnderFireState mission, bool opening, out CampaignMissionRuntimeComponent next)
        {
            next = runtime;
            if (!Matches(in mission, in runtime) || runtime.Outcome != MissionOutcomeKind.None || runtime.Phase >= MissionPhaseKind.Result) return false;
            var phase = runtime.Phase; var outcome = MissionOutcomeKind.None;
            if (phase >= MissionPhaseKind.FindSquad && mission.Failure != TrustUnderFireFailure.None) { phase = MissionPhaseKind.Result; outcome = MissionOutcomeKind.Defeat; }
            else if (phase == MissionPhaseKind.FindSquad && mission.Ready != 0 && opening) phase = MissionPhaseKind.Engage;
            else if (phase == MissionPhaseKind.Engage && VictoryQualified(in mission, in facts)) { phase = MissionPhaseKind.Result; outcome = MissionOutcomeKind.Victory; }
            return phase != runtime.Phase && CampaignMissionRuntimeSystem.TryTransition(in runtime, phase, outcome,
                outcome == MissionOutcomeKind.None ? MissionReturnDestinationKind.None : MissionReturnDestinationKind.CampaignOperations, out next);
        }
    }
}
