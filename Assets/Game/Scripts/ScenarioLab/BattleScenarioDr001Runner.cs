using System;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    public static class BattleScenarioDr001Runner
    {
        public const string ScenarioId = "DR-001_DroneReconDetectionAndThreatWarning";

        private const int Seed = 301001;
        private const float FixedDeltaTime = 0.05f;
        private const float MaxDurationSeconds = 2f;
        private const int DetectorRadiusCells = 8;
        private const float CellSize = 4f;
        private static readonly int2 SensorCell = new(10, 10);
        private static readonly int2 DroneOutsideCell = new(24, 10);
        private static readonly int2 DroneDetectedCell = new(17, 10);

        public static BattleScenarioVariant[] CreateDefaultVariants()
        {
            return new[]
            {
                new BattleScenarioVariant
                {
                    VariantId = "DR-001-A-DroneAirWarning",
                    Label = "Drone Recon / Air Threat Warning",
                    SupportMode = BattleScenarioSupportMode.None,
                    IncomingThreatKind = BattleScenarioIncomingThreatKind.Drone,
                    IncomingThreatSpeedMultiplier = 1f,
                    IncomingThreatStartDistance = DetectorRadiusCells + 6f,
                    IncomingThreatAltitude = 16f,
                    LauncherCount = 1,
                    RadarDistanceFromLauncher = DetectorRadiusCells,
                    ExpectedOutcome = BattleScenarioExpectedOutcome.Baseline
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
                throw new ArgumentException($"Unsupported DR-001 scenario id '{definition.ScenarioId}'.", nameof(definition));

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
            CreateGrid(em);
            CreateRuntimeGameplayState(em);
            CreateAirThreatDetector(em);
            Entity drone = CreateIncomingDroneThreat(em, DroneOutsideCell);

            SystemHandle warningSystem = world.CreateSystem<ThreatDetectionWarningSystem>();
            using EntityQuery warningStateQuery = ThreatWarningRuntimeState.CreateQuery(em, readOnly: true);

            bool outOfRangeTickObserved = false;
            BattleScenarioMetrics metrics = BattleScenarioFixedStepRunner.RunVariant(
                ScenarioId,
                variant,
                seed,
                fixedDeltaTime,
                maxDurationSeconds,
                (state, metrics) =>
                {
                    world.SetTime(new TimeData(state.TimeSeconds, state.FixedDeltaTime));

                    if (state.Frame == 1)
                        MoveDroneIntoDetectorRange(em, drone);

                    UpdateIfHasWork(world, warningSystem);

                    bool hasWarningState = ThreatWarningRuntimeState.TryRead(
                        em,
                        warningStateQuery,
                        out ThreatWarningRuntimeStateComponent warningState);
                    bool hasPendingWarning = hasWarningState && warningState.HasPendingWarning != 0;
                    if (state.Frame == 0 && !hasPendingWarning)
                    {
                        outOfRangeTickObserved = true;
                        metrics.TrackingStarted = true;
                        metrics.TrackingStartTimeSeconds = state.TimeSeconds;
                    }

                    if (hasPendingWarning)
                    {
                        CaptureWarningMetrics(metrics, state, warningState);
                        metrics.Intercepted = outOfRangeTickObserved;
                        metrics.InterceptTimeSeconds = state.TimeSeconds;
                        metrics.FailureReason = metrics.Intercepted
                            ? BattleScenarioFailureReason.None
                            : BattleScenarioFailureReason.NoDetection;
                        return metrics.Intercepted
                            ? BattleScenarioStepOutcome.Complete
                            : BattleScenarioStepOutcome.Failed;
                    }

                    return BattleScenarioStepOutcome.Continue;
                });
            return metrics;
        }

        private static bool EvaluateResult(BattleScenarioResult result)
        {
            if (result.Variants.Length != 1 || result.Comparisons.Length != 0)
                return false;

            BattleScenarioMetrics metrics = result.Variants[0];
            return metrics.TrackingStarted &&
                   metrics.Detected &&
                   metrics.Locked &&
                   metrics.Intercepted &&
                   metrics.IncomingThreatDistanceAtDetection <= DetectorRadiusCells &&
                   metrics.InterceptDistanceFromDefendedTarget > 0f &&
                   metrics.FailureReason == BattleScenarioFailureReason.None;
        }

        private static void CreateGrid(EntityManager em)
        {
            Entity entity = em.CreateEntity(typeof(GridConfig));
            em.SetComponentData(entity, new GridConfig
            {
                Width = 64,
                Height = 64,
                CellSize = CellSize,
                Origin = float3.zero
            });
        }

        private static void CreateRuntimeGameplayState(EntityManager em)
        {
            Entity entity = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(entity, new RuntimeGameplayStateComponent
            {
                PlayRequested = 1,
                SimulationActive = 1
            });
        }

        private static void CreateAirThreatDetector(EntityManager em)
        {
            Entity entity = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(ThreatDetector));
            em.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(entity, new UnitGrid { Cell = SensorCell });
            em.SetComponentData(entity, new UnitHealth { Current = 500, Max = 500 });
            em.SetComponentData(entity, new ThreatDetector
            {
                Kind = (byte)ThreatDetectionKind.Air,
                RadiusCells = DetectorRadiusCells
            });
        }

        private static Entity CreateIncomingDroneThreat(EntityManager em, int2 cell)
        {
            Entity entity = em.CreateEntity(
                typeof(Faction),
                typeof(UnitGrid),
                typeof(UnitHealth),
                typeof(UnitAirMovement),
                typeof(UnitMove),
                typeof(UnitPathRequest));
            em.SetComponentData(entity, new Faction { Id = FactionIdentity.EnemyFactionId });
            em.SetComponentData(entity, new UnitGrid { Cell = cell });
            em.SetComponentData(entity, new UnitHealth { Current = 80, Max = 80 });
            em.SetComponentData(entity, new UnitAirMovement
            {
                CruiseHeight = 16f,
                RunwayTaxiSpeed = 5f
            });
            em.SetComponentData(entity, new UnitMove
            {
                Speed = 8f,
                WalkSpeed = 8f,
                RoadSpeedMultiplier = 1f,
                ArriveDistance = 0.25f
            });
            UnitMoveOrderRequestSystem.SetPathRequest(em, entity, SensorCell);
            return entity;
        }

        private static void MoveDroneIntoDetectorRange(EntityManager em, Entity drone)
        {
            if (!em.Exists(drone) || !em.HasComponent<UnitGrid>(drone))
                return;

            em.SetComponentData(drone, new UnitGrid { Cell = DroneDetectedCell });
        }

        private static void CaptureWarningMetrics(
            BattleScenarioMetrics metrics,
            BattleScenarioFixedStepState state,
            ThreatWarningRuntimeStateComponent warningState)
        {
            int cellDistance = math.max(
                math.abs(DroneDetectedCell.x - SensorCell.x),
                math.abs(DroneDetectedCell.y - SensorCell.y));
            metrics.Detected = true;
            metrics.DetectionTimeSeconds = state.TimeSeconds;
            metrics.Locked = warningState.PendingType == ThreatWarningType.Air;
            metrics.LockTimeSeconds = state.TimeSeconds;
            metrics.InterceptorLaunched = warningState.PendingThreatCount == 1;
            metrics.LaunchTimeSeconds = state.TimeSeconds;
            metrics.IncomingThreatDistanceAtDetection = cellDistance;
            metrics.InterceptDistanceFromDefendedTarget = warningState.PendingEtaSeconds;
            metrics.LauncherEffectiveRange = DetectorRadiusCells;
        }

        private static void UpdateIfHasWork(World world, SystemHandle system)
        {
            if (world.Unmanaged.ResolveSystemStateRef(system).ShouldRunSystem())
                system.Update(world.Unmanaged);
        }
    }
}
