using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapEntityPresentationOwnerClassificationProbeTests
{
    [TestCase(true, false, false, false, false, false,
        OperationMapEntityPresentationOwnerClassificationProbe.GameplayBuilding)]
    [TestCase(false, true, false, false, false, false,
        OperationMapEntityPresentationOwnerClassificationProbe.GameplayVehicle)]
    [TestCase(false, false, false, false, false, false,
        OperationMapEntityPresentationOwnerClassificationProbe.RenderOnlyEntity)]
    [TestCase(true, false, true, false, false, false,
        OperationMapEntityPresentationOwnerClassificationProbe.RejectedUnresolved)]
    [TestCase(true, true, false, false, false, false,
        OperationMapEntityPresentationOwnerClassificationProbe.RejectedUnresolved)]
    [TestCase(false, false, false, false, true, false,
        OperationMapEntityPresentationOwnerClassificationProbe.RejectedUnresolved)]
    [TestCase(false, false, false, true, false, false,
        OperationMapEntityPresentationOwnerClassificationProbe.RejectedUnresolved)]
    public void ClassifyOwnerRole_FailsClosedForInvalidEvidence(
        bool building,
        bool vehicle,
        bool mixed,
        bool unresolved,
        bool blocking,
        bool externalReference,
        string expected)
    {
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe.ClassifyOwnerRole(
                building, vehicle, mixed, unresolved, blocking, externalReference),
            Is.EqualTo(expected));
    }

    [Test]
    public void RequiresApprovedManagedBoundaryUntilEcsCutover_OnlyRequiresBuildings()
    {
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe
                .RequiresApprovedManagedBoundaryUntilEcsCutover(
                    OperationMapEntityPresentationOwnerClassificationProbe.GameplayBuilding),
            Is.True);
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe
                .RequiresApprovedManagedBoundaryUntilEcsCutover(
                    OperationMapEntityPresentationOwnerClassificationProbe.GameplayVehicle),
            Is.False);
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe
                .RequiresApprovedManagedBoundaryUntilEcsCutover(
                    OperationMapEntityPresentationOwnerClassificationProbe.RenderOnlyEntity),
            Is.False);
    }

    [Test]
    public void Catalogs_AreDeterministicAndContainRequiredIds()
    {
        List<OperationMapEntityPresentationOwnerClassificationProbe.CatalogEntry> boundary =
            OperationMapEntityPresentationOwnerClassificationProbe.BuildApprovedManagedBoundaryCatalog();
        List<OperationMapEntityPresentationOwnerClassificationProbe.CatalogEntry> proxy =
            OperationMapEntityPresentationOwnerClassificationProbe.BuildMapMetadataProxyCatalog();

        Assert.That(boundary.ConvertAll(entry => entry.proxyId), Is.Ordered);
        Assert.That(proxy.ConvertAll(entry => entry.proxyId), Is.Ordered);
        Assert.That(boundary.Exists(entry =>
            entry.proxyId == "runtime-building-entity-interim-presentation" &&
            !entry.isApprovedTransientBoundary), Is.True);
        Assert.That(boundary.Exists(entry =>
            entry.proxyId == "production-transport-drop-visuals" &&
            entry.isApprovedTransientBoundary), Is.True);
        Assert.That(boundary.Exists(entry =>
            entry.proxyId == "door-open-state-fx" && entry.isApprovedTransientBoundary), Is.True);
        Assert.That(proxy.ConvertAll(entry => entry.proxyId), Does.Contain("grid-config-surface-metadata"));
        Assert.That(proxy.TrueForAll(entry => !entry.isVisualMigrationOwner), Is.True);
    }

    [Test]
    public void HasRequiredReportShape_RequiresClassificationCollections()
    {
        var report = new OperationMapEntityPresentationOwnerClassificationProbe.OwnerClassificationReport
        {
            reportSchema = OperationMapEntityPresentationOwnerClassificationProbe.ReportSchema,
            reportSchemaVersion =
                OperationMapEntityPresentationOwnerClassificationProbe.ReportSchemaVersion,
            result = "OwnerClassificationComplete",
            counts =
                new OperationMapEntityPresentationOwnerClassificationProbe.OwnerClassificationCounts(),
            ownerRows =
                new List<OperationMapEntityPresentationOwnerClassificationProbe.OwnerClassificationRow>(),
            countsByRole =
                new List<OperationMapEntityPresentationOwnerClassificationProbe.RoleCount>(),
            approvedManagedBoundaryCatalog =
                new List<OperationMapEntityPresentationOwnerClassificationProbe.CatalogEntry>(),
            mapMetadataProxyCatalog =
                new List<OperationMapEntityPresentationOwnerClassificationProbe.CatalogEntry>()
        };

        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.True);

        report.reportSchema = "wrong";
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.False);
    }

    [Test]
    public void IsSuccessResult_OnlyAcceptsCompleteResult()
    {
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe.IsSuccessResult(
                "OwnerClassificationComplete"),
            Is.True);
        Assert.That(
            OperationMapEntityPresentationOwnerClassificationProbe.IsSuccessResult(
                "OwnerClassificationHasRejectedOwners"),
            Is.False);
    }
}
