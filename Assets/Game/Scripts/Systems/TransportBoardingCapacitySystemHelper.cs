using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    internal static class TransportBoardingCapacitySystemHelper
    {
        private const int TransportPlaneVehicleMaxFootprintSpan = 3;
        private const int TransportPlaneVehicleMaxFootprintCells = 9;

        public static bool IsBoardablePlayerTransport(EntityManager em, Entity transport)
        {
            return em.Exists(transport) &&
                   new UnitTransportCapacitySystem().TryEnsureTransportCapacity(em, transport) &&
                   em.HasComponent<Faction>(transport) &&
                   FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(transport).Id) &&
                   em.HasComponent<UnitGrid>(transport) &&
                   em.HasComponent<UnitFootprint>(transport) &&
                   em.HasComponent<LocalTransform>(transport);
        }

        public static bool TryResolveBoardingPassengerKind(
            EntityManager em,
            Entity transport,
            Entity passenger,
            out byte passengerKind,
            out int cargoWeight)
        {
            cargoWeight = 0;
            if (IsSoldierBoardingCandidate(em, passenger))
            {
                passengerKind = UnitTransportPassengerKind.Soldier;
                return true;
            }

            if (IsVehicleBoardingCandidateForTransport(em, transport, passenger))
            {
                passengerKind = UnitTransportPassengerKind.Vehicle;
                cargoWeight = ResolveVehicleCargoWeight(em, passenger);
                return true;
            }

            passengerKind = UnitTransportPassengerKind.Soldier;
            return false;
        }

        public static bool HasAvailableTransportBoardingSlot(
            EntityManager em,
            Entity transport,
            byte passengerKind,
            out int occupied,
            out int capacity)
        {
            occupied = CountTransportPassengerOccupancy(em, transport, passengerKind);
            capacity = ResolveTransportPassengerCapacity(em, transport, passengerKind);
            return capacity > occupied;
        }

        public static bool HasAnyAvailableTransportBoardingSlot(EntityManager em, Entity transport)
        {
            return HasAvailableTransportBoardingSlot(em, transport, UnitTransportPassengerKind.Soldier, out _, out _) ||
                   HasAvailableTransportBoardingSlot(em, transport, UnitTransportPassengerKind.Vehicle, out _, out _);
        }

        public static TransportSlotAvailability ResolveTransportSlotAvailability(EntityManager em, Entity transport)
        {
            return new TransportSlotAvailability(
                CountTransportPassengerOccupancy(em, transport, UnitTransportPassengerKind.Soldier),
                ResolveTransportPassengerCapacity(em, transport, UnitTransportPassengerKind.Soldier),
                CountTransportPassengerOccupancy(em, transport, UnitTransportPassengerKind.Vehicle),
                ResolveTransportPassengerCapacity(em, transport, UnitTransportPassengerKind.Vehicle));
        }

        public static bool IsPotentialVehicleCargoPassenger(EntityManager em, Entity entity, bool allowLoadedPassenger = false)
        {
            if (!em.Exists(entity) ||
                !em.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
                !em.HasComponent<UnitGrid>(entity) ||
                !em.HasComponent<UnitMove>(entity) ||
                !em.HasComponent<UnitFootprint>(entity) ||
                !em.HasComponent<UnitMovementBehavior>(entity) ||
                em.HasComponent<UnitAirMovement>(entity) ||
                (!allowLoadedPassenger && em.HasComponent<UnitTransportPassenger>(entity)) ||
                em.HasComponent<RuntimeBuildingCombatTag>(entity) ||
                em.HasComponent<StaticGridBlocker>(entity))
            {
                return false;
            }

            UnitFootprint footprint = em.GetComponentData<UnitFootprint>(entity);
            int2 size = UnitFootprintUtility.ClampSize(footprint.Size);
            if (math.max(size.x, size.y) > TransportPlaneVehicleMaxFootprintSpan ||
                size.x * size.y > TransportPlaneVehicleMaxFootprintCells)
            {
                return false;
            }

            if (!UnitVehicleMovementUtility.IsVehicle(footprint, em.GetComponentData<UnitMovementBehavior>(entity)))
                return false;

            string sourceName = ResolveSourceName(em, entity);
            return sourceName.IndexOf("_Veh_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sourceName.StartsWith("Unit_Veh", System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVehicleBoardingCandidateForTransport(EntityManager em, Entity transport, Entity passenger)
        {
            return IsCargoPlaneTransport(em, transport) &&
                   IsPotentialVehicleCargoPassenger(em, passenger);
        }

        public static bool IsCargoPlaneTransport(EntityManager em, Entity transport)
        {
            if (!em.Exists(transport))
                return false;

            if (em.HasComponent<UnitTransportPlaneDoorReference>(transport))
                return true;

            if (!em.HasComponent<UnitTransportCargoCapacity>(transport) ||
                em.GetComponentData<UnitTransportCargoCapacity>(transport).VehicleCapacity <= 0)
            {
                return false;
            }

            string sourceName = ResolveSourceName(em, transport);
            return new UnitTransportCapacitySystem().IsTransportPlaneName(sourceName);
        }

        public static int ResolveTransportPassengerCapacity(EntityManager em, Entity transport, byte passengerKind)
        {
            if (!em.Exists(transport))
                return 0;

            if (passengerKind == UnitTransportPassengerKind.Vehicle)
            {
                return em.HasComponent<UnitTransportCargoCapacity>(transport)
                    ? math.max(0, em.GetComponentData<UnitTransportCargoCapacity>(transport).VehicleCapacity)
                    : 0;
            }

            int soldierCapacity = em.HasComponent<UnitTransportCapacity>(transport)
                ? math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity)
                : 0;
            if (em.HasComponent<UnitTransportCargoCapacity>(transport))
            {
                UnitTransportCargoCapacity cargoCapacity = em.GetComponentData<UnitTransportCargoCapacity>(transport);
                if (cargoCapacity.SoldierCapacity > 0)
                    soldierCapacity = math.max(0, cargoCapacity.SoldierCapacity);
            }

            return soldierCapacity;
        }

        public static int CountTransportPassengerOccupancy(EntityManager em, Entity transport, byte passengerKind)
        {
            if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
                return 0;

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int count = 0;
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].Passenger;
                if (!em.Exists(passenger))
                    continue;

                byte storedKind = UnitTransportPassengerKind.Soldier;
                if (em.HasComponent<UnitTransportCargoPassenger>(passenger) &&
                    em.GetComponentData<UnitTransportCargoPassenger>(passenger).Transport == transport)
                {
                    storedKind = ResolvePassengerKind(em.GetComponentData<UnitTransportCargoPassenger>(passenger).PassengerKind);
                }
                else if (em.HasComponent<UnitTransportBoardingTarget>(passenger) &&
                         em.GetComponentData<UnitTransportBoardingTarget>(passenger).Transport == transport)
                {
                    storedKind = ResolvePassengerKind(em.GetComponentData<UnitTransportBoardingTarget>(passenger).PassengerKind);
                }
                else if (IsCargoPlaneTransport(em, transport) &&
                         IsPotentialVehicleCargoPassenger(em, passenger, true))
                {
                    storedKind = UnitTransportPassengerKind.Vehicle;
                }

                if (storedKind == passengerKind)
                    count++;
            }

            return count;
        }

        public static byte ResolveLoadedPassengerKind(EntityManager em, Entity transport, Entity passenger)
        {
            if (!em.Exists(passenger))
                return UnitTransportPassengerKind.Soldier;

            if (em.HasComponent<UnitTransportCargoPassenger>(passenger) &&
                em.GetComponentData<UnitTransportCargoPassenger>(passenger).Transport == transport)
            {
                return ResolvePassengerKind(em.GetComponentData<UnitTransportCargoPassenger>(passenger).PassengerKind);
            }

            if (IsCargoPlaneTransport(em, transport) &&
                IsPotentialVehicleCargoPassenger(em, passenger, true))
            {
                return UnitTransportPassengerKind.Vehicle;
            }

            return UnitTransportPassengerKind.Soldier;
        }

        public static void CountLoadedPassengerKinds(
            EntityManager em,
            Entity transport,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            int countLimit,
            out int soldierCount,
            out int vehicleCount)
        {
            soldierCount = 0;
            vehicleCount = 0;
            int count = math.min(countLimit, passengers.Length);
            for (int i = 0; i < count; i++)
            {
                Entity passenger = passengers[i].Passenger;
                byte passengerKind = ResolveLoadedPassengerKind(em, transport, passenger);
                if (passengerKind == UnitTransportPassengerKind.Vehicle)
                    vehicleCount++;
                else
                    soldierCount++;
            }
        }

        public static bool IsSoldierBoardingCandidate(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity) ||
                !em.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
                !em.HasComponent<UnitGrid>(entity) ||
                !em.HasComponent<UnitMove>(entity) ||
                !em.HasComponent<UnitFootprint>(entity) ||
                !em.HasComponent<UnitMovementBehavior>(entity) ||
                em.HasComponent<UnitAirMovement>(entity) ||
                em.HasComponent<UnitTransportPassenger>(entity))
            {
                return false;
            }

            string sourceName = ResolveSourceName(em, entity);
            if (sourceName.IndexOf("_Chr_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                sourceName.StartsWith("Unit_Chr", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (sourceName.IndexOf("_Veh_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                sourceName.StartsWith("Unit_Veh", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !UnitVehicleMovementUtility.IsVehicle(
                em.GetComponentData<UnitFootprint>(entity),
                em.GetComponentData<UnitMovementBehavior>(entity));
        }

        private static int ResolveVehicleCargoWeight(EntityManager em, Entity passenger)
        {
            if (!em.Exists(passenger) || !em.HasComponent<UnitFootprint>(passenger))
                return 0;

            int2 size = UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(passenger).Size);
            return math.max(1, size.x * size.y);
        }

        private static byte ResolvePassengerKind(byte passengerKind)
        {
            return passengerKind == UnitTransportPassengerKind.Vehicle
                ? UnitTransportPassengerKind.Vehicle
                : UnitTransportPassengerKind.Soldier;
        }

        private static string ResolveSourceName(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity))
                return string.Empty;

            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                if (!string.IsNullOrWhiteSpace(sourceName))
                    return sourceName;
            }

            return em.GetName(entity);
        }
    }
}
