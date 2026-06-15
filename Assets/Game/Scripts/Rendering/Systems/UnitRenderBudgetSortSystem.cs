using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public readonly struct UnitRenderBudgetSort
{
    public void Sort(NativeList<UnitRenderBudgetDistance.UnitDistance> distances)
    {
        if (distances.Length <= 1)
            return;

        new SortDistancesJob
        {
            Distances = distances.AsArray()
        }.Run();
    }

    [BurstCompile]
    private struct SortDistancesJob : IJob
    {
        public NativeArray<UnitRenderBudgetDistance.UnitDistance> Distances;

        public void Execute()
        {
            Distances.Sort(new UnitDistanceComparer());
        }
    }

    private struct UnitDistanceComparer : IComparer<UnitRenderBudgetDistance.UnitDistance>
    {
        public int Compare(UnitRenderBudgetDistance.UnitDistance x, UnitRenderBudgetDistance.UnitDistance y)
        {
            if (x.Priority < y.Priority)
                return -1;
            if (x.Priority > y.Priority)
                return 1;
            if (x.DistanceSq < y.DistanceSq)
                return -1;
            if (x.DistanceSq > y.DistanceSq)
                return 1;

            return 0;
        }
    }
}
