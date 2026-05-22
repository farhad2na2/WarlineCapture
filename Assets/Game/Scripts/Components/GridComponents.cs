using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct GridConfig : IComponentData
{
    public int Width;
    public int Height;
    public float CellSize;
    public float3 Origin;
}

public struct GridWalkable : IBufferElementData
{
    public byte Value; // 1 = walkable, 0 = blocked
}

public struct GridRoad : IBufferElementData
{
    public byte Value; // 1 = road, 0 = not road
}

public struct GridRoadSidewalk : IBufferElementData
{
    public byte Value; // 1 = sidewalk road cell, 0 = not sidewalk
}

public struct GridRoadDirt : IBufferElementData
{
    public byte Value; // 1 = dirt road cell, 0 = not dirt road
}

public struct UnitGrid : IComponentData
{
    public int2 Cell;
}

public struct UnitMove : IComponentData
{
    public float Speed;
    public float WalkSpeed;
    public float RoadSpeedMultiplier;
    public float ArriveDistance;
}

public struct UnitFootprint : IComponentData
{
    public int2 Size;
}

public struct UnitMovementBehavior : IComponentData
{
    public byte AllowIdleWander;
    public byte UsesVehicleMotion;
}

public struct UnitVehicleMovement : IComponentData
{
    public float TurnSpeedDegrees;
    public float Acceleration;
    public float Braking;
    public float RearPivotOffset;
}

public struct UnitVehicleKinematics : IComponentData
{
    public float CurrentSpeed;
    public float StallSeconds;
}

public struct UnitTransportCapacity : IComponentData
{
    public int SoldierCapacity;
}

public struct UnitTransportPassenger : IComponentData
{
    public Entity Transport;
}

public struct UnitTransportBoardingTarget : IComponentData
{
    public Entity Transport;
    public int2 Goal;
}

public struct UnitTransportPassengerElement : IBufferElementData
{
    public Entity Passenger;
}

public struct UnitTransportRopeDisembarkRequest : IComponentData
{
    public int2 ReferenceCell;
    public float NextDropAt;
    public float DropIntervalSeconds;
    public int DropCount;
}

public struct UnitTransportRopeDropState : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public int2 DisperseCell;
    public float StartedAt;
    public float DurationSeconds;
    public byte HasDisperseCell;
}

public struct UnitTransportRopeLandingClearance : IComponentData
{
    public Entity Transport;
    public int2 LandingCell;
}

public struct UnitTransportRopeDisperseState : IComponentData
{
    public float3 StartPosition;
    public float3 EndPosition;
    public int2 EndCell;
    public float StartedAt;
    public float DurationSeconds;
}

public struct UnitAirMovement : IComponentData
{
    public float CruiseHeight;
    public float RunwayTaxiSpeed;
}

public struct UnitAirState : IComponentData
{
    public float3 HomePosition;
    public int2 HomeCell;
    public byte HomeInitialized;
    public byte ReturningHome;
    public byte Airborne;
    public byte UsesRunway;
    public byte TakeoffRolling;
    public byte LandingRolling;
    public byte AttackRunActive;
    public byte ReturnApproachInitialized;
    public float3 RunwayTakeoffPosition;
    public int2 RunwayTakeoffCell;
    public float3 RunwayLandingPosition;
    public int2 RunwayLandingCell;
    public float3 AttackRunExitPosition;
}

public struct UnitSpawnTransitTag : IComponentData
{
}

public struct UnitPathRequest : IComponentData
{
    public int2 Goal;
}

public struct UnitTarget : IComponentData
{
    public int2 Cell;
}

public struct UnitPathFollow : IComponentData
{
    public int PathIndex;
}

public struct UnitPathRange : IComponentData
{
    public int Start;
    public int Length;
}

public struct UnitLongDistanceMove : IComponentData
{
    public int2 FinalGoal;
}

public struct UnitPathRetryCooldown : IComponentData
{
    public int ResumeFrame;
}

public struct UnitPathCell : IBufferElementData
{
    public int2 Value;
}

public struct UnitGridInitialized : IComponentData
{
}

public struct StaticGridBlocker : IComponentData
{
}

public struct FriendlyPassGridBlocker : IComponentData
{
    public byte AllowedFactionId;
}

public struct GridBlockerSize : IComponentData
{
    public int2 Size; // in cells, >= (1,1)
}

public struct StaticBlockerPrevBounds : ICleanupComponentData
{
    public int2 Min; // inclusive
    public int2 Max; // exclusive
    public byte FriendlyPassFactionId;
}

public struct RuntimeGridBlockerDependencyState : IComponentData
{
    public byte ReadyForDependents;
    public byte SpawnOnStart;
    public byte Spawned;
    public byte SpawnFinalizing;
    public int FinalizeAfterFrames;
    public byte PendingCity;
    public byte CityHasSpawned;
    public byte CityGenerating;
}

public static class GridUtils
{
    public static bool InBounds(int2 cell, int width, int height) =>
        (uint)cell.x < (uint)width && (uint)cell.y < (uint)height;

    public static int CellToIndex(int2 cell, int width) => cell.x + (cell.y * width);

    public static int2 IndexToCell(int index, int width) => new(index % width, index / width);

    public static float3 CellToWorldCenter(in GridConfig grid, int2 cell) =>
        grid.Origin + new float3((cell.x + 0.5f) * grid.CellSize, 0f, (cell.y + 0.5f) * grid.CellSize);

    public static int2 WorldToCell(in GridConfig grid, float3 worldPos)
    {
        float3 local = worldPos - grid.Origin;
        int x = (int)math.floor(local.x / grid.CellSize);
        int y = (int)math.floor(local.z / grid.CellSize);
        return new int2(x, y);
    }
}

public struct DynamicBlockerData : IComponentData
{
    public int GridSize;
    public NativeArray<int> Counts;
    public NativeBitArray Blocked;
    public NativeArray<byte> FriendlyPassFactionIds;
}

public struct PathPoolData : IComponentData
{
    public NativeList<int2> Cells;
}

public struct DynamicOccupancyData : IComponentData
{
    public int GridSize;
    public NativeBitArray Occupied;
}
