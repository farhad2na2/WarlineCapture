using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityGenerationRecordsTests
{
    [Test]
    public void RecordIdentity_CreatesDeterministicBoundedGeneratedStableId()
    {
        var identity = new DenseCityRecordIdentity(
            "dense-city-v1",
            42,
            7,
            "building",
            13,
            "0123456789abcdef0123456789abcdef",
            1234);

        string first = identity.CreateBakedStableId();

        Assert.That(first, Is.EqualTo(identity.CreateBakedStableId()));
        Assert.That(first, Does.StartWith("densecity."));
        Assert.That(first, Has.Length.EqualTo(74));
        Assert.That(Game.Configs.OperationMapIdentityRules.IsValidGeneratedStableId(first), Is.True);
    }

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
                Vector2Int.zero,
                Vector2Int.one,
                Vector2.zero,
                0f,
                new Bounds(Vector3.zero, Vector3.one),
                Vector3.forward,
                0,
                100f,
                OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked,
                CreateIdentity(2, "foundation"),
                CreateIdentity(3, "blocker"),
                CreateIdentity(4, "intact"),
                CreateIdentity(5, "destroyed")),
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

    [Test]
    public void BuildingGroup_AddsAndRemovesAllFiveRecordsAtomically()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 2);
        CreateBuildingGroup(
            out DenseCityBuildingBakeRecord building,
            out DenseCitySurfaceBakeRecord foundation,
            out DenseCitySurfaceBakeRecord blocker,
            out DenseCityPresentationBakeRecord intact,
            out DenseCityPresentationBakeRecord destroyed);

        records.AddBuildingGroup(building, foundation, blocker, intact, destroyed);
        records.RemoveBuildingGroup(building);
        records.Seal();

        Assert.That(records.Buildings, Is.Empty);
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Is.Empty);
    }

    [Test]
    public void BuildingGroup_DuplicatePreflightLeavesSetUnchanged()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 3);
        CreateBuildingGroup(
            out DenseCityBuildingBakeRecord building,
            out DenseCitySurfaceBakeRecord foundation,
            out DenseCitySurfaceBakeRecord blocker,
            out DenseCityPresentationBakeRecord intact,
            out DenseCityPresentationBakeRecord destroyed);
        records.Add(CreatePresentation(building.Identity));

        Assert.That(
            () => records.AddBuildingGroup(building, foundation, blocker, intact, destroyed),
            Throws.InvalidOperationException.With.Message.Contains("Duplicate"));
        records.Seal();
        Assert.That(records.Buildings, Is.Empty);
        Assert.That(records.Surfaces, Is.Empty);
        Assert.That(records.Presentations, Has.Count.EqualTo(1));
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

    private static void CreateBuildingGroup(
        out DenseCityBuildingBakeRecord building,
        out DenseCitySurfaceBakeRecord foundation,
        out DenseCitySurfaceBakeRecord blocker,
        out DenseCityPresentationBakeRecord intact,
        out DenseCityPresentationBakeRecord destroyed)
    {
        DenseCityRecordIdentity buildingIdentity = CreateIdentity(10, "building");
        DenseCityRecordIdentity foundationIdentity = CreateIdentity(11, "foundation");
        DenseCityRecordIdentity blockerIdentity = CreateIdentity(12, "blocker");
        DenseCityRecordIdentity intactIdentity = CreateIdentity(13, "building-intact");
        DenseCityRecordIdentity destroyedIdentity = CreateIdentity(14, "building-destroyed");
        building = new DenseCityBuildingBakeRecord(
            buildingIdentity,
            Matrix4x4.identity,
            new Vector2Int(4, 7),
            new Vector2Int(8, 6),
            new Vector2(8f, 6f),
            0f,
            new Bounds(Vector3.zero, new Vector3(8f, 5f, 6f)),
            Vector3.forward,
            0,
            100f,
            OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked,
            foundationIdentity,
            blockerIdentity,
            intactIdentity,
            destroyedIdentity);
        Vector2[] polygon =
        {
            new(-4f, -3f),
            new(4f, -3f),
            new(4f, 3f),
            new(-4f, 3f)
        };
        foundation = new DenseCitySurfaceBakeRecord(
            foundationIdentity,
            DenseCitySurfaceRecordKind.Terrain,
            polygon,
            0f,
            1,
            0,
            Vector2Int.zero);
        blocker = new DenseCitySurfaceBakeRecord(
            blockerIdentity,
            DenseCitySurfaceRecordKind.Blocker,
            polygon,
            0f,
            0,
            0,
            Vector2Int.zero);
        intact = CreateBuildingPresentation(intactIdentity, DenseCityPresentationCategory.GameplayBuildingIntact);
        destroyed = CreateBuildingPresentation(
            destroyedIdentity,
            DenseCityPresentationCategory.GameplayBuildingDestroyed);
    }

    private static DenseCityPresentationBakeRecord CreateBuildingPresentation(
        DenseCityRecordIdentity identity,
        DenseCityPresentationCategory category) =>
        new(
            identity,
            category,
            SourceGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            3);
}
