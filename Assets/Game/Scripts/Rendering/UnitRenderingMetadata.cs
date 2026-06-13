using System.Collections.Generic;
using UnityEngine;

public readonly struct UnitRenderingMetadata
{
    public readonly bool IsAirUnit;
    public readonly Vector2Int FootprintCells;
    public readonly IReadOnlyList<UnitAnimationKind> AnimationOrder;

    public UnitRenderingMetadata(
        bool isAirUnit,
        Vector2Int footprintCells,
        IReadOnlyList<UnitAnimationKind> animationOrder)
    {
        IsAirUnit = isAirUnit;
        FootprintCells = footprintCells;
        AnimationOrder = animationOrder;
    }
}

public delegate bool TryGetUnitRenderingMetadataDelegate(GameObject prefab, out UnitRenderingMetadata metadata);
