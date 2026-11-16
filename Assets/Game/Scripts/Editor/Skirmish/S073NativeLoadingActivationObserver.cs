#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Composition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Opt-in, passive observation of one real legacy S073 start through the native
    // Match start boundary and automatic gameplay loading gate. It never writes ECS.
    [InitializeOnLoad]
    internal static class S073NativeLoadingActivationObserver
    {
        private const string OutputKey = "WARLINE_S073_NATIVE_LOADING_ACTIVATION_OUTPUT";
        private const double TimeoutSeconds = 180d;
        private static readonly JArray Samples = new();
        private static bool done;
        private static World observedWorld;
        private static ulong observedWorldSequence;
        private static Entity observedSession;
        private static string observedSessionId;
        private static double observedAt;
        private static double playModeStartedAt = -1d;
        private static string lastSampleKey;

        static S073NativeLoadingActivationObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey)))
                EditorApplication.update += Observe;
        }

        private static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            if (playModeStartedAt < 0d) playModeStartedAt = EditorApplication.timeSinceStartup;
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                {
                    HandleMissingTarget("DefaultWorldUnavailable");
                    return;
                }
                EntityManager em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1)
                {
                    HandleMissingTarget("UniqueSkirmishSessionUnavailable");
                    return;
                }
                Entity session = matches.GetSingletonEntity();
                SkirmishMatchState match = em.GetComponentData<SkirmishMatchState>(session);
                if (match.ScenarioIndex != 3 || match.SessionId.IsEmpty)
                {
                    HandleMissingTarget("S073LegacySessionNotFound");
                    return;
                }
                if (em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                    !em.HasComponent<SkirmishLaunchSnapshot>(session))
                {
                    HandleMissingTarget("OriginalLegacyLaunchSnapshotUnavailable");
                    return;
                }
                QuickGameConfig queuedConfig = em.GetComponentObject<SkirmishLaunchSnapshot>(session).Configuration;
                if (queuedConfig.ScenarioIndex != 3 || queuedConfig.MapSeed != match.Seed)
                {
                    HandleMissingTarget("S073LegacySnapshotIdentityMismatch");
                    return;
                }

                if (observedSessionId == null)
                {
                    observedWorld = world;
                    observedWorldSequence = world.Unmanaged.SequenceNumber;
                    observedSession = session;
                    observedSessionId = match.SessionId.ToString();
                    observedAt = EditorApplication.timeSinceStartup;
                }
                else if (world != observedWorld || !world.IsCreated ||
                         world.Unmanaged.SequenceNumber != observedWorldSequence ||
                         session != observedSession || match.SessionId.ToString() != observedSessionId)
                {
                    Finish("Rejected", "WorldOrSessionChanged");
                    return;
                }

                JObject sample = ReadSample(em, session, in match);
                string key = SampleKey(sample);
                if (key != lastSampleKey)
                {
                    sample["editor_time_seconds"] = EditorApplication.timeSinceStartup;
                    sample["frame"] = Time.frameCount;
                    Samples.Add(sample);
                    lastSampleKey = key;
                }

                string failure = FailureReason(sample, in match);
                if (failure != null)
                {
                    Finish("Failed", failure);
                    return;
                }

                bool started = (bool?)sample["match_start_has_started"] == true &&
                    (int?)sample["match_start_owner_count"] == 1 &&
                    (int?)sample["match_scene_reference_count"] == 1 &&
                    (bool?)sample["match_scene_view_available"] == true &&
                    (bool?)sample["match_scene_loaded"] == true &&
                    (bool?)sample["gameplay_start_complete"] == true &&
                    (int?)sample["runtime_state_count"] == 1 &&
                    (int?)sample["play_requested"] == 1 &&
                    (int?)sample["simulation_active"] == 1;
                if (started)
                {
                    Finish("Observed", null);
                    return;
                }

                if (EditorApplication.timeSinceStartup - observedAt >= TimeoutSeconds)
                    Finish("TimedOut", "NativeStartOrLoadingActivationNotObservedWithin180Seconds");
            }
            catch (Exception error)
            {
                Finish("Failed", error.GetType().Name + ": " + error.Message);
            }
        }

        private static void HandleMissingTarget(string reason)
        {
            if (observedSessionId != null)
            {
                Finish("Rejected", reason);
                return;
            }
            if (playModeStartedAt >= 0d && EditorApplication.timeSinceStartup - playModeStartedAt >= TimeoutSeconds)
                Finish("NotObserved", reason + "Within180SecondsOfPlayMode");
        }

        private static JObject ReadSample(EntityManager em, Entity session, in SkirmishMatchState match)
        {
            JObject sample = new()
            {
                ["session_entity"] = new JArray(session.Index, session.Version),
                ["session_id"] = match.SessionId.ToString(),
                ["scenario_index"] = match.ScenarioIndex,
                ["seed"] = match.Seed,
                ["phase"] = match.Phase.ToString(),
                ["startup_failure"] = match.StartupFailure.ToString()
            };

            using (var starts = em.CreateEntityQuery(typeof(MatchStartStateComponent), typeof(MatchStartQueueComponent)))
            {
                int count = starts.CalculateEntityCount();
                sample["match_start_owner_count"] = count;
                if (count == 1)
                {
                    Entity owner = starts.GetSingletonEntity();
                    MatchStartQueueComponent queue = em.GetComponentData<MatchStartQueueComponent>(owner);
                    sample["match_start_request_id"] = queue.ActiveRequestId;
                    sample["match_start_is_pending"] = queue.IsStartPending;
                    sample["match_start_has_started"] = queue.HasStarted;
                    sample["match_start_status"] = queue.LastStatus.ToString();
                    if (em.HasComponent<MatchStartProgressComponent>(owner))
                    {
                        MatchStartProgressComponent progress = em.GetComponentData<MatchStartProgressComponent>(owner);
                        sample["match_start_progress"] = progress.Progress01;
                        sample["match_start_progress_status"] = progress.Status.ToString();
                    }
                    if (em.HasBuffer<MatchStartResultElement>(owner))
                    {
                        var results = em.GetBuffer<MatchStartResultElement>(owner, true);
                        var rows = new JArray();
                        bool activeRequestFailed = false;
                        for (int i = 0; i < results.Length; i++)
                        {
                            if (queue.ActiveRequestId != 0 && results[i].RequestId == queue.ActiveRequestId &&
                                results[i].Status == MatchStartStatusKind.Failed)
                                activeRequestFailed = true;
                            rows.Add(new JObject { ["request_id"] = results[i].RequestId,
                                ["status"] = results[i].Status.ToString(), ["message"] = results[i].Message.ToString() });
                        }
                        sample["match_start_results"] = rows;
                        sample["active_request_failed_result"] = activeRequestFailed;
                    }
                }
            }

            using (var references = em.CreateEntityQuery(typeof(MatchSceneReferenceComponent)))
            {
                int count = references.CalculateEntityCount();
                sample["match_scene_reference_count"] = count;
                MatchSceneView view = count == 1 ? references.GetSingleton<MatchSceneReferenceComponent>().View.Value : null;
                sample["match_scene_view_available"] = view != null;
                if (view != null)
                {
                    sample["match_scene_name"] = view.gameObject.scene.name;
                    sample["match_scene_loaded"] = view.gameObject.scene.IsValid() && view.gameObject.scene.isLoaded;
                    sample["gameplay_start_requested"] = view.GameplayStartRequested;
                    sample["gameplay_start_complete"] = view.GameplayStartComplete;
                    sample["gameplay_start_failed"] = view.GameplayStartFailed;
                    sample["gameplay_start_progress"] = view.GameplayStartProgress01;
                    sample["gameplay_start_status"] = view.GameplayStartStatus;
                    sample["gameplay_start_failure_message"] = view.GameplayStartFailureMessage;
                }
            }

            using (var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
            {
                int count = gameplay.CalculateEntityCount();
                sample["runtime_state_count"] = count;
                if (count == 1)
                {
                    RuntimeGameplayStateComponent state = gameplay.GetSingleton<RuntimeGameplayStateComponent>();
                    sample["play_requested"] = state.PlayRequested;
                    sample["simulation_active"] = state.SimulationActive;
                }
            }
            return sample;
        }

        private static string FailureReason(JObject sample, in SkirmishMatchState match)
        {
            if (match.StartupFailure != SkirmishStartupFailureCode.None)
                return "SkirmishStartupFailure:" + match.StartupFailure;
            if ((int?)sample["match_start_owner_count"] > 1 || (int?)sample["runtime_state_count"] > 1 ||
                (int?)sample["match_scene_reference_count"] > 1)
                return "NativeOwnerNotUnique";
            if ((bool?)sample["gameplay_start_failed"] == true)
                return "MatchSceneGameplayStartupFailed:" + (string)sample["gameplay_start_failure_message"];
            if ((string)sample["match_start_status"] == MatchStartStatusKind.Failed.ToString())
                return "NativeMatchStartStatusFailed";
            if ((bool?)sample["active_request_failed_result"] == true)
                return "NativeMatchStartActiveRequestResultFailed";
            return null;
        }

        private static string SampleKey(JObject sample)
        {
            // Progress percentages are included so a real loading interval is
            // preserved; frame/time are omitted to avoid duplicate idle samples.
            return string.Join("|", new[] { "phase", "startup_failure", "match_start_owner_count",
                "match_start_request_id", "match_start_is_pending", "match_start_has_started",
                "match_start_status", "match_start_progress", "match_start_progress_status", "match_start_results",
                "active_request_failed_result",
                "match_scene_reference_count", "match_scene_view_available", "match_scene_loaded",
                "gameplay_start_requested", "gameplay_start_complete", "gameplay_start_failed",
                "gameplay_start_progress", "gameplay_start_status", "runtime_state_count",
                "play_requested", "simulation_active" }.Select(k => k == "match_start_results"
                    ? (sample[k] as JArray)?.ToString(Formatting.None) ?? "null"
                    : sample[k]?.ToString() ?? "null"));
        }

        private static void Finish(string result, string reason)
        {
            if (done) return;
            done = true;
            EditorApplication.update -= Observe;
            try
            {
                string rawPath = Environment.GetEnvironmentVariable(OutputKey);
                if (string.IsNullOrWhiteSpace(rawPath)) return;
                string path = Path.GetFullPath(rawPath);
                string projectLogs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SkirmishProduction")) + Path.DirectorySeparatorChar;
                if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) &&
                    !path.StartsWith(projectLogs, StringComparison.Ordinal))
                    throw new InvalidOperationException("Private temporary or project SkirmishProduction output required");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                JObject output = new()
                {
                    ["kind"] = "S073NativeLoadingActivationObservation",
                    ["schema_version"] = 1,
                    ["scope"] = "SameNativeSessionMatchStartAndAutomaticLoadingGateTransition_NotNormalInputAttributionOrMissionReadiness",
                    ["result"] = result,
                    ["reason"] = reason,
                    ["unity_version"] = Application.unityVersion,
                    ["world_name"] = observedWorld != null ? observedWorld.Name : null,
                    ["world_sequence"] = observedWorldSequence.ToString(),
                    ["session_id"] = observedSessionId,
                    ["session_entity"] = observedSession == Entity.Null ? null : new JArray(observedSession.Index, observedSession.Version),
                    ["normal_input_invoked_by_observer"] = false,
                    ["launch_input_attribution"] = "NotObserved",
                    ["play_requested_semantics"] = "Native gameplay start transition sets PlayRequested; observer does not claim a post-load player Play click",
                    ["samples"] = Samples
                };
                using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                using var writer = new StreamWriter(file);
                writer.Write(output.ToString(Formatting.Indented) + "\n");
                Debug.Log("[S073NativeLoadingActivation] result=" + result + " path=" + path);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S073NativeLoadingActivation] result=Failed " + error.Message);
            }
        }
    }
}
#endif
