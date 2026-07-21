using System.Collections.Generic;
using System.Linq;
using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapRenderOnlyCandidateMigrationPlannerTests
{
    private const string OwnerA =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1000-0";
    private const string OwnerB =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2000-0";

    [Test]
    public void TryAssignBucket_UsesAuthoredMapChildFolderOnly()
    {
        Assert.That(
            OperationMapRenderOnlyCandidateMigrationPlanner.TryAssignBucket(
                "Map/Roads/SM_Road_01",
                out string folder,
                out string bucket,
                out string rejectionReason),
            Is.True,
            rejectionReason);
        Assert.That(folder, Is.EqualTo("Roads"));
        Assert.That(bucket, Is.EqualTo(OperationMapRenderOnlyCandidateMigrationPlanner.BucketRoadsAndBridges));

        Assert.That(
            OperationMapRenderOnlyCandidateMigrationPlanner.TryAssignBucket(
                "Map/UnknownFolder/Thing",
                out _,
                out _,
                out rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Is.EqualTo("unapproved-map-child-folder:UnknownFolder"));
    }

    [Test]
    public void TryCreatePlan_IsDeterministicAndCoversApprovedFolders()
    {
        var owners = new List<OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport>
        {
            CreateOwner(OwnerB, "Map/Trees/Tree_01"),
            CreateOwner(OwnerA, "Map/Ground/Terrain_01")
        };

        Assert.That(
            OperationMapRenderOnlyCandidateMigrationPlanner.TryCreatePlan(
                owners,
                out OperationMapRenderOnlyCandidateMigrationPlan plan,
                out string rejectionReason),
            Is.True,
            rejectionReason);

        Assert.That(plan.Status, Is.EqualTo("RenderOnlyCopyPlanReadyPendingCandidateHierarchy"));
        Assert.That(plan.OwnerCount, Is.EqualTo(2));
        Assert.That(
            plan.Assignments.Select(assignment => assignment.SourceOwnerGlobalObjectId),
            Is.EqualTo(new[] { OwnerA, OwnerB }));
        Assert.That(plan.Assignments[0].DestinationBucket,
            Is.EqualTo(OperationMapRenderOnlyCandidateMigrationPlanner.BucketTerrain));
        Assert.That(plan.Assignments[1].DestinationBucket,
            Is.EqualTo(OperationMapRenderOnlyCandidateMigrationPlanner.BucketVegetation));
        Assert.That(
            plan.CountsByBucket.Single(entry =>
                entry.Name == OperationMapRenderOnlyCandidateMigrationPlanner.BucketTerrain).Count,
            Is.EqualTo(1));
        Assert.That(
            plan.CountsByBucket.Single(entry =>
                entry.Name == OperationMapRenderOnlyCandidateMigrationPlanner.BucketVegetation).Count,
            Is.EqualTo(1));
    }

    [Test]
    public void TryCreatePlan_RejectsNonRenderOnlyDisposition()
    {
        var owners = new List<OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport>
        {
            CreateOwner(OwnerA, "Map/Props/Prop_01", disposition: "GameplayBuildingCandidate")
        };

        Assert.That(
            OperationMapRenderOnlyCandidateMigrationPlanner.TryCreatePlan(
                owners,
                out _,
                out string rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain("disposition-not-render-only"));
    }

    private static OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport CreateOwner(
        string globalObjectId,
        string nameHierarchyPath,
        string disposition = "RenderOnlyEntityCandidate")
    {
        return new OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport
        {
            globalObjectId = globalObjectId,
            nameHierarchyPath = nameHierarchyPath,
            hierarchyPath = "Map[5]/Owner[0]",
            candidateDisposition = disposition,
            sourceRendererCount = 1,
            hierarchyObjectCount = 1
        };
    }
}
