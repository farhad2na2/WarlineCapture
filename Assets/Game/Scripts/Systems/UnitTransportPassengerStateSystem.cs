using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct UnitTransportPassengerStateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate intentionally omitted: disabled transport helper; boarding/drop systems call methods directly.
            state.Enabled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
        }

        public void ApplyBoardingOrderState(
            EntityManager em,
            ref EntityCommandBuffer ecb,
            Entity passenger,
            Entity transport,
            int2 goal,
            byte passengerKind = UnitTransportPassengerKind.Soldier,
            int cargoWeight = 0)
        {
            if (!em.HasBuffer<UnitTransportHiddenVisualScale>(passenger))
                ecb.AddBuffer<UnitTransportHiddenVisualScale>(passenger);

            EnsureGroundMovementRuntimeComponents(em, ref ecb, passenger);

            var boardingTarget = new UnitTransportBoardingTarget
            {
                Transport = transport,
                Goal = goal,
                PassengerKind = passengerKind,
                CargoWeight = cargoWeight
            };
            if (em.HasComponent<UnitTransportBoardingTarget>(passenger))
                ecb.SetComponent(passenger, boardingTarget);
            else
                ecb.AddComponent(passenger, boardingTarget);

            if (!em.HasComponent<ManualMoveGroupMemberTag>(passenger))
                ecb.AddComponent<ManualMoveGroupMemberTag>(passenger);
        }

        private static void EnsureGroundMovementRuntimeComponents(EntityManager em, ref EntityCommandBuffer ecb, Entity passenger)
        {
            if (!em.Exists(passenger) || em.HasComponent<UnitAirMovement>(passenger))
                return;

            if (!em.HasComponent<UnitVehicleMovement>(passenger))
                ecb.AddComponent(passenger, ResolveFallbackVehicleMovement(em, passenger));

            if (!em.HasComponent<UnitVehicleKinematics>(passenger))
                ecb.AddComponent(passenger, new UnitVehicleKinematics { CurrentSpeed = 0f, StallSeconds = 0f });
        }

        private static UnitVehicleMovement ResolveFallbackVehicleMovement(EntityManager em, Entity passenger)
        {
            UnitFootprint footprint = em.HasComponent<UnitFootprint>(passenger)
                ? em.GetComponentData<UnitFootprint>(passenger)
                : new UnitFootprint { Size = new int2(1, 1) };
            UnitMovementBehavior behavior = em.HasComponent<UnitMovementBehavior>(passenger)
                ? em.GetComponentData<UnitMovementBehavior>(passenger)
                : default;
            bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, behavior);
            float modelLength = math.max(1f, math.max(footprint.Size.x, footprint.Size.y));

            return new UnitVehicleMovement
            {
                TurnSpeedDegrees = isVehicle ? 180f : 720f,
                Acceleration = isVehicle ? math.max(6f, modelLength * 3f) : 999f,
                Braking = isVehicle ? math.max(8f, modelLength * 4f) : 999f,
                RearPivotOffset = isVehicle ? math.max(0.35f, modelLength * 0.22f) : 0f
            };
        }

        public int BoardPassenger(
            EntityManager em,
            ref EntityCommandBuffer ecb,
            DynamicBuffer<UnitTransportPassengerElement> passengers,
            Entity passenger,
            Entity transport,
            byte passengerKind = UnitTransportPassengerKind.Soldier,
            int cargoWeight = 0)
        {
            passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });
            UnitTransportVisualUtility.SetPassengerHidden(em, passenger, ecb);
            ecb.RemoveComponent<UnitTransportBoardingTarget>(passenger);
            RemoveIfPresent<UnitTarget>(ref ecb, em, passenger);
            RemoveIfPresent<UnitPathRequest>(ref ecb, em, passenger);
            RemoveIfPresent<UnitPathFollow>(ref ecb, em, passenger);
            RemoveIfPresent<UnitPathRange>(ref ecb, em, passenger);
            RemoveIfPresent<ManualMoveOrderTag>(ref ecb, em, passenger);
            RemoveIfPresent<ManualMoveGroupMemberTag>(ref ecb, em, passenger);
            RemoveIfPresent<AutoWanderMoveTag>(ref ecb, em, passenger);
            RemoveIfPresent<EngageTarget>(ref ecb, em, passenger);
            RemoveIfPresent<SelectedUnitTag>(ref ecb, em, passenger);
            ecb.AddComponent(passenger, new UnitTransportPassenger { Transport = transport });
            if (passengerKind == UnitTransportPassengerKind.Vehicle)
            {
                var cargoPassenger = new UnitTransportCargoPassenger
                {
                    Transport = transport,
                    PassengerKind = passengerKind,
                    CargoWeight = cargoWeight
                };
                if (em.HasComponent<UnitTransportCargoPassenger>(passenger))
                    ecb.SetComponent(passenger, cargoPassenger);
                else
                    ecb.AddComponent(passenger, cargoPassenger);
            }
            else
            {
                RemoveIfPresent<UnitTransportCargoPassenger>(ref ecb, em, passenger);
            }
            ecb.AddComponent<Disabled>(passenger);
            return passengers.Length;
        }

        private static void RemoveIfPresent<T>(ref EntityCommandBuffer ecb, EntityManager em, Entity entity)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity))
                ecb.RemoveComponent<T>(entity);
        }
    }
}
