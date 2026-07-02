using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RoadBuildRuntimeActionCompositionSystemHelper
    {
        internal sealed class State
        {
            public RoadBuildInteractionContextCompositionSystemHelper InteractionContextSystem;
            public RoadBuildInteractionContextCompositionSystemHelper.Context InteractionContext;
            public RoadBuildCommandCompositionSystemHelper CommandSystem;
            public RoadBuildCommandCompositionSystemHelper.Context CommandContext;
            public RoadBuildEcsCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public RoadDeletePromptUiSystemHelper DeletePromptSystem;
            public RoadDeletePromptUiSystemHelper.Context DeletePromptContext;
            public Camera WorldCamera;
        }

        public static State CreateState()
        {
            return new State();
        }

        public static void ConfigureGui(
            State state,
            RoadDeletePromptUiSystemHelper deletePromptSystem,
            RoadDeletePromptUiSystemHelper.Context deletePromptContext)
        {
            if (state == null)
                return;

            state.DeletePromptSystem = deletePromptSystem;
            state.DeletePromptContext = deletePromptContext;
        }

        public static void ConfigureInput(
            State state,
            RoadBuildInteractionContextCompositionSystemHelper interactionContextSystem,
            RoadBuildInteractionContextCompositionSystemHelper.Context interactionContext,
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
            RoadBuildCommandCompositionSystemHelper commandSystem,
            RoadBuildCommandCompositionSystemHelper.Context commandContext,
            RoadBuildEcsCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager)
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
}
