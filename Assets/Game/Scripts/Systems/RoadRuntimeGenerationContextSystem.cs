internal sealed class RoadRuntimeGenerationContextSystem
{
    public readonly struct Context
    {
        public readonly RoadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCellsDelegate TryGetRoadCellSizeInGridCells;
        public readonly RoadRuntimeGenerationSystem.RuntimeAction BeginDeferredRoadEcsSync;
        public readonly RoadRuntimeGenerationSystem.RuntimeAction EndDeferredRoadEcsSync;
        public readonly RoadRuntimeGenerationSystem.CreateStrokeDelegate CreateStroke;
        public readonly RoadSpecialVisualSystem SpecialVisualSystem;
        public readonly RoadSpecialVisualSystem.Context SpecialVisualContext;

        public Context(
            RoadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCellsDelegate tryGetRoadCellSizeInGridCells,
            RoadRuntimeGenerationSystem.RuntimeAction beginDeferredRoadEcsSync,
            RoadRuntimeGenerationSystem.RuntimeAction endDeferredRoadEcsSync,
            RoadRuntimeGenerationSystem.CreateStrokeDelegate createStroke,
            RoadSpecialVisualSystem specialVisualSystem,
            RoadSpecialVisualSystem.Context specialVisualContext)
        {
            TryGetRoadCellSizeInGridCells = tryGetRoadCellSizeInGridCells;
            BeginDeferredRoadEcsSync = beginDeferredRoadEcsSync;
            EndDeferredRoadEcsSync = endDeferredRoadEcsSync;
            CreateStroke = createStroke;
            SpecialVisualSystem = specialVisualSystem;
            SpecialVisualContext = specialVisualContext;
        }
    }

    public RoadRuntimeGenerationSystem.Context CreateContext(Context context)
    {
        return new RoadRuntimeGenerationSystem.Context(
            context.TryGetRoadCellSizeInGridCells,
            context.BeginDeferredRoadEcsSync,
            context.EndDeferredRoadEcsSync,
            context.CreateStroke,
            context.SpecialVisualSystem,
            context.SpecialVisualContext);
    }
}
