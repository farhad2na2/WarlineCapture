using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    public static class BattleScenarioGm001Runner
    {
        public const string ScenarioId = "GM-001_GroundMissileLauncher_FiresVisibleRocketAndDamagesTarget";

        private const int Seed = 201001;
        private const float FixedDeltaTime = 0.05f;
        private const float MaxDurationSeconds = 8f;
        private const int TargetInitialHealth = 300;
        private static readonly float3 LauncherPosition = new(0f, 0f, 0f);
        private static readonly float3 TargetPosition = new(110f, 0f, 0f);

        public static BattleScenarioVariant[] CreateDefaultVariants()
        {
            return new[]
            {
                new BattleScenarioVariant
                {
                    VariantId = "GM-001-A-DirectFireVisibleRocket",
                    Label = "Direct Fire / Visible Rocket",
                    SupportMode = BattleScenarioSupportMode.None,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.GroundMissile,
                    IncomingThreatSpeedMultiplier = 1f,
                    IncomingThreatStartDistance = 110f,
                    IncomingThreatAltitude = 0f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = 0f,
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
                throw new ArgumentException($"Unsupported GM-001 scenario id '{definition.ScenarioId}'.", nameof(definition));

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
            Entity target = BattleScenarioEcsSpawnHelpers.CreateGroundTarget(
                em,
                TargetPosition,
                FactionIdentity.PlayerFactionId,
                TargetInitialHealth);
            Entity launcher = CreateArmedLauncher(em, target);
            CreateRocketVisualSlot(em, launcher);

            SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();
            SystemHandle flyingRocketVisualSystem = world.CreateSystem<GroundMissileFlyingRocketVisualSystem>();
            SystemHandle projectileFlightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
            SystemHandle impactSystem = world.CreateSystem<GroundMissileImpactSystem>();

            return BattleScenarioFixedStepRunner.RunVariant(
                ScenarioId,
                variant,
                seed,
                fixedDeltaTime,
                maxDurationSeconds,
                (state, metrics) =>
                {
                    world.SetTime(new TimeData(state.TimeSeconds, state.FixedDeltaTime));
                    CaptureLauncherState(em, launcher, metrics, state);
                    UpdateIfHasWork(world, fireSystem);
                    CaptureLaunchState(em, launcher, target, metrics, state);
                    UpdateIfHasWork(world, flyingRocketVisualSystem);
                    CaptureFlyingRocketState(em, metrics);
                    UpdateIfHasWork(world, projectileFlightSystem);
                    CaptureLaunchState(em, launcher, target, metrics, state);
                    UpdateIfHasWork(world, impactSystem);
                    CaptureDamageState(em, target, metrics, state);

                    return metrics.Intercepted
                        ? BattleScenarioStepOutcome.Complete
                        : BattleScenarioStepOutcome.Continue;
                });
        }

        private static bool EvaluateResult(BattleScenarioResult result)
        {
            if (result.Variants.Length != 1 || result.Comparisons.Length != 0)
                return false;

            BattleScenarioMetrics metrics = result.Variants[0];
            return metrics.Detected &&
                   metrics.TrackingStarted &&
                   metrics.Locked &&
                   metrics.InterceptorLaunched &&
                   metrics.Intercepted &&
                   metrics.LauncherEffectiveTrackingQuality > 0f &&
                   metrics.ClosestInterceptorDistanceToThreat >= 0f &&
                   metrics.FailureReason == BattleScenarioFailureReason.None;
        }

        private static Entity CreateArmedLauncher(EntityManager em, Entity target)
        {
            var config = new GroundMissileLauncherComponent
            {
                MinRange = 8f,
                MaxRange = 180f,
                PrepareSeconds = 0.1f,
                ReloadSeconds = 1f,
                BatteryElevatedAngleDegrees = 35f,
                RocketSpeed = 95f,
                ArcHeight = 24f,
                DamageRadius = 10f,
                Damage = 140
            };
            var state = new GroundMissileLauncherStateComponent
            {
                Phase = (byte)GroundMissileLauncherPhase.Preparing,
                TargetEntity = target,
                TargetCell = default,
                TargetWorldPosition = TargetPosition,
                Timer = GroundMissileLauncherTiming.PrepareAndHoldSeconds(config.PrepareSeconds),
                SelectedRocketSlot = 0
            };
            return BattleScenarioEcsSpawnHelpers.CreateGroundMissileLauncher(
                em,
                LauncherPosition,
                FactionIdentity.EnemyFactionId,
                400,
                config,
                state);
        }

        private static void CreateRocketVisualSlot(EntityManager em, Entity launcher)
        {
            Entity rocket = em.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld));
            float3 rocketPosition = LauncherPosition + new float3(0f, 1.4f, 0.75f);
            quaternion rocketRotation = quaternion.identity;
            em.SetComponentData(rocket, LocalTransform.FromPositionRotationScale(rocketPosition, rocketRotation, 1f));
            em.SetComponentData(rocket, new LocalToWorld
            {
                Value = float4x4.TRS(rocketPosition, rocketRotation, new float3(1f))
            });

            DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets =
                em.AddBuffer<GroundMissileLauncherRocketVisualComponent>(launcher);
            rockets.Add(new GroundMissileLauncherRocketVisualComponent
            {
                Rocket = rocket,
                SlotIndex = 0,
                InitialLocalPosition = rocketPosition,
                InitialLocalRotation = rocketRotation,
                InitialLocalScale = 1f
            });
        }

        private static void CaptureLauncherState(
            EntityManager em,
            Entity launcher,
            BattleScenarioMetrics metrics,
            BattleScenarioFixedStepState state)
        {
            if (!em.Exists(launcher) || !em.HasComponent<GroundMissileLauncherStateComponent>(launcher))
                return;

            GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
            if (!metrics.Detected && launcherState.TargetEntity != Entity.Null)
            {
                metrics.Detected = true;
                metrics.DetectionTimeSeconds = state.TimeSeconds;
                metrics.IncomingThreatDistanceAtDetection = math.distance(LauncherPosition, launcherState.TargetWorldPosition);
            }

            if (!metrics.TrackingStarted && launcherState.Phase == (byte)GroundMissileLauncherPhase.Preparing)
            {
                metrics.TrackingStarted = true;
                metrics.TrackingStartTimeSeconds = state.TimeSeconds;
            }

            if (!metrics.Locked &&
                (launcherState.Phase == (byte)GroundMissileLauncherPhase.Launching ||
                 launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading))
            {
                metrics.Locked = true;
                metrics.LockTimeSeconds = state.TimeSeconds;
            }
        }

        private static void CaptureLaunchState(
            EntityManager em,
            Entity launcher,
            Entity target,
            BattleScenarioMetrics metrics,
            BattleScenarioFixedStepState state)
        {
            using EntityQuery projectileQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GroundMissileProjectileComponent>(),
                ComponentType.ReadOnly<LocalTransform>());
            if (projectileQuery.CalculateEntityCount() <= 0)
                return;

            if (!metrics.InterceptorLaunched)
            {
                metrics.InterceptorLaunched = true;
                metrics.LaunchTimeSeconds = state.TimeSeconds;
            }

            if (!em.Exists(target) || !em.HasComponent<LocalTransform>(target))
                return;

            float3 targetPosition = em.GetComponentData<LocalTransform>(target).Position;
            using Unity.Collections.NativeArray<LocalTransform> transforms =
                projectileQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);
            for (int i = 0; i < transforms.Length; i++)
            {
                float distance = math.distance(transforms[i].Position, targetPosition);
                if (metrics.ClosestInterceptorDistanceToThreat < 0f || distance < metrics.ClosestInterceptorDistanceToThreat)
                    metrics.ClosestInterceptorDistanceToThreat = distance;
            }

            if (em.Exists(launcher) && em.HasComponent<GroundMissileLauncherStateComponent>(launcher))
                CaptureLauncherState(em, launcher, metrics, state);
        }

        private static void CaptureFlyingRocketState(EntityManager em, BattleScenarioMetrics metrics)
        {
            if (metrics.LauncherEffectiveTrackingQuality > 0f)
                return;

            using EntityQuery flyingVisualQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GroundMissileFlyingRocketVisualComponent>());
            if (flyingVisualQuery.CalculateEntityCount() > 0)
                metrics.LauncherEffectiveTrackingQuality = 1f;
        }

        private static void CaptureDamageState(
            EntityManager em,
            Entity target,
            BattleScenarioMetrics metrics,
            BattleScenarioFixedStepState state)
        {
            if (!em.Exists(target) || !em.HasComponent<UnitHealth>(target))
                return;

            UnitHealth health = em.GetComponentData<UnitHealth>(target);
            int damage = TargetInitialHealth - health.Current;
            if (damage <= 0)
                return;

            metrics.Intercepted = true;
            metrics.InterceptTimeSeconds = state.TimeSeconds;
            metrics.InterceptDistanceFromDefendedTarget = damage;
            metrics.FailureReason = BattleScenarioFailureReason.None;
        }

        private static void UpdateIfHasWork(World world, SystemHandle system)
        {
            if (world.Unmanaged.ResolveSystemStateRef(system).ShouldRunSystem())
                system.Update(world.Unmanaged);
        }
    }
}
