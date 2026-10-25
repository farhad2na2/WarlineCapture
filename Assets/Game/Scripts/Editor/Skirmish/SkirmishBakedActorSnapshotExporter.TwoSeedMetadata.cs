#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Metadata-only qualification. No mission, actor, storage or opening placement is transferred.
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        public static void RunTwoSeedMetadataQualification()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishTwoSeedMetadata");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit new output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string file = Path.Combine(output, "two-seed-metadata.json");
            if (File.Exists(file)) throw new InvalidOperationException("Output already exists");
            var baseline = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));
            var expansion = SkirmishExpansionCatalogConfig.Load();
            var definition = expansion?.Definitions.SingleOrDefault(d => d != null && d.CatalogId == "S002");
            if (baseline == null || baseline.buildingPlacement == null || scene == null || scene.UnitPrefabRegistryConfig == null || definition == null)
                throw new InvalidOperationException("Original source providers are incomplete");
            var roots = new[] { AssetDatabase.GetAssetPath(baseline), AssetDatabase.GetAssetPath(baseline.buildingPlacement),
                AssetDatabase.GetAssetPath(scene), AssetDatabase.GetAssetPath(scene.UnitPrefabRegistryConfig),
                AssetDatabase.GetAssetPath(expansion), AssetDatabase.GetAssetPath(definition) };
            var pins = SourcePins(roots);
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            JObject first = CaptureTwoSeedContext(104731, baseline.buildingPlacement, scene, definition);
            JObject second = CaptureTwoSeedContext(1, baseline.buildingPlacement, scene, definition);
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld)) throw new InvalidOperationException("Default World changed");
            RequireMetadataEqual(first["metadata"], second["metadata"]);
            var negatives = new JArray();
            RejectMetadataMutation(first, second, "prefab-guid", m => m["buildings"][0]["prefab"]["guid"] = new string('0', 32), negatives);
            RejectMetadataMutation(first, second, "prefab-local-id", m => m["buildings"][0]["prefab"]["local_id"] = -123, negatives);
            RejectMetadataMutation(first, second, "provider-order", m => { var a = (JArray)m["providers"]["baseline"]; JToken old = a[0].DeepClone(); a[0] = a[1].DeepClone(); a[1] = old; }, negatives);
            RejectMetadataMutation(first, second, "requestability", m => m["buildings"][0]["configured_entry"]["fields"]["CanRequest"]["value"] = !(bool)m["buildings"][0]["configured_entry"]["fields"]["CanRequest"]["value"], negatives);
            RejectMetadataMutation(first, second, "definition-field", m => m["buildings"][0]["fields"]["MaxHealth"]["value"] = "1201", negatives);
            RejectMetadataMutation(first, second, "recipe-quantity", m => m["recipes"][0]["Quantity"] = (int)m["recipes"][0]["Quantity"] + 1, negatives);
            RejectMetadataMutation(first, second, "unit-metadata", m => m["units"][0]["production_metadata"]["fields"]["ProductionDurationSeconds"]["bits"] = "3f800000", negatives);
            RejectMetadataMutation(first, second, "consumed-context", m => m["catalog_inputs"]["catalog_id"] = "S026", negatives);
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source drift: " + pin.Key);
            var report = new JObject { ["kind"] = "NativeTwoSeedMetadataQualification", ["mission"] = "S002", ["size"] = "Standard", ["difficulty"] = "Regular",
                ["contexts"] = new JArray(first, second), ["metadata_equal"] = true, ["negative_controls"] = negatives,
                ["source_pins"] = JObject.FromObject(pins), ["full_mission_initialized"] = false, ["opening_placement_transferred"] = false,
                ["runtime_state_transferred"] = false, ["public_admission_changed"] = false,
                ["scope"] = "OriginalCompilerProvidersAndMetadataFactoriesOnly" };
            byte[] bytes = Encoding.UTF8.GetBytes(report.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidOperationException("Bounded metadata output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(bytes, 0, bytes.Length);
            Debug.Log("[SkirmishTwoSeedMetadata] result=Passed contexts=2 negatives=" + negatives.Count + " scope=MetadataOnly");
        }

        static JObject CaptureTwoSeedContext(int seed, BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene, SkirmishScenarioDefinitionConfig definition)
        {
            return CaptureCompiledMetadataContext(seed, baseline, scene, definition, "S002", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard);
        }

        static JObject CaptureCompiledMetadataContext(int seed, BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene,
            SkirmishScenarioDefinitionConfig definition, string expectedCatalog, SkirmishDifficultyId difficulty, SkirmishSizeId size)
        {
            if (definition == null || definition.CatalogId != expectedCatalog) throw new InvalidOperationException("Exact compiled metadata definition required");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error)) throw new InvalidOperationException(error);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size, seed,
                manifest, matrix, out var setup, out var reasons)) throw new InvalidOperationException(string.Join(";", reasons));
            RequireCompiledMetadataContext(setup, seed, expectedCatalog, difficulty, size);
            uint originalHash = setup.SetupHash;
            bool wrongHashRejected = false;
            try
            {
                setup.SetupHash = originalHash ^ 1u;
                try { RequireCompiledMetadataContext(setup, seed, expectedCatalog, difficulty, size); }
                catch (InvalidOperationException) { wrongHashRejected = true; }
            }
            finally { setup.SetupHash = originalHash; }
            if (!wrongHashRejected) throw new InvalidOperationException("Changed context hash was accepted");
            bool wrongSeedRejected = false;
            try { RequireCompiledMetadataContext(setup, seed == 1 ? 104731 : 1, expectedCatalog, difficulty, size); }
            catch (InvalidOperationException) { wrongSeedRejected = true; }
            if (!wrongSeedRejected) throw new InvalidOperationException("Wrong target seed was accepted");
            var actualTimingFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper").GetMethod("TryGetMetadata");
            var actualCreate = typeof(SkirmishProductionCatalog).GetMethods().Single(m => m.Name == "Create" && m.GetParameters().Length == 4);
            var timingResolver = Delegate.CreateDelegate(actualCreate.GetParameters()[3].ParameterType, actualTimingFactory);
            using var production = (SkirmishProductionCatalog)actualCreate.Invoke(null, new object[] { setup, baseline, scene, timingResolver });
            var buildings = production.Config.Spawnables.ToList();
            var units = production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.ToList();
            bool legacyS002Domain = expectedCatalog == "S002";
            if (expectedCatalog != "S002" && expectedCatalog != "S026") throw new NotSupportedException("Explicit metadata context family required: " + expectedCatalog);
            if (legacyS002Domain ? buildings.Count != 8 || units.Count != 12 :
                buildings.Count < 1 || buildings.Count > 64 || units.Count < 1 || units.Count > 64)
                throw new InvalidOperationException("Finite native source domain changed catalog=" + expectedCatalog + " buildings=" + buildings.Count + " units=" + units.Count + " expected=" + (legacyS002Domain ? "8/12" : "1..64/1..64"));
            var buildingKeys = CaptureUniqueCompiledDomain(buildings, expectedCatalog + "/buildings");
            var unitKeys = CaptureUniqueCompiledDomain(units, expectedCatalog + "/units");
            var missing = new JArray();
            var metadata = new JObject { ["providers"] = new JObject { ["baseline_asset"] = SourceAsset(baseline), ["scene_asset"] = SourceAsset(scene),
                ["unit_registry_asset"] = SourceAsset(scene.UnitPrefabRegistryConfig), ["baseline"] = CapturePrefabList(baseline.Spawnables),
                ["scene"] = CapturePrefabList(scene.Spawnables), ["units"] = CapturePrefabList(scene.UnitPrefabRegistryConfig.UnitSpawnPrefabs) },
                ["catalog_inputs"] = new JObject { ["catalog_id"] = setup.CatalogId, ["army_profile"] = setup.ArmyProfileId.ToString(),
                    ["structure_ids"] = new JArray(setup.Structures.Select(s => s.StructureId)),
                    ["role_overlays"] = CaptureDefinitionValue(setup.RoleOverlays, "catalog/roles", missing, 0) } };
            var recipes = new JArray();
            foreach (var recipe in production.Config.RuntimeProductionRecipes)
            {
                if (recipe.ProducerPrefab == null || recipe.UnitPrefab == null || !buildings.Contains(recipe.ProducerPrefab) || !units.Contains(recipe.UnitPrefab))
                    throw new InvalidOperationException("Original recipe outside selected native domain catalog=" + expectedCatalog + " buildings=" + buildings.Count + " units=" + units.Count + " recipe=" + recipes.Count + " producer=" + (recipe.ProducerPrefab == null ? "<null>" : recipe.ProducerPrefab.name) + " unit=" + (recipe.UnitPrefab == null ? "<null>" : recipe.UnitPrefab.name));
                recipes.Add(new JObject { ["ProducerPrefab"] = SourceAsset(recipe.ProducerPrefab), ["UnitPrefab"] = SourceAsset(recipe.UnitPrefab),
                    ["Quantity"] = recipe.Quantity, ["MaterialsCost"] = recipe.MaterialsCost, ["CreditsCost"] = recipe.CreditsCost,
                    ["UseProducerGroundExit"] = recipe.UseProducerGroundExit });
            }
            if (recipes.Count == 0) throw new InvalidOperationException("Original recipe output absent");
            metadata["recipes"] = recipes;
            var deliveries = new JArray();
            foreach (var overlay in setup.RoleOverlays)
            {
                bool found = production.TryReadProductionDeliverySeconds(overlay.RoleKind, out float seconds);
                deliveries.Add(new JObject { ["role"] = overlay.RoleKind.ToString(), ["found"] = found,
                    ["seconds"] = CaptureDefinitionValue(seconds, "catalog/delivery", missing, 0) });
            }
            metadata["delivery_seconds"] = deliveries;
            Type helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            Type resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true);
            var configure = helperType.GetMethod("ConfigureAuthoringMetadataResolvers");
            var cp = configure.GetParameters();
            configure.Invoke(helper, new object[] {
                Delegate.CreateDelegate(cp[0].ParameterType, resolverType.GetMethod("TryGetBuildingDefinitionMetadata")),
                Delegate.CreateDelegate(cp[1].ParameterType, resolverType.GetMethod("TryGetUnitDefinitionMetadata")) });
            helperType.GetMethod("RebuildSpawnablesLookup").Invoke(helper, new object[] { buildings, units });
            helperType.GetMethod("ConfigureProductionRecipes").Invoke(helper, new object[] { production.Config.RuntimeProductionRecipes });
            var rebuild = helperType.GetMethod("RebuildConfiguredSpawnableDefinitions");
            object destroy = Delegate.CreateDelegate(rebuild.GetParameters()[1].ParameterType,
                typeof(SkirmishBakedActorSnapshotExporter).GetMethod(nameof(DestroyTwoSeedObject), BindingFlags.Static | BindingFlags.NonPublic));
            var rows = new JArray();
            using (var world = new World("TwoSeedMetadataOnly", WorldFlags.Game))
            {
                try
                {
                    var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                    foreach (var prefab in buildings)
                    {
                        object value = helperType.GetMethod("CreateRuntimeBuildingDefinition").Invoke(helper,
                            new object[] { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, runway });
                        var fields = new JObject();
                        foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                            fields[field.Name] = CaptureDefinitionValue(field.GetValue(value), prefab.name + "/" + field.Name, missing, 0);
                        rows.Add(new JObject { ["index"] = rows.Count, ["prefab"] = SourceAsset(prefab), ["fields"] = fields,
                            ["construction_index"] = baseline.Spawnables.ToList().IndexOf(prefab), ["scene_index"] = scene.Spawnables.ToList().IndexOf(prefab) });
                    }
                    rebuild.Invoke(helper, new object[] { runway, destroy });
                    MethodInfo get = helperType.GetMethods().Single(m => m.Name == "TryGetConfiguredSpawnable" && m.GetParameters()[0].ParameterType == typeof(GameObject));
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        object[] call = { buildings[i], null };
                        if (!(bool)get.Invoke(helper, call)) throw new InvalidOperationException("Original configured factory rejected selected prefab");
                        rows[i]["configured_entry"] = CaptureMetadataFields(call[1], buildings[i].name + "/entry", missing);
                    }
                }
                finally { helperType.GetMethod("ClearConfiguredSpawnableDefinitions").Invoke(helper, new[] { destroy }); }
            }
            metadata["buildings"] = rows;
            var unitRows = new JArray();
            var transports = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var productionFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper").GetMethod("TryGetMetadata");
            var definitionFactory = resolverType.GetMethod("TryGetUnitDefinitionMetadata");
            foreach (var prefab in units)
            {
                object[] pa = { prefab, null }, da = { prefab, null };
                bool hd = (bool)definitionFactory.Invoke(null, da), hp = (bool)productionFactory.Invoke(null, pa);
                var row = new JObject { ["index"] = unitRows.Count, ["prefab"] = SourceAsset(prefab), ["has_definition_metadata"] = hd, ["has_production_metadata"] = hp };
                if (hd) row["definition_metadata"] = CaptureMetadataFields(da[1], prefab.name + "/definition", missing);
                if (hd || hp) row["authoring_catalog_properties"] = CaptureUnitCatalogAuthoringProperties(prefab, missing);
                if (hp)
                {
                    row["production_metadata"] = CaptureMetadataFields(pa[1], prefab.name + "/production", missing);
                    var transport = pa[1].GetType().GetField("ProductionTransportPrefab").GetValue(pa[1]) as GameObject;
                    if (transport != null) transports[AssetDatabase.GetAssetPath(transport)] = transport;
                }
                unitRows.Add(row);
            }
            metadata["units"] = unitRows;
            var transportRows = new JArray();
            foreach (var pair in transports.OrderBy(p => p.Key, StringComparer.Ordinal)) transportRows.Add(CaptureSourceTransportGraph(pair.Value, missing));
            metadata["transports"] = transportRows;
            if (missing.Count != 0) throw new InvalidOperationException("Unsupported metadata values: " + missing);
            return new JObject { ["catalog_id"] = expectedCatalog, ["difficulty"] = difficulty.ToString(), ["size"] = size.ToString(), ["seed"] = seed, ["setup_hash"] = setup.SetupHash, ["wrong_context_hash_rejected"] = wrongHashRejected, ["wrong_target_seed_rejected"] = wrongSeedRejected, ["selected_native_domain"] = new JObject { ["building_count"] = buildings.Count, ["unit_count"] = units.Count, ["building_keys"] = buildingKeys, ["unit_keys"] = unitKeys, ["recipe_count"] = recipes.Count, ["all_recipe_producers_and_units_in_selected_domain"] = true }, ["metadata_key_sha256"] = MetadataDigest(metadata), ["metadata"] = metadata };
        }

        static JArray CaptureUniqueCompiledDomain(IReadOnlyList<GameObject> prefabs, string context)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal); var names = new HashSet<string>(StringComparer.Ordinal); var result = new JArray();
            foreach (var prefab in prefabs)
            {
                if (prefab == null) throw new InvalidOperationException("Null original selected prefab: " + context + " count=" + prefabs.Count);
                JObject identity = SourceAsset(prefab);
                string key = (string)identity["guid"] + ":" + (string)identity["local_id"];
                if (string.IsNullOrEmpty((string)identity["guid"]) || ((long?)identity["local_id"] ?? 0) == 0 || string.IsNullOrEmpty(prefab.name) || !names.Add(prefab.name) || !keys.Add(key)) throw new InvalidOperationException("Duplicate/missing original selected prefab identity: " + context + " key=" + key + " count=" + prefabs.Count);
                result.Add(new JObject { ["index"] = result.Count, ["key"] = key, ["runtime_prefab_key"] = prefab.name, ["prefab"] = identity });
            }
            return result;
        }

        static void RequireTwoSeedContext(SkirmishResolvedSetup setup, int seed) => RequireCompiledMetadataContext(setup, seed, "S002", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard);

        static void RequireCompiledMetadataContext(SkirmishResolvedSetup setup, int seed, string expectedCatalog, SkirmishDifficultyId difficulty, SkirmishSizeId size)
        {
            if (setup == null || setup.CatalogId != expectedCatalog || setup.DifficultyId != difficulty || setup.SizeId != size || setup.Seed != seed || setup.SetupHash != SkirmishSetupCompiler.ComputeHash(setup, seed))
                throw new InvalidOperationException("Compiled target context identity mismatch");
        }

        static JArray CapturePrefabList(IEnumerable<GameObject> prefabs)
        {
            var rows = new JArray();
            foreach (var prefab in prefabs) rows.Add(prefab == null ? new JObject { ["codec"] = "null-v1" } : SourceAsset(prefab));
            return rows;
        }
        static void DestroyTwoSeedObject(UnityEngine.Object value) { if (value != null) UnityEngine.Object.DestroyImmediate(value); }
        static string MetadataDigest(JToken value)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value.ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
        }
        static void RequireMetadataEqual(JToken expected, JToken actual)
        {
            if (!JToken.DeepEquals(expected, actual) || MetadataDigest(expected) != MetadataDigest(actual))
                throw new InvalidOperationException("Metadata compatibility rejected at " + FirstTwoSeedDifference(expected, actual, "$"));
        }
        static string FirstTwoSeedDifference(JToken a, JToken b, string path)
        {
            if (JToken.DeepEquals(a, b)) return path;
            if (a is JObject oa && b is JObject ob)
                foreach (var property in oa.Properties())
                    if (!JToken.DeepEquals(property.Value, ob[property.Name])) return FirstTwoSeedDifference(property.Value, ob[property.Name], path + "/" + property.Name);
            if (a is JArray aa && b is JArray ab)
                for (int i = 0; i < Math.Min(aa.Count, ab.Count); i++)
                    if (!JToken.DeepEquals(aa[i], ab[i])) return FirstTwoSeedDifference(aa[i], ab[i], path + "/" + i);
            return path + " expected=" + (a?.ToString(Formatting.None) ?? "<missing>") + " actual=" + (b?.ToString(Formatting.None) ?? "<missing>");
        }

        static void RejectMetadataMutation(JObject first, JObject second, string name, Action<JToken> mutate, JArray checks)
        {
            JToken changed = second["metadata"].DeepClone(); mutate(changed);
            try { RequireMetadataEqual(first["metadata"], changed); }
            catch (InvalidOperationException) { checks.Add(name); return; }
            throw new InvalidOperationException("Negative metadata control accepted: " + name);
        }
    }
}
#endif
