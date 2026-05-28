using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialAirPlatformSpawnSystem
{
    public bool TryGetInitialAirPlatformSpawn(
        EntityManager em,
        Entity boundaryEntity,
        byte factionId,
        int2 configuredSpawnOffset,
        GridConfig grid,
        out int2 cell,
        out float3 position)
    {
        cell = default;
        position = default;
        if (!new InitialBuildingBoundarySystem().TryGetFactionProductionSpawnPoints(
                em,
                boundaryEntity,
                out DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints))
            return false;

        bool useHelipad = configuredSpawnOffset.y <= -45;
        string buildingId = BuildingDefinitionSystem.NormalizeSpawnableKey(useHelipad ? "Building_Helipad" : "Building_Airport");
        int remainingSlotIndex = ResolveInitialAirPlatformSlotIndex(configuredSpawnOffset, useHelipad);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            BuildingFactionProductionSpawnPointReadModel spawnPoint = spawnPoints[i];
            if (spawnPoint.FactionId != factionId ||
                spawnPoint.BuildingId.ToString() != buildingId)
            {
                continue;
            }

            if (remainingSlotIndex > 0)
            {
                remainingSlotIndex--;
                continue;
            }

            if (!GridUtils.InBounds(spawnPoint.Cell, grid.Width, grid.Height))
                return false;

            cell = spawnPoint.Cell;
            position = spawnPoint.WorldPosition;
            return true;
        }

        return false;
    }

    private static int ResolveInitialAirPlatformSlotIndex(int2 configuredSpawnOffset, bool useHelipad)
    {
        int x = configuredSpawnOffset.x;
        if (useHelipad)
        {
            if (x < 80)
                return 0;
            if (x < 100)
                return 1;
            return 2;
        }

        if (x < 56)
            return 0;
        if (x < 70)
            return 1;
        return 2;
    }
}
