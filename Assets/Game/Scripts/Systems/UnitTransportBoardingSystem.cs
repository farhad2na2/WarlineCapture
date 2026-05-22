using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportBoardingSystem : ISystem
{
    private const int BoardingClearanceCells = 4;
    private const int AirBoardingClearanceCells = 1;
    private const float AirBoardingGroundedHeightTolerance = 3f;
    private const int DiagnosticLogIntervalFrames = 180;
    private static readonly RuntimeDiagnosticsSystem RuntimeDiagnostics = new();

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitTransportBoardingTarget>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        EntityManager em = state.EntityManager;

        foreach (var (boarding, passengerGrid, passengerTransform, entity) in
                 SystemAPI.Query<RefRO<UnitTransportBoardingTarget>, RefRO<UnitGrid>, RefRO<LocalTransform>>()
                     .WithNone<Disabled>()
                     .WithEntityAccess())
        {
            Entity transport = boarding.ValueRO.Transport;
            if (!em.Exists(transport) ||
                !em.HasComponent<UnitTransportCapacity>(transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                LogDiagnostic($"result=Cancel reason=TransportMissingOrInvalid passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)}");
                ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
                continue;
            }

            if (!IsTransportLandedForBoarding(em, transport))
            {
                LogPeriodic(entity, $"result=Waiting reason=TransportNotLanded passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} {DescribeAirState(em, transport)}");
                continue;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
            if (passengers.Length >= capacity)
            {
                LogDiagnostic($"result=Cancel reason=NoSeats passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} seats={passengers.Length}/{capacity}");
                ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
                continue;
            }

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 passengerCell = passengerGrid.ValueRO.Cell;
            int2 boardingGoal = boarding.ValueRO.Goal;
            float3 transportPosition = em.GetComponentData<LocalTransform>(transport).Position;
            float3 passengerPosition = passengerTransform.ValueRO.Position;
            passengerPosition.y = transportPosition.y;
            int boardingClearance = em.HasComponent<UnitAirMovement>(transport)
                ? AirBoardingClearanceCells
                : BoardingClearanceCells;

            bool movementFinished =
                !em.HasComponent<UnitTarget>(entity) &&
                !em.HasComponent<UnitPathRequest>(entity) &&
                !em.HasComponent<UnitPathFollow>(entity);
            bool airTransport = em.HasComponent<UnitAirMovement>(transport);
            int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
            bool reachedBoardingGoal = passengerCell.Equals(boardingGoal);
            int distanceToBoardingGoal = math.max(math.abs(passengerCell.x - boardingGoal.x), math.abs(passengerCell.y - boardingGoal.y));
            bool settledNearBoardingGoal = movementFinished && distanceToBoardingGoal <= (airTransport ? 0 : boardingClearance);
            bool nearTransportFootprint = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, passengerCell, boardingClearance);
            bool boardingGoalNearTransport = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, boardingGoal, boardingClearance);
            float boardDistanceSq = airTransport ? 1.25f * 1.25f : 4f;
            int boardCellDistance = airTransport ? 1 : 2;
            bool reachedTransport =
                nearTransportFootprint ||
                (boardingGoalNearTransport && (reachedBoardingGoal || settledNearBoardingGoal)) ||
                math.distancesq(passengerPosition, transportPosition) <= boardDistanceSq ||
                math.max(math.abs(passengerCell.x - transportCell.x), math.abs(passengerCell.y - transportCell.y)) <= boardCellDistance;
            if (!reachedTransport)
            {
                LogPeriodic(
                    entity,
                    $"result=Waiting reason=NotReached passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} " +
                    $"passengerCell={passengerCell} goal={boardingGoal} transportCell={transportCell} transportSize={transportSize} " +
                    $"distGoal={distanceToBoardingGoal} clearance={boardingClearance} movementFinished={(movementFinished ? 1 : 0)} " +
                    $"hasTarget={(em.HasComponent<UnitTarget>(entity) ? 1 : 0)} hasRequest={(em.HasComponent<UnitPathRequest>(entity) ? 1 : 0)} hasFollow={(em.HasComponent<UnitPathFollow>(entity) ? 1 : 0)} " +
                    $"reachedGoal={(reachedBoardingGoal ? 1 : 0)} settledNearGoal={(settledNearBoardingGoal ? 1 : 0)} nearTransport={(nearTransportFootprint ? 1 : 0)} seats={passengers.Length}/{capacity}");
                continue;
            }

            passengers.Add(new UnitTransportPassengerElement { Passenger = entity });
            LogDiagnostic($"result=Boarded passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} seats={passengers.Length}/{capacity}");
            UnitTransportVisualUtility.SetPassengerHidden(em, entity, ecb);
            ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
            RemoveIfPresent<UnitTarget>(ref ecb, em, entity);
            RemoveIfPresent<UnitPathRequest>(ref ecb, em, entity);
            RemoveIfPresent<UnitPathFollow>(ref ecb, em, entity);
            RemoveIfPresent<UnitPathRange>(ref ecb, em, entity);
            RemoveIfPresent<ManualMoveOrderTag>(ref ecb, em, entity);
            RemoveIfPresent<AutoWanderMoveTag>(ref ecb, em, entity);
            RemoveIfPresent<EngageTarget>(ref ecb, em, entity);
            RemoveIfPresent<SelectedUnitTag>(ref ecb, em, entity);
            ecb.AddComponent(entity, new UnitTransportPassenger { Transport = transport });
            ecb.AddComponent<Disabled>(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static void RemoveIfPresent<T>(ref EntityCommandBuffer ecb, EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    public int GetTransportBoardingClickPaddingCells(EntityManager em, Entity transport, int2 footprint)
    {
        int footprintMax = math.max(footprint.x, footprint.y);
        if (em.Exists(transport) && em.HasComponent<UnitAirMovement>(transport))
            return math.max(24, footprintMax + 24);

        return math.max(6, footprintMax + 4);
    }

    public bool IsBoardablePlayerTransport(EntityManager em, Entity transport)
    {
        return em.Exists(transport) &&
               TryEnsureTransportCapacity(em, transport) &&
               em.HasComponent<Faction>(transport) &&
               em.GetComponentData<Faction>(transport).Id == 0 &&
               em.HasComponent<UnitGrid>(transport) &&
               em.HasComponent<UnitFootprint>(transport) &&
               em.HasComponent<LocalTransform>(transport);
    }

    public bool IsSoldierBoardingCandidate(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<Faction>(entity) ||
            em.GetComponentData<Faction>(entity).Id != 0 ||
            !em.HasComponent<UnitGrid>(entity) ||
            !em.HasComponent<UnitMove>(entity) ||
            !em.HasComponent<UnitFootprint>(entity) ||
            !em.HasComponent<UnitMovementBehavior>(entity) ||
            em.HasComponent<UnitAirMovement>(entity) ||
            em.HasComponent<UnitTransportPassenger>(entity))
        {
            return false;
        }

        string sourceName = ResolveSourceName(em, entity);
        if (sourceName.IndexOf("_Chr_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            sourceName.StartsWith("Unit_Chr", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (sourceName.IndexOf("_Veh_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            sourceName.StartsWith("Unit_Veh", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !UnitVehicleMovementUtility.IsVehicle(
            em.GetComponentData<UnitFootprint>(entity),
            em.GetComponentData<UnitMovementBehavior>(entity));
    }

    public bool TryEnsureTransportCapacity(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport))
            return false;

        int capacity = 0;
        if (em.HasComponent<UnitTransportCapacity>(transport))
            capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);

        if (capacity <= 0)
            capacity = ResolveTransportCapacity(em, transport);
        if (capacity <= 0)
            return false;

        if (em.HasComponent<UnitTransportCapacity>(transport))
            em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });
        else
            em.AddComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });

        if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
            em.AddBuffer<UnitTransportPassengerElement>(transport);

        return true;
    }

    public int ResolveTransportCapacity(EntityManager em, Entity entity)
    {
        string sourceName = ResolveSourceName(em, entity);
        return IsPersonnelTransportName(sourceName) ? 10 : 0;
    }

    public bool IsPersonnelTransportName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return false;

        return sourceName.IndexOf("Unit_Veh_APC_Fast", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_Heavy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_Slow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_01", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_02", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_Truck_Canopy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
    {
        if (!em.HasComponent<UnitAirMovement>(transport))
            return true;

        if (!em.HasComponent<UnitAirState>(transport) || !em.HasComponent<LocalTransform>(transport))
            return false;

        UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
        bool physicallyGrounded = transform.Position.y <= groundY + AirBoardingGroundedHeightTolerance;
        return airState.Airborne == 0 &&
               airState.TakeoffRolling == 0 &&
               airState.LandingRolling == 0 &&
               physicallyGrounded &&
               !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
    }

    public int GetTransportBoardingDirectCells(EntityManager em, Entity transport)
    {
        return em.HasComponent<UnitAirMovement>(transport)
            ? AirBoardingClearanceCells
            : BoardingClearanceCells;
    }

    public bool IsRopeDisembarkTransport(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
            return false;

        string sourceName = ResolveSourceName(em, transport);
        return sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool TryPrepareAirTransportPickupForBoarding(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        List<Entity> selectedPassengers,
        int selectedCount,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        UnitMoveOrderSystem moveOrderSystem,
        out int2 pickupCell)
    {
        if (!TryFindAirTransportPickupForBoarding(
                em,
                transport,
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                selectedPassengers,
                selectedCount,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                out pickupCell))
        {
            return false;
        }

        CommandAirTransportPickup(em, transport, grid, pickupCell, moveOrderSystem);
        return true;
    }

    public bool TryFindAirTransportPickupForBoarding(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        List<Entity> selectedPassengers,
        int selectedCount,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        out int2 pickupCell)
    {
        pickupCell = default;
        if (!em.Exists(transport) ||
            !em.HasComponent<UnitAirMovement>(transport) ||
            !em.HasComponent<UnitAirState>(transport) ||
            !em.HasComponent<LocalTransform>(transport))
        {
            return false;
        }

        byte factionId = em.HasComponent<Faction>(transport) ? em.GetComponentData<Faction>(transport).Id : (byte)0;
        int count = math.min(selectedCount, selectedPassengers.Count);
        for (int i = 0; i < count; i++)
        {
            Entity passenger = selectedPassengers[i];
            if (!IsSoldierBoardingCandidate(em, passenger) || !em.HasComponent<UnitGrid>(passenger))
                continue;

            int2 passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            if (!TryFindAirTransportPickupCellNearPassenger(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    transportSize,
                    passengerCell,
                    transport,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    factionId,
                    out pickupCell))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void CommandAirTransportPickup(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        int2 pickupCell,
        UnitMoveOrderSystem moveOrderSystem)
    {
        moveOrderSystem.ClearMovementOrderComponents(em, transport);

        UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : grid.Origin.y;
        float3 pickupPosition = GridUtils.CellToWorldCenter(grid, pickupCell);
        pickupPosition.y = groundY;

        airState.HomePosition = pickupPosition;
        airState.HomeCell = pickupCell;
        airState.HomeInitialized = 1;
        airState.ReturningHome = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        if (transform.Position.y > groundY + AirBoardingGroundedHeightTolerance)
            airState.Airborne = 1;
        em.SetComponentData(transport, airState);

        moveOrderSystem.IssueTargetOnlyMoveCommand(em, transport, pickupCell);
    }

    public bool StartRopeDisembarkTransport(
        EntityManager em,
        Entity transport,
        int2 referenceCell,
        UnitMoveOrderSystem moveOrderSystem)
    {
        if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
            return false;

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        if (passengers.Length <= 0)
            return false;

        moveOrderSystem.ClearMovementOrderComponents(em, transport);
        if (em.HasComponent<UnitAirMovement>(transport) &&
            em.HasComponent<UnitAirState>(transport) &&
            em.HasComponent<LocalTransform>(transport))
        {
            UnitAirMovement airMovement = em.GetComponentData<UnitAirMovement>(transport);
            UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            if (airState.Airborne == 0)
            {
                transform.Position.y = groundY + math.max(3f, airMovement.CruiseHeight);
                em.SetComponentData(transport, transform);
            }

            airState.ReturningHome = 0;
            airState.Airborne = 1;
            airState.TakeoffRolling = 0;
            airState.LandingRolling = 0;
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            em.SetComponentData(transport, airState);
        }

        UnitTransportRopeDisembarkRequest request = new()
        {
            ReferenceCell = referenceCell,
            NextDropAt = 0f,
            DropIntervalSeconds = 0.8f
        };

        if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
            em.SetComponentData(transport, request);
        else
            em.AddComponentData(transport, request);

        return true;
    }

    public bool TryFindTransportApproachCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int2 passengerFootprint,
        Entity passenger,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        HashSet<int> reservedCells,
        int directBoardingCells,
        byte factionId,
        out int2 goal)
    {
        return TryFindNearbyTransportApproachCell(
            grid,
            walkable,
            blocked,
            friendlyPassFactionIds,
            occupied,
            transportCell,
            transportSize,
            referenceCell,
            passengerFootprint,
            passenger,
            liveUnitEntities,
            liveUnitGrids,
            liveUnitFootprints,
            ignoredOccupancyEntity,
            ignoredOccupancyCell,
            ignoredOccupancySize,
            reservedCells,
            directBoardingCells,
            factionId,
            out goal);
    }

    public void ReserveFootprintCells(GridConfig grid, int2 cell, int2 footprintSize, HashSet<int> reservedCells)
    {
        if (reservedCells == null)
            return;

        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;
        for (int y = min.y; y < max.y; y++)
        {
            for (int x = min.x; x < max.x; x++)
            {
                int2 reservedCell = new int2(x, y);
                if (GridUtils.InBounds(reservedCell, grid.Width, grid.Height))
                    reservedCells.Add(GridUtils.CellToIndex(reservedCell, grid.Width));
            }
        }
    }

    public bool TryFindTransportDisembarkCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        HashSet<int> reservedCells,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        out int2 goal)
    {
        return TryFindTransportRingCell(
            grid,
            walkable,
            blocked,
            occupied,
            reservedCells,
            transportCell,
            transportSize,
            referenceCell,
            BoardingClearanceCells,
            false,
            out goal);
    }

    private static bool TryFindAirTransportPickupCellNearPassenger(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 passengerCell,
        Entity transport,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        byte factionId,
        out int2 pickupCell)
    {
        pickupCell = default;
        for (int radius = 2; radius <= 10; radius++)
        {
            int bestScore = int.MaxValue;
            bool found = false;
            int minX = passengerCell.x - radius;
            int minY = passengerCell.y - radius;
            int maxX = passengerCell.x + radius;
            int maxY = passengerCell.y + radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (x != minX && x != maxX && y != minY && y != maxY)
                        continue;

                    int2 candidate = new int2(x, y);
                    if (!IsTransportApproachPassable(
                            grid,
                            walkable,
                            blocked,
                            friendlyPassFactionIds,
                            occupied,
                            candidate,
                            transportSize,
                            transportCell,
                            transport,
                            liveUnitEntities,
                            liveUnitGrids,
                            liveUnitFootprints,
                            Entity.Null,
                            default,
                            default,
                            null,
                            candidate,
                            factionId,
                            false))
                    {
                        continue;
                    }

                    int2 delta = candidate - passengerCell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    pickupCell = candidate;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private static bool TryFindNearbyTransportApproachCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int2 passengerFootprint,
        Entity passenger,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        HashSet<int> reservedCells,
        int directBoardingCells,
        byte factionId,
        out int2 goal)
    {
        goal = default;
        if (!GridUtils.InBounds(referenceCell, grid.Width, grid.Height))
            return false;

        int gridSize = grid.Width * grid.Height;
        if (gridSize <= 0 || walkable.Length < gridSize)
            return false;

        int2 size = UnitFootprintUtility.ClampSize(transportSize);
        int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
        int2 max = min + size;
        if (directBoardingCells > BoardingClearanceCells &&
            UnitFootprintUtility.ContainsCellWithPadding(transportCell, size, referenceCell, directBoardingCells))
        {
            goal = referenceCell;
            return true;
        }

        int maxRadius = math.max(1, directBoardingCells);
        int bestScore = int.MaxValue;
        bool found = false;
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int minX = min.x - radius;
            int minY = min.y - radius;
            int maxX = max.x - 1 + radius;
            int maxY = max.y - 1 + radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool onRing = x == minX || x == maxX || y == minY || y == maxY;
                    if (!onRing)
                        continue;

                    int2 candidate = new int2(x, y);
                    if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                        continue;

                    if (!IsTransportApproachPassable(
                            grid,
                            walkable,
                            blocked,
                            friendlyPassFactionIds,
                            occupied,
                            candidate,
                            passengerFootprint,
                            referenceCell,
                            passenger,
                            liveUnitEntities,
                            liveUnitGrids,
                            liveUnitFootprints,
                            ignoredOccupancyEntity,
                            ignoredOccupancyCell,
                            ignoredOccupancySize,
                            reservedCells,
                            referenceCell,
                            factionId,
                            candidate.Equals(referenceCell)))
                    {
                        continue;
                    }

                    int2 delta = candidate - referenceCell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    goal = candidate;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private static bool IsTransportApproachPassable(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 cell,
        int2 footprintSize,
        int2 currentCell,
        Entity movingEntity,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        HashSet<int> reservedCells,
        int2 referenceCell,
        byte factionId,
        bool allowReferenceCellOccupied)
    {
        int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
        int2 max = min + clamped;
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
                if (reservedCells != null && reservedCells.Contains(index))
                    return false;

                if (blocked.IsCreated && blocked.IsSet(index) &&
                    (!friendlyPassFactionIds.IsCreated || (uint)index >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[index] != factionId))
                {
                    return false;
                }

                bool isReferenceCell = x == referenceCell.x && y == referenceCell.y;
                bool isCurrentFootprintCell = UnitFootprintUtility.ContainsCell(currentCell, clamped, new int2(x, y));
                bool isIgnoredOccupancyCell =
                    ignoredOccupancyEntity != Entity.Null &&
                    UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                if (!isCurrentFootprintCell &&
                    occupied.IsCreated &&
                    occupied.IsSet(index) &&
                    (!allowReferenceCellOccupied || !isReferenceCell) &&
                    !isIgnoredOccupancyCell)
                {
                    return false;
                }
            }
        }

        for (int i = 0; i < liveUnitEntities.Length; i++)
        {
            Entity other = liveUnitEntities[i];
            if (other == movingEntity || other == ignoredOccupancyEntity)
                continue;

            int2 otherCell = liveUnitGrids[i].Cell;
            int2 otherSize = liveUnitFootprints[i].Size;
            if (UnitFootprintUtility.Overlaps(cell, clamped, otherCell, otherSize) &&
                !UnitFootprintUtility.Overlaps(currentCell, clamped, otherCell, otherSize))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryFindTransportRingCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeBitArray occupied,
        HashSet<int> reservedCells,
        int2 transportCell,
        int2 transportSize,
        int2 referenceCell,
        int minRadius,
        bool allowReferenceCellOccupied,
        out int2 goal)
    {
        goal = default;
        int2 size = UnitFootprintUtility.ClampSize(transportSize);
        int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
        int2 max = min + size;
        int bestScore = int.MaxValue;
        bool found = false;
        int startRadius = math.max(1, minRadius);
        int maxRadius = math.max(8, math.max(size.x, size.y) + 6);

        for (int radius = startRadius; radius <= maxRadius; radius++)
        {
            int minX = min.x - radius;
            int minY = min.y - radius;
            int maxX = max.x - 1 + radius;
            int maxY = max.y - 1 + radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    bool onRing = x == minX || x == maxX || y == minY || y == maxY;
                    if (!onRing)
                        continue;

                    int2 candidate = new int2(x, y);
                    if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                        continue;

                    int index = GridUtils.CellToIndex(candidate, grid.Width);
                    if (reservedCells != null && reservedCells.Contains(index))
                        continue;
                    if (walkable[index].Value == 0)
                        continue;
                    if (blocked.IsCreated && blocked.IsSet(index))
                        continue;

                    bool isReferenceCell = candidate.Equals(referenceCell);
                    if (occupied.IsCreated && occupied.IsSet(index) && (!allowReferenceCellOccupied || !isReferenceCell))
                        continue;

                    int2 delta = candidate - referenceCell;
                    int score = math.abs(delta.x) + math.abs(delta.y);
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    goal = candidate;
                    found = true;
                }
            }

            if (found)
                return true;
        }

        return false;
    }

    private static void LogDiagnostic(string message)
    {
        if (RuntimeDiagnostics.ShouldLogTransportBoarding)
            Debug.Log($"[TransportBoard] {message}");
    }

    private static void LogPeriodic(Entity entity, string message)
    {
        if (!RuntimeDiagnostics.ShouldLogTransportBoarding)
            return;

        if (Time.frameCount % DiagnosticLogIntervalFrames == 0)
            Debug.Log($"[TransportBoard] {message}");
    }

    private static string DescribeBoardingEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
            return "null";
        if (!em.Exists(entity))
            return $"{entity}:missing";

        string sourceName = ResolveSourceName(em, entity);
        if (string.IsNullOrWhiteSpace(sourceName))
            sourceName = "<unnamed>";

        string cell = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "no-cell";
        string faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id.ToString()
            : "no-faction";
        string health = em.HasComponent<UnitHealth>(entity)
            ? $"{em.GetComponentData<UnitHealth>(entity).Current}/{em.GetComponentData<UnitHealth>(entity).Max}"
            : "no-health";

        return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health}";
    }

    private static string DescribeAirState(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) || !em.HasComponent<UnitAirMovement>(entity))
            return "air=none";
        if (!em.HasComponent<UnitAirState>(entity))
            return "air=missing-state";

        UnitAirState airState = em.GetComponentData<UnitAirState>(entity);
        return $"airborne={airState.Airborne} takeoff={airState.TakeoffRolling} landing={airState.LandingRolling} returning={airState.ReturningHome} rope={(em.HasComponent<UnitTransportRopeDisembarkRequest>(entity) ? 1 : 0)}";
    }

    private static string ResolveSourceName(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return string.Empty;

        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(sourceName))
                return sourceName;
        }

        return em.GetName(entity);
    }
}
