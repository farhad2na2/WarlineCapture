using Unity.Entities;

public readonly struct InitialBuildingBoundarySystem
{
    public bool TryGetRuntimeSpawnRequests(
        EntityManager em,
        Entity boundaryEntity,
        out DynamicBuffer<BuildingRuntimeSpawnRequest> requests)
    {
        requests = default;
        if (boundaryEntity == Entity.Null ||
            !em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
            return false;

        requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
        return true;
    }

    public DynamicBuffer<BuildingRuntimeSpawnRequest> GetRuntimeSpawnRequests(
        EntityManager em,
        Entity boundaryEntity)
    {
        return em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity);
    }

    public bool TryGetConfiguredSpawnableReadModels(
        EntityManager em,
        Entity boundaryEntity,
        out DynamicBuffer<BuildingConfiguredSpawnableReadModel> spawnables)
    {
        spawnables = default;
        if (boundaryEntity == Entity.Null ||
            !em.HasBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity))
            return false;

        spawnables = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true);
        return true;
    }

    public bool TryGetFactionProductionSpawnPoints(
        EntityManager em,
        Entity boundaryEntity,
        out DynamicBuffer<BuildingFactionProductionSpawnPointReadModel> spawnPoints)
    {
        spawnPoints = default;
        if (boundaryEntity == Entity.Null ||
            !em.HasBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity))
            return false;

        spawnPoints = em.GetBuffer<BuildingFactionProductionSpawnPointReadModel>(boundaryEntity, true);
        return true;
    }
}
