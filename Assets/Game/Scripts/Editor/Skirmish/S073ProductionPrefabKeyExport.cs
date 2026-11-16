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
    /// <summary>
    /// Derives the exact S073 startup and production prefab key set from current authored references.
    /// This reads metadata and asset identities only; it does not initialize a mission world.
    /// </summary>
    public static class S073ProductionPrefabKeyExport
    {
        private const string OutputKey = "-skirmishS073ProductionPrefabKeys";
        private const string AuditPathKey = "-skirmishS073AssetAudit";
        private const string AuditShaKey = "-skirmishS073AssetAuditSha";
        private const string PresetPath = "Assets/Game/Resources/SkirmishIndustrialBasin.asset";
        private const string ConstructionPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset";
        private const string InitialForcesPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset";
        private const string UnitRegistryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
        private static readonly string[] RequiredProductionRoots =
        {
            "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy", "Unit_Veh_APC_Slow",
            "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air", "Unit_Veh_Missle_Launcher_Ground",
            "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA", "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker",
            "Unit_Veh_Truck_Tray"
        };

        private sealed class PrefabIdentity
        {
            public string Name;
            public string Path;
            public string Guid;
            public long LocalId;
            public string AssetSha;
            public string MetaSha;
        }

        public static void RunS073ProductionPrefabKeyExport()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Idle Editor required for source-only key derivation");
                string outputPath = Path.GetFullPath(Arg(OutputKey));
                string auditPath = Path.GetFullPath(Arg(AuditPathKey));
                string auditSha = Arg(AuditShaKey);
                string provenancePath = outputPath + ".provenance.json";
                RequireFreshAbsolute(outputPath);
                RequireFreshAbsolute(provenancePath);
                if (!File.Exists(auditPath) || !IsSha(auditSha) || Sha256(auditPath) != auditSha)
                    throw new InvalidDataException("Current hash-pinned S073 YAML asset audit required");

                JObject audit = JObject.Parse(File.ReadAllText(auditPath), new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if ((int?)audit["schema_version"] != 1 ||
                    (string)audit["kind"] != "S073LegacySourceAssetReferenceAudit" ||
                    (string)audit["authority"] != "Current Unity YAML references resolved by exact current .meta GUIDs; data only" ||
                    (string)audit["mission"] != "S073" ||
                    audit["configuration_roots"] is not JArray configRoots ||
                    audit["references"] is not JArray references)
                    throw new InvalidDataException("Exact S073 source asset audit required");

                var sourcePins = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (JObject root in configRoots.OfType<JObject>())
                {
                    string path = (string)root["path"], sha = (string)root["sha256"];
                    PinCurrentAssetPath(path, sha, sourcePins);
                }
                if (!sourcePins.ContainsKey(InitialForcesPath) || !sourcePins.ContainsKey(UnitRegistryPath))
                    throw new InvalidDataException("S073 InitialForces and UnitRegistry audit roots are required");

                var auditRows = new Dictionary<string, JObject>(StringComparer.Ordinal);
                foreach (JObject reference in references.OfType<JObject>())
                {
                    string path = (string)reference["asset_path"], sha = (string)reference["asset_sha256"];
                    string metaPath = (string)reference["meta_path"], metaSha = (string)reference["meta_sha256"];
                    if ((bool?)reference["exists"] != true || path == null || sha == null || metaPath == null || metaSha == null ||
                        Sha256(path) != sha || Sha256(metaPath) != metaSha)
                        throw new InvalidDataException("Complete current S073 reference inventory required: " + path);
                    if (!auditRows.TryAdd((string)reference["guid"], reference))
                        throw new InvalidDataException("Duplicate GUID in S073 source audit");
                    sourcePins[path] = sha;
                    sourcePins[metaPath] = metaSha;
                }

                var startupRows = auditRows.Values
                    .Where(row => IsStartupUse(row["uses"] as JArray))
                    .Where(row => ((string)row["asset_path"])?.EndsWith(".prefab", StringComparison.Ordinal) == true)
                    .OrderBy(row => (string)row["guid"], StringComparer.Ordinal)
                    .ToArray();
                if (startupRows.Length != 4 || startupRows.Select(row => (string)row["guid"]).Distinct(StringComparer.Ordinal).Count() != 4)
                    throw new InvalidDataException("Exactly four current audited S073 startup prefab references are required");

                var preset = AssetDatabase.LoadAssetAtPath<SkirmishPresetConfig>(PresetPath);
                var placement = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(ConstructionPath);
                if (preset == null || placement == null || preset.buildingPlacement != placement ||
                    AssetDatabase.GetAssetPath(preset) != PresetPath || AssetDatabase.GetAssetPath(placement) != ConstructionPath)
                    throw new InvalidDataException("Exact S073 preset to IndustrialBasin Construction reference required");
                RequireClean(preset, PresetPath);
                RequireClean(placement, ConstructionPath);
                PinAssetAndDependencies(PresetPath, sourcePins);
                PinAssetAndDependencies(ConstructionPath, sourcePins);
                PinCode("Assets/Game/Scripts/Editor/Skirmish/S073ProductionPrefabKeyExport.cs", sourcePins);
                PinCode("Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs", sourcePins);
                PinCode("Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs", sourcePins);

                var spawnables = placement.Spawnables;
                if (spawnables == null || spawnables.Count != 6)
                    throw new InvalidDataException("Expected exactly six current S073 Construction spawnables");
                Type helper = Type.GetType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper, Game.Composition", true);
                MethodInfo resolver = helper.GetMethod("TryGetBuildingDefinitionMetadata", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (resolver == null) throw new MissingMethodException(helper.FullName, "TryGetBuildingDefinitionMetadata");

                var roots = new SortedDictionary<string, PrefabIdentity>(StringComparer.Ordinal);
                var startupIdentities = new JArray();
                foreach (JObject row in startupRows)
                {
                    PrefabIdentity identity = ResolveAuditedIdentity(row);
                    AddRoot(roots, identity);
                    startupIdentities.Add(IdentityJson(identity));
                }

                var buildingRows = new JArray();
                foreach (GameObject buildingAsset in spawnables)
                {
                    if (buildingAsset == null) throw new InvalidDataException("Missing Construction spawnable");
                    string buildingPath = AssetDatabase.GetAssetPath(buildingAsset);
                    PrefabIdentity building = ResolveIdentity(buildingAsset, buildingPath);
                    PinAssetAndDependencies(buildingPath, sourcePins);
                    RequireClean(buildingAsset, buildingPath);
                    GameObject isolated = PrefabUtility.LoadPrefabContents(buildingPath);
                    if (isolated == null) throw new InvalidDataException("Could not load authored building prefab: " + buildingPath);
                    try
                    {
                        object[] args = { isolated, null };
                        if (!(bool)resolver.Invoke(null, args) || args[1] == null)
                            throw new InvalidDataException("Original effective BuildingDefinition metadata is unavailable: " + buildingPath);
                        GameObject[] production = Read<GameObject[]>(args[1], "ProductionSpawnUnitPrefabs");
                        var products = new JArray();
                        for (int i = 0; production != null && i < production.Length; i++)
                        {
                            GameObject product = production[i];
                            if (product == null) throw new InvalidDataException("Null authored production prefab on " + buildingPath);
                            PrefabIdentity identity = ResolveIdentity(product, AssetDatabase.GetAssetPath(product));
                            PinAssetAndDependencies(identity.Path, sourcePins);
                            AddRoot(roots, identity);
                            products.Add(new JObject
                            {
                                ["index"] = i,
                                ["prefab"] = IdentityJson(identity)
                            });
                        }
                        buildingRows.Add(new JObject
                        {
                            ["building"] = IdentityJson(building),
                            ["production_prefabs"] = products
                        });
                    }
                    finally { PrefabUtility.UnloadPrefabContents(isolated); }
                }
                if (roots.Count != RequiredProductionRoots.Length || RequiredProductionRoots.Any(key => !roots.ContainsKey(key)))
                    throw new InvalidDataException("Current audited S073 startup plus original Construction metadata must resolve to exactly the 12 named legacy production roots; found " +
                        roots.Count + "; missing=" + string.Join(",", RequiredProductionRoots.Where(key => !roots.ContainsKey(key))));

                // Ensure startup identities and current AssetDatabase names agree with the same four audit roots.
                foreach (var identity in roots.Values)
                {
                    if (AssetDatabase.AssetPathToGUID(identity.Path) != identity.Guid || Sha256(identity.Path) != identity.AssetSha ||
                        Sha256(identity.Path + ".meta") != identity.MetaSha)
                        throw new InvalidOperationException("Prefab source changed during identity derivation: " + identity.Path);
                }
                JObject finalPins = HashSourcePaths(sourcePins.Keys);
                if (!JToken.DeepEquals(finalPins, JObject.FromObject(sourcePins)))
                    throw new InvalidOperationException("S073 authored source or dependency changed during key derivation");

                string[] keys = roots.Keys.ToArray();
                byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keys, Formatting.Indented) + "\n");
                string keySha = Sha256(keyBytes);
                // The generic capture consumer accepts a JSON string array. Keep provenance adjacent.
                byte[] consumerKeyBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(keys, Formatting.Indented) + "\n");
                if (Sha256(consumerKeyBytes) != keySha) throw new InvalidOperationException("Key serialization hash mismatch");
                var provenance = new JObject
                {
                    ["schema_version"] = 1,
                    ["kind"] = "S073OriginalProductionPrefabKeyDerivation",
                    ["mission"] = "S073",
                    ["authority"] = "CurrentYamlAuditAndOriginalConstructionBuildingDefinitionMetadata",
                    ["unity_version"] = Application.unityVersion,
                    ["key_array_path"] = outputPath,
                    ["key_array_sha256"] = keySha,
                    ["key_count"] = keys.Length,
                    ["keys"] = new JArray(keys),
                    ["audited_startup_roots"] = startupIdentities,
                    ["construction_path"] = ConstructionPath,
                    ["preset_path"] = PresetPath,
                    ["construction_buildings"] = buildingRows,
                    ["source_audit_path"] = auditPath,
                    ["source_audit_sha256"] = auditSha,
                    ["source_pins_sha256"] = finalPins,
                    ["mission_world_loaded"] = false,
                    ["mission_initialized"] = false,
                    ["native_readiness"] = false,
                    ["physical_geometry_exported"] = false
                };
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                using (var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    stream.Write(consumerKeyBytes, 0, consumerKeyBytes.Length);
                try
                {
                    using (var stream = new FileStream(provenancePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream)) writer.Write(provenance.ToString(Formatting.Indented) + "\n");
                }
                catch
                {
                    File.Delete(outputPath);
                    throw;
                }
                Debug.Log("[S073ProductionPrefabKeyExport] result=Passed keys=12 startup=4 constructionBuildings=6 productionReferences=" +
                    buildingRows.Sum(row => ((JArray)row["production_prefabs"]).Count) + " mission=0 readiness=0 keySha256=" + keySha);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S073ProductionPrefabKeyExport] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static bool IsStartupUse(JArray uses)
        {
            if (uses == null) return false;
            return uses.OfType<JObject>().Any(use =>
            {
                string source = (string)use["source_path"], field = (string)use["field_path"];
                return source == InitialForcesPath && field.Contains(".units[") && field.EndsWith(".prefab", StringComparison.Ordinal) ||
                       source == UnitRegistryPath && field.Contains(".unitSpawnPrefabs[");
            });
        }

        private static PrefabIdentity ResolveAuditedIdentity(JObject row)
        {
            string path = (string)row["asset_path"], guid = (string)row["guid"];
            long[] fileIds = (row["file_ids"] as JArray)?.Values<long>().Distinct().ToArray() ?? Array.Empty<long>();
            if (fileIds.Length != 1) throw new InvalidDataException("Startup prefab must have one audited local file ID: " + path);
            UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath(path).SingleOrDefault(value =>
                value is GameObject && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string objectGuid, out long localId) &&
                objectGuid == guid && localId == fileIds[0]);
            if (!(asset is GameObject gameObject)) throw new InvalidDataException("Audited startup identity did not resolve in AssetDatabase: " + path);
            return ResolveIdentity(gameObject, path);
        }

        private static PrefabIdentity ResolveIdentity(GameObject asset, string path)
        {
            if (asset == null || string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".prefab", StringComparison.Ordinal) || AssetDatabase.LoadAssetAtPath<GameObject>(path) != asset)
                throw new InvalidDataException("Expected exact prefab asset root");
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (guid.Length != 32 || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string objectGuid, out long localId) || objectGuid != guid)
                throw new InvalidDataException("Prefab GUID/local file identity unresolved: " + path);
            return new PrefabIdentity
            {
                Name = asset.name,
                Path = path,
                Guid = guid,
                LocalId = localId,
                AssetSha = Sha256(path),
                MetaSha = Sha256(path + ".meta")
            };
        }

        private static void AddRoot(SortedDictionary<string, PrefabIdentity> roots, PrefabIdentity identity)
        {
            if (string.IsNullOrWhiteSpace(identity.Name))
                throw new InvalidDataException("Prefab key must have a canonical asset-root name: " + identity.Path);
            if (roots.TryGetValue(identity.Name, out PrefabIdentity existing))
            {
                if (existing.Guid != identity.Guid || existing.LocalId != identity.LocalId)
                    throw new InvalidDataException("Prefab key name resolves to multiple GUID/local identities: " + identity.Name);
                return;
            }
            roots.Add(identity.Name, identity);
        }

        private static JObject IdentityJson(PrefabIdentity identity) => new JObject
        {
            ["name"] = identity.Name,
            ["asset_path"] = identity.Path,
            ["guid"] = identity.Guid,
            ["local_file_id"] = identity.LocalId,
            ["asset_sha256"] = identity.AssetSha,
            ["meta_sha256"] = identity.MetaSha
        };

        private static void PinAssetAndDependencies(string path, SortedDictionary<string, string> pins)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidDataException("Asset dependency outside project Assets: " + path);
            foreach (string dependency in AssetDatabase.GetDependencies(path, true).Append(path).Distinct(StringComparer.Ordinal))
            {
                if (!dependency.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                PinCurrentAssetPath(dependency, Sha256(dependency), pins);
                PinCurrentAssetPath(dependency + ".meta", Sha256(dependency + ".meta"), pins);
            }
        }

        private static void PinCode(string path, SortedDictionary<string, string> pins) => PinCurrentAssetPath(path, Sha256(path), pins);

        private static void PinCurrentAssetPath(string path, string expected, SortedDictionary<string, string> pins)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) && !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !IsSha(expected) || Sha256(path) != expected)
                throw new InvalidDataException("Current source pin required: " + path);
            if (pins.TryGetValue(path, out string existing) && existing != expected)
                throw new InvalidDataException("Conflicting source pin: " + path);
            pins[path] = expected;
        }

        private static JObject HashSourcePaths(IEnumerable<string> paths)
        {
            var hashes = new JObject();
            foreach (string path in paths.OrderBy(value => value, StringComparer.Ordinal)) hashes[path] = Sha256(path);
            return hashes;
        }

        private static T Read<T>(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.FieldType != typeof(T)) throw new MissingFieldException(instance.GetType().FullName, fieldName);
            return (T)field.GetValue(instance);
        }

        private static void RequireClean(UnityEngine.Object asset, string path)
        {
            if (asset == null || EditorUtility.IsDirty(asset))
                throw new InvalidOperationException("Authored asset must remain clean during key derivation: " + path);
        }

        private static string Arg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Required argument " + name);
            return args[index + 1];
        }

        private static void RequireFreshAbsolute(string path)
        {
            if (!Path.IsPathRooted(path) || File.Exists(path))
                throw new InvalidOperationException("Fresh absolute output path required: " + path);
        }

        private static bool IsSha(string value) => value != null && value.Length == 64 &&
            value.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f');

        private static string Sha256(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Required authored source is missing", path);
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        private static string Sha256(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
