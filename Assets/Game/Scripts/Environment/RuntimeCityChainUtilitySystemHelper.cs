using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed class RuntimeCityChainUtilitySystemHelper
{
    private readonly RuntimeCityChainState _state = new();

    public RuntimeCityChainState State => _state;

    public bool TryPlanNextCity(
        Context context,
        List<CityLayoutData> existingCities,
        HashSet<Vector2Int> occupiedRoadCells,
        CityLayoutData currentCity,
        Vector2Int? previousTravelDirection,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        List<RectInt> baseExclusionRoadRects,
        ref Unity.Mathematics.Random rng,
        out List<Vector2Int> sourceExitRoad,
        out List<Vector2Int> autobahnPath,
        out Vector2Int travelDirection,
        out CityLayoutData nextCity)
    {
        return _state.TryPlanNextCity(
            context,
            existingCities,
            occupiedRoadCells,
            currentCity,
            previousTravelDirection,
            grid,
            roadCellSizeInGridCells,
            townRadius,
            baseExclusionRoadRects,
            ref rng,
            out sourceExitRoad,
            out autobahnPath,
            out travelDirection,
            out nextCity);
    }

    public readonly struct Context
    {
        public readonly RuntimeCityConfigSystem.Snapshot CityConfig;
        public readonly RuntimeCityLayoutState LayoutSystem;
        public readonly RuntimeCityRoadLayoutState RoadLayoutSystem;
        public readonly RuntimeCityPrefabSelectionState PrefabSelectionSystem;
        public readonly RuntimeCityRoadCommitState RoadCommitSystem;
        public readonly RuntimeCityIngressState IngressSystem;
        public readonly RuntimeCityIngressSystem.Context IngressContext;

        public Context(
            RuntimeCityConfigSystem.Snapshot cityConfig,
            RuntimeCityLayoutState layoutSystem,
            RuntimeCityRoadLayoutState roadLayoutSystem,
            RuntimeCityPrefabSelectionState prefabSelectionSystem,
            RuntimeCityRoadCommitState roadCommitSystem,
            RuntimeCityIngressState ingressSystem,
            RuntimeCityIngressSystem.Context ingressContext)
        {
            CityConfig = cityConfig;
            LayoutSystem = layoutSystem;
            RoadLayoutSystem = roadLayoutSystem;
            PrefabSelectionSystem = prefabSelectionSystem;
            RoadCommitSystem = roadCommitSystem;
            IngressSystem = ingressSystem;
            IngressContext = ingressContext;
        }
    }
}

internal sealed class RuntimeCityChainState
{
    private static readonly Vector2Int North = new(0, 1);
    private static readonly Vector2Int East = new(1, 0);
    private static readonly Vector2Int South = new(0, -1);
    private static readonly Vector2Int West = new(-1, 0);
    private static readonly Vector2Int[] CardinalDirections = { North, East, South, West };

