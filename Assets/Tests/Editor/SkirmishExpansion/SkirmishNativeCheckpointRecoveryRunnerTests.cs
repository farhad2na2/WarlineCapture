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
    public sealed class SkirmishNativeCheckpointRecoveryRunnerTests
    {
        [Test]
        public void NativeStagesWaitForOwnersThenRestoreWithoutStartingForceDuplicates()
        {
            using var source = new World("runner-source");
            var saved = SkirmishNativeCheckpointStructureReconstructionTests.Capture(source.EntityManager, out var setup);
            using var sourceSessions = source.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity originalSession = sourceSessions.GetSingletonEntity();
            var state = source.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(originalSession);
            state.Phase = SkirmishSessionPhase.Playing; state.SpawnComplete = 1; state.InitializationComplete = 1;
            source.EntityManager.SetComponentData(originalSession, state);
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(source.EntityManager, originalSession,
                _ => "prefab", out saved, out var failure), failure);
            using var destination = new World("runner-destination");
            var em = destination.EntityManager;
            Entity session = SkirmishNativeCheckpointStructureReconstructionTests.Fresh(em, out var boundary);
            em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("Building_Barrack"), FootprintCells = new int2(2, 2) });
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryRunner.TryQueue(em, session, saved, setup, _ => null));
            Assert.IsFalse(SkirmishNativeCheckpointRecoveryRunner.TryQueue(em, session, saved, setup, _ => null));
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            var record = em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(session);
            Assert.IsFalse(record.Complete);
            Assert.AreEqual(1, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            Entity building = em.CreateEntity();
            em.AddComponentData(building, new RuntimeBuildingCombatInfo
            { RuntimeBuildingId = 105, OwnerFactionId = 1, OriginCell = new int2(15, 23), FootprintCells = new int2(2, 2) });
            em.AddComponentData(building, new UnitHealth { Current = 800, Max = 800 });
            var owner = new SkirmishNativeBuildingCheckpointRecord
            { Building = new RuntimeBuildingEntity { Id = 105, CombatEntity = building } };
            em.AddComponentObject(building, owner);
            var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
            var request = requests[0]; request.Status = BuildingRuntimeSpawnRequest.Succeeded;
            request.BuildingRuntimeId = 105; requests[0] = request;
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            Assert.IsFalse(record.Complete, "Wait for the authoritative production helper, not a placeholder owner.");
            using var presentation = new ProductionScope();
            presentation.Helper.BindCheckpointProductionOwner(default, owner.Building, owner);
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            Assert.IsTrue(record.Complete, record.Failure);
            Assert.IsNull(record.Failure);
            Assert.AreEqual(500, em.GetComponentData<UnitHealth>(building).Current);
            Assert.AreEqual(105, em.GetComponentData<RuntimeBuildingCombatInfo>(building).RuntimeBuildingId);
            Assert.AreEqual(SkirmishSessionPhase.Playing, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            using var actors = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            Assert.AreEqual(2, actors.CalculateEntityCount());
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            Assert.AreEqual(2, actors.CalculateEntityCount(), "Completed recovery is idempotent.");
            Assert.IsFalse(em.HasComponent<BuildingStartingGrantOwner>(session));
            Assert.IsFalse(em.HasComponent<SkirmishNativeCheckpointStructureReconstructionRecord>(session));
            Assert.IsTrue(em.HasComponent<SkirmishSharedBuildingsReady>(session));
        }

        [Test]
        public void FailedNativePreflightNeverFallsBackToStartingForceGrant()
        {
            using var source = new World("runner-reject-source");
            var saved = SkirmishNativeCheckpointStructureReconstructionTests.Capture(source.EntityManager, out var setup);
            using var sourceSessions = source.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity original = sourceSessions.GetSingletonEntity();
            var state = source.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(original);
            state.Phase = SkirmishSessionPhase.Playing; state.SpawnComplete = 1;
            source.EntityManager.SetComponentData(original, state);
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(source.EntityManager, original, _ => "prefab", out saved, out _));
            using var destination = new World("runner-reject-destination");
            var em = destination.EntityManager;
            Entity session = SkirmishNativeCheckpointStructureReconstructionTests.Fresh(em, out var boundary);
            em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary).Add(new BuildingConfiguredSpawnableReadModel
            { BuildingId = new FixedString128Bytes("unrelated-building"), FootprintCells = new int2(1, 1) });
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryRunner.TryQueue(em, session, saved, setup, _ => null));
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            Assert.AreEqual(SkirmishSessionPhase.Failed, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
            Assert.AreEqual(0, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            using var actors = em.CreateEntityQuery(new EntityQueryDesc
            { All = new[] { ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>() }, Options = EntityQueryOptions.IncludeDisabledEntities });
            Assert.AreEqual(0, actors.CalculateEntityCount());
            Assert.IsFalse(em.HasComponent<BuildingStartingGrantOwner>(session));
        }

        private sealed class ProductionScope : System.IDisposable
        {
            internal readonly BuildingProductionTransportPresentationSystemHelper Helper = new();
            public void Dispose() => Helper.Dispose();
        }

        [Test]
        public void IncompleteStreamedSourceCensusWaitsThenFailsClosedOnTimeout()
        {
            using var source = new World("runner-stream-source");
            SkirmishNativeCheckpointCodecTests.Capture(source.EntityManager, out var setup, out _);
            var em = source.EntityManager;
            Entity wall = em.CreateEntity();
            em.AddComponentData(wall, new RuntimeBuildingCombatInfo { RuntimeBuildingId = 7, OwnerFactionId = 0 });
            em.AddComponentData(wall, new UnitSourcePrefabKey { Value = new FixedString64Bytes("wall") });
            using var sourceSessions = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent));
            Entity original = sourceSessions.GetSingletonEntity();
            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(original);
            state.Phase = SkirmishSessionPhase.Playing; state.SpawnComplete = 1;
            em.SetComponentData(original, state);
            Assert.IsTrue(SkirmishNativeCheckpointSnapshotService.TryCapture(em, original, _ => "prefab", out var saved, out _));
            using var destination = new World("runner-stream-destination");
            em = destination.EntityManager;
            Entity session = SkirmishNativeCheckpointStructureReconstructionTests.Fresh(em, out var boundary);
            Assert.IsTrue(SkirmishNativeCheckpointRecoveryRunner.TryQueue(em, session, saved, setup, _ => null));
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            var record = em.GetComponentObject<SkirmishNativeCheckpointRecoveryRecord>(session);
            Assert.IsNull(record.Failure);
            Assert.IsFalse(record.StructuresRequested);
            Assert.AreEqual(0, em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).Length);
            record.Deadline = -1;
            SkirmishNativeCheckpointRecoveryRunner.Step(em, session, setup);
            StringAssert.StartsWith("NativeRecoveryTimedOut:NativePhysicalMapCensusIncomplete", record.Failure);
            Assert.AreEqual(SkirmishSessionPhase.Failed, em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase);
        }

        public static void RunFocusedValidation()
        {
            var suite = new SkirmishNativeCheckpointRecoveryRunnerTests();
            suite.NativeStagesWaitForOwnersThenRestoreWithoutStartingForceDuplicates();
            suite.FailedNativePreflightNeverFallsBackToStartingForceGrant();
            suite.IncompleteStreamedSourceCensusWaitsThenFailsClosedOnTimeout();
            Debug.Log("[SkirmishNativeCheckpointRecoveryRunnerTests] result=Passed tests=3 nativeRecoveredSessions=0");
        }
    }
}
