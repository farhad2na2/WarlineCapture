internal sealed class RoadRuntimeGenerationContextCompositionSystemHelper
{
    public readonly struct Context
    {
        public readonly RoadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCellsDelegate TryGetRoadCellSizeInGridCells;
        public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
        public readonly RoadGridProjectionSystem.Context RoadGridProjectionContext;
        public readonly RoadRuntimeGenerationSystem.CreateStrokeDelegate CreateStroke;
        public readonly RoadSpecialVisualSystem SpecialVisualSystem;
        public readonly RoadSpecialVisualSystem.Context SpecialVisualContext;

        public Context(
            RoadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCellsDelegate tryGetRoadCellSizeInGridCells,
            RoadGridProjectionSystem roadGridProjectionSystem,
            RoadGridProjectionSystem.Context roadGridProjectionContext,
            RoadRuntimeGenerationSystem.CreateStrokeDelegate createStroke,
            RoadSpecialVisualSystem specialVisualSystem,
            RoadSpecialVisualSystem.Context specialVisualContext)
        {
            TryGetRoadCellSizeInGridCells = tryGetRoadCellSizeInGridCells;
            RoadGridProjectionSystem = roadGridProjectionSystem;
            RoadGridProjectionContext = roadGridProjectionContext;
            CreateStroke = createStroke;
            SpecialVisualSystem = specialVisualSystem;
            SpecialVisualContext = specialVisualContext;
        }
    }

    public static RoadRuntimeGenerationSystem.Context CreateContext(Context context)
    {
        return new RoadRuntimeGenerationSystem.Context(
            context.TryGetRoadCellSizeInGridCells,
            () => BeginDeferredRoadEcsSync(context),
            () => EndDeferredRoadEcsSync(context),
            context.CreateStroke,
            context.SpecialVisualSystem,
            context.SpecialVisualContext);
    }

    public static void BeginDeferredRoadEcsSync(Context context)
    {
        context.RoadGridProjectionSystem?.BeginDeferredRoadEcsSync();
    }

    public static void EndDeferredRoadEcsSync(Context context)
    {
        context.RoadGridProjectionSystem?.EndDeferredRoadEcsSync(context.RoadGridProjectionContext);
    }
}
