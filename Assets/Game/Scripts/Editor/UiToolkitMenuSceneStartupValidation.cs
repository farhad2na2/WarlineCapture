using System;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class UiToolkitMenuSceneStartupValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string RuntimeUiConfigPath = "Assets/Game/Data/UI/RuntimeUiConfig.asset";
    private const string PanelSettingsPath = "Assets/Game/UI Toolkit/RuntimePanelSettings.asset";
    private const string ShellUxmlPath = "Assets/Game/UI Toolkit/UIShellAppCanvas/UIShellAppCanvas.uxml";
    private const string LoadingUxmlPath = "Assets/Game/UI Toolkit/SCN01_LoadingContent/SCN01_LoadingContent.uxml";
    private const string MainMenuUxmlPath = "Assets/Game/UI Toolkit/SCN02_MainMenuContent/SCN02_MainMenuContent.uxml";
    private const string MatchHudUxmlPath = "Assets/Game/UI Toolkit/SCN08_MatchHudContent/SCN08_MatchHudContent.uxml";
    private const string ArmoryUxmlPath = "Assets/Game/UI Toolkit/SCN19_ArmoryContent/SCN19_ArmoryContent.uxml";
    private const string BuildDrawerUxmlPath = "Assets/Game/UI Toolkit/SCN09_BuildDrawerPopup/SCN09_BuildDrawerPopup.uxml";
    private const string BuildPlacementConfirmationBarUxmlPath = "Assets/Game/UI Toolkit/SCN08_BuildPlacementConfirmationBar/SCN08_BuildPlacementConfirmationBar.uxml";
    private const string CommanderProfileUxmlPath = "Assets/Game/UI Toolkit/SCN03_CommanderProfileContent/SCN03_CommanderProfileContent.uxml";
    private const string MissionResultPopupUxmlPath = "Assets/Game/UI Toolkit/POP05_MissionResultPopup/POP05_MissionResultPopup.uxml";
    private const string SettingsPopupUxmlPath = "Assets/Game/UI Toolkit/POP06_SettingsPopup/POP06_SettingsPopup.uxml";
    private const string InboxPopupUxmlPath = "Assets/Game/UI Toolkit/POP07_InboxPopup/POP07_InboxPopup.uxml";
    private const string RuntimeThemePath = "Assets/Game/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";
    private const string ScreenshotPath = "/private/tmp/warline-uitoolkit-menu-startup.png";

    private static int screenshotFrameCount;
    private static int screenshotCaptureRequestedFrame;
    private static double screenshotValidationStartedAt;
    private static bool screenshotValidationCompleted;
    private static bool screenshotCaptureRequested;
    private static bool screenshotValidationShouldExitEditor;
    private static int deployValidationFrameCount;
    private static int deployValidationSubmitFrame;
    private static double deployValidationStartedAt;
    private static bool deployValidationCompleted;
    private static bool deployValidationSubmitted;
    private static bool deployValidationShouldExitEditor;
    private static int selectCommandValidationFrameCount;
    private static int selectCommandDeployFrame;
    private static int selectCommandClickFrame;
    private static double selectCommandValidationStartedAt;
    private static bool selectCommandValidationCompleted;
    private static bool selectCommandDeploySubmitted;
    private static bool selectCommandClicked;
    private static bool selectCommandValidationShouldExitEditor;

    [MenuItem("Game/UI Toolkit/Repair Menu Scene Wiring")]
    public static void RepairMenuSceneWiring()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        MenuBootstrapView bootstrap = FindSceneObject<MenuBootstrapView>();
        if (bootstrap == null)
            throw new InvalidOperationException("Menu scene is missing MenuBootstrapView.");

        RuntimeUiConfig runtimeConfig = LoadRequired<RuntimeUiConfig>(RuntimeUiConfigPath);
        SetRuntimeUiMode(runtimeConfig, RuntimeUiMode.UiToolkit);
        PanelSettings panelSettings = LoadOrCreatePanelSettings();

        VisualTreeAsset shellAsset = LoadRequired<VisualTreeAsset>(ShellUxmlPath);
        VisualTreeAsset loadingAsset = LoadRequired<VisualTreeAsset>(LoadingUxmlPath);
        VisualTreeAsset mainMenuAsset = LoadRequired<VisualTreeAsset>(MainMenuUxmlPath);
        VisualTreeAsset matchHudAsset = LoadRequired<VisualTreeAsset>(MatchHudUxmlPath);
        VisualTreeAsset armoryAsset = LoadRequired<VisualTreeAsset>(ArmoryUxmlPath);
        VisualTreeAsset buildDrawerAsset = LoadRequired<VisualTreeAsset>(BuildDrawerUxmlPath);
        VisualTreeAsset buildPlacementAsset = LoadRequired<VisualTreeAsset>(BuildPlacementConfirmationBarUxmlPath);
        VisualTreeAsset commanderAsset = LoadRequired<VisualTreeAsset>(CommanderProfileUxmlPath);
        VisualTreeAsset missionResultAsset = LoadRequired<VisualTreeAsset>(MissionResultPopupUxmlPath);
        VisualTreeAsset settingsAsset = LoadRequired<VisualTreeAsset>(SettingsPopupUxmlPath);
        VisualTreeAsset inboxAsset = LoadRequired<VisualTreeAsset>(InboxPopupUxmlPath);

        GameObject shellRoot = EnsureChild(bootstrap.transform, "UiToolkitShellRoot");
        UIDocument document = shellRoot.GetComponent<UIDocument>();
        if (document == null)
            document = shellRoot.AddComponent<UIDocument>();
        UiToolkitShellView shellView = shellRoot.GetComponent<UiToolkitShellView>();
        if (shellView == null)
            shellView = shellRoot.AddComponent<UiToolkitShellView>();

        document.visualTreeAsset = shellAsset;
        document.panelSettings = panelSettings;
        shellView.Configure(
            document,
            shellAsset,
            loadingAsset,
            mainMenuAsset,
            matchHudAsset,
            armoryAsset,
            buildDrawerAsset,
            buildPlacementAsset,
            commanderAsset,
            missionResultAsset,
            settingsAsset,
            inboxAsset);
        bootstrap.Configure(
            bootstrap.UiCamera,
            bootstrap.UiCanvas,
            bootstrap.ShellView,
            bootstrap.ShellEcsPresentation,
            bootstrap.ContentSystem,
            bootstrap.Router,
            runtimeConfig,
            document,
            shellRoot,
            shellView);

        EditorUtility.SetDirty(runtimeConfig);
        EditorUtility.SetDirty(panelSettings);
        EditorUtility.SetDirty(document);
        EditorUtility.SetDirty(shellView);
        EditorUtility.SetDirty(bootstrap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[UiToolkitMenuSceneStartupValidation] Menu scene UI Toolkit wiring repaired.");
    }

    public static void Run()
    {
        try
        {
            RepairMenuSceneWiring();
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            MenuBootstrapView bootstrap = FindSceneObject<MenuBootstrapView>();
            if (bootstrap == null)
                throw new InvalidOperationException("Menu scene is missing MenuBootstrapView after wiring repair.");

            if (bootstrap.RuntimeUiConfig == null || bootstrap.RuntimeUiConfig.Mode != RuntimeUiMode.UiToolkit)
                throw new InvalidOperationException("Menu scene does not boot in UI Toolkit mode.");
            if (bootstrap.UiToolkitDocument == null)
                throw new InvalidOperationException("Menu scene has no UIDocument assigned to the bootstrap.");
            if (bootstrap.UiToolkitDocument.panelSettings == null)
                throw new InvalidOperationException("Menu scene UIDocument has no PanelSettings assigned.");
            if (bootstrap.UiToolkitShellRoot == null)
                throw new InvalidOperationException("Menu scene has no UI Toolkit shell root assigned to the bootstrap.");
            if (bootstrap.UiToolkitShellView == null)
                throw new InvalidOperationException("Menu scene has no UI Toolkit shell view assigned to the bootstrap.");

            bootstrap.ApplyRuntimeUiMode();
            UiToolkitShellView shellView = bootstrap.UiToolkitShellView;
            if (!shellView.IsMounted && !shellView.Mount())
                throw new InvalidOperationException("UI Toolkit shell did not mount.");
            if (!shellView.HasMountedMainMenuScreen)
                throw new InvalidOperationException("UI Toolkit Main Menu content did not mount.");
            if (!shellView.EnsureMainMenuVisible(UIRoute.MainMenu))
                throw new InvalidOperationException("UI Toolkit Main Menu content could not be made visible.");
            if (shellView.MainMenuScreenSlot.ClassListContains("shell-hidden"))
                throw new InvalidOperationException("UI Toolkit Main Menu screen slot is hidden after startup.");
            if (!shellView.MainMenuScreenSlot.ClassListContains(UiToolkitShellView.GetMotionStateClass(UiToolkitShellMotionState.Visible)))
                throw new InvalidOperationException("UI Toolkit Main Menu screen slot is not in visible motion state after startup.");
            if (!shellView.HasRequiredRegions)
                throw new InvalidOperationException("UI Toolkit shell is missing required regions.");
            if (!shellView.HasRequiredScreenSlots)
                throw new InvalidOperationException("UI Toolkit shell is missing required screen slots.");

            Debug.Log($"[UiToolkitMenuSceneStartupValidation] result=Passed scene={scene.path} root={shellView.Root.name} mainMenu={shellView.MainMenuContentRoot.name}");
            ExitIfBatchMode(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] result=Failed\n{exception}");
            ExitIfBatchMode(1);
        }
    }

    public static void RunPlayModeScreenshot()
    {
        try
        {
            RepairMenuSceneWiring();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            if (File.Exists(ScreenshotPath))
                File.Delete(ScreenshotPath);

            screenshotFrameCount = 0;
            screenshotCaptureRequestedFrame = 0;
            screenshotValidationStartedAt = EditorApplication.timeSinceStartup;
            screenshotValidationCompleted = false;
            screenshotCaptureRequested = false;
            screenshotValidationShouldExitEditor = true;
            EditorApplication.update -= ContinuePlayModeScreenshotValidation;
            EditorApplication.update += ContinuePlayModeScreenshotValidation;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] screenshotResult=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    public static void RunDeployCommandValidation()
    {
        try
        {
            RepairMenuSceneWiring();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            deployValidationFrameCount = 0;
            deployValidationSubmitFrame = 0;
            deployValidationStartedAt = EditorApplication.timeSinceStartup;
            deployValidationCompleted = false;
            deployValidationSubmitted = false;
            deployValidationShouldExitEditor = true;
            EditorApplication.update -= ContinueDeployCommandValidation;
            EditorApplication.update += ContinueDeployCommandValidation;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] deployResult=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    public static void RunMatchHudSelectCommandValidation()
    {
        try
        {
            RepairMenuSceneWiring();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            selectCommandValidationFrameCount = 0;
            selectCommandDeployFrame = 0;
            selectCommandClickFrame = 0;
            selectCommandValidationStartedAt = EditorApplication.timeSinceStartup;
            selectCommandValidationCompleted = false;
            selectCommandDeploySubmitted = false;
            selectCommandClicked = false;
            selectCommandValidationShouldExitEditor = true;
            EditorApplication.update -= ContinueMatchHudSelectCommandValidation;
            EditorApplication.update += ContinueMatchHudSelectCommandValidation;
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] selectCommandResult=Failed\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    private static void ContinueDeployCommandValidation()
    {
        if (deployValidationCompleted)
            return;

        try
        {
            double elapsed = EditorApplication.timeSinceStartup - deployValidationStartedAt;
            if (elapsed > 120d)
            {
                CompleteDeployValidation(false, $"Timed out waiting for Deploy to enter Match HUD. {DescribeDeployRuntimeState()}");
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            deployValidationFrameCount++;
            if (deployValidationFrameCount < 45)
                return;

            MenuBootstrapView bootstrap = FindSceneObject<MenuBootstrapView>();
            if (bootstrap == null)
            {
                CompleteDeployValidation(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            UiToolkitShellView shellView = bootstrap.UiToolkitShellView;
            if (shellView == null)
            {
                CompleteDeployValidation(false, "Menu scene has no UI Toolkit shell view in PlayMode.");
                return;
            }

            if (!shellView.IsMounted && !shellView.Mount())
            {
                CompleteDeployValidation(false, "UI Toolkit shell failed to mount in PlayMode.");
                return;
            }

            if (!deployValidationSubmitted && !shellView.EnsureMainMenuVisible(UIRoute.MainMenu))
            {
                CompleteDeployValidation(false, "UI Toolkit Main Menu failed to become visible in PlayMode.");
                return;
            }

            if (!deployValidationSubmitted && !shellView.HasRequiredMainMenuBindings)
            {
                CompleteDeployValidation(false, $"UI Toolkit Main Menu is missing runtime bindings. {DescribeMenuRenderState(shellView)}");
                return;
            }

            if (!deployValidationSubmitted)
            {
                Button deployButton = shellView.MainMenuContentRoot?.Q<Button>("DeployOperationButton");
                if (deployButton == null)
                {
                    CompleteDeployValidation(false, "DeployOperationButton was not found in the mounted Main Menu.");
                    return;
                }

                if (!IsPickedByPanel(deployButton, out string pickedElement))
                {
                    CompleteDeployValidation(false, $"DeployOperationButton is blocked by another UI Toolkit element. picked={pickedElement}");
                    return;
                }

                using ClickEvent clickEvent = ClickEvent.GetPooled();
                clickEvent.target = deployButton;
                deployButton.SendEvent(clickEvent);
                deployValidationSubmitted = true;
                deployValidationSubmitFrame = deployValidationFrameCount;
                Debug.Log("[UiToolkitMenuSceneStartupValidation] deployActionSubmitted=ClickEvent target=DeployOperationButton");
                return;
            }

            if (IsMatchHudActive())
            {
                Camera uiCamera = bootstrap.UiCamera;
                if (uiCamera != null && uiCamera.enabled)
                {
                    CompleteDeployValidation(false, $"Match HUD is active but the legacy UI camera is still enabled. {DescribeDeployRuntimeState()}");
                    return;
                }

                if (uiCamera != null && uiCamera.clearFlags != CameraClearFlags.Depth)
                {
                    CompleteDeployValidation(false, $"Match HUD is active but the legacy UI camera is still clearing {uiCamera.clearFlags}. {DescribeDeployRuntimeState()}");
                    return;
                }

                CompleteDeployValidation(true, $"Deploy loaded Match scene and entered Match HUD. {DescribeDeployRuntimeState()}");
                return;
            }

            if (deployValidationFrameCount - deployValidationSubmitFrame < 12)
                return;

            if (UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState) &&
                shellState.ActiveRoute != UIRoute.Match)
            {
                CompleteDeployValidation(false, $"Deploy click did not route to Match. {DescribeDeployRuntimeState()}");
                return;
            }

            if (TryReadSceneLifecycleState(out SceneLifecycleStateComponent lifecycleState) &&
                lifecycleState.Status == SceneLifecycleStatusKind.Failed)
            {
                CompleteDeployValidation(false, $"Deploy Match scene load failed. {DescribeDeployRuntimeState()}");
            }
        }
        catch (Exception exception)
        {
            CompleteDeployValidation(false, exception.ToString());
        }
    }

    private static void ContinueMatchHudSelectCommandValidation()
    {
        if (selectCommandValidationCompleted)
            return;

        try
        {
            double elapsed = EditorApplication.timeSinceStartup - selectCommandValidationStartedAt;
            if (elapsed > 120d)
            {
                CompleteSelectCommandValidation(false, $"Timed out waiting for Select command validation. {DescribeDeployRuntimeState()}");
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            selectCommandValidationFrameCount++;
            if (selectCommandValidationFrameCount < 45)
                return;

            MenuBootstrapView bootstrap = FindSceneObject<MenuBootstrapView>();
            if (bootstrap == null)
            {
                CompleteSelectCommandValidation(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            UiToolkitShellView shellView = bootstrap.UiToolkitShellView;
            if (shellView == null)
            {
                CompleteSelectCommandValidation(false, "Menu scene has no UI Toolkit shell view in PlayMode.");
                return;
            }

            if (!shellView.IsMounted && !shellView.Mount())
            {
                CompleteSelectCommandValidation(false, "UI Toolkit shell failed to mount in PlayMode.");
                return;
            }

            if (!selectCommandDeploySubmitted)
            {
                if (!shellView.EnsureMainMenuVisible(UIRoute.MainMenu))
                {
                    CompleteSelectCommandValidation(false, "UI Toolkit Main Menu failed to become visible in PlayMode.");
                    return;
                }

                Button deployButton = shellView.MainMenuContentRoot?.Q<Button>("DeployOperationButton");
                if (deployButton == null)
                {
                    CompleteSelectCommandValidation(false, "DeployOperationButton was not found before Select validation.");
                    return;
                }

                if (!IsPickedByPanel(deployButton, out string pickedElement))
                {
                    CompleteSelectCommandValidation(false, $"DeployOperationButton is blocked before Select validation. picked={pickedElement}");
                    return;
                }

                using ClickEvent clickEvent = ClickEvent.GetPooled();
                clickEvent.target = deployButton;
                deployButton.SendEvent(clickEvent);
                selectCommandDeploySubmitted = true;
                selectCommandDeployFrame = selectCommandValidationFrameCount;
                Debug.Log("[UiToolkitMenuSceneStartupValidation] selectCommandDeploySubmitted=ClickEvent target=DeployOperationButton");
                return;
            }

            if (!IsMatchHudActive())
                return;

            if (selectCommandValidationFrameCount - selectCommandDeployFrame < 12)
                return;

            if (!selectCommandClicked)
            {
                Button selectButton = shellView.MatchHudContentRoot?.Q<Button>("SelectCommand");
                if (selectButton == null)
                {
                    CompleteSelectCommandValidation(false, "SelectCommand was not found in the mounted Match HUD.");
                    return;
                }

                if (!IsPickedByPanel(selectButton, out string pickedElement))
                {
                    CompleteSelectCommandValidation(false, $"SelectCommand is blocked by another UI Toolkit element. picked={pickedElement}");
                    return;
                }

                using ClickEvent clickEvent = ClickEvent.GetPooled();
                clickEvent.target = selectButton;
                selectButton.SendEvent(clickEvent);
                selectCommandClicked = true;
                selectCommandClickFrame = selectCommandValidationFrameCount;
                Debug.Log("[UiToolkitMenuSceneStartupValidation] selectCommandSubmitted=ClickEvent target=SelectCommand");
                return;
            }

            if (selectCommandValidationFrameCount - selectCommandClickFrame < 12)
                return;

            if (!TryReadSelectionInputState(out RtsSelectionInputStateComponent inputState))
            {
                CompleteSelectCommandValidation(false, "Selection input state was not available after SelectCommand click.");
                return;
            }

            TacticalCommandMode activeMode = (TacticalCommandMode)inputState.ActiveCommandMode;
            if (activeMode != TacticalCommandMode.Select)
            {
                CompleteSelectCommandValidation(false, $"SelectCommand did not set active command mode. activeMode={activeMode}");
                return;
            }

            Button activeSelectButton = shellView.MatchHudContentRoot?.Q<Button>("SelectCommand");
            if (activeSelectButton == null || !activeSelectButton.ClassListContains("command-button-selected"))
            {
                CompleteSelectCommandValidation(false, "SelectCommand active mode was set but the UI Toolkit selected class was not applied.");
                return;
            }

            CompleteSelectCommandValidation(true, "SelectCommand entered selection mode and applied selected visual state.");
        }
        catch (Exception exception)
        {
            CompleteSelectCommandValidation(false, exception.ToString());
        }
    }

    private static void ContinuePlayModeScreenshotValidation()
    {
        if (screenshotValidationCompleted)
            return;

        try
        {
            double elapsed = EditorApplication.timeSinceStartup - screenshotValidationStartedAt;
            if (elapsed > 45d)
            {
                CompleteScreenshotValidation(false, "Timed out before a non-black Menu screenshot could be captured.");
                return;
            }

            if (!EditorApplication.isPlaying)
                return;

            screenshotFrameCount++;
            if (screenshotFrameCount < 45)
                return;

            MenuBootstrapView bootstrap = FindSceneObject<MenuBootstrapView>();
            if (bootstrap == null)
            {
                CompleteScreenshotValidation(false, "Menu scene is missing MenuBootstrapView in PlayMode.");
                return;
            }

            bootstrap.ApplyRuntimeUiMode();
            UiToolkitShellView shellView = bootstrap.UiToolkitShellView;
            if (shellView == null)
            {
                CompleteScreenshotValidation(false, "Menu scene has no UI Toolkit shell view in PlayMode.");
                return;
            }

            if (!shellView.IsMounted && !shellView.Mount())
            {
                CompleteScreenshotValidation(false, "UI Toolkit shell failed to mount in PlayMode.");
                return;
            }

            if (!shellView.EnsureMainMenuVisible(UIRoute.MainMenu))
            {
                CompleteScreenshotValidation(false, "UI Toolkit Main Menu failed to become visible in PlayMode.");
                return;
            }

            if (!screenshotCaptureRequested)
            {
                ScreenCapture.CaptureScreenshot(ScreenshotPath);
                screenshotCaptureRequested = true;
                screenshotCaptureRequestedFrame = screenshotFrameCount;
                return;
            }

            if (screenshotFrameCount - screenshotCaptureRequestedFrame < 12)
                return;

            if (!File.Exists(ScreenshotPath))
                return;

            Texture2D screenshot = new(2, 2, TextureFormat.RGBA32, false);
            if (!screenshot.LoadImage(File.ReadAllBytes(ScreenshotPath)))
            {
                UnityEngine.Object.DestroyImmediate(screenshot);
                CompleteScreenshotValidation(false, $"Captured Menu screenshot could not be read. path={ScreenshotPath}");
                return;
            }

            float luma = EstimateAverageLuma(screenshot);
            UnityEngine.Object.DestroyImmediate(screenshot);

            if (luma < 0.05f)
            {
                CompleteScreenshotValidation(
                    false,
                    $"Captured Menu screenshot is still black or near-black. luma={luma:0.000} path={ScreenshotPath} {DescribeMenuRenderState(shellView)}");
                return;
            }

            CompleteScreenshotValidation(
                true,
                $"Captured non-black Menu screenshot. luma={luma:0.000} path={ScreenshotPath} {DescribeMenuRenderState(shellView)}");
        }
        catch (Exception exception)
        {
            CompleteScreenshotValidation(false, exception.ToString());
        }
    }

    private static string DescribeMenuRenderState(UiToolkitShellView shellView)
    {
        if (shellView == null)
            return "shellView=null";

        return
            "state={" +
            DescribeElement("root", shellView.Root) + "; " +
            DescribeElement("safe", shellView.SafeAreaRoot) + "; " +
            DescribeElement("content", shellView.ContentRoot) + "; " +
            DescribeElement("mainSlot", shellView.MainMenuScreenSlot) + "; " +
            DescribeElement("mainRoot", shellView.MainMenuContentRoot) +
            "}";
    }

    private static string DescribeElement(string name, VisualElement element)
    {
        if (element == null)
            return $"{name}=null";

        Rect worldBound = element.worldBound;
        IResolvedStyle style = element.resolvedStyle;
        return
            $"{name}[children={element.childCount},hidden={element.ClassListContains("shell-hidden")}," +
            $"display={style.display},visibility={style.visibility},opacity={style.opacity:0.00}," +
            $"wb=({worldBound.x:0},{worldBound.y:0},{worldBound.width:0},{worldBound.height:0})]";
    }

    private static bool IsPickedByPanel(VisualElement target, out string pickedElement)
    {
        pickedElement = "none";
        if (target == null || target.panel == null)
            return false;

        Rect worldBound = target.worldBound;
        Vector2 center = worldBound.center;
        VisualElement picked = target.panel.Pick(center);
        pickedElement = DescribePickedElement(picked);
        while (picked != null)
        {
            if (picked == target)
                return true;
            picked = picked.parent;
        }

        return false;
    }

    private static string DescribePickedElement(VisualElement element)
    {
        if (element == null)
            return "null";

        string typeName = element.GetType().Name;
        string name = string.IsNullOrEmpty(element.name) ? "(unnamed)" : element.name;
        return $"{typeName}:{name}";
    }

    private static float EstimateAverageLuma(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        if (pixels == null || pixels.Length == 0)
            return 0f;

        long total = 0;
        int count = 0;
        int stride = Math.Max(1, pixels.Length / 20000);
        for (int index = 0; index < pixels.Length; index += stride)
        {
            Color32 pixel = pixels[index];
            total += (pixel.r * 299) + (pixel.g * 587) + (pixel.b * 114);
            count++;
        }

        return count == 0 ? 0f : total / (count * 255000f);
    }

    private static bool IsMatchSceneLoaded()
    {
        Scene matchScene = SceneManager.GetSceneByName(SceneLifecycleSystem.MatchSceneName);
        if (matchScene.IsValid() && matchScene.isLoaded)
            return true;

        return TryReadSceneLifecycleState(out SceneLifecycleStateComponent state) &&
            state.IsMatchLoaded != 0;
    }

    private static bool IsMatchHudActive()
    {
        return IsMatchSceneLoaded() &&
            UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState) &&
            shellState.CurrentMode == UiShellMode.MatchHud &&
            shellState.ActiveRoute == UIRoute.Match;
    }

    private static bool TryReadSceneLifecycleState(out SceneLifecycleStateComponent lifecycleState)
    {
        lifecycleState = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SceneLifecycleBoundaryComponent>(),
            ComponentType.ReadOnly<SceneLifecycleStateComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        lifecycleState = entityManager.GetComponentData<SceneLifecycleStateComponent>(entity);
        return true;
    }

    private static bool TryReadSelectionInputState(out RtsSelectionInputStateComponent inputState)
    {
        inputState = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RtsSelectionInputStateComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity entity = query.GetSingletonEntity();
        inputState = entityManager.GetComponentData<RtsSelectionInputStateComponent>(entity);
        return true;
    }

    private static string DescribeDeployRuntimeState()
    {
        string shell = UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel shellState)
            ? $"shell(mode={shellState.CurrentMode},route={shellState.ActiveRoute},phase={shellState.Phase},transition={shellState.IsTransitionRunning},seq={shellState.TransitionSequenceId})"
            : "shell=unavailable";
        string loading = UiShellRuntimeGateway.TryReadLoadingProgress(out UiShellLoadingProgressModel loadingState)
            ? $"loading(progress={loadingState.Progress01:0.00},complete={loadingState.IsComplete},status={loadingState.Status})"
            : "loading=unavailable";
        string lifecycle = TryReadSceneLifecycleState(out SceneLifecycleStateComponent lifecycleState)
            ? $"scene(status={lifecycleState.Status},busy={lifecycleState.IsBusy},loaded={lifecycleState.IsMatchLoaded},progress={lifecycleState.Progress01:0.00})"
            : "scene=unavailable";
        Scene matchScene = SceneManager.GetSceneByName(SceneLifecycleSystem.MatchSceneName);
        string unityScene = $"unityScene(valid={matchScene.IsValid()},loaded={matchScene.isLoaded})";
        return $"{shell}; {loading}; {lifecycle}; {unityScene}";
    }

    private static void CompleteScreenshotValidation(bool success, string message)
    {
        screenshotValidationCompleted = true;
        EditorApplication.update -= ContinuePlayModeScreenshotValidation;
        if (success)
            Debug.Log($"[UiToolkitMenuSceneStartupValidation] screenshotResult=Passed {message}");
        else
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] screenshotResult=Failed {message}");

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();

        if (Application.isBatchMode || screenshotValidationShouldExitEditor)
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }

    private static void CompleteDeployValidation(bool success, string message)
    {
        deployValidationCompleted = true;
        EditorApplication.update -= ContinueDeployCommandValidation;
        if (success)
            Debug.Log($"[UiToolkitMenuSceneStartupValidation] deployResult=Passed {message}");
        else
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] deployResult=Failed {message}");

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();

        if (Application.isBatchMode || deployValidationShouldExitEditor)
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }

    private static void CompleteSelectCommandValidation(bool success, string message)
    {
        selectCommandValidationCompleted = true;
        EditorApplication.update -= ContinueMatchHudSelectCommandValidation;
        if (success)
            Debug.Log($"[UiToolkitMenuSceneStartupValidation] selectCommandResult=Passed {message}");
        else
            Debug.LogError($"[UiToolkitMenuSceneStartupValidation] selectCommandResult=Failed {message}");

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();

        if (Application.isBatchMode || selectCommandValidationShouldExitEditor)
            EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Missing asset: {path}");
        return asset;
    }

    private static T FindSceneObject<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }

    private static GameObject EnsureChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing.gameObject;

        GameObject child = new(childName);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetRuntimeUiMode(RuntimeUiConfig runtimeConfig, RuntimeUiMode mode)
    {
        SerializedObject serializedObject = new(runtimeConfig);
        SerializedProperty modeProperty = serializedObject.FindProperty("mode");
        if (modeProperty == null)
            throw new InvalidOperationException("RuntimeUiConfig is missing serialized mode field.");
        modeProperty.enumValueIndex = (int)mode;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static PanelSettings LoadOrCreatePanelSettings()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings == null)
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
        }

        ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RuntimeThemePath);
        SerializedObject serializedObject = new(panelSettings);
        SetSerializedObject(serializedObject, "m_ThemeStyleSheet", theme);
        SetSerializedInt(serializedObject, "m_ScaleMode", 1);
        SetSerializedVector2(serializedObject, "m_ReferenceResolution", new Vector2(1920f, 1080f));
        SetSerializedFloat(serializedObject, "m_Match", 0.5f);
        SetSerializedBool(serializedObject, "m_ClearDepthStencil", true);
        SetSerializedBool(serializedObject, "m_VertexBudgetAutoAdjust", true);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return panelSettings;
    }

    private static void SetSerializedObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetSerializedInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.intValue = value;
    }

    private static void SetSerializedFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void SetSerializedBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetSerializedVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
            return;

        if (property.propertyType == SerializedPropertyType.Vector2Int)
            property.vector2IntValue = Vector2Int.RoundToInt(value);
        else if (property.propertyType == SerializedPropertyType.Vector2)
            property.vector2Value = value;
    }

    private static void ExitIfBatchMode(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
