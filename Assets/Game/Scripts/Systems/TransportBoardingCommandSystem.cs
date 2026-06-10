using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class TransportBoardingCommandSystem
{
    public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate bool TryGetClickedCellDelegate(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint);

    public readonly struct Result
    {
        public readonly bool Accepted;
        public readonly int2 MarkerCell;
        public readonly float3 MarkerPosition;
        public readonly byte MarkerFactionId;

        private Result(bool accepted, int2 markerCell, float3 markerPosition, byte markerFactionId)
        {
            Accepted = accepted;
            MarkerCell = markerCell;
            MarkerPosition = markerPosition;
            MarkerFactionId = markerFactionId;
        }

        public static Result Rejected()
        {
            return new Result(false, default, default, 0);
        }

        public static Result AcceptedAt(int2 markerCell, float3 markerPosition, byte markerFactionId)
        {
            return new Result(true, markerCell, markerPosition, markerFactionId);
        }
    }

    private struct PendingTransportBoardingOrder
    {
        public Entity Passenger;
        public int2 PassengerCell;
        public int2 Goal;
        public bool DirectBoarding;
    }

    private World _queryWorld;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _selectedTagQuery;
    private EntityQuery _gridPathingQuery;
    private EntityQuery _allSelectableQuery;
    private EntityQuery _transportBoardingTargetQuery;
    private EntityQuery _pathingLiveUnitsQuery;
    private readonly List<Entity> _selectedBoardingSourceEntities = new();
    private readonly List<Entity> _targetedBoardingSourceEntities = new();

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
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        _gridPathingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        _allSelectableQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<LocalToWorld>());
        _transportBoardingTargetQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitTransportBoardingTarget>());
        _pathingLiveUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            }
        });
    }

    public Result TryIssueBoardTransportOrderToClickedUnit(
        EntityManager em,
        Vector2 screenPosition,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem approachCellSystem,
        UnitTransportAirPickupSystem airPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell)
    {
        EnsureEntityQueries(em);
        if (!TryGetClickedOrNearbyBoardableTransport(
                screenPosition,
                em,
                transportBoardingRuleSystem,
                transportBoardingQuerySystem,
                tryGetClickedUnitEntity,
                tryGetClickedCell,
                out Entity transport))
        {
            return Result.Rejected();
        }

        bool shouldLogTransportBoarding = ShouldQueueTransportBoardingDiagnostics(em);
        if (!transportBoardingQuerySystem.IsBoardablePlayerTransport(em, transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return Result.Rejected();
        }

        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        bool transportLanded = transportBoardingRuleSystem.IsTransportLandedForBoarding(em, transport);
        if (!transportLanded && !airTransport)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportNotLanded transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return Result.Rejected();
        }

        if (!transportLanded && em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportBusyRopeDisembark transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return Result.Rejected();
        }

        int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
        int occupiedSeats = em.GetBuffer<UnitTransportPassengerElement>(transport).Length + CountPendingBoardingOrders(em, transport);
        int availableSeats = capacity - occupiedSeats;
        if (availableSeats <= 0)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoSeats transport={DescribeTransportBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity}");
            return Result.Rejected();
        }

        int selectedCount = CollectSelectedBoardingSourceEntities(em, selectionStateSystem, _selectedBoardingSourceEntities, out int selectedTagCount, out int selectedMoveCount, out bool usedCachedSelection);
        if (selectedCount == 0)
        {
            if (shouldLogTransportBoarding)
            {
                EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=NoSelectedPassengers transport={DescribeTransportBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity} " +
                    $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} cached={selectionStateSystem.CachedSelectedMoveEntities.Count}");
            }

            return Result.Rejected();
        }

        if (_gridPathingQuery.IsEmptyIgnoreFilter)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoGridPathing transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} usedCache={(usedCachedSelection ? 1 : 0)}");
            return Result.Rejected();
        }

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        NativeBitArray blocked = blockerData.Blocked;
        NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
        using NativeArray<Entity> liveUnitEntities = _pathingLiveUnitsQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveUnitGrids = _pathingLiveUnitsQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveUnitFootprints = _pathingLiveUnitsQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        bool hasPendingAirPickupLanding = false;
        int2 pendingAirPickupCell = default;
        if (airTransport && !transportLanded)
        {
            if (!airPickupSystem.TryFindAirTransportPickupForBoarding(
                    em,
                    transport,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    transportSize,
                    _selectedBoardingSourceEntities,
                    selectedCount,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    out pendingAirPickupCell))
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount}");
                return Result.Rejected();
            }

            transportCell = pendingAirPickupCell;
            hasPendingAirPickupLanding = true;
        }

        List<PendingTransportBoardingOrder> boardingOrders = new();
        HashSet<int> reservedBoardingCells = new();
        for (int i = 0; i < selectedCount && boardingOrders.Count < availableSeats; i++)
        {
            Entity passenger = _selectedBoardingSourceEntities[i];
            if (passenger == transport)
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=IsTransport passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            if (!transportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger))
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NotSoldierBoardingCandidate passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            int2 referenceCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
            int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
            int directBoardingCells = transportBoardingRuleSystem.GetTransportBoardingDirectCells(em, transport);
            if (!approachCellSystem.TryFindTransportApproachCell(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    boardingTransportSize,
                    referenceCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    transport,
                    em.GetComponentData<UnitGrid>(transport).Cell,
                    transportSize,
                    reservedBoardingCells,
                    directBoardingCells,
                    passengerFaction,
                    out int2 goal))
            {
                if (shouldLogTransportBoarding)
                {
                    EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoApproach passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                        $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={directBoardingCells}");
                }

                continue;
            }

            boardingOrders.Add(new PendingTransportBoardingOrder
            {
                Passenger = passenger,
                PassengerCell = referenceCell,
                Goal = goal,
                DirectBoarding = goal.Equals(referenceCell)
            });
            approachCellSystem.ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
        }

        if (boardingOrders.Count <= 0)
        {
            if (shouldLogTransportBoarding)
            {
                EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=NoBoardingOrders transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} " +
                    $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} usedCache={(usedCachedSelection ? 1 : 0)} seats={occupiedSeats}/{capacity} availableSeats={availableSeats}");
            }

            return Result.Rejected();
        }

        if (hasPendingAirPickupLanding)
        {
            airPickupSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, moveOrderSystem);
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=AirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
        }

        for (int i = 0; i < boardingOrders.Count; i++)
        {
            Entity passenger = boardingOrders[i].Passenger;
            int2 goal = boardingOrders[i].Goal;
            if (!em.Exists(passenger) || !transportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger))
                continue;

            moveOrderSystem.ClearMovementOrderComponents(em, passenger);
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
                em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
            moveOrderSystem.IssueImmediateMoveCommand(em, passenger, goal);
            if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
                em.SetComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = goal });
            else
                em.AddComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = goal });

            if (shouldLogTransportBoarding)
            {
                EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=Order passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                    $"from={boardingOrders[i].PassengerCell} goal={goal} direct={(boardingOrders[i].DirectBoarding ? 1 : 0)} usedCache={(usedCachedSelection ? 1 : 0)} seats={occupiedSeats + i}/{capacity}");
            }
        }

        float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
        return Result.AcceptedAt(transportCell, markerPosition, 0);
    }

    public Result TryIssueBoardSelectedTransportOrderToClickedPassenger(
        EntityManager em,
        Entity transport,
        Vector2 screenPosition,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem approachCellSystem,
        UnitTransportAirPickupSystem airPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
    {
        EnsureEntityQueries(em);
        bool shouldLogTransportBoarding = ShouldQueueTransportBoardingDiagnostics(em);
        if (transport == Entity.Null ||
            !em.Exists(transport) ||
            !transportBoardingQuerySystem.IsBoardablePlayerTransport(em, transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SelectedTransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)}");
            return Result.Rejected();
        }

        Entity passenger = Entity.Null;
        if (tryGetClickedUnitEntity == null ||
            !tryGetClickedUnitEntity(screenPosition, em, out passenger) ||
            passenger == transport)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=ClickedPassengerNotBoardable passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
            return Result.Rejected();
        }

        return TryIssueBoardSelectedTransportOrderToPassenger(
            em,
            transport,
            passenger,
            transportBoardingQuerySystem,
            transportBoardingRuleSystem,
            approachCellSystem,
            airPickupSystem,
            moveOrderSystem);
    }

    public Result TryIssueBoardSelectedTransportOrderToPassenger(
        EntityManager em,
        Entity transport,
        Entity passenger,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem approachCellSystem,
        UnitTransportAirPickupSystem airPickupSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        EnsureEntityQueries(em);
        bool shouldLogTransportBoarding = ShouldQueueTransportBoardingDiagnostics(em);
        if (transport == Entity.Null ||
            !em.Exists(transport) ||
            !transportBoardingQuerySystem.IsBoardablePlayerTransport(em, transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SelectedTransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)}");
            return Result.Rejected();
        }

        if (passenger == Entity.Null ||
            !em.Exists(passenger) ||
            !transportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger) ||
            passenger == transport)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=PassengerNotBoardable passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
            return Result.Rejected();
        }

        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        bool transportLanded = transportBoardingRuleSystem.IsTransportLandedForBoarding(em, transport);
        if (!transportLanded && !airTransport)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportNotLanded transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return Result.Rejected();
        }

        if (!transportLanded && em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportBusyRopeDisembark transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return Result.Rejected();
        }

        int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
        int occupiedSeats = em.GetBuffer<UnitTransportPassengerElement>(transport).Length + CountPendingBoardingOrders(em, transport);
        int availableSeats = capacity - occupiedSeats;
        if (availableSeats <= 0)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoSeats transport={DescribeTransportBoardingEntity(em, transport)} seats={occupiedSeats}/{capacity}");
            return Result.Rejected();
        }

        if (_gridPathingQuery.IsEmptyIgnoreFilter)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoGridPathing transport={DescribeTransportBoardingEntity(em, transport)} passenger={DescribeTransportBoardingEntity(em, passenger)}");
            return Result.Rejected();
        }

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        NativeBitArray blocked = blockerData.Blocked;
        NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
        using NativeArray<Entity> liveUnitEntities = _pathingLiveUnitsQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<UnitGrid> liveUnitGrids = _pathingLiveUnitsQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using NativeArray<UnitFootprint> liveUnitFootprints = _pathingLiveUnitsQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        bool hasPendingAirPickupLanding = false;
        int2 pendingAirPickupCell = default;
        _targetedBoardingSourceEntities.Clear();
        _targetedBoardingSourceEntities.Add(passenger);
        if (airTransport && !transportLanded)
        {
            if (!airPickupSystem.TryFindAirTransportPickupForBoarding(
                    em,
                    transport,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    transportSize,
                    _targetedBoardingSourceEntities,
                    1,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    out pendingAirPickupCell))
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} passenger={DescribeTransportBoardingEntity(em, passenger)}");
                return Result.Rejected();
            }

            transportCell = pendingAirPickupCell;
            hasPendingAirPickupLanding = true;
        }

        int2 referenceCell = em.GetComponentData<UnitGrid>(passenger).Cell;
        byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
        int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
        int directBoardingCells = transportBoardingRuleSystem.GetTransportBoardingDirectCells(em, transport);
        var reservedBoardingCells = new HashSet<int>();
        if (!approachCellSystem.TryFindTransportApproachCell(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                boardingTransportSize,
                referenceCell,
                passengerFootprint,
                passenger,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                transport,
                em.GetComponentData<UnitGrid>(transport).Cell,
                transportSize,
                reservedBoardingCells,
                directBoardingCells,
                passengerFaction,
                out int2 goal))
        {
            if (shouldLogTransportBoarding)
            {
                EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=NoApproach passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                    $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={directBoardingCells}");
            }

            return Result.Rejected();
        }

        if (hasPendingAirPickupLanding)
        {
            airPickupSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, moveOrderSystem);
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=AirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
        }

        moveOrderSystem.ClearMovementOrderComponents(em, passenger);
        if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
            em.AddBuffer<UnitTransportHiddenVisualScale>(passenger);
        moveOrderSystem.IssueImmediateMoveCommand(em, passenger, goal);
        if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
            em.SetComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = goal });
        else
            em.AddComponentData(passenger, new UnitTransportBoardingTarget { Transport = transport, Goal = goal });

        if (shouldLogTransportBoarding)
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                $"[TransportBoard] result=Order passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                $"from={referenceCell} goal={goal} direct={goal.Equals(referenceCell)} selectedTransport=1 seats={occupiedSeats}/{capacity}");
        }

        float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
        return Result.AcceptedAt(transportCell, markerPosition, 0);
    }

    public bool IsBoardablePlayerTransportClick(
        EntityManager em,
        Vector2 screenPosition,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell)
    {
        EnsureEntityQueries(em);
        return TryGetClickedOrNearbyBoardableTransport(
            screenPosition,
            em,
            transportBoardingRuleSystem,
            transportBoardingQuerySystem,
            tryGetClickedUnitEntity,
            tryGetClickedCell,
            out _,
            false);
    }

    private int CollectSelectedBoardingSourceEntities(
        EntityManager em,
        SelectionStateSystem selectionStateSystem,
        List<Entity> selectedEntities,
        out int selectedTagCount,
        out int selectedMoveCount,
        out bool usedCachedSelection)
    {
        selectedEntities.Clear();
        selectedTagCount = 0;
        selectedMoveCount = 0;
        usedCachedSelection = false;

        EnsureEntityQueries(em);
        using NativeArray<Entity> selectedMoveEntities = _selectedMoveQuery.ToEntityArray(Allocator.Temp);
        selectedMoveCount = selectedMoveEntities.Length;
        if (selectedMoveEntities.Length > 0)
        {
            selectionStateSystem.CachedSelectedMoveEntities.Clear();
            for (int i = 0; i < selectedMoveEntities.Length; i++)
            {
                Entity entity = selectedMoveEntities[i];
                selectedEntities.Add(entity);
                if (SelectionStateSystem.IsCacheableSelectedMoveEntity(em, entity))
                    selectionStateSystem.CachedSelectedMoveEntities.Add(entity);
            }

            selectedTagCount = selectedMoveCount;
            return selectedEntities.Count;
        }

        using NativeArray<Entity> selectedTagEntities = _selectedTagQuery.ToEntityArray(Allocator.Temp);
        selectedTagCount = selectedTagEntities.Length;
        if (selectedTagEntities.Length > 0)
        {
            for (int i = 0; i < selectedTagEntities.Length; i++)
                selectedEntities.Add(selectedTagEntities[i]);
            return selectedEntities.Count;
        }

        List<Entity> cachedSelectedMoveEntities = selectionStateSystem.CachedSelectedMoveEntities;
        for (int i = cachedSelectedMoveEntities.Count - 1; i >= 0; i--)
        {
            Entity entity = cachedSelectedMoveEntities[i];
            if (!SelectionStateSystem.IsCacheableSelectedMoveEntity(em, entity))
            {
                cachedSelectedMoveEntities.RemoveAt(i);
                continue;
            }

            selectedEntities.Add(entity);
        }

        if (selectedEntities.Count > 0)
            usedCachedSelection = true;
        return selectedEntities.Count;
    }

    private bool TryGetClickedOrNearbyBoardableTransport(
        Vector2 screenPosition,
        EntityManager em,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell,
        out Entity transport,
        bool logDiagnostics = true)
    {
        transport = Entity.Null;
        bool shouldLogTransportBoarding = logDiagnostics && ShouldQueueTransportBoardingDiagnostics(em);
        Entity clickedEntity = Entity.Null;
        bool hasClickedEntity = tryGetClickedUnitEntity(screenPosition, em, out clickedEntity);
        if (hasClickedEntity && transportBoardingQuerySystem.IsBoardablePlayerTransport(em, clickedEntity))
        {
            transport = clickedEntity;
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=ClickedTransport transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return true;
        }

        if (!tryGetClickedCell(screenPosition, em, out int2 clickedCell, out _))
        {
            if (shouldLogTransportBoarding && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoClickedCell clicked={DescribeTransportBoardingEntity(em, clickedEntity)} {DescribeTransportAirState(em, clickedEntity)}");
            return false;
        }

        if (TryFindNearbyBoardableTransport(em, clickedCell, transportBoardingQuerySystem, out transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NearbyTransport clickedCell={clickedCell} transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return true;
        }

        if (shouldLogTransportBoarding && hasClickedEntity && IsKnownPersonnelTransport(em, clickedEntity))
        {
            EnqueueTransportBoardingDiagnostic(
                em,
                $"[TransportBoard] result=ClickedTransportRejected clicked={DescribeTransportBoardingEntity(em, clickedEntity)} " +
                $"player={(IsPlayerFaction(em, clickedEntity) ? 1 : 0)} landed={(transportBoardingRuleSystem.IsTransportLandedForBoarding(em, clickedEntity) ? 1 : 0)} {DescribeTransportAirState(em, clickedEntity)}");
        }

        if (hasClickedEntity &&
            em.Exists(clickedEntity) &&
            em.HasComponent<UnitMove>(clickedEntity) &&
            !em.HasComponent<RuntimeBuildingCombatTag>(clickedEntity) &&
            !em.HasComponent<StaticGridBlocker>(clickedEntity))
        {
            return false;
        }

        return false;
    }

    private bool TryFindNearbyBoardableTransport(
        EntityManager em,
        int2 clickedCell,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        out Entity transport)
    {
        transport = Entity.Null;
        EnsureEntityQueries(em);
        using NativeArray<Entity> entities = _allSelectableQuery.ToEntityArray(Allocator.Temp);
        int bestScore = int.MaxValue;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity candidate = entities[i];
            if (!transportBoardingQuerySystem.IsBoardablePlayerTransport(em, candidate))
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
            int2 footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
            int clickPaddingCells = transportBoardingQuerySystem.GetTransportBoardingClickPaddingCells(em, candidate, footprint);
            if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, clickPaddingCells))
                continue;

            int2 delta = clickedCell - cell;
            int score = math.abs(delta.x) + math.abs(delta.y);
            if (score >= bestScore)
                continue;

            bestScore = score;
            transport = candidate;
        }

        return transport != Entity.Null;
    }

    private int CountPendingBoardingOrders(EntityManager em, Entity transport)
    {
        EnsureEntityQueries(em);
        using NativeArray<Entity> entities = _transportBoardingTargetQuery.ToEntityArray(Allocator.Temp);
        int count = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.Exists(entity) &&
                em.HasComponent<UnitTransportBoardingTarget>(entity) &&
                em.GetComponentData<UnitTransportBoardingTarget>(entity).Transport == transport)
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsKnownPersonnelTransport(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return false;

        if (em.HasComponent<UnitTransportCapacity>(entity) &&
            math.max(0, em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity) > 0)
        {
            return true;
        }

        return new UnitTransportCapacitySystem().ResolveTransportCapacity(em, entity) > 0;
    }

    private static bool IsPlayerFaction(EntityManager em, Entity entity)
    {
        return em.Exists(entity) &&
               em.HasComponent<Faction>(entity) &&
               FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
    }

    private static string DescribeTransportBoardingEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
            return "null";
        if (!em.Exists(entity))
            return $"{entity}:missing";

        string sourceName = ResolveUnitSourceName(em, entity);
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
        string capacity = em.HasComponent<UnitTransportCapacity>(entity)
            ? em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity.ToString()
            : "no-capacity";
        string passengers = em.HasBuffer<UnitTransportPassengerElement>(entity)
            ? em.GetBuffer<UnitTransportPassengerElement>(entity).Length.ToString()
            : "no-passengers";

        return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health} seats={passengers}/{capacity}";
    }

    private static string ResolveUnitSourceName(EntityManager em, Entity entity)
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

    private static string DescribeTransportAirState(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) || !em.HasComponent<UnitAirMovement>(entity))
            return "air=none";
        if (!em.HasComponent<UnitAirComponent>(entity))
            return "air=missing-state";

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
        return $"airborne={airState.Airborne} takeoff={airState.TakeoffRolling} landing={airState.LandingRolling} returning={airState.ReturningHome} rope={(em.HasComponent<UnitTransportRopeDisembarkRequest>(entity) ? 1 : 0)}";
    }

    private static bool ShouldQueueTransportBoardingDiagnostics(EntityManager em)
    {
        if (Application.isBatchMode)
            return true;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeDiagnosticsStateComponent>());
        return !query.IsEmptyIgnoreFilter &&
            em.GetComponentData<RuntimeDiagnosticsStateComponent>(query.GetSingletonEntity()).TransportBoardingDiagnostics != 0;
    }

    private static Entity EnsureTransportBoardingDiagnosticQueue(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<TransportBoardingDiagnosticLogQueueComponent>(),
            ComponentType.ReadWrite<TransportBoardingDiagnosticLogComponent>());
        if (!query.IsEmptyIgnoreFilter)
            return query.GetSingletonEntity();

        Entity queueEntity = em.CreateEntity(typeof(TransportBoardingDiagnosticLogQueueComponent));
        em.SetName(queueEntity, "TransportBoardingDiagnosticLogQueue");
        em.AddBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
        return queueEntity;
    }

    private static void EnqueueTransportBoardingDiagnostic(EntityManager em, FixedString512Bytes message)
    {
        Entity queueEntity = EnsureTransportBoardingDiagnosticQueue(em);
        DynamicBuffer<TransportBoardingDiagnosticLogComponent> logs = em.GetBuffer<TransportBoardingDiagnosticLogComponent>(queueEntity);
        logs.Add(new TransportBoardingDiagnosticLogComponent { Message = message });
    }
}
