using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingProductionCompositionSystemHelper
{
    public BuildingProductionContextCompositionSystemHelper.Source CreateRuntimeContextSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        BuildingPlacementInteractionSystem.Context interactionContext = default,
        MaterialPropertyBlock markerPropertyBlock = null)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = createRuntimeContextSource(source);
        BuildingRuntimeQuerySystem.Context runtimeQueryContext = source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(runtimeSource);
        BuildingSpawnSystem.Context spawnContext = source.BuildingRuntimeContextSystem.CreateBuildingSpawnContext(runtimeSource);
        BuildingProductionContextCompositionSystemHelper.Source productionSource = default;
        productionSource = source.BuildingProductionContextCompositionSystemHelper.CreateSource(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingPlacementStartupSystemHelper.WorldCamera,
            source.BuildingDefinitionSystem,
            source.BuildingProductionSystem,
            source.BuildingProductionUpdateSystem,
            source.BuildingProductionTransportSystem,
            source.BuildingProductionTransportBridgeSystem,
            source.BuildingProductionSlotSystem,
            source.BuildingRunwaySystem,
            source.BuildingVisualSystem,
            source.BuildingSpawnSystem,
            spawnContext,
            source.RuntimeResourceSystem.CurrentDollars,
            prefab => EnqueueAndProcessBeginPlacementForConfiguredSpawnable(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock),
                prefab),
            source.RuntimeResourceSystem.TrySpendDollars,
            source.RuntimeResourceSystem.AddDollars,
            cost => source.BuildingPlacementCommandRequestCompositionSystemHelper.SetActivePlacementCost(
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock),
                cost),
            (building, productionIndex, spawnUnitPrefab) =>
                source.BuildingProductionContextCompositionSystemHelper.TryQueuePlayerUnitProduction(
                    productionSource,
                    building,
                    productionIndex,
                    spawnUnitPrefab,
                    UnityEngine.Time.time),
            buildingId => source.RuntimeBuildingSystem.SelectBuilding(buildingId),
            () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
            () => runtimeSource.RefreshBuildingMarkerVisibility?.Invoke(),
            source.BuildingGameplayDependencyCompositionSystemHelper.ClearFocusedUnit,
            source.BuildingGameplayDependencyCompositionSystemHelper.IsBuildDrawerOpen,
            source.BuildingGameplayDependencyCompositionSystemHelper.SmoothMoveCameraGroundCenterTo,
            building => BuildingRuntimeFocusPositionPresentationSystemHelper.Resolve(runtimeSource, building),
            GameRuntimeStats.RecordUnitOrdered,
            Debug.LogWarning,
            (factionId, unitId) => source.BuildingRuntimeQuerySystem.CountPendingProductionsForFaction(runtimeQueryContext, factionId, unitId),
            (factionId, unitId) => source.BuildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction(runtimeQueryContext, factionId, unitId),
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            (out EntityManager entityManager) => runtimeSource.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                runtimeSource.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => runtimeSource.EnsureEntityQueries?.Invoke(entityManager),
            () => source.BuildingGameplayEcsQueryCompositionSystemHelper.HaulerUnitsQuery,
            () => source.BuildingGameplayEcsQueryCompositionSystemHelper.SelectedUnitsQuery,
            runtimeSource.TryGetRuntimeBuilding,
            runtimeSource.GetEffectivePlacementRect,
            source.PrepareTransportDropVisual);
        return productionSource;
    }

    private static bool EnqueueAndProcessBeginPlacementForConfiguredSpawnable(
        BuildingGameplaySourceCompositionSystemHelper source,
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context,
        GameObject prefab)
    {
        return source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager entityManager)
            ? source.BuildingPlacementCommandRequestCompositionSystemHelper.EnqueueAndProcessBeginPlacementForConfiguredSpawnable(entityManager, context, prefab)
            : BeginPlacementForConfiguredSpawnableWithoutEntityManager(context, prefab);
    }

    private static bool BeginPlacementForConfiguredSpawnableWithoutEntityManager(
        BuildingPlacementCommandRequestCompositionSystemHelper.Context context,
        GameObject prefab)
    {
        if (context.DefinitionSystem == null ||
            !context.DefinitionSystem.TryGetConfiguredDefinition(prefab, out BuildingDefinition definition))
        {
            return false;
        }

        context.SessionSystem?.BeginPlacement(context.SessionContext, definition);
        return true;
    }
}
