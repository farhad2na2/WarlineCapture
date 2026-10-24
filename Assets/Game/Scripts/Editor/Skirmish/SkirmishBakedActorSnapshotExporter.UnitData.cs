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
        public static void RunCompleteUnitReferenceData()
        {
            RunReferenceData();
            RunUnitReferenceData();
        }

        // Authoring metadata only. No mission, production update, instantiated
        // transport, camera state or future command/outcome is sampled here.
        public static void RunUnitReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-skirmishUnitReferenceData");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit new output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            if (preset == null || preset.buildingPlacement == null) throw new InvalidOperationException("Actual source preset missing");
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null) throw new InvalidOperationException("Actual scene unit catalog missing");
            var expansion = SkirmishExpansionCatalogConfig.Load();
            var definition = expansion?.Definitions.FirstOrDefault(d => d != null && d.CatalogId == "S002");
            if (definition == null) throw new InvalidOperationException("Actual S002 definition missing");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError)) throw new InvalidOperationException(matrixError);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 104731,
                manifest, matrix, out var setup, out var reasons)) throw new InvalidOperationException(string.Join(";", reasons));
            using var catalog = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig);
            var units = catalog.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.ToArray();
            if (units.Length == 0 || units.Length > 1024) throw new InvalidOperationException("Configured source unit domain invalid");
            var roots = units.Where(p => p != null).Select(AssetDatabase.GetAssetPath).Concat(new[] {
                AssetDatabase.GetAssetPath(preset), AssetDatabase.GetAssetPath(preset.buildingPlacement),
                AssetDatabase.GetAssetPath(sceneConfig), AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig),
                AssetDatabase.GetAssetPath(expansion) }).Distinct().ToArray();
            var pins = SourcePins(roots);
            var productionFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper")
                .GetMethod("TryGetMetadata", BindingFlags.Public | BindingFlags.Static);
            var definitionFactory = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper")
                .GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Public | BindingFlags.Static);
            if (productionFactory == null || definitionFactory == null) throw new InvalidOperationException("Original source metadata factory absent");
            var rows = new JArray(); var missing = new JArray(); var transports = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int index = 0; index < units.Length; index++)
            {
                GameObject prefab = units[index];
                if (prefab == null) { rows.Add(new JObject { ["index"] = index, ["prefab"] = new JObject { ["codec"] = "null-v1" } }); continue; }
                var productionArgs = new object[] { prefab, null }; var definitionArgs = new object[] { prefab, null };
                bool hasDefinition = (bool)definitionFactory.Invoke(null, definitionArgs);
                bool hasProduction = (bool)productionFactory.Invoke(null, productionArgs);
                var row = new JObject { ["index"] = index, ["key"] = prefab.name, ["prefab"] = SourceAsset(prefab),
                    ["has_definition_metadata"] = hasDefinition, ["has_production_metadata"] = hasProduction };
                if (hasDefinition) row["definition_metadata"] = CaptureMetadataFields(definitionArgs[1], prefab.name + "/definition", missing);
                if (hasDefinition || hasProduction)
                    row["authoring_catalog_properties"] = CaptureUnitCatalogAuthoringProperties(prefab, missing);
                if (hasProduction)
                {
                    row["production_metadata"] = CaptureMetadataFields(productionArgs[1], prefab.name + "/production", missing);
                    var transport = productionArgs[1].GetType().GetField("ProductionTransportPrefab").GetValue(productionArgs[1]) as GameObject;
                    if (transport != null) transports[AssetDatabase.GetAssetPath(transport)] = transport;
                }
                rows.Add(row);
            }
            foreach (string key in OpeningKeys)
                if (!rows.OfType<JObject>().Any(r => (string)r["key"] == key && (bool?)r["has_definition_metadata"] == true && (bool?)r["has_production_metadata"] == true))
                    throw new InvalidOperationException("Required opening unit metadata missing: " + key);
            var transportRows = new JArray();
            foreach (var pair in transports.OrderBy(p => p.Key, StringComparer.Ordinal))
                transportRows.Add(CaptureSourceTransportGraph(pair.Value, missing));
            if (missing.Count != 0) throw new InvalidOperationException("Required authored metadata has unsupported values: " + missing);
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source changed during export: " + pin.Key);
            var result = new JObject { ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishUnitProductionMetadata",
                ["authority"] = "OriginalConfiguredAuthoringFactories_NotMissionState", ["setup_hash"] = setup.SetupHash,
                ["seed"] = setup.Seed, ["units"] = rows, ["transports"] = transportRows, ["source_pins"] = JObject.FromObject(pins),
                ["unsupported_values"] = missing, ["baked_actor_footprint_substituted"] = false,
                ["ui_catalog_runtime_policy_sampled"] = false,
                ["runtime_camera_basis_exported"] = false, ["full_mission_initialized"] = false, ["native_visual_readiness"] = false };
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "unit-production-metadata.json"), result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishAuthoredUnitProductionMetadata] result=Passed units=" + rows.Count + " transports=" + transportRows.Count + " unsupported=0 fullMission=0");
        }

        static JObject CaptureMetadataFields(object value, string path, JArray missing)
        {
            var fields = new JObject();
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields[field.Name] = CaptureDefinitionValue(field.GetValue(value), path + "/" + field.Name, missing, 0);
            return new JObject { ["type"] = value.GetType().FullName, ["fields"] = fields };
        }

        static JObject CaptureUnitCatalogAuthoringProperties(GameObject prefab, JArray missing)
        {
            var authoring = prefab.GetComponents<Component>().SingleOrDefault(c => c != null && c.GetType().FullName == "Game.Authoring.UnitGridAuthoring");
            if (authoring == null) throw new InvalidOperationException("Required source unit authoring absent: " + prefab.name);
            var fields = new JObject();
            foreach (string name in new[] { "ConfiguredDisplayName", "ConfiguredDescription", "CanRequest", "MaterialsCost", "Price",
                "ProductionDurationSeconds", "PortraitSprite", "PortraitCardSprite", "PortraitActionSprite", "IsAirUnit", "IsProductionTransportUnit",
                "SoldierTransportCapacity", "ConfiguredAllowIdleWander", "ConfiguredResourceHaulerBarrelCapacity", "ConfiguredCanAttack",
                "ConfiguredAttackDamage", "ConfiguredAttackRange", "ConfiguredSpeed", "ConfiguredMaxHealth" })
            {
                var property = authoring.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property == null) throw new InvalidOperationException("Required catalog authoring getter absent: " + name);
                fields[name] = CaptureDefinitionValue(property.GetValue(authoring), prefab.name + "/catalogAuthoring/" + name, missing, 0);
            }
            return new JObject { ["source_type"] = authoring.GetType().FullName, ["fields"] = fields,
                ["runtime_catalog_admission_and_role_overlay_applied"] = false };
        }

        static JObject CaptureSourceTransportGraph(GameObject prefab, JArray missing)
        {
            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            if (transforms.Length == 0 || transforms.Length > 4096) throw new InvalidOperationException("Source transport hierarchy bound exceeded");
            var indices = transforms.Select((t, i) => new { t, i }).ToDictionary(p => p.t, p => p.i);
            var nodes = new JArray();
            foreach (var transform in transforms)
            {
                var meshes = new JArray();
                foreach (var mesh in transform.GetComponents<MeshFilter>())
                    if (mesh.sharedMesh != null)
                    {
                        var renderer = transform.GetComponent<MeshRenderer>();
                        meshes.Add(new JObject { ["kind"] = "MeshFilter", ["mesh"] = SourceAsset(mesh.sharedMesh),
                            ["renderer_present"] = renderer != null,
                            ["renderer_enabled"] = renderer != null ? (JToken)renderer.enabled : JValue.CreateNull(),
                            ["local_bounds"] = CaptureDefinitionValue(mesh.sharedMesh.bounds, prefab.name + "/meshBounds", missing, 0) });
                    }
                foreach (var mesh in transform.GetComponents<SkinnedMeshRenderer>())
                    if (mesh.sharedMesh != null) meshes.Add(new JObject { ["kind"] = "SkinnedMeshRenderer", ["mesh"] = SourceAsset(mesh.sharedMesh),
                        ["local_bounds"] = CaptureDefinitionValue(mesh.localBounds, prefab.name + "/skinBounds", missing, 0), ["enabled"] = mesh.enabled });
                var renderers = new JArray();
                foreach (var renderer in transform.GetComponents<Renderer>())
                    renderers.Add(new JObject { ["type"] = renderer.GetType().FullName, ["enabled"] = renderer.enabled,
                        ["local_bounds"] = CaptureDefinitionValue(renderer.localBounds, prefab.name + "/rendererLocalBounds", missing, 0),
                        ["world_bounds"] = CaptureDefinitionValue(renderer.bounds, prefab.name + "/rendererWorldBounds", missing, 0) });
                nodes.Add(new JObject { ["index"] = indices[transform], ["parent"] = transform.parent != null && indices.TryGetValue(transform.parent, out int parent) ? parent : -1,
                    ["name"] = transform.name, ["active_self"] = transform.gameObject.activeSelf,
                    ["local_position"] = CaptureDefinitionValue(transform.localPosition, prefab.name + "/position", missing, 0),
                    ["local_rotation"] = CaptureDefinitionValue(transform.localRotation, prefab.name + "/rotation", missing, 0),
                    ["initial_local_euler_degrees"] = CaptureDefinitionValue(transform.localEulerAngles, prefab.name + "/euler", missing, 0),
                    ["local_scale"] = CaptureDefinitionValue(transform.localScale, prefab.name + "/scale", missing, 0), ["meshes"] = meshes, ["renderers"] = renderers });
            }
            var row = new JObject { ["prefab"] = SourceAsset(prefab), ["nodes"] = nodes };
            var authoring = prefab.GetComponents<Component>().SingleOrDefault(c => c != null && c.GetType().FullName == "Game.Authoring.UnitGridAuthoring");
            if (authoring != null)
            {
                var fields = new JObject();
                foreach (string name in new[] { "IsAirUnit", "IsProductionTransportUnit", "SoldierTransportCapacity", "VehicleTransportCapacity", "CargoWeightCapacity",
                    "TransportCruiseHeight", "RunwayTaxiSpeed", "ConfiguredSpeed", "ProductionTransportArrivalSeconds", "ProductionTransportHoldForNextReadySeconds",
                    "ProductionTransportMaxConcurrent", "ProductionTransportRequiresAirportRunway", "ProductionTransportUsesRunwayLanding" })
                {
                    var property = authoring.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (property == null) throw new InvalidOperationException("Source transport property missing: " + name);
                    fields[name] = CaptureDefinitionValue(property.GetValue(authoring), prefab.name + "/" + name, missing, 0);
                }
                row["authoring_properties"] = fields;
            }
            return row;
        }
    }
}
#endif
