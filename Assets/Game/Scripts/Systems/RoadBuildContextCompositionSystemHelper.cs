internal sealed class RoadBuildContextCompositionSystemHelper
{
    public readonly struct Context
    {
        public readonly RoadBuildEcsBoundaryCompositionSystemHelper.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly RoadBuildEcsBoundaryCompositionSystemHelper.TryGetGridDataDelegate TryGetGridData;
        public readonly RoadBuildEcsBoundaryCompositionSystemHelper.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper BuildingPlacementInteractionBoundaryCompositionSystemHelper;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context BuildingPlacementInteractionContext;
        public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
        public readonly uint BuildingSpawnRandomState;

        public Context(
            RoadBuildEcsBoundaryCompositionSystemHelper.TryGetEntityManagerDelegate tryGetEntityManager,
            RoadBuildEcsBoundaryCompositionSystemHelper.TryGetGridDataDelegate tryGetGridData,
            RoadBuildEcsBoundaryCompositionSystemHelper.GetFootprintCenterDelegate getFootprintCenter,
            BuildingPlacementInteractionBoundaryCompositionSystemHelper buildingPlacementInteractionSystem,
            BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context buildingPlacementInteractionContext,
            RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
            uint buildingSpawnRandomState)
        {
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            GetFootprintCenter = getFootprintCenter;
            BuildingPlacementInteractionBoundaryCompositionSystemHelper = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
            BuildingSpawnRandomState = buildingSpawnRandomState;
        }
    }

    public RoadBuildEcsBoundaryCompositionSystemHelper.Context CreateEcsContext(Context context)
    {
        return new RoadBuildEcsBoundaryCompositionSystemHelper.Context(
            context.TryGetEntityManager,
            context.TryGetGridData,
            context.GetFootprintCenter,
            context.BuildingPlacementInteractionBoundaryCompositionSystemHelper,
            context.BuildingPlacementInteractionContext,
            context.RuntimeBuildingEntityLinks,
            context.BuildingSpawnRandomState);
    }
}
