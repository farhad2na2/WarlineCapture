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
        private static bool CanStartPlaneAirdrop(EntityManager em, Entity transport, out TacticalCommandReasonCode reasonCode)
        {
            reasonCode = TacticalCommandReasonCode.None;
            if (!em.Exists(transport) ||
                !IsCargoPlaneTransport(em, transport) ||
                !em.HasComponent<UnitAirComponent>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                reasonCode = TacticalCommandReasonCode.InvalidTransport;
                return false;
            }

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
            if (airState.TakeoffRolling != 0 || airState.LandingRolling != 0)
            {
                reasonCode = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            if (airState.Airborne == 0 && !IsTransportLandedForBoarding(em, transport))
            {
                if (!IsTransportPhysicallyAirborneForAirdrop(em, transport, airState))
                {
                    reasonCode = TacticalCommandReasonCode.CommandUnavailable;
                    return false;
                }
            }

            return true;
        }

        private static bool IsTransportPhysicallyAirborneForAirdrop(EntityManager em, Entity transport, UnitAirComponent airState)
        {
            if (!em.HasComponent<LocalTransform>(transport))
                return false;

            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            return transform.Position.y > groundY + TransportBoardingData.AirBoardingGroundedHeightTolerance;
        }

        private static bool TryValidatePlaneAirdropPassengers(
            EntityManager em,
            Entity transport,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            int dropCount,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            int2 dropReferenceCell,
            out TacticalCommandReasonCode reasonCode,
            out string message)
        {
            reasonCode = TacticalCommandReasonCode.None;
            message = null;
            int validatedCount = 0;
            int count = math.min(dropCount, passengers.Length);
            for (int i = 0; i < passengers.Length && validatedCount < count; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (!em.Exists(passenger))
                    continue;

                if (!TryValidatePlaneAirdropPassenger(
                        em,
                        transport,
                        passenger,
                        validatedCount,
                        grid,
                        walkable,
                        blocked,
                        occupied,
                        dropReferenceCell,
                        out reasonCode,
                        out message))
                {
                    return false;
                }

                validatedCount++;
            }

            if (validatedCount > 0)
                return true;

            reasonCode = TacticalCommandReasonCode.TransportPassengerMissing;
            return false;
        }

        private static bool TryValidatePlaneAirdropPassenger(
            EntityManager em,
            Entity transport,
            Entity passenger,
            int dropOrdinal,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            int2 dropReferenceCell,
            out TacticalCommandReasonCode reasonCode,
            out string message)
        {
            reasonCode = TacticalCommandReasonCode.None;
            message = null;
            byte passengerKind = TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(em, transport, passenger);
            if (!UnitTransportAirdropSystem.HasResolvableDropVisualPrefab(em, transport, passengerKind))
            {
                reasonCode = TacticalCommandReasonCode.CommandUnavailable;
                message = passengerKind == UnitTransportPassengerKind.Vehicle
                    ? GameText.Get("tactical.airdrop.emergency_drop_visual_missing", "Emergency drop visual missing.")
                    : GameText.Get("tactical.airdrop.parachute_visual_missing", "Parachute visual missing.");
                return false;
            }

            int2 passengerFootprint = em.HasComponent<UnitFootprint>(passenger)
                ? em.GetComponentData<UnitFootprint>(passenger).Size
                : new int2(1, 1);
            if (UnitTransportAirdropSystem.TryFindLandingCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    dropReferenceCell,
                    passengerFootprint,
                    dropOrdinal + passenger.Index,
                    out _))
            {
                return true;
            }

            reasonCode = TacticalCommandReasonCode.TargetBlocked;
            message = GameText.Get("tactical.airdrop.no_clear_landing_zone", "No clear airdrop landing zone.");
            return false;
        }

        private static bool TryValidateAirdropReferenceCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            int2 dropReferenceCell,
            out TacticalCommandReasonCode reasonCode)
        {
            reasonCode = TacticalCommandReasonCode.None;
            if (!GridUtils.InBounds(dropReferenceCell, grid.Width, grid.Height))
            {
                reasonCode = TacticalCommandReasonCode.TargetOutOfBounds;
                return false;
            }

            int index = GridUtils.CellToIndex(dropReferenceCell, grid.Width);
            if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
            {
                reasonCode = TacticalCommandReasonCode.TargetBlocked;
                return false;
            }

            return true;
        }

        private static string ResolveAirdropRejectedMessage(TacticalCommandReasonCode reasonCode)
        {
            return reasonCode == TacticalCommandReasonCode.TargetBlocked
                ? GameText.Get("tactical.airdrop.cargo_drop_blocked", "Cargo drop blocked.")
                : ResolveReasonText(reasonCode);
        }

        private static string ResolveReasonText(TacticalCommandReasonCode reasonCode)
        {
            return GameText.Get(
                TacticalCommandFeedbackText.ToDisplayTextKey(reasonCode),
                TacticalCommandFeedbackText.ToDisplayText(reasonCode));
        }

        private static void SetPlaneAirdropRequest(
            EntityManager em,
            Entity transport,
            int2 dropReferenceCell,
            int soldierDropCount,
            int vehicleDropCount)
        {
            int totalDropCount = soldierDropCount + vehicleDropCount;
            byte dropMode = soldierDropCount > 0 && vehicleDropCount > 0
                ? UnitTransportAirdropMode.Mixed
                : vehicleDropCount > 0
                    ? UnitTransportAirdropMode.VehicleOnly
                    : UnitTransportAirdropMode.SoldierOnly;
            UnitTransportAirdropRequest request = new()
            {
                DropReferenceCell = dropReferenceCell,
                NextDropAt = 0f,
                DropIntervalSeconds = 0.65f,
                DropCount = totalDropCount,
                SoldierDropCount = soldierDropCount,
                VehicleDropCount = vehicleDropCount,
                DropMode = dropMode
            };

            if (em.HasComponent<UnitTransportAirdropRequest>(transport))
                em.SetComponentData(transport, request);
            else
                em.AddComponentData(transport, request);

            if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
                em.RemoveComponent<UnitTransportRopeDisembarkRequest>(transport);

            if (em.HasComponent<UnitAirComponent>(transport))
            {
                UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
                airState.ReturningHome = 0;
                airState.AttackRunActive = 0;
                airState.ReturnApproachInitialized = 0;
                em.SetComponentData(transport, airState);
            }
        }

        private static bool TryPlanPassengerDisembarkCells(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            HashSet<int> reservedDisembarkCells,
            bool usePlaneRampDisembark,
            int2 referenceCell,
            int2 transportCell,
            int2 transportSize,
            int2 passengerFootprint,
            out int2 disembarkCell,
            out int2 rolloutCell)
        {
            bool foundDisembarkCell = usePlaneRampDisembark
                ? TransportBoardingApproachSystemHelper.TryFindPlaneRampDisembarkCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    referenceCell,
                    passengerFootprint,
                    out disembarkCell)
                : TryFindTransportDisembarkCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    transportCell,
                    transportSize,
                    referenceCell,
                    out disembarkCell);
            if (!foundDisembarkCell)
            {
                rolloutCell = default;
                return false;
            }

            ReserveFootprintCells(grid, disembarkCell, passengerFootprint, reservedDisembarkCells);
            rolloutCell = disembarkCell;
            if (usePlaneRampDisembark &&
                TransportBoardingApproachSystemHelper.TryFindPlaneRampRolloutCell(
                    grid,
                    walkable,
                    blocked,
                    occupied,
                    reservedDisembarkCells,
                    referenceCell,
                    transportCell,
                    passengerFootprint,
                    out int2 candidateRolloutCell))
            {
                rolloutCell = candidateRolloutCell;
                ReserveFootprintCells(grid, rolloutCell, passengerFootprint, reservedDisembarkCells);
            }

            return true;
        }


    }
}
