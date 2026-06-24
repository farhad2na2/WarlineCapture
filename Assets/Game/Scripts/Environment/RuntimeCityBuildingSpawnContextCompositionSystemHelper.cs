internal sealed class RuntimeCityBuildingSpawnContextCompositionSystemHelper
{
    public Context Create(
        RuntimeCityConfigSystem.Snapshot config,
        RuntimeCityBuildingPlotState buildingPlotSystem,
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualPresentationSystemHelper visualSystem,
        RuntimeCitySpawnBridgeState spawnBridgeSystem,
        RuntimeCityDiagnosticsSystemHelper diagnosticSystem)
    {
        return CreateFallback(
            config,
            buildingPlotSystem,
            walkabilitySystem,
            prefabSelectionSystem,
            visualSystem,
            spawnBridgeSystem,
            diagnosticSystem);
    }

    public static Context CreateFallback(
        RuntimeCityConfigSystem.Snapshot config,
        RuntimeCityBuildingPlotState buildingPlotSystem,
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualPresentationSystemHelper visualSystem,
        RuntimeCitySpawnBridgeState spawnBridgeSystem,
        RuntimeCityDiagnosticsSystemHelper diagnosticSystem)
    {
        return new Context(
            config,
            buildingPlotSystem,
            walkabilitySystem,
            prefabSelectionSystem,
            visualSystem,
            spawnBridgeSystem,
            diagnosticSystem);
    }

    public readonly struct Systems
    {
        public readonly RuntimeCityBuildingPlacementState PlacementSystem;
        public readonly RuntimeCityLandmarkOffsetState LandmarkOffsetSystem;
        public readonly RuntimeCityHallSpawnState HallSpawnSystem;
        public readonly RuntimeCityLandmarkSpawnState LandmarkSpawnSystem;
        public readonly RuntimeCityBulkPlotPlanState BulkPlotPlanSystem;
        public readonly RuntimeCityEntryBuildingSpawnState EntryBuildingSpawnSystem;
        public readonly RuntimeCityRoadsideBuildingSpawnState RoadsideBuildingSpawnSystem;
        public readonly RuntimeCityRuralBuildingSpawnState RuralBuildingSpawnSystem;
        public readonly RuntimeCityBulkBuildingSpawnRoutineState BulkBuildingSpawnRoutineSystem;
        public readonly RuntimeCityCorridorBuildingSpawnState CorridorBuildingSpawnSystem;
        public readonly RuntimeCityYardWallPlanState YardWallPlanSystem;
        public readonly RuntimeCityYardGateState YardGateSystem;
        public readonly RuntimeCityYardWallVisualState YardWallVisualSystem;
        public readonly RuntimeCityHouseYardWallState HouseYardWallSystem;
        public readonly RuntimeCityDecorationPrefabGroupState DecorationPrefabGroupSystem;
        public readonly RuntimeCityClothCoverSpawnState ClothCoverSpawnSystem;
        public readonly RuntimeCityArchwaySpawnState ArchwaySpawnSystem;
        public readonly RuntimeCityFreeScatterDecorationState FreeScatterDecorationSystem;
        public readonly RuntimeCityDecorationBuildingSpawnState DecorationBuildingSpawnSystem;

        public Systems(
            RuntimeCityBuildingPlacementState placementSystem,
            RuntimeCityLandmarkOffsetState landmarkOffsetSystem,
            RuntimeCityHallSpawnState hallSpawnSystem,
            RuntimeCityLandmarkSpawnState landmarkSpawnSystem,
            RuntimeCityBulkPlotPlanState bulkPlotPlanSystem,
            RuntimeCityEntryBuildingSpawnState entryBuildingSpawnSystem,
            RuntimeCityRoadsideBuildingSpawnState roadsideBuildingSpawnSystem,
            RuntimeCityRuralBuildingSpawnState ruralBuildingSpawnSystem,
            RuntimeCityBulkBuildingSpawnRoutineState bulkBuildingSpawnRoutineSystem,
            RuntimeCityCorridorBuildingSpawnState corridorBuildingSpawnSystem,
            RuntimeCityYardWallPlanState yardWallPlanSystem,
            RuntimeCityYardGateState yardGateSystem,
            RuntimeCityYardWallVisualState yardWallVisualSystem,
            RuntimeCityHouseYardWallState houseYardWallSystem,
            RuntimeCityDecorationPrefabGroupState decorationPrefabGroupSystem,
            RuntimeCityClothCoverSpawnState clothCoverSpawnSystem,
            RuntimeCityArchwaySpawnState archwaySpawnSystem,
            RuntimeCityFreeScatterDecorationState freeScatterDecorationSystem,
            RuntimeCityDecorationBuildingSpawnState decorationBuildingSpawnSystem)
        {
            PlacementSystem = placementSystem;
            LandmarkOffsetSystem = landmarkOffsetSystem;
            HallSpawnSystem = hallSpawnSystem;
            LandmarkSpawnSystem = landmarkSpawnSystem;
            BulkPlotPlanSystem = bulkPlotPlanSystem;
            EntryBuildingSpawnSystem = entryBuildingSpawnSystem;
            RoadsideBuildingSpawnSystem = roadsideBuildingSpawnSystem;
            RuralBuildingSpawnSystem = ruralBuildingSpawnSystem;
            BulkBuildingSpawnRoutineSystem = bulkBuildingSpawnRoutineSystem;
            CorridorBuildingSpawnSystem = corridorBuildingSpawnSystem;
            YardWallPlanSystem = yardWallPlanSystem;
            YardGateSystem = yardGateSystem;
            YardWallVisualSystem = yardWallVisualSystem;
            HouseYardWallSystem = houseYardWallSystem;
            DecorationPrefabGroupSystem = decorationPrefabGroupSystem;
            ClothCoverSpawnSystem = clothCoverSpawnSystem;
            ArchwaySpawnSystem = archwaySpawnSystem;
            FreeScatterDecorationSystem = freeScatterDecorationSystem;
            DecorationBuildingSpawnSystem = decorationBuildingSpawnSystem;
        }
    }

    public readonly struct Context
    {
        public readonly RuntimeCityConfigSystem.Snapshot Config;
        public readonly RuntimeCityBuildingPlotState BuildingPlotSystem;
        public readonly RuntimeCityWalkabilityState WalkabilitySystem;
        public readonly RuntimeCityPrefabSelectionState PrefabSelectionSystem;
        public readonly RuntimeCityVisualPresentationSystemHelper VisualSystem;
        public readonly RuntimeCitySpawnBridgeState SpawnBridgeSystem;
        public readonly RuntimeCityDiagnosticsSystemHelper DiagnosticSystem;

        public Context(
            RuntimeCityConfigSystem.Snapshot config,
            RuntimeCityBuildingPlotState buildingPlotSystem,
            RuntimeCityWalkabilityState walkabilitySystem,
            RuntimeCityPrefabSelectionState prefabSelectionSystem,
            RuntimeCityVisualPresentationSystemHelper visualSystem,
            RuntimeCitySpawnBridgeState spawnBridgeSystem,
            RuntimeCityDiagnosticsSystemHelper diagnosticSystem)
        {
            Config = config;
            BuildingPlotSystem = buildingPlotSystem;
            WalkabilitySystem = walkabilitySystem;
            PrefabSelectionSystem = prefabSelectionSystem;
            VisualSystem = visualSystem;
            SpawnBridgeSystem = spawnBridgeSystem;
            DiagnosticSystem = diagnosticSystem;
        }
    }
}
