using System;
using Game.Runtime;

internal static class AuthoredRoadCacheWaterKeyTest
{
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        Console.WriteLine("PASS " + name);
    }

    private static void Main()
    {
        object providerA = new object();
        object providerB = new object();
        Check(BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(null, false, null, null, false),
            "stable no-match cache does not thrash");
        Check(!BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(null, false, null, providerA, false),
            "provider appearance invalidates cache");
        Check(!BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(providerA, false, null, providerA, true),
            "match appearance invalidates no-match cache");
        Check(!BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(providerA, true, null, providerA, true),
            "cleared active-match mask after failed creation remains invalid");
        Check(!BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(providerA, true, new bool[1], providerB, true),
            "provider replacement invalidates cache");
        Check(BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(providerA, true, new bool[1], providerA, true),
            "stable active-match cache is reused");
        Check(!BuildingPlacementAuthoredRoadCache.WaterSourceIsCurrent(providerA, true, new bool[1], providerA, false),
            "match disappearance invalidates active cache");
        Check(!BuildingPlacementAuthoredRoadCache.IsWaterMaskKnownForPlacement(true, null, null),
            "active match with no provider has unknown water");
        Check(!BuildingPlacementAuthoredRoadCache.IsWaterMaskKnownForPlacement(true, providerA, null),
            "active match with failed mask creation has unknown water");
        Check(BuildingPlacementAuthoredRoadCache.IsWaterMaskKnownForPlacement(true, providerA, new bool[1]),
            "active match with a mask has known water");
        Check(BuildingPlacementAuthoredRoadCache.IsWaterMaskKnownForPlacement(false, null, null),
            "non-Skirmish retains no-water behavior");

        var cache = new BuildingPlacementAuthoredRoadCache();
        var grid = new Game.Components.GridConfig { Width = 1, Height = 1, CellSize = 1 };
        cache.Ensure(default, default, grid);
        Check(cache.GetWater() == null, "active match without provider does not publish an empty water mask");
        Check(cache.OverlapsWater(grid, new UnityEngine.Vector2Int { x = 0, y = 0 }, new UnityEngine.Vector2Int { x = 1, y = 1 }),
            "actual cache water-overlap path rejects unknown active-match water");

        Unity.Entities.EntityQuery.EntityCount = 0;
        try
        {
            cache.Ensure(default, default, grid);
            Check(cache.GetWater() == null, "missing surface owner leaves water mask unavailable");
            Check(cache.OverlapsWater(grid, new UnityEngine.Vector2Int { x = 0, y = 0 }, new UnityEngine.Vector2Int { x = 1, y = 1 }),
                "missing surface owner refreshes active-match state and rejects unknown water");
        }
        finally { Unity.Entities.EntityQuery.EntityCount = 1; }
    }
}
