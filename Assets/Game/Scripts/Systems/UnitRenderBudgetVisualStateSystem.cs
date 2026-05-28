using Unity.Entities;
using UnityEngine;

public readonly struct UnitRenderBudgetVisualStateSystem
{
    private const int VisualTransitionStableFrames = 2;
    private const int MaxVisualStateTransitionsPerUpdate = 32;

    public UnitRenderVisualKind ResolveStableUnitRenderVisualState(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity unit,
        UnitRenderVisualKind desiredVisual,
        bool forceImmediate,
        ref int visualStateChanges,
        ref int visualStatePending,
        ref int visualTransitionsCommitted)
    {
        byte desired = (byte)desiredVisual;
        if (!em.HasComponent<UnitRenderVisualState>(unit))
        {
            ecb.AddComponent(unit, new UnitRenderVisualState
            {
                Current = desired,
                Desired = desired,
                LastChangedFrame = Time.frameCount
            });
            visualStateChanges++;
            return desiredVisual;
        }

        UnitRenderVisualState state = em.GetComponentData<UnitRenderVisualState>(unit);
        if (state.Desired != desired)
        {
            state.Desired = desired;
            state.LastChangedFrame = Time.frameCount;
            if (forceImmediate)
                state.Current = desired;
            em.SetComponentData(unit, state);
            visualStateChanges++;
            if (forceImmediate)
            {
                visualTransitionsCommitted++;
                return desiredVisual;
            }

            visualStatePending++;
            return (UnitRenderVisualKind)state.Current;
        }

        if (state.Current == desired)
            return (UnitRenderVisualKind)state.Current;

        visualStatePending++;
        if (forceImmediate)
        {
            state.Current = desired;
            state.Desired = desired;
            state.LastChangedFrame = Time.frameCount;
            em.SetComponentData(unit, state);
            visualStateChanges++;
            visualTransitionsCommitted++;
            return desiredVisual;
        }

        bool stableLongEnough = Time.frameCount - state.LastChangedFrame >= VisualTransitionStableFrames;
        bool transitionBudgetAvailable = visualTransitionsCommitted < MaxVisualStateTransitionsPerUpdate;
        if (!stableLongEnough || !transitionBudgetAvailable)
            return (UnitRenderVisualKind)state.Current;

        state.Current = desired;
        em.SetComponentData(unit, state);
        visualStateChanges++;
        visualTransitionsCommitted++;
        return desiredVisual;
    }
}
