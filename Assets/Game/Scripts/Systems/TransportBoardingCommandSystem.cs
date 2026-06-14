using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct TransportBoardingCommandSystem : ISystem
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

    private bool _queriesInitialized;
    private EntityQuery _commandQueueQuery;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _selectedTagQuery;
    private EntityQuery _gridPathingQuery;
    private EntityQuery _allSelectableQuery;
    private EntityQuery _transportBoardingTargetQuery;
    private EntityQuery _pathingLiveUnitsQuery;

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
            ComponentType.ReadWrite<RtsSelectionCommandResultElement>());
        EnsureEntityQueries(state.EntityManager);
    }

    public void OnUpdate(ref SystemState state)
    {
        ProcessPreResolvedTransportRequests(state.EntityManager);
    }

    public void EnsureEntityQueries(EntityManager em)
    {
        if (_queriesInitialized)
            return;

        _queriesInitialized = true;
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

    public bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell)
    {
        EnsureEntityQueries(em);
        bool handledAny = false;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (!IsTransportCommandIntent(request.Kind))
            {
                i++;
                continue;
            }

            if (IsPreResolvedTransportCommandIntent(request))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            RtsSelectionCommandResultElement result = request.Kind switch
            {
                RtsSelectionCommandIntentKind.BoardTransport => ProcessBoardTransportRequest(
                    em,
                    request,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    selectionStateSystem,
                    tryGetClickedUnitEntity,
                    tryGetClickedCell),
                RtsSelectionCommandIntentKind.BoardSelectedTransport => ProcessBoardSelectedTransportRequest(
                    em,
                    request,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    tryGetClickedUnitEntity),
                RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                    em,
                    request,
                    transportAirPickupSystem,
                    moveOrderSystem),
                RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                    em,
                    request,
                    transportCapacitySystem,
                    moveOrderSystem),
                _ => ProcessDisembarkTransportRequest(
                    em,
                    request,
                    transportCapacitySystem,
                    moveOrderSystem)
            };
            AddCommandResult(em, commandEntity, commandResults, result);
            if (commandEntity != Entity.Null && em.Exists(commandEntity))
            {
                if (em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity))
                    commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
                if (em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
                    commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            }
        }

        return handledAny;
    }

    private bool ProcessPreResolvedTransportRequests(EntityManager em)
    {
        if (_commandQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = _commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        bool handledAny = false;
        var transportCapacitySystem = new UnitTransportCapacitySystem();
        var transportAirPickupSystem = new UnitTransportAirPickupSystem();
        var moveOrderSystem = new UnitMoveOrderSystem();
        var selectionStateSystem = new SelectionStateSystem();

        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (!IsPreResolvedTransportCommandIntent(request))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            RtsSelectionCommandResultElement result = request.Kind switch
            {
                RtsSelectionCommandIntentKind.BoardTransport => ProcessBoardTransportTargetRequest(
                    em,
                    request,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    selectionStateSystem),
                RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                    em,
                    request,
                    transportAirPickupSystem,
                    moveOrderSystem),
                RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                    em,
                    request,
                    transportCapacitySystem,
                    moveOrderSystem),
                _ => ProcessDisembarkTransportRequest(
                    em,
                    request,
                    transportCapacitySystem,
                    moveOrderSystem)
            };

            AddCommandResult(em, commandEntity, commandResults, result);
            if (em.Exists(commandEntity))
            {
                if (em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity))
                    commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
                if (em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
                    commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            }
        }

        return handledAny;
    }

    private static bool IsTransportCommandIntent(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.BoardTransport ||
               kind == RtsSelectionCommandIntentKind.BoardSelectedTransport ||
               kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger ||
               kind == RtsSelectionCommandIntentKind.DisembarkTransport ||
               kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger;
    }

    private static bool IsPreResolvedTransportCommandIntent(RtsSelectionCommandIntentRequestElement request)
    {
        return (request.Kind == RtsSelectionCommandIntentKind.BoardTransport &&
                request.HasTargetEntity != 0) ||
               (request.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger &&
                request.HasTargetEntity != 0 &&
                request.HasSecondaryTargetEntity != 0) ||
               (request.Kind == RtsSelectionCommandIntentKind.DisembarkTransport &&
                request.HasTargetEntity != 0) ||
               (request.Kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger &&
                request.HasTargetEntity != 0 &&
                request.HasSecondaryTargetEntity != 0);
    }

    private static void AddCommandResult(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandResultElement> fallbackResults,
        RtsSelectionCommandResultElement result)
    {
        if (commandEntity != Entity.Null && em.Exists(commandEntity) && em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
        {
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Add(result);
            return;
        }

        fallbackResults.Add(result);
    }

    private RtsSelectionCommandResultElement ProcessBoardTransportRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell)
    {
        Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
        Result result = TryIssueBoardTransportOrderToClickedUnit(
            em,
            screenPosition,
            transportAirPickupSystem,
            moveOrderSystem,
            selectionStateSystem,
            tryGetClickedUnitEntity,
            tryGetClickedCell);

        return ToBoardingCommandResultElement(request, result);
    }

    private RtsSelectionCommandResultElement ProcessBoardTransportTargetRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem)
    {
        Result result = request.HasTargetEntity != 0
            ? TryIssueBoardTransportOrderToTransport(
                em,
                request.TargetEntity,
                transportAirPickupSystem,
                moveOrderSystem,
                selectionStateSystem)
            : Result.Rejected();

        return ToBoardingCommandResultElement(request, result);
    }

    private RtsSelectionCommandResultElement ProcessBoardSelectedTransportRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
    {
        Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
        Result result = TryIssueBoardSelectedTransportOrderToClickedPassenger(
            em,
            request.TargetEntity,
            screenPosition,
            transportAirPickupSystem,
            moveOrderSystem,
            tryGetClickedUnitEntity);

        return ToBoardingCommandResultElement(request, result);
    }

    private RtsSelectionCommandResultElement ProcessBoardSelectedTransportPassengerRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        Result result = request.HasTargetEntity != 0 &&
            request.HasSecondaryTargetEntity != 0
            ? TryIssueBoardSelectedTransportOrderToPassenger(
                em,
                request.TargetEntity,
                request.SecondaryTargetEntity,
                transportAirPickupSystem,
                moveOrderSystem)
            : Result.Rejected();

        return ToBoardingCommandResultElement(request, result);
    }

    private static RtsSelectionCommandResultElement ToBoardingCommandResultElement(
        RtsSelectionCommandIntentRequestElement request,
        Result result)
    {
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetCell = result.MarkerCell,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.MarkerPosition,
            TargetKind = result.Accepted ? RtsSelectionCommandTargetKind.Cell : RtsSelectionCommandTargetKind.None,
            CommandMode = (int)TacticalCommandMode.Board,
            HasCommandResult = 1,
            Accepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = result.Accepted ? 0 : (int)TacticalCommandReasonCode.CommandUnavailable,
            FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
            EmitScreenMarker = result.Accepted ? (byte)1 : (byte)0,
            MarkerFactionId = result.MarkerFactionId,
            HasTargetCell = result.Accepted ? (byte)1 : (byte)0,
            HasWorldPosition = result.Accepted ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.Accepted ? (byte)1 : (byte)0
        };
    }

    private RtsSelectionCommandResultElement ProcessDisembarkTransportRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        bool accepted = request.HasTargetEntity != 0 &&
                        TryDisembarkTransport(em, request.TargetEntity, transportCapacitySystem, moveOrderSystem, _gridPathingQuery);
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetEntity = request.TargetEntity,
            TargetKind = request.HasTargetEntity != 0 ? RtsSelectionCommandTargetKind.Entity : RtsSelectionCommandTargetKind.None,
            CommandMode = (int)TacticalCommandMode.Board,
            HasCommandResult = accepted ? (byte)1 : (byte)0,
            Accepted = accepted ? (byte)1 : (byte)0,
            FeedbackLifetime = accepted
                ? RtsSelectionCommandFeedbackLifetime.Transient
                : RtsSelectionCommandFeedbackLifetime.Hidden,
            HasTargetEntity = request.HasTargetEntity
        };
    }

    private RtsSelectionCommandResultElement ProcessDisembarkTransportPassengerRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        bool accepted = request.HasTargetEntity != 0 &&
                        request.HasSecondaryTargetEntity != 0 &&
                        TryDisembarkTransportPassenger(
                            em,
                            request.TargetEntity,
                            request.SecondaryTargetEntity,
                            transportCapacitySystem,
                            moveOrderSystem,
                            _gridPathingQuery);

        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetEntity = request.TargetEntity,
            TargetKind = request.HasTargetEntity != 0 ? RtsSelectionCommandTargetKind.Entity : RtsSelectionCommandTargetKind.None,
            CommandMode = (int)TacticalCommandMode.Board,
            HasCommandResult = accepted ? (byte)1 : (byte)0,
            Accepted = accepted ? (byte)1 : (byte)0,
            FeedbackLifetime = accepted
                ? RtsSelectionCommandFeedbackLifetime.Transient
                : RtsSelectionCommandFeedbackLifetime.Hidden,
            HasTargetEntity = request.HasTargetEntity
        };
    }

    public Result TryIssueBoardTransportOrderToClickedUnit(
        EntityManager em,
        Vector2 screenPosition,
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
                tryGetClickedUnitEntity,
                tryGetClickedCell,
                out Entity transport))
        {
            return Result.Rejected();
        }

        return TryIssueBoardTransportOrderToTransport(
            em,
            transport,
            airPickupSystem,
            moveOrderSystem,
            selectionStateSystem);
    }

    public Result TryIssueBoardTransportOrderToTransport(
        EntityManager em,
        Entity transport,
        UnitTransportAirPickupSystem airPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem)
    {
        EnsureEntityQueries(em);
        selectionStateSystem ??= new SelectionStateSystem();
        bool shouldLogTransportBoarding = ShouldQueueTransportBoardingDiagnostics(em);
        if (!IsBoardablePlayerTransport(em, transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)} {DescribeTransportAirState(em, transport)}");
            return Result.Rejected();
        }

        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        bool transportLanded = IsTransportLandedForBoarding(em, transport);
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

        List<Entity> selectedBoardingSourceEntities = new();
        int selectedCount = CollectSelectedBoardingSourceEntities(em, selectionStateSystem, selectedBoardingSourceEntities, out int selectedTagCount, out int selectedMoveCount, out bool usedCachedSelection);
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
        int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
        using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
        using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
        using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
        CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
        NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
        NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
        NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();

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
                    selectedBoardingSourceEntities,
                    selectedCount,
                    liveUnitEntityArray,
                    liveUnitGridArray,
                    liveUnitFootprintArray,
                    out pendingAirPickupCell))
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={DescribeTransportBoardingEntity(em, transport)} selected={selectedCount}");
                return Result.Rejected();
            }

            transportCell = pendingAirPickupCell;
            hasPendingAirPickupLanding = true;
        }

        List<PendingTransportBoardingOrder> boardingOrders = new(32);
        HashSet<int> reservedBoardingCells = new();
        for (int i = 0; i < selectedCount && boardingOrders.Count < availableSeats; i++)
        {
            Entity passenger = selectedBoardingSourceEntities[i];
            if (passenger == transport)
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=IsTransport passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            if (!IsSoldierBoardingCandidate(em, passenger))
            {
                if (shouldLogTransportBoarding)
                    EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NotSoldierBoardingCandidate passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
                continue;
            }

            int2 referenceCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
            int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            if (!TryFindTransportApproachCell(
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
                    liveUnitEntityArray,
                    liveUnitGridArray,
                    liveUnitFootprintArray,
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
            ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
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

        var passengerStateSystem = new UnitTransportPassengerStateSystem();
        EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
        try
        {
            for (int i = 0; i < boardingOrders.Count; i++)
            {
                Entity passenger = boardingOrders[i].Passenger;
                int2 goal = boardingOrders[i].Goal;
                if (!em.Exists(passenger) || !IsSoldierBoardingCandidate(em, passenger))
                    continue;

                UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
                UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, goal);
                passengerStateSystem.ApplyBoardingOrderState(
                    em,
                    ref boardingStateEcb,
                    passenger,
                    transport,
                    goal);

                if (shouldLogTransportBoarding)
                {
                    EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=Order passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)} " +
                        $"from={boardingOrders[i].PassengerCell} goal={goal} direct={(boardingOrders[i].DirectBoarding ? 1 : 0)} usedCache={(usedCachedSelection ? 1 : 0)} seats={occupiedSeats + i}/{capacity}");
                }
            }

            boardingStateEcb.Playback(em);
        }
        finally
        {
            boardingStateEcb.Dispose();
        }

        float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
        return Result.AcceptedAt(transportCell, markerPosition, 0);
    }

    public Result TryIssueBoardSelectedTransportOrderToClickedPassenger(
        EntityManager em,
        Entity transport,
        Vector2 screenPosition,
        UnitTransportAirPickupSystem airPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
    {
        EnsureEntityQueries(em);
        bool shouldLogTransportBoarding = ShouldQueueTransportBoardingDiagnostics(em);
        if (transport == Entity.Null ||
            !em.Exists(transport) ||
            !IsBoardablePlayerTransport(em, transport))
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
            airPickupSystem,
            moveOrderSystem);
    }

    public Result TryIssueBoardSelectedTransportOrderToPassenger(
        EntityManager em,
        Entity transport,
        Entity passenger,
        UnitTransportAirPickupSystem airPickupSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        EnsureEntityQueries(em);
        bool shouldLogTransportBoarding = ShouldQueueTransportBoardingDiagnostics(em);
        if (transport == Entity.Null ||
            !em.Exists(transport) ||
            !IsBoardablePlayerTransport(em, transport))
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SelectedTransportNotBoardable transport={DescribeTransportBoardingEntity(em, transport)}");
            return Result.Rejected();
        }

        if (passenger == Entity.Null ||
            !em.Exists(passenger) ||
            !IsSoldierBoardingCandidate(em, passenger) ||
            passenger == transport)
        {
            if (shouldLogTransportBoarding)
                EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=PassengerNotBoardable passenger={DescribeTransportBoardingEntity(em, passenger)} transport={DescribeTransportBoardingEntity(em, transport)}");
            return Result.Rejected();
        }

        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        bool transportLanded = IsTransportLandedForBoarding(em, transport);
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
        int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
        using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
        using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
        using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
        CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
        NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
        NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
        NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();

        bool hasPendingAirPickupLanding = false;
        int2 pendingAirPickupCell = default;
        List<Entity> targetedBoardingSourceEntities = new(1) { passenger };
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
                    targetedBoardingSourceEntities,
                    1,
                    liveUnitEntityArray,
                    liveUnitGridArray,
                    liveUnitFootprintArray,
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
        int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
        HashSet<int> targetedReservedBoardingCells = new();
        if (!TryFindTransportApproachCell(
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
                liveUnitEntityArray,
                liveUnitGridArray,
                liveUnitFootprintArray,
                transport,
                em.GetComponentData<UnitGrid>(transport).Cell,
                transportSize,
                targetedReservedBoardingCells,
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

        UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
        UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, goal);
        var passengerStateSystem = new UnitTransportPassengerStateSystem();
        EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
        try
        {
            passengerStateSystem.ApplyBoardingOrderState(
                em,
                ref boardingStateEcb,
                passenger,
                transport,
                goal);
            boardingStateEcb.Playback(em);
        }
        finally
        {
            boardingStateEcb.Dispose();
        }

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
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell)
    {
        EnsureEntityQueries(em);
        return TryGetClickedOrNearbyBoardableTransport(
            screenPosition,
            em,
            tryGetClickedUnitEntity,
            tryGetClickedCell,
            out _,
            false);
    }

    public bool TryResolveBoardablePlayerTransportClick(
        EntityManager em,
        Vector2 screenPosition,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell,
        out Entity transport)
    {
        EnsureEntityQueries(em);
        return TryGetClickedOrNearbyBoardableTransport(
            screenPosition,
            em,
            tryGetClickedUnitEntity,
            tryGetClickedCell,
            out transport,
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
        selectedMoveCount = _selectedMoveQuery.CalculateEntityCount();
        if (selectedMoveCount > 0)
        {
            selectionStateSystem.CachedSelectedMoveEntities.Clear();
            CollectEntities(em, _selectedMoveQuery, selectedEntities);
            for (int i = 0; i < selectedEntities.Count; i++)
            {
                Entity entity = selectedEntities[i];
                if (SelectionStateSystem.IsCacheableSelectedMoveEntity(em, entity))
                    selectionStateSystem.CachedSelectedMoveEntities.Add(entity);
            }

            selectedTagCount = selectedMoveCount;
            return selectedEntities.Count;
        }

        selectedTagCount = _selectedTagQuery.CalculateEntityCount();
        if (selectedTagCount > 0)
        {
            CollectEntities(em, _selectedTagQuery, selectedEntities);
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

    private static void CollectEntities(EntityManager em, EntityQuery query, List<Entity> entities)
    {
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < chunkEntities.Length; i++)
                entities.Add(chunkEntities[i]);
        }
    }

    private void CollectPathingLiveUnits(
        EntityManager em,
        NativeList<Entity> entities,
        NativeList<UnitGrid> grids,
        NativeList<UnitFootprint> footprints)
    {
        entities.Clear();
        grids.Clear();
        footprints.Clear();

        EnsureEntityQueries(em);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<UnitGrid> gridType = em.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<UnitFootprint> footprintType = em.GetComponentTypeHandle<UnitFootprint>(true);
        using NativeArray<ArchetypeChunk> chunks = _pathingLiveUnitsQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> chunkEntities = chunk.GetNativeArray(entityType);
            NativeArray<UnitGrid> chunkGrids = chunk.GetNativeArray(ref gridType);
            NativeArray<UnitFootprint> chunkFootprints = chunk.GetNativeArray(ref footprintType);
            for (int i = 0; i < chunkEntities.Length; i++)
            {
                entities.Add(chunkEntities[i]);
                grids.Add(chunkGrids[i]);
                footprints.Add(chunkFootprints[i]);
            }
        }
    }

    private bool TryGetClickedOrNearbyBoardableTransport(
        Vector2 screenPosition,
        EntityManager em,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TryGetClickedCellDelegate tryGetClickedCell,
        out Entity transport,
        bool logDiagnostics = true)
    {
        transport = Entity.Null;
        bool shouldLogTransportBoarding = logDiagnostics && ShouldQueueTransportBoardingDiagnostics(em);
        Entity clickedEntity = Entity.Null;
        bool hasClickedEntity = tryGetClickedUnitEntity(screenPosition, em, out clickedEntity);
        if (hasClickedEntity && IsBoardablePlayerTransport(em, clickedEntity))
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

        if (TryFindNearbyBoardableTransport(em, clickedCell, out transport))
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
                $"player={(IsPlayerFaction(em, clickedEntity) ? 1 : 0)} landed={(IsTransportLandedForBoarding(em, clickedEntity) ? 1 : 0)} {DescribeTransportAirState(em, clickedEntity)}");
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
        out Entity transport)
    {
        transport = Entity.Null;
        EnsureEntityQueries(em);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _allSelectableQuery.ToArchetypeChunkArray(Allocator.Temp);
        int bestScore = int.MaxValue;
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity candidate = entities[i];
                if (!IsBoardablePlayerTransport(em, candidate))
                    continue;

                int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                int2 footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
                int clickPaddingCells = GetTransportBoardingClickPaddingCells(em, candidate, footprint);
                if (!UnitFootprintUtility.ContainsCellWithPadding(cell, footprint, clickedCell, clickPaddingCells))
                    continue;

                int2 delta = clickedCell - cell;
                int score = math.abs(delta.x) + math.abs(delta.y);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                transport = candidate;
            }
        }

        return transport != Entity.Null;
    }

    internal static bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
    {
        if (!em.HasComponent<UnitAirMovement>(transport))
            return true;

        if (!em.HasComponent<UnitAirComponent>(transport) || !em.HasComponent<LocalTransform>(transport))
            return false;

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
        bool physicallyGrounded = transform.Position.y <= groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
        return airState.Airborne == 0 &&
               airState.TakeoffRolling == 0 &&
               airState.LandingRolling == 0 &&
               physicallyGrounded &&
               !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
    }

    internal static int GetTransportBoardingDirectCells(EntityManager em, Entity transport)
    {
        return em.HasComponent<UnitAirMovement>(transport)
            ? TransportBoardingData.AirBoardingClearanceCells
            : TransportBoardingData.BoardingClearanceCells;
    }

    private static int GetTransportBoardingClickPaddingCells(EntityManager em, Entity transport, int2 footprint)
    {
        int footprintMax = math.max(footprint.x, footprint.y);
        if (em.Exists(transport) && em.HasComponent<UnitAirMovement>(transport))
            return math.max(24, footprintMax + 24);

        return math.max(6, footprintMax + 4);
    }

    public static bool IsBoardablePlayerTransport(EntityManager em, Entity transport)
    {
        return em.Exists(transport) &&
               new UnitTransportCapacitySystem().TryEnsureTransportCapacity(em, transport) &&
               em.HasComponent<Faction>(transport) &&
               FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(transport).Id) &&
               em.HasComponent<UnitGrid>(transport) &&
               em.HasComponent<UnitFootprint>(transport) &&
               em.HasComponent<LocalTransform>(transport);
    }

    public static bool IsSoldierBoardingCandidate(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<Faction>(entity) ||
            !FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
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

    public static bool TryFindAirTransportPickupCellNearPassenger(
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

    public static bool TryFindTransportApproachCell(
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

    public static void ReserveFootprintCells(GridConfig grid, int2 cell, int2 footprintSize, HashSet<int> reservedCells)
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

    public static bool TryFindTransportDisembarkCell(
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
            TransportBoardingData.BoardingClearanceCells,
            false,
            out goal);
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
        if (directBoardingCells > TransportBoardingData.BoardingClearanceCells &&
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

    private static bool IsRopeDisembarkTransport(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
            return false;

        string sourceName = ResolveUnitSourceName(em, transport);
        return sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool StartRopeDisembarkTransport(
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

        UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, transport);
        if (em.HasComponent<UnitAirMovement>(transport) &&
            em.HasComponent<UnitAirComponent>(transport) &&
            em.HasComponent<LocalTransform>(transport))
        {
            UnitAirMovement airMovement = em.GetComponentData<UnitAirMovement>(transport);
            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
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

    private static bool TryDisembarkTransport(
        EntityManager em,
        Entity transport,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitMoveOrderSystem moveOrderSystem,
        EntityQuery gridPathingQuery)
    {
        if (!em.Exists(transport) ||
            !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
            !em.HasComponent<UnitGrid>(transport) ||
            !em.HasComponent<UnitFootprint>(transport) ||
            gridPathingQuery.IsEmptyIgnoreFilter)
        {
            return false;
        }

        Entity gridEntity = gridPathingQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);

        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 referenceCell = transportCell;
        if (em.HasComponent<LocalTransform>(transport))
            referenceCell = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);

        if (IsRopeDisembarkTransport(em, transport))
            return StartRopeDisembarkTransport(em, transport, referenceCell, moveOrderSystem);

        List<Entity> passengerSnapshot = new(passengers.Length);
        for (int i = 0; i < passengers.Length; i++)
            passengerSnapshot.Add(passengers[i].Passenger);
        if (passengerSnapshot.Count == 0)
            return false;

        passengers.Clear();
        HashSet<int> reservedDisembarkCells = new();
        List<Entity> remainingPassengers = new();
        List<Entity> disembarkingPassengers = new();
        List<int2> disembarkCells = new();
        for (int i = 0; i < passengerSnapshot.Count; i++)
        {
            Entity passenger = passengerSnapshot[i];
            if (!em.Exists(passenger))
                continue;

            if (!TryFindTransportDisembarkCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    transportCell,
                    transportSize,
                    referenceCell,
                    out int2 cell))
            {
                remainingPassengers.Add(passenger);
                continue;
            }

            int cellIndex = GridUtils.CellToIndex(cell, grid.Width);
            reservedDisembarkCells.Add(cellIndex);
            disembarkingPassengers.Add(passenger);
            disembarkCells.Add(cell);
        }

        EntityCommandBuffer ecb = new(Allocator.Temp);
        for (int i = 0; i < disembarkingPassengers.Count; i++)
        {
            Entity passenger = disembarkingPassengers[i];
            int2 cell = disembarkCells[i];
            if (!em.Exists(passenger))
                continue;

            moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
            moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
            UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, passenger);

            if (em.HasComponent<UnitGrid>(passenger))
                ecb.SetComponent(passenger, new UnitGrid { Cell = cell });
            if (em.HasComponent<LocalTransform>(passenger))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(passenger);
                transform.Position = GridUtils.CellToWorldCenter(grid, cell);
                ecb.SetComponent(passenger, transform);
            }
        }
        ecb.Playback(em);
        ecb.Dispose();

        for (int i = 0; i < disembarkingPassengers.Count; i++)
        {
            Entity passenger = disembarkingPassengers[i];
            if (em.Exists(passenger))
                UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
        }

        if (remainingPassengers.Count > 0 && em.Exists(transport) && em.HasBuffer<UnitTransportPassengerElement>(transport))
        {
            DynamicBuffer<UnitTransportPassengerElement> remainingBuffer = em.GetBuffer<UnitTransportPassengerElement>(transport);
            for (int i = 0; i < remainingPassengers.Count; i++)
            {
                Entity passenger = remainingPassengers[i];
                if (em.Exists(passenger))
                    remainingBuffer.Add(new UnitTransportPassengerElement { Passenger = passenger });
            }
        }

        return disembarkingPassengers.Count > 0 || remainingPassengers.Count != passengerSnapshot.Count;
    }

    private static bool TryDisembarkTransportPassenger(
        EntityManager em,
        Entity transport,
        Entity passenger,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitMoveOrderSystem moveOrderSystem,
        EntityQuery gridPathingQuery)
    {
        if (!em.Exists(transport) ||
            !em.Exists(passenger) ||
            !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
            !em.HasBuffer<UnitTransportPassengerElement>(transport))
        {
            return false;
        }

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        int passengerIndex = IndexOfPassenger(passengers, passenger);
        if (passengerIndex < 0)
            return false;

        if (IsRopeDisembarkTransport(em, transport))
        {
            if (passengerIndex > 0)
            {
                UnitTransportPassengerElement selected = passengers[passengerIndex];
                passengers.RemoveAt(passengerIndex);
                passengers.Insert(0, selected);
            }

            int2 ropeReferenceCell = em.HasComponent<UnitGrid>(transport)
                ? em.GetComponentData<UnitGrid>(transport).Cell
                : default;
            if (em.HasComponent<LocalTransform>(transport) && !gridPathingQuery.IsEmptyIgnoreFilter)
            {
                Entity gridEntity = gridPathingQuery.GetSingletonEntity();
                GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                ropeReferenceCell = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);
            }

            return StartRopeDisembarkTransport(em, transport, ropeReferenceCell, moveOrderSystem);
        }

        if (!em.HasComponent<UnitGrid>(transport) ||
            !em.HasComponent<UnitFootprint>(transport) ||
            gridPathingQuery.IsEmptyIgnoreFilter)
        {
            return false;
        }

        Entity pathingGridEntity = gridPathingQuery.GetSingletonEntity();
        GridConfig pathingGrid = em.GetComponentData<GridConfig>(pathingGridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(pathingGridEntity).AsNativeArray();
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(pathingGridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(pathingGridEntity).Occupied;
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 groundReferenceCell = transportCell;
        if (em.HasComponent<LocalTransform>(transport))
            groundReferenceCell = GridUtils.WorldToCell(pathingGrid, em.GetComponentData<LocalTransform>(transport).Position);

        HashSet<int> reservedDisembarkCells = new();
        if (!TryFindTransportDisembarkCell(
                pathingGrid,
                walkable,
                blocked,
                occupied,
                reservedDisembarkCells,
                transportCell,
                transportSize,
                groundReferenceCell,
                out int2 cell))
        {
            return false;
        }

        passengers.RemoveAt(passengerIndex);
        EntityCommandBuffer ecb = new(Allocator.Temp);
        moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
        moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
        UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, passenger);

        if (em.HasComponent<UnitGrid>(passenger))
            ecb.SetComponent(passenger, new UnitGrid { Cell = cell });
        if (em.HasComponent<LocalTransform>(passenger))
        {
            LocalTransform transform = em.GetComponentData<LocalTransform>(passenger);
            transform.Position = GridUtils.CellToWorldCenter(pathingGrid, cell);
            ecb.SetComponent(passenger, transform);
        }

        ecb.Playback(em);
        ecb.Dispose();
        UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
        return true;
    }

    private static int IndexOfPassenger(DynamicBuffer<UnitTransportPassengerElement> passengers, Entity passenger)
    {
        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i].Passenger == passenger)
                return i;
        }

        return -1;
    }

    private int CountPendingBoardingOrders(EntityManager em, Entity transport)
    {
        EnsureEntityQueries(em);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _transportBoardingTargetQuery.ToArchetypeChunkArray(Allocator.Temp);
        int count = 0;
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
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
