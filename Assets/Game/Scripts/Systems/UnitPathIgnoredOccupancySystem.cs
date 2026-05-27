using Unity.Entities;
using Unity.Mathematics;

internal struct UnitPathIgnoredOccupancySystem
{
    public void AddForRequest(ref SystemState state, ref UnitPathRequestBufferSystem requestBuffers, Entity entity)
    {
        Entity ignoredEntity = Entity.Null;
        int2 ignoredCell = default;
        int2 ignoredSize = default;

        EntityManager em = state.EntityManager;
        if (em.HasComponent<UnitTransportBoardingTarget>(entity))
        {
            Entity transport = em.GetComponentData<UnitTransportBoardingTarget>(entity).Transport;
            if (transport != Entity.Null &&
                em.Exists(transport) &&
                em.HasComponent<UnitGrid>(transport) &&
                em.HasComponent<UnitFootprint>(transport))
            {
                ignoredEntity = transport;
                ignoredCell = em.GetComponentData<UnitGrid>(transport).Cell;
                ignoredSize = em.GetComponentData<UnitFootprint>(transport).Size;
            }
        }

        requestBuffers.IgnoredOccupancyEntities.Add(ignoredEntity);
        requestBuffers.IgnoredOccupancyCells.Add(ignoredCell);
        requestBuffers.IgnoredOccupancySizes.Add(ignoredSize);
    }
}
