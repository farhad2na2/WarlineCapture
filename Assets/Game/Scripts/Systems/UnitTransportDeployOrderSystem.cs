using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(UnitAirMovementSystem))]
[UpdateAfter(typeof(UnitGridMovementSystem))]
[UpdateBefore(typeof(UnitTransportAirdropSystem))]
public partial struct UnitTransportDeployOrderSystem : ISystem
{
    private const int DeployCellSearchRadius = 12;

    private EntityQuery _gridPathingQuery;
    private EntityQuery _deployQuery;
    private EntityTypeHandle _entityType;

    public void OnCreate(ref SystemState state)
    {
        _gridPathingQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        _deployQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<UnitTransportDeployOrder>(),
            ComponentType.ReadOnly<UnitGrid>());
        _entityType = state.GetEntityTypeHandle();
        state.RequireForUpdate(_deployQuery);
        state.RequireForUpdate(_gridPathingQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_gridPathingQuery.IsEmptyIgnoreFilter)
            return;

        EntityManager em = state.EntityManager;
        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;

        var capacitySystem = new UnitTransportCapacitySystem();
        var moveOrderSystem = new UnitMoveOrderSystem();
        _entityType.Update(ref state);
        using NativeArray<ArchetypeChunk> deployChunks = _deployQuery.ToArchetypeChunkArray(Allocator.Temp);
        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            bool stopProcessing = false;
            for (int chunkIndex = 0; chunkIndex < deployChunks.Length && !stopProcessing; chunkIndex++)
            {
                NativeArray<Entity> deployEntities = deployChunks[chunkIndex].GetNativeArray(_entityType);
                for (int i = 0; i < deployEntities.Length; i++)
                {
                    Entity entity = deployEntities[i];
                    if (!em.Exists(entity) ||
                        !em.HasComponent<UnitTransportDeployOrder>(entity) ||
                        !em.HasComponent<UnitGrid>(entity))
                    {
                        continue;
                    }

                    if (!IsLoadedTransport(em, entity))
                    {
                        RemoveIfPresent<UnitTransportDeployOrder>(em, ecb, entity);
                        continue;
                    }

                    UnitTransportDeployOrder order = em.GetComponentData<UnitTransportDeployOrder>(entity);
                    if (!TryResolveDeployCell(grid, walkable, blocked, occupied, order.TargetCell, out int2 deployCell))
                    {
                        RemoveIfPresent<UnitTransportDeployOrder>(em, ecb, entity);
                        continue;
                    }

                    if (TransportBoardingCommandSystem.IsCargoPlaneTransport(em, entity))
                    {
                        bool issued = TransportBoardingCommandSystem.TryIssueDeployDisembark(
                            em,
                            entity,
                            capacitySystem,
                            moveOrderSystem,
                            _gridPathingQuery,
                            deployCell,
                            order.TargetEntity,
                            order.TargetCell,
                            order.TargetPosition,
                            order.AttackAfterDeploy,
                            out TacticalCommandReasonCode reasonCode);

                        if (issued || IsTerminalDeployFailure(reasonCode))
                            RemoveIfPresent<UnitTransportDeployOrder>(em, ecb, entity);
                        stopProcessing = true;
                        break;
                    }

                    UnitGrid unitGrid = em.GetComponentData<UnitGrid>(entity);
                    if (!HasReachedDeployCell(unitGrid.Cell, deployCell, em, entity))
                    {
                        IssueDeployMove(em, ecb, entity, deployCell);
                        continue;
                    }

                    bool disembarked = TransportBoardingCommandSystem.TryIssueDeployDisembark(
                        em,
                        entity,
                        capacitySystem,
                        moveOrderSystem,
                        _gridPathingQuery,
                        deployCell,
                        order.TargetEntity,
                        order.TargetCell,
                        order.TargetPosition,
                        order.AttackAfterDeploy,
                        out TacticalCommandReasonCode disembarkReason);

                    if (disembarked || IsTerminalDeployFailure(disembarkReason))
                        RemoveIfPresent<UnitTransportDeployOrder>(em, ecb, entity);
                    stopProcessing = true;
                    break;
                }
            }

