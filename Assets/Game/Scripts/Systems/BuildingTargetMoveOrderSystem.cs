using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct BuildingTargetMoveOrderSystem : ISystem
{
    private EntityQuery _queueQuery;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridPathingQuery;
    private EntityTypeHandle _entityType;

    public void OnCreate(ref SystemState state)
    {
        _queueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<BuildingTargetMoveOrderQueueComponent>(),
            ComponentType.ReadWrite<BuildingTargetMoveOrderRequestElement>(),
            ComponentType.ReadWrite<BuildingTargetMoveOrderResultElement>());
        _selectedMoveQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _gridPathingQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        _entityType = state.GetEntityTypeHandle();
        EnsureCommandEntity(state.EntityManager, _queueQuery);
        state.RequireForUpdate(_queueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        _entityType.Update(ref state);
        ProcessPendingRequests(state.EntityManager, _queueQuery, _selectedMoveQuery, _gridPathingQuery, _entityType);
    }

    public readonly bool TryRequestMoveOrderToBuilding(EntityManager em, int2 originCell, int2 footprintCells)
    {
        int requestId = EnqueueMoveOrderToBuilding(em, originCell, footprintCells);
        ProcessPendingRequests(em);
        return TryGetResult(em, requestId, out BuildingTargetMoveOrderResultElement result) &&
               result.Accepted != 0;
    }

    public static int EnqueueMoveOrderToBuilding(EntityManager em, int2 originCell, int2 footprintCells)
    {
        Entity queueEntity = EnsureCommandEntity(em);
        BuildingTargetMoveOrderQueueComponent queue = em.GetComponentData<BuildingTargetMoveOrderQueueComponent>(queueEntity);
        queue.LastRequestId++;
        em.SetComponentData(queueEntity, queue);
        em.GetBuffer<BuildingTargetMoveOrderRequestElement>(queueEntity).Add(new BuildingTargetMoveOrderRequestElement
        {
            RequestId = queue.LastRequestId,
            OriginCell = originCell,
            FootprintCells = footprintCells
        });
        return queue.LastRequestId;
    }

    public static void ProcessPendingRequests(EntityManager em)
    {
        using EntityQuery queueQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingTargetMoveOrderQueueComponent>());
        using EntityQuery selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        using EntityQuery gridPathingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ProcessPendingRequests(em, queueQuery, selectedMoveQuery, gridPathingQuery, entityType);
    }

    private static void ProcessPendingRequests(
        EntityManager em,
        EntityQuery queueQuery,
        EntityQuery selectedMoveQuery,
        EntityQuery gridPathingQuery,
        EntityTypeHandle entityType)
    {
        Entity queueEntity = EnsureCommandEntity(em, queueQuery);
        DynamicBuffer<BuildingTargetMoveOrderRequestElement> requests = em.GetBuffer<BuildingTargetMoveOrderRequestElement>(queueEntity);
        if (requests.Length == 0)
            return;

        using NativeList<BuildingTargetMoveOrderRequestElement> pendingRequests = new(requests.Length, Allocator.Temp);
        for (int i = 0; i < requests.Length; i++)
            pendingRequests.Add(requests[i]);
        requests.Clear();

        DynamicBuffer<BuildingTargetMoveOrderResultElement> results = em.GetBuffer<BuildingTargetMoveOrderResultElement>(queueEntity);
        results.Clear();
        NativeArray<BuildingTargetMoveOrderRequestElement> pendingRequestArray = pendingRequests.AsArray();
        for (int i = 0; i < pendingRequestArray.Length; i++)
        {
            BuildingTargetMoveOrderRequestElement request = pendingRequestArray[i];
            bool accepted = TryApplyMoveOrderToBuilding(
                em,
                selectedMoveQuery,
                gridPathingQuery,
                entityType,
                request.OriginCell,
                request.FootprintCells,
                out int2 goal,
                out int issuedUnitCount);

            results = em.GetBuffer<BuildingTargetMoveOrderResultElement>(queueEntity);
            results.Add(new BuildingTargetMoveOrderResultElement
            {
                RequestId = request.RequestId,
                OriginCell = request.OriginCell,
                FootprintCells = request.FootprintCells,
                GoalCell = goal,
                IssuedUnitCount = issuedUnitCount,
                Accepted = accepted ? (byte)1 : (byte)0
            });
        }
    }

    private static bool TryGetResult(EntityManager em, int requestId, out BuildingTargetMoveOrderResultElement result)
    {
        result = default;
        Entity queueEntity = EnsureCommandEntity(em);
        DynamicBuffer<BuildingTargetMoveOrderResultElement> results = em.GetBuffer<BuildingTargetMoveOrderResultElement>(queueEntity);
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i].RequestId == requestId)
            {
                result = results[i];
                return true;
            }
        }

        return false;
    }

    private static Entity EnsureCommandEntity(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingTargetMoveOrderQueueComponent>());
        return EnsureCommandEntity(em, query);
    }

    private static Entity EnsureCommandEntity(EntityManager em, EntityQuery query)
    {
        Entity entity;
        if (!query.IsEmptyIgnoreFilter)
        {
            entity = query.GetSingletonEntity();
            EnsureBuffers(em, entity);
            return entity;
        }

        entity = em.CreateEntity(typeof(BuildingTargetMoveOrderQueueComponent));
        em.SetName(entity, "BuildingTargetMoveOrders");
        EnsureBuffers(em, entity);
        return entity;
    }

    private static void EnsureBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<BuildingTargetMoveOrderRequestElement>(entity))
            em.AddBuffer<BuildingTargetMoveOrderRequestElement>(entity);
        if (!em.HasBuffer<BuildingTargetMoveOrderResultElement>(entity))
            em.AddBuffer<BuildingTargetMoveOrderResultElement>(entity);
    }

    private static bool TryApplyMoveOrderToBuilding(
        EntityManager em,
        EntityQuery selectedMoveQuery,
        EntityQuery gridPathingQuery,
        EntityTypeHandle entityType,
        int2 originCell,
        int2 footprintCells,
        out int2 goal,
        out int issuedUnitCount)
    {
        goal = default;
        issuedUnitCount = 0;
        using NativeList<Entity> selectedEntities = CollectSelectedMoveEntities(selectedMoveQuery, entityType);
        NativeArray<Entity> entities = selectedEntities.AsArray();
        if (entities.Length == 0)
            return false;

        if (gridPathingQuery.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;

        int2 referenceCell = em.GetComponentData<UnitGrid>(entities[0]).Cell;
        if (!TryFindBuildingApproachCell(grid, walkable, blocked, occupied, originCell, footprintCells, referenceCell, out goal))
            return false;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];

            if (IsAlreadyMovingToGoal(em, entity, goal))
                continue;

            if (UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, entity, goal))
                issuedUnitCount++;
        }

        return true;
    }

    private static NativeList<Entity> CollectSelectedMoveEntities(EntityQuery selectedMoveQuery, EntityTypeHandle entityType)
    {
        int count = selectedMoveQuery.CalculateEntityCount();
        NativeList<Entity> selectedEntities = new(count, Allocator.Temp);
        if (count <= 0)
            return selectedEntities;

        using NativeArray<ArchetypeChunk> chunks = selectedMoveQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
                selectedEntities.Add(entities[i]);
        }

        return selectedEntities;
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
        int2 originCell,
        int2 footprintCells,
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
