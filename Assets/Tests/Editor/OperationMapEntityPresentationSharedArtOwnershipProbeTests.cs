using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapEntityPresentationSharedArtOwnershipProbeTests
{
    [Test]
    public void TryBuildSharedArtReport_ProvesCompactSharedOwnershipWithoutAssetDatabase()
    {
        var inventory = new OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport
        {
            counts = new OperationMapEntityPresentationMigrationInventoryProbe.InventoryCountsReport
            {
                sourceCount = 3
            },
            sources = new List<OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport>
            {
                CreateSource("mesh-a", "mat-a", "prefab-a"),
                CreateSource("mesh-a", "mat-a", "prefab-a"),
                CreateSource("mesh-b", "mat-b", "prefab-b")
            }
        };

        Assert.That(
            OperationMapEntityPresentationSharedArtOwnershipProbe.TryBuildSharedArtReport(
                inventory,
                out OperationMapEntityPresentationSharedArtOwnershipProbe.SharedArtOwnershipReport report,
                out string rejectionReason,
                resolveAssetsInAssetDatabase: false),
            Is.True,
            rejectionReason);
        Assert.That(report.result, Is.EqualTo("SharedArtOwnershipProven"));
        Assert.That(report.sourceCount, Is.EqualTo(3));
        Assert.That(report.uniqueMeshAssetCount, Is.EqualTo(2));
        Assert.That(report.meshPlacementReferenceCount, Is.EqualTo(3));
        Assert.That(report.repeatedMeshAssetCount, Is.EqualTo(1));
        Assert.That(report.uniqueMaterialAssetCount, Is.EqualTo(2));
        Assert.That(report.uniquePrefabAssetCount, Is.EqualTo(2));
        Assert.That(report.missingAssetCount, Is.EqualTo(0));
        Assert.That(report.compactInstanceDataProven, Is.True);
    }

    private static OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport CreateSource(
        string mesh,
        string material,
        string prefab)
    {
        return new OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport
        {
            meshAssetGuid = mesh,
            materialGuids = new List<string> { material },
            prefabAssetGuid = prefab
        };
    }
}
