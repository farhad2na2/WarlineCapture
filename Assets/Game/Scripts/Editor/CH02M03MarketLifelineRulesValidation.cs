using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH02M03MarketLifelineRulesValidation
    {
        [MenuItem("Game/Campaign/Market Lifeline/Validate Rules")]
        public static void Run()
        {
            var rules=new CampaignMissionMarketLifelineDefinitionBlob {RequiredDeliveries=3,ManifestHoldMilliseconds=6000,VictoryHoldMilliseconds=10000,DeadlineMilliseconds=600000};
            var state=new CampaignMissionMarketLifelineState {DeliveredCount=3,LegitimateManifestInspected=1,CorruptManifestInspected=1,ManifestVerified=1,VictoryHoldMilliseconds=10000};Require(CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Complete facts must win.");
            state.ManifestVerified=0;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Delivery cannot bypass both manifest inspections.");state.ManifestVerified=1;state.DeliveredCount=2;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"Partial delivery cannot win.");state.DeliveredCount=3;state.Failure=MarketLifelineFailure.WrongTransfer;Require(!CampaignMissionMarketLifelineRuleUtility.IsVictory(in state,in rules),"The copied transfer cannot settle victory.");
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.SupplyLine)==CampaignMissionSequence.MarketLifeline,"Supply Line must unlock Market Lifeline.");Require(CampaignMissionSequence.Next(CampaignMissionSequence.MarketLifeline)==CampaignMissionSequence.PowerRelay,"Market Lifeline must unlock Power Relay.");
            Debug.Log("[MarketLifelineRules] result=Passed groups=6 buttons=existing-only");
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
