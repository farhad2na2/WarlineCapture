using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectionHudFeedbackBoundary
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager em);
    public delegate Sprite ResolveSelectionPortraitSpriteDelegate(EntityManager em, Entity entity);
    public delegate void EnsureEntityQueriesDelegate(EntityManager em);
    public delegate void RefreshFocusedUnitDelegate(EntityManager em, SelectionStateSystem selectionStateSystem);
    public delegate bool TryGetAttackModeOrderSnapshotDelegate(out string orderText);
    public delegate bool IsBoardCommandAvailableDelegate(EntityManager em, Entity entity);
    public delegate bool HasSelectedBoardActionDelegate(EntityManager em);

    public readonly struct Context
    {
        public readonly SelectionUiQuerySystem SelectionUiQuerySystem;
        public readonly TryGetEntityManagerDelegate TryGetDefaultEntityManager;
        public readonly ResolveSelectionPortraitSpriteDelegate ResolveSelectionPortraitSprite;

        public Context(
            SelectionUiQuerySystem selectionUiQuerySystem,
            TryGetEntityManagerDelegate tryGetDefaultEntityManager,
            ResolveSelectionPortraitSpriteDelegate resolveSelectionPortraitSprite = null)
        {
            SelectionUiQuerySystem = selectionUiQuerySystem;
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
            ResolveSelectionPortraitSprite = resolveSelectionPortraitSprite;
        }
    }

    private IBattleHudRuntimeFeedbackView _battleHudView;
    private IMatchHudSelectionPanelView _matchHudSelectionPanelView;
    private World _queryWorld;
    private EntityQuery _feedbackQuery;

    public void ResetViewCache()
    {
        _battleHudView = null;
    }

    public void BindMatchHudSelectionPanel(IMatchHudSelectionPanelView view)
    {
        _matchHudSelectionPanelView = view;
    }

    public void BindBattleHudRuntimeFeedback(IBattleHudRuntimeFeedbackView view)
    {
        _battleHudView = view;
    }

    public Entity EnsureFeedbackQueue(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld != world || world == null || !world.IsCreated)
        {
            _queryWorld = world;
            _feedbackQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectionHudFeedbackQueueComponent>(),
                ComponentType.ReadWrite<SelectionHudFeedbackElement>());
        }

        if (!_feedbackQuery.IsEmptyIgnoreFilter)
            return _feedbackQuery.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(SelectionHudFeedbackQueueComponent));
        em.SetName(entity, "SelectionHudFeedbackQueue");
        em.AddBuffer<SelectionHudFeedbackElement>(entity);
        return entity;
    }

    public void QueueSelection(EntityManager em, Entity entity, SelectionUiQuerySystem selectionUiQuerySystem)
    {
        if (entity == Entity.Null || !em.Exists(entity))
        {
            QueueClearSelection(em);
            return;
        }

        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.Selection,
            Label = ToFixed64(selectionUiQuerySystem.ResolveFocusedUnitName(em, entity)),
            Status = ToFixed64(selectionUiQuerySystem.ResolveHudSelectionStatus(em, entity))
        });
    }

    public void QueueSquadSelection(EntityManager em, int selectedCount)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        if (selectedCount <= 0)
        {
            feedback.Add(new SelectionHudFeedbackElement { Kind = SelectionHudFeedbackKind.ClearSelection });
            return;
        }

        string unitLabel = selectedCount == 1 ? "UNIT" : "UNITS";
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.SquadSelection,
            Label = ToFixed64($"{selectedCount} {unitLabel}"),
            Status = ToFixed64("SQUAD SELECTED"),
            Count = selectedCount
        });
    }

    public void QueueClearSelection(EntityManager em)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement { Kind = SelectionHudFeedbackKind.ClearSelection });
    }

    public void QueueCommandMode(EntityManager em, TacticalCommandMode mode)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.CommandMode,
            CommandMode = (int)mode
        });
    }

    public void QueueClearCommandMode(EntityManager em)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement { Kind = SelectionHudFeedbackKind.ClearCommandMode });
    }

    public void QueueCommandResult(EntityManager em, TacticalCommandResult result)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.CommandResult,
            CommandAccepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)result.ReasonCode,
            Message = ToFixed64(result.Message)
        });
    }

    public void QueueWorldMarkersVisible(EntityManager em, bool visible)
    {
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(EnsureFeedbackQueue(em));
        feedback.Add(new SelectionHudFeedbackElement
        {
            Kind = SelectionHudFeedbackKind.WorldMarkersVisible,
            Visible = visible ? (byte)1 : (byte)0
        });
    }

    public void ProcessPendingFeedback(EntityManager em)
    {
        Entity entity = EnsureFeedbackQueue(em);
        DynamicBuffer<SelectionHudFeedbackElement> feedback = em.GetBuffer<SelectionHudFeedbackElement>(entity);
        if (feedback.Length == 0)
            return;

        IBattleHudRuntimeFeedbackView view = ResolveBattleHudView();
        if (view == null)
        {
            feedback.Clear();
            return;
        }

        for (int i = 0; i < feedback.Length; i++)
            ApplyFeedback(view, feedback[i]);
        feedback.Clear();
    }

    public void ApplySelection(EntityManager em, Entity entity, SelectionUiQuerySystem selectionUiQuerySystem)
    {
        bool validSelection = entity != Entity.Null && em.Exists(entity);
        QueueSelection(em, entity, selectionUiQuerySystem);
        ProcessPendingFeedback(em);
        _matchHudSelectionPanelView?.SetSelectionVisible(validSelection);
    }

    public void ApplySelection(Context context, EntityManager em, Entity entity)
    {
        Sprite portraitSprite = context.ResolveSelectionPortraitSprite?.Invoke(em, entity);
        ApplySelection(em, entity, context.SelectionUiQuerySystem, portraitSprite);
    }

    private void ApplySelection(EntityManager em, Entity entity, SelectionUiQuerySystem selectionUiQuerySystem, Sprite portraitSprite)
    {
        bool validSelection = entity != Entity.Null && em.Exists(entity);
        QueueSelection(em, entity, selectionUiQuerySystem);
        ProcessPendingFeedback(em);
        _matchHudSelectionPanelView?.SetSelectionVisible(validSelection, portraitSprite);
    }

    public void ApplySquadSelection(EntityManager em, int selectedCount)
    {
        QueueSquadSelection(em, selectedCount);
        ProcessPendingFeedback(em);
        _matchHudSelectionPanelView?.SetSelectionVisible(selectedCount > 0);
    }

    public void ApplySquadSelection(Context context, int selectedCount)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ApplySquadSelection(em, selectedCount);
    }

    public void ApplyBuildingSelection(Sprite portraitSprite)
    {
        _matchHudSelectionPanelView?.SetSelectionVisible(true, portraitSprite);
    }

    public void RefreshFocusedSelectionReadModels(
        Context context,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitUiReadModelSystem focusedUnitUiReadModelSystem,
        UnitTransportCapacitySystem unitTransportCapacitySystem,
        EnsureEntityQueriesDelegate ensureEntityQueries,
        RefreshFocusedUnitDelegate refreshFocusedUnit,
        float timeSeconds)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ensureEntityQueries?.Invoke(em);
        refreshFocusedUnit?.Invoke(em, selectionStateSystem);
        focusedUnitUiReadModelSystem?.Publish(
            em,
            selectionStateSystem,
            context.SelectionUiQuerySystem,
            unitTransportCapacitySystem,
            timeSeconds);
    }

    public void UpdateMatchHudSelectionPanel(
        Context context,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
        FocusedUnitUiReadModelSystem focusedUnitUiReadModelSystem,
        SelectionSummaryQuerySystem selectionSummaryQuerySystem,
        List<MatchHudSelectionPanelPassengerItemModel> transportPassengerPanelItems,
        EnsureEntityQueriesDelegate ensureEntityQueries,
        TryGetAttackModeOrderSnapshotDelegate tryGetAttackModeOrderSnapshot,
        ResolveSelectionPortraitSpriteDelegate resolveSelectionCardPortraitSprite,
        System.Func<Sprite> resolveSelectedBuildingPortraitSprite,
        System.Func<Sprite> resolveActiveSquadTrayPortraitSprite,
        System.Func<bool> hasSelectedBuilding,
        System.Func<string> selectedBuildingLabel,
        IsBoardCommandAvailableDelegate isBoardCommandAvailable,
        HasSelectedBoardActionDelegate hasSelectedBoardAction)
    {
        if (_matchHudSelectionPanelView == null)
            return;

        if (!TryGetDefaultEntityManager(context, out EntityManager em))
        {
            ApplySelectionPanelHidden();
            return;
        }

        ensureEntityQueries?.Invoke(em);
        int selectedCount = CountSelectedTags(em);
        if (selectedCount > 1)
        {
            _matchHudSelectionPanelView.Apply(BuildSquadPanelModel(
                context,
                em,
                selectedCount,
                selectionSummaryQuerySystem,
                tryGetAttackModeOrderSnapshot,
                resolveActiveSquadTrayPortraitSprite,
                hasSelectedBuilding,
                hasSelectedBoardAction));
            _matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
            return;
        }

        if (focusedUnitLifecycleSystem != null &&
            focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
            em.Exists(focusedUnit))
        {
            _matchHudSelectionPanelView.Apply(BuildFocusedUnitPanelModel(
                context,
                em,
                focusedUnit,
                tryGetAttackModeOrderSnapshot,
                isBoardCommandAvailable));
            _matchHudSelectionPanelView.ApplyTransportPassengers(BuildTransportPassengersPanelModel(
                context,
                em,
                focusedUnit,
                focusedUnitUiReadModelSystem,
                transportPassengerPanelItems,
                resolveSelectionCardPortraitSprite));
            return;
        }

        if (selectedCount > 0)
        {
            _matchHudSelectionPanelView.Apply(BuildSquadPanelModel(
                context,
                em,
                selectedCount,
                selectionSummaryQuerySystem,
                tryGetAttackModeOrderSnapshot,
                resolveActiveSquadTrayPortraitSprite,
                hasSelectedBuilding,
                hasSelectedBoardAction));
            _matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
            return;
        }

        if (hasSelectedBuilding != null && hasSelectedBuilding())
        {
            _matchHudSelectionPanelView.Apply(BuildSelectedBuildingPanelModel(
                selectedBuildingLabel,
                resolveSelectedBuildingPortraitSprite));
            _matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
            return;
        }

        ApplySelectionPanelHidden();
    }

    public string ResolveCurrentSelectionOrderTextSnapshot(
        Context context,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleSystem focusedUnitLifecycleSystem,
        SelectionSummaryQuerySystem selectionSummaryQuerySystem,
        EnsureEntityQueriesDelegate ensureEntityQueries,
        System.Func<bool> hasSelectedBuilding)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return "Idle";

        ensureEntityQueries?.Invoke(em);
        if (focusedUnitLifecycleSystem != null &&
            focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
            em.Exists(focusedUnit))
        {
            return ResolveFocusedUnitOrderText(
                em,
                focusedUnit,
                context.SelectionUiQuerySystem);
        }

        int selectedCount = CountSelectedTags(em);
        if (selectedCount > 0)
        {
            bool includeSelectedBuilding = hasSelectedBuilding != null && hasSelectedBuilding();
            return selectionSummaryQuerySystem.BuildSelectedSummary(
                em,
                context.SelectionUiQuerySystem,
                includeSelectedBuilding).OrderText;
        }

        if (hasSelectedBuilding != null && hasSelectedBuilding())
            return "Structure selected";

        return "Idle";
    }

    private static int CountSelectedTags(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        return query.CalculateEntityCount();
    }

    private void ApplySelectionPanelHidden()
    {
        _matchHudSelectionPanelView?.Apply(MatchHudSelectionPanelModel.Hidden);
        _matchHudSelectionPanelView?.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
    }

    private MatchHudSelectionPanelModel BuildFocusedUnitPanelModel(
        Context context,
        EntityManager em,
        Entity entity,
        TryGetAttackModeOrderSnapshotDelegate tryGetAttackModeOrderSnapshot,
        IsBoardCommandAvailableDelegate isBoardCommandAvailable)
    {
        Sprite portraitSprite = context.ResolveSelectionPortraitSprite?.Invoke(em, entity);
        portraitSprite ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad);
        bool owned = context.SelectionUiQuerySystem.IsOwnedByPlayer(em, entity);
        bool movable = em.HasComponent<UnitMove>(entity);
        bool vehicle = context.SelectionUiQuerySystem.IsVehicleForVisibleSelection(em, entity);
        TryGetHealthModel(context, em, entity, out string healthLabel, out float health01);
        string orderText = ResolveFocusedUnitOrderText(em, entity, context.SelectionUiQuerySystem);
        if (tryGetAttackModeOrderSnapshot != null &&
            tryGetAttackModeOrderSnapshot(out string attackModeOrderText))
        {
            orderText = attackModeOrderText;
        }

        return new MatchHudSelectionPanelModel(
            true,
            context.SelectionUiQuerySystem.ResolveFocusedUnitName(em, entity),
            context.SelectionUiQuerySystem.ResolveFocusedUnitDescription(em, entity),
            orderText,
            healthLabel,
            health01,
            portraitSprite,
            !vehicle,
            null,
            owned && movable && !em.HasComponent<UnitTransportPassenger>(entity),
            owned,
            isBoardCommandAvailable != null && isBoardCommandAvailable(em, entity));
    }

    private MatchHudTransportPassengersModel BuildTransportPassengersPanelModel(
        Context context,
        EntityManager em,
        Entity transport,
        FocusedUnitUiReadModelSystem focusedUnitUiReadModelSystem,
        List<MatchHudSelectionPanelPassengerItemModel> transportPassengerPanelItems,
        ResolveSelectionPortraitSpriteDelegate resolveSelectionCardPortraitSprite)
    {
        transportPassengerPanelItems?.Clear();
        if (focusedUnitUiReadModelSystem == null ||
            transportPassengerPanelItems == null ||
            !focusedUnitUiReadModelSystem.TryRead(
                em,
                out FocusedUnitUiReadModelComponent focusedModel,
                out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers) ||
            focusedModel.HasFocusedUnit == 0 ||
            focusedModel.FocusedUnit != transport ||
            focusedModel.OwnedByPlayer == 0 ||
            focusedModel.TransportPassengerCapacity <= 0)
        {
            return MatchHudTransportPassengersModel.Hidden;
        }

        int capacity = math.max(0, focusedModel.TransportPassengerCapacity);
        if (capacity <= 0)
            return MatchHudTransportPassengersModel.Hidden;

        for (int i = 0; i < passengers.Length; i++)
        {
            FocusedUnitPassengerUiReadModelElement passengerModel = passengers[i];
            Entity passenger = passengerModel.Passenger;
            if (!em.Exists(passenger))
                continue;

            BuildHealthModelFromValues(
                passengerModel.HealthCurrent,
                passengerModel.HealthMax,
                out string healthLabel,
                out float health01);
            Sprite portrait = resolveSelectionCardPortraitSprite?.Invoke(em, passenger);
            portrait ??= context.ResolveSelectionPortraitSprite?.Invoke(em, passenger);
            portrait ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers);
            transportPassengerPanelItems.Add(new MatchHudSelectionPanelPassengerItemModel(
                ToUiHandle(passenger),
                passengerModel.DisplayName.ToString(),
                ResolvePassengerRoleText(context, em, passenger),
                healthLabel,
                health01,
                portrait,
                true));
        }

        return new MatchHudTransportPassengersModel(
            true,
            false,
            ToUiHandle(transport),
            transportPassengerPanelItems.Count,
            capacity,
            transportPassengerPanelItems.Count > 0,
            transportPassengerPanelItems);
    }

    private string ResolvePassengerRoleText(Context context, EntityManager em, Entity passenger)
    {
        if (!em.Exists(passenger))
            return "UNIT";

        if (context.SelectionUiQuerySystem.IsVehicleForVisibleSelection(em, passenger))
            return "VEHICLE";

        return "SOLDIER";
    }

    private MatchHudSelectionPanelModel BuildSquadPanelModel(
        Context context,
        EntityManager em,
        int selectedCount,
        SelectionSummaryQuerySystem selectionSummaryQuerySystem,
        TryGetAttackModeOrderSnapshotDelegate tryGetAttackModeOrderSnapshot,
        System.Func<Sprite> resolveActiveSquadTrayPortraitSprite,
        System.Func<bool> hasSelectedBuilding,
        HasSelectedBoardActionDelegate hasSelectedBoardAction)
    {
        bool includeSelectedBuilding = hasSelectedBuilding != null && hasSelectedBuilding();
        SelectionSummaryQuerySystem.Summary summary = selectionSummaryQuerySystem.BuildSelectedSummary(
            em,
            context.SelectionUiQuerySystem,
            includeSelectedBuilding);
        string orderText = tryGetAttackModeOrderSnapshot != null &&
                           tryGetAttackModeOrderSnapshot(out string attackModeOrderText)
            ? attackModeOrderText
            : summary.OrderText;
        Sprite portraitSprite = _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(summary.PortraitKind);
        portraitSprite ??= resolveActiveSquadTrayPortraitSprite?.Invoke();
        portraitSprite ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad);
        return new MatchHudSelectionPanelModel(
            true,
            summary.Title,
            summary.Subtitle,
            orderText,
            summary.HealthText,
            summary.Health01,
            portraitSprite,
            summary.PortraitKind,
            false,
            null,
            selectedCount > 0,
            selectedCount > 0,
            hasSelectedBoardAction != null && hasSelectedBoardAction(em));
    }

    private MatchHudSelectionPanelModel BuildSelectedBuildingPanelModel(
        System.Func<string> selectedBuildingLabel,
        System.Func<Sprite> resolveSelectedBuildingPortraitSprite)
    {
        string label = selectedBuildingLabel?.Invoke();
        Sprite portraitSprite = resolveSelectedBuildingPortraitSprite?.Invoke();
        portraitSprite ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Buildings);
        return new MatchHudSelectionPanelModel(
            true,
            string.IsNullOrWhiteSpace(label) ? "Selected Building" : label,
            "Base Structure",
            "Structure selected",
            "-",
            0f,
            portraitSprite,
            false,
            null,
            false,
            true,
            false);
    }

    internal static string ResolveFocusedUnitOrderText(
        EntityManager em,
        Entity entity,
        SelectionUiQuerySystem selectionUiQuerySystem)
    {
        if (em.HasComponent<UnitTransportPassenger>(entity))
            return "In transport";
        if (em.HasComponent<UnitTransportBoardingTarget>(entity))
            return "Boarding transport";

        return selectionUiQuerySystem.GetFocusedUnitUiStatus(em, entity) switch
        {
            SelectionUiQuerySystem.FocusedUnitUiStatus.ReturningToBase => "Returning to base",
            SelectionUiQuerySystem.FocusedUnitUiStatus.MissileLaunched => "Missile launched",
            SelectionUiQuerySystem.FocusedUnitUiStatus.AirspaceClear => "Airspace clear",
            SelectionUiQuerySystem.FocusedUnitUiStatus.TrackingAirTarget => "Tracking air target",
            SelectionUiQuerySystem.FocusedUnitUiStatus.InterceptingMissile => "Intercepting missile",
            SelectionUiQuerySystem.FocusedUnitUiStatus.AirDefenseReloading => "Reloading",
            SelectionUiQuerySystem.FocusedUnitUiStatus.Engaged => "Engaging target",
            SelectionUiQuerySystem.FocusedUnitUiStatus.Moving => "Moving",
            _ => "Idle"
        };
    }

    private static void TryGetHealthModel(
        Context context,
        EntityManager em,
        Entity entity,
        out string healthLabel,
        out float health01)
    {
        if (!context.SelectionUiQuerySystem.TryGetFocusedUnitHealth(em, entity, out int current, out int max) || max <= 0)
        {
            healthLabel = "Health: -";
            health01 = 0f;
            return;
        }

        BuildHealthModelFromValues(current, max, out healthLabel, out health01);
    }

    private static void BuildHealthModelFromValues(int current, int max, out string healthLabel, out float health01)
    {
        if (max <= 0)
        {
            healthLabel = "Health: -";
            health01 = 0f;
            return;
        }

        healthLabel = $"Health: {math.max(0, current)}/{max}";
        health01 = math.saturate((float)current / max);
    }

    private static UiEntityHandle ToUiHandle(Entity entity)
    {
        return entity == Entity.Null
            ? UiEntityHandle.Null
            : new UiEntityHandle(entity.Index, entity.Version);
    }

    public void ClearSelection(EntityManager em)
    {
        QueueClearSelection(em);
        ProcessPendingFeedback(em);
        _matchHudSelectionPanelView?.SetSelectionVisible(false);
    }

    public void ClearSelection(Context context)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ClearSelection(em);
    }

    public void ApplyCommandMode(EntityManager em, TacticalCommandMode mode)
    {
        QueueCommandMode(em, mode);
        ProcessPendingFeedback(em);
        _matchHudSelectionPanelView?.SetBoardActionSelected(mode == TacticalCommandMode.Board);
    }

    public void ApplyCommandMode(Context context, TacticalCommandMode mode)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ApplyCommandMode(em, mode);
    }

    public void ApplyBoardCommandMode(Context context, BoardCommandModeDirection direction, bool boardAllInteractable)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        QueueCommandMode(em, TacticalCommandMode.Board);
        ProcessPendingFeedback(em);
        BattleHudRuntimeFeedbackBoundary.ApplyBoardCommandMode(
            ResolveBattleHudView(),
            MapBoardCommandModeDirection(direction),
            boardAllInteractable);
        _matchHudSelectionPanelView?.SetBoardActionSelected(true);
    }

    private static UiBoardCommandModeDirection MapBoardCommandModeDirection(BoardCommandModeDirection direction)
    {
        return direction switch
        {
            BoardCommandModeDirection.PassengerToTransport => UiBoardCommandModeDirection.PassengerToTransport,
            BoardCommandModeDirection.TransportToPassenger => UiBoardCommandModeDirection.TransportToPassenger,
            _ => UiBoardCommandModeDirection.None
        };
    }

    public void ClearCommandMode(EntityManager em)
    {
        QueueClearCommandMode(em);
        ProcessPendingFeedback(em);
        IBattleHudRuntimeFeedbackView view = ResolveBattleHudView();
        if (!HasStickyCommandMode())
            view?.ClearCommandModeTabs();
        _matchHudSelectionPanelView?.SetBoardActionSelected(false);
    }

    public bool HasStickyCommandMode()
    {
        IBattleHudRuntimeFeedbackView view = ResolveBattleHudView();
        return view != null &&
               BattleHudRuntimeFeedbackBoundary.GetState(view).StickyCommandMode != TacticalCommandMode.None;
    }

    public void ClearCommandMode(Context context)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ClearCommandMode(em);
    }

    public void ApplyCommandResult(EntityManager em, TacticalCommandResult result)
    {
        QueueCommandResult(em, result);
        ProcessPendingFeedback(em);
    }

    public void ApplyCommandResult(Context context, TacticalCommandResult result)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        ApplyCommandResult(em, result);
    }

    public void SetWorldMarkersVisible(EntityManager em, bool visible)
    {
        QueueWorldMarkersVisible(em, visible);
        ProcessPendingFeedback(em);
    }

    public void SetWorldMarkersVisible(Context context, bool visible)
    {
        if (!TryGetDefaultEntityManager(context, out EntityManager em))
            return;

        SetWorldMarkersVisible(em, visible);
    }

    private static bool TryGetDefaultEntityManager(Context context, out EntityManager em)
    {
        em = default;
        return context.TryGetDefaultEntityManager != null &&
               context.TryGetDefaultEntityManager(out em);
    }

    private void ApplyFeedback(IBattleHudRuntimeFeedbackView view, SelectionHudFeedbackElement feedback)
    {
        switch (feedback.Kind)
        {
            case SelectionHudFeedbackKind.Selection:
            case SelectionHudFeedbackKind.SquadSelection:
                BattleHudRuntimeFeedbackBoundary.ApplySelection(view, feedback.Label.ToString(), feedback.Status.ToString());
                _matchHudSelectionPanelView?.SetSelectionVisible(true);
                break;
            case SelectionHudFeedbackKind.ClearSelection:
                BattleHudRuntimeFeedbackBoundary.ClearSelection(view);
                _matchHudSelectionPanelView?.SetSelectionVisible(false);
                break;
            case SelectionHudFeedbackKind.CommandMode:
                BattleHudRuntimeFeedbackBoundary.ApplyCommandMode(view, (TacticalCommandMode)feedback.CommandMode);
                break;
            case SelectionHudFeedbackKind.ClearCommandMode:
                BattleHudRuntimeFeedbackBoundary.ClearCommandMode(view);
                break;
            case SelectionHudFeedbackKind.CommandResult:
                BattleHudRuntimeFeedbackBoundary.ApplyCommandResult(view, feedback.CommandAccepted != 0
                    ? TacticalCommandResult.Success(feedback.Message.ToString())
                    : TacticalCommandResult.Rejected((TacticalCommandReasonCode)feedback.ReasonCode, feedback.Message.ToString()));
                break;
            case SelectionHudFeedbackKind.WorldMarkersVisible:
                BattleHudRuntimeFeedbackBoundary.SetWorldMarkersVisible(view, feedback.Visible != 0);
                break;
        }
    }

    private static FixedString64Bytes ToFixed64(string value)
    {
        FixedString64Bytes result = default;
        if (string.IsNullOrEmpty(value))
            return result;
        result.Append(value.Length <= 61 ? value : value.Substring(0, 61));
        return result;
    }

    private IBattleHudRuntimeFeedbackView ResolveBattleHudView()
    {
        return _battleHudView;
    }
}
