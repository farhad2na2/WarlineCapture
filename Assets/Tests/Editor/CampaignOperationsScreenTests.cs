using System;
using System.Collections.Generic;
using NUnit.Framework;
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

public sealed class CampaignOperationsScreenTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(MenuSceneRoutesCampaignWithoutReplacingSharedHeader), test => test.MenuSceneRoutesCampaignWithoutReplacingSharedHeader(), ref passed);
            Run(nameof(MainMenuCampaignEntryAndUnavailableActionsAreHonest), test => test.MainMenuCampaignEntryAndUnavailableActionsAreHonest(), ref passed);
            Run(nameof(CampaignAndBackButtonsDriveShellRouteHistory), test => test.CampaignAndBackButtonsDriveShellRouteHistory(), ref passed);
            Run(nameof(CampaignPrefabUsesIndependentArtAndStableHierarchy), test => test.CampaignPrefabUsesIndependentArtAndStableHierarchy(), ref passed);
            Debug.Log($"[CampaignOperationsScreenValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[CampaignOperationsScreenValidation] result=Failed passed={passed}\n{exception}");
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
    public void MenuSceneRoutesCampaignWithoutReplacingSharedHeader()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.CampaignContentPrefab, "Menu scene must assign the SCN-05 Campaign prefab.");
        Assert.AreEqual("SCN05_CampaignOperationsContent", content.CampaignContentPrefab.name);
        CampaignOperationsScreenView prefabView = content.CampaignContentPrefab.GetComponent<CampaignOperationsScreenView>();
        Assert.NotNull(prefabView, "SCN-05 must expose its serialized screen contract at the prefab root.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);

        content.InstallMenuRouteBody(UIRoute.Campaign);

        GameObject headerAfter = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter, "Campaign must preserve the shared Main Menu header instance.");
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.LeftRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.MiddleRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.RightRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.FooterRegion);
        GameObject campaign = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        CampaignOperationsScreenView installedView = campaign.GetComponent<CampaignOperationsScreenView>();
        Assert.NotNull(installedView);
        Assert.NotNull(installedView.ChapterRail);
        Assert.NotNull(installedView.StrategicMap);
        Assert.NotNull(installedView.MissionBriefing);
        Assert.NotNull(installedView.LaunchMissionButton);
    }

    [Test]
    public void MainMenuCampaignEntryAndUnavailableActionsAreHonest()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        List<UIShellRouteButtonView> mainMenuRoutes = CollectComponentsInHierarchy<UIShellRouteButtonView>(content.MainMenuContentPrefab.transform);
        UIShellRouteButtonView campaignRoute = ResolveRoute(mainMenuRoutes, UiShellRouteIntent.OpenMenuRoute, UIRoute.Campaign);
        Assert.NotNull(campaignRoute, "Campaign card must expose a route hotspot.");
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, campaignRoute.Intent);
        Assert.AreEqual(UIRoute.Campaign, campaignRoute.Route);
        Assert.IsTrue(campaignRoute.PushHistory);

        CampaignOperationsScreenView campaignView = content.CampaignContentPrefab.GetComponent<CampaignOperationsScreenView>();
        Assert.NotNull(campaignView);
        UIShellRouteButtonView backRoute = campaignView.BackRouteButton;
        Assert.NotNull(backRoute);
        Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, backRoute.Intent);
        Assert.AreEqual(UIRoute.MainMenu, backRoute.Route);

        Assert.IsFalse(campaignView.StoryArchiveButton.interactable);
        Assert.IsFalse(campaignView.ChapterIntelButton.interactable);
        Assert.IsTrue(campaignView.LaunchMissionButton.interactable, "Selected Campaign missions must open their briefing screen.");
        UIShellRouteButtonView missionBriefingRoute = campaignView.LaunchMissionButton.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(missionBriefingRoute);
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, missionBriefingRoute.Intent);
        Assert.AreEqual(UIRoute.MissionBriefing, missionBriefingRoute.Route);
        Assert.IsTrue(missionBriefingRoute.PushHistory);
        Assert.IsNull(ResolveComponentInHierarchy<UIGameStartButtonView>(content.CampaignContentPrefab.transform),
            "SCN-05 must not launch the default Skirmish Match path while Campaign launch data is unavailable.");

        UIShellRouteButtonView skirmishRoute = ResolveRoute(mainMenuRoutes, UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup);
        Assert.NotNull(skirmishRoute);
        Assert.AreEqual(UIRoute.QuickCustomSetup, skirmishRoute.Route);

        int directDeployCount = 0;
        for (int i = 0; i < mainMenuRoutes.Count; i++)
        {
            if (mainMenuRoutes[i].Intent == UiShellRouteIntent.EnterMatch && mainMenuRoutes[i].Route == UIRoute.Match)
                directDeployCount++;
        }
        Assert.GreaterOrEqual(directDeployCount, 1, "General Deploy must remain a direct Match launch.");
    }

    [Test]
    public void CampaignAndBackButtonsDriveShellRouteHistory()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("CampaignOperationsRouteFlowTests");
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
        GameObject campaignInstance = UnityEngine.Object.Instantiate(content.CampaignContentPrefab);
        try
        {
            List<UIShellRouteButtonView> menuRoutes = CollectComponentsInHierarchy<UIShellRouteButtonView>(menuInstance.transform);
            UIShellRouteButtonView campaignRoute = ResolveRoute(menuRoutes, UiShellRouteIntent.OpenMenuRoute, UIRoute.Campaign);
            Assert.NotNull(campaignRoute);
            campaignRoute.SendMessage("OnEnable");
            campaignRoute.GetComponent<Button>().onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            DynamicBuffer<UiShellRouteHistoryComponent> history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
            Assert.AreEqual(UIRoute.Campaign, shellState.ActiveRoute);
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

            CampaignOperationsScreenView campaignView = campaignInstance.GetComponent<CampaignOperationsScreenView>();
            Assert.NotNull(campaignView);
            UIShellRouteButtonView backRoute = campaignView.BackRouteButton;
            Assert.NotNull(backRoute);
            backRoute.SendMessage("OnEnable");
            backRoute.GetComponent<Button>().onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            Assert.AreEqual(UIRoute.MainMenu, shellState.ActiveRoute);
            Assert.AreEqual(0, history.Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuInstance);
            UnityEngine.Object.DestroyImmediate(campaignInstance);
        }
    }

    [Test]
    public void CampaignPrefabUsesIndependentArtAndStableHierarchy()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        GameObject prefab = content.CampaignContentPrefab;
        Assert.NotNull(prefab);

        CampaignOperationsScreenView view = prefab.GetComponent<CampaignOperationsScreenView>();
        Assert.NotNull(view);
        Assert.NotNull(view.ChapterRail);
        Assert.NotNull(view.StrategicMap);
        Assert.NotNull(view.MissionBriefing);
        Assert.AreEqual(5, view.ChapterCards.Length);
        Assert.AreEqual(5, view.MissionNodes.Length);
        Assert.AreEqual(5, view.ProgressNodes.Length);
        AssertAllAssigned(view.ChapterCards, "chapter card");
        AssertAllAssigned(view.MissionNodes, "mission node");
        AssertAllAssigned(view.ProgressNodes, "progress node");

        Assert.AreEqual("Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_sahrin_district_map_v01.png", AssetDatabase.GetAssetPath(view.DistrictMapImage.texture));
        Assert.AreEqual("Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_blackout_relay_preview_v01.png", AssetDatabase.GetAssetPath(view.MissionPreviewImage.texture));
        Assert.AreEqual("Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset", AssetDatabase.GetAssetPath(view.ScreenTitle.font));
        Assert.GreaterOrEqual(view.ScreenTitle.fontSize, 110f);
        Assert.GreaterOrEqual(view.MissionName.fontSize, 80f);
        Assert.AreEqual(0f, view.ChapterRail.anchorMin.y, 0.001f);
        Assert.AreEqual(1f, view.ChapterRail.anchorMax.y, 0.001f);
        RectTransform launchRect = view.LaunchMissionButton.GetComponent<RectTransform>();
        Assert.AreEqual(0f, launchRect.anchorMin.y, 0.001f);
        Assert.AreEqual(0f, launchRect.anchorMax.y, 0.001f);
    }

    private static void Run(string name, Action<CampaignOperationsScreenTests> action, ref int passed)
    {
        var test = new CampaignOperationsScreenTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[CampaignOperationsScreenValidation] passed={name}");
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
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} must be empty while SCN-05 owns the body overlay.");
    }

    private static void AssertAllAssigned(RectTransform[] values, string label)
    {
        Assert.NotNull(values);
        for (int i = 0; i < values.Length; i++)
            Assert.NotNull(values[i], $"Missing {label} at index {i}.");
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
        var roots = new List<GameObject>();
        scene.GetRootGameObjects(roots);
        for (int i = 0; i < roots.Count; i++)
        {
            T result = ResolveComponentInHierarchy<T>(roots[i].transform);
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
