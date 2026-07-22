using System;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class DenseCityBuildingAttachmentTransactionTests
{
    private const string SourceGuid = "0123456789abcdef0123456789abcdef";
    private const string DestroyedGuid = "fedcba9876543210fedcba9876543210";
    private const string MaterialGuid = "abcdef0123456789abcdef0123456789";

    [TestCase(true, 3)]
    [TestCase(false, 2)]
    public void TryCommitAndRealize_RetainsOnlyAcceptedAttachment(bool accepted, int expectedPresentationCount)
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 3);
        DenseCityBuildingBakeRecord building = AddBuilding(records);
        DenseCityPresentationBakeRecord attachment = CreateAttachment(building.Identity.StableKey);

        Assert.That(
            DenseCityBuildingAttachmentTransaction.TryCommitAndRealize(
                records,
                attachment,
                () => accepted),
            Is.EqualTo(accepted));
        records.Seal();

        Assert.That(records.Presentations, Has.Count.EqualTo(expectedPresentationCount));
    }

    [Test]
    public void TryCommitAndRealize_ExceptionRemovesAttachmentBeforeRethrow()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 3);
        DenseCityBuildingBakeRecord building = AddBuilding(records);
        DenseCityPresentationBakeRecord attachment = CreateAttachment(building.Identity.StableKey);

        Assert.That(
            () => DenseCityBuildingAttachmentTransaction.TryCommitAndRealize(
                records,
                attachment,
                () => throw new InvalidOperationException("realization failed")),
            Throws.InvalidOperationException.With.Message.EqualTo("realization failed"));
        records.Seal();

        Assert.That(records.Presentations, Has.Count.EqualTo(2));
    }

    [Test]
    public void TryCommitAndRealize_RejectsUnknownBuildingOwnerBeforeRealization()
    {
        using var records = new DenseCityGenerationRecordSet(1, 2, 3);
        DenseCityPresentationBakeRecord attachment = CreateAttachment("missing-owner");
        bool realized = false;

        Assert.That(
            () => DenseCityBuildingAttachmentTransaction.TryCommitAndRealize(
                records,
                attachment,
                () => realized = true),
            Throws.InvalidOperationException.With.Message.Contains("owner is not committed"));
        Assert.That(realized, Is.False);
    }

    private static DenseCityBuildingBakeRecord AddBuilding(DenseCityGenerationRecordSet records)
    {
        DenseCityBuildingRecordGroup group = DenseCityBuildingRecordFactory.Create(
            new DenseCityBuildingRecordInput(
                "dense-city-v1",
                42,
                3,
                0,
                SourceGuid,
                123,
                DestroyedGuid,
                456,
                new[] { MaterialGuid },
                new[] { MaterialGuid },
                Matrix4x4.identity,
                new Vector2(8f, 6f),
                0f,
                new Bounds(Vector3.up * 2f, new Vector3(8f, 4f, 6f)),
                Vector3.forward,
                0,
                500f,
                1,
                0,
                Vector2Int.zero));
        DenseCityBuildingRecordFactory.Add(records, group);
        return group.Building;
    }

    private static DenseCityPresentationBakeRecord CreateAttachment(string ownerStableKey) =>
        new(
            new DenseCityRecordIdentity(
                "dense-city-v1",
                42,
                3,
                "building-attachment-intact",
                0,
                SourceGuid,
                789),
            DenseCityPresentationCategory.BuildingAttachmentIntact,
            SourceGuid,
            null,
            new[] { MaterialGuid },
            Matrix4x4.identity,
            true,
            true,
            2,
            ownerStableKey);
}
