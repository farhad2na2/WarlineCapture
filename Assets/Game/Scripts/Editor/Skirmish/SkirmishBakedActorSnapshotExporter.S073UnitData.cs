#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        // S073 source authoring readback. This is deliberately separate from the
        // S002 reference-data entry point and never initializes a mission world.
        public static void RunS073UnitProductionReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");

            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishS073UnitProductionReferenceData");
            if (at < 0 || at + 1 >= args.Length)
                throw new ArgumentException("Explicit fresh S073 output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string outputFile = Path.Combine(output, "s073-unit-production-metadata.json");
            if (File.Exists(outputFile))
                throw new InvalidOperationException("Fresh S073 unit production output required");

            const int seed = 104729;
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.IndustrialBasinScenarioIndex);
            if (preset == null || preset.buildingPlacement == null)
                throw new InvalidOperationException("Actual S073 legacy preset and placement config required");
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null)
                throw new InvalidOperationException("Actual match scene unit prefab registry required");
            const string initialForcesPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset";
            const string legacyUnitRegistryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
            var initialForces = AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(initialForcesPath);
            var unitRegistry = AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(legacyUnitRegistryPath);
            var sceneUnitRegistry = sceneConfig.UnitPrefabRegistryConfig;
            if (initialForces == null || unitRegistry == null || sceneUnitRegistry == null)
                throw new InvalidOperationException("Exact S073 legacy InitialForces, preset UnitRegistry, and scene default registry required");
            if (!ReferenceEquals(preset.buildingPlacement.InitialUnitsConfig, initialForces) ||
                !ReferenceEquals(preset.buildingPlacement.UnitPrefabRegistryConfig, unitRegistry))
                throw new InvalidOperationException("S073 preset does not resolve to its exact InitialForces and UnitRegistry sources");

            // The legacy host resolves MatchSceneView.BuildingPlacementConfig to
            // the selected preset's buildingPlacement. The scene's serialized
            // default registry is a separate identity and is recorded as such.
            var units = unitRegistry.UnitSpawnPrefabs.ToArray();
            if (units.Length == 0 || units.Length > 1024 || units.All(unit => unit == null))
                throw new InvalidOperationException("Actual S073 legacy registered unit prefab domain invalid");
            var startingUnitRows = new JArray();
            foreach (var faction in initialForces.Factions)
            {
                if (faction == null) throw new InvalidOperationException("Null S073 legacy initial-force faction");
                foreach (var entry in faction.Units)
                {
                    if (entry == null || entry.Prefab == null)
                        throw new InvalidOperationException("Missing actual S073 legacy starting-unit prefab");
                    startingUnitRows.Add(new JObject {
                        ["faction_id"] = faction.FactionId,
                        ["prefab"] = SourceAsset(entry.Prefab),
                        ["count"] = entry.Count,
                        ["spawn_offset"] = new JObject { ["x"] = entry.SpawnOffset.x, ["y"] = entry.SpawnOffset.y }
                    });
                }
            }
            var startingPrefabs = initialForces.Factions.SelectMany(faction => faction?.Units ??
                throw new InvalidOperationException("Null S073 legacy initial-force faction"))
                .Where(entry => entry?.Prefab != null).Select(entry => entry.Prefab).ToArray();
            if (!units.Where(unit => unit != null).Intersect(startingPrefabs).Any())
                throw new InvalidOperationException("No actual S073 starting-unit prefab joins the registered unit catalog");

            var unitAssets = units.Where(unit => unit != null).Select(AssetDatabase.GetAssetPath);
            string[] roots = unitAssets.Concat(new[] {
                AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(preset.buildingPlacement),
                AssetDatabase.GetAssetPath(sceneConfig), AssetDatabase.GetAssetPath(sceneUnitRegistry),
                AssetDatabase.GetAssetPath(unitRegistry), AssetDatabase.GetAssetPath(initialForces),
                initialForcesPath, legacyUnitRegistryPath
            }).Where(path => !string.IsNullOrEmpty(path)).Distinct().ToArray();
            var pins = SourcePins(roots);

            var productionFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper")
                .GetMethod("TryGetMetadata", BindingFlags.Public | BindingFlags.Static);
            var definitionFactory = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper")
                .GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Public | BindingFlags.Static);
            if (productionFactory == null || definitionFactory == null)
                throw new InvalidOperationException("Original native source metadata readers absent");

            var rows = new JArray();
            var missing = new JArray();
            var transports = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int index = 0; index < units.Length; index++)
            {
                GameObject prefab = units[index];
                if (prefab == null)
                {
                    rows.Add(new JObject { ["index"] = index, ["prefab"] = new JObject { ["codec"] = "null-v1" } });
                    continue;
                }
                var productionArgs = new object[] { prefab, null };
                var definitionArgs = new object[] { prefab, null };
                bool hasDefinition = (bool)definitionFactory.Invoke(null, definitionArgs);
                bool hasProduction = (bool)productionFactory.Invoke(null, productionArgs);
                var row = new JObject {
                    ["index"] = index, ["key"] = prefab.name, ["prefab"] = SourceAsset(prefab),
                    ["has_definition_metadata"] = hasDefinition, ["has_production_metadata"] = hasProduction
                };
                if (hasDefinition)
                    row["definition_metadata"] = CaptureMetadataFields(definitionArgs[1], prefab.name + "/definition", missing);
                if (hasDefinition || hasProduction)
                    row["authoring_catalog_properties"] = CaptureUnitCatalogAuthoringProperties(prefab, missing);
                if (hasProduction)
                {
                    row["production_metadata"] = CaptureMetadataFields(productionArgs[1], prefab.name + "/production", missing);
                    var transport = productionArgs[1].GetType().GetField("ProductionTransportPrefab")
                        ?.GetValue(productionArgs[1]) as GameObject;
                    if (transport != null)
                        transports[AssetDatabase.GetAssetPath(transport)] = transport;
                }
                rows.Add(row);
            }

            var transportRows = new JArray();
            foreach (var pair in transports.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (!string.IsNullOrEmpty(pair.Key))
                    pins[Path.GetFullPath(pair.Key)] = SkirmishOfflineDataExporter.HashFile(pair.Key);
                transportRows.Add(CaptureSourceTransportGraph(pair.Value, missing));
            }
            if (missing.Count != 0)
                throw new InvalidOperationException("Required authored S073 metadata has unsupported values: " + missing);
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("Source changed during S073 export: " + pin.Key);

            var result = new JObject {
                ["schema_version"] = 1,
                ["kind"] = "S073AuthoredUnitProductionMetadata",
                ["authority"] = "OriginalConfiguredAuthoringFactories_NotMissionState",
                ["mission"] = "S073",
                ["seed_context"] = seed,
                ["scope"] = "LegacyInitialForcesAndUnitRegistry_AuthoringOnly_NotRuntimeActivation",
                ["legacy_preset"] = SourceAsset(preset),
                ["building_placement_config"] = SourceAsset(preset.buildingPlacement),
                ["unit_registry_config"] = SourceAsset(sceneConfig.UnitPrefabRegistryConfig),
                ["legacy_active_unit_registry"] = SourceAsset(unitRegistry),
                ["match_scene_default_unit_registry"] = SourceAsset(sceneUnitRegistry),
                ["registry_selection"] = "Legacy S073 uses SkirmishPreset.buildingPlacement through MatchSceneView.BuildingPlacementConfig; scene serialized default is retained separately and is not treated as the active S073 registry.",
                ["legacy_initial_forces_config"] = SourceAsset(initialForces),
                ["legacy_starting_units"] = startingUnitRows,
                ["registered_unit_prefabs"] = new JArray(units.Select((unit, index) => unit == null
                    ? new JObject { ["index"] = index, ["codec"] = "null-v1" }
                    : new JObject { ["index"] = index, ["prefab"] = SourceAsset(unit),
                        ["also_in_legacy_initial_forces"] = startingPrefabs.Contains(unit) })),
                ["units"] = rows,
                ["transports"] = transportRows,
                ["source_pins"] = JObject.FromObject(pins),
                ["unsupported_values"] = missing,
                ["baked_actor_footprint_substituted"] = false,
                ["ui_catalog_runtime_policy_sampled"] = false,
                ["runtime_camera_basis_exported"] = false,
                ["full_mission_initialized"] = false,
                ["native_visual_readiness"] = false
            };
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.WriteLine(result.ToString(Newtonsoft.Json.Formatting.Indented));
            Debug.Log("[S073AuthoredUnitProductionMetadata] result=Passed units=" + rows.Count +
                " transports=" + transportRows.Count + " unsupported=0 seed=" + seed + " fullMission=0");
        }
    }
}
#endif
