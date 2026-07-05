using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Components
{
    public struct UnitCombat : IComponentData
    {
        public int AggroRangeCells;
        public float ChaseBreakDistance; // world units
        public byte CanAttack;
        public byte AutoEngage;
    }

    public enum ThreatDetectionKind : byte
    {
        None = 0,
        Ground = 1,
        Air = 2
    }

    public struct ThreatDetector : IComponentData
    {
        public byte Kind;
        public int RadiusCells;
    }

    public struct UnitHealth : IComponentData
    {
        public int Current;
        public int Max;
    }

    public struct UnitAttack : IComponentData
    {
        public float Range; // world units
        public float CooldownSeconds;
        public int Damage;
        public float4 TraceColor;
        public float TraceWidth;
        public float TraceScrollSpeed;
        public float TraceDashDensity;
        public float TraceVisibleSeconds;
        public int TracerEveryNthShot; // 0/1 = tracer on every shot
    }

    public struct UnitAttackTraceOriginPattern : IComponentData
    {
        public byte OriginCount;
        public float LateralOffset;
        public float TargetLateralOffset;
    }

    public struct UnitAttackImpactVfxReference : IComponentData
    {
        public UnityObjectRef<GameObject> Prefab;
    }

    public struct UnitMuzzleFlashVfxReference : IComponentData
    {
        public UnityObjectRef<GameObject> Prefab;
        public float HeightOffset;
        public float ForwardOffset;
    }

    public enum UnitAttackVfxRequestKind : byte
    {
        None = 0,
        MuzzleFlash = 1,
        Impact = 2
    }

    public struct UnitAttackVfxRequest : IComponentData
    {
        public byte Kind;
        public Entity Source;
        public Entity Target;
        public float3 SourcePosition;
        public float3 TargetPosition;
        public UnityObjectRef<GameObject> Prefab;
        public float3 PlaybackPosition;
        public quaternion PlaybackRotation;
        public float3 SideRight;
        public byte OriginCount;
        public float LateralOffset;
    }

    public enum CombatGameObjectVfxRequestKind : byte
    {
        None = 0,
        Play = 1,
        TimedLoop = 2
    }

    public struct CombatGameObjectVfxRequest : IComponentData
    {
        public byte Kind;
        public UnityObjectRef<GameObject> Prefab;
        public UnityObjectRef<GameObject> FallbackPrefab;
        public float3 Position;
        public quaternion Rotation;
        public float EmitSeconds;
        public float ActiveSeconds;
    }

    public struct UnitAttackCooldownComponent : IComponentData
    {
        public float CooldownRemaining;
    }

    public struct UnitAttackTraceComponent : IComponentData
    {
        public float TimeRemaining;
        public float Phase;
        public int ShotCounter;
    }

    public struct EngageTarget : IComponentData
    {
        public Entity Target;
        public int2 Cell; // last known target cell
        public Unity.Mathematics.float3 Position; // last known target world position
        public byte IsCommanded;
    }

    public struct SelectedUnitDebugFireState : IComponentData
    {
        public Entity Target;
        public Entity PreviousTarget;
        public int2 PreviousCell;
        public Unity.Mathematics.float3 PreviousPosition;
        public byte PreviousIsCommanded;
        public byte HadPreviousTarget;
    }

    public struct DebugFireTargetTag : IComponentData
    {
        public Entity Source;
    }

    public struct BaseBreachOrder : IComponentData
    {
        public const byte StageAttackingBreach = 0;
        public const byte StageMovingToEnemyBreach = 1;
        public const byte StageMovingToFinalTarget = 2;

        public Entity FinalTarget;
        public int2 FinalCell;
        public Unity.Mathematics.float3 FinalPosition;
        public Entity BreachTarget;
        public int2 BreachCell;
        public Unity.Mathematics.float3 BreachPosition;
        public byte Stage;
        public byte IsCommanded;
    }

    public struct RecentAttacker : IComponentData
    {
        public Entity Attacker;
        public int2 Cell;
        public Unity.Mathematics.float3 Position;
    }

    public struct UnitTurretReference : IComponentData
    {
        public Entity Turret;
    }

    public struct UnitRespawnPrefab : IComponentData
    {
        public Entity Prefab;
    }

    public struct UnitSourcePrefabKey : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public struct UnitDisplayInfo : IComponentData
    {
        public FixedString64Bytes Name;
        public FixedString128Bytes Description;
    }

    public struct UnitResourceHauler : IComponentData
    {
        public int BarrelCapacity;
        public float FillDurationSeconds;
        public float UnloadDurationSeconds;
        public float CargoOilBarrels;
        public float CargoFuelBarrels;
    }

    public struct FuelLogisticsOilHaulerTag : IComponentData
    {
    }

    public struct FuelLogisticsFuelHaulerTag : IComponentData
    {
    }

    public struct UnitResourceHaulOrder : IComponentData
    {
        public int SourceBuildingId;
        public int DestinationBuildingId;
        public int2 TargetCell;
        public float ActionEndsAt;
        public byte Phase;
        public byte ResourceKind;
    }

    public struct UnitResourceHaulReservation : IComponentData
    {
        public int SourceBuildingId;
        public int DestinationBuildingId;
        public float ReservedBarrels;
        public byte ResourceKind;
        public byte SourceReservationActive;
        public byte DestinationReservationActive;
        public uint ReservationId;
    }

    public struct RespawnQueueTag : IComponentData { }

    public struct RespawnQueueComponent : IComponentData
    {
        public uint RandomState;
        public int SpawnRadiusCells;
        public float RespawnDelaySeconds;
    }

    public struct RespawnFactionSpawnPoint : IBufferElementData
    {
        public byte FactionId;
        public int2 SpawnCell;
    }

    public struct RespawnRequest : IBufferElementData
    {
        public Entity Prefab;
        public byte FactionId;
        public int2 Goal;
        public double ReadyTime;
    }
}
