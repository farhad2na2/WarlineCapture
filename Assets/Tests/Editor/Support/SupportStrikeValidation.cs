#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportStrikeValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                var t=new SupportStrikeValidation();t.ReserveApproachImpactOnce();t.HiddenTargetAbortsAndRestoresOnce();t.DestroyedDepotIsNotRefunded();
                t.StandOffRouteCanStrikeKnownAA();t.MissingKnowledgeAndRouteRejectBeforeCosts();t.InFlightReceiptSurvivesRejectedInputFlood();
                t.ExternalFlightRemovalReleasesOwnership();t.PausePreservesReservationUntilRelease();t.InfantryRejectedAndCatalogChangeAborts();t.EmbeddedAircraftHierarchyRemapsWithoutGameplay();
                Debug.Log("[SupportStrikeValidation] result=Passed tests=10");ValidationExit.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportStrikeValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        internal static SupportRequestElement Prepare(SupportValidationFixture f,out Entity target)
        {
            var em=f.Em;var old=em.GetComponentData<SupportCatalogComponent>(f.Root);old.Blob.Dispose();
            using var builder=new BlobBuilder(Allocator.Temp);ref var blob=ref builder.ConstructRoot<SupportCatalogBlob>();var entries=builder.Allocate(ref blob.Abilities,4);
            for(int i=0;i<4;i++)entries[i]=new SupportAbilityDefinition {Kind=(SupportAbilityKind)(i+1),Charges=i==0?2:1,FuelCost=i==0?1:4,
                CooldownSeconds=120,ApproachSeconds=3,Damage=250,Radius=i==0?12:0,DurationSeconds=15,DirectDamagePermille=650,AircraftSourceKey="jet-test"};
            em.SetComponentData(f.Root,new SupportCatalogComponent {Blob=builder.CreateBlobAssetReference<SupportCatalogBlob>(Allocator.Persistent),Revision=1,OwnsBlob=1});
            var policy=em.GetComponentData<SupportMissionPolicyComponent>(f.Root);policy.TestGrantMask=15;em.SetComponentData(f.Root,policy);
            em.GetBuffer<SupportAbilityStateElement>(f.Root).Add(new SupportAbilityStateElement {Kind=SupportAbilityKind.Strike,ChargesRemaining=1,Enabled=1,StateVersion=1});
            var model=em.CreateEntity(typeof(Prefab),typeof(LocalTransform));
            var aircraft=em.CreateEntity(typeof(Prefab),typeof(UnitSourcePrefabKey),typeof(UnitModelPrefabReference));
            em.SetComponentData(aircraft,new UnitSourcePrefabKey {Value="jet-test"});em.SetComponentData(aircraft,new UnitModelPrefabReference {Prefab=model});
            var registry=em.CreateEntity(typeof(UnitPrefabRegistryTag));em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry {Prefab=aircraft});
            em.AddComponentData(f.Root,new SupportAirRouteComponent {SessionToken="support-test",AttemptOrdinal=1,Version=1,Authored=1,
                Entry=new float3(-30,20,-30),Release=new float3(0,20,-30),Exit=new float3(30,20,-30),Clearance=2});
            target=f.Unit(float3.zero);em.AddComponentData(target,new UnitMovementBehavior {UsesVehicleMotion=1});
            em.AddComponentData(target,new SupportTargetEligibilityComponent {SessionToken="support-test",AttemptOrdinal=1,SourceVersion=1,CurrentlyVisible=1,HostileConfirmed=1,IsMilitaryTarget=1});
            var request=f.Request();request.Kind=SupportAbilityKind.Strike;request.TargetKind=SupportTargetKind.Entity;request.Target=target;request.TargetKnowledgeVersion=1;
            em.SetComponentData(f.Root,new SupportPreviewComponent {PreviewId=1,Request=request,Valid=1,FuelCost=4});return request;
        }
        private static Entity Flight(SupportValidationFixture f)=>f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].Effect;
        private static void AdvanceTime(SupportValidationFixture f,double seconds)
        {var session=f.Em.GetComponentData<SupportSessionComponent>(f.Root);session.SimulationSeconds=seconds;f.Em.SetComponentData(f.Root,session);}
        [Test] public void EmbeddedAircraftHierarchyRemapsWithoutGameplay()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out _);f.Store(4);
            using var registry=f.Em.CreateEntityQuery(typeof(UnitPrefabRegistryTag));
            var aircraft=f.Em.GetBuffer<UnitPrefabRegistryEntry>(registry.GetSingletonEntity())[0].Prefab;
            f.Em.RemoveComponent<UnitModelPrefabReference>(aircraft);
            f.Em.AddComponentData(aircraft,new UnitHealth {Current=100,Max=100});
            f.Em.AddComponentData(aircraft,new UnitAirMovement());f.Em.AddComponentData(aircraft,new Faction {Id=1});
            f.Em.AddComponentData(aircraft,LocalTransform.Identity);
            var child=f.Em.CreateEntity(typeof(Prefab),typeof(LocalTransform));
            f.Em.SetComponentData(child,LocalTransform.Identity);f.Em.AddComponentData(child,new Parent {Value=aircraft});
            f.Em.AddComponentData(aircraft,new UnitDetailedVisualReference {Root=child});
            var group=f.Em.AddBuffer<LinkedEntityGroup>(aircraft);group.Add(new LinkedEntityGroup {Value=aircraft});group.Add(new LinkedEntityGroup {Value=child});
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));var flight=Flight(f);
            var visual=f.Em.GetComponentData<UnitDetailedVisualReference>(flight).Root;
            Assert.AreNotEqual(child,visual);Assert.AreEqual(flight,f.Em.GetComponentData<Parent>(visual).Value);
            Assert.IsFalse(f.Em.HasComponent<UnitHealth>(flight));Assert.IsFalse(f.Em.HasComponent<UnitAirMovement>(flight));Assert.IsFalse(f.Em.HasComponent<Faction>(flight));
            SupportFlightSystem.Abort(f.Em,flight,SupportRejectionReason.TargetGone);
            Assert.IsFalse(f.Em.Exists(visual));Assert.IsTrue(f.Em.Exists(child));Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));
        }
        [Test] public void ReserveApproachImpactOnce()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);var depot=f.Store(10,3,3);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));var flight=Flight(f);
            Assert.AreEqual(10,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredFuelBarrels);Assert.AreEqual(7,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).ReservedFuelOutboundBarrels);
            Assert.AreEqual(0,SupportFuelTransactionSystem.Available(f.Em,1));Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);
            Assert.IsFalse(f.Em.HasComponent<UnitHealth>(flight));Assert.IsFalse(f.Em.HasComponent<UnitAirMovement>(flight));Assert.IsFalse(f.Em.HasComponent<Faction>(flight));
            AdvanceTime(f,2.99);SupportFlightSystem.Advance(f.Em,flight);Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);
            AdvanceTime(f,3);SupportFlightSystem.Advance(f.Em,flight);SupportFlightSystem.Advance(f.Em,flight);
            Assert.AreEqual(750,f.Em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(6,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredFuelBarrels);
            Assert.AreEqual(3,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).ReservedFuelOutboundBarrels);
            Assert.AreEqual(4,f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].SpentFuel);
            using var observations=f.Em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));var hits=f.Em.GetBuffer<CombatDamageObservationElement>(observations.GetSingletonEntity());
            Assert.AreEqual(1,hits.Length);Assert.AreEqual(250,hits[0].DamageApplied);Assert.AreEqual(CombatDamageSourceKind.SupportStrike,hits[0].SourceKind);
        }
        [Test] public void HiddenTargetAbortsAndRestoresOnce()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);var depot=f.Store(4);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));var flight=Flight(f);
            var knowledge=f.Em.GetComponentData<SupportTargetEligibilityComponent>(target);knowledge.CurrentlyVisible=0;f.Em.SetComponentData(target,knowledge);
            SupportFlightSystem.Advance(f.Em,flight);SupportFlightSystem.Abort(f.Em,flight,SupportRejectionReason.NotVisible);
            Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));Assert.AreEqual(4,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredFuelBarrels);
            var a=f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[1];Assert.AreEqual(1,a.ChargesRemaining);Assert.AreEqual(0,a.CooldownUntil);
            Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(SupportRejectionReason.NotVisible,f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].Reason);
        }
        [Test] public void DestroyedDepotIsNotRefunded()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);var lost=f.Store(2);var survivor=f.Store(2);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));f.Em.DestroyEntity(lost);AdvanceTime(f,3);
            SupportFlightSystem.Advance(f.Em,Flight(f));Assert.AreEqual(2,SupportFuelTransactionSystem.Available(f.Em,1));
            Assert.AreEqual(2,f.Em.GetComponentData<BuildingResourceStorageComponent>(survivor).StoredFuelBarrels);Assert.AreEqual(0,f.Em.GetComponentData<BuildingResourceStorageComponent>(survivor).ReservedFuelOutboundBarrels);
            Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(1,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[1].ChargesRemaining);
        }
        [Test] public void StandOffRouteCanStrikeKnownAA()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);f.Store(4);
            f.Em.AddComponentData(target,new AirMissileLauncherComponent {BaseDetectionRange=20,MaxDetectionRange=20});
            Assert.AreEqual(SupportRejectionReason.None,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));
            var route=f.Em.GetComponentData<SupportAirRouteComponent>(f.Root);route.Release=new float3(0,20,0);route.Version++;f.Em.SetComponentData(f.Root,route);
            Assert.AreEqual(SupportRejectionReason.NoSafeAirRoute,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));Assert.AreEqual(1,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[1].ChargesRemaining);
        }
        [Test] public void MissingKnowledgeAndRouteRejectBeforeCosts()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);f.Store(4);
            var knowledge=f.Em.GetComponentData<SupportTargetEligibilityComponent>(target);f.Em.RemoveComponent<SupportTargetEligibilityComponent>(target);
            Assert.AreEqual(SupportRejectionReason.NotVisible,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));
            f.Em.AddComponentData(target,knowledge);f.Em.RemoveComponent<SupportAirRouteComponent>(f.Root);
            Assert.AreEqual(SupportRejectionReason.NoSafeAirRoute,SupportTargetValidationUtilitySystemHelper.Validate(f.Em,f.Root,request));Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));
        }
        [Test] public void InFlightReceiptSurvivesRejectedInputFlood()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);f.Store(4);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));var flight=Flight(f);
            for(uint i=2;i<100;i++){request.RequestId=i;SupportAbilityRequestSystem.Process(f.Em,f.Root,request);}
            var receipts=f.Em.GetBuffer<SupportReceiptElement>(f.Root);Assert.AreEqual(64,receipts.Length);Assert.AreEqual(flight,receipts[0].Effect);
            AdvanceTime(f,3);SupportFlightSystem.Advance(f.Em,flight);Assert.AreEqual(750,f.Em.GetComponentData<UnitHealth>(target).Current);
            Assert.AreEqual(SupportExecutionPhase.Resolved,f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].Phase);
        }
        [Test] public void ExternalFlightRemovalReleasesOwnership()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);f.Store(4);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));var flight=Flight(f);f.Em.DestroyEntity(flight);
            var system=f.World.CreateSystem<SupportFlightSystem>();system.Update(f.World.Unmanaged);system.Update(f.World.Unmanaged);
            Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));Assert.AreEqual(1,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[1].ChargesRemaining);
            Assert.AreEqual(SupportExecutionPhase.Aborted,f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].Phase);Assert.IsFalse(f.Em.Exists(flight));
            Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);
        }
        [Test] public void InfantryRejectedAndCatalogChangeAborts()
        {
            using var f=new SupportValidationFixture();var r=Prepare(f,out var target);f.Store(4);
            f.Em.SetComponentData(target,new UnitMovementBehavior {UsesVehicleMotion=0});
            Assert.AreEqual(SupportRejectionReason.InvalidTargetType,SupportAbilityRequestSystem.Process(f.Em,f.Root,r));Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));
            f.Em.SetComponentData(target,new UnitMovementBehavior {UsesVehicleMotion=1});r.RequestId=2;f.Em.SetComponentData(f.Root,new SupportPreviewComponent {PreviewId=r.PreviewId,Request=r,Valid=1,FuelCost=4});
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,r));var flight=f.Em.GetBuffer<SupportReceiptElement>(f.Root)[1].Effect;
            var catalog=f.Em.GetComponentData<SupportCatalogComponent>(f.Root);catalog.Revision++;f.Em.SetComponentData(f.Root,catalog);
            SupportFlightSystem.Advance(f.Em,flight);Assert.AreEqual(4,SupportFuelTransactionSystem.Available(f.Em,1));Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);
            Assert.AreEqual(1,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[1].ChargesRemaining);
        }
        [Test] public void PausePreservesReservationUntilRelease()
        {
            using var f=new SupportValidationFixture();var request=Prepare(f,out var target);f.Store(4);
            Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));var flight=Flight(f);
            var session=f.Em.GetComponentData<SupportSessionComponent>(f.Root);session.Active=0;session.SimulationSeconds=3;f.Em.SetComponentData(f.Root,session);
            SupportFlightSystem.Advance(f.Em,flight);Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(0,SupportFuelTransactionSystem.Available(f.Em,1));
            session.Active=1;f.Em.SetComponentData(f.Root,session);SupportFlightSystem.Advance(f.Em,flight);Assert.AreEqual(750,f.Em.GetComponentData<UnitHealth>(target).Current);
        }
    }
}
#endif
