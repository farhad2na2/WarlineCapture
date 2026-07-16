using Game.Composition;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class StaticMapPresentationManifestSchemaCompatibilityTests
{
    private const string ManifestPath =
        "Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset";

    [Test]
    public void SchemaOne_RemainsReadableAndCurrentManifestUsesIt()
    {
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);

        Assert.That(manifest, Is.Not.Null);
        Assert.That(StaticMapPresentationManifest.MinimumReadableSchemaVersion, Is.EqualTo(1));
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(1), Is.True);
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(
            StaticMapPresentationManifest.CurrentSchemaVersion), Is.True);
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(0), Is.False);
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(
            StaticMapPresentationManifest.CurrentSchemaVersion + 1), Is.False);
        Assert.That(manifest.SchemaVersion, Is.EqualTo(1));
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(manifest.SchemaVersion), Is.True);
    }

    [Test]
    public void CurrentSchemaOneManifest_BuildsRuntimeChunkIndex()
    {
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
        GameObject cameraObject = new("SchemaOneCompatibilityCamera");
        try
        {
            Camera camera = cameraObject.AddComponent<Camera>();
            Assert.That(StaticMapPresentationManifestIndex.TryCreate(
                manifest,
                camera,
                out StaticMapPresentationChunk[] chunks,
                out float chunkSize,
                out string error), Is.True, error);
            Assert.That(chunks.Length, Is.EqualTo(514));
            Assert.That(chunkSize, Is.EqualTo(32f));
        }
        finally
        {
            Object.DestroyImmediate(cameraObject);
        }
    }
}
