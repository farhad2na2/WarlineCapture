using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SkirmishScenarioSpawnSystem))]
    [UpdateBefore(typeof(SkirmishRosterProjectionSystem))]
    public partial struct SkirmishVisualSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                    TryAttachMissing(em, entities[i]);
            }
            finally
            {
                entities.Dispose();
            }
        }

        private static void TryAttachMissing(EntityManager em, Entity entity)
        {
            SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
            if (session.IsLegacy != 0 || session.SpawnComplete == 0)
                return;
            if (session.Phase != SkirmishSessionPhase.Playing)
                return;
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                return;
            SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
            SkirmishVisualSpawnService.AttachMissing(em, entity, setup);
        }
    }
}
