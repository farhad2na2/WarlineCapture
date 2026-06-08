using Unity.Entities;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UnitAttackTraceStateEnsureSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitAttack>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

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
