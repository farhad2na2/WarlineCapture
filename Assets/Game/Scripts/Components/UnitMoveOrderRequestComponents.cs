using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum UnitMoveOrderRequestKind : byte
    {
        GroupedManual,
        Immediate,
        TargetOnly,
        TargetPathOnly,
        ClearMovement
    }

    public struct UnitMoveOrderQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct UnitMoveOrderRequestElement : IBufferElementData
    {
        public int RequestId;
        public Entity Entity;
        public int2 Goal;
        public UnitMoveOrderRequestKind Kind;
        public int ResumeFrame;
        public int CurrentFrame;
        public byte IssueGroundPathNow;
        public byte UseGroundPathRetryCooldown;
    }

    public struct UnitMoveOrderResultElement : IBufferElementData
    {
        public int RequestId;
        public Entity Entity;
        public int2 Goal;
        public byte Issued;
        public int StructuralAdds;
        public int StructuralRemoves;
        public int PathRequests;
        public int StaggeredPathRequests;
        public int MaxStaggerDelayFrames;
        public int AirUnits;
        public int RejectionReasonCode;
    }
}
