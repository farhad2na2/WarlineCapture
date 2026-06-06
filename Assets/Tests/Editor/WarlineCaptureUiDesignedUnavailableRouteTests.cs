using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiDesignedUnavailableRouteTests
{
    private static readonly RouteCase[] Cases =
    {
        new RouteCase("Screen_Inbox", WarlineCaptureRoute.Inbox, "COMMAND INBOX", "INBOX READY", "Assets/Game/Art/UI/Generated/Inbox/LayeredOneGo"),
        new RouteCase("Screen_Events", WarlineCaptureRoute.Events, "EVENTS", "NO ACTIVE EVENT", "Assets/Game/Art/UI/Generated/Events/LayeredOneGo"),
        new RouteCase("Screen_Ranking", WarlineCaptureRoute.Ranking, "RANKING", "LOCAL STATS ONLY", "Assets/Game/Art/UI/Generated/Ranking/LayeredOneGo"),
        new RouteCase("Screen_CommandFeed", WarlineCaptureRoute.CommandFeed, "COMMAND FEED", "LOCAL FEED ACTIVE", "Assets/Game/Art/UI/Generated/CommandFeed/LayeredOneGo")
    };

    [Test]
    public void DesignedUnavailableRouteScreens_HaveLayeredHierarchyAndRoutes()
    {
        foreach (RouteCase routeCase in Cases)
        {
            GameObject prefab = LoadPrefab(routeCase);
            WarlineCaptureScreenSystem controller = prefab.GetComponent<WarlineCaptureScreenSystem>();
            Assert.NotNull(controller, routeCase.ScreenName);
            Assert.AreEqual(routeCase.Route, controller.Route, routeCase.ScreenName);
            Assert.IsNull(prefab.GetComponent<Image>(), $"{routeCase.ScreenName} must not bake a full target image into the root.");

            AssertChildren(
                prefab.transform,
                "ShellFill",
                "ShellFrame",
                "HeaderBar/BackButton/IconText",
                "HeaderBar/TitleText",
                "CategoryRail/Tab_1/LabelText",
                "HeroPanel/HeroTitleText",
                "HeroPanel/UnavailableButton/LabelText",
                "StatusCard_1/TitleText",
                "FeedRow_1/TagText");

            Assert.NotNull(prefab.transform.Find("ImplementationNotePanel/BodyText"), routeCase.ScreenName);
        }
    }

    [Test]
    public void DesignedUnavailableRouteScreens_UseLayeredArtWithoutBakedRootTargets()
    {
        foreach (RouteCase routeCase in Cases)
        {
            GameObject prefab = LoadPrefab(routeCase);
            AssertFlatImage(prefab.transform, "ShellFill", false);
            AssertFlatImage(prefab.transform, "ShellFrame", false);
            AssertFlatImage(prefab.transform, "HeaderBar", false);
            AssertFlatImage(prefab.transform, "HeaderBar/CreditsCounter", false);
            AssertFlatImage(prefab.transform, "CategoryRail/Tab_1", true);
            AssertFlatImage(prefab.transform, "HeroPanel", false);
            AssertImageSpritePath(prefab.transform, "HeroPanel/HeroArtImage", routeCase.LayerRoot, "Content/art_intel_dossier.png");
            AssertFlatImage(prefab.transform, "StatusCard_1", true);
        }
    }

    [Test]
    public void DesignedUnavailableRouteScreens_PreserveLiveTextAndDisabledCta()
    {
        foreach (RouteCase routeCase in Cases)
        {
            GameObject prefab = LoadPrefab(routeCase);
            AssertText(prefab.transform, "HeaderBar/TitleText", routeCase.Title);
            AssertText(prefab.transform, "HeroPanel/HeroTitleText", routeCase.HeroTitle);
            AssertText(prefab.transform, "HeroPanel/UnavailableButton/LabelText", "DESIGNED UNAVAILABLE");

            Button unavailable = prefab.transform.Find("HeroPanel/UnavailableButton").GetComponent<Button>();
            Assert.NotNull(unavailable, routeCase.ScreenName);
            Assert.IsFalse(unavailable.interactable, routeCase.ScreenName);

            AssertMotionButton(prefab, "CategoryRail/Tab_1");
            AssertMotionButton(prefab, "CategoryRail/Tab_2");
            AssertMotionButton(prefab, "StatusCard_1");
        }
    }

    private static GameObject LoadPrefab(RouteCase routeCase)
    {
        string path = $"Assets/Game/Prefabs/UI/Screens/{routeCase.ScreenName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(prefab, path);
        return prefab;
    }

    private static void AssertChildren(Transform root, params string[] paths)
    {
        foreach (string path in paths)
            Assert.NotNull(root.Find(path), path);
    }

    private static void AssertImageSpritePath(Transform root, string path, string layerRoot, string expectedRelativePath)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual($"{layerRoot}/{expectedRelativePath}", AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertFlatImage(Transform root, string path, bool raycastTarget)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.IsNull(image.sprite, $"{path} must be a live generated flat surface, not a baked target crop.");
        Assert.AreEqual(raycastTarget, image.raycastTarget, path);
    }

    private static void AssertText(Transform root, string path, string expected)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);
        TMP_Text text = target.GetComponent<TMP_Text>();
        Assert.NotNull(text, path);
        Assert.AreEqual(expected, text.text, path);
    }

    private static void AssertMotionButton(GameObject prefab, string path)
    {
        Transform target = prefab.transform.Find(path);
        Assert.NotNull(target, path);
        Button button = target.GetComponent<Button>();
        Assert.NotNull(button, path);
        Assert.NotNull(target.GetComponent<UiMotionFeedback>(), path);
    }

    private readonly struct RouteCase
    {
        public readonly string ScreenName;
        public readonly WarlineCaptureRoute Route;
        public readonly string Title;
        public readonly string HeroTitle;
        public readonly string LayerRoot;

        public RouteCase(string screenName, WarlineCaptureRoute route, string title, string heroTitle, string layerRoot)
        {
            ScreenName = screenName;
            Route = route;
            Title = title;
            HeroTitle = heroTitle;
            LayerRoot = layerRoot;
        }
    }
}
