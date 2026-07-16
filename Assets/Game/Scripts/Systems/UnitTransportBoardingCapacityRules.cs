using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal static class UnitTransportBoardingCapacityRules
    {
        public static byte NormalizePassengerKind(byte passengerKind)
        {
            return passengerKind == UnitTransportPassengerKind.Vehicle
                ? UnitTransportPassengerKind.Vehicle
                : UnitTransportPassengerKind.Soldier;
        }

        public static int ResolveCapacity(
            UnitTransportCapacity transportCapacity,
            bool hasCargoCapacity,
            UnitTransportCargoCapacity cargoCapacity,
            byte passengerKind)
        {
            if (NormalizePassengerKind(passengerKind) == UnitTransportPassengerKind.Vehicle)
                return hasCargoCapacity ? math.max(0, cargoCapacity.VehicleCapacity) : 0;

            int soldierCapacity = math.max(0, transportCapacity.SoldierCapacity);
            if (hasCargoCapacity && cargoCapacity.SoldierCapacity > 0)
                soldierCapacity = math.max(0, cargoCapacity.SoldierCapacity);

            return soldierCapacity;
        }

        public static bool CountsTowardOccupancy(
            Entity transport,
            byte requestedPassengerKind,
            bool passengerExists,
            bool hasCargoPassenger,
            UnitTransportCargoPassenger cargoPassenger,
            bool hasBoardingTarget,
            UnitTransportBoardingTarget boardingTarget)
        {
            if (!passengerExists)
                return false;

            byte storedKind = UnitTransportPassengerKind.Soldier;
            if (hasCargoPassenger && cargoPassenger.Transport == transport)
            {
                storedKind = NormalizePassengerKind(cargoPassenger.PassengerKind);
            }
            else if (hasBoardingTarget && boardingTarget.Transport == transport)
            {
                storedKind = NormalizePassengerKind(boardingTarget.PassengerKind);
            }

            return storedKind == NormalizePassengerKind(requestedPassengerKind);
        }
    }
}
