using UnityEngine;
using YardSide = RuntimeCityYardGateSystem.YardSide;

internal sealed class RuntimeCityYardWallVisualSystem
{
    public void BuildYardBoundaryVisuals(
        RuntimeCityBuildingSpawnContextSystem.Context context,
        RuntimeCityBuildingPlacementSystem placementSystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        RuntimeCityYardGateSystem gateSystem,
        RectInt yardRect,
        YardSide gateSide,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GameObject pillarPrefab,
        GridConfig grid)
    {
        if (visualSystem == null)
            return;

        int horizontalThickness = prefabSelectionSystem.GetMinorFootprint(wallPrefab);
        int verticalThickness = prefabSelectionSystem.GetMinorFootprint(wallPrefab);
        int horizontalGateLength = Mathf.Max(1, prefabSelectionSystem.GetMajorFootprint(gatePrefab));
        int verticalGateLength = Mathf.Max(1, prefabSelectionSystem.GetMajorFootprint(gatePrefab));

        int northGateStart = gateSide == YardSide.North ? gateSystem.GetCenteredOpeningStart(yardRect.width, horizontalGateLength) : -1;
        int southGateStart = gateSide == YardSide.South ? gateSystem.GetCenteredOpeningStart(yardRect.width, horizontalGateLength) : -1;
        int eastGateStart = gateSide == YardSide.East ? gateSystem.GetCenteredOpeningStart(yardRect.height, verticalGateLength) : -1;
        int westGateStart = gateSide == YardSide.West ? gateSystem.GetCenteredOpeningStart(yardRect.height, verticalGateLength) : -1;

        PlaceHorizontalWallSide(prefabSelectionSystem, visualSystem, yardRect, yardRect.yMin, yardRect.width, wallPrefab, gatePrefab, grid, southGateStart, horizontalGateLength, false, horizontalThickness);
        PlaceHorizontalWallSide(prefabSelectionSystem, visualSystem, yardRect, yardRect.yMax - horizontalThickness, yardRect.width, wallPrefab, gatePrefab, grid, northGateStart, horizontalGateLength, false, horizontalThickness);
        PlaceVerticalWallSide(prefabSelectionSystem, visualSystem, yardRect, yardRect.xMin, yardRect.height, wallPrefab, gatePrefab, grid, westGateStart, verticalGateLength, verticalThickness);
        PlaceVerticalWallSide(prefabSelectionSystem, visualSystem, yardRect, yardRect.xMax - verticalThickness, yardRect.height, wallPrefab, gatePrefab, grid, eastGateStart, verticalGateLength, verticalThickness);

        if (pillarPrefab != null)
        {
            Vector2Int pillarFootprint = placementSystem.GetFootprint(context, pillarPrefab);
            visualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMin, yardRect.yMin), pillarFootprint, Quaternion.identity, grid);
            visualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMax - pillarFootprint.x, yardRect.yMin), pillarFootprint, Quaternion.identity, grid);
            visualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMin, yardRect.yMax - pillarFootprint.y), pillarFootprint, Quaternion.identity, grid);
            visualSystem.SpawnVisualOnlyPrefab(pillarPrefab, new Vector2Int(yardRect.xMax - pillarFootprint.x, yardRect.yMax - pillarFootprint.y), pillarFootprint, Quaternion.identity, grid);
        }
    }

    private void PlaceHorizontalWallSide(
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        RectInt yardRect,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GridConfig grid,
        int gateStartOffset,
        int gateLength,
        bool rotateGate,
        int thickness)
    {
        PlaceHorizontalWallRun(prefabSelectionSystem, visualSystem, yardRect.xMin, yOrigin, totalLength, wallPrefab, grid, thickness, gateStartOffset, gateLength);
        if (gateStartOffset >= 0)
        {
            Vector2Int gateFootprint = new(Mathf.Max(1, gateLength), Mathf.Max(1, prefabSelectionSystem.GetMinorFootprint(gatePrefab)));
            visualSystem.SpawnVisualOnlyPrefab(gatePrefab, new Vector2Int(yardRect.xMin + gateStartOffset, yOrigin), gateFootprint, rotateGate ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity, grid);
        }
    }

    private void PlaceHorizontalWallRun(
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        int xOrigin,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GridConfig grid,
        int thickness,
        int gateStartOffset,
        int gateLength)
    {
        int segmentLength = Mathf.Max(1, prefabSelectionSystem.GetMajorFootprint(wallPrefab));
        int current = 0;
        while (current < totalLength)
        {
            if (gateStartOffset >= 0 && current >= gateStartOffset && current < gateStartOffset + gateLength)
            {
                current = gateStartOffset + gateLength;
                continue;
            }

            int nextStop = totalLength;
            if (gateStartOffset >= 0 && current < gateStartOffset)
                nextStop = gateStartOffset;
            int pieceLength = Mathf.Min(segmentLength, nextStop - current);
            if (pieceLength <= 0)
                break;

            visualSystem.SpawnVisualOnlyPrefab(
                wallPrefab,
                new Vector2Int(xOrigin + current, yOrigin),
                new Vector2Int(pieceLength, Mathf.Max(1, thickness)),
                Quaternion.identity,
                grid);
            current += pieceLength;
        }
    }

    private void PlaceVerticalWallSide(
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        RectInt yardRect,
        int xOrigin,
        int totalLength,
        GameObject wallPrefab,
        GameObject gatePrefab,
        GridConfig grid,
        int gateStartOffset,
        int gateLength,
        int thickness)
    {
        PlaceVerticalWallRun(prefabSelectionSystem, visualSystem, xOrigin, yardRect.yMin, totalLength, wallPrefab, grid, thickness, gateStartOffset, gateLength);
        if (gateStartOffset >= 0)
        {
            Vector2Int gateFootprint = new(Mathf.Max(1, prefabSelectionSystem.GetMinorFootprint(gatePrefab)), Mathf.Max(1, gateLength));
            visualSystem.SpawnVisualOnlyPrefab(gatePrefab, new Vector2Int(xOrigin, yardRect.yMin + gateStartOffset), gateFootprint, Quaternion.Euler(0f, 90f, 0f), grid);
        }
    }

    private void PlaceVerticalWallRun(
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        int xOrigin,
        int yOrigin,
        int totalLength,
        GameObject wallPrefab,
        GridConfig grid,
        int thickness,
        int gateStartOffset,
        int gateLength)
    {
        int segmentLength = Mathf.Max(1, prefabSelectionSystem.GetMajorFootprint(wallPrefab));
        int current = 0;
        while (current < totalLength)
        {
            if (gateStartOffset >= 0 && current >= gateStartOffset && current < gateStartOffset + gateLength)
            {
                current = gateStartOffset + gateLength;
                continue;
            }

            int nextStop = totalLength;
            if (gateStartOffset >= 0 && current < gateStartOffset)
                nextStop = gateStartOffset;
            int pieceLength = Mathf.Min(segmentLength, nextStop - current);
            if (pieceLength <= 0)
                break;

            visualSystem.SpawnVisualOnlyPrefab(
                wallPrefab,
                new Vector2Int(xOrigin, yOrigin + current),
                new Vector2Int(Mathf.Max(1, thickness), pieceLength),
                Quaternion.Euler(0f, 90f, 0f),
                grid);
            current += pieceLength;
        }
    }
}
