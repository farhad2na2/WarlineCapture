using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    // Session-owned memory has no actor Entity handle: hidden despawn cannot
    // erase the last legitimately observed contact or reveal its hidden death.
    public struct SkirmishContactLedgerElement : IBufferElementData
    {
        public FixedString64Bytes ContactStableObjectId;
        public byte ContactFactionId;
        public byte ObserverFactionId;
        public SkirmishContactSight Sight;
        public int2 LastSeenCell;
        public float3 LastSeenPosition;
        public int LastSeenHealth;
        public int LastSeenMax;
        public float LastObservedTimeSeconds;
        public FixedString64Bytes ObservationSourceStableObjectId;
        public int ScanRequestId;
        public int ObservationFrame;
        public byte ObservationKind;
    }

    // Guards native replay/reused session entities without changing session layout.
    public struct SkirmishContactLedgerStateComponent : IComponentData
    {
        public FixedString64Bytes SessionId;
    }
}
