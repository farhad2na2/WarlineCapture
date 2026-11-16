#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Opt-in passive snapshot of the original S073 startup. No gameplay state is written.
    [InitializeOnLoad]
    internal static class S073NativeAIStartupSnapshot
    {
        const string Key = "WARLINE_S073_NATIVE_AI_OUTPUT";
        const string PendingExitCodeKey = "WARLINE_S073_NATIVE_AI_PENDING_EXIT_CODE";
        const string ExitDeadlineKey = "WARLINE_S073_NATIVE_AI_EXIT_DEADLINE";
        const string CompletedKey = "WARLINE_S073_NATIVE_AI_COMPLETED";
        const double ExitTimeoutSeconds = 30.0;
        static readonly string[] Pins = {
            "Assets/Game/Resources/SkirmishIndustrialBasin.asset", "Assets/Game/Resources/SkirmishIndustrialBasin.asset.meta",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset", "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset.meta",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset", "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset.meta",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset", "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset.meta",
            "Assets/Game/Configs/Skirmish/Enemy.asset", "Assets/Game/Configs/Skirmish/Enemy.asset.meta",
            "Assets/Game/Configs/Skirmish/Player.asset", "Assets/Game/Configs/Skirmish/Player.asset.meta",
            "Assets/Game/Configs/Skirmish/Plan.asset", "Assets/Game/Configs/Skirmish/Plan.asset.meta",
            "Assets/Game/Configs/Skirmish/MapVehicles.asset", "Assets/Game/Configs/Skirmish/MapVehicles.asset.meta",
            "Assets/Game/Scripts/Components/SelectionComponents.cs", "Assets/Game/Scripts/Components/SelectionComponents.cs.meta",
            "Assets/Game/Scripts/Components/FactionAIComponents.cs", "Assets/Game/Scripts/Components/FactionAIComponents.cs.meta",
            "Assets/Game/Scripts/Composition/MatchSceneView.cs", "Assets/Game/Scripts/Composition/MatchSceneView.cs.meta",
            "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs", "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs.meta",
            "Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs", "Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs.meta",
            "Assets/Game/Scripts/Composition/MatchBootstrapStartupConfigProjection.cs", "Assets/Game/Scripts/Composition/MatchBootstrapStartupConfigProjection.cs.meta",
            "Assets/Game/Scripts/Composition/MatchAISettingsStartupProjection.cs", "Assets/Game/Scripts/Composition/MatchAISettingsStartupProjection.cs.meta",
            "Assets/Game/Scripts/Systems/AIStartupSystem.cs", "Assets/Game/Scripts/Systems/AIStartupSystem.cs.meta",
            "Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs", "Assets/Game/Scripts/Systems/AIFactionControlStartupSystem.cs.meta",
            "Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs", "Assets/Game/Scripts/Systems/InitialFactionSpawnCellSystem.cs.meta",
            "Assets/Game/Scripts/Configs/AISettingsValues.cs", "Assets/Game/Scripts/Configs/AISettingsValues.cs.meta",
            "Assets/Game/Scripts/Configs/AISettingsModels.cs", "Assets/Game/Scripts/Configs/AISettingsModels.cs.meta",
            "Assets/Game/Scripts/Configs/QuickGameConfig.cs", "Assets/Game/Scripts/Configs/QuickGameConfig.cs.meta",
            "Assets/Game/Scripts/Editor/SkirmishIndustrialBasinPlacementProbe.cs", "Assets/Game/Scripts/Editor/SkirmishIndustrialBasinPlacementProbe.cs.meta",
            "Assets/Game/Scripts/Editor/Skirmish/S073NativeAIStartupSnapshot.cs",
            "Assets/Game/Scripts/Editor/Skirmish/S073NativeAIStartupSnapshot.cs.meta"
        };
        static bool done;
        static int pendingExitCode = -1;
        static double exitDeadline;
        static string originalSession;
        static World originalWorld;
        static JObject initialPins;

        static S073NativeAIStartupSnapshot()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Key))) return;

            pendingExitCode = SessionState.GetInt(PendingExitCodeKey, -1);
            if (pendingExitCode >= 0)
            {
                exitDeadline = SessionState.GetFloat(ExitDeadlineKey, (float)(EditorApplication.timeSinceStartup + ExitTimeoutSeconds));
                AttachShutdownWait();
            }
            else if (!SessionState.GetBool(CompletedKey, false))
            {
                EditorApplication.update += Observe;
            }
        }

        static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            try
            {
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                EntityManager em = world.EntityManager;
                using (var matches = em.CreateEntityQuery(typeof(SkirmishMatchState)))
                {
                    if (matches.CalculateEntityCount() != 1) return;
                    var match = matches.GetSingleton<SkirmishMatchState>();
                    if (match.ScenarioIndex != 3 || match.Seed != 104729) return;
                    string session = match.SessionId.ToString();
                    if (originalSession == null) { originalSession = session; originalWorld = world; initialPins = HashPins(); }
                    if (session != originalSession || !ReferenceEquals(world, originalWorld)) throw new InvalidOperationException("Original S073 session or world changed");
                    if (match.Phase != SkirmishPhase.Playing) return;
                    if (match.StartupFailure.ToString() != "None") throw new InvalidOperationException("S073 startup failure: " + match.StartupFailure);
                    Capture(world, em, match);
                }
            }
            catch (Exception error) { FinishFailure(error); }
        }

        static void Capture(World world, EntityManager em, SkirmishMatchState match)
        {
            using (var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent)))
            using (var controls = em.CreateEntityQuery(typeof(FactionControlConfigTag), typeof(FactionControlEntry)))
            using (var plans = em.CreateEntityQuery(typeof(AIBuildPlan), typeof(AIBuildPlanEntry)))
            {
                if (maps.CalculateEntityCount() != 1 || controls.CalculateEntityCount() != 1)
                    throw new InvalidOperationException("Unique actual map and faction control buffer required");
                var active = maps.GetSingleton<ActiveOperationMapComponent>();
                var map = maps.GetSingleton<OperationMapMetadataComponent>();
                if (!map.Blob.IsCreated || active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                    map.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
                    throw new InvalidOperationException("Exact S073 live map/source identity unavailable");
                MatchSceneView[] views = UnityEngine.Object.FindObjectsOfType<MatchSceneView>();
                if (views.Length != 1) throw new InvalidOperationException("Unique live MatchSceneView required");
                object bootstrap = Field(views[0], "matchBootstrapSystem");
                if (!ReferenceEquals(FieldValue(bootstrap, "runtimeWorld"), world))
                    throw new InvalidOperationException("MatchSceneView is not bound to the original S073 World");
                object startup = Field(bootstrap, "gameplayStartupSystem");
                if (!(bool)FieldValue(startup, "gameplayStartComplete") || (bool)FieldValue(startup, "gameplayStartPending") ||
                    (bool)FieldValue(startup, "gameplayStartFailed")) return;
                object settings = FieldValue(startup, "pendingAiSettingsSnapshot");
                object fallback = FieldValue(startup, "initialFactionSpawnCellFallbackEntries");
                var controlsBuffer = em.GetBuffer<FactionControlEntry>(controls.GetSingletonEntity(), true);
                var controlRows = new JArray();
                var spawnRows = new JArray();
                MethodInfo resolveCell = startup.GetType().GetMethod("ResolveInitialFactionSpawnCell", BindingFlags.Instance | BindingFlags.NonPublic);
                if (resolveCell == null) throw new MissingMethodException(startup.GetType().FullName, "ResolveInitialFactionSpawnCell");
                foreach (var row in controlsBuffer)
                {
                    controlRows.Add(PrimitiveObject(row));
                    object[] args = { row.FactionId, default(int2) };
                    bool found = (bool)resolveCell.Invoke(startup, args);
                    var cell = (int2)args[1];
                    spawnRows.Add(new JObject { ["faction_id"] = row.FactionId, ["resolved"] = found,
                        ["cell"] = new JArray(cell.x, cell.y), ["scope"] = "PostStartupOriginalResolverObservation" });
                }
                using (var planEntities = plans.ToEntityArray(Allocator.Temp))
                {
                    var planRows = new JArray();
                    foreach (Entity entity in planEntities)
                    {
                        var plan = em.GetComponentData<AIBuildPlan>(entity);
                        var entries = em.GetBuffer<AIBuildPlanEntry>(entity, true);
                        var ordered = new JArray();
                        foreach (var entry in entries) ordered.Add(entry.BuildingId.ToString());
                        JObject row = (JObject)PrimitiveObject(plan); row["entity"] = new JArray(entity.Index, entity.Version);
                        row["entries_ordered"] = ordered; planRows.Add(row);
                    }
                    JObject finalPins = HashPins();
                    if (!JToken.DeepEquals(initialPins, finalPins)) throw new InvalidOperationException("Pinned inputs changed during observation");
                    string output = Path.GetFullPath(Environment.GetEnvironmentVariable(Key));
                    if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(output))
                        throw new InvalidOperationException("Fresh /private/tmp create-new output required");
                    var result = new JObject {
                        ["kind"] = "S073NativeAIStartupSnapshot", ["schema_version"] = 1,
                        ["scope"] = "OriginalS073InstrumentedStartupSnapshot_NotNormalInputOrReadiness",
                        ["unity_version"] = Application.unityVersion, ["world_name"] = world.Name,
                        ["world_sequence"] = world.SequenceNumber.ToString(), ["session_id"] = match.SessionId.ToString(),
                        ["scenario_index"] = match.ScenarioIndex, ["seed"] = match.Seed, ["phase"] = match.Phase.ToString(),
                        ["map_id"] = active.OperationMapId.ToString(), ["source_map_id"] = map.Blob.Value.SourceOperationMapId.ToString(),
                        ["source_identity_hash"] = map.Blob.Value.SourceIdentityHash.ToString(),
                        ["source_content_hash"] = map.Blob.Value.SourceContentHash.ToString(), ["map_generation"] = map.Generation,
                        ["pending_ai_settings_snapshot"] = PrimitiveObject(settings),
                        ["initial_faction_spawn_cell_fallback_entries"] = PrimitiveObject(fallback),
                        ["post_startup_spawn_resolver_observations"] = spawnRows,
                        ["faction_control_config_tag_count"] = controls.CalculateEntityCount(), ["faction_control_entries"] = controlRows,
                        ["ai_build_plan_count"] = planRows.Count, ["ai_build_plans"] = planRows,
                        ["source_pins_sha256"] = finalPins,
                        ["gameplay_start_complete"] = true, ["gameplay_start_failed"] = false,
                        ["normal_input_playthrough"] = false, ["player_readiness"] = false
                    };
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                    using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                    done = true;
                    SessionState.SetBool(CompletedKey, true);
                    Debug.Log("[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup path=" + output);
                    BeginShutdown(0);
                }
            }
        }

        static object Field(object target, string name) => FieldValue(target, name);
        static object FieldValue(object target, string name)
        {
            if (target == null) throw new InvalidOperationException("Missing reflection target for " + name);
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return field.GetValue(target);
        }
        static JToken PrimitiveObject(object value)
        {
            if (value == null) return JValue.CreateNull();
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string) return Scalar(value);
            if (value is IEnumerable enumerable)
            {
                var array = new JArray(); foreach (object item in enumerable) array.Add(PrimitiveObject(item)); return array;
            }
            var obj = new JObject();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string) || field.FieldType.IsValueType)
                    obj[field.Name] = PrimitiveObject(field.GetValue(value));
            }
            if (!obj.HasValues) throw new InvalidOperationException("No primitive fields in snapshot type " + type.FullName);
            return obj;
        }
        static JToken Scalar(object value)
        {
            if (value is float f) return new JObject { ["value"] = f, ["bits_hex"] = BitConverter.ToInt32(BitConverter.GetBytes(f), 0).ToString("x8") };
            if (value is double d) return new JObject { ["value"] = d, ["bits_hex"] = BitConverter.DoubleToInt64Bits(d).ToString("x16") };
            return JToken.FromObject(value);
        }
        static JObject HashPins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var hashes = new JObject(); using (var sha = SHA256.Create())
                foreach (string path in Pins) hashes[path] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, path)))).Replace("-", "").ToLowerInvariant();
            return hashes;
        }
        static void FinishFailure(Exception error)
        {
            done = true;
            SessionState.SetBool(CompletedKey, true);
            Debug.LogException(error);
            Debug.LogError("[S073NativeAIStartupSnapshot] result=Failed " + error.Message);
            BeginShutdown(1);
        }

        static void BeginShutdown(int exitCode)
        {
            done = true;
            EditorApplication.update -= Observe;
            pendingExitCode = exitCode;
            exitDeadline = EditorApplication.timeSinceStartup + ExitTimeoutSeconds;
            SessionState.SetInt(PendingExitCodeKey, exitCode);
            SessionState.SetFloat(ExitDeadlineKey, (float)exitDeadline);
            AttachShutdownWait();

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = false;
            else
                ScheduleExitAfterEditMode();
        }

        static void AttachShutdownWait()
        {
            EditorApplication.update -= WaitForEditModeBeforeExit;
            EditorApplication.update += WaitForEditModeBeforeExit;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (pendingExitCode < 0) return;
            if (state == PlayModeStateChange.EnteredEditMode) ScheduleExitAfterEditMode();
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
            Debug.LogError("[S073NativeAIStartupSnapshot] shutdown result=TimedOut waiting for Edit Mode; requesting bounded exit");
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
            int exitCode = pendingExitCode;
            pendingExitCode = -1;
            SessionState.EraseInt(PendingExitCodeKey);
            SessionState.EraseFloat(ExitDeadlineKey);
            EditorApplication.Exit(exitCode);
        }
    }
}
#endif
