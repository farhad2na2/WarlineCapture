internal sealed class BuildingGameplaySourceCompositionSystemHelper
{
    internal RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    internal readonly RuntimeDiagnosticsSystem RuntimeDiagnosticsSystem = new();
    internal readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem = new();
    internal readonly BuildingVisualSystem BuildingVisualSystem;
    internal readonly BuildingRuntimeVisualSystem BuildingRuntimeVisualSystem;
    internal readonly BuildingSelectionMarkerSystem BuildingSelectionMarkerSystem;
    internal readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
    internal readonly BuildingDestroyedVisualPresentationSystemHelper BuildingDestroyedVisualPresentationSystemHelper;
    internal readonly BuildingCombatSystem BuildingCombatSystem = new();
    internal readonly FactionResourceSystem FactionResourceSystem = new();
    internal readonly ResourceHaulerSystem ResourceHaulerSystem = new();
    internal readonly BuildingProductionSystem BuildingProductionSystem = new();
    internal readonly BuildingProductionUpdateSystem BuildingProductionUpdateSystem = new();
    internal readonly BuildingProductionTransportSystem BuildingProductionTransportSystem = new();
    internal readonly BuildingProductionTransportBridgeSystem BuildingProductionTransportBridgeSystem = new();
    internal readonly BuildingProductionContextCompositionSystemHelper BuildingProductionContextCompositionSystemHelper = new();
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
    internal readonly BuildingPlacementInteractionContextCompositionSystemHelper BuildingPlacementInteractionContextCompositionSystemHelper = new();
    internal readonly BuildingRunwaySystem BuildingRunwaySystem = new();
    internal readonly BuildingPlacementValidationSystem BuildingPlacementValidationSystem = new();
    internal readonly BuildingPlacementPreviewSystem BuildingPlacementPreviewSystem = new();
    internal readonly BuildingPlacementVisualUpdateCompositionSystemHelper BuildingPlacementVisualUpdateCompositionSystemHelper;
    internal readonly BuildingPlacementVisualCompositionPresentationSystemHelper BuildingPlacementVisualCompositionPresentationSystemHelper;
    internal readonly BuildingPlacementAdapterCompositionSystemHelper BuildingPlacementAdapterCompositionSystemHelper = new();
    internal readonly BuildingPlacementCommitSystem BuildingPlacementCommitSystem = new();
    internal readonly BuildingPlacementInputSystem BuildingPlacementInputSystem = new();
    internal readonly BuildingPlacementContextCompositionSystemHelper BuildingPlacementContextCompositionSystemHelper = new();
    internal readonly BuildingPlacementCommandSystem BuildingPlacementCommandSystem = new();
    internal readonly BuildingPlacementCommandCompositionSystemHelper BuildingPlacementCommandCompositionSystemHelper = new();
    internal readonly BuildingPlacementSessionSystem BuildingPlacementSessionSystem = new();
    internal readonly BuildingProductionRequestBoundary BuildingProductionRequestBoundary = new();
    internal readonly BuildingProductionCompositionSystemHelper BuildingProductionCompositionSystemHelper = new();
    internal readonly BuildingRuntimeCreationSystem BuildingRuntimeCreationSystem = new();
    internal readonly BuildingSelectionSystem BuildingSelectionSystem = new();
    internal readonly BuildingSelectionCompositionSystemHelper BuildingSelectionCompositionHelper = new();
    internal readonly BuildingSelectionClickSystem BuildingSelectionClickSystem = new();
    internal readonly BuildingSelectionClickCompositionSystemHelper BuildingSelectionClickCompositionHelper = new();
    internal readonly BuildingBarrierSystem BuildingBarrierSystem = new();
    internal readonly BuildingRuntimeQuerySystem BuildingRuntimeQuerySystem = new();
    internal readonly BuildingDefinitionSystem BuildingDefinitionSystem = new();
    internal readonly BuildingPlacementLifecycleSystem BuildingPlacementLifecycleSystem = new();
    internal readonly BuildingPlacementGridSystem BuildingPlacementGridSystem = new();
    internal readonly BuildingPlacementVisualPresentationSystemHelper BuildingPlacementVisualPresentationSystemHelper;
    internal readonly BuildingRuntimeSpawnSystem BuildingRuntimeSpawnSystem = new();
    internal readonly BuildingRuntimeSpawnCommandBoundary BuildingRuntimeSpawnCommandBoundary = new();
    internal readonly BuildingRuntimeContextSystem BuildingRuntimeContextSystem = new();
    internal readonly BuildingRuntimeContextCompositionSystemHelper BuildingRuntimeContextCompositionSystemHelper = new();
    internal readonly BuildingRuntimeQueryCompositionSystemHelper BuildingRuntimeQueryCompositionSystemHelper = new();
    internal readonly BuildingRuntimeSideEffectCompositionSystemHelper BuildingRuntimeSideEffectCompositionSystemHelper = new();
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
    internal readonly BuildingRuntimeResourcePrefabContextCompositionSystemHelper BuildingRuntimeResourcePrefabContextCompositionSystemHelper;
    internal readonly BuildingRuntimeResourcePrefabCompositionSystemHelper BuildingRuntimeResourcePrefabCompositionHelper;
    internal readonly BuildingPlacementStartupSystem BuildingPlacementStartupSystem = new();
    internal readonly BuildingGameplayDependencyCompositionSystemHelper BuildingGameplayDependencyCompositionSystemHelper = new();
    internal readonly BuildingRuntimeObjectPresentationSystemHelper RuntimeObjectPresentationHelper = new();
    internal readonly BuildingGameplayDisposalExecutionCompositionSystemHelper BuildingGameplayDisposalExecutionCompositionSystemHelper = new();
    internal readonly BuildingGameplayEcsQueryCompositionSystemHelper BuildingGameplayEcsQueryCompositionSystemHelper = new();
    internal readonly BuildingGameplayGridDataCompositionSystemHelper BuildingGameplayGridDataCompositionSystemHelper = new();
    internal readonly BuildingGridCompositionSystem BuildingGridCompositionSystem = new();
    internal readonly BuildingEntityManagerAccessSystem BuildingEntityManagerAccessSystem = new();
    internal readonly BuildingPlacementInvalidCellSystem BuildingPlacementInvalidCellSystem = new();
    internal readonly UnitPathfindingPendingStateReader UnitPathfindingPendingStateReader = new();
    internal BuildingProductionTransportSystem.PrepareTransportDropVisualDelegate PrepareTransportDropVisual;
    internal uint BuildingSpawnRandomState = 0x12345678u;

    public BuildingGameplaySourceCompositionSystemHelper()
    {
        BuildingVisualSystem = ResolveBuildingVisualSystem();
        BuildingRuntimeVisualSystem = ResolveBuildingRuntimeVisualSystem();
        BuildingSelectionMarkerSystem = ResolveBuildingSelectionMarkerSystem();
        BuildingFactionVisualSystem = ResolveBuildingFactionVisualSystem();
        BuildingDestroyedVisualPresentationSystemHelper = ResolveBuildingDestroyedVisualPresentationSystemHelper();
        BuildingPlacementVisualUpdateCompositionSystemHelper = ResolveBuildingPlacementVisualUpdateCompositionSystemHelper();
        BuildingPlacementVisualCompositionPresentationSystemHelper = ResolveBuildingPlacementVisualCompositionPresentationSystemHelper();
        BuildingPlacementVisualPresentationSystemHelper = ResolveBuildingPlacementVisualPresentationSystemHelper();
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper = ResolveBuildingRuntimeResourcePrefabContextCompositionSystemHelper();
        BuildingRuntimeResourcePrefabCompositionHelper = ResolveBuildingRuntimeResourcePrefabCompositionHelper();
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

    private static BuildingDestroyedVisualPresentationSystemHelper ResolveBuildingDestroyedVisualPresentationSystemHelper()
    {
        return new BuildingDestroyedVisualPresentationSystemHelper();
    }

    private static BuildingPlacementVisualPresentationSystemHelper ResolveBuildingPlacementVisualPresentationSystemHelper()
    {
        return new BuildingPlacementVisualPresentationSystemHelper();
    }

    private static BuildingPlacementVisualUpdateCompositionSystemHelper ResolveBuildingPlacementVisualUpdateCompositionSystemHelper()
    {
        return new BuildingPlacementVisualUpdateCompositionSystemHelper();
    }

    private static BuildingPlacementVisualCompositionPresentationSystemHelper ResolveBuildingPlacementVisualCompositionPresentationSystemHelper()
    {
        return new BuildingPlacementVisualCompositionPresentationSystemHelper();
    }

    private static BuildingRuntimeResourcePrefabCompositionSystemHelper ResolveBuildingRuntimeResourcePrefabCompositionHelper()
    {
        return new BuildingRuntimeResourcePrefabCompositionSystemHelper();
    }

    private static BuildingRuntimeResourcePrefabContextCompositionSystemHelper ResolveBuildingRuntimeResourcePrefabContextCompositionSystemHelper()
    {
        return new BuildingRuntimeResourcePrefabContextCompositionSystemHelper();
    }
}
