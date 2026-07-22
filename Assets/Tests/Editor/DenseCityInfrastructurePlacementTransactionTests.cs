using System;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityInfrastructurePlacementTransactionTests
{
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [Test]
    public void InfrastructureTransaction_KeepsPairedRecordsAfterAcceptedRealization()
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        DenseCityInfrastructureRecordGroup group = CreateInfrastructureGroup();

        bool accepted = DenseCityInfrastructurePlacementTransaction.TryCommitAndRealize(
            records,
            group,
            () => true);
        records.Seal();

        Assert.That(accepted, Is.True);
        Assert.That(records.Surfaces, Has.Count.EqualTo(1));
        Assert.That(records.Presentations, Has.Count.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InfrastructureTransaction_RollsBackPairedRecords(bool throwDuringRealization)
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        DenseCityInfrastructureRecordGroup group = CreateInfrastructureGroup();

        if (throwDuringRealization)
        {
            Assert.That(
                () => DenseCityInfrastructurePlacementTransaction.TryCommitAndRealize(
                    records,
                    group,
                    () => throw new InvalidOperationException("realization failed")),
                Throws.InvalidOperationException.With.Message.EqualTo("realization failed"));
        }
        else
        {
            Assert.That(
                DenseCityInfrastructurePlacementTransaction.TryCommitAndRealize(
                    records,
                    group,
                    () => false),
                Is.False);
        }

        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void InfrastructureGroup_PreflightRejectsNonInfrastructurePresentationWithoutMutation()
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        DenseCityInfrastructureRecordGroup valid = CreateInfrastructureGroup();
        var invalidPresentation = new DenseCityPresentationBakeRecord(
            valid.Presentation.Identity,
            DenseCityPresentationCategory.Prop,
            SourceGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            1);

        Assert.That(
            () => records.AddInfrastructureGroup(valid.Surface, invalidPresentation),
            Throws.ArgumentException);
        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void SurfaceTransaction_SupportsSurfaceOnlyRampAndRollsBackRejection()
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        DenseCitySurfaceBakeRecord ramp = CreateSurface(0, "ramp", DenseCitySurfaceRecordKind.Ramp);

        bool accepted = DenseCitySurfacePlacementTransaction.TryCommitAndRealize(
            records,
            ramp,
            () => false);
        records.Seal();

        Assert.That(accepted, Is.False);
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void BridgeTransaction_KeepsBridgePresentationAndBothApproachesAfterAcceptedRealization()
    {
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityBridgeRecordGroup group = CreateBridgeGroup();

        bool accepted = DenseCityBridgePlacementTransaction.TryCommitAndRealize(
            records,
            group,
            () => true);
        records.Seal();

        Assert.That(accepted, Is.True);
        Assert.That(records.Surfaces, Has.Count.EqualTo(3));
        Assert.That(records.Presentations, Has.Count.EqualTo(1));
        Assert.That(
            records.Surfaces.Count(record => record.Kind == DenseCitySurfaceRecordKind.Bridge),
            Is.EqualTo(1));
        Assert.That(
            records.Surfaces.Count(record => record.Kind == DenseCitySurfaceRecordKind.Ramp),
            Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BridgeTransaction_RollsBackAllRecords(bool throwDuringRealization)
    {
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityBridgeRecordGroup group = CreateBridgeGroup();

        if (throwDuringRealization)
        {
            Assert.That(
                () => DenseCityBridgePlacementTransaction.TryCommitAndRealize(
                    records,
                    group,
                    () => throw new InvalidOperationException("bridge realization failed")),
                Throws.InvalidOperationException.With.Message.EqualTo("bridge realization failed"));
        }
        else
        {
            Assert.That(
                DenseCityBridgePlacementTransaction.TryCommitAndRealize(records, group, () => false),
                Is.False);
        }

        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void BridgeGroup_PreflightRejectsInvalidSecondApproachWithoutMutation()
    {
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityBridgeRecordGroup valid = CreateBridgeGroup();
        DenseCitySurfaceBakeRecord invalidSecondApproach =
            CreateSurface(3, "bridge-ramp-b", DenseCitySurfaceRecordKind.Road);

        Assert.That(
            () => records.AddBridgeGroup(
                valid.Bridge,
                valid.Presentation,
                valid.FirstApproachRamp,
                invalidSecondApproach),
            Throws.ArgumentException);
        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void RoadTransaction_KeepsRoadPresentationAndShouldersAfterAcceptedRealization()
    {
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityRoadRecordGroup group = CreateRoadGroup();

        bool accepted = DenseCityRoadPlacementTransaction.TryCommitAndRealize(
            records,
            group,
            () => true);
        records.Seal();

        Assert.That(accepted, Is.True);
        Assert.That(records.Surfaces, Has.Count.EqualTo(3));
        Assert.That(records.Presentations, Has.Count.EqualTo(1));
        Assert.That(records.Surfaces.Count(record => record.Kind == DenseCitySurfaceRecordKind.Road), Is.EqualTo(1));
        Assert.That(records.Surfaces.Count(record => record.Kind == DenseCitySurfaceRecordKind.Terrain), Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RoadTransaction_RollsBackWholeGroup(bool throwDuringRealization)
    {
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityRoadRecordGroup group = CreateRoadGroup();

        if (throwDuringRealization)
        {
            Assert.That(
                () => DenseCityRoadPlacementTransaction.TryCommitAndRealize(
                    records,
                    group,
                    () => throw new InvalidOperationException("road realization failed")),
                Throws.InvalidOperationException.With.Message.EqualTo("road realization failed"));
        }
        else
        {
            Assert.That(
                DenseCityRoadPlacementTransaction.TryCommitAndRealize(records, group, () => false),
                Is.False);
        }

        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void RoadGroup_PreflightRejectsNonTerrainShoulderWithoutMutation()
    {
        using var records = new DenseCityGenerationRecordSet(1, 3, 1);
        DenseCityRoadRecordGroup valid = CreateRoadGroup();
        var invalidShoulders = new[]
        {
            valid.Shoulders[0],
            CreateSurface(3, "road-shoulder", DenseCitySurfaceRecordKind.Road)
        };

        Assert.That(
            () => records.AddRoadGroup(valid.Road, valid.Presentation, invalidShoulders),
            Throws.ArgumentException);
        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    private static DenseCityInfrastructureRecordGroup CreateInfrastructureGroup()
    {
        DenseCitySurfaceBakeRecord surface = CreateSurface(0, "road", DenseCitySurfaceRecordKind.Road);
        var presentation = new DenseCityPresentationBakeRecord(
            CreateIdentity(1, "road-visual"),
            DenseCityPresentationCategory.Infrastructure,
            SourceGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            2);
        return new DenseCityInfrastructureRecordGroup(surface, presentation);
    }

    private static DenseCityBridgeRecordGroup CreateBridgeGroup()
    {
        DenseCitySurfaceBakeRecord bridge = CreateSurface(0, "bridge", DenseCitySurfaceRecordKind.Bridge);
        var presentation = new DenseCityPresentationBakeRecord(
            CreateIdentity(1, "bridge-visual"),
            DenseCityPresentationCategory.Infrastructure,
            SourceGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            4);
        DenseCitySurfaceBakeRecord firstApproach =
            CreateSurface(2, "bridge-ramp-a", DenseCitySurfaceRecordKind.Ramp);
        DenseCitySurfaceBakeRecord secondApproach =
            CreateSurface(3, "bridge-ramp-b", DenseCitySurfaceRecordKind.Ramp);
        return new DenseCityBridgeRecordGroup(bridge, presentation, firstApproach, secondApproach);
    }

    private static DenseCityRoadRecordGroup CreateRoadGroup()
    {
        DenseCityInfrastructureRecordGroup road = CreateInfrastructureGroup();
        return new DenseCityRoadRecordGroup(
            road.Surface,
            road.Presentation,
            new[]
            {
                CreateSurface(2, "road-shoulder", DenseCitySurfaceRecordKind.Terrain),
                CreateSurface(3, "road-shoulder", DenseCitySurfaceRecordKind.Terrain)
            });
    }

    private static DenseCitySurfaceBakeRecord CreateSurface(
        int sequence,
        string kind,
        DenseCitySurfaceRecordKind recordKind) =>
        new(
            CreateIdentity(sequence, kind),
            recordKind,
            new[]
            {
                new Vector2(-1f, -1f),
                new Vector2(1f, -1f),
                new Vector2(1f, 1f),
                new Vector2(-1f, 1f)
            },
            0f,
            1,
            0,
            Vector2Int.zero);

    private static DenseCityRecordIdentity CreateIdentity(int sequence, string kind) =>
        new("dense-city-v1", 42, 3, kind, sequence, SourceGuid, 123);
}
