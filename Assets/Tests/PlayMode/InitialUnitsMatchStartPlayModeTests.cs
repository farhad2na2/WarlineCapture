using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class InitialUnitsMatchStartPlayModeTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;

    [TearDown]
    public void TearDown()
    {
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
    }

    [Test]
    public void MatchStartPlayRequested_SpawnsConfiguredInitialUnit()
    {
        using var world = new World("MatchStartPlayRequested_SpawnsConfiguredInitialUnit");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 24, 24);
        CreateBuildingRuntimeState(em);
        CreateStartedMatchBoundary(em);
        Entity runtimeState = CreateRuntimeGameplayState(em, playRequested: false);
        Entity prefab = CreateInitialUnitPrefab(em);
        Entity config = CreateInitialSpawnConfig(em, prefab);
        SystemHandle spawnSystem = world.CreateSystem<InitialUnitsSpawnSystem>();

        spawnSystem.Update(world.Unmanaged);
        Assert.AreEqual(0, CountSpawnedInitialUnits(em), "Initial spawn must wait until match start requests gameplay.");
        Assert.IsFalse(em.HasComponent<InitialUnitsSpawnProgress>(config), "Spawn progress should not initialize before gameplay starts.");

        RuntimeGameplayStateComponent state = em.GetComponentData<RuntimeGameplayStateComponent>(runtimeState);
        state.PlayRequested = 1;
        em.SetComponentData(runtimeState, state);

        spawnSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, CountSpawnedInitialUnits(em));
        Assert.IsTrue(em.HasComponent<InitialUnitsSpawnInitialized>(config));
        using EntityQuery spawnedQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>(),
            ComponentType.ReadOnly<UnitPrevWorldPos>());
        using NativeArray<Entity> spawned = spawnedQuery.ToEntityArray(Allocator.Temp);
        Assert.AreEqual(1, spawned.Length);
        Entity unit = spawned[0];
        Assert.AreEqual(FactionIdentity.PlayerFactionId, em.GetComponentData<Faction>(unit).Id);
        Assert.AreEqual(new int2(13, 12), em.GetComponentData<UnitGrid>(unit).Cell);
        Assert.AreEqual(new FixedString64Bytes("playmode_initial_soldier"), em.GetComponentData<UnitSourcePrefabKey>(unit).Value);
        Assert.AreEqual(new float3(13.5f, 0f, 12.5f), em.GetComponentData<UnitPrevWorldPos>(unit).Value);
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static void CreateBuildingRuntimeState(EntityManager em)
    {
        Entity boundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        em.AddBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
        em.AddBuffer<BuildingFactionProductionSpawnPointReadModel>(boundary);
        em.AddBuffer<BuildingRuntimeSpawnRequest>(boundary);
    }

    private static void CreateStartedMatchBoundary(EntityManager em)
    {
        Entity matchStart = em.CreateEntity(typeof(MatchStartStateComponent), typeof(MatchStartQueueComponent), typeof(MatchStartProgressComponent));
        em.SetComponentData(matchStart, new MatchStartQueueComponent
        {
            LastRequestId = 1,
            ActiveRequestId = 1,
            HasStarted = 1,
            LastStatus = MatchStartStatusKind.Started
        });
        em.SetComponentData(matchStart, new MatchStartProgressComponent
        {
            Progress01 = 1f,
            Status = new FixedString64Bytes("Started")
        });
        em.AddBuffer<MatchStartRequestElement>(matchStart);
        DynamicBuffer<MatchStartResultElement> results = em.AddBuffer<MatchStartResultElement>(matchStart);
        results.Add(new MatchStartResultElement
        {
            RequestId = 1,
            Status = MatchStartStatusKind.Started,
            Message = new FixedString128Bytes("PlayMode match start smoke")
        });
    }

    private static Entity CreateRuntimeGameplayState(EntityManager em, bool playRequested)
    {
        Entity entity = em.CreateEntity(
            typeof(RuntimeGameplayStateComponent),
            typeof(RuntimeCameraFocusRequestComponent));
        em.SetComponentData(entity, new RuntimeGameplayStateComponent { PlayRequested = (byte)(playRequested ? 1 : 0) });
        return entity;
    }

    private static Entity CreateInitialUnitPrefab(EntityManager em)
    {
        Entity prefab = em.CreateEntity(typeof(UnitFootprint), typeof(UnitSourcePrefabKey));
        em.SetComponentData(prefab, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(prefab, new UnitSourcePrefabKey { Value = new FixedString64Bytes("playmode_initial_soldier") });
        return prefab;
    }

    private static Entity CreateInitialSpawnConfig(EntityManager em, Entity prefab)
    {
        Entity config = em.CreateEntity(typeof(InitialUnitsSpawnConfig));
        em.SetComponentData(config, new InitialUnitsSpawnConfig
        {
            SpawnRadiusCells = 0,
            RandomSeed = 17,
            RespawnDelaySeconds = 5f,
            CreateFactionBases = 0
        });

        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.AddBuffer<InitialUnitsFactionSpawnEntry>(config);
        factionSpawns.Add(new InitialUnitsFactionSpawnEntry
        {
            FactionId = FactionIdentity.PlayerFactionId,
            SpawnCell = new int2(12, 12)
        });

        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns = em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(config);
        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Prefab = prefab,
            Count = 1,
            SpawnOffset = new int2(1, 0)
        });
        em.AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(config);
        return config;
    }

    private static int CountSpawnedInitialUnits(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>());
        return query.CalculateEntityCount();
    }
}
#endif
