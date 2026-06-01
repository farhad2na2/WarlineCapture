using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class MapBuildingPlacementSpawnSystem
{
    private const int MaxPlacementsPerUpdate = 32;

    public delegate bool TryGetGridDataDelegate(
        out Entity gridEntity,
        out GridConfig grid,
        out DynamicBuffer<GridRoad> roads,
        out DynamicBlockerData blockerData);

    public readonly struct Context
    {
        public readonly MapBuildingPlacementConfig Config;
        public readonly Transform AuthoringBuildingsRoot;
        public readonly BuildingRuntimeSpawnSystem RuntimeSpawnSystem;
        public readonly BuildingRuntimeSpawnSystem.Context RuntimeSpawnContext;
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly Action<string> LogWarning;

        public Context(
            MapBuildingPlacementConfig config,
            Transform authoringBuildingsRoot,
            BuildingRuntimeSpawnSystem runtimeSpawnSystem,
            BuildingRuntimeSpawnSystem.Context runtimeSpawnContext,
            TryGetGridDataDelegate tryGetGridData,
            Action<string> logWarning)
        {
            Config = config;
            AuthoringBuildingsRoot = authoringBuildingsRoot;
            RuntimeSpawnSystem = runtimeSpawnSystem;
            RuntimeSpawnContext = runtimeSpawnContext;
            TryGetGridData = tryGetGridData;
            LogWarning = logWarning;
        }
    }

    private bool _queued;
    private bool _authoringHidden;
    private bool _warnedMissingConfig;
    private int _nextPlacementIndex;
    private bool _warnedFailedPlacement;

    public void Update(Context context)
    {
        if (context.Config == null || !context.Config.SpawnOnMatchStart)
            return;

        if (_queued)
        {
            HideAuthoringVisuals(context);
            return;
        }

        SpawnPlacements(context);
    }

    private void SpawnPlacements(Context context)
    {
        if (context.Config.Placements == null || context.Config.Placements.Count == 0)
        {
            WarnOnce(ref _warnedMissingConfig, context, "[MapBuildingPlacement] no baked map building placements configured.");
            _queued = true;
            return;
        }

        if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            return;

        int processed = 0;
        for (; _nextPlacementIndex < context.Config.Placements.Count && processed < MaxPlacementsPerUpdate; _nextPlacementIndex++, processed++)
        {
            MapBuildingPlacementConfigEntry placement = context.Config.Placements[_nextPlacementIndex];
            if (placement == null || placement.BuildingPrefab == null)
                continue;

            if (!context.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint(
                    context.RuntimeSpawnContext,
                    placement.BuildingPrefab,
                    placement.RotateVertical,
                    out Vector2Int footprint))
            {
                context.LogWarning?.Invoke($"[MapBuildingPlacement] skipped {placement.SourcePath}: could not resolve footprint for {placement.BuildingPrefab.name}.");
                continue;
            }

            Vector3 worldCenter = placement.WorldCenter;
            int2 centerCell = GridUtils.WorldToCell(grid, new float3(worldCenter.x, worldCenter.y, worldCenter.z));
            int2 originCell = CenterCellToOrigin(centerCell, footprint, grid);
            if (!TrySpawnAuthoredPlacement(context, placement, new Vector2Int(originCell.x, originCell.y), footprint))
            {
                WarnOnce(
                    ref _warnedFailedPlacement,
                    context,
                    $"[MapBuildingPlacement] at least one authored building failed to register. First failed source={placement.SourcePath} prefab={placement.BuildingPrefab.name}.");
            }
        }

        if (_nextPlacementIndex >= context.Config.Placements.Count)
        {
            _queued = true;
            HideAuthoringVisuals(context);
        }
    }

    private static bool TrySpawnAuthoredPlacement(
        Context context,
        MapBuildingPlacementConfigEntry placement,
        Vector2Int originCell,
        Vector2Int footprint)
    {
        BuildingRuntimeSpawnSystem.Context spawnContext = context.RuntimeSpawnContext;
        if (spawnContext.DefinitionSystem == null ||
            spawnContext.CreateBuildingVisualInstance == null ||
            spawnContext.RegisterRuntimeBuilding == null)
        {
            return false;
        }

        BuildingDefinition definition = spawnContext.DefinitionSystem.CreateRuntimeBuildingDefinition(
            placement.BuildingPrefab,
            placement.BuildingPrefab.name,
            "Authored map building.",
            footprint,
            500,
            spawnContext.RunwaySystem);
        GameObject instance = spawnContext.CreateBuildingVisualInstance(definition, spawnContext.BuildingRoot);
        if (instance == null)
            return false;

        Transform instanceTransform = instance.transform;
        instanceTransform.SetPositionAndRotation(
            placement.WorldPosition,
            Quaternion.Euler(placement.WorldEulerAngles));
        instanceTransform.localScale = placement.WorldScale;
        RuntimeBuildingData building = spawnContext.RegisterRuntimeBuilding(
            BuildingRuntimeSpawnSystem.CloneDefinitionWithFootprint(definition, footprint),
            instance,
            originCell,
            true);
        if (building == null)
            return false;

        spawnContext.SetRuntimeBuildingOwnerFaction?.Invoke(building, placement.FactionId);
        return true;
    }

    private void HideAuthoringVisuals(Context context)
    {
        if (_authoringHidden || !context.Config.HideAuthoringVisualsAfterSpawn || context.AuthoringBuildingsRoot == null)
            return;

        context.AuthoringBuildingsRoot.gameObject.SetActive(false);
        _authoringHidden = true;
    }

    private static int2 CenterCellToOrigin(int2 centerCell, Vector2Int footprint, GridConfig grid)
    {
        int originX = centerCell.x - Mathf.Max(0, footprint.x - 1) / 2;
        int originY = centerCell.y - Mathf.Max(0, footprint.y - 1) / 2;
        return new int2(
            Mathf.Clamp(originX, 0, Mathf.Max(0, grid.Width - footprint.x)),
            Mathf.Clamp(originY, 0, Mathf.Max(0, grid.Height - footprint.y)));
    }

    private static void WarnOnce(ref bool flag, Context context, string message)
    {
        if (flag)
            return;

        flag = true;
        context.LogWarning?.Invoke(message);
    }
}
