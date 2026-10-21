using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Synthetic provider isolates the birth contract. It is NOT a real loaded EntityScene or source-fit fixture.
    public sealed class RuntimeActorBirthContextTests
    {
        sealed class Provider : IRuntimeActorBirthSourceProvider
        {
            internal RuntimeActorSourceIdentity Identity;
            public bool TryReadBirthSource(World world, Entity grid, out RuntimeActorSourceIdentity source)
            { source = Identity; return world.Unmanaged.SequenceNumber == Identity.WorldSequence && grid == Identity.Grid; }
        }
        sealed class Fixture : IDisposable
        {
            internal readonly World World = new World("isolated-birth-contract-not-normal-mission");
            internal EntityManager Em => World.EntityManager;
            internal readonly Entity Grid, Root, Scene, Session;
            internal readonly GridConfig Layout = new GridConfig { Width = 16, Height = 16, CellSize = 1, Origin = float3.zero };
            internal readonly Provider Source = new Provider();
            internal readonly FixedString64Bytes Attempt = new FixedString64Bytes("birth-attempt");
            internal BuildingRuntimeSpawnRequest Request;
            internal Fixture()
            {
                Grid = Em.CreateEntity(typeof(GridConfig)); Em.SetComponentData(Grid, Layout);
                Root = Em.CreateEntity(typeof(OperationMapRootComponent)); Scene = Em.CreateEntity();
                Session = Em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(BuildingStartingGrantOwner));
                Em.SetComponentData(Session, new SkirmishExpandedSessionComponent { SessionId = Attempt, Phase = SkirmishSessionPhase.Playing });
                Em.AddComponentObject(Session, new SkirmishResolvedSetupRecord { Setup = new SkirmishResolvedSetup {
                    Structures = new[] { new SkirmishResolvedStructureEntry { StructureId = SkirmishStructureIds.Barracks, FactionId = 1 } } } });
                Request = new BuildingRuntimeSpawnRequest { RequestId = 7, PlanEntity = Session, EntryIndex = 0,
                    BuildingId = new FixedString128Bytes(SkirmishStructureIds.VisualKey(SkirmishStructureIds.Barracks)),
                    FactionId = 1, HasOwnerFaction = 1, AuthoredStartingGrant = 1, PreferredOrigin = new int2(2, 3) };
                Em.AddBuffer<SkirmishStartingBuildingRequest>(Session).Add(new SkirmishStartingBuildingRequest { RequestId = 7, StructureIndex = 0 });
                ResetSource(); Em.AddComponentObject(Root, new RuntimeActorBirthSourceProvider(Source));
            }
            internal void ResetSource(int generation = 7, string content = "content") => Source.Identity = new RuntimeActorSourceIdentity(
                World.Unmanaged.SequenceNumber, Root, Grid, Scene, generation, 123, 456, "identity", content, "generated", Layout);
            internal RuntimeActorBirthContext Capture() => RuntimeActorBirthContext.CaptureBuilding(Em, Grid, Layout, Request);
            internal (Entity blocker, Entity combat) CreatePair(RuntimeActorBirthContext birth)
            {
                var em = Em;
                var context = new BuildingRuntimeEntityCompositionSystemHelper.Context(
                    (out EntityManager result) => { result = em; return true; },
                    (out Entity entity, out GridConfig layout, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockers) =>
                    { entity = Grid; layout = Layout; roads = default; blockers = default; return true; },
                    (origin, footprint, layout) => new Vector3(origin.x, 0, origin.y), null, default, null, 0);
                var creator = new BuildingRuntimeEntityCompositionSystemHelper();
                var definition = new BuildingDefinition { DisplayName = "birth-control", FootprintCells = new Vector2Int(2, 2), MaxHealth = 100 };
                var blocker = creator.CreateBlockerEntity(context, definition, new Vector2Int(2, 3), definition.FootprintCells, birth);
                var combat = creator.CreateBuildingCombatEntity(context, 11, new Vector2Int(2, 3), definition, 1, Quaternion.identity, birth);
                Em.AddComponentObject(combat, new SkirmishNativeBuildingCheckpointRecord { Building = new RuntimeBuildingEntity {
                    Id = 11, BlockerEntity = blocker, CombatEntity = combat } });
                return (blocker, combat);
            }
            internal string Reason(Entity actor) => RuntimeActorBirthContext.ContributorUnavailableReason(Em, actor, Grid, Scene, Attempt);
            public void Dispose() => World.Dispose();
        }
        [Test] public void ActualSeparateBuildingBirthsCarryRequestWithoutInventingGameplayOwnership()
        {
            using var f = new Fixture(); var pair = f.CreatePair(f.Capture());
            Assert.AreNotEqual(pair.blocker, pair.combat);
            Assert.IsTrue(RuntimeActorBirthContext.HasRecord(f.Em, pair.blocker)); Assert.IsTrue(RuntimeActorBirthContext.HasRecord(f.Em, pair.combat));
            Assert.IsFalse(f.Em.HasComponent<SkirmishAttemptOwnedComponent>(pair.blocker)); Assert.IsFalse(f.Em.HasComponent<SkirmishAttemptOwnedComponent>(pair.combat));
            Assert.IsNull(f.Reason(pair.blocker)); Assert.AreEqual("WriterBirthActorOwnershipMissing", f.Reason(pair.combat));
            var result = f.Request; result.Status = BuildingRuntimeSpawnRequest.Succeeded;
            RuntimeActorBirthContext.VerifyBuildingAdoption(f.Em, pair.combat, result);
            Assert.IsTrue(RuntimeActorBirthContext.HasRecord(f.Em, pair.blocker)); Assert.IsTrue(RuntimeActorBirthContext.HasRecord(f.Em, pair.combat));
            Assert.AreEqual(new int2(2, 3), f.Em.GetComponentData<UnitGrid>(pair.blocker).Cell);
        }
        [Test] public void MismatchedResultInvalidatesBothRecordsWithoutChangingActualActors()
        {
            using var f = new Fixture(); var pair = f.CreatePair(f.Capture()); var wrong = f.Request; wrong.RequestId++;
            RuntimeActorBirthContext.VerifyBuildingAdoption(f.Em, pair.combat, wrong);
            Assert.IsFalse(RuntimeActorBirthContext.HasRecord(f.Em, pair.blocker)); Assert.IsFalse(RuntimeActorBirthContext.HasRecord(f.Em, pair.combat));
            Assert.IsTrue(f.Em.HasComponent<StaticGridBlocker>(pair.blocker)); Assert.AreEqual(100, f.Em.GetComponentData<UnitHealth>(pair.combat).Current);
            Assert.AreEqual(new int2(2, 3), f.Em.GetComponentData<UnitGrid>(pair.blocker).Cell);
        }
        [Test] public void MissingProducerDoesNotBlockActualBuildingBirthOrCreateProofAtAdoption()
        {
            using var f = new Fixture(); f.Em.RemoveComponent<RuntimeActorBirthSourceProvider>(f.Root);
            var pair = f.CreatePair(f.Capture()); Assert.IsTrue(f.Em.Exists(pair.blocker)); Assert.IsTrue(f.Em.Exists(pair.combat));
            Assert.IsFalse(RuntimeActorBirthContext.HasRecord(f.Em, pair.blocker));
            f.Em.AddComponentObject(f.Root, new RuntimeActorBirthSourceProvider(f.Source));
            RuntimeActorBirthContext.VerifyBuildingAdoption(f.Em, pair.combat, f.Request);
            Assert.IsFalse(RuntimeActorBirthContext.HasRecord(f.Em, pair.blocker)); Assert.IsFalse(RuntimeActorBirthContext.HasRecord(f.Em, pair.combat));
        }
        [Test] public void OriginalRequestEntryOwnerAndExactPlacementLayoutAreRequired()
        {
            using var f = new Fixture(); Assert.IsTrue(RuntimeActorBirthContext.IsOwnedBuildingRequest(f.Em, f.Request));
            var r = f.Request; r.RequestId++; Assert.IsFalse(RuntimeActorBirthContext.IsOwnedBuildingRequest(f.Em, r));
            r = f.Request; r.EntryIndex++; Assert.IsFalse(RuntimeActorBirthContext.IsOwnedBuildingRequest(f.Em, r));
            r = f.Request; r.PlanEntity = f.Grid; Assert.IsFalse(RuntimeActorBirthContext.IsOwnedBuildingRequest(f.Em, r));
            r = f.Request; r.AuthoredStartingGrant = 0; Assert.IsFalse(RuntimeActorBirthContext.IsOwnedBuildingRequest(f.Em, r));
            var wrong = f.Layout; wrong.Width++;
            var context = RuntimeActorBirthContext.CaptureBuilding(f.Em, f.Grid, wrong, f.Request);
            Assert.IsFalse(context.IsCurrent(f.Em, f.Grid));
            var other = f.Em.CreateEntity(typeof(GridConfig)); f.Em.SetComponentData(other, f.Layout);
            Assert.IsFalse(RuntimeActorBirthContext.CaptureBuilding(f.Em, other, f.Layout, f.Request).IsCurrent(f.Em, other));
        }
        [Test] public void LaterGenerationContentOrLayoutCannotBlessOldBirth()
        {
            using var f = new Fixture(); var pair = f.CreatePair(f.Capture()); Assert.IsNull(f.Reason(pair.blocker));
            f.ResetSource(8); Assert.AreEqual("WriterBirthSourceLifetimeChanged", f.Reason(pair.blocker));
            f.ResetSource(content: "changed"); Assert.AreEqual("WriterBirthSourceLifetimeChanged", f.Reason(pair.blocker));
            var changed = f.Layout; changed.Origin.x++;
            f.Source.Identity = new RuntimeActorSourceIdentity(f.World.Unmanaged.SequenceNumber, f.Root, f.Grid, f.Scene, 7, 123, 456, "identity", "content", "generated", changed);
            Assert.AreEqual("WriterBirthSourceLifetimeChanged", f.Reason(pair.blocker));
        }
        [Test] public void InstantiateAndEntityVersionReuseCannotCopyUsableProvenance()
        {
            using var f = new Fixture(); var pair = f.CreatePair(f.Capture());
            var copy = f.Em.Instantiate(pair.blocker); Assert.AreEqual("ForeignOrClonedWriterBirth", f.Reason(copy));
            var record = f.Em.GetComponentData<RuntimeActorBirthContext.RuntimeActorBirthRecord>(pair.blocker);
            f.Em.DestroyEntity(pair.blocker); var replacement = f.Em.CreateEntity(typeof(StaticGridBlocker));
            f.Em.AddComponentData(replacement, record); Assert.AreEqual("ForeignOrClonedWriterBirth", f.Reason(replacement));
        }
        [Test] public void WorldAndSessionLifetimeMismatchRefuseUnchangedRecord()
        {
            using var f = new Fixture(); var pair = f.CreatePair(f.Capture());
            using var other = new World("foreign-birth-world"); var actor = other.EntityManager.CreateEntity();
            var record = f.Em.GetComponentData<RuntimeActorBirthContext.RuntimeActorBirthRecord>(pair.blocker);
            other.EntityManager.AddComponentData(actor, record);
            Assert.AreEqual("ForeignOrClonedWriterBirth", RuntimeActorBirthContext.ContributorUnavailableReason(other.EntityManager, actor, f.Grid, f.Scene, f.Attempt));
            var state = f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session); state.SessionId = new FixedString64Bytes("other");
            f.Em.SetComponentData(f.Session, state); Assert.AreEqual("WriterBirthSourceLifetimeChanged", f.Reason(pair.blocker));
        }
        [Test] public void RealReservationCorrelationRefusesWrongReceiptProducerAndAttempt()
        {
            using var f = new Fixture(); var key = new FixedString64Bytes(SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle));
            f.Em.AddBuffer<SkirmishProductionReservation>(f.Session).Add(new SkirmishProductionReservation {
                ReservationId = 3, ProducerRuntimeId = 42, FactionId = 1, Role = SkirmishRoleKind.Rifle, MemberCount = 2, Phase = SkirmishReservationPhase.Producing });
            var good = RuntimeActorBirthContext.CaptureNative(f.Em, f.Grid, f.Session, 3, 42, 1, key, f.Attempt, f.Layout);
            Assert.IsTrue(good.IsCurrent(f.Em, f.Grid));
            Assert.IsFalse(RuntimeActorBirthContext.CaptureNative(f.Em, f.Grid, f.Session, 4, 42, 1, key, f.Attempt, f.Layout).IsCurrent(f.Em, f.Grid));
            Assert.IsFalse(RuntimeActorBirthContext.CaptureNative(f.Em, f.Grid, f.Session, 3, 43, 1, key, f.Attempt, f.Layout).IsCurrent(f.Em, f.Grid));
            Assert.IsFalse(RuntimeActorBirthContext.CaptureNative(f.Em, f.Grid, f.Session, 3, 42, 1, key, new FixedString64Bytes("old"), f.Layout).IsCurrent(f.Em, f.Grid));
            var actor = f.Em.CreateEntity(); good.StampFresh(f.Em, actor, f.Grid);
            RuntimeActorBirthContext.VerifyNativeAdoption(f.Em, actor, f.Session, 3, 99, f.Attempt);
            Assert.IsFalse(RuntimeActorBirthContext.HasRecord(f.Em, actor)); Assert.IsTrue(f.Em.Exists(actor));
            Assert.AreEqual(0, f.Em.GetBuffer<SkirmishProductionReservation>(f.Session)[0].DeliveredMembers);
        }
        [Test] public void ReconstructionRequiresFreshRealOwnerContextNotSavedEntityPointers()
        {
            using var f = new Fixture(); var stable = new FixedString64Bytes("saved.unit");
            Assert.IsFalse(RuntimeActorBirthContext.CaptureReconstruction(f.Em, f.Grid, f.Session, stable).IsCurrent(f.Em, f.Grid));
            f.Em.AddComponentObject(f.Session, new SkirmishNativeCheckpointRecoveryRecord());
            var fresh = RuntimeActorBirthContext.CaptureReconstruction(f.Em, f.Grid, f.Session, stable); Assert.IsTrue(fresh.IsCurrent(f.Em, f.Grid));
            var actor = f.Em.CreateEntity(); fresh.StampFresh(f.Em, actor, f.Grid);
            f.Em.AddComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = f.Attempt, StableObjectId = stable });
            Assert.IsNull(f.Reason(actor));
            var copy = f.Em.Instantiate(actor); Assert.AreEqual("ForeignOrClonedWriterBirth", f.Reason(copy));
        }
        [Test] public void ScenarioUsesExplicitUniqueSessionAndImmutableStableIdentity()
        {
            using var f = new Fixture(); var stable = new FixedString64Bytes("force.0.1");
            var birth = RuntimeActorBirthContext.CaptureScenario(f.Em, f.Grid, f.Attempt, stable, f.Layout);
            var actor = f.Em.CreateEntity(); birth.StampFresh(f.Em, actor, f.Grid);
            f.Em.AddComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = f.Attempt, StableObjectId = stable }); Assert.IsNull(f.Reason(actor));
            f.Em.SetComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = f.Attempt, StableObjectId = new FixedString64Bytes("other") });
            Assert.AreEqual("WriterBirthAttemptChanged", f.Reason(actor));
            var duplicate = f.Em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            f.Em.SetComponentData(duplicate, f.Em.GetComponentData<SkirmishExpandedSessionComponent>(f.Session));
            Assert.IsFalse(RuntimeActorBirthContext.CaptureScenario(f.Em, f.Grid, f.Attempt, stable, f.Layout).IsCurrent(f.Em, f.Grid));
        }
        [Test] public void RealUnboundCompositionProviderCannotIssueBirthSource()
        {
            using var f = new Fixture(); var go = new GameObject("inactive-real-birth-provider"); go.SetActive(false);
            try
            {
                var view = go.AddComponent<MatchSceneView>();
                Assert.IsFalse(view.TryReadBirthSource(f.World, f.Grid, out _));
                f.Em.RemoveComponent<RuntimeActorBirthSourceProvider>(f.Root);
                f.Em.AddComponentObject(f.Root, new RuntimeActorBirthSourceProvider(view));
                Assert.IsFalse(f.Capture().IsCurrent(f.Em, f.Grid));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [Test] public void CheckpointWhitelistDoesNotSerializeBirthRecordOrSourcePointers()
        {
            using var f = new Fixture(); var pair = f.CreatePair(f.Capture());
            var withBirth = SkirmishNativeCheckpointParts.Capture(f.Em, pair.blocker, _ => "unused");
            f.Em.RemoveComponent<RuntimeActorBirthContext.RuntimeActorBirthRecord>(pair.blocker);
            var withoutBirth = SkirmishNativeCheckpointParts.Capture(f.Em, pair.blocker, _ => "unused");
            Assert.AreEqual(withoutBirth.Length, withBirth.Length);
            for (int i = 0; i < withBirth.Length; i++)
            { Assert.AreEqual(withoutBirth[i].Type, withBirth[i].Type); Assert.AreEqual(withoutBirth[i].Data, withBirth[i].Data); }
        }
        public static void RunFocusedValidation()
        {
            var t = new RuntimeActorBirthContextTests();
            try
            {
                t.ActualSeparateBuildingBirthsCarryRequestWithoutInventingGameplayOwnership(); t.MismatchedResultInvalidatesBothRecordsWithoutChangingActualActors();
                t.MissingProducerDoesNotBlockActualBuildingBirthOrCreateProofAtAdoption(); t.OriginalRequestEntryOwnerAndExactPlacementLayoutAreRequired();
                t.LaterGenerationContentOrLayoutCannotBlessOldBirth(); t.InstantiateAndEntityVersionReuseCannotCopyUsableProvenance();
                t.WorldAndSessionLifetimeMismatchRefuseUnchangedRecord(); t.RealReservationCorrelationRefusesWrongReceiptProducerAndAttempt();
                t.ReconstructionRequiresFreshRealOwnerContextNotSavedEntityPointers(); t.ScenarioUsesExplicitUniqueSessionAndImmutableStableIdentity();
                t.RealUnboundCompositionProviderCannotIssueBirthSource(); t.CheckpointWhitelistDoesNotSerializeBirthRecordOrSourcePointers();
                Debug.Log("[RuntimeActorBirthContextTests] result=Passed controls=12 scope=IsolatedContractWithSyntheticProvider normalEntitySceneObserved=0 sourceFitCertified=0 missionReady=0");
            }
            catch (Exception error) { Debug.LogError("[RuntimeActorBirthContextTests] result=Failed\n" + error); throw; }
        }
    }
}
