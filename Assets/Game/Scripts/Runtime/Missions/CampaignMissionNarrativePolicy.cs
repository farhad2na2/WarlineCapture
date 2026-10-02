using Unity.Collections;
using Game.Components;
using Game.Missions.Contracts;

namespace Game.Runtime
{
    internal static class CampaignMissionNarrativePolicy
    {
        private static readonly FixedString64Bytes M01 = "saga.ch01.m01.first_contact";
        // Initial onboarding already plays this story before handing off to M1.
        // Campaign entries and retries own a fresh playback, independently of save progress.
        internal static bool UsesFirstContactOpening(in CampaignMissionRuntimeComponent runtime) =>
            runtime.MissionId.Equals(M01) &&
            (runtime.LaunchOrigin != MissionLaunchOriginKind.FirstLaunch || runtime.AttemptOrdinal > 0);

        private static readonly FixedString64Bytes M02 = "saga.ch01.m02.establish_base";
        private static readonly FixedString64Bytes M03 = "saga.ch01.m03.radar_warning";
        private static readonly FixedString64Bytes SupplyLine = "saga.ch02.m02.supply_line";
        private static readonly FixedString64Bytes Gridlock = "saga.ch02.m01.gridlock";
        private static readonly FixedString64Bytes MarketLifeline = "saga.ch02.m03.market_lifeline";
        private static readonly FixedString64Bytes PowerRelay = "saga.ch02.m04.power_relay";
        private static readonly FixedString64Bytes RouteReopened = "saga.ch02.m05.route_reopened";
        private static readonly FixedString64Bytes SignalTrace = "saga.ch03.m01.signal_trace";
        private static readonly FixedString64Bytes SafehouseSweep = "saga.ch03.m02.safehouse_sweep";
        private static readonly FixedString64Bytes FalseFront = "saga.ch03.m03.false_front";
        private static readonly FixedString64Bytes EvidenceChain = CampaignMissionSequence.EvidenceChain;
        private static readonly FixedString64Bytes CitywideAlert = CampaignMissionSequence.CitywideAlert;
        private static readonly FixedString64Bytes GroundedSignal = CampaignMissionSequence.GroundedSignal;
        private static readonly FixedString64Bytes SplitFront = CampaignMissionSequence.SplitFront;
        private static readonly FixedString64Bytes SteelPush = CampaignMissionSequence.SteelPush;
        private static readonly FixedString64Bytes AirCorridor = CampaignMissionSequence.AirCorridor;
        private static readonly FixedString64Bytes NetworkBreak = CampaignMissionSequence.NetworkBreak;
        private static readonly FixedString64Bytes M05 = "saga.ch01.m05.breach_assault";
        private static readonly FixedString64Bytes M04 = "saga.ch01.m04.airlift";
        private static readonly FixedString64Bytes MixedDebrief = "seq.ch01.m03.debrief.mixed";
        private static readonly FixedString64Bytes CivilianLossDebrief = "seq.ch01.m03.debrief.civilian-loss";
        private static readonly FixedString64Bytes DamagedDebrief = "seq.ch01.m03.debrief.damaged";
        private static readonly FixedString64Bytes CleanDebrief = "seq.ch01.m03.debrief.clean";
        internal static bool UsesBlockingComms(in FixedString64Bytes missionId, in CampaignMissionAttemptFactsComponent facts) =>
            missionId.Equals(CitywideAlert) && facts.CitywideCommsReady != 0 ||
            missionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)) && facts.ArmorBreakHeavyDisabled != 0 && facts.ArmorBreakCommandDisabled == 0 ||
            missionId.Equals(GroundedSignal) && facts.GroundedSignalInserted!=0 && facts.ExtractionDeparted==0 ||
            missionId.Equals(M02) && facts.DefenseWaveWarningIssued!=0 && facts.DefenseWaveActivated==0 ||
            missionId.Equals(SplitFront) && facts.ElapsedMilliseconds>=20000 && facts.DefenseWaveActivated==0 ||
            missionId.Equals(SteelPush) && facts.HostileDefeatedCount>=2 && facts.DefenseWaveActivated==0 ||
            missionId.Equals(AirCorridor) && facts.HostileDefeatedCount>=3 && facts.DefenseWaveActivated==0 ||
            missionId.Equals(NetworkBreak) && facts.BreachGateDestroyed!=0 && facts.BreachCoreDestroyed==0 ||
            missionId.Equals(MarketLifeline) && facts.MarketManifestVerified!=0 && facts.MarketOpen==0 ||
            missionId.Equals(SignalTrace) && facts.MarketManifestVerified!=0 && facts.MarketOpen==0 ||
            missionId.Equals(SafehouseSweep) && facts.MarketManifestVerified!=0 && facts.MarketOpen==0 ||
            missionId.Equals(FalseFront) && facts.MarketManifestVerified!=0 && facts.MarketOpen==0 ||
            missionId.Equals(EvidenceChain) && facts.ExtractionCarrierLegCount>=2 && facts.ExtractionDeparted==0 ||
            missionId.Equals(RouteReopened) && facts.RouteRelayNodeActivated!=0 && facts.RouteRecordsPreserved==0 ||
            missionId.Equals(PowerRelay) && facts.PowerSafeRouteConfirmed==0;
        internal static bool UsesMissionSequences(in FixedString64Bytes missionId) =>
            missionId.Equals(CitywideAlert) || missionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)) || missionId.Equals(GroundedSignal) || missionId.Equals(SplitFront) || missionId.Equals(SteelPush) || missionId.Equals(AirCorridor) || missionId.Equals(NetworkBreak) || missionId.Equals(EvidenceChain) || missionId.Equals(FalseFront) || missionId.Equals(SafehouseSweep) || missionId.Equals(SignalTrace) || missionId.Equals(RouteReopened) || missionId.Equals(PowerRelay) || missionId.Equals(MarketLifeline) || missionId.Equals(SupplyLine) || missionId.Equals(Gridlock) || missionId.Equals(M02) || missionId.Equals(M03) || missionId.Equals(M04) || missionId.Equals(M05);
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
