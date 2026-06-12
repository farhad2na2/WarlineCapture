using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

public struct Faction : IComponentData
{
    public byte Id; // 0 = neutral, 1 = player, 2+ = hostile/AI factions.
}

public struct FactionEconomy : IComponentData
{
    public byte FactionId;
    public int Money;
    public float Oil;
    public float Fuel;
    public float OilIncomeRate;
    public float FuelIncomeRate;
    public float LastSellTime;
    public float LastLogTime;
}

public struct FactionEconomyPolicy : IComponentData
{
    public byte Enabled;
    public float IncomeMultiplier;
    public int OilSellPrice;
    public int FuelSellPrice;
    public float SellIntervalSeconds;
}

public struct AIBuildPlan : IComponentData
{
    public byte FactionId;
    public byte Enabled;
    public int NextBuildIndex;
    public int2 BaseCenterCell;
    public float BuildIntervalSeconds;
    public float LastBuildTime;
    public float LastLogTime;
}

public struct AIBuildPlanEntry : IBufferElementData
{
    public FixedString64Bytes BuildingId;
}

public struct AIProductionPlan : IComponentData
{
    public byte FactionId;
    public byte Enabled;
    public int NextUnitIndex;
    public int TargetProducedUnits;
    public int MaxQueuedUnits;
    public float UnitProductionIntervalSeconds;
    public float LastProductionTime;
    public float LastLogTime;
}

public struct AIProductionPlanEntry : IBufferElementData
{
    public FixedString64Bytes UnitId;
}

public enum AISquadPurpose : byte
{
    Attack = 0,
    Defend = 1,
    Scout = 2,
    Harass = 3
}

public enum AITargetKind : byte
{
    None = 0,
    Unit = 1,
    Building = 2,
    Threat = 3
}

public struct AITargetPrioritySetting : IComponentData
{
    public byte FactionId;
    public byte Priority;
}

public struct AISquadPlan : IComponentData
{
    public byte FactionId;
    public byte Enabled;
    public int MinUnits;
    public int MaxUnits;
    public int MaxActiveSquads;
    public int NextSquadId;
    public float LastLogTime;
}

public struct AISquad : IComponentData
{
    public int SquadId;
    public byte FactionId;
    public byte Purpose;
    public byte TargetFactionId;
    public byte TargetKind;
    public Entity TargetEntity;
    public int2 RallyCell;
    public int2 TargetCell;
    public int TargetScore;
    public int MinUnits;
    public int MaxUnits;
    public float LastOrderTime;
    public float LastLogTime;
}

public struct AISquadMember : IComponentData
{
    public Entity Squad;
    public int SquadId;
}

public struct AISquadUnit : IBufferElementData
{
    public Entity Unit;
}

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

public sealed class UnitAttackImpactVfxReference : IComponentData
{
    public GameObject Prefab;
}

public sealed class UnitMuzzleFlashVfxReference : IComponentData
{
    public GameObject Prefab;
    public float HeightOffset;
}

public enum GroundMissileLauncherPhase : byte
{
    Idle = 0,
    Preparing = 1,
    Launching = 2,
    Recovering = 3,
    Reloading = 4
}

public static class GroundMissileLauncherTiming
{
    public const float PostOpenLaunchDelaySeconds = 1f;
    public const float PostLaunchHoldSeconds = 1f;

    public static float PrepareAndHoldSeconds(float prepareSeconds)
    {
        return math.max(0.01f, prepareSeconds) + PostOpenLaunchDelaySeconds;
    }

    public static float FullAttackCycleSeconds(float prepareSeconds, float reloadSeconds)
    {
        return PrepareAndHoldSeconds(prepareSeconds) + PostLaunchHoldSeconds + math.max(0.01f, reloadSeconds);
    }
}

public struct GroundMissileLauncherComponent : IComponentData
{
    public float MinRange;
    public float MaxRange;
    public float PrepareSeconds;
    public float ReloadSeconds;
    public float BatteryElevatedAngleDegrees;
    public float RocketSpeed;
    public float ArcHeight;
    public float DamageRadius;
    public int Damage;
}

