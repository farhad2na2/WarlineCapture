using System;

internal sealed class BuildingGameplayDisposalCompositionSystemHelper
{
    public Action CreateDisposeAction(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        return () => source.BuildingGameplayDisposalSystem.Dispose(CreateSource(source, createPlacementCommandContext));
    }

    public BuildingGameplayDisposalSystem.Source CreateSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        return new BuildingGameplayDisposalSystem.Source(
            source.RuntimeBuildingSystem,
            source.BuildingPlacementStartupSystem,
            source.BuildingDefinitionSystem,
            source.BuildingPlacementPreviewSystem,
            source.RuntimeObjectPresentationHelper,
            source.UnitPathfindingPendingStateReader,
            () => ExitBuildModeWithoutEntityManager(createPlacementCommandContext()));
    }

    private static void ExitBuildModeWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }
}
