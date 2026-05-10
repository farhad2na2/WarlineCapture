using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(UnitMoveVisualStateSystem))]
[UpdateAfter(typeof(UnitAttackSystem))]
[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct MissionRuntimeSpritePresenterSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MissionRuntimeSpritePresenter>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();
        ComponentLookup<UnitHealth> healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
        ComponentLookup<UnitMoveVisualState> moveLookup = SystemAPI.GetComponentLookup<UnitMoveVisualState>(true);
        ComponentLookup<UnitTarget> targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
        ComponentLookup<UnitPathRequest> pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
        ComponentLookup<UnitPathFollow> pathFollowLookup = SystemAPI.GetComponentLookup<UnitPathFollow>(true);
        ComponentLookup<UnitAttackAnimationState> attackAnimationLookup = SystemAPI.GetComponentLookup<UnitAttackAnimationState>(true);
        ComponentLookup<EngageTarget> engageLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);
        ComponentLookup<UnitDeathAnimationState> deathLookup = SystemAPI.GetComponentLookup<UnitDeathAnimationState>(true);

        foreach (var (presenter, entity) in SystemAPI.Query<RefRW<MissionRuntimeSpritePresenter>>().WithEntityAccess())
        {
            MissionRuntimeSpriteVisualState visualState = ResolveVisualState(
                entity,
                ref healthLookup,
                ref moveLookup,
                ref targetLookup,
                ref pathRequestLookup,
                ref pathFollowLookup,
                ref attackAnimationLookup,
                ref engageLookup,
                ref deathLookup);
            presenter.ValueRW.CurrentState = (byte)visualState;
            presenter.ValueRW.CurrentSpriteId = Chapter01M01SpritePresenterCatalog.ResolveSpriteId(presenter.ValueRO, visualState);
        }
    }

    public static MissionRuntimeSpriteVisualState ResolveVisualState(
        EntityManager em,
        Entity entity)
    {
        bool isDestroyed = false;
        bool isDamaged = false;
        if (em.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            isDestroyed = health.Current <= 0 || em.HasComponent<UnitDeathAnimationState>(entity);
            isDamaged = health.Max > 0 && health.Current < math.max(1, health.Max / 2);
        }

        if (isDestroyed)
            return MissionRuntimeSpriteVisualState.Destroyed;
        if (isDamaged)
            return MissionRuntimeSpriteVisualState.Damaged;
        if ((em.HasComponent<UnitAttackAnimationState>(entity) && em.GetComponentData<UnitAttackAnimationState>(entity).TimeRemaining > 0f) ||
            em.HasComponent<EngageTarget>(entity))
        {
            return MissionRuntimeSpriteVisualState.Attack;
        }
        if (em.HasComponent<UnitMoveVisualState>(entity) && em.GetComponentData<UnitMoveVisualState>(entity).IsMoving != 0)
            return MissionRuntimeSpriteVisualState.Move;
        if (em.HasComponent<UnitTarget>(entity) ||
            em.HasComponent<UnitPathRequest>(entity) ||
            em.HasComponent<UnitPathFollow>(entity))
        {
            return MissionRuntimeSpriteVisualState.Move;
        }

        return MissionRuntimeSpriteVisualState.Idle;
    }

    internal static MissionRuntimeSpriteVisualState ResolveVisualState(
        Entity entity,
        ref ComponentLookup<UnitHealth> healthLookup,
        ref ComponentLookup<UnitMoveVisualState> moveLookup,
        ref ComponentLookup<UnitTarget> targetLookup,
        ref ComponentLookup<UnitPathRequest> pathRequestLookup,
        ref ComponentLookup<UnitPathFollow> pathFollowLookup,
        ref ComponentLookup<UnitAttackAnimationState> attackAnimationLookup,
        ref ComponentLookup<EngageTarget> engageLookup,
        ref ComponentLookup<UnitDeathAnimationState> deathLookup)
    {
        if (healthLookup.HasComponent(entity))
        {
            UnitHealth health = healthLookup[entity];
            if (health.Current <= 0 || deathLookup.HasComponent(entity))
                return MissionRuntimeSpriteVisualState.Destroyed;
            if (health.Max > 0 && health.Current < math.max(1, health.Max / 2))
                return MissionRuntimeSpriteVisualState.Damaged;
        }

        if ((attackAnimationLookup.HasComponent(entity) && attackAnimationLookup[entity].TimeRemaining > 0f) ||
            engageLookup.HasComponent(entity))
        {
            return MissionRuntimeSpriteVisualState.Attack;
        }

        if (moveLookup.HasComponent(entity) && moveLookup[entity].IsMoving != 0)
            return MissionRuntimeSpriteVisualState.Move;
        if (targetLookup.HasComponent(entity) ||
            pathRequestLookup.HasComponent(entity) ||
            pathFollowLookup.HasComponent(entity))
        {
            return MissionRuntimeSpriteVisualState.Move;
        }

        return MissionRuntimeSpriteVisualState.Idle;
    }
}
