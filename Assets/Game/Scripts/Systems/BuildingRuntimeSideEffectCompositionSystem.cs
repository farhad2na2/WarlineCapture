using Unity.Entities;
using UnityEngine;

internal sealed partial class BuildingRuntimeSideEffectCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    internal delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);

    public void BeginDeferredRuntimeBuildingSideEffects(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager)
    {
        source.BuildingPlacementRedirectSystem.BeginDeferredRuntimeBuildingSideEffects(
            () => source.BuildingPlacementInvalidCellSystem.RebuildPlacementInvalidPrefix(
                source.BuildingGameplayGridDataSystem,
                source.BuildingGameplayEcsQuerySystem,
                (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                source.BuildingPlacementStartupSystem,
                source.BuildingGameplayDependencySystem));
    }

    public void EndDeferredRuntimeBuildingSideEffects(
        BuildingGameplayCompositionSourceSystem source,
        TryGetEntityManagerDelegate tryGetEntityManager)
    {
        BuildingRuntimeContextSystem.RuntimeSource runtimeSource =
            source.BuildingRuntimeCompositionSystem.CreateRuntimeContextSource(
                source,
                (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                (BuildingGameplayCompositionSourceSystem gridSource, out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData) =>
                    gridSource.BuildingGridCompositionSystem.TryGetGridData(
                        gridSource,
                        (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                        out gridEntity,
                        out grid,
                        out roads,
                        out blockerData),
                (querySource, building) => querySource.BuildingRuntimeCompositionQuerySystem.IsHouseBuilding(querySource, building),
                (BuildingGameplayCompositionSourceSystem querySource, RuntimeBuildingEntity building, out Vector3 worldPosition) =>
                    querySource.BuildingRuntimeCompositionQuerySystem.TryResolveBuildingFocusWorldPosition(
                        querySource,
                        building,
                        (out EntityManager entityManager) => tryGetEntityManager(out entityManager),
                        out worldPosition),
                (BuildingGameplayCompositionSourceSystem querySource, int id, out RuntimeBuildingEntity building) =>
                    querySource.BuildingRuntimeCompositionQuerySystem.TryGetRuntimeBuilding(querySource, id, out building),
                (querySource, definition, originCell, grid, rotateVertical) =>
                    querySource.BuildingRuntimeCompositionQuerySystem.GetEffectivePlacementRect(querySource, definition, originCell, grid, rotateVertical));
        source.BuildingPlacementRedirectSystem.EndDeferredRuntimeBuildingSideEffects(
            source.BuildingRuntimeContextSystem.CreateRedirectContext(runtimeSource),
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                    runtimeSource,
                    source.BuildingPlacementStartupSystem.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystem.BuildingRoot,
                    null,
                    source.BuildingRuntimeObjectSystem.DestroyRuntimeObject)),
            source.BuildingPlacementInvalidCellSystem.Clear);
    }
}
