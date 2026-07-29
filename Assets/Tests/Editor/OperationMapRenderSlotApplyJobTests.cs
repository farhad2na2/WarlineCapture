using Game.Components;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public sealed class OperationMapRenderSlotApplyJobTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            RunCase(nameof(AssignedCommand_WritesCompleteFlattenedRenderState),
                AssignedCommand_WritesCompleteFlattenedRenderState);
            RunCase(nameof(ReleaseCommand_FullyResetsAndDisablesSlot),
                ReleaseCommand_FullyResetsAndDisablesSlot);
            RunCase(nameof(CleanSlot_IsNotWritten),
                CleanSlot_IsNotWritten);
            RunCase(nameof(InvalidCommand_FailsWithoutPartialWrite),
                InvalidCommand_FailsWithoutPartialWrite);
            RunCase(nameof(SlotsRemainHierarchyFree),
                SlotsRemainHierarchyFree);
            Debug.Log(
                "[OperationMapRenderSlotApplyValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[OperationMapRenderSlotApplyValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    private static void RunCase(string name, System.Action action)
    {
        try
        {
            action();
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[OperationMapRenderSlotApplyValidation] " +
                $"result=Failed test={name} error={exception}");
            throw;
        }
    }

    [Test]
    public static void AssignedCommand_WritesCompleteFlattenedRenderState()
    {
        using var fixture = new ApplyFixture();
        fixture.Commands[0] = AssignedCommand(0, 9);
        fixture.Dirty.Set(0, true);
        fixture.Run();

        LocalToWorld transform =
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Entities[0]);
        Assert.That(transform.Value.c3.x, Is.EqualTo(12f).Within(0.0001f));
        Assert.That(transform.Value.c3.y, Is.EqualTo(2f).Within(0.0001f));
        Assert.That(transform.Value.c3.z, Is.EqualTo(3f).Within(0.0001f));
        RenderBounds bounds =
            fixture.EntityManager.GetComponentData<RenderBounds>(
                fixture.Entities[0]);
        Assert.That(bounds.Value.Center, Is.EqualTo(new float3(1f, 2f, 3f)));
        Assert.That(bounds.Value.Extents, Is.EqualTo(new float3(4f, 5f, 6f)));
        MaterialMeshInfo materialMesh =
            fixture.EntityManager.GetComponentData<MaterialMeshInfo>(
                fixture.Entities[0]);
        Assert.That(
            MaterialMeshInfo.StaticIndexToArrayIndex(materialMesh.Material),
            Is.EqualTo(3));
        Assert.That(
            MaterialMeshInfo.StaticIndexToArrayIndex(materialMesh.Mesh),
            Is.EqualTo(2));
        Assert.That(materialMesh.SubMesh, Is.EqualTo(1));
        Assert.That(
            fixture.EntityManager.GetComponentData<
                URPMaterialPropertyBaseColor>(fixture.Entities[0]).Value,
            Is.EqualTo(new float4(0.1f, 0.2f, 0.3f, 1f)));
        OperationMapRenderProxySlotComponent binding =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(fixture.Entities[0]);
        Assert.That(binding.PlacementIndex, Is.Zero);
        Assert.That(binding.PartIndex, Is.Zero);
        Assert.That(binding.AssignmentGeneration, Is.EqualTo(9));
        Assert.That(
            fixture.EntityManager.IsComponentEnabled<MaterialMeshInfo>(
                fixture.Entities[0]),
            Is.True);
        Assert.That(fixture.Failures[0], Is.EqualTo(
            OperationMapRenderSlotApplyFailure.None));
    }

    [Test]
    public static void ReleaseCommand_FullyResetsAndDisablesSlot()
    {
        using var fixture = new ApplyFixture();
        fixture.SetAssignedSentinel(1);
        fixture.Commands[1] = new OperationMapRenderSlotCommand
        {
            SlotIndex = 1,
            LogicalRowIndex = -1,
            PlacementIndex = -1,
            PartIndex = -1,
            PoolBucketIndex = -1,
            AssignmentGeneration = 12,
            Assigned = 0
        };
        fixture.Dirty.Set(1, true);
        fixture.Run();

        Assert.That(
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Entities[1]).Value,
            Is.EqualTo(float4x4.identity));
        RenderBounds bounds =
            fixture.EntityManager.GetComponentData<RenderBounds>(
                fixture.Entities[1]);
        Assert.That(bounds.Value.Center, Is.EqualTo(float3.zero));
        Assert.That(bounds.Value.Extents, Is.EqualTo(float3.zero));
        Assert.That(
            fixture.EntityManager.GetComponentData<
                URPMaterialPropertyBaseColor>(fixture.Entities[1]).Value,
            Is.EqualTo(new float4(1f)));
        OperationMapRenderProxySlotComponent binding =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(fixture.Entities[1]);
        Assert.That(binding.PlacementIndex, Is.EqualTo(-1));
        Assert.That(binding.PartIndex, Is.EqualTo(-1));
        Assert.That(binding.AssignmentGeneration, Is.EqualTo(12));
        Assert.That(
            fixture.EntityManager.IsComponentEnabled<MaterialMeshInfo>(
                fixture.Entities[1]),
            Is.False);
    }

    [Test]
    public static void CleanSlot_IsNotWritten()
    {
        using var fixture = new ApplyFixture();
        fixture.SetAssignedSentinel(0);
        fixture.Commands[0] = AssignedCommand(0, 15);
        fixture.Run();

        OperationMapRenderProxySlotComponent binding =
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(fixture.Entities[0]);
        Assert.That(binding.AssignmentGeneration, Is.EqualTo(4));
        Assert.That(
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Entities[0]).Value.c3.x,
            Is.EqualTo(99f));
    }

    [Test]
    public static void InvalidCommand_FailsWithoutPartialWrite()
    {
        using var fixture = new ApplyFixture();
        fixture.SetAssignedSentinel(0);
        OperationMapRenderSlotCommand command = AssignedCommand(0, 5);
        command.PoolBucketIndex = 1;
        fixture.Commands[0] = command;
        fixture.Dirty.Set(0, true);
        fixture.Run();

        Assert.That(fixture.Failures[0], Is.EqualTo(
            OperationMapRenderSlotApplyFailure.InvalidPlacement));
        Assert.That(
            fixture.EntityManager.GetComponentData<LocalToWorld>(
                fixture.Entities[0]).Value.c3.x,
            Is.EqualTo(99f));
        Assert.That(
            fixture.EntityManager.GetComponentData<
                OperationMapRenderProxySlotComponent>(
                    fixture.Entities[0]).AssignmentGeneration,
            Is.EqualTo(4));
    }

    [Test]
    public static void SlotsRemainHierarchyFree()
    {
        using var fixture = new ApplyFixture();
        fixture.Commands[0] = AssignedCommand(0, 1);
        fixture.Dirty.Set(0, true);
        fixture.Run();

        Assert.That(
            fixture.EntityManager.HasComponent<Parent>(fixture.Entities[0]),
            Is.False);
        Assert.That(
            fixture.EntityManager.HasComponent<Child>(fixture.Entities[0]),
            Is.False);
        Assert.That(
            fixture.EntityManager.HasComponent<LocalTransform>(
                fixture.Entities[0]),
            Is.False);
    }

    private static OperationMapRenderSlotCommand AssignedCommand(
        int slotIndex,
        int generation) =>
        new OperationMapRenderSlotCommand
        {
            SlotIndex = slotIndex,
            LogicalRowIndex = 0,
            PlacementIndex = 0,
            PartIndex = 0,
            PoolBucketIndex = 0,
            AssignmentGeneration = generation,
            Assigned = 1
        };

    private sealed class ApplyFixture : System.IDisposable
    {
        private readonly World _world;
        private readonly BlobAssetReference<OperationMapRenderDatabaseBlob>
            _database;
        private readonly EntityQuery _query;
        internal EntityManager EntityManager => _world.EntityManager;
        internal NativeArray<Entity> Entities;
        internal NativeArray<OperationMapRenderSlotCommand> Commands;
        internal NativeBitArray Dirty;
        internal NativeArray<OperationMapRenderSlotApplyFailure> Failures;

        internal ApplyFixture()
        {
            _world = new World("OperationMapRenderSlotApplyJobTests");
            _database = CreateDatabase();
            EntityArchetype archetype = EntityManager.CreateArchetype(
                typeof(OperationMapRenderProxySlotComponent),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(MaterialMeshInfo),
                typeof(URPMaterialPropertyBaseColor));
            Entities = EntityManager.CreateEntity(
                archetype, 2, Allocator.Persistent);
            for (int index = 0; index < Entities.Length; index++)
            {
                EntityManager.SetComponentData(
                    Entities[index],
                    new OperationMapRenderProxySlotComponent
                    {
                        SlotIndex = index,
                        PoolBucketIndex = 0,
                        PlacementIndex = -1,
                        PartIndex = -1,
                        AssignmentGeneration = 0
                    });
                EntityManager.SetComponentEnabled<MaterialMeshInfo>(
                    Entities[index], false);
            }
            _query = EntityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<
                        OperationMapRenderProxySlotComponent>(),
                    ComponentType.ReadWrite<LocalToWorld>(),
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ReadWrite<URPMaterialPropertyBaseColor>()
                },
                Options = EntityQueryOptions.IgnoreComponentEnabledState
            });
            Commands = new NativeArray<OperationMapRenderSlotCommand>(
                2, Allocator.Persistent);
            Dirty = new NativeBitArray(
                2, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            Failures =
                new NativeArray<OperationMapRenderSlotApplyFailure>(
                    2, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }

        internal void Run()
        {
            var job = new OperationMapRenderSlotApplyJob
            {
                Database = _database,
                SlotCommands = Commands,
                DirtySlots = Dirty,
                ProxySlotType = EntityManager.GetComponentTypeHandle<
                    OperationMapRenderProxySlotComponent>(false),
                LocalToWorldType =
                    EntityManager.GetComponentTypeHandle<LocalToWorld>(false),
                RenderBoundsType =
                    EntityManager.GetComponentTypeHandle<RenderBounds>(false),
                MaterialMeshInfoType =
                    EntityManager.GetComponentTypeHandle<MaterialMeshInfo>(false),
                BaseColorType = EntityManager.GetComponentTypeHandle<
                    URPMaterialPropertyBaseColor>(false),
                SlotFailures = Failures
            };
            job.ScheduleParallel(_query, default).Complete();
        }

        internal void SetAssignedSentinel(int slotIndex)
        {
            Entity entity = Entities[slotIndex];
            EntityManager.SetComponentData(
                entity,
                new LocalToWorld
                {
                    Value = float4x4.Translate(new float3(99f, 8f, 7f))
                });
            EntityManager.SetComponentData(
                entity,
                new RenderBounds
                {
                    Value = new AABB
                    {
                        Center = new float3(9f),
                        Extents = new float3(8f)
                    }
                });
            EntityManager.SetComponentData(
                entity,
                MaterialMeshInfo.FromRenderMeshArrayIndices(4, 5, 2));
            EntityManager.SetComponentData(
                entity,
                new URPMaterialPropertyBaseColor
                    { Value = new float4(0.8f) });
            EntityManager.SetComponentData(
                entity,
                new OperationMapRenderProxySlotComponent
                {
                    SlotIndex = slotIndex,
                    PoolBucketIndex = 0,
                    PlacementIndex = 7,
                    PartIndex = 8,
                    AssignmentGeneration = 4
                });
            EntityManager.SetComponentEnabled<MaterialMeshInfo>(entity, true);
        }

        public void Dispose()
        {
            EntityManager.CompleteAllTrackedJobs();
            Failures.Dispose();
            Dirty.Dispose();
            Commands.Dispose();
            Entities.Dispose();
            _database.Dispose();
            _world.Dispose();
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
            MeshArrayIndex = 2,
            MaterialArrayIndex = 3,
            SubMeshIndex = 1,
            LocalToPlacement =
                float4x4.Translate(new float3(2f, 0f, 0f)),
            LocalBounds = new OperationMapRenderBoundsBlob
            {
                Center = new float3(1f, 2f, 3f),
                Extents = new float3(4f, 5f, 6f)
            },
            LinearBaseColor = new float4(0.1f, 0.2f, 0.3f, 1f),
            PoolBucketIndex = 0
        };
        BlobBuilderArray<OperationMapRenderPlacementBlob> placements =
            builder.Allocate(ref root.Placements, 1);
        placements[0] = new OperationMapRenderPlacementBlob
        {
            PrototypeIndex = 0,
            WorldMatrix = float4x4.Translate(new float3(10f, 2f, 3f))
        };
        BlobBuilderArray<OperationMapRenderPoolBucketBlob> buckets =
            builder.Allocate(ref root.PoolBuckets, 1);
        buckets[0] = new OperationMapRenderPoolBucketBlob
            { FirstSlot = 0, Capacity = 2 };
        builder.Allocate(ref root.Cells, 0);
        builder.Allocate(ref root.CellPlacementIndices, 0);
        return builder.CreateBlobAssetReference<OperationMapRenderDatabaseBlob>(
            Allocator.Persistent);
    }
}
