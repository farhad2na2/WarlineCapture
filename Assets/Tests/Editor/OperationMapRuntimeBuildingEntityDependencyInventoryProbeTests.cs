using System.Collections.Generic;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapRuntimeBuildingEntityDependencyInventoryProbeTests
{
    [Test]
    public void ClassifyPlacementDisposition_PrioritizesUnresolvedJoin()
    {
        string disposition =
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.ClassifyPlacementDisposition(
                exactAuthoredJoin: false,
                hasBuildingPrefab: false,
                hasBuildingDefinitionAuthoring: false);

        Assert.That(disposition, Is.EqualTo("UnresolvedAuthoredSourceJoin"));
    }

    [Test]
    public void ClassifyPlacementDisposition_ReportsMissingPrefabThenAuthoring()
    {
        Assert.That(
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.ClassifyPlacementDisposition(
                exactAuthoredJoin: true,
                hasBuildingPrefab: false,
                hasBuildingDefinitionAuthoring: false),
            Is.EqualTo("MissingBuildingPrefab"));
        Assert.That(
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.ClassifyPlacementDisposition(
                exactAuthoredJoin: true,
                hasBuildingPrefab: true,
                hasBuildingDefinitionAuthoring: false),
            Is.EqualTo("MissingBuildingDefinitionAuthoring"));
    }

    [Test]
    public void ClassifyPlacementDisposition_RequiresManagedRuntimeWhenInputsArePresent()
    {
        string disposition =
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.ClassifyPlacementDisposition(
                exactAuthoredJoin: true,
                hasBuildingPrefab: true,
                hasBuildingDefinitionAuthoring: true);

        Assert.That(disposition, Is.EqualTo("RequiresManagedRuntimeBuildingEntity"));
        Assert.That(
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.IsManagedDependencyDisposition(
                disposition),
            Is.True);
    }

    [Test]
    public void BuildDependencyCatalog_IsNonEmptyAndHasDeterministicRequiredIds()
    {
        List<OperationMapRuntimeBuildingEntityDependencyInventoryProbe.DependencyCatalogEntry> first =
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.BuildDependencyCatalog();
        List<OperationMapRuntimeBuildingEntityDependencyInventoryProbe.DependencyCatalogEntry> second =
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.BuildDependencyCatalog();

        Assert.That(first, Is.Not.Empty);
        Assert.That(first.Select(entry => entry.dependencyId), Is.EqualTo(
            second.Select(entry => entry.dependencyId)));
        Assert.That(first.Select(entry => entry.dependencyId), Is.EqualTo(new[]
        {
            "instance-presentation-hierarchy",
            "faction-visual-materials",
            "door-open-state",
            "intact-destroyed-visuals",
            "animated-resource-visuals",
            "production-queues-and-slots",
            "production-transport-visuals",
            "runtime-building-transform-sync",
            "runway-transform-discovery",
            "selection-focus-transform"
        }));
    }

    [Test]
    public void HasRequiredReportShape_RequiresSchemaCatalogCountsAndPlacements()
    {
        var report =
            new OperationMapRuntimeBuildingEntityDependencyInventoryProbe.DependencyInventoryReport
            {
                reportSchema = OperationMapRuntimeBuildingEntityDependencyInventoryProbe.ReportSchema,
                reportSchemaVersion =
                    OperationMapRuntimeBuildingEntityDependencyInventoryProbe.ReportSchemaVersion,
                result = "AllPlacementsRequireManagedRuntimeBuildingEntity",
                dependencyCatalog =
                    OperationMapRuntimeBuildingEntityDependencyInventoryProbe.BuildDependencyCatalog(),
                counts =
                    new OperationMapRuntimeBuildingEntityDependencyInventoryProbe
                        .DependencyInventoryCountsReport(),
                dispositionCounts =
                    new List<OperationMapRuntimeBuildingEntityDependencyInventoryProbe
                        .DispositionCountReport>(),
                placements =
                    new List<OperationMapRuntimeBuildingEntityDependencyInventoryProbe
                        .PlacementDependencyReport>()
            };

        Assert.That(
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.True);

        report.reportSchema = "wrong";
        Assert.That(
            OperationMapRuntimeBuildingEntityDependencyInventoryProbe.HasRequiredReportShape(
                JsonUtility.ToJson(report)),
            Is.False);
    }
}
