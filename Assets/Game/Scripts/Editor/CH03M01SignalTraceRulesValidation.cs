using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CH03M01SignalTraceRulesValidation
    {
        [MenuItem("Game/Campaign/Signal Trace/Validate Rules")]
        public static void Run()
        {
            var rules=new CampaignMissionMarketLifelineDefinitionBlob{RequiredDeliveries=3,ManifestHoldMilliseconds=6000,VictoryHoldMilliseconds=10000,DeadlineMilliseconds=600000};
            var state=new CampaignMissionMarketLifelineState{LegitimateManifestInspected=1,CorruptManifestInspected=1,ManifestVerified=1,DeliveredCount=3,VictoryHoldMilliseconds=10000};
            Require(CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Both observation points, three protected signals and an open district must win.");state.CorruptManifestInspected=0;state.ManifestVerified=0;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"One observation point cannot confirm the hostile carrier.");state.CorruptManifestInspected=1;state.ManifestVerified=1;state.DeliveredCount=2;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"All protected civilian signals must be preserved.");state.DeliveredCount=3;state.Failure=MarketLifelineFailure.WrongTransfer;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Targeting a protected transmitter must fail the operation.");Require(CampaignMissionSequence.Next(CampaignMissionSequence.RouteReopened)==CampaignMissionSequence.SignalTrace,"Route Reopened must unlock Signal Trace.");
            Debug.Log("[SignalTraceRules] result=Passed observations=2 protectedSignals=3 confirmedEscort=required buttons=existing-only");
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
