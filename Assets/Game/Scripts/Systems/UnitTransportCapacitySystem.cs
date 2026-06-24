using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportCapacitySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public bool TryEnsureTransportCapacity(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport))
            return false;

        UnitTransportCargoCapacity cargoCapacity = ResolveTransportCargoCapacity(em, transport);
        if (!HasAnyCapacity(cargoCapacity))
            return false;

        int soldierCapacity = math.max(0, cargoCapacity.SoldierCapacity);

        if (em.HasComponent<UnitTransportCapacity>(transport))
            em.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = soldierCapacity });
        else
            em.AddComponentData(transport, new UnitTransportCapacity { SoldierCapacity = soldierCapacity });

        if (cargoCapacity.VehicleCapacity > 0 || cargoCapacity.CargoWeightCapacity > 0)
        {
            if (em.HasComponent<UnitTransportCargoCapacity>(transport))
                em.SetComponentData(transport, cargoCapacity);
            else
                em.AddComponentData(transport, cargoCapacity);
        }

        if (!em.HasBuffer<UnitTransportPassengerElement>(transport))
            em.AddBuffer<UnitTransportPassengerElement>(transport);

        return true;
    }

    public int ResolveTransportCapacity(EntityManager em, Entity entity)
    {
        return math.max(0, ResolveTransportCargoCapacity(em, entity).SoldierCapacity);
    }

    public UnitTransportCargoCapacity ResolveTransportCargoCapacity(EntityManager em, Entity entity)
    {
        UnitTransportCargoCapacity capacity = default;

        if (em.Exists(entity) && em.HasComponent<UnitTransportCargoCapacity>(entity))
        {
            capacity = em.GetComponentData<UnitTransportCargoCapacity>(entity);
            capacity.SoldierCapacity = math.max(0, capacity.SoldierCapacity);
            capacity.VehicleCapacity = math.max(0, capacity.VehicleCapacity);
            capacity.CargoWeightCapacity = math.max(0, capacity.CargoWeightCapacity);
        }

        if (em.Exists(entity) && em.HasComponent<UnitTransportCapacity>(entity))
        {
            int soldierCapacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity);
            if (soldierCapacity > capacity.SoldierCapacity)
                capacity.SoldierCapacity = soldierCapacity;
        }

        if (HasAnyCapacity(capacity))
            return capacity;

        string sourceName = ResolveSourceName(em, entity);
        if (IsTransportPlaneName(sourceName))
        {
            return new UnitTransportCargoCapacity
            {
                SoldierCapacity = 24,
                VehicleCapacity = 2,
                CargoWeightCapacity = 0
            };
        }

        return IsPersonnelTransportName(sourceName)
            ? new UnitTransportCargoCapacity { SoldierCapacity = 10 }
            : default;
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

    public bool IsTransportPlaneName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            return false;

        return sourceName.IndexOf("Unit_Veh_Plane_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               sourceName.IndexOf("SM_Veh_TransportPlane", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool HasAnyCapacity(UnitTransportCargoCapacity capacity)
    {
        return math.max(0, capacity.SoldierCapacity) > 0 ||
               math.max(0, capacity.VehicleCapacity) > 0 ||
               math.max(0, capacity.CargoWeightCapacity) > 0;
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
