using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(EngageTargetValidateSystem))]
public partial struct BaseBreachOrderSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BaseBreachOrder>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        var gridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        EntityManager em = state.EntityManager;

        foreach (var (breachOrder, entity) in SystemAPI
                     .Query<RefRW<BaseBreachOrder>>()
                     .WithEntityAccess())
        {
            BaseBreachOrder order = breachOrder.ValueRO;

            if (!IsAliveTarget(order.FinalTarget, transformLookup, healthLookup))
            {
                ecb.RemoveComponent<BaseBreachOrder>(entity);
                continue;
            }

            if (order.Stage == BaseBreachOrder.StageMovingToEnemyBreach)
            {
                if (!IsAliveTarget(order.BreachTarget, transformLookup, healthLookup))
                {
                    order.Stage = BaseBreachOrder.StageMovingToFinalTarget;
                    breachOrder.ValueRW = order;
                    EnsurePathRequest(em, ecb, entity, order.FinalCell);
                    RemoveIfPresent<EngageTarget>(em, ecb, entity);
                    continue;
                }

                if (!IsNearCell(entity, order.BreachCell, gridLookup))
                {
                    EnsurePathRequest(em, ecb, entity, order.BreachCell);
                    RemoveIfPresent<EngageTarget>(em, ecb, entity);
                    continue;
                }

                order.Stage = BaseBreachOrder.StageAttackingBreach;
                breachOrder.ValueRW = order;
                SetEngageTarget(em, ecb, entity, order.BreachTarget, order.BreachCell, order.BreachPosition, order.IsCommanded);
                RemovePathingState(em, ecb, entity);
                continue;
            }

            if (order.Stage == BaseBreachOrder.StageMovingToFinalTarget)
            {
                if (HasActivePathingState(em, entity))
                {
                    EnsurePathRequest(em, ecb, entity, order.FinalCell);
                    RemoveIfPresent<EngageTarget>(em, ecb, entity);
                    continue;
                }

                RemovePathingState(em, ecb, entity);
                SetEngageTarget(em, ecb, entity, order.FinalTarget, order.FinalCell, order.FinalPosition, order.IsCommanded);
                ecb.RemoveComponent<BaseBreachOrder>(entity);
                continue;
            }

            if (em.HasComponent<EngageTarget>(entity))
            {
                EngageTarget engage = em.GetComponentData<EngageTarget>(entity);
                if (engage.Target == order.FinalTarget)
                {
                    ecb.RemoveComponent<BaseBreachOrder>(entity);
                    continue;
                }

                if (IsAliveTarget(engage.Target, transformLookup, healthLookup))
                    continue;
            }

            if (IsAliveTarget(order.BreachTarget, transformLookup, healthLookup))
            {
                SetEngageTarget(em, ecb, entity, order.BreachTarget, order.BreachCell, order.BreachPosition, order.IsCommanded);
                continue;
            }

            order.Stage = BaseBreachOrder.StageMovingToFinalTarget;
            breachOrder.ValueRW = order;
            EnsurePathRequest(em, ecb, entity, order.FinalCell);
            RemoveIfPresent<EngageTarget>(em, ecb, entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static bool IsAliveTarget(
        Entity target,
        ComponentLookup<LocalTransform> transformLookup,
        ComponentLookup<UnitHealth> healthLookup)
    {
        if (target == Entity.Null || !transformLookup.HasComponent(target))
            return false;

        return !healthLookup.HasComponent(target) || healthLookup[target].Current > 0;
    }

    private static bool IsNearCell(
        Entity entity,
        int2 targetCell,
        ComponentLookup<UnitGrid> gridLookup)
    {
        if (!gridLookup.HasComponent(entity))
            return false;

        int2 delta = gridLookup[entity].Cell - targetCell;
        return math.abs(delta.x) <= 1 && math.abs(delta.y) <= 1;
    }

    private static void EnsurePathRequest(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity entity,
        int2 goal)
    {
        if (em.HasComponent<UnitTarget>(entity))
            em.SetComponentData(entity, new UnitTarget { Cell = goal });
        else
            ecb.AddComponent(entity, new UnitTarget { Cell = goal });

        if (!em.HasComponent<UnitPathRequest>(entity) &&
            !em.HasComponent<UnitPathFollow>(entity) &&
            !em.HasComponent<UnitPathRetryCooldown>(entity))
        {
            ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
        }

        if (!em.HasComponent<ManualMoveOrderTag>(entity))
            ecb.AddComponent<ManualMoveOrderTag>(entity);
    }

    private static void SetEngageTarget(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity entity,
        Entity target,
        int2 cell,
        float3 position,
        byte isCommanded)
    {
        EngageTarget engage = new()
        {
            Target = target,
            Cell = cell,
            Position = position,
            IsCommanded = isCommanded
        };

        if (em.HasComponent<EngageTarget>(entity))
            em.SetComponentData(entity, engage);
        else
            ecb.AddComponent(entity, engage);
    }

    private static void RemovePathingState(EntityManager em, EntityCommandBuffer ecb, Entity entity)
    {
        RemoveIfPresent<UnitPathRequest>(em, ecb, entity);
        RemoveIfPresent<UnitPathFollow>(em, ecb, entity);
        RemoveIfPresent<UnitPathRange>(em, ecb, entity);
        RemoveIfPresent<UnitTarget>(em, ecb, entity);
        RemoveIfPresent<ManualMoveOrderTag>(em, ecb, entity);
        RemoveIfPresent<ManualMoveGroupMemberTag>(em, ecb, entity);
    }

    private static void RemoveIfPresent<T>(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private static bool HasActivePathingState(EntityManager em, Entity entity)
    {
        return em.HasComponent<UnitPathRequest>(entity) ||
               em.HasComponent<UnitPathFollow>(entity) ||
               em.HasComponent<UnitPathRetryCooldown>(entity);
    }
}
