using System;
using Game.Components;
using Game.Configs;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class OperationMapRenderStateSyncSystemTests
{
    private const string OperationMapId = "dense-city-state-sync";
    private const string ContentHash =
        "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    public static void RunFocusedValidation()
    {
        try
        {
            var suite = new OperationMapRenderStateSyncSystemTests();
            suite.Update_InitializesExactCanonicalStateFromBuildings();
            suite.Update_ConsumesBoundedEventAndRetainsOffCameraState();
            suite.Update_VisibleStateChangeReassignsOnlyCurrentEnvelope();
            suite.Update_OutOfSequenceEventFailsClosedAndRemainsPending();
            Debug.Log(
                "[OperationMapRenderStateSyncValidation] result=Passed tests=4");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderStateSyncValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Update_InitializesExactCanonicalStateFromBuildings()
    {
        using var fixture = new StateSyncFixture(secondBuildingDestroyed: true);

        fixture.UpdateStateSync();

        DynamicBuffer<OperationMapRenderCanonicalStateComponent> states =
            fixture.EntityManager.GetBuffer<
                OperationMapRenderCanonicalStateComponent>(fixture.DatabaseEntity);
        OperationMapRenderStateSyncStateComponent sync =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderStateSyncStateComponent>(fixture.DatabaseEntity);
        Assert.That(states.Length, Is.EqualTo(2));
        Assert.That(states[0].VisualState,
            Is.EqualTo(OperationMapRenderVisualState.Intact));
        Assert.That(states[1].VisualState,
            Is.EqualTo(OperationMapRenderVisualState.Destroyed));
        Assert.That(sync.Initialized, Is.EqualTo(1));
        Assert.That(sync.StateOwnerCount, Is.EqualTo(2));
        Assert.That(sync.Revision, Is.EqualTo(1));
        Assert.That(sync.LastAppliedChangeVersion, Is.Zero);
    }

    [Test]
    public void Update_ConsumesBoundedEventAndRetainsOffCameraState()
    {
        using var fixture = new StateSyncFixture();
        fixture.UpdateStateSync();
        fixture.AppendChange(0, OperationMapRenderVisualState.Destroyed, 1);

        fixture.UpdateStateSync();

        DynamicBuffer<OperationMapRenderCanonicalStateComponent> states =
            fixture.EntityManager.GetBuffer<
                OperationMapRenderCanonicalStateComponent>(fixture.DatabaseEntity);
        OperationMapRenderStateSyncStateComponent sync =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderStateSyncStateComponent>(fixture.DatabaseEntity);
        OperationMapRenderVirtualizationStateComponent runtime =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(
                    fixture.DatabaseEntity);
        Assert.That(states[0].VisualState,
            Is.EqualTo(OperationMapRenderVisualState.Destroyed));
        Assert.That(states[0].ChangeVersion, Is.EqualTo(1));
        Assert.That(sync.LastAppliedChangeVersion, Is.EqualTo(1));
        Assert.That(sync.DirtyPlacementCount, Is.EqualTo(2));
        Assert.That(sync.DirtyCellCount, Is.EqualTo(1));
        Assert.That(runtime.DirtyPlacementCount, Is.EqualTo(2));
        Assert.That(runtime.ActiveSlotCount, Is.Zero);

        fixture.PlaybackEndSimulation();
        Assert.That(
            fixture.EntityManager.GetBuffer<
                OperationMapRenderStateChangeComponent>(fixture.DatabaseEntity)
                .Length,
            Is.Zero);
        Assert.DoesNotThrow(fixture.UpdateStateSync);
    }

    [Test]
    public void Update_VisibleStateChangeReassignsOnlyCurrentEnvelope()
    {
        using var fixture = new StateSyncFixture();
        fixture.UpdateStateSync();
        fixture.CreateCamera();
        fixture.UpdateVirtualization();
        Assert.That(fixture.AssignedPlacementIndices(),
            Is.EquivalentTo(new[] { 0, 2 }));

        fixture.AppendChange(0, OperationMapRenderVisualState.Destroyed, 1);
        fixture.UpdateStateSync();
        fixture.UpdateVirtualization();

        Assert.That(fixture.AssignedPlacementIndices(),
            Is.EquivalentTo(new[] { 1, 2 }));
        OperationMapRenderVirtualizationMetricsComponent metrics =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationMetricsComponent>(
                    fixture.DatabaseEntity);
        OperationMapRenderVirtualizationStateComponent runtime =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(
                    fixture.DatabaseEntity);
        Assert.That(metrics.RebuildReason,
            Is.EqualTo(OperationMapRenderRebuildReason.VisualStateChanged));
        Assert.That(runtime.RebuildCount, Is.EqualTo(2));
        Assert.That(runtime.DirtyPlacementCount, Is.Zero);
    }

    [Test]
    public void Update_OutOfSequenceEventFailsClosedAndRemainsPending()
    {
        using var fixture = new StateSyncFixture();
        fixture.UpdateStateSync();
        fixture.AppendChange(0, OperationMapRenderVisualState.Destroyed, 2);

        Assert.That(
            fixture.UpdateStateSync,
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains(
                "invalid or out of sequence"));
        Assert.That(
            fixture.EntityManager.GetBuffer<
                OperationMapRenderStateChangeComponent>(fixture.DatabaseEntity)
                .Length,
            Is.EqualTo(1));
        Assert.That(
            fixture.EntityManager.GetBuffer<
                OperationMapRenderCanonicalStateComponent>(fixture.DatabaseEntity)[0]
                .VisualState,
            Is.EqualTo(OperationMapRenderVisualState.Intact));
    }

    private sealed class StateSyncFixture : IDisposable
    {
        private readonly World _world;
        private readonly SystemHandle _stateSyncSystem;
        private readonly SystemHandle _virtualizationSystem;
        private BlobAssetReference<OperationMapRenderDatabaseBlob> _blob;

        internal EntityManager EntityManager => _world.EntityManager;
        internal Entity DatabaseEntity { get; }

        internal StateSyncFixture(bool secondBuildingDestroyed = false)
        {
            _world = new World("OperationMapRenderStateSyncTests");
            _stateSyncSystem =
                _world.CreateSystem<OperationMapRenderStateSyncSystem>();
            _virtualizationSystem = _world.CreateSystem<
                OperationMapRenderVirtualizationInitializationSystem>();
            _blob = CreateDatabaseBlob();

            Entity activeMap = EntityManager.CreateEntity(
                typeof(ActiveOperationMapComponent));
            EntityManager.SetComponentData(activeMap, new ActiveOperationMapComponent
            {
                OperationMapId = new FixedString64Bytes(OperationMapId),
                Generation = 1
            });
            DatabaseEntity = CreateDatabaseOwner();
            CreateBuilding(0, false);
            CreateBuilding(1, secondBuildingDestroyed);
            CreateSlots();
        }

        internal void UpdateStateSync()
        {
            _stateSyncSystem.Update(_world.Unmanaged);
            EntityManager.CompleteAllTrackedJobs();
        }

        internal void UpdateVirtualization()
        {
            _virtualizationSystem.Update(_world.Unmanaged);
            EntityManager.CompleteAllTrackedJobs();
        }

        internal void PlaybackEndSimulation()
        {
            EntityManager.CompleteAllTrackedJobs();
        }

        internal void AppendChange(
            int stateOwnerIndex,
            OperationMapRenderVisualState visualState,
            uint version)
        {
            EntityManager.GetBuffer<OperationMapRenderStateChangeComponent>(
                DatabaseEntity).Add(new OperationMapRenderStateChangeComponent
                {
                    StateOwnerIndex = stateOwnerIndex,
                    VisualState = visualState,
                    ChangeVersion = version
                });
            EntityManager.SetComponentData(
                DatabaseEntity,
                new OperationMapRenderStateChangeSequenceComponent
                {
                    LastPublishedVersion = version
                });
        }

        internal void CreateCamera()
        {
            Entity camera = EntityManager.CreateEntity(
                typeof(RuntimeCameraSnapshotComponent));
            var cameraObject = new GameObject("VRP-062 focused camera");
            try
            {
                Camera unityCamera = cameraObject.AddComponent<Camera>();
                unityCamera.transform.position = new Vector3(1f, 20f, 1f);
                unityCamera.transform.LookAt(Vector3.zero, Vector3.up);
                unityCamera.fieldOfView = 60f;
                unityCamera.aspect = 1f;
                unityCamera.nearClipPlane = 0.1f;
                unityCamera.farClipPlane = 100f;
                Matrix4x4 worldToCamera = unityCamera.worldToCameraMatrix;
                Matrix4x4 projection = unityCamera.projectionMatrix;
                EntityManager.SetComponentData(camera,
                    new RuntimeCameraSnapshotComponent
                    {
                        IsValid = 1,
                        PublicationVersion = 1,
                        Position = unityCamera.transform.position,
                        Rotation = unityCamera.transform.rotation,
                        WorldToCamera = ToFloat4x4(worldToCamera),
                        Projection = ToFloat4x4(projection),
                        ViewProjection =
                            ToFloat4x4(projection * worldToCamera)
                    });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static float4x4 ToFloat4x4(Matrix4x4 matrix) => new(
            new float4(matrix.m00, matrix.m10, matrix.m20, matrix.m30),
            new float4(matrix.m01, matrix.m11, matrix.m21, matrix.m31),
            new float4(matrix.m02, matrix.m12, matrix.m22, matrix.m32),
            new float4(matrix.m03, matrix.m13, matrix.m23, matrix.m33));

        internal int[] AssignedPlacementIndices()
        {
            DynamicBuffer<OperationMapRenderSlotCommandComponent> commands =
                EntityManager.GetBuffer<OperationMapRenderSlotCommandComponent>(
                    DatabaseEntity);
            var result = new System.Collections.Generic.List<int>();
            for (int index = 0; index < commands.Length; index++)
            {
                if (commands[index].Assigned != 0)
                    result.Add(commands[index].PlacementIndex);
            }
            result.Sort();
            return result.ToArray();
        }

        public void Dispose()
        {
            if (_world.IsCreated)
            {
                _world.DestroySystem(_virtualizationSystem);
                _world.DestroySystem(_stateSyncSystem);
                _world.Dispose();
            }
            if (_blob.IsCreated)
                _blob.Dispose();
        }

        private Entity CreateDatabaseOwner()
        {
            Entity owner = EntityManager.CreateEntity(
                typeof(OperationMapRenderDatabaseComponent),
                typeof(OperationMapRenderPackedReadinessComponent),
                typeof(OperationMapRenderVirtualizationStateComponent),
                typeof(OperationMapRenderVirtualizationMetricsComponent),
                typeof(OperationMapRenderSlotCommandStateComponent),
                typeof(OperationMapRenderStateChangeSequenceComponent),
                typeof(OperationMapRenderStateSyncStateComponent));
            EntityManager.AddBuffer<OperationMapRenderStateChangeComponent>(owner);
            EntityManager.AddBuffer<OperationMapRenderCanonicalStateComponent>(owner);
            DynamicBuffer<OperationMapRenderSlotCommandComponent> commands =
                EntityManager.AddBuffer<OperationMapRenderSlotCommandComponent>(owner);
            for (int index = 0; index < 2; index++)
            {
                commands.Add(new OperationMapRenderSlotCommandComponent
                {
                    SlotIndex = index,
                    LogicalRowIndex = -1,
                    PlacementIndex = -1,
                    PartIndex = -1,
                    PoolBucketIndex = -1
                });
            }
            EntityManager.SetComponentData(owner,
                new OperationMapRenderDatabaseComponent
                {
                    Blob = _blob,
                    ContentHash = new FixedString128Bytes(ContentHash),
                    SchemaVersion = 1
                });
            EntityManager.SetComponentData(owner,
                new OperationMapRenderPackedReadinessComponent
                {
                    ResidencyMode =
                        (byte)OperationMapRenderResidencyMode.VirtualizedProxyPool,
                    ProxySlotCount = 2
                });
            return owner;
        }

        private void CreateBuilding(int stateOwnerIndex, bool destroyed)
        {
            Entity building = EntityManager.CreateEntity(
                typeof(OperationMapVirtualizedBuildingPresentationComponent),
                typeof(OperationMapBuildingDestroyedComponent));
            EntityManager.SetComponentData(building,
                new OperationMapVirtualizedBuildingPresentationComponent
                {
                    StateOwnerIndex = stateOwnerIndex
                });
            EntityManager.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(
                building,
                destroyed);
        }

        private void CreateSlots()
        {
            for (int index = 0; index < 2; index++)
            {
                Entity slot = EntityManager.CreateEntity(
                    typeof(OperationMapRenderProxySlotComponent));
                EntityManager.SetComponentData(slot,
                    new OperationMapRenderProxySlotComponent
                    {
                        SlotIndex = index,
                        PoolBucketIndex = 0,
                        PlacementIndex = -1,
                        PartIndex = -1
                    });
            }
        }

        private static BlobAssetReference<OperationMapRenderDatabaseBlob>
            CreateDatabaseBlob()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref OperationMapRenderDatabaseBlob root =
                ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
            root.OperationMapId = new FixedString64Bytes(OperationMapId);
            root.ContentHash = new FixedString128Bytes(ContentHash);
            root.SchemaVersion = 1;
            root.CellSize = 32f;
            root.GridOrigin = float3.zero;
            root.GridDimensions = new int2(2, 1);

            BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
                builder.Allocate(ref root.Prototypes, 1);
            prototypes[0] = new OperationMapRenderPrototypeBlob
            {
                FirstPart = 0,
                PartCount = 1,
                EligibilityFlags = OperationMapRenderEligibilityFlags.Eligible |
                                   OperationMapRenderEligibilityFlags.RequiresStateOwner
            };
            BlobBuilderArray<OperationMapRenderPrototypePartBlob> parts =
                builder.Allocate(ref root.Parts, 1);
            parts[0] = new OperationMapRenderPrototypePartBlob
            {
                PoolBucketIndex = 0
            };
            BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
                builder.Allocate(ref root.Placements, 4);
            placements[0] = Placement(0, 0, OperationMapRenderVisualState.Intact);
            placements[1] = Placement(0, 0, OperationMapRenderVisualState.Destroyed);
            placements[2] = Placement(1, 0, OperationMapRenderVisualState.Intact);
            placements[3] = Placement(1, 0, OperationMapRenderVisualState.Destroyed);

            BlobBuilderArray<OperationMapRenderCellBlob> cells =
                builder.Allocate(ref root.Cells, 1);
            cells[0] = new OperationMapRenderCellBlob
            {
                Coordinate = int2.zero,
                WorldBounds = new OperationMapRenderBoundsBlob
                {
                    Center = new float3(16f, 0f, 16f),
                    Extents = new float3(16f, 10f, 16f)
                },
                FirstPlacementIndex = 0,
                PlacementIndexCount = 4
            };
            BlobBuilderArray<int> memberships =
                builder.Allocate(ref root.CellPlacementIndices, 4);
            for (int index = 0; index < 4; index++)
                memberships[index] = index;
            BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
                builder.Allocate(ref root.PoolBuckets, 1);
            buckets[0] = new OperationMapRenderPoolBucketBlob
            {
                PolicyBucket = OperationMapRenderPolicyBucket.OpaqueShadowsOff,
                FirstSlot = 0,
                Capacity = 2,
                PeakRequiredCount = 2,
                HeadroomCount = 0
            };
            return builder.CreateBlobAssetReference<
                OperationMapRenderDatabaseBlob>(Allocator.Persistent);
        }

        private static OperationMapRenderPlacementBlob Placement(
            int stateOwnerIndex,
            int cellIndex,
            OperationMapRenderVisualState visualState) => new()
        {
            PrototypeIndex = 0,
            CellIndex = cellIndex,
            StateOwnerIndex = stateOwnerIndex,
            RequiredVisualState = visualState
        };
    }
}
