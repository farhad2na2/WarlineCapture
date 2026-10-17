using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    // Player-visible normal command feedback, not authoritative objective facts.
    public struct SkirmishPublicOrderFeedbackComponent : IComponentData
    {
        public uint Revision,GroupId,NavigationGroupId;
        public RtsSelectionCommandIntentKind Kind;
        public int Frame,RequestId;
        public float2 ScreenPosition;
        public float3 WorldPosition;
        public byte Accepted,HasWorldPosition,Presented;
    }
}
