using System;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed partial class BuildingGameplayCompositionSystemHelper
    {
        // One initialization-owned binding set; delegates retain only this runtime's child systems.
        private sealed class RuntimeBindings
        {
            private readonly BuildingGameplaySourceCompositionSystemHelper childSystems;
            public readonly BuildingGameplayGridDataCompositionSystemHelper.TryGetEntityManagerDelegate tryGetGridEntityManager;
            public readonly BuildingRuntimeContextCompositionSystemHelper.GetEffectivePlacementRectDelegate getEffectivePlacementRect;
            public readonly BuildingRuntimeContextCompositionSystemHelper.IsHouseBuildingDelegate isHouseBuilding;
            public readonly BuildingRuntimeContextCompositionSystemHelper.TryResolveBuildingFocusWorldPositionDelegate tryResolveBuildingFocusWorldPosition;
            public readonly BuildingRuntimeContextCompositionSystemHelper.TryGetRuntimeBuildingDelegate tryGetRuntimeBuilding;
            public readonly BuildingRuntimeContextCompositionSystemHelper.OverlapsAnyPlacementOccupantDelegate overlapsAnyPlacementOccupant;
            public readonly Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource;
            public readonly Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeEntityCompositionSystemHelper.Context> createBuildingRuntimeEntityContext;
            public readonly Func<BuildingGameplaySourceCompositionSystemHelper, BuildingPlacementInteractionCompositionSystemHelper.Context, MaterialPropertyBlock, BuildingRuntimeContextFactoryCompositionSystemHelper.Source> createBuildingRuntimeContextSource;
            public readonly BuildingPlacementAdapterCompositionSystemHelper.CreateRuntimeContextSourceDelegate createRuntimeContextSourceForAdapter;
            public readonly BuildingPlacementAdapterCompositionSystemHelper.CreateBuildingRuntimeContextSourceDelegate createBuildingRuntimeContextSourceForAdapter;
            public RuntimeBindings(BuildingGameplaySourceCompositionSystemHelper configuredSystems)
            {
                childSystems = configuredSystems;
                tryGetGridEntityManager = tryGetEntityManager;
                getEffectivePlacementRect =
                (source, definition, originCell, grid, rotateVertical) => source.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(
                    source,
                    definition,
                    originCell,
                    grid,
                    rotateVertical);
                isHouseBuilding =
                (source, building) => source.BuildingRuntimeQueryCompositionSystemHelper.IsHouseBuilding(source, building);
                tryResolveBuildingFocusWorldPosition =
                (BuildingGameplaySourceCompositionSystemHelper source, RuntimeBuildingEntity building, out Vector3 worldPosition) =>
                    source.BuildingRuntimeQueryCompositionSystemHelper.TryResolveBuildingFocusWorldPosition(
                        source,
                        building,
                        tryGetEntityManager,
                        out worldPosition);
                tryGetRuntimeBuilding =
                (BuildingGameplaySourceCompositionSystemHelper source, int id, out RuntimeBuildingEntity building) =>
                    source.BuildingRuntimeQueryCompositionSystemHelper.TryGetRuntimeBuilding(source, id, out building);
                overlapsAnyPlacementOccupant =
                (source, candidateRect) =>
                    source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyRuntimeBuilding(
                        source,
                        candidateRect,
                        tryGetGridData,
                        (querySource, definition, originCell, grid, rotateVertical) => getEffectivePlacementRect(querySource, definition, originCell, grid, rotateVertical)) ||
                    source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(
                        source,
                        candidateRect,
                        (out EntityManager entityManager) => tryGetEntityManager(out entityManager));
                createRuntimeContextSource =
                source => source.BuildingRuntimeContextCompositionSystemHelper.CreateRuntimeContextSource(
                    source,
                    tryGetEntityManager,
                    tryGetGridData,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    getEffectivePlacementRect);
                createBuildingRuntimeEntityContext =
                source => source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeEntityContext(
                    source,
                    tryGetEntityManager,
                    tryGetGridData,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    getEffectivePlacementRect,
                    DestroyedBuildingLifetimeSeconds);
                createBuildingRuntimeContextSource =
                (source, placementInteractionContext, placementMarkerPropertyBlock) => source.BuildingRuntimeContextCompositionSystemHelper.CreateBuildingRuntimeContextSource(
                    source,
                    placementInteractionContext,
                    placementMarkerPropertyBlock,
                    tryGetEntityManager,
                    tryGetGridData,
                    getEffectivePlacementRect,
                    overlapsAnyPlacementOccupant,
                    isHouseBuilding,
                    tryResolveBuildingFocusWorldPosition,
                    tryGetRuntimeBuilding,
                    source => source.BuildingRuntimeSideEffectCompositionSystemHelper.BeginDeferredRuntimeBuildingSideEffects(source, tryGetEntityManager),
                    source => source.BuildingRuntimeSideEffectCompositionSystemHelper.EndDeferredRuntimeBuildingSideEffects(source, tryGetEntityManager),
                    DestroyedBuildingLifetimeSeconds);
                createRuntimeContextSourceForAdapter =
                source => createRuntimeContextSource(source);
                createBuildingRuntimeContextSourceForAdapter =
                (source, placementInteractionContext, placementMarkerPropertyBlock) =>
                    createBuildingRuntimeContextSource(source, placementInteractionContext, placementMarkerPropertyBlock);
            }
            public bool tryGetEntityManager(out EntityManager entityManager)
            {
                return childSystems.BuildingEntityManagerAccessSystem.TryGetEntityManager(out entityManager);
            }

            public bool tryGetGridData(
                BuildingGameplaySourceCompositionSystemHelper source,
                out Entity gridEntity,
                out GridConfig grid,
                out DynamicBuffer<GridRoad> roads,
                out DynamicBlockerComponent blockerData)
            {
                return source.BuildingGridCompositionSystem.TryGetGridData(
                    source,
                    tryGetGridEntityManager,
                    out gridEntity,
                    out grid,
                    out roads,
                    out blockerData);
            }

            public bool tryGetGridForSelection(BuildingGameplaySourceCompositionSystemHelper source, out GridConfig grid)
            {
                return source.BuildingGridCompositionSystem.TryGetGridForSelection(
                    source,
                    tryGetGridEntityManager,
                    out grid);
            }

            public bool tryGetGridForPlacementInput(BuildingGameplaySourceCompositionSystemHelper source, out GridConfig grid)
            {
                return source.BuildingGridCompositionSystem.TryGetGridForPlacementInput(
                    source,
                    tryGetGridEntityManager,
                    out grid);
            }

            public bool tryGetGridCell(
                BuildingGameplaySourceCompositionSystemHelper source,
                Vector2 screenPosition,
                GridConfig grid,
                out Vector2Int cell)
            {
                return source.BuildingGridCompositionSystem.TryGetGridCell(
                    source,
                    screenPosition,
                    grid,
                    out cell);
            }

        }
    }
}
