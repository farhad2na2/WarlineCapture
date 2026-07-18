using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Composition;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[TestFixture]
public sealed class StaticMapPresentationStreamerTests
{
    private readonly List<Object> _objects = new();

    [Test]
    public void DefaultConstructor_UsesRetainedAddressablesSceneApi()
    {
        StaticMapPresentationStreamer streamer = new();
        FieldInfo field = typeof(StaticMapPresentationStreamer).GetField(
            "_sceneApi",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        Assert.That(field.GetValue(streamer), Is.TypeOf<StaticMapPresentationAddressablesSceneApi>());
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(_objects[i]);
        _objects.Clear();
    }

    [Test]
    public void Bind_ValidManifestPreloadsViewportAndOneRingOneOperationPerUpdate()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(-1, 1, -1, 1);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);

        Assert.That(streamer.Bind(manifest, camera), Is.True);
        Assert.That(streamer.PendingOperationCount, Is.EqualTo(9));
        Assert.That(streamer.PreloadComplete, Is.False);

        for (int completed = 0; completed < 9; completed++)
        {
            streamer.Update();
            Assert.That(api.Started.Count, Is.EqualTo(completed + 1));
            streamer.Update();
            Assert.That(api.Started.Count, Is.EqualTo(completed + 1), "An in-flight operation must block another start.");
            api.Active.Complete(true);
        }
        CompleteUntilIdle(streamer, api);

