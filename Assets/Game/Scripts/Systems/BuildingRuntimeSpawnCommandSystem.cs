using UnityEngine;

internal sealed class BuildingRuntimeSpawnCommandSystem
{
    public readonly struct Context
    {
        public readonly BuildingRuntimeSpawnSystem RuntimeSpawnSystem;
        public readonly BuildingRuntimeSpawnSystem.Context SpawnContext;
        public readonly BuildingDefinition SoldierBaseDefinition;
        public readonly BuildingDefinition SoldierTentDefinition;
        public readonly BuildingDefinition FactoryDefinition;

        public Context(
            BuildingRuntimeSpawnSystem runtimeSpawnSystem,
            BuildingRuntimeSpawnSystem.Context spawnContext,
            BuildingDefinition soldierBaseDefinition,
            BuildingDefinition soldierTentDefinition,
            BuildingDefinition factoryDefinition)
        {
            RuntimeSpawnSystem = runtimeSpawnSystem;
            SpawnContext = spawnContext;
            SoldierBaseDefinition = soldierBaseDefinition;
            SoldierTentDefinition = soldierTentDefinition;
            FactoryDefinition = factoryDefinition;
        }
    }

    public void SpawnInitialTestRoster(Context context, Vector2Int anchorCell)
    {
        context.RuntimeSpawnSystem?.SpawnInitialTestRoster(
            context.SpawnContext,
            context.SoldierBaseDefinition,
            context.SoldierTentDefinition,
            context.FactoryDefinition,
            anchorCell);
    }

    public bool TrySpawnRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        string fallbackDisplayName = "Building",
        string fallbackDescription = "Operational building.",
        Vector2Int? fallbackFootprint = null,
        int fallbackMaxHealth = 500,
        bool isCityGenerated = false,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        return TrySpawnRuntimeBuilding(
            context,
            prefab,
            preferredOrigin,
            out buildingId,
            out _,
            out _,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint,
            fallbackMaxHealth,
            isCityGenerated,
            ownerFactionId,
            rotateVertical);
    }

    public bool TrySpawnRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        out int buildingId,
        out Vector2Int actualOrigin,
        out Vector2Int actualFootprint,
        string fallbackDisplayName = "Building",
        string fallbackDescription = "Operational building.",
        Vector2Int? fallbackFootprint = null,
        int fallbackMaxHealth = 500,
        bool isCityGenerated = false,
        byte? ownerFactionId = null,
        bool rotateVertical = false)
    {
        buildingId = 0;
        actualOrigin = default;
        actualFootprint = default;
        if (context.RuntimeSpawnSystem == null ||
            !context.RuntimeSpawnSystem.TrySpawnRuntimeBuilding(
                context.SpawnContext,
                prefab,
                preferredOrigin,
                fallbackDisplayName,
                fallbackDescription,
                fallbackFootprint,
                fallbackMaxHealth,
                isCityGenerated,
                ownerFactionId,
                rotateVertical,
                out BuildingRuntimeSpawnSystem.SpawnRuntimeBuildingResult result))
        {
            return false;
        }

        buildingId = result.BuildingId;
        actualOrigin = result.ActualOrigin;
        actualFootprint = result.ActualFootprint;
        return true;
    }

    public int TrySpawnRuntimeWallRun(Context context, GameObject prefab, Vector2Int startOrigin, Vector2Int endOrigin, byte? ownerFactionId)
    {
        return context.RuntimeSpawnSystem != null
            ? context.RuntimeSpawnSystem.TrySpawnRuntimeWallRun(context.SpawnContext, prefab, startOrigin, endOrigin, ownerFactionId)
            : 0;
    }

    public bool TryGetRuntimeWallSegmentFootprint(Context context, GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TryGetRuntimeWallSegmentFootprint(context.SpawnContext, prefab, rotateVertical, out footprint);
    }

    public bool TrySpawnRuntimeWallSegment(Context context, GameObject prefab, Vector2Int origin, bool rotateVertical, byte? ownerFactionId, bool allowExistingWallOverlap)
    {
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TrySpawnRuntimeWallSegment(context.SpawnContext, prefab, origin, rotateVertical, ownerFactionId, allowExistingWallOverlap);
    }

    public bool TryGetRuntimeBuildingPlacementFootprint(Context context, GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint(context.SpawnContext, prefab, rotateVertical, out footprint);
    }

    public bool TrySpawnInitialBuilding(Context context, BuildingDefinition definition, Vector2Int preferredOrigin, bool rotateVertical, out RuntimeBuildingData building)
    {
        building = null;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TrySpawnInitialBuilding(context.SpawnContext, definition, preferredOrigin, rotateVertical, out building);
    }

    public bool TrySpawnInitialBuilding(Context context, BuildingDefinition definition, Vector2Int preferredOrigin, out RuntimeBuildingData building)
    {
        building = null;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TrySpawnInitialBuilding(context.SpawnContext, definition, preferredOrigin, out building);
    }

    public bool TryResolveInitialPlacementOrigin(Context context, BuildingDefinition definition, Vector2Int preferredOrigin, out Vector2Int resolvedOrigin)
    {
        resolvedOrigin = preferredOrigin;
        return context.RuntimeSpawnSystem != null &&
               context.RuntimeSpawnSystem.TryResolveInitialPlacementOrigin(context.SpawnContext, definition, preferredOrigin, out resolvedOrigin);
    }
}
