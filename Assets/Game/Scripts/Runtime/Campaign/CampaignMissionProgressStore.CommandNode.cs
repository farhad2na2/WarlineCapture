using System;
using Game.Missions.Contracts;
using Game.Components;
using System.Linq;
namespace Game.Runtime
{
    public sealed partial class CampaignMissionProgressStore
    {
        public void RecordRecoveryEvidence(string missionId,string session,int attempt,in CampaignMissionAttemptFactsComponent f)
        {
            if(string.IsNullOrWhiteSpace(session)||attempt<0)throw new ArgumentException("Recovery evidence requires a settled attempt");
            var profile=_saveService.LoadProfile();var entries=ToList(profile.campaignMissionProgress);var entry=FindOrAdd(entries,missionId);
            if(!entry.firstClearCompleted || entry.recoveryEvidenceRecorded || f.CivilianLossCount<0 || f.SquadLossCount<0 ||
                !Contains(entry.settledTokens,session+":"+attempt))return;
            bool audit=missionId==CampaignMissionSequence.NetworkCollapse ? f.NetworkAuditRecovered!=0&&f.NetworkExtracted!=0 :
                missionId==CampaignMissionSequence.NetworkBreak ? f.BreachGateDestroyed!=0&&f.BreachCoreDestroyed!=0&&f.BreachArchiveSecured!=0 :
                missionId==CampaignMissionSequence.EvidenceChain&&f.ExtractionPassengerTotal>0&&f.ExtractionPassengersDelivered==f.ExtractionPassengerTotal&&f.ExtractionDeparted!=0;
            bool infrastructure=missionId switch {
                CampaignMissionSequence.PowerRelay=>f.PowerRestored!=0&&f.PowerRelaySecured!=0,
                CampaignMissionSequence.RouteReopened=>f.RouteLinkRestored!=0&&f.RouteReliefDelivered!=0&&f.RouteFuelDelivered!=0,
                CampaignMissionSequence.CitywideAlert=>f.CitywideClinicRecovered!=0&&f.CitywideUtilityRecovered!=0,
                CampaignMissionSequence.LastCorridor=>f.CorridorLinkRecovered!=0&&f.CorridorSuppliesDelivered==3&&f.CorridorFuelReceived==40&&f.CorridorKeysDelivered!=0,
                _=>false};
            entry.recoveryEvidenceRecorded=true;entry.recoveryCivilianLosses=f.CivilianLossCount;entry.recoverySquadLosses=f.SquadLossCount;
            entry.recoveryAuditCustody=audit;entry.recoveryInfrastructurePreserved=infrastructure;
            entry.recoveryEvidenceReceipt=session+":"+attempt;Save(profile,entries);
        }
        public int ResolveRecordedRecoveryEmphasis()
        {
            var entries=ReadAll();
            bool All(string[] ids,Func<CampaignMissionProgressSaveData,bool> predicate)=>ids.All(id=>entries.Any(e=>e.missionId==id&&e.firstClearCompleted&&e.recoveryEvidenceRecorded&&predicate(e)));
            bool trust=All(new[]{CampaignMissionSequence.MarketLifeline,CampaignMissionSequence.PowerRelay,CampaignMissionSequence.CitywideAlert,CampaignMissionSequence.TrustUnderFire,CampaignMissionSequence.LastCorridor},e=>e.recoveryCivilianLosses==0);
            bool evidence=All(new[]{CampaignMissionSequence.EvidenceChain,CampaignMissionSequence.NetworkBreak,CampaignMissionSequence.NetworkCollapse},e=>e.recoveryAuditCustody);
            bool infrastructure=All(new[]{CampaignMissionSequence.PowerRelay,CampaignMissionSequence.RouteReopened,CampaignMissionSequence.CitywideAlert,CampaignMissionSequence.LastCorridor},e=>e.recoveryInfrastructurePreserved&&e.recoveryCivilianLosses==0&&e.recoverySquadLosses==0);
            return (trust?1:0)|(evidence?2:0)|(infrastructure?4:0);
        }
        public int ReadCommandNodeEmphasis()
        {
            var entry=Find(ToList(_saveService.LoadProfile().campaignMissionProgress),CampaignMissionSequence.CommandNode);
            return entry?.commandNodeEmphasisRecorded==true ? entry.commandNodeEmphasisMask&7 : 0;
        }
        public void RecordCommandNodeFinale(string sessionToken,int attemptOrdinal,int emphasisMask)
        {
            if(string.IsNullOrWhiteSpace(sessionToken)||attemptOrdinal<0)throw new ArgumentException("Finale receipt requires an actual attempt");
            var profile=_saveService.LoadProfile();var entries=ToList(profile.campaignMissionProgress);
            var entry=Find(entries,CampaignMissionSequence.CommandNode);
            if(entry==null || !entry.firstClearCompleted || !Contains(entry.settledTokens,sessionToken+":"+attemptOrdinal))return;
            // Preserve the first actual completed finale for archive playback. Replays do not rewrite its account.
            if(entry.commandNodeEmphasisRecorded)return;
            entry.commandNodeEmphasisRecorded=true;entry.commandNodeEmphasisMask=emphasisMask&7;
            entry.commandNodeFinaleReceipt=sessionToken+":"+attemptOrdinal+":canonical-postscript";
            Save(profile,entries);
        }
    }
}
