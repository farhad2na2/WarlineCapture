using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Game.Components
{
    public struct GridConfig : IComponentData
    {
        public int Width;
        public int Height;
        public float CellSize;
        public float3 Origin;
    }

    public struct RuntimeGridBootstrapGridTag : IComponentData
    {
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

    public struct UnitFuelConsumption : IComponentData
    {
        public float GroundFuelPerCell;
        public float AirFuelPerCell;
        public byte Enabled;
    }

    public struct UnitFuelConsumptionState : IComponentData
    {
        public int2 LastCell;
        public byte Initialized;
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

    public static class UnitTransportPassengerKind
    {
        public const byte Soldier = 0;
        public const byte Vehicle = 1;
    }

    public struct UnitTransportCargoCapacity : IComponentData
    {
        public int SoldierCapacity;
        public int VehicleCapacity;
        public int CargoWeightCapacity;
    }

    public struct UnitTransportPassenger : IComponentData
    {
        public Entity Transport;
    }

    public struct UnitTransportCargoPassenger : IComponentData
    {
        public Entity Transport;
        public byte PassengerKind;
        public int CargoWeight;
    }

    public struct UnitTransportAirdropVisualPrefabs : IComponentData
    {
        public Entity SoldierParachuteVisualPrefab;
        public Entity VehicleEmergencyDropVisualPrefab;
    }

    public struct UnitTransportAirdropVisualPrefabRegistryEntry : IBufferElementData
    {
        public FixedString64Bytes SourceKey;
        public Entity SoldierParachuteVisualPrefab;
        public Entity VehicleEmergencyDropVisualPrefab;
    }

    public struct UnitTransportPlaneDoorReference : IComponentData
    {
        public Entity DoorEntity;
        public quaternion ClosedLocalRotation;
        public quaternion OpenLocalRotation;
        public float OpenSeconds;
        public float CloseSeconds;
        public float3 DoorLocalPosition;
        public float3 InteriorLocalPosition;
        public float3 ApproachLocalPosition;
        public float3 RolloutLocalPosition;
    }

    public struct UnitTransportPlaneDoorState : IComponentData
    {
        public float Open01;
        public byte TargetOpen;
    }

    public struct UnitTransportPlaneDoorOpenRequest : IComponentData
    {
        public float RemainingSeconds;
    }

    public struct UnitTransportBoardingTarget : IComponentData
    {
        public Entity Transport;
        public int2 Goal;
        public byte PassengerKind;
        public int CargoWeight;
    }

    public struct UnitTransportDeployOrder : IComponentData
    {
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 TargetPosition;
        public byte AttackAfterDeploy;
    }

    public struct UnitTransportDeployAttackTarget : IComponentData
    {
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 TargetPosition;
    }

    public static class TransportBoardingData
    {
        public const int BoardingClearanceCells = 4;
        public const int AirBoardingClearanceCells = 1;
        public const float AirBoardingGroundedHeightTolerance = 3f;
    }

    public readonly struct TransportBoardingReachState
    {
        public readonly int2 TransportCell;
        public readonly int2 TransportSize;
        public readonly int2 PassengerCell;
        public readonly int2 BoardingGoal;
        public readonly int BoardingClearance;
        public readonly bool MovementFinished;
        public readonly bool AirTransport;
        public readonly bool ReachedBoardingGoal;
        public readonly int DistanceToBoardingGoal;
        public readonly bool SettledNearBoardingGoal;
        public readonly bool NearTransportFootprint;
        public readonly bool BoardingGoalNearTransport;
        public readonly bool ReachedTransport;

        public TransportBoardingReachState(
            int2 transportCell,
            int2 transportSize,
            int2 passengerCell,
            int2 boardingGoal,
            int boardingClearance,
            bool movementFinished,
            bool airTransport,
            bool reachedBoardingGoal,
            int distanceToBoardingGoal,
            bool settledNearBoardingGoal,
            bool nearTransportFootprint,
            bool boardingGoalNearTransport,
            bool reachedTransport)
        {
            TransportCell = transportCell;
            TransportSize = transportSize;
            PassengerCell = passengerCell;
            BoardingGoal = boardingGoal;
            BoardingClearance = boardingClearance;
            MovementFinished = movementFinished;
            AirTransport = airTransport;
            ReachedBoardingGoal = reachedBoardingGoal;
            DistanceToBoardingGoal = distanceToBoardingGoal;
            SettledNearBoardingGoal = settledNearBoardingGoal;
            NearTransportFootprint = nearTransportFootprint;
            BoardingGoalNearTransport = boardingGoalNearTransport;
            ReachedTransport = reachedTransport;
        }
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
        public int TotalDropCount;
    }

    public struct UnitTransportRopeDropComponent : IComponentData
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

    public static class UnitTransportAirdropMode
    {
        public const byte Mixed = 0;
        public const byte SoldierOnly = 1;
        public const byte VehicleOnly = 2;
    }

    public struct UnitTransportAirdropRequest : IComponentData
    {
        public int2 DropReferenceCell;
        public float NextDropAt;
        public float DropIntervalSeconds;
        public int DropCount;
        public int DroppedCount;
        public int SoldierDropCount;
        public int VehicleDropCount;
        public byte DropMode;
        public byte PassReady;
    }

    public struct UnitTransportParachuteDropComponent : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public int2 LandingCell;
        public float StartedAt;
        public float DurationSeconds;
        public Entity VisualEntity;
    }

    public struct UnitTransportCargoDropComponent : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public int2 LandingCell;
        public float StartedAt;
        public float DurationSeconds;
        public Entity VisualEntity;
    }

    public struct UnitTransportAirdropVisualCleanup : IComponentData
    {
        public float DestroyAt;
    }

    public struct UnitTransportAirdropSettleComponent : IComponentData
    {
        public float3 StartPosition;
        public float3 EndPosition;
        public int2 EndCell;
        public float StartedAt;
        public float DurationSeconds;
    }

    public struct UnitTransportRopeDisperseComponent : IComponentData
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

    public struct UnitAirComponent : IComponentData
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
        public sbyte AttackManeuverTurnSign;
        public int2 AttackPassGoalCell;
        public byte ReturnApproachInitialized;
        public float3 RunwayTakeoffPosition;
        public int2 RunwayTakeoffCell;
        public float3 RunwayLandingPosition;
        public int2 RunwayLandingCell;
        public float3 AttackRunExitPosition;
        public float FixedWingCruiseY;
        public byte FixedWingCruiseYInitialized;
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

    public struct UnitPathSurfaceNode : IBufferElementData
    {
        public int SurfaceId;
        public int LayerId;
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

    public struct RuntimeGridBlockerDependencyComponent : IComponentData
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

    public struct DynamicBlockerComponent : IComponentData
    {
        public int GridSize;
        public NativeArray<int> Counts;
        public NativeBitArray Blocked;
        public NativeArray<byte> FriendlyPassFactionIds;
    }

    public struct PathPoolComponent : IComponentData
    {
        public NativeList<int2> Cells;
    }

    public struct DynamicOccupancyComponent : IComponentData
    {
        public int GridSize;
        public NativeBitArray Occupied;
    }
}
