using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum OperationsReconPhase : byte { Preparing, Playing, Terminal }
    public enum OperationsReconOutcome : byte { None, Victory, Partial, Defeat, Withdraw }
    public enum OperationsReconAction : byte { Scan, RecoverEvidence, CancelInteraction, Conclude, Withdraw }

    // Session-owned presentation state. Checkpoints resume into a fresh recap, never
    // replay a saved half-tour or advance the mission clock during introduction.
    public struct OperationsReconIntroduction : IComponentData
    {
        public byte Stage; // 0 briefing, 1..5 public-objective tour, 6 player control
        public byte Resumed;
        public double NextStageAt;
    }

    public struct OperationsReconMemberComponent : IComponentData
    {
        public Entity Session;
        public int StableIndex;
    }

    // Retained even when combat deletes a unit; checkpoint restore must not respawn it.
    public struct OperationsReconSpawnRecord : IBufferElementData
    {
        public Entity Unit;
        public int StableIndex;
    }

    public struct OperationsReconReserveComponent : IComponentData
    {
        public Entity Session;
        public byte Wave;
        public byte Released;
    }

    public struct OperationsReconWaveComponent : IComponentData
    {
        public float WarningSeconds;
        public float EvidenceWarningSeconds;
        public float WaveAReleaseAt;
        public float WaveBReleaseAt;
        public byte WaveAAnnounced;
        public byte WaveBAnnounced;
    }

    public struct OperationsReconPatrolComponent : IComponentData
    {
        public Entity Session;
        public float3 Offset;
        public int Waypoint;
        public float NextOrderAt;
        public byte Issued;
    }

    [InternalBufferCapacity(4)]
    public struct OperationsReconPatrolWaypoint : IBufferElementData
    {
        public float3 Position;
    }

    public struct OperationsReconMissionComponent : IComponentData
    {
        public FixedString64Bytes SessionId;
        public uint Seed;
        public OperationsReconPhase Phase;
        public OperationsReconOutcome Outcome;
        public float ElapsedSeconds;
        public float DeadlineSeconds;
        public float ScanSeconds;
        public float EvidenceSeconds;
        public float3 ExitPosition;
        public float ExitRadius;
        public int CompletedScans;
        public int SurvivingInfantry;
        public int InfantryAtExit;
        public byte PartialAvailable;
        public byte ReconLostBeforeScansComplete;
        public byte MasteryCompleted;
        // Trigger latches are distinct from wave spawn/arrival state.
        public byte FirstScanWaveTriggered;
        public float3 FirstScanPosition;
        public byte EvidenceWaveTriggered;
    }

    [InternalBufferCapacity(3)]
    public struct OperationsReconSiteElement : IBufferElementData
    {
        public FixedString64Bytes RoleId;
        public float3 Position;
        public float Radius;
        public Entity Actor;
        public float ChannelSeconds;
        public byte Completed;
    }

    public struct OperationsReconEvidenceComponent : IComponentData
    {
        public float3 Position;
        public Entity Carrier;
        public Entity Actor;
        public float ChannelSeconds;
        public byte Recovered;
    }

    // Stable original roster remains after combat deletes an entity. Replacements cannot
    // manufacture survivors, and dead/destroyed original recon still affects mastery.
    [InternalBufferCapacity(16)]
    public struct OperationsReconRosterElement : IBufferElementData
    {
        public Entity Unit;
        public byte Recon;
    }

    [InternalBufferCapacity(8)]
    public struct OperationsReconActionElement : IBufferElementData
    {
        public FixedString64Bytes SessionId;
        public OperationsReconAction Action;
        public Entity Actor;
        public int SiteIndex;
    }
}
