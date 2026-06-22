using System.Collections.Generic;
using UnityEngine;
using CityChainAxis = RuntimeCityLayoutSystem.CityChainAxis;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityLayoutSystem
{
    private readonly RuntimeCityLayoutState _state = new();

    public RuntimeCityLayoutState State => _state;

    public enum CityChainAxis
    {
        Horizontal,
        Vertical
    }

    public sealed class CityLayoutData
    {
        public Vector2Int CenterRoadCell;
        public int TownRadius;
        public int ChainCoordinate;
        public bool HallPlaced;
        public bool HasIncomingAnchor;
        public Vector2Int IncomingAnchorCell;
        public Vector2Int IncomingOutwardDirection;
        public List<List<Vector2Int>> RoadStrokes = new();
        public HashSet<Vector2Int> RoadCells = new();
        public List<ReservedFootprint> ReservedFootprints = new();
    }

    public int CalculateTownRadius(RuntimeCityConfigSystem.Snapshot config)
    {
        return _state.CalculateTownRadius(config);
    }

    public CityChainAxis ChooseCityChainAxis(
        GridConfig grid,
        int roadCellSizeInGridCells,
        int hallPlazaRadiusRoadCells)
    {
        return _state.ChooseCityChainAxis(grid, roadCellSizeInGridCells, hallPlazaRadiusRoadCells);
    }

    public List<Vector2Int> BuildCityCenters(
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        CityChainAxis chainAxis,
        RuntimeCityConfigSystem.Snapshot config,
        ref Unity.Mathematics.Random rng)
    {
        return _state.BuildCityCenters(grid, roadCellSizeInGridCells, townRadius, chainAxis, config, ref rng);
    }

    public Vector2Int ClampRoadCellToBuildableArea(
        Vector2Int roadCell,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        int hallPlazaRadiusRoadCells)
    {
        return _state.ClampRoadCellToBuildableArea(
            roadCell,
            grid,
            roadCellSizeInGridCells,
            townRadius,
            hallPlazaRadiusRoadCells);
    }

    public Vector2Int FindNearestRoadCellOutsideBaseExclusions(
        Vector2Int roadCell,
        List<RectInt> baseExclusionRoadRects,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        int hallPlazaRadiusRoadCells)
    {
        return _state.FindNearestRoadCellOutsideBaseExclusions(
            roadCell,
            baseExclusionRoadRects,
            grid,
            roadCellSizeInGridCells,
            townRadius,
            hallPlazaRadiusRoadCells);
    }

    public bool IsCityCenterFarEnough(
        Vector2Int candidateCenter,
        List<CityLayoutData> existingCities,
        int townRadius,
        List<RectInt> baseExclusionRoadRects,
        RuntimeCityConfigSystem.Snapshot config)
    {
        return _state.IsCityCenterFarEnough(candidateCenter, existingCities, townRadius, baseExclusionRoadRects, config);
    }

    public void GetRoadGridBounds(
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        int hallPlazaRadiusRoadCells,
        out int minRoadX,
        out int maxRoadX,
        out int minRoadY,
        out int maxRoadY)
    {
        _state.GetRoadGridBounds(
            grid,
            roadCellSizeInGridCells,
            townRadius,
            hallPlazaRadiusRoadCells,
            out minRoadX,
            out maxRoadX,
            out minRoadY,
            out maxRoadY);
    }

    public static bool IsRoadCellInsideAnyBaseExclusion(Vector2Int roadCell, List<RectInt> baseExclusionRoadRects)
    {
        return RuntimeCityLayoutState.IsRoadCellInsideAnyBaseExclusion(roadCell, baseExclusionRoadRects);
    }

    public static bool IsRoadCellWithinBounds(Vector2Int cell, int minRoadX, int maxRoadX, int minRoadY, int maxRoadY)
    {
        return RuntimeCityLayoutState.IsRoadCellWithinBounds(cell, minRoadX, maxRoadX, minRoadY, maxRoadY);
    }
}

