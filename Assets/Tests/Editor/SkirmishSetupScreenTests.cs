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

public sealed class SkirmishSetupScreenTests
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private World _previousWorld;
    private World _world;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(MenuSceneRoutesSkirmishSetupWithoutReplacingSharedHeader), test => test.MenuSceneRoutesSkirmishSetupWithoutReplacingSharedHeader(), ref passed);
            Run(nameof(MainMenuSkirmishEntryOpensSetupAndDeployRemainsDirect), test => test.MainMenuSkirmishEntryOpensSetupAndDeployRemainsDirect(), ref passed);
            Run(nameof(SkirmishAndBackButtonsDriveShellRouteHistory), test => test.SkirmishAndBackButtonsDriveShellRouteHistory(), ref passed);
            Run(nameof(SkirmishSetupReadsVisibleControlsAndLaunchesThroughContracts), test => test.SkirmishSetupReadsVisibleControlsAndLaunchesThroughContracts(), ref passed);
            Debug.Log($"[SkirmishSetupScreenValidation] result=Passed tests={passed}");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SkirmishSetupScreenValidation] result=Failed passed={passed}\n{exception}");
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
    public void MenuSceneRoutesSkirmishSetupWithoutReplacingSharedHeader()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        Assert.NotNull(content.SkirmishSetupContentPrefab, "Menu scene must assign the SCN-13 setup prefab.");
        Assert.AreEqual("SCN13_SkirmishSetupContent", content.SkirmishSetupContentPrefab.name);
        Assert.IsNull(FindRecursive(content.SkirmishSetupContentPrefab.transform, "HeaderContent"), "SCN-13 must not duplicate the shared header.");

        content.PrepareForCommandSequence(new[]
        {
            new UiShellPresentationCommandModel(UiShellCommandKind.EnterMenu, default, default, default, 0)
        });
        GameObject headerBefore = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);

        content.InstallMenuRouteBody(UIRoute.QuickCustomSetup);

        GameObject headerAfter = AssertRegionHasChild(content.ShellView, UIShellRegionId.HeaderRegion);
        Assert.AreSame(headerBefore, headerAfter, "Skirmish setup must preserve the shared Main Menu header instance.");
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.LeftRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.MiddleRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.RightRegion);
        AssertRegionIsEmpty(content.ShellView, UIShellRegionId.FooterRegion);
        GameObject setup = AssertRegionHasChild(content.ShellView, UIShellRegionId.PopupLayer);
        Assert.NotNull(setup.GetComponent<QuickCustomScreenView>(), "Routed SCN-13 must own the config controller.");
        Assert.NotNull(FindRecursive(setup.transform, "PresetRail"));
        Assert.NotNull(FindRecursive(setup.transform, "OperationPreview"));
        Assert.NotNull(FindRecursive(setup.transform, "OpposingForce"));
        Assert.NotNull(FindRecursive(setup.transform, "MatchEconomy"));
        Assert.NotNull(FindRecursive(setup.transform, "LaunchMissionButton"));
    }

    [Test]
    public void MainMenuSkirmishEntryOpensSetupAndDeployRemainsDirect()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        Transform skirmishCard = FindRecursive(content.MainMenuContentPrefab.transform, "Card_Skirmish");
        Assert.NotNull(skirmishCard);
        UIShellRouteButtonView skirmishRoute = skirmishCard.GetComponentInChildren<UIShellRouteButtonView>(true);
        Assert.NotNull(skirmishRoute, "Skirmish card must expose a route hotspot.");
        Assert.AreEqual(UiShellRouteIntent.OpenMenuRoute, skirmishRoute.Intent);
        Assert.AreEqual(UIRoute.QuickCustomSetup, skirmishRoute.Route);
        Assert.IsTrue(skirmishRoute.PushHistory);

        Transform back = FindRecursive(content.SkirmishSetupContentPrefab.transform, "BackButton");
        Assert.NotNull(back);
        UIShellRouteButtonView backRoute = back.GetComponent<UIShellRouteButtonView>();
        Assert.NotNull(backRoute);
        Assert.AreEqual(UiShellRouteIntent.BackMenuRoute, backRoute.Intent);
        Assert.AreEqual(UIRoute.MainMenu, backRoute.Route);

        UIShellRouteButtonView[] routes = content.MainMenuContentPrefab.GetComponentsInChildren<UIShellRouteButtonView>(true);
        int directDeployCount = 0;
        for (int i = 0; i < routes.Length; i++)
        {
            if (routes[i].Intent == UiShellRouteIntent.EnterMatch && routes[i].Route == UIRoute.Match)
                directDeployCount++;
        }
        Assert.GreaterOrEqual(directDeployCount, 1, "General Deploy must remain a direct Match launch.");
    }

    [Test]
    public void SkirmishAndBackButtonsDriveShellRouteHistory()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);

        _previousWorld = World.DefaultGameObjectInjectionWorld;
        _world = new World("SkirmishSetupRouteFlowTests");
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
        GameObject setupInstance = UnityEngine.Object.Instantiate(content.SkirmishSetupContentPrefab);
        try
        {
            UIShellRouteButtonView skirmishRoute = FindRecursive(menuInstance.transform, "Card_Skirmish")
                ?.GetComponentInChildren<UIShellRouteButtonView>(true);
            Assert.NotNull(skirmishRoute, "Instantiated Skirmish card must expose its route component.");
            skirmishRoute.SendMessage("OnEnable");
            Button skirmishButton = skirmishRoute.GetComponent<Button>();
            Assert.NotNull(skirmishButton, "Instantiated Skirmish card must expose its route button.");
            skirmishButton.onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            UiShellStateComponent shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            DynamicBuffer<UiShellRouteHistoryComponent> history = em.GetBuffer<UiShellRouteHistoryComponent>(boundary);
            Assert.AreEqual(UIRoute.QuickCustomSetup, shellState.ActiveRoute);
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

            UIShellRouteButtonView backRoute = FindRecursive(setupInstance.transform, "BackButton")
                ?.GetComponent<UIShellRouteButtonView>();
            Assert.NotNull(backRoute, "Instantiated SCN-13 must expose its Back route component.");
            backRoute.SendMessage("OnEnable");
            Button backButton = backRoute.GetComponent<Button>();
            Assert.NotNull(backButton, "Instantiated SCN-13 must expose its Back button.");
            backButton.onClick.Invoke();
            flowSystem.Update(_world.Unmanaged);

            shellState = em.GetComponentData<UiShellStateComponent>(boundary);
            Assert.AreEqual(UIRoute.MainMenu, shellState.ActiveRoute);
            Assert.AreEqual(0, history.Length);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(menuInstance);
            UnityEngine.Object.DestroyImmediate(setupInstance);
        }
    }

    [Test]
    public void SkirmishSetupReadsVisibleControlsAndLaunchesThroughContracts()
    {
        Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        UIShellContentView content = FindInScene<UIShellContentView>(scene);
        Assert.NotNull(content);
        GameObject instance = UnityEngine.Object.Instantiate(content.SkirmishSetupContentPrefab);
        try
        {
            QuickCustomScreenView view = instance.GetComponent<QuickCustomScreenView>();
            Assert.NotNull(view);
            var store = new FakeConfigStore();
            var launcher = new FakeLaunchCommand();
            view.BindRuntimeDependencies(store, launcher);

            UISegmentedControlView enemyCount = FindRecursive(instance.transform, "EnemyFactionStepper").GetComponent<UISegmentedControlView>();
            UISegmentedControlView difficulty = FindRecursive(instance.transform, "Difficulty").GetComponent<UISegmentedControlView>();
            UISegmentedControlView startingCredits = FindRecursive(instance.transform, "StartingCredits").GetComponent<UISegmentedControlView>();
            UISegmentedControlView startingResources = FindRecursive(instance.transform, "StartingResources").GetComponent<UISegmentedControlView>();
            UISegmentedControlView aggression = FindRecursive(instance.transform, "Aggression").GetComponent<UISegmentedControlView>();
            UISegmentedControlView winCondition = FindRecursive(instance.transform, "WinCondition").GetComponent<UISegmentedControlView>();
            UISliderRowView income = FindRecursive(instance.transform, "Income").GetComponent<UISliderRowView>();
            TMP_InputField seed = FindRecursive(instance.transform, "MapSeedInput").GetComponent<TMP_InputField>();
            Toggle fog = FindRecursive(instance.transform, "FogOfWar").GetComponentInChildren<Toggle>(true);
            Assert.NotNull(enemyCount);
            Assert.NotNull(difficulty);
            Assert.NotNull(startingCredits);
            Assert.NotNull(startingResources);
            Assert.NotNull(aggression);
            Assert.NotNull(winCondition);
            Assert.NotNull(income);
            Assert.NotNull(seed);
            Assert.NotNull(fog);
            Assert.IsFalse(fog.interactable, "Fog control must remain locked until fog runtime exists.");

            enemyCount.Bind(new[] { "-", "3", "+" }, 1);
            difficulty.Bind(new[] { "EASY", "NORMAL", "HARD", "BRUTAL" }, 2);
            startingCredits.Bind(new[] { "LOW", "STANDARD", "HIGH" }, 2);
            startingResources.Bind(new[] { "STANDARD", "LOW", "HIGH" }, 2);
            aggression.Bind(new[] { "DEFENSIVE", "BALANCED", "AGGRESSIVE" }, 2);
            winCondition.Bind(new[] { "DESTROY", "SURVIVE", "SANDBOX" }, 2);
            income.Slider.SetValueWithoutNotify(1.5f);
            seed.SetTextWithoutNotify("424242");

            view.ApplyCurrentConfigToRuntime();
            UiQuickCustomGameConfig applied = store.Current;
            Assert.AreEqual(3, applied.EnemyCount);
            Assert.AreEqual(UiAiDifficultySetting.Hard, applied.Difficulty);
            Assert.AreEqual(UiAiStartingMoneySetting.High, applied.StartingMoney);
            Assert.AreEqual(UiQuickGameStartingResources.High, applied.StartingResources);
            Assert.AreEqual(UiAiAggressionSetting.Aggressive, applied.Aggression);
            Assert.AreEqual(UiQuickGameWinCondition.Sandbox, applied.WinCondition);
            Assert.AreEqual(1.5f, applied.IncomeMultiplier, 0.001f);
            Assert.AreEqual(424242, applied.MapSeed);

            view.LaunchMatch();
            Assert.AreEqual(1, launcher.LaunchCount, "SCN-13 Launch must use IMatchLaunchCommand exactly once.");
            Assert.AreSame(view, launcher.Source);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void Run(string name, Action<SkirmishSetupScreenTests> action, ref int passed)
    {
        var test = new SkirmishSetupScreenTests();
        try
        {
            action(test);
            passed++;
            Debug.Log($"[SkirmishSetupScreenValidation] passed={name}");
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
        Assert.AreEqual(0, region.ContentRoot.childCount, $"{regionId} must be empty while SCN-13 owns the body overlay.");
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        var roots = new List<GameObject>();
        scene.GetRootGameObjects(roots);
        for (int i = 0; i < roots.Count; i++)
        {
            T result = roots[i].GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }
        return null;
    }

    private sealed class FakeConfigStore : IQuickCustomGameConfigStore
    {
        public UiQuickCustomGameConfig Current { get; private set; } = UiQuickCustomGameConfig.Defaults;
        public UiQuickCustomGameConfig Defaults => UiQuickCustomGameConfig.Defaults;
        public void Apply(UiQuickCustomGameConfig config) => Current = config;
    }

    private sealed class FakeLaunchCommand : IMatchLaunchCommand
    {
        public int LaunchCount { get; private set; }
        public Component Source { get; private set; }
        public void LaunchMatch(Component source)
        {
            LaunchCount++;
            Source = source;
        }
    }
}
