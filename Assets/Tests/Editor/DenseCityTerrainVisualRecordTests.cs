using System;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityTerrainVisualRecordTests
{
    private const string FirstSourceGuid = "0123456789abcdef0123456789abcdef";
    private const string SecondSourceGuid = "abcdef0123456789abcdef0123456789";
    private const string MaterialGuid = "11111111111111111111111111111111";

    [Test]
    public void Factory_ProducesOneTerrainAndOrderedPersistentPresentations()
    {
        DenseCityTerrainVisualRecordGroup group = DenseCityTerrainVisualRecordFactory.Create(
            CreateInput(20, 3));

        Assert.That(group.Terrain.Kind, Is.EqualTo(DenseCitySurfaceRecordKind.Terrain));
        Assert.That(group.Terrain.Identity.Kind, Is.EqualTo("canal-bank-terrain"));
        Assert.That(group.Terrain.Identity.DeterministicSequence, Is.EqualTo(20));
        Assert.That(group.Presentations, Has.Length.EqualTo(3));
        Assert.That(group.Presentations.Select(record => record.Identity.DeterministicSequence),
            Is.EqualTo(new[] { 21, 22, 23 }));
        Assert.That(group.Presentations.All(
            record => record.Category == DenseCityPresentationCategory.Infrastructure), Is.True);
        Assert.That(group.Presentations[1].Identity.SourceAssetGuid, Is.EqualTo(SecondSourceGuid));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Transaction_RollsBackTerrainAndEveryPresentation(bool throwDuringRealization)
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 3);
        DenseCityTerrainVisualRecordGroup group = DenseCityTerrainVisualRecordFactory.Create(
            CreateInput(0, 3));

        if (throwDuringRealization)
        {
            Assert.That(
                () => DenseCityTerrainVisualPlacementTransaction.TryCommitAndRealize(
                    records,
                    group,
                    () => throw new InvalidOperationException("terrain realization failed")),
                Throws.InvalidOperationException.With.Message.EqualTo("terrain realization failed"));
        }
        else
        {
            Assert.That(
                DenseCityTerrainVisualPlacementTransaction.TryCommitAndRealize(records, group, () => false),
                Is.False);
        }

        records.Seal();
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void Context_RejectsDeclaredCountMismatchBeforeAdvancingSequence()
    {
        using var context = new DenseCityGenerationTransactionContext(1, 2, 4);
        Assert.That(
            () => context.TryPlaceTerrainVisuals(
                2,
                2,
                sequence => DenseCityTerrainVisualRecordFactory.Create(CreateInput(sequence, 3)),
                () => true),
            Throws.InvalidOperationException);

        DenseCitySurfaceBakeRecord ramp = default;
        Assert.That(
            context.TryPlaceSurface(
                2,
                sequence => ramp = CreateRamp(sequence),
                () => true),
            Is.True);
        Assert.That(ramp.Identity.DeterministicSequence, Is.Zero);
    }

    private static DenseCityTerrainVisualRecordInput CreateInput(int sequence, int presentationCount)
    {
        var presentations = new DenseCityTerrainVisualPresentationInput[presentationCount];
        for (int index = 0; index < presentations.Length; index++)
        {
            presentations[index] = new DenseCityTerrainVisualPresentationInput(
                "canal-bank-visual",
                index == 1 ? SecondSourceGuid : FirstSourceGuid,
                index + 1,
                new[] { MaterialGuid },
                Matrix4x4.TRS(new Vector3(index * 2f, 1f, 4f), Quaternion.identity, Vector3.one),
                false,
                true,
                1);
        }
        return new DenseCityTerrainVisualRecordInput(
            "dense-city-v1",
            7,
            2,
            sequence,
            "canal-bank-terrain",
            Matrix4x4.TRS(new Vector3(4f, 1f, 8f), Quaternion.identity, Vector3.one),
            new Vector2(12f, 6f),
            1f,
            31,
            0,
            Vector2Int.zero,
            presentations);
    }

    private static DenseCitySurfaceBakeRecord CreateRamp(int sequence) =>
        new(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                7,
                2,
                "ramp",
                sequence,
                FirstSourceGuid,
                1),
            DenseCitySurfaceRecordKind.Ramp,
            new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
            0f,
            1,
            0,
            Vector2Int.zero);
}