        Assert.That(streamer.PreloadComplete, Is.True);
        Assert.That(streamer.Progress01, Is.EqualTo(1f));
        Assert.That(streamer.Status, Is.EqualTo("Streaming"));
        Assert.That(api.ManifestBindCount, Is.EqualTo(1));
    }

    [Test]
    public void Bind_ManifestSceneApiFailureFailsBeforeStartingChunkWork()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new() { ManifestBindFailure = "manifest address binding failed" };
        StaticMapPresentationStreamer streamer = new(api);

        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.False);
        Assert.That(streamer.Failed, Is.True);
        Assert.That(streamer.Status, Does.Contain("manifest address binding failed"));
        Assert.That(api.ManifestBindCount, Is.EqualTo(1));
        Assert.That(api.Started, Is.Empty);
    }

    [Test]
    public void Update_KeepsSecondRingThenUnloadsBeyondIt()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(-3, 3, 0, 0);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(manifest, camera), Is.True);
        CompleteUntilIdle(streamer, api);

        string negativeOne = ScenePath(-1, 0);
        camera.transform.position = new Vector3(48f, 100f, 16f);
        CompleteUntilIdle(streamer, api);
        Assert.That(api.Loaded, Does.Contain(negativeOne), "The second ring is the unload hysteresis band.");

        camera.transform.position = new Vector3(80f, 100f, 16f);
        CompleteUntilIdle(streamer, api);
        Assert.That(api.Loaded, Does.Not.Contain(negativeOne));
        Assert.That(api.Started, Has.Some.Matches<FakeSceneOperation>(operation =>
            operation.Path == negativeOne && operation.Kind == OperationKind.Unload));
    }

    [Test]
    public void Bind_NeverOwnsMoreThanSixtyFourQueuedOperations()
    {
        Camera camera = CreateTopDownCamera(1600f, 16f, 2000f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 99, 0, 0);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);

        Assert.That(streamer.Bind(manifest, camera), Is.True);
        Assert.That(streamer.PendingOperationCount, Is.LessThanOrEqualTo(64));
        for (int i = 0; i < 100; i++)
        {
            streamer.Update();
            Assert.That(streamer.LastSceneStateChecks, Is.LessThanOrEqualTo(16));
            Assert.That(streamer.PendingOperationCount, Is.LessThanOrEqualTo(64));
            api.Active?.Complete(true);
        }
    }

    [Test]
    public void CameraMove_InvalidatesReadinessAndRebuildsTargetsOnce()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 2, 0, 0), camera), Is.True);
        CompleteUntilIdle(streamer, api);
        Assert.That(streamer.PreloadComplete, Is.True);
        int initialRebuilds = streamer.TargetRebuildCount;

        camera.transform.position = new Vector3(48f, 100f, 16f);
        streamer.Update();

        Assert.That(streamer.PreloadComplete, Is.False);
        Assert.That(streamer.TargetRebuildCount, Is.EqualTo(initialRebuilds + 1));
    }

    [Test]
    public void Bind_PerspectiveCameraProjectsViewportFootprintToGround()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        camera.orthographic = false;
        camera.fieldOfView = 30f;
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);

        Assert.That(streamer.Bind(CreateGridManifest(-2, 2, -2, 2), camera), Is.True);
        CompleteUntilIdle(streamer, api);

        Assert.That(streamer.PreloadComplete, Is.True);
        Assert.That(api.Loaded, Does.Contain(ScenePath(-2, -2)));
        Assert.That(api.Loaded, Does.Contain(ScenePath(2, 2)));
    }

    [Test]
    public void Bind_DerivesGridOwnershipFromFirstSourceNotChunkBoundsSize()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        StaticMapPresentationManifest manifest = CreateManifest(new[]
        {
            ChunkData.At(0, 0, "near", ScenePath(0, 0), 0, 1, 10000f),
            ChunkData.At(5, 0, "far", ScenePath(5, 0), 1, 1, 10000f)
        });

        Assert.That(streamer.Bind(manifest, camera), Is.True);
        CompleteUntilIdle(streamer, api);

        Assert.That(api.Loaded, Does.Contain(ScenePath(0, 0)));
        Assert.That(api.Loaded, Does.Not.Contain(ScenePath(5, 0)));
    }

    [Test]
    public void CameraMove_ClearsStaleQueuedLoadsBeforeStartingWork()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 10, 0, 0), camera), Is.True);

        camera.transform.position = new Vector3(336f, 100f, 16f);
        streamer.Update();

        Assert.That(api.Active.Path, Is.EqualTo(ScenePath(9, 0)).Or.EqualTo(ScenePath(10, 0)));
        Assert.That(api.Started, Has.None.Matches<FakeSceneOperation>(operation =>
            operation.Path == ScenePath(0, 0) || operation.Path == ScenePath(1, 0)));
    }

    [Test]
    public void StableViewport_ReusesTargetsAndStatusWithoutPerFrameReplacement()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.True);
        CompleteUntilIdle(streamer, api);
        int rebuilds = streamer.TargetRebuildCount;
        string status = streamer.Status;

        for (int i = 0; i < 100; i++)
        {
            streamer.Update();
            Assert.That(streamer.LastSceneStateChecks, Is.LessThanOrEqualTo(2));
            Assert.That(streamer.TargetRebuildCount, Is.EqualTo(rebuilds));
            Assert.That(streamer.Status, Is.SameAs(status));
        }
    }

    [Test]
    public void CompositionRouteReversal_DrainsBeforeRebindingMatchPresentation()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 0, 0, 0);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        MenuBootstrapCompositionSystemHelper composition = new(streamer);
        GameObject matchObject = new("MatchSceneView");
        _objects.Add(matchObject);
        MatchSceneView matchView = matchObject.AddComponent<MatchSceneView>();
        SerializedObject serializedView = new(matchView);
        serializedView.FindProperty("worldCamera").objectReferenceValue = camera;
        serializedView.FindProperty("staticMapPresentationManifest").objectReferenceValue = manifest;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        composition.UpdateStaticMapPresentationForLoadedMatch(true, matchView);
        Assert.That(api.Active.Kind, Is.EqualTo(OperationKind.Load));
        api.Active.Complete(true);
        composition.UpdateStaticMapPresentationForLoadedMatch(true, matchView);
        Assert.That(streamer.PreloadComplete, Is.True);

        composition.UpdateStaticMapPresentationForLoadedMatch(false, matchView);
        Assert.That(api.Active.Kind, Is.EqualTo(OperationKind.Unload));
        api.Active.Complete(true);

        composition.UpdateStaticMapPresentationForLoadedMatch(true, matchView);
        Assert.That(streamer.IsDraining, Is.False);
        Assert.That(api.Active, Is.Null);
        composition.UpdateStaticMapPresentationForLoadedMatch(true, matchView);

        Assert.That(api.Active.Kind, Is.EqualTo(OperationKind.Load));
        Assert.That(api.Started, Has.Count.EqualTo(3));
    }

    [Test]
    public void BeginDrain_WaitsForInflightLoadThenUnloadsItsScene()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 0, 0, 0);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(manifest, camera), Is.True);
        streamer.Update();

        streamer.BeginDrain();
        Assert.That(streamer.DrainComplete, Is.False);
        api.Active.Complete(true);
        streamer.Update();

        Assert.That(api.Active.Kind, Is.EqualTo(OperationKind.Unload));
        api.Active.Complete(true);
        streamer.Update();
        Assert.That(streamer.DrainComplete, Is.True);
        Assert.That(streamer.Progress01, Is.EqualTo(1f));
        Assert.That(streamer.Status, Is.EqualTo("Drained"));
    }

    [Test]
    public void BeginDrain_ClearsPreloadReadiness()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        CompleteUntilIdle(streamer, api);
        Assert.That(streamer.PreloadComplete, Is.True);

        streamer.BeginDrain();

        Assert.That(streamer.PreloadComplete, Is.False);
        Assert.That(streamer.IsDraining, Is.True);
    }

    [Test]
    public void BeginDrain_UnloadsManifestScenesThatWereAlreadyLoaded()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 2, 0, 0);
        FakeSceneApi api = new();
        api.Loaded.UnionWith(new[] { ScenePath(0, 0), ScenePath(2, 0) });
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(manifest, camera), Is.True);

        streamer.BeginDrain();
        CompleteUntilIdle(streamer, api);

        Assert.That(streamer.DrainComplete, Is.True);
        Assert.That(api.Loaded, Is.Empty);
        Assert.That(api.Started.FindAll(item => item.Kind == OperationKind.Unload), Has.Count.EqualTo(2));
    }

    [Test]
    public void BeginDrain_WhenAlreadyDrainingPreservesQueueAndProgress()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 1, 0, 0);
        FakeSceneApi api = new();
        api.Loaded.UnionWith(new[] { ScenePath(0, 0), ScenePath(1, 0) });
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(manifest, camera), Is.True);
        streamer.BeginDrain();
        int pending = streamer.PendingOperationCount;
        float progress = streamer.Progress01;
        string status = streamer.Status;

        streamer.BeginDrain();

        Assert.That(streamer.PendingOperationCount, Is.EqualTo(pending));
        Assert.That(streamer.Progress01, Is.EqualTo(progress));
        Assert.That(streamer.Status, Is.SameAs(status));
    }

    [Test]
    public void Unbind_PreservesInflightOwnershipWhileResettingPublicState()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        streamer.Update();
        streamer.BeginDrain();

        streamer.Unbind();

        Assert.That(streamer.PreloadComplete, Is.False);
        Assert.That(streamer.DrainComplete, Is.False);
        Assert.That(streamer.Failed, Is.False);
        Assert.That(streamer.Progress01, Is.Zero);
        Assert.That(streamer.Status, Is.EqualTo("Unbound"));
        Assert.That(streamer.PendingOperationCount, Is.Zero);
        Assert.That(streamer.HasActiveOperation, Is.True);
        Assert.That(streamer.HasDetachedOperation, Is.True);
        Assert.That(api.Active, Is.Not.Null, "Unbind drops ownership without mutating the injected API operation.");
    }

    [Test]
    public void Bind_WaitsForDetachedOperationBeforeStartingReplacementWork()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        streamer.Update();
        streamer.Unbind();
        StaticMapPresentationManifest replacement = CreateManifest(new[]
        {
            ChunkData.At(0, 0, "replacement", "Assets/replacement.unity", 0, 1)
        });
        Camera camera = CreateTopDownCamera(16f, 16f);

        Assert.That(streamer.Bind(replacement, camera), Is.False);
        Assert.That(api.Started, Has.Count.EqualTo(1));
        api.Active.Complete(true);
        Assert.That(streamer.Bind(replacement, camera), Is.False);
        Assert.That(api.Active.Kind, Is.EqualTo(OperationKind.Unload));
        Assert.That(api.Active.Path, Is.EqualTo(ScenePath(0, 0)));
        api.Active.Complete(true);
        Assert.That(streamer.Bind(replacement, camera), Is.True);
        streamer.Update();

        Assert.That(api.Active.Path, Is.EqualTo("Assets/replacement.unity"));
        Assert.That(api.Started, Has.Count.EqualTo(3));
    }

    [Test]
    public void Update_CleansDetachedLoadWithoutAReplacementBind()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        streamer.Update();
        streamer.Unbind();
        api.Active.Complete(true);

        streamer.Update();
        Assert.That(api.Active.Kind, Is.EqualTo(OperationKind.Unload));
        api.Active.Complete(true);
        streamer.Update();

        Assert.That(streamer.HasActiveOperation, Is.False);
        Assert.That(api.Loaded, Is.Empty);
    }

    [Test]
    public void Update_FailsClosedWhenDetachedCleanupUnloadFailsTwice()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        streamer.Update();
        streamer.Unbind();
        api.Active.Complete(true);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        api.Active.Complete(false);

        streamer.Update();

        Assert.That(streamer.Failed, Is.True);
        Assert.That(streamer.Status, Does.Contain("failed twice"));
        Assert.That(streamer.HasActiveOperation, Is.True);
    }

    [Test]
    public void PostCompletionSceneStateMismatch_IsRetriedOnceAndCanRecover()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        Assert.That(api.Started, Has.Count.EqualTo(2));

        api.Active.Complete(true);
        streamer.Update();
        Assert.That(streamer.Failed, Is.False);
        Assert.That(streamer.PreloadComplete, Is.True);
    }

    [Test]
    public void NullOperationStart_IsRetriedOnceAndCanRecover()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new() { NullStartsRemaining = 1 };
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.True);

        streamer.Update();
        streamer.Update();
        Assert.That(api.StartAttempts, Is.EqualTo(2));
        api.Active.Complete(true);
        CompleteUntilIdle(streamer, api);
        Assert.That(streamer.Failed, Is.False);
    }

    [Test]
    public void ThrownOperationStart_TwiceFailsClosed()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new() { ThrowStartsRemaining = 2 };
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.True);

        streamer.Update();
        streamer.Update();

        Assert.That(api.StartAttempts, Is.EqualTo(2));
        Assert.That(streamer.Failed, Is.True);
        Assert.That(streamer.Status, Does.Contain("failed to start twice"));
    }

    [Test]
    public void FailedOperation_TwiceFailsClosed()
    {
        StaticMapPresentationStreamer streamer = CreateSingleChunkStreamer(out FakeSceneApi api);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();

        Assert.That(streamer.Failed, Is.True);
        Assert.That(streamer.PreloadComplete, Is.False);
        Assert.That(streamer.Status, Does.StartWith("Failed:"));
        Assert.That(api.Started, Has.Count.EqualTo(2));
    }

    [Test]
    public void PreloadFailure_StillAllowsPreviouslyLoadedChunksToDrain()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 1, 0, 0), camera), Is.True);

        streamer.Update();
        api.Active.Complete(true);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        Assert.That(streamer.Failed, Is.True);
        Assert.That(api.Loaded, Is.Not.Empty);

        streamer.BeginDrain();
        CompleteUntilIdle(streamer, api);

        Assert.That(streamer.DrainComplete, Is.True);
        Assert.That(api.Loaded, Is.Empty);
    }

    [Test]
    public void DrainFailure_TwiceStopsFurtherWorkAndBlocksCompletion()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        FakeSceneApi api = new();
        api.Loaded.Add(ScenePath(0, 0));
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.True);
        streamer.BeginDrain();

        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        api.Active.Complete(false);
        streamer.Update();
        int started = api.Started.Count;
        streamer.Update();

        Assert.That(streamer.Failed, Is.True);
        Assert.That(streamer.DrainComplete, Is.False);
        Assert.That(api.Started.Count, Is.EqualTo(started));
    }

    [Test]
    public void Bind_EditorProductionDefaultIsReadyNoOp()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 0, 0, 0);
        StaticMapPresentationStreamer streamer = new();

        Assert.That(streamer.Bind(manifest, camera), Is.True);
        Assert.That(streamer.PreloadComplete, Is.True);
        Assert.That(streamer.Status, Is.EqualTo("Disabled"));
        streamer.BeginDrain();
        Assert.That(streamer.DrainComplete, Is.True);
    }

    [Test]
    public void Bind_DisabledPlatformBypassesManifestAndCameraIndexing()
    {
        StaticMapPresentationStreamer streamer = new(sceneApi: null, enabledOverride: false);

        Assert.That(streamer.Bind(null, null), Is.True);
        Assert.That(streamer.PreloadComplete, Is.True);
        Assert.That(streamer.Status, Is.EqualTo("Disabled"));
    }

    [Test]
    public void Bind_RejectsMissingManifestOrCamera()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationManifest manifest = CreateGridManifest(0, 0, 0, 0);

        StaticMapPresentationStreamer missingManifest = new(new FakeSceneApi());
        Assert.That(missingManifest.Bind(null, camera), Is.False);
        Assert.That(missingManifest.Failed, Is.True);

        StaticMapPresentationStreamer missingCamera = new(new FakeSceneApi());
        Assert.That(missingCamera.Bind(manifest, null), Is.False);
        Assert.That(missingCamera.Failed, Is.True);
    }

    [Test]
    public void Bind_RejectsDuplicateCoordinatesPathsAndInvalidRanges()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);

        StaticMapPresentationStreamer duplicateCoordinate = new(new FakeSceneApi());
        Assert.That(duplicateCoordinate.Bind(CreateManifest(new[]
        {
            ChunkData.At(0, 0, "a", "Assets/a.unity", 0, 1),
            ChunkData.At(0, 0, "b", "Assets/b.unity", 1, 1)
        }), camera), Is.False);

        StaticMapPresentationStreamer duplicatePath = new(new FakeSceneApi());
        Assert.That(duplicatePath.Bind(CreateManifest(new[]
        {
            ChunkData.At(0, 0, "a", "Assets/same.unity", 0, 1),
            ChunkData.At(1, 0, "b", "Assets/same.unity", 1, 1)
        }), camera), Is.False);

        StaticMapPresentationStreamer invalidRange = new(new FakeSceneApi());
        Assert.That(invalidRange.Bind(CreateManifest(new[]
        {
            ChunkData.At(0, 0, "a", "Assets/a.unity", 1, 1)
        }), camera), Is.False);
    }

    [Test]
    public void Update_FailsClosedWhenViewportCannotReachGroundPlane()
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        StaticMapPresentationStreamer streamer = new(new FakeSceneApi());
        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.True);

        camera.transform.rotation = Quaternion.identity;
        streamer.Update();

        Assert.That(streamer.Failed, Is.True);
        Assert.That(streamer.PreloadComplete, Is.False);
    }

    private StaticMapPresentationStreamer CreateSingleChunkStreamer(out FakeSceneApi api)
    {
        Camera camera = CreateTopDownCamera(16f, 16f);
        api = new FakeSceneApi();
        StaticMapPresentationStreamer streamer = new(api);
        Assert.That(streamer.Bind(CreateGridManifest(0, 0, 0, 0), camera), Is.True);
        return streamer;
    }

    private static void CompleteUntilIdle(StaticMapPresentationStreamer streamer, FakeSceneApi api)
    {
        for (int guard = 0; guard < 1000; guard++)
        {
            streamer.Update();
            if (api.Active != null)
            {
                api.Active.Complete(true);
                continue;
            }
            if (streamer.PendingOperationCount == 0 &&
                (streamer.PreloadComplete || streamer.DrainComplete))
                return;
        }
        Assert.Fail("Streamer did not become idle.");
    }

    private Camera CreateTopDownCamera(float x, float z, float orthographicSize = 0.1f)
    {
        GameObject gameObject = new("StaticMapPresentationStreamerTests.Camera");
        _objects.Add(gameObject);
        Camera camera = gameObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.aspect = 1f;
        camera.transform.SetPositionAndRotation(new Vector3(x, 100f, z), Quaternion.Euler(90f, 0f, 0f));
        return camera;
    }

    private StaticMapPresentationManifest CreateGridManifest(int minX, int maxX, int minZ, int maxZ)
    {
        List<ChunkData> chunks = new();
        int sourceIndex = 0;
        for (int x = minX; x <= maxX; x++)
        for (int z = minZ; z <= maxZ; z++)
        {
            string id = $"chunk_{x}_{z}";
            chunks.Add(ChunkData.At(x, z, id, ScenePath(x, z), sourceIndex++, 1));
        }
        return CreateManifest(chunks);
    }

    private StaticMapPresentationManifest CreateManifest(IReadOnlyList<ChunkData> data)
    {
        StaticMapPresentationManifest manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        _objects.Add(manifest);
        List<StaticMapPresentationChunkEntry> chunks = new();
        List<StaticMapPresentationSourceEntry> sources = new();
        for (int i = 0; i < data.Count; i++)
        {
            ChunkData item = data[i];
            Vector3 center = new(item.X * 32f + 16f, 0f, item.Z * 32f + 16f);
            Bounds bounds = new(center, Vector3.one);
            chunks.Add(new StaticMapPresentationChunkEntry(
                item.Id, item.Path, new Bounds(center, Vector3.one * item.ChunkBoundsSize), item.Start, item.Count));
            sources.Add(new StaticMapPresentationSourceEntry(
                $"source-{i}", $"root/source-{i}", "hash", item.Id, $"visual-{i}", bounds,
                null, "mesh-guid", i, new List<StaticMapPresentationMaterialEntry>(), false));
        }
        manifest.EditorSetData("Assets/Match.unity", "hash", 32f, "content", chunks, sources);
        return manifest;
    }

    private static string ScenePath(int x, int z) => $"Assets/chunk_{x}_{z}.unity";

    private readonly struct ChunkData
    {
        public readonly int X;
        public readonly int Z;
        public readonly string Id;
        public readonly string Path;
        public readonly int Start;
        public readonly int Count;
        public readonly float ChunkBoundsSize;

        private ChunkData(int x, int z, string id, string path, int start, int count, float chunkBoundsSize)
        {
            X = x; Z = z; Id = id; Path = path; Start = start; Count = count;
            ChunkBoundsSize = chunkBoundsSize;
        }

        public static ChunkData At(
            int x, int z, string id, string path, int start, int count, float chunkBoundsSize = 1f) =>
            new(x, z, id, path, start, count, chunkBoundsSize);
    }

    private enum OperationKind
    {
        Load,
        Unload
    }

    private sealed class FakeSceneApi :
        IStaticMapPresentationSceneApi,
        IStaticMapPresentationManifestBindingSceneApi
    {
        public readonly HashSet<string> Loaded = new(StringComparer.Ordinal);
        public readonly List<FakeSceneOperation> Started = new();
        public FakeSceneOperation Active { get; private set; }
        public int NullStartsRemaining { get; set; }
        public int ThrowStartsRemaining { get; set; }
        public int StartAttempts { get; private set; }
        public int ManifestBindCount { get; private set; }
        public string ManifestBindFailure { get; set; }

        public bool TryBindManifest(StaticMapPresentationManifest manifest, out string error)
        {
            ManifestBindCount++;
            error = ManifestBindFailure;
            return string.IsNullOrEmpty(error);
        }

        public bool IsLoaded(string scenePath) => Loaded.Contains(scenePath);
        public IStaticMapPresentationSceneOperation LoadAdditive(string scenePath) =>
            Start(scenePath, OperationKind.Load);
        public IStaticMapPresentationSceneOperation Unload(string scenePath) =>
            Start(scenePath, OperationKind.Unload);

        private FakeSceneOperation Start(string path, OperationKind kind)
        {
            StartAttempts++;
            if (ThrowStartsRemaining > 0)
            {
                ThrowStartsRemaining--;
                throw new InvalidOperationException("Injected scene start failure.");
            }
            if (NullStartsRemaining > 0)
            {
                NullStartsRemaining--;
                return null;
            }
            Assert.That(Active, Is.Null, "The streamer started overlapping scene operations.");
            Active = new FakeSceneOperation(this, path, kind);
            Started.Add(Active);
            return Active;
        }

        public void Finish(FakeSceneOperation operation, bool succeeded)
        {
            Assert.That(operation, Is.SameAs(Active));
            if (succeeded)
            {
                if (operation.Kind == OperationKind.Load) Loaded.Add(operation.Path);
                else Loaded.Remove(operation.Path);
            }
            Active = null;
        }
    }

    private sealed class FakeSceneOperation : IStaticMapPresentationSceneOperation
    {
        private readonly FakeSceneApi _owner;

        public FakeSceneOperation(FakeSceneApi owner, string path, OperationKind kind)
        {
            _owner = owner;
            Path = path;
            Kind = kind;
        }

        public string Path { get; }
        public OperationKind Kind { get; }
        public bool IsDone { get; private set; }
        public bool Succeeded { get; private set; }
        public float Progress01 => IsDone ? 1f : 0.5f;

        public void Complete(bool succeeded)
        {
            Succeeded = succeeded;
            IsDone = true;
            _owner.Finish(this, succeeded);
        }
    }
}
