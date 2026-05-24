using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingPlacementInteractionContextSystem
{
    public readonly struct Source
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
        public readonly Action<int, Entity, GameObject> HandleRuntimeBuildingEntityDestroyed;
        public readonly BuildingPlacementInteractionSystem.TryResolveBaseBreachTargetDelegate TryResolveBaseBreachTarget;

        public Source(
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
            Action<int, Entity, GameObject> handleRuntimeBuildingEntityDestroyed,
            BuildingPlacementInteractionSystem.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget)
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
            HandleRuntimeBuildingEntityDestroyed = handleRuntimeBuildingEntityDestroyed;
            TryResolveBaseBreachTarget = tryResolveBaseBreachTarget;
        }
    }

    public BuildingPlacementInteractionSystem.Context CreateContext(Source source)
    {
        return new BuildingPlacementInteractionSystem.Context(
            source.HasPendingBuildingPlacement,
            source.CanConfirmBuildingPlacement,
            source.HasSelectedBuilding,
            source.HasActiveBuilding,
            source.IsDraggingPlacementPreview,
            source.GetPlacementStatusText,
            source.GetSelectedBuildingLabel,
            source.BeginSoldierBasePlacement,
            source.ConfirmBuildingPlacement,
            source.CancelBuildingPlacement,
            source.CreateUnitFromSelectedBuilding,
            source.DeleteSelectedBuilding,
            source.ClearSelectedBuilding,
            source.HandleRuntimeBuildingEntityDestroyed,
            source.TryResolveBaseBreachTarget);
    }
}
