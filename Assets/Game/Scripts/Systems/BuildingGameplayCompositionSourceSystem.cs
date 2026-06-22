internal sealed class BuildingGameplayCompositionSourceSystem
{
    internal RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    internal readonly RuntimeDiagnosticsSystem RuntimeDiagnosticsSystem = new();
    internal readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem = new();
    internal readonly BuildingVisualSystem BuildingVisualSystem;
    internal readonly BuildingRuntimeVisualSystem BuildingRuntimeVisualSystem;
    internal readonly BuildingSelectionMarkerSystem BuildingSelectionMarkerSystem;
    internal readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
    internal readonly BuildingDestroyedVisualSystem BuildingDestroyedVisualSystem;
    internal readonly BuildingCombatSystem BuildingCombatSystem = new();
    internal readonly FactionResourceSystem FactionResourceSystem = new();
    internal readonly ResourceHaulerSystem ResourceHaulerSystem = new();
    internal readonly BuildingProductionSystem BuildingProductionSystem = new();
    internal readonly BuildingProductionUpdateSystem BuildingProductionUpdateSystem = new();
    internal readonly BuildingProductionTransportSystem BuildingProductionTransportSystem = new();
    internal readonly BuildingProductionTransportBridgeSystem BuildingProductionTransportBridgeSystem = new();
    internal readonly BuildingProductionContextSystem BuildingProductionContextSystem = new();
    internal readonly BuildingSpawnSystem BuildingSpawnSystem = new();
    internal readonly BuildingSpawnPrefabSystem BuildingSpawnPrefabSystem = new();
    internal readonly BuildingProductionSlotSystem BuildingProductionSlotSystem = new();
    internal readonly BuildingPlacementQuerySystem BuildingPlacementQuerySystem = new();
    internal readonly BuildingPlacementQueryCompositionSystem BuildingPlacementQueryCompositionSystem = new();
    internal readonly BuildingUiQuerySystem BuildingUiQuerySystem = new();
    internal readonly BuildingUiCommandBoundary BuildingUiCommandBoundary = new();
    internal readonly BuildingUiContextSystem BuildingUiContextSystem = new();
    internal readonly BuildingUiCompositionSystem BuildingUiCompositionSystem = new();
    internal readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem = new();
    internal readonly BuildingPlacementInteractionContextSystem BuildingPlacementInteractionContextSystem = new();
    internal readonly BuildingRunwaySystem BuildingRunwaySystem = new();
    internal readonly BuildingPlacementValidationSystem BuildingPlacementValidationSystem = new();
    internal readonly BuildingPlacementPreviewSystem BuildingPlacementPreviewSystem = new();
    internal readonly BuildingPlacementVisualUpdateSystem BuildingPlacementVisualUpdateSystem;
    internal readonly BuildingPlacementVisualCompositionSystem BuildingPlacementVisualCompositionSystem;
    internal readonly BuildingPlacementAdapterSystem BuildingPlacementAdapterSystem = new();
    internal readonly BuildingPlacementCommitSystem BuildingPlacementCommitSystem = new();
    internal readonly BuildingPlacementInputSystem BuildingPlacementInputSystem = new();
    internal readonly BuildingPlacementContextSystem BuildingPlacementContextSystem = new();
    internal readonly BuildingPlacementCommandSystem BuildingPlacementCommandSystem = new();
    internal readonly BuildingPlacementCommandCompositionSystem BuildingPlacementCommandCompositionSystem = new();
    internal readonly BuildingPlacementSessionSystem BuildingPlacementSessionSystem = new();
    internal readonly BuildingProductionRequestBoundary BuildingProductionRequestBoundary = new();
    internal readonly BuildingProductionCompositionSystem BuildingProductionCompositionSystem = new();
    internal readonly BuildingRuntimeCreationSystem BuildingRuntimeCreationSystem = new();
    internal readonly BuildingSelectionSystem BuildingSelectionSystem = new();
    internal readonly BuildingSelectionCompositionSystem BuildingSelectionCompositionSystem = new();
    internal readonly BuildingSelectionClickSystem BuildingSelectionClickSystem = new();
    internal readonly BuildingSelectionClickCompositionSystem BuildingSelectionClickCompositionSystem = new();
    internal readonly BuildingBarrierSystem BuildingBarrierSystem = new();
    internal readonly BuildingRuntimeQuerySystem BuildingRuntimeQuerySystem = new();
    internal readonly BuildingDefinitionSystem BuildingDefinitionSystem = new();
    internal readonly BuildingPlacementLifecycleSystem BuildingPlacementLifecycleSystem = new();
    internal readonly BuildingPlacementGridSystem BuildingPlacementGridSystem = new();
    internal readonly BuildingPlacementVisualSystem BuildingPlacementVisualSystem;
    internal readonly BuildingRuntimeSpawnSystem BuildingRuntimeSpawnSystem = new();
    internal readonly BuildingRuntimeSpawnCommandBoundary BuildingRuntimeSpawnCommandBoundary = new();
    internal readonly BuildingRuntimeContextSystem BuildingRuntimeContextSystem = new();
    internal readonly BuildingRuntimeCompositionSystem BuildingRuntimeCompositionSystem = new();
    internal readonly BuildingRuntimeCompositionQuerySystem BuildingRuntimeCompositionQuerySystem = new();
    internal readonly BuildingRuntimeSideEffectCompositionSystem BuildingRuntimeSideEffectCompositionSystem = new();
    internal readonly BuildingRuntimeCitySpawnSystem BuildingRuntimeCitySpawnSystem = new();
    internal readonly BuildingRuntimeOwnershipSystem BuildingRuntimeOwnershipSystem = new();
    internal readonly BuildingRuntimeEntitySystem BuildingRuntimeEntitySystem = new();
    internal readonly BuildingPlacementRedirectSystem BuildingPlacementRedirectSystem = new();
    internal readonly BuildingResourceHaulerBridgeSystem BuildingResourceHaulerBridgeSystem = new();
    internal readonly BuildingRuntimeBoundarySystem BuildingRuntimeBoundarySystem = new();
    internal readonly MapBuildingPlacementSpawnSystem MapBuildingPlacementSpawnSystem = new();
    internal readonly MapVehiclePlacementSpawnSystem MapVehiclePlacementSpawnSystem = new();
    internal readonly BuildingPlacementRuntimeTickSystem BuildingPlacementRuntimeTickSystem = new();
    internal readonly BuildingPlacementInputRuntimeTickSystem BuildingPlacementInputRuntimeTickSystem = new();
    internal readonly RuntimeResourceSystem RuntimeResourceSystem = new();
    internal readonly RuntimeUnitPrefabSystem RuntimeUnitPrefabSystem = new();
    internal readonly BuildingRuntimeResourcePrefabContextSystem BuildingRuntimeResourcePrefabContextSystem;
    internal readonly BuildingRuntimeResourcePrefabCompositionSystem BuildingRuntimeResourcePrefabCompositionSystem;
    internal readonly BuildingPlacementStartupSystem BuildingPlacementStartupSystem = new();
    internal readonly BuildingGameplayDependencySystem BuildingGameplayDependencySystem = new();
    internal readonly BuildingRuntimeObjectSystem BuildingRuntimeObjectSystem = new();
    internal readonly BuildingGameplayDisposalSystem BuildingGameplayDisposalSystem = new();
    internal readonly BuildingGameplayEcsQuerySystem BuildingGameplayEcsQuerySystem = new();
    internal readonly BuildingGameplayGridDataSystem BuildingGameplayGridDataSystem = new();
    internal readonly BuildingGridCompositionSystem BuildingGridCompositionSystem = new();
    internal readonly BuildingEntityManagerAccessSystem BuildingEntityManagerAccessSystem = new();
    internal readonly BuildingPlacementInvalidCellSystem BuildingPlacementInvalidCellSystem = new();
    internal readonly UnitPathfindingPendingStateReader UnitPathfindingPendingStateReader = new();
    internal BuildingProductionTransportSystem.PrepareTransportDropVisualDelegate PrepareTransportDropVisual;
    internal uint BuildingSpawnRandomState = 0x12345678u;

    public BuildingGameplayCompositionSourceSystem()
    {
        BuildingVisualSystem = ResolveBuildingVisualSystem();
        BuildingRuntimeVisualSystem = ResolveBuildingRuntimeVisualSystem();
        BuildingSelectionMarkerSystem = ResolveBuildingSelectionMarkerSystem();
        BuildingFactionVisualSystem = ResolveBuildingFactionVisualSystem();
        BuildingDestroyedVisualSystem = ResolveBuildingDestroyedVisualSystem();
        BuildingPlacementVisualUpdateSystem = ResolveBuildingPlacementVisualUpdateSystem();
        BuildingPlacementVisualCompositionSystem = ResolveBuildingPlacementVisualCompositionSystem();
        BuildingPlacementVisualSystem = ResolveBuildingPlacementVisualSystem();
        BuildingRuntimeResourcePrefabContextSystem = ResolveBuildingRuntimeResourcePrefabContextSystem();
        BuildingRuntimeResourcePrefabCompositionSystem = ResolveBuildingRuntimeResourcePrefabCompositionSystem();
    }

    private static BuildingVisualSystem ResolveBuildingVisualSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<BuildingVisualSystem>()
            : null;
    }

    private static BuildingFactionVisualSystem ResolveBuildingFactionVisualSystem()
    {
        Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        return world != null && world.IsCreated
            ? world.GetOrCreateSystemManaged<BuildingFactionVisualSystem>()
            : null;
    }

    private static BuildingRuntimeVisualSystem ResolveBuildingRuntimeVisualSystem()
    {
        return new BuildingRuntimeVisualSystem();
    }

    private static BuildingSelectionMarkerSystem ResolveBuildingSelectionMarkerSystem()
    {
        return new BuildingSelectionMarkerSystem();
    }

    private static BuildingDestroyedVisualSystem ResolveBuildingDestroyedVisualSystem()
    {
        return new BuildingDestroyedVisualSystem();
    }

    private static BuildingPlacementVisualSystem ResolveBuildingPlacementVisualSystem()
    {
        return new BuildingPlacementVisualSystem();
    }

    private static BuildingPlacementVisualUpdateSystem ResolveBuildingPlacementVisualUpdateSystem()
    {
        return new BuildingPlacementVisualUpdateSystem();
    }

    private static BuildingPlacementVisualCompositionSystem ResolveBuildingPlacementVisualCompositionSystem()
    {
        return new BuildingPlacementVisualCompositionSystem();
    }

    private static BuildingRuntimeResourcePrefabCompositionSystem ResolveBuildingRuntimeResourcePrefabCompositionSystem()
    {
        return new BuildingRuntimeResourcePrefabCompositionSystem();
    }

    private static BuildingRuntimeResourcePrefabContextSystem ResolveBuildingRuntimeResourcePrefabContextSystem()
    {
        return new BuildingRuntimeResourcePrefabContextSystem();
    }
}
