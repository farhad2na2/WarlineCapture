#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingPlacementProductionPlayModeTests
{
    [Test]
    public void BuildDrawerPlacementThenProduction_UsesRuntimeBoundaryData()
    {
        using var world = new World("BuildDrawerPlacementThenProduction_UsesRuntimeBoundaryData");
        EntityManager em = world.EntityManager;
        var requestBoundary = new BuildingProductionRequestSystemHelper();
        var productionSystem = new BuildingProductionQueueCompositionSystemHelper();
        GameObject buildingPrefab = new("PlayMode Barracks");
        GameObject unitPrefab = new("PlayMode Rifleman");

        try
        {
            RuntimeBuildingEntity producer = CreateProducerBuilding(
                id: 11,
                displayName: "PlayMode Barracks",
                unitPrefab,
                ownerFactionId: FactionIdentity.PlayerFactionId);
            var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
            {
                [producer.Id] = producer
            };
            BuildingDefinition placementDefinition = new()
            {
                DisplayName = "PlayMode Barracks",
                Prefab = buildingPrefab
            };

            bool beganPlacement = false;
            int activePlacementCost = -1;
            int spentDollars = 0;
            int selectedBuildingId = 0;
            BuildingProductionRequestSystemHelper.Context context = CreateRequestContext(
                runtimeBuildings,
                placementDefinition,
                buildingPrefab,
                unitPrefab,
                productionSystem,
                em,
                requestPrefab =>
                {
                    beganPlacement = requestPrefab == buildingPrefab;
                    return true;
                },
                amount =>
                {
                    spentDollars += amount;
                    return true;
                },
                cost => activePlacementCost = cost,
                buildingId => selectedBuildingId = buildingId);

            int placementRequestId = requestBoundary.EnqueueCampItemRequest(
                em,
                buildingPrefab,
                price: 300,
                focusProducerOnSuccess: true);
            requestBoundary.ProcessPendingUiCampItemCommands(em, context, frameCount: 10);

            Assert.IsTrue(requestBoundary.TryGetUiCampItemCommandResult(
                em,
                placementRequestId,
                out BuildingUiCampItemCommandResultElement placementResult));
            Assert.AreEqual(1, placementResult.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.PlacementStarted, placementResult.ResultCode);
            Assert.AreEqual(300, activePlacementCost);
            Assert.IsTrue(beganPlacement);
            Assert.AreEqual(0, producer.PendingProductions.Count);

            int productionRequestId = requestBoundary.EnqueueCampItemRequest(
                em,
                unitPrefab,
                price: 125,
                focusProducerOnSuccess: true);
            requestBoundary.ProcessPendingUiCampItemCommands(em, context, frameCount: 11);

            Assert.IsTrue(requestBoundary.TryGetUiCampItemCommandResult(
                em,
                productionRequestId,
                out BuildingUiCampItemCommandResultElement productionResult));
            Assert.AreEqual(1, productionResult.Accepted);
            Assert.AreEqual(BuildingUiCampItemCommandResultElement.ProductionQueued, productionResult.ResultCode);
            Assert.AreEqual(125, spentDollars);
            Assert.AreEqual(producer.Id, selectedBuildingId);
            Assert.AreEqual(1, producer.PendingProductions.Count);
            Assert.AreSame(unitPrefab, producer.PendingProductions[0].Prefab);
            Assert.AreEqual(0, producer.PendingProductions[0].ProductionIndex);
        }
        finally
        {
            Object.DestroyImmediate(unitPrefab);
            Object.DestroyImmediate(buildingPrefab);
        }
    }

    private static RuntimeBuildingEntity CreateProducerBuilding(
        int id,
        string displayName,
        GameObject unitPrefab,
        byte ownerFactionId)
    {
        return new RuntimeBuildingEntity
        {
            Id = id,
            HasOwnerFaction = true,
            OwnerFactionId = ownerFactionId,
            Definition = new BuildingDefinition
            {
                DisplayName = displayName,
                ProductionSlots = new List<BuildingDefinition.ProductionSlotDefinition>
                {
                    new() { SpawnUnitPrefab = unitPrefab }
                }
            },
            ProducedUnits = new List<Entity>(),
            ProducedUnitPrefabs = new Dictionary<Entity, GameObject>(),
            PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>()
        };
    }

    private static BuildingProductionRequestSystemHelper.Context CreateRequestContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        BuildingDefinition placementDefinition,
        GameObject buildingPrefab,
        GameObject unitPrefab,
        BuildingProductionQueueCompositionSystemHelper productionSystem,
        EntityManager em,
        BuildingProductionRequestSystemHelper.BeginPlacementForConfiguredSpawnableDelegate beginPlacement,
        BuildingProductionRequestSystemHelper.TrySpendDollarsDelegate trySpendDollars,
        BuildingProductionRequestSystemHelper.SetActivePlacementCostDelegate setActivePlacementCost,
        BuildingProductionRequestSystemHelper.SelectRuntimeBuildingDelegate selectRuntimeBuilding)
    {
        var unitPrefabs = new List<GameObject> { unitPrefab };
        var unitPrefabsByKey = new Dictionary<string, GameObject>();
        BuildingProductionQueueCompositionSystemHelper.QueueContext queueContext = new(
            unitPrefabs,
            unitPrefabsByKey,
            new BuildingProductionSlotUtilitySystemHelper(),
            null,
            null);

        return new BuildingProductionRequestSystemHelper.Context(
            runtimeBuildings,
            new List<BuildingDefinition> { placementDefinition },
            new Dictionary<GameObject, BuildingDefinition> { [buildingPrefab] = placementDefinition },
            unitPrefabs,
            unitPrefabsByKey,
            10000,
            25,
            productionSystem,
            queueContext,
            null,
            BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
            null,
            beginPlacement,
            trySpendDollars,
            _ => { },
            setActivePlacementCost,
            (building, productionIndex, spawnUnitPrefab) => productionSystem.TryQueuePlayerUnitFromBuilding(
                queueContext,
                building,
                productionIndex,
                spawnUnitPrefab,
                em,
                now: 2f),
            selectRuntimeBuilding,
            () => { },
            () => { },
            () => { },
            _ => { },
            _ => Vector3.zero,
            _ => { },
            Debug.LogWarning,
            (_, _) => 0,
            (_, _) => 0);
    }
}
#endif
