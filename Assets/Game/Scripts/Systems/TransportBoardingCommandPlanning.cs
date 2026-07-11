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
        private static bool TryValidateBoardingTransport(
            EntityManager em,
            Entity transport,
            bool shouldLogTransportBoarding,
            string invalidTransportDiagnosticResult,
            bool appendInvalidTransportAirState,
            out bool airTransport,
            out bool transportLanded,
            out bool cargoPlaneTransport,
            out Result rejectedResult)
        {
            airTransport = false;
            transportLanded = false;
            cargoPlaneTransport = false;
            rejectedResult = default;

            if (!IsBoardablePlayerTransport(em, transport))
            {
                if (shouldLogTransportBoarding)
                {
                    string message = $"[TransportBoard] result={invalidTransportDiagnosticResult} transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)}";
                    if (appendInvalidTransportAirState)
                        message += $" {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}";
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, message);
                }

                rejectedResult = Result.Rejected(TacticalCommandReasonCode.InvalidTransport);
                return false;
            }

            airTransport = em.HasComponent<UnitAirMovement>(transport);
            transportLanded = IsTransportLandedForBoarding(em, transport);
            cargoPlaneTransport = IsCargoPlaneTransport(em, transport);
            if (!transportLanded && (!airTransport || cargoPlaneTransport))
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportNotLanded transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                rejectedResult = Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
                return false;
            }

            if (!transportLanded && em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
            {
                if (shouldLogTransportBoarding)
                    TransportBoardingDiagnosticSystemHelper.EnqueueTransportBoardingDiagnostic(em, $"[TransportBoard] result=TransportBusyRopeDisembark transport={TransportBoardingDiagnosticSystemHelper.DescribeTransportBoardingEntity(em, transport)} {TransportBoardingDiagnosticSystemHelper.DescribeTransportAirState(em, transport)}");
                rejectedResult = Result.Rejected(TacticalCommandReasonCode.CommandUnavailable);
                return false;
            }

            return true;
        }

        private static bool TryCreateTransportBoardingGoalOrder(
            EntityManager em,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            Entity transport,
            int2 transportCell,
            int2 boardingTransportSize,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            Entity passenger,
            byte passengerKind,
            int cargoWeight,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            HashSet<int> reservedBoardingCells,
            int directBoardingCells,
            HashSet<Entity> ignoredBoardingEntities,
            HashSet<int> ignoredBoardingOccupiedCells,
            out PendingTransportBoardingOrder order,
            out int2 passengerCell)
        {
            order = default;
            passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            int2 passengerFootprint = em.GetComponentData<UnitFootprint>(passenger).Size;
            byte passengerFaction = em.GetComponentData<Faction>(passenger).Id;
            if (!TryFindTransportBoardingGoal(
                    em,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    boardingTransportSize,
                    passengerCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    transport,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    reservedBoardingCells,
                    directBoardingCells,
                    passengerFaction,
                    ignoredBoardingEntities,
                    ignoredBoardingOccupiedCells,
                    out int2 goal))
            {
                return false;
            }

            order = TransportBoardingOrderPlanningSystemHelper.CreatePendingBoardingOrder(
                passenger,
                passengerCell,
                goal,
                passengerKind,
                cargoWeight);
            ReserveFootprintCells(grid, goal, passengerFootprint, reservedBoardingCells);
            return true;
        }

        private bool TryResolveSelectedBoardTransport(
            EntityManager em,
            SelectionStateCompositionSystemHelper selectionStateSystem,
            out Entity transport)
        {
            transport = Entity.Null;
            EnsureEntityQueries(em);
            if (selectionStateSystem != null &&
                selectionStateSystem.FocusedUnit != Entity.Null &&
                IsBoardablePlayerTransport(em, selectionStateSystem.FocusedUnit))
            {
                transport = selectionStateSystem.FocusedUnit;
                return true;
            }

            if (_selectedTagQuery.IsEmptyIgnoreFilter)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _selectedTagQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity selected = entities[i];
                    if (!IsBoardablePlayerTransport(em, selected))
                        continue;

                    transport = selected;
                    return true;
                }
            }

            return false;
        }

        private bool TryIssueBoardNearestSoldierOrders(
            EntityManager em,
            Entity transport,
            UnitTransportCapacitySystem transportCapacitySystem,
            out int orderedCount)
        {
            orderedCount = 0;
            EnsureEntityQueries(em);
            if (!IsBoardablePlayerTransport(em, transport))
                return false;

            bool transportLanded = IsTransportLandedForBoarding(em, transport);
            if (!transportLanded || !transportCapacitySystem.TryEnsureTransportCapacity(em, transport))
                return false;

            TransportSlotAvailability slotAvailability =
                TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(em, transport);
            if (!slotAvailability.HasAnyAvailableSlot || _gridPathingQuery.IsEmptyIgnoreFilter)
                return false;

            Entity gridEntity = _gridPathingQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            NativeBitArray blocked = blockerData.Blocked;
            NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;

            int liveUnitCount = math.max(1, _pathingLiveUnitsQuery.CalculateEntityCount());
            using NativeList<Entity> liveUnitEntities = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitGrid> liveUnitGrids = new(liveUnitCount, Allocator.Temp);
            using NativeList<UnitFootprint> liveUnitFootprints = new(liveUnitCount, Allocator.Temp);
            CollectPathingLiveUnits(em, liveUnitEntities, liveUnitGrids, liveUnitFootprints);
            NativeArray<Entity> liveUnitEntityArray = liveUnitEntities.AsArray();
            NativeArray<UnitGrid> liveUnitGridArray = liveUnitGrids.AsArray();
            NativeArray<UnitFootprint> liveUnitFootprintArray = liveUnitFootprints.AsArray();

            List<BoardAllTransportCandidate> candidates = new(math.max(1, _boardingCandidateQuery.CalculateEntityCount()));
            CollectNearestBoardingCandidates(em, transport, candidates);
            if (candidates.Count == 0)
                return false;

            List<Entity> candidateEntities = new(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
                candidateEntities.Add(candidates[i].Entity);

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 boardingTransportSize = em.HasComponent<UnitAirMovement>(transport) ? new int2(1, 1) : transportSize;
            int directBoardingCells = GetTransportBoardingDirectCells(em, transport);
            HashSet<int> reservedBoardingCells = new();
            HashSet<Entity> ignoredBoardingEntities = new();
            HashSet<int> ignoredBoardingOccupiedCells = new();
            BuildSelectedBoardingPassengerIgnoreSets(
                em,
                grid,
                transport,
                candidateEntities,
                slotAvailability.AvailableSoldierSeats,
                slotAvailability.AvailableVehicleSlots,
                ignoredBoardingEntities,
                ignoredBoardingOccupiedCells);
            List<PendingTransportBoardingOrder> plannedOrders =
                TransportBoardingOrderPlanningSystemHelper.CreatePlannedBoardingOrderList(
                    TransportBoardingOrderPlanningSystemHelper.ResolvePlannedOrderCapacity(
                        candidates.Count,
                        slotAvailability.TotalAvailableSlots));
            TransportBoardingPlannedSlotCounts plannedSlots = default;

            for (int i = 0; i < candidates.Count; i++)
            {
                Entity passenger = candidates[i].Entity;
                byte passengerKind = default;
                int cargoWeight = 0;
                bool hasPassengerKind =
                    em.Exists(passenger) &&
                    TryResolveBoardingPassengerKind(em, transport, passenger, out passengerKind, out cargoWeight);
                BoardAllTransportBoardingCandidateDecisionKind candidateDecision =
                    TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllTransportCandidateDecision(
                        hasPassengerKind,
                        passengerKind,
                        slotAvailability,
                        plannedSlots);
                if (candidateDecision != BoardAllTransportBoardingCandidateDecisionKind.Accept)
                {
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
                        transportCell,
                        transportSize,
                        passenger,
                        passengerKind,
                        cargoWeight,
                        liveUnitEntityArray,
                        liveUnitGridArray,
                        liveUnitFootprintArray,
                        reservedBoardingCells,
                        directBoardingCells,
                        ignoredBoardingEntities,
                        ignoredBoardingOccupiedCells,
                        out PendingTransportBoardingOrder boardingOrder,
                        out _))
                {
                    continue;
                }

                TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
                    plannedOrders,
                    boardingOrder,
                    slotAvailability,
                    ref plannedSlots);
            }

            if (plannedOrders.Count <= 0)
                return false;

            var passengerStateSystem = new UnitTransportPassengerStateSystem();
            EntityCommandBuffer boardingStateEcb = new(Allocator.Temp);
            try
            {
                for (int i = 0; i < plannedOrders.Count; i++)
                {
                    Entity passenger = plannedOrders[i].Passenger;
                    if (!em.Exists(passenger) ||
                        !IsBoardingCandidateForTransport(em, transport, passenger))
                    {
                        continue;
                    }

                    UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, passenger);
                    UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, plannedOrders[i].Goal);
                    passengerStateSystem.ApplyBoardingOrderState(
                        em,
                        ref boardingStateEcb,
                        passenger,
                        transport,
                        plannedOrders[i].Goal,
                        plannedOrders[i].PassengerKind,
                        plannedOrders[i].CargoWeight);
                    orderedCount++;
                }

                boardingStateEcb.Playback(em);
            }
            finally
            {
                boardingStateEcb.Dispose();
            }

            return orderedCount > 0;
        }

        private void CollectNearestBoardingCandidates(
            EntityManager em,
            Entity transport,
            List<BoardAllTransportCandidate> candidates)
        {
            candidates.Clear();
            EnsureEntityQueries(em);
            if (_boardingCandidateQuery.IsEmptyIgnoreFilter || !em.HasComponent<UnitGrid>(transport))
                return;

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = _boardingCandidateQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity candidate = entities[i];
                    if (candidate == transport ||
                        !IsBoardingCandidateForTransport(em, transport, candidate))
                    {
                        continue;
                    }

                    if (em.HasComponent<UnitTransportBoardingTarget>(candidate) &&
                        em.GetComponentData<UnitTransportBoardingTarget>(candidate).Transport != transport)
                    {
                        continue;
                    }

                    int2 cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                    int distance = math.abs(cell.x - transportCell.x) + math.abs(cell.y - transportCell.y);
                    if (distance > TransportBoardingCommandMaxDistanceCells)
                        continue;

                    candidates.Add(new BoardAllTransportCandidate(candidate, distance));
                }
            }

            candidates.Sort();
        }


    }
}
