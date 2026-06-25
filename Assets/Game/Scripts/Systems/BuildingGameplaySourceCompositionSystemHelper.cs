internal sealed class BuildingGameplaySourceCompositionSystemHelper
{
    internal RuntimeGameplayStateSystem RuntimeGameplayStateSystem = new();
    internal readonly RuntimeDiagnosticsSystem RuntimeDiagnosticsSystem = new();
    internal readonly RuntimeBuildingCollection<RuntimeBuildingEntity> RuntimeBuildingSystem = new();
    internal readonly BuildingVisualSystem BuildingVisualSystem;
    internal readonly BuildingRuntimeVisualPresentationSystemHelper BuildingRuntimeVisualPresentationSystemHelper;
    internal readonly BuildingSelectionMarkerSystem BuildingSelectionMarkerSystem;
    internal readonly BuildingFactionVisualSystem BuildingFactionVisualSystem;
    internal readonly BuildingDestroyedVisualPresentationSystemHelper BuildingDestroyedVisualPresentationSystemHelper;
    internal readonly BuildingCombatUtilitySystemHelper BuildingCombatUtilitySystemHelper = new();
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
    internal readonly BuildingPlacementQueryUiSystemHelper BuildingPlacementQueryUiSystemHelper = new();
    internal readonly BuildingPlacementQueryCompositionSystem BuildingPlacementQueryCompositionSystem = new();
    internal readonly BuildingUiQuerySystem BuildingUiQuerySystem = new();
    internal readonly BuildingUiCommandBoundary BuildingUiCommandBoundary = new();
    internal readonly BuildingUiContextSystem BuildingUiContextSystem = new();
    internal readonly BuildingUiCompositionSystem BuildingUiCompositionSystem = new();
    internal readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper BuildingPlacementInteractionBoundaryCompositionSystemHelper = new();
    internal readonly BuildingPlacementInteractionContextCompositionSystemHelper BuildingPlacementInteractionContextCompositionSystemHelper = new();
    internal readonly BuildingRunwaySystem BuildingRunwaySystem = new();
    internal readonly BuildingPlacementValidationSystem BuildingPlacementValidationSystem = new();
    internal readonly BuildingPlacementPreviewPresentationSystemHelper BuildingPlacementPreviewPresentationSystemHelper = new();
    internal readonly BuildingPlacementVisualUpdateCompositionSystemHelper BuildingPlacementVisualUpdateCompositionSystemHelper;
    internal readonly BuildingPlacementVisualCompositionPresentationSystemHelper BuildingPlacementVisualCompositionPresentationSystemHelper;
    internal readonly BuildingPlacementAdapterCompositionSystemHelper BuildingPlacementAdapterCompositionSystemHelper = new();
    internal readonly BuildingPlacementCommitCompositionSystemHelper BuildingPlacementCommitCompositionSystemHelper = new();
    internal readonly BuildingPlacementInputUiSystemHelper BuildingPlacementInputUiSystemHelper = new();
    internal readonly BuildingPlacementContextCompositionSystemHelper BuildingPlacementContextCompositionSystemHelper = new();
    internal readonly BuildingPlacementCommandRequestCompositionSystemHelper BuildingPlacementCommandRequestCompositionSystemHelper = new();
    internal readonly BuildingPlacementCommandCompositionSystemHelper BuildingPlacementCommandCompositionSystemHelper = new();
    internal readonly BuildingPlacementSessionCompositionSystemHelper BuildingPlacementSessionCompositionSystemHelper = new();
    internal readonly BuildingProductionRequestBoundary BuildingProductionRequestBoundary = new();
    internal readonly BuildingProductionCompositionSystemHelper BuildingProductionCompositionSystemHelper = new();
    internal readonly BuildingRuntimeCreationSystem BuildingRuntimeCreationSystem = new();
    internal readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinkRegistry = new();
    internal readonly BuildingSelectionSystem BuildingSelectionSystem = new();
    internal readonly BuildingSelectionCompositionSystemHelper BuildingSelectionCompositionHelper = new();
    internal readonly BuildingSelectionClickSystem BuildingSelectionClickSystem = new();
    internal readonly BuildingSelectionClickCompositionSystemHelper BuildingSelectionClickCompositionHelper = new();
    internal readonly BuildingBarrierUtilitySystemHelper BuildingBarrierUtilitySystemHelper = new();
    internal readonly BuildingRuntimeQuerySystem BuildingRuntimeQuerySystem = new();
    internal readonly BuildingDefinitionPrefabSystemHelper BuildingDefinitionPrefabSystemHelper = new();
    internal readonly BuildingPlacementLifecycleCompositionSystemHelper BuildingPlacementLifecycleCompositionSystemHelper = new();
    internal readonly BuildingPlacementGridCameraSystemHelper BuildingPlacementGridCameraSystemHelper = new();
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
    internal readonly BuildingPlacementRedirectCompositionSystemHelper BuildingPlacementRedirectCompositionSystemHelper = new();
    internal readonly BuildingResourceHaulerBridgeSystem BuildingResourceHaulerBridgeSystem = new();
    internal readonly BuildingRuntimeBoundarySystem BuildingRuntimeBoundarySystem = new();
    internal readonly MapBuildingPlacementSpawnSystem MapBuildingPlacementSpawnSystem = new();
    internal readonly MapVehiclePlacementSpawnSystem MapVehiclePlacementSpawnSystem = new();
    internal readonly BuildingPlacementRuntimeTickSystem BuildingPlacementRuntimeTickSystem = new();
    internal readonly BuildingPlacementInputRuntimeTickUiSystemHelper BuildingPlacementInputRuntimeTickUiSystemHelper = new();
    internal readonly RuntimeResourceSystem RuntimeResourceSystem = new();
    internal readonly RuntimeUnitPrefabSystem RuntimeUnitPrefabSystem = new();
    internal readonly BuildingRuntimeResourcePrefabContextCompositionSystemHelper BuildingRuntimeResourcePrefabContextCompositionSystemHelper;
    internal readonly BuildingRuntimeResourcePrefabCompositionSystemHelper BuildingRuntimeResourcePrefabCompositionHelper;
    internal readonly BuildingPlacementStartupSystemHelper BuildingPlacementStartupSystemHelper = new();
    internal readonly BuildingGameplayDependencyCompositionSystemHelper BuildingGameplayDependencyCompositionSystemHelper = new();
    internal readonly BuildingRuntimeObjectPresentationSystemHelper RuntimeObjectPresentationHelper = new();
    internal readonly BuildingGameplayDisposalExecutionCompositionSystemHelper BuildingGameplayDisposalExecutionCompositionSystemHelper = new();
    internal readonly BuildingGameplayEcsQueryCompositionSystemHelper BuildingGameplayEcsQueryCompositionSystemHelper = new();
    internal readonly BuildingGameplayGridDataCompositionSystemHelper BuildingGameplayGridDataCompositionSystemHelper = new();
    internal readonly BuildingGridCompositionSystem BuildingGridCompositionSystem = new();
    internal readonly BuildingEntityManagerAccessSystem BuildingEntityManagerAccessSystem = new();
    internal readonly BuildingPlacementInvalidCellCacheCompositionSystemHelper BuildingPlacementInvalidCellCacheCompositionSystemHelper = new();
    internal readonly UnitPathfindingPendingStateReader UnitPathfindingPendingStateReader = new();
    internal BuildingProductionTransportSystem.PrepareTransportDropVisualDelegate PrepareTransportDropVisual;
    internal uint BuildingSpawnRandomState = 0x12345678u;

    public BuildingGameplaySourceCompositionSystemHelper()
    {
        BuildingVisualSystem = ResolveBuildingVisualSystem();
        BuildingRuntimeVisualPresentationSystemHelper = ResolveBuildingRuntimeVisualPresentationSystemHelper();
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

    private static BuildingRuntimeVisualPresentationSystemHelper ResolveBuildingRuntimeVisualPresentationSystemHelper()
    {
        return new BuildingRuntimeVisualPresentationSystemHelper();
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
