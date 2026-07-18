using System.Collections.Generic;
using Game.Composition;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;

public sealed class StaticMapPresentationAddressablesSceneApiTests
{
    private const string ManifestPath =
        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";

    [Test]
    public void BindLoadUnload_RetainsAndReleasesOneSceneOperation()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
        StaticMapPresentationChunkEntry chunk = manifest.Chunks[0];
        var backend = new FakeBackend();
        var api = new StaticMapPresentationAddressablesSceneApi(backend);

        Assert.That(api.TryBindManifest(manifest, out string error), Is.True, error);
        IStaticMapPresentationSceneOperation load = api.LoadAdditive(chunk.ScenePath);
        Assert.That(backend.LoadAddresses[0], Is.EqualTo(
            $"operation-map/{manifest.OperationMapId}/presentation/{chunk.ChunkId}"));
        Assert.That(api.RetainedSceneCount, Is.EqualTo(1));
        Assert.That(api.IsLoaded(chunk.ScenePath), Is.False);

        backend.LastLoad.Complete(true, true);
        Assert.That(load.IsDone, Is.True);
        Assert.That(api.IsLoaded(chunk.ScenePath), Is.True);

        IStaticMapPresentationSceneOperation unload = api.Unload(chunk.ScenePath);
        backend.LastUnload.Complete(true, false);
        Assert.That(unload.IsDone, Is.True);
        Assert.That(api.IsLoaded(chunk.ScenePath), Is.False);
        Assert.That(api.RetainedSceneCount, Is.Zero);
        Assert.That(backend.LastLoad.DisposeCount, Is.EqualTo(1));
        Assert.That(backend.LastUnload.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedLoad_IsReleasedBeforeSingleRetry()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
        string path = manifest.Chunks[0].ScenePath;
        var backend = new FakeBackend();
        var api = new StaticMapPresentationAddressablesSceneApi(backend);
        Assert.That(api.TryBindManifest(manifest, out string error), Is.True, error);

        api.LoadAdditive(path);
        FakeOperation failed = backend.LastLoad;
        failed.Complete(false, false);
        Assert.That(api.IsLoaded(path), Is.False);

        api.LoadAdditive(path);
        Assert.That(failed.DisposeCount, Is.EqualTo(1));
        Assert.That(backend.LoadAddresses, Has.Count.EqualTo(2));
        Assert.That(api.RetainedSceneCount, Is.EqualTo(1));
    }

    [Test]
    public void Bind_RejectsReplacementWhileSceneIsRetained()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
        var backend = new FakeBackend();
        var api = new StaticMapPresentationAddressablesSceneApi(backend);
        Assert.That(api.TryBindManifest(manifest, out string error), Is.True, error);
        api.LoadAdditive(manifest.Chunks[0].ScenePath);

        Assert.That(api.TryBindManifest(manifest, out error), Is.False);
        Assert.That(error, Does.Contain("drain"));
    }

    [Test]
    public void FailedUnload_KeepsLoadedSceneForRetry()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
        string path = manifest.Chunks[0].ScenePath;
        var backend = new FakeBackend();
        var api = new StaticMapPresentationAddressablesSceneApi(backend);
        Assert.That(api.TryBindManifest(manifest, out string error), Is.True, error);
        api.LoadAdditive(path);
        backend.LastLoad.Complete(true, true);

        IStaticMapPresentationSceneOperation failedUnload = api.Unload(path);
        FakeOperation firstUnload = backend.LastUnload;
        firstUnload.Complete(false, true);
        Assert.That(failedUnload.IsDone, Is.True);
        Assert.That(api.IsLoaded(path), Is.True);
        Assert.That(firstUnload.DisposeCount, Is.EqualTo(1));

        Assert.That(api.Unload(path), Is.Not.Null);
        Assert.That(backend.UnloadCount, Is.EqualTo(2));
    }

    private static StaticMapPresentationManifest LoadManifest()
    {
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
        Assert.That(manifest, Is.Not.Null);
        return manifest;
    }

    private sealed class FakeBackend : IStaticMapPresentationAddressablesSceneBackend
    {
        internal readonly List<string> LoadAddresses = new();
        internal FakeOperation LastLoad;
        internal FakeOperation LastUnload;
        internal int UnloadCount;

        public IStaticMapPresentationAddressablesSceneOperation LoadAdditive(string address)
        {
            LoadAddresses.Add(address);
            LastLoad = new FakeOperation();
            return LastLoad;
        }

        public IStaticMapPresentationAddressablesSceneOperation Unload(
            IStaticMapPresentationAddressablesSceneOperation loadOperation)
        {
            UnloadCount++;
            LastUnload = new FakeOperation();
            return LastUnload;
        }
    }

    private sealed class FakeOperation : IStaticMapPresentationAddressablesSceneOperation
    {
        private bool done;
        private bool succeeded;
        private bool sceneLoaded;

        internal int DisposeCount { get; private set; }
        public bool IsDone => done;
        public bool Succeeded => succeeded;
        public bool SceneLoaded => sceneLoaded;
        public float Progress01 => done ? 1f : 0.5f;

        internal void Complete(bool success, bool loaded)
        {
            done = true;
            succeeded = success;
            sceneLoaded = loaded;
        }

        public void Dispose()
        {
            DisposeCount++;
        }
    }
}
