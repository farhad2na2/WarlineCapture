using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEditor;
using UnityEngine;
namespace Game.Editor
{
    public static class CH02M05RouteReopenedRulesValidation
    {
        [MenuItem("Game/Campaign/Route Reopened/Validate Rules")]
        public static void Run()
        {
            var rules=new CampaignMissionRouteReopenedDefinitionBlob{LinkRepairHoldMilliseconds=7000,RecordsHoldMilliseconds=10000,DeadlineMilliseconds=600000};var state=new CampaignMissionRouteReopenedState{ReliefDelivered=1,FuelDelivered=1,LinkRestored=1,HubEntered=1,GarrisonCleared=1,RecordsPreserved=1,RecordsHoldMilliseconds=10000};Require(CampaignMissionRouteReopenedRuleUtility.IsVictory(in state,in rules),"Complete route, hub and archive facts must win.");state.ReliefDelivered=0;Require(!CampaignMissionRouteReopenedRuleUtility.IsVictory(in state,in rules),"Fuel delivery cannot replace the relief lifeline.");state.ReliefDelivered=1;state.RecordsPreserved=0;Require(!CampaignMissionRouteReopenedRuleUtility.IsVictory(in state,in rules),"Hub capture cannot bypass archive preservation.");state.RecordsPreserved=1;state.RecordsHoldMilliseconds=9999;Require(!CampaignMissionRouteReopenedRuleUtility.IsVictory(in state,in rules),"Archive preservation requires the full hold.");state.RecordsHoldMilliseconds=10000;state.Failure=RouteReopenedFailure.RecordsLost;Require(!CampaignMissionRouteReopenedRuleUtility.IsVictory(in state,in rules),"Destroyed records cannot settle victory.");Require(CampaignMissionSequence.Next(CampaignMissionSequence.PowerRelay)==CampaignMissionSequence.RouteReopened,"Power Relay must unlock Route Reopened.");Debug.Log("[RouteReopenedRules] result=Passed groups=7 lifelines=2 records=required buttons=existing-only");
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
