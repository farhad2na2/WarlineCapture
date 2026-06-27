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
    private const float VisualInterceptProximityFuseRadius = 0.35f;
    private const float VisualGroundMissileArcHeight = 8f;
    private const float ScenarioGroundMissileBaseFlightSeconds = 8f;
    private const float ScenarioAirBaseDetectionRange = 140f;
    private const float ScenarioAirMaxDetectionRange = 260f;
    private const float ScenarioAirMissileSpeed = 95f;
    private const float ScenarioAirMissileTurnRateDegreesPerSecond = 140f;
    private const float ScenarioAirMissileLifetimeSeconds = 5f;
    private const float ScenarioAirLockSeconds = 0.9f;
    private const float ScenarioAirLaunchDelaySeconds = 0.1f;
    private const float ScenarioAirTrackingQuality = 0.75f;

    private static readonly float3 AirLauncherPosition = new(0f, 0f, 0f);
    private static readonly float3 DefendedTargetPosition = new(-40f, 0f, 0f);
    private static readonly quaternion AirLauncherRotation = quaternion.RotateY(math.radians(90f));
    private static readonly quaternion GroundLauncherRotation = quaternion.RotateY(math.radians(-90f));

    private Coroutine playbackRoutine;

    public void Play(BattleScenarioVariant variant, BattleScenarioMetrics metrics)
    {
        if (!isActiveAndEnabled)
            return;

        if (playbackRoutine != null)
            StopCoroutine(playbackRoutine);

        playbackRoutine = StartCoroutine(PlayLiveEcsRoutine(variant));
    }

    private IEnumerator PlayLiveEcsRoutine(BattleScenarioVariant variant)
    {
        BattleScenarioVariant resolvedVariant = ResolveVariant(variant);
        PositionAuthoringRoots(resolvedVariant);
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
        ConfigureLiveScenario(em, resolvedVariant, airLauncher, groundLauncher, radar);

        yield return CameraOpeningRoutine(em, resolvedVariant, airLauncher, groundLauncher);
    }

    private static BattleScenarioVariant ResolveVariant(BattleScenarioVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(variant.VariantId))
            return variant;

        BattleScenarioVariant[] variants = BattleScenarioAd001Runner.CreateDefaultVariants();
        return variants.Length > 0 ? variants[^1] : variant;
    }

    private void PositionAuthoringRoots(BattleScenarioVariant variant)
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
        ResetAirLauncher(em, airLauncher);
        StartGroundMissileLaunch(em, groundLauncher, variant);
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

    private static void ResetAirLauncher(EntityManager em, Entity airLauncher)
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
        launcher.IncomingMissilePriority = math.max(launcher.IncomingMissilePriority, 100f);
        launcher.TurretYawSpeedDegreesPerSecond = math.max(launcher.TurretYawSpeedDegreesPerSecond, 900f);
        launcher.AimToleranceDegrees = math.max(launcher.AimToleranceDegrees, 5f);
        launcher.LockSeconds = ScenarioAirLockSeconds;
        launcher.LaunchDelaySeconds = ScenarioAirLaunchDelaySeconds;
        launcher.MissileSpeed = ScenarioAirMissileSpeed;
        launcher.MissileAcceleration = 0f;
        launcher.MissileTurnRateDegreesPerSecond = ScenarioAirMissileTurnRateDegreesPerSecond;
        launcher.MissileLifetimeSeconds = ScenarioAirMissileLifetimeSeconds;
        launcher.ProximityFuseRadius = VisualInterceptProximityFuseRadius;
        launcher.IncomingMissileDamage = math.max(launcher.IncomingMissileDamage, 9999);
        launcher.TrackingQuality = ScenarioAirTrackingQuality;
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
                SourceKeyMatches(key, RadarKey))
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
