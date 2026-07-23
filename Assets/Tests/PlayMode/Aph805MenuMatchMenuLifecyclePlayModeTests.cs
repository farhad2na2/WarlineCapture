using System.Collections;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class Aph805MenuMatchMenuLifecyclePlayModeTests
{
    internal const string MenuSceneName = "Menu";
    internal const string MatchSceneName = "Match";
    internal const string OperationMapSceneName = "opmap_skirmish_desert_base_01_runtime";
    internal const float LifecycleTimeoutSeconds = 180f;
    internal const float OperationMapLoadTimeoutSeconds = 30f;

    internal sealed class TransitionContext
    {
        public MenuBootstrapView Menu { get; set; }
        public MatchSceneView Match { get; set; }
        public MatchBootstrapCompositionSystemHelper MatchBootstrap { get; set; }
        public World World { get; set; }
        public string OperationMapSceneName { get; set; } =
            Aph805MenuMatchMenuLifecyclePlayModeTests.OperationMapSceneName;
    }

    [UnityTest]
    public IEnumerator MenuToMatchToMenu_PreservesWorldBindsUiAndCleansMatchRuntime()
    {
        var context = new TransitionContext();
        yield return PrepareStableMenu(context);
        yield return EnterStableMatch(context);
        yield return ReturnToStableMenu(context);
    }

    [UnityTest]
    public IEnumerator TwoSequentialMatches_ReleaseMapStateBeforeTheNextLoad()
    {
        var context = new TransitionContext();
        yield return PrepareStableMenu(context);

        yield return EnterStableMatch(context);
        yield return ReturnToStableMenu(context);

        yield return EnterStableMatch(context);
        yield return ReturnToStableMenu(context);
    }

    internal static IEnumerator PrepareStableMenu(TransitionContext context)
    {
        Assert.That(context, Is.Not.Null);
        yield return EnsureMatchIsUnloaded();
        yield return LoadScene(MenuSceneName, LoadSceneMode.Single);
        yield return WaitUntil(
            () => FindInLoadedScene<MenuBootstrapView>(MenuSceneName) != null,
            "MenuBootstrapView did not become available.");

        context.Menu = FindInLoadedScene<MenuBootstrapView>(MenuSceneName);
        AssertMenuSerializedReferences(context.Menu);

        yield return WaitUntil(
            () => World.DefaultGameObjectInjectionWorld is { IsCreated: true },
            "The default ECS world was not created while Menu was active.");

        context.World = World.DefaultGameObjectInjectionWorld;
        yield return WaitUntil(
            () => CountLifecycleRoots(context.World) == 1,
            "Menu composition did not create exactly one scene-lifecycle root.");
        AssertLifecycleRootCount(context.World, 1, "Menu composition must own one scene-lifecycle root.");
    }

    internal static IEnumerator EnterStableMatch(TransitionContext context)
    {
        Assert.That(context?.Menu, Is.Not.Null);
        Assert.That(context.World, Is.Not.Null);
        Assert.That(
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.EnterMatch,
                UIRoute.Match,
                pushHistory: false),
            Is.True,
            "The production UI gateway rejected the Menu -> Match request.");

        yield return WaitUntil(
            () => SceneManager.GetSceneByName(MatchSceneName).isLoaded,
            "The production Menu composition did not load Match additively.");
        Debug.Log("[Aph805Lifecycle] stage=MatchSceneLoaded");
        yield return WaitUntil(
            () => FindInLoadedScene<MatchSceneView>(MatchSceneName) != null,
            "MatchSceneView did not become available after Match loaded.");

        context.Match = FindInLoadedScene<MatchSceneView>(MatchSceneName);
        Debug.Log("[Aph805Lifecycle] stage=MatchSceneViewResolved");
        yield return WaitForOperationMapContent(context.Match);
        Debug.Log("[Aph805Lifecycle] stage=OperationMapContentReady");
        Assert.That(
            SceneManager.GetSceneByName(context.OperationMapSceneName).isLoaded,
            Is.True,
            "The selected operation-map source scene was not loaded additively.");
        AssertMatchSerializedReferences(context.Match);
        Assert.That(World.DefaultGameObjectInjectionWorld, Is.SameAs(context.World));
        Assert.That(context.World.IsCreated, Is.True);
        AssertLifecycleRootCount(context.World, 1, "Match loading must not duplicate the scene-lifecycle root.");
        yield return WaitUntil(
            () => CountOperationMapRoots(context.World) == 1,
            "Match composition did not publish exactly one active compatibility operation map.");
        Debug.Log("[Aph805Lifecycle] stage=OperationMapRootPublished");
        AssertActiveCompatibilityMap(context.World);

        yield return WaitForRuntimeUiDependencies(context.Match);
        Debug.Log("[Aph805Lifecycle] stage=RuntimeUiDependenciesReady");
        yield return WaitUntil(
            () => IsMatchUiBound(context.Menu.ContentSystem, context.Match.MatchBootstrap),
            "Menu composition did not bind Match runtime dependencies into the installed HUD.");
        Debug.Log("[Aph805Lifecycle] stage=MatchUiBound");
        yield return WaitUntil(
            () => IsStableShellState(UIRoute.Match, UiShellMode.MatchHud, UiShellTransitionPhase.MatchHudReady),
            "Match shell route did not reach its stable idle checkpoint.");
        Debug.Log("[Aph805Lifecycle] stage=MatchStable");

        context.MatchBootstrap = context.Match.MatchBootstrap;
        bool entityScene =
            context.Match.CanonicalPresentationMode ==
            Game.Rendering.OperationMapCanonicalPresentationMode.EntityScene;
        Assert.That(
            context.MatchBootstrap.RuntimeCity,
            entityScene ? Is.Null : Is.Not.Null,
            entityScene
                ? "EntityScene Match must not construct the legacy runtime city generator."
                : "StaticSceneChunks Match must preserve legacy runtime city ownership.");
    }

    internal static IEnumerator ReturnToStableMenu(TransitionContext context)
    {
        Assert.That(context?.MatchBootstrap, Is.Not.Null);
        Assert.That(
            UiShellRuntimeGateway.TryEnqueueRouteRequest(
                UiShellRouteIntent.ReturnToMainMenu,
                UIRoute.MainMenu,
                pushHistory: false),
            Is.True,
            "The production UI gateway rejected the Match -> Menu request.");

        yield return WaitUntil(
            () => !SceneManager.GetSceneByName(MatchSceneName).isLoaded,
            "The production Menu composition did not unload Match.");
        yield return WaitUntil(
            () => IsStableShellState(UIRoute.MainMenu, UiShellMode.MainMenu, UiShellTransitionPhase.MenuReady),
            "Menu shell route did not reach its stable idle checkpoint.");

        Assert.That(SceneManager.GetSceneByName(MenuSceneName).isLoaded, Is.True);
        Assert.That(
            SceneManager.GetSceneByName(context.OperationMapSceneName).isLoaded,
            Is.False,
            "The operation-map source scene remained loaded after Match teardown.");
        Assert.That(FindInLoadedScene<MatchSceneView>(MatchSceneName), Is.Null);
        Assert.That(World.DefaultGameObjectInjectionWorld, Is.SameAs(context.World));
        Assert.That(context.World.IsCreated, Is.True);
        AssertLifecycleRootCount(context.World, 1, "Returning to Menu must preserve one lifecycle root.");
        Assert.That(CountOperationMapRoots(context.World), Is.Zero,
            "Returning to Menu must dispose the compatibility operation-map root.");
        Assert.That(context.MatchBootstrap.HasSceneView, Is.False, "Match composition retained its destroyed scene view.");
        Assert.That(context.MatchBootstrap.SelectionUiCommand, Is.Null, "Match selection UI command survived teardown.");
        Assert.That(context.MatchBootstrap.SelectionUiReadModel, Is.Null, "Match selection read model survived teardown.");
        Assert.That(context.MatchBootstrap.MainMenu, Is.Null, "Match runtime UI survived teardown.");
        Assert.That(
            context.Menu.ContentSystem.GetComponentInChildren<MatchHudFooterContentView>(includeInactive: true),
            Is.Null,
            "Match HUD content remained installed after returning to Menu.");
        context.Match = null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return EnsureMatchIsUnloaded();
    }

    private static void AssertMenuSerializedReferences(MenuBootstrapView menu)
    {
        Assert.That(menu, Is.Not.Null);
        Assert.That(menu.RuntimeUiConfig, Is.Not.Null);
        Assert.That(menu.UiCamera, Is.Not.Null);
        Assert.That(menu.UiCanvas, Is.Not.Null);
        Assert.That(menu.ShellView, Is.Not.Null);
        Assert.That(menu.ShellEcsPresentation, Is.Not.Null);
        Assert.That(menu.ContentSystem, Is.Not.Null);
        Assert.That(menu.Router, Is.Not.Null);
        Assert.That(menu.Router.ContentRoot, Is.Not.Null);
        Assert.That(menu.ContentSystem.MatchHudContentPrefab, Is.Not.Null);
    }

    private static void AssertMatchSerializedReferences(MatchSceneView match)
    {
        Assert.That(match, Is.Not.Null);
        Assert.That(match.WorldCamera, Is.Not.Null);
        Assert.That(match.DirectionalLight, Is.Not.Null);
        Assert.That(ReadPrivateField(match, "globalVolume"), Is.Not.Null);
        Assert.That(ReadPrivateField(match, "staticMapPresentationManifest"), Is.Null);
        Assert.That(
            match.StaticMapPresentationManifest,
            match.CanonicalPresentationMode == Game.Rendering.OperationMapCanonicalPresentationMode.EntityScene
                ? Is.Null
                : Is.Not.Null);
        Assert.That(match.MapSurfaceAuthoring, Is.Not.Null);
        bool entityScene =
            match.CanonicalPresentationMode ==
            Game.Rendering.OperationMapCanonicalPresentationMode.EntityScene;
        Assert.That(match.MapBuildingPlacementConfig, entityScene ? Is.Null : Is.Not.Null);
        Assert.That(match.MapVehiclePlacementConfig, entityScene ? Is.Null : Is.Not.Null);
        Assert.That(match.RtsSelectionConfig, Is.Not.Null);
        Assert.That(match.BuildingPlacementConfig, Is.Not.Null);
        Assert.That(match.RuntimeGridConfig, Is.Not.Null);
        Assert.That(match.GameStringsConfig, Is.Not.Null);
        Assert.That(match.OperationMapCatalog, Is.Not.Null);
        Assert.That(match.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(match.ScenarioId, Is.EqualTo("scenario.skirmish.desert_base_standard"));
        Assert.That(match.MissionId, Is.EqualTo("skirmish"));
    }

    private static void AssertActiveCompatibilityMap(World world)
    {
        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRootComponent>(),
            ComponentType.ReadOnly<ActiveOperationMapComponent>());
        Entity root = query.GetSingletonEntity();
        ActiveOperationMapComponent active =
            world.EntityManager.GetComponentData<ActiveOperationMapComponent>(root);
        Assert.That(active.OperationMapId.ToString(), Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(active.ScenarioId.ToString(), Is.EqualTo("scenario.skirmish.desert_base_standard"));
        Assert.That(active.MissionId.ToString(), Is.EqualTo("skirmish"));
    }

    private static int CountOperationMapRoots(World world)
    {
        if (world is not { IsCreated: true })
            return 0;
        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRootComponent>());
        return query.CalculateEntityCount();
    }

    private static bool IsMatchUiBound(
        UIShellContentView content,
        MatchBootstrapCompositionSystemHelper matchBootstrap)
    {
        return ReferenceEquals(ReadPrivateField(content, "_selectionUiCommandSystem"), matchBootstrap.SelectionUiCommand) &&
               ReferenceEquals(ReadPrivateField(content, "_selectionUiReadModelSystem"), matchBootstrap.SelectionUiReadModel) &&
               ReferenceEquals(ReadPrivateField(content, "_mainMenuPlayUi"), matchBootstrap.MainMenu);
    }

    private static bool IsStableShellState(
        UIRoute route,
        UiShellMode mode,
        UiShellTransitionPhase phase)
    {
        return UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel state) &&
               !state.IsTransitionRunning &&
               state.ActiveRoute == route &&
               state.CurrentMode == mode &&
               state.Phase == phase;
    }

    private static object ReadPrivateField(object owner, string fieldName)
    {
        if (owner == null)
            return null;

        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected focused lifecycle field '{fieldName}' was not found.");
        return field.GetValue(owner);
    }

    private static void AssertLifecycleRootCount(World world, int expected, string message)
    {
        Assert.That(CountLifecycleRoots(world), Is.EqualTo(expected), message);
    }

    private static int CountLifecycleRoots(World world)
    {
        if (world == null || !world.IsCreated)
            return 0;

        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<SceneLifecycleRootComponent>());
        return query.CalculateEntityCount();
    }

    private static T FindInLoadedScene<T>(string sceneName) where T : Component
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            T component = roots[rootIndex].GetComponentInChildren<T>(includeInactive: true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static IEnumerator LoadScene(string sceneName, LoadSceneMode mode)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
        Assert.That(operation, Is.Not.Null, $"Failed to start loading scene '{sceneName}'.");
        while (!operation.isDone)
            yield return null;
    }

    internal static IEnumerator EnsureMatchIsUnloaded()
    {
        Scene match = SceneManager.GetSceneByName(MatchSceneName);
        if (!match.IsValid() || !match.isLoaded)
            yield break;

        AsyncOperation operation = SceneManager.UnloadSceneAsync(match);
        Assert.That(operation, Is.Not.Null, "Failed to start Match cleanup.");
        while (!operation.isDone)
            yield return null;
    }

    private static IEnumerator WaitUntil(System.Func<bool> predicate, string failureMessage)
    {
        float deadline = Time.realtimeSinceStartup + LifecycleTimeoutSeconds;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup >= deadline)
                Assert.Fail(failureMessage);
            yield return null;
        }
    }

    private static IEnumerator WaitForOperationMapContent(MatchSceneView match)
    {
        float deadline = Time.realtimeSinceStartup + OperationMapLoadTimeoutSeconds;
        while (!match.OperationMapContentReady)
        {
            if (!string.IsNullOrEmpty(match.OperationMapContentFailure))
                Assert.Fail($"Addressable operation-map content failed: {match.OperationMapContentFailure}");
            if (Time.realtimeSinceStartup >= deadline)
            {
                Assert.Fail(
                    "Addressable operation-map content did not become ready. " +
                    $"sourceComplete={match.OperationMapSourceSceneLoadComplete} " +
                    $"manifestComplete={match.OperationMapPresentationManifestLoadComplete} " +
                    $"progress={match.OperationMapContentProgress01:0.000}");
            }
            yield return null;
        }
    }

    private static IEnumerator WaitForRuntimeUiDependencies(MatchSceneView match)
    {
        float deadline = Time.realtimeSinceStartup + OperationMapLoadTimeoutSeconds;
        while (match.MatchBootstrap.SelectionUiCommand == null ||
               match.MatchBootstrap.SelectionUiReadModel == null ||
               match.MatchBootstrap.MainMenu == null)
        {
            if (match.GameplayStartFailed)
                Assert.Fail($"Match gameplay startup failed: {match.GameplayStartFailureMessage}");
            if (Time.realtimeSinceStartup >= deadline)
            {
                Assert.Fail(
                    "Match composition did not create the runtime UI dependencies. " +
                    $"requested={match.GameplayStartRequested} " +
                    $"complete={match.GameplayStartComplete} " +
                    $"progress={match.GameplayStartProgress01:0.000} " +
                    $"status={match.GameplayStartStatus} " +
                    $"matchStart={ReadMatchStartProgress()} " +
                    $"unitRegistryReady={IsUnitPrefabRegistryReady()}");
            }
            yield return null;
        }
    }

    private static string ReadMatchStartProgress()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return "world-missing";
        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MatchStartProgressComponent>());
        if (query.CalculateEntityCount() != 1)
            return $"count-{query.CalculateEntityCount()}";
        MatchStartProgressComponent progress = query.GetSingleton<MatchStartProgressComponent>();
        return $"{progress.Progress01:0.000}:{progress.Status}";
    }

    private static bool IsUnitPrefabRegistryReady()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;
        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
            ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
        return !query.IsEmptyIgnoreFilter;
    }
}
