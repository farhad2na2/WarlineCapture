using System;
using System.Collections.Generic;
using Game.Components;
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
            Run(nameof(AbortReadyLoadReleasesExactlyOnce), test => test.AbortReadyLoadReleasesExactlyOnce(), ref passed);
            Run(nameof(FailedLoadCanResetAndRetry), test => test.FailedLoadCanResetAndRetry(), ref passed);
            Run(nameof(ReadyLoadCanResetBeforeSequentialLoad), test => test.ReadyLoadCanResetBeforeSequentialLoad(), ref passed);
            Run(nameof(UnloadWaitsForCompletionBeforeReleasingHandles), test => test.UnloadWaitsForCompletionBeforeReleasingHandles(), ref passed);
            Run(nameof(UnloadFailureReleasesHandlesAndRetainsFailure), test => test.UnloadFailureReleasesHandlesAndRetainsFailure(), ref passed);
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
        Assert.That(helper.FailureCode, Is.EqualTo(OperationMapLoadResultCode.MissingDefinition));
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
        Assert.That(helper.FailureCode, Is.EqualTo(OperationMapLoadResultCode.SourceLoadFailed));
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
        Assert.That(
            helper.FailureCode,
            Is.EqualTo(OperationMapLoadResultCode.PresentationPreloadFailed));
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
            LoadedManifest = CreateMismatchedManifest(),
            Progress = 1f
        };
        var helper = CreateHelper(sceneOperation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Does.Contain("does not match"));
        Assert.That(helper.FailureCode, Is.EqualTo(OperationMapLoadResultCode.StaleContent));
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(manifestOperation.LoadedManifest);
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

    [Test]
    public void AbortReadyLoadReleasesExactlyOnce()
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
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var helper = CreateHelper(sceneOperation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        helper.Abort("metadata bind failed");
        helper.Abort("duplicate abort");

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Is.EqualTo("metadata bind failed"));
        Assert.That(helper.FailureCode, Is.EqualTo(OperationMapLoadResultCode.Interrupted));
        Assert.That(helper.SceneView, Is.Null);
        Assert.That(helper.Manifest, Is.Null);
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(manifestOperation.LoadedManifest);
    }

    [Test]
    public void FailedLoadCanResetAndRetry()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var failedSceneOperation = new FakeSceneOperation
        {
            Done = true,
            FailureMessage = "source unavailable"
        };
        var failedManifestOperation = new FakeManifestOperation();
        var readySceneOperation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f
        };
        var readyManifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var helper = CreateHelper(
            new[] { failedSceneOperation, readySceneOperation },
            new[] { failedManifestOperation, readyManifestOperation });

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.TryReset(out error), Is.True, error);
        Assert.That(helper.TryStart(LoadDefinition(), out error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsReady, Is.True);
        Assert.That(helper.HasFailed, Is.False);
        Assert.That(failedSceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(failedManifestOperation.DisposeCount, Is.EqualTo(1));
        helper.Dispose();
        Assert.That(readySceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(readyManifestOperation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(readyManifestOperation.LoadedManifest);
    }

    [Test]
    public void ReadyLoadCanResetBeforeSequentialLoad()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var firstSceneOperation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f
        };
        var secondSceneOperation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f
        };
        var firstManifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var secondManifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var helper = CreateHelper(
            new[] { firstSceneOperation, secondSceneOperation },
            new[] { firstManifestOperation, secondManifestOperation });

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        Assert.That(helper.IsReady, Is.True);
        Assert.That(helper.TryReset(out error), Is.True, error);
        Assert.That(firstSceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(firstManifestOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(helper.TryStart(LoadDefinition(), out error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsReady, Is.True);
        helper.Dispose();
        Assert.That(secondSceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(secondManifestOperation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(firstManifestOperation.LoadedManifest);
        UnityEngine.Object.DestroyImmediate(secondManifestOperation.LoadedManifest);
    }

    [Test]
    public void UnloadWaitsForCompletionBeforeReleasingHandles()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var sceneOperation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f,
            UnloadSuccess = true
        };
        var manifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var helper = CreateHelper(sceneOperation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        Assert.That(helper.TryBeginUnload(out error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsUnloading, Is.True);
        Assert.That(helper.UnloadComplete, Is.False);
        Assert.That(sceneOperation.DisposeCount, Is.Zero);
        Assert.That(manifestOperation.DisposeCount, Is.Zero);

        sceneOperation.UnloadDoneState = true;
        helper.Update();

        Assert.That(helper.IsUnloading, Is.False);
        Assert.That(helper.UnloadComplete, Is.True);
        Assert.That(helper.Progress01, Is.EqualTo(1f));
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
        helper.Dispose();
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(manifestOperation.LoadedManifest);
    }

    [Test]
    public void UnloadFailureReleasesHandlesAndRetainsFailure()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var sceneOperation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f,
            UnloadDoneState = true,
            UnloadFailureMessage = "source unload failed"
        };
        var manifestOperation = new FakeManifestOperation
        {
            Done = true,
            Success = true,
            LoadedManifest = CreateMatchingManifest(scene),
            Progress = 1f
        };
        var helper = CreateHelper(sceneOperation, manifestOperation);

        Assert.That(helper.TryStart(LoadDefinition(), out string error), Is.True, error);
        helper.Update();
        Assert.That(helper.TryBeginUnload(out error), Is.True, error);
        helper.Update();

        Assert.That(helper.HasFailed, Is.True);
        Assert.That(helper.Failure, Is.EqualTo("source unload failed"));
        Assert.That(helper.FailureCode, Is.EqualTo(OperationMapLoadResultCode.SourceUnloadFailed));
        Assert.That(helper.UnloadComplete, Is.False);
        Assert.That(sceneOperation.DisposeCount, Is.EqualTo(1));
        Assert.That(manifestOperation.DisposeCount, Is.EqualTo(1));
        helper.Dispose();
        Assert.That(helper.Failure, Is.EqualTo("source unload failed"));
        UnityEngine.Object.DestroyImmediate(manifestOperation.LoadedManifest);
    }

    private static OperationMapSceneLoadingSceneSystemHelper CreateHelper(
        FakeSceneOperation sceneOperation,
        FakeManifestOperation manifestOperation) =>
        new(
            new FakeSceneApi(sceneOperation),
            new FakeManifestApi(manifestOperation));

    private static OperationMapSceneLoadingSceneSystemHelper CreateHelper(
        FakeSceneOperation[] sceneOperations,
        FakeManifestOperation[] manifestOperations) =>
        new(
            new FakeSceneApi(sceneOperations),
            new FakeManifestApi(manifestOperations));

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

    private static StaticMapPresentationManifest CreateMismatchedManifest()
    {
        OperationMapDefinition definition = LoadDefinition();
        StaticMapPresentationManifest source = LoadConfiguredManifest();
        StaticMapPresentationManifest manifest = UnityEngine.Object.Instantiate(source);
        const string mismatchedScenePath = "Assets/Game/Scenes/Match.unity";
        manifest.EditorSetData(
            definition.OperationMapId,
            AssetDatabase.AssetPathToGUID(mismatchedScenePath),
            mismatchedScenePath,
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
        private readonly FakeSceneOperation[] operations;
        private int nextOperation;

        public FakeSceneApi(params FakeSceneOperation[] operations)
        {
            this.operations = operations;
        }

        public IOperationMapSourceSceneOperation LoadAdditive(object runtimeKey)
        {
            return nextOperation < operations.Length ? operations[nextOperation++] : null;
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
        public bool UnloadDoneState;
        public bool UnloadSuccess;
        public float UnloadProgress;
        public string UnloadFailureMessage;
        public int BeginUnloadCount;

        public bool IsDone => Done;
        public bool Succeeded => Success;
        public float Progress01 => Progress;
        public Scene Scene => LoadedScene;
        public string Failure => FailureMessage;
        public bool UnloadStarted { get; private set; }
        public bool UnloadDone => UnloadStarted && UnloadDoneState;
        public bool UnloadSucceeded => UnloadDone && UnloadSuccess;
        public float UnloadProgress01 => UnloadProgress;
        public string UnloadFailure => UnloadFailureMessage;

        public bool TryBeginUnload(out string error)
        {
            BeginUnloadCount++;
            UnloadStarted = true;
            error = null;
            return true;
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }

    private sealed class FakeManifestApi : IOperationMapPresentationManifestApi
    {
        private readonly FakeManifestOperation[] operations;
        private int nextOperation;

        public FakeManifestApi(params FakeManifestOperation[] operations)
        {
            this.operations = operations;
        }

        public IOperationMapPresentationManifestOperation Load(object runtimeKey) =>
            nextOperation < operations.Length ? operations[nextOperation++] : null;
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
