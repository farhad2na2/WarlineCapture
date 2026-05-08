using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class WarlineCaptureUiShellTests
{
    private const string GameScenePath = "Assets/Game/Scenes/Game.unity";
    private const string ShellPrefabPath = "Assets/Game/Prefabs/UI/Shell/WarlineCaptureAppCanvas.prefab";
    private const string SplashPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_Splash.prefab";
    private const string MainMenuPrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab";
    private const string SplashBackgroundPath = "Assets/Game/Art/UI/Generated/Splash/Backgrounds/Splash_Background_CityDawn.png";
    private const string SplashLoadingPanelPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_LoadingPanel_9Slice.png";
    private const string SplashProgressTrackPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_ProgressTrackMask.png";
    private const string SplashProgressFillPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_ProgressFillMask.png";
    private const string SplashBottomPanelPath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_BottomStatusPanel_9Slice.png";
    private const string SplashOuterFramePath = "Assets/Game/Art/UI/Generated/Splash/Frames/Splash_OuterFrame_Overlay.png";
    private const string SplashLogoEmblemPath = "Assets/Game/Art/UI/Brand/WarlineCapture_LionLogo_Display.png";
    private const string SplashTitleWordmarkPath = "Assets/Game/Art/UI/Generated/Splash/Titles/Splash_Title_Wordmark.png";
    private const string OxaniumFontFolder = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/";
    private static readonly string[] ScreenPrefabPaths =
    {
        "Assets/Game/Prefabs/UI/Screens/Screen_Splash.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Settings.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_QuickCustomSetup.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_SagaMap.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_MissionBriefing.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_LoadoutSquadPrep.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_CommanderProfile.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Armory.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_CommandExchange.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Inbox.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Events.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_Ranking.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_CommandFeed.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_OperationDashboard.prefab",
        "Assets/Game/Prefabs/UI/Screens/Screen_DistrictDetail.prefab"
    };

    [Test]
    public void GameScene_ParallelUiBootstrapIsEnabledAndLegacyCanvasIsKeptInactive()
    {
        SceneYamlTestUtility scene = SceneYamlTestUtility.Load(GameScenePath);
        string bootstrapBlock = scene.FindRequiredBlockContaining("m_EditorClassIdentifier: Assembly-CSharp::WarlineCaptureUiBootstrap");
        string legacyCanvasBlock = scene.FindRequiredBlockContaining("m_Name: UI_Canvas");

        StringAssert.Contains("enableParallelUiOnStart: 1", bootstrapBlock);
        StringAssert.Contains("startupMode: 1", bootstrapBlock);
        StringAssert.Contains("parallelStartupRoute: 0", bootstrapBlock);
        StringAssert.Contains("appCanvasPrefab: {fileID:", bootstrapBlock);
        StringAssert.Contains("m_IsActive: 0", legacyCanvasBlock);
    }

    [Test]
    public void ShellPrefab_KeepsScreensAsSeparatePrefabReferences()
    {
        GameObject shellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPrefabPath);
        Assert.NotNull(shellPrefab);

        Transform contentRoot = shellPrefab.transform.Find("SafeAreaRoot/ContentRoot");
        Assert.NotNull(contentRoot);
        Assert.AreEqual(0, contentRoot.GetComponentsInChildren<WarlineCaptureScreenController>(true).Length);
        RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
        Assert.AreEqual(Vector2.zero, contentRect.anchorMin);
        Assert.AreEqual(Vector2.one, contentRect.anchorMax);
        Assert.IsFalse(shellPrefab.transform.Find("SafeAreaRoot/HeaderBar").gameObject.activeSelf);
        Assert.IsFalse(shellPrefab.transform.Find("SafeAreaRoot/FooterBar").gameObject.activeSelf);

        var router = shellPrefab.GetComponent<WarlineCaptureRouter>();
        Assert.NotNull(router);
        Assert.NotNull(shellPrefab.GetComponent<WarlineCaptureModalController>());
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
            Assert.AreEqual(17, contentRoot.GetComponentsInChildren<WarlineCaptureScreenController>(true).Length);
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
            Assert.AreEqual(WarlineCaptureRoute.Splash, router.ActiveRoute);
            Assert.IsTrue(contentRoot.Find("Screen_Splash").gameObject.activeSelf);
            Assert.IsFalse(contentRoot.Find("Screen_MainMenu").gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
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
        foreach (string prefabPath in ScreenPrefabPaths)
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
        foreach (string prefabPath in ScreenPrefabPaths)
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
    public void MainMenuPrefab_ContainsPhaseOneModeCards()
    {
        GameObject mainMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
        Assert.NotNull(mainMenuPrefab);

        Assert.NotNull(mainMenuPrefab.transform.Find("TopProfileBar/LogoImage"));
        Assert.NotNull(mainMenuPrefab.transform.Find("TopProfileBar/CommanderNameText"));
        Assert.NotNull(mainMenuPrefab.transform.Find("TopProfileBar/SettingsButton"));
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_Saga"));
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_Operation"));
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_QuickCustom"));
        Assert.NotNull(mainMenuPrefab.transform.Find("BottomUtilityBar"));

        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_Saga").GetComponent<WarlineCaptureModeCardView>());
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_Operation").GetComponent<WarlineCaptureModeCardView>());
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_QuickCustom").GetComponent<WarlineCaptureModeCardView>());
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_Saga/Button").GetComponent<ScreenRouteButton>());
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_Operation/Button").GetComponent<ScreenRouteButton>());
        Assert.NotNull(mainMenuPrefab.transform.Find("ModeCardList/ModeCard_QuickCustom/Button").GetComponent<ScreenRouteButton>());
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
        Assert.IsNull(splashPrefab.GetComponentInChildren<ScreenRouteButton>(true));
        SplashScreenController splashController = splashPrefab.GetComponent<SplashScreenController>();
        Assert.NotNull(splashController);
        var splashSerialized = new SerializedObject(splashController);
        Assert.AreEqual(3f, splashSerialized.FindProperty("fakeLoadingSeconds").floatValue, 0.001f);
        Assert.AreEqual((int)WarlineCaptureRoute.MainMenu, splashSerialized.FindProperty("routeAfterFakeLoad").enumValueIndex);

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
                    splash.GetComponent<WarlineCaptureScreenController>(),
                    mainMenu.GetComponent<WarlineCaptureScreenController>(),
                    settings.GetComponent<WarlineCaptureScreenController>()
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
            router.ConfigureForTests(new[] { splash.GetComponent<WarlineCaptureScreenController>() }, WarlineCaptureRoute.Splash);

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
            WarlineCaptureModalController modal = root.AddComponent<WarlineCaptureModalController>();
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

            SplashScreenController splash = root.AddComponent<SplashScreenController>();
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
        WarlineCaptureScreenController controller = screen.AddComponent<WarlineCaptureScreenController>();
        controller.SetRouteForTests(route);
        return screen;
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
