using Unity.Mathematics;

internal struct UnitPathSegmentation
{
    public const float DefaultLongDistanceSegmentCells = 32f;
    public const float ManualInfantryLongDistanceSegmentCells = 1024f;
    public const float ManualVehicleLongDistanceSegmentCells = 128f;

    public float GetMaxSegmentCells(bool manualMove, bool isVehicle)
    {
        if (!manualMove)
            return DefaultLongDistanceSegmentCells;

        return isVehicle
            ? ManualVehicleLongDistanceSegmentCells
            : ManualInfantryLongDistanceSegmentCells;
    }

    public bool ExceedsMaxSegment(int2 start, int2 goal, bool manualMove, bool isVehicle)
    {
        float2 delta = new float2(goal.x - start.x, goal.y - start.y);
        return math.length(delta) > GetMaxSegmentCells(manualMove, isVehicle);
    }

    public int2 GetSegmentGoal(int2 start, int2 requestedGoal, float maxSegmentCells)
    {
        float2 delta = new float2(requestedGoal.x - start.x, requestedGoal.y - start.y);
        float distance = math.length(delta);
        if (distance <= maxSegmentCells || distance <= 0.001f)
            return requestedGoal;

        float2 dir = delta / distance;
        return start + (int2)math.round(dir * maxSegmentCells);
    }
}
