using Game.Components;
using Game.Configs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Authoring
{
    public partial class UnitGridAuthoring
    {
        private partial class UnitGridBaker
        {
            private void AddAirMissileLauncherComponents(UnitGridAuthoring authoring, Entity entity)
            {
                AirMissileLauncherConfig missileConfig = authoring.AirMissileLauncherConfig;
                if (missileConfig == null)
                    return;

                DependsOn(missileConfig);
                AddComponent(entity, new AirMissileLauncherComponent
                {
                    MinRange = missileConfig.MinRange,
                    BaseDetectionRange = missileConfig.BaseDetectionRange,
                    MaxDetectionRange = missileConfig.MaxDetectionRange,
                    AirTargetPriority = missileConfig.AirTargetPriority,
                    IncomingMissilePriority = missileConfig.IncomingMissilePriority,
                    TurretYawSpeedDegreesPerSecond = missileConfig.TurretYawSpeedDegreesPerSecond,
                    AimToleranceDegrees = missileConfig.AimToleranceDegrees,
                    LockSeconds = missileConfig.LockSeconds,
                    LaunchDelaySeconds = missileConfig.LaunchDelaySeconds,
                    ReloadSeconds = missileConfig.ReloadSeconds,
                    MissileSpeed = missileConfig.MissileSpeed,
                    MissileAcceleration = missileConfig.MissileAcceleration,
                    MissileTurnRateDegreesPerSecond = missileConfig.MissileTurnRateDegreesPerSecond,
                    MissileLifetimeSeconds = missileConfig.MissileLifetimeSeconds,
                    ProximityFuseRadius = missileConfig.ProximityFuseRadius,
                    AirTargetDamage = missileConfig.AirTargetDamage,
                    IncomingMissileDamage = missileConfig.IncomingMissileDamage,
                    TrackingQuality = missileConfig.TrackingQuality,
                    MaxSupportRangeBonus = missileConfig.MaxSupportRangeBonus,
                    MaxSupportTrackingBonus = missileConfig.MaxSupportTrackingBonus
                });
                AddComponent(entity, new AirMissileLauncherStateComponent
                {
                    Phase = (byte)AirMissileLauncherPhase.Idle,
                    TargetEntity = Entity.Null,
                    TargetKind = (byte)AirMissileTargetKind.None,
                    TargetWorldPosition = float3.zero,
                    PredictedInterceptPosition = float3.zero,
                    Timer = 0f,
                    SelectedMissileSlot = -1,
                    EffectiveRange = missileConfig.BaseDetectionRange,
                    EffectiveLockSeconds = missileConfig.LockSeconds,
                    EffectiveTrackingQuality = missileConfig.TrackingQuality,
                    EffectiveTurnRateDegreesPerSecond = missileConfig.MissileTurnRateDegreesPerSecond
                });
                AddComponent(entity, new AirDefenseSupportLinkComponent
                {
                    RangeBonus = 0f,
                    LockTimeMultiplier = 1f,
                    TrackingBonus = 0f,
                    TurnRateBonus = 0f,
                    RadarProvider = Entity.Null,
                    SatelliteProvider = Entity.Null
                });

                Transform turret = authoring.AirMissileLauncherTurret;
                if (turret != null)
                {
                    AddComponent(entity, new AirMissileLauncherVisualReferenceComponent
                    {
                        Turret = GetEntity(turret.gameObject, TransformUsageFlags.Dynamic),
                        LaunchSpawn = authoring.AirMissileLauncherLaunchSpawn != null
                            ? GetEntity(authoring.AirMissileLauncherLaunchSpawn.gameObject, TransformUsageFlags.Dynamic)
                            : Entity.Null,
                        TurretDefaultLocalRotation = turret.localRotation,
                        TurretDefaultLocalPosition = turret.localPosition
                    });
                }

                DynamicBuffer<AirMissileLauncherMissileVisualComponent> missiles =
                    AddBuffer<AirMissileLauncherMissileVisualComponent>(entity);
                IReadOnlyList<Transform> missileReferences = authoring.AirMissileLauncherMissiles;
                if (missileReferences != null)
                {
                    for (int i = 0; i < missileReferences.Count; i++)
                    {
                        Transform missile = missileReferences[i];
                        if (missile == null)
                            continue;

                        missiles.Add(new AirMissileLauncherMissileVisualComponent
                        {
                            Missile = GetEntity(missile.gameObject, TransformUsageFlags.Dynamic),
                            SlotIndex = i,
                            InitialLocalPosition = missile.localPosition,
                            InitialLocalRotation = missile.localRotation,
                            InitialLocalScale = missile.localScale.x
                        });
                    }
                }

                AddComponent(entity, new AirMissileLauncherVfxReferenceComponent
                {
                    MissileVisualPrefab = missileConfig.MissileVisualPrefab,
                    LaunchFlashPrefab = missileConfig.LaunchFlashPrefab,
                    LaunchSmokePrefab = missileConfig.LaunchSmokePrefab,
                    MissileTrailPrefab = missileConfig.MissileTrailPrefab,
                    AirburstExplosionPrefab = missileConfig.AirburstExplosionPrefab,
                    AirTargetImpactPrefab = missileConfig.AirTargetImpactPrefab,
                    InterceptExplosionPrefab = missileConfig.InterceptExplosionPrefab
                });
            }
        }
    }
}
