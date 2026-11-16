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
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Exports the effective S073 configured unit catalog without loading a mission world.</summary>
    public static class S073ConfiguredUnitCatalogExport
    {
        private const string OutputKey = "WARLINE_S073_UNIT_CATALOG_OUTPUT";
        private const string PresetPath = "Assets/Game/Resources/SkirmishIndustrialBasin.asset";
        private const string ConstructionPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset";
        private const string RegistryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";

        public static void RunS073ConfiguredUnitCatalogExport()
        {
            var prefabRecords = new List<PrefabRecord>();
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Idle Editor required for isolated prefab unit catalog export");
                string output = Environment.GetEnvironmentVariable(OutputKey);
                if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                    throw new InvalidOperationException(OutputKey + " must name an absolute create-new JSON path");
                output = Path.GetFullPath(output);
                if (File.Exists(output)) throw new InvalidOperationException("Output already exists; create-new export required");

                var preset = AssetDatabase.LoadAssetAtPath<SkirmishPresetConfig>(PresetPath);
                var construction = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(ConstructionPath);
                var registry = AssetDatabase.LoadAssetAtPath<UnitPrefabRegistryAuthoringConfig>(RegistryPath);
                if (preset == null || AssetDatabase.GetAssetPath(preset) != PresetPath)
                    throw new InvalidDataException("Exact S073 IndustrialBasin preset is unavailable");
                if (construction == null || AssetDatabase.GetAssetPath(construction) != ConstructionPath || preset.buildingPlacement != construction)
                    throw new InvalidDataException("S073 preset does not reference the exact IndustrialBasin Construction.asset");
                if (registry == null || AssetDatabase.GetAssetPath(registry) != RegistryPath || construction.UnitPrefabRegistryConfig != registry)
                    throw new InvalidDataException("IndustrialBasin Construction.asset does not reference the exact UnitRegistry.asset");
                RequireClean(preset, PresetPath);
                RequireClean(construction, ConstructionPath);
                RequireClean(registry, RegistryPath);
                if (construction.Spawnables == null || construction.Spawnables.Count != 6)
                    throw new InvalidDataException("Expected six authored construction buildings");
                if (registry.UnitSpawnPrefabs == null || registry.UnitSpawnPrefabs.Count == 0)
                    throw new InvalidDataException("Exact S073 unit registry has no configured unit entries");

                Type helperType = Type.GetType("Game.Runtime.BuildingDefinitionPrefabSystemHelper, Game.Runtime", true);
                Type resolverType = Type.GetType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper, Game.Composition", true);
                MethodInfo buildingResolver = FindStatic(resolverType, "TryGetBuildingDefinitionMetadata");
                MethodInfo unitResolver = FindStatic(resolverType, "TryGetUnitDefinitionMetadata");
                Type buildingDelegate = helperType.GetNestedType("TryGetBuildingDefinitionMetadataDelegate", BindingFlags.Public);
                Type unitDelegate = helperType.GetNestedType("TryGetUnitDefinitionMetadataDelegate", BindingFlags.Public);
                if (buildingDelegate == null || unitDelegate == null) throw new MissingMemberException(helperType.FullName, "authoring metadata delegates");
                object helper = Activator.CreateInstance(helperType, true);
                MethodInfo configure = helperType.GetMethod("ConfigureAuthoringMetadataResolvers", BindingFlags.Public | BindingFlags.Instance);
                configure.Invoke(helper, new[] { Delegate.CreateDelegate(buildingDelegate, buildingResolver), Delegate.CreateDelegate(unitDelegate, unitResolver) });

                var sourcePaths = new HashSet<string>(StringComparer.Ordinal)
                {
                    PresetPath, PresetPath + ".meta", ConstructionPath, ConstructionPath + ".meta", RegistryPath, RegistryPath + ".meta",
                    "Assets/Game/Scripts/Editor/Skirmish/S073ConfiguredUnitCatalogExport.cs",
                    "Assets/Game/Scripts/Editor/Skirmish/S073ConfiguredUnitCatalogExport.cs.meta",
                    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
                    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs.meta",
                    "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs",
                    "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs.meta",
                    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
                    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs.meta",
                    "Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs",
                    "Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs.meta",
                    "Assets/Game/Scripts/Systems/BuildingUiCommandSystemHelper.cs",
                    "Assets/Game/Scripts/Systems/BuildingUiCommandSystemHelper.cs.meta"
                };
                AddDependencies(PresetPath, sourcePaths);
                AddDependencies(ConstructionPath, sourcePaths);
                AddDependencies(RegistryPath, sourcePaths);
                foreach (GameObject authored in construction.Spawnables.Concat(registry.UnitSpawnPrefabs))
                {
                    if (authored == null) throw new InvalidDataException("Missing authored prefab reference");
                    string path = AssetDatabase.GetAssetPath(authored);
                    if (string.IsNullOrEmpty(path)) throw new InvalidDataException("Authored prefab asset path required");
                    RequireClean(authored, path);
                    sourcePaths.Add(path); sourcePaths.Add(path + ".meta"); AddDependencies(path, sourcePaths);
                }
                JObject initialHashes = HashSources(sourcePaths);
                var buildings = LoadCatalog(construction.Spawnables, "building", prefabRecords, sourcePaths);
                var units = LoadCatalog(registry.UnitSpawnPrefabs, "unit", prefabRecords, sourcePaths);

                MethodInfo rebuild = helperType.GetMethod("RebuildSpawnablesLookup", BindingFlags.Public | BindingFlags.Instance);
                if (rebuild == null) throw new MissingMethodException(helperType.FullName, "RebuildSpawnablesLookup");
                rebuild.Invoke(helper, new object[] { buildings, units });
                MethodInfo readModel = helperType.GetMethod("TryGetConfiguredUnitReadModel", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo costs = helperType.GetMethod("TryResolveConfiguredUnitResourceCosts", BindingFlags.Public | BindingFlags.Instance);
                if (readModel == null || costs == null) throw new MissingMethodException(helperType.FullName, "configured unit projection/resource cost methods");

                var rows = new JArray();
                var buildingRows = new JArray();
                foreach (PrefabRecord record in prefabRecords.Where(item => item.Kind == "building"))
                {
                    buildingRows.Add(new JObject
                    {
                        ["construction_order"] = buildingRows.Count,
                        ["prefab_name"] = record.Isolated.name,
                        ["asset_path"] = record.Path,
                        ["guid"] = record.Guid,
                        ["local_file_id"] = record.LocalId
                    });
                }
                for (int i = 0; i < units.Count; i++)
                {
                    object[] modelArgs = { i, null, null, 0, false, false };
                    if (!(bool)readModel.Invoke(helper, modelArgs)) throw new InvalidDataException("Configured unit read model failed at registry index " + i);
                    var prefab = (GameObject)modelArgs[1];
                    string displayName = (string)modelArgs[2];
                    int displayedPrice = (int)modelArgs[3];
                    bool canRequest = (bool)modelArgs[4];
                    bool isVehicle = (bool)modelArgs[5];
                    object[] costArgs = { prefab, displayedPrice, 0, 0 };
                    if (!(bool)costs.Invoke(helper, costArgs)) throw new InvalidDataException("Configured resource costs unavailable at registry index " + i);
                    PrefabRecord record = prefabRecords.Single(item => item.Kind == "unit" && item.Isolated == prefab);
                    rows.Add(new JObject
                    {
                        ["registry_order"] = i,
                        ["prefab_name"] = prefab.name,
                        ["display_name"] = displayName,
                        ["asset_path"] = record.Path,
                        ["guid"] = record.Guid,
                        ["local_file_id"] = record.LocalId,
                        ["can_request"] = canRequest,
                        ["is_vehicle"] = isVehicle,
                        ["displayed_price"] = displayedPrice,
                        ["credits_cost"] = (int)costArgs[2],
                        ["materials_cost"] = (int)costArgs[3]
                    });
                }

                foreach (PrefabRecord record in prefabRecords) RequireClean(record.Authored, record.Path);
                RequireClean(preset, PresetPath);
                RequireClean(construction, ConstructionPath);
                RequireClean(registry, RegistryPath);
                JObject finalHashes = HashSources(sourcePaths);
                if (!JToken.DeepEquals(initialHashes, finalHashes)) throw new InvalidOperationException("Authored source/dependency files changed during catalog export");
                var result = new JObject
                {
                    ["kind"] = "S073ConfiguredUnitCatalogExport",
                    ["schema_version"] = 1,
                    ["scope"] = "ConfiguredUnitCatalogOnly_NoMissionWorldOrReadiness",
                    ["mission"] = "S073",
                    ["preset_asset_path"] = PresetPath,
                    ["building_placement_source_path"] = ConstructionPath,
                    ["unit_registry_source_path"] = RegistryPath,
                    ["source_dependencies_sha256"] = finalHashes,
                    ["configured_building_count"] = buildings.Count,
                    ["buildings"] = buildingRows,
                    ["entries"] = rows,
                    ["mission_world_loaded"] = false,
                    ["native_readiness"] = false
                };
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                Debug.Log("[S073UnitCatalogExport] result=Passed entries=" + rows.Count + " path=" + output);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S073UnitCatalogExport] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
            finally
            {
                for (int i = prefabRecords.Count - 1; i >= 0; i--)
                {
                    GameObject isolated = prefabRecords[i].Isolated;
                    if (isolated != null) PrefabUtility.UnloadPrefabContents(isolated);
                }
            }
        }

        private sealed class PrefabRecord
        {
            public string Kind, Path, Guid;
            public long LocalId;
            public GameObject Authored, Isolated;
        }

        private static List<GameObject> LoadCatalog(IReadOnlyList<GameObject> authoredList, string kind, List<PrefabRecord> records, HashSet<string> paths)
        {
            var loaded = new List<GameObject>(authoredList.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameObject authored in authoredList)
            {
                if (authored == null) throw new InvalidDataException("Missing " + kind + " prefab reference");
                string path = AssetDatabase.GetAssetPath(authored);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.Ordinal) || AssetDatabase.LoadAssetAtPath<GameObject>(path) != authored)
                    throw new InvalidDataException(kind + " reference is not the prefab asset root: " + authored.name);
                RequireClean(authored, path);
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(authored, out string objectGuid, out long localId) || guid.Length != 32 || objectGuid != guid)
                    throw new InvalidDataException("Prefab GUID/local file identity unavailable: " + path);
                if (!seen.Add(guid + ":" + localId)) throw new InvalidDataException("Duplicate prefab identity in " + kind + " catalog: " + path);
                paths.Add(path); paths.Add(path + ".meta"); AddDependencies(path, paths);
                GameObject isolated = PrefabUtility.LoadPrefabContents(path);
                if (isolated == null) throw new InvalidDataException("Could not load isolated prefab contents: " + path);
                records.Add(new PrefabRecord { Kind = kind, Path = path, Guid = guid, LocalId = localId, Authored = authored, Isolated = isolated });
                loaded.Add(isolated);
            }
            return loaded;
        }

        private static MethodInfo FindStatic(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException(type.FullName, name);
        private static void AddDependencies(string assetPath, HashSet<string> paths)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(assetPath, true))
                if (dependency.StartsWith("Assets/", StringComparison.Ordinal)) { paths.Add(dependency); paths.Add(dependency + ".meta"); }
        }
        private static JObject HashSources(IEnumerable<string> sourcePaths)
        {
            var hashes = new JObject();
            foreach (string path in sourcePaths.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Required source/dependency or .meta missing", path);
                using (var sha = SHA256.Create()) hashes[path] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
            }
            return hashes;
        }
        private static void RequireClean(UnityEngine.Object asset, string path)
        {
            if (asset == null || EditorUtility.IsDirty(asset)) throw new InvalidOperationException("Relevant authored asset must remain clean: " + path);
        }
    }
}
#endif
