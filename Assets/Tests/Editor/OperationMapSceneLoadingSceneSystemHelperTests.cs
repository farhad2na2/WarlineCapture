using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public sealed class OperationMapSceneLoadingSceneSystemHelperTests
{
    private const string DefinitionPath =
        "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
    private const string ScenePath =
        "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity";

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            Run(nameof(RejectsMissingDefinition), test => test.RejectsMissingDefinition(), ref passed);
            Run(nameof(PendingLoadPublishesProgress), test => test.PendingLoadPublishesProgress(), ref passed);
            Run(nameof(SuccessfulLoadResolvesValidatedStagedView), test => test.SuccessfulLoadResolvesValidatedStagedView(), ref passed);
            Run(nameof(EntitySceneLoadSkipsManifestAndResolvesWithoutStaticOwnership), test => test.EntitySceneLoadSkipsManifestAndResolvesWithoutStaticOwnership(), ref passed);
            Run(nameof(EntitySceneUnloadWaitsForOwnedMetadataRelease), test => test.EntitySceneUnloadWaitsForOwnedMetadataRelease(), ref passed);
            Run(nameof(EntitySceneLoadRejectsBoundStaticManifestReference), test => test.EntitySceneLoadRejectsBoundStaticManifestReference(), ref passed);
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
    public void EntitySceneLoadSkipsManifestAndResolvesWithoutStaticOwnership()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        OperationMapDefinition definition = CreateEntitySceneDefinition("opmap.skirmish.entity_scene_load");
        FakeManifestApi manifestApi = new(new FakeManifestOperation());
        OperationMapSceneView view = CreateEntitySceneView(scene, definition);
        var operation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f
        };
        FakeEntitySceneApi entitySceneApi = new();
        var helper = new OperationMapSceneLoadingSceneSystemHelper(
            new FakeSceneApi(operation),
            manifestApi,
            entitySceneApi: entitySceneApi);

        Assert.That(helper.TryStart(definition, out string error), Is.True, error);
        Assert.That(manifestApi.LoadCount, Is.EqualTo(0));
        helper.Update();

        Assert.That(helper.IsReady, Is.True, helper.Failure);
        Assert.That(helper.SceneView, Is.SameAs(view));
        Assert.That(helper.Manifest, Is.Null);
        Assert.That(entitySceneApi.EnsureReadyCount, Is.EqualTo(1));
        helper.Dispose();
        Assert.That(entitySceneApi.ReleaseOwnedCount, Is.EqualTo(1));
        Assert.That(operation.DisposeCount, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(definition);
    }

    [Test]
    public void EntitySceneLoadRejectsBoundStaticManifestReference()
    {
        OperationMapDefinition definition = CreateEntitySceneDefinition("opmap.skirmish.entity_scene_reject");
        Set(definition, "staticPresentationManifestReference", new AssetReference(new string('a', 32)));
        var helper = CreateHelper(new FakeSceneOperation(), new FakeManifestOperation());

        Assert.That(helper.TryStart(definition, out string error), Is.False);
        Assert.That(error, Does.Contain("must not bind a production static presentation manifest"));
        Assert.That(helper.FailureCode, Is.EqualTo(OperationMapLoadResultCode.StaleContent));
        UnityEngine.Object.DestroyImmediate(definition);
    }

    [Test]
    public void EntitySceneUnloadWaitsForOwnedMetadataRelease()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        OperationMapDefinition definition =
            CreateEntitySceneDefinition("opmap.skirmish.entity_scene_unload");
        OperationMapSceneView view = CreateEntitySceneView(scene, definition);
        var operation = new FakeSceneOperation
        {
            Done = true,
            Success = true,
            LoadedScene = scene,
            Progress = 1f,
            UnloadDoneState = true,
            UnloadSuccess = true
        };
        FakeEntitySceneApi entitySceneApi = new() { ReleaseComplete = false };
        var helper = new OperationMapSceneLoadingSceneSystemHelper(
            new FakeSceneApi(operation),
            new FakeManifestApi(new FakeManifestOperation()),
            entitySceneApi: entitySceneApi);

        Assert.That(helper.TryStart(definition, out string error), Is.True, error);
        helper.Update();
        Assert.That(helper.IsReady, Is.True, helper.Failure);
        Assert.That(helper.SceneView, Is.SameAs(view));
        Assert.That(helper.TryBeginUnload(out error), Is.True, error);
        helper.Update();

        Assert.That(helper.IsUnloading, Is.True);
        Assert.That(helper.UnloadComplete, Is.False);
        Assert.That(entitySceneApi.ReleaseOwnedCount, Is.EqualTo(1));
        Assert.That(operation.DisposeCount, Is.Zero);

        entitySceneApi.ReleaseComplete = true;
        helper.Update();

        Assert.That(helper.IsUnloading, Is.False);
        Assert.That(helper.UnloadComplete, Is.True);
        Assert.That(entitySceneApi.ReleaseOwnedCount, Is.EqualTo(1));
        Assert.That(operation.DisposeCount, Is.EqualTo(1));
        helper.Dispose();
        UnityEngine.Object.DestroyImmediate(definition);
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

    private static OperationMapDefinition CreateEntitySceneDefinition(string operationMapId)
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "operationMapId", operationMapId);
        Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
        Set(definition, "sourceSceneReference", new AssetReference(new string('b', 32)));
        Set(definition, "staticPresentationManifestReference", new AssetReference());
        return definition;
    }

    private static OperationMapSceneView CreateEntitySceneView(Scene scene, OperationMapDefinition definition)
    {
        GameObject viewObject = new("OperationMapSceneView");
        SceneManager.MoveGameObjectToScene(viewObject, scene);
        OperationMapSceneView view = viewObject.AddComponent<OperationMapSceneView>();
        Transform mapRoot = new GameObject("Map").transform;
        mapRoot.SetParent(viewObject.transform, false);
        CombinedMeshBaker decoration = new GameObject("Decoration").AddComponent<CombinedMeshBaker>();
        decoration.transform.SetParent(mapRoot, false);
        Transform buildings = new GameObject("Buildings").transform;
        buildings.SetParent(mapRoot, false);
        Transform vehicles = new GameObject("Vehicles").transform;
        vehicles.SetParent(mapRoot, false);
        MapSurfaceAuthoring surface = new GameObject("Surface").AddComponent<MapSurfaceAuthoring>();
        surface.transform.SetParent(viewObject.transform, false);
        GridAuthoringConfig grid = ScriptableObject.CreateInstance<GridAuthoringConfig>();
        SubScene subScene = viewObject.AddComponent<SubScene>();
        Set(
            definition,
            "navigationMetadata",
            new OperationMapNavigationMetadataConfig(
                subScene.SceneGUID.ToString(),
                0,
                0,
                false,
                false,
                false));

        SerializedObject serialized = new(view);
        serialized.FindProperty("operationMapId").stringValue = definition.OperationMapId;
        serialized.FindProperty("definition").objectReferenceValue = definition;
        serialized.FindProperty("canonicalPresentationMode").enumValueIndex =
            (int)OperationMapCanonicalPresentationMode.EntityScene;
        serialized.FindProperty("mapRoot").objectReferenceValue = mapRoot;
        serialized.FindProperty("decorationCombinedMeshBaker").objectReferenceValue = decoration;
        serialized.FindProperty("decorationRoot").objectReferenceValue = decoration.transform;
        serialized.FindProperty("buildingAuthoringRoot").objectReferenceValue = buildings;
        serialized.FindProperty("vehicleAuthoringRoot").objectReferenceValue = vehicles;
        serialized.FindProperty("mapSurfaceAuthoring").objectReferenceValue = surface;
        serialized.FindProperty("gridAuthoringConfig").objectReferenceValue = grid;
        serialized.FindProperty("mapSubScene").objectReferenceValue = subScene;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(definition, value);
    }

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
        OperationMapSceneView view = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
            .Single();
        StaticMapPresentationManifest source = LoadConfiguredManifest();
        StaticMapPresentationManifest manifest =
            UnityEngine.Object.Instantiate(source);
        manifest.EditorSetData(
            definition.OperationMapId,
            view.PresentationSourceSceneGuid,
            view.PresentationSourceScenePath,
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

        public int LoadCount { get; private set; }

        public IOperationMapPresentationManifestOperation Load(object runtimeKey)
        {
            LoadCount++;
            return nextOperation < operations.Length ? operations[nextOperation++] : null;
        }
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

    private sealed class FakeEntitySceneApi : IOperationMapEntitySceneApi
    {
        public int EnsureReadyCount { get; private set; }
        public int ReleaseOwnedCount { get; private set; }
        public bool ReleaseComplete { get; set; } = true;

        public bool TryEnsureReady(
            string sceneGuid,
            ref Entity sceneEntity,
            ref bool ownsScene,
            out bool ready,
            out string error)
        {
            EnsureReadyCount++;
            sceneEntity = new Entity { Index = 1, Version = 1 };
            ownsScene = true;
            ready = true;
            error = null;
            return true;
        }

        public bool TryReleaseOwned(
            ref Entity sceneEntity,
            ref bool ownsScene,
            ref bool releaseStarted,
            out bool complete,
            out string error)
        {
            error = null;
            if (!ownsScene)
            {
                complete = true;
                return true;
            }
            if (!releaseStarted)
            {
                releaseStarted = true;
                ReleaseOwnedCount++;
            }

            complete = ReleaseComplete;
            if (!complete)
                return true;

            sceneEntity = Entity.Null;
            ownsScene = false;
            releaseStarted = false;
            return true;
        }
    }
}
