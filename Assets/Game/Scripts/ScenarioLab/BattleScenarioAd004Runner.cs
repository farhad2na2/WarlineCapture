using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    public static class BattleScenarioAd004Runner
    {
        public const string ScenarioId = "AD-004_AirMissileLauncher_InterceptTwoIncomingGroundMissiles_RadarComparison";

        private const int Seed = 42345;
        private const float FixedDeltaTime = 0.05f;
        private const float MaxDurationSeconds = 16f;
        private static readonly float3 LauncherPosition = new(0f, 0f, 0f);
        private static readonly float3 DefendedTargetPosition = new(-40f, 0f, 0f);

        public static BattleScenarioVariant[] CreateDefaultVariants()
        {
            return new[]
            {
                new BattleScenarioVariant
                {
                    VariantId = "AD-004-A-NoSupport-TwoGroundMissiles",
                    Label = "No Support / Two Ground Missiles",
                    SupportMode = BattleScenarioSupportMode.None,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
                    IncomingThreatSpeedMultiplier = 1f,
                    IncomingThreatStartDistance = 135f,
                    IncomingThreatAltitude = 8f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = 0f,
                    ExpectedOutcome = BattleScenarioExpectedOutcome.Baseline
                },
                new BattleScenarioVariant
                {
                    VariantId = "AD-004-B-RadarNear-TwoGroundMissiles",
                    Label = "Radar Near / Two Ground Missiles",
                    SupportMode = BattleScenarioSupportMode.RadarNear,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
                    IncomingThreatSpeedMultiplier = 1f,
                    IncomingThreatStartDistance = 135f,
                    IncomingThreatAltitude = 8f,
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
                throw new ArgumentException($"Unsupported AD-004 scenario id '{definition.ScenarioId}'.", nameof(definition));

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
            Entity[] incomingMissiles = CreateIncomingGroundMissiles(em, variant);
            bool[] incomingWasAlive = { true, true };
            if (variant.SupportMode == BattleScenarioSupportMode.RadarNear)
                CreateRadarSupportProvider(em, variant.RadarDistanceFromLauncher);

            SystemHandle supportSystem = world.CreateSystem<AirMissileLauncherSupportLinkSystem>();
            SystemHandle acquisitionSystem = world.CreateSystem<AirMissileLauncherTargetAcquisitionSystem>();
            SystemHandle fireControlSystem = world.CreateSystem<AirMissileLauncherFireControlSystem>();
            SystemHandle homingSystem = world.CreateSystem<AirMissileHomingProjectileSystem>();
            SystemHandle airImpactSystem = world.CreateSystem<AirMissileImpactSystem>();
            SystemHandle groundFlightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
            SystemHandle groundImpactSystem = world.CreateSystem<GroundMissileImpactSystem>();

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
                    UpdateIfHasWork(world, acquisitionSystem);
                    CaptureLauncherMetrics(em, launcher, incomingMissiles, metrics, state);
                    UpdateIfHasWork(world, fireControlSystem);
                    CaptureLauncherMetrics(em, launcher, incomingMissiles, metrics, state);
                    CaptureProjectileMetrics(em, incomingMissiles, metrics);
                    UpdateIfHasWork(world, homingSystem);
                    UpdateIfHasWork(world, airImpactSystem);
                    UpdateIfHasWork(world, groundImpactSystem);
                    CaptureProjectileMetrics(em, incomingMissiles, metrics);

                    bool allResolved = true;
                    for (int i = 0; i < incomingMissiles.Length; i++)
                    {
                        Entity incomingMissile = incomingMissiles[i];
                        if (em.Exists(incomingMissile) && em.HasComponent<GroundMissileImpactRequestComponent>(incomingMissile))
                        {
                            metrics.IncomingThreatImpacted = true;
                            metrics.IncomingThreatImpactTimeSeconds = state.TimeSeconds;
                            metrics.FailureReason = BattleScenarioFailureReason.IncomingThreatImpactedTarget;
                            return BattleScenarioStepOutcome.Failed;
                        }

                        bool incomingAlive = em.Exists(incomingMissile) && em.HasComponent<GroundMissileProjectileComponent>(incomingMissile);
                        if (incomingWasAlive[i] && !incomingAlive)
                            incomingWasAlive[i] = false;

                        allResolved &= !incomingAlive;
                    }

                    if (allResolved)
                    {
                        metrics.Intercepted = !metrics.IncomingThreatImpacted;
                        metrics.InterceptTimeSeconds = state.TimeSeconds;
                        metrics.FailureReason = metrics.Intercepted
                            ? BattleScenarioFailureReason.None
                            : BattleScenarioFailureReason.IncomingThreatImpactedTarget;
                        return metrics.Intercepted
                            ? BattleScenarioStepOutcome.Complete
                            : BattleScenarioStepOutcome.Failed;
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
                   comparison.RadarImprovedLockTime &&
                   comparison.RadarImprovedOrMatchedOutcome;
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
                    AirTargetPriority = 25f,
                    IncomingMissilePriority = 100f,
                    TurretYawSpeedDegreesPerSecond = 900f,
                    AimToleranceDegrees = 5f,
                    LockSeconds = 0.9f,
                    LaunchDelaySeconds = 0.1f,
                    ReloadSeconds = 1.1f,
                    MissileSpeed = 95f,
                    MissileAcceleration = 0f,
                    MissileTurnRateDegreesPerSecond = 140f,
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
                    EffectiveTurnRateDegreesPerSecond = 140f,
                    SelectedMissileSlot = -1
                },
                new AirDefenseSupportLinkComponent
                {
                    LockTimeMultiplier = 1f
                });
        }

        private static Entity[] CreateIncomingGroundMissiles(EntityManager em, BattleScenarioVariant variant)
        {
            float speed = math.max(0.1f, variant.IncomingThreatSpeedMultiplier);
            float3 firstStart = new(variant.IncomingThreatStartDistance, variant.IncomingThreatAltitude, -14f);
            float3 secondStart = new(variant.IncomingThreatStartDistance + 48f, variant.IncomingThreatAltitude + 2f, 16f);
            return new[]
            {
                BattleScenarioEcsSpawnHelpers.CreateIncomingGroundMissile(
                    em,
                    firstStart,
                    DefendedTargetPosition + new float3(0f, 0f, -4f),
                    FactionIdentity.EnemyFactionId,
                    8f / speed,
                    10f,
                    8f,
                    120),
                BattleScenarioEcsSpawnHelpers.CreateIncomingGroundMissile(
                    em,
                    secondStart,
                    DefendedTargetPosition + new float3(0f, 0f, 5f),
                    FactionIdentity.EnemyFactionId,
                    10.5f / speed,
                    10f,
                    8f,
                    120)
            };
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
            Entity[] incomingMissiles,
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
                metrics.IncomingThreatDistanceAtDetection = FindClosestActiveMissileDistance(em, incomingMissiles);
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
            Entity[] incomingMissiles,
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

            using Unity.Collections.NativeArray<LocalTransform> projectileTransforms =
                projectileQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < projectileTransforms.Length; i++)
            {
                float distance = FindClosestMissileDistanceToPosition(em, incomingMissiles, projectileTransforms[i].Position);
                if (distance >= 0f &&
                    (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat))
                {
                    metrics.ClosestInterceptorDistanceToThreat = distance;
                }
            }
        }

        private static float FindClosestActiveMissileDistance(EntityManager em, Entity[] incomingMissiles)
        {
            float closest = -1f;
            for (int i = 0; i < incomingMissiles.Length; i++)
            {
                Entity incomingMissile = incomingMissiles[i];
                if (!em.Exists(incomingMissile) || !em.HasComponent<LocalTransform>(incomingMissile))
                    continue;

                float3 missilePosition = em.GetComponentData<LocalTransform>(incomingMissile).Position;
                float distance = math.distance(LauncherPosition, missilePosition);
                if (closest < 0f || distance < closest)
                    closest = distance;
            }

            return closest;
        }

        private static float FindClosestMissileDistanceToPosition(EntityManager em, Entity[] incomingMissiles, float3 position)
        {
            float closest = -1f;
            for (int i = 0; i < incomingMissiles.Length; i++)
            {
                Entity incomingMissile = incomingMissiles[i];
                if (!em.Exists(incomingMissile) || !em.HasComponent<LocalTransform>(incomingMissile))
                    continue;

                float distance = math.distance(position, em.GetComponentData<LocalTransform>(incomingMissile).Position);
                if (closest < 0f || distance < closest)
                    closest = distance;
            }

            return closest;
        }
    }
}
