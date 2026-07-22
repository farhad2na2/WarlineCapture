using System.Collections.Generic;
using Game.Configs;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityGenerationTransactionContextTests
{
    private const string IntactGuid = "0123456789abcdef0123456789abcdef";
    private const string DestroyedGuid = "fedcba9876543210fedcba9876543210";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [Test]
    public void TryPlaceBuilding_AllocatesStableFiveRecordSequencesPerDistrict()
    {
        using var context = new DenseCityGenerationTransactionContext(3, 6, 6);
        int firstSequence = -1;
        int secondSequence = -1;
        int otherDistrictSequence = -1;

        Assert.That(context.TryPlaceBuilding(2, sequence =>
        {
            firstSequence = sequence;
            return CreateGroup(2, sequence);
        }, () => true), Is.True);
        Assert.That(context.TryPlaceBuilding(2, sequence =>
        {
            secondSequence = sequence;
            return CreateGroup(2, sequence);
        }, () => true), Is.True);
        Assert.That(context.TryPlaceBuilding(7, sequence =>
        {
            otherDistrictSequence = sequence;
            return CreateGroup(7, sequence);
        }, () => true), Is.True);
        context.Seal();

        Assert.That(firstSequence, Is.Zero);
        Assert.That(secondSequence, Is.EqualTo(5));
        Assert.That(otherDistrictSequence, Is.Zero);
        Assert.That(context.Records.Buildings, Has.Count.EqualTo(3));
    }

    [Test]
    public void TryPlaceBuilding_RejectionConsumesAttemptSequenceButLeavesNoRecords()
    {
        using var context = new DenseCityGenerationTransactionContext(1, 2, 2);
        int rejectedSequence = -1;
        int acceptedSequence = -1;

        Assert.That(context.TryPlaceBuilding(4, sequence =>
        {
            rejectedSequence = sequence;
            return CreateGroup(4, sequence);
        }, () => false), Is.False);
        Assert.That(context.TryPlaceBuilding(4, sequence =>
        {
            acceptedSequence = sequence;
            return CreateGroup(4, sequence);
        }, () => true), Is.True);
        context.Seal();

        Assert.That(rejectedSequence, Is.Zero);
        Assert.That(acceptedSequence, Is.EqualTo(5));
        Assert.That(context.Records.Buildings, Has.Count.EqualTo(1));
        Assert.That(context.Records.Buildings[0].Identity.DeterministicSequence, Is.EqualTo(5));
    }

    [Test]
    public void InfrastructureAndSurfacePlacements_ShareStableSequenceAcrossRejectedAttempts()
    {
        using var context = new DenseCityGenerationTransactionContext(1, 3, 1);
        var observedSequences = new List<int>();

        Assert.That(
            context.TryPlaceInfrastructure(
                4,
                sequence =>
                {
                    observedSequences.Add(sequence);
                    return CreateInfrastructureGroup(sequence);
                },
                () => false),
            Is.False);
        Assert.That(
            context.TryPlaceSurface(
                4,
                sequence =>
                {
                    observedSequences.Add(sequence);
                    return CreateRamp(sequence);
                },
                () => true),
            Is.True);
        context.Seal();

        Assert.That(observedSequences, Is.EqualTo(new[] { 0, 2 }));
        Assert.That(context.Records.Surfaces, Has.Count.EqualTo(1));
        Assert.That(context.Records.Surfaces[0].Identity.DeterministicSequence, Is.EqualTo(2));
        Assert.That(context.Records.Presentations, Is.Empty);
    }

    [Test]
    public void RegisterRealizedBuildingOwner_UsesCommittedIdentityAndRejectsDuplicates()
    {
        using var context = new DenseCityGenerationTransactionContext(1, 2, 2);
        Assert.That(context.TryPlaceBuilding(
            3,
            sequence => CreateGroup(3, sequence),
            () => true,
            out DenseCityBuildingBakeRecord building), Is.True);
        var rootObject = new GameObject("IntactPresentationRoot");
        var sourcePrefab = new GameObject("SourcePrefab");
        try
        {
            context.RegisterRealizedBuildingOwner(
                building,
                rootObject.transform,
                sourcePrefab,
                GeneratedCityBuildingRole.Shop);

            Assert.That(context.RealizedBuildingOwners, Has.Count.EqualTo(1));
            Assert.That(
                context.RealizedBuildingOwners[0].Building.Identity.StableKey,
                Is.EqualTo(building.Identity.StableKey));
            Assert.That(context.RealizedBuildingOwners[0].IntactPresentationRoot, Is.SameAs(rootObject.transform));
            Assert.That(context.RealizedBuildingOwners[0].SourcePrefab, Is.SameAs(sourcePrefab));
            Assert.That(context.RealizedBuildingOwners[0].Role, Is.EqualTo(GeneratedCityBuildingRole.Shop));
            Assert.That(
                () => context.RegisterRealizedBuildingOwner(
                    building,
                    rootObject.transform,
                    sourcePrefab,
                    GeneratedCityBuildingRole.Shop),
                Throws.InvalidOperationException.With.Message.Contains("duplicated"));
        }
        finally
        {
            Object.DestroyImmediate(rootObject);
            Object.DestroyImmediate(sourcePrefab);
        }
    }

    private static DenseCityBuildingRecordGroup CreateGroup(int districtId, int sequence) =>
        DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                districtId,
                sequence,
                IntactGuid,
                123,
                DestroyedGuid,
                456,
                new[] { MaterialGuid },
                new[] { MaterialGuid },
                Matrix4x4.TRS(new Vector3(sequence, 2f, districtId), Quaternion.identity, Vector3.one),
                new Vector2(8f, 6f),
                2f,
                new Bounds(new Vector3(sequence, 4f, districtId), new Vector3(8f, 4f, 6f)),
                Vector3.forward,
                0,
                500f,
                1,
                0,
                new Vector2Int(districtId, 0)));

    private static DenseCityInfrastructureRecordGroup CreateInfrastructureGroup(int sequence)
    {
        DenseCitySurfaceBakeRecord surface = CreateSurface(
            sequence,
            DenseCitySurfaceRecordKind.Road,
            "road");
        var presentation = new DenseCityPresentationBakeRecord(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                4,
                "road-visual",
                sequence + 1,
                IntactGuid,
                123),
            DenseCityPresentationCategory.Infrastructure,
            IntactGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            2);
        return new DenseCityInfrastructureRecordGroup(surface, presentation);
    }

    private static DenseCitySurfaceBakeRecord CreateRamp(int sequence) =>
        CreateSurface(sequence, DenseCitySurfaceRecordKind.Ramp, "ramp");

    private static DenseCitySurfaceBakeRecord CreateSurface(
        int sequence,
        DenseCitySurfaceRecordKind kind,
        string recordKind) =>
        new(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                4,
                recordKind,
                sequence,
                IntactGuid,
                123),
            kind,
            new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
            0f,
            1,
            0,
            Vector2Int.zero);
}
