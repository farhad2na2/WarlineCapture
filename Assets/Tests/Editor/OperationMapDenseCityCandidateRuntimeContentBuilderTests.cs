using System;
using System.IO;
using Game.Editor;
using Game.Configs;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public sealed class OperationMapDenseCityCandidateRuntimeContentBuilderTests
{
    public static void RunFocusedValidation()
    {
        var suite = new OperationMapDenseCityCandidateRuntimeContentBuilderTests();
        Action[] tests =
        {
            suite.MeasureEntityContent_SeparatesArchiveAndMetadataBytes,
            suite.MeasureEntityContent_FingerprintsArchiveSetDeterministically,
            suite.MeasureEntityContent_RejectsMissingCatalog,
            suite.MeasureEntityContent_ReportsMultipleArchivesForFailClosedCaller,
            suite.MeasureFrozenRollbackContent_SeparatesManifestAndChunkBytes,
            suite.MeasureFrozenRollbackContent_RejectsMissingManifest,
            suite.MeasureFrozenRollbackContent_AcceptsCurrentFrozenPackage,
            suite.MeasureProductionStaticAddressables_ReportsLegacyBaseline,
            suite.MeasureProductionStaticAddressables_RejectsRetiredEntriesAfterCutover,
            suite.MeasureProductionStaticAddressables_AcceptsZeroEntriesAfterCutover,
            suite.MeasurePackedDependencyBytes_ReportsSharedAndExcessPhysicalBytes,
            suite.MeasurePackedDependencyBytes_DeduplicatesSameBundleRows,
            suite.MeasurePackedDependencyBytes_RejectsMissingSharedEvidence,
            suite.MeasurePackedDependencyBytes_AdaptsAddressablesBuildLayout,
            suite.RequireNoPackedDependencyDuplication_AcceptsSinglePhysicalBundle,
            suite.RequireNoPackedDependencyDuplication_RejectsCrossBundleCopy,
            suite.MeasurePackedSourceHierarchy_AcceptsRuntimeOnlyPaths,
            suite.MeasurePackedSourceHierarchy_RejectsExplicitOrImplicitSourcePath,
            suite.MeasurePackedSourceHierarchy_AdaptsAddressablesBuildLayout,
            suite.SelectSingleGeneratedBuildLayoutPath_ReturnsOnlyNewJson,
            suite.SelectSingleGeneratedBuildLayoutPath_RejectsMissingReport,
            suite.SelectSingleGeneratedBuildLayoutPath_RejectsAmbiguousReports,
            suite.MeasureSourceHierarchyExclusion_AcceptsRuntimeEntriesAndUnrelatedBuildScenes,
            suite.MeasureSourceHierarchyExclusion_ReportsForbiddenExplicitEntry,
            suite.MeasureSourceHierarchyExclusion_ReportsForbiddenPlayerBuildScene,
            suite.MeasureLocalBundleDelivery_AcceptsIncludedLocalOfflineGroup,
            suite.MeasureLocalBundleDelivery_RejectsRemoteCatalog,
            suite.MeasureLocalBundleDelivery_RejectsExcludedGroup,
            suite.MeasureLocalBundleDelivery_RejectsNetworkLoadPath,
            suite.MeasurePublishedLocalContent_ReportsCatalogAndBundleBytes,
            suite.MeasurePublishedLocalContent_FingerprintsBundleSetDeterministically,
            suite.MeasurePublishedLocalContent_RejectsCatalogOutsideOutput,
            suite.MeasurePublishedLocalContent_RejectsMissingBundle
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
    public void MeasureEntityContent_FingerprintsArchiveSetDeterministically()
    {
        WithContentDirectory(
            root =>
            {
                string firstRoot = Path.Combine(root, "first");
                string secondRoot = Path.Combine(root, "second");
                string firstCatalog = WriteBytes(firstRoot, "catalog.bin", 7);
                string secondCatalog = WriteBytes(secondRoot, "catalog.bin", 7);
                string firstArchive = WriteBytes(firstRoot, "scene.archive", 19);
                WriteBytes(secondRoot, "scene.archive", 19);
                WriteBytes(firstRoot, "metadata/header.bin", 5);
                WriteBytes(secondRoot, "metadata/header.bin", 5);

                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    first = OperationMapDenseCityCandidateRuntimeContentBuilder
                        .MeasureEntityContent(firstRoot, firstCatalog);
                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    second = OperationMapDenseCityCandidateRuntimeContentBuilder
                        .MeasureEntityContent(secondRoot, secondCatalog);

                Assert.That(first.ArchiveSetSha256, Has.Length.EqualTo(64));
                Assert.That(second.ArchiveSetSha256, Is.EqualTo(first.ArchiveSetSha256));

                File.WriteAllBytes(firstArchive, new byte[23]);
                OperationMapDenseCityCandidateRuntimeContentBuilder.EntityContentBuildResult
                    changed = OperationMapDenseCityCandidateRuntimeContentBuilder
                        .MeasureEntityContent(firstRoot, firstCatalog);
                Assert.That(changed.ArchiveSetSha256, Is.Not.EqualTo(first.ArchiveSetSha256));
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

    [Test]
    public void MeasurePackedDependencyBytes_ReportsSharedAndExcessPhysicalBytes()
    {
        var occurrences =
            new[]
            {
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "shared-guid",
                    "shared.bundle",
                    100),
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "duplicate-guid",
                    "a.bundle",
                    30),
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "duplicate-guid",
                    "b.bundle",
                    25),
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "unique-guid",
                    "a.bundle",
                    9)
            };

        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedDependencyByteResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder.MeasurePackedDependencyBytes(
                occurrences,
                new[] { "shared-guid" });

        Assert.That(result.SharedDependencyGuidCount, Is.EqualTo(1));
        Assert.That(result.SharedDependencyBytes, Is.EqualTo(100));
        Assert.That(result.DuplicatedDependencyGuidCount, Is.EqualTo(1));
        Assert.That(result.DuplicatedDependencyBytes, Is.EqualTo(25));
    }

    [Test]
    public void MeasurePackedDependencyBytes_DeduplicatesSameBundleRows()
    {
        var occurrences =
            new[]
            {
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "shared-guid",
                    "shared.bundle",
                    40),
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "shared-guid",
                    "shared.bundle",
                    40)
            };

        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedDependencyByteResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder.MeasurePackedDependencyBytes(
                occurrences,
                new[] { "shared-guid", "shared-guid" });

        Assert.That(result.SharedDependencyGuidCount, Is.EqualTo(1));
        Assert.That(result.SharedDependencyBytes, Is.EqualTo(40));
        Assert.That(result.DuplicatedDependencyGuidCount, Is.Zero);
        Assert.That(result.DuplicatedDependencyBytes, Is.Zero);
    }

    [Test]
    public void MeasurePackedDependencyBytes_RejectsMissingSharedEvidence()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasurePackedDependencyBytes(
                    new[]
                    {
                        new OperationMapDenseCityCandidateRuntimeContentBuilder
                            .PackedAssetOccurrence("present-guid", "bundle", 10)
                    },
                    new[] { "missing-guid" }));
        Assert.That(exception.Message, Does.Contain("missing from Build Layout evidence"));
    }

    [Test]
    public void MeasurePackedDependencyBytes_AdaptsAddressablesBuildLayout()
    {
        var layout = new BuildLayout();
        var group = new BuildLayout.Group { Name = "Dense Candidate" };
        layout.Groups.Add(group);

        BuildLayout.File firstFile = AddBuildLayoutFile(group, "first.bundle");
        firstFile.Assets.Add(
            new BuildLayout.ExplicitAsset
            {
                Guid = "shared-guid",
                Bundle = firstFile.Bundle,
                SerializedSize = 60,
                StreamedSize = 40
            });
        firstFile.OtherAssets.Add(
            new BuildLayout.DataFromOtherAsset
            {
                AssetGuid = "duplicate-guid",
                File = firstFile,
                SerializedSize = 25,
                StreamedSize = 5
            });

        BuildLayout.File secondFile = AddBuildLayoutFile(group, "second.bundle");
        secondFile.OtherAssets.Add(
            new BuildLayout.DataFromOtherAsset
            {
                AssetGuid = "duplicate-guid",
                File = secondFile,
                SerializedSize = 20,
                StreamedSize = 5
            });

        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedDependencyByteResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder.MeasurePackedDependencyBytes(
                layout,
                new[] { "shared-guid" });

        Assert.That(result.SharedDependencyGuidCount, Is.EqualTo(1));
        Assert.That(result.SharedDependencyBytes, Is.EqualTo(100));
        Assert.That(result.DuplicatedDependencyGuidCount, Is.EqualTo(1));
        Assert.That(result.DuplicatedDependencyBytes, Is.EqualTo(25));
    }

    [Test]
    public void RequireNoPackedDependencyDuplication_AcceptsSinglePhysicalBundle()
    {
        var occurrences =
            new[]
            {
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "shared-guid",
                    "dense.bundle",
                    40),
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "shared-guid",
                    "dense.bundle",
                    40)
            };
        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedDependencyByteResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder.MeasurePackedDependencyBytes(
                occurrences,
                new[] { "shared-guid" });

        Assert.DoesNotThrow(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireNoPackedDependencyDuplication(result));
    }

    [Test]
    public void RequireNoPackedDependencyDuplication_RejectsCrossBundleCopy()
    {
        var occurrences =
            new[]
            {
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "copied-guid",
                    "first.bundle",
                    40),
                new OperationMapDenseCityCandidateRuntimeContentBuilder.PackedAssetOccurrence(
                    "copied-guid",
                    "second.bundle",
                    25)
            };
        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedDependencyByteResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder.MeasurePackedDependencyBytes(
                occurrences,
                Array.Empty<string>());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireNoPackedDependencyDuplication(result));
        Assert.That(exception.Message, Does.Contain("guids=1"));
        Assert.That(exception.Message, Does.Contain("excessBytes=25"));
    }

    private static BuildLayout.File AddBuildLayoutFile(
        BuildLayout.Group group,
        string bundleName)
    {
        var bundle = new BuildLayout.Bundle
        {
            Name = bundleName,
            Group = group
        };
        group.Bundles.Add(bundle);
        var file = new BuildLayout.File
        {
            Name = bundleName,
            Bundle = bundle
        };
        bundle.Files.Add(file);
        return file;
    }

    [Test]
    public void MeasurePackedSourceHierarchy_AcceptsRuntimeOnlyPaths()
    {
        var occurrences =
            new[]
            {
                new OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PackedAssetPathOccurrence(
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                            .DenseCandidateDefinitionPath,
                        "dense.bundle",
                        true),
                new OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PackedAssetPathOccurrence(
                        "Assets/Game/Art/Shared/road.mat",
                        "dense.bundle",
                        false)
            };

        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedSourceHierarchyResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasurePackedSourceHierarchy(occurrences);

        Assert.That(result.PackedAssetPathCount, Is.EqualTo(2));
        Assert.That(result.SourceHierarchyExplicitAssetCount, Is.Zero);
        Assert.That(result.SourceHierarchyImplicitAssetCount, Is.Zero);
        Assert.DoesNotThrow(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequirePackedSourceHierarchyExclusion(result));
    }

    [Test]
    public void MeasurePackedSourceHierarchy_RejectsExplicitOrImplicitSourcePath()
    {
        var occurrences =
            new[]
            {
                new OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PackedAssetPathOccurrence(
                        DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                        "dense.bundle",
                        true),
                new OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PackedAssetPathOccurrence(
                        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                        "dense.bundle",
                        false)
            };

        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedSourceHierarchyResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasurePackedSourceHierarchy(occurrences);

        Assert.That(result.SourceHierarchyExplicitAssetCount, Is.EqualTo(1));
        Assert.That(result.SourceHierarchyImplicitAssetCount, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequirePackedSourceHierarchyExclusion(result));
    }

    [Test]
    public void MeasurePackedSourceHierarchy_AdaptsAddressablesBuildLayout()
    {
        var layout = new BuildLayout();
        var group = new BuildLayout.Group { Name = "Dense Candidate" };
        layout.Groups.Add(group);
        BuildLayout.File file = AddBuildLayoutFile(group, "dense.bundle");
        file.Assets.Add(
            new BuildLayout.ExplicitAsset
            {
                AssetPath = DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                Bundle = file.Bundle,
                File = file
            });
        file.OtherAssets.Add(
            new BuildLayout.DataFromOtherAsset
            {
                AssetPath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                File = file
            });

        OperationMapDenseCityCandidateRuntimeContentBuilder.PackedSourceHierarchyResult result =
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasurePackedSourceHierarchy(layout);

        Assert.That(result.PackedAssetPathCount, Is.EqualTo(2));
        Assert.That(result.SourceHierarchyExplicitAssetCount, Is.EqualTo(1));
        Assert.That(result.SourceHierarchyImplicitAssetCount, Is.EqualTo(1));
    }

    [Test]
    public void SelectSingleGeneratedBuildLayoutPath_ReturnsOnlyNewJson()
    {
        string result =
            OperationMapDenseCityCandidateRuntimeContentBuilder
                .SelectSingleGeneratedBuildLayoutPath(
                    new[] { "old.json" },
                    new[] { "old.json", "new.json", "ignored.txt" },
                    path => path == "new.json");

        Assert.That(result, Is.EqualTo("new.json"));
    }

    [Test]
    public void SelectSingleGeneratedBuildLayoutPath_RejectsMissingReport()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .SelectSingleGeneratedBuildLayoutPath(
                    new[] { "old.json" },
                    new[] { "old.json" },
                    _ => true));
        Assert.That(exception.Message, Does.Contain("found 0"));
    }

    [Test]
    public void SelectSingleGeneratedBuildLayoutPath_RejectsAmbiguousReports()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .SelectSingleGeneratedBuildLayoutPath(
                    Array.Empty<string>(),
                    new[] { "first.json", "second.json" },
                    _ => true));
        Assert.That(exception.Message, Does.Contain("found 2"));
    }

    [Test]
    public void MeasureSourceHierarchyExclusion_AcceptsRuntimeEntriesAndUnrelatedBuildScenes()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.SourceHierarchyExclusionResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureSourceHierarchyExclusion(
                    new[]
                    {
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                            .DenseCandidateDefinitionPath,
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                            .DenseCandidateRuntimeBindingPath
                    },
                    new[]
                    {
                        "Assets/Game/Scenes/Bootstrap.unity",
                        "Assets/Game/Scenes/MainMenu.unity"
                    });

        Assert.That(result.ExplicitAddressableEntryCount, Is.EqualTo(2));
        Assert.That(result.EnabledPlayerBuildSceneCount, Is.EqualTo(2));
        Assert.That(result.SourceHierarchyExplicitAddressableEntryCount, Is.Zero);
        Assert.That(result.SourceHierarchyPlayerBuildSceneCount, Is.Zero);
    }

    [Test]
    public void MeasureSourceHierarchyExclusion_ReportsForbiddenExplicitEntry()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.SourceHierarchyExclusionResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureSourceHierarchyExclusion(
                    new[]
                    {
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                            .DenseCandidateDefinitionPath,
                        DenseCityCandidateAuthoringTransaction.CandidateMapScenePath
                    },
                    Array.Empty<string>());

        Assert.That(result.SourceHierarchyExplicitAddressableEntryCount, Is.EqualTo(1));
        Assert.That(result.SourceHierarchyPlayerBuildSceneCount, Is.Zero);
        Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireSourceHierarchyExclusion(result, expectedExplicitEntryCount: 2));
    }

    [Test]
    public void MeasureSourceHierarchyExclusion_ReportsForbiddenPlayerBuildScene()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.SourceHierarchyExclusionResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureSourceHierarchyExclusion(
                    Array.Empty<string>(),
                    new[]
                    {
                        "Assets/Game/Scenes/Bootstrap.unity",
                        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath
                    });

        Assert.That(result.SourceHierarchyExplicitAddressableEntryCount, Is.Zero);
        Assert.That(result.SourceHierarchyPlayerBuildSceneCount, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireSourceHierarchyExclusion(result, expectedExplicitEntryCount: 2));
    }

    [Test]
    public void MeasureLocalBundleDelivery_AcceptsIncludedLocalOfflineGroup()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.LocalBundleDeliveryResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureLocalBundleDelivery(
                    buildRemoteCatalog: false,
                    disableCatalogUpdateOnStartup: true,
                    includeInBuild: true,
                    AddressableAssetSettings.kLocalBuildPath,
                    AddressableAssetSettings.kLocalLoadPath,
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneWindows64");

        Assert.DoesNotThrow(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireLocalBundleDelivery(result));
    }

    [Test]
    public void MeasureLocalBundleDelivery_RejectsRemoteCatalog()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.LocalBundleDeliveryResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureLocalBundleDelivery(
                    buildRemoteCatalog: true,
                    disableCatalogUpdateOnStartup: true,
                    includeInBuild: true,
                    AddressableAssetSettings.kLocalBuildPath,
                    AddressableAssetSettings.kLocalLoadPath,
                    "C:/Warline/LocalBundles");

        Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireLocalBundleDelivery(result));
    }

    [Test]
    public void MeasureLocalBundleDelivery_RejectsExcludedGroup()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.LocalBundleDeliveryResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureLocalBundleDelivery(
                    buildRemoteCatalog: false,
                    disableCatalogUpdateOnStartup: true,
                    includeInBuild: false,
                    AddressableAssetSettings.kLocalBuildPath,
                    AddressableAssetSettings.kLocalLoadPath,
                    "C:/Warline/LocalBundles");

        Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireLocalBundleDelivery(result));
    }

    [Test]
    public void MeasureLocalBundleDelivery_RejectsNetworkLoadPath()
    {
        OperationMapDenseCityCandidateRuntimeContentBuilder.LocalBundleDeliveryResult
            result = OperationMapDenseCityCandidateRuntimeContentBuilder
                .MeasureLocalBundleDelivery(
                    buildRemoteCatalog: false,
                    disableCatalogUpdateOnStartup: true,
                    includeInBuild: true,
                    AddressableAssetSettings.kLocalBuildPath,
                    AddressableAssetSettings.kLocalLoadPath,
                    "https://cdn.example.invalid/warline");

        Assert.That(result.NetworkLoadPath, Is.True);
        Assert.Throws<InvalidOperationException>(
            () => OperationMapDenseCityCandidateRuntimeContentBuilder
                .RequireLocalBundleDelivery(result));
    }

    [Test]
    public void MeasurePublishedLocalContent_ReportsCatalogAndBundleBytes()
    {
        WithContentDirectory(
            root =>
            {
                string catalog = WriteBytes(root, "catalog.bin", 7);
                WriteBytes(root, "StandaloneOSX/first.bundle", 11);
                WriteBytes(root, "StandaloneOSX/second.bundle", 13);

                OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PublishedLocalContentResult result =
                        OperationMapDenseCityCandidateRuntimeContentBuilder
                            .MeasurePublishedLocalContent(root, catalog);

                Assert.That(result.CatalogBytes, Is.EqualTo(7));
                Assert.That(result.BundleCount, Is.EqualTo(2));
                Assert.That(result.BundleBytes, Is.EqualTo(24));
            });
    }

    [Test]
    public void MeasurePublishedLocalContent_FingerprintsBundleSetDeterministically()
    {
        WithContentDirectory(
            root =>
            {
                string firstRoot = Path.Combine(root, "first");
                string secondRoot = Path.Combine(root, "second");
                string firstCatalog = WriteBytes(firstRoot, "catalog.bin", 7);
                string secondCatalog = WriteBytes(secondRoot, "catalog.bin", 7);
                string firstBundle =
                    WriteBytes(firstRoot, "StandaloneOSX/dense.bundle", 11);
                WriteBytes(secondRoot, "StandaloneOSX/dense.bundle", 11);

                OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PublishedLocalContentResult first =
                        OperationMapDenseCityCandidateRuntimeContentBuilder
                            .MeasurePublishedLocalContent(firstRoot, firstCatalog);
                OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PublishedLocalContentResult second =
                        OperationMapDenseCityCandidateRuntimeContentBuilder
                            .MeasurePublishedLocalContent(secondRoot, secondCatalog);

                Assert.That(first.BundleSetSha256, Has.Length.EqualTo(64));
                Assert.That(second.BundleSetSha256, Is.EqualTo(first.BundleSetSha256));

                File.WriteAllBytes(firstBundle, new byte[13]);
                OperationMapDenseCityCandidateRuntimeContentBuilder
                    .PublishedLocalContentResult changed =
                        OperationMapDenseCityCandidateRuntimeContentBuilder
                            .MeasurePublishedLocalContent(firstRoot, firstCatalog);
                Assert.That(changed.BundleSetSha256, Is.Not.EqualTo(first.BundleSetSha256));
            });
    }

    [Test]
    public void MeasurePublishedLocalContent_RejectsCatalogOutsideOutput()
    {
        WithContentDirectory(
            root =>
            {
                string output = Path.Combine(root, "output");
                Directory.CreateDirectory(output);
                WriteBytes(output, "dense.bundle", 11);
                string outsideCatalog = WriteBytes(root, "outside-catalog.bin", 7);

                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => OperationMapDenseCityCandidateRuntimeContentBuilder
                        .MeasurePublishedLocalContent(output, outsideCatalog));
                Assert.That(exception.Message, Does.Contain("outside its output"));
            });
    }

    [Test]
    public void MeasurePublishedLocalContent_RejectsMissingBundle()
    {
        WithContentDirectory(
            root =>
            {
                string catalog = WriteBytes(root, "catalog.bin", 7);

                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => OperationMapDenseCityCandidateRuntimeContentBuilder
                        .MeasurePublishedLocalContent(root, catalog));
                Assert.That(exception.Message, Does.Contain("bundles=0"));
            });
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
