using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UnitAttackTraceStateEnsureSystem : ISystem
{
    private EntityQuery _ecbSingletonQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _ecbSingletonQuery = state.GetEntityQuery(ComponentType.ReadOnly<EndSimulationEntityCommandBufferSystem.Singleton>());
        state.RequireForUpdate<UnitAttack>();
        state.RequireForUpdate(_ecbSingletonQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
        var ecbSystem = state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitAttack>>().WithNone<UnitAttackTraceComponent>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
        }
    }
}
