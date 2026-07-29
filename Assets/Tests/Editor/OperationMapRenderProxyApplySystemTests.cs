using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public sealed class OperationMapRenderProxyApplySystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            OrdersBeforePresentationUpdateAndEntitiesGraphics();
            SchedulesApplyThroughStateDependency();
            UnchangedGenerationLeavesSlotUntouched();
            StableVersion_SchedulesNothingAllocatesNothingAndWritesNothing();
            ContainedStableEnvelope_RequestsNoRebuild();
            ApplyScheduleDecision_IsAllocationFree();
            Debug.Log(
                "[OperationMapRenderProxyApplySystemValidation] " +
                "result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderProxyApplySystemValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public static void OrdersBeforePresentationUpdateAndEntitiesGraphics()
    {
        System.Type type = typeof(OperationMapRenderProxyApplySystem);
        UpdateInGroupAttribute group =
            type.GetCustomAttribute<UpdateInGroupAttribute>();
        Assert.That(group, Is.Not.Null);
        Assert.That(group.GroupType, Is.EqualTo(typeof(PresentationSystemGroup)));
        System.Type[] targets = type.GetCustomAttributes<UpdateBeforeAttribute>()
            .Select(attribute => attribute.SystemType)
            .ToArray();
        Assert.That(targets, Does.Contain(typeof(UpdatePresentationSystemGroup)));
        Assert.That(targets, Does.Contain(typeof(EntitiesGraphicsSystem)));
    }

    [Test]
    public static void SchedulesApplyThroughStateDependency()
    {
        using var fixture = new SystemFixture(commandGeneration: 4);
        fixture.UpdateAndCompleteForTest();

        OperationMapRenderProxySlotComponent slot =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(fixture.Slot);
        Assert.That(slot.PlacementIndex, Is.Zero);
        Assert.That(slot.PartIndex, Is.Zero);
        Assert.That(slot.AssignmentGeneration, Is.EqualTo(4));
        Assert.That(
            fixture.EntityManager.IsComponentEnabled<MaterialMeshInfo>(
                fixture.Slot),
            Is.True);
        Assert.That(
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Slot).Value.c3.x,
            Is.EqualTo(7f).Within(0.0001f));
        Assert.That(fixture.GetScheduledApplyCount(), Is.EqualTo(1));
    }

    [Test]
    public static void
        StableVersion_SchedulesNothingAllocatesNothingAndWritesNothing()
    {
        using var fixture = new SystemFixture(commandGeneration: 4);
        fixture.UpdateAndCompleteForTest();
        LocalToWorld transformBefore =
            fixture.EntityManager.GetComponentData<LocalToWorld>(fixture.Slot);
        OperationMapRenderProxySlotComponent slotBefore =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(fixture.Slot);
        bool enabledBefore =
            fixture.EntityManager.IsComponentEnabled<MaterialMeshInfo>(
                fixture.Slot);
        uint scheduledBefore = fixture.GetScheduledApplyCount();

        long allocatedBytes = fixture.UpdateStableAndMeasureAllocation();

        Assert.That(allocatedBytes, Is.Zero);
        Assert.That(fixture.GetScheduledApplyCount(), Is.EqualTo(scheduledBefore));
        Assert.That(
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Slot).Value,
            Is.EqualTo(transformBefore.Value));
        Assert.That(
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(fixture.Slot),
            Is.EqualTo(slotBefore));
        Assert.That(
            fixture.EntityManager.IsComponentEnabled<MaterialMeshInfo>(
                fixture.Slot),
            Is.EqualTo(enabledBefore));
        Assert.That(
            fixture.EntityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(
                    fixture.Owner).RebuildCount,
            Is.EqualTo(7));
    }

    [Test]
    public static void ContainedStableEnvelope_RequestsNoRebuild()
    {
        var input = new OperationMapRenderGuardEnvelopeInput
        {
            InitialViewApplied = 1,
            RequiredEnvelope = new OperationMapRenderCellEnvelope
            {
                Min = new int2(2, 3),
                Max = new int2(4, 5)
            },
            GuardEnvelope = new OperationMapRenderCellEnvelope
            {
                Min = new int2(1, 2),
                Max = new int2(5, 6)
            }
        };

        Assert.That(
            OperationMapRenderGuardEnvelopeDecision.TryDecide(
                input,
                out OperationMapRenderRebuildReason reason),
            Is.True);
        Assert.That(reason, Is.EqualTo(OperationMapRenderRebuildReason.None));
    }

    [Test]
    public static void ApplyScheduleDecision_IsAllocationFree()
    {
        Entity owner = new Entity { Index = 17, Version = 3 };
        Assert.That(
            OperationMapRenderApplyScheduleDecision.ShouldSchedule(
                owner, 9, owner, 9),
            Is.False);
        long before = System.GC.GetAllocatedBytesForCurrentThread();
        bool scheduled = false;
        for (int index = 0; index < 10000; index++)
        {
            scheduled |=
                OperationMapRenderApplyScheduleDecision.ShouldSchedule(
                    owner, 9, owner, 9);
        }
        long after = System.GC.GetAllocatedBytesForCurrentThread();

        Assert.That(scheduled, Is.False);
        Assert.That(after - before, Is.Zero);
    }

    [Test]
    public static void UnchangedGenerationLeavesSlotUntouched()
    {
        using var fixture = new SystemFixture(commandGeneration: 0);
        fixture.EntityManager.SetComponentData(
            fixture.Slot,
            new LocalToWorld
            {
                Value = float4x4.Translate(new float3(91f, 0f, 0f))
            });
        fixture.UpdateAndCompleteForTest();

        Assert.That(
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Slot).Value.c3.x,
            Is.EqualTo(91f));
        Assert.That(
            fixture.EntityManager.IsComponentEnabled<MaterialMeshInfo>(
                fixture.Slot),
            Is.False);
    }

    private sealed class SystemFixture : System.IDisposable
    {
        private readonly World _world;
        private readonly BlobAssetReference<OperationMapRenderDatabaseBlob>
            _database;
        private readonly SystemHandle _system;
        internal EntityManager EntityManager => _world.EntityManager;
        internal Entity Owner { get; }
        internal Entity Slot { get; }

        internal SystemFixture(int commandGeneration)
        {
            _world = new World("OperationMapRenderProxyApplySystemTests");
            _database = CreateDatabase();
            Owner = EntityManager.CreateEntity(
                typeof(OperationMapRenderDatabaseComponent),
                typeof(OperationMapRenderSlotCommandStateComponent),
                typeof(OperationMapRenderVirtualizationStateComponent));
            EntityManager.SetComponentData(
                Owner,
                new OperationMapRenderDatabaseComponent { Blob = _database });
            EntityManager.SetComponentData(
                Owner,
                new OperationMapRenderSlotCommandStateComponent
                    { Version = (uint)commandGeneration });
            EntityManager.SetComponentData(
                Owner,
                new OperationMapRenderVirtualizationStateComponent
                    { RebuildCount = 7 });
            DynamicBuffer<OperationMapRenderSlotCommandComponent> commands =
                EntityManager.AddBuffer<
                    OperationMapRenderSlotCommandComponent>(Owner);
            commands.Add(new OperationMapRenderSlotCommandComponent
            {
                SlotIndex = 0,
                LogicalRowIndex = commandGeneration == 0 ? -1 : 0,
                PlacementIndex = commandGeneration == 0 ? -1 : 0,
                PartIndex = commandGeneration == 0 ? -1 : 0,
                PoolBucketIndex = commandGeneration == 0 ? -1 : 0,
                AssignmentGeneration = commandGeneration,
                Assigned = commandGeneration == 0 ? (byte)0 : (byte)1
            });

            Slot = EntityManager.CreateEntity(
                typeof(OperationMapRenderProxySlotComponent),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(MaterialMeshInfo),
                typeof(URPMaterialPropertyBaseColor));
            EntityManager.SetComponentData(
                Slot,
                new OperationMapRenderProxySlotComponent
                {
                    SlotIndex = 0,
                    PoolBucketIndex = 0,
                    PlacementIndex = -1,
                    PartIndex = -1,
                    AssignmentGeneration = 0
                });
            EntityManager.SetComponentEnabled<MaterialMeshInfo>(Slot, false);
            _system =
                _world.CreateSystem<OperationMapRenderProxyApplySystem>();
        }

        internal void UpdateAndCompleteForTest()
        {
            _system.Update(_world.Unmanaged);
            _world.EntityManager.CompleteAllTrackedJobs();
        }

        internal long UpdateStableAndMeasureAllocation()
        {
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            _system.Update(_world.Unmanaged);
            return System.GC.GetAllocatedBytesForCurrentThread() - before;
        }

        internal uint GetScheduledApplyCount() =>
            _world.Unmanaged.GetUnsafeSystemRef<
                OperationMapRenderProxyApplySystem>(_system)
                .ScheduledApplyCount;

        public void Dispose()
        {
            if (_world.IsCreated)
            {
                _world.DestroySystem(_system);
                _world.Dispose();
            }
            if (_database.IsCreated)
                _database.Dispose();
        }
    }

    private static BlobAssetReference<OperationMapRenderDatabaseBlob>
        CreateDatabase()
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref OperationMapRenderDatabaseBlob root =
            ref builder.ConstructRoot<OperationMapRenderDatabaseBlob>();
        BlobBuilderArray<OperationMapRenderPrototypeBlob> prototypes =
            builder.Allocate(ref root.Prototypes, 1);
        prototypes[0] = new OperationMapRenderPrototypeBlob
            { FirstPart = 0, PartCount = 1 };
        BlobBuilderArray<OperationMapRenderPrototypePartBlob> parts =
            builder.Allocate(ref root.Parts, 1);
        parts[0] = new OperationMapRenderPrototypePartBlob
        {
            MeshArrayIndex = 0,
            MaterialArrayIndex = 0,
            SubMeshIndex = 0,
            LocalToPlacement =
                float4x4.Translate(new float3(2f, 0f, 0f)),
            LocalBounds = new OperationMapRenderBoundsBlob
            {
                Center = float3.zero,
                Extents = new float3(1f)
            },
            LinearBaseColor = new float4(1f),
            PoolBucketIndex = 0
        };
        BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
            builder.Allocate(ref root.Placements, 1);
        placements[0] = new OperationMapRenderPlacementBlob
        {
            PrototypeIndex = 0,
            WorldMatrix = float4x4.Translate(new float3(5f, 0f, 0f))
        };
        BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
            builder.Allocate(ref root.PoolBuckets, 1);
        buckets[0] = new OperationMapRenderPoolBucketBlob
            { FirstSlot = 0, Capacity = 1 };
        builder.Allocate(ref root.Cells, 0);
        builder.Allocate(ref root.CellPlacementIndices, 0);
        return builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(
            Allocator.Persistent);
    }
}
