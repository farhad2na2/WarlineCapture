using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using RoadTileData = RoadNetworkSystem.RoadTileData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;

public sealed class RoadFootprintQuerySystem
{
    public enum FootprintKind
    {
        Dirt,
        Sidewalk
    }

    public sealed class FootprintBoundsData
    {
        public Bounds Bounds;
        public FootprintKind Kind;
    }

    public sealed class CombinedRoadVisualData
    {
        public Mesh Mesh;
        public Material[] Materials;
        public List<FootprintBoundsData> FootprintBounds = new();
    }

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<Vector2Int, RoadTileData> RoadTiles;
        public readonly IReadOnlyDictionary<Vector2Int, GameObject> SpecialRoadObjects;
        public readonly IReadOnlyDictionary<RoadVisualType, CombinedRoadVisualData> VisualData;
        public readonly Vector3 GridOrigin;
        public readonly float BuildPlaneY;
        public readonly float RoadGridSize;

        public Context(
            IReadOnlyDictionary<Vector2Int, RoadTileData> roadTiles,
            IReadOnlyDictionary<Vector2Int, GameObject> specialRoadObjects,
            IReadOnlyDictionary<RoadVisualType, CombinedRoadVisualData> visualData,
            Vector3 gridOrigin,
            float buildPlaneY,
            float roadGridSize)
        {
            RoadTiles = roadTiles;
            SpecialRoadObjects = specialRoadObjects;
            VisualData = visualData;
            GridOrigin = gridOrigin;
            BuildPlaneY = buildPlaneY;
            RoadGridSize = roadGridSize;
        }
    }

    public bool HasRoadInFootprint(Context context, GridConfig grid, Vector2Int originCell, Vector2Int footprintCells)
    {
        if (context.RoadTiles == null || context.RoadTiles.Count == 0)
            return false;

        int buildingMinX = originCell.x;
        int buildingMinY = originCell.y;
        int buildingMaxX = originCell.x + footprintCells.x;
        int buildingMaxY = originCell.y + footprintCells.y;

        foreach (var entry in context.RoadTiles)
        {
            bool foundOverlap = false;
            ForEachRoadWorldFootprint(context, entry.Key, entry.Value, (worldMin, worldMax) =>
            {
                GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                bool overlaps = false;
                for (int y = minY; y < maxY && !overlaps; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        if (x < buildingMinX || y < buildingMinY || x >= buildingMaxX || y >= buildingMaxY)
                            continue;

                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                    return true;

                foundOverlap = true;
                return false;
            });

            if (foundOverlap)
                return true;
        }

        return false;
    }

    public void FillRoadFootprintMask(Context context, GridConfig grid, bool[] occupiedCells)
    {
        if (occupiedCells == null || occupiedCells.Length < grid.Width * grid.Height || context.RoadTiles == null)
            return;

        foreach (var entry in context.RoadTiles)
        {
            ForEachRoadWorldFootprint(context, entry.Key, entry.Value, (worldMin, worldMax) =>
            {
                GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                for (int y = minY; y < maxY; y++)
                {
                    for (int x = minX; x < maxX; x++)
                    {
                        if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                            continue;

                        occupiedCells[GridUtils.CellToIndex(new int2(x, y), grid.Width)] = true;
                    }
                }

                return true;
            });
        }
    }

    public void GetRoadWorldFootprint(Context context, Vector2Int roadCell, RoadTileData tile, out Vector3 worldMin, out Vector3 worldMax)
    {
        bool hasBounds = false;
        Bounds combinedBounds = default;

        ForEachRoadWorldFootprint(context, roadCell, tile, (footprintMin, footprintMax) =>
        {
            var footprintBounds = new Bounds((footprintMin + footprintMax) * 0.5f, footprintMax - footprintMin);
            if (!hasBounds)
            {
                combinedBounds = footprintBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(footprintMin);
                combinedBounds.Encapsulate(footprintMax);
            }

            return true;
        });

        if (hasBounds)
        {
            worldMin = combinedBounds.min;
            worldMax = combinedBounds.max;
            return;
        }

        worldMin = context.GridOrigin + new Vector3(roadCell.x * context.RoadGridSize, 0f, roadCell.y * context.RoadGridSize);
        worldMax = worldMin + new Vector3(context.RoadGridSize, 0f, context.RoadGridSize);
    }

    public void ForEachRoadWorldFootprint(Context context, Vector2Int roadCell, RoadTileData tile, Func<Vector3, Vector3, bool> visitor)
    {
        ForEachRoadWorldFootprintKind(context, roadCell, tile, (worldMin, worldMax, _) => visitor(worldMin, worldMax));
    }

    public void ForEachRoadWorldFootprintKind(
        Context context,
        Vector2Int roadCell,
        RoadTileData tile,
        Func<Vector3, Vector3, FootprintKind, bool> visitor)
    {
        if (context.SpecialRoadObjects != null &&
            context.SpecialRoadObjects.TryGetValue(roadCell, out var specialRoadObject) &&
            specialRoadObject != null)
        {
            MeshFilter[] meshFilters = specialRoadObject.GetComponentsInChildren<MeshFilter>(true);
            bool foundSpecialBounds = false;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter.sharedMesh == null)
                    continue;
                if (!TryGetFootprintKind(
                        meshFilter.transform,
                        tile.Type == RoadVisualType.Autobahn || tile.Type == RoadVisualType.AutobahnConnect,
                        out FootprintKind footprintKind))
                    continue;

                Bounds worldBounds = TransformBounds(meshFilter.sharedMesh.bounds, meshFilter.transform.localToWorldMatrix);
                foundSpecialBounds = true;
                if (!visitor(worldBounds.min, worldBounds.max, footprintKind))
                    return;
            }

            if (foundSpecialBounds)
                return;
        }

        if (context.VisualData != null &&
            context.VisualData.TryGetValue(tile.Type, out var visualData) &&
            visualData.FootprintBounds != null &&
            visualData.FootprintBounds.Count > 0)
        {
            Vector3 basePosition = GetPlacementPosition(context, roadCell, tile.Rotation, tile.Scale);
            for (int boundsIndex = 0; boundsIndex < visualData.FootprintBounds.Count; boundsIndex++)
            {
                FootprintBoundsData footprintData = visualData.FootprintBounds[boundsIndex];
                if (!VisitTransformedBounds(
                        footprintData.Bounds,
                        basePosition,
                        tile.Rotation,
                        tile.Scale,
                        (worldMin, worldMax) => visitor(worldMin, worldMax, footprintData.Kind)))
                {
                    return;
                }
            }

            return;
        }

        if (context.VisualData != null &&
            context.VisualData.TryGetValue(tile.Type, out var fallbackVisualData) &&
            fallbackVisualData.Mesh != null)
        {
            Vector3 basePosition = GetPlacementPosition(context, roadCell, tile.Rotation, tile.Scale);
            VisitTransformedBounds(
                fallbackVisualData.Mesh.bounds,
                basePosition,
                tile.Rotation,
                tile.Scale,
                (worldMin, worldMax) => visitor(worldMin, worldMax, FootprintKind.Dirt));
            return;
        }

        Vector3 fallbackMin = context.GridOrigin + new Vector3(roadCell.x * context.RoadGridSize, 0f, roadCell.y * context.RoadGridSize);
        Vector3 fallbackMax = fallbackMin + new Vector3(context.RoadGridSize, 0f, context.RoadGridSize);
        visitor(fallbackMin, fallbackMax, FootprintKind.Dirt);
    }

    public static bool ShouldReserveRoadRenderer(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return IsReserveMarkerName(name) ||
               name.IndexOf("sm_env_dirt", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("sm_env_sidewalk", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool TryGetFootprintKind(Transform transform, bool typeOverride, out FootprintKind kind)
    {
        Transform current = transform;
        while (current != null)
        {
            if (IsSidewalkMarkerName(current.name))
            {
                kind = FootprintKind.Sidewalk;
                return true;
            }

            if (IsDirtMarkerName(current.name))
            {
                kind = FootprintKind.Dirt;
                return true;
            }

            if (!typeOverride && ShouldReserveRoadRenderer(current.name))
            {
                kind = current.name.IndexOf("sidewalk", StringComparison.OrdinalIgnoreCase) >= 0
                    ? FootprintKind.Sidewalk
                    : FootprintKind.Dirt;
                return true;
            }

            current = current.parent;
        }

        kind = FootprintKind.Dirt;
        return false;
    }

    public static bool IsGridCellCenterInsideBounds(GridConfig grid, int x, int y, Vector3 worldMin, Vector3 worldMax)
    {
        Vector3 center = (Vector3)grid.Origin + new Vector3((x + 0.5f) * grid.CellSize, 0f, (y + 0.5f) * grid.CellSize);
        return center.x >= worldMin.x && center.x < worldMax.x &&
               center.z >= worldMin.z && center.z < worldMax.z;
    }

    public static Bounds TransformBounds(Bounds localBounds, Matrix4x4 matrix)
    {
        Vector3[] corners =
        {
            new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
            new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
            new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
            new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
            new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
            new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
            new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
            new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
        };

        Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 worldCorner = matrix.MultiplyPoint3x4(corners[i]);
            min = Vector3.Min(min, worldCorner);
            max = Vector3.Max(max, worldCorner);
        }

        return new Bounds((min + max) * 0.5f, max - min);
    }

    private static bool VisitTransformedBounds(
        Bounds localBounds,
        Vector3 basePosition,
        Quaternion rotation,
        Vector3 scale,
        Func<Vector3, Vector3, bool> visitor)
    {
        Vector3[] corners =
        {
            new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
            new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
            new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
            new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
            new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
            new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
            new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
            new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
        };

        Vector3 worldMin = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 worldMax = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 worldCorner = basePosition + rotation * Vector3.Scale(corners[i], scale);
            worldMin = Vector3.Min(worldMin, worldCorner);
            worldMax = Vector3.Max(worldMax, worldCorner);
        }

        return visitor(worldMin, worldMax);
    }

    private static void GetGridBounds(GridConfig grid, Vector3 worldMin, Vector3 worldMax, out int minX, out int minY, out int maxX, out int maxY)
    {
        float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
        float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

        minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
        minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
        maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
        maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);
    }

    private static Vector3 GetPlacementPosition(Context context, Vector2Int cell, Quaternion rotation, Vector3 scale)
    {
        Vector3 basePosition = context.GridOrigin + new Vector3(cell.x * context.RoadGridSize, context.BuildPlaneY, cell.y * context.RoadGridSize);
        Vector3[] corners =
        {
            new(0f, 0f, 0f),
            new(context.RoadGridSize, 0f, 0f),
            new(0f, 0f, context.RoadGridSize),
            new(context.RoadGridSize, 0f, context.RoadGridSize)
        };

        float minX = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 transformed = rotation * Vector3.Scale(corners[i], scale);
            if (transformed.x < minX)
                minX = transformed.x;
            if (transformed.z < minZ)
                minZ = transformed.z;
        }

        return basePosition - new Vector3(minX, 0f, minZ);
    }

    private static bool IsReserveMarkerName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return IsDirtMarkerName(name) || IsSidewalkMarkerName(name);
    }

    private static bool IsDirtMarkerName(string name)
    {
        return string.Equals(name, "Dirt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidewalkMarkerName(string name)
    {
        return string.Equals(name, "Sidewalk", StringComparison.OrdinalIgnoreCase);
    }
}
