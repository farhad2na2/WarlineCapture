using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;

    internal sealed class BuildingPlacementAdapterCompositionSystemHelper
    {
        internal delegate bool TryGetGridDataDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData);

        internal delegate BuildingRuntimeContextFactoryCompositionSystemHelper.Source CreateBuildingRuntimeContextSourceDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock);

        internal delegate BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource CreateRuntimeContextSourceDelegate(
            BuildingGameplaySourceCompositionSystemHelper source);

        internal delegate RectInt GetEffectivePlacementRectDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical);

        internal delegate bool OverlapsAnyRuntimeBuildingDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            RectInt candidateRect);

        internal delegate bool IsPlacementValidDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition definition,
            Vector2Int originCell,
            Vector2Int footprintCells,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData);

        public bool TryResolveInitialPlacementOrigin(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            BuildingDefinition definition,
            Vector2Int preferredOrigin,
            CreateBuildingRuntimeContextSourceDelegate createBuildingRuntimeContextSource,
            out Vector2Int resolvedOrigin)
        {
            resolvedOrigin = preferredOrigin;
            if (source.BuildingRuntimeSpawnCompositionSystemHelper == null)
                return false;

            BuildingRuntimeSpawnCompositionSystemHelper.Context context = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnContext(
                createBuildingRuntimeContextSource(source, interactionContext, markerPropertyBlock));
            return source.BuildingRuntimeSpawnCompositionSystemHelper.TryResolveInitialPlacementOrigin(
                       context,
                       definition,
                       preferredOrigin,
                       out resolvedOrigin);
        }

        public Vector2Int GetCenterScreenPlacementOrigin(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2Int footprintCells,
            TryGetGridDataDelegate tryGetGridData)
        {
            if (!tryGetGridData(source, out _, out GridConfig grid, out _, out _))
                return Vector2Int.zero;

            return source.BuildingPlacementGridCameraSystemHelper.GetCenterScreenPlacementOrigin(
                footprintCells,
                grid,
                source.BuildingPlacementStartupSystemHelper.WorldCamera,
                source.BuildingPlacementStartupSystemHelper.BuildPlaneY,
                new Vector2(Screen.width, Screen.height));
        }

        public bool IsActivePlacementValid(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2Int originCell,
            Vector2Int footprintCells,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            CreateRuntimeContextSourceDelegate createRuntimeContextSource,
            IsPlacementValidDelegate isPlacementValid)
        {
            PlacementState activePlacement = source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            bool rotateVertical = source.BuildingBarrierUtilitySystemHelper.ResolvePlacementRotateVertical(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(createRuntimeContextSource(source)),
                source.BuildingPlacementInputUiSystemHelper,
                activePlacement);
            return isPlacementValid(source, activePlacement?.Definition, originCell, footprintCells, rotateVertical, grid, roads, blockerData);
        }

        public bool TryAlignGateToNearbyWall(
            BuildingGameplaySourceCompositionSystemHelper source,
            Vector2Int originCell,
            BuildingDefinition definition,
            CreateRuntimeContextSourceDelegate createRuntimeContextSource,
            out bool gateVertical)
        {
            return source.BuildingBarrierUtilitySystemHelper.ShouldAlignGateToNearbyWall(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(createRuntimeContextSource(source)),
                originCell,
                definition,
                out gateVertical);
        }

        public bool IsPlacementValid(
            BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition definition,
            Vector2Int originCell,
            Vector2Int footprintCells,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            OverlapsAnyRuntimeBuildingDelegate overlapsAnyRuntimeBuilding)
        {
            return source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.IsPlacementValid(
                definition,
                originCell,
                footprintCells,
                rotateVertical,
                grid,
                roads,
                blockerData,
                source.BuildingGameplayDependencyCompositionSystemHelper,
                source.BuildingPlacementStartupSystemHelper,
                (candidateDefinition, candidateOrigin, candidateGrid, candidateRotateVertical) =>
                    getEffectivePlacementRect(source, candidateDefinition, candidateOrigin, candidateGrid, candidateRotateVertical),
                candidateRect => overlapsAnyRuntimeBuilding(source, candidateRect));
        }
    }
}
