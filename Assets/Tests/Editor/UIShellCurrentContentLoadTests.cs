using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
                nameof(InstalledMatchHudSelectionPanelActivatesThroughRuntimeBinding),
                test => test.InstalledMatchHudSelectionPanelActivatesThroughRuntimeBinding(),
                ref passed);
            RunValidationStep(
                nameof(RightQuickRailBuildButtonShowsAndClosesBuildDrawerPopup),
                test => test.RightQuickRailBuildButtonShowsAndClosesBuildDrawerPopup(),
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
    public void InstalledMatchHudSelectionPanelActivatesThroughRuntimeBinding()
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

        GameObject matchLeft = AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        MatchHudSelectionPanelView selectionPanelView = matchLeft.GetComponent<MatchHudSelectionPanelView>();
        Assert.NotNull(selectionPanelView, "Installed Match HUD left region must own MatchHudSelectionPanelView.");

        Transform selectedPanel = matchLeft.transform.Find("SelectedSquadPanel");
        Assert.NotNull(selectedPanel, "Installed Match HUD must contain SelectedSquadPanel under LeftContent.");

        var feedback = new SelectionHudFeedbackSystem();
        content.BindGameplayRuntimeDependencies(
            new SelectionUiCommandSystem(),
            null,
            feedback.BindMatchHudSelectionPanel);
        Assert.IsFalse(selectedPanel.gameObject.activeSelf, "Runtime binding should start with the selection panel hidden.");

        Entity unit = CreatePlayerUnit(_world.EntityManager, "Echo Squad", new int2(8, 9), 96);
        feedback.ApplySelection(_world.EntityManager, unit, new SelectionUiQuerySystem());

        Assert.IsTrue(selectedPanel.gameObject.activeSelf, "Selecting a valid unit must activate the active Match HUD SelectedSquadPanel.");
    }

    [Test]
    public void RightQuickRailBuildButtonShowsAndClosesBuildDrawerPopup()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandComponent { Kind = UiShellCommandKind.EnterMatchHud }
        });

        GameObject matchRight = AssertRegionHasChild(content.ShellView, UIShellRegionId.RightRegion);
        MatchHudRightQuickRailView quickRail = matchRight.GetComponent<MatchHudRightQuickRailView>();
        Assert.NotNull(quickRail, "RightContent must own MatchHudRightQuickRailView for serialized quick rail button bindings.");
        Assert.NotNull(quickRail.BuildButton, "Right quick rail Build button must be serialized.");
        Canvas.ForceUpdateCanvases();
        AssertButtonHasInteractiveRect(
            quickRail.BuildButton,
            "Right quick rail Build button must have a non-zero rect after layout so live pointer clicks can hit it.");
        AssertButtonHasRaycastableHitTarget(
            quickRail.BuildButton,
            "Right quick rail Build button must have a raycastable hit target so live pointer clicks fire.");
        AssertButtonTargetGraphicHasInteractiveRect(
            quickRail.BuildButton,
            "Right quick rail Build button target graphic must have a non-zero rect after layout.");

        var mainMenu = new MainMenuPlayUI();
        content.BindGameplayRuntimeDependencies(new SelectionUiCommandSystem(), mainMenu);
        Assert.AreNotEqual(
            quickRail.BuildButton.gameObject,
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null,
            "Right quick rail Build button must not start in Unity selected state after Match HUD binding.");
        Assert.AreNotEqual(
            TacticalCommandMode.Build,
            BattleHudRuntimeFeedbackSystem.GetState().CurrentCommandMode,
            "Build command mode must not be active by default when the Match HUD loads.");

        Vector2 buttonCenter = GetButtonTargetGraphicCenterScreenPoint(quickRail.BuildButton);
        Assert.IsTrue(
            mainMenu.IsPointerOverAnyGameplayUi(buttonCenter, out string gameplayUiSource),
            "Runtime UI hit filter must treat the moved Build button as gameplay UI.");
        Assert.AreEqual(
            "MatchHudRightQuickRail",
            gameplayUiSource,
            "Runtime UI hit filter should identify the moved Build button as the right quick rail.");

        quickRail.UnbindBuildCommand();
        AssertPointerClickDispatchesToButton(scene, quickRail.BuildButton);

        GameObject popup = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.AreEqual("SCN09_BuildDrawerPopup", popup.name);

        Transform closeTransform = popup.transform.Find("BuildDrawerRoot/DrawerFrame/CloseButton");
        Assert.NotNull(closeTransform, "Build drawer popup must expose its close button at BuildDrawerRoot/DrawerFrame/CloseButton.");
        Button closeButton = closeTransform.GetComponent<Button>();
        Assert.NotNull(closeButton, "Build drawer close object must be a Button.");
        Canvas.ForceUpdateCanvases();
        AssertButtonHasInteractiveRect(
            closeButton,
            "Build drawer close button must have a non-zero rect after layout so live pointer clicks can hit it.");
        AssertButtonHasRaycastableHitTarget(
            closeButton,
            "Build drawer close button must have a raycastable hit target so live pointer clicks fire.");
        AssertButtonTargetGraphicHasInteractiveRect(
            closeButton,
            "Build drawer close button target graphic must have a non-zero rect after layout.");

        AssertPointerClickDispatchesToButtonRoot(scene, closeButton);

        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.PopupLayer);
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

    private static void AssertButtonHasRaycastableHitTarget(Button button, string message)
    {
        Graphic[] graphics = button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null && graphic.raycastTarget)
                return;
        }

        Assert.Fail(message);
    }

    private static void AssertButtonHasInteractiveRect(Button button, string message)
    {
        RectTransform rectTransform = button.transform as RectTransform;
        Assert.NotNull(rectTransform, message);
        Rect rect = rectTransform.rect;
        Assert.Greater(rect.width, 1f, message);
        Assert.Greater(rect.height, 1f, message);
    }

    private static void AssertButtonTargetGraphicHasInteractiveRect(Button button, string message)
    {
        Assert.NotNull(button.targetGraphic, message);
        Rect rect = button.targetGraphic.rectTransform.rect;
        Assert.Greater(rect.width, 1f, message);
        Assert.Greater(rect.height, 1f, message);
    }

    private static void AssertPointerClickDispatchesToButton(Scene scene, Button button)
    {
        EventSystem eventSystem = FindInScene<EventSystem>(scene);
        Assert.NotNull(eventSystem, "Menu scene must contain an EventSystem for UI pointer validation.");
        Assert.NotNull(button.targetGraphic, "Build button must have a target graphic for pointer dispatch.");

        var pointerEvent = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left
        };

        bool handled = ExecuteEvents.ExecuteHierarchy(
            button.targetGraphic.gameObject,
            pointerEvent,
            ExecuteEvents.pointerClickHandler);
        Assert.IsTrue(handled, "Pointer click on Build button target graphic must dispatch to the parent Button.");
    }

    private static void AssertPointerClickDispatchesToButtonRoot(Scene scene, Button button)
    {
        EventSystem eventSystem = FindInScene<EventSystem>(scene);
        Assert.NotNull(eventSystem, "Menu scene must contain an EventSystem for UI pointer validation.");

        var pointerEvent = new PointerEventData(eventSystem)
        {
            button = PointerEventData.InputButton.Left
        };

        bool handled = ExecuteEvents.Execute(
            button.gameObject,
            pointerEvent,
            ExecuteEvents.pointerClickHandler);
        Assert.IsTrue(handled, "Pointer click on Button root must dispatch to the close Button.");
    }

    private static Vector2 GetButtonTargetGraphicCenterScreenPoint(Button button)
    {
        Assert.NotNull(button.targetGraphic, "Button must have a target graphic for screen point calculation.");
        RectTransform rectTransform = button.targetGraphic.rectTransform;
        Camera eventCamera = ResolveEventCamera(button);
        return RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            rectTransform.TransformPoint(rectTransform.rect.center));
    }

    private static Camera ResolveEventCamera(Component component)
    {
        Canvas canvas = component != null ? component.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
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

    private static Entity CreatePlayerUnit(EntityManager em, string displayName, int2 cell, int health)
    {
        Entity entity = em.CreateEntity();
        em.AddComponentData(entity, new Faction { Id = 0 });
        em.AddComponentData(entity, new UnitGrid { Cell = cell });
        em.AddComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.AddComponentData(entity, new UnitDisplayInfo
        {
            Name = new FixedString64Bytes(displayName),
            Description = new FixedString128Bytes("Runtime feedback system test unit")
        });
        em.AddComponentData(entity, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 5f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.AddComponentData(entity, LocalTransform.FromPosition(new float3(cell.x, 0f, cell.y)));
        return entity;
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
