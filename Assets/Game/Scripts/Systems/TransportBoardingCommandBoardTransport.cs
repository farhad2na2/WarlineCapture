using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        public Result TryRequestBoardTransportOrderToClickedUnit(
            EntityManager em,
            Vector2 screenPosition,
            UnitTransportAirPickupSystem airPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            SelectionStateCompositionSystemHelper selectionStateSystem,
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
            SelectionStateCompositionSystemHelper selectionStateSystem)
        {
            EnsureEntityQueries(em);
            selectionStateSystem ??= new SelectionStateCompositionSystemHelper();
            bool shouldLogTransportBoarding = TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            if (!TryValidateBoardingTransport(
                    em,
                    transport,
                    shouldLogTransportBoarding,
                    "TransportNotBoardable",
                    true,
                    out bool airTransport,
                    out bool transportLanded,
                    out bool cargoPlaneTransport,
                    out Result rejectedTransportResult))
            {
                return rejectedTransportResult;
            }

            TransportSlotAvailability slotAvailability =
                TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em, transport);
            if (!slotAvailability.HasAnyAvailableSlot)
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoSeats transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                        $"soldiers={slotAvailability.OccupiedSoldierSeats}/{slotAvailability.SoldierCapacity} vehicles={slotAvailability.OccupiedVehicleSlots}/{slotAvailability.VehicleCapacity}");
                }
                return Result.Rejected(TacticalCommandReasonCode.TransportFull);
            }

            List<Entity> selectedBoardingSourceEntities = new();
            int selectedCount = CollectSelectedBoardingSourceEntities(em, selectionStateSystem, selectedBoardingSourceEntities, out int selectedTagCount, out int selectedMoveCount, out bool usedCachedSelection);
            if (selectedCount == 0)
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoSelectedPassengers transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                        $"soldiers={slotAvailability.OccupiedSoldierSeats}/{slotAvailability.SoldierCapacity} vehicles={slotAvailability.OccupiedVehicleSlots}/{slotAvailability.VehicleCapacity} " +
                        $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} cached={selectionStateSystem.CachedSelectedMoveEntities.Count}");
                }

                return Result.Rejected(TacticalCommandReasonCode.InvalidPassenger);
            }

            if (_gridPathingQuery.IsEmptyIgnoreFilter)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoGridPathing transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} usedCache={(usedCachedSelection ? 1 : 0)}");
                return Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
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
            int2 originalTransportCell = transportCell;
            int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
            using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
            CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
            NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
            NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
            NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();
            HashSet<Entity> ignoredSelectedBoardingEntities = new();
            HashSet<int> ignoredSelectedBoardingOccupiedCells = new();
            BuildSelectedBoardingPassengerIgnoreSets(
                em,
                grid,
                transport,
                selectedBoardingSourceEntities,
                slotAvailability.AvailableSoldierSeats,
                slotAvailability.AvailableVehicleSlots,
                ignoredSelectedBoardingEntities,
                ignoredSelectedBoardingOccupiedCells);

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
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} selected={selectedCount}");
                    return Result.Rejected(
                        TacticalCommandReasonCode.NoEligiblePassengers,
                        "No landing zone for selected transport.");
                }

                transportCell = pendingAirPickupCell;
                hasPendingAirPickupLanding = true;
            }

            List<PendingTransportBoardingOrder> boardingOrders =
                TransportBoardingOrderPlanningSystemHelper.CreatePlannedBoardingOrderList(DefaultBoardingOrderCapacity);
            HashSet<int> reservedBoardingCells = new();
            TransportBoardingPlannedSlotCounts plannedSlots = default;
            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            for (int i = 0; i < selectedCount; i++)
            {
                Entity passenger = selectedBoardingSourceEntities[i];
                byte passengerKind = default;
                int cargoWeight = 0;
                bool hasPassengerKind =
                    passenger != transport &&
                    TryResolveBoardingPassengerKind(em, transport, passenger, out passengerKind, out cargoWeight);
                SelectedTransportBoardingCandidateDecisionKind candidateDecision =
                    TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                        passenger,
                        transport,
                        hasPassengerKind,
                        passengerKind,
                        slotAvailability,
                        plannedSlots);
                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipTransport)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=IsTransport passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                    continue;
                }

                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipNotBoardingCandidate)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NotBoardingCandidate passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                    continue;
                }

                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipNoVehicleSlots)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NoVehicleSlots passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} vehicles={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedVehicleOccupancy(slotAvailability.OccupiedVehicleSlots, plannedSlots)}/{slotAvailability.VehicleCapacity}");
                    continue;
                }

                if (candidateDecision == SelectedTransportBoardingCandidateDecisionKind.SkipNoSoldierSeats)
                {
                    if (shouldLogTransportBoarding)
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=SkipPassenger reason=NoSoldierSeats passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} soldiers={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSoldierOccupancy(slotAvailability.OccupiedSoldierSeats, plannedSlots)}/{slotAvailability.SoldierCapacity}");

                    continue;
                }

                if (!TryCreateTransportBoardingGoalOrder(
                        em,
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        transport,
                        transportCell,
                        boardingTransportSize,
                        originalTransportCell,
                        transportSize,
                        passenger,
                        passengerKind,
                        cargoWeight,
                        liveUnitEntityArray,
                        liveUnitGridArray,
                        liveUnitFootprintArray,
                        reservedBoardingCells,
                        directBoardingCells,
                        ignoredSelectedBoardingEntities,
                        ignoredSelectedBoardingOccupiedCells,
                        out PendingTransportBoardingOrder boardingOrder,
                        out int2 referenceCell))
                {
                    if (shouldLogTransportBoarding)
                    {
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                            em,
                            $"[TransportBoard] result=NoApproach passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                            $"passengerCell={referenceCell} transportCell={transportCell} transportSize={boardingTransportSize} directCells={directBoardingCells}");
                    }

                    continue;
                }

                TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
                    boardingOrders,
                    boardingOrder,
                    slotAvailability,
                    ref plannedSlots);
            }

            if (boardingOrders.Count <= 0)
            {
                if (shouldLogTransportBoarding)
                {
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                        em,
                        $"[TransportBoard] result=NoBoardingOrders transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} selected={selectedCount} " +
                        $"selectedTag={selectedTagCount} selectedMove={selectedMoveCount} usedCache={(usedCachedSelection ? 1 : 0)} " +
                        $"soldiers={slotAvailability.OccupiedSoldierSeats}/{slotAvailability.SoldierCapacity} vehicles={slotAvailability.OccupiedVehicleSlots}/{slotAvailability.VehicleCapacity}");
                }

                return Result.Rejected(
                    TacticalCommandReasonCode.NoEligiblePassengers,
                    "No boarding path to selected transport.");
            }

            if (hasPendingAirPickupLanding)
            {
                airPickupSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, moveOrderSystem);
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=AirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
            }

            var passengerStateSystem = new UnitTransportPassengerStateSystem();
            EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
            try
            {
                for (int i = 0; i < boardingOrders.Count; i++)
                {
                    Entity passenger = boardingOrders[i].Passenger;
                    int2 goal = boardingOrders[i].Goal;
                    if (!em.Exists(passenger) ||
                        !IsBoardingCandidateForTransport(em, transport, passenger))
                    {
                        continue;
                    }

                    UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
                    UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, goal);
                    passengerStateSystem.ApplyBoardingOrderState(
                        em,
                        ref boardingStateEcb,
                        passenger,
                        transport,
                        goal,
                        boardingOrders[i].PassengerKind,
                        boardingOrders[i].CargoWeight);

                    if (shouldLogTransportBoarding)
                    {
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                            em,
                            $"[TransportBoard] result=Order passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                            $"from={boardingOrders[i].PassengerCell} goal={goal} kind={boardingOrders[i].PassengerKind} direct={(boardingOrders[i].DirectBoarding ? 1 : 0)} usedCache={(usedCachedSelection ? 1 : 0)} " +
                            $"soldiers={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSoldierOccupancy(slotAvailability.OccupiedSoldierSeats, plannedSlots)}/{slotAvailability.SoldierCapacity} vehicles={TransportBoardingOrderPlanningSystemHelper.ResolvePlannedVehicleOccupancy(slotAvailability.OccupiedVehicleSlots, plannedSlots)}/{slotAvailability.VehicleCapacity}");
                    }
                }

                boardingStateEcb.Playback(em);
            }
            finally
            {
                boardingStateEcb.Dispose();
            }

            float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
            return Result.AcceptedAt(
                transportCell,
                markerPosition,
                0,
                TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(cargoPlaneTransport, plannedSlots));
        }


    }
}
