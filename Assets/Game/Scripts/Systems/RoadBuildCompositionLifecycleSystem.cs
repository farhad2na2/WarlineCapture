using UnityEngine;

internal sealed class RoadBuildCompositionLifecycleSystem
{
    public void Init(
        RoadBuildCompositionSourceSystem source,
        RoadBuildCompositionContextSystem contextSystem,
        RoadBuildSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default)
    {
        source.RoadBuildStartupState = source.RoadBuildStartupSystem.Initialize(
            configAsset,
            sceneWorldCamera,
            runtimeRoot,
            source.RoadBuildConfigSystem,
            source.RoadRuntimeRootSystem,
            source.RoadVisualVariantSystem);
        source.RoadBuildDependencySystem.BindBuildingInteraction(
            source.RoadBuildDependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext);
        source.RoadBuildReadModelSystem.Configure(contextSystem.CreateRoadBuildReadModelContext(source));
        source.RoadBuildRuntimeActionSystem.ConfigureInput(
            source.RoadBuildRuntimeActionState,
            source.RoadBuildInteractionContextSystem,
            contextSystem.CreateRoadBuildInteractionContext(source),
            source.RoadBuildStartupState.WorldCamera);
        source.RoadBuildRuntimeActionSystem.ConfigureGui(
            source.RoadBuildRuntimeActionState,
            source.RoadDeletePromptSystem,
            contextSystem.CreateRoadDeletePromptContext(source));

        source.RoadBuildDefinitionProjectionSystem.BuildDefinitions(
            source.RoadBuildStartupState.SoldierBasePrefab,
            source.RoadBuildStartupState.SoldierBaseFootprintCells,
            source.RoadBuildPlacementStorageSystem);
        source.RoadBuildPlacementVisualSystem.CreatePlacementOutline(
            source.RoadBuildPlacementVisualState,
            source.RoadBuildStartupState.RuntimeRoot,
            source.RoadBuildStartupState.PlacementValidColor);
    }

    public void BindDependencies(
        RoadBuildCompositionSourceSystem source,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext = default,
        MainMenuPlayUI mainMenuPlayUi = null,
        RuntimeGridBlockerSystem runtimeGridBlockers = null)
    {
        source.RoadBuildDependencySystem.BindDependencies(
            source.RoadBuildDependencyState,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            mainMenuPlayUi,
            runtimeGridBlockers,
            source.RoadMinimapEventSystem);
    }

    public void Dispose(
        RoadBuildCompositionSourceSystem source,
        RoadBuildCompositionContextSystem contextSystem)
    {
        source.RoadBuildCommandSystem.ExitBuildMode(contextSystem.CreateRoadBuildCommandContext(source));
        source.RoadBuildSessionSystem.ResetSkipBuildClickFrames(source.RoadBuildSessionState);
        source.RoadBuildDisposalSystem.Dispose(contextSystem.CreateRoadBuildDisposalContext(source));
    }
}
