using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class UIShellCurrentContentLoadTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";

    [TearDown]
    public void TearDown()
    {
        BattleHudRuntimeFeedbackSystem.ClearActiveView(BattleHudRuntimeFeedbackSystem.ResolveActiveView());
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void MenuSceneShellInstallsCurrentMenuArmoryAndMatchHudContent()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");
        Assert.NotNull(content.ShellView, "Shell content binder must serialize the shell view.");
        Assert.NotNull(content.MainMenuContentPrefab, "Main menu content prefab must be assigned.");
        Assert.NotNull(content.ArmoryContentPrefab, "Armory content prefab must be assigned.");
        Assert.NotNull(content.MatchHudContentPrefab, "Match HUD content prefab must be assigned.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMenu }
        });

        AssertRegionHasChild(content.ShellView, UIShellRegionId.MenuBackgroundRegion);
        AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);
        AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        AssertRegionHasChild(content.ShellView, UIShellRegionId.MiddleRegion);
        AssertRegionHasChild(content.ShellView, UIShellRegionId.RightRegion);
        AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);

        content.InstallMenuRouteBody(UIRoute.Armory);
        GameObject armoryLeft = AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        GameObject armoryMiddle = AssertRegionHasChild(content.ShellView, UIShellRegionId.MiddleRegion);
        GameObject armoryRight = AssertRegionHasChild(content.ShellView, UIShellRegionId.RightRegion);
        Assert.NotNull(armoryLeft.GetComponent<ArmoryCategoryNavigationView>());
        Assert.NotNull(armoryMiddle.GetComponent<ArmoryContentListView>());
        Assert.NotNull(armoryRight.GetComponent<ArmoryRightContentView>());

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });

        GameObject matchLeft = AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        Assert.NotNull(matchLeft.GetComponent<MatchHudSelectionPanelView>());
        Assert.NotNull(matchFooter.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true));
        Assert.NotNull(matchFooter.GetComponentInChildren<MatchOverlayCommandControlsView>(true));
        Assert.NotNull(matchFooter.GetComponentInChildren<MatchHudMinimapView>(true));
        Assert.NotNull(matchFooter.GetComponentInChildren<MatchHudSquadTrayView>(true));

        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.MiddleRegion);
    }

    private static GameObject AssertRegionHasChild(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region), $"{regionId} must be registered.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must have a content root.");
        Assert.Greater(region.ContentRoot.childCount, 0, $"{regionId} should contain installed content.");
        return region.ContentRoot.GetChild(0).gameObject;
    }

    private static void AssertRegionIsEmpty(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region), $"{regionId} must be registered.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must have a content root.");
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} should be empty for the installed Match HUD.");
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        List<GameObject> roots = new();
        scene.GetRootGameObjects(roots);
        for (int i = 0; i < roots.Count; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
