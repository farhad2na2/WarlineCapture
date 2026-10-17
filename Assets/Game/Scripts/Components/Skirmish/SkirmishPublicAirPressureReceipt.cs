using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Game.Skirmish.Contracts;
namespace Game.Components
{
    // Ephemeral normal-input feedback; never a checkpoint completion token.
    public struct SkirmishPublicAirPressureReceipt : IComponentData
    {
        public Entity Actor,Session;
        public FixedString64Bytes Attempt,StableId,PrefabKey;
        public SkirmishRoleKind Role;
        public ulong World;
        public uint SetupHash,GroupId,Revision;
        public int RequestId,Frame;
        public float2 Screen;
        public int2 Goal;
        public float3 Position;
    }
}
