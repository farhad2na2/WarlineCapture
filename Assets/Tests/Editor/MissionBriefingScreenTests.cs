using System;
using System.Collections.Generic;
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

public sealed class MissionBriefingScreenTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(MenuSceneRoutesMissionBriefingWithoutReplacingSharedHeader), test => test.MenuSceneRoutesMissionBriefingWithoutReplacingSharedHeader(), ref passed);
            Run(nameof(CampaignMissionOpensBriefingAndDeployBoundaryIsHonest), test => test.CampaignMissionOpensBriefingAndDeployBoundaryIsHonest(), ref passed);
            Run(nameof(CampaignBriefingBackFlowPreservesNestedRouteHistory), test => test.CampaignBriefingBackFlowPreservesNestedRouteHistory(), ref passed);
            Run(nameof(MissionBriefingPrefabUsesProductionArtAndStableHierarchy), test => test.MissionBriefingPrefabUsesProductionArtAndStableHierarchy(), ref passed);
            Debug.Log($"[MissionBriefingScreenValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MissionBriefingScreenValidation] result=Failed passed={passed}\n{exception}");
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
    public void MenuSceneRoutesMissionBriefingWithoutReplacingSharedHeader()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.MissionBriefingContentPrefab, "Menu scene must assign the SCN-06 Mission Briefing prefab.");
        Assert.AreEqual("SCN06_MissionBriefingContent", content.MissionBriefingContentPrefab.name);
        Assert.NotNull(content.MissionBriefingContentPrefab.GetComponent<MissionBriefingScreenView>());

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);

        content.InstallMenuRouteBody(UIRoute.MissionBriefing);

        GameObject headerAfter = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter, "Mission Briefing must preserve the shared Main Menu header instance.");
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.LeftRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.MiddleRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.RightRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.FooterRegion);
        GameObject briefing = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        MissionBriefingScreenView installedView = briefing.GetComponent<MissionBriefingScreenView>();
        Assert.NotNull(installedView);
        Assert.NotNull(installedView.MissionOverview);
        Assert.NotNull(installedView.PrimaryObjectives);
        Assert.NotNull(installedView.EnemyIntel);
    }

    [Test]
    public void CampaignMissionOpensBriefingAndDeployBoundaryIsHonest()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        CampaignOperationsScreenView campaign = content.CampaignContentPrefab.GetComponent<CampaignOperationsScreenView>();
        Assert.NotNull(campaign);
        Assert.IsTrue(campaign.LaunchMissionButton.interactable);
        UIShellRouteButtonView briefingRoute = campaign.LaunchMissionButton.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(briefingRoute, "SCN-05 selected mission action must be a shell route button.");
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, briefingRoute.Intent);
        Assert.AreEqual(UIRoute.MissionBriefing, briefingRoute.Route);
        Assert.IsTrue(briefingRoute.PushHistory);

        MissionBriefingScreenView briefing = content.MissionBriefingContentPrefab.GetComponent<MissionBriefingScreenView>();
        Assert.NotNull(briefing);
        Assert.NotNull(briefing.BackRouteButton);
        Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, briefing.BackRouteButton.Intent);
        Assert.AreEqual(UIRoute.Campaign, briefing.BackRouteButton.Route);
        Assert.IsFalse(briefing.DeployOperationButton.interactable, "Deploy must remain disabled until Campaign startup contracts exist.");
        Assert.IsNull(ResolveComponentInHierarchy<UIGameStartButtonView>(content.MissionBriefingContentPrefab.transform),
            "SCN-06 must not invoke the default Skirmish Match startup path.");
    }

    [Test]
    public void CampaignBriefingBackFlowPreservesNestedRouteHistory()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("MissionBriefingRouteFlowTests");
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
            ActiveRoute = UIRoute.Campaign,
            Phase = UiShellTransitionPhase.MenuReady,
            TransitionSequenceId = 0,
            IsTransitionRunning = 0
        });
        DynamicBuffer<UiShellRouteHistoryComponent> history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
        history.Add(new UiShellRouteHistoryComponent { Route = UIRoute.MainMenu });

        SystemHandle flowSystem = _world.CreateSystem<UiShellFlowSystem>();
        GameObject campaignInstance = UnityEngine.Object.Instantiate(content.CampaignContentPrefab);
        GameObject briefingInstance = UnityEngine.Object.Instantiate(content.MissionBriefingContentPrefab);
        try
        {
            CampaignOperationsScreenView campaign = campaignInstance.GetComponent<CampaignOperationsScreenView>();
            UIShellRouteButtonView briefingRoute = campaign.LaunchMissionButton.GetComponent<UIShellRouteButtonView>();
            briefingRoute.SendMessage("OnEnable");
            campaign.LaunchMissionButton.onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
            Assert.AreEqual(UIRoute.MissionBriefing, shellState.ActiveRoute);
            Assert.AreEqual(2, history.Length);
            Assert.AreEqual(UIRoute.Campaign, history[1].Route);

            DynamicBuffer<UiShellTransitionCompleteComponent> completions = em.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
            completions.Add(new UiShellTransitionCompleteComponent
            {
                Kind = UiShellCommandKind.SwapMenuMiddle,
                Region = Game.UI.Contracts.UiShellRegionId.MiddleRegion,
                SequenceId = shellState.TransitionSequenceId
            });
            flowSystem.Update(_world.Unmanaged);

            MissionBriefingScreenView briefing = briefingInstance.GetComponent<MissionBriefingScreenView>();
            briefing.BackRouteButton.SendMessage("OnEnable");
            briefing.BackRouteButton.GetComponent<Button>().onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
            Assert.AreEqual(UIRoute.Campaign, shellState.ActiveRoute);
            Assert.AreEqual(1, history.Length);
            Assert.AreEqual(UIRoute.MainMenu, history[0].Route);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(campaignInstance);
            UnityEngine.Object.DestroyImmediate(briefingInstance);
        }
    }

    [Test]
    public void MissionBriefingPrefabUsesProductionArtAndStableHierarchy()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = ResolveComponentInScene<UIShellContentView>(scene);
        MissionBriefingScreenView view = content.MissionBriefingContentPrefab.GetComponent<MissionBriefingScreenView>();
        Assert.NotNull(view);
        Assert.NotNull(view.MissionOverview);
        Assert.NotNull(view.PrimaryObjectives);
        Assert.NotNull(view.TacticalConditions);
        Assert.NotNull(view.EnemyIntel);
        Assert.NotNull(view.ChapterProgress);
        Assert.NotNull(view.Rewards);
        Assert.AreEqual(5, view.ProgressNodes.Length);
        AssertAllAssigned(view.ProgressNodes, "progress node");
        Assert.AreEqual(
            "Assets/Game/Art/UI/Generated/CampaignOperations/TargetLockV01/scn05_blackout_relay_preview_v01.png",
            AssetDatabase.GetAssetPath(view.MissionArtImage.texture));
        Assert.AreEqual(
            "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset",
            AssetDatabase.GetAssetPath(view.ScreenTitle.font));
        Assert.GreaterOrEqual(view.ScreenTitle.fontSize, 110f);
        Assert.GreaterOrEqual(view.MissionTitle.fontSize, 80f);
        Assert.IsFalse(view.DeployOperationButton.interactable);
        RectTransform deployRect = view.DeployOperationButton.GetComponent<RectTransform>();
        Assert.AreEqual(0f, deployRect.anchorMin.y, 0.001f);
        Assert.AreEqual(0f, deployRect.anchorMax.y, 0.001f);
    }

    private static void Run(string name, Action<MissionBriefingScreenTests> action, ref int passed)
    {
        var test = new MissionBriefingScreenTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[MissionBriefingScreenValidation] passed={name}");
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
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} must be empty while SCN-06 owns the body overlay.");
    }

    private static void AssertAllAssigned(RectTransform[] values, string label)
    {
        Assert.NotNull(values);
        for (int i = 0; i < values.Length; i++)
            Assert.NotNull(values[i], $"Missing {label} at index {i}.");
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
