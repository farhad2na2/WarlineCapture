using System;

namespace Game.Simulation
{
    // Data-only projection of the original legacy initial-spawn ScriptableObjects.
    // Unity GameObject/Texture references are represented by exact source keys; the
    // runtime owner resolves unit keys to the original baked prefab entities.
    public sealed class LegacyInitialSpawnData
    {
        public int BlockerCount;
        public int SpawnRadiusCells;
        public float RespawnDelaySeconds;
        public uint RandomSeed;
        public int InitialDollars;
        public int InitialMaterials;
        public int MaterialsCapacity;
        public int InitialAiMaterials;
        public int AiMaterialsCapacity;
        public int InitialOil;
        public int InitialFuel;
        public bool CreateFactionBases;
        public string BaseWallPrefabKey;
        public string BaseGatePrefabKey;
        public string BaseCoreBuildingPrefabKey;
        public int BaseHalfWidthCells;
        public int BaseHalfHeightCells;
        public int BaseMinimumUnitsPerFaction;
        public bool EnableBlockerChurn;
        public float ChurnIntervalSeconds;
        public int AddRemovePerInterval;
        public int MissingVisualReferenceCount;
        public string[] UnitPrefabRegistryKeys = Array.Empty<string>();
        public LegacyInitialFactionData[] Factions = Array.Empty<LegacyInitialFactionData>();
        public LegacyInitialVisualData[] VisualRegistry = Array.Empty<LegacyInitialVisualData>();
    }

    public sealed class LegacyInitialFactionData
    {
        public int FactionId;
        public int SpawnX;
        public int SpawnY;
        public LegacyInitialUnitData[] Units = Array.Empty<LegacyInitialUnitData>();
        public LegacyInitialBuildingData[] Buildings = Array.Empty<LegacyInitialBuildingData>();
    }

    public sealed class LegacyInitialUnitData
    {
        public string SourceKey;
        public int Count;
        public int OffsetX;
        public int OffsetY;
    }

    public sealed class LegacyInitialBuildingData
    {
        public string LookupKey;
        public int OriginOffsetX;
        public int OriginOffsetY;
    }

    public sealed class LegacyInitialVisualData
    {
        public string SourceKey;
        public bool HasAtlas;
        public int DirectionCount;
        public int Columns;
        public int Rows;
        public float WorldSizeX;
        public float WorldSizeY;
    }
}
