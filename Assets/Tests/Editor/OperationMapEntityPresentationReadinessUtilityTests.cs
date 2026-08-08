using System;
using Game.Components;
using Game.Composition;
using Game.Configs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Scenes;

public sealed class OperationMapEntityPresentationReadinessUtilityTests
{
    private const string OperationMapId = "opmap.skirmish.desert_base_01";
    private static readonly FixedString128Bytes MigrationHash = new(new string('a', 64));
    private World world;
    private Entity sceneEntity;
    private Entity sectionEntity;
    private Entity contractEntity;
    private Entity vehicleIdentityEntity;
    private Entity renderOnlyIdentityEntity;
    private Entity virtualizedDatabaseEntity;
    private BlobAssetReference<OperationMapRenderDatabaseBlob> virtualizedBlob;

    public static void RunFocusedValidation()
    {
        var suite = new OperationMapEntityPresentationReadinessUtilityTests();
        Action[] tests =
        {
            suite.TryValidate_AcceptsCompleteEntitySceneWithoutStaticPreload,
            suite.TryValidate_RejectsIncompleteIdentitySet,
            suite.TryValidate_RejectsStaticPresentationPreloadRequirement,
            suite.TryValidate_AcceptsExplicitGeneratedIdentityTotals,
            suite.TryValidate_RejectsDuplicateGeneratedIdentity,
            suite.TryValidate_RejectsGeneratedRoleCategoryMismatch,
            suite.TryValidate_RejectsProtectedOverlapOnNonInfrastructure,
            suite.TryValidate_AcceptsCompleteVirtualizedPackedContract,
            suite.TryValidate_AcceptsRetainedVirtualizedIdentityOverlap,
            suite.TryValidate_RejectsRetainedOverlapLargerThanVirtualizedClass,
            suite.TryValidate_RejectsVirtualizedMissingDatabase,
            suite.TryValidate_RejectsEligibleSourceSurvivor,
            suite.TryValidate_RejectsDuplicateProxySlotIdentity
        };
        foreach (Action test in tests)
        {
            suite.SetUp();
            try
            {
                test();
            }
            finally
            {
                suite.TearDown();
            }
        }
        Debug.Log($"[OperationMapEntityPresentationReadinessValidation] result=Passed tests={tests.Length}");
    }

    [SetUp]
    public void SetUp()
    {
        world = new World("OperationMapEntityPresentationReadinessUtilityTests");
        EntityManager entityManager = world.EntityManager;
        sceneEntity = entityManager.CreateEntity();
        sectionEntity = entityManager.CreateEntity();
        entityManager.AddBuffer<ResolvedSectionEntity>(sceneEntity).Add(
            new ResolvedSectionEntity { SectionEntity = sectionEntity });

        contractEntity = CreateSectionEntity(
            typeof(OperationMapEntityPresentationReadinessContract));
        entityManager.SetComponentData(
            contractEntity,
            new OperationMapEntityPresentationReadinessContract
            {
                OperationMapId = new FixedString128Bytes(OperationMapId),
                MigrationRecordSetHash = MigrationHash,
                ExpectedPresentationRootCount = 3,
                ExpectedGameplayBuildingCount = 1,
                ExpectedGameplayVehicleCount = 1,
                ExpectedRenderOnlyCount = 1,
                ExpectedGeneratedIdentityCount = 0,
                RequiresStaticPresentationPreload = 0
            });

        CreateRoot(1);
        CreateRoot(2);
        CreateRoot(3);
        CreateIdentity(1, "building");
        vehicleIdentityEntity = CreateIdentity(2, "vehicle");
        renderOnlyIdentityEntity = CreateIdentity(3, "render-only");
        CreateSectionEntity(typeof(OperationMapBuildingPresentation));
        CreateSectionEntity(typeof(OperationMapAuthoredVehiclePresentation));
    }

