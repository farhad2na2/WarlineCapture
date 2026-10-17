using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    // Player-visible normal command feedback, not authoritative objective facts.
    // Actual normal Attack feedback. AcceptedMembers may be less than SourceMembers.
    public struct SkirmishPublicPrecisionAttackFeedbackComponent : IComponentData
    {
        public Entity OriginalTarget; // Native-only provenance; never exposed in the public UI contract.
        public uint Revision, GroupId;
        public int Frame, RequestId, PublicTargetId, AcceptedMembers, SourceMembers;
        public float2 ScreenPosition;
        public byte Presented;
    }
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
