using Unity.Entities;

public readonly struct InitialUnitSpawnResetSystem
{
    public void ResetSpawnedUnitRuntimeState(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        ref Unity.Mathematics.Random rng)
    {
        if (hasPrefab && em.HasComponent<UnitIdleWanderState>(prefab))
        {
            ecb.SetComponent(instance, new UnitIdleWanderState
            {
                RandomState = rng.NextUInt(),
                RetrySeconds = 0f,
                CurrentIdleDelaySeconds = 0f
            });
        }

        if (!hasPrefab)
            return;

        ecb.RemoveComponent<UnitPathFollow>(instance);
        ecb.RemoveComponent<UnitPathRange>(instance);
        ecb.RemoveComponent<EngageTarget>(instance);
        ecb.RemoveComponent<UnitPathRequest>(instance);
        ecb.RemoveComponent<UnitTarget>(instance);
        ecb.RemoveComponent<AutoWanderMoveTag>(instance);
    }
}
