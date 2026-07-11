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
        private static DisembarkResult TryDisembarkTransport(
            EntityManager em,
            Entity transport,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem,
            EntityQuery gridPathingQuery,
            int2 requestedDropCell,
            byte hasRequestedDropCell,
            List<Entity> allowedPassengers = null)
        {
            if (!em.Exists(transport) ||
                !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                gridPathingQuery.IsEmptyIgnoreFilter)
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport);
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
            {
                int totalDropCount = 0;
                if (allowedPassengers != null)
                {
                    totalDropCount = FilterTransportPassengerBuffer(
                        em,
                        passengers,
                        allowedPassengers);
                    if (totalDropCount <= 0)
                        return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);
                }

                return StartRopeDisembarkTransport(em, transport, referenceCell, moveOrderSystem, totalDropCount)
                    ? DisembarkResult.Success()
                    : DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
            }

            bool cargoPlaneTransport = IsCargoPlaneTransport(em, transport);
            if (cargoPlaneTransport && (hasRequestedDropCell != 0 || !IsTransportLandedForBoarding(em, transport)))
            {
                int maxDropCount = passengers.Length;
                if (allowedPassengers != null)
                {
                    maxDropCount = FilterTransportPassengerBuffer(
                        em,
                        passengers,
                        allowedPassengers);
                }

                return TryStartPlaneAirdrop(
                    em,
                    transport,
                    passengers,
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    referenceCell,
                    requestedDropCell,
                    hasRequestedDropCell,
                    maxDropCount);
            }

            int2 rampReferenceCell = default;
            bool usePlaneRampDisembark = cargoPlaneTransport &&
                                         TryResolvePlaneRampApproachCell(
                                             em,
                                             grid,
                                             transport,
                                             out rampReferenceCell);
            if (usePlaneRampDisembark)
                referenceCell = rampReferenceCell;

            List<Entity> passengerSnapshot = new(passengers.Length);
            List<Entity> deferredPassengers = allowedPassengers != null ? new List<Entity>() : null;
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (allowedPassengers == null || ContainsEntity(allowedPassengers, passenger))
                    passengerSnapshot.Add(passenger);
                else
                    deferredPassengers.Add(passenger);
            }
            if (passengerSnapshot.Count == 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

            passengers.Clear();
            HashSet<int> reservedDisembarkCells = new();
            List<Entity> remainingPassengers = deferredPassengers ?? new List<Entity>();
            List<Entity> disembarkingPassengers = new();
            List<int2> disembarkCells = new();
            List<int2> rolloutCells = new();
            for (int i = 0; i < passengerSnapshot.Count; i++)
            {
                Entity passenger = passengerSnapshot[i];
                if (!em.Exists(passenger))
                    continue;

                int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
                    ? em.GetComponentData<UnitFootprint>(passenger).Size
                    : new int2(1, 1);
                if (!TryPlanPassengerDisembarkCells(
                        grid,
                        walkable,
                        blocked,
                        occupied,
                        reservedDisembarkCells,
                        usePlaneRampDisembark,
                        referenceCell,
                        transportCell,
                        transportSize,
                        passengerFootprint,
                        out int2 cell,
                        out int2 rolloutCell))
                {
                    remainingPassengers.Add(passenger);
                    continue;
                }

                disembarkingPassengers.Add(passenger);
                disembarkCells.Add(cell);
                rolloutCells.Add(rolloutCell);
            }

            if (usePlaneRampDisembark && disembarkingPassengers.Count > 0)
                RequestPlaneDoorOpen(em, transport);

            EntityCommandBuffer ecb = new(Allocator.Temp);
            for (int i = 0; i < disembarkingPassengers.Count; i++)
            {
                Entity passenger = disembarkingPassengers[i];
                int2 cell = disembarkCells[i];
                if (!em.Exists(passenger))
                    continue;

                moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
                moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
                moveOrderSystem.RemoveComponentIfPresent<UnitTransportCargoPassenger>(em, ecb, passenger);
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
                {
                    UnitTransportVisualUtility.SetPassengerVisible(em, passenger, true);
                    if (usePlaneRampDisembark &&
                        i < rolloutCells.Count &&
                        !rolloutCells[i].Equals(disembarkCells[i]))
                    {
                        UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, rolloutCells[i]);
                    }
                }
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

            return disembarkingPassengers.Count > 0 || remainingPassengers.Count != passengerSnapshot.Count
                ? DisembarkResult.Success()
                : DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
        }

        private static DisembarkResult TryDisembarkTransportPassenger(
            EntityManager em,
            Entity transport,
            Entity passenger,
            UnitTransportCapacitySystem transportCapacitySystem,
            UnitMoveOrderSystem moveOrderSystem,
            EntityQuery gridPathingQuery,
            int2 requestedDropCell,
            byte hasRequestedDropCell)
        {
            if (!em.Exists(transport) ||
                !em.Exists(passenger) ||
                !transportCapacitySystem.TryEnsureTransportCapacity(em, transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport))
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport);
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int passengerIndex = IndexOfPassenger(passengers, passenger);
            if (passengerIndex < 0)
                return DisembarkResult.Rejected(TacticalCommandReasonCode.TransportPassengerMissing);

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

                return StartRopeDisembarkTransport(em, transport, ropeReferenceCell, moveOrderSystem)
                    ? DisembarkResult.Success()
                    : DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
            }

            if (!em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                gridPathingQuery.IsEmptyIgnoreFilter)
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.InvalidTransport);
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
            bool cargoPlaneTransport = IsCargoPlaneTransport(em, transport);
            if (cargoPlaneTransport && (hasRequestedDropCell != 0 || !IsTransportLandedForBoarding(em, transport)))
            {
                if (!CanStartPlaneAirdrop(em, transport, out TacticalCommandReasonCode reasonCode))
                    return DisembarkResult.Rejected(reasonCode);

                int2 dropReferenceCell = hasRequestedDropCell != 0 ? requestedDropCell : groundReferenceCell;
                if (!TryValidateAirdropReferenceCell(pathingGrid, walkable, dropReferenceCell, out TacticalCommandReasonCode dropCellReason))
                    return DisembarkResult.Rejected(dropCellReason, message: ResolveAirdropRejectedMessage(dropCellReason));

                if (passengerIndex > 0)
                {
                    UnitTransportPassengerElement selected = passengers[passengerIndex];
                    passengers.RemoveAt(passengerIndex);
                    passengers.Insert(0, selected);
                }

                byte passengerKind = TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(em, transport, passenger);
                if (!TryValidatePlaneAirdropPassenger(
                        em,
                        transport,
                        passenger,
                        0,
                        pathingGrid,
                        walkable,
                        blocked,
                        occupied,
                        dropReferenceCell,
                        out TacticalCommandReasonCode airdropReason,
                        out string airdropMessage))
                {
                    return DisembarkResult.Rejected(airdropReason, message: airdropMessage);
                }

                int soldierDropCount = passengerKind == UnitTransportPassengerKind.Vehicle ? 0 : 1;
                int vehicleDropCount = passengerKind == UnitTransportPassengerKind.Vehicle ? 1 : 0;
                SetPlaneAirdropRequest(em, transport, dropReferenceCell, soldierDropCount, vehicleDropCount);
                RequestPlaneDoorOpen(em, transport);
                return DisembarkResult.Success("Airdrop in progress.");
            }

            int2 rampReferenceCell = default;
            bool usePlaneRampDisembark = cargoPlaneTransport &&
                                         TryResolvePlaneRampApproachCell(
                                             em,
                                             pathingGrid,
                                             transport,
                                             out rampReferenceCell);
            if (usePlaneRampDisembark)
                groundReferenceCell = rampReferenceCell;

            int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
                ? em.GetComponentData<UnitFootprint>(passenger).Size
                : new int2(1, 1);
            if (!TryPlanPassengerDisembarkCells(
                    pathingGrid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    usePlaneRampDisembark,
                    groundReferenceCell,
                    transportCell,
                    transportSize,
                    passengerFootprint,
                    out int2 cell,
                    out int2 rolloutCell))
            {
                return DisembarkResult.Rejected(TacticalCommandReasonCode.NoDisembarkCell);
            }

            passengers.RemoveAt(passengerIndex);
            if (usePlaneRampDisembark)
                RequestPlaneDoorOpen(em, transport);

            EntityCommandBuffer ecb = new(Allocator.Temp);
            moveOrderSystem.RemoveComponentIfPresent<Disabled>(em, ecb, passenger);
            moveOrderSystem.RemoveComponentIfPresent<UnitTransportPassenger>(em, ecb, passenger);
            moveOrderSystem.RemoveComponentIfPresent<UnitTransportCargoPassenger>(em, ecb, passenger);
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
            if (usePlaneRampDisembark && !rolloutCell.Equals(cell))
                UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, passenger, rolloutCell);
            return DisembarkResult.Success();
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
                   FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id);
        }

    }
}
