using Game.Components;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class OperationsScenarioSourceBindingTests
{
    [Test]
    public void ReplayResetsFinishedStartBoundaryButPreservesRequestSequence()
    {
        using var world = new World("Operations replay boundary");
        var em = world.EntityManager;
        var boundary = em.CreateEntity(typeof(MatchStartQueueComponent), typeof(MatchStartProgressComponent));
        em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = 8, HasStarted = 1, LastStatus = MatchStartStatusKind.Started });
        em.AddBuffer<MatchStartRequestElement>(boundary).Add(new MatchStartRequestElement { RequestId = 8 });
        em.AddBuffer<MatchStartResultElement>(boundary).Add(new MatchStartResultElement { RequestId = 8, Status = MatchStartStatusKind.Started });
        var content = Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
        Assert.IsTrue(OperationsReconLaunchProjection.TryQueue(em, content, "session.operations.aabb", out var error), error);
        var state = em.GetComponentData<MatchStartQueueComponent>(boundary);
        Assert.That(state.LastRequestId, Is.EqualTo(8));
        Assert.That(state.HasStarted, Is.Zero);
        Assert.That(state.LastStatus, Is.EqualTo(MatchStartStatusKind.None));
        Assert.That(em.GetBuffer<MatchStartRequestElement>(boundary).Length, Is.Zero);
        Assert.That(em.GetBuffer<MatchStartResultElement>(boundary).Length, Is.Zero);
        Assert.IsFalse(OperationsReconLaunchProjection.TryQueue(em, content, "session.operations.ccdd", out _));
    }

    [Test]
    public void PendingOtherLaunchCannotBeResetByOperations()
    {
        using var world = new World("Operations conflicting launch");
        var em = world.EntityManager;
        var boundary = em.CreateEntity(typeof(MatchStartQueueComponent));
        em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = 8, IsStartPending = 1 });
        var content = Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
        Assert.IsFalse(OperationsReconLaunchProjection.TryQueue(em, content, "session.operations.aabb", out _));
        Assert.That(em.GetComponentData<MatchStartQueueComponent>(boundary).IsStartPending, Is.EqualTo(1));
        using var operations = em.CreateEntityQuery(typeof(OperationsReconMissionComponent));
        Assert.That(operations.IsEmptyIgnoreFilter, Is.True);
    }

    [Test]
    public void PreparingMapNeutralizesInheritedOwnershipWithoutChangingMissionForce()
    {
        using var world = new World("Operations inherited scenery");
        var em = world.EntityManager;
        var building = em.CreateEntity(typeof(OperationMapBuildingComponent), typeof(Faction), typeof(RuntimeBuildingCombatInfo), typeof(BuildingResourceStorageComponent), typeof(AIControlledTag));
        em.SetComponentData(building, new Faction { Id = 2 });
        em.SetComponentData(building, new RuntimeBuildingCombatInfo { OwnerFactionId = 2 });
        em.SetComponentData(building, new BuildingResourceStorageComponent { OwnerFactionId = 2 });
        var infantry = em.CreateEntity(typeof(Faction));
        em.SetComponentData(infantry, new Faction { Id = 1 });
        var vehicle = em.CreateEntity(typeof(Faction), typeof(OperationMapAuthoredVehiclePresentation), typeof(UnitCombat), typeof(UnitRespawnPrefab));
        em.SetComponentData(vehicle, new Faction { Id = 2 });
        em.SetComponentData(vehicle, new OperationMapAuthoredVehiclePresentation { FactionId = 2 });
        em.SetComponentData(vehicle, new UnitCombat { CanAttack = 1, AutoEngage = 1 });
        var content = Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
        Assert.IsTrue(OperationsReconLaunchProjection.TryQueue(em, content, "session.operations.aabb", out var error), error);
        OperationsReconLaunchProjection.PrepareMap(em);
        Assert.That(em.GetComponentData<Faction>(building).Id, Is.Zero);
        Assert.That(em.GetComponentData<RuntimeBuildingCombatInfo>(building).OwnerFactionId, Is.Zero);
        Assert.That(em.GetComponentData<BuildingResourceStorageComponent>(building).OwnerFactionId, Is.Zero);
        Assert.That(em.HasComponent<AIControlledTag>(building), Is.False);
        Assert.That(em.GetComponentData<Faction>(infantry).Id, Is.EqualTo(1));
        Assert.That(em.GetComponentData<Faction>(vehicle).Id, Is.Zero);
        Assert.That(em.GetComponentData<OperationMapAuthoredVehiclePresentation>(vehicle).FactionId, Is.Zero);
        Assert.That(em.GetComponentData<UnitCombat>(vehicle).CanAttack, Is.Zero);
        Assert.That(em.HasComponent<UnitRespawnPrefab>(vehicle), Is.False);
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    public void ReusesOnlySelectedExactPhysicalSource(bool staleContent, bool wrongScenario, bool expected)
    {
        var preset = Resources.Load<OperationsReconMissionConfig>(OperationsReconMissionConfig.ResourcePath);
        Assert.NotNull(preset);
        var logical = Object.Instantiate(preset.operationMap);
        var physical = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
            "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset");
        using var world = new World("Operations source binding validation");
        BlobAssetReference<OperationMapBlob> blob = default;
        try
        {
            if (staleContent)
            {
                var so = new SerializedObject(logical);
                so.FindProperty("sourceBinding").FindPropertyRelative("sourceContentHash").stringValue = new string('a',64);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.IsTrue(logical.TryCreatePersistentMetadataBlob(out blob,out string error),error);
            var em = world.EntityManager;
            Assert.IsTrue(OperationsReconLaunchProjection.TryQueue(em, preset, "session.operations.aabb", out error), error);
            var root = em.CreateEntity(typeof(OperationMapRootComponent),typeof(ActiveOperationMapComponent),typeof(OperationMapMetadataComponent));
            em.SetComponentData(root,new ActiveOperationMapComponent
            {
                OperationMapId = new FixedString64Bytes(logical.OperationMapId),
                MissionId = new FixedString64Bytes(preset.missionId),
                ScenarioId = new FixedString64Bytes(wrongScenario ? "scenario.operations.wrong" : preset.scenarioId),
                Generation = 3, SchemaVersion = logical.SchemaVersion, ContentVersion = logical.ContentVersion
            });
            em.SetComponentData(root,new OperationMapMetadataComponent { Blob=blob,Generation=3 });
            bool accepted = CampaignMissionOperationMapReuseUtility.TryReuse(em,physical,out var resolved,out error);
            Assert.AreEqual(expected,accepted,error);
            Assert.AreEqual(expected ? root : Entity.Null,resolved);
            Assert.AreEqual(expected ? 1 : 0,em.GetComponentData<OperationMapMetadataComponent>(root).PhysicalSourceValidated);
        }
        finally { if(blob.IsCreated)blob.Dispose(); Object.DestroyImmediate(logical); }
    }
}
