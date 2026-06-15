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
        RuntimeCityBuildingPlotSystem buildingPlotSystem,
        RuntimeCityWalkabilitySystem walkabilitySystem,
        RuntimeCityPrefabSelectionSystem prefabSelectionSystem,
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
        RuntimeCityBuildingPlotSystem buildingPlotSystem,
        RuntimeCityWalkabilitySystem walkabilitySystem,
        RuntimeCityPrefabSelectionSystem prefabSelectionSystem,
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
        public readonly RuntimeCityLandmarkOffsetSystem LandmarkOffsetSystem;
        public readonly RuntimeCityHallSpawnSystem HallSpawnSystem;
        public readonly RuntimeCityLandmarkSpawnSystem LandmarkSpawnSystem;
        public readonly RuntimeCityBulkPlotPlanSystem BulkPlotPlanSystem;
        public readonly RuntimeCityEntryBuildingSpawnSystem EntryBuildingSpawnSystem;
        public readonly RuntimeCityRoadsideBuildingSpawnSystem RoadsideBuildingSpawnSystem;
        public readonly RuntimeCityRuralBuildingSpawnSystem RuralBuildingSpawnSystem;
        public readonly RuntimeCityBulkBuildingSpawnRoutineSystem BulkBuildingSpawnRoutineSystem;
        public readonly RuntimeCityCorridorBuildingSpawnSystem CorridorBuildingSpawnSystem;
        public readonly RuntimeCityYardWallPlanSystem YardWallPlanSystem;
        public readonly RuntimeCityYardGateSystem YardGateSystem;
        public readonly RuntimeCityYardWallVisualSystem YardWallVisualSystem;
        public readonly RuntimeCityHouseYardWallSystem HouseYardWallSystem;
        public readonly RuntimeCityDecorationPrefabGroupSystem DecorationPrefabGroupSystem;
        public readonly RuntimeCityClothCoverSpawnSystem ClothCoverSpawnSystem;
        public readonly RuntimeCityArchwaySpawnSystem ArchwaySpawnSystem;
        public readonly RuntimeCityFreeScatterDecorationSystem FreeScatterDecorationSystem;
        public readonly RuntimeCityDecorationBuildingSpawnSystem DecorationBuildingSpawnSystem;

        public Systems(
            RuntimeCityBuildingPlacementSystem placementSystem,
            RuntimeCityLandmarkOffsetSystem landmarkOffsetSystem,
            RuntimeCityHallSpawnSystem hallSpawnSystem,
            RuntimeCityLandmarkSpawnSystem landmarkSpawnSystem,
            RuntimeCityBulkPlotPlanSystem bulkPlotPlanSystem,
            RuntimeCityEntryBuildingSpawnSystem entryBuildingSpawnSystem,
            RuntimeCityRoadsideBuildingSpawnSystem roadsideBuildingSpawnSystem,
            RuntimeCityRuralBuildingSpawnSystem ruralBuildingSpawnSystem,
            RuntimeCityBulkBuildingSpawnRoutineSystem bulkBuildingSpawnRoutineSystem,
            RuntimeCityCorridorBuildingSpawnSystem corridorBuildingSpawnSystem,
            RuntimeCityYardWallPlanSystem yardWallPlanSystem,
            RuntimeCityYardGateSystem yardGateSystem,
            RuntimeCityYardWallVisualSystem yardWallVisualSystem,
            RuntimeCityHouseYardWallSystem houseYardWallSystem,
            RuntimeCityDecorationPrefabGroupSystem decorationPrefabGroupSystem,
            RuntimeCityClothCoverSpawnSystem clothCoverSpawnSystem,
            RuntimeCityArchwaySpawnSystem archwaySpawnSystem,
            RuntimeCityFreeScatterDecorationSystem freeScatterDecorationSystem,
            RuntimeCityDecorationBuildingSpawnSystem decorationBuildingSpawnSystem)
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
        public readonly RuntimeCityBuildingPlotSystem BuildingPlotSystem;
        public readonly RuntimeCityWalkabilitySystem WalkabilitySystem;
        public readonly RuntimeCityPrefabSelectionSystem PrefabSelectionSystem;
        public readonly RuntimeCityVisualSystem VisualSystem;
        public readonly RuntimeCitySpawnBridgeSystem SpawnBridgeSystem;
        public readonly RuntimeCityDiagnosticSystem DiagnosticSystem;

        public Context(
            RuntimeCityConfigSystem.Snapshot config,
            RuntimeCityBuildingPlotSystem buildingPlotSystem,
            RuntimeCityWalkabilitySystem walkabilitySystem,
            RuntimeCityPrefabSelectionSystem prefabSelectionSystem,
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
