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
