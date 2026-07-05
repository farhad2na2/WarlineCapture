using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct FocusedUnitUiReadModelComponent : IComponentData
    {
        public Entity FocusedUnit;
        public byte HasFocusedUnit;
        public byte OwnedByPlayer;
        public byte IsVehicle;
        public byte CanAttack;
        public byte CanHold;
        public int HoldDisabledReason;
        public byte CanStop;
        public int StopDisabledReason;
        public byte CanScan;
        public int ScanDisabledReason;
        public uint CommandStateVersion;
        public byte HasHealth;
        public int HealthCurrent;
        public int HealthMax;
        public byte HasCapacity;
        public int CapacityCurrent;
        public int CapacityMax;
        public float CapacityProgress01;
        public int PassengerCount;
        public int TransportPassengerCapacity;
        public int TransportSoldierPassengerCount;
        public int TransportSoldierPassengerCapacity;
        public int TransportVehiclePassengerCount;
        public int TransportVehiclePassengerCapacity;
        public byte HasResourceCargo;
        public int ResourceCargoOilBarrels;
        public int ResourceCargoFuelBarrels;
        public int ResourceCargoCapacity;
        public int Status;
        public FixedString64Bytes Label;
        public FixedString128Bytes Description;
        public FixedString32Bytes HealthText;
        public byte HasWorldPosition;
        public float3 WorldPosition;
        public byte HasPortraitPose;
        public float3 PortraitWorldPosition;
        public float3 PortraitForward;
    }

    public struct FocusedUnitPassengerUiReadModelElement : IBufferElementData
    {
        public Entity Passenger;
        public FixedString64Bytes DisplayName;
        public int HealthCurrent;
        public int HealthMax;
    }
}
