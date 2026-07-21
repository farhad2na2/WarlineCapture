using Game.Editor;
using NUnit.Framework;

public sealed class OperationMapVehicleEcsConversionInventoryProbeTests
{
    [Test]
    public void ClassifyConversionDisposition_ReadyOnlyWhenJoinPrefabAuthoringRenderAndHideArePresent()
    {
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: true,
                hasModelVisualRoot: true,
                hasModelRenderers: true,
                hasDestroyedVisualPrefab: true,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("AlreadyProducesEcsGameplayAndRender"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: true,
                hasModelVisualRoot: true,
                hasModelRenderers: true,
                hasDestroyedVisualPrefab: false,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("AlreadyProducesEcsMissingDestroyedVisual"));
    }

    [Test]
    public void ClassifyConversionDisposition_FailClosedForMissingJoinPrefabAuthoringOrRender()
    {
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: false,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: true,
                hasModelVisualRoot: true,
                hasModelRenderers: true,
                hasDestroyedVisualPrefab: true,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("UnresolvedAuthoredSourceJoin"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: false,
                hasUnitGridAuthoring: false,
                usesVehicleMotion: false,
                hasModelVisualRoot: false,
                hasModelRenderers: false,
                hasDestroyedVisualPrefab: false,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("MissingVehiclePrefab"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: false,
                usesVehicleMotion: false,
                hasModelVisualRoot: false,
                hasModelRenderers: false,
                hasDestroyedVisualPrefab: false,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("MissingUnitGridAuthoring"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: false,
                hasModelVisualRoot: true,
                hasModelRenderers: true,
                hasDestroyedVisualPrefab: true,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("UnitGridAuthoringNotVehicleMotion"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: true,
                hasModelVisualRoot: false,
                hasModelRenderers: false,
                hasDestroyedVisualPrefab: true,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: true),
            Is.EqualTo("MissingModelRenderEntityRoot"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: true,
                hasModelVisualRoot: true,
                hasModelRenderers: true,
                hasDestroyedVisualPrefab: true,
                hideAuthoringAfterSpawn: false,
                hasSourceKey: true),
            Is.EqualTo("DuplicateAuthoringVisualRisk"));

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.ClassifyConversionDisposition(
                exactAuthoredJoin: true,
                hasVehiclePrefab: true,
                hasUnitGridAuthoring: true,
                usesVehicleMotion: true,
                hasModelVisualRoot: true,
                hasModelRenderers: true,
                hasDestroyedVisualPrefab: true,
                hideAuthoringAfterSpawn: true,
                hasSourceKey: false),
            Is.EqualTo("MissingVehicleSourceKey"));
    }

    [Test]
    public void AlreadyReadyAndCleanupHelpers_PartitionDispositionsWithoutNameInference()
    {
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.IsAlreadyReadyDisposition(
                "AlreadyProducesEcsGameplayAndRender"),
            Is.True);
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.IsAlreadyReadyDisposition(
                "AlreadyProducesEcsMissingDestroyedVisual"),
            Is.True);
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.IsCleanupRequiredDisposition(
                "MissingUnitGridAuthoring"),
            Is.True);
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.IsCleanupRequiredDisposition(
                "UnresolvedAuthoredSourceJoin"),
            Is.False);
    }

    [Test]
    public void HasRequiredReportShape_RequiresSchemaCountsAndPlacements()
    {
        var report = new OperationMapVehicleEcsConversionInventoryProbe.ConversionReport
        {
            reportSchema = OperationMapVehicleEcsConversionInventoryProbe.ReportSchema,
            reportSchemaVersion = OperationMapVehicleEcsConversionInventoryProbe.ReportSchemaVersion,
            result = "AllPlacementsAlreadyProduceEcs",
            counts = new OperationMapVehicleEcsConversionInventoryProbe.ConversionCountsReport(),
            dispositionCounts =
                new System.Collections.Generic.List<
                    OperationMapVehicleEcsConversionInventoryProbe.DispositionCountReport>(),
            placements =
                new System.Collections.Generic.List<
                    OperationMapVehicleEcsConversionInventoryProbe.PlacementConversionReport>()
        };

        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.HasRequiredReportShape(
                UnityEngine.JsonUtility.ToJson(report)),
            Is.True);

        report.reportSchema = "wrong";
        Assert.That(
            OperationMapVehicleEcsConversionInventoryProbe.HasRequiredReportShape(
                UnityEngine.JsonUtility.ToJson(report)),
            Is.False);
    }
}
