using System.Collections.Generic;
using UnityEngine;

public enum BuildingUiCommandFailure
{
    None = 0,
    NotEnoughMoney = 1,
    MissingProducerBuilding = 2,
    InvalidSelection = 3
}

public readonly struct BuildingPendingProductionUiEntry
{
    public readonly int BuildingId;
    public readonly int PendingProductionIndex;
    public readonly GameObject Prefab;
    public readonly float RemainingSeconds;
    public readonly float DurationSeconds;
    public readonly float Progress01;
    public readonly float StartedAt;
    public readonly float ReadyAt;
    public readonly string ProducerDisplayName;

    public BuildingPendingProductionUiEntry(
        int buildingId,
        GameObject prefab,
        float remainingSeconds,
        float durationSeconds,
        float progress01,
        float startedAt,
        float readyAt,
        string producerDisplayName = "")
        : this(buildingId, -1, prefab, remainingSeconds, durationSeconds, progress01, startedAt, readyAt, producerDisplayName)
    {
    }

    public BuildingPendingProductionUiEntry(
        int buildingId,
        int pendingProductionIndex,
        GameObject prefab,
        float remainingSeconds,
        float durationSeconds,
        float progress01,
        float startedAt,
        float readyAt,
        string producerDisplayName)
    {
        BuildingId = buildingId;
        PendingProductionIndex = pendingProductionIndex;
        Prefab = prefab;
        RemainingSeconds = remainingSeconds;
        DurationSeconds = durationSeconds;
        Progress01 = progress01;
        StartedAt = startedAt;
        ReadyAt = readyAt;
        ProducerDisplayName = producerDisplayName ?? string.Empty;
    }
}

public interface IBuildingUiCommand
{
    int CurrentDollars { get; }
    bool HasPendingBuildingPlacement { get; }
    bool CanConfirmBuildingPlacement { get; }
    string PlacementStatusText { get; }
    int ActivePlacementCost { get; }
    float ActivePlacementDurationSeconds { get; }

    BuildingUiCommandFailure GetCampRequestFailure(GameObject prefab, int price, out string requiredBuildingDisplayName);
    BuildingUiCommandFailure TryRequestCampItem(GameObject prefab, int price, out string requiredBuildingDisplayName, bool focusProducerOnSuccess);
    bool CancelProduction(int buildingId, int pendingProductionIndex);
    bool ConfirmBuildingPlacement();
    void CancelBuildingPlacement();
    bool RotateBuildingPlacement();
}

public interface IBuildingUiQuery
{
    void GetFriendlyPendingProductionUiEntries(List<BuildingPendingProductionUiEntry> entries);
}

public interface IMatchRuntimeState
{
    bool PlayRequested { get; set; }
    bool SelectionModeActive { get; set; }
    bool BuildModeActive { get; set; }
    bool ZoomInHeld { get; set; }
    bool ZoomOutHeld { get; set; }
    bool SuppressNextWorldClick { get; set; }
}

public readonly struct SelectionRectangleStateModel
{
    public readonly bool CanDraw;
    public readonly Rect ScreenRect;

    public SelectionRectangleStateModel(bool canDraw, Rect screenRect)
    {
        CanDraw = canDraw;
        ScreenRect = screenRect;
    }
}

public interface ISelectionRectangleState
{
    bool TryRead(out SelectionRectangleStateModel state);
}

public interface IMatchHudCameraControl
{
    Camera WorldCamera { get; }
    bool IsCameraDragging { get; }
    void MoveCameraGroundCenterTo(Vector3 worldPosition);
}

public interface IUiCatalogPrefabSource
{
    IReadOnlyList<GameObject> UnitSpawnPrefabs { get; }
    IReadOnlyList<GameObject> BuildingSpawnPrefabs { get; }
}

public enum UiBoardCommandModeDirection : byte
{
    None = 0,
    PassengerToTransport = 1,
    TransportToPassenger = 2
}

