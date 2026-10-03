using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    // Focused save/queue regression. Synthetic deterministic fixtures are not player-readiness evidence.
    public static class CH05M05CommandNodeRecoveryValidation
    {
        private static readonly string[] Missions={CampaignMissionSequence.MarketLifeline,CampaignMissionSequence.PowerRelay,CampaignMissionSequence.CitywideAlert,CampaignMissionSequence.TrustUnderFire,CampaignMissionSequence.LastCorridor,CampaignMissionSequence.EvidenceChain,CampaignMissionSequence.NetworkBreak,CampaignMissionSequence.NetworkCollapse,CampaignMissionSequence.RouteReopened};
        public static void Run()
        {
            string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"command-node-recovery-"+Guid.NewGuid().ToString("N"));
            try
            {
                for(int mask=0;mask<8;mask++)ValidateMask(System.IO.Path.Combine(directory,"mask-"+mask),mask);
                ValidateRejected(System.IO.Path.Combine(directory,"negative"));
                ValidateLegacy(System.IO.Path.Combine(directory,"legacy"));
                Debug.Log("[CommandNodeRecoveryValidation] result=Passed masks=8 legacyUnknown=Low exactSettledTokens=Required unrelated=Rejected stale=Rejected corrupt=Rejected duplicate=Stable firstFinale=Stable canonicalClose=8 canonicalEpilogue=9 postscript=3 starsProxy=0 endingGates=0");
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        private static CampaignMissionProgressStore Store(string directory)=>new(new SaveService(new JsonSaveRepository(directory)));
        private static void Settle(CampaignMissionProgressStore store,string id,string token,int attempt=1)
        {
            store.EnsureAvailable(id);
            var receipt=store.SettleWithRewards(id,token,attempt,true,1,1000,CampaignMissionSequence.Next(id),Array.Empty<CampaignMissionRewardGrant>(),false);
            Require(receipt.Applied||receipt.IsDuplicate,"Ordinary settlement rejected: "+id);
        }
        private static CampaignMissionAttemptFactsComponent Qualifying(string id)
        {
            var f=new CampaignMissionAttemptFactsComponent();
            if(id==CampaignMissionSequence.EvidenceChain){f.ExtractionPassengerTotal=2;f.ExtractionPassengersDelivered=2;f.ExtractionDeparted=1;}
            if(id==CampaignMissionSequence.NetworkBreak){f.BreachGateDestroyed=1;f.BreachCoreDestroyed=1;f.BreachArchiveSecured=1;}
            if(id==CampaignMissionSequence.NetworkCollapse){f.NetworkAuditRecovered=1;f.NetworkExtracted=1;}
            if(id==CampaignMissionSequence.PowerRelay){f.PowerRestored=1;f.PowerRelaySecured=1;}
            if(id==CampaignMissionSequence.RouteReopened){f.RouteLinkRestored=1;f.RouteReliefDelivered=1;f.RouteFuelDelivered=1;}
            if(id==CampaignMissionSequence.CitywideAlert){f.CitywideClinicRecovered=1;f.CitywideUtilityRecovered=1;}
            if(id==CampaignMissionSequence.LastCorridor){f.CorridorLinkRecovered=1;f.CorridorSuppliesDelivered=3;f.CorridorFuelReceived=40;f.CorridorKeysDelivered=1;}
            return f;
        }
        private static void ValidateMask(string directory,int mask)
        {
            var store=Store(directory);
            foreach(string id in Missions)
            {
                string token="actual-"+id;Settle(store,id,token);var facts=Qualifying(id);
                // Missing genuine history independently represents each conservative recovery family.
                if((mask&1)==0&&id==CampaignMissionSequence.MarketLifeline || (mask&2)==0&&id==CampaignMissionSequence.EvidenceChain || (mask&4)==0&&id==CampaignMissionSequence.RouteReopened)continue;
                store.RecordRecoveryEvidence(id,token,1,in facts);
            }
            store=Store(directory);Require(store.ResolveRecordedRecoveryEmphasis()==mask,"Save roundtrip changed emphasis mask "+mask);
            int[] panels={4,8,9,1,1,1,3};int sum=0;
            var configs=AssetDatabase.LoadAllAssetsAtPath(CH05M05CommandNodeNarrativeBuilder.Path).OfType<NarrativeSequenceConfig>().ToArray();
            for(int index=0;index<7;index++)
            {
                string id=CampaignMissionDebriefCompositionSystemHelper.CommandNodeFinaleSequence(index,mask);
                var config=configs.Single(x=>x.SequenceId==id);Require(config.States.Count==panels[index]+1,"Canonical queue topology changed: "+id);
                Require(config.States.Last().CompletionPayloadId==CampaignMissionDebriefCompositionSystemHelper.CommandNodeFinalePayload(index,mask),"Wrong actual queue completion payload: "+id);sum+=panels[index];
            }
            Require(sum==27,"Finale panel sum changed");
            Settle(store,CampaignMissionSequence.CommandNode,"finale-real");
            store.RecordCommandNodeFinale("finale-real",1,mask);
            store.RecordCommandNodeFinale("not-settled",2,mask^7);
            Require(Store(directory).ReadCommandNodeEmphasis()==mask,"Stale finale receipt rewrote first recorded emphasis");
            var replay=store.SettleWithRewards(CampaignMissionSequence.CommandNode,"finale-replay",2,false,3,900,CampaignMissionSequence.Next(CampaignMissionSequence.CommandNode),Array.Empty<CampaignMissionRewardGrant>(),true);
            Require(replay.Applied,"Actual replay settlement failed");store.RecordCommandNodeFinale("finale-replay",2,mask^7);
            Require(Store(directory).ReadCommandNodeEmphasis()==mask,"Replay rewrote first recorded finale");
        }
        private static void ValidateRejected(string directory)
        {
            var store=Store(directory);string id=CampaignMissionSequence.EvidenceChain;var facts=Qualifying(id);
            store.RecordRecoveryEvidence(id,"not-settled",1,in facts);
            Require(!store.ReadAll().Any(x=>x.recoveryEvidenceRecorded),"Unsettled evidence accepted");
            Settle(store,id,"actual-token");
            store.RecordRecoveryEvidence(id,"wrong-token",1,in facts);store.RecordRecoveryEvidence(id,"actual-token",2,in facts);
            Require(!store.ReadAll().Single(x=>x.missionId==id).recoveryEvidenceRecorded,"Stale or unrelated attempt accepted");
            var corrupt=facts;corrupt.CivilianLossCount=-1;store.RecordRecoveryEvidence(id,"actual-token",1,in corrupt);
            Require(!store.ReadAll().Single(x=>x.missionId==id).recoveryEvidenceRecorded,"Corrupt negative loss accepted");
            store.RecordRecoveryEvidence(id,"actual-token",1,in facts);var changed=facts;changed.ExtractionDeparted=0;
            store.RecordRecoveryEvidence(id,"actual-token",1,in changed);
            Require(Store(directory).ReadAll().Single(x=>x.missionId==id).recoveryAuditCustody,"Duplicate corrupted first custody receipt");
            Settle(store,CampaignMissionSequence.CommandNode,"end-token");store.RecordCommandNodeFinale("wrong-token",1,7);
            Require(!store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.CommandNode).commandNodeEmphasisRecorded,"Unsettled finale accepted");
            Settle(store,CampaignMissionSequence.Gridlock,"unrelated");var unrelated=Qualifying(CampaignMissionSequence.NetworkCollapse);
            store.RecordRecoveryEvidence(CampaignMissionSequence.Gridlock,"unrelated",1,in unrelated);
            Require(store.ResolveRecordedRecoveryEmphasis()==0,"Unrelated mission facts earned false high family");
        }
        private static void ValidateLegacy(string directory)
        {
            var save=new SaveService(new JsonSaveRepository(directory));
            save.SaveProfile(new PlayerProfileSaveData{campaignMissionProgress=Missions.Select(id=>new CampaignMissionProgressSaveData{missionId=id,firstClearCompleted=true,bestStars=3,available=true}).ToArray()});
            var store=Store(directory);Require(store.ResolveRecordedRecoveryEmphasis()==0&&store.ReadCommandNodeEmphasis()==0,"Legacy stars inferred false recovery history");
        }
        private static void Require(bool condition,string detail){if(!condition)throw new InvalidOperationException(detail);}
    }
}