    [TearDown]
    public void TearDown()
    {
        if (world != null && world.IsCreated)
            world.Dispose();
        if (virtualizedBlob.IsCreated)
            virtualizedBlob.Dispose();
    }

    [Test]
    public void TryValidate_AcceptsCompleteEntitySceneWithoutStaticPreload()
    {
        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void TryValidate_RejectsIncompleteIdentitySet()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedRenderOnlyCount = 2;
        world.EntityManager.SetComponentData(contractEntity, contract);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("identity counts"));
    }

    [Test]
    public void TryValidate_RejectsStaticPresentationPreloadRequirement()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.RequiresStaticPresentationPreload = 1;
        world.EntityManager.SetComponentData(contractEntity, contract);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("prohibited static preload"));
    }

    [Test]
    public void TryValidate_AcceptsExplicitGeneratedIdentityTotals()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedGameplayBuildingCount = 2;
        contract.ExpectedRenderOnlyCount = 2;
        contract.ExpectedGeneratedIdentityCount = 2;
        world.EntityManager.SetComponentData(contractEntity, contract);
        CreateGeneratedIdentity(1, "dense-building");
        CreateGeneratedIdentity(3, "dense-render-only");
        CreateSectionEntity(typeof(OperationMapBuildingPresentation));

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void TryValidate_RejectsDuplicateGeneratedIdentity()
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedGeneratedIdentityCount = 2;
        world.EntityManager.SetComponentData(contractEntity, contract);
        CreateGeneratedIdentity(1, "duplicate");
        CreateGeneratedIdentity(3, "duplicate");

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("duplicate generated identity"));
    }

    [Test]
    public void TryValidate_RejectsGeneratedRoleCategoryMismatch()
    {
        ConfigureGeneratedContract(1, 2, 1);
        CreateGeneratedIdentity(
            1,
            "dense-building",
            DenseCityPresentationSemanticCategory.Prop);
        CreateSectionEntity(typeof(OperationMapBuildingPresentation));

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("invalid semantic metadata"));
    }

    [Test]
    public void TryValidate_RejectsProtectedOverlapOnNonInfrastructure()
    {
        ConfigureGeneratedContract(1, 2, 1);
        CreateGeneratedIdentity(
            3,
            "dense-prop",
            DenseCityPresentationSemanticCategory.Prop,
            DenseCityPresentationSemanticFlags.AllowsProtectedOverlap);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("invalid semantic metadata"));
    }

    [Test]
    public void TryValidate_AcceptsCompleteVirtualizedPackedContract()
    {
        ConfigureVirtualizedPackedContract();

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void TryValidate_AcceptsRetainedVirtualizedIdentityOverlap()
    {
        ConfigureVirtualizedPackedContract(retainAcceptedRenderOnlyIdentity: true);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string error),
            Is.True,
            error);
    }

    [Test]
    public void TryValidate_RejectsRetainedOverlapLargerThanVirtualizedClass()
    {
        ConfigureVirtualizedPackedContract(retainAcceptedRenderOnlyIdentity: true);
        OperationMapRenderPackedReadinessComponent readiness =
            world.EntityManager.GetComponentData<
                OperationMapRenderPackedReadinessComponent>(virtualizedDatabaseEntity);
        readiness.RetainedVirtualizedAcceptedRenderOnlyIdentityCount = 2;
        world.EntityManager.SetComponentData(virtualizedDatabaseEntity, readiness);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("readiness metrics are invalid"));
    }

    [Test]
    public void TryValidate_RejectsVirtualizedMissingDatabase()
    {
        ConfigureVirtualizedPackedContract();
        world.EntityManager.DestroyEntity(virtualizedDatabaseEntity);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("exactly one packed render database"));
    }

    [Test]
    public void TryValidate_RejectsEligibleSourceSurvivor()
    {
        ConfigureVirtualizedPackedContract();
        CreateSectionEntity(typeof(OperationMapRenderEligibleSourceComponent));

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("eligible source render rows"));
    }

    [Test]
    public void TryValidate_RejectsDuplicateProxySlotIdentity()
    {
        ConfigureVirtualizedPackedContract(duplicateSlotIndex: true);

        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                world.EntityManager,
                sceneEntity,
                OperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string error),
            Is.False);
        Assert.That(error, Does.Contain("invalid or duplicate slot identity"));
    }

    private void ConfigureGeneratedContract(
        int generatedCount,
        int expectedBuildings,
        int expectedRenderOnly)
    {
        OperationMapEntityPresentationReadinessContract contract =
            world.EntityManager.GetComponentData<
                OperationMapEntityPresentationReadinessContract>(contractEntity);
        contract.ExpectedGeneratedIdentityCount = generatedCount;
        contract.ExpectedGameplayBuildingCount = expectedBuildings;
        contract.ExpectedRenderOnlyCount = expectedRenderOnly;
        world.EntityManager.SetComponentData(contractEntity, contract);
    }

    private Entity CreateSectionEntity(params ComponentType[] componentTypes)
    {
        Entity entity = world.EntityManager.CreateEntity(componentTypes);
        world.EntityManager.AddSharedComponent(
            entity,
            new SceneTag { SceneEntity = sectionEntity });
        return entity;
    }

    private void CreateRoot(byte role)
    {
        Entity entity = CreateSectionEntity(typeof(OperationMapEntityPresentationRoot));
        world.EntityManager.SetComponentData(
            entity,
            new OperationMapEntityPresentationRoot
            {
                OperationMapId = new FixedString128Bytes(OperationMapId),
                Role = role,
                SchemaVersion = 1,
                MigrationRecordSetHash = MigrationHash
            });
    }

    private Entity CreateIdentity(byte role, string sourceId)
    {
        Entity entity = CreateSectionEntity(typeof(OperationMapEntityPresentationIdentity));
        world.EntityManager.SetComponentData(
            entity,
            new OperationMapEntityPresentationIdentity
            {
                OperationMapId = new FixedString128Bytes(OperationMapId),
                SourceGlobalObjectId = new FixedString128Bytes(sourceId),
                Role = role,
                PlacementIndex = role
            });
        return entity;
    }

    private void ConfigureVirtualizedPackedContract(
        bool duplicateSlotIndex = false,
        bool retainAcceptedRenderOnlyIdentity = false)
    {
        if (!retainAcceptedRenderOnlyIdentity)
            world.EntityManager.DestroyEntity(renderOnlyIdentityEntity);
        virtualizedBlob = CreateVirtualizedBlob();
        virtualizedDatabaseEntity = CreateSectionEntity(
            typeof(OperationMapRenderDatabaseComponent),
            typeof(OperationMapRenderPackedReadinessComponent));
        world.EntityManager.SetComponentData(
            virtualizedDatabaseEntity,
            new OperationMapRenderDatabaseComponent
            {
                Blob = virtualizedBlob,
                ContentHash = new FixedString128Bytes(new string('b', 64)),
                SchemaVersion = 1,
                MapGeneration = 0
            });
        world.EntityManager.SetComponentData(
            virtualizedDatabaseEntity,
            new OperationMapRenderPackedReadinessComponent
            {
                ResidencyMode =
                    (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool,
                EligibleSourceRowCount = 1,
                EligibleSourceRendererCount = 1,
                ResidentSourceRowCount = 1,
                ProxySlotCount = 2,
                VirtualizedAcceptedRenderOnlyIdentityCount = 1,
                RetainedVirtualizedAcceptedRenderOnlyIdentityCount =
                    retainAcceptedRenderOnlyIdentity ? 1 : 0
            });
        world.EntityManager.AddBuffer<
            OperationMapRenderResidentSourceRowComponent>(
            virtualizedDatabaseEntity).Add(
            new OperationMapRenderResidentSourceRowComponent
            {
                RenderEntity = vehicleIdentityEntity,
                OwnerIdentity = new OperationMapRenderIdentity128
                {
                    Low = 11,
                    High = 12
                },
                RendererPathIdentity = new OperationMapRenderIdentity128
                {
                    Low = 13,
                    High = 14
                }
            });

        for (int index = 0; index < 2; index++)
        {
            Entity slot =
                CreateSectionEntity(typeof(OperationMapRenderProxySlotComponent));
            world.EntityManager.SetComponentData(
                slot,
                new OperationMapRenderProxySlotComponent
                {
                    SlotIndex = duplicateSlotIndex && index == 1 ? 0 : index,
                    PoolBucketIndex = 0,
                    PlacementIndex = -1,
                    PartIndex = -1,
                    AssignmentGeneration = 0
                });
        }
    }

    private static BlobAssetReference<OperationMapRenderDatabaseBlob>
        CreateVirtualizedBlob()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapRenderDatabaseBlob root =
            ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        root.OperationMapId = new FixedString64Bytes(OperationMapId);
        root.ContentHash = new FixedString128Bytes(new string('b', 64));
        root.SchemaVersion = 1;
        root.CellSize = 32f;
        root.GridDimensions = new Unity.Mathematics.int2(1, 1);
        builder.Allocate(ref root.Prototypes, 1)[0] =
            new OperationMapRenderPrototypeBlob
            {
                ContentIdentity = new OperationMapRenderIdentity128
                {
                    Low = 1,
                    High = 2
                },
                FirstPart = 0,
                PartCount = 1,
                EligibilityFlags = OperationMapRenderEligibilityFlags.Eligible
            };
        builder.Allocate(ref root.Parts, 1)[0] =
            new OperationMapRenderPrototypePartBlob
            {
                RendererPathHash = new OperationMapRenderIdentity128
                {
                    Low = 3,
                    High = 4
                },
                PoolBucketIndex = 0
            };
        builder.Allocate(ref root.Placements, 1)[0] =
            new OperationMapRenderPlacementBlob
            {
                StableIdentityHash = new OperationMapRenderIdentity128
                {
                    Low = 5,
                    High = 6
                },
                PrototypeIndex = 0,
                CellIndex = 0,
                StateOwnerIndex = -1
            };
        builder.Allocate(ref root.Cells, 1)[0] =
            new OperationMapRenderCellBlob
            {
                FirstPlacementIndex = 0,
                PlacementIndexCount = 1
            };
        builder.Allocate(ref root.CellPlacementIndices, 1)[0] = 0;
        builder.Allocate(ref root.PoolBuckets, 1)[0] =
            new OperationMapRenderPoolBucketBlob
            {
                FirstSlot = 0,
                Capacity = 2,
                PeakRequiredCount = 1,
                HeadroomCount = 1,
                ReportIdentity = new OperationMapRenderIdentity128
                {
                    Low = 7,
                    High = 8
                }
            };
        return builder.CreateBlobAssetReference<
            OperationMapRenderDatabaseBlob>(Allocator.Persistent);
    }

    private void CreateGeneratedIdentity(
        byte role,
        string stableId,
        DenseCityPresentationSemanticCategory category =
            DenseCityPresentationSemanticCategory.Unknown,
        DenseCityPresentationSemanticFlags flags = DenseCityPresentationSemanticFlags.None)
    {
        if (category == DenseCityPresentationSemanticCategory.Unknown)
        {
            category = role == 1
                ? DenseCityPresentationSemanticCategory.GameplayBuildingIntact
                : DenseCityPresentationSemanticCategory.Infrastructure;
        }
        Entity entity = CreateSectionEntity(typeof(DenseCityPresentationIdentity));
        world.EntityManager.SetComponentData(
            entity,
            new DenseCityPresentationIdentity
            {
                StableId = new FixedString128Bytes(stableId),
                Role = role,
                Category = (byte)category,
                Flags = (byte)flags
            });
    }
}
