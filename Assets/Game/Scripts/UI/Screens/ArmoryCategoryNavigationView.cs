using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ArmoryCategoryNavigationView : MonoBehaviour
{
    private readonly List<ButtonBinding> bindings = new();
    private EntityQuery boundaryQuery;
    private World cachedWorld;
    private bool hasBoundaryQuery;

    private void OnEnable()
    {
        Wire("Nav_Characters", ArmoryCatalogCategory.Characters);
        Wire("Nav_Vehicles", ArmoryCatalogCategory.Vehicles);
        Wire("Nav_Aircrafts", ArmoryCatalogCategory.Aircrafts);
        Wire("Nav_Buildings", ArmoryCatalogCategory.Buildings);
        Wire("Nav_Support", ArmoryCatalogCategory.Support);
    }

    private void OnDisable()
    {
        for (int i = 0; i < bindings.Count; i++)
            bindings[i].Button.onClick.RemoveListener(bindings[i].Action);

        bindings.Clear();
    }

    private void Wire(string navName, ArmoryCatalogCategory category)
    {
        Transform nav = FindDeep(transform, navName);
        if (nav == null)
            return;

        Button button = FindButton(nav);
        if (button == null)
            return;

        UnityEngine.Events.UnityAction action = () => SelectCategory(category);
        button.onClick.AddListener(action);
        bindings.Add(new ButtonBinding(button, action));
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

    private readonly struct ButtonBinding
    {
        public readonly Button Button;
        public readonly UnityEngine.Events.UnityAction Action;

        public ButtonBinding(Button button, UnityEngine.Events.UnityAction action)
        {
            Button = button;
            Action = action;
        }
    }
}
