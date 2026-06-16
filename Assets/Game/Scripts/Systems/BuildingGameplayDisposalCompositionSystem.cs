using System;
using Unity.Entities;

internal sealed partial class BuildingGameplayDisposalCompositionSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

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
            source.UnitPathfindingPendingStateReader,
            () => ExitBuildModeWithoutEntityManager(createPlacementCommandContext()));
    }

    private static void ExitBuildModeWithoutEntityManager(BuildingPlacementCommandSystem.Context context)
    {
        context.SessionSystem?.ExitBuildMode(context.SessionContext);
    }
}
