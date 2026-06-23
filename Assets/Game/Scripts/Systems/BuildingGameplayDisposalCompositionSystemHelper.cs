using System;

internal sealed class BuildingGameplayDisposalCompositionSystemHelper
{
    public Action CreateDisposeAction(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        return () => source.BuildingGameplayDisposalExecutionCompositionSystemHelper.Dispose(CreateSource(source, createPlacementCommandContext));
    }

    public BuildingGameplayDisposalExecutionCompositionSystemHelper.Source CreateSource(
        BuildingGameplaySourceCompositionSystemHelper source,
        Func<BuildingPlacementCommandSystem.Context> createPlacementCommandContext)
    {
        return new BuildingGameplayDisposalExecutionCompositionSystemHelper.Source(
            source.RuntimeBuildingSystem,
            source.BuildingPlacementStartupSystemHelper,
            source.BuildingDefinitionSystem,
            source.BuildingPlacementPreviewPresentationSystemHelper,
            source.RuntimeObjectPresentationHelper,
            source.UnitPathfindingPendingStateReader,
            () => ExitBuildModeWithoutEntityManager(createPlacementCommandContext()));
    }

    private static void ExitBuildModeWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }
}
