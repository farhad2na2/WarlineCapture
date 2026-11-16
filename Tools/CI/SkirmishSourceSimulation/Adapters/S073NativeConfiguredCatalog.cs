using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Invocation-scoped index shared by both current-authored S073 binders. It
    // retains the complete .meta path set and hashes so the final check can
    // independently reject additions, removals, or edits during binding.
    internal sealed class S073AssetGuidIndex
    {
        private readonly string root;
        private readonly Dictionary<string, string[]> pathsByGuid;
        private readonly Dictionary<string, string> hashesByPath;

        internal S073AssetGuidIndex(string repositoryRoot)
        {
            root = NormalizeRoot(repositoryRoot);
            string assets = Path.Combine(root, "Assets");
            pathsByGuid = new Dictionary<string, string[]>(StringComparer.Ordinal);
            hashesByPath = new Dictionary<string, string>(StringComparer.Ordinal);
            var found = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                string full = Path.GetFullPath(path);
                byte[] bytes = File.ReadAllBytes(full);
                string relative = full.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/');
                hashesByPath.Add(relative, Digest(bytes));
                string assetPath = relative.Substring(0, relative.Length - ".meta".Length);
                foreach (string guid in GuidsFromText(ReadTextWithBomDetection(bytes)))
                {
                    if (!found.TryGetValue(guid, out List<string> matches)) found.Add(guid, matches = new List<string>());
                    matches.Add(assetPath);
                }
            }
            foreach (var row in found) pathsByGuid.Add(row.Key, row.Value.ToArray());
        }

        private static string Digest(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static string NormalizeRoot(string repositoryRoot) =>
            Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        private static string ReadTextWithBomDetection(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, false);
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        private static string[] GuidsFromText(string text)
        {
            var rows = Regex.Matches(text, @"(?m)^guid: ([0-9a-f]{32})\r?$");
            return rows.Cast<Match>().Select(row => row.Groups[1].Value).Distinct(StringComparer.Ordinal).ToArray();
        }

        internal void EnsureRepositoryRoot(string repositoryRoot)
        {
            if (!string.Equals(root, NormalizeRoot(repositoryRoot), StringComparison.Ordinal))
                throw new InvalidDataException("S073 asset GUID index belongs to a different repository root");
        }

        internal string FindUniquePath(string guid)
        {
            if (!pathsByGuid.TryGetValue(guid, out string[] matches) || matches.Length != 1)
                throw new InvalidDataException("Current asset GUID must resolve uniquely: " + guid);
            return matches[0];
        }

        internal void VerifyFresh()
        {
            string assets = Path.Combine(root, "Assets");
            var current = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                string full = Path.GetFullPath(path);
                string relative = full.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/');
                current.Add(relative);
                if (!hashesByPath.TryGetValue(relative, out string expected) || Digest(File.ReadAllBytes(full)) != expected)
                    throw new InvalidDataException("Asset metadata changed during current catalog binding: " + relative);
            }
            if (current.Count != hashesByPath.Count)
                throw new InvalidDataException("Asset metadata set changed during current catalog binding");
        }
    }

    // This binds captured authoring metadata only. It does not establish the
    // unit catalog, native match startup, physical source fit, or readiness.
    internal static class S073NativeConfiguredCatalog
    {
        private static string Sha(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        internal static Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> Bind(
            string repositoryRoot, string receiptPath, string receiptSha,
            IReadOnlyList<RuntimeDefinition> definitions)
        {
            if (definitions == null || definitions.Count != 6 || receiptSha == null || receiptSha.Length != 64)
                throw new InvalidDataException("Complete S073 definitions and exact metadata receipt required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            byte[] receiptBytes = File.ReadAllBytes(receiptPath);
            if (Sha(receiptBytes) != receiptSha) throw new InvalidDataException("Metadata receipt hash changed");
            var receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(receiptBytes));
            if ((string)receipt["kind"] != "S073ConfiguredCatalogMetadataReceipt" || (int?)receipt["schema_version"] != 1 ||
                (string)receipt["validation_status"] != "Passed" || (int?)receipt["wrapper_exit_code"] != 0 ||
                (bool?)receipt["required_marker_present"] != true || (bool?)receipt["mission_ready"] != false)
                throw new InvalidDataException("Qualified source-only native metadata receipt required");
            string directory = Path.GetDirectoryName(Path.GetFullPath(receiptPath));
            byte[] catalogBytes = File.ReadAllBytes(Path.Combine(directory, "catalog.json"));
            byte[] logBytes = File.ReadAllBytes(Path.Combine(directory, "editor.log"));
            if (Sha(catalogBytes) != (string)receipt["catalog_sha256"] || Sha(logBytes) != (string)receipt["editor_log_sha256"] ||
                !System.Text.Encoding.UTF8.GetString(logBytes).Contains("[S073CatalogMetadataExport] result=Passed entries=6 "))
                throw new InvalidDataException("Qualified catalog or editor log changed");
            var catalog = JObject.Parse(System.Text.Encoding.UTF8.GetString(catalogBytes));
            if ((string)catalog["kind"] != "S073ConfiguredCatalogMetadataExport" || (int?)catalog["schema_version"] != 1 ||
                (string)catalog["mission"] != "S073" || (bool?)catalog["mission_world_loaded"] != false ||
                (bool?)catalog["native_readiness"] != false ||
                (string)catalog["preset_asset_path"] != "Assets/Game/Resources/SkirmishIndustrialBasin.asset" ||
                (string)catalog["building_placement_source_path"] != "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset")
                throw new InvalidDataException("Exact S073 authoring capture required");
            var pins = catalog["source_dependencies_sha256"] as JObject;
            if (pins == null || pins.Count != (int?)receipt["source_pin_count"] || pins.Count < 6)
                throw new InvalidDataException("Complete capture source pins required");
            foreach (var pin in pins.Properties())
            {
                string path = Path.GetFullPath(Path.Combine(root, pin.Name));
                if (Path.IsPathRooted(pin.Name) || !path.StartsWith(root, StringComparison.Ordinal) || Sha(File.ReadAllBytes(path)) != (string)pin.Value)
                    throw new InvalidDataException("Captured metadata source changed: " + pin.Name);
            }
            var rows = catalog["entries"] as JArray;
            if (rows == null || rows.Count != definitions.Count) throw new InvalidDataException("Complete S073 metadata rows required");
            var result = new Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry>();
            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var row = rows[i] as JObject;
                var prefab = definition?.Prefab;
                if (prefab == null || row == null || !prefab.IsAssetIdentityOnly ||
                    prefab.name != (string)row["prefab_name"] || prefab.SourceAssetPath != (string)row["asset_path"] ||
                    prefab.SourceAssetGuid != (string)row["guid"] || prefab.SourceAssetLocalId != (long?)row["local_file_id"] ||
                    row["effective_can_request"]?.Type != JTokenType.Boolean || row["effective_price"]?.Type != JTokenType.Integer ||
                    row["effective_materials_cost"]?.Type != JTokenType.Integer ||
                    definition.CreditsCost != (int?)row["effective_price"] || definition.MaterialsCost != (int?)row["effective_materials_cost"] ||
                    pins[prefab.SourceAssetPath] == null || pins[prefab.SourceAssetPath + ".meta"] == null)
                    throw new InvalidDataException("Captured metadata differs from original source definition at row " + i);
                result.Add(prefab, new BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry(
                    definition.DisplayName, definition.Description, prefab, (bool)row["effective_can_request"], (int)row["effective_price"]));
            }
            if (Sha(File.ReadAllBytes(receiptPath)) != receiptSha || Sha(File.ReadAllBytes(Path.Combine(directory, "catalog.json"))) != (string)receipt["catalog_sha256"])
                throw new InvalidDataException("Metadata evidence changed during binding");
            return result;
        }

        // Current-source alternative for the diagnostic host. It follows the same
        // original authoring owner (BuildingDefinitionAuthoring.ApplyConfigIfAvailable)
        // from the exact Construction asset and its referenced prefab config assets;
        // it does not reuse a native catalog receipt tied to an older InitialForces.
        internal static Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> BindCurrentAuthored(
            string repositoryRoot, IReadOnlyList<RuntimeDefinition> definitions, out JObject evidence)
        {
            return BindCurrentAuthored(repositoryRoot, definitions, new S073AssetGuidIndex(repositoryRoot), out evidence);
        }

        internal static Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry> BindCurrentAuthored(
            string repositoryRoot, IReadOnlyList<RuntimeDefinition> definitions, S073AssetGuidIndex guidIndex, out JObject evidence)
        {
            if (guidIndex == null) throw new InvalidDataException("Current S073 asset GUID index required");
            guidIndex.EnsureRepositoryRoot(repositoryRoot);
            if (definitions == null || definitions.Count != 6 || guidIndex == null)
                throw new InvalidDataException("Six current S073 Construction definitions required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var pins = new Dictionary<string, string>(StringComparer.Ordinal);
            string Pin(string relative)
            {
                string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
                if (!full.StartsWith(root, StringComparison.Ordinal) || !File.Exists(full))
                    throw new InvalidDataException("Current S073 catalog source is missing: " + relative);
                string digest = Sha(File.ReadAllBytes(full));
                pins[relative] = digest;
                return full;
            }
            string GuidFor(string relativeMeta)
            {
                string text = File.ReadAllText(Pin(relativeMeta));
                var rows = Regex.Matches(text, @"(?m)^guid: ([0-9a-f]{32})\r?$");
                if (rows.Count != 1) throw new InvalidDataException("Unique GUID required: " + relativeMeta);
                return rows[0].Groups[1].Value;
            }
            string FindPathForGuid(string guid)
            {
                return guidIndex.FindUniquePath(guid);
            }
            string Field(string text, string name)
            {
                string[] lines = text.Replace("\r", string.Empty).Split('\n');
                var rows = Regex.Matches(text, @"(?m)^  " + Regex.Escape(name) + @": ?([^\r\n]*)\r?$");
                if (rows.Count != 1) throw new InvalidDataException("Expected one current authoring field: " + name);
                int lineIndex = text.Substring(0, rows[0].Index).Count(c => c == '\n');
                var value = new System.Text.StringBuilder(rows[0].Groups[1].Value.Trim());
                for (int i = lineIndex + 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) { value.Append(' '); continue; }
                    int indent = lines[i].TakeWhile(c => c == ' ').Count();
                    if (indent <= 2) break;
                    value.Append(' ').Append(lines[i].Trim());
                }
                return value.ToString().Trim();
            }
            int IntField(string text, string name, int? defaultValue = null)
            {
                var matches = Regex.Matches(text, @"(?m)^  " + Regex.Escape(name) + @": (-?[0-9]+)\r?$");
                if (matches.Count == 0 && defaultValue.HasValue) return defaultValue.Value;
                if (matches.Count != 1 || !int.TryParse(matches[0].Groups[1].Value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int parsed))
                    throw new InvalidDataException("Current authoring integer field required: " + name);
                return parsed;
            }
            string presetPath = "Assets/Game/Resources/SkirmishIndustrialBasin.asset";
            string constructionPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset";
            string[] scriptPins = {
                "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs",
                "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
                "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs"
            };
            string preset = File.ReadAllText(Pin(presetPath));
            string construction = File.ReadAllText(Pin(constructionPath));
            string registryGuid = GuidFor("Assets/Game/Configs/Skirmish/UnitRegistry.asset.meta");
            string constructionGuid = GuidFor(constructionPath + ".meta");
            if (Field(preset, "m_Name") != "SkirmishIndustrialBasin" ||
                Regex.Matches(preset, @"(?m)^  buildingPlacement: \{fileID: 11400000, guid: " + Regex.Escape(constructionGuid) + @", type: 2\}\r?$").Count != 1 ||
                Field(construction, "m_Name") != "Construction" ||
                Regex.Matches(construction, @"(?m)^  unitPrefabRegistryConfig: \{fileID: 11400000, guid: " + Regex.Escape(registryGuid) + @", type: 2\}\r?$").Count != 1)
                throw new InvalidDataException("Current S073 preset, Construction and UnitRegistry references do not match the original source chain");

            var expected = new[] { "Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
                "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder" };
            var result = new Dictionary<SourceUnitPrefab, BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry>();
            var rowsOut = new JArray();
            for (int i = 0; i < definitions.Count; i++)
            {
                RuntimeDefinition definition = definitions[i];
                SourceUnitPrefab prefab = definition?.Prefab;
                if (prefab == null || prefab.name != expected[i] || string.IsNullOrEmpty(prefab.SourceAssetPath) ||
                    prefab.SourceAssetGuid == null || prefab.SourceAssetGuid.Length != 32 || prefab.SourceAssetLocalId == 0)
                    throw new InvalidDataException("Exact current S073 Construction prefab identity required at " + i);
                string prefabText = File.ReadAllText(Pin(prefab.SourceAssetPath));
                string prefabMeta = prefab.SourceAssetPath + ".meta";
                if (GuidFor(prefabMeta) != prefab.SourceAssetGuid)
                    throw new InvalidDataException("Current building prefab GUID differs from source definition: " + prefab.name);
                var configRefs = Regex.Matches(prefabText,
                    @"(?m)^    - target: \{fileID: 5385380694625381001, guid: 7f40f52c9aed94fefb3dda0fc58866c0, type: 3\}\r?\n      propertyPath: config\r?\n      value: *\r?\n      objectReference: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}\r?$");
                if (configRefs.Count != 1)
                    throw new InvalidDataException("Unique original BuildingDefinitionAuthoring config override required: " + prefab.name);
                string configGuid = configRefs[0].Groups[1].Value;
                string configPath = FindPathForGuid(configGuid);
                string configText = File.ReadAllText(Pin(configPath));
                if (Field(configText, "m_Name").Length == 0)
                    throw new InvalidDataException("Current building config identity missing: " + configPath);
                string displayName = Field(configText, "displayName");
                string description = Field(configText, "description");
                // BuildingDefinitionConfig defaults this serialized field to true;
                // two current S073 config assets intentionally omit the default.
                bool canRequest = IntField(configText, "canRequest", 1) != 0;
                int credits = Math.Max(0, IntField(configText, "price"));
                if (displayName != definition.DisplayName || description != definition.Description || credits != definition.CreditsCost)
                    throw new InvalidDataException("Current S073 BuildingDefinitionAuthoring config differs from original definition owner at " + prefab.name);
                result.Add(prefab, new BuildingUiCommandSystemHelper.ConfiguredSpawnableEntry(
                    displayName, description, prefab, canRequest, credits));
                rowsOut.Add(new JObject { ["construction_order"] = i, ["prefab"] = prefab.name,
                    ["prefab_path"] = prefab.SourceAssetPath, ["prefab_guid"] = prefab.SourceAssetGuid,
                    ["prefab_local_id"] = prefab.SourceAssetLocalId, ["config_path"] = configPath,
                    ["config_guid"] = configGuid, ["can_request"] = canRequest,
                    ["display_name"] = displayName, ["credits_cost"] = credits });
            }
            foreach (string script in scriptPins) { Pin(script); Pin(script + ".meta"); }
            foreach (var pin in pins.ToArray())
                if (Sha(File.ReadAllBytes(Path.Combine(root, pin.Key.Replace('/', Path.DirectorySeparatorChar)))) != pin.Value)
                    throw new InvalidDataException("Current S073 catalog source changed during readback: " + pin.Key);
            guidIndex.VerifyFresh();
            evidence = new JObject {
                ["kind"] = "S073CurrentSourceConfiguredBuildingReadback",
                ["authority"] = "OriginalBuildingDefinitionAuthoringMetadataPrefabSystemHelper_CurrentAssetChain",
                ["mission_world_loaded"] = false, ["native_readiness"] = false,
                ["rows"] = rowsOut, ["source_pins_sha256"] = JObject.FromObject(pins)
            };
            return result;
        }
    }
}
