internal sealed class RoadBuildCompositionSourceSystem
{
    public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    public readonly RoadBuildStartupSystem RoadBuildStartupSystem = new();
    public readonly RoadBuildDependencySystem RoadBuildDependencySystem = new();
    public readonly RoadBuildReadModelSystem RoadBuildReadModelSystem = new();
    public readonly RoadBuildVisualContextSystem RoadBuildVisualContextSystem = new();
    public readonly RoadBuildInteractionContextSystem RoadBuildInteractionContextSystem = new();
    public readonly RoadBuildRuntimeActionSystem RoadBuildRuntimeActionSystem = new();
    public readonly RoadBuildRuntimeActionSystem.State RoadBuildRuntimeActionState;
    public readonly RoadBuildDisposalSystem RoadBuildDisposalSystem = new();
    public readonly RoadGridContextSystem RoadGridContextSystem = new();
    public readonly RoadBuildConfigSystem RoadBuildConfigSystem = new();
    public readonly RoadRuntimeRootSystem RoadRuntimeRootSystem = new();
    public readonly RoadNetworkSystem RoadNetworkSystem = new();
    public readonly RoadPathPlanningSystem RoadPathPlanningSystem = new();
    public readonly RoadSurfacePlacementSystem RoadSurfacePlacementSystem = new();
    public readonly RoadFootprintQuerySystem RoadFootprintQuerySystem = new();
    public readonly RoadGridProjectionSystem RoadGridProjectionSystem = new();
    public readonly RoadVisualVariantSystem RoadVisualVariantSystem = new();
    public readonly RoadVisualResolutionSystem RoadVisualResolutionSystem = new();
    public readonly RoadVisualRefreshSystem RoadVisualRefreshSystem = new();
    public readonly RoadChunkVisualSystem RoadChunkVisualSystem = new();
    public readonly RoadPreviewSystem RoadPreviewSystem = new();
    public readonly RoadSpecialVisualSystem RoadSpecialVisualSystem = new();
    public readonly RoadBuildSessionSystem RoadBuildSessionSystem = new();
    public readonly RoadBuildSessionSystem.State RoadBuildSessionState = new();
    public readonly RoadMinimapEventSystem RoadMinimapEventSystem = new();
    public readonly RoadBuildInputSystem RoadBuildInputSystem = new();
    public readonly RoadBuildInputSystem.State RoadBuildInputState = new();
    public readonly RoadBuildCommandSystem RoadBuildCommandSystem = new();
    public readonly RoadDeletePromptSystem RoadDeletePromptSystem = new();
    public readonly RoadBuildPlacementStorageSystem RoadBuildPlacementStorageSystem = new();
    public readonly RoadBuildDefinitionProjectionSystem RoadBuildDefinitionProjectionSystem = new();
    public readonly RoadBuildPlacementVisualSystem RoadBuildPlacementVisualSystem = new();
    public readonly RoadBuildBuildingPlacementSystem RoadBuildBuildingPlacementSystem = new();
    public readonly RoadBuildInteractionSystem RoadBuildInteractionSystem = new();
    public readonly RoadBuildGridQuerySystem RoadBuildGridQuerySystem = new();
    public readonly RoadBuildContextSystem RoadBuildContextSystem = new();
    public readonly RoadBuildEcsBoundarySystem RoadBuildEcsBoundarySystem = new();
    public readonly RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem = new();
    public readonly RoadRuntimeGenerationContextSystem RoadRuntimeGenerationContextSystem = new();
    public readonly RoadBuildMutationSystem RoadBuildMutationSystem = new();
    public readonly RoadBuildCompositionContextSystem RoadBuildCompositionContextSystem = new();
    public readonly RoadBuildCompositionLifecycleSystem RoadBuildCompositionLifecycleSystem = new();

    public RoadBuildStartupSystem.State RoadBuildStartupState = new();
    public readonly RoadBuildDependencySystem.State RoadBuildDependencyState;
    public readonly RoadBuildPlacementVisualSystem.State RoadBuildPlacementVisualState;
    public readonly RoadBuildBuildingPlacementSystem.State RoadBuildPlacementState;
    public readonly RoadBuildGridQuerySystem.State RoadBuildGridState;
    public uint BuildingSpawnRandomState = 0x12345678u;

    public RoadBuildCompositionSourceSystem()
    {
        RoadBuildRuntimeActionState = RoadBuildRuntimeActionSystem.CreateState();
        RoadBuildDependencyState = RoadBuildDependencySystem.CreateState();
        RoadBuildPlacementVisualState = RoadBuildPlacementVisualSystem.CreateState();
        RoadBuildPlacementState = RoadBuildBuildingPlacementSystem.CreateState();
        RoadBuildGridState = RoadBuildGridQuerySystem.CreateState();
    }
}
