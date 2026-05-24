using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class ManagedGameplayStartupSystem
{
    public readonly struct Result
    {
        public readonly DayNightSystem DayNight;
        public readonly FactionVisualSettings FactionVisuals;
        public readonly RoadBuildSystem RoadBuild;
        public readonly BuildingPlacementSystem BuildingPlacement;
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

        var buildingPlacement = new BuildingPlacementSystem();
        buildingPlacement.Init(buildingPlacementConfig, worldCamera, runtimeUiRoot, roadBuild, null, factionVisuals, dayNight);
        var buildingRuntimeUpdate = new BuildingRuntimeUpdateSystem();
        var buildingRuntimeUpdateContext = new BuildingRuntimeUpdateSystem.Context(buildingPlacement.Update);

        var selection = new RTSSelectionSystem();
        selection.Init(
            rtsSelectionConfig,
            worldCamera,
            runtimeUiRoot,
            null,
            roadBuild,
            buildingPlacement.BuildingPlacementInteractionSystem,
            buildingPlacement.CreateBuildingPlacementInteractionContext(),
            factionVisuals);

        roadBuild.BindDependencies(
            buildingPlacement.BuildingPlacementInteractionSystem,
            buildingPlacement.CreateBuildingPlacementInteractionContext());
        buildingPlacement.BindDependencies(roadBuild, null, dayNight, selection);
        selection.BindDependencies(
            null,
            roadBuild,
            buildingPlacement.BuildingPlacementInteractionSystem,
            buildingPlacement.CreateBuildingPlacementInteractionContext());

        var unitAttackTraces = new UnitAttackTraceSystem();
        unitAttackTraces.Init(unitAttackTraceConfig, worldCamera, ownerLayer, factionVisuals);

        var unitImpostors = new UnitImpostorRenderSystem();
        unitImpostors.Init(worldCamera, ownerLayer, buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);

        var citizenPopulation = new CitizenPopulationSystem();
        citizenPopulation.Init(
            buildingPlacement.RuntimeQuerySystem,
            buildingPlacement.CreateRuntimeBuildingQueryContext(),
            dayNight,
            worldCamera,
            buildingPlacement.RuntimeResourceSystem.CreateCitizenResourceContext(),
            buildingPlacement.RuntimeUnitPrefabSystem.CreateCitizenPrefabContext(buildingPlacement.CreateRuntimeUnitPrefabContext()));
        buildingPlacement.BindDependencies(roadBuild, null, dayNight, selection, citizenPopulationSystem: citizenPopulation);

        GameStrings.Init(gameStringsConfig);
        SharedPrefabPreviewCache.Init(prefabPreviewCameraConfig);

        return new Result(
            dayNight,
            factionVisuals,
            roadBuild,
            buildingPlacement,
            buildingRuntimeUpdate,
            buildingRuntimeUpdateContext,
            selection,
            unitAttackTraces,
            unitImpostors,
            citizenPopulation);
    }
}
