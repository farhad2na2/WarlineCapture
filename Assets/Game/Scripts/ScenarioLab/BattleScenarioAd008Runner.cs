using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class BattleScenarioAd008Runner
{
    public const string ScenarioId = "AD-008_AirMissileLauncher_SaturatedMixedDroneAndGroundMissile_RadarComparison";

    private const int Seed = 82345;
    private const float FixedDeltaTime = 0.05f;
    private const float MaxDurationSeconds = 18f;
    private static readonly float3 LauncherPosition = new(0f, 0f, 0f);
    private static readonly float3 DefendedTargetPosition = new(-40f, 0f, -5f);
    private static readonly float3 DroneEndPosition = new(-25f, 16f, 18f);

    public static BattleScenarioVariant[] CreateDefaultVariants()
    {
        return new[]
        {
            new BattleScenarioVariant
            {
                VariantId = "AD-008-A-NoSupport-MixedDroneAndGroundMissile",
                Label = "No Support / Drone Plus Ground Missile",
                SupportMode = BattleScenarioSupportMode.None,
                IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
                IncomingThreatSpeedMultiplier = 1f,
                IncomingThreatStartDistance = 145f,
                IncomingThreatAltitude = 10f,
                LauncherCount = 1,
                RadarDistanceFromLauncher = 0f,
                ExpectedOutcome = BattleScenarioExpectedOutcome.Baseline
            },
            new BattleScenarioVariant
            {
                VariantId = "AD-008-B-RadarNear-MixedDroneAndGroundMissile",
                Label = "Radar Near / Drone Plus Ground Missile",
                SupportMode = BattleScenarioSupportMode.RadarNear,
                IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
                IncomingThreatSpeedMultiplier = 1f,
                IncomingThreatStartDistance = 145f,
                IncomingThreatAltitude = 10f,
                LauncherCount = 1,
                RadarDistanceFromLauncher = 8f,
                ExpectedOutcome = BattleScenarioExpectedOutcome.MustImproveOrMatchBaseline
            }
        };
    }

    public static BattleScenarioResult RunDefault()
    {
        return RunVariants(CreateDefaultVariants(), Seed, FixedDeltaTime, MaxDurationSeconds);
    }

    public static BattleScenarioResult RunDefinition(BattleScenarioDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (definition.ScenarioId != ScenarioId)
            throw new ArgumentException($"Unsupported AD-008 scenario id '{definition.ScenarioId}'.", nameof(definition));

        BattleScenarioVariant[] variants = definition.ScenarioVariants;
        if (variants.Length == 0)
            variants = CreateDefaultVariants();

        return RunVariants(
            variants,
            definition.RandomSeed,
            definition.FixedDeltaTime,
            definition.MaxDurationSeconds);
    }

    private static BattleScenarioResult RunVariants(
        BattleScenarioVariant[] variants,
        int seed,
        float fixedDeltaTime,
        float maxDurationSeconds)
    {
        var result = new BattleScenarioResult
        {
            ScenarioId = ScenarioId,
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            FixedDeltaTime = fixedDeltaTime,
            Variants = new BattleScenarioMetrics[variants.Length],
            FailureReason = BattleScenarioFailureReason.None
        };

        for (int i = 0; i < variants.Length; i++)
            result.Variants[i] = RunVariant(variants[i], seed, fixedDeltaTime, maxDurationSeconds);

        result.Comparisons = new[]
        {
            BattleScenarioResultComparison.CompareRadarSupport(result.Variants[0], result.Variants[1])
        };
        result.Passed = EvaluateResult(result);
        if (!result.Passed)
            result.FailureReason = BattleScenarioFailureReason.MetricsComparisonFailed;

        return result;
    }

    private static BattleScenarioMetrics RunVariant(
        BattleScenarioVariant variant,
        int seed,
        float fixedDeltaTime,
        float maxDurationSeconds)
    {
        using World world = new($"BattleScenarioLab_{variant.VariantId}");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em);
        Entity incomingMissile = CreateIncomingGroundMissile(em, variant);
        Entity droneTarget = BattleScenarioEcsSpawnHelpers.CreateAirTarget(
            em,
            new float3(variant.IncomingThreatStartDistance + 12f, 16f, 24f),
            FactionIdentity.EnemyFactionId,
            80,
            16f,
            5f);
        if (variant.SupportMode == BattleScenarioSupportMode.RadarNear)
            CreateRadarSupportProvider(em, variant.RadarDistanceFromLauncher);

        SystemHandle supportSystem = world.CreateSystem<AirMissileLauncherSupportLinkSystem>();
        SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
        SystemHandle airImpactSystem = world.CreateSystem<AirMissileImpactSystem>();
        SystemHandle groundFlightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
        SystemHandle groundImpactSystem = world.CreateSystem<GroundMissileImpactSystem>();

        bool incomingWasAlive = true;
        bool missileIntercepted = false;
        bool droneDestroyed = false;
        return BattleScenarioFixedStepRunner.RunVariant(
            ScenarioId,
            variant,
            seed,
            fixedDeltaTime,
            maxDurationSeconds,
            (state, metrics) =>
            {
                world.SetTime(new TimeData(state.TimeSeconds, state.FixedDeltaTime));
                MoveDroneTarget(em, droneTarget, variant, state);
                UpdateIfHasWork(world, supportSystem);
                UpdateIfHasWork(world, groundFlightSystem);
                UpdateIfHasWork(world, acquisitionSystem);
                CaptureLauncherMetrics(em, launcher, incomingMissile, droneTarget, metrics, state);
                UpdateIfHasWork(world, fireControlSystem);
                CaptureLauncherMetrics(em, launcher, incomingMissile, droneTarget, metrics, state);
                CaptureProjectileMetrics(em, incomingMissile, droneTarget, metrics);
                UpdateIfHasWork(world, homingSystem);
                UpdateIfHasWork(world, airImpactSystem);
                UpdateIfHasWork(world, groundImpactSystem);
                CaptureProjectileMetrics(em, incomingMissile, droneTarget, metrics);

                bool incomingAlive = em.Exists(incomingMissile) && em.HasComponent<GroundMissileProjectileComponent>(incomingMissile);
                if (incomingWasAlive && !incomingAlive)
                {
                    incomingWasAlive = false;
                    missileIntercepted = !metrics.IncomingThreatImpacted;
                }

                if (em.Exists(incomingMissile) && em.HasComponent<GroundMissileImpactRequestComponent>(incomingMissile))
                {
                    metrics.IncomingThreatImpacted = true;
                    metrics.IncomingThreatImpactTimeSeconds = state.TimeSeconds;
                    metrics.FailureReason = BattleScenarioFailureReason.IncomingThreatImpactedTarget;
                    return BattleScenarioStepOutcome.Failed;
                }

                if (!em.Exists(droneTarget) || !em.HasComponent<UnitHealth>(droneTarget))
                {
                    metrics.FailureReason = BattleScenarioFailureReason.TargetEntityMissing;
                    return BattleScenarioStepOutcome.Failed;
                }

                UnitHealth droneHealth = em.GetComponentData<UnitHealth>(droneTarget);
                if (droneHealth.Current <= 0)
                    droneDestroyed = true;

                if (missileIntercepted && droneDestroyed)
                {
                    metrics.Intercepted = true;
                    metrics.InterceptTimeSeconds = state.TimeSeconds;
                    metrics.FailureReason = BattleScenarioFailureReason.None;
                    return BattleScenarioStepOutcome.Complete;
                }

                return BattleScenarioStepOutcome.Continue;
            });
    }

    private static bool EvaluateResult(BattleScenarioResult result)
    {
        if (result.Variants.Length < 2 || result.Comparisons.Length < 1)
            return false;

        BattleScenarioMetrics supported = result.Variants[1];
        BattleScenarioComparison comparison = result.Comparisons[0];
        return supported.Intercepted &&
               supported.RadarProviderUsed &&
               !supported.IncomingThreatImpacted &&
               comparison.RadarImprovedLockTime &&
               comparison.RadarImprovedOrMatchedOutcome;
    }

    private static void MoveDroneTarget(
        EntityManager em,
        Entity droneTarget,
        BattleScenarioVariant variant,
        BattleScenarioFixedStepState state)
    {
        if (!em.Exists(droneTarget) || !em.HasComponent<LocalTransform>(droneTarget))
            return;

        LocalTransform transform = em.GetComponentData<LocalTransform>(droneTarget);
        if (em.HasComponent<UnitPrevWorldPos>(droneTarget))
            em.SetComponentData(droneTarget, new UnitPrevWorldPos { Value = transform.Position });

        float duration = 9f / math.max(0.1f, variant.IncomingThreatSpeedMultiplier);
        float t = math.saturate(state.TimeSeconds / duration);
        float3 start = new(variant.IncomingThreatStartDistance + 12f, 16f, 24f);
        transform.Position = math.lerp(start, DroneEndPosition, t);
        em.SetComponentData(droneTarget, transform);
    }

    private static void UpdateIfHasWork(World world, SystemHandle system)
    {
        if (world.Unmanaged.ResolveSystemStateRef(system).ShouldRunSystem())
            system.Update(world.Unmanaged);
    }

    private static Entity CreateLauncher(EntityManager em)
    {
        return BattleScenarioEcsSpawnHelpers.CreateAirMissileLauncher(
            em,
            LauncherPosition,
            FactionIdentity.PlayerFactionId,
            500,
            new AirMissileLauncherComponent
            {
                MinRange = 4f,
                BaseDetectionRange = 140f,
                MaxDetectionRange = 260f,
                AirTargetPriority = 90f,
                IncomingMissilePriority = 120f,
                TurretYawSpeedDegreesPerSecond = 900f,
                AimToleranceDegrees = 5f,
                LockSeconds = 0.9f,
                LaunchDelaySeconds = 0.1f,
                ReloadSeconds = 1.1f,
                MissileSpeed = 95f,
                MissileAcceleration = 0f,
                MissileTurnRateDegreesPerSecond = 150f,
                MissileLifetimeSeconds = 5f,
                ProximityFuseRadius = 4f,
                AirTargetDamage = 120,
                IncomingMissileDamage = 9999,
                TrackingQuality = 0.75f,
                MaxSupportRangeBonus = 120f,
                MaxSupportTrackingBonus = 0.3f
            },
            new AirMissileLauncherStateComponent
            {
                Phase = (byte)AirMissileLauncherPhase.Idle,
                TargetEntity = Entity.Null,
                TargetKind = (byte)AirMissileTargetKind.None,
                EffectiveRange = 140f,
                EffectiveLockSeconds = 0.9f,
                EffectiveTrackingQuality = 0.75f,
                EffectiveTurnRateDegreesPerSecond = 150f,
                SelectedMissileSlot = -1
            },
            new AirDefenseSupportLinkComponent
            {
                LockTimeMultiplier = 1f
            });
    }

    private static Entity CreateIncomingGroundMissile(EntityManager em, BattleScenarioVariant variant)
    {
        float3 start = new(variant.IncomingThreatStartDistance, variant.IncomingThreatAltitude, -12f);
        return BattleScenarioEcsSpawnHelpers.CreateIncomingGroundMissile(
            em,
            start,
            DefendedTargetPosition,
            FactionIdentity.EnemyFactionId,
            8.5f / math.max(0.1f, variant.IncomingThreatSpeedMultiplier),
            10f,
            8f,
            120);
    }

    private static void CreateRadarSupportProvider(EntityManager em, float distanceFromLauncher)
    {
        BattleScenarioEcsSpawnHelpers.CreateAirDefenseSupportProvider(
            em,
            LauncherPosition + new float3(distanceFromLauncher, 0f, 0f),
            FactionIdentity.PlayerFactionId,
            AirDefenseSupportProviderKind.Radar,
            1,
            90f,
            AirDefenseSupportTuning.RadarRangeBonus,
            AirDefenseSupportTuning.RadarLockTimeMultiplier,
            AirDefenseSupportTuning.RadarTrackingBonus,
            AirDefenseSupportTuning.RadarTurnRateBonus);
    }

    private static void CaptureLauncherMetrics(
        EntityManager em,
        Entity launcher,
        Entity incomingMissile,
        Entity droneTarget,
        BattleScenarioMetrics metrics,
        BattleScenarioFixedStepState state)
    {
        if (!em.Exists(launcher))
            return;

        AirMissileLauncherStateComponent launcherState = em.GetComponentData<AirMissileLauncherStateComponent>(launcher);
        AirDefenseSupportLinkComponent supportLink = em.GetComponentData<AirDefenseSupportLinkComponent>(launcher);
        metrics.LauncherEffectiveRange = launcherState.EffectiveRange;
        metrics.LauncherEffectiveLockSeconds = launcherState.EffectiveLockSeconds;
        metrics.LauncherEffectiveTrackingQuality = launcherState.EffectiveTrackingQuality;
        metrics.LauncherEffectiveTurnRateDegreesPerSecond = launcherState.EffectiveTurnRateDegreesPerSecond;
        metrics.RadarProviderUsed = supportLink.RadarProvider != Entity.Null;
        metrics.SatelliteProviderUsed = supportLink.SatelliteProvider != Entity.Null;

        if (!metrics.Detected && em.HasComponent<AirMissileLauncherTargetComponent>(launcher))
        {
            metrics.Detected = true;
            metrics.DetectionTimeSeconds = state.TimeSeconds;
            metrics.IncomingThreatDistanceAtDetection = FindClosestActiveThreatDistance(
                em,
                incomingMissile,
                droneTarget);
        }

        if (!metrics.TrackingStarted && launcherState.Phase == (byte)AirMissileLauncherPhase.Tracking)
        {
            metrics.TrackingStarted = true;
            metrics.TrackingStartTimeSeconds = state.TimeSeconds;
        }

        if (!metrics.Locked &&
            (launcherState.Phase == (byte)AirMissileLauncherPhase.Locked ||
             launcherState.Phase == (byte)AirMissileLauncherPhase.Launching ||
             launcherState.Phase == (byte)AirMissileLauncherPhase.Reloading))
        {
            metrics.Locked = true;
            metrics.LockTimeSeconds = state.TimeSeconds;
        }
    }

    private static void CaptureProjectileMetrics(
        EntityManager em,
        Entity incomingMissile,
        Entity droneTarget,
        BattleScenarioMetrics metrics)
    {
        using EntityQuery projectileQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<AirMissileProjectileComponent>(),
            ComponentType.ReadOnly<LocalTransform>());
        if (projectileQuery.CalculateEntityCount() <= 0)
            return;

        if (!metrics.InterceptorLaunched)
        {
            metrics.InterceptorLaunched = true;
            metrics.LaunchTimeSeconds = metrics.DurationSeconds;
        }

        using Unity.Collections.NativeArray<LocalTransform> transforms =
            projectileQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < transforms.Length; i++)
        {
            float distance = FindClosestThreatDistanceToPosition(em, incomingMissile, droneTarget, transforms[i].Position);
            if (distance >= 0f &&
                (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat))
            {
                metrics.ClosestInterceptorDistanceToThreat = distance;
            }
        }
    }

    private static float FindClosestActiveThreatDistance(EntityManager em, Entity incomingMissile, Entity droneTarget)
    {
        return FindClosestThreatDistanceToPosition(em, incomingMissile, droneTarget, LauncherPosition);
    }

    private static float FindClosestThreatDistanceToPosition(
        EntityManager em,
        Entity incomingMissile,
        Entity droneTarget,
        float3 position)
    {
        float closest = -1f;
        if (em.Exists(incomingMissile) && em.HasComponent<LocalTransform>(incomingMissile))
        {
            float distance = math.distance(position, em.GetComponentData<LocalTransform>(incomingMissile).Position);
            closest = distance;
        }

        if (em.Exists(droneTarget) && em.HasComponent<LocalTransform>(droneTarget))
        {
            float distance = math.distance(position, em.GetComponentData<LocalTransform>(droneTarget).Position);
            if (closest < 0f || distance < closest)
                closest = distance;
        }

        return closest;
    }
}
