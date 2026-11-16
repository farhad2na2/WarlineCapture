#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    // Starts the normal S073 native placement probe and passively waits for
    // naturally scheduled production ticks. It never advances simulation.
    [InitializeOnLoad]
    internal static class S073NativeProductionDeliveryWindow
    {
        private const string WindowOutputKey = "WARLINE_S073_PRODUCTION_WINDOW_OUTPUT";
        private const string DeliveryOutputKey = "WARLINE_S073_PRODUCTION_DELIVERY_OUTPUT";
        private const string PredicateOutputKey = "WARLINE_S073_PREDICATE_OUTPUT";
        private const string MaskOutputKey = "WARLINE_S073_MASK_OUTPUT";
        private const string PlacementAuditOutputKey = "WARLINE_S073_PLACEMENT_AUDIT_OUTPUT";
        private const string PendingExitCodeKey = "WARLINE_S073_PRODUCTION_WINDOW_PENDING_EXIT_CODE";
        private const string ExitDeadlineKey = "WARLINE_S073_PRODUCTION_WINDOW_EXIT_DEADLINE";
        private const string ShutdownTimedOutKey = "WARLINE_S073_PRODUCTION_WINDOW_SHUTDOWN_TIMED_OUT";
        private const double PlayingWaitTimeoutSeconds = 180d;
        private const double ObservationWindowSeconds = 45d;
        private const double ExitTimeoutSeconds = 30d;
        private static double startedAt;
        private static double playingAt;
        private static double deadline;
        private static World capturedWorld;
        private static string capturedSession;
        private static int capturedFrame;
        private static SkirmishPhase lastMatchPhase;
        private static SkirmishOutcome lastMatchOutcome;
        private static SkirmishEndReason lastMatchEndReason;
        private static float lastMatchElapsedSeconds;
        private static double lastMatchObservedAt;
        private static int lastMatchObservedFrame;
        private static bool hasLastMatchSnapshot;
        private static JObject playerBaseAtPlaying;
        private static JObject enemyBaseAtPlaying;
        private static JObject playerBaseLatest;
        private static JObject enemyBaseLatest;
        private static JObject playerBaseLastExisting;
        private static JObject enemyBaseLastExisting;
        private static JObject sceneOperationMapAtPlaying;
        private static JObject sceneOperationMapLatest;
        private static bool completed;
        private static string windowObserverError;
        private static int pendingExitCode = -1;
        private static double exitDeadline;
        private static bool shutdownTimedOut;

        static S073NativeProductionDeliveryWindow()
        {
            pendingExitCode = SessionState.GetInt(PendingExitCodeKey, -1);
            shutdownTimedOut = SessionState.GetBool(ShutdownTimedOutKey, false);
            if (pendingExitCode >= 0)
            {
                exitDeadline = SessionState.GetFloat(ExitDeadlineKey,
                    (float)(EditorApplication.timeSinceStartup + ExitTimeoutSeconds));
                AttachShutdownWait();
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(WindowOutputKey)))
                EditorApplication.update += Observe;
        }

        // Use as the executeMethod entrypoint. The placement probe launches the
        // actual mission; this observer only watches its naturally running world.
        public static void Launch()
        {
            string windowPath = RequireFreshTemporaryOutput(WindowOutputKey);
            string deliveryPath = RequireFreshTemporaryOutput(DeliveryOutputKey);
            RequireFreshTemporaryOutput(PredicateOutputKey);
            RequireFreshTemporaryOutput(MaskOutputKey);
            RequireFreshTemporaryOutput(PlacementAuditOutputKey);
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_WATER_MASK_OUTPUT")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S073_NATIVE_STARTUP_WATER_MASK_OUTPUT")))
                throw new InvalidOperationException("Disable the water-mask observers for the production observation window; they exit immediately after capture.");
            if (!EditorApplication.isPlaying)
                SkirmishIndustrialBasinPlacementProbe.Launch();
            else
                throw new InvalidOperationException("Exit play mode before launching the standalone S073 production observation window.");
            Debug.Log("[S073NativeProductionWindow] started windowOutput=" + windowPath + " deliveryOutput=" + deliveryPath);
        }

        private static void Observe()
        {
            try { ObserveCore(); }
            catch (Exception error)
            {
                windowObserverError = error.GetType().Name + ":" + error.Message.Replace('\n', ' ').Replace('\r', ' ');
                Complete("WindowObserverError", 2);
            }
        }

        private static void ObserveCore()
        {
            if (completed) return;
            double now = EditorApplication.timeSinceStartup;
            if (startedAt == 0d)
            {
                startedAt = now;
                deadline = now + PlayingWaitTimeoutSeconds;
            }

            if (!EditorApplication.isPlaying)
            {
                if (playingAt > 0d) Complete("PlayModeExitedDuringObservation", 2);
                else if (now >= deadline) Complete("NoNaturalPlayingSession", 2);
                return;
            }

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            EntityManager em = world.EntityManager;
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() != 1) return;
            SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
            string session = match.SessionId.ToString();
            if (capturedWorld == null || ReferenceEquals(world, capturedWorld) && session == capturedSession)
            {
                lastMatchPhase = match.Phase;
                lastMatchOutcome = match.Outcome;
                lastMatchEndReason = match.Reason;
                lastMatchElapsedSeconds = match.ElapsedSeconds;
                lastMatchObservedAt = now;
                lastMatchObservedFrame = Time.frameCount;
                hasLastMatchSnapshot = true;
                sceneOperationMapLatest = CaptureSceneOperationMap(em, world, session);
                playerBaseLatest = CaptureMainBase(em, match.PlayerMainBase, 1);
                enemyBaseLatest = CaptureMainBase(em, match.EnemyMainBase, 2);
                if ((bool?)playerBaseLatest["entity_exists"] == true) playerBaseLastExisting = (JObject)playerBaseLatest.DeepClone();
                if ((bool?)enemyBaseLatest["entity_exists"] == true) enemyBaseLastExisting = (JObject)enemyBaseLatest.DeepClone();
                playerBaseLatest["snapshot_editor_seconds_from_playing"] = playingAt > 0d
                    ? now - playingAt : (double?)null;
                enemyBaseLatest["snapshot_editor_seconds_from_playing"] = playingAt > 0d
                    ? now - playingAt : (double?)null;
                playerBaseLatest["match_elapsed_seconds"] = match.ElapsedSeconds;
                enemyBaseLatest["match_elapsed_seconds"] = match.ElapsedSeconds;
                playerBaseLatest["snapshot_frame"] = Time.frameCount;
                enemyBaseLatest["snapshot_frame"] = Time.frameCount;
                UpdateBaseHealthDelta(playerBaseAtPlaying, playerBaseLatest);
                UpdateBaseHealthDelta(enemyBaseAtPlaying, enemyBaseLatest);
            }
            if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Phase != SkirmishPhase.Playing)
            {
                if (playingAt > 0d)
                    Complete(now >= deadline ? "ObservationWindowElapsed" : "NaturalPlayingSessionEndedBeforeWindow", now >= deadline ? 0 : 2);
                else if (now >= deadline)
                    Complete("NoNaturalPlayingSession", 2);
                return;
            }

            if (playingAt == 0d)
            {
                capturedWorld = world;
                capturedSession = session;
                capturedFrame = Time.frameCount;
                playingAt = now;
                deadline = now + ObservationWindowSeconds;
                sceneOperationMapAtPlaying = sceneOperationMapLatest != null
                    ? (JObject)sceneOperationMapLatest.DeepClone()
                    : CaptureSceneOperationMap(em, world, session);
                playerBaseAtPlaying = CaptureMainBase(em, match.PlayerMainBase, 1);
                enemyBaseAtPlaying = CaptureMainBase(em, match.EnemyMainBase, 2);
                playerBaseAtPlaying["snapshot_editor_seconds_from_playing"] = 0d;
                enemyBaseAtPlaying["snapshot_editor_seconds_from_playing"] = 0d;
                playerBaseAtPlaying["match_elapsed_seconds"] = match.ElapsedSeconds;
                enemyBaseAtPlaying["match_elapsed_seconds"] = match.ElapsedSeconds;
                playerBaseAtPlaying["snapshot_frame"] = Time.frameCount;
                enemyBaseAtPlaying["snapshot_frame"] = Time.frameCount;
                Debug.Log("[S073NativeProductionWindow] phase=Playing session=" + session + " frame=" + capturedFrame);
                return;
            }
            if (!ReferenceEquals(world, capturedWorld) || session != capturedSession)
            {
                Complete("WorldOrSessionChanged", 2);
                return;
            }
            if (now >= deadline)
                Complete("ObservationWindowElapsed", 0);
        }

        private static void Complete(string reason, int exitCode)
        {
            if (completed) return;
            completed = true;
            EditorApplication.update -= Observe;
            try
            {
                JObject delivery = InspectDeliveryOutput(capturedSession);
                double endedAt = EditorApplication.timeSinceStartup;
                var result = new JObject
                {
                    ["kind"] = "S073NativeProductionDeliveryWindow",
                    ["schema_version"] = 3,
                    ["scope"] = "PassiveNaturalSessionObservation_NoSimulationAdvance_NoReadinessClaim",
                    ["result"] = reason,
                    ["mission"] = "S073",
                    ["scenario_index"] = 3,
                    ["seed"] = 104729,
                    ["session_id"] = capturedSession,
                    ["world_name"] = capturedWorld != null ? capturedWorld.Name : null,
                    ["started_frame"] = capturedFrame,
                    ["window_seconds"] = playingAt > 0d ? endedAt - playingAt : 0d,
                    ["terminal_match_snapshot_available"] = hasLastMatchSnapshot,
                    ["terminal_match_phase"] = hasLastMatchSnapshot ? lastMatchPhase.ToString() : null,
                    ["terminal_match_outcome"] = hasLastMatchSnapshot ? lastMatchOutcome.ToString() : null,
                    ["terminal_match_end_reason"] = hasLastMatchSnapshot ? lastMatchEndReason.ToString() : null,
                    ["terminal_match_elapsed_seconds"] = hasLastMatchSnapshot ? lastMatchElapsedSeconds : (float?)null,
                    ["terminal_match_snapshot_editor_seconds_from_playing"] = hasLastMatchSnapshot && playingAt > 0d
                        ? lastMatchObservedAt - playingAt : (double?)null,
                    ["terminal_match_snapshot_frame"] = hasLastMatchSnapshot ? lastMatchObservedFrame : (int?)null,
                    ["terminal_match_snapshot_is_finished"] = hasLastMatchSnapshot && lastMatchPhase == SkirmishPhase.Finished,
                    ["main_bases_at_playing"] = new JObject
                    {
                        ["player"] = playerBaseAtPlaying,
                        ["enemy"] = enemyBaseAtPlaying
                    },
                    ["main_bases_at_terminal_observation"] = new JObject
                    {
                        ["player"] = playerBaseLatest,
                        ["player_last_existing"] = playerBaseLastExisting,
                        ["enemy"] = enemyBaseLatest,
                        ["enemy_last_existing"] = enemyBaseLastExisting
                    },
                    ["scene_operation_map_at_playing"] = sceneOperationMapAtPlaying,
                    ["scene_operation_map_latest_same_world_session"] = sceneOperationMapLatest,
                    ["production_delivery_observer"] = delivery,
                    ["observer_error_count"] = S073NativeProductionDeliveryObserver.ObserverErrorCount,
                    ["last_observer_error"] = S073NativeProductionDeliveryObserver.LastObserverError,
                    ["window_observer_error"] = windowObserverError,
                    ["mission_ready"] = false,
                    ["source_fit_certified"] = false,
                    ["normal_input_playthrough"] = false,
                    ["player_readiness"] = false
                };
                WriteFreshTemporaryOutput(WindowOutputKey, result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
                Debug.Log("[S073NativeProductionWindow] result=" + reason + " observer_status=" + delivery["status"]);
                BeginShutdown(exitCode);
            }
            catch (Exception error)
            {
                windowObserverError = error.GetType().Name + ":" + error.Message.Replace('\n', ' ').Replace('\r', ' ');
                Debug.LogError("[S073NativeProductionWindow] result=Failed " + error);
                try
                {
                    var failure = new JObject
                    {
                        ["kind"] = "S073NativeProductionDeliveryWindow",
                        ["schema_version"] = 1,
                        ["result"] = "Failed",
                        ["reason"] = windowObserverError,
                        ["session_id"] = capturedSession,
                        ["observer_error_count"] = S073NativeProductionDeliveryObserver.ObserverErrorCount,
                        ["last_observer_error"] = S073NativeProductionDeliveryObserver.LastObserverError,
                        ["mission_ready"] = false,
                        ["player_readiness"] = false
                    };
                    WriteFreshTemporaryOutput(WindowOutputKey, failure.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
                }
                catch { }
                BeginShutdown(1);
            }
        }

        private static void BeginShutdown(int exitCode)
        {
            completed = true;
            EditorApplication.update -= Observe;
            pendingExitCode = exitCode;
            shutdownTimedOut = false;
            exitDeadline = EditorApplication.timeSinceStartup + ExitTimeoutSeconds;
            SessionState.SetInt(PendingExitCodeKey, exitCode);
            SessionState.SetFloat(ExitDeadlineKey, (float)exitDeadline);
            SessionState.SetBool(ShutdownTimedOutKey, false);
            AttachShutdownWait();

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;
            else
                ScheduleExitAfterEditMode();
        }

        private static void AttachShutdownWait()
        {
            EditorApplication.update -= WaitForEditModeBeforeExit;
            EditorApplication.update += WaitForEditModeBeforeExit;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (pendingExitCode < 0) return;
            if (state == PlayModeStateChange.EnteredEditMode) ScheduleExitAfterEditMode();
        }

        private static void WaitForEditModeBeforeExit()
        {
            if (pendingExitCode < 0) return;
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ScheduleExitAfterEditMode();
                return;
            }

            if (EditorApplication.timeSinceStartup < exitDeadline) return;
            EditorApplication.update -= WaitForEditModeBeforeExit;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Debug.LogError("[S073NativeProductionWindow] shutdown result=TimedOut waiting for Edit Mode; requesting bounded exit");
            pendingExitCode = 1;
            shutdownTimedOut = true;
            SessionState.SetInt(PendingExitCodeKey, pendingExitCode);
            SessionState.SetBool(ShutdownTimedOutKey, true);
            EditorApplication.delayCall += ExitAfterEditModeWait;
        }

        private static void ScheduleExitAfterEditMode()
        {
            if (pendingExitCode < 0) return;
            EditorApplication.update -= WaitForEditModeBeforeExit;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.delayCall -= ExitAfterEditModeWait;
            EditorApplication.delayCall += ExitAfterEditModeWait;
        }

        private static void ExitAfterEditModeWait()
        {
            if (pendingExitCode < 0) return;
            bool editModeConfirmed = !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
            int exitCode = pendingExitCode;
            if (shutdownTimedOut || !editModeConfirmed)
            {
                Debug.LogError("[S073NativeProductionWindow] shutdown result=TimedOutExit edit_mode_confirmed=" + editModeConfirmed);
                exitCode = 1;
            }
            else
                Debug.Log("[S073NativeProductionWindow] shutdown result=EditModeConfirmed exit_code=" + exitCode);
            pendingExitCode = -1;
            shutdownTimedOut = false;
            SessionState.EraseInt(PendingExitCodeKey);
            SessionState.EraseFloat(ExitDeadlineKey);
            SessionState.EraseBool(ShutdownTimedOutKey);
            EditorApplication.Exit(exitCode);
        }

        private static JObject CaptureSceneOperationMap(EntityManager em, World world, string session)
        {
            try
            {
                var scenes = new JArray();
                Scene activeScene = SceneManager.GetActiveScene();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    scenes.Add(new JObject
                    {
                        ["sequence_index"] = i,
                        ["name"] = scene.name,
                        ["path"] = scene.path,
                        ["handle"] = scene.handle.ToString(),
                        ["is_valid"] = scene.IsValid(),
                        ["is_loaded"] = scene.isLoaded,
                        ["is_active"] = scene.IsValid() && scene.handle == activeScene.handle
                    });
                }

                using var roots = em.CreateEntityQuery(typeof(OperationMapRootComponent));
                using var activeMaps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent));
                using var metadata = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                using var rootEntities = roots.ToEntityArray(Allocator.Temp);
                using var activeMapEntities = activeMaps.ToEntityArray(Allocator.Temp);
                using var metadataEntities = metadata.ToEntityArray(Allocator.Temp);
                var rootIds = EntityIds(rootEntities);
                var activeMapRows = new JArray();
                for (int i = 0; i < activeMapEntities.Length; i++)
                {
                    Entity entity = activeMapEntities[i];
                    ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(entity);
                    activeMapRows.Add(new JObject
                    {
                        ["entity_index"] = entity.Index,
                        ["entity_version"] = entity.Version,
                        ["operation_map_id"] = active.OperationMapId.ToString(),
                        ["scenario_id"] = active.ScenarioId.ToString(),
                        ["mission_id"] = active.MissionId.ToString(),
                        ["generation"] = active.Generation,
                        ["schema_version"] = active.SchemaVersion,
                        ["content_version"] = active.ContentVersion
                    });
                }
                var metadataRows = new JArray();
                for (int i = 0; i < metadataEntities.Length; i++)
                {
                    Entity entity = metadataEntities[i];
                    OperationMapMetadataComponent data = em.GetComponentData<OperationMapMetadataComponent>(entity);
                    bool blobCreated = data.Blob.IsCreated;
                    metadataRows.Add(new JObject
                    {
                        ["entity_index"] = entity.Index,
                        ["entity_version"] = entity.Version,
                        ["metadata_hash"] = data.MetadataHash.ToString(),
                        ["generation"] = data.Generation,
                        ["physical_source_validated"] = data.PhysicalSourceValidated != 0,
                        ["physical_source_validated_raw"] = data.PhysicalSourceValidated,
                        ["blob_created"] = blobCreated,
                        ["blob_operation_map_id"] = blobCreated ? data.Blob.Value.OperationMapId.ToString() : null
                    });
                }

                Entity uniqueRoot = rootEntities.Length == 1 ? rootEntities[0] : Entity.Null;
                Entity uniqueActive = activeMapEntities.Length == 1 ? activeMapEntities[0] : Entity.Null;
                Entity uniqueMetadata = metadataEntities.Length == 1 ? metadataEntities[0] : Entity.Null;
                ActiveOperationMapComponent uniqueActiveData = uniqueActive != Entity.Null
                    ? em.GetComponentData<ActiveOperationMapComponent>(uniqueActive) : default;
                OperationMapMetadataComponent uniqueMetadataData = uniqueMetadata != Entity.Null
                    ? em.GetComponentData<OperationMapMetadataComponent>(uniqueMetadata) : default;
                bool uniqueMapIdentityAvailable = rootEntities.Length == 1 && activeMapEntities.Length == 1 &&
                    metadataEntities.Length == 1 && uniqueRoot == uniqueActive && uniqueRoot == uniqueMetadata &&
                    uniqueMetadataData.Blob.IsCreated && uniqueMetadataData.Generation == uniqueActiveData.Generation &&
                    uniqueMetadataData.Blob.Value.OperationMapId.Equals(uniqueActiveData.OperationMapId);
                var result = new JObject
                {
                    ["capture_status"] = "Observed",
                    ["world_name"] = world != null ? world.Name : null,
                    ["world_sequence"] = world != null ? world.Unmanaged.SequenceNumber : (ulong?)null,
                    ["session_id"] = session,
                    ["frame"] = Time.frameCount,
                    ["scene_sequence_count"] = scenes.Count,
                    ["active_scene_name"] = activeScene.IsValid() ? activeScene.name : null,
                    ["active_scene_path"] = activeScene.IsValid() ? activeScene.path : null,
                    ["active_scene_handle"] = activeScene.IsValid() ? activeScene.handle.ToString() : null,
                    ["loaded_scene_sequence"] = scenes,
                    ["operation_map_root_query_cardinality"] = rootEntities.Length,
                    ["operation_map_root_entity_ids"] = rootIds,
                    ["active_operation_map_query_cardinality"] = activeMapEntities.Length,
                    ["active_operation_map_rows"] = activeMapRows,
                    ["operation_map_metadata_query_cardinality"] = metadataEntities.Length,
                    ["operation_map_metadata_rows"] = metadataRows,
                    ["unique_consistent_operation_map_identity_available"] = uniqueMapIdentityAvailable,
                    ["unique_operation_map_id"] = uniqueMapIdentityAvailable ? uniqueActiveData.OperationMapId.ToString() : null,
                    ["unique_scenario_id"] = uniqueMapIdentityAvailable ? uniqueActiveData.ScenarioId.ToString() : null,
                    ["unique_mission_id"] = uniqueMapIdentityAvailable ? uniqueActiveData.MissionId.ToString() : null,
                    ["unique_operation_map_generation"] = uniqueMapIdentityAvailable ? uniqueActiveData.Generation : (int?)null,
                    ["unique_metadata_hash"] = uniqueMapIdentityAvailable ? uniqueMetadataData.MetadataHash.ToString() : null,
                    ["unique_metadata_generation"] = uniqueMapIdentityAvailable ? uniqueMetadataData.Generation : (int?)null,
                    ["unique_physical_source_validated"] = uniqueMapIdentityAvailable
                        ? uniqueMetadataData.PhysicalSourceValidated != 0 : (bool?)null,
                    ["unique_identity_matches_metadata"] = uniqueMapIdentityAvailable
                };
                return result;
            }
            catch (Exception error)
            {
                return new JObject
                {
                    ["capture_status"] = "Unavailable",
                    ["capture_error"] = error.GetType().Name + ":" + error.Message.Replace('\n', ' ').Replace('\r', ' '),
                    ["unique_consistent_operation_map_identity_available"] = false,
                    ["loaded_scene_sequence"] = new JArray()
                };
            }
        }

        private static JArray EntityIds(NativeArray<Entity> entities)
        {
            var ids = new JArray();
            for (int i = 0; i < entities.Length; i++)
                ids.Add(new JObject
                {
                    ["entity_index"] = entities[i].Index,
                    ["entity_version"] = entities[i].Version
                });
            return ids;
        }

        private static JObject CaptureMainBase(EntityManager em, Entity entity, byte expectedFaction)
        {
            bool exists = entity != Entity.Null && em.Exists(entity);
            bool hasHealth = exists && em.HasComponent<UnitHealth>(entity);
            bool hasTransform = exists && em.HasComponent<LocalTransform>(entity);
            bool hasFaction = exists && em.HasComponent<Faction>(entity);
            bool hasSourceKey = exists && em.HasComponent<UnitSourcePrefabKey>(entity);
            var result = new JObject
            {
                ["expected_faction_id"] = expectedFaction,
                ["entity_index"] = entity == Entity.Null ? -1 : entity.Index,
                ["entity_version"] = entity == Entity.Null ? -1 : entity.Version,
                ["entity_exists"] = exists,
                ["faction_id"] = hasFaction ? em.GetComponentData<Faction>(entity).Id : (byte?)null,
                ["faction_matches_expected"] = hasFaction
                    ? em.GetComponentData<Faction>(entity).Id == expectedFaction : (bool?)null,
                ["source_prefab_key"] = hasSourceKey
                    ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : null,
                ["health_current"] = hasHealth ? em.GetComponentData<UnitHealth>(entity).Current : (int?)null,
                ["health_max"] = hasHealth ? em.GetComponentData<UnitHealth>(entity).Max : (int?)null,
                ["alive_by_original_match_rule"] = hasHealth
                    ? em.GetComponentData<UnitHealth>(entity).Current > 0 : (bool?)null,
                ["has_transform"] = hasTransform
            };
            if (hasTransform)
            {
                var position = em.GetComponentData<LocalTransform>(entity).Position;
                result["transform_position"] = new JArray(position.x, position.y, position.z);
            }
            else result["transform_position"] = null;
            return result;
        }

        private static void UpdateBaseHealthDelta(JObject baseline, JObject latest)
        {
            bool sameIdentity = baseline != null && latest != null &&
                (int?)baseline["entity_index"] == (int?)latest["entity_index"] &&
                (int?)baseline["entity_version"] == (int?)latest["entity_version"];
            latest["entity_identity_matches_playing"] = sameIdentity;
            latest["entity_removed_since_playing"] = (bool?)baseline?["entity_exists"] == true &&
                (bool?)latest?["entity_exists"] == false && sameIdentity;
            int? initialHealth = baseline?["health_current"]?.Value<int?>();
            int? latestHealth = latest?["health_current"]?.Value<int?>();
            latest["health_delta_from_playing"] = sameIdentity && initialHealth.HasValue && latestHealth.HasValue
                ? latestHealth.Value - initialHealth.Value : (int?)null;
            latest["net_health_lost_from_playing"] = sameIdentity && initialHealth.HasValue && latestHealth.HasValue
                ? Math.Max(0, initialHealth.Value - latestHealth.Value) : (int?)null;
        }

        private static JObject InspectDeliveryOutput(string expectedSession)
        {
            string configured = Environment.GetEnvironmentVariable(DeliveryOutputKey);
            if (string.IsNullOrEmpty(configured))
                return new JObject { ["status"] = "ObserverOutputPathNotConfigured", ["event_claim"] = "None" };
            string path = Path.GetFullPath(configured);
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal))
                return new JObject { ["status"] = "ObserverOutputPathOutsidePrivateTmp", ["path"] = path, ["event_claim"] = "None" };
            if (!File.Exists(path))
                return new JObject
                {
                    ["status"] = "ObserverOutputMissing_HookNotObservedOrOptInUnavailable",
                    ["path"] = path,
                    ["event_claim"] = "None_NoProductionConclusion"
                };

            int rows = 0, sameSessionRows = 0, otherSessionRows = 0;
            int noEventTicks = 0, productionEvents = 0, staticBoundsEvents = 0, malformedRows = 0;
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    JObject row = JObject.Parse(line);
                    rows++;
                    if (!string.Equals((string)row["session_id"], expectedSession, StringComparison.Ordinal))
                    {
                        otherSessionRows++;
                        continue;
                    }
                    sameSessionRows++;
                    if ((bool?)row["production_event_present"] == true) productionEvents++;
                    if ((string)row["production_event_status"] == "NoPendingProductionOrActiveTransportOnThisNaturalTick") noEventTicks++;
                    if ((bool?)row["static_render_bounds_observed"] == true) staticBoundsEvents++;
                }
                catch { malformedRows++; }
            }
            string status = productionEvents > 0 ? "NaturalProductionStateObserved" :
                staticBoundsEvents > 0 ? "NaturalStaticBoundsReservationObserved" :
                noEventTicks > 0 ? "NaturalTickObserved_NoProductionEventInThatTick" :
                rows > 0 ? "ObserverRowsPresent_NoRecognizedProductionEvent" : "ObserverFileEmpty";
            return new JObject
            {
                ["status"] = status,
                ["path"] = path,
                ["rows"] = rows,
                ["same_session_rows"] = sameSessionRows,
                ["other_session_rows"] = otherSessionRows,
                ["no_event_tick_rows"] = noEventTicks,
                ["production_event_rows"] = productionEvents,
                ["static_bounds_event_rows"] = staticBoundsEvents,
                ["static_bounds_observation_count"] = S073NativeProductionDeliveryObserver.StaticBoundsObservationCountFor(expectedSession),
                ["static_bounds_skipped_overflow_count"] = S073NativeProductionDeliveryObserver.StaticBoundsSkippedCountFor(expectedSession),
                ["malformed_rows"] = malformedRows,
                ["observer_error_count"] = S073NativeProductionDeliveryObserver.ObserverErrorCount,
                ["last_observer_error"] = S073NativeProductionDeliveryObserver.LastObserverError,
                ["event_claim"] = productionEvents > 0 || staticBoundsEvents > 0 ? "Observed" : "None"
            };
        }

        private static string RequireFreshTemporaryOutput(string key)
        {
            string configured = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(configured)) throw new InvalidOperationException(key + " must be configured.");
            string path = Path.GetFullPath(configured);
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(path))
                throw new InvalidOperationException("Fresh /private/tmp output required for " + key + ": " + path);
            return path;
        }

        private static void WriteFreshTemporaryOutput(string key, string contents)
        {
            string path = RequireFreshTemporaryOutput(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file, new UTF8Encoding(false))) writer.Write(contents);
        }
    }
}
#endif
