using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
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
            Run(nameof(FailedManifestLoadReleasesBothExactlyOnce), test => test.FailedManifestLoadReleasesBothExactlyOnce(), ref passed);
            Run(nameof(MismatchedManifestFailsClosed), test => test.MismatchedManifestFailsClosed(), ref passed);
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
        var helper = CreateHelper(new FakeSceneOperation(), new FakeManifestOperation());

        Assert.That(helper.TryStart(null, out string error), Is.False);
        Assert.That(error, Does.Contain("required"));
    }

    [Test]
    public void PendingLoadPublishesProgress()
    {
        var operation = new FakeSceneOperation { Progress = 0.42f };
        var manifestOperation = new FakeManifestOperation { Progress = 0.58f };
        var helper = CreateHelper(operation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsLoading, Is.True);
        Assert.That(helper.Progress01, Is.EqualTo(0.5f));
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
        var manifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var helper = CreateHelper(operation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsReady, Is.True, helper.Failure);
        Assert.That(helper.SceneView, Is.Not.Null);
        Assert.That(helper.Manifest, Is.SameAs(manifestOperation.LoadedManifest));
        Assert.That(helper.SceneView.gameObject.scene, Is.EqualTo(scene));
        helper.Dispose();
        Assert.That(operation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(manifestOperation.LoadedManifest);
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
        var manifestOperation = new FakeManifestOperation();
        var helper = CreateHelper(operation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        helper.Dispose();

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Does.Contain("catalog load failed"));
        Assert.That(operation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedManifestLoadReleasesBothExactlyOnce()
    {
        var sceneOperation = new FakeSceneOperation();
        var manifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = false,
            FailureMessage = "manifest load failed"
        };
        var helper = CreateHelper(sceneOperation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        helper.Dispose();

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Does.Contain("manifest load failed"));
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void MismatchedManifestFailsClosed()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var sceneOperation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f
        };
        var manifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = LoadConfiguredManifest(),
            Progress = 1f
        };
        var helper = CreateHelper(sceneOperation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Does.Contain("does not match"));
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void DisposePendingLoadReleasesExactlyOnce()
    {
        var operation = new FakeSceneOperation();
        var manifestOperation = new FakeManifestOperation();
        var helper = CreateHelper(operation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Dispose();
        helper.Dispose();

        Assert.That(operation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
    }

    private static OperationMapSceneLoadingSceneSystemHelper CreateHelper(
        FakeSceneOperation sceneOperation,
        FakeManifestOperation manifestOperation) =>
        new(
            new FakeSceneApi(sceneOperation),
            new FakeManifestApi(manifestOperation));

    private static OperationMapDefinition LoadDefinition()
    {
        OperationMapDefinition definition =
            AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(DefinitionPath);
        Assert.That(definition, Is.Not.Null);
        return definition;
    }

    private static StaticMapPresentationManifest LoadConfiguredManifest()
    {
        OperationMapDefinition definition = LoadDefinition();
        string path = AssetDatabase.GUIDToAssetPath(
            definition.StaticPresentationManifestReference.AssetGUID);
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(path);
        Assert.That(manifest, Is.Not.Null);
        return manifest;
    }

    private static StaticMapPresentationManifest CreateMatchingManifest(Scene scene)
    {
        OperationMapDefinition definition = LoadDefinition();
        StaticMapPresentationManifest source = LoadConfiguredManifest();
        StaticMapPresentationManifest manifest =
            UnityEngine.Object.Instantiate(source);
        manifest.EditorSetData(
            definition.OperationMapId,
            definition.SourceSceneReference.AssetGUID,
            scene.path,
            source.CanonicalSceneDependencyHash,
            source.ChunkSize,
            source.ContentHash,
            new List<StaticMapPresentationChunkEntry>(source.Chunks),
            new List<StaticMapPresentationSourceEntry>(source.Sources));
        return manifest;
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

    private sealed class FakeManifestApi : IOperationMapPresentationManifestApi
    {
        private readonly FakeManifestOperation operation;

        public FakeManifestApi(FakeManifestOperation operation)
        {
            this.operation = operation;
        }

        public IOperationMapPresentationManifestOperation Load(object runtimeKey) => operation;
    }

    private sealed class FakeManifestOperation : IOperationMapPresentationManifestOperation
    {
        public bool Done;
        public bool Success;
        public float Progress;
        public StaticMapPresentationManifest LoadedManifest;
        public string FailureMessage;
        public int DisposeCount;

        public bool IsDone => Done;
        public bool Succeeded => Success;
        public float Progress01 => Progress;
        public StaticMapPresentationManifest Manifest => LoadedManifest;
        public string Failure => FailureMessage;

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
