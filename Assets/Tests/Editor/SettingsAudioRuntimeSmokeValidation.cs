using System;
using Game.Components;
using Game.Runtime;
using Game.UI.Runtime;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using SettingsService = Game.UI.Runtime.SettingsService;

public static class SettingsAudioRuntimeSmokeValidation
{
    private const string MenuScenePath = "Assets/Game/Scenes/Menu.unity";
    private const string MatchSceneName = "Match";
    private const string MatchHudContentName = "SCN08_MatchHudContent";
    private const string ActiveKey = "SettingsAudioRuntimeSmokeValidation.Active";
    private const string PhaseKey = "SettingsAudioRuntimeSmokeValidation.Phase";
    private const string ModeKey = "SettingsAudioRuntimeSmokeValidation.Mode";
    private const string StartedAtKey = "SettingsAudioRuntimeSmokeValidation.StartedAt";
    private const double TimeoutSeconds = 90d;

    private enum SmokeMode
    {
        Menu = 0,
        Match = 1
    }

    private enum Phase
    {
        Idle = 0,
        WaitingForPlayMode = 1,
        WaitingForMenuWorld = 2,
        WaitingForMutedProjection = 3,
        WaitingForEnabledProjection = 4,
        WaitingForMatchReady = 5,
        WaitingForMatchMutedProjection = 6,
        WaitingForMatchEnabledProjection = 7
    }

