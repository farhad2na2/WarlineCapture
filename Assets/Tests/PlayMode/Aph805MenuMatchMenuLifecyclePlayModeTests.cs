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
    private const string MenuSceneName = "Menu";
    private const string MatchSceneName = "Match";
    private const float LifecycleTimeoutSeconds = 180f;

    [UnityTest]
    public IEnumerator MenuToMatchToMenu_PreservesWorldBindsUiAndCleansMatchRuntime()
    {
        yield return EnsureMatchIsUnloaded();
        yield return LoadScene(MenuSceneName, LoadSceneMode.Single);
        yield return WaitUntil(
            () => FindInLoadedScene<MenuBootstrapView>(MenuSceneName) != null,
            "MenuBootstrapView did not become available.");

        MenuBootstrapView menu = FindInLoadedScene<MenuBootstrapView>(MenuSceneName);
        AssertMenuSerializedReferences(menu);

        yield return WaitUntil(
            () => World.DefaultGameObjectInjectionWorld is { IsCreated: true },
            "The default ECS world was not created while Menu was active.");

        World lifecycleWorld = World.DefaultGameObjectInjectionWorld;
        yield return WaitUntil(
            () => CountLifecycleRoots(lifecycleWorld) == 1,
            "Menu composition did not create exactly one scene-lifecycle root.");
        AssertLifecycleRootCount(lifecycleWorld, 1, "Menu composition must own one scene-lifecycle root.");

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
        yield return WaitUntil(
            () => FindInLoadedScene<MatchSceneView>(MatchSceneName) != null,
            "MatchSceneView did not become available after Match loaded.");

        MatchSceneView match = FindInLoadedScene<MatchSceneView>(MatchSceneName);
        AssertMatchSerializedReferences(match);
        Assert.That(World.DefaultGameObjectInjectionWorld, Is.SameAs(lifecycleWorld));
        Assert.That(lifecycleWorld.IsCreated, Is.True);
        AssertLifecycleRootCount(lifecycleWorld, 1, "Match loading must not duplicate the scene-lifecycle root.");
        yield return WaitUntil(
            () => CountOperationMapRoots(lifecycleWorld) == 1,
            "Match composition did not publish exactly one active compatibility operation map.");
        AssertActiveCompatibilityMap(lifecycleWorld);

        yield return WaitUntil(
            () => match.MatchBootstrap.SelectionUiCommand != null &&
                  match.MatchBootstrap.SelectionUiReadModel != null &&
                  match.MatchBootstrap.MainMenu != null,
            "Match composition did not create the runtime UI dependencies.");
        yield return WaitUntil(
            () => IsMatchUiBound(menu.ContentSystem, match.MatchBootstrap),
            "Menu composition did not bind Match runtime dependencies into the installed HUD.");

        MatchBootstrapCompositionSystemHelper matchBootstrap = match.MatchBootstrap;
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
        yield return null;

        Assert.That(SceneManager.GetSceneByName(MenuSceneName).isLoaded, Is.True);
        Assert.That(FindInLoadedScene<MatchSceneView>(MatchSceneName), Is.Null);
        Assert.That(World.DefaultGameObjectInjectionWorld, Is.SameAs(lifecycleWorld));
        Assert.That(lifecycleWorld.IsCreated, Is.True);
        AssertLifecycleRootCount(lifecycleWorld, 1, "Returning to Menu must preserve one lifecycle root.");
        Assert.That(CountOperationMapRoots(lifecycleWorld), Is.Zero,
            "Returning to Menu must dispose the compatibility operation-map root.");
        Assert.That(matchBootstrap.HasSceneView, Is.False, "Match composition retained its destroyed scene view.");
        Assert.That(matchBootstrap.SelectionUiCommand, Is.Null, "Match selection UI command survived teardown.");
        Assert.That(matchBootstrap.SelectionUiReadModel, Is.Null, "Match selection read model survived teardown.");
        Assert.That(matchBootstrap.MainMenu, Is.Null, "Match runtime UI survived teardown.");
        Assert.That(
            menu.ContentSystem.GetComponentInChildren<MatchHudFooterContentView>(includeInactive: true),
            Is.Null,
            "Match HUD content remained installed after returning to Menu.");
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
        Assert.That(ReadPrivateField(match, "staticMapPresentationManifest"), Is.Not.Null);
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

    private static IEnumerator EnsureMatchIsUnloaded()
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
}
