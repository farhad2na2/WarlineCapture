using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum ArmorBreakFailure : byte { None, Integrity, AirDefenseLost, LauncherLost, AircraftLost, CommandSquadLost, ReliefLost, FuelReserveLost, Deadline, ArmorLost, MasteryLost }
    public enum ArmorBreakMemberKind : byte { AirDefense, Launcher, Radar, Armor, Infantry, Aircraft, Battery, HostileArmor, HostileCommand, HostileAir, Relief }
    public struct CampaignMissionArmorBreakState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public Entity AirDefense, Launcher, Radar, Aircraft, Battery, FuelReserve;
        public float3 CoverageCenter, AssaultApproach, AuthorityCenter, ReliefCenter;
        public float CoverageRadius, AuthorityRadius, ReliefRadius;
        public int ElapsedMilliseconds, RecoveryMilliseconds, FuelSpawnRequestId, LauncherShots, AircraftShotBaseline;
        public float LowestUsableFuel;
        public byte Initialized, Ready, CoverageReady, AirCleared, BatteryDisabled, ArmoredThreatsCleared, AircraftUsed, AircraftAttackOrdered, ArmorApproached, CommandDisabled, AuthorityRecovered, ReliefLost, FuelSpent, CloseCompleted;
        public ArmorBreakFailure Failure;
    }
    public struct CampaignMissionArmorBreakAircraftStandoff : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public float Padding;
    }
    [InternalBufferCapacity(32)]
    public struct CampaignMissionArmorBreakMember : IBufferElementData
    {
        public Entity Entity;
        public ArmorBreakMemberKind Kind;
        public byte HealthInitialized, Dead;
    }
}
