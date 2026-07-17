using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class StaticMapPresentationManifestSchemaCompatibilityTests
{
    private const string ManifestPath =
        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new StaticMapPresentationManifestSchemaCompatibilityTests();
            tests.SchemaOne_RemainsReadableWithoutMapIdentity();
            tests.CurrentSchemaManifest_HasStableMapAndSceneIdentity();
            tests.LegacyManifest_MigratesExplicitlyToCurrentSchema();
            tests.CurrentSchema_RejectsIncompleteIdentity();
            tests.CurrentManifest_BuildsRuntimeChunkIndex();
            Debug.Log("[StaticMapPresentationManifestSchemaValidation] result=Passed tests=5");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void SchemaOne_RemainsReadableWithoutMapIdentity()
    {
        Assert.That(StaticMapPresentationManifest.MinimumReadableSchemaVersion, Is.EqualTo(1));
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(1), Is.True);
        Assert.That(StaticMapPresentationManifest.HasRequiredIdentity(
            1,
            string.Empty,
            string.Empty,
            "Assets/Game/Scenes/Match.unity"), Is.True);
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(
            StaticMapPresentationManifest.CurrentSchemaVersion), Is.True);
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(0), Is.False);
        Assert.That(StaticMapPresentationManifest.IsSchemaReadable(
            StaticMapPresentationManifest.CurrentSchemaVersion + 1), Is.False);
    }

    [Test]
    public void CurrentSchemaManifest_HasStableMapAndSceneIdentity()
    {
        StaticMapPresentationManifest manifest = LoadManifest();

        Assert.That(manifest.SchemaVersion, Is.EqualTo(2));
        Assert.That(manifest.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
        Assert.That(manifest.CanonicalSceneGuid, Is.Not.Empty);
        Assert.That(AssetDatabase.GUIDToAssetPath(manifest.CanonicalSceneGuid),
            Is.EqualTo(manifest.CanonicalScenePath));
        Assert.That(StaticMapPresentationManifest.HasRequiredIdentity(
            manifest.SchemaVersion,
            manifest.OperationMapId,
            manifest.CanonicalSceneGuid,
            manifest.CanonicalScenePath), Is.True);
    }

    [Test]
    public void LegacyManifest_MigratesExplicitlyToCurrentSchema()
    {
        StaticMapPresentationManifest manifest =
            ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        try
        {
#pragma warning disable CS0618
            manifest.EditorSetData(
                "Assets/Game/Scenes/Match.unity",
                "legacy-dependency",
                32f,
                "legacy-content",
                new List<StaticMapPresentationChunkEntry>(),
                new List<StaticMapPresentationSourceEntry>());
#pragma warning restore CS0618
            Assert.That(manifest.SchemaVersion, Is.EqualTo(1));
            Assert.That(manifest.OperationMapId, Is.Empty);
            Assert.That(manifest.CanonicalSceneGuid, Is.Empty);

            manifest.EditorSetData(
                "opmap.skirmish.desert_base_01",
                "scene-guid",
                "Assets/Game/Scenes/Match.unity",
                "current-dependency",
                32f,
                "current-content",
                new List<StaticMapPresentationChunkEntry>(),
                new List<StaticMapPresentationSourceEntry>());

            Assert.That(manifest.SchemaVersion,
                Is.EqualTo(StaticMapPresentationManifest.CurrentSchemaVersion));
            Assert.That(manifest.OperationMapId, Is.EqualTo("opmap.skirmish.desert_base_01"));
            Assert.That(manifest.CanonicalSceneGuid, Is.EqualTo("scene-guid"));
        }
        finally
        {
            Object.DestroyImmediate(manifest);
        }
    }

    [Test]
    public void CurrentSchema_RejectsIncompleteIdentity()
    {
        Assert.That(StaticMapPresentationManifest.HasRequiredIdentity(
            StaticMapPresentationManifest.CurrentSchemaVersion,
            string.Empty,
            "scene-guid",
            "Assets/Game/Scenes/Match.unity"), Is.False);
        Assert.That(StaticMapPresentationManifest.HasRequiredIdentity(
            StaticMapPresentationManifest.CurrentSchemaVersion,
            "opmap.skirmish.desert_base_01",
            string.Empty,
            "Assets/Game/Scenes/Match.unity"), Is.False);
    }

    [Test]
    public void CurrentManifest_BuildsRuntimeChunkIndex()
    {
        StaticMapPresentationManifest manifest = LoadManifest();
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

    private static StaticMapPresentationManifest LoadManifest()
    {
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
        Assert.That(manifest, Is.Not.Null);
        return manifest;
    }
}
