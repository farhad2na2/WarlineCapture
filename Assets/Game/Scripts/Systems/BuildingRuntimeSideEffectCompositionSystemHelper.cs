using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeSideEffectCompositionSystemHelper
{
    internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public void BeginDeferredRuntimeBuildingSideEffects(
        BuildingGameplaySourceCompositionSystemHelper source,
        TryGetEntityManagerDelegate tryGetEntityManager)
    {
        source.BuildingPlacementRedirectSystem.BeginDeferredRuntimeBuildingSideEffects(
            () => source.BuildingPlacementInvalidCellSystem.RebuildPlacementInvalidPrefix(
                source.BuildingGameplayGridDataCompositionSystemHelper,
                source.BuildingGameplayEcsQueryCompositionSystemHelper,
                (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                source.BuildingPlacementStartupSystemHelper,
                source.BuildingGameplayDependencyCompositionSystemHelper));
    }

    public void EndDeferredRuntimeBuildingSideEffects(
        BuildingGameplaySourceCompositionSystemHelper source,
        TryGetEntityManagerDelegate tryGetEntityManager)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource =
            source.BuildingRuntimeContextCompositionSystemHelper.CreateRuntimeContextSource(
                source,
                (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                (BuildingGameplaySourceCompositionSystemHelper gridSource, out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    gridSource.BuildingGridCompositionSystem.TryGetGridData(
                        gridSource,
                        (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                        out gridEntity,
                        out grid,
                        out roads,
                        out blockerData),
                (querySource, building) => querySource.BuildingRuntimeQueryCompositionSystemHelper.IsHouseBuilding(querySource, building),
                (BuildingGameplaySourceCompositionSystemHelper querySource, RuntimeBuildingEntity building, out Vector3 worldPosition) =>
                    querySource.BuildingRuntimeQueryCompositionSystemHelper.TryResolveBuildingFocusWorldPosition(
                        querySource,
                        building,
                        (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                        out worldPosition),
                (BuildingGameplaySourceCompositionSystemHelper querySource, int id, out RuntimeBuildingEntity building) =>
                    querySource.BuildingRuntimeQueryCompositionSystemHelper.TryGetRuntimeBuilding(querySource, id, out building),
                (querySource, definition, originCell, grid, rotateVertical) =>
                    querySource.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(querySource, definition, originCell, grid, rotateVertical));
        source.BuildingPlacementRedirectSystem.EndDeferredRuntimeBuildingSideEffects(
            source.BuildingRuntimeContextSystem.CreateRedirectContext(runtimeSource),
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                    runtimeSource,
                    source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                    null,
                    source.RuntimeObjectPresentationHelper.DestroyRuntimeObject)),
            source.BuildingPlacementInvalidCellSystem.Clear);
    }
}
