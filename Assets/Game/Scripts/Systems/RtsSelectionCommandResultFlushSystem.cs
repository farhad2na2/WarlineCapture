using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RtsSelectionCommandResultFlushSystem
{
    public delegate bool TryGetEntityManagerAction(out EntityManager em);
    public delegate void ClearCurrentSelectionAction(EntityManager em, string reason);

    private readonly List<RtsSelectionCommandResultElement> _moveCommandResultScratch = new();
    private readonly List<RtsSelectionCommandResultElement> _attackCommandResultScratch = new();
    private readonly List<RtsSelectionCommandResultElement> _transportCommandResultScratch = new();

    public readonly struct Context
    {
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly SelectionHudFeedbackSystem HudFeedbackSystem;
        public readonly SelectionOrderMarkerSystem OrderMarkerSystem;
        public readonly SelectionMoveCommandRequestSystem MoveCommandRequestSystem;
        public readonly SelectionAttackCommandRequestSystem AttackCommandRequestSystem;
        public readonly SelectionTransportCommandRequestSystem TransportCommandRequestSystem;
        public readonly SelectedMoveOrderCommandSystem SelectedMoveOrderCommandSystem;
        public readonly AttackOrderCommandSystem AttackOrderCommandSystem;
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
        public readonly TryGetEntityManagerAction TryGetDefaultEntityManager;
        public readonly Action<EntityManager> EnsureEntityQueries;
        public readonly ClearCurrentSelectionAction ClearCurrentSelection;
        public readonly Action<TacticalCommandResult> ApplyHudCommandResult;
        public readonly Action ClearHudCommandMode;
        public readonly Action<bool> SetHudWorldMarkersVisible;
        public readonly Action<Vector2> RequestMoveOrderScreenMarker;
        public readonly Action<Vector2> RequestAttackOrderScreenMarker;
        public readonly Action<bool> SetCameraDragging;
        public readonly Action<SelectionStateSystem> ClearFocusedUnit;
        public readonly SelectedMoveOrderCommandSystem.ClickedUnitResolver TryGetMoveClickedUnitEntity;
        public readonly SelectedMoveOrderCommandSystem.ClickedCellResolver TryGetMoveClickedCell;
        public readonly AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate TryGetAttackClickedUnitEntity;
        public readonly TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate TryGetTransportClickedUnitEntity;
        public readonly TransportBoardingCommandSystem.TryGetClickedCellDelegate TryGetTransportClickedCell;

        public Context(
            RtsSelectionInputSystem inputSystem,
            SelectionHudFeedbackSystem hudFeedbackSystem,
            SelectionOrderMarkerSystem orderMarkerSystem,
            SelectionMoveCommandRequestSystem moveCommandRequestSystem,
            SelectionAttackCommandRequestSystem attackCommandRequestSystem,
            SelectionTransportCommandRequestSystem transportCommandRequestSystem,
            SelectedMoveOrderCommandSystem selectedMoveOrderCommandSystem,
            AttackOrderCommandSystem attackOrderCommandSystem,
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
            TryGetEntityManagerAction tryGetDefaultEntityManager,
            Action<EntityManager> ensureEntityQueries,
            ClearCurrentSelectionAction clearCurrentSelection,
            Action<TacticalCommandResult> applyHudCommandResult,
            Action clearHudCommandMode,
            Action<bool> setHudWorldMarkersVisible,
            Action<Vector2> requestMoveOrderScreenMarker,
            Action<Vector2> requestAttackOrderScreenMarker,
            Action<bool> setCameraDragging,
            Action<SelectionStateSystem> clearFocusedUnit,
            SelectedMoveOrderCommandSystem.ClickedUnitResolver tryGetMoveClickedUnitEntity,
            SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetMoveClickedCell,
            AttackOrderCommandSystem.TryGetClickedUnitEntityDelegate tryGetAttackClickedUnitEntity,
            TransportBoardingCommandSystem.TryGetClickedUnitEntityDelegate tryGetTransportClickedUnitEntity,
            TransportBoardingCommandSystem.TryGetClickedCellDelegate tryGetTransportClickedCell)
        {
            InputSystem = inputSystem;
            HudFeedbackSystem = hudFeedbackSystem;
            OrderMarkerSystem = orderMarkerSystem;
            MoveCommandRequestSystem = moveCommandRequestSystem;
            AttackCommandRequestSystem = attackCommandRequestSystem;
            TransportCommandRequestSystem = transportCommandRequestSystem;
            SelectedMoveOrderCommandSystem = selectedMoveOrderCommandSystem;
            AttackOrderCommandSystem = attackOrderCommandSystem;
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
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            ClearCurrentSelection = clearCurrentSelection;
            ApplyHudCommandResult = applyHudCommandResult;
            ClearHudCommandMode = clearHudCommandMode;
            SetHudWorldMarkersVisible = setHudWorldMarkersVisible;
            RequestMoveOrderScreenMarker = requestMoveOrderScreenMarker;
            RequestAttackOrderScreenMarker = requestAttackOrderScreenMarker;
            SetCameraDragging = setCameraDragging;
            ClearFocusedUnit = clearFocusedUnit;
            TryGetMoveClickedUnitEntity = tryGetMoveClickedUnitEntity;
            TryGetMoveClickedCell = tryGetMoveClickedCell;
            TryGetAttackClickedUnitEntity = tryGetAttackClickedUnitEntity;
            TryGetTransportClickedUnitEntity = tryGetTransportClickedUnitEntity;
            TryGetTransportClickedCell = tryGetTransportClickedCell;
        }
    }

    public void UpdateOrderMarkerVisibility(Context context)
    {
        context.OrderMarkerSystem.UpdateMoveOrderMarkerVisibility(context.SetHudWorldMarkersVisible);
        context.OrderMarkerSystem.UpdateAttackOrderMarkerVisibility(context.SetHudWorldMarkersVisible);
    }

    public void ProcessMoveCommandRequests(Context context)
    {
        EnsureFeedbackQueue(context);

        if (!context.InputSystem.TryGetCommandBuffers(
                out EntityManager em,
                out Entity commandEntity,
                out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                out DynamicBuffer<RtsSelectionCommandResultElement> commandResults))
        {
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            context.ClearHudCommandMode?.Invoke();
            return;
        }

        context.EnsureEntityQueries?.Invoke(em);
        context.MoveCommandRequestSystem.ProcessPendingRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.SelectedMoveQuery,
            context.GridConfigQuery,
            context.UnitMoveOrderSystem,
            context.OrderMarkerSystem,
            context.SelectedMoveOrderCommandSystem,
            context.TryGetMoveClickedUnitEntity,
            context.TryGetMoveClickedCell);

        commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DrainResults(commandResults, RtsSelectionCommandIntentKind.Move, _moveCommandResultScratch);

        bool handled = false;
        for (int i = 0; i < _moveCommandResultScratch.Count; i++)
        {
            RtsSelectionCommandResultElement result = _moveCommandResultScratch[i];
            context.ApplyHudCommandResult?.Invoke(ToTacticalCommandResult(result));
            context.ClearHudCommandMode?.Invoke();
            if (result.EmitScreenMarker != 0)
                context.RequestMoveOrderScreenMarker?.Invoke(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
            if (result.ShowWorldMarkers != 0)
                context.SetHudWorldMarkersVisible?.Invoke(true);
            handled = true;
        }

        if (!handled)
        {
            context.ApplyHudCommandResult?.Invoke(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));
            context.ClearHudCommandMode?.Invoke();
        }
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
        context.AttackCommandRequestSystem.ProcessPendingRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.AttackOrderCommandSystem,
            context.UnitTargetOrderSystem,
            context.TryGetAttackClickedUnitEntity,
            context.BuildingPlacementInteractionSystem,
            context.BuildingPlacementInteractionContext);

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

            if (result.HasWorldPosition != 0)
                context.OrderMarkerSystem.ShowAttackOrderMarker(em, result.WorldPosition);
            if (result.EmitScreenMarker != 0)
                context.RequestAttackOrderScreenMarker?.Invoke(new Vector2(result.ScreenPosition.x, result.ScreenPosition.y));
            context.ClearCurrentSelection?.Invoke(em, "AttackOrderIssued");
            context.ClearFocusedUnit?.Invoke(context.SelectionStateSystem);
            context.SetCameraDragging?.Invoke(false);
            context.ClearHudCommandMode?.Invoke();
            if (result.ShowWorldMarkers != 0)
                context.SetHudWorldMarkersVisible?.Invoke(true);
            issued = true;
        }

        return issued;
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
        context.TransportCommandRequestSystem.ProcessPendingRequests(
            em,
            commandEntity,
            commandRequests,
            commandResults,
            context.TransportBoardingCommandSystem,
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
                result.Kind != RtsSelectionCommandIntentKind.DisembarkTransport)
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
                continue;

            accepted = true;
            if (result.Kind != RtsSelectionCommandIntentKind.BoardTransport)
                continue;

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
            context.ClearCurrentSelection?.Invoke(em, "BoardTransportOrderIssued");
            context.ClearFocusedUnit?.Invoke(context.SelectionStateSystem);
            context.SetCameraDragging?.Invoke(false);
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
        return result.Accepted != 0
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode);
    }
}
