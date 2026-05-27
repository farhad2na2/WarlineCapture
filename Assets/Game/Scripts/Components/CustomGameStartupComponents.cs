using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CustomGameStartupStateComponent : IComponentData
{
    public FixedString64Bytes GameModeId;
    public int GridWidth;
    public int GridHeight;
    public float CellSize;
    public float3 GridOrigin;
    public int FactionCount;
    public int UnitRosterCount;
    public int InitialUnitEntryCount;
    public int VisualEntryCount;
}

public struct CustomGameUnitSourceRegistryEntry : IBufferElementData
{
    public FixedString64Bytes SourceKey;
    public FixedString64Bytes DisplayName;
    public Entity LegacyUnitPrefab;
    public Entity VisualPrefab;
}

public struct CustomGameFactionUnitSourceSpawnEntry : IBufferElementData
{
    public byte FactionId;
    public FixedString64Bytes SourceKey;
    public int Count;
    public int2 SpawnOffset;
}

public struct CustomGameVisualRegistryEntry : IBufferElementData
{
    public FixedString64Bytes SourceKey;
    public Entity VisualPrefab;
    public int DirectionCount;
    public int Columns;
    public int Rows;
    public float2 WorldSize;
}
