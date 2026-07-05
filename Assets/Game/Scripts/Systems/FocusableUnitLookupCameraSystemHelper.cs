using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed class FocusableUnitLookupCameraSystemHelper
    {
        private const float ClickScreenFallbackTorsoHeight = 0.85f;
        private const float SelectionHitboxScreenPaddingPixels = 8f;
        private const float MinimumSelectionHitboxExtent = 0.05f;
        private const int MaxAirSelectionHitboxPaddingCells = 32;

        private struct FocusableUnitCoverage
        {
            public int2 Cell;
            public int2 Size;
            public int Padding;
        }

        private World _queryWorld;
        private EntityQuery _gridConfigQuery;
        private EntityQuery _focusableUnitsQuery;
        private EntityQuery _changedFocusableCoverageQuery;
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
            _focusableUnitsQuery = em.CreateEntityQuery(CreateFocusableUnitsQueryDesc());
            _changedFocusableCoverageQuery = em.CreateEntityQuery(CreateFocusableUnitsQueryDesc());
            _changedFocusableCoverageQuery.SetChangedVersionFilter(new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>()
            });
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
                int padding = GetFocusablePadding(em, entity, grid);
                if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, padding))
                {
                    RefreshLookupEntry(em, grid, entity);
                    continue;
                }

                LocalToWorld localToWorld = em.GetComponentData<LocalToWorld>(entity);
                Vector3 screen = worldCamera.WorldToScreenPoint(localToWorld.Position);
                float distanceSq = (new Vector2(screen.x, screen.y) - screenPosition).sqrMagnitude;
                if (em.HasComponent<UnitSelectionHitbox>(entity) &&
                    TryGetSelectionHitboxScreenDistanceSq(
                        worldCamera,
                        localToWorld.Value,
                        em.GetComponentData<UnitSelectionHitbox>(entity),
                        screenPosition,
                        out float hitboxDistanceSq))
                {
                    distanceSq = math.min(distanceSq, hitboxDistanceSq);
                }

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
            using NativeList<FocusableScreenDistanceCandidate> candidates = new(
                _focusableUnitsQuery.CalculateEntityCount(),
                Allocator.TempJob);
            JobHandle collectHandle = new CollectFocusableScreenDistanceCandidatesJob
            {
                EntityType = em.GetEntityTypeHandle(),
                LocalToWorldType = em.GetComponentTypeHandle<LocalToWorld>(true),
                TransitTagType = em.GetComponentTypeHandle<UnitSpawnTransitTag>(true),
                UnitAirType = em.GetComponentTypeHandle<UnitAirComponent>(true),
                UnitTargetType = em.GetComponentTypeHandle<UnitTarget>(true),
                EngageTargetType = em.GetComponentTypeHandle<EngageTarget>(true),
                UnitSelectionHitboxType = em.GetComponentTypeHandle<UnitSelectionHitbox>(true),
                Candidates = candidates
            }.Schedule(_focusableUnitsQuery, default);
            collectHandle.Complete();

            for (int i = 0; i < candidates.Length; i++)
            {
                FocusableScreenDistanceCandidate candidate = candidates[i];
                Vector3 worldPosition = candidate.Position;
                float distanceSq = math.min(
                    ScreenDistanceSq(worldCamera, worldPosition, screenPosition),
                    ScreenDistanceSq(worldCamera, worldPosition + Vector3.up * ClickScreenFallbackTorsoHeight, screenPosition));
                if (candidate.HasSelectionHitbox != 0 &&
                    TryGetSelectionHitboxScreenDistanceSq(
                        worldCamera,
                        candidate.LocalToWorld,
                        candidate.SelectionHitbox,
                        screenPosition,
                        out float hitboxDistanceSq))
                {
                    distanceSq = math.min(distanceSq, hitboxDistanceSq);
                }

                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestEntity = candidate.Entity;
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

        private static EntityQueryDesc CreateFocusableUnitsQueryDesc()
        {
            return new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitMove>(),
                    ComponentType.ReadOnly<UnitFootprint>(),
                    ComponentType.ReadOnly<LocalToWorld>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<StaticGridBlocker>()
                }
            };
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

            if (_changedFocusableCoverageQuery.IsEmptyIgnoreFilter)
                return;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _changedFocusableCoverageQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var changedEntities = new NativeList<Entity>(_changedFocusableCoverageQuery.CalculateEntityCount(), Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                changedEntities.AddRange(entities);
            }

            for (int i = 0; i < changedEntities.Length; i++)
                RefreshLookupEntry(em, grid, changedEntities[i]);
        }

        private void RebuildLookup(EntityManager em, GridConfig grid, int focusableUnitCount)
        {
            _focusableUnitsByCell.Clear();
            _focusableUnitCoverage.Clear();

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<UnitGrid> unitGridType = em.GetComponentTypeHandle<UnitGrid>(true);
            ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
            using NativeArray<ArchetypeChunk> chunks = _focusableUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<UnitGrid> grids = chunk.GetNativeArray(ref unitGridType);
                NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!IsFocusableUnitCandidate(em, entity))
                        continue;

                    AddLookupEntry(em, grid, entity, grids[i].Cell, footprints[i].Size);
                }
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
            int padding = GetFocusablePadding(em, entity, grid);
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

        private static int GetFocusablePadding(EntityManager em, Entity entity, GridConfig grid)
        {
            int padding = em.HasComponent<UnitAirMovement>(entity) ? 4 : 1;
            if (!em.HasComponent<UnitAirMovement>(entity) || !em.HasComponent<UnitSelectionHitbox>(entity))
                return padding;

            UnitSelectionHitbox hitbox = em.GetComponentData<UnitSelectionHitbox>(entity);
            float horizontalRadius = math.max(
                math.abs(hitbox.Center.x) + math.abs(hitbox.Extents.x),
                math.abs(hitbox.Center.z) + math.abs(hitbox.Extents.z));
            if (horizontalRadius <= MinimumSelectionHitboxExtent)
                return padding;

            float cellSize = math.max(0.01f, grid.CellSize);
            int visualPadding = (int)math.ceil(horizontalRadius / cellSize) + 1;
            return math.min(MaxAirSelectionHitboxPaddingCells, math.max(padding, visualPadding));
        }

        private static float ScreenDistanceSq(Camera worldCamera, Vector3 worldPosition, Vector2 screenPosition)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
                return float.MaxValue;

            return (new Vector2(screen.x, screen.y) - screenPosition).sqrMagnitude;
        }

        private static bool TryGetSelectionHitboxScreenDistanceSq(
            Camera worldCamera,
            float4x4 localToWorld,
            UnitSelectionHitbox hitbox,
            Vector2 screenPosition,
            out float distanceSq)
        {
            distanceSq = float.MaxValue;
            float3 extents = math.abs(hitbox.Extents);
            if (math.cmax(extents) <= MinimumSelectionHitboxExtent)
                return false;

            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            bool hasVisibleCorner = false;
            for (int ix = 0; ix < 2; ix++)
            {
                float x = ix == 0 ? -extents.x : extents.x;
                for (int iy = 0; iy < 2; iy++)
                {
                    float y = iy == 0 ? -extents.y : extents.y;
                    for (int iz = 0; iz < 2; iz++)
                    {
                        float z = iz == 0 ? -extents.z : extents.z;
                        float3 world = math.transform(localToWorld, hitbox.Center + new float3(x, y, z));
                        Vector3 screen = worldCamera.WorldToScreenPoint(new Vector3(world.x, world.y, world.z));
                        if (screen.z <= 0f)
                            continue;

                        Vector2 point = new(screen.x, screen.y);
                        min = Vector2.Min(min, point);
                        max = Vector2.Max(max, point);
                        hasVisibleCorner = true;
                    }
                }
            }

            if (!hasVisibleCorner)
                return false;

            Vector2 padding = Vector2.one * SelectionHitboxScreenPaddingPixels;
            min -= padding;
            max += padding;

            float dx = screenPosition.x < min.x
                ? min.x - screenPosition.x
                : (screenPosition.x > max.x ? screenPosition.x - max.x : 0f);
            float dy = screenPosition.y < min.y
                ? min.y - screenPosition.y
                : (screenPosition.y > max.y ? screenPosition.y - max.y : 0f);
            distanceSq = dx * dx + dy * dy;
            return true;
        }

        private struct FocusableScreenDistanceCandidate
        {
            public Entity Entity;
            public float3 Position;
            public float4x4 LocalToWorld;
            public UnitSelectionHitbox SelectionHitbox;
            public byte HasSelectionHitbox;
        }

        [BurstCompile]
        private struct CollectFocusableScreenDistanceCandidatesJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityType;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldType;
            [ReadOnly] public ComponentTypeHandle<UnitSpawnTransitTag> TransitTagType;
            [ReadOnly] public ComponentTypeHandle<UnitAirComponent> UnitAirType;
            [ReadOnly] public ComponentTypeHandle<UnitTarget> UnitTargetType;
            [ReadOnly] public ComponentTypeHandle<EngageTarget> EngageTargetType;
            [ReadOnly] public ComponentTypeHandle<UnitSelectionHitbox> UnitSelectionHitboxType;
            public NativeList<FocusableScreenDistanceCandidate> Candidates;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                bool hasTransitTag = chunk.Has(ref TransitTagType);
                bool hasAir = chunk.Has(ref UnitAirType);
                bool hasUnitTarget = chunk.Has(ref UnitTargetType);
                bool hasEngageTarget = chunk.Has(ref EngageTargetType);
                bool hasSelectionHitbox = chunk.Has(ref UnitSelectionHitboxType);
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
                NativeArray<LocalToWorld> transforms = chunk.GetNativeArray(ref LocalToWorldType);
                NativeArray<UnitAirComponent> airStates = hasAir
                    ? chunk.GetNativeArray(ref UnitAirType)
                    : default;
                NativeArray<UnitSelectionHitbox> selectionHitboxes = hasSelectionHitbox
                    ? chunk.GetNativeArray(ref UnitSelectionHitboxType)
                    : default;

                for (int i = 0; i < entities.Length; i++)
                {
                    if (!IsFocusableTransitState(i, hasTransitTag, hasAir, hasUnitTarget, hasEngageTarget, airStates))
                        continue;

                    Candidates.Add(new FocusableScreenDistanceCandidate
                    {
                        Entity = entities[i],
                        Position = transforms[i].Position,
                        LocalToWorld = transforms[i].Value,
                        SelectionHitbox = hasSelectionHitbox ? selectionHitboxes[i] : default,
                        HasSelectionHitbox = (byte)(hasSelectionHitbox ? 1 : 0)
                    });
                }
            }

            private static bool IsFocusableTransitState(
                int index,
                bool hasTransitTag,
                bool hasAir,
                bool hasUnitTarget,
                bool hasEngageTarget,
                NativeArray<UnitAirComponent> airStates)
            {
                if (!hasTransitTag)
                    return true;

                if (!hasAir || hasUnitTarget || hasEngageTarget)
                    return false;

                UnitAirComponent air = airStates[index];
                return air.Airborne == 0 &&
                       air.ReturningHome == 0 &&
                       air.TakeoffRolling == 0 &&
                       air.LandingRolling == 0;
            }
        }
    }
}
