internal sealed class RoadBuildDependencyCompositionSystemHelper
{
    internal sealed class State
    {
        public BuildingPlacementInteractionBoundaryCompositionSystemHelper BuildingPlacementInteractionBoundaryCompositionSystemHelper;
        public BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context BuildingPlacementInteractionContext;
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
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteractionSystem,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext)
    {
        state.BuildingPlacementInteractionBoundaryCompositionSystemHelper = buildingPlacementInteractionSystem;
        state.BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
    }

    public void BindDependencies(
        State state,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteractionSystem,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext,
        IMatchRuntimeUi mainMenuPlayUi,
        RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
        RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
        RoadMinimapEventSystem roadMinimapEventSystem)
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
