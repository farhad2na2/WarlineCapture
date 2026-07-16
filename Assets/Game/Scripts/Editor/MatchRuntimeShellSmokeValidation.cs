using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.UI.Contracts;
using Game.UI.Shell.Ecs;
using Game.UI.Shell.Contracts.Ecs;
using Game.Components;
using Game.Configs;
using Game.UI.Runtime;
using Game.Runtime;

namespace Game.Editor
{
    public static class MatchRuntimeShellSmokeValidation
    {
        private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
        private const string InitialUnitsConfigPath = "Assets/Game/Configs/Scene/MatchSubScene_InitialUnitsSpawner_Config.asset";
        private const string MatchSceneName = "Match";
        private const string MatchHudContentName = "SCN08_MatchHudContent";
        private const string ActiveKey = "MatchRuntimeShellSmokeValidation.Active";
        private const string PhaseKey = "MatchRuntimeShellSmokeValidation.Phase";
        private const string StartedAtKey = "MatchRuntimeShellSmokeValidation.StartedAt";
        private const string ErrorCountKey = "MatchRuntimeShellSmokeValidation.ErrorCount";
        private const string RequireFrameDiagKey = "MatchRuntimeShellSmokeValidation.RequireFrameDiag";
        private const string RequireAirMissileSmokeKey = "MatchRuntimeShellSmokeValidation.RequireAirMissileSmoke";
        private const string RequireBaselineMetricsKey = "MatchRuntimeShellSmokeValidation.RequireBaselineMetrics";
        private const string RequirePerformanceRegressionReportKey = "MatchRuntimeShellSmokeValidation.RequirePerformanceRegressionReport";
        private const string RequireInitialBuildingSmokeKey = "MatchRuntimeShellSmokeValidation.RequireInitialBuildingSmoke";
        private const string RequireFuelReadinessKey = "MatchRuntimeShellSmokeValidation.RequireFuelReadiness";
        private const string RequireResourceHaulerMovementKey = "MatchRuntimeShellSmokeValidation.RequireResourceHaulerMovement";
        private const string EvidenceCommitKey = "MatchRuntimeShellSmokeValidation.EvidenceCommit";
        private const string EvidenceEnvironmentKey = "MatchRuntimeShellSmokeValidation.EvidenceEnvironment";
        private const string EvidenceDirtyKey = "MatchRuntimeShellSmokeValidation.EvidenceDirty";
        private const string InitialBuildingImmediateStatusKey = "MatchRuntimeShellSmokeValidation.InitialBuildingImmediateStatus";
        private const string FrameDiagKey = "MatchRuntimeShellSmokeValidation.FrameDiag";
        private const string ReadyAtKey = "MatchRuntimeShellSmokeValidation.ReadyAt";
        private const string LastProgressLogAtKey = "MatchRuntimeShellSmokeValidation.LastProgressLogAt";
        private const string OverrideEnterPlayModeSettingsKey = "MatchRuntimeShellSmokeValidation.OverrideEnterPlayModeSettings";
        private const string PreviousEnterPlayModeOptionsEnabledKey = "MatchRuntimeShellSmokeValidation.PreviousEnterPlayModeOptionsEnabled";
        private const string PreviousEnterPlayModeOptionsKey = "MatchRuntimeShellSmokeValidation.PreviousEnterPlayModeOptions";
        private const string BaselineMetricsReportPath = "/private/tmp/warlinecapture-match-runtime-baseline-metrics.json";
        private const string PerformanceRegressionReportPath = "Design/AgentReports/performance_regression_match_baseline.md";
        private const string PerformanceRegressionMetricsArtifactPath = "Design/AgentReports/performance_regression_match_baseline.json";
        private const string PerformanceRegressionAcceptedBaselinePath = "Design/Architecture/performance_regression_accepted_baseline.json";
        private const double PerformanceRegressionEditorP95FrameBudgetMs = 50d;
        private const double AirMissileSmokeTimeoutSeconds = 20d;
        private const double TimeoutSeconds = 120d;
        private const double ProgressLogIntervalSeconds = 5d;
        private const double StableFrameDiagObservationSeconds = 4d;
        private const double BaselineMetricsObservationSeconds = 4d;
        private const double InitialBuildingPostAiObservationSeconds = 10d;
        private const double FuelReadinessObservationSeconds = 8d;
        private const double ResourceHaulerMovementObservationSeconds = 10d;
        private const double ResourceHaulerMovementMinObservationSeconds = 2d;
        private const float ResourceHaulerMovementMinDistance = 4f;
        private const int ResourceHaulerMovementMinGoalProgressCells = 3;
        private const int BaselineMetricsFrameTarget = 180;
        private const string AirLauncherConfigPath = "Assets/Game/Configs/Weapons/AirMissileLauncher_Air_Config.asset";

        private enum Phase
        {
            Idle = 0,
            WaitingForPlayMode = 1,
            WaitingForShellReady = 2,
            WaitingForMatchReady = 3,
            WaitingForFrameDiag = 4,
            WaitingForAirMissileSmoke = 5,
            WaitingForBaselineMetrics = 6,
            WaitingForInitialBuildingPostAi = 7,
            WaitingForFuelReadiness = 8,
            WaitingForResourceHaulerMovement = 9
        }

        private static Entity _airSmokeLauncher = Entity.Null;
        private static Entity _airSmokeTarget = Entity.Null;
        private static bool _airSmokeProjectileSeen;
        private static bool _airSmokeTrailSeen;
        private static double _airSmokeStartedAt;
        private static Entity _resourceHaulerObservedEntity = Entity.Null;
        private static float3 _resourceHaulerObservedStartPosition;
        private static int2 _resourceHaulerObservedStartCell;
        private static int2 _resourceHaulerObservedGoalCell;
        private static double _resourceHaulerObservedStartedAt;
        private static bool _resourceHaulerScenarioSeeded;
        private static readonly List<double> BaselineFrameTimesMs = new(BaselineMetricsFrameTarget + 16);
        private static double _baselineMetricsStartedAt;
        private static long _baselineMetricsAllocatedBytesAtStart;
        private static int _baselineMetricsLastFrame = -1;
        private static bool _performanceFixtureSeeded;
        private static bool _performanceFixtureReady;
        private static int _performanceFixtureWarmupUntilFrame = -1;
        private static string _performanceFixtureStatus = string.Empty;

        [InitializeOnLoadMethod]
        private static void ResumeActiveValidation()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            RegisterCallbacks();
        }

        public static void Run()
        {
            RunInternal(requireFrameDiag: false);
        }

        public static void RunFrameRateDiagnostics()
        {
            RunInternal(requireFrameDiag: true, requireAirMissileSmoke: false);
        }

        public static void RunAirMissileLauncherSmoke()
        {
            RunInternal(requireFrameDiag: false, requireAirMissileSmoke: true);
        }

        public static void RunBaselineMetrics()
        {
            RunInternal(requireFrameDiag: false, requireAirMissileSmoke: false, requireBaselineMetrics: true);
        }

        public static void RunPerformanceRegressionBaseline()
        {
            ArchitectureEvidenceIdentity evidence = ArchitectureEvidenceIdentityUtility.ResolveIfAvailable(
                Directory.GetCurrentDirectory(),
                new[]
                {
                    Aph700AssemblyDependencyReportGenerator.JsonReportPath,
                    Aph700AssemblyDependencyReportGenerator.MarkdownReportPath
                });
            if (evidence == null)
                throw new InvalidOperationException("Performance evidence requires a Git-bound project checkout.");
            SessionState.SetString(EvidenceCommitKey, evidence.ExactCommit);
            SessionState.SetString(EvidenceEnvironmentKey, evidence.EnvironmentIdentitySha256);
            SessionState.SetBool(EvidenceDirtyKey, evidence.Dirty);
            RunInternal(
                requireFrameDiag: false,
                requireAirMissileSmoke: false,
                requireBaselineMetrics: true,
                requirePerformanceRegressionReport: true,
                requireInitialBuildingSmoke: false);
        }

        public static void RunInitialBuildingSmoke()
        {
            RunInternal(
                requireFrameDiag: false,
                requireAirMissileSmoke: false,
                requireBaselineMetrics: false,
                requireInitialBuildingSmoke: true);
        }

        public static void RunFuelReadiness()
        {
            RunInternal(
                requireFrameDiag: false,
                requireAirMissileSmoke: false,
                requireBaselineMetrics: false,
                requirePerformanceRegressionReport: false,
                requireInitialBuildingSmoke: false,
                requireFuelReadiness: true);
        }

        public static void RunResourceHaulerMovement()
        {
            RunInternal(
                requireFrameDiag: false,
                requireAirMissileSmoke: false,
                requireBaselineMetrics: false,
                requirePerformanceRegressionReport: false,
                requireInitialBuildingSmoke: false,
                requireFuelReadiness: false,
                requireResourceHaulerMovement: true);
        }

        private static void RunInternal(bool requireFrameDiag)
        {
            RunInternal(requireFrameDiag, requireAirMissileSmoke: false, requireBaselineMetrics: false);
        }

        private static void RunInternal(bool requireFrameDiag, bool requireAirMissileSmoke)
        {
            RunInternal(requireFrameDiag, requireAirMissileSmoke, requireBaselineMetrics: false);
        }

        private static void RunInternal(bool requireFrameDiag, bool requireAirMissileSmoke, bool requireBaselineMetrics)
        {
            RunInternal(requireFrameDiag, requireAirMissileSmoke, requireBaselineMetrics, requireInitialBuildingSmoke: false);
        }

        private static void RunInternal(
            bool requireFrameDiag,
            bool requireAirMissileSmoke,
            bool requireBaselineMetrics,
            bool requireInitialBuildingSmoke)
        {
            RunInternal(
                requireFrameDiag,
                requireAirMissileSmoke,
                requireBaselineMetrics,
                requirePerformanceRegressionReport: false,
                requireInitialBuildingSmoke);
        }

