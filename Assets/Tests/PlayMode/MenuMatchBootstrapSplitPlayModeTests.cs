#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Threading.Tasks;
using Game.Scripts.UI;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuMatchBootstrapSplitPlayModeTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string MenuSceneName = "Menu";
    private const string MatchSceneName = "Match";
    private const int MatchRuntimeContentMaxFrames = 1800;

    [TearDown]
    public void TearDown()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        InitialUnitsRuntimeState.SelectionModeActive = false;
        InitialUnitsRuntimeState.BuildModeActive = false;
        InitialUnitsRuntimeState.ZoomInHeld = false;
        InitialUnitsRuntimeState.ZoomOutHeld = false;
        InitialUnitsRuntimeState.SuppressNextWorldClick = false;
        InitialUnitsRuntimeState.FullscreenMapOpen = false;
        InitialUnitsRuntimeState.FullscreenMapIsoMode = false;
        InitialUnitsRuntimeState.PlayerAutoModeEnabled = false;
        new ActiveMissionSession().Clear();
        GameRuntimeStats.Reset();
        Time.timeScale = 1f;
        SetLogAssertIgnoreFailingMessages(false);
    }

    [Test]
    public async Task FooterDeployButton_LoadsMatchAdditivelyAndStartsGameplay()
    {
        Scene menuScene = await LoadMenuScene();
        await StartMatchFromMenu(menuScene);
    }

    [Test]
    public async Task MenuDiagnosticsFpsPanel_TogglesRuntimeLogWithCloseButton()
    {
        Scene menuScene = await LoadMenuScene();
        MenuDiagnosticsView diagnostics = FindSceneComponent<MenuDiagnosticsView>(menuScene);
        Assert.NotNull(diagnostics, "Menu scene must keep the FPS/log diagnostics surface on the persistent Menu canvas.");
        Assert.NotNull(diagnostics.FpsButton, "Menu diagnostics FPS panel must expose a button.");
        Assert.NotNull(diagnostics.FpsText, "Menu diagnostics FPS panel must expose the FPS label.");
        Assert.NotNull(diagnostics.LogPanel, "Menu diagnostics must expose the on-screen log panel.");
        Assert.NotNull(diagnostics.LogText, "Menu diagnostics must expose the on-screen log text.");
        Assert.NotNull(diagnostics.CloseButton, "Menu diagnostics log panel must expose a close button.");
        Assert.IsFalse(diagnostics.LogPanel.activeSelf, "On-screen log must start hidden.");

        Debug.Log("[MenuDiagnosticsTest] runtime log surface smoke");
        diagnostics.FpsButton.onClick.Invoke();
        await NextFrame();

        Assert.IsTrue(diagnostics.LogPanel.activeSelf, "Clicking the FPS panel must show the on-screen log.");
        StringAssert.Contains("[MenuDiagnosticsTest] runtime log surface smoke", diagnostics.LogText.text);

        diagnostics.CloseButton.onClick.Invoke();
        await NextFrame();
        Assert.IsFalse(diagnostics.LogPanel.activeSelf, "Clicking the on-screen log close button must hide the panel.");
    }

    [Test]
    public async Task FooterDeployButton_ShowsLoadingBeforeMatchSceneLoadStarts()
    {
        Scene menuScene = await LoadMenuScene();
        WarlineCaptureShellRouteButtonView deployButton = null;
        await WaitFor(
            () =>
            {
                deployButton = FindFooterDeployButton(menuScene);
                return deployButton != null && deployButton.gameObject.activeInHierarchy;
            },
            240,
            "FooterContent/DeployCommandButton did not become available from Menu.unity.");

        deployButton.GetComponent<Button>().onClick.Invoke();

        await WaitFor(
            () => TryReadUiShellState(out UiShellStateComponent state) &&
                  state.ActiveRoute == WarlineCaptureRoute.Match &&
                  state.CurrentMode == UiShellMode.Loading &&
                  state.IsTransitionRunning == 0 &&
                  LoadingLayerVisiblyCoversMenu(menuScene) &&
                  PersistentMenuUiCameraCanRenderLoading(menuScene) &&
                  !IsSceneLoaded(MatchSceneName),
            120,
            "Footer Deploy must visibly cover the menu with an active UI camera before the expensive Match scene load starts.");

        await WaitFor(
            () => IsSceneLoaded(MatchSceneName),
            600,
            "Footer Deploy showed loading but did not start the deferred Match scene load.");
    }

    [Test]
    public async Task ResultContinueButton_UnloadsMatchAndKeepsMenuAlive()
    {
        Scene menuScene = await LoadMenuScene();
        await StartMatchFromMenu(menuScene);
        await ReturnToMenuFromMatchResult(menuScene);
    }

    [Test]
    public async Task FooterDeployAndContinue_CanRepeatThreeLoadUnloadCycles()
    {
        Scene menuScene = await LoadMenuScene();
        for (int cycle = 0; cycle < 3; cycle++)
        {
            await StartMatchFromMenu(menuScene);
            await ReturnToMenuFromMatchResult(menuScene);
        }
    }

    private static async Task ReturnToMenuFromMatchResult(Scene menuScene)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity boundary = GetUiShellBoundary(entityManager);
        DynamicBuffer<UiShellPopupRequestComponent> popupRequests = entityManager.GetBuffer<UiShellPopupRequestComponent>(boundary);
        popupRequests.Add(new UiShellPopupRequestComponent
        {
            PopupKind = UiShellPopupKind.MissionResult,
            Intent = UiShellPopupIntent.Show,
            PayloadId = 0
        });

        WarlineCaptureShellResultConfirmButtonView continueButton = null;
        await WaitFor(
            () =>
            {
                continueButton = FindResultContinueButton(menuScene);
                return continueButton != null && continueButton.gameObject.activeInHierarchy;
            },
            240,
            "Mission result ContinueButton did not become available from the persistent Menu shell.");

        continueButton.GetComponent<Button>().onClick.Invoke();

        await WaitFor(
            () => !IsSceneLoaded(MatchSceneName),
            600,
            "Pressing the mission result ContinueButton did not unload Match.unity.");

        Assert.IsTrue(menuScene.IsValid() && menuScene.isLoaded, "Menu scene must remain loaded after returning from Match.");
        Assert.NotNull(FindSceneComponent<MenuBootstrapView>(menuScene), "MenuBootstrapView must remain alive after Match unload.");

        await WaitFor(
            () => TryReadUiShellState(out UiShellStateComponent state) &&
                state.CurrentMode == UiShellMode.MainMenu &&
                state.IsTransitionRunning == 0,
            600,
            "Menu shell did not return to an idle MainMenu mode after Match unload.");
    }

    private static async Task<Scene> LoadMenuScene()
    {
        InitialUnitsRuntimeState.PlayRequested = false;
        new ActiveMissionSession().Clear();
        IgnoreFailingPackageLogsForNographicsRunner();

        Scene loadedMenuScene = EditorSceneManager.LoadSceneInPlayMode(
            MenuScenePath,
            new LoadSceneParameters(LoadSceneMode.Single));
        await NextFrame();
        await NextFrame();

        Scene menuScene = SceneManager.GetSceneByName(MenuSceneName);
        Assert.IsTrue(menuScene.IsValid(), "Menu scene should be loaded by the PlayMode smoke.");
        Assert.IsTrue(menuScene.isLoaded, "Menu scene should remain loaded while Match loads additively.");
        Assert.AreEqual(loadedMenuScene.path, menuScene.path);

        MenuBootstrapView menuBootstrap = FindSceneComponent<MenuBootstrapView>(menuScene);
        Assert.NotNull(menuBootstrap, "Menu scene must contain MenuBootstrapView for persistent shell startup.");
        menuBootstrap.Configure(
            menuBootstrap.UiCamera,
            menuBootstrap.UiCanvas,
            menuBootstrap.ShellView,
            menuBootstrap.ShellEcsPresentation,
            menuBootstrap.ContentSystem,
            menuBootstrap.Router);

        return menuScene;
    }

    private static async Task StartMatchFromMenu(Scene menuScene)
    {
        InitialUnitsRuntimeState.PlayRequested = false;

        WarlineCaptureShellRouteButtonView deployButton = null;
        await WaitFor(
            () =>
            {
                deployButton = FindFooterDeployButton(menuScene);
                return deployButton != null && deployButton.gameObject.activeInHierarchy;
            },
            240,
            "FooterContent/DeployCommandButton did not become available from Menu.unity.");
        Assert.AreEqual(UiShellRouteIntent.EnterMatch, deployButton.Intent);
        Assert.AreEqual(WarlineCaptureRoute.Match, deployButton.Route);
        Assert.IsTrue(deployButton.gameObject.activeInHierarchy, "Footer Deploy button must be active before the smoke invokes it.");

        deployButton.GetComponent<Button>().onClick.Invoke();

        await WaitFor(
            () => IsSceneLoaded(MatchSceneName),
            600,
            "Pressing FooterContent/DeployCommandButton did not load Match.unity additively.");

        Scene matchScene = SceneManager.GetSceneByName(MatchSceneName);
        Assert.IsTrue(menuScene.isLoaded, "Menu scene must stay loaded after Match loads additively.");
        Assert.IsTrue(matchScene.IsValid() && matchScene.isLoaded, "Match scene should be loaded additively after footer Deploy.");
        Assert.AreNotEqual(menuScene, matchScene, "Footer Deploy must not replace the persistent Menu scene.");
        MatchSceneView matchSceneView = FindSceneComponent<MatchSceneView>(matchScene);
        Assert.NotNull(matchSceneView, "Loaded Match scene must contain MatchSceneView.");
        AssertMatchWorldCameraCanRender(matchSceneView.WorldCamera);
        await WaitFor(
            HasReadyUnitPrefabRegistry,
            MatchRuntimeContentMaxFrames,
            "Footer Deploy loaded Match.unity but the Match subscene unit prefab registry did not become ready.");

        MenuBootstrapView menuBootstrap = FindSceneComponent<MenuBootstrapView>(menuScene);
        Assert.NotNull(menuBootstrap, "MenuBootstrapView must remain available while Match is loaded.");
        await WaitFor(
            () => PersistentMenuUiCannotClearMatchWorld(menuBootstrap),
            120,
            "Persistent Menu UI must switch to overlay mode while Match is loaded so it cannot clear over the world camera.");

        await WaitFor(
            () => InitialUnitsRuntimeState.PlayRequested,
            MatchRuntimeContentMaxFrames,
            "Footer Deploy loaded Match.unity but did not issue the gameplay start request.");

        await WaitFor(
            () => TryReadUiShellState(out UiShellStateComponent state) &&
                  state.ActiveRoute == WarlineCaptureRoute.Match &&
                  state.CurrentMode == UiShellMode.MatchHud &&
                  state.IsTransitionRunning == 0,
            MatchRuntimeContentMaxFrames,
            "Footer Deploy started gameplay but the persistent shell did not settle into Match HUD mode.");

        Assert.IsTrue(
            PersistentMenuUiCannotClearMatchWorld(menuBootstrap),
            "Persistent Menu UI must still be overlay-only after Match HUD transition completes.");
        AssertMatchWorldCameraCanRender(matchSceneView.WorldCamera);

        await WaitFor(
            HasSpawnedRuntimeUnits,
            MatchRuntimeContentMaxFrames,
            "Footer Deploy reached Match HUD but runtime units did not spawn.");

        await WaitFor(
            HasSpawnedRuntimeBuildings,
            MatchRuntimeContentMaxFrames,
            "Footer Deploy reached Match HUD but runtime buildings did not spawn.");
    }

    private static WarlineCaptureShellRouteButtonView FindFooterDeployButton(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            WarlineCaptureShellRouteButtonView routeButton = FindFooterDeployButton(root.transform);
            if (routeButton != null)
                return routeButton;
        }

        return null;
    }

    private static WarlineCaptureShellRouteButtonView FindFooterDeployButton(Transform root)
    {
        if (root == null)
            return null;

        WarlineCaptureShellRouteButtonView routeButton = root.GetComponent<WarlineCaptureShellRouteButtonView>();
        if (routeButton != null &&
            routeButton.name == "DeployCommandButton" &&
            routeButton.Intent == UiShellRouteIntent.EnterMatch &&
            routeButton.Route == WarlineCaptureRoute.Match &&
            GetHierarchyPath(routeButton.transform).Contains("FooterContent/DeployCommandButton", StringComparison.Ordinal))
        {
            return routeButton;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            WarlineCaptureShellRouteButtonView child = FindFooterDeployButton(root.GetChild(i));
            if (child != null)
                return child;
        }

        return null;
    }

    private static WarlineCaptureShellResultConfirmButtonView FindResultContinueButton(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            WarlineCaptureShellResultConfirmButtonView button = FindResultContinueButton(root.transform);
            if (button != null)
                return button;
        }

        return null;
    }

    private static WarlineCaptureShellResultConfirmButtonView FindResultContinueButton(Transform root)
    {
        if (root == null)
            return null;

        WarlineCaptureShellResultConfirmButtonView button = root.GetComponent<WarlineCaptureShellResultConfirmButtonView>();
        if (button != null &&
            button.name == "ContinueButton" &&
            GetHierarchyPath(button.transform).Contains("POP05_MissionResultPopup", StringComparison.Ordinal))
        {
            return button;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            WarlineCaptureShellResultConfirmButtonView child = FindResultContinueButton(root.GetChild(i));
            if (child != null)
                return child;
        }

        return null;
    }

    private static bool LoadingLayerVisiblyCoversMenu(Scene scene)
    {
        WarlineCaptureShellRegionView loadingLayer = FindShellRegion(scene, WarlineCaptureShellRegionId.LoadingLayer);
        return loadingLayer != null &&
               loadingLayer.gameObject.activeInHierarchy &&
               loadingLayer.RegionRoot != null &&
               loadingLayer.RegionRoot.localScale == Vector3.one &&
               loadingLayer.CanvasGroup != null &&
               loadingLayer.CanvasGroup.alpha >= 0.99f &&
               loadingLayer.CanvasGroup.blocksRaycasts;
    }

    private static bool PersistentMenuUiCameraCanRenderLoading(Scene scene)
    {
        MenuBootstrapView menuBootstrap = FindSceneComponent<MenuBootstrapView>(scene);
        return menuBootstrap != null &&
               menuBootstrap.UiCamera != null &&
               menuBootstrap.UiCamera.enabled &&
               menuBootstrap.UiCamera.clearFlags == CameraClearFlags.SolidColor &&
               menuBootstrap.UiCanvas != null &&
               menuBootstrap.UiCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
    }

    private static WarlineCaptureShellRegionView FindShellRegion(Scene scene, WarlineCaptureShellRegionId regionId)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            WarlineCaptureShellRegionView[] regions = root.GetComponentsInChildren<WarlineCaptureShellRegionView>(true);
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i] != null && regions[i].RegionId == regionId)
                    return regions[i];
            }
        }

        return null;
    }

    private static void IgnoreFailingPackageLogsForNographicsRunner()
    {
        if (!string.Equals(SystemInfo.graphicsDeviceType.ToString(), "Null", StringComparison.Ordinal))
            return;

        SetLogAssertIgnoreFailingMessages(true);
    }

    private static void SetLogAssertIgnoreFailingMessages(bool ignore)
    {
        Type logAssertType = Type.GetType("UnityEngine.TestTools.LogAssert, UnityEngine.TestRunner");
        logAssertType?.GetProperty("ignoreFailingMessages")?.SetValue(null, ignore);
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private static bool HasReadyUnitPrefabRegistry()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
            ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entityManager.HasBuffer<UnitPrefabRegistryEntry>(entity) &&
                entityManager.GetBuffer<UnitPrefabRegistryEntry>(entity).Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PersistentMenuUiCannotClearMatchWorld(MenuBootstrapView menuBootstrap)
    {
        if (menuBootstrap == null ||
            menuBootstrap.UiCanvas == null ||
            menuBootstrap.UiCanvas.renderMode != RenderMode.ScreenSpaceOverlay ||
            menuBootstrap.UiCanvas.worldCamera != null)
        {
            return false;
        }

        Camera uiCamera = menuBootstrap.UiCamera;
        return uiCamera == null || (!uiCamera.enabled && uiCamera.clearFlags == CameraClearFlags.Depth);
    }

    private static void AssertMatchWorldCameraCanRender(Camera worldCamera)
    {
        Assert.NotNull(worldCamera, "Loaded Match scene must expose a world camera.");
        Assert.IsTrue(worldCamera.gameObject.activeInHierarchy, "Match world camera object must be active.");
        Assert.IsTrue(worldCamera.enabled, "Match world camera must be enabled.");
        Assert.AreEqual(null, worldCamera.targetTexture, "Match world camera must render to the Game view, not a detached target texture.");
        Assert.AreNotEqual(0, worldCamera.cullingMask, "Match world camera culling mask must include renderable layers.");
        Assert.Greater(worldCamera.rect.width, 0f, "Match world camera viewport width must be visible.");
        Assert.Greater(worldCamera.rect.height, 0f, "Match world camera viewport height must be visible.");
    }

    private static bool HasSpawnedRuntimeUnits()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<Faction>());
        return query.CalculateEntityCount() > 0;
    }

    private static bool HasSpawnedRuntimeBuildings()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
        return query.CalculateEntityCount() > 0;
    }

    private static Entity GetUiShellBoundary(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
        Assert.IsFalse(query.IsEmptyIgnoreFilter, "Menu shell boundary must exist before a result popup can be shown.");
        return query.GetSingletonEntity();
    }

    private static bool TryReadUiShellState(out UiShellStateComponent state)
    {
        state = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UiShellBoundaryComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity boundary = query.GetSingletonEntity();
        if (!entityManager.HasComponent<UiShellStateComponent>(boundary))
            return false;

        state = entityManager.GetComponentData<UiShellStateComponent>(boundary);
        return true;
    }

    private static async Task WaitFor(Func<bool> predicate, int maxFrames, string failureMessage)
    {
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (predicate())
                return;

            await NextFrame();
        }

        Assert.Fail(failureMessage);
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private static async Task NextFrame()
    {
        await Task.Delay(16);
        await Task.Yield();
    }
}
#endif
