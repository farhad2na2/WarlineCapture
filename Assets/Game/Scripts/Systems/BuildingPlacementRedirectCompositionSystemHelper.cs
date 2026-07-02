using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementRedirectCompositionSystemHelper
    {
        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
        public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData);
        public delegate void EnsureEntityQueriesDelegate(EntityManager entityManager);
        public delegate EntityQuery GetRedirectUnitsQueryDelegate();

        private readonly List<RectInt> _deferredRedirectFootprints = new();
        private int _deferSideEffectsDepth;
        private bool _pendingMarkerRefresh;

        public bool IsDeferringSideEffects => _deferSideEffectsDepth > 0;

        public readonly struct Context
        {
            public readonly TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly TryGetGridDataDelegate TryGetGridData;
            public readonly EnsureEntityQueriesDelegate EnsureEntityQueries;
            public readonly GetRedirectUnitsQueryDelegate GetRedirectUnitsQuery;

            public Context(
                TryGetEntityManagerDelegate tryGetEntityManager,
                TryGetGridDataDelegate tryGetGridData,
                EnsureEntityQueriesDelegate ensureEntityQueries,
                GetRedirectUnitsQueryDelegate getRedirectUnitsQuery)
            {
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                EnsureEntityQueries = ensureEntityQueries;
                GetRedirectUnitsQuery = getRedirectUnitsQuery;
            }
        }

        public void BeginDeferredRuntimeBuildingSideEffects(System.Action rebuildPlacementInvalidPrefix)
        {
            _deferSideEffectsDepth++;
            if (_deferSideEffectsDepth == 1)
                rebuildPlacementInvalidPrefix?.Invoke();
        }

        public void EndDeferredRuntimeBuildingSideEffects(
            Context context,
            System.Action refreshBuildingMarkerVisibility,
            System.Action clearPlacementInvalidPrefix)
        {
            if (_deferSideEffectsDepth <= 0)
                return;

            _deferSideEffectsDepth--;
            if (_deferSideEffectsDepth > 0)
                return;

            if (_deferredRedirectFootprints.Count > 0)
            {
                RedirectUnitsAroundPlacedBuildings(context, _deferredRedirectFootprints);
                _deferredRedirectFootprints.Clear();
            }

            FlushPendingMarkerRefresh(refreshBuildingMarkerVisibility);
            clearPlacementInvalidPrefix?.Invoke();
        }

        public void AddDeferredRedirectFootprint(RectInt occupiedRect)
        {
            _deferredRedirectFootprints.Add(occupiedRect);
        }

        public void MarkPendingMarkerRefresh()
        {
            _pendingMarkerRefresh = true;
        }

        public void FlushPendingMarkerRefresh(System.Action refreshBuildingMarkerVisibility)
        {
            if (!_pendingMarkerRefresh)
                return;

            refreshBuildingMarkerVisibility?.Invoke();
            _pendingMarkerRefresh = false;
        }

        public void RedirectUnitsAroundPlacedBuilding(Context context, RectInt footprintRect)
        {
            _deferredRedirectFootprints.Clear();
            _deferredRedirectFootprints.Add(footprintRect);
            RedirectUnitsAroundPlacedBuildings(context, _deferredRedirectFootprints);
            _deferredRedirectFootprints.Clear();
        }

        private static void RedirectUnitsAroundPlacedBuildings(Context context, IReadOnlyList<RectInt> placedFootprints)
        {
            if (placedFootprints == null || placedFootprints.Count == 0)
                return;
            if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
                return;
            if (context.TryGetGridData == null || !context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerComponent blockerData))
                return;

            var walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            var occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
            var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
            var redirectUnits = new NativeList<Entity>(Allocator.Temp);
            var redirectGoals = new NativeList<int2>(Allocator.Temp);
            var overlapFlags = new NativeList<byte>(Allocator.Temp);
            context.EnsureEntityQueries?.Invoke(em);
            EntityQuery redirectUnitsQuery = context.GetRedirectUnitsQuery != null
                ? context.GetRedirectUnitsQuery()
                : default;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = redirectUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
            using var units = new NativeList<Entity>(redirectUnitsQuery.CalculateEntityCount(), Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                units.AddRange(entities);
            }

            try
            {
                for (int footprintIndex = 0; footprintIndex < placedFootprints.Count; footprintIndex++)
                {
                    RectInt footprintRect = placedFootprints[footprintIndex];
                    ReserveBuildingBuffer(ref reserved, grid, footprintRect.position, footprintRect.size, 0);
                }

                NativeArray<int2> pathPool = default;
                if (em.HasComponent<PathPoolComponent>(gridEntity))
                    pathPool = em.GetComponentData<PathPoolComponent>(gridEntity).Cells.AsArray();

                for (int i = 0; i < units.Length; i++)
                {
                    Entity unit = units[i];
                    if (em.HasComponent<Prefab>(unit) || em.HasComponent<StaticGridBlocker>(unit))
                        continue;

                    bool needsRedirect = false;
                    RectInt matchedFootprint = default;
                    int2 currentCell = em.GetComponentData<UnitGrid>(unit).Cell;
                    for (int footprintIndex = 0; footprintIndex < placedFootprints.Count; footprintIndex++)
                    {
                        RectInt footprintRect = placedFootprints[footprintIndex];
                        if (IsCellInsideFootprint(currentCell, footprintRect.position, footprintRect.size))
                        {
                            matchedFootprint = footprintRect;
                            needsRedirect = true;
                            break;
                        }

                        if (em.HasComponent<UnitTarget>(unit))
                        {
                            int2 targetCell = em.GetComponentData<UnitTarget>(unit).Cell;
                            if (IsCellInsideFootprint(targetCell, footprintRect.position, footprintRect.size))
                            {
                                matchedFootprint = footprintRect;
                                needsRedirect = true;
                                break;
                            }
                        }
                    }

                    if (!needsRedirect && pathPool.IsCreated && em.HasComponent<UnitPathFollow>(unit) && em.HasComponent<UnitPathRange>(unit))
                    {
                        for (int footprintIndex = 0; footprintIndex < placedFootprints.Count; footprintIndex++)
                        {
                            RectInt footprintRect = placedFootprints[footprintIndex];
                            if (!DoesRemainingPathIntersectFootprint(em, unit, pathPool, footprintRect.position, footprintRect.size))
                                continue;

                            matchedFootprint = footprintRect;
                            needsRedirect = true;
                            break;
                        }
                    }

                    if (!needsRedirect)
                        continue;

                    if (!TryFindNearestPerimeterCell(
                        grid,
                        walkable,
                        blockerData.Blocked,
                        occupied,
                        ref reserved,
                        matchedFootprint.position,
                        matchedFootprint.size,
                        currentCell,
                        out int2 goal))
                    {
                        continue;
                    }

                    redirectUnits.Add(unit);
                    redirectGoals.Add(goal);
                    overlapFlags.Add((byte)(IsCellInsideFootprint(currentCell, matchedFootprint.position, matchedFootprint.size) ? 1 : 0));
                }

                ApplyRedirects(em, grid, redirectUnits, redirectGoals, overlapFlags);
            }
            finally
            {
                redirectUnits.Dispose();
                redirectGoals.Dispose();
                overlapFlags.Dispose();
                reserved.Dispose();
            }
        }

        private static void ApplyRedirects(
            EntityManager em,
            GridConfig grid,
            NativeList<Entity> redirectUnits,
            NativeList<int2> redirectGoals,
            NativeList<byte> overlapFlags)
        {
            for (int i = 0; i < redirectUnits.Length; i++)
            {
                Entity unit = redirectUnits[i];
                int2 goal = redirectGoals[i];
                bool wasInsideFootprint = overlapFlags[i] != 0;

                if (em.HasComponent<EngageTarget>(unit))
                    em.RemoveComponent<EngageTarget>(unit);
                if (em.HasComponent<UnitPathFollow>(unit))
                    em.RemoveComponent<UnitPathFollow>(unit);
                if (em.HasComponent<UnitPathRange>(unit))
                    em.RemoveComponent<UnitPathRange>(unit);
                if (em.HasComponent<AutoWanderMoveTag>(unit))
                    em.RemoveComponent<AutoWanderMoveTag>(unit);
                if (em.HasComponent<ManualMoveOrderTag>(unit))
                    em.RemoveComponent<ManualMoveOrderTag>(unit);

                if (wasInsideFootprint)
                {
                    float3 worldPosition = GridUtils.CellToWorldCenter(grid, goal);
                    em.SetComponentData(unit, new UnitGrid { Cell = goal });
                    if (em.HasComponent<LocalTransform>(unit))
                        em.SetComponentData(unit, LocalTransform.FromPosition(worldPosition));
                    if (em.HasComponent<UnitPrevWorldPos>(unit))
                        em.SetComponentData(unit, new UnitPrevWorldPos { Value = worldPosition });
                    if (em.HasComponent<UnitMoveVisualComponent>(unit))
                        em.SetComponentData(unit, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
                }

                if (wasInsideFootprint)
                {
                    if (em.HasComponent<UnitTarget>(unit))
                        em.RemoveComponent<UnitTarget>(unit);
                    if (em.HasComponent<UnitPathRequest>(unit))
                        em.RemoveComponent<UnitPathRequest>(unit);
                }
                else
                {
                    UnitMoveOrderRequestSystem.EnqueueAndProcessTargetPathMoveOrder(em, unit, goal);
                }
            }
        }

        private static bool DoesRemainingPathIntersectFootprint(
            EntityManager em,
            Entity unit,
            NativeArray<int2> pathPool,
            Vector2Int originCell,
            Vector2Int footprintCells)
        {
            UnitPathFollow follow = em.GetComponentData<UnitPathFollow>(unit);
            UnitPathRange range = em.GetComponentData<UnitPathRange>(unit);
            int startIndex = math.max(0, follow.PathIndex);
            int endIndex = math.min(range.Length, pathPool.Length - range.Start);
            for (int i = startIndex; i < endIndex; i++)
            {
                int poolIndex = range.Start + i;
                if ((uint)poolIndex >= (uint)pathPool.Length)
                    break;

                if (IsCellInsideFootprint(pathPool[poolIndex], originCell, footprintCells))
                    return true;
            }

            return false;
        }

        private static bool TryFindNearestPerimeterCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            Vector2Int originCell,
            Vector2Int footprintCells,
            int2 referenceCell,
            out int2 goal)
        {
            goal = default;
            int maxRadius = math.max(grid.Width, grid.Height);
            int bestScore = int.MaxValue;
            bool found = false;

            for (int extraRadius = 1; extraRadius <= maxRadius; extraRadius++)
            {
                int minX = originCell.x - extraRadius;
                int minY = originCell.y - extraRadius;
                int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
                int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

                for (int x = minX; x <= maxX; x++)
                {
                    TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, x, minY, ref bestScore, ref goal, ref found);
                    if (maxY != minY)
                        TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, x, maxY, ref bestScore, ref goal, ref found);
                }

                for (int y = minY + 1; y < maxY; y++)
                {
                    TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, minX, y, ref bestScore, ref goal, ref found);
                    if (maxX != minX)
                        TryScorePerimeterGoal(grid, walkable, blocked, occupied, reserved, referenceCell, maxX, y, ref bestScore, ref goal, ref found);
                }

                if (found)
                {
                    reserved.Set(GridUtils.CellToIndex(goal, grid.Width), true);
                    return true;
                }
            }

            return false;
        }

        private static void TryScorePerimeterGoal(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            in NativeBitArray reserved,
            int2 referenceCell,
            int x,
            int y,
            ref int bestScore,
            ref int2 bestCell,
            ref bool found)
        {
            if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
                return;

            int2 candidate = new(x, y);
            int index = GridUtils.CellToIndex(candidate, grid.Width);
            if (walkable[index].Value == 0 || blocked.IsSet(index) || occupied.IsSet(index) || reserved.IsSet(index))
                return;

            int score = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
            if (!found || score < bestScore)
            {
                bestScore = score;
                bestCell = candidate;
                found = true;
            }
        }

        private static bool IsCellInsideFootprint(int2 cell, Vector2Int originCell, Vector2Int footprintCells)
        {
            return cell.x >= originCell.x &&
                   cell.y >= originCell.y &&
                   cell.x < originCell.x + footprintCells.x &&
                   cell.y < originCell.y + footprintCells.y;
        }

        private static void ReserveBuildingBuffer(ref NativeBitArray reserved, GridConfig grid, Vector2Int originCell, Vector2Int footprintCells, int extraRadius)
        {
            int minX = Mathf.Max(0, originCell.x - extraRadius);
            int minY = Mathf.Max(0, originCell.y - extraRadius);
            int maxX = Mathf.Min(grid.Width, originCell.x + footprintCells.x + extraRadius);
            int maxY = Mathf.Min(grid.Height, originCell.y + footprintCells.y + extraRadius);

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    reserved.Set(index, true);
                }
            }
        }
    }
}
