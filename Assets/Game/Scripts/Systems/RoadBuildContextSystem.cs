internal sealed class RoadBuildContextSystem
{
    public readonly struct Context
    {
        public readonly RoadBuildEcsBoundarySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly RoadBuildEcsBoundarySystem.TryGetGridDataDelegate TryGetGridData;
        public readonly RoadBuildEcsBoundarySystem.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper BuildingPlacementInteractionBoundaryCompositionSystemHelper;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context BuildingPlacementInteractionContext;
        public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
        public readonly uint BuildingSpawnRandomState;

        public Context(
            RoadBuildEcsBoundarySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            RoadBuildEcsBoundarySystem.TryGetGridDataDelegate tryGetGridData,
            RoadBuildEcsBoundarySystem.GetFootprintCenterDelegate getFootprintCenter,
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

    public RoadBuildEcsBoundarySystem.Context CreateEcsContext(Context context)
    {
        return new RoadBuildEcsBoundarySystem.Context(
            context.TryGetEntityManager,
            context.TryGetGridData,
            context.GetFootprintCenter,
            context.BuildingPlacementInteractionBoundaryCompositionSystemHelper,
            context.BuildingPlacementInteractionContext,
            context.RuntimeBuildingEntityLinks,
            context.BuildingSpawnRandomState);
    }
}
