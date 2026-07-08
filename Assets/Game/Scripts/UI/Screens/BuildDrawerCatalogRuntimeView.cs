using System;
using System.Collections.Generic;
using Game.Catalog.Contracts;
using Game.Configs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildDrawerCatalogRuntimeView : MonoBehaviour
    {
        private const float QueueRefreshIntervalSeconds = 0.2f;

        [SerializeField] private BuildDrawerView view;
        [SerializeField] private ScriptableObject unitPrefabRegistryConfig;
        [SerializeField] private ScriptableObject buildingPlacementConfig;

        private readonly BuildDrawerCatalogQueryUiSystemHelper _query = new();
        private readonly List<BuildDrawerCatalogItem> _items = new();
        private readonly List<BuildDrawerCatalogItem> _countScratch = new();
        private readonly List<BuildingPendingProductionUiEntry> _pendingProductions = new();
        private readonly List<BuildingPendingProductionUiEntry> _clearProductionScratch = new();
        private readonly List<BuildDrawerItemView> _runtimeItems = new();
        private readonly List<BuildDrawerQueueItemView> _runtimeQueueItems = new();
        private readonly List<ButtonBinding> _tabBindings = new();
        private readonly List<ButtonBinding> _itemBindings = new();
        private BuildDrawerCategory _activeCategory = BuildDrawerCategory.Buildings;
        private BuildDrawerItemView _selectedItemView;
        private BuildDrawerCatalogItem _selectedItem;
        private bool _hasSelectedItem;
        private IBuildingUiCommand _uiCommandSystem;
        private IBuildingUiQuery _uiQuerySystem;
        private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private Action _closeDrawer;
        private Button _primaryActionButton;
        private UnityAction _primaryActionListener;
        private Button _cancelButton;
        private UnityAction _cancelButtonListener;
        private Button _clearButton;
        private UnityAction _clearButtonListener;
        private float _nextQueueRefreshTime;
        private ICatalogPrefabSource _unitPrefabSourceOverride;
        private ICatalogPrefabSource _buildingPrefabSourceOverride;

        private void Awake()
        {
            if (view == null)
                view = GetComponent<BuildDrawerView>();
        }

        private void OnEnable()
        {
            _nextQueueRefreshTime = 0f;
            WireTabs();
            WirePrimaryAction();
            WireQueueControls();
            Refresh();
        }

        private void Update()
        {
            if (view == null || _uiQuerySystem == null || Time.unscaledTime < _nextQueueRefreshTime)
                return;

            _nextQueueRefreshTime = Time.unscaledTime + QueueRefreshIntervalSeconds;
            RefreshQueue();
        }

        private void OnDisable()
        {
            UnwirePrimaryAction();
            UnwireQueueControls();
            ClearTabBindings();
            ClearItemBindings();
            ClearRuntimeItems();
            ClearRuntimeQueueItems();
            _selectedItemView = null;
            _hasSelectedItem = false;
            _nextQueueRefreshTime = 0f;
        }

        public void ConfigureForTests(
            BuildDrawerView drawerView,
            ICatalogPrefabSource unitRegistry,
            ICatalogPrefabSource buildingPlacement)
        {
            view = drawerView;
            _unitPrefabSourceOverride = unitRegistry;
            _buildingPrefabSourceOverride = buildingPlacement;
        }

        public void ConfigureCatalogMetadataResolvers(
            TryResolveUiBuildingCatalogMetadata tryResolveBuildingMetadata,
            TryResolveUiUnitCatalogMetadata tryResolveUnitMetadata)
        {
            _query.ConfigureMetadataResolvers(tryResolveBuildingMetadata, tryResolveUnitMetadata);
            if (isActiveAndEnabled)
                Refresh();
        }

        public void SelectCategoryForTests(BuildDrawerCategory category)
        {
            SelectCategory(category);
        }

        public void RefreshForTests()
        {
            Refresh();
        }

        public void ApplyQueueSnapshotForTests(IReadOnlyList<BuildingPendingProductionUiEntry> entries)
        {
            _pendingProductions.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                    _pendingProductions.Add(entries[i]);
            }

            ApplyQueueEntries();
        }

        public void BindRuntimeCommands(
            IBuildingUiCommand uiCommandSystem,
            Action closeDrawer,
            BattleHudRuntimeFeedbackView runtimeFeedbackView = null)
        {
            _uiCommandSystem = uiCommandSystem;
            _closeDrawer = closeDrawer;
            _runtimeFeedbackView = runtimeFeedbackView;
            WirePrimaryAction();
            WireQueueControls();
            RefreshQueue();
            ApplyInstructionForCurrentSelection();
        }

        public void BindRuntimeQueries(IBuildingUiQuery uiQuerySystem)
        {
            _uiQuerySystem = uiQuerySystem;
            _nextQueueRefreshTime = 0f;
            RefreshQueue();
        }

        private void WireTabs()
        {
            if (view == null || view.Tabs == null)
                return;

            for (int i = 0; i < view.Tabs.Length; i++)
            {
                BuildDrawerTabView tab = view.Tabs[i];
                if (tab == null || tab.Button == null || HasTabBinding(tab.Button))
                    continue;

                BuildDrawerCategory category = tab.Category;
                UnityAction action = () => SelectCategory(category);
                tab.Button.onClick.AddListener(action);
                _tabBindings.Add(new ButtonBinding(tab.Button, action));
            }
        }

        private void WirePrimaryAction()
        {
            if (view == null)
                return;

            Button button = view.PrimaryActionButton;
            if (button == null || button == _primaryActionButton)
                return;

            UnwirePrimaryAction();
            _primaryActionButton = button;
            _primaryActionListener = OnPrimaryActionClicked;
            _primaryActionButton.onClick.RemoveListener(_primaryActionListener);
            _primaryActionButton.onClick.AddListener(_primaryActionListener);
        }

        private void WireQueueControls()
        {
            if (view == null)
                return;

            Button button = ResolveProductionCancelButton();
            if (button != null && button != _cancelButton)
            {
                UnwireCancelButton();
                _cancelButton = button;
                _cancelButtonListener = OnCancelProductionClicked;
                _cancelButton.onClick.RemoveListener(_cancelButtonListener);
                _cancelButton.onClick.AddListener(_cancelButtonListener);
            }

            Button clearButton = view.ClearButton;
            if (clearButton != null && clearButton != _clearButton)
            {
                UnwireClearButton();
                _clearButton = clearButton;
                _clearButtonListener = OnClearProductionsClicked;
                _clearButton.onClick.RemoveListener(_clearButtonListener);
                _clearButton.onClick.AddListener(_clearButtonListener);
            }
        }

        private Button ResolveProductionCancelButton()
        {
            if (view == null)
                return null;

            return view.ActiveItemView != null && view.ActiveItemView.CancelButton != null
                ? view.ActiveItemView.CancelButton
                : view.CancelButton;
        }

        private void SelectCategory(BuildDrawerCategory category)
        {
            if (_activeCategory == category)
                return;

            _activeCategory = category;
            Refresh();
        }

        private void Refresh()
        {
            if (view == null)
                return;

            int[] counts = CountCategories();
            bool[] enabledStates = BuildEnabledStates(counts);
            view.ApplyTabVisuals(_activeCategory, counts, enabledStates);

            _query.Collect(UnitPrefabSource, BuildingPrefabSource, _activeCategory, _items);
            PopulateItems(_items);
            RefreshQueue();
        }

        private int[] CountCategories()
        {
            int[] counts = new int[4];
            for (int i = 0; i < counts.Length; i++)
            {
                BuildDrawerCategory category = (BuildDrawerCategory)i;
                _query.Collect(UnitPrefabSource, BuildingPrefabSource, category, _countScratch);
                counts[i] = _countScratch.Count;
            }

            _countScratch.Clear();
            return counts;
        }

        private ICatalogPrefabSource UnitPrefabSource =>
            _unitPrefabSourceOverride ?? unitPrefabRegistryConfig as ICatalogPrefabSource;

        private ICatalogPrefabSource BuildingPrefabSource =>
            _buildingPrefabSourceOverride ?? buildingPlacementConfig as ICatalogPrefabSource;

        private static bool[] BuildEnabledStates(int[] counts)
        {
            bool[] states = new bool[4];
            for (int i = 0; i < states.Length; i++)
                states[i] = counts != null && i < counts.Length && counts[i] > 0;

            return states;
        }

        private void PopulateItems(IReadOnlyList<BuildDrawerCatalogItem> items)
        {
            ClearItemBindings();
            ClearRuntimeItems();
            HideStaticPlaceholderItems();

            BuildDrawerItemView template = view.ItemTemplate;
            if (template == null)
            {
                ClearDetail();
                return;
            }

            if (items == null || items.Count == 0)
            {
                template.gameObject.SetActive(false);
                ClearDetail();
                return;
            }

            BindItem(template, items[0]);
            template.gameObject.SetActive(true);
            for (int i = 1; i < items.Count; i++)
            {
                BuildDrawerItemView item = Instantiate(template, view.ItemContentRoot, false);
                item.gameObject.name = $"ItemView - {items[i].DisplayName}";
                BindItem(item, items[i]);
                item.gameObject.SetActive(true);
                _runtimeItems.Add(item);
            }

            SelectItem(template, items[0]);
        }

        private void BindItem(BuildDrawerItemView item, BuildDrawerCatalogItem model)
        {
            if (item == null)
                return;

            item.gameObject.name = item == view.ItemTemplate ? "ItemView" : $"ItemView - {model.DisplayName}";
            item.BindText(
                model.DisplayName,
                model.TypeLabel,
                model.Description,
                FormatPrice(model.Price),
                string.Empty,
                FormatDuration(model),
                FormatRequirements(model));
            item.BindThumbnail(model.CardPortrait);
            item.SetInteractable(true);
            item.SetSelected(false, view.SelectedItemFrameSprite);

            Button button = item.SelectionButton;
            if (button == null)
                return;

            UnityAction action = () => SelectItem(item, model);
            button.onClick.AddListener(action);
            _itemBindings.Add(new ButtonBinding(button, action));
        }

        private void SelectItem(BuildDrawerItemView item, BuildDrawerCatalogItem model)
        {
            if (_selectedItemView != null && _selectedItemView != item)
                _selectedItemView.SetSelected(false, view.SelectedItemFrameSprite);

            _selectedItemView = item;
            _selectedItemView?.SetSelected(true, view.SelectedItemFrameSprite);
            _selectedItem = model;
            _hasSelectedItem = true;
            BindDetail(model);
            ApplyInstructionForCurrentSelection();
        }

        private void OnPrimaryActionClicked()
        {
            if (!_hasSelectedItem || _selectedItem.Prefab == null)
            {
                ApplyBuildDrawerCommandResult(BuildingUiCommandFailure.InvalidSelection, string.Empty);
                return;
            }

            if (_uiCommandSystem == null)
            {
                string connecting = GameText.Get("build.drawer.failure.connecting", "Build drawer is still connecting. Try again in a moment.");
                ApplyInstruction(connecting, BuildDrawerInstructionSeverity.Error);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    GameText.Get("build.feedback.drawer_not_ready", "Build drawer is not ready.")));
                return;
            }

            BuildingUiCommandFailure failure = _uiCommandSystem.TryRequestCampItem(
                _selectedItem.Prefab,
                _selectedItem.Price,
                out string requiredBuildingDisplayName,
                false);

            if (failure != BuildingUiCommandFailure.None)
            {
                ApplyBuildDrawerCommandResult(failure, requiredBuildingDisplayName);
                return;
            }

            if (_selectedItem.Category == BuildDrawerCategory.Buildings)
            {
                ApplyInstruction(GameText.Format("build.drawer.action.place_choose_footprint", "Place {0}: choose a valid footprint.", _selectedItem.DisplayName), BuildDrawerInstructionSeverity.Ready);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Success(GameText.Get("build.feedback.place_building", "PLACE BUILDING")));
                _closeDrawer?.Invoke();
                return;
            }

            ApplyInstruction(FormatPrimarySuccessInstruction(_selectedItem), BuildDrawerInstructionSeverity.Ready);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Success(
                GameText.Format("build.feedback.production_requested", "{0}: {1}", _selectedItem.ActionLabel, _selectedItem.DisplayName)));
            RefreshQueue();
        }

        private void OnCancelProductionClicked()
        {
            if (_pendingProductions.Count == 0 ||
                _pendingProductions[0].PendingProductionIndex < 0 ||
                _uiCommandSystem == null)
            {
                string unavailable = GameText.Get("build.feedback.production_cancel_unavailable", "Production cancel unavailable.");
                ApplyInstruction(unavailable, BuildDrawerInstructionSeverity.Error);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    unavailable));
                return;
            }

            BuildingPendingProductionUiEntry active = _pendingProductions[0];
            bool cancelled = _uiCommandSystem.CancelProduction(active.BuildingId, active.PendingProductionIndex);
            ApplyInstruction(cancelled
                    ? GameText.Format("build.feedback.production_cancelled_named", "Cancelled {0}.", ResolveQueueDisplayName(active))
                    : GameText.Get("build.feedback.production_cancel_unavailable", "Production cancel unavailable."),
                cancelled ? BuildDrawerInstructionSeverity.Warning : BuildDrawerInstructionSeverity.Error);

            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, cancelled
                ? TacticalCommandResult.Success(GameText.Get("build.feedback.production_cancelled", "PRODUCTION CANCELLED"))
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, GameText.Get("build.feedback.production_cancel_unavailable", "Production cancel unavailable.")));
            RefreshQueue();
        }

        private void OnClearProductionsClicked()
        {
            if (_pendingProductions.Count == 0 || _uiCommandSystem == null)
            {
                string empty = GameText.Get("build.feedback.production_queue_empty", "Production queue is empty.");
                ApplyInstruction(empty, BuildDrawerInstructionSeverity.Warning);
                BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                    TacticalCommandReasonCode.BuildUnavailable,
                    empty));
                return;
            }

            _clearProductionScratch.Clear();
            for (int i = 0; i < _pendingProductions.Count; i++)
            {
                BuildingPendingProductionUiEntry entry = _pendingProductions[i];
                if (entry.PendingProductionIndex >= 0)
                    _clearProductionScratch.Add(entry);
            }

            _clearProductionScratch.Sort(CompareProductionCancelOrder);

            int cancelledCount = 0;
            for (int i = 0; i < _clearProductionScratch.Count; i++)
            {
                BuildingPendingProductionUiEntry entry = _clearProductionScratch[i];
                if (_uiCommandSystem.CancelProduction(entry.BuildingId, entry.PendingProductionIndex))
                    cancelledCount++;
            }

            _clearProductionScratch.Clear();
            ApplyInstruction(cancelledCount > 0
                    ? GameText.Get("build.feedback.production_queue_cleared_sentence", "Production queue cleared.")
                    : GameText.Get("build.feedback.production_clear_unavailable", "Production clear unavailable."),
                cancelledCount > 0 ? BuildDrawerInstructionSeverity.Warning : BuildDrawerInstructionSeverity.Error);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, cancelledCount > 0
                ? TacticalCommandResult.Success(GameText.Get("build.feedback.production_queue_cleared", "PRODUCTION QUEUE CLEARED"))
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, GameText.Get("build.feedback.production_clear_unavailable", "Production clear unavailable.")));
            RefreshQueue();
        }

        private void RefreshQueue()
        {
            HideRuntimeQueueItems();
            if (view == null)
                return;

            BuildDrawerQueueItemView activeItem = view.ActiveItemView;
            BuildDrawerQueueItemView queuedTemplate = view.QueuedItemTemplate;
            HideStaticQueuePlaceholderItems(activeItem, queuedTemplate);
            if (_uiQuerySystem == null)
            {
                ApplyEmptyQueue(activeItem, queuedTemplate);
                return;
            }

            _uiQuerySystem.GetFriendlyPendingProductionUiEntries(_pendingProductions);
            ApplyQueueEntries();
        }

        private void ApplyQueueEntries()
        {
            HideRuntimeQueueItems();
            if (view == null)
                return;

            BuildDrawerQueueItemView activeItem = view.ActiveItemView;
            BuildDrawerQueueItemView queuedTemplate = view.QueuedItemTemplate;
            HideStaticQueuePlaceholderItems(activeItem, queuedTemplate);
            if (_pendingProductions.Count == 0)
            {
                ApplyEmptyQueue(activeItem, queuedTemplate);
                return;
            }

            BuildingPendingProductionUiEntry active = _pendingProductions[0];
            BindQueueItem(activeItem, active, 1);
            if (activeItem != null)
                activeItem.gameObject.SetActive(true);

            if (queuedTemplate != null)
            {
                if (_pendingProductions.Count > 1)
                {
                    BindQueueItem(queuedTemplate, _pendingProductions[1], 2);
                    queuedTemplate.gameObject.SetActive(true);
                }
                else
                {
                    queuedTemplate.gameObject.SetActive(false);
                }

                for (int i = 2; i < _pendingProductions.Count; i++)
                {
                    BuildDrawerQueueItemView item = GetOrCreateRuntimeQueueItem(i - 2, queuedTemplate);
                    if (item == null)
                        continue;

                    item.gameObject.name = $"ProductionItemView - {ResolveQueueDisplayName(_pendingProductions[i])}";
                    BindQueueItem(item, _pendingProductions[i], i + 1);
                    item.gameObject.SetActive(true);
                }

                HideUnusedRuntimeQueueItems(Mathf.Max(0, _pendingProductions.Count - 2));
            }

            view.ApplyQueueSummary(
                true,
                active.Progress01,
                FormatPercent(active.Progress01),
                FormatRemaining(active.RemainingSeconds),
                _pendingProductions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            view.ApplySecondaryQueueControls(
                _uiCommandSystem != null && active.PendingProductionIndex >= 0,
                false,
                _uiCommandSystem != null && _pendingProductions.Count > 0);
        }

        private void BindDetail(BuildDrawerCatalogItem model)
        {
            view.BindDetail(
                model.DisplayName,
                model.TypeLabel,
                model.Description,
                FormatPrice(model.Price),
                string.Empty,
                FormatDuration(model),
                FormatPlacement(model),
                FormatRequirements(model),
                model.ActionPortrait,
                model.CardPortrait,
                model.ActionLabel,
                true);
        }

        private void ClearDetail()
        {
            _selectedItemView = null;
            _hasSelectedItem = false;
            view.BindDetail(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "No requestable items.",
                null,
                null,
                string.Empty,
                false);
            ApplyInstruction(FormatEmptyCategoryInstruction(_activeCategory), BuildDrawerInstructionSeverity.Warning);
        }

        private void HideStaticPlaceholderItems()
        {
            RectTransform root = view.ItemContentRoot;
            BuildDrawerItemView template = view.ItemTemplate;
            if (root == null || template == null)
                return;

            Transform templateTransform = template.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != templateTransform)
                    child.gameObject.SetActive(false);
            }
        }

        private void HideStaticQueuePlaceholderItems(
            BuildDrawerQueueItemView activeItem,
            BuildDrawerQueueItemView queuedTemplate)
        {
            if (view == null)
                return;

            RectTransform root = view.QueueContentRoot;
            if (root == null)
                return;

            Transform activeTransform = activeItem != null ? activeItem.transform : null;
            Transform queuedTemplateTransform = queuedTemplate != null ? queuedTemplate.transform : null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child != activeTransform && child != queuedTemplateTransform)
                    child.gameObject.SetActive(false);
            }
        }

        private void ClearRuntimeItems()
        {
            for (int i = _runtimeItems.Count - 1; i >= 0; i--)
            {
                BuildDrawerItemView item = _runtimeItems[i];
                if (item == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(item.gameObject);
                else
                    DestroyImmediate(item.gameObject);
            }

            _runtimeItems.Clear();
            if (view != null && view.ItemTemplate != null)
                view.ItemTemplate.SetSelected(false, view.SelectedItemFrameSprite);
        }

        private void ClearRuntimeQueueItems()
        {
            for (int i = _runtimeQueueItems.Count - 1; i >= 0; i--)
            {
                BuildDrawerQueueItemView item = _runtimeQueueItems[i];
                if (item == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(item.gameObject);
                else
                    DestroyImmediate(item.gameObject);
            }

            _runtimeQueueItems.Clear();
        }

        private void ClearItemBindings()
        {
            for (int i = 0; i < _itemBindings.Count; i++)
            {
                ButtonBinding binding = _itemBindings[i];
                if (binding.Button != null)
                    binding.Button.onClick.RemoveListener(binding.Action);
            }

            _itemBindings.Clear();
        }

        private void UnwirePrimaryAction()
        {
            if (_primaryActionButton != null && _primaryActionListener != null)
                _primaryActionButton.onClick.RemoveListener(_primaryActionListener);

            _primaryActionButton = null;
            _primaryActionListener = null;
        }

        private void UnwireQueueControls()
        {
            UnwireCancelButton();
            UnwireClearButton();
        }

        private void UnwireCancelButton()
        {
            if (_cancelButton != null && _cancelButtonListener != null)
                _cancelButton.onClick.RemoveListener(_cancelButtonListener);

            _cancelButton = null;
            _cancelButtonListener = null;
        }

        private void UnwireClearButton()
        {
            if (_clearButton != null && _clearButtonListener != null)
                _clearButton.onClick.RemoveListener(_clearButtonListener);

            _clearButton = null;
            _clearButtonListener = null;
        }

        private void ClearTabBindings()
        {
            for (int i = 0; i < _tabBindings.Count; i++)
            {
                ButtonBinding binding = _tabBindings[i];
                if (binding.Button != null)
                    binding.Button.onClick.RemoveListener(binding.Action);
            }

            _tabBindings.Clear();
        }

        private bool HasTabBinding(Button button)
        {
            for (int i = 0; i < _tabBindings.Count; i++)
            {
                if (_tabBindings[i].Button == button)
                    return true;
            }

            return false;
        }

        private static string FormatPrice(int price)
        {
            return Mathf.Max(0, price).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string FormatDuration(BuildDrawerCatalogItem model)
        {
            if (model.ProductionDurationSeconds <= 0f)
                return "-";

            int seconds = Mathf.CeilToInt(model.ProductionDurationSeconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        private static string FormatPlacement(BuildDrawerCatalogItem model)
        {
            return model.Category == BuildDrawerCategory.Buildings
                ? $"{model.FootprintCells.x}x{model.FootprintCells.y}"
                : "-";
        }

        private static string FormatRequirements(BuildDrawerCatalogItem model)
        {
            return model.Category switch
            {
                BuildDrawerCategory.Buildings => GameText.Get("build.drawer.requirements.buildings", "Valid footprint required."),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.requirements.aircraft", "Requires compatible air production."),
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.requirements.vehicles", "Requires compatible vehicle production."),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.requirements.soldiers", "Requires compatible recruitment building."),
                _ => string.Empty
            };
        }

        private void ApplyEmptyQueue(BuildDrawerQueueItemView activeItem, BuildDrawerQueueItemView queuedTemplate)
        {
            if (activeItem != null)
                activeItem.gameObject.SetActive(false);
            if (queuedTemplate != null)
                queuedTemplate.gameObject.SetActive(false);
            HideRuntimeQueueItems();

            view.ApplyQueueSummary(false, 0f, string.Empty, string.Empty, string.Empty);
            view.ApplySecondaryQueueControls(false, false, false);
        }

        private BuildDrawerQueueItemView GetOrCreateRuntimeQueueItem(int poolIndex, BuildDrawerQueueItemView queuedTemplate)
        {
            if (poolIndex < 0 || queuedTemplate == null || view == null || view.QueueContentRoot == null)
                return null;

            while (_runtimeQueueItems.Count <= poolIndex)
            {
                BuildDrawerQueueItemView item = Instantiate(queuedTemplate, view.QueueContentRoot, false);
                item.gameObject.SetActive(false);
                _runtimeQueueItems.Add(item);
            }

            return _runtimeQueueItems[poolIndex];
        }

        private void HideRuntimeQueueItems()
        {
            HideUnusedRuntimeQueueItems(0);
        }

        private void HideUnusedRuntimeQueueItems(int usedCount)
        {
            int clampedUsedCount = Mathf.Clamp(usedCount, 0, _runtimeQueueItems.Count);
            for (int i = clampedUsedCount; i < _runtimeQueueItems.Count; i++)
            {
                BuildDrawerQueueItemView item = _runtimeQueueItems[i];
                if (item != null)
                    item.gameObject.SetActive(false);
            }
        }

        private void BindQueueItem(BuildDrawerQueueItemView item, BuildingPendingProductionUiEntry entry, int queueNumber)
        {
            if (item == null)
                return;

            item.Bind(
                queueNumber,
                ResolveQueueDisplayName(entry),
                string.IsNullOrWhiteSpace(entry.ProducerDisplayName) ? $"Building {entry.BuildingId}" : entry.ProducerDisplayName,
                FormatRemaining(entry.RemainingSeconds),
                entry.Progress01,
                ResolveQueueThumbnail(entry),
                queueNumber == 1 && _uiCommandSystem != null && entry.PendingProductionIndex >= 0);
        }

        private string ResolveQueueDisplayName(BuildingPendingProductionUiEntry entry)
        {
            return _query.TryResolvePrefab(UnitPrefabSource, BuildingPrefabSource, entry.Prefab, out BuildDrawerCatalogItem item)
                ? item.DisplayName
                : entry.Prefab != null ? entry.Prefab.name : GameText.Get("build.drawer.production.fallback_name", "Production");
        }

        private Sprite ResolveQueueThumbnail(BuildingPendingProductionUiEntry entry)
        {
            return _query.TryResolvePrefab(UnitPrefabSource, BuildingPrefabSource, entry.Prefab, out BuildDrawerCatalogItem item)
                ? item.CardPortrait
                : null;
        }

        private static string FormatRemaining(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatPercent(float progress01)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
        }

        private static int CompareProductionCancelOrder(
            BuildingPendingProductionUiEntry left,
            BuildingPendingProductionUiEntry right)
        {
            int buildingComparison = left.BuildingId.CompareTo(right.BuildingId);
            if (buildingComparison != 0)
                return buildingComparison;

            return right.PendingProductionIndex.CompareTo(left.PendingProductionIndex);
        }

        private void ApplyBuildDrawerCommandResult(
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName)
        {
            ApplyInstruction(FormatInstructionFailureMessage(failure, requiredBuildingDisplayName), BuildDrawerInstructionSeverity.Error);
            TacticalCommandReasonCode reason = failure == BuildingUiCommandFailure.NotEnoughMoney
                ? TacticalCommandReasonCode.InsufficientResources
                : TacticalCommandReasonCode.BuildUnavailable;
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Rejected(
                reason,
                FormatFailureMessage(failure, requiredBuildingDisplayName)));
        }

        private string FormatFailureMessage(
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName)
        {
            return failure switch
            {
                BuildingUiCommandFailure.NotEnoughMoney => GameText.Get("build.drawer.failure.short.not_enough_money", "Insufficient credits."),
                BuildingUiCommandFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    GameText.Format("build.drawer.failure.short.requires_named", "Requires {0}.", requiredBuildingDisplayName),
                BuildingUiCommandFailure.MissingProducerBuilding => GameText.Get("build.drawer.failure.short.missing_producer", "Required producer is missing."),
                BuildingUiCommandFailure.ProductionQueueFull when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    GameText.Format("build.drawer.failure.short.queue_full_named", "{0} production slots are full.", requiredBuildingDisplayName),
                BuildingUiCommandFailure.ProductionQueueFull => GameText.Get("build.drawer.failure.short.queue_full", "All compatible production slots are full."),
                BuildingUiCommandFailure.GlobalProductionQueueFull => GameText.Format("build.drawer.failure.short.global_queue_full", "Production queue limit reached ({0} max).", FormatMaxQueuedUnitProductions()),
                BuildingUiCommandFailure.InvalidSelection => GameText.Get("build.drawer.failure.invalid_selection", "Select a build drawer item first."),
                _ => GameText.Get("build.drawer.failure.short.unavailable", "Build request unavailable.")
            };
        }

        private void ApplyInstructionForCurrentSelection()
        {
            if (view == null)
                return;

            if (!_hasSelectedItem || _selectedItem.Prefab == null)
            {
                ApplyInstruction(FormatEmptyCategoryInstruction(_activeCategory), BuildDrawerInstructionSeverity.Warning);
                return;
            }

            if (_uiCommandSystem == null)
            {
                ApplyInstruction(FormatReadyInstruction(_selectedItem), BuildDrawerInstructionSeverity.Ready);
                return;
            }

            BuildingUiCommandFailure failure = _uiCommandSystem.GetCampRequestFailure(
                _selectedItem.Prefab,
                _selectedItem.Price,
                out string requiredBuildingDisplayName);

            if (failure == BuildingUiCommandFailure.None)
            {
                if (_selectedItem.Category == BuildDrawerCategory.Buildings &&
                    _uiCommandSystem.HasPendingBuildingPlacement)
                {
                    string status = _uiCommandSystem.PlacementStatusText;
                    bool canConfirm = _uiCommandSystem.CanConfirmBuildingPlacement;
                    ApplyInstruction(
                        canConfirm
                            ? GameText.Format("build.drawer.instruction.place_pending_confirm", "Place {0}: drag to position, then confirm.", _selectedItem.DisplayName)
                            : GameText.Format("build.drawer.instruction.cannot_place_here", "Cannot place here: {0}.", FormatPlacementStatus(status)),
                        canConfirm ? BuildDrawerInstructionSeverity.Ready : BuildDrawerInstructionSeverity.Error);
                    return;
                }

                ApplyInstruction(FormatReadyInstruction(_selectedItem), BuildDrawerInstructionSeverity.Ready);
                return;
            }

            ApplyInstruction(FormatInstructionFailureMessage(failure, requiredBuildingDisplayName), BuildDrawerInstructionSeverity.Error);
        }

        private void ApplyInstruction(string text, BuildDrawerInstructionSeverity severity)
        {
            view?.ApplyInstruction(text, severity);
        }

        private string FormatInstructionFailureMessage(
            BuildingUiCommandFailure failure,
            string requiredBuildingDisplayName)
        {
            string itemName = _hasSelectedItem ? _selectedItem.DisplayName : GameText.Get("build.drawer.item.fallback_name", "item");
            return failure switch
            {
                BuildingUiCommandFailure.NotEnoughMoney =>
                    GameText.Format("build.drawer.failure.not_enough_money", "Need {0} more credits to {1} {2}.", FormatMissingCredits(), FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName),
                BuildingUiCommandFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    GameText.Format("build.drawer.failure.missing_producer_named", "Cannot {0} {1}: requires {2}.", FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName, requiredBuildingDisplayName),
                BuildingUiCommandFailure.MissingProducerBuilding =>
                    GameText.Format("build.drawer.failure.missing_producer", "Cannot {0} {1}: {2}.", FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName, FormatMissingProducerFallback(_selectedItem.Category)),
                BuildingUiCommandFailure.ProductionQueueFull when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                    GameText.Format("build.drawer.failure.queue_full_named", "Cannot {0} {1}: all {2} production slots are full.", FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName, requiredBuildingDisplayName),
                BuildingUiCommandFailure.ProductionQueueFull =>
                    GameText.Format("build.drawer.failure.queue_full", "Cannot {0} {1}: all compatible production slots are full.", FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName),
                BuildingUiCommandFailure.GlobalProductionQueueFull =>
                    GameText.Format("build.drawer.failure.global_queue_full", "Cannot {0} {1}: production queue limit reached ({2} max).", FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName, FormatMaxQueuedUnitProductions()),
                BuildingUiCommandFailure.InvalidSelection => GameText.Get("build.drawer.failure.invalid_selection", "Select a build drawer item first."),
                _ => GameText.Format("build.drawer.failure.unavailable", "Cannot {0} {1}: request unavailable.", FormatActionVerb(_selectedItem.Category).ToLowerInvariant(), itemName)
            };
        }

        private int FormatMaxQueuedUnitProductions()
        {
            return Mathf.Max(0, _uiCommandSystem != null ? _uiCommandSystem.MaxQueuedUnitProductions : 25);
        }

        private int FormatMissingCredits()
        {
            int current = _uiCommandSystem != null
                ? _uiCommandSystem.CurrentDollars
                : 0;
            return Mathf.Max(0, _selectedItem.Price - current);
        }

        private static string FormatReadyInstruction(BuildDrawerCatalogItem model)
        {
            return model.Category switch
            {
                BuildDrawerCategory.Buildings => GameText.Format("build.drawer.ready.buildings", "PLACE: choose a location for {0}.", model.DisplayName),
                BuildDrawerCategory.Vehicles => GameText.Format("build.drawer.ready.vehicles", "PRODUCE: add {0} to the vehicle queue.", model.DisplayName),
                BuildDrawerCategory.Aircrafts => GameText.Format("build.drawer.ready.aircraft", "PRODUCE: add {0} to the aircraft queue.", model.DisplayName),
                BuildDrawerCategory.Soldiers => GameText.Format("build.drawer.ready.soldiers", "RECRUIT: add {0} to the training queue.", model.DisplayName),
                _ => GameText.Format("build.drawer.ready.default", "Select {0}.", model.DisplayName)
            };
        }

        private static string FormatPrimarySuccessInstruction(BuildDrawerCatalogItem model)
        {
            return model.Category == BuildDrawerCategory.Soldiers
                ? GameText.Format("build.drawer.success.recruitment_queued", "{0} added to recruitment queue.", model.DisplayName)
                : GameText.Format("build.drawer.success.production_queued", "{0} added to production queue.", model.DisplayName);
        }

        private static string FormatEmptyCategoryInstruction(BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Buildings => GameText.Get("build.drawer.empty.buildings", "No requestable buildings are configured."),
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.empty.vehicles", "No requestable vehicles are configured."),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.empty.aircraft", "No requestable aircraft are configured."),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.empty.soldiers", "No requestable soldiers are configured."),
                _ => GameText.Get("build.drawer.empty.select_item", "Select an item to place, produce, or recruit.")
            };
        }

        private static string FormatMissingProducerFallback(BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.missing_producer.vehicles", "no compatible vehicle producer is available"),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.missing_producer.aircraft", "no compatible air producer is available"),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.missing_producer.soldiers", "no compatible training building is available"),
                _ => GameText.Get("build.drawer.missing_producer.default", "required producer is missing")
            };
        }

        private static string FormatActionVerb(BuildDrawerCategory category)
        {
            return category switch
            {
                BuildDrawerCategory.Buildings => GameText.Get("build.drawer.verb.place", "Place"),
                BuildDrawerCategory.Soldiers => GameText.Get("build.drawer.verb.recruit", "Recruit"),
                BuildDrawerCategory.Vehicles => GameText.Get("build.drawer.verb.produce", "Produce"),
                BuildDrawerCategory.Aircrafts => GameText.Get("build.drawer.verb.produce", "Produce"),
                _ => GameText.Get("build.drawer.verb.request", "Request")
            };
        }

        private static string FormatPlacementStatus(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? GameText.Get("build.drawer.placement.invalid", "invalid placement") : status;
        }

        private readonly struct ButtonBinding
        {
            public readonly Button Button;
            public readonly UnityAction Action;

            public ButtonBinding(Button button, UnityAction action)
            {
                Button = button;
                Action = action;
            }
        }
    }
}
