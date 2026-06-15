using System.Collections.Generic;
using UnityEngine;
using CityLayoutData = RuntimeCityLayoutSystem.CityLayoutData;

internal sealed class RuntimeCityIngressSystem
{
    public CityLayoutData CreateCityLayout(
        Context context,
        Vector2Int centerRoadCell,
        int townRadius,
        Vector2Int? incomingAnchorCell,
        Vector2Int incomingOutwardDirection,
        ref Unity.Mathematics.Random rng)
    {
        var city = new CityLayoutData
        {
            CenterRoadCell = centerRoadCell,
            TownRadius = townRadius,
            RoadStrokes = context.RoadLayoutSystem.BuildTownRoadStrokes(
                centerRoadCell,
                townRadius,
                context.CityConfig.HallPlazaRadiusRoadCells,
                ref rng)
        };

        if (!incomingAnchorCell.HasValue)
            return city;

        city.HasIncomingAnchor = true;
        city.IncomingAnchorCell = incomingAnchorCell.Value;
        city.IncomingOutwardDirection = incomingOutwardDirection;
        Vector2Int innerConnectionCell = GetCityInnerConnectionCell(context.CityConfig, centerRoadCell, incomingOutwardDirection);
        context.RoadLayoutSystem.AddStroke(city.RoadStrokes, incomingAnchorCell.Value, innerConnectionCell);
        return city;
    }

    public Vector2Int GetCityInnerConnectionCell(
        RuntimeCityConfigSystem.Snapshot cityConfig,
        Vector2Int centerRoadCell,
        Vector2Int outwardDirection)
    {
        int ringRadius = cityConfig.HallPlazaRadiusRoadCells + 1;
        return centerRoadCell + outwardDirection * ringRadius;
    }

    public int GetCityConnectionOffset(RuntimeCityConfigSystem.Snapshot cityConfig, int townRadius)
    {
        return Mathf.Max(townRadius + cityConfig.HallPlazaRadiusRoadCells + 3, cityConfig.HallPlazaRadiusRoadCells + 5);
    }

    public void PruneIngressCorridorStrokes(
        CityLayoutData city,
        Vector2Int incomingRoadAnchorCell,
        Vector2Int inwardDirection,
        int ingressRoadLength)
    {
        if (city.RoadStrokes == null || city.RoadStrokes.Count <= 1)
            return;

        var protectedCells = new HashSet<Vector2Int>();
        Vector2Int current = incomingRoadAnchorCell;
        protectedCells.Add(current);
        for (int i = 0; i < ingressRoadLength; i++)
        {
            current += inwardDirection;
            protectedCells.Add(current);
        }

        for (int strokeIndex = city.RoadStrokes.Count - 2; strokeIndex >= 0; strokeIndex--)
        {
            List<Vector2Int> stroke = city.RoadStrokes[strokeIndex];
            for (int cellIndex = 0; cellIndex < stroke.Count; cellIndex++)
            {
                if (!protectedCells.Contains(stroke[cellIndex]))
                    continue;

                city.RoadStrokes.RemoveAt(strokeIndex);
                break;
            }
        }
    }

    public readonly struct Context
    {
        public readonly RuntimeCityConfigSystem.Snapshot CityConfig;
        public readonly RuntimeCityRoadLayoutState RoadLayoutSystem;

        public Context(
            RuntimeCityConfigSystem.Snapshot cityConfig,
            RuntimeCityRoadLayoutState roadLayoutSystem)
        {
            CityConfig = cityConfig;
            RoadLayoutSystem = roadLayoutSystem;
        }
    }
}
