using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class MapSurfaceCommandTargetSystem
{
    private readonly MapSurfaceQuerySystem _querySystem = new();

    public readonly struct Result
    {
        public readonly int2 Cell;
        public readonly Vector3 WorldPoint;
        public readonly MapSurfaceSample Surface;
        public readonly bool HasSurface;

        private Result(int2 cell, Vector3 worldPoint, MapSurfaceSample surface, bool hasSurface)
        {
            Cell = cell;
            WorldPoint = worldPoint;
            Surface = surface;
            HasSurface = hasSurface;
        }

        public static Result FlatFallback(int2 cell, Vector3 worldPoint)
        {
            return new Result(cell, worldPoint, default, false);
        }

        public static Result SurfaceHit(int2 cell, Vector3 worldPoint, MapSurfaceSample surface)
        {
            return new Result(cell, worldPoint, surface, true);
        }
    }

    public bool TryResolveCommandTarget(
        EntityManager entityManager,
        EntityQuery surfaceQuery,
        GridConfig grid,
        Camera worldCamera,
        Vector2 screenPosition,
        SelectionStateSystem selectionStateSystem,
        out Result result)
    {
        result = default;
        if (worldCamera == null)
            return false;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        if (!TryResolveFlatFallback(grid, ray, out int2 fallbackCell, out Vector3 fallbackWorldPoint))
            return false;

        if (!_querySystem.TryCreateContext(entityManager, surfaceQuery, out MapSurfaceQuerySystem.Context surfaceContext))
        {
            result = Result.FlatFallback(fallbackCell, fallbackWorldPoint);
            return true;
        }

        TryResolvePreferredSelectionLayer(entityManager, selectionStateSystem, out int preferredSurfaceId, out int preferredLayerId);
        if (TryResolveSurfaceHit(
                surfaceContext,
                grid,
                ray,
                fallbackCell,
                preferredSurfaceId,
                preferredLayerId,
                out result))
        {
            return true;
        }

        result = Result.FlatFallback(fallbackCell, fallbackWorldPoint);
        return true;
    }

    private static bool TryResolveFlatFallback(GridConfig grid, Ray ray, out int2 cell, out Vector3 worldPoint)
    {
        cell = default;
        worldPoint = default;

        Plane plane = new(Vector3.up, new Vector3(0f, grid.Origin.y, 0f));
        if (!plane.Raycast(ray, out float distance))
            return false;

        worldPoint = ray.GetPoint(distance);
        cell = GridUtils.WorldToCell(grid, worldPoint);
        return GridUtils.InBounds(cell, grid.Width, grid.Height);
    }

    private bool TryResolveSurfaceHit(
        MapSurfaceQuerySystem.Context context,
        GridConfig grid,
        Ray ray,
        int2 fallbackCell,
        int preferredSurfaceId,
        int preferredLayerId,
        out Result result)
    {
        result = default;
        float bestScore = float.MaxValue;
        bool found = false;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                int2 candidateCell = fallbackCell + new int2(x, y);
                if (!GridUtils.InBounds(candidateCell, grid.Width, grid.Height) ||
                    !_querySystem.TryGetSurfaceRange(context, candidateCell, out MapSurfaceCellSurfaceRange range))
                {
                    continue;
                }

                for (int i = 0; i < range.SurfaceCount; i++)
                {
                    if (!_querySystem.TryGetSurfaceInRange(context, range, i, out MapSurfaceSample sample) ||
                        !TryIntersectSurface(grid, ray, sample, out Vector3 worldPoint, out float distance))
                    {
                        continue;
                    }

                    int2 hitCell = GridUtils.WorldToCell(grid, worldPoint);
                    if (!hitCell.Equals(sample.Cell) || !GridUtils.InBounds(hitCell, grid.Width, grid.Height))
                        continue;

                    float score = distance;
                    if (sample.SurfaceId == preferredSurfaceId)
                        score -= 0.05f;
                    if (sample.LayerId == preferredLayerId)
                        score -= 0.025f;

                    if (!found || score < bestScore)
                    {
                        bestScore = score;
                        result = Result.SurfaceHit(hitCell, worldPoint, sample);
                        found = true;
                    }
                }
            }
        }

        return found;
    }

    private static bool TryIntersectSurface(GridConfig grid, Ray ray, MapSurfaceSample sample, out Vector3 worldPoint, out float distance)
    {
        worldPoint = default;
        distance = 0f;

        Vector3 sampleCenter = GridUtils.CellToWorldCenter(grid, sample.Cell);
        sampleCenter.y = sample.Height;
        Vector3 normal = math.lengthsq(sample.Normal) > 0.0001f
            ? new Vector3(sample.Normal.x, sample.Normal.y, sample.Normal.z).normalized
            : Vector3.up;

        Plane plane = new(normal, sampleCenter);
        if (!plane.Raycast(ray, out distance) || distance < 0f)
            return false;

        worldPoint = ray.GetPoint(distance);
        return true;
    }

    private static void TryResolvePreferredSelectionLayer(
        EntityManager entityManager,
        SelectionStateSystem selectionStateSystem,
        out int preferredSurfaceId,
        out int preferredLayerId)
    {
        preferredSurfaceId = -1;
        preferredLayerId = -1;
        if (selectionStateSystem == null)
            return;

        if (TryReadSurface(entityManager, selectionStateSystem.FocusedUnit, out preferredSurfaceId, out preferredLayerId))
            return;

        System.Collections.Generic.List<Entity> selected = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < selected.Count; i++)
        {
            if (TryReadSurface(entityManager, selected[i], out preferredSurfaceId, out preferredLayerId))
                return;
        }
    }

    private static bool TryReadSurface(EntityManager entityManager, Entity entity, out int surfaceId, out int layerId)
    {
        surfaceId = -1;
        layerId = -1;
        if (entity == Entity.Null ||
            !entityManager.Exists(entity) ||
            !entityManager.HasComponent<UnitSurfaceComponent>(entity))
        {
            return false;
        }

        UnitSurfaceComponent surface = entityManager.GetComponentData<UnitSurfaceComponent>(entity);
        if (surface.HasSurface == 0)
            return false;

        surfaceId = surface.SurfaceId;
        layerId = surface.LayerId;
        return true;
    }
}
