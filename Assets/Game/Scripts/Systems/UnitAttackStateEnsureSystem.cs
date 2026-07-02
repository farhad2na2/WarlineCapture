using Unity.Burst;
using Unity.Entities;
using Game.Components;

// Ensures all units that can attack have a UnitAttackCooldownComponent, even if the prefab was baked before the component existed.

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct UnitAttackStateEnsureSystem : ISystem
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

            foreach (var (attack, entity) in SystemAPI.Query<RefRO<UnitAttack>>().WithNone<UnitAttackCooldownComponent>().WithEntityAccess())
            {
                ecb.AddComponent(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
            }
        }
    }
}
