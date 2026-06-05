using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ArmoryCategoryNavigationView : MonoBehaviour
{
    private readonly List<TabBinding> bindings = new();
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private Sprite selectedFrameSprite;
    private Sprite inactiveFrameSprite;
    private ArmoryCatalogCategory activeCategory = ArmoryCatalogCategory.Characters;
    private bool hasBoundaryQuery;

    private void Awake()
    {
        DisableRouteButton("Nav_Characters");
        DisableRouteButton("Nav_Vehicles");
        DisableRouteButton("Nav_Aircrafts");
        DisableRouteButton("Nav_Buildings");
        DisableRouteButton("Nav_Support");
        WireAll();
    }

    private void OnEnable()
    {
        WireAll();
    }

    private void WireAll()
    {
        if (bindings.Count > 0)
            return;

        Wire("Nav_Characters", ArmoryCatalogCategory.Characters);
        Wire("Nav_Vehicles", ArmoryCatalogCategory.Vehicles);
        Wire("Nav_Aircrafts", ArmoryCatalogCategory.Aircrafts);
        Wire("Nav_Buildings", ArmoryCatalogCategory.Buildings);
        Wire("Nav_Support", ArmoryCatalogCategory.Support);
        activeCategory = TryReadCategory(out ArmoryCatalogCategory category)
            ? category
            : ArmoryCatalogCategory.Characters;
        ApplyVisualState(activeCategory);
    }

    private void OnDisable()
    {
        for (int i = 0; i < bindings.Count; i++)
            bindings[i].Button.onClick.RemoveListener(bindings[i].Action);

        bindings.Clear();
    }

    private void Update()
    {
        if (!TryReadCategory(out ArmoryCatalogCategory category) || category == activeCategory)
            return;

        activeCategory = category;
        ApplyVisualState(category);
    }

    private void Wire(string navName, ArmoryCatalogCategory category)
    {
        Transform nav = FindDeep(transform, navName);
        if (nav == null)
            return;

        Button button = FindButton(nav);
        if (button == null)
            return;

        DisableRouteButtonComponent(button);

        UnityEngine.Events.UnityAction action = () => SelectCategory(category);
        button.onClick.AddListener(action);

        Image frame = nav.Find("Frame")?.GetComponent<Image>();
        CacheFrameSprite(category, frame);
        bindings.Add(new TabBinding(category, button, frame, action));
    }

    private void DisableRouteButton(string navName)
    {
        Transform nav = FindDeep(transform, navName);
        if (nav == null)
            return;

        Button button = FindButton(nav);
        DisableRouteButtonComponent(button);
    }

    private static void DisableRouteButtonComponent(Button button)
    {
        if (button == null)
            return;

        WarlineCaptureShellRouteButtonView routeButton = button.GetComponent<WarlineCaptureShellRouteButtonView>();
        if (routeButton != null)
            routeButton.enabled = false;
    }

    private void SelectCategory(ArmoryCatalogCategory category)
    {
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return;

        DynamicBuffer<UiShellArmoryCategoryRequestComponent> requests =
            entityManager.GetBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        requests.Add(new UiShellArmoryCategoryRequestComponent
        {
            Category = category
        });
        activeCategory = category;
        ApplyVisualState(category);
    }

    private bool TryReadCategory(out ArmoryCatalogCategory category)
    {
        category = ArmoryCatalogCategory.Characters;
        if (!TryGetBoundary(out EntityManager entityManager, out Entity boundary))
            return false;

        category = entityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary).Category;
        return true;
    }

    private void CacheFrameSprite(ArmoryCatalogCategory category, Image frame)
    {
        if (frame == null || frame.sprite == null)
            return;

        string spriteName = frame.sprite.name.ToLowerInvariant();
        if (category == ArmoryCatalogCategory.Characters || spriteName.Contains("selected"))
            selectedFrameSprite ??= frame.sprite;
        else
            inactiveFrameSprite ??= frame.sprite;
    }

    private void ApplyVisualState(ArmoryCatalogCategory category)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            Image frame = bindings[i].Frame;
            if (frame == null)
                continue;

            bool selected = bindings[i].Category == category;
            Sprite sprite = selected ? selectedFrameSprite : inactiveFrameSprite;
            if (sprite != null)
                frame.sprite = sprite;
        }
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

    private static Button FindButton(Transform nav)
    {
        if (nav == null)
            return null;

        if (nav.TryGetComponent(out Button navButton))
            return navButton;

        Transform hotspot = nav.Find("Frame/Hotspot");
        if (hotspot != null && hotspot.TryGetComponent(out Button hotspotButton))
            return hotspotButton;

        return nav.GetComponentInChildren<Button>(true);
    }

    private static Transform FindDeep(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform matched = FindDeep(root.GetChild(i), targetName);
            if (matched != null)
                return matched;
        }

        return null;
    }

    private readonly struct TabBinding
    {
        public readonly ArmoryCatalogCategory Category;
        public readonly Button Button;
        public readonly Image Frame;
        public readonly UnityEngine.Events.UnityAction Action;

        public TabBinding(
            ArmoryCatalogCategory category,
            Button button,
            Image frame,
            UnityEngine.Events.UnityAction action)
        {
            Category = category;
            Button = button;
            Frame = frame;
            Action = action;
        }
    }
}
