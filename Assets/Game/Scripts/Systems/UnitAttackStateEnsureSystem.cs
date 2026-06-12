using Unity.Burst;
using Unity.Entities;

// Ensures all units that can attack have a UnitAttackCooldownComponent, even if the prefab was baked before the component existed.
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UnitAttackStateEnsureSystem : ISystem
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

        foreach (var (attack, entity) in SystemAPI.Query<RefRO<UnitAttack>>().WithNone<UnitAttackCooldownComponent>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        }
    }
}
