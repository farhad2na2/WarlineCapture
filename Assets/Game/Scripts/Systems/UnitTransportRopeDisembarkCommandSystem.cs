using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly struct UnitTransportRopeDisembarkCommandSystem
{
    public bool IsRopeDisembarkTransport(EntityManager em, Entity transport)
    {
        if (!em.Exists(transport) || !em.HasComponent<UnitAirMovement>(transport))
            return false;

        string sourceName = ResolveSourceName(em, transport);
        return sourceName.IndexOf("Unit_Veh_Helicopter_Transport", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool StartRopeDisembarkTransport(
        EntityManager em,
        Entity transport,
        int2 referenceCell,
        UnitMoveOrderSystem moveOrderSystem)
    {
        if (!em.Exists(transport) || !em.HasBuffer<UnitTransportPassengerElement>(transport))
            return false;

        DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
        if (passengers.Length <= 0)
            return false;

        moveOrderSystem.ClearMovementOrderComponents(em, transport);
        if (em.HasComponent<UnitAirMovement>(transport) &&
            em.HasComponent<UnitAirState>(transport) &&
            em.HasComponent<LocalTransform>(transport))
        {
            UnitAirMovement airMovement = em.GetComponentData<UnitAirMovement>(transport);
            UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            if (airState.Airborne == 0)
            {
                transform.Position.y = groundY + math.max(3f, airMovement.CruiseHeight);
                em.SetComponentData(transport, transform);
            }

            airState.ReturningHome = 0;
            airState.Airborne = 1;
            airState.TakeoffRolling = 0;
            airState.LandingRolling = 0;
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            em.SetComponentData(transport, airState);
        }

        UnitTransportRopeDisembarkRequest request = new()
        {
            ReferenceCell = referenceCell,
            NextDropAt = 0f,
            DropIntervalSeconds = 0.8f
        };

        if (em.HasComponent<UnitTransportRopeDisembarkRequest>(transport))
            em.SetComponentData(transport, request);
        else
            em.AddComponentData(transport, request);

        return true;
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
