using Unity.Entities;
using Unity.Transforms;

[UpdateBefore(typeof(UnitEngagementSystem))]
public partial struct MissionRuntimeOpeningControlProtectionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MissionRuntimeOpeningControlProtection>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Chapter01M01PlayableRuntime.IsActiveMission())
            return;

        EntityManager em = state.EntityManager;
        Entity commandSquad = Entity.Null;
        foreach (var (_, entity) in SystemAPI.Query<RefRO<MissionRuntimeCommandSquadTag>>().WithEntityAccess())
        {
            commandSquad = entity;
            break;
        }

        bool releaseProtection = false;
        if (commandSquad != Entity.Null && em.Exists(commandSquad) && em.HasComponent<EngageTarget>(commandSquad))
        {
            EngageTarget commandEngage = em.GetComponentData<EngageTarget>(commandSquad);
            releaseProtection = commandEngage.IsCommanded != 0;
            if (!releaseProtection)
                em.RemoveComponent<EngageTarget>(commandSquad);
        }

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach (var (_, entity) in SystemAPI.Query<RefRO<MissionRuntimeOpeningControlProtection>>().WithEntityAccess())
        {
            if (releaseProtection)
            {
                if (em.HasComponent<UnitCombat>(entity))
                {
                    UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                    combat.CanAttack = 1;
                    combat.AutoEngage = 1;
                    em.SetComponentData(entity, combat);
                }

                ecb.RemoveComponent<MissionRuntimeOpeningControlProtection>(entity);
                continue;
            }

            if (em.HasComponent<UnitCombat>(entity))
            {
                UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                if (combat.CanAttack != 0 || combat.AutoEngage != 0)
                {
                    combat.CanAttack = 0;
                    combat.AutoEngage = 0;
                    em.SetComponentData(entity, combat);
                }
            }

            if (em.HasComponent<EngageTarget>(entity))
                ecb.RemoveComponent<EngageTarget>(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}

[UpdateAfter(typeof(UnitAttackSystem))]
[UpdateBefore(typeof(UnitDeathSystem))]
public partial struct MissionRuntimeOpeningControlSurvivalSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MissionRuntimeOpeningControlProtection>();
        state.RequireForUpdate<MissionRuntimeCommandSquadTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Chapter01M01PlayableRuntime.IsActiveMission())
            return;

        EntityManager em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach (var (_, entity) in SystemAPI.Query<RefRO<MissionRuntimeCommandSquadTag>>().WithEntityAccess())
        {
            if (!em.HasComponent<UnitHealth>(entity))
                continue;

            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            int protectedHealth = health.Max > 0 ? health.Max : 1;
            if (health.Current != protectedHealth)
            {
                health.Current = protectedHealth;
                em.SetComponentData(entity, health);
            }

            if (em.HasComponent<UnitDeathAnimationState>(entity))
                ecb.RemoveComponent<UnitDeathAnimationState>(entity);
            if (em.HasComponent<RecentAttacker>(entity))
                ecb.RemoveComponent<RecentAttacker>(entity);
            if (em.HasComponent<RecentDamageHealthBarVisibility>(entity))
                ecb.RemoveComponent<RecentDamageHealthBarVisibility>(entity);
        }
        ecb.Playback(em);
        ecb.Dispose();
    }
}
