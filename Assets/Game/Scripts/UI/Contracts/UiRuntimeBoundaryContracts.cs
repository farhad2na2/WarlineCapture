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

public interface IQuickCustomGameConfigStore
{
    QuickGameConfig Current { get; }
    QuickGameConfig Defaults { get; }
    void Apply(QuickGameConfig config);
}

public interface IMatchLaunchCommand
{
    void LaunchMatch(Component source);
}

public interface ISelectionDiagnosticsSink
{
    void LogMoveCommandTrace(string message);
}
