#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class AirMissileLauncherRuntimeTests
{
    [Test]
    public void TargetAcquisition_TracksHostileAirAndIgnoresGroundUnit()
    {
        using var world = new World("AirMissileLauncherRuntimeTests_TargetAcquisition");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity hostileGround = CreateUnit(em, FactionIdentitySystem.EnemyFactionId, new float3(10f, 0f, 0f), air: false);
        Entity hostileAir = CreateUnit(em, FactionIdentitySystem.EnemyFactionId, new float3(30f, 12f, 0f), air: true);
        CreateUnit(em, FactionIdentitySystem.PlayerFactionId, new float3(5f, 10f, 0f), air: true);

        SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        acquisitionSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<AirMissileLauncherTargetComponent>(launcher));
        AirMissileLauncherTargetComponent target = em.GetComponentData<AirMissileLauncherTargetComponent>(launcher);
        Assert.AreEqual(hostileAir, target.Target);
        Assert.AreEqual((byte)AirMissileTargetKind.EnemyAirUnit, target.TargetKind);
        Assert.AreNotEqual(hostileGround, target.Target);
    }

    [Test]
    public void SupportLink_AppliesRadarAndSatelliteBonuses()
    {
        using var world = new World("AirMissileLauncherRuntimeTests_SupportLink");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity radar = CreateSupportProvider(em, AirDefenseSupportProviderKind.Radar, new float3(10f, 0f, 0f), rangeBonus: 40f, lockMultiplier: 0.75f, trackingBonus: 0.1f, turnBonus: 20f);
        Entity satellite = CreateSupportProvider(em, AirDefenseSupportProviderKind.Satellite, new float3(20f, 0f, 0f), rangeBonus: 70f, lockMultiplier: 0.65f, trackingBonus: 0.15f, turnBonus: 30f);

        SystemHandle supportSystem = world.CreateSystem<AirMissileLauncherSupportLinkSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        supportSystem.Update(world.Unmanaged);

        AirMissileLauncherStateComponent state = em.GetComponentData<AirMissileLauncherStateComponent>(launcher);
        Assert.AreEqual(230f, state.EffectiveRange, 0.001f);
        Assert.AreEqual(0.65f, state.EffectiveLockSeconds, 0.001f);
        Assert.AreEqual(1f, state.EffectiveTrackingQuality, 0.001f);
        Assert.AreEqual(170f, state.EffectiveTurnRateDegreesPerSecond, 0.001f);

        AirDefenseSupportLinkComponent link = em.GetComponentData<AirDefenseSupportLinkComponent>(launcher);
        Assert.AreEqual(radar, link.RadarProvider);
        Assert.AreEqual(satellite, link.SatelliteProvider);
    }

    [Test]
    public void TurretAim_RotatesTurretOnLocalYawTowardTarget()
    {
        using var world = new World("AirMissileLauncherRuntimeTests_TurretAim");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity turret = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(turret, LocalTransform.Identity);
        em.AddComponentData(launcher, new AirMissileLauncherVisualReferenceComponent
        {
            Turret = turret,
            LaunchSpawn = Entity.Null,
            TurretDefaultLocalPosition = float3.zero,
            TurretDefaultLocalRotation = quaternion.identity
        });
        em.AddComponentData(launcher, new AirMissileLauncherTargetComponent
        {
            Target = Entity.Null,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(10f, 0f, 0f),
            PredictedInterceptPosition = new float3(10f, 0f, 0f),
            Score = 1f
        });

        SystemHandle aimSystem = world.CreateSystem<AirMissileLauncherTurretAimSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        aimSystem.Update(world.Unmanaged);

        LocalTransform turretTransform = em.GetComponentData<LocalTransform>(turret);
        float3 forward = math.rotate(turretTransform.Rotation, new float3(0f, 0f, 1f));
        Assert.Greater(forward.x, 0.7f);
        Assert.AreEqual(0f, forward.y, 0.001f);
    }

    [Test]
    public void FireControl_LockedLauncherCreatesHomingProjectile()
    {
        using var world = new World("AirMissileLauncherRuntimeTests_FireControl");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity target = CreateUnit(em, FactionIdentitySystem.EnemyFactionId, new float3(30f, 10f, 0f), air: true);
        em.SetComponentData(launcher, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Locked,
            TargetEntity = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(30f, 10f, 0f),
            PredictedInterceptPosition = new float3(30f, 10f, 0f),
            Timer = 0f,
            EffectiveRange = 120f,
            EffectiveLockSeconds = 1f,
            EffectiveTrackingQuality = 0.75f,
            EffectiveTurnRateDegreesPerSecond = 120f
        });
        em.AddComponentData(launcher, new AirMissileLauncherTargetComponent
        {
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(30f, 10f, 0f),
            PredictedInterceptPosition = new float3(30f, 10f, 0f),
            Score = 25f
        });

        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        fireControlSystem.Update(world.Unmanaged);

        using EntityQuery projectileQuery = em.CreateEntityQuery(typeof(AirMissileProjectileComponent));
        Assert.AreEqual(1, projectileQuery.CalculateEntityCount());
        Assert.AreEqual((byte)AirMissileLauncherPhase.Reloading, em.GetComponentData<AirMissileLauncherStateComponent>(launcher).Phase);
    }

    [Test]
    public void FireControl_DebugTargetLaunchesImmediatelyWithoutTurretAimLock()
    {
        using var world = new World("AirMissileLauncherRuntimeTests_DebugFireControl");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity turret = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(turret, LocalTransform.Identity);
        em.AddComponentData(launcher, new AirMissileLauncherVisualReferenceComponent
        {
            Turret = turret,
            LaunchSpawn = Entity.Null,
            TurretDefaultLocalPosition = float3.zero,
            TurretDefaultLocalRotation = quaternion.identity
        });

        Entity target = CreateUnit(em, FactionIdentitySystem.EnemyFactionId, new float3(30f, 10f, 0f), air: true);
        em.AddComponentData(target, new DebugFireTargetTag { Source = launcher });
        em.SetComponentData(launcher, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Tracking,
            TargetEntity = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(30f, 10f, 0f),
            PredictedInterceptPosition = new float3(30f, 10f, 0f),
            Timer = 5f,
            EffectiveRange = 120f,
            EffectiveLockSeconds = 1f,
            EffectiveTrackingQuality = 0.75f,
            EffectiveTurnRateDegreesPerSecond = 120f
        });
        em.AddComponentData(launcher, new AirMissileLauncherTargetComponent
        {
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            TargetWorldPosition = new float3(30f, 10f, 0f),
            PredictedInterceptPosition = new float3(30f, 10f, 0f),
            Score = 1025f
        });

        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        fireControlSystem.Update(world.Unmanaged);

        using EntityQuery projectileQuery = em.CreateEntityQuery(typeof(AirMissileProjectileComponent));
        Assert.AreEqual(1, projectileQuery.CalculateEntityCount());
        Assert.AreEqual((byte)AirMissileLauncherPhase.Reloading, em.GetComponentData<AirMissileLauncherStateComponent>(launcher).Phase);
    }

    [Test]
    public void HomingImpact_DamagesAirTargetAndRemovesProjectile()
    {
        using var world = new World("AirMissileLauncherRuntimeTests_HomingImpact");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity target = CreateUnit(em, FactionIdentitySystem.EnemyFactionId, new float3(1f, 0f, 0f), air: true);
        Entity projectile = em.CreateEntity(
            typeof(LocalTransform),
            typeof(AirMissileProjectileComponent));
        em.SetComponentData(projectile, LocalTransform.FromPosition(float3.zero));
        em.SetComponentData(projectile, new AirMissileProjectileComponent
        {
            Source = launcher,
            Target = target,
            TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit,
            FactionId = FactionIdentitySystem.PlayerFactionId,
            Velocity = new float3(10f, 0f, 0f),
            Speed = 10f,
            Acceleration = 0f,
            TurnRateDegreesPerSecond = 180f,
            LifetimeSeconds = 5f,
            ProximityFuseRadius = 5f,
            ElapsedSeconds = 0f,
            Damage = 60,
            TrackingQuality = 1f
        });

        SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
        SystemHandle impactSystem = world.CreateSystem<AirMissileImpactSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        homingSystem.Update(world.Unmanaged);
        impactSystem.Update(world.Unmanaged);

        Assert.AreEqual(40, em.GetComponentData<UnitHealth>(target).Current);
        Assert.IsFalse(em.Exists(projectile));
    }

    private static Entity CreateLauncher(EntityManager em, float3 position, float range)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(AirMissileLauncherComponent),
            typeof(AirMissileLauncherStateComponent),
            typeof(AirDefenseSupportLinkComponent));
        em.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new AirMissileLauncherComponent
        {
            MinRange = 4f,
            BaseDetectionRange = range,
            MaxDetectionRange = 260f,
            AirTargetPriority = 25f,
            IncomingMissilePriority = 100f,
            TurretYawSpeedDegreesPerSecond = 900f,
            AimToleranceDegrees = 5f,
            LockSeconds = 1f,
            LaunchDelaySeconds = 0.1f,
            ReloadSeconds = 1.5f,
            MissileSpeed = 95f,
            MissileAcceleration = 0f,
            MissileTurnRateDegreesPerSecond = 120f,
            MissileLifetimeSeconds = 5f,
            ProximityFuseRadius = 4f,
            AirTargetDamage = 120,
            IncomingMissileDamage = 9999,
            TrackingQuality = 0.75f,
            MaxSupportRangeBonus = 180f,
            MaxSupportTrackingBonus = 0.3f
        });
        em.SetComponentData(entity, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetKind = (byte)AirMissileTargetKind.None,
            EffectiveRange = range,
            EffectiveLockSeconds = 1f,
            EffectiveTrackingQuality = 0.75f,
            EffectiveTurnRateDegreesPerSecond = 120f
        });
        em.SetComponentData(entity, new AirDefenseSupportLinkComponent
        {
            LockTimeMultiplier = 1f
        });
        return entity;
    }

    private static Entity CreateUnit(EntityManager em, byte factionId, float3 position, bool air)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        if (air)
        {
            em.AddComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 12f,
                RunwayTaxiSpeed = 5f
            });
        }

        return entity;
    }

    private static Entity CreateSupportProvider(
        EntityManager em,
        AirDefenseSupportProviderKind kind,
        float3 position,
        float rangeBonus,
        float lockMultiplier,
        float trackingBonus,
        float turnBonus)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(LocalTransform),
            typeof(AirDefenseSupportProviderComponent));
        em.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new AirDefenseSupportProviderComponent
        {
            Kind = (byte)kind,
            Level = 1,
            SupportRadius = 80f,
            RangeBonus = rangeBonus,
            LockTimeMultiplier = lockMultiplier,
            TrackingBonus = trackingBonus,
            TurnRateBonus = turnBonus
        });
        return entity;
    }
}
#endif
