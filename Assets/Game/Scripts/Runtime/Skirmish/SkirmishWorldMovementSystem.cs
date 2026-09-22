using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishArmyGroupSystem))]
    public partial struct SkirmishWorldMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            bool paused = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                          gameplay.SimulationActive == 0;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                if (em.HasComponent<SkirmishObjectiveClockComponent>(entity) &&
                    em.GetComponentData<SkirmishObjectiveClockComponent>(entity).Paused != 0)
                    paused = true;
                float delta = SystemAPI.Time.DeltaTime;
                SkirmishExpandedEngagementService.Step(em, entity, delta, paused);
                SkirmishWorldMovementService.Step(em, entity, delta, paused);
                SkirmishArmyDrawerProjection.Project(em, entity);
            }
        }
    }
}
