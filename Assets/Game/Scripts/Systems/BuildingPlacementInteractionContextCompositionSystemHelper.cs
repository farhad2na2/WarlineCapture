using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementInteractionContextCompositionSystemHelper
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
            public readonly Action ExitBuildMode;
            public readonly Action<int, Entity, GameObject> HandleRuntimeBuildingEntityDestroyed;
            public readonly BuildingPlacementInteractionCompositionSystemHelper.TryResolveBaseBreachTargetDelegate TryResolveBaseBreachTarget;
            public readonly BuildingPlacementInteractionCompositionSystemHelper.TryResolveSelectedBuildingFollowTargetDelegate TryResolveSelectedBuildingFollowTarget;

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
                Action exitBuildMode,
                Action<int, Entity, GameObject> handleRuntimeBuildingEntityDestroyed,
                BuildingPlacementInteractionCompositionSystemHelper.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget,
                BuildingPlacementInteractionCompositionSystemHelper.TryResolveSelectedBuildingFollowTargetDelegate tryResolveSelectedBuildingFollowTarget = null)
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

        public Source CreateSource(
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
            BuildingPlacementInteractionCompositionSystemHelper.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget,
            BuildingPlacementInteractionCompositionSystemHelper.TryResolveSelectedBuildingFollowTargetDelegate tryResolveSelectedBuildingFollowTarget = null)
        {
            return new Source(
                hasPendingBuildingPlacement,
                canConfirmBuildingPlacement,
                hasSelectedBuilding,
                hasActiveBuilding,
                isDraggingPlacementPreview,
                getPlacementStatusText,
                getSelectedBuildingLabel,
                beginSoldierBasePlacement,
                confirmBuildingPlacement,
                cancelBuildingPlacement,
                createUnitFromSelectedBuilding,
                deleteSelectedBuilding,
                clearSelectedBuilding,
                exitBuildMode,
                handleRuntimeBuildingEntityDestroyed,
                tryResolveBaseBreachTarget,
                tryResolveSelectedBuildingFollowTarget);
        }

        public BuildingPlacementInteractionCompositionSystemHelper.Context CreateContext(Source source)
        {
            return new BuildingPlacementInteractionCompositionSystemHelper.Context(
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
                source.ExitBuildMode,
                source.HandleRuntimeBuildingEntityDestroyed,
                source.TryResolveBaseBreachTarget,
                source.TryResolveSelectedBuildingFollowTarget);
        }
    }
}
