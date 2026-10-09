#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeSupportRecoveryTests
    {
        public static void RunFocusedValidation()
        {
            try
            {
                var tests=new SkirmishNativeSupportRecoveryTests();
                tests.NativeLifecyclePreservesCostsAcrossPauseAndEndsWithOwner();
                tests.ColdWorldRebindsSmokeAndPaidLedger();
                tests.ForeignOwnerAndUnsupportedFlightFailClosed();
                tests.ExpiredReceiptIsNormalizedWithoutChangingLiveLedger();
                tests.NativeWireAndRecoveryCommitRestoreSupport();
                Debug.Log("[SkirmishNativeSupportRecoveryTests] result=Passed tests=5 nativePlayerJourneys=0");
                ValidationExit.Exit(0);
            }
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SkirmishNativeSupportRecoveryTests] result=Failed");ValidationExit.Exit(1);throw;}
        }

        private static Entity Native(SupportValidationFixture f)
        {
            var entity=f.Em.CreateEntity();
            f.Em.AddComponentData(entity,new SkirmishExpandedSessionComponent {
                SessionId="support-native",CatalogId="S005",Phase=SkirmishSessionPhase.Playing,SpawnComplete=1 });
            f.Em.AddComponentData(entity,new SkirmishObjectiveClockComponent {Playing=1});
            var state=f.Em.GetComponentData<SupportSessionComponent>(f.Root);
            state.SessionToken="support-native";state.TestEncounter=0;state.SimulationSeconds=10;
            f.Em.SetComponentData(f.Root,state);
            f.Em.AddComponentData(f.Root,new SkirmishSupportOwnerComponent {Session=entity,SessionId=state.SessionToken});
            f.Em.SetComponentData(f.Root,default(SupportPreviewComponent));
            return entity;
        }
        private static SkirmishNativeCheckpointSupportState.Record[] Capture(SupportValidationFixture f,Entity session)
        {
            var ids=new Dictionary<Entity,string> {[session]="$session"};
            return SkirmishNativeCheckpointSupportState.Capture(f.Em,session,ids,new HashSet<string> {"$session"});
        }

        [Test] public void NativeLifecyclePreservesCostsAcrossPauseAndEndsWithOwner()
        {
            using var f=new SupportValidationFixture();var session=Native(f);
            var gameplay=f.Em.CreateEntity();f.Em.AddComponentData(gameplay,new RuntimeGameplayStateComponent {SimulationActive=1,PlayRequested=1});
            var ability=f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0];ability.ChargesRemaining=1;ability.CooldownUntil=45;
            var abilities=f.Em.GetBuffer<SupportAbilityStateElement>(f.Root);abilities[0]=ability;
            var system=f.World.CreateSystem<SupportAbilityStartupSystem>();system.Update(f.World.Unmanaged);
            Assert.AreEqual(1,f.Em.GetComponentData<SupportSessionComponent>(f.Root).Active);
            var clock=f.Em.GetComponentData<SkirmishObjectiveClockComponent>(session);clock.Paused=1;f.Em.SetComponentData(session,clock);
            system.Update(f.World.Unmanaged);var paused=f.Em.GetComponentData<SupportSessionComponent>(f.Root);
            Assert.AreEqual(0,paused.Active);Assert.AreEqual(ability,f.Em.GetBuffer<SupportAbilityStateElement>(f.Root)[0]);
            system.Update(f.World.Unmanaged);Assert.AreEqual(paused.SimulationSeconds,f.Em.GetComponentData<SupportSessionComponent>(f.Root).SimulationSeconds);
            Assert.IsFalse(SupportSessionUtility.IsTerminal(f.Em,f.Root,paused));
            clock.Paused=0;f.Em.SetComponentData(session,clock);system.Update(f.World.Unmanaged);
            Assert.AreEqual(1,f.Em.GetComponentData<SupportSessionComponent>(f.Root).Active);
            var zone=f.Em.CreateEntity();f.Em.AddComponentData(zone,new SupportSmokeZoneComponent {Root=f.Root,SessionToken="support-native",AttemptOrdinal=1});
            f.Em.AddComponent<SkirmishExpandedCleanupRequest>(session);
            system.Update(f.World.Unmanaged);Assert.IsTrue(SupportSessionUtility.IsTerminal(f.Em,f.Root,f.Em.GetComponentData<SupportSessionComponent>(f.Root)));
            var cleanup=f.World.CreateSystem<SupportAttemptCleanupSystem>();cleanup.Update(f.World.Unmanaged);
            Assert.IsFalse(f.Em.Exists(zone));
            f.Em.DestroyEntity(session);Assert.IsTrue(SupportSessionUtility.IsTerminal(f.Em,f.Root,paused));
        }

        [Test] public void ColdWorldRebindsSmokeAndPaidLedger()
        {
            using var source=new SupportValidationFixture();var session=Native(source);
            var ability=source.Em.GetBuffer<SupportAbilityStateElement>(source.Root)[0];ability.ChargesRemaining=1;ability.CooldownUntil=45;
            var abilities=source.Em.GetBuffer<SupportAbilityStateElement>(source.Root);abilities[0]=ability;
            var zone=source.Em.CreateEntity();source.Em.AddComponentData(zone,new SupportSmokeZoneComponent {
                Root=source.Root,SessionToken="support-native",AttemptOrdinal=1,Center=new float3(3,0,7),RadiusSquared=144,ExpiresAt=25,DirectDamagePermille=650 });
            source.Em.GetBuffer<SupportReceiptElement>(source.Root).Add(new SupportReceiptElement {Effect=zone,RequestId=7,SpentFuel=1,EffectApplied=1});
            var state=source.Em.GetComponentData<SupportSessionComponent>(source.Root);state.LastPlayerRequestId=7;state.NextRequestId=8;source.Em.SetComponentData(source.Root,state);
            var records=Capture(source,session);
            using var fresh=new SupportValidationFixture();var freshSession=fresh.Em.CreateEntity();
            fresh.Em.AddComponentData(freshSession,new SkirmishExpandedSessionComponent {SessionId="support-native"});
            var bindings=new SkirmishNativeCheckpointTransientState.Bindings();
            SkirmishNativeCheckpointSupportState.Rebuild(fresh.Em,freshSession,records,bindings);
            Entity Resolve(string id)=>id=="$session"?freshSession:bindings.Identities[id];
            foreach(var record in records)SkirmishNativeCheckpointParts.Apply(fresh.Em,Resolve(record.Id),record.Parts,Resolve);
            var root=Resolve("support/root/1");var restored=fresh.Em.GetComponentData<SupportSessionComponent>(root);
            Assert.AreEqual(10,restored.SimulationSeconds);Assert.AreEqual(7,restored.LastPlayerRequestId);Assert.AreEqual(8,restored.NextRequestId);
            Assert.AreEqual(ability,fresh.Em.GetBuffer<SupportAbilityStateElement>(root)[0]);
            Assert.IsFalse(fresh.Em.HasComponent<CampaignMissionRuntimeComponent>(root));
            Assert.AreEqual(freshSession,fresh.Em.GetComponentData<SkirmishSupportOwnerComponent>(root).Session);
            var restoredZone=fresh.Em.GetBuffer<SupportReceiptElement>(root)[0].Effect;
            Assert.AreEqual(root,fresh.Em.GetComponentData<SupportSmokeZoneComponent>(restoredZone).Root);
            Assert.AreEqual(25,fresh.Em.GetComponentData<SupportSmokeZoneComponent>(restoredZone).ExpiresAt);
            Assert.AreEqual(0,fresh.Em.GetComponentData<SupportCatalogComponent>(root).OwnsBlob);
            SkirmishNativeCheckpointTransientState.DestroyCreated(fresh.Em,bindings);
            Assert.IsFalse(fresh.Em.Exists(root));Assert.IsTrue(fresh.Em.Exists(fresh.Root));
        }

        [Test] public void ForeignOwnerAndUnsupportedFlightFailClosed()
        {
            using var f=new SupportValidationFixture();var session=Native(f);
            var owner=f.Em.GetComponentData<SkirmishSupportOwnerComponent>(f.Root);owner.SessionId="foreign";f.Em.SetComponentData(f.Root,owner);
            Assert.Throws<InvalidOperationException>(()=>Capture(f,session));
            owner.SessionId="support-native";f.Em.SetComponentData(f.Root,owner);
            var records=Capture(f,session);records[0].Faction=2;
            Assert.IsFalse(SkirmishNativeCheckpointSupportState.PublishAliases(records,"support-native",new HashSet<string> {"$session"}));
            var flight=f.Em.CreateEntity();f.Em.AddComponent<SupportFlightComponent>(flight);
            Assert.Throws<InvalidOperationException>(()=>SkirmishNativeCheckpointTransientState.Capture(f.Em,new List<Entity>(),
                new Dictionary<Entity,string>(),new HashSet<string>(),out _,out _));
        }

        [Test] public void NativeWireAndRecoveryCommitRestoreSupport()
        {
            using var source=new SupportValidationFixture();
            var support=source.Em.GetComponentData<SupportSessionComponent>(source.Root);support.Active=0;source.Em.SetComponentData(source.Root,support);
            SkirmishNativeCheckpointCodecTests.Capture(source.Em,out var setup,out _);
            using var sessions=source.Em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            var session=sessions.GetSingletonEntity();var native=source.Em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            native.Phase=SkirmishSessionPhase.Playing;native.SpawnComplete=1;source.Em.SetComponentData(session,native);
            support.SessionToken=native.SessionId;support.TestEncounter=0;support.Active=1;support.SimulationSeconds=10;
            source.Em.SetComponentData(source.Root,support);
            source.Em.AddComponentData(source.Root,new SkirmishSupportOwnerComponent {Session=session,SessionId=native.SessionId});
            source.Em.SetComponentData(source.Root,default(SupportPreviewComponent));
            var abilities=source.Em.GetBuffer<SupportAbilityStateElement>(source.Root);var ability=abilities[0];ability.ChargesRemaining=1;ability.CooldownUntil=45;abilities[0]=ability;
            var zone=source.Em.CreateEntity();source.Em.AddComponentData(zone,new SupportSmokeZoneComponent {
                Root=source.Root,SessionToken=native.SessionId,AttemptOrdinal=1,Center=float3.zero,RadiusSquared=144,ExpiresAt=25,DirectDamagePermille=650 });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(source.Em,session,_=>"prefab",out var saved,out var failure),failure);
            var wire=SkirmishNativeCheckpointCodec.Encode(saved,setup);Assert.IsNotNull(wire);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.TryDecode(wire,setup,out saved));
            using var fresh=new SupportValidationFixture();var template=fresh.Em.GetComponentData<SupportSessionComponent>(fresh.Root);template.Active=0;fresh.Em.SetComponentData(fresh.Root,template);
            SkirmishNativeCheckpointCodecTests.Capture(fresh.Em,out _,out var actor);
            using var freshSessions=fresh.Em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            var freshSession=freshSessions.GetSingletonEntity();var freshNative=fresh.Em.GetComponentData<SkirmishExpandedSessionComponent>(freshSession);
            freshNative.Phase=SkirmishSessionPhase.Spawning;fresh.Em.SetComponentData(freshSession,freshNative);
            fresh.Em.SetComponentData(actor,new UnitHealth {Current=99,Max=100});
            var bindings=new SkirmishNativeCheckpointTransientState.Bindings();
            SkirmishNativeCheckpointSupportState.Rebuild(fresh.Em,freshSession,saved.Support,bindings);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryPreparation.TryPrepare(fresh.Em,freshSession,saved,setup,_=>null,out var prepared,out failure,bindings),failure);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryCommit.TryCommit(fresh.Em,prepared,out failure),failure);
            var root=bindings.Identities["support/root/1"];
            Assert.AreEqual(ability,fresh.Em.GetBuffer<SupportAbilityStateElement>(root)[0]);
            Assert.AreEqual(10,fresh.Em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds);
            Assert.AreEqual(37,fresh.Em.GetComponentData<UnitHealth>(actor).Current);
            Assert.AreEqual(SkirmishSessionPhase.Playing,fresh.Em.GetComponentData<SkirmishExpandedSessionComponent>(freshSession).Phase);
            SkirmishNativeCheckpointTransientState.DestroySession(fresh.Em,freshNative.SessionId);
            Assert.IsFalse(fresh.Em.Exists(root));
        }

        [Test] public void ExpiredReceiptIsNormalizedWithoutChangingLiveLedger()
        {
            using var f=new SupportValidationFixture();var session=Native(f);
            var expired=f.Em.CreateEntity();f.Em.DestroyEntity(expired);
            var receipt=new SupportReceiptElement {Effect=expired,RequestId=3,SpentFuel=1,EffectApplied=1};
            f.Em.GetBuffer<SupportReceiptElement>(f.Root).Add(receipt);
            var records=Capture(f,session);
            Assert.IsTrue(SkirmishNativeCheckpointParts.TryReadBuffer(records[0].Parts,out SupportReceiptElement[] saved));
            Assert.AreEqual(Entity.Null,saved[0].Effect);Assert.AreEqual(1,saved[0].SpentFuel);Assert.AreEqual(1,saved[0].EffectApplied);
            Assert.AreEqual(receipt,f.Em.GetBuffer<SupportReceiptElement>(f.Root)[0]);
        }
    }
}
#endif
