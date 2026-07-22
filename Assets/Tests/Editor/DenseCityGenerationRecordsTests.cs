using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityGenerationRecordsTests
{
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [Test]
    public void Identity_ProducesStableOrdinalKey()
    {
        var identity = CreateIdentity(7, "building");

        Assert.That(
            identity.StableKey,
            Is.EqualTo(
                "dense-city-v1:0000000042:000003:building:0000000007:" +
                "0123456789abcdef0123456789abcdef:00000000000000000123"));
    }

    [Test]
    public void RecordSet_SealsInStableIdentityOrder()
    {
        using var records = new DenseCityGenerationRecordSet(2, 2, 3);
        records.Add(CreatePresentation(9));
        records.Add(CreatePresentation(2));
        records.Add(CreatePresentation(5));

        records.Seal();

        Assert.That(records.Presentations[0].Identity.DeterministicSequence, Is.EqualTo(2));
        Assert.That(records.Presentations[1].Identity.DeterministicSequence, Is.EqualTo(5));
        Assert.That(records.Presentations[2].Identity.DeterministicSequence, Is.EqualTo(9));
        Assert.That(() => records.Add(CreatePresentation(10)), Throws.InvalidOperationException);
    }

    [Test]
    public void RecordSet_RejectsDuplicateIdentityAcrossRecordKinds()
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        DenseCityRecordIdentity identity = CreateIdentity(1, "shared");
        records.Add(new DenseCitySurfaceBakeRecord(
            identity,
            DenseCitySurfaceRecordKind.Terrain,
            new[] { Vector2.zero, Vector2.right, Vector2.up },
            0f,
            1,
            0,
            Vector2Int.zero));

        Assert.That(
            () => records.Add(CreatePresentation(identity)),
            Throws.InvalidOperationException.With.Message.Contains("Duplicate"));
    }

    [Test]
    public void RecordSet_RejectsCapacityOverflow()
    {
        using var records = new DenseCityGenerationRecordSet(1, 1, 1);
        records.Add(CreatePresentation(1));

        Assert.That(
            () => records.Add(CreatePresentation(2)),
            Throws.InvalidOperationException.With.Message.Contains("capacity"));
    }

    [Test]
    public void BuildingAndSurfaceRecords_RejectInvalidGeometry()
    {
        DenseCityRecordIdentity identity = CreateIdentity(1, "building");
        Assert.That(
            () => new DenseCityBuildingBakeRecord(
                identity,
                Matrix4x4.identity,
                Vector2.zero,
                0f,
                new Bounds(Vector3.zero, Vector3.one),
                Vector3.forward,
                0,
                100f,
                OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked,
                CreateIdentity(2, "intact"),
                CreateIdentity(3, "destroyed")),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => new DenseCitySurfaceBakeRecord(
                CreateIdentity(4, "road"),
                DenseCitySurfaceRecordKind.Road,
                new[] { Vector2.zero, Vector2.right },
                0f,
                1,
                0,
                Vector2Int.zero),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static DenseCityRecordIdentity CreateIdentity(int sequence, string kind) =>
        new("dense-city-v1", 42, 3, kind, sequence, SourceGuid, 123);

    private static DenseCityPresentationBakeRecord CreatePresentation(int sequence) =>
        CreatePresentation(CreateIdentity(sequence, "prop"));

    private static DenseCityPresentationBakeRecord CreatePresentation(DenseCityRecordIdentity identity) =>
        new(
            identity,
            DenseCityPresentationCategory.Prop,
            SourceGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            1);
}
