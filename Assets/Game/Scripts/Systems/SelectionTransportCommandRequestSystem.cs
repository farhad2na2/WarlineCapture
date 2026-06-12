using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class SelectionTransportCommandRequestSystem
{
    private World _queryWorld;
    private EntityQuery _gridPathingQuery;
    private readonly List<Entity> _passengerSnapshot = new();
    private readonly List<Entity> _remainingPassengers = new();
    private readonly List<Entity> _disembarkingPassengers = new();
    private readonly List<int2> _disembarkCells = new();
    private readonly List<RtsSelectionCommandIntentRequestElement> _pendingTransportRequests = new();
    private readonly HashSet<int> _reservedCells = new();

    public bool ProcessPendingRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        TransportBoardingCommandSystem transportBoardingCommandSystem,
        UnitTransportCapacitySystem transportCapacitySystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitTransportRopeDisembarkCommandSystem ropeDisembarkCommandSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem,
        TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TransportBoardingCommandSystem.TryGetClickedCellDelegate tryGetClickedCell)
    {
        _pendingTransportRequests.Clear();
        EnsureEntityQueries(em);
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.BoardTransport &&
                request.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransport &&
                request.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger &&
                request.Kind != RtsSelectionCommandIntentKind.DisembarkTransport &&
                request.Kind != RtsSelectionCommandIntentKind.DisembarkTransportPassenger)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _pendingTransportRequests.Add(request);
        }

        for (int i = 0; i < _pendingTransportRequests.Count; i++)
        {
            RtsSelectionCommandIntentRequestElement request = _pendingTransportRequests[i];
            RtsSelectionCommandResultElement result = request.Kind switch
            {
                RtsSelectionCommandIntentKind.BoardTransport => ProcessBoardTransportRequest(
                    em,
                    request,
                    transportBoardingCommandSystem,
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
                    transportBoardingCommandSystem,
                    transportBoardingQuerySystem,
                    transportBoardingRuleSystem,
                    transportApproachCellSystem,
                    transportAirPickupSystem,
                    moveOrderSystem,
                    tryGetClickedUnitEntity),
                RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger => ProcessBoardSelectedTransportPassengerRequest(
                    em,
                    request,
                    transportBoardingCommandSystem,
                    transportBoardingQuerySystem,
                    transportBoardingRuleSystem,
                    transportApproachCellSystem,
                    transportAirPickupSystem,
                    moveOrderSystem),
                RtsSelectionCommandIntentKind.DisembarkTransportPassenger => ProcessDisembarkTransportPassengerRequest(em, request, transportCapacitySystem, transportApproachCellSystem, ropeDisembarkCommandSystem, moveOrderSystem),
                _ => ProcessDisembarkTransportRequest(em, request, transportCapacitySystem, transportApproachCellSystem, ropeDisembarkCommandSystem, moveOrderSystem)
            };
            AddCommandResult(em, commandEntity, commandResults, result);
        }

        return _pendingTransportRequests.Count > 0;
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

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _gridPathingQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
    }

    private RtsSelectionCommandResultElement ProcessBoardTransportRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        TransportBoardingCommandSystem transportBoardingCommandSystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionStateSystem selectionStateSystem,
        TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        TransportBoardingCommandSystem.TryGetClickedCellDelegate tryGetClickedCell)
    {
        Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
        TransportBoardingCommandSystem.Result result = transportBoardingCommandSystem.TryIssueBoardTransportOrderToClickedUnit(
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

        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetCell = result.MarkerCell,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.MarkerPosition,
            HasCommandResult = 1,
            Accepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = result.Accepted ? 0 : (int)TacticalCommandReasonCode.CommandUnavailable,
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
            HasCommandResult = accepted ? (byte)1 : (byte)0,
            Accepted = accepted ? (byte)1 : (byte)0
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
            HasCommandResult = accepted ? (byte)1 : (byte)0,
            Accepted = accepted ? (byte)1 : (byte)0
        };
    }

    private RtsSelectionCommandResultElement ProcessBoardSelectedTransportRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        TransportBoardingCommandSystem transportBoardingCommandSystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem,
        TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
    {
        Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
        TransportBoardingCommandSystem.Result result = transportBoardingCommandSystem.TryIssueBoardSelectedTransportOrderToClickedPassenger(
            em,
            request.TargetEntity,
            screenPosition,
            transportBoardingQuerySystem,
            transportBoardingRuleSystem,
            transportApproachCellSystem,
            transportAirPickupSystem,
            moveOrderSystem,
            tryGetClickedUnitEntity);

        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetCell = result.MarkerCell,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.MarkerPosition,
            HasCommandResult = 1,
            Accepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = result.Accepted ? 0 : (int)TacticalCommandReasonCode.CommandUnavailable,
            EmitScreenMarker = result.Accepted ? (byte)1 : (byte)0,
            MarkerFactionId = result.MarkerFactionId,
            HasTargetCell = result.Accepted ? (byte)1 : (byte)0,
            HasWorldPosition = result.Accepted ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.Accepted ? (byte)1 : (byte)0
        };
    }

    private RtsSelectionCommandResultElement ProcessBoardSelectedTransportPassengerRequest(
        EntityManager em,
        RtsSelectionCommandIntentRequestElement request,
        TransportBoardingCommandSystem transportBoardingCommandSystem,
        UnitTransportBoardingQuerySystem transportBoardingQuerySystem,
        UnitTransportBoardingRuleSystem transportBoardingRuleSystem,
        UnitTransportApproachCellSystem transportApproachCellSystem,
        UnitTransportAirPickupSystem transportAirPickupSystem,
        UnitMoveOrderSystem moveOrderSystem)
    {
        TransportBoardingCommandSystem.Result result = request.HasTargetEntity != 0 &&
            request.HasSecondaryTargetEntity != 0
            ? transportBoardingCommandSystem.TryIssueBoardSelectedTransportOrderToPassenger(
                em,
                request.TargetEntity,
                request.SecondaryTargetEntity,
                transportBoardingQuerySystem,
                transportBoardingRuleSystem,
                transportApproachCellSystem,
                transportAirPickupSystem,
                moveOrderSystem)
            : TransportBoardingCommandSystem.Result.Rejected();

        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetCell = result.MarkerCell,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.MarkerPosition,
            HasCommandResult = 1,
            Accepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = result.Accepted ? 0 : (int)TacticalCommandReasonCode.CommandUnavailable,
            EmitScreenMarker = result.Accepted ? (byte)1 : (byte)0,
            MarkerFactionId = result.MarkerFactionId,
            HasTargetCell = result.Accepted ? (byte)1 : (byte)0,
            HasWorldPosition = result.Accepted ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.Accepted ? (byte)1 : (byte)0
        };
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
        _reservedCells.Clear();
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
                    _reservedCells,
                    transportCell,
                    transportSize,
                    referenceCell,
                    out int2 cell))
            {
                _remainingPassengers.Add(passenger);
                continue;
            }

            int cellIndex = GridUtils.CellToIndex(cell, grid.Width);
            _reservedCells.Add(cellIndex);
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

        _reservedCells.Clear();
        if (!transportApproachCellSystem.TryFindTransportDisembarkCell(
                pathingGrid,
                walkable,
                blocked,
                occupied,
                _reservedCells,
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
}
