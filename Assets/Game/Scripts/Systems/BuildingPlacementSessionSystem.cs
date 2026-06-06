using System;
using UnityEngine;

internal sealed class BuildingPlacementSessionSystem
{
    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly BuildingPlacementLifecycleSystem LifecycleSystem;
        public readonly BuildingPlacementInputSystem InputSystem;
        public readonly BuildingPlacementPreviewSystem PreviewSystem;
        public readonly Func<BuildingPlacementLifecycleSystem.CancelContext> CreateCancelContext;
        public readonly Func<BuildingPlacementLifecycleSystem.BeginContext> CreateBeginContext;
        public readonly Func<BuildingPlacementLifecycleSystem.ConfirmContext> CreateConfirmContext;
        public readonly Action RecordBuildingBuilt;
        public readonly Action NotifyStaticMinimapChanged;
        public readonly Action<string> ClearSelectedBuilding;
        public readonly Action ClearCommandMode;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            BuildingPlacementLifecycleSystem lifecycleSystem,
            BuildingPlacementInputSystem inputSystem,
            BuildingPlacementPreviewSystem previewSystem,
            Func<BuildingPlacementLifecycleSystem.CancelContext> createCancelContext,
            Func<BuildingPlacementLifecycleSystem.BeginContext> createBeginContext,
            Func<BuildingPlacementLifecycleSystem.ConfirmContext> createConfirmContext,
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
        if (new MissionCommandPolicySystem().TryRejectBuildForActiveOperation())
            return;

        context.LifecycleSystem?.Begin(definition, context.CreateBeginContext());
    }

    public bool ConfirmBuildingPlacement(Context context)
    {
        if (context.LifecycleSystem == null ||
            !context.LifecycleSystem.Confirm(context.CreateConfirmContext()))
            return false;

        context.RecordBuildingBuilt?.Invoke();
        context.NotifyStaticMinimapChanged?.Invoke();
        _preserveBuildingSelectionOnNextExitBuildMode = true;
        ExitBuildMode(context, clearBuildingSelection: false);
        return true;
    }

    public void CancelBuildingPlacement(Context context)
    {
        CancelActivePlacement(context);
        if (context.RuntimeGameplayStateSystem != null)
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
        if (context.RuntimeGameplayStateSystem != null)
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
