using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityBuildingPlacementTransactionTests
{
    private const string IntactGuid = "0123456789abcdef0123456789abcdef";
    private const string DestroyedGuid = "fedcba9876543210fedcba9876543210";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [Test]
    public void TryCommitAndRealize_KeepsFiveRecordsAfterAcceptedRealization()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 2);
        DenseCityBuildingRecordGroup group = CreateGroup();

        bool accepted = DenseCityBuildingPlacementTransaction.TryCommitAndRealize(
            records,
            group,
            () => true);
        records.Seal();

        Assert.That(accepted, Is.True);
        Assert.That(records.Buildings, Has.Count.EqualTo(1));
        Assert.That(records.Surfaces, Has.Count.EqualTo(2));
        Assert.That(records.Presentations, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryCommitAndRealize_RemovesFiveRecordsAfterRejectedRealization()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 2);
        DenseCityBuildingRecordGroup group = CreateGroup();

        bool accepted = DenseCityBuildingPlacementTransaction.TryCommitAndRealize(
            records,
            group,
            () => false);
        records.Seal();

        Assert.That(accepted, Is.False);
        Assert.That(records.Buildings, Is.Empty);
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void TryCommitAndRealize_RemovesFiveRecordsAndRethrowsRealizationFailure()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 2);
        DenseCityBuildingRecordGroup group = CreateGroup();

        Assert.That(
            () => DenseCityBuildingPlacementTransaction.TryCommitAndRealize(
                records,
                group,
                () => throw new InvalidOperationException("realization failed")),
            Throws.InvalidOperationException.With.Message.EqualTo("realization failed"));
        records.Seal();

        Assert.That(records.Buildings, Is.Empty);
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void Create_WithCivicPrefix_LabelsEveryAtomicBuildingRecord()
    {
        DenseCityBuildingRecordGroup group = CreateGroup("civic");

        Assert.That(group.Building.Identity.Kind, Is.EqualTo("civic-building"));
        Assert.That(group.Foundation.Identity.Kind, Is.EqualTo("civic-foundation"));
        Assert.That(group.Blocker.Identity.Kind, Is.EqualTo("civic-blocker"));
        Assert.That(group.IntactPresentation.Identity.Kind, Is.EqualTo("civic-building-intact"));
        Assert.That(group.DestroyedPresentation.Identity.Kind, Is.EqualTo("civic-building-destroyed"));
    }

    private static DenseCityBuildingRecordGroup CreateGroup(string identityKindPrefix = null) =>
        DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                3,
                20,
                IntactGuid,
                123,
                DestroyedGuid,
                456,
                new[] { MaterialGuid },
                new[] { MaterialGuid },
                Matrix4x4.TRS(new Vector3(10f, 2f, 20f), Quaternion.identity, Vector3.one),
                new Vector2Int(6, 9),
                new Vector2Int(8, 6),
                new Vector2(8f, 6f),
                2f,
                new Bounds(new Vector3(10f, 4f, 20f), new Vector3(8f, 4f, 6f)),
                Vector3.forward,
                0,
                500f,
                1,
                0,
                new Vector2Int(1, 2),
                identityKindPrefix));
}
