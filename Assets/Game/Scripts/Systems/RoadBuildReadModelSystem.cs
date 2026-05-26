using System;

public sealed class RoadBuildReadModelSystem
{
    private Func<bool> _isRoadBuildModeActive;
    private Func<bool> _isDraggingBuildInteraction;
    private Func<bool> _hasPendingBuildingPlacement;
    private Func<bool> _hasSelectedBuilding;
    private Func<bool> _canConfirmBuildingPlacement;

    public bool IsRoadBuildModeActive => _isRoadBuildModeActive?.Invoke() == true;
    public bool IsDraggingBuildInteraction => _isDraggingBuildInteraction?.Invoke() == true;
    public bool HasPendingBuildingPlacement => _hasPendingBuildingPlacement?.Invoke() == true;
    public bool HasSelectedBuilding => _hasSelectedBuilding?.Invoke() == true;
    public bool CanConfirmBuildingPlacement => _canConfirmBuildingPlacement?.Invoke() == true;

    public void Configure(
        Func<bool> isRoadBuildModeActive,
        Func<bool> isDraggingBuildInteraction,
        Func<bool> hasPendingBuildingPlacement,
        Func<bool> hasSelectedBuilding,
        Func<bool> canConfirmBuildingPlacement)
    {
        _isRoadBuildModeActive = isRoadBuildModeActive;
        _isDraggingBuildInteraction = isDraggingBuildInteraction;
        _hasPendingBuildingPlacement = hasPendingBuildingPlacement;
        _hasSelectedBuilding = hasSelectedBuilding;
        _canConfirmBuildingPlacement = canConfirmBuildingPlacement;
    }

    public void Clear()
    {
        _isRoadBuildModeActive = null;
        _isDraggingBuildInteraction = null;
        _hasPendingBuildingPlacement = null;
        _hasSelectedBuilding = null;
        _canConfirmBuildingPlacement = null;
    }
}
