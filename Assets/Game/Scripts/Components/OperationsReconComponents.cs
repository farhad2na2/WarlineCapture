using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum OperationsReconPhase : byte { Preparing, Playing, Terminal }
    public enum OperationsReconOutcome : byte { None, Victory, Partial, Defeat, Withdraw }
    public enum OperationsReconAction : byte { Scan, RecoverEvidence, CancelInteraction, Conclude, Withdraw }

    public struct OperationsReconMissionComponent : IComponentData
    {
        public FixedString64Bytes SessionId;
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
