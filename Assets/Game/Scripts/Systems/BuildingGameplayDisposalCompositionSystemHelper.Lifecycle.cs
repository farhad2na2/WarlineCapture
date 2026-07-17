using System;

namespace Game.Runtime
{
    internal sealed partial class BuildingGameplayDisposalCompositionSystemHelper
    {
        public Action CreateDisposeAction(
            BuildingGameplaySourceCompositionSystemHelper source,
            Func<BuildingPlacementCommandRequestCompositionSystemHelper.Context> createPlacementCommandContext)
        {
            return () =>
            {
                try
                {
                    source.BuildingGameplayDisposalExecutionCompositionSystemHelper.Dispose(
                        CreateSource(source, createPlacementCommandContext));
                }
                finally
                {
                    try
                    {
                        source.BuildingResourceHaulerBridgeCompositionSystemHelper.Dispose();
                    }
                    finally
                    {
                        source.FactionResourceCompositionSystemHelper.Dispose();
                    }
                }
            };
        }
    }
}
