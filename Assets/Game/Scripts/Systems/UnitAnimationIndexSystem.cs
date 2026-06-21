using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(UnitMoveVisualStateSystem))]
[UpdateAfter(typeof(UnitAttackSystem))]
[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct UnitAnimationIndexSystem : ISystem
{
    private const double FreezeLogThresholdSeconds = 0.05d;
    private static readonly bool EnableAnimationIndexFreezeLogs = false;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitMoveVisualComponent>();
        state.RequireForUpdate<UnitHealth>();
    }

    public void OnUpdate(ref SystemState state)
    {
        bool collectFreezeMetrics = EnableAnimationIndexFreezeLogs;
        double startTime = collectFreezeMetrics ? Time.realtimeSinceStartupAsDouble : 0d;
        state.Dependency.Complete();
        double afterCompleteTime = collectFreezeMetrics ? Time.realtimeSinceStartupAsDouble : 0d;
        float dt = SystemAPI.Time.DeltaTime;
        var animationOrderLookup = SystemAPI.GetBufferLookup<UnitAnimationOrderEntry>(true);
        var deathAnimationLookup = SystemAPI.GetComponentLookup<UnitDeathAnimationComponent>(true);
        var autoWanderLookup = SystemAPI.GetComponentLookup<AutoWanderMoveTag>(true);
        var engageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);

        state.Dependency = new ResolveAnimationIndexJob
        {
            DeltaTime = dt,
            AnimationOrderLookup = animationOrderLookup,
            DeathAnimationLookup = deathAnimationLookup,
            AutoWanderLookup = autoWanderLookup,
            EngageTargetLookup = engageTargetLookup
        }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var animationIndexLookup = SystemAPI.GetComponentLookup<MaterialAnimationIndex>();
        var modelInstanceLookup = SystemAPI.GetComponentLookup<UnitModelInstanceReference>(true);
        var midLodInstanceLookup = SystemAPI.GetComponentLookup<UnitMidLodInstanceReference>(true);
        var lowLodInstanceLookup = SystemAPI.GetComponentLookup<UnitLowLodInstanceReference>(true);
        int checkedUnits = 0;
        int appliedUnits = 0;

        foreach (var (resolvedAnimation, entity) in SystemAPI
                 .Query<RefRW<UnitResolvedAnimationIndex>>()
                 .WithAll<UnitMoveVisualComponent, UnitHealth, UnitAttackAnimationComponent>()
                 .WithNone<StaticGridBlocker>()
                 .WithEntityAccess())
        {
            if (collectFreezeMetrics)
                checkedUnits++;

            if (resolvedAnimation.ValueRO.Updated == 0)
                continue;

            byte targetAnimationIndex = resolvedAnimation.ValueRO.Value;
            bool resolvedChanged = resolvedAnimation.ValueRO.Changed != 0;
            bool appliedToVisuals = resolvedChanged
                ? ApplyAnimationIndexRecursive(entity, targetAnimationIndex, ref animationIndexLookup, ref childLookup)
                : ApplyAnimationIndexToVisualRoots(
                    entity,
                    targetAnimationIndex,
                    ref animationIndexLookup,
                    ref childLookup,
                    ref modelInstanceLookup,
                    ref midLodInstanceLookup,
                    ref lowLodInstanceLookup);

            if (resolvedChanged || appliedToVisuals)
                appliedUnits++;

            if (resolvedAnimation.ValueRO.Changed != 0)
                resolvedAnimation.ValueRW.Changed = 0;
        }

        if (collectFreezeMetrics)
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
            if (elapsed >= FreezeLogThresholdSeconds)
            {
                Debug.Log(
                    $"[FreezeDetect:ECS] UnitAnimationIndexSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms " +
                    $"complete={(afterCompleteTime - startTime) * 1000d:F1}ms units={checkedUnits} applied={appliedUnits}");
            }
        }
    }

    [BurstCompile]
    [WithNone(typeof(StaticGridBlocker))]
    private partial struct ResolveAnimationIndexJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public BufferLookup<UnitAnimationOrderEntry> AnimationOrderLookup;
        [ReadOnly] public ComponentLookup<UnitDeathAnimationComponent> DeathAnimationLookup;
        [ReadOnly] public ComponentLookup<AutoWanderMoveTag> AutoWanderLookup;
        [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;

        private void Execute(
            Entity entity,
            in UnitMoveVisualComponent moveVisual,
            in UnitHealth health,
            ref UnitAttackAnimationComponent attackAnimation,
            ref UnitResolvedAnimationIndex resolvedAnimation)
        {
            attackAnimation.TimeRemaining = math.max(0f, attackAnimation.TimeRemaining - DeltaTime);
            byte targetAnimationIndex;

            if (AnimationOrderLookup.HasBuffer(entity))
            {
                DynamicBuffer<UnitAnimationOrderEntry> animationOrder = AnimationOrderLookup[entity];
                if (animationOrder.Length == 0)
                {
                    resolvedAnimation.Changed = 0;
                    resolvedAnimation.Updated = 0;
                    return;
                }

                targetAnimationIndex = ResolveConfiguredAnimationIndex(
                    animationOrder,
                    moveVisual.IsMoving != 0,
                    attackAnimation.TimeRemaining > 0f,
                    EngageTargetLookup.HasComponent(entity),
                    health.Current <= 0 || DeathAnimationLookup.HasComponent(entity),
                    AutoWanderLookup.HasComponent(entity));
            }
            else
            {
                targetAnimationIndex = 1;
                if (health.Current <= 0 || DeathAnimationLookup.HasComponent(entity))
                    targetAnimationIndex = 5;
                else if (attackAnimation.TimeRemaining > 0f)
                    targetAnimationIndex = 4;
                else if (EngageTargetLookup.HasComponent(entity))
                    targetAnimationIndex = 4;
                else if (moveVisual.IsMoving != 0)
                    targetAnimationIndex = AutoWanderLookup.HasComponent(entity) ? (byte)2 : (byte)3;
            }

            bool resolvedChanged = resolvedAnimation.Value != targetAnimationIndex;
            if (resolvedChanged)
                resolvedAnimation.Value = targetAnimationIndex;

            resolvedAnimation.Changed = (byte)(resolvedChanged ? 1 : 0);
            resolvedAnimation.Updated = 1;
        }
    }

    private static byte ResolveConfiguredAnimationIndex(
        DynamicBuffer<UnitAnimationOrderEntry> animationOrder,
        bool isMoving,
        bool isAttacking,
        bool isInAttackMode,
        bool isDead,
        bool isAutoWandering)
    {
        if (isDead)
        {
            return FindFirstAnimationIndex(animationOrder, UnitAnimationKind.Death01, UnitAnimationKind.Death02, UnitAnimationKind.Death03);
        }

        if (isAttacking)
        {
            if (isMoving)
            {
                return isAutoWandering
                    ? FindFirstAnimationIndex(animationOrder, UnitAnimationKind.WalkShoot, UnitAnimationKind.WalkAim, UnitAnimationKind.Shoot, UnitAnimationKind.Aim)
                    : FindFirstAnimationIndex(animationOrder, UnitAnimationKind.RunShoot, UnitAnimationKind.RunAim, UnitAnimationKind.Shoot, UnitAnimationKind.Aim);
            }

            return FindFirstAnimationIndex(animationOrder, UnitAnimationKind.Shoot, UnitAnimationKind.Aim, UnitAnimationKind.Idle);
        }

        if (isInAttackMode)
        {
            if (isMoving)
            {
                return isAutoWandering
                    ? FindFirstAnimationIndex(animationOrder, UnitAnimationKind.WalkAim, UnitAnimationKind.WalkShoot, UnitAnimationKind.Aim, UnitAnimationKind.Shoot)
                    : FindFirstAnimationIndex(animationOrder, UnitAnimationKind.RunAim, UnitAnimationKind.RunShoot, UnitAnimationKind.Aim, UnitAnimationKind.Shoot);
            }

            return FindFirstAnimationIndex(animationOrder, UnitAnimationKind.Aim, UnitAnimationKind.Shoot, UnitAnimationKind.Idle);
        }

        if (isMoving)
        {
            return isAutoWandering
                ? FindFirstAnimationIndex(animationOrder, UnitAnimationKind.Walk, UnitAnimationKind.Run, UnitAnimationKind.Idle)
                : FindFirstAnimationIndex(animationOrder, UnitAnimationKind.Run, UnitAnimationKind.Walk, UnitAnimationKind.Idle);
        }

        return FindFirstAnimationIndex(animationOrder, UnitAnimationKind.Idle, UnitAnimationKind.Aim, UnitAnimationKind.Walk);
    }

    private static byte FindFirstAnimationIndex(DynamicBuffer<UnitAnimationOrderEntry> animationOrder, UnitAnimationKind preferredKind)
    {
        return TryFindAnimationIndex(animationOrder, preferredKind, out byte animationIndex)
            ? animationIndex
            : (byte)0;
    }

    private static byte FindFirstAnimationIndex(DynamicBuffer<UnitAnimationOrderEntry> animationOrder, UnitAnimationKind first, UnitAnimationKind second)
    {
        if (TryFindAnimationIndex(animationOrder, first, out byte animationIndex))
            return animationIndex;

        return FindFirstAnimationIndex(animationOrder, second);
    }

    private static byte FindFirstAnimationIndex(DynamicBuffer<UnitAnimationOrderEntry> animationOrder, UnitAnimationKind first, UnitAnimationKind second, UnitAnimationKind third)
    {
        if (TryFindAnimationIndex(animationOrder, first, out byte animationIndex))
            return animationIndex;
        if (TryFindAnimationIndex(animationOrder, second, out animationIndex))
            return animationIndex;

        return FindFirstAnimationIndex(animationOrder, third);
    }

    private static byte FindFirstAnimationIndex(DynamicBuffer<UnitAnimationOrderEntry> animationOrder, UnitAnimationKind first, UnitAnimationKind second, UnitAnimationKind third, UnitAnimationKind fourth)
    {
        if (TryFindAnimationIndex(animationOrder, first, out byte animationIndex))
            return animationIndex;
        if (TryFindAnimationIndex(animationOrder, second, out animationIndex))
            return animationIndex;
        if (TryFindAnimationIndex(animationOrder, third, out animationIndex))
            return animationIndex;

        return FindFirstAnimationIndex(animationOrder, fourth);
    }

    private static bool TryFindAnimationIndex(DynamicBuffer<UnitAnimationOrderEntry> animationOrder, UnitAnimationKind preferredKind, out byte animationIndex)
    {
        byte preferred = (byte)preferredKind;
        for (int slotIndex = 0; slotIndex < animationOrder.Length; slotIndex++)
        {
            if (animationOrder[slotIndex].Kind != preferred)
                continue;

            animationIndex = (byte)(preferred + 1);
            return true;
        }

        animationIndex = 0;
        return false;
    }

    private static bool ApplyAnimationIndexToVisualRoots(
        Entity entity,
        byte animationIndex,
        ref ComponentLookup<MaterialAnimationIndex> animationIndexLookup,
        ref BufferLookup<Child> childLookup,
        ref ComponentLookup<UnitModelInstanceReference> modelInstanceLookup,
        ref ComponentLookup<UnitMidLodInstanceReference> midLodInstanceLookup,
        ref ComponentLookup<UnitLowLodInstanceReference> lowLodInstanceLookup)
    {
        bool applied = false;

        if (modelInstanceLookup.HasComponent(entity))
            applied |= ApplyAnimationIndexIfNeeded(modelInstanceLookup[entity].Instance, animationIndex, ref animationIndexLookup, ref childLookup);

        if (midLodInstanceLookup.HasComponent(entity))
            applied |= ApplyAnimationIndexIfNeeded(midLodInstanceLookup[entity].Instance, animationIndex, ref animationIndexLookup, ref childLookup);

        if (lowLodInstanceLookup.HasComponent(entity))
            applied |= ApplyAnimationIndexIfNeeded(lowLodInstanceLookup[entity].Instance, animationIndex, ref animationIndexLookup, ref childLookup);

        return applied;
    }

    private static bool ApplyAnimationIndexIfNeeded(
        Entity entity,
        byte animationIndex,
        ref ComponentLookup<MaterialAnimationIndex> animationIndexLookup,
        ref BufferLookup<Child> childLookup)
    {
        if (entity == Entity.Null)
            return false;

        if (animationIndexLookup.HasComponent(entity) &&
            animationIndexLookup[entity].Value == animationIndex)
        {
            return false;
        }

        return ApplyAnimationIndexRecursive(entity, animationIndex, ref animationIndexLookup, ref childLookup);
    }

    private static bool ApplyAnimationIndexRecursive(
        Entity entity,
        byte animationIndex,
        ref ComponentLookup<MaterialAnimationIndex> animationIndexLookup,
        ref BufferLookup<Child> childLookup)
    {
        bool applied = false;

        if (animationIndexLookup.HasComponent(entity))
        {
            var current = animationIndexLookup[entity];
            if (current.Value != animationIndex)
            {
                current.Value = animationIndex;
                animationIndexLookup[entity] = current;
                applied = true;
            }
        }

        if (!childLookup.HasBuffer(entity))
            return applied;

        var children = childLookup[entity];
        for (int i = 0; i < children.Length; i++)
            applied |= ApplyAnimationIndexRecursive(children[i].Value, animationIndex, ref animationIndexLookup, ref childLookup);

        return applied;
    }
}