internal sealed class RuntimeCityLayoutState
{
    public int CalculateTownRadius(RuntimeCityConfigSystem.Snapshot config)
    {
        int totalBuildings = 1 +
            Mathf.Max(0, config.GasStationCount) +
            Mathf.Max(0, config.ShopCount) +
            Mathf.Max(0, config.HouseCount) +
            Mathf.Max(0, config.OtherBuildingCount) +
            Mathf.Max(0, config.CityDecorationBuildingCount);
        return Mathf.Max(
            config.HallPlazaRadiusRoadCells + 3,
            Mathf.CeilToInt(Mathf.Sqrt(totalBuildings)) + config.ExtraTownRadiusRoadCells);
    }

    public CityChainAxis ChooseCityChainAxis(
        GridConfig grid,
        int roadCellSizeInGridCells,
        int hallPlazaRadiusRoadCells)
    {
        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int margin = Mathf.Max(8, hallPlazaRadiusRoadCells + 6);
        int usableWidth = Mathf.Max(1, roadGridWidth - margin * 2);
        int usableHeight = Mathf.Max(1, roadGridHeight - margin * 2);
        return usableWidth >= usableHeight ? CityChainAxis.Horizontal : CityChainAxis.Vertical;
    }

    public List<Vector2Int> BuildCityCenters(
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        CityChainAxis chainAxis,
        RuntimeCityConfigSystem.Snapshot config,
        ref Unity.Mathematics.Random rng)
    {
        int requestedCount = Mathf.Max(0, config.CityCount);
        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int margin = Mathf.Max(8, config.HallPlazaRadiusRoadCells + 6);
        int minX = Mathf.Clamp(margin, 0, Mathf.Max(0, roadGridWidth - 1));
        int minY = Mathf.Clamp(margin, 0, Mathf.Max(0, roadGridHeight - 1));
        int maxX = Mathf.Clamp(roadGridWidth - 1 - margin, minX, Mathf.Max(minX, roadGridWidth - 1));
        int maxY = Mathf.Clamp(roadGridHeight - 1 - margin, minY, Mathf.Max(minY, roadGridHeight - 1));

        Vector2Int preferredCenter = new(
            Mathf.Clamp(config.StartCell.x / roadCellSizeInGridCells, minX, maxX),
            Mathf.Clamp(config.StartCell.y / roadCellSizeInGridCells, minY, maxY));

        if (requestedCount == 1)
            return new List<Vector2Int> { preferredCenter };

        int minSpacing = Mathf.Max(config.CityMinSpacingRoadCells, townRadius + 2);
        if (chainAxis == CityChainAxis.Horizontal)
            return BuildLinearCityCenters(preferredCenter, requestedCount, minX, maxX, minY, maxY, minSpacing, true, ref rng);

        return BuildLinearCityCenters(preferredCenter, requestedCount, minY, maxY, minX, maxX, minSpacing, false, ref rng);
    }

    public Vector2Int ClampRoadCellToBuildableArea(
        Vector2Int roadCell,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        int hallPlazaRadiusRoadCells)
    {
        GetRoadGridBounds(
            grid,
            roadCellSizeInGridCells,
            townRadius,
            hallPlazaRadiusRoadCells,
            out int minRoadX,
            out int maxRoadX,
            out int minRoadY,
            out int maxRoadY);
        return new Vector2Int(
            Mathf.Clamp(roadCell.x, minRoadX, maxRoadX),
            Mathf.Clamp(roadCell.y, minRoadY, maxRoadY));
    }

