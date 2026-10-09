using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishNativeCheckpointStructureReconstructionTests
    {
        [Test]
        public void PreflightAllStructureDefinitionsBeforeSubmittingAnyNativeRequest()
        {
            using var source = new World("structure-recovery-source");
            var saved = Capture(source.EntityManager, out var setup);
            using var destination = new World("structure-recovery-destination");
            var em = destination.EntityManager;
            Entity session = Fresh(em, out var boundary);
            Assert.IsFalse(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out var failure));
            Assert.AreEqual("NativeStructureOwnerNotReady", failure);
            em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("unrelated-building"), FootprintCells = new int2(1, 1) });
            Assert.IsFalse(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out failure));
            StringAssert.Contains("PrefabOrFootprintMissing", failure);
            Assert.AreEqual(0, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            Assert.IsFalse(em.HasComponent<BuildingStartingGrantOwner>(session));
            em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("Building_Barrack"), FootprintCells = new int2(3, 3) });
            Assert.IsFalse(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out failure));
            StringAssert.StartsWith("NativeStructureFootprintNotReady:", failure);
            Assert.AreEqual(0, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            var configured = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            configured[1] = new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("Building_Barrack"), FootprintCells = new int2(2, 2) };
            Assert.IsTrue(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out failure), failure);
            var request = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary)[0];
            Assert.AreEqual(new int2(15, 23), request.PreferredOrigin);
            Assert.AreEqual(1, request.RequirePreferredOrigin);
            Assert.AreEqual(1, request.AuthoredStartingGrant);
            Assert.AreEqual(session, request.PlanEntity);
            Assert.IsTrue(SkirmishNativeCheckpointStructureReconstruction.TryBindCompleted(em, session, out var complete, out failure), failure);
            Assert.IsFalse(complete);
            SkirmishNativeCheckpointStructureReconstruction.Cancel(em, session);
            Assert.AreEqual(BuildingRuntimeSpawnRequest.Failed, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary)[0].Status);
            Assert.IsFalse(em.HasComponent<BuildingStartingGrantOwner>(session));
        }

        [Test]
        public void CompletedRequestRequiresFunctionalOwnerAndCannotClaimCollidingSourceBuilding()
        {
            using var source = new World("structure-bind-source");
            var saved = Capture(source.EntityManager, out var setup);
            using var destination = new World("structure-bind-destination");
            var em = destination.EntityManager;
            Entity session = Fresh(em, out var boundary);
            em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("Building_Barrack"), FootprintCells = new int2(2, 2) });
            Assert.IsTrue(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out var failure), failure);
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var result = requests[0];
            result.Status = BuildingRuntimeSpawnRequest.Succeeded; result.BuildingRuntimeId = 105; requests[0] = result;
            Entity scenery = em.CreateEntity(typeof(OperationMapBuildingComponent));
            em.AddComponentData(scenery, new RuntimeBuildingCombatInfo { RuntimeBuildingId = 105 });
            em.AddComponentData(scenery, new UnitHealth { Current = 999, Max = 999 });
            Entity building = em.CreateEntity();
            em.AddComponentData(building, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = 105, OwnerFactionId = 1, OriginCell = new int2(15, 23), FootprintCells = new int2(2, 2) });
            em.AddComponentData(building, new UnitHealth { Current = 800, Max = 800 });
            Assert.IsFalse(SkirmishNativeCheckpointStructureReconstruction.TryBindCompleted(em, session, out _, out failure));
            StringAssert.Contains("OwnerMissing", failure);
            em.AddComponentObject(building, new SkirmishNativeBuildingCheckpointRecord
            { Building = new RuntimeBuildingEntity { Id = 105, CombatEntity = building } });
            Assert.IsTrue(SkirmishNativeCheckpointStructureReconstruction.TryBindCompleted(em, session, out var complete, out failure), failure);
            Assert.IsTrue(complete);
            Assert.AreEqual("original-base", em.GetComponentData<SkirmishAttemptOwnedComponent>(building).StableObjectId.ToString());
            Assert.IsFalse(em.HasComponent<SkirmishAttemptOwnedComponent>(scenery));
            Assert.IsFalse(em.HasComponent<BuildingStartingGrantOwner>(session));
            SkirmishNativeCheckpointStructureReconstruction.Cancel(em, session);
            Assert.AreEqual(0, em.GetComponentData<UnitHealth>(building).Current);
            Assert.AreEqual(999, em.GetComponentData<UnitHealth>(scenery).Current);
        }

        [Test]
        public void PersistentSourceCensusAcceptsSameCellWallsAndRejectsRealDuplicates()
        {
            using var source = new World("persistent-source-census");
            Capture(source.EntityManager, out var setup);
            for (int i = 0; i < 2; i++) AddSourceWall(source.EntityManager, i, 44 + i);
            using var sourceSessions = source.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(source.EntityManager,
                sourceSessions.GetSingletonEntity(), _ => "prefab", out var saved, out var captureFailure), captureFailure);
            Assert.IsTrue(SkirmishNativeCheckpointCodec.Validate(saved, setup));

            using var destination = new World("fresh-persistent-source-census");
            var em = destination.EntityManager;
            var session = Fresh(em, out var boundary);
            AddSourceWall(em, 0, 144);
            var second = AddSourceWall(em, 1, 145);
            em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = "Building_Barrack", FootprintCells = new int2(2, 2) });
            var identity = em.GetComponentData<OperationMapBuildingIdentity>(second);
            identity.StableId = "wall.0";
            em.SetComponentData(second, identity);
            Assert.IsFalse(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out var failure));
            Assert.AreEqual("NativePhysicalMapAmbiguous", failure);
            Assert.AreEqual(0, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            identity.StableId = "different-wall";
            em.SetComponentData(second, identity);
            Assert.IsFalse(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out failure));
            Assert.AreEqual("NativePhysicalMapFootprintMissing", failure);
            identity.StableId = "wall.1";
            em.SetComponentData(second, identity);
            Assert.IsTrue(SkirmishNativeCheckpointStructureReconstruction.TryBegin(em, session, saved, setup, out failure), failure);
            Assert.AreEqual(1, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
        }

        private static Entity AddSourceWall(EntityManager em, int index, int runtimeId)
        {
            var wall = em.CreateEntity(typeof(RuntimeBuildingCombatInfo), typeof(UnitSourcePrefabKey), typeof(OperationMapBuildingIdentity));
            em.SetComponentData(wall, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = runtimeId, OriginCell = new int2(30, 30), FootprintCells = new int2(1, 1) });
            em.SetComponentData(wall, new UnitSourcePrefabKey { Value = "Wall_Dirt_Straight" });
            em.SetComponentData(wall, new OperationMapBuildingIdentity
            { OperationMapId = "opmap.skirmish.desert_base_01", StableId = "wall." + index });
            return wall;
        }

        internal static SkirmishNativeCheckpointSnapshotService.Snapshot Capture(EntityManager em, out Game.Configs.SkirmishResolvedSetup setup)
        {
            SkirmishNativeCheckpointCodecTests.Capture(em, out setup, out var unit);
            em.AddComponentData(unit, new SkirmishUnitRoleComponent { Role = SkirmishRoleKind.Rifle });
            em.AddComponentData(unit, LocalTransform.FromPosition(float3.zero));
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity session = sessions.GetSingletonEntity();
            Entity building = em.CreateEntity(typeof(SkirmishSharedActorTag));
            em.AddComponentData(building, new SkirmishAttemptOwnedComponent
            { SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
              StableObjectId = new FixedString64Bytes("original-base"), IsStructure = 1, FactionId = 1 });
            em.AddComponentData(building, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Building_Barrack") });
            em.AddComponentData(building, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = 5, OwnerFactionId = 1, OriginCell = new int2(15, 23), FootprintCells = new int2(2, 2) });
            em.AddComponentData(building, new UnitHealth { Current = 500, Max = 800 });
            em.AddComponentObject(building, new SkirmishNativeBuildingCheckpointRecord
            { Building = new RuntimeBuildingEntity { Id = 5, CombatEntity = building } });
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, session, _ => "prefab", out var saved, out var failure), failure);
            return saved;
        }

        internal static Entity Fresh(EntityManager em, out Entity boundary)
        {
            SkirmishNativeCheckpointCodecTests.Capture(em, out _, out var unit);
            em.DestroyEntity(unit);
            using var sessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity session = sessions.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            state.Phase = SkirmishSessionPhase.Spawning; em.SetComponentData(session, state);
            Entity prefab = em.CreateEntity(typeof(Prefab));
            em.AddComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("rifle") });
            Entity registry = em.CreateEntity();
            em.AddBuffer<UnitPrefabRegistryEntry>(registry).Add(new UnitPrefabRegistryEntry { Prefab = prefab });
            boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
            em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
            em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
            return session;
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointStructureReconstructionTests();
            suite.PreflightAllStructureDefinitionsBeforeSubmittingAnyNativeRequest();
            suite.CompletedRequestRequiresFunctionalOwnerAndCannotClaimCollidingSourceBuilding();
            suite.PersistentSourceCensusAcceptsSameCellWallsAndRejectsRealDuplicates();
            Debug.Log("[SkirmishNativeCheckpointStructureReconstructionTests] result=Passed tests=3 nativeRecoveredSessions=0");
        }
    }
}
