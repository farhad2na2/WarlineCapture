using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal static class BuildingRuntimePathInteractionContextFactoryCompositionSystemHelper
    {
        internal static BuildingPlacementRedirectCompositionSystemHelper.Context CreateCreationRedirectContext(
            BuildingRuntimeContextFactoryCompositionSystemHelper.Source source)
        {
            return new BuildingPlacementRedirectCompositionSystemHelper.Context(
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity,
                    out GridConfig grid,
                    out DynamicBuffer<GridRoad> roads,
                    out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                source.GetRedirectUnitsQuery);
        }

        internal static BuildingPlacementRedirectCompositionSystemHelper.Context CreateRedirectContext(
            BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource source)
        {
            return new BuildingPlacementRedirectCompositionSystemHelper.Context(
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity,
                    out GridConfig grid,
                    out DynamicBuffer<GridRoad> roads,
                    out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                () => source.RedirectUnitsQuery);
        }

        internal static BuildingBarrierUtilitySystemHelper.Context CreateBarrierContext(
            BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource source)
        {
            return new BuildingBarrierUtilitySystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity,
                    out GridConfig grid,
                    out DynamicBuffer<GridRoad> roads,
                    out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                () => source.LiveFactionUnitsQuery,
                source.BarrierSystem.IsWallGateDefinitionCached,
                (RuntimeBuildingEntity building, int2 unitFootprint, int2 referenceCell, out int2 goal) =>
                {
                    goal = default;
                    return source.ResourceHaulerBridgeSystem != null &&
                        source.ResourceHaulerBridgeSystem.TryGetRuntimeBuildingApproachCell(
                            CreateResourceHaulerBridgeContext(source),
                            building,
                            unitFootprint,
                            referenceCell,
                            out goal);
                });
        }

        internal static BuildingResourceHaulerBridgeCompositionSystemHelper.Context CreateResourceHaulerBridgeContext(
            BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource source)
        {
            return new BuildingResourceHaulerBridgeCompositionSystemHelper.Context(
                source.RuntimeBuildingSystem.Buildings,
                source.ResourceHaulerUtilitySystemHelper,
                source.FactionResourceCompositionSystemHelper,
                (out EntityManager entityManager) => source.TryGetEntityManager(out entityManager),
                (out Entity gridEntity,
                    out GridConfig grid,
                    out DynamicBuffer<GridRoad> roads,
                    out DynamicBlockerComponent blockerData) =>
                    source.TryGetGridData(out gridEntity, out grid, out roads, out blockerData),
                entityManager => source.EnsureEntityQueries?.Invoke(entityManager),
                () => source.HaulerUnitsQuery,
                () => source.SelectedUnitsQuery,
                source.TryGetRuntimeBuilding,
                building => source.TryResolveBuildingFocusWorldPosition(building, out Vector3 worldPosition)
                    ? worldPosition
                    : Vector3.zero,
                source.GetEffectivePlacementRect,
                source.TryResolveFactionAIOilAllocationInput);
        }

        internal static bool TryAssignSelectedHaulerOrders(
            BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource source,
            int clickedBuildingId)
        {
            return source.ResourceHaulerBridgeSystem != null &&
                source.ResourceHaulerBridgeSystem.TryAssignSelectedHaulerOrders(
                    CreateResourceHaulerBridgeContext(source),
                    clickedBuildingId);
        }
    }
}
