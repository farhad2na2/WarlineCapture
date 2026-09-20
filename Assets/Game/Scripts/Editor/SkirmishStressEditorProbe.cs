#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class SkirmishStressEditorProbe
    {
        private const string Key = "Warline.SkirmishStressProbe";
        private static int stage;
        private static double next;
        private static double deadline;

        static SkirmishStressEditorProbe()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress Sequence")]
        public static void LaunchDefaultSequence()
        {
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.Warmup, SkirmishStressLayout.Spread, sequence: true);
        }

        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Warmup Spread P100")]
        public static void LaunchWarmup() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.Warmup, SkirmishStressLayout.Spread, false);
        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Idle Spread P100")]
        public static void LaunchIdle() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.Idle, SkirmishStressLayout.Spread, false);
        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Mass Move Spread P100")]
        public static void LaunchMassMove() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.MassMove, SkirmishStressLayout.Spread, false);
        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Dense Combat Concentrated P100")]
        public static void LaunchDenseCombat() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.DenseCombat, SkirmishStressLayout.Concentrated, false);
        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Air Transport Spread P100")]
        public static void LaunchAirTransport() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.AirTransport, SkirmishStressLayout.Spread, false);
        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Destruction Spread P100")]
        public static void LaunchDestruction() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.Destruction, SkirmishStressLayout.Spread, false);
        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Stress/Concentrated Sequence P100")]
        public static void LaunchConcentratedSequence() =>
            Launch(SkirmishStressScale.P100, SkirmishStressPhase.Warmup, SkirmishStressLayout.Concentrated, true);

        public static void Launch(SkirmishStressScale scale, SkirmishStressPhase phase, SkirmishStressLayout layout, bool sequence)
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the stress probe.");
            SessionState.SetInt(Key + ".Scale", (int)scale);
            SessionState.SetInt(Key + ".Phase", (int)phase);
            SessionState.SetInt(Key + ".Layout", (int)layout);
            SessionState.SetBool(Key + ".Sequence", sequence);
            SessionState.SetInt(Key + ".Seed", SkirmishStressRecipe.FixedSeed);
            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        public static string RunFocusedValidation()
        {
            return SkirmishStressValidation.Run();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0) deadline = now + 420;
            if (now > deadline)
            {
                Finish("Timed out at stage " + stage);
                return;
            }
            if (now < next) return;
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                if (stage == 0)
                {
                    if (!TryQueue(em)) return;
                    stage = 1;
                    next = now + 2;
                    return;
                }
                using var query = em.CreateEntityQuery(typeof(SkirmishMatchState), typeof(SkirmishStressSession));
                if (query.CalculateEntityCount() != 1) return;
                var match = query.GetSingleton<SkirmishMatchState>();
                var session = query.GetSingleton<SkirmishStressSession>();
                if (match.Phase != SkirmishPhase.Playing) return;
                if (stage == 1)
                {
                    Debug.Log(SkirmishStressRecipe.ReportMarker + " playing seed=" + match.Seed
                        + " scale=" + session.Scale + " layout=" + session.Layout
                        + " sequence=" + session.Sequence);
                    stage = 2;
                    next = now + 1;
                    return;
                }
                var samples = em.GetBuffer<SkirmishStressCensusSample>(query.GetSingletonEntity());
                if (samples.Length == 0) return;
                if (session.Sequence != 0 && !HasPhase(samples, SkirmishStressPhaseCode.Destruction))
                    return;
                string report = SkirmishStressCensus.FormatReport(session, samples);
                string path = WriteReport(report);
                Debug.Log(SkirmishStressRecipe.ReportMarker + " report=" + path);
                Finish("Census samples=" + samples.Length);
            }
            catch (Exception exception)
            {
                Finish(exception.ToString());
            }
        }

        private static bool TryQueue(EntityManager em)
        {
            var config = QuickGameConfig.Defaults;
            config.MapSeed = SessionState.GetInt(Key + ".Seed", SkirmishStressRecipe.FixedSeed);
            config.ScenarioIndex = SkirmishPresetConfig.StressScaleProbeScenarioIndex;
            using var shell = em.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            if (shell.IsEmptyIgnoreFilter) return false;
            if (!SkirmishLaunchProjection.TryQueue(em, config)) return false;
            if (!SkirmishLaunchProjection.TryGet(em, out var session, out _)) return false;
            em.AddComponentData(session, new SkirmishStressLaunchRequest
            {
                Seed = config.MapSeed,
                Scale = SessionState.GetInt(Key + ".Scale", (int)SkirmishStressScale.P100),
                Phase = (SkirmishStressPhaseCode)SessionState.GetInt(Key + ".Phase", 0),
                Layout = (SkirmishStressLayoutCode)SessionState.GetInt(Key + ".Layout", 0),
                Sequence = SessionState.GetBool(Key + ".Sequence", true) ? (byte)1 : (byte)0
            });
            var routes = em.GetBuffer<UiShellRouteRequestComponent>(shell.GetSingletonEntity());
            routes.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            return true;
        }

        private static bool HasPhase(DynamicBuffer<SkirmishStressCensusSample> samples, SkirmishStressPhaseCode phase)
        {
            for (int i = 0; i < samples.Length; i++)
                if (samples[i].Phase == phase) return true;
            return false;
        }

        private static string WriteReport(string report)
        {
            string directory = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STRESS_REPORT_DIR");
            if (string.IsNullOrWhiteSpace(directory))
                directory = Path.Combine(Path.GetTempPath(), "warline-e03-stress");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "E0_3_STRESS_CENSUS.md");
            File.WriteAllText(path, report);
            return path;
        }

        private static void Finish(string message)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Debug.Log("[SkirmishStressProbe] " + message);
        }
    }
}
#endif
