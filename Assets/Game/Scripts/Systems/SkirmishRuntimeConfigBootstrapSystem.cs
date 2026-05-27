using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SkirmishRuntimeConfigBootstrapSystem
{
    public void EnsureRuntimeConfigs(
        World world,
        InitialUnitsSpawnerAuthoringConfig initialUnitsConfig,
        UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig)
    {
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        int prefabCandidateCount = CountPrefabCandidates(em);
        int registryEntries = EnsureUnitPrefabRegistry(em, unitPrefabRegistryConfig);
        int initialUnitEntries = EnsureInitialUnitsSpawnConfig(em, initialUnitsConfig);

        Debug.Log(
            $"[SkirmishRuntimeConfig] prefabCandidates={prefabCandidateCount} " +
            $"unitRegistryEntries={registryEntries} initialUnitEntries={initialUnitEntries}");
    }

    private static int EnsureUnitPrefabRegistry(
        EntityManager em,
        UnitPrefabRegistryAuthoringConfig config)
    {
        using EntityQuery existingQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitPrefabRegistryTag>());
        if (!existingQuery.IsEmptyIgnoreFilter)
        {
            Entity existing = existingQuery.GetSingletonEntity();
            return em.HasBuffer<UnitPrefabRegistryEntry>(existing)
                ? em.GetBuffer<UnitPrefabRegistryEntry>(existing).Length
                : 0;
        }

        if (config == null || config.UnitSpawnPrefabs == null || config.UnitSpawnPrefabs.Count == 0)
            return 0;

        Entity entity = em.CreateEntity(typeof(UnitPrefabRegistryTag));
        DynamicBuffer<UnitPrefabRegistryEntry> entries = em.AddBuffer<UnitPrefabRegistryEntry>(entity);
        for (int i = 0; i < config.UnitSpawnPrefabs.Count; i++)
        {
            GameObject prefab = config.UnitSpawnPrefabs[i];
            entries.Add(new UnitPrefabRegistryEntry
            {
                Prefab = TryResolvePrefabEntity(em, prefab, out Entity prefabEntity)
                    ? prefabEntity
                    : Entity.Null
            });
        }

        return entries.Length;
    }

    private static int EnsureInitialUnitsSpawnConfig(
        EntityManager em,
        InitialUnitsSpawnerAuthoringConfig config)
    {
        using EntityQuery existingQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        if (!existingQuery.IsEmptyIgnoreFilter)
        {
            Entity existing = existingQuery.GetSingletonEntity();
            return em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(existing)
                ? em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(existing).Length
                : 0;
        }

        if (config == null)
            return 0;

        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new InitialUnitsSpawnConfig
        {
            BlockerPrefab = TryResolvePrefabEntity(em, config.BlockerPrefab, out Entity blockerPrefab)
                ? blockerPrefab
                : Entity.Null,
            BlockerCount = config.BlockerCount,
            SpawnRadiusCells = math.max(0, config.SpawnRadiusCells),
            RespawnDelaySeconds = math.max(0.01f, config.RespawnDelaySeconds),
            RandomSeed = math.max(1u, config.RandomSeed),
            InitialDollars = config.InitialDollars,
            InitialOil = config.InitialOil,
            InitialFuel = config.InitialFuel,
            CreateFactionBases = config.CreateFactionBases ? (byte)1 : (byte)0,
            BaseWallPrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(config.BaseWallPrefab, "Wall_Dirt_Straight")),
            BaseGatePrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(config.BaseGatePrefab, "Building_Road_Barrier")),
            BaseCoreBuildingPrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(config.BaseCoreBuildingPrefab, "Building_Ammunition_Depot")),
            BaseHalfWidthCells = config.BaseHalfWidthCells,
            BaseHalfHeightCells = config.BaseHalfHeightCells,
            BaseMinimumUnitsPerFaction = config.BaseMinimumUnitsPerFaction
        });

        em.AddComponentData(entity, new InitialUnitsBlockerChurnConfig
        {
            Enabled = config.EnableBlockerChurn,
            IntervalSeconds = config.ChurnIntervalSeconds,
            AddRemovePerInterval = config.AddRemovePerInterval
        });
        em.AddComponentData(entity, new InitialUnitsBlockerChurnState
        {
            Timer = 0f,
            RandomState = math.max(1u, config.RandomSeed),
            BlockerPrefab = TryResolvePrefabEntity(em, config.BlockerPrefab, out Entity churnBlockerPrefab)
                ? churnBlockerPrefab
                : Entity.Null
        });

        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.AddBuffer<InitialUnitsFactionSpawnEntry>(entity);
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns = em.AddBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
        DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns = em.AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity);
        FillInitialSpawnBuffers(em, config, factionSpawns, unitSpawns, buildingSpawns);
        return unitSpawns.Length;
    }

    private static void FillInitialSpawnBuffers(
        EntityManager em,
        InitialUnitsSpawnerAuthoringConfig config,
        DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns,
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns,
        DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns)
    {
        if (config.Factions == null)
            return;

        for (int factionIndex = 0; factionIndex < config.Factions.Count; factionIndex++)
        {
            InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[factionIndex];
            if (faction == null)
                continue;

            byte factionId = (byte)math.clamp(faction.FactionId, 0, 255);
            factionSpawns.Add(new InitialUnitsFactionSpawnEntry
            {
                FactionId = factionId,
                SpawnCell = new int2(faction.SpawnCell.x, faction.SpawnCell.y)
            });

            GameObject firstUnitPrefab = null;
            int configuredUnitCount = 0;
            if (faction.Units != null)
            {
                for (int unitIndex = 0; unitIndex < faction.Units.Count; unitIndex++)
                {
                    InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry unit = faction.Units[unitIndex];
                    if (unit == null || unit.Prefab == null || unit.Count <= 0)
                        continue;

                    firstUnitPrefab ??= unit.Prefab;
                    configuredUnitCount += math.max(0, unit.Count);
                    AddUnitSpawnEntry(em, unitSpawns, factionId, unit.Prefab, unit.Count, unit.SpawnOffset);
                }
            }

            if (config.CreateFactionBases && firstUnitPrefab != null && configuredUnitCount < config.BaseMinimumUnitsPerFaction)
            {
                AddUnitSpawnEntry(
                    em,
                    unitSpawns,
                    factionId,
                    firstUnitPrefab,
                    config.BaseMinimumUnitsPerFaction - configuredUnitCount,
                    default);
            }

            if (faction.Buildings == null)
                continue;

            for (int buildingIndex = 0; buildingIndex < faction.Buildings.Count; buildingIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry building = faction.Buildings[buildingIndex];
                if (building == null || building.Prefab == null)
                    continue;

                buildingSpawns.Add(new InitialUnitsFactionBuildingSpawnEntry
                {
                    FactionId = factionId,
                    Prefab = TryResolvePrefabEntity(em, building.Prefab, out Entity buildingPrefab)
                        ? buildingPrefab
                        : Entity.Null,
                    PrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(building.Prefab)),
                    OriginOffset = new int2(building.OriginOffset.x, building.OriginOffset.y)
                });
            }
        }
    }

    private static void AddUnitSpawnEntry(
        EntityManager em,
        DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns,
        byte factionId,
        GameObject prefab,
        int count,
        Vector2Int spawnOffset)
    {
        if (!TryResolvePrefabEntity(em, prefab, out Entity prefabEntity))
        {
            Debug.LogWarning($"[SkirmishRuntimeConfig] missing converted unit prefab. prefab={GetPrefabName(prefab)}");
            return;
        }

        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry
        {
            FactionId = factionId,
            Prefab = prefabEntity,
            Count = math.max(0, count),
            SpawnOffset = new int2(spawnOffset.x, spawnOffset.y)
        });
    }

    private static bool TryResolvePrefabEntity(EntityManager em, GameObject prefab, out Entity prefabEntity)
    {
        prefabEntity = Entity.Null;
        string targetName = GetPrefabName(prefab);
        if (string.IsNullOrEmpty(targetName))
            return false;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!NamesMatch(em.GetName(entity), targetName))
                continue;

            prefabEntity = entity;
            return true;
        }

        return false;
    }

    private static int CountPrefabCandidates(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
        return query.CalculateEntityCount();
    }

    private static string GetBuildingLookupKey(GameObject prefab, string fallback = "")
    {
        if (prefab == null)
            return fallback;

        return prefab.name.Trim().ToLowerInvariant();
    }

    private static string GetPrefabName(GameObject prefab)
    {
        return prefab != null ? prefab.name : string.Empty;
    }

    private static bool NamesMatch(string candidateName, string targetName)
    {
        if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(targetName))
            return false;

        return string.Equals(candidateName, targetName, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(candidateName.Replace(" (Clone)", string.Empty), targetName, System.StringComparison.OrdinalIgnoreCase);
    }
}
