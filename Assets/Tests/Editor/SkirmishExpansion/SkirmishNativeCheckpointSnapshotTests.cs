using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointSnapshotTests
    {
        [Test]
        public void NativeConstructionCommitMakesBothFabricationActorsCheckpointable()
        {
            using var world = new World(nameof(NativeConstructionCommitMakesBothFabricationActorsCheckpointable));
            var em = world.EntityManager;
            Entity session = Session(em);
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.Phase = Game.Skirmish.Contracts.SkirmishSessionPhase.Playing;
            em.SetComponentData(session, state);
            em.AddComponentObject(session, new SkirmishResolvedSetupRecord { Setup = new Game.Configs.SkirmishResolvedSetup {ObjectiveKind=Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault} });
            em.AddComponentObject(session, new SkirmishProductionCatalogRecord());
            foreach (int id in new[] { 15, 16 })
            {
                Entity building = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitHealth));
                em.SetComponentData(building, new RuntimeBuildingCombatInfo { RuntimeBuildingId = id, OwnerFactionId = 1 });
                em.SetComponentData(building, new UnitHealth { Current = 900, Max = 900 });
                em.AddComponentObject(building, new SkirmishNativeBuildingCheckpointRecord
                    { Building = new RuntimeBuildingEntity { CombatEntity = building, Id = id } });
                Assert.IsTrue(SkirmishScenarioSpawnSystem.AdoptConstructedBuilding(em, building, "Building_Ammunition_Depot"));
                Assert.IsTrue(em.HasComponent<SkirmishSharedActorTag>(building));
                Assert.AreEqual("constructed." + id, em.GetComponentData<SkirmishAttemptOwnedComponent>(building).StableObjectId.ToString());
                Assert.AreEqual("Building_Ammunition_Depot", em.GetComponentData<UnitSourcePrefabKey>(building).Value.ToString());
                Assert.IsFalse(em.HasComponent<SkirmishObjectiveRoleComponent>(building));
            }
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var snapshot, out var failure), failure);
            Assert.IsTrue(SkirmishNativeLegacyBa7Json.TryDecodeInner(SkirmishNativeLegacyBa7Json.EncodeInner(snapshot),out var decoded));
            Assert.IsNull(decoded.Objective);
            Assert.AreEqual(2, decoded.Actors.Length);
            Assert.AreNotEqual(decoded.Actors[0].Id, decoded.Actors[1].Id);
            Assert.IsTrue(decoded.Actors[0].HasProduction && decoded.Actors[1].HasProduction);
        }

        [Test]
        public void NativeStructureCaptureRequiresItsAuthoritativeProductionOwner()
        {
            using var world = new World(nameof(NativeStructureCaptureRequiresItsAuthoritativeProductionOwner));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity actor = Actor(em, "original-base", true);
            em.AddComponentData(actor, new RuntimeBuildingCombatInfo { RuntimeBuildingId = 5 });
            Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out _, out var failure));
            StringAssert.StartsWith("NativeBuildingOwnerMissing", failure);
            em.AddComponentObject(actor, new SkirmishNativeBuildingCheckpointRecord
            {
                Building = new RuntimeBuildingEntity { CombatEntity = actor, Id = 5 }
            });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var snapshot, out failure), failure);
            Assert.AreEqual(1, snapshot.Actors.Length);
            Assert.IsTrue(snapshot.Actors[0].HasProduction);
            Assert.AreEqual(0, snapshot.Actors[0].Production.Queue.Length);
        }

        [Test]
        public void UnmappedCombatReferenceRejectsCaptureWithoutPartialSnapshot()
        {
            using var world = new World(nameof(UnmappedCombatReferenceRejectsCaptureWithoutPartialSnapshot));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity actor = Actor(em, "rifle-1", false);
            Entity target = em.CreateEntity();
            em.AddComponentData(actor, new EngageTarget { Target = target });
            Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var snapshot, out var failure));
            Assert.IsNull(snapshot);
            StringAssert.StartsWith("NativeCaptureIncomplete", failure);
            Assert.AreEqual(target, em.GetComponentData<EngageTarget>(actor).Target);
        }

        [Test]
        public void MapReferencesUseSourceFootprintsInsteadOfCrossWorldHandles()
        {
            using var world = new World(nameof(MapReferencesUseSourceFootprintsInsteadOfCrossWorldHandles));
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity actor = Actor(em, "rifle-1", false);
            Entity wall = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey));
            em.SetComponentData(wall, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = 44, OwnerFactionId = 2, OriginCell = new Unity.Mathematics.int2(20, 30) });
            em.SetComponentData(wall, new UnitSourcePrefabKey { Value = new FixedString64Bytes("wall") });
            em.AddComponentData(actor, new EngageTarget { Target = wall });
            em.AddComponentData(wall, new UnitHealth { Current = 17, Max = 120 });
            em.AddComponentData(wall, new BuildingResourceStorageComponent
            { RuntimeBuildingId = 44, StoredOilBarrels = 13.5f, ReservedOilOutboundBarrels = 2.5f });
            em.AddComponentData(wall, new MaterialFabricationComponent
            { RuntimeBuildingId = 44, CycleProgressSeconds = 15.3f, ProductionEnabled = 0 });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var snapshot, out var failure), failure);
            Assert.AreEqual("map/2/20/30/wall", snapshot.MapReferences[0].Id);
            var json = SkirmishNativeLegacyBa7Json.EncodeInner(snapshot);
            StringAssert.Contains("map/2/20/30/wall", json);
            Assert.AreEqual(44, snapshot.MapReferences[0].OldRuntimeBuildingId);
            Assert.IsTrue(em.Exists(wall));
            Assert.IsTrue(SkirmishNativeLegacyBa7Json.TryDecodeInner(json,out var decoded));
            Assert.IsNull(decoded.Objective);
            using var restoredWorld = new World("restored-map");
            var restored = restoredWorld.EntityManager;
            Entity newWall = restored.CreateEntity();
            var parts = SkirmishNativeCheckpointParts.RemapBuildingIds(
                decoded.MapReferences[0].Parts, new Dictionary<int, int>(), 144);
            SkirmishNativeCheckpointParts.Apply(restored, newWall, parts, _ => Entity.Null);
            Assert.AreEqual(17, restored.GetComponentData<UnitHealth>(newWall).Current);
            Assert.AreEqual(144, restored.GetComponentData<RuntimeBuildingCombatInfo>(newWall).RuntimeBuildingId);
            var storage = restored.GetComponentData<BuildingResourceStorageComponent>(newWall);
            Assert.AreEqual(144, storage.RuntimeBuildingId);
            Assert.AreEqual(13.5f, storage.StoredOilBarrels);
            Assert.AreEqual(2.5f, storage.ReservedOilOutboundBarrels);
            var fabrication = restored.GetComponentData<MaterialFabricationComponent>(newWall);
            Assert.AreEqual(144, fabrication.RuntimeBuildingId);
            Assert.AreEqual(15.3f, fabrication.CycleProgressSeconds);
            Assert.AreEqual(0, fabrication.ProductionEnabled);
        }

        [Test]
        public void RuntimeBuildingRemapPreservesPaidReservationsAndHaulState()
        {
            using var world = new World(nameof(RuntimeBuildingRemapPreservesPaidReservationsAndHaulState));
            var em = world.EntityManager;
            Entity actor = em.CreateEntity();
            em.AddComponentData(actor, new UnitResourceHaulOrder
            { SourceBuildingId = 5, DestinationBuildingId = 6, ActionEndsAt = 19, Phase = 2 });
            em.AddComponentData(actor, new UnitResourceHaulReservation
            { SourceBuildingId = 5, DestinationBuildingId = 6, ReservedBarrels = 4.5f, ReservationId = 7 });
            em.AddBuffer<SkirmishProductionReservation>(actor).Add(new SkirmishProductionReservation
            { ProducerRuntimeId = 5, ReservationId = 10, MaterialsPaid = 75, RemainingMembers = 3 });
            var original = SkirmishNativeCheckpointParts.Capture(em, actor, _ => null);
            var remapped = SkirmishNativeCheckpointParts.RemapBuildingIds(original,
                new Dictionary<int, int> { [5] = 105, [6] = 106 });
            Entity destination = em.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(em, destination, remapped, _ => Entity.Null);
            Assert.AreEqual(105, em.GetComponentData<UnitResourceHaulOrder>(destination).SourceBuildingId);
            Assert.AreEqual(106, em.GetComponentData<UnitResourceHaulOrder>(destination).DestinationBuildingId);
            Assert.AreEqual(19, em.GetComponentData<UnitResourceHaulOrder>(destination).ActionEndsAt);
            Assert.AreEqual(4.5f, em.GetComponentData<UnitResourceHaulReservation>(destination).ReservedBarrels);
            var paid = em.GetBuffer<SkirmishProductionReservation>(destination)[0];
            Assert.AreEqual(105, paid.ProducerRuntimeId);
            Assert.AreEqual(75, paid.MaterialsPaid);
            Assert.AreEqual(3, paid.RemainingMembers);
            Assert.AreEqual(5, em.GetComponentData<UnitResourceHaulOrder>(actor).SourceBuildingId);
            Assert.Throws<ArgumentException>(() => SkirmishNativeCheckpointParts.RemapBuildingIds(original,
                new Dictionary<int, int> { [5] = 105 }));
            Assert.AreEqual(106, em.GetComponentData<UnitResourceHaulOrder>(destination).DestinationBuildingId);
        }

        private static Entity Session(EntityManager em)
        {
            Entity session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent));
            em.SetComponentData(session, new SkirmishExpandedSessionComponent
            { ObjectiveKind=Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault, SessionId = new FixedString64Bytes("snapshot-fixture"), CatalogId = new FixedString64Bytes("S005") });
            return session;
        }

        private static Entity Actor(EntityManager em, string id, bool structure)
        {
            Entity actor = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishSharedActorTag), typeof(UnitSourcePrefabKey));
            em.SetComponentData(actor, new SkirmishAttemptOwnedComponent
            { SessionId = new FixedString64Bytes("snapshot-fixture"), StableObjectId = new FixedString64Bytes(id), IsStructure = (byte)(structure ? 1 : 0) });
            em.SetComponentData(actor, new UnitSourcePrefabKey { Value = new FixedString64Bytes("native-prefab") });
            return actor;
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointSnapshotTests();
            suite.NativeStructureCaptureRequiresItsAuthoritativeProductionOwner();
            suite.NativeConstructionCommitMakesBothFabricationActorsCheckpointable();
            suite.UnmappedCombatReferenceRejectsCaptureWithoutPartialSnapshot();
            suite.MapReferencesUseSourceFootprintsInsteadOfCrossWorldHandles();
            suite.RuntimeBuildingRemapPreservesPaidReservationsAndHaulState();
            suite.DestroyedCampaignSourceReplaysRubbleThroughFreshPresentationOwners();
            suite.DenseWallsSharingGridCellRetainDistinctPersistentSourceIdentities();
            Debug.Log("[SkirmishNativeCheckpointSnapshotTests] result=Passed tests=7 nativeRecoveryComplete=0");
        }

        [Test]
        public void DenseWallsSharingGridCellRetainDistinctPersistentSourceIdentities()
        {
            using var world = new World("same-cell-authored-walls");
            var em = world.EntityManager;
            Entity session = Session(em);
            Entity actor = Actor(em, "rifle-1", false);
            var walls = new Entity[2];
            for (int i = 0; i < walls.Length; i++)
            {
                walls[i] = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey), typeof(OperationMapBuildingIdentity));
                em.SetComponentData(walls[i], new RuntimeBuildingCombatInfo
                    { RuntimeBuildingId = 44+i, OriginCell = new Unity.Mathematics.int2(926,373) });
                em.SetComponentData(walls[i], new UnitSourcePrefabKey { Value = "Wall_Dirt_Straight" });
                em.SetComponentData(walls[i], new OperationMapBuildingIdentity
                    { OperationMapId = "opmap.skirmish.desert_base_01", StableId = "wall."+i });
            }
            em.AddComponentData(actor, new EngageTarget { Target = walls[1] });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "unit", out var saved, out var failure), failure);
            Assert.AreEqual(2, saved.MapReferences.Length);
            Assert.AreNotEqual(saved.MapReferences[0].Id, saved.MapReferences[1].Id);
            using var fresh = new World("fresh-source-aliases");
            foreach (var wall in walls)
            {
                var recreated = fresh.EntityManager.CreateEntity(typeof(OperationMapBuildingIdentity));
                fresh.EntityManager.SetComponentData(recreated, em.GetComponentData<OperationMapBuildingIdentity>(wall));
                var info = em.GetComponentData<RuntimeBuildingCombatInfo>(wall); info.RuntimeBuildingId += 100;
                string alias = SkirmishNativeCheckpointSnapshotService.SourceMapIdentity(fresh.EntityManager, recreated, info, "Wall_Dirt_Straight");
                Assert.That(saved.MapReferences, Has.Some.Matches<SkirmishNativeCheckpointSnapshotService.MapReference>(x => x.Id == alias));
            }
            em.SetComponentData(walls[1], em.GetComponentData<OperationMapBuildingIdentity>(walls[0]));
            Assert.IsFalse(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "unit", out _, out failure));
            StringAssert.StartsWith("MapReferenceAmbiguous:", failure);
        }

        [Test]
        public void DestroyedCampaignSourceReplaysRubbleThroughFreshPresentationOwners()
        {
            foreach (bool virtualized in new[] { false, true })
            {
                using var source = new World("destroyed-source-checkpoint");
                var em = source.EntityManager;
                Entity session = Session(em);
                Actor(em, "rifle-1", false);
                Entity original = SourceBuilding(em, 44, 0, virtualized, 7, 2f, out _, out _);
                RunDestruction(source);
                Assert.IsTrue(em.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(original));
                Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "unit", out var saved, out var failure), failure);
                Assert.IsTrue(SkirmishNativeLegacyBa7Json.TryDecodeInner(SkirmishNativeLegacyBa7Json.EncodeInner(saved),out saved));
                Assert.IsNull(saved.Objective);
                Assert.AreEqual(1, saved.MapReferences.Length, "Rubble stays in the physical source census.");

                using var fresh = new World("fresh-destroyed-source-presentation");
                var restored = fresh.EntityManager;
                for (int i = 0; i < 8; i++) restored.CreateEntity();
                Entity rebuilt = SourceBuilding(restored, 144, 100, virtualized, 17, 3f, out var intact, out var rubble);
                Entity freshBuffer = Entity.Null;
                if (virtualized)
                {
                    using var buffers = restored.CreateEntityQuery(typeof(OperationMapRenderStateChangeSequenceComponent));
                    freshBuffer = buffers.GetSingletonEntity();
                    restored.SetComponentData(freshBuffer, new OperationMapRenderStateChangeSequenceComponent { LastPublishedVersion = 100 });
                }
                int count = restored.UniversalQuery.CalculateEntityCount();
                var parts = SkirmishNativeCheckpointParts.RemapBuildingIds(saved.MapReferences[0].Parts, new Dictionary<int, int>(), 144);
                SkirmishNativeCheckpointParts.Apply(restored, rebuilt, parts, _ => Entity.Null);
                RunDestruction(fresh);
                RunDestruction(fresh);
                Assert.AreEqual(0, restored.GetComponentData<UnitHealth>(rebuilt).Current);
                Assert.AreEqual(144, restored.GetComponentData<RuntimeBuildingCombatInfo>(rebuilt).RuntimeBuildingId);
                Assert.IsTrue(restored.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(rebuilt));
                Assert.IsTrue(restored.HasComponent<StaticGridBlocker>(rebuilt));
                Assert.AreEqual(count, restored.UniversalQuery.CalculateEntityCount());
                if (virtualized)
                {
                    var changes = restored.GetBuffer<OperationMapRenderStateChangeComponent>(freshBuffer);
                    Assert.AreEqual(1, changes.Length, "Recovery publishes rubble once through the fresh map's rendering buffer.");
                    Assert.AreEqual(17, changes[0].StateOwnerIndex, "The previous world's renderer index must not cross the save boundary.");
                    Assert.AreEqual(101u, changes[0].ChangeVersion);
                    Assert.AreEqual(OperationMapRenderVisualState.Destroyed, changes[0].VisualState);
                }
                else
                {
                    Assert.AreEqual(0f, restored.GetComponentData<LocalTransform>(intact).Scale);
                    Assert.AreEqual(3f, restored.GetComponentData<LocalTransform>(rubble).Scale, "Use the fresh hierarchy's authored rubble scale.");
                    Assert.AreEqual(1, restored.GetComponentData<OperationMapBuildingPresentation>(rebuilt).State);
                }
            }
        }

        private static Entity SourceBuilding(EntityManager em, int runtimeId, int health, bool virtualized,
            int stateOwner, float rubbleScale, out Entity intact, out Entity rubble)
        {
            Entity building = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey), typeof(UnitHealth),
                typeof(StaticGridBlocker), typeof(OperationMapBuildingComponent), typeof(OperationMapBuildingDestroyedComponent));
            em.SetComponentData(building, new RuntimeBuildingCombatInfo { RuntimeBuildingId = runtimeId, OwnerFactionId = 0, OriginCell = new Unity.Mathematics.int2(20, 30) });
            em.SetComponentData(building, new UnitSourcePrefabKey { Value = new FixedString64Bytes("campaign-town") });
            em.SetComponentData(building, new UnitHealth { Current = health, Max = 100 });
            em.SetComponentData(building, new OperationMapBuildingComponent { BlockerPolicy = OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked });
            em.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(building, false);
            intact = rubble = Entity.Null;
            if (virtualized)
            {
                em.AddComponentData(building, new OperationMapVirtualizedBuildingPresentationComponent { StateOwnerIndex = stateOwner });
                Entity buffer = em.CreateEntity(typeof(OperationMapRenderStateChangeSequenceComponent));
                em.AddBuffer<OperationMapRenderStateChangeComponent>(buffer);
            }
            else
            {
                intact = em.CreateEntity(typeof(LocalTransform)); rubble = em.CreateEntity(typeof(LocalTransform));
                em.SetComponentData(intact, LocalTransform.Identity); em.SetComponentData(rubble, LocalTransform.Identity);
                em.AddComponentData(building, new OperationMapBuildingPresentation { IntactVisualRoot = intact, DestroyedVisualRoot = rubble,
                    IntactVisibleScale = 1f, DestroyedVisibleScale = rubbleScale, State = byte.MaxValue });
            }
            return building;
        }

        private static void RunDestruction(World world)
        {
            var handle = world.CreateSystem<OperationMapBuildingDestructionSystem>();
            ref var state = ref world.Unmanaged.ResolveSystemStateRef(handle);
            world.Unmanaged.GetUnsafeSystemRef<OperationMapBuildingDestructionSystem>(handle).OnUpdate(ref state);
            state.Dependency.Complete();
            world.EntityManager.CompleteAllTrackedJobs();
            world.DestroySystem(handle);
        }
    }
}
