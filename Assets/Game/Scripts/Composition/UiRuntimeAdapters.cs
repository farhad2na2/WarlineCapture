using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class BuildingUiCommandAdapter : IBuildingUiCommand
    {
        private readonly BuildingUiCommandSystemHelper boundary;
        private readonly BuildingUiCommandSystemHelper.Context context;

        public BuildingUiCommandAdapter(BuildingUiCommandSystemHelper boundary, BuildingUiCommandSystemHelper.Context context)
        {
            this.boundary = boundary;
            this.context = context;
        }

        public int CurrentDollars => boundary != null ? boundary.CurrentDollars(context) : 0;
        public bool HasPendingBuildingPlacement => boundary != null && boundary.HasPendingBuildingPlacement(context);
        public bool CanConfirmBuildingPlacement => boundary != null && boundary.CanConfirmBuildingPlacement(context);
        public string PlacementStatusText => boundary != null ? boundary.PlacementStatusText(context) : string.Empty;
        public int ActivePlacementCost => boundary != null ? boundary.ActivePlacementCost(context) : 0;
        public float ActivePlacementDurationSeconds => boundary != null ? boundary.ActivePlacementDurationSeconds(context) : 0f;
        public int MaxQueuedUnitProductions => boundary != null ? boundary.MaxQueuedUnitProductions(context) : 25;

        public BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int materialsCost, out string requiredBuildingDisplayName)
        {
            requiredBuildingDisplayName = string.Empty;
            return boundary != null
                ? Map(boundary.GetCampRequestFailure(context, prefab, materialsCost, out requiredBuildingDisplayName))
                : BuildingUiCommandFailure.InvalidSelection;
        }

        public BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int materialsCost, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
        {
            requiredBuildingDisplayName = string.Empty;
            return boundary != null
                ? Map(boundary.TryRequestCampItem(context, prefab, materialsCost, out requiredBuildingDisplayName, focusProducerOnSuccess))
                : BuildingUiCommandFailure.InvalidSelection;
        }

        public bool CancelProduction(int buildingId, int pendingProductionIndex)
        {
            return boundary != null && boundary.CancelProduction(context, buildingId, pendingProductionIndex);
        }

        public bool ConfirmBuildingPlacement()
        {
            return boundary != null && boundary.ConfirmBuildingPlacement(context);
        }

        public void CancelBuildingPlacement()
        {
            boundary?.CancelBuildingPlacement(context);
        }

        public bool RotateBuildingPlacement()
        {
            return boundary != null && boundary.RotateBuildingPlacement(context);
        }

        private static BuildingUiCommandFailure Map(BuildingUiCommandSystemHelper.CampRequestFailure failure)
        {
            return failure switch
            {
                BuildingUiCommandSystemHelper.CampRequestFailure.None => BuildingUiCommandFailure.None,
                BuildingUiCommandSystemHelper.CampRequestFailure.NotEnoughMoney => BuildingUiCommandFailure.NotEnoughMoney,
                BuildingUiCommandSystemHelper.CampRequestFailure.MissingProducerBuilding => BuildingUiCommandFailure.MissingProducerBuilding,
                BuildingUiCommandSystemHelper.CampRequestFailure.InvalidSelection => BuildingUiCommandFailure.InvalidSelection,
                BuildingUiCommandSystemHelper.CampRequestFailure.ProductionQueueFull => BuildingUiCommandFailure.ProductionQueueFull,
                BuildingUiCommandSystemHelper.CampRequestFailure.GlobalProductionQueueFull => BuildingUiCommandFailure.GlobalProductionQueueFull,
                BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCredits => BuildingUiCommandFailure.InsufficientCredits,
                BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientMaterials => BuildingUiCommandFailure.InsufficientMaterials,
                BuildingUiCommandSystemHelper.CampRequestFailure.InsufficientCreditsAndMaterials => BuildingUiCommandFailure.InsufficientCreditsAndMaterials,
                _ => BuildingUiCommandFailure.InvalidSelection
            };
        }
    }

    internal sealed class BuildingUiQueryAdapter : IBuildingUiQuery
    {
        private readonly BuildingUiQueryUiSystemHelper system;
        private readonly BuildingUiQueryUiSystemHelper.Context context;
        private readonly List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> scratch = new();

        public BuildingUiQueryAdapter(BuildingUiQueryUiSystemHelper system, BuildingUiQueryUiSystemHelper.Context context)
        {
            this.system = system;
            this.context = context;
        }

        public void GetFriendlyPendingProductionUiEntries(List<BuildingPendingProductionUiEntry> entries)
        {
            if (entries == null)
                return;

            entries.Clear();
            if (system == null)
                return;

            scratch.Clear();
            system.GetFriendlyPendingProductionUiEntries(context, scratch);
            for (int i = 0; i < scratch.Count; i++)
            {
                BuildingUiQueryUiSystemHelper.PendingProductionUiEntry entry = scratch[i];
                entries.Add(new BuildingPendingProductionUiEntry(
                    entry.BuildingId,
                    entry.PendingProductionIndex,
                    entry.Prefab,
                    entry.RemainingSeconds,
                    entry.DurationSeconds,
                    entry.Progress01,
                    entry.StartedAt,
                    entry.ReadyAt,
                    entry.ProducerDisplayName));
            }
        }
    }

    internal sealed class MatchRuntimeStateAdapter : IMatchRuntimeState
    {
        private RuntimeGameplayStateSystem state;

        public MatchRuntimeStateAdapter(RuntimeGameplayStateSystem state)
        {
            this.state = state;
        }

        public bool PlayRequested
        {
            get => state.PlayRequested;
            set => state.PlayRequested = value;
        }

        public bool SimulationActive
        {
            get => state.SimulationActive;
            set => state.SimulationActive = value;
        }

        public bool SelectionModeActive
        {
            get => state.SelectionModeActive;
            set => state.SelectionModeActive = value;
        }

        public bool BuildModeActive
        {
            get => state.BuildModeActive;
            set => state.BuildModeActive = value;
        }

        public bool ZoomInHeld
        {
            get => state.ZoomInHeld;
            set => state.ZoomInHeld = value;
        }

        public bool ZoomOutHeld
        {
            get => state.ZoomOutHeld;
            set => state.ZoomOutHeld = value;
        }

        public bool SuppressNextWorldClick
        {
            get => state.SuppressNextWorldClick;
            set => state.SuppressNextWorldClick = value;
        }
    }

    internal sealed class SelectionRectangleStateAdapter : ISelectionRectangleState
    {
        private readonly IMatchRuntimeState runtimeState;
        private readonly RtsSelectionInputStateCompositionSystemHelper inputStateSystem = new();

        public SelectionRectangleStateAdapter(IMatchRuntimeState runtimeState)
        {
            this.runtimeState = runtimeState;
        }

        public bool TryRead(out SelectionRectangleStateModel state)
        {
            state = default;
            if (runtimeState == null || !runtimeState.PlayRequested)
                return false;

            if (!inputStateSystem.TryRead(out _, out RtsSelectionInputStateComponent inputState))
                return false;

            bool canDrawSelectionRect = runtimeState.SelectionModeActive ||
                                        (TacticalCommandMode)inputState.ActiveCommandMode == TacticalCommandMode.Board;
            if (!canDrawSelectionRect || inputState.HasLiveSelectionRect == 0)
                return false;

            state = new SelectionRectangleStateModel(true, ToGuiRect(inputState.LastLiveSelectionRect));
            return true;
        }

        private static Rect ToGuiRect(float4 screenRect)
        {
            var rect = Rect.MinMaxRect(screenRect.x, screenRect.y, screenRect.z, screenRect.w);
            rect.y = Screen.height - rect.yMax;
            return rect;
        }
    }

    internal sealed class MatchHudCameraControlAdapter : IMatchHudCameraControl
    {
        private readonly SelectionUiCameraSystemHelper cameraSystem;

        public MatchHudCameraControlAdapter(SelectionUiCameraSystemHelper cameraSystem)
        {
            this.cameraSystem = cameraSystem;
        }

        public Camera WorldCamera => cameraSystem != null ? cameraSystem.WorldCamera : null;
        public bool IsCameraDragging => cameraSystem != null && cameraSystem.IsCameraDragging;

        public void MoveCameraGroundCenterTo(Vector3 worldPosition)
        {
            cameraSystem?.MoveCameraGroundCenterTo(worldPosition);
        }

        public void UpdateZoomTransition()
        {
            cameraSystem?.UpdateZoomTransition();
        }

        public MatchHudZoomControlState ReadZoomControlState()
        {
            return cameraSystem != null ? cameraSystem.ReadZoomControlState() : MatchHudZoomControlState.Disabled;
        }

        public bool RequestZoomInLevel()
        {
            return cameraSystem != null && cameraSystem.RequestZoomInLevel();
        }

        public bool RequestZoomOutLevel()
        {
            return cameraSystem != null && cameraSystem.RequestZoomOutLevel();
        }
    }

    internal sealed class QuickCustomGameConfigStore : IQuickCustomGameConfigStore
    {
        private AISettingsSnapshot currentSnapshot = AISettingsSnapshot.Defaults;

        public UiQuickCustomGameConfig Current => ToUiConfig(QuickGameConfig.FromAISettingsSnapshot(currentSnapshot));
        public UiQuickCustomGameConfig Defaults => ToUiConfig(QuickGameConfig.Defaults);
        internal AISettingsSnapshot CurrentSnapshot => currentSnapshot;

        public void Apply(UiQuickCustomGameConfig config)
        {
            currentSnapshot = ToRuntimeConfig(config).ToAISettingsSnapshot();
        }

        private static UiQuickCustomGameConfig ToUiConfig(QuickGameConfig config)
        {
            return new UiQuickCustomGameConfig
            {
                EnemyType = (UiQuickGameEnemyType)config.EnemyType,
                EnemyCount = config.EnemyCount,
                Difficulty = (UiAiDifficultySetting)config.Difficulty,
                StartingMoney = (UiAiStartingMoneySetting)config.StartingMoney,
                IncomeMultiplier = config.IncomeMultiplier,
                BuildSpeed = (UiAiSpeedSetting)config.BuildSpeed,
                UnitProductionSpeed = (UiAiSpeedSetting)config.UnitProductionSpeed,
                AttackGroupSize = (UiAiAttackGroupSizeSetting)config.AttackGroupSize,
                AttackFrequency = (UiAiAttackFrequencySetting)config.AttackFrequency,
                Aggression = (UiAiAggressionSetting)config.Aggression,
                Expansion = (UiAiExpansionSetting)config.Expansion,
                TargetPriority = (UiAiTargetPriority)config.TargetPriority,
                PlayerAutoAIEnabled = config.PlayerAutoAIEnabled,
                WinCondition = (UiQuickGameWinCondition)config.WinCondition,
                FogOfWar = config.FogOfWar,
                IntelReveal = config.IntelReveal,
                StartingResources = (UiQuickGameStartingResources)config.StartingResources,
                MapSeed = config.MapSeed
            };
        }

        private static QuickGameConfig ToRuntimeConfig(UiQuickCustomGameConfig config)
        {
            return new QuickGameConfig
            {
                EnemyType = (QuickGameEnemyType)config.EnemyType,
                EnemyCount = config.EnemyCount,
                Difficulty = (AIDifficultySetting)config.Difficulty,
                StartingMoney = (AIStartingMoneySetting)config.StartingMoney,
                IncomeMultiplier = config.IncomeMultiplier,
                BuildSpeed = (AISpeedSetting)config.BuildSpeed,
                UnitProductionSpeed = (AISpeedSetting)config.UnitProductionSpeed,
                AttackGroupSize = (AIAttackGroupSizeSetting)config.AttackGroupSize,
                AttackFrequency = (AIAttackFrequencySetting)config.AttackFrequency,
                Aggression = (AIAggressionSetting)config.Aggression,
                Expansion = (AIExpansionSetting)config.Expansion,
                TargetPriority = (AITargetPriority)config.TargetPriority,
                PlayerAutoAIEnabled = config.PlayerAutoAIEnabled,
                WinCondition = (QuickGameWinCondition)config.WinCondition,
                FogOfWar = config.FogOfWar,
                IntelReveal = config.IntelReveal,
                StartingResources = (QuickGameStartingResources)config.StartingResources,
                MapSeed = config.MapSeed
            };
        }
    }

    internal sealed class MatchLaunchCommand : IMatchLaunchCommand
    {
        private readonly SceneLifecycleSceneSystemHelper sceneLifecycleSceneSystemHelper = new();
        private readonly MatchStartRequestStartupSystemHelper matchStartRequestSystem = new();
        private readonly QuickCustomGameConfigStore configStore;

        public MatchLaunchCommand(QuickCustomGameConfigStore configStore)
        {
            this.configStore = configStore;
        }

        public void LaunchMatch(Component source)
        {
            QueueMatchLoadAndStart();

            UIRouterView router = source != null ? source.GetComponentInParent<UIRouterView>() : null;
            if (router != null)
                router.gameObject.SetActive(false);
        }

        private void QueueMatchLoadAndStart()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[GameLaunch] Cannot queue Match start because the default ECS world is missing.");
                return;
            }

            EntityManager entityManager = world.EntityManager;
            bool loadQueued = sceneLifecycleSceneSystemHelper.QueueLoadMatch(entityManager);
            bool startQueued = matchStartRequestSystem.QueueStartAfterMatchLoaded(entityManager);
            if (startQueued && configStore != null)
                MatchAISettingsStartupProjection.Project(entityManager, configStore.CurrentSnapshot);
            if (!loadQueued || !startQueued)
                Debug.LogError($"[GameLaunch] Failed to queue Match start. loadQueued={(loadQueued ? 1 : 0)} startQueued={(startQueued ? 1 : 0)}");
        }
    }

    internal sealed class SelectionDiagnosticsSinkAdapter : ISelectionDiagnosticsSink
    {
        public void LogMoveCommandTrace(string message)
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(message);
        }
    }
}
