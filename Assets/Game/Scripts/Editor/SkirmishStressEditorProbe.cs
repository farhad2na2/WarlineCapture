#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
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
        public const string PlayerSetupScreenshotFile = "e03-player-setup-two-battles.png";
        private static int stage;
        private static double next;
        private static double deadline;
        private static int lastCapturedSamples;

        static SkirmishStressEditorProbe()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch E0.3 Player Setup Capture")]
        public static void LaunchPlayerSetupCapture()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the setup capture.");
            SessionState.SetBool(Key + ".SetupOnly", true);
            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            lastCapturedSamples = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
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

        [MenuItem("Tools/Warline/Skirmish/Capture E0.3 Census Now")]
        public static void CaptureCensusNowMenu()
        {
            Debug.Log(SkirmishStressRecipe.ReportMarker + " captureNow=" + CaptureCensusNow());
        }

        [MenuItem("Tools/Warline/Skirmish/Capture E0.3 Screenshot/Player Setup Two Battles")]
        public static void CapturePlayerSetupScreenshot()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter play mode on Skirmish setup first, then capture.");
            string path = CaptureNamedScreenshot(PlayerSetupScreenshotFile);
            Debug.Log(SkirmishStressRecipe.ReportMarker + " setupScreenshot=" + path);
        }

        [MenuItem("Tools/Warline/Skirmish/Capture E0.3 Screenshot/Current Phase")]
        public static void CaptureCurrentPhaseScreenshot()
        {
            if (!TryGetLiveSession(out _, out _, out var session))
                throw new InvalidOperationException("Enter play mode with Scenario 2 stress first.");
            string path = CapturePhaseScreenshot(session, session.ActivePhase);
            Debug.Log(SkirmishStressRecipe.ReportMarker + " phaseScreenshot=" + path);
        }

        public static void Launch(SkirmishStressScale scale, SkirmishStressPhase phase, SkirmishStressLayout layout, bool sequence)
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the stress probe.");
            SessionState.SetInt(Key + ".Scale", (int)scale);
            SessionState.SetInt(Key + ".Phase", (int)phase);
            SessionState.SetInt(Key + ".Layout", (int)layout);
            SessionState.SetBool(Key + ".Sequence", sequence);
            SessionState.SetInt(Key + ".Seed", SkirmishStressRecipe.FixedSeed);
            SessionState.SetBool(Key + ".SetupOnly", false);
            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            lastCapturedSamples = 0;
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

        public static string ReportDirectory()
        {
            string directory = Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_STRESS_REPORT_DIR");
            if (string.IsNullOrWhiteSpace(directory))
                directory = Path.Combine(Path.GetTempPath(), "warline-e03-stress");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string ScreenshotFileName(in SkirmishStressSession session, SkirmishStressPhaseCode phase)
        {
            string phaseName = phase.ToString().ToLowerInvariant();
            string layout = session.Layout == SkirmishStressLayoutCode.Concentrated ? "concentrated" : "spread";
            return "e03-" + phaseName + "-" + layout + "-p" + session.Scale + ".png";
        }

        public static string CaptureCensusNow()
        {
            if (!TryGetLiveSession(out var em, out var entity, out var session))
                throw new InvalidOperationException("Enter play mode with Scenario 2 stress first.");
            SkirmishStressDirector.RecordSample(em, entity, session);
            var samples = em.GetBuffer<SkirmishStressCensusSample>(entity);
            string reportPath = WriteReport(SkirmishStressCensus.FormatReport(session, samples));
            string shot = CapturePhaseScreenshot(session, session.ActivePhase);
            CaptureNamedScreenshot("e03-census-now.png");
            return reportPath + " screenshot=" + shot;
        }

        public static string CapturePhaseScreenshot(in SkirmishStressSession session, SkirmishStressPhaseCode phase)
        {
            return CaptureNamedScreenshot(ScreenshotFileName(session, phase));
        }

        public static string CaptureNamedScreenshot(string fileName)
        {
            string path = Path.Combine(ReportDirectory(), fileName);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log(SkirmishStressRecipe.ReportMarker + " screenshot queued=" + path);
            return path;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (SessionState.GetBool(Key + ".SetupOnly", false))
            {
                TickSetupCapture(now);
                return;
            }
            if (deadline == 0) deadline = now + 420;
            if (now > deadline)
            {
                TryWritePartialReport("timeout");
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
                    CapturePhaseScreenshot(session, session.ActivePhase);
                    stage = 2;
                    next = now + 1;
                    return;
                }
                var samples = em.GetBuffer<SkirmishStressCensusSample>(query.GetSingletonEntity());
                if (samples.Length > lastCapturedSamples)
                {
                    var newest = samples[samples.Length - 1];
                    CapturePhaseScreenshot(session, newest.Phase);
                    WriteReport(SkirmishStressCensus.FormatReport(session, samples));
                    lastCapturedSamples = samples.Length;
                }
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

        private static void TickSetupCapture(double now)
        {
            if (deadline == 0) deadline = now + 60;
            if (now > deadline)
            {
                Finish("Setup capture timed out at stage " + stage);
                return;
            }
            if (now < next) return;
            try
            {
                if (stage == 0)
                {
                    GameLocalization.SetLocale("en", false);
                    if (!UiShellRuntimeGateway.TryEnqueueRouteRequest(
                            UiShellRouteIntent.OpenMenuRoute, UIRoute.QuickCustomSetup, false))
                        return;
                    stage = 1;
                    next = now + 2;
                    return;
                }
                var setup = Object.FindAnyObjectByType<QuickCustomScreenView>();
                if (setup == null) return;
                string path = CaptureNamedScreenshot(PlayerSetupScreenshotFile);
                Debug.Log(SkirmishStressRecipe.ReportMarker + " setupScreenshot=" + path);
                Finish("Player setup captured=" + path);
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

        private static bool TryGetLiveSession(out EntityManager em, out Entity entity, out SkirmishStressSession session)
        {
            em = default;
            entity = Entity.Null;
            session = default;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;
            em = world.EntityManager;
            using var query = em.CreateEntityQuery(typeof(SkirmishMatchState), typeof(SkirmishStressSession));
            if (query.CalculateEntityCount() != 1) return false;
            entity = query.GetSingletonEntity();
            session = em.GetComponentData<SkirmishStressSession>(entity);
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
            string path = Path.Combine(ReportDirectory(), "E0_3_STRESS_CENSUS.md");
            File.WriteAllText(path, report);
            return path;
        }

        private static void TryWritePartialReport(string reason)
        {
            try
            {
                if (!TryGetLiveSession(out var em, out var entity, out var session)) return;
                if (!em.HasBuffer<SkirmishStressCensusSample>(entity)) return;
                var samples = em.GetBuffer<SkirmishStressCensusSample>(entity);
                if (samples.Length == 0)
                    SkirmishStressDirector.RecordSample(em, entity, session);
                samples = em.GetBuffer<SkirmishStressCensusSample>(entity);
                string path = WriteReport(SkirmishStressCensus.FormatReport(session, samples)
                    + Environment.NewLine + "Partial write reason: `" + reason + "`.");
                Debug.Log(SkirmishStressRecipe.ReportMarker + " partialReport=" + path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(SkirmishStressRecipe.ReportMarker + " partial report failed: " + exception.Message);
            }
        }

        private static void Finish(string message)
        {
            SessionState.SetBool(Key, false);
            SessionState.SetBool(Key + ".SetupOnly", false);
            EditorApplication.update -= Tick;
            Debug.Log("[SkirmishStressProbe] " + message);
        }
    }
}
#endif
