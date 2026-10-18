#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using Game.Configs;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class VehicleFieldRepairNativeTests
    {
        private sealed class Fixture
        {
            public EntityManager Em;public Entity Session,Actor,Facility,Bank;
            public Fixture(EntityManager em,Entity session)
            {
                Em=em;Session=session;var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                if(!em.HasComponent<SkirmishEconomyStockComponent>(session))em.AddComponentData(session,new SkirmishEconomyStockComponent{FactionId=1});
                if(!em.HasComponent<SkirmishCapacityComponent>(session))em.AddComponentData(session,new SkirmishCapacityComponent{SupplyLive=18});
                var definition=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session).Definition;
                Assert.IsTrue(definition.MapLayout.TryGetPad(1,SkirmishLegalPadKind.Service,out var pad));
                Assert.IsTrue(definition.MapLayout.TryProjectLocalToMap(pad.CenterX,pad.CenterZ,out float x,out float z));
                Actor=em.CreateEntity();em.AddComponent<SkirmishSharedActorTag>(Actor);
                em.AddComponentData(Actor,new SkirmishAttemptOwnedComponent{SessionId=attempt,StableObjectId="repair.own.tank",FactionId=1});
                em.AddComponentData(Actor,new Faction{Id=1});em.AddComponentData(Actor,new UnitHealth{Current=100,Max=200});
                em.AddComponentData(Actor,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Tank,Category=SkirmishPopulationCategory.Ground});
                em.AddComponentData(Actor,new UnitMovementBehavior{UsesVehicleMotion=1});
                em.AddComponentData(Actor,LocalTransform.FromPosition(new float3(x,0,z)));
                Facility=em.CreateEntity();em.AddComponentData(Facility,new SkirmishAttemptOwnedComponent{SessionId=attempt,StableObjectId="repair.ground.facility",FactionId=1,IsStructure=1});
                em.AddComponent<SkirmishSharedActorTag>(Facility);
                em.AddComponentData(Facility,new Faction{Id=1});em.AddComponentData(Facility,new UnitHealth{Current=800,Max=800});
                em.AddComponentData(Facility,LocalTransform.FromPosition(new float3(x,0,z)));
                em.AddComponentData(Facility,new SkirmishStructureIdentityComponent{StructureId=new FixedString64Bytes(SkirmishStructureIds.GroundStaging)});
                Bank=em.CreateEntity();em.AddComponentData(Bank,new FactionEconomy{FactionId=1});
                em.AddComponentData(Bank,new FactionTacticalMaterialsComponent{FactionId=1,Current=100,Capacity=200,Version=1});
            }
            public void Start()
            {Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(Em,Session,Actor,1,VehicleRepairAction.Start,out var error),error);Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(Em,Actor,out error),error);}
            public void Time(float seconds,bool paused=false)
            {var clock=Em.GetComponentData<SkirmishObjectiveClockComponent>(Session);clock.ElapsedSeconds=seconds;clock.Paused=(byte)(paused?1:0);Em.SetComponentData(Session,clock);Assert.IsTrue(VehicleFieldRepairService.Tick(Em,Actor,out string error),error);}
            public VehicleRepairStateComponent State=>Em.GetComponentData<VehicleRepairStateComponent>(Actor);
            public int Money=>Em.GetComponentData<FactionTacticalMaterialsComponent>(Bank).Current;
            public int Health=>Em.GetComponentData<UnitHealth>(Actor).Current;
        }
        static void With(Action<Fixture> test)=>SkirmishMissionLogicalBindingNativeTestFixture.WithActiveFrontline((em,session)=>test(new Fixture(em,session)));
        [Test] public void PaidRepairHealsOnlyAtCompletionAndCannotDoubleCharge()
        {With(f=>{f.Start();Assert.AreEqual(80,f.Money);Assert.AreEqual(100,f.Health);Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Start,out _));f.Time(19);Assert.AreEqual(100,f.Health);f.Time(20);Assert.AreEqual(150,f.Health);f.Time(30);Assert.AreEqual(150,f.Health);Assert.AreEqual(80,f.Money);Assert.AreEqual(VehicleRepairPhase.Completed,f.State.Phase);});}
        [Test] public void CancelBeforeWorkRefundsAllAndAfterWorkRefundsFloorThreeQuarters()
        {With(f=>{f.Start();Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Cancel,out _));Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));Assert.AreEqual(100,f.Money);f.Start();f.Time(1);f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);f.Time(2);Assert.AreEqual(95,f.Money);Assert.AreEqual(100,f.Health);Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);f.Time(3);Assert.AreEqual(95,f.Money);});}
        [Test] public void IncomingDamageCancelsWithoutHealAndDeathLosesReservation()
        {With(f=>{f.Start();f.Time(1);f.Em.SetComponentData(f.Actor,new UnitHealth{Current=99,Max=200});f.Time(2);Assert.AreEqual(95,f.Money);Assert.AreEqual(99,f.Health);f.Start();f.Em.SetComponentData(f.Actor,new UnitHealth{Current=0,Max=200});f.Time(3);Assert.AreEqual(75,f.Money);Assert.AreEqual(VehicleRepairPhase.Destroyed,f.State.Phase);Assert.AreEqual(0,f.Health);});}
        [Test] public void FacilityLossCancelsAndPauseFreezesProgress()
        {With(f=>{f.Start();f.Time(0,true);Assert.AreEqual(f.State.DurationTicks,f.State.RemainingTicks);f.Time(1);f.Em.DestroyEntity(f.Facility);f.Time(2);Assert.AreEqual(95,f.Money);Assert.AreEqual(100,f.Health);});}
        [Test] public void HiddenEnemyDoesNotChangeQuoteButVisibleArmedEnemyCancels()
        {With(f=>{var enemy=f.Em.CreateEntity();var attempt=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session).SessionId;f.Em.AddComponentData(enemy,new SkirmishAttemptOwnedComponent{SessionId=attempt,FactionId=2,StableObjectId="repair.enemy"});f.Em.AddComponentData(enemy,new Faction{Id=2});f.Em.AddComponentData(enemy,new UnitHealth{Current=100,Max=100});f.Em.AddComponentData(enemy,new UnitAttack{Damage=1});f.Em.AddComponentData(enemy,f.Em.GetComponentData<LocalTransform>(f.Actor));Assert.IsTrue(VehicleFieldRepairService.TryQuote(f.Em,f.Session,f.Actor,1,out _,out _));f.Start();f.Time(1);var contacts=f.Em.AddBuffer<SkirmishContactKnowledgeElement>(enemy);contacts.Add(new SkirmishContactKnowledgeElement{ObserverFactionId=1,Sight=SkirmishContactSight.Visible});f.Time(2);Assert.AreEqual(95,f.Money);Assert.IsFalse(VehicleFieldRepairService.TryQuote(f.Em,f.Session,f.Actor,1,out _,out _));});}
        [Test] public void HealthUpgradePreservesPercentageAndAcceptedFraction()
        {With(f=>{f.Start();f.Em.SetComponentData(f.Actor,new UnitHealth{Current=150,Max=300});f.Time(20);Assert.AreEqual(225,f.Health);Assert.AreEqual(80,f.Money);});}
        [Test] public void MovingOrForeignActorsAndAmbiguousBanksNeverSpend()
        {With(f=>{f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);Assert.IsFalse(VehicleFieldRepairService.TryQuote(f.Em,f.Session,f.Actor,1,out _,out _));f.Em.RemoveComponent<ManualMoveOrderTag>(f.Actor);Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,2,VehicleRepairAction.Start,out _));var bank=f.Em.Instantiate(f.Bank);Assert.IsFalse(VehicleFieldRepairService.TryQuote(f.Em,f.Session,f.Actor,1,out _,out _));Assert.AreEqual(100,f.Money);Assert.AreEqual(100,f.Health);});}
        [Test] public void RefundCapacityFailureRetainsDebtAndCannotStartAnotherRepair()
        {With(f=>{f.Start();f.Time(1);var bank=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);bank.Current=bank.Capacity;f.Em.SetComponentData(f.Bank,bank);f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);Assert.IsFalse(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));Assert.AreEqual(VehicleRepairPhase.AccountingFailure,f.State.Phase);Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Start,out _));bank.Current-=15;f.Em.SetComponentData(f.Bank,bank);Assert.IsTrue(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));Assert.AreEqual(15,f.State.RefundedMaterials);Assert.AreEqual(bank.Capacity,f.Money);});}
        [Test] public void NativeDamageBoundaryCancelsEvenIfHealthRestoredBeforeNextTick()
        {With(f=>{f.Start();f.Time(1);Game.Runtime.Combat.CombatDamageObservationUtility.Append(f.Em,Entity.Null,Entity.Null,f.Actor,
            default,100,99,200,1,float3.zero,float3.zero);Assert.AreEqual(1,f.State.IncomingDamage);f.Time(2);Assert.AreEqual(95,f.Money);Assert.AreEqual(100,f.Health);Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);});}
        [Test] public void NativeMoveBoundaryCancelsEvenIfOrderClearedBeforeNextTick()
        {With(f=>{f.Start();f.Time(1);var orders=new UnitMoveOrderSystem();orders.IssueTargetOnlyMoveCommand(f.Em,f.Actor,new int2(1,1));
            orders.ClearMovementOrderComponents(f.Em,f.Actor);Assert.AreEqual(1,f.State.NewMovement);f.Time(2);Assert.AreEqual(95,f.Money);Assert.AreEqual(100,f.Health);Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);});}
        [Test] public void ProratedNativeRepairNeverGrantsFuelOrChangesCombatSupply()
        {With(f=>{f.Em.SetComponentData(f.Actor,new UnitHealth{Current=199,Max=200});
            var stock=f.Em.GetComponentData<SkirmishEconomyStockComponent>(f.Session);stock.Fuel=17;stock.Oil=23;f.Em.SetComponentData(f.Session,stock);
            f.Em.AddComponentData(f.Actor,new UnitFuelConsumption{GroundFuelPerCell=2,AirFuelPerCell=3});
            var capacity=f.Em.GetComponentData<SkirmishCapacityComponent>(f.Session);f.Start();Assert.AreEqual(99,f.Money);f.Time(.4f);
            Assert.AreEqual(200,f.Health);Assert.AreEqual(17,f.Em.GetComponentData<SkirmishEconomyStockComponent>(f.Session).Fuel);
            Assert.AreEqual(23,f.Em.GetComponentData<SkirmishEconomyStockComponent>(f.Session).Oil);
            Assert.AreEqual(2,f.Em.GetComponentData<UnitFuelConsumption>(f.Actor).GroundFuelPerCell);
            Assert.AreEqual(capacity.SupplyLive,f.Em.GetComponentData<SkirmishCapacityComponent>(f.Session).SupplyLive);});}
        [Test] public void MultipleNormalFacilitiesChooseNearestThenStableIdWithoutMovingPad()
        {With(f=>{var farther=f.Em.Instantiate(f.Facility);var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(farther);own.StableObjectId="a.farther";f.Em.SetComponentData(farther,own);
            var position=f.Em.GetComponentData<LocalTransform>(f.Facility).Position;f.Em.SetComponentData(farther,LocalTransform.FromPosition(position+new float3(50,0,0)));
            f.Start();Assert.AreEqual(f.Facility,f.State.ServiceFacility);f.Time(1);Assert.AreEqual(VehicleRepairPhase.Working,f.State.Phase);
            Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Cancel,out _));Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));
            var tie=f.Em.Instantiate(f.Facility);own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(tie);own.StableObjectId="a.tie";f.Em.SetComponentData(tie,own);
            f.Start();Assert.AreEqual(tie,f.State.ServiceFacility);Assert.AreEqual(position,f.State.PadPosition);});}
        [Test] public void ActiveRepairKeepsBoundFacilityAndRejectsMovedOrSameIdReplacement()
        {With(f=>{f.Start();f.Time(1);var closer=f.Em.Instantiate(f.Facility);var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(closer);own.StableObjectId="a.new.closer";f.Em.SetComponentData(closer,own);
            f.Time(2);Assert.AreEqual(f.Facility,f.State.ServiceFacility);Assert.AreEqual(VehicleRepairPhase.Working,f.State.Phase);
            var position=f.Em.GetComponentData<LocalTransform>(f.Facility).Position;f.Em.SetComponentData(f.Facility,LocalTransform.FromPosition(position+new float3(1,0,0)));
            f.Time(3);Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);Assert.AreEqual(95,f.Money);
            f.Em.DestroyEntity(f.Facility);f.Start();var replacement=f.Em.Instantiate(closer);f.Em.DestroyEntity(closer);
            f.Time(4);Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);Assert.AreEqual(90,f.Money);});}
        [Test] public void FirstTickInterruptionAfterElapsedWorkUsesPartialRefund()
        {With(f=>{f.Start();f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);f.Time(.5f);Assert.AreEqual(95,f.Money);
            Assert.AreEqual(15,f.State.RefundedMaterials);Assert.AreEqual(100,f.Health);});}
        [Test] public void StaleReceiptCannotRefundThroughExplicitCancelOrAccountingRetry()
        {With(f=>{f.Start();var state=f.State;state.ActorStableId="foreign.receipt";f.Em.SetComponentData(f.Actor,state);
            Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Cancel,out _));
            f.Em.AddComponentData(f.Actor,new VehicleRepairRequestComponent{Session=f.Session,Faction=1,Action=VehicleRepairAction.Cancel,RequestId=state.RequestId+1});
            Assert.IsFalse(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));Assert.AreEqual(80,f.Money);
            state.Phase=VehicleRepairPhase.AccountingFailure;state.RefundDue=15;f.Em.SetComponentData(f.Actor,state);
            Assert.IsFalse(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));Assert.AreEqual(80,f.Money);Assert.AreEqual(0,f.State.RefundedMaterials);});}
        [Test] public void QuotedStartRejectsShownOrQueuedPriceDriftWithoutSpending()
        {With(f=>{Assert.IsTrue(VehicleFieldRepairService.TryQuote(f.Em,f.Session,f.Actor,1,out var shown,out _));
            f.Em.SetComponentData(f.Actor,new UnitHealth{Current=199,Max=200});
            Assert.IsFalse(VehicleFieldRepairService.TryEnqueueQuoted(f.Em,f.Session,f.Actor,1,in shown,out _));Assert.AreEqual(100,f.Money);
            f.Em.SetComponentData(f.Actor,new UnitHealth{Current=100,Max=200});
            Assert.IsTrue(VehicleFieldRepairService.TryEnqueueQuoted(f.Em,f.Session,f.Actor,1,in shown,out _));
            f.Em.SetComponentData(f.Actor,new UnitHealth{Current=199,Max=200});Assert.IsFalse(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));
            Assert.AreEqual(100,f.Money);Assert.AreEqual(199,f.Health);Assert.IsFalse(f.Em.HasComponent<VehicleRepairStateComponent>(f.Actor));
            Assert.IsTrue(VehicleFieldRepairService.TryQuote(f.Em,f.Session,f.Actor,1,out shown,out _));
            Assert.IsTrue(VehicleFieldRepairService.TryEnqueueQuoted(f.Em,f.Session,f.Actor,1,in shown,out _));
            Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));Assert.AreEqual(99,f.Money);Assert.AreEqual(199,f.Health);});}
        [Test] public void ActualSystemSettlesDisabledPaidActorRatherThanStrandingReservation()
        {With(f=>{f.Start();var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(f.Session);clock.ElapsedSeconds=1;f.Em.SetComponentData(f.Session,clock);
            f.Em.AddComponent<Disabled>(f.Actor);f.Em.World.GetOrCreateSystemManaged<VehicleFieldRepairSystem>().Update();
            Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);Assert.AreEqual(15,f.State.RefundedMaterials);Assert.AreEqual(95,f.Money);Assert.AreEqual(100,f.Health);
            // A second real update reuses the system-owned queries and cannot
            // double-refund, heal, or encounter a query disposed by our code.
            f.Em.World.GetOrCreateSystemManaged<VehicleFieldRepairSystem>().Update();
            Assert.AreEqual(VehicleRepairPhase.Cancelled,f.State.Phase);Assert.AreEqual(15,f.State.RefundedMaterials);Assert.AreEqual(95,f.Money);Assert.AreEqual(100,f.Health);});}

        private sealed class RepairScopeFixture:IDisposable
        {
            internal readonly Action<SkirmishProductionService.ProductionLedgerObservation> Production=_=>{};
            internal readonly Action<SkirmishResearchService.ResearchLedgerObservation> Research=_=>{};
            internal readonly Action<SkirmishConstructionLedger.Observation> Construction=_=>{};
            internal readonly Action<VehicleFieldRepairService.RepairLedgerObservation> Repair=_=>{};
            internal readonly System.Collections.Generic.List<string> Closed=new System.Collections.Generic.List<string>();
            internal int Restores;
            internal RepairScopeFixture()
            {
                Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);
                Assert.IsNull(SkirmishConstructionLedger.Observer);Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);
            }
            internal void Earlier()=>Game.Editor.SkirmishS002AriaRunHarness.StartPaidObserverGroup(true,
                ()=>SkirmishProductionService.ProductionLedgerObserver=Production,
                ()=>SkirmishResearchService.ResearchLedgerObserver=Research,()=>SkirmishConstructionLedger.OpenOwned(Construction),
                CloseConstruction,CloseResearch,CloseProduction);
            internal void CloseProduction(){Game.Editor.SkirmishS002AriaRunHarness.ReleasePaidProductionObserver(Production);Closed.Add("production");}
            internal void CloseResearch(){Game.Editor.SkirmishS002AriaRunHarness.ReleasePaidResearchObserver(Research);Closed.Add("research");}
            internal void CloseConstruction(){SkirmishConstructionLedger.CloseOwned(Construction);Closed.Add("construction");}
            internal void CloseRepair(){VehicleFieldRepairService.CloseRepairLedgerOwned(Repair);Closed.Add("repair");}
            internal void CloseEarlier()=>Game.Editor.SkirmishS002AriaRunHarness.ClosePaidObserverGroup(
                CloseConstruction,CloseResearch,CloseProduction,()=>Restores++);
            public void Dispose()
            {VehicleFieldRepairService.CloseRepairLedgerOwned(Repair);SkirmishConstructionLedger.CloseOwned(Construction);
                Game.Editor.SkirmishS002AriaRunHarness.ReleasePaidResearchObserver(Research);Game.Editor.SkirmishS002AriaRunHarness.ReleasePaidProductionObserver(Production);}
        }
        private static void WithRepairRows(Action<Fixture,System.Collections.Generic.List<VehicleFieldRepairService.RepairLedgerObservation>> test)
        {
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);
            var rows=new System.Collections.Generic.List<VehicleFieldRepairService.RepairLedgerObservation>();
            Action<VehicleFieldRepairService.RepairLedgerObservation> sink=rows.Add;long faults=VehicleFieldRepairService.RepairLedgerFaults;
            VehicleFieldRepairService.OpenRepairLedgerOwned(sink);
            try{With(f=>test(f,rows));Assert.AreEqual(faults,VehicleFieldRepairService.RepairLedgerFaults);}
            finally{Assert.IsTrue(VehicleFieldRepairService.CloseRepairLedgerOwned(sink));}
        }
        [Test] public void RepairLedgerDefaultOffRetainsNativeOutcome()
        {
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);long faults=VehicleFieldRepairService.RepairLedgerFaults;
            With(f=>{f.Start();f.Time(20);Assert.AreEqual(80,f.Money);Assert.AreEqual(150,f.Health);});
            Assert.AreEqual(faults,VehicleFieldRepairService.RepairLedgerFaults);
            bool earlier=false,repair=false;
            Game.Editor.SkirmishS002AriaRunHarness.StartRepairObserverScope(false,()=>earlier=true,()=>repair=true,()=>Assert.Fail(),()=>Assert.Fail());
            Assert.IsTrue(earlier);Assert.IsFalse(repair);Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);
        }
        private static string RepairDtoFirstDifference(Newtonsoft.Json.Linq.JObject expected,Newtonsoft.Json.Linq.JObject actual)
        {
            foreach(var token in expected.Descendants())
            {
                if(!(token is Newtonsoft.Json.Linq.JValue value))continue;
                var other=actual.SelectToken(value.Path);
                if(Newtonsoft.Json.Linq.JToken.DeepEquals(value,other))continue;
                string left=value.ToString(Newtonsoft.Json.Formatting.None),right=other?.ToString(Newtonsoft.Json.Formatting.None)??"<missing>";
                return value.Path+" expected="+value.Type+"/"+(value.Value?.GetType().FullName??"null")+":"+left.Substring(0,Math.Min(160,left.Length))+
                    " actual="+(other?.Type.ToString()??"missing")+"/"+((other as Newtonsoft.Json.Linq.JValue)?.Value?.GetType().FullName??"null")+":"+right.Substring(0,Math.Min(160,right.Length));
            }
            return "Repair DTO container mismatch";
        }
        private static void AssertRepairDtoFiniteRoundtrip()
        {
            var state=new VehicleRepairStateComponent {LastClockSeconds=.25,FractionalTicks=.125,
                AcceptedPosition=new float3(.1f,-.3f,1344.7f),PadPosition=new float3(-.1f,.3f,-1344.7f),
                ServiceFacilityPosition=new float3(.3f,-.1f,1344.7f)};
            var point=new VehicleFieldRepairService.RepairLedgerPoint {HasState=true,State=state,
                HasBank=true,Bank=new FactionTacticalMaterialsComponent {Current=100,Capacity=200,Version=1}};
            var json=Game.Editor.SkirmishS002AriaRunHarness.RepairPointJson(point);
            Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(json,Newtonsoft.Json.Linq.JObject.Parse(
                Game.Editor.SkirmishS002AriaRunHarness.SerializeProductionNativeRecord(json))));
            Assert.AreEqual((double)state.AcceptedPosition.x,(double)json["state"]["accepted_position"][0]);
            Assert.AreEqual(.25,(double)json["state"]["last_clock_seconds"]);
            Assert.AreEqual(100,(int)json["bank"]["Current"]);
            point.HasState=false;
            Assert.AreEqual(Newtonsoft.Json.Linq.JTokenType.Null,Game.Editor.SkirmishS002AriaRunHarness.RepairPointJson(point)["state"].Type);
            foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            {
                var bad=state;bad.LastClockSeconds=invalid;
                Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.RepairStateJson(bad));
                bad=state;bad.FractionalTicks=invalid;
                Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.RepairStateJson(bad));
                for(int position=0;position<3;position++)
                {
                    bad=state;var value=new float3((float)invalid,.1f,-.3f);
                    if(position==0)bad.AcceptedPosition=value;else if(position==1)bad.PadPosition=value;else bad.ServiceFacilityPosition=value;
                    Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.RepairStateJson(bad));
                }
            }
        }
        [Test] public void RepairLedgerActualDebitAndCompletionRoundtrip()
        {
            AssertRepairDtoFiniteRoundtrip();
            WithRepairRows((f,rows)=>{
                f.Start();Assert.AreEqual(1,rows.Count);var debit=rows[0];
                Assert.AreEqual("DebitCommitted",debit.Operation);Assert.AreEqual(f.Session,debit.Session);
                Assert.AreEqual(f.Actor,debit.Before.Actor);Assert.AreEqual(f.Bank,debit.Before.BankEntity);
                Assert.IsFalse(debit.Before.HasState);Assert.AreEqual(100,debit.Before.Bank.Current);Assert.AreEqual(80,debit.After.Bank.Current);
                Assert.AreEqual(20,debit.After.Bank.LifetimeRepairSpent);Assert.AreEqual(20,debit.After.Bank.LifetimeSpent);
                Assert.AreEqual(debit.Before.Bank.Version+1,debit.After.Bank.Version);Assert.AreEqual(20,debit.PolicyState.PaidMaterials);
                Assert.AreEqual(f.State.ActorStableId,debit.After.Owner.StableObjectId);
                f.Time(1);Assert.AreEqual(1,rows.Count,"Positive progress is not a paid event");
                f.Time(20);Assert.AreEqual(2,rows.Count);Assert.AreEqual("Completed",rows[1].Operation);
                Assert.AreEqual(VehicleRepairPhase.Completed,rows[1].After.State.Phase);Assert.AreEqual(150,rows[1].After.Health.Current);
                Assert.AreEqual(rows[1].Before.Bank.Current,rows[1].After.Bank.Current);
                Assert.AreEqual(rows[1].Before.Bank.Version,rows[1].After.Bank.Version);
                f.Time(30);Assert.AreEqual(2,rows.Count);
                var json=Game.Editor.SkirmishS002AriaRunHarness.RepairPointJson(debit.After);
                string compact=Game.Editor.SkirmishS002AriaRunHarness.SerializeProductionNativeRecord(json);
                var parsed=Newtonsoft.Json.Linq.JObject.Parse(compact);
                Assert.IsTrue(Newtonsoft.Json.Linq.JToken.DeepEquals(json,parsed),RepairDtoFirstDifference(json,parsed));
                Assert.AreEqual(20,(int)json["bank"]["LifetimeRepairSpent"]);
            });
        }
        [Test] public void RepairLedgerCancelRecordsActualFullAndPartialRefunds()
        {
            WithRepairRows((f,rows)=>{
                f.Start();Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Cancel,out _));
                Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));Assert.AreEqual("RefundCommitted",rows[1].Operation);
                Assert.AreEqual(20,rows[1].PolicyState.RefundDue);Assert.AreEqual(100,rows[1].After.Bank.Current);
                Assert.AreEqual(0,rows[1].After.Bank.LifetimeRepairSpent);Assert.AreEqual(20,rows[1].After.State.RefundedMaterials);
                f.Start();f.Time(1);f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);f.Time(2);
                Assert.AreEqual(4,rows.Count);var refund=rows[3];
                Assert.AreEqual(15,refund.PolicyState.RefundDue);Assert.AreEqual(15,refund.After.Bank.Current-refund.Before.Bank.Current);
                Assert.AreEqual(5,refund.After.Bank.LifetimeRepairSpent);Assert.AreEqual(95,f.Money);f.Time(3);Assert.AreEqual(4,rows.Count);
            });
        }
        [Test] public void RepairLedgerDeferredDebtThenRetryIsNotReward()
        {
            WithRepairRows((f,rows)=>{
                f.Start();f.Time(1);var bank=f.Em.GetComponentData<FactionTacticalMaterialsComponent>(f.Bank);
                bank.Current=bank.Capacity;f.Em.SetComponentData(f.Bank,bank); // Explicit fixture-only external bank interleaving.
                f.Em.AddComponent<ManualMoveOrderTag>(f.Actor);Assert.IsFalse(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));
                Assert.AreEqual(2,rows.Count);Assert.AreEqual("RefundDeferred",rows[1].Operation);
                Assert.AreEqual(VehicleRepairPhase.AccountingFailure,rows[1].After.State.Phase);
                Assert.AreEqual(rows[1].Before.Bank.Current,rows[1].After.Bank.Current);Assert.AreEqual(15,rows[1].PolicyState.RefundDue);
                for(int i=0;i<10;i++)Assert.IsFalse(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));
                Assert.AreEqual(2,rows.Count,"Unchanged failed debt retries must not generate frame-cadence rows");
                bank.Current-=15;f.Em.SetComponentData(f.Bank,bank);
                Assert.IsTrue(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));Assert.AreEqual(3,rows.Count);
                Assert.AreEqual("RefundCommitted",rows[2].Operation);Assert.AreEqual(15,rows[2].After.State.RefundedMaterials);
                Assert.AreEqual(rows[2].Before.Bank.LifetimeRewarded,rows[2].After.Bank.LifetimeRewarded);
                Assert.IsTrue(VehicleFieldRepairService.Tick(f.Em,f.Actor,out _));Assert.AreEqual(3,rows.Count);
            });
        }
        [Test] public void RepairLedgerDeathHasNoRefundAndRejectedReceiptEmitsNothing()
        {
            WithRepairRows((f,rows)=>{
                f.Start();f.Em.SetComponentData(f.Actor,new UnitHealth {Current=0,Max=200});f.Time(1);
                Assert.AreEqual(2,rows.Count);Assert.AreEqual("Destroyed",rows[1].Operation);Assert.AreEqual(0,rows[1].PolicyState.RefundDue);
                Assert.AreEqual(80,rows[1].After.Bank.Current);Assert.AreEqual(VehicleRepairPhase.Destroyed,rows[1].After.State.Phase);
                Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,2,VehicleRepairAction.Start,out _));Assert.AreEqual(2,rows.Count);
            });
            WithRepairRows((f,rows)=>{
                f.Start();var state=f.State;state.ActorStableId="foreign";f.Em.SetComponentData(f.Actor,state);
                Assert.IsFalse(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Cancel,out _));Assert.AreEqual(1,rows.Count);
            });
        }
        [Test] public void RepairLedgerOrdinarySinkFailurePreservesCommittedGameplay()
        {
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);
            Action<VehicleFieldRepairService.RepairLedgerObservation> sink=_=>throw new InvalidOperationException("fixture-observation-fault");
            long before=VehicleFieldRepairService.RepairLedgerFaults;VehicleFieldRepairService.OpenRepairLedgerOwned(sink);
            try{With(f=>{f.Start();Assert.AreEqual(80,f.Money);Assert.AreEqual(VehicleRepairPhase.Working,f.State.Phase);
                Assert.IsTrue(VehicleFieldRepairService.TryEnqueue(f.Em,f.Session,f.Actor,1,VehicleRepairAction.Cancel,out _));
                Assert.IsTrue(VehicleFieldRepairService.ProcessRequest(f.Em,f.Actor,out _));Assert.AreEqual(100,f.Money);});}
            finally{Assert.IsTrue(VehicleFieldRepairService.CloseRepairLedgerOwned(sink));}
            Assert.AreEqual(before+2,VehicleFieldRepairService.RepairLedgerFaults);
        }
        [Test] public void RepairLedgerForeignGuardPrecedesReservation()
        {
            using var f=new RepairScopeFixture();int reservations=0;VehicleFieldRepairService.OpenRepairLedgerOwned(f.Repair);
            Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.GuardRepairObserverReservation(true,false,()=>reservations++));
            Assert.AreEqual(0,reservations);Assert.AreSame(f.Repair,VehicleFieldRepairService.RepairLedgerObserver);
            Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.GuardRepairObserverReservation(true,true,()=>reservations++));
            Assert.AreEqual(0,reservations);
        }
        [Test] public void RepairLedgerStartupFaultClosesRealEarlierRegistries()
        {
            using var f=new RepairScopeFixture();
            Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.StartRepairObserverScope(true,
                f.Earlier,()=>{VehicleFieldRepairService.OpenRepairLedgerOwned(f.Repair);throw new InvalidOperationException("after-repair-install");},f.CloseRepair,f.CloseEarlier));
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);
            Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);Assert.IsNull(SkirmishConstructionLedger.Observer);
            CollectionAssert.AreEqual(new[]{"repair","construction","research","production"},f.Closed);Assert.AreEqual(1,f.Restores);
        }
        [Test] public void RepairLedgerEarlierStartupFaultStillClosesOnlyOwned()
        {
            using var f=new RepairScopeFixture();
            Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.StartRepairObserverScope(true,
                ()=>{f.Earlier();throw new InvalidOperationException("after-earlier-install");},
                ()=>VehicleFieldRepairService.OpenRepairLedgerOwned(f.Repair),f.CloseRepair,f.CloseEarlier));
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);
            Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);Assert.IsNull(SkirmishConstructionLedger.Observer);Assert.AreEqual(1,f.Restores);
        }
        [Test] public void RepairLedgerForeignReplacementSurvivesOwnedClose()
        {
            using var f=new RepairScopeFixture();Action<VehicleFieldRepairService.RepairLedgerObservation> foreign=_=>{};
            Game.Editor.SkirmishS002AriaRunHarness.StartRepairObserverScope(true,f.Earlier,()=>VehicleFieldRepairService.OpenRepairLedgerOwned(f.Repair),f.CloseRepair,f.CloseEarlier);
            Assert.IsTrue(VehicleFieldRepairService.CloseRepairLedgerOwned(f.Repair));VehicleFieldRepairService.OpenRepairLedgerOwned(foreign);
            try{Game.Editor.SkirmishS002AriaRunHarness.CloseRepairObserverScope(f.CloseRepair,f.CloseEarlier);
                Assert.AreSame(foreign,VehicleFieldRepairService.RepairLedgerObserver);Assert.AreEqual(1,f.Restores);
                Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);Assert.IsNull(SkirmishConstructionLedger.Observer);}
            finally{Assert.IsTrue(VehicleFieldRepairService.CloseRepairLedgerOwned(foreign));}
        }
        [Test] public void RepairLedgerCloseFaultStillRunsRealRegistryAndGameplayCleanup()
        {
            using var f=new RepairScopeFixture();
            Game.Editor.SkirmishS002AriaRunHarness.StartRepairObserverScope(true,f.Earlier,()=>VehicleFieldRepairService.OpenRepairLedgerOwned(f.Repair),f.CloseRepair,f.CloseEarlier);
            using var world=new World("repair-observer-cleanup");Entity actor=world.EntityManager.CreateEntity();
            Assert.Throws<InvalidOperationException>(()=>Game.Editor.SkirmishS002AriaRunHarness.CloseRepairObserverScope(
                ()=>{f.CloseRepair();throw new InvalidOperationException("close-repair");},
                ()=>{try{f.CloseEarlier();}finally{world.EntityManager.DestroyEntity(actor);}}));
            Assert.IsFalse(world.EntityManager.Exists(actor));Assert.AreEqual(1,f.Restores);
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);Assert.IsNull(SkirmishProductionService.ProductionLedgerObserver);
            Assert.IsNull(SkirmishResearchService.ResearchLedgerObserver);Assert.IsNull(SkirmishConstructionLedger.Observer);
        }
        [Test] public void RepairLedgerSuccessClosesFourRegistriesInReverseOrder()
        {
            using var f=new RepairScopeFixture();
            Game.Editor.SkirmishS002AriaRunHarness.GuardRepairObserverReservation(true,false,()=>{});
            Game.Editor.SkirmishS002AriaRunHarness.StartRepairObserverScope(true,f.Earlier,()=>VehicleFieldRepairService.OpenRepairLedgerOwned(f.Repair),f.CloseRepair,f.CloseEarlier);
            Game.Editor.SkirmishS002AriaRunHarness.CloseRepairObserverScope(f.CloseRepair,f.CloseEarlier);
            CollectionAssert.AreEqual(new[]{"repair","construction","research","production"},f.Closed);Assert.AreEqual(1,f.Restores);
            Assert.IsNull(VehicleFieldRepairService.RepairLedgerObserver);
        }
        public static void RunRepairCommittedLedgerFocusedValidation()
        {
            var t=new VehicleFieldRepairNativeTests();
            t.RepairLedgerDefaultOffRetainsNativeOutcome();t.RepairLedgerActualDebitAndCompletionRoundtrip();
            t.RepairLedgerCancelRecordsActualFullAndPartialRefunds();t.RepairLedgerDeferredDebtThenRetryIsNotReward();
            t.RepairLedgerDeathHasNoRefundAndRejectedReceiptEmitsNothing();t.RepairLedgerOrdinarySinkFailurePreservesCommittedGameplay();
            t.RepairLedgerForeignGuardPrecedesReservation();t.RepairLedgerStartupFaultClosesRealEarlierRegistries();
            t.RepairLedgerEarlierStartupFaultStillClosesOnlyOwned();t.RepairLedgerForeignReplacementSurvivesOwnedClose();
            t.RepairLedgerCloseFaultStillRunsRealRegistryAndGameplayCleanup();t.RepairLedgerSuccessClosesFourRegistriesInReverseOrder();
            Debug.Log("[RepairCommittedLedgerControls] result=Passed methods=12 normalEnvelope=0 normalJournal=0 paidResourcesQualified=0");
            RunFocusedValidation();
            Debug.Log("[RepairCommittedLedgerFocusedControls] result=Passed ledgerMethods=12 legacyMethods=17 normalJournal=0 paidResourcesQualified=0");
        }

        public static void RunFocusedValidation()
        {
            var t=new VehicleFieldRepairNativeTests();t.PaidRepairHealsOnlyAtCompletionAndCannotDoubleCharge();
            t.CancelBeforeWorkRefundsAllAndAfterWorkRefundsFloorThreeQuarters();t.IncomingDamageCancelsWithoutHealAndDeathLosesReservation();
            t.FacilityLossCancelsAndPauseFreezesProgress();t.HiddenEnemyDoesNotChangeQuoteButVisibleArmedEnemyCancels();
            t.HealthUpgradePreservesPercentageAndAcceptedFraction();t.MovingOrForeignActorsAndAmbiguousBanksNeverSpend();
            t.RefundCapacityFailureRetainsDebtAndCannotStartAnotherRepair();
            t.NativeDamageBoundaryCancelsEvenIfHealthRestoredBeforeNextTick();t.NativeMoveBoundaryCancelsEvenIfOrderClearedBeforeNextTick();
            t.ProratedNativeRepairNeverGrantsFuelOrChangesCombatSupply();
            t.MultipleNormalFacilitiesChooseNearestThenStableIdWithoutMovingPad();t.ActiveRepairKeepsBoundFacilityAndRejectsMovedOrSameIdReplacement();
            t.FirstTickInterruptionAfterElapsedWorkUsesPartialRefund();t.StaleReceiptCannotRefundThroughExplicitCancelOrAccountingRetry();
            t.QuotedStartRejectsShownOrQueuedPriceDriftWithoutSpending();t.ActualSystemSettlesDisabledPaidActorRatherThanStrandingReservation();
            Debug.Log("[VehicleFieldRepairNativeTests] result=Passed tests=17 normalInputReadiness=Pending");
        }
    }
}
#endif
