using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class FocusableUnitLookupSystem
{
    private const float ClickScreenFallbackTorsoHeight = 0.85f;

    private struct FocusableUnitCoverage
    {
        public int2 Cell;
        public int2 Size;
        public int Padding;
    }

    private World _queryWorld;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _focusableUnitsQuery;
    private EntityQuery _changedFocusableGridQuery;
    private EntityQuery _changedFocusableFootprintQuery;
    private readonly Dictionary<int, List<Entity>> _focusableUnitsByCell = new();
    private readonly Dictionary<Entity, FocusableUnitCoverage> _focusableUnitCoverage = new();
    private int _lastFocusableUnitCount = -1;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        ResetLookup();
        _gridConfigQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        _focusableUnitsQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _changedFocusableGridQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _changedFocusableGridQuery.SetChangedVersionFilter(ComponentType.ReadOnly<UnitGrid>());
        _changedFocusableFootprintQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _changedFocusableFootprintQuery.SetChangedVersionFilter(ComponentType.ReadOnly<UnitFootprint>());
    }

    public bool TryGetClickedUnitEntity(
        EntityManager em,
        Camera worldCamera,
        int2 clickedCell,
        Vector2 screenPosition,
        out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        if (worldCamera == null)
            return false;

        EnsureEntityQueries(em);
        RefreshLookup(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return false;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        int cellIndex = GridUtils.CellToIndex(clickedCell, grid.Width);
        if (!_focusableUnitsByCell.TryGetValue(cellIndex, out List<Entity> candidates) || candidates == null || candidates.Count == 0)
            return false;

        float bestDistanceSq = float.MaxValue;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            Entity entity = candidates[i];
            if (!IsFocusableUnitCandidate(em, entity))
            {
                candidates.RemoveAt(i);
                _focusableUnitCoverage.Remove(entity);
                continue;
            }

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
            int padding = GetFocusablePadding(em, entity);
            if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, padding))
            {
                RefreshLookupEntry(em, grid, entity);
                continue;
            }

            Vector3 screen = worldCamera.WorldToScreenPoint(em.GetComponentData<LocalToWorld>(entity).Position);
            float distanceSq = (new Vector2(screen.x, screen.y) - screenPosition).sqrMagnitude;
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestEntity = entity;
            }
        }

        if (candidates.Count == 0)
            _focusableUnitsByCell.Remove(cellIndex);

        return bestEntity != Entity.Null;
    }

    public bool TryGetClickedUnitEntityByScreenDistance(
        EntityManager em,
        Camera worldCamera,
        Vector2 screenPosition,
        float maxDistancePixels,
        out Entity bestEntity)
    {
        bestEntity = Entity.Null;
        if (worldCamera == null || maxDistancePixels <= 0f)
            return false;

        EnsureEntityQueries(em);
        RefreshLookup(em);

        float maxDistanceSq = maxDistancePixels * maxDistancePixels;
        float bestDistanceSq = maxDistanceSq;
        using NativeArray<Entity> entities = _focusableUnitsQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsFocusableUnitCandidate(em, entity))
                continue;

            Vector3 worldPosition = em.GetComponentData<LocalToWorld>(entity).Position;
            float distanceSq = math.min(
                ScreenDistanceSq(worldCamera, worldPosition, screenPosition),
                ScreenDistanceSq(worldCamera, worldPosition + Vector3.up * ClickScreenFallbackTorsoHeight, screenPosition));
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                bestEntity = entity;
            }
        }

        return bestEntity != Entity.Null;
    }

    private void ResetLookup()
    {
        _focusableUnitsByCell.Clear();
        _focusableUnitCoverage.Clear();
        _lastFocusableUnitCount = -1;
    }

    private void RefreshLookup(EntityManager em)
    {
        EnsureEntityQueries(em);
        if (_gridConfigQuery.IsEmptyIgnoreFilter)
            return;

        GridConfig grid = em.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
        int focusableUnitCount = _focusableUnitsQuery.CalculateEntityCount();
        if (_lastFocusableUnitCount < 0 || focusableUnitCount != _lastFocusableUnitCount)
        {
            RebuildLookup(em, grid, focusableUnitCount);
            return;
        }

        bool gridChanged = !_changedFocusableGridQuery.IsEmptyIgnoreFilter;
        bool footprintChanged = !_changedFocusableFootprintQuery.IsEmptyIgnoreFilter;
        if (!gridChanged && !footprintChanged)
            return;

        var changedEntities = new HashSet<Entity>();
        if (gridChanged)
        {
            using var changedGridEntities = _changedFocusableGridQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < changedGridEntities.Length; i++)
                changedEntities.Add(changedGridEntities[i]);
        }

        if (footprintChanged)
        {
            using var changedFootprintEntities = _changedFocusableFootprintQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < changedFootprintEntities.Length; i++)
                changedEntities.Add(changedFootprintEntities[i]);
        }

        foreach (Entity entity in changedEntities)
            RefreshLookupEntry(em, grid, entity);
    }

    private void RebuildLookup(EntityManager em, GridConfig grid, int focusableUnitCount)
    {
        _focusableUnitsByCell.Clear();
        _focusableUnitCoverage.Clear();

        using var entities = _focusableUnitsQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsFocusableUnitCandidate(em, entity))
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int2 size = em.GetComponentData<UnitFootprint>(entity).Size;
            AddLookupEntry(em, grid, entity, cell, size);
        }

        _lastFocusableUnitCount = focusableUnitCount;
    }

    private void RefreshLookupEntry(EntityManager em, GridConfig grid, Entity entity)
    {
        if (_focusableUnitCoverage.TryGetValue(entity, out FocusableUnitCoverage previousCoverage))
        {
            RemoveLookupEntry(grid, entity, previousCoverage.Cell, previousCoverage.Size, previousCoverage.Padding);
            _focusableUnitCoverage.Remove(entity);
        }

        if (!IsFocusableUnitCandidate(em, entity))
            return;

        int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
        int2 size = em.GetComponentData<UnitFootprint>(entity).Size;
        AddLookupEntry(em, grid, entity, cell, size);
    }

    private static bool IsFocusableUnitCandidate(EntityManager em, Entity entity)
    {
        bool hasTransitTag = em.HasComponent<UnitSpawnTransitTag>(entity);
        if (hasTransitTag)
        {
            bool groundedIdleAirUnit =
                em.HasComponent<UnitAirComponent>(entity) &&
                !em.HasComponent<UnitTarget>(entity) &&
                !em.HasComponent<EngageTarget>(entity) &&
                em.GetComponentData<UnitAirComponent>(entity).Airborne == 0 &&
                em.GetComponentData<UnitAirComponent>(entity).ReturningHome == 0 &&
                em.GetComponentData<UnitAirComponent>(entity).TakeoffRolling == 0 &&
                em.GetComponentData<UnitAirComponent>(entity).LandingRolling == 0;

            if (!groundedIdleAirUnit)
                return false;
        }

        return em.Exists(entity) &&
            !em.HasComponent<Prefab>(entity) &&
            !em.HasComponent<StaticGridBlocker>(entity) &&
            em.HasComponent<Faction>(entity) &&
            em.HasComponent<UnitGrid>(entity) &&
            em.HasComponent<UnitMove>(entity) &&
            em.HasComponent<UnitFootprint>(entity) &&
            em.HasComponent<LocalToWorld>(entity);
    }

    private void AddLookupEntry(EntityManager em, GridConfig grid, Entity entity, int2 cell, int2 size)
    {
        int padding = GetFocusablePadding(em, entity);
        GetPaddedFocusableBounds(grid, cell, size, padding, out int2 min, out int2 max);

        for (int y = min.y; y < max.y; y++)
        {
            int rowStart = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = rowStart + x;
                if (!_focusableUnitsByCell.TryGetValue(index, out List<Entity> entities))
                {
                    entities = new List<Entity>();
                    _focusableUnitsByCell.Add(index, entities);
                }

                entities.Add(entity);
            }
        }

        _focusableUnitCoverage[entity] = new FocusableUnitCoverage
        {
            Cell = cell,
            Size = size,
            Padding = padding
        };
    }

    private void RemoveLookupEntry(GridConfig grid, Entity entity, int2 cell, int2 size, int padding)
    {
        GetPaddedFocusableBounds(grid, cell, size, padding, out int2 min, out int2 max);

        for (int y = min.y; y < max.y; y++)
        {
            int rowStart = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = rowStart + x;
                if (!_focusableUnitsByCell.TryGetValue(index, out List<Entity> entities))
                    continue;

                entities.Remove(entity);
                if (entities.Count == 0)
                    _focusableUnitsByCell.Remove(index);
            }
        }
    }

    private static void GetPaddedFocusableBounds(GridConfig grid, int2 centerCell, int2 size, int paddingAmount, out int2 min, out int2 max)
    {
        int2 clampedSize = UnitFootprintUtility.ClampSize(size);
        int2 padding = new int2(paddingAmount, paddingAmount);
        int2 paddedMin = UnitFootprintUtility.GetMinCell(centerCell, clampedSize) - padding;
        int2 paddedMax = paddedMin + clampedSize + (padding * 2);
        min = new int2(math.clamp(paddedMin.x, 0, grid.Width), math.clamp(paddedMin.y, 0, grid.Height));
        max = new int2(math.clamp(paddedMax.x, 0, grid.Width), math.clamp(paddedMax.y, 0, grid.Height));
    }

    private static int GetFocusablePadding(EntityManager em, Entity entity)
    {
        return em.HasComponent<UnitAirMovement>(entity) ? 4 : 1;
    }

    private static float ScreenDistanceSq(Camera worldCamera, Vector3 worldPosition, Vector2 screenPosition)
    {
        Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f)
            return float.MaxValue;

        return (new Vector2(screen.x, screen.y) - screenPosition).sqrMagnitude;
    }
}
