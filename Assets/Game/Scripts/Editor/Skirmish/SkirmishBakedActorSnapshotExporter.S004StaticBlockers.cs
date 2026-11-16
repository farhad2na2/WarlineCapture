#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
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
        // Capture physical inputs only; no scene gameplay state is exported or admitted.
        public static void RunS004StaticBlockerSourceCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-skirmishS004StaticBlockerData");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Fresh output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[index + 1]);
            const string guid = "cd836ff2ddd2a440e81db4a20332d266";
            const string scenePath = "Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/PreparedEntities.unity";
            const string mapPath = "Assets/Game/Resources/SkirmishS004OperationMap.asset";
            const string gridPath = "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Grid.asset";
            if (AssetDatabase.GUIDToAssetPath(guid) != scenePath || !File.ReadAllText(mapPath).Contains("authoredSubSceneGuid: " + guid))
                throw new InvalidDataException("Selected S004 authored subscene identity changed");
            var pins = SourcePins(scenePath, mapPath, gridPath); VerifyPins(pins);
            var originalWorld = World.DefaultGameObjectInjectionWorld; int originalCount = World.All.Count;
            var rows = new JArray(); int nonBuilding = 0;
            using (var world = new World("S004StaticBlockerSourceOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems) if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal)) throw new InvalidOperationException("Gameplay systems forbidden");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(guid), new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update(); if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene)) throw new InvalidOperationException("Selected authored subscene not loaded");
                var em = world.EntityManager; em.CompleteAllTrackedJobs();
                if (!em.HasBuffer<ResolvedSectionEntity>(scene) || em.GetBuffer<ResolvedSectionEntity>(scene, true).Length == 0) throw new InvalidOperationException("Resolved source sections required");
                var sections = em.GetBuffer<ResolvedSectionEntity>(scene, true).ToNativeArray(Allocator.Temp);
                try
                {
                    using var query = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
                    using var entities = query.ToEntityArray(Allocator.Temp);
                    foreach (var entity in entities)
                    {
                        if (!em.HasComponent<SceneTag>(entity)) throw new InvalidOperationException("Source blocker lacks scene ownership");
                        var section = em.GetSharedComponent<SceneTag>(entity).SceneEntity;
                        if (!sections.ToArray().Any(row => row.SectionEntity == section)) throw new InvalidOperationException("Source blocker belongs to another section");
                        var cell = em.GetComponentData<UnitGrid>(entity).Cell; var size = em.GetComponentData<GridBlockerSize>(entity).Size;
                        bool building = em.HasComponent<OperationMapBuildingComponent>(entity);
                        var origin = cell;
                        if (building)
                        {
                            if (!em.HasComponent<RuntimeBuildingCombatInfo>(entity) || !em.HasComponent<OperationMapBuildingIdentity>(entity)) throw new InvalidOperationException("Authored building physical identity missing");
                            origin = em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OriginCell;
                        }
                        else nonBuilding++;
                        var identity = building ? em.GetComponentData<OperationMapBuildingIdentity>(entity).StableId.ToString() : null;
                        var row = new JObject { ["source_entity"] = entity.Index + ":" + entity.Version, ["building"] = building, ["stable_id"] = identity,
                            ["origin"] = new JArray(origin.x, origin.y), ["unit_grid"] = new JArray(cell.x, cell.y), ["size"] = new JArray(size.x, size.y),
                            ["friendly_pass_present"] = em.HasComponent<FriendlyPassGridBlocker>(entity),
                            ["friendly_pass_faction"] = em.HasComponent<FriendlyPassGridBlocker>(entity) ? (JToken)em.GetComponentData<FriendlyPassGridBlocker>(entity).AllowedFactionId : JValue.CreateNull(),
                            ["opening_present"] = em.HasComponent<StaticGridBlockerSurfaceOpeningComponent>(entity),
                            ["opening"] = em.HasComponent<StaticGridBlockerSurfaceOpeningComponent>(entity) ? JObject.FromObject(em.GetComponentData<StaticGridBlockerSurfaceOpeningComponent>(entity)) : null,
                            ["source_section_owned"] = true };
                        rows.Add(row);
                    }
                }
                finally { sections.Dispose(); }
            }
            if (rows.Count == 0 || !ReferenceEquals(originalWorld, World.DefaultGameObjectInjectionWorld) || World.All.Count != originalCount) throw new InvalidOperationException("Source blocker capture or World cleanup failed");
            VerifyPins(pins);
            var result = new JObject { ["kind"] = "S004IsolatedStaticBlockerSourceCapture", ["status"] = "CollectedSourceOnly", ["scene_guid"] = guid, ["scene_path"] = scenePath,
                ["source_pins"] = JObject.FromObject(pins), ["blockers"] = rows, ["blocker_count"] = rows.Count, ["non_building_count"] = nonBuilding,
                ["gameplay_state_copied"] = false, ["live_session_observed"] = false, ["physical_source_validated"] = false, ["source_fit_certified"] = false, ["mission_ready"] = false };
            Directory.CreateDirectory(output); string file = Path.Combine(output, "static-blockers.json");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) writer.Write(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[S004StaticBlockerSourceCapture] result=Collected blockers=" + rows.Count + " nonBuilding=" + nonBuilding + " world=0 readiness=0");
        }
    }
}
#endif
