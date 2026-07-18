using System;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OperationMapSceneLoadingSceneSystemHelperTests
{
    private const string DefinitionPath =
        "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
    private const string ScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(RejectsMissingDefinition), test => test.RejectsMissingDefinition(), ref passed);
            Run(nameof(PendingLoadPublishesProgress), test => test.PendingLoadPublishesProgress(), ref passed);
            Run(nameof(SuccessfulLoadResolvesValidatedStagedView), test => test.SuccessfulLoadResolvesValidatedStagedView(), ref passed);
            Run(nameof(FailedLoadReleasesExactlyOnce), test => test.FailedLoadReleasesExactlyOnce(), ref passed);
            Run(nameof(DisposePendingLoadReleasesExactlyOnce), test => test.DisposePendingLoadReleasesExactlyOnce(), ref passed);
            Debug.Log($"[OperationMapSceneLoadingValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[OperationMapSceneLoadingValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        Scene loaded = SceneManager.GetSceneByPath(ScenePath);
        if (loaded.IsValid() && loaded.isLoaded)
            EditorSceneManager.CloseScene(loaded, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    }

    [Test]
    public void RejectsMissingDefinition()
    {
        var helper = new OperationMapSceneLoadingSceneSystemHelper(new FakeSceneApi());

        Assert.That(helper.TryStart(null, out string error), Is.False);
        Assert.That(error, Does.Contain("required"));
    }

    [Test]
    public void PendingLoadPublishesProgress()
    {
        var operation = new FakeSceneOperation { Progress = 0.42f };
        var helper = new OperationMapSceneLoadingSceneSystemHelper(
            new FakeSceneApi(operation));

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsLoading, Is.True);
        Assert.That(helper.Progress01, Is.EqualTo(0.42f));
    }

    [Test]
    public void SuccessfulLoadResolvesValidatedStagedView()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var operation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f
        };
        var helper = new OperationMapSceneLoadingSceneSystemHelper(
            new FakeSceneApi(operation));

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsReady, Is.True, helper.Failure);
        Assert.That(helper.SceneView, Is.Not.Null);
        Assert.That(helper.SceneView.gameObject.scene, Is.EqualTo(scene));
        helper.Dispose();
        Assert.That(operation.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedLoadReleasesExactlyOnce()
    {
        var operation = new FakeSceneOperation
        {
            Done = true,
            Success = false,
            FailureMessage = "catalog load failed"
        };
        var helper = new OperationMapSceneLoadingSceneSystemHelper(
            new FakeSceneApi(operation));

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        helper.Dispose();

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Does.Contain("catalog load failed"));
        Assert.That(operation.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void DisposePendingLoadReleasesExactlyOnce()
    {
        var operation = new FakeSceneOperation();
        var helper = new OperationMapSceneLoadingSceneSystemHelper(
            new FakeSceneApi(operation));

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Dispose();
        helper.Dispose();

        Assert.That(operation.DisposeCount, Is.EqualTo(1));
    }

    private static OperationMapDefinition LoadDefinition()
    {
        OperationMapDefinition definition =
            AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
        Assert.That(definition, Is.Not.Null);
        return definition;
    }

    private static void Run(
        string name,
        Action<OperationMapSceneLoadingSceneSystemHelperTests> action,
        ref int passed)
    {
        var tests = new OperationMapSceneLoadingSceneSystemHelperTests();
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

    private sealed class FakeSceneApi : IOperationMapSourceSceneApi
    {
        private readonly FakeSceneOperation operation;

        public FakeSceneApi(FakeSceneOperation operation = null)
        {
            this.operation = operation;
        }

        public IOperationMapSourceSceneOperation LoadAdditive(object runtimeKey)
        {
            return operation;
        }
    }

    private sealed class FakeSceneOperation : IOperationMapSourceSceneOperation
    {
        public bool Done;
        public bool Success;
        public float Progress;
        public Scene LoadedScene;
        public string FailureMessage;
        public int DisposeCount;

        public bool IsDone => Done;
        public bool Succeeded => Success;
        public float Progress01 => Progress;
        public Scene Scene => LoadedScene;
        public string Failure => FailureMessage;

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
