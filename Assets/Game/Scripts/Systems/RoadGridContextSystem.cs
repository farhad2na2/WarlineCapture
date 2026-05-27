internal sealed class RoadGridContextSystem
{
    public readonly struct Context
    {
        public readonly RoadNetworkSystem RoadNetworkSystem;
        public readonly RoadSpecialVisualSystem RoadSpecialVisualSystem;
        public readonly RoadVisualVariantSystem RoadVisualVariantSystem;
        public readonly RoadFootprintQuerySystem RoadFootprintQuerySystem;
        public readonly RoadBuildStartupSystem.State StartupState;

        public Context(
            RoadNetworkSystem roadNetworkSystem,
            RoadSpecialVisualSystem roadSpecialVisualSystem,
            RoadVisualVariantSystem roadVisualVariantSystem,
            RoadFootprintQuerySystem roadFootprintQuerySystem,
            RoadBuildStartupSystem.State startupState)
        {
            RoadNetworkSystem = roadNetworkSystem;
            RoadSpecialVisualSystem = roadSpecialVisualSystem;
            RoadVisualVariantSystem = roadVisualVariantSystem;
            RoadFootprintQuerySystem = roadFootprintQuerySystem;
            StartupState = startupState;
        }
    }

    public RoadFootprintQuerySystem.Context CreateFootprintQueryContext(Context context)
    {
        return new RoadFootprintQuerySystem.Context(
            context.RoadNetworkSystem.RoadTiles,
            context.RoadSpecialVisualSystem.SpecialRoadObjects,
            context.RoadVisualVariantSystem.VisualData,
            context.StartupState.GridOrigin,
            context.StartupState.BuildPlaneY,
            context.StartupState.RoadGridSize);
    }

    public RoadGridProjectionSystem.Context CreateGridProjectionContext(Context context)
    {
        return new RoadGridProjectionSystem.Context(
            context.RoadNetworkSystem.RoadTiles,
            context.RoadFootprintQuerySystem,
            CreateFootprintQueryContext(context),
            context.StartupState.RoadGridSize);
    }
}
