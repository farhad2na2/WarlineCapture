using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public readonly struct UnitRenderBudgetBand
{
    public struct Plan : IDisposable
    {
        public NativeHashSet<Entity> DetailedUnits;
        public NativeHashSet<Entity> MidLodUnits;
        public NativeHashSet<Entity> LowLodUnits;
        public int DetailedCount;
        public int MidCount;
        public int LowCount;

        public Plan(
            NativeHashSet<Entity> detailedUnits,
            NativeHashSet<Entity> midLodUnits,
            NativeHashSet<Entity> lowLodUnits,
            int detailedCount,
            int midCount,
            int lowCount)
        {
            DetailedUnits = detailedUnits;
            MidLodUnits = midLodUnits;
            LowLodUnits = lowLodUnits;
            DetailedCount = detailedCount;
            MidCount = midCount;
            LowCount = lowCount;
        }

        public void Dispose()
        {
            if (LowLodUnits.IsCreated)
                LowLodUnits.Dispose();
            if (MidLodUnits.IsCreated)
                MidLodUnits.Dispose();
            if (DetailedUnits.IsCreated)
                DetailedUnits.Dispose();
        }
    }

    public Plan Create(
        NativeList<UnitRenderBudgetDistance.UnitDistance> distances,
        int maxDetailedUnits,
        int maxMidLodUnits,
        int maxLowLodUnits,
        float alwaysDetailedDistanceSq,
        Allocator allocator)
    {
        Allocator jobSafeAllocator = allocator == Allocator.Temp ? Allocator.TempJob : allocator;
        NativeHashSet<Entity> detailedUnits = new(math.max(1, distances.Length), jobSafeAllocator);
        NativeHashSet<Entity> midLodUnits = new(math.max(1, distances.Length), jobSafeAllocator);
        NativeHashSet<Entity> lowLodUnits = new(math.max(1, distances.Length), jobSafeAllocator);
        using NativeArray<int> counts = new(3, Allocator.TempJob);
        JobHandle buildHandle = new BuildBandPlanJob
        {
            Distances = distances.AsArray(),
            DetailedUnits = detailedUnits,
            MidLodUnits = midLodUnits,
            LowLodUnits = lowLodUnits,
            Counts = counts,
            MaxDetailedUnits = maxDetailedUnits,
            MaxMidLodUnits = maxMidLodUnits,
            MaxLowLodUnits = maxLowLodUnits,
            AlwaysDetailedDistanceSq = alwaysDetailedDistanceSq
        }.Schedule();
        buildHandle.Complete();

        return new Plan(detailedUnits, midLodUnits, lowLodUnits, counts[0], counts[1], counts[2]);
    }

    [BurstCompile]
    private struct BuildBandPlanJob : IJob
    {
        [ReadOnly] public NativeArray<UnitRenderBudgetDistance.UnitDistance> Distances;
        public NativeHashSet<Entity> DetailedUnits;
        public NativeHashSet<Entity> MidLodUnits;
        public NativeHashSet<Entity> LowLodUnits;
        public NativeArray<int> Counts;
        public int MaxDetailedUnits;
        public int MaxMidLodUnits;
        public int MaxLowLodUnits;
        public float AlwaysDetailedDistanceSq;

        public void Execute()
        {
            int detailedCount = 0;
            for (int i = 0; i < Distances.Length && detailedCount < MaxDetailedUnits; i++)
            {
                if (Distances[i].DistanceSq > AlwaysDetailedDistanceSq)
                    continue;

                if (DetailedUnits.Add(Distances[i].Unit))
                    detailedCount++;
            }

            for (int i = 0; i < Distances.Length && detailedCount < MaxDetailedUnits; i++)
            {
                if (DetailedUnits.Add(Distances[i].Unit))
                    detailedCount++;
            }

            int midCount = 0;
            for (int i = 0; i < Distances.Length && midCount < MaxMidLodUnits; i++)
            {
                Entity unit = Distances[i].Unit;
                if (DetailedUnits.Contains(unit))
                    continue;

                if (MidLodUnits.Add(unit))
                    midCount++;
            }

            int lowCount = 0;
            for (int i = 0; i < Distances.Length && lowCount < MaxLowLodUnits; i++)
            {
                Entity unit = Distances[i].Unit;
                if (DetailedUnits.Contains(unit) || MidLodUnits.Contains(unit))
                    continue;

                if (LowLodUnits.Add(unit))
                    lowCount++;
            }

            Counts[0] = detailedCount;
            Counts[1] = midCount;
            Counts[2] = lowCount;
        }
    }
}
