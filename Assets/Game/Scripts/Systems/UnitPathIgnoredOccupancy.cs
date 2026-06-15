using Unity.Entities;
using Unity.Mathematics;

internal struct UnitPathIgnoredOccupancy
{
    public void AddForRequest(ref SystemState state, ref UnitPathRequestBuffer requestBuffers, Entity entity)
    {
        ResolveIgnoredOccupancy(state.EntityManager, entity, out Entity ignoredEntity, out int2 ignoredCell, out int2 ignoredSize);

        requestBuffers.IgnoredOccupancyEntities.Add(ignoredEntity);
        requestBuffers.IgnoredOccupancyCells.Add(ignoredCell);
        requestBuffers.IgnoredOccupancySizes.Add(ignoredSize);
    }

    internal static void ResolveIgnoredOccupancy(EntityManager em, Entity entity, out Entity ignoredEntity, out int2 ignoredCell, out int2 ignoredSize)
    {
        ignoredEntity = Entity.Null;
        ignoredCell = default;
        ignoredSize = default;

        if (entity != Entity.Null &&
            em.Exists(entity) &&
            em.HasComponent<UnitGrid>(entity) &&
            em.HasComponent<UnitFootprint>(entity))
        {
            ignoredEntity = entity;
            ignoredCell = em.GetComponentData<UnitGrid>(entity).Cell;
            ignoredSize = em.GetComponentData<UnitFootprint>(entity).Size;
        }

        if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<UnitTransportBoardingTarget>(entity))
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
    }
}