    public Vector2Int FindNearestRoadCellOutsideBaseExclusions(
        Vector2Int roadCell,
        List<RectInt> baseExclusionRoadRects,
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        int hallPlazaRadiusRoadCells)
    {
        if (!IsRoadCellInsideAnyBaseExclusion(roadCell, baseExclusionRoadRects))
            return roadCell;

        GetRoadGridBounds(
            grid,
            roadCellSizeInGridCells,
            townRadius,
            hallPlazaRadiusRoadCells,
            out int minRoadX,
            out int maxRoadX,
            out int minRoadY,
            out int maxRoadY);
        int maxRadius = Mathf.Max(maxRoadX - minRoadX, maxRoadY - minRoadY);
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    Vector2Int candidate = roadCell + new Vector2Int(x, y);
                    if (!IsRoadCellWithinBounds(candidate, minRoadX, maxRoadX, minRoadY, maxRoadY))
                        continue;
                    if (IsRoadCellInsideAnyBaseExclusion(candidate, baseExclusionRoadRects))
                        continue;

                    return candidate;
                }
            }
        }

        return roadCell;
    }

    public bool IsCityCenterFarEnough(
        Vector2Int candidateCenter,
        List<CityLayoutData> existingCities,
        int townRadius,
        List<RectInt> baseExclusionRoadRects,
        RuntimeCityConfigSystem.Snapshot config)
    {
        if (IsRoadCellInsideAnyBaseExclusion(candidateCenter, baseExclusionRoadRects))
            return false;

        int minDistance = Mathf.Max(
            config.CityMinSpacingRoadCells,
            townRadius * 2 + config.HallPlazaRadiusRoadCells + 4);
        for (int i = 0; i < existingCities.Count; i++)
        {
            CityLayoutData city = existingCities[i];
            int distance = Mathf.Abs(candidateCenter.x - city.CenterRoadCell.x) + Mathf.Abs(candidateCenter.y - city.CenterRoadCell.y);
            if (distance < minDistance)
                return false;
        }

        return true;
    }

    public void GetRoadGridBounds(
        GridConfig grid,
        int roadCellSizeInGridCells,
        int townRadius,
        int hallPlazaRadiusRoadCells,
        out int minRoadX,
        out int maxRoadX,
        out int minRoadY,
        out int maxRoadY)
    {
        int roadGridWidth = Mathf.Max(1, Mathf.CeilToInt(grid.Width / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int roadGridHeight = Mathf.Max(1, Mathf.CeilToInt(grid.Height / (float)Mathf.Max(1, roadCellSizeInGridCells)));
        int margin = Mathf.Max(8, townRadius + hallPlazaRadiusRoadCells + 3);
        minRoadX = margin;
        maxRoadX = Mathf.Max(margin, roadGridWidth - 1 - margin);
        minRoadY = margin;
        maxRoadY = Mathf.Max(margin, roadGridHeight - 1 - margin);
    }

    public static bool IsRoadCellInsideAnyBaseExclusion(Vector2Int roadCell, List<RectInt> baseExclusionRoadRects)
    {
        if (baseExclusionRoadRects == null)
            return false;

        for (int i = 0; i < baseExclusionRoadRects.Count; i++)
        {
            if (baseExclusionRoadRects[i].Contains(roadCell))
                return true;
        }

        return false;
    }

    public static bool IsRoadCellWithinBounds(Vector2Int cell, int minRoadX, int maxRoadX, int minRoadY, int maxRoadY)
    {
        return cell.x >= minRoadX && cell.x <= maxRoadX && cell.y >= minRoadY && cell.y <= maxRoadY;
    }

    private static List<Vector2Int> BuildLinearCityCenters(
        Vector2Int preferredCenter,
        int requestedCount,
        int minPrimary,
        int maxPrimary,
        int minSecondary,
        int maxSecondary,
        int minSpacing,
        bool horizontal,
        ref Unity.Mathematics.Random rng)
    {
        int availableSpan = Mathf.Max(0, maxPrimary - minPrimary);
        int maxCities = Mathf.Max(1, availableSpan / Mathf.Max(1, minSpacing) + 1);
        int actualCount = Mathf.Min(requestedCount, maxCities);
        if (actualCount <= 1)
            return new List<Vector2Int> { preferredCenter };

        int step = actualCount <= 1 ? 0 : Mathf.Max(minSpacing, Mathf.FloorToInt(availableSpan / (float)(actualCount - 1)));
        int totalSpan = step * Mathf.Max(0, actualCount - 1);
        int slack = Mathf.Max(0, availableSpan - totalSpan);
        int startPrimary = minPrimary + (slack > 0 ? rng.NextInt(0, slack + 1) : 0);
        int secondary = Mathf.Clamp(preferredCenter.y, minSecondary, maxSecondary);
        if (!horizontal)
            secondary = Mathf.Clamp(preferredCenter.x, minSecondary, maxSecondary);
        if (maxSecondary > minSecondary)
            secondary = Mathf.Clamp(secondary + rng.NextInt(-2, 3), minSecondary, maxSecondary);

        var centers = new List<Vector2Int>(actualCount);
        for (int i = 0; i < actualCount; i++)
        {
            int primary = Mathf.Clamp(startPrimary + step * i, minPrimary, maxPrimary);
            centers.Add(horizontal
                ? new Vector2Int(primary, secondary)
                : new Vector2Int(secondary, primary));
        }

        return centers;
    }
}
