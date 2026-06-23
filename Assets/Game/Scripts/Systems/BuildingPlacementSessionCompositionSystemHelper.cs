using System;
using UnityEngine;

internal sealed class BuildingPlacementSessionCompositionSystemHelper
{
    public struct Context
    {
        public RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly BuildingPlacementLifecycleCompositionSystemHelper LifecycleSystem;
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewPresentationSystemHelper PreviewSystem;
        public readonly Func<BuildingPlacementLifecycleCompositionSystemHelper.CancelContext> CreateCancelContext;
        public readonly Func<BuildingPlacementLifecycleCompositionSystemHelper.BeginContext> CreateBeginContext;
        public readonly Func<BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext> CreateConfirmContext;
        public readonly Func<BuildingPlacementLifecycleCompositionSystemHelper.RotateContext> CreateRotateContext;
        public readonly Action RecordBuildingBuilt;
        public readonly Action NotifyStaticMinimapChanged;
        public readonly Action<string> ClearSelectedBuilding;
        public readonly Action ClearCommandMode;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            BuildingPlacementLifecycleCompositionSystemHelper lifecycleSystem,
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewPresentationSystemHelper previewSystem,
            Func<BuildingPlacementLifecycleCompositionSystemHelper.CancelContext> createCancelContext,
            Func<BuildingPlacementLifecycleCompositionSystemHelper.BeginContext> createBeginContext,
            Func<BuildingPlacementLifecycleCompositionSystemHelper.ConfirmContext> createConfirmContext,
            Func<BuildingPlacementLifecycleCompositionSystemHelper.RotateContext> createRotateContext,
            Action recordBuildingBuilt,
            Action notifyStaticMinimapChanged,
            Action<string> clearSelectedBuilding,
            Action clearCommandMode)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            LifecycleSystem = lifecycleSystem;
            InputSystem = inputSystem;
            PreviewSystem = previewSystem;
            CreateCancelContext = createCancelContext;
            CreateBeginContext = createBeginContext;
            CreateConfirmContext = createConfirmContext;
            CreateRotateContext = createRotateContext;
            RecordBuildingBuilt = recordBuildingBuilt;
            NotifyStaticMinimapChanged = notifyStaticMinimapChanged;
            ClearSelectedBuilding = clearSelectedBuilding;
            ClearCommandMode = clearCommandMode;
        }
    }

    private bool _preserveBuildingSelectionOnNextExitBuildMode;

    public void SetActivePlacementCost(Context context, int cost)
    {
        context.LifecycleSystem?.SetActivePlacementCost(cost);
    }

    public void BeginPlacement(Context context, BuildingDefinition definition)
    {
        context.LifecycleSystem?.Begin(definition, context.CreateBeginContext());
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        return ConfirmBuildingPlacement(context, out _);
    }

    public bool ConfirmBuildingPlacement(
        Context context,
        out BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason failureReason)
    {
        failureReason = BuildingPlacementLifecycleCompositionSystemHelper.ConfirmFailureReason.MissingActivePlacement;
        if (context.LifecycleSystem == null)
            return false;

        if (!context.LifecycleSystem.Confirm(context.CreateConfirmContext(), out failureReason))
            return false;

        context.RecordBuildingBuilt?.Invoke();
        context.NotifyStaticMinimapChanged?.Invoke();
        _preserveBuildingSelectionOnNextExitBuildMode = true;
        ExitBuildMode(context, clearBuildingSelection: false);
        return true;
    }

    public bool RotateBuildingPlacement(Context context)
    {
        return context.LifecycleSystem != null &&
               context.LifecycleSystem.Rotate(
                   context.CreateRotateContext != null
                       ? context.CreateRotateContext()
                       : default);
    }

    public void CancelBuildingPlacement(Context context)
    {
        CancelActivePlacement(context);
        context.RuntimeGameplayStateSystem.BuildModeActive = false;
        context.ClearCommandMode?.Invoke();
    }

    public void ExitBuildMode(Context context)
    {
        ExitBuildMode(context, clearBuildingSelection: true);
    }

    public void ExitBuildMode(Context context, bool clearBuildingSelection)
    {
        bool shouldClearSelection = clearBuildingSelection && !_preserveBuildingSelectionOnNextExitBuildMode;
        context.RuntimeGameplayStateSystem.BuildModeActive = false;
        context.InputSystem?.Reset();
        CancelActivePlacement(context);
        if (shouldClearSelection)
            context.ClearSelectedBuilding?.Invoke("ExitBuildMode");
        _preserveBuildingSelectionOnNextExitBuildMode = false;
        context.PreviewSystem?.HideOutline();
        context.ClearCommandMode?.Invoke();
    }

    public void NotifyPlacementUiPointerDown(Context context)
    {
        context.LifecycleSystem?.NotifyPlacementUiPointerDown(context.InputSystem);
    }

    private static void CancelActivePlacement(Context context)
    {
        context.LifecycleSystem?.Cancel(context.CreateCancelContext());
    }
}
