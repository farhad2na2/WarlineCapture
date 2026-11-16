// Data-only union of the captured fuel and steering component containers.
using Unity.Mathematics;
namespace Game.Components {
public struct UnitGrid
    {
        public int2 Cell;
    }
public struct UnitMovementBehavior
    {
        public byte AllowIdleWander;
        public byte UsesVehicleMotion;
    }
public struct UnitFuelConsumption
    {
        public float GroundFuelPerCell;
        public float AirFuelPerCell;
        public byte Enabled;
    }
public struct UnitFuelConsumptionState
    {
        public int2 LastCell;
        public byte Initialized;
    }
public struct Faction
    {
        public byte Id; // 0 = neutral, 1 = player, 2+ = hostile/AI factions.
    }
public struct BuildingResourceStorageComponent
    {
        public int RuntimeBuildingId;
        public byte OwnerFactionId;
        public int OilStorageCapacity;
        public int FuelStorageCapacity;
        public float OilBarrelsPerDay;
        public float FuelBarrelsPerDay;
        public float StoredOilBarrels;
        public float StoredFuelBarrels;
        public float ReservedOilInboundBarrels;
        public float ReservedOilOutboundBarrels;
        public float ReservedFuelInboundBarrels;
        public float ReservedFuelOutboundBarrels;
        public float CivilianFuelReserveBarrels;
        public float ReceivedHaulerFuelBarrels; // Completed unloads; independent of production and spending.
        public uint Version;
    }
}

namespace Unity.Transforms { public struct LocalTransform { public float3 Position; public quaternion Rotation; public float Scale; } }
namespace Game.Components {
public struct UnitFootprint
    {
        public int2 Size;
    }

public struct UnitVehicleMovement
    {
        public float TurnSpeedDegrees;
        public float Acceleration;
        public float Braking;
        public float RearPivotOffset;
    }
public struct UnitVehicleKinematics
    {
        public float CurrentSpeed;
        public float StallSeconds;
    }
}
