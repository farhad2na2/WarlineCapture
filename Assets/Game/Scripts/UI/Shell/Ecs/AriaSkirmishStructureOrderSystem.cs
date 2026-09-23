using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    /// <summary>
    /// While ARIA is conducting an expanded match, orders the player's
    /// structure column onto the designated enemy base. The public Attack
    /// button remains the same command; this runs when that tap never lands.
    /// Manual play is left untouched. Nothing is spawned and no result is stamped.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(SkirmishArmyGroupSystem))]
    [UpdateBefore(typeof(SkirmishWorldMovementSystem))]
    public partial struct AriaSkirmishStructureOrderSystem : ISystem
    {
        private EntityQuery sessions;
        private EntityQuery aria;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            sessions = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishExpandedSessionComponent>());
            aria = state.GetEntityQuery(ComponentType.ReadOnly<AriaPlaySessionComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!AriaIsConducting(state.EntityManager))
                return;

            EntityManager em = state.EntityManager;
            using NativeArray<Entity> entities = sessions.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0 || session.Phase != SkirmishSessionPhase.Playing)
                    continue;
                SkirmishExpandedPresentedOrders.TryOrderStructureAssault(em, entity);
            }
        }

        private bool AriaIsConducting(EntityManager em)
        {
            if (aria.IsEmptyIgnoreFilter)
                return false;
            using NativeArray<Entity> entities = aria.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.HasComponent<AriaPlaySessionComponent>(entities[i]))
                    continue;
                AriaPlayPhase phase = em.GetComponentData<AriaPlaySessionComponent>(entities[i]).Phase;
                if (phase != AriaPlayPhase.Manual)
                    return true;
            }

            return false;
        }
    }
}
