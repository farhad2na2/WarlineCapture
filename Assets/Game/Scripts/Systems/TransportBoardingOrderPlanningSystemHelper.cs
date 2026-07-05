using Game.Components;

namespace Game.Runtime
{
    internal static class TransportBoardingOrderPlanningSystemHelper
    {
        public static bool TryReservePlannedBoardingSlot(
            byte passengerKind,
            int availableSoldierSeats,
            int availableVehicleSlots,
            ref int plannedSoldierSeats,
            ref int plannedVehicleSlots)
        {
            if (passengerKind == UnitTransportPassengerKind.Vehicle)
            {
                if (plannedVehicleSlots >= availableVehicleSlots)
                    return false;

                plannedVehicleSlots++;
                return true;
            }

            if (plannedSoldierSeats >= availableSoldierSeats)
                return false;

            plannedSoldierSeats++;
            return true;
        }
    }
}
