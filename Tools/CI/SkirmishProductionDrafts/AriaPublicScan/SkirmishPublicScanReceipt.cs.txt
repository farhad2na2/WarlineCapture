using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct SkirmishPublicScanReceipt : IComponentData
    {
        public Entity Session,Actor;
        public FixedString64Bytes Attempt,StableId;
        public ulong World;
        public uint SetupHash,GroupId,Revision;
        public int RequestId,Frame,RadiusCells;
        public int2 CenterCell;
        public float3 CenterWorld;
        public float2 Screen;
        public byte Completed;
    }
}
