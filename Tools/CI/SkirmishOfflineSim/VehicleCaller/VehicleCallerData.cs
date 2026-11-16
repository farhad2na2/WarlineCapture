using Unity.Mathematics;using Unity.Entities;using Unity.Collections;
namespace Game.Components {
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
public struct UnitMove : IComponentData
    {
        public float Speed;
        public float WalkSpeed;
        public float RoadSpeedMultiplier;
        public float ArriveDistance;
    }
public struct UnitTransportBoardingTarget : IComponentData
    {
        public Entity Transport;
        public int2 Goal;
        public byte PassengerKind;
        public int CargoWeight;
    }
public struct UnitAirMovement : IComponentData
    {
        public float CruiseHeight;
        public float RunwayTaxiSpeed;
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
        public byte ManualMove;
    }
public struct UnitDeathAnimationComponent : IComponentData
    {
        public float TimeRemaining;
        public byte PoseFrozen;
    }
public struct AutoWanderMoveTag : IComponentData
    {
    }
public struct EngageTarget : IComponentData
    {
        public Entity Target;
        public int2 Cell; // last known target cell
        public Unity.Mathematics.float3 Position; // last known target world position
        public byte IsCommanded;
    }
public struct ManualMoveOrderTag : IComponentData
    {
    }
public struct ManualMoveGroupMemberTag : IComponentData
    {
    }
public struct CampaignMissionGuidedMoveInProgressTag : IComponentData
    {
    }
public struct CampaignMissionStationaryUnitTag : IComponentData
    {
    }
public static class GridUtils
    {
        public static bool InBounds(int2 cell, int width, int height) =>
            (uint)cell.x < (uint)width && (uint)cell.y < (uint)height;

        public static int CellToIndex(int2 cell, int width) => cell.x + (cell.y * width);

        public static int2 IndexToCell(int index, int width) => new(index % width, index / width);

        private static GridCoordinateFrame CoordinateFrame(in GridConfig grid) => new GridCoordinateFrame
        {
            OriginX = grid.Origin.x, OriginY = grid.Origin.y, OriginZ = grid.Origin.z, CellSize = grid.CellSize
        };

        public static float3 CellToWorldCenter(in GridConfig grid, int2 cell)
        {
            var value = GridCoordinateKernel.CellToWorldCenter(CoordinateFrame(grid), cell.x, cell.y);
            return new float3(value.X, value.Y, value.Z);
        }

        public static int2 WorldToCell(in GridConfig grid, float3 worldPos)
        {
            var value = GridCoordinateKernel.WorldToCell(CoordinateFrame(grid), worldPos.x, worldPos.z);
            return new int2(value.X, value.Z);
        }
    }
}
