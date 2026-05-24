using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class ManagedGameplayStartupSystem
{
    private readonly BuildingGameplayCompositionSystem _buildingGameplayCompositionSystem = new();

    public readonly struct Result
    {
        public readonly DayNightSystem DayNight;
        public readonly FactionVisualSettings FactionVisuals;
        public readonly RoadBuildSystem RoadBuild;
        public readonly BuildingPlacementSystem BuildingPlacement;
        public readonly BuildingSelectionClickSystem BuildingSelectionClick;
        public readonly BuildingSelectionClickSystem.Context BuildingSelectionClickContext;
        public readonly BuildingRuntimeCitySpawnSystem BuildingRuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnSystem.Context BuildingRuntimeCitySpawnContext;
        public readonly BuildingUiCommandSystem BuildingUiCommand;
        public readonly BuildingUiCommandSystem.Context BuildingUiCommandContext;
        public readonly BuildingUiQuerySystem BuildingUiQuery;
        public readonly BuildingUiQuerySystem.Context BuildingUiQueryContext;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteraction;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly System.Action<MainMenuPlayUI, RTSSelectionSystem> BindBuildingMainMenu;
        public readonly System.Action<MainMenuPlayUI, RTSSelectionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> BindBuildingGameplayFeatures;
        public readonly BuildingRuntimeUpdateSystem BuildingRuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context BuildingRuntimeUpdateContext;
        public readonly RTSSelectionSystem Selection;
        public readonly UnitAttackTraceSystem UnitAttackTraces;
        public readonly UnitImpostorRenderSystem UnitImpostors;
        public readonly CitizenPopulationSystem CitizenPopulation;

        public Result(
            DayNightSystem dayNight,
            FactionVisualSettings factionVisuals,
            RoadBuildSystem roadBuild,
            BuildingPlacementSystem buildingPlacement,
            BuildingSelectionClickSystem buildingSelectionClick,
            BuildingSelectionClickSystem.Context buildingSelectionClickContext,
            BuildingRuntimeCitySpawnSystem buildingRuntimeCitySpawn,
            BuildingRuntimeCitySpawnSystem.Context buildingRuntimeCitySpawnContext,
            BuildingUiCommandSystem buildingUiCommand,
            BuildingUiCommandSystem.Context buildingUiCommandContext,
            BuildingUiQuerySystem buildingUiQuery,
            BuildingUiQuerySystem.Context buildingUiQueryContext,
            BuildingPlacementInteractionSystem buildingPlacementInteraction,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            System.Action<MainMenuPlayUI, RTSSelectionSystem> bindBuildingMainMenu,
            System.Action<MainMenuPlayUI, RTSSelectionSystem, RuntimeGridBlockerSystem, RuntimeCitySpawnerSystem, CitizenPopulationSystem> bindBuildingGameplayFeatures,
            BuildingRuntimeUpdateSystem buildingRuntimeUpdate,
            BuildingRuntimeUpdateSystem.Context buildingRuntimeUpdateContext,
            RTSSelectionSystem selection,
            UnitAttackTraceSystem unitAttackTraces,
            UnitImpostorRenderSystem unitImpostors,
            CitizenPopulationSystem citizenPopulation)
        {
            DayNight = dayNight;
            FactionVisuals = factionVisuals;
            RoadBuild = roadBuild;
            BuildingPlacement = buildingPlacement;
            BuildingSelectionClick = buildingSelectionClick;
            BuildingSelectionClickContext = buildingSelectionClickContext;
            BuildingRuntimeCitySpawn = buildingRuntimeCitySpawn;
            BuildingRuntimeCitySpawnContext = buildingRuntimeCitySpawnContext;
            BuildingUiCommand = buildingUiCommand;
            BuildingUiCommandContext = buildingUiCommandContext;
            BuildingUiQuery = buildingUiQuery;
            BuildingUiQueryContext = buildingUiQueryContext;
            BuildingPlacementInteraction = buildingPlacementInteraction;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            BindBuildingMainMenu = bindBuildingMainMenu;
            BindBuildingGameplayFeatures = bindBuildingGameplayFeatures;
            BuildingRuntimeUpdate = buildingRuntimeUpdate;
            BuildingRuntimeUpdateContext = buildingRuntimeUpdateContext;
            Selection = selection;
            UnitAttackTraces = unitAttackTraces;
            UnitImpostors = unitImpostors;
            CitizenPopulation = citizenPopulation;
        }
    }

    public Result Initialize(
        DayNightSystemConfig dayNightConfig,
        FactionVisualSettingsConfig factionVisualConfig,
        RoadBuildSystemConfig roadBuildConfig,
        BuildingPlacementSystemConfig buildingPlacementConfig,
        RTSSelectionSystemConfig rtsSelectionConfig,
        UnitAttackTraceSystemConfig unitAttackTraceConfig,
        GameStringsConfig gameStringsConfig,
        PrefabPreviewCameraConfig prefabPreviewCameraConfig,
        Camera worldCamera,
        Light directionalLight,
        Volume globalVolume,
        Transform runtimeUiRoot,
        int ownerLayer)
    {
        var dayNight = new DayNightSystem();
        dayNight.Init(dayNightConfig, directionalLight, globalVolume);

        var factionVisuals = new FactionVisualSettings();
        factionVisuals.Init(factionVisualConfig);

        var roadBuild = new RoadBuildSystem();
        roadBuild.Init(roadBuildConfig, worldCamera, runtimeUiRoot, null);

        BuildingGameplayCompositionSystem.Result building = _buildingGameplayCompositionSystem.Initialize(
            buildingPlacementConfig,
            worldCamera,
            runtimeUiRoot,
            roadBuild,
            factionVisuals,
            dayNight);

        var selection = new RTSSelectionSystem();
        selection.Init(
            rtsSelectionConfig,
            worldCamera,
            runtimeUiRoot,
            null,
            roadBuild,
            building.Interaction,
            building.InteractionContext,
            factionVisuals);

        roadBuild.BindDependencies(
            building.Interaction,
            building.InteractionContext);
        _buildingGameplayCompositionSystem.BindSelection(building, roadBuild, dayNight, selection);
        selection.BindDependencies(
            null,
            roadBuild,
            building.Interaction,
            building.InteractionContext);

        var unitAttackTraces = new UnitAttackTraceSystem();
        unitAttackTraces.Init(unitAttackTraceConfig, worldCamera, ownerLayer, factionVisuals);

        var unitImpostors = new UnitImpostorRenderSystem();
        unitImpostors.Init(worldCamera, ownerLayer, buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);

        CitizenPopulationSystem citizenPopulation = _buildingGameplayCompositionSystem.CreateCitizenPopulation(
            building,
            dayNight,
            worldCamera);
        _buildingGameplayCompositionSystem.BindCitizenPopulation(building, roadBuild, dayNight, selection, citizenPopulation);

        GameStrings.Init(gameStringsConfig);
        SharedPrefabPreviewCache.Init(prefabPreviewCameraConfig);

        return new Result(
            dayNight,
            factionVisuals,
            roadBuild,
            building.PlacementFacade,
            building.SelectionClick,
            building.SelectionClickContext,
            building.RuntimeCitySpawn,
            building.RuntimeCitySpawnContext,
            building.UiCommand,
            building.UiCommandContext,
            building.UiQuery,
            building.UiQueryContext,
            building.Interaction,
            building.InteractionContext,
            building.BindMainMenu,
            building.BindGameplayFeatures,
            building.RuntimeUpdate,
            building.RuntimeUpdateContext,
            selection,
            unitAttackTraces,
            unitImpostors,
            citizenPopulation);
    }
}
