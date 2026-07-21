using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapBuildingAttachmentOwnershipInventoryProbeTests
{
    [Test]
    public void ClassifyIntactDisposition_AssignsOnlyExactUniqueSources()
    {
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.ClassifyIntactDisposition(
                exactJoin: true,
                sourceReused: false),
            Is.EqualTo("AssignedIntact"));
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.ClassifyIntactDisposition(
                exactJoin: true,
                sourceReused: true),
            Is.EqualTo("SharedAcrossBuildings"));
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.ClassifyIntactDisposition(
                exactJoin: false,
                sourceReused: false),
            Is.EqualTo("UnresolvedAuthoredSourceJoin"));
    }

    [Test]
    public void ClassifyOrphanDisposition_IsAlwaysUnassignedWithoutRoleInference()
    {
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.ClassifyOrphanDisposition(),
            Is.EqualTo("UnassignedOrphan"));
    }

    [Test]
    public void BuildClaimKey_ChangesForEachOwnershipOrPrefabIdentityPart()
    {
        string first = OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
            "GlobalObjectId_V1-a",
            "guid-a",
            "100");
        string same = OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
            "GlobalObjectId_V1-a",
            "guid-a",
            "100");
        string differentOwner = OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
            "GlobalObjectId_V1-b",
            "guid-a",
            "100");
        string differentRenderer = OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
            "GlobalObjectId_V1-a",
            "guid-a",
            "101");

        Assert.That(same, Is.EqualTo(first));
        Assert.That(differentOwner, Is.Not.EqualTo(first));
        Assert.That(differentRenderer, Is.Not.EqualTo(first));
    }

    [Test]
    public void DestroyedClaimKeys_AllowSharedPrefabAcrossDistinctBuildingSources()
    {
        var claims = new HashSet<string>
        {
            OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
                "GlobalObjectId_V1-source-a", "shared-prefab-guid", "42")
        };

        Assert.That(
            claims.Add(OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
                "GlobalObjectId_V1-source-b", "shared-prefab-guid", "42")),
            Is.True);
        Assert.That(
            claims.Add(OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildClaimKey(
                "GlobalObjectId_V1-source-a", "shared-prefab-guid", "42")),
            Is.False);
    }

    [Test]
    public void HasRequiredReportShape_RequiresSchemaCountsPlacementsAndAttachments()
    {
        var report =
            new OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport
            {
                reportSchema = OperationMapBuildingAttachmentOwnershipInventoryProbe.ReportSchema,
                reportSchemaVersion =
                    OperationMapBuildingAttachmentOwnershipInventoryProbe.ReportSchemaVersion,
                result = "AttachmentOwnershipInventoryComplete",
                counts =
                    new OperationMapBuildingAttachmentOwnershipInventoryProbe
                        .AttachmentOwnershipCountsReport(),
                dispositionCounts =
                    new List<OperationMapBuildingAttachmentOwnershipInventoryProbe
                        .DispositionCountReport>(),
                placements =
                    new List<OperationMapBuildingAttachmentOwnershipInventoryProbe
                        .BuildingPlacementReport>(),
                attachments =
                    new List<OperationMapBuildingAttachmentOwnershipInventoryProbe
                        .AttachmentOwnershipReport>()
            };

        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.True);

        report.reportSchema = "wrong";
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.False);
    }

    [Test]
    public void IsSuccessResult_RecognizesOnlyCompleteInventoryResult()
    {
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.IsSuccessResult(
                "AttachmentOwnershipInventoryComplete"),
            Is.True);
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.IsSuccessResult(
                "AttachmentOwnershipHasOrphansOrConflicts"),
            Is.False);
        Assert.That(
            OperationMapBuildingAttachmentOwnershipInventoryProbe.IsSuccessResult(
                "UnresolvedBuildingJoinsPendingReview"),
            Is.False);
    }
}
