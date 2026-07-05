using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public sealed class SelectionHudFeedbackUiSystemHelper
    {
        public delegate bool TryGetEntityManagerDelegate(out EntityManager em);
        public delegate Sprite ResolveSelectionPortraitSpriteDelegate(EntityManager em, Entity entity);
        public delegate void EnsureEntityQueriesDelegate(EntityManager em);
        public delegate void RefreshFocusedUnitDelegate(EntityManager em, SelectionStateCompositionSystemHelper selectionStateSystem);
        public delegate bool TryGetAttackModeOrderSnapshotDelegate(out string orderText);
        public delegate bool IsBoardCommandAvailableDelegate(EntityManager em, Entity entity);
        public delegate bool HasSelectedBoardActionDelegate(EntityManager em);
        public delegate bool TryGetSelectedBuildingResourceStorageDelegate(
            out int oilCurrent,
            out int oilCapacity,
            out int fuelCurrent,
            out int fuelCapacity);
        public delegate bool TryGetSelectedBuildingResourceStorageSnapshotDelegate(
            out SelectedBuildingResourceStorageSnapshot snapshot);

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
        private World _selectedTagQueryWorld;
        private EntityQuery _selectedTagQuery;
        private bool _hasLastPanelKey;
        private SelectionPanelCacheKey _lastPanelKey;
        private bool _hasLastSummaryPanelKey;
        private SelectionSummaryPanelCacheKey _lastSummaryPanelKey;
        private bool _hasLastTransportKey;
        private TransportPanelCacheKey _lastTransportKey;
        private bool _hasLastSelectedBuildingPanelKey;
        private SelectedBuildingPanelCacheKey _lastSelectedBuildingPanelKey;
        private bool _selectionPanelHiddenApplied;

        public void ResetViewCache()
        {
            _battleHudFeedbackSink = null;
        }

        public void BindMatchHudSelectionPanel(IMatchHudSelectionPanelView view)
        {
            _matchHudSelectionPanelView = view;
            _selectionPanelHiddenApplied = false;
            ClearPanelCache();
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
            SelectionStateCompositionSystemHelper selectionStateSystem,
            FocusedUnitUiReadModelUiSystemHelper focusedUnitUiReadModelSystem,
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
            SelectionStateCompositionSystemHelper selectionStateSystem,
            FocusedUnitLifecycleCompositionSystemHelper focusedUnitLifecycleSystem,
            FocusedUnitUiReadModelUiSystemHelper focusedUnitUiReadModelSystem,
            List<MatchHudSelectionPanelPassengerItemModel> transportPassengerPanelItems,
            EnsureEntityQueriesDelegate ensureEntityQueries,
            TryGetAttackModeOrderSnapshotDelegate tryGetAttackModeOrderSnapshot,
            ResolveSelectionPortraitSpriteDelegate resolveSelectionCardPortraitSprite,
            System.Func<Sprite> resolveSelectedBuildingPortraitSprite,
            System.Func<Sprite> resolveActiveSquadTrayPortraitSprite,
            System.Func<bool> hasSelectedBuilding,
            System.Func<string> selectedBuildingLabel,
            TryGetSelectedBuildingResourceStorageDelegate tryGetSelectedBuildingResourceStorage,
            TryGetSelectedBuildingResourceStorageSnapshotDelegate tryGetSelectedBuildingResourceStorageSnapshot,
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
            int selectedCount = CountSelectedTagsCached(em);
            if (selectedCount > 1)
            {
                MarkSelectionPanelVisible();
                SelectionSummaryPanelCacheKey summaryKey = CreateSelectionSummaryPanelCacheKey(
                    context,
                    em,
                    tryGetAttackModeOrderSnapshot,
                    hasSelectedBuilding,
                    hasSelectedBoardAction);
                if (!_hasLastSummaryPanelKey || !_lastSummaryPanelKey.Equals(summaryKey))
                {
                    ClearFocusedPanelCache();
                    _matchHudSelectionPanelView.Apply(BuildSquadPanelModel(
                        context,
                        em,
                        selectedCount,
                        tryGetAttackModeOrderSnapshot,
                        resolveActiveSquadTrayPortraitSprite,
                        hasSelectedBuilding,
                        hasSelectedBoardAction));
                    _lastSummaryPanelKey = summaryKey;
                    _hasLastSummaryPanelKey = true;
                }

                ApplyTransportPassengersHiddenCached();
                return;
            }

            if (focusedUnitLifecycleSystem != null &&
                focusedUnitLifecycleSystem.TryGetFocusedUnitEntity(em, selectionStateSystem, out Entity focusedUnit) &&
                em.Exists(focusedUnit))
            {
                string attackModeOrderText = null;
                MarkSelectionPanelVisible();
                bool hasAttackModeSnapshot = tryGetAttackModeOrderSnapshot != null &&
                                             tryGetAttackModeOrderSnapshot(out attackModeOrderText);
                bool boardAvailable = isBoardCommandAvailable != null && isBoardCommandAvailable(em, focusedUnit);
                SelectionPanelCacheKey panelKey = CreateFocusedPanelCacheKey(
                    context,
                    em,
                    focusedUnit,
                    hasAttackModeSnapshot,
                    attackModeOrderText,
                    boardAvailable);
                if (!_hasLastPanelKey || !_lastPanelKey.Equals(panelKey))
                {
                    ClearSummaryPanelCache();
                    _matchHudSelectionPanelView.Apply(BuildFocusedUnitPanelModel(
                        context,
                        em,
                        focusedUnit,
                        hasAttackModeSnapshot,
                        attackModeOrderText,
                        boardAvailable));
                    _lastPanelKey = panelKey;
                    _hasLastPanelKey = true;
                }

                if (TryCreateFocusedTransportPanelCacheKey(
                        focusedUnit,
                        focusedUnitUiReadModelSystem,
                        em,
                        out TransportPanelCacheKey transportKey) &&
                    _hasLastTransportKey &&
                    _lastTransportKey.Equals(transportKey))
                {
                    return;
                }

                _matchHudSelectionPanelView.ApplyTransportPassengers(BuildTransportPassengersPanelModel(
                    context,
                    em,
                    focusedUnit,
                    focusedUnitUiReadModelSystem,
                    transportPassengerPanelItems,
                    resolveSelectionCardPortraitSprite));
                _lastTransportKey = transportKey;
                _hasLastTransportKey = true;
                return;
            }

            if (selectedCount > 0)
            {
                MarkSelectionPanelVisible();
                SelectionSummaryPanelCacheKey summaryKey = CreateSelectionSummaryPanelCacheKey(
                    context,
                    em,
                    tryGetAttackModeOrderSnapshot,
                    hasSelectedBuilding,
                    hasSelectedBoardAction);
                if (!_hasLastSummaryPanelKey || !_lastSummaryPanelKey.Equals(summaryKey))
                {
                    ClearFocusedPanelCache();
                    _matchHudSelectionPanelView.Apply(BuildSquadPanelModel(
                        context,
                        em,
                        selectedCount,
                        tryGetAttackModeOrderSnapshot,
                        resolveActiveSquadTrayPortraitSprite,
                        hasSelectedBuilding,
                        hasSelectedBoardAction));
                    _lastSummaryPanelKey = summaryKey;
                    _hasLastSummaryPanelKey = true;
                }

                ApplyTransportPassengersHiddenCached();
                return;
            }

            if (hasSelectedBuilding != null && hasSelectedBuilding())
            {
                MarkSelectionPanelVisible();
                string buildingLabel = selectedBuildingLabel?.Invoke();
                SelectedBuildingPanelCacheKey selectedBuildingPanelKey = new(StableStringHash(buildingLabel));
                if (!_hasLastSelectedBuildingPanelKey || !_lastSelectedBuildingPanelKey.Equals(selectedBuildingPanelKey))
                {
                    ClearFocusedPanelCache();
                    ClearSummaryPanelCache();
                    _matchHudSelectionPanelView.Apply(BuildSelectedBuildingPanelModel(
                        buildingLabel,
                        resolveSelectedBuildingPortraitSprite));
                    _lastSelectedBuildingPanelKey = selectedBuildingPanelKey;
                    _hasLastSelectedBuildingPanelKey = true;
                }

                MatchHudTransportPassengersModel storageModel =
                    BuildSelectedBuildingResourceStoragePanelModel(
                        tryGetSelectedBuildingResourceStorage,
                        tryGetSelectedBuildingResourceStorageSnapshot,
                        out TransportPanelCacheKey storageKey);
                if (!_hasLastTransportKey || !_lastTransportKey.Equals(storageKey))
                {
                    _matchHudSelectionPanelView.ApplyTransportPassengers(storageModel);
                    _lastTransportKey = storageKey;
                    _hasLastTransportKey = true;
                }

                return;
            }

            ApplySelectionPanelHidden();
        }

        public string ResolveCurrentSelectionOrderTextSnapshot(
            Context context,
            SelectionStateCompositionSystemHelper selectionStateSystem,
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

            int selectedCount = CountSelectedTagsCached(em);
            if (selectedCount > 0)
            {
                bool includeSelectedBuilding = hasSelectedBuilding != null && hasSelectedBuilding();
                return BuildSelectedSummary(
                    em,
                    context.SelectionUiReadModelLookup,
                    includeSelectedBuilding,
                    GetSelectedTagQuery(em)).OrderText;
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
            return BuildSelectedSummary(
                em,
                selectionUiReadModelLookup,
                includeSelectedBuilding,
                query);
        }

        private static SelectedSummary BuildSelectedSummary(
            EntityManager em,
            SelectionUiReadModelLookup selectionUiReadModelLookup,
            bool includeSelectedBuilding,
            EntityQuery query)
        {
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

        private SelectionSummaryPanelCacheKey CreateSelectionSummaryPanelCacheKey(
            Context context,
            EntityManager em,
            TryGetAttackModeOrderSnapshotDelegate tryGetAttackModeOrderSnapshot,
            System.Func<bool> hasSelectedBuilding,
            HasSelectedBoardActionDelegate hasSelectedBoardAction)
        {
            bool includeSelectedBuilding = hasSelectedBuilding != null && hasSelectedBuilding();
            SelectedSummaryFingerprint summary = BuildSelectedSummaryFingerprint(
                em,
                context.SelectionUiReadModelLookup,
                includeSelectedBuilding,
                GetSelectedTagQuery(em));
            string attackModeOrderText = null;
            bool hasAttackModeSnapshot = tryGetAttackModeOrderSnapshot != null &&
                                         tryGetAttackModeOrderSnapshot(out attackModeOrderText);
            return new SelectionSummaryPanelCacheKey(
                summary.UnitCount,
                summary.SoldierCount,
                summary.VehicleCount,
                summary.AircraftCount,
                summary.TransportCount,
                summary.BuildingCount,
                summary.CurrentTotal,
                summary.MaxTotal,
                summary.HasOrder,
                summary.MixedOrders,
                (int)summary.FirstOrder,
                summary.PortraitKind,
                hasAttackModeSnapshot,
                StableStringHash(attackModeOrderText),
                hasSelectedBoardAction != null && hasSelectedBoardAction(em));
        }

        private static SelectedSummaryFingerprint BuildSelectedSummaryFingerprint(
            EntityManager em,
            SelectionUiReadModelLookup selectionUiReadModelLookup,
            bool includeSelectedBuilding,
            EntityQuery query)
        {
            if (query.IsEmptyIgnoreFilter)
            {
                int noSelectionBuildingCount = includeSelectedBuilding ? 1 : 0;
                return new SelectedSummaryFingerprint(
                    0,
                    0,
                    0,
                    0,
                    0,
                    noSelectionBuildingCount,
                    0,
                    0,
                    false,
                    false,
                    SelectionUiReadModelLookup.FocusedUnitUiStatus.Idle,
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
            return new SelectedSummaryFingerprint(
                unitCount,
                soldierCount,
                vehicleCount,
                aircraftCount,
                transportCount,
                buildingCount,
                currentTotal,
                maxTotal,
                hasOrder,
                mixedOrders,
                firstOrder,
                ResolvePortraitKind(soldierCount, vehicleCount, aircraftCount, transportCount, buildingCount));
        }

        private EntityQuery GetSelectedTagQuery(EntityManager em)
        {
            World world = em.World;
            if (_selectedTagQueryWorld != world || world == null || !world.IsCreated)
            {
                _selectedTagQueryWorld = world;
                _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
            }

            return _selectedTagQuery;
        }

        private int CountSelectedTagsCached(EntityManager em)
        {
            return GetSelectedTagQuery(em).CalculateEntityCount();
        }

        private void ApplySelectionPanelHidden()
        {
            if (_selectionPanelHiddenApplied)
                return;

            ClearPanelCache();
            _matchHudSelectionPanelView?.Apply(MatchHudSelectionPanelModel.Hidden);
            _matchHudSelectionPanelView?.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
            _selectionPanelHiddenApplied = true;
        }

        private void MarkSelectionPanelVisible()
        {
            _selectionPanelHiddenApplied = false;
        }

        private void ClearPanelCache()
        {
            ClearFocusedPanelCache();
            ClearSummaryPanelCache();
        }

        private void ClearSummaryPanelCache()
        {
            _hasLastSummaryPanelKey = false;
            _lastSummaryPanelKey = default;
        }

        private void ClearFocusedPanelCache()
        {
            _hasLastPanelKey = false;
            _lastPanelKey = default;
            _hasLastTransportKey = false;
            _lastTransportKey = default;
            _hasLastSelectedBuildingPanelKey = false;
            _lastSelectedBuildingPanelKey = default;
        }

        private void ApplyTransportPassengersHiddenCached()
        {
            TransportPanelCacheKey hiddenKey = new(UiEntityHandle.Null, false);
            if (_hasLastTransportKey && _lastTransportKey.Equals(hiddenKey))
                return;

            _matchHudSelectionPanelView.ApplyTransportPassengers(MatchHudTransportPassengersModel.Hidden);
            _lastTransportKey = hiddenKey;
            _hasLastTransportKey = true;
        }

        private static SelectionPanelCacheKey CreateFocusedPanelCacheKey(
            Context context,
            EntityManager em,
            Entity entity,
            bool hasAttackModeOrderSnapshot,
            string attackModeOrderText,
            bool boardAvailable)
        {
            context.SelectionUiReadModelLookup.TryGetFocusedUnitHealth(em, entity, out int healthCurrent, out int healthMax);
            return new SelectionPanelCacheKey(
                ToUiHandle(entity),
                (int)context.SelectionUiReadModelLookup.GetFocusedUnitUiStatus(em, entity),
                healthCurrent,
                healthMax,
                context.SelectionUiReadModelLookup.IsOwnedByPlayer(em, entity),
                em.HasComponent<UnitMove>(entity),
                context.SelectionUiReadModelLookup.IsVehicleForVisibleSelection(em, entity),
                em.HasComponent<UnitTransportPassenger>(entity),
                em.HasComponent<UnitTransportBoardingTarget>(entity),
                hasAttackModeOrderSnapshot,
                StableStringHash(attackModeOrderText),
                boardAvailable);
        }

        private static bool TryCreateFocusedTransportPanelCacheKey(
            Entity focusedUnit,
            FocusedUnitUiReadModelUiSystemHelper focusedUnitUiReadModelSystem,
            EntityManager em,
            out TransportPanelCacheKey key)
        {
            key = new TransportPanelCacheKey(ToUiHandle(focusedUnit), false);
            if (focusedUnitUiReadModelSystem == null ||
                !focusedUnitUiReadModelSystem.TryRead(
                    em,
                    out FocusedUnitUiReadModelComponent focusedModel,
                    out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers) ||
                focusedModel.HasFocusedUnit == 0 ||
                focusedModel.FocusedUnit != focusedUnit ||
                focusedModel.OwnedByPlayer == 0)
            {
                return true;
            }

            if (focusedModel.TransportPassengerCapacity > 0)
            {
                int passengerHash = 17;
                for (int i = 0; i < passengers.Length; i++)
                {
                    FocusedUnitPassengerUiReadModelElement passenger = passengers[i];
                    unchecked
                    {
                        passengerHash = passengerHash * 31 + ToUiHandle(passenger.Passenger).GetHashCode();
                        passengerHash = passengerHash * 31 + passenger.HealthCurrent;
                        passengerHash = passengerHash * 31 + passenger.HealthMax;
                    }
                }

                key = new TransportPanelCacheKey(
                    ToUiHandle(focusedUnit),
                    true,
                    MatchHudStorageChipKind.Passengers,
                    focusedModel.PassengerCount,
                    focusedModel.TransportPassengerCapacity,
                    focusedModel.TransportSoldierPassengerCount,
                    focusedModel.TransportSoldierPassengerCapacity,
                    focusedModel.TransportVehiclePassengerCount,
                    focusedModel.TransportVehiclePassengerCapacity,
                    0,
                    0,
                    0,
                    0,
                    passengerHash);
                return true;
            }

            if (focusedModel.HasResourceCargo != 0 && focusedModel.ResourceCargoCapacity > 0)
            {
                key = new TransportPanelCacheKey(
                    ToUiHandle(focusedUnit),
                    true,
                    MatchHudStorageChipKind.ResourceCargo,
                    focusedModel.ResourceCargoOilBarrels + focusedModel.ResourceCargoFuelBarrels,
                    focusedModel.ResourceCargoCapacity,
                    0,
                    0,
                    0,
                    0,
                    focusedModel.ResourceCargoOilBarrels,
                    focusedModel.ResourceCargoCapacity,
                    focusedModel.ResourceCargoFuelBarrels,
                    focusedModel.ResourceCargoCapacity,
                    focusedModel.ResourceCargoStatusText.GetHashCode());
            }

            return true;
        }

        private static int StableStringHash(string value)
        {
            return string.IsNullOrEmpty(value)
                ? 0
                : System.StringComparer.Ordinal.GetHashCode(value);
        }

        private MatchHudSelectionPanelModel BuildFocusedUnitPanelModel(
            Context context,
            EntityManager em,
            Entity entity,
            bool hasAttackModeOrderSnapshot,
            string attackModeOrderText,
            bool boardAvailable)
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
            if (hasAttackModeOrderSnapshot)
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
                boardAvailable);
        }

        private MatchHudTransportPassengersModel BuildTransportPassengersPanelModel(
            Context context,
            EntityManager em,
            Entity transport,
            FocusedUnitUiReadModelUiSystemHelper focusedUnitUiReadModelSystem,
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
                focusedModel.OwnedByPlayer == 0)
            {
                return MatchHudTransportPassengersModel.Hidden;
            }

            if (focusedModel.TransportPassengerCapacity <= 0)
                return BuildResourceCargoPanelModel(transport, focusedModel);

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

        private static MatchHudTransportPassengersModel BuildResourceCargoPanelModel(
            Entity entity,
            FocusedUnitUiReadModelComponent focusedModel)
        {
            if (focusedModel.HasResourceCargo == 0 || focusedModel.ResourceCargoCapacity <= 0)
                return MatchHudTransportPassengersModel.Hidden;

            return new MatchHudTransportPassengersModel(
                true,
                false,
                ToUiHandle(entity),
                focusedModel.ResourceCargoOilBarrels + focusedModel.ResourceCargoFuelBarrels,
                focusedModel.ResourceCargoCapacity,
                false,
                null,
                storageKind: MatchHudStorageChipKind.ResourceCargo,
                oilCurrent: focusedModel.ResourceCargoOilBarrels,
                oilCapacity: focusedModel.ResourceCargoCapacity,
                fuelCurrent: focusedModel.ResourceCargoFuelBarrels,
                fuelCapacity: focusedModel.ResourceCargoCapacity,
                statusText: focusedModel.ResourceCargoStatusText.Length > 0
                    ? focusedModel.ResourceCargoStatusText.ToString()
                    : null);
        }

        private static MatchHudTransportPassengersModel BuildSelectedBuildingResourceStoragePanelModel(
            TryGetSelectedBuildingResourceStorageDelegate tryGetSelectedBuildingResourceStorage,
            TryGetSelectedBuildingResourceStorageSnapshotDelegate tryGetSelectedBuildingResourceStorageSnapshot,
            out TransportPanelCacheKey cacheKey)
        {
            SelectedBuildingResourceStorageSnapshot snapshot;
            if (tryGetSelectedBuildingResourceStorageSnapshot == null ||
                !tryGetSelectedBuildingResourceStorageSnapshot(out snapshot))
            {
                if (tryGetSelectedBuildingResourceStorage == null ||
                    !tryGetSelectedBuildingResourceStorage(
                        out int fallbackOilCurrent,
                        out int fallbackOilCapacity,
                        out int fallbackFuelCurrent,
                        out int fallbackFuelCapacity))
                {
                    cacheKey = new TransportPanelCacheKey(UiEntityHandle.Null, false);
                    return MatchHudTransportPassengersModel.Hidden;
                }

                snapshot = new SelectedBuildingResourceStorageSnapshot(
                    0,
                    fallbackOilCurrent,
                    fallbackOilCapacity,
                    fallbackFuelCurrent,
                    fallbackFuelCapacity,
                    0u);
            }

            string statusText = ResolveSelectedBuildingResourceStorageStatusText(snapshot);
            bool hasOil = snapshot.OilCapacity > 0 || snapshot.OilCurrent > 0;
            bool hasFuel = snapshot.FuelCapacity > 0 || snapshot.FuelCurrent > 0;
            if (!hasOil && !hasFuel)
            {
                cacheKey = new TransportPanelCacheKey(UiEntityHandle.Null, false);
                return MatchHudTransportPassengersModel.Hidden;
            }

            MatchHudStorageChipKind kind = hasOil && hasFuel
                ? MatchHudStorageChipKind.OilAndFuel
                : hasOil
                    ? MatchHudStorageChipKind.OilBarrels
                    : MatchHudStorageChipKind.FuelBarrels;
            cacheKey = new TransportPanelCacheKey(
                UiEntityHandle.Null,
                true,
                kind,
                snapshot.OilCurrent + snapshot.FuelCurrent,
                snapshot.OilCapacity + snapshot.FuelCapacity,
                0,
                0,
                0,
                0,
                snapshot.OilCurrent,
                snapshot.OilCapacity,
                snapshot.FuelCurrent,
                snapshot.FuelCapacity,
                unchecked(((int)snapshot.Version * 397) ^ StableStringHash(statusText)));

            return new MatchHudTransportPassengersModel(
                true,
                false,
                UiEntityHandle.Null,
                snapshot.OilCurrent + snapshot.FuelCurrent,
                snapshot.OilCapacity + snapshot.FuelCapacity,
                false,
                null,
                storageKind: kind,
                oilCurrent: snapshot.OilCurrent,
                oilCapacity: snapshot.OilCapacity,
                fuelCurrent: snapshot.FuelCurrent,
                fuelCapacity: snapshot.FuelCapacity,
                statusText: statusText);
        }

        private static string ResolveSelectedBuildingResourceStorageStatusText(
            SelectedBuildingResourceStorageSnapshot snapshot)
        {
            if (snapshot.FuelBarrelsPerDay <= 0f)
                return null;
            if (snapshot.OilCurrent <= 0)
                return "WAITING OIL";
            if (snapshot.FuelCapacity > 0 && snapshot.FuelCurrent >= snapshot.FuelCapacity)
                return "FUEL FULL";
            return "CONVERTING";
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
                includeSelectedBuilding,
                GetSelectedTagQuery(em));
            string orderText = tryGetAttackModeOrderSnapshot != null &&
                               tryGetAttackModeOrderSnapshot(out string attackModeOrderText)
                ? attackModeOrderText
                : summary.OrderText;
            Sprite portraitSprite = null;
            if (summary.PortraitKind == SelectionSummaryPortraitKind.GenericSquad)
            {
                portraitSprite = resolveActiveSquadTrayPortraitSprite?.Invoke();
                portraitSprite ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.GenericSquad);
            }

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
            string selectedBuildingLabel,
            System.Func<Sprite> resolveSelectedBuildingPortraitSprite)
        {
            Sprite portraitSprite = resolveSelectedBuildingPortraitSprite?.Invoke();
            portraitSprite ??= _matchHudSelectionPanelView.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Buildings);
            return new MatchHudSelectionPanelModel(
                true,
                string.IsNullOrWhiteSpace(selectedBuildingLabel) ? "Selected Building" : selectedBuildingLabel,
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

        private readonly struct SelectedSummaryFingerprint
        {
            public readonly int UnitCount;
            public readonly int SoldierCount;
            public readonly int VehicleCount;
            public readonly int AircraftCount;
            public readonly int TransportCount;
            public readonly int BuildingCount;
            public readonly int CurrentTotal;
            public readonly int MaxTotal;
            public readonly bool HasOrder;
            public readonly bool MixedOrders;
            public readonly SelectionUiReadModelLookup.FocusedUnitUiStatus FirstOrder;
            public readonly SelectionSummaryPortraitKind PortraitKind;

            public SelectedSummaryFingerprint(
                int unitCount,
                int soldierCount,
                int vehicleCount,
                int aircraftCount,
                int transportCount,
                int buildingCount,
                int currentTotal,
                int maxTotal,
                bool hasOrder,
                bool mixedOrders,
                SelectionUiReadModelLookup.FocusedUnitUiStatus firstOrder,
                SelectionSummaryPortraitKind portraitKind)
            {
                UnitCount = unitCount;
                SoldierCount = soldierCount;
                VehicleCount = vehicleCount;
                AircraftCount = aircraftCount;
                TransportCount = transportCount;
                BuildingCount = buildingCount;
                CurrentTotal = currentTotal;
                MaxTotal = maxTotal;
                HasOrder = hasOrder;
                MixedOrders = mixedOrders;
                FirstOrder = firstOrder;
                PortraitKind = portraitKind;
            }
        }

        private readonly struct SelectionSummaryPanelCacheKey : System.IEquatable<SelectionSummaryPanelCacheKey>
        {
            private readonly int _unitCount;
            private readonly int _soldierCount;
            private readonly int _vehicleCount;
            private readonly int _aircraftCount;
            private readonly int _transportCount;
            private readonly int _buildingCount;
            private readonly int _currentTotal;
            private readonly int _maxTotal;
            private readonly int _orderStatus;
            private readonly int _attackModeOrderHash;
            private readonly SelectionSummaryPortraitKind _portraitKind;
            private readonly bool _hasOrder;
            private readonly bool _mixedOrders;
            private readonly bool _hasAttackModeOrder;
            private readonly bool _boardAvailable;

            public SelectionSummaryPanelCacheKey(
                int unitCount,
                int soldierCount,
                int vehicleCount,
                int aircraftCount,
                int transportCount,
                int buildingCount,
                int currentTotal,
                int maxTotal,
                bool hasOrder,
                bool mixedOrders,
                int orderStatus,
                SelectionSummaryPortraitKind portraitKind,
                bool hasAttackModeOrder,
                int attackModeOrderHash,
                bool boardAvailable)
            {
                _unitCount = unitCount;
                _soldierCount = soldierCount;
                _vehicleCount = vehicleCount;
                _aircraftCount = aircraftCount;
                _transportCount = transportCount;
                _buildingCount = buildingCount;
                _currentTotal = currentTotal;
                _maxTotal = maxTotal;
                _hasOrder = hasOrder;
                _mixedOrders = mixedOrders;
                _orderStatus = orderStatus;
                _portraitKind = portraitKind;
                _hasAttackModeOrder = hasAttackModeOrder;
                _attackModeOrderHash = attackModeOrderHash;
                _boardAvailable = boardAvailable;
            }

            public bool Equals(SelectionSummaryPanelCacheKey other)
            {
                return _unitCount == other._unitCount &&
                       _soldierCount == other._soldierCount &&
                       _vehicleCount == other._vehicleCount &&
                       _aircraftCount == other._aircraftCount &&
                       _transportCount == other._transportCount &&
                       _buildingCount == other._buildingCount &&
                       _currentTotal == other._currentTotal &&
                       _maxTotal == other._maxTotal &&
                       _hasOrder == other._hasOrder &&
                       _mixedOrders == other._mixedOrders &&
                       _orderStatus == other._orderStatus &&
                       _portraitKind == other._portraitKind &&
                       _hasAttackModeOrder == other._hasAttackModeOrder &&
                       _attackModeOrderHash == other._attackModeOrderHash &&
                       _boardAvailable == other._boardAvailable;
            }
        }

        private readonly struct SelectedBuildingPanelCacheKey : System.IEquatable<SelectedBuildingPanelCacheKey>
        {
            private readonly int _labelHash;

            public SelectedBuildingPanelCacheKey(int labelHash)
            {
                _labelHash = labelHash;
            }

            public bool Equals(SelectedBuildingPanelCacheKey other)
            {
                return _labelHash == other._labelHash;
            }
        }

        private static UnitCategory ResolveCategory(EntityManager em, Entity entity)
        {
            bool sourceKeyStartsWithVehicle = false;
            bool namedTransport = false;
            if (em.HasComponent<UnitSourcePrefabKey>(entity))
            {
                FixedString64Bytes sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
                sourceKeyStartsWithVehicle = StartsWithUnitVehiclePrefix(sourceKey);
                namedTransport = ContainsTransportName(sourceKey);
            }
            else if (em.HasComponent<UnitDisplayInfo>(entity))
            {
                UnitDisplayInfo displayInfo = em.GetComponentData<UnitDisplayInfo>(entity);
                namedTransport =
                    ContainsTransportName(displayInfo.Name) ||
                    ContainsTransportName(displayInfo.Description);
            }

            bool isAir = em.HasComponent<UnitAirMovement>(entity);
            bool hasTransportCapacity = em.HasComponent<UnitTransportCapacity>(entity) &&
                                        em.GetComponentData<UnitTransportCapacity>(entity).SoldierCapacity > 0;
            bool usesVehicleMotion = isAir ||
                                     (em.HasComponent<UnitMovementBehavior>(entity) &&
                                      em.GetComponentData<UnitMovementBehavior>(entity).UsesVehicleMotion != 0);
            if (isAir)
                return UnitCategory.Aircraft;
            if (hasTransportCapacity || namedTransport && usesVehicleMotion)
                return UnitCategory.Transport;
            if (usesVehicleMotion || sourceKeyStartsWithVehicle)
                return UnitCategory.Vehicle;

            return UnitCategory.Soldier;
        }

        private static bool StartsWithUnitVehiclePrefix(FixedString64Bytes value)
        {
            return HasNineBytePrefixIgnoreCase(
                value,
                (byte)'U',
                (byte)'n',
                (byte)'i',
                (byte)'t',
                (byte)'_',
                (byte)'V',
                (byte)'e',
                (byte)'h',
                (byte)'_');
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

        private static bool ContainsTransportName(FixedString64Bytes value)
        {
            if (value.Length == 0)
                return false;

            return ContainsIgnoreCase(value, "transport") ||
                   ContainsIgnoreCase(value, "apc") ||
                   ContainsIgnoreCase(value, "truck") ||
                   ContainsIgnoreCase(value, "tanker") ||
                   ContainsIgnoreCase(value, "hauler") ||
                   ContainsIgnoreCase(value, "canopy");
        }

        private static bool ContainsTransportName(FixedString128Bytes value)
        {
            if (value.Length == 0)
                return false;

            return ContainsIgnoreCase(value, "transport") ||
                   ContainsIgnoreCase(value, "apc") ||
                   ContainsIgnoreCase(value, "truck") ||
                   ContainsIgnoreCase(value, "tanker") ||
                   ContainsIgnoreCase(value, "hauler") ||
                   ContainsIgnoreCase(value, "canopy");
        }

        private static bool ContainsIgnoreCase(FixedString64Bytes value, string needle)
        {
            if (string.IsNullOrEmpty(needle) || value.Length < needle.Length)
                return false;

            for (int start = 0; start <= value.Length - needle.Length; start++)
            {
                bool matched = true;
                for (int i = 0; i < needle.Length; i++)
                {
                    if (!EqualsAsciiIgnoreCase(value[start + i], (byte)needle[i]))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                    return true;
            }

            return false;
        }

        private static bool ContainsIgnoreCase(FixedString128Bytes value, string needle)
        {
            if (string.IsNullOrEmpty(needle) || value.Length < needle.Length)
                return false;

            for (int start = 0; start <= value.Length - needle.Length; start++)
            {
                bool matched = true;
                for (int i = 0; i < needle.Length; i++)
                {
                    if (!EqualsAsciiIgnoreCase(value[start + i], (byte)needle[i]))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                    return true;
            }

            return false;
        }

        private static bool HasNineBytePrefixIgnoreCase(
            FixedString64Bytes value,
            byte c0,
            byte c1,
            byte c2,
            byte c3,
            byte c4,
            byte c5,
            byte c6,
            byte c7,
            byte c8)
        {
            return value.Length >= 9 &&
                   EqualsAsciiIgnoreCase(value[0], c0) &&
                   EqualsAsciiIgnoreCase(value[1], c1) &&
                   EqualsAsciiIgnoreCase(value[2], c2) &&
                   EqualsAsciiIgnoreCase(value[3], c3) &&
                   EqualsAsciiIgnoreCase(value[4], c4) &&
                   EqualsAsciiIgnoreCase(value[5], c5) &&
                   EqualsAsciiIgnoreCase(value[6], c6) &&
                   EqualsAsciiIgnoreCase(value[7], c7) &&
                   EqualsAsciiIgnoreCase(value[8], c8);
        }

        private static bool EqualsAsciiIgnoreCase(byte a, byte b)
        {
            return ToLowerAscii(a) == ToLowerAscii(b);
        }

        private static byte ToLowerAscii(byte value)
        {
            return value >= (byte)'A' && value <= (byte)'Z'
                ? (byte)(value + 32)
                : value;
        }

        private enum UnitCategory
        {
            Soldier = 0,
            Vehicle = 1,
            Aircraft = 2,
            Transport = 3
        }

        private readonly struct SelectionPanelCacheKey : System.IEquatable<SelectionPanelCacheKey>
        {
            private readonly UiEntityHandle _entity;
            private readonly int _status;
            private readonly int _healthCurrent;
            private readonly int _healthMax;
            private readonly int _attackModeOrderHash;
            private readonly bool _owned;
            private readonly bool _movable;
            private readonly bool _vehicle;
            private readonly bool _transportPassenger;
            private readonly bool _boardingTarget;
            private readonly bool _hasAttackModeOrder;
            private readonly bool _boardAvailable;

            public SelectionPanelCacheKey(
                UiEntityHandle entity,
                int status,
                int healthCurrent,
                int healthMax,
                bool owned,
                bool movable,
                bool vehicle,
                bool transportPassenger,
                bool boardingTarget,
                bool hasAttackModeOrder,
                int attackModeOrderHash,
                bool boardAvailable)
            {
                _entity = entity;
                _status = status;
                _healthCurrent = healthCurrent;
                _healthMax = healthMax;
                _owned = owned;
                _movable = movable;
                _vehicle = vehicle;
                _transportPassenger = transportPassenger;
                _boardingTarget = boardingTarget;
                _hasAttackModeOrder = hasAttackModeOrder;
                _attackModeOrderHash = attackModeOrderHash;
                _boardAvailable = boardAvailable;
            }

            public bool Equals(SelectionPanelCacheKey other)
            {
                return _entity == other._entity &&
                       _status == other._status &&
                       _healthCurrent == other._healthCurrent &&
                       _healthMax == other._healthMax &&
                       _attackModeOrderHash == other._attackModeOrderHash &&
                       _owned == other._owned &&
                       _movable == other._movable &&
                       _vehicle == other._vehicle &&
                       _transportPassenger == other._transportPassenger &&
                       _boardingTarget == other._boardingTarget &&
                       _hasAttackModeOrder == other._hasAttackModeOrder &&
                       _boardAvailable == other._boardAvailable;
            }
        }

        private readonly struct TransportPanelCacheKey : System.IEquatable<TransportPanelCacheKey>
        {
            private readonly UiEntityHandle _entity;
            private readonly bool _visible;
            private readonly MatchHudStorageChipKind _storageKind;
            private readonly int _passengerCount;
            private readonly int _capacity;
            private readonly int _soldierPassengerCount;
            private readonly int _soldierCapacity;
            private readonly int _vehiclePassengerCount;
            private readonly int _vehicleCapacity;
            private readonly int _oilCurrent;
            private readonly int _oilCapacity;
            private readonly int _fuelCurrent;
            private readonly int _fuelCapacity;
            private readonly int _passengerHash;

            public TransportPanelCacheKey(UiEntityHandle entity, bool visible)
                : this(
                    entity,
                    visible,
                    MatchHudStorageChipKind.Passengers,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0)
            {
            }

            public TransportPanelCacheKey(
                UiEntityHandle entity,
                bool visible,
                MatchHudStorageChipKind storageKind,
                int passengerCount,
                int capacity,
                int soldierPassengerCount,
                int soldierCapacity,
                int vehiclePassengerCount,
                int vehicleCapacity,
                int oilCurrent,
                int oilCapacity,
                int fuelCurrent,
                int fuelCapacity,
                int passengerHash)
            {
                _entity = entity;
                _visible = visible;
                _storageKind = storageKind;
                _passengerCount = passengerCount;
                _capacity = capacity;
                _soldierPassengerCount = soldierPassengerCount;
                _soldierCapacity = soldierCapacity;
                _vehiclePassengerCount = vehiclePassengerCount;
                _vehicleCapacity = vehicleCapacity;
                _oilCurrent = oilCurrent;
                _oilCapacity = oilCapacity;
                _fuelCurrent = fuelCurrent;
                _fuelCapacity = fuelCapacity;
                _passengerHash = passengerHash;
            }

            public bool Equals(TransportPanelCacheKey other)
            {
                return _entity == other._entity &&
                       _visible == other._visible &&
                       _storageKind == other._storageKind &&
                       _passengerCount == other._passengerCount &&
                       _capacity == other._capacity &&
                       _soldierPassengerCount == other._soldierPassengerCount &&
                       _soldierCapacity == other._soldierCapacity &&
                       _vehiclePassengerCount == other._vehiclePassengerCount &&
                       _vehicleCapacity == other._vehicleCapacity &&
                       _oilCurrent == other._oilCurrent &&
                       _oilCapacity == other._oilCapacity &&
                       _fuelCurrent == other._fuelCurrent &&
                       _fuelCapacity == other._fuelCapacity &&
                       _passengerHash == other._passengerHash;
            }
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
}
