using Game.Components;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal enum TransportBoardingPlannedSlotRejectionKind
    {
        None,
        NoSoldierSeats,
        NoVehicleSlots
    }

    internal struct TransportBoardingPlannedSlotCounts
    {
        public int SoldierSeats;
        public int VehicleSlots;

        public TransportBoardingPlannedSlotCounts(int soldierSeats, int vehicleSlots)
        {
            SoldierSeats = soldierSeats;
            VehicleSlots = vehicleSlots;
        }
    }

    internal struct PendingTransportBoardingOrder
    {
        public Entity Passenger;
        public int2 PassengerCell;
        public int2 Goal;
        public byte PassengerKind;
        public int CargoWeight;
        public bool DirectBoarding;
    }

    internal readonly struct BoardAllTransportCandidate : System.IComparable<BoardAllTransportCandidate>
    {
        public readonly Entity Entity;
        public readonly int Distance;

        public BoardAllTransportCandidate(Entity entity, int distance)
        {
            Entity = entity;
            Distance = distance;
        }

        public int CompareTo(BoardAllTransportCandidate other)
        {
            int distanceCompare = Distance.CompareTo(other.Distance);
            return distanceCompare != 0 ? distanceCompare : Entity.Index.CompareTo(other.Entity.Index);
        }
    }

    internal readonly struct TransportSlotAvailability
    {
        public readonly int OccupiedSoldierSeats;
        public readonly int SoldierCapacity;
        public readonly int AvailableSoldierSeats;
        public readonly int OccupiedVehicleSlots;
        public readonly int VehicleCapacity;
        public readonly int AvailableVehicleSlots;

        public TransportSlotAvailability(
            int occupiedSoldierSeats,
            int soldierCapacity,
            int occupiedVehicleSlots,
            int vehicleCapacity)
        {
            OccupiedSoldierSeats = occupiedSoldierSeats;
            SoldierCapacity = soldierCapacity;
            AvailableSoldierSeats = soldierCapacity - occupiedSoldierSeats;
            OccupiedVehicleSlots = occupiedVehicleSlots;
            VehicleCapacity = vehicleCapacity;
            AvailableVehicleSlots = vehicleCapacity - occupiedVehicleSlots;
        }

        public bool HasAnyAvailableSlot => AvailableSoldierSeats > 0 || AvailableVehicleSlots > 0;

        public int TotalAvailableSlots => math.max(1, AvailableSoldierSeats + AvailableVehicleSlots);

        public void GetPassengerKindCounts(byte passengerKind, out int occupiedSlots, out int slotCapacity, out int availableSlots)
        {
            if (passengerKind == UnitTransportPassengerKind.Vehicle)
            {
                occupiedSlots = OccupiedVehicleSlots;
                slotCapacity = VehicleCapacity;
                availableSlots = AvailableVehicleSlots;
                return;
            }

            occupiedSlots = OccupiedSoldierSeats;
            slotCapacity = SoldierCapacity;
            availableSlots = AvailableSoldierSeats;
        }
    }

    internal static class TransportBoardingOrderPlanningSystemHelper
    {
        public static int ResolvePlannedOrderCapacity(int candidateCount, int totalAvailableSlots)
        {
            return math.max(0, math.min(candidateCount, totalAvailableSlots));
        }

        public static List<PendingTransportBoardingOrder> CreatePlannedBoardingOrderList(int capacity)
        {
            return new List<PendingTransportBoardingOrder>(math.max(0, capacity));
        }

        public static PendingTransportBoardingOrder CreatePendingBoardingOrder(
            Entity passenger,
            int2 passengerCell,
            int2 goal,
            byte passengerKind,
            int cargoWeight)
        {
            return new PendingTransportBoardingOrder
            {
                Passenger = passenger,
                PassengerCell = passengerCell,
                Goal = goal,
                PassengerKind = passengerKind,
                CargoWeight = cargoWeight,
                DirectBoarding = goal.Equals(passengerCell)
            };
        }

        public static string ResolveBoardingAcceptedMessage(
            bool cargoPlaneTransport,
            int plannedSoldierSeats,
            int plannedVehicleSlots)
        {
            if (!cargoPlaneTransport)
                return "Boarding transport.";

            if (plannedVehicleSlots > 0 && plannedSoldierSeats > 0)
                return "Loading troops and cargo.";

            return plannedVehicleSlots > 0
                ? "Loading cargo."
                : "Boarding transport plane.";
        }

        public static string ResolveBoardingAcceptedMessage(
            bool cargoPlaneTransport,
            in TransportBoardingPlannedSlotCounts plannedSlots)
        {
            return ResolveBoardingAcceptedMessage(
                cargoPlaneTransport,
                plannedSlots.SoldierSeats,
                plannedSlots.VehicleSlots);
        }

        public static string ResolveBoardingAcceptedMessage(bool cargoPlaneTransport, byte passengerKind)
        {
            if (!cargoPlaneTransport)
                return "Loading transport.";

            return passengerKind == UnitTransportPassengerKind.Vehicle
                ? "Loading cargo."
                : "Boarding transport plane.";
        }

        public static string ResolveBoardAllAcceptedMessage(int orderedCount)
        {
            return orderedCount == 1
                ? "Boarding 1 unit."
                : $"Boarding {orderedCount} units.";
        }

        public static bool HasPlannedBoardingSlot(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            int plannedSoldierSeats,
            int plannedVehicleSlots)
        {
            return passengerKind == UnitTransportPassengerKind.Vehicle
                ? plannedVehicleSlots < availableVehicleSlots
                : plannedSoldierSeats < availableSoldierSeats;
        }

        public static bool HasPlannedBoardingSlot(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            in TransportBoardingPlannedSlotCounts plannedSlots)
        {
            return HasPlannedBoardingSlot(
                passengerKind,
                availableSoldierSeats,
                availableVehicleSlots,
                plannedSlots.SoldierSeats,
                plannedSlots.VehicleSlots);
        }

        public static TransportBoardingPlannedSlotRejectionKind ResolvePlannedSlotRejection(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            int plannedSoldierSeats,
            int plannedVehicleSlots)
        {
            if (HasPlannedBoardingSlot(
                    passengerKind,
                    availableSoldierSeats,
                    availableVehicleSlots,
                    plannedSoldierSeats,
                    plannedVehicleSlots))
            {
                return TransportBoardingPlannedSlotRejectionKind.None;
            }

            return passengerKind == UnitTransportPassengerKind.Vehicle
                ? TransportBoardingPlannedSlotRejectionKind.NoVehicleSlots
                : TransportBoardingPlannedSlotRejectionKind.NoSoldierSeats;
        }

        public static TransportBoardingPlannedSlotRejectionKind ResolvePlannedSlotRejection(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            in TransportBoardingPlannedSlotCounts plannedSlots)
        {
            return ResolvePlannedSlotRejection(
                passengerKind,
                availableSoldierSeats,
                availableVehicleSlots,
                plannedSlots.SoldierSeats,
                plannedSlots.VehicleSlots);
        }

        public static bool TryReservePlannedBoardingSlot(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            ref int plannedSoldierSeats,
            ref int plannedVehicleSlots)
        {
            if (!HasPlannedBoardingSlot(
                    passengerKind,
                    availableSoldierSeats,
                    availableVehicleSlots,
                    plannedSoldierSeats,
                    plannedVehicleSlots))
            {
                return false;
            }

            if (passengerKind == UnitTransportPassengerKind.Vehicle)
                plannedVehicleSlots++;
            else
                plannedSoldierSeats++;

            return true;
        }

        public static bool TryReservePlannedBoardingSlot(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            ref TransportBoardingPlannedSlotCounts plannedSlots)
        {
            if (!HasPlannedBoardingSlot(
                    passengerKind,
                    availableSoldierSeats,
                    availableVehicleSlots,
                    plannedSlots))
            {
                return false;
            }

            if (passengerKind == UnitTransportPassengerKind.Vehicle)
                plannedSlots.VehicleSlots++;
            else
                plannedSlots.SoldierSeats++;

            return true;
        }

        public static bool TryAppendPlannedBoardingOrder(
            List<PendingTransportBoardingOrder> plannedOrders,
            PendingTransportBoardingOrder boardingOrder,
            int availableSoldierSeats,
            int availableVehicleSlots,
            ref TransportBoardingPlannedSlotCounts plannedSlots)
        {
            if (!TryReservePlannedBoardingSlot(
                    boardingOrder.PassengerKind,
                    availableSoldierSeats,
                    availableVehicleSlots,
                    ref plannedSlots))
            {
                return false;
            }

            plannedOrders.Add(boardingOrder);
            return true;
        }

        public static int ResolvePlannedSoldierOccupancy(
            int occupiedSoldierSeats,
            in TransportBoardingPlannedSlotCounts plannedSlots)
        {
            return occupiedSoldierSeats + plannedSlots.SoldierSeats;
        }

        public static int ResolvePlannedVehicleOccupancy(
            int occupiedVehicleSlots,
            in TransportBoardingPlannedSlotCounts plannedSlots)
        {
            return occupiedVehicleSlots + plannedSlots.VehicleSlots;
        }
    }
}
