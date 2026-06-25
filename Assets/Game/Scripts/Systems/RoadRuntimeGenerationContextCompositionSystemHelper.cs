internal sealed class RoadRuntimeGenerationContextCompositionSystemHelper
{
    public readonly struct Context
    {
        public readonly RoadRuntimeGenerationCompositionSystemHelper.TryGetRoadCellSizeInGridCellsDelegate TryGetRoadCellSizeInGridCells;
        public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
        public readonly RoadGridProjectionSystem.Context RoadGridProjectionContext;
        public readonly RoadRuntimeGenerationCompositionSystemHelper.CreateStrokeDelegate CreateStroke;
        public readonly RoadSpecialVisualSystem SpecialVisualSystem;
        public readonly RoadSpecialVisualSystem.Context SpecialVisualContext;

        public Context(
            RoadRuntimeGenerationCompositionSystemHelper.TryGetRoadCellSizeInGridCellsDelegate tryGetRoadCellSizeInGridCells,
            RoadGridProjectionSystem roadGridProjectionSystem,
            RoadGridProjectionSystem.Context roadGridProjectionContext,
            RoadRuntimeGenerationCompositionSystemHelper.CreateStrokeDelegate createStroke,
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

    public static RoadRuntimeGenerationCompositionSystemHelper.Context CreateContext(Context context)
    {
        return new RoadRuntimeGenerationCompositionSystemHelper.Context(
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
