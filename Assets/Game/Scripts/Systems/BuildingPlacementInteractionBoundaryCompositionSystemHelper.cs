using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BuildingPlacementInteractionBoundaryCompositionSystemHelper
{
    public delegate bool TryResolveSelectedBuildingFollowTargetDelegate(out Vector3 worldPosition, out float boundsRadius);

    public delegate bool TryResolveBaseBreachTargetDelegate(
        byte attackerFactionId,
        Entity finalTarget,
        int2 finalTargetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition,
        out string reason);

    public readonly struct Context
    {
        public readonly Func<bool> HasPendingBuildingPlacement;
        public readonly Func<bool> CanConfirmBuildingPlacement;
        public readonly Func<bool> HasSelectedBuilding;
        public readonly Func<bool> HasActiveBuilding;
        public readonly Func<bool> IsDraggingPlacementPreview;
        public readonly Func<string> GetPlacementStatusText;
        public readonly Func<string> GetSelectedBuildingLabel;
        public readonly Action BeginSoldierBasePlacement;
        public readonly Func<bool> ConfirmBuildingPlacement;
        public readonly Action CancelBuildingPlacement;
        public readonly Action CreateUnitFromSelectedBuilding;
        public readonly Action DeleteSelectedBuilding;
        public readonly Action<string> ClearSelectedBuilding;
        public readonly Action ExitBuildMode;
        public readonly Action<int, Entity, GameObject> HandleRuntimeBuildingEntityDestroyed;
        public readonly TryResolveBaseBreachTargetDelegate TryResolveBaseBreachTarget;
        public readonly TryResolveSelectedBuildingFollowTargetDelegate TryResolveSelectedBuildingFollowTarget;

        public Context(
            Func<bool> hasPendingBuildingPlacement,
            Func<bool> canConfirmBuildingPlacement,
            Func<bool> hasSelectedBuilding,
            Func<bool> hasActiveBuilding,
            Func<bool> isDraggingPlacementPreview,
            Func<string> getPlacementStatusText,
            Func<string> getSelectedBuildingLabel,
            Action beginSoldierBasePlacement,
            Func<bool> confirmBuildingPlacement,
            Action cancelBuildingPlacement,
            Action createUnitFromSelectedBuilding,
            Action deleteSelectedBuilding,
            Action<string> clearSelectedBuilding,
            Action exitBuildMode,
            Action<int, Entity, GameObject> handleRuntimeBuildingEntityDestroyed,
            TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget,
            TryResolveSelectedBuildingFollowTargetDelegate tryResolveSelectedBuildingFollowTarget = null)
        {
            HasPendingBuildingPlacement = hasPendingBuildingPlacement;
            CanConfirmBuildingPlacement = canConfirmBuildingPlacement;
            HasSelectedBuilding = hasSelectedBuilding;
            HasActiveBuilding = hasActiveBuilding;
            IsDraggingPlacementPreview = isDraggingPlacementPreview;
            GetPlacementStatusText = getPlacementStatusText;
            GetSelectedBuildingLabel = getSelectedBuildingLabel;
            BeginSoldierBasePlacement = beginSoldierBasePlacement;
            ConfirmBuildingPlacement = confirmBuildingPlacement;
            CancelBuildingPlacement = cancelBuildingPlacement;
            CreateUnitFromSelectedBuilding = createUnitFromSelectedBuilding;
            DeleteSelectedBuilding = deleteSelectedBuilding;
            ClearSelectedBuilding = clearSelectedBuilding;
            ExitBuildMode = exitBuildMode;
            HandleRuntimeBuildingEntityDestroyed = handleRuntimeBuildingEntityDestroyed;
            TryResolveBaseBreachTarget = tryResolveBaseBreachTarget;
            TryResolveSelectedBuildingFollowTarget = tryResolveSelectedBuildingFollowTarget;
        }
    }

    public bool HasPendingBuildingPlacement(Context context)
    {
        return context.HasPendingBuildingPlacement != null &&
               context.HasPendingBuildingPlacement();
    }

    public bool CanConfirmBuildingPlacement(Context context)
    {
        return context.CanConfirmBuildingPlacement != null &&
               context.CanConfirmBuildingPlacement();
    }

    public bool HasSelectedBuilding(Context context)
    {
        return context.HasSelectedBuilding != null &&
               context.HasSelectedBuilding();
    }

    public bool HasActiveBuilding(Context context)
    {
        return context.HasActiveBuilding != null &&
               context.HasActiveBuilding();
    }

    public bool IsDraggingPlacementPreview(Context context)
    {
        return context.IsDraggingPlacementPreview != null &&
               context.IsDraggingPlacementPreview();
    }

    public string PlacementStatusText(Context context)
    {
        return context.GetPlacementStatusText?.Invoke() ?? string.Empty;
    }

    public string SelectedBuildingLabel(Context context)
    {
        return context.GetSelectedBuildingLabel?.Invoke() ?? string.Empty;
    }

    public void BeginSoldierBasePlacement(Context context)
    {
        context.BeginSoldierBasePlacement?.Invoke();
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        return context.ConfirmBuildingPlacement != null &&
               context.ConfirmBuildingPlacement();
    }

    public void CancelBuildingPlacement(Context context)
    {
        context.CancelBuildingPlacement?.Invoke();
    }

    public void CreateUnitFromSelectedBuilding(Context context)
    {
        context.CreateUnitFromSelectedBuilding?.Invoke();
    }

    public void DeleteSelectedBuilding(Context context)
    {
        context.DeleteSelectedBuilding?.Invoke();
    }

    public void ClearSelectedBuilding(Context context, string reason)
    {
        context.ClearSelectedBuilding?.Invoke(reason);
    }

    public void ExitBuildMode(Context context)
    {
        context.ExitBuildMode?.Invoke();
    }

    public void HandleRuntimeBuildingEntityDestroyed(Context context, int buildingId, Entity blockerEntity, GameObject buildingObject)
    {
        context.HandleRuntimeBuildingEntityDestroyed?.Invoke(buildingId, blockerEntity, buildingObject);
    }

    public bool TryResolveBaseBreachTarget(
        Context context,
        byte attackerFactionId,
        Entity finalTarget,
        int2 finalTargetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition,
        out string reason)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        reason = string.Empty;
        return context.TryResolveBaseBreachTarget != null &&
               context.TryResolveBaseBreachTarget(
                   attackerFactionId,
                   finalTarget,
                   finalTargetCell,
                   attackerCell,
                   out breachTarget,
                   out breachCell,
                   out breachPosition,
                   out reason);
    }

    public bool TryResolveSelectedBuildingFollowTarget(Context context, out Vector3 worldPosition, out float boundsRadius)
    {
        worldPosition = Vector3.zero;
        boundsRadius = 0f;
        return context.TryResolveSelectedBuildingFollowTarget != null &&
               context.TryResolveSelectedBuildingFollowTarget(out worldPosition, out boundsRadius);
    }
}
