using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum ThreatWarningSourceKind : byte { None = 0, ScoutReport = 1, GroundSensor = 2, VisualContact = 3, RadarPing = 4 }

    public struct ThreatWarningLedgerState : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public uint Version;
        public uint PresentationVersion;
        public uint AcknowledgedPresentationVersion;
        public int FocusElementIndex;
        public byte Active;
    }

    [InternalBufferCapacity(4)]
    public struct ThreatWarningRecord : IBufferElementData
    {
        public FixedString64Bytes ElementId;
        public FixedString64Bytes RouteId;
        public FixedString64Bytes ContactAnchorId;
        public float3 FocusPosition;
        public Entity ObservedTarget;
        public ThreatWarningSourceKind Source;
        public int ElementIndex;
        public int KnownVehicleCount; // -1 means unconfirmed, never an invented one-unit force.
        public int ObservedAtMilliseconds;
        public int FirstReportedAtMilliseconds;
        public byte ReadByPlayer;
        public byte AttentionEscalated;
        public int ContactAtMilliseconds;
        public int EtaSeconds;
        public byte HasFocus;
        public byte Stale;
        public byte ContactWindowOpen;
        public byte Critical;
        public byte Resolved;
    }

    [InternalBufferCapacity(16)]
    public struct ThreatWarningObservation : IBufferElementData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public int ElementIndex;
        public ThreatWarningSourceKind Source;
        public int KnownVehicleCount;
        public int ObservedAtMilliseconds;
        public float3 Position;
        public Entity Target;
        public byte HasPosition;
    }
}
