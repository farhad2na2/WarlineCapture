#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeStrikeRecoveryTests
    {
        public static void RunFocusedValidation()
        {
            try {
                var t=new SkirmishNativeStrikeRecoveryTests();
                t.ApproachingFlightRestoresAndImpactsOnce();t.ReleasedFlightRestoresWithoutSecondPaymentOrHit();
                t.NativePauseThenTerminalReleasesReservationOnce();t.RegistryWithoutTagAndAmbiguityFailClosed();t.MismatchedPaidLedgerCannotBeSaved();
                Debug.Log("[SkirmishNativeStrikeRecoveryTests] result=Passed tests=5 nativePlayerJourneys=0");ValidationExit.Exit(0);
            } catch(Exception ex) {Debug.LogException(ex);Debug.LogError("[SkirmishNativeStrikeRecoveryTests] result=Failed");ValidationExit.Exit(1);throw;}
        }
        private static Entity Prepare(SupportValidationFixture f,bool native,out Entity target,out Entity depot,out SkirmishResolvedSetup setup)
        {
            var request=SupportStrikeValidation.Prepare(f,out target);
            var catalog=f.Em.GetComponentData<SupportCatalogComponent>(f.Root);
            using(var builder=new BlobBuilder(Allocator.Temp)) {
                ref var blob=ref builder.ConstructRoot<SupportCatalogBlob>();var entries=builder.Allocate(ref blob.Abilities,4);
                for(int i=0;i<4;i++){entries[i]=catalog.Blob.Value.Abilities[i];entries[i].ProductionReady=1;}
                catalog.Blob.Dispose();catalog.Blob=builder.CreateBlobAssetReference<SupportCatalogBlob>(Allocator.Persistent);f.Em.SetComponentData(f.Root,catalog);
            }
            var support=f.Em.GetComponentData<SupportSessionComponent>(f.Root);support.Active=0;f.Em.SetComponentData(f.Root,support);
            SkirmishNativeCheckpointCodecTests.Capture(f.Em,out setup,out var actor);
            using var sessions=f.Em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));var session=sessions.GetSingletonEntity();
            var state=f.Em.GetComponentData<SkirmishExpandedSessionComponent>(session);state.Phase=native?SkirmishSessionPhase.Playing:SkirmishSessionPhase.Spawning;
            state.SpawnComplete=(byte)(native?1:0);f.Em.SetComponentData(session,state);
            f.Em.AddComponentData(session,new SkirmishObjectiveClockComponent {Playing=1});
            f.Em.AddComponentData(target,f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor));f.Em.DestroyEntity(actor);
            f.Em.AddComponent<SkirmishSharedActorTag>(target);f.Em.AddComponentData(target,new UnitSourcePrefabKey {Value="target-test"});
            depot=f.Store(10,1,1);f.Em.AddComponentData(depot,new UnitSourcePrefabKey {Value="depot-test"});
            f.Em.AddComponentData(depot,new RuntimeBuildingCombatInfo {RuntimeBuildingId=1,OwnerFactionId=1});
            if(native) {
                support.SessionToken=state.SessionId;support.TestEncounter=0;support.Active=1;f.Em.SetComponentData(f.Root,support);
                f.Em.AddComponentData(f.Root,new SkirmishSupportOwnerComponent {Session=session,SessionId=state.SessionId});
                f.Em.AddComponentData(f.Root,new SupportFuelScopeComponent {Storage=depot,Required=1});
                var policy=f.Em.GetComponentData<SupportMissionPolicyComponent>(f.Root);policy.TestGrantMask=0;policy.OwnedMask=15;f.Em.SetComponentData(f.Root,policy);
                var route=f.Em.GetComponentData<SupportAirRouteComponent>(f.Root);route.SessionToken=state.SessionId;f.Em.SetComponentData(f.Root,route);
                var knowledge=f.Em.GetComponentData<SupportTargetEligibilityComponent>(target);knowledge.SessionToken=state.SessionId;f.Em.SetComponentData(target,knowledge);
                request.SessionToken=state.SessionId;f.Em.SetComponentData(f.Root,new SupportPreviewComponent {PreviewId=1,Request=request,Valid=1,FuelCost=4});
                Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(f.Em,f.Root,request));
            } else {
                var knowledge=f.Em.GetComponentData<SupportTargetEligibilityComponent>(target);knowledge.SessionToken=state.SessionId;f.Em.SetComponentData(target,knowledge);
            }
            using var registries=f.Em.CreateEntityQuery(typeof(UnitPrefabRegistryTag));
            using var list=registries.ToEntityArray(Allocator.Temp);foreach(var registry in list)f.Em.RemoveComponent<UnitPrefabRegistryTag>(registry);
            return session;
        }
        private static void Time(SupportValidationFixture f,Entity root,double seconds)
        {var s=f.Em.GetComponentData<SupportSessionComponent>(root);s.SimulationSeconds=seconds;f.Em.SetComponentData(root,s);}
        private static void RecoverAndAdvance(bool released)
        {
            using var source=new SupportValidationFixture();var session=Prepare(source,true,out var target,out var depot,out var setup);
            var flight=source.Em.GetBuffer<SupportReceiptElement>(source.Root)[0].Effect;
            Time(source,source.Root,released?3:1);if(released)SupportFlightSystem.Advance(source.Em,flight);
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(source.Em,session,_=>"prefab",out var saved,out var failure),failure);
            var wire=SkirmishNativeCheckpointCodec.Encode(saved,setup);Assert.IsNotNull(wire);Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire,setup,out saved));
            using var fresh=new SupportValidationFixture();var freshSession=Prepare(fresh,false,out var freshTarget,out var freshDepot,out _);
            var bindings=new SkirmishNativeCheckpointTransientState.Bindings();SkirmishNativeCheckpointSupportState.Rebuild(fresh.Em,freshSession,saved.Support,bindings);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh.Em,freshSession,saved,setup,_=>null,out var prepared,out failure,bindings),failure);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh.Em,prepared,out failure),failure);
            var root=bindings.Identities["support/root/1"];var restored=fresh.Em.GetBuffer<SupportReceiptElement>(root)[0].Effect;
            Assert.AreEqual(released?750:1000,fresh.Em.GetComponentData<UnitHealth>(freshTarget).Current);
            Time(fresh,root,3);SupportFlightSystem.Advance(fresh.Em,restored);SupportFlightSystem.Advance(fresh.Em,restored);
            Assert.AreEqual(750,fresh.Em.GetComponentData<UnitHealth>(freshTarget).Current);
            var storage=fresh.Em.GetComponentData<BuildingResourceStorageComponent>(freshDepot);
            Assert.AreEqual(6,storage.StoredFuelBarrels);Assert.AreEqual(1,storage.ReservedFuelOutboundBarrels);Assert.AreEqual(1,storage.CivilianFuelReserveBarrels);
            Assert.AreEqual(4,fresh.Em.GetBuffer<SupportReceiptElement>(root)[0].SpentFuel);
            Assert.AreEqual(0,fresh.Em.GetBuffer<SupportAbilityStateElement>(root)[1].ChargesRemaining);
            Time(fresh,root,20);SupportFlightSystem.Advance(fresh.Em,restored);
            Assert.AreEqual(750,fresh.Em.GetComponentData<UnitHealth>(freshTarget).Current);Assert.AreEqual(6,fresh.Em.GetComponentData<BuildingResourceStorageComponent>(freshDepot).StoredFuelBarrels);
        }
        [Test] public void ApproachingFlightRestoresAndImpactsOnce()=>RecoverAndAdvance(false);
        [Test] public void ReleasedFlightRestoresWithoutSecondPaymentOrHit()=>RecoverAndAdvance(true);
        [Test] public void NativePauseThenTerminalReleasesReservationOnce()
        {
            using var f=new SupportValidationFixture();var owner=Prepare(f,true,out var target,out var depot,out _);var flight=f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].Effect;
            var state=f.Em.GetComponentData<SupportSessionComponent>(f.Root);state.Active=0;state.SimulationSeconds=3;f.Em.SetComponentData(f.Root,state);
            SupportFlightSystem.Advance(f.Em,flight);Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);Assert.AreEqual(5,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).ReservedFuelOutboundBarrels);
            f.Em.AddComponent<SkirmishExpandedCleanupRequest>(owner);SupportFlightSystem.Advance(f.Em,flight);SupportFlightSystem.Abort(f.Em,flight,SupportRejectionReason.WrongAttempt);
            Assert.AreEqual(1,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).ReservedFuelOutboundBarrels);Assert.AreEqual(10,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredFuelBarrels);
            Assert.AreEqual(1,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[1].ChargesRemaining);
        }
        [Test] public void MismatchedPaidLedgerCannotBeSaved()
        {
            using var f=new SupportValidationFixture();var session=Prepare(f,true,out var target,out var depot,out _);
            var flight=f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0].Effect;
            var fuel=f.Em.GetBuffer<SupportFuelReservationElement>(flight);var reservation=fuel[0];reservation.Amount--;fuel[0]=reservation;
            Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(f.Em,session,_=>"prefab",out _,out _));
            Assert.AreEqual(1000,f.Em.GetComponentData<UnitHealth>(target).Current);
            Assert.AreEqual(10,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredFuelBarrels);
            Assert.AreEqual(5,f.Em.GetComponentData<BuildingResourceStorageComponent>(depot).ReservedFuelOutboundBarrels);
        }
        [Test] public void RegistryWithoutTagAndAmbiguityFailClosed()
        {
            using var f=new SupportValidationFixture();SupportStrikeValidation.Prepare(f,out _);
            using var q=f.Em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));var registry=q.GetSingletonEntity();f.Em.RemoveComponent<UnitPrefabRegistryTag>(registry);
            var original=SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(f.Em,"jet-test");Assert.AreNotEqual(Entity.Null,original);
            var duplicate=f.Em.Instantiate(original);f.Em.AddComponent<Prefab>(duplicate);f.Em.GetBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry {Prefab=duplicate});
            Assert.AreEqual(Entity.Null,SupportAirValidationUtilitySystemHelper.ResolveAircraftPrefab(f.Em,"jet-test"));
            var cleanup=f.Em.CreateEntity();f.Em.AddComponent<SupportFlightCleanupComponent>(cleanup);f.Em.AddBuffer<SupportFuelReservationElement>(cleanup);
            Assert.Throws<InvalidOperationException>(()=>SkirmishNativeCheckpointTransientState.Capture(f.Em,new List<Entity>(),new Dictionary<Entity,string>(),new HashSet<string>(),out _,out _));
        }
    }
}
#endif
