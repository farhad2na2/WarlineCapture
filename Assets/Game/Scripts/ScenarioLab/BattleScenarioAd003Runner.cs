using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class BattleScenarioAd003Runner
{
    public const string ScenarioId = "AD-003_AirMissileLauncher_TrackAndInterceptDroneScout_RadarComparison";

    private const int Seed = 32345;
    private const float FixedDeltaTime = 0.05f;
    private const float MaxDurationSeconds = 14f;
    private static readonly float3 LauncherPosition = new(0f, 0f, 0f);
    private static readonly float3 DroneEndPosition = new(-25f, 16f, 0f);

    public static BattleScenarioVariant[] CreateDefaultVariants()
    {
        return new[]
        {
            new BattleScenarioVariant
            {
                VariantId = "AD-003-A-NoSupport-DroneScout",
                Label = "No Support / Drone Scout",
                SupportMode = BattleScenarioSupportMode.None,
                IncomingThreatKind = BattleScenarioIncomingThreatKind.Drone,
                IncomingThreatSpeedMultiplier = 0.8f,
                IncomingThreatStartDistance = 150f,
                IncomingThreatAltitude = 16f,
                LauncherCount = 1,
                RadarDistanceFromLauncher = 0f,
                ExpectedOutcome = BattleScenarioExpectedOutcome.Baseline
            },
            new BattleScenarioVariant
            {
                VariantId = "AD-003-B-RadarNear-DroneScout",
                Label = "Radar Near / Drone Scout",
                SupportMode = BattleScenarioSupportMode.RadarNear,
                IncomingThreatKind = BattleScenarioIncomingThreatKind.Drone,
                IncomingThreatSpeedMultiplier = 0.8f,
                IncomingThreatStartDistance = 150f,
                IncomingThreatAltitude = 16f,
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
            throw new ArgumentException($"Unsupported AD-003 scenario id '{definition.ScenarioId}'.", nameof(definition));

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
        Entity airTarget = BattleScenarioEcsSpawnHelpers.CreateAirTarget(
            em,
            new float3(variant.IncomingThreatStartDistance, variant.IncomingThreatAltitude, 0f),
            FactionIdentity.EnemyFactionId,
            80,
            variant.IncomingThreatAltitude,
            5f);
        if (variant.SupportMode == BattleScenarioSupportMode.RadarNear)
            CreateRadarSupportProvider(em, variant.RadarDistanceFromLauncher);

        SystemHandle supportSystem = world.CreateSystem<AirMissileLauncherSupportLinkSystem>();
        SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
        SystemHandle airImpactSystem = world.CreateSystem<AirMissileImpactSystem>();

        return BattleScenarioFixedStepRunner.RunVariant(
            ScenarioId,
            variant,
            seed,
            fixedDeltaTime,
            maxDurationSeconds,
            (state, metrics) =>
            {
                world.SetTime(new TimeData(state.TimeSeconds, state.FixedDeltaTime));
                MoveDroneTarget(em, airTarget, variant, state);
                UpdateIfHasWork(world, supportSystem);
                UpdateIfHasWork(world, acquisitionSystem);
                CaptureLauncherMetrics(em, launcher, airTarget, metrics, state);
                UpdateIfHasWork(world, fireControlSystem);
                CaptureLauncherMetrics(em, launcher, airTarget, metrics, state);
                CaptureProjectileMetrics(em, airTarget, metrics);
                UpdateIfHasWork(world, homingSystem);
                UpdateIfHasWork(world, airImpactSystem);
                CaptureProjectileMetrics(em, airTarget, metrics);

                if (!em.Exists(airTarget) || !em.HasComponent<UnitHealth>(airTarget))
                {
                    metrics.FailureReason = BattleScenarioFailureReason.TargetEntityMissing;
                    return BattleScenarioStepOutcome.Failed;
                }

                UnitHealth health = em.GetComponentData<UnitHealth>(airTarget);
                if (health.Current <= 0)
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
               comparison.RadarImprovedDetectionTime &&
               comparison.RadarImprovedLockTime &&
               comparison.RadarImprovedOrMatchedOutcome;
    }

    private static void MoveDroneTarget(
        EntityManager em,
        Entity airTarget,
        BattleScenarioVariant variant,
        BattleScenarioFixedStepState state)
    {
        if (!em.Exists(airTarget) || !em.HasComponent<LocalTransform>(airTarget))
            return;

        LocalTransform transform = em.GetComponentData<LocalTransform>(airTarget);
        if (em.HasComponent<UnitPrevWorldPos>(airTarget))
            em.SetComponentData(airTarget, new UnitPrevWorldPos { Value = transform.Position });

        float duration = 8f / math.max(0.1f, variant.IncomingThreatSpeedMultiplier);
        float t = math.saturate(state.TimeSeconds / duration);
        float3 start = new(variant.IncomingThreatStartDistance, variant.IncomingThreatAltitude, 0f);
        transform.Position = math.lerp(start, DroneEndPosition, t);
        em.SetComponentData(airTarget, transform);
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
                AirTargetPriority = 100f,
                IncomingMissilePriority = 25f,
                TurretYawSpeedDegreesPerSecond = 900f,
                AimToleranceDegrees = 5f,
                LockSeconds = 0.9f,
                LaunchDelaySeconds = 0.1f,
                ReloadSeconds = 1.5f,
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
        Entity airTarget,
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
            if (em.Exists(airTarget) && em.HasComponent<LocalTransform>(airTarget))
            {
                float3 targetPosition = em.GetComponentData<LocalTransform>(airTarget).Position;
                metrics.IncomingThreatDistanceAtDetection = math.distance(LauncherPosition, targetPosition);
            }
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
        Entity airTarget,
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

        if (!em.Exists(airTarget) || !em.HasComponent<LocalTransform>(airTarget))
            return;

        float3 targetPosition = em.GetComponentData<LocalTransform>(airTarget).Position;
        using Unity.Collections.NativeArray<LocalTransform> transforms =
            projectileQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < transforms.Length; i++)
        {
            float distance = math.distance(transforms[i].Position, targetPosition);
            if (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat)
                metrics.ClosestInterceptorDistanceToThreat = distance;
        }
    }
}
