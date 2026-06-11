using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public readonly struct UnitRenderBudgetSnapshotSystem
{
    public struct Snapshot : IDisposable
    {
        public NativeArray<Entity> Units;
        public NativeArray<LocalTransform> Transforms;

        public Snapshot(NativeArray<Entity> units, NativeArray<LocalTransform> transforms)
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

    public Snapshot Create(EntityQuery unitQuery, Allocator allocator)
    {
        return new Snapshot(
            unitQuery.ToEntityArray(allocator),
            unitQuery.ToComponentDataArray<LocalTransform>(allocator));
    }
}