public struct GroundMissileLauncherStateComponent : IComponentData
{
    public byte Phase;
    public Entity TargetEntity;
    public int2 TargetCell;
    public float3 TargetWorldPosition;
    public float Timer;
    public int SelectedRocketSlot;
}

public struct GroundMissileInFlightComponent : IComponentData
{
    public Entity TargetEntity;
    public int2 TargetCell;
    public float3 TargetWorldPosition;
}

public struct GroundMissileLauncherVisualReferenceComponent : IComponentData
{
    public Entity Battery;
    public Entity SmokeSpawn;
    public quaternion BatteryDefaultLocalRotation;
    public float3 BatteryDefaultLocalPosition;
}

public struct GroundMissileLauncherRocketVisualComponent : IBufferElementData
{
    public Entity Rocket;
    public int SlotIndex;
    public float3 InitialLocalPosition;
    public quaternion InitialLocalRotation;
    public float InitialLocalScale;
}

public struct GroundMissileFlyingRocketVisualComponent : IComponentData
{
    public Entity Launcher;
    public Entity OriginalParent;
    public int SlotIndex;
    public float3 InitialLocalPosition;
    public quaternion InitialLocalRotation;
    public float InitialLocalScale;
    public float3 StartPosition;
    public float3 TargetPosition;
    public float3 LaunchDirection;
    public float ElapsedSeconds;
    public float DurationSeconds;
    public float ArcHeight;
}

public sealed class GroundMissileLauncherVfxReferenceComponent : IComponentData
{
    public GameObject LauncherBackfirePrefab;
    public GameObject RocketTrailPrefab;
    public GameObject ImpactExplosionPrefab;
    public GameObject ImpactSmokePrefab;
}

public struct GroundMissileProjectileComponent : IComponentData
{
    public Entity Source;
    public Entity TargetEntity;
    public int2 TargetCell;
    public float3 StartPosition;
    public float3 TargetPosition;
    public float ElapsedSeconds;
    public float DurationSeconds;
    public float ArcHeight;
    public float DamageRadius;
    public int Damage;
    public byte FactionId;
    public byte Interceptable;
}

public struct GroundMissileProjectileTrailComponent : IComponentData
{
    public float TimeUntilNextTrail;
    public float TrailIntervalSeconds;
}

public struct GroundMissileImpactRequestComponent : IComponentData
{
    public Entity Source;
    public Entity TargetEntity;
    public int2 TargetCell;
    public float3 Position;
    public float DamageRadius;
    public int Damage;
    public byte FactionId;
}

public struct MissileInterceptionTargetComponent : IComponentData
{
    public Entity Source;
    public byte FactionId;
}

public struct MissileInterceptedComponent : IComponentData
{
    public Entity Interceptor;
}

public enum AirMissileLauncherPhase : byte
{
    Idle = 0,
    Tracking = 1,
    Locked = 2,
    Launching = 3,
    Reloading = 4
}

public enum AirMissileTargetKind : byte
{
    None = 0,
    EnemyAirUnit = 1,
    IncomingGroundMissile = 2
}

public enum AirDefenseSupportProviderKind : byte
{
    Radar = 1,
    Satellite = 2
}

public struct AirMissileLauncherComponent : IComponentData
{
    public float MinRange;
    public float BaseDetectionRange;
    public float MaxDetectionRange;
    public float AirTargetPriority;
    public float IncomingMissilePriority;
    public float TurretYawSpeedDegreesPerSecond;
    public float AimToleranceDegrees;
    public float LockSeconds;
    public float LaunchDelaySeconds;
    public float ReloadSeconds;
    public float MissileSpeed;
    public float MissileAcceleration;
    public float MissileTurnRateDegreesPerSecond;
    public float MissileLifetimeSeconds;
    public float ProximityFuseRadius;
    public int AirTargetDamage;
    public int IncomingMissileDamage;
    public float TrackingQuality;
    public float MaxSupportRangeBonus;
    public float MaxSupportTrackingBonus;
}

