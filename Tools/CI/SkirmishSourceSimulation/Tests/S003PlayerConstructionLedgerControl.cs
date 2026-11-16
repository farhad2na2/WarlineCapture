using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;

internal static class S003PlayerConstructionLedgerControl
{
 static void Need(bool ok,string label){if(!ok)throw new InvalidOperationException(label);}
 public static int Main(){try{
  int next=1;var em=new EntityManager {LogicalAllocatorAdmitted=true};em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
  var bank=em.CreateEntity();em.AddComponentData(bank,new FactionEconomy{FactionId=1,Money=1000});em.AddComponentData(bank,new FactionTacticalMaterialsComponent{FactionId=1,Current=200,Capacity=1000});
  var actor=em.CreateEntity();em.AddComponentData(actor,new SkirmishAttemptOwnedComponent{FactionId=1});
  var definition=new RuntimeDefinition{Prefab=new SourceUnitPrefab("FixtureTower",actor),CreditsCost=100,MaterialsCost=25};
  var placement=new SourceHostPlacementLifecycle.PlacementState{Definition=definition,IsValid=true,OriginCell=new Vector2Int(10,20)};
  var owner=new SourcePlayerConstructionReservation(new SourceCampResourceReads(em,bank));var lifecycle=new SourceHostPlacementLifecycle{ActivePlacement=placement};
  var rows=new List<SkirmishConstructionLedger.Observation>();Action<SkirmishConstructionLedger.Observation> sink=rows.Add;long faults=SkirmishConstructionLedger.Faults;SkirmishConstructionLedger.OpenOwned(sink);
  try{
   var ctx=new SourceHostPlacementLifecycle.ConfirmContext{ValidateConfirm=_=>true,TryReserveCost=owner.TryReserve,FinalizeCost=owner.TryFinalize,RollbackCost=owner.TryRollback,CommitPlacement=_=>new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity{CombatEntity=actor},1,1)};
   Need(lifecycle.Confirm(1,ctx,out var reason)&&reason==SourceHostPlacementLifecycle.ConfirmFailureReason.None,"commit failed");
   Need(rows.Select(r=>r.Operation).SequenceEqual(new[]{"PlayerReserve","PlayerPlacementRequest","PlayerPlacementOutcome","PlayerFinalize"}),"ledger order");
   var request=rows[1];var outcome=rows[2];Need(request.PrefabName=="FixtureTower"&&request.Origin.x==10&&request.Origin.y==20&&request.TransactionId==1,"request fields");
   Need(outcome.Actor==actor&&outcome.HasActorOwner&&outcome.CommittedCount==1&&outcome.ExpectedCount==1,"outcome fields");
   Need(em.GetComponentData<FactionEconomy>(bank).Money==900&&em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current==175,"commit resources");
   int before=rows.Count;Need(!lifecycle.Confirm(1,ctx,out _)&&rows.Count==before,"duplicate accepted");
   var failed=new SourceHostPlacementLifecycle.ConfirmContext{ValidateConfirm=_=>true,TryReserveCost=owner.TryReserve,FinalizeCost=owner.TryFinalize,RollbackCost=owner.TryRollback,CommitPlacement=_=>new SourceHostPlacementCommit.CommitOutcome(null,0,1)};
   Need(!lifecycle.Confirm(2,failed,out _)&&em.GetComponentData<FactionEconomy>(bank).Money==900&&em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current==175,"rollback failed");
   Need(rows.Last().Operation=="PlayerRollback"&&rows[rows.Count-2].ExpectedCount==1&&rows[rows.Count-2].CommittedCount==0,"rollback observation");
   var partial=new SourceHostPlacementLifecycle.ConfirmContext{ValidateConfirm=_=>true,TryReserveCost=owner.TryReserve,FinalizeCost=owner.TryFinalize,RollbackCost=owner.TryRollback,CommitPlacement=_=>new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity{CombatEntity=actor},1,2)};
   Need(!lifecycle.Confirm(3,partial,out var partialReason)&&partialReason==SourceHostPlacementLifecycle.ConfirmFailureReason.RegistrationFailed,"partial commit result");
   Need(rows.Last().Operation=="PlayerFinalize"&&rows[rows.Count-2].ExpectedCount==2&&rows[rows.Count-2].CommittedCount==1,"partial commit observation");
   Need(em.GetComponentData<FactionEconomy>(bank).Money==800&&em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current==150,"partial commit settlement");
   Need(SkirmishConstructionLedger.Faults==faults,"observer fault");
  } finally {Need(SkirmishConstructionLedger.CloseOwned(sink),"observer ownership");}
  int observedRows=rows.Count;
  var unobserved=new SourceHostPlacementLifecycle.ConfirmContext{ValidateConfirm=_=>true,TryReserveCost=owner.TryReserve,FinalizeCost=owner.TryFinalize,RollbackCost=owner.TryRollback,CommitPlacement=_=>new SourceHostPlacementCommit.CommitOutcome(new RuntimeBuildingEntity{CombatEntity=actor},1,1)};
  Need(lifecycle.Confirm(4,unobserved,out _)&&rows.Count==observedRows&&SkirmishConstructionLedger.Faults==faults,"observer-off behavior");
  Need(em.GetComponentData<FactionEconomy>(bank).Money==700&&em.GetComponentData<FactionTacticalMaterialsComponent>(bank).Current==125,"observer-off resources");
  Console.WriteLine("[S003PlayerConstructionLedgerControl] result=Passed observed_commit=1 unobserved_commit=1 partial_settled=1 rollback=1 duplicate_rejected=1 fixtureOnly=1 readiness=0");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
