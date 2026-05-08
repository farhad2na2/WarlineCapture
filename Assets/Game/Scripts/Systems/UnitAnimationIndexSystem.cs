using SnivelerCode.GpuAnimation.Scripts.Components;
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
        state.RequireForUpdate<UnitMoveVisualState>();
        state.RequireForUpdate<UnitHealth>();
    }

    public void OnUpdate(ref SystemState state)
    {
        double startTime = Time.realtimeSinceStartupAsDouble;
        state.Dependency.Complete();
        double afterCompleteTime = Time.realtimeSinceStartupAsDouble;
        float dt = SystemAPI.Time.DeltaTime;
        var childLookup = SystemAPI.GetBufferLookup<Child>(true);
        var animationOrderLookup = SystemAPI.GetBufferLookup<UnitAnimationOrderEntry>(true);
        var animationIndexLookup = SystemAPI.GetComponentLookup<MaterialAnimationIndex>();
        var deathAnimationLookup = SystemAPI.GetComponentLookup<UnitDeathAnimationState>(true);
        var autoWanderLookup = SystemAPI.GetComponentLookup<AutoWanderMoveTag>(true);
        var engageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);
        var modelInstanceLookup = SystemAPI.GetComponentLookup<UnitModelInstanceReference>(true);
        var midLodInstanceLookup = SystemAPI.GetComponentLookup<UnitMidLodInstanceReference>(true);
        var lowLodInstanceLookup = SystemAPI.GetComponentLookup<UnitLowLodInstanceReference>(true);
        int checkedUnits = 0;
        int appliedUnits = 0;

        foreach (var (moveVisual, health, attackAnimation, resolvedAnimation, entity) in SystemAPI
                 .Query<RefRO<UnitMoveVisualState>, RefRO<UnitHealth>, RefRW<UnitAttackAnimationState>, RefRW<UnitResolvedAnimationIndex>>()
                 .WithNone<StaticGridBlocker>()
                 .WithEntityAccess())
        {
            checkedUnits++;
            attackAnimation.ValueRW.TimeRemaining = math.max(0f, attackAnimation.ValueRW.TimeRemaining - dt);
            byte targetAnimationIndex;

            if (animationOrderLookup.HasBuffer(entity))
            {
                DynamicBuffer<UnitAnimationOrderEntry> animationOrder = animationOrderLookup[entity];
                if (animationOrder.Length == 0)
                    continue;

                targetAnimationIndex = ResolveConfiguredAnimationIndex(
                    animationOrder,
                    moveVisual.ValueRO.IsMoving != 0,
                    attackAnimation.ValueRO.TimeRemaining > 0f,
                    engageTargetLookup.HasComponent(entity),
                    health.ValueRO.Current <= 0 || deathAnimationLookup.HasComponent(entity),
                    autoWanderLookup.HasComponent(entity));
            }
            else
            {
                targetAnimationIndex = 1;
                if (health.ValueRO.Current <= 0 || deathAnimationLookup.HasComponent(entity))
                    targetAnimationIndex = 5;
                else if (attackAnimation.ValueRO.TimeRemaining > 0f)
                    targetAnimationIndex = 4;
                else if (engageTargetLookup.HasComponent(entity))
                    targetAnimationIndex = 4;
                else if (moveVisual.ValueRO.IsMoving != 0)
                    targetAnimationIndex = autoWanderLookup.HasComponent(entity) ? (byte)2 : (byte)3;
            }

            bool resolvedChanged = resolvedAnimation.ValueRO.Value != targetAnimationIndex;
            if (resolvedChanged)
                resolvedAnimation.ValueRW.Value = targetAnimationIndex;

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
        }

        double elapsed = Time.realtimeSinceStartupAsDouble - startTime;
        if (EnableAnimationIndexFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
        {
            Debug.Log(
                $"[FreezeDetect:ECS] UnitAnimationIndexSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms " +
                $"complete={(afterCompleteTime - startTime) * 1000d:F1}ms units={checkedUnits} applied={appliedUnits}");
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

            animationIndex = (byte)(slotIndex + 1);
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
