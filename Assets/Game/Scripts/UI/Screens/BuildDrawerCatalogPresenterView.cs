using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BuildDrawerCatalogPresenterView : MonoBehaviour
{
    [SerializeField] private BuildDrawerView view;
    [SerializeField] private UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig;
    [SerializeField] private BuildingPlacementSystemConfig buildingPlacementConfig;

    private readonly BuildDrawerCatalogQuerySystem _query = new();
    private readonly List<BuildDrawerCatalogItem> _items = new();
    private readonly List<BuildDrawerCatalogItem> _countScratch = new();
    private readonly List<BuildingUiQuerySystem.PendingProductionUiEntry> _pendingProductions = new();
    private readonly List<BuildDrawerItemView> _runtimeItems = new();
    private readonly List<BuildDrawerQueueItemView> _runtimeQueueItems = new();
    private readonly List<ButtonBinding> _tabBindings = new();
    private readonly List<ButtonBinding> _itemBindings = new();
    private BuildDrawerCategory _activeCategory = BuildDrawerCategory.Buildings;
    private BuildDrawerItemView _selectedItemView;
    private BuildDrawerCatalogItem _selectedItem;
    private bool _hasSelectedItem;
    private BuildingUiCommandSystem _uiCommandSystem;
    private BuildingUiCommandSystem.Context _uiCommandContext;
    private BuildingUiQuerySystem _uiQuerySystem;
    private BuildingUiQuerySystem.Context _uiQueryContext;
    private Action _closeDrawer;
    private Button _primaryActionButton;
    private UnityAction _primaryActionListener;
    private Button _cancelButton;
    private UnityAction _cancelButtonListener;

    private void Awake()
    {
        if (view == null)
            view = GetComponent<BuildDrawerView>();
    }

    private void OnEnable()
    {
        WireTabs();
        WirePrimaryAction();
        WireQueueControls();
        Refresh();
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
    }

    public void ConfigureForTests(
        BuildDrawerView drawerView,
        UnitPrefabRegistryAuthoringConfig unitRegistry,
        BuildingPlacementSystemConfig buildingPlacement)
    {
        view = drawerView;
        unitPrefabRegistryConfig = unitRegistry;
        buildingPlacementConfig = buildingPlacement;
    }

    public void SelectCategoryForTests(BuildDrawerCategory category)
    {
        SelectCategory(category);
    }

    public void RefreshForTests()
    {
        Refresh();
    }

    public void ApplyQueueSnapshotForTests(IReadOnlyList<BuildingUiQuerySystem.PendingProductionUiEntry> entries)
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
        BuildingUiCommandSystem uiCommandSystem,
        BuildingUiCommandSystem.Context uiCommandContext,
        Action closeDrawer)
    {
        _uiCommandSystem = uiCommandSystem;
        _uiCommandContext = uiCommandContext;
        _closeDrawer = closeDrawer;
        WirePrimaryAction();
        WireQueueControls();
    }

    internal void BindRuntimeQueries(
        BuildingUiQuerySystem uiQuerySystem,
        BuildingUiQuerySystem.Context uiQueryContext)
    {
        _uiQuerySystem = uiQuerySystem;
        _uiQueryContext = uiQueryContext;
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

        Button button = view.CancelButton;
        if (button == null || button == _cancelButton)
            return;

        UnwireQueueControls();
        _cancelButton = button;
        _cancelButtonListener = OnCancelProductionClicked;
        _cancelButton.onClick.RemoveListener(_cancelButtonListener);
        _cancelButton.onClick.AddListener(_cancelButtonListener);
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

        _query.Collect(unitPrefabRegistryConfig, buildingPlacementConfig, _activeCategory, _items);
        PopulateItems(_items);
        RefreshQueue();
    }

    private int[] CountCategories()
    {
        int[] counts = new int[4];
        for (int i = 0; i < counts.Length; i++)
        {
            BuildDrawerCategory category = (BuildDrawerCategory)i;
            _query.Collect(unitPrefabRegistryConfig, buildingPlacementConfig, category, _countScratch);
            counts[i] = _countScratch.Count;
        }

        _countScratch.Clear();
        return counts;
    }

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
    }

    private void OnPrimaryActionClicked()
    {
        Debug.Log(
            $"BUILD_DRAWER_PRIMARY_CLICK selected={_hasSelectedItem} item={(_hasSelectedItem ? _selectedItem.DisplayName : "<none>")} category={(_hasSelectedItem ? _selectedItem.Category.ToString() : "<none>")} commandBound={_uiCommandSystem != null} prefab={(_hasSelectedItem && _selectedItem.Prefab != null ? _selectedItem.Prefab.name : "<null>")} frame={Time.frameCount}");

        if (!_hasSelectedItem || _selectedItem.Prefab == null)
        {
            ApplyBuildDrawerCommandResult(BuildingUiCommandSystem.CampRequestFailure.InvalidSelection, string.Empty);
            return;
        }

        if (_uiCommandSystem == null)
        {
            BattleHudRuntimeFeedbackSystem.ApplyCommandResult(TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                "Build drawer is not ready."));
            return;
        }

        BuildingUiCommandSystem.CampRequestFailure failure = _uiCommandSystem.TryRequestCampItem(
            _uiCommandContext,
            _selectedItem.Prefab,
            _selectedItem.Price,
            out string requiredBuildingDisplayName,
            false);

        if (failure != BuildingUiCommandSystem.CampRequestFailure.None)
        {
            Debug.LogWarning(
                $"BUILD_DRAWER_PRIMARY_FAILED item={_selectedItem.DisplayName} category={_selectedItem.Category} failure={failure} required={requiredBuildingDisplayName} frame={Time.frameCount}");
            ApplyBuildDrawerCommandResult(failure, requiredBuildingDisplayName);
            return;
        }

        if (_selectedItem.Category == BuildDrawerCategory.Buildings)
        {
            Debug.Log(
                $"BUILD_DRAWER_PRIMARY_SUCCESS action=Place item={_selectedItem.DisplayName} frame={Time.frameCount}");
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(TacticalCommandMode.Build);
            BattleHudRuntimeFeedbackSystem.ApplyCommandResult(TacticalCommandResult.Success("PLACE BUILDING"));
            _closeDrawer?.Invoke();
            return;
        }

        Debug.Log(
            $"BUILD_DRAWER_PRIMARY_SUCCESS action={_selectedItem.ActionLabel} item={_selectedItem.DisplayName} frame={Time.frameCount}");
        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(TacticalCommandResult.Success($"{_selectedItem.ActionLabel}: {_selectedItem.DisplayName}"));
        Refresh();
    }

    private void OnCancelProductionClicked()
    {
        if (_pendingProductions.Count == 0 ||
            _pendingProductions[0].PendingProductionIndex < 0 ||
            _uiCommandSystem == null)
        {
            BattleHudRuntimeFeedbackSystem.ApplyCommandResult(TacticalCommandResult.Rejected(
                TacticalCommandReasonCode.BuildUnavailable,
                "Production cancel unavailable."));
            return;
        }

        BuildingUiQuerySystem.PendingProductionUiEntry active = _pendingProductions[0];
        bool cancelled = _uiCommandSystem.CancelProduction(
            _uiCommandContext,
            active.BuildingId,
            active.PendingProductionIndex);

        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(cancelled
            ? TacticalCommandResult.Success("PRODUCTION CANCELLED")
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, "Production cancel unavailable."));
        Refresh();
    }

    private void RefreshQueue()
    {
        ClearRuntimeQueueItems();
        if (view == null)
            return;

        BuildDrawerQueueItemView activeItem = view.ActiveItemView;
        BuildDrawerQueueItemView queuedTemplate = view.QueuedItemTemplate;
        if (_uiQuerySystem == null)
        {
            ApplyEmptyQueue(activeItem, queuedTemplate);
            return;
        }

        _uiQuerySystem.GetFriendlyPendingProductionUiEntries(_uiQueryContext, _pendingProductions);
        ApplyQueueEntries();
    }

    private void ApplyQueueEntries()
    {
        ClearRuntimeQueueItems();
        if (view == null)
            return;

        BuildDrawerQueueItemView activeItem = view.ActiveItemView;
        BuildDrawerQueueItemView queuedTemplate = view.QueuedItemTemplate;
        if (_pendingProductions.Count == 0)
        {
            ApplyEmptyQueue(activeItem, queuedTemplate);
            return;
        }

        BuildingUiQuerySystem.PendingProductionUiEntry active = _pendingProductions[0];
        BindQueueItem(activeItem, active);
        if (activeItem != null)
            activeItem.gameObject.SetActive(true);

        if (queuedTemplate != null)
        {
            if (_pendingProductions.Count > 1)
            {
                BindQueueItem(queuedTemplate, _pendingProductions[1]);
                queuedTemplate.gameObject.SetActive(true);
            }
            else
            {
                queuedTemplate.gameObject.SetActive(false);
            }

            for (int i = 2; i < _pendingProductions.Count; i++)
            {
                BuildDrawerQueueItemView item = Instantiate(queuedTemplate, view.QueueContentRoot, false);
                item.gameObject.name = $"ProductionItemView - {ResolveQueueDisplayName(_pendingProductions[i])}";
                BindQueueItem(item, _pendingProductions[i]);
                item.gameObject.SetActive(true);
                _runtimeQueueItems.Add(item);
            }
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
            false);
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
        if (_cancelButton != null && _cancelButtonListener != null)
            _cancelButton.onClick.RemoveListener(_cancelButtonListener);

        _cancelButton = null;
        _cancelButtonListener = null;
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
            BuildDrawerCategory.Buildings => "Valid footprint required.",
            BuildDrawerCategory.Aircrafts => "Requires compatible air production.",
            BuildDrawerCategory.Vehicles => "Requires compatible vehicle production.",
            BuildDrawerCategory.Soldiers => "Requires compatible recruitment building.",
            _ => string.Empty
        };
    }

    private void ApplyEmptyQueue(BuildDrawerQueueItemView activeItem, BuildDrawerQueueItemView queuedTemplate)
    {
        if (activeItem != null)
            activeItem.gameObject.SetActive(false);
        if (queuedTemplate != null)
            queuedTemplate.gameObject.SetActive(false);

        view.ApplyQueueSummary(false, 0f, string.Empty, string.Empty, string.Empty);
        view.ApplySecondaryQueueControls(false, false, false);
    }

    private void BindQueueItem(BuildDrawerQueueItemView item, BuildingUiQuerySystem.PendingProductionUiEntry entry)
    {
        if (item == null)
            return;

        item.Bind(
            ResolveQueueDisplayName(entry),
            string.IsNullOrWhiteSpace(entry.ProducerDisplayName) ? $"Building {entry.BuildingId}" : entry.ProducerDisplayName,
            FormatRemaining(entry.RemainingSeconds),
            entry.Progress01,
            ResolveQueueThumbnail(entry),
            false);
    }

    private string ResolveQueueDisplayName(BuildingUiQuerySystem.PendingProductionUiEntry entry)
    {
        return _query.TryResolvePrefab(unitPrefabRegistryConfig, buildingPlacementConfig, entry.Prefab, out BuildDrawerCatalogItem item)
            ? item.DisplayName
            : entry.Prefab != null ? entry.Prefab.name : "Production";
    }

    private Sprite ResolveQueueThumbnail(BuildingUiQuerySystem.PendingProductionUiEntry entry)
    {
        return _query.TryResolvePrefab(unitPrefabRegistryConfig, buildingPlacementConfig, entry.Prefab, out BuildDrawerCatalogItem item)
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

    private static void ApplyBuildDrawerCommandResult(
        BuildingUiCommandSystem.CampRequestFailure failure,
        string requiredBuildingDisplayName)
    {
        TacticalCommandReasonCode reason = failure == BuildingUiCommandSystem.CampRequestFailure.NotEnoughMoney
            ? TacticalCommandReasonCode.InsufficientResources
            : TacticalCommandReasonCode.BuildUnavailable;
        BattleHudRuntimeFeedbackSystem.ApplyCommandResult(TacticalCommandResult.Rejected(
            reason,
            FormatFailureMessage(failure, requiredBuildingDisplayName)));
    }

    private static string FormatFailureMessage(
        BuildingUiCommandSystem.CampRequestFailure failure,
        string requiredBuildingDisplayName)
    {
        return failure switch
        {
            BuildingUiCommandSystem.CampRequestFailure.NotEnoughMoney => "Insufficient credits.",
            BuildingUiCommandSystem.CampRequestFailure.MissingProducerBuilding when !string.IsNullOrWhiteSpace(requiredBuildingDisplayName) =>
                $"Requires {requiredBuildingDisplayName}.",
            BuildingUiCommandSystem.CampRequestFailure.MissingProducerBuilding => "Required producer is missing.",
            BuildingUiCommandSystem.CampRequestFailure.InvalidSelection => "Select a build drawer item first.",
            _ => "Build request unavailable."
        };
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
