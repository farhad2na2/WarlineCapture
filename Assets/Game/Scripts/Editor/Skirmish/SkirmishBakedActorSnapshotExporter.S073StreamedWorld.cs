#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        // Isolated, pre-game source capture. It cannot establish a live S073
        // loading transition, source fit, scheduler admission, or readiness.
        public static void RunS073StreamedWorldSourceCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle Editor required for isolated source capture");
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-s073StreamedWorldOutput");
            if (index < 0 || index + 1 >= args.Length)
                throw new ArgumentException("Fresh capture output directory required");
            string directory = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[index + 1]);
            Directory.CreateDirectory(directory);
            const string sceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
                throw new InvalidOperationException("Original S073 Campaign ECS scene is missing");
            string sceneHash = SkirmishOfflineDataExporter.HashFile(scenePath);
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var missing = new JArray();
            JArray entities;
            JArray roots;
            int scenery;
            using (var world = new World("S073SourceStreamOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay systems cannot run during source capture");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(sceneGuid),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Original Campaign ECS scene did not finish streaming");
                var em = world.EntityManager;
                em.CompleteAllTrackedJobs();
                using var buildings = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
                using var buildingEntities = buildings.ToEntityArray(Allocator.Temp);
                scenery = buildingEntities.Length;
                string[] rootIds = buildingEntities.ToArray().Select(EntityId).ToArray();
                roots = new JArray(rootIds);
                entities = CaptureTemplateClosure(em, rootIds, missing);
            }
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld) ||
                SkirmishOfflineDataExporter.HashFile(scenePath) != sceneHash)
                throw new InvalidOperationException("Default world or captured source scene changed");
            var output = new JObject
            {
                ["schema_version"] = 1, ["kind"] = "S073IsolatedStreamedSourceWorldCapture",
                ["scope"] = "IsolatedPreGameSource_NotLiveSessionOrReadiness",
                ["scene_guid"] = sceneGuid, ["scene_path"] = scenePath, ["scene_sha256"] = sceneHash,
                ["unity_version"] = Application.unityVersion, ["building_faction_query_count"] = scenery,
                ["root_entity_ids"] = roots, ["entities"] = entities, ["unsupported_values"] = missing,
                ["entity_namespace"] = "One isolated streaming world; identities are not portable",
                ["capture_domain"] = "Game.Components, Unity.Transforms and linked/prefab/disabled/simulate components",
                ["closure_complete"] = false,
                ["live_s073_session_observed"] = false, ["physical_source_validated"] = false,
                ["scheduler_admitted"] = false, ["mission_ready"] = false
            };
            string path = Path.Combine(directory, "source-world.json");
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(output.ToString(Formatting.None));
            Debug.Log("[S073StreamedSourceWorld] result=Captured buildings=" + scenery +
                " entities=" + entities.Count + " missing=" + missing.Count + " ready=0");
        }
    }
}
#endif
