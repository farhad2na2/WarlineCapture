#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Opt-in observation of the actual S073 MatchSceneView source-binding read.
    // It does not start a mission, update systems, or write gameplay state.
    [InitializeOnLoad]
    internal static class S073NativeEntitySceneBindingObserver
    {
        const string OutputKey = "WARLINE_S073_ENTITY_SCENE_BINDING_OUTPUT";
        static bool done;

        static S073NativeEntitySceneBindingObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey)))
                EditorApplication.update += Observe;
        }

        static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(GridRoad));
                using var references = em.CreateEntityQuery(typeof(MatchSceneReferenceComponent));
                if (matches.CalculateEntityCount() != 1) return;

                var match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Phase != SkirmishPhase.Playing) return;
                int mapCount = maps.CalculateEntityCount(), gridCount = grids.CalculateEntityCount(), referenceCount = references.CalculateEntityCount();
                if (mapCount != 1 || gridCount != 1 || referenceCount != 1)
                {
                    WriteResult(new JObject { ["kind"] = "S073NativeEntitySceneBindingObservation", ["schema_version"] = 1,
                        ["scope"] = "ActualMatchSceneViewTryReadEntitySceneGridBindingOutcome_NotReadinessOrMissionAcceptance",
                        ["unity_version"] = Application.unityVersion, ["world_name"] = world.Name,
                        ["world_sequence"] = world.Unmanaged.SequenceNumber.ToString(), ["session_id"] = match.SessionId.ToString(),
                        ["scenario_index"] = match.ScenarioIndex, ["seed"] = match.Seed, ["phase"] = match.Phase.ToString(),
                        ["binding_read_succeeded"] = false, ["binding_unavailable_reason"] = "UniqueMapGridOrMatchReferenceUnavailable",
                        ["cardinality"] = new JObject { ["maps"] = mapCount, ["grids"] = gridCount, ["match_references"] = referenceCount },
                        ["mission_ready"] = false, ["source_fit_certified"] = false, ["normal_input_playthrough"] = false,
                        ["player_readiness"] = false, ["source_pins_sha256"] = HashPins() });
                    return;
                }
                var mapEntity = maps.GetSingletonEntity();
                var gridEntity = grids.GetSingletonEntity();
                var active = maps.GetSingleton<ActiveOperationMapComponent>();
                var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
                var readiness = em.HasComponent<OperationMapReadinessComponent>(mapEntity)
                    ? em.GetComponentData<OperationMapReadinessComponent>(mapEntity) : default;
                var view = references.GetSingleton<MatchSceneReferenceComponent>().View.Value;
                RuntimeGridSourceBinding binding = default;
                string reason = null;
                bool available = view != null && view.TryReadEntitySceneGridBinding(world, mapEntity, gridEntity,
                    out binding, out reason);
                if (view == null) reason = "NormalMatchViewUnavailable";

                var grid = em.GetComponentData<GridConfig>(gridEntity);
                var result = new JObject
                {
                    ["kind"] = "S073NativeEntitySceneBindingObservation", ["schema_version"] = 1,
                    ["scope"] = "ActualMatchSceneViewTryReadEntitySceneGridBindingOutcome_NotReadinessOrMissionAcceptance",
                    ["unity_version"] = Application.unityVersion, ["world_name"] = world.Name,
                    ["world_sequence"] = world.Unmanaged.SequenceNumber.ToString(), ["session_id"] = match.SessionId.ToString(),
                    ["scenario_index"] = match.ScenarioIndex, ["seed"] = match.Seed, ["phase"] = match.Phase.ToString(),
                    ["binding_read_succeeded"] = available, ["binding_unavailable_reason"] = available ? (JToken)null : new JValue(reason),
                    ["map"] = new JObject { ["entity_index"] = mapEntity.Index, ["entity_version"] = mapEntity.Version,
                        ["operation_map_id"] = active.OperationMapId.ToString(), ["generation"] = active.Generation,
                        ["metadata_generation"] = metadata.Generation, ["physical_source_validated"] = metadata.PhysicalSourceValidated,
                        ["source_identity_hash"] = metadata.Blob.IsCreated ? metadata.Blob.Value.SourceIdentityHash.ToString() : null,
                        ["source_content_hash"] = metadata.Blob.IsCreated ? metadata.Blob.Value.SourceContentHash.ToString() : null,
                        ["generated_metadata_hash"] = metadata.Blob.IsCreated ? metadata.Blob.Value.GeneratedMetadataHash.ToString() : null },
                    ["grid"] = new JObject { ["entity_index"] = gridEntity.Index, ["entity_version"] = gridEntity.Version,
                        ["width"] = grid.Width, ["height"] = grid.Height, ["cell_size"] = grid.CellSize,
                        ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z) },
                    ["readiness"] = new JObject { ["present"] = em.HasComponent<OperationMapReadinessComponent>(mapEntity),
                        ["generation"] = readiness.Generation, ["ready_flags"] = (int)readiness.ReadyFlags,
                        ["required_flags"] = (int)readiness.RequiredFlags, ["failed_flags"] = (int)readiness.FailedFlags },
                    ["mission_ready"] = false, ["source_fit_certified"] = false,
                    ["normal_input_playthrough"] = false, ["player_readiness"] = false,
                    ["source_pins_sha256"] = HashPins()
                };
                if (available)
                    result["binding"] = new JObject { ["scene_root_index"] = binding.SceneRoot.Index,
                        ["scene_root_version"] = binding.SceneRoot.Version, ["generation"] = binding.Generation,
                        ["source_identity"] = binding.SourceIdentity, ["source_content"] = binding.SourceContent,
                        ["generated_metadata"] = binding.GeneratedMetadata };
                WriteResult(result);
            }
            catch (Exception error) { FinishFailure(error); }
        }

        static void WriteResult(JObject result)
        {
            string path = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(path))
                throw new InvalidOperationException("Fresh /private/tmp create-new output required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
            done = true;
            EditorApplication.update -= Observe;
            Debug.Log("[S073NativeEntitySceneBinding] result=Recorded scope=actual_binding_outcome binding_read_succeeded=" +
                result["binding_read_succeeded"] + " path=" + path);
            EditorApplication.Exit(0);
        }

        static JObject HashPins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string scenePath = AssetDatabase.GUIDToAssetPath("c00140f2e94a04c3084c8dcb0c18cbd0");
            string[] paths = { "Assets/Game/Scripts/Editor/Skirmish/S073NativeEntitySceneBindingObserver.cs",
                "Assets/Game/Scripts/Composition/MatchSceneView.RuntimeGridSourceBinding.cs",
                "Assets/Game/Scripts/Composition/MatchSceneView.cs",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset", scenePath };
            var result = new JObject();
            using (var sha = SHA256.Create())
                foreach (string path in paths)
                    result[path] = !string.IsNullOrEmpty(path) && File.Exists(Path.Combine(root, path))
                        ? BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, path)))).Replace("-", "").ToLowerInvariant()
                        : null;
            return result;
        }

        static void FinishFailure(Exception error)
        {
            done = true; EditorApplication.update -= Observe; Debug.LogException(error);
            Debug.LogError("[S073NativeEntitySceneBinding] result=Failed " + error.Message);
            EditorApplication.Exit(1);
        }
    }
}
#endif
