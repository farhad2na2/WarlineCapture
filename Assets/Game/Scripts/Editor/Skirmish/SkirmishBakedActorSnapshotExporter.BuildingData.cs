#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        static readonly string[] StartingBuildingKeys = { "Building_Barrack", "Building_GroundStaging", "Building_OilPump",
            "Building_Refinery", "Building_Fuel_Bladder", "Building_Ammunition_Depot", "Building_GuardTower", "Building_Satelite_Dish" };

        // Source metadata through the ORIGINAL definition factory. No player build,
        // mission, native instance creation, production tick or outcome is involved.
        public static void RunBuildingReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-skirmishBuildingReferenceData");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit new output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string catalogId = BuildingReferenceArgument(args, "-skirmishBuildingReferenceCatalog", "S002");
            string seedText = BuildingReferenceArgument(args, "-skirmishBuildingReferenceSeed", "104731");
            if (!int.TryParse(seedText, out int seed)) throw new ArgumentException("Explicit integer source seed required");
            string keyText = BuildingReferenceArgument(args, "-skirmishBuildingReferenceKeys", null);
            string[] keys = keyText == null ? StartingBuildingKeys : keyText.Split(',').Select(k => k.Trim()).ToArray();
            if (keys.Length < 1 || keys.Length > 32 || keys.Any(k => string.IsNullOrEmpty(k) || k.Length > 128) || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new ArgumentException("Unique bounded configured building keys required");
            bool captureGeometry = Array.IndexOf(args, "-skirmishBuildingReferenceGeometry") >= 0;
            // TryQueueExpanded in SkirmishLaunchProjection loads the shared MAP
            // preset (index0), even when the S002 scenario selection is index4.
            var preset = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            if (preset == null || preset.buildingPlacement == null) throw new InvalidOperationException("Actual S002 placement preset missing");
            string buildingCatalog = AssetDatabase.GetAssetPath(preset.buildingPlacement);
            if (string.IsNullOrEmpty(buildingCatalog)) throw new InvalidOperationException("S002 placement preset source path missing");
            // Match.unity's actual serialized buildingPlacementConfig reference.
            string sceneCatalog = AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004");
            var sceneConfig = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(sceneCatalog);
            if (sceneConfig == null || sceneConfig.UnitPrefabRegistryConfig == null) throw new InvalidOperationException("Actual Match scene catalog missing");
            string unitCatalog = AssetDatabase.GetAssetPath(sceneConfig.UnitPrefabRegistryConfig);
            if (string.IsNullOrEmpty(unitCatalog)) throw new InvalidOperationException("Actual S002 unit registry source missing");
            var expansion = SkirmishExpansionCatalogConfig.Load();
            var definitionSource = expansion?.Definitions.FirstOrDefault(d => d != null && d.CatalogId == catalogId);
            if (definitionSource == null) throw new InvalidOperationException("Actual S002 scenario definition missing");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError)) throw new InvalidOperationException(matrixError);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definitionSource.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definitionSource, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, seed,
                manifest, matrix, out var setup, out var reasons)) throw new InvalidOperationException("Original S002 compiler rejected source: " + string.Join(";", reasons));
            // The actual native composition merges baseline+scene source prefabs and
            // compiles mission recipes. Export that overlay, not either input alone.
            using var production = SkirmishProductionCatalog.Create(setup, preset.buildingPlacement, sceneConfig);
            var pins = SourcePins(buildingCatalog, sceneCatalog, unitCatalog, AssetDatabase.GetAssetPath(expansion), AssetDatabase.GetAssetPath(definitionSource), "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceGeometry.cs");
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var buildings = new List<GameObject>(production.Config.Spawnables);
            var units = new List<GameObject>(production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs);
            Type helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            Type resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true);
            MethodInfo configure = helperType.GetMethod("ConfigureAuthoringMetadataResolvers");
            var parameters = configure.GetParameters();
            object buildingResolver = Delegate.CreateDelegate(parameters[0].ParameterType,
                resolverType.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            object unitResolver = Delegate.CreateDelegate(parameters[1].ParameterType,
                resolverType.GetMethod("TryGetUnitDefinitionMetadata", BindingFlags.Static | BindingFlags.Public));
            configure.Invoke(helper, new[] { buildingResolver, unitResolver });
            helperType.GetMethod("RebuildSpawnablesLookup").Invoke(helper, new object[] { buildings, units });
            helperType.GetMethod("ConfigureProductionRecipes").Invoke(helper, new object[] { production.Config.RuntimeProductionRecipes });
            var rows = new JArray(); var missing = new JArray();
            using (var world = new World("SourceBuildingDefinitionMetadata", WorldFlags.Game))
            {
                var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                var create = helperType.GetMethod("CreateRuntimeBuildingDefinition");
                foreach (string key in keys)
                {
                    var resolveArgs = new object[] { key, null };
                    if (!(bool)helperType.GetMethod("TryResolveConfiguredSpawnablePrefab").Invoke(helper, resolveArgs))
                        throw new InvalidOperationException("Original configured building resolver rejected: " + key);
                    GameObject prefab = (GameObject)resolveArgs[1];
                    var sourceAuthoring = prefab.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().FullName == "Game.Authoring.BuildingDefinitionAuthoring");
                    if (sourceAuthoring == null) throw new InvalidOperationException("Actual building authoring metadata absent: " + key);
                    object definition = create.Invoke(helper, new object[] { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, runway });
                    if (definition == null) throw new InvalidOperationException("Original factory returned no definition");
                    var fields = new JObject();
                    foreach (var field in definition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        fields[field.Name] = CaptureDefinitionValue(field.GetValue(definition), key + "/" + field.Name, missing, 0);
                    var row = new JObject { ["key"] = key, ["prefab"] = SourceAsset(prefab), ["definition_type"] = definition.GetType().FullName,
                        ["factory"] = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition", ["fields"] = fields };
                    if (captureGeometry) row["evaluated_geometry"] = CaptureBuildingReferenceGeometry(prefab, sourceAuthoring);
                    rows.Add(row);
                }
            }
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld)) throw new InvalidOperationException("Default World changed");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source changed during metadata export: " + pin.Key);
            var report = new JObject { ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishBuildingDefinitions",
                ["authority"] = "OriginalConfiguredPrefabMetadataFactory_NotMissionState", ["definitions"] = rows,
                ["building_catalog"] = buildingCatalog, ["scene_catalog"] = sceneCatalog, ["unit_catalog"] = unitCatalog,
                ["setup_hash"] = setup.SetupHash, ["seed"] = setup.Seed, ["catalog_id"] = catalogId, ["requested_keys"] = JArray.FromObject(keys),
                ["source_pins"] = JObject.FromObject(pins), ["unsupported_values"] = missing,
                ["full_mission_initialized"] = false, ["native_visual_readiness"] = false };
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "building-definitions.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishAuthoredBuildingDefinitions] result=Passed definitions=" + rows.Count + " unsupported=" + missing.Count + " fullMission=0");
        }

        static Type LoadedType(string name)
        {
            var matches = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).Where(t => t != null).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Exact loaded source type required: " + name);
            return matches[0];
        }
        static JObject SourceAsset(UnityEngine.Object value)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id))
                throw new InvalidOperationException("Source asset identity unresolved: " + value.name);
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Asset source path missing");
            return new JObject { ["codec"] = "source-asset-reference-v1", ["type"] = value.GetType().FullName,
                ["key"] = value.name, ["path"] = path, ["guid"] = guid, ["local_id"] = id };
        }
        static JToken CaptureDefinitionValue(object value, string path, JArray missing, int depth)
        {
            if (depth > 8) throw new InvalidOperationException("Definition nesting bound exceeded");
            if (value == null) return new JObject { ["codec"] = "null-v1" };
            // Unity's unassigned optional asset handles are CLR-nonnull but compare
            // equal to Unity null. Preserve the same null branch as native owners.
            if (value is UnityEngine.Object asset)
                return asset == null ? new JObject { ["codec"] = "null-v1" } : SourceAsset(asset);
            Type type = value.GetType();
            if (type.IsValueType || value is string) return S002InitialWorldEntityExport.CaptureValue(value, path, missing);
            if (value is IEnumerable sequence)
            {
                var items = new JArray();
                foreach (object item in sequence)
                {
                    if (items.Count >= 4096) throw new InvalidOperationException("Definition list bound exceeded");
                    items.Add(CaptureDefinitionValue(item, path + "/" + items.Count, missing, depth + 1));
                }
                return new JObject { ["codec"] = "definition-list-v1", ["items"] = items };
            }
            var fields = new JObject();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                fields[field.Name] = CaptureDefinitionValue(field.GetValue(value), path + "/" + field.Name, missing, depth + 1);
            return new JObject { ["codec"] = "definition-fields-v1", ["type"] = type.FullName, ["fields"] = fields };
        }
    }
}
#endif
