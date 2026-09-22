using System;
using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using UnityEngine;
namespace Game.Editor
{
    public static class CH02M02SupplyLineRulesValidation
    {
        public static void Run()
        {
            var rules=new CampaignMissionSupplyLineDefinitionBlob {Enabled=1,ReserveBarrels=40,CivilianReserveBarrels=20,HoldMilliseconds=20000,DeadlineMilliseconds=720000};
            var state=new CampaignMissionSupplyLineState {RouteRecovered=1,AllocatedCivilianBarrels=20,Ready=1,OilTransferred=1,FuelTransferred=1,StoredFuel=40,HoldMilliseconds=20000,ElapsedMilliseconds=300000};
            Require(CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"A real complete chain should win.");
            state.AllocatedCivilianBarrels=0;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"Civilian allocation is a required player decision.");state.AllocatedCivilianBarrels=20;
            state.RouteRecovered=0;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"A stockpile cannot bypass alternate-lane recovery.");state.RouteRecovered=1;
            state.OilTransferred=0;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"Existing storage cannot fake Oil transfer.");state.OilTransferred=1;
            state.FuelTransferred=0;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"Refinery stock cannot fake delivered reserve.");state.FuelTransferred=1;
            state.StoredFuel=39.99f;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"Reserve threshold must not round up.");state.StoredFuel=40;
            state.ElapsedMilliseconds=720000;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"Deadline wins a same-frame race.");state.ElapsedMilliseconds=300000;
            foreach(SupplyLineFailure failure in Enum.GetValues(typeof(SupplyLineFailure)))
            {if(failure==SupplyLineFailure.None)continue;state.Failure=failure;Require(!CampaignMissionSupplyLineRuleUtility.IsVictory(state,rules),"Failure cannot settle victory: "+failure);}
            Require(CampaignMissionSupplyLineRuleUtility.AdvanceHold(5000,1000,20000,false,true,true,40,40)==5000,"Pause must preserve hold without advancing.");
            Require(CampaignMissionSupplyLineRuleUtility.AdvanceHold(5000,1000,20000,true,true,true,39,40)==0,"Consuming reserve resets hold.");
            Require(CampaignMissionSupplyLineRuleUtility.AdvanceHold(5000,1000,20000,true,false,true,40,40)==0,"Broken link resets hold.");
            Require(CampaignMissionSupplyLineRuleUtility.AdvanceHold(5000,1000,20000,true,true,false,40,40)==0,"Active attackers block hold.");
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.Gridlock)==CampaignMissionSequence.SupplyLine,"Gridlock unlocks Supply Line.");
            Require(CampaignMissionSequence.Next(CampaignMissionSequence.SupplyLine)=="saga.ch02.m03.market_lifeline","Supply Line cannot link to itself.");
            ValidateCivilianReserve();
            Debug.Log("[SupplyLineRules] result=Passed groups=13 reserveRegression=Passed");
        }
        private static void ValidateCivilianReserve()
        {
            const byte fuel=BuildingResourceStorageTransferSystemHelper.FuelResourceKind;
            var storage=new BuildingResourceStorageComponent {StoredFuelBarrels=40,CivilianFuelReserveBarrels=20,FuelStorageCapacity=200};
            Require(BuildingResourceStorageTransferSystemHelper.GetAvailableSourceResource(storage,fuel)==20,"Civilian fuel is unavailable to hauling.");
            Require(!BuildingResourceStorageTransferSystemHelper.TryConsumeAvailableSourceResource(ref storage,fuel,21),"Consumption cannot cross the reserve floor.");
            Require(BuildingResourceStorageTransferSystemHelper.TryReserveSource(ref storage,fuel,8),"Relief fuel remains available.");
            Require(BuildingResourceStorageTransferSystemHelper.TryConsumeSourceReservation(ref storage,fuel,8),"A covered reservation can load.");
            Require(storage.StoredFuelBarrels==32 && storage.CivilianFuelReserveBarrels==20,"Pickup preserves civilian allocation.");
            storage.ReservedFuelOutboundBarrels=16;
            Require(!BuildingResourceStorageTransferSystemHelper.TryConsumeSourceReservation(ref storage,fuel,16),"A stale pickup cannot consume protected barrels.");
            storage.CivilianFuelReserveBarrels=0;storage.ReservedFuelOutboundBarrels=0;
            Require(BuildingResourceStorageTransferSystemHelper.GetAvailableSourceResource(storage,fuel)==32,"Other missions retain all stock availability.");
        }
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}
