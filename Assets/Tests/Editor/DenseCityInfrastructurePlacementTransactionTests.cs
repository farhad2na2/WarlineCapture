using System;
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
