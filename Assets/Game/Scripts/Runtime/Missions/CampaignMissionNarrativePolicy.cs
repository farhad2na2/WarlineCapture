using Unity.Collections;
using Game.Components;

namespace Game.Runtime
{
    internal static class CampaignMissionNarrativePolicy
    {
        private static readonly FixedString64Bytes M02 = "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes M03 = "saga.ch01.m03.radar_warning";
        internal static bool UsesBlockingComms(in FixedString64Bytes missionId, in CampaignMissionAttemptFactsComponent facts) =>
            missionId.Equals(M02) && facts.DefenseWaveWarningIssued!=0 && facts.DefenseWaveActivated==0;
        internal static bool UsesMissionSequences(in FixedString64Bytes missionId) =>
            missionId.Equals(M02) || missionId.Equals(M03) || missionId.Equals(new FixedString64Bytes("saga.ch01.m04.airlift"));
        internal static FixedString64Bytes ResolveDebrief(in FixedString64Bytes missionId,
            in CampaignMissionAttemptFactsComponent facts, in FixedString64Bytes fallback)
        {
            if(!missionId.Equals(M03)) return fallback;
            if(facts.CivilianLossCount>0) return facts.ForwardPostDamaged!=0
                ? new FixedString64Bytes("seq.ch01.m03.debrief.mixed") : new FixedString64Bytes("seq.ch01.m03.debrief.civilian-loss");
            return facts.ForwardPostDamaged!=0
                ? new FixedString64Bytes("seq.ch01.m03.debrief.damaged") : new FixedString64Bytes("seq.ch01.m03.debrief.clean");
        }
    }
}
