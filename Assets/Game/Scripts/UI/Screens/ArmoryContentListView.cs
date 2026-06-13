using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ArmoryContentListView : MonoBehaviour
{
    [SerializeField] private ScriptableObject unitPrefabRegistryConfig;
    [SerializeField] private ScriptableObject buildingPlacementConfig;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private ArmoryCatalogItemView itemTemplate;

    private readonly ArmoryCatalogQuerySystem catalogQuerySystem = new();
    private readonly List<ArmoryCatalogItem> itemScratch = new();
    private readonly List<ArmoryCatalogItemView> runtimeItems = new();
    private readonly List<ItemClickBinding> itemClickBindings = new();
    private ArmoryInspectionPanelView inspectionPanel;
    private ArmoryCatalogItem activeInspectionItem;
    private ArmoryCatalogCategory activeCategory = ArmoryCatalogCategory.Characters;
    private bool hasActiveInspectionItem;
    private ArmoryCatalogItemView activeItemView;
    private ICatalogPrefabSource unitPrefabSourceOverride;
    private ICatalogPrefabSource buildingPrefabSourceOverride;

    private void OnEnable()
    {
        activeCategory = ArmoryCatalogCategory.Characters;
        TryQueueBoundaryCategory(activeCategory);
        Refresh(activeCategory);
    }

    private void Update()
    {
        if (!TryReadBoundaryCategory(out ArmoryCatalogCategory category) || category == activeCategory)
            return;

        activeCategory = category;
        Refresh(activeCategory);
    }

    private void OnDisable()
    {
        ClearRuntimeItems();
    }

    public void ConfigureForTests(
        ICatalogPrefabSource unitRegistry,
        ICatalogPrefabSource buildingPlacement,
        RectTransform content,
        ArmoryCatalogItemView template,
        ArmoryInspectionPanelView inspection = null)
    {
        unitPrefabSourceOverride = unitRegistry;
        buildingPrefabSourceOverride = buildingPlacement;
        contentRoot = content;
        itemTemplate = template;
        inspectionPanel = inspection;
    }

    public void ConfigureCatalogMetadataResolvers(
        TryResolveUiBuildingCatalogMetadata tryResolveBuildingMetadata,
        TryResolveUiUnitCatalogMetadata tryResolveUnitMetadata)
    {
        catalogQuerySystem.ConfigureMetadataResolvers(tryResolveBuildingMetadata, tryResolveUnitMetadata);
    }

    public void SetInspectionPanel(ArmoryInspectionPanelView panel)
    {
        inspectionPanel = panel;
        if (hasActiveInspectionItem)
            BindInspectionPanel(activeInspectionItem);
        else
            ClearInspectionPanel();
    }

    public void RefreshForTests(ArmoryCatalogCategory category)
    {
        Refresh(category);
    }

    private void Refresh(ArmoryCatalogCategory category)
    {
        if (contentRoot == null || itemTemplate == null)
            return;

        catalogQuerySystem.Collect(UnitPrefabSource, BuildingPrefabSource, category, itemScratch);
        Populate(itemScratch);
    }

    private ICatalogPrefabSource UnitPrefabSource =>
        unitPrefabSourceOverride ?? unitPrefabRegistryConfig as ICatalogPrefabSource;

    private ICatalogPrefabSource BuildingPrefabSource =>
        buildingPrefabSourceOverride ?? buildingPlacementConfig as ICatalogPrefabSource;

    private void Populate(IReadOnlyList<ArmoryCatalogItem> items)
    {
        ClearRuntimeItems();
        itemTemplate.gameObject.SetActive(items != null && items.Count > 0);

        if (items == null || items.Count == 0)
        {
            ClearInspectionPanel();
            return;
        }

        itemTemplate.Bind(items[0]);
        itemTemplate.gameObject.name = $"ItemView - {items[0].DisplayName}";

        for (int i = 1; i < items.Count; i++)
        {
            ArmoryCatalogItemView item = Instantiate(itemTemplate, contentRoot, false);
            item.gameObject.name = $"ItemView - {items[i].DisplayName}";
            item.Bind(items[i]);
            WireItemSelection(item, items[i]);
            item.gameObject.SetActive(true);
            runtimeItems.Add(item);
        }

        WireItemSelection(itemTemplate, items[0]);
        BindInspectionPanel(items[0]);
        SetSelectedItem(itemTemplate);
    }

    private void ClearRuntimeItems()
    {
        ClearItemSelectionBindings();

        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            ArmoryCatalogItemView item = runtimeItems[i];
            if (item == null)
                continue;

            if (Application.isPlaying)
                Destroy(item.gameObject);
            else
                DestroyImmediate(item.gameObject);
        }

        runtimeItems.Clear();

        if (itemTemplate != null)
        {
            itemTemplate.gameObject.name = "ItemView";
            itemTemplate.SetSelected(false);
        }

        activeItemView = null;
    }

    private void ClearItemSelectionBindings()
    {
        for (int i = 0; i < itemClickBindings.Count; i++)
        {
            ItemClickBinding binding = itemClickBindings[i];
            if (binding.Button != null)
                binding.Button.onClick.RemoveListener(binding.Action);
        }

        itemClickBindings.Clear();
    }

    private void WireItemSelection(ArmoryCatalogItemView item, ArmoryCatalogItem model)
    {
        if (item == null)
            return;

        Button button = item.SelectionButton;
        if (button == null)
            return;

        UIShellRouteButtonView routeButton = button.GetComponent<UIShellRouteButtonView>();
        if (routeButton != null)
            routeButton.enabled = false;

        UnityEngine.Events.UnityAction action = () =>
        {
            BindInspectionPanel(model);
            SetSelectedItem(item);
        };
        button.onClick.AddListener(action);
        itemClickBindings.Add(new ItemClickBinding(button, action));
    }

    private void SetSelectedItem(ArmoryCatalogItemView item)
    {
        if (activeItemView != null && activeItemView != item)
            activeItemView.SetSelected(false);

        activeItemView = item;
        if (activeItemView != null)
            activeItemView.SetSelected(true);
    }

    private void BindInspectionPanel(ArmoryCatalogItem model)
    {
        activeInspectionItem = model;
        hasActiveInspectionItem = true;

        if (inspectionPanel != null)
            inspectionPanel.Bind(model);
    }

    private void ClearInspectionPanel()
    {
        hasActiveInspectionItem = false;

        if (inspectionPanel != null)
            inspectionPanel.Clear();
    }

    private bool TryReadBoundaryCategory(out ArmoryCatalogCategory category)
    {
        return UiShellRuntimeGateway.TryReadArmoryCategory(out category);
    }

    private bool TryQueueBoundaryCategory(ArmoryCatalogCategory category)
    {
        return UiShellRuntimeGateway.TryEnqueueArmoryCategory(category);
    }

    private readonly struct ItemClickBinding
    {
        public readonly Button Button;
        public readonly UnityEngine.Events.UnityAction Action;

        public ItemClickBinding(Button button, UnityEngine.Events.UnityAction action)
        {
            Button = button;
            Action = action;
        }
    }
}
