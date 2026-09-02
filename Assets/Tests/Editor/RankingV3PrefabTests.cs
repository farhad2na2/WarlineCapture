#if UNITY_EDITOR
using System;
using System.Reflection;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RankingV3PrefabTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RankingV3PrefabBuilder.Build();
            RankingV3PrefabTests suite = new();
            suite.Prefab_MatchesResponsiveGradientAndArtContracts(); passed++;
            suite.TabsAndRewardsButton_SwitchVisibleBodies(); passed++;
            suite.MenuScene_AssignsAndMountsRankingRoute(); passed++;
            suite.Navigation_UsesLogoAndSettingsRoutes(); passed++;
            Debug.Log($"[RankingV3PrefabTests] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[RankingV3PrefabTests] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Prefab_MatchesResponsiveGradientAndArtContracts()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RankingV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab);
        RankingV3View view = prefab.GetComponent<RankingV3View>();
        Assert.NotNull(view);
        Assert.AreEqual(4, view.CategoryButtons.Length);
        Assert.AreEqual(4, view.CategoryBodies.Length);
        Assert.NotNull(view.ViewRewardsButton);

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.GreaterOrEqual(gradients.Length, 38);
        foreach (V3GradientGraphic gradient in gradients)
        {
            SerializedObject serialized = new(gradient);
            if (serialized.FindProperty("borderColor").colorValue.a <= .01f) continue;
            Assert.AreEqual(3f, serialized.FindProperty("borderWidth").floatValue, .001f, gradient.name);
        }

        foreach (RawImage image in prefab.GetComponentsInChildren<RawImage>(true))
        {
            if (image.texture == null) continue;
            Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent,
                image.GetComponent<AspectRatioFitter>()?.aspectMode, image.name);
            string path = AssetDatabase.GetAssetPath(image.texture);
            Assert.IsTrue(path.StartsWith("Assets/Game/Art/UI/Portraits/", StringComparison.Ordinal) ||
                          path.StartsWith("Assets/Game/Art/Narrative/FirstLaunch/Dialogue/Portraits/", StringComparison.Ordinal),
                $"Ranking must reuse the shared portrait library: {path}");
        }
    }

    [Test]
    public void TabsAndRewardsButton_SwitchVisibleBodies()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RankingV3PrefabBuilder.PrefabPath);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            RankingV3View view = instance.GetComponent<RankingV3View>();
            InvokeAwake(view);
            Assert.IsTrue(view.CategoryBodies[0].activeSelf);
            view.CategoryButtons[2].onClick.Invoke();
            Assert.IsTrue(view.CategoryBodies[2].activeSelf);
            Assert.IsFalse(view.CategoryBodies[0].activeSelf);
            view.ViewRewardsButton.onClick.Invoke();
            Assert.IsTrue(view.CategoryBodies[3].activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void MenuScene_AssignsAndMountsRankingRoute()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.RankingContentPrefab);
        Assert.AreEqual("SCN17_RankingContent", content.RankingContentPrefab.name);
        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        content.InstallMenuRouteBody(UIRoute.Ranking);
        Assert.IsTrue(content.ShellView.TryGetRegion(UIShellRegionId.PopupLayer, out UIShellRegionView popup));
        Assert.NotNull(popup.ContentRoot.GetComponentInChildren<RankingV3View>(true));
    }

    [Test]
    public void Navigation_UsesLogoAndSettingsRoutes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RankingV3PrefabBuilder.PrefabPath);
        AssertRoute(prefab, "LogoPanel", UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu);
        AssertRoute(prefab, "SettingsButton", UiShellRouteIntent.OpenSettings, UIRoute.Settings);
    }

    private static void AssertRoute(GameObject prefab, string name, UiShellRouteIntent intent, UIRoute route)
    {
        UIShellRouteButtonView view = Find(prefab.transform, name)?.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(view, name);
        Assert.AreEqual(intent, view.Intent, name);
        Assert.AreEqual(route, view.Route, name);
    }

    private static void InvokeAwake(RankingV3View view)
    {
        MethodInfo awake = typeof(RankingV3View).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(awake);
        awake.Invoke(view, null);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = Find(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
