using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class BattleScenarioLabVisualPlayback : MonoBehaviour
{
    [SerializeField] private Camera scenarioCamera;
    [SerializeField] private Transform groundLauncherRoot;
    [SerializeField] private Transform airLauncherRoot;
    [SerializeField] private Transform radarRoot;
    [SerializeField] private Transform defendedTargetVisual;
    [SerializeField, Min(0.1f)] private float entityWaitTimeoutSeconds = 30f;

    private const string GroundLauncherKey = "Unit_Veh_Missle_Launcher_Ground";
    private const string AirLauncherKey = "Unit_Veh_Missle_Launcher_Air";
    private const string RadarKey = "Unit_Veh_Radar_Tank";
    private const string JetTargetKey = "Unit_Veh_Jet_01";
    private const string HelicopterTargetKey = "Unit_Veh_Helicopter_Attack";
    private const string DroneTargetKey = "Unit_Veh_Drone";
    private const float VisualInterceptProximityFuseRadius = 0.35f;
    private const float VisualAirTargetProximityFuseRadius = 4f;
    private const float VisualGroundMissileArcHeight = 8f;
    private const float ScenarioGroundMissileBaseFlightSeconds = 8f;
    private const float ScenarioAirBaseDetectionRange = 140f;
    private const float ScenarioAirMaxDetectionRange = 260f;
    private const float ScenarioAirMissileSpeed = 95f;
    private const float ScenarioAirMissileTurnRateDegreesPerSecond = 140f;
    private const float ScenarioAirTargetMissileTurnRateDegreesPerSecond = 170f;
    private const float ScenarioAirMissileLifetimeSeconds = 5f;
    private const float ScenarioAirLockSeconds = 0.9f;
    private const float ScenarioAirLaunchDelaySeconds = 0.1f;
    private const float ScenarioAirTrackingQuality = 0.75f;
    private const float ScenarioAirTargetTrackingQuality = 0.8f;

    private static readonly float3 AirLauncherPosition = new(0f, 0f, 0f);
    private static readonly float3 DefendedTargetPosition = new(-40f, 0f, 0f);
    private static readonly quaternion AirLauncherRotation = quaternion.RotateY(math.radians(90f));
    private static readonly quaternion GroundLauncherRotation = quaternion.RotateY(math.radians(-90f));

    private Coroutine playbackRoutine;

    public bool CanPlay(BattleScenarioDefinition definition)
    {
        if (definition == null)
            return true;

        BattleScenarioVariant[] variants = definition.ScenarioVariants;
        if (variants == null || variants.Length == 0)
            return string.Equals(definition.ScenarioId, BattleScenarioAd001Runner.ScenarioId, System.StringComparison.Ordinal);

        for (int i = 0; i < variants.Length; i++)
            if (CanPlayVariant(variants[i]))
                return true;

        return false;
    }

    public void Play(BattleScenarioDefinition definition, BattleScenarioVariant variant, BattleScenarioMetrics metrics)
    {
        if (!isActiveAndEnabled)
            return;

        StopPlaybackAndClear();

        playbackRoutine = StartCoroutine(PlayLiveEcsRoutine(definition, variant));
    }

    public void StopPlaybackAndClear()
    {
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
            ResetPreviousRun(world.EntityManager);
    }

    private IEnumerator PlayLiveEcsRoutine(BattleScenarioDefinition definition, BattleScenarioVariant variant)
    {
        BattleScenarioVariant resolvedVariant = ResolveVariant(definition, variant);
        if (IsAirTargetKind(resolvedVariant.IncomingThreatKind))
        {
            yield return PlayAirTargetLiveEcsRoutine(resolvedVariant);
            yield break;
        }

        yield return PlayGroundMissileLiveEcsRoutine(resolvedVariant);
    }

    private IEnumerator PlayGroundMissileLiveEcsRoutine(BattleScenarioVariant variant)
    {
        PositionGroundMissileAuthoringRoots(variant);
        SetCamera(new Vector3(112f, 50f, -88f), new Vector3(58f, 8f, 0f));

        World world = null;
        Entity airLauncherPrefab = Entity.Null;
        Entity groundLauncherPrefab = Entity.Null;
        Entity radarPrefab = Entity.Null;
        float waitStart = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
        {
            world = World.DefaultGameObjectInjectionWorld;
            if (world != null &&
                TryResolveUnitPrefab(world.EntityManager, AirLauncherKey, out airLauncherPrefab) &&
                TryResolveUnitPrefab(world.EntityManager, GroundLauncherKey, out groundLauncherPrefab))
            {
                TryResolveUnitPrefab(world.EntityManager, RadarKey, out radarPrefab);
                break;
            }

            yield return null;
        }

        if (world == null || airLauncherPrefab == Entity.Null || groundLauncherPrefab == Entity.Null)
        {
            Debug.LogError(
                "[BattleScenarioLab] Live ECS visual run could not resolve baked production launcher prefab entities " +
                $"within {entityWaitTimeoutSeconds:0.#}s. The scene must autoload the Scenario Lab prefab registry SubScene.");
            yield break;
        }

        EntityManager em = world.EntityManager;
        ResetPreviousRun(em);
        Entity airLauncher = InstantiateUnitPrefab(em, airLauncherPrefab);
        Entity groundLauncher = InstantiateUnitPrefab(em, groundLauncherPrefab);
        Entity radar = radarPrefab != Entity.Null ? InstantiateUnitPrefab(em, radarPrefab) : Entity.Null;
        ConfigureLiveScenario(em, variant, airLauncher, groundLauncher, radar);

        yield return CameraOpeningRoutine(em, variant, airLauncher, groundLauncher);
    }

    private IEnumerator PlayAirTargetLiveEcsRoutine(BattleScenarioVariant variant)
    {
        PositionAirTargetAuthoringRoots(variant);
        SetCamera(new Vector3(98f, 34f, -86f), new Vector3(46f, 11f, 0f));

        World world = null;
        Entity airLauncherPrefab = Entity.Null;
        Entity targetPrefab = Entity.Null;
        Entity radarPrefab = Entity.Null;
        string targetKey = ResolveAirTargetPrefabKey(variant);
        float waitStart = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - waitStart < entityWaitTimeoutSeconds)
        {
            world = World.DefaultGameObjectInjectionWorld;
            if (world != null &&
                TryResolveUnitPrefab(world.EntityManager, AirLauncherKey, out airLauncherPrefab) &&
                TryResolveUnitPrefab(world.EntityManager, targetKey, out targetPrefab))
            {
                TryResolveUnitPrefab(world.EntityManager, RadarKey, out radarPrefab);
                break;
            }

            yield return null;
        }

        if (world == null || airLauncherPrefab == Entity.Null || targetPrefab == Entity.Null)
        {
            Debug.LogError(
                "[BattleScenarioLab] Live ECS visual run could not resolve baked production air launcher/air target prefab entities " +
                $"within {entityWaitTimeoutSeconds:0.#}s. The scene must autoload the Scenario Lab prefab registry SubScene.");
            yield break;
        }

        EntityManager em = world.EntityManager;
        ResetPreviousRun(em);
        Entity airLauncher = InstantiateUnitPrefab(em, airLauncherPrefab);
        Entity target = InstantiateUnitPrefab(em, targetPrefab);
        Entity radar = radarPrefab != Entity.Null ? InstantiateUnitPrefab(em, radarPrefab) : Entity.Null;
        ConfigureLiveAirTargetScenario(em, variant, airLauncher, target, radar);

        yield return CameraAirTargetRoutine(em, variant, airLauncher, target);
    }

    private static BattleScenarioVariant ResolveVariant(BattleScenarioDefinition definition, BattleScenarioVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.VariantId))
            return variant;

        if (definition != null && definition.ScenarioVariants != null && definition.ScenarioVariants.Length > 0)
            return definition.ScenarioVariants[0];

        BattleScenarioVariant[] variants = BattleScenarioAd001Runner.CreateDefaultVariants();
        return variants.Length > 0 ? variants[^1] : variant;
    }

    private static bool CanPlayVariant(BattleScenarioVariant variant)
    {
        return variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.GroundMissile ||
               IsAirTargetKind(variant.IncomingThreatKind);
    }

    private static bool IsAirTargetKind(BattleScenarioIncomingThreatKind threatKind)
    {
        return threatKind == BattleScenarioIncomingThreatKind.Jet ||
               threatKind == BattleScenarioIncomingThreatKind.Drone ||
               threatKind == BattleScenarioIncomingThreatKind.Helicopter;
    }

    private void PositionGroundMissileAuthoringRoots(BattleScenarioVariant variant)
    {
        Vector3 groundPosition = new(Mathf.Max(40f, variant.IncomingThreatStartDistance), 0f, 0f);
        if (groundLauncherRoot != null)
        {
            groundLauncherRoot.gameObject.SetActive(false);
            groundLauncherRoot.SetPositionAndRotation(groundPosition, Quaternion.Euler(0f, -90f, 0f));
        }

        if (airLauncherRoot != null)
        {
            airLauncherRoot.gameObject.SetActive(false);
            airLauncherRoot.SetPositionAndRotation((Vector3)AirLauncherPosition, Quaternion.Euler(0f, 90f, 0f));
        }

        if (radarRoot != null)
        {
            bool radarEnabled = variant.SupportMode == BattleScenarioSupportMode.RadarNear;
            radarRoot.gameObject.SetActive(false);
            radarRoot.SetPositionAndRotation(new Vector3(Mathf.Max(0f, variant.RadarDistanceFromLauncher), 0f, -12f), Quaternion.identity);
            radarRoot.localScale = radarEnabled ? Vector3.one : Vector3.one * 0.0001f;
        }

        if (defendedTargetVisual != null)
            defendedTargetVisual.position = (Vector3)DefendedTargetPosition + new Vector3(0f, 1.2f, 0f);
    }

    private void PositionAirTargetAuthoringRoots(BattleScenarioVariant variant)
    {
        if (groundLauncherRoot != null)
            groundLauncherRoot.gameObject.SetActive(false);

        if (airLauncherRoot != null)
        {
            airLauncherRoot.gameObject.SetActive(false);
            airLauncherRoot.SetPositionAndRotation((Vector3)AirLauncherPosition, Quaternion.Euler(0f, 90f, 0f));
        }

        if (radarRoot != null)
        {
            bool radarEnabled = variant.SupportMode == BattleScenarioSupportMode.RadarNear;
            radarRoot.gameObject.SetActive(false);
            radarRoot.SetPositionAndRotation(new Vector3(Mathf.Max(0f, variant.RadarDistanceFromLauncher), 0f, -12f), Quaternion.identity);
            radarRoot.localScale = radarEnabled ? Vector3.one : Vector3.one * 0.0001f;
        }

        if (defendedTargetVisual != null)
            defendedTargetVisual.position = (Vector3)ResolveAirTargetPosition(variant) + new Vector3(0f, -1.2f, 0f);
    }

    private static string ResolveAirTargetPrefabKey(BattleScenarioVariant variant)
    {
        return variant.IncomingThreatKind switch
        {
            BattleScenarioIncomingThreatKind.Helicopter => HelicopterTargetKey,
            BattleScenarioIncomingThreatKind.Drone => DroneTargetKey,
            _ => JetTargetKey
        };
    }

    private static float3 ResolveAirTargetPosition(BattleScenarioVariant variant)
    {
        float distance = math.max(45f, variant.IncomingThreatStartDistance);
        float altitude = math.max(8f, variant.IncomingThreatAltitude);
        float z = variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Helicopter ? 12f : 0f;
        if (variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Drone)
            z = -10f;
        if (!string.IsNullOrWhiteSpace(variant.VariantId) &&
            variant.VariantId.IndexOf("Attacking", System.StringComparison.OrdinalIgnoreCase) >= 0)
            z = 8f;

        return new float3(distance, altitude, z);
    }

    private static void ConfigureLiveScenario(
        EntityManager em,
        BattleScenarioVariant variant,
        Entity airLauncher,
        Entity groundLauncher,
        Entity radar)
    {
        SetFaction(em, airLauncher, FactionIdentity.PlayerFactionId);
        SetFaction(em, groundLauncher, FactionIdentity.EnemyFactionId);

        SetLocalTransform(em, airLauncher, AirLauncherPosition, AirLauncherRotation, 1f);
        SetLocalTransform(
            em,
            groundLauncher,
            new float3(math.max(40f, variant.IncomingThreatStartDistance), 0f, 0f),
            GroundLauncherRotation,
            1f);

        ConfigureRadar(em, variant, radar);
        ResetAirLauncherForGroundMissile(em, airLauncher);
        StartGroundMissileLaunch(em, groundLauncher, variant);
    }

    private static void ConfigureLiveAirTargetScenario(
        EntityManager em,
        BattleScenarioVariant variant,
        Entity airLauncher,
        Entity target,
        Entity radar)
    {
        SetFaction(em, airLauncher, FactionIdentity.PlayerFactionId);
        SetFaction(em, target, FactionIdentity.EnemyFactionId);

        SetLocalTransform(em, airLauncher, AirLauncherPosition, AirLauncherRotation, 1f);
        SetLocalTransform(
            em,
            target,
            ResolveAirTargetPosition(variant),
            quaternion.RotateY(math.radians(-90f)),
            1f);

        ConfigureRadar(em, variant, radar);
        ConfigureAirTarget(em, target, variant);
        ResetAirLauncherForAirTarget(em, airLauncher);
    }

    private static void ConfigureRadar(EntityManager em, BattleScenarioVariant variant, Entity radar)
    {
        if (radar == Entity.Null || !em.Exists(radar))
            return;

        bool enabled = variant.SupportMode == BattleScenarioSupportMode.RadarNear;
        SetFaction(em, radar, FactionIdentity.PlayerFactionId);
        SetLocalTransform(
            em,
            radar,
            new float3(math.max(0f, variant.RadarDistanceFromLauncher), 0f, -12f),
            quaternion.identity,
            enabled ? 1f : 0.0001f);

        if (!em.HasComponent<AirDefenseSupportProviderComponent>(radar))
            return;

        em.SetComponentData(radar, new AirDefenseSupportProviderComponent
        {
            Kind = (byte)AirDefenseSupportProviderKind.Radar,
            Level = 1,
            SupportRadius = enabled ? 90f : 0f,
            RangeBonus = AirDefenseSupportTuning.RadarRangeBonus,
            LockTimeMultiplier = AirDefenseSupportTuning.RadarLockTimeMultiplier,
            TrackingBonus = AirDefenseSupportTuning.RadarTrackingBonus,
            TurnRateBonus = AirDefenseSupportTuning.RadarTurnRateBonus
        });
    }

    private static void ConfigureAirTarget(EntityManager em, Entity target, BattleScenarioVariant variant)
    {
        if (target == Entity.Null || !em.Exists(target))
            return;

        float3 targetPosition = ResolveAirTargetPosition(variant);
        if (em.HasComponent<UnitHealth>(target))
        {
            int health = variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Helicopter ? 130 : 100;
            em.SetComponentData(target, new UnitHealth { Current = health, Max = health });
        }
        else
        {
            em.AddComponentData(target, new UnitHealth { Current = 100, Max = 100 });
        }

        if (em.HasComponent<UnitPrevWorldPos>(target))
            em.SetComponentData(target, new UnitPrevWorldPos { Value = targetPosition });
        else
            em.AddComponentData(target, new UnitPrevWorldPos { Value = targetPosition });

        if (!em.HasComponent<UnitAirMovement>(target))
        {
            em.AddComponentData(target, new UnitAirMovement
            {
                CruiseHeight = math.max(8f, variant.IncomingThreatAltitude),
                RunwayTaxiSpeed = 12f
            });
        }

        if (em.HasComponent<UnitAirComponent>(target))
        {
            UnitAirComponent air = em.GetComponentData<UnitAirComponent>(target);
            air.HomePosition = new float3(targetPosition.x, 0f, targetPosition.z);
            air.HomeInitialized = 1;
            air.Airborne = 1;
            air.ReturningHome = 0;
            air.TakeoffRolling = 0;
            air.LandingRolling = 0;
            air.AttackRunActive = (byte)(IsAttackRunVariant(variant) ? 1 : 0);
            em.SetComponentData(target, air);
        }
        else
        {
            em.AddComponentData(target, new UnitAirComponent
            {
                HomePosition = new float3(targetPosition.x, 0f, targetPosition.z),
                HomeInitialized = 1,
                Airborne = 1,
                AttackRunActive = (byte)(IsAttackRunVariant(variant) ? 1 : 0)
            });
        }
    }

    private static bool IsAttackRunVariant(BattleScenarioVariant variant)
    {
        return !string.IsNullOrWhiteSpace(variant.VariantId) &&
               variant.VariantId.IndexOf("Attacking", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void ResetAirLauncherForGroundMissile(EntityManager em, Entity airLauncher)
    {
        ResetAirLauncher(
            em,
            airLauncher,
            airTargetPriority: 25f,
            incomingMissilePriority: 100f,
            turnRateDegreesPerSecond: ScenarioAirMissileTurnRateDegreesPerSecond,
            proximityFuseRadius: VisualInterceptProximityFuseRadius,
            trackingQuality: ScenarioAirTrackingQuality,
            airTargetDamage: 120);
    }

    private static void ResetAirLauncherForAirTarget(EntityManager em, Entity airLauncher)
    {
        ResetAirLauncher(
            em,
            airLauncher,
            airTargetPriority: 100f,
            incomingMissilePriority: 25f,
            turnRateDegreesPerSecond: ScenarioAirTargetMissileTurnRateDegreesPerSecond,
            proximityFuseRadius: VisualAirTargetProximityFuseRadius,
            trackingQuality: ScenarioAirTargetTrackingQuality,
            airTargetDamage: 140);
    }

    private static void ResetAirLauncher(
        EntityManager em,
        Entity airLauncher,
        float airTargetPriority,
        float incomingMissilePriority,
        float turnRateDegreesPerSecond,
        float proximityFuseRadius,
        float trackingQuality,
        int airTargetDamage)
    {
        if (!em.HasComponent<AirMissileLauncherComponent>(airLauncher) ||
            !em.HasComponent<AirMissileLauncherStateComponent>(airLauncher))
        {
            return;
        }

        AirMissileLauncherComponent launcher = em.GetComponentData<AirMissileLauncherComponent>(airLauncher);
        launcher.MinRange = 4f;
        launcher.BaseDetectionRange = ScenarioAirBaseDetectionRange;
        launcher.MaxDetectionRange = ScenarioAirMaxDetectionRange;
        launcher.AirTargetPriority = math.max(launcher.AirTargetPriority, airTargetPriority);
        launcher.IncomingMissilePriority = math.max(launcher.IncomingMissilePriority, incomingMissilePriority);
        launcher.TurretYawSpeedDegreesPerSecond = math.max(launcher.TurretYawSpeedDegreesPerSecond, 900f);
        launcher.AimToleranceDegrees = math.max(launcher.AimToleranceDegrees, 5f);
        launcher.LockSeconds = ScenarioAirLockSeconds;
        launcher.LaunchDelaySeconds = ScenarioAirLaunchDelaySeconds;
        launcher.MissileSpeed = ScenarioAirMissileSpeed;
        launcher.MissileAcceleration = 0f;
        launcher.MissileTurnRateDegreesPerSecond = turnRateDegreesPerSecond;
        launcher.MissileLifetimeSeconds = ScenarioAirMissileLifetimeSeconds;
        launcher.ProximityFuseRadius = proximityFuseRadius;
        launcher.AirTargetDamage = math.max(launcher.AirTargetDamage, airTargetDamage);
        launcher.IncomingMissileDamage = math.max(launcher.IncomingMissileDamage, 9999);
        launcher.TrackingQuality = trackingQuality;
        launcher.MaxSupportRangeBonus = math.max(launcher.MaxSupportRangeBonus, 120f);
        launcher.MaxSupportTrackingBonus = math.max(launcher.MaxSupportTrackingBonus, 0.3f);
        em.SetComponentData(airLauncher, launcher);

        em.SetComponentData(airLauncher, new AirMissileLauncherStateComponent
        {
            Phase = (byte)AirMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetKind = (byte)AirMissileTargetKind.None,
            TargetWorldPosition = float3.zero,
            PredictedInterceptPosition = float3.zero,
            Timer = 0f,
            SelectedMissileSlot = -1,
            EffectiveRange = launcher.BaseDetectionRange,
            EffectiveLockSeconds = launcher.LockSeconds,
            EffectiveTrackingQuality = launcher.TrackingQuality,
            EffectiveTurnRateDegreesPerSecond = launcher.MissileTurnRateDegreesPerSecond
        });

        if (em.HasComponent<AirMissileLauncherTargetComponent>(airLauncher))
            em.RemoveComponent<AirMissileLauncherTargetComponent>(airLauncher);
    }

    private static void StartGroundMissileLaunch(EntityManager em, Entity groundLauncher, BattleScenarioVariant variant)
    {
        if (!em.HasComponent<GroundMissileLauncherComponent>(groundLauncher) ||
            !em.HasComponent<GroundMissileLauncherStateComponent>(groundLauncher) ||
            !em.HasComponent<LocalTransform>(groundLauncher))
        {
            return;
        }

        GroundMissileLauncherComponent launcher = em.GetComponentData<GroundMissileLauncherComponent>(groundLauncher);
        LocalTransform launcherTransform = em.GetComponentData<LocalTransform>(groundLauncher);
        float horizontalDistance = math.distance(
            new float2(launcherTransform.Position.x, launcherTransform.Position.z),
            new float2(DefendedTargetPosition.x, DefendedTargetPosition.z));
        float flightSeconds = ScenarioGroundMissileBaseFlightSeconds /
                              math.max(0.1f, variant.IncomingThreatSpeedMultiplier);
        launcher.RocketSpeed = horizontalDistance / math.max(0.35f, flightSeconds);
        launcher.ArcHeight = math.min(
            math.max(0f, variant.IncomingThreatAltitude),
            VisualGroundMissileArcHeight);
        em.SetComponentData(groundLauncher, launcher);

        int rocketCount = em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(groundLauncher)
            ? em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(groundLauncher).Length
            : 0;

        em.SetComponentData(groundLauncher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = Entity.Null,
            TargetCell = default,
            TargetWorldPosition = DefendedTargetPosition,
            Timer = GroundMissileLauncherTiming.PrepareAndHoldSeconds(launcher.PrepareSeconds),
            SelectedRocketSlot = rocketCount > 0 ? 0 : -1
        });
    }

    private static void ResetPreviousRun(EntityManager em)
    {
        RestoreAirMissileVisuals(em);
        RestoreGroundRocketVisuals(em);
        DestroyScenarioLabUnits(em);
        DestroyEntitiesWith<GroundMissileProjectileComponent>(em);
        DestroyEntitiesWith<AirMissileProjectileComponent>(em);
        DestroyEntitiesWith<GroundMissileImpactRequestComponent>(em);
        RemoveComponentFromAll<AirMissileImpactRequestComponent>(em);
        RemoveComponentFromAll<AirMissileProjectileTrailComponent>(em);
        RemoveComponentFromAll<AirMissileLauncherTargetComponent>(em);
        RemoveComponentFromAll<MissileInterceptedComponent>(em);
    }

    private static void DestroyScenarioLabUnits(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity) || !em.HasComponent<UnitSourcePrefabKey>(entity) || em.HasComponent<Prefab>(entity))
                continue;

            FixedString64Bytes key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value;
            if (SourceKeyMatches(key, GroundLauncherKey) ||
                SourceKeyMatches(key, AirLauncherKey) ||
                SourceKeyMatches(key, RadarKey) ||
                SourceKeyMatches(key, JetTargetKey) ||
                SourceKeyMatches(key, HelicopterTargetKey) ||
                SourceKeyMatches(key, DroneTargetKey))
            {
                DestroyLinkedEntityGroup(em, entity);
            }
        }
    }

    private static void DestroyLinkedEntityGroup(EntityManager em, Entity root)
    {
        if (!em.Exists(root))
            return;

        if (!em.HasBuffer<LinkedEntityGroup>(root))
        {
            em.DestroyEntity(root);
            return;
        }

        DynamicBuffer<LinkedEntityGroup> linkedGroup = em.GetBuffer<LinkedEntityGroup>(root);
        NativeArray<Entity> entities = new(linkedGroup.Length, Allocator.Temp);
        try
        {
            for (int i = 0; i < linkedGroup.Length; i++)
                entities[i] = linkedGroup[i].Value;
            em.DestroyEntity(entities);
        }
        finally
        {
            entities.Dispose();
        }
    }

    private static void RestoreAirMissileVisuals(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<AirMissileFlyingVisualComponent>(),
            ComponentType.ReadWrite<LocalTransform>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            AirMissileFlyingVisualComponent visual = em.GetComponentData<AirMissileFlyingVisualComponent>(entity);
            if (visual.OriginalParent != Entity.Null && em.Exists(visual.OriginalParent) && !em.HasComponent<Parent>(entity))
                em.AddComponentData(entity, new Parent { Value = visual.OriginalParent });

            em.SetComponentData(
                entity,
                LocalTransform.FromPositionRotationScale(
                    visual.InitialLocalPosition,
                    visual.InitialLocalRotation,
                    math.max(0.0001f, visual.InitialLocalScale)));

            if (em.HasComponent<AirMissileProjectileComponent>(entity))
                em.RemoveComponent<AirMissileProjectileComponent>(entity);
            if (em.HasComponent<AirMissileProjectileTrailComponent>(entity))
                em.RemoveComponent<AirMissileProjectileTrailComponent>(entity);
            if (em.HasComponent<AirMissileImpactRequestComponent>(entity))
                em.RemoveComponent<AirMissileImpactRequestComponent>(entity);
            em.RemoveComponent<AirMissileFlyingVisualComponent>(entity);
        }
    }

    private static void RestoreGroundRocketVisuals(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<GroundMissileFlyingRocketVisualComponent>(),
            ComponentType.ReadWrite<LocalTransform>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            GroundMissileFlyingRocketVisualComponent visual = em.GetComponentData<GroundMissileFlyingRocketVisualComponent>(entity);
            if (visual.OriginalParent != Entity.Null && em.Exists(visual.OriginalParent) && !em.HasComponent<Parent>(entity))
                em.AddComponentData(entity, new Parent { Value = visual.OriginalParent });

            em.SetComponentData(
                entity,
                LocalTransform.FromPositionRotationScale(
                    visual.InitialLocalPosition,
                    visual.InitialLocalRotation,
                    math.max(0.0001f, visual.InitialLocalScale)));
            em.RemoveComponent<GroundMissileFlyingRocketVisualComponent>(entity);
        }
    }

    private IEnumerator CameraAirTargetRoutine(EntityManager em, BattleScenarioVariant variant, Entity airLauncher, Entity target)
    {
        float startedAt = Time.time;
        bool sawAirProjectile = false;
        bool sawImpact = false;
        Vector3 lastFocus = TryGetPosition(em, target, out float3 initialTargetPosition)
            ? (Vector3)initialTargetPosition
            : (Vector3)ResolveAirTargetPosition(variant);

        while (Time.time - startedAt < 14f)
        {
            Entity airProjectile = FindFirstEntity<AirMissileProjectileComponent>(em);
            bool hasAirProjectile = TryGetPosition(em, airProjectile, out float3 airPosition);
            bool hasTarget = TryGetPosition(em, target, out float3 targetPosition);

            if (hasAirProjectile)
            {
                sawAirProjectile = true;
                lastFocus = (Vector3)airPosition;
            }

            if (target != Entity.Null &&
                em.Exists(target) &&
                em.HasComponent<UnitHealth>(target) &&
                em.GetComponentData<UnitHealth>(target).Current <= 0)
            {
                sawImpact = true;
            }

            if (hasAirProjectile && hasTarget)
            {
                Vector3 midpoint = ((Vector3)airPosition + (Vector3)targetPosition) * 0.5f;
                SetCamera(midpoint + new Vector3(18f, 10f, -30f), midpoint);
            }
            else if (hasAirProjectile)
            {
                SetCamera((Vector3)airPosition + new Vector3(18f, 9f, -24f), lastFocus);
            }
            else if (hasTarget && !sawAirProjectile)
            {
                Vector3 targetVector = (Vector3)targetPosition;
                SetCamera(targetVector + new Vector3(34f, 15f, -44f), targetVector + new Vector3(-24f, 0f, 0f));
            }
            else if (sawImpact)
            {
                SetCamera(lastFocus + new Vector3(18f, 12f, -30f), lastFocus);
                yield return new WaitForSeconds(2f);
                break;
            }
            else
            {
                TryGetPosition(em, airLauncher, out float3 airLauncherPosition);
                SetCamera((Vector3)airLauncherPosition + new Vector3(26f, 13f, -30f), (Vector3)airLauncherPosition + new Vector3(36f, 10f, 0f));
            }

            if (sawAirProjectile && !hasAirProjectile)
            {
                SetCamera(lastFocus + new Vector3(18f, 12f, -30f), lastFocus);
                yield return new WaitForSeconds(2f);
                break;
            }

            yield return null;
        }
    }

    private IEnumerator CameraOpeningRoutine(EntityManager em, BattleScenarioVariant variant, Entity airLauncher, Entity groundLauncher)
    {
        float startedAt = Time.time;
        bool sawGroundProjectile = false;
        bool sawAirProjectile = false;
        Vector3 lastFocus = new(Mathf.Max(40f, variant.IncomingThreatStartDistance) * 0.5f, 10f, 0f);

        while (Time.time - startedAt < 16f)
        {
            Entity groundProjectile = FindFirstEntity<GroundMissileProjectileComponent>(em);
            Entity airProjectile = FindFirstEntity<AirMissileProjectileComponent>(em);
            bool hasGroundProjectile = TryGetPosition(em, groundProjectile, out float3 groundPosition);
            bool hasAirProjectile = TryGetPosition(em, airProjectile, out float3 airPosition);

            if (hasGroundProjectile)
            {
                sawGroundProjectile = true;
                lastFocus = (Vector3)groundPosition;
            }

            if (hasAirProjectile)
            {
                sawAirProjectile = true;
                lastFocus = (Vector3)airPosition;
            }

            if (hasAirProjectile && hasGroundProjectile)
            {
                Vector3 midpoint = ((Vector3)airPosition + (Vector3)groundPosition) * 0.5f;
                SetCamera(midpoint + new Vector3(22f, 13f, -32f), midpoint);
            }
            else if (hasAirProjectile)
            {
                SetCamera((Vector3)airPosition + new Vector3(18f, 9f, -24f), Vector3.Lerp((Vector3)airPosition, lastFocus, 0.45f));
            }
            else if (hasGroundProjectile)
            {
                SetCamera((Vector3)groundPosition + new Vector3(24f, 10f, -30f), (Vector3)groundPosition + Vector3.left * 12f);
            }
            else if (!sawGroundProjectile)
            {
                TryGetPosition(em, groundLauncher, out float3 groundLauncherPosition);
                SetCamera((Vector3)groundLauncherPosition + new Vector3(26f, 12f, -28f), (Vector3)groundLauncherPosition + new Vector3(-18f, 4f, 0f));
            }
            else if (sawAirProjectile)
            {
                SetCamera(lastFocus + new Vector3(24f, 14f, -34f), lastFocus);
                yield return new WaitForSeconds(2f);
                break;
            }
            else
            {
                TryGetPosition(em, airLauncher, out float3 airLauncherPosition);
                SetCamera((Vector3)airLauncherPosition + new Vector3(26f, 13f, -30f), (Vector3)airLauncherPosition + new Vector3(26f, 8f, 0f));
            }

            yield return null;
        }
    }

    private static void SetFaction(EntityManager em, Entity entity, byte factionId)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        if (em.HasComponent<Faction>(entity))
            em.SetComponentData(entity, new Faction { Id = factionId });
        else
            em.AddComponentData(entity, new Faction { Id = factionId });
    }

    private static void SetLocalTransform(EntityManager em, Entity entity, float3 position, quaternion rotation, float scale)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<LocalTransform>(entity))
            return;

        em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(position, rotation, math.max(0.0001f, scale)));
    }

    private static bool TryResolveUnitPrefab(EntityManager em, string sourceKey, out Entity prefab)
    {
        prefab = Entity.Null;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
            ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
        if (query.CalculateEntityCount() <= 0)
            return false;

        using NativeArray<Entity> registries = query.ToEntityArray(Allocator.Temp);
        for (int registryIndex = 0; registryIndex < registries.Length; registryIndex++)
        {
            Entity registry = registries[registryIndex];
            DynamicBuffer<UnitPrefabRegistryEntry> entries = em.GetBuffer<UnitPrefabRegistryEntry>(registry);
            for (int i = 0; i < entries.Length; i++)
            {
                Entity candidate = entries[i].Prefab;
                if (candidate == Entity.Null || !em.Exists(candidate) || !em.HasComponent<Prefab>(candidate))
                    continue;

                if (EntityMatchesSourceKey(em, candidate, sourceKey))
                {
                    prefab = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static Entity InstantiateUnitPrefab(EntityManager em, Entity prefab)
    {
        Entity instance = em.Instantiate(prefab);
        if (em.HasComponent<Disabled>(instance))
            em.RemoveComponent<Disabled>(instance);
        return instance;
    }

    private static bool EntityMatchesSourceKey(EntityManager em, Entity candidate, string sourceKey)
    {
        if (em.HasComponent<UnitSourcePrefabKey>(candidate) &&
            SourceKeyMatches(em.GetComponentData<UnitSourcePrefabKey>(candidate).Value, sourceKey))
        {
            return true;
        }

        return SourceKeyMatches(em.GetName(candidate), sourceKey);
    }

    private static bool SourceKeyMatches(FixedString64Bytes candidate, string sourceKey)
    {
        return SourceKeyMatches(candidate.ToString(), sourceKey);
    }

    private static bool SourceKeyMatches(string candidate, string sourceKey)
    {
        string normalizedCandidate = NormalizeSourceKey(candidate);
        string normalizedSource = NormalizeSourceKey(sourceKey);
        return !string.IsNullOrEmpty(normalizedCandidate) &&
               string.Equals(normalizedCandidate, normalizedSource, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSourceKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" (Clone)", string.Empty).Trim().ToLowerInvariant();
    }

    private static Entity FindFirstEntity<T>(EntityManager em)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<LocalTransform>());
        if (query.CalculateEntityCount() <= 0)
            return Entity.Null;

        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        return entities[0];
    }

    private static bool TryGetPosition(EntityManager em, Entity entity, out float3 position)
    {
        if (entity != Entity.Null && em.Exists(entity) && em.HasComponent<LocalTransform>(entity))
        {
            position = em.GetComponentData<LocalTransform>(entity).Position;
            return true;
        }

        position = float3.zero;
        return false;
    }

    private static void DestroyEntitiesWith<T>(EntityManager em)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
            if (em.Exists(entities[i]))
                em.DestroyEntity(entities[i]);
    }

    private static void RemoveComponentFromAll<T>(EntityManager em)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
            if (em.Exists(entities[i]) && em.HasComponent<T>(entities[i]))
                em.RemoveComponent<T>(entities[i]);
    }

    private void SetCamera(Vector3 position, Vector3 lookAt)
    {
        if (scenarioCamera == null)
            return;

        scenarioCamera.transform.position = position;
        Vector3 direction = lookAt - position;
        if (direction.sqrMagnitude > 0.001f)
            scenarioCamera.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