public enum UiQuickGameEnemyType : byte
{
    Balanced = 0,
    Military = 1,
    Defensive = 2,
    Air = 3,
    Swarm = 4,
    Random = 5
}

public enum UiQuickGameWinCondition : byte
{
    DestroyAllEnemies = 0,
    SurviveDuration = 1,
    Sandbox = 2
}

public enum UiQuickGameStartingResources : byte
{
    Standard = 0,
    Low = 1,
    High = 2
}

public enum UiAiDifficultySetting : byte
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Brutal = 3
}

public enum UiAiStartingMoneySetting : byte
{
    Low = 0,
    Normal = 1,
    High = 2
}

public enum UiAiSpeedSetting : byte
{
    Slow = 0,
    Normal = 1,
    Fast = 2
}

public enum UiAiAttackGroupSizeSetting : byte
{
    Small = 0,
    Normal = 1,
    Large = 2
}

public enum UiAiAttackFrequencySetting : byte
{
    Rare = 0,
    Normal = 1,
    Frequent = 2
}

public enum UiAiAggressionSetting : byte
{
    Defensive = 0,
    Balanced = 1,
    Aggressive = 2
}

public enum UiAiExpansionSetting : byte
{
    Off = 0,
    Slow = 1,
    Normal = 2,
    Fast = 3
}

public enum UiAiTargetPriority : byte
{
    Balanced = 0,
    Units = 1,
    Economy = 2,
    Production = 3
}

public struct UiQuickCustomGameConfig
{
    public UiQuickGameEnemyType EnemyType;
    public int EnemyCount;
    public UiAiDifficultySetting Difficulty;
    public UiAiStartingMoneySetting StartingMoney;
    public float IncomeMultiplier;
    public UiAiSpeedSetting BuildSpeed;
    public UiAiSpeedSetting UnitProductionSpeed;
    public UiAiAttackGroupSizeSetting AttackGroupSize;
    public UiAiAttackFrequencySetting AttackFrequency;
    public UiAiAggressionSetting Aggression;
    public UiAiExpansionSetting Expansion;
    public UiAiTargetPriority TargetPriority;
    public bool PlayerAutoAIEnabled;
    public UiQuickGameWinCondition WinCondition;
    public bool FogOfWar;
    public bool IntelReveal;
    public UiQuickGameStartingResources StartingResources;
    public int MapSeed;

    public static UiQuickCustomGameConfig Defaults => new()
    {
        EnemyType = UiQuickGameEnemyType.Balanced,
        EnemyCount = 1,
        Difficulty = UiAiDifficultySetting.Normal,
        StartingMoney = UiAiStartingMoneySetting.Normal,
        IncomeMultiplier = 1f,
        BuildSpeed = UiAiSpeedSetting.Normal,
        UnitProductionSpeed = UiAiSpeedSetting.Normal,
        AttackGroupSize = UiAiAttackGroupSizeSetting.Normal,
        AttackFrequency = UiAiAttackFrequencySetting.Normal,
        Aggression = UiAiAggressionSetting.Balanced,
        Expansion = UiAiExpansionSetting.Normal,
        TargetPriority = UiAiTargetPriority.Balanced,
        PlayerAutoAIEnabled = false,
        WinCondition = UiQuickGameWinCondition.DestroyAllEnemies,
        FogOfWar = false,
        IntelReveal = true,
        StartingResources = UiQuickGameStartingResources.Standard,
        MapSeed = 104729
    };
}

public interface IQuickCustomGameConfigStore
{
    UiQuickCustomGameConfig Current { get; }
    UiQuickCustomGameConfig Defaults { get; }
    void Apply(UiQuickCustomGameConfig config);
}

public interface IMatchLaunchCommand
{
    void LaunchMatch(Component source);
}

public interface ISelectionDiagnosticsSink
{
    void LogMoveCommandTrace(string message);
}
