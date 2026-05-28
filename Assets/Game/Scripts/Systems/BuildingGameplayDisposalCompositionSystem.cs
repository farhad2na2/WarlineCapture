using System;

internal sealed class BuildingGameplayDisposalCompositionSystem
{
    public Action CreateDisposeAction(
        BuildingGameplayCompositionSourceSystem source,
        Func<BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        return () => source.BuildingGameplayDisposalSystem.Dispose(CreateSource(source, createPlacementCommandContext));
    }

    public BuildingGameplayDisposalSystem.Source CreateSource(
        BuildingGameplayCompositionSourceSystem source,
        Func<BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        return new BuildingGameplayDisposalSystem.Source(
            source.RuntimeBuildingSystem,
            source.BuildingPlacementStartupSystem,
            source.BuildingDefinitionSystem,
            source.BuildingPlacementPreviewSystem,
            source.BuildingRuntimeObjectSystem,
            source.UnitPathfindingPendingStateReadSystem,
            () => source.BuildingPlacementCommandSystem.ExitBuildMode(createPlacementCommandContext()));
    }
}
