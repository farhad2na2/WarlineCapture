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
                request.Kind != RtsSelectionCommandIntentKind.DisembarkTransport)
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
            RtsSelectionCommandResultElement result = request.Kind == RtsSelectionCommandIntentKind.BoardTransport
                ? ProcessBoardTransportRequest(
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
                    tryGetClickedCell)
                : ProcessDisembarkTransportRequest(em, request, transportCapacitySystem, transportApproachCellSystem, ropeDisembarkCommandSystem, moveOrderSystem);
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
            ComponentType.ReadOnly<DynamicBlockerData>(),
            ComponentType.ReadOnly<DynamicOccupancyData>());
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
            HasCommandResult = result.Accepted ? (byte)1 : (byte)0,
            Accepted = result.Accepted ? (byte)1 : (byte)0,
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
        NativeBitArray blocked = em.GetComponentData<DynamicBlockerData>(gridEntity).Blocked;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
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

        for (int i = 0; i < _disembarkingPassengers.Count; i++)
        {
            Entity passenger = _disembarkingPassengers[i];
            int2 cell = _disembarkCells[i];
            if (!em.Exists(passenger))
                continue;

            if (em.HasComponent<Disabled>(passenger))
                em.RemoveComponent<Disabled>(passenger);
            if (em.HasComponent<UnitTransportPassenger>(passenger))
                em.RemoveComponent<UnitTransportPassenger>(passenger);
            if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
                em.RemoveComponent<UnitTransportBoardingTarget>(passenger);
            moveOrderSystem.ClearMovementOrderComponents(em, passenger);

            if (em.HasComponent<UnitGrid>(passenger))
                em.SetComponentData(passenger, new UnitGrid { Cell = cell });
            if (em.HasComponent<LocalTransform>(passenger))
            {
                LocalTransform transform = em.GetComponentData<LocalTransform>(passenger);
                transform.Position = GridUtils.CellToWorldCenter(grid, cell);
                em.SetComponentData(passenger, transform);
            }
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
}
