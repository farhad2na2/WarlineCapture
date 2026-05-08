using Unity.Collections;
using Unity.Entities;

// Ensures all units that can attack have a UnitAttackState, even if the prefab was baked before the component existed.
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct UnitAttackStateEnsureSystem : ISystem
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

        foreach (var (attack, entity) in SystemAPI.Query<RefRO<UnitAttack>>().WithNone<UnitAttackState>().WithEntityAccess())
        {
            ecb.AddComponent(entity, new UnitAttackState { CooldownRemaining = 0f });
        }
    }
}

