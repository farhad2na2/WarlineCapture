using System.Collections.Generic;
using UnityEngine;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityClothCoverSpawnSystem
{
    public int PlaceClothCoverBuildings(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        List<GameObject> clothCoverPrefabs,
        int maxCount,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints,
        List<RectInt> shopAndHouseFootprints)
    {
        if (clothCoverPrefabs == null || clothCoverPrefabs.Count == 0 || maxCount <= 0 || shopAndHouseFootprints == null || shopAndHouseFootprints.Count == 0)
            return 0;

        var anchorIndices = new List<int>(shopAndHouseFootprints.Count);
        for (int i = 0; i < shopAndHouseFootprints.Count; i++)
            anchorIndices.Add(i);
        context.PrefabSelectionSystem.Shuffle(anchorIndices, ref rng);

        int placed = 0;
        int anchorCursor = 0;
        int prefabCursor = 0;
        int targetCount = Mathf.Min(maxCount, clothCoverPrefabs.Count);
        while (placed < targetCount && anchorCursor < anchorIndices.Count)
        {
            GameObject prefab = clothCoverPrefabs[prefabCursor % clothCoverPrefabs.Count];
            prefabCursor++;
            RectInt anchor = shopAndHouseFootprints[anchorIndices[anchorCursor]];
            anchorCursor++;

            if (TrySpawnAdjacentDecoration(context, placementSystem, prefab, anchor, ref rng, reservedFootprints))
                placed++;
        }

        return placed;
    }

    private bool TrySpawnAdjacentDecoration(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        GameObject prefab,
        RectInt anchorRect,
        ref Unity.Mathematics.Random rng,
        List<ReservedFootprint> reservedFootprints)
    {
        if (prefab == null)
            return false;

        Vector2Int footprint = placementSystem.GetFootprint(context, prefab);
        var candidateOrigins = context.BuildingPlotSystem.BuildAdjacentOrigins(anchorRect, footprint);
        context.PrefabSelectionSystem.Shuffle(candidateOrigins, ref rng);

        for (int i = 0; i < candidateOrigins.Count; i++)
        {
            Vector2Int preferredOrigin = candidateOrigins[i];
            if (placementSystem.TrySpawnAndReserve(
                context,
                new RuntimeCityBuildingPlacementSystem.Request(
                    prefab,
                    preferredOrigin,
                    footprint,
                    "City Decoration",
                    "Decorative structure beside a town building.",
                    context.Config.DefaultBuildingMaxHealth,
                    reservedFootprints,
                    0,
                    requiredTouchRect: anchorRect),
                out _))
            {
                return true;
            }
        }

        return false;
    }
}
