using UnityEngine;

internal sealed class RuntimeCityYardGateSystem
{
    public enum YardSide
    {
        North,
        East,
        South,
        West
    }

    public int GetCenteredOpeningStart(int totalLength, int openingLength)
    {
        if (openingLength >= totalLength - 1)
            return Mathf.Max(0, (totalLength - Mathf.Max(1, totalLength / 2)) / 2);

        return Mathf.Clamp((totalLength - openingLength) / 2, 1, Mathf.Max(1, totalLength - openingLength - 1));
    }

    public YardSide GetPreferredYardGateSide(RectInt houseRect, Vector2Int centerRoadCell)
    {
        Vector2 houseCenter = new(houseRect.center.x, houseRect.center.y);
        Vector2 cityCenter = new(centerRoadCell.x, centerRoadCell.y);
        Vector2 delta = cityCenter - houseCenter;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            return delta.x >= 0f ? YardSide.East : YardSide.West;

        return delta.y >= 0f ? YardSide.North : YardSide.South;
    }
}
