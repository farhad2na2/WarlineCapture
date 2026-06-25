using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed class BuildingRuntimeSpawnCompositionSystemHelper
{
    public delegate bool TryGetGridDataDelegate(
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerComponent blockerData);

    public delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);
    public delegate RectInt GetEffectivePlacementRectDelegate(BuildingDefinition definition, Vector2Int originCell, GridConfig grid, bool rotateVertical);
    public delegate bool IsPlacementValidDelegate(BuildingDefinition definition, Vector2Int originCell, Vector2Int footprintCells, bool rotateVertical, GridConfig grid, DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockerData);
    public delegate bool HasCachedInvalidCellInFootprintDelegate(Vector2Int originCell, Vector2Int footprintCells);
    public delegate GameObject CreateBuildingVisualInstanceDelegate(BuildingDefinition definition, Transform parent);
    public delegate void PositionBuildingObjectDelegate(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid, bool rotateVertical);
    public delegate RuntimeBuildingEntity RegisterRuntimeBuildingDelegate(BuildingDefinition definition, GameObject instance, Vector2Int originCell, bool removeOverlappingBlockers);
    public delegate void SetRuntimeBuildingOwnerFactionDelegate(RuntimeBuildingEntity building, byte? ownerFactionId);

    public readonly struct SpawnRuntimeBuildingResult
    {
        public readonly int BuildingId;
        public readonly Vector2Int ActualOrigin;
        public readonly Vector2Int ActualFootprint;

        public SpawnRuntimeBuildingResult(int buildingId, Vector2Int actualOrigin, Vector2Int actualFootprint)
        {
            BuildingId = buildingId;
            ActualOrigin = actualOrigin;
            ActualFootprint = actualFootprint;
        }
    }

    public readonly struct Context
    {
        public readonly Transform BuildingRoot;
        public readonly BuildingDefinitionPrefabSystemHelper DefinitionSystem;
        public readonly BuildingRunwaySystem RunwaySystem;
        public readonly BuildingPlacementValidationUtilitySystemHelper PlacementValidationSystem;
        public readonly BuildingPlacementValidationUtilitySystemHelper.WallValidationContext WallValidationContext;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly GetEffectivePlacementRectDelegate GetEffectivePlacementRect;
        public readonly IsPlacementValidDelegate IsPlacementValid;
        public readonly HasCachedInvalidCellInFootprintDelegate HasCachedInvalidCellInFootprint;
        public readonly CreateBuildingVisualInstanceDelegate CreateBuildingVisualInstance;
        public readonly PositionBuildingObjectDelegate PositionBuildingObject;
        public readonly RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
        public readonly SetRuntimeBuildingOwnerFactionDelegate SetRuntimeBuildingOwnerFaction;

        public Context(
            Transform buildingRoot,
            BuildingDefinitionPrefabSystemHelper definitionSystem,
            BuildingRunwaySystem runwaySystem,
            BuildingPlacementValidationUtilitySystemHelper placementValidationSystem,
            BuildingPlacementValidationUtilitySystemHelper.WallValidationContext wallValidationContext,
            TryGetGridDataDelegate tryGetGridData,
            GetPlacementFootprintDelegate getPlacementFootprint,
            GetEffectivePlacementRectDelegate getEffectivePlacementRect,
            IsPlacementValidDelegate isPlacementValid,
            HasCachedInvalidCellInFootprintDelegate hasCachedInvalidCellInFootprint,
            CreateBuildingVisualInstanceDelegate createBuildingVisualInstance,
            PositionBuildingObjectDelegate positionBuildingObject,
            RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
            SetRuntimeBuildingOwnerFactionDelegate setRuntimeBuildingOwnerFaction)
        {
            BuildingRoot = buildingRoot;
            DefinitionSystem = definitionSystem;
            RunwaySystem = runwaySystem;
            PlacementValidationSystem = placementValidationSystem;
            WallValidationContext = wallValidationContext;
            TryGetGridData = tryGetGridData;
            GetPlacementFootprint = getPlacementFootprint;
            GetEffectivePlacementRect = getEffectivePlacementRect;
            IsPlacementValid = isPlacementValid;
            HasCachedInvalidCellInFootprint = hasCachedInvalidCellInFootprint;
            CreateBuildingVisualInstance = createBuildingVisualInstance;
            PositionBuildingObject = positionBuildingObject;
            RegisterRuntimeBuilding = registerRuntimeBuilding;
            SetRuntimeBuildingOwnerFaction = setRuntimeBuildingOwnerFaction;
        }
    }

    public void SpawnInitialTestRoster(
        Context context,
        BuildingDefinition soldierBaseDefinition,
        BuildingDefinition soldierTentDefinition,
        BuildingDefinition factoryDefinition,
        Vector2Int anchorCell)
    {
        TrySpawnInitialBuilding(context, soldierBaseDefinition, anchorCell + new Vector2Int(-18, -10), out _);
        TrySpawnInitialBuilding(context, soldierTentDefinition, anchorCell + new Vector2Int(-18, 16), out _);
        TrySpawnInitialBuilding(context, factoryDefinition, anchorCell + new Vector2Int(18, -4), out _);
    }

    public bool TrySpawnRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        string fallbackDisplayName,
        string fallbackDescription,
        Vector2Int? fallbackFootprint,
        int fallbackMaxHealth,
        bool isCityGenerated,
        byte? ownerFactionId,
        bool rotateVertical,
        out SpawnRuntimeBuildingResult result)
    {
        result = default;
        if (prefab == null || context.DefinitionSystem == null)
            return false;

        BuildingDefinition definition = context.DefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint ?? new Vector2Int(10, 10),
            fallbackMaxHealth,
            context.RunwaySystem);
        Vector2Int placementFootprint = context.GetPlacementFootprint(definition, rotateVertical);

        if (!TrySpawnInitialBuilding(context, definition, preferredOrigin, rotateVertical, out RuntimeBuildingEntity building))
            return false;

        building.IsCityGenerated = isCityGenerated;
        context.SetRuntimeBuildingOwnerFaction?.Invoke(building, ownerFactionId);
        result = new SpawnRuntimeBuildingResult(building.Id, building.OriginCell, building.Definition.FootprintCells);

        if (placementFootprint.x <= 0 || placementFootprint.y <= 0)
            result = new SpawnRuntimeBuildingResult(result.BuildingId, result.ActualOrigin, building.Definition.FootprintCells);

        return true;
    }

    public bool TryPlaceRuntimeBuilding(
        Context context,
        GameObject prefab,
        Vector2Int preferredOrigin,
        string fallbackDisplayName,
        string fallbackDescription,
        Vector2Int? fallbackFootprint,
        int fallbackMaxHealth,
        bool isCityGenerated,
        byte? ownerFactionId,
        bool rotateVertical,
        out SpawnRuntimeBuildingResult result)
    {
        return TrySpawnRuntimeBuilding(
            context,
            prefab,
            preferredOrigin,
            fallbackDisplayName,
            fallbackDescription,
            fallbackFootprint,
            fallbackMaxHealth,
            isCityGenerated,
            ownerFactionId,
            rotateVertical,
            out result);
    }

    public int TrySpawnRuntimeWallRun(
        Context context,
        GameObject prefab,
        Vector2Int startOrigin,
        Vector2Int endOrigin,
        byte? ownerFactionId)
    {
        if (prefab == null || context.DefinitionSystem == null || context.PlacementValidationSystem == null)
            return 0;
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            return 0;

        BuildingDefinition definition = CreateRuntimeWallDefinition(context, prefab);
        if (!BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(definition))
            return 0;

        bool vertical = Mathf.Abs(endOrigin.y - startOrigin.y) > Mathf.Abs(endOrigin.x - startOrigin.x);
        if (vertical)
            endOrigin.x = startOrigin.x;
        else
            endOrigin.y = startOrigin.y;

        Vector2Int wallFootprint = BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(definition, vertical);
        List<Vector2Int> origins = BuildingPlacementCommitCompositionSystemHelper.BuildWallRunOrigins(startOrigin, endOrigin, wallFootprint, vertical);
        int spawned = 0;
        for (int i = 0; i < origins.Count; i++)
        {
            Vector2Int origin = origins[i];
            if (context.TryGetGridData == null ||
                !context.TryGetGridData(out _, out grid, out DynamicBuffer<GridRoad> currentRoads, out DynamicBlockerComponent currentBlockerData))
            {
                break;
            }

            if (!context.PlacementValidationSystem.IsWallPlacementValid(
                    origin,
                    wallFootprint,
                    vertical,
                    grid,
                    currentRoads,
                    currentBlockerData,
                    context.WallValidationContext))
            {
                continue;
            }

            GameObject instance = context.CreateBuildingVisualInstance?.Invoke(definition, context.BuildingRoot);
            if (instance == null)
                continue;

            context.PositionBuildingObject?.Invoke(instance, origin, definition, grid, vertical);
            RuntimeBuildingEntity building = context.RegisterRuntimeBuilding?.Invoke(CloneDefinitionWithFootprint(definition, wallFootprint), instance, origin, true);
            context.SetRuntimeBuildingOwnerFaction?.Invoke(building, ownerFactionId);
            spawned++;
        }

        return spawned;
    }

    public bool TryGetRuntimeWallSegmentFootprint(Context context, GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        if (prefab == null || context.DefinitionSystem == null)
            return false;

        BuildingDefinition definition = CreateRuntimeWallDefinition(context, prefab);
        footprint = BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(definition, rotateVertical);
        return footprint.x > 0 && footprint.y > 0;
    }

    public bool TrySpawnRuntimeWallSegment(
        Context context,
        GameObject prefab,
        Vector2Int origin,
        bool rotateVertical,
        byte? ownerFactionId,
        bool allowExistingWallOverlap)
    {
        if (prefab == null || context.DefinitionSystem == null || context.PlacementValidationSystem == null)
            return false;
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
            return false;

        BuildingDefinition definition = CreateRuntimeWallDefinition(context, prefab);
        if (!BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(definition))
            return false;

        Vector2Int wallFootprint = BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(definition, rotateVertical);
        if (!context.PlacementValidationSystem.IsWallPlacementValid(
                origin,
                wallFootprint,
                rotateVertical,
                grid,
                roads,
                blockerData,
                context.WallValidationContext,
                allowExistingWallOverlap))
        {
            return false;
        }

        GameObject instance = context.CreateBuildingVisualInstance?.Invoke(definition, context.BuildingRoot);
        if (instance == null)
            return false;

        context.PositionBuildingObject?.Invoke(instance, origin, definition, grid, rotateVertical);
        RuntimeBuildingEntity building = context.RegisterRuntimeBuilding?.Invoke(CloneDefinitionWithFootprint(definition, wallFootprint), instance, origin, !allowExistingWallOverlap);
        context.SetRuntimeBuildingOwnerFaction?.Invoke(building, ownerFactionId);
        return true;
    }

    public bool TryGetRuntimeBuildingPlacementFootprint(Context context, GameObject prefab, bool rotateVertical, out Vector2Int footprint)
    {
        footprint = default;
        if (prefab == null || context.DefinitionSystem == null)
            return false;

        BuildingDefinition definition = context.DefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Operational building.",
            new Vector2Int(10, 10),
            500,
            context.RunwaySystem);
        footprint = context.GetPlacementFootprint != null
            ? context.GetPlacementFootprint(definition, rotateVertical)
            : definition.FootprintCells;
        return footprint.x > 0 && footprint.y > 0;
    }

    public bool TrySpawnInitialBuilding(
        Context context,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out RuntimeBuildingEntity building)
    {
        return TrySpawnInitialBuilding(context, definition, preferredOrigin, false, out building);
    }

    public bool TrySpawnInitialBuilding(
        Context context,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        bool rotateVertical,
        out RuntimeBuildingEntity building)
    {
        building = null;
        if (definition == null || definition.Prefab == null)
            return false;

        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
            return false;

        if (!TryFindValidInitialBuildingOrigin(context, definition, preferredOrigin, rotateVertical, grid, roads, blockerData, out Vector2Int originCell))
            return false;

        GameObject instance = context.CreateBuildingVisualInstance?.Invoke(definition, context.BuildingRoot);
        if (instance == null)
            return false;

        context.PositionBuildingObject?.Invoke(instance, originCell, definition, grid, rotateVertical);
        Vector2Int footprint = context.GetPlacementFootprint != null
            ? context.GetPlacementFootprint(definition, rotateVertical)
            : definition.FootprintCells;
        building = context.RegisterRuntimeBuilding?.Invoke(CloneDefinitionWithFootprint(definition, footprint), instance, originCell, true);
        return building != null;
    }

    public bool TryFindValidInitialBuildingOrigin(
        Context context,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        bool rotateVertical,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData,
        out Vector2Int originCell)
    {
        originCell = default;
        if (definition == null || context.GetPlacementFootprint == null || context.GetEffectivePlacementRect == null || context.IsPlacementValid == null)
            return false;

        Vector2Int placementFootprint = context.GetPlacementFootprint(definition, rotateVertical);
        Vector2Int clampedPreferred = new(
            Mathf.Clamp(preferredOrigin.x, 0, Mathf.Max(0, grid.Width - placementFootprint.x)),
            Mathf.Clamp(preferredOrigin.y, 0, Mathf.Max(0, grid.Height - placementFootprint.y)));

        RectInt preferredPlacementRect = context.GetEffectivePlacementRect(definition, clampedPreferred, grid, rotateVertical);
        int footprintSearchRadius = Mathf.Max(placementFootprint.x, placementFootprint.y) * 4;
        int maxSearchRadius = Mathf.Max(
            24,
            Mathf.Min(
                160,
                Mathf.Max(
                    footprintSearchRadius,
                    preferredPlacementRect.width,
                    preferredPlacementRect.height)));
        for (int radius = 0; radius <= maxSearchRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (radius > 0 && Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue;

                    Vector2Int candidate = clampedPreferred + new Vector2Int(dx, dy);
                    RectInt candidateRect = context.GetEffectivePlacementRect(definition, candidate, grid, rotateVertical);
                    if (context.HasCachedInvalidCellInFootprint != null &&
                        context.HasCachedInvalidCellInFootprint(candidateRect.position, candidateRect.size))
                    {
                        continue;
                    }

                    if (!context.IsPlacementValid(definition, candidate, placementFootprint, rotateVertical, grid, roads, blockerData))
                        continue;

                    originCell = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryResolveInitialPlacementOrigin(
        Context context,
        BuildingDefinition definition,
        Vector2Int preferredOrigin,
        out Vector2Int resolvedOrigin)
    {
        resolvedOrigin = preferredOrigin;
        if (definition == null || context.GetPlacementFootprint == null || context.IsPlacementValid == null)
            return false;
        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
            return false;

        const bool rotateVertical = false;
        Vector2Int footprint = context.GetPlacementFootprint(definition, rotateVertical);
        Vector2Int clampedPreferred = new(
            Mathf.Clamp(preferredOrigin.x, 0, Mathf.Max(0, grid.Width - footprint.x)),
            Mathf.Clamp(preferredOrigin.y, 0, Mathf.Max(0, grid.Height - footprint.y)));

        if (context.IsPlacementValid(definition, clampedPreferred, footprint, rotateVertical, grid, roads, blockerData))
        {
            resolvedOrigin = clampedPreferred;
            return true;
        }

        int maxRadius = Mathf.Max(grid.Width, grid.Height);
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                        continue;

                    Vector2Int candidate = clampedPreferred + new Vector2Int(dx, dy);
                    candidate.x = Mathf.Clamp(candidate.x, 0, Mathf.Max(0, grid.Width - footprint.x));
                    candidate.y = Mathf.Clamp(candidate.y, 0, Mathf.Max(0, grid.Height - footprint.y));
                    if (!context.IsPlacementValid(definition, candidate, footprint, rotateVertical, grid, roads, blockerData))
                        continue;

                    resolvedOrigin = candidate;
                    return true;
                }
            }
        }

        for (int y = 0; y <= Mathf.Max(0, grid.Height - footprint.y); y++)
        {
            for (int x = 0; x <= Mathf.Max(0, grid.Width - footprint.x); x++)
            {
                Vector2Int candidate = new(x, y);
                if (!context.IsPlacementValid(definition, candidate, footprint, rotateVertical, grid, roads, blockerData))
                    continue;

                resolvedOrigin = candidate;
                return true;
            }
        }

        return false;
    }

    public static BuildingDefinition CloneDefinitionWithFootprint(BuildingDefinition definition, Vector2Int footprintCells)
    {
        if (definition == null)
            return null;

        return new BuildingDefinition
        {
            DisplayName = definition.DisplayName,
            Description = definition.Description,
            MaxHealth = definition.MaxHealth,
            ProductionSlots = definition.ProductionSlots,
            SpawnUnitPrefab = definition.SpawnUnitPrefab,
            SecondarySpawnUnitPrefab = definition.SecondarySpawnUnitPrefab,
            TertiarySpawnUnitPrefab = definition.TertiarySpawnUnitPrefab,
            QuaternarySpawnUnitPrefab = definition.QuaternarySpawnUnitPrefab,
            Prefab = definition.Prefab,
            DestroyedVisualPrefab = definition.DestroyedVisualPrefab,
            FootprintCells = footprintCells,
            Role = definition.Role,
            IsWall = definition.IsWall,
            ProductionDurationSeconds = definition.ProductionDurationSeconds,
            OilBarrelsPerDay = definition.OilBarrelsPerDay,
            OilStorageCapacity = definition.OilStorageCapacity,
            FuelBarrelsPerDay = definition.FuelBarrelsPerDay,
            FuelStorageCapacity = definition.FuelStorageCapacity,
            RefugeeCapacity = definition.RefugeeCapacity,
            RefugeeUpkeepPerCitizenPerDay = definition.RefugeeUpkeepPerCitizenPerDay,
            ThreatDetectionKind = definition.ThreatDetectionKind,
            ThreatDetectionRadiusCells = definition.ThreatDetectionRadiusCells,
            LocalBounds = definition.LocalBounds,
            HasLocalBounds = definition.HasLocalBounds,
            VisualTemplate = definition.VisualTemplate,
            GeneratedMeshes = definition.GeneratedMeshes,
            ProductionSpawnLocalPositions = definition.ProductionSpawnLocalPositions,
            HasRunway = definition.HasRunway,
            RunwayLocalPosition = definition.RunwayLocalPosition,
            RunwayLocalRotation = definition.RunwayLocalRotation,
            RunwayHalfExtents = definition.RunwayHalfExtents
        };
    }

    private static BuildingDefinition CreateRuntimeWallDefinition(Context context, GameObject prefab)
    {
        BuildingDefinition definition = context.DefinitionSystem.CreateRuntimeBuildingDefinition(
            prefab,
            prefab.name,
            "Defensive wall.",
            new Vector2Int(4, 1),
            500,
            context.RunwaySystem);
        definition.IsWall = true;
        return definition;
    }
}
