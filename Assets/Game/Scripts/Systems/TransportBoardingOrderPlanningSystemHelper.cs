using Game.Components;

namespace Game.Runtime
{
    internal static class TransportBoardingOrderPlanningSystemHelper
    {
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

        public static string ResolveBoardingAcceptedMessage(bool cargoPlaneTransport, byte passengerKind)
        {
            if (!cargoPlaneTransport)
                return "Loading transport.";

            return passengerKind == UnitTransportPassengerKind.Vehicle
                ? "Loading cargo."
                : "Boarding transport plane.";
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
    }
}
