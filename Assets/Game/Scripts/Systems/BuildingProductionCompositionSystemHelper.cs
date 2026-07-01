using System;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingProductionCompositionSystemHelper
{
    public BuildingProductionContextCompositionSystemHelper.Source CreateRuntimeContextSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource,
        Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext,
        BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext = default,
        MaterialPropertyBlock markerPropertyBlock = null)
    {
        BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource runtimeSource = createRuntimeContextSource(source);
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateRuntimeQueryContext(runtimeSource);
        BuildingSpawnCompositionSystemHelper.Context spawnContext = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBuildingSpawnContext(runtimeSource);
        BuildingProductionContextCompositionSystemHelper.Source productionSource = default;
        productionSource = source.BuildingProductionContextCompositionSystemHelper.CreateSource(
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingPlacementStartupSystemHelper.WorldCamera,
            source.BuildingDefinitionPrefabSystemHelper,
            source.BuildingProductionQueueCompositionSystemHelper,
            source.BuildingProductionUpdateCompositionSystemHelper,
            source.BuildingProductionTransportPresentationSystemHelper,
            source.BuildingProductionTransportBridgeCompositionSystemHelper,
            source.BuildingProductionSlotUtilitySystemHelper,
            source.BuildingRunwaySystem,
            source.BuildingVisualSystem,
            source.BuildingSpawnCompositionSystemHelper,
            spawnContext,
            source.RuntimeResourceUtilitySystemHelper.CurrentDollars,
            source.BuildingPlacementStartupSystemHelper.MaxQueuedUnitProductions,
            prefab => EnqueueAndProcessBeginPlacementForConfiguredSpawnable(
                source,
                createPlacementCommandContext(source, interactionContext, markerPropertyBlock),
                prefab),
            source.RuntimeResourceUtilitySystemHelper.TrySpendDollars,
            source.RuntimeResourceUtilitySystemHelper.AddDollars,
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
            (factionId, unitId) => source.BuildingRuntimeReadModelCompositionSystemHelper.CountPendingProductionsForFaction(runtimeQueryContext, factionId, unitId),
            (factionId, unitId) => source.BuildingRuntimeReadModelCompositionSystemHelper.CountRuntimeProducedUnitsForFaction(runtimeQueryContext, factionId, unitId),
            source.ResourceHaulerUtilitySystemHelper,
            source.FactionResourceCompositionSystemHelper,
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
