using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace Game.Runtime
{
    // Identity join only. This does not authorize live overlay, unit delivery,
    // scheduler admission, or native mission readiness.
    internal sealed class S073NativeConfiguredUnits
    {
        private sealed class Row
        {
            internal SourceUnitPrefab Prefab;
            internal string Display;
            internal int Displayed, Credits, Materials;
            internal bool Requestable, Vehicle;
        }
        private readonly EntityManager manager;
        private readonly List<Row> rows;
        private S073NativeConfiguredUnits(EntityManager manager, List<Row> rows) { this.manager = manager; this.rows = rows; }
        internal int Count => rows.Count;
        internal bool BelongsTo(EntityManager candidate) => ReferenceEquals(manager, candidate);
        private static string Hash(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }

        internal static S073NativeConfiguredUnits Bind(EntityManager em, GeneratedReferenceTemplateImporter.Result imported,
            string repositoryRoot, string receiptPath, string receiptSha, string identityPath, string identitySha,
            string prefabCapturePath, string prefabCaptureSha)
        {
            if (em == null || imported == null || !ReferenceEquals(imported.Manager, em))
                throw new InvalidDataException("Actual imported templates in this source world required");
            byte[] receiptBytes = File.ReadAllBytes(receiptPath), identityBytes = File.ReadAllBytes(identityPath), captureBytes = File.ReadAllBytes(prefabCapturePath);
            if (Hash(receiptBytes) != receiptSha || Hash(identityBytes) != identitySha || Hash(captureBytes) != prefabCaptureSha)
                throw new InvalidDataException("Exact unit receipt, source identities and prefab capture required");
            var receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(receiptBytes));
            if ((string)receipt["kind"] != "S073ConfiguredUnitCatalogReceipt" || (int?)receipt["schema_version"] != 1 ||
                (string)receipt["validation_status"] != "Passed" || (int?)receipt["wrapper_exit_code"] != 0 ||
                (bool?)receipt["required_marker_present"] != true || (bool?)receipt["mission_ready"] != false)
                throw new InvalidDataException("Qualified source-only unit receipt required");
            string dir = Path.GetDirectoryName(Path.GetFullPath(receiptPath));
            byte[] catalogBytes = File.ReadAllBytes(Path.Combine(dir, "unit-catalog.json")), logBytes = File.ReadAllBytes(Path.Combine(dir, "editor.log"));
            if (Hash(catalogBytes) != (string)receipt["catalog_sha256"] || Hash(logBytes) != (string)receipt["editor_log_sha256"] ||
                !System.Text.Encoding.UTF8.GetString(logBytes).Contains("[S073UnitCatalogExport] result=Passed entries=3 "))
                throw new InvalidDataException("Qualified unit capture or log changed");
            var catalog = JObject.Parse(System.Text.Encoding.UTF8.GetString(catalogBytes));
            if ((string)catalog["kind"] != "S073ConfiguredUnitCatalogExport" || (int?)catalog["schema_version"] != 1 ||
                (string)catalog["mission"] != "S073" || (bool?)catalog["mission_world_loaded"] != false ||
                (bool?)catalog["native_readiness"] != false ||
                (string)catalog["unit_registry_source_path"] != "Assets/Game/Configs/Skirmish/UnitRegistry.asset" ||
                (string)catalog["building_placement_source_path"] != "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset")
                throw new InvalidDataException("Exact source-only S073 configured unit capture required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var pins = catalog["source_dependencies_sha256"] as JObject;
            if (pins == null || pins.Count != (int?)receipt["source_pin_count"] || pins.Count < 6)
                throw new InvalidDataException("Complete unit metadata source pins required");
            foreach (var pin in pins.Properties())
            {
                string path = Path.GetFullPath(Path.Combine(root, pin.Name));
                if (Path.IsPathRooted(pin.Name) || !path.StartsWith(root, StringComparison.Ordinal) || Hash(File.ReadAllBytes(path)) != (string)pin.Value)
                    throw new InvalidDataException("Unit metadata source changed: " + pin.Name);
            }
            var identity = JObject.Parse(System.Text.Encoding.UTF8.GetString(identityBytes));
            var capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(captureBytes));
            if ((string)identity["kind"] != "S073LegacyPrefabSourceNameReadback" || (string)identity["mission"] != "S073" ||
                (string)identity["prefab_capture_sha256"] != prefabCaptureSha || (bool?)identity["mission_world_allocated"] != false ||
                (string)capture["kind"] != "AuthoredSkirmishPrefabReferenceData")
                throw new InvalidDataException("Same source-only S073 identity and prefab capture required");
            var sourceObjects = identity["objects"] as JArray;
            var entries = catalog["entries"] as JArray;
            var roots = capture["opening_prefabs"] as JObject;
            if (sourceObjects == null || entries == null || entries.Count != 3 || roots == null)
                throw new InvalidDataException("Complete configured-unit and prefab source domains required");
            var result = new List<Row>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                var row = entries[i] as JObject ?? throw new InvalidDataException("Unit metadata row required");
                string name = (string)row["prefab_name"], path = (string)row["asset_path"], guid = (string)row["guid"];
                long? local = (long?)row["local_file_id"];
                var matches = sourceObjects.OfType<JObject>().Where(x => (string)x["game_object_name"] == name &&
                    (string)x["unit_source_key"] == name && (string)x["asset_path"] == path && (string)x["guid"] == guid && (long?)x["file_id"] == local).ToArray();
                if ((int?)row["registry_order"] != i || string.IsNullOrEmpty(name) || matches.Length != 1 || !seen.Add(guid + ":" + local) ||
                    pins[path] == null || pins[path + ".meta"] == null || row["can_request"]?.Type != JTokenType.Boolean ||
                    (string)matches[0]["asset_sha256"] != (string)pins[path] || (string)matches[0]["meta_sha256"] != (string)pins[path + ".meta"] ||
                    row["is_vehicle"]?.Type != JTokenType.Boolean || row["display_name"]?.Type != JTokenType.String ||
                    row["displayed_price"]?.Type != JTokenType.Integer || row["credits_cost"]?.Type != JTokenType.Integer ||
                    row["materials_cost"]?.Type != JTokenType.Integer || (int)row["displayed_price"] < 0 ||
                    (int)row["credits_cost"] < 0 || (int)row["materials_cost"] < 0)
                    throw new InvalidDataException("Exact ordered native unit identity and metadata required at " + i);
                if (!imported.Prefabs.TryGetValue(name, out Entity entity) || roots[name] == null ||
                    !imported.SourceToLogical.TryGetValue(ExactInitialValueCodec.EntityId((string)roots[name]), out Entity mapped) || mapped != entity ||
                    !em.Exists(entity) || !em.HasComponent<Prefab>(entity) || !em.HasComponent<UnitSourcePrefabKey>(entity) ||
                    em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() != name)
                    throw new InvalidDataException("Actual same-world source prefab template mismatch: " + name);
                result.Add(new Row { Prefab = new SourceUnitPrefab(name, entity, path, guid, local.Value), Display = (string)row["display_name"],
                    Displayed = (int)row["displayed_price"], Credits = (int)row["credits_cost"], Materials = (int)row["materials_cost"],
                    Requestable = (bool)row["can_request"], Vehicle = (bool)row["is_vehicle"] });
            }
            if (Hash(File.ReadAllBytes(receiptPath)) != receiptSha || Hash(File.ReadAllBytes(identityPath)) != identitySha ||
                Hash(File.ReadAllBytes(prefabCapturePath)) != prefabCaptureSha)
                throw new InvalidDataException("Unit join inputs changed during binding");
            return new S073NativeConfiguredUnits(em, result);
        }

        // Current-source projection from the live UnitRegistry and each selected
        // prefab's UnitGridAuthoringConfig. Existing capture rows are used only to
        // confirm exact object identity; current prices/requestability come from
        // current serialized assets and the original metadata owner semantics.
        internal static S073NativeConfiguredUnits BindCurrentAuthored(EntityManager em,
            GeneratedReferenceTemplateImporter.Result imported, string repositoryRoot,
            string identityPath, string identitySha, string prefabCapturePath, string prefabCaptureSha,
            string registryPath, out JObject evidence)
        {
            return BindCurrentAuthored(em, imported, repositoryRoot, identityPath, identitySha,
                prefabCapturePath, prefabCaptureSha, registryPath, new S073AssetGuidIndex(repositoryRoot), out evidence);
        }

        internal static S073NativeConfiguredUnits BindCurrentAuthored(EntityManager em,
            GeneratedReferenceTemplateImporter.Result imported, string repositoryRoot,
            string identityPath, string identitySha, string prefabCapturePath, string prefabCaptureSha,
            string registryPath, S073AssetGuidIndex guidIndex, out JObject evidence)
        {
            if (guidIndex == null) throw new InvalidDataException("Current S073 asset GUID index required");
            guidIndex.EnsureRepositoryRoot(repositoryRoot);
            if (em == null || imported == null || !ReferenceEquals(imported.Manager, em) || guidIndex == null)
                throw new InvalidDataException("Actual imported templates in this source EntityManager required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var pins = new Dictionary<string, string>(StringComparer.Ordinal);
            byte[] identityBytes = File.ReadAllBytes(identityPath), captureBytes = File.ReadAllBytes(prefabCapturePath);
            if (Hash(identityBytes) != identitySha || Hash(captureBytes) != prefabCaptureSha)
                throw new InvalidDataException("Exact S073 identity/capture input hashes required");
            string Pin(string relative)
            {
                string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
                if (!full.StartsWith(root, StringComparison.Ordinal) || !File.Exists(full))
                    throw new InvalidDataException("Current S073 unit source missing: " + relative);
                string digest = Hash(File.ReadAllBytes(full)); pins[relative] = digest; return full;
            }
            string GuidFor(string relativeMeta)
            {
                string text = File.ReadAllText(Pin(relativeMeta));
                var matches = Regex.Matches(text, @"(?m)^guid: ([0-9a-f]{32})\r?$");
                if (matches.Count != 1) throw new InvalidDataException("Unique current asset GUID required: " + relativeMeta);
                return matches[0].Groups[1].Value;
            }
            string FindPathForGuid(string guid)
            {
                return guidIndex.FindUniquePath(guid);
            }
            string TextField(string text, string name)
            {
                var matches = Regex.Matches(text, @"(?m)^  " + Regex.Escape(name) + @": ?([^\r\n]*)\r?$");
                if (matches.Count != 1) throw new InvalidDataException("Expected one current UnitGridAuthoringConfig field: " + name);
                string[] lines = text.Replace("\r", string.Empty).Split('\n');
                int lineIndex = text.Substring(0, matches[0].Index).Count(c => c == '\n');
                var value = new System.Text.StringBuilder(matches[0].Groups[1].Value.Trim());
                for (int i = lineIndex + 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) { value.Append(' '); continue; }
                    int indent = lines[i].TakeWhile(c => c == ' ').Count();
                    if (indent <= 2) break;
                    value.Append(' ').Append(lines[i].Trim());
                }
                return value.ToString().Trim();
            }
            int IntField(string text, string name, int defaultValue = 0)
            {
                var matches = Regex.Matches(text, @"(?m)^  " + Regex.Escape(name) + @": (-?[0-9]+)\r?$");
                if (matches.Count == 0) return defaultValue;
                if (matches.Count != 1 || !int.TryParse(matches[0].Groups[1].Value,
                    System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value))
                    throw new InvalidDataException("Exact current UnitGridAuthoringConfig integer required: " + name);
                return value;
            }
            const string registryExpected = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
            if (Path.GetFullPath(registryPath) != Path.GetFullPath(Path.Combine(root, registryExpected.Replace('/', Path.DirectorySeparatorChar))))
                throw new InvalidDataException("Exact current S073 UnitRegistry asset path required");
            string registryText = File.ReadAllText(Pin(registryExpected));
            string constructionText = File.ReadAllText(Pin("Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"));
            string presetText = File.ReadAllText(Pin("Assets/Game/Resources/SkirmishIndustrialBasin.asset"));
            string registryGuid = GuidFor(registryExpected + ".meta");
            string constructionGuid = GuidFor("Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset.meta");
            if (Regex.Matches(constructionText, @"(?m)^  unitPrefabRegistryConfig: \{fileID: 11400000, guid: " + Regex.Escape(registryGuid) + @", type: 2\}\r?$").Count != 1 ||
                Regex.Matches(presetText, @"(?m)^  buildingPlacement: \{fileID: 11400000, guid: " + Regex.Escape(constructionGuid) + @", type: 2\}\r?$").Count != 1)
                throw new InvalidDataException("Current S073 preset-to-Construction-to-UnitRegistry chain mismatch");
            var registryBlock = Regex.Match(registryText, @"(?ms)^  unitSpawnPrefabs:\s*\n(?<rows>.*?)(?=^  impostorAtlases:)");
            if (!registryBlock.Success) throw new InvalidDataException("Current UnitRegistry unitSpawnPrefabs list required");
            var registryRows = Regex.Matches(registryBlock.Groups["rows"].Value,
                @"(?m)^  - \{fileID: ([0-9]+), guid: ([0-9a-f]{32}), type: 3\}\r?$");
            if (registryRows.Count != 3) throw new InvalidDataException("Exact three-unit S073 current registry required");

            var identity = JObject.Parse(System.Text.Encoding.UTF8.GetString(identityBytes));
            var capture = JObject.Parse(System.Text.Encoding.UTF8.GetString(captureBytes));
            if ((string)identity["kind"] != "S073LegacyPrefabSourceNameReadback" || (string)identity["mission"] != "S073" ||
                (bool?)identity["mission_world_allocated"] != false || (string)identity["prefab_capture_sha256"] != prefabCaptureSha ||
                (string)capture["kind"] != "AuthoredSkirmishPrefabReferenceData")
                throw new InvalidDataException("S073 identity rows and imported prefab closure must retain their source-only provenance");
            var identityRows = (identity["objects"] as JArray)?.OfType<JObject>()
                .Where(row => !string.IsNullOrEmpty((string)row["unit_source_key"]))
                .ToDictionary(row => (string)row["unit_source_key"], StringComparer.Ordinal);
            var roots = capture["opening_prefabs"] as JObject;
            // The retained source-only identity snapshot also names an earlier
            // Armored Car root that is no longer in current UnitRegistry. Only
            // the three current registry rows are catalog members; keep the extra
            // captured identity available without mistaking it for a registry row.
            if (identityRows == null || identityRows.Count < 3 || roots == null)
                throw new InvalidDataException("Current three-unit UnitRegistry identities and captured roots required");

            var result = new List<Row>();
            var outputRows = new JArray();
            for (int i = 0; i < registryRows.Count; i++)
            {
                long fileId = long.Parse(registryRows[i].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                string guid = registryRows[i].Groups[2].Value;
                JObject identityRow = identityRows.Values.SingleOrDefault(row =>
                    (string)row["guid"] == guid && (long?)row["file_id"] == fileId);
                if (identityRow == null) throw new InvalidDataException("Current UnitRegistry order does not join exact prefab identity at " + i);
                string name = (string)identityRow["game_object_name"];
                string prefabPath = (string)identityRow["asset_path"];
                if (string.IsNullOrEmpty(name) || prefabPath == null ||
                    (string)identityRow["unit_source_key"] != name || GuidFor(prefabPath + ".meta") != guid)
                    throw new InvalidDataException("Current unit identity/path metadata mismatch at registry row " + i);
                byte[] prefabBytes = File.ReadAllBytes(Pin(prefabPath));
                string actualPrefabSha = Hash(prefabBytes);
                string actualMetaSha = pins[prefabPath + ".meta"];
                if (actualPrefabSha != (string)identityRow["asset_sha256"] || actualMetaSha != (string)identityRow["meta_sha256"])
                    throw new InvalidDataException("Current unit prefab bytes differ from the retained identity row: " + name);
                string prefabText = System.Text.Encoding.UTF8.GetString(prefabBytes);
                var configRefs = Regex.Matches(prefabText,
                    @"(?m)^    - target: \{fileID: 106347591433935587, guid: 5680e7910ed3741beba6f07116b6b03c, type: 3\}\r?\n      propertyPath: config\r?\n      value: *\r?\n      objectReference: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}\r?$");
                if (configRefs.Count != 1) throw new InvalidDataException("Unique root UnitGridAuthoring config override required: " + name);
                string configGuid = configRefs[0].Groups[1].Value;
                string configPath = FindPathForGuid(configGuid);
                string configText = File.ReadAllText(Pin(configPath));
                string configName = TextField(configText, "m_Name");
                string display = TextField(configText, "displayName");
                int canRequestValue = IntField(configText, "canRequest", 1);
                int configuredPrice = IntField(configText, "price", 0);
                int configuredMaterials = IntField(configText, "materialsCost", 0);
                var footprintMatch = Regex.Match(configText, @"(?m)^  footprintCells: \{x: ([0-9]+), y: ([0-9]+)\}\r?$");
                int width = footprintMatch.Success ? int.Parse(footprintMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
                int height = footprintMatch.Success ? int.Parse(footprintMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
                bool isVehicle = width > 1 || height > 1 || name.IndexOf("Veh", StringComparison.OrdinalIgnoreCase) >= 0;
                // Match UnitGridAuthoringConfig.Price/MaterialsCost exactly. Its
                // legacy zero-price fallback keys off the config asset name and
                // footprint, not the prefab name or inferred vehicle flag.
                bool configVehicle = width > 1 || height > 1 || configName.IndexOf("Veh", StringComparison.OrdinalIgnoreCase) >= 0;
                int credits = configuredPrice > 0 ? configuredPrice : (configVehicle ? 15000 : 10000);
                int materials = configuredMaterials > 0 ? configuredMaterials : Math.Max(1, (int)Math.Ceiling(Math.Max(0, credits) / 500d));
                if (string.IsNullOrWhiteSpace(display)) display = name;
                if (!imported.Prefabs.TryGetValue(name, out Entity template) || !em.Exists(template) ||
                    !em.HasComponent<Prefab>(template) || !em.HasComponent<UnitSourcePrefabKey>(template) ||
                    em.GetComponentData<UnitSourcePrefabKey>(template).Value.ToString() != name || roots[name] == null ||
                    !imported.SourceToLogical.TryGetValue(ExactInitialValueCodec.EntityId((string)roots[name]), out Entity mapped) || mapped != template)
                    throw new InvalidDataException("Current registry unit does not resolve to its exact imported source prefab template: " + name);
                result.Add(new Row { Prefab = new SourceUnitPrefab(name, template, prefabPath, guid, fileId),
                    Display = display, Displayed = materials, Credits = credits, Materials = materials,
                    Requestable = canRequestValue != 0, Vehicle = isVehicle });
                outputRows.Add(new JObject { ["registry_order"] = i, ["prefab"] = name, ["asset_path"] = prefabPath,
                    ["guid"] = guid, ["local_file_id"] = fileId, ["config_path"] = configPath, ["config_guid"] = configGuid,
                    ["display_name"] = display, ["can_request"] = canRequestValue != 0, ["is_vehicle"] = isVehicle,
                    ["displayed_price_materials"] = materials, ["credits_cost"] = credits, ["materials_cost"] = materials });
            }
            foreach (string relative in new[] {
                "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
                "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
                "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionUnitProductionResources.cs",
                "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs" })
            { Pin(relative); Pin(relative + ".meta"); }
            foreach (var pin in pins.ToArray())
                if (Hash(File.ReadAllBytes(Path.Combine(root, pin.Key.Replace('/', Path.DirectorySeparatorChar)))) != pin.Value)
                    throw new InvalidDataException("Current S073 unit source changed during readback: " + pin.Key);
            if (Hash(File.ReadAllBytes(identityPath)) != identitySha || Hash(File.ReadAllBytes(prefabCapturePath)) != prefabCaptureSha)
                throw new InvalidDataException("S073 identity/capture changed during current unit readback");
            guidIndex.VerifyFresh();
            evidence = new JObject { ["kind"] = "S073CurrentSourceConfiguredUnitReadback",
                ["authority"] = "OriginalUnitGridAuthoringConfig_UnitPrefabRegistryAuthoringConfig_CurrentAssets",
                ["mission_world_loaded"] = false, ["native_readiness"] = false,
                ["rows"] = outputRows, ["source_pins_sha256"] = JObject.FromObject(pins),
                ["identity_input_sha256"] = identitySha, ["prefab_capture_sha256"] = prefabCaptureSha };
            return new S073NativeConfiguredUnits(em, result);
        }

        internal bool TryRead(int index, out SourceUnitPrefab prefab, out string display, out int price, out bool canRequest, out bool vehicle)
        {
            prefab = null; display = string.Empty; price = 0; canRequest = false; vehicle = false;
            if (index < 0 || index >= rows.Count) return false;
            var row = rows[index];
            if (!manager.Exists(row.Prefab.Template)) throw new InvalidOperationException("Bound source unit template no longer exists");
            prefab = row.Prefab; display = row.Display; price = row.Displayed; canRequest = row.Requestable; vehicle = row.Vehicle;
            return true;
        }

        internal bool TryCosts(SourceUnitPrefab prefab, int fallback, out int credits, out int materials)
        {
            credits = 0; materials = 0;
            var row = rows.SingleOrDefault(x => ReferenceEquals(x.Prefab, prefab));
            if (row == null) return false;
            if (!manager.Exists(row.Prefab.Template)) throw new InvalidOperationException("Bound source unit template no longer exists");
            credits = row.Credits; materials = row.Materials; return true;
        }
    }
}
