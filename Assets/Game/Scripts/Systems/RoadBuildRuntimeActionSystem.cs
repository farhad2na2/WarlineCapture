using Unity.Entities;
using UnityEngine;

internal sealed partial class RoadBuildRuntimeActionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    internal sealed class State
    {
        public RoadBuildInteractionContextSystem InteractionContextSystem;
        public RoadBuildInteractionContextSystem.Context InteractionContext;
        public RoadBuildCommandSystem CommandSystem;
        public RoadBuildCommandSystem.Context CommandContext;
        public RoadBuildEcsBoundarySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public RoadDeletePromptSystem DeletePromptSystem;
        public RoadDeletePromptSystem.Context DeletePromptContext;
        public Camera WorldCamera;
    }

    public static State CreateState()
    {
        return new State();
    }

    public static void ConfigureGui(
        State state,
        RoadDeletePromptSystem deletePromptSystem,
        RoadDeletePromptSystem.Context deletePromptContext)
    {
        if (state == null)
            return;

        state.DeletePromptSystem = deletePromptSystem;
        state.DeletePromptContext = deletePromptContext;
    }

    public static void ConfigureInput(
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

    public static void ConfigureCommands(
        State state,
        RoadBuildCommandSystem commandSystem,
        RoadBuildCommandSystem.Context commandContext,
        RoadBuildEcsBoundarySystem.TryGetEntityManagerDelegate tryGetEntityManager)
    {
        if (state == null)
            return;

        state.CommandSystem = commandSystem;
        state.CommandContext = commandContext;
        state.TryGetEntityManager = tryGetEntityManager;
    }

    public static void Update(State state)
    {
        ProcessCommandQueue(state);

        if (state?.InteractionContextSystem == null)
            return;

        state.InteractionContext.InputSystem.Update(
            state.InteractionContextSystem.CreateInputContext(state.InteractionContext),
            state.WorldCamera);
    }

    public static void OnGui(State state)
    {
        state?.DeletePromptSystem?.OnGui(state.DeletePromptContext);
    }

    private static void ProcessCommandQueue(State state)
    {
        if (state?.CommandSystem == null ||
            state.TryGetEntityManager == null ||
            !state.TryGetEntityManager(out EntityManager em))
        {
            return;
        }

        state.CommandSystem.ProcessPendingRoadBuildCommands(em, state.CommandContext);
    }
}
