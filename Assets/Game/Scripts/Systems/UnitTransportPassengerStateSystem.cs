using Unity.Entities;

public readonly struct UnitTransportPassengerStateSystem
{
    public int BoardPassenger(
        EntityManager em,
        ref EntityCommandBuffer ecb,
        DynamicBuffer<UnitTransportPassengerElement> passengers,
        Entity passenger,
        Entity transport)
    {
        passengers.Add(new UnitTransportPassengerElement { Passenger = passenger });
        UnitTransportVisualUtility.SetPassengerHidden(em, passenger, ecb);
        ecb.RemoveComponent<UnitTransportBoardingTarget>(passenger);
        RemoveIfPresent<UnitTarget>(ref ecb, em, passenger);
        RemoveIfPresent<UnitPathRequest>(ref ecb, em, passenger);
        RemoveIfPresent<UnitPathFollow>(ref ecb, em, passenger);
        RemoveIfPresent<UnitPathRange>(ref ecb, em, passenger);
        RemoveIfPresent<ManualMoveOrderTag>(ref ecb, em, passenger);
        RemoveIfPresent<AutoWanderMoveTag>(ref ecb, em, passenger);
        RemoveIfPresent<EngageTarget>(ref ecb, em, passenger);
        RemoveIfPresent<SelectedUnitTag>(ref ecb, em, passenger);
        ecb.AddComponent(passenger, new UnitTransportPassenger { Transport = transport });
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
