using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public sealed class CustomGameStartupSystemHelper
    {
        private readonly EntityManager _entityManager;

        public readonly struct Result
        {
            public readonly bool Initialized;
            public readonly int FactionCount;
            public readonly int UnitRosterCount;
            public readonly int InitialUnitEntryCount;
            public readonly int InitialBuildingEntryCount;
            public readonly int UnitRegistryEntryCount;
            public readonly int VisualEntryCount;
            public readonly int MissingVisualReferenceCount;

            public Result(
                bool initialized,
                int factionCount,
                int unitRosterCount,
                int initialUnitEntryCount,
                int initialBuildingEntryCount,
                int unitRegistryEntryCount,
                int visualEntryCount,
                int missingVisualReferenceCount)
            {
                Initialized = initialized;
                FactionCount = factionCount;
                UnitRosterCount = unitRosterCount;
                InitialUnitEntryCount = initialUnitEntryCount;
                InitialBuildingEntryCount = initialBuildingEntryCount;
                UnitRegistryEntryCount = unitRegistryEntryCount;
                VisualEntryCount = visualEntryCount;
                MissingVisualReferenceCount = missingVisualReferenceCount;
            }
        }

        public CustomGameStartupSystemHelper(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        public Result Initialize(CustomGameStartupConfig config)
        {
            if (config == null)
                return default;

            EntityManager em = _entityManager;
            Entity entity = GetOrCreateStartupEntity(em);
            CustomGameMapConfig map = config.MapConfig;

            ResetInitialSpawnLifecycle(em, entity);
            RemoveRuntimeSpawnRequestsForPlan(em, entity);
            SetInitialUnitsConfig(em, entity, map);
            EnsureBuffer<InitialUnitsFactionSpawnEntry>(em, entity);
            EnsureBuffer<InitialUnitsFactionUnitSpawnEntry>(em, entity);
            EnsureBuffer<InitialUnitsFactionBuildingSpawnEntry>(em, entity);
            EnsureBuffer<CustomGameFactionUnitSourceSpawnEntry>(em, entity);
            EnsureBuffer<CustomGameUnitSourceRegistryEntry>(em, entity);
            EnsureBuffer<CustomGameVisualRegistryEntry>(em, entity);

            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns =
                em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> initialBuildings =
                em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity);
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceUnitSpawns =
                em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity);
            DynamicBuffer<CustomGameUnitSourceRegistryEntry> unitSources =
                em.GetBuffer<CustomGameUnitSourceRegistryEntry>(entity);
            DynamicBuffer<CustomGameVisualRegistryEntry> visualEntries =
                em.GetBuffer<CustomGameVisualRegistryEntry>(entity);

            factionSpawns.Clear();
            initialUnits.Clear();
            initialBuildings.Clear();
            sourceUnitSpawns.Clear();
            unitSources.Clear();
            visualEntries.Clear();

            FillFactionBuffers(config.FactionConfig, factionSpawns, initialUnits, initialBuildings, sourceUnitSpawns);
            FillUnitSourceRegistry(config.UnitRosterConfig, unitSources);
            FillVisualRegistry(config.VisualRegistryConfig, visualEntries);

            int factionCount = factionSpawns.Length;
            int initialUnitCount = sourceUnitSpawns.Length;
            int initialBuildingCount = initialBuildings.Length;
            int unitRosterCount = unitSources.Length;
            int visualCount = visualEntries.Length;

            em.SetComponentData(entity, new CustomGameStartupStateComponent
            {
                GameModeId = ToFixed64(config.GameModeId),
                GridWidth = map != null ? map.GridWidth : 0,
                GridHeight = map != null ? map.GridHeight : 0,
                CellSize = map != null ? map.CellSize : 0f,
                GridOrigin = map != null ? map.GridOrigin : default,
                FactionCount = factionCount,
                UnitRosterCount = unitRosterCount,
                InitialUnitEntryCount = initialUnitCount,
                VisualEntryCount = visualCount
            });

            return new Result(true, factionCount, unitRosterCount, initialUnitCount, initialBuildingCount, unitRosterCount, visualCount, 0);
        }

        public Result InitializeFromLegacyConfigs(
            InitialUnitsSpawnerAuthoringConfig initialUnitsConfig,
            UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig)
        {
            EntityManager em = _entityManager;
            Entity entity = GetOrCreateLegacyStartupEntity(em);
            RemoveDuplicateCustomInitialSpawnConfigs(em, entity);
            Dictionary<string, Entity> convertedPrefabLookup = BuildConvertedPrefabLookup(em, initialUnitsConfig, unitPrefabRegistryConfig, entity);

            ResetInitialSpawnLifecycle(em, entity);
            RemoveRuntimeSpawnRequestsForPlan(em, entity);
            SetInitialUnitsConfig(em, entity, initialUnitsConfig);
            if (em.HasComponent<UnitPrefabRegistryTag>(entity))
                em.RemoveComponent<UnitPrefabRegistryTag>(entity);
            EnsureBuffer<UnitPrefabRegistryEntry>(em, entity);
            EnsureBuffer<InitialUnitsFactionSpawnEntry>(em, entity);
            EnsureBuffer<InitialUnitsFactionUnitSpawnEntry>(em, entity);
            EnsureBuffer<InitialUnitsFactionBuildingSpawnEntry>(em, entity);
            EnsureBuffer<CustomGameFactionUnitSourceSpawnEntry>(em, entity);
            EnsureBuffer<CustomGameUnitSourceRegistryEntry>(em, entity);
            EnsureBuffer<CustomGameVisualRegistryEntry>(em, entity);

            DynamicBuffer<UnitPrefabRegistryEntry> legacyRegistry = em.GetBuffer<UnitPrefabRegistryEntry>(entity);
            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = em.GetBuffer<InitialUnitsFactionSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> initialBuildings =
                em.GetBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity);
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceUnitSpawns =
                em.GetBuffer<CustomGameFactionUnitSourceSpawnEntry>(entity);
            DynamicBuffer<CustomGameUnitSourceRegistryEntry> unitSources = em.GetBuffer<CustomGameUnitSourceRegistryEntry>(entity);
            DynamicBuffer<CustomGameVisualRegistryEntry> visualEntries = em.GetBuffer<CustomGameVisualRegistryEntry>(entity);

            legacyRegistry.Clear();
            factionSpawns.Clear();
            initialUnits.Clear();
            initialBuildings.Clear();
            sourceUnitSpawns.Clear();
            unitSources.Clear();
            visualEntries.Clear();

            FillLegacyUnitRegistry(em, unitPrefabRegistryConfig, convertedPrefabLookup, legacyRegistry, unitSources, visualEntries, out int missingVisualReferences);
            FillLegacyFactionBuffers(em, initialUnitsConfig, convertedPrefabLookup, factionSpawns, initialUnits, initialBuildings, sourceUnitSpawns);
            LogMissingConvertedPrefabResolutionIfNeeded(em, initialUnits, sourceUnitSpawns, legacyRegistry);

            int factionCount = factionSpawns.Length;
            int initialUnitCount = sourceUnitSpawns.Length;
            int initialBuildingCount = initialBuildings.Length;
            int unitRegistryCount = unitSources.Length;
            int visualCount = visualEntries.Length;

            em.SetComponentData(entity, new CustomGameStartupStateComponent
            {
                GameModeId = new FixedString64Bytes("custom.skirmish.legacy"),
                GridWidth = 0,
                GridHeight = 0,
                CellSize = 0f,
                GridOrigin = default,
                FactionCount = factionCount,
                UnitRosterCount = unitRegistryCount,
                InitialUnitEntryCount = initialUnitCount,
                VisualEntryCount = visualCount
            });

            return new Result(
                true,
                factionCount,
                unitRegistryCount,
                initialUnitCount,
                initialBuildingCount,
                unitRegistryCount,
                visualCount,
                missingVisualReferences);
        }

        private static Entity GetOrCreateStartupEntity(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<CustomGameStartupStateComponent>());
            if (!query.IsEmptyIgnoreFilter)
                return query.GetSingletonEntity();

            return em.CreateEntity(typeof(CustomGameStartupStateComponent));
        }

        private static Entity GetOrCreateLegacyStartupEntity(EntityManager em)
        {
            using (EntityQuery customInitialQuery = em.CreateEntityQuery(
                       ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                       ComponentType.ReadOnly<InitialUnitsSpawnConfig>()))
            {
                if (TryGetFirstEntity(customInitialQuery, em, out Entity customInitialEntity))
                    return customInitialEntity;
            }

            using (EntityQuery initialQuery = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>()))
            {
                if (TryGetFirstEntity(initialQuery, em, out Entity entity))
                {
                    EnsureComponent<CustomGameStartupStateComponent>(em, entity);
                    return entity;
                }
            }

            return GetOrCreateStartupEntity(em);
        }

        private static void RemoveDuplicateCustomInitialSpawnConfigs(EntityManager em, Entity startupEntity)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<CustomGameStartupStateComponent>(),
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            List<Entity> entities = new();
            CollectEntities(query, em, entities);
            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                if (entity == startupEntity)
                    continue;

                RemoveInitialSpawnComponents(em, entity);
            }
        }

        private static void RemoveInitialSpawnComponents(EntityManager em, Entity entity)
        {
            RemoveComponentIfPresent<InitialUnitsSpawnConfig>(em, entity);
            RemoveComponentIfPresent<InitialUnitsBlockerChurnConfig>(em, entity);
            RemoveComponentIfPresent<InitialUnitsBlockerChurnComponent>(em, entity);
            ResetInitialSpawnLifecycle(em, entity);
            RemoveComponentIfPresent<InitialUnitsFactionSpawnEntry>(em, entity);
            RemoveComponentIfPresent<InitialUnitsFactionUnitSpawnEntry>(em, entity);
            RemoveComponentIfPresent<InitialUnitsFactionBuildingSpawnEntry>(em, entity);
        }

        private static void ResetInitialSpawnLifecycle(EntityManager em, Entity entity)
        {
            RemoveComponentIfPresent<InitialUnitsSpawnProgress>(em, entity);
            RemoveComponentIfPresent<InitialUnitsSpawnInitialized>(em, entity);
            RemoveComponentIfPresent<InitialUnitsFactionUnitSpawnProgress>(em, entity);
        }

        private static void RemoveRuntimeSpawnRequestsForPlan(EntityManager em, Entity planEntity)
        {
            if (planEntity == Entity.Null)
                return;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadWrite<BuildingRuntimeSpawnRequest>());
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> boundaryEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int boundaryIndex = 0; boundaryIndex < boundaryEntities.Length; boundaryIndex++)
                {
                    Entity boundaryEntity = boundaryEntities[boundaryIndex];
                    DynamicBuffer<BuildingRuntimeSpawnRequest> requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
                    for (int requestIndex = requests.Length - 1; requestIndex >= 0; requestIndex--)
                    {
                        if (requests[requestIndex].PlanEntity == planEntity)
                            requests.RemoveAt(requestIndex);
                    }
                }
            }
        }

        private static void RemoveComponentIfPresent<T>(EntityManager em, Entity entity)
        {
            if (em.HasComponent<T>(entity))
                em.RemoveComponent<T>(entity);
        }

        private static void EnsureBuffer<T>(EntityManager em, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            if (!em.HasBuffer<T>(entity))
                em.AddBuffer<T>(entity);
        }

        private static void EnsureComponent<T>(EntityManager em, Entity entity)
            where T : unmanaged, IComponentData
        {
            if (!em.HasComponent<T>(entity))
                em.AddComponent<T>(entity);
        }

        private static void SetInitialUnitsConfig(EntityManager em, Entity entity, CustomGameMapConfig map)
        {
            InitialUnitsSpawnConfig config = new()
            {
                BlockerPrefab = Entity.Null,
                UnitSelectionMarkerPrefab = Entity.Null,
                UnitHealthBarPrefab = Entity.Null,
                BlockerCount = map != null ? map.BlockerCount : 0,
                SpawnRadiusCells = map != null ? math.max(0, map.SpawnRadiusCells) : 0,
                RespawnDelaySeconds = map != null ? math.max(0.01f, map.RespawnDelaySeconds) : 0.01f,
                RandomSeed = map != null ? math.max(1u, map.RandomSeed) : 1u,
                InitialDollars = map != null ? math.max(0, map.InitialDollars) : 0,
                InitialMaterials = map != null ? math.max(0, map.InitialMaterials) : 0,
                MaterialsCapacity = map != null ? math.max(0, map.MaterialsCapacity) : 0,
                InitialAiMaterials = map != null ? math.max(0, map.InitialAiMaterials) : 0,
                AiMaterialsCapacity = map != null ? math.max(0, map.AiMaterialsCapacity) : 0,
                InitialOil = map != null ? math.max(0, map.InitialOil) : 0,
                InitialFuel = map != null ? math.max(0, map.InitialFuel) : 0,
                CreateFactionBases = map != null && map.CreateFactionBases ? (byte)1 : (byte)0,
                BaseWallPrefabLookupKey = ToFixed128(map != null ? map.BaseWallLookupKey : string.Empty),
                BaseGatePrefabLookupKey = ToFixed128(map != null ? map.BaseGateLookupKey : string.Empty),
                BaseCoreBuildingPrefabLookupKey = ToFixed128(map != null ? map.BaseCoreBuildingLookupKey : string.Empty),
                BaseHalfWidthCells = map != null ? math.max(1, map.BaseHalfWidthCells) : 1,
                BaseHalfHeightCells = map != null ? math.max(1, map.BaseHalfHeightCells) : 1,
                BaseMinimumUnitsPerFaction = map != null ? math.max(0, map.BaseMinimumUnitsPerFaction) : 0
            };

            if (em.HasComponent<InitialUnitsSpawnConfig>(entity))
                em.SetComponentData(entity, config);
            else
                em.AddComponentData(entity, config);

            InitialUnitsBlockerChurnConfig churnConfig = new()
            {
                Enabled = map != null && map.EnableBlockerChurn,
                IntervalSeconds = map != null ? math.max(0.01f, map.ChurnIntervalSeconds) : 0.01f,
                AddRemovePerInterval = map != null ? math.max(0, map.AddRemovePerInterval) : 0
            };

            if (em.HasComponent<InitialUnitsBlockerChurnConfig>(entity))
                em.SetComponentData(entity, churnConfig);
            else
                em.AddComponentData(entity, churnConfig);

            InitialUnitsBlockerChurnComponent churnState = new()
            {
                Timer = 0f,
                RandomState = config.RandomSeed,
                BlockerPrefab = Entity.Null
            };

            if (em.HasComponent<InitialUnitsBlockerChurnComponent>(entity))
                em.SetComponentData(entity, churnState);
            else
                em.AddComponentData(entity, churnState);
        }

        private static void SetInitialUnitsConfig(EntityManager em, Entity entity, InitialUnitsSpawnerAuthoringConfig config)
        {
            InitialUnitsSpawnConfig initialConfig = new()
            {
                BlockerPrefab = Entity.Null,
                UnitSelectionMarkerPrefab = Entity.Null,
                UnitHealthBarPrefab = Entity.Null,
                BlockerCount = config != null ? config.BlockerCount : 0,
                SpawnRadiusCells = config != null ? math.max(0, config.SpawnRadiusCells) : 0,
                RespawnDelaySeconds = config != null ? math.max(0.01f, config.RespawnDelaySeconds) : 0.01f,
                RandomSeed = config != null ? math.max(1u, config.RandomSeed) : 1u,
                InitialDollars = config != null ? math.max(0, config.InitialDollars) : 0,
                InitialMaterials = config != null ? math.max(0, config.InitialMaterials) : 0,
                MaterialsCapacity = config != null ? math.max(0, config.MaterialsCapacity) : 0,
                InitialAiMaterials = config != null ? math.max(0, config.InitialAiMaterials) : 0,
                AiMaterialsCapacity = config != null ? math.max(0, config.AiMaterialsCapacity) : 0,
                InitialOil = config != null ? math.max(0, config.InitialOil) : 0,
                InitialFuel = config != null ? math.max(0, config.InitialFuel) : 0,
                CreateFactionBases = config != null && config.CreateFactionBases ? (byte)1 : (byte)0,
                BaseWallPrefabLookupKey = ToFixed128(GetBuildingLookupKey(config != null ? config.BaseWallPrefab : null, "Wall_Dirt_Straight")),
                BaseGatePrefabLookupKey = ToFixed128(GetBuildingLookupKey(config != null ? config.BaseGatePrefab : null, "Building_Road_Barrier")),
                BaseCoreBuildingPrefabLookupKey = ToFixed128(GetBuildingLookupKey(config != null ? config.BaseCoreBuildingPrefab : null, "Building_Ammunition_Depot")),
                BaseHalfWidthCells = config != null ? math.max(1, config.BaseHalfWidthCells) : 1,
                BaseHalfHeightCells = config != null ? math.max(1, config.BaseHalfHeightCells) : 1,
                BaseMinimumUnitsPerFaction = config != null ? math.max(0, config.BaseMinimumUnitsPerFaction) : 0
            };

            if (em.HasComponent<InitialUnitsSpawnConfig>(entity))
                em.SetComponentData(entity, initialConfig);
            else
                em.AddComponentData(entity, initialConfig);

            InitialUnitsBlockerChurnConfig churnConfig = new()
            {
                Enabled = config != null && config.EnableBlockerChurn,
                IntervalSeconds = config != null ? math.max(0.01f, config.ChurnIntervalSeconds) : 0.01f,
                AddRemovePerInterval = config != null ? math.max(0, config.AddRemovePerInterval) : 0
            };

            if (em.HasComponent<InitialUnitsBlockerChurnConfig>(entity))
                em.SetComponentData(entity, churnConfig);
            else
                em.AddComponentData(entity, churnConfig);

            InitialUnitsBlockerChurnComponent churnState = new()
            {
                Timer = 0f,
                RandomState = initialConfig.RandomSeed,
                BlockerPrefab = Entity.Null
            };

            if (em.HasComponent<InitialUnitsBlockerChurnComponent>(entity))
                em.SetComponentData(entity, churnState);
            else
                em.AddComponentData(entity, churnState);
        }

        private static void FillFactionBuffers(
            CustomGameFactionConfig config,
            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns,
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits,
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> initialBuildings,
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceUnitSpawns)
        {
            if (config == null || config.Factions == null)
                return;

            for (int factionIndex = 0; factionIndex < config.Factions.Count; factionIndex++)
            {
                CustomGameFactionConfig.FactionEntry faction = config.Factions[factionIndex];
                if (faction == null)
                    continue;

                byte factionId = (byte)math.clamp(faction.FactionId, 0, 255);
                factionSpawns.Add(new InitialUnitsFactionSpawnEntry
                {
                    FactionId = factionId,
                    SpawnCell = new int2(faction.SpawnCell.x, faction.SpawnCell.y)
                });

                if (faction.Units != null)
                {
                    for (int unitIndex = 0; unitIndex < faction.Units.Count; unitIndex++)
                    {
                        CustomGameFactionConfig.UnitSpawnEntry unit = faction.Units[unitIndex];
                        if (unit == null || unit.Count <= 0 || string.IsNullOrWhiteSpace(unit.SourceKey))
                            continue;

                        sourceUnitSpawns.Add(new CustomGameFactionUnitSourceSpawnEntry
                        {
                            FactionId = factionId,
                            SourceKey = ToFixed64(unit.SourceKey),
                            Count = math.max(0, unit.Count),
                            SpawnOffset = new int2(unit.SpawnOffset.x, unit.SpawnOffset.y)
                        });
                        initialUnits.Add(new InitialUnitsFactionUnitSpawnEntry
                        {
                            FactionId = factionId,
                            Prefab = Entity.Null,
                            Count = math.max(0, unit.Count),
                            SpawnOffset = new int2(unit.SpawnOffset.x, unit.SpawnOffset.y)
                        });
                    }
                }

                if (faction.Buildings == null)
                    continue;

                for (int buildingIndex = 0; buildingIndex < faction.Buildings.Count; buildingIndex++)
                {
                    CustomGameFactionConfig.BuildingSpawnEntry building = faction.Buildings[buildingIndex];
                    if (building == null || string.IsNullOrWhiteSpace(building.LookupKey))
                        continue;

                    initialBuildings.Add(new InitialUnitsFactionBuildingSpawnEntry
                    {
                        FactionId = factionId,
                        Prefab = Entity.Null,
                        PrefabLookupKey = ToFixed128(building.LookupKey),
                        OriginOffset = new int2(building.OriginOffset.x, building.OriginOffset.y)
                    });
                }
            }
        }

        private static void FillUnitSourceRegistry(
            CustomGameUnitRosterConfig config,
            DynamicBuffer<CustomGameUnitSourceRegistryEntry> unitSources)
        {
            if (config == null || config.Units == null)
                return;

            for (int i = 0; i < config.Units.Count; i++)
            {
                CustomGameUnitRosterConfig.UnitEntry unit = config.Units[i];
                if (unit == null || string.IsNullOrWhiteSpace(unit.SourceKey))
                    continue;

                unitSources.Add(new CustomGameUnitSourceRegistryEntry
                {
                    SourceKey = ToFixed64(unit.SourceKey),
                    DisplayName = ToFixed64(unit.DisplayName),
                    LegacyUnitPrefab = Entity.Null,
                    VisualPrefab = Entity.Null
                });
            }
        }

        private static void FillVisualRegistry(
            CustomGameVisualRegistryConfig config,
            DynamicBuffer<CustomGameVisualRegistryEntry> visualEntries)
        {
            if (config == null || config.Visuals == null)
                return;

            for (int i = 0; i < config.Visuals.Count; i++)
            {
                CustomGameVisualRegistryConfig.VisualEntry visual = config.Visuals[i];
                if (visual == null || string.IsNullOrWhiteSpace(visual.SourceKey))
                    continue;

                visualEntries.Add(new CustomGameVisualRegistryEntry
                {
                    SourceKey = ToFixed64(visual.SourceKey),
                    VisualPrefab = Entity.Null,
                    DirectionCount = math.max(1, visual.DirectionCount),
                    Columns = math.max(1, visual.Columns),
                    Rows = math.max(1, visual.Rows),
                    WorldSize = new float2(visual.WorldSize.x, visual.WorldSize.y)
                });
            }
        }

        private static void FillLegacyFactionBuffers(
            EntityManager em,
            InitialUnitsSpawnerAuthoringConfig config,
            Dictionary<string, Entity> convertedPrefabLookup,
            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns,
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits,
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> initialBuildings,
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceUnitSpawns)
        {
            if (config == null || config.Factions == null)
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
                        AddLegacyUnitSpawnEntry(em, convertedPrefabLookup, initialUnits, sourceUnitSpawns, factionId, unit.Prefab, unit.Count, unit.SpawnOffset);
                    }
                }

                if (config.CreateFactionBases && firstUnitPrefab != null && configuredUnitCount < config.BaseMinimumUnitsPerFaction)
                {
                    AddLegacyUnitSpawnEntry(
                        em,
                        convertedPrefabLookup,
                        initialUnits,
                        sourceUnitSpawns,
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

                    initialBuildings.Add(new InitialUnitsFactionBuildingSpawnEntry
                    {
                        FactionId = factionId,
                        Prefab = Entity.Null,
                        PrefabLookupKey = ToFixed128(GetBuildingLookupKey(building.Prefab)),
                        OriginOffset = new int2(building.OriginOffset.x, building.OriginOffset.y)
                    });
                }
            }
        }

        private static void FillLegacyUnitRegistry(
            EntityManager em,
            UnitPrefabRegistryAuthoringConfig config,
            Dictionary<string, Entity> convertedPrefabLookup,
            DynamicBuffer<UnitPrefabRegistryEntry> legacyRegistry,
            DynamicBuffer<CustomGameUnitSourceRegistryEntry> unitSources,
            DynamicBuffer<CustomGameVisualRegistryEntry> visualEntries,
            out int missingVisualReferences)
        {
            missingVisualReferences = 0;
            if (config == null || config.UnitSpawnPrefabs == null)
                return;

            for (int i = 0; i < config.UnitSpawnPrefabs.Count; i++)
            {
                GameObject prefab = config.UnitSpawnPrefabs[i];
                string sourceKey = GetPrefabName(prefab);
                if (string.IsNullOrWhiteSpace(sourceKey))
                {
                    missingVisualReferences++;
                    continue;
                }

                TryResolveConvertedPrefabEntity(em, convertedPrefabLookup, prefab, out Entity prefabEntity);
                legacyRegistry.Add(new UnitPrefabRegistryEntry { Prefab = prefabEntity });
                unitSources.Add(new CustomGameUnitSourceRegistryEntry
                {
                    SourceKey = ToFixed64(sourceKey),
                    DisplayName = ToFixed64(sourceKey),
                    LegacyUnitPrefab = prefabEntity,
                    VisualPrefab = Entity.Null
                });

                UnitImpostorAtlasEntry atlasEntry = FindAtlasEntry(config, prefab);
                if (atlasEntry == null || atlasEntry.Atlas == null)
                    missingVisualReferences++;

                visualEntries.Add(new CustomGameVisualRegistryEntry
                {
                    SourceKey = ToFixed64(sourceKey),
                    VisualPrefab = Entity.Null,
                    DirectionCount = atlasEntry != null ? math.max(1, atlasEntry.DirectionCount) : 8,
                    Columns = atlasEntry != null ? math.max(1, atlasEntry.Columns) : 4,
                    Rows = atlasEntry != null ? math.max(1, atlasEntry.Rows) : 2,
                    WorldSize = atlasEntry != null ? new float2(atlasEntry.Size.x, atlasEntry.Size.y) : new float2(1f, 1.8f)
                });
            }
        }

        private static void AddLegacyUnitSpawnEntry(
            EntityManager em,
            Dictionary<string, Entity> convertedPrefabLookup,
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits,
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceUnitSpawns,
            byte factionId,
            GameObject prefab,
            int count,
            Vector2Int spawnOffset)
        {
            string sourceKey = GetPrefabName(prefab);
            if (string.IsNullOrWhiteSpace(sourceKey))
                return;

            initialUnits.Add(new InitialUnitsFactionUnitSpawnEntry
            {
                FactionId = factionId,
                Prefab = TryResolveConvertedPrefabEntity(em, convertedPrefabLookup, prefab, out Entity prefabEntity) ? prefabEntity : Entity.Null,
                Count = math.max(0, count),
                SpawnOffset = new int2(spawnOffset.x, spawnOffset.y)
            });
            sourceUnitSpawns.Add(new CustomGameFactionUnitSourceSpawnEntry
            {
                FactionId = factionId,
                SourceKey = ToFixed64(sourceKey),
                Count = math.max(0, count),
                SpawnOffset = new int2(spawnOffset.x, spawnOffset.y)
            });
        }

        private static Dictionary<string, Entity> BuildConvertedPrefabLookup(
            EntityManager em,
            InitialUnitsSpawnerAuthoringConfig initialUnitsConfig,
            UnitPrefabRegistryAuthoringConfig config,
            Entity startupEntity)
        {
            Dictionary<string, Entity> lookup = new();
            AddExistingInitialSpawnPrefabsToLookup(em, initialUnitsConfig, startupEntity, lookup);
            if (config == null || config.UnitSpawnPrefabs == null || config.UnitSpawnPrefabs.Count == 0)
                return lookup;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            List<Entity> registryEntities = new();
            CollectEntities(query, em, registryEntities);
            for (int registryIndex = 0; registryIndex < registryEntities.Count; registryIndex++)
            {
                Entity registryEntity = registryEntities[registryIndex];
                // The packed support EntityScene can place the baked registry buffer on
                // the same entity that owns InitialUnitsSpawnConfig. Capture that buffer
                // before InitializeFromLegacyConfigs removes its registry tag and rebuilds
                // the runtime buffers. A previously initialized runtime entity has already
                // lost the tag, so it cannot re-enter this query with stale data.
                if (!em.HasBuffer<UnitPrefabRegistryEntry>(registryEntity))
                    continue;

                DynamicBuffer<UnitPrefabRegistryEntry> entries = em.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
                int count = math.min(config.UnitSpawnPrefabs.Count, entries.Length);
                for (int i = 0; i < count; i++)
                {
                    GameObject prefab = config.UnitSpawnPrefabs[i];
                    string sourceKey = GetPrefabName(prefab);
                    Entity prefabEntity = entries[i].Prefab;
                    if (string.IsNullOrWhiteSpace(sourceKey) || prefabEntity == Entity.Null || !em.Exists(prefabEntity))
                        continue;

                    lookup[sourceKey] = prefabEntity;
                }
            }

            return lookup;
        }

        private static void AddExistingInitialSpawnPrefabsToLookup(
            EntityManager em,
            InitialUnitsSpawnerAuthoringConfig config,
            Entity startupEntity,
            Dictionary<string, Entity> lookup)
        {
            if (config == null ||
                config.Factions == null ||
                startupEntity == Entity.Null ||
                !em.Exists(startupEntity) ||
                !em.HasBuffer<InitialUnitsFactionUnitSpawnEntry>(startupEntity))
            {
                return;
            }

            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> existingUnits =
                em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(startupEntity);
            int existingUnitIndex = 0;
            for (int factionIndex = 0; factionIndex < config.Factions.Count; factionIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[factionIndex];
                if (faction == null)
                    continue;

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
                        AddExistingInitialSpawnPrefabToLookup(em, existingUnits, existingUnitIndex, unit.Prefab, lookup);
                        existingUnitIndex++;
                    }
                }

                if (config.CreateFactionBases && firstUnitPrefab != null && configuredUnitCount < config.BaseMinimumUnitsPerFaction)
                {
                    AddExistingInitialSpawnPrefabToLookup(em, existingUnits, existingUnitIndex, firstUnitPrefab, lookup);
                    existingUnitIndex++;
                }
            }
        }

        private static void AddExistingInitialSpawnPrefabToLookup(
            EntityManager em,
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> existingUnits,
            int existingUnitIndex,
            GameObject sourcePrefab,
            Dictionary<string, Entity> lookup)
        {
            if (sourcePrefab == null ||
                existingUnitIndex < 0 ||
                existingUnitIndex >= existingUnits.Length)
            {
                return;
            }

            Entity prefabEntity = existingUnits[existingUnitIndex].Prefab;
            string sourceKey = GetPrefabName(sourcePrefab);
            if (string.IsNullOrWhiteSpace(sourceKey) ||
                prefabEntity == Entity.Null ||
                !em.Exists(prefabEntity))
            {
                return;
            }

            lookup[sourceKey] = prefabEntity;
        }

        private static void LogMissingConvertedPrefabResolutionIfNeeded(
            EntityManager em,
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> initialUnits,
            DynamicBuffer<CustomGameFactionUnitSourceSpawnEntry> sourceUnitSpawns,
            DynamicBuffer<UnitPrefabRegistryEntry> legacyRegistry)
        {
            if (initialUnits.Length == 0)
                return;

            int resolvedInitialPrefabs = 0;
            for (int i = 0; i < initialUnits.Length; i++)
            {
                Entity prefab = initialUnits[i].Prefab;
                if (prefab != Entity.Null && em.Exists(prefab))
                    resolvedInitialPrefabs++;
            }

            if (resolvedInitialPrefabs > 0)
                return;

            int resolvedRegistryPrefabs = 0;
            for (int i = 0; i < legacyRegistry.Length; i++)
            {
                Entity prefab = legacyRegistry[i].Prefab;
                if (prefab != Entity.Null && em.Exists(prefab))
                    resolvedRegistryPrefabs++;
            }

            using EntityQuery prefabQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            Debug.LogWarning(
                $"[CustomGameStartup] no converted ECS unit prefabs resolved for legacy skirmish. " +
                $"initialUnitEntries={initialUnits.Length} sourceUnitEntries={sourceUnitSpawns.Length} " +
                $"resolvedInitialPrefabs={resolvedInitialPrefabs} resolvedRegistryPrefabs={resolvedRegistryPrefabs} " +
                $"prefabCandidates={prefabQuery.CalculateEntityCount()} unitRegistrySingletons={registryQuery.CalculateEntityCount()}. " +
                "Android player builds must switch to Android before BuildPipeline.BuildPlayer so MatchSubScene EntityScene artifacts are baked for Android.");
        }

        private static bool TryResolveConvertedPrefabEntity(
            EntityManager em,
            Dictionary<string, Entity> convertedPrefabLookup,
            GameObject prefab,
            out Entity prefabEntity)
        {
            prefabEntity = Entity.Null;
            string sourceKey = GetPrefabName(prefab);
            if (!string.IsNullOrWhiteSpace(sourceKey) &&
                convertedPrefabLookup != null &&
                convertedPrefabLookup.TryGetValue(sourceKey, out prefabEntity) &&
                prefabEntity != Entity.Null &&
                em.Exists(prefabEntity))
            {
                return true;
            }

            return TryResolveConvertedPrefabEntity(em, prefab, out prefabEntity);
        }

        private static bool TryResolveConvertedPrefabEntity(EntityManager em, GameObject prefab, out Entity prefabEntity)
        {
            prefabEntity = Entity.Null;
            if (prefab == null)
                return false;

            string targetName = prefab.name;
            if (string.IsNullOrWhiteSpace(targetName))
                return false;

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>());
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (NamesMatch(em.GetName(candidate).ToString(), targetName))
                    {
                        prefabEntity = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetFirstEntity(EntityQuery query, EntityManager em, out Entity entity)
        {
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                if (entities.Length > 0)
                {
                    entity = entities[0];
                    return true;
                }
            }

            entity = Entity.Null;
            return false;
        }

        private static void CollectEntities(EntityQuery query, EntityManager em, List<Entity> results)
        {
            results.Clear();
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                    results.Add(entities[i]);
            }
        }

        private static bool NamesMatch(string candidateName, string targetName)
        {
            if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(targetName))
                return false;

            return string.Equals(candidateName, targetName, System.StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(candidateName.Replace(" (Clone)", string.Empty), targetName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static UnitImpostorAtlasEntry FindAtlasEntry(UnitPrefabRegistryAuthoringConfig config, GameObject prefab)
        {
            if (config.ImpostorAtlases == null || prefab == null)
                return null;

            for (int i = 0; i < config.ImpostorAtlases.Count; i++)
            {
                UnitImpostorAtlasEntry entry = config.ImpostorAtlases[i];
                if (entry != null && entry.Prefab == prefab)
                    return entry;
            }

            return null;
        }

        private static FixedString64Bytes ToFixed64(string value)
        {
            return new FixedString64Bytes(value ?? string.Empty);
        }

        private static FixedString128Bytes ToFixed128(string value)
        {
            return new FixedString128Bytes(value ?? string.Empty);
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
    }
}
