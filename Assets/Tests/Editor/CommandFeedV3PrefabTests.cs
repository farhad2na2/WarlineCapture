#if UNITY_EDITOR
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommandFeedV3PrefabTests
{
    [Test]
    public void Prefab_UsesResponsiveSharedV3CompositionAndRealRoutes()
    {
        GameObject prefab = RequirePrefab();
        CommandFeedScreenView view = prefab.GetComponent<CommandFeedScreenView>();
        Assert.NotNull(view);
        Assert.AreEqual(5, view.FilterButtons.Length);
        Assert.AreEqual(5, view.FeedRows.Length);
        Assert.NotNull(view.PauseButton);
        Assert.NotNull(view.SearchButton);
        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.IsTrue(layout.ExpandToCanvasWidth);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        AssertRoute(prefab.transform, "BackButton", UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu);
        AssertRoute(prefab.transform, "ViewOperationButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations);
        AssertRoute(prefab.transform, "OpenIntelButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.Inbox);
        Transform logo = Find(prefab.transform, "WarlineLogo");
        Assert.NotNull(logo);
        bool foundCanonicalLogo = false;
        foreach (Image image in logo.GetComponentsInChildren<Image>(true))
        {
            if (AssetDatabase.GetAssetPath(image.sprite) == V3UiFoundationBuilder.MainMenuLogoPath)
                foundCanonicalLogo = true;
        }
        Assert.IsTrue(foundCanonicalLogo, "SCN-18 must reuse the canonical V3 logo asset.");
    }

    [Test]
    public void FiltersSearchAndPause_ChangeLiveScreenState()
    {
        GameObject instance = Object.Instantiate(RequirePrefab());
        try
        {
            CommandFeedScreenView view = instance.GetComponent<CommandFeedScreenView>();
            Assert.NotNull(view);
            view.SelectFilter((int)CommandFeedCategory.Aria);
            for (int i = 0; i < view.FeedRows.Length; i++)
                Assert.AreEqual(i == 1, view.FeedRows[i].gameObject.activeSelf, $"ARIA filter row mismatch at {i}.");

            view.ToggleSearch();
            Assert.IsTrue(view.IsSearchActive);
            for (int i = 0; i < view.FeedRows.Length; i++)
                Assert.AreEqual(i == 1 || i == 4, view.FeedRows[i].gameObject.activeSelf, $"Search result row mismatch at {i}.");

            bool before = view.IsPaused;
            view.TogglePause();
            Assert.AreNotEqual(before, view.IsPaused);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    public static void RunFocusedValidation()
    {
        var tests = new CommandFeedV3PrefabTests();
        tests.Prefab_UsesResponsiveSharedV3CompositionAndRealRoutes();
        tests.FiltersSearchAndPause_ChangeLiveScreenState();
        Debug.Log("[CommandFeedV3FocusedValidation] result=Passed tests=2 filters=interactive search=interactive pause=interactive");
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CommandFeedV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab, $"Missing {CommandFeedV3PrefabBuilder.PrefabPath}");
        return prefab;
    }

    private static void AssertRoute(Transform root, string name, UiShellRouteIntent intent, UIRoute route)
    {
        Transform target = Find(root, name);
        Assert.NotNull(target, $"Missing {name}");
        Button button = target.GetComponent<Button>();
        UIShellRouteButtonView routeView = target.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(button, $"{name} must be a real Button.");
        Assert.IsTrue(button.interactable, $"{name} must be interactable.");
        Assert.NotNull(routeView, $"{name} must submit a route.");
        Assert.AreEqual(intent, routeView.Intent);
        Assert.AreEqual(route, routeView.Route);
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = Find(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
