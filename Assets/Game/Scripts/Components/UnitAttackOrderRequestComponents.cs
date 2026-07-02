using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum UnitAttackOrderRequestKind : byte
    {
        SelectedAttackTarget,
        SourceAttackTarget,
        SourceBaseBreachAttackTarget,
        DirectAttackTarget,
        RadarAttackTarget,
        ClearCommandedAttackOrder,
        ClearAccidentalAirSelectionMove
    }

    public struct UnitAttackOrderQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct UnitAttackOrderRequestElement : IBufferElementData
    {
        public int RequestId;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public Entity BreachTargetEntity;
        public int2 TargetCell;
        public int2 BreachCell;
        public float3 TargetPosition;
        public float3 BreachPosition;
        public UnitAttackOrderRequestKind Kind;
        public byte FactionId;
        public byte RequireAirTarget;
    }

    public struct UnitAttackOrderResultElement : IBufferElementData
    {
        public int RequestId;
        public Entity TargetEntity;
        public float3 TargetPosition;
        public int IssuedCount;
        public int ReasonCode;
        public byte Issued;
        public byte HasCommandResult;
        public byte Accepted;
        public FixedString64Bytes Message;
    }
}
