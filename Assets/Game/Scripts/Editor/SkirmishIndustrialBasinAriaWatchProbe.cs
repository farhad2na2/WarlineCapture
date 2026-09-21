#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Launches Industrial Basin (scenario index 3), starts Watch ARIA through the approved API,
    /// and records an autonomous English victory or failure for Editor baseline evidence.
    /// </summary>
    [InitializeOnLoad]
    public static class SkirmishIndustrialBasinAriaWatchProbe
    {
        private const string Key = "Warline.IndustrialBasinAriaWatch";
        private const string EvidenceDirectory = "/private/tmp/warline-s3-aria";
        private const string TelemetryFile = "aria-s3-ib-en.jsonl";
        private const int MapSeed = 104729;
        private const double MatchDeadlineSeconds = 900d;

        private static int stage;
        private static double next;
        private static double deadline;
        private static double matchStartedAt;
        private static int completedGestures;
        private static int lastActions;

        static SkirmishIndustrialBasinAriaWatchProbe()
        {
            if (SessionState.GetBool(Key, false))
                EditorApplication.update += Tick;
        }

        public static string AcceptExpandedPayload(in SkirmishAriaAcceptancePayload payload)
        {
            if (!payload.TryValidate(out string error))
                throw new ArgumentException(error, nameof(payload));
            string line = "[SkirmishIndustrialBasinAriaWatch] selected " + payload.FormatSelectedConfiguration();
            Debug.Log(line);
            return line;
        }

        [MenuItem("Tools/Warline/Skirmish/Launch S3 Industrial Basin ARIA Watch EN")]
        public static void LaunchEnglish()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit play mode before launching the S3 ARIA probe.");

            SessionState.SetBool(Key, true);
            stage = 0;
            next = 0;
            deadline = 0;
            matchStartedAt = 0;
            completedGestures = 0;
            lastActions = 0;
            Directory.CreateDirectory(EvidenceDirectory);
            var telemetry = Path.Combine(EvidenceDirectory, TelemetryFile);
            if (File.Exists(telemetry))
                File.Delete(telemetry);

            SessionState.SetString("Warline.AriaPlayValidation",
                Environment.GetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT") ?? "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",
                "/private/tmp/aria-play-validation-profile");
            var save = new Game.Runtime.SaveService(
                new Game.Runtime.JsonSaveRepository("/private/tmp/aria-play-validation-profile"));
            var profile = save.LoadProfile();
            profile.firstLaunchStatus = Game.Runtime.FirstLaunchProfileState.Completed;
            profile.firstLaunchWatched = true;
            save.SaveProfile(profile);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920, 1080);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Game/Scenes/Menu.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
                return;

            double now = EditorApplication.timeSinceStartup;
            if (deadline == 0d)
                deadline = now + MatchDeadlineSeconds + 180d;
            if (now > deadline)
            {
                Finish("timeout");
                return;
            }

            if (now < next)
                return;

            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;
                EntityManager em = world.EntityManager;

                if (stage == 0)
                {
                    GameLocalization.SetLocale("en", false);
                    if (!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em))
                    {
                        next = now + 0.5d;
                        return;
                    }

                    if (!TryQueue(em))
                        return;
                    stage = 1;
                    next = now + 2d;
                    return;
                }

                if (!SkirmishLaunchProjection.TryGet(em, out Entity session, out SkirmishMatchState match))
                    return;

                if (stage == 1)
                {
                    SkirmishLaunchProjection.TryEnterMatch(em);
                    SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
                    SkirmishLaunchProjection.TryRequestPlay(em);
                    if (match.StartupFailure != SkirmishStartupFailureCode.None)
                    {
                        Finish("startupFailure=" + match.StartupFailure);
                        return;
                    }

                    if (match.Phase != SkirmishPhase.Playing)
                    {
                        next = now + 0.5d;
                        return;
                    }

                    matchStartedAt = now;
                    Debug.Log($"[SkirmishIndustrialBasinAria] playing seed={match.Seed} scenario={match.ScenarioIndex}");
                    stage = 2;
                    next = now + 1d;
                    return;
                }

                if (stage == 2)
                {
                    // User-approved temporary API start; gameplay remains the shipping touch driver.
                    if (!UiShellRuntimeGateway.TryStartAriaPlay())
                    {
                        next = now + 0.5d;
                        return;
                    }

                    Debug.Log("[SkirmishIndustrialBasinAria] watchStarted=1");
                    stage = 3;
                    next = now + 1d;
                    return;
                }

                if (stage == 3)
                {
                    AppendTelemetry(em, session, match);
                    if (match.Phase == SkirmishPhase.Finished)
                    {
                        Finish(match.Outcome == SkirmishOutcome.Victory ? "victory" : "finished");
                        return;
                    }

                    next = now + 2d;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish("exception");
            }
        }

        private static bool TryQueue(EntityManager em)
        {
            QuickGameConfig config = QuickGameConfig.Defaults;
            config.MapSeed = MapSeed;
            config.ScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex;
            if (SkirmishLaunchProjection.TryGet(em, out _, out _))
                return false;
            if (!SkirmishLaunchProjection.TryQueue(em, config))
                return false;
            if (!SkirmishLaunchProjection.TryGet(em, out _, out var match))
                return false;
            Debug.Log($"[SkirmishIndustrialBasinAria] queued scenario={match.ScenarioIndex} seed={match.Seed}");
            return true;
        }

        private static void AppendTelemetry(EntityManager em, Entity session, in SkirmishMatchState match)
        {
            using var q = em.CreateEntityQuery(typeof(AriaPlaySessionComponent), typeof(AriaPlayObservationComponent));
            if (q.CalculateEntityCount() != 1)
                return;
            var touch = q.GetSingleton<AriaPlaySessionComponent>();
            if (touch.Actions > lastActions)
            {
                completedGestures += touch.Actions - lastActions;
                lastActions = touch.Actions;
            }

            var observation = q.GetSingleton<AriaPlayObservationComponent>();
            string line = JsonUtility.ToJson(new TelemetryRow
            {
                Elapsed = match.ElapsedSeconds,
                Phase = (int)match.Phase,
                Outcome = (int)match.Outcome,
                Reason = (int)match.Reason,
                AriaPhase = (int)touch.Phase,
                Actions = touch.Actions,
                StopReason = (int)touch.StopReason,
                TargetId = observation.TargetId,
                ObservationKind = (int)observation.Kind
            });
            File.AppendAllText(Path.Combine(EvidenceDirectory, TelemetryFile), line + "\n");
        }

        private static void Finish(string reason)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(Key, false);
            string previous = SessionState.GetString("Warline.AriaPlayValidation", "");
            Environment.SetEnvironmentVariable("WARLINE_VALIDATION_SAVE_ROOT",
                string.IsNullOrEmpty(previous) ? null : previous);

            SkirmishMatchState match = default;
            bool haveMatch = false;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated &&
                SkirmishLaunchProjection.TryGet(world.EntityManager, out _, out match))
                haveMatch = true;

            string resultPath = Path.Combine(EvidenceDirectory, "aria-s3-ib-en-result.json");
            File.WriteAllText(resultPath, JsonUtility.ToJson(new ResultRow
            {
                Reason = reason,
                Victory = haveMatch && match.Outcome == SkirmishOutcome.Victory,
                Seed = haveMatch ? match.Seed : MapSeed,
                ScenarioIndex = haveMatch ? match.ScenarioIndex : 3,
                Phase = haveMatch ? (int)match.Phase : -1,
                Outcome = haveMatch ? (int)match.Outcome : -1,
                EndReason = haveMatch ? (int)match.Reason : -1,
                ElapsedSeconds = haveMatch ? match.ElapsedSeconds : 0f,
                PlayerUnitsLost = haveMatch ? match.PlayerUnitsLost : 0,
                EnemyUnitsLost = haveMatch ? match.EnemyUnitsLost : 0,
                PlayerBuildingsLost = haveMatch ? match.PlayerBuildingsLost : 0,
                EnemyBuildingsLost = haveMatch ? match.EnemyBuildingsLost : 0,
                ResultSaved = haveMatch ? match.ResultSaved : (byte)0,
                CompletedGestures = completedGestures,
                WallClockSeconds = matchStartedAt > 0
                    ? EditorApplication.timeSinceStartup - matchStartedAt
                    : 0d
            }, true));

            string marker = haveMatch && match.Outcome == SkirmishOutcome.Victory
                ? $"[SkirmishIndustrialBasinAria] result=Passed reason={reason} elapsed={match.ElapsedSeconds:F1} evidence={resultPath}"
                : $"[SkirmishIndustrialBasinAria] result=Failed reason={reason} evidence={resultPath}";
            if (haveMatch && match.Outcome == SkirmishOutcome.Victory)
                Debug.Log(marker);
            else
                Debug.LogError(marker);

            UiShellRuntimeGateway.StopAriaPlay();
            EditorApplication.ExitPlaymode();
        }

        [Serializable]
        private struct TelemetryRow
        {
            public float Elapsed;
            public int Phase;
            public int Outcome;
            public int Reason;
            public int AriaPhase;
            public int Actions;
            public int StopReason;
            public int TargetId;
            public int ObservationKind;
        }

        [Serializable]
        private struct ResultRow
        {
            public string Reason;
            public bool Victory;
            public int Seed;
            public int ScenarioIndex;
            public int Phase;
            public int Outcome;
            public int EndReason;
            public float ElapsedSeconds;
            public int PlayerUnitsLost;
            public int EnemyUnitsLost;
            public int PlayerBuildingsLost;
            public int EnemyBuildingsLost;
            public byte ResultSaved;
            public int CompletedGestures;
            public double WallClockSeconds;
        }
    }
}
#endif
