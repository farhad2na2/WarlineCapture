using System;
using System.IO;
using Game.Editor;
using Game.Configs;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapDenseCityCandidateRuntimeContentBuilderTests
{
    public static void RunFocusedValidation()
    {
        var suite = new OperationMapDenseCityCandidateRuntimeContentBuilderTests();
        Action[] tests =
        {
            suite.MeasureEntityContent_SeparatesArchiveAndMetadataBytes,
            suite.MeasureEntityContent_RejectsMissingCatalog,
            suite.MeasureEntityContent_ReportsMultipleArchivesForFailClosedCaller,
            suite.MeasureFrozenRollbackContent_SeparatesManifestAndChunkBytes,
            suite.MeasureFrozenRollbackContent_RejectsMissingManifest,
            suite.MeasureFrozenRollbackContent_AcceptsCurrentFrozenPackage,
            suite.MeasureProductionStaticAddressables_ReportsLegacyBaseline,
            suite.MeasureProductionStaticAddressables_RejectsRetiredEntriesAfterCutover,
            suite.MeasureProductionStaticAddressables_AcceptsZeroEntriesAfterCutover
        };
        for (int i = 0; i < tests.Length; i++)
            tests[i]();
        Debug.Log(
            $"[DenseCityRuntimeContentByteInventoryValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void MeasureEntityContent_SeparatesArchiveAndMetadataBytes()
    {
        WithContentDirectory(
            root =>
            {
                string catalog = WriteBytes(root, "catalog.bin", 7);
                WriteBytes(root, "scene.archive", 19);
                WriteBytes(root, "metadata/header.bin", 5);

                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    result =
                        OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureEntityContent(
                            root,
                            catalog);

                Assert.That(result.ArchiveCount, Is.EqualTo(1));
                Assert.That(result.ArchiveBytes, Is.EqualTo(19));
                Assert.That(result.MetadataBytes, Is.EqualTo(12));
                Assert.That(result.TotalBytes, Is.EqualTo(31));
            });
    }

    [Test]
    public void MeasureEntityContent_RejectsMissingCatalog()
    {
        WithContentDirectory(
            root =>
            {
                WriteBytes(root, "scene.archive", 19);
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureEntityContent(
                        root,
                        Path.Combine(root, "missing-catalog.bin")));
                Assert.That(exception.Message, Does.Contain("catalog is missing"));
            });
    }

    [Test]
    public void MeasureEntityContent_ReportsMultipleArchivesForFailClosedCaller()
    {
        WithContentDirectory(
            root =>
            {
                string catalog = WriteBytes(root, "catalog.bin", 3);
                WriteBytes(root, "a.archive", 11);
                WriteBytes(root, "nested/b.archive", 13);

                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    result =
                        OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureEntityContent(
                            root,
                            catalog);

                Assert.That(result.ArchiveCount, Is.EqualTo(2));
                Assert.That(result.ArchiveBytes, Is.EqualTo(24));
                Assert.That(result.MetadataBytes, Is.EqualTo(3));
                Assert.That(result.TotalBytes, Is.EqualTo(27));
            });
    }

    [Test]
    public void MeasureFrozenRollbackContent_SeparatesManifestAndChunkBytes()
    {
        WithProjectDirectory(
            projectRoot =>
            {
                string rollbackRoot = Path.Combine(
                    projectRoot,
                    OperationMapDenseCityCandidateRuntimeContentBuilder.FrozenRollbackRootPath);
                WriteBytes(rollbackRoot, "StaticMapPresentationManifest.asset", 17);
                WriteBytes(rollbackRoot, "Scenes/chunk-a.unity", 11);
                WriteBytes(rollbackRoot, "Scenes/chunk-b.unity", 13);
                WriteBytes(rollbackRoot, "Scenes/chunk-a.unity.meta", 101);

                OperationMapDenseCityCandidateRuntimeContentBuilder.FrozenRollbackContentResult
                    result =
                        OperationMapDenseCityCandidateRuntimeContentBuilder
                            .MeasureFrozenRollbackContent(projectRoot);

                Assert.That(result.ManifestBytes, Is.EqualTo(17));
                Assert.That(result.ChunkCount, Is.EqualTo(2));
                Assert.That(result.ChunkBytes, Is.EqualTo(24));
            });
    }

    [Test]
    public void MeasureFrozenRollbackContent_RejectsMissingManifest()
    {
        WithProjectDirectory(
            projectRoot =>
            {
                string rollbackRoot = Path.Combine(
                    projectRoot,
                    OperationMapDenseCityCandidateRuntimeContentBuilder.FrozenRollbackRootPath);
                WriteBytes(rollbackRoot, "Scenes/chunk-a.unity", 11);

                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => OperationMapDenseCityCandidateRuntimeContentBuilder
                        .MeasureFrozenRollbackContent(projectRoot));
                Assert.That(exception.Message, Does.Contain("rollback manifest is missing"));
            });
    }

    [Test]
    public void MeasureFrozenRollbackContent_AcceptsCurrentFrozenPackage()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        Assert.That(projectRoot, Is.Not.Null.And.Not.Empty);

        OperationMapDenseCityCandidateRuntimeContentBuilder.FrozenRollbackContentResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureFrozenRollbackContent(
                projectRoot);

        Assert.That(result.ManifestBytes, Is.EqualTo(10097753));
        Assert.That(result.ChunkCount, Is.EqualTo(269));
        Assert.That(result.ChunkBytes, Is.EqualTo(32381589));
    }

    [Test]
    public void MeasureProductionStaticAddressables_ReportsLegacyBaseline()
    {
        string[] paths =
        {
            OperationMapAddressablesLayoutBuilder.ManifestPath,
            StaticMapPresentationBaker.SceneOutputFolder + "/chunk-a.unity",
            StaticMapPresentationBaker.SceneOutputFolder + "/chunk-b.unity",
            "Assets/Game/Configs/OperationMaps/other.asset"
        };

        OperationMapDenseCityCandidateRuntimeContentBuilder
            .ProductionStaticAddressablesResult result =
                OperationMapDenseCityCandidateRuntimeContentBuilder
                    .MeasureProductionStaticAddressables(
                        OperationMapPresentationKind.StaticSceneChunks,
                        paths);

        Assert.That(result.PresentationKind, Is.EqualTo("StaticSceneChunks"));
        Assert.That(result.ManifestEntryCount, Is.EqualTo(1));
        Assert.That(result.ChunkEntryCount, Is.EqualTo(2));
        Assert.That(result.ZeroCountsSatisfied, Is.False);
    }

    [Test]
    public void MeasureProductionStaticAddressables_RejectsRetiredEntriesAfterCutover()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureProductionStaticAddressables(
                    OperationMapPresentationKind.EntityScene,
                    new[] { OperationMapAddressablesLayoutBuilder.ManifestPath }));
        Assert.That(exception.Message, Does.Contain("still owns retired static"));
    }

    [Test]
    public void MeasureProductionStaticAddressables_AcceptsZeroEntriesAfterCutover()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder
            .ProductionStaticAddressablesResult result =
                OperationMapDenseCityCandidateRuntimeContentBuilder
                    .MeasureProductionStaticAddressables(
                        OperationMapPresentationKind.EntityScene,
                        new[] { "Assets/Game/Configs/OperationMaps/other.asset" });

        Assert.That(result.ManifestEntryCount, Is.Zero);
        Assert.That(result.ChunkEntryCount, Is.Zero);
        Assert.That(result.ZeroCountsSatisfied, Is.True);
    }

    private static string WriteBytes(string root, string relativePath, int count)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, new byte[count]);
        return path;
    }

    private static void WithContentDirectory(Action<string> action)
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "WarlineDenseRuntimeContentByteTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            action(root);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static void WithProjectDirectory(Action<string> action)
    {
        string projectRoot = Path.Combine(
            Path.GetTempPath(),
            "WarlineDenseRuntimeContentRollbackByteTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectRoot);
        try
        {
            action(projectRoot);
        }
        finally
        {
            Directory.Delete(projectRoot, true);
        }
    }
}
