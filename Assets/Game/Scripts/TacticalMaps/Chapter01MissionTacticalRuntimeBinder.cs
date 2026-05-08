using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Chapter01MissionTacticalRuntimeBinder : MonoBehaviour
{
    private const string DefaultMissionId = "saga.ch01.m01.first_contact";
    private const string PlayerSpawnAnchorId = "player_spawn.command_squad";
    private const string EnemySpawnAnchorId = "enemy_spawn.patrol_start";
    private const int PlayerUnitCap = 1;
    private const int EnemyUnitCap = 1;

    [SerializeField] private TacticalMapRuntimeLoader tacticalMapLoader;
    [SerializeField] private TacticalMapDefinition[] missionDefinitions = Array.Empty<TacticalMapDefinition>();
    [SerializeField] private GridAuthoringConfig[] missionGridConfigs = Array.Empty<GridAuthoringConfig>();
    [SerializeField] private bool useDefaultMissionWhenNoSession = true;

    public TacticalMapDefinition ActiveDefinition { get; private set; }
    public GridAuthoringConfig ActiveGridConfig { get; private set; }
    public TacticalMapRuntimeLoader TacticalMapLoader => tacticalMapLoader;

    public bool TryApplyActiveMission(Camera gameplayCamera)
    {
        string missionId = WarlineCaptureMissionSession.HasActiveMission
            ? WarlineCaptureMissionSession.ActiveMission.MissionId
            : (useDefaultMissionWhenNoSession ? DefaultMissionId : string.Empty);

        if (string.IsNullOrWhiteSpace(missionId) || !TryResolveMission(missionId, out TacticalMapDefinition definition, out GridAuthoringConfig gridConfig))
            return false;

        ActiveDefinition = definition;
        ActiveGridConfig = gridConfig;
        EnsureLoader();
        tacticalMapLoader.Configure(definition, gridConfig, gameplayCamera);
        tacticalMapLoader.Load();

        ApplyGridToEcsWorld(definition, gridConfig);
        ApplyInitialSpawnsToEcsWorld(definition);
        Debug.Log($"CHAPTER01_TACTICAL_BINDER_APPLIED mission={missionId} mapId={definition.MapId} grid={definition.GridWidth}x{definition.GridHeight}");
        return true;
    }

    private bool TryResolveMission(string missionId, out TacticalMapDefinition definition, out GridAuthoringConfig gridConfig)
    {
        for (int i = 0; i < missionDefinitions.Length; i++)
        {
            TacticalMapDefinition candidate = missionDefinitions[i];
            if (candidate == null || candidate.MissionId != missionId)
                continue;

            definition = candidate;
            gridConfig = i < missionGridConfigs.Length ? missionGridConfigs[i] : null;
            return true;
        }

        definition = null;
        gridConfig = null;
        return false;
    }

    private void EnsureLoader()
    {
        if (tacticalMapLoader != null)
            return;

        tacticalMapLoader = GetComponent<TacticalMapRuntimeLoader>();
        if (tacticalMapLoader == null)
            tacticalMapLoader = gameObject.AddComponent<TacticalMapRuntimeLoader>();
    }

    private static void ApplyGridToEcsWorld(TacticalMapDefinition definition, GridAuthoringConfig gridConfig)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || definition == null)
            return;

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadWrite<GridConfig>(),
            ComponentType.ReadWrite<GridWalkable>(),
            ComponentType.ReadWrite<GridRoad>());
        if (query.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = query.GetSingletonEntity();
        GridConfig grid = new()
        {
            Width = math.max(1, definition.GridWidth),
            Height = math.max(1, definition.GridHeight),
            CellSize = math.max(0.001f, definition.CellSize),
            Origin = new float3(definition.WorldOrigin.x, 0f, definition.WorldOrigin.y)
        };
        em.SetComponentData(gridEntity, grid);

        int size = grid.Width * grid.Height;
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBuffer<GridRoad> roads = em.GetBuffer<GridRoad>(gridEntity);
        DynamicBuffer<GridRoadSidewalk> sidewalks = em.HasBuffer<GridRoadSidewalk>(gridEntity)
            ? em.GetBuffer<GridRoadSidewalk>(gridEntity)
            : em.AddBuffer<GridRoadSidewalk>(gridEntity);
        DynamicBuffer<GridRoadDirt> dirtRoads = em.HasBuffer<GridRoadDirt>(gridEntity)
            ? em.GetBuffer<GridRoadDirt>(gridEntity)
            : em.AddBuffer<GridRoadDirt>(gridEntity);

        ResizeAndClear(walkable, size, 1);
        ResizeAndClear(roads, size, 0);
        ResizeAndClear(sidewalks, size, 0);
        ResizeAndClear(dirtRoads, size, 0);

        if (gridConfig?.BlockedCells != null)
        {
            foreach (Vector2Int blockedCell in gridConfig.BlockedCells)
            {
                if ((uint)blockedCell.x >= (uint)grid.Width || (uint)blockedCell.y >= (uint)grid.Height)
                    continue;

                walkable[blockedCell.x + blockedCell.y * grid.Width] = new GridWalkable { Value = 0 };
            }
        }

        MarkSurfaceCells(definition, TacticalMapSurfaceType.MainRoad, grid.Width, grid.Height, roads, 1);
        MarkSurfaceCells(definition, TacticalMapSurfaceType.RoadShoulder, grid.Width, grid.Height, sidewalks, 1);
    }

    private static void ResizeAndClear(DynamicBuffer<GridWalkable> buffer, int size, byte value)
    {
        buffer.ResizeUninitialized(size);
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new GridWalkable { Value = value };
    }

    private static void ResizeAndClear(DynamicBuffer<GridRoad> buffer, int size, byte value)
    {
        buffer.ResizeUninitialized(size);
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new GridRoad { Value = value };
    }

    private static void ResizeAndClear(DynamicBuffer<GridRoadSidewalk> buffer, int size, byte value)
    {
        buffer.ResizeUninitialized(size);
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new GridRoadSidewalk { Value = value };
    }

    private static void ResizeAndClear(DynamicBuffer<GridRoadDirt> buffer, int size, byte value)
    {
        buffer.ResizeUninitialized(size);
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = new GridRoadDirt { Value = value };
    }

    private static void MarkSurfaceCells(TacticalMapDefinition definition, TacticalMapSurfaceType type, int width, int height, DynamicBuffer<GridRoad> buffer, byte value)
    {
        foreach (TacticalMapSurface surface in definition.Surfaces)
        {
            if (surface.Type != type)
                continue;

            foreach (int index in EnumerateSurfaceIndices(surface.NormalizedBounds, width, height))
                buffer[index] = new GridRoad { Value = value };
        }
    }

    private static void MarkSurfaceCells(TacticalMapDefinition definition, TacticalMapSurfaceType type, int width, int height, DynamicBuffer<GridRoadSidewalk> buffer, byte value)
    {
        foreach (TacticalMapSurface surface in definition.Surfaces)
        {
            if (surface.Type != type)
                continue;

            foreach (int index in EnumerateSurfaceIndices(surface.NormalizedBounds, width, height))
                buffer[index] = new GridRoadSidewalk { Value = value };
        }
    }

    private static System.Collections.Generic.IEnumerable<int> EnumerateSurfaceIndices(Rect normalizedBounds, int width, int height)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt(normalizedBounds.xMin * width), 0, width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(normalizedBounds.xMax * width), 0, width);
        int minY = Mathf.Clamp(Mathf.FloorToInt(normalizedBounds.yMin * height), 0, height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(normalizedBounds.yMax * height), 0, height);

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
                yield return x + y * width;
        }
    }

    private static void ApplyInitialSpawnsToEcsWorld(TacticalMapDefinition definition)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || definition == null)
            return;

        EntityManager em = world.EntityManager;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<InitialUnitsSpawnConfig>());
        if (query.IsEmptyIgnoreFilter)
            return;

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
            config.BlockerCount = 0;
            config.SpawnRadiusCells = 3;
            config.CreateFactionBases = 0;
            em.SetComponentData(entity, config);

            if (em.HasBuffer<InitialUnitsFactionSpawnEntry>(entity))
                ApplyFactionSpawnAnchors(em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity), definition);
            if (em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(entity))
                ApplyCompactMissionUnitRoster(em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity));

            RemoveSpawnProgress(em, entity);
        }
    }

    private static void ApplyFactionSpawnAnchors(DynamicBuffer<InitialUnitsFactionSpawnEntry> spawns, TacticalMapDefinition definition)
    {
        Vector2Int playerSpawn = definition.TryGetAnchor(PlayerSpawnAnchorId, out TacticalMapAnchor playerAnchor)
            ? definition.NormalizedToCell(playerAnchor.NormalizedPosition)
            : new Vector2Int(14, 18);
        Vector2Int enemySpawn = definition.TryGetAnchor(EnemySpawnAnchorId, out TacticalMapAnchor enemyAnchor)
            ? definition.NormalizedToCell(enemyAnchor.NormalizedPosition)
            : new Vector2Int(49, 19);

        for (int i = 0; i < spawns.Length; i++)
        {
            InitialUnitsFactionSpawnEntry spawn = spawns[i];
            spawn.SpawnCell = spawn.FactionId == 0
                ? new int2(playerSpawn.x, playerSpawn.y)
                : new int2(enemySpawn.x, enemySpawn.y);
            spawns[i] = spawn;
        }
    }

    private static void ApplyCompactMissionUnitRoster(DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> units)
    {
        int playerCount = 0;
        int enemyCount = 0;
        for (int i = 0; i < units.Length; i++)
        {
            InitialUnitsFactionUnitSpawnEntry unit = units[i];
            int cap = unit.FactionId == 0 ? PlayerUnitCap : EnemyUnitCap;
            int assigned = unit.FactionId == 0 ? playerCount : enemyCount;
            if (assigned >= cap || unit.Prefab == Entity.Null)
            {
                unit.Count = 0;
                unit.SpawnOffset = int2.zero;
                units[i] = unit;
                continue;
            }

            unit.Count = 1;
            unit.SpawnOffset = ResolveFormationOffset(assigned, unit.FactionId == 0);
            units[i] = unit;

            if (unit.FactionId == 0)
                playerCount++;
            else
                enemyCount++;
        }
    }

    private static int2 ResolveFormationOffset(int index, bool player)
    {
        int direction = player ? 1 : -1;
        return index switch
        {
            0 => new int2(0, 0),
            1 => new int2(-1 * direction, 1),
            2 => new int2(-1 * direction, -1),
            3 => new int2(-2 * direction, 0),
            4 => new int2(-2 * direction, 1),
            _ => new int2(-2 * direction, -1)
        };
    }

    private static void RemoveSpawnProgress(EntityManager em, Entity entity)
    {
        if (em.HasComponent<InitialUnitsSpawnInitialized>(entity))
            em.RemoveComponent<InitialUnitsSpawnInitialized>(entity);
        if (em.HasComponent<InitialUnitsSpawnProgress>(entity))
            em.RemoveComponent<InitialUnitsSpawnProgress>(entity);
        if (em.HasBuffer<InitialUnitsFactionUnitSpawnProgress>(entity))
            em.RemoveComponent<InitialUnitsFactionUnitSpawnProgress>(entity);
    }
}
