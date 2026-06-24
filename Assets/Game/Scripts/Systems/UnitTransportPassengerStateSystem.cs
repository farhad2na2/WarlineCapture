using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportPassengerStateSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
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
