#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Editor
{
    // Opt-in observation only. This never launches a mission or writes gameplay state.
    [InitializeOnLoad]
    internal static class S073PrepareMapCensusObserver
    {
        private const string EnvironmentKey = "WARLINE_S073_PREPARE_CENSUS_OUTPUT";
        private const string SceneGuid = "dad0bd13fb20943dfb2f881cbe225f05";
        private static JObject before;
        private static string sessionId;
        private static bool done;

        static S073PrepareMapCensusObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentKey)))
                EditorApplication.update += Observe;
        }

        private static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                var em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                if (matches.CalculateEntityCount() != 1 || maps.CalculateEntityCount() != 1) return;
                var match = matches.GetSingleton<SkirmishMatchState>();
                var active = maps.GetSingleton<ActiveOperationMapComponent>();
                var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729 || !metadata.Blob.IsCreated ||
                    metadata.PhysicalSourceValidated == 0 || active.Generation != metadata.Generation ||
                    active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                    metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01") return;
                string scenePath = AssetDatabase.GUIDToAssetPath(SceneGuid);
                var sourceScene = SceneManager.GetSceneByPath(scenePath);
                if (string.IsNullOrEmpty(scenePath) || !sourceScene.IsValid() || !sourceScene.isLoaded) return;
                string currentSession = match.SessionId.ToString();
                if (before == null && match.Phase == SkirmishPhase.Queued)
                {
                    sessionId = currentSession;
                    before = Snapshot(em, match, active, metadata, scenePath);
                    return;
                }
                if (before == null || currentSession != sessionId || match.Phase != SkirmishPhase.Preparing) return;
                var after = Snapshot(em, match, active, metadata, scenePath);
                var result = new JObject
                {
                    ["kind"] = "S073NativePrepareMapSceneryCensus", ["schema_version"] = 1,
                    ["scope"] = "ObservedLoadedSceneAndEcsScenery_NotMissionOutcomeOrReadiness",
                    ["unity_version"] = Application.unityVersion,
                    ["source_pins"] = SourcePins(),
                    ["session_id"] = sessionId, ["before"] = before, ["after"] = after,
                    ["mission_ready"] = false, ["mission_outcome_observed"] = false
                };
                string path = Path.GetFullPath(Environment.GetEnvironmentVariable(EnvironmentKey));
                if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(path))
                    throw new InvalidOperationException("Fresh private temporary census output required");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                done = true;
                EditorApplication.update -= Observe;
                Debug.Log("[S073PrepareMapCensus] result=Passed scope=observed_before_after path=" + path);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                done = true;
                EditorApplication.update -= Observe;
                Debug.LogException(error);
                Debug.LogError("[S073PrepareMapCensus] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static JObject SourcePins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] paths = {
                "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs",
                "Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
                "Assets/Game/Scripts/Components/SkirmishMatchComponents.cs",
                "Assets/Game/Scripts/Components/OperationMapComponents.cs",
                "Assets/Game/Scripts/Editor/Skirmish/S073PrepareMapCensusObserver.cs"
            };
            var result = new JObject();
            using var sha = SHA256.Create();
            foreach (string relative in paths)
                result[relative] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, relative))))
                    .Replace("-", "").ToLowerInvariant();
            return result;
        }

        private static JObject Snapshot(EntityManager em, SkirmishMatchState match,
            ActiveOperationMapComponent active, OperationMapMetadataComponent metadata, string scenePath)
        {
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
            using var entities = query.ToEntityArray(Allocator.Temp);
            var rows = new JArray();
            foreach (var entity in entities)
            {
                var row = new JObject
                {
                    ["index"] = entity.Index, ["version"] = entity.Version,
                    ["faction"] = em.GetComponentData<Faction>(entity).Id,
                    ["ai"] = em.HasComponent<AIControlledTag>(entity),
                    ["combat_owner"] = em.HasComponent<RuntimeBuildingCombatInfo>(entity)
                        ? (int?)em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OwnerFactionId : null,
                    ["storage_owner"] = em.HasComponent<BuildingResourceStorageComponent>(entity)
                        ? (int?)em.GetComponentData<BuildingResourceStorageComponent>(entity).OwnerFactionId : null
                };
                rows.Add(row);
            }
            var scenes = new JArray();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new JObject { ["path"] = scene.path, ["loaded"] = scene.isLoaded });
            }
            return new JObject
            {
                ["phase"] = match.Phase.ToString(), ["source_scene_guid"] = SceneGuid,
                ["source_scene_path"] = scenePath, ["source_scene_loaded"] = SceneManager.GetSceneByPath(scenePath).isLoaded,
                ["loaded_scenes"] = scenes, ["map_id"] = active.OperationMapId.ToString(),
                ["source_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
                ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
                ["physical_source_validated"] = metadata.PhysicalSourceValidated,
                ["generation"] = metadata.Generation, ["entity_count"] = entities.Length, ["entities"] = rows
            };
        }
    }
}
#endif