        private static void RunInternal(
            bool requireFrameDiag,
            bool requireAirMissileSmoke,
            bool requireBaselineMetrics,
            bool requirePerformanceRegressionReport,
            bool requireInitialBuildingSmoke,
            bool requireFuelReadiness = false,
            bool requireResourceHaulerMovement = false)
        {
            try
            {
                ResetAirMissileSmokeState();
                ResetBaselineMetricsState();
                ResetPerformanceFixtureState();
                ResetResourceHaulerMovementState();
                _resourceHaulerScenarioSeeded = false;
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
                SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(ErrorCountKey, 0);
                SessionState.SetBool(RequireFrameDiagKey, requireFrameDiag);
                SessionState.SetBool(RequireAirMissileSmokeKey, requireAirMissileSmoke);
                SessionState.SetBool(RequireBaselineMetricsKey, requireBaselineMetrics);
                SessionState.SetBool(RequirePerformanceRegressionReportKey, requirePerformanceRegressionReport);
                SessionState.SetBool(RequireInitialBuildingSmokeKey, requireInitialBuildingSmoke);
                SessionState.SetBool(RequireFuelReadinessKey, requireFuelReadiness);
                SessionState.SetBool(RequireResourceHaulerMovementKey, requireResourceHaulerMovement);
                SessionState.EraseString(FrameDiagKey);
                SessionState.EraseString(InitialBuildingImmediateStatusKey);
                SessionState.EraseFloat(ReadyAtKey);
                SessionState.EraseFloat(LastProgressLogAtKey);

                ConfigurePlayModeReloadForBatchValidation();
                RegisterCallbacks();
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false, exception.Message);
            }
        }

        private static void RegisterCallbacks()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceived -= OnLogMessageReceived;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForShellReady);
        }

        private static void Update()
        {
            if (!SessionState.GetBool(ActiveKey, false))
                return;

            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f) > TimeoutSeconds)
            {
                IsMatchRuntimeReady(out string timeoutStatus);
                IsGameplayStableForFrameDiag(out string stableStatus);
                Finish(false, $"Timed out waiting for Match runtime shell smoke validation. {timeoutStatus} stable={stableStatus}");
                return;
            }

            Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);
            LogProgressIfDue(phase, "polling");
            if (phase == Phase.WaitingForPlayMode)
            {
                EnsurePlayModeRequested();
                return;
            }

            if (phase == Phase.WaitingForShellReady)
            {
                if (!TryGetShellState(out UiShellStateComponent shellState) ||
                    shellState.CurrentMode != UiShellMode.MainMenu ||
                    shellState.ActiveRoute != UIRoute.MainMenu ||
                    shellState.IsTransitionRunning != 0)
                {
                    return;
                }

                if (!TryEnqueueMatchRoute(out string enqueueError))
                {
                    Finish(false, enqueueError);
                    return;
                }

                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForMatchReady);
                return;
            }

            if (phase == Phase.WaitingForFrameDiag)
            {
                string frameDiag = SessionState.GetString(FrameDiagKey, string.Empty);
                if (!string.IsNullOrEmpty(frameDiag))
                {
                    Finish(true, frameDiag);
                    return;
                }

                int frameDiagErrorCount = SessionState.GetInt(ErrorCountKey, 0);
                if (frameDiagErrorCount > 0)
                {
                    IsMatchRuntimeReady(out string errorStatus);
                    Finish(false, $"Match runtime stayed ready but logged {frameDiagErrorCount} runtime error(s). status={errorStatus}");
                    return;
                }

                float readyAt = SessionState.GetFloat(ReadyAtKey, 0f);
                if (readyAt <= 0f)
                {
                    SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                    return;
                }

                if (EditorApplication.timeSinceStartup - readyAt >= StableFrameDiagObservationSeconds)
                {
                    IsMatchRuntimeReady(out string readyStatus);
                    IsGameplayStableForFrameDiag(out string stableStatus);
                    Finish(
                        true,
                        $"No low-FPS FrameRateDiag emitted during stable observation window. {readyStatus} stable={stableStatus}");
                    return;
                }

                return;
            }

            if (phase == Phase.WaitingForAirMissileSmoke)
            {
                if (UpdateAirMissileSmoke(out bool complete, out bool failed, out string smokeStatus))
                {
                    Finish(!failed, smokeStatus);
                    return;
                }

                int smokeErrorCount = SessionState.GetInt(ErrorCountKey, 0);
                if (smokeErrorCount > 0)
                {
                    CleanupAirMissileSmoke();
                    Finish(false, $"Air missile smoke logged {smokeErrorCount} runtime error(s). status={smokeStatus}");
                    return;
                }

                if (complete)
                {
                    Finish(true, smokeStatus);
                    return;
                }

                return;
            }

            if (phase == Phase.WaitingForBaselineMetrics)
            {
                if (UpdateBaselineMetrics(out bool complete, out string metricsStatus))
                {
                    Finish(complete, metricsStatus);
                    return;
                }

                int metricsErrorCount = SessionState.GetInt(ErrorCountKey, 0);
                if (metricsErrorCount > 0)
                {
                    IsMatchRuntimeReady(out string errorStatus);
                    Finish(false, $"Match baseline metrics logged {metricsErrorCount} runtime error(s). status={errorStatus}");
                    return;
                }

                return;
            }

            if (phase == Phase.WaitingForInitialBuildingPostAi)
            {
                float readyAt = SessionState.GetFloat(ReadyAtKey, 0f);
                if (readyAt <= 0f)
                {
                    SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                    return;
                }

                if (EditorApplication.timeSinceStartup - readyAt < InitialBuildingPostAiObservationSeconds)
                    return;

                bool passed = ValidateInitialBuildingSmoke(
                    requireNoFaction2OilPump: false,
                    out string postAiStatus);
                string immediateStatus = SessionState.GetString(InitialBuildingImmediateStatusKey, string.Empty);
                Finish(passed, $"{immediateStatus} postAi={postAiStatus}");
                return;
            }

            if (phase == Phase.WaitingForFuelReadiness)
            {
                bool passed = ValidateFuelReadiness(out string fuelStatus);
                if (passed)
                {
                    Finish(true, fuelStatus);
                    return;
                }

                int fuelErrorCount = SessionState.GetInt(ErrorCountKey, 0);
                if (fuelErrorCount > 0)
                {
                    Finish(false, $"Fuel readiness logged {fuelErrorCount} runtime error(s). {fuelStatus}");
                    return;
                }

                float readyAt = SessionState.GetFloat(ReadyAtKey, 0f);
                if (readyAt <= 0f)
                {
                    SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                    return;
                }

                if (EditorApplication.timeSinceStartup - readyAt >= FuelReadinessObservationSeconds)
                {
                    Finish(false, fuelStatus);
                    return;
                }

                return;
            }

            if (phase == Phase.WaitingForResourceHaulerMovement)
            {
                bool passed = ValidateResourceHaulerMovement(out string movementStatus);
                if (passed)
                {
                    Finish(true, movementStatus);
                    return;
                }

                int movementErrorCount = SessionState.GetInt(ErrorCountKey, 0);
                if (movementErrorCount > 0)
                {
                    Finish(false, $"Resource hauler movement logged {movementErrorCount} runtime error(s). {movementStatus}");
                    return;
                }

                float readyAt = SessionState.GetFloat(ReadyAtKey, 0f);
                if (readyAt <= 0f)
                {
                    SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                    return;
                }

                if (EditorApplication.timeSinceStartup - readyAt >= ResourceHaulerMovementObservationSeconds)
                {
                    Finish(false, movementStatus);
                    return;
                }

                return;
            }

            if (phase != Phase.WaitingForMatchReady)
                return;

            bool requireCurtainHidden = !SessionState.GetBool(RequireAirMissileSmokeKey, false);
            if (!IsMatchRuntimeReady(out string status, requireCurtainHidden))
                return;

            int errorCount = SessionState.GetInt(ErrorCountKey, 0);
            if (errorCount > 0)
            {
                Finish(false, $"Match runtime reached ready state but logged {errorCount} runtime error(s). status={status}");
                return;
            }

            if (SessionState.GetBool(RequireFrameDiagKey, false))
            {
                Debug.Log($"[MatchRuntimeShellSmokeValidation] runtimeReady waitingFrameRateDiag {status}");
                SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForFrameDiag);
                return;
            }

            if (SessionState.GetBool(RequireAirMissileSmokeKey, false))
            {
                Debug.Log($"[MatchRuntimeShellSmokeValidation] runtimeReady startingAirMissileSmoke {status}");
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForAirMissileSmoke);
                return;
            }

            if (SessionState.GetBool(RequireBaselineMetricsKey, false))
            {
                Debug.Log($"[MatchRuntimeShellSmokeValidation] runtimeReady collectingBaselineMetrics {status}");
                ResetBaselineMetricsState();
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForBaselineMetrics);
                return;
            }

            if (SessionState.GetBool(RequireInitialBuildingSmokeKey, false))
            {
                bool passed = ValidateInitialBuildingSmoke(
                    requireNoFaction2OilPump: true,
                    out string initialBuildingStatus);
                if (!passed)
                {
                    Finish(false, initialBuildingStatus);
                    return;
                }

                Debug.Log($"[MatchRuntimeShellSmokeValidation] initialBuildingImmediatePassed {initialBuildingStatus}");
                SessionState.SetString(InitialBuildingImmediateStatusKey, initialBuildingStatus);
                SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForInitialBuildingPostAi);
                return;
            }

            if (SessionState.GetBool(RequireFuelReadinessKey, false))
            {
                Debug.Log($"[MatchRuntimeShellSmokeValidation] runtimeReady checkingFuelReadiness {status}");
                SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForFuelReadiness);
                return;
            }

            if (SessionState.GetBool(RequireResourceHaulerMovementKey, false))
            {
                Debug.Log($"[MatchRuntimeShellSmokeValidation] runtimeReady checkingResourceHaulerMovement {status}");
                ResetResourceHaulerMovementState();
                SessionState.SetFloat(ReadyAtKey, (float)EditorApplication.timeSinceStartup);
                SessionState.SetInt(PhaseKey, (int)Phase.WaitingForResourceHaulerMovement);
                return;
            }

            Finish(true, status);
        }

        private static bool ValidateResourceHaulerMovement(out string status)
        {
            status = "[ResourceHaulerMovement] result=Failed world=missing";
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            string scenarioStatus = EnsureResourceHaulerMovementScenario(em);
            if (!TryFindOilResourceHauler(
                    em,
                    out Entity hauler,
                    out LocalTransform transform,
                    out UnitResourceHaulOrder order,
                    out string sourceKey))
            {
                ResetResourceHaulerMovementState();
                status = "[ResourceHaulerMovement] result=Waiting reason=NoOilHaulerWithOrder " + scenarioStatus;
                return false;
            }
            if (!em.HasComponent<UnitGrid>(hauler))
            {
                ResetResourceHaulerMovementState();
                status = "[ResourceHaulerMovement] result=Waiting reason=OilHaulerMissingGrid " + scenarioStatus;
                return false;
            }

            if (_resourceHaulerObservedEntity != hauler ||
                _resourceHaulerObservedEntity == Entity.Null ||
                !em.Exists(_resourceHaulerObservedEntity))
            {
                _resourceHaulerObservedEntity = hauler;
                _resourceHaulerObservedStartPosition = transform.Position;
                _resourceHaulerObservedStartCell = em.GetComponentData<UnitGrid>(hauler).Cell;
                _resourceHaulerObservedGoalCell = order.TargetCell;
                _resourceHaulerObservedStartedAt = EditorApplication.timeSinceStartup;
            }

            float2 start = _resourceHaulerObservedStartPosition.xz;
            float2 current = transform.Position.xz;
            float distance = math.distance(start, current);
            bool hasActivePath = HasActivePath(em, hauler);
            double observedSeconds = EditorApplication.timeSinceStartup - _resourceHaulerObservedStartedAt;
            int2 currentCell = em.GetComponentData<UnitGrid>(hauler).Cell;
            int startGoalDistance = ManhattanDistance(_resourceHaulerObservedStartCell, _resourceHaulerObservedGoalCell);
            int currentGoalDistance = ManhattanDistance(currentCell, _resourceHaulerObservedGoalCell);
            int goalProgressCells = startGoalDistance - currentGoalDistance;
            bool passed = observedSeconds >= ResourceHaulerMovementMinObservationSeconds &&
                          distance >= ResourceHaulerMovementMinDistance &&
                          goalProgressCells >= ResourceHaulerMovementMinGoalProgressCells;
            status =
                "[ResourceHaulerMovement] " +
                $"result={(passed ? "Passed" : "Waiting")} " +
                $"entity={hauler.Index}:{hauler.Version} key='{sourceKey}' " +
                $"phase={(ResourceHaulerUtilitySystemHelper.ResourceHaulPhase)order.Phase} " +
                $"target={order.TargetCell.x},{order.TargetCell.y} activePath={(hasActivePath ? 1 : 0)} " +
                $"distance={distance.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"goalProgressCells={goalProgressCells} " +
                $"observedSeconds={observedSeconds.ToString("0.###", CultureInfo.InvariantCulture)} " +
                DescribeResourceHaulerMovementState(em, hauler) + " " +
                scenarioStatus;
            return passed;
        }

        private static int ManhattanDistance(int2 a, int2 b)
        {
            return math.abs(a.x - b.x) + math.abs(a.y - b.y);
        }

        private static string DescribeResourceHaulerMovementState(EntityManager em, Entity entity)
        {
            var builder = new StringBuilder("state=");
            builder.Append("grid=");
            builder.Append(em.HasComponent<UnitGrid>(entity)
                ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
                : "none");
            builder.Append(",target=");
            builder.Append(em.HasComponent<UnitTarget>(entity)
                ? em.GetComponentData<UnitTarget>(entity).Cell.ToString()
                : "none");
            builder.Append(",request=");
            builder.Append(em.HasComponent<UnitPathRequest>(entity)
                ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString()
                : "none");
            builder.Append(",follow=");
            builder.Append(em.HasComponent<UnitPathFollow>(entity)
                ? em.GetComponentData<UnitPathFollow>(entity).PathIndex.ToString(CultureInfo.InvariantCulture)
                : "none");
            builder.Append(",range=");
            if (em.HasComponent<UnitPathRange>(entity))
            {
                UnitPathRange range = em.GetComponentData<UnitPathRange>(entity);
                builder.Append(range.Start.ToString(CultureInfo.InvariantCulture));
                builder.Append(':');
                builder.Append(range.Length.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append("none");
            }

            builder.Append(",retry=");
            builder.Append(em.HasComponent<UnitPathRetryCooldown>(entity)
                ? em.GetComponentData<UnitPathRetryCooldown>(entity).ResumeFrame.ToString(CultureInfo.InvariantCulture)
                : "none");
            builder.Append(",long=");
            builder.Append(em.HasComponent<UnitLongDistanceMove>(entity)
                ? em.GetComponentData<UnitLongDistanceMove>(entity).FinalGoal.ToString()
                : "none");
            builder.Append(",manual=");
            builder.Append(em.HasComponent<ManualMoveOrderTag>(entity) ? '1' : '0');
            builder.Append(",kinematics=");
            if (em.HasComponent<UnitVehicleKinematics>(entity))
            {
                UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(entity);
                builder.Append(kinematics.CurrentSpeed.ToString("0.###", CultureInfo.InvariantCulture));
                builder.Append('/');
                builder.Append(kinematics.StallSeconds.ToString("0.###", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append("none");
            }

            return builder.ToString();
        }

        private static string EnsureResourceHaulerMovementScenario(EntityManager em)
        {
            if (_resourceHaulerScenarioSeeded)
                return "scenarioSeeded=1";

            if (!TryFindOilResourceHaulerCandidate(
                    em,
                    out byte factionId,
                    out float loadAmount,
                    out string haulerKey,
                    out string haulerStatus))
            {
                return "scenarioSeeded=0 reason=" + haulerStatus;
            }

            if (!TrySeedOilLogisticsStorage(
                    em,
                    factionId,
                    loadAmount,
                    out string storageStatus))
            {
                return $"scenarioSeeded=0 haulerKey='{haulerKey}' reason={storageStatus}";
            }

            _resourceHaulerScenarioSeeded = true;
            return $"scenarioSeeded=1 haulerKey='{haulerKey}' {storageStatus}";
        }

        private static bool TryFindOilResourceHaulerCandidate(
            EntityManager em,
            out byte factionId,
            out float loadAmount,
            out string haulerKey,
            out string status)
        {
            factionId = 0;
            loadAmount = 0f;
            haulerKey = string.Empty;
            status = "NoTrayTruckCandidate";

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitResourceHauler>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>(),
                ComponentType.ReadOnly<Faction>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (em.HasComponent<UnitAirMovement>(entity))
                    continue;

                UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity);
                string key = sourceKey.Value.ToString();
                if (!string.Equals(key, "Unit_Veh_Truck_Tray", StringComparison.Ordinal))
                    continue;

                UnitResourceHauler hauler = em.GetComponentData<UnitResourceHauler>(entity);
                loadAmount = Mathf.Max(1f, hauler.BarrelCapacity);
                factionId = em.GetComponentData<Faction>(entity).Id;
                haulerKey = key;
                status = $"TrayTruckCandidate entity={entity.Index}:{entity.Version} faction={factionId}";
                return true;
            }

            return false;
        }

        private static bool TrySeedOilLogisticsStorage(
            EntityManager em,
            byte factionId,
            float loadAmount,
            out string status)
        {
            status = "NoOilSource";
            Entity sourceEntity = Entity.Null;
            Entity destinationEntity = Entity.Null;
            BuildingResourceStorageComponent source = default;
            BuildingResourceStorageComponent destination = default;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadWrite<BuildingResourceStorageComponent>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                BuildingResourceStorageComponent storage = em.GetComponentData<BuildingResourceStorageComponent>(entity);
                if (storage.OwnerFactionId != factionId)
                    continue;

                if (sourceEntity == Entity.Null &&
                    storage.OilBarrelsPerDay > 0f &&
                    storage.OilStorageCapacity > 0)
                {
                    sourceEntity = entity;
                    source = storage;
                }

                if (destinationEntity == Entity.Null &&
                    storage.FuelBarrelsPerDay > 0f &&
                    storage.OilStorageCapacity > 0)
                {
                    destinationEntity = entity;
                    destination = storage;
                }
            }

            if (sourceEntity == Entity.Null)
                return false;
            if (destinationEntity == Entity.Null)
            {
                status = "NoRefineryDestination";
                return false;
            }

            float requiredSourceOil = loadAmount + source.ReservedOilOutboundBarrels + 0.5f;
            if (source.StoredOilBarrels + 0.001f < requiredSourceOil)
            {
                source.StoredOilBarrels = Mathf.Min(source.OilStorageCapacity, requiredSourceOil);
                source.Version++;
                em.SetComponentData(sourceEntity, source);
            }

            float destinationFree = destination.OilStorageCapacity -
                                    destination.StoredOilBarrels -
                                    destination.ReservedOilInboundBarrels;
            if (destinationFree + 0.001f < loadAmount)
            {
                destination.StoredOilBarrels = Mathf.Max(
                    0f,
                    destination.OilStorageCapacity - destination.ReservedOilInboundBarrels - loadAmount - 0.5f);
                destination.Version++;
                em.SetComponentData(destinationEntity, destination);
            }

            status =
                $"source={sourceEntity.Index}:{sourceEntity.Version} sourceOil={source.StoredOilBarrels.ToString("0.###", CultureInfo.InvariantCulture)}/{source.OilStorageCapacity} " +
                $"destination={destinationEntity.Index}:{destinationEntity.Version} destinationOil={destination.StoredOilBarrels.ToString("0.###", CultureInfo.InvariantCulture)}/{destination.OilStorageCapacity} " +
                $"load={loadAmount.ToString("0.###", CultureInfo.InvariantCulture)} faction={factionId}";
            return true;
        }

        private static bool TryFindOilResourceHauler(
            EntityManager em,
            out Entity hauler,
            out LocalTransform transform,
            out UnitResourceHaulOrder order,
            out string sourceKey)
        {
            hauler = Entity.Null;
            transform = default;
            order = default;
            sourceKey = string.Empty;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitResourceHauler>(),
                ComponentType.ReadOnly<UnitResourceHaulOrder>(),
                ComponentType.ReadOnly<LocalTransform>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                UnitResourceHaulOrder candidateOrder = em.GetComponentData<UnitResourceHaulOrder>(entity);
                if (candidateOrder.ResourceKind != (byte)ResourceHaulerUtilitySystemHelper.ResourceHaulKind.Oil)
                    continue;

                if (!em.HasComponent<UnitVehicleMovement>(entity))
                    continue;

                string candidateKey = em.HasComponent<UnitSourcePrefabKey>(entity)
                    ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
                    : string.Empty;
                hauler = entity;
                transform = em.GetComponentData<LocalTransform>(entity);
                order = candidateOrder;
                sourceKey = candidateKey;
                if (candidateKey.Contains("Truck_Tray", StringComparison.Ordinal))
                    return true;
            }

            return hauler != Entity.Null;
        }

        private static bool HasActivePath(EntityManager em, Entity entity)
        {
            return em.HasComponent<UnitPathRequest>(entity) ||
                   em.HasComponent<UnitPathFollow>(entity) ||
                   em.HasComponent<UnitPathRange>(entity) ||
                   em.HasComponent<UnitPathRetryCooldown>(entity) ||
                   em.HasComponent<UnitLongDistanceMove>(entity);
        }

        private static bool ValidateFuelReadiness(out string status)
        {
            status = "[MatchFuelReadiness] result=Failed world=missing";
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            int configuredInitialFuel = LoadConfiguredInitialFuel();
            int pendingFuelSeedCount = CountEntities<InitialUsableFuelStorageSeedPending>(em);
            CapturePlayerUsableFuelStorage(
                em,
                out int playerUsableStorageCount,
                out int playerFuelStorageCapacity,
                out float playerStoredFuel,
                out float playerReservedFuel,
                out uint playerStorageVersion);
            CapturePlayerUsableFuelSummary(
                em,
                out bool hasUsableFuelSummaryBuffer,
                out bool hasPlayerUsableFuelSummary,
                out int usableFuelSummaryCount,
                out float summaryStoredFuel,
                out int summaryFuelCapacity,
                out uint summaryVersion);

            bool headerRead = UiShellEcsGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header);
            int headerFuel = headerRead ? ParsePositiveIntText(header.FuelText) : 0;
            bool moveAccepted = TryValidateSyntheticFuelMove(em, out string moveStatus);

            bool expectsFuel = configuredInitialFuel > 0;
            bool passed =
                (!expectsFuel || playerStoredFuel > 0.5f) &&
                (!expectsFuel || headerFuel > 0) &&
                (!expectsFuel || moveAccepted);
            status =
                "[MatchFuelReadiness] " +
                $"result={(passed ? "Passed" : "Failed")} " +
                $"configuredInitialFuel={configuredInitialFuel} pendingFuelSeeds={pendingFuelSeedCount} " +
                $"playerUsableStorageCount={playerUsableStorageCount} playerFuelCapacity={playerFuelStorageCapacity} " +
                $"playerStoredFuel={playerStoredFuel.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"playerReservedFuel={playerReservedFuel.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"playerStorageVersion={playerStorageVersion} " +
                $"summaryBuffer={(hasUsableFuelSummaryBuffer ? 1 : 0)} summaryEntries={usableFuelSummaryCount} " +
                $"playerSummary={(hasPlayerUsableFuelSummary ? 1 : 0)} " +
                $"summaryFuel={summaryStoredFuel.ToString("0.###", CultureInfo.InvariantCulture)} " +
                $"summaryFuelCapacity={summaryFuelCapacity} summaryVersion={summaryVersion} " +
                $"headerRead={(headerRead ? 1 : 0)} headerFuelText='{header.FuelText}' headerFuel={headerFuel} " +
                moveStatus;
            return passed;
        }

        private static int LoadConfiguredInitialFuel()
        {
            InitialUnitsSpawnerAuthoringConfig config =
                AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(InitialUnitsConfigPath);
            return config != null ? config.InitialFuel : 0;
        }

        private static int CountEntities<T>(EntityManager em)
            where T : unmanaged, IComponentData
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateEntityCount();
        }

        private static void CapturePlayerUsableFuelStorage(
            EntityManager em,
            out int count,
            out int capacity,
            out float storedFuel,
            out float reservedFuel,
            out uint version)
        {
            count = 0;
            capacity = 0;
            storedFuel = 0f;
            reservedFuel = 0f;
            version = 0u;
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
            if (query.IsEmptyIgnoreFilter)
                return;

            using NativeArray<BuildingResourceStorageComponent> storages =
                query.ToComponentDataArray<BuildingResourceStorageComponent>(Allocator.Temp);
            for (int i = 0; i < storages.Length; i++)
            {
                BuildingResourceStorageComponent storage = storages[i];
                if (!IsPlayerUsableFuelStorage(storage))
                    continue;

                count++;
                capacity += Math.Max(0, storage.FuelStorageCapacity);
                storedFuel += Math.Max(0f, storage.StoredFuelBarrels);
                reservedFuel += Math.Max(0f, storage.ReservedFuelOutboundBarrels);
                version = CombineValidationVersion(version, storage.Version);
            }
        }

        private static bool IsPlayerUsableFuelStorage(in BuildingResourceStorageComponent storage)
        {
            return FactionIdentity.IsPlayerControlled(storage.OwnerFactionId) &&
                   storage.FuelStorageCapacity > 0 &&
                   storage.FuelBarrelsPerDay <= 0f &&
                   storage.OilBarrelsPerDay <= 0f;
        }

        private static void CapturePlayerUsableFuelSummary(
            EntityManager em,
            out bool hasBuffer,
            out bool hasPlayerSummary,
            out int entryCount,
            out float storedFuel,
            out int fuelCapacity,
            out uint version)
        {
            hasBuffer = false;
            hasPlayerSummary = false;
            entryCount = 0;
            storedFuel = 0f;
            fuelCapacity = 0;
            version = 0u;
            using EntityQuery boundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
            if (boundaryQuery.IsEmptyIgnoreFilter)
                return;

            using NativeArray<Entity> boundaries = boundaryQuery.ToEntityArray(Allocator.Temp);
            for (int boundaryIndex = 0; boundaryIndex < boundaries.Length; boundaryIndex++)
            {
                Entity boundary = boundaries[boundaryIndex];
                if (!em.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary))
                    continue;

                hasBuffer = true;
                DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                    em.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary, true);
                entryCount += summaries.Length;
                for (int i = 0; i < summaries.Length; i++)
                {
                    BuildingRuntimeFactionUsableFuelSummary summary = summaries[i];
                    if (!FactionIdentity.IsPlayerControlled(summary.FactionId))
                        continue;

                    hasPlayerSummary = true;
                    storedFuel += Math.Max(0f, summary.StoredFuelBarrels);
                    fuelCapacity += Math.Max(0, summary.FuelStorageCapacity);
                    version = CombineValidationVersion(version, summary.Version);
                }
            }
        }

        private static bool TryValidateSyntheticFuelMove(EntityManager em, out string status)
        {
            Entity testUnit = em.CreateEntity(
                typeof(UnitGrid),
                typeof(Faction),
                typeof(UnitMovementBehavior),
                typeof(UnitFuelConsumption),
                typeof(UnitFuelConsumptionState));
            try
            {
                em.SetComponentData(testUnit, new UnitGrid { Cell = new int2(4, 4) });
                em.SetComponentData(testUnit, new Faction { Id = FactionIdentity.PlayerFactionId });
                em.SetComponentData(testUnit, new UnitMovementBehavior { UsesVehicleMotion = 1 });
                em.SetComponentData(testUnit, new UnitFuelConsumption
                {
                    Enabled = 1,
                    GroundFuelPerCell = 1f,
                    AirFuelPerCell = 1f
                });
                em.SetComponentData(testUnit, new UnitFuelConsumptionState());
                int requestId = UnitMoveOrderRequestSystem.EnqueueGroupedManualMoveOrder(
                    em,
                    testUnit,
                    new int2(5, 4),
                    issueGroundPathNow: false,
                    useGroundPathRetryCooldown: false,
                    resumeFrame: 0,
                    currentFrame: Time.frameCount);
                UnitMoveOrderRequestSystem.ProcessPendingRequests(em);
                bool found = UnitMoveOrderRequestSystem.TryGetResult(em, requestId, out UnitMoveOrderResultElement result);
                status =
                    $"syntheticMoveResult={(found ? 1 : 0)} syntheticMoveIssued={(found && result.Issued != 0 ? 1 : 0)} " +
                    $"syntheticMoveReject={result.RejectionReasonCode}";
                return found && result.Issued != 0;
            }
            finally
            {
                if (em.Exists(testUnit))
                    em.DestroyEntity(testUnit);
            }
        }

        private static int ParsePositiveIntText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int result = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < '0' || c > '9')
                    continue;

                result = checked((result * 10) + (c - '0'));
            }

            return result;
        }

        private static uint CombineValidationVersion(uint current, uint value)
        {
            unchecked
            {
                return (current * 16777619u) ^ value;
            }
        }

        private static bool TryEnqueueMatchRoute(out string error)
        {
            error = string.Empty;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                error = "Default ECS world is missing.";
                return false;
            }

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                error = "UI shell root is missing.";
                return false;
            }

            Entity boundary = query.GetSingletonEntity();
            DynamicBuffer<UiShellRouteRequestComponent> routeRequests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(boundary);
            routeRequests.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            return true;
        }

        private static void EnsurePlayModeRequested()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static bool IsMatchRuntimeReady(out string status)
        {
            return IsMatchRuntimeReady(out status, requireCurtainHidden: true);
        }

        private static bool IsMatchRuntimeReady(out string status, bool requireCurtainHidden)
        {
            status = "waiting";
            if (!TryGetShellState(out UiShellStateComponent shellState))
                return false;

            if (!TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState))
                return false;

            if (!TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro))
                return false;

            bool matchSceneLoaded = IsSceneLoaded(MatchSceneName);
            bool hudLoaded = LoadedScenesContainMatchHudContent();
            bool curtainHidden = IsMatchIntroCurtainHidden();
            status =
                $"mode={shellState.CurrentMode} route={shellState.ActiveRoute} phase={shellState.Phase} " +
                $"transition={shellState.IsTransitionRunning} playRequested={runtimeState.PlayRequested} " +
                $"matchIntro={matchIntro.State} inputLocked={matchIntro.InputLocked} " +
                $"matchSceneLoaded={(matchSceneLoaded ? 1 : 0)} hudLoaded={(hudLoaded ? 1 : 0)} " +
                $"curtainHidden={(curtainHidden ? 1 : 0)}";

            return shellState.CurrentMode == UiShellMode.MatchHud &&
                   shellState.ActiveRoute == UIRoute.Match &&
                   shellState.IsTransitionRunning == 0 &&
                   runtimeState.PlayRequested != 0 &&
                   matchIntro.State == MatchIntroTransitionStateKind.Complete &&
                   matchIntro.InputLocked == 0 &&
                   matchSceneLoaded &&
                   hudLoaded &&
                   (!requireCurtainHidden || curtainHidden);
        }

        private static bool TryGetShellState(out UiShellStateComponent shellState)
        {
            shellState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<UiShellStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            shellState = entityManager.GetComponentData<UiShellStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool TryGetRuntimeGameplayState(out RuntimeGameplayStateComponent runtimeState)
        {
            runtimeState = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            runtimeState = entityManager.GetComponentData<RuntimeGameplayStateComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool TryGetMatchIntroState(out MatchIntroTransitionComponent matchIntro)
        {
            matchIntro = default;
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadOnly<MatchIntroTransitionComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            matchIntro = entityManager.GetComponentData<MatchIntroTransitionComponent>(query.GetSingletonEntity());
            return true;
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool LoadedScenesContainMatchHudContent()
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    if (TreeContains(roots[rootIndex].transform, MatchHudContentName) ||
                        roots[rootIndex].GetComponentInChildren<MatchOverlayCommandControlsView>(true) != null ||
                        roots[rootIndex].GetComponentInChildren<BattleHudRuntimeFeedbackView>(true) != null ||
                        roots[rootIndex].GetComponentInChildren<MatchHudMinimapView>(true) != null ||
                        roots[rootIndex].GetComponentInChildren<MatchHudSquadTrayView>(true) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMatchIntroCurtainHidden()
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    MatchIntroCurtainView curtain = roots[rootIndex].GetComponentInChildren<MatchIntroCurtainView>(true);
                    if (curtain == null)
                        continue;

                    bool rootHidden = curtain.Root == null || !curtain.Root.activeSelf;
                    bool transparent = curtain.CanvasGroup == null || curtain.CanvasGroup.alpha <= 0.001f;
                    return rootHidden && transparent;
                }
            }

            return false;
        }

        private static bool TreeContains(Transform node, string objectName)
        {
            if (node.name == objectName)
                return true;

            for (int i = 0; i < node.childCount; i++)
            {
                if (TreeContains(node.GetChild(i), objectName))
                    return true;
            }

            return false;
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying)
                return;

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                if (type == LogType.Log &&
                    SessionState.GetBool(RequireFrameDiagKey, false) &&
                    condition != null &&
                    condition.StartsWith("[FrameRateDiag] ", StringComparison.Ordinal))
                {
                    if (IsGameplayStableForFrameDiag(out string stableStatus))
                        SessionState.SetString(FrameDiagKey, condition);
                    else
                        Debug.Log($"[MatchRuntimeShellSmokeValidation] ignoredFrameRateDiagBeforeStable {stableStatus}");
                }

                return;
            }

            if (condition != null &&
                (condition.Contains("[MatchRuntimeShellSmokeValidation] result=Failed", StringComparison.Ordinal) ||
                 condition.Contains("[Licensing::", StringComparison.Ordinal)))
            {
                return;
            }

            if (condition != null &&
                stackTrace != null &&
                condition.StartsWith("ArgumentOutOfRangeException", StringComparison.Ordinal) &&
                (stackTrace.Contains("UnityEditor.Search.SearchDatabase", StringComparison.Ordinal) ||
                 stackTrace.Contains("UnityEditor.Search.SearchInit", StringComparison.Ordinal)))
            {
                return;
            }

            SessionState.SetInt(ErrorCountKey, SessionState.GetInt(ErrorCountKey, 0) + 1);
        }

        private static void ConfigurePlayModeReloadForBatchValidation()
        {
            if (!Application.isBatchMode)
            {
                SessionState.EraseBool(OverrideEnterPlayModeSettingsKey);
                return;
            }

            SessionState.SetBool(OverrideEnterPlayModeSettingsKey, true);
            SessionState.SetBool(PreviousEnterPlayModeOptionsEnabledKey, EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(PreviousEnterPlayModeOptionsKey, (int)EditorSettings.enterPlayModeOptions);

            EnterPlayModeOptions batchOptions =
                EditorSettings.enterPlayModeOptions &
                ~(EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload);
            bool batchOptionsEnabled = batchOptions != EnterPlayModeOptions.None;
            if (EditorSettings.enterPlayModeOptionsEnabled == batchOptionsEnabled &&
                EditorSettings.enterPlayModeOptions == batchOptions)
            {
                return;
            }

            Debug.Log(
                "[MatchRuntimeShellSmokeValidation] forcingSceneReloadForBatch " +
                $"previousEnabled={EditorSettings.enterPlayModeOptionsEnabled} " +
                $"previousOptions={EditorSettings.enterPlayModeOptions} " +
                $"batchEnabled={batchOptionsEnabled} batchOptions={batchOptions}");
            EditorSettings.enterPlayModeOptionsEnabled = batchOptionsEnabled;
            EditorSettings.enterPlayModeOptions = batchOptions;
        }

        private static void RestorePlayModeReloadSettings()
        {
            if (!SessionState.GetBool(OverrideEnterPlayModeSettingsKey, false))
                return;

            bool previousEnabled = SessionState.GetBool(PreviousEnterPlayModeOptionsEnabledKey, false);
            EnterPlayModeOptions previousOptions =
                (EnterPlayModeOptions)SessionState.GetInt(PreviousEnterPlayModeOptionsKey, (int)EnterPlayModeOptions.None);
            EditorSettings.enterPlayModeOptionsEnabled = previousEnabled;
            EditorSettings.enterPlayModeOptions = previousOptions;
            SessionState.EraseBool(OverrideEnterPlayModeSettingsKey);
            SessionState.EraseBool(PreviousEnterPlayModeOptionsEnabledKey);
            SessionState.EraseInt(PreviousEnterPlayModeOptionsKey);
        }

        private static void LogProgressIfDue(Phase phase, string status)
        {
            double now = EditorApplication.timeSinceStartup;
            double lastLogAt = SessionState.GetFloat(LastProgressLogAtKey, 0f);
            if (now - lastLogAt < ProgressLogIntervalSeconds)
                return;

            SessionState.SetFloat(LastProgressLogAtKey, (float)now);
            Debug.Log(
                "[MatchRuntimeShellSmokeValidation] progress " +
                $"phase={phase} isPlaying={EditorApplication.isPlaying} " +
                $"willChangePlayMode={EditorApplication.isPlayingOrWillChangePlaymode} " +
                $"frame={Time.frameCount} status={status}");
        }

        private static bool IsGameplayStableForFrameDiag(out string status)
        {
            status = "world=missing";
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery runtimeQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
            if (runtimeQuery.IsEmptyIgnoreFilter)
            {
                using EntityQuery matchStartQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<MatchStartProgressComponent>());
                status = matchStartQuery.CalculateEntityCount() == 1
                    ? $"runtimeState=missing matchStart={matchStartQuery.GetSingleton<MatchStartProgressComponent>().Status}"
                    : "runtimeState=missing matchStart=missing";
                return false;
            }

            RuntimeGameplayStateComponent runtimeState =
                entityManager.GetComponentData<RuntimeGameplayStateComponent>(runtimeQuery.GetSingletonEntity());
            if (runtimeState.PlayRequested == 0)
            {
                status = "playRequested=0";
                return false;
            }

            using EntityQuery allSpawnConfigs = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
            using EntityQuery initializedSpawnConfigs = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnInitialized>());
            using EntityQuery progressingSpawnConfigs = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<InitialUnitsSpawnConfig>(),
                ComponentType.ReadOnly<InitialUnitsSpawnProgress>());
            using EntityQuery sourceKeys = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitSourcePrefabKey>());

            int totalConfigCount = allSpawnConfigs.CalculateEntityCount();
            int initializedConfigCount = initializedSpawnConfigs.CalculateEntityCount();
            int progressingConfigCount = progressingSpawnConfigs.CalculateEntityCount();
            int sourceKeyCount = sourceKeys.CalculateEntityCount();
            status =
                $"playRequested=1 spawnConfigs={initializedConfigCount}/{totalConfigCount} " +
                $"progressing={progressingConfigCount} sourceKeys={sourceKeyCount}";

            return (totalConfigCount == 0 || initializedConfigCount >= totalConfigCount) &&
                   progressingConfigCount == 0 &&
                   sourceKeyCount > 0;
        }

        private static bool UpdateAirMissileSmoke(out bool complete, out bool failed, out string status)
        {
            complete = false;
            failed = false;
            status = "airSmoke=waiting";

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                failed = true;
                status = "airSmoke=failed world=missing";
                return true;
            }

            EntityManager em = world.EntityManager;
            if (_airSmokeLauncher == Entity.Null)
            {
                CreateAirMissileSmokeScenario(em);
                status = "airSmoke=created";
                return false;
            }

            if (!em.Exists(_airSmokeLauncher) || !em.Exists(_airSmokeTarget))
            {
                failed = true;
                status = "airSmoke=failed scenarioEntityMissing";
                return true;
            }

            TrackAirMissileProjectileState(em);
            UnitHealth targetHealth = em.GetComponentData<UnitHealth>(_airSmokeTarget);
            byte phase = em.HasComponent<AirMissileLauncherStateComponent>(_airSmokeLauncher)
                ? em.GetComponentData<AirMissileLauncherStateComponent>(_airSmokeLauncher).Phase
                : (byte)AirMissileLauncherPhase.Idle;
            bool hasTarget = em.HasComponent<AirMissileLauncherTargetComponent>(_airSmokeLauncher);
            bool damaged = targetHealth.Current < targetHealth.Max;
            status =
                $"airSmoke=running phase={(AirMissileLauncherPhase)phase} hasTarget={(hasTarget ? 1 : 0)} " +
                $"projectileSeen={(_airSmokeProjectileSeen ? 1 : 0)} trailSeen={(_airSmokeTrailSeen ? 1 : 0)} " +
                $"health={targetHealth.Current}/{targetHealth.Max}";

            if (_airSmokeProjectileSeen && _airSmokeTrailSeen && damaged)
            {
                CleanupAirMissileSmoke();
                complete = true;
                status =
                    $"[AirMissileLauncherMatchSmoke] result=Passed projectileSeen=1 trailSeen=1 " +
                    $"targetHealth={targetHealth.Current}/{targetHealth.Max}";
                return false;
            }

            if (EditorApplication.timeSinceStartup - _airSmokeStartedAt > AirMissileSmokeTimeoutSeconds)
            {
                CleanupAirMissileSmoke();
                failed = true;
                status = $"[AirMissileLauncherMatchSmoke] result=Failed timeout {status}";
                return true;
            }

            return false;
        }

        private static bool UpdateBaselineMetrics(out bool complete, out string status)
        {
            complete = false;
            status = "baselineMetrics=waiting";

            if (!IsMatchRuntimeReady(out string readyStatus))
                return false;

            if (!IsGameplayStableForFrameDiag(out string stableStatus))
            {
                ResetBaselineMetricsState();
                status = $"baselineMetrics=waiting stable={stableStatus}";
                return false;
            }

            if (SessionState.GetBool(RequirePerformanceRegressionReportKey, false) &&
                !_performanceFixtureReady &&
                !PreparePerformanceFixture(out string fixtureStatus))
            {
                ResetBaselineMetricsState();
                status = fixtureStatus;
                return false;
            }

            if (_baselineMetricsStartedAt <= 0d)
            {
                _baselineMetricsStartedAt = EditorApplication.timeSinceStartup;
                _baselineMetricsAllocatedBytesAtStart = GC.GetAllocatedBytesForCurrentThread();
            }

            if (Time.frameCount != _baselineMetricsLastFrame && Time.unscaledDeltaTime > 0f)
            {
                BaselineFrameTimesMs.Add(Time.unscaledDeltaTime * 1000d);
                _baselineMetricsLastFrame = Time.frameCount;
            }

            double elapsedSeconds = EditorApplication.timeSinceStartup - _baselineMetricsStartedAt;
            if (BaselineFrameTimesMs.Count < BaselineMetricsFrameTarget ||
                elapsedSeconds < BaselineMetricsObservationSeconds)
            {
                status =
                    $"baselineMetrics=collecting frames={BaselineFrameTimesMs.Count}/{BaselineMetricsFrameTarget} " +
                    $"elapsed={elapsedSeconds:0.0}s {readyStatus} stable={stableStatus}";
                return false;
            }

            long allocatedBytes = Math.Max(
                0,
                GC.GetAllocatedBytesForCurrentThread() - _baselineMetricsAllocatedBytesAtStart);
            string reportStableStatus = string.IsNullOrEmpty(_performanceFixtureStatus)
                ? stableStatus
                : $"{stableStatus} {_performanceFixtureStatus}";
            if (!TryWriteBaselineMetricsReport(readyStatus, reportStableStatus, elapsedSeconds, allocatedBytes, out string reportStatus))
            {
                status = reportStatus;
                return true;
            }

            complete = true;
            status = reportStatus;
            return true;
        }

        private static bool TryWriteBaselineMetricsReport(
            string readyStatus,
            string stableStatus,
            double elapsedSeconds,
            long allocatedBytes,
            out string status)
        {
            status = string.Empty;
            try
            {
                double averageMs = Average(BaselineFrameTimesMs);
                double p95Ms = Percentile(BaselineFrameTimesMs, 0.95d);
                double p99Ms = Percentile(BaselineFrameTimesMs, 0.99d);
                double maxMs = Max(BaselineFrameTimesMs);
                BaselineEntityCounts counts = CaptureBaselineEntityCounts();
                bool requirePerformanceRegressionReport = SessionState.GetBool(RequirePerformanceRegressionReportKey, false);
                PerformanceRegressionAcceptedBaseline acceptedBaseline = default;
                double editorP95BudgetMs = PerformanceRegressionEditorP95FrameBudgetMs;
                string performanceBaselineStatus = string.Empty;

                if (requirePerformanceRegressionReport)
                {
                    if (!TryLoadPerformanceRegressionAcceptedBaseline(out acceptedBaseline, out string baselineStatus))
                    {
                        status =
                            $"[PerformanceRegressionBaseline] result=Failed acceptedBaseline={PerformanceRegressionAcceptedBaselinePath} " +
                            baselineStatus;
                        return false;
                    }

                    editorP95BudgetMs = acceptedBaseline.EditorP95FrameBudgetMs;
                }

                string metricsJson = BuildBaselineMetricsJson(
                    readyStatus,
                    stableStatus,
                    elapsedSeconds,
                    allocatedBytes,
                    averageMs,
                    p95Ms,
                    p99Ms,
                    maxMs,
                    editorP95BudgetMs,
                    counts);
                File.WriteAllText(BaselineMetricsReportPath, metricsJson);

                if (requirePerformanceRegressionReport)
                {
                    WritePerformanceRegressionMetricsArtifact(metricsJson);
                    WritePerformanceRegressionReport(
                        readyStatus,
                        stableStatus,
                        elapsedSeconds,
                        allocatedBytes,
                        averageMs,
                        p95Ms,
                        p99Ms,
                        maxMs,
                        acceptedBaseline,
                        counts);

                    if (!TryValidatePerformanceRegressionAcceptedBaseline(
                            acceptedBaseline,
                            allocatedBytes,
                            p95Ms,
                            counts,
                            out string acceptedStatus))
                    {
                        status =
                            $"[PerformanceRegressionBaseline] result=Failed report={PerformanceRegressionReportPath} " +
                            acceptedStatus;
                        return false;
                    }

                    performanceBaselineStatus = acceptedStatus;
                }

                status =
                    $"[MatchRuntimeBaselineMetrics] result=Passed report={BaselineMetricsReportPath} " +
                    $"frames={BaselineFrameTimesMs.Count} avg={averageMs:F2}ms p95={p95Ms:F2}ms " +
                    $"p99={p99Ms:F2}ms max={maxMs:F2}ms alloc={allocatedBytes} " +
                    $"units={counts.UnitCount} buildings={counts.RuntimeBuildingCount} projectiles={counts.ProjectileCount} " +
                    $"markers={counts.MarkerCount} visibleModels={counts.VisibleModelEstimate} {performanceBaselineStatus}".TrimEnd();
                return true;
            }
            catch (Exception exception)
            {
                status = $"[MatchRuntimeBaselineMetrics] result=Failed {exception.Message}";
                return false;
            }
        }

        private static string BuildBaselineMetricsJson(
            string readyStatus,
            string stableStatus,
            double elapsedSeconds,
            long allocatedBytes,
            double averageMs,
            double p95Ms,
            double p99Ms,
            double maxMs,
            double editorP95BudgetMs,
            BaselineEntityCounts counts)
        {
            StringBuilder builder = new();
            builder.AppendLine("{");
            AppendJson(builder, "source", "Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline", trailingComma: true);
            AppendEvidenceIdentityJson(builder);
            AppendJson(builder, "observationSeconds", elapsedSeconds, trailingComma: true);
            AppendJson(builder, "frameCount", BaselineFrameTimesMs.Count, trailingComma: true);
            AppendJson(builder, "averageFrameMs", averageMs, trailingComma: true);
            AppendJson(builder, "p95FrameMs", p95Ms, trailingComma: true);
            AppendJson(builder, "editorP95FrameBudgetMs", editorP95BudgetMs, trailingComma: true);
            AppendJson(builder, "editorP95FrameBudgetPassed", p95Ms <= editorP95BudgetMs, trailingComma: true);
            AppendJson(builder, "p99FrameMs", p99Ms, trailingComma: true);
            AppendJson(builder, "maxFrameMs", maxMs, trailingComma: true);
            AppendJson(builder, "allocatedBytesCurrentThread", allocatedBytes, trailingComma: true);
            AppendJson(builder, "unitCount", counts.UnitCount, trailingComma: true);
            AppendJson(builder, "runtimeBuildingCount", counts.RuntimeBuildingCount, trailingComma: true);
            AppendJson(builder, "groundMissileProjectileCount", counts.GroundMissileProjectileCount, trailingComma: true);
            AppendJson(builder, "airMissileProjectileCount", counts.AirMissileProjectileCount, trailingComma: true);
            AppendJson(builder, "projectileCount", counts.ProjectileCount, trailingComma: true);
            AppendJson(builder, "selectionMarkerEntityCount", counts.SelectionMarkerEntityCount, trailingComma: true);
            AppendJson(builder, "minimapMarkerCount", counts.MinimapMarkerCount, trailingComma: true);
            AppendJson(builder, "markerCount", counts.MarkerCount, trailingComma: true);
            AppendJson(builder, "unitModelInstanceCount", counts.UnitModelInstanceCount, trailingComma: true);
            AppendJson(builder, "culledUnitCount", counts.CulledUnitCount, trailingComma: true);
            AppendJson(builder, "visibleRenderStateCount", counts.VisibleRenderStateCount, trailingComma: true);
            AppendJson(builder, "visibleModelEstimate", counts.VisibleModelEstimate, trailingComma: true);
            AppendJson(builder, "renderVisualStateCount", counts.RenderVisualStateCount, trailingComma: true);
            AppendJson(builder, "readyStatus", readyStatus, trailingComma: true);
            AppendJson(builder, "stableStatus", stableStatus, trailingComma: false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void WritePerformanceRegressionMetricsArtifact(string metricsJson)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PerformanceRegressionMetricsArtifactPath) ?? ".");
            File.WriteAllText(PerformanceRegressionMetricsArtifactPath, metricsJson);
            Debug.Log($"[PerformanceRegressionBaseline] wroteMetricsArtifact {PerformanceRegressionMetricsArtifactPath}");
        }

        private static void WritePerformanceRegressionReport(
            string readyStatus,
            string stableStatus,
            double elapsedSeconds,
            long allocatedBytes,
            double averageMs,
            double p95Ms,
            double p99Ms,
            double maxMs,
            PerformanceRegressionAcceptedBaseline acceptedBaseline,
            BaselineEntityCounts counts)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PerformanceRegressionReportPath) ?? ".");

            StringBuilder builder = new();
            string evidenceCommit = SessionState.GetString(EvidenceCommitKey, "not-bound");
            string evidenceEnvironment = SessionState.GetString(EvidenceEnvironmentKey, "not-bound");
            string evidenceDirty = SessionState.GetBool(EvidenceDirtyKey, true).ToString().ToLowerInvariant();
            builder.AppendLine("# Performance Regression Match Baseline");
            builder.AppendLine();
            builder.AppendLine("Source: `Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline`.");
            builder.AppendLine();
            builder.AppendLine($"- Exact commit: `{evidenceCommit}`");
            builder.AppendLine($"- Environment identity SHA-256: `{evidenceEnvironment}`");
            builder.AppendLine($"- Dirty at capture start: `{evidenceDirty}`");
            builder.AppendLine($"- Quality: `{ResolveQualityName()}` (index `{QualitySettings.GetQualityLevel()}`)");
            builder.AppendLine($"- Resolution: `{Screen.width}x{Screen.height}`");
            builder.AppendLine($"- Instrumentation: `{ResolveFrameInstrumentationState()}`");
            builder.AppendLine($"- Target frame rate: `{Application.targetFrameRate}`; vSync count: `{QualitySettings.vSyncCount}`");
            builder.AppendLine();
            builder.AppendLine("| Metric | Value |");
            builder.AppendLine("|---|---:|");
            builder.AppendLine($"| Observation seconds | {elapsedSeconds.ToString("F2", CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Frame count | {BaselineFrameTimesMs.Count.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Average frame ms | {averageMs.ToString("F2", CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| P95 frame ms | {p95Ms.ToString("F2", CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Editor P95 frame budget ms | {acceptedBaseline.EditorP95FrameBudgetMs.ToString("F2", CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Editor P95 frame budget passed | {(p95Ms <= acceptedBaseline.EditorP95FrameBudgetMs ? "yes" : "no")} |");
            builder.AppendLine($"| P99 frame ms | {p99Ms.ToString("F2", CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Max frame ms | {maxMs.ToString("F2", CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Current-thread allocated bytes | {allocatedBytes.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Current-thread allocation budget bytes | {acceptedBaseline.CurrentThreadAllocatedBytesBudget.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Units | {counts.UnitCount.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Minimum units | {acceptedBaseline.MinimumUnitCount.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Runtime buildings | {counts.RuntimeBuildingCount.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Minimum runtime buildings | {acceptedBaseline.MinimumRuntimeBuildingCount.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Projectiles | {counts.ProjectileCount.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Markers | {counts.MarkerCount.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Visible model estimate | {counts.VisibleModelEstimate.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine($"| Minimum visible model estimate | {acceptedBaseline.MinimumVisibleModelEstimate.ToString(CultureInfo.InvariantCulture)} |");
            builder.AppendLine();
            builder.AppendLine("## Runtime Status");
            builder.AppendLine();
            builder.AppendLine($"- Accepted baseline: `{PerformanceRegressionAcceptedBaselinePath}`");
            builder.AppendLine($"- Metrics artifact: `{PerformanceRegressionMetricsArtifactPath}`");
            builder.AppendLine($"- Ready: `{readyStatus}`");
            builder.AppendLine($"- Stable: `{stableStatus}`");
            builder.AppendLine();
            builder.AppendLine("The editor P95 budget is intentionally lenient and catches large regressions only; Android device development/release lanes remain the mobile rendering-performance gates.");

            File.WriteAllText(PerformanceRegressionReportPath, builder.ToString());
            Debug.Log($"[PerformanceRegressionBaseline] wroteReport {PerformanceRegressionReportPath}");
        }

        private static void AppendEvidenceIdentityJson(StringBuilder builder)
        {
            string exactCommit = SessionState.GetString(EvidenceCommitKey, string.Empty);
            string environmentIdentity = SessionState.GetString(EvidenceEnvironmentKey, string.Empty);
            if (string.IsNullOrWhiteSpace(exactCommit) || string.IsNullOrWhiteSpace(environmentIdentity))
                return;

            AppendJson(builder, "exactCommit", exactCommit, trailingComma: true);
            AppendJson(builder, "environmentIdentitySha256", environmentIdentity, trailingComma: true);
            AppendJson(builder, "dirty", SessionState.GetBool(EvidenceDirtyKey, true), trailingComma: true);
            AppendJson(builder, "qualityLevel", QualitySettings.GetQualityLevel(), trailingComma: true);
            AppendJson(builder, "qualityName", ResolveQualityName(), trailingComma: true);
            AppendJson(builder, "resolutionWidth", Screen.width, trailingComma: true);
            AppendJson(builder, "resolutionHeight", Screen.height, trailingComma: true);
            AppendJson(builder, "instrumentationState", ResolveFrameInstrumentationState(), trailingComma: true);
            AppendJson(builder, "targetFrameRate", Application.targetFrameRate, trailingComma: true);
            AppendJson(builder, "vSyncCount", QualitySettings.vSyncCount, trailingComma: true);
        }

        private static string ResolveQualityName()
        {
            int qualityLevel = QualitySettings.GetQualityLevel();
            string[] names = QualitySettings.names;
            return qualityLevel >= 0 && qualityLevel < names.Length
                ? names[qualityLevel]
                : $"index-{qualityLevel}";
        }

        private static string ResolveFrameInstrumentationState()
        {
            return
                $"frameSampler=stopwatch profilerEnabled={UnityEngine.Profiling.Profiler.enabled.ToString().ToLowerInvariant()} " +
                "deepProfiling=false instrumentationOffControl=not-required-stopwatch-only";
        }

        private static bool TryLoadPerformanceRegressionAcceptedBaseline(
            out PerformanceRegressionAcceptedBaseline acceptedBaseline,
            out string status)
        {
            acceptedBaseline = default;
            status = "acceptedBaseline=missing";

            if (!File.Exists(PerformanceRegressionAcceptedBaselinePath))
                return false;

            try
            {
                acceptedBaseline = JsonUtility.FromJson<PerformanceRegressionAcceptedBaseline>(
                    File.ReadAllText(PerformanceRegressionAcceptedBaselinePath));
            }
            catch (Exception exception)
            {
                status = $"acceptedBaseline=parseFailed message=\"{exception.Message}\"";
                return false;
            }

            if (acceptedBaseline.EditorP95FrameBudgetMs <= 0d)
            {
                status = "acceptedBaseline=invalid editorP95FrameBudgetMs<=0";
                return false;
            }

            if (acceptedBaseline.MinimumFrameCount <= 0)
            {
                status = "acceptedBaseline=invalid minimumFrameCount<=0";
                return false;
            }

            status = "acceptedBaseline=loaded";
            return true;
        }

        private static bool TryValidatePerformanceRegressionAcceptedBaseline(
            PerformanceRegressionAcceptedBaseline acceptedBaseline,
            long allocatedBytes,
            double p95Ms,
            BaselineEntityCounts counts,
            out string status)
        {
            if (p95Ms > acceptedBaseline.EditorP95FrameBudgetMs)
            {
                status = $"p95={p95Ms:F2}ms budget={acceptedBaseline.EditorP95FrameBudgetMs:F2}ms";
                return false;
            }

            if (BaselineFrameTimesMs.Count < acceptedBaseline.MinimumFrameCount)
            {
                status = $"frames={BaselineFrameTimesMs.Count} minimum={acceptedBaseline.MinimumFrameCount}";
                return false;
            }

            if (allocatedBytes > acceptedBaseline.CurrentThreadAllocatedBytesBudget)
            {
                status = $"allocatedBytes={allocatedBytes} budget={acceptedBaseline.CurrentThreadAllocatedBytesBudget}";
                return false;
            }

            if (counts.UnitCount < acceptedBaseline.MinimumUnitCount)
            {
                status = $"units={counts.UnitCount} minimum={acceptedBaseline.MinimumUnitCount}";
                return false;
            }

            if (counts.RuntimeBuildingCount < acceptedBaseline.MinimumRuntimeBuildingCount)
            {
                status = $"buildings={counts.RuntimeBuildingCount} minimum={acceptedBaseline.MinimumRuntimeBuildingCount}";
                return false;
            }

            if (counts.VisibleModelEstimate < acceptedBaseline.MinimumVisibleModelEstimate)
            {
                status = $"visibleModels={counts.VisibleModelEstimate} minimum={acceptedBaseline.MinimumVisibleModelEstimate}";
                return false;
            }

            status =
                $"acceptedBaseline=passed p95={p95Ms:F2}ms/{acceptedBaseline.EditorP95FrameBudgetMs:F2}ms " +
                $"alloc={allocatedBytes}/{acceptedBaseline.CurrentThreadAllocatedBytesBudget} " +
                $"frames={BaselineFrameTimesMs.Count}/{acceptedBaseline.MinimumFrameCount}";
            return true;
        }

        private static BaselineEntityCounts CaptureBaselineEntityCounts()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return default;

            EntityManager em = world.EntityManager;
            int groundProjectileCount = CountEntities(em, ComponentType.ReadOnly<GroundMissileProjectileComponent>());
            int airProjectileCount = CountEntities(em, ComponentType.ReadOnly<AirMissileProjectileComponent>());
            int unitModelInstanceCount = CountEntities(em, ComponentType.ReadOnly<UnitModelInstanceReference>());
            int culledUnitCount = CountEntities(em, ComponentType.ReadOnly<UnitRenderBudgetCulledUnitTag>());
            int renderVisualStateCount = CountEntities(em, ComponentType.ReadOnly<UnitRenderVisualComponent>());
            int visibleRenderStateCount = CountEntities(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<UnitRenderVisualComponent>() },
                None = new[] { ComponentType.ReadOnly<UnitRenderBudgetCulledUnitTag>() }
            });
            int visibleModelEstimate = unitModelInstanceCount > 0
                ? Math.Max(0, unitModelInstanceCount - culledUnitCount)
                : visibleRenderStateCount;
            int selectionMarkerEntityCount = CountEntities(em, ComponentType.ReadOnly<SelectionMarkerTag>());
            int minimapMarkerCount = CountMinimapMarkers(em);

            return new BaselineEntityCounts
            {
                UnitCount = CountEntities(em, ComponentType.ReadOnly<UnitSourcePrefabKey>()),
                RuntimeBuildingCount = CountEntities(em, ComponentType.ReadOnly<RuntimeBuildingCombatTag>()),
                GroundMissileProjectileCount = groundProjectileCount,
                AirMissileProjectileCount = airProjectileCount,
                ProjectileCount = groundProjectileCount + airProjectileCount,
                SelectionMarkerEntityCount = selectionMarkerEntityCount,
                MinimapMarkerCount = minimapMarkerCount,
                MarkerCount = selectionMarkerEntityCount + minimapMarkerCount,
                UnitModelInstanceCount = unitModelInstanceCount,
                CulledUnitCount = culledUnitCount,
                VisibleModelEstimate = visibleModelEstimate,
                VisibleRenderStateCount = visibleRenderStateCount,
                RenderVisualStateCount = renderVisualStateCount
            };
        }

        private static int CountEntities(EntityQueryDesc queryDescription)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return 0;

            using EntityQuery query = world.EntityManager.CreateEntityQuery(queryDescription);
            return query.CalculateEntityCount();
        }

        private static int CountEntities(EntityManager em, params ComponentType[] componentTypes)
        {
            using EntityQuery query = em.CreateEntityQuery(componentTypes);
            return query.CalculateEntityCount();
        }

        private static int CountMinimapMarkers(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<MatchHudMinimapMarkerStateComponent>(),
                ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
            if (query.IsEmptyIgnoreFilter)
                return 0;

            int count = 0;
            using NativeArray<Entity> boundaries = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < boundaries.Length; i++)
                count += em.GetBuffer<MatchHudMinimapMarkerElement>(boundaries[i], true).Length;

            return count;
        }

        private static bool ValidateInitialBuildingSmoke(bool requireNoFaction2OilPump, out string status)
        {
            status = "initialBuildingSmoke=failed world=missing";
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager em = world.EntityManager;
            int faction2Tent = CountRuntimeBuildingsByFactionAndKey(em, 2, "Tent_Regular");
            int faction2OilPump = CountRuntimeBuildingsByFactionAndKey(em, 2, "Building_OilPump");
            int allOilPump = CountRuntimeBuildingsByKey(em, "Building_OilPump");
            bool hasExpectedInitialTentOrigin = TryResolveExpectedInitialBuildingOrigin(
                2,
                "Tent_Regular",
                out int2 expectedInitialTentOrigin,
                out string expectedOriginStatus);
            int faction2InitialTent = hasExpectedInitialTentOrigin
                ? CountRuntimeBuildingsByFactionKeyAndOrigin(em, 2, "Tent_Regular", expectedInitialTentOrigin)
                : 0;
            int visibleFaction2InitialTent = hasExpectedInitialTentOrigin
                ? CountVisibleRuntimeBuildingInstancesByFactionKeyAndOrigin(em, 2, "Tent_Regular", expectedInitialTentOrigin)
                : 0;
            string buildings = DescribeRuntimeBuildingKeys(em);
            string visibleBuildings = DescribeRuntimeBuildingInstances(em);
            string requests = DescribeRuntimeSpawnRequests(em);

            if (!hasExpectedInitialTentOrigin ||
                faction2InitialTent <= 0 ||
                visibleFaction2InitialTent <= 0 ||
                faction2Tent <= 0 ||
                requireNoFaction2OilPump && faction2OilPump > 0)
            {
                status =
                    $"[InitialBuildingMenuDeploySmoke] result=Failed faction2Tent={faction2Tent} " +
                    $"faction2InitialTent={faction2InitialTent} expectedInitialTentOrigin={expectedOriginStatus} " +
                    $"visibleFaction2InitialTent={visibleFaction2InitialTent} requireNoFaction2OilPump={(requireNoFaction2OilPump ? 1 : 0)} " +
                    $"faction2OilPump={faction2OilPump} allOilPump={allOilPump} " +
                    $"buildings={buildings} visibleBuildings={visibleBuildings} requests={requests}";
                return false;
            }

            status =
                $"[InitialBuildingMenuDeploySmoke] result=Passed faction2Tent={faction2Tent} " +
                $"faction2InitialTent={faction2InitialTent} expectedInitialTentOrigin={expectedOriginStatus} " +
                $"visibleFaction2InitialTent={visibleFaction2InitialTent} requireNoFaction2OilPump={(requireNoFaction2OilPump ? 1 : 0)} " +
                $"faction2OilPump={faction2OilPump} " +
                $"allOilPump={allOilPump} buildings={buildings} visibleBuildings={visibleBuildings}";
            return true;
        }

        private static int CountRuntimeBuildingsByFactionAndKey(EntityManager em, byte factionId, string key)
        {
            string normalized = NormalizeRuntimeKey(key);
            int count = 0;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                if (info.OwnerFactionId != factionId)
                    continue;

                UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity);
                if (NormalizeRuntimeKey(sourceKey.Value.ToString()) == normalized)
                    count++;
            }

            return count;
        }

        private static int CountRuntimeBuildingsByKey(EntityManager em, string key)
        {
            string normalized = NormalizeRuntimeKey(key);
            int count = 0;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entities[i]);
                if (NormalizeRuntimeKey(sourceKey.Value.ToString()) == normalized)
                    count++;
            }

            return count;
        }

        private static int CountRuntimeBuildingsByFactionKeyAndOrigin(EntityManager em, byte factionId, string key, int2 originCell)
        {
            string normalized = NormalizeRuntimeKey(key);
            int count = 0;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                if (info.OwnerFactionId != factionId || !info.OriginCell.Equals(originCell))
                    continue;

                UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity);
                if (NormalizeRuntimeKey(sourceKey.Value.ToString()) == normalized)
                    count++;
            }

            return count;
        }

        private static int CountVisibleRuntimeBuildingInstancesByFactionKeyAndOrigin(EntityManager em, byte factionId, string key, int2 originCell)
        {
            string normalized = NormalizeRuntimeKey(key);
            int count = 0;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                if (info.OwnerFactionId != factionId || !info.OriginCell.Equals(originCell))
                    continue;

                UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity);
                if (NormalizeRuntimeKey(sourceKey.Value.ToString()) != normalized)
                    continue;

                if (HasVisibleRuntimeBuildingInstance(entity))
                    count++;
            }

            return count;
        }

        private static bool HasVisibleRuntimeBuildingInstance(Entity combatEntity)
        {
            RuntimeBuildingEntityLink[] links = UnityEngine.Object.FindObjectsByType<RuntimeBuildingEntityLink>(FindObjectsInactive.Include);
            for (int linkIndex = 0; linkIndex < links.Length; linkIndex++)
            {
                RuntimeBuildingEntityLink link = links[linkIndex];
                if (link == null || link.Entity != combatEntity || link.gameObject == null || !link.gameObject.activeInHierarchy)
                    continue;

                Renderer[] renderers = link.GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                        return true;
                }
            }

            return false;
        }

        private static bool TryResolveExpectedInitialBuildingOrigin(
            byte factionId,
            string prefabName,
            out int2 origin,
            out string status)
        {
            origin = default;
            status = "missing";
            InitialUnitsSpawnerAuthoringConfig config =
                AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(InitialUnitsConfigPath);
            if (config == null || config.Factions == null)
            {
                status = $"missingConfig path={InitialUnitsConfigPath}";
                return false;
            }

            for (int factionIndex = 0; factionIndex < config.Factions.Count; factionIndex++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[factionIndex];
                if (faction == null || faction.FactionId != factionId || faction.Buildings == null)
                    continue;

                for (int buildingIndex = 0; buildingIndex < faction.Buildings.Count; buildingIndex++)
                {
                    InitialUnitsSpawnerAuthoringConfig.FactionBuildingEntry building = faction.Buildings[buildingIndex];
                    if (building?.Prefab == null ||
                        !string.Equals(building.Prefab.name, prefabName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    origin = new int2(
                        faction.SpawnCell.x + building.OriginOffset.x,
                        faction.SpawnCell.y + building.OriginOffset.y);
                    status = $"{origin.x},{origin.y}";
                    return true;
                }
            }

            status = $"missingEntry faction={factionId} prefab={prefabName}";
            return false;
        }

        private static string DescribeRuntimeBuildingKeys(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),
                ComponentType.ReadOnly<UnitSourcePrefabKey>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
                return "<none>";

            StringBuilder builder = new();
            int max = Math.Min(entities.Length, 32);
            for (int i = 0; i < max; i++)
            {
                Entity entity = entities[i];
                RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
                UnitSourcePrefabKey sourceKey = em.GetComponentData<UnitSourcePrefabKey>(entity);
                if (builder.Length > 0)
                    builder.Append(';');
                builder
                    .Append("f")
                    .Append(info.OwnerFactionId)
                    .Append(':')
                    .Append(sourceKey.Value.ToString())
                    .Append('@')
                    .Append(info.OriginCell.x)
                    .Append(',')
                    .Append(info.OriginCell.y);
            }

            if (entities.Length > max)
                builder.Append(";...");
            return builder.ToString();
        }

        private static string DescribeRuntimeBuildingInstances(EntityManager em)
        {
            RuntimeBuildingEntityLink[] links = UnityEngine.Object.FindObjectsByType<RuntimeBuildingEntityLink>(FindObjectsInactive.Include);
            if (links.Length == 0)
                return "<none>";

            StringBuilder builder = new();
            int max = Math.Min(links.Length, 32);
            for (int i = 0; i < max; i++)
            {
                RuntimeBuildingEntityLink link = links[i];
                if (link == null)
                    continue;

                string source = "<none>";
                string origin = "<none>";
                byte faction = 0;
                if (em.Exists(link.Entity) &&
                    em.HasComponent<RuntimeBuildingCombatInfo>(link.Entity))
                {
                    RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(link.Entity);
                    faction = info.OwnerFactionId;
                    origin = $"{info.OriginCell.x},{info.OriginCell.y}";
                    if (em.HasComponent<UnitSourcePrefabKey>(link.Entity))
                        source = em.GetComponentData<UnitSourcePrefabKey>(link.Entity).Value.ToString();
                }

                Renderer[] renderers = link.GetComponentsInChildren<Renderer>(true);
                int enabledRenderers = 0;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                        enabledRenderers++;
                }

                if (builder.Length > 0)
                    builder.Append(';');
                builder
                    .Append("f")
                    .Append(faction)
                    .Append(':')
                    .Append(source)
                    .Append('@')
                    .Append(origin)
                    .Append(":go=")
                    .Append(link.gameObject != null ? link.gameObject.name : "<null>")
                    .Append(":active=")
                    .Append(link.gameObject != null && link.gameObject.activeInHierarchy ? 1 : 0)
                    .Append(":renderers=")
                    .Append(enabledRenderers);
            }

            if (links.Length > max)
                builder.Append(";...");
            return builder.Length == 0 ? "<empty>" : builder.ToString();
        }

        private static string DescribeRuntimeSpawnRequests(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingRuntimeStateTag>(),
                ComponentType.ReadOnly<BuildingRuntimeSpawnRequest>());
            if (query.IsEmptyIgnoreFilter)
                return "<no-boundary>";

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            StringBuilder builder = new();
            for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                DynamicBuffer<BuildingRuntimeSpawnRequest> requests =
                    em.GetBuffer<BuildingRuntimeSpawnRequest>(entities[entityIndex], true);
                int max = Math.Min(requests.Length, 16);
                for (int i = 0; i < max; i++)
                {
                    BuildingRuntimeSpawnRequest request = requests[i];
                    if (builder.Length > 0)
                        builder.Append(';');
                    builder
                        .Append("f")
                        .Append(request.FactionId)
                        .Append(':')
                        .Append(request.BuildingId.ToString())
                        .Append(":status=")
                        .Append(request.Status)
                        .Append(":result=")
                        .Append(request.ResultCode)
                        .Append(":origin=")
                        .Append(request.ActualOrigin.x)
                        .Append(',')
                        .Append(request.ActualOrigin.y);
                }

                if (requests.Length > max)
                    builder.Append(";...");
            }

            return builder.Length == 0 ? "<empty>" : builder.ToString();
        }

        private static string NormalizeRuntimeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("\0", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();
        }

        private static double Average(List<double> values)
        {
            if (values.Count == 0)
                return 0d;

            double total = 0d;
            for (int i = 0; i < values.Count; i++)
                total += values[i];
            return total / values.Count;
        }

        private static double Max(List<double> values)
        {
            if (values.Count == 0)
                return 0d;

            double max = values[0];
            for (int i = 1; i < values.Count; i++)
                max = Math.Max(max, values[i]);
            return max;
        }

        private static double Percentile(List<double> values, double percentile)
        {
            if (values.Count == 0)
                return 0d;

            double[] sorted = values.ToArray();
            Array.Sort(sorted);
            int index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
            index = Math.Max(0, Math.Min(sorted.Length - 1, index));
            return sorted[index];
        }

        private static void AppendJson(StringBuilder builder, string name, int value, bool trailingComma)
        {
            builder.Append("  \"").Append(name).Append("\": ").Append(value);
            builder.AppendLine(trailingComma ? "," : string.Empty);
        }

        private static void AppendJson(StringBuilder builder, string name, long value, bool trailingComma)
        {
            builder.Append("  \"").Append(name).Append("\": ").Append(value);
            builder.AppendLine(trailingComma ? "," : string.Empty);
        }

        private static void AppendJson(StringBuilder builder, string name, double value, bool trailingComma)
        {
            builder
                .Append("  \"")
                .Append(name)
                .Append("\": ")
                .Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            builder.AppendLine(trailingComma ? "," : string.Empty);
        }

        private static void AppendJson(StringBuilder builder, string name, bool value, bool trailingComma)
        {
            builder.Append("  \"").Append(name).Append("\": ").Append(value ? "true" : "false");
            builder.AppendLine(trailingComma ? "," : string.Empty);
        }

        private static void AppendJson(StringBuilder builder, string name, string value, bool trailingComma)
        {
            builder
                .Append("  \"")
                .Append(name)
                .Append("\": \"")
                .Append(EscapeJson(value))
                .Append('"');
            builder.AppendLine(trailingComma ? "," : string.Empty);
        }

        private static string EscapeJson(string value)
        {
            return value == null
                ? string.Empty
                : value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        private static void ResetBaselineMetricsState()
        {
            BaselineFrameTimesMs.Clear();
            _baselineMetricsStartedAt = 0d;
            _baselineMetricsAllocatedBytesAtStart = 0;
            _baselineMetricsLastFrame = -1;
        }

        private static bool PreparePerformanceFixture(out string status)
        {
            if (!_performanceFixtureSeeded)
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                {
                    status = "performanceFixture=waiting world=missing";
                    return false;
                }

                MatchPerformanceFixtureSeed.Result result =
                    MatchPerformanceFixtureSeed.Ensure(world.EntityManager);
                _performanceFixtureSeeded = true;
                _performanceFixtureStatus = result.ToString();
                if (!result.AddedEntities)
                {
                    _performanceFixtureReady = true;
                    status = _performanceFixtureStatus;
                    Debug.Log($"[MatchPerformanceFixture] {_performanceFixtureStatus} warmupFrames=0");
                    return true;
                }

                _performanceFixtureWarmupUntilFrame = Time.frameCount + MatchPerformanceFixtureSeed.WarmupFrames;
                status =
                    $"performanceFixture=warming frame={Time.frameCount}/{_performanceFixtureWarmupUntilFrame} " +
                    _performanceFixtureStatus;
                Debug.Log(
                    $"[MatchPerformanceFixture] {_performanceFixtureStatus} " +
                    $"warmupFrames={MatchPerformanceFixtureSeed.WarmupFrames}");
                return false;
            }

            if (Time.frameCount < _performanceFixtureWarmupUntilFrame)
            {
                status =
                    $"performanceFixture=warming frame={Time.frameCount}/{_performanceFixtureWarmupUntilFrame} " +
                    _performanceFixtureStatus;
                return false;
            }

            _performanceFixtureReady = true;
            status = _performanceFixtureStatus;
            Debug.Log($"[MatchPerformanceFixture] {_performanceFixtureStatus} warmup=complete");
            return true;
        }

        private static void ResetPerformanceFixtureState()
        {
            _performanceFixtureSeeded = false;
            _performanceFixtureReady = false;
            _performanceFixtureWarmupUntilFrame = -1;
            _performanceFixtureStatus = string.Empty;
        }

        [Serializable]
        private struct PerformanceRegressionAcceptedBaseline
        {
            public int acceptedBaselineVersion;
            public string acceptedAtUtc;
            public string source;
            public double editorP95FrameBudgetMs;
            public long currentThreadAllocatedBytesBudget;
            public int minimumFrameCount;
            public int minimumUnitCount;
            public int minimumRuntimeBuildingCount;
            public int minimumVisibleModelEstimate;

            public double EditorP95FrameBudgetMs => editorP95FrameBudgetMs;
            public long CurrentThreadAllocatedBytesBudget => currentThreadAllocatedBytesBudget;
            public int MinimumFrameCount => minimumFrameCount;
            public int MinimumUnitCount => minimumUnitCount;
            public int MinimumRuntimeBuildingCount => minimumRuntimeBuildingCount;
            public int MinimumVisibleModelEstimate => minimumVisibleModelEstimate;
        }

        private struct BaselineEntityCounts
        {
            public int UnitCount;
            public int RuntimeBuildingCount;
            public int GroundMissileProjectileCount;
            public int AirMissileProjectileCount;
            public int ProjectileCount;
            public int SelectionMarkerEntityCount;
            public int MinimapMarkerCount;
            public int MarkerCount;
            public int UnitModelInstanceCount;
            public int CulledUnitCount;
            public int VisibleModelEstimate;
            public int VisibleRenderStateCount;
            public int RenderVisualStateCount;
        }

        private static void CreateAirMissileSmokeScenario(EntityManager em)
        {
            _airSmokeStartedAt = EditorApplication.timeSinceStartup;
            _airSmokeProjectileSeen = false;
            _airSmokeTrailSeen = false;

            _airSmokeLauncher = em.CreateEntity(
                typeof(Faction),
                typeof(UnitHealth),
                typeof(LocalTransform),
                typeof(AirMissileLauncherComponent),
                typeof(AirMissileLauncherStateComponent),
                typeof(AirDefenseSupportLinkComponent));
            em.SetComponentData(_airSmokeLauncher, new Faction { Id = FactionIdentity.PlayerFactionId });
            em.SetComponentData(_airSmokeLauncher, new UnitHealth { Current = 500, Max = 500 });
            em.SetComponentData(_airSmokeLauncher, LocalTransform.FromPosition(new float3(0f, 0f, 0f)));
            em.SetComponentData(_airSmokeLauncher, new AirMissileLauncherComponent
            {
                MinRange = 1f,
                BaseDetectionRange = 320f,
                MaxDetectionRange = 420f,
                AirTargetPriority = 25f,
                IncomingMissilePriority = 100f,
                TurretYawSpeedDegreesPerSecond = 900f,
                AimToleranceDegrees = 15f,
                LockSeconds = 0.04f,
                LaunchDelaySeconds = 0.02f,
                ReloadSeconds = 1.2f,
                MissileSpeed = 100f,
                MissileAcceleration = 0f,
                MissileTurnRateDegreesPerSecond = 360f,
                MissileLifetimeSeconds = 5f,
                ProximityFuseRadius = 10f,
                AirTargetDamage = 60,
                IncomingMissileDamage = 9999,
                TrackingQuality = 1f,
                MaxSupportRangeBonus = 180f,
                MaxSupportTrackingBonus = 0.3f
            });
            em.SetComponentData(_airSmokeLauncher, new AirMissileLauncherStateComponent
            {
                Phase = (byte)AirMissileLauncherPhase.Idle,
                TargetEntity = Entity.Null,
                TargetKind = (byte)AirMissileTargetKind.None,
                EffectiveRange = 320f,
                EffectiveLockSeconds = 0.04f,
                EffectiveTrackingQuality = 1f,
                EffectiveTurnRateDegreesPerSecond = 360f
            });
            em.SetComponentData(_airSmokeLauncher, new AirDefenseSupportLinkComponent
            {
                LockTimeMultiplier = 1f
            });
            AddAirMissileVfxReference(em, _airSmokeLauncher);

            _airSmokeTarget = em.CreateEntity(
                typeof(Faction),
                typeof(UnitHealth),
                typeof(LocalTransform),
                typeof(UnitAirMovement));
            em.SetComponentData(_airSmokeTarget, new Faction { Id = FactionIdentity.EnemyFactionId });
            em.SetComponentData(_airSmokeTarget, new UnitHealth { Current = 100, Max = 100 });
            em.SetComponentData(_airSmokeTarget, LocalTransform.FromPosition(new float3(42f, 12f, 0f)));
            em.SetComponentData(_airSmokeTarget, new UnitAirMovement
            {
                CruiseHeight = 12f,
                RunwayTaxiSpeed = 5f
            });
        }

        private static void AddAirMissileVfxReference(EntityManager em, Entity launcher)
        {
            AirMissileLauncherConfig config = AssetDatabase.LoadAssetAtPath<AirMissileLauncherConfig>(AirLauncherConfigPath);
            if (config == null)
                return;

            em.AddComponentData(launcher, new AirMissileLauncherVfxReferenceComponent
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

        private static void TrackAirMissileProjectileState(EntityManager em)
        {
            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<AirMissileProjectileComponent>());
            using NativeArray<Entity> projectiles = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < projectiles.Length; i++)
            {
                Entity projectile = projectiles[i];
                AirMissileProjectileComponent projectileData = em.GetComponentData<AirMissileProjectileComponent>(projectile);
                if (projectileData.Source != _airSmokeLauncher)
                    continue;

                _airSmokeProjectileSeen = true;
                if (em.HasComponent<AirMissileProjectileTrailComponent>(projectile))
                    _airSmokeTrailSeen = true;
            }
        }

        private static void CleanupAirMissileSmoke()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                ResetAirMissileSmokeState();
                return;
            }

            EntityManager em = world.EntityManager;
            DestroyIfExists(em, _airSmokeLauncher);
            DestroyIfExists(em, _airSmokeTarget);

            using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<AirMissileProjectileComponent>());
            using NativeArray<Entity> projectiles = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < projectiles.Length; i++)
            {
                Entity projectile = projectiles[i];
                if (em.GetComponentData<AirMissileProjectileComponent>(projectile).Source == _airSmokeLauncher)
                    DestroyIfExists(em, projectile);
            }

            ResetAirMissileSmokeState();
        }

        private static void DestroyIfExists(EntityManager em, Entity entity)
        {
            if (entity != Entity.Null && em.Exists(entity))
                em.DestroyEntity(entity);
        }

        private static void ResetAirMissileSmokeState()
        {
            _airSmokeLauncher = Entity.Null;
            _airSmokeTarget = Entity.Null;
            _airSmokeProjectileSeen = false;
            _airSmokeTrailSeen = false;
            _airSmokeStartedAt = 0d;
        }

        private static void ResetResourceHaulerMovementState()
        {
            _resourceHaulerObservedEntity = Entity.Null;
            _resourceHaulerObservedStartPosition = default;
            _resourceHaulerObservedStartCell = default;
            _resourceHaulerObservedGoalCell = default;
            _resourceHaulerObservedStartedAt = 0d;
        }

        private static void Finish(bool passed, string details)
        {
            Debug.Log(
                "[MatchRuntimeShellSmokeValidation] " +
                $"result={(passed ? "Passed" : "Failed")} {details}");
            Cleanup();
            EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Application.logMessageReceived -= OnLogMessageReceived;
            CleanupAirMissileSmoke();
            RestorePlayModeReloadSettings();
            SessionState.EraseBool(ActiveKey);
            SessionState.EraseInt(PhaseKey);
            SessionState.EraseFloat(StartedAtKey);
            SessionState.EraseInt(ErrorCountKey);
            SessionState.EraseBool(RequireFrameDiagKey);
            SessionState.EraseBool(RequireAirMissileSmokeKey);
            SessionState.EraseBool(RequireBaselineMetricsKey);
            SessionState.EraseBool(RequirePerformanceRegressionReportKey);
            SessionState.EraseBool(RequireInitialBuildingSmokeKey);
            SessionState.EraseBool(RequireFuelReadinessKey);
            SessionState.EraseBool(RequireResourceHaulerMovementKey);
            SessionState.EraseString(EvidenceCommitKey);
            SessionState.EraseString(EvidenceEnvironmentKey);
            SessionState.EraseBool(EvidenceDirtyKey);
            SessionState.EraseString(FrameDiagKey);
            SessionState.EraseString(InitialBuildingImmediateStatusKey);
            SessionState.EraseFloat(ReadyAtKey);
            SessionState.EraseFloat(LastProgressLogAtKey);
            ResetBaselineMetricsState();
            ResetPerformanceFixtureState();
            ResetResourceHaulerMovementState();
            _resourceHaulerScenarioSeeded = false;
        }
    }
}
