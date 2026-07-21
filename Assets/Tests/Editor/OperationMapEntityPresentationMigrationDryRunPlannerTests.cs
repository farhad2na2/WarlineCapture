using System.Collections.Generic;
using System.Linq;
using Game.Editor;
using NUnit.Framework;
using UnityEngine;

public sealed class OperationMapEntityPresentationMigrationDryRunPlannerTests
{
    private const string OwnerA =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1000-0";
    private const string OwnerB =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2000-0";
    private const string SourceA1 =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1100-0";
    private const string SourceA2 =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1200-0";
    private const string SourceB1 =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2100-0";
    private const string BuildingPlacementObject =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-3100-0";
    private const string VehiclePlacementObject =
        "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-3200-0";
    private const string DestinationPath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_Map.unity";

    [Test]
    public void TryCreateDryRunCandidatePlan_IsDeterministicUnderInputReordering()
    {
        var forward = CreateValidReport();
        var reordered = CreateValidReport();
        reordered.sources.Reverse();
        reordered.owners.Reverse();
        reordered.placementJoins.Reverse();
        reordered.classificationCounts.Reverse();
        foreach (var owner in reordered.owners)
            owner.dispositionCounts.Reverse();

        AssertPlan(forward, out var first);
        AssertPlan(reordered, out var second);

        Assert.That(
            first.Status,
            Is.EqualTo(
                OperationMapEntityPresentationMigrationPlanStatus
                    .StaticOwnersReadyGameplayOwnersPending));
        Assert.That(first.RecordSetHash, Is.EqualTo(second.RecordSetHash));
        Assert.That(first.PlacementJoinSetHash, Is.EqualTo(second.PlacementJoinSetHash));
        Assert.That(first.RecordSetHash, Does.Match("^[0-9a-f]{64}$"));
        Assert.That(first.PlacementJoinSetHash, Does.Match("^[0-9a-f]{64}$"));
        Assert.That(
            forward.sources.All(source =>
                source.buildingJoinCount == 0 && source.vehicleJoinCount == 0),
            Is.True);
        Assert.That(
            forward.placementJoins.Any(join =>
                forward.sources.Any(source =>
                    source.sourceGlobalObjectId == join.resolvedSourceGlobalObjectId)),
            Is.False);
        Assert.That(
            first.Records.Select(record => record.SourceOwnerGlobalObjectId),
            Is.EqualTo(first.Records.Select(record => record.SourceOwnerGlobalObjectId).OrderBy(id => id)));
        Assert.That(
            first.Records.All(record => record.PlacementConfigIdentitiesCanonical.Length == 0),
            Is.True);
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_AggregatesMultipleRenderersLosslessly()
    {
        AssertPlan(CreateValidReport(), out var plan);

        OperationMapEntityPresentationMigrationRecord ownerRecord =
            plan.Records.Single(record => record.SourceOwnerGlobalObjectId == OwnerA);
        Assert.That(ownerRecord.ApprovedRole, Is.EqualTo("RenderOnly"));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical.Split('\n'), Has.Length.EqualTo(2));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical, Does.Contain(SourceA1));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical, Does.Contain(SourceA2));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical, Does.Contain("chunk-001"));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical, Does.Contain("chunk-002"));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical, Does.Contain(new string('d', 32)));
        Assert.That(ownerRecord.SourceRendererPayloadCanonical, Does.Contain("1"));
        Assert.That(ownerRecord.RollbackChunkIdsCanonical, Is.EqualTo("chunk-001\nchunk-002"));
        Assert.That(
            ownerRecord.ComponentDispositionCanonical,
            Is.EqualTo("BakeEntitiesGraphics=4|BakeEntityTransform=2"));
        Assert.That(ownerRecord.PlacementConfigIdentitiesCanonical, Is.Empty);
        Assert.That(ownerRecord.RollbackManifestContentHash, Is.EqualTo(new string('a', 32)));
        Assert.That(
            ownerRecord.RollbackCanonicalSceneDependencyHash,
            Is.EqualTo(new string('b', 32)));
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsMissingOwner()
    {
        var report = CreateValidReport();
        report.sources[0].migrationOwnerGlobalObjectId =
            "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-9999-0";

        AssertRejected(report, "source-owner-missing");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsDuplicateOrNestedOwners()
    {
        var duplicate = CreateValidReport();
        duplicate.owners[1].globalObjectId = OwnerA;
        AssertRejected(duplicate, "duplicate-owner-globalObjectId");

        var nested = CreateValidReport();
        nested.owners[1].hierarchyPath = "Map[0]/OwnerA[0]/Nested[0]";
        AssertRejected(nested, "duplicate-or-nested-owner-hierarchy");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsMismatchedCounts()
    {
        var ownerMismatch = CreateValidReport();
        ownerMismatch.owners[0].sourceRendererCount = 1;
        AssertRejected(ownerMismatch, "owner-source-count-mismatch");

        var listMismatch = CreateValidReport();
        listMismatch.counts.sourceCount = 4;
        listMismatch.counts.staticRenderOnlyCandidateCount = 3;
        AssertRejected(listMismatch, "source-or-chunk-count-mismatch");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsUnresolvedOrReusedPlacement()
    {
        var unresolved = CreateValidReport();
        unresolved.placementJoins[0].resolveState = "Unresolved";
        AssertRejected(unresolved, "not-exact");

        var reused = CreateValidReport();
        reused.placementJoins[1].resolvedSourceGlobalObjectId =
            reused.placementJoins[0].resolvedSourceGlobalObjectId;
        AssertRejected(reused, "duplicate-or-reused");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsBlockerOrExternalReference()
    {
        var blocker = CreateValidReport();
        blocker.owners[0].blockingDependencyCount = 1;
        AssertRejected(blocker, "invalid-or-blocked");

        var reference = CreateValidReport();
        reference.owners[0].externalSceneReferenceCount = 1;
        reference.owners[0].externalSceneReferences.Add(
            new OperationMapEntityPresentationMigrationInventoryProbe.CrossObjectReferenceReport
            {
                componentGlobalObjectId = OwnerA,
                componentType = "Example",
                propertyPath = "target",
                targetGlobalObjectId = OwnerB,
                targetHierarchyPath = "Map[0]/OwnerB[1]"
            });
        AssertRejected(reference, "invalid-or-blocked");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsMalformedSourceAndRollbackHash()
    {
        var malformedSource = CreateValidReport();
        malformedSource.sources[0].sourceGlobalObjectId = "source-a";
        AssertRejected(malformedSource, "sourceGlobalObjectId-invalid");

        var malformedRollback = CreateValidReport();
        malformedRollback.manifest.contentHash = "ABCDEF";
        AssertRejected(malformedRollback, "manifest.contentHash-invalid");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsUnsortedOrDuplicateMaterials()
    {
        var unsorted = CreateValidReport();
        unsorted.sources[0].materialGuids =
            new List<string> { new string('d', 32), new string('c', 32) };
        AssertRejected(unsorted, "materialGuids-not-sorted-unique");

        var duplicate = CreateValidReport();
        duplicate.sources[0].materialGuids =
            new List<string> { new string('c', 32), new string('c', 32) };
        AssertRejected(duplicate, "materialGuids-not-sorted-unique");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsComponentDispositionMismatch()
    {
        var report = CreateValidReport();
        report.owners[0].componentTypes[0].count++;

        AssertRejected(report, "component-disposition-count-mismatch");
    }

    [Test]
    public void TryCreateDryRunCandidatePlan_RejectsUnknownDisposition()
    {
        var report = CreateValidReport();
        report.owners[0].dispositionCounts[0].type = "SilentlyDropUnknownComponent";

        AssertRejected(report, "disposition");
    }

    private static void AssertPlan(
        OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report,
        out OperationMapEntityPresentationMigrationPlan plan)
    {
        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryCreateDryRunCandidatePlan(
                report,
                DestinationPath,
                "phase0a-dry-run-test",
                out plan,
                out string rejectionReason),
            Is.True,
            rejectionReason);
    }

    private static void AssertRejected(
        OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport report,
        string expectedReason)
    {
        Assert.That(
            OperationMapEntityPresentationMigrationEditor.TryCreateDryRunCandidatePlan(
                report,
                DestinationPath,
                "phase0a-dry-run-test",
                out _,
                out string rejectionReason),
            Is.False);
        Assert.That(rejectionReason, Does.Contain(expectedReason));
    }

    private static OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport
        CreateValidReport()
    {
        var ownerA = CreateOwner(OwnerA, "Map[0]/OwnerA[0]", rendererCount: 2);
        var ownerB = CreateOwner(OwnerB, "Map[0]/OwnerB[1]", rendererCount: 1);
        ownerB.prefabAssetGuid = string.Empty;
        ownerB.prefabLocalId = 0;

        return new OperationMapEntityPresentationMigrationInventoryProbe.InventoryReport
        {
            reportSchema =
                OperationMapEntityPresentationMigrationInventoryProbe.ReportSchema,
            reportSchemaVersion =
                OperationMapEntityPresentationMigrationInventoryProbe.ReportSchemaVersion,
            result = "InventoryCompletePendingReview",
            counts =
                new OperationMapEntityPresentationMigrationInventoryProbe.InventoryCountsReport
                {
                    sourceCount = 3,
                    chunkCount = 2,
                    migrationOwnerCount = 2,
                    buildingPlacementCount = 1,
                    vehiclePlacementCount = 1,
                    protectedAuthoredCandidateCount = 1,
                    staticRenderOnlyCandidateCount = 2
                },
            manifest =
                new OperationMapEntityPresentationMigrationInventoryProbe.ManifestIdentityReport
                {
                    path =
                        "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/" +
                        "desert_base_01/StaticMapPresentationManifest.asset",
                    operationMapId = "opmap.skirmish.desert_base_01",
                    contentHash = new string('a', 32),
                    canonicalScenePath =
                        "Assets/Game/Scenes/OperationMaps/Skirmish/" +
                        "opmap_skirmish_desert_base_01.unity",
                    canonicalSceneGuid = "ca1f2d7f265d8495f8c815441d68fda0",
                    canonicalSceneDependencyHash = new string('b', 32)
                },
            owners =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport>
                {
                    ownerA,
                    ownerB
                },
            sources =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport>
                {
                    CreateSource(
                        0,
                        SourceA1,
                        OwnerA,
                        "Map[0]/OwnerA[0]/RendererA[0]",
                        "chunk-001",
                        "StaticRenderOnlyCandidate",
                        overlay: false,
                        material: new string('c', 32)),
                    CreateSource(
                        1,
                        SourceA2,
                        OwnerA,
                        "Map[0]/OwnerA[0]/RendererB[1]",
                        "chunk-002",
                        "ProtectedAuthoredCandidate",
                        overlay: true,
                        material: new string('d', 32)),
                    CreateSource(
                        2,
                        SourceB1,
                        OwnerB,
                        "Map[0]/OwnerB[1]/Renderer[0]",
                        "chunk-001",
                        "StaticRenderOnlyCandidate",
                        overlay: false,
                        material: new string('e', 32))
                },
            placementJoins =
                new List<OperationMapEntityPresentationMigrationInventoryProbe.PlacementJoinReport>
                {
                    CreateJoin(
                        "Building",
                        0,
                        "Map/Buildings/AuthoredBuilding",
                        BuildingPlacementObject),
                    CreateJoin(
                        "Vehicle",
                        0,
                        "Map/Vehicles/AuthoredVehicle",
                        VehiclePlacementObject)
                },
            classificationCounts =
                new List<
                    OperationMapEntityPresentationMigrationInventoryProbe.ClassificationCountReport>
                {
                    new()
                    {
                        classification = "ProtectedAuthoredCandidate",
                        count = 1
                    },
                    new()
                    {
                        classification = "StaticRenderOnlyCandidate",
                        count = 2
                    }
                },
            protectedRoots =
                new List<
                    OperationMapEntityPresentationMigrationInventoryProbe.ProtectedRootReport>()
        };
    }

    private static OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport
        CreateOwner(string id, string hierarchyPath, int rendererCount)
    {
        return new OperationMapEntityPresentationMigrationInventoryProbe.OwnerInventoryReport
        {
            globalObjectId = id,
            hierarchyPath = hierarchyPath,
            prefabAssetGuid = "0123456789abcdef0123456789abcdef",
            prefabLocalId = 100,
            worldPosition = new Vector3(1f, 2f, 3f),
            worldRotation = Quaternion.identity,
            worldScale = Vector3.one,
            sourceRendererCount = rendererCount,
            hierarchyObjectCount = rendererCount + 1,
            candidateDisposition = "RenderOnlyEntityCandidate",
            componentTypes =
                new List<
                    OperationMapEntityPresentationMigrationInventoryProbe.DependencyTypeCountReport>
                {
                    new() { type = "UnityEngine.MeshFilter", count = rendererCount },
                    new() { type = "UnityEngine.MeshRenderer", count = rendererCount },
                    new() { type = "UnityEngine.Transform", count = rendererCount }
                },
            dispositionCounts =
                new List<
                    OperationMapEntityPresentationMigrationInventoryProbe.DependencyTypeCountReport>
                {
                    new() { type = "BakeEntityTransform", count = rendererCount },
                    new() { type = "BakeEntitiesGraphics", count = rendererCount * 2 }
                },
            externalSceneReferences =
                new List<
                    OperationMapEntityPresentationMigrationInventoryProbe.CrossObjectReferenceReport>()
        };
    }

    private static OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport
        CreateSource(
            int index,
            string id,
            string ownerId,
            string hierarchyPath,
            string chunkId,
            string classification,
            bool overlay,
            string material)
    {
        return new OperationMapEntityPresentationMigrationInventoryProbe.SourceInventoryReport
        {
            sourceIndex = index,
            sourceGlobalObjectId = id,
            sourceHierarchyPath = hierarchyPath,
            sourceDependencyHash = new string((char)('1' + index), 32),
            chunkId = chunkId,
            meshAssetGuid = new string((char)('6' + index), 32),
            meshLocalId = 200 + index,
            overlaySource = overlay,
            sourceObjectResolved = true,
            migrationOwnerGlobalObjectId = ownerId,
            classification = classification,
            materialGuids = new List<string> { material },
            componentTypes = new List<string>()
        };
    }

    private static OperationMapEntityPresentationMigrationInventoryProbe.PlacementJoinReport
        CreateJoin(string kind, int index, string sourcePath, string sourceId)
    {
        return new OperationMapEntityPresentationMigrationInventoryProbe.PlacementJoinReport
        {
            kind = kind,
            placementIndex = index,
            sourcePath = sourcePath,
            resolveState = "Exact",
            resolutionMethod = "UniqueHierarchyPath",
            scenePathMatchCount = 1,
            transformTupleMatchCount = 1,
            resolvedSourceGlobalObjectId = sourceId
        };
    }
}
