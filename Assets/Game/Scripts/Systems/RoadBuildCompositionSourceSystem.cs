using Unity.Entities;

internal sealed partial class RoadBuildCompositionSourceSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    public readonly RoadBuildStartupSystem RoadBuildStartupSystem = new();
    public readonly RoadBuildDependencySystem RoadBuildDependencySystem = new();
    public readonly RoadBuildReadModelSystem RoadBuildReadModelSystem = new();
    public readonly RoadBuildVisualContextSystem RoadBuildVisualContextSystem;
    public readonly RoadBuildInteractionContextSystem RoadBuildInteractionContextSystem = new();
    public readonly RoadBuildRuntimeActionSystem RoadBuildRuntimeActionSystem;
    public readonly RoadBuildRuntimeActionSystem.State RoadBuildRuntimeActionState;
    public readonly RoadBuildDisposalSystem RoadBuildDisposalSystem = new();
    public readonly RoadBuildConfigSystem RoadBuildConfigSystem = new();
    public readonly RoadRuntimeRootSystem RoadRuntimeRootSystem;
    public readonly RoadNetworkSystem RoadNetworkSystem = new();
    public readonly RoadPathPlanningSystem RoadPathPlanningSystem = new();
    public readonly RoadSurfacePlacementSystem RoadSurfacePlacementSystem = new();
    public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
    public readonly RoadVisualVariantSystem RoadVisualVariantSystem;
    public readonly RoadVisualResolutionSystem RoadVisualResolutionSystem;
    public readonly RoadVisualRefreshSystem RoadVisualRefreshSystem;
    public readonly RoadChunkVisualSystem RoadChunkVisualSystem;
    public readonly RoadPreviewSystem RoadPreviewSystem;
    public readonly RoadSpecialVisualSystem RoadSpecialVisualSystem;
    public readonly RoadBuildSessionSystem RoadBuildSessionSystem = new();
    public readonly RoadBuildSessionSystem.State RoadBuildSessionState = new();
    public readonly RoadMinimapEventSystem RoadMinimapEventSystem;
    public readonly RoadBuildInputSystem RoadBuildInputSystem = new();
    public readonly RoadBuildInputSystem.State RoadBuildInputState = new();
    public readonly RoadBuildCommandSystem RoadBuildCommandSystem = new();
    public readonly RoadDeletePromptSystem RoadDeletePromptSystem = new();
    public readonly RoadBuildPlacementStorageSystem RoadBuildPlacementStorageSystem = new();
    public readonly RoadBuildDefinitionProjectionSystem RoadBuildDefinitionProjectionSystem = new();
    public readonly RoadBuildPlacementVisualSystem RoadBuildPlacementVisualSystem;
    public readonly RoadBuildBuildingPlacementSystem RoadBuildBuildingPlacementSystem = new();
    public readonly RoadBuildInteractionSystem RoadBuildInteractionSystem = new();
    public readonly RoadBuildContextSystem RoadBuildContextSystem = new();
    public readonly RoadBuildEcsBoundarySystem RoadBuildEcsBoundarySystem = new();
    public readonly RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem;
    public readonly RoadRuntimeGenerationContextSystem RoadRuntimeGenerationContextSystem;
    public readonly RoadBuildMutationSystem RoadBuildMutationSystem = new();
    public readonly RoadBuildCompositionContextSystem RoadBuildCompositionContextSystem = new();
    public readonly RoadBuildCompositionLifecycleSystem RoadBuildCompositionLifecycleSystem = new();

    public RoadBuildStartupSystem.State RoadBuildStartupState = new();
    public readonly RoadBuildDependencySystem.State RoadBuildDependencyState;
    public readonly RoadBuildPlacementVisualSystem.State RoadBuildPlacementVisualState;
    public readonly RoadBuildBuildingPlacementSystem.State RoadBuildPlacementState;
    public uint BuildingSpawnRandomState = 0x12345678u;

    public RoadBuildCompositionSourceSystem()
    {
        RoadGridProjectionSystem = ResolveRoadGridProjectionSystem();
        RoadRuntimeRootSystem = ResolveRoadRuntimeRootSystem();
        RoadVisualVariantSystem = ResolveRoadVisualVariantSystem();
        RoadBuildVisualContextSystem = ResolveRoadBuildVisualContextSystem();
        RoadVisualResolutionSystem = ResolveRoadVisualResolutionSystem();
        RoadVisualRefreshSystem = ResolveRoadVisualRefreshSystem();
        RoadChunkVisualSystem = ResolveRoadChunkVisualSystem();
        RoadSpecialVisualSystem = ResolveRoadSpecialVisualSystem();
        RoadPreviewSystem = ResolveRoadPreviewSystem();
        RoadRuntimeGenerationSystem = ResolveRoadRuntimeGenerationSystem();
        RoadRuntimeGenerationContextSystem = ResolveRoadRuntimeGenerationContextSystem();
        RoadMinimapEventSystem = ResolveRoadMinimapEventSystem();
        RoadBuildRuntimeActionSystem = ResolveRoadBuildRuntimeActionSystem();
        RoadBuildPlacementVisualSystem = ResolveRoadBuildPlacementVisualSystem();
        RoadBuildRuntimeActionState = global::RoadBuildRuntimeActionSystem.CreateState();
        RoadBuildDependencyState = RoadBuildDependencySystem.CreateState();
        RoadBuildPlacementVisualState = RoadBuildPlacementVisualSystem?.CreateState() ?? new RoadBuildPlacementVisualSystem.State();
        RoadBuildPlacementState = RoadBuildBuildingPlacementSystem.CreateState();
    }

    private static RoadGridProjectionSystem ResolveRoadGridProjectionSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadGridProjectionSystem>()
            : null;
    }

    private static RoadRuntimeRootSystem ResolveRoadRuntimeRootSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadRuntimeRootSystem>()
            : null;
    }

    private static RoadVisualVariantSystem ResolveRoadVisualVariantSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadVisualVariantSystem>()
            : null;
    }

    private static RoadBuildPlacementVisualSystem ResolveRoadBuildPlacementVisualSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadBuildPlacementVisualSystem>()
            : null;
    }

    private static RoadBuildVisualContextSystem ResolveRoadBuildVisualContextSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadBuildVisualContextSystem>()
            : null;
    }

    private static RoadVisualResolutionSystem ResolveRoadVisualResolutionSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadVisualResolutionSystem>()
            : null;
    }

    private static RoadVisualRefreshSystem ResolveRoadVisualRefreshSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadVisualRefreshSystem>()
            : null;
    }

    private static RoadChunkVisualSystem ResolveRoadChunkVisualSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadChunkVisualSystem>()
            : null;
    }

    private static RoadSpecialVisualSystem ResolveRoadSpecialVisualSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadSpecialVisualSystem>()
            : null;
    }

    private static RoadPreviewSystem ResolveRoadPreviewSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadPreviewSystem>()
            : null;
    }

    private static RoadRuntimeGenerationSystem ResolveRoadRuntimeGenerationSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadRuntimeGenerationSystem>()
            : null;
    }

    private static RoadRuntimeGenerationContextSystem ResolveRoadRuntimeGenerationContextSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadRuntimeGenerationContextSystem>()
            : null;
    }

    private static RoadMinimapEventSystem ResolveRoadMinimapEventSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadMinimapEventSystem>()
            : null;
    }

    private static RoadBuildRuntimeActionSystem ResolveRoadBuildRuntimeActionSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<RoadBuildRuntimeActionSystem>()
            : null;
    }
}
