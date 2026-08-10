using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Composition;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class MatchGameplayStartupCompositionSystemHelperTests
{
    private const string BootstrapPath =
        "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs";
    private const string StartupHelperPath =
        "Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs";
    private const string MatchStartPath =
        "Assets/Game/Scripts/Composition/MatchStartSceneSystemHelper.cs";

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MatchGameplayStartupCompositionSystemHelperTests();
            tests.BeginGameplay_IsIdempotentAndPreservesPublicInitialState();
            tests.Bootstrap_ForwardsGameplayStartStateWithoutOwningIt();
            tests.Advance_CapturesFailureOnceAndDoesNotRetry();
            tests.ResetForShutdown_PreservesTheExistingFailureLatch();
            tests.MatchStart_DoesNotBlockGameplayRequestOnProjectedRuntimeContent();
            tests.MenuBootstrap_AdvancesMatchStartWhileLoadedViewIsPublishing();
            tests.SourceBoundary_IsNarrowAndKeepsTheBootstrapBelowItsRatchet();
            Debug.Log("[MatchGameplayStartupCompositionValidation] result=Passed tests=7");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MatchGameplayStartupCompositionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void BeginGameplay_IsIdempotentAndPreservesPublicInitialState()
    {
        var helper = new MatchGameplayStartupCompositionSystemHelper();

        Assert.IsFalse(helper.GameplayStartRequested);
        Assert.IsFalse(helper.GameplayStartComplete);
        Assert.IsFalse(helper.GameplayStartFailed);
        Assert.AreEqual(string.Empty, helper.GameplayStartFailureMessage);
        Assert.AreEqual(0f, helper.GameplayStartProgress01);
        Assert.AreEqual("Waiting for match scene", helper.GameplayStartStatus);

        helper.BeginGameplay();
        helper.BeginGameplay();

        Assert.IsTrue(helper.GameplayStartRequested);
        Assert.IsFalse(helper.GameplayStartComplete);
        Assert.IsFalse(helper.GameplayStartFailed);
        Assert.AreEqual(0f, helper.GameplayStartProgress01);
        Assert.AreEqual("Preparing match", helper.GameplayStartStatus);
    }

    [Test]
    public void Bootstrap_ForwardsGameplayStartStateWithoutOwningIt()
    {
        var bootstrap = new MatchBootstrapCompositionSystemHelper();

        bootstrap.BeginGameplay();

        Assert.IsTrue(bootstrap.GameplayStartRequested);
        Assert.IsFalse(bootstrap.GameplayStartComplete);
        Assert.IsFalse(bootstrap.GameplayStartFailed);
        Assert.AreEqual(string.Empty, bootstrap.GameplayStartFailureMessage);
        Assert.AreEqual(0f, bootstrap.GameplayStartProgress01);
        Assert.AreEqual("Preparing match", bootstrap.GameplayStartStatus);
    }

    [Test]
    public void Advance_CapturesFailureOnceAndDoesNotRetry()
    {
        using var world = new World(nameof(Advance_CapturesFailureOnceAndDoesNotRetry));
        var helper = new MatchGameplayStartupCompositionSystemHelper();
        var runtimeState = new RuntimeGameplayStateSystem(world.EntityManager);
        Exception reportedFailure = null;
        int initializeCalls = 0;
        helper.Bind(
            null,
            runtimeState,
            () =>
            {
                initializeCalls++;
                throw new InvalidOperationException("startup failed");
            },
            null,
            null,
            null,
            exception => reportedFailure = exception);

        helper.BeginGameplay();
        helper.Advance(null, default);
        helper.BeginGameplay();
        helper.Advance(null, default);

        Assert.AreEqual(1, initializeCalls);
        Assert.IsInstanceOf<InvalidOperationException>(reportedFailure);
        Assert.IsTrue(helper.GameplayStartRequested);
        Assert.IsFalse(helper.GameplayStartComplete);
        Assert.IsTrue(helper.GameplayStartFailed);
        Assert.AreEqual("startup failed", helper.GameplayStartFailureMessage);
        Assert.AreEqual("startup failed", helper.GameplayStartStatus);
        Assert.IsFalse(helper.PendingState);
    }

    [Test]
    public void ResetForShutdown_PreservesTheExistingFailureLatch()
    {
        using var world = new World(nameof(ResetForShutdown_PreservesTheExistingFailureLatch));
        var helper = new MatchGameplayStartupCompositionSystemHelper();
        int initializeCalls = 0;
        helper.Bind(
            null,
            new RuntimeGameplayStateSystem(world.EntityManager),
            () =>
            {
                initializeCalls++;
                throw new InvalidOperationException("latched failure");
            },
            null,
            null,
            null,
            _ => { });
        helper.BeginGameplay();
        helper.Advance(null, default);

        helper.ResetForShutdown();
        helper.BeginGameplay();
        helper.Advance(null, default);

        Assert.AreEqual(1, initializeCalls);
        Assert.IsTrue(helper.GameplayStartRequested);
        Assert.IsTrue(helper.GameplayStartFailed);
        Assert.AreEqual("latched failure", helper.GameplayStartFailureMessage);
        Assert.AreEqual("Preparing match", helper.GameplayStartStatus);
    }

    [Test]
    public void MatchStart_DoesNotBlockGameplayRequestOnProjectedRuntimeContent()
    {
        string source = File.ReadAllText(MatchStartPath);

        StringAssert.Contains("matchScene.BeginGameplay();", source);
        StringAssert.DoesNotContain("IsUnitPrefabRegistryReady", source);
        StringAssert.DoesNotContain("Waiting for unit prefab registry", source);
        StringAssert.DoesNotContain("RequiresUnitPrefabRegistry", source);
    }

    [Test]
    public void MenuBootstrap_AdvancesMatchStartWhileLoadedViewIsPublishing()
    {
        var composition = new MenuBootstrapCompositionSystemHelper();
        MethodInfo canAdvance = typeof(MenuBootstrapCompositionSystemHelper).GetMethod(
            "CanAdvanceMatchStart",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(canAdvance, Is.Not.Null);

        var matchState = new UiShellStateComponent { ActiveRoute = UIRoute.Match };
        var menuState = new UiShellStateComponent { ActiveRoute = UIRoute.MainMenu };

        Assert.That(canAdvance.Invoke(composition, new object[] { matchState }), Is.True);
        Assert.That(canAdvance.Invoke(composition, new object[] { menuState }), Is.False);
    }

    [Test]
    public void SourceBoundary_IsNarrowAndKeepsTheBootstrapBelowItsRatchet()
    {
        string bootstrapSource = File.ReadAllText(BootstrapPath);
        string helperSource = File.ReadAllText(StartupHelperPath);

        Assert.LessOrEqual(File.ReadAllLines(BootstrapPath).Length, 1163);
        Assert.LessOrEqual(File.ReadAllBytes(BootstrapPath).Length, 55416);
        StringAssert.Contains("MatchGameplayStartupCompositionSystemHelper", bootstrapSource);
        StringAssert.DoesNotContain("enum GameplayStartStep", bootstrapSource);
        StringAssert.DoesNotContain("AdvanceGameplayStartPipeline", bootstrapSource);
        StringAssert.DoesNotContain("ResolveCustomGameStartupSystemHelper", bootstrapSource);
        StringAssert.DoesNotContain("ResolveResourceExchangeStartupProjectionSystemHelper", bootstrapSource);
        StringAssert.DoesNotContain("ResolveMaterialsScenarioRecoveryStartupSystemHelper", bootstrapSource);
        StringAssert.DoesNotContain("ResolveAIStartupSystem", bootstrapSource);
        StringAssert.DoesNotContain("void Update(", helperSource);
        StringAssert.DoesNotContain(": ISystem", helperSource);
        StringAssert.DoesNotContain(": MonoBehaviour", helperSource);

        MatchCollection declarations = Regex.Matches(
            helperSource,
            @"\b(?:class|struct|interface)\s+(?<name>[A-Za-z0-9_]+)");
        foreach (Match declaration in declarations)
        {
            string name = declaration.Groups["name"].Value;
            StringAssert.DoesNotContain("Controller", name);
            StringAssert.DoesNotContain("Manager", name);
            StringAssert.DoesNotContain("Player", name);
        }
    }
}
#endif
