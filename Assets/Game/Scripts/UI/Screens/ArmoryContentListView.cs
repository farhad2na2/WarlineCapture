using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ArmoryContentListView : MonoBehaviour
{
    [SerializeField] private UnitPrefabRegistryAuthoringConfig unitPrefabRegistryConfig;
    [SerializeField] private BuildingPlacementSystemConfig buildingPlacementConfig;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject itemTemplate;

    private readonly ArmoryCatalogQuerySystem catalogQuerySystem = new();
    private readonly List<ArmoryCatalogItem> itemScratch = new();
    private readonly List<GameObject> runtimeItems = new();
    private ArmoryCatalogCategory activeCategory = ArmoryCatalogCategory.Characters;
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;

    private void OnEnable()
    {
        ResolveReferences();
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
        UnitPrefabRegistryAuthoringConfig unitRegistry,
        BuildingPlacementSystemConfig buildingPlacement,
        RectTransform content,
        GameObject template)
    {
        unitPrefabRegistryConfig = unitRegistry;
        buildingPlacementConfig = buildingPlacement;
        contentRoot = content;
        itemTemplate = template;
    }

    public void RefreshForTests(ArmoryCatalogCategory category)
    {
        Refresh(category);
    }

    private void Refresh(ArmoryCatalogCategory category)
    {
        ResolveReferences();
        if (contentRoot == null || itemTemplate == null)
            return;

        catalogQuerySystem.Collect(unitPrefabRegistryConfig, buildingPlacementConfig, category, itemScratch);
        Populate(itemScratch);
    }

    private void ResolveReferences()
    {
        if (contentRoot == null)
            contentRoot = transform.Find("Scroll View/Viewport/Content") as RectTransform;

        if (itemTemplate == null && contentRoot != null)
        {
            Transform template = contentRoot.Find("ItemView");
            if (template != null)
                itemTemplate = template.gameObject;
        }
    }

    private void Populate(IReadOnlyList<ArmoryCatalogItem> items)
    {
        ClearRuntimeItems();
        itemTemplate.SetActive(items != null && items.Count > 0);

        if (items == null || items.Count == 0)
            return;

        BindItem(itemTemplate, items[0]);
        itemTemplate.name = $"ItemView - {items[0].DisplayName}";

        for (int i = 1; i < items.Count; i++)
        {
            GameObject item = Instantiate(itemTemplate, contentRoot, false);
            item.name = $"ItemView - {items[i].DisplayName}";
            BindItem(item, items[i]);
            item.SetActive(true);
            runtimeItems.Add(item);
        }
    }

    private void ClearRuntimeItems()
    {
        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            GameObject item = runtimeItems[i];
            if (item == null)
                continue;

            if (Application.isPlaying)
                Destroy(item);
            else
                DestroyImmediate(item);
        }

        runtimeItems.Clear();

        if (itemTemplate != null)
            itemTemplate.name = "ItemView";
    }

    private static void BindItem(GameObject item, ArmoryCatalogItem model)
    {
        if (item == null)
            return;

        TMP_Text title = FindComponent<TMP_Text>(item.transform, "Frame/Title");
        if (title != null)
            title.text = model.DisplayName;

        Image art = BindCategoryBackgroundAndGetArt(item.transform, model.Category);
        if (art != null)
        {
            art.sprite = model.Portrait;
            art.preserveAspect = true;
            art.enabled = model.Portrait != null;
        }

        TMP_Text type = FindComponent<TMP_Text>(item.transform, "Frame/Progress/Type");
        if (type != null)
            type.text = FormatCategory(model.Category);
    }

    private static Image BindCategoryBackgroundAndGetArt(Transform itemRoot, ArmoryCatalogCategory category)
    {
        Transform selectedBackground = null;
        string selectedBackgroundName = GetBackgroundName(category);

        SetBackgroundActive(itemRoot, "Background_Character", selectedBackgroundName, ref selectedBackground);
        SetBackgroundActive(itemRoot, "Background_Vehicle", selectedBackgroundName, ref selectedBackground);
        SetBackgroundActive(itemRoot, "Background_Aircraft", selectedBackgroundName, ref selectedBackground);
        SetBackgroundActive(itemRoot, "Background_Building", selectedBackgroundName, ref selectedBackground);

        Image categoryArt = selectedBackground != null
            ? selectedBackground.Find("Art")?.GetComponent<Image>()
            : null;
        if (categoryArt != null)
            return categoryArt;

        return FindComponent<Image>(itemRoot, "Art");
    }

    private static void SetBackgroundActive(
        Transform itemRoot,
        string backgroundName,
        string selectedBackgroundName,
        ref Transform selectedBackground)
    {
        Transform background = itemRoot != null ? itemRoot.Find(backgroundName) : null;
        if (background == null)
            return;

        bool selected = backgroundName == selectedBackgroundName;
        background.gameObject.SetActive(selected);
        if (selected)
            selectedBackground = background;
    }

    private static string GetBackgroundName(ArmoryCatalogCategory category)
    {
        return category switch
        {
            ArmoryCatalogCategory.Aircrafts => "Background_Aircraft",
            ArmoryCatalogCategory.Buildings => "Background_Building",
            ArmoryCatalogCategory.Vehicles => "Background_Vehicle",
            _ => "Background_Character"
        };
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        if (root == null)
            return null;

        Transform child = root.Find(path);
        return child != null ? child.GetComponent<T>() : null;
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

    private static string FormatCategory(ArmoryCatalogCategory category)
    {
        return category switch
        {
            ArmoryCatalogCategory.Aircrafts => "AIRCRAFT",
            ArmoryCatalogCategory.Buildings => "BUILDING",
            ArmoryCatalogCategory.Vehicles => "VEHICLE",
            ArmoryCatalogCategory.Support => "SUPPORT",
            _ => "CHARACTER"
        };
    }
}
