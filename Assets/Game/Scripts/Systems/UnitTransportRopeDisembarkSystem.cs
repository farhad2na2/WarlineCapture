using System.Collections.Generic;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct UnitTransportRopeDisembarkSystem : ISystem
{
    private const int RopeDropClearanceCells = 2;
    private const float RopeDropDurationSeconds = 1.2f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<UnitTransportRopeDisembarkRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        CleanupClearedLandingCells(em);

        Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerData blockerData = em.HasComponent<DynamicBlockerData>(gridEntity) ? em.GetComponentData<DynamicBlockerData>(gridEntity) : default;
        DynamicOccupancyData occupancyData = em.HasComponent<DynamicOccupancyData>(gridEntity) ? em.GetComponentData<DynamicOccupancyData>(gridEntity) : default;
        float now = (float)SystemAPI.Time.ElapsedTime;

        EntityCommandBuffer ecb = new(Allocator.Temp);
        List<Entity> droppedPassengers = new();

        foreach (var (request, transportGrid, transportFootprint, transportTransform, passengers, transport) in
                 SystemAPI.Query<RefRW<UnitTransportRopeDisembarkRequest>, RefRO<UnitGrid>, RefRO<UnitFootprint>, RefRO<LocalTransform>, DynamicBuffer<UnitTransportPassengerElement>>()
                     .WithEntityAccess())
        {
            if (passengers.Length <= 0)
            {
                if (IsRopeLandingClear(em, transport))
                    ecb.RemoveComponent<UnitTransportRopeDisembarkRequest>(transport);
                continue;
            }

            if (!IsRopeLandingClear(em, transport))
                continue;

            if (request.ValueRO.NextDropAt > 0f && now < request.ValueRO.NextDropAt)
                continue;

            int passengerIndex = FindExistingPassenger(em, passengers);
            if (passengerIndex < 0)
            {
                passengers.Clear();
                ecb.RemoveComponent<UnitTransportRopeDisembarkRequest>(transport);
                continue;
            }

            Entity passenger = passengers[passengerIndex].Passenger;
            passengers.RemoveAt(passengerIndex);

            if (!TryFindRopeDropCell(
                    grid,
                    walkable.AsNativeArray(),
                    blockerData.Blocked,
                    occupancyData.Occupied,
                    transportGrid.ValueRO.Cell,
                    transportFootprint.ValueRO.Size,
                    request.ValueRO.ReferenceCell,
                    request.ValueRO.DropCount,
                    out int2 dropCell))
            {
                passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });
                request.ValueRW.NextDropAt = now + 0.25f;
                continue;
            }

            float3 startPosition = ResolveTransportRopeAnchor(em, transport, transportTransform.ValueRO);
            float3 endPosition = startPosition;
            endPosition.y = grid.Origin.y;
            dropCell = GridUtils.WorldToCell(grid, endPosition);
            int2 shortDisperseCell = ResolveAdjacentDisperseCell(grid, dropCell, request.ValueRO.DropCount);
            if (startPosition.y < endPosition.y + 2f)
                startPosition.y = endPosition.y + 2f;

            if (em.HasComponent<Disabled>(passenger))
                ecb.RemoveComponent<Disabled>(passenger);
            RemoveIfPresent<UnitTransportPassenger>(ref ecb, em, passenger);
            RemoveIfPresent<UnitTransportBoardingTarget>(ref ecb, em, passenger);
            RemoveIfPresent<UnitTarget>(ref ecb, em, passenger);
            RemoveIfPresent<UnitPathRequest>(ref ecb, em, passenger);
            RemoveIfPresent<UnitPathFollow>(ref ecb, em, passenger);
            RemoveIfPresent<UnitPathRange>(ref ecb, em, passenger);
            RemoveIfPresent<ManualMoveOrderTag>(ref ecb, em, passenger);
            RemoveIfPresent<AutoWanderMoveTag>(ref ecb, em, passenger);
            RemoveIfPresent<EngageTarget>(ref ecb, em, passenger);

            if (em.HasComponent<UnitGrid>(passenger))
                ecb.SetComponent(passenger, new UnitGrid { Cell = dropCell });
            if (em.HasComponent<LocalTransform>(passenger))
                ecb.SetComponent(passenger, LocalTransform.FromPosition(startPosition));
            if (em.HasComponent<UnitTransportRopeLandingClearance>(passenger))
            {
                ecb.SetComponent(passenger, new UnitTransportRopeLandingClearance
                {
                    Transport = transport,
                    LandingCell = dropCell
                });
            }
            else
            {
                ecb.AddComponent(passenger, new UnitTransportRopeLandingClearance
                {
                    Transport = transport,
                    LandingCell = dropCell
                });
            }

            UnitTransportRopeDropState dropState = new()
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                DisperseCell = shortDisperseCell,
                StartedAt = now,
                DurationSeconds = RopeDropDurationSeconds,
                HasDisperseCell = (byte)(CellsEqual(shortDisperseCell, dropCell) ? 0 : 1)
            };
            if (em.HasComponent<UnitTransportRopeDropState>(passenger))
                ecb.SetComponent(passenger, dropState);
            else
                ecb.AddComponent(passenger, dropState);

            droppedPassengers.Add(passenger);
            request.ValueRW.NextDropAt = now + math.max(RopeDropDurationSeconds + 0.25f, request.ValueRO.DropIntervalSeconds);
            request.ValueRW.DropCount++;
        }

        ecb.Playback(em);
        ecb.Dispose();

        for (int i = 0; i < droppedPassengers.Count; i++)
            UnitTransportVisualUtility.SetPassengerVisible(em, droppedPassengers[i], true);
    }

    private static int FindExistingPassenger(EntityManager em, DynamicBuffer<UnitTransportPassengerElement> passengers)
    {
        for (int i = 0; i < passengers.Length; i++)
        {
            if (em.Exists(passengers[i].Passenger))
                return i;
        }

        return -1;
    }

    private static void CleanupClearedLandingCells(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitTransportRopeLandingClearance>(),
            ComponentType.ReadOnly<UnitGrid>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitTransportRopeLandingClearance> clearances = query.ToComponentDataArray<UnitTransportRopeLandingClearance>(Allocator.Temp);
        using NativeArray<UnitGrid> grids = query.ToComponentDataArray<UnitGrid>(Allocator.Temp);

        EntityCommandBuffer ecb = new(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            if (!em.Exists(clearances[i].Transport) || !grids[i].Cell.Equals(clearances[i].LandingCell))
                ecb.RemoveComponent<UnitTransportRopeLandingClearance>(entities[i]);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static bool IsRopeLandingClear(EntityManager em, Entity transport)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitTransportRopeLandingClearance>(),
            ComponentType.ReadOnly<UnitGrid>());
        using NativeArray<UnitTransportRopeLandingClearance> clearances = query.ToComponentDataArray<UnitTransportRopeLandingClearance>(Allocator.Temp);
        using NativeArray<UnitGrid> grids = query.ToComponentDataArray<UnitGrid>(Allocator.Temp);

        for (int i = 0; i < clearances.Length; i++)
        {
            if (clearances[i].Transport == transport &&
                grids[i].Cell.Equals(clearances[i].LandingCell))
            {
                return false;
            }
        }

        return true;
    }

    private static float3 ResolveTransportRopeAnchor(EntityManager em, Entity transport, LocalTransform transportTransform)
    {
        float3 anchor = transportTransform.Position;
        if (em.HasComponent<UnitModelLocalTransform>(transport))
        {
            UnitModelLocalTransform model = em.GetComponentData<UnitModelLocalTransform>(transport);
            anchor += math.rotate(transportTransform.Rotation, model.Position);
        }

        return anchor;
    }

    private static bool TryFindRopeDropCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int dropCount,
        out int2 dropCell)
    {
        dropCell = default;
        int2 size = UnitFootprintUtility.ClampSize(transportSize);
        int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
        int2 max = min + size;

        for (int radius = 1; radius <= RopeDropClearanceCells + 6; radius++)
        {
            int minX = min.x - radius;
            int minY = min.y - radius;
            int maxX = max.x - 1 + radius;
            int maxY = max.y - 1 + radius;
            int validCount = CountValidRopeDropCells(
                grid,
                walkable,
                blocked,
                occupied,
                minX,
                minY,
                maxX,
                maxY);
            if (validCount <= 0)
                continue;

            int desiredOrdinal = math.abs(dropCount) % validCount;
            if (TryGetValidRopeDropCellAtOrdinal(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    minX,
                    minY,
                    maxX,
                    maxY,
                    referenceCell,
                    desiredOrdinal,
                    out dropCell))
                return true;
        }

        return false;
    }

    private static int2 ResolveAdjacentDisperseCell(in GridConfig grid, int2 landingCell, int dropCount)
    {
        int2 candidate = landingCell + DirectionForDropCount(dropCount);
        if (grid.Width > 0)
            candidate.x = math.clamp(candidate.x, 0, grid.Width - 1);
        if (grid.Height > 0)
            candidate.y = math.clamp(candidate.y, 0, grid.Height - 1);
        if (!CellsEqual(candidate, landingCell))
            return candidate;

        for (int i = 0; i < 8; i++)
        {
            candidate = landingCell + DirectionForDropCount(dropCount + i);
            if (grid.Width > 0)
                candidate.x = math.clamp(candidate.x, 0, grid.Width - 1);
            if (grid.Height > 0)
                candidate.y = math.clamp(candidate.y, 0, grid.Height - 1);
            if (!CellsEqual(candidate, landingCell))
                return candidate;
        }

        return landingCell;
    }

    private static int2 DirectionForDropCount(int dropCount)
    {
        return math.abs(dropCount) % 8 switch
        {
            0 => new int2(1, 0),
            1 => new int2(1, 1),
            2 => new int2(0, 1),
            3 => new int2(-1, 1),
            4 => new int2(-1, 0),
            5 => new int2(-1, -1),
            6 => new int2(0, -1),
            _ => new int2(1, -1)
        };
    }

    private static bool CellsEqual(int2 a, int2 b) => a.x == b.x && a.y == b.y;

    private static int CountValidRopeDropCells(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int minX,
        int minY,
        int maxX,
        int maxY)
    {
        int count = 0;
        ForEachRopeDropRingCell(grid, walkable, blocked, occupied, minX, minY, maxX, maxY, (int2 _) => count++);
        return count;
    }

    private static bool TryGetValidRopeDropCellAtOrdinal(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int minX,
        int minY,
        int maxX,
        int maxY,
        int2 referenceCell,
        int desiredOrdinal,
        out int2 dropCell)
    {
        dropCell = default;
        int ordinal = 0;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!IsValidRopeDropRingCell(grid, walkable, blocked, occupied, minX, minY, maxX, maxY, x, y, out int2 candidate))
                    continue;

                if (ordinal == desiredOrdinal)
                {
                    dropCell = candidate;
                    return true;
                }

                ordinal++;
            }
        }

        return false;
    }

    private static void ForEachRopeDropRingCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int minX,
        int minY,
        int maxX,
        int maxY,
        System.Action<int2> action)
    {
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!IsValidRopeDropRingCell(grid, walkable, blocked, occupied, minX, minY, maxX, maxY, x, y, out int2 candidate))
                    continue;
                action(candidate);
            }
        }
    }

    private static bool IsValidRopeDropRingCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int minX,
        int minY,
        int maxX,
        int maxY,
        int x,
        int y,
        out int2 candidate)
    {
        candidate = new int2(x, y);
        bool onRing = x == minX || x == maxX || y == minY || y == maxY;
        if (!onRing)
            return false;

        if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
            return false;

        int index = GridUtils.CellToIndex(candidate, grid.Width);
        if (walkable[index].Value == 0)
            return false;
        if (blocked.IsCreated && blocked.IsSet(index))
            return false;
        if (occupied.IsCreated && occupied.IsSet(index))
            return false;

        return true;
    }

    private static void RemoveIfPresent<T>(ref EntityCommandBuffer ecb, EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }
}

