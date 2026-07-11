using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class Aph806BuildingPlacementProductionPlayModeTests
{
    [Test]
    public void BuildingPlacementThenProduction_ProductionGatewaysCommitAndQueueExactlyOnce()
    {
        using var world = new World(nameof(BuildingPlacementThenProduction_ProductionGatewaysCommitAndQueueExactlyOnce));
        EntityManager em = world.EntityManager;
        GameObject buildingPrefab = new("APH806 Barracks");
        GameObject unitPrefab = new("APH806 Rifleman");
        GameObject placementRoot = new("APH806 Placement Root");

        try
        {
            bool placementCommitted = false;
            BuildingPlacementCommandRequestCompositionSystemHelper.Context placementContext =
                CreatePlacementContext(placementRoot.transform, placement => placementCommitted = placement.IsValid);
            BuildingPlacementSessionCompositionSystemHelper session = placementContext.SessionSystem;
            session.BeginPlacement(
                placementContext.SessionContext,
                new BuildingDefinition
                {
                    DisplayName = "APH806 Barracks",
                    Prefab = buildingPrefab,
                    FootprintCells = Vector2Int.one
                });
            session.SetActivePlacementCost(placementContext.SessionContext, 300);

            var placementGateway = new BuildingPlacementCommandRequestCompositionSystemHelper();
            int placementRequestId = placementGateway.EnqueueConfirmBuildingPlacement(em);
            placementGateway.ProcessPendingUiPlacementCommands(em, placementContext);

            Assert.That(
                placementGateway.TryGetUiPlacementCommandResult(em, placementRequestId, out var placementResult),
                Is.True);
            Assert.That(placementResult.Accepted, Is.EqualTo(1));
            Assert.That(placementResult.ResultCode, Is.EqualTo(BuildingUiPlacementCommandResultElement.Completed));
            Assert.That(placementCommitted, Is.True);

            RuntimeBuildingEntity producer = CreateProducer(unitPrefab);
            var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity> { [producer.Id] = producer };
            var productionQueue = new BuildingProductionQueueCompositionSystemHelper();
            BuildingProductionRequestSystemHelper.Context productionContext = CreateProductionContext(
                runtimeBuildings,
                buildingPrefab,
                unitPrefab,
                productionQueue,
                em);
            var productionGateway = new BuildingProductionRequestSystemHelper();

            int productionRequestId = productionGateway.EnqueueCampItemRequest(
                em,
                unitPrefab,
                price: 125,
                focusProducerOnSuccess: true);
            productionGateway.ProcessPendingUiCampItemCommands(em, productionContext, frameCount: 20);

            Assert.That(
                productionGateway.TryGetUiCampItemCommandResult(em, productionRequestId, out var productionResult),
                Is.True);
            Assert.That(productionResult.Accepted, Is.EqualTo(1));
            Assert.That(productionResult.ResultCode, Is.EqualTo(BuildingUiCampItemCommandResultElement.ProductionQueued));
            Assert.That(producer.PendingProductions.Count, Is.EqualTo(1));
            Assert.That(producer.PendingProductions[0].Prefab, Is.SameAs(unitPrefab));
            Assert.That(producer.PendingProductions[0].ProductionIndex, Is.EqualTo(0));
        }
        finally
        {
            Object.DestroyImmediate(placementRoot);
            Object.DestroyImmediate(unitPrefab);
            Object.DestroyImmediate(buildingPrefab);
        }
    }

    private static BuildingPlacementCommandRequestCompositionSystemHelper.Context CreatePlacementContext(
        Transform placementRoot,
        BuildingPlacementLifecycleCompositionSystemHelper.CommitPlacementDelegate commitPlacement)
    {
        var runtimeState = new RuntimeGameplayStateSystem();
        var lifecycle = new BuildingPlacementLifecycleCompositionSystemHelper();
        var session = new BuildingPlacementSessionCompositionSystemHelper();
        BuildingPlacementLifecycleCompositionSystemHelper.UpdatePlacementVisualDelegate updateVisual =
            (placement, _, _) => placement.IsValid = true;
        BuildingPlacementSessionCompositionSystemHelper.Context sessionContext = new(
            runtimeState,
            lifecycle,
            null,
            null,
            () => new BuildingPlacementLifecycleCompositionSystemHelper.CancelContext(null, null, Object.DestroyImmediate),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.BeginContext(
                runtimeState,
                null,
                null,
                placementRoot,
                null,
                Object.DestroyImmediate,
                _ => Vector2Int.zero,
                null,
                updateVisual,
                null,
                null,
                null),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext(
                placement => placement.IsValid,
                _ => true,
                commitPlacement),
            () => new BuildingPlacementLifecycleCompositionSystemHelper.RotateContext(updateVisual),
            null,
            null,
            null,
            null);

        return new BuildingPlacementCommandRequestCompositionSystemHelper.Context(
            null,
            null,
            session,
            sessionContext,
            null);
    }

    private static RuntimeBuildingEntity CreateProducer(GameObject unitPrefab)
    {
        return new RuntimeBuildingEntity
        {
            Id = 806,
            HasOwnerFaction = true,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            Definition = new BuildingDefinition
            {
                DisplayName = "APH806 Barracks",
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

    private static BuildingProductionRequestSystemHelper.Context CreateProductionContext(
        IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        GameObject buildingPrefab,
        GameObject unitPrefab,
        BuildingProductionQueueCompositionSystemHelper productionQueue,
        EntityManager em)
    {
        BuildingDefinition placementDefinition = new()
        {
            DisplayName = "APH806 Barracks",
            Prefab = buildingPrefab
        };
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
            productionQueue,
            queueContext,
            null,
            BuildingDefinitionPrefabSystemHelper.GetProductionPrefab,
            null,
            _ => true,
            _ => true,
            _ => { },
            _ => { },
            (building, productionIndex, spawnUnitPrefab) => productionQueue.TryQueuePlayerUnitFromBuilding(
                queueContext,
                building,
                productionIndex,
                spawnUnitPrefab,
                em,
                now: 2f),
            _ => { },
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
