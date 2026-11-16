#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Game.Composition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;

namespace Game.Editor
{
    // Opt-in, read-only observation of actual registered S073 building instances.
    // It records TransformPoint results and the associated combat entity pose;
    // the latter is a related ECS value, not a full transform-parity claim.
    [InitializeOnLoad]
    internal static class S073NativeSpawnpointTransformObserver
    {
        const string OutputKey = "WARLINE_S073_NATIVE_SPAWNPOINT_TRANSFORM_OUTPUT";
        const string PendingExitCodeKey = "Warline.S073NativeSpawnpointTransform.PendingExitCode";
        const string ExitDeadlineKey = "Warline.S073NativeSpawnpointTransform.ExitDeadline";
        const string CompletedKey = "Warline.S073NativeSpawnpointTransform.Completed";
        const string AwaitingProbeEvidenceKey = "Warline.S073NativeSpawnpointTransform.AwaitingProbeEvidence";
        const string ProbeEvidenceDeadlineKey = "Warline.S073NativeSpawnpointTransform.ProbeEvidenceDeadline";
        const double ExitTimeoutSeconds = 30.0;
        const double ProbeEvidenceTimeoutSeconds = 30.0;
        static bool done;
        static bool waitingForProbeEvidence;
        static World firstWorld;
        static string firstSession;
        static int pendingExitCode = -1;
        static double exitDeadline;
        static double probeEvidenceDeadline;

