#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string S073MatchScenePath = "Assets/Game/Scenes/Match.unity";
        const string S073BlockerConfigGuid = "b2010000000000000000000000000008";

        // Captures only the original metadata used by the runtime blocker owner.
        // It does not run its random placement loop or allocate a mission World.
        public static void RunS073RuntimeBlockerPrefabMetadata()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");

            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishS073RuntimeBlockerMetadataData");
            if (at < 0 || at + 1 >= args.Length)
                throw new ArgumentException("Explicit fresh S073 runtime blocker metadata output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string outputFile = Path.Combine(output, "s073-runtime-blocker-prefab-metadata.json");
            if (File.Exists(outputFile)) throw new InvalidOperationException("Fresh blocker metadata output required");

            string sceneText = File.ReadAllText(S073MatchScenePath);
            MatchCollection configReferences = Regex.Matches(sceneText,
                @"(?m)^  runtimeGridBlockerConfig: \{fileID: 11400000, guid: ([a-f0-9]{32}), type: 2\}$");
            if (configReferences.Count != 1 || configReferences[0].Groups[1].Value != S073BlockerConfigGuid)
                throw new InvalidOperationException("Exact Match scene runtime blocker config reference required");
            string configPath = AssetDatabase.GUIDToAssetPath(S073BlockerConfigGuid);
            RuntimeGridBlockerSystemSceneConfigAsset config = AssetDatabase.LoadAssetAtPath<RuntimeGridBlockerSystemSceneConfigAsset>(configPath);
            if (string.IsNullOrEmpty(configPath) || config == null || !config.SpawnOnStart || config.BlockerCount != 1 ||
                config.Prefabs == null || config.Prefabs.Count != 63 || config.Prefabs.Any(p => p == null) ||
                config.Prefabs.Distinct().Count() != 63)
                throw new InvalidOperationException("Exact source-bound S073 runtime blocker config and 63 unique prefabs required");

            MethodInfo buildMetadata = typeof(RuntimeGridBlockerPresentationSystemHelper).GetMethod(
                "BuildMetadata", BindingFlags.Static | BindingFlags.NonPublic);
            if (buildMetadata == null) throw new MissingMethodException(typeof(RuntimeGridBlockerPresentationSystemHelper).FullName, "BuildMetadata");

            var prefabRows = new JArray();
            string[] prefabPaths = config.Prefabs.Select(AssetDatabase.GetAssetPath).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            if (prefabPaths.Any(string.IsNullOrWhiteSpace) || prefabPaths.Distinct(StringComparer.Ordinal).Count() != 63)
                throw new InvalidOperationException("All 63 configured blocker prefabs must resolve to distinct current project assets");
            string[] roots = new[] { S073MatchScenePath, configPath,
                "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
                "Assets/Game/Scripts/Configs/Scene/RuntimeGridBlockerSystemSceneConfigAsset.cs",
                "Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073RuntimeBlockers.cs" }
                .Concat(prefabPaths).ToArray();
            var pins = SourcePins(roots);

            foreach (GameObject prefab in config.Prefabs.OrderBy(p => AssetDatabase.GetAssetPath(p), StringComparer.Ordinal))
            {
                string path = AssetDatabase.GetAssetPath(prefab);
                object metadata = buildMetadata.Invoke(null, new object[] { prefab });
                if (metadata == null) throw new InvalidOperationException("Original BuildMetadata returned null for " + path);
                Type type = metadata.GetType();
                bool hasBounds = (bool)type.GetField("HasBounds").GetValue(metadata);
                Bounds bounds = (Bounds)type.GetField("LocalBounds").GetValue(metadata);
                bool centerOnly = (bool)type.GetField("BlocksOnlyCenterCell").GetValue(metadata);
                object kind = type.GetField("Kind").GetValue(metadata);
                prefabRows.Add(new JObject { ["config_index"] = config.Prefabs.IndexOf(prefab),
                    ["path"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path),
                    ["name"] = prefab.name, ["has_bounds"] = hasBounds,
                    ["local_bounds_center"] = new JArray(bounds.center.x, bounds.center.y, bounds.center.z),
                    ["local_bounds_size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z),
                    ["blocks_only_center_cell"] = centerOnly, ["kind"] = kind?.ToString() });
            }

            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("Source changed during S073 runtime blocker metadata capture: " + pin.Key);
            var report = new JObject { ["schema_version"] = 1, ["kind"] = "S073OriginalRuntimeBlockerPrefabMetadata",
                ["authority"] = "MatchSceneConfig_OriginalRuntimeGridBlockerPresentationSystemHelper.BuildMetadata",
                ["catalog_id"] = "S073", ["match_scene"] = S073MatchScenePath,
                ["config"] = new JObject { ["path"] = configPath, ["guid"] = S073BlockerConfigGuid,
                    ["spawn_on_start"] = config.SpawnOnStart, ["blocker_count"] = config.BlockerCount,
                    ["random_seed"] = config.RandomSeed, ["tree_spawn_ratio"] = config.TreeSpawnRatio,
                    ["tree_cluster_count"] = config.TreeClusterCount,
                    ["tree_cluster_spacing_min_cells"] = config.TreeClusterSpacingMinCells,
                    ["tree_cluster_spacing_max_cells"] = config.TreeClusterSpacingMaxCells,
                    ["tree_cluster_distance_min_cells"] = config.TreeClusterDistanceMinCells,
                    ["tree_cluster_distance_max_cells"] = config.TreeClusterDistanceMaxCells,
                    ["prefab_count"] = config.Prefabs.Count },
                ["prefabs"] = prefabRows, ["source_pins"] = JObject.FromObject(pins),
                ["random_placement_executed"] = false, ["blocker_spawned"] = false,
                ["mission_world_allocated"] = false, ["mission_initialized"] = false,
                ["native_visual_readiness"] = false };
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                writer.Write(report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishS073RuntimeBlockerPrefabMetadata] result=Passed prefabs=" + prefabRows.Count +
                " placementExecuted=0 blockerSpawned=0 missionWorld=0");
        }
    }
}
#endif
