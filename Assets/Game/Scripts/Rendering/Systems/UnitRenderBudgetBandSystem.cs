using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public readonly struct UnitRenderBudgetBandSystem
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
        NativeList<UnitRenderBudgetDistanceSystem.UnitDistance> distances,
        int maxDetailedUnits,
        int maxMidLodUnits,
        int maxLowLodUnits,
        float alwaysDetailedDistanceSq,
        Allocator allocator)
    {
        NativeHashSet<Entity> detailedUnits = new(math.max(1, distances.Length), allocator);
        int detailedCount = 0;
        for (int i = 0; i < distances.Length && detailedCount < maxDetailedUnits; i++)
        {
            if (distances[i].DistanceSq > alwaysDetailedDistanceSq)
                continue;

            if (detailedUnits.Add(distances[i].Unit))
                detailedCount++;
        }

        for (int i = 0; i < distances.Length && detailedCount < maxDetailedUnits; i++)
        {
            if (detailedUnits.Add(distances[i].Unit))
                detailedCount++;
        }

        NativeHashSet<Entity> midLodUnits = new(math.max(1, distances.Length), allocator);
        int midCount = 0;
        for (int i = 0; i < distances.Length && midCount < maxMidLodUnits; i++)
        {
            Entity unit = distances[i].Unit;
            if (detailedUnits.Contains(unit))
                continue;

            if (midLodUnits.Add(unit))
                midCount++;
        }

        NativeHashSet<Entity> lowLodUnits = new(math.max(1, distances.Length), allocator);
        int lowCount = 0;
        for (int i = 0; i < distances.Length && lowCount < maxLowLodUnits; i++)
        {
            Entity unit = distances[i].Unit;
            if (detailedUnits.Contains(unit) || midLodUnits.Contains(unit))
                continue;

            if (lowLodUnits.Add(unit))
                lowCount++;
        }

        return new Plan(detailedUnits, midLodUnits, lowLodUnits, detailedCount, midCount, lowCount);
    }
}
