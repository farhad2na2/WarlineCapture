using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using PlacementRequest = RuntimeCityBuildingPlacementPrefabSystemHelper.Request;
    using PlacementResult = RuntimeCityBuildingPlacementPrefabSystemHelper.Result;
    using PlotCandidate = RuntimeCityBuildingPlotUtilitySystemHelper.PlotCandidate;
    using ReservedFootprint = RuntimeCityWalkabilityUtilitySystemHelper.ReservedFootprint;

    internal sealed class RuntimeCityBuildingPlacementPrefabSystemHelper
    {
        private readonly RuntimeCityBuildingPlacementState _state = new();

        public RuntimeCityBuildingPlacementState State => _state;

        public readonly struct Request
        {
            public readonly GameObject Prefab;
            public readonly Vector2Int PreferredOrigin;
            public readonly Vector2Int Footprint;
            public readonly string FallbackDisplayName;
            public readonly string FallbackDescription;
            public readonly int MaxHealth;
            public readonly List<ReservedFootprint> ReservedFootprints;
            public readonly int ReservationPadding;
            public readonly int RoadCellSizeInGridCells;
            public readonly HashSet<Vector2Int> RoadCells;
            public readonly RectInt? RequiredTouchRect;
            public readonly Vector2Int VisualFacingDirection;

            public Request(
                GameObject prefab,
                Vector2Int preferredOrigin,
                Vector2Int footprint,
                string fallbackDisplayName,
                string fallbackDescription,
                int maxHealth,
                List<ReservedFootprint> reservedFootprints,
                int reservationPadding,
                int roadCellSizeInGridCells = 0,
                HashSet<Vector2Int> roadCells = null,
                RectInt? requiredTouchRect = null,
                Vector2Int visualFacingDirection = default)
            {
                Prefab = prefab;
                PreferredOrigin = preferredOrigin;
                Footprint = footprint;
                FallbackDisplayName = fallbackDisplayName;
                FallbackDescription = fallbackDescription;
                MaxHealth = maxHealth;
                ReservedFootprints = reservedFootprints;
                ReservationPadding = reservationPadding;
                RoadCellSizeInGridCells = roadCellSizeInGridCells;
                RoadCells = roadCells;
                RequiredTouchRect = requiredTouchRect;
                VisualFacingDirection = visualFacingDirection;
            }
        }

        public readonly struct Result
        {
            public readonly int BuildingId;
            public readonly Vector2Int ActualOrigin;
            public readonly Vector2Int ActualFootprint;

            public Result(int buildingId, Vector2Int actualOrigin, Vector2Int actualFootprint)
            {
                BuildingId = buildingId;
                ActualOrigin = actualOrigin;
                ActualFootprint = actualFootprint;
            }
        }

        public bool TrySpawnAndReserve(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            Request request,
            out Result result,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            return _state.TrySpawnAndReserve(
                context,
                request,
                out result,
                placementAnchors,
                secondaryPlacementAnchors);
        }

        public Vector2Int GetFootprint(RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context, GameObject prefab)
        {
            return _state.GetFootprint(context, prefab);
        }

        public void PlaceFromPlots(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            List<GameObject> prefabs,
            List<PlotCandidate> candidates,
            int count,
            int minPlotSpacing,
            int roadCellSizeInGridCells,
            string fallbackDisplayName,
            string fallbackDescription,
            int maxHealth,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            _state.PlaceFromPlots(
                context,
                prefabs,
                candidates,
                count,
                minPlotSpacing,
                roadCellSizeInGridCells,
                fallbackDisplayName,
                fallbackDescription,
                maxHealth,
                ref rng,
                usedPlotCells,
                reservedFootprints,
                placementAnchors,
                secondaryPlacementAnchors);
        }
    }

    internal sealed class RuntimeCityBuildingPlacementState
    {
        public bool TrySpawnAndReserve(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            PlacementRequest request,
            out PlacementResult result,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            result = default;
            if (request.Prefab == null)
                return false;

            var preferredRect = new RectInt(request.PreferredOrigin, request.Footprint);
            if (OverlapsRoad(context, preferredRect, request))
                return false;
            if (context.WalkabilitySystem.WouldBeTooCloseToReserved(request.PreferredOrigin, request.Footprint, request.ReservedFootprints, request.ReservationPadding))
                return false;

            if (!context.SpawnBridgeSystem.TrySpawnCityBuilding(
                    request.Prefab,
                    request.PreferredOrigin,
                    out int buildingId,
                    out Vector2Int actualOrigin,
                    out Vector2Int actualFootprint,
                    request.FallbackDisplayName,
                    request.FallbackDescription,
                    request.Footprint,
                    request.MaxHealth,
                    GetVisualRotation(request.VisualFacingDirection)))
                return false;

            var actualRect = new RectInt(actualOrigin, actualFootprint);
            if (OverlapsRoad(context, actualRect, request) ||
                context.WalkabilitySystem.WouldBeTooCloseToReserved(actualOrigin, actualFootprint, request.ReservedFootprints, request.ReservationPadding) ||
                !TouchesRequiredRect(context, actualRect, request.RequiredTouchRect))
            {
                context.SpawnBridgeSystem.DeleteCityBuilding(buildingId);
                return false;
            }

            context.WalkabilitySystem.ReserveFootprint(request.ReservedFootprints, actualOrigin, actualFootprint, request.ReservationPadding);
            placementAnchors?.Add(actualRect);
            secondaryPlacementAnchors?.Add(actualRect);
            result = new PlacementResult(buildingId, actualOrigin, actualFootprint);
            return true;
        }

        public Vector2Int GetFootprint(RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context, GameObject prefab)
        {
            return context.PrefabSelectionSystem.GetCachedFootprintCells(prefab);
        }

        public void PlaceFromPlots(
            RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context,
            List<GameObject> prefabs,
            List<PlotCandidate> candidates,
            int count,
            int minPlotSpacing,
            int roadCellSizeInGridCells,
            string fallbackDisplayName,
            string fallbackDescription,
            int maxHealth,
            ref Unity.Mathematics.Random rng,
            List<Vector2Int> usedPlotCells,
            List<ReservedFootprint> reservedFootprints,
            List<RectInt> placementAnchors = null,
            List<RectInt> secondaryPlacementAnchors = null)
        {
            if (prefabs == null || prefabs.Count == 0 || count <= 0 || candidates == null || candidates.Count == 0)
                return;

            int placed = 0;
            for (int i = 0; i < candidates.Count && placed < count; i++)
            {
                PlotCandidate candidate = candidates[i];
                if (!context.BuildingPlotSystem.HasPlotSpacing(candidate.PlotCell, usedPlotCells, minPlotSpacing))
                    continue;

                GameObject prefab = context.PrefabSelectionSystem.GetRandomPrefab(prefabs, ref rng);
                if (prefab == null)
                    continue;

                Vector2Int footprint = GetFootprint(context, prefab);
                Vector2Int preferredOrigin = context.BuildingPlotSystem.GetCenteredOriginForPlot(candidate.PlotCell, footprint, roadCellSizeInGridCells);
                if (!TrySpawnAndReserve(
                        context,
                        new PlacementRequest(
                            prefab,
                            preferredOrigin,
                            footprint,
                            fallbackDisplayName,
                            fallbackDescription,
                            maxHealth,
                            reservedFootprints,
                            0,
                            visualFacingDirection: candidate.RoadFacingDirection),
                        out _,
                        placementAnchors,
                        secondaryPlacementAnchors))
                    continue;

                usedPlotCells.Add(candidate.PlotCell);
                placed++;
            }
        }

        private static bool OverlapsRoad(RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context, RectInt rect, PlacementRequest request)
        {
            return request.RoadCells != null &&
                   context.WalkabilitySystem.DoesRectOverlapRoadCells(rect, request.RoadCellSizeInGridCells, request.RoadCells);
        }

        private static bool TouchesRequiredRect(RuntimeCityBuildingSpawnContextCompositionSystemHelper.Context context, RectInt actualRect, RectInt? requiredTouchRect)
        {
            return !requiredTouchRect.HasValue || context.WalkabilitySystem.TouchesRect(actualRect, requiredTouchRect.Value);
        }

        internal static Quaternion GetVisualRotation(Vector2Int facingDirection)
        {
            if (facingDirection == Vector2Int.zero)
                return Quaternion.identity;

            Vector3 forward = new(facingDirection.x, 0f, facingDirection.y);
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
    }
}
