using System.Collections.Generic;
using Unity.Entities;
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
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasActiveInspectionItem;
    private bool hasBoundaryQuery;
    private ArmoryCatalogItemView activeItemView;
    private IUiCatalogPrefabSource unitPrefabSourceOverride;
    private IUiCatalogPrefabSource buildingPrefabSourceOverride;

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
        IUiCatalogPrefabSource unitRegistry,
        IUiCatalogPrefabSource buildingPlacement,
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

    private IUiCatalogPrefabSource UnitPrefabSource =>
        unitPrefabSourceOverride ?? unitPrefabRegistryConfig as IUiCatalogPrefabSource;

    private IUiCatalogPrefabSource BuildingPrefabSource =>
        buildingPrefabSourceOverride ?? buildingPlacementConfig as IUiCatalogPrefabSource;

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
        category = ArmoryCatalogCategory.Characters;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        UiShellArmoryCategoryComponent state = entityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary);
        category = state.Category;
        return true;
    }

    private bool TryQueueBoundaryCategory(ArmoryCatalogCategory category)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        DynamicBuffer<UiShellArmoryCategoryRequestComponent> requests =
            entityManager.GetBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        requests.Add(new UiShellArmoryCategoryRequestComponent
        {
            Category = category
        });
        return true;
    }

    private bool TryGetBoundary(out EntityManager entityManager, out Entity boundary)
    {
        entityManager = default;
        boundary = Entity.Null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        if (cachedWorld != world || !hasBoundaryQuery)
        {
            cachedWorld = world;
            boundaryQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellBoundaryComponent>());
            hasBoundaryQuery = true;
        }

        if (boundaryQuery.IsEmptyIgnoreFilter)
            return false;

        entityManager = world.EntityManager;
        boundary = boundaryQuery.GetSingletonEntity();
        if (!entityManager.HasComponent<UiShellArmoryCategoryComponent>(boundary))
        {
            entityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
            {
                Category = ArmoryCatalogCategory.Characters
            });
        }

        if (!entityManager.HasBuffer<UiShellArmoryCategoryRequestComponent>(boundary))
            entityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);

        return true;
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
