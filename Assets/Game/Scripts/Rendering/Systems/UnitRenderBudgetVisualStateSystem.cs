using Unity.Entities;

public readonly struct UnitRenderBudgetVisualState
{
    private const int VisualTransitionStableFrames = 2;
    private const int MaxVisualStateTransitionsPerUpdate = 32;

    public UnitRenderVisualKind ResolveStableUnitRenderVisualState(
        ComponentLookup<UnitRenderVisualComponent> visualStateLookup,
        EntityCommandBuffer ecb,
        Entity unit,
        UnitRenderVisualKind desiredVisual,
        bool forceImmediate,
        int currentFrame,
        ref int visualStateChanges,
        ref int visualStatePending,
        ref int visualTransitionsCommitted)
    {
        byte desired = (byte)desiredVisual;
        if (!visualStateLookup.HasComponent(unit))
        {
            ecb.AddComponent(unit, new UnitRenderVisualComponent
            {
                Current = desired,
                Desired = desired,
                LastChangedFrame = currentFrame
            });
            visualStateChanges++;
            return desiredVisual;
        }

        UnitRenderVisualComponent state = visualStateLookup[unit];
        if (state.Desired != desired)
        {
            state.Desired = desired;
            state.LastChangedFrame = currentFrame;
            if (forceImmediate)
                state.Current = desired;
            ecb.SetComponent(unit, state);
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
            state.LastChangedFrame = currentFrame;
            ecb.SetComponent(unit, state);
            visualStateChanges++;
            visualTransitionsCommitted++;
            return desiredVisual;
        }

        bool stableLongEnough = currentFrame - state.LastChangedFrame >= VisualTransitionStableFrames;
        bool transitionBudgetAvailable = visualTransitionsCommitted < MaxVisualStateTransitionsPerUpdate;
        if (!stableLongEnough || !transitionBudgetAvailable)
            return (UnitRenderVisualKind)state.Current;

        state.Current = desired;
        ecb.SetComponent(unit, state);
        visualStateChanges++;
        visualTransitionsCommitted++;
        return desiredVisual;
    }
}
