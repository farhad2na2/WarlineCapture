using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Authoring;
using Game.Configs;
using Game.Components;
using Game.Editor;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

public sealed class OperationMapRenderVirtualizationValidation
{
    public static void RunFocusedValidation()
    {
        try
        {
            OperationMapRenderVirtualizationValidation tests = new();
            tests.RenderResidencyMode_IsClosedAndStable();
            tests.OperationMapDefinition_DefaultsToResidentEntities();
            tests.StaticSceneChunks_RejectsVirtualizedProxyPool();
            tests.EntityScene_ResidentEntitiesRetainsCurrentBehavior();
            tests.EntityScene_VirtualizedProxyPoolFailsClosedUntilDatabaseContractExists();
            tests.UnknownRenderResidencyMode_IsRejected();
            tests.RenderVirtualizationSchema_IsUnmanaged();
            tests.RuntimeContracts_UseExpectedEcsKinds();
            tests.RenderPolicyAndStateEnums_AreClosed();
            tests.DatabaseBlob_RetainsEveryRequiredLogicalField();
            tests.RuntimeComponents_RetainRequiredState();
            tests.IdentityProjection_IsExactAndRepeatable();
            tests.IdentityProjection_RejectsEmptySources();
            tests.IdentityCollisionDetector_RejectsDifferentSourcesForOneIdentity();
            tests.IdentityComparer_SortsByLowThenHighDeterministically();
            tests.PrototypeFingerprint_IsDeterministic();
            tests.PrototypeFingerprint_ChangesForEveryContractField();
            tests.PrototypeFingerprint_RejectsInvalidInputs();
            tests.CellAssignment_UsesHalfOpenBoundariesAndPointOwnership();
            tests.CellAssignment_EmitsEveryIntersectedCellInRowMajorOrder();
            tests.CellAssignment_ClampsPartialOverlapAndRejectsOutsideBounds();
            tests.CellAssignment_RejectsInvalidGridOrBounds();
            tests.MultiCellGather_DeduplicatesAndSortsPlacementIndices();
            tests.MultiCellGather_RejectsInvalidRangesAndIndices();
            tests.PolicyClassifier_MapsEverySupportedSurfaceAndShadowCombination();
            tests.PolicyClassifier_PreservesCompleteFixedFilterIdentity();
            tests.PolicyClassifier_UsesExplicitAlwaysResidentBucket();
            tests.PolicyClassifier_RejectsUnsupportedOrUnknownCombinations();
            tests.CapacitySweep_IsOrderIndependentAndSortedByPolicy();
            tests.CapacitySweep_UsesPeakAndExactTwentyPercentCeiling();
            tests.CapacitySweep_RequiresIdenticalCanonicalSamplesPerPolicy();
            tests.CapacitySweep_RejectsInvalidDuplicateNegativeAndOverflowInputs();
            tests.VirtualizationReport_SerializesDeterministicallyAndRoundTrips();
            tests.VirtualizationReport_RejectsMissingUnknownAndDuplicateProperties();
            tests.VirtualizationReport_RejectsDefaultAndNegativeMetrics();
            tests.VirtualizationReport_RejectsCapacityReconciliationFailures();
            tests.RenderDatabaseBakeConfig_IsGeneratedOnlyAndRetainsCompleteSchema();
            tests.RenderDatabaseBakeConfig_RejectsMissingOrCorruptRecords();
            tests.SharedRenderMeshArray_PreservesSortedAssetsAndEveryLogicalIndex();
            tests.ProxySlotBakePlan_UsesEveryReportedSlotAndExactFixedPolicy();
            tests.ProxySlots_BakeAsDisabledLeafEntitiesWithExactBucketRanges();
            tests.EligibleSourceRows_BakeOnlyWhileGameplayAndResidentOwnersSurvive();
            tests.VirtualizedBuilding_ReplacesOnlyRenderRootOwnershipWithStateIndex();
            Debug.Log("[OperationMapRenderVirtualizationValidation] result=Passed tests=43");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[OperationMapRenderVirtualizationValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void RenderResidencyMode_IsClosedAndStable()
    {
        Assert.That((byte)OperationMapRenderResidencyMode.ResidentEntities, Is.EqualTo(0));
        Assert.That((byte)OperationMapRenderResidencyMode.VirtualizedProxyPool, Is.EqualTo(1));
        Assert.That(
            Enum.GetValues(typeof(OperationMapRenderResidencyMode)).Length,
            Is.EqualTo(2));
    }

    [Test]
    public void OperationMapDefinition_DefaultsToResidentEntities()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        try
        {
            Assert.That(
                definition.RenderResidencyMode,
                Is.EqualTo(OperationMapRenderResidencyMode.ResidentEntities));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void StaticSceneChunks_RejectsVirtualizedProxyPool()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(
                definition,
                "renderResidencyMode",
                OperationMapRenderResidencyMode.VirtualizedProxyPool);

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("require ResidentEntities"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void EntityScene_ResidentEntitiesRetainsCurrentBehavior()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Set(definition, "staticPresentationManifestReference", new AssetReference());

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.True, error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void EntityScene_VirtualizedProxyPoolFailsClosedUntilDatabaseContractExists()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(definition, "presentationKind", OperationMapPresentationKind.EntityScene);
            Set(
                definition,
                "renderResidencyMode",
                OperationMapRenderResidencyMode.VirtualizedProxyPool);
            Set(definition, "staticPresentationManifestReference", new AssetReference());

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("validated render-virtualization database"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void UnknownRenderResidencyMode_IsRejected()
    {
        OperationMapDefinition definition = CreateDefinitionWithRequiredLocalReferences();
        try
        {
            Set(definition, "renderResidencyMode", (OperationMapRenderResidencyMode)byte.MaxValue);

            Assert.That(definition.TryValidateLocalContentReferences(out string error), Is.False);
            Assert.That(error, Does.Contain("Unknown operation-map render-residency mode: 255"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void RenderVirtualizationSchema_IsUnmanaged()
    {
        AssertUnmanaged<OperationMapRenderBoundsBlob>();
        AssertUnmanaged<OperationMapRenderIdentity128>();
        AssertUnmanaged<OperationMapRenderDatabaseBlob>();
        AssertUnmanaged<OperationMapRenderPrototypeBlob>();
        AssertUnmanaged<OperationMapRenderPrototypePartBlob>();
        AssertUnmanaged<OperationMapRenderPlacementBlob>();
        AssertUnmanaged<OperationMapRenderCellBlob>();
        AssertUnmanaged<OperationMapRenderPoolBucketBlob>();
        AssertUnmanaged<OperationMapRenderDatabaseComponent>();
        AssertUnmanaged<OperationMapRenderProxySlotComponent>();
        AssertUnmanaged<OperationMapRenderVirtualizationStateComponent>();
        AssertUnmanaged<OperationMapVirtualizedBuildingPresentationComponent>();
        AssertUnmanaged<OperationMapRenderStateChangeComponent>();
        AssertUnmanaged<OperationMapRenderVirtualizationMetricsComponent>();
    }

    [Test]
    public void RuntimeContracts_UseExpectedEcsKinds()
    {
        AssertComponent<OperationMapRenderDatabaseComponent>();
        AssertComponent<OperationMapRenderProxySlotComponent>();
        AssertComponent<OperationMapRenderVirtualizationStateComponent>();
        AssertComponent<OperationMapVirtualizedBuildingPresentationComponent>();
        AssertBuffer<OperationMapRenderStateChangeComponent>();
        AssertComponent<OperationMapRenderVirtualizationMetricsComponent>();
    }

    [Test]
    public void RenderPolicyAndStateEnums_AreClosed()
    {
        Assert.That(Enum.GetValues(typeof(OperationMapRenderPolicyBucket)).Length, Is.EqualTo(6));
        Assert.That(
            OperationMapRenderPolicyBucket.TransparentShadowsOff,
            Is.Not.EqualTo(OperationMapRenderPolicyBucket.OpaqueShadowsOff));
        Assert.That(Enum.GetValues(typeof(OperationMapRenderVisualState)).Length, Is.EqualTo(3));
        Assert.That(Enum.GetValues(typeof(OperationMapRenderRebuildReason)).Length, Is.EqualTo(5));
    }

    [Test]
    public void DatabaseBlob_RetainsEveryRequiredLogicalField()
    {
        using BlobBuilder builder = new(Allocator.Temp);
        ref OperationMapRenderDatabaseBlob root =
            ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        root.OperationMapId = new FixedString64Bytes("opmap.skirmish.virtualized");
        root.ContentHash = new FixedString128Bytes(new string('a', 64));
        root.SchemaVersion = 1;
        root.CellSize = 32f;
        root.GridOrigin = new float3(-64f, 0f, -64f);
        root.GridDimensions = new int2(4, 4);

        BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
            builder.Allocate(ref root.Prototypes, 1);
        prototypes[0] = new OperationMapRenderPrototypeBlob
        {
            ContentIdentity = Identity(11ul, 12ul),
            FirstPart = 0,
            PartCount = 1,
            CombinedLocalBounds = Bounds(float3.zero, new float3(2f)),
            SemanticCategory = DenseCityPresentationSemanticCategory.Vegetation,
            EligibilityFlags = OperationMapRenderEligibilityFlags.Eligible
        };

        BlobBuilderArray<OperationMapRenderPrototypePartBlob> parts =
            builder.Allocate(ref root.Parts, 1);
        parts[0] = new OperationMapRenderPrototypePartBlob
        {
            RendererPathHash = Identity(21ul, 22ul),
            MeshArrayIndex = 3,
            MaterialArrayIndex = 4,
            SubMeshIndex = 1,
            LocalToPlacement = float4x4.identity,
            LocalBounds = Bounds(new float3(0f, 1f, 0f), new float3(1f)),
            LinearBaseColor = new float4(0.25f, 0.5f, 0.75f, 1f),
            PolicyBucket = OperationMapRenderPolicyBucket.AlphaClippedShadowsOn,
            PoolBucketIndex = 0,
            LodFlags = OperationMapRenderLodFlags.Lod0,
            ShadowFlags = OperationMapRenderShadowFlags.CastShadows |
                          OperationMapRenderShadowFlags.ReceiveShadows
        };

        BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
            builder.Allocate(ref root.Placements, 1);
        placements[0] = new OperationMapRenderPlacementBlob
        {
            StableIdentityHash = Identity(31ul, 32ul),
            PrototypeIndex = 0,
            WorldMatrix = float4x4.Translate(new float3(10f, 0f, 20f)),
            CellIndex = 0,
            StateOwnerIndex = -1,
            RequiredVisualState = OperationMapRenderVisualState.Any,
            Priority = 7,
            SemanticCategory = DenseCityPresentationSemanticCategory.Vegetation
        };

        BlobBuilderArray<OperationMapRenderCellBlob> cells =
            builder.Allocate(ref root.Cells, 1);
        cells[0] = new OperationMapRenderCellBlob
        {
            Coordinate = new int2(2, 3),
            WorldBounds = Bounds(new float3(16f, 0f, 16f), new float3(16f)),
            FirstPlacementIndex = 0,
            PlacementIndexCount = 1
        };
        builder.Allocate(ref root.CellPlacementIndices, 1)[0] = 0;

        BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
            builder.Allocate(ref root.PoolBuckets, 1);
        buckets[0] = new OperationMapRenderPoolBucketBlob
        {
            PolicyBucket = OperationMapRenderPolicyBucket.AlphaClippedShadowsOn,
            Layer = 2,
            RenderingLayerMask = 4u,
            MotionVectorMode = OperationMapRenderMotionVectorMode.ForceNoMotion,
            ShadowFlags = OperationMapRenderShadowFlags.CastShadows |
                          OperationMapRenderShadowFlags.ReceiveShadows,
            FirstSlot = 10,
            Capacity = 120,
            PeakRequiredCount = 100,
            HeadroomCount = 20,
            ReportIdentity = Identity(41ul, 42ul)
        };

        BlobAssetReference<OperationMapRenderDatabaseBlob> blob =
            builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(Allocator.Temp);
        try
        {
            Assert.That(blob.Value.OperationMapId.ToString(), Is.EqualTo("opmap.skirmish.virtualized"));
            Assert.That(blob.Value.Prototypes[0].PartCount, Is.EqualTo(1));
            Assert.That(blob.Value.Parts[0].MeshArrayIndex, Is.EqualTo(3));
            Assert.That(blob.Value.Parts[0].PoolBucketIndex, Is.EqualTo(0));
            Assert.That(blob.Value.Placements[0].StateOwnerIndex, Is.EqualTo(-1));
            Assert.That(blob.Value.Cells[0].Coordinate, Is.EqualTo(new int2(2, 3)));
            Assert.That(blob.Value.CellPlacementIndices[0], Is.EqualTo(0));
            Assert.That(blob.Value.PoolBuckets[0].HeadroomCount, Is.EqualTo(20));
            Assert.That(blob.Value.PoolBuckets[0].Layer, Is.EqualTo(2));
            Assert.That(blob.Value.PoolBuckets[0].RenderingLayerMask, Is.EqualTo(4u));
        }
        finally
        {
            blob.Dispose();
        }
    }

    [Test]
    public void RuntimeComponents_RetainRequiredState()
    {
        OperationMapRenderProxySlotComponent slot = new()
        {
            SlotIndex = 9,
            PoolBucketIndex = 2,
            PlacementIndex = -1,
            PartIndex = -1,
            AssignmentGeneration = 5
        };
        OperationMapRenderVirtualizationStateComponent state = new()
        {
            Initialized = 1,
            InitialViewApplied = 0,
            ActiveEnvelopeMin = new int2(-2, -3),
            ActiveEnvelopeMax = new int2(4, 5),
            CameraSignature = Identity(51ul, 52ul),
            ActiveSlotCount = 7,
            DirtyPlacementCount = 2,
            OverflowCount = 0,
            RebuildCount = 1
        };
        OperationMapRenderVirtualizationMetricsComponent metrics = new()
        {
            Capacity = 120,
            EnabledSlotCount = 100,
            RetainedCount = 80,
            ReleasedCount = 10,
            ReboundCount = 10,
            OverflowCount = 0,
            RebuildReason = OperationMapRenderRebuildReason.InitialView
        };

        Assert.That(slot.PlacementIndex, Is.EqualTo(-1));
        Assert.That(state.Initialized, Is.EqualTo(1));
        Assert.That(state.InitialViewApplied, Is.Zero);
        Assert.That(state.CameraSignature.Low, Is.EqualTo(51ul));
        Assert.That(state.CameraSignature.High, Is.EqualTo(52ul));
        Assert.That(metrics.EnabledSlotCount, Is.LessThanOrEqualTo(metrics.Capacity));
        Assert.That(metrics.OverflowCount, Is.Zero);
    }

    [Test]
    public void IdentityProjection_IsExactAndRepeatable()
    {
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                "dense.city.test",
                out OperationMapRenderIdentity128 first,
                out string firstError),
            Is.True,
            firstError);
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                "dense.city.test",
                out OperationMapRenderIdentity128 second,
                out string secondError),
            Is.True,
            secondError);

        Assert.That(first.Low, Is.EqualTo(1444930817465541404ul));
        Assert.That(first.High, Is.EqualTo(16092883370877825258ul));
        Assert.That(second.Low, Is.EqualTo(first.Low));
        Assert.That(second.High, Is.EqualTo(first.High));
    }

    [Test]
    public void IdentityProjection_RejectsEmptySources()
    {
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                string.Empty,
                out OperationMapRenderIdentity128 identity,
                out string error),
            Is.False);
        Assert.That(identity.Low, Is.Zero);
        Assert.That(identity.High, Is.Zero);
        Assert.That(error, Does.Contain("non-empty"));
    }

    [Test]
    public void IdentityCollisionDetector_RejectsDifferentSourcesForOneIdentity()
    {
        OperationMapRenderIdentityCollisionDetector detector = new();
        OperationMapRenderIdentity128 forcedIdentity = Identity(71ul, 72ul);

        Assert.That(
            detector.TryRegister(forcedIdentity, "stable.alpha", out string firstError),
            Is.True,
            firstError);
        Assert.That(
            detector.TryRegister(forcedIdentity, "stable.alpha", out string repeatError),
            Is.True,
            repeatError);
        Assert.That(
            detector.TryRegister(forcedIdentity, "stable.beta", out string collisionError),
            Is.False);
        Assert.That(collisionError, Does.Contain("stable.alpha"));
        Assert.That(collisionError, Does.Contain("stable.beta"));
        Assert.That(detector.Count, Is.EqualTo(1));
    }

    [Test]
    public void IdentityComparer_SortsByLowThenHighDeterministically()
    {
        List<OperationMapRenderIdentity128> first = new()
        {
            Identity(2ul, 0ul),
            Identity(1ul, 9ul),
            Identity(1ul, 1ul)
        };
        List<OperationMapRenderIdentity128> second = new(first);

        first.Sort(OperationMapRenderIdentityComparer.Instance);
        second.Sort(OperationMapRenderIdentityComparer.Instance);

        Assert.That(first[0].Low, Is.EqualTo(1ul));
        Assert.That(first[0].High, Is.EqualTo(1ul));
        Assert.That(first[1].Low, Is.EqualTo(1ul));
        Assert.That(first[1].High, Is.EqualTo(9ul));
        Assert.That(first[2].Low, Is.EqualTo(2ul));
        for (int index = 0; index < first.Count; index++)
        {
            Assert.That(second[index].Low, Is.EqualTo(first[index].Low));
            Assert.That(second[index].High, Is.EqualTo(first[index].High));
        }
    }

    [Test]
    public void PrototypeFingerprint_IsDeterministic()
    {
        OperationMapRenderPrototypeFingerprintInput input = CreatePrototypeFingerprintInput();
        OperationMapRenderIdentity128 first = Fingerprint(input);
        OperationMapRenderIdentity128 second = Fingerprint(input);

        Assert.That(second.Low, Is.EqualTo(first.Low));
        Assert.That(second.High, Is.EqualTo(first.High));
    }

    [Test]
    public void PrototypeFingerprint_ChangesForEveryContractField()
    {
        OperationMapRenderPrototypeFingerprintInput baseline = CreatePrototypeFingerprintInput();
        HashSet<string> fingerprints = new()
        {
            IdentityKey(Fingerprint(baseline))
        };

        OperationMapRenderPrototypeFingerprintInput changed = baseline;
        changed.RendererPath = "Root/Changed";
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.MeshAssetGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.MeshLocalId = 101;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.MaterialAssetGuid = "dddddddddddddddddddddddddddddddd";
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.MaterialLocalId = 201;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.SubMeshIndex = 2;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.LocalToPlacement.c3.x = 3f;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.LocalBounds.Center.y = 4f;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.LocalBounds.Extents.z = 5f;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.LinearBaseColor.x = 0.9f;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.PolicyBucket = OperationMapRenderPolicyBucket.OpaqueShadowsOn;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.Layer = 3;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.RenderingLayerMask = 8u;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.MotionVectorMode = OperationMapRenderMotionVectorMode.Object;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.ShadowFlags = OperationMapRenderShadowFlags.CastShadows;
        AssertUnique(fingerprints, changed);
        changed = baseline;
        changed.LodFlags = OperationMapRenderLodFlags.Lod1;
        AssertUnique(fingerprints, changed);

        Assert.That(fingerprints.Count, Is.EqualTo(17));
    }

    [Test]
    public void PrototypeFingerprint_RejectsInvalidInputs()
    {
        OperationMapRenderPrototypeFingerprintInput input = CreatePrototypeFingerprintInput();
        input.RendererPath = "C:\\session\\Renderer";
        AssertFingerprintRejected(input, "Renderer path");

        input = CreatePrototypeFingerprintInput();
        input.MeshAssetGuid = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        AssertFingerprintRejected(input, "Mesh identity");

        input = CreatePrototypeFingerprintInput();
        input.LocalToPlacement.c0.x = float.NaN;
        AssertFingerprintRejected(input, "finite");

        input = CreatePrototypeFingerprintInput();
        input.LocalBounds.Extents.x = -1f;
        AssertFingerprintRejected(input, "nonnegative extents");

        input = CreatePrototypeFingerprintInput();
        input.PolicyBucket = (OperationMapRenderPolicyBucket)byte.MaxValue;
        AssertFingerprintRejected(input, "Unknown render-policy bucket");

        input = CreatePrototypeFingerprintInput();
        input.ShadowFlags = (OperationMapRenderShadowFlags)(1 << 7);
        AssertFingerprintRejected(input, "Unknown render shadow flags");

        input = CreatePrototypeFingerprintInput();
        input.RenderingLayerMask = 0u;
        AssertFingerprintRejected(input, "Rendering-layer mask");

        input = CreatePrototypeFingerprintInput();
        input.MotionVectorMode = (OperationMapRenderMotionVectorMode)byte.MaxValue;
        AssertFingerprintRejected(input, "Unknown motion-vector mode");

        input = CreatePrototypeFingerprintInput();
        input.LodFlags = OperationMapRenderLodFlags.None;
        AssertFingerprintRejected(input, "Invalid render LOD flags");
    }

    [Test]
    public void CellAssignment_UsesHalfOpenBoundariesAndPointOwnership()
    {
        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                Bounds(new float3(16f, 0f, 16f), new float3(16f, 1f, 16f)),
                32f,
                float3.zero,
                new int2(4, 4),
                out int[] firstCell,
                out string firstError),
            Is.True,
            firstError);
        Assert.That(firstCell, Is.EqualTo(new[] { 0 }));

        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                Bounds(new float3(32f, 0f, 32f), float3.zero),
                32f,
                float3.zero,
                new int2(4, 4),
                out int[] boundaryPoint,
                out string pointError),
            Is.True,
            pointError);
        Assert.That(boundaryPoint, Is.EqualTo(new[] { 5 }));
    }

