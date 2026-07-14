using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor;
using Game.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class StaticMapAndroidBuildSceneResolverTests
{
    private const string Match = "Assets/Game/Scenes/Match.unity";
    private const string Menu = "Assets/Game/Scenes/MainMenu.unity";
    private const string ChunkA = "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_n001_p002.unity";
    private const string ChunkB = "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p000_p000.unity";
    private const string StaleChunk = "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p999_p999.unity";

    [Test]
    public void Resolve_PreservesBaseAndManifestOrderAndIncludesEveryPathOnce()
    {
        string[] result = Resolve(new[] { Menu, ChunkB, Match, Menu }, Snapshot(ChunkA, ChunkB));

        CollectionAssert.AreEqual(new[] { Menu, Match, ChunkA, ChunkB }, result);
    }

    [TestCase("missing")]
    [TestCase("schema")]
    [TestCase("empty")]
    [TestCase("canonical")]
    [TestCase("contentHash")]
    [TestCase("canonicalDependencyHash")]
    [TestCase("staleCanonicalDependency")]
    [TestCase("nullChunk")]
    [TestCase("duplicateChunk")]
    [TestCase("invalidChunk")]
    [TestCase("nonOwnedChunk")]
    [TestCase("missingChunk")]
    [TestCase("missingMatch")]
    [TestCase("missingMatchFile")]
    [TestCase("missingBaseScene")]
    [TestCase("staleEnabled")]
    [TestCase("integrity")]
    public void Resolve_FailsClosedForInvalidInputs(string invalidCase)
    {
        StaticMapAndroidBuildManifestSnapshot snapshot = Snapshot(ChunkA, ChunkB);
        string[] enabled = { Menu, Match };
        Func<string, bool> exists = _ => true;
        Func<string, bool> canonicalDependencyMatches = _ => true;
        Func<string, IReadOnlyList<string>, bool> integrity = (_, __) => true;

        switch (invalidCase)
        {
            case "missing": snapshot = null; break;
            case "schema": snapshot = SnapshotWith(schemaVersion: StaticMapPresentationManifest.CurrentSchemaVersion + 1); break;
            case "empty": snapshot = SnapshotWith(chunks: Array.Empty<string>()); break;
            case "canonical": snapshot = SnapshotWith(canonical: Menu); break;
            case "contentHash": snapshot = SnapshotWith(contentHash: " "); break;
            case "canonicalDependencyHash": snapshot = SnapshotWith(canonicalDependencyHash: " "); break;
            case "staleCanonicalDependency": canonicalDependencyMatches = _ => false; break;
            case "nullChunk": snapshot = Snapshot(ChunkA, null); break;
            case "duplicateChunk": snapshot = Snapshot(ChunkA, ChunkA); break;
            case "invalidChunk": snapshot = Snapshot(ChunkA, "Assets/Game/GeneratedStaticMapPresentation/Scenes/invalid.unity"); break;
            case "nonOwnedChunk": snapshot = Snapshot(ChunkA, "Assets/Game/Scenes/Other.unity"); break;
            case "missingChunk": exists = path => path != ChunkB; break;
            case "missingMatch": enabled = new[] { Menu }; break;
            case "missingMatchFile": exists = path => path != Match; break;
            case "missingBaseScene": exists = path => path != Menu; break;
            case "staleEnabled": enabled = new[] { Menu, Match, StaleChunk }; break;
            case "integrity": integrity = (_, __) => false; break;
            default: Assert.Fail($"Unknown test case: {invalidCase}"); break;
        }

        Assert.Throws<InvalidOperationException>(() =>
            StaticMapAndroidBuildSceneResolver.Resolve(
                enabled,
                snapshot,
                exists,
                IsOwned,
                canonicalDependencyMatches,
                integrity));
    }

    [Test]
    public void Resolve_PassesManifestOrderAndContentHashToIntegrityDelegate()
    {
        string capturedHash = null;
        IReadOnlyList<string> capturedPaths = null;

        Resolve(
            new[] { Match },
            Snapshot(ChunkB, ChunkA),
            (hash, paths) =>
            {
                capturedHash = hash;
                capturedPaths = paths;
                return true;
            });

        Assert.That(capturedHash, Is.EqualTo("content-hash"));
        CollectionAssert.AreEqual(new[] { ChunkB, ChunkA }, capturedPaths);
    }

    [Test]
    public void ResolveForCurrentProject_IncludesEnabledBaseScenesThenEveryManifestChunkExactlyOnce()
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        StaticMapPresentationManifest manifest =
            AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(
                StaticMapPresentationBaker.ManifestPath);

        Assert.NotNull(manifest, "Generated static-map presentation manifest is missing.");
        string[] expectedBaseScenes = enabledScenes
            .Where(path => !StaticMapPresentationOutputOwnership.IsOwnedScenePath(path))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] expectedChunks = manifest.Chunks.Select(chunk => chunk.ScenePath).ToArray();

        string[] result = StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject(enabledScenes);

        CollectionAssert.AreEqual(expectedBaseScenes.Concat(expectedChunks).ToArray(), result);
        Assert.AreEqual(result.Length, result.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(514, expectedChunks.Length, "The audited current-map manifest must include all generated chunks.");
    }

    [Test]
    public void BuildScript_UsesManifestResolverForBothAndroidBuildPipelinesOnly()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string source = File.ReadAllText(Path.Combine(
            projectRoot,
            "Assets/Game/Scripts/Editor/BuildScript.cs"));
        const string resolverCall =
            "StaticMapAndroidBuildSceneResolver.ResolveForCurrentProject(GetEnabledScenes())";

        Assert.AreEqual(2, CountOccurrences(source, resolverCall));
    }

    private static string[] Resolve(
        IEnumerable<string> enabled,
        StaticMapAndroidBuildManifestSnapshot snapshot,
        Func<string, IReadOnlyList<string>, bool> integrity = null)
    {
        return StaticMapAndroidBuildSceneResolver.Resolve(
            enabled,
            snapshot,
            _ => true,
            IsOwned,
            _ => true,
            integrity ?? ((_, __) => true));
    }

    private static StaticMapAndroidBuildManifestSnapshot Snapshot(params string[] chunks)
    {
        return SnapshotWith(chunks: chunks);
    }

    private static StaticMapAndroidBuildManifestSnapshot SnapshotWith(
        int schemaVersion = StaticMapPresentationManifest.CurrentSchemaVersion,
        string canonical = Match,
        string canonicalDependencyHash = "canonical-dependency-hash",
        string contentHash = "content-hash",
        IReadOnlyList<string> chunks = null)
    {
        return new StaticMapAndroidBuildManifestSnapshot(
            schemaVersion,
            canonical,
            canonicalDependencyHash,
            contentHash,
            chunks ?? new[] { ChunkA, ChunkB });
    }

    private static bool IsOwned(string path)
    {
        return string.Equals(path, ChunkA, StringComparison.Ordinal) ||
               string.Equals(path, ChunkB, StringComparison.Ordinal) ||
               string.Equals(path, StaleChunk, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }
}
