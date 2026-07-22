using System;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityRenderOnlyPresentationRecordTests
{
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private const string MaterialGuid = "11111111111111111111111111111111";

    [TestCase((int)DenseCityPresentationCategory.Infrastructure)]
    [TestCase((int)DenseCityPresentationCategory.Vegetation)]
    [TestCase((int)DenseCityPresentationCategory.Prop)]
    [TestCase((int)DenseCityPresentationCategory.Horizon)]
    public void Factory_AcceptsOnlyIndependentRenderCategories(int categoryValue)
    {
        var category = (DenseCityPresentationCategory)categoryValue;
        DenseCityPresentationBakeRecord record = DenseCityRenderOnlyPresentationRecordFactory.Create(
            CreateInput(7, category));

        Assert.That(record.Identity.Kind, Is.EqualTo("canal-detail-visual"));
        Assert.That(record.Identity.DeterministicSequence, Is.EqualTo(7));
        Assert.That(record.Category, Is.EqualTo(category));
    }

    [TestCase((int)DenseCityPresentationCategory.GameplayBuildingIntact)]
    [TestCase((int)DenseCityPresentationCategory.BuildingAttachmentIntact)]
    public void Factory_RejectsGameplayOwnedCategories(int categoryValue)
    {
        var category = (DenseCityPresentationCategory)categoryValue;
        Assert.That(
            () => DenseCityRenderOnlyPresentationRecordFactory.Create(CreateInput(0, category)),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Transaction_RollsBackRejectedOrExceptionalRealization(bool throwDuringRealization)
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        DenseCityPresentationBakeRecord presentation =
            DenseCityRenderOnlyPresentationRecordFactory.Create(
                CreateInput(0, DenseCityPresentationCategory.Vegetation));

        if (throwDuringRealization)
        {
            Assert.That(
                () => DenseCityRenderOnlyPresentationPlacementTransaction.TryCommitAndRealize(
                    records,
                    presentation,
                    () => throw new InvalidOperationException("detail realization failed")),
                Throws.InvalidOperationException.With.Message.EqualTo("detail realization failed"));
        }
        else
        {
            Assert.That(
                DenseCityRenderOnlyPresentationPlacementTransaction.TryCommitAndRealize(
                    records,
                    presentation,
                    () => false),
                Is.False);
        }

        records.Seal();
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void Context_SharesSequenceWithInfrastructureRecords()
    {
        using var context = new DenseCityGenerationTransactionContext(1, 1, 2);
        DenseCityPresentationBakeRecord detail = default;
        Assert.That(
            context.TryPlaceRenderOnlyPresentation(
                3,
                sequence => detail = DenseCityRenderOnlyPresentationRecordFactory.Create(
                    CreateInput(sequence, DenseCityPresentationCategory.Prop)),
                () => true),
            Is.True);

        DenseCitySurfaceBakeRecord surface = default;
        Assert.That(
            context.TryPlaceSurface(
                3,
                sequence => surface = new DenseCitySurfaceBakeRecord(
                    new DenseCityRecordIdentity(
                        "dense-city-v1", 5, 3, "terrain", sequence, SourceGuid, 1),
                    DenseCitySurfaceRecordKind.Terrain,
                    new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                    0f,
                    1,
                    0,
                    Vector2Int.zero),
                () => true),
            Is.True);

        Assert.That(detail.Identity.DeterministicSequence, Is.Zero);
        Assert.That(surface.Identity.DeterministicSequence, Is.EqualTo(1));
    }

    private static DenseCityRenderOnlyPresentationRecordInput CreateInput(
        int sequence,
        DenseCityPresentationCategory category) =>
        new(
            "dense-city-v1",
            5,
            3,
            sequence,
            "canal-detail-visual",
            category,
            SourceGuid,
            1,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            1);
}
