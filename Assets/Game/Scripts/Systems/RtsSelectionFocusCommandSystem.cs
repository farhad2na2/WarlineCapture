using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionFocusCommandSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);
    public delegate TacticalCommandResult ValidateControllableEntityDelegate(Entity entity);

    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly SelectionStateSystem SelectionStateSystem;
        public readonly FocusedUnitLifecycleSystem FocusedUnitLifecycleSystem;
        public readonly UnitTargetOrderSystem UnitTargetOrderSystem;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly Camera WorldCamera;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly Action<EntityManager, string> ClearCurrentSelection;
        public readonly Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionSystem.Filter> QueueSelectionRectangleRequest;
        public readonly Action ProcessSelectionRectangleRequests;
        public readonly Action<EntityManager, Entity> ApplyHudSelection;
        public readonly Action<TacticalCommandResult> ApplyHudCommandResult;
        public readonly Action ClearHudSelection;
        public readonly Action ClearHudCommandMode;
        public readonly Action<bool> SetHudWorldMarkersVisible;
        public readonly Action<bool> SetCameraDragging;
        public readonly Action<bool> SetExplicitAttackTargetModeActive;
        public readonly Action<string> LogSelectionDiagnostic;
        public readonly FocusedUnitLifecycleSystem.DescribeEntityDelegate DescribeEntity;
        public readonly ValidateControllableEntityDelegate ValidateControllableEntity;
        public readonly Action IssueHoldPositionOrder;
        public readonly Action IssueStopOrder;
        public readonly Action DestroyFocusedUnit;
        public readonly Func<bool> IssueFocusedMissileLauncherRadarAttack;
        public readonly Func<bool> ArmFocusedAttackTargetMode;
        public readonly Action CancelExplicitAttackTargetMode;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputSystem inputSystem,
            SelectionStateSystem selectionStateSystem,
            FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
            UnitTargetOrderSystem unitTargetOrderSystem,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            Camera worldCamera,
            TryGetEntityManagerDelegate tryGetEntityManager,
            Action<EntityManager> ensureEntityQueries,
            Action<EntityManager, string> clearCurrentSelection,
            Action<Rect, RtsSelectionPointerRequestKind, VisibleUnitSelectionSystem.Filter> queueSelectionRectangleRequest,
            Action processSelectionRectangleRequests,
            Action<EntityManager, Entity> applyHudSelection,
            Action<TacticalCommandResult> applyHudCommandResult,
            Action clearHudSelection,
            Action clearHudCommandMode,
            Action<bool> setHudWorldMarkersVisible,
            Action<bool> setCameraDragging,
            Action<bool> setExplicitAttackTargetModeActive,
            Action<string> logSelectionDiagnostic,
            FocusedUnitLifecycleSystem.DescribeEntityDelegate describeEntity,
            ValidateControllableEntityDelegate validateControllableEntity,
            Action issueHoldPositionOrder,
            Action issueStopOrder,
            Action destroyFocusedUnit,
            Func<bool> issueFocusedMissileLauncherRadarAttack,
            Func<bool> armFocusedAttackTargetMode,
            Action cancelExplicitAttackTargetMode)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            SelectionStateSystem = selectionStateSystem;
            FocusedUnitLifecycleSystem = focusedUnitLifecycleSystem;
            UnitTargetOrderSystem = unitTargetOrderSystem;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            WorldCamera = worldCamera;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            ClearCurrentSelection = clearCurrentSelection;
            QueueSelectionRectangleRequest = queueSelectionRectangleRequest;
            ProcessSelectionRectangleRequests = processSelectionRectangleRequests;
            ApplyHudSelection = applyHudSelection;
            ApplyHudCommandResult = applyHudCommandResult;
            ClearHudSelection = clearHudSelection;
            ClearHudCommandMode = clearHudCommandMode;
            SetHudWorldMarkersVisible = setHudWorldMarkersVisible;
            SetCameraDragging = setCameraDragging;
            SetExplicitAttackTargetModeActive = setExplicitAttackTargetModeActive;
            LogSelectionDiagnostic = logSelectionDiagnostic;
            DescribeEntity = describeEntity;
            ValidateControllableEntity = validateControllableEntity;
            IssueHoldPositionOrder = issueHoldPositionOrder;
            IssueStopOrder = issueStopOrder;
            DestroyFocusedUnit = destroyFocusedUnit;
            IssueFocusedMissileLauncherRadarAttack = issueFocusedMissileLauncherRadarAttack;
            ArmFocusedAttackTargetMode = armFocusedAttackTargetMode;
            CancelExplicitAttackTargetMode = cancelExplicitAttackTargetMode;
        }
    }

    private readonly List<RtsSelectionCommandIntentKind> _externalSelectionCommandScratch = new();

    public void ProcessExternalSelectionCommandRequests(Context context)
    {
        if (!context.InputSystem.TryGetCommandBuffers(
                out _,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out _))
        {
            return;
        }

        _externalSelectionCommandScratch.Clear();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (!IsExternalSelectionCommand(request.Kind))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            _externalSelectionCommandScratch.Add(request.Kind);
        }

        for (int i = 0; i < _externalSelectionCommandScratch.Count; i++)
            ProcessExternalSelectionCommand(context, _externalSelectionCommandScratch[i]);
    }

    public void ClearFocusedUnit(Context context)
    {
        context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.ClearHudSelection?.Invoke();
        context.ClearHudCommandMode?.Invoke();
        context.SetHudWorldMarkersVisible?.Invoke(false);
    }

    public void DeselectAllUnits(Context context, string reason)
    {
        if (!context.TryGetEntityManager(out EntityManager em))
        {
            context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.ClearHudSelection?.Invoke();
            context.ClearHudCommandMode?.Invoke();
            context.SetHudWorldMarkersVisible?.Invoke(false);
            return;
        }

        context.ClearCurrentSelection?.Invoke(em, reason);
        context.FocusedUnitLifecycleSystem.ClearFocusedUnit(context.SelectionStateSystem);
        context.SetExplicitAttackTargetModeActive?.Invoke(false);
        context.ClearHudSelection?.Invoke();
        context.ClearHudCommandMode?.Invoke();
        context.SetHudWorldMarkersVisible?.Invoke(false);
    }

    public void SelectAllVisiblePlayerUnits(Context context, VisibleUnitSelectionSystem.Filter filter)
    {
        if (context.WorldCamera == null)
        {
            context.LogSelectionDiagnostic?.Invoke($"result=SelectAllSkipped reason=NoCamera filter={filter}");
            return;
        }

        context.QueueSelectionRectangleRequest?.Invoke(
            new Rect(0f, 0f, Screen.width, Screen.height),
            RtsSelectionPointerRequestKind.SelectionRectCommitted,
            filter);
        context.ProcessSelectionRectangleRequests?.Invoke();
        context.InputSystem.IgnoreNextLeftMouseRelease = false;
        context.InputSystem.SkipNextWorldReleaseAfterSelection = false;
        context.SetCameraDragging?.Invoke(false);
    }

    public bool FocusUnitEntity(Context context, Entity entity)
    {
        if (entity == Entity.Null || !context.TryGetEntityManager(out EntityManager em))
            return false;

        context.EnsureEntityQueries?.Invoke(em);
        if (!context.FocusedUnitLifecycleSystem.FocusUnitEntity(
                em,
                entity,
                context.SelectionStateSystem,
                context.UnitTargetOrderSystem,
                "FocusUnitEntity",
                "FocusUnitEntity",
                context.LogSelectionDiagnostic,
                context.DescribeEntity,
                context.ClearHudSelection,
                context.ApplyHudSelection))
        {
            return false;
        }

        context.BuildingPlacementInteractionSystem?.ClearSelectedBuilding(context.BuildingPlacementInteractionContext, "RTSSelection.FocusUnitEntity");
        context.InputSystem.IgnoreNextLeftMouseRelease = true;
        context.InputSystem.IgnoreWorldCommandsUntilFrame = Time.frameCount + 1;
        context.RuntimeGameplayStateSystem.SuppressNextWorldClick = true;
        context.SetCameraDragging?.Invoke(false);
        return true;
    }

    public TacticalCommandResult TrySelectRuntimeEntity(Context context, Entity entity)
    {
        TacticalCommandResult result = context.ValidateControllableEntity(entity);
        if (!result.Accepted)
        {
            context.ApplyHudCommandResult?.Invoke(result);
            return result;
        }

        result = FocusUnitEntity(context, entity)
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        context.ApplyHudCommandResult?.Invoke(result);
        return result;
    }

    private static bool IsExternalSelectionCommand(RtsSelectionCommandIntentKind kind)
    {
        return kind == RtsSelectionCommandIntentKind.SelectAll ||
               kind == RtsSelectionCommandIntentKind.SelectAllSoldiers ||
               kind == RtsSelectionCommandIntentKind.SelectAllVehicles ||
               kind == RtsSelectionCommandIntentKind.DeselectAll ||
               kind == RtsSelectionCommandIntentKind.HoldPosition ||
               kind == RtsSelectionCommandIntentKind.Stop ||
               kind == RtsSelectionCommandIntentKind.DestroyFocusedUnit ||
               kind == RtsSelectionCommandIntentKind.ToggleAttackTargetMode ||
               kind == RtsSelectionCommandIntentKind.CancelAttackTargetMode;
    }

    private void ProcessExternalSelectionCommand(Context context, RtsSelectionCommandIntentKind kind)
    {
        switch (kind)
        {
            case RtsSelectionCommandIntentKind.SelectAll:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionSystem.Filter.All);
                break;
            case RtsSelectionCommandIntentKind.SelectAllSoldiers:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionSystem.Filter.Soldiers);
                break;
            case RtsSelectionCommandIntentKind.SelectAllVehicles:
                SelectAllVisiblePlayerUnits(context, VisibleUnitSelectionSystem.Filter.Vehicles);
                break;
            case RtsSelectionCommandIntentKind.DeselectAll:
                DeselectAllUnits(context, "SelectionUiCommandSystem");
                break;
            case RtsSelectionCommandIntentKind.HoldPosition:
                context.IssueHoldPositionOrder?.Invoke();
                break;
            case RtsSelectionCommandIntentKind.Stop:
                context.IssueStopOrder?.Invoke();
                break;
            case RtsSelectionCommandIntentKind.DestroyFocusedUnit:
                context.DestroyFocusedUnit?.Invoke();
                break;
            case RtsSelectionCommandIntentKind.ToggleAttackTargetMode:
                if (context.IssueFocusedMissileLauncherRadarAttack == null ||
                    !context.IssueFocusedMissileLauncherRadarAttack())
                {
                    context.ArmFocusedAttackTargetMode?.Invoke();
                }
                break;
            case RtsSelectionCommandIntentKind.CancelAttackTargetMode:
                context.CancelExplicitAttackTargetMode?.Invoke();
                break;
        }
    }
}
