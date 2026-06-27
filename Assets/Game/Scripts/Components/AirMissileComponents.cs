using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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

public static class AirDefenseSupportTuning
{
    public const float RadarRangeBonus = 100f;
    public const float RadarLockTimeMultiplier = 0.5f;
    public const float RadarTrackingBonus = 0.2f;
    public const float RadarTurnRateBonus = 50f;

    public const float SatelliteRangeBonus = 120f;
    public const float SatelliteLockTimeMultiplier = 0.65f;
    public const float SatelliteTrackingBonus = 0.18f;
    public const float SatelliteTurnRateBonus = 50f;
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

public struct AirMissileLauncherVfxReferenceComponent : IComponentData
{
    public UnityObjectRef<GameObject> MissileVisualPrefab;
    public UnityObjectRef<GameObject> LaunchFlashPrefab;
    public UnityObjectRef<GameObject> LaunchSmokePrefab;
    public UnityObjectRef<GameObject> MissileTrailPrefab;
    public UnityObjectRef<GameObject> AirburstExplosionPrefab;
    public UnityObjectRef<GameObject> AirTargetImpactPrefab;
    public UnityObjectRef<GameObject> InterceptExplosionPrefab;
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
