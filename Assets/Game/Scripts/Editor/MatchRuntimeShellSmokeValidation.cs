using System;
using Unity.Collections;
using Unity.Entities;
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
    private const string FrameDiagKey = "MatchRuntimeShellSmokeValidation.FrameDiag";
    private const string ReadyAtKey = "MatchRuntimeShellSmokeValidation.ReadyAt";
    private const double TimeoutSeconds = 120d;
    private const double StableFrameDiagObservationSeconds = 4d;

    private enum Phase
    {
        Idle = 0,
        WaitingForPlayMode = 1,
        WaitingForShellReady = 2,
        WaitingForMatchReady = 3,
        WaitingForFrameDiag = 4
    }

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
        RunInternal(requireFrameDiag: true);
    }

    private static void RunInternal(bool requireFrameDiag)
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
            SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
            SessionState.SetInt(ErrorCountKey, 0);
            SessionState.SetBool(RequireFrameDiagKey, requireFrameDiag);
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

        if (phase != Phase.WaitingForMatchReady)
            return;

        if (!IsMatchRuntimeReady(out string status))
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
               curtainHidden;
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
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseFloat(StartedAtKey);
        SessionState.EraseInt(ErrorCountKey);
        SessionState.EraseBool(RequireFrameDiagKey);
        SessionState.EraseString(FrameDiagKey);
        SessionState.EraseFloat(ReadyAtKey);
    }
}
