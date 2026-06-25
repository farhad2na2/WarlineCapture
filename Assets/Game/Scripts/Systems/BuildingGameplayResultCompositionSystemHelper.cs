using System;
using UnityEngine;

internal sealed class BuildingGameplayResultCompositionSystemHelper
{
    public Result Create(
        BuildingSelectionClickSystem selectionClick,
        BuildingSelectionClickSystem.Context selectionClickContext,
        BuildingRuntimeUpdateCompositionSystemHelper runtimeUpdate,
        BuildingRuntimeUpdateCompositionSystemHelper.Context runtimeUpdateContext,
        BuildingRuntimeCitySpawnBridgeCompositionSystemHelper runtimeCitySpawn,
        BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context runtimeCitySpawnContext,
        BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
        BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
        BuildingRuntimeSpawnCommandBoundary runtimeSpawnCommand,
        BuildingRuntimeSpawnCommandBoundary.Context runtimeSpawnCommandContext,
        BuildingSpawnSystem spawn,
        BuildingSpawnSystem.Context spawnContext,
        Func<BuildingSpawnSystem.Context> createSpawnContext,
        BuildingBarrierUtilitySystemHelper barrier,
        Func<BuildingBarrierUtilitySystemHelper.Context> createBarrierContext,
        BuildingCombatUtilitySystemHelper combat,
        Func<BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>> createCombatContext,
        BuildingUiCommandBoundary uiCommand,
        BuildingUiCommandBoundary.Context uiCommandContext,
        BuildingUiQuerySystem uiQuery,
        BuildingUiQuerySystem.Context uiQueryContext,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper interaction,
        BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
        BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
        BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
        RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
        BuildingCitizenPopulationCompositionSystemHelper citizenPopulationCompositionSystem,
        CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
        CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
        System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
        Func<Rect, bool> trySelectFirstBuildingInScreenRect,
        Action<IMatchRuntimeUi> bindMainMenu,
        Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventSystem> bindGameplayFeatures,
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
            runtimeBuildingEntityLinks,
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
        public readonly BuildingRuntimeUpdateCompositionSystemHelper RuntimeUpdate;
        public readonly BuildingRuntimeUpdateCompositionSystemHelper.Context RuntimeUpdateContext;
        public readonly BuildingRuntimeCitySpawnBridgeCompositionSystemHelper RuntimeCitySpawn;
        public readonly BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context RuntimeCitySpawnContext;
        public readonly BuildingRuntimeReadModelCompositionSystemHelper RuntimeQuery;
        public readonly BuildingRuntimeReadModelCompositionSystemHelper.Context RuntimeQueryContext;
        public readonly BuildingRuntimeSpawnCommandBoundary RuntimeSpawnCommand;
        public readonly BuildingRuntimeSpawnCommandBoundary.Context RuntimeSpawnCommandContext;
        public readonly BuildingSpawnSystem Spawn;
        public readonly BuildingSpawnSystem.Context SpawnContext;
        public readonly Func<BuildingSpawnSystem.Context> CreateSpawnContext;
        public readonly BuildingBarrierUtilitySystemHelper Barrier;
        public readonly Func<BuildingBarrierUtilitySystemHelper.Context> CreateBarrierContext;
        public readonly BuildingCombatUtilitySystemHelper Combat;
        public readonly Func<BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>> CreateCombatContext;
        public readonly BuildingUiCommandBoundary UiCommand;
        public readonly BuildingUiCommandBoundary.Context UiCommandContext;
        public readonly BuildingUiQuerySystem UiQuery;
        public readonly BuildingUiQuerySystem.Context UiQueryContext;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper Interaction;
        public readonly BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context InteractionContext;
        private readonly BuildingGameplayDependencyCompositionSystemHelper DependencySystem;
        private readonly BuildingRuntimeResourcePrefabContextCompositionSystemHelper RuntimeResourcePrefabContextSystem;
        private readonly BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source RuntimeResourcePrefabSource;
        public readonly RuntimeBuildingEntityLinkRegistry RuntimeBuildingEntityLinks;
        private readonly BuildingCitizenPopulationCompositionSystemHelper CitizenPopulationCompositionBridge;
        private readonly CitizenPopulationCompositionSystem CitizenPopulationCompositionBoundary;
        public readonly CitizenPopulationCompositionSystem.Result CitizenPopulationComposition;
        public readonly System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly Func<Rect, bool> TrySelectFirstBuildingInScreenRect;
        public readonly Action<IMatchRuntimeUi> BindMainMenu;
        public readonly Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventSystem> BindGameplayFeatures;
        public readonly Action Dispose;

        public Result(
            BuildingSelectionClickSystem selectionClick,
            BuildingSelectionClickSystem.Context selectionClickContext,
            BuildingRuntimeUpdateCompositionSystemHelper runtimeUpdate,
            BuildingRuntimeUpdateCompositionSystemHelper.Context runtimeUpdateContext,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper runtimeCitySpawn,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context runtimeCitySpawnContext,
            BuildingRuntimeReadModelCompositionSystemHelper runtimeQuery,
            BuildingRuntimeReadModelCompositionSystemHelper.Context runtimeQueryContext,
            BuildingRuntimeSpawnCommandBoundary runtimeSpawnCommand,
            BuildingRuntimeSpawnCommandBoundary.Context runtimeSpawnCommandContext,
            BuildingSpawnSystem spawn,
            BuildingSpawnSystem.Context spawnContext,
            Func<BuildingSpawnSystem.Context> createSpawnContext,
            BuildingBarrierUtilitySystemHelper barrier,
            Func<BuildingBarrierUtilitySystemHelper.Context> createBarrierContext,
            BuildingCombatUtilitySystemHelper combat,
            Func<BuildingCombatUtilitySystemHelper.Context<RuntimeBuildingEntity>> createCombatContext,
            BuildingUiCommandBoundary uiCommand,
            BuildingUiCommandBoundary.Context uiCommandContext,
            BuildingUiQuerySystem uiQuery,
            BuildingUiQuerySystem.Context uiQueryContext,
            BuildingPlacementInteractionBoundaryCompositionSystemHelper interaction,
            BuildingPlacementInteractionBoundaryCompositionSystemHelper.Context interactionContext,
            BuildingGameplayDependencyCompositionSystemHelper dependencySystem,
            BuildingRuntimeResourcePrefabContextCompositionSystemHelper runtimeResourcePrefabContextSystem,
            BuildingRuntimeResourcePrefabContextCompositionSystemHelper.Source runtimeResourcePrefabSource,
            RuntimeBuildingEntityLinkRegistry runtimeBuildingEntityLinks,
            BuildingCitizenPopulationCompositionSystemHelper citizenPopulationCompositionSystem,
            CitizenPopulationCompositionSystem citizenPopulationCompositionBoundary,
            CitizenPopulationCompositionSystem.Result citizenPopulationComposition,
            System.Collections.Generic.IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            Func<Rect, bool> trySelectFirstBuildingInScreenRect,
            Action<IMatchRuntimeUi> bindMainMenu,
            Action<IMatchRuntimeUi, SelectionUiCameraSystem, SelectionBuildingInteractionSystem, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventSystem> bindGameplayFeatures,
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
            RuntimeBuildingEntityLinks = runtimeBuildingEntityLinks;
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
            BuildingCitizenPopulationCompositionSystemHelper.Initialize(
                CitizenPopulationCompositionBridge,
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
            BuildingCitizenPopulationCompositionSystemHelper.Dispose(
                CitizenPopulationCompositionBridge,
                CitizenPopulationCompositionBoundary,
                CitizenPopulationComposition);
        }

        public void BindCitizenPopulation(
            DayNightSystem dayNight,
            SelectionUiCameraSystem selectionUiCameraSystem,
            SelectionBuildingInteractionSystem selectionBuildingInteractionSystem,
            CitizenPopulationEventSystem citizenPopulationEventSystem)
        {
            BuildingCitizenPopulationCompositionSystemHelper.Bind(
                CitizenPopulationCompositionBridge,
                DependencySystem,
                dayNight,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                citizenPopulationEventSystem);
        }
    }
}
