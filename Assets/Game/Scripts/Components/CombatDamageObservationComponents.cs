using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public enum CombatDamageSourceKind : byte
    {
        Unknown = 0,
        DirectFire = 1,
        BuildingDefense = 2,
        GroundMissile = 3,
        AirMissile = 4,
        Explosion = 5
    }

    public struct CombatDamageObservationQueueComponent : IComponentData
    {
        public int LastEventId;
        public uint Version;
    }

    [InternalBufferCapacity(8)]
    public struct CombatDamageObservationElement : IBufferElementData
    {
        public int EventId;
        public int Frame;
        public Entity SourceEntity;
        public Entity TargetEntity;
        public CombatDamageSourceKind SourceKind;
        public int DamageApplied;
        public int TargetHealthAfter;
        public int TargetMaxHealth;
        public float ObservedAt;
        public float3 SourceWorldPosition;
        public float3 TargetWorldPosition;
    }
}
