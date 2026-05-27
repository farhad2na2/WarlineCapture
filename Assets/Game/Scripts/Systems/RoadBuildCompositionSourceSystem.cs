internal sealed class RoadBuildCompositionSourceSystem
{
    public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    public readonly RoadBuildStartupSystem RoadBuildStartupSystem = new();
    public readonly RoadBuildDependencySystem RoadBuildDependencySystem = new();
    public readonly RoadBuildReadModelSystem RoadBuildReadModelSystem = new();
    public readonly RoadBuildVisualContextSystem RoadBuildVisualContextSystem = new();
    public readonly RoadBuildInteractionContextSystem RoadBuildInteractionContextSystem = new();
    public readonly RoadGridContextSystem RoadGridContextSystem = new();
    public readonly RoadBuildConfigSystem RoadBuildConfigSystem = new();
    public readonly RoadRuntimeRootSystem RoadRuntimeRootSystem = new();
    public readonly RoadNetworkSystem RoadNetworkSystem = new();
    public readonly RoadPathPlanningSystem RoadPathPlanningSystem = new();
    public readonly RoadFootprintQuerySystem RoadFootprintQuerySystem = new();
    public readonly RoadGridProjectionSystem RoadGridProjectionSystem = new();
    public readonly RoadVisualVariantSystem RoadVisualVariantSystem = new();
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
    public readonly BuildingRoadLegacyContextSystem BuildingRoadLegacyContextSystem = new();
    public readonly BuildingRoadLegacyEcsSystem BuildingRoadLegacyEcsSystem = new();
    public readonly RoadRuntimeGenerationSystem RoadRuntimeGenerationSystem = new();
    public readonly RoadRuntimeGenerationContextSystem RoadRuntimeGenerationContextSystem = new();
}
