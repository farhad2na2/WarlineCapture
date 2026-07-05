using Game.Components;

namespace Game.Runtime
{
    internal static class TransportBoardingOrderPlanningSystemHelper
    {
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