        static S073NativeSpawnpointTransformObserver()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey))) return;
            pendingExitCode = SessionState.GetInt(PendingExitCodeKey, -1);
            if (pendingExitCode >= 0)
            {
                exitDeadline = SessionState.GetFloat(ExitDeadlineKey,
                    (float)(EditorApplication.timeSinceStartup + ExitTimeoutSeconds));
                AttachShutdownWait();
            }
            else if (SessionState.GetBool(AwaitingProbeEvidenceKey, false))
            {
                waitingForProbeEvidence = true;
                probeEvidenceDeadline = SessionState.GetFloat(ProbeEvidenceDeadlineKey,
                    (float)(EditorApplication.timeSinceStartup + ProbeEvidenceTimeoutSeconds));
                EditorApplication.update += WaitForProbeEvidence;
            }
            else if (!SessionState.GetBool(CompletedKey, false))
                EditorApplication.update += Observe;
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
                    SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                    if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Phase != SkirmishPhase.Playing) return;
                    string session = match.SessionId.ToString();
                    if (firstWorld == null) { firstWorld = world; firstSession = session; }
                    if (!ReferenceEquals(firstWorld, world) || firstSession != session)
                        throw new InvalidOperationException("S073 world or session changed during passive observation");

                    using (var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent)))
                    {
                        if (maps.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique S073 active map required");
                        ActiveOperationMapComponent active = maps.GetSingleton<ActiveOperationMapComponent>();
                        OperationMapMetadataComponent metadata = maps.GetSingleton<OperationMapMetadataComponent>();
                        if (!metadata.Blob.IsCreated || active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                            metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
                            throw new InvalidOperationException("Exact S073 map/source binding required");

                        MatchSceneView[] views = UnityEngine.Object.FindObjectsByType<MatchSceneView>(FindObjectsSortMode.None);
                        if (views.Length != 1) throw new InvalidOperationException("Unique live MatchSceneView required");
                        object bootstrap = Field(views[0], "matchBootstrapSystem");
                        if (!ReferenceEquals(FieldValue(bootstrap, "runtimeWorld"), world))
                            throw new InvalidOperationException("MatchSceneView is not bound to the observed S073 world");
                        object context = PropertyValue(bootstrap, "BuildingUiQueryContext");
                        object registry = FieldValue(context, "RuntimeBuildings");
                        if (!(registry is IDictionary buildings))
                            throw new InvalidOperationException("Actual managed runtime-building registry unavailable");

                        var targets = new System.Collections.Generic.List<(string key, object building)>();
                        foreach (DictionaryEntry entry in buildings)
                        {
                            object candidate = entry.Value;
                            object definition = FieldValue(candidate, "Definition");
                            object prefab = MemberValue(definition, "Prefab");
                            if (prefab is GameObject sourcePrefab &&
                                (sourcePrefab.name == "Building_Barrack" || sourcePrefab.name == "Building_Fuel_Bladder"))
                                targets.Add((sourcePrefab.name, candidate));
                        }
                        if (targets.Count != 4) throw new InvalidOperationException("Exactly four registered target instances (two source keys × factions 1/2) required");
                        var rows = new JArray();
                        foreach (string key in new[] { "Building_Barrack", "Building_Fuel_Bladder" })
                            foreach (byte faction in new byte[] { 1, 2 })
                            {
                                object[] matchesForPair = targets.FindAll(row => row.key == key &&
                                    (bool)MemberValue(row.building, "HasOwnerFaction") &&
                                    (byte)MemberValue(row.building, "OwnerFactionId") == faction).Select(row => row.building).ToArray();
                                if (matchesForPair.Length != 1)
                                    throw new InvalidOperationException("Expected exactly one registered " + key + " for faction " + faction);
                                rows.Add(CaptureBuilding(em, key, faction, matchesForPair[0]));
                            }

                        JObject pins = HashPins();
                        string output = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
                        if (!output.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(output))
                            throw new InvalidOperationException("Fresh /private/tmp output required");
                        var result = new JObject
                        {
                            ["kind"] = "S073NativeSpawnpointTransformObservation",
                            ["schema_version"] = 1,
                            ["scope"] = "ActualRegisteredS073BuildingGameObjectTransformPointAndAssociatedCombatEntityLocalTransform",
                            ["unity_version"] = Application.unityVersion,
                            ["world_name"] = world.Name,
                            ["world_sequence"] = world.SequenceNumber.ToString(),
                            ["session_id"] = session,
                            ["scenario_index"] = match.ScenarioIndex,
                            ["seed"] = match.Seed,
                            ["phase"] = match.Phase.ToString(),
                            ["active_map_id"] = active.OperationMapId.ToString(),
                            ["map_generation"] = active.Generation,
                            ["source_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
                            ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                            ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
                            ["registered_building_count"] = buildings.Count,
                            ["buildings"] = rows,
                            ["source_pins_sha256"] = pins,
                            ["faction_ids_required"] = new JArray(1, 2),
                            ["observation_is_passive"] = true,
                            ["gameplay_state_mutated"] = false,
                            ["objects_synthesized"] = false,
                            ["full_transform_parity_claimed"] = false,
                            ["mission_readiness_claimed"] = false
                        };
                        Directory.CreateDirectory(Path.GetDirectoryName(output));
                        using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                        using (var writer = new StreamWriter(file, new UTF8Encoding(false)))
                            writer.Write(result.ToString(Formatting.Indented) + "\n");
                        done = true;
                        EditorApplication.update -= Observe;
                        waitingForProbeEvidence = true;
                        probeEvidenceDeadline = EditorApplication.timeSinceStartup + ProbeEvidenceTimeoutSeconds;
                        SessionState.SetBool(AwaitingProbeEvidenceKey, true);
                        SessionState.SetFloat(ProbeEvidenceDeadlineKey, (float)probeEvidenceDeadline);
                        EditorApplication.update -= WaitForProbeEvidence;
                        EditorApplication.update += WaitForProbeEvidence;
                        Debug.Log("[S073NativeSpawnpointTransform] result=Recorded scope=actual_registered_instances buildings=" + rows.Count + " path=" + output);
                    }
                }
            }
            catch (Exception error)
            {
                done = true;
                SessionState.SetBool(CompletedKey, true);
                Debug.LogException(error);
                Debug.LogError("[S073NativeSpawnpointTransform] result=Failed " + error.Message);
                BeginShutdown(1);
            }
        }

        static void WaitForProbeEvidence()
        {
            if (!waitingForProbeEvidence) { EditorApplication.update -= WaitForProbeEvidence; return; }
            string predicate = Environment.GetEnvironmentVariable("WARLINE_S073_PREDICATE_OUTPUT");
            string mask = Environment.GetEnvironmentVariable("WARLINE_S073_MASK_OUTPUT");
            string audit = Environment.GetEnvironmentVariable("WARLINE_S073_PLACEMENT_AUDIT_OUTPUT");
            bool available = !string.IsNullOrWhiteSpace(predicate) && !string.IsNullOrWhiteSpace(mask) &&
                !string.IsNullOrWhiteSpace(audit) && File.Exists(predicate) && new FileInfo(predicate).Length > 0 &&
                File.Exists(mask) && File.Exists(mask + ".json") && File.Exists(audit) && new FileInfo(audit).Length > 0;
            if (available)
            {
                waitingForProbeEvidence = false;
                SessionState.SetBool(AwaitingProbeEvidenceKey, false);
                Debug.Log("[S073NativeSpawnpointTransform] harness_evidence=result=Recorded predicate=1 mask=1 placement_audit=1");
                BeginShutdown(0);
                return;
            }
            if (EditorApplication.timeSinceStartup < probeEvidenceDeadline) return;
            waitingForProbeEvidence = false;
            SessionState.SetBool(AwaitingProbeEvidenceKey, false);
            Debug.LogError("[S073NativeSpawnpointTransform] harness_evidence=result=Failed missing_fresh_predicate_mask_or_placement_audit");
            BeginShutdown(1);
        }

        static void BeginShutdown(int exitCode)
        {
            done = true;
            waitingForProbeEvidence = false;
            EditorApplication.update -= Observe;
            EditorApplication.update -= WaitForProbeEvidence;
            SessionState.SetBool(CompletedKey, true);
            SessionState.SetBool(AwaitingProbeEvidenceKey, false);
            SessionState.EraseFloat(ProbeEvidenceDeadlineKey);
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
            if (pendingExitCode >= 0 && state == PlayModeStateChange.EnteredEditMode)
                ScheduleExitAfterEditMode();
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
            Debug.LogError("[S073NativeSpawnpointTransform] shutdown result=TimedOut waiting for Edit Mode; requesting bounded exit");
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

        static JObject CaptureBuilding(EntityManager em, string expectedKey, byte expectedFaction, object building)
        {
            int id = (int)MemberValue(building, "Id");
            bool hasOwner = (bool)MemberValue(building, "HasOwnerFaction");
            byte faction = (byte)MemberValue(building, "OwnerFactionId");
            GameObject instance = FieldValue(building, "Instance") as GameObject;
            Entity entity = (Entity)MemberValue(building, "CombatEntity");
            if (!hasOwner || faction != expectedFaction || instance == null || entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<LocalTransform>(entity))
                throw new InvalidOperationException("Registered " + expectedKey + " requires live instance, faction and associated combat LocalTransform");
            Vector3[] localPoints = FieldValue(building, "ProductionSpawnLocalPositions") as Vector3[];
            if (localPoints == null || localPoints.Length == 0)
                throw new InvalidOperationException("Registered " + expectedKey + " production spawnpoint locals unavailable");

            LocalTransform ecs = em.GetComponentData<LocalTransform>(entity);
            Vector3 ecsPosition = new Vector3(ecs.Position.x, ecs.Position.y, ecs.Position.z);
            Transform transform = instance.transform;
            Vector3 rootPosition = transform.position;
            Vector3 lossyScale = transform.lossyScale;
            const float scaleTolerance = 0.00001f;
            if (lossyScale.x <= 0f || lossyScale.y <= 0f || lossyScale.z <= 0f ||
                Mathf.Abs(lossyScale.x - lossyScale.y) > scaleTolerance ||
                Mathf.Abs(lossyScale.x - lossyScale.z) > scaleTolerance)
                throw new InvalidOperationException("Nonuniform or mirrored GameObject hierarchy scale cannot be compared to ECS LocalTransform: " + expectedKey);
            var points = new JArray();
            for (int i = 0; i < localPoints.Length; i++)
            {
                Vector3 worldPoint = transform.TransformPoint(localPoints[i]);
                float3 local = new float3(localPoints[i].x, localPoints[i].y, localPoints[i].z);
                float3 ecsPoint3 = ecs.Position + math.rotate(ecs.Rotation, local * ecs.Scale);
                Vector3 ecsSpawnpoint = new Vector3(ecsPoint3.x, ecsPoint3.y, ecsPoint3.z);
                points.Add(new JObject
                {
                    ["slot_index"] = i,
                    ["local_bits"] = VectorBits(localPoints[i]),
                    ["local_value"] = VectorNumbers(localPoints[i]),
                    ["gameobject_transform_point"] = VectorBits(worldPoint),
                    ["gameobject_transform_point_value"] = VectorNumbers(worldPoint),
                    ["ecs_local_transform_spawnpoint"] = VectorBits(ecsSpawnpoint),
                    ["ecs_local_transform_spawnpoint_value"] = VectorNumbers(ecsSpawnpoint),
                    ["gameobject_transform_point_minus_ecs_spawnpoint_bits"] = VectorBits(worldPoint - ecsSpawnpoint),
                    ["gameobject_transform_point_minus_ecs_spawnpoint_value"] = VectorNumbers(worldPoint - ecsSpawnpoint),
                    ["gameobject_transform_point_equals_ecs_spawnpoint_bits"] = SameBits(worldPoint, ecsSpawnpoint)
                });
            }
            return new JObject
            {
                ["source_definition_key"] = expectedKey,
                ["runtime_building_id"] = id,
                ["faction_id"] = faction,
                ["expected_faction_id"] = expectedFaction,
                ["instance_name"] = instance.name,
                ["instance_scene_path"] = HierarchyPath(instance.transform),
                ["instance_active_in_hierarchy"] = instance.activeInHierarchy,
                ["combat_entity_index"] = entity.Index,
                ["combat_entity_version"] = entity.Version,
                ["registered_gameobject_root_position"] = VectorBits(rootPosition),
                ["registered_gameobject_root_position_value"] = VectorNumbers(rootPosition),
                ["registered_gameobject_lossy_scale_bits"] = VectorBits(lossyScale),
                ["associated_ecs_local_transform"] = new JObject
                {
                    ["position_bits"] = VectorBits(ecsPosition),
                    ["position_value"] = VectorNumbers(ecsPosition),
                    ["rotation_bits"] = QuaternionBits(ecs.Rotation.value),
                    ["scale_bits"] = Bits(ecs.Scale)
                },
                ["root_position_delta_gameobject_minus_ecs_bits"] = VectorBits(rootPosition - ecsPosition),
                ["root_position_delta_gameobject_minus_ecs_value"] = VectorNumbers(rootPosition - ecsPosition),
                ["root_position_equal_ecs_bits"] = SameBits(rootPosition, ecsPosition),
                ["production_spawnpoints"] = points
            };
        }

        static string HierarchyPath(Transform value)
        {
            string path = value.name;
            while (value.parent != null) { value = value.parent; path = value.name + "/" + path; }
            return path;
        }
        static JObject VectorBits(Vector3 value) => new JObject { ["x"] = Bits(value.x), ["y"] = Bits(value.y), ["z"] = Bits(value.z) };
        static JArray VectorNumbers(Vector3 value) => new JArray(value.x, value.y, value.z);
        static JArray QuaternionBits(Unity.Mathematics.quaternion value) => new JArray(Bits(value.value.x), Bits(value.value.y), Bits(value.value.z), Bits(value.value.w));
        static string Bits(float value) => BitConverter.ToUInt32(BitConverter.GetBytes(value), 0).ToString("x8");
        static bool SameBits(Vector3 a, Vector3 b) => Bits(a.x) == Bits(b.x) && Bits(a.y) == Bits(b.y) && Bits(a.z) == Bits(b.z);
        static object Field(object target, string name) => FieldValue(target, name);
        static object FieldValue(object target, string name)
        {
            if (target == null) throw new InvalidOperationException("Missing reflection target for " + name);
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return field.GetValue(target);
        }
        static object PropertyValue(object target, string name)
        {
            if (target == null) throw new InvalidOperationException("Missing property target for " + name);
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) throw new MissingMemberException(target.GetType().FullName, name);
            return property.GetValue(target);
        }
        static object MemberValue(object target, string name)
        {
            if (target == null) throw new InvalidOperationException("Missing member target for " + name);
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(target);
            return PropertyValue(target, name);
        }
        static JObject HashPins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] paths = {
                "Assets/Game/Scripts/Editor/Skirmish/S073NativeSpawnpointTransformObserver.cs",
                "Assets/Game/Scripts/Editor/SkirmishIndustrialBasinPlacementProbe.cs",
                "Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntity.cs",
                "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
                "Assets/Game/Scripts/Components/SkirmishMatchComponents.cs",
                "Assets/Game/Scripts/Components/OperationMapComponents.cs"
            };
            var result = new JObject();
            using (var sha = SHA256.Create())
                foreach (string path in paths)
                {
                    string full = Path.Combine(root, path);
                    result[path] = File.Exists(full)
                        ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(full))).Replace("-", "").ToLowerInvariant()
                        : null;
                }
            return result;
        }
    }
}
#endif
