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
using Game.UI.Shell.Contracts.Ecs;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class BuildingUiQueryAdapter : IBuildingUiQuery, IBuildingProductionProgressQuery
    {
        private readonly BuildingUiQueryUiSystemHelper system;
        private readonly BuildingUiQueryUiSystemHelper.Context context;
        private readonly List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry> scratch = new();

        public bool HasProductionDeliveryInProgress
        {
            get
            {
                if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out var em)) return false;
                using var query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingProductionDeliveryReadModel>());
                if (query.CalculateEntityCount() != 1) return false;
                var delivery = query.GetSingleton<BuildingProductionDeliveryReadModel>();
                return delivery.ActiveCanonicalDeliveryCount > 0 || delivery.ActiveManagedDeliveryCount > 0;
            }
        }

        public bool HasProducedUnitsThisAttempt
        {
            get
            {
                if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out var em)) return false;
                using var attemptQuery = em.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionAttemptFactProjectionStateComponent>(),
                    ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
                using var producedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                    ComponentType.ReadOnly<BuildingProducedUnitReadModel>());
                if (attemptQuery.CalculateEntityCount() != 1 || producedQuery.CalculateEntityCount() != 1) return false;
                var attempt = attemptQuery.GetSingleton<CampaignMissionAttemptFactProjectionStateComponent>();
                var runtime = attemptQuery.GetSingleton<CampaignMissionRuntimeComponent>();
                if (attempt.Initialized == 0 || !attempt.SessionToken.Equals(runtime.SessionToken) ||
                    attempt.AttemptOrdinal != runtime.AttemptOrdinal) return false;
                return em.GetBuffer<BuildingProducedUnitReadModel>(producedQuery.GetSingletonEntity(), true).Length >
                    attempt.ProducedUnitReadModelBaselineCount;
            }
        }

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
        private readonly RtsSelectionInputStateCompositionSystemHelper inputStateSystem;

        public SelectionRectangleStateAdapter(IMatchRuntimeState runtimeState, EntityManager entityManager)
        {
            this.runtimeState = runtimeState;
            inputStateSystem = new RtsSelectionInputStateCompositionSystemHelper(entityManager);
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

    internal sealed class QuickCustomGameConfigStore : IQuickCustomGameConfigStore, ISkirmishMissionSizePreferenceStore
    {
        private SaveService saveService;
        private QuickGameSaveData saved;
        private QuickGameConfig currentConfig;
        private UiSkirmishLibrarySelection librarySelection;

        internal QuickCustomGameConfigStore(SaveService saveService = null)
        {
            this.saveService = saveService;
        }

        private void EnsureLoaded()
        {
            if (saved != null) return;
            // The store is owned by a MonoBehaviour field initializer. Unity's
            // persistentDataPath is only legal once the runtime asks for settings.
            saveService ??= SaveService.CreateDefault();
            saved = saveService.LoadQuickGame();
            currentConfig = saved.configuration;
        }

        public UiSkirmishMissionSize ReadMissionSize(string catalogId, string definitionId, int contentVersion)
        {
            EnsureLoaded();
            var catalog = SkirmishExpansionCatalogConfig.Load();
            if (catalog == null || !catalog.TryGetDefinition(catalogId, out var definition) ||
                definition.DefinitionId != definitionId || definition.ContentVersion != contentVersion ||
                saved.skirmishSizeCatalogId != catalogId || saved.skirmishSizeDefinitionId != definitionId ||
                saved.skirmishSizeContentVersion != contentVersion) return UiSkirmishMissionSize.Standard;
            var storedSize=saved.skirmishSizeId=="War"?Game.Skirmish.Contracts.SkirmishSizeId.War:
                saved.skirmishSizeId=="LargeWar"?Game.Skirmish.Contracts.SkirmishSizeId.LargeWar:Game.Skirmish.Contracts.SkirmishSizeId.Standard;
            if(!SkirmishBattleCatalogConfig.IsMissionSizeSelectionAvailable(catalogId,definitionId,contentVersion,storedSize))
                return UiSkirmishMissionSize.Standard;
            return saved.skirmishSizeId switch {
                "Standard" => UiSkirmishMissionSize.Standard, "War" => UiSkirmishMissionSize.War,
                "LargeWar" => UiSkirmishMissionSize.LargeWar, _ => UiSkirmishMissionSize.Standard };
        }

        private static bool ValidMissionSize(UiSkirmishMissionSize value) =>
            value == UiSkirmishMissionSize.Standard || value == UiSkirmishMissionSize.War ||
            value == UiSkirmishMissionSize.LargeWar;

        public UiQuickCustomGameConfig Current { get { EnsureLoaded(); var value=ToUiConfig(currentConfig); value.SkirmishSelection=librarySelection; return value; } }
        public UiQuickCustomGameConfig Defaults => ToUiConfig(QuickGameConfig.Defaults);
        internal AISettingsSnapshot CurrentSnapshot { get { EnsureLoaded(); return currentConfig.ToAISettingsSnapshot(); } }
        internal QuickGameConfig CurrentConfig { get { EnsureLoaded(); return currentConfig; } }
        internal UiSkirmishLibrarySelection CurrentLibrarySelection { get { EnsureLoaded(); return librarySelection; } }

        public void Apply(UiQuickCustomGameConfig config)
        {
            EnsureLoaded();
            currentConfig = ToRuntimeConfig(config).NormalizeForBaseAssault();
            librarySelection = config.SkirmishSelection;
            // Results are written by the match after this setup store was loaded.
            // Refresh the document so changing setup cannot restore a stale result.
            saved = saveService.LoadQuickGame();
            saved.configuration = currentConfig;
            var selected = librarySelection;
            if (selected.Kind == UiSkirmishLibraryLaunchKind.ExpandedDefinition)
            {
                var catalog = SkirmishExpansionCatalogConfig.Load();
                var size = selected.MissionSize == UiSkirmishMissionSize.Unspecified ? UiSkirmishMissionSize.Standard : selected.MissionSize;
                if (!ValidMissionSize(size) || catalog == null || !catalog.TryGetDefinition(selected.CatalogId, out var definition) ||
                    definition.DefinitionId != selected.DefinitionId || definition.ContentVersion != selected.ContentVersion)
                    throw new System.InvalidOperationException("Invalid expanded mission-size preference identity.");
                saved.skirmishSizeCatalogId = selected.CatalogId; saved.skirmishSizeDefinitionId = selected.DefinitionId;
                saved.skirmishSizeContentVersion = selected.ContentVersion; saved.skirmishSizeId = size.ToString();
            }
            saveService.SaveQuickGame(saved);
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
                MapSeed = config.MapSeed,
                ScenarioIndex = config.ScenarioIndex
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
                MapSeed = config.MapSeed,
                ScenarioIndex = config.ScenarioIndex
            };
        }
    }

    internal sealed class MatchLaunchCommand : IMatchLaunchCommand
    {
        private readonly QuickCustomGameConfigStore configStore;
        public MatchLaunchCommand(QuickCustomGameConfigStore configStore)
        {
            this.configStore = configStore;
        }

        public void LaunchMatch(Component source)
        {
            _ = source;
            QueueMatchRoute();
        }

        private bool QueueMatchRoute()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogError("[GameLaunch] Cannot queue Match route because the default ECS world is missing.");
                return false;
            }

            return QueueMatchRoute(world.EntityManager);
        }

        internal bool QueueMatchRoute(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                Debug.LogError("[GameLaunch] Cannot queue Match route because the UI shell boundary is missing.");
                return false;
            }

            Entity boundary;
            try
            {
                boundary = query.GetSingletonEntity();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[GameLaunch] Cannot queue Match route because the UI shell boundary is ambiguous. error={exception.Message}");
                return false;
            }

            UiShellStateComponent shellState = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            bool alreadyQueued = shellState.ActiveRoute == UIRoute.Match;
            for (int index = 0; index < routeRequests.Length && !alreadyQueued; index++)
                alreadyQueued = routeRequests[index].Intent == UiShellRouteIntent.EnterMatch;

            if (!alreadyQueued)
            {
                if (!SkirmishLaunchProjection.TryQueueSelected(entityManager, configStore.CurrentConfig, configStore.CurrentLibrarySelection))
                    return false;
                routeRequests = entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
                routeRequests.Add(new UiShellRouteRequestComponent
                {
                    Intent = UiShellRouteIntent.EnterMatch,
                    Route = UIRoute.Match,
                    PushHistory = 0
                });
            }

            Debug.Log($"[GameLaunch] submitted authoritative Match route request. alreadyQueued={(alreadyQueued ? 1 : 0)}");
            return true;
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
