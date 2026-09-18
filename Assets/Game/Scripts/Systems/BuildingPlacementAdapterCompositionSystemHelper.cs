using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;
    using Source = BuildingGameplaySourceCompositionSystemHelper;

    internal sealed class BuildingPlacementAdapterCompositionSystemHelper
    {
        private const float SkirmishMaxFoundationHeightDelta = 0.5f;
        private const float SkirmishMaxBuildingSlopeDegrees = 8f;
        private const float SkirmishMaxFoundationElevation = 0.75f;
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
            return CampaignMissionBuildingPlacementPolicy.TryResolveInitialPlacementOrigin(
                source, context, definition, origin, out resolved);
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
            RectInt footprint = definition != null && getEffectivePlacementRect != null
                ? getEffectivePlacementRect(source, definition, originCell, grid, rotateVertical)
                : new RectInt(originCell, footprintCells);
            return CampaignMissionBuildingPlacementPolicy.IsAllowed(source, definition, footprint) &&
                IsSkirmishSurfaceValid(source, footprint) &&
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

        private bool IsSkirmishSurfaceValid(Source source, RectInt footprint)
        {
            if (!source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out EntityManager em))
                return false;
            using EntityQuery skirmish = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
            if (skirmish.IsEmptyIgnoreFilter ||
                skirmish.GetSingleton<SkirmishMatchState>().Phase != SkirmishPhase.Playing)
                return true; // Scripted starting buildings use their authored spawn rules.

            using EntityQuery surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            if (surfaceQuery.CalculateEntityCount() != 1)
                return false;
            MapSurfaceComponent surface = surfaceQuery.GetSingleton<MapSurfaceComponent>();
            return IsSkirmishFootprintLevel(surface, footprint);
        }

        internal static bool IsSkirmishFootprintLevel(MapSurfaceComponent surface, RectInt footprint)
        {
            if (surface.HasSurfaceData == 0 || !surface.SurfaceBlob.IsCreated ||
                footprint.width <= 0 || footprint.height <= 0 ||
                footprint.xMin < 0 || footprint.yMin < 0 ||
                footprint.xMax > surface.Dimensions.x || footprint.yMax > surface.Dimensions.y)
                return false;

            // Skirmish's clear-cell grid includes mountain slopes and plateaus.
            // Check every cell of the real rotated footprint against the baked
            // terrain. SurfaceId is per cell on this map, so it is not a region ID.
            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            float minHeight = float.PositiveInfinity;
            float maxHeight = float.NegativeInfinity;
            for (int y = footprint.yMin; y < footprint.yMax; y++)
            for (int x = footprint.xMin; x < footprint.xMax; x++)
            {
                if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, new int2(x, y), out MapSurfaceSample sample) ||
                    !math.isfinite(sample.Height) || !math.isfinite(sample.SlopeDegrees) ||
                    sample.SlopeDegrees > SkirmishMaxBuildingSlopeDegrees)
                    return false;

                minHeight = math.min(minHeight, sample.Height);
                maxHeight = math.max(maxHeight, sample.Height);
                if (maxHeight - minHeight > SkirmishMaxFoundationHeightDelta ||
                    maxHeight - surface.GridOrigin.y > SkirmishMaxFoundationElevation)
                    return false;
            }
            return true;
        }
    }
}
