using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

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

public struct GroundMissileLauncherVfxReferenceComponent : IComponentData
{
    public UnityObjectRef<GameObject> LauncherBackfirePrefab;
    public UnityObjectRef<GameObject> RocketTrailPrefab;
    public UnityObjectRef<GameObject> ImpactExplosionPrefab;
    public UnityObjectRef<GameObject> ImpactSmokePrefab;
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
    public float VisualSeparation;
}
