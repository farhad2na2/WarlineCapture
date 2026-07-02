using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    public static class BattleScenarioAd011Runner
    {
        public const string ScenarioId = "AD-011_AirMissileLauncher_TracksAndHitsAirTargetClasses";

        private const int Seed = 112345;
        private const float FixedDeltaTime = 0.05f;
        private const float MaxDurationSeconds = 14f;
        private const float RequiredClosestDistanceMeters = 4.5f;
        private static readonly float3 LauncherPosition = new(0f, 0f, 0f);
        private static readonly float3 PatrolEndPosition = new(-32f, 15f, 0f);
        private static readonly float3 HelicopterEndPosition = new(-20f, 10f, 22f);
        private static readonly float3 DroneEndPosition = new(-26f, 17f, -18f);
        private static readonly float3 AttackRunEndPosition = new(-18f, 8f, 0f);

        public static BattleScenarioVariant[] CreateDefaultVariants()
        {
            return new[]
            {
                new BattleScenarioVariant
                {
                    VariantId = "AD-011-A-RadarNear-JetPatrol",
                    Label = "Radar Near / Jet Patrol",
                    SupportMode = BattleScenarioSupportMode.RadarNear,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.Jet,
                    IncomingThreatSpeedMultiplier = 1.2f,
                    IncomingThreatStartDistance = 155f,
                    IncomingThreatAltitude = 18f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = 8f,
                    ExpectedOutcome = BattleScenarioExpectedOutcome.MustIntercept
                },
                new BattleScenarioVariant
                {
                    VariantId = "AD-011-B-RadarNear-Helicopter",
                    Label = "Radar Near / Helicopter",
                    SupportMode = BattleScenarioSupportMode.RadarNear,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.Helicopter,
                    IncomingThreatSpeedMultiplier = 0.55f,
                    IncomingThreatStartDistance = 115f,
                    IncomingThreatAltitude = 10f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = 8f,
                    ExpectedOutcome = BattleScenarioExpectedOutcome.MustIntercept
                },
                new BattleScenarioVariant
                {
                    VariantId = "AD-011-C-RadarNear-Drone",
                    Label = "Radar Near / Drone",
                    SupportMode = BattleScenarioSupportMode.RadarNear,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.Drone,
                    IncomingThreatSpeedMultiplier = 0.85f,
                    IncomingThreatStartDistance = 145f,
                    IncomingThreatAltitude = 16f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = 8f,
                    ExpectedOutcome = BattleScenarioExpectedOutcome.MustIntercept
                },
                new BattleScenarioVariant
                {
                    VariantId = "AD-011-D-RadarNear-AttackingJet",
                    Label = "Radar Near / Attacking Jet",
                    SupportMode = BattleScenarioSupportMode.RadarNear,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.Jet,
                    IncomingThreatSpeedMultiplier = 1.35f,
                    IncomingThreatStartDistance = 165f,
                    IncomingThreatAltitude = 13f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = 8f,
                    ExpectedOutcome = BattleScenarioExpectedOutcome.MustIntercept
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
                throw new ArgumentException($"Unsupported AD-011 scenario id '{definition.ScenarioId}'.", nameof(definition));

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
            using World world = new($"BattleScenarioLab_{variant.VariantId}");
            EntityManager em = world.EntityManager;
            Entity launcher = CreateLauncher(em);
            Entity airTarget = CreateAirTarget(em, variant);
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
                    MoveAirTarget(em, airTarget, variant, state);
                    UpdateIfHasWork(world, supportSystem);
                    UpdateIfHasWork(world, acquisitionSystem);
                    CaptureLauncherMetrics(em, launcher, airTarget, metrics, state);
                    UpdateIfHasWork(world, fireControlSystem);
                    CaptureLauncherMetrics(em, launcher, airTarget, metrics, state);
                    CaptureProjectileMetrics(em, airTarget, metrics, state.FixedDeltaTime);
                    UpdateIfHasWork(world, homingSystem);
                    CaptureImpactRequestMetrics(em, airTarget, metrics);
                    UpdateIfHasWork(world, airImpactSystem);
                    CaptureProjectileMetrics(em, airTarget, metrics, state.FixedDeltaTime);

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
            if (result.Variants.Length < 4)
                return false;

            for (int i = 0; i < result.Variants.Length; i++)
            {
                BattleScenarioMetrics metrics = result.Variants[i];
                if (!metrics.Detected ||
                    !metrics.TrackingStarted ||
                    !metrics.Locked ||
                    !metrics.InterceptorLaunched ||
                    !metrics.Intercepted ||
                    metrics.ClosestInterceptorDistanceToThreat < 0f ||
                    metrics.ClosestInterceptorDistanceToThreat > RequiredClosestDistanceMeters)
                {
                    return false;
                }
            }

            return true;
        }

        private static Entity CreateAirTarget(EntityManager em, BattleScenarioVariant variant)
        {
            int health = variant.IncomingThreatKind == BattleScenarioIncomingThreatKind.Helicopter ? 130 : 100;
            Entity airTarget = BattleScenarioEcsSpawnHelpers.CreateAirTarget(
                em,
                ResolveStartPosition(variant),
                FactionIdentity.EnemyFactionId,
                health,
                variant.IncomingThreatAltitude,
                ResolveTaxiSpeed(variant));

            if (IsAttackRunVariant(variant))
            {
                em.AddComponentData(airTarget, new UnitAirComponent
                {
                    HomePosition = ResolveStartPosition(variant),
                    HomeCell = default,
                    HomeInitialized = 1,
                    Airborne = 1,
                    AttackRunActive = 1,
                    AttackRunExitPosition = AttackRunEndPosition + new float3(-45f, 8f, 0f)
                });
            }

            return airTarget;
        }

        private static void MoveAirTarget(
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

            float duration = ResolveFlightSeconds(variant);
            float t = math.saturate(state.TimeSeconds / duration);
            float3 start = ResolveStartPosition(variant);
            float3 end = ResolveEndPosition(variant);
            transform.Position = math.lerp(start, end, t);
            em.SetComponentData(airTarget, transform);
        }

        private static float3 ResolveStartPosition(BattleScenarioVariant variant)
        {
            float z = variant.IncomingThreatKind switch
            {
                BattleScenarioIncomingThreatKind.Helicopter => 24f,
                BattleScenarioIncomingThreatKind.Drone => -22f,
                _ => 0f
            };
            return new float3(variant.IncomingThreatStartDistance, variant.IncomingThreatAltitude, z);
        }

        private static float3 ResolveEndPosition(BattleScenarioVariant variant)
        {
            if (IsAttackRunVariant(variant))
                return AttackRunEndPosition;

            return variant.IncomingThreatKind switch
            {
                BattleScenarioIncomingThreatKind.Helicopter => HelicopterEndPosition,
                BattleScenarioIncomingThreatKind.Drone => DroneEndPosition,
                _ => PatrolEndPosition
            };
        }

        private static float ResolveFlightSeconds(BattleScenarioVariant variant)
        {
            float baseSeconds = variant.IncomingThreatKind switch
            {
                BattleScenarioIncomingThreatKind.Helicopter => 11f,
                BattleScenarioIncomingThreatKind.Drone => 9f,
                _ => 8f
            };
            return baseSeconds / math.max(0.1f, variant.IncomingThreatSpeedMultiplier);
        }

        private static float ResolveTaxiSpeed(BattleScenarioVariant variant)
        {
            return variant.IncomingThreatKind switch
            {
                BattleScenarioIncomingThreatKind.Helicopter => 3f,
                BattleScenarioIncomingThreatKind.Drone => 4f,
                _ => 6f
            };
        }

        private static bool IsAttackRunVariant(BattleScenarioVariant variant)
        {
            return variant.VariantId != null &&
                   variant.VariantId.IndexOf("Attacking", StringComparison.OrdinalIgnoreCase) >= 0;
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
                    MissileTurnRateDegreesPerSecond = 170f,
                    MissileLifetimeSeconds = 5f,
                    ProximityFuseRadius = 4f,
                    AirTargetDamage = 140,
                    IncomingMissileDamage = 9999,
                    TrackingQuality = 0.8f,
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
                    EffectiveTrackingQuality = 0.8f,
                    EffectiveTurnRateDegreesPerSecond = 170f,
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
                LauncherPosition + new float3(distanceFromLauncher, 0f, -12f),
                FactionIdentity.PlayerFactionId,
                AirDefenseSupportProviderKind.Radar,
                1,
                95f,
                AirDefenseSupportTuning.RadarRangeBonus,
                AirDefenseSupportTuning.RadarLockTimeMultiplier,
                AirDefenseSupportTuning.RadarTrackingBonus,
                AirDefenseSupportTuning.RadarTurnRateBonus);
        }

        private static void UpdateIfHasWork(World world, SystemHandle system)
        {
            if (world.Unmanaged.ResolveSystemStateRef(system).ShouldRunSystem())
                system.Update(world.Unmanaged);
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
            BattleScenarioMetrics metrics,
            float fixedDeltaTime)
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
            float3 previousTargetPosition = em.HasComponent<UnitPrevWorldPos>(airTarget)
                ? em.GetComponentData<UnitPrevWorldPos>(airTarget).Value
                : targetPosition;
            using Unity.Collections.NativeArray<AirMissileProjectileComponent> projectiles =
                projectileQuery.ToComponentDataArray<AirMissileProjectileComponent>(Unity.Collections.Allocator.Temp);
            using Unity.Collections.NativeArray<LocalTransform> transforms =
                projectileQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < transforms.Length; i++)
            {
                float3 projectilePosition = transforms[i].Position;
                float3 previousProjectilePosition = projectilePosition - projectiles[i].Velocity * math.max(0f, fixedDeltaTime);
                float distance = SegmentDistance(
                    previousProjectilePosition,
                    projectilePosition,
                    previousTargetPosition,
                    targetPosition);
                if (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat)
                    metrics.ClosestInterceptorDistanceToThreat = distance;
            }
        }

        private static void CaptureImpactRequestMetrics(
            EntityManager em,
            Entity airTarget,
            BattleScenarioMetrics metrics)
        {
            using EntityQuery impactQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<AirMissileImpactRequestComponent>());
            if (impactQuery.CalculateEntityCount() <= 0)
                return;

            using Unity.Collections.NativeArray<AirMissileImpactRequestComponent> requests =
                impactQuery.ToComponentDataArray<AirMissileImpactRequestComponent>(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < requests.Length; i++)
            {
                AirMissileImpactRequestComponent request = requests[i];
                if (request.Target != airTarget || !math.isfinite(request.VisualSeparation))
                    continue;

                float distance = math.max(0f, request.VisualSeparation);
                if (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat)
                    metrics.ClosestInterceptorDistanceToThreat = distance;
            }
        }

        private static float SegmentDistance(float3 p1, float3 q1, float3 p2, float3 q2)
        {
            float3 d1 = q1 - p1;
            float3 d2 = q2 - p2;
            float3 r = p1 - p2;
            float a = math.lengthsq(d1);
            float e = math.lengthsq(d2);
            float f = math.dot(d2, r);
            float s;
            float t;

            if (a <= 1e-6f && e <= 1e-6f)
            {
                s = 0f;
                t = 0f;
            }
            else if (a <= 1e-6f)
            {
                s = 0f;
                t = math.saturate(f / e);
            }
            else
            {
                float c = math.dot(d1, r);
                if (e <= 1e-6f)
                {
                    t = 0f;
                    s = math.saturate(-c / a);
                }
                else
                {
                    float b = math.dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom != 0f ? math.saturate((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;

                    if (t < 0f)
                    {
                        t = 0f;
                        s = math.saturate(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = math.saturate((b - c) / a);
                    }
                }
            }

            float3 closest1 = p1 + d1 * s;
            float3 closest2 = p2 + d2 * t;
            return math.distance(closest1, closest2);
        }
    }
}
