using System.Collections;
using Game.Composition;
using Game.Rendering;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

public sealed class StaticMapPresentationAddressablesSceneApiPlayModeTests
{
    private const string ManifestAddress =
        "operation-map/opmap.skirmish.desert_base_01/static-manifest";
    private const int MaximumFrames = 900;

    [UnityTest]
    public IEnumerator OneChunk_LoadsAndUnloadsThroughRetainedAddressablesHandles()
    {
        AsyncOperationHandle<StaticMapPresentationManifest> manifestHandle =
            Addressables.LoadAssetAsync<StaticMapPresentationManifest>(ManifestAddress);
        yield return manifestHandle;
        Assert.That(manifestHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
            manifestHandle.OperationException?.Message);

        var api = new StaticMapPresentationAddressablesSceneApi();
        try
        {
            StaticMapPresentationManifest manifest = manifestHandle.Result;
            Assert.That(api.TryBindManifest(manifest, out string error), Is.True, error);
            string scenePath = manifest.Chunks[0].ScenePath;

            IStaticMapPresentationSceneOperation load = api.LoadAdditive(scenePath);
            yield return WaitFor(load);
            Assert.That(api.IsLoaded(scenePath), Is.True);
            Assert.That(api.RetainedSceneCount, Is.EqualTo(1));

            IStaticMapPresentationSceneOperation unload = api.Unload(scenePath);
            yield return WaitFor(unload);
            Assert.That(api.IsLoaded(scenePath), Is.False);
            Assert.That(api.RetainedSceneCount, Is.Zero);
        }
        finally
        {
            if (manifestHandle.IsValid())
                Addressables.Release(manifestHandle);
        }
    }

    private static IEnumerator WaitFor(IStaticMapPresentationSceneOperation operation)
    {
        Assert.That(operation, Is.Not.Null);
        int frames = 0;
        while (!operation.IsDone && frames++ < MaximumFrames)
            yield return null;
        Assert.That(operation.IsDone, Is.True, "Addressables scene operation timed out.");
    }
}
