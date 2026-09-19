#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Test setup and read-only evidence only: match actions must come from the shipping touch driver.
    [InitializeOnLoad]
    public static class AriaPlayEditorValidation
    {
        private const string Key = "Warline.AriaPlayValidation";
        private const string ProfileRoot = "/private/tmp/aria-play-validation-profile";
        static AriaPlayEditorValidation()
        {
            EditorApplication.playModeStateChanged += phase =>
            {
                if (phase == PlayModeStateChange.EnteredEditMode) RestoreProfileRoot();
            };
            if (!EditorApplication.isPlayingOrWillChangePlaymode) RestoreProfileRoot();
        }
        private static void RestoreProfileRoot()
        {
            if (Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") != ProfileRoot) return;
            var previous = SessionState.GetString(Key, "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", string.IsNullOrEmpty(previous) ? null : previous);
        }
        private static int setupMission, setupPhase;
        private static string setupLocale = "en";
        private static double setupDeadline, setupAfter;
        public static void Launch(int mission = 0, string locale = "en")
        {
            if (locale != Game.Configs.GameLocalization.EnglishLocaleCode && locale != Game.Configs.GameLocalization.PersianLocaleCode)
                throw new ArgumentException("Use catalog locale en or fa-IR.", nameof(locale));
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Requires idle Editor.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene.");
            SessionState.SetString(Key, Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") ?? "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT", ProfileRoot);
            var save = new SaveService(new JsonSaveRepository(ProfileRoot));
            var profile = save.LoadProfile();
            profile.firstLaunchStatus = FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            save.SaveProfile(profile);
            setupMission = mission; setupLocale = locale; setupPhase = 0; setupDeadline = EditorApplication.timeSinceStartup + 90;
            if (mission != 0) { EditorApplication.update -= SetupMatch; EditorApplication.update += SetupMatch; }
            EditorApplication.EnterPlaymode();
        }
        private static void SetupMatch()
        {
            if (EditorApplication.timeSinceStartup > setupDeadline)
            { EditorApplication.update -= SetupMatch; Debug.LogError("ARIA test setup timed out"); return; }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < setupAfter) return;
            try
            {
                if (World.DefaultGameObjectInjectionWorld == null) return;
                if (setupPhase == 0)
                {
                    if (setupMission < 0)
                    {
                        Game.Configs.GameLocalization.SetLocale(setupLocale, false);
                        if (!UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup, false)) return;
                    }
                    else if (Prepare(setupMission, setupLocale) != "True") return;
                    setupPhase = 1; setupAfter = EditorApplication.timeSinceStartup + .5; return;
                }
                if (setupMission < 0)
                {
                    var setup = UnityEngine.Object.FindAnyObjectByType<QuickCustomScreenView>();
                    if (setup == null) return;
                    setup.LaunchMatch(); EditorApplication.update -= SetupMatch;
                }
                else if (Deploy(setupMission) == "True") EditorApplication.update -= SetupMatch;
            }
            catch (Exception error)
            { EditorApplication.update -= SetupMatch; Debug.LogException(error); }
        }
        public static string Prepare(int mission, string locale = "en")
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var q = em.CreateEntityQuery(typeof(CampaignMissionProgressStoreReferenceComponent));
            if (q.CalculateEntityCount() != 1) return "waiting for campaign";
            var store = em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(q.GetSingletonEntity()).Store;
            string[] ids = { "saga.ch01.m01.first_contact", "saga.ch01.m02.establish_base", "saga.ch01.m03.radar_warning", "saga.ch01.m04.airlift", "saga.ch01.m05.breach_assault" };
            store.EnsureAvailable(ids[mission - 1]);
            Game.Configs.GameLocalization.SetLocale(locale, false);
            return UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select, ids[mission - 1]).ToString();
        }
        public static string Deploy(int mission)
        {
            string[] ids = { "saga.ch01.m01.first_contact", "saga.ch01.m02.establish_base", "saga.ch01.m03.radar_warning", "saga.ch01.m04.airlift", "saga.ch01.m05.breach_assault" };
            return UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy, ids[mission - 1]).ToString();
        }
        public static string Status()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var q = em.CreateEntityQuery(typeof(AriaPlaySessionComponent), typeof(AriaPlayObservationComponent));
            string result = "no observation";
            if (q.CalculateEntityCount() == 1)
                result = JsonUtility.ToJson(q.GetSingleton<AriaPlaySessionComponent>()) + "\n" + JsonUtility.ToJson(q.GetSingleton<AriaPlayObservationComponent>());
            var aria = UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
            return result + "\n" + (aria != null ? aria.CurrentInstructionBody : "no ARIA view");
        }
        public static void Restore()
        {
            UiShellRuntimeGateway.StopAriaPlay();
            EditorApplication.ExitPlaymode();
            RestoreProfileRoot();
        }
    }
}
#endif