            ecb.Playback(em);
        }
        finally
        {
            ecb.Dispose();
        }
    }

    private static bool IsLoadedTransport(EntityManager em, Entity entity)
    {
        return em.Exists(entity) &&
               em.HasComponent<UnitMove>(entity) &&
               em.HasBuffer<UnitTransportPassengerElement>(entity) &&
               em.GetBuffer<UnitTransportPassengerElement>(entity).Length > 0;
    }

    private static bool HasReachedDeployCell(int2 currentCell, int2 deployCell, EntityManager em, Entity entity)
    {
        int clearance = TransportBoardingCommandSystem.GetTransportBoardingDirectCells(em, entity);
        int2 delta = math.abs(currentCell - deployCell);
        return math.max(delta.x, delta.y) <= math.max(1, clearance);
    }

    private static bool IsTerminalDeployFailure(TacticalCommandReasonCode reasonCode)
    {
        return reasonCode != TacticalCommandReasonCode.None &&
               reasonCode != TacticalCommandReasonCode.CommandUnavailable &&
               reasonCode != TacticalCommandReasonCode.NoDisembarkCell;
    }

    private static bool TryResolveDeployCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 referenceCell,
        out int2 deployCell)
    {
        deployCell = referenceCell;
        if (IsValidDeployCell(grid, walkable, blocked, occupied, referenceCell))
            return true;

        int bestDistanceSq = int.MaxValue;
        bool found = false;
        for (int radius = 1; radius <= DeployCellSearchRadius; radius++)
        {
            int minX = referenceCell.x - radius;
            int maxX = referenceCell.x + radius;
            int minY = referenceCell.y - radius;
            int maxY = referenceCell.y + radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x != minX && x != maxX && y != minY && y != maxY)
                        continue;

                    int2 candidate = new(x, y);
                    if (!IsValidDeployCell(grid, walkable, blocked, occupied, candidate))
                        continue;

                    int2 delta = candidate - referenceCell;
                    int distanceSq = delta.x * delta.x + delta.y * delta.y;
                    if (found && distanceSq >= bestDistanceSq)
                        continue;

                    deployCell = candidate;
                    bestDistanceSq = distanceSq;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private static bool IsValidDeployCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 cell)
    {
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
            return false;

        int index = GridUtils.CellToIndex(cell, grid.Width);
        if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
            return false;
        if (blocked.IsCreated && blocked.IsSet(index))
            return false;
        if (occupied.IsCreated && occupied.IsSet(index))
            return false;

        return true;
    }

    private static void IssueDeployMove(EntityManager em, EntityCommandBuffer ecb, Entity entity, int2 deployCell)
    {
        if (em.HasComponent<UnitTarget>(entity) &&
            em.GetComponentData<UnitTarget>(entity).Cell.Equals(deployCell))
        {
            return;
        }

        RemoveIfPresent<EngageTarget>(em, ecb, entity);
        RemoveIfPresent<UnitPathFollow>(em, ecb, entity);
        RemoveIfPresent<UnitPathRange>(em, ecb, entity);
        RemoveIfPresent<UnitPathRetryCooldown>(em, ecb, entity);
        RemoveIfPresent<UnitLongDistanceMove>(em, ecb, entity);
        RemoveIfPresent<AutoWanderMoveTag>(em, ecb, entity);
        RemoveIfPresent<HoldPositionOrderTag>(em, ecb, entity);
        RemoveIfPresent<BaseBreachOrder>(em, ecb, entity);
        RemoveIfPresent<UnitTransportBoardingTarget>(em, ecb, entity);
        RemoveIfPresent<UnitTransportRopeDisembarkRequest>(em, ecb, entity);
        RemoveIfPresent<UnitTransportAirdropRequest>(em, ecb, entity);
        RemoveIfPresent<UnitResourceHaulOrder>(em, ecb, entity);

        if (em.HasComponent<UnitAirMovement>(entity))
        {
            SetOrAdd(em, ecb, entity, new UnitTarget { Cell = deployCell });
            RemoveIfPresent<UnitPathRequest>(em, ecb, entity);
        }
        else
        {
            UnitMoveOrderRequestSystem.ApplyTargetPathMoveOrder(em, ecb, entity, deployCell);
        }

        if (!em.HasComponent<ManualMoveOrderTag>(entity))
            ecb.AddComponent<ManualMoveOrderTag>(entity);
    }

    private static void SetOrAdd<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.SetComponent(entity, value);
        else
            ecb.AddComponent(entity, value);
    }

    private static void RemoveIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.Exists(entity) && em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }
}
