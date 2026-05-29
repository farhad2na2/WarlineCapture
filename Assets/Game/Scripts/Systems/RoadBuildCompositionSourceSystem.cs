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
    public readonly BuildingRoadLegacyStorageSystem BuildingRoadLegacyStorageSystem = new();
    public readonly BuildingRoadLegacyDefinitionSystem BuildingRoadLegacyDefinitionSystem = new();
    public readonly BuildingRoadLegacyPlacementVisualSystem BuildingRoadLegacyPlacementVisualSystem = new();
    public readonly BuildingRoadLegacyPlacementSystem BuildingRoadLegacyPlacementSystem = new();
    public readonly BuildingRoadLegacyInteractionSystem BuildingRoadLegacyInteractionSystem = new();
    public readonly BuildingRoadLegacyGridSystem BuildingRoadLegacyGridSystem = new();
    public readonly BuildingRoadLegacyContextSystem BuildingRoadLegacyContextSystem = new();
    public readonly BuildingRoadLegacyEcsSystem BuildingRoadLegacyEcsSystem = new();
    public readonly RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem = new();
    public readonly RoadRuntimeGenerationContextSystem RoadRuntimeGenerationContextSystem = new();
    public readonly RoadBuildMutationSystem RoadBuildMutationSystem = new();
    public readonly RoadBuildCompositionContextSystem RoadBuildCompositionContextSystem = new();
    public readonly RoadBuildCompositionLifecycleSystem RoadBuildCompositionLifecycleSystem = new();

    public RoadBuildStartupSystem.State RoadBuildStartupState = new();
    public readonly RoadBuildDependencySystem.State RoadBuildDependencyState;
    public readonly BuildingRoadLegacyPlacementVisualSystem.State BuildingRoadLegacyPlacementVisualState;
    public readonly BuildingRoadLegacyPlacementSystem.State BuildingRoadLegacyPlacementState;
    public readonly BuildingRoadLegacyGridSystem.State BuildingRoadLegacyGridState;
    public uint BuildingSpawnRandomState = 0x12345678u;

    public RoadBuildCompositionSourceSystem()
    {
        RoadBuildRuntimeActionState = RoadBuildRuntimeActionSystem.CreateState();
        RoadBuildDependencyState = RoadBuildDependencySystem.CreateState();
        BuildingRoadLegacyPlacementVisualState = BuildingRoadLegacyPlacementVisualSystem.CreateState();
        BuildingRoadLegacyPlacementState = BuildingRoadLegacyPlacementSystem.CreateState();
        BuildingRoadLegacyGridState = BuildingRoadLegacyGridSystem.CreateState();
    }
}
