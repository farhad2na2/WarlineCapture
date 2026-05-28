using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingProductionCompositionSystem
{
    public BuildingProductionContextSystem.Source CreateRuntimeContextSource(
        BuildingGameplayCompositionSourceSystem source,
        Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplayCompositionSourceSystem, BuildingPlacementInteractionSystem.Context, MaterialPropertyBlock, BuildingPlacementCommandSystem.Context> createPlacementCommandContext,
        BuildingPlacementInteractionSystem.Context interactionContext = default,
        MaterialPropertyBlock markerPropertyBlock = null)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource = createRuntimeContextSource(source);
        BuildingRuntimeQuerySystem.Context runtimeQueryContext = source.BuildingRuntimeContextSystem.CreateRuntimeQueryContext(runtimeSource);
        BuildingSpawnSystem.Context spawnContext = source.BuildingRuntimeContextSystem.CreateBuildingSpawnContext(runtimeSource);
        BuildingProductionContextSystem.Source productionSource = default;
        productionSource = source.BuildingProductionContextSystem.CreateSource(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingPlacementStartupSystem.WorldCamera,
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
            prefab => source.BuildingPlacementCommandSystem.BeginPlacementForConfiguredSpawnable(
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock),
                prefab),
            source.RuntimeResourceSystem.TrySpendDollars,
            source.RuntimeResourceSystem.AddDollars,
            cost => source.BuildingPlacementCommandSystem.SetActivePlacementCost(
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock),
                cost),
            (building, productionIndex, spawnUnitPrefab) =>
                TryQueuePlayerUnitProduction(source, productionSource, building, productionIndex, spawnUnitPrefab),
            buildingId => source.RuntimeBuildingSystem.SelectBuilding(buildingId),
            () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
            () => runtimeSource.RefreshBuildingMarkerVisibility?.Invoke(),
            source.BuildingGameplayDependencySystem.ClearFocusedUnit,
            source.BuildingGameplayDependencySystem.SmoothMoveCameraGroundCenterTo,
            building => ResolveBuildingFocusWorldPosition(runtimeSource, building),
            GameRuntimeStats.RecordUnitOrdered,
            Debug.LogWarning,
            (factionId, unitId) => source.BuildingRuntimeQuerySystem.CountPendingProductionsForFaction(runtimeQueryContext, factionId, unitId),
            (factionId, unitId) => source.BuildingRuntimeQuerySystem.CountRuntimeProducedUnitsForFaction(runtimeQueryContext, factionId, unitId),
            source.ResourceHaulerSystem,
            source.FactionResourceSystem,
            (out EntityManager entityManager) => runtimeSource.TryGetEntityManager(out entityManager),
            (out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData) =>
                runtimeSource.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
            entityManager => runtimeSource.EnsureEntityQueries?.Invoke(entityManager),
            () => source.BuildingGameplayEcsQuerySystem.HaulerUnitsQuery,
            () => source.BuildingGameplayEcsQuerySystem.SelectedUnitsQuery,
            runtimeSource.TryGetRuntimeBuilding,
            runtimeSource.GetEffectivePlacementRect);
        return productionSource;
    }

    private static bool TryQueuePlayerUnitProduction(
        BuildingGameplayCompositionSourceSystem source,
        BuildingProductionContextSystem.Source productionSource,
        RuntimeBuildingData building,
        int productionIndex,
        GameObject spawnUnitPrefab)
    {
        if (!productionSource.TryGetEntityManager(out EntityManager em))
            return false;

        return source.BuildingProductionSystem.TryQueuePlayerUnitFromBuilding(
            source.BuildingProductionContextSystem.CreateProductionQueueContext(productionSource),
            building,
            productionIndex,
            spawnUnitPrefab,
            em,
            Time.time);
    }

    private static Vector3 ResolveBuildingFocusWorldPosition(
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource,
        RuntimeBuildingData building)
    {
        if (runtimeSource.TryResolveBuildingFocusWorldPosition != null &&
            runtimeSource.TryResolveBuildingFocusWorldPosition(building, out Vector3 worldPosition))
            return worldPosition;

        return building != null && building.Instance != null
            ? building.Instance.transform.position
            : Vector3.zero;
    }
}
