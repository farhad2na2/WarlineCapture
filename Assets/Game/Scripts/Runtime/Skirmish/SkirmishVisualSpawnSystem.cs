using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
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
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.SpawnComplete == 0)
                    continue;
                if (session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                    continue;
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
                SkirmishVisualSpawnService.AttachMissing(em, entity, setup);
            }
        }
    }
}
