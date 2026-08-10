using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Rendering
{
    public readonly struct UnitRenderBudgetSnapshot
    {
        public struct Snapshot : IDisposable
        {
            public NativeList<Entity> Units;
            public NativeList<LocalTransform> Transforms;

            public Snapshot(NativeList<Entity> units, NativeList<LocalTransform> transforms)
            {
                Units = units;
                Transforms = transforms;
            }

            public void Dispose()
            {
                if (Transforms.IsCreated)
                    Transforms.Dispose();
                if (Units.IsCreated)
                    Units.Dispose();
            }
        }

        public Snapshot Create(
            EntityQuery unitQuery,
            EntityTypeHandle entityTypeHandle,
            ComponentTypeHandle<LocalTransform> localTransformTypeHandle,
            JobHandle dependency,
            Allocator allocator)
        {
            int capacity = unitQuery.CalculateEntityCount();
            NativeList<Entity> units = new(capacity, allocator);
            NativeList<LocalTransform> transforms = new(capacity, allocator);
            JobHandle collectHandle = new CollectSnapshotJob
            {
                EntityTypeHandle = entityTypeHandle,
                LocalTransformTypeHandle = localTransformTypeHandle,
                Units = units,
                Transforms = transforms
            }.Schedule(unitQuery, dependency);
            collectHandle.Complete();

            return new Snapshot(units, transforms);
        }

        [BurstCompile]
        private struct CollectSnapshotJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformTypeHandle;
            public NativeList<Entity> Units;
            public NativeList<LocalTransform> Transforms;

            public void Execute(
                in ArchetypeChunk chunk,
                int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref LocalTransformTypeHandle);
                for (int i = 0; i < chunk.Count; i++)
                {
                    Units.Add(entities[i]);
                    Transforms.Add(transforms[i]);
                }
            }
        }
    }
}
