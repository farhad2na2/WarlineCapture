using System;
namespace Game.Runtime.Pathfinding
{
    public interface IPathGoalIdentityContext
    {
        int Width { get; } int Height { get; }
        bool CanUseGoal(PlacementCell cell);
        void ReserveGoal(PlacementCell cell);
    }
    public static class PathGoalPolicyKernel
    {
        public const int InfantryGoalSearchRadius = 10, VehicleGoalSearchRadius = 20;
        public const int InfantryAlternateGoalCandidates = 16, VehicleAlternateGoalCandidates = 32;
        public const float DefaultLongDistanceSegmentCells = 32f;
        public const float ResourceHaulerDetailedSearchCells = 256f;
        public const float ManualInfantryLongDistanceSegmentCells = 1024f;
        public const float ManualVehicleLongDistanceSegmentCells = 128f;
        // False means the native fallback ring search must run; it does not authorize using desired.
        public static bool TryUseDesiredGoal<T>(ref T context, PlacementCell desired, int startIndex, out PlacementCell goal)
            where T : struct, IPathGoalIdentityContext
        {
            goal = default;
            if (desired.x < 0 || desired.y < 0 || desired.x >= context.Width || desired.y >= context.Height) return false;
            int index = desired.x + desired.y * context.Width;
            if (index == startIndex) { goal = desired; return true; }
            if (!context.CanUseGoal(desired)) return false;
            context.ReserveGoal(desired); goal = desired; return true;
        }
        public static byte GetRoutingMode(bool manual, bool vehicle, bool detailedSkirmish, bool detailedHauler) =>
            (byte)(detailedHauler ? 3 : (detailedSkirmish || detailedHauler) && vehicle ? 2 : manual && !vehicle ? 1 : 0);
        public static bool SkipAlternateSearch(bool manual, bool vehicle, bool detailedSkirmish, bool detailedHauler) =>
            detailedHauler || manual && (!vehicle || detailedSkirmish);
        public static float GetMaxSegmentCells(bool manual, bool vehicle) =>
            !manual ? DefaultLongDistanceSegmentCells : vehicle ? ManualVehicleLongDistanceSegmentCells : ManualInfantryLongDistanceSegmentCells;
        private static float Distance(PlacementCell start, PlacementCell goal)
        {
            float x = unchecked(goal.x - start.x), z = unchecked(goal.y - start.y);
            float squared = x*x + z*z;
            return (float)Math.Sqrt((float)squared);
        }
        public static bool WithinSegment(PlacementCell start, PlacementCell goal, float maxSegmentCells)
        { float distance=Distance(start,goal); return distance <= maxSegmentCells || distance <= .001f; }
        public static bool ExceedsMaxSegment(PlacementCell start, PlacementCell goal, bool manual, bool vehicle) =>
            Distance(start,goal) > GetMaxSegmentCells(manual,vehicle);
    }
}
