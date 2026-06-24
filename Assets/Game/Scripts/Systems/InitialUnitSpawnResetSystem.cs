using Unity.Entities;

[DisableAutoCreation]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct InitialUnitSpawnResetSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // RequireForUpdate intentionally omitted: disabled spawn helper; initial spawn code calls methods directly.
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public void ResetSpawnedUnitRuntimeState(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity instance,
        Entity prefab,
        bool hasPrefab,
        ref Unity.Mathematics.Random rng)
    {
        if (hasPrefab && em.HasComponent<UnitIdleWanderComponent>(prefab))
        {
            ecb.SetComponent(instance, new UnitIdleWanderComponent
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
