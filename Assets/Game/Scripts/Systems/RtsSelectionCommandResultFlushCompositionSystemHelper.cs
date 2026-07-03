using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed class RtsSelectionCommandResultFlushCompositionSystemHelper
    {
        public delegate bool TryGetEntityManagerAction(out EntityManager em);
        public delegate void ClearCurrentSelectionAction(EntityManager em, string reason);
        public delegate void RefreshFocusedUnitAction(EntityManager em, SelectionStateCompositionSystemHelper selectionStateSystem);
        public delegate void SetFocusedUnitAction(SelectionStateCompositionSystemHelper selectionStateSystem, Entity entity);
        public delegate void ApplyHudSelectionAction(EntityManager em, Entity entity);

        private readonly List<RtsSelectionCommandResultElement> _moveCommandResultScratch = new();
        private readonly List<RtsSelectionCommandResultElement> _attackCommandResultScratch = new();
        private readonly List<RtsSelectionCommandResultElement> _scanCommandResultScratch = new();
        private readonly List<RtsSelectionCommandResultElement> _transportCommandResultScratch = new();
        private readonly List<Entity> _selectedAttackSourceScratch = new();

        public readonly struct Context
        {
            public readonly RtsSelectionInputCompositionSystemHelper InputSystem;
            public readonly SelectionHudFeedbackUiSystemHelper HudFeedbackSystem;
            public readonly SelectionOrderMarkerPresentationSystemHelper OrderMarkerSystem;
            public readonly SelectedMoveOrderCommandSystem SelectedMoveOrderCommandSystem;
            public readonly AttackOrderCommandSystem AttackOrderCommandSystem;
            public readonly ScanIntelCommandSystem ScanIntelCommandSystem;
            public readonly TransportBoardingCommandSystem TransportBoardingCommandSystem;
            public readonly UnitMoveOrderSystem UnitMoveOrderSystem;
            public readonly UnitTransportCapacitySystem UnitTransportCapacitySystem;
            public readonly UnitTransportAirPickupSystem UnitTransportAirPickupSystem;
            public readonly SelectionStateCompositionSystemHelper SelectionStateCompositionSystemHelper;
            public readonly BuildingPlacementInteractionCompositionSystemHelper BuildingPlacementInteractionCompositionSystemHelper;
            public readonly BuildingPlacementInteractionCompositionSystemHelper.Context BuildingPlacementInteractionContext;
            public readonly EntityQuery SelectedMoveQuery;
            public readonly EntityQuery MoveTargetCommandQueueQuery;
            public readonly EntityQuery MoveTargetRuntimeStateQuery;
            public readonly EntityQuery MoveTargetSelectedMoveQuery;
            public readonly EntityQuery SelectAllCommandQueueQuery;
            public readonly EntityQuery ImmediateRespawnQueueQuery;
            public readonly EntityQuery ImmediateBuildingRuntimeStateQuery;
            public readonly EntityQuery SelectedTagQuery;
            public readonly EntityQuery GridConfigQuery;
            public readonly EntityQuery MapSurfaceQuery;
            public readonly TryGetEntityManagerAction TryGetDefaultEntityManager;
            public readonly Action<EntityManager> EnsureEntityQueries;
            public readonly ClearCurrentSelectionAction ClearCurrentSelection;
            public readonly Action<TacticalCommandMode> ApplyHudCommandMode;
            public readonly Action<BoardCommandModeDirection, bool> ApplyHudBoardCommandMode;
            public readonly Action<TacticalCommandResult> ApplyHudCommandResult;
            public readonly Action ClearHudSelection;
            public readonly ApplyHudSelectionAction ApplyHudSelection;
            public readonly Action ClearHudCommandMode;
            public readonly Action<bool> SetExplicitAttackTargetModeActive;
            public readonly Action<bool> SetHudWorldMarkersVisible;
            public readonly Action ProcessSelectionRectangleRequests;
            public readonly Action<string> LogSelectionClickDiagnostic;
            public readonly Action<Vector2> RequestMoveOrderScreenMarker;
            public readonly Action<Vector2> RequestAttackOrderScreenMarker;
            public readonly Action<bool> SetCameraDragging;
            public readonly Action<SelectionStateCompositionSystemHelper> ClearFocusedUnit;
            public readonly RefreshFocusedUnitAction RefreshFocusedUnit;
            public readonly SetFocusedUnitAction SetFocusedUnit;
            public readonly SelectedMoveOrderCommandSystem.ClickedUnitResolver TryGetMoveClickedUnitEntity;
            public readonly SelectedMoveOrderCommandSystem.ClickedCellResolver TryGetMoveClickedCell;
            public readonly SelectedMoveOrderCommandSystem.ClickedCellResolver TryGetScanClickedCell;
            public readonly AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate TryGetAttackClickedUnitEntity;
            public readonly TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate TryGetTransportClickedUnitEntity;
            public readonly TransportBoardingCommandSystem.TryGetClickedCellDelegate TryGetTransportClickedCell;

            public Context(
                RtsSelectionInputCompositionSystemHelper inputSystem,
                SelectionHudFeedbackUiSystemHelper hudFeedbackSystem,
                SelectionOrderMarkerPresentationSystemHelper orderMarkerSystem,
                SelectedMoveOrderCommandSystem selectedMoveOrderCommandSystem,
                AttackOrderCommandSystem attackOrderCommandSystem,
                ScanIntelCommandSystem scanIntelCommandSystem,
                TransportBoardingCommandSystem transportBoardingCommandSystem,
                UnitMoveOrderSystem unitMoveOrderSystem,
                UnitTransportCapacitySystem unitTransportCapacitySystem,
                UnitTransportAirPickupSystem unitTransportAirPickupSystem,
                SelectionStateCompositionSystemHelper selectionStateSystem,
                BuildingPlacementInteractionCompositionSystemHelper buildingPlacementInteractionSystem,
                BuildingPlacementInteractionCompositionSystemHelper.Context buildingPlacementInteractionContext,
                EntityQuery selectedMoveQuery,
                EntityQuery moveTargetCommandQueueQuery,
                EntityQuery moveTargetRuntimeStateQuery,
                EntityQuery moveTargetSelectedMoveQuery,
                EntityQuery selectAllCommandQueueQuery,
                EntityQuery immediateRespawnQueueQuery,
                EntityQuery immediateBuildingRuntimeStateQuery,
                EntityQuery selectedTagQuery,
                EntityQuery gridConfigQuery,
                EntityQuery mapSurfaceQuery,
                TryGetEntityManagerAction tryGetDefaultEntityManager,
                Action<EntityManager> ensureEntityQueries,
                ClearCurrentSelectionAction clearCurrentSelection,
                Action<TacticalCommandMode> applyHudCommandMode,
                Action<BoardCommandModeDirection, bool> applyHudBoardCommandMode,
                Action<TacticalCommandResult> applyHudCommandResult,
                Action clearHudSelection,
                ApplyHudSelectionAction applyHudSelection,
                Action clearHudCommandMode,
                Action<bool> setExplicitAttackTargetModeActive,
                Action<bool> setHudWorldMarkersVisible,
                Action processSelectionRectangleRequests,
                Action<string> logSelectionClickDiagnostic,
                Action<Vector2> requestMoveOrderScreenMarker,
                Action<Vector2> requestAttackOrderScreenMarker,
                Action<bool> setCameraDragging,
                Action<SelectionStateCompositionSystemHelper> clearFocusedUnit,
                RefreshFocusedUnitAction refreshFocusedUnit,
                SetFocusedUnitAction setFocusedUnit,
                SelectedMoveOrderCommandSystem.ClickedUnitResolver tryGetMoveClickedUnitEntity,
                SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetMoveClickedCell,
                SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetScanClickedCell,
                AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate tryGetAttackClickedUnitEntity,
                TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate tryGetTransportClickedUnitEntity,
                TransportBoardingCommandSystem.TryGetClickedCellDelegate tryGetTransportClickedCell)
            {
                InputSystem = inputSystem;
                HudFeedbackSystem = hudFeedbackSystem;
                OrderMarkerSystem = orderMarkerSystem;
                SelectedMoveOrderCommandSystem = selectedMoveOrderCommandSystem;
                AttackOrderCommandSystem = attackOrderCommandSystem;
                ScanIntelCommandSystem = scanIntelCommandSystem;
                TransportBoardingCommandSystem = transportBoardingCommandSystem;
                UnitMoveOrderSystem = unitMoveOrderSystem;
                UnitTransportCapacitySystem = unitTransportCapacitySystem;
                UnitTransportAirPickupSystem = unitTransportAirPickupSystem;
                SelectionStateCompositionSystemHelper = selectionStateSystem;
                BuildingPlacementInteractionCompositionSystemHelper = buildingPlacementInteractionSystem;
                BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
                SelectedMoveQuery = selectedMoveQuery;
                MoveTargetCommandQueueQuery = moveTargetCommandQueueQuery;
                MoveTargetRuntimeStateQuery = moveTargetRuntimeStateQuery;
                MoveTargetSelectedMoveQuery = moveTargetSelectedMoveQuery;
                SelectAllCommandQueueQuery = selectAllCommandQueueQuery;
                ImmediateRespawnQueueQuery = immediateRespawnQueueQuery;
                ImmediateBuildingRuntimeStateQuery = immediateBuildingRuntimeStateQuery;
                SelectedTagQuery = selectedTagQuery;
                GridConfigQuery = gridConfigQuery;
                MapSurfaceQuery = mapSurfaceQuery;
                TryGetDefaultEntityManager = tryGetDefaultEntityManager;
                EnsureEntityQueries = ensureEntityQueries;
                ClearCurrentSelection = clearCurrentSelection;
                ApplyHudCommandMode = applyHudCommandMode;
                ApplyHudBoardCommandMode = applyHudBoardCommandMode;
                ApplyHudCommandResult = applyHudCommandResult;
                ClearHudSelection = clearHudSelection;
                ApplyHudSelection = applyHudSelection;
                ClearHudCommandMode = clearHudCommandMode;
                SetExplicitAttackTargetModeActive = setExplicitAttackTargetModeActive;
                SetHudWorldMarkersVisible = setHudWorldMarkersVisible;
                ProcessSelectionRectangleRequests = processSelectionRectangleRequests;
                LogSelectionClickDiagnostic = logSelectionClickDiagnostic;
                RequestMoveOrderScreenMarker = requestMoveOrderScreenMarker;
                RequestAttackOrderScreenMarker = requestAttackOrderScreenMarker;
                SetCameraDragging = setCameraDragging;
                ClearFocusedUnit = clearFocusedUnit;
                RefreshFocusedUnit = refreshFocusedUnit;
                SetFocusedUnit = setFocusedUnit;
                TryGetMoveClickedUnitEntity = tryGetMoveClickedUnitEntity;
                TryGetMoveClickedCell = tryGetMoveClickedCell;
                TryGetScanClickedCell = tryGetScanClickedCell;
                TryGetAttackClickedUnitEntity = tryGetAttackClickedUnitEntity;
                TryGetTransportClickedUnitEntity = tryGetTransportClickedUnitEntity;
                TryGetTransportClickedCell = tryGetTransportClickedCell;
            }
        }

        public bool ProcessFocusedMissileLauncherRadarAttack(Context context, Entity launcher)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionMissileLauncherRadarAttackCommandSystem.TryIssuePendingFocusedRadarAttack(
                    em,
                    launcher,
                    out float3 targetPosition))
            {
                return false;
            }

            context.OrderMarkerSystem?.ShowAttackOrderMarker(
                em,
                new Vector3(targetPosition.x, targetPosition.y, targetPosition.z));
            context.ClearCurrentSelection?.Invoke(em, "MissileLauncherRadarAttack");
            if (context.SelectionStateCompositionSystemHelper != null)
                context.SetFocusedUnit?.Invoke(context.SelectionStateCompositionSystemHelper, launcher);
            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.SetCameraDragging?.Invoke(false);
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Success());
            context.ClearHudCommandMode?.Invoke();
            context.SetHudWorldMarkersVisible?.Invoke(true);
            context.ApplyHudSelection?.Invoke(em, launcher);
            return true;
        }

        public void UpdateCommandPreviewMarkers(
            Context context,
            bool explicitAttackTargetModeActive,
            SelectionOrderMarkerPresentationSystemHelper.IsPreviewTargetValidWithSourceDelegate isValidBoardTransportPreviewTarget,
            SelectionOrderMarkerPresentationSystemHelper.IsPreviewTargetValidWithSourceDelegate isValidBoardPassengerPreviewTarget)
        {
            if (context.OrderMarkerSystem == null)
                return;

            if (!explicitAttackTargetModeActive)
            {
                context.OrderMarkerSystem.UpdateAttackTargetPreviewMarkers(default, false);
            }
            else if (context.TryGetDefaultEntityManager(out EntityManager attackPreviewEm))
            {
                context.EnsureEntityQueries?.Invoke(attackPreviewEm);
                context.OrderMarkerSystem.UpdateAttackTargetPreviewMarkers(attackPreviewEm, true);
            }

            if (context.InputSystem == null ||
                !context.InputSystem.TryGetActiveBoardCommandMode(out BoardCommandModeDirection direction, out Entity transport))
            {
                if (!explicitAttackTargetModeActive)
                    context.OrderMarkerSystem.UpdateBoardTargetPreviewMarkers(default, false, Entity.Null, null);
                return;
            }

            if (!context.TryGetDefaultEntityManager(out EntityManager boardPreviewEm))
                return;

            context.EnsureEntityQueries?.Invoke(boardPreviewEm);
            if (direction == BoardCommandModeDirection.PassengerToTransport)
            {
                context.OrderMarkerSystem.UpdateBoardTargetPreviewMarkers(
                    boardPreviewEm,
                    true,
                    Entity.Null,
                    isValidBoardTransportPreviewTarget);
                return;
            }

            if (direction == BoardCommandModeDirection.TransportToPassenger)
            {
                context.OrderMarkerSystem.UpdateBoardTargetPreviewMarkers(
                    boardPreviewEm,
                    true,
                    transport,
                    isValidBoardPassengerPreviewTarget);
                return;
            }

            context.OrderMarkerSystem.UpdateBoardTargetPreviewMarkers(default, false, Entity.Null, null);
        }

        public bool ProcessSelectAllCommandRequests(Context context)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !ProcessSelectAllCommandRequests(context, em))
            {
                return false;
            }

            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.ClearHudCommandMode?.Invoke();
            context.SetHudWorldMarkersVisible?.Invoke(false);
            if (context.InputSystem != null &&
                context.InputSystem.HasPendingSelectionRectangleRequests())
            {
                context.ProcessSelectionRectangleRequests?.Invoke();
            }
            context.SetCameraDragging?.Invoke(false);
            return true;
        }

        public bool TryProcessSelectedBuildingDestroyFallback(
            Context context,
            RtsSelectionCommandIntentKind processedKind,
            bool accepted,
            TacticalCommandReasonCode rejectionReason)
        {
            if (processedKind != RtsSelectionCommandIntentKind.DestroyFocusedUnit ||
                accepted ||
                rejectionReason != TacticalCommandReasonCode.NoSelection ||
                context.BuildingPlacementInteractionCompositionSystemHelper == null ||
                !context.BuildingPlacementInteractionCompositionSystemHelper.HasSelectedBuilding(context.BuildingPlacementInteractionContext))
            {
                return false;
            }

            context.BuildingPlacementInteractionCompositionSystemHelper.DeleteSelectedBuilding(context.BuildingPlacementInteractionContext);
            context.ClearHudSelection?.Invoke();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Success("Destroyed selected building."));
            return true;
        }

        public bool ProcessImmediateSelectedUnitCommandRequests(Context context, Entity focusedUnit)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !ProcessImmediateSelectedUnitCommandRequests(
                    context,
                    em,
                    focusedUnit,
                    out RtsSelectionCommandIntentKind processedKind,
                    out bool accepted,
                    out TacticalCommandReasonCode rejectionReason,
                    out int issuedCount))
            {
                return false;
            }

            bool hasCommandMode = TryGetImmediateSelectedUnitCommandMode(processedKind, out TacticalCommandMode mode);
            bool destroyFocusedUnit = processedKind == RtsSelectionCommandIntentKind.DestroyFocusedUnit;
            if (hasCommandMode)
                context.ApplyHudCommandMode?.Invoke(mode);
            if (!accepted)
            {
                if (TryProcessSelectedBuildingDestroyFallback(
                        context,
                        processedKind,
                        accepted,
                        rejectionReason))
                {
                    return true;
                }

                context.ApplyHudCommandResult?.Invoke(
                    BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
                if (hasCommandMode)
                    context.ClearHudCommandMode?.Invoke();
                return true;
            }

            if (destroyFocusedUnit)
            {
                if (context.SelectionStateCompositionSystemHelper != null)
                    context.ClearFocusedUnit?.Invoke(context.SelectionStateCompositionSystemHelper);
                context.ClearHudSelection?.Invoke();
                context.ApplyHudCommandResult?.Invoke(
                    BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
                return true;
            }

            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.SetHudWorldMarkersVisible?.Invoke(false);
            if (hasCommandMode)
            {
                context.BuildingPlacementInteractionCompositionSystemHelper?.ExitBuildMode(context.BuildingPlacementInteractionContext);
                context.BuildingPlacementInteractionCompositionSystemHelper?.CancelBuildingPlacement(context.BuildingPlacementInteractionContext);
                context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                    context.BuildingPlacementInteractionContext,
                    $"SelectionUiCommandUiSystemHelper.{mode}");
                context.SetCameraDragging?.Invoke(false);
                context.ClearHudCommandMode?.Invoke();
                context.ApplyHudCommandResult?.Invoke(
                    BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
                if (context.SelectionStateCompositionSystemHelper != null)
                    context.RefreshFocusedUnit?.Invoke(em, context.SelectionStateCompositionSystemHelper);
                return true;
            }

            context.ApplyHudCommandResult?.Invoke(
                BuildImmediateSelectedUnitCommandResult(processedKind, accepted, rejectionReason, issuedCount));
            return true;
        }

        public bool ProcessDeselectAllCommandRequests(Context context)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !ProcessDeselectAllCommandRequests(context, em))
            {
                return false;
            }

            context.SelectionStateCompositionSystemHelper?.ClearSelectedMoveCache();
            if (context.SelectionStateCompositionSystemHelper != null)
                context.ClearFocusedUnit?.Invoke(context.SelectionStateCompositionSystemHelper);
            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.ClearHudSelection?.Invoke();
            context.ClearHudCommandMode?.Invoke();
            context.SetHudWorldMarkersVisible?.Invoke(false);
            context.SetCameraDragging?.Invoke(false);
            return true;
        }

        private static bool ProcessSelectAllCommandRequests(Context context, EntityManager em)
        {
            context.EnsureEntityQueries?.Invoke(em);
            return RtsSelectionSelectAllCommandSystem.ProcessPendingRequests(
                em,
                context.SelectAllCommandQueueQuery);
        }

        private static bool ProcessDeselectAllCommandRequests(Context context, EntityManager em)
        {
            context.EnsureEntityQueries?.Invoke(em);
            return RtsSelectionDeselectAllCommandSystem.ProcessPendingRequests(
                em,
                context.MoveTargetCommandQueueQuery,
                context.SelectedTagQuery);
        }

        public bool ProcessCancelActiveCommandModeRequests(Context context)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em))
            {
                return false;
            }

            context.EnsureEntityQueries?.Invoke(em);
            if (!RtsSelectionCancelActiveCommandModeSystem.ProcessPendingRequests(
                    em,
                    context.MoveTargetCommandQueueQuery,
                    context.MoveTargetRuntimeStateQuery,
                    out _))
            {
                return false;
            }

            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.SetCameraDragging?.Invoke(false);
            context.SetHudWorldMarkersVisible?.Invoke(false);
            context.ClearHudCommandMode?.Invoke();
            return true;
        }

        public bool ProcessMoveTargetModeCommandRequests(Context context, int currentFrame)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !ProcessMoveTargetModeCommandRequests(context, em, currentFrame, out bool accepted, out TacticalCommandReasonCode rejectionReason))
            {
                return false;
            }

            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                context.BuildingPlacementInteractionContext,
                "SelectionUiCommandUiSystemHelper.EnterMoveTargetMode");
            context.SetCameraDragging?.Invoke(false);
            if (!accepted)
            {
                context.ClearHudCommandMode?.Invoke();
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(rejectionReason));
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"moveModeEntered result=False reason={rejectionReason} frame={currentFrame}");
                return true;
            }

            context.SetHudWorldMarkersVisible?.Invoke(false);
            context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Move);
            if (context.InputSystem != null)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"enterMoveTargetModeArmed mode={TacticalCommandMode.Move} oneShot=True requiresWorldTarget=True " +
                    $"ignoreWorldUntil={context.InputSystem.IgnoreWorldCommandsUntilFrame} frame={currentFrame}");
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"moveModeEntered result=True frame={currentFrame} dragReset={context.InputSystem.LastPointerPosition}");
            }
            else
            {
                context.LogSelectionClickDiagnostic?.Invoke($"moveModeEntered result=True frame={currentFrame}");
            }
            return true;
        }

        public bool ProcessAttackTargetModeCommandRequests(Context context, int currentFrame, Entity focusedUnit)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionAttackTargetModeCommandSystem.ProcessPendingRequests(
                    em,
                    context.MoveTargetCommandQueueQuery,
                    context.MoveTargetRuntimeStateQuery,
                    context.SelectedTagQuery,
                    focusedUnit,
                    currentFrame,
                    out RtsSelectionCommandIntentKind processedKind,
                    out bool accepted,
                    out bool airDefenseAutoEngageOnly,
                    out TacticalCommandReasonCode rejectionReason))
            {
                return false;
            }

            bool enterAttackTargetMode = processedKind == RtsSelectionCommandIntentKind.EnterAttackTargetMode;
            bool toggleAttackTargetMode = processedKind == RtsSelectionCommandIntentKind.ToggleAttackTargetMode;
            if (enterAttackTargetMode)
            {
                context.SetExplicitAttackTargetModeActive?.Invoke(false);
                context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                    context.BuildingPlacementInteractionContext,
                    "SelectionUiCommandUiSystemHelper.EnterAttackTargetMode");
            }

            if (enterAttackTargetMode || accepted)
                context.SetCameraDragging?.Invoke(false);

            if (airDefenseAutoEngageOnly)
            {
                context.ClearHudCommandMode?.Invoke();
                context.ApplyHudCommandResult?.Invoke(
                    TacticalCommandResult.Success("Air defense auto-engages aircraft and incoming missiles."));
                context.SetHudWorldMarkersVisible?.Invoke(false);
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"attackModeEntered result=False reason=AirDefenseAutoEngage frame={currentFrame}");
                return true;
            }

            if (!accepted)
            {
                if (enterAttackTargetMode)
                    context.ClearHudCommandMode?.Invoke();
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(rejectionReason));
                if (enterAttackTargetMode)
                    context.SetHudWorldMarkersVisible?.Invoke(false);
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"{(toggleAttackTargetMode ? "attackModeToggled" : "attackModeEntered")} result=False reason={rejectionReason} frame={currentFrame}");
                return true;
            }

            context.SetExplicitAttackTargetModeActive?.Invoke(true);
            context.SetHudWorldMarkersVisible?.Invoke(true);
            context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Attack);
            context.LogSelectionClickDiagnostic?.Invoke(
                $"{(toggleAttackTargetMode ? "attackModeToggled" : "attackModeEntered")} result=True frame={currentFrame} dragReset={SelectionPointerPosition(context)}");
            return true;
        }

        public bool ProcessSelectionModeCommandRequests(Context context, int currentFrame)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em) ||
                !RtsSelectionModeCommandSystem.ProcessPendingRequests(
                    em,
                    currentFrame,
                    out bool enteredSelectionMode,
                    out bool exitedSelectionMode,
                    out RtsSelectionCommandIntentKind lastProcessedKind))
            {
                return false;
            }

            if (enteredSelectionMode)
            {
                context.SetExplicitAttackTargetModeActive?.Invoke(false);
                context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                    context.BuildingPlacementInteractionContext,
                    "SelectionUiCommandUiSystemHelper.EnterSelectionMode");
            }

            context.SetHudWorldMarkersVisible?.Invoke(false);
            if (lastProcessedKind == RtsSelectionCommandIntentKind.EnterSelectionMode)
                context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Select);
            else if (lastProcessedKind == RtsSelectionCommandIntentKind.ExitSelectionMode)
                context.ClearHudCommandMode?.Invoke();

            context.SetCameraDragging?.Invoke(false);

            if (enteredSelectionMode)
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"selectionModeEntered source=ui frame={currentFrame} dragReset={SelectionPointerPosition(context)}");
            if (exitedSelectionMode)
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"selectionModeExited source=ui frame={currentFrame} dragReset={SelectionPointerPosition(context)}");
            return true;
        }

        private static bool ProcessMoveTargetModeCommandRequests(
            Context context,
            EntityManager em,
            int currentFrame,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason)
        {
            context.EnsureEntityQueries?.Invoke(em);
            return RtsSelectionMoveTargetModeCommandSystem.ProcessPendingRequests(
                em,
                context.MoveTargetCommandQueueQuery,
                context.MoveTargetRuntimeStateQuery,
                context.MoveTargetSelectedMoveQuery,
                currentFrame,
                out accepted,
                out rejectionReason);
        }

        private static bool ProcessImmediateSelectedUnitCommandRequests(
            Context context,
            EntityManager em,
            Entity focusedUnit,
            out RtsSelectionCommandIntentKind processedKind,
            out bool accepted,
            out TacticalCommandReasonCode rejectionReason,
            out int issuedCount)
        {
            context.EnsureEntityQueries?.Invoke(em);
            return RtsSelectionImmediateSelectedUnitCommandSystem.ProcessPendingRequests(
                em,
                context.MoveTargetCommandQueueQuery,
                context.MoveTargetRuntimeStateQuery,
                context.ImmediateRespawnQueueQuery,
                context.ImmediateBuildingRuntimeStateQuery,
                context.SelectedTagQuery,
                context.SelectedMoveQuery,
                focusedUnit,
                out processedKind,
                out accepted,
                out rejectionReason,
                out issuedCount);
        }

        public bool ProcessScanTargetModeCommandRequests(Context context, int currentFrame)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em))
            {
                return false;
            }

            context.EnsureEntityQueries?.Invoke(em);
            if (!RtsSelectionScanTargetModeCommandSystem.ProcessPendingRequests(
                    em,
                    context.MoveTargetCommandQueueQuery,
                    context.MoveTargetRuntimeStateQuery,
                    currentFrame))
            {
                return false;
            }

            SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                $"processScanTargetModeCommandRequests accepted=True frame={currentFrame}");
            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.BuildingPlacementInteractionCompositionSystemHelper?.ExitBuildMode(context.BuildingPlacementInteractionContext);
            context.BuildingPlacementInteractionCompositionSystemHelper?.CancelBuildingPlacement(context.BuildingPlacementInteractionContext);
            context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                context.BuildingPlacementInteractionContext,
                "SelectionUiCommandUiSystemHelper.EnterScanTargetMode");
            context.SetCameraDragging?.Invoke(false);
            context.SetHudWorldMarkersVisible?.Invoke(false);
            context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Scan);
            context.LogSelectionClickDiagnostic?.Invoke(
                $"scanModeEntered result=True frame={currentFrame} dragReset={SelectionPointerPosition(context)}");
            return true;
        }

        public bool ProcessBoardTargetModeCommandRequests(Context context, int currentFrame)
        {
            if (!context.TryGetDefaultEntityManager(out EntityManager em))
            {
                return false;
            }

            context.EnsureEntityQueries?.Invoke(em);
            if (!RtsSelectionBoardTargetModeCommandSystem.ProcessPendingRequests(
                    em,
                    context.MoveTargetCommandQueueQuery,
                    context.MoveTargetRuntimeStateQuery,
                    context.SelectedTagQuery,
                    currentFrame,
                    out bool accepted,
                    out bool toggledOff,
                    out BoardCommandModeDirection direction,
                    out Entity transport,
                    out TacticalCommandReasonCode rejectionReason))
            {
                return false;
            }

            context.SetExplicitAttackTargetModeActive?.Invoke(false);
            context.BuildingPlacementInteractionCompositionSystemHelper?.ClearSelectedBuilding(
                context.BuildingPlacementInteractionContext,
                "SelectionUiCommandUiSystemHelper.EnterBoardTargetMode");
            context.SetCameraDragging?.Invoke(false);

            if (toggledOff)
            {
                context.ClearHudCommandMode?.Invoke();
                context.SetHudWorldMarkersVisible?.Invoke(false);
                context.LogSelectionClickDiagnostic?.Invoke($"boardModeToggledOff frame={currentFrame}");
                return true;
            }

            if (!accepted)
            {
                string message = rejectionReason == TacticalCommandReasonCode.CommandUnavailable
                    ? "Selected unit cannot board."
                    : "Select a unit first.";
                context.ClearHudCommandMode?.Invoke();
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(rejectionReason, message));
                context.SetHudWorldMarkersVisible?.Invoke(false);
                context.LogSelectionClickDiagnostic?.Invoke(
                    $"boardModeEntered result=False reason={rejectionReason} message=\"{message}\" frame={currentFrame}");
                return true;
            }

            context.SetHudWorldMarkersVisible?.Invoke(true);
            bool boardAllInteractable = direction == BoardCommandModeDirection.TransportToPassenger &&
                                        transport != Entity.Null;
            context.ApplyHudBoardCommandMode?.Invoke(direction, boardAllInteractable);
            context.LogSelectionClickDiagnostic?.Invoke(
                $"boardModeEntered result=True direction={direction} transport={transport} frame={currentFrame} dragReset={SelectionPointerPosition(context)}");
            return true;
        }

        public void UpdateOrderMarkerVisibility(Context context)
        {
            context.OrderMarkerSystem.UpdateMoveOrderMarkerVisibility(context.SetHudWorldMarkersVisible);
            context.OrderMarkerSystem.UpdateAttackOrderMarkerVisibility(context.SetHudWorldMarkersVisible);
            context.OrderMarkerSystem.UpdateScanOrderMarkerVisibility(context.SetHudWorldMarkersVisible);
        }

        public void ProcessMoveCommandRequests(Context context)
        {
            EnsureFeedbackQueue(context);
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"processMoveCommandRequestsEnter frame={UnityEngine.Time.frameCount}");

            if (!TryGetFreshCommandBuffers(
                    context,
                    out EntityManager em,
                    out Entity commandEntity,
                    out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                    out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
            {
                if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
                    SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"processMoveCommandRequestsNoBuffers frame={UnityEngine.Time.frameCount}");
                context.ClearHudCommandMode?.Invoke();
                context.InputSystem.ClearActiveCommandMode();
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return;
            }

            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"processMoveCommandRequestsBuffers commandEntity={commandEntity} totalRequests={commandRequests.Length} " +
                    $"moveRequests={CountRequests(commandRequests, RtsSelectionCommandIntentKind.Move)} resultBuffer={commandResults.Length} frame={UnityEngine.Time.frameCount}");
            }

            context.SelectedMoveOrderCommandSystem.ProcessCommandIntentRequests(
                em,
                commandEntity,
                context.SelectedMoveQuery,
                context.GridConfigQuery,
                context.MapSurfaceQuery,
                context.SelectionStateCompositionSystemHelper?.CachedSelectedMoveEntities,
                context.UnitMoveOrderSystem,
                context.TryGetMoveClickedUnitEntity,
                context.TryGetMoveClickedCell);

            if (!TryRefreshCommandBuffers(em, commandEntity, out commandRequests, out commandResults))
            {
                context.ClearHudCommandMode?.Invoke();
                context.InputSystem.ClearActiveCommandMode();
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return;
            }

            DrainResults(commandResults, RtsSelectionCommandIntentKind.Move, _moveCommandResultScratch);
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"processMoveCommandRequestsDrained results={_moveCommandResultScratch.Count} remainingResultBuffer={commandResults.Length} frame={UnityEngine.Time.frameCount}");
            }

            bool handled = false;
            for (int i = 0; i < _moveCommandResultScratch.Count; i++)
            {
                RtsSelectionCommandResultElement result = _moveCommandResultScratch[i];
                bool clearCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Move);
                if (clearCommandMode)
                    context.InputSystem.ClearActiveCommandMode();
                if (result.Accepted != 0)
                {
                    context.OrderMarkerSystem.TryShowCommandResultMarker(em, result);
                    context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
                    if (clearCommandMode)
                        context.ClearHudCommandMode?.Invoke();
                    else
                        context.ApplyHudCommandMode?.Invoke(TacticalCommandMode.Move);
                }
                else
                {
                    if (clearCommandMode)
                        context.ClearHudCommandMode?.Invoke();
                    context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
                }
                if (result.EmitScreenMarker != 0)
                    context.RequestMoveOrderScreenMarker?.Invoke(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
                if (result.ShowWorldMarkers != 0)
                    context.SetHudWorldMarkersVisible?.Invoke(true);
                handled = true;
            }

            if (!handled)
            {
                if (HasPendingPreResolvedMoveRequest(commandRequests))
                    return;

                if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
                    SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"processMoveCommandRequestsUnhandled frame={UnityEngine.Time.frameCount}");
                context.ClearHudCommandMode?.Invoke();
                context.InputSystem.ClearActiveCommandMode();
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            }
        }

        private static bool HasPendingPreResolvedMoveRequest(DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests)
        {
            for (int i = 0; i < commandRequests.Length; i++)
            {
                RtsSelectionCommandIntentRequestElement request = commandRequests[i];
                if (request.Kind == RtsSelectionCommandIntentKind.Move &&
                    request.HasTargetCell != 0 &&
                    request.HasWorldPosition != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFreshCommandBuffers(
            Context context,
            out EntityManager em,
            out Entity commandEntity,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
        {
            em = default;
            commandEntity = Entity.Null;
            commandRequests = default;
            commandResults = default;

            bool ensuredBeforeBufferRead = false;
            if (context.TryGetDefaultEntityManager != null &&
                context.TryGetDefaultEntityManager(out EntityManager resolvedEntityManager))
            {
                context.EnsureEntityQueries?.Invoke(resolvedEntityManager);
                ensuredBeforeBufferRead = true;
            }

            if (context.InputSystem == null ||
                !context.InputSystem.TryGetCommandBuffers(out em, out commandEntity, out commandRequests, out commandResults))
            {
                return false;
            }

            if (!ensuredBeforeBufferRead)
                context.EnsureEntityQueries?.Invoke(em);

            return TryRefreshCommandBuffers(em, commandEntity, out commandRequests, out commandResults);
        }

        private static bool TryRefreshCommandBuffers(
            EntityManager em,
            Entity commandEntity,
            out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
            out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
        {
            commandRequests = default;
            commandResults = default;
            if (commandEntity == Entity.Null ||
                !em.Exists(commandEntity) ||
                !em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity) ||
                !em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
            {
                return false;
            }

            commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
            commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            return true;
        }

        private static int CountRequests(
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests,
            RtsSelectionCommandIntentKind kind)
        {
            int count = 0;
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].Kind == kind)
                    count++;
            }

            return count;
        }

        public bool ProcessAttackCommandRequests(Context context, bool explicitAttackTargetModeActive)
        {
            EnsureFeedbackQueue(context);

            if (!TryGetFreshCommandBuffers(
                    context,
                    out EntityManager em,
                    out Entity commandEntity,
                    out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                    out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
            {
                if (explicitAttackTargetModeActive)
                    context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
                return false;
            }

            context.AttackOrderCommandSystem.ProcessCommandIntentRequests(
                em,
                commandEntity,
                commandRequests,
                commandResults,
                context.TryGetAttackClickedUnitEntity,
                (sourceEm, sources) => CollectSelectedAttackSources(context, sourceEm, sources),
                context.BuildingPlacementInteractionCompositionSystemHelper,
                context.BuildingPlacementInteractionContext,
                _selectedAttackSourceScratch);

            if (!TryRefreshCommandBuffers(em, commandEntity, out _, out commandResults))
                return false;

            DrainResults(commandResults, RtsSelectionCommandIntentKind.Attack, _attackCommandResultScratch);

            bool issued = false;
            for (int i = 0; i < _attackCommandResultScratch.Count; i++)
            {
                RtsSelectionCommandResultElement result = _attackCommandResultScratch[i];
                if (result.HasCommandResult != 0)
                    context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));

                if (result.Accepted == 0)
                    continue;

                bool clearInputCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Attack);
                bool clearHudCommandMode = clearInputCommandMode || explicitAttackTargetModeActive;
                if (clearInputCommandMode)
                    context.InputSystem.ClearActiveCommandMode();
                context.OrderMarkerSystem.TryShowCommandResultMarker(em, result);
                if (result.EmitScreenMarker != 0)
                    context.RequestAttackOrderScreenMarker?.Invoke(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
                context.SetCameraDragging?.Invoke(false);
                if (clearHudCommandMode)
                    context.ClearHudCommandMode?.Invoke();
                if (result.ShowWorldMarkers != 0)
                    context.SetHudWorldMarkersVisible?.Invoke(true);
                issued = true;
            }

            return issued;
        }

        private static void CollectSelectedAttackSources(Context context, EntityManager em, List<Entity> sources)
        {
            if (sources == null || em.World == null || !em.World.IsCreated)
                return;

            context.EnsureEntityQueries?.Invoke(em);
            TryAddAttackSource(em, context.SelectionStateCompositionSystemHelper.FocusedUnit, sources);

            List<Entity> cached = context.SelectionStateCompositionSystemHelper.CachedSelectedMoveEntities;
            for (int i = 0; i < cached.Count; i++)
                TryAddAttackSource(em, cached[i], sources);

            EntityQuery selectedTagQuery = context.SelectedTagQuery;
            if (selectedTagQuery.IsEmptyIgnoreFilter)
                return;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedTagQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> selectedEntities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < selectedEntities.Length; i++)
                    TryAddAttackSource(em, selectedEntities[i], sources);
            }
        }

        private static bool TryAddAttackSource(EntityManager em, Entity entity, List<Entity> sources)
        {
            if (entity == Entity.Null ||
                !em.Exists(entity) ||
                sources.Contains(entity) ||
                em.HasComponent<Disabled>(entity) ||
                em.HasComponent<UnitTransportPassenger>(entity) ||
                !em.HasComponent<UnitAttack>(entity) ||
                !em.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            if (!IsAttackSourceEntity(em, entity))
                return false;

            sources.Add(entity);
            return true;
        }

        private static bool IsAttackSourceEntity(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<Faction>(entity) ||
                !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id) ||
                !em.HasComponent<UnitMove>(entity) ||
                !em.HasComponent<UnitCombat>(entity) ||
                em.GetComponentData<UnitCombat>(entity).CanAttack == 0)
            {
                return false;
            }

            return !em.HasComponent<UnitHealth>(entity) ||
                   em.GetComponentData<UnitHealth>(entity).Current > 0;
        }

        public bool ProcessScanCommandRequests(Context context)
        {
            EnsureFeedbackQueue(context);

            if (!TryGetFreshCommandBuffers(
                    context,
                    out EntityManager em,
                    out Entity commandEntity,
                    out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                    out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                    $"processScanCommandRequests result=False reason=NoCommandBuffers frame={UnityEngine.Time.frameCount}");
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable));
                return false;
            }

            SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                $"processScanCommandRequests begin requests={commandRequests.Length} results={commandResults.Length} frame={UnityEngine.Time.frameCount}");
            context.ScanIntelCommandSystem.ProcessCommandIntentRequests(
                em,
                commandEntity,
                commandRequests,
                commandResults,
                context.GridConfigQuery,
                context.TryGetScanClickedCell);

            if (!TryRefreshCommandBuffers(em, commandEntity, out _, out commandResults))
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                    $"processScanCommandRequests result=False reason=RefreshBuffersFailed frame={UnityEngine.Time.frameCount}");
                return false;
            }

            DrainResults(commandResults, RtsSelectionCommandIntentKind.Scan, _scanCommandResultScratch);
            SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                $"processScanCommandRequests drainedResults={_scanCommandResultScratch.Count} remainingResults={commandResults.Length} frame={UnityEngine.Time.frameCount}");

            bool issued = false;
            for (int i = 0; i < _scanCommandResultScratch.Count; i++)
            {
                RtsSelectionCommandResultElement result = _scanCommandResultScratch[i];
                SelectionRuntimeDiagnosticsSystemHelper.LogScanCommandTrace(
                    $"processScanCommandResult index={i} accepted={result.Accepted} reason={(TacticalCommandReasonCode)result.ReasonCode} " +
                    $"hasCommandResult={result.HasCommandResult} revealed={result.RevealedCount} source={result.SourceEntity} frame={UnityEngine.Time.frameCount}");
                if (result.Accepted == 0)
                {
                    bool clearRejectedInputCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Scan);
                    if (clearRejectedInputCommandMode)
                    {
                        context.InputSystem.ClearActiveCommandMode();
                        context.SetCameraDragging?.Invoke(false);
                        context.ClearHudCommandMode?.Invoke();
                    }
                    if (result.HasCommandResult != 0)
                        context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
                    continue;
                }

                bool clearInputCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Scan);
                if (clearInputCommandMode)
                    context.InputSystem.ClearActiveCommandMode();
                context.OrderMarkerSystem.TryShowCommandResultMarker(em, result);
                context.SetCameraDragging?.Invoke(false);
                if (clearInputCommandMode)
                    context.ClearHudCommandMode?.Invoke();
                if (result.HasCommandResult != 0)
                    context.ApplyHudCommandResult?.Invoke(ToScanCommandResult(result));
                if (result.ShowWorldMarkers != 0)
                    context.SetHudWorldMarkersVisible?.Invoke(true);
                issued = true;
            }

            return issued || _scanCommandResultScratch.Count > 0;
        }

        public bool ProcessTransportCommandRequests(Context context)
        {
            EnsureFeedbackQueue(context);

            if (!TryGetFreshCommandBuffers(
                    context,
                    out EntityManager em,
                    out Entity commandEntity,
                    out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                    out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
            {
                return false;
            }

            context.TransportBoardingCommandSystem.ProcessCommandIntentRequests(
                em,
                commandEntity,
                commandRequests,
                commandResults,
                context.UnitTransportCapacitySystem,
                context.UnitTransportAirPickupSystem,
                context.UnitMoveOrderSystem,
                context.SelectionStateCompositionSystemHelper,
                context.TryGetTransportClickedUnitEntity,
                context.TryGetTransportClickedCell);

            if (!TryRefreshCommandBuffers(em, commandEntity, out _, out commandResults))
                return false;

            _transportCommandResultScratch.Clear();
            for (int i = 0; i < commandResults.Length;)
            {
                RtsSelectionCommandResultElement result = commandResults[i];
                if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport &&
                    result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransport &&
                    result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger &&
                    result.Kind != RtsSelectionCommandIntentKind.BoardNearestSoldiers &&
                    result.Kind != RtsSelectionCommandIntentKind.BoardAllSelectedTransport &&
                    result.Kind != RtsSelectionCommandIntentKind.DisembarkTransport &&
                    result.Kind != RtsSelectionCommandIntentKind.DisembarkTransportPassenger)
                {
                    i++;
                    continue;
                }

                commandResults.RemoveAt(i);
                _transportCommandResultScratch.Add(result);
            }

            bool accepted = false;
            for (int i = 0; i < _transportCommandResultScratch.Count; i++)
            {
                RtsSelectionCommandResultElement result = _transportCommandResultScratch[i];
                if (result.Accepted == 0)
                {
                    if (result.HasCommandResult != 0)
                        context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
                    continue;
                }

                accepted = true;
                if (result.Kind == RtsSelectionCommandIntentKind.DisembarkTransport ||
                    result.Kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger)
                {
                    context.ApplyHudCommandResult?.Invoke(ToAcceptedTransportCommandResult(
                        result,
                        result.Kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger
                            ? "Exiting unit."
                            : "Exiting passengers."));
                    continue;
                }

                if (result.Kind == RtsSelectionCommandIntentKind.BoardNearestSoldiers ||
                    result.Kind == RtsSelectionCommandIntentKind.BoardAllSelectedTransport)
                {
                    context.InputSystem.ClearActiveCommandMode();
                    context.SetCameraDragging?.Invoke(false);
                    context.SetHudWorldMarkersVisible?.Invoke(false);
                    context.ClearHudCommandMode?.Invoke();
                    context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
                    continue;
                }

                if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport &&
                    result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransport &&
                    result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger)
                    continue;

                bool clearInputCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Board);
                if (clearInputCommandMode)
                    context.InputSystem.ClearActiveCommandMode();
                context.OrderMarkerSystem.TryShowCommandResultMarker(em, result);
                if (result.EmitScreenMarker != 0)
                    context.RequestMoveOrderScreenMarker?.Invoke(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
                context.SetCameraDragging?.Invoke(false);
                if (IsTransportFirstBoardResult(result))
                    PreserveSelectedTransportAfterBoarding(context, em, result.TargetEntity);
                if (clearInputCommandMode)
                    context.ClearHudCommandMode?.Invoke();
                context.ApplyHudCommandResult?.Invoke(ToAcceptedTransportCommandResult(
                    result,
                    result.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransport ||
                    result.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger
                        ? "Loading transport."
                        : "Boarding transport."));
            }

            return accepted;
        }

        private static TacticalCommandResult ToAcceptedTransportCommandResult(RtsSelectionCommandResultElement result, string fallbackMessage)
        {
            string message = result.Message.ToString();
            return TacticalCommandResult.Success(string.IsNullOrWhiteSpace(message) ? fallbackMessage : message);
        }

        private static bool IsTransportFirstBoardResult(RtsSelectionCommandResultElement result)
        {
            return result.HasTargetEntity != 0 &&
                   (result.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransport ||
                    result.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger);
        }

        private static void PreserveSelectedTransportAfterBoarding(Context context, EntityManager em, Entity transport)
        {
            if (transport == Entity.Null || !em.Exists(transport))
                return;

            context.ClearCurrentSelection?.Invoke(em, "TransportFirstBoardingPreserveTransport");
            if (em.HasComponent<Faction>(transport) &&
                FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(transport).Id) &&
                !em.HasComponent<SelectedUnitTag>(transport))
            {
                em.AddComponent<SelectedUnitTag>(transport);
            }

            context.SelectionStateCompositionSystemHelper.CacheSelectedMoveEntity(em, transport);
            context.SetFocusedUnit?.Invoke(context.SelectionStateCompositionSystemHelper, transport);
            context.ApplyHudSelection?.Invoke(em, transport);
        }

        private static void EnsureFeedbackQueue(Context context)
        {
            if (context.TryGetDefaultEntityManager?.Invoke(out EntityManager defaultEntityManager) == true)
                context.HudFeedbackSystem.EnsureFeedbackQueue(defaultEntityManager);
        }

        private static void DrainResults(
            DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
            RtsSelectionCommandIntentKind kind,
            List<RtsSelectionCommandResultElement> scratch)
        {
            scratch.Clear();
            for (int i = 0; i < commandResults.Length;)
            {
                RtsSelectionCommandResultElement result = commandResults[i];
                if (result.Kind != kind)
                {
                    i++;
                    continue;
                }

                commandResults.RemoveAt(i);
                scratch.Add(result);
            }
        }

        private static TacticalCommandResult ToTacticalCommandResult(RtsSelectionCommandResultElement result)
        {
            string message = result.Message.ToString();
            return result.Accepted != 0
                ? TacticalCommandResult.Success(message)
                : TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode, message);
        }

        internal static bool TryGetImmediateSelectedUnitCommandMode(
            RtsSelectionCommandIntentKind kind,
            out TacticalCommandMode mode)
        {
            switch (kind)
            {
                case RtsSelectionCommandIntentKind.HoldPosition:
                    mode = TacticalCommandMode.Hold;
                    return true;
                case RtsSelectionCommandIntentKind.Stop:
                    mode = TacticalCommandMode.Stop;
                    return true;
                default:
                    mode = TacticalCommandMode.None;
                    return false;
            }
        }

        internal static TacticalCommandResult BuildImmediateSelectedUnitCommandResult(
            RtsSelectionCommandIntentKind kind,
            bool accepted,
            TacticalCommandReasonCode rejectionReason,
            int issuedCount)
        {
            if (!accepted)
                return TacticalCommandResult.Rejected(rejectionReason);

            return kind switch
            {
                RtsSelectionCommandIntentKind.HoldPosition => TacticalCommandResult.Success("Holding current position."),
                RtsSelectionCommandIntentKind.Stop => TacticalCommandResult.Success("Stopped selected units."),
                RtsSelectionCommandIntentKind.ReturnToBase => TacticalCommandResult.Success(
                    issuedCount == 1 ? "Unit returning to base." : $"{issuedCount} units returning to base."),
                RtsSelectionCommandIntentKind.DestroyFocusedUnit => TacticalCommandResult.Success(
                    issuedCount == 1 ? "Destroyed selected unit." : $"Destroyed {issuedCount} selected units."),
                _ => TacticalCommandResult.Success()
            };
        }

        private static Vector2 SelectionPointerPosition(Context context)
        {
            return context.InputSystem != null
                ? context.InputSystem.LastPointerPosition
                : default;
        }

        private static TacticalCommandResult ToScanCommandResult(RtsSelectionCommandResultElement result)
        {
            if (result.Accepted == 0)
                return TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode);

            if (result.DeferredToSource != 0)
                return TacticalCommandResult.Success("SCAN ORDERED: SCANNER EN ROUTE");

            string contacts = result.RevealedCount == 1
                ? "1 CONTACT"
                : $"{result.RevealedCount} CONTACTS";
            return TacticalCommandResult.Success($"SCAN COMPLETE: {contacts}");
        }
    }
}
