using System;
using Game.Components;
using Game.Configs;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapRenderVirtualizationInitializationSystemTests
{
    private const string OperationMapId = "dense-city";
    private const string ContentHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private World _world;
    private BlobAssetReference<OperationMapRenderDatabaseBlob> _blob;
    private SystemHandle _system;
    private Entity _activeMapEntity;
    private Entity _databaseEntity;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests =
                new OperationMapRenderVirtualizationInitializationSystemTests();
            RunCase(tests, nameof(ValidDatabase_InitializesExactlyOnce),
                test => test.ValidDatabase_InitializesExactlyOnce());
            RunCase(tests, nameof(MapGenerationChange_ReinitializesBoundaryState),
                test => test.MapGenerationChange_ReinitializesBoundaryState());
            RunCase(tests, nameof(NativeState_BuildsExactUnboundMaps),
                test => test.NativeState_BuildsExactUnboundMaps());
            RunCase(tests, nameof(OperationMapMismatch_FailsClosed),
                test => test.OperationMapMismatch_FailsClosed());
            RunCase(tests, nameof(DuplicateSlotIdentity_FailsClosed),
                test => test.DuplicateSlotIdentity_FailsClosed());
            RunCase(tests, nameof(MissingStateOwner_FailsClosed),
                test => test.MissingStateOwner_FailsClosed());
            Debug.Log(
                "[OperationMapRenderVirtualizationInitializationFocusedValidation] " +
                "result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderVirtualizationInitializationFocusedValidation] " +
                "result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(
        OperationMapRenderVirtualizationInitializationSystemTests tests,
        string name,
        Action<OperationMapRenderVirtualizationInitializationSystemTests> action)
    {
        tests.SetUp();
        try
        {
            action(tests);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[OperationMapRenderVirtualizationInitializationFocusedValidation] " +
                $"result=Failed test={name} error={exception}");
            throw;
        }
        finally
        {
            tests.TearDown();
        }
    }

    [SetUp]
    public void SetUp()
    {
        _world = new World(
            nameof(OperationMapRenderVirtualizationInitializationSystemTests));
        _system =
            _world.CreateSystem<
                OperationMapRenderVirtualizationInitializationSystem>();
        _activeMapEntity =
            _world.EntityManager.CreateEntity(typeof(ActiveOperationMapComponent));
        _world.EntityManager.SetComponentData(
            _activeMapEntity,
            new ActiveOperationMapComponent
            {
                OperationMapId = new FixedString64Bytes(OperationMapId),
                Generation = 7
            });
        CreateDatabaseAndSlots();
    }

    [TearDown]
    public void TearDown()
    {
        if (_world != null && _world.IsCreated)
        {
            _world.DestroySystem(_system);
            _world.Dispose();
        }
        if (_blob.IsCreated)
            _blob.Dispose();
    }

    [Test]
    public void ValidDatabase_InitializesExactlyOnce()
    {
        _system.Update(_world.Unmanaged);

        OperationMapRenderVirtualizationStateComponent initialized =
            _world.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(_databaseEntity);
        OperationMapRenderVirtualizationMetricsComponent metrics =
            _world.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationMetricsComponent>(_databaseEntity);
        OperationMapRenderDatabaseComponent database =
            _world.EntityManager.GetComponentData<
                OperationMapRenderDatabaseComponent>(_databaseEntity);
        Assert.That(initialized.Initialized, Is.EqualTo(1));
        Assert.That(initialized.InitialViewApplied, Is.Zero);
        Assert.That(metrics.Capacity, Is.EqualTo(2));
        Assert.That(
            metrics.RebuildReason,
            Is.EqualTo(OperationMapRenderRebuildReason.InitialView));
        Assert.That(database.MapGeneration, Is.EqualTo(7));

        initialized.RebuildCount = 4;
        _world.EntityManager.SetComponentData(_databaseEntity, initialized);
        _system.Update(_world.Unmanaged);

        OperationMapRenderVirtualizationStateComponent unchanged =
            _world.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(_databaseEntity);
        Assert.That(unchanged.RebuildCount, Is.EqualTo(4));
    }

    [Test]
    public void MapGenerationChange_ReinitializesBoundaryState()
    {
        _system.Update(_world.Unmanaged);
        OperationMapRenderVirtualizationStateComponent changed =
            _world.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(_databaseEntity);
        changed.RebuildCount = 4;
        _world.EntityManager.SetComponentData(_databaseEntity, changed);

        ActiveOperationMapComponent active =
            _world.EntityManager.GetComponentData<ActiveOperationMapComponent>(
                _activeMapEntity);
        active.Generation = 8;
        _world.EntityManager.SetComponentData(_activeMapEntity, active);
        _system.Update(_world.Unmanaged);

        OperationMapRenderVirtualizationStateComponent reinitialized =
            _world.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(_databaseEntity);
        OperationMapRenderVirtualizationMetricsComponent metrics =
            _world.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationMetricsComponent>(_databaseEntity);
        OperationMapRenderDatabaseComponent database =
            _world.EntityManager.GetComponentData<
                OperationMapRenderDatabaseComponent>(_databaseEntity);
        Assert.That(reinitialized.Initialized, Is.EqualTo(1));
        Assert.That(reinitialized.RebuildCount, Is.Zero);
        Assert.That(database.MapGeneration, Is.EqualTo(8));
        Assert.That(
            metrics.RebuildReason,
            Is.EqualTo(OperationMapRenderRebuildReason.MapGenerationChanged));
    }

    [Test]
    public void NativeState_BuildsExactUnboundMaps()
    {
        using EntityQuery slots = _world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>());
        using NativeArray<OperationMapRenderProxySlotComponent> slotData =
            slots.ToComponentDataArray<OperationMapRenderProxySlotComponent>(
                Allocator.Temp);
        OperationMapRenderPackedReadinessComponent readiness =
            _world.EntityManager.GetComponentData<
                OperationMapRenderPackedReadinessComponent>(_databaseEntity);
        var nativeState = new OperationMapRenderVirtualizationNativeState();
        try
        {
            nativeState.Initialize(_blob, readiness, slotData);

            Assert.That(nativeState.SlotCapacity, Is.EqualTo(2));
            Assert.That(nativeState.LogicalRowCapacity, Is.EqualTo(1));
            Assert.That(nativeState.PlacementCapacity, Is.EqualTo(1));
            Assert.That(nativeState.GetSlotBinding(0), Is.EqualTo(-1));
            Assert.That(nativeState.GetSlotBinding(1), Is.EqualTo(-1));
            Assert.That(nativeState.GetLogicalRowBinding(0), Is.EqualTo(-1));
            Assert.That(nativeState.GetPlacementFirstLogicalRow(0), Is.Zero);
            Assert.That(nativeState.GetPlacementFirstLogicalRow(1), Is.EqualTo(1));
            nativeState.Dispose();
            Assert.That(nativeState.IsCreated, Is.False);
            Assert.That(nativeState.SlotCapacity, Is.Zero);
            Assert.That(nativeState.LogicalRowCapacity, Is.Zero);
            Assert.That(nativeState.PlacementCapacity, Is.Zero);
        }
        finally
        {
            nativeState.Dispose();
        }
    }

    [Test]
    public void OperationMapMismatch_FailsClosed()
    {
        ActiveOperationMapComponent active =
            _world.EntityManager.GetComponentData<ActiveOperationMapComponent>(
                _activeMapEntity);
        active.OperationMapId = new FixedString64Bytes("wrong-map");
        OperationMapRenderDatabaseComponent database =
            _world.EntityManager.GetComponentData<
                OperationMapRenderDatabaseComponent>(_databaseEntity);
        OperationMapRenderPackedReadinessComponent readiness =
            _world.EntityManager.GetComponentData<
                OperationMapRenderPackedReadinessComponent>(_databaseEntity);
        Assert.Throws<InvalidOperationException>(
            () => OperationMapRenderVirtualizationInitializationSystem
                .ValidateDatabaseIdentity(database, readiness, active));
    }

    [Test]
    public void DuplicateSlotIdentity_FailsClosed()
    {
        using EntityQuery slots = _world.EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<OperationMapRenderProxySlotComponent>());
        using NativeArray<Entity> slotEntities =
            slots.ToEntityArray(Allocator.Temp);
        OperationMapRenderProxySlotComponent duplicate =
            _world.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(slotEntities[0]);
        _world.EntityManager.SetComponentData(slotEntities[1], duplicate);
        using NativeArray<OperationMapRenderProxySlotComponent> slotData =
            slots.ToComponentDataArray<OperationMapRenderProxySlotComponent>(
                Allocator.Temp);
        OperationMapRenderPackedReadinessComponent readiness =
            _world.EntityManager.GetComponentData<
                OperationMapRenderPackedReadinessComponent>(_databaseEntity);
        var nativeState = new OperationMapRenderVirtualizationNativeState();
        try
        {
            Assert.Throws<InvalidOperationException>(
                () => nativeState.Initialize(_blob, readiness, slotData));
        }
        finally
        {
            nativeState.Dispose();
        }
    }

    [Test]
    public void MissingStateOwner_FailsClosed()
    {
        Assert.Throws<InvalidOperationException>(
            () => OperationMapRenderVirtualizationInitializationSystem
                .ValidateOwnership(
                    1,
                    0,
                    1,
                    _databaseEntity,
                    Entity.Null));
    }

    private void CreateDatabaseAndSlots()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapRenderDatabaseBlob root =
            ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        root.OperationMapId = new FixedString64Bytes(OperationMapId);
        root.ContentHash = new FixedString128Bytes(ContentHash);
        root.SchemaVersion = 1;
        root.CellSize = 32f;
        root.GridDimensions = new int2(1);

        BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
            builder.Allocate(ref root.Prototypes, 1);
        prototypes[0] = new OperationMapRenderPrototypeBlob
        {
            FirstPart = 0,
            PartCount = 1,
            EligibilityFlags = OperationMapRenderEligibilityFlags.Eligible
        };
        builder.Allocate(ref root.Parts, 1);
        BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
            builder.Allocate(ref root.Placements, 1);
        placements[0] = new OperationMapRenderPlacementBlob
        {
            PrototypeIndex = 0,
            CellIndex = 0,
            StateOwnerIndex = -1
        };
        builder.Allocate(ref root.Cells, 1);
        BlobBuilderArray<int> cellPlacements =
            builder.Allocate(ref root.CellPlacementIndices, 1);
        cellPlacements[0] = 0;
        BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
            builder.Allocate(ref root.PoolBuckets, 1);
        buckets[0] = new OperationMapRenderPoolBucketBlob
        {
            PolicyBucket = OperationMapRenderPolicyBucket.OpaqueShadowsOff,
            FirstSlot = 0,
            Capacity = 2,
            PeakRequiredCount = 1,
            HeadroomCount = 1
        };
        _blob =
            builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(
                Allocator.Persistent);

        _databaseEntity = _world.EntityManager.CreateEntity(
            typeof(OperationMapRenderDatabaseComponent),
            typeof(OperationMapRenderPackedReadinessComponent),
            typeof(OperationMapRenderVirtualizationStateComponent),
            typeof(OperationMapRenderVirtualizationMetricsComponent));
        _world.EntityManager.AddBuffer<OperationMapRenderStateChangeComponent>(
            _databaseEntity);
        _world.EntityManager.SetComponentData(
            _databaseEntity,
            new OperationMapRenderDatabaseComponent
            {
                Blob = _blob,
                ContentHash = new FixedString128Bytes(ContentHash),
                SchemaVersion = 1
            });
        _world.EntityManager.SetComponentData(
            _databaseEntity,
            new OperationMapRenderPackedReadinessComponent
            {
                ResidencyMode =
                    (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool,
                ProxySlotCount = 2
            });

        for (int slotIndex = 0; slotIndex < 2; slotIndex++)
        {
            Entity slot = _world.EntityManager.CreateEntity(
                typeof(OperationMapRenderProxySlotComponent));
            _world.EntityManager.SetComponentData(
                slot,
                new OperationMapRenderProxySlotComponent
                {
                    SlotIndex = slotIndex,
                    PoolBucketIndex = 0,
                    PlacementIndex = -1,
                    PartIndex = -1
                });
        }
    }
}
