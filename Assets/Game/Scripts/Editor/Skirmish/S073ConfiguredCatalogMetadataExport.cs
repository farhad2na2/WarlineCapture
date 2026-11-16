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
    /// <summary>Exports authored catalog metadata only; it does not load a mission world.</summary>
    public static class S073ConfiguredCatalogMetadataExport
    {
        private const string OutputKey = "WARLINE_S073_CATALOG_METADATA_OUTPUT";
        private const string PresetPath = "Assets/Game/Resources/SkirmishIndustrialBasin.asset";
        private const string ConstructionPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset";

        public static void RunS073ConfiguredCatalogMetadataExport()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Idle Editor required for isolated prefab metadata export");
                string output = Environment.GetEnvironmentVariable(OutputKey);
                if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output))
                    throw new InvalidOperationException(OutputKey + " must name an absolute create-new JSON path");
                output = Path.GetFullPath(output);
                if (File.Exists(output)) throw new InvalidOperationException("Output already exists; create-new export required");

                var preset = AssetDatabase.LoadAssetAtPath<SkirmishPresetConfig>(PresetPath);
                if (preset == null || AssetDatabase.GetAssetPath(preset) != PresetPath)
                    throw new InvalidDataException("Exact S073 IndustrialBasin preset is unavailable");
                var placement = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(ConstructionPath);
                if (placement == null || AssetDatabase.GetAssetPath(placement) != ConstructionPath)
                    throw new InvalidDataException("Exact authored Construction.asset is unavailable");
                if (preset.buildingPlacement != placement)
                    throw new InvalidDataException("S073 preset BuildingPlacement does not reference the exact IndustrialBasin Construction.asset");
                RequireClean(preset, PresetPath);
                RequireClean(placement, ConstructionPath);

                var spawnables = placement.Spawnables;
                if (spawnables == null || spawnables.Count != 6)
                    throw new InvalidDataException("Expected exactly six authored Construction spawnables; found " + (spawnables == null ? "null" : spawnables.Count.ToString()));

                Type helper = Type.GetType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper, Game.Composition", true);
                MethodInfo resolver = helper.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (resolver == null) throw new MissingMethodException(helper.FullName, "TryGetBuildingDefinitionMetadata");
                var rows = new JArray();
                var identities = new HashSet<string>(StringComparer.Ordinal);
                var sourcePaths = new HashSet<string>(StringComparer.Ordinal)
                {
                    PresetPath, PresetPath + ".meta", ConstructionPath, ConstructionPath + ".meta",
                    "Assets/Game/Scripts/Editor/Skirmish/S073ConfiguredCatalogMetadataExport.cs",
                    "Assets/Game/Scripts/Editor/Skirmish/S073ConfiguredCatalogMetadataExport.cs.meta",
                    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
                    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs.meta",
                    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
                    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs.meta"
                };
                AddDependencies(ConstructionPath, sourcePaths);
                AddDependencies(PresetPath, sourcePaths);
                foreach (GameObject authored in spawnables)
                {
                    if (authored == null) throw new InvalidDataException("Construction spawnables contains a missing prefab reference");
                    string assetPath = AssetDatabase.GetAssetPath(authored);
                    if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab", StringComparison.Ordinal))
                        throw new InvalidDataException("Spawnable is not an unambiguous prefab asset: " + authored.name);
                    RequireClean(authored, assetPath);
                    sourcePaths.Add(assetPath);
                    sourcePaths.Add(assetPath + ".meta");
                    AddDependencies(assetPath, sourcePaths);
                }
                JObject initialHashes = HashSources(sourcePaths);
                foreach (GameObject authored in spawnables)
                {
                    if (authored == null) throw new InvalidDataException("Construction spawnables contains a missing prefab reference");
                    string prefabPath = AssetDatabase.GetAssetPath(authored);
                    if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.Ordinal))
                        throw new InvalidDataException("Spawnable is not an unambiguous prefab asset: " + authored.name);
                    string guid = AssetDatabase.AssetPathToGUID(prefabPath);
                    if (guid.Length != 32 || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != authored)
                        throw new InvalidDataException("Referenced object is not the prefab asset root: " + prefabPath);
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(authored, out string objectGuid, out long localId) || objectGuid != guid)
                        throw new InvalidDataException("Prefab GUID/local file identity could not be resolved: " + prefabPath);
                    string identity = guid + ":" + localId;
                    if (!identities.Add(identity)) throw new InvalidDataException("Duplicate prefab identity in Construction catalog: " + identity);

                    GameObject isolated = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (isolated == null) throw new InvalidDataException("Prefab contents failed to load: " + prefabPath);
                    try
                    {
                        object[] args = { isolated, null };
                        if (!(bool)resolver.Invoke(null, args) || args[1] == null)
                            throw new InvalidDataException("Effective BuildingDefinition metadata missing: " + prefabPath);
                        object metadata = args[1];
                        bool canRequest = Read<bool>(metadata, "CanRequest");
                        int price = Read<int>(metadata, "Price");
                        int materialsCost = Read<int>(metadata, "MaterialsCost");
                        Vector2Int footprintCells = Read<Vector2Int>(metadata, "FootprintCells");
                        if (footprintCells.x <= 0 || footprintCells.y <= 0)
                            throw new InvalidDataException("Effective BuildingDefinition footprint must be positive: " + prefabPath);
                        rows.Add(new JObject
                        {
                            ["prefab_name"] = isolated.name,
                            ["asset_path"] = prefabPath,
                            ["guid"] = guid,
                            ["local_file_id"] = localId,
                            ["effective_can_request"] = canRequest,
                            ["effective_price"] = price,
                            ["effective_materials_cost"] = materialsCost,
                            ["effective_footprint_cells"] = new JObject
                            {
                                ["x"] = footprintCells.x,
                                ["y"] = footprintCells.y
                            }
                        });
                    }
                    finally { PrefabUtility.UnloadPrefabContents(isolated); }
                }

                RequireClean(preset, PresetPath);
                RequireClean(placement, ConstructionPath);
                foreach (GameObject authored in spawnables) RequireClean(authored, AssetDatabase.GetAssetPath(authored));
                JObject finalHashes = HashSources(sourcePaths);
                if (!JToken.DeepEquals(initialHashes, finalHashes))
                    throw new InvalidOperationException("Authored source/dependency files changed during metadata export");
                var result = new JObject
                {
                    ["kind"] = "S073ConfiguredCatalogMetadataExport",
                    ["schema_version"] = 1,
                    ["scope"] = "AuthoredConstructionCatalogMetadataOnly_NoMissionWorldOrNativeReadiness",
                    ["mission"] = "S073",
                    ["preset_asset_path"] = PresetPath,
                    ["building_placement_source_path"] = ConstructionPath,
                    ["footprint_authority"] = "OriginalBuildingDefinitionMetadata.FootprintCells",
                    ["source_dependencies_sha256"] = finalHashes,
                    ["entries"] = rows,
                    ["mission_world_loaded"] = false,
                    ["native_readiness"] = false
                };
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
                Debug.Log("[S073CatalogMetadataExport] result=Passed entries=" + rows.Count +
                    " footprints=" + rows.Count + " path=" + output);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S073CatalogMetadataExport] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static void AddDependencies(string assetPath, HashSet<string> paths)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(assetPath, true))
            {
                if (!dependency.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                paths.Add(dependency);
                paths.Add(dependency + ".meta");
            }
        }

        private static JObject HashSources(IEnumerable<string> sourcePaths)
        {
            var hashes = new JObject();
            foreach (string path in sourcePaths.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Required source/dependency file or .meta missing", path);
                hashes[path] = Sha256(path);
            }
            return hashes;
        }

        private static void RequireClean(UnityEngine.Object asset, string path)
        {
            if (asset == null || EditorUtility.IsDirty(asset))
                throw new InvalidOperationException("Relevant authored asset must be clean before and after export: " + path);
        }

        private static T Read<T>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.FieldType != typeof(T)) throw new MissingFieldException(instance.GetType().FullName, fieldName);
            return (T)field.GetValue(instance);
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
