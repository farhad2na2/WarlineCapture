using System.Collections.Generic;
using Unity.Collections;

public readonly struct UnitRenderBudgetSortSystem
{
    private struct UnitDistanceComparer : IComparer<UnitRenderBudgetDistanceSystem.UnitDistance>
    {
        public int Compare(UnitRenderBudgetDistanceSystem.UnitDistance x, UnitRenderBudgetDistanceSystem.UnitDistance y)
        {
            int priorityCompare = x.Priority.CompareTo(y.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            return x.DistanceSq.CompareTo(y.DistanceSq);
        }
    }

    public void Sort(NativeList<UnitRenderBudgetDistanceSystem.UnitDistance> distances)
    {
        distances.AsArray().Sort(new UnitDistanceComparer());
    }
}
