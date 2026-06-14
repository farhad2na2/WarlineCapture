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
    private readonly List<PendingTransportBoardingOrder> _boardingOrders = new(32);
    private readonly HashSet<int> _reservedBoardingCells = new();
    private readonly HashSet<int> _targetedReservedBoardingCells = new();
    private readonly List<Entity> _passengerSnapshot = new();
    private readonly List<Entity> _remainingPassengers = new();
    private readonly List<Entity> _disembarkingPassengers = new();
    private readonly List<int2> _disembarkCells = new();
    private readonly HashSet<int> _reservedDisembarkCells = new();

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

    public bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitTransportRopeDisembarkCommandSystem ropeDisembarkCommandSystem,
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

            commandRequests.RemoveAt(i);
            handledAny = true;
            RtsSelectionCommandResultElement result = request.Kind switch
            {
                RtsSelectionCommandIntentKind.BoardTransport => ProcessBoardTransportRequest(
                    em,
                    request,
                    transportBoardingQuerySystem,
                    transportBoardingRuleSystem,
                    transportApproachCellSystem,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    selectionStateSystem,
                    tryGetClickedUnitEntity,
                    tryGetClickedCell),
                RtsSelectionCommandIntentKind.BoardSelectedTransport => ProcessBoardSelectedTransportRequest(
                    em,
                    request,
                    transportBoardingQuerySystem,
                    transportBoardingRuleSystem,
                    transportApproachCellSystem,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    tryGetClickedUnitEntity),
                RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                    em,
                    request,
                    transportBoardingQuerySystem,
                    transportBoardingRuleSystem,
                    transportApproachCellSystem,
                    transportAirPickupSystem,
                    moveOrderSystem),
                RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(
                    em,
                    request,
                    transportCapacitySystem,
                    transportApproachCellSystem,
                    ropeDisembarkCommandSystem,
                    moveOrderSystem),
                _ => ProcessDisembarkTransportRequest(
                    em,
                    request,
                    transportCapacitySystem,
                    transportApproachCellSystem,
                    ropeDisembarkCommandSystem,
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

    private static bool IsTransportCommandIntent(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.BoardTransport ||
               kind == RtsSelectionCommandIntentKind.BoardSelectedTransport ||
               kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger ||
               kind == RtsSelectionCommandIntentKind.DisembarkTransport ||
               kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger;
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
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
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
            transportBoardingQuerySystem,
            transportBoardingRuleSystem,
            transportApproachCellSystem,
            transportAirPickupSystem,
            moveOrderSystem,
            selectionStateSystem,
            tryGetClickedUnitEntity,
            tryGetClickedCell);

        return ToBoardingCommandResultElement(request, result);
    }

    private RtsSelectionCommandResultElement ProcessBoardSelectedTransportRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
    {
        Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
        Result result = TryIssueBoardSelectedTransportOrderToClickedPassenger(
            em,
            request.TargetEntity,
            screenPosition,
            transportBoardingQuerySystem,
            transportBoardingRuleSystem,
            transportApproachCellSystem,
            transportAirPickupSystem,
            moveOrderSystem,
            tryGetClickedUnitEntity);

        return ToBoardingCommandResultElement(request, result);
    }

    private RtsSelectionCommandResultElement ProcessBoardSelectedTransportPassengerRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        Result result = request.HasTargetEntity != 0 &&
            request.HasSecondaryTargetEntity != 0
            ? TryIssueBoardSelectedTransportOrderToPassenger(
                em,
                request.TargetEntity,
                request.SecondaryTargetEntity,
                transportBoardingQuerySystem,
                transportBoardingRuleSystem,
                transportApproachCellSystem,
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
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportRopeDisembarkCommandSystem ropeDisembarkCommandSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        bool accepted = request.HasTargetEntity != 0 &&
                        TryDisembarkTransport(em, request.TargetEntity, transportCapacitySystem, transportApproachCellSystem, ropeDisembarkCommandSystem, moveOrderSystem);
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
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportRopeDisembarkCommandSystem ropeDisembarkCommandSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        bool accepted = request.HasTargetEntity != 0 &&
                        request.HasSecondaryTargetEntity != 0 &&
                        TryDisembarkTransportPassenger(
                            em,
                            request.TargetEntity,
                            request.SecondaryTargetEntity,
                            transportCapacitySystem,
                            transportApproachCellSystem,
                            ropeDisembarkCommandSystem,
                            moveOrderSystem);

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
                    _selectedBoardingSourceEntities,
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

        _boardingOrders.Clear();
        _reservedBoardingCells.Clear();
        for (int i = 0; i < selectedCount && _boardingOrders.Count < availableSeats; i++)
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
                    liveUnitEntityArray,
                    liveUnitGridArray,
                    liveUnitFootprintArray,
                    transport,
                    em.GetComponentData<UnitGrid>(transport).Cell,
                    transportSize,
                    _reservedBoardingCells,
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

            _boardingOrders.Add(new PendingTransportBoardingOrder
            {
                Passenger = passenger,
                PassengerCell = referenceCell,
                Goal = goal,
                DirectBoarding = goal.Equals(referenceCell)
            });
            approachCellSystem.ReserveFootprintCells(grid, goal, passengerFootprint, _reservedBoardingCells);
        }

        if (_boardingOrders.Count <= 0)
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
            for (int i = 0; i < _boardingOrders.Count; i++)
            {
                Entity passenger = _boardingOrders[i].Passenger;
                int2 goal = _boardingOrders[i].Goal;
                if (!em.Exists(passenger) || !transportBoardingQuerySystem.IsSoldierBoardingCandidate(em, passenger))
                    continue;

                moveOrderSystem.ClearMovementOrderComponents(em, passenger);
                moveOrderSystem.IssueImmediateMoveCommand(em, passenger, goal);
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
                        $"from={_boardingOrders[i].PassengerCell} goal={goal} direct={(_boardingOrders[i].DirectBoarding ? 1 : 0)} usedCache={(usedCachedSelection ? 1 : 0)} seats={occupiedSeats + i}/{capacity}");
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
        int directBoardingCells = transportBoardingRuleSystem.GetTransportBoardingDirectCells(em, transport);
        _targetedReservedBoardingCells.Clear();
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
                liveUnitEntityArray,
                liveUnitGridArray,
                liveUnitFootprintArray,
                transport,
                em.GetComponentData<UnitGrid>(transport).Cell,
                transportSize,
                _targetedReservedBoardingCells,
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
        moveOrderSystem.IssueImmediateMoveCommand(em, passenger, goal);
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
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _allSelectableQuery.ToArchetypeChunkArray(Allocator.Temp);
        int bestScore = int.MaxValue;
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
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
        }

        return transport != Entity.Null;
    }

    private bool TryDisembarkTransport(
        EntityManager em,
        Entity transport,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportRopeDisembarkCommandSystem ropeDisembarkCommandSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        if (!em.Exists(transport) ||
            !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
            !em.HasComponent<UnitGrid>(transport) ||
            !em.HasComponent<UnitFootprint>(transport) ||
            _gridPathingQuery.IsEmptyIgnoreFilter)
        {
            return false;
        }

        Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
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

        if (ropeDisembarkCommandSystem.IsRopeDisembarkTransport(em, transport))
            return ropeDisembarkCommandSystem.StartRopeDisembarkTransport(em, transport, referenceCell, moveOrderSystem);

        _passengerSnapshot.Clear();
        for (int i = 0; i < passengers.Length; i++)
            _passengerSnapshot.Add(passengers[i].Passenger);
        if (_passengerSnapshot.Count == 0)
            return false;

        passengers.Clear();
        _reservedDisembarkCells.Clear();
        _remainingPassengers.Clear();
        _disembarkingPassengers.Clear();
        _disembarkCells.Clear();
        for (int i = 0; i < _passengerSnapshot.Count; i++)
        {
            Entity passenger = _passengerSnapshot[i];
            if (!em.Exists(passenger))
                continue;

            if (!transportApproachCellSystem.TryFindTransportDisembarkCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    _reservedDisembarkCells,
                    transportCell,
                    transportSize,
                    referenceCell,
                    out int2 cell))
            {
                _remainingPassengers.Add(passenger);
                continue;
            }

            int cellIndex = GridUtils.CellToIndex(cell, grid.Width);
            _reservedDisembarkCells.Add(cellIndex);
            _disembarkingPassengers.Add(passenger);
            _disembarkCells.Add(cell);
        }

        EntityCommandBuffer ecb = new(Allocator.Temp);
        for (int i = 0; i < _disembarkingPassengers.Count; i++)
        {
            Entity passenger = _disembarkingPassengers[i];
            int2 cell = _disembarkCells[i];
            if (!em.Exists(passenger))
                continue;

            moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
            moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
            moveOrderSystem.ClearMovementOrderComponents(em, ecb, passenger);

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

        for (int i = 0; i < _disembarkingPassengers.Count; i++)
        {
            Entity passenger = _disembarkingPassengers[i];
            if (em.Exists(passenger))
                UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
        }

        if (_remainingPassengers.Count > 0 && em.Exists(transport) && em.HasBuffer<UnitTransportPassengerElement>(transport))
        {
            DynamicBuffer<UnitTransportPassengerElement> remainingBuffer = em.GetBuffer<UnitTransportPassengerElement>(transport);
            for (int i = 0; i < _remainingPassengers.Count; i++)
            {
                Entity passenger = _remainingPassengers[i];
                if (em.Exists(passenger))
                    remainingBuffer.Add(new UnitTransportPassengerElement { Passenger = passenger });
            }
        }

        return _disembarkingPassengers.Count > 0 || _remainingPassengers.Count != _passengerSnapshot.Count;
    }

    private bool TryDisembarkTransportPassenger(
        EntityManager em,
        Entity transport,
        Entity passenger,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportRopeDisembarkCommandSystem ropeDisembarkCommandSystem,
        UnitMoveOrderSystem moveOrderSystem)
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

        if (ropeDisembarkCommandSystem.IsRopeDisembarkTransport(em, transport))
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
            if (em.HasComponent<LocalTransform>(transport) && !_gridPathingQuery.IsEmptyIgnoreFilter)
            {
                Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
                GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                ropeReferenceCell = GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);
            }

            return ropeDisembarkCommandSystem.StartRopeDisembarkTransport(em, transport, ropeReferenceCell, moveOrderSystem);
        }

        if (!em.HasComponent<UnitGrid>(transport) ||
            !em.HasComponent<UnitFootprint>(transport) ||
            _gridPathingQuery.IsEmptyIgnoreFilter)
        {
            return false;
        }

        Entity pathingGridEntity = _gridPathingQuery.GetSingletonEntity();
        GridConfig pathingGrid = em.GetComponentData<GridConfig>(pathingGridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(pathingGridEntity).AsNativeArray();
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerComponent>(pathingGridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(pathingGridEntity).Occupied;
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        int2 groundReferenceCell = transportCell;
        if (em.HasComponent<LocalTransform>(transport))
            groundReferenceCell = GridUtils.WorldToCell(pathingGrid, em.GetComponentData<LocalTransform>(transport).Position);

        _reservedDisembarkCells.Clear();
        if (!transportApproachCellSystem.TryFindTransportDisembarkCell(
                pathingGrid,
                walkable,
                blocked,
                occupied,
                _reservedDisembarkCells,
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
        moveOrderSystem.ClearMovementOrderComponents(em, ecb, passenger);

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
