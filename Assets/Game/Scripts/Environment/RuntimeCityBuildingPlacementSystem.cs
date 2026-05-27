using System.Collections.Generic;
using UnityEngine;
using PlotCandidate = RuntimeCityBuildingPlotSystem.PlotCandidate;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityBuildingPlacementSystem
{
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
            RectInt? requiredTouchRect = null)
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
        RuntimeCityBuildingSpawnContextSystem.Context context,
        Request request,
        out Result result,
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
                request.MaxHealth))
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
        result = new Result(buildingId, actualOrigin, actualFootprint);
        return true;
    }

    public Vector2Int GetFootprint(RuntimeCityBuildingSpawnContextSystem.Context context, GameObject prefab)
    {
        return context.PrefabSelectionSystem.GetCachedFootprintCells(prefab);
    }

    public void PlaceFromPlots(
        RuntimeCityBuildingSpawnContextSystem.Context context,
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
                    new Request(
                        prefab,
                        preferredOrigin,
                        footprint,
                        fallbackDisplayName,
                        fallbackDescription,
                        maxHealth,
                        reservedFootprints,
                        0),
                    out _,
                    placementAnchors,
                    secondaryPlacementAnchors))
                continue;

            usedPlotCells.Add(candidate.PlotCell);
            placed++;
        }
    }

    private static bool OverlapsRoad(RuntimeCityBuildingSpawnContextSystem.Context context, RectInt rect, Request request)
    {
        return request.RoadCells != null &&
               context.WalkabilitySystem.DoesRectOverlapRoadCells(rect, request.RoadCellSizeInGridCells, request.RoadCells);
    }

    private static bool TouchesRequiredRect(RuntimeCityBuildingSpawnContextSystem.Context context, RectInt actualRect, RectInt? requiredTouchRect)
    {
        return !requiredTouchRect.HasValue || context.WalkabilitySystem.TouchesRect(actualRect, requiredTouchRect.Value);
    }
}