    [InitializeOnLoadMethod]
    private static void ResumeActiveValidation()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        RegisterCallbacks();
    }

    public static void RunMenuSettingsSmoke()
    {
        StartSmoke(SmokeMode.Menu);
    }

    public static void RunMatchSettingsSmoke()
    {
        StartSmoke(SmokeMode.Match);
    }

    private static void StartSmoke(SmokeMode mode)
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForPlayMode);
            SessionState.SetInt(ModeKey, (int)mode);
            SessionState.SetFloat(StartedAtKey, (float)EditorApplication.timeSinceStartup);
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
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForMenuWorld);
    }

    private static void Update()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(StartedAtKey, 0f) > TimeoutSeconds)
        {
            Finish(false, "Timed out waiting for settings audio runtime smoke validation. " + DescribeWaitState());
            return;
        }

        Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);
        if (phase == Phase.WaitingForPlayMode)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.EnterPlaymode();
            return;
        }

        if (phase == Phase.WaitingForMenuWorld)
        {
            if (!TryGetAudioSettings(out _, out string worldStatus))
                return;

            SmokeMode mode = (SmokeMode)SessionState.GetInt(ModeKey, (int)SmokeMode.Menu);
            if (mode == SmokeMode.Match)
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
                Debug.Log("[SettingsAudioRuntimeSmoke] matchRouteQueued " + worldStatus);
                return;
            }

            SettingsService.ApplyRuntime(CreateMutedModel());
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForMutedProjection);
            Debug.Log("[SettingsAudioRuntimeSmoke] appliedMutedModel " + worldStatus);
            return;
        }

        if (phase == Phase.WaitingForMutedProjection)
        {
            if (!TryGetAudioSettings(out AudioSettingsComponent settings, out _))
                return;

            if (!MatchesMutedModel(settings, out string mismatch))
                return;

            SettingsService.ApplyRuntime(CreateEnabledModel());
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForEnabledProjection);
            Debug.Log("[SettingsAudioRuntimeSmoke] mutedProjectionPassed " + DescribeSettings(settings));
            return;
        }

        if (phase == Phase.WaitingForEnabledProjection)
        {
            if (!TryGetAudioSettings(out AudioSettingsComponent settings, out _))
                return;

            if (!MatchesEnabledModel(settings, out string mismatch))
                return;

            Finish(true, "Menu settings ApplyRuntime projected disabled and enabled Music/Sound/Voice bus states. " + DescribeSettings(settings));
        }

        if (phase == Phase.WaitingForMatchReady)
        {
            if (!IsMatchRuntimeReady(out _))
                return;

            SettingsService.ApplyRuntime(CreateMutedModel());
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForMatchMutedProjection);
            Debug.Log("[SettingsAudioRuntimeSmoke] appliedMatchMutedModel " + DescribeCurrentState());
            return;
        }

        if (phase == Phase.WaitingForMatchMutedProjection)
        {
            if (!IsMatchRuntimeReady(out _))
                return;

            if (!TryGetAudioSettings(out AudioSettingsComponent settings, out _))
                return;

            if (!MatchesMutedModel(settings, out string mismatch))
                return;

            SettingsService.ApplyRuntime(CreateEnabledModel());
            SessionState.SetInt(PhaseKey, (int)Phase.WaitingForMatchEnabledProjection);
            Debug.Log("[SettingsAudioRuntimeSmoke] matchMutedProjectionPassed " + DescribeSettings(settings));
            return;
        }

        if (phase == Phase.WaitingForMatchEnabledProjection)
        {
            if (!IsMatchRuntimeReady(out string matchStatus))
                return;

            if (!TryGetAudioSettings(out AudioSettingsComponent settings, out _))
                return;

            if (!MatchesEnabledModel(settings, out string mismatch))
                return;

            Finish(true, "Match settings ApplyRuntime projected disabled and enabled Music/Sound/Voice bus states while HUD stayed active. " + matchStatus + " " + DescribeSettings(settings));
        }
    }

    private static UISettingsModel CreateMutedModel()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.MasterVolume = 40f;
        model.Audio.MusicVolume = 20f;
        model.Audio.SfxVolume = 30f;
        model.Audio.AlertsVolume = 50f;
        model.Audio.VoiceVolume = 60f;
        model.Audio.MusicEnabled = false;
        model.Audio.SoundEnabled = false;
        model.Audio.VoiceEnabled = false;
        return model;
    }

    private static UISettingsModel CreateEnabledModel()
    {
        UISettingsModel model = SettingsService.Defaults;
        model.Audio.MasterVolume = 80f;
        model.Audio.MusicVolume = 70f;
        model.Audio.SfxVolume = 65f;
        model.Audio.AlertsVolume = 55f;
        model.Audio.VoiceVolume = 75f;
        model.Audio.MusicEnabled = true;
        model.Audio.SoundEnabled = true;
        model.Audio.VoiceEnabled = true;
        return model;
    }

    private static bool TryGetAudioSettings(out AudioSettingsComponent settings, out string status)
    {
        settings = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            status = "world=missing";
            return false;
        }

        EntityManager em = world.EntityManager;
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        if (!em.HasComponent<AudioSettingsComponent>(audioEntity))
        {
            status = "audioSettings=missing";
            return false;
        }

        settings = em.GetComponentData<AudioSettingsComponent>(audioEntity);
        status = "world=ready " + DescribeSettings(settings);
        return true;
    }

    private static bool MatchesMutedModel(AudioSettingsComponent settings, out string mismatch)
    {
        if (settings.MusicMuted != 1 ||
            settings.SfxMuted != 1 ||
            settings.UiMuted != 1 ||
            settings.AlertsMuted != 1 ||
            settings.AmbienceMuted != 1 ||
            settings.VoiceMuted != 1)
        {
            mismatch = "mute flags did not match disabled model. " + DescribeSettings(settings);
            return false;
        }

        if (!Approximately(settings.MasterVolume, 0.4f) ||
            !Approximately(settings.MusicVolume, 0.2f) ||
            !Approximately(settings.SfxVolume, 0.3f) ||
            !Approximately(settings.UiVolume, 0.3f) ||
            !Approximately(settings.AlertsVolume, 0.5f) ||
            !Approximately(settings.VoiceVolume, 0.6f))
        {
            mismatch = "volumes did not match disabled model. " + DescribeSettings(settings);
            return false;
        }

        mismatch = string.Empty;
        return true;
    }

    private static bool MatchesEnabledModel(AudioSettingsComponent settings, out string mismatch)
    {
        if (settings.MusicMuted != 0 ||
            settings.SfxMuted != 0 ||
            settings.UiMuted != 0 ||
            settings.AlertsMuted != 0 ||
            settings.AmbienceMuted != 0 ||
            settings.VoiceMuted != 0)
        {
            mismatch = "mute flags did not match enabled model. " + DescribeSettings(settings);
            return false;
        }

        if (!Approximately(settings.MasterVolume, 0.8f) ||
            !Approximately(settings.MusicVolume, 0.7f) ||
            !Approximately(settings.SfxVolume, 0.65f) ||
            !Approximately(settings.UiVolume, 0.65f) ||
            !Approximately(settings.AlertsVolume, 0.55f) ||
            !Approximately(settings.VoiceVolume, 0.75f))
        {
            mismatch = "volumes did not match enabled model. " + DescribeSettings(settings);
            return false;
        }

        mismatch = string.Empty;
        return true;
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
                if (TreeContains(roots[rootIndex].transform, MatchHudContentName))
                    return true;

                if (roots[rootIndex].GetComponentInChildren<MatchOverlayCommandControlsView>(true) != null ||
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

    private static bool Approximately(float actual, float expected)
    {
        return Mathf.Abs(actual - expected) <= 0.001f;
    }

    private static string DescribeCurrentState()
    {
        return TryGetAudioSettings(out AudioSettingsComponent settings, out string status)
            ? status
            : status;
    }

    private static string DescribeWaitState()
    {
        Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);
        SmokeMode mode = (SmokeMode)SessionState.GetInt(ModeKey, (int)SmokeMode.Menu);
        string audioStatus = DescribeCurrentState();
        string matchStatus = IsMatchRuntimeReady(out string status) ? status : status;
        return $"mode={mode} phase={phase} audio=({audioStatus}) match=({matchStatus})";
    }

    private static string DescribeSettings(AudioSettingsComponent settings)
    {
        return
            $"version={settings.Version} " +
            $"master={settings.MasterVolume:0.###} " +
            $"music={settings.MusicVolume:0.###}/{settings.MusicMuted} " +
            $"sfx={settings.SfxVolume:0.###}/{settings.SfxMuted} " +
            $"ui={settings.UiVolume:0.###}/{settings.UiMuted} " +
            $"alerts={settings.AlertsVolume:0.###}/{settings.AlertsMuted} " +
            $"ambience={settings.AmbienceVolume:0.###}/{settings.AmbienceMuted} " +
            $"voice={settings.VoiceVolume:0.###}/{settings.VoiceMuted}";
    }

    private static void Finish(bool passed, string message)
    {
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseInt(ModeKey);
        SessionState.EraseFloat(StartedAtKey);

        SettingsService.ApplyRuntime(SettingsService.Load());

        if (passed)
            Debug.Log("[SettingsAudioRuntimeSmoke] result=Passed " + message);
        else
            Debug.LogError("[SettingsAudioRuntimeSmoke] result=Failed " + message);

        if (Application.isBatchMode)
            EditorApplication.Exit(passed ? 0 : 1);
    }
}
