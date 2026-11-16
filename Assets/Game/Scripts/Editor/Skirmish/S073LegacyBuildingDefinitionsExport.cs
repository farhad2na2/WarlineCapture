#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Captures the six S073 authored legacy definitions through the original
    /// BuildingDefinitionPrefabSystemHelper factory, without mission setup or recipe overlay.
    /// </summary>
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string OutputKey = "WARLINE_S073_LEGACY_BUILDING_DEFINITIONS_OUTPUT";
        const string PresetPath = "Assets/Game/Resources/SkirmishIndustrialBasin.asset";
        const string ConstructionPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset";
        const string RegistryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
        const string ExporterPath = "Assets/Game/Scripts/Editor/Skirmish/S073LegacyBuildingDefinitionsExport.cs";
        static readonly string[] ExpectedKeys =
        {
            "Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
            "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder"
        };

        public static void RunS073LegacyBuildingDefinitionsExport()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Idle Editor required for source-only definition capture");
                string output = Environment.GetEnvironmentVariable(OutputKey);
                if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                    throw new InvalidOperationException(OutputKey + " must be an absolute create-new JSON path");
                output = Path.GetFullPath(output);
                if (File.Exists(output)) throw new InvalidOperationException("Output exists; use a fresh evidence path");
                if (!Directory.Exists(Path.GetDirectoryName(output)))
                    throw new DirectoryNotFoundException("Output parent must already exist: " + Path.GetDirectoryName(output));

                var preset = AssetDatabase.LoadAssetAtPath<SkirmishPresetConfig>(PresetPath);
                var construction = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(ConstructionPath);
                var registry = AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(RegistryPath);
                Require(preset != null && AssetDatabase.GetAssetPath(preset) == PresetPath, "Exact IndustrialBasin preset required");
                Require(construction != null && AssetDatabase.GetAssetPath(construction) == ConstructionPath && preset.buildingPlacement == construction,
                    "S073 preset must reference exact IndustrialBasin Construction asset");
                Require(registry != null && AssetDatabase.GetAssetPath(registry) == RegistryPath && construction.UnitPrefabRegistryConfig == registry,
                    "Construction must reference exact UnitRegistry asset");
                Require(construction.Spawnables != null && construction.Spawnables.Count == ExpectedKeys.Length,
                    "Exactly six authored S073 building prefabs required");
                Require(registry.UnitSpawnPrefabs != null && registry.UnitSpawnPrefabs.Count > 0,
                    "Linked S073 unit registry must contain authored prefabs");
                RequireClean(preset, PresetPath); RequireClean(construction, ConstructionPath); RequireClean(registry, RegistryPath);

                var sourcePaths = new HashSet<string>(StringComparer.Ordinal);
                AddAssetClosure(PresetPath, sourcePaths);
                AddAssetClosure(ConstructionPath, sourcePaths);
                AddAssetClosure(RegistryPath, sourcePaths);
                foreach (GameObject prefab in construction.Spawnables.Concat(registry.UnitSpawnPrefabs))
                {
                    Require(prefab != null, "Missing authored building/unit prefab reference");
                    string path = AssetDatabase.GetAssetPath(prefab);
                    Require(!string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.Ordinal) && AssetDatabase.LoadAssetAtPath<GameObject>(path) == prefab,
                        "Catalog references must resolve to exact prefab roots");
                    RequireClean(prefab, path);
                    AddAssetClosure(path, sourcePaths);
                }
                var unitIdentities = new HashSet<string>(StringComparer.Ordinal);
                foreach (GameObject prefab in registry.UnitSpawnPrefabs)
                {
                    string path = AssetDatabase.GetAssetPath(prefab);
                    Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId) &&
                        guid == AssetDatabase.AssetPathToGUID(path) && unitIdentities.Add(guid + ":" + localId),
                        "Unit registry prefab identities must be known and unique: " + path);
                }

                // Pin the complete project script source set. The factory calls helpers in
                // multiple runtime/composition assemblies; pinning the set avoids an incomplete
                // hand-maintained transitive-code list while remaining strictly source-derived.
                foreach (string path in Directory.GetFiles("Assets/Game/Scripts", "*.cs", SearchOption.AllDirectories))
                {
                    string normalized = path.Replace('\\', '/');
                    sourcePaths.Add(normalized);
                    sourcePaths.Add(normalized + ".meta");
                }
                AddRequiredSource("Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs", sourcePaths);
                AddRequiredSource("Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs", sourcePaths);
                AddRequiredSource(ExporterPath, sourcePaths);

                JObject before = HashSources(sourcePaths);
                var orderedBuildings = construction.Spawnables.ToArray();
                var buildings = orderedBuildings.ToList();
                var units = registry.UnitSpawnPrefabs.ToList();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var rootRows = new JArray();
                for (int i = 0; i < orderedBuildings.Length; i++)
                {
                    GameObject prefab = orderedBuildings[i];
                    string path = AssetDatabase.GetAssetPath(prefab);
                    string key = prefab.name;
                    Require(key == ExpectedKeys[i], "Construction order/key differs at index " + i + ": " + key);
                    Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId) &&
                        guid == AssetDatabase.AssetPathToGUID(path) && seen.Add(guid + ":" + localId),
                        "Building prefab identity must be known and unique: " + path);
                    rootRows.Add(new JObject { ["construction_order"] = i, ["key"] = key, ["prefab"] = SourceAsset(prefab) });
                }

                Type helperType = Type.GetType("Game.Runtime.BuildingDefinitionPrefabSystemHelper, Game.Runtime", true);
                Type resolverType = Type.GetType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper, Game.Composition", true);
                MethodInfo configure = ExactMethod(helperType, "ConfigureAuthoringMetadataResolvers", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo buildingResolver = ExactMethod(resolverType, "TryGetBuildingDefinitionMetadata", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo unitResolver = ExactMethod(resolverType, "TryGetUnitDefinitionMetadata", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                Type[] parameters = configure.GetParameters().Select(p => p.ParameterType).ToArray();
                object helper = Activator.CreateInstance(helperType, true);
                configure.Invoke(helper, new[]
                {
                    Delegate.CreateDelegate(parameters[0], buildingResolver),
                    Delegate.CreateDelegate(parameters[1], unitResolver)
                });
                ExactMethod(helperType, "RebuildSpawnablesLookup", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(helper, new object[] { buildings, units });
                // Deliberately do not call ConfigureProductionRecipes. This is the original
                // legacy authored definition path, not a runtime mission recipe overlay.
                Require(ExactMethod(helperType, "CreateDefinition", BindingFlags.Public | BindingFlags.Instance).GetParameters().Length == 8,
                    "Expected original CreateDefinition factory signature");

                var definitions = new JArray();
                var unsupported = new JArray();
                var originalDefaultWorld = World.DefaultGameObjectInjectionWorld;
                using (var world = new World("S073LegacyAuthoredDefinitionMetadata", WorldFlags.Game))
                {
                    var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                    MethodInfo create = ExactMethod(helperType, "CreateDefinition", BindingFlags.Public | BindingFlags.Instance);
                    foreach (GameObject prefab in orderedBuildings)
                    {
                        var authoring = prefab.GetComponents<Component>().FirstOrDefault(c => c != null &&
                            c.GetType().FullName == "Game.Authoring.BuildingDefinitionAuthoring");
                        Require(authoring != null, "Authored building metadata component missing: " + prefab.name);
                        object definition = create.Invoke(helper, new object[] { prefab, prefab.name, "Operational building.", 500, null, null, null, runway });
                        Require(definition != null, "Original definition factory returned null: " + prefab.name);
                        Require(definition.GetType().FullName == "Game.Runtime.BuildingDefinition",
                            "Factory returned an unexpected definition type: " + definition.GetType().FullName);
                        var fields = new JObject();
                        foreach (FieldInfo field in definition.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                            fields[field.Name] = CaptureDefinitionValue(field.GetValue(definition), prefab.name + "/" + field.Name, unsupported, 0);
                        definitions.Add(new JObject
                        {
                            ["key"] = prefab.name,
                            ["prefab"] = SourceAsset(prefab),
                            ["definition_type"] = definition.GetType().FullName,
                            ["factory"] = "BuildingDefinitionPrefabSystemHelper.CreateDefinition",
                            ["fallback_arguments"] = new JObject
                            {
                                ["display_name"] = prefab.name, ["description"] = "Operational building.", ["max_health"] = 500,
                                ["primary_spawn_unit_prefab"] = null, ["secondary_spawn_unit_prefab"] = null,
                                ["tertiary_spawn_unit_prefab"] = null
                            },
                            ["fields"] = fields
                        });
                    }
                }
                Require(unsupported.Count == 0, "Every definition field must be captured by a supported codec; unsupported=" + unsupported.Count);
                Require(ReferenceEquals(originalDefaultWorld, World.DefaultGameObjectInjectionWorld), "Default World changed");
                foreach (GameObject prefab in orderedBuildings.Concat(registry.UnitSpawnPrefabs)) RequireClean(prefab, AssetDatabase.GetAssetPath(prefab));
                RequireClean(preset, PresetPath); RequireClean(construction, ConstructionPath); RequireClean(registry, RegistryPath);
                JObject after = HashSources(sourcePaths);
                Require(JToken.DeepEquals(before, after), "A pinned source changed during export");

                var report = new JObject
                {
                    ["schema_version"] = 1,
                    ["kind"] = "S073LegacyAuthoredBuildingDefinitions",
                    ["authority"] = "OriginalLegacyPresetConstruction_NoRuntimeRecipeOverlay",
                    ["catalog_id"] = "S073",
                    ["preset_path"] = PresetPath,
                    ["building_catalog"] = ConstructionPath,
                    ["unit_catalog"] = RegistryPath,
                    ["building_order"] = rootRows,
                    ["definitions"] = definitions,
                    ["factory"] = "BuildingDefinitionPrefabSystemHelper.CreateDefinition",
                    ["production_recipe_overlay_configured"] = false,
                    ["mission_world_loaded"] = false,
                    ["mission_initialized"] = false,
                    ["native_readiness"] = false,
                    ["unsupported_values"] = unsupported,
                    ["source_pins"] = after
                };
                using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(file)) writer.Write(report.ToString(Formatting.Indented) + "\n");
                Debug.Log("[S073LegacyAuthoredBuildingDefinitions] result=Passed definitions=" + definitions.Count +
                    " unsupported=" + unsupported.Count + " recipeOverlay=0 mission=0 readiness=0 output=" + output);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S073LegacyAuthoredBuildingDefinitions] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        static void AddAssetClosure(string assetPath, HashSet<string> paths)
        {
            paths.Add(assetPath); paths.Add(assetPath + ".meta");
            foreach (string dependency in AssetDatabase.GetDependencies(assetPath, true))
                if (dependency.StartsWith("Assets/", StringComparison.Ordinal)) { paths.Add(dependency); paths.Add(dependency + ".meta"); }
        }
        static void AddRequiredSource(string path, HashSet<string> paths)
        {
            if (!File.Exists(path) || !File.Exists(path + ".meta")) throw new FileNotFoundException("Required pinned source missing", path);
            paths.Add(path); paths.Add(path + ".meta");
        }
        static JObject HashSources(IEnumerable<string> paths)
        {
            var result = new JObject();
            foreach (string path in paths.OrderBy(p => p, StringComparer.Ordinal))
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Pinned source or metadata missing", path);
                using (var sha = SHA256.Create()) result[path] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
            }
            return result;
        }
        static void RequireClean(UnityEngine.Object asset, string path)
        {
            if (asset == null || EditorUtility.IsDirty(asset)) throw new InvalidOperationException("Authored source must be clean: " + path);
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
        static MethodInfo ExactMethod(Type type, string name, BindingFlags flags) =>
            type.GetMethods(flags).SingleOrDefault(m => m.Name == name) ?? throw new MissingMethodException(type.FullName, name);
    }
}
#endif
