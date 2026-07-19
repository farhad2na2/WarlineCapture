using System;
using System.Linq;
using Game.Composition;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapRuntimeBindingSceneValidatorTests
{
    public static void RunFocusedValidation()
    {
        var tests = new OperationMapRuntimeBindingSceneValidatorTests();
        int passed = 0;
        try
        {
            tests.GeneratedScenePassesStructuralValidation();
            passed++;
            tests.GeneratedSceneDoesNotDependOnAuthoringScene();
            passed++;
            tests.PresentationOnlySceneRejectsAddedRenderer();
            passed++;
            Debug.Log($"[OperationMapRuntimeBindingSceneValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[OperationMapRuntimeBindingSceneValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void GeneratedScenePassesStructuralValidation()
    {
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            OpenSceneMode.Single);
        try
        {
            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    out string error),
                Is.True,
                error);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void GeneratedSceneDoesNotDependOnAuthoringScene()
    {
        string[] dependencies = AssetDatabase.GetDependencies(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            true);

        Assert.That(
            dependencies,
            Does.Not.Contain(StaticMapPresentationBaker.CurrentStagedOperationMapScenePath));
        Assert.That(
            dependencies.Any(path => path.Contains("GeneratedStaticMapPresentation", StringComparison.Ordinal)),
            Is.False,
            "Presentation chunk scenes must remain independently streamed dependencies.");
    }

    [Test]
    public void PresentationOnlySceneRejectsAddedRenderer()
    {
        Scene scene = EditorSceneManager.OpenScene(
            OperationMapRuntimeBindingSceneBuilder.OutputPath,
            OpenSceneMode.Single);
        try
        {
            OperationMapSceneView view = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
                .Single();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(view.MapRoot, false);

            Assert.That(
                OperationMapRuntimeBindingSceneValidator.TryValidateLoadedScene(
                    scene,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    out string error),
                Is.False);
            Assert.That(error, Does.Contain("renderer"));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
