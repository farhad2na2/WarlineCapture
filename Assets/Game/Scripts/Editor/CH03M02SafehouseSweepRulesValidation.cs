using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M02SafehouseSweepRulesValidation
    {
        [MenuItem("Game/Campaign/Safehouse Sweep/Validate Rules")]
        public static void Run()
        {
            var rules=new CampaignMissionMarketLifelineDefinitionBlob{RequiredDeliveries=3,ManifestHoldMilliseconds=6000,VictoryHoldMilliseconds=10000,DeadlineMilliseconds=600000};
            var state=new CampaignMissionMarketLifelineState{LegitimateManifestInspected=1,CorruptManifestInspected=1,ManifestVerified=1,DeliveredCount=3,VictoryHoldMilliseconds=10000};
            Require(CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Both entrances, all protected homes and the ledger capture must win.");
            state.CorruptManifestInspected=0;state.ManifestVerified=0;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"One entrance cannot confirm a sealed perimeter.");
            state.CorruptManifestInspected=1;state.ManifestVerified=1;state.DeliveredCount=2;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"All protected civilian assets must remain safe.");
            state.DeliveredCount=3;state.Failure=MarketLifelineFailure.WrongTransfer;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Breaching the protected family home must fail the operation.");
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.SignalTrace)==CampaignMissionSequence.SafehouseSweep,"Signal Trace must unlock Safehouse Sweep.");
            Debug.Log("[SafehouseSweepRules] result=Passed entrances=2 protectedAssets=3 ledgerCapture=required wrongHouse=failure controls=existing-only");
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
