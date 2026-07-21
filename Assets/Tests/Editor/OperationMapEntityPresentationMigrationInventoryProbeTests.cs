using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapEntityPresentationMigrationInventoryProbeTests
{
    [Test]
    public void ClassifyWithoutNameInference_UsesExactJoinsAndProtectedIdentityOnly()
    {
        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.ClassifyWithoutNameInference(
                buildingJoinExact: true,
                vehicleJoinExact: false,
                underProtectedRoot: true,
                buildingJoinCount: 1,
                vehicleJoinCount: 0),
            Is.EqualTo("GameplayBuildingCandidate"));

        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.ClassifyWithoutNameInference(
                buildingJoinExact: false,
                vehicleJoinExact: true,
                underProtectedRoot: false,
                buildingJoinCount: 1,
                vehicleJoinCount: 1),
            Is.EqualTo("GameplayVehicleCandidate"));

        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.ClassifyWithoutNameInference(
                buildingJoinExact: false,
                vehicleJoinExact: false,
                underProtectedRoot: true,
                buildingJoinCount: 0,
                vehicleJoinCount: 0),
            Is.EqualTo("ProtectedAuthoredCandidate"));

        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.ClassifyWithoutNameInference(
                buildingJoinExact: true,
                vehicleJoinExact: true,
                underProtectedRoot: false,
                buildingJoinCount: 1,
                vehicleJoinCount: 1),
            Is.EqualTo("MixedOrAmbiguous"));

        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.ClassifyWithoutNameInference(
                buildingJoinExact: false,
                vehicleJoinExact: false,
                underProtectedRoot: false,
                buildingJoinCount: 0,
                vehicleJoinCount: 0),
            Is.EqualTo("UnresolvedPendingReview"));
    }

    [Test]
    public void HasRequiredReportShape_RequiresIdentityCountsAndCollections()
    {
        var report = new OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport
        {
            reportSchema = OperationMapEntityPresentationMigrationInventoryProbe.ReportSchema,
            reportSchemaVersion = OperationMapEntityPresentationMigrationInventoryProbe.ReportSchemaVersion,
            result = "InventoryCompletePendingReview",
            counts = new OperationMapEntityPresentationMigrationInventoryProbe.InventoryCountsReport(),
            protectedRoots =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.ProtectedRootReport>(),
            classificationCounts =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.ClassificationCountReport>(),
            owners =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport>(),
            placementJoins =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.PlacementJoinReport>(),
            sources =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport>()
        };

        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.True);

        report.reportSchema = "wrong";
        Assert.That(
            OperationMapEntityPresentationMigrationInventoryProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.False);
    }
}
