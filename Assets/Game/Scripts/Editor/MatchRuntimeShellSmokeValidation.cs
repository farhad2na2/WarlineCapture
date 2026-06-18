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

public static class MatchRuntimeShellSmokeValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string MatchSceneName = "Match";
    private const string MatchHudContentName = "SCN08_MatchHudContent";
    private const string ActiveKey = "MatchRuntimeShellSmokeValidation.Active";
    private const string PhaseKey = "MatchRuntimeShellSmokeValidation.Phase";
    private const string StartedAtKey = "MatchRuntimeShellSmokeValidation.StartedAt";
    private const string ErrorCountKey = "MatchRuntimeShellSmokeValidation.ErrorCount";
    private const string RequireFrameDiagKey = "MatchRuntimeShellSmokeValidation.RequireFrameDiag";
    private const string RequireAirMissileSmokeKey = "MatchRuntimeShellSmokeValidation.RequireAirMissileSmoke";
    private const string RequireBaselineMetricsKey = "MatchRuntimeShellSmokeValidation.RequireBaselineMetrics";
    private const string FrameDiagKey = "MatchRuntimeShellSmokeValidation.FrameDiag";
    private const string ReadyAtKey = "MatchRuntimeShellSmokeValidation.ReadyAt";
    private const string BaselineMetricsReportPath = "/private/tmp/warlinecapture-match-runtime-baseline-metrics.json";
    private const double AirMissileSmokeTimeoutSeconds = 20d;
    private const double TimeoutSeconds = 120d;
    private const double StableFrameDiagObservationSeconds = 4d;
    private const double BaselineMetricsObservationSeconds = 4d;
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
        WaitingForBaselineMetrics = 6
    }

    private static Entity _airSmokeLauncher = Entity.Null;
    private static Entity _airSmokeTarget = Entity.Null;
    private static bool _airSmokeProjectileSeen;
    private static bool _airSmokeTrailSeen;
    private static double _airSmokeStartedAt;
    private static readonly List<double> BaselineFrameTimesMs = new(BaselineMetricsFrameTarget + 16);
    private static double _baselineMetricsStartedAt;
    private static long _baselineMetricsAllocatedBytesAtStart;
    private static int _baselineMetricsLastFrame = -1;

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
        try
        {
            ResetAirMissileSmokeState();
            ResetBaselineMetricsState();
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
            SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(ErrorCountKey, 0);
            SessionState.SetBool(RequireFrameDiagKey, requireFrameDiag);
            SessionState.SetBool(RequireAirMissileSmokeKey, requireAirMissileSmoke);
            SessionState.SetBool(RequireBaselineMetricsKey, requireBaselineMetrics);
            SessionState.EraseString(FrameDiagKey);
            SessionState.EraseFloat(ReadyAtKey);

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

        Finish(true, status);
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
            ComponentType.ReadWrite<UiShellRouteRequestComponent>());
        if (query.IsEmptyIgnoreFilter)
        {
            error = "UI shell boundary is missing.";
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
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
            ComponentType.ReadOnly<UiShellBoundaryComponent>(),
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
            status = "runtimeState=missing";
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
        if (!TryWriteBaselineMetricsReport(readyStatus, stableStatus, elapsedSeconds, allocatedBytes, out string reportStatus))
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

            StringBuilder builder = new();
            builder.AppendLine("{");
            AppendJson(builder, "observationSeconds", elapsedSeconds, trailingComma: true);
            AppendJson(builder, "frameCount", BaselineFrameTimesMs.Count, trailingComma: true);
            AppendJson(builder, "averageFrameMs", averageMs, trailingComma: true);
            AppendJson(builder, "p95FrameMs", p95Ms, trailingComma: true);
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
            File.WriteAllText(BaselineMetricsReportPath, builder.ToString());

            status =
                $"[MatchRuntimeBaselineMetrics] result=Passed report={BaselineMetricsReportPath} " +
                $"frames={BaselineFrameTimesMs.Count} avg={averageMs:F2}ms p95={p95Ms:F2}ms " +
                $"p99={p99Ms:F2}ms max={maxMs:F2}ms alloc={allocatedBytes} " +
                $"units={counts.UnitCount} buildings={counts.RuntimeBuildingCount} projectiles={counts.ProjectileCount} " +
                $"markers={counts.MarkerCount} visibleModels={counts.VisibleModelEstimate}";
            return true;
        }
        catch (Exception exception)
        {
            status = $"[MatchRuntimeBaselineMetrics] result=Failed {exception.Message}";
            return false;
        }
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
            ComponentType.ReadOnly<MatchHudMinimapMarkerBoundary>(),
            ComponentType.ReadOnly<MatchHudMinimapMarkerElement>());
        if (query.IsEmptyIgnoreFilter)
            return 0;

        int count = 0;
        using NativeArray<Entity> boundaries = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < boundaries.Length; i++)
            count += em.GetBuffer<MatchHudMinimapMarkerElement>(boundaries[i], true).Length;

        return count;
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
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseFloat(StartedAtKey);
        SessionState.EraseInt(ErrorCountKey);
        SessionState.EraseBool(RequireFrameDiagKey);
        SessionState.EraseBool(RequireAirMissileSmokeKey);
        SessionState.EraseBool(RequireBaselineMetricsKey);
        SessionState.EraseString(FrameDiagKey);
        SessionState.EraseFloat(ReadyAtKey);
        ResetBaselineMetricsState();
    }
}
