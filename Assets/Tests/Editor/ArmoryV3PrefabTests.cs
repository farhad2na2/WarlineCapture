#if UNITY_EDITOR
using System;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class ArmoryV3PrefabTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            ArmoryV3PrefabBuilder.Build();
            ArmoryV3PrefabTests tests = new();
            tests.Prefab_UsesSixResponsiveSectionsAndConstantBorders(); passed++;
            tests.Catalog_ReusesRuntimePortraitsAndKeepsFiveWorkingCategories(); passed++;
            tests.Navigation_UsesOnlyExpectedV3Routes(); passed++;
            Debug.Log($"[ArmoryV3Validation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ArmoryV3Validation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_UsesSixResponsiveSectionsAndConstantBorders()
    {
        GameObject prefab = RequirePrefab();
        UIShellContentSectionsView sections =
            prefab.GetComponent<UIShellContentSectionsView>();
        Assert.NotNull(sections);
        Assert.AreEqual(6, sections.Sections.Count);
        foreach (UIShellContentSectionId id in Enum.GetValues(typeof(UIShellContentSectionId)))
            Assert.IsTrue(sections.TryGetSection(id, out _), id.ToString());

        MainMenuV3SectionLayoutView[] layouts =
            prefab.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.That(layouts.Length, Is.GreaterThanOrEqualTo(5));
        foreach (MainMenuV3SectionLayoutView layout in layouts)
            Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);

        Transform background = prefab.transform.Find(
            "MenuBackgroundContent/ArmoryBackground");
        Assert.NotNull(background);
        Assert.AreEqual(
            ArmoryV3PrefabBuilder.BackgroundPath,
            AssetDatabase.GetAssetPath(background.GetComponent<Image>().sprite));
        Assert.AreEqual(
            AspectRatioFitter.AspectMode.EnvelopeParent,
            background.GetComponent<AspectRatioFitter>().aspectMode);

        int bordered = 0;
        foreach (V3GradientGraphic gradient in
                 prefab.GetComponentsInChildren<V3GradientGraphic>(true))
        {
            SerializedObject serialized = new(gradient);
            float width = serialized.FindProperty("borderWidth").floatValue;
            Color color = serialized.FindProperty("borderColor").colorValue;
            if (color.a <= .01f)
                continue;
            bordered++;
            Assert.That(width, Is.EqualTo(3f).Within(.001f), gradient.name);
        }
        Assert.That(bordered, Is.GreaterThanOrEqualTo(25));
    }

    [Test]
    public void Catalog_ReusesRuntimePortraitsAndKeepsFiveWorkingCategories()
    {
        GameObject prefab = RequirePrefab();
        ArmoryCategoryNavigationView navigation =
            prefab.GetComponentInChildren<ArmoryCategoryNavigationView>(true);
        Assert.NotNull(navigation);
        SerializedProperty tabs = new SerializedObject(navigation).FindProperty("tabs");
        Assert.NotNull(tabs);
        Assert.AreEqual(5, tabs.arraySize);
        for (int i = 0; i < tabs.arraySize; i++)
        {
            Button button = tabs.GetArrayElementAtIndex(i)
                .FindPropertyRelative("button").objectReferenceValue as Button;
            Assert.NotNull(button);
            Assert.NotNull(button.GetComponent<ArmoryV3CategoryTabVisual>());
        }

        ArmoryContentListView list =
            prefab.GetComponentInChildren<ArmoryContentListView>(true);
        Assert.NotNull(list);
        SerializedObject listObject = new(list);
        Assert.NotNull(listObject.FindProperty("unitPrefabRegistryConfig").objectReferenceValue);
        Assert.NotNull(listObject.FindProperty("buildingPlacementConfig").objectReferenceValue);
        ArmoryCatalogItemView template = listObject.FindProperty("itemTemplate")
            .objectReferenceValue as ArmoryCatalogItemView;
        Assert.NotNull(template);
        Assert.NotNull(template.GetComponent<ArmoryV3CatalogItemVisual>());

        SerializedProperty visuals =
            new SerializedObject(template).FindProperty("categoryVisuals");
        Assert.AreEqual(5, visuals.arraySize);
        for (int i = 0; i < visuals.arraySize; i++)
        {
            Image art = visuals.GetArrayElementAtIndex(i)
                .FindPropertyRelative("artImage").objectReferenceValue as Image;
            Assert.NotNull(art);
            Assert.IsNull(art.sprite,
                "Card art must come from the existing runtime catalog, not duplicated prefab textures.");
            Assert.IsTrue(art.preserveAspect);
        }
    }

    [Test]
    public void Navigation_UsesOnlyExpectedV3Routes()
    {
        GameObject prefab = RequirePrefab();
        AssertRoute(prefab, "BackButton", UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu);
        AssertRoute(prefab, "CommanderProfileButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed);
        AssertRoute(prefab, "UpgradeButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory);
        AssertRoute(prefab, "EquipButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.Armory);

        Transform settings = Find(prefab.transform, "SettingsButton");
        Assert.NotNull(settings);
        UIShellActionButtonView action =
            settings.GetComponent<UIShellActionButtonView>();
        Assert.NotNull(action);
        Assert.AreEqual(UiActionKind.OpenSettings, action.ActionKind);

        foreach (UIShellRouteButtonView route in
                 prefab.GetComponentsInChildren<UIShellRouteButtonView>(true))
        {
            Assert.AreNotEqual(UIRoute.Operations, route.Route,
                $"Legacy Armory action {route.name} must not open Operations.");
        }
    }

    private static void AssertRoute(
        GameObject prefab,
        string name,
        UiShellRouteIntent intent,
        UIRoute route)
    {
        Transform target = Find(prefab.transform, name);
        Assert.NotNull(target, name);
        UIShellRouteButtonView button = target.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(button, name);
        Assert.AreEqual(intent, button.Intent, name);
        Assert.AreEqual(route, button.Route, name);
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            ArmoryV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        return prefab;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
