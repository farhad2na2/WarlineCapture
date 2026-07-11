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
        public Result TryIssueBoardSelectedTransportOrderToClickedPassenger(
            EntityManager em,
            Entity transport,
            Vector2 screenPosition,
            UnitTransportAirPickupSystem airPickupSystem,
            UnitMoveOrderSystem moveOrderSystem,
            TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity)
        {
            EnsureEntityQueries(em);
            bool shouldLogTransportBoarding = TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            if (!TryValidateBoardingTransport(
                    em,
                    transport,
                    shouldLogTransportBoarding,
                    "SelectedTransportNotBoardable",
                    false,
                    out _,
                    out _,
                    out _,
                    out Result rejectedTransportResult))
                return rejectedTransportResult;

            Entity passenger = Entity.Null;
            if (tryGetClickedUnitEntity == null ||
                !tryGetClickedUnitEntity(screenPosition, em, out passenger) ||
                passenger == transport)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=ClickedPassengerNotBoardable passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                return Result.Rejected(TacticalCommandReasonCode.InvalidPassenger);
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
            bool shouldLogTransportBoarding = TransportBoardingDiagnosticSystemHelper.ShouldQueueTransportBoardingDiagnostics(em);
            if (!TryValidateBoardingTransport(
                    em,
                    transport,
                    shouldLogTransportBoarding,
                    "SelectedTransportNotBoardable",
                    false,
                    out bool airTransport,
                    out bool transportLanded,
                    out bool cargoPlaneTransport,
                    out Result rejectedTransportResult))
            {
                return rejectedTransportResult;
            }

            if (passenger == Entity.Null ||
                !em.Exists(passenger) ||
                !TryResolveBoardingPassengerKind(em, transport, passenger, out byte passengerKind, out int cargoWeight) ||
                passenger == transport)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=PassengerNotBoardable passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}");
                return Result.Rejected(TacticalCommandReasonCode.InvalidPassenger);
            }

            TransportSlotAvailability slotAvailability =
                TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em, transport);
            slotAvailability.GetPassengerKindCounts(passengerKind, out int occupiedSlots, out int slotCapacity, out int availableSlots);
            if (availableSlots <= 0)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoSeats transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} kind={passengerKind} slots={occupiedSlots}/{slotCapacity}");
                return Result.Rejected(TacticalCommandReasonCode.TransportFull);
            }

            if (_gridPathingQuery.IsEmptyIgnoreFilter)
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoGridPathing transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)}");
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
                        TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=NoAirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)}");
                    return Result.Rejected(TacticalCommandReasonCode.NoEligiblePassengers);
                }

                transportCell = pendingAirPickupCell;
                hasPendingAirPickupLanding = true;
            }

            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            HashSet<int> targetedReservedBoardingCells = new();
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
                    targetedReservedBoardingCells,
                    directBoardingCells,
                    null,
                    null,
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

                return Result.Rejected(TacticalCommandReasonCode.NoEligiblePassengers);
            }

            if (hasPendingAirPickupLanding)
            {
                airPickupSystem.CommandAirTransportPickup(em, transport, grid, pendingAirPickupCell, moveOrderSystem);
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=AirPickupLanding transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} landing={pendingAirPickupCell}");
            }

            UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
            UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, boardingOrder.Goal);
            var passengerStateSystem = new UnitTransportPassengerStateSystem();
            EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
            try
            {
                passengerStateSystem.ApplyBoardingOrderState(
                    em,
                    ref boardingStateEcb,
                    passenger,
                    transport,
                    boardingOrder.Goal,
                    passengerKind,
                    cargoWeight);
                boardingStateEcb.Playback(em);
            }
            finally
            {
                boardingStateEcb.Dispose();
            }

            if (shouldLogTransportBoarding)
            {
                TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(
                    em,
                    $"[TransportBoard] result=Order passenger={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, passenger)} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} " +
                    $"from={referenceCell} goal={boardingOrder.Goal} kind={passengerKind} direct={boardingOrder.DirectBoarding} selectedTransport=1 slots={occupiedSlots}/{slotCapacity}");
            }

            float3 markerPosition = em.GetComponentData<LocalTransform>(transport).Position;
            return Result.AcceptedAt(
                transportCell,
                markerPosition,
                0,
                TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(cargoPlaneTransport, passengerKind));
        }


    }
}
