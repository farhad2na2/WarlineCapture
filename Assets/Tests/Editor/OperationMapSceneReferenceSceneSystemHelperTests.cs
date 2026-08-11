using System;
using System.IO;
using System.Reflection;
using Game.Composition;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapSceneReferenceSceneSystemHelperTests
{
    private const string OperationMapId = "opmap.skirmish.desert_base_01";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(ResolvesExactlyOneLoadedView), test => test.ResolvesExactlyOneLoadedView(), ref passed);
            Run(nameof(RejectsUnloadedScene), test => test.RejectsUnloadedScene(), ref passed);
            Run(nameof(RejectsMultipleViews), test => test.RejectsMultipleViews(), ref passed);
            Run(nameof(RejectsRequestedIdentityMismatch), test => test.RejectsRequestedIdentityMismatch(), ref passed);
            Run(nameof(WarmLookupDoesNotAllocate), test => test.WarmLookupDoesNotAllocate(), ref passed);
            Run(nameof(BoundaryIsTransitionOnlyAndReusesStorage), test => test.BoundaryIsTransitionOnlyAndReusesStorage(), ref passed);
            Debug.Log($"[OperationMapSceneReferenceValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[OperationMapSceneReferenceValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void ResolvesExactlyOneLoadedView()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        OperationMapSceneView expected = CreateView("OperationMapSceneView", OperationMapId);
        var helper = new OperationMapSceneReferenceSceneSystemHelper();

        Assert.That(
            helper.TryGetLoadedSceneView(scene, OperationMapId, out OperationMapSceneView actual, out string error),
            Is.True,
            error);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void RejectsUnloadedScene()
    {
        var helper = new OperationMapSceneReferenceSceneSystemHelper();

        Assert.That(
            helper.TryGetLoadedSceneView(default, OperationMapId, out OperationMapSceneView view, out string error),
            Is.False);
        Assert.That(view, Is.Null);
        Assert.That(error, Does.Contain("not loaded"));
    }

    [Test]
    public void RejectsMultipleViews()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        CreateView("FirstOperationMapSceneView", OperationMapId);
        CreateView("SecondOperationMapSceneView", OperationMapId);
        var helper = new OperationMapSceneReferenceSceneSystemHelper();

        Assert.That(
            helper.TryGetLoadedSceneView(scene, OperationMapId, out _, out string error),
            Is.False);
        Assert.That(error, Does.Contain("found 2"));
    }

    [Test]
    public void RejectsRequestedIdentityMismatch()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        CreateView("OperationMapSceneView", OperationMapId);
        var helper = new OperationMapSceneReferenceSceneSystemHelper();

        Assert.That(
            helper.TryGetLoadedSceneView(scene, "opmap.test.other", out _, out string error),
            Is.False);
        Assert.That(error, Does.Contain("identity"));
    }

    [Test]
    public void WarmLookupDoesNotAllocate()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        CreateView("OperationMapSceneView", OperationMapId);
        for (int index = 0; index < 8; index++)
            new GameObject($"MapRoot{index}");
        var helper = new OperationMapSceneReferenceSceneSystemHelper();
        Assert.That(helper.TryGetLoadedSceneView(scene, OperationMapId, out _, out _), Is.True);

        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 300; iteration++)
            helper.TryGetLoadedSceneView(scene, OperationMapId, out _, out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;

        Assert.That(allocated, Is.EqualTo(0L));
    }

    [Test]
    public void BoundaryIsTransitionOnlyAndReusesStorage()
    {
        string compositionRoot = Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "Game/Scripts/Composition"));
        string helperPath = Path.Combine(
            compositionRoot,
            "OperationMapSceneReferenceSceneSystemHelper.cs");
        string helperSource = File.ReadAllText(helperPath);
        string loaderSource = File.ReadAllText(Path.Combine(
            compositionRoot,
            "OperationMapSceneLoadingSceneSystemHelper.cs"));
        int callSites = loaderSource.Split(
            new[] { "sceneReference.TryGetLoadedSceneView(" },
            StringSplitOptions.None).Length - 1;

        Assert.That(File.ReadAllLines(helperPath).Length, Is.EqualTo(67));
        Assert.That(new FileInfo(helperPath).Length, Is.EqualTo(2223));
        Assert.That(helperSource, Does.Contain("private readonly List<GameObject> roots = new(4);"));
        Assert.That(helperSource, Does.Contain("private readonly List<OperationMapSceneView> candidates = new(2);"));
        Assert.That(helperSource, Does.Contain("roots.Clear();"));
        Assert.That(helperSource, Does.Contain("candidates.Clear();"));
        Assert.That(helperSource, Does.Not.Contain("Update("));
        Assert.That(helperSource, Does.Not.Contain("static readonly"));
        Assert.That(callSites, Is.EqualTo(1));
    }

    private static OperationMapSceneView CreateView(string name, string operationMapId)
    {
        OperationMapSceneView view = new GameObject(name).AddComponent<OperationMapSceneView>();
        FieldInfo field = typeof(OperationMapSceneView).GetField(
            "operationMapId",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(view, operationMapId);
        return view;
    }

    private static void Run(
        string name,
        Action<OperationMapSceneReferenceSceneSystemHelperTests> action,
        ref int passed)
    {
        var tests = new OperationMapSceneReferenceSceneSystemHelperTests();
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
