using System.Collections.Generic;
using System;
using Unity.Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UIShellCurrentContentLoadTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(MenuSceneShellInstallsCurrentMenuArmoryAndMatchHudContent),
                test => test.MenuSceneShellInstallsCurrentMenuArmoryAndMatchHudContent(),
                ref passed);
            RunValidationStep(
                nameof(InstalledMatchHudCommandControlsRebindWhenRuntimeDependenciesArrive),
                test => test.InstalledMatchHudCommandControlsRebindWhenRuntimeDependenciesArrive(),
                ref passed);
            RunValidationStep(
                nameof(ReinstalledMatchHudCommandControlsKeepRuntimeDependencies),
                test => test.ReinstalledMatchHudCommandControlsKeepRuntimeDependencies(),
                ref passed);
            RunValidationStep(
                nameof(RebindingCommandControlsWithAnotherInputSystemDropsStaleListeners),
                test => test.RebindingCommandControlsWithAnotherInputSystemDropsStaleListeners(),
                ref passed);
            RunValidationStep(
                nameof(MenuSceneShellSerializesMatchIntroCurtain),
                test => test.MenuSceneShellSerializesMatchIntroCurtain(),
                ref passed);

            Debug.Log($"[UIShellCurrentContentLoadValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UIShellCurrentContentLoadValidation] result=Failed passed={passed}\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        BattleHudRuntimeFeedbackSystem.ClearActiveView(BattleHudRuntimeFeedbackSystem.ResolveActiveView());
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        if (_world == null)
            return;

        if (_world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world = null;
        _previousWorld = null;
    }

    [Test]
    public void MenuSceneShellInstallsCurrentMenuArmoryAndMatchHudContent()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        MenuBootstrapView bootstrap = FindInScene<MenuBootstrapView>(scene);
        Assert.NotNull(bootstrap, "Menu scene must contain the menu bootstrap view.");
        Assert.NotNull(bootstrap.ContentSystem, "Menu bootstrap must serialize the shell content binder.");
        Assert.NotNull(bootstrap.ShellEcsPresentation, "Menu bootstrap must serialize the shell ECS presentation view.");

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

    [Test]
    public void InstalledMatchHudCommandControlsRebindWhenRuntimeDependenciesArrive()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellCurrentContentLoadTests");
        World.DefaultGameObjectInjectionWorld = _world;

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });

        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls =
            matchFooter.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        Assert.NotNull(controls);

        content.BindGameplayRuntimeDependencies(new SelectionUiCommandSystem());
        controls.MoveButton.onClick.Invoke();

        Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
        Assert.AreEqual(1, requests.Length, "Move click must queue after runtime dependencies arrive after HUD install.");
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterMoveTargetMode, requests[0].Kind);
    }

    [Test]
    public void ReinstalledMatchHudCommandControlsKeepRuntimeDependencies()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellCurrentContentLoadTests");
        World.DefaultGameObjectInjectionWorld = _world;

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.BindGameplayRuntimeDependencies(new SelectionUiCommandSystem());
        int beforeInstallVersion = content.ContentVersion;

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });

        Assert.Greater(content.ContentVersion, beforeInstallVersion, "Installing Match HUD content must advance the shell content version.");
        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls =
            matchFooter.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        Assert.NotNull(controls);

        controls.MoveButton.onClick.Invoke();

        Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
        Assert.AreEqual(1, requests.Length, "Move click must queue after HUD reinstall when dependencies were already known.");
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterMoveTargetMode, requests[0].Kind);
    }

    [Test]
    public void RebindingCommandControlsWithAnotherInputSystemDropsStaleListeners()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellCurrentContentLoadTests");
        World.DefaultGameObjectInjectionWorld = _world;

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });

        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls =
            matchFooter.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
        Assert.NotNull(controls);

        var staleInputSystem = new MatchOverlayCommandInputSystem();
        staleInputSystem.Bind(controls, new SelectionUiCommandSystem());
        var currentInputSystem = new MatchOverlayCommandInputSystem();
        currentInputSystem.Bind(controls, new SelectionUiCommandSystem());

        controls.MoveButton.onClick.Invoke();

        Assert.IsTrue(TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests));
        Assert.AreEqual(1, requests.Length, "Rebinding through another input system must not leave stale Move listeners attached.");
        Assert.AreEqual(RtsSelectionCommandIntentKind.EnterMoveTargetMode, requests[0].Kind);
    }

    [Test]
    public void MenuSceneShellSerializesMatchIntroCurtain()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellView shell = FindInScene<UIShellView>(scene);
        Assert.NotNull(shell, "Menu scene must contain the shell view.");
        Assert.NotNull(shell.MatchIntroCurtain, "Shell view must serialize the Match intro curtain.");
        Assert.NotNull(shell.MatchIntroCurtain.Root, "Match intro curtain must serialize its root.");
        Assert.NotNull(shell.MatchIntroCurtain.CanvasGroup, "Match intro curtain must serialize its CanvasGroup.");

        Assert.IsFalse(shell.MatchIntroCurtain.Root.activeSelf, "Curtain should start inactive until Match loading starts.");
        Assert.That(shell.MatchIntroCurtain.CanvasGroup.alpha, Is.EqualTo(0f).Within(0.0001f));
        Assert.IsFalse(shell.MatchIntroCurtain.CanvasGroup.interactable);
        Assert.IsFalse(shell.MatchIntroCurtain.CanvasGroup.blocksRaycasts);

        Image curtainImage = shell.MatchIntroCurtain.Root.GetComponent<Image>();
        Assert.NotNull(curtainImage, "Curtain should render through a serialized Image component.");
        Assert.IsFalse(curtainImage.raycastTarget, "Curtain image must not block HUD or shell input.");
        Assert.That(curtainImage.color.r, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(curtainImage.color.g, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(curtainImage.color.b, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(curtainImage.color.a, Is.EqualTo(1f).Within(0.0001f));

        Assert.IsTrue(shell.TryGetRegion(UIShellRegionId.MenuBackgroundRegion, out UIShellRegionView background));
        Assert.IsTrue(shell.TryGetRegion(UIShellRegionId.HeaderRegion, out UIShellRegionView header));
        int curtainIndex = shell.MatchIntroCurtain.Root.transform.GetSiblingIndex();
        Assert.Greater(curtainIndex, background.RegionRoot.GetSiblingIndex(), "Curtain should draw above menu/world background.");
        Assert.Less(curtainIndex, header.RegionRoot.GetSiblingIndex(), "HUD regions should draw above the curtain.");
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

    private static bool TryGetCommandRequests(out DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests)
    {
        var inputState = new RtsSelectionInputSystem();
        return inputState.TryGetCommandBuffers(
            out _,
            out requests,
            out DynamicBuffer<RtsSelectionCommandResultElement> _);
    }

    private static void RunValidationStep(
        string name,
        Action<UIShellCurrentContentLoadTests> step,
        ref int passed)
    {
        var test = new UIShellCurrentContentLoadTests();
        try
        {
            step(test);
            passed++;
            Debug.Log($"[UIShellCurrentContentLoadValidation] passed={name}");
        }
        finally
        {
            test.TearDown();
        }
    }
}
