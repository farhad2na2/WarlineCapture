using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

[DisallowMultipleComponent]
public class InitialUnitsSpawnerAuthoring : MonoBehaviour
{
    [SerializeField] private InitialUnitsSpawnerAuthoringConfig config;
    private GameObject BlockerPrefab => config != null ? config.BlockerPrefab : null;
    private int BlockerCount => config != null ? config.BlockerCount : 2000;
    private int SpawnRadiusCells => config != null ? config.SpawnRadiusCells : 5;
    private float RespawnDelaySeconds => config != null ? config.RespawnDelaySeconds : 10f;
    private uint RandomSeed => config != null ? config.RandomSeed : 1u;
    private bool CreateFactionBases => config == null || config.CreateFactionBases;
    private bool EnableBlockerChurn => config == null || config.EnableBlockerChurn;
    private float ChurnIntervalSeconds => config != null ? config.ChurnIntervalSeconds : 1f;
    private int AddRemovePerInterval => config != null ? config.AddRemovePerInterval : 50;

    private class InitialUnitsSpawnerBaker : Baker<InitialUnitsSpawnerAuthoring>
    {
        public override void Bake(InitialUnitsSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new InitialUnitsSpawnConfig
            {
                BlockerPrefab = authoring.BlockerPrefab != null
                    ? GetEntity(authoring.BlockerPrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null,
                BlockerCount = authoring.BlockerCount,
                SpawnRadiusCells = math.max(0, authoring.SpawnRadiusCells),
                RespawnDelaySeconds = math.max(0.01f, authoring.RespawnDelaySeconds),
                RandomSeed = math.max(1u, authoring.RandomSeed),
                InitialDollars = authoring.config != null ? authoring.config.InitialDollars : 0,
                InitialOil = authoring.config != null ? authoring.config.InitialOil : 0,
                InitialFuel = authoring.config != null ? authoring.config.InitialFuel : 0,
                CreateFactionBases = authoring.CreateFactionBases ? (byte)1 : (byte)0,
                BaseWallPrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(authoring.config != null ? authoring.config.BaseWallPrefab : null, "Wall_Dirt_Straight")),
                BaseGatePrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(authoring.config != null ? authoring.config.BaseGatePrefab : null, "Building_Road_Barrier")),
                BaseCoreBuildingPrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(authoring.config != null ? authoring.config.BaseCoreBuildingPrefab : null, "Building_Ammunition_Depot")),
                BaseHalfWidthCells = authoring.config != null ? authoring.config.BaseHalfWidthCells : 120,
                BaseHalfHeightCells = authoring.config != null ? authoring.config.BaseHalfHeightCells : 80,
                BaseMinimumUnitsPerFaction = authoring.config != null ? authoring.config.BaseMinimumUnitsPerFaction : 18
            });

            AddComponent(entity, new InitialUnitsBlockerChurnConfig
            {
                Enabled = authoring.EnableBlockerChurn,
                IntervalSeconds = authoring.ChurnIntervalSeconds,
                AddRemovePerInterval = authoring.AddRemovePerInterval
            });

            AddComponent(entity, new InitialUnitsBlockerChurnState
            {
                Timer = 0f,
                RandomState = math.max(1u, authoring.RandomSeed),
                BlockerPrefab = authoring.BlockerPrefab != null
                    ? GetEntity(authoring.BlockerPrefab, TransformUsageFlags.Dynamic)
                    : Entity.Null
            });

            DynamicBuffer<InitialUnitsFactionSpawnEntry> factionSpawns = AddBuffer<InitialUnitsFactionSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionUnitSpawnEntry> unitSpawns = AddBuffer<InitialUnitsFactionUnitSpawnEntry>(entity);
            DynamicBuffer<InitialUnitsFactionBuildingSpawnEntry> buildingSpawns = AddBuffer<InitialUnitsFactionBuildingSpawnEntry>(entity);
            if (authoring.config != null && authoring.config.Factions != null)
            {
                for (int i = 0; i < authoring.config.Factions.Count; i++)
                {
                    InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = authoring.config.Factions[i];
                    if (faction == null)
                        continue;

                    byte factionId = (byte)math.clamp(faction.FactionId, 0, 255);
                    GameObject firstUnitPrefab = null;
                    int configuredUnitCount = 0;
                    factionSpawns.Add(new InitialUnitsFactionSpawnEntry
                    {
                        FactionId = factionId,
                        SpawnCell = new int2(faction.SpawnCell.x, faction.SpawnCell.y)
                    });

                    if (faction.Units != null)
                    {
                        for (int unitIndex = 0; unitIndex < faction.Units.Count; unitIndex++)
                        {
                            InitialUnitsSpawnerAuthoringConfig.FactionUnitEntry unit = faction.Units[unitIndex];
                            if (unit == null || unit.Prefab == null || unit.Count <= 0)
                                continue;
                            firstUnitPrefab ??= unit.Prefab;
                            configuredUnitCount += math.max(0, unit.Count);

                            unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry
                            {
                                FactionId = factionId,
                                Prefab = GetEntity(unit.Prefab, TransformUsageFlags.Dynamic),
                                Count = math.max(0, unit.Count),
                                SpawnOffset = new int2(unit.SpawnOffset.x, unit.SpawnOffset.y)
                            });
                        }
                    }

                    int minimumUnits = authoring.config != null ? authoring.config.BaseMinimumUnitsPerFaction : 18;
                    if (authoring.CreateFactionBases && firstUnitPrefab != null && configuredUnitCount < minimumUnits)
                    {
                        unitSpawns.Add(new InitialUnitsFactionUnitSpawnEntry
                        {
                            FactionId = factionId,
                            Prefab = GetEntity(firstUnitPrefab, TransformUsageFlags.Dynamic),
                            Count = minimumUnits - configuredUnitCount,
                            SpawnOffset = default
                        });
                    }

                    if (faction.Buildings != null)
                    {
                        for (int buildingIndex = 0; buildingIndex < faction.Buildings.Count; buildingIndex++)
                        {
                            InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry building = faction.Buildings[buildingIndex];
                            if (building == null || building.Prefab == null)
                                continue;

                            buildingSpawns.Add(new InitialUnitsFactionBuildingSpawnEntry
                            {
                                FactionId = factionId,
                                Prefab = GetEntity(building.Prefab, TransformUsageFlags.Dynamic),
                                PrefabLookupKey = new FixedString128Bytes(GetBuildingLookupKey(building.Prefab)),
                                OriginOffset = new int2(building.OriginOffset.x, building.OriginOffset.y)
                            });
                        }
                    }
                }
            }
        }

        private static string GetBuildingLookupKey(GameObject prefab, string fallback = "")
        {
            if (prefab == null)
                return fallback;

            return prefab.name.Trim().ToLowerInvariant();
        }
    }
}
