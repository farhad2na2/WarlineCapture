using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.Layout;

public sealed class OperationMapAddressablesBuildReportBuilderTests
{
    [Test]
    public void RuntimeSettings_AcceptBuiltInCatalogWithUpdatesDisabled()
    {
        const string json =
            "{\"m_DisableCatalogUpdateOnStart\":true," +
            "\"m_CatalogLocations\":[{\"m_InternalId\":" +
            "\"{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin\"}]}";

        Assert.That(
            OperationMapAddressablesBuildReportBuilder.TryValidateRuntimeSettings(
                json,
                out string error),
            Is.True,
            error);
    }

    [TestCase(false, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/catalog.bin")]
    [TestCase(true, "https://content.example/catalog.bin")]
    public void RuntimeSettings_RejectCatalogUpdateOrRemoteLocation(
        bool disableUpdates,
        string internalId)
    {
        string json =
            $"{{\"m_DisableCatalogUpdateOnStart\":{disableUpdates.ToString().ToLowerInvariant()}," +
            $"\"m_CatalogLocations\":[{{\"m_InternalId\":\"{internalId}\"}}]}}";

        Assert.That(
            OperationMapAddressablesBuildReportBuilder.TryValidateRuntimeSettings(
                json,
                out string error),
            Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void Create_AttributesBundleClosurePartitionsEntitiesAndDuplicates()
    {
        BuildLayout layout = CreateLayout(reverseAssets: false);

        OperationMapAddressablesBuildReport report =
            OperationMapAddressablesBuildReportBuilder.Create(layout);

        Assert.AreEqual(1, report.Maps.Length);
        Assert.AreEqual(3, report.Maps[0].BundleCount);
        Assert.AreEqual(600ul, report.Maps[0].BundleBytes);
        Assert.AreEqual(600ul, report.AggregateBundleBytes);
        Assert.AreEqual(1, report.Partitions.Length);
        Assert.AreEqual(2, report.Partitions[0].EntryCount);
        Assert.AreEqual(2, report.Partitions[0].BundleCount);
        Assert.AreEqual(2, report.RequiredAddresses.Length);
        Assert.AreEqual(1, report.EntitiesArtifacts.Length);
        Assert.AreEqual(44ul, report.EntitiesArtifacts[0].Bytes);
        Assert.AreEqual(1, report.DuplicateDependencies.Length);
        Assert.AreEqual(2, report.DuplicateDependencies[0].BundleCount);
        Assert.AreEqual(12ul, report.DuplicateDependencies[0].DuplicateBytes);
    }

    [Test]
    public void Serialize_IsByteIdenticalWhenInputOrderChanges()
    {
        string first = OperationMapAddressablesBuildReportBuilder.Serialize(
            OperationMapAddressablesBuildReportBuilder.Create(CreateLayout(reverseAssets: false)));
        string second = OperationMapAddressablesBuildReportBuilder.Serialize(
            OperationMapAddressablesBuildReportBuilder.Create(CreateLayout(reverseAssets: true)));

        Assert.AreEqual(first, second);
    }

    [Test]
    public void Create_RejectsDuplicateStableAddresses()
    {
        BuildLayout layout = CreateLayout(reverseAssets: false);
        layout.Groups[0].Bundles[1].Files[0].Assets[0].AddressableName =
            layout.Groups[0].Bundles[0].Files[0].Assets[0].AddressableName;

        Assert.Throws<InvalidOperationException>(
            () => OperationMapAddressablesBuildReportBuilder.Create(layout));
    }

    [Test]
    public void DuplicateValidation_RejectsProjectOwnedDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new("project-guid", "Assets/Shared.asset", 2, 12)
        };

        Assert.IsFalse(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error));
        StringAssert.Contains("project-guid", error);
        StringAssert.Contains("Assets/Shared.asset", error);
    }

    [Test]
    public void DuplicateValidation_RejectsRuntimePackageDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new("package-guid", "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader", 2, 12)
        };

