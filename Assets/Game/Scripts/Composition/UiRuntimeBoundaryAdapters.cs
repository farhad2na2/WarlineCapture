using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingUiCommandAdapter : IBuildingUiCommand
{
    private readonly BuildingUiCommandSystem system;
    private readonly BuildingUiCommandSystem.Context context;

    public BuildingUiCommandAdapter(BuildingUiCommandSystem system, BuildingUiCommandSystem.Context context)
    {
        this.system = system;
        this.context = context;
    }

    public int CurrentDollars => system != null ? system.CurrentDollars(context) : 0;
    public bool HasPendingBuildingPlacement => system != null && system.HasPendingBuildingPlacement(context);
    public bool CanConfirmBuildingPlacement => system != null && system.CanConfirmBuildingPlacement(context);
    public string PlacementStatusText => system != null ? system.PlacementStatusText(context) : string.Empty;
    public int ActivePlacementCost => system != null ? system.ActivePlacementCost(context) : 0;
    public float ActivePlacementDurationSeconds => system != null ? system.ActivePlacementDurationSeconds(context) : 0f;

    public BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName)
    {
        requiredBuildingDisplayName = string.Empty;
        return system != null
            ? Map(system.GetCampRequestFailure(context, prefab, price, out requiredBuildingDisplayName))
            : BuildingUiCommandFailure.InvalidSelection;
    }

    public BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess)
    {
        requiredBuildingDisplayName = string.Empty;
        return system != null
            ? Map(system.TryRequestCampItem(context, prefab, price, out requiredBuildingDisplayName, focusProducerOnSuccess))
            : BuildingUiCommandFailure.InvalidSelection;
    }

    public bool CancelProduction(int buildingId, int pendingProductionIndex)
    {
        return system != null && system.CancelProduction(context, buildingId, pendingProductionIndex);
    }

    public bool ConfirmBuildingPlacement()
    {
        return system != null && system.ConfirmBuildingPlacement(context);
    }

    public void CancelBuildingPlacement()
    {
        system?.CancelBuildingPlacement(context);
    }

    public bool RotateBuildingPlacement()
    {
        return system != null && system.RotateBuildingPlacement(context);
    }

    private static BuildingUiCommandFailure Map(BuildingUiCommandSystem.CampRequestFailure failure)
    {
        return failure switch
        {
            BuildingUiCommandSystem.CampRequestFailure.None => BuildingUiCommandFailure.None,
            BuildingUiCommandSystem.CampRequestFailure.NotEnoughMoney => BuildingUiCommandFailure.NotEnoughMoney,
            BuildingUiCommandSystem.CampRequestFailure.MissingProducerBuilding => BuildingUiCommandFailure.MissingProducerBuilding,
            BuildingUiCommandSystem.CampRequestFailure.InvalidSelection => BuildingUiCommandFailure.InvalidSelection,
            _ => BuildingUiCommandFailure.InvalidSelection
        };
    }
}

internal sealed class BuildingUiQueryAdapter : IBuildingUiQuery
{
    private readonly BuildingUiQuerySystem system;
    private readonly BuildingUiQuerySystem.Context context;
    private readonly List<BuildingUiQuerySystem.PendingProductionUiEntry> scratch = new();

    public BuildingUiQueryAdapter(BuildingUiQuerySystem system, BuildingUiQuerySystem.Context context)
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
            BuildingUiQuerySystem.PendingProductionUiEntry entry = scratch[i];
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
    private readonly RuntimeGameplayStateSystem state;

    public MatchRuntimeStateAdapter(RuntimeGameplayStateSystem state)
    {
        this.state = state;
    }

    public bool PlayRequested
    {
        get => state != null && state.PlayRequested;
        set
        {
            if (state != null)
                state.PlayRequested = value;
        }
    }

    public bool SelectionModeActive
    {
        get => state != null && state.SelectionModeActive;
        set
        {
            if (state != null)
                state.SelectionModeActive = value;
        }
    }

    public bool BuildModeActive
    {
        get => state != null && state.BuildModeActive;
        set
        {
            if (state != null)
                state.BuildModeActive = value;
        }
    }

    public bool ZoomInHeld
    {
        get => state != null && state.ZoomInHeld;
        set
        {
            if (state != null)
                state.ZoomInHeld = value;
        }
    }

    public bool ZoomOutHeld
    {
        get => state != null && state.ZoomOutHeld;
        set
        {
            if (state != null)
                state.ZoomOutHeld = value;
        }
    }

    public bool SuppressNextWorldClick
    {
        get => state != null && state.SuppressNextWorldClick;
        set
        {
            if (state != null)
                state.SuppressNextWorldClick = value;
        }
    }
}

internal sealed class SelectionRectangleStateAdapter : ISelectionRectangleState
{
    private readonly IMatchRuntimeState runtimeState;
    private readonly RtsSelectionInputStateSystem inputStateSystem = new();

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
    private readonly SelectionUiCameraSystem cameraSystem;

    public MatchHudCameraControlAdapter(SelectionUiCameraSystem cameraSystem)
    {
        this.cameraSystem = cameraSystem;
    }

    public Camera WorldCamera => cameraSystem != null ? cameraSystem.WorldCamera : null;
    public bool IsCameraDragging => cameraSystem != null && cameraSystem.IsCameraDragging;

    public void MoveCameraGroundCenterTo(Vector3 worldPosition)
    {
        cameraSystem?.MoveCameraGroundCenterTo(worldPosition);
    }
}

internal sealed class QuickCustomGameConfigStore : IQuickCustomGameConfigStore
{
    public UiQuickCustomGameConfig Current => ToUiConfig(QuickGameConfig.FromAISettingsSnapshot(AISettingsRuntimeState.CurrentSnapshot));
    public UiQuickCustomGameConfig Defaults => ToUiConfig(QuickGameConfig.Defaults);

    public void Apply(UiQuickCustomGameConfig config)
    {
        AISettingsRuntimeState.ApplySnapshot(ToRuntimeConfig(config).ToAISettingsSnapshot());
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
    private readonly SceneLifecycleSystem sceneLifecycleSystem = new();
    private readonly MatchStartRequestSystem matchStartRequestSystem = new();

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
        bool loadQueued = sceneLifecycleSystem.QueueLoadMatch(entityManager);
        bool startQueued = matchStartRequestSystem.QueueStartAfterMatchLoaded(entityManager);
        if (!loadQueued || !startQueued)
            Debug.LogError($"[GameLaunch] Failed to queue Match start. loadQueued={(loadQueued ? 1 : 0)} startQueued={(startQueued ? 1 : 0)}");
    }
}

internal sealed class SelectionDiagnosticsSinkAdapter : ISelectionDiagnosticsSink
{
    public void LogMoveCommandTrace(string message)
    {
        SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(message);
    }
}
