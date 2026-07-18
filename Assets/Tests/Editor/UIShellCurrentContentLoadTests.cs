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
using TMPro;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.UI.Shell.Ecs;
using Game.Composition;
using Game.Runtime;

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
                nameof(MenuSceneShellInstallsCommanderProfileRouteWithoutReplacingHeader),
                test => test.MenuSceneShellInstallsCommanderProfileRouteWithoutReplacingHeader(),
                ref passed);
            RunValidationStep(
                nameof(MainMenuCommanderRouteButtonOpensProfileAndBackReturnsToMainMenu),
                test => test.MainMenuCommanderRouteButtonOpensProfileAndBackReturnsToMainMenu(),
                ref passed);
            RunValidationStep(
                nameof(CommanderProfilePrefabBindsReadModelAndExposesOnlyAvailableActions),
                test => test.CommanderProfilePrefabBindsReadModelAndExposesOnlyAvailableActions(),
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
                nameof(InstalledMatchHudCommandControlsUseSelectionReadModelCapabilities),
                test => test.InstalledMatchHudCommandControlsUseSelectionReadModelCapabilities(),
                ref passed);
            RunValidationStep(
                nameof(InstalledMatchHudRuntimeFeedbackBindsThroughMainMenuPlayUi),
                test => test.InstalledMatchHudRuntimeFeedbackBindsThroughMainMenuPlayUi(),
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
            RunValidationStep(
                nameof(LoadingProgressGatewayQueuesRequestAndFlowAppliesIt),
                test => test.LoadingProgressGatewayQueuesRequestAndFlowAppliesIt(),
                ref passed);
            RunValidationStep(
                nameof(EnterMatchRouteAndLoadingCompletionTransitionToMatchHud),
                test => test.EnterMatchRouteAndLoadingCompletionTransitionToMatchHud(),
                ref passed);

            Debug.Log($"[UIShellCurrentContentLoadValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[UIShellCurrentContentLoadValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
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
        Assert.NotNull(bootstrap.Router, "Menu bootstrap must keep the UI router serialized for Canvas routing.");
        Assert.NotNull(bootstrap.RuntimeUiConfig, "Menu bootstrap must serialize the runtime UI config so startup uses the intended UI mode.");
        Assert.AreEqual(RuntimeUiMode.Canvas, bootstrap.RuntimeUiConfig.Mode, "Menu scene must default to Canvas runtime UI.");
        Assert.NotNull(bootstrap.Router.ContentRoot, "Serialized UI router must have a content root for initial route instantiation.");
        Assert.AreSame(
            bootstrap.ShellView.transform,
            bootstrap.Router.ContentRoot.parent,
            "Serialized UI router content root must be a shell-level route host, not a regional content root.");

        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");
        Assert.NotNull(content.ShellView, "Shell content binder must serialize the shell view.");
        Assert.NotNull(content.MainMenuContentPrefab, "Main menu content prefab must be assigned.");
        Assert.NotNull(content.CommanderProfileContentPrefab, "Commander profile content prefab must be assigned.");
        Assert.NotNull(content.ArmoryContentPrefab, "Armory content prefab must be assigned.");
        Assert.NotNull(content.MatchHudContentPrefab, "Match HUD content prefab must be assigned.");
        Assert.NotNull(content.BuildPlacementConfirmationBarPrefab, "Build placement confirmation bar prefab must be assigned.");
        BuildPlacementConfirmationBarView placementBarPrefabView =
            content.BuildPlacementConfirmationBarPrefab.GetComponent<BuildPlacementConfirmationBarView>();
        Assert.NotNull(placementBarPrefabView, "Build placement confirmation bar prefab must own BuildPlacementConfirmationBarView.");
        AssertPlacementBarSpritesAssigned(placementBarPrefabView);

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
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
            new UiShellPresentationCommandModel(UiShellCommandKind.ShowLoading, default, default, default, 0)
        });
        AssertRegionHasChild(content.ShellView, UIShellRegionId.LoadingLayer);

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchLeft = AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        Assert.NotNull(matchLeft.GetComponent<MatchHudSelectionPanelView>());
        MatchHudFooterContentView footerView = AssertMatchHudFooterView(matchFooter);
        Assert.NotNull(footerView.RuntimeFeedback);
        Assert.NotNull(footerView.CommandControls);
        Assert.NotNull(footerView.Minimap);
        Assert.NotNull(footerView.SquadTray);
        BuildPlacementConfirmationBarView placementBar =
            content.ShellView.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
        Assert.NotNull(placementBar, "Match HUD install must instantiate the build placement confirmation bar.");
        CanvasGroup placementBarCanvasGroup = placementBar.GetComponent<CanvasGroup>();
        Assert.NotNull(placementBarCanvasGroup, "Build placement confirmation bar must have a CanvasGroup visibility gate.");
        Assert.IsFalse(placementBarCanvasGroup.blocksRaycasts, "Build placement confirmation bar must start hidden and non-blocking.");

        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.MiddleRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.LoadingLayer);
    }

    [Test]
    public void MenuSceneShellInstallsCommanderProfileRouteWithoutReplacingHeader()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");
        Assert.NotNull(content.CommanderProfileContentPrefab, "Commander profile content prefab must be assigned.");
        Assert.AreEqual(
            "SCN03_CommanderProfileContent",
            content.CommanderProfileContentPrefab.name,
            "Commander profile route must use the SCN-03 Commander content prefab.");
        AssertDirectChildMissing(
            content.CommanderProfileContentPrefab.transform,
            "MenuBackgroundContent",
            "Commander content must be body-only; the shell owns the menu background.");
        AssertDirectChildMissing(
            content.CommanderProfileContentPrefab.transform,
            "HeaderContent",
            "Commander content must be body-only; the shell owns the shared menu header.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });

        GameObject headerBefore = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);

        content.InstallMenuRouteBody(UIRoute.CommandFeed);

        GameObject headerAfter = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter, "Commander route body install must preserve the shared menu header.");

        GameObject commanderLeft = AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        GameObject commanderMiddle = AssertRegionHasChild(content.ShellView, UIShellRegionId.MiddleRegion);
        GameObject commanderRight = AssertRegionHasChild(content.ShellView, UIShellRegionId.RightRegion);
        GameObject commanderFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);

        AssertChildExists(commanderLeft.transform, "OverviewTab");
        AssertChildExists(commanderLeft.transform, "StatsTab");
        AssertChildExists(commanderMiddle.transform, "CommanderIdentityPanel");
        AssertChildExists(commanderMiddle.transform, "OverviewPanel");
        AssertChildExists(commanderMiddle.transform, "AccountSnapshotPanel");
        AssertChildExists(commanderRight.transform, "RewardTrackPanel");
        AssertChildExists(commanderRight.transform, "RecentHistoryPanel");
        Assert.NotNull(FindChildRecursive(commanderRight.transform, "RewardXpBar"));
        Assert.NotNull(FindChildRecursive(commanderMiddle.transform, "RankEmblem"));
        Assert.NotNull(FindChildRecursive(commanderMiddle.transform, "LevelMedallion"));
        Assert.NotNull(FindChildRecursive(commanderLeft.transform, "LockedState"));
        AssertChildExists(commanderFooter.transform, "CommanderFooterRail");
        AssertChildExists(commanderFooter.transform, "OpenArmoryButton");
        AssertChildExists(commanderFooter.transform, "DetailButton");
        AssertChildExists(commanderFooter.transform, "ReplayButton");

        Transform backgroundScrim = FindChildRecursive(content.ShellView.transform, "CommanderBackgroundScrim");
        Assert.NotNull(backgroundScrim, "Commander route must dim the shared menu background without owning a replacement background.");
        Image scrimImage = backgroundScrim.GetComponent<Image>();
        Assert.NotNull(scrimImage);
        Assert.IsFalse(scrimImage.raycastTarget, "Commander background treatment must not intercept input.");
        Assert.Greater(scrimImage.color.a, 0.25f, "Commander background treatment must visibly separate the dashboard from the map.");

        content.InstallMenuRouteBody(UIRoute.MainMenu);
        Assert.IsNull(
            FindChildRecursive(content.ShellView.transform, "CommanderBackgroundScrim"),
            "Returning to Main Menu must remove the Commander-only background treatment.");
    }

    [Test]
    public void MainMenuCommanderRouteButtonOpensProfileAndBackReturnsToMainMenu()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");
        Assert.NotNull(content.MainMenuContentPrefab, "Main menu content prefab must be assigned.");
        Assert.NotNull(content.CommanderProfileContentPrefab, "Commander profile content prefab must be assigned.");

        Transform commanderHotspot = FindChildRecursive(content.MainMenuContentPrefab.transform, "CommanderPanelHotspot");
        Assert.NotNull(commanderHotspot, "Main menu Commander panel must expose a clickable hotspot.");
        UIShellRouteButtonView commanderRoute = commanderHotspot.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(commanderRoute, "Commander panel hotspot must submit a shell route request.");
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, commanderRoute.Intent);
        Assert.AreEqual(UIRoute.CommandFeed, commanderRoute.Route);
        Assert.IsTrue(commanderRoute.PushHistory, "Opening Commander profile must push MainMenu so Back returns there.");
        Button commanderButton = commanderHotspot.GetComponent<Button>();
        Assert.NotNull(commanderButton, "Commander panel hotspot must have an actual Button component.");
        AssertButtonHasInteractiveRect(commanderButton, "Commander panel hotspot must cover an interactive rectangle.");
        AssertButtonHasRaycastableHitTarget(commanderButton, "Commander panel hotspot must have a raycast target.");

        Transform backTransform = FindChildRecursive(content.CommanderProfileContentPrefab.transform, "BackButton");
        Assert.NotNull(backTransform, "Commander profile content must include a BackButton.");
        Button backButton = backTransform.GetComponent<Button>();
        Assert.NotNull(backButton, "Commander BackButton must have an actual Button component.");
        AssertButtonHasInteractiveRect(backButton, "Commander BackButton must cover an interactive rectangle.");
        AssertButtonHasRaycastableHitTarget(backButton, "Commander BackButton must have a raycast target.");
        UIShellRouteButtonView backRoute = backTransform.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(backRoute, "Commander BackButton must submit a shell route request.");
        Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, backRoute.Intent);
        Assert.AreEqual(UIRoute.MainMenu, backRoute.Route);
        Assert.IsFalse(backRoute.PushHistory, "Back must not push another history entry.");

        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellCommanderRouteFlowTests");
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
        Assert.IsTrue(UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandFeed, pushHistory: true));
        flowSystem.Update(_world.Unmanaged);
        UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
        DynamicBuffer<UiShellRouteHistoryComponent> history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
        Assert.AreEqual(UIRoute.CommandFeed, shellState.ActiveRoute);
        Assert.AreEqual(1, history.Length);
        Assert.AreEqual(UIRoute.MainMenu, history[0].Route);

        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            em.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(new UiShellTransitionCompleteComponent
        {
            Kind = UiShellCommandKind.SwapMenuMiddle,
            Region = UiShellRegionId.MiddleRegion,
            SequenceId = shellState.TransitionSequenceId
        });
        flowSystem.Update(_world.Unmanaged);

        Assert.IsTrue(UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.BackMenuRoute, UIRoute.MainMenu, pushHistory: false));
        flowSystem.Update(_world.Unmanaged);
        shellState = em.GetComponentData<UiShellStateComponent>(boundary);
        Assert.AreEqual(UIRoute.MainMenu, shellState.ActiveRoute);
        Assert.AreEqual(0, history.Length);
    }

    [Test]
    public void CommanderProfilePrefabBindsReadModelAndExposesOnlyAvailableActions()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        GameObject prefab = content.CommanderProfileContentPrefab;
        Assert.NotNull(prefab);

        Transform middle = FindChildRecursive(prefab.transform, "MiddleContent");
        Assert.NotNull(middle);
        CommanderProfileContentView profileView = middle.GetComponent<CommanderProfileContentView>();
        Assert.NotNull(profileView, "Commander middle section must bind the shell Commander profile read model.");
        Assert.NotNull(profileView.CommanderNameLabel);
        Assert.NotNull(profileView.CommanderSubtitleLabel);

        Transform openArmory = FindChildRecursive(prefab.transform, "OpenArmoryButton");
        Button openArmoryButton = openArmory != null ? openArmory.GetComponent<Button>() : null;
        UIShellRouteButtonView armoryRoute = openArmory != null ? openArmory.GetComponent<UIShellRouteButtonView>() : null;
        Assert.NotNull(openArmoryButton);
        Assert.IsTrue(openArmoryButton.interactable, "Open Armory is an available Commander action.");
        Assert.NotNull(armoryRoute);
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, armoryRoute.Intent);
        Assert.AreEqual(UIRoute.Armory, armoryRoute.Route);
        Assert.IsTrue(armoryRoute.PushHistory);

        AssertCommanderActionDisabled(prefab.transform, "DetailButton");
        AssertCommanderActionDisabled(prefab.transform, "ReplayButton");
        AssertCommanderActionDisabled(prefab.transform, "StatsTab");
        AssertCommanderActionDisabled(prefab.transform, "BadgesTab");
        AssertCommanderActionDisabled(prefab.transform, "HistoryTab");
        AssertCommanderActionDisabled(prefab.transform, "UpgradesTab");
        AssertCommanderProfileReadability(prefab.transform);

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        Assert.NotNull(instance);
        try
        {
            Transform instanceMiddle = FindChildRecursive(instance.transform, "MiddleContent");
            Transform instanceRight = FindChildRecursive(instance.transform, "RightContent");
            Transform instanceFooter = FindChildRecursive(instance.transform, "FooterContent");
            CommanderProfileResponsiveLayoutView middleLayout = instanceMiddle != null
                ? instanceMiddle.GetComponent<CommanderProfileResponsiveLayoutView>()
                : null;
            CommanderProfileResponsiveLayoutView footerLayout = instanceFooter != null
                ? instanceFooter.GetComponent<CommanderProfileResponsiveLayoutView>()
                : null;
            CommanderProfileResponsiveLayoutView rightLayout = instanceRight != null
                ? instanceRight.GetComponent<CommanderProfileResponsiveLayoutView>()
                : null;
            Assert.NotNull(middleLayout, "Commander middle section must adapt to the expanded logical canvas height.");
            Assert.NotNull(rightLayout, "Commander right section must compact its panels for ultrawide canvases.");
            Assert.NotNull(footerLayout, "Commander footer must adapt to the expanded logical canvas height.");

            RectTransform identity = FindChildRecursive(instance.transform, "CommanderIdentityPanel") as RectTransform;
            RectTransform account = FindChildRecursive(instance.transform, "AccountSnapshotPanel") as RectTransform;
            RectTransform history = FindChildRecursive(instance.transform, "RecentHistoryPanel") as RectTransform;
            RectTransform footerAction = FindChildRecursive(instance.transform, "OpenArmoryButton") as RectTransform;
            Assert.NotNull(identity);
            Assert.NotNull(account);
            Assert.NotNull(history);
            Assert.NotNull(footerAction);

            middleLayout.ApplyLayout(2160f);
            rightLayout.ApplyLayout(2160f);
            footerLayout.ApplyLayout(2160f);
            Assert.AreEqual(0f, identity.anchoredPosition.y, 0.01f, "20:9 must lower the centered Commander body into the visible menu grid.");
            Assert.AreEqual(540f, account.rect.height, 0.01f, "20:9 must compact the Account Snapshot panel.");
            Assert.AreEqual(900f, history.rect.height, 0.01f, "20:9 must compact Recent History before the footer.");
            Assert.AreEqual(-100f, footerAction.anchoredPosition.y, 0.01f, "20:9 footer actions must remain fully visible below compact content panels.");

            middleLayout.ApplyLayout(2700f);
            rightLayout.ApplyLayout(2700f);
            footerLayout.ApplyLayout(2700f);
            Assert.AreEqual(240f, identity.anchoredPosition.y, 0.01f, "16:9 must retain the validated raised middle layout.");
            Assert.AreEqual(700f, account.rect.height, 0.01f, "16:9 must retain the expanded Account Snapshot panel.");
            Assert.AreEqual(1020f, history.rect.height, 0.01f, "16:9 must retain the expanded Recent History panel.");
            Assert.AreEqual(150f, footerAction.anchoredPosition.y, 0.01f, "16:9 footer actions must sit in the lower integrated action rail.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void LoadingProgressGatewayQueuesRequestAndFlowAppliesIt()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellLoadingProgressRequestTests");
        World.DefaultGameObjectInjectionWorld = _world;

        _world.CreateSystem<UiShellStateSystem>();
        EntityManager em = _world.EntityManager;
        using EntityQuery boundaryQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressRequestComponent>());
        Assert.AreEqual(1, boundaryQuery.CalculateEntityCount(), "Boundary setup must create a request-capable shell boundary in OnCreate.");

        Entity boundary = boundaryQuery.GetSingletonEntity();
        DynamicBuffer<UiShellLoadingProgressRequestComponent> requests =
            em.GetBuffer<UiShellLoadingProgressRequestComponent>(boundary);
        Assert.AreEqual(0, requests.Length);

        Assert.IsTrue(UiShellRuntimeGateway.TrySetLoadingProgress(0.42f, "Streaming map", false));
        Assert.AreEqual(1, requests.Length, "Gateway must enqueue loading progress instead of writing the component directly.");

        SystemHandle flowSystem = _world.CreateSystem<UiShellFlowSystem>();
        flowSystem.Update(_world.Unmanaged);

        Assert.AreEqual(0, requests.Length, "Flow system must consume queued loading progress requests.");
        Assert.IsTrue(UiShellRuntimeGateway.TryReadLoadingProgress(out UiShellLoadingProgressModel loading));
        Assert.AreEqual(0.42f, loading.Progress01, 0.001f);
        Assert.AreEqual("Streaming map", loading.Status);
        Assert.IsFalse(loading.IsComplete);
    }

    [Test]
    public void EnterMatchRouteAndLoadingCompletionTransitionToMatchHud()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellEnterMatchRouteTests");
        World.DefaultGameObjectInjectionWorld = _world;

        _world.CreateSystem<UiShellStateSystem>();
        EntityManager em = _world.EntityManager;
        using EntityQuery boundaryQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<UiShellRootComponent>(),
            ComponentType.ReadWrite<UiShellStateComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressComponent>(),
            ComponentType.ReadWrite<UiShellLoadingProgressRequestComponent>(),
            ComponentType.ReadWrite<MatchIntroTransitionComponent>(),
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
        Assert.IsTrue(UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.EnterMatch, UIRoute.Match, pushHistory: false));
        flowSystem.Update(_world.Unmanaged);

        UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
        MatchIntroTransitionComponent matchIntro = em.GetComponentData<MatchIntroTransitionComponent>(boundary);
        Assert.AreEqual(UiShellMode.Loading, shellState.CurrentMode);
        Assert.AreEqual(UIRoute.Match, shellState.ActiveRoute);
        Assert.AreEqual(MatchIntroTransitionStateKind.WaitingForWorldReady, matchIntro.State);

        DynamicBuffer<UiShellTransitionCompleteComponent> completions =
            em.GetBuffer<UiShellTransitionCompleteComponent>(boundary);
        completions.Add(new UiShellTransitionCompleteComponent
        {
            Kind = UiShellCommandKind.ShowLoading,
            Region = UiShellRegionId.LoadingLayer,
            SequenceId = shellState.TransitionSequenceId
        });
        Assert.IsTrue(UiShellRuntimeGateway.TrySetLoadingProgress(1f, "Ready", true));
        flowSystem.Update(_world.Unmanaged);

        shellState = em.GetComponentData<UiShellStateComponent>(boundary);
        matchIntro = em.GetComponentData<MatchIntroTransitionComponent>(boundary);
        Assert.AreEqual(UiShellMode.MatchHud, shellState.CurrentMode);
        Assert.AreEqual(UiShellTransitionPhase.EnteringMatchHud, shellState.Phase);
        Assert.AreEqual(MatchIntroTransitionStateKind.EnteringHud, matchIntro.State);
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
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls = AssertMatchHudFooterView(matchFooter).CommandControls;
        Assert.NotNull(controls);

        content.BindGameplayRuntimeDependencies(new SelectionUiCommandUiSystemHelper());
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
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchLeft = AssertRegionHasChild(content.ShellView, UIShellRegionId.LeftRegion);
        MatchHudSelectionPanelView selectionPanelView = matchLeft.GetComponent<MatchHudSelectionPanelView>();
        Assert.NotNull(selectionPanelView, "Installed Match HUD left region must own MatchHudSelectionPanelView.");

        Transform selectedPanel = matchLeft.transform.Find("SelectedSquadPanel");
        Assert.NotNull(selectedPanel, "Installed Match HUD must contain SelectedSquadPanel under LeftContent.");

        var feedback = new SelectionHudFeedbackUiSystemHelper();
        content.BindGameplayRuntimeDependencies(
            new SelectionUiCommandUiSystemHelper(),
            null,
            feedback.BindMatchHudSelectionPanel);
        Assert.IsFalse(selectedPanel.gameObject.activeSelf, "Runtime binding should start with the selection panel hidden.");

        Entity unit = CreatePlayerUnit(_world.EntityManager, "Echo Squad", new int2(8, 9), 96);
        feedback.ApplySelection(_world.EntityManager, unit, new SelectionUiReadModelLookup());

        Assert.IsTrue(selectedPanel.gameObject.activeSelf, "Selecting a valid unit must activate the active Match HUD SelectedSquadPanel.");
    }

    [Test]
    public void InstalledMatchHudCommandControlsUseSelectionReadModelCapabilities()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls = AssertMatchHudFooterView(matchFooter).CommandControls;
        Assert.NotNull(controls);

        var readModel = new FakeSelectionUiReadModel
        {
            CanHold = true,
            CanStop = true,
            CanScan = false,
            ScanReason = TacticalCommandReasonCode.ScanUnavailable
        };
        content.BindGameplayRuntimeDependencies(
            new SelectionUiCommandUiSystemHelper(),
            selectionUiReadModelSystem: readModel);
        content.RefreshMatchHudCommandControlState();

        Assert.IsTrue(controls.HoldButton.interactable, "Hold should follow the focused-unit read-model capability.");
        Assert.IsTrue(controls.StopButton.interactable, "Stop should follow the focused-unit read-model capability.");
        if (controls.CommandWheelStopButton != null)
            Assert.IsTrue(controls.CommandWheelStopButton.interactable, "Command wheel Stop should share the Stop capability model.");
        Assert.IsTrue(controls.ScanButton.interactable, "Scan should stay pressable when unavailable so the HUD can show rejection feedback.");

        readModel.CanHold = false;
        readModel.HoldReason = TacticalCommandReasonCode.CommandUnavailable;
        readModel.CanStop = false;
        readModel.StopReason = TacticalCommandReasonCode.CommandUnavailable;
        readModel.CanScan = true;
        readModel.ScanReason = TacticalCommandReasonCode.None;
        content.RefreshMatchHudCommandControlState();

        Assert.IsTrue(controls.HoldButton.interactable, "Hold stays pressable so the HUD can explain why the order is unavailable.");
        Assert.IsTrue(controls.StopButton.interactable, "Stop stays pressable so the HUD can explain why the order is unavailable.");
        if (controls.CommandWheelStopButton != null)
            Assert.IsFalse(controls.CommandWheelStopButton.interactable, "Command wheel Stop should keep sharing the Stop capability model.");
        Assert.IsTrue(controls.ScanButton.interactable, "Scan should enable when the read model allows scan.");
    }

    [Test]
    public void InstalledMatchHudRuntimeFeedbackBindsThroughMainMenuPlayUi()
    {
        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("UIShellCurrentContentLoadTests");
        World.DefaultGameObjectInjectionWorld = _world;

        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        BattleHudRuntimeFeedbackView runtimeFeedback = AssertMatchHudFooterView(matchFooter).RuntimeFeedback;
        Assert.NotNull(runtimeFeedback);

        var feedback = new SelectionHudFeedbackUiSystemHelper();
        var mainMenuPlayUi = new MainMenuPlayUI();
        mainMenuPlayUi.ConfigureMatchHudRuntimeFeedbackSinkBinding(feedback.BindBattleHudRuntimeFeedback);
        content.BindGameplayRuntimeDependencies(new SelectionUiCommandUiSystemHelper(), mainMenuPlayUi);

        feedback.ApplyCommandMode(_world.EntityManager, TacticalCommandMode.Move);

        Assert.IsTrue(runtimeFeedback.FeedbackPanel.activeSelf);
        Assert.AreEqual("Choose destination.", runtimeFeedback.FeedbackText.text);
        Assert.NotNull(runtimeFeedback.CurrentOrderBanner, "The separately installed footer runtime feedback must bind the live header CurrentOrderBanner.");
        Assert.IsTrue(runtimeFeedback.CurrentOrderBanner.BannerRoot.activeSelf, "Move command mode must show the live header CurrentOrderBanner.");
        Assert.AreEqual("MOVE ORDER", runtimeFeedback.CurrentOrderBanner.OrderText.text);
        Assert.AreEqual("Select a destination.", runtimeFeedback.CurrentOrderBanner.DescriptionText.text);
    }

    [Test]
    public void RightQuickRailBuildButtonShowsAndClosesBuildDrawerPopup()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content, "Menu scene must contain the shell content binder.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchRight = AssertRegionHasChild(content.ShellView, UIShellRegionId.RightRegion);
        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchHudRightQuickRailView quickRail = matchRight.GetComponent<MatchHudRightQuickRailView>();
        BattleHudRuntimeFeedbackView runtimeFeedback = AssertMatchHudFooterView(matchFooter).RuntimeFeedback;
        Assert.NotNull(quickRail, "RightContent must own MatchHudRightQuickRailView for serialized quick rail button bindings.");
        Assert.NotNull(quickRail.BuildButton, "Right quick rail Build button must be serialized.");
        Assert.NotNull(runtimeFeedback, "Match HUD footer must expose explicit runtime feedback for command state checks.");
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
        content.BindGameplayRuntimeDependencies(new SelectionUiCommandUiSystemHelper(), mainMenu);
        Assert.AreNotEqual(
            quickRail.BuildButton.gameObject,
            EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null,
            "Right quick rail Build button must not start in Unity selected state after Match HUD binding.");
        Assert.AreNotEqual(
            TacticalCommandMode.Build,
            BattleHudRuntimeFeedbackUiSystemHelper.GetState(runtimeFeedback).CurrentCommandMode,
            "Build command mode must not be active by default when the Match HUD loads.");

        Vector2 buttonCenter = GetButtonTargetGraphicCenterScreenPoint(quickRail.BuildButton);
        Assert.IsTrue(
            mainMenu.IsPointerOverAnyGameplayUi(buttonCenter, out string gameplayUiSource),
            "Runtime UI hit filter must treat the moved Build button as gameplay UI.");
        Assert.AreEqual(
            "MatchHudRightQuickRail",
            gameplayUiSource,
            "Runtime UI hit filter should identify the moved Build button as the right quick rail.");

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

        content.BindGameplayRuntimeDependencies(new SelectionUiCommandUiSystemHelper());
        int beforeInstallVersion = content.ContentVersion;

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        Assert.Greater(content.ContentVersion, beforeInstallVersion, "Installing Match HUD content must advance the shell content version.");
        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls = AssertMatchHudFooterView(matchFooter).CommandControls;
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
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMatchHud, default, default, default, 0)
        });

        GameObject matchFooter = AssertRegionHasChild(content.ShellView, UIShellRegionId.FooterRegion);
        MatchOverlayCommandControlsView controls = AssertMatchHudFooterView(matchFooter).CommandControls;
        Assert.NotNull(controls);

        var staleInputSystem = new MatchOverlayCommandInputUiSystemHelper();
        staleInputSystem.Bind(controls, new SelectionUiCommandUiSystemHelper());
        var currentInputSystem = new MatchOverlayCommandInputUiSystemHelper();
        currentInputSystem.Bind(controls, new SelectionUiCommandUiSystemHelper());

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

    private static MatchHudFooterContentView AssertMatchHudFooterView(GameObject matchFooter)
    {
        MatchHudFooterContentView footerView = matchFooter.GetComponent<MatchHudFooterContentView>();
        Assert.NotNull(
            footerView,
            "Match HUD FooterContent must own MatchHudFooterContentView so shell runtime binding uses serialized references.");
        return footerView;
    }

    private static void AssertRegionIsEmpty(UIShellView shell, UIShellRegionId regionId)
    {
        Assert.IsTrue(shell.TryGetRegion(regionId, out UIShellRegionView region), $"{regionId} must be registered.");
        Assert.NotNull(region.ContentRoot, $"{regionId} must have a content root.");
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} should be empty for the installed Match HUD.");
    }

    private static void AssertChildExists(Transform parent, string childName)
    {
        Assert.NotNull(parent != null ? parent.Find(childName) : null, $"{parent?.name ?? "<null>"} must contain child {childName}.");
    }

    private static void AssertDirectChildMissing(Transform parent, string childName, string message)
    {
        Assert.IsNull(parent != null ? parent.Find(childName) : null, message);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildRecursive(root.GetChild(i), childName);
            if (child != null)
                return child;
        }

        return null;
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
        var inputState = new RtsSelectionInputCompositionSystemHelper();
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

    private static void AssertPlacementBarSpritesAssigned(BuildPlacementConfirmationBarView view)
    {
        SerializedObject serialized = new(view);
        AssertSerializedReference(serialized, "root");
        AssertSerializedReference(serialized, "titleText");
        AssertSerializedReference(serialized, "statusText");
        AssertSerializedReference(serialized, "costText");
        AssertSerializedReference(serialized, "durationText");
        AssertSerializedReference(serialized, "instructionText");
        AssertSerializedReference(serialized, "cancelButton");
        AssertSerializedReference(serialized, "rotateButton");
        AssertSerializedReference(serialized, "confirmButton");
        AssertSerializedReference(serialized, "panelFrameSprite");
        AssertSerializedReference(serialized, "statusChipSprite");
        AssertSerializedReference(serialized, "secondaryButtonSprite");
        AssertSerializedReference(serialized, "goldActionButtonSprite");
        AssertSerializedReference(serialized, "squareButtonSprite");
        AssertSerializedReference(serialized, "instructionStripSprite");
        AssertSerializedReference(serialized, "materialsIconSprite");
        AssertSerializedReference(serialized, "timeIconSprite");
        AssertSerializedReference(serialized, "cancelIconSprite");
        AssertSerializedReference(serialized, "rotateIconSprite");
        AssertSerializedReference(serialized, "confirmIconSprite");
        AssertSerializedReference(serialized, "infoIconSprite");
    }

    private static void AssertCommanderActionDisabled(Transform root, string objectName)
    {
        Transform action = FindChildRecursive(root, objectName);
        Assert.NotNull(action, $"Commander prefab is missing {objectName}.");
        Button button = action.GetComponent<Button>();
        Assert.NotNull(button, $"{objectName} must retain its Button component for a future implementation.");
        Assert.IsFalse(button.interactable, $"{objectName} must be visibly disabled until its destination is implemented.");
    }

    private static void AssertCommanderProfileReadability(Transform root)
    {
        AssertCommanderElementMinimums(root, "VictoriesStatCard", 370f, 220f, "StatIcon", 90f, "Value", 60f);
        AssertCommanderElementMinimums(root, "CampaignSnapshot", 1500f, 140f, "Icon", 86f, "Mode", 42f);
        AssertCommanderElementMinimums(root, "RewardCard38", 190f, 180f, "Icon", 94f, "State", 22f);
        AssertCommanderElementMinimums(root, "FirstContactRow", 1250f, 148f, "Icon", 86f, "Title", 40f);
        Assert.NotNull(FindChildRecursive(root, "RewardXpBar"), "Commander reward track must expose a visible XP meter.");
        Assert.NotNull(FindChildRecursive(root, "CommanderFooterRail"), "Commander footer actions must share an integrated rail.");
    }

    private static void AssertCommanderElementMinimums(
        Transform root,
        string elementName,
        float minimumWidth,
        float minimumHeight,
        string iconName,
        float minimumIconSize,
        string labelName,
        float minimumFontSize)
    {
        RectTransform element = FindChildRecursive(root, elementName) as RectTransform;
        Assert.NotNull(element, $"Commander prefab is missing readability target {elementName}.");
        Assert.GreaterOrEqual(element.rect.width, minimumWidth, $"{elementName} is too narrow for its content.");
        Assert.GreaterOrEqual(element.rect.height, minimumHeight, $"{elementName} is too short for its content.");

        RectTransform icon = FindChildRecursive(element, iconName) as RectTransform;
        Assert.NotNull(icon, $"{elementName} is missing {iconName}.");
        Assert.GreaterOrEqual(Mathf.Min(icon.rect.width, icon.rect.height), minimumIconSize,
            $"{elementName}/{iconName} is too small at the logical design resolution.");

        Transform labelTransform = FindChildRecursive(element, labelName);
        TMP_Text label = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        Assert.NotNull(label, $"{elementName} is missing text {labelName}.");
        Assert.GreaterOrEqual(label.fontSize, minimumFontSize,
            $"{elementName}/{labelName} is too small at the logical design resolution.");
    }

    private static void AssertSerializedReference(SerializedObject serialized, string propertyName)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.NotNull(property, $"{propertyName} must exist on BuildPlacementConfirmationBarView.");
        Assert.NotNull(property.objectReferenceValue, $"{propertyName} must be assigned on the placement bar prefab.");
    }

    private sealed class FakeSelectionUiReadModel : ISelectionUiReadModel
    {
        public bool CanHold;
        public bool CanStop;
        public bool CanScan;
        public bool HasSelectedUnits = true;
        public TacticalCommandReasonCode HoldReason;
        public TacticalCommandReasonCode StopReason;
        public TacticalCommandReasonCode ScanReason;

        public bool HasAnySelectedUnits => HasSelectedUnits;
        public uint CommandStateVersion { get; set; }
        public bool FocusedUnitCanHold => CanHold;
        public TacticalCommandReasonCode FocusedUnitHoldDisabledReason => HoldReason;
        public bool FocusedUnitCanStop => CanStop;
        public TacticalCommandReasonCode FocusedUnitStopDisabledReason => StopReason;
        public bool FocusedUnitCanScan => CanScan;
        public TacticalCommandReasonCode FocusedUnitScanDisabledReason => ScanReason;
    }
}
