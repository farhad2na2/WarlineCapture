using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;

public sealed class OperationsDashboardScreenTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(MenuSceneRoutesOperationsWithoutReplacingSharedHeader), test => test.MenuSceneRoutesOperationsWithoutReplacingSharedHeader(), ref passed);
            Run(nameof(MainMenuOperationsCardAndBackUseShellHistory), test => test.MainMenuOperationsCardAndBackUseShellHistory(), ref passed);
            Run(nameof(OperationsPrefabUsesProductionArtAndHonestActionStates), test => test.OperationsPrefabUsesProductionArtAndHonestActionStates(), ref passed);
            Run(nameof(RaidAndEndDayButtonsMountSharedV3Popups), test => test.RaidAndEndDayButtonsMountSharedV3Popups(), ref passed);
            Run(nameof(TargetLocksAreStoredUnderVisualLockLayered), test => test.TargetLocksAreStoredUnderVisualLockLayered(), ref passed);
            Debug.Log($"[OperationsDashboardScreenValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[OperationsDashboardScreenValidation] result=Failed passed={passed}\n{exception}");
            EditorApplication.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        if (_world != null && _world.IsCreated)
            _world.Dispose();
        World.DefaultGameObjectInjectionWorld = _previousWorld;
        _world = null;
        _previousWorld = null;
    }

    [Test]
    public void MenuSceneRoutesOperationsWithoutReplacingSharedHeader()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.OperationsContentPrefab, "Menu scene must assign the SCN-11 Operations Dashboard prefab.");
        Assert.AreEqual("SCN11_OperationsDashboardContent", content.OperationsContentPrefab.name);
        Assert.NotNull(content.OperationsContentPrefab.GetComponentInChildren<OperationsDashboardScreenView>(true));

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);

        content.InstallMenuRouteBody(UIRoute.Operations);

        GameObject headerAfter = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter, "Operations must preserve the shared Main Menu header instance.");
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.LeftRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.MiddleRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.RightRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.FooterRegion);
        GameObject dashboard = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.NotNull(dashboard.GetComponentInChildren<OperationsDashboardScreenView>(true));
    }

    [Test]
    public void MainMenuOperationsCardAndBackUseShellHistory()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("OperationsDashboardRouteFlowTests");
        World.DefaultGameObjectInjectionWorld = _world;
        _world.CreateSystem<UiShellStateSystem>();
        EntityManager em = _world.EntityManager;
        using EntityQuery boundaryQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadWrite<UiShellStateComponent>(),
            ComponentType.ReadWrite<UiShellRouteHistoryComponent>(),
            ComponentType.ReadWrite<UiShellRouteRequestComponent>(),
            ComponentType.ReadWrite<UiShellPresentationCommandComponent>(),
            ComponentType.ReadWrite<UiShellTransitionCompleteComponent>());
        Entity boundary = boundaryQuery.GetSingletonEntity();
        em.SetComponentData(boundary, new UiShellStateComponent
        {
            CurrentMode = UiShellMode.MainMenu,
            ActiveRoute = UIRoute.MainMenu,
            Phase = UiShellTransitionPhase.MenuReady,
            TransitionSequenceId = 0,
            IsTransitionRunning = 0
        });

        SystemHandle flowSystem = _world.CreateSystem<UiShellFlowSystem>();
        GameObject menuInstance = UnityEngine.Object.Instantiate(content.MainMenuContentPrefab);
        GameObject operationsInstance = UnityEngine.Object.Instantiate(content.OperationsContentPrefab);
        try
        {
            List<UIShellRouteButtonView> menuRoutes = CollectComponentsInHierarchy<UIShellRouteButtonView>(menuInstance.transform);
            UIShellRouteButtonView operationsRoute = ResolveRoute(menuRoutes, UiShellRouteIntent.OpenMenuRoute, UIRoute.Operations);
            Assert.NotNull(operationsRoute, "The Main Menu Operations card must submit the Operations route.");
            Assert.IsTrue(operationsRoute.PushHistory);
            RebindRouteButton(operationsRoute);
            operationsRoute.GetComponent<Button>().onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            DynamicBuffer<UiShellRouteHistoryComponent> history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
            Assert.AreEqual(UIRoute.Operations, shellState.ActiveRoute);
            Assert.AreEqual(1, history.Length);
            Assert.AreEqual(UIRoute.MainMenu, history[0].Route);

            DynamicBuffer<UiShellTransitionCompleteComponent> completions = em.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
            completions.Add(new UiShellTransitionCompleteComponent
            {
                Kind = UiShellCommandKind.SwapMenuMiddle,
                Region = UiShellRegionId.MiddleRegion,
                SequenceId = shellState.TransitionSequenceId
            });
            flowSystem.Update(_world.Unmanaged);

            OperationsDashboardScreenView operations = operationsInstance.GetComponentInChildren<OperationsDashboardScreenView>(true);
            Assert.NotNull(operations);
            RebindRouteButton(operations.BackRouteButton);
            operations.BackRouteButton.GetComponent<Button>().onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            Assert.AreEqual(UIRoute.MainMenu, shellState.ActiveRoute);
            Assert.AreEqual(0, history.Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuInstance);
            UnityEngine.Object.DestroyImmediate(operationsInstance);
        }
    }

    [Test]
    public void OperationsPrefabUsesProductionArtAndHonestActionStates()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        OperationsDashboardScreenView view = content.OperationsContentPrefab.GetComponentInChildren<OperationsDashboardScreenView>(true);
        Assert.NotNull(view);
        Assert.IsNull(
            content.OperationsContentPrefab.transform.Find("BodyScrim"),
            "Operations content must remain transparent and use the shared shell background.");
        Assert.NotNull(view.ReadinessRail);
        Assert.NotNull(view.DistrictMap);
        Assert.NotNull(view.DailyBriefing);
        Assert.NotNull(view.ActiveWarnings);
        Assert.NotNull(view.CommandBar);
        Assert.AreEqual(5, view.ReadinessCards.Length);
        Assert.AreEqual(5, view.DistrictButtons.Length);
        Assert.AreEqual(3, view.WarningButtons.Length);
        AssertAllAssigned(view.ReadinessCards, "readiness row");
        AssertAllAssigned(view.DistrictButtons, "district button");
        AssertAllAssigned(view.WarningButtons, "warning button");

        Assert.AreEqual(
            "Assets/Game/Art/UI/V3Shared/CampaignScenes/SCN05_SahrinMissionMap_V3.png",
            AssetDatabase.GetAssetPath(view.DistrictMapImage.texture));
        Assert.AreEqual(
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset",
            AssetDatabase.GetAssetPath(view.ScreenTitle.font));
        Assert.GreaterOrEqual(view.ScreenTitle.fontSize, 50f);
        Assert.GreaterOrEqual(view.DayLabel.fontSize, 24f);

        Assert.IsTrue(view.IntelReportButton.interactable);
        Assert.IsTrue(view.PatrolButton.interactable);
        Assert.IsTrue(view.ArmoryButton.interactable);
        Assert.IsTrue(view.RaidButton.interactable);
        Assert.IsTrue(view.RepairButton.interactable);
        Assert.IsTrue(view.EndDayButton.interactable);
        AssertAllInteractable(view.DistrictButtons, "district detail hotspot");
        AssertAllInteractable(view.WarningButtons, "warning detail hotspot");

        AssertRoute(view.IntelReportButton, UIRoute.CommandFeed);
        AssertRoute(view.PatrolButton, UIRoute.DistrictDetail);
        AssertRoute(view.RepairButton, UIRoute.DistrictDetail);
        AssertRoute(view.ArmoryButton, UIRoute.Armory);
        Assert.NotNull(view.ConfirmRaidPopupPrefab);
        Assert.NotNull(view.ConfirmRaidPopupPrefab.GetComponent<ConfirmRaidV3PopupView>());
        Assert.NotNull(view.EndOfDayReportPopupPrefab);
        Assert.NotNull(view.EndOfDayReportPopupPrefab.GetComponent<EndOfDayReportPopupView>());

        MainMenuV3SectionLayoutView responsive = content.OperationsContentPrefab.GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
        Assert.NotNull(responsive);
        Assert.IsTrue(responsive.ExpandToCanvasWidth);
        Assert.AreEqual(new Vector2(1672f, 941f), responsive.ReferenceResolution);
        Assert.AreEqual(6, responsive.RightAnchoredTargets.Length);
        OperationsDashboardMapResponsiveView mapResponsive = content.OperationsContentPrefab.GetComponentInChildren<OperationsDashboardMapResponsiveView>(true);
        Assert.NotNull(mapResponsive);
        Assert.AreEqual(5, mapResponsive.DistrictZones.Length);
        Assert.AreEqual(5, mapResponsive.DistrictMarkers.Length);
    }

    [Test]
    public void TargetLocksAreStoredUnderVisualLockLayered()
    {
        AssertTargetLockExists("Design/VisualLockLayered/SCN-05_CampaignOperations/reference/SCN-05_CampaignOperations_CommandBase_TargetLock_V01.png");
        AssertTargetLockExists("Design/VisualLockLayered/SCN-06_MissionBriefing/reference/SCN-06_MissionBriefing_CommandBase_TargetLock_V01.png");
        AssertTargetLockExists("Design/VisualLockLayered/SCN-11_OperationsDashboard/reference/SCN-11_OperationsDashboardV3_Final_Target.png");
    }

    [Test]
    public void RaidAndEndDayButtonsMountSharedV3Popups()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        AssertModalMounts<ConfirmRaidV3PopupView>(content.OperationsContentPrefab, view => view.RaidButton);
        AssertModalMounts<EndOfDayReportPopupView>(content.OperationsContentPrefab, view => view.EndDayButton);
    }

    private static void AssertModalMounts<T>(
        GameObject operationsPrefab,
        Func<OperationsDashboardScreenView, Button> resolveButton)
        where T : Component
    {
        GameObject canvasObject = new GameObject("OperationsActionTestCanvas", typeof(RectTransform), typeof(Canvas));
        GameObject instance = null;
        try
        {
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            instance = UnityEngine.Object.Instantiate(operationsPrefab, canvasObject.transform, false);
            OperationsDashboardScreenView view = instance.GetComponentInChildren<OperationsDashboardScreenView>(true);
            Assert.NotNull(view);
            view.RefreshBindings();
            Button button = resolveButton(view);
            Assert.NotNull(button);
            button.onClick.Invoke();

            T modal = canvasObject.GetComponentInChildren<T>(true);
            Assert.NotNull(modal, $"{button.name} must mount the shared {typeof(T).Name} prefab.");
            Assert.IsTrue(modal.gameObject.activeSelf);
        }
        finally
        {
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }
    }

    private static void AssertRoute(Button button, UIRoute route)
    {
        UIShellRouteButtonView routeButton = button.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(routeButton);
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, routeButton.Intent);
        Assert.AreEqual(route, routeButton.Route);
        Assert.IsTrue(routeButton.PushHistory);
    }

    private static void AssertTargetLockExists(string path)
    {
        Assert.IsTrue(File.Exists(path), $"Missing visual target lock: {path}");
    }

    private static void RebindRouteButton(UIShellRouteButtonView routeButton)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo onDisable = typeof(UIShellRouteButtonView).GetMethod("OnDisable", flags);
        MethodInfo onEnable = typeof(UIShellRouteButtonView).GetMethod("OnEnable", flags);
        Assert.NotNull(onDisable);
        Assert.NotNull(onEnable);
        onDisable.Invoke(routeButton, null);
        onEnable.Invoke(routeButton, null);
    }

    private static void Run(string name, Action<OperationsDashboardScreenTests> action, ref int passed)
    {
        var test = new OperationsDashboardScreenTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[OperationsDashboardScreenValidation] passed={name}");
        }
        finally
        {
            test.TearDown();
        }
    }

    private static GameObject AssertRegionHasChild(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region));
        Assert.NotNull(region.ContentRoot);
        Assert.Greater(region.ContentRoot.childCount, 0, $"{regionId} must contain routed content.");
        return region.ContentRoot.GetChild(0).gameObject;
    }

    private static void AssertRegionIsEmpty(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region));
        Assert.NotNull(region.ContentRoot);
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} must be empty while SCN-11 owns the body overlay.");
    }

    private static void AssertAllAssigned<T>(T[] values, string label) where T : UnityEngine.Object
    {
        Assert.NotNull(values);
        for (int i = 0; i < values.Length; i++)
            Assert.NotNull(values[i], $"Missing {label} at index {i}.");
    }

    private static void AssertAllInteractable(Button[] buttons, string message)
    {
        for (int i = 0; i < buttons.Length; i++)
            Assert.IsTrue(buttons[i].interactable, $"{message}; index={i}");
    }

    private static UIShellRouteButtonView ResolveRoute(
        List<UIShellRouteButtonView> routes,
        UiShellRouteIntent intent,
        UIRoute route)
    {
        for (int i = 0; i < routes.Count; i++)
        {
            if (routes[i].Intent == intent && routes[i].Route == route)
                return routes[i];
        }
        return null;
    }

    private static List<T> CollectComponentsInHierarchy<T>(Transform root) where T : Component
    {
        var components = new List<T>();
        CollectComponentsInHierarchy(root, components);
        return components;
    }

    private static void CollectComponentsInHierarchy<T>(Transform root, List<T> components) where T : Component
    {
        if (root == null)
            return;
        T component = root.GetComponent<T>();
        if (component != null)
            components.Add(component);
        for (int i = 0; i < root.childCount; i++)
            CollectComponentsInHierarchy(root.GetChild(i), components);
    }

    private static T ResolveComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = ResolveComponentInHierarchy<T>(root.transform);
            if (result != null)
                return result;
        }
        return null;
    }

    private static T ResolveComponentInHierarchy<T>(Transform root) where T : Component
    {
        if (root == null)
            return null;
        T component = root.GetComponent<T>();
        if (component != null)
            return component;
        for (int i = 0; i < root.childCount; i++)
        {
            component = ResolveComponentInHierarchy<T>(root.GetChild(i));
            if (component != null)
                return component;
        }
        return null;
    }
}
