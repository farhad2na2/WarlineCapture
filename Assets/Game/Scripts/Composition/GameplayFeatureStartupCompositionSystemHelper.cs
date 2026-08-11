using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.UI.Contracts;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class GameplayFeatureStartupCompositionSystemHelper
    {
        public readonly struct Result
        {
            public readonly RuntimeCityCompositionSystemHelper RuntimeCity;
            public readonly RuntimeGridBlockerPresentationSystemHelper RuntimeGridBlockers;
            public readonly RuntimeDecorationSpawnerPresentationSystemHelper RuntimeDecorations;

            public Result(
                RuntimeCityCompositionSystemHelper runtimeCity,
                RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers,
                RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations)
            {
                RuntimeCity = runtimeCity;
                RuntimeGridBlockers = runtimeGridBlockers;
                RuntimeDecorations = runtimeDecorations;
            }
        }

        public Result Initialize(
            bool enableLegacyRuntimeMapPresentation,
            RuntimeCitySpawnerSystemConfig runtimeCitySpawnerConfig,
            RuntimeGridBlockerSystemConfig runtimeGridBlockerConfig,
            RuntimeDecorationSpawnerSystemConfig runtimeDecorationSpawnerConfig,
            RoadRuntimeGenerationCompositionSystemHelper roadRuntimeGenerationHelper,
            RoadRuntimeGenerationCompositionSystemHelper.Context roadRuntimeGenerationContext,
            Action<IMatchRuntimeUi, RuntimeGridBlockerPresentationSystemHelper> bindRoadGameplayFeatures,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper buildingRuntimeCitySpawn,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context buildingRuntimeCitySpawnContext,
            BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteraction,
            BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext,
            Action<IMatchRuntimeUi, SelectionUiCameraSystemHelper, SelectionBuildingInteractionCompositionSystemHelper, RuntimeGridBlockerPresentationSystemHelper, RuntimeCityCompositionSystemHelper, CitizenPopulationEventCompositionSystemHelper> bindBuildingGameplayFeatures,
            IMatchRuntimeUi mainMenu,
            SelectionUiCameraSystemHelper selectionUiCameraSystem,
            SelectionBuildingInteractionCompositionSystemHelper selectionBuildingInteractionSystem,
            CitizenPopulationEventCompositionSystemHelper citizenPopulationEventSystem,
            Transform runtimeCityRoot,
            Transform runtimeBlockerRoot,
            Transform decorationRoot,
            CombinedMeshBaker decorationCombinedMeshBaker,
            IReadOnlyList<GridAuthoring> runtimeGridDebugViews,
            GameplaySceneBindingSceneSystemHelper sceneBindingSystem)
        {
            RuntimeCityCompositionSystemHelper runtimeCity = enableLegacyRuntimeMapPresentation
                ? ResolveRuntimeCityCompositionSystemHelper()
                : null;
            runtimeCity?.Configure(
                runtimeCitySpawnerConfig,
                roadRuntimeGenerationHelper,
                roadRuntimeGenerationContext,
                buildingRuntimeCitySpawn,
                buildingRuntimeCitySpawnContext,
                runtimeCityRoot,
                mainMenu);

            RuntimeCityReadModelCompositionSystemHelper runtimeCityReadModel = runtimeCity?.ReadModel;
            RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers =
                enableLegacyRuntimeMapPresentation
                    ? ResolveRuntimeGridBlockerPresentationHelper()
                    : null;
            runtimeGridBlockers?.Init(runtimeGridBlockerConfig, runtimeBlockerRoot, runtimeCityReadModel);
            bindRoadGameplayFeatures?.Invoke(mainMenu, runtimeGridBlockers);
#if UNITY_EDITOR
            World runtimeDebugWorld = buildingRuntimeCitySpawnContext.TryGetEntityManager != null &&
                                      buildingRuntimeCitySpawnContext.TryGetEntityManager(
                                          out EntityManager runtimeEntityManager)
                ? runtimeEntityManager.World
                : null;
            sceneBindingSystem?.BindRuntimeGridBlockerDebugViews(
                runtimeGridBlockers,
                runtimeDebugWorld,
                runtimeGridDebugViews);
#endif
            bindBuildingGameplayFeatures?.Invoke(
                mainMenu,
                selectionUiCameraSystem,
                selectionBuildingInteractionSystem,
                runtimeGridBlockers,
                runtimeCity,
                citizenPopulationEventSystem);

            RuntimeDecorationSpawnerPresentationSystemHelper runtimeDecorations =
                enableLegacyRuntimeMapPresentation
                    ? ResolveRuntimeDecorationSpawnerPresentationHelper()
                    : null;
            runtimeDecorations?.Init(
                runtimeDecorationSpawnerConfig,
                decorationRoot,
                decorationCombinedMeshBaker,
                runtimeCityReadModel,
                runtimeGridBlockers);

            return new Result(runtimeCity, runtimeGridBlockers, runtimeDecorations);
        }

        private static RuntimeGridBlockerPresentationSystemHelper ResolveRuntimeGridBlockerPresentationHelper()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? new RuntimeGridBlockerPresentationSystemHelper()
                : null;
        }

        private static RuntimeCityCompositionSystemHelper ResolveRuntimeCityCompositionSystemHelper()
        {
            return new RuntimeCityCompositionSystemHelper();
        }

        private static RuntimeDecorationSpawnerPresentationSystemHelper ResolveRuntimeDecorationSpawnerPresentationHelper()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            return world != null && world.IsCreated
                ? new RuntimeDecorationSpawnerPresentationSystemHelper()
                : null;
        }
    }
}
