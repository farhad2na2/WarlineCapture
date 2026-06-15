using Unity.Entities;
using UnityEngine;

internal sealed partial class RuntimeCityYardGateSystem : SystemBase
{
    private readonly RuntimeCityYardGateState _state = new();

    public enum YardSide
    {
        North,
        East,
        South,
        West
    }

    public RuntimeCityYardGateState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public int GetCenteredOpeningStart(int totalLength, int openingLength)
    {
        return _state.GetCenteredOpeningStart(totalLength, openingLength);
    }

    public YardSide GetPreferredYardGateSide(RectInt houseRect, Vector2Int centerRoadCell)
    {
        return _state.GetPreferredYardGateSide(houseRect, centerRoadCell);
    }
}

internal sealed class RuntimeCityYardGateState
{
    public int GetCenteredOpeningStart(int totalLength, int openingLength)
    {
        if (openingLength >= totalLength - 1)
            return Mathf.Max(0, (totalLength - Mathf.Max(1, totalLength / 2)) / 2);

        return Mathf.Clamp((totalLength - openingLength) / 2, 1, Mathf.Max(1, totalLength - openingLength - 1));
    }

    public RuntimeCityYardGateSystem.YardSide GetPreferredYardGateSide(RectInt houseRect, Vector2Int centerRoadCell)
    {
        Vector2 houseCenter = new(houseRect.center.x, houseRect.center.y);
        Vector2 cityCenter = new(centerRoadCell.x, centerRoadCell.y);
        Vector2 delta = cityCenter - houseCenter;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x >= 0f ? RuntimeCityYardGateSystem.YardSide.East : RuntimeCityYardGateSystem.YardSide.West;

        return delta.y >= 0f ? RuntimeCityYardGateSystem.YardSide.North : RuntimeCityYardGateSystem.YardSide.South;
    }
}
