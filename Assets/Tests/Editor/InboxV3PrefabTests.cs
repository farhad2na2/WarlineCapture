#if UNITY_EDITOR
using System;
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

public sealed class InboxV3PrefabTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            InboxV3PrefabBuilder.Build();
            InboxV3PrefabTests suite = new();
            suite.MenuScene_AssignsInboxAndRouteMountsTargetBody(); passed++;
            suite.Prefab_UsesResponsiveTargetStructureSharedArtAndConstantBorders(); passed++;
            suite.CategoriesSearchReadAndAttachmentActionsAreInteractive(); passed++;
            suite.Navigation_UsesBackSettingsAndDistrictIntelRoutes(); passed++;
            Debug.Log($"[InboxV3PrefabTests] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[InboxV3PrefabTests] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void MenuScene_AssignsInboxAndRouteMountsTargetBody()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.InboxContentPrefab);
        Assert.AreEqual("SCN15_InboxContent", content.InboxContentPrefab.name);
        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = RegionChild(content.ShellView, UIShellRegionId.HeaderRegion);
        content.InstallMenuRouteBody(UIRoute.Inbox);
        GameObject headerAfter = RegionChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter);
        GameObject mounted = RegionChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.NotNull(mounted.GetComponent<InboxV3View>());
    }

    [Test]
    public void Prefab_UsesResponsiveTargetStructureSharedArtAndConstantBorders()
    {
        GameObject prefab = RequirePrefab();
        InboxV3View view = prefab.GetComponent<InboxV3View>();
        Assert.NotNull(view);
        Assert.AreEqual(5, view.CategoryButtons.Length);
        Assert.AreEqual(5, view.MessageButtons.Length);
        string[] required =
        {
            "Header", "CategoryRail", "MessagePanel", "SearchInput", "SortButton",
            "DetailPanel", "DetailArtClip", "Attachment_0", "Attachment_1",
            "MarkAllReadButton", "MarkReadButton", "ViewIntelButton"
        };
        for (int i = 0; i < required.Length; i++)
            Assert.NotNull(Find(prefab.transform, required[i]), required[i]);

        MainMenuV3SectionLayoutView layout = prefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(layout);
        Assert.AreEqual(new Vector2(1672f, 941f), layout.ReferenceResolution);
        Assert.IsTrue(layout.ExpandToCanvasWidth);
        Assert.GreaterOrEqual(layout.RightAnchoredTargets.Length, 4);
        SerializedProperty widthTargets = new SerializedObject(layout).FindProperty("widthExpandedTargets");
        Assert.GreaterOrEqual(widthTargets.arraySize, 2);

        RawImage detail = Find(prefab.transform, "DetailArt").GetComponent<RawImage>();
        Assert.NotNull(detail.texture);
        AspectRatioFitter fitter = detail.GetComponent<AspectRatioFitter>();
        Assert.NotNull(fitter);
        Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, fitter.aspectMode);

        V3GradientGraphic[] gradients = prefab.GetComponentsInChildren<V3GradientGraphic>(true);
        Assert.GreaterOrEqual(gradients.Length, 29);
        int bordered = 0;
        for (int i = 0; i < gradients.Length; i++)
        {
            SerializedObject serialized = new(gradients[i]);
            Color color = serialized.FindProperty("borderColor").colorValue;
            float width = serialized.FindProperty("borderWidth").floatValue;
            if (color.a <= .01f) continue;
            bordered++;
            Assert.AreEqual(3f, width, .001f, gradients[i].name);
        }
        Assert.GreaterOrEqual(bordered, 24);

        foreach (RawImage image in prefab.GetComponentsInChildren<RawImage>(true))
        {
            if (image.texture == null) continue;
            string path = AssetDatabase.GetAssetPath(image.texture);
            Assert.IsTrue(path.StartsWith("Assets/Game/Art/UI/V3Shared/", StringComparison.Ordinal),
                $"Inbox must reuse shared V3 art: {image.name} -> {path}");
        }
    }

    [Test]
    public void CategoriesSearchReadAndAttachmentActionsAreInteractive()
    {
        GameObject instance = UnityEngine.Object.Instantiate(RequirePrefab());
        try
        {
            InboxV3View view = instance.GetComponent<InboxV3View>();
            TMP_Text detailTitle = Find(instance.transform, "DetailTitle").GetComponent<TMP_Text>();
            TMP_Text firstBadge = Find(instance.transform, "Category_0").Find("Badge/Count").GetComponent<TMP_Text>();
            TMP_Text attachmentState = Find(instance.transform, "Attachment_0").Find("State").GetComponent<TMP_Text>();
            Assert.AreEqual("5", firstBadge.text);

            view.CategoryButtons[2].onClick.Invoke();
            Assert.AreEqual("ARIA TACTICAL REVIEW", detailTitle.text);
            Assert.IsTrue(view.MessageButtons[0].gameObject.activeSelf);
            Assert.IsFalse(view.MessageButtons[1].gameObject.activeSelf);

            view.CategoryButtons[0].onClick.Invoke();
            view.SearchInput.text = "Ranger";
            Assert.AreEqual("RANGER SQUAD UNLOCKED", detailTitle.text);
            Assert.IsTrue(view.MessageButtons[0].gameObject.activeSelf);
            Assert.IsFalse(view.MessageButtons[1].gameObject.activeSelf);

            view.MarkReadButton.onClick.Invoke();
            Assert.AreEqual("4", firstBadge.text);
            Find(instance.transform, "Attachment_0").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("OPEN VIA INTEL", attachmentState.text);

            view.SearchInput.text = string.Empty;
            view.MarkAllReadButton.onClick.Invoke();
            Assert.AreEqual("0", firstBadge.text);
            for (int i = 0; i < view.MessageButtons.Length; i++)
            {
                Transform unread = view.MessageButtons[i].transform.Find("UnreadBar");
                if (unread != null) Assert.IsFalse(unread.gameObject.activeSelf);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Navigation_UsesBackSettingsAndDistrictIntelRoutes()
    {
        GameObject prefab = RequirePrefab();
        AssertRoute(prefab, "BackButton", UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu);
        AssertRoute(prefab, "SettingsButton", UiShellRouteIntent.OpenSettings, UIRoute.Settings);
        AssertRoute(prefab, "ViewIntelButton", UiShellRouteIntent.OpenMenuRoute, UIRoute.DistrictDetail);
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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InboxV3PrefabBuilder.PrefabPath);
        Assert.NotNull(prefab, InboxV3PrefabBuilder.PrefabPath);
        return prefab;
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
