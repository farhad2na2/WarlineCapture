using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishArmyGroupSystem))]
    public partial struct SkirmishWorldMovementSystem : ISystem
    {
        private EntityQuery sessions;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            sessions = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            bool paused = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                          gameplay.SimulationActive == 0;
            float delta = SystemAPI.Time.DeltaTime;
            // Drawer projection can add its buffer. Copy the session list first.
            using NativeArray<Entity> entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0 || session.Phase != SkirmishSessionPhase.Playing)
                    continue;
                if (em.HasComponent<SkirmishObjectiveClockComponent>(entity) &&
                    em.GetComponentData<SkirmishObjectiveClockComponent>(entity).Paused != 0)
                    paused = true;
                SkirmishVisualSpawnService.EnsureMissingCombatTransforms(em, entity);
                SkirmishExpandedEngagementService.Step(em, entity, delta, paused);
                SkirmishWorldMovementService.Step(em, entity, delta, paused);
                SkirmishArmyDrawerProjection.Project(em, entity);
            }
        }
    }
}
