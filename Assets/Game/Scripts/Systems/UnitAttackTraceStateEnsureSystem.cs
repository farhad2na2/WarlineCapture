using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UnitAttackTraceStateEnsureSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitAttack>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (_, entity) in SystemAPI.Query<RefRO<UnitAttack>>().WithNone<UnitAttackTraceComponent>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
        }
    }
}
