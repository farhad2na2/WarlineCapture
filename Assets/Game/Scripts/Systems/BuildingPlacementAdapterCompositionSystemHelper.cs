using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;
    using Source = BuildingGameplaySourceCompositionSystemHelper;

    internal sealed class BuildingPlacementAdapterCompositionSystemHelper
    {
        internal delegate bool TryGetGridDataDelegate(
            Source source,
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData);
        internal delegate BuildingRuntimeContextFactoryCompositionSystemHelper.Source CreateBuildingRuntimeContextSourceDelegate(
            Source source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock);

        internal delegate BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource CreateRuntimeContextSourceDelegate(
            Source source);

        internal delegate RectInt GetEffectivePlacementRectDelegate(
            Source source,
            BuildingDefinition definition,
            Vector2Int originCell,
            GridConfig grid,
            bool rotateVertical);

        internal delegate bool OverlapsAnyPlacementOccupantDelegate(
            Source source,
            RectInt candidateRect);
        internal delegate bool IsPlacementValidDelegate(
            Source source,
            BuildingDefinition definition,
            Vector2Int originCell,
            Vector2Int footprintCells,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData);
        public bool TryResolveInitialPlacementOrigin(
            Source source,
            BuildingPlacementInteractionCompositionSystemHelper.Context interactionContext,
            MaterialPropertyBlock markerPropertyBlock,
            BuildingDefinition definition,
            Vector2Int origin,
            CreateBuildingRuntimeContextSourceDelegate createContext,
            out Vector2Int resolved)
        {
            resolved = origin;
            if (source.BuildingRuntimeSpawnCompositionSystemHelper == null)
                return false;

            BuildingRuntimeSpawnCompositionSystemHelper.Context context = source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSpawnContext(
                createContext(source, interactionContext, markerPropertyBlock));
            origin = CampaignMissionBuildingPlacementPolicy.ResolvePreferredOrigin(
                source, definition, context.GetPlacementFootprint, origin);
            return source.BuildingRuntimeSpawnCompositionSystemHelper.TryResolveInitialPlacementOrigin(
                       context,
                       definition,
                       origin,
                       out resolved);
        }

        public Vector2Int GetCenterScreenPlacementOrigin(
            Source source,
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
            Source source,
            Vector2Int originCell,
            Vector2Int footprintCells,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            CreateRuntimeContextSourceDelegate createContext,
            IsPlacementValidDelegate isPlacementValid)
        {
            PlacementState activePlacement = source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            bool rotateVertical = source.BuildingBarrierUtilitySystemHelper.ResolvePlacementRotateVertical(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(createContext(source)),
                source.BuildingPlacementInputUiSystemHelper,
                activePlacement);
            return isPlacementValid(source, activePlacement?.Definition, originCell, footprintCells, rotateVertical, grid, roads, blockerData);
        }

        public bool TryAlignGateToNearbyWall(
            Source source,
            Vector2Int originCell,
            BuildingDefinition definition,
            CreateRuntimeContextSourceDelegate createContext,
            out bool gateVertical)
        {
            return source.BuildingBarrierUtilitySystemHelper.ShouldAlignGateToNearbyWall(
                source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateBarrierContext(createContext(source)),
                originCell,
                definition,
                out gateVertical);
        }

        public bool IsPlacementValid(
            Source source,
            BuildingDefinition definition,
            Vector2Int originCell,
            Vector2Int footprintCells,
            bool rotateVertical,
            GridConfig grid,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            OverlapsAnyPlacementOccupantDelegate overlapsAnyPlacementOccupant)
        {
            return CampaignMissionBuildingPlacementPolicy.IsAllowed(source, definition, new RectInt(originCell, footprintCells)) &&
                source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.IsPlacementValid(
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
                candidateRect => overlapsAnyPlacementOccupant(source, candidateRect));
        }
    }
}
