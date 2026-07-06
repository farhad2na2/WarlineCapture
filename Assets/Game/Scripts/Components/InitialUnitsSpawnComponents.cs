using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Game.Components
{
    public struct InitialUnitsSpawnConfig : IComponentData
    {
        public Entity BlockerPrefab;
        public Entity UnitSelectionMarkerPrefab;
        public Entity UnitHealthBarPrefab;
        public int BlockerCount;
        public int SpawnRadiusCells;
        public float RespawnDelaySeconds;
        public uint RandomSeed;
        public int InitialDollars;
        public int InitialOil;
        public int InitialFuel;
        public byte CreateFactionBases;
        public FixedString128Bytes BaseWallPrefabLookupKey;
        public FixedString128Bytes BaseGatePrefabLookupKey;
        public FixedString128Bytes BaseCoreBuildingPrefabLookupKey;
        public int BaseHalfWidthCells;
        public int BaseHalfHeightCells;
        public int BaseMinimumUnitsPerFaction;
    }

    public struct InitialUnitsSpawnInitialized : IComponentData
    {
    }

    public struct InitialUnitsFactionSpawnEntry : IBufferElementData
    {
        public byte FactionId;
        public int2 SpawnCell;
    }

    public struct InitialUnitsFactionUnitSpawnEntry : IBufferElementData
    {
        public byte FactionId;
        public Entity Prefab;
        public int Count;
        public int2 SpawnOffset;
    }

    public struct InitialUnitsFactionBuildingSpawnEntry : IBufferElementData
    {
        public byte FactionId;
        public Entity Prefab;
        public FixedString128Bytes PrefabLookupKey;
        public int2 OriginOffset;
    }

    public struct InitialUnitsFactionUnitSpawnProgress : IBufferElementData
    {
        public int Spawned;
    }

    public struct InitialUnitsSpawnProgress : IComponentData
    {
        public uint RandomState;
        public int BlockersSpawned;
        public byte InitialResourcesApplied;
        public byte InitialFuelStorageApplied;
        public byte InitialBuildingRequestsIssued;
        public byte InitialBuildingsSpawned;
        public int InitialBuildingCompletionWaitFrames;
    }

    public struct InitialUnitsBlockerChurnConfig : IComponentData
    {
        public bool Enabled;
        public float IntervalSeconds;
        public int AddRemovePerInterval;
    }

    public struct InitialUnitsBlockerChurnComponent : IComponentData
    {
        public float Timer;
        public uint RandomState;
        public Entity BlockerPrefab;
    }
}
