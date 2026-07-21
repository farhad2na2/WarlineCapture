using System;
using System.Collections.Generic;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapEntityPresentationMigrationEditorTests
{
    private const string OwnerA =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-902583272-0";
    private const string OwnerB =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1836082762-0";
    private const string OwnerOther =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2444809882377260586-0";

    [Test]
    public void ComputeRecordHash_IsDeterministicAndChangesWithOwnerIdentity()
    {
        AssertValid(CreateValidArgs(OwnerA, "destination-a"), out var first);
        AssertValid(CreateValidArgs(OwnerA, "destination-a"), out var second);

        string firstHash = OperationMapEntityPresentationMigrationEditor.ComputeRecordHash(first);
        string secondHash = OperationMapEntityPresentationMigrationEditor.ComputeRecordHash(second);
        Assert.That(firstHash, Is.EqualTo(secondHash));
        Assert.That(firstHash, Does.Match("^[0-9a-f]{64}$"));

        AssertValid(CreateValidArgs(OwnerB, "destination-b"), out var changed);
        Assert.That(
            OperationMapEntityPresentationMigrationEditor.ComputeRecordHash(changed),
            Is.Not.EqualTo(firstHash));
    }

    [Test]
    public void ComputeOrderedRecordSetHash_IsOrderIndependent()
    {
        AssertValid(CreateValidArgs(OwnerB, "destination-b"), out var recordB);
        AssertValid(CreateValidArgs(OwnerA, "destination-a"), out var recordA);

        var forward = new List<OperationMapEntityPresentationMigrationRecord> { recordA, recordB };
        var reverse = new List<OperationMapEntityPresentationMigrationRecord> { recordB, recordA };

        Assert.That(
            OperationMapEntityPresentationMigrationEditor.ComputeOrderedRecordSetHash(forward),
            Is.EqualTo(OperationMapEntityPresentationMigrationEditor.ComputeOrderedRecordSetHash(reverse)));
    }

    [Test]
    public void TryValidateRecordSet_RejectsDuplicateOwnerAndDestination()
    {
        AssertValid(CreateValidArgs(OwnerA, "destination-a"), out var recordA);
        AssertValid(CreateValidArgs(OwnerA, "destination-other"), out var duplicateOwner);
        var duplicateOwners = new List<OperationMapEntityPresentationMigrationRecord>
        {
            recordA,
            duplicateOwner
        };

        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryValidateRecordSet(
                duplicateOwners,
                out string rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain("duplicate-sourceOwnerGlobalObjectId"));
        Assert.Throws<InvalidOperationException>(() =>
            OperationMapEntityPresentationMigrationEditor.ComputeOrderedRecordSetHash(duplicateOwners));

        AssertValid(CreateValidArgs(OwnerOther, "destination-a"), out var duplicateDestination);
        var duplicateDestinations = new List<OperationMapEntityPresentationMigrationRecord>
        {
            recordA,
            duplicateDestination
        };

        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryValidateRecordSet(
                duplicateDestinations,
                out rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain("duplicate-destinationStableIdentity"));
    }

    [TestCase(
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-222-0|mesh-b\n" +
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-111-0|mesh-a")]
    [TestCase(
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-111-0|mesh-a\n" +
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-111-0|mesh-a")]
    public void TryCreateRecord_RejectsUnsortedOrDuplicateRendererPayload(string value)
    {
        RecordCreateArgs args = CreateValidArgs(OwnerA, "destination-a");
        args.SourceRendererPayloadCanonical = value;

        AssertRejected(args, "sourceRendererPayloadCanonical-not-sorted-unique-canonical");
    }

    [TestCase("BakeEntityTransform|BakeEntitiesGraphics")]
    [TestCase("BakeEntitiesGraphics|BakeEntitiesGraphics")]
    public void TryCreateRecord_RejectsUnsortedOrDuplicateComponentDispositions(string value)
    {
        RecordCreateArgs args = CreateValidArgs(OwnerA, "destination-a");
        args.ComponentDispositionCanonical = value;

        AssertRejected(args, "componentDispositionCanonical-not-sorted-unique-canonical");
    }

    [TestCase("placement.vehicle.002\nplacement.vehicle.001")]
    [TestCase("placement.vehicle.001\nplacement.vehicle.001")]
    public void TryCreateRecord_RejectsUnsortedOrDuplicatePlacementIdentities(string value)
    {
        RecordCreateArgs args = CreateValidArgs(OwnerA, "destination-a");
        args.PlacementConfigIdentitiesCanonical = value;

        AssertRejected(args, "placementConfigIdentitiesCanonical-not-sorted-unique-canonical");
    }

    [TestCase("chunk-002\nchunk-001")]
    [TestCase("chunk-001\nchunk-001")]
    public void TryCreateRecord_RejectsUnsortedOrDuplicateRollbackChunkSets(string value)
    {
        RecordCreateArgs args = CreateValidArgs(OwnerA, "destination-a");
        args.RollbackChunkIdsCanonical = value;

        AssertRejected(args, "rollbackChunkIdsCanonical-not-sorted-unique-canonical");
    }

    [Test]
    public void TryCreateRecord_RejectsInvalidPrefabGuidLocalIdPairing()
    {
        RecordCreateArgs guidWithoutLocalId = CreateValidArgs(OwnerA, "destination-a");
        guidWithoutLocalId.PrefabLocalId = 0L;
        AssertRejected(guidWithoutLocalId, "prefabAssetGuid-localId-pair-invalid");

        RecordCreateArgs localIdWithoutGuid = CreateValidArgs(OwnerA, "destination-a");
        localIdWithoutGuid.PrefabAssetGuid = string.Empty;
        AssertRejected(localIdWithoutGuid, "prefabAssetGuid-localId-pair-invalid");

        RecordCreateArgs malformedGuid = CreateValidArgs(OwnerA, "destination-a");
        malformedGuid.PrefabAssetGuid = "0123456789ABCDEF0123456789ABCDEF";
        AssertRejected(malformedGuid, "prefabAssetGuid-invalid");

        RecordCreateArgs noPrefab = CreateValidArgs(OwnerA, "destination-a");
        noPrefab.PrefabAssetGuid = string.Empty;
        noPrefab.PrefabLocalId = 0L;
        AssertValid(noPrefab, out _);
    }

    [Test]
    public void TryCreateRecord_FailClosedRejectsInvalidOwnerAndRequiredFields()
    {
        RecordCreateArgs invalidOwner = CreateValidArgs("owner-a", "destination-a");
        AssertRejected(invalidOwner, "sourceOwnerGlobalObjectId-invalid");

        RecordCreateArgs unknownRole = CreateValidArgs(OwnerA, "destination-a");
        unknownRole.ApprovedRole = "StaticRenderOnlyCandidate";
        AssertRejected(unknownRole, "approvedRole-unknown");

        RecordCreateArgs nonFinite = CreateValidArgs(OwnerA, "destination-a");
        nonFinite.WorldPosition = new Vector3(1f, float.NaN, 3f);
        AssertRejected(nonFinite, "worldPosition-non-finite");

        RecordCreateArgs missingRenderers = CreateValidArgs(OwnerA, "destination-a");
        missingRenderers.SourceRendererPayloadCanonical = string.Empty;
        AssertRejected(missingRenderers, "sourceRendererPayloadCanonical-empty");

        RecordCreateArgs missingChunks = CreateValidArgs(OwnerA, "destination-a");
        missingChunks.RollbackChunkIdsCanonical = string.Empty;
        AssertRejected(missingChunks, "rollbackChunkIdsCanonical-empty");

        RecordCreateArgs malformedManifestHash = CreateValidArgs(OwnerA, "destination-a");
        malformedManifestHash.RollbackManifestContentHash =
            "6A215389B88CB1C9656D2580942D5F70";
        AssertRejected(malformedManifestHash, "rollbackManifestContentHash-invalid");

        RecordCreateArgs malformedSceneHash = CreateValidArgs(OwnerA, "destination-a");
        malformedSceneHash.RollbackCanonicalSceneDependencyHash = "40a126c4";
        AssertRejected(
            malformedSceneHash,
            "rollbackCanonicalSceneDependencyHash-invalid");
    }

    private static void AssertValid(
        RecordCreateArgs args,
        out OperationMapEntityPresentationMigrationRecord record)
    {
        Assert.That(
            TryCreateRecord(args, out record, out string rejectionReason),
            Is.True,
            rejectionReason);
    }

    private static void AssertRejected(RecordCreateArgs args, string expectedReason)
    {
        Assert.That(TryCreateRecord(args, out _, out string rejectionReason), Is.False);
        Assert.That(rejectionReason, Does.Contain(expectedReason));
    }

    private static bool TryCreateRecord(
        RecordCreateArgs args,
        out OperationMapEntityPresentationMigrationRecord record,
        out string rejectionReason)
    {
        return OperationMapEntityPresentationMigrationEditor.TryCreateRecord(
            args.SourceScenePath,
            args.SourceOwnerGlobalObjectId,
            args.SourceOwnerHierarchyPath,
            args.ApprovedRole,
            args.PrefabAssetGuid,
            args.PrefabLocalId,
            args.SourceRendererPayloadCanonical,
            args.WorldPosition,
            args.WorldRotation,
            args.WorldScale,
            args.ComponentDispositionCanonical,
            args.DestinationSubScenePath,
            args.DestinationStableIdentity,
            args.PlacementConfigIdentitiesCanonical,
            args.RollbackChunkIdsCanonical,
            args.RollbackManifestPath,
            args.RollbackManifestContentHash,
            args.RollbackCanonicalSceneDependencyHash,
            args.DecisionOwner,
            out record,
            out rejectionReason);
    }

    private static RecordCreateArgs CreateValidArgs(string ownerId, string destinationId)
    {
        return new RecordCreateArgs
        {
            SourceScenePath =
                "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity",
            SourceOwnerGlobalObjectId = ownerId,
            SourceOwnerHierarchyPath = "Map[5]/Buildings[18]/Owner[0]",
            ApprovedRole = OperationMapEntityPresentationMigrationEditor.RoleRenderOnly,
            PrefabAssetGuid = "0123456789abcdef0123456789abcdef",
            PrefabLocalId = 2444809882377260586L,
            SourceRendererPayloadCanonical =
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-111-0|mesh-a\n" +
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-222-0|mesh-b",
            WorldPosition = new Vector3(1.25f, 2.5f, -3.75f),
            WorldRotation = Quaternion.identity,
            WorldScale = Vector3.one,
            ComponentDispositionCanonical =
                "BakeEntitiesGraphics|BakeEntityTransform|OmitInertAnimator",
            DestinationSubScenePath =
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
            DestinationStableIdentity = destinationId,
            PlacementConfigIdentitiesCanonical =
                "placement.building.001\nplacement.building.002",
            RollbackChunkIdsCanonical = "chunk-001\nchunk-002",
            RollbackManifestPath =
                "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/" +
                "desert_base_01/StaticMapPresentationManifest.asset",
            RollbackManifestContentHash = "6a215389b88cb1c9656d2580942d5f70",
            RollbackCanonicalSceneDependencyHash = "40a126c478b62305d31b8aa6a8445ba9",
            DecisionOwner = "phase0a-scaffolding-test"
        };
    }

    private sealed class RecordCreateArgs
    {
        public string SourceScenePath;
        public string SourceOwnerGlobalObjectId;
        public string SourceOwnerHierarchyPath;
        public string ApprovedRole;
        public string PrefabAssetGuid;
        public long PrefabLocalId;
        public string SourceRendererPayloadCanonical;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public Vector3 WorldScale;
        public string ComponentDispositionCanonical;
        public string DestinationSubScenePath;
        public string DestinationStableIdentity;
        public string PlacementConfigIdentitiesCanonical;
        public string RollbackChunkIdsCanonical;
        public string RollbackManifestPath;
        public string RollbackManifestContentHash;
        public string RollbackCanonicalSceneDependencyHash;
        public string DecisionOwner;
    }
}
