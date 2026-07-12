using Game.Composition;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchSceneReferenceSceneSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(
                nameof(ResolveLoadedMatchSceneViewFromMatchSceneRoot),
                test => test.ResolveLoadedMatchSceneViewFromMatchSceneRoot(),
                ref passed);
            RunValidationStep(
                nameof(ReturnsFalseWhenMatchSceneIsNotLoaded),
                test => test.ReturnsFalseWhenMatchSceneIsNotLoaded(),
                ref passed);
            RunValidationStep(
                nameof(RepeatedLookup_ReusesRootScratchWithoutManagedAllocation),
                test => test.RepeatedLookup_ReusesRootScratchWithoutManagedAllocation(),
                ref passed);
            RunValidationStep(
                nameof(SceneReplacement_DoesNotReturnStaleView),
                test => test.SceneReplacement_DoesNotReturnStaleView(),
                ref passed);
            RunValidationStep(
                nameof(ViewReplacementWithinLoadedScene_ReturnsReplacement),
                test => test.ViewReplacementWithinLoadedScene_ReturnsReplacement(),
                ref passed);

            Debug.Log($"[MatchSceneReferenceFocusedValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[MatchSceneReferenceFocusedValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void ResolveLoadedMatchSceneViewFromMatchSceneRoot()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        GameObject root = new("Bootstrap");
        root.AddComponent<MatchSceneView>();

        MatchSceneReferenceSceneSystemHelper referenceSystem = new();

        Assert.IsTrue(referenceSystem.TryGetLoadedSceneView(scene, out MatchSceneView view));
        Assert.NotNull(view);
        Assert.AreEqual(scene.name, view.gameObject.scene.name);
    }

    [Test]
    public void ReturnsFalseWhenMatchSceneIsNotLoaded()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        MatchSceneReferenceSceneSystemHelper referenceSystem = new();

        Assert.IsFalse(referenceSystem.TryGetLoadedMatchSceneView(out MatchSceneView view));
        Assert.IsNull(view);
    }

    [Test]
    public void RepeatedLookup_ReusesRootScratchWithoutManagedAllocation()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        new GameObject("Bootstrap").AddComponent<MatchSceneView>();
        for (int i = 0; i < 11; i++)
            new GameObject($"AuthoredRoot{i}");
        MatchSceneReferenceSceneSystemHelper referenceSystem = new();

        Assert.IsTrue(referenceSystem.TryGetLoadedSceneView(scene, out _));
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 300; i++)
            referenceSystem.TryGetLoadedSceneView(scene, out _);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Assert.AreEqual(0L, allocatedBytes, "Warm scene-root lookup must reuse its capacity-stable list.");
    }

    [Test]
    public void SceneReplacement_DoesNotReturnStaleView()
    {
        Scene firstScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        MatchSceneView firstView = new GameObject("FirstBootstrap").AddComponent<MatchSceneView>();
        MatchSceneReferenceSceneSystemHelper referenceSystem = new();
        Assert.IsTrue(referenceSystem.TryGetLoadedSceneView(firstScene, out MatchSceneView resolvedFirst));
        Assert.AreEqual(firstView, resolvedFirst);

        Scene secondScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        Assert.IsFalse(referenceSystem.TryGetLoadedSceneView(firstScene, out _));
        Assert.IsFalse(referenceSystem.TryGetLoadedSceneView(secondScene, out _));

        MatchSceneView secondView = new GameObject("SecondBootstrap").AddComponent<MatchSceneView>();
        Assert.IsTrue(referenceSystem.TryGetLoadedSceneView(secondScene, out MatchSceneView resolvedSecond));
        Assert.AreEqual(secondView, resolvedSecond);
    }

    [Test]
    public void ViewReplacementWithinLoadedScene_ReturnsReplacement()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        MatchSceneView firstView = new GameObject("FirstBootstrap").AddComponent<MatchSceneView>();
        MatchSceneReferenceSceneSystemHelper referenceSystem = new();
        Assert.IsTrue(referenceSystem.TryGetLoadedSceneView(scene, out MatchSceneView resolvedFirst));
        Assert.AreEqual(firstView, resolvedFirst);

        UnityEngine.Object.DestroyImmediate(firstView.gameObject);
        MatchSceneView replacementView = new GameObject("ReplacementBootstrap").AddComponent<MatchSceneView>();

        Assert.IsTrue(referenceSystem.TryGetLoadedSceneView(scene, out MatchSceneView resolvedReplacement));
        Assert.AreEqual(replacementView, resolvedReplacement);
    }

    private static void RunValidationStep(
        string name,
        Action<MatchSceneReferenceSceneSystemHelperTests> action,
        ref int passed)
    {
        var tests = new MatchSceneReferenceSceneSystemHelperTests();
        try
        {
            action(tests);
            passed++;
        }
        finally
        {
            tests.TearDown();
        }
    }
}
#endif
