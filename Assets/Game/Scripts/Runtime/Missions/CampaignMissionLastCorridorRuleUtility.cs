using Game.Components;
using Game.Missions.Contracts;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class CampaignMissionLastCorridorRuleUtility
    {
        public const int HoldMilliseconds = 6000, DeadlineMilliseconds = 900000, OriginalCount = 26, MilitaryCount = 7;
        public static bool Matches(in CampaignMissionLastCorridorState s, in CampaignMissionRuntimeComponent r) => s.Initialized != 0 && s.SessionToken.Equals(r.SessionToken) && s.AttemptOrdinal == r.AttemptOrdinal && s.SourceVersion == r.SourceVersion;
        public static int Stage(in CampaignMissionLastCorridorState s) => s.MilitaryCleared == 0 ? 1 : s.LinkRecovered == 0 ? 2 : s.MedicineDelivered == 0 ? 3 : s.FuelDelivered == 0 ? 4 : s.ReinforcementsDelivered == 0 ? 5 : s.EngineerReady == 0 ? 6 : s.EngineerAboard == 0 ? 7 : 8;
        public static int AdvanceHold(int current, int delta, bool qualified) => !qualified ? 0 : current >= HoldMilliseconds - math.max(0, delta) ? HoldMilliseconds : current + math.max(0, delta);
        // Proof is earned by physically visiting the ordered route points and
        // staying within the connected two-segment corridor until the exit.
        // Allow the observed legal junction turn (up to 11.5 m). The
        // exclusive 12 m boundary still rejects the other lane centre.
        public static byte AdvanceRoute(byte step, float3 position, float3 entry, float3 middle, float3 exit)
        {
            if (step >= 3) return 3;
            if (step != 0 && math.min(SegmentDistance(position.xz, entry.xz, middle.xz), SegmentDistance(position.xz, middle.xz, exit.xz)) >= 12) return 0;
            float3 target = step == 0 ? entry : step == 1 ? middle : exit;
            return math.distancesq(position.xz, target.xz) <= 25 ? (byte)(step + 1) : step;
        }
        private static float SegmentDistance(float2 p, float2 a, float2 b) { float2 d = b - a; float t = math.saturate(math.dot(p - a, d) / math.max(.001f, math.lengthsq(d))); return math.distance(p, a + d * t); }
        public static bool VictoryQualified(in CampaignMissionLastCorridorState s, in CampaignMissionAttemptFactsComponent f) => s.Ready != 0 && s.Failure == LastCorridorFailure.None && s.LinkRecovered != 0 && s.MedicineDelivered != 0 && s.FuelDelivered != 0 && s.DeliveredFuelBarrels == 40 && s.ReinforcementsDelivered != 0 && s.EngineerAboard != 0 && s.EngineerDelivered != 0 && s.KeysDelivered != 0 && s.KeyMilliseconds >= HoldMilliseconds && s.MilitaryCleared != 0 && f.HostileTotalCount == MilitaryCount && f.HostileDefeatedCount == MilitaryCount && f.CivilianTotalCount == 4 && f.CivilianLossCount == 0;
        public static bool TryAdvance(in CampaignMissionRuntimeComponent r, in CampaignMissionAttemptFactsComponent f, in CampaignMissionLastCorridorState s, bool opening, out CampaignMissionRuntimeComponent next)
        {
            next = r; if (!Matches(in s, in r) || r.Outcome != MissionOutcomeKind.None) return false;
            if (s.Failure != LastCorridorFailure.None) { next.Phase = MissionPhaseKind.Result; next.Outcome = MissionOutcomeKind.Defeat; next.ReturnDestination = MissionReturnDestinationKind.CampaignOperations; }
            else if (r.Phase == MissionPhaseKind.FindSquad && s.Ready != 0 && opening) next.Phase = MissionPhaseKind.Engage;
            else if (r.Phase == MissionPhaseKind.Engage && VictoryQualified(in s, in f)) { next.Phase = MissionPhaseKind.Result; next.Outcome = MissionOutcomeKind.Victory; next.ReturnDestination = MissionReturnDestinationKind.CampaignOperations; }
            else return false;
            next.Version = r.Version == uint.MaxValue ? 1 : r.Version + 1; return true;
        }
    }
}
