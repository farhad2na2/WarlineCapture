using UnityEngine;

public sealed class GameplayFeatureStartupSystem
{
    public readonly struct Result
    {
        public readonly RuntimeCitySpawnerSystem RuntimeCitySpawner;
        public readonly RuntimeGridBlockerSystem RuntimeGridBlockers;
        public readonly RuntimeDecorationSpawnerSystem RuntimeDecorations;

        public Result(
            RuntimeCitySpawnerSystem runtimeCitySpawner,
            RuntimeGridBlockerSystem runtimeGridBlockers,
            RuntimeDecorationSpawnerSystem runtimeDecorations)
        {
            RuntimeCitySpawner = runtimeCitySpawner;
            RuntimeGridBlockers = runtimeGridBlockers;
            RuntimeDecorations = runtimeDecorations;
        }
    }

    public Result Initialize(
        RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig,
        RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig,
        RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig,
        RoadBuildSystem roadBuild,
        BuildingPlacementSystem buildingPlacement,
        MainMenuPlayUI mainMenu,
        DayNightSystem dayNight,
        RTSSelectionSystem selection,
        CitizenPopulationSystem citizenPopulation,
        Transform runtimeCityRoot,
        Transform runtimeBlockerRoot,
        Transform decorationRoot,
        CombinedMeshBaker decorationCombinedMeshBaker,
        GameplaySceneBindingSystem sceneBindingSystem)
    {
        var runtimeCitySpawner = new RuntimeCitySpawnerSystem();
        runtimeCitySpawner.Init(
            runtimeCitySpawnerConfig,
            roadBuild,
            buildingPlacement?.RuntimeCitySpawnSystem,
            buildingPlacement != null ? buildingPlacement.CreateRuntimeCitySpawnContext() : default,
            runtimeCityRoot,
            mainMenu);

        var runtimeGridBlockers = new RuntimeGridBlockerSystem();
        runtimeGridBlockers.Init(runtimeGridBlockerConfig, runtimeBlockerRoot, runtimeCitySpawner);
        roadBuild?.BindDependencies(buildingPlacement, mainMenu, runtimeGridBlockers);
        sceneBindingSystem?.BindRuntimeGridBlockerDebugViews(runtimeGridBlockers);
        buildingPlacement?.BindDependencies(
            roadBuild,
            mainMenu,
            dayNight,
            selection,
            runtimeGridBlockers,
            runtimeCitySpawner,
            citizenPopulation);

        var runtimeDecorations = new RuntimeDecorationSpawnerSystem();
        runtimeDecorations.Init(
            runtimeDecorationSpawnerConfig,
            decorationRoot,
            decorationCombinedMeshBaker,
            runtimeCitySpawner,
            runtimeGridBlockers);

        return new Result(runtimeCitySpawner, runtimeGridBlockers, runtimeDecorations);
    }
}
