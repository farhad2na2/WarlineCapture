using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Skirmish.Contracts;

namespace Game.Components
{
    // Passive normal-input result. It grants no movement, fuel or production rights.
    // Recovery intentionally reconstructs no receipt; a fresh contact is required.
    public struct SkirmishPublicAircraftReturnReceipt : IComponentData
    {
        public Entity Session, Actor;
        public FixedString64Bytes Attempt, StableId;
        public ulong WorldSequence;
        public uint SetupHash, GroupId, Revision;
        public SkirmishRoleKind Role;
        public FixedString64Bytes PrefabKey;
        public int RequestId, Frame;
        public float2 Screen;
        public int2 HomeCell;
        public float3 HomePosition;
        public byte ScreenKnown;
    }
}
