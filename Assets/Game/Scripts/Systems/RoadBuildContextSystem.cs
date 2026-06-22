internal sealed class RoadBuildContextSystem
{
    public readonly struct Context
    {
        public readonly RoadBuildEcsBoundarySystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly RoadBuildEcsBoundarySystem.TryGetGridDataDelegate TryGetGridData;
        public readonly RoadBuildEcsBoundarySystem.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly uint BuildingSpawnRandomState;

        public Context(
            RoadBuildEcsBoundarySystem.TryGetEntityManagerDelegate tryGetEntityManager,
            RoadBuildEcsBoundarySystem.TryGetGridDataDelegate tryGetGridData,
            RoadBuildEcsBoundarySystem.GetFootprintCenterDelegate getFootprintCenter,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            uint buildingSpawnRandomState)
        {
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            GetFootprintCenter = getFootprintCenter;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            BuildingSpawnRandomState = buildingSpawnRandomState;
        }
    }

    public RoadBuildEcsBoundarySystem.Context CreateEcsContext(Context context)
    {
        return new RoadBuildEcsBoundarySystem.Context(
            context.TryGetEntityManager,
            context.TryGetGridData,
            context.GetFootprintCenter,
            context.BuildingPlacementInteractionSystem,
            context.BuildingPlacementInteractionContext,
            context.BuildingSpawnRandomState);
    }
}
