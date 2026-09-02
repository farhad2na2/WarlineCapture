#if UNITY_EDITOR
using System;
using System.Reflection;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EventsV3PrefabTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            EventsV3PrefabBuilder.Build();
            EventsV3PrefabTests suite = new();
            suite.MenuScene_AssignsEventsAndRouteMountsTargetBody(); passed++;
            suite.Prefab_UsesResponsiveSharedArtAndConstantBorders(); passed++;
            suite.TabsAndEventCards_UpdateTheSelectedEvent(); passed++;
            suite.Navigation_UsesLogoSettingsAndOperationRoutes(); passed++;
            Debug.Log($"[EventsV3PrefabTests] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[EventsV3PrefabTests] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void MenuScene_AssignsEventsAndRouteMountsTargetBody()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.EventsContentPrefab);
        Assert.AreEqual("SCN16_EventsContent", content.EventsContentPrefab.name);
        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = RegionChild(content.ShellView, UIShellRegionId.HeaderRegion);
        content.InstallMenuRouteBody(UIRoute.Events);
        GameObject headerAfter = RegionChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter);
        Assert.NotNull(RegionChild(content.ShellView, UIShellRegionId.PopupLayer).GetComponent<EventsV3View>());
    }

    [Test]
    public void Prefab_UsesResponsiveSharedArtAndConstantBorders()
    {
        GameObject prefab = RequirePrefab();
        EventsV3View view = prefab.GetComponent<EventsV3View>();
        Assert.NotNull(view);
        Assert.AreEqual(4, view.TabButtons.Length);
        Assert.AreEqual(3, view.EventButtons.Length);
        string[] required =
        {
            "Header", "CategoryRail", "EventCards", "EventCard_0", "EventCard_1", "EventCard_2",
            "DetailPanel", "Objective_0", "Modifier_0", "Reward_0", "EnterOperationButton"
        };
        for (int i = 0; i < required.Length; i++) Assert.NotNull(Find(prefab.transform, required[i]), required[i]);

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);
        Assert.GreaterOrEqual(layout.RightAnchoredTargets.Length, 4);

        foreach (RawImage image in prefab.GetComponentsInChildren<RawImage>(true))
        {
            Assert.NotNull(image.texture, image.name);
            Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, image.GetComponent<AspectRatioFitter>()?.aspectMode, image.name);
            string path = AssetDatabase.GetAssetPath(image.texture);
            Assert.IsTrue(path.StartsWith("Assets/Game/Art/UI/V3Shared/", StringComparison.Ordinal), path);
        }

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.GreaterOrEqual(gradients.Length, 45);
        int bordered = 0;
        for (int i = 0; i < gradients.Length; i++)
        {
            SerializedObject data = new(gradients[i]);
            Color border = data.FindProperty("borderColor").colorValue;
            if (border.a <= .01f) continue;
            bordered++;
            Assert.AreEqual(3f, data.FindProperty("borderWidth").floatValue, .001f, gradients[i].name);
        }
        Assert.GreaterOrEqual(bordered, 35);
    }

    [Test]
    public void TabsAndEventCards_UpdateTheSelectedEvent()
    {
        GameObject instance = UnityEngine.Object.Instantiate(RequirePrefab());
        try
        {
            EventsV3View view = instance.GetComponent<EventsV3View>();
            InvokeAwake(view);
            TMP_Text firstCardTitle = Find(instance.transform, "EventCard_0").Find("Title").GetComponent<TMP_Text>();
            TMP_Text detailTitle = Find(instance.transform, "DetailTitle").GetComponent<TMP_Text>();
            TMP_Text detailTimer = Find(instance.transform, "DetailTimer").GetComponent<TMP_Text>();

            view.TabButtons[1].onClick.Invoke();
            Assert.AreEqual("EAST RIDGE DEFENSE", firstCardTitle.text);
            Assert.AreEqual("EAST RIDGE DEFENSE", detailTitle.text);

            view.EventButtons[2].onClick.Invoke();
            Assert.AreEqual("SIGNAL BLACKOUT", detailTitle.text);
            Assert.AreEqual("STARTS 5D REMAINING", detailTimer.text);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Navigation_UsesLogoSettingsAndOperationRoutes()
    {
        GameObject prefab = RequirePrefab();
        AssertRoute(prefab, "LogoPanel", UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu);
        AssertRoute(prefab, "SettingsButton", UiShellRouteIntent.OpenSettings, UIRoute.Settings);
        AssertRoute(prefab, "EnterOperationButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations);
    }

    private static void AssertRoute(GameObject prefab, string name, UiShellRouteIntent intent, UIRoute route)
    {
        UIShellRouteButtonView view = Find(prefab.transform, name)?.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(view, name);
        Assert.AreEqual(intent, view.Intent, name);
        Assert.AreEqual(route, view.Route, name);
    }

    private static GameObject RequirePrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EventsV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab, EventsV3PrefabBuilder.PrefabPath);
        return prefab;
    }

    private static void InvokeAwake(EventsV3View view)
    {
        MethodInfo awake = typeof(EventsV3View).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(awake);
        awake.Invoke(view, null);
    }

    private static GameObject RegionChild(UIShellView shell, UIShellRegionId id)
    {
        Assert.IsTrue(shell.TryGetRegion(id, out UIShellRegionView region));
        Assert.NotNull(region.ContentRoot);
        Assert.Greater(region.ContentRoot.childCount, 0, id.ToString());
        return region.ContentRoot.GetChild(0).gameObject;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
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
