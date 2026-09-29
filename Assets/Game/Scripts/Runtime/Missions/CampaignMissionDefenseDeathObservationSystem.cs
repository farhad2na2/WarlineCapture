using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    // Death remains observable while a cinematic pauses the mission clock. Record
    // actual zero health before native corpse cleanup can remove the entity.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GroundMissileImpactSystem))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitDeathSystem))]
    public partial struct CampaignMissionDefenseDeathObservationSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<CampaignMissionDefenseMember>();
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (runtime, defense, entity) in SystemAPI.Query<RefRO<CampaignMissionRuntimeComponent>, RefRO<CampaignMissionDefenseStateComponent>>().WithEntityAccess())
            {
                if (!runtime.ValueRO.SessionToken.Equals(defense.ValueRO.SessionToken) || runtime.ValueRO.AttemptOrdinal != defense.ValueRO.AttemptOrdinal || !state.EntityManager.HasBuffer<CampaignMissionDefenseMember>(entity)) continue;
                Observe(state.EntityManager, entity, runtime.ValueRO.SessionToken);
            }
        }

        public static void Observe(EntityManager em, Entity root, Unity.Collections.FixedString64Bytes session)
        {
            var members = em.GetBuffer<CampaignMissionDefenseMember>(root);
            for (int i = 0; i < members.Length; i++)
            {
                var member = members[i];
                if (!em.Exists(member.Entity) || !em.HasComponent<UnitHealth>(member.Entity) || !em.HasComponent<CampaignMissionUnitRoleComponent>(member.Entity)) continue;
                if (!em.GetComponentData<CampaignMissionUnitRoleComponent>(member.Entity).SessionToken.Equals(session)) continue;
                var health = em.GetComponentData<UnitHealth>(member.Entity);
                if (health.Max > 0) member.HealthInitialized = 1;
                if (member.HealthInitialized != 0 && health.Current <= 0) member.Defeated = 1;
                members[i] = member;
            }
        }
    }
}
