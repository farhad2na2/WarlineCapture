using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct CampaignMissionBreachDefinitionBlob
    {
        public byte Enabled;
        public FixedString128Bytes GateBuildingId, CoreBuildingId;
        public FixedString64Bytes ApproachAnchorId, GateAnchorId, CoreAnchorId, ArchiveAnchorId, SupportRoleId, CounterattackRoleId;
        public int GateHealth, CoreHealth, SecureHoldMilliseconds, DeadlineMilliseconds, CounterattackWarningMilliseconds;
        public float ArchiveRadius;
        public MissionCameraTourBlob CameraTour;
    }

    public struct CampaignMissionBreachState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion, GuidanceCompletedMask;
        public float3 ApproachCenter, GateCenter, CoreCenter, ArchiveCenter;
        public Entity Gate, Core, Support;
        public int DeadlineMilliseconds, SecureRequiredMilliseconds;
        public int GateRequestId, CoreRequestId, SecureHoldMilliseconds, CounterattackReleaseAtMilliseconds, PreparationMilliseconds;
        public byte Ready, GateInitialized, CoreInitialized, GateDestroyed, CoreDestroyed, CounterattackReleased;
        public byte SupportLost, ArchiveSecured, Contested, TimedOut;
    }

    [InternalBufferCapacity(32)]
    public struct CampaignMissionBreachMember : IBufferElementData
    {
        public Entity Entity;
        public byte Kind; // 0 rifle squad, 1 support APC, 2 garrison, 3 counterattack
        public byte HealthInitialized, Dead;
    }
}
