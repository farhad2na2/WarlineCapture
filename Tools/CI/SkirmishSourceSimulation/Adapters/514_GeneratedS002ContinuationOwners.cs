using System;using Unity.Entities;
namespace Game.Runtime {
 internal sealed partial class AutonomousWorld {
  SkirmishArmyGroupSystem continuationGroups;SystemState continuationGroupsState;
  SkirmishCapacityLifecycleSystem continuationCapacity;SystemState continuationCapacityState;
  SkirmishResearchSystem continuationResearch;SystemState continuationResearchState;
  VehicleWreckCleanupSystem sourceVehicleWreck;SystemState sourceVehicleWreckState;bool vehicleWreckCreated,continuationOwnersCreated;
  internal int ContinuationGroupUpdates {get;private set;}internal int ContinuationCapacityUpdates{get;private set;}internal int ContinuationResearchUpdates{get;private set;}internal int ContinuationWreckUpdates{get;private set;}
  internal void InitializeActualContinuationOwners(){
   if(continuationOwnersCreated)throw new InvalidOperationException("Original continuation owners initialize once");
   continuationGroupsState=FreshState();continuationGroupsState.Begin();try{continuationGroups.OnCreate(ref continuationGroupsState);}finally{continuationGroupsState.End();}continuationGroupsState.LastSystemVersion=0;
   continuationCapacityState=FreshState();continuationCapacityState.Begin();try{continuationCapacity.OnCreate(ref continuationCapacityState);}finally{continuationCapacityState.End();}continuationCapacityState.LastSystemVersion=0;
   continuationResearchState=FreshState();continuationResearchState.Begin();try{continuationResearch.OnCreate(ref continuationResearchState);}finally{continuationResearchState.End();}continuationResearchState.LastSystemVersion=0;
   if(!vehicleWreckCreated)InitializeActualVehicleWreckOwner();continuationOwnersCreated=true;
  }
  void RequireContinuationOwners(){if(!continuationOwnersCreated)throw new InvalidOperationException("Original continuation owners not initialized");}
  // Native partial order: capacity precedes objective-fact projection; groups and research follow capacity.
  internal void AdvanceActualCapacityLifecycle(){RequireContinuationOwners();continuationCapacityState.Begin();try{if(continuationCapacityState.Enabled&&continuationCapacityState.RequiredQueriesPresent){continuationCapacity.OnUpdate(ref continuationCapacityState);ContinuationCapacityUpdates++;}}finally{continuationCapacityState.End();}}
  internal void AdvanceActualArmyGroups(){RequireContinuationOwners();continuationGroupsState.Begin();try{if(continuationGroupsState.Enabled&&continuationGroupsState.RequiredQueriesPresent){continuationGroups.OnUpdate(ref continuationGroupsState);ContinuationGroupUpdates++;}}finally{continuationGroupsState.End();}}
  internal void AdvanceActualResearch(){RequireContinuationOwners();continuationResearchState.Begin();try{if(continuationResearchState.Enabled&&continuationResearchState.RequiredQueriesPresent){continuationResearch.OnUpdate(ref continuationResearchState);ContinuationResearchUpdates++;}}finally{continuationResearchState.End();}}
  // Existing roster initialization calls this same method; no second wreck owner is created.
  void InitializeActualVehicleWreckOwner(){if(vehicleWreckCreated)throw new InvalidOperationException("Original wreck owner creates once");sourceVehicleWreckState=FreshState();sourceVehicleWreckState.Begin();try{sourceVehicleWreck.OnCreate(ref sourceVehicleWreckState);}finally{sourceVehicleWreckState.End();}sourceVehicleWreckState.LastSystemVersion=0;vehicleWreckCreated=true;}
  // Native UpdateAfter(UnitDeathSystem): retain original queue/countdown/finalization, including query absence.
  void AdvanceActualVehicleWrecks(){if(!vehicleWreckCreated)throw new InvalidOperationException("Original wreck owner missing");sourceVehicleWreckState.Begin();try{if(sourceVehicleWreckState.Enabled&&sourceVehicleWreckState.RequiredQueriesPresent){sourceVehicleWreck.OnUpdate(ref sourceVehicleWreckState);ContinuationWreckUpdates++;}}finally{sourceVehicleWreckState.End();}}
 }
}