public struct AirMissileLauncherStateComponent : IComponentData
{
    public byte Phase;
    public Entity TargetEntity;
    public byte TargetKind;
    public float3 TargetWorldPosition;
    public float3 PredictedInterceptPosition;
    public float Timer;
    public int SelectedMissileSlot;
    public float EffectiveRange;
    public float EffectiveLockSeconds;
    public float EffectiveTrackingQuality;
    public float EffectiveTurnRateDegreesPerSecond;
}

public struct AirMissileLauncherTargetComponent : IComponentData
{
    public Entity Target;
    public byte TargetKind;
    public float3 TargetWorldPosition;
    public float3 TargetVelocity;
    public float3 PredictedInterceptPosition;
    public float Score;
}

public struct AirMissileLauncherVisualReferenceComponent : IComponentData
{
    public Entity Turret;
    public Entity LaunchSpawn;
    public quaternion TurretDefaultLocalRotation;
    public float3 TurretDefaultLocalPosition;
}

public struct AirMissileLauncherMissileVisualComponent : IBufferElementData
{
    public Entity Missile;
    public int SlotIndex;
    public float3 InitialLocalPosition;
    public quaternion InitialLocalRotation;
    public float InitialLocalScale;
}

public sealed class AirMissileLauncherVfxReferenceComponent : IComponentData
{
    public GameObject MissileVisualPrefab;
    public GameObject LaunchFlashPrefab;
    public GameObject LaunchSmokePrefab;
    public GameObject MissileTrailPrefab;
    public GameObject AirburstExplosionPrefab;
    public GameObject AirTargetImpactPrefab;
    public GameObject InterceptExplosionPrefab;
}

public struct AirDefenseSupportProviderComponent : IComponentData
{
    public byte Kind;
    public int Level;
    public float SupportRadius;
    public float RangeBonus;
    public float LockTimeMultiplier;
    public float TrackingBonus;
    public float TurnRateBonus;
}

public struct AirDefenseSupportLinkComponent : IComponentData
{
    public float RangeBonus;
    public float LockTimeMultiplier;
    public float TrackingBonus;
    public float TurnRateBonus;
    public Entity RadarProvider;
    public Entity SatelliteProvider;
}

public struct AirMissileProjectileComponent : IComponentData
{
    public Entity Source;
    public Entity Target;
    public byte TargetKind;
    public byte FactionId;
    public float3 Velocity;
    public float Speed;
    public float Acceleration;
    public float TurnRateDegreesPerSecond;
    public float LifetimeSeconds;
    public float ProximityFuseRadius;
    public float ElapsedSeconds;
    public int Damage;
    public float TrackingQuality;
}

public struct AirMissileProjectileTrailComponent : IComponentData
{
    public float TimeUntilNextTrail;
    public float TrailIntervalSeconds;
}

public struct AirMissileFlyingVisualComponent : IComponentData
{
    public Entity Launcher;
    public Entity OriginalParent;
    public int SlotIndex;
    public float3 InitialLocalPosition;
    public quaternion InitialLocalRotation;
    public float InitialLocalScale;
}

public struct AirMissileImpactRequestComponent : IComponentData
{
    public Entity Source;
    public Entity Target;
    public byte TargetKind;
    public byte FactionId;
    public float3 Position;
    public int Damage;
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

public struct RuntimeBuildingCombatTag : IComponentData { }

public struct RuntimeBuildingCombatInfo : IComponentData
{
    public int RuntimeBuildingId;
    public byte OwnerFactionId;
    public int2 OriginCell;
    public int2 FootprintCells;
    public byte IsWall;
    public byte IsGate;
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

public struct UnitResourceHaulOrder : IComponentData
{
    public int SourceBuildingId;
    public int DestinationBuildingId;
    public int2 TargetCell;
    public float ActionEndsAt;
    public byte Phase;
    public byte ResourceKind;
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
