internal sealed class RoadBuildCompositionSourceSystem
{
    public RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    public readonly RoadBuildStartupSystem RoadBuildStartupSystem = new();
    public readonly RoadBuildDependencyCompositionSystemHelper RoadBuildDependencyCompositionSystemHelper = new();
    public readonly RoadBuildReadModelCompositionSystemHelper RoadBuildReadModelCompositionSystemHelper = new();
    public readonly RoadBuildVisualContextSystem RoadBuildVisualContextSystem;
    public readonly RoadBuildInteractionContextSystem RoadBuildInteractionContextSystem = new();
    public readonly RoadBuildRuntimeActionCompositionSystemHelper RoadBuildRuntimeActionCompositionSystemHelper;
    public readonly RoadBuildRuntimeActionCompositionSystemHelper.State RoadBuildRuntimeActionState;
    public readonly RoadBuildDisposalCompositionSystemHelper RoadBuildDisposalCompositionSystemHelper = new();
    public readonly RoadBuildConfigSystem RoadBuildConfigSystem = new();
    public readonly RoadRuntimeRootSystem RoadRuntimeRootSystem;
    public readonly RoadNetworkCompositionSystemHelper RoadNetworkCompositionSystemHelper = new();
    public readonly RoadPathPlanningUtilitySystemHelper RoadPathPlanningUtilitySystemHelper = new();
    public readonly RoadSurfacePlacementSystem RoadSurfacePlacementSystem = new();
    public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
    public readonly RoadVisualVariantSystem RoadVisualVariantSystem;
    public readonly RoadVisualResolutionSystem RoadVisualResolutionSystem;
    public readonly RoadVisualRefreshSystem RoadVisualRefreshSystem;
    public readonly RoadChunkVisualSystem RoadChunkVisualSystem;
    public readonly RoadPreviewPresentationSystemHelper RoadPreviewPresentationSystemHelper;
    public readonly RoadSpecialVisualSystem RoadSpecialVisualSystem;
    public readonly RoadBuildSessionCompositionSystemHelper RoadBuildSessionCompositionSystemHelper = new();
    public readonly RoadBuildSessionCompositionSystemHelper.State RoadBuildSessionState = new();
    public readonly RoadMinimapEventUiSystemHelper RoadMinimapEventUiSystemHelper;
    public readonly RoadBuildInputCompositionSystemHelper RoadBuildInputCompositionSystemHelper = new();
    public readonly RoadBuildInputCompositionSystemHelper.State RoadBuildInputState = new();
    public readonly RoadBuildCommandCompositionSystemHelper RoadBuildCommandCompositionSystemHelper = new();
    public readonly RoadDeletePromptUiSystemHelper RoadDeletePromptUiSystemHelper = new();
    public readonly RoadBuildPlacementStorageCompositionSystemHelper RoadBuildPlacementStorageCompositionSystemHelper = new();
    public readonly RoadBuildDefinitionProjectionSystem RoadBuildDefinitionProjectionSystem = new();
    public readonly RoadBuildPlacementVisualSystem RoadBuildPlacementVisualSystem;
    public readonly RoadBuildBuildingPlacementCompositionSystemHelper RoadBuildBuildingPlacementCompositionSystemHelper = new();
    public readonly RoadBuildInteractionCompositionSystemHelper RoadBuildInteractionCompositionSystemHelper = new();
    public readonly RoadBuildContextSystem RoadBuildContextSystem = new();
    public readonly RoadBuildEcsBoundaryCompositionSystemHelper RoadBuildEcsBoundaryCompositionSystemHelper = new();
    public readonly RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem;
    public readonly RoadRuntimeGenerationContextSystem RoadRuntimeGenerationContextSystem;
    public readonly RoadBuildMutationCompositionSystemHelper RoadBuildMutationCompositionSystemHelper = new();
    public readonly RoadBuildCompositionContextCompositionSystemHelper RoadBuildCompositionContextCompositionSystemHelper = new();
    public readonly RoadBuildCompositionLifecycleCompositionSystemHelper RoadBuildCompositionLifecycleCompositionSystemHelper = new();

    public RoadBuildStartupSystem.State RoadBuildStartupState = new();
    public readonly RoadBuildDependencyCompositionSystemHelper.State RoadBuildDependencyState;
    public readonly RoadBuildPlacementVisualSystem.State RoadBuildPlacementVisualState;
    public readonly RoadBuildBuildingPlacementCompositionSystemHelper.State RoadBuildPlacementState;
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
        RoadPreviewPresentationSystemHelper = ResolveRoadPreviewPresentationSystemHelper();
        RoadRuntimeGenerationSystem = ResolveRoadRuntimeGenerationSystem();
        RoadRuntimeGenerationContextSystem = ResolveRoadRuntimeGenerationContextSystem();
        RoadMinimapEventUiSystemHelper = ResolveRoadMinimapEventUiSystemHelper();
        RoadBuildRuntimeActionCompositionSystemHelper = ResolveRoadBuildRuntimeActionCompositionSystemHelper();
        RoadBuildPlacementVisualSystem = ResolveRoadBuildPlacementVisualSystem();
        RoadBuildRuntimeActionState = global::RoadBuildRuntimeActionCompositionSystemHelper.CreateState();
        RoadBuildDependencyState = RoadBuildDependencyCompositionSystemHelper.CreateState();
        RoadBuildPlacementVisualState = RoadBuildPlacementVisualSystem?.CreateState() ?? new RoadBuildPlacementVisualSystem.State();
        RoadBuildPlacementState = RoadBuildBuildingPlacementCompositionSystemHelper.CreateState();
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
        return new RoadRuntimeRootSystem();
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
        return new RoadBuildVisualContextSystem();
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
        return new RoadVisualRefreshSystem();
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

    private static RoadPreviewPresentationSystemHelper ResolveRoadPreviewPresentationSystemHelper()
    {
        return new RoadPreviewPresentationSystemHelper();
    }

    private static RoadRuntimeGenerationSystem ResolveRoadRuntimeGenerationSystem()
    {
        return new RoadRuntimeGenerationSystem();
    }

    private static RoadRuntimeGenerationContextSystem ResolveRoadRuntimeGenerationContextSystem()
    {
        return new RoadRuntimeGenerationContextSystem();
    }

    private static RoadMinimapEventUiSystemHelper ResolveRoadMinimapEventUiSystemHelper()
    {
        return new RoadMinimapEventUiSystemHelper();
    }

    private static RoadBuildRuntimeActionCompositionSystemHelper ResolveRoadBuildRuntimeActionCompositionSystemHelper()
    {
        return new RoadBuildRuntimeActionCompositionSystemHelper();
    }
}
