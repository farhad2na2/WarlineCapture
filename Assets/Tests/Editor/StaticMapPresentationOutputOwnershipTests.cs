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
    private const string SceneA = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_n001_p002.unity";
    private const string SceneB = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p000_p000.unity";
    private const string SceneC = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p012_n014.unity";
    private const string AlternateMapId = "opmap.test.alternate";
    private const string AlternateOutputRoot =
        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate";
    private const string AlternateSceneA =
        AlternateOutputRoot + "/Scenes/StaticMapPresentation_opmap_test_alternate_chunk_p000_p000.unity";
    private const string AlternateManifestPath =
        AlternateOutputRoot + "/StaticMapPresentationManifest.asset";
    private const string AlternateIntegrityPath =
        AlternateOutputRoot + "/StaticMapPresentationSceneIntegrity.json";

    [Test]
    public void ComputeStaleScenePaths_ReturnsOnlyPriorManifestOwnershipInOrdinalOrder()
    {
        string[] result = StaticMapPresentationOutputOwnership.ComputeStaleScenePaths(
            new[] { SceneC, SceneA, SceneB, SceneA },
            new[] { SceneB });

        Assert.That(result, Is.EqualTo(new[] { SceneA, SceneC }));
    }

    [TestCase("Assets/Game/Elsewhere/StaticMapPresentation_chunk_p000_p000.unity")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/Nested/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p000_p000.unity")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p000_p000.asset")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/../StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p000_p000.unity")]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_x000_p000.unity")]
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
        const string sentinel = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/KEEP_ME.txt";
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
    public void DeleteStaleSceneAssets_DeletesOnlyScenesOwnedByTheSuppliedMapManifest()
    {
        StaticMapPresentationManifest manifest = CreateManifest(
            AlternateMapId,
            AlternateSceneA);
        HashSet<string> existing = new(StringComparer.Ordinal) { SceneA, AlternateSceneA };
        List<string> deleted = new();
        try
        {
            int deletedCount = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
                AlternateMapId,
                AlternateOutputRoot,
                manifest,
                Array.Empty<string>(),
                existing.Contains,
                _ => false,
                path =>
                {
                    deleted.Add(path);
                    return existing.Remove(path);
                },
                _ => false);

            Assert.That(deletedCount, Is.EqualTo(1));
            Assert.That(deleted, Is.EqualTo(new[] { AlternateSceneA }));
            Assert.That(existing, Does.Contain(SceneA));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(manifest);
        }
    }

    [Test]
    public void DeleteStaleSceneAssets_RejectsManifestOwnedByAnotherMap()
    {
        StaticMapPresentationManifest manifest = CreateManifest(
            StaticMapPresentationBaker.CurrentOperationMapId,
            SceneA);
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
                    AlternateMapId,
                    AlternateOutputRoot,
                    manifest,
                    Array.Empty<string>(),
                    _ => true,
                    _ => false,
                    _ => throw new AssertionException("Cross-map deletion must not run."),
                    _ => false));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(manifest);
        }
    }

    [Test]
    public void CanReuseExpectedScenes_RequiresMatchingManifestAndEveryExpectedScene()
    {
        StaticMapPresentationManifest manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        try
        {
            manifest.EditorSetData(
                StaticMapPresentationBaker.CurrentOperationMapId,
                "00000000000000000000000000000001",
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
    public void IdenticalSecondBake_IsNoOpForEachMapOwner()
    {
        AssertIdenticalSecondBakeIsNoOp(
            StaticMapPresentationBaker.CurrentOperationMapId,
            StaticMapPresentationBaker.OutputRoot,
            SceneA);
        AssertIdenticalSecondBakeIsNoOp(
            AlternateMapId,
            AlternateOutputRoot,
            AlternateSceneA);
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
        string integrityPath = ProjectPath(projectRoot, StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath);
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
                            StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
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
    public void BakeTransaction_ScopedOwnerRejectsForeignMapMutablePath()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                StaticMapPresentationBakeTransaction.Begin(
                    projectRoot,
                    AlternateMapId,
                    AlternateOutputRoot,
                    AlternateManifestPath,
                    AlternateIntegrityPath,
                    new[] { SceneA }));
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void BakeTransaction_ScopedOwnerRestoresAlternateMapSceneAndMeta()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        string scenePath = ProjectPath(projectRoot, AlternateSceneA);
        try
        {
            WriteFile(scenePath, "alternate-scene-before");
            WriteFile(scenePath + ".meta", "alternate-meta-before");

            Assert.Throws<InvalidOperationException>(() =>
            {
                using StaticMapPresentationBakeTransaction transaction =
                    StaticMapPresentationBakeTransaction.Begin(
                        projectRoot,
                        AlternateMapId,
                        AlternateOutputRoot,
                        AlternateManifestPath,
                        AlternateIntegrityPath,
                        new[] { AlternateSceneA });
                WriteFile(scenePath, "alternate-scene-after");
                File.Delete(scenePath + ".meta");
                throw new InvalidOperationException("simulated alternate-map bake failure");
            });

            Assert.That(File.ReadAllText(scenePath), Is.EqualTo("alternate-scene-before"));
            Assert.That(File.ReadAllText(scenePath + ".meta"), Is.EqualTo("alternate-meta-before"));
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void MapBBakeMutationScope_CannotModifyOrDeleteMapAOutputs()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        string mapAScenePath = ProjectPath(projectRoot, SceneA);
        string mapAMetaPath = mapAScenePath + ".meta";
        string mapBScenePath = ProjectPath(projectRoot, AlternateSceneA);
        try
        {
            WriteFile(mapAScenePath, "map-a-scene");
            WriteFile(mapAMetaPath, "map-a-meta");
            WriteFile(mapBScenePath, "map-b-scene");
            WriteFile(mapBScenePath + ".meta", "map-b-meta");
            byte[] expectedMapAScene = File.ReadAllBytes(mapAScenePath);
            byte[] expectedMapAMeta = File.ReadAllBytes(mapAMetaPath);

            StaticMapPresentationManifest mapBManifest = CreateManifest(
                AlternateMapId,
                AlternateSceneA);
            try
            {
                using StaticMapPresentationBakeTransaction transaction =
                    StaticMapPresentationBakeTransaction.Begin(
                        projectRoot,
                        AlternateMapId,
                        AlternateOutputRoot,
                        AlternateManifestPath,
                        AlternateIntegrityPath,
                        new[] { AlternateSceneA });
                int deleted = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
                    AlternateMapId,
                    AlternateOutputRoot,
                    mapBManifest,
                    Array.Empty<string>(),
                    _ => false,
                    path => File.Exists(ProjectPath(projectRoot, path)),
                    _ => throw new AssertionException("Synthetic files must use physical deletion."),
                    path => DeletePhysicalScene(projectRoot, path));
                Assert.That(deleted, Is.EqualTo(1));
                transaction.Commit();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mapBManifest);
            }

            Assert.That(File.Exists(mapBScenePath), Is.False);
            Assert.That(File.ReadAllBytes(mapAScenePath), Is.EqualTo(expectedMapAScene));
            Assert.That(File.ReadAllBytes(mapAMetaPath), Is.EqualTo(expectedMapAMeta));
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
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                "content-hash",
                new[] { SceneB, SceneA });

            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
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
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
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
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
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
    public void SceneIntegrity_MetadataRefresh_PreservesSceneContentGate()
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
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                "content-hash",
                new[] { SceneA, SceneB });

            WriteFile(sceneAPath + ".meta", "scene-a-meta-normalized");
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryRefreshMetadataHashes(
                    projectRoot,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    "content-hash",
                    new[] { SceneA, SceneB },
                    out string refreshReason),
                Is.True,
                refreshReason);
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    "content-hash",
                    new[] { SceneA, SceneB },
                    out _,
                    out string validationReason),
                Is.True,
                validationReason);

            WriteFile(sceneBPath, "scene-b-corrupted");
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryRefreshMetadataHashes(
                    projectRoot,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    "content-hash",
                    new[] { SceneA, SceneB },
                    out string changedReason),
                Is.False);
            Assert.That(changedReason, Is.EqualTo($"integrity-scene-content-changed:{SceneB}"));
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void SceneIntegrity_TwoMapLedgersValidateIndependently()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        string mapAScenePath = ProjectPath(projectRoot, SceneA);
        string mapBScenePath = ProjectPath(projectRoot, AlternateSceneA);
        try
        {
            WriteFile(mapAScenePath, "map-a-scene");
            WriteFile(mapAScenePath + ".meta", "map-a-meta");
            WriteFile(mapBScenePath, "map-b-scene");
            WriteFile(mapBScenePath + ".meta", "map-b-meta");
            StaticMapPresentationSceneIntegrity.Write(
                projectRoot,
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                "map-a-content",
                new[] { SceneA });
            StaticMapPresentationSceneIntegrity.Write(
                projectRoot,
                AlternateMapId,
                AlternateIntegrityPath,
                "map-b-content",
                new[] { AlternateSceneA });

            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    "map-a-content",
                    new[] { SceneA },
                    out _,
                    out string mapAReason),
                Is.True,
                mapAReason);
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    AlternateMapId,
                    AlternateIntegrityPath,
                    "map-b-content",
                    new[] { AlternateSceneA },
                    out _,
                    out string mapBReason),
                Is.True,
                mapBReason);

            WriteFile(mapBScenePath, "map-b-corrupted");
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    StaticMapPresentationBaker.CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    "map-a-content",
                    new[] { SceneA },
                    out _,
                    out mapAReason),
                Is.True,
                mapAReason);
            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    AlternateMapId,
                    AlternateIntegrityPath,
                    "map-b-content",
                    new[] { AlternateSceneA },
                    out _,
                    out _),
                Is.False);
        }
        finally
        {
            DeleteTemporaryProjectRoot(projectRoot);
        }
    }

    [Test]
    public void SceneIntegrity_RejectsLedgerOwnedByAnotherOperationMap()
    {
        string projectRoot = CreateTemporaryProjectRoot();
        try
        {
            WriteFile(ProjectPath(projectRoot, SceneA), "scene-a");
            WriteFile(ProjectPath(projectRoot, SceneA + ".meta"), "scene-a-meta");
            StaticMapPresentationSceneIntegrity.Write(
                projectRoot,
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                "content-hash",
                new[] { SceneA });

            Assert.That(
                StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                    projectRoot,
                    "opmap.ch01.district-edge_01",
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    "content-hash",
                    new[] { SceneA },
                    out _,
                    out string rejectionReason),
                Is.False);
            Assert.That(rejectionReason, Is.EqualTo("integrity-ledger-owner-changed"));
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
        string alternateGeneratedPath = AlternateSceneA;
        string sourceFile = Path.Combine(root, "source.unity");
        string generatedFile = Path.Combine(root, "generated.unity");
        string alternateGeneratedFile = Path.Combine(root, "alternate-generated.unity");
        try
        {
            WriteFile(sourceFile, "source-v1");
            WriteFile(sourceFile + ".meta", "source-meta");
            WriteFile(generatedFile, "generated-v1");
            WriteFile(generatedFile + ".meta", "generated-meta");
            WriteFile(alternateGeneratedFile, "alternate-generated-v1");
            WriteFile(alternateGeneratedFile + ".meta", "alternate-generated-meta");
            Dictionary<string, string> physicalPaths = new(StringComparer.Ordinal)
            {
                [sourcePath] = sourceFile,
                [generatedPath] = generatedFile,
                [alternateGeneratedPath] = alternateGeneratedFile
            };

            string initialHash = StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
                new[] { sourcePath, generatedPath, alternateGeneratedPath },
                path => physicalPaths[path],
                _ => throw new AssertionException("Physical test assets must not use fallback identity."));
            WriteFile(generatedFile, "generated-v2");
            WriteFile(alternateGeneratedFile, "alternate-generated-v2");
            string generatedChangedHash = StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
                new[] { alternateGeneratedPath, generatedPath, sourcePath },
                path => physicalPaths[path],
                _ => throw new AssertionException("Physical test assets must not use fallback identity."));
            WriteFile(sourceFile, "source-v2");
            string sourceChangedHash = StaticMapPresentationCanonicalSourceHash.ComputeDirectDependencySetHash(
                new[] { sourcePath, alternateGeneratedPath, generatedPath },
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
    public void MapOwnedSourceSetHash_IsOrderIndependentAndSensitiveToMapChanges()
    {
        string initial = StaticMapPresentationCanonicalSourceHash.ComputeMapOwnedSourceSetHash(
            new[] { "map/source-b|dependency-b", "map/source-a|dependency-a" });
        string reordered = StaticMapPresentationCanonicalSourceHash.ComputeMapOwnedSourceSetHash(
            new[] { "map/source-a|dependency-a", "map/source-b|dependency-b" });
        string mapChanged = StaticMapPresentationCanonicalSourceHash.ComputeMapOwnedSourceSetHash(
            new[] { "map/source-a|dependency-a-v2", "map/source-b|dependency-b" });

        Assert.That(reordered, Is.EqualTo(initial));
        Assert.That(mapChanged, Is.Not.EqualTo(initial));
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
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/Scenes/StaticMapPresentation_opmap_skirmish_desert_base_01_chunk_p000_p000.unity", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate/StaticMapPresentationSceneIntegrity.json", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/test/alternate/Scenes/StaticMapPresentation_opmap_test_alternate_chunk_p000_p000.unity", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMapsSibling/opmap/test/alternate.asset", true)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentationSibling/asset.asset", false)]
    [TestCase("Assets/Game/GeneratedStaticMapPresentation/OperationMaps/../Source.asset", false)]
    [TestCase("Assets\\Game\\GeneratedStaticMapPresentation\\OperationMaps\\opmap\\test\\alternate.asset", false)]
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

    private static StaticMapPresentationManifest CreateManifest(
        string operationMapId,
        string scenePath)
    {
        StaticMapPresentationManifest manifest =
            ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
        manifest.EditorSetData(
            operationMapId,
            "00000000000000000000000000000001",
            StaticMapPresentationBaker.CanonicalMatchScenePath,
            "canonical-hash",
            StaticMapPresentationBaker.ChunkSize,
            "content-hash",
            new List<StaticMapPresentationChunkEntry>
            {
                new("chunk_p000_p000", scenePath, new Bounds(Vector3.zero, Vector3.one), 0, 1)
            },
            new List<StaticMapPresentationSourceEntry>());
        return manifest;
    }

    private static void AssertIdenticalSecondBakeIsNoOp(
        string operationMapId,
        string outputRoot,
        string scenePath)
    {
        StaticMapPresentationManifest manifest = CreateManifest(operationMapId, scenePath);
        try
        {
            string[] ownedPaths = StaticMapPresentationOutputOwnership.CaptureOwnedScenePaths(
                manifest,
                operationMapId,
                outputRoot);
            Assert.That(
                StaticMapPresentationOutputOwnership.CanReuseExpectedScenes(
                    operationMapId,
                    outputRoot,
                    manifest.SchemaVersion,
                    manifest.CanonicalScenePath,
                    manifest.ChunkSize,
                    manifest.ContentHash,
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    StaticMapPresentationBaker.ChunkSize,
                    manifest.ContentHash,
                    ownedPaths,
                    new[] { scenePath },
                    _ => true,
                    out string rejectionReason),
                Is.True,
                rejectionReason);

            int deleted = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
                operationMapId,
                outputRoot,
                manifest,
                new[] { scenePath },
                _ => true,
                _ => true,
                _ => throw new AssertionException("An identical second bake must not delete a scene asset."),
                _ => throw new AssertionException("An identical second bake must not delete a physical scene."));
            Assert.That(deleted, Is.Zero);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(manifest);
        }
    }

    private static string ProjectPath(string projectRoot, string assetPath)
    {
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static bool DeletePhysicalScene(string projectRoot, string assetPath)
    {
        string physicalPath = ProjectPath(projectRoot, assetPath);
        bool existed = File.Exists(physicalPath);
        if (existed)
            File.Delete(physicalPath);
        if (File.Exists(physicalPath + ".meta"))
            File.Delete(physicalPath + ".meta");
        return existed;
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