public partial struct UnitTransportRopeDropSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<UnitTransportRopeDropState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float now = (float)SystemAPI.Time.ElapsedTime;
        EntityManager em = state.EntityManager;
        Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerData blockerData = em.HasComponent<DynamicBlockerData>(gridEntity) ? em.GetComponentData<DynamicBlockerData>(gridEntity) : default;
        DynamicOccupancyData occupancyData = em.HasComponent<DynamicOccupancyData>(gridEntity) ? em.GetComponentData<DynamicOccupancyData>(gridEntity) : default;
        using EntityQuery liveUnitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>());
        using NativeArray<Entity> liveEntities = liveUnitQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveGrids = liveUnitQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveFootprints = liveUnitQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);
        EntityCommandBuffer ecb = new(Allocator.Temp);

        foreach (var (transform, drop, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitTransportRopeDropState>>()
                     .WithEntityAccess())
        {
            float duration = math.max(0.01f, drop.ValueRO.DurationSeconds);
            float t = math.saturate((now - drop.ValueRO.StartedAt) / duration);
            transform.ValueRW.Position = math.lerp(drop.ValueRO.StartPosition, drop.ValueRO.EndPosition, t);
            if (t >= 1f)
            {
                transform.ValueRW.Position = drop.ValueRO.EndPosition;
                IssueDisperseMoveOrder(
                    em,
                    ecb,
                    entity,
                    transform.ValueRW.Position,
                    now,
                    grid,
                    walkable.AsNativeArray(),
                    blockerData.Blocked,
                    occupancyData.Occupied,
                    liveEntities,
                    liveGrids,
                    liveFootprints);
                ecb.RemoveComponent<UnitTransportRopeDropState>(entity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static void IssueDisperseMoveOrder(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity entity,
        float3 currentPosition,
        float now,
        GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        in NativeArray<Entity> liveEntities,
        in NativeArray<UnitGrid> liveGrids,
        in NativeArray<UnitFootprint> liveFootprints)
    {
        if (!em.HasComponent<UnitGrid>(entity) ||
            !em.HasComponent<UnitFootprint>(entity))
        {
            if (em.HasComponent<UnitTransportRopeLandingClearance>(entity))
                ecb.RemoveComponent<UnitTransportRopeLandingClearance>(entity);
            return;
        }

        int2 landingCell = em.GetComponentData<UnitGrid>(entity).Cell;
        int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
        if (!TryFindFreeDisperseCell(
                grid,
                walkable,
                blocked,
                occupied,
                landingCell,
                footprint,
                entity,
                liveEntities,
                liveGrids,
                liveFootprints,
                out int2 disperseCell))
        {
            if (em.HasComponent<UnitTransportRopeLandingClearance>(entity))
                ecb.RemoveComponent<UnitTransportRopeLandingClearance>(entity);
            return;
        }

        RemoveIfPresent<UnitPathFollow>(ecb, em, entity);
        RemoveIfPresent<UnitPathRange>(ecb, em, entity);
        RemoveIfPresent<UnitTarget>(ecb, em, entity);
        RemoveIfPresent<UnitPathRequest>(ecb, em, entity);
        RemoveIfPresent<ManualMoveOrderTag>(ecb, em, entity);
        RemoveIfPresent<AutoWanderMoveTag>(ecb, em, entity);

        float3 endPosition = GridUtils.CellToWorldCenter(grid, disperseCell);
        endPosition.y = currentPosition.y;
        float speed = em.HasComponent<UnitMove>(entity) ? math.max(0.1f, em.GetComponentData<UnitMove>(entity).Speed) : 2f;
        float duration = math.max(0.1f, math.distance(currentPosition, endPosition) / speed);
        UnitTransportRopeDisperseState disperse = new()
        {
            StartPosition = currentPosition,
            EndPosition = endPosition,
            EndCell = disperseCell,
            StartedAt = now,
            DurationSeconds = duration
        };
        if (em.HasComponent<UnitTransportRopeDisperseState>(entity))
            ecb.SetComponent(entity, disperse);
        else
            ecb.AddComponent(entity, disperse);
        if (em.HasComponent<UnitTransportRopeLandingClearance>(entity))
            ecb.RemoveComponent<UnitTransportRopeLandingClearance>(entity);
        if (em.HasComponent<UnitMoveVisualState>(entity))
            ecb.SetComponent(entity, new UnitMoveVisualState { IsMoving = 1, StillSeconds = 0f });
    }

    private static bool TryFindFreeDisperseCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 landingCell,
        int2 footprint,
        Entity movingEntity,
        in NativeArray<Entity> liveEntities,
        in NativeArray<UnitGrid> liveGrids,
        in NativeArray<UnitFootprint> liveFootprints,
        out int2 disperseCell)
    {
        disperseCell = default;
        int ordinal = math.abs(movingEntity.Index + (movingEntity.Version * 17));
        for (int radius = 1; radius <= 12; radius++)
        {
            int validCount = CountValidDisperseCells(
                grid,
                walkable,
                blocked,
                occupied,
                landingCell,
                footprint,
                movingEntity,
                liveEntities,
                liveGrids,
                liveFootprints,
                radius);
            if (validCount <= 0)
                continue;

            int desiredOrdinal = ordinal % validCount;
            if (TryGetValidDisperseCellAtOrdinal(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    landingCell,
                    footprint,
                    movingEntity,
                    liveEntities,
                    liveGrids,
                    liveFootprints,
                    radius,
                    desiredOrdinal,
                    out disperseCell))
                return true;
        }

        return false;
    }

    private static int CountValidDisperseCells(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 landingCell,
        int2 footprint,
        Entity movingEntity,
        in NativeArray<Entity> liveEntities,
        in NativeArray<UnitGrid> liveGrids,
        in NativeArray<UnitFootprint> liveFootprints,
        int radius)
    {
        int count = 0;
        int minX = landingCell.x - radius;
        int minY = landingCell.y - radius;
        int maxX = landingCell.x + radius;
        int maxY = landingCell.y + radius;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (x != minX && x != maxX && y != minY && y != maxY)
                    continue;

                if (IsValidDisperseCell(grid, walkable, blocked, occupied, new int2(x, y), footprint, movingEntity, liveEntities, liveGrids, liveFootprints))
                    count++;
            }
        }

        return count;
    }

    private static bool TryGetValidDisperseCellAtOrdinal(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 landingCell,
        int2 footprint,
        Entity movingEntity,
        in NativeArray<Entity> liveEntities,
        in NativeArray<UnitGrid> liveGrids,
        in NativeArray<UnitFootprint> liveFootprints,
        int radius,
        int desiredOrdinal,
        out int2 disperseCell)
    {
        disperseCell = default;
        int ordinal = 0;
        int minX = landingCell.x - radius;
        int minY = landingCell.y - radius;
        int maxX = landingCell.x + radius;
        int maxY = landingCell.y + radius;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (x != minX && x != maxX && y != minY && y != maxY)
                    continue;

                int2 candidate = new int2(x, y);
                if (!IsValidDisperseCell(grid, walkable, blocked, occupied, candidate, footprint, movingEntity, liveEntities, liveGrids, liveFootprints))
                    continue;

                if (ordinal == desiredOrdinal)
                {
                    disperseCell = candidate;
                    return true;
                }

                ordinal++;
            }
        }

        return false;
    }

    private static bool IsValidDisperseCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 cell,
        int2 footprint,
        Entity movingEntity,
        in NativeArray<Entity> liveEntities,
        in NativeArray<UnitGrid> liveGrids,
        in NativeArray<UnitFootprint> liveFootprints)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprint);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
            return false;

        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                int index = row + x;
                if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
                    return false;
                if (blocked.IsCreated && blocked.IsSet(index))
                    return false;
                if (occupied.IsCreated && occupied.IsSet(index))
                    return false;
            }
        }

        for (int i = 0; i < liveEntities.Length; i++)
        {
            if (liveEntities[i] == movingEntity)
                continue;
            if (UnitFootprintUtility.Overlaps(cell, size, liveGrids[i].Cell, liveFootprints[i].Size))
                return false;
        }

        return true;
    }

    private static void RemoveIfPresent<T>(EntityCommandBuffer ecb, EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

}

public partial struct UnitTransportRopeDisperseSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitTransportRopeDisperseState>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float now = (float)SystemAPI.Time.ElapsedTime;
        EntityCommandBuffer ecb = new(Allocator.Temp);

        foreach (var (transform, disperse, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitTransportRopeDisperseState>>()
                     .WithEntityAccess())
        {
            float duration = math.max(0.01f, disperse.ValueRO.DurationSeconds);
            float t = math.saturate((now - disperse.ValueRO.StartedAt) / duration);
            transform.ValueRW.Position = math.lerp(disperse.ValueRO.StartPosition, disperse.ValueRO.EndPosition, t);
            if (t < 1f)
                continue;

            transform.ValueRW.Position = disperse.ValueRO.EndPosition;
            if (state.EntityManager.HasComponent<UnitGrid>(entity))
                ecb.SetComponent(entity, new UnitGrid { Cell = disperse.ValueRO.EndCell });
            if (state.EntityManager.HasComponent<UnitTransportRopeLandingClearance>(entity))
                ecb.RemoveComponent<UnitTransportRopeLandingClearance>(entity);
            ecb.RemoveComponent<UnitTransportRopeDisperseState>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
