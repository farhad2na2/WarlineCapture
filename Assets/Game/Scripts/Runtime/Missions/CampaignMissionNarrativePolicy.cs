using Unity.Collections;
using Game.Components;

namespace Game.Runtime
{
    internal static class CampaignMissionNarrativePolicy
    {
        private static readonly FixedString64Bytes M02 = "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes M03 = "saga.ch01.m03.radar_warning";
        private static readonly FixedString64Bytes M04 = "saga.ch01.m04.airlift";
        private static readonly FixedString64Bytes MixedDebrief = "seq.ch01.m03.debrief.mixed";
        private static readonly FixedString64Bytes CivilianLossDebrief = "seq.ch01.m03.debrief.civilian-loss";
        private static readonly FixedString64Bytes DamagedDebrief = "seq.ch01.m03.debrief.damaged";
        private static readonly FixedString64Bytes CleanDebrief = "seq.ch01.m03.debrief.clean";
        internal static bool UsesBlockingComms(in FixedString64Bytes missionId, in CampaignMissionAttemptFactsComponent facts) =>
            missionId.Equals(M02) && facts.DefenseWaveWarningIssued!=0 && facts.DefenseWaveActivated==0;
        internal static bool UsesMissionSequences(in FixedString64Bytes missionId) =>
            missionId.Equals(M02) || missionId.Equals(M03) || missionId.Equals(M04);
        internal static FixedString64Bytes ResolveDebrief(in FixedString64Bytes missionId,
            in CampaignMissionAttemptFactsComponent facts, in FixedString64Bytes fallback)
        {
            if(!missionId.Equals(M03)) return fallback;
            if(facts.CivilianLossCount>0) return facts.ForwardPostDamaged!=0
                ? MixedDebrief : CivilianLossDebrief;
            return facts.ForwardPostDamaged!=0
                ? DamagedDebrief : CleanDebrief;
        }
    }
}
