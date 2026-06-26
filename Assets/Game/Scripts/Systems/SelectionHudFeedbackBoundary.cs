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
        public readonly SelectionUiReadModelLookup SelectionUiReadModelLookup;
        public readonly TryGetEntityManagerDelegate TryGetDefaultEntityManager;
        public readonly ResolveSelectionPortraitSpriteDelegate ResolveSelectionPortraitSprite;

        public Context(
            SelectionUiReadModelLookup selectionUiReadModelLookup,
            TryGetEntityManagerDelegate tryGetDefaultEntityManager,
            ResolveSelectionPortraitSpriteDelegate resolveSelectionPortraitSprite = null)
        {
            SelectionUiReadModelLookup = selectionUiReadModelLookup;
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
            ResolveSelectionPortraitSprite = resolveSelectionPortraitSprite;
        }
    }

    public readonly struct SelectedSummary
    {
        public readonly int UnitCount;
        public readonly int SoldierCount;
        public readonly int VehicleCount;
        public readonly int AircraftCount;
        public readonly int TransportCount;
        public readonly int BuildingCount;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string OrderText;
        public readonly string HealthText;
        public readonly float Health01;
        public readonly SelectionSummaryPortraitKind PortraitKind;

        public SelectedSummary(
            int unitCount,
            int soldierCount,
            int vehicleCount,
            int aircraftCount,
            int transportCount,
            int buildingCount,
            string title,
            string subtitle,
            string orderText,
            string healthText,
            float health01,
            SelectionSummaryPortraitKind portraitKind)
        {
            UnitCount = unitCount;
            SoldierCount = soldierCount;
            VehicleCount = vehicleCount;
            AircraftCount = aircraftCount;
            TransportCount = transportCount;
            BuildingCount = buildingCount;
            Title = title;
            Subtitle = subtitle;
            OrderText = orderText;
            HealthText = healthText;
            Health01 = health01;
            PortraitKind = portraitKind;
        }
    }

    private IBattleHudRuntimeFeedbackSink _battleHudFeedbackSink;
    private IMatchHudSelectionPanelView _matchHudSelectionPanelView;
    private World _queryWorld;
    private EntityQuery _feedbackQuery;

    public void ResetViewCache()
    {
        _battleHudFeedbackSink = null;
    }

    public void BindMatchHudSelectionPanel(IMatchHudSelectionPanelView view)
    {
        _matchHudSelectionPanelView = view;
    }

    public void BindBattleHudRuntimeFeedback(IBattleHudRuntimeFeedbackSink feedbackSink)
    {
        _battleHudFeedbackSink = feedbackSink;
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

    public void QueueSelection(EntityManager em, Entity entity, SelectionUiReadModelLookup selectionUiReadModelLookup)
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
            Label = ToFixed64(selectionUiReadModelLookup.ResolveFocusedUnitName(em, entity)),
            Status = ToFixed64(selectionUiReadModelLookup.ResolveHudSelectionStatus(em, entity))
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

        IBattleHudRuntimeFeedbackSink feedbackSink = ResolveBattleHudFeedbackSink();
        if (feedbackSink == null)
        {
            feedback.Clear();
            return;
        }

        for (int i = 0; i < feedback.Length; i++)
            ApplyFeedback(feedbackSink, feedback[i]);
        feedback.Clear();
    }

    public void ApplySelection(EntityManager em, Entity entity, SelectionUiReadModelLookup selectionUiReadModelLookup)
    {
        bool validSelection = entity != Entity.Null && em.Exists(entity);
        QueueSelection(em, entity, selectionUiReadModelLookup);
        ProcessPendingFeedback(em);
        _matchHudSelectionPanelView?.SetSelectionVisible(validSelection);
    }

    public void ApplySelection(Context context, EntityManager em, Entity entity)
    {
        Sprite portraitSprite = context.ResolveSelectionPortraitSprite?.Invoke(em, entity);
        ApplySelection(em, entity, context.SelectionUiReadModelLookup, portraitSprite);
    }

    private void ApplySelection(EntityManager em, Entity entity, SelectionUiReadModelLookup selectionUiReadModelLookup, Sprite portraitSprite)
    {
        bool validSelection = entity != Entity.Null && em.Exists(entity);
        QueueSelection(em, entity, selectionUiReadModelLookup);
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
            context.SelectionUiReadModelLookup,
            unitTransportCapacitySystem,
            timeSeconds);
    }

    public void UpdateMatchHudSelectionPanel(
        Context context,
        SelectionStateSystem selectionStateSystem,
        FocusedUnitLifecycleCompositionSystemHelper focusedUnitLifecycleSystem,
        FocusedUnitUiReadModelSystem focusedUnitUiReadModelSystem,
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
        FocusedUnitLifecycleCompositionSystemHelper focusedUnitLifecycleSystem,
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
                context.SelectionUiReadModelLookup);
        }

        int selectedCount = CountSelectedTags(em);
        if (selectedCount > 0)
        {
            bool includeSelectedBuilding = hasSelectedBuilding != null && hasSelectedBuilding();
            return BuildSelectedSummary(
                em,
                context.SelectionUiReadModelLookup,
                includeSelectedBuilding).OrderText;
        }

        if (hasSelectedBuilding != null && hasSelectedBuilding())
            return "Structure selected";

        return "Idle";
    }

    public static SelectedSummary BuildSelectedSummary(
        EntityManager em,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        bool includeSelectedBuilding)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        if (query.IsEmptyIgnoreFilter)
        {
            int noSelectionBuildingCount = includeSelectedBuilding ? 1 : 0;
            return new SelectedSummary(
                0,
                0,
                0,
                0,
                0,
                noSelectionBuildingCount,
                noSelectionBuildingCount > 0 ? "1 STRUCTURE" : "NO SELECTION",
                noSelectionBuildingCount > 0 ? "Building selected" : string.Empty,
                noSelectionBuildingCount > 0 ? "Structure selected" : "Idle",
                "-",
                0f,
                noSelectionBuildingCount > 0 ? SelectionSummaryPortraitKind.Buildings : SelectionSummaryPortraitKind.None);
        }

        int unitCount = 0;
        int soldierCount = 0;
        int vehicleCount = 0;
        int aircraftCount = 0;
        int transportCount = 0;
        int currentTotal = 0;
        int maxTotal = 0;
        bool hasOrder = false;
        bool mixedOrders = false;
        SelectionUiReadModelLookup.FocusedUnitUiStatus firstOrder = SelectionUiReadModelLookup.FocusedUnitUiStatus.Idle;

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                unitCount++;
                UnitCategory category = ResolveCategory(em, entity);
                soldierCount += category == UnitCategory.Soldier ? 1 : 0;
                vehicleCount += category == UnitCategory.Vehicle ? 1 : 0;
                aircraftCount += category == UnitCategory.Aircraft ? 1 : 0;
                transportCount += category == UnitCategory.Transport ? 1 : 0;

                if (em.HasComponent<UnitHealth>(entity))
                {
                    UnitHealth health = em.GetComponentData<UnitHealth>(entity);
                    currentTotal += math.max(0, health.Current);
                    maxTotal += math.max(0, health.Max);
                }

                SelectionUiReadModelLookup.FocusedUnitUiStatus order = selectionUiReadModelLookup.GetFocusedUnitUiStatus(em, entity);
                if (!hasOrder)
                {
                    firstOrder = order;
                    hasOrder = true;
                }
                else if (firstOrder != order)
                {
                    mixedOrders = true;
                }
            }
        }

        int buildingCount = includeSelectedBuilding ? 1 : 0;
        string healthText = maxTotal > 0 ? $"{currentTotal}/{maxTotal}" : "-";
        float health01 = maxTotal > 0 ? math.saturate((float)currentTotal / maxTotal) : 0f;
        string orderText = mixedOrders ? "Mixed orders" : ToOrderText(firstOrder);
        SelectionSummaryPortraitKind portraitKind = ResolvePortraitKind(soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount);

        return new SelectedSummary(
            unitCount,
            soldierCount,
            vehicleCount,
            aircraftCount,
            transportCount,
            buildingCount,
            ResolveTitle(unitCount, soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount),
            ResolveSubtitle(unitCount, soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount),
            orderText,
            healthText,
            health01,
            portraitKind);
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
        bool owned = context.SelectionUiReadModelLookup.IsOwnedByPlayer(em, entity);
        bool movable = em.HasComponent<UnitMove>(entity);
        bool vehicle = context.SelectionUiReadModelLookup.IsVehicleForVisibleSelection(em, entity);
        TryGetHealthModel(context, em, entity, out string healthLabel, out float health01);
        string orderText = ResolveFocusedUnitOrderText(em, entity, context.SelectionUiReadModelLookup);
        string focusedName = context.SelectionUiReadModelLookup.ResolveFocusedUnitName(em, entity);
        string focusedDescription = context.SelectionUiReadModelLookup.ResolveFocusedUnitDescription(em, entity);
        if (tryGetAttackModeOrderSnapshot != null &&
            tryGetAttackModeOrderSnapshot(out string attackModeOrderText))
        {
            orderText = attackModeOrderText;
        }

        return new MatchHudSelectionPanelModel(
            true,
            focusedName,
            focusedDescription,
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
            transportPassengerPanelItems,
            focusedModel.TransportSoldierPassengerCount,
            focusedModel.TransportSoldierPassengerCapacity,
            focusedModel.TransportVehiclePassengerCount,
            focusedModel.TransportVehiclePassengerCapacity);
    }

    private string ResolvePassengerRoleText(Context context, EntityManager em, Entity passenger)
    {
        if (!em.Exists(passenger))
            return "UNIT";

        if (context.SelectionUiReadModelLookup.IsVehicleForVisibleSelection(em, passenger))
            return "VEHICLE";

        return "SOLDIER";
    }

    private MatchHudSelectionPanelModel BuildSquadPanelModel(
        Context context,
        EntityManager em,
        int selectedCount,
        TryGetAttackModeOrderSnapshotDelegate tryGetAttackModeOrderSnapshot,
        System.Func<Sprite> resolveActiveSquadTrayPortraitSprite,
        System.Func<bool> hasSelectedBuilding,
        HasSelectedBoardActionDelegate hasSelectedBoardAction)
    {
        bool includeSelectedBuilding = hasSelectedBuilding != null && hasSelectedBuilding();
        SelectedSummary summary = BuildSelectedSummary(
            em,
            context.SelectionUiReadModelLookup,
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
        SelectionUiReadModelLookup selectionUiReadModelLookup)
    {
        if (em.HasComponent<UnitTransportPassenger>(entity))
            return "In transport";
        if (em.HasComponent<UnitTransportBoardingTarget>(entity))
            return "Boarding transport";

        return selectionUiReadModelLookup.GetFocusedUnitUiStatus(em, entity) switch
        {
            SelectionUiReadModelLookup.FocusedUnitUiStatus.ReturningToBase => "Returning to base",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched => "Missile launched",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.AirspaceClear => "Airspace clear",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.TrackingAirTarget => "Tracking air target",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.InterceptingMissile => "Intercepting missile",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.AirDefenseReloading => "Reloading",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.Engaged => "Engaging target",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.Moving => "Moving",
            _ => "Idle"
        };
    }

    private static string ResolveTitle(
        int unitCount,
        int soldierCount,
        int vehicleCount,
        int aircraftCount,
        int transportCount,
        int buildingCount)
    {
        if (unitCount <= 0)
            return buildingCount == 1 ? "1 STRUCTURE" : "NO SELECTION";
        if (buildingCount > 0)
            return "MIXED SELECTION";
        if (soldierCount == unitCount)
            return unitCount == 1 ? "1 SOLDIER" : $"{unitCount} SOLDIERS";
        if (transportCount == unitCount)
            return unitCount == 1 ? "1 TRANSPORT" : $"{unitCount} TRANSPORTS";
        if (aircraftCount == unitCount)
            return unitCount == 1 ? "1 AIRCRAFT" : $"{unitCount} AIRCRAFT";
        if (vehicleCount == unitCount)
            return unitCount == 1 ? "1 VEHICLE" : $"{unitCount} VEHICLES";
        if (aircraftCount > 0 && soldierCount + vehicleCount + transportCount > 0)
            return "MIXED FORCE";

        return "MIXED SQUAD";
    }

    private static string ResolveSubtitle(
        int unitCount,
        int soldierCount,
        int vehicleCount,
        int aircraftCount,
        int transportCount,
        int buildingCount)
    {
        if (unitCount <= 0)
            return buildingCount > 0 ? "Building Group" : string.Empty;
        if (buildingCount > 0)
            return $"{unitCount} Units / {buildingCount} Structure";
        if (soldierCount == unitCount)
            return "Infantry Squad";
        if (transportCount == unitCount)
            return "Transport Group";
        if (aircraftCount == unitCount)
            return "Air Wing";
        if (vehicleCount == unitCount)
            return "Vehicle Squad";

        int groundCount = soldierCount + vehicleCount + transportCount;
        if (aircraftCount > 0 && groundCount > 0)
            return $"{groundCount} Ground / {aircraftCount} Air";
        if (soldierCount > 0 && vehicleCount + transportCount > 0)
            return $"{soldierCount} Infantry / {vehicleCount + transportCount} Vehicles";

        return $"{unitCount} Selected Units";
    }

    private static SelectionSummaryPortraitKind ResolvePortraitKind(
        int soldierCount,
        int vehicleCount,
        int aircraftCount,
        int transportCount,
        int buildingCount)
    {
        int categories = 0;
        categories += soldierCount > 0 ? 1 : 0;
        int groundVehicleCount = vehicleCount + transportCount;
        categories += groundVehicleCount > 0 ? 1 : 0;
        categories += aircraftCount > 0 ? 1 : 0;
        categories += buildingCount > 0 ? 1 : 0;

        if (buildingCount > 0)
            return SelectionSummaryPortraitKind.MixedForce;
        if (soldierCount > 0 && groundVehicleCount > 0 && aircraftCount > 0)
            return SelectionSummaryPortraitKind.MixedSoldierVehicleAircraft;
        if (soldierCount > 0 && aircraftCount > 0)
            return SelectionSummaryPortraitKind.MixedSoldierAircraft;
        if (groundVehicleCount > 0 && aircraftCount > 0)
            return SelectionSummaryPortraitKind.MixedVehicleAircraft;
        if (soldierCount > 0 && groundVehicleCount > 0)
            return SelectionSummaryPortraitKind.MixedSoldierVehicle;
        if (categories != 1)
            return SelectionSummaryPortraitKind.MixedForce;
        if (soldierCount > 0)
            return SelectionSummaryPortraitKind.Soldiers;
        if (transportCount > 0)
            return SelectionSummaryPortraitKind.Vehicles;
        if (aircraftCount > 0)
            return SelectionSummaryPortraitKind.Aircraft;
        if (vehicleCount > 0)
            return SelectionSummaryPortraitKind.Vehicles;
        if (buildingCount > 0)
            return SelectionSummaryPortraitKind.Buildings;

        return SelectionSummaryPortraitKind.GenericSquad;
    }

    private static UnitCategory ResolveCategory(EntityManager em, Entity entity)
    {
        string source = ResolveSource(em, entity);
        string lower = source.ToLowerInvariant();
        bool isAir = em.HasComponent<UnitAirMovement>(entity);
        bool hasTransportCapacity = em.HasComponent<UnitTransportCapacity>(entity) &&
                                    em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity > 0;
        bool usesVehicleMotion = isAir ||
                                 (em.HasComponent<UnitMovementBehavior>(entity) &&
                                  em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0);
        bool namedTransport = ContainsAny(lower, "transport", "apc", "truck", "tanker", "hauler", "canopy");
        if (isAir)
            return UnitCategory.Aircraft;
        if (hasTransportCapacity || namedTransport && usesVehicleMotion)
            return UnitCategory.Transport;
        if (usesVehicleMotion || lower.Contains("unit_veh_", System.StringComparison.OrdinalIgnoreCase))
            return UnitCategory.Vehicle;

        return UnitCategory.Soldier;
    }

    private static string ResolveSource(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string source = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(source))
                return source;
        }

        if (em.HasComponent<UnitDisplayInfo>(entity))
        {
            string displayName = em.GetComponentData<UnitDisplayInfo>(entity).Name.ToString();
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return em.GetName(entity);
    }

    private static string ToOrderText(SelectionUiReadModelLookup.FocusedUnitUiStatus status)
    {
        return status switch
        {
            SelectionUiReadModelLookup.FocusedUnitUiStatus.ReturningToBase => "Returning to base",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.MissileLaunched => "Missile launched",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.AirspaceClear => "Airspace clear",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.TrackingAirTarget => "Tracking air target",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.InterceptingMissile => "Intercepting missile",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.AirDefenseReloading => "Reloading",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.Engaged => "Engaging target",
            SelectionUiReadModelLookup.FocusedUnitUiStatus.Moving => "Moving",
            _ => "Idle"
        };
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < needles.Length; i++)
        {
            if (value.Contains(needles[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private enum UnitCategory
    {
        Soldier = 0,
        Vehicle = 1,
        Aircraft = 2,
        Transport = 3
    }

    private static void TryGetHealthModel(
        Context context,
        EntityManager em,
        Entity entity,
        out string healthLabel,
        out float health01)
    {
        if (!context.SelectionUiReadModelLookup.TryGetFocusedUnitHealth(em, entity, out int current, out int max) || max <= 0)
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
        ResolveBattleHudFeedbackSink()?.ApplyBoardCommandMode(
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
        IBattleHudRuntimeFeedbackSink feedbackSink = ResolveBattleHudFeedbackSink();
        if (!HasStickyCommandMode())
            feedbackSink?.ClearCommandModeTabs();
        _matchHudSelectionPanelView?.SetBoardActionSelected(false);
    }

    public bool HasStickyCommandMode()
    {
        IBattleHudRuntimeFeedbackSink feedbackSink = ResolveBattleHudFeedbackSink();
        return feedbackSink != null &&
               feedbackSink.GetState().StickyCommandMode != TacticalCommandMode.None;
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

    private void ApplyFeedback(IBattleHudRuntimeFeedbackSink feedbackSink, SelectionHudFeedbackElement feedback)
    {
        switch (feedback.Kind)
        {
            case SelectionHudFeedbackKind.Selection:
            case SelectionHudFeedbackKind.SquadSelection:
                feedbackSink.ApplySelection(feedback.Label.ToString(), feedback.Status.ToString());
                _matchHudSelectionPanelView?.SetSelectionVisible(true);
                break;
            case SelectionHudFeedbackKind.ClearSelection:
                feedbackSink.ClearSelection();
                _matchHudSelectionPanelView?.SetSelectionVisible(false);
                break;
            case SelectionHudFeedbackKind.CommandMode:
                feedbackSink.ApplyCommandMode((TacticalCommandMode)feedback.CommandMode);
                break;
            case SelectionHudFeedbackKind.ClearCommandMode:
                feedbackSink.ClearCommandMode();
                break;
            case SelectionHudFeedbackKind.CommandResult:
                feedbackSink.ApplyCommandResult(feedback.CommandAccepted != 0
                    ? TacticalCommandResult.Success(feedback.Message.ToString())
                    : TacticalCommandResult.Rejected((TacticalCommandReasonCode)feedback.ReasonCode, feedback.Message.ToString()));
                break;
            case SelectionHudFeedbackKind.WorldMarkersVisible:
                feedbackSink.SetWorldMarkersVisible(feedback.Visible != 0);
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

    private IBattleHudRuntimeFeedbackSink ResolveBattleHudFeedbackSink()
    {
        return _battleHudFeedbackSink;
    }
}
