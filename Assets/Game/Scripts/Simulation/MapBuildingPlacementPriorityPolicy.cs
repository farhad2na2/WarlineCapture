namespace Game.Simulation
{
    /// <summary>
    /// Shared source rule for placing player production buildings before the
    /// bounded sequential map-placement batches.
    /// </summary>
    public static class MapBuildingPlacementPriorityPolicy
    {
        public static int GetProductionCount(
            int productionSlotCount,
            bool hasSpawnUnitPrefab,
            bool hasSecondarySpawnUnitPrefab,
            bool hasTertiarySpawnUnitPrefab,
            bool hasQuaternarySpawnUnitPrefab)
        {
            if (productionSlotCount < 0)
                throw new System.ArgumentOutOfRangeException(nameof(productionSlotCount));
            if (productionSlotCount > 0)
                return productionSlotCount;

            if (hasQuaternarySpawnUnitPrefab) return 4;
            if (hasTertiarySpawnUnitPrefab) return 3;
            if (hasSecondarySpawnUnitPrefab) return 2;
            return hasSpawnUnitPrefab ? 1 : 0;
        }

        public static bool IsPlayerProductionPlacement(byte factionId, byte playerFactionId, int productionCount) =>
            factionId == playerFactionId && productionCount > 0;
    }
}
