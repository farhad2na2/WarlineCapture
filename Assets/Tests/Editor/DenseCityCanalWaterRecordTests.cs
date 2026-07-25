using System;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityCanalWaterRecordTests
{
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private const string BedMaterialGuid = "11111111111111111111111111111111";
    private const string WaterMaterialGuid = "22222222222222222222222222222222";

    public static void RunFocusedValidation()
    {
        var suite = new DenseCityCanalWaterRecordTests();
        suite.Factory_ProducesBlockedExclusionAndSeparateBedAndWaterPresentations();
        suite.Factory_PropagatesExplicitProtectedOverlapPermission();
        suite.Transaction_RollsBackAllCanalRecordsAfterRejectedOrExceptionalRealization();
        suite.Context_AllocatesContiguousCanalSequenceBeforeFollowingInfrastructure();
        suite.SurfaceRecord_RequiresNoneOnlyForBlockers();
        Debug.Log("[DenseCityCanalWaterRecordValidation] result=Passed tests=5");
    }

    [Test]
    public void Factory_ProducesBlockedExclusionAndSeparateBedAndWaterPresentations()
    {
        Matrix4x4 bedMatrix = Matrix4x4.TRS(
            new Vector3(20f, 1.9f, 40f),
            Quaternion.identity,
            new Vector3(12f, 1f, 8f));
        Matrix4x4 waterMatrix = Matrix4x4.TRS(
            new Vector3(20f, 2f, 40f),
            Quaternion.identity,
            new Vector3(12f, 1f, 8f));

        DenseCityCanalWaterRecordGroup group = DenseCityCanalWaterRecordFactory.Create(
            CreateInput(30, bedMatrix, waterMatrix));

        Assert.That(group.Exclusion.Kind, Is.EqualTo(DenseCitySurfaceRecordKind.Blocker));
        Assert.That(group.Exclusion.MovementMask, Is.Zero);
        Assert.That(group.Exclusion.Identity.Kind, Is.EqualTo("canal-water-exclusion"));
        Assert.That(group.Exclusion.Identity.DeterministicSequence, Is.EqualTo(30));
        Assert.That(group.BedPresentation.Identity.Kind, Is.EqualTo("canal-bed-visual"));
        Assert.That(group.BedPresentation.Identity.DeterministicSequence, Is.EqualTo(31));
        Assert.That(group.WaterPresentation.Identity.Kind, Is.EqualTo("canal-water-visual"));
        Assert.That(group.WaterPresentation.Identity.DeterministicSequence, Is.EqualTo(32));
        Assert.That(group.BedPresentation.MaterialAssetGuids.Span[0], Is.EqualTo(BedMaterialGuid));
        Assert.That(group.WaterPresentation.MaterialAssetGuids.Span[0], Is.EqualTo(WaterMaterialGuid));
        Assert.That(group.Exclusion.Polygon.Span[0], Is.EqualTo(new Vector2(14f, 36f)));
        Assert.That(group.Exclusion.Polygon.Span[2], Is.EqualTo(new Vector2(26f, 44f)));
        Assert.That(group.BedPresentation.AllowsProtectedOverlap, Is.False);
        Assert.That(group.WaterPresentation.AllowsProtectedOverlap, Is.False);
    }

    [Test]
    public void Factory_PropagatesExplicitProtectedOverlapPermission()
    {
        DenseCityCanalWaterRecordInput input = CreateInput(
            30,
            Matrix4x4.identity,
            Matrix4x4.identity,
            true);

        DenseCityCanalWaterRecordGroup group = DenseCityCanalWaterRecordFactory.Create(input);

        Assert.That(group.BedPresentation.AllowsProtectedOverlap, Is.True);
        Assert.That(group.WaterPresentation.AllowsProtectedOverlap, Is.True);
    }

    [Test]
    public void Transaction_RollsBackAllCanalRecordsAfterRejectedOrExceptionalRealization()
    {
        foreach (bool throwDuringRealization in new[] { false, true })
        {
            using var records = new DenseCityGenerationRecordSet(1, 1, 2);
            DenseCityCanalWaterRecordGroup group = DenseCityCanalWaterRecordFactory.Create(
                CreateInput(0, Matrix4x4.identity, Matrix4x4.identity));

            if (throwDuringRealization)
            {
                Assert.That(
                    () => DenseCityCanalWaterPlacementTransaction.TryCommitAndRealize(
                        records,
                        group,
                        () => throw new InvalidOperationException("canal realization failed")),
                    Throws.InvalidOperationException.With.Message.EqualTo("canal realization failed"));
            }
            else
            {
                Assert.That(
                    DenseCityCanalWaterPlacementTransaction.TryCommitAndRealize(
                        records,
                        group,
                        () => false),
                    Is.False);
            }

            records.Seal();
            Assert.That(records.Surfaces, Is.Empty);
            Assert.That(records.Presentations, Is.Empty);
        }
    }

    [Test]
    public void Context_AllocatesContiguousCanalSequenceBeforeFollowingInfrastructure()
    {
        using var context = new DenseCityGenerationTransactionContext(1, 2, 3);
        DenseCityCanalWaterRecordGroup canal = default;
        Assert.That(
            context.TryPlaceCanalWater(
                4,
                sequence => canal = DenseCityCanalWaterRecordFactory.Create(
                    CreateInput(sequence, Matrix4x4.identity, Matrix4x4.identity)),
                () => true),
            Is.True);

        DenseCitySurfaceBakeRecord ramp = default;
        Assert.That(
            context.TryPlaceSurface(
                4,
                sequence => ramp = new DenseCitySurfaceBakeRecord(
                    new DenseCityRecordIdentity("dense-city-v1", 7, 4, "ramp", sequence, SourceGuid, 1),
                    DenseCitySurfaceRecordKind.Ramp,
                    new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                    0f,
                    1,
                    0,
                    Vector2Int.zero),
                () => true),
            Is.True);

        Assert.That(canal.Exclusion.Identity.DeterministicSequence, Is.EqualTo(0));
        Assert.That(canal.WaterPresentation.Identity.DeterministicSequence, Is.EqualTo(2));
        Assert.That(ramp.Identity.DeterministicSequence, Is.EqualTo(3));
        context.Seal();
        Assert.That(context.Records.Surfaces.Select(record => record.Identity.DeterministicSequence),
            Is.EquivalentTo(new[] { 0, 3 }));
    }

    [Test]
    public void SurfaceRecord_RequiresNoneOnlyForBlockers()
    {
        DenseCityRecordIdentity identity = new(
            "dense-city-v1",
            7,
            0,
            "surface",
            0,
            SourceGuid,
            1);
        Vector2[] polygon = { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };

        Assert.That(
            () => new DenseCitySurfaceBakeRecord(
                identity,
                DenseCitySurfaceRecordKind.Blocker,
                polygon,
                0f,
                1,
                0,
                Vector2Int.zero),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => new DenseCitySurfaceBakeRecord(
                identity,
                DenseCitySurfaceRecordKind.Terrain,
                polygon,
                0f,
                0,
                0,
                Vector2Int.zero),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static DenseCityCanalWaterRecordInput CreateInput(
        int sequence,
        Matrix4x4 bedMatrix,
        Matrix4x4 waterMatrix,
        bool allowsProtectedOverlap = false) =>
        new(
            "dense-city-v1",
            7,
            4,
            sequence,
            SourceGuid,
            1,
            new[] { BedMaterialGuid },
            new[] { WaterMaterialGuid },
            bedMatrix,
            waterMatrix,
            new Vector2(12f, 8f),
            2f,
            0,
            new Vector2Int(2, 4),
            allowsProtectedOverlap: allowsProtectedOverlap);
}
