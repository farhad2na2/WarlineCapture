#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Explicit opt-in diagnostic. It injects one labeled target-path command through
    // the production request owner in the original live S073 world; it does not
    // change gameplay state or represent normal player input.
    [InitializeOnLoad]
    internal static class S073NativePathfindingCommandDiagnostic
    {
        const string OutputKey = "WARLINE_S073_NATIVE_PATHFINDING_COMMAND_OUTPUT";
        const string RawInputOutputDirectoryKey = "WARLINE_S073_NATIVE_PATH_INPUT_RAW_DIR";
        const string PendingExitCodeKey = "WARLINE_S073_NATIVE_PATHFINDING_PENDING_EXIT_CODE";
        const string ExitDeadlineKey = "WARLINE_S073_NATIVE_PATHFINDING_EXIT_DEADLINE";
        const string CompletedKey = "WARLINE_S073_NATIVE_PATHFINDING_COMPLETED";
        const string BootstrapStartedKey = "WARLINE_S073_NATIVE_PATHFINDING_BOOTSTRAP_STARTED";
        const string StartedAtKey = "WARLINE_S073_NATIVE_PATHFINDING_STARTED_AT";
        const string CommandStateKey = "WARLINE_S073_NATIVE_PATHFINDING_COMMAND_STATE";
        const string StartPinsKey = "WARLINE_S073_NATIVE_PATHFINDING_START_PINS";
        const string WorldSequenceKey = "WARLINE_S073_NATIVE_PATHFINDING_WORLD_SEQUENCE";
        const string HasWorldSequenceKey = "WARLINE_S073_NATIVE_PATHFINDING_HAS_WORLD_SEQUENCE";
        const string MatchIdentityKey = "WARLINE_S073_NATIVE_PATHFINDING_MATCH_IDENTITY";
        const double TimeoutSeconds = 90;
        const double BootstrapTimeoutSeconds = 180;
        const double ExitTimeoutSeconds = 30;
        static double bootstrapStartedAt;
        static int pendingExitCode = -1;
        static double exitDeadline;
        static double startedAt = -1;
        static ulong targetWorldSequence;
        static bool hasTargetWorldSequence;
        static string matchSessionId;
        static Entity matchEntity;
        static bool hasMatchIdentity;
        static World targetWorld;
        static Entity targetUnit;
        static int requestId;
        static int2 goal;
        static byte simulationActiveAtStart;
        static bool started;
        static bool finished;
        static string admission = "not_observed";
        static int admissionFrame = -1;
        static readonly JArray admissionSamples = new JArray();
        static JObject startPins;
        static JObject preEnqueueObservation;
        static readonly string[] Pins = {
            "Assets/Game/Resources/SkirmishIndustrialBasin.asset",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
            "Assets/Game/Scripts/Editor/Skirmish/S073NativeAIStartupSnapshot.cs",
            "Assets/Game/Scripts/Editor/Skirmish/S073NativePathfindingCommandDiagnostic.cs",
            "Assets/Game/Scripts/Systems/UnitMoveOrderRequestSystem.cs",
            "Assets/Game/Scripts/Systems/UnitMoveOrderSystem.cs",
            "Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs",
            "Assets/Game/Scripts/Systems/Pathfinding/PathJobSchedulingKernel.cs",
            "Assets/Game/Scripts/Systems/UnitPathResultApply.cs",
            "Assets/Game/Scripts/Systems/UnitPathfindingApply.cs",
            "Assets/Game/Scripts/Systems/PathPoolMaintenanceSystem.cs",
            "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs",
            "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs",
            "Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs",
            "Assets/Game/Scripts/Components/UnitCombatComponents.cs",
            "Assets/Game/Scripts/Editor/Skirmish/S002RuntimeBlockerWitness.cs",
            "Assets/Game/Scripts/Components/GridComponents.cs",
            "Tools/CI/invoke_unity_macos.sh"
        };

        static S073NativePathfindingCommandDiagnostic()
        {
            int pending = SessionState.GetInt(PendingExitCodeKey, -1);
            if (pending >= 0)
            {
                pendingExitCode = pending;
                exitDeadline = SessionState.GetFloat(ExitDeadlineKey, 0);
                AttachShutdownWait();
                if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) ScheduleExitAfterEditMode();
                return;
            }
            if (SessionState.GetBool(CompletedKey, false)) return;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OutputKey))) return;
            bootstrapStartedAt = SessionState.GetFloat(BootstrapStartedKey, -1);
            if (bootstrapStartedAt < 0)
            {
                bootstrapStartedAt = EditorApplication.timeSinceStartup;
                SessionState.SetFloat(BootstrapStartedKey, (float)bootstrapStartedAt);
            }
            startedAt = SessionState.GetFloat(StartedAtKey, -1);
            hasTargetWorldSequence = SessionState.GetBool(HasWorldSequenceKey, false);
            if (hasTargetWorldSequence && !ulong.TryParse(SessionState.GetString(WorldSequenceKey, string.Empty), out targetWorldSequence))
                throw new InvalidOperationException("Persisted S073 World sequence is malformed");
            string matchIdentity = SessionState.GetString(MatchIdentityKey, string.Empty);
            if (!string.IsNullOrEmpty(matchIdentity)) RestoreMatchIdentity(matchIdentity);
            string serializedPins = SessionState.GetString(StartPinsKey, string.Empty);
            if (!string.IsNullOrEmpty(serializedPins)) startPins = JObject.Parse(serializedPins);
            string commandState = SessionState.GetString(CommandStateKey, string.Empty);
            if (!string.IsNullOrEmpty(commandState)) RestoreCommandState(commandState);
            SessionState.SetBool(CompletedKey, false);
            EditorApplication.update += Observe;
            UnitPathfindingSystem.AdmissionObserved += OnAdmission;
        }

        static void OnAdmission(int frame, Game.Runtime.Pathfinding.PathJobAdmission value)
        {
            if (targetWorld == null || !targetWorld.IsCreated || targetUnit == Entity.Null) return;
            try
            {
                var em = targetWorld.EntityManager;
                if (!em.Exists(targetUnit) || !em.HasComponent<UnitPathRequest>(targetUnit)) return;
                admission = value.ToString();
                admissionFrame = frame;
                admissionSamples.Add(new JObject { ["frame"] = frame, ["decision"] = admission,
                    ["diagnostic_unit_has_path_request"] = true, ["target_world_sequence_observed"] = targetWorld.SequenceNumber.ToString() });
            }
            catch { /* A teardown race must not change the production system update. */ }
        }

        static void Observe()
        {
            if (finished) return;
            try
            {
                if (startedAt >= 0 && EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
                    throw new TimeoutException("No completed original S073 pathfinding result before the diagnostic timeout");
                if (startedAt < 0 && EditorApplication.timeSinceStartup - bootstrapStartedAt > BootstrapTimeoutSeconds)
                    throw new TimeoutException("Original S073 playing world was not found before the bootstrap timeout");
                if (!EditorApplication.isPlaying) return;
                if (targetWorld == null)
                {
                    targetWorld = FindOriginalWorld();
                    if (targetWorld == null) return;
                    startedAt = EditorApplication.timeSinceStartup;
                    SessionState.SetFloat(StartedAtKey, (float)startedAt);
                    if (!hasTargetWorldSequence)
                    {
                        targetWorldSequence = targetWorld.SequenceNumber;
                        hasTargetWorldSequence = true;
                        SessionState.SetString(WorldSequenceKey, targetWorldSequence.ToString());
                        SessionState.SetBool(HasWorldSequenceKey, true);
                    }
                }
                var em = targetWorld.EntityManager;
                if (hasTargetWorldSequence && targetWorld.SequenceNumber != targetWorldSequence)
                    throw new InvalidOperationException("Original S073 World changed during diagnostic");
                if (!VerifyScenario(em)) return;
                if (startPins == null)
                {
                    startPins = HashPins();
                    SessionState.SetString(StartPinsKey, startPins.ToString(Formatting.None));
                }
                if (!started)
                {
                    if (!TryChooseCommand(em, out targetUnit, out goal)) return;
                    using var stateQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
                    if (stateQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique live gameplay state required");
                    var gameplay = stateQuery.GetSingleton<RuntimeGameplayStateComponent>();
                    if (gameplay.PlayRequested == 0) throw new InvalidOperationException("Original gameplay-start owner has not requested Play; diagnostic will not activate it");
                    simulationActiveAtStart = gameplay.SimulationActive;
            preEnqueueObservation = CaptureInputObservation(em, "pre_enqueue_input_observation", startPins);
                    requestId = UnitMoveOrderRequestSystem.EnqueueTargetPathMoveOrder(em, targetUnit, goal);
                    started = true;
                    SaveCommandState();
                    Debug.Log("[S073NativePathfindingCommandDiagnostic] command=enqueued kind=explicit_synthetic_target_path requestId=" + requestId + " SimulationActive=" + simulationActiveAtStart);
                    return;
                }

                if (!UnitMoveOrderRequestSystem.TryGetResult(em, requestId, out UnitMoveOrderResultElement receipt)) return;
                if (receipt.Issued == 0) throw new InvalidOperationException("Production move-order owner rejected the diagnostic target-path request; reason=" + receipt.RejectionReasonCode);
                if (!em.Exists(targetUnit)) throw new InvalidOperationException("Commanded S073 unit disappeared before path result application");
                if (!em.HasComponent<UnitPathFollow>(targetUnit) || !em.HasComponent<UnitPathRange>(targetUnit)) return;
                using var stateAfterQuery = em.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
                if (stateAfterQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique live gameplay state required after path application");
                var stateAfter = stateAfterQuery.GetSingleton<RuntimeGameplayStateComponent>();
                if (stateAfter.SimulationActive != simulationActiveAtStart)
                    throw new InvalidOperationException("SimulationActive changed during diagnostic; refusing to report a controlled-state result");
                WriteResult(em, receipt, stateAfter);
            }
            catch (Exception ex) { FinishFailure(ex); }
        }

        static World FindOriginalWorld()
        {
            var views = UnityEngine.Object.FindObjectsOfType<MatchSceneView>();
            foreach (var view in views)
            {
                var bootstrap = FieldValue(view, "matchBootstrapSystem");
                var candidate = FieldValue(bootstrap, "runtimeWorld") as World;
                if (candidate == null || !candidate.IsCreated) continue;
                var em = candidate.EntityManager;
                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                if (maps.CalculateEntityCount() != 1) continue;
                var active = maps.GetSingleton<ActiveOperationMapComponent>();
                var map = maps.GetSingleton<OperationMapMetadataComponent>();
                if (active.OperationMapId.ToString() == "opmap.skirmish.industrial_basin" && map.Blob.IsCreated &&
                    map.Blob.Value.SourceOperationMapId.ToString() == "opmap.skirmish.desert_base_01") return candidate;
            }
            return null;
        }

        static bool VerifyScenario(EntityManager em)
        {
            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            if (maps.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique S073 map identity required");
            var active = maps.GetSingleton<ActiveOperationMapComponent>();
            var map = maps.GetSingleton<OperationMapMetadataComponent>();
            if (!map.Blob.IsCreated || active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                map.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
                throw new InvalidOperationException("Exact retained S073 source-created map identity required");
            using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matches.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique original S073 match state required");
            Entity currentMatchEntity = matches.GetSingletonEntity();
            var match = matches.GetSingleton<SkirmishMatchState>();
            if (match.ScenarioIndex != 3 || match.Seed != 104729)
                throw new InvalidOperationException("Exact S073 scenario and seed required");
            string currentSessionId = match.SessionId.ToString();
            if (string.IsNullOrWhiteSpace(currentSessionId)) throw new InvalidOperationException("Nonempty original S073 session identity required");
            if (!hasMatchIdentity)
            {
                matchSessionId = currentSessionId;
                matchEntity = currentMatchEntity;
                hasMatchIdentity = true;
                SaveMatchIdentity();
            }
            else if (!string.Equals(matchSessionId, currentSessionId, StringComparison.Ordinal) || matchEntity != currentMatchEntity)
                throw new InvalidOperationException("Original S073 session ID or match entity changed during diagnostic");
            if (match.StartupFailure != SkirmishStartupFailureCode.None)
                throw new InvalidOperationException("S073 startup failed: " + match.StartupFailure);
            if (match.Phase != SkirmishPhase.Playing) return false;
            MatchSceneView[] views = UnityEngine.Object.FindObjectsOfType<MatchSceneView>();
            if (views.Length != 1) throw new InvalidOperationException("Unique live S073 MatchSceneView required");
            object bootstrap = FieldValue(views[0], "matchBootstrapSystem");
            if (!ReferenceEquals(FieldValue(bootstrap, "runtimeWorld"), targetWorld))
                throw new InvalidOperationException("MatchSceneView is not bound to the retained source-created S073 world");
            object startup = FieldValue(bootstrap, "gameplayStartupSystem");
            if ((bool)FieldValue(startup, "gameplayStartFailed"))
                throw new InvalidOperationException("Original S073 gameplay startup owner reported failure");
            return (bool)FieldValue(startup, "gameplayStartComplete") && !(bool)FieldValue(startup, "gameplayStartPending");
        }

        static bool TryChooseCommand(EntityManager em, out Entity unit, out int2 destination)
        {
            unit = Entity.Null; destination = default;
            using var gridQuery = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
            if (gridQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique live source grid and path blockers required");
            Entity gridEntity = gridQuery.GetSingletonEntity();
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            var walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
            var blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            var occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            if (!RuntimeGridBlockerEpoch.IsCommitted(grid, blockers) || !blockers.Blocked.IsCreated ||
                blockers.GridSize != grid.Width * grid.Height || occupancy.GridSize != grid.Width * grid.Height || !occupancy.Occupied.IsCreated)
                throw new InvalidOperationException("Committed source blocker and occupancy snapshots required");
            using var units = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMovementBehavior>(),
                ComponentType.ReadOnly<UnitFootprint>(), ComponentType.ReadOnly<Faction>(), ComponentType.Exclude<UnitAirComponent>(),
                ComponentType.Exclude<UnitSpawnTransitTag>(), ComponentType.Exclude<UnitPathFollow>(), ComponentType.Exclude<UnitPathRange>(),
                ComponentType.Exclude<UnitPathRequest>());
            using var entities = units.ToEntityArray(Allocator.Temp);
            foreach (Entity candidate in entities)
            {
                if (em.GetComponentData<Faction>(candidate).Id != FactionIdentity.PlayerFactionId) continue;
                var cell = em.GetComponentData<UnitGrid>(candidate).Cell;
                var footprint = em.GetComponentData<UnitFootprint>(candidate).Size;
                if (footprint.x != 1 || footprint.y != 1 || !GridUtils.InBounds(cell, grid.Width, grid.Height)) continue;
                int sourceIndex = GridUtils.CellToIndex(cell, grid.Width);
                if (sourceIndex >= walkable.Length || walkable[sourceIndex].Value == 0) continue;
                int2[] offsets = { new int2(1,0), new int2(0,1), new int2(-1,0), new int2(0,-1), new int2(1,1), new int2(-1,1), new int2(1,-1), new int2(-1,-1) };
                foreach (int2 offset in offsets)
                {
                    int2 next = cell + offset;
                    if (!GridUtils.InBounds(next, grid.Width, grid.Height)) continue;
                    int index = GridUtils.CellToIndex(next, grid.Width);
                    if (index >= walkable.Length || walkable[index].Value == 0 || blockers.Blocked.IsSet(index) || occupancy.Occupied.IsSet(index)) continue;
                    unit = candidate; destination = next; return true;
                }
            }
            return false;
        }

        static void WriteResult(EntityManager em, UnitMoveOrderResultElement receipt, RuntimeGameplayStateComponent state)
        {
            string output = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
            if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(output))
                throw new InvalidOperationException("Fresh create-new /private/tmp output path required");
            JObject finishPins = HashPins();
            if (startPins == null || !JToken.DeepEquals(startPins, finishPins))
                throw new InvalidOperationException("Diagnostic, native probe, command owner or admission dependency changed during the S073 session");
            using var matchQuery = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (matchQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique original S073 match state required");
            var match = matchQuery.GetSingleton<SkirmishMatchState>();
            if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Phase != SkirmishPhase.Playing)
                throw new InvalidOperationException("Exact playing S073 session and seed required");
            using var sourceMapQuery = em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
            if (sourceMapQuery.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique operation map metadata required for source hash evidence");
            var sourceMap = sourceMapQuery.GetSingleton<OperationMapMetadataComponent>();
            JObject postApplyObservation = CaptureInputObservation(em, "post_apply_input_observation", finishPins);
            var result = new JObject {
                ["kind"] = "S073NativePathfindingCommandDiagnostic", ["schema_version"] = 1,
                ["scope"] = "RetainedOriginalS073World_ExplicitSyntheticCommand_NotNormalInputOrReadiness",
                ["world_name"] = targetWorld.Name, ["world_sequence"] = targetWorld.SequenceNumber.ToString(),
                ["session_id"] = match.SessionId.ToString(), ["seed"] = match.Seed, ["scenario_index"] = match.ScenarioIndex,
                ["match_entity"] = new JArray(matchEntity.Index, matchEntity.Version),
                ["simulation_active_before_and_after"] = state.SimulationActive,
                ["play_requested_observed"] = state.PlayRequested,
                ["input_provenance"] = "Diagnostic enqueued one explicit synthetic TargetPathOnly command through UnitMoveOrderRequestSystem.EnqueueTargetPathMoveOrder; not player input",
                ["request_id"] = requestId, ["unit"] = new JArray(targetUnit.Index, targetUnit.Version),
                ["goal"] = new JArray(goal.x, goal.y), ["production_owner_issued"] = receipt.Issued != 0,
                ["input_capture_qualification"] = "Pre-enqueue and post-apply observations of the live native world; they are not an exact scheduler-time job snapshot and may differ because the simulation continues",
                ["pre_enqueue_input_observation"] = preEnqueueObservation,
                ["post_apply_input_observation"] = postApplyObservation,
                ["owner_path_requests"] = receipt.PathRequests,
                ["process_wide_pathfinding_admission_temporal_correlation_only"] = admission,
                ["admission_frame"] = admissionFrame,
                ["admission_observation_scope"] = "Static callback has no World parameter; these samples correlate frame and target UnitPathRequest presence only, not emitting World identity",
                ["process_wide_admission_samples_temporal_correlation_only"] = admissionSamples,
                ["path_follow_applied"] = em.HasComponent<UnitPathFollow>(targetUnit),
                ["path_range_applied"] = em.HasComponent<UnitPathRange>(targetUnit),
                ["source_map_id"] = "opmap.skirmish.industrial_basin", ["source_campaign_map_id"] = "opmap.skirmish.desert_base_01",
                ["source_identity_hash"] = sourceMap.Blob.Value.SourceIdentityHash.ToString(),
                ["source_content_hash"] = sourceMap.Blob.Value.SourceContentHash.ToString(),
                ["session_start_dependency_pins_sha256"] = startPins,
                ["session_finish_dependency_pins_sha256"] = finishPins,
                ["normal_input_playthrough"] = false, ["player_readiness"] = false
            };
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(result.ToString(Formatting.Indented) + "\n");
            Debug.Log("[S073NativePathfindingCommandDiagnostic] result=Recorded scope=explicit_synthetic_command path=" + output);
            Finish(0);
        }

        static JObject CaptureInputObservation(EntityManager em, string observationName, JObject sourcePins)
        {
            using var gridQuery = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent),
                typeof(DynamicOccupancyComponent), typeof(PathPoolComponent));
            if (gridQuery.CalculateEntityCount() != 1)
                throw new InvalidOperationException("Unique native grid, blockers, occupancy, and path pool required for input capture");
            Entity gridEntity = gridQuery.GetSingletonEntity();
            GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
            DynamicBlockerComponent blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
            long cellCount = (long)grid.Width * grid.Height;
            if (cellCount <= 0 || cellCount > 2_097_152 || !RuntimeGridBlockerEpoch.IsCommitted(grid, blockers) || !blockers.Blocked.IsCreated ||
                blockers.GridSize != grid.Width * grid.Height || blockers.Blocked.Length != cellCount || !blockers.Counts.IsCreated ||
                !blockers.FriendlyPassFactionIds.IsCreated || blockers.Counts.Length != grid.Width * grid.Height ||
                blockers.FriendlyPassFactionIds.Length != grid.Width * grid.Height ||
                occupancy.GridSize != grid.Width * grid.Height || !occupancy.Occupied.IsCreated ||
                occupancy.Occupied.Length != cellCount || blockers.WriteInProgress != 0)
                throw new InvalidOperationException("Committed native blocker and occupancy storage required for fingerprint capture");

            DynamicBuffer<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
            if (walkable.Length != grid.Width * grid.Height)
                throw new InvalidOperationException("Native walkable grid dimensions do not match its buffer");
            string gridFingerprint = HashBytes(writer => {
                writer.Write(grid.Width); writer.Write(grid.Height); writer.Write(grid.CellSize);
                writer.Write(grid.Origin.x); writer.Write(grid.Origin.y); writer.Write(grid.Origin.z);
                writer.Write(walkable.Length);
                for (int i = 0; i < walkable.Length; i++) writer.Write(walkable[i].Value);
            });
            string blockerFingerprint = HashBytes(writer => {
                writer.Write(blockers.GridSize); writer.Write(blockers.StorageEpoch);
                writer.Write(blockers.AppliedStorageEpoch); writer.Write(blockers.AppliedRevision);
                for (int i = 0; i < blockers.GridSize; i++) {
                    writer.Write(blockers.Blocked.IsSet(i));
                    writer.Write(blockers.Counts[i]);
                    writer.Write(blockers.FriendlyPassFactionIds[i]);
                }
            });
            string occupancyFingerprint = HashBytes(writer => {
                writer.Write(occupancy.GridSize);
                for (int i = 0; i < occupancy.GridSize; i++) writer.Write(occupancy.Occupied.IsSet(i));
            });

            int frame = UnityEngine.Time.frameCount;
            double editorTime = EditorApplication.timeSinceStartup;
            JObject rawSnapshot = CaptureRawBlockerOccupancySnapshot(
                grid, blockers, occupancy, observationName, frame, editorTime, sourcePins);

            int2 unitCell = em.HasComponent<UnitGrid>(targetUnit) ? em.GetComponentData<UnitGrid>(targetUnit).Cell : default;
            int2 footprint = em.HasComponent<UnitFootprint>(targetUnit) ? em.GetComponentData<UnitFootprint>(targetUnit).Size : default;
            string sourcePrefabKey = em.HasComponent<UnitSourcePrefabKey>(targetUnit)
                ? em.GetComponentData<UnitSourcePrefabKey>(targetUnit).Value.ToString() : "missing";
            var observation = new JObject {
                ["observation_name"] = observationName,
                ["world_sequence"] = targetWorld.SequenceNumber.ToString(),
                ["session_id"] = matchSessionId,
                ["frame"] = frame,
                ["editor_time_since_startup_seconds"] = editorTime,
                ["unit_entity"] = new JArray(targetUnit.Index, targetUnit.Version),
                ["unit_start_cell_observed"] = new JArray(unitCell.x, unitCell.y),
                ["unit_footprint_cells"] = new JArray(footprint.x, footprint.y),
                ["unit_source_prefab_key"] = sourcePrefabKey,
                ["request_goal"] = new JArray(goal.x, goal.y),
                ["grid_entity"] = new JArray(gridEntity.Index, gridEntity.Version),
                ["grid_width"] = grid.Width, ["grid_height"] = grid.Height,
                ["grid_cell_size"] = grid.CellSize,
                ["grid_origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z),
                ["grid_walkable_fingerprint_sha256"] = gridFingerprint,
                ["blocker_fingerprint_sha256"] = blockerFingerprint,
                ["blocker_storage_epoch"] = blockers.StorageEpoch.ToString(),
                ["blocker_applied_revision"] = blockers.AppliedRevision.ToString(),
                ["occupancy_fingerprint_sha256"] = occupancyFingerprint,
                ["raw_blocker_occupancy_snapshot"] = rawSnapshot,
                ["occupancy_contributor_census"] = CaptureOccupancyContributorCensus(em, grid, gridEntity, occupancy, sourcePins)
            };
            if (em.HasComponent<UnitPathFollow>(targetUnit) && em.HasComponent<UnitPathRange>(targetUnit)) {
                UnitPathFollow follow = em.GetComponentData<UnitPathFollow>(targetUnit);
                UnitPathRange range = em.GetComponentData<UnitPathRange>(targetUnit);
                PathPoolComponent pool = em.GetComponentData<PathPoolComponent>(gridEntity);
                bool valid = pool.Cells.IsCreated && range.Start >= 0 && range.Length > 0 &&
                    range.Start <= pool.Cells.Length && range.Length <= pool.Cells.Length - range.Start;
                var cells = new JArray();
                if (valid) for (int i = 0; i < range.Length; i++) {
                    int2 cell = pool.Cells[range.Start + i];
                    cells.Add(new JArray(cell.x, cell.y));
                }
                observation["path_pool_range_valid"] = valid;
                observation["path_pool_length"] = pool.Cells.IsCreated ? pool.Cells.Length : 0;
                observation["path_range_start"] = range.Start;
                observation["path_range_length"] = range.Length;
                observation["path_follow_index"] = follow.PathIndex;
                observation["resulting_path_cells"] = cells;
            } else {
                observation["path_pool_range_valid"] = false;
                observation["path_pool_range_unavailable_reason"] = "UnitPathFollow or UnitPathRange absent at observation time";
            }
            return observation;
        }

        // Read-only census of the exact live entities tracked by DynamicOccupancyRebuildSystem.
        // This does not import state, rebuild occupancy, or change component data.
        static JObject CaptureOccupancyContributorCensus(EntityManager em, GridConfig grid, Entity gridEntity,
            DynamicOccupancyComponent occupancy, JObject sourcePins)
        {
            using var tracked = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>() },
                None = new[] { ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<RuntimeBuildingCombatTag>(), ComponentType.ReadOnly<UnitDeathAnimationComponent>() }
            });
            using var entities = tracked.ToEntityArray(Allocator.Temp);
            using var candidatesQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>());
            using var candidateEntities = candidatesQuery.ToEntityArray(Allocator.Temp);
            int excludedStaticBlocker = 0, excludedRuntimeBuilding = 0, excludedDeathAnimation = 0;
            for (int i = 0; i < candidateEntities.Length; i++) {
                Entity candidate = candidateEntities[i];
                if (em.HasComponent<StaticGridBlocker>(candidate)) excludedStaticBlocker++;
                if (em.HasComponent<RuntimeBuildingCombatTag>(candidate)) excludedRuntimeBuilding++;
                if (em.HasComponent<UnitDeathAnimationComponent>(candidate)) excludedDeathAnimation++;
            }
            var contributors = new JArray();
            var observedCells = new Dictionary<int, List<string>>();
            int inBoundsFootprintCells = 0;
            int occupancyClaimCells = 0;
            int occupancyMissingCells = 0;
            int outOfBoundsFootprintCells = 0;
            int factionZeroActorCount = 0;
            int unfactionedContributorCount = 0;
            var factionCounts = new SortedDictionary<int, int>();
            var componentClasses = new SortedDictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
                int2 authoredSize = em.GetComponentData<UnitFootprint>(entity).Size;
                int sizeX = Math.Max(1, authoredSize.x), sizeY = Math.Max(1, authoredSize.y);
                Game.Runtime.Navigation.OccupancyBounds bounds =
                    Game.Runtime.Navigation.GridOccupancyKernel.Bounds(grid.Width, grid.Height, cell.x, cell.y, authoredSize.x, authoredSize.y);
                int minX = bounds.MinX, minY = bounds.MinZ, maxX = bounds.MaxX, maxY = bounds.MaxZ;
                bool hasFaction = em.HasComponent<Faction>(entity);
                int faction = hasFaction ? em.GetComponentData<Faction>(entity).Id : -1;
                if (hasFaction && faction == 0) factionZeroActorCount++;
                if (hasFaction) factionCounts[faction] = factionCounts.TryGetValue(faction, out int fc) ? fc + 1 : 1;
                else unfactionedContributorCount++;
                string className = em.HasComponent<UnitAirComponent>(entity) ? "air" : "ground_or_other";
                componentClasses[className] = componentClasses.TryGetValue(className, out int cc) ? cc + 1 : 1;
                string entityKey = entity.Index + ":" + entity.Version;
                var cells = new JArray();
                int entityInBounds = 0, entitySet = 0, entityMissing = 0;
                long nominalCells = (long)sizeX * sizeY;
                long clampedCells = (long)Math.Max(0, maxX - minX) * Math.Max(0, maxY - minY);
                int entityOut = (int)Math.Min(int.MaxValue, Math.Max(0L, nominalCells - clampedCells));
                outOfBoundsFootprintCells += entityOut;
                for (int y = minY; y < maxY; y++) for (int x = minX; x < maxX; x++) {
                    int2 c = new int2(x, y);
                    int index = GridUtils.CellToIndex(c, grid.Width);
                    entityInBounds++; inBoundsFootprintCells++;
                    bool set = index >= 0 && index < occupancy.Occupied.Length && occupancy.Occupied.IsSet(index);
                    if (set) entitySet++; else entityMissing++;
                    if (!observedCells.TryGetValue(index, out var claims)) observedCells[index] = claims = new List<string>();
                    claims.Add(entityKey);
                    cells.Add(new JObject { ["x"] = c.x, ["y"] = c.y, ["row_major_index"] = index,
                        ["occupancy_bit_set"] = set });
                }
                occupancyClaimCells += entitySet; occupancyMissingCells += entityMissing;
                contributors.Add(new JObject {
                    ["entity"] = new JArray(entity.Index, entity.Version), ["cell"] = new JArray(cell.x, cell.y),
                    ["authored_footprint"] = new JArray(authoredSize.x, authoredSize.y),
                    ["kernel_footprint"] = new JArray(sizeX, sizeY),
                    ["kernel_clamped_bounds_min"] = new JArray(minX, minY),
                    ["kernel_clamped_bounds_max_exclusive"] = new JArray(maxX, maxY), ["faction_id"] = hasFaction ? (JToken)JValue.FromObject(faction) : JValue.CreateNull(),
                    ["class"] = className, ["has_unit_air_component"] = em.HasComponent<UnitAirComponent>(entity),
                    ["has_spawn_transit_tag"] = em.HasComponent<UnitSpawnTransitTag>(entity),
                    ["has_path_follow"] = em.HasComponent<UnitPathFollow>(entity),
                    ["has_death_animation_component"] = em.HasComponent<UnitDeathAnimationComponent>(entity),
                    ["source_provenance"] = hasFaction && faction == 0 ? CaptureFactionZeroActorProvenance(em, entity) : null,
                    ["in_bounds_footprint_cells"] = entityInBounds, ["occupancy_set_footprint_cells"] = entitySet,
                    ["occupancy_missing_footprint_cells"] = entityMissing, ["out_of_bounds_footprint_cells"] = entityOut,
                    ["footprint_cells"] = cells
                });
            }
            var overlapCells = new JArray();
            foreach (var pair in observedCells) if (pair.Value.Count > 1)
                overlapCells.Add(new JObject { ["row_major_index"] = pair.Key,
                    ["cell"] = new JArray(pair.Key % grid.Width, pair.Key / grid.Width),
                    ["occupancy_bit_set"] = occupancy.Occupied.IsSet(pair.Key), ["entity_keys"] = new JArray(pair.Value) });
            var unclaimedOccupiedCells = new JArray();
            int actualOccupiedCellCount = 0;
            for (int index = 0; index < occupancy.GridSize; index++) if (occupancy.Occupied.IsSet(index)) {
                actualOccupiedCellCount++;
                if (!observedCells.ContainsKey(index)) unclaimedOccupiedCells.Add(new JArray(index % grid.Width, index / grid.Width));
            }
            var factionSummary = new JObject();
            foreach (var pair in factionCounts) factionSummary[pair.Key.ToString()] = pair.Value;
            var classSummary = new JObject();
            foreach (var pair in componentClasses) classSummary[pair.Key] = pair.Value;
            return new JObject {
                ["source"] = "live_native_EntityManager_read_only_query",
                ["membership_contract"] = "DynamicOccupancyRebuildSystem tracked query: UnitGrid+UnitFootprint excluding StaticGridBlocker, RuntimeBuildingCombatTag, UnitDeathAnimationComponent; default EntityQuery semantics exclude Prefab and Disabled entities",
                ["candidate_entity_count_before_owner_exclusions"] = candidateEntities.Length,
                ["excluded_candidate_counts"] = new JObject { ["static_grid_blocker"] = excludedStaticBlocker,
                    ["runtime_building_combat"] = excludedRuntimeBuilding, ["unit_death_animation"] = excludedDeathAnimation },
                ["entity_query_semantics"] = "default query filters apply; Prefab and Disabled entities are not included",
                ["world_sequence"] = targetWorld.SequenceNumber.ToString(), ["session_id"] = matchSessionId,
                ["map_id"] = "opmap.skirmish.industrial_basin", ["source_campaign_map_id"] = "opmap.skirmish.desert_base_01",
                ["source_pins_sha256"] = sourcePins == null ? null : (JObject)sourcePins.DeepClone(),
                ["grid_entity"] = new JArray(gridEntity.Index, gridEntity.Version), ["grid_width"] = grid.Width,
                ["grid_height"] = grid.Height, ["entity_count"] = entities.Length,
                ["counts_by_faction_id"] = factionSummary,
                ["unfactioned_contributor_count"] = unfactionedContributorCount,
                ["counts_by_component_class"] = classSummary,
                ["footprint_cell_total_in_bounds"] = inBoundsFootprintCells,
                ["footprint_cell_total_out_of_bounds"] = outOfBoundsFootprintCells,
                ["out_of_bounds_definition"] = "Nominal normalized footprint cells clipped by the directly called production GridOccupancyKernel.Bounds; actual production iteration stays in-grid",
                ["footprint_cells_with_occupancy_bit_set"] = occupancyClaimCells,
                ["footprint_cells_missing_occupancy_bit"] = occupancyMissingCells,
                ["distinct_live_contributor_cells"] = observedCells.Count,
                ["actual_occupied_cell_count"] = actualOccupiedCellCount,
                ["occupied_cells_without_live_tracked_entity_footprint_count"] = unclaimedOccupiedCells.Count,
                ["occupied_cells_without_live_tracked_entity_footprint"] = unclaimedOccupiedCells,
                ["overlap_cell_count"] = overlapCells.Count, ["overlap_cells"] = overlapCells,
                ["entities"] = contributors,
                ["read_only"] = true, ["state_imported"] = false, ["gameplay_state_mutated"] = false
            };
        }

        static JObject CaptureFactionZeroActorProvenance(EntityManager em, Entity entity)
        {
            bool hasPrefabKey = em.HasComponent<UnitSourcePrefabKey>(entity);
            string prefabKey = hasPrefabKey ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() : null;
            bool hasMapBuilding = em.HasComponent<OperationMapBuildingComponent>(entity);
            JObject mapBuilding = null;
            if (hasMapBuilding) {
                OperationMapBuildingComponent source = em.GetComponentData<OperationMapBuildingComponent>(entity);
                mapBuilding = new JObject {
                    ["component_present"] = true,
                    ["operation_map_id"] = source.OperationMapId.ToString(),
                    ["stable_id"] = source.StableId.ToString(),
                    ["source_global_object_id"] = source.SourceGlobalObjectId.ToString(),
                    ["placement_index"] = source.PlacementIndex,
                    ["blocker_policy"] = source.BlockerPolicy.ToString()
                };
            }
            string entityName = null, nameStatus;
            try {
                entityName = em.GetName(entity);
                nameStatus = string.IsNullOrWhiteSpace(entityName) ? "present_empty_or_unavailable" : "present";
            } catch { nameStatus = "unknown_read_failed"; }

            bool hasSceneTag = em.HasComponent<SceneTag>(entity);
            JObject sceneAssociation = new JObject { ["scene_tag_component_present"] = hasSceneTag };
            if (hasSceneTag) {
                try {
                    Entity section = em.GetSharedComponent<SceneTag>(entity).SceneEntity;
                    sceneAssociation["scene_section_entity"] = section == Entity.Null ? null : (JToken)new JArray(section.Index, section.Version);
                    sceneAssociation["scene_section_entity_status"] = section == Entity.Null ? "present_null" : "present";
                    bool sectionExists = section != Entity.Null && em.Exists(section);
                    sceneAssociation["scene_section_exists"] = sectionExists;
                    if (sectionExists && em.HasComponent<SceneSectionData>(section)) {
                        SceneSectionData sectionData = em.GetComponentData<SceneSectionData>(section);
                        sceneAssociation["scene_guid"] = sectionData.SceneGUID.ToString();
                        sceneAssociation["scene_section_data_status"] = "present";
                    } else sceneAssociation["scene_section_data_status"] = sectionExists ? "absent" : "unknown_section_unavailable";
                    if (sectionExists && em.HasComponent<SceneEntityReference>(section)) {
                        Entity root = em.GetComponentData<SceneEntityReference>(section).SceneEntity;
                        sceneAssociation["scene_root_entity"] = root == Entity.Null ? null : (JToken)new JArray(root.Index, root.Version);
                        sceneAssociation["scene_root_reference_status"] = root == Entity.Null ? "present_null" : "present";
                    } else sceneAssociation["scene_root_reference_status"] = sectionExists ? "absent" : "unknown_section_unavailable";
                } catch { sceneAssociation["scene_association_read_status"] = "unknown_read_failed"; }
            } else sceneAssociation["scene_association_read_status"] = "component_absent";

            return new JObject {
                ["entity_name"] = entityName, ["entity_name_status"] = nameStatus,
                ["unit_source_prefab_key_component_present"] = hasPrefabKey,
                ["unit_source_prefab_key"] = prefabKey,
                ["unit_source_prefab_key_status"] = !hasPrefabKey ? "component_absent" :
                    string.IsNullOrWhiteSpace(prefabKey) ? "component_present_value_empty" : "component_present_value_observed",
                ["operation_map_building_component_present"] = hasMapBuilding,
                ["operation_map_building_status"] = hasMapBuilding ? "component_present_values_observed" : "component_absent",
                ["operation_map_building_source"] = mapBuilding,
                ["scene_association"] = sceneAssociation,
                ["evidence_scope"] = "Observed native entity component values and available ECS scene association only; no ownership or Campaign garrison inference"
            };
        }

        static JObject CaptureRawBlockerOccupancySnapshot(GridConfig grid, DynamicBlockerComponent blockers,
            DynamicOccupancyComponent occupancy, string observationName, int frame, double editorTime, JObject sourcePins)
        {
            string configuredDirectory = Environment.GetEnvironmentVariable(RawInputOutputDirectoryKey);
            if (string.IsNullOrWhiteSpace(configuredDirectory)) return null;
            string directory = Path.GetFullPath(configuredDirectory);
            if (!directory.StartsWith("/private/tmp/", StringComparison.Ordinal) && directory != "/private/tmp")
                throw new InvalidOperationException("Native raw S073 path snapshots require an explicit /private/tmp output directory");
            Directory.CreateDirectory(directory);
            string suffix = observationName == "pre_enqueue_input_observation" ? "pre-enqueue" :
                observationName == "post_apply_input_observation" ? "post-apply" : null;
            if (suffix == null) throw new InvalidOperationException("Unknown S073 raw path observation label");
            string rawPath = Path.Combine(directory, "native-blocker-occupancy-" + suffix + "-r1.bin");
            string sidecarPath = rawPath + ".json";
            int cells = blockers.GridSize;
            if (cells != checked(grid.Width * grid.Height) || occupancy.GridSize != cells ||
                blockers.Counts.Length != cells || blockers.FriendlyPassFactionIds.Length != cells ||
                !blockers.Blocked.IsCreated || blockers.Blocked.Length != cells || !occupancy.Occupied.IsCreated ||
                occupancy.Occupied.Length != cells || blockers.WriteInProgress != 0 ||
                !RuntimeGridBlockerEpoch.IsCommitted(grid, blockers))
                throw new InvalidOperationException("Committed, complete native S073 blocker and occupancy inputs required for raw snapshot");

            byte[] rawBytes;
            using (var memory = new MemoryStream(checked(60 + ((cells + 7) / 8) * 2 + cells * 5)))
            {
                using (var writer = new BinaryWriter(memory, Encoding.UTF8, true))
                {
                    writer.Write(new byte[] { (byte)'S', (byte)'0', (byte)'7', (byte)'3', (byte)'B', (byte)'O', (byte)'C', (byte)'1' });
                    writer.Write((uint)grid.Width); writer.Write((uint)grid.Height);
                    writer.Write(grid.CellSize); writer.Write(grid.Origin.x); writer.Write(grid.Origin.y); writer.Write(grid.Origin.z);
                    writer.Write(blockers.StorageEpoch); writer.Write(blockers.AppliedStorageEpoch); writer.Write(blockers.AppliedRevision);
                    writer.Write((uint)cells);
                    WriteBitSet(writer, cells, blockers.Blocked.IsSet);
                    for (int i = 0; i < cells; i++) writer.Write(blockers.Counts[i]);
                    for (int i = 0; i < cells; i++) writer.Write(blockers.FriendlyPassFactionIds[i]);
                    WriteBitSet(writer, cells, occupancy.Occupied.IsSet);
                    writer.Flush();
                }
                rawBytes = memory.ToArray();
            }
            string rawHash;
            using (var sha = SHA256.Create())
                rawHash = BitConverter.ToString(sha.ComputeHash(rawBytes)).Replace("-", "").ToLowerInvariant();
            using (var stream = new FileStream(rawPath, FileMode.CreateNew, FileAccess.Write))
                stream.Write(rawBytes, 0, rawBytes.Length);

            var sidecar = new JObject {
                ["kind"] = "S073BlockerOccupancyRawSnapshot", ["schema_version"] = 1,
                ["producer"] = "native", ["observation"] = observationName,
                ["raw_path"] = rawPath, ["raw_sha256"] = rawHash, ["byte_length"] = rawBytes.Length,
                ["width"] = grid.Width, ["height"] = grid.Height, ["cell_size"] = grid.CellSize,
                ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z),
                ["storage_epoch"] = blockers.StorageEpoch.ToString(),
                ["applied_storage_epoch"] = blockers.AppliedStorageEpoch.ToString(),
                ["applied_revision"] = blockers.AppliedRevision.ToString(),
                ["mission"] = "S073", ["seed"] = 104729,
                ["source_map_id"] = "opmap.skirmish.industrial_basin",
                ["source_campaign_map_id"] = "opmap.skirmish.desert_base_01",
                ["native_session_id"] = matchSessionId, ["native_world_sequence"] = targetWorld.SequenceNumber.ToString(),
                ["native_world_name"] = targetWorld.Name, ["native_frame"] = frame,
                ["native_editor_time_since_startup_seconds"] = editorTime,
                ["native_source_pins_sha256"] = sourcePins == null ? null : (JObject)sourcePins.DeepClone(),
                ["source_host_state_transferred"] = false, ["campaign_state_transferred"] = false,
                ["readiness_claimed"] = false,
                ["binary_layout"] = "S073BOC1_LE_v1_Header_GridAndBlockerEpochs_ThenRowMajorBlockedLsbBitset_CountsInt32_FriendlyPassFactionIdsUInt8_OccupiedLsbBitset"
            };
            using (var file = new FileStream(sidecarPath, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(sidecar.ToString(Formatting.Indented) + "\n");
            return sidecar;
        }

        static void WriteBitSet(BinaryWriter writer, int cells, Func<int, bool> isSet)
        {
            for (int baseIndex = 0; baseIndex < cells; baseIndex += 8)
            {
                byte bits = 0;
                for (int bit = 0; bit < 8 && baseIndex + bit < cells; bit++)
                    if (isSet(baseIndex + bit)) bits |= (byte)(1 << bit);
                writer.Write(bits);
            }
        }

        static string HashBytes(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) write(writer);
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }

        static JObject HashPins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var hashes = new JObject();
            using (var sha = SHA256.Create()) foreach (string pin in Pins)
                hashes[pin] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, pin)))).Replace("-", "").ToLowerInvariant();
            return hashes;
        }

        static object FieldValue(object target, string name)
        {
            if (target == null) throw new InvalidOperationException("Missing reflection target " + name);
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return field.GetValue(target);
        }

        static void FinishFailure(Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[S073NativePathfindingCommandDiagnostic] result=Failed scope=explicit_synthetic_command " + ex.Message);
            Finish(1);
        }

        static void Finish(int exitCode)
        {
            if (finished) return;
            finished = true;
            EditorApplication.update -= Observe;
            UnitPathfindingSystem.AdmissionObserved -= OnAdmission;
            SessionState.SetBool(CompletedKey, true);
            pendingExitCode = exitCode;
            exitDeadline = EditorApplication.timeSinceStartup + ExitTimeoutSeconds;
            SessionState.SetInt(PendingExitCodeKey, exitCode);
            SessionState.SetFloat(ExitDeadlineKey, (float)exitDeadline);
            AttachShutdownWait();
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;
            else ScheduleExitAfterEditMode();
        }

        static void AttachShutdownWait()
        {
            EditorApplication.update -= WaitForEditModeBeforeExit;
            EditorApplication.update += WaitForEditModeBeforeExit;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (pendingExitCode >= 0 && change == PlayModeStateChange.EnteredEditMode) ScheduleExitAfterEditMode();
        }

        static void WaitForEditModeBeforeExit()
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
            Debug.LogError("[S073NativePathfindingCommandDiagnostic] shutdown result=TimedOut waiting for Edit Mode; requesting bounded exit");
            pendingExitCode = 1;
            SessionState.SetInt(PendingExitCodeKey, pendingExitCode);
            EditorApplication.delayCall += ExitAfterEditModeWait;
        }

        static void ScheduleExitAfterEditMode()
        {
            if (pendingExitCode < 0) return;
            EditorApplication.update -= WaitForEditModeBeforeExit;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.delayCall -= ExitAfterEditModeWait;
            EditorApplication.delayCall += ExitAfterEditModeWait;
        }

        static void ExitAfterEditModeWait()
        {
            if (pendingExitCode < 0) return;
            int code = pendingExitCode;
            pendingExitCode = -1;
            SessionState.EraseInt(PendingExitCodeKey);
            SessionState.EraseFloat(ExitDeadlineKey);
            SessionState.EraseFloat(BootstrapStartedKey);
            SessionState.EraseFloat(StartedAtKey);
            SessionState.EraseString(CommandStateKey);
            SessionState.EraseString(StartPinsKey);
            SessionState.EraseString(WorldSequenceKey);
            SessionState.EraseBool(HasWorldSequenceKey);
            SessionState.EraseString(MatchIdentityKey);
            EditorApplication.Exit(code);
        }

        static void SaveCommandState()
        {
            var state = new JObject {
                ["request_id"] = requestId,
                ["unit_index"] = targetUnit.Index,
                ["unit_version"] = targetUnit.Version,
                ["goal_x"] = goal.x,
                ["goal_y"] = goal.y,
                ["simulation_active"] = simulationActiveAtStart,
                ["world_sequence"] = targetWorld.SequenceNumber.ToString(),
                ["start_pins"] = startPins,
                ["pre_enqueue_observation"] = preEnqueueObservation
            };
            SessionState.SetString(CommandStateKey, state.ToString(Formatting.None));
        }

        static void SaveMatchIdentity()
        {
            var state = new JObject {
                ["session_id"] = matchSessionId,
                ["entity_index"] = matchEntity.Index,
                ["entity_version"] = matchEntity.Version
            };
            SessionState.SetString(MatchIdentityKey, state.ToString(Formatting.None));
        }

        static void RestoreMatchIdentity(string serialized)
        {
            var state = JObject.Parse(serialized);
            matchSessionId = (string)state["session_id"];
            matchEntity = new Entity { Index = (int)state["entity_index"], Version = (int)state["entity_version"] };
            if (string.IsNullOrWhiteSpace(matchSessionId) || matchEntity == Entity.Null)
                throw new InvalidOperationException("Persisted S073 match identity is invalid");
            hasMatchIdentity = true;
        }

        static void RestoreCommandState(string serialized)
        {
            var state = JObject.Parse(serialized);
            requestId = (int)state["request_id"];
            targetUnit = new Entity { Index = (int)state["unit_index"], Version = (int)state["unit_version"] };
            goal = new int2((int)state["goal_x"], (int)state["goal_y"]);
            simulationActiveAtStart = (byte)state["simulation_active"];
            targetWorldSequence = ulong.Parse((string)state["world_sequence"]);
            startPins = (JObject)state["start_pins"];
            preEnqueueObservation = state["pre_enqueue_observation"] as JObject;
            started = true;
        }
    }
}
#endif
