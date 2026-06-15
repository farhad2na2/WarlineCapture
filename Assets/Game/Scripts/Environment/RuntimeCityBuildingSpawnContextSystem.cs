using Unity.Entities;

internal sealed partial class RuntimeCityBuildingSpawnContextSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public Context Create(
        RuntimeCityConfigSystem.Snapshot config,
        RuntimeCityBuildingPlotState buildingPlotSystem,
        RuntimeCityWalkabilityState walkabilitySystem,
        RuntimeCityPrefabSelectionState prefabSelectionSystem,
        RuntimeCityVisualSystem visualSystem,
        RuntimeCitySpawnBridgeSystem spawnBridgeSystem,
        RuntimeCityDiagnosticSystem diagnosticSystem)
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
        RuntimeCityVisualSystem visualSystem,
        RuntimeCitySpawnBridgeSystem spawnBridgeSystem,
        RuntimeCityDiagnosticSystem diagnosticSystem)
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
        public readonly RuntimeCityBuildingPlacementSystem PlacementSystem;
        public readonly RuntimeCityLandmarkOffsetState LandmarkOffsetSystem;
        public readonly RuntimeCityHallSpawnState HallSpawnSystem;
        public readonly RuntimeCityLandmarkSpawnState LandmarkSpawnSystem;
        public readonly RuntimeCityBulkPlotPlanState BulkPlotPlanSystem;
        public readonly RuntimeCityEntryBuildingSpawnState EntryBuildingSpawnSystem;
        public readonly RuntimeCityRoadsideBuildingSpawnState RoadsideBuildingSpawnSystem;
        public readonly RuntimeCityRuralBuildingSpawnState RuralBuildingSpawnSystem;
        public readonly RuntimeCityBulkBuildingSpawnRoutineSystem BulkBuildingSpawnRoutineSystem;
        public readonly RuntimeCityCorridorBuildingSpawnState CorridorBuildingSpawnSystem;
        public readonly RuntimeCityYardWallPlanSystem YardWallPlanSystem;
        public readonly RuntimeCityYardGateSystem YardGateSystem;
        public readonly RuntimeCityYardWallVisualSystem YardWallVisualSystem;
        public readonly RuntimeCityHouseYardWallSystem HouseYardWallSystem;
        public readonly RuntimeCityDecorationPrefabGroupState DecorationPrefabGroupSystem;
        public readonly RuntimeCityClothCoverSpawnState ClothCoverSpawnSystem;
        public readonly RuntimeCityArchwaySpawnState ArchwaySpawnSystem;
        public readonly RuntimeCityFreeScatterDecorationState FreeScatterDecorationSystem;
        public readonly RuntimeCityDecorationBuildingSpawnState DecorationBuildingSpawnSystem;

        public Systems(
            RuntimeCityBuildingPlacementSystem placementSystem,
            RuntimeCityLandmarkOffsetState landmarkOffsetSystem,
            RuntimeCityHallSpawnState hallSpawnSystem,
            RuntimeCityLandmarkSpawnState landmarkSpawnSystem,
            RuntimeCityBulkPlotPlanState bulkPlotPlanSystem,
            RuntimeCityEntryBuildingSpawnState entryBuildingSpawnSystem,
            RuntimeCityRoadsideBuildingSpawnState roadsideBuildingSpawnSystem,
            RuntimeCityRuralBuildingSpawnState ruralBuildingSpawnSystem,
            RuntimeCityBulkBuildingSpawnRoutineSystem bulkBuildingSpawnRoutineSystem,
            RuntimeCityCorridorBuildingSpawnState corridorBuildingSpawnSystem,
            RuntimeCityYardWallPlanSystem yardWallPlanSystem,
            RuntimeCityYardGateSystem yardGateSystem,
            RuntimeCityYardWallVisualSystem yardWallVisualSystem,
            RuntimeCityHouseYardWallSystem houseYardWallSystem,
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
        public readonly RuntimeCityVisualSystem VisualSystem;
        public readonly RuntimeCitySpawnBridgeSystem SpawnBridgeSystem;
        public readonly RuntimeCityDiagnosticSystem DiagnosticSystem;

        public Context(
            RuntimeCityConfigSystem.Snapshot config,
            RuntimeCityBuildingPlotState buildingPlotSystem,
            RuntimeCityWalkabilityState walkabilitySystem,
            RuntimeCityPrefabSelectionState prefabSelectionSystem,
            RuntimeCityVisualSystem visualSystem,
            RuntimeCitySpawnBridgeSystem spawnBridgeSystem,
            RuntimeCityDiagnosticSystem diagnosticSystem)
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
