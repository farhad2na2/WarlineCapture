internal sealed class RoadBuildDependencySystem
{
    internal sealed class State
    {
        public BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public MainMenuPlayUI MainMenuPlayUi;
        public RuntimeGridBlockerSystem RuntimeGridBlockers;
    }

    public State CreateState()
    {
        return new State();
    }

    public void BindBuildingInteraction(
        State state,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        state.BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        state.BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
    }

    public void BindDependencies(
        State state,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        MainMenuPlayUI mainMenuPlayUi,
        RuntimeGridBlockerSystem runtimeGridBlockers,
        RoadMinimapEventSystem roadMinimapEventSystem)
    {
        BindBuildingInteraction(
            state,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext);

        state.MainMenuPlayUi = mainMenuPlayUi;
        roadMinimapEventSystem.Configure(mainMenuPlayUi);
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
