using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum MatchObjectiveState : byte
    {
        Active = 0,
        Complete = 1,
        Warning = 2,
        Blocked = 3,
        Failed = 4
    }

    public struct MatchObjectiveRuntimeStateComponent : IComponentData
    {
        public uint Version;
        public FixedString64Bytes MissionId;
        public float MatchStartedAt;
        public int ElapsedWholeSeconds;
        public byte MatchActive;
    }

    // Objectives share the UI shell boundary with the assistant read models.
    [InternalBufferCapacity(0)]
    public struct MatchObjectiveRuntimeElement : IBufferElementData
    {
        public int GoalId;
        public FixedString64Bytes ObjectiveId;
        public FixedString64Bytes OperationMapAnchorId;
        public MatchObjectiveState State;
        public byte Priority;
        public byte IsPrimary;
        public FixedString64Bytes Title;
        public FixedString128Bytes Body;
        public Entity TargetEntity;
        public int2 TargetCell;
        public float3 WorldPosition;
        public byte HasTargetCell;
        public byte HasWorldPosition;
        public byte ProtectsTarget;
    }
}
