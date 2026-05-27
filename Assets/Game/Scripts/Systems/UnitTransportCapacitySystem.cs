using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct UnitTransportCapacitySystem
{
    public bool TryEnsureTransportCapacity(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport))
            return false;

        int capacity = 0;
        if (em.HasComponent<UnitTransportCapacity>(transport))
            capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);

        if (capacity <= 0)
            capacity = ResolveTransportCapacity(em, transport);
        if (capacity <= 0)
            return false;

        if (em.HasComponent<UnitTransportCapacity>(transport))
            em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });
        else
            em.AddComponentData(transport, new UnitTransportCapacity { SoldierCapacity = capacity });

        if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
            em.AddBuffer<UnitTransportPassengerElement>(transport);

        return true;
    }

    public int ResolveTransportCapacity(EntityManager em, Entity entity)
    {
        string sourceName = ResolveSourceName(em, entity);
        return IsPersonnelTransportName(sourceName) ? 10 : 0;
    }

    public bool IsPersonnelTransportName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return false;

        return sourceName.IndexOf("Unit_Veh_APC_Fast", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_Heavy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_Slow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_01", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_APC_02", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_Truck_Canopy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
