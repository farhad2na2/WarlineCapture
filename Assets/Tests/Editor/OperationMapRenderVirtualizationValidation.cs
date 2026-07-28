using System;
using System.Reflection;
using Game.Configs;
using Game.Components;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
            Debug.Log("[OperationMapRenderVirtualizationValidation] result=Passed tests=11");
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
        root.OperationMapId = new FixedString64Bytes("opmap.test.virtualized");
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
            Assert.That(blob.Value.OperationMapId.ToString(), Is.EqualTo("opmap.test.virtualized"));
            Assert.That(blob.Value.Prototypes[0].PartCount, Is.EqualTo(1));
            Assert.That(blob.Value.Parts[0].MeshArrayIndex, Is.EqualTo(3));
            Assert.That(blob.Value.Placements[0].StateOwnerIndex, Is.EqualTo(-1));
            Assert.That(blob.Value.Cells[0].Coordinate, Is.EqualTo(new int2(2, 3)));
            Assert.That(blob.Value.CellPlacementIndices[0], Is.EqualTo(0));
            Assert.That(blob.Value.PoolBuckets[0].HeadroomCount, Is.EqualTo(20));
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

    private static void Set<T>(OperationMapDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(OperationMapDefinition).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(definition, value);
    }
}
