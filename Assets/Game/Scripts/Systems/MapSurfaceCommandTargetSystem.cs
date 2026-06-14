using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class MapSurfaceCommandTargetSystem
{
    private const int TraversableTargetSearchRadius = 24;
    private readonly MapSurfaceQuerySystem _querySystem = new();
    private readonly MapSurfaceSlopeClassificationSystem _slopeClassificationSystem = new();
    private readonly MapSurfacePathfindingReadSystem _surfaceReadSystem = new();
    private readonly UnitMoveOrderSystem _moveOrderSystem = new();
    private readonly List<Entity> _selectedMoveEntities = new();

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
        MapSurfaceMovementMask movementMask = ResolveSelectedMovementMask(entityManager, selectionStateSystem);
        if (TryResolveSurfaceHit(
                surfaceContext,
                grid,
                ray,
                fallbackCell,
                movementMask,
                preferredSurfaceId,
                preferredLayerId,
                out result))
        {
            return true;
        }

        if (TryResolveNearestTraversableTarget(surfaceContext, grid, fallbackCell, movementMask, out result))
            return true;

        result = Result.FlatFallback(fallbackCell, fallbackWorldPoint);
        return true;
    }

    public bool TryResolveMoveCommandTarget(
        EntityManager entityManager,
        EntityQuery surfaceQuery,
        Entity gridEntity,
        GridConfig grid,
        Camera worldCamera,
        Vector2 screenPosition,
        SelectionStateSystem selectionStateSystem,
        out Result result)
    {
        if (!TryResolveCommandTarget(
                entityManager,
                surfaceQuery,
                grid,
                worldCamera,
                screenPosition,
                selectionStateSystem,
                out result))
        {
            return false;
        }

        if (TryResolveSelectedMoveFootprintTarget(
                entityManager,
                surfaceQuery,
                gridEntity,
                grid,
                selectionStateSystem,
                result.Cell,
                out _,
                out Result footprintResult))
        {
            result = footprintResult;
        }

        return true;
    }

    public bool TryResolveSelectedMoveFootprintTarget(
        EntityManager entityManager,
        EntityQuery surfaceQuery,
        Entity gridEntity,
        GridConfig grid,
        SelectionStateSystem selectionStateSystem,
        int2 desiredGoal,
        out int2 resolvedCell,
        out Result result)
    {
        resolvedCell = desiredGoal;
        result = default;
        if (!TryBuildSelectedGroundMoveEntityList(entityManager, selectionStateSystem, out Entity primaryEntity) ||
            !TryReadGridPathingData(
                entityManager,
                gridEntity,
                out NativeArray<GridWalkable> walkable,
                out DynamicBlockerComponent blockerData,
                out DynamicOccupancyComponent occupancyData))
        {
            return false;
        }

        MapSurfacePathfindingReadSystem.Context surfaceContext =
            _surfaceReadSystem.TryCreateContext(entityManager, surfaceQuery, out MapSurfacePathfindingReadSystem.Context resolvedSurfaceContext)
                ? resolvedSurfaceContext
                : _surfaceReadSystem.CreateFlatFallbackContext();

        var reservedGoalCells = new HashSet<int>();
        HashSet<int> selectedCurrentCells = _moveOrderSystem.BuildSelectedCurrentFootprintCells(entityManager, grid, _selectedMoveEntities);
        resolvedCell = _moveOrderSystem.FindManualMoveGoal(
            entityManager,
            grid,
            walkable,
            blockerData.Blocked,
            blockerData.FriendlyPassFactionIds,
            occupancyData.Occupied,
            reservedGoalCells,
            selectedCurrentCells,
            primaryEntity,
            desiredGoal,
            0,
            surfaceContext);

        MapSurfaceMovementMask movementMask = ResolveSelectedMovementMask(entityManager, selectionStateSystem);
        if (_querySystem.TryCreateContext(entityManager, surfaceQuery, out MapSurfaceQuerySystem.Context queryContext) &&
            TryResolveTraversableCell(queryContext, grid, resolvedCell, movementMask, out result))
        {
            return true;
        }

        Vector3 worldPoint = GridUtils.CellToWorldCenter(grid, resolvedCell);
        result = Result.FlatFallback(resolvedCell, worldPoint);
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

    private bool TryBuildSelectedGroundMoveEntityList(
        EntityManager entityManager,
        SelectionStateSystem selectionStateSystem,
        out Entity primaryEntity)
    {
        primaryEntity = Entity.Null;
        _selectedMoveEntities.Clear();
        if (selectionStateSystem == null)
            return false;

        TryAddGroundMoveEntity(entityManager, selectionStateSystem.FocusedUnit);
        List<Entity> selected = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < selected.Count; i++)
            TryAddGroundMoveEntity(entityManager, selected[i]);

        if (_selectedMoveEntities.Count == 0)
            return false;

        primaryEntity = _selectedMoveEntities[0];
        return true;
    }

    private void TryAddGroundMoveEntity(EntityManager entityManager, Entity entity)
    {
        if (entity == Entity.Null ||
            _selectedMoveEntities.Contains(entity) ||
            !SelectionStateSystem.IsCacheableSelectedMoveEntity(entityManager, entity) ||
            entityManager.HasComponent<UnitAirMovement>(entity) ||
            !entityManager.HasComponent<UnitFootprint>(entity) ||
            !entityManager.HasComponent<UnitMovementBehavior>(entity))
        {
            return;
        }

        _selectedMoveEntities.Add(entity);
    }

    private static bool TryReadGridPathingData(
        EntityManager entityManager,
        Entity gridEntity,
        out NativeArray<GridWalkable> walkable,
        out DynamicBlockerComponent blockerData,
        out DynamicOccupancyComponent occupancyData)
    {
        walkable = default;
        blockerData = default;
        occupancyData = default;
        if (gridEntity == Entity.Null ||
            !entityManager.Exists(gridEntity) ||
            !entityManager.HasBuffer<GridWalkable>(gridEntity))
        {
            return false;
        }

        DynamicBuffer<GridWalkable> walkableBuffer = entityManager.GetBuffer<GridWalkable>(gridEntity);
        if (walkableBuffer.Length == 0)
            return false;

        walkable = walkableBuffer.AsNativeArray();
        blockerData = entityManager.HasComponent<DynamicBlockerComponent>(gridEntity)
            ? entityManager.GetComponentData<DynamicBlockerComponent>(gridEntity)
            : default;
        occupancyData = entityManager.HasComponent<DynamicOccupancyComponent>(gridEntity)
            ? entityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity)
            : default;
        return true;
    }

    private bool TryResolveSurfaceHit(
        MapSurfaceQuerySystem.Context context,
        GridConfig grid,
        Ray ray,
        int2 fallbackCell,
        MapSurfaceMovementMask movementMask,
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
                        !CanTraverse(sample, movementMask) ||
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

    private bool TryResolveNearestTraversableTarget(
        MapSurfaceQuerySystem.Context context,
        GridConfig grid,
        int2 originCell,
        MapSurfaceMovementMask movementMask,
        out Result result)
    {
        result = default;
        if (movementMask == MapSurfaceMovementMask.None)
            return false;

        if (TryResolveTraversableCell(context, grid, originCell, movementMask, out result))
            return true;

        for (int radius = 1; radius <= TraversableTargetSearchRadius; radius++)
        {
            int ringLen = math.max(1, 8 * radius);
            for (int step = 0; step < ringLen; step++)
            {
                int2 candidate = originCell + SquareRingOffset(radius, step);
                if (TryResolveTraversableCell(context, grid, candidate, movementMask, out result))
                    return true;
            }
        }

        return false;
    }

    private bool TryResolveTraversableCell(
        MapSurfaceQuerySystem.Context context,
        GridConfig grid,
        int2 cell,
        MapSurfaceMovementMask movementMask,
        out Result result)
    {
        result = default;
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height) ||
            !_querySystem.TryGetSurfaceRange(context, cell, out MapSurfaceCellSurfaceRange range))
        {
            return false;
        }

        for (int i = 0; i < range.SurfaceCount; i++)
        {
            if (!_querySystem.TryGetSurfaceInRange(context, range, i, out MapSurfaceSample sample) ||
                !CanTraverse(sample, movementMask))
            {
                continue;
            }

            Vector3 worldPoint = GridUtils.CellToWorldCenter(grid, cell);
            worldPoint.y = sample.Height;
            result = Result.SurfaceHit(cell, worldPoint, sample);
            return true;
        }

        return false;
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

    private MapSurfaceMovementMask ResolveSelectedMovementMask(
        EntityManager entityManager,
        SelectionStateSystem selectionStateSystem)
    {
        if (selectionStateSystem == null)
            return MapSurfaceMovementMask.Infantry;

        bool hasGroundUnit = false;
        bool hasVehicle = false;
        if (TryReadMovement(entityManager, selectionStateSystem.FocusedUnit, out bool focusedVehicle))
        {
            hasGroundUnit = true;
            hasVehicle |= focusedVehicle;
        }

        System.Collections.Generic.List<Entity> selected = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = 0; i < selected.Count; i++)
        {
            if (!TryReadMovement(entityManager, selected[i], out bool vehicle))
                continue;

            hasGroundUnit = true;
            hasVehicle |= vehicle;
        }

        if (!hasGroundUnit)
            return MapSurfaceMovementMask.Infantry;

        return hasVehicle
            ? MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle
            : MapSurfaceMovementMask.Infantry;
    }

    private static bool TryReadMovement(EntityManager entityManager, Entity entity, out bool isVehicle)
    {
        isVehicle = false;
        if (entity == Entity.Null ||
            !entityManager.Exists(entity) ||
            entityManager.HasComponent<UnitAirMovement>(entity) ||
            !entityManager.HasComponent<UnitFootprint>(entity) ||
            !entityManager.HasComponent<UnitMovementBehavior>(entity))
        {
            return false;
        }

        UnitFootprint footprint = entityManager.GetComponentData<UnitFootprint>(entity);
        UnitMovementBehavior movementBehavior = entityManager.GetComponentData<UnitMovementBehavior>(entity);
        isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior);
        return true;
    }

    private bool CanTraverse(MapSurfaceSample sample, MapSurfaceMovementMask movementMask)
    {
        return _slopeClassificationSystem.AllowsMovement(sample, movementMask);
    }

    private static int2 SquareRingOffset(int radius, int step)
    {
        int topLen = (2 * radius) + 1;
        if (step < topLen)
            return new int2(-radius + step, radius);

        step -= topLen;
        int rightLen = 2 * radius;
        if (step < rightLen)
            return new int2(radius, (radius - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * radius;
        if (step < bottomLen)
            return new int2((radius - 1) - step, -radius);

        step -= bottomLen;
        return new int2(-radius, (-radius + 1) + step);
    }
}
