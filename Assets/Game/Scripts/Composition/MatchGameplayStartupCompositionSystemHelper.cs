using System;
using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Composition
{
    internal sealed class MatchGameplayStartupCompositionSystemHelper
    {
        private enum GameplayStartStep : byte
        {
            Idle = 0,
            InitializeManagedRuntime,
            ResetStats,
            ProjectStartupConfig,
            CustomGameStartup,
            AiStartup,
            ResourceExchangeStartup,
            BindMainMenu,
            InitializeGameplayFeatures,
            ValidateScenarioRecovery,
            FinalizeRuntimeState,
            Complete
        }

        private readonly InitialFactionSpawnCellSystem initialFactionSpawnCellSystem = new();
        private readonly List<InitialFactionSpawnCellFallbackEntry> initialFactionSpawnCellFallbackEntries = new();
        private RuntimeGridBootstrapStartupSystemHelper runtimeGridBootstrapSystem;
        private MapSurfaceRuntimeBootstrapSceneSystemHelper mapSurfaceRuntimeBootstrapSystem;
        private CustomGameStartupSystemHelper customGameStartupSystem;
        private MaterialsScenarioRecoveryStartupSystemHelper materialsScenarioRecoveryStartupSystem;
        private ResourceExchangeStartupProjectionSystemHelper resourceExchangeStartupProjectionSystem;
        private AIStartupSystem aiStartupSystem;
        private MatchSceneView sceneView;
        private RuntimeGameplayStateSystem runtimeGameplayStateSystem;
        private Action initializeManagedRuntime;
        private Action bindMainMenu;
        private Action initializeGameplayFeatures;
        private Func<World, RuntimeCameraReferenceSystem> resolveRuntimeCameraReferenceSystem;
        private Action<Exception> reportFailure = Debug.LogException;
        private bool gameplayStartPending;
        private bool gameplayStartRequested;
        private bool gameplayStartComplete;
        private bool gameplayStartFailed;
        private string gameplayStartFailureMessage = string.Empty;
        private double materialsScenarioValidationStartedAt = -1d;
        private GameplayStartStep gameplayStartStep;
        private AISettingsSnapshot pendingAiSettingsSnapshot;
        private AIStartupSystem.Result pendingAiStartupResult;
        private float gameplayStartProgress01;
        private string gameplayStartStatus = "Waiting for match scene";

        public bool GameplayStartRequested => gameplayStartRequested;
        public bool GameplayStartComplete => gameplayStartComplete && !gameplayStartPending;
        public bool GameplayStartFailed => gameplayStartFailed;
        public string GameplayStartFailureMessage => gameplayStartFailureMessage;
        public float GameplayStartProgress01 => gameplayStartComplete && gameplayStartPending
            ? 0.98f
            : gameplayStartProgress01;
        public string GameplayStartStatus => gameplayStartComplete && gameplayStartPending
            ? "Spawning world"
            : gameplayStartStatus;
        internal MapSurfaceRuntimeBootstrapSceneSystemHelper MapSurfaceRuntimeBootstrapSystem =>
            mapSurfaceRuntimeBootstrapSystem;
        internal ref bool PendingState => ref gameplayStartPending;

        public void Bind(
            MatchSceneView view,
            RuntimeGameplayStateSystem runtimeState,
            Action initializeManaged,
            Action bindMenu,
            Action initializeFeatures,
            Func<World, RuntimeCameraReferenceSystem> resolveCameraReference,
            Action<Exception> failureReporter = null)
        {
            sceneView = view;
            runtimeGameplayStateSystem = runtimeState;
            initializeManagedRuntime = initializeManaged;
            bindMainMenu = bindMenu;
            initializeGameplayFeatures = initializeFeatures;
            resolveRuntimeCameraReferenceSystem = resolveCameraReference;
            reportFailure = failureReporter ?? Debug.LogException;
        }

        public void BeginGameplay()
        {
            if (gameplayStartComplete || gameplayStartRequested)
                return;

            gameplayStartRequested = true;
            gameplayStartComplete = false;
            gameplayStartStep = GameplayStartStep.InitializeManagedRuntime;
            gameplayStartProgress01 = 0f;
            gameplayStartStatus = "Preparing match";
        }

        public void Advance(
            BuildingRuntimeUpdateCompositionSystemHelper buildingRuntimeUpdate,
            in BuildingRuntimeUpdateCompositionSystemHelper.Context buildingRuntimeUpdateContext)
        {
            if (gameplayStartFailed)
                return;

            try
            {
                AdvanceStep(buildingRuntimeUpdate, buildingRuntimeUpdateContext);
            }
            catch (Exception exception)
            {
                gameplayStartFailed = true;
                gameplayStartPending = false;
                gameplayStartFailureMessage = exception.Message;
                gameplayStartStatus = exception.Message;
                reportFailure?.Invoke(exception);
            }
        }

        public void ResetForShutdown()
        {
            runtimeGridBootstrapSystem = null;
            mapSurfaceRuntimeBootstrapSystem = null;
            initialFactionSpawnCellFallbackEntries.Clear();
            customGameStartupSystem = null;
            aiStartupSystem = default;
            gameplayStartRequested = false;
            gameplayStartComplete = false;
            gameplayStartStep = GameplayStartStep.Idle;
            gameplayStartProgress01 = 0f;
            gameplayStartStatus = "Waiting for match scene";
        }

        private void AdvanceStep(
            BuildingRuntimeUpdateCompositionSystemHelper buildingRuntimeUpdate,
            in BuildingRuntimeUpdateCompositionSystemHelper.Context buildingRuntimeUpdateContext)
        {
            if (!gameplayStartRequested || gameplayStartComplete)
                return;

            World world = World.DefaultGameObjectInjectionWorld;
            BuildingPlacementSystemConfig buildingPlacementConfig =
                sceneView != null ? sceneView.BuildingPlacementConfig : null;
            IReadOnlyList<AIControllerConfig> aiControllerConfigs =
                sceneView != null ? sceneView.AIControllerConfigs : Array.Empty<AIControllerConfig>();
            ResourceExchangeRecipeConfigSet resourceExchangeConfig =
                sceneView != null ? sceneView.ResourceExchangeConfig : null;

            switch (gameplayStartStep)
            {
                case GameplayStartStep.InitializeManagedRuntime:
                    SetProgress(0.02f, "Preparing gameplay runtime");
                    initializeManagedRuntime?.Invoke();
                    gameplayStartStep = GameplayStartStep.ResetStats;
                    break;

                case GameplayStartStep.ResetStats:
                    SetProgress(0.10f, "Resetting match state");
                    GameRuntimeStats.ConfigureUnitPrefabClassifier(
                        GameRuntimeStatsUnitPrefabClassifierPrefabSystemHelper.ClassifyUnitPrefab);
                    GameRuntimeStats.Reset();
                    pendingAiSettingsSnapshot = AISettingsRuntimeState.CurrentSnapshot;
                    FactionVisualSystem.ProjectConfig(
                        world,
                        sceneView != null ? sceneView.FactionVisualConfig : null);
                    gameplayStartStep = GameplayStartStep.ProjectStartupConfig;
                    break;

                case GameplayStartStep.ProjectStartupConfig:
                    SetProgress(0.24f, "Preparing map data");
                    MatchBootstrapStartupConfigProjection.ProjectRuntimeStartupConfig(
                        world,
                        ResolveRuntimeGridBootstrapStartupSystemHelper(world),
                        ResolveMapSurfaceRuntimeBootstrapSystem(world),
                        sceneView != null ? sceneView.RuntimeGridConfig : null,
                        sceneView != null ? sceneView.MapSurfaceAuthoring : null,
                        buildingPlacementConfig,
                        ResolveAIStartupSystem(world),
                        aiControllerConfigs,
                        pendingAiSettingsSnapshot,
                        initialFactionSpawnCellFallbackEntries);
                    gameplayStartStep = GameplayStartStep.CustomGameStartup;
                    break;

                case GameplayStartStep.CustomGameStartup:
                    SetProgress(0.38f, "Preparing unit prefabs");
                    CustomGameStartupSystemHelper customStartup = ResolveCustomGameStartupSystemHelper(world);
                    if (customStartup != null)
                    {
                        customStartup.InitializeFromLegacyConfigs(
                            buildingPlacementConfig != null ? buildingPlacementConfig.InitialUnitsConfig : null,
                            buildingPlacementConfig != null ? buildingPlacementConfig.UnitPrefabRegistryConfig : null);
                    }
                    else
                    {
                        Debug.LogWarning("[MatchBootstrap] missingCustomGameStartupSystemHelper");
                    }

                    gameplayStartStep = GameplayStartStep.AiStartup;
                    break;

                case GameplayStartStep.AiStartup:
                    SetProgress(0.52f, "Preparing AI factions");
                    pendingAiStartupResult = MatchBootstrapStartupConfigProjection.InitializeAiStartupConfig(
                        ResolveAIStartupSystem(world),
                        aiControllerConfigs,
                        sceneView != null ? sceneView.AIPlanEntryConfig : null,
                        pendingAiSettingsSnapshot,
                        ResolveInitialFactionSpawnCell);
                    if (pendingAiStartupResult.HasPlayerAutoMode)
                    {
                        runtimeGameplayStateSystem.PlayerAutoModeEnabled =
                            pendingAiStartupResult.PlayerAutoModeEnabled;
                    }

                    gameplayStartStep = GameplayStartStep.ResourceExchangeStartup;
                    break;

                case GameplayStartStep.ResourceExchangeStartup:
                    SetProgress(0.60f, "Preparing resource exchange");
                    ResourceExchangeStartupProjectionSystemHelper exchangeStartup =
                        ResolveResourceExchangeStartupProjectionSystemHelper(world);
                    ResourceExchangeStartupProjectionSystemHelper.Result exchangeResult =
                        exchangeStartup != null ? exchangeStartup.Initialize(resourceExchangeConfig) : default;
                    if (!exchangeResult.Projected)
                    {
                        Debug.LogWarning(
                            $"[MatchBootstrap] resourceExchangeProjectionSkipped reason={exchangeResult.Reason}");
                    }

                    if (exchangeStartup != null)
                    {
                        ResourceExchangeStartupProjectionSystemHelper.AIProjectionResult aiExchangeResult =
                            exchangeStartup.InitializeEligibleAIFactions(resourceExchangeConfig);
                        if (aiExchangeResult.ScenarioAllowsAIExchange &&
                            aiExchangeResult.ProjectedFactionCount != aiExchangeResult.EligibleFactionCount)
                        {
                            Debug.LogWarning(
                                $"[MatchBootstrap] aiResourceExchangeProjectionIncomplete " +
                                $"eligible={aiExchangeResult.EligibleFactionCount} " +
                                $"projected={aiExchangeResult.ProjectedFactionCount} " +
                                $"reason={aiExchangeResult.Reason}");
                        }
                    }

                    gameplayStartStep = GameplayStartStep.BindMainMenu;
                    break;

                case GameplayStartStep.BindMainMenu:
                    SetProgress(0.66f, "Binding match HUD");
                    bindMainMenu?.Invoke();
                    gameplayStartStep = GameplayStartStep.InitializeGameplayFeatures;
                    break;

                case GameplayStartStep.InitializeGameplayFeatures:
                    SetProgress(0.80f, "Starting gameplay systems");
                    initializeGameplayFeatures?.Invoke();
                    gameplayStartStep = GameplayStartStep.ValidateScenarioRecovery;
                    break;

                case GameplayStartStep.ValidateScenarioRecovery:
                    SetProgress(0.86f, "Validating scenario recovery");
                    if (materialsScenarioValidationStartedAt < 0d)
                        materialsScenarioValidationStartedAt = Time.realtimeSinceStartupAsDouble;
                    buildingRuntimeUpdate?.UpdateStartup(buildingRuntimeUpdateContext);
                    MaterialsScenarioRecoveryStartupSystemHelper materialsRecoveryStartup =
                        ResolveMaterialsScenarioRecoveryStartupSystemHelper(world);
                    MaterialsScenarioRecoveryValidationResult materialsRecoveryResult =
                        materialsRecoveryStartup != null
                            ? materialsRecoveryStartup.Validate(resourceExchangeConfig)
                            : default;
                    if (materialsRecoveryResult.Code == MaterialsScenarioRecoveryValidationCode.CatalogNotReady &&
                        Time.realtimeSinceStartupAsDouble - materialsScenarioValidationStartedAt < 15d)
                    {
                        SetProgress(0.84f, "Waiting for scenario catalog");
                        break;
                    }

                    if (!materialsRecoveryResult.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Match scenario Materials recovery validation failed: " +
                            $"code={materialsRecoveryResult.Code} " +
                            $"faction={materialsRecoveryResult.FactionId} " +
                            $"detail={materialsRecoveryStartup.LastInvalidConstructionId} " +
                            $"validatedFactions={materialsRecoveryResult.ValidatedFactionCount}.");
                    }

                    materialsScenarioValidationStartedAt = -1d;
                    gameplayStartStep = GameplayStartStep.FinalizeRuntimeState;
                    break;

                case GameplayStartStep.FinalizeRuntimeState:
                    SetProgress(0.92f, "Focusing camera");
                    gameplayStartPending = true;
                    resolveRuntimeCameraReferenceSystem?.Invoke(world)?.SetWorldCamera(
                        sceneView != null ? sceneView.WorldCamera : null);
                    runtimeGameplayStateSystem.ResetForGameplayStart();
                    gameplayStartStep = GameplayStartStep.Complete;
                    break;

                case GameplayStartStep.Complete:
                    SetProgress(0.98f, "Spawning world");
                    gameplayStartComplete = true;
                    break;
            }
        }

        private void SetProgress(float progress01, string status)
        {
            gameplayStartProgress01 = Mathf.Clamp01(progress01);
            gameplayStartStatus = string.IsNullOrEmpty(status) ? "Starting match" : status;
        }

        private RuntimeGridBootstrapStartupSystemHelper ResolveRuntimeGridBootstrapStartupSystemHelper(World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            runtimeGridBootstrapSystem ??= new RuntimeGridBootstrapStartupSystemHelper();
            return runtimeGridBootstrapSystem;
        }

        private MapSurfaceRuntimeBootstrapSceneSystemHelper ResolveMapSurfaceRuntimeBootstrapSystem(World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            mapSurfaceRuntimeBootstrapSystem ??= new MapSurfaceRuntimeBootstrapSceneSystemHelper(world);
            return mapSurfaceRuntimeBootstrapSystem;
        }

        private bool ResolveInitialFactionSpawnCell(byte factionId, out int2 spawnCell)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                spawnCell = default;
                return false;
            }

            return initialFactionSpawnCellSystem.TryGetConfiguredFactionSpawnCell(
                world.EntityManager,
                initialFactionSpawnCellFallbackEntries,
                factionId,
                out spawnCell);
        }

        private CustomGameStartupSystemHelper ResolveCustomGameStartupSystemHelper(World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            customGameStartupSystem ??= new CustomGameStartupSystemHelper(world.EntityManager);
            return customGameStartupSystem;
        }

        private ResourceExchangeStartupProjectionSystemHelper ResolveResourceExchangeStartupProjectionSystemHelper(
            World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            resourceExchangeStartupProjectionSystem ??=
                new ResourceExchangeStartupProjectionSystemHelper(world.EntityManager);
            return resourceExchangeStartupProjectionSystem;
        }

        private MaterialsScenarioRecoveryStartupSystemHelper ResolveMaterialsScenarioRecoveryStartupSystemHelper(
            World world)
        {
            if (world == null || !world.IsCreated)
                return null;

            materialsScenarioRecoveryStartupSystem ??=
                new MaterialsScenarioRecoveryStartupSystemHelper(world.EntityManager);
            return materialsScenarioRecoveryStartupSystem;
        }

        private AIStartupSystem ResolveAIStartupSystem(World world)
        {
            aiStartupSystem = new AIStartupSystem();
            return aiStartupSystem;
        }
    }
}
