#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Composition;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Editor
{
    [InitializeOnLoad]
    internal static class S073PrepareMapBoundaryObserver
    {
        private const string EnvironmentKey = "WARLINE_S073_PREPARE_BOUNDARY_OUTPUT";
        private const string SourceSceneGuid = "dad0bd13fb20943dfb2f881cbe225f05";
        private const string EcsSceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        private static JObject before;
        private static string session;
        private static bool completed;

        static S073PrepareMapBoundaryObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentKey)))
                SkirmishLaunchProjection.PrepareMapObserver += Observe;
        }

        private static void Observe(EntityManager em, bool after)
        {
            if (completed) return;
            try
            {
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1) return;
                var match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729) return;
                var snapshot = Snapshot(em, match);
                if (!after)
                {
                    if (before != null || match.Phase != SkirmishPhase.Queued)
                        throw new InvalidOperationException("Unique queued S073 before-state required");
                    session = match.SessionId.ToString();
                    before = snapshot;
                    return;
                }
                if (before == null || session != match.SessionId.ToString() || match.Phase != SkirmishPhase.Preparing)
                    throw new InvalidOperationException("Matching S073 before/after session required");
                bool ecsReady = (bool)before["ecs_scene_loaded"] && (bool)snapshot["ecs_scene_loaded"] &&
                    (bool)before["authored_grid_section_owned"] && (bool)snapshot["authored_grid_section_owned"] &&
                    JToken.DeepEquals(before["ecs_scene_entity"], snapshot["ecs_scene_entity"]);
                var result = new JObject
                {
                    ["kind"] = "S073PrepareMapBoundaryCensus", ["schema_version"] = 1,
                    ["scope"] = "ObservedExactCallBoundary_NotMissionOutcomeOrReadiness",
                    ["unity_version"] = Application.unityVersion, ["session_id"] = session,
                    ["source_pins"] = SourcePins(), ["before"] = before, ["after"] = snapshot,
                    ["ecs_scene_loaded_both"] = ecsReady, ["mission_ready"] = false,
                    ["mission_outcome_observed"] = false
                };
                string path = Path.GetFullPath(Environment.GetEnvironmentVariable(EnvironmentKey));
                if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(path))
                    throw new InvalidOperationException("Fresh private temporary boundary output required");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                completed = true;
                SkirmishLaunchProjection.PrepareMapObserver -= Observe;
                Debug.Log("[S073PrepareMapBoundary] result=Passed ecsLoadedBoth=" + (ecsReady ? 1 : 0) + " path=" + path);
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                completed = true;
                SkirmishLaunchProjection.PrepareMapObserver -= Observe;
                Debug.LogException(error);
                Debug.LogError("[S073PrepareMapBoundary] result=Failed " + error.Message);
                EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }

        private static JObject Snapshot(EntityManager em, SkirmishMatchState match)
        {
            using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
            if (maps.CalculateEntityCount() != 1) throw new InvalidOperationException("Unique active operation-map root required");
            var active = maps.GetSingleton<ActiveOperationMapComponent>();
            var metadata = maps.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated || active.Generation != metadata.Generation ||
                active.OperationMapId.ToString() != "opmap.skirmish.industrial_basin" ||
                metadata.Blob.Value.SourceOperationMapId.ToString() != "opmap.skirmish.desert_base_01")
                throw new InvalidOperationException("Exact S073 logical/source map required");
            string scenePath = AssetDatabase.GUIDToAssetPath(SourceSceneGuid);
            var unityScene = SceneManager.GetSceneByPath(scenePath);
            var ecsGuid = new Unity.Entities.Hash128(EcsSceneGuid);
            var ecsScene = SceneSystem.GetSceneEntity(em.World.Unmanaged, ecsGuid);
            bool ecsLoaded = ecsScene != Entity.Null && SceneSystem.IsSceneLoaded(em.World.Unmanaged, ecsScene);
            int sections = ecsScene != Entity.Null && em.Exists(ecsScene) && em.HasBuffer<ResolvedSectionEntity>(ecsScene)
                ? em.GetBuffer<ResolvedSectionEntity>(ecsScene, true).Length : -1;
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            bool gridOwned = false;
            if (ecsLoaded && sections > 0 && grids.CalculateEntityCount() == 1)
            {
                var grid = grids.GetSingletonEntity();
                if (em.HasComponent<SceneTag>(grid))
                {
                    var section = em.GetSharedComponent<SceneTag>(grid).SceneEntity;
                    if (em.Exists(section) && em.HasComponent<SceneSectionData>(section) && em.HasComponent<SceneEntityReference>(section) &&
                        em.GetComponentData<SceneSectionData>(section).SceneGUID.Equals(ecsGuid) &&
                        em.GetComponentData<SceneEntityReference>(section).SceneEntity == ecsScene)
                        foreach (var resolved in em.GetBuffer<ResolvedSectionEntity>(ecsScene, true))
                            if (resolved.SectionEntity == section) gridOwned = true;
                }
            }
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
            using var entities = query.ToEntityArray(Allocator.Temp);
            var rows = new JArray();
            foreach (var entity in entities)
            {
                Entity section = em.HasComponent<SceneTag>(entity) ? em.GetSharedComponent<SceneTag>(entity).SceneEntity : Entity.Null;
                rows.Add(new JObject
                {
                    ["entity"] = new JArray(entity.Index, entity.Version), ["faction"] = em.GetComponentData<Faction>(entity).Id,
                    ["scene_section"] = section == Entity.Null ? null : new JArray(section.Index, section.Version),
                    ["ai"] = em.HasComponent<AIControlledTag>(entity),
                    ["combat_owner"] = em.HasComponent<RuntimeBuildingCombatInfo>(entity)
                        ? (int?)em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OwnerFactionId : null,
                    ["storage_owner"] = em.HasComponent<BuildingResourceStorageComponent>(entity)
                        ? (int?)em.GetComponentData<BuildingResourceStorageComponent>(entity).OwnerFactionId : null
                });
            }
            return new JObject
            {
                ["phase"] = match.Phase.ToString(), ["source_scene_guid"] = SourceSceneGuid,
                ["source_scene_path"] = scenePath, ["source_scene_loaded"] = unityScene.IsValid() && unityScene.isLoaded,
                ["ecs_scene_guid"] = EcsSceneGuid, ["ecs_scene_loaded"] = ecsLoaded,
                ["ecs_scene_entity"] = ecsScene == Entity.Null ? null : new JArray(ecsScene.Index, ecsScene.Version),
                ["resolved_section_count"] = sections, ["authored_grid_section_owned"] = gridOwned,
                ["physical_source_validated"] = metadata.PhysicalSourceValidated,
                ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
                ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
                ["generation"] = metadata.Generation, ["entity_count"] = entities.Length, ["entities"] = rows
            };
        }

        private static JObject SourcePins()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] paths = { "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs",
                "Assets/Game/Scripts/Composition/MatchGameplayStartupCompositionSystemHelper.cs",
                "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
                "Assets/Game/Scripts/Editor/Skirmish/S073PrepareMapBoundaryObserver.cs" };
            var pins = new JObject();
            using var sha = SHA256.Create();
            foreach (string relative in paths)
                pins[relative] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, relative))))
                    .Replace("-", "").ToLowerInvariant();
            return pins;
        }
    }
}
#endif
