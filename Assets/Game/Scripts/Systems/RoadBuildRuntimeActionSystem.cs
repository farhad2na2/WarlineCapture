using UnityEngine;

internal sealed class RoadBuildRuntimeActionSystem
{
    internal sealed class State
    {
        public RoadBuildInteractionContextSystem InteractionContextSystem;
        public RoadBuildInteractionContextSystem.Context InteractionContext;
        public RoadDeletePromptSystem DeletePromptSystem;
        public RoadDeletePromptSystem.Context DeletePromptContext;
        public Camera WorldCamera;
    }

    public State CreateState()
    {
        return new State();
    }

    public void ConfigureGui(
        State state,
        RoadDeletePromptSystem deletePromptSystem,
        RoadDeletePromptSystem.Context deletePromptContext)
    {
        if (state == null)
            return;

        state.DeletePromptSystem = deletePromptSystem;
        state.DeletePromptContext = deletePromptContext;
    }

    public void ConfigureInput(
        State state,
        RoadBuildInteractionContextSystem interactionContextSystem,
        RoadBuildInteractionContextSystem.Context interactionContext,
        Camera worldCamera)
    {
        if (state == null)
            return;

        state.InteractionContextSystem = interactionContextSystem;
        state.InteractionContext = interactionContext;
        state.WorldCamera = worldCamera;
    }

    public void Update(State state)
    {
        if (state?.InteractionContextSystem == null)
            return;

        state.InteractionContext.InputSystem.Update(
            state.InteractionContextSystem.CreateInputContext(state.InteractionContext),
            state.WorldCamera);
    }

    public void OnGui(State state)
    {
        state?.DeletePromptSystem?.OnGui(state.DeletePromptContext);
    }
}
