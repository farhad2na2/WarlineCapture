using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Components
{
    // Optional supplemental checkpoint part: existing component layouts remain unchanged.
    [InternalBufferCapacity(2)]
    public struct SkirmishContactKnowledgeElement : IBufferElementData
    {
        public byte ObserverFactionId;
        public SkirmishContactSight Sight;
        public int2 LastSeenCell;
        public float3 LastSeenPosition;
        public int LastSeenHealth;
        public int LastSeenMax;
        public float LastObservedTimeSeconds;
        public Entity ObservationSource;
        public FixedString64Bytes ObservationSourceStableObjectId;
        public int ScanRequestId;
        public int ObservationFrame;
        // 1 direct acquisition, 2 native Scan, 3 explicit development full vision.
        public byte ObservationKind;
    }
}