        Assert.IsFalse(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error),
            error);
        StringAssert.Contains("Lit.shader", error);
    }

    [Test]
    public void DuplicateValidation_AllowsEditorOnlyPackageDependency()
    {
        OperationMapAddressablesDuplicateDependencyReport[] duplicates =
        {
            new(
                "package-guid",
                "Packages/com.unity.shadergraph/Editor/Resources/Shaders/FallbackError.shader",
                2,
                12),
            new(
                "mixed-case-package-guid",
                "Packages/com.example.rendering/eDiToR/Shaders/FallbackError.shader",
                2,
                12)
        };

        Assert.IsTrue(
            OperationMapAddressablesBuildReportBuilder.TryValidateDuplicateDependencies(
                duplicates,
                out string error),
            error);
    }

    [Test]
    public void Publish_DoesNotRewriteIdenticalContent()
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "operation-map-addressables-build-report-test.json");
        try
        {
            Assert.IsTrue(OperationMapAddressablesBuildReportBuilder.Publish(path, "report\n"));
            DateTime firstWrite = System.IO.File.GetLastWriteTimeUtc(path);
            Assert.IsFalse(OperationMapAddressablesBuildReportBuilder.Publish(path, "report\n"));
            Assert.AreEqual(firstWrite, System.IO.File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    private static BuildLayout CreateLayout(bool reverseAssets)
    {
        BuildLayout layout = new()
        {
            BuildResultHash = "0123456789abcdef0123456789abcdef",
            BuildTarget = BuildTarget.Android
        };
        BuildLayout.Group group = new() { Name = "Operation Map" };
        layout.Groups.Add(group);

        BuildLayout.Bundle dependency = CreateBundle("shared", 300);
        BuildLayout.Bundle first = CreateBundle("first", 100);
        BuildLayout.Bundle second = CreateBundle("second", 200);
        first.Dependencies = new List<BuildLayout.Bundle> { dependency };
        second.Dependencies = new List<BuildLayout.Bundle> { dependency };
        group.Bundles.Add(first);
        group.Bundles.Add(second);
        group.Bundles.Add(dependency);

        const string partition = "operation-map-partition-skirmish-desert-base-01-region-p000-p000";
        BuildLayout.ExplicitAsset firstAsset = CreateAsset(
            first,
            "operation-map/opmap.skirmish.desert_base_01/a",
            partition);
        BuildLayout.ExplicitAsset secondAsset = CreateAsset(
            second,
            "operation-map/opmap.skirmish.desert_base_01/b",
            partition);
        if (reverseAssets)
        {
            first.Files[0].Assets.Add(secondAsset);
            secondAsset.File = first.Files[0];
            secondAsset.Bundle = first;
            second.Files[0].Assets.Add(firstAsset);
            firstAsset.File = second.Files[0];
            firstAsset.Bundle = second;
        }
        else
        {
            first.Files[0].Assets.Add(firstAsset);
            second.Files[0].Assets.Add(secondAsset);
        }

        first.Files[0].SubFiles.Add(new BuildLayout.SubFile
        {
            Name = "scene.entityheader",
            Size = 44
        });
        AddDuplicateImplicitAsset(first, "duplicate-guid", 12);
        AddDuplicateImplicitAsset(second, "duplicate-guid", 12);
        layout.DuplicatedAssets.Add(new BuildLayout.AssetDuplicationData
        {
            AssetGuid = "duplicate-guid",
            DuplicatedObjects = new List<BuildLayout.ObjectDuplicationData>
            {
                new()
                {
                    LocalIdentifierInFile = 1,
                    IncludedInBundleFiles = new List<BuildLayout.File>
                    {
                        first.Files[0],
                        second.Files[0]
                    }
                }
            }
        });
        return layout;
    }

    private static BuildLayout.Bundle CreateBundle(string name, ulong bytes)
    {
        BuildLayout.Bundle bundle = new() { Name = name, FileSize = bytes };
        BuildLayout.File file = new() { Name = name + ".file", Bundle = bundle };
        bundle.Files.Add(file);
        return bundle;
    }

    private static BuildLayout.ExplicitAsset CreateAsset(
        BuildLayout.Bundle bundle,
        string address,
        string partition)
    {
        return new BuildLayout.ExplicitAsset
        {
            AddressableName = address,
            AssetPath = "Assets/" + address.GetHashCode() + ".asset",
            Bundle = bundle,
            File = bundle.Files[0],
            Labels = new[]
            {
                OperationMapAddressablesLayoutBuilder.PackLabel,
                partition
            }
        };
    }

    private static void AddDuplicateImplicitAsset(
        BuildLayout.Bundle bundle,
        string guid,
        ulong bytes)
    {
        bundle.Files[0].OtherAssets.Add(new BuildLayout.DataFromOtherAsset
        {
            AssetGuid = guid,
            AssetPath = "Assets/Shared.asset",
            File = bundle.Files[0],
            SerializedSize = bytes
        });
    }
}
