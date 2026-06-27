using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public static class BattleScenarioAd010Runner
{
    public const string ScenarioId = "AD-010_AirMissileLauncher_InterceptionGeometrySweep";

    private const int Seed = 102345;
    private const float FixedDeltaTime = 0.05f;
    private const float MaxDurationSeconds = 18f;
    private static readonly float3 LauncherPosition = new(0f, 0f, 0f);

    public static BattleScenarioVariant[] CreateDefaultVariants()
    {
        return new[]
        {
            CreateVariant("AD-010-A-HeadOn", "Head-On Shot"),
            CreateVariant("AD-010-B-SideShot", "Side Shot"),
            CreateVariant("AD-010-C-TailChase", "Tail Chase"),
            CreateVariant("AD-010-D-CrossingShot", "Crossing Shot")
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
            throw new ArgumentException($"Unsupported AD-010 scenario id '{definition.ScenarioId}'.", nameof(definition));

        BattleScenarioVariant[] variants = definition.ScenarioVariants;
        if (variants.Length == 0)
            variants = CreateDefaultVariants();

        return RunVariants(
            variants,
            definition.RandomSeed,
            definition.FixedDeltaTime,
            definition.MaxDurationSeconds);
    }

    private static BattleScenarioVariant CreateVariant(string variantId, string label)
    {
        return new BattleScenarioVariant
        {
            VariantId = variantId,
            Label = label,
            SupportMode = BattleScenarioSupportMode.RadarNear,
            IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
            IncomingThreatSpeedMultiplier = 1f,
            IncomingThreatStartDistance = 180f,
            IncomingThreatAltitude = 10f,
            LauncherCount = 1,
            RadarDistanceFromLauncher = 8f,
            ExpectedOutcome = BattleScenarioExpectedOutcome.MustIntercept
        };
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
            Comparisons = Array.Empty<BattleScenarioComparison>(),
            FailureReason = BattleScenarioFailureReason.None
        };

        for (int i = 0; i < variants.Length; i++)
            result.Variants[i] = RunVariant(variants[i], seed, fixedDeltaTime, maxDurationSeconds);

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
        ResolveGeometry(variant.VariantId, out float3 start, out float3 target, out float durationSeconds);

        using World world = new($"BattleScenarioLab_{variant.VariantId}");
        EntityManager em = world.EntityManager;
        Entity launcher = CreateLauncher(em);
        Entity incomingMissile = BattleScenarioEcsSpawnHelpers.CreateIncomingGroundMissile(
            em,
            start,
            target,
            FactionIdentity.EnemyFactionId,
            durationSeconds / math.max(0.1f, variant.IncomingThreatSpeedMultiplier),
            10f,
            8f,
            120);
        CreateRadarSupportProvider(em, variant.RadarDistanceFromLauncher);

        SystemHandle supportSystem = world.CreateSystem<AirMissileLauncherSupportLinkSystem>();
        SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
        SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
        SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
        SystemHandle airImpactSystem = world.CreateSystem<AirMissileImpactSystem>();
        SystemHandle groundFlightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
        SystemHandle groundImpactSystem = world.CreateSystem<GroundMissileImpactSystem>();

        bool incomingWasAlive = true;
        float3 lastThreatPosition = start;
        return BattleScenarioFixedStepRunner.RunVariant(
            ScenarioId,
            variant,
            seed,
            fixedDeltaTime,
            maxDurationSeconds,
            (state, metrics) =>
            {
                world.SetTime(new TimeData(state.TimeSeconds, state.FixedDeltaTime));
                UpdateIfHasWork(world, supportSystem);
                UpdateIfHasWork(world, groundFlightSystem);
                lastThreatPosition = CaptureLauncherMetrics(em, launcher, incomingMissile, metrics, state, lastThreatPosition);
                UpdateIfHasWork(world, acquisitionSystem);
                lastThreatPosition = CaptureLauncherMetrics(em, launcher, incomingMissile, metrics, state, lastThreatPosition);
                UpdateIfHasWork(world, fireControlSystem);
                lastThreatPosition = CaptureLauncherMetrics(em, launcher, incomingMissile, metrics, state, lastThreatPosition);
                CaptureProjectileMetrics(em, incomingMissile, metrics);
                UpdateIfHasWork(world, homingSystem);
                UpdateIfHasWork(world, airImpactSystem);
                UpdateIfHasWork(world, groundImpactSystem);
                CaptureProjectileMetrics(em, incomingMissile, metrics);

                bool incomingAlive = em.Exists(incomingMissile) && em.HasComponent<GroundMissileProjectileComponent>(incomingMissile);
                if (incomingWasAlive && !incomingAlive)
                {
                    incomingWasAlive = false;
                    metrics.Intercepted = !metrics.IncomingThreatImpacted;
                    metrics.InterceptTimeSeconds = state.TimeSeconds;
                    metrics.InterceptDistanceFromDefendedTarget = math.distance(lastThreatPosition, target);
                    metrics.FailureReason = metrics.Intercepted
                        ? BattleScenarioFailureReason.None
                        : BattleScenarioFailureReason.IncomingThreatImpactedTarget;
                    return metrics.Intercepted
                        ? BattleScenarioStepOutcome.Complete
                        : BattleScenarioStepOutcome.Failed;
                }

                if (em.Exists(incomingMissile) && em.HasComponent<GroundMissileImpactRequestComponent>(incomingMissile))
                {
                    metrics.IncomingThreatImpacted = true;
                    metrics.IncomingThreatImpactTimeSeconds = state.TimeSeconds;
                    metrics.FailureReason = BattleScenarioFailureReason.IncomingThreatImpactedTarget;
                    return BattleScenarioStepOutcome.Failed;
                }

                return BattleScenarioStepOutcome.Continue;
            });
    }

    private static bool EvaluateResult(BattleScenarioResult result)
    {
        if (result.Variants.Length != 4 || result.Comparisons.Length != 0)
            return false;

        for (int i = 0; i < result.Variants.Length; i++)
        {
            BattleScenarioMetrics metrics = result.Variants[i];
            if (!metrics.RadarProviderUsed ||
                !metrics.Detected ||
                !metrics.TrackingStarted ||
                !metrics.Locked ||
                !metrics.InterceptorLaunched ||
                !metrics.Intercepted ||
                metrics.IncomingThreatImpacted ||
                metrics.FailureReason != BattleScenarioFailureReason.None)
            {
                return false;
            }
        }

        return true;
    }

    private static void ResolveGeometry(
        string variantId,
        out float3 start,
        out float3 target,
        out float durationSeconds)
    {
        if (string.Equals(variantId, "AD-010-B-SideShot", StringComparison.Ordinal))
        {
            start = new float3(0f, 10f, 165f);
            target = new float3(0f, 0f, -65f);
            durationSeconds = 8.5f;
            return;
        }

        if (string.Equals(variantId, "AD-010-C-TailChase", StringComparison.Ordinal))
        {
            start = new float3(-95f, 9f, 0f);
            target = new float3(-230f, 0f, 0f);
            durationSeconds = 7f;
            return;
        }

        if (string.Equals(variantId, "AD-010-D-CrossingShot", StringComparison.Ordinal))
        {
            start = new float3(165f, 11f, -90f);
            target = new float3(-80f, 0f, 90f);
            durationSeconds = 9f;
            return;
        }

        start = new float3(160f, 10f, 0f);
        target = new float3(-50f, 0f, 0f);
        durationSeconds = 8f;
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
                BaseDetectionRange = 170f,
                MaxDetectionRange = 300f,
                AirTargetPriority = 25f,
                IncomingMissilePriority = 100f,
                TurretYawSpeedDegreesPerSecond = 900f,
                AimToleranceDegrees = 5f,
                LockSeconds = 0.8f,
                LaunchDelaySeconds = 0.1f,
                ReloadSeconds = 1.2f,
                MissileSpeed = 120f,
                MissileAcceleration = 0f,
                MissileTurnRateDegreesPerSecond = 180f,
                MissileLifetimeSeconds = 7f,
                ProximityFuseRadius = 5f,
                AirTargetDamage = 120,
                IncomingMissileDamage = 9999,
                TrackingQuality = 0.8f,
                MaxSupportRangeBonus = 140f,
                MaxSupportTrackingBonus = 0.25f
            },
            new AirMissileLauncherStateComponent
            {
                Phase = (byte)AirMissileLauncherPhase.Idle,
                TargetEntity = Entity.Null,
                TargetKind = (byte)AirMissileTargetKind.None,
                EffectiveRange = 170f,
                EffectiveLockSeconds = 0.8f,
                EffectiveTrackingQuality = 0.8f,
                EffectiveTurnRateDegreesPerSecond = 180f,
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
            100f,
            AirDefenseSupportTuning.RadarRangeBonus,
            AirDefenseSupportTuning.RadarLockTimeMultiplier,
            AirDefenseSupportTuning.RadarTrackingBonus,
            AirDefenseSupportTuning.RadarTurnRateBonus);
    }

    private static float3 CaptureLauncherMetrics(
        EntityManager em,
        Entity launcher,
        Entity incomingMissile,
        BattleScenarioMetrics metrics,
        BattleScenarioFixedStepState state,
        float3 lastThreatPosition)
    {
        if (!em.Exists(launcher))
            return lastThreatPosition;

        if (em.Exists(incomingMissile) && em.HasComponent<LocalTransform>(incomingMissile))
            lastThreatPosition = em.GetComponentData<LocalTransform>(incomingMissile).Position;

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
            metrics.IncomingThreatDistanceAtDetection = math.distance(LauncherPosition, lastThreatPosition);
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

        return lastThreatPosition;
    }

    private static void CaptureProjectileMetrics(
        EntityManager em,
        Entity incomingMissile,
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

        if (!em.Exists(incomingMissile) || !em.HasComponent<LocalTransform>(incomingMissile))
            return;

        float3 threatPosition = em.GetComponentData<LocalTransform>(incomingMissile).Position;
        using Unity.Collections.NativeArray<LocalTransform> transforms =
            projectileQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < transforms.Length; i++)
        {
            float distance = math.distance(transforms[i].Position, threatPosition);
            if (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat)
                metrics.ClosestInterceptorDistanceToThreat = distance;
        }
    }
}
