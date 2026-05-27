using UnityEngine;

internal sealed class RuntimeCityLandmarkOffsetSystem
{
    private static readonly Vector2Int[] HallOffsetsValue =
    {
        Vector2Int.zero,
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(2, 0),
        new Vector2Int(-2, 0),
        new Vector2Int(0, 2),
        new Vector2Int(0, -2),
        new Vector2Int(2, 1),
        new Vector2Int(-2, 1),
        new Vector2Int(2, -1),
        new Vector2Int(-2, -1),
        new Vector2Int(1, 2),
        new Vector2Int(-1, 2),
        new Vector2Int(1, -2),
        new Vector2Int(-1, -2)
    };

    private static readonly Vector2Int[] ClockTowerOffsetsValue =
    {
        new(3, 0),
        new(-3, 0),
        new(0, 3),
        new(0, -3),
        new(3, 2),
        new(-3, 2),
        new(3, -2),
        new(-3, -2),
        new(4, 1),
        new(-4, 1),
        new(1, 4),
        new(-1, 4)
    };

    private static readonly Vector2Int[] FountainOffsetsValue =
    {
        new(2, 3),
        new(-2, 3),
        new(3, 2),
        new(-3, 2),
        new(2, -3),
        new(-2, -3),
        new(3, -2),
        new(-3, -2),
        new(4, 0),
        new(-4, 0),
        new(0, 4),
        new(0, -4)
    };

    private static readonly Vector2Int[] MonumentOffsetsValue =
    {
        new(3, 4),
        new(-3, 4),
        new(4, 3),
        new(-4, 3),
        new(3, -4),
        new(-3, -4),
        new(4, -3),
        new(-4, -3),
        new(5, 0),
        new(-5, 0),
        new(0, 5),
        new(0, -5)
    };

    private static readonly Vector2Int[] PillarOffsetsValue =
    {
        new(5, 2),
        new(-5, 2),
        new(2, 5),
        new(-2, 5),
        new(5, -2),
        new(-5, -2),
        new(2, -5),
        new(-2, -5),
        new(6, 0),
        new(-6, 0),
        new(0, 6),
        new(0, -6)
    };

    public Vector2Int[] HallOffsets => HallOffsetsValue;
    public Vector2Int[] ClockTowerOffsets => ClockTowerOffsetsValue;
    public Vector2Int[] FountainOffsets => FountainOffsetsValue;
    public Vector2Int[] MonumentOffsets => MonumentOffsetsValue;
    public Vector2Int[] PillarOffsets => PillarOffsetsValue;

    public bool IsTooCloseToHall(RuntimeCityConfigSystem.Snapshot config, Vector2Int offset)
    {
        int distance = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
        return distance < Mathf.Max(1, config.LandmarkMinDistanceFromHallRoadCells);
    }
}