    [Test]
    public void CellAssignment_EmitsEveryIntersectedCellInRowMajorOrder()
    {
        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                Bounds(new float3(32f, 0f, 32f), new float3(2f, 1f, 2f)),
                32f,
                float3.zero,
                new int2(4, 4),
                out int[] cells,
                out string error),
            Is.True,
            error);

        Assert.That(cells, Is.EqualTo(new[] { 0, 1, 4, 5 }));
    }

    [Test]
    public void CellAssignment_ClampsPartialOverlapAndRejectsOutsideBounds()
    {
        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                Bounds(new float3(-1f, 0f, 16f), new float3(2f, 1f, 4f)),
                32f,
                float3.zero,
                new int2(4, 4),
                out int[] partial,
                out string partialError),
            Is.True,
            partialError);
        Assert.That(partial, Is.EqualTo(new[] { 0 }));

        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                Bounds(new float3(-4f, 0f, 16f), new float3(1f)),
                32f,
                float3.zero,
                new int2(4, 4),
                out int[] outside,
                out string outsideError),
            Is.False);
        Assert.That(outside, Is.Empty);
        Assert.That(outsideError, Does.Contain("do not intersect"));
    }

    [Test]
    public void CellAssignment_RejectsInvalidGridOrBounds()
    {
        OperationMapRenderBoundsBlob invalidBounds =
            Bounds(float3.zero, new float3(-1f, 1f, 1f));
        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                invalidBounds,
                32f,
                float3.zero,
                new int2(4, 4),
                out _,
                out string boundsError),
            Is.False);
        Assert.That(boundsError, Does.Contain("nonnegative extents"));

        Assert.That(
            OperationMapRenderCellAssignment.TryAssign(
                Bounds(float3.zero, new float3(1f)),
                0f,
                float3.zero,
                new int2(4, 4),
                out _,
                out string gridError),
            Is.False);
        Assert.That(gridError, Does.Contain("positive cell size"));
    }

    [Test]
    public void MultiCellGather_DeduplicatesAndSortsPlacementIndices()
    {
        int[] cellPlacementIndices = { 5, 2, 5, 7, 2, 3 };
        OperationMapRenderCellRange[] ranges =
        {
            new(0, 3),
            new(2, 4),
            new(0, 3)
        };

        Assert.That(
            OperationMapRenderCellAssignment.TryGatherUnique(
                cellPlacementIndices,
                ranges,
                8,
                out int[] unique,
                out string error),
            Is.True,
            error);
        Assert.That(unique, Is.EqualTo(new[] { 2, 3, 5, 7 }));
    }

    [Test]
    public void MultiCellGather_RejectsInvalidRangesAndIndices()
    {
        Assert.That(
            OperationMapRenderCellAssignment.TryGatherUnique(
                new[] { 0, 1 },
                new[] { new OperationMapRenderCellRange(1, 2) },
                2,
                out int[] invalidRange,
                out string rangeError),
            Is.False);
        Assert.That(invalidRange, Is.Empty);
        Assert.That(rangeError, Does.Contain("outside"));

        Assert.That(
            OperationMapRenderCellAssignment.TryGatherUnique(
                new[] { 0, 3 },
                new[] { new OperationMapRenderCellRange(0, 2) },
                3,
                out int[] invalidIndex,
                out string indexError),
            Is.False);
        Assert.That(invalidIndex, Is.Empty);
        Assert.That(indexError, Does.Contain("outside [0,3)"));
    }

    [Test]
    public void PolicyClassifier_MapsEverySupportedSurfaceAndShadowCombination()
    {
        AssertPolicyBucket(
            OperationMapRenderMaterialSurface.Opaque,
            OperationMapRenderShadowFlags.CastShadows,
            OperationMapRenderPolicyBucket.OpaqueShadowsOn);
        AssertPolicyBucket(
            OperationMapRenderMaterialSurface.Opaque,
            OperationMapRenderShadowFlags.None,
            OperationMapRenderPolicyBucket.OpaqueShadowsOff);
        AssertPolicyBucket(
            OperationMapRenderMaterialSurface.AlphaClipped,
            OperationMapRenderShadowFlags.CastShadows,
            OperationMapRenderPolicyBucket.AlphaClippedShadowsOn);
        AssertPolicyBucket(
            OperationMapRenderMaterialSurface.AlphaClipped,
            OperationMapRenderShadowFlags.None,
            OperationMapRenderPolicyBucket.AlphaClippedShadowsOff);
        AssertPolicyBucket(
            OperationMapRenderMaterialSurface.Transparent,
            OperationMapRenderShadowFlags.ReceiveShadows,
            OperationMapRenderPolicyBucket.TransparentShadowsOff);
    }

    [Test]
    public void PolicyClassifier_PreservesCompleteFixedFilterIdentity()
    {
        OperationMapRenderPolicyClassificationInput baseline =
            PolicyInput(OperationMapRenderMaterialSurface.Opaque);
        Assert.That(
            OperationMapRenderPolicyClassifier.TryClassify(
                baseline,
                out OperationMapRenderPolicyKey first,
                out string firstError),
            Is.True,
            firstError);

        OperationMapRenderPolicyClassificationInput differentLayer =
            PolicyInput(OperationMapRenderMaterialSurface.Opaque, layer: 3);
        OperationMapRenderPolicyClassificationInput differentRenderingLayer =
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                renderingLayerMask: 4u);
        OperationMapRenderPolicyClassificationInput differentMotion =
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                motionVectorMode: OperationMapRenderMotionVectorMode.Object);
        OperationMapRenderPolicyClassificationInput differentReceive =
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                shadowFlags: OperationMapRenderShadowFlags.ReceiveShadows);

        AssertPolicyDiffers(first, differentLayer);
        AssertPolicyDiffers(first, differentRenderingLayer);
        AssertPolicyDiffers(first, differentMotion);
        AssertPolicyDiffers(first, differentReceive);
    }

    [Test]
    public void PolicyClassifier_UsesExplicitAlwaysResidentBucket()
    {
        OperationMapRenderPolicyClassificationInput input =
            PolicyInput(
                OperationMapRenderMaterialSurface.AlphaClipped,
                shadowFlags: OperationMapRenderShadowFlags.CastShadows |
                             OperationMapRenderShadowFlags.ReceiveShadows,
                alwaysResidentException: true);

        Assert.That(
            OperationMapRenderPolicyClassifier.TryClassify(
                input,
                out OperationMapRenderPolicyKey policy,
                out string error),
            Is.True,
            error);
        Assert.That(
            policy.Bucket,
            Is.EqualTo(OperationMapRenderPolicyBucket.AlwaysResidentException));
        Assert.That(
            policy.ShadowFlags,
            Is.EqualTo(
                OperationMapRenderShadowFlags.CastShadows |
                OperationMapRenderShadowFlags.ReceiveShadows));
    }

    [Test]
    public void PolicyClassifier_RejectsUnsupportedOrUnknownCombinations()
    {
        AssertPolicyRejected(
            PolicyInput(
                OperationMapRenderMaterialSurface.Transparent,
                shadowFlags: OperationMapRenderShadowFlags.CastShadows),
            "Transparent render policy");
        AssertPolicyRejected(
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                shadowFlags: OperationMapRenderShadowFlags.StaticShadowCaster),
            "requires CastShadows");
        AssertPolicyRejected(
            PolicyInput((OperationMapRenderMaterialSurface)byte.MaxValue),
            "Unknown material surface");
        AssertPolicyRejected(
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                motionVectorMode: (OperationMapRenderMotionVectorMode)byte.MaxValue),
            "Unknown motion-vector mode");
        AssertPolicyRejected(
            PolicyInput(OperationMapRenderMaterialSurface.Opaque, layer: 32),
            "must be in [0,31]");
        AssertPolicyRejected(
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                renderingLayerMask: 0u),
            "must contain at least one layer");
        AssertPolicyRejected(
            PolicyInput(
                OperationMapRenderMaterialSurface.Opaque,
                shadowFlags: (OperationMapRenderShadowFlags)(1 << 7)),
            "Unknown render shadow flags");
    }

    [Test]
    public void CapacitySweep_IsOrderIndependentAndSortedByPolicy()
    {
        OperationMapRenderPolicyKey opaque =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.Opaque,
                OperationMapRenderShadowFlags.CastShadows);
        OperationMapRenderPolicyKey alpha =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.AlphaClipped,
                OperationMapRenderShadowFlags.None);
        OperationMapRenderCapacitySweepInput[] forward =
        {
            new("route.b", alpha, 2),
            new("route.a", opaque, 7),
            new("route.a", alpha, 5),
            new("route.b", opaque, 3)
        };
        OperationMapRenderCapacitySweepInput[] reverse =
        {
            forward[3],
            forward[2],
            forward[1],
            forward[0]
        };

        Assert.That(
            OperationMapRenderCapacitySweep.TryCalculate(
                forward,
                out OperationMapRenderCapacitySweepResult[] first,
                out string firstError),
            Is.True,
            firstError);
        Assert.That(
            OperationMapRenderCapacitySweep.TryCalculate(
                reverse,
                out OperationMapRenderCapacitySweepResult[] second,
                out string secondError),
            Is.True,
            secondError);

        Assert.That(first.Length, Is.EqualTo(2));
        Assert.That(
            first[0].Policy.Bucket,
            Is.EqualTo(OperationMapRenderPolicyBucket.OpaqueShadowsOn));
        Assert.That(
            first[1].Policy.Bucket,
            Is.EqualTo(OperationMapRenderPolicyBucket.AlphaClippedShadowsOff));
        for (int index = 0; index < first.Length; index++)
        {
            Assert.That(second[index].Policy, Is.EqualTo(first[index].Policy));
            Assert.That(
                second[index].PeakRequiredPartRows,
                Is.EqualTo(first[index].PeakRequiredPartRows));
            Assert.That(second[index].Capacity, Is.EqualTo(first[index].Capacity));
            Assert.That(second[index].HeadroomCount, Is.EqualTo(first[index].HeadroomCount));
            Assert.That(second[index].SweepSampleCount, Is.EqualTo(2));
        }
    }

    [Test]
    public void CapacitySweep_UsesPeakAndExactTwentyPercentCeiling()
    {
        OperationMapRenderPolicyKey opaque =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.Opaque,
                OperationMapRenderShadowFlags.None);
        OperationMapRenderCapacitySweepInput[] inputs =
        {
            new("pose.normal", opaque, 1),
            new("pose.build", opaque, 5),
            new("pose.map", opaque, 6),
            new("pose.zoom", opaque, 10)
        };

        Assert.That(
            OperationMapRenderCapacitySweep.TryCalculate(
                inputs,
                out OperationMapRenderCapacitySweepResult[] results,
                out string error),
            Is.True,
            error);
        Assert.That(results.Length, Is.EqualTo(1));
        Assert.That(results[0].SweepSampleCount, Is.EqualTo(4));
        Assert.That(results[0].PeakRequiredPartRows, Is.EqualTo(10));
        Assert.That(results[0].Capacity, Is.EqualTo(12));
        Assert.That(results[0].HeadroomCount, Is.EqualTo(2));

        Assert.That(
            OperationMapRenderCapacitySweep.TryCalculate(
                new[] { new OperationMapRenderCapacitySweepInput("pose.one", opaque, 6) },
                out results,
                out error),
            Is.True,
            error);
        Assert.That(results[0].Capacity, Is.EqualTo(8));
        Assert.That(results[0].HeadroomCount, Is.EqualTo(2));
    }

    [Test]
    public void CapacitySweep_RequiresIdenticalCanonicalSamplesPerPolicy()
    {
        OperationMapRenderPolicyKey opaque =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.Opaque,
                OperationMapRenderShadowFlags.None);
        OperationMapRenderPolicyKey alpha =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.AlphaClipped,
                OperationMapRenderShadowFlags.None);
        OperationMapRenderCapacitySweepInput[] inputs =
        {
            new("pose.normal", opaque, 2),
            new("pose.build", opaque, 4),
            new("pose.normal", alpha, 1)
        };

        Assert.That(
            OperationMapRenderCapacitySweep.TryCalculate(inputs, out _, out string error),
            Is.False);
        Assert.That(error, Does.Contain("identical canonical sweep sample set"));
    }

    [Test]
    public void CapacitySweep_RejectsInvalidDuplicateNegativeAndOverflowInputs()
    {
        OperationMapRenderPolicyKey opaque =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.Opaque,
                OperationMapRenderShadowFlags.None);
        AssertCapacitySweepRejected(null, "at least one input");
        AssertCapacitySweepRejected(
            Array.Empty<OperationMapRenderCapacitySweepInput>(),
            "at least one input");
        AssertCapacitySweepRejected(
            new[] { new OperationMapRenderCapacitySweepInput("", opaque, 1) },
            "empty sample identity");
        AssertCapacitySweepRejected(
            new[]
            {
                new OperationMapRenderCapacitySweepInput("pose", opaque, 1),
                new OperationMapRenderCapacitySweepInput("pose", opaque, 2)
            },
            "Duplicate capacity sweep sample");
        AssertCapacitySweepRejected(
            new[] { new OperationMapRenderCapacitySweepInput("pose", opaque, -1) },
            "negative required part rows");
        AssertCapacitySweepRejected(
            new[]
            {
                new OperationMapRenderCapacitySweepInput(
                    "pose",
                    new OperationMapRenderPolicyKey(
                        OperationMapRenderPolicyBucket.OpaqueShadowsOn,
                        0,
                        1u,
                        OperationMapRenderMotionVectorMode.ForceNoMotion,
                        OperationMapRenderShadowFlags.None),
                    1)
            },
            "requires CastShadows");
        AssertCapacitySweepRejected(
            new[]
            {
                new OperationMapRenderCapacitySweepInput(
                    "pose",
                    opaque,
                    int.MaxValue)
            },
            "exceeds Int32");
    }

    [Test]
    public void VirtualizationReport_SerializesDeterministicallyAndRoundTrips()
    {
        OperationMapRenderVirtualizationReportDocument report = CreateValidReport();
        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TrySerialize(
                report,
                out string firstJson,
                out string firstError),
            Is.True,
            firstError);
        Assert.That(firstJson, Does.EndWith("\n"));
        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TrySerialize(
                report,
                out string secondJson,
                out string secondError),
            Is.True,
            secondError);
        Assert.That(secondJson, Is.EqualTo(firstJson));

        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TryDeserialize(
                firstJson,
                out OperationMapRenderVirtualizationReportDocument roundTrip,
                out string readError),
            Is.True,
            readError);
        Assert.That(roundTrip.SchemaVersion, Is.EqualTo(1));
        Assert.That(roundTrip.OperationMapId, Is.EqualTo("opmap.skirmish.virtualized"));
        Assert.That(roundTrip.ContentHash, Is.EqualTo(new string('a', 64)));
        Assert.That(
            roundTrip.ResidencyMode,
            Is.EqualTo(OperationMapRenderResidencyMode.VirtualizedProxyPool));
        Assert.That(roundTrip.CapacityByPolicy.Length, Is.EqualTo(2));
        Assert.That(roundTrip.TotalSlotCount, Is.EqualTo(18));

        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TrySerialize(
                roundTrip,
                out string roundTripJson,
                out string roundTripError),
            Is.True,
            roundTripError);
        Assert.That(roundTripJson, Is.EqualTo(firstJson));
    }

    [Test]
    public void VirtualizationReport_RejectsMissingUnknownAndDuplicateProperties()
    {
        string validJson = SerializeValidReport();

        AssertReportDeserializeRejected(
            RemoveJsonPropertyLine(validJson, "prototypeCount"),
            "missing required property 'prototypeCount'");

        int finalBrace = validJson.LastIndexOf('}');
        string unknown =
            validJson.Insert(finalBrace, ",\n  \"unexpected\": 1\n");
        AssertReportDeserializeRejected(
            unknown,
            "unknown property 'unexpected'");

        string duplicate = validJson.Replace(
            "\"schemaVersion\": 1,",
            "\"schemaVersion\": 1,\n  \"schemaVersion\": 1,");
        AssertReportDeserializeRejected(duplicate, "schemaVersion");
    }

    [Test]
    public void VirtualizationReport_RejectsDefaultAndNegativeMetrics()
    {
        OperationMapRenderVirtualizationReportDocument report = CreateValidReport();
        report.SchemaVersion = 0;
        AssertReportSerializeRejected(report, "schema version must be 1");

        report = CreateValidReport();
        report.VirtualizedLogicalRows = 0;
        AssertReportSerializeRejected(report, "virtualizedLogicalRows must be positive");

        report = CreateValidReport();
        report.PackedDatabaseBytes = 0;
        AssertReportSerializeRejected(report, "packedDatabaseBytes must be positive");

        report = CreateValidReport();
        report.ResidentRenderRows = -1;
        AssertReportSerializeRejected(report, "residentRenderRows must be nonnegative");

        report = CreateValidReport();
        report.ExcludedRowCount = -1;
        AssertReportSerializeRejected(report, "excludedRowCount must be nonnegative");
    }

    [Test]
    public void VirtualizationReport_RejectsCapacityReconciliationFailures()
    {
        OperationMapRenderVirtualizationReportDocument report = CreateValidReport();
        report.TotalSlotCount--;
        AssertReportSerializeRejected(report, "does not match capacity sum");

        report = CreateValidReport();
        Array.Reverse(report.CapacityByPolicy);
        AssertReportSerializeRejected(report, "strictly sorted");

        report = CreateValidReport();
        OperationMapRenderCapacitySweepResult valid = report.CapacityByPolicy[0];
        report.CapacityByPolicy[0] = new OperationMapRenderCapacitySweepResult(
            valid.Policy,
            valid.SweepSampleCount,
            valid.PeakRequiredPartRows,
            valid.Capacity,
            valid.HeadroomCount + 1);
        AssertReportSerializeRejected(report, "exact 20% headroom");

        report = CreateValidReport();
        valid = report.CapacityByPolicy[1];
        report.CapacityByPolicy[1] = new OperationMapRenderCapacitySweepResult(
            valid.Policy,
            valid.SweepSampleCount + 1,
            valid.PeakRequiredPartRows,
            valid.Capacity,
            valid.HeadroomCount);
        AssertReportSerializeRejected(report, "same sweep sample count");
    }

    [Test]
    public void RenderDatabaseBakeConfig_IsGeneratedOnlyAndRetainsCompleteSchema()
    {
        Assert.That(
            typeof(OperationMapRenderDatabaseBakeConfig)
                .GetCustomAttribute<CreateAssetMenuAttribute>(),
            Is.Null);
        OperationMapRenderDatabaseBakeConfig generated =
            AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(
                "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/" +
                "VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset");
        Assert.That(generated, Is.Not.Null);
        Assert.That(generated.TryValidateSchema(out string generatedError), Is.True, generatedError);

        OperationMapRenderDatabaseBakeConfig config =
            CreateValidBakeConfig(out Mesh mesh, out Material material);
        try
        {
            Assert.That(config.TryValidateSchema(out string error), Is.True, error);
            Assert.That(config.SchemaVersion, Is.EqualTo(1));
            Assert.That(config.OperationMapId, Is.EqualTo("opmap.skirmish.virtualized"));
            Assert.That(config.Meshes.Count, Is.EqualTo(1));
            Assert.That(config.Materials.Count, Is.EqualTo(1));
            Assert.That(config.Prototypes.Count, Is.EqualTo(1));
            Assert.That(config.Parts[0].PoolBucketIndex, Is.EqualTo(0));
            Assert.That(config.Placements[0].StateOwnerIndex, Is.EqualTo(-1));
            Assert.That(config.Cells[0].PlacementIndexCount, Is.EqualTo(1));
            Assert.That(config.PoolBuckets[0].RenderingLayerMask, Is.EqualTo(1u));
            Assert.That(
                config.PoolBuckets[0].MotionVectorMode,
                Is.EqualTo(OperationMapRenderMotionVectorMode.ForceNoMotion));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void RenderDatabaseBakeConfig_RejectsMissingOrCorruptRecords()
    {
        OperationMapRenderDatabaseBakeConfig empty =
            ScriptableObject.CreateInstance<OperationMapRenderDatabaseBakeConfig>();
        try
        {
            Assert.That(empty.TryValidateSchema(out string emptyError), Is.False);
            Assert.That(emptyError, Does.Contain("schema must be 1"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(empty);
        }

        OperationMapRenderDatabaseBakeConfig config =
            CreateValidBakeConfig(out Mesh mesh, out Material material);
        try
        {
            Set(config, "contentHash", new string('A', 64));
            Assert.That(config.TryValidateSchema(out string hashError), Is.False);
            Assert.That(hashError, Does.Contain("lowercase hex"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        config = CreateValidBakeConfig(out mesh, out material);
        try
        {
            Set(config, "cellPlacementIndices", Array.Empty<int>());
            Assert.That(config.TryValidateSchema(out string recordsError), Is.False);
            Assert.That(recordsError, Does.Contain("nonempty cellPlacementIndices"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        config = CreateValidBakeConfig(out mesh, out material);
        try
        {
            Set(config.Parts[0], "poolBucketIndex", 1);
            Assert.That(config.TryValidateSchema(out string policyError), Is.False);
            Assert.That(policyError, Does.Contain("invalid identity, reference"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        config = CreateValidBakeConfig(out mesh, out material);
        try
        {
            Set(config.Parts[0], "subMeshIndex", mesh.subMeshCount);
            Assert.That(config.TryValidateSchema(out string subMeshError), Is.False);
            Assert.That(subMeshError, Does.Contain("invalid identity, reference"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void SharedRenderMeshArray_PreservesSortedAssetsAndEveryLogicalIndex()
    {
        OperationMapRenderDatabaseBakeConfig config =
            AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(
                "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/" +
                "VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(
            OperationMapRenderMeshArrayBuilder.TryBuild(
                config,
                out RenderMeshArray renderMeshArray,
                out string error),
            Is.True,
            error);
        Assert.That(renderMeshArray.MeshReferences, Has.Length.EqualTo(config.Meshes.Count));
        Assert.That(
            renderMeshArray.MaterialReferences,
            Has.Length.EqualTo(config.Materials.Count));

        for (int index = 0; index < config.Meshes.Count; index++)
        {
            Assert.That(
                renderMeshArray.MeshReferences[index].Value,
                Is.SameAs(config.Meshes[index].Mesh),
                $"mesh[{index}]");
        }

        for (int index = 0; index < config.Materials.Count; index++)
        {
            Assert.That(
                renderMeshArray.MaterialReferences[index].Value,
                Is.SameAs(config.Materials[index].Material),
                $"material[{index}]");
        }

        for (int index = 0; index < config.Parts.Count; index++)
        {
            OperationMapRenderPrototypePartConfigRecord part = config.Parts[index];
            Assert.That(part.MeshIndex, Is.InRange(0, config.Meshes.Count - 1));
            Assert.That(part.MaterialIndex, Is.InRange(0, config.Materials.Count - 1));
            Assert.That(
                part.SubMeshIndex,
                Is.InRange(0, config.Meshes[part.MeshIndex].Mesh.subMeshCount - 1));
            Assert.That(
                renderMeshArray.MeshReferences[part.MeshIndex].Value,
                Is.SameAs(config.Meshes[part.MeshIndex].Mesh),
                $"parts[{index}].mesh");
            Assert.That(
                renderMeshArray.MaterialReferences[part.MaterialIndex].Value,
                Is.SameAs(config.Materials[part.MaterialIndex].Material),
                $"parts[{index}].material");
        }
    }

    [Test]
    public void ProxySlotBakePlan_UsesEveryReportedSlotAndExactFixedPolicy()
    {
        OperationMapRenderDatabaseBakeConfig config =
            AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(
                "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/" +
                "VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(
            OperationMapRenderProxySlotBuilder.TryBuild(
                config,
                out OperationMapRenderProxySlotBakeDescriptor[] descriptors,
                out string error),
            Is.True,
            error);

        int expectedTotal = 0;
        for (int bucketIndex = 0; bucketIndex < config.PoolBuckets.Count; bucketIndex++)
        {
            OperationMapRenderPoolBucketConfigRecord bucket =
                config.PoolBuckets[bucketIndex];
            expectedTotal += bucket.Capacity;
            int endSlot = bucket.FirstSlot + bucket.Capacity;
            for (int slotIndex = bucket.FirstSlot; slotIndex < endSlot; slotIndex++)
            {
                OperationMapRenderProxySlotBakeDescriptor descriptor =
                    descriptors[slotIndex];
                Assert.That(descriptor.SlotIndex, Is.EqualTo(slotIndex));
                Assert.That(descriptor.PoolBucketIndex, Is.EqualTo(bucketIndex));
                Assert.That(descriptor.FilterSettings.Layer, Is.EqualTo(bucket.Layer));
                Assert.That(
                    descriptor.FilterSettings.RenderingLayerMask,
                    Is.EqualTo(bucket.RenderingLayerMask));
                Assert.That(
                    descriptor.FilterSettings.MotionMode,
                    Is.EqualTo(ToUnityMotionMode(bucket.MotionVectorMode)));
                Assert.That(
                    descriptor.FilterSettings.ShadowCastingMode,
                    Is.EqualTo(
                        HasShadowFlag(
                            bucket.ShadowFlags,
                            OperationMapRenderShadowFlags.CastShadows)
                            ? ShadowCastingMode.On
                            : ShadowCastingMode.Off));
                Assert.That(
                    descriptor.FilterSettings.ReceiveShadows,
                    Is.EqualTo(
                        HasShadowFlag(
                            bucket.ShadowFlags,
                            OperationMapRenderShadowFlags.ReceiveShadows)));
                Assert.That(
                    descriptor.FilterSettings.StaticShadowCaster,
                    Is.EqualTo(
                        HasShadowFlag(
                            bucket.ShadowFlags,
                            OperationMapRenderShadowFlags.StaticShadowCaster)));
                Assert.That(descriptor.FilterSettings.ForceMeshLod, Is.EqualTo(-1));
                Assert.That(descriptor.FilterSettings.MeshLodSelectionBias, Is.Zero);
            }
        }

        Assert.That(descriptors, Has.Length.EqualTo(expectedTotal));
        Assert.That(descriptors, Has.Length.EqualTo(704));
    }

    [Test]
    public void ProxySlots_BakeAsDisabledLeafEntitiesWithExactBucketRanges()
    {
        const string configPath =
            "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/" +
            "VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset";
        OperationMapRenderDatabaseBakeConfig config =
            AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(configPath);
        Assert.That(config, Is.Not.Null);
        Assert.That(config.TryValidateSchema(out string configError), Is.True, configError);

        GameObject root = new("VRP033 Virtualized Presentation Bake Root");
        World world = new("VRP033VirtualizedPresentationBake");
        IDisposable blobAssetStore = null;
        try
        {
            OperationMapVirtualizedPresentationAuthoring authoring =
                root.AddComponent<OperationMapVirtualizedPresentationAuthoring>();
            Set(authoring, "databaseConfig", config);
            Set(authoring, "mapGeneration", 0);

            BakeGameObjects(world, root, out blobAssetStore);

            EntityManager entityManager = world.EntityManager;
            using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>());
            Entity databaseEntity = databaseQuery.GetSingletonEntity();
            Assert.That(
                entityManager.HasComponent<
                    OperationMapRenderVirtualizationStateComponent>(databaseEntity),
                Is.True);
            Assert.That(
                entityManager.HasComponent<
                    OperationMapRenderVirtualizationMetricsComponent>(databaseEntity),
                Is.True);
            Assert.That(
                entityManager.HasBuffer<OperationMapRenderStateChangeComponent>(
                    databaseEntity),
                Is.True);
            using EntityQuery stateOwnerQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapRenderVirtualizationStateComponent>());
            Assert.That(stateOwnerQuery.CalculateEntityCount(), Is.EqualTo(1));

            EntityQuery slotsQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>());
            using NativeArray<Entity> slots = slotsQuery.ToEntityArray(Allocator.Temp);
            Assert.That(slots, Has.Length.EqualTo(704));

            var seenSlotIndices = new HashSet<int>();
            var seenByBucket = new int[config.PoolBuckets.Count];
            for (int slotArrayIndex = 0; slotArrayIndex < slots.Length; slotArrayIndex++)
            {
                Entity slotEntity = slots[slotArrayIndex];
                OperationMapRenderProxySlotComponent slot =
                    entityManager.GetComponentData<OperationMapRenderProxySlotComponent>(slotEntity);
                Assert.That(seenSlotIndices.Add(slot.SlotIndex), Is.True, $"duplicate slot {slot.SlotIndex}");
                Assert.That(slot.PoolBucketIndex, Is.InRange(0, config.PoolBuckets.Count - 1));

                OperationMapRenderPoolBucketConfigRecord bucket =
                    config.PoolBuckets[slot.PoolBucketIndex];
                Assert.That(slot.SlotIndex, Is.InRange(bucket.FirstSlot, bucket.FirstSlot + bucket.Capacity - 1));
                seenByBucket[slot.PoolBucketIndex]++;
                Assert.That(slot.PlacementIndex, Is.EqualTo(-1));
                Assert.That(slot.PartIndex, Is.EqualTo(-1));
                Assert.That(slot.AssignmentGeneration, Is.Zero);

                Assert.That(entityManager.HasComponent<RenderMeshArray>(slotEntity), Is.True);
                Assert.That(entityManager.HasComponent<RenderFilterSettings>(slotEntity), Is.True);
                Assert.That(entityManager.HasComponent<MaterialMeshInfo>(slotEntity), Is.True);
                Assert.That(entityManager.HasComponent<LocalToWorld>(slotEntity), Is.True);
                Assert.That(entityManager.HasComponent<RenderBounds>(slotEntity), Is.True);
                Assert.That(entityManager.HasComponent<URPMaterialPropertyBaseColor>(slotEntity), Is.True);
                Assert.That(entityManager.HasComponent<Parent>(slotEntity), Is.False);
                Assert.That(entityManager.HasComponent<Child>(slotEntity), Is.False);
                Assert.That(entityManager.HasComponent<LocalTransform>(slotEntity), Is.False);
                Assert.That(entityManager.IsComponentEnabled<MaterialMeshInfo>(slotEntity), Is.False);
            }

            for (int bucketIndex = 0; bucketIndex < config.PoolBuckets.Count; bucketIndex++)
            {
                OperationMapRenderPoolBucketConfigRecord bucket = config.PoolBuckets[bucketIndex];
                Assert.That(seenByBucket[bucketIndex], Is.EqualTo(bucket.Capacity), $"bucket {bucketIndex}");
            }
        }
        finally
        {
            world.Dispose();
            blobAssetStore?.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EligibleSourceRows_BakeOnlyWhileGameplayAndResidentOwnersSurvive()
    {
        const string eligibleStableId =
            "densecity.aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string residentStableId =
            "densecity.bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        const string gameplaySourceId =
            "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-100000-117742413752296747";
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                "densegenerated|" + eligibleStableId,
                out OperationMapRenderIdentity128 ownerIdentity,
                out string ownerError),
            Is.True,
            ownerError);
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                "renderer-path|<owner>",
                out OperationMapRenderIdentity128 pathIdentity,
                out string pathError),
            Is.True,
            pathError);

        OperationMapRenderDatabaseBakeConfig config =
            CreateValidBakeConfig(out Mesh mesh, out Material material);
        mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateBounds();
        Set(
            config,
            "parts",
            new[]
            {
                new OperationMapRenderPrototypePartConfigRecord(
                    pathIdentity.Low,
                    pathIdentity.High,
                    0,
                    0,
                    0,
                    Matrix4x4.identity,
                    mesh.bounds,
                    Color.white,
                    OperationMapRenderPolicyBucket.OpaqueShadowsOff,
                    0,
                    OperationMapRenderLodFlags.Lod0,
                    OperationMapRenderShadowFlags.None)
            });
        Set(
            config,
            "placements",
            new[]
            {
                new OperationMapRenderPlacementConfigRecord(
                    ownerIdentity.Low,
                    ownerIdentity.High,
                    0,
                    Matrix4x4.identity,
                    0,
                    -1,
                    OperationMapRenderVisualState.Any,
                    0,
                    DenseCityPresentationSemanticCategory.Vegetation)
            });
        Assert.That(config.TryValidateSchema(out string configError), Is.True, configError);

        GameObject root = new("VRP034 Virtualized Presentation Bake Root");
        GameObject sources = new("VRP034 Source Presentation");
        sources.transform.SetParent(root.transform, false);
        World world = new("VRP034SourceOwnershipBake");
        IDisposable blobAssetStore = null;
        try
        {
            OperationMapVirtualizedPresentationAuthoring virtualization =
                root.AddComponent<OperationMapVirtualizedPresentationAuthoring>();
            Set(virtualization, "databaseConfig", config);
            Set(virtualization, "sourcePresentationRoot", sources);

            GameObject eligible = CreateRenderOwner(
                "VRP034 Eligible RenderOnly",
                sources.transform,
                mesh,
                material);
            eligible.AddComponent<DenseCityPresentationIdentityAuthoring>()
                .ConfigureForEditor(
                    eligibleStableId,
                    OperationMapEntityPresentationRole.RenderOnly,
                    DenseCityPresentationSemanticCategory.Vegetation);

            GameObject resident = CreateRenderOwner(
                "VRP034 Named Resident Exception",
                sources.transform,
                mesh,
                material);
            resident.AddComponent<DenseCityPresentationIdentityAuthoring>()
                .ConfigureForEditor(
                    residentStableId,
                    OperationMapEntityPresentationRole.RenderOnly,
                    DenseCityPresentationSemanticCategory.Infrastructure);

            GameObject gameplay = CreateRenderOwner(
                "VRP034 Canonical Gameplay Owner",
                sources.transform,
                mesh,
                material);
            gameplay.AddComponent<OperationMapEntityPresentationIdentityAuthoring>()
                .ConfigureForEditor(
                    "opmap.skirmish.virtualized",
                    gameplaySourceId,
                    OperationMapEntityPresentationRole.GameplayBuildings,
                    0);

            BakeGameObjects(world, root, out blobAssetStore);

            EntityManager entityManager = world.EntityManager;
            Entity eligibleEntity = FindNamedRenderEntity(
                entityManager,
                eligible.name);
            Entity residentEntity = FindNamedRenderEntity(
                entityManager,
                resident.name);
            Entity gameplayEntity = FindNamedRenderEntity(
                entityManager,
                gameplay.name);

            Assert.That(
                HasBakingOnlyEntity(entityManager, eligibleEntity),
                Is.True,
                "The exact eligible logical/source row must be baking-only.");
            Assert.That(
                HasBakingOnlyEntity(entityManager, residentEntity),
                Is.False,
                "An unmatched named resident exception must remain packed.");
            Assert.That(
                HasBakingOnlyEntity(entityManager, gameplayEntity),
                Is.False,
                "A canonical gameplay entity must never be removed by VRP-034.");
            Assert.That(
                entityManager.HasComponent<OperationMapEntityPresentationIdentity>(
                    gameplayEntity),
                Is.True);

            using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadOnly<
                    OperationMapRenderPackedReadinessComponent>());
            Assert.That(databaseQuery.CalculateEntityCount(), Is.EqualTo(1));
            Entity databaseEntity = databaseQuery.GetSingletonEntity();
            OperationMapRenderDatabaseComponent database =
                entityManager.GetComponentData<
                    OperationMapRenderDatabaseComponent>(databaseEntity);
            Assert.That(database.Blob.IsCreated, Is.True);
            Assert.That(
                database.Blob.Value.OperationMapId.ToString(),
                Is.EqualTo("opmap.skirmish.virtualized"));
            Assert.That(database.Blob.Value.Prototypes.Length, Is.EqualTo(1));
            Assert.That(database.Blob.Value.Parts.Length, Is.EqualTo(1));
            Assert.That(database.Blob.Value.Placements.Length, Is.EqualTo(1));
            OperationMapRenderPackedReadinessComponent packedReadiness =
                entityManager.GetComponentData<
                    OperationMapRenderPackedReadinessComponent>(databaseEntity);
            Assert.That(packedReadiness.ResidencyMode, Is.EqualTo(1));
            Assert.That(packedReadiness.EligibleSourceRowCount, Is.EqualTo(1));
            Assert.That(packedReadiness.ResidentSourceRowCount, Is.EqualTo(2));
            Assert.That(packedReadiness.ProxySlotCount, Is.EqualTo(12));
            Assert.That(
                packedReadiness.VirtualizedGeneratedRenderOnlyIdentityCount,
                Is.EqualTo(1));
            Assert.That(
                entityManager.GetBuffer<
                    OperationMapRenderResidentSourceRowComponent>(
                    databaseEntity,
                    true).Length,
                Is.EqualTo(2));
        }
        finally
        {
            world.Dispose();
            blobAssetStore?.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    [Test]
    public void VirtualizedBuilding_ReplacesOnlyRenderRootOwnershipWithStateIndex()
    {
        const string buildingStableId =
            "densecity.cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                "densegenerated|" + buildingStableId,
                out OperationMapRenderIdentity128 ownerIdentity,
                out string ownerError),
            Is.True,
            ownerError);
        Assert.That(
            OperationMapRenderIdentityProjection.TryProject(
                "renderer-path|<owner>",
                out OperationMapRenderIdentity128 pathIdentity,
                out string pathError),
            Is.True,
            pathError);

        OperationMapRenderDatabaseBakeConfig config =
            CreateValidBakeConfig(out Mesh mesh, out Material material);
        mesh.vertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateBounds();
        Set(
            config,
            "prototypes",
            new[]
            {
                new OperationMapRenderPrototypeConfigRecord(
                    1ul,
                    2ul,
                    0,
                    1,
                    mesh.bounds,
                    DenseCityPresentationSemanticCategory.GameplayBuildingIntact,
                    OperationMapRenderEligibilityFlags.Eligible |
                    OperationMapRenderEligibilityFlags.RequiresStateOwner)
            });
        Set(
            config,
            "parts",
            new[]
            {
                new OperationMapRenderPrototypePartConfigRecord(
                    pathIdentity.Low,
                    pathIdentity.High,
                    0,
                    0,
                    0,
                    Matrix4x4.identity,
                    mesh.bounds,
                    Color.white,
                    OperationMapRenderPolicyBucket.OpaqueShadowsOff,
                    0,
                    OperationMapRenderLodFlags.Lod0,
                    OperationMapRenderShadowFlags.None)
            });
        Set(
            config,
            "placements",
            new[]
            {
                new OperationMapRenderPlacementConfigRecord(
                    ownerIdentity.Low,
                    ownerIdentity.High,
                    0,
                    Matrix4x4.identity,
                    0,
                    7,
                    OperationMapRenderVisualState.Intact,
                    0,
                    DenseCityPresentationSemanticCategory.GameplayBuildingIntact)
            });
        Assert.That(config.TryValidateSchema(out string configError), Is.True, configError);

        GameObject root = new("VRP035 Virtualized Presentation Bake Root");
        GameObject sources = new("VRP035 Source Presentation");
        sources.transform.SetParent(root.transform, false);
        GameObject buildingOwner = new("VRP035 Canonical Building");
        buildingOwner.transform.SetParent(sources.transform, false);
        GameObject intact = CreateRenderOwner(
            "VRP035 Intact Visual",
            buildingOwner.transform,
            mesh,
            material);
        GameObject destroyed = new("VRP035 Destroyed Visual");
        destroyed.transform.SetParent(buildingOwner.transform, false);
        intact.AddComponent<DenseCityPresentationIdentityAuthoring>()
            .ConfigureForEditor(
                buildingStableId,
                OperationMapEntityPresentationRole.GameplayBuildings,
                DenseCityPresentationSemanticCategory.GameplayBuildingIntact);
        BuildingDefinitionAuthoring definition =
            buildingOwner.AddComponent<BuildingDefinitionAuthoring>();
        buildingOwner.AddComponent<OperationMapBuildingAuthoring>()
            .ConfigureGeneratedForEditor(
                "opmap.skirmish.virtualized",
                buildingStableId,
                3,
                2,
                new Vector2Int(4, 6),
                new Vector2Int(2, 2),
                300,
                definition,
                intact,
                destroyed);
        OperationMapVirtualizedPresentationAuthoring virtualization =
            root.AddComponent<OperationMapVirtualizedPresentationAuthoring>();
        Set(virtualization, "databaseConfig", config);
        Set(virtualization, "sourcePresentationRoot", sources);

        World world = new("VRP035BuildingOwnershipBake");
        IDisposable blobAssetStore = null;
        try
        {
            BakeGameObjects(world, root, out blobAssetStore);

            EntityManager entityManager = world.EntityManager;
            Entity buildingEntity = FindNamedEntityWithComponent<
                OperationMapBuildingComponent>(
                entityManager,
                buildingOwner.name);
            Entity intactEntity = FindNamedRenderEntity(
                entityManager,
                intact.name);

            Assert.That(HasBakingOnlyEntity(entityManager, intactEntity), Is.True);
            Assert.That(HasBakingOnlyEntity(entityManager, buildingEntity), Is.False);
            Assert.That(
                entityManager.HasComponent<OperationMapBuildingPresentation>(
                    buildingEntity),
                Is.False);
            Assert.That(
                entityManager.HasComponent<
                    OperationMapVirtualizedBuildingPresentationComponent>(
                    buildingEntity),
                Is.True);
            Assert.That(
                entityManager.GetComponentData<
                    OperationMapVirtualizedBuildingPresentationComponent>(
                    buildingEntity).StateOwnerIndex,
                Is.EqualTo(7));
            Assert.That(
                entityManager.HasComponent<OperationMapBuildingDestroyedComponent>(
                    buildingEntity),
                Is.True);
        }
        finally
        {
            world.Dispose();
            blobAssetStore?.Dispose();
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(config);
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh);
        }
    }

    private static GameObject CreateRenderOwner(
        string name,
        Transform parent,
        Mesh mesh,
        Material material)
    {
        GameObject owner = new(name);
        owner.transform.SetParent(parent, false);
        owner.AddComponent<MeshFilter>().sharedMesh = mesh;
        owner.AddComponent<MeshRenderer>().sharedMaterial = material;
        return owner;
    }

    private static Entity FindNamedRenderEntity(
        EntityManager entityManager,
        string name)
    {
        using NativeArray<Entity> entities =
            entityManager.GetAllEntities(Allocator.Temp);
        for (int index = 0; index < entities.Length; index++)
        {
            Entity entity = entities[index];
            if (entityManager.GetName(entity) == name &&
                entityManager.HasComponent<MaterialMeshInfo>(entity))
            {
                return entity;
            }
        }

        Assert.Fail($"No converted render entity named '{name}' was found.");
        return Entity.Null;
    }

    private static Entity FindNamedEntityWithComponent<T>(
        EntityManager entityManager,
        string name)
        where T : unmanaged, IComponentData
    {
        using NativeArray<Entity> entities =
            entityManager.GetAllEntities(Allocator.Temp);
        for (int index = 0; index < entities.Length; index++)
        {
            Entity entity = entities[index];
            if (entityManager.GetName(entity) == name &&
                entityManager.HasComponent<T>(entity))
            {
                return entity;
            }
        }

        Assert.Fail(
            $"No converted entity named '{name}' with {typeof(T).Name} was found.");
        return Entity.Null;
    }

    private static bool HasBakingOnlyEntity(
        EntityManager entityManager,
        Entity entity)
    {
        Type bakingOnlyType =
            Type.GetType("Unity.Entities.BakingOnlyEntity, Unity.Entities.Hybrid", true);
        TypeIndex typeIndex = TypeManager.GetTypeIndex(bakingOnlyType);
        return entityManager.HasComponent(
            entity,
            ComponentType.FromTypeIndex(typeIndex));
    }

    private static void BakeGameObjects(
        World world,
        GameObject root,
        out IDisposable blobAssetStoreLifetime)
    {
        Type bakingUtilityType = Type.GetType("Unity.Entities.BakingUtility, Unity.Entities.Hybrid", true);
        Type bakingSettingsType = Type.GetType("Unity.Entities.BakingSettings, Unity.Entities.Hybrid", true);
        Type blobAssetStoreType =
            Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities") ??
            Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities.Hybrid");
        Assert.That(blobAssetStoreType, Is.Not.Null);

        object blobAssetStore = Activator.CreateInstance(blobAssetStoreType, 128);
        blobAssetStoreLifetime = blobAssetStore as IDisposable;
        try
        {
            object settings = Activator.CreateInstance(bakingSettingsType);
            object assignName = Enum.Parse(bakingUtilityType.GetNestedType("BakingFlags"), "AssignName");
            bakingSettingsType.GetField("BakingFlags")?.SetValue(settings, assignName);
            bakingSettingsType.GetProperty("BlobAssetStore")?.SetValue(settings, blobAssetStore);

            MethodInfo bake = bakingUtilityType.GetMethod(
                "BakeGameObjects",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(bake, Is.Not.Null);
            bake.Invoke(null, new object[] { world, new[] { root }, settings });
        }
        catch
        {
            blobAssetStoreLifetime?.Dispose();
            blobAssetStoreLifetime = null;
            throw;
        }
    }

    private static MotionVectorGenerationMode ToUnityMotionMode(
        OperationMapRenderMotionVectorMode source)
    {
        return source switch
        {
            OperationMapRenderMotionVectorMode.Camera => MotionVectorGenerationMode.Camera,
            OperationMapRenderMotionVectorMode.Object => MotionVectorGenerationMode.Object,
            OperationMapRenderMotionVectorMode.ForceNoMotion =>
                MotionVectorGenerationMode.ForceNoMotion,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }

    private static bool HasShadowFlag(
        OperationMapRenderShadowFlags source,
        OperationMapRenderShadowFlags flag)
    {
        return (source & flag) != 0;
    }

    private static OperationMapRenderDatabaseBakeConfig CreateValidBakeConfig(
        out Mesh mesh,
        out Material material)
    {
        mesh = new Mesh { name = "VRP020Mesh" };
        Shader shader = Shader.Find("Hidden/InternalErrorShader");
        Assert.That(shader, Is.Not.Null);
        material = new Material(shader) { name = "VRP020Material" };

        OperationMapRenderDatabaseBakeConfig config =
            ScriptableObject.CreateInstance<OperationMapRenderDatabaseBakeConfig>();
        Set(config, "schemaVersion", 1);
        Set(config, "operationMapId", "opmap.skirmish.virtualized");
        Set(config, "contentHash", new string('a', 64));
        Set(config, "cellSize", 32f);
        Set(config, "gridOrigin", Vector3.zero);
        Set(config, "gridDimensions", new Vector2Int(1, 1));
        Set(
            config,
            "meshes",
            new[]
            {
                new OperationMapRenderMeshConfigRecord(
                    new string('a', 32),
                    1,
                    mesh)
            });
        Set(
            config,
            "materials",
            new[]
            {
                new OperationMapRenderMaterialConfigRecord(
                    new string('b', 32),
                    2,
                    material)
            });
        Set(
            config,
            "prototypes",
            new[]
            {
                new OperationMapRenderPrototypeConfigRecord(
                    1ul,
                    2ul,
                    0,
                    1,
                    new UnityEngine.Bounds(Vector3.zero, Vector3.one * 2f),
                    DenseCityPresentationSemanticCategory.Vegetation,
                    OperationMapRenderEligibilityFlags.Eligible)
            });
        Set(
            config,
            "parts",
            new[]
            {
                new OperationMapRenderPrototypePartConfigRecord(
                    3ul,
                    4ul,
                    0,
                    0,
                    0,
                    Matrix4x4.identity,
                    new UnityEngine.Bounds(Vector3.zero, Vector3.one),
                    Color.white,
                    OperationMapRenderPolicyBucket.OpaqueShadowsOff,
                    0,
                    OperationMapRenderLodFlags.Lod0,
                    OperationMapRenderShadowFlags.None)
            });
        Set(
            config,
            "placements",
            new[]
            {
                new OperationMapRenderPlacementConfigRecord(
                    5ul,
                    6ul,
                    0,
                    Matrix4x4.identity,
                    0,
                    -1,
                    OperationMapRenderVisualState.Any,
                    0,
                    DenseCityPresentationSemanticCategory.Vegetation)
            });
        Set(
            config,
            "cells",
            new[]
            {
                new OperationMapRenderCellConfigRecord(
                    Vector2Int.zero,
                    new UnityEngine.Bounds(
                        new Vector3(16f, 0f, 16f),
                        new Vector3(32f, 1f, 32f)),
                    0,
                    1)
            });
        Set(config, "cellPlacementIndices", new[] { 0 });
        Set(
            config,
            "poolBuckets",
            new[]
            {
                new OperationMapRenderPoolBucketConfigRecord(
                    OperationMapRenderPolicyBucket.OpaqueShadowsOff,
                    0,
                    1u,
                    OperationMapRenderMotionVectorMode.ForceNoMotion,
                    OperationMapRenderShadowFlags.None,
                    0,
                    12,
                    10,
                    2,
                    7ul,
                    8ul)
            });
        return config;
    }

    private static OperationMapRenderVirtualizationReportDocument CreateValidReport()
    {
        OperationMapRenderPolicyKey opaque =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.Opaque,
                OperationMapRenderShadowFlags.None);
        OperationMapRenderPolicyKey alpha =
            ClassifyPolicy(
                OperationMapRenderMaterialSurface.AlphaClipped,
                OperationMapRenderShadowFlags.None);
        return new OperationMapRenderVirtualizationReportDocument
        {
            SchemaVersion =
                OperationMapRenderVirtualizationReportSerializer.ReportSchemaVersion,
            OperationMapId = "opmap.skirmish.virtualized",
            ContentHash = new string('a', 64),
            ResidencyMode = OperationMapRenderResidencyMode.VirtualizedProxyPool,
            ResidentRenderRows = 4,
            VirtualizedLogicalRows = 20,
            PrototypeCount = 2,
            PartCount = 5,
            PlacementCount = 20,
            CellCount = 8,
            PolicyBucketCount = 2,
            TotalSlotCount = 18,
            PackedDatabaseBytes = 4096,
            SourceRowsRemoved = 20,
            ExcludedRowCount = 4,
            SourceHierarchyObjectCount = 24,
            CapacityByPolicy = new[]
            {
                new OperationMapRenderCapacitySweepResult(
                    opaque,
                    4,
                    10,
                    12,
                    2),
                new OperationMapRenderCapacitySweepResult(
                    alpha,
                    4,
                    5,
                    6,
                    1)
            }
        };
    }

    private static string SerializeValidReport()
    {
        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TrySerialize(
                CreateValidReport(),
                out string json,
                out string error),
            Is.True,
            error);
        return json;
    }

    private static void AssertReportSerializeRejected(
        OperationMapRenderVirtualizationReportDocument report,
        string expectedError)
    {
        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TrySerialize(
                report,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain(expectedError).IgnoreCase);
    }

    private static void AssertReportDeserializeRejected(
        string json,
        string expectedError)
    {
        Assert.That(
            OperationMapRenderVirtualizationReportSerializer.TryDeserialize(
                json,
                out _,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain(expectedError).IgnoreCase);
    }

    private static string RemoveJsonPropertyLine(string json, string propertyName)
    {
        string token = $"\"{propertyName}\"";
        int tokenIndex = json.IndexOf(token, StringComparison.Ordinal);
        Assert.That(tokenIndex, Is.GreaterThanOrEqualTo(0));
        int lineStart = json.LastIndexOf('\n', tokenIndex);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        int lineEnd = json.IndexOf('\n', tokenIndex);
        lineEnd = lineEnd < 0 ? json.Length : lineEnd + 1;
        return json.Remove(lineStart, lineEnd - lineStart);
    }

    private static OperationMapRenderPolicyKey ClassifyPolicy(
        OperationMapRenderMaterialSurface surface,
        OperationMapRenderShadowFlags shadowFlags)
    {
        OperationMapRenderPolicyClassificationInput input =
            PolicyInput(surface, shadowFlags: shadowFlags);
        Assert.That(
            OperationMapRenderPolicyClassifier.TryClassify(
                input,
                out OperationMapRenderPolicyKey policy,
                out string error),
            Is.True,
            error);
        return policy;
    }

    private static void AssertCapacitySweepRejected(
        IReadOnlyList<OperationMapRenderCapacitySweepInput> inputs,
        string expectedError)
    {
        Assert.That(
            OperationMapRenderCapacitySweep.TryCalculate(inputs, out _, out string error),
            Is.False);
        Assert.That(error, Does.Contain(expectedError));
    }

    private static void AssertPolicyBucket(
        OperationMapRenderMaterialSurface surface,
        OperationMapRenderShadowFlags shadowFlags,
        OperationMapRenderPolicyBucket expected)
    {
        OperationMapRenderPolicyClassificationInput input =
            PolicyInput(surface, shadowFlags: shadowFlags);
        Assert.That(
            OperationMapRenderPolicyClassifier.TryClassify(
                input,
                out OperationMapRenderPolicyKey policy,
                out string error),
            Is.True,
            error);
        Assert.That(policy.Bucket, Is.EqualTo(expected));
    }

    private static void AssertPolicyDiffers(
        OperationMapRenderPolicyKey baseline,
        OperationMapRenderPolicyClassificationInput input)
    {
        Assert.That(
            OperationMapRenderPolicyClassifier.TryClassify(
                input,
                out OperationMapRenderPolicyKey policy,
                out string error),
            Is.True,
            error);
        Assert.That(policy, Is.Not.EqualTo(baseline));
    }

    private static void AssertPolicyRejected(
        OperationMapRenderPolicyClassificationInput input,
        string expectedError)
    {
        Assert.That(
            OperationMapRenderPolicyClassifier.TryClassify(input, out _, out string error),
            Is.False);
        Assert.That(error, Does.Contain(expectedError));
    }

    private static OperationMapRenderPolicyClassificationInput PolicyInput(
        OperationMapRenderMaterialSurface surface,
        int layer = 0,
        uint renderingLayerMask = 1u,
        OperationMapRenderMotionVectorMode motionVectorMode =
            OperationMapRenderMotionVectorMode.ForceNoMotion,
        OperationMapRenderShadowFlags shadowFlags = OperationMapRenderShadowFlags.None,
        bool alwaysResidentException = false)
    {
        return new OperationMapRenderPolicyClassificationInput(
            surface,
            layer,
            renderingLayerMask,
            motionVectorMode,
            shadowFlags,
            alwaysResidentException);
    }

    private static OperationMapRenderPrototypeFingerprintInput CreatePrototypeFingerprintInput()
    {
        return new OperationMapRenderPrototypeFingerprintInput
        {
            RendererPath = "Root/Renderer",
            MeshAssetGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            MeshLocalId = 100,
            MaterialAssetGuid = "cccccccccccccccccccccccccccccccc",
            MaterialLocalId = 200,
            SubMeshIndex = 1,
            LocalToPlacement = float4x4.TRS(
                new float3(1f, 2f, 3f),
                quaternion.RotateY(0.5f),
                new float3(1f, 2f, 1f)),
            LocalBounds = Bounds(new float3(0f, 1f, 0f), new float3(2f, 3f, 4f)),
            LinearBaseColor = new float4(0.25f, 0.5f, 0.75f, 1f),
            PolicyBucket = OperationMapRenderPolicyBucket.AlphaClippedShadowsOn,
            Layer = 2,
            RenderingLayerMask = 4u,
            MotionVectorMode = OperationMapRenderMotionVectorMode.ForceNoMotion,
            ShadowFlags = OperationMapRenderShadowFlags.CastShadows |
                          OperationMapRenderShadowFlags.ReceiveShadows,
            LodFlags = OperationMapRenderLodFlags.Lod0
        };
    }

    private static OperationMapRenderIdentity128 Fingerprint(
        OperationMapRenderPrototypeFingerprintInput input)
    {
        Assert.That(
            OperationMapRenderPrototypeFingerprint.TryCompute(
                in input,
                out OperationMapRenderIdentity128 fingerprint,
                out string error),
            Is.True,
            error);
        return fingerprint;
    }

    private static void AssertUnique(
        HashSet<string> fingerprints,
        OperationMapRenderPrototypeFingerprintInput input)
    {
        Assert.That(fingerprints.Add(IdentityKey(Fingerprint(input))), Is.True);
    }

    private static string IdentityKey(OperationMapRenderIdentity128 identity)
    {
        return $"{identity.Low:x16}{identity.High:x16}";
    }

    private static void AssertFingerprintRejected(
        OperationMapRenderPrototypeFingerprintInput input,
        string expectedError)
    {
        Assert.That(
            OperationMapRenderPrototypeFingerprint.TryCompute(
                in input,
                out OperationMapRenderIdentity128 fingerprint,
                out string error),
            Is.False);
        Assert.That(fingerprint.Low, Is.Zero);
        Assert.That(fingerprint.High, Is.Zero);
        Assert.That(error, Does.Contain(expectedError));
    }

    private static OperationMapRenderBoundsBlob Bounds(float3 center, float3 extents)
    {
        return new OperationMapRenderBoundsBlob
        {
            Center = center,
            Extents = extents
        };
    }

    private static OperationMapRenderIdentity128 Identity(ulong low, ulong high)
    {
        return new OperationMapRenderIdentity128
        {
            Low = low,
            High = high
        };
    }

    private static void AssertComponent<T>() where T : unmanaged, IComponentData
    {
        Assert.That(typeof(IComponentData).IsAssignableFrom(typeof(T)), Is.True, typeof(T).Name);
    }

    private static void AssertBuffer<T>() where T : unmanaged, IBufferElementData
    {
        Assert.That(typeof(IBufferElementData).IsAssignableFrom(typeof(T)), Is.True, typeof(T).Name);
    }

    private static void AssertUnmanaged<T>() where T : unmanaged
    {
        Assert.That(UnsafeUtility.IsUnmanaged<T>(), Is.True, typeof(T).Name);
    }

    private static OperationMapDefinition CreateDefinitionWithRequiredLocalReferences()
    {
        OperationMapDefinition definition = ScriptableObject.CreateInstance<OperationMapDefinition>();
        Set(definition, "sourceSceneReference", CreateReference("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        Set(definition, "mapSurfaceDataReference", CreateReference("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"));
        Set(definition, "minimapRasterReference", CreateReference("cccccccccccccccccccccccccccccccc"));
        Set(
            definition,
            "staticPresentationManifestReference",
            CreateReference("dddddddddddddddddddddddddddddddd"));
        Set(definition, "buildingPlacementsReference", CreateReference("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"));
        Set(definition, "vehiclePlacementsReference", CreateReference("ffffffffffffffffffffffffffffffff"));
        return definition;
    }

    private static AssetReference CreateReference(string guid)
    {
        return new AssetReference(guid);
    }

    private static void Set<T>(object target, string fieldName, T value)
    {
        Assert.That(target, Is.Not.Null);
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }
}
