using System;

namespace Game.Runtime
{
    internal sealed partial class BuildingGameplayDisposalCompositionSystemHelper
    {
        public BuildingGameplayDisposalExecutionCompositionSystemHelper.Source CreateSource(
            BuildingGameplaySourceCompositionSystemHelper source,
            Func<BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext)
        {
            return new BuildingGameplayDisposalExecutionCompositionSystemHelper.Source(
                source.RuntimeBuildingSystem,
                source.BuildingPlacementStartupSystemHelper,
                source.BuildingDefinitionPrefabSystemHelper,
                source.BuildingPlacementPreviewPresentationSystemHelper,
                source.BuildingPlacementVisualPresentationSystemHelper,
                source.RuntimeObjectPresentationHelper,
                source.UnitPathfindingPendingStateReader,
                () => ExitBuildModeWithoutEntityManager(createPlacementCommandContext()));
        }

        static void ExitBuildModeWithoutEntityManager(BuildingPlacementCommandRequestCompositionSystemHelper.Context context)
        {
            context.SessionSystem?.ExitBuildMode(context.SessionContext);
        }
    }
}