    public bool TryPlanNextCity(
        RuntimeCityChainUtilitySystemHelper.Context context,
        List<CityLayoutData> existingCities,
        HashSet<Vector2Int> occupiedRoadCells,
        CityLayoutData currentCity,
        Vector2Int? previousTravelDirection,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        List<RectInt> baseExclusionRoadRects,
        ref Unity.Mathematics.Random rng,
        out List<Vector2Int> sourceExitRoad,
        out List<Vector2Int> autobahnPath,
        out Vector2Int travelDirection,
        out CityLayoutData nextCity)
    {
        var directions = new List<Vector2Int>(CardinalDirections);
        context.PrefabSelectionSystem.Shuffle(directions, ref rng);

        if (previousTravelDirection.HasValue)
        {
            Vector2Int reverse = -previousTravelDirection.Value;
            directions.Sort((a, b) =>
            {
                bool aIsReverse = a == reverse;
                bool bIsReverse = b == reverse;
                if (aIsReverse == bIsReverse)
                    return 0;
                return aIsReverse ? 1 : -1;
            });
        }

        int cityConnectionOffset = context.IngressSystem.GetCityConnectionOffset(context.CityConfig, townRadius);
        int autobahnLength = Mathf.Max(context.CityConfig.AutobahnMinLengthRoadCells, context.CityConfig.CityMinSpacingRoadCells);
        context.LayoutSystem.GetRoadGridBounds(
            grid,
            roadCellSizeInGridCells,
            townRadius,
            context.CityConfig.HallPlazaRadiusRoadCells,
            out int minRoadX,
            out int maxRoadX,
            out int minRoadY,
            out int maxRoadY);

        for (int dirIndex = 0; dirIndex < directions.Count; dirIndex++)
        {
            Vector2Int direction = directions[dirIndex];
            Vector2Int sourceInnerConnection = context.IngressSystem.GetCityInnerConnectionCell(context.CityConfig, currentCity.CenterRoadCell, direction);
            Vector2Int targetCenter = currentCity.CenterRoadCell + direction * (autobahnLength + cityConnectionOffset * 2);

            if (!RuntimeCityLayoutState.IsRoadCellWithinBounds(sourceInnerConnection, minRoadX, maxRoadX, minRoadY, maxRoadY) ||
                !RuntimeCityLayoutState.IsRoadCellWithinBounds(targetCenter, minRoadX, maxRoadX, minRoadY, maxRoadY))
            {
                continue;
            }

            if (!context.LayoutSystem.IsCityCenterFarEnough(targetCenter, existingCities, townRadius, baseExclusionRoadRects, context.CityConfig))
            {
                continue;
            }

            var trialRng = rng;
            CityLayoutData plannedCity = context.IngressSystem.CreateCityLayout(context.IngressContext, targetCenter, townRadius, null, default, ref trialRng);
            context.RoadCommitSystem.PopulateCityRoadCells(plannedCity);

            if (!TryGetCityConnectionCell(currentCity, direction, out Vector2Int sourceConnectionCell))
            {
                continue;
            }

            if (!TryGetCityConnectionCell(plannedCity, -direction, out Vector2Int targetConnectionCell))
            {
                continue;
            }

            if (!RuntimeCityLayoutState.IsRoadCellWithinBounds(sourceConnectionCell, minRoadX, maxRoadX, minRoadY, maxRoadY) ||
                !RuntimeCityLayoutState.IsRoadCellWithinBounds(targetConnectionCell, minRoadX, maxRoadX, minRoadY, maxRoadY))
            {
                continue;
            }

            List<Vector2Int> candidateExitRoad = context.RoadLayoutSystem.BuildStraightRoadPath(sourceInnerConnection, sourceConnectionCell);
            if (candidateExitRoad.Count < 2)
            {
                continue;
            }
            if (!IsCityExitPathValid(candidateExitRoad, occupiedRoadCells, currentCity))
            {
                continue;
            }

            List<Vector2Int> candidatePath = context.RoadLayoutSystem.BuildStraightRoadPath(sourceConnectionCell, targetConnectionCell);
            if (!IsAutobahnPathValid(candidatePath, occupiedRoadCells, existingCities, currentCity, plannedCity, context.CityConfig.HallPlazaRadiusRoadCells))
            {
                continue;
            }

            sourceExitRoad = candidateExitRoad;
            autobahnPath = candidatePath;
            travelDirection = direction;
            nextCity = plannedCity;
            rng = trialRng;
            return true;
        }

        sourceExitRoad = null;
        autobahnPath = null;
        travelDirection = default;
        nextCity = null;
        return false;
    }

    private static bool IsCityExitPathValid(List<Vector2Int> path, HashSet<Vector2Int> occupiedRoadCells, CityLayoutData sourceCity)
    {
        if (path == null || path.Count < 2)
            return false;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            if (sourceCity.RoadCells.Contains(cell))
                continue;
            if (occupiedRoadCells.Contains(cell))
                return false;
        }

        return true;
    }

    private static bool IsAutobahnPathValid(
        List<Vector2Int> path,
        HashSet<Vector2Int> occupiedRoadCells,
        List<CityLayoutData> existingCities,
        CityLayoutData sourceCity,
        CityLayoutData targetCity,
        int hallPlazaRadiusRoadCells)
    {
        if (path == null || path.Count < 3)
            return false;

        for (int i = 1; i < path.Count; i++)
        {
            if (occupiedRoadCells.Contains(path[i]))
                return false;
        }

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int cell = path[i];
            for (int cityIndex = 0; cityIndex < existingCities.Count; cityIndex++)
            {
                CityLayoutData city = existingCities[cityIndex];
                if (ReferenceEquals(city, sourceCity))
                    continue;

                int distance = Mathf.Abs(cell.x - city.CenterRoadCell.x) + Mathf.Abs(cell.y - city.CenterRoadCell.y);
                if (distance <= city.TownRadius + hallPlazaRadiusRoadCells + 2)
                    return false;
            }
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (targetCity.RoadCells.Contains(path[i]))
                return false;
        }

        return true;
    }

    private static bool TryGetCityConnectionCell(CityLayoutData city, Vector2Int direction, out Vector2Int connectionCell)
    {
        connectionCell = default;
        bool found = false;
        int bestDistance = int.MinValue;

        foreach (Vector2Int roadCell in city.RoadCells)
        {
            Vector2Int delta = roadCell - city.CenterRoadCell;
            int distance;

            if (direction == East)
            {
                if (delta.y != 0 || delta.x <= 0)
                    continue;
                distance = delta.x;
            }
            else if (direction == West)
            {
                if (delta.y != 0 || delta.x >= 0)
                    continue;
                distance = -delta.x;
            }
            else if (direction == North)
            {
                if (delta.x != 0 || delta.y <= 0)
                    continue;
                distance = delta.y;
            }
            else if (direction == South)
            {
                if (delta.x != 0 || delta.y >= 0)
                    continue;
                distance = -delta.y;
            }
            else
            {
                continue;
            }

            if (distance <= bestDistance)
                continue;

            bestDistance = distance;
            connectionCell = roadCell;
            found = true;
        }

        return found;
    }

}
