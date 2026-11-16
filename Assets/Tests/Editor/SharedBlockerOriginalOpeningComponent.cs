#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using Unity.Entities;
using Game.Components;
using Unity.Mathematics;

namespace Game.Tests.Editor
{
    // Removes only this blocker's contribution on the authored traversable surface.
    // Placement reservation, other overlapping blockers and occupancy remain independent.
    public struct SharedBlockerOriginalOpeningComponent : IComponentData
    {
        public float2 Center;
        public float2 Right;
        public float2 Forward;
        public float2 HalfExtents;

        public bool IsValid => math.all(math.isfinite(Center)) && math.all(math.isfinite(Right)) &&
            math.all(math.isfinite(Forward)) && math.all(math.isfinite(HalfExtents)) &&
            math.all(HalfExtents > 0f) && math.abs(math.lengthsq(Right) - 1f) < .001f &&
            math.abs(math.lengthsq(Forward) - 1f) < .001f && math.abs(math.dot(Right, Forward)) < .001f;

        public bool ContainsFullCell(in GridConfig grid, int x, int z)
        {
            if (!IsValid || grid.CellSize <= 0f) return false;
            float2 delta = grid.Origin.xz + new float2(x + .5f, z + .5f) * grid.CellSize - Center;
            float half = grid.CellSize * .5f;
            return math.abs(math.dot(delta, Right)) + half * math.csum(math.abs(Right)) <= HalfExtents.x &&
                math.abs(math.dot(delta, Forward)) + half * math.csum(math.abs(Forward)) <= HalfExtents.y;
        }

        public bool SameGeometry(in SharedBlockerOriginalOpeningComponent other) =>
            math.all(Center == other.Center) && math.all(Right == other.Right) &&
            math.all(Forward == other.Forward) && math.all(HalfExtents == other.HalfExtents);
    }

    // Retained after native entity destruction so subtraction exactly matches admission.
    public struct SharedBlockerOriginalPrevOpeningComponent : ICleanupComponentData
    {
        public SharedBlockerOriginalOpeningComponent Value;
    }
}

#endif
