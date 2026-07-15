using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Editor;
using Game.Rendering;
using NUnit.Framework;
using UnityEngine;

public sealed class StaticMapPresentationOutputOwnershipTests
{
    private const string SceneA = "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_n001_p002.unity";
    private const string SceneB = "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p000_p000.unity";
    private const string SceneC = "Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p012_n014.unity";

    [Test]
    public void ComputeStaleScenePaths_ReturnsOnlyPriorManifestOwnershipInOrdinalOrder()
    {
        string[] result = StaticMapPresentationOutputOwnership.ComputeStaleScenePaths(
            new[] { SceneC, SceneA, SceneB, SceneA },
            new[] { SceneB });

        Assert.That(result, Is.EqualTo(new[] { SceneA, SceneC }));
    }

    [TestCase("Assets/Game/Elsewhere/StaticMapPresentation_chunk_p000_p000.unity")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/Scenes/Nested/StaticMapPresentation_chunk_p000_p000.unity")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p000_p000.asset")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/Scenes/../StaticMapPresentation_chunk_p000_p000.unity")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_x000_p000.unity")]
    [TestCase("Assets\\Game\\GeneratedStaticMapPresentation\\Scenes\\StaticMapPresentation_chunk_p000_p000.unity")]
    public void InvalidOwnedPath_IsRejectedBeforeDeletion(string invalidPath)
    {
        Assert.That(StaticMapPresentationOutputOwnership.IsOwnedScenePath(invalidPath), Is.False);
        Assert.Throws<InvalidOperationException>(() =>
            StaticMapPresentationOutputOwnership.ComputeStaleScenePaths(new[] { invalidPath }, Array.Empty<string>()));
    }

    [Test]
    public void DeleteStaleSceneAssets_DeletesOwnedStaleSceneAndPreservesExpectedAndUnlistedSentinel()
    {
        const string sentinel = "Assets/Game/GeneratedStaticMapPresentation/Scenes/KEEP_ME.txt";
        HashSet<string> existing = new(StringComparer.Ordinal) { SceneA, SceneB, sentinel };
        List<string> deleteCalls = new();

        int deleted = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
            new[] { SceneA, SceneB },
            new[] { SceneB },
            existing.Contains,
            path =>
            {
                deleteCalls.Add(path);
                return existing.Remove(path);
            });

        Assert.That(deleted, Is.EqualTo(1));
        Assert.That(deleteCalls, Is.EqualTo(new[] { SceneA }));
        Assert.That(existing, Does.Contain(SceneB));
        Assert.That(existing, Does.Contain(sentinel));
    }

    [Test]
    public void DeleteStaleSceneAssets_MissingOwnedSceneIsAnIdempotentNoOp()
    {
        int deleted = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
            new[] { SceneA },
            Array.Empty<string>(),
            _ => false,
            _ => throw new AssertionException("Missing assets must not be passed to deletion."));

        Assert.That(deleted, Is.Zero);
    }

    [Test]
    public void DeleteStaleSceneAssets_RemovesPhysicalSceneWhenAssetDatabaseHasNoGuid()
    {
        HashSet<string> physicalFiles = new(StringComparer.Ordinal)
        {
            SceneA,
            SceneA + ".meta"
        };
        List<string> physicalDeleteCalls = new();

        int deleted = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
            new[] { SceneA },
            Array.Empty<string>(),
            _ => false,
            path => physicalFiles.Contains(path) || physicalFiles.Contains(path + ".meta"),
            _ => throw new AssertionException("Database deletion must not run without a database asset."),
            path =>
            {
                physicalDeleteCalls.Add(path);
                physicalFiles.Remove(path);
                physicalFiles.Remove(path + ".meta");
                return true;
            });

        Assert.That(deleted, Is.EqualTo(1));
        Assert.That(physicalDeleteCalls, Is.EqualTo(new[] { SceneA }));
        Assert.That(physicalFiles, Is.Empty);
    }

    [Test]
    public void CanReuseExpectedScenes_RequiresMatchingManifestAndEveryExpectedScene()
    {
        StaticMapPresentationManifest manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        try
        {
            manifest.EditorSetData(
                StaticMapPresentationBaker.CanonicalMatchScenePath,
                "canonical-hash",
                StaticMapPresentationBaker.ChunkSize,
                "content-hash",
                new List<StaticMapPresentationChunkEntry>
                {
                    new("chunk_n001_p002", SceneA, new Bounds(Vector3.zero, Vector3.one), 0, 1),
                    new("chunk_p000_p000", SceneB, new Bounds(Vector3.one, Vector3.one), 1, 1)
                },
                new List<StaticMapPresentationSourceEntry>());
            string[] ownedPaths = StaticMapPresentationOutputOwnership.CaptureOwnedScenePaths(manifest);

            Assert.That(
                StaticMapPresentationOutputOwnership.CanReuseExpectedScenes(
                    manifest.SchemaVersion,
                    manifest.CanonicalScenePath,
                    manifest.ChunkSize,
                    "content-hash",
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    StaticMapPresentationBaker.ChunkSize,
                    "content-hash",
                    ownedPaths,
                    new[] { SceneB, SceneA },
                    _ => true),
                Is.True);

            Assert.That(
                StaticMapPresentationOutputOwnership.CanReuseExpectedScenes(
                    manifest.SchemaVersion,
                    manifest.CanonicalScenePath,
                    manifest.ChunkSize,
                    "content-hash",
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    StaticMapPresentationBaker.ChunkSize,
                    "changed-content-hash",
                    ownedPaths,
                    new[] { SceneA, SceneB },
                    _ => true),
                Is.False);

            Assert.That(
                StaticMapPresentationOutputOwnership.CanReuseExpectedScenes(
                    manifest.SchemaVersion,
                    manifest.CanonicalScenePath,
                    manifest.ChunkSize,
                    "content-hash",
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    StaticMapPresentationBaker.ChunkSize,
                    "content-hash",
                    ownedPaths,
                    new[] { SceneA, SceneB },
                    path => !string.Equals(path, SceneB, StringComparison.Ordinal)),
                Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(manifest);
        }
    }

    [Test]
    public void CanReuseExpectedScenes_RejectsSceneWhoseIntegrityCheckFails()
    {
        bool reused = StaticMapPresentationOutputOwnership.CanReuseExpectedScenes(
            StaticMapPresentationManifest.CurrentSchemaVersion,
            StaticMapPresentationBaker.CanonicalMatchScenePath,
            StaticMapPresentationBaker.ChunkSize,
            "content-hash",
            StaticMapPresentationBaker.CanonicalMatchScenePath,
            StaticMapPresentationBaker.ChunkSize,
            "content-hash",
            new[] { SceneA },
            new[] { SceneA },
            _ => false,
            out string rejectionReason);

        Assert.That(reused, Is.False);
        Assert.That(rejectionReason, Is.EqualTo($"owned-scene-integrity-invalid:{SceneA}"));
    }

    [Test]
    public void BakeTransaction_RollsBackOverwritesDeletesAndNewFilesAfterFailure()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        string manifestPath = ProjectPath(projectRoot, StaticMapPresentationBaker.ManifestPath);
        string manifestMetaPath = manifestPath + ".meta";
        string sceneAPath = ProjectPath(projectRoot, SceneA);
        string sceneAMetaPath = sceneAPath + ".meta";
        string sceneBPath = ProjectPath(projectRoot, SceneB);
        string integrityPath = ProjectPath(projectRoot, StaticMapPresentationSceneIntegrity.IntegrityAssetPath);
        try
        {
            WriteFile(manifestPath, "manifest-before");
            WriteFile(manifestMetaPath, "manifest-meta-before");
            WriteFile(sceneAPath, "scene-a-before");
            WriteFile(sceneAMetaPath, "scene-a-meta-before");

            Assert.Throws<InvalidOperationException>(() =>
            {
                using StaticMapPresentationBakeTransaction transaction =
                    StaticMapPresentationBakeTransaction.Begin(
                        projectRoot,
                        new[]
                        {
                            StaticMapPresentationBaker.ManifestPath,
                            StaticMapPresentationSceneIntegrity.IntegrityAssetPath,
                            SceneA,
                            SceneB
                        });
                WriteFile(manifestPath, "manifest-after");
                File.Delete(sceneAPath);
                File.Delete(sceneAMetaPath);
                WriteFile(sceneBPath, "scene-b-created");
                WriteFile(sceneBPath + ".meta", "scene-b-meta-created");
                WriteFile(integrityPath, "integrity-created");
                WriteFile(integrityPath + ".meta", "integrity-meta-created");
                throw new InvalidOperationException("simulated bake failure");
            });

            Assert.That(File.ReadAllText(manifestPath), Is.EqualTo("manifest-before"));
            Assert.That(File.ReadAllText(manifestMetaPath), Is.EqualTo("manifest-meta-before"));
            Assert.That(File.ReadAllText(sceneAPath), Is.EqualTo("scene-a-before"));
            Assert.That(File.ReadAllText(sceneAMetaPath), Is.EqualTo("scene-a-meta-before"));
            Assert.That(File.Exists(sceneBPath), Is.False);
            Assert.That(File.Exists(sceneBPath + ".meta"), Is.False);
            Assert.That(File.Exists(integrityPath), Is.False);
            Assert.That(File.Exists(integrityPath + ".meta"), Is.False);
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void BakeTransaction_CommitKeepsCompletedGeneration()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        string manifestPath = ProjectPath(projectRoot, StaticMapPresentationBaker.ManifestPath);
        try
        {
            WriteFile(manifestPath, "before");
            using (StaticMapPresentationBakeTransaction transaction =
                   StaticMapPresentationBakeTransaction.Begin(
                       projectRoot,
                       new[] { StaticMapPresentationBaker.ManifestPath }))
            {
                WriteFile(manifestPath, "after");
                transaction.Commit();
            }

            Assert.That(File.ReadAllText(manifestPath), Is.EqualTo("after"));
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void SceneIntegrity_RejectsModifiedGeneratedSceneContent()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        string sceneAPath = ProjectPath(projectRoot, SceneA);
        string sceneBPath = ProjectPath(projectRoot, SceneB);
        try
        {
            WriteFile(sceneAPath, "scene-a");
            WriteFile(sceneAPath + ".meta", "scene-a-meta");
            WriteFile(sceneBPath, "scene-b");
            WriteFile(sceneBPath + ".meta", "scene-b-meta");
            StaticMapPresentationSceneIntegrity.Write(
                projectRoot,
                "content-hash",
                new[] { SceneB, SceneA });

            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    "content-hash",
                    new[] { SceneA, SceneB },
                    out StaticMapPresentationSceneIntegrity integrity,
                    out string initialReason),
                Is.True,
                initialReason);
            Assert.That(integrity.IsSceneFileValid(SceneA), Is.True);

            WriteFile(sceneBPath, "scene-b-corrupted");

            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    "content-hash",
                    new[] { SceneA, SceneB },
                    out _,
                    out string changedReason),
                Is.False);
            Assert.That(changedReason, Is.EqualTo($"integrity-scene-file-changed:{SceneB}"));

            WriteFile(sceneBPath, "scene-b");
            WriteFile(sceneAPath + ".meta", "scene-a-meta-corrupted");
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    "content-hash",
                    new[] { SceneA, SceneB },
                    out _,
                    out string metaChangedReason),
                Is.False);
            Assert.That(metaChangedReason, Is.EqualTo($"integrity-scene-file-changed:{SceneA}"));
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void SourceDependencyIdentity_ChangesWhenGameObjectLayerChanges()
    {
        StringBuilder layerZero = new("source");
        StringBuilder layerEight = new("source");

        StaticMapPresentationBaker.AppendGameObjectLayerIdentity(layerZero, 0);
        StaticMapPresentationBaker.AppendGameObjectLayerIdentity(layerEight, 8);

        Assert.That(layerZero.ToString(), Is.Not.EqualTo(layerEight.ToString()));
        Assert.That(layerEight.ToString(), Does.EndWith("|layer:8"));
    }

    [Test]
    public void CanonicalSourceGraph_StopsAtGeneratedNodeWithoutIncludingItsUniqueDependencies()
    {
        const string matchScene = "Assets/Game/Scenes/Match.unity";
        const string directSource = "Assets/Game/Art/DirectSource.mat";
        const string directSourceTexture = "Assets/Game/Textures/DirectSource.png";
        string generatedScene = SceneA;
        const string generatedOnlyDependency = "Assets/Game/Textures/GeneratedOnly.png";
        Dictionary<string, string[]> graph = new(StringComparer.Ordinal)
        {
            [matchScene] = new[] { generatedScene, directSource },
            [generatedScene] = new[] { generatedOnlyDependency },
            [directSource] = new[] { directSourceTexture },
            [directSourceTexture] = Array.Empty<string>(),
            [generatedOnlyDependency] = Array.Empty<string>()
        };
        bool generatedNodeTraversed = false;

        string[] sourcePaths = StaticMapPresentationCanonicalSourceHash.TraverseSourceDependencyGraph(
            matchScene,
            path =>
            {
                if (string.Equals(path, generatedScene, StringComparison.Ordinal))
                    generatedNodeTraversed = true;
                return graph[path];
            });

        Assert.That(
            sourcePaths,
            Is.EqualTo(new[] { directSource, matchScene, directSourceTexture }));
        Assert.That(generatedNodeTraversed, Is.False);
        Assert.That(sourcePaths, Does.Not.Contain(generatedScene));
        Assert.That(sourcePaths, Does.Not.Contain(generatedOnlyDependency));
    }

    [Test]
    public void CanonicalSourceHash_HashesOnlyProvidedFilteredFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Aph604-Canonical-" + Guid.NewGuid().ToString("N"));
        const string sourcePath = "Assets/Game/Scenes/Match.unity";
        string generatedPath = SceneA;
        string sourceFile = Path.Combine(root, "source.unity");
        string generatedFile = Path.Combine(root, "generated.unity");
        try
        {
            WriteFile(sourceFile, "source-v1");
            WriteFile(sourceFile + ".meta", "source-meta");
            WriteFile(generatedFile, "generated-v1");
            WriteFile(generatedFile + ".meta", "generated-meta");
            Dictionary<string, string> physicalPaths = new(StringComparer.Ordinal)
            {
                [sourcePath] = sourceFile,
                [generatedPath] = generatedFile
            };

            string initialHash = StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
                new[] { sourcePath, generatedPath },
                path => physicalPaths[path],
                _ => throw new AssertionException("Physical test assets must not use fallback identity."));
            WriteFile(generatedFile, "generated-v2");
            string generatedChangedHash = StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
                new[] { generatedPath, sourcePath },
                path => physicalPaths[path],
                _ => throw new AssertionException("Physical test assets must not use fallback identity."));
            WriteFile(sourceFile, "source-v2");
            string sourceChangedHash = StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
                new[] { sourcePath, generatedPath },
                path => physicalPaths[path],
                _ => throw new AssertionException("Physical test assets must not use fallback identity."));

            Assert.That(generatedChangedHash, Is.EqualTo(initialHash));
            Assert.That(sourceChangedHash, Is.Not.EqualTo(initialHash));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public void CanonicalSourceHash_NormalizesTextSourceAndMetaLineEndings()
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Build110-Text-" + Guid.NewGuid().ToString("N"));
        const string sourcePath = "Assets/Game/Scenes/Match.unity";
        string lfFile = Path.Combine(root, "lf.unity");
        string crlfFile = Path.Combine(root, "crlf.unity");
        string crFile = Path.Combine(root, "cr.unity");
        try
        {
            WriteBytes(lfFile, "scene: one\nline: two\n");
            WriteBytes(lfFile + ".meta", "guid: source\nversion: one\n");
            WriteBytes(crlfFile, "scene: one\r\nline: two\r\n");
            WriteBytes(crlfFile + ".meta", "guid: source\r\nversion: one\r\n");
            WriteBytes(crFile, "scene: one\rline: two\r");
            WriteBytes(crFile + ".meta", "guid: source\rversion: one\r");

            string lfHash = ComputeCanonicalHash(sourcePath, lfFile);

            Assert.That(ComputeCanonicalHash(sourcePath, crlfFile), Is.EqualTo(lfHash));
            Assert.That(ComputeCanonicalHash(sourcePath, crFile), Is.EqualTo(lfHash));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public void CanonicalSourceHash_LfTextMatchesLegacyRawByteContract()
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Build110-LfContract-" + Guid.NewGuid().ToString("N"));
        const string sourcePath = "Assets/Game/Scenes/Match.unity";
        string sourceFile = Path.Combine(root, "source.unity");
        try
        {
            WriteBytes(sourceFile, "scene: one\nline: two\n");
            WriteBytes(sourceFile + ".meta", "guid: source\nversion: one\n");

            Assert.That(
                ComputeCanonicalHash(sourcePath, sourceFile),
                Is.EqualTo(ComputeLegacyRawCanonicalHash(sourcePath, sourceFile)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [TestCase(".hlsl")]
    [TestCase(".glslinc")]
    [TestCase(".inputactions")]
    [TestCase(".md")]
    [TestCase(".rsp")]
    [TestCase(".tss")]
    [TestCase(".vfx")]
    [TestCase(".lighting")]
    [TestCase(".preset")]
    [TestCase(".physicMaterial")]
    [TestCase(".renderTexture")]
    [TestCase(".spriteatlas")]
    [TestCase(".fontsettings")]
    [TestCase(".customtext")]
    public void CanonicalSourceHash_NormalizesDetectedTextRegardlessOfExtension(string extension)
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Build110-Extension-" + Guid.NewGuid().ToString("N"));
        string sourcePath = "Assets/Game/Data/source" + extension;
        string lfFile = Path.Combine(root, "lf" + extension);
        string crlfFile = Path.Combine(root, "crlf" + extension);
        try
        {
            WriteBytes(lfFile, "first: value\nsecond: value\n");
            WriteBytes(lfFile + ".meta", "guid: source\n");
            WriteBytes(crlfFile, "first: value\r\nsecond: value\r\n");
            WriteBytes(crlfFile + ".meta", "guid: source\r\n");

            Assert.That(
                ComputeCanonicalHash(sourcePath, crlfFile),
                Is.EqualTo(ComputeCanonicalHash(sourcePath, lfFile)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public void CanonicalSourceHash_PreservesBinaryBytesIncludingLineEndingSequences()
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Build110-Binary-" + Guid.NewGuid().ToString("N"));
        const string sourcePath = "Assets/Game/Data/source.asset";
        string crlfFile = Path.Combine(root, "crlf.asset");
        string lfFile = Path.Combine(root, "lf.asset");
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(crlfFile, new byte[] { 0, 1, 13, 10, 2, 255 });
            WriteBytes(crlfFile + ".meta", "guid: source\n");
            File.WriteAllBytes(lfFile, new byte[] { 0, 1, 10, 2, 255 });
            WriteBytes(lfFile + ".meta", "guid: source\n");

            Assert.That(
                ComputeCanonicalHash(sourcePath, crlfFile),
                Is.Not.EqualTo(ComputeCanonicalHash(sourcePath, lfFile)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public void CanonicalSourceHash_PreservesValidUtf8BytesForKnownBinaryExtension()
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Build110-BinaryText-" + Guid.NewGuid().ToString("N"));
        const string sourcePath = "Assets/Game/Data/source.bytes";
        string crlfFile = Path.Combine(root, "crlf.bytes");
        string lfFile = Path.Combine(root, "lf.bytes");
        try
        {
            WriteBytes(crlfFile, "binary-header\r\nbinary-payload\r\n");
            WriteBytes(crlfFile + ".meta", "guid: source\n");
            WriteBytes(lfFile, "binary-header\nbinary-payload\n");
            WriteBytes(lfFile + ".meta", "guid: source\n");

            Assert.That(
                ComputeCanonicalHash(sourcePath, crlfFile),
                Is.Not.EqualTo(ComputeCanonicalHash(sourcePath, lfFile)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Test]
    public void CanonicalSourceHash_RemainsSensitiveToTextContentOutsideLineEndings()
    {
        string root = Path.Combine(Path.GetTempPath(), "WarlineCapture-Build110-Content-" + Guid.NewGuid().ToString("N"));
        const string sourcePath = "Assets/Game/Scenes/Match.unity";
        string firstFile = Path.Combine(root, "first.unity");
        string secondFile = Path.Combine(root, "second.unity");
        try
        {
            WriteBytes(firstFile, "scene: one\nline: two\n");
            WriteBytes(firstFile + ".meta", "guid: source\n");
            WriteBytes(secondFile, "scene: one\r\nline: changed\r\n");
            WriteBytes(secondFile + ".meta", "guid: source\r\n");

            Assert.That(
                ComputeCanonicalHash(sourcePath, firstFile),
                Is.Not.EqualTo(ComputeCanonicalHash(sourcePath, secondFile)));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [TestCase("Assets/Game/GeneratedStaticMapPresentation", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/Scenes/StaticMapPresentation_chunk_p000_p000.unity", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentationSibling/asset.asset", false)]
    [TestCase("Assets/Game/Scenes/Match.unity", false)]
    public void CanonicalSourceHash_ExcludesOnlyGeneratedPresentationOutput(string path, bool expected)
    {
        Assert.That(StaticMapPresentationCanonicalSourceHash.IsGeneratedOutputPath(path), Is.EqualTo(expected));
    }

    private static string CreateTemporaryProjectRoot()
    {
        string path = Path.Combine(Path.GetTempPath(), "WarlineCapture-Aph604-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string ProjectPath(string projectRoot, string assetPath)
    {
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void WriteFile(string path, string content)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, content);
    }

    private static void WriteBytes(string path, string content)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(content));
    }

    private static string ComputeCanonicalHash(string assetPath, string physicalPath)
    {
        return StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
            new[] { assetPath },
            _ => physicalPath,
            _ => throw new AssertionException("Physical test assets must not use fallback identity."));
    }

    private static string ComputeLegacyRawCanonicalHash(string assetPath, string physicalPath)
    {
        StringBuilder builder = new();
        builder.Append(assetPath).Append('|');
        AppendRawSha256(builder, physicalPath);
        builder.Append('|');
        AppendRawSha256(builder, physicalPath + ".meta");
        builder.Append(';');
        return Hash128.Compute(builder.ToString()).ToString();
    }

    private static void AppendRawSha256(StringBuilder builder, string path)
    {
        using SHA256 algorithm = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        byte[] hash = algorithm.ComputeHash(stream);
        for (int index = 0; index < hash.Length; index++)
            builder.Append(hash[index].ToString("x2"));
    }

    private static void DeleteTemporaryProjectRoot(string projectRoot)
    {
        if (Directory.Exists(projectRoot))
            Directory.Delete(projectRoot, true);
    }
}
