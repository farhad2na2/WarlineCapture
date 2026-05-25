using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public enum M01AssistantCommandKind : byte
{
    SelectRuntimeEntity = 1,
    MoveSelectedUnitsToCell = 2,
    AttackRuntimeEntity = 3
}

public struct M01AssistantCommandQueueComponent : IComponentData
{
    public int LastRequestId;
}

public struct M01AssistantCommandRequestElement : IBufferElementData
{
    public M01AssistantCommandKind Kind;
    public int RequestId;
    public FixedString128Bytes RuntimeEntityId;
    public int2 TargetCell;
    public byte HasTargetCell;
}

public struct M01AssistantCommandResultElement : IBufferElementData
{
    public M01AssistantCommandKind Kind;
    public int RequestId;
    public byte Accepted;
    public int ReasonCode;
    public FixedString512Bytes Message;
}
