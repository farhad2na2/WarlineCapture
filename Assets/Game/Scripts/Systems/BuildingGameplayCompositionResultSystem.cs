using System;
using UnityEngine;

internal sealed class BuildingGameplayCompositionResultSystem
{
    public Result Create(
        BuildingSelectionClickSystem selectionClick,
        BuildingSelectionClickSystem.Context selectionClickContext,
        BuildingRuntimeUpdateSystem runtimeUpdate,
        BuildingRuntimeUpdateSystem.Context runtimeUpdateContext,
        BuildingRuntimeCitySpawnSystem runtimeCitySpawn,
        BuildingRuntimeCitySpawnSystem.Context runtimeCitySpawnContext,
        BuildingRuntimeQuerySystem runtimeQuery,
        BuildingRuntimeQuerySystem.Context runtimeQueryContext,
        BuildingRuntimeSpawnCommandSystem runtimeSpawnCommand,
        BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext,
        BuildingSpawnSystem spawn,
        BuildingSpawnSystem.Context spawnContext,
        Func<BuildingSpawnSystem.Context> createSpawnContext,
        BuildingBarrierSystem barrier,
        Func<BuildingBarrierSystem.Context> createBarrierContext,
        BuildingCombatSystem combat,
        Func<BuildingCombatSystem.Context<RuntimeBuildingEntity>> createCombatContext,
        BuildingUiCommandBoundary uiCommand,
        BuildingUiCommandBoundary.Context uiCommandContext,
        BuildingUiQuerySystem uiQuery,
        BuildingUiQuerySystem.Context uiQueryContext,
        BuildingPlacementInteractionSystem interaction,
        BuildingPlacementInteractionSystem.Context interactionContext,
        BuildingGameplayDependencySystem dependencySystem,
        BuildingRuntimeResourcePrefabContextSystem runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource,
        BuildingCitizenPopulationCompositionSystem citizenPopulationCompositionSystem,
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
        System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        Func<Rect, bool> trySelectFirstBuildingInScreenRect,
        Action<IMatchRuntimeUi> bindMainMenu,
        Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> bindGameplayFeatures,
        Action dispose)
    {
        return new Result(
            selectionClick,
            selectionClickContext,
            runtimeUpdate,
            runtimeUpdateContext,
            runtimeCitySpawn,
            runtimeCitySpawnContext,
            runtimeQuery,
            runtimeQueryContext,
            runtimeSpawnCommand,
            runtimeSpawnCommandContext,
            spawn,
            spawnContext,
            createSpawnContext,
            barrier,
            createBarrierContext,
            combat,
            createCombatContext,
            uiCommand,
            uiCommandContext,
            uiQuery,
            uiQueryContext,
            interaction,
            interactionContext,
            dependencySystem,
            runtimeResourcePrefabContextSystem,
            runtimeResourcePrefabSource,
            citizenPopulationCompositionSystem,
            citizenPopulationCompositionBoundary,
            citizenPopulationComposition,
            runtimeBuildings,
            trySelectFirstBuildingInScreenRect,
            bindMainMenu,
            bindGameplayFeatures,
            dispose);
    }

