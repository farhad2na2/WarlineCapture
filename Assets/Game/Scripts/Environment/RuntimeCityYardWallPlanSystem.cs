using System.Collections.Generic;
using UnityEngine;
using ReservedFootprint = RuntimeCityWalkabilitySystem.ReservedFootprint;

internal sealed class RuntimeCityYardWallPlanSystem
{
    public readonly struct HousePlan
    {
        public readonly List<RectInt> ShuffledHouses;
        public readonly int TargetCount;

        public HousePlan(List<RectInt> shuffledHouses, int targetCount)
        {
            ShuffledHouses = shuffledHouses;
            TargetCount = targetCount;
        }
    }

    public HousePlan CreateHousePlan(
        List<RectInt> houseFootprints,
        float houseWallChance,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        ref Unity.Mathematics.Random rng)
    {
        var shuffledHouses = new List<RectInt>(houseFootprints);
        prefabSelectionSystem.Shuffle(shuffledHouses, ref rng);

        int targetCount = Mathf.RoundToInt(shuffledHouses.Count * Mathf.Clamp01(houseWallChance));
        return new HousePlan(shuffledHouses, targetCount);
    }

    public bool TryFindYardRect(
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RectInt houseRect,
        int minDistanceCells,
        int maxDistanceCells,
        int roadCellSizeInGridCells,
        HashSet<Vector2Int> roadCells,
        List<ReservedFootprint> reservedFootprints,
        GridConfig grid,
        ref Unity.Mathematics.Random rng,
        out RectInt yardRect)
    {
        int minPadding = Mathf.Max(1, Mathf.Min(minDistanceCells, maxDistanceCells));
        int maxPadding = Mathf.Max(minPadding, maxDistanceCells);
        var candidatePaddings = new List<int>();
        for (int padding = minPadding; padding <= maxPadding; padding++)
            candidatePaddings.Add(padding);
        prefabSelectionSystem.Shuffle(candidatePaddings, ref rng);

        for (int i = 0; i < candidatePaddings.Count; i++)
        {
            yardRect = walkabilitySystem.ExpandRect(houseRect, candidatePaddings[i]);
            if (walkabilitySystem.CanPlaceHouseYardRect(yardRect, houseRect, roadCellSizeInGridCells, roadCells, reservedFootprints, grid))
                return true;
        }

        yardRect = default;
        return false;
    }
}
