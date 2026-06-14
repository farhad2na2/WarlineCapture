using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionCommandResultFlushSystem
{
    public delegate bool TryGetEntityManagerAction(out EntityManager em);
    public delegate void ClearCurrentSelectionAction(EntityManager em, string reason);

    private readonly List<RtsSelectionCommandResultElement> _moveCommandResultScratch = new();
    private readonly List<RtsSelectionCommandResultElement> _attackCommandResultScratch = new();
    private readonly List<RtsSelectionCommandResultElement> _scanCommandResultScratch = new();
    private readonly List<RtsSelectionCommandResultElement> _transportCommandResultScratch = new();
    private readonly List<Entity> _selectedAttackSourceScratch = new();

    public readonly struct Context
    {
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly SelectionHudFeedbackSystem HudFeedbackSystem;
        public readonly SelectionOrderMarkerSystem OrderMarkerSystem;
        public readonly SelectedMoveOrderCommandSystem SelectedMoveOrderCommandSystem;
        public readonly AttackOrderCommandSystem AttackOrderCommandSystem;
        public readonly ScanIntelCommandSystem ScanIntelCommandSystem;
        public readonly TransportBoardingCommandSystem TransportBoardingCommandSystem;
        public readonly UnitMoveOrderSystem UnitMoveOrderSystem;
        public readonly UnitTargetOrderSystem UnitTargetOrderSystem;
        public readonly UnitTransportCapacitySystem UnitTransportCapacitySystem;
        public readonly UnitTransportBoardingQuerySystem UnitTransportBoardingQuerySystem;
        public readonly UnitTransportBoardingRuleSystem UnitTransportBoardingRuleSystem;
        public readonly UnitTransportApproachCellSystem UnitTransportApproachCellSystem;
        public readonly UnitTransportAirPickupSystem UnitTransportAirPickupSystem;
        public readonly UnitTransportRopeDisembarkCommandSystem UnitTransportRopeDisembarkCommandSystem;
        public readonly SelectionStateSystem SelectionStateSystem;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly EntityQuery SelectedMoveQuery;
        public readonly EntityQuery GridConfigQuery;
        public readonly EntityQuery MapSurfaceQuery;
        public readonly TryGetEntityManagerAction TryGetDefaultEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly ClearCurrentSelectionAction ClearCurrentSelection;
        public readonly Action<TacticalCommandMode> ApplyHudCommandMode;
        public readonly Action<TacticalCommandResult> ApplyHudCommandResult;
        public readonly Action ClearHudCommandMode;
        public readonly Action<bool> SetHudWorldMarkersVisible;
        public readonly Action<Vector2> RequestMoveOrderScreenMarker;
        public readonly Action<Vector2> RequestAttackOrderScreenMarker;
        public readonly Action<bool> SetCameraDragging;
        public readonly Action<SelectionStateSystem> ClearFocusedUnit;
        public readonly SelectedMoveOrderCommandSystem.ClickedUnitResolver TryGetMoveClickedUnitEntity;
        public readonly SelectedMoveOrderCommandSystem.ClickedCellResolver TryGetMoveClickedCell;
        public readonly SelectedMoveOrderCommandSystem.ClickedCellResolver TryGetScanClickedCell;
        public readonly AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate TryGetAttackClickedUnitEntity;
        public readonly AttackOrderCommandSystem.CollectSelectedAttackSourcesDelegate CollectSelectedAttackSources;
        public readonly TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate TryGetTransportClickedUnitEntity;
        public readonly TransportBoardingCommandSystem.TryGetClickedCellDelegate TryGetTransportClickedCell;

        public Context(
            RtsSelectionInputSystem inputSystem,
            SelectionHudFeedbackSystem hudFeedbackSystem,
            SelectionOrderMarkerSystem orderMarkerSystem,
            SelectedMoveOrderCommandSystem selectedMoveOrderCommandSystem,
            AttackOrderCommandSystem attackOrderCommandSystem,
            ScanIntelCommandSystem scanIntelCommandSystem,
            TransportBoardingCommandSystem transportBoardingCommandSystem,
            UnitMoveOrderSystem unitMoveOrderSystem,
            UnitTargetOrderSystem unitTargetOrderSystem,
            UnitTransportCapacitySystem unitTransportCapacitySystem,
            UnitTransportBoardingQuerySystem unitTransportBoardingQuerySystem,
            UnitTransportBoardingRuleSystem unitTransportBoardingRuleSystem,
            UnitTransportApproachCellSystem unitTransportApproachCellSystem,
            UnitTransportAirPickupSystem unitTransportAirPickupSystem,
            UnitTransportRopeDisembarkCommandSystem unitTransportRopeDisembarkCommandSystem,
            SelectionStateSystem selectionStateSystem,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            EntityQuery selectedMoveQuery,
            EntityQuery gridConfigQuery,
            EntityQuery mapSurfaceQuery,
            TryGetEntityManagerAction tryGetDefaultEntityManager,
            Action<EntityManager> ensureEntityQueries,
            ClearCurrentSelectionAction clearCurrentSelection,
            Action<TacticalCommandMode> applyHudCommandMode,
            Action<TacticalCommandResult> applyHudCommandResult,
            Action clearHudCommandMode,
            Action<bool> setHudWorldMarkersVisible,
            Action<Vector2> requestMoveOrderScreenMarker,
            Action<Vector2> requestAttackOrderScreenMarker,
            Action<bool> setCameraDragging,
            Action<SelectionStateSystem> clearFocusedUnit,
            SelectedMoveOrderCommandSystem.ClickedUnitResolver tryGetMoveClickedUnitEntity,
            SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetMoveClickedCell,
            SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetScanClickedCell,
            AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate tryGetAttackClickedUnitEntity,
            AttackOrderCommandSystem.CollectSelectedAttackSourcesDelegate collectSelectedAttackSources,
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
            UnitTargetOrderSystem = unitTargetOrderSystem;
            UnitTransportCapacitySystem = unitTransportCapacitySystem;
            UnitTransportBoardingQuerySystem = unitTransportBoardingQuerySystem;
            UnitTransportBoardingRuleSystem = unitTransportBoardingRuleSystem;
            UnitTransportApproachCellSystem = unitTransportApproachCellSystem;
            UnitTransportAirPickupSystem = unitTransportAirPickupSystem;
            UnitTransportRopeDisembarkCommandSystem = unitTransportRopeDisembarkCommandSystem;
            SelectionStateSystem = selectionStateSystem;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            SelectedMoveQuery = selectedMoveQuery;
            GridConfigQuery = gridConfigQuery;
            MapSurfaceQuery = mapSurfaceQuery;
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            ClearCurrentSelection = clearCurrentSelection;
            ApplyHudCommandMode = applyHudCommandMode;
            ApplyHudCommandResult = applyHudCommandResult;
            ClearHudCommandMode = clearHudCommandMode;
            SetHudWorldMarkersVisible = setHudWorldMarkersVisible;
            RequestMoveOrderScreenMarker = requestMoveOrderScreenMarker;
            RequestAttackOrderScreenMarker = requestAttackOrderScreenMarker;
            SetCameraDragging = setCameraDragging;
            ClearFocusedUnit = clearFocusedUnit;
            TryGetMoveClickedUnitEntity = tryGetMoveClickedUnitEntity;
            TryGetMoveClickedCell = tryGetMoveClickedCell;
            TryGetScanClickedCell = tryGetScanClickedCell;
            TryGetAttackClickedUnitEntity = tryGetAttackClickedUnitEntity;
            CollectSelectedAttackSources = collectSelectedAttackSources;
            TryGetTransportClickedUnitEntity = tryGetTransportClickedUnitEntity;
            TryGetTransportClickedCell = tryGetTransportClickedCell;
        }
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
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"processMoveCommandRequestsEnter frame={Time.frameCount}");

        if (!context.InputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"processMoveCommandRequestsNoBuffers frame={Time.frameCount}");
            context.ClearHudCommandMode?.Invoke();
            context.InputSystem.ClearActiveCommandMode();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return;
        }

        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"processMoveCommandRequestsBuffers commandEntity={commandEntity} totalRequests={commandRequests.Length} " +
                $"moveRequests={CountRequests(commandRequests, RtsSelectionCommandIntentKind.Move)} resultBuffer={commandResults.Length} frame={Time.frameCount}");
        }

        context.EnsureEntityQueries?.Invoke(em);
        context.SelectedMoveOrderCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.SelectedMoveQuery,
            context.GridConfigQuery,
            context.MapSurfaceQuery,
            context.SelectionStateSystem?.CachedSelectedMoveEntities,
            context.UnitMoveOrderSystem,
            context.OrderMarkerSystem,
            context.TryGetMoveClickedUnitEntity,
            context.TryGetMoveClickedCell);

        commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DrainResults(commandResults, RtsSelectionCommandIntentKind.Move, _moveCommandResultScratch);
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"processMoveCommandRequestsDrained results={_moveCommandResultScratch.Count} remainingResultBuffer={commandResults.Length} frame={Time.frameCount}");
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
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"processMoveCommandRequestsUnhandled frame={Time.frameCount}");
            context.ClearHudCommandMode?.Invoke();
            context.InputSystem.ClearActiveCommandMode();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
        }
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

        if (!context.InputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            if (explicitAttackTargetModeActive)
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        context.AttackOrderCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.UnitTargetOrderSystem,
            context.TryGetAttackClickedUnitEntity,
            context.CollectSelectedAttackSources,
            context.BuildingPlacementInteractionSystem,
            context.BuildingPlacementInteractionContext,
            _selectedAttackSourceScratch);

        commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
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
            if (result.HasWorldPosition != 0)
            {
                if (result.HasTargetEntity != 0)
                    context.OrderMarkerSystem.ShowAttackOrderMarker(em, result.TargetEntity, result.WorldPosition, 6f);
                else
                    context.OrderMarkerSystem.ShowAttackOrderMarker(em, result.WorldPosition, 6f);
            }
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

    public bool ProcessScanCommandRequests(Context context)
    {
        EnsureFeedbackQueue(context);

        if (!context.InputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.ScanUnavailable));
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        context.ScanIntelCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.GridConfigQuery,
            context.TryGetScanClickedCell);

        commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DrainResults(commandResults, RtsSelectionCommandIntentKind.Scan, _scanCommandResultScratch);

        bool issued = false;
        for (int i = 0; i < _scanCommandResultScratch.Count; i++)
        {
            RtsSelectionCommandResultElement result = _scanCommandResultScratch[i];
            if (result.Accepted == 0)
            {
                if (result.HasCommandResult != 0)
                    context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
                continue;
            }

            bool clearInputCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Scan);
            if (clearInputCommandMode)
                context.InputSystem.ClearActiveCommandMode();
            if (result.HasWorldPosition != 0)
                context.OrderMarkerSystem.ShowScanOrderMarker(em, result.TargetCell, result.WorldPosition, result.RadiusCells);
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

        if (!context.InputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            return false;
        }

        context.EnsureEntityQueries?.Invoke(em);
        context.TransportBoardingCommandSystem.ProcessCommandIntentRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.UnitTransportCapacitySystem,
            context.UnitTransportBoardingQuerySystem,
            context.UnitTransportBoardingRuleSystem,
            context.UnitTransportApproachCellSystem,
            context.UnitTransportAirPickupSystem,
            context.UnitTransportRopeDisembarkCommandSystem,
            context.UnitMoveOrderSystem,
            context.SelectionStateSystem,
            context.TryGetTransportClickedUnitEntity,
            context.TryGetTransportClickedCell);

        commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        _transportCommandResultScratch.Clear();
        for (int i = 0; i < commandResults.Length;)
        {
            RtsSelectionCommandResultElement result = commandResults[i];
            if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport &&
                result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransport &&
                result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger &&
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
                context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Success(
                    result.Kind == RtsSelectionCommandIntentKind.DisembarkTransportPassenger
                        ? "Exiting unit."
                        : "Exiting passengers."));
                continue;
            }

            if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport &&
                result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransport &&
                result.Kind != RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger)
                continue;

            bool clearInputCommandMode = context.InputSystem.ShouldClearActiveCommandModeAfterCommand(TacticalCommandMode.Board);
            if (clearInputCommandMode)
                context.InputSystem.ClearActiveCommandMode();
            if (result.HasTargetCell != 0 && result.HasWorldPosition != 0)
            {
                context.OrderMarkerSystem.ShowMoveOrderMarker(
                    em,
                    result.TargetCell,
                    result.WorldPosition,
                    result.MarkerFactionId);
            }
            if (result.EmitScreenMarker != 0)
                context.RequestMoveOrderScreenMarker?.Invoke(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
            context.SetCameraDragging?.Invoke(false);
            if (clearInputCommandMode)
                context.ClearHudCommandMode?.Invoke();
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Success(
                result.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransport ||
                result.Kind == RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger
                    ? "Loading transport."
                    : "Boarding transport."));
        }

        return accepted;
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

    private static TacticalCommandResult ToScanCommandResult(RtsSelectionCommandResultElement result)
    {
        if (result.Accepted == 0)
            return TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode);

        string contacts = result.RevealedCount == 1
            ? "1 CONTACT"
            : $"{result.RevealedCount} CONTACTS";
        return TacticalCommandResult.Success($"SCAN COMPLETE: {contacts}");
    }
}
