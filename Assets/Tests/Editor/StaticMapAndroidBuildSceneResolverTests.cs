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
    private const int CurrentMapChunkCount = 501;

    private const string Match = "Assets/Game/Scenes/Match.unity";
    private const string Menu = "Assets/Game/Scenes/MainMenu.unity";
    private const string ChunkA = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_n001_p002.unity";
    private const string ChunkB = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p000_p000.unity";
    private const string StaleChunk = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p999_p999.unity";
    private const string AlternateMapId = "opmap.test.alternate";
    private const string AlternateChunk = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate/Scenes/StaticMapPresentation_opmap_test_alternate_chunk_p000_p000.unity";
    private const string AlternateStaleChunk = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate/Scenes/StaticMapPresentation_opmap_test_alternate_chunk_p999_p999.unity";

    public static void RunCurrentProjectValidation()
    {
        try
        {
            var tests = new StaticMapAndroidBuildSceneResolverTests();
            tests.ResolveForCurrentProject_IncludesOnlyEnabledBaseScenesAfterValidatingManifestChunks();
            tests.BuildScript_UsesManifestResolverForBothAndroidBuildPipelinesOnly();
            tests.Resolve_CurrentSchemaWithoutMapIdentityFails();
            tests.Resolve_LegacySchemaOneWithoutMapIdentityRemainsReadable();
            Debug.Log("[StaticMapAndroidBuildSceneResolverValidation] result=Passed tests=4");
        }
        catch (Exception exception)
        {
            Debug.LogError("[StaticMapAndroidBuildSceneResolverValidation] result=Failed");
            Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void Resolve_CurrentSchemaWithoutMapIdentityFails()
    {
        StaticMapAndroidBuildManifestSnapshot snapshot = SnapshotWith(operationMapId: string.Empty);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            Resolve(new[] { Match }, snapshot));

        Assert.That(exception.Message, Does.Contain("identity is incomplete"));
    }

    [Test]
    public void Resolve_LegacySchemaOneWithoutMapIdentityRemainsReadable()
    {
        StaticMapAndroidBuildManifestSnapshot snapshot = SnapshotWith(
            schemaVersion: 1,
            operationMapId: string.Empty,
            canonicalSceneGuid: string.Empty);

        string[] result = Resolve(new[] { Match }, snapshot);

        CollectionAssert.AreEqual(new[] { Match }, result);
    }

    [Test]
    public void Resolve_PreservesBaseAndManifestOrderAndIncludesEveryPathOnce()
    {
        string[] result = Resolve(new[] { Menu, ChunkB, Match, Menu }, Snapshot(ChunkA, ChunkB));

        CollectionAssert.AreEqual(new[] { Menu, Match }, result);
    }

    [Test]
    public void Resolve_AcceptsSchemaOneCompatibilitySnapshot()
    {
        StaticMapAndroidBuildManifestSnapshot snapshot = SnapshotWith(
            schemaVersion: StaticMapPresentationManifest.MinimumReadableSchemaVersion);

        Assert.That(StaticMapPresentationManifest.MinimumReadableSchemaVersion, Is.EqualTo(1));
        CollectionAssert.AreEqual(
            new[] { Menu, Match },
            Resolve(new[] { Menu, Match }, snapshot));
    }

    [Test]
    public void Resolve_AcceptsAddressableCanonicalSceneOutsideBuildSettings()
    {
        CollectionAssert.AreEqual(
            new[] { Menu },
            Resolve(new[] { Menu }, Snapshot(ChunkA, ChunkB)));
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
    [TestCase("missingMatchFile")]
    [TestCase("missingBaseScene")]
    [TestCase("staleEnabled")]
    [TestCase("integrity")]
    public void Resolve_FailsClosedForInvalidInputs(string invalidCase)
    {
        StaticMapAndroidBuildManifestSnapshot snapshot = Snapshot(ChunkA, ChunkB);
        string[] enabled = { Menu, Match };
        Func<string, bool> exists = _ => true;
        Func<string> computeCanonicalDependencyHash = () => snapshot.CanonicalSceneDependencyHash;
        Func<string, IReadOnlyList<string>, bool> integrity = (_, __) => true;

        switch (invalidCase)
        {
            case "missing": snapshot = null; break;
            case "schema": snapshot = SnapshotWith(schemaVersion: StaticMapPresentationManifest.CurrentSchemaVersion + 1); break;
            case "empty": snapshot = SnapshotWith(chunks: Array.Empty<string>()); break;
            case "canonical":
                snapshot = SnapshotWith(canonical: "Assets/Game/Scenes/UnselectedMap.unity");
                exists = path => path != snapshot.CanonicalScenePath;
                break;
            case "contentHash": snapshot = SnapshotWith(contentHash: " "); break;
            case "canonicalDependencyHash": snapshot = SnapshotWith(canonicalDependencyHash: " "); break;
            case "staleCanonicalDependency": computeCanonicalDependencyHash = () => "actual-canonical-dependency-hash"; break;
            case "nullChunk": snapshot = Snapshot(ChunkA, null); break;
            case "duplicateChunk": snapshot = Snapshot(ChunkA, ChunkA); break;
            case "invalidChunk": snapshot = Snapshot(ChunkA, "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/invalid.unity"); break;
            case "nonOwnedChunk": snapshot = Snapshot(ChunkA, "Assets/Game/Scenes/Other.unity"); break;
            case "missingChunk": exists = path => path != ChunkB; break;
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
                computeCanonicalDependencyHash,
                integrity));
    }

    [Test]
    public void Resolve_StaleCanonicalDependencyReportsExpectedAndActualHashes()
    {
        StaticMapAndroidBuildManifestSnapshot snapshot = Snapshot(ChunkA);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            StaticMapAndroidBuildSceneResolver.Resolve(
                new[] { Match },
                snapshot,
                _ => true,
                IsOwned,
                () => "recomputed-hash",
                (_, __) => true));

        Assert.That(exception.Message, Does.Contain("Expected 'canonical-dependency-hash'"));
        Assert.That(exception.Message, Does.Contain("actual 'recomputed-hash'"));
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
    public void Resolve_CatalogSelectedManifestSetIsMapScopedAndRejectsUnownedChunks()
    {
        StaticMapAndroidBuildManifestSnapshot current = Snapshot(ChunkA, ChunkB);
        StaticMapAndroidBuildManifestSnapshot alternate = SnapshotWith(
            operationMapId: AlternateMapId,
            chunks: new[] { AlternateChunk });
        var validatedIntegrityOwners = new List<string>();

        string[] result = StaticMapAndroidBuildSceneResolver.Resolve(
            new[] { Match },
            new[] { current, alternate },
            _ => true,
            IsOwnedByMap,
            _ => "canonical-dependency-hash",
            (operationMapId, _, __) =>
            {
                validatedIntegrityOwners.Add(operationMapId);
                return true;
            });

        CollectionAssert.AreEqual(
            new[] { Match },
            result);
        CollectionAssert.AreEqual(
            new[] { "opmap.skirmish.desert_base_01", AlternateMapId },
            validatedIntegrityOwners);

        Assert.Throws<InvalidOperationException>(() =>
            StaticMapAndroidBuildSceneResolver.Resolve(
                new[] { Match },
                new[]
                {
                    current,
                    SnapshotWith(operationMapId: AlternateMapId, chunks: new[] { ChunkA })
                },
                _ => true,
                IsOwnedByMap,
                _ => "canonical-dependency-hash",
                (_, __, ___) => true));

        Assert.Throws<InvalidOperationException>(() =>
            StaticMapAndroidBuildSceneResolver.Resolve(
                new[] { Match, AlternateStaleChunk },
                new[] { current, alternate },
                _ => true,
                IsOwnedByMap,
                _ => "canonical-dependency-hash",
                (_, __, ___) => true));
    }

    [Test]
    public void ResolveForCurrentProject_IncludesOnlyEnabledBaseScenesAfterValidatingManifestChunks()
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

        CollectionAssert.AreEqual(expectedBaseScenes, result);
        Assert.AreEqual(result.Length, result.Distinct(StringComparer.Ordinal).Count());
        Assert.AreEqual(
            CurrentMapChunkCount,
            expectedChunks.Length,
            "The audited current-map manifest must include all generated chunks.");
        Assert.That(result, Has.None.Matches<string>(StaticMapPresentationOutputOwnership.IsOwnedScenePath));
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
            () => snapshot.CanonicalSceneDependencyHash,
            integrity ?? ((_, __) => true));
    }

    private static StaticMapAndroidBuildManifestSnapshot Snapshot(params string[] chunks)
    {
        return SnapshotWith(chunks: chunks);
    }

    private static StaticMapAndroidBuildManifestSnapshot SnapshotWith(
        int schemaVersion = StaticMapPresentationManifest.CurrentSchemaVersion,
        string operationMapId = "opmap.skirmish.desert_base_01",
        string canonicalSceneGuid = "canonical-scene-guid",
        string canonical = Match,
        string canonicalDependencyHash = "canonical-dependency-hash",
        string contentHash = "content-hash",
        IReadOnlyList<string> chunks = null)
    {
        return new StaticMapAndroidBuildManifestSnapshot(
            schemaVersion,
            operationMapId,
            canonicalSceneGuid,
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

    private static bool IsOwnedByMap(string operationMapId, string path)
    {
        if (string.Equals(operationMapId, "opmap.skirmish.desert_base_01", StringComparison.Ordinal))
            return IsOwned(path);
        return string.Equals(operationMapId, AlternateMapId, StringComparison.Ordinal) &&
               (string.Equals(path, AlternateChunk, StringComparison.Ordinal) ||
                string.Equals(path, AlternateStaleChunk, StringComparison.Ordinal));
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
