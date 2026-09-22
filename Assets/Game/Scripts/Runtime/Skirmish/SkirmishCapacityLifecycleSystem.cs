using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SkirmishObjectiveFactProjectionSystem))]
    public partial struct SkirmishCapacityLifecycleSystem : ISystem
    {
        private EntityQuery ownedUnits;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            ownedUnits = state.GetEntityQuery(
                ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),
                ComponentType.ReadOnly<SkirmishUnitRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.Phase != SkirmishSessionPhase.Playing)
                    continue;
                ReleaseDead(em, entity, ownedUnits, session.ValueRO.SessionId);
            }
        }

        public static int ReleaseDead(
            EntityManager em,
            Entity session,
            EntityQuery owned,
            FixedString64Bytes sessionId)
        {
            using NativeArray<Entity> entities = owned.ToEntityArray(Allocator.Temp);
            int released = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity unit = entities[i];
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(unit).SessionId.Equals(sessionId))
                    continue;
                if (!em.HasComponent<UnitHealth>(unit) || em.GetComponentData<UnitHealth>(unit).Current > 0)
                    continue;
                if (SkirmishProductionService.TryReleaseDeath(em, session, unit))
                    released++;
            }

            return released;
        }
    }
}
