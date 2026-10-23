using Unity.Mathematics;
using Game.Runtime.Pathfinding;

namespace Game.Runtime
{
    internal struct UnitPathSegmentation
    {
        public const float DefaultLongDistanceSegmentCells = PathGoalPolicyKernel.DefaultLongDistanceSegmentCells;
        public const float ResourceHaulerDetailedSearchCells = PathGoalPolicyKernel.ResourceHaulerDetailedSearchCells;
        public const float ManualInfantryLongDistanceSegmentCells = PathGoalPolicyKernel.ManualInfantryLongDistanceSegmentCells;
        public const float ManualVehicleLongDistanceSegmentCells = PathGoalPolicyKernel.ManualVehicleLongDistanceSegmentCells;

        public float GetMaxSegmentCells(bool manualMove, bool isVehicle) => PathGoalPolicyKernel.GetMaxSegmentCells(manualMove,isVehicle);
        public bool ExceedsMaxSegment(int2 start, int2 goal, bool manualMove, bool isVehicle) =>
            PathGoalPolicyKernel.ExceedsMaxSegment(new PlacementCell(start.x,start.y),new PlacementCell(goal.x,goal.y),manualMove,isVehicle);

        public int2 GetSegmentGoal(int2 start, int2 requestedGoal, float maxSegmentCells)
        {
            float2 delta = new float2(requestedGoal.x - start.x, requestedGoal.y - start.y);
            float distance = math.length(delta);
            if (PathGoalPolicyKernel.WithinSegment(new PlacementCell(start.x,start.y),new PlacementCell(requestedGoal.x,requestedGoal.y),maxSegmentCells))
                return requestedGoal;

            float2 dir = delta / distance;
            return start + (int2)math.round(dir * maxSegmentCells);
        }
    }
}