    public readonly struct Result
    {
        public readonly BuildingSelectionClickSystem SelectionClick;
        public readonly BuildingSelectionClickSystem.Context SelectionClickContext;
        public readonly BuildingRuntimeUpdateSystem RuntimeUpdate;
        public readonly BuildingRuntimeUpdateSystem.Context RuntimeUpdateContext;
        public readonly BuildingRuntimeCitySpawnSystem RuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnSystem.Context RuntimeCitySpawnContext;
        public readonly BuildingRuntimeQuerySystem RuntimeQuery;
        public readonly BuildingRuntimeQuerySystem.Context RuntimeQueryContext;
        public readonly BuildingRuntimeSpawnCommandSystem RuntimeSpawnCommand;
        public readonly BuildingRuntimeSpawnCommandSystem.Context RuntimeSpawnCommandContext;
        public readonly BuildingSpawnSystem Spawn;
        public readonly BuildingSpawnSystem.Context SpawnContext;
        public readonly Func<BuildingSpawnSystem.Context> CreateSpawnContext;
        public readonly BuildingBarrierSystem Barrier;
        public readonly Func<BuildingBarrierSystem.Context> CreateBarrierContext;
        public readonly BuildingCombatSystem Combat;
        public readonly Func<BuildingCombatSystem.Context<RuntimeBuildingEntity>> CreateCombatContext;
        public readonly BuildingUiCommandBoundary UiCommand;
        public readonly BuildingUiCommandBoundary.Context UiCommandContext;
        public readonly BuildingUiQuerySystem UiQuery;
        public readonly BuildingUiQuerySystem.Context UiQueryContext;
        public readonly BuildingPlacementInteractionSystem Interaction;
        public readonly BuildingPlacementInteractionSystem.Context InteractionContext;
        private readonly BuildingGameplayDependencySystem DependencySystem;
        private readonly BuildingRuntimeResourcePrefabContextSystem RuntimeResourcePrefabContextSystem;
        private readonly BuildingRuntimeResourcePrefabContextSystem.Source RuntimeResourcePrefabSource;
        private readonly BuildingCitizenPopulationCompositionSystem CitizenPopulationCompositionBridge;
        private readonly CitizenPopulationCompositionSystem CitizenPopulationCompositionBoundary;
        public readonly CitizenPopulationCompositionSystem.Result CitizenPopulationComposition;
        public readonly System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Func<Rect, bool> TrySelectFirstBuildingInScreenRect;
        public readonly Action<IMatchRuntimeUi> BindMainMenu;
        public readonly Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> BindGameplayFeatures;
        public readonly Action Dispose;

        public Result(
            BuildingSelectionClickSystem selectionClick,
            BuildingSelectionClickSystem.Context selectionClickContext,
            BuildingRuntimeUpdateSystem runtimeUpdate,
            BuildingRuntimeUpdateSystem.Context runtimeUpdateContext,
            BuildingRuntimeCitySpawnSystem runtimeCitySpawn,
            BuildingRuntimeCitySpawnSystem.Context runtimeCitySpawnContext,
            BuildingRuntimeQuerySystem runtimeQuery,
            BuildingRuntimeQuerySystem.Context runtimeQueryContext,
            BuildingRuntimeSpawnCommandSystem runtimeSpawnCommand,
            BuildingRuntimeSpawnCommandSystem.Context runtimeSpawnCommandContext,
            BuildingSpawnSystem spawn,
            BuildingSpawnSystem.Context spawnContext,
            Func<BuildingSpawnSystem.Context> createSpawnContext,
            BuildingBarrierSystem barrier,
            Func<BuildingBarrierSystem.Context> createBarrierContext,
            BuildingCombatSystem combat,
            Func<BuildingCombatSystem.Context<RuntimeBuildingEntity>> createCombatContext,
            BuildingUiCommandBoundary uiCommand,
            BuildingUiCommandBoundary.Context uiCommandContext,
            BuildingUiQuerySystem uiQuery,
            BuildingUiQuerySystem.Context uiQueryContext,
            BuildingPlacementInteractionSystem interaction,
            BuildingPlacementInteractionSystem.Context interactionContext,
            BuildingGameplayDependencySystem dependencySystem,
            BuildingRuntimeResourcePrefabContextSystem runtimeResourcePrefabContextSystem,
            BuildingRuntimeResourcePrefabContextSystem.Source runtimeResourcePrefabSource,
            BuildingCitizenPopulationCompositionSystem citizenPopulationCompositionSystem,
            CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
            CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
            System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Func<Rect, bool> trySelectFirstBuildingInScreenRect,
            Action<IMatchRuntimeUi> bindMainMenu,
            Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerSystem, RuntimeCityCompositionSystem, CitizenPopulationEventSystem> bindGameplayFeatures,
            Action dispose)
        {
            SelectionClick = selectionClick;
            SelectionClickContext = selectionClickContext;
            RuntimeUpdate = runtimeUpdate;
            RuntimeUpdateContext = runtimeUpdateContext;
            RuntimeCitySpawn = runtimeCitySpawn;
            RuntimeCitySpawnContext = runtimeCitySpawnContext;
            RuntimeQuery = runtimeQuery;
            RuntimeQueryContext = runtimeQueryContext;
            RuntimeSpawnCommand = runtimeSpawnCommand;
            RuntimeSpawnCommandContext = runtimeSpawnCommandContext;
            Spawn = spawn;
            SpawnContext = spawnContext;
            CreateSpawnContext = createSpawnContext;
            Barrier = barrier;
            CreateBarrierContext = createBarrierContext;
            Combat = combat;
            CreateCombatContext = createCombatContext;
            UiCommand = uiCommand;
            UiCommandContext = uiCommandContext;
            UiQuery = uiQuery;
            UiQueryContext = uiQueryContext;
            Interaction = interaction;
            InteractionContext = interactionContext;
            DependencySystem = dependencySystem;
            RuntimeResourcePrefabContextSystem = runtimeResourcePrefabContextSystem;
            RuntimeResourcePrefabSource = runtimeResourcePrefabSource;
            CitizenPopulationCompositionBridge = citizenPopulationCompositionSystem;
            CitizenPopulationCompositionBoundary = citizenPopulationCompositionBoundary;
            CitizenPopulationComposition = citizenPopulationComposition;
            RuntimeBuildings = runtimeBuildings;
            TrySelectFirstBuildingInScreenRect = trySelectFirstBuildingInScreenRect;
            BindMainMenu = bindMainMenu;
            BindGameplayFeatures = bindGameplayFeatures;
            Dispose = dispose;
        }

        public void BindSelection(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
            Func<bool> shouldBlockBuildingSelectionClick)
        {
            DependencySystem?.BindRuntimeDependencies(
                null,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                shouldBlockBuildingSelectionClick: shouldBlockBuildingSelectionClick);
        }

        public void InitializeCitizenPopulation(DayNightSystem dayNight, Camera worldCamera, RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig)
        {
            CitizenPopulationCompositionBridge.Initialize(
                CitizenPopulationCompositionBoundary,
                CitizenPopulationComposition,
                RuntimeResourcePrefabContextSystem,
                RuntimeResourcePrefabSource,
                RuntimeQuery,
                RuntimeQueryContext,
                dayNight,
                worldCamera,
                runtimeCitySpawnerConfig != null && runtimeCitySpawnerConfig.CityCount > 0);
        }

        public void DisposeCitizenPopulation()
        {
            CitizenPopulationCompositionBridge.Dispose(
                CitizenPopulationCompositionBoundary,
                CitizenPopulationComposition);
        }

        public void BindCitizenPopulation(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
            CitizenPopulationEventSystem citizenPopulationEventSystem)
        {
            CitizenPopulationCompositionBridge.Bind(
                DependencySystem,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                citizenPopulationEventSystem);
        }
    }
}
