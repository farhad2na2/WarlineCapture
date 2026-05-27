internal sealed class BuildingRoadLegacyContextSystem
{
    public readonly struct Context
    {
        public readonly BuildingRoadLegacyEcsSystem.TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingRoadLegacyEcsSystem.TryGetGridDataDelegate TryGetGridData;
        public readonly BuildingRoadLegacyEcsSystem.GetFootprintCenterDelegate GetFootprintCenter;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly uint BuildingSpawnRandomState;

        public Context(
            BuildingRoadLegacyEcsSystem.TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingRoadLegacyEcsSystem.TryGetGridDataDelegate tryGetGridData,
            BuildingRoadLegacyEcsSystem.GetFootprintCenterDelegate getFootprintCenter,
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

    public BuildingRoadLegacyEcsSystem.Context CreateEcsContext(Context context)
    {
        return new BuildingRoadLegacyEcsSystem.Context(
            context.TryGetEntityManager,
            context.TryGetGridData,
            context.GetFootprintCenter,
            context.BuildingPlacementInteractionSystem,
            context.BuildingPlacementInteractionContext,
            context.BuildingSpawnRandomState);
    }
}
