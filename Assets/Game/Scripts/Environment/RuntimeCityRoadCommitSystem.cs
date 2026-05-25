using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed class RuntimeCityRoadCommitSystem
{
    public void CommitCityRoadNetwork(Context context, CityLayoutData city, HashSet<Vector2Int> occupiedRoadCells)
    {
        PopulateCityRoadCells(city);
        for (int strokeIndex = 0; strokeIndex < city.RoadStrokes.Count; strokeIndex++)
        {
            List<Vector2Int> stroke = city.RoadStrokes[strokeIndex];
            context.RoadBuildBridgeSystem.CreateRoadStrokeFromRoadCells(stroke);
            for (int cellIndex = 0; cellIndex < stroke.Count; cellIndex++)
            {
                Vector2Int cell = stroke[cellIndex];
                city.RoadCells.Add(cell);
                occupiedRoadCells.Add(cell);
            }
        }
    }

    public void PopulateCityRoadCells(CityLayoutData city)
    {
        city.RoadCells.Clear();
        for (int strokeIndex = 0; strokeIndex < city.RoadStrokes.Count; strokeIndex++)
        {
            List<Vector2Int> stroke = city.RoadStrokes[strokeIndex];
            for (int cellIndex = 0; cellIndex < stroke.Count; cellIndex++)
                city.RoadCells.Add(stroke[cellIndex]);
        }
    }

    public bool TryCommitSourceExitRoad(
        Context context,
        int cityNumber,
        List<Vector2Int> sourceExitRoad,
        CityLayoutData currentCity,
        HashSet<Vector2Int> occupiedRoadCells)
    {
        if (!context.RoadBuildBridgeSystem.CreateRoadStrokeFromRoadCells(sourceExitRoad))
        {
            context.Diagnostics?.LogSourceExitRoadFailed(cityNumber, sourceExitRoad.Count);
            return false;
        }

        for (int exitIndex = 0; exitIndex < sourceExitRoad.Count; exitIndex++)
        {
            Vector2Int cell = sourceExitRoad[exitIndex];
            occupiedRoadCells.Add(cell);
            currentCity.RoadCells.Add(cell);
        }

        return true;
    }

    public bool TryCommitAutobahn(
        Context context,
        int cityNumber,
        List<Vector2Int> autobahnPath,
        Vector2Int travelDirection,
        CityLayoutData currentCity,
        HashSet<Vector2Int> occupiedRoadCells,
        out List<Vector2Int> extendedAutobahnPath,
        out Vector2Int endConnectorCell)
    {
        extendedAutobahnPath = new List<Vector2Int>(autobahnPath);
        extendedAutobahnPath.Add(autobahnPath[autobahnPath.Count - 1] + travelDirection);

        if (!context.RoadBuildBridgeSystem.CreateAutobahnStrokeFromRoadCells(extendedAutobahnPath, true, true))
        {
            context.Diagnostics?.LogAutobahnFailed(cityNumber, extendedAutobahnPath.Count, travelDirection);
            endConnectorCell = default;
            return false;
        }

        for (int pathIndex = 0; pathIndex < extendedAutobahnPath.Count; pathIndex++)
        {
            Vector2Int cell = extendedAutobahnPath[pathIndex];
            occupiedRoadCells.Add(cell);
            currentCity.RoadCells.Add(cell);
        }

        endConnectorCell = extendedAutobahnPath[extendedAutobahnPath.Count - 1];
        return true;
    }

    public bool TryCreateStandaloneConnector(
        Context context,
        Vector2Int endConnectorCell,
        Vector2Int travelDirection,
        int roadLength,
        out Vector2Int secondCityAnchorCell)
    {
        secondCityAnchorCell = default;
        if (!context.RoadBuildBridgeSystem.CreateStandaloneStraightRoadChainFromConnector(
                endConnectorCell,
                travelDirection,
                roadLength))
        {
            return false;
        }

        return context.RoadBuildBridgeSystem.TryGetStandaloneStraightChainEndRoadCell(travelDirection, out secondCityAnchorCell);
    }

    public readonly struct Context
    {
        public readonly RuntimeCityRoadBuildBridgeSystem RoadBuildBridgeSystem;
        public readonly RuntimeCityDiagnosticSystem Diagnostics;

        public Context(
            RuntimeCityRoadBuildBridgeSystem roadBuildBridgeSystem,
            RuntimeCityDiagnosticSystem diagnostics)
        {
            RoadBuildBridgeSystem = roadBuildBridgeSystem;
            Diagnostics = diagnostics;
        }
    }
}
