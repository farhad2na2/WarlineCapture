using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public struct FactionVisualConfig : IComponentData
{
    public float4 PlayerColor;
    public float4 EnemyColor;
    public float4 NeutralColor;
}

[MaterialProperty("_BaseColor")]
public struct FactionTintColor : IComponentData
{
    public float4 Value;
}

public struct FactionTintTarget : IComponentData { }
