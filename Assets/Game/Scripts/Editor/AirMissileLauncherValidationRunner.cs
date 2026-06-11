#if UNITY_EDITOR
using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AirMissileLauncherValidationRunner
{
    private const string AirLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Air.prefab";
    private const string AirLauncherConfigPath = "Assets/Game/Configs/Weapons/AirMissileLauncher_Air_Config.asset";
    private const string AirLauncherUnitConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Air_Config.asset";

    public static void Run()
    {
        try
        {
            ValidateAssets();
            ValidateEcsFlow();
            ValidateGroundMissileInterception();
            Debug.Log("[AirMissileLauncherValidation] PASS");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AirMissileLauncherValidation] FAIL {ex}");
            Exit(1);
        }
    }

    private static void ValidateAssets()
    {
        AirMissileLauncherConfig config = AssetDatabase.LoadAssetAtPath<AirMissileLauncherConfig>(AirLauncherConfigPath);
        Require(config != null, $"Missing config at {AirLauncherConfigPath}.");
        Require(config.MinRange > 0f, "Min range must be positive.");
        Require(config.BaseDetectionRange > config.MinRange, "Base detection range must exceed min range.");
        Require(config.MaxDetectionRange > config.BaseDetectionRange, "Max detection range must exceed base range.");
        Require(config.LaunchFlashPrefab != null, "Launch flash prefab is not assigned.");
        Require(config.LaunchSmokePrefab != null, "Launch smoke prefab is not assigned.");
        Require(config.MissileTrailPrefab != null, "Missile trail prefab is not assigned.");
        Require(config.AirburstExplosionPrefab != null, "Airburst explosion prefab is not assigned.");
        Require(config.AirTargetImpactPrefab != null, "Air target impact prefab is not assigned.");
        Require(config.InterceptExplosionPrefab != null, "Intercept explosion prefab is not assigned.");

        UnitGridAuthoringConfig unitConfig = AssetDatabase.LoadAssetAtPath<UnitGridAuthoringConfig>(AirLauncherUnitConfigPath);
        Require(unitConfig != null, $"Missing unit config at {AirLauncherUnitConfigPath}.");
        Require(unitConfig.AirMissileLauncherConfig == config, "Unit config does not reference the air missile launcher config.");
        Require(unitConfig.CanAttack, "Air launcher unit config must be attack-capable for UI/read-model support.");
        Require(unitConfig.AllowAutoEngage, "Air launcher unit config must allow auto engage.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AirLauncherPrefabPath);
        Require(prefab != null, $"Missing prefab at {AirLauncherPrefabPath}.");
        UnitGridAuthoring authoring = prefab.GetComponent<UnitGridAuthoring>();
        Require(authoring != null, "Air launcher prefab is missing UnitGridAuthoring.");
        Require(authoring.AirMissileLauncherConfig == config, "Air launcher prefab does not resolve the air missile config.");

        SerializedObject serialized = new(authoring);
        Transform turret = GetReference<Transform>(serialized, "airMissileLauncherTurret");
        Require(turret != null, "Air launcher turret reference is not serialized.");
        Require(turret.name == "Missle_Launcher_Air", $"Unexpected turret reference '{turret.name}'.");

        SerializedProperty missiles = serialized.FindProperty("airMissileLauncherMissiles");
        Require(missiles != null, "Air launcher missile slot array is not serialized.");
        Require(missiles.arraySize == 12, $"Expected 12 missile slots, found {missiles.arraySize}.");
        Require(missiles.GetArrayElementAtIndex(0).objectReferenceValue?.name == "SM_Prop_Missle_Launcher_02_Missle_1", "First missile slot is not assigned to missile 1.");
        Require(missiles.GetArrayElementAtIndex(11).objectReferenceValue?.name == "SM_Prop_Missle_Launcher_02_Missle_12", "Last missile slot is not assigned to missile 12.");
    }

    private static void ValidateEcsFlow()
    {
        using World world = new("AirMissileLauncherValidationRunner");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity target = CreateAirTarget(em, FactionIdentitySystem.EnemyFactionId, new float3(30f, 12f, 0f));
        CreateSupportProvider(em, AirDefenseSupportProviderKind.Radar, new float3(8f, 0f, 0f), rangeBonus: 50f, lockMultiplier: 0.5f, trackingBonus: 0.2f, turnBonus: 40f);
        AddAirMissileVfxReference(em, launcher);

        SystemHandle supportSystem = world.CreateSystem<AirMissileLauncherSupportLinkSystem>();
        SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        SystemHandle trailSystem = world.CreateSystem<AirMissileProjectileTrailSystem>();
        SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
        SystemHandle impactSystem = world.CreateSystem<AirMissileImpactSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        supportSystem.Update(world.Unmanaged);
        acquisitionSystem.Update(world.Unmanaged);

        AirMissileLauncherStateComponent launcherState = em.GetComponentData<AirMissileLauncherStateComponent>(launcher);
        Require(launcherState.EffectiveRange > 120f, "Support link did not increase effective range.");
        Require(launcherState.EffectiveLockSeconds < 1f, "Support link did not reduce lock time.");
        Require(em.HasComponent<AirMissileLauncherTargetComponent>(launcher), "Launcher did not acquire hostile air target.");
        AirMissileLauncherTargetComponent acquiredTarget = em.GetComponentData<AirMissileLauncherTargetComponent>(launcher);
        Require(acquiredTarget.Target == target, "Launcher acquired the wrong target.");
        Require(acquiredTarget.TargetKind == (byte)AirMissileTargetKind.EnemyAirUnit, "Launcher target kind is not enemy air unit.");
        Require(!em.HasComponent<EngageTarget>(launcher), "Air launcher should not receive generic EngageTarget.");

        launcherState.Phase = (byte)AirMissileLauncherPhase.Locked;
        launcherState.TargetEntity = target;
        launcherState.TargetKind = (byte)AirMissileTargetKind.EnemyAirUnit;
        launcherState.TargetWorldPosition = new float3(30f, 12f, 0f);
        launcherState.PredictedInterceptPosition = new float3(30f, 12f, 0f);
        launcherState.Timer = 0f;
        em.SetComponentData(launcher, launcherState);

        world.SetTime(new TimeData(0.2d, 0.1f));
        fireControlSystem.Update(world.Unmanaged);

        using EntityQuery projectileQuery = em.CreateEntityQuery(typeof(AirMissileProjectileComponent));
        Require(projectileQuery.CalculateEntityCount() == 1, "Fire control did not create exactly one homing projectile.");
        Entity projectile = projectileQuery.GetSingletonEntity();
        Require(em.HasComponent<AirMissileProjectileTrailComponent>(projectile), "Configured missile trail did not attach to the homing projectile.");
        AirMissileProjectileComponent projectileData = em.GetComponentData<AirMissileProjectileComponent>(projectile);
        projectileData.ProximityFuseRadius = 500f;
        projectileData.Damage = 60;
        em.SetComponentData(projectile, projectileData);

        world.SetTime(new TimeData(0.3d, 0.1f));
        trailSystem.Update(world.Unmanaged);
        homingSystem.Update(world.Unmanaged);
        impactSystem.Update(world.Unmanaged);

        UnitHealth targetHealth = em.GetComponentData<UnitHealth>(target);
        Require(targetHealth.Current == 40, $"Expected target health 40 after impact, found {targetHealth.Current}.");
        Require(!em.Exists(projectile), "Projectile entity was not cleaned up after impact.");
    }

    private static void ValidateGroundMissileInterception()
    {
        using World world = new("AirMissileLauncherValidationRunner_GroundMissile");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), range: 120f);
        Entity incomingMissile = CreateIncomingGroundMissile(em, FactionIdentitySystem.EnemyFactionId, new float3(35f, 4f, 0f));

        SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
        SystemHandle impactSystem = world.CreateSystem<AirMissileImpactSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        acquisitionSystem.Update(world.Unmanaged);

        Require(em.HasComponent<AirMissileLauncherTargetComponent>(launcher), "Launcher did not acquire incoming ground missile.");
        AirMissileLauncherTargetComponent acquiredTarget = em.GetComponentData<AirMissileLauncherTargetComponent>(launcher);
        Require(acquiredTarget.Target == incomingMissile, "Launcher acquired the wrong incoming missile target.");
        Require(acquiredTarget.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile, "Launcher target kind is not incoming ground missile.");

        AirMissileLauncherStateComponent launcherState = em.GetComponentData<AirMissileLauncherStateComponent>(launcher);
        launcherState.Phase = (byte)AirMissileLauncherPhase.Locked;
        launcherState.TargetEntity = incomingMissile;
        launcherState.TargetKind = (byte)AirMissileTargetKind.IncomingGroundMissile;
        launcherState.TargetWorldPosition = new float3(35f, 4f, 0f);
        launcherState.PredictedInterceptPosition = new float3(35f, 4f, 0f);
        launcherState.Timer = 0f;
        em.SetComponentData(launcher, launcherState);

        world.SetTime(new TimeData(0.2d, 0.1f));
        fireControlSystem.Update(world.Unmanaged);

        using EntityQuery projectileQuery = em.CreateEntityQuery(typeof(AirMissileProjectileComponent));
        Require(projectileQuery.CalculateEntityCount() == 1, "Ground missile interception did not create exactly one homing projectile.");
        Entity projectile = projectileQuery.GetSingletonEntity();
        AirMissileProjectileComponent projectileData = em.GetComponentData<AirMissileProjectileComponent>(projectile);
        projectileData.ProximityFuseRadius = 500f;
        em.SetComponentData(projectile, projectileData);

        world.SetTime(new TimeData(0.3d, 0.1f));
        homingSystem.Update(world.Unmanaged);
        impactSystem.Update(world.Unmanaged);

        Require(!em.Exists(incomingMissile), "Incoming ground missile was not destroyed by interception.");
        Require(!em.Exists(projectile), "Interceptor projectile was not cleaned up after interception.");
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

    private static void AddAirMissileVfxReference(EntityManager em, Entity launcher)
    {
        AirMissileLauncherConfig config = AssetDatabase.LoadAssetAtPath<AirMissileLauncherConfig>(AirLauncherConfigPath);
        Require(config != null, $"Missing config at {AirLauncherConfigPath}.");
        em.AddComponentObject(launcher, new AirMissileLauncherVfxReferenceComponent
        {
            MissileVisualPrefab = config.MissileVisualPrefab,
            LaunchFlashPrefab = config.LaunchFlashPrefab,
            LaunchSmokePrefab = config.LaunchSmokePrefab,
            MissileTrailPrefab = config.MissileTrailPrefab,
            AirburstExplosionPrefab = config.AirburstExplosionPrefab,
            AirTargetImpactPrefab = config.AirTargetImpactPrefab,
            InterceptExplosionPrefab = config.InterceptExplosionPrefab
        });
    }

    private static Entity CreateAirTarget(EntityManager em, byte factionId, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitHealth),
            typeof(LocalTransform),
            typeof(UnitAirMovement));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitAirMovement
        {
            CruiseHeight = 12f,
            RunwayTaxiSpeed = 5f
        });
        return entity;
    }

    private static Entity CreateIncomingGroundMissile(EntityManager em, byte factionId, float3 position)
    {
        Entity entity = em.CreateEntity(
            typeof(LocalTransform),
            typeof(GroundMissileProjectileComponent),
            typeof(MissileInterceptionTargetComponent));
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new GroundMissileProjectileComponent
        {
            Source = Entity.Null,
            TargetEntity = Entity.Null,
            TargetCell = default,
            StartPosition = position,
            TargetPosition = position + new float3(20f, 0f, 0f),
            ElapsedSeconds = 0f,
            DurationSeconds = 5f,
            ArcHeight = 12f,
            DamageRadius = 8f,
            Damage = 120,
            FactionId = factionId,
            Interceptable = 1
        });
        em.SetComponentData(entity, new MissileInterceptionTargetComponent
        {
            Source = Entity.Null,
            FactionId = factionId
        });
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

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Require(property != null, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}
#endif
