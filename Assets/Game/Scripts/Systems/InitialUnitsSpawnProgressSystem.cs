using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct InitialUnitsSpawnProgressSystem
{
    public void InitializePending(EntityManager em, InitialUnitsSpawnQuerySystem.Context queryContext)
    {
        using var initEntities = queryContext.PendingInitQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < initEntities.Length; i++)
        {
            Entity entity = initEntities[i];
            InitialUnitsSpawnConfig config = em.GetComponentData<InitialUnitsSpawnConfig>(entity);
            int unitSpawnCount = em.GetBuffer<InitialUnitsFactionUnitSpawnEntry>(entity).Length;
            em.AddComponentData(entity, new InitialUnitsSpawnProgress
            {
                RandomState = math.max(1u, config.RandomSeed),
                BlockersSpawned = 0,
                InitialResourcesApplied = 0,
                InitialBuildingRequestsIssued = 0,
                InitialBuildingsSpawned = 0,
                InitialBuildingCompletionWaitFrames = 0
            });

            DynamicBuffer<InitialUnitsFactionUnitSpawnProgress> progressBuffer = em.AddBuffer<InitialUnitsFactionUnitSpawnProgress>(entity);
            progressBuffer.ResizeUninitialized(unitSpawnCount);
            for (int unitIndex = 0; unitIndex < unitSpawnCount; unitIndex++)
                progressBuffer[unitIndex] = new InitialUnitsFactionUnitSpawnProgress { Spawned = 0 };
        }
    }
}
