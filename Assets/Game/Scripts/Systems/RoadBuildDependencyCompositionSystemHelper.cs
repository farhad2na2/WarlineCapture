using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.Runtime
{
    internal sealed class RoadBuildDependencyCompositionSystemHelper
    {
        internal sealed class State
        {
            public BuildingPlacementInteractionCompositionSystemHelper BuildingPlacementInteractionCompositionSystemHelper;
            public BuildingPlacementInteractionCompositionSystemHelper.Context BuildingPlacementInteractionContext;
            public RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
            public IMatchRuntimeUi MainMenuPlayUi;
            public RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers;
        }

        public State CreateState()
        {
            return new State();
        }

        public void BindBuildingInteraction(
            State state,
            BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteractionSystem,
            BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext)
        {
            state.BuildingPlacementInteractionCompositionSystemHelper = buildingPlacementInteractionSystem;
            state.BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
        }

        public void BindDependencies(
            State state,
            BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteractionSystem,
            BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext,
            IMatchRuntimeUi mainMenuPlayUi,
            RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
            RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
            RoadMinimapEventUiSystemHelper roadMinimapEventSystem)
        {
            BindBuildingInteraction(
                state,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext);

            state.MainMenuPlayUi = mainMenuPlayUi;
            if (runtimeBuildingEntityLinks != null)
                state.RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
            roadMinimapEventSystem?.Configure(mainMenuPlayUi);
            if (runtimeGridBlockers != null)
                state.RuntimeGridBlockers = runtimeGridBlockers;
        }

        public void ApplyBuildCommandMode(State state)
        {
            state?.MainMenuPlayUi?.ApplyMatchHudCommandMode(TacticalCommandMode.Build);
        }

        public void ClearCommandMode(State state)
        {
            state?.MainMenuPlayUi?.ClearMatchHudCommandMode();
        }
    }
}
