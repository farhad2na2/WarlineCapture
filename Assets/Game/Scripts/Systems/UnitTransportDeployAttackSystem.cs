using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(UnitTransportAirdropSystem))]
public partial struct UnitTransportDeployAttackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitTransportDeployAttackTarget>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            foreach (var (attack, entity) in SystemAPI
                         .Query<RefRO<UnitTransportDeployAttackTarget>>()
                         .WithNone<Disabled>()
                         .WithNone<UnitTransportPassenger>()
                         .WithNone<UnitTransportCargoPassenger>()
                         .WithNone<UnitTransportParachuteDropComponent>()
                         .WithNone<UnitTransportCargoDropComponent>()
                         .WithNone<UnitTransportAirdropSettleComponent>()
                         .WithEntityAccess())
            {
                UnitTransportDeployAttackTarget target = attack.ValueRO;
                if (!CanPassengerAttack(em, entity) || !IsLiveAttackTarget(em, target.TargetEntity))
                {
                    ecb.RemoveComponent<UnitTransportDeployAttackTarget>(entity);
                    continue;
                }

                SetOrAdd(
                    em,
                    ecb,
                    entity,
                    new EngageTarget
                    {
                        Target = target.TargetEntity,
                        Cell = target.TargetCell,
                        Position = target.TargetPosition,
                        IsCommanded = 1
                    });
                RemoveIfPresent<UnitTarget>(em, ecb, entity);
                RemoveIfPresent<UnitPathRequest>(em, ecb, entity);
                RemoveIfPresent<UnitPathFollow>(em, ecb, entity);
                RemoveIfPresent<UnitPathRange>(em, ecb, entity);
                RemoveIfPresent<ManualMoveOrderTag>(em, ecb, entity);
                ecb.RemoveComponent<UnitTransportDeployAttackTarget>(entity);
            }

            ecb.Playback(em);
        }
        finally
        {
            ecb.Dispose();
        }
    }

    private static bool CanPassengerAttack(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) ||
            !em.HasComponent<UnitCombat>(entity) ||
            !em.HasComponent<UnitAttack>(entity))
        {
            return false;
        }

        if (em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
            return false;

        return !em.HasComponent<UnitHealth>(entity) ||
               em.GetComponentData<UnitHealth>(entity).Current > 0;
    }

    private static bool IsLiveAttackTarget(EntityManager em, Entity target)
    {
        if (target == Entity.Null ||
            !em.Exists(target))
        {
            return false;
        }

        return !em.HasComponent<UnitHealth>(target) ||
               em.GetComponentData<UnitHealth>(target).Current > 0;
    }

    private static void SetOrAdd<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.SetComponent(entity, value);
        else
            ecb.AddComponent(entity, value);
    }

    private static void RemoveIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.Exists(entity) && em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }
}
