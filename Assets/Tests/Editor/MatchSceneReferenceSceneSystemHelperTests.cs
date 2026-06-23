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
