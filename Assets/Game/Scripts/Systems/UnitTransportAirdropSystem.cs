using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UnitAirMovementSystem))]
public partial struct UnitTransportAirdropSystem : ISystem
{
    private const int LandingSearchRadius = 14;
    private const float SoldierDropDurationSeconds = 3.4f;
    private const float VehicleDropDurationSeconds = 4.8f;
    private const float SoldierDropIntervalSeconds = 0.65f;
    private const float VehicleDropIntervalSeconds = 1.45f;
    private const float MinimumDropHeight = 12f;
    private const float ParachuteVisualHeight = 2.2f;
    private const float CargoVisualHeight = 1.6f;
    private const float ParachuteVisualScale = 1.2f;
    private const float CargoVisualScale = 1f;
    private const float VisualCleanupDelaySeconds = 1.25f;
    private const float DoorCloseDelayAfterFinalDropSeconds = 0.75f;
    private const int SoldierSettleSearchRadius = 3;
    private const int VehicleSettleSearchRadius = 5;
    private const float SoldierSettleMinSeconds = 0.35f;
    private const float VehicleSettleMinSeconds = 0.75f;

    private MapSurfaceSpawnGrounding _spawnGroundingSystem;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity);
        DynamicBlockerComponent blockerData = em.HasComponent<DynamicBlockerComponent>(gridEntity)
            ? em.GetComponentData<DynamicBlockerComponent>(gridEntity)
            : default;
        DynamicOccupancyComponent occupancyData = em.HasComponent<DynamicOccupancyComponent>(gridEntity)
            ? em.GetComponentData<DynamicOccupancyComponent>(gridEntity)
            : default;
        float now = (float)SystemAPI.Time.ElapsedTime;

        EntityCommandBuffer ecb = new(Allocator.Temp);
        ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();

        foreach (var (transform, settle, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitTransportAirdropSettleComponent>>()
                     .WithEntityAccess())
        {
            UpdateSettleMove(em, ecb, transform, settle, entity, now);
        }

        foreach (var (transform, drop, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitTransportParachuteDropComponent>>()
                     .WithEntityAccess())
        {
            UpdateParachuteDrop(
                em,
                ecb,
                transform,
                drop,
                entity,
                transformLookup,
                now,
                grid,
                walkable.AsNativeArray(),
                blockerData.Blocked,
                occupancyData.Occupied);
        }

        foreach (var (transform, drop, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitTransportCargoDropComponent>>()
                     .WithEntityAccess())
        {
            UpdateCargoDrop(
                em,
                ecb,
                transform,
                drop,
                entity,
                transformLookup,
                now,
                grid,
                walkable.AsNativeArray(),
                blockerData.Blocked,
                occupancyData.Occupied);
        }

        foreach (var (cleanup, visual) in
                 SystemAPI.Query<RefRO<UnitTransportAirdropVisualCleanup>>()
                     .WithEntityAccess())
        {
            if (cleanup.ValueRO.DestroyAt > 0f && now >= cleanup.ValueRO.DestroyAt && em.Exists(visual))
                ecb.DestroyEntity(visual);
        }

        foreach (var (request, transportTransform, passengers, transport) in
                 SystemAPI.Query<RefRW<UnitTransportAirdropRequest>, RefRO<LocalTransform>, DynamicBuffer<UnitTransportPassengerElement>>()
                     .WithEntityAccess())
        {
            ProcessAirdropRequest(
                em,
                ecb,
                ref request.ValueRW,
                transportTransform.ValueRO,
                passengers,
                transport,
                now,
                grid,
                walkable.AsNativeArray(),
                blockerData.Blocked,
                occupancyData.Occupied);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private void ProcessAirdropRequest(
        EntityManager em,
        EntityCommandBuffer ecb,
        ref UnitTransportAirdropRequest request,
        in LocalTransform transportTransform,
        DynamicBuffer<UnitTransportPassengerElement> passengers,
        Entity transport,
        float now,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied)
    {
        if (passengers.Length <= 0 || request.DroppedCount >= request.DropCount)
        {
            FinishAirdropRequest(em, ecb, transport);
            return;
        }

        if (request.PassReady == 0)
            return;

        if (request.NextDropAt > 0f && now < request.NextDropAt)
            return;

        int passengerIndex = FindExistingPassenger(em, passengers);
        if (passengerIndex < 0)
        {
            passengers.Clear();
            FinishAirdropRequest(em, ecb, transport);
            return;
        }

        Entity passenger = passengers[passengerIndex].Passenger;
        byte passengerKind = ResolveLoadedPassengerKind(em, transport, passenger);
        if (!TryResolveDropVisualPrefab(em, ecb, transport, passengerKind, out Entity visualPrefab))
            throw new InvalidOperationException(CreateMissingAirdropVisualPrefabMessage(em, transport, passengerKind));

        int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
            ? em.GetComponentData<UnitFootprint>(passenger).Size
            : new int2(1, 1);
        if (!TryFindLandingCell(
                grid,
                walkable,
                blocked,
                occupied,
                request.DropReferenceCell,
                passengerFootprint,
                request.DroppedCount + passenger.Index,
                out int2 landingCell))
        {
            throw new InvalidOperationException(CreateNoAirdropLandingCellMessage(em, transport, request.DropReferenceCell, passenger, passengerFootprint));
        }

        passengers.RemoveAt(passengerIndex);
        float3 startPosition = ResolveAirdropStartPosition(em, transport, transportTransform);
        float3 endPosition = GridUtils.CellToWorldCenter(grid, landingCell);
        _spawnGroundingSystem.TryGroundCellCenter(em, grid, landingCell, ref endPosition, out _);
        if (startPosition.y < endPosition.y + MinimumDropHeight)
            startPosition.y = endPosition.y + MinimumDropHeight;

        RestorePassengerForDrop(em, ecb, passenger, landingCell, startPosition);
        Entity visualEntity = SpawnDropVisual(em, ecb, visualPrefab, passengerKind, startPosition);
        if (passengerKind == UnitTransportPassengerKind.Vehicle)
        {
            SetOrAdd(em, ecb, passenger, new UnitTransportCargoDropComponent
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                LandingCell = landingCell,
                StartedAt = now,
                DurationSeconds = VehicleDropDurationSeconds,
                VisualEntity = visualEntity
            });
            request.NextDropAt = now + math.max(VehicleDropIntervalSeconds, request.DropIntervalSeconds);
        }
        else
        {
            SetOrAdd(em, ecb, passenger, new UnitTransportParachuteDropComponent
            {
                StartPosition = startPosition,
                EndPosition = endPosition,
                LandingCell = landingCell,
                StartedAt = now,
                DurationSeconds = SoldierDropDurationSeconds,
                VisualEntity = visualEntity
            });
            request.NextDropAt = now + math.max(SoldierDropIntervalSeconds, request.DropIntervalSeconds);
        }

        request.DroppedCount++;
        if (request.DroppedCount >= request.DropCount || passengers.Length <= 0)
            FinishAirdropRequest(em, ecb, transport);
    }

    private static void FinishAirdropRequest(EntityManager em, EntityCommandBuffer ecb, Entity transport)
    {
        if (em.HasComponent<UnitAirComponent>(transport))
        {
            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
            airState.ReturningHome = (byte)(airState.Airborne != 0 ? 1 : airState.ReturningHome);
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            ecb.SetComponent(transport, airState);
        }

        ecb.RemoveComponent<UnitTransportAirdropRequest>(transport);
        if (em.HasComponent<UnitTransportPlaneDoorState>(transport))
        {
            SetOrAdd(
                em,
                ecb,
                transport,
                new UnitTransportPlaneDoorOpenRequest { RemainingSeconds = DoorCloseDelayAfterFinalDropSeconds });
        }
        else
        {
            RemoveIfPresent<UnitTransportPlaneDoorOpenRequest>(em, ecb, transport);
        }
    }

    private static void UpdateParachuteDrop(
        EntityManager em,
        EntityCommandBuffer ecb,
        RefRW<LocalTransform> transform,
        RefRO<UnitTransportParachuteDropComponent> drop,
        Entity entity,
        ComponentLookup<LocalTransform> transformLookup,
        float now,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied)
    {
        float duration = math.max(0.01f, drop.ValueRO.DurationSeconds);
        float t = math.saturate((now - drop.ValueRO.StartedAt) / duration);
        transform.ValueRW.Position = math.lerp(drop.ValueRO.StartPosition, drop.ValueRO.EndPosition, SmoothStep(t));
        UpdateVisualPosition(transformLookup, drop.ValueRO.VisualEntity, transform.ValueRO.Position + new float3(0f, ParachuteVisualHeight, 0f));
        if (t >= 1f)
        {
            transform.ValueRW.Position = drop.ValueRO.EndPosition;
            FinishDrop(
                em,
                ecb,
                entity,
                drop.ValueRO.LandingCell,
                drop.ValueRO.VisualEntity,
                now,
                drop.ValueRO.EndPosition,
                grid,
                walkable,
                blocked,
                occupied,
                UnitTransportPassengerKind.Soldier);
        }
    }

    private static void UpdateCargoDrop(
        EntityManager em,
        EntityCommandBuffer ecb,
        RefRW<LocalTransform> transform,
        RefRO<UnitTransportCargoDropComponent> drop,
        Entity entity,
        ComponentLookup<LocalTransform> transformLookup,
        float now,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied)
    {
        float duration = math.max(0.01f, drop.ValueRO.DurationSeconds);
        float t = math.saturate((now - drop.ValueRO.StartedAt) / duration);
        transform.ValueRW.Position = math.lerp(drop.ValueRO.StartPosition, drop.ValueRO.EndPosition, SmoothStep(t));
        UpdateVisualPosition(transformLookup, drop.ValueRO.VisualEntity, transform.ValueRO.Position + new float3(0f, CargoVisualHeight, 0f));
        if (t >= 1f)
        {
            transform.ValueRW.Position = drop.ValueRO.EndPosition;
            FinishDrop(
                em,
                ecb,
                entity,
                drop.ValueRO.LandingCell,
                drop.ValueRO.VisualEntity,
                now,
                drop.ValueRO.EndPosition,
                grid,
                walkable,
                blocked,
                occupied,
                UnitTransportPassengerKind.Vehicle);
        }
    }

    private static void FinishDrop(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity passenger,
        int2 landingCell,
        Entity visual,
        float now,
        float3 finalPosition,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        byte passengerKind)
    {
        if (em.HasComponent<UnitGrid>(passenger))
            ecb.SetComponent(passenger, new UnitGrid { Cell = landingCell });
        if (em.HasComponent<UnitTransportParachuteDropComponent>(passenger))
            ecb.RemoveComponent<UnitTransportParachuteDropComponent>(passenger);
        if (em.HasComponent<UnitTransportCargoDropComponent>(passenger))
            ecb.RemoveComponent<UnitTransportCargoDropComponent>(passenger);
        if (visual != Entity.Null && em.Exists(visual))
            SetOrAdd(em, ecb, visual, new UnitTransportAirdropVisualCleanup { DestroyAt = now + VisualCleanupDelaySeconds });

        bool settling = TryStartSettleMove(
            em,
            ecb,
            passenger,
            landingCell,
            finalPosition,
            now,
            grid,
            walkable,
            blocked,
            occupied,
            passengerKind);
        if (em.HasComponent<UnitMoveVisualComponent>(passenger))
            ecb.SetComponent(passenger, new UnitMoveVisualComponent { IsMoving = (byte)(settling ? 1 : 0), StillSeconds = 0f });
    }

    private static void UpdateSettleMove(
        EntityManager em,
        EntityCommandBuffer ecb,
        RefRW<LocalTransform> transform,
        RefRO<UnitTransportAirdropSettleComponent> settle,
        Entity entity,
        float now)
    {
        float duration = math.max(0.01f, settle.ValueRO.DurationSeconds);
        float t = math.saturate((now - settle.ValueRO.StartedAt) / duration);
        transform.ValueRW.Position = math.lerp(settle.ValueRO.StartPosition, settle.ValueRO.EndPosition, SmoothStep(t));
        if (t < 1f)
            return;

        transform.ValueRW.Position = settle.ValueRO.EndPosition;
        if (em.HasComponent<UnitGrid>(entity))
            ecb.SetComponent(entity, new UnitGrid { Cell = settle.ValueRO.EndCell });
        if (em.HasComponent<UnitMoveVisualComponent>(entity))
            ecb.SetComponent(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });
        ecb.RemoveComponent<UnitTransportAirdropSettleComponent>(entity);
    }

    private static bool TryStartSettleMove(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity entity,
        int2 landingCell,
        float3 startPosition,
        float now,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        byte passengerKind)
    {
        if (!em.HasComponent<UnitFootprint>(entity))
            return false;

        int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
        int radius = passengerKind == UnitTransportPassengerKind.Vehicle ? VehicleSettleSearchRadius : SoldierSettleSearchRadius;
        if (!TryFindSettleCell(grid, walkable, blocked, occupied, landingCell, footprint, entity.Index, radius, out int2 settleCell))
            return false;

        float3 endPosition = GridUtils.CellToWorldCenter(grid, settleCell);
        endPosition.y = startPosition.y;
        float speed = em.HasComponent<UnitMove>(entity) ? math.max(0.1f, em.GetComponentData<UnitMove>(entity).Speed) : 2f;
        float minSeconds = passengerKind == UnitTransportPassengerKind.Vehicle ? VehicleSettleMinSeconds : SoldierSettleMinSeconds;
        float duration = math.max(minSeconds, math.distance(startPosition, endPosition) / speed);
        SetOrAdd(em, ecb, entity, new UnitTransportAirdropSettleComponent
        {
            StartPosition = startPosition,
            EndPosition = endPosition,
            EndCell = settleCell,
            StartedAt = now,
            DurationSeconds = duration
        });
        return true;
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

    private static byte ResolveLoadedPassengerKind(EntityManager em, Entity transport, Entity passenger)
    {
        if (!em.Exists(passenger) ||
            !em.HasComponent<UnitTransportCargoPassenger>(passenger))
        {
            return UnitTransportPassengerKind.Soldier;
        }

        UnitTransportCargoPassenger cargoPassenger = em.GetComponentData<UnitTransportCargoPassenger>(passenger);
        return cargoPassenger.Transport == transport && cargoPassenger.PassengerKind == UnitTransportPassengerKind.Vehicle
            ? UnitTransportPassengerKind.Vehicle
            : UnitTransportPassengerKind.Soldier;
    }

    internal static bool TryFindLandingCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 referenceCell,
        int2 footprint,
        int ordinalSeed,
        out int2 landingCell)
    {
        landingCell = default;
        int ordinal = math.abs(ordinalSeed);
        for (int radius = 0; radius <= LandingSearchRadius; radius++)
        {
            int validCount = CountValidLandingCells(grid, walkable, blocked, occupied, referenceCell, footprint, radius);
            if (validCount <= 0)
                continue;

            int desiredOrdinal = ordinal % validCount;
            if (TryGetValidLandingCellAtOrdinal(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    referenceCell,
                    footprint,
                    radius,
                    desiredOrdinal,
                    out landingCell))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountValidLandingCells(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 referenceCell,
        int2 footprint,
        int radius)
    {
        int count = 0;
        int minX = referenceCell.x - radius;
        int minY = referenceCell.y - radius;
        int maxX = referenceCell.x + radius;
        int maxY = referenceCell.y + radius;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (radius > 0 && x != minX && x != maxX && y != minY && y != maxY)
                    continue;

                if (IsValidLandingCell(grid, walkable, blocked, occupied, new int2(x, y), footprint))
                    count++;
            }
        }

        return count;
    }

    private static bool TryGetValidLandingCellAtOrdinal(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 referenceCell,
        int2 footprint,
        int radius,
        int desiredOrdinal,
        out int2 landingCell)
    {
        landingCell = default;
        int ordinal = 0;
        int minX = referenceCell.x - radius;
        int minY = referenceCell.y - radius;
        int maxX = referenceCell.x + radius;
        int maxY = referenceCell.y + radius;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (radius > 0 && x != minX && x != maxX && y != minY && y != maxY)
                    continue;

                int2 candidate = new int2(x, y);
                if (!IsValidLandingCell(grid, walkable, blocked, occupied, candidate, footprint))
                    continue;

                if (ordinal == desiredOrdinal)
                {
                    landingCell = candidate;
                    return true;
                }

                ordinal++;
            }
        }

        return false;
    }

    private static bool IsValidLandingCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 centerCell,
        int2 footprint)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprint);
        int2 min = UnitFootprintUtility.GetMinCell(centerCell, size);
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

        return true;
    }

    private static bool TryFindSettleCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 landingCell,
        int2 footprint,
        int ordinalSeed,
        int searchRadius,
        out int2 settleCell)
    {
        settleCell = default;
        int ordinal = math.abs(ordinalSeed);
        for (int radius = 1; radius <= searchRadius; radius++)
        {
            int validCount = CountValidSettleCells(
                grid,
                walkable,
                blocked,
                occupied,
                landingCell,
                footprint,
                radius);
            if (validCount <= 0)
                continue;

            int desiredOrdinal = ordinal % validCount;
            if (TryGetValidSettleCellAtOrdinal(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    landingCell,
                    footprint,
                    radius,
                    desiredOrdinal,
                    out settleCell))
                return true;
        }

        return false;
    }

    private static int CountValidSettleCells(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 landingCell,
        int2 footprint,
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

                int2 candidate = new int2(x, y);
                if (!CellsEqual(candidate, landingCell) &&
                    IsValidLandingCell(grid, walkable, blocked, occupied, candidate, footprint))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool TryGetValidSettleCellAtOrdinal(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        int2 landingCell,
        int2 footprint,
        int radius,
        int desiredOrdinal,
        out int2 settleCell)
    {
        settleCell = default;
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
                if (CellsEqual(candidate, landingCell) ||
                    !IsValidLandingCell(grid, walkable, blocked, occupied, candidate, footprint))
                {
                    continue;
                }

                if (ordinal == desiredOrdinal)
                {
                    settleCell = candidate;
                    return true;
                }

                ordinal++;
            }
        }

        return false;
    }

    private static float3 ResolveAirdropStartPosition(EntityManager em, Entity transport, LocalTransform transportTransform)
    {
        float3 localAnchor = new(0f, -0.5f, -4f);
        if (em.HasComponent<UnitTransportPlaneDoorReference>(transport))
            localAnchor = em.GetComponentData<UnitTransportPlaneDoorReference>(transport).DoorLocalPosition;

        return transportTransform.Position + math.rotate(transportTransform.Rotation, localAnchor);
    }

    private static bool TryResolveDropVisualPrefab(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity transport,
        byte passengerKind,
        out Entity visualPrefab)
    {
        visualPrefab = Entity.Null;
        if (!TryResolveAirdropVisualPrefabs(em, ecb, transport, passengerKind, out UnitTransportAirdropVisualPrefabs prefabs))
            return false;

        visualPrefab = passengerKind == UnitTransportPassengerKind.Vehicle
            ? prefabs.VehicleEmergencyDropVisualPrefab
            : prefabs.SoldierParachuteVisualPrefab;
        return visualPrefab != Entity.Null && em.Exists(visualPrefab);
    }

    internal static bool HasResolvableDropVisualPrefab(
        EntityManager em,
        Entity transport,
        byte passengerKind)
    {
        if (!em.Exists(transport))
            return false;

        if (em.HasComponent<UnitTransportAirdropVisualPrefabs>(transport))
        {
            UnitTransportAirdropVisualPrefabs prefabs = em.GetComponentData<UnitTransportAirdropVisualPrefabs>(transport);
            if (HasVisualPrefabForKind(em, prefabs, passengerKind))
                return true;
        }

        return TryFindSourceAirdropVisualPrefabs(em, transport, passengerKind, out _);
    }

    internal static string CreateMissingAirdropVisualPrefabMessage(
        EntityManager em,
        Entity transport,
        byte passengerKind)
    {
        string transportName = transport == Entity.Null || !em.Exists(transport)
            ? "missing-transport"
            : ResolveEntityDebugName(em, transport);
        string visualKind = passengerKind == UnitTransportPassengerKind.Vehicle
            ? nameof(UnitTransportAirdropVisualPrefabs.VehicleEmergencyDropVisualPrefab)
            : nameof(UnitTransportAirdropVisualPrefabs.SoldierParachuteVisualPrefab);
        return $"Transport plane airdrop requires baked ECS visual prefab '{visualKind}' for {transportName}. " +
               "Assign the parachute/emergency-drop source prefab in UnitGridAuthoringPrefabConfigAsset and rebake the unit prefab/subscene.";
    }

    internal static string CreateNoAirdropLandingCellMessage(
        EntityManager em,
        Entity transport,
        int2 dropReferenceCell,
        Entity passenger,
        int2 passengerFootprint)
    {
        string transportName = transport == Entity.Null || !em.Exists(transport)
            ? "missing-transport"
            : ResolveEntityDebugName(em, transport);
        string passengerName = passenger == Entity.Null || !em.Exists(passenger)
            ? "missing-passenger"
            : ResolveEntityDebugName(em, passenger);
        return $"No clear airdrop landing zone for {transportName} passenger={passengerName} dropCell={dropReferenceCell} footprint={passengerFootprint}.";
    }

    private static string ResolveEntityDebugName(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(sourceKey))
                return $"{sourceKey} {entity}";
        }

        string entityName = em.GetName(entity);
        return string.IsNullOrWhiteSpace(entityName) ? entity.ToString() : $"{entityName} {entity}";
    }

    private static bool TryResolveAirdropVisualPrefabs(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity transport,
        byte passengerKind,
        out UnitTransportAirdropVisualPrefabs prefabs)
    {
        prefabs = default;
        if (!em.Exists(transport))
            return false;

        if (em.HasComponent<UnitTransportAirdropVisualPrefabs>(transport))
        {
            prefabs = em.GetComponentData<UnitTransportAirdropVisualPrefabs>(transport);
            if (HasVisualPrefabForKind(em, prefabs, passengerKind))
                return true;
        }

        if (!TryFindSourceAirdropVisualPrefabs(em, transport, passengerKind, out prefabs))
            return false;

        if (em.HasComponent<UnitTransportAirdropVisualPrefabs>(transport))
            ecb.SetComponent(transport, prefabs);
        else
            ecb.AddComponent(transport, prefabs);

        return true;
    }

    private static bool TryFindSourceAirdropVisualPrefabs(
        EntityManager em,
        Entity transport,
        byte passengerKind,
        out UnitTransportAirdropVisualPrefabs prefabs)
    {
        prefabs = default;
        if (!em.Exists(transport) || !em.HasComponent<UnitSourcePrefabKey>(transport))
            return false;

        UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(transport);
        if (TryFindRegistryAirdropVisualPrefabs(em, sourceKey.Value, passengerKind, out prefabs))
            return true;

        using EntityQuery query = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<UnitTransportAirdropVisualPrefabs>()
            },
            Options = EntityQueryOptions.IncludePrefab
        });
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> candidates = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < candidates.Length; i++)
            {
                Entity candidate = candidates[i];
                if (candidate == transport)
                    continue;

                UnitSourcePrefabKey candidateKey = em.GetComponentData<UnitSourcePrefabKey>(candidate);
                if (!candidateKey.Value.Equals(sourceKey.Value))
                    continue;

                UnitTransportAirdropVisualPrefabs candidatePrefabs =
                    em.GetComponentData<UnitTransportAirdropVisualPrefabs>(candidate);
                if (!HasVisualPrefabForKind(em, candidatePrefabs, passengerKind))
                    continue;

                prefabs = candidatePrefabs;
                return true;
            }
        }

        return false;
    }

    private static bool TryFindRegistryAirdropVisualPrefabs(
        EntityManager em,
        FixedString64Bytes sourceKey,
        byte passengerKind,
        out UnitTransportAirdropVisualPrefabs prefabs)
    {
        prefabs = default;
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportAirdropVisualPrefabRegistryEntry>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> registryEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int registryIndex = 0; registryIndex < registryEntities.Length; registryIndex++)
            {
                DynamicBuffer<UnitTransportAirdropVisualPrefabRegistryEntry> registry =
                    em.GetBuffer<UnitTransportAirdropVisualPrefabRegistryEntry>(registryEntities[registryIndex]);
                for (int i = 0; i < registry.Length; i++)
                {
                    UnitTransportAirdropVisualPrefabRegistryEntry entry = registry[i];
                    if (!entry.SourceKey.Equals(sourceKey))
                        continue;

                    UnitTransportAirdropVisualPrefabs candidate = new()
                    {
                        SoldierParachuteVisualPrefab = entry.SoldierParachuteVisualPrefab,
                        VehicleEmergencyDropVisualPrefab = entry.VehicleEmergencyDropVisualPrefab
                    };
                    if (!HasVisualPrefabForKind(em, candidate, passengerKind))
                        continue;

                    prefabs = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasVisualPrefabForKind(
        EntityManager em,
        in UnitTransportAirdropVisualPrefabs prefabs,
        byte passengerKind)
    {
        Entity prefab = passengerKind == UnitTransportPassengerKind.Vehicle
            ? prefabs.VehicleEmergencyDropVisualPrefab
            : prefabs.SoldierParachuteVisualPrefab;
        return prefab != Entity.Null && em.Exists(prefab);
    }

    private static Entity SpawnDropVisual(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity visualPrefab,
        byte passengerKind,
        float3 startPosition)
    {
        Entity visual = ecb.Instantiate(visualPrefab);
        LocalTransform visualTransform = ResolveDropVisualTransform(em, visualPrefab, passengerKind, startPosition);
        if (em.HasComponent<LocalTransform>(visualPrefab))
            ecb.SetComponent(visual, visualTransform);
        else
            ecb.AddComponent(visual, visualTransform);
        ecb.AddComponent(visual, new UnitTransportAirdropVisualCleanup { DestroyAt = 0f });
        return visual;
    }

    private static LocalTransform ResolveDropVisualTransform(
        EntityManager em,
        Entity prefab,
        byte passengerKind,
        float3 startPosition)
    {
        LocalTransform prefabTransform = em.HasComponent<LocalTransform>(prefab)
            ? em.GetComponentData<LocalTransform>(prefab)
            : LocalTransform.Identity;
        float visualHeight = passengerKind == UnitTransportPassengerKind.Vehicle
            ? CargoVisualHeight
            : ParachuteVisualHeight;
        float visualScale = passengerKind == UnitTransportPassengerKind.Vehicle
            ? CargoVisualScale
            : ParachuteVisualScale;

        prefabTransform.Position = startPosition + new float3(0f, visualHeight, 0f);
        prefabTransform.Scale = math.max(0.01f, prefabTransform.Scale) * visualScale;
        return prefabTransform;
    }

    private static void RestorePassengerForDrop(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity passenger,
        int2 landingCell,
        float3 startPosition)
    {
        RemoveIfPresent<Disabled>(em, ecb, passenger);
        RemoveIfPresent<UnitTransportPassenger>(em, ecb, passenger);
        RemoveIfPresent<UnitTransportCargoPassenger>(em, ecb, passenger);
        UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, passenger);
        RestoreHiddenVisuals(em, ecb, passenger);

        if (em.HasComponent<UnitGrid>(passenger))
            ecb.SetComponent(passenger, new UnitGrid { Cell = landingCell });
        if (em.HasComponent<LocalTransform>(passenger))
            ecb.SetComponent(passenger, LocalTransform.FromPosition(startPosition));
        if (em.HasComponent<UnitMoveVisualComponent>(passenger))
            ecb.SetComponent(passenger, new UnitMoveVisualComponent { IsMoving = 1, StillSeconds = 0f });
    }

    private static void RestoreHiddenVisuals(EntityManager em, EntityCommandBuffer ecb, Entity passenger)
    {
        if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
            return;

        DynamicBuffer<UnitTransportHiddenVisualScale> hiddenVisuals = em.GetBuffer<UnitTransportHiddenVisualScale>(passenger);
        for (int i = 0; i < hiddenVisuals.Length; i++)
        {
            UnitTransportHiddenVisualScale hidden = hiddenVisuals[i];
            if (!em.Exists(hidden.Visual))
                continue;

            if (em.HasComponent<LocalTransform>(hidden.Visual))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(hidden.Visual);
                transform.Scale = hidden.PreviousScale;
                ecb.SetComponent(hidden.Visual, transform);
            }

            if (hidden.Visual != passenger && hidden.WasDisabled == 0 && em.HasComponent<Disabled>(hidden.Visual))
                ecb.RemoveComponent<Disabled>(hidden.Visual);
        }

        hiddenVisuals.Clear();
    }

    private static void UpdateVisualPosition(ComponentLookup<LocalTransform> transformLookup, Entity visual, float3 position)
    {
        if (visual == Entity.Null || !transformLookup.HasComponent(visual))
            return;

        LocalTransform visualTransform = transformLookup[visual];
        visualTransform.Position = position;
        transformLookup[visual] = visualTransform;
    }

    private static float SmoothStep(float t) => t * t * (3f - (2f * t));

    private static bool CellsEqual(int2 a, int2 b) => a.x == b.x && a.y == b.y;

    private static void SetOrAdd<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity, T component)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.SetComponent(entity, component);
        else
            ecb.AddComponent(entity, component);
    }

    private static void RemoveIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }
}
