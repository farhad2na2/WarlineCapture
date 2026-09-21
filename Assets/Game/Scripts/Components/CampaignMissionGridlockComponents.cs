using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    // UI request only: replaying chapter story never launches or settles a mission.
    public struct GridlockChapterReplayRequest : IComponentData { public byte Pending; }
    public enum GridlockWorkStatus : byte { CrewMissing, ThreatNearby, Working, Clearing, Complete }
    public enum GridlockFailure : byte { None, FadiLost, WorkersLost, RiflesLost, VehicleLost, Deadline, Integrity }
    public enum GridlockMemberKind : byte { Rifle, Fadi, Worker, ReliefVehicle, Hostile, Counterattack }

    public struct CampaignMissionGridlockDefinitionBlob
    {
        public byte Enabled;
        public FixedString64Bytes SiteAAnchorId, SiteBAnchorId, HospitalAnchorId, ObstructionAAnchorId, ObstructionBAnchorId;
        public FixedString64Bytes FadiRoleId, WorkerRoleId, VehicleRoleId, CounterattackRoleId;
        public FixedString128Bytes ObstructionBuildingId;
        public int WorkMilliseconds, HoldMilliseconds, DeadlineMilliseconds, WarningMilliseconds;
        public float WorkRadius, ThreatRadius, HospitalRadius;
    }

    public struct CampaignMissionGridlockState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public float3 HospitalCenter;
        public Entity Vehicle;
        public int PreparationMilliseconds, ElapsedMilliseconds, HoldMilliseconds, CounterattackReleaseAtMilliseconds;
        public byte Ready, RouteConnected, VehicleArrived, Delivered, RouteContested, CounterattackWarned;
        public byte LivingFadi, LivingWorkers, LivingRifles, LivingVehicle;
        public GridlockFailure Failure;
    }

    [InternalBufferCapacity(2)]
    public struct CampaignMissionGridlockWorkSite : IBufferElementData
    {
        public float3 Center;
        public int2 ObstructionOrigin;
        public Entity Obstruction;
        public int SpawnRequestId, BuildingRuntimeId, WorkMilliseconds;
        public byte Initialized, ClearRequested, Complete, Contested;
        public GridlockWorkStatus Status;
    }

    [InternalBufferCapacity(24)]
    public struct CampaignMissionGridlockMember : IBufferElementData
    {
        public Entity Entity;
        public GridlockMemberKind Kind;
        public byte HealthInitialized, Dead;
    }
}
