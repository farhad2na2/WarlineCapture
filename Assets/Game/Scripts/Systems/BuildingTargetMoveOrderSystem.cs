using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingTargetMoveOrderSystem
{
    private World _queryWorld;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridPathingQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _gridPathingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerData>(),
            ComponentType.ReadOnly<DynamicOccupancyData>());
    }

    public bool TryIssueMoveOrderToBuilding(
        EntityManager em,
        Vector2Int originCell,
        Vector2Int footprintCells)
    {
        EnsureEntityQueries(em);
        using var selectedEntities = _selectedMoveQuery.ToEntityArray(Allocator.Temp);
        if (selectedEntities.Length == 0)
            return false;

        if (_gridPathingQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerData>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;

        int2 referenceCell = em.GetComponentData<UnitGrid>(selectedEntities[0]).Cell;
        if (!TryFindBuildingApproachCell(grid, walkable, blocked, occupied, originCell, footprintCells, referenceCell, out int2 goal))
            return false;

        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];

            if (IsAlreadyMovingToGoal(em, entity, goal))
                continue;

            if (em.HasComponent<EngageTarget>(entity))
                em.RemoveComponent<EngageTarget>(entity);
            if (em.HasComponent<UnitPathFollow>(entity))
                em.RemoveComponent<UnitPathFollow>(entity);
            if (em.HasComponent<UnitPathRange>(entity))
                em.RemoveComponent<UnitPathRange>(entity);
            if (em.HasComponent<AutoWanderMoveTag>(entity))
                em.RemoveComponent<AutoWanderMoveTag>(entity);

            if (em.HasComponent<UnitTarget>(entity))
                em.SetComponentData(entity, new UnitTarget { Cell = goal });
            else
                em.AddComponentData(entity, new UnitTarget { Cell = goal });

            if (!em.HasComponent<UnitAirMovement>(entity))
            {
                if (em.HasComponent<UnitPathRequest>(entity))
                    em.SetComponentData(entity, new UnitPathRequest { Goal = goal });
                else
                    em.AddComponentData(entity, new UnitPathRequest { Goal = goal });
            }
            else if (em.HasComponent<UnitPathRequest>(entity))
            {
                em.RemoveComponent<UnitPathRequest>(entity);
            }

            if (!em.HasComponent<ManualMoveOrderTag>(entity))
                em.AddComponent<ManualMoveOrderTag>(entity);
        }

        return true;
    }

    private static bool IsAlreadyMovingToGoal(EntityManager em, Entity entity, int2 goal)
    {
        if (!em.Exists(entity))
            return false;

        bool sameTarget =
            em.HasComponent<UnitTarget>(entity) &&
            em.GetComponentData<UnitTarget>(entity).Cell.Equals(goal);
        bool samePendingRequest =
            em.HasComponent<UnitPathRequest>(entity) &&
            em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal);
        bool hasActiveMovement =
            em.HasComponent<UnitPathFollow>(entity) ||
            em.HasComponent<UnitPathRequest>(entity);

        return sameTarget && (samePendingRequest || hasActiveMovement);
    }

    private static bool TryFindBuildingApproachCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
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
                TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, x, minY, ref bestScore, ref goal, ref found);
                if (maxY != minY)
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, x, maxY, ref bestScore, ref goal, ref found);
            }

            for (int y = minY + 1; y < maxY; y++)
            {
                TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, minX, y, ref bestScore, ref goal, ref found);
                if (maxX != minX)
                    TryScoreBuildingApproachCandidate(grid, walkable, blocked, occupied, referenceCell, maxX, y, ref bestScore, ref goal, ref found);
            }

            if (found)
                return true;
        }

        return false;
    }

    private static void TryScoreBuildingApproachCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 referenceCell,
        int x,
        int y,
        ref int bestScore,
        ref int2 bestCell,
        ref bool found)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
        if (walkable[index].Value == 0 || blocked.IsSet(index) || occupied.IsSet(index))
            return;

        int score = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
        if (!found || score < bestScore)
        {
            bestScore = score;
            bestCell = new int2(x, y);
            found = true;
        }
    }
}
