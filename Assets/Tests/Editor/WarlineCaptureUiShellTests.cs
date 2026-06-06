using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiShellTests
{
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string ShellPrefabPath = "Assets/Game/Prefabs/UI/Shell/WarlineCaptureAppCanvas.prefab";
    private const string ShellMainMenuContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab";
    private const string ShellCommanderProfileContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab";
    private const string ShellArmoryContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab";
    private const string ArmoryRosterCardDefaultFramePath = "Assets/Game/Art/UI/Final/scn19_roster_card_default_frame.png";
    private const string ArmoryRosterCardSelectedFramePath = "Assets/Game/Art/UI/Final/scn19_roster_card_selected_frame.png";
    private const string ShellMatchHudContentPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const string ShellBuildDrawerPopupPrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/SCN09_BuildDrawerPopup.prefab";
    private const string SplashPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Splash.prefab";
    private const string SplashBackgroundPath = "Assets/Game/Art/UI/Generated/Splash/Backgrounds/Splash_Background_CityDawn.png";
    private const string SplashLoadingPanelPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_LoadingPanel_9Slice.png";
    private const string SplashProgressTrackPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_ProgressTrackMask.png";
    private const string SplashProgressFillPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_ProgressFillMask.png";
    private const string SplashBottomPanelPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_BottomStatusPanel_9Slice.png";
    private const string SplashOuterFramePath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_OuterFrame_Overlay.png";
    private const string SplashLogoEmblemPath = "Assets/Game/Art/UI/Brand/WarlineCapture_LionLogo_Display.png";
    private const string SplashTitleWordmarkPath = "Assets/Game/Art/UI/Generated/Splash/Titles/Splash_Title_Wordmark.png";
    private const string OxaniumFontFolder = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/";
    private static readonly string[] GenericShellScreenPrefabPaths =
    {
        "Assets/Game/Prefabs/UI/Screens/Screen_Splash.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Settings.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_QuickCustomSetup.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_SagaMap.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_MissionBriefing.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_LoadoutSquadPrep.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_CommandExchange.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Inbox.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Events.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Ranking.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_CommandFeed.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_OperationDashboard.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_DistrictDetail.prefab"
    };

    [Test]
    public void MatchScene_UsesSceneOwnedCanvasWithoutParallelLegacyBootstrap()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(MatchScenePath);
        string legacyCanvasBlock = scene.FindRequiredBlockContaining("m_Name: UI_Canvas");

        Assert.Throws<AssertionException>(() => scene.FindRequiredBlockContaining("m_EditorClassIdentifier: Assembly-CSharp::WarlineCaptureUiBootstrap"));
        StringAssert.Contains("m_IsActive: 1", legacyCanvasBlock);
    }

    [Test]
    public void ShellPrefab_KeepsScreensAsSeparatePrefabReferences()
    {
        GameObject shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPrefabPath);
        Assert.NotNull(shellPrefab);

        Transform contentRoot = shellPrefab.transform.Find("SafeAreaRoot/ContentRoot");
        Assert.NotNull(contentRoot);
        Assert.AreEqual(0, contentRoot.GetComponentsInChildren<WarlineCaptureScreenSystem>(true).Length);
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        Assert.AreEqual(Vector2.zero, contentRect.anchorMin);
        Assert.AreEqual(Vector2.one, contentRect.anchorMax);
        Assert.IsFalse(shellPrefab.transform.Find("SafeAreaRoot/HeaderBar").gameObject.activeSelf);
        Assert.IsFalse(shellPrefab.transform.Find("SafeAreaRoot/FooterBar").gameObject.activeSelf);

        var router = shellPrefab.GetComponent<WarlineCaptureRouter>();
        Assert.NotNull(router);
        Assert.NotNull(shellPrefab.GetComponent<WarlineCaptureModalSystem>());
        WarlineCaptureMatchResultFlow resultFlow = shellPrefab.GetComponent<WarlineCaptureMatchResultFlow>();
        Assert.NotNull(resultFlow);
        WarlineCaptureUiAccessibilityApplier accessibilityApplier = shellPrefab.GetComponent<WarlineCaptureUiAccessibilityApplier>();
        Assert.NotNull(accessibilityApplier);

        var serializedAccessibility = new SerializedObject(accessibilityApplier);
        Assert.AreEqual(contentRoot, serializedAccessibility.FindProperty("scaleRoot").objectReferenceValue);

        var serializedRouter = new SerializedObject(router);
        SerializedProperty screenPrefabs = serializedRouter.FindProperty("screenPrefabs");
        Assert.NotNull(screenPrefabs);
        Assert.AreEqual(17, screenPrefabs.arraySize);

        var serializedResultFlow = new SerializedObject(resultFlow);
        Assert.AreEqual(router, serializedResultFlow.FindProperty("router").objectReferenceValue);
        Assert.AreEqual(shellPrefab.transform.Find("SafeAreaRoot/ModalOverlay"), serializedResultFlow.FindProperty("modalOverlay").objectReferenceValue);
        Assert.NotNull(serializedResultFlow.FindProperty("missionResultPopupPrefab").objectReferenceValue);

        Transform placeholderPopup = shellPrefab.transform.Find("SafeAreaRoot/ModalOverlay/PlaceholderPopup");
        Assert.NotNull(placeholderPopup);
        Assert.NotNull(placeholderPopup.Find("TitleText"));
        Assert.NotNull(placeholderPopup.Find("BodyText"));
        Assert.NotNull(placeholderPopup.Find("CloseButton"));
    }

    [Test]
    public void ShellPrefab_InstantiatesScreenPrefabsIntoContentRoot()
    {
        GameObject shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPrefabPath);
        Assert.NotNull(shellPrefab);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shellPrefab);
        try
        {
            var router = instance.GetComponent<WarlineCaptureRouter>();
            Assert.NotNull(router);
            router.Initialize();

            Transform contentRoot = instance.transform.Find("SafeAreaRoot/ContentRoot");
            Assert.NotNull(contentRoot);
            Assert.AreEqual(17, contentRoot.GetComponentsInChildren<WarlineCaptureScreenSystem>(true).Length);
            Assert.NotNull(contentRoot.Find("Screen_Splash"));
            Assert.NotNull(contentRoot.Find("Screen_MainMenu"));
            Assert.NotNull(contentRoot.Find("Screen_Settings"));
            Assert.NotNull(contentRoot.Find("Screen_QuickCustomSetup"));
            Assert.NotNull(contentRoot.Find("Screen_MatchOverlay"));
            Assert.NotNull(contentRoot.Find("Screen_SagaMap"));
            Assert.NotNull(contentRoot.Find("Screen_MissionBriefing"));
            Assert.NotNull(contentRoot.Find("Screen_LoadoutSquadPrep"));
            Assert.NotNull(contentRoot.Find("Screen_CommanderProfile"));
            Assert.NotNull(contentRoot.Find("Screen_Armory"));
            Assert.NotNull(contentRoot.Find("Screen_CommandExchange"));
            Assert.NotNull(contentRoot.Find("Screen_Inbox"));
            Assert.NotNull(contentRoot.Find("Screen_Events"));
            Assert.NotNull(contentRoot.Find("Screen_Ranking"));
            Assert.NotNull(contentRoot.Find("Screen_CommandFeed"));
            Assert.NotNull(contentRoot.Find("Screen_OperationDashboard"));
            Assert.NotNull(contentRoot.Find("Screen_DistrictDetail"));
            Assert.AreEqual(WarlineCaptureRoute.MainMenu, router.ActiveRoute);
            Assert.IsFalse(contentRoot.Find("Screen_Splash").gameObject.activeSelf);
            Assert.IsTrue(contentRoot.Find("Screen_MainMenu").gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellFlow_InitialStartupEntersMainMenuWithoutLoading()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("ShellFlow_InitialStartup_NoLoading");
        World.DefaultGameObjectInjectionWorld = world;
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity boundary = entityManager.CreateEntity(typeof(UiShellBoundaryComponent));
            entityManager.AddComponentData(boundary, new UiShellStateComponent
            {
                CurrentMode = UiShellMode.None,
                ActiveRoute = WarlineCaptureRoute.Splash,
                Phase = UiShellTransitionPhase.Idle
            });
            entityManager.AddComponentData(boundary, new UiShellLoadingProgressComponent());
            entityManager.AddComponentData(boundary, new UiShellArmoryCategoryComponent
            {
                Category = ArmoryCatalogCategory.Characters
            });
            entityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
            entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
            entityManager.AddBuffer<UiShellRouteHistoryComponent>(boundary);
            entityManager.AddBuffer<UiShellPopupRequestComponent>(boundary);
            DynamicBuffer<UiShellPresentationCommandComponent> commands =
                entityManager.AddBuffer<UiShellPresentationCommandComponent>(boundary);
            entityManager.AddBuffer<UiShellTransitionCompleteComponent>(boundary);

            SystemHandle flowSystem = world.CreateSystem<UiShellFlowSystem>();
            flowSystem.Update(world.Unmanaged);

            UiShellStateComponent state = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            Assert.AreEqual(UiShellMode.MainMenu, state.CurrentMode);
            Assert.AreEqual(WarlineCaptureRoute.MainMenu, state.ActiveRoute);
            Assert.AreEqual(UiShellTransitionPhase.EnteringMenu, state.Phase);
            Assert.AreEqual(1, commands.Length);
            Assert.AreEqual(UiShellCommandKind.EnterMenu, commands[0].Kind);
            Assert.AreEqual(UiShellRegionId.None, commands[0].Region);
            Assert.AreEqual(WarlineCaptureRoute.MainMenu, commands[0].Route);
            Assert.AreEqual(UiShellMode.MainMenu, commands[0].TargetMode);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ShellMainMenuContent_ArmoryNavRoutesToArmoryContent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMainMenuContentPrefabPath);
        Assert.NotNull(prefab, ShellMainMenuContentPrefabPath);

        AssertMainMenuNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Leaderboards", WarlineCaptureRoute.MainMenu, true, false);
        AssertMainMenuNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Armory", WarlineCaptureRoute.Armory, false, true);
        AssertMainMenuNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Store", WarlineCaptureRoute.MainMenu, false, false);
        AssertMainMenuNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Contests", WarlineCaptureRoute.MainMenu, false, false);
        AssertMainMenuNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Tutorials", WarlineCaptureRoute.MainMenu, false, false);
        MainMenuNavigationView navigationView = prefab.GetComponentInChildren<MainMenuNavigationView>(true);
        Assert.NotNull(navigationView);
        AssertNavigationTabsAssigned(new SerializedObject(navigationView).FindProperty("tabs"), 5, "Main Menu");
    }

    [Test]
    public void ShellMatchHudContent_FooterSelectCommandIsClickable()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        Transform footer = prefab.transform.Find("FooterContent");
        Assert.NotNull(footer, "Match HUD shell content must expose FooterContent because the shell clones sections independently.");

        MatchOverlayCommandControlsView view = footer.GetComponent<MatchOverlayCommandControlsView>();
        Assert.NotNull(view, "FooterContent must own command control references because the root prefab is not instantiated at runtime.");

        Transform selectTransform = footer.Find("CommandRail/Frame/SelectCommand");
        Assert.NotNull(selectTransform, "Match HUD footer must keep SelectCommand in the command rail.");
        Transform buildTransform = footer.Find("CommandRail/Frame/BuildCommand");
        Assert.NotNull(buildTransform, "Match HUD footer must keep BuildCommand in the command rail.");

        Button selectButton = selectTransform.GetComponent<Button>();
        Button buildButton = buildTransform.GetComponent<Button>();
        Assert.NotNull(selectButton, "SelectCommand must be a real Button on the shell content prefab.");
        Assert.NotNull(buildButton, "BuildCommand must be a real Button on the shell content prefab.");
        Assert.IsTrue(selectButton.interactable);
        Assert.IsTrue(buildButton.interactable);
        Assert.NotNull(selectButton.targetGraphic, "SelectCommand needs a raycastable target graphic for UI clicks.");
        Assert.NotNull(buildButton.targetGraphic, "BuildCommand needs a raycastable target graphic for UI clicks.");
        Assert.IsTrue(selectButton.targetGraphic.raycastTarget, "SelectCommand target graphic must receive pointer raycasts.");
        Assert.IsTrue(buildButton.targetGraphic.raycastTarget, "BuildCommand target graphic must receive pointer raycasts.");

        SerializedObject serializedView = new(view);
        Assert.AreEqual(selectButton, serializedView.FindProperty("selectButton").objectReferenceValue);
        Assert.AreEqual(buildButton, serializedView.FindProperty("buildButton").objectReferenceValue);
        Assert.NotNull(serializedView.FindProperty("buildDrawerPopupPrefab").objectReferenceValue);

        Transform commandRailFrame = footer.Find("CommandRail/Frame");
        Assert.NotNull(commandRailFrame, "Match HUD footer must expose CommandRail/Frame as the tab parent.");

        MatchOverlayCommandTabGroupView tabGroup = commandRailFrame.GetComponent<MatchOverlayCommandTabGroupView>();
        Assert.NotNull(tabGroup, "CommandRail/Frame must own the command tab group.");
        Assert.AreEqual(tabGroup, serializedView.FindProperty("commandTabGroup").objectReferenceValue);
        AssertCommandTabsAssigned(tabGroup, commandRailFrame);

        BattleHudRuntimeFeedbackView hudView = footer.GetComponent<BattleHudRuntimeFeedbackView>();
        Assert.NotNull(hudView, "The shell-instantiated Match HUD footer must own a live view for sticky command tab state.");
        var serializedHudView = new SerializedObject(hudView);
        SerializedProperty commandTabGroups = serializedHudView.FindProperty("commandTabGroups");
        Assert.AreEqual(1, commandTabGroups.arraySize);
        Assert.AreEqual(tabGroup, commandTabGroups.GetArrayElementAtIndex(0).objectReferenceValue);
    }

    [Test]
    public void ShellMatchHudContent_FooterMinimapViewIsSerialized()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        Transform footer = prefab.transform.Find("FooterContent");
        Assert.NotNull(footer, "Match HUD shell content must expose FooterContent because the shell clones sections independently.");

        Transform minimap = footer.Find("MinimapPanel");
        Assert.NotNull(minimap, "Match HUD footer must keep the minimap in FooterContent/MinimapPanel.");
        Transform map = minimap.Find("Map");
        Transform viewport = minimap.Find("Viewport");
        Transform zoomIn = minimap.Find("ZoomIn");
        Transform zoomOut = minimap.Find("ZoomOut");
        Assert.NotNull(map, "MinimapPanel must keep Map for the generated top-down background.");
        Assert.NotNull(viewport, "MinimapPanel must keep Viewport for the camera rect overlay.");
        Assert.NotNull(zoomIn, "MinimapPanel must keep ZoomIn.");
        Assert.NotNull(zoomOut, "MinimapPanel must keep ZoomOut.");

        MatchHudMinimapView view = minimap.GetComponent<MatchHudMinimapView>();
        Assert.NotNull(view, "MinimapPanel must own MatchHudMinimapView so runtime code uses serialized references.");

        Image mapImage = map.GetComponent<Image>();
        Button zoomInButton = zoomIn.GetComponent<Button>();
        Button zoomOutButton = zoomOut.GetComponent<Button>();
        Assert.NotNull(mapImage);
        Assert.NotNull(zoomInButton);
        Assert.NotNull(zoomOutButton);
        Assert.IsTrue(mapImage.raycastTarget, "Map must receive pointer raycasts for click-to-focus and viewport dragging.");

        SerializedObject serializedView = new(view);
        Assert.AreEqual(mapImage, serializedView.FindProperty("mapImage").objectReferenceValue);
        Assert.AreEqual(map.GetComponent<RectTransform>(), serializedView.FindProperty("mapRect").objectReferenceValue);
        Assert.AreEqual(viewport.GetComponent<RectTransform>(), serializedView.FindProperty("viewportRect").objectReferenceValue);
        Assert.AreEqual(zoomInButton, serializedView.FindProperty("zoomInButton").objectReferenceValue);
        Assert.AreEqual(zoomOutButton, serializedView.FindProperty("zoomOutButton").objectReferenceValue);
    }

    [Test]
    public void ShellMatchHudContent_SelectedSquadPanelIsDisabledOnInit()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        Transform prefabPanel = prefab.transform.Find("LeftContent/SelectedSquadPanel");
        Assert.NotNull(prefabPanel, "Match HUD shell content must keep the selected squad panel in the left content region.");
        Assert.IsFalse(prefabPanel.gameObject.activeSelf, "SelectedSquadPanel must be disabled by default before any unit/building selection exists.");
        Assert.IsNull(prefab.GetComponent<BattleHudTacticalFeedbackSystem>(), "SCN08 root must not own gameplay feedback; shell installs match HUD regions separately.");
        MatchHudSelectionPanelSystem prefabSelectionPanelSystem = prefab.transform.Find("LeftContent").GetComponent<MatchHudSelectionPanelSystem>();
        Assert.NotNull(prefabSelectionPanelSystem, "LeftContent must own match HUD selection panel init because the shell installs left and footer regions separately.");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            Transform panel = instance.transform.Find("LeftContent/SelectedSquadPanel");
            Assert.NotNull(panel);
            Assert.IsFalse(panel.gameObject.activeSelf);

            MatchHudSelectionPanelSystem selectionPanelSystem = instance.transform.Find("LeftContent").GetComponent<MatchHudSelectionPanelSystem>();
            Assert.NotNull(selectionPanelSystem);
            panel.gameObject.SetActive(true);
            InvokePrivate(selectionPanelSystem, "OnEnable");
            Assert.IsFalse(panel.gameObject.activeSelf, "Match HUD init must deactivate SelectedSquadPanel even if a stale active state leaks into launch.");

            selectionPanelSystem.ShowSelection();
            Assert.IsTrue(panel.gameObject.activeSelf, "SelectedSquadPanel must be visible when selection HUD feedback reports a selected unit, squad, or building.");

            Image portraitFrame = instance.transform.Find("LeftContent/SelectedSquadPanel/Frame/PortraitFrame").GetComponent<Image>();
            Assert.NotNull(portraitFrame, "SelectedSquadPanel must expose the PortraitFrame image for selected unit/building portrait art.");
            Texture2D texture = new(4, 4);
            Sprite selectedPortrait = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            MatchHudSelectionPanelSystem.SetActiveSelectionVisible(true, selectedPortrait);
            Assert.AreEqual(selectedPortrait, portraitFrame.sprite, "Selected unit/building portraitActionSprite must update the PortraitFrame image.");
            Assert.IsTrue(portraitFrame.enabled);
            Assert.IsTrue(portraitFrame.preserveAspect);

            MatchHudSelectionPanelSystem.SetActiveSelectionVisible(false);
            Assert.IsFalse(panel.gameObject.activeSelf, "SelectedSquadPanel must hide when selection HUD feedback reports no active selection.");
            UnityEngine.Object.DestroyImmediate(selectedPortrait);
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMatchHudContent_BuildCommandInstallsConfiguredPopupWithoutPresenter()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject canvasObject = new("Canvas");
        canvasObject.AddComponent<Canvas>();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var inputSystem = new MatchOverlayCommandInputSystem();
        new ActiveMissionSession().Clear();
        try
        {
            instance.transform.SetParent(canvasObject.transform, false);
            Transform footer = instance.transform.Find("FooterContent");
            Assert.NotNull(footer);

            MatchOverlayCommandControlsView view = footer.GetComponent<MatchOverlayCommandControlsView>();
            Assert.NotNull(view);
            Assert.NotNull(view.BuildButton);
            Assert.NotNull(view.BuildDrawerPopupPrefab);

            inputSystem.Bind(view, null);
            view.BuildButton.onClick.Invoke();

            Assert.NotNull(canvasObject.transform.Find("SCN09_BuildDrawerPopup"));
            int buildIndex = FindCommandTabIndex(view.CommandTabGroup, view.BuildButton);
            int otherIndex = buildIndex == 0 ? 1 : 0;
            AssertCommandTabSelected(view.CommandTabGroup, buildIndex);

            view.CommandTabGroup.Tabs[otherIndex].Button.onClick.Invoke();

            Assert.IsNull(canvasObject.transform.Find("SCN09_BuildDrawerPopup"));
            AssertCommandTabSelected(view.CommandTabGroup, otherIndex);
        }
        finally
        {
            inputSystem.Unbind(instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true));
            UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            new ActiveMissionSession().Clear();
        }
    }

    [Test]
    public void ShellBuildDrawerPopup_CloseButtonOnlyClosesPopup()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellBuildDrawerPopupPrefabPath);
        Assert.NotNull(prefab, ShellBuildDrawerPopupPrefabPath);

        WarlineCapturePopupCloseView closeView = prefab.GetComponent<WarlineCapturePopupCloseView>();
        Assert.NotNull(closeView, "SCN09 popup root must own local close references.");
        Assert.NotNull(prefab.GetComponent<WarlineCapturePopupCloseSystem>(), "SCN09 popup root must own local close behavior system.");

        SerializedObject serializedCloseView = new(closeView);
        Button closeButton = serializedCloseView.FindProperty("closeButton").objectReferenceValue as Button;
        GameObject popupRoot = serializedCloseView.FindProperty("popupRoot").objectReferenceValue as GameObject;
        Transform topRightClose = prefab.transform.Find("BuildDrawerRoot/CloseButton");
        Assert.NotNull(topRightClose, "SCN09 must keep the top-right CloseButton.");
        Button topRightHotspot = topRightClose.Find("Hotspot")?.GetComponent<Button>();

        Assert.NotNull(closeButton, "SCN09 close behavior must serialize the CloseButton hotspot.");
        Assert.NotNull(topRightHotspot, "SCN09 top-right CloseButton must expose a clickable Hotspot button.");
        Assert.AreEqual(topRightHotspot, closeButton, "SCN09 close behavior must bind the visible top-right close hotspot.");
        Assert.AreEqual(prefab, popupRoot, "SCN09 close behavior must target only the popup root.");
        Assert.AreEqual(TacticalCommandMode.Build, closeView.CommandModeToClear,
            "SCN09 close must clear sticky Build mode without routing away from the match HUD.");
        Assert.AreEqual(0, topRightClose.GetComponentsInChildren<WarlineCaptureShellRouteButtonView>(true).Length,
            "SCN09 top-right close button must not submit a shell route request or return to MainMenu.");
    }

    [Test]
    public void ShellBuildDrawerPopup_CloseButtonDestroysOnlyPopupInstance()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellBuildDrawerPopupPrefabPath);
        Assert.NotNull(prefab, ShellBuildDrawerPopupPrefabPath);

        GameObject parent = new("PopupLayer");
        GameObject sibling = new("SiblingPopup");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            sibling.transform.SetParent(parent.transform, false);
            instance.transform.SetParent(parent.transform, false);

            WarlineCapturePopupCloseSystem closeSystem = instance.GetComponent<WarlineCapturePopupCloseSystem>();
            Assert.NotNull(closeSystem);

            closeSystem.ClosePopup();

            Assert.IsTrue(instance == null, "Close must destroy the SCN09 popup instance.");
            Assert.IsFalse(sibling == null, "Close must not clear unrelated popup layer content.");
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(sibling);
            UnityEngine.Object.DestroyImmediate(parent);
        }
    }

    [Test]
    public void ShellBuildDrawerPopup_CloseButtonClearsStickyBuildTab()
    {
        GameObject matchHudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        GameObject drawerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellBuildDrawerPopupPrefabPath);
        Assert.NotNull(matchHudPrefab, ShellMatchHudContentPrefabPath);
        Assert.NotNull(drawerPrefab, ShellBuildDrawerPopupPrefabPath);

        GameObject matchHud = (GameObject)PrefabUtility.InstantiatePrefab(matchHudPrefab);
        GameObject drawer = (GameObject)PrefabUtility.InstantiatePrefab(drawerPrefab);
        GameObject hudViewObject = new("BattleHudRuntimeFeedbackViewTest");
        var hudView = hudViewObject.AddComponent<BattleHudRuntimeFeedbackView>();
        try
        {
            MatchOverlayCommandControlsView view = matchHud.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            Assert.NotNull(view);
            Assert.NotNull(view.BuildButton);
            Assert.NotNull(view.CommandTabGroup);
            AssignCommandTabGroups(hudView, view.CommandTabGroup);

            int buildIndex = FindCommandTabIndex(view.CommandTabGroup, view.BuildButton);
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(hudView, TacticalCommandMode.Build);
            AssertCommandTabSelected(view.CommandTabGroup, buildIndex);

            WarlineCapturePopupCloseSystem closeSystem = drawer.GetComponent<WarlineCapturePopupCloseSystem>();
            Assert.NotNull(closeSystem);
            closeSystem.ClosePopup();

            Assert.AreEqual(TacticalCommandMode.None, BattleHudRuntimeFeedbackSystem.GetState(hudView).StickyCommandMode);
            AssertNoCommandTabSelected(view.CommandTabGroup);
            Assert.IsTrue(drawer == null, "Close must destroy the build drawer instance.");
        }
        finally
        {
            if (drawer != null)
                UnityEngine.Object.DestroyImmediate(drawer);
            UnityEngine.Object.DestroyImmediate(hudViewObject);
            UnityEngine.Object.DestroyImmediate(matchHud);
        }
    }

    [Test]
    public void ShellMatchHudContent_BuildCommandInvokesBuildDrawerAction()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var inputSystem = new MatchOverlayCommandInputSystem();
        int buildDrawerOpenCount = 0;
        new ActiveMissionSession().Clear();
        try
        {
            Transform footer = instance.transform.Find("FooterContent");
            Assert.NotNull(footer);

            MatchOverlayCommandControlsView view = footer.GetComponent<MatchOverlayCommandControlsView>();
            Assert.NotNull(view);
            Assert.NotNull(view.BuildButton);

            inputSystem.Bind(view, null, () => buildDrawerOpenCount++);
            view.BuildButton.onClick.Invoke();

            Assert.AreEqual(1, buildDrawerOpenCount);
            AssertCommandTabSelected(view.CommandTabGroup, FindCommandTabIndex(view.CommandTabGroup, view.BuildButton));
            view.BuildButton.onClick.Invoke();
            Assert.AreEqual(2, buildDrawerOpenCount);
            AssertCommandTabSelected(view.CommandTabGroup, FindCommandTabIndex(view.CommandTabGroup, view.BuildButton));
        }
        finally
        {
            inputSystem.Unbind(instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true));
            UnityEngine.Object.DestroyImmediate(instance);
            new ActiveMissionSession().Clear();
        }
    }

    [Test]
    public void ShellMatchHudContent_BuildCommandClosesDrawerWhenAnotherCommandTabIsSelected()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var inputSystem = new MatchOverlayCommandInputSystem();
        int buildDrawerOpenCount = 0;
        int buildDrawerCloseCount = 0;
        new ActiveMissionSession().Clear();
        try
        {
            Transform footer = instance.transform.Find("FooterContent");
            Assert.NotNull(footer);

            MatchOverlayCommandControlsView view = footer.GetComponent<MatchOverlayCommandControlsView>();
            Assert.NotNull(view);
            Assert.NotNull(view.BuildButton);
            Assert.NotNull(view.CommandTabGroup);

            int buildIndex = FindCommandTabIndex(view.CommandTabGroup, view.BuildButton);
            int otherIndex = buildIndex == 0 ? 1 : 0;

            inputSystem.Bind(
                view,
                null,
                () => buildDrawerOpenCount++,
                () => buildDrawerCloseCount++);

            view.BuildButton.onClick.Invoke();
            Assert.AreEqual(1, buildDrawerOpenCount);
            Assert.AreEqual(0, buildDrawerCloseCount);
            AssertCommandTabSelected(view.CommandTabGroup, buildIndex);

            view.CommandTabGroup.Tabs[otherIndex].Button.onClick.Invoke();

            Assert.AreEqual(1, buildDrawerOpenCount);
            Assert.AreEqual(1, buildDrawerCloseCount);
            AssertCommandTabSelected(view.CommandTabGroup, otherIndex);
        }
        finally
        {
            inputSystem.Unbind(instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true));
            UnityEngine.Object.DestroyImmediate(instance);
            new ActiveMissionSession().Clear();
        }
    }

    [Test]
    public void ShellMatchHudContent_CommandRailTabsKeepSingleSelectedSprite()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var inputSystem = new MatchOverlayCommandInputSystem();
        try
        {
            Transform footer = instance.transform.Find("FooterContent");
            Assert.NotNull(footer);

            MatchOverlayCommandControlsView view = footer.GetComponent<MatchOverlayCommandControlsView>();
            Assert.NotNull(view);
            Assert.NotNull(view.CommandTabGroup);

            inputSystem.Bind(view, null);

            AssertNoCommandTabSelected(view.CommandTabGroup);
            view.CommandTabGroup.Tabs[1].Button.onClick.Invoke();
            AssertCommandTabSelected(view.CommandTabGroup, 1);
            view.CommandTabGroup.Tabs[1].Button.onClick.Invoke();
            AssertNoCommandTabSelected(view.CommandTabGroup);
            view.CommandTabGroup.Tabs[2].Button.onClick.Invoke();
            AssertCommandTabSelected(view.CommandTabGroup, 2);
            view.CommandTabGroup.Tabs[6].Button.onClick.Invoke();
            AssertCommandTabSelected(view.CommandTabGroup, 6);
        }
        finally
        {
            inputSystem.Unbind(instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true));
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMatchHudContent_ClearCommandModeClearsCommandRailTabSelection()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObject viewObject = new("BattleHudRuntimeFeedbackViewTest");
        var feedbackView = viewObject.AddComponent<BattleHudRuntimeFeedbackView>();
        try
        {
            MatchOverlayCommandTabGroupView tabGroup = instance.GetComponentInChildren<MatchOverlayCommandTabGroupView>(true);
            Assert.NotNull(tabGroup);

            var tabVisualSystem = new MatchOverlayCommandTabVisualSystem(tabGroup);
            tabVisualSystem.Select(tabGroup.Tabs[1]);
            AssertCommandTabSelected(tabGroup, 1);

            BattleHudRuntimeFeedbackSystem.ClearCommandMode(feedbackView);

            AssertNoCommandTabSelected(tabGroup);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(viewObject);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMatchHudContent_BuildCommandModeKeepsBuildTabSelected()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObject viewObject = new("BattleHudRuntimeFeedbackViewTest");
        var feedbackView = viewObject.AddComponent<BattleHudRuntimeFeedbackView>();
        try
        {
            Transform footer = instance.transform.Find("FooterContent");
            Assert.NotNull(footer);

            MatchOverlayCommandControlsView controlsView = footer.GetComponent<MatchOverlayCommandControlsView>();
            Assert.NotNull(controlsView);
            Assert.NotNull(controlsView.BuildButton);
            Assert.NotNull(controlsView.CommandTabGroup);

            AssignCommandTabGroups(feedbackView, controlsView.CommandTabGroup);

            int buildIndex = FindCommandTabIndex(controlsView.CommandTabGroup, controlsView.BuildButton);
            new MatchOverlayCommandTabVisualSystem(controlsView.CommandTabGroup).Select(null);
            AssertNoCommandTabSelected(controlsView.CommandTabGroup);

            BattleHudRuntimeFeedbackSystem.ApplyCommandMode(feedbackView, TacticalCommandMode.Build);

            Assert.AreEqual(TacticalCommandMode.Build, BattleHudRuntimeFeedbackSystem.GetState(feedbackView).CurrentCommandMode);
            AssertCommandTabSelected(controlsView.CommandTabGroup, buildIndex);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(viewObject);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMatchHudContent_StickyBuildCommandModeSurvivesGenericClear()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObject viewObject = new("BattleHudRuntimeFeedbackViewTest");
        var feedbackView = viewObject.AddComponent<BattleHudRuntimeFeedbackView>();
        try
        {
            Transform footer = instance.transform.Find("FooterContent");
            Assert.NotNull(footer);

            MatchOverlayCommandControlsView controlsView = footer.GetComponent<MatchOverlayCommandControlsView>();
            Assert.NotNull(controlsView);
            Assert.NotNull(controlsView.BuildButton);
            Assert.NotNull(controlsView.CommandTabGroup);

            AssignCommandTabGroups(feedbackView, controlsView.CommandTabGroup);

            int buildIndex = FindCommandTabIndex(controlsView.CommandTabGroup, controlsView.BuildButton);
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(feedbackView, TacticalCommandMode.Build);
            AssertCommandTabSelected(controlsView.CommandTabGroup, buildIndex);

            BattleHudRuntimeFeedbackSystem.ClearCommandMode(feedbackView);

            BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackSystem.GetState(feedbackView);
            Assert.AreEqual(TacticalCommandMode.Build, state.StickyCommandMode);
            Assert.AreEqual(TacticalCommandMode.Build, state.CurrentCommandMode);
            AssertCommandTabSelected(controlsView.CommandTabGroup, buildIndex);

            BattleHudRuntimeFeedbackSystem.ClearStickyCommandMode(feedbackView, TacticalCommandMode.Build);

            state = BattleHudRuntimeFeedbackSystem.GetState(feedbackView);
            Assert.AreEqual(TacticalCommandMode.None, state.StickyCommandMode);
            Assert.AreEqual(TacticalCommandMode.None, state.CurrentCommandMode);
            AssertNoCommandTabSelected(controlsView.CommandTabGroup);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(viewObject);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMatchHudContent_SelectionHudClearCommandModeDoesNotClearStickyBuildTab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("ShellMatchHudContent_StickyBuildClear");
        World.DefaultGameObjectInjectionWorld = world;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObject viewObject = new("BattleHudRuntimeFeedbackViewTest");
        var feedbackView = viewObject.AddComponent<BattleHudRuntimeFeedbackView>();
        try
        {
            MatchOverlayCommandControlsView controlsView = instance.GetComponentInChildren<MatchOverlayCommandControlsView>(true);
            Assert.NotNull(controlsView);
            Assert.NotNull(controlsView.BuildButton);
            Assert.NotNull(controlsView.CommandTabGroup);

            AssignCommandTabGroups(feedbackView, controlsView.CommandTabGroup);
            int buildIndex = FindCommandTabIndex(controlsView.CommandTabGroup, controlsView.BuildButton);
            BattleHudRuntimeFeedbackSystem.ApplyStickyCommandMode(feedbackView, TacticalCommandMode.Build);
            AssertCommandTabSelected(controlsView.CommandTabGroup, buildIndex);

            new SelectionHudFeedbackSystem().ClearCommandMode(world.EntityManager);

            BattleHudRuntimeFeedbackState state = BattleHudRuntimeFeedbackSystem.GetState(feedbackView);
            Assert.AreEqual(TacticalCommandMode.Build, state.StickyCommandMode);
            Assert.AreEqual(TacticalCommandMode.Build, state.CurrentCommandMode);
            AssertCommandTabSelected(controlsView.CommandTabGroup, buildIndex);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(viewObject);
            UnityEngine.Object.DestroyImmediate(instance);
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ShellMatchHudContent_CommandRailTabToggleUsesVisibleStateAfterExternalClear()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            MatchOverlayCommandTabGroupView tabGroup = instance.GetComponentInChildren<MatchOverlayCommandTabGroupView>(true);
            Assert.NotNull(tabGroup);

            var inputVisualSystem = new MatchOverlayCommandTabVisualSystem(tabGroup);
            var bridgeVisualSystem = new MatchOverlayCommandTabVisualSystem(tabGroup);
            MatchOverlayCommandTabView selectTab = tabGroup.Tabs[1];

            Assert.IsTrue(inputVisualSystem.Toggle(selectTab));
            AssertCommandTabSelected(tabGroup, 1);

            bridgeVisualSystem.Select(null);
            AssertNoCommandTabSelected(tabGroup);

            Assert.IsTrue(inputVisualSystem.Toggle(selectTab),
                "After drag completion clears the visible tab state, the input binding must treat Select as deselected.");
            AssertCommandTabSelected(tabGroup, 1);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMatchHudContent_SelectionHudClearCommandModeClearsTabWithoutRuntimeFeedbackView()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMatchHudContentPrefabPath);
        Assert.NotNull(prefab, ShellMatchHudContentPrefabPath);

        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("ShellMatchHudContent_ClearCommandMode");
        World.DefaultGameObjectInjectionWorld = world;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            MatchOverlayCommandTabGroupView tabGroup = instance.GetComponentInChildren<MatchOverlayCommandTabGroupView>(true);
            Assert.NotNull(tabGroup);

            new MatchOverlayCommandTabVisualSystem(tabGroup).Select(tabGroup.Tabs[1]);
            AssertCommandTabSelected(tabGroup, 1);

            new SelectionHudFeedbackSystem().ClearCommandMode(world.EntityManager);

            AssertNoCommandTabSelected(tabGroup);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void ShellMainMenuContent_LeftNavTabVisualsMoveOnClick()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMainMenuContentPrefabPath);
        Assert.NotNull(prefab, ShellMainMenuContentPrefabPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            MainMenuNavigationView navigationView = instance.GetComponentInChildren<MainMenuNavigationView>(true);
            Assert.NotNull(navigationView);
            InvokeMainMenuNavigationOnEnable(navigationView);

            AssertMainMenuRuntimeSelectedState(instance, "LeftContent/LeftNavPanel/Nav_Leaderboards");
            InvokeButton(instance, "LeftContent/LeftNavPanel/Nav_Store");
            AssertMainMenuRuntimeSelectedState(instance, "LeftContent/LeftNavPanel/Nav_Store");
            InvokeMainMenuNavigationOnEnable(navigationView);
            AssertMainMenuRuntimeSelectedState(instance, "LeftContent/LeftNavPanel/Nav_Store");
            InvokeButton(instance, "LeftContent/LeftNavPanel/Nav_Contests");
            AssertMainMenuRuntimeSelectedState(instance, "LeftContent/LeftNavPanel/Nav_Contests");
            InvokeButton(instance, "LeftContent/LeftNavPanel/Nav_Armory");
            AssertMainMenuRuntimeSelectedState(instance, "LeftContent/LeftNavPanel/Nav_Armory");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ShellMainMenuContent_CommanderPanelRoutesToCommanderProfileContent()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellMainMenuContentPrefabPath);
        Assert.NotNull(prefab, ShellMainMenuContentPrefabPath);

        AssertShellRouteButton(prefab, "RightContent/CommanderPanel/CommanderPanelHotspot", WarlineCaptureRoute.CommanderProfile);
    }

    [Test]
    public void ShellCommanderProfileContent_ArmoryRoutesPushHistory()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellCommanderProfileContentPrefabPath);
        Assert.NotNull(prefab, ShellCommanderProfileContentPrefabPath);

        AssertShellRouteButton(prefab, "RightContent/ArmorySquadsPanel/OpenArmoryButton/Hotspot", WarlineCaptureRoute.Armory, UiShellRouteIntent.OpenMenuRoute, true);
        AssertShellRouteButton(prefab, "FooterContent/RouteStrip/ArmoryHotspot", WarlineCaptureRoute.Armory, UiShellRouteIntent.OpenMenuRoute, true);
    }

    [Test]
    public void ShellArmoryContent_BackUsesRouteHistoryWithMainMenuFallback()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellArmoryContentPrefabPath);
        Assert.NotNull(prefab, ShellArmoryContentPrefabPath);

        AssertShellRouteButton(prefab, "LeftContent/ArmoryTitleBlock/BackHotspot", WarlineCaptureRoute.MainMenu, UiShellRouteIntent.BackMenuRoute, false);
    }

    [Test]
    public void ShellArmoryContent_UsesViewsAndCatalogQueryForRuntimeRoster()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellArmoryContentPrefabPath);
        Assert.NotNull(prefab, ShellArmoryContentPrefabPath);

        ArmoryCategoryNavigationView navigationView = prefab.transform
            .Find("LeftContent")
            .GetComponent<ArmoryCategoryNavigationView>();
        Assert.NotNull(navigationView);
        AssertNavigationTabsAssigned(new SerializedObject(navigationView).FindProperty("tabs"), 5, "Armory");

        ArmoryContentListView listView = prefab.transform
            .Find("MiddleContent")
            .GetComponent<ArmoryContentListView>();
        Assert.NotNull(listView);

        var serializedList = new SerializedObject(listView);
        Assert.NotNull(serializedList.FindProperty("unitPrefabRegistryConfig").objectReferenceValue);
        Assert.NotNull(serializedList.FindProperty("buildingPlacementConfig").objectReferenceValue);
        Assert.AreEqual(prefab.transform.Find("MiddleContent/Scroll View/Viewport/Content") as RectTransform,
            serializedList.FindProperty("contentRoot").objectReferenceValue);
        ArmoryCatalogItemView itemView = prefab.transform
            .Find("MiddleContent/Scroll View/Viewport/Content/ItemView")
            .GetComponent<ArmoryCatalogItemView>();
        Assert.NotNull(itemView);
        ArmoryInspectionPanelView inspectionPanel = prefab.transform
            .Find("RightContent/InspectionPanel")
            .GetComponent<ArmoryInspectionPanelView>();
        Assert.NotNull(inspectionPanel);
        Assert.AreEqual(itemView, serializedList.FindProperty("itemTemplate").objectReferenceValue);
        Assert.Null(serializedList.FindProperty("inspectionPanel"));
        ArmoryRightContentView rightContentView = prefab.transform
            .Find("RightContent")
            .GetComponent<ArmoryRightContentView>();
        Assert.NotNull(rightContentView);
        var serializedRightContent = new SerializedObject(rightContentView);
        Assert.AreEqual(inspectionPanel, serializedRightContent.FindProperty("inspectionPanel").objectReferenceValue);
        Assert.NotNull(prefab.transform.Find("MiddleContent/Scroll View/Viewport/Content/ItemView").GetComponent<Image>());
        Assert.NotNull(prefab.transform.Find("MiddleContent/Scroll View/Viewport/Content/ItemView").GetComponent<Button>());
        AssertArmoryItemRootButton(itemView);
        AssertArmoryItemSelectionFrame(itemView);
        AssertArmoryInspectionPanelReferences(inspectionPanel);

        Assert.IsFalse(File.Exists("Assets/Game/Scripts/UI/Screens/ArmoryContentListController.cs"));
        Assert.IsFalse(File.Exists("Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationController.cs"));
        StringAssert.DoesNotContain("static event", File.ReadAllText("Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationView.cs"));
        StringAssert.Contains("public sealed class ArmoryCatalogQuerySystem",
            File.ReadAllText("Assets/Game/Scripts/UI/Screens/ArmoryCatalogQuerySystem.cs"));
        AssertArmoryRuntimeUiCodeDoesNotUseHierarchyStringLookup();

        AssertArmoryNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Characters", true);
        AssertArmoryNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Vehicles", false);
        AssertArmoryNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Aircrafts", false);
        AssertArmoryNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Buildings", false);
        AssertArmoryNavTabButton(prefab, "LeftContent/LeftNavPanel/Nav_Support", false);
    }

    [Test]
    public void ShellArmoryContent_ItemTemplateUsesCategoryBackgroundsForPortraits()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellArmoryContentPrefabPath);
        Assert.NotNull(prefab, ShellArmoryContentPrefabPath);

        Transform template = prefab.transform.Find("MiddleContent/Scroll View/Viewport/Content/ItemView");
        Assert.NotNull(template);

        ArmoryCatalogItemView item = ((GameObject)PrefabUtility.InstantiatePrefab(template.gameObject))
            .GetComponent<ArmoryCatalogItemView>();
        Texture2D texture = new(2, 2);
        Sprite sprite = null;
        try
        {
            texture.SetPixel(0, 0, Color.white);
            texture.SetPixel(1, 0, Color.white);
            texture.SetPixel(0, 1, Color.white);
            texture.SetPixel(1, 1, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

            item.Bind(new ArmoryCatalogItem("Character", sprite, ArmoryCatalogCategory.Characters));
            AssertArmoryItemBackgroundState(item.gameObject, "Background_Character", sprite);

            item.Bind(new ArmoryCatalogItem("Vehicle", sprite, ArmoryCatalogCategory.Vehicles));
            AssertArmoryItemBackgroundState(item.gameObject, "Background_Vehicle", sprite);

            item.Bind(new ArmoryCatalogItem("Aircraft", sprite, ArmoryCatalogCategory.Aircrafts));
            AssertArmoryItemBackgroundState(item.gameObject, "Background_Aircraft", sprite);

            item.Bind(new ArmoryCatalogItem("Building", sprite, ArmoryCatalogCategory.Buildings));
            AssertArmoryItemBackgroundState(item.gameObject, "Background_Building", sprite);
        }
        finally
        {
            if (sprite != null)
                UnityEngine.Object.DestroyImmediate(sprite);

            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(item.gameObject);
        }
    }

    [Test]
    public void ShellArmoryContent_UsesSecondaryPortraitFallbacks()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellArmoryContentPrefabPath);
        Assert.NotNull(prefab, ShellArmoryContentPrefabPath);

        Transform template = prefab.transform.Find("MiddleContent/Scroll View/Viewport/Content/ItemView");
        Assert.NotNull(template);

        ArmoryCatalogItemView item = ((GameObject)PrefabUtility.InstantiatePrefab(template.gameObject))
            .GetComponent<ArmoryCatalogItemView>();
        GameObject right = (GameObject)PrefabUtility.InstantiatePrefab(prefab.transform.Find("RightContent").gameObject);
        Texture2D baseTexture = null;
        Texture2D cardTexture = null;
        Texture2D actionTexture = null;
        Sprite baseSprite = null;
        Sprite cardSprite = null;
        Sprite actionSprite = null;
        try
        {
            baseSprite = CreateTestSprite(Color.white, out baseTexture);
            cardSprite = CreateTestSprite(Color.cyan, out cardTexture);
            actionSprite = CreateTestSprite(Color.red, out actionTexture);

            ArmoryRightContentView rightContentView = right.GetComponent<ArmoryRightContentView>();
            Assert.NotNull(rightContentView);
            Assert.NotNull(rightContentView.InspectionPanel);

            ArmoryCatalogItem fullSecondary =
                new("Character", baseSprite, cardSprite, actionSprite, ArmoryCatalogCategory.Characters);
            item.Bind(fullSecondary);
            AssertArmoryItemBackgroundState(item.gameObject, "Background_Character", cardSprite);
            rightContentView.InspectionPanel.Bind(fullSecondary);
            AssertArmoryItemBackgroundState(rightContentView.InspectionPanel.gameObject, "Background_Character", actionSprite);

            ArmoryCatalogItem noAction =
                new("Character", baseSprite, cardSprite, null, ArmoryCatalogCategory.Characters);
            rightContentView.InspectionPanel.Bind(noAction);
            AssertArmoryItemBackgroundState(rightContentView.InspectionPanel.gameObject, "Background_Character", cardSprite);

            ArmoryCatalogItem noSecondary =
                new("Character", baseSprite, null, null, ArmoryCatalogCategory.Characters);
            item.Bind(noSecondary);
            AssertArmoryItemBackgroundState(item.gameObject, "Background_Character", baseSprite);
            rightContentView.InspectionPanel.Bind(noSecondary);
            AssertArmoryItemBackgroundState(rightContentView.InspectionPanel.gameObject, "Background_Character", baseSprite);
        }
        finally
        {
            if (actionSprite != null)
                UnityEngine.Object.DestroyImmediate(actionSprite);
            if (cardSprite != null)
                UnityEngine.Object.DestroyImmediate(cardSprite);
            if (baseSprite != null)
                UnityEngine.Object.DestroyImmediate(baseSprite);
            if (actionTexture != null)
                UnityEngine.Object.DestroyImmediate(actionTexture);
            if (cardTexture != null)
                UnityEngine.Object.DestroyImmediate(cardTexture);
            if (baseTexture != null)
                UnityEngine.Object.DestroyImmediate(baseTexture);

            UnityEngine.Object.DestroyImmediate(item.gameObject);
            UnityEngine.Object.DestroyImmediate(right);
        }
    }

    [Test]
    public void ShellArmoryContent_ItemSelectionUpdatesInspectionPanel()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellArmoryContentPrefabPath);
        Assert.NotNull(prefab, ShellArmoryContentPrefabPath);

        GameObject middle = (GameObject)PrefabUtility.InstantiatePrefab(prefab.transform.Find("MiddleContent").gameObject);
        GameObject right = (GameObject)PrefabUtility.InstantiatePrefab(prefab.transform.Find("RightContent").gameObject);
        Texture2D texture = new(2, 2);
        Sprite sprite = null;
        try
        {
            texture.SetPixel(0, 0, Color.white);
            texture.SetPixel(1, 0, Color.white);
            texture.SetPixel(0, 1, Color.white);
            texture.SetPixel(1, 1, Color.white);
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

            ArmoryContentListView listView = middle.GetComponent<ArmoryContentListView>();
            Assert.NotNull(listView);
            ArmoryRightContentView rightContentView = right.GetComponent<ArmoryRightContentView>();
            Assert.NotNull(rightContentView);
            Assert.NotNull(rightContentView.InspectionPanel);
            listView.SetInspectionPanel(rightContentView.InspectionPanel);

            ArmoryCatalogItemView item = middle.transform
                .Find("Scroll View/Viewport/Content/ItemView")
                .GetComponent<ArmoryCatalogItemView>();
            GameObject inspectionPanel = rightContentView.InspectionPanel.gameObject;
            Sprite defaultFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ArmoryRosterCardDefaultFramePath);
            Sprite selectedFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ArmoryRosterCardSelectedFramePath);
            Assert.AreEqual(defaultFrame, item.FrameImage.sprite);

            InvokeArmoryWireItemSelection(listView, item, new ArmoryCatalogItem("Character", sprite, ArmoryCatalogCategory.Characters));
            item.SelectionButton.onClick.Invoke();
            AssertArmoryItemBackgroundState(inspectionPanel, "Background_Character", sprite);
            Assert.AreEqual(selectedFrame, item.FrameImage.sprite);

            InvokeArmoryWireItemSelection(listView, item, new ArmoryCatalogItem("Vehicle", sprite, ArmoryCatalogCategory.Vehicles));
            item.SelectionButton.onClick.Invoke();
            AssertArmoryItemBackgroundState(inspectionPanel, "Background_Vehicle", sprite);

            InvokeArmoryWireItemSelection(listView, item, new ArmoryCatalogItem("Aircraft", sprite, ArmoryCatalogCategory.Aircrafts));
            item.SelectionButton.onClick.Invoke();
            AssertArmoryItemBackgroundState(inspectionPanel, "Background_Aircraft", sprite);

            InvokeArmoryWireItemSelection(listView, item, new ArmoryCatalogItem("Building", sprite, ArmoryCatalogCategory.Buildings));
            item.SelectionButton.onClick.Invoke();
            AssertArmoryItemBackgroundState(inspectionPanel, "Background_Building", sprite);
        }
        finally
        {
            if (sprite != null)
                UnityEngine.Object.DestroyImmediate(sprite);

            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(middle);
            UnityEngine.Object.DestroyImmediate(right);
        }
    }

    [Test]
    public void ShellArmoryContent_CategoryHotspotsSelectExpectedRuntimeCategories()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using var world = new World("ArmoryCategoryNavigationTestWorld");
        World.DefaultGameObjectInjectionWorld = world;

        EntityManager entityManager = world.EntityManager;
        Entity boundary = entityManager.CreateEntity(
            typeof(UiShellBoundaryComponent),
            typeof(UiShellArmoryCategoryComponent));
        entityManager.SetComponentData(boundary, new UiShellArmoryCategoryComponent
        {
            Category = ArmoryCatalogCategory.Characters
        });
        entityManager.AddBuffer<UiShellArmoryCategoryRequestComponent>(boundary);
        entityManager.AddBuffer<UiShellRouteRequestComponent>(boundary);
        SystemHandle categorySystem = world.CreateSystem<UiShellArmoryCategorySystem>();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellArmoryContentPrefabPath);
        Assert.NotNull(prefab, ShellArmoryContentPrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.SetActive(false);
        instance.SetActive(true);
        ArmoryCategoryNavigationView navigationView = instance.GetComponentInChildren<ArmoryCategoryNavigationView>(true);
        Assert.NotNull(navigationView);
        InvokeArmoryNavigationOnEnable(navigationView);
        try
        {
            AssertArmoryCategoryHotspot(instance, world, categorySystem, entityManager, boundary,
                "LeftContent/LeftNavPanel/Nav_Vehicles", ArmoryCatalogCategory.Vehicles);
            AssertArmoryCategoryHotspot(instance, world, categorySystem, entityManager, boundary,
                "LeftContent/LeftNavPanel/Nav_Aircrafts", ArmoryCatalogCategory.Aircrafts);
            AssertArmoryCategoryHotspot(instance, world, categorySystem, entityManager, boundary,
                "LeftContent/LeftNavPanel/Nav_Buildings", ArmoryCatalogCategory.Buildings);
            AssertArmoryCategoryHotspot(instance, world, categorySystem, entityManager, boundary,
                "LeftContent/LeftNavPanel/Nav_Support", ArmoryCatalogCategory.Support);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void MatchResultFlow_ShowsRuntimeMissionResultAndContinuesToReturnRoute()
    {
        GameObject shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPrefabPath);
        Assert.NotNull(shellPrefab);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shellPrefab);
        try
        {
            var router = instance.GetComponent<WarlineCaptureRouter>();
            var resultFlow = instance.GetComponent<WarlineCaptureMatchResultFlow>();
            Assert.NotNull(router);
            Assert.NotNull(resultFlow);
            router.Initialize();

            var result = new MissionResultData(
                "saga.ch01.m01.first_contact",
                "First Contact",
                true,
                2,
                6,
                0,
                1,
                120,
                new[]
                {
                    new ObjectiveRuntimeState("destroy", "Destroy the forward patrol", ObjectiveType.DestroyAllEnemies, 6, 6, true, true)
                });

            resultFlow.ShowResult(result, WarlineCaptureRoute.SagaMap);

            Transform modalOverlay = instance.transform.Find("SafeAreaRoot/ModalOverlay");
            Assert.IsTrue(modalOverlay.gameObject.activeSelf);
            Assert.AreEqual(WarlineCaptureRoute.Match, router.ActiveRoute);
            Transform popup = modalOverlay.Find("MissionResultPopup(Clone)");
            Assert.NotNull(popup);
            Assert.AreEqual("First Contact", popup.Find("Frame/Header/MissionNameText").GetComponent<TMP_Text>().text);

            Button continueButton = popup.Find("Frame/ButtonRow/ContinueButton").GetComponent<Button>();
            continueButton.onClick.Invoke();

            Assert.AreEqual(WarlineCaptureRoute.SagaMap, router.ActiveRoute);
            Assert.IsFalse(modalOverlay.gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void ScreenPrefabs_UseOxaniumFamilyForAllText()
    {
        foreach (string prefabPath in GenericShellScreenPrefabPaths)
        {
            string prefabText = File.ReadAllText(prefabPath);
            MatchCollection fontMatches = Regex.Matches(prefabText, @"m_fontAsset: \{fileID: 11400000, guid: ([a-f0-9]+), type: 2\}");

            Assert.Greater(fontMatches.Count, 0, prefabPath);
            foreach (Match fontMatch in fontMatches)
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(fontMatch.Groups[1].Value);
                StringAssert.StartsWith(OxaniumFontFolder, fontPath, prefabPath);
                StringAssert.Contains("Oxanium", Path.GetFileNameWithoutExtension(fontPath), prefabPath);
            }
        }
    }

    [Test]
    public void ScreenPrefabs_DisableDecorativeGraphicRaycasts()
    {
        foreach (string prefabPath in GenericShellScreenPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.NotNull(prefab, prefabPath);

            foreach (Graphic graphic in prefab.GetComponentsInChildren<Graphic>(true))
            {
                bool expectedRaycast = IsInteractiveRaycastGraphic(prefab, graphic);
                Assert.AreEqual(expectedRaycast, graphic.raycastTarget, $"{prefabPath}:{GetHierarchyPath(graphic.transform)} has an incorrect raycastTarget value.");
            }
        }
    }

    [Test]
    public void SplashPrefab_UsesVisualLockStructureAndSeparateRuntimeArt()
    {
        GameObject splashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SplashPrefabPath);
        Assert.NotNull(splashPrefab);

        Assert.NotNull(splashPrefab.transform.Find("BackdropVignette"));
        Assert.NotNull(splashPrefab.transform.Find("OuterHudFrame"));
        Assert.NotNull(splashPrefab.transform.Find("LogoRoot/LogoImage"));
        Assert.NotNull(splashPrefab.transform.Find("TitleWordmark"));
        Assert.NotNull(splashPrefab.transform.Find("LoadingBar/LoadingLabelText"));
        Assert.NotNull(splashPrefab.transform.Find("LoadingBar/ProgressTrack/Fill"));
        Assert.NotNull(splashPrefab.transform.Find("LoadingBar/PercentText"));
        Assert.NotNull(splashPrefab.transform.Find("TipText"));
        Assert.NotNull(splashPrefab.transform.Find("BottomStatusStrip/SecureLinkText"));
        Assert.NotNull(splashPrefab.transform.Find("BottomStatusStrip/CenterStatusText"));
        Assert.NotNull(splashPrefab.transform.Find("BottomStatusStrip/SyncDataText"));
        Assert.IsNull(splashPrefab.transform.Find("StartButton"));
        Assert.IsNull(splashPrefab.GetComponentInChildren<ScreenRouteSystem>(true));
        SplashScreenSystem splashController = splashPrefab.GetComponent<SplashScreenSystem>();
        Assert.NotNull(splashController);
        var splashSerialized = new SerializedObject(splashController);
        Assert.IsNull(splashSerialized.FindProperty("fakeLoadingSeconds"));
        Assert.IsNull(splashSerialized.FindProperty("routeAfterFakeLoad"));

        AssertImageSpritePath(splashPrefab.transform, string.Empty, SplashBackgroundPath);
        AssertImageSpritePath(splashPrefab.transform, "OuterHudFrame", SplashOuterFramePath);
        AssertImageDoesNotUseSpritePath(splashPrefab.transform, "LogoRoot/LogoImage", "Assets/Game/Textures/Logo.png");
        AssertImageSpritePath(splashPrefab.transform, "LogoRoot/LogoImage", SplashLogoEmblemPath);
        AssertImageSpritePath(splashPrefab.transform, "TitleWordmark", SplashTitleWordmarkPath);
        AssertImageSpritePath(splashPrefab.transform, "LoadingBar", SplashLoadingPanelPath);
        AssertImageSpritePath(splashPrefab.transform, "LoadingBar/ProgressTrack", SplashProgressTrackPath);
        AssertImageSpritePath(splashPrefab.transform, "LoadingBar/ProgressTrack/Fill", SplashProgressFillPath);
        AssertImageSpritePath(splashPrefab.transform, "BottomStatusStrip", SplashBottomPanelPath);

        Assert.IsNull(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Art/UI/Generated/Splash/SCN-01_SplashLoading_Landscape_Target.png"));
    }

    [Test]
    public void PhaseOneComponentPrefabs_Exist()
    {
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Components/ModeCardView.prefab"));
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Components/ResourceCounterView.prefab"));
        Assert.NotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Components/ActionButtonView.prefab"));
    }

    [Test]
    public void Router_ShowsInitialRouteAndNavigatesBack()
    {
        var root = new GameObject("RouterRoot");
        var contentRoot = new GameObject("ContentRoot");
        var splash = CreateScreen("Screen_Splash", WarlineCaptureRoute.Splash);
        var mainMenu = CreateScreen("Screen_MainMenu", WarlineCaptureRoute.MainMenu);
        var settings = CreateScreen("Screen_Settings", WarlineCaptureRoute.Settings);

        try
        {
            contentRoot.transform.SetParent(root.transform);
            splash.transform.SetParent(contentRoot.transform);
            mainMenu.transform.SetParent(contentRoot.transform);
            settings.transform.SetParent(contentRoot.transform);

            WarlineCaptureRouter router = root.AddComponent<WarlineCaptureRouter>();
            router.ConfigureForTests(
                new[]
                {
                    splash.GetComponent<WarlineCaptureScreenSystem>(),
                    mainMenu.GetComponent<WarlineCaptureScreenSystem>(),
                    settings.GetComponent<WarlineCaptureScreenSystem>()
                },
                WarlineCaptureRoute.Splash);

            Assert.IsTrue(router.HasActiveRoute);
            Assert.AreEqual(WarlineCaptureRoute.Splash, router.ActiveRoute);
            Assert.IsTrue(splash.activeSelf);
            Assert.IsFalse(mainMenu.activeSelf);

            router.GoTo(WarlineCaptureRoute.MainMenu);
            router.GoTo(WarlineCaptureRoute.Settings);

            Assert.AreEqual(WarlineCaptureRoute.Settings, router.ActiveRoute);
            Assert.IsFalse(mainMenu.activeSelf);
            Assert.IsTrue(settings.activeSelf);

            Assert.IsTrue(router.Back());
            Assert.AreEqual(WarlineCaptureRoute.MainMenu, router.ActiveRoute);
            Assert.IsTrue(mainMenu.activeSelf);
            Assert.IsFalse(settings.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ShellAccessibilityApplier_ScalesContentRootForLargeText()
    {
        GameObject shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPrefabPath);
        Assert.NotNull(shellPrefab);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shellPrefab);
        try
        {
            WarlineCaptureUiAccessibilityApplier accessibilityApplier = instance.GetComponent<WarlineCaptureUiAccessibilityApplier>();
            Assert.NotNull(accessibilityApplier);
            Transform contentRoot = instance.transform.Find("SafeAreaRoot/ContentRoot");
            Assert.NotNull(contentRoot);

            WarlineCaptureSettingsModel model = SettingsService.Defaults;
            model.Accessibility.LargeText = true;
            accessibilityApplier.Apply(model);
            Assert.AreEqual(1.08f, contentRoot.localScale.x, 0.001f);
            Assert.AreEqual(1.08f, contentRoot.localScale.y, 0.001f);

            model.Accessibility.LargeText = false;
            accessibilityApplier.Apply(model);
            Assert.AreEqual(1f, contentRoot.localScale.x, 0.001f);
            Assert.AreEqual(1f, contentRoot.localScale.y, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void Router_MissingRouteThrowsClearly()
    {
        var root = new GameObject("RouterRoot");
        var splash = CreateScreen("Screen_Splash", WarlineCaptureRoute.Splash);

        try
        {
            splash.transform.SetParent(root.transform);
            WarlineCaptureRouter router = root.AddComponent<WarlineCaptureRouter>();
            router.ConfigureForTests(new[] { splash.GetComponent<WarlineCaptureScreenSystem>() }, WarlineCaptureRoute.Splash);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => router.GoTo(WarlineCaptureRoute.Settings));
            StringAssert.Contains("Settings", exception.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void ModalController_ClosesOverlayByDefaultAndCanReopen()
    {
        var root = new GameObject("ModalRoot");
        var overlay = new GameObject("ModalOverlay");

        try
        {
            overlay.transform.SetParent(root.transform);
            overlay.SetActive(true);
            WarlineCaptureModalSystem modal = root.AddComponent<WarlineCaptureModalSystem>();
            SetPrivateField(modal, "modalOverlay", overlay);
            InvokePrivate(modal, "Awake");

            Assert.IsFalse(overlay.activeSelf);

            modal.ShowModal(null);
            Assert.IsTrue(overlay.activeSelf);

            modal.CloseModal();
            Assert.IsFalse(overlay.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LoadingTipsAsset_ContainsPhaseOneTips()
    {
        WarlineCaptureLoadingTips tips = AssetDatabase.LoadAssetAtPath<WarlineCaptureLoadingTips>("Assets/Game/Configs/UI/LoadingTips.asset");

        Assert.NotNull(tips);
        Assert.GreaterOrEqual(tips.Count, 5);
        StringAssert.Contains("roads", tips.GetTip(1).ToLowerInvariant());
    }

    [Test]
    public void SplashScreen_BindsProgressStatusAndTips()
    {
        var root = new GameObject("Screen_Splash");
        var fillObject = new GameObject("Fill");
        var percentObject = new GameObject("PercentText");
        var statusObject = new GameObject("StatusText");
        var tipObject = new GameObject("TipText");

        try
        {
            fillObject.transform.SetParent(root.transform);
            percentObject.transform.SetParent(root.transform);
            statusObject.transform.SetParent(root.transform);
            tipObject.transform.SetParent(root.transform);

            SplashScreenSystem splash = root.AddComponent<SplashScreenSystem>();
            Image fill = fillObject.AddComponent<Image>();
            TMP_Text percent = percentObject.AddComponent<TextMeshProUGUI>();
            TMP_Text status = statusObject.AddComponent<TextMeshProUGUI>();
            TMP_Text tip = tipObject.AddComponent<TextMeshProUGUI>();
            WarlineCaptureLoadingTips tips = AssetDatabase.LoadAssetAtPath<WarlineCaptureLoadingTips>("Assets/Game/Configs/UI/LoadingTips.asset");

            SetPrivateField(splash, "loadingBarFill", fill);
            SetPrivateField(splash, "percentText", percent);
            SetPrivateField(splash, "statusText", status);
            SetPrivateField(splash, "tipText", tip);

            splash.Bind(tips);
            splash.SetProgress(0.5f);
            splash.SetStatus("LOADING ASSETS... 50%");
            splash.RefreshTip(2);

            Assert.AreEqual(0.5f, fill.fillAmount, 0.001f);
            Assert.AreEqual("50%", percent.text);
            Assert.AreEqual("LOADING ASSETS... 50%", status.text);
            Assert.AreEqual(tips.GetTip(2), tip.text);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static GameObject CreateScreen(string name, WarlineCaptureRoute route)
    {
        var screen = new GameObject(name, typeof(RectTransform));
        WarlineCaptureScreenSystem controller = screen.AddComponent<WarlineCaptureScreenSystem>();
        controller.SetRouteForTests(route);
        return screen;
    }

    private static void AssertShellRouteButton(
        GameObject prefab,
        string path,
        WarlineCaptureRoute route,
        UiShellRouteIntent intent = UiShellRouteIntent.OpenMenuRoute,
        bool pushHistory = false)
    {
        Transform hotspot = prefab.transform.Find(path);
        Assert.NotNull(hotspot, $"Main menu shell content must keep the route hotspot at {path}.");

        WarlineCaptureShellRouteButtonView routeButton = hotspot.GetComponent<WarlineCaptureShellRouteButtonView>();
        Assert.NotNull(routeButton, $"{path} must submit a shell route request.");
        Assert.AreEqual(intent, routeButton.Intent);
        Assert.AreEqual(route, routeButton.Route);
        Assert.AreEqual(pushHistory, routeButton.PushHistory);
    }

    private static void AssertMainMenuNavTabButton(
        GameObject prefab,
        string path,
        WarlineCaptureRoute route,
        bool selected,
        bool pushHistory)
    {
        Transform nav = prefab.transform.Find(path);
        Assert.NotNull(nav, $"{path} must exist on Main Menu content.");
        Assert.NotNull(nav.GetComponent<Button>(), $"{path} must be the Main Menu tab Button.");
        Assert.Null(nav.Find("Hotspot"), $"{path} must not keep a nested Hotspot button.");
        Assert.Null(nav.Find("Frame/Hotspot"), $"{path} must not keep a nested Frame/Hotspot button.");

        WarlineCaptureShellRouteButtonView routeButton = nav.GetComponent<WarlineCaptureShellRouteButtonView>();
        Assert.NotNull(routeButton, $"{path} must submit a shell route request.");
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, routeButton.Intent);
        Assert.AreEqual(route, routeButton.Route);
        Assert.AreEqual(pushHistory, routeButton.PushHistory);

        Image frame = nav.Find("Frame")?.GetComponent<Image>();
        Assert.NotNull(frame, $"{path}/Frame must keep the tab visual.");
        string spriteName = frame.sprite != null ? frame.sprite.name.ToLowerInvariant() : string.Empty;
        if (selected)
            StringAssert.Contains("selected", spriteName, $"{path}/Frame must use the selected tab state.");
        else
            StringAssert.Contains("inactive", spriteName, $"{path}/Frame must use the inactive tab state.");
    }

    private static void AssertMainMenuRuntimeSelectedState(GameObject root, string selectedPath)
    {
        AssertMainMenuRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Leaderboards", selectedPath == "LeftContent/LeftNavPanel/Nav_Leaderboards");
        AssertMainMenuRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Armory", selectedPath == "LeftContent/LeftNavPanel/Nav_Armory");
        AssertMainMenuRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Store", selectedPath == "LeftContent/LeftNavPanel/Nav_Store");
        AssertMainMenuRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Contests", selectedPath == "LeftContent/LeftNavPanel/Nav_Contests");
        AssertMainMenuRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Tutorials", selectedPath == "LeftContent/LeftNavPanel/Nav_Tutorials");
    }

    private static void AssertMainMenuRuntimeTabState(GameObject root, string path, bool selected)
    {
        Image frame = root.transform.Find(path)?.Find("Frame")?.GetComponent<Image>();
        Assert.NotNull(frame, $"{path}/Frame must exist.");
        string spriteName = frame.sprite != null ? frame.sprite.name.ToLowerInvariant() : string.Empty;
        if (selected)
            StringAssert.Contains("selected", spriteName, $"{path} must show selected state.");
        else
            StringAssert.Contains("inactive", spriteName, $"{path} must show inactive state.");
    }

    private static void InvokeButton(GameObject root, string path)
    {
        Button button = root.transform.Find(path)?.GetComponent<Button>();
        Assert.NotNull(button, $"{path} must have a Button.");
        button.onClick.Invoke();
    }

    private static void AssertArmoryNavTabButton(GameObject prefab, string path, bool selected)
    {
        Transform nav = prefab.transform.Find(path);
        Assert.NotNull(nav, $"{path} must exist on Armory content.");
        Assert.NotNull(nav.GetComponent<Button>(), $"{path} must be the Armory tab Button.");
        Assert.Null(nav.Find("Frame/Hotspot"), $"{path} must not keep a nested Frame/Hotspot button.");

        Image frame = nav.Find("Frame")?.GetComponent<Image>();
        Assert.NotNull(frame, $"{path}/Frame must keep the tab visual.");
        string spriteName = frame.sprite != null ? frame.sprite.name : string.Empty;
        if (selected)
            StringAssert.Contains("selected", spriteName.ToLowerInvariant(), $"{path}/Frame must use the selected tab state.");
        else
            StringAssert.Contains("inactive", spriteName.ToLowerInvariant(), $"{path}/Frame must use the inactive tab state.");
    }

    private static void AssertArmoryItemRootButton(ArmoryCatalogItemView itemTemplate)
    {
        Assert.NotNull(itemTemplate, "Armory ItemView must exist.");

        Button rootButton = itemTemplate.GetComponent<Button>();
        Assert.NotNull(rootButton, "Armory ItemView must be its own Button.");
        Assert.AreEqual(rootButton, itemTemplate.SelectionButton, "Armory ItemView selectionButton must reference the root Button.");
        Assert.AreEqual(itemTemplate.transform, itemTemplate.SelectionButton.transform, "Armory ItemView must not use a child selection hotspot.");
        Assert.Null(itemTemplate.GetComponent<WarlineCaptureShellRouteButtonView>(), "Armory ItemView root Button must not navigate away from Armory.");

        Image image = itemTemplate.GetComponent<Image>();
        Assert.NotNull(image, "Armory ItemView root Button must keep a raycast Image.");
        Assert.IsTrue(image.raycastTarget, "Armory ItemView root Image must be raycastable.");
    }

    private static void AssertArmoryItemSelectionFrame(ArmoryCatalogItemView itemTemplate)
    {
        Assert.NotNull(itemTemplate, "Armory ItemView must exist.");

        Image frame = itemTemplate.transform.Find("Frame")?.GetComponent<Image>();
        Assert.NotNull(frame, "Armory ItemView/Frame must keep the roster-card frame Image.");

        var serializedItem = new SerializedObject(itemTemplate);
        Assert.AreEqual(frame, serializedItem.FindProperty("frameImage").objectReferenceValue,
            "Armory ItemView must serialize ItemView/Frame as its selected-state frame image.");

        Sprite defaultFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ArmoryRosterCardDefaultFramePath);
        Sprite selectedFrame = AssetDatabase.LoadAssetAtPath<Sprite>(ArmoryRosterCardSelectedFramePath);
        Assert.NotNull(defaultFrame, ArmoryRosterCardDefaultFramePath);
        Assert.NotNull(selectedFrame, ArmoryRosterCardSelectedFramePath);
        Assert.AreEqual(defaultFrame, serializedItem.FindProperty("defaultFrameSprite").objectReferenceValue,
            "Armory ItemView default frame must use the final roster-card default sprite.");
        Assert.AreEqual(selectedFrame, serializedItem.FindProperty("selectedFrameSprite").objectReferenceValue,
            "Armory ItemView selected frame must use the final roster-card selected sprite.");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(itemTemplate.gameObject);
        try
        {
            ArmoryCatalogItemView item = instance.GetComponent<ArmoryCatalogItemView>();
            item.SetSelected(false);
            Assert.AreEqual(defaultFrame, item.FrameImage.sprite);

            item.SetSelected(true);
            Assert.AreEqual(selectedFrame, item.FrameImage.sprite);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void AssertArmoryInspectionPanelReferences(ArmoryInspectionPanelView inspectionPanel)
    {
        var serializedInspection = new SerializedObject(inspectionPanel);
        SerializedProperty categoryVisuals = serializedInspection.FindProperty("categoryVisuals");
        Assert.NotNull(categoryVisuals);
        Assert.AreEqual(4, categoryVisuals.arraySize);
        for (int i = 0; i < categoryVisuals.arraySize; i++)
        {
            SerializedProperty visual = categoryVisuals.GetArrayElementAtIndex(i);
            Assert.NotNull(visual.FindPropertyRelative("backgroundRoot").objectReferenceValue);
            Assert.NotNull(visual.FindPropertyRelative("artImage").objectReferenceValue);
        }
    }

    private static void AssertArmoryRuntimeUiCodeDoesNotUseHierarchyStringLookup()
    {
        string[] runtimeUiFiles =
        {
            "Assets/Game/Scripts/UI/Screens/ArmoryContentListView.cs",
            "Assets/Game/Scripts/UI/Screens/ArmoryCatalogItemView.cs",
            "Assets/Game/Scripts/UI/Screens/ArmoryInspectionPanelView.cs",
            "Assets/Game/Scripts/UI/Screens/ArmoryRightContentView.cs",
            "Assets/Game/Scripts/UI/Screens/ArmoryCategoryNavigationView.cs",
            "Assets/Game/Scripts/UI/Screens/MainMenuNavigationView.cs"
        };

        foreach (string runtimeUiFile in runtimeUiFiles)
        {
            string text = File.ReadAllText(runtimeUiFile);
            StringAssert.DoesNotContain("transform.Find", text, runtimeUiFile);
            StringAssert.DoesNotContain(".Find(\"", text, runtimeUiFile);
            StringAssert.DoesNotContain("GetComponentInChildren", text, runtimeUiFile);
        }
    }

    private static void AssertNavigationTabsAssigned(SerializedProperty tabs, int expectedSize, string label)
    {
        Assert.NotNull(tabs, $"{label} navigation must expose serialized tabs.");
        Assert.AreEqual(expectedSize, tabs.arraySize, $"{label} navigation tab count.");
        for (int i = 0; i < tabs.arraySize; i++)
        {
            SerializedProperty tab = tabs.GetArrayElementAtIndex(i);
            Assert.NotNull(tab.FindPropertyRelative("button").objectReferenceValue, $"{label} tab {i} button.");
            Assert.NotNull(tab.FindPropertyRelative("frame").objectReferenceValue, $"{label} tab {i} frame.");
        }
    }

    private static void AssertCommandTabsAssigned(MatchOverlayCommandTabGroupView tabGroup, Transform commandRailFrame)
    {
        Assert.AreEqual(8, tabGroup.Tabs.Length, "Match HUD command rail tab count.");
        Assert.AreEqual(-1, tabGroup.DefaultSelectedIndex, "Match HUD command rail must start with no selected tab.");

        for (int i = 0; i < tabGroup.Tabs.Length; i++)
        {
            MatchOverlayCommandTabView tab = tabGroup.Tabs[i];
            Assert.NotNull(tab, $"Match HUD command tab {i}.");
            Assert.NotNull(tab.Button, $"Match HUD command tab {i} button.");
            Assert.NotNull(tab.FrameImage, $"Match HUD command tab {i} frame image.");
            Assert.NotNull(tab.NormalFrameSprite, $"Match HUD command tab {i} normal sprite.");
            Assert.NotNull(tab.SelectedFrameSprite, $"Match HUD command tab {i} selected sprite.");
        }

        for (int i = 0; i < commandRailFrame.childCount; i++)
        {
            Transform child = commandRailFrame.GetChild(i);
            Assert.NotNull(child.GetComponent<Button>(), $"{child.name} must be a direct Button tab.");
        }
    }

    private static int FindCommandTabIndex(MatchOverlayCommandTabGroupView tabGroup, Button button)
    {
        Assert.NotNull(tabGroup);
        Assert.NotNull(button);

        for (int i = 0; i < tabGroup.Tabs.Length; i++)
        {
            if (tabGroup.Tabs[i]?.Button == button)
                return i;
        }

        Assert.Fail($"No command tab is assigned to button {button.name}.");
        return -1;
    }

    private static void AssignCommandTabGroups(BattleHudRuntimeFeedbackView view, params MatchOverlayCommandTabGroupView[] tabGroups)
    {
        SerializedObject serializedView = new(view);
        SerializedProperty groups = serializedView.FindProperty("commandTabGroups");
        groups.arraySize = tabGroups.Length;
        for (int i = 0; i < tabGroups.Length; i++)
            groups.GetArrayElementAtIndex(i).objectReferenceValue = tabGroups[i];

        serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssertCommandTabSelected(MatchOverlayCommandTabGroupView tabGroup, int selectedIndex)
    {
        Assert.NotNull(tabGroup);
        for (int i = 0; i < tabGroup.Tabs.Length; i++)
        {
            MatchOverlayCommandTabView tab = tabGroup.Tabs[i];
            Sprite expected = i == selectedIndex ? tab.SelectedFrameSprite : tab.NormalFrameSprite;
            Assert.AreEqual(expected, tab.FrameImage.sprite, $"Match HUD command tab {i} selected state.");
        }
    }

    private static void AssertNoCommandTabSelected(MatchOverlayCommandTabGroupView tabGroup)
    {
        Assert.NotNull(tabGroup);
        for (int i = 0; i < tabGroup.Tabs.Length; i++)
        {
            MatchOverlayCommandTabView tab = tabGroup.Tabs[i];
            Assert.AreEqual(tab.NormalFrameSprite, tab.FrameImage.sprite, $"Match HUD command tab {i} cleared state.");
        }
    }

    private static void AssertArmoryCategoryHotspot(
        GameObject root,
        World world,
        SystemHandle categorySystem,
        EntityManager entityManager,
        Entity boundary,
        string path,
        ArmoryCatalogCategory expectedCategory)
    {
        Transform hotspot = root.transform.Find(path);
        Assert.NotNull(hotspot, $"{path} must exist on Armory content.");

        Button button = hotspot.GetComponent<Button>();
        Assert.NotNull(button, $"{path} must have a Button.");

        WarlineCaptureShellRouteButtonView routeButton = hotspot.GetComponent<WarlineCaptureShellRouteButtonView>();
        if (routeButton != null)
            Assert.IsFalse(routeButton.enabled, $"{path} must not submit shell route requests.");

        button.onClick.Invoke();
        categorySystem.Update(world.Unmanaged);

        UiShellArmoryCategoryComponent categoryState =
            entityManager.GetComponentData<UiShellArmoryCategoryComponent>(boundary);
        Assert.AreEqual(expectedCategory, categoryState.Category, path);
        AssertArmoryRuntimeSelectedState(root, expectedCategory);

        DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
            entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
        Assert.AreEqual(0, routeRequests.Length, $"{path} must not navigate away from Armory.");
    }

    private static void AssertArmoryRuntimeSelectedState(GameObject root, ArmoryCatalogCategory selectedCategory)
    {
        AssertArmoryRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Characters", selectedCategory == ArmoryCatalogCategory.Characters);
        AssertArmoryRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Vehicles", selectedCategory == ArmoryCatalogCategory.Vehicles);
        AssertArmoryRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Aircrafts", selectedCategory == ArmoryCatalogCategory.Aircrafts);
        AssertArmoryRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Buildings", selectedCategory == ArmoryCatalogCategory.Buildings);
        AssertArmoryRuntimeTabState(root, "LeftContent/LeftNavPanel/Nav_Support", selectedCategory == ArmoryCatalogCategory.Support);
    }

    private static void AssertArmoryRuntimeTabState(GameObject root, string path, bool selected)
    {
        Image frame = root.transform.Find(path)?.Find("Frame")?.GetComponent<Image>();
        Assert.NotNull(frame, $"{path}/Frame must exist.");
        string spriteName = frame.sprite != null ? frame.sprite.name.ToLowerInvariant() : string.Empty;
        if (selected)
            StringAssert.Contains("selected", spriteName, $"{path} must show selected state.");
        else
            StringAssert.Contains("inactive", spriteName, $"{path} must show inactive state.");
    }

    private static void InvokeArmoryWireItemSelection(
        ArmoryContentListView listView,
        ArmoryCatalogItemView item,
        ArmoryCatalogItem model)
    {
        MethodInfo wireItemSelection = typeof(ArmoryContentListView).GetMethod(
            "WireItemSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(wireItemSelection);
        wireItemSelection.Invoke(listView, new object[] { item, model });
    }

    private static void AssertArmoryItemBackgroundState(GameObject item, string selectedBackground, Sprite expectedSprite)
    {
        AssertArmoryItemBackground(item, "Background_Character", selectedBackground, expectedSprite);
        AssertArmoryItemBackground(item, "Background_Vehicle", selectedBackground, expectedSprite);
        AssertArmoryItemBackground(item, "Background_Aircraft", selectedBackground, expectedSprite);
        AssertArmoryItemBackground(item, "Background_Building", selectedBackground, expectedSprite);
    }

    private static void AssertArmoryItemBackground(
        GameObject item,
        string backgroundName,
        string selectedBackground,
        Sprite expectedSprite)
    {
        Transform background = item.transform.Find(backgroundName);
        Assert.NotNull(background, $"{backgroundName} must exist on the Armory item template.");

        bool selected = backgroundName == selectedBackground;
        Assert.AreEqual(selected, background.gameObject.activeSelf, $"{backgroundName} active state mismatch.");

        Image art = background.Find("Art")?.GetComponent<Image>();
        Assert.NotNull(art, $"{backgroundName}/Art must exist.");
        if (selected)
        {
            Assert.AreEqual(expectedSprite, art.sprite, $"{backgroundName}/Art must receive the config portrait sprite.");
            Assert.IsTrue(art.enabled, $"{backgroundName}/Art must be enabled when the model has a portrait.");
            Assert.IsTrue(art.preserveAspect, $"{backgroundName}/Art must preserve portrait aspect.");
        }
    }

    private static Sprite CreateTestSprite(Color color, out Texture2D texture)
    {
        texture = new Texture2D(2, 2);
        texture.SetPixel(0, 0, color);
        texture.SetPixel(1, 0, color);
        texture.SetPixel(0, 1, color);
        texture.SetPixel(1, 1, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private static void InvokeArmoryNavigationOnEnable(ArmoryCategoryNavigationView navigationView)
    {
        MethodInfo onEnable = typeof(ArmoryCategoryNavigationView).GetMethod(
            "OnEnable",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(onEnable);
        onEnable.Invoke(navigationView, null);
    }

    private static void InvokeMainMenuNavigationOnEnable(MainMenuNavigationView navigationView)
    {
        MethodInfo onEnable = typeof(MainMenuNavigationView).GetMethod(
            "OnEnable",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(onEnable);
        onEnable.Invoke(navigationView, null);
    }

    private static bool IsInteractiveRaycastGraphic(GameObject root, Graphic graphic)
    {
        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            if (selectable.targetGraphic == graphic)
                return true;
        }

        foreach (ScrollRect scrollRect in root.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scrollRect.GetComponent<Graphic>() == graphic)
                return true;

            if (scrollRect.viewport != null && scrollRect.viewport.GetComponent<Graphic>() == graphic)
                return true;
        }

        return string.Equals(graphic.name, "Scrim", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = $"{transform.name}/{path}";
        }

        return path;
    }

    private static void AssertImageSpritePath(Transform root, string path, string expectedSpritePath)
    {
        Transform target = string.IsNullOrEmpty(path) ? root : root.Find(path);
        Assert.NotNull(target, path);

        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        Assert.NotNull(image.sprite, path);
        Assert.AreEqual(expectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void AssertImageDoesNotUseSpritePath(Transform root, string path, string rejectedSpritePath)
    {
        Transform target = root.Find(path);
        Assert.NotNull(target, path);

        Image image = target.GetComponent<Image>();
        Assert.NotNull(image, path);
        if (image.sprite != null)
            Assert.AreNotEqual(rejectedSpritePath, AssetDatabase.GetAssetPath(image.sprite), path);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, fieldName);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method, methodName);
        method.Invoke(target, Array.Empty<object>());
    }
}
