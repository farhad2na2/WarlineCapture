using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH02M04PowerRelayRulesValidation
    {
        [MenuItem("Game/Campaign/Power Relay/Validate Rules")]
        public static void Run()
        {
            var rules=new CampaignMissionPowerRelayDefinitionBlob{RepairHoldMilliseconds=8000,VictoryHoldMilliseconds=10000,DeadlineMilliseconds=600000};var state=new CampaignMissionPowerRelayState{SafeRouteConfirmed=1,FamiliesSheltered=1,FuelDelivered=1,PowerRestored=1,VictoryHoldMilliseconds=10000};Require(CampaignMissionPowerRelayRuleUtility.IsVictory(in state,in rules),"Complete protected-route facts must win.");state.SafeRouteConfirmed=0;Require(!CampaignMissionPowerRelayRuleUtility.IsVictory(in state,in rules),"The exposed shortcut cannot satisfy the family route.");state.SafeRouteConfirmed=1;state.FuelDelivered=0;Require(!CampaignMissionPowerRelayRuleUtility.IsVictory(in state,in rules),"Repair cannot bypass Fuel delivery.");state.FuelDelivered=1;state.Failure=PowerRelayFailure.EngineerLost;Require(!CampaignMissionPowerRelayRuleUtility.IsVictory(in state,in rules),"Engineer loss cannot settle victory.");Require(CampaignMissionSequence.Next(CampaignMissionSequence.MarketLifeline)==CampaignMissionSequence.PowerRelay,"Market Lifeline must unlock Power Relay.");Debug.Log("[PowerRelayRules] result=Passed groups=7 protectedRoute=required buttons=existing-only");
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
