namespace Game.Runtime
{
    internal sealed class RoadBuildContextCompositionSystemHelper
    {
        public readonly struct Context
        {
            public readonly RoadBuildEcsCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
            public readonly RoadBuildEcsCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
            public readonly RoadBuildEcsCompositionSystemHelper.GetFootprintCenterDelegate GetFootprintCenter;
            public readonly BuildingPlacementInteractionCompositionSystemHelper BuildingPlacementInteractionCompositionSystemHelper;
            public readonly BuildingPlacementInteractionCompositionSystemHelper.Context BuildingPlacementInteractionContext;
            public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
            public readonly uint BuildingSpawnRandomState;

            public Context(
                RoadBuildEcsCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
                RoadBuildEcsCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
                RoadBuildEcsCompositionSystemHelper.GetFootprintCenterDelegate getFootprintCenter,
                BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteractionSystem,
                BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext,
                RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
                uint buildingSpawnRandomState)
            {
                TryGetEntityManager = tryGetEntityManager;
                TryGetGridData = tryGetGridData;
                GetFootprintCenter = getFootprintCenter;
                BuildingPlacementInteractionCompositionSystemHelper = buildingPlacementInteractionSystem;
                BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
                RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
                BuildingSpawnRandomState = buildingSpawnRandomState;
            }
        }

        public RoadBuildEcsCompositionSystemHelper.Context CreateEcsContext(Context context)
        {
            return new RoadBuildEcsCompositionSystemHelper.Context(
                context.TryGetEntityManager,
                context.TryGetGridData,
                context.GetFootprintCenter,
                context.BuildingPlacementInteractionCompositionSystemHelper,
                context.BuildingPlacementInteractionContext,
                context.RuntimeBuildingEntityLinks,
                context.BuildingSpawnRandomState);
        }
    }
}
