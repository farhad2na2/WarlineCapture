using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.Runtime;

namespace Game.Editor
{
    // Joins the actual compiler result to its hash-pinned authoring request and
    // selected Campaign source. This is evidence validation only; no setup is
    // inferred and no readiness flag is promoted.
    internal static class CompilerSelectedAuthoringEvidence
    {
        internal static JObject Validate(JObject physicalRequest, JObject result, string resultPath,
            string projectRoot, IDictionary<string, string> retainedPins)
        {
            return Validate(physicalRequest, result, resultPath, projectRoot, retainedPins, out _);
        }

        internal static JObject Validate(JObject physicalRequest, JObject result, string resultPath,
            string projectRoot, IDictionary<string, string> retainedPins, out SkirmishResolvedSetup selectedSetup)
        {
            return Validate(physicalRequest, result, resultPath, projectRoot, retainedPins, out selectedSetup, out _);
        }

        internal static JObject Validate(JObject physicalRequest, JObject result, string resultPath,
            string projectRoot, IDictionary<string, string> retainedPins, out SkirmishResolvedSetup selectedSetup,
            out JArray hydratedAssetGuids)
        {
            var request = ValidateLinkage(physicalRequest, result, resultPath, projectRoot, retainedPins);
            selectedSetup = VerifyCurrentCompiledSetup(request, result, (JObject)result["input_pins"],
                Path.GetFullPath(projectRoot), retainedPins, out hydratedAssetGuids);
            return request;
        }

        internal static JObject ValidateLinkage(JObject physicalRequest, JObject result, string resultPath,
            string projectRoot, IDictionary<string, string> retainedPins)
        {
            if (physicalRequest == null || result == null || retainedPins == null)
                throw new ArgumentNullException("Pinned compiler selection inputs required");
            ExactKeys(result, "schema_version", "kind", "status", "catalog_id", "seed", "size", "difficulty",
                "value_format", "definition_id", "layout_id", "operation_map_id", "source_content_hash",
                "layout_fingerprint", "source_fingerprint", "input_pins", "compiler_accepted",
                "compiler_reasons", "native_authoring_inputs_returned", "authoring_capture_reasons",
                "world_allocated", "source_fit_verified", "mission_ready", "native_acceptance",
                "catalog_mutated", "native_playable", "validation_passed", "scope", "resolved_setup",
                "partial_authoring_capture_for_missing_binding");
            var pins = result["input_pins"] as JObject;
            if (pins == null || pins.Count < 1 || pins.Count > 8192)
                throw new InvalidDataException("Bounded original compiler input pin set required");

            JObject request = FindRequest(pins, retainedPins);
            string catalog = (string)result["catalog_id"];
            if ((int?)result["schema_version"] != 1 || (string)result["kind"] != "SkirmishMissionAuthoringPreflightResult" ||
                (string)result["status"] != "ConfigurationCompiled" || (bool?)result["compiler_accepted"] != true ||
                (bool?)result["native_authoring_inputs_returned"] != true || !SupportedCatalog(catalog) ||
                result["seed"]?.Type != JTokenType.Integer || (long)result["seed"] < 1 || (long)result["seed"] > int.MaxValue ||
                (string)result["definition_id"] != "skirmish." + catalog.ToLowerInvariant() ||
                (result["compiler_reasons"] as JArray)?.Count != 0 || (result["authoring_capture_reasons"] as JArray)?.Count != 0 ||
                (bool?)result["partial_authoring_capture_for_missing_binding"] != false)
                throw new InvalidDataException("Exact accepted original compiler result required");
            foreach (string flag in new[] { "world_allocated", "source_fit_verified", "mission_ready", "native_acceptance", "catalog_mutated", "native_playable", "validation_passed" })
                if (result[flag]?.Type != JTokenType.Boolean || (bool)result[flag])
                    throw new InvalidDataException("Compiler result must retain false non-readiness flag: " + flag);

            string[] requestKeys = { "schema_version", "kind", "catalog_id", "seed", "size", "difficulty", "graph", "profiles",
                "campaign_source_path", "layout", "setup_matrix", "binding", "source_repository_root",
                "data_repository_root", "output_directory", "runtime_config", "resource_preset" };
            bool hasResourcePreset = request.Property("resource_preset") != null;
            if (request.Properties().Any(p => !requestKeys.Contains(p.Name)) ||
                request.Count != requestKeys.Length - (hasResourcePreset ? 0 : 1))
                throw new InvalidDataException("Exact compiler authoring request schema required");
            var profiles = request["profiles"] as JObject;
            ExactKeys(profiles, "objective", "army", "start", "economy", "upgrades", "intel", "roles");
            if ((int?)request["schema_version"] != 1 || (string)request["kind"] != "SkirmishMissionAuthoringPreflight" ||
                (string)request["catalog_id"] != catalog || request["seed"]?.Type != JTokenType.Integer ||
                (long)request["seed"] != (long)result["seed"] || (string)request["size"] != (string)result["size"] ||
                (string)request["difficulty"] != (string)result["difficulty"] ||
                (string)request["campaign_source_path"] != (string)physicalRequest["source_path"] ||
                (string)result["operation_map_id"] != (string)physicalRequest["operation_map_id"] ||
                (string)result["source_content_hash"] != (string)physicalRequest["content_hash"])
                throw new InvalidDataException("Compiler result, original request and selected Campaign source disagree");

            string root = Path.GetFullPath(projectRoot);
            if (Path.GetFullPath((string)request["source_repository_root"]) != root ||
                Path.GetFullPath(Path.Combine(root, (string)request["campaign_source_path"])) != Path.GetFullPath(Path.Combine(root, (string)physicalRequest["source_path"])) ||
                Path.GetFullPath((string)request["output_directory"]) != Path.GetDirectoryName(Path.GetFullPath(resultPath)) ||
                !Path.IsPathFullyQualified((string)request["data_repository_root"]) ||
                !Directory.Exists((string)request["data_repository_root"]))
                throw new InvalidDataException("Authoring request repository/source/output linkage changed");

            PinDescriptor(pins, request["graph"], 16 * 1024 * 1024, retainedPins);
            PinDescriptor(pins, request["layout"], 1024 * 1024, retainedPins);
            PinDescriptor(pins, request["setup_matrix"], 4 * 1024 * 1024, retainedPins);
            PinDescriptor(pins, request["binding"], 2 * 1024 * 1024, retainedPins);
            PinDescriptor(pins, request["runtime_config"], 1024 * 1024, retainedPins);
            string sourcePath = Path.GetFullPath(Path.Combine(root, (string)request["campaign_source_path"]));
            var sourcePin = PinRecord(pins, sourcePath, 4 * 1024 * 1024, retainedPins);
            if ((string)sourcePin["sha256"] != (string)physicalRequest["source_sha256"])
                throw new InvalidDataException("Compiler request source bytes differ from physical request source pin");
            foreach (var profile in profiles.Properties())
            {
                if (profile.Value.Type != JTokenType.String || !((string)profile.Value).StartsWith("Assets/", StringComparison.Ordinal) ||
                    ((string)profile.Value).Split('/').Contains(".."))
                    throw new InvalidDataException("Exact project-relative typed profile path required: " + profile.Name);
                PinRecord(pins, Path.Combine(root, (string)profile.Value), 4 * 1024 * 1024, retainedPins);
            }
            if (request["resource_preset"] != null)
            {
                var preset = request["resource_preset"] as JObject;
                ExactKeys(preset, "path", "sha256");
                string presetPath = Path.GetFullPath((string)preset["path"]);
                if (!Path.IsPathFullyQualified((string)preset["path"]) || !IsSha256((string)preset["sha256"]) ||
                    !presetPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidDataException("Exact project resource-preset descriptor required");
                var graph = ParseObject(ReadPinned((string)((JObject)request["graph"])["path"],
                    (string)((JObject)request["graph"])["sha256"], 16 * 1024 * 1024));
                var sourcePins = graph["source_pins"] as JObject;
                string relative = presetPath.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                var graphPin = sourcePins?.Properties().SingleOrDefault(p =>
                    Path.GetFullPath(p.Name).Equals(presetPath, StringComparison.Ordinal));
                if (graphPin == null || (string)graphPin.Value != (string)preset["sha256"] ||
                    !graph["nodes"].OfType<JObject>().Any(n => (string)n["source_path"] == relative && (string)n["source_type"] == "SkirmishPresetConfig"))
                    throw new InvalidDataException("Selected resource preset is not an exact graph-owned preset asset");
                var presetPin = PinRecord(pins, presetPath, 4 * 1024 * 1024, retainedPins);
                if ((string)presetPin["sha256"] != (string)preset["sha256"])
                    throw new InvalidDataException("Selected resource preset input pin differs");
            }
            return request;
        }

        static SkirmishResolvedSetup VerifyCurrentCompiledSetup(JObject request, JObject result, JObject inputPins, string root,
            IDictionary<string, string> retainedPins, out JArray hydratedAssetGuids)
        {
            SkirmishResolvedSetup selectedSetup = null;
            hydratedAssetGuids = null;
            var graphDescriptor = (JObject)request["graph"];
            var graphBytes = ReadPinned((string)graphDescriptor["path"], (string)graphDescriptor["sha256"], 16 * 1024 * 1024);
            var graph = ParseObject(graphBytes);
            var graphPins = graph["source_pins"] as JObject;
            if (graphPins == null || graphPins.Count < 1 || graphPins.Count > 4096)
                throw new InvalidDataException("Exact bounded authored source graph pins required");
            foreach (var pin in graphPins.Properties())
            {
                if (!Path.IsPathFullyQualified(pin.Name) || pin.Value.Type != JTokenType.String || !IsSha256((string)pin.Value))
                    throw new InvalidDataException("Exact authored source graph pin required: " + pin.Name);
                ReadPinned(pin.Name, (string)pin.Value, 4 * 1024 * 1024);
                Retain(retainedPins, Path.GetFullPath(pin.Name), (string)pin.Value);
            }

            string[] allowedTypes = {
                "SkirmishMissionViewProfile", "SkirmishScenarioDefinitionConfig", "SkirmishBattleCatalogConfig",
                "SkirmishObjectiveConfig", "SkirmishArmyProfileConfig", "SkirmishStartPackageConfig", "SkirmishEconomyConfig",
                "SkirmishUpgradeConfig", "SkirmishIntelConfig", "SkirmishRoleCatalogConfig", "SkirmishMapLayoutConfig",
                "SkirmishExpansionCatalogConfig", "SkirmishSizeConfig", "SkirmishDifficultyConfig", "SkirmishPublicationConfig",
                "SkirmishGroundStagingConfig", "SkirmishRoleOverlayCatalog", "OperationMapDefinition", "MapBuildingPlacementConfig",
                "MapSurfaceDataAsset", "GridAuthoringSceneConfigAsset", "SkirmishPresetConfig", "AIControllerConfig",
                "BuildingPlacementSystemConfig", "InitialUnitsSpawnerAuthoringConfig", "UnitPrefabRegistryAuthoringConfig",
                "UnitImpostorAtlasEntry", "AIPlanEntryStartupConfig", "ResourceExchangeRecipeConfigSet",
                "MapVehiclePlacementConfig", "MapVehiclePlacementConfigEntry"
            };
            var values = new AuthoredSourceConfigGraphMaterializer(graph, allowedTypes);
            SkirmishScenarioDefinitionConfig definition = null;
            try
            {
                var layout = values.LogicalLayout(ParseObject(ReadDescriptorBytes(request["layout"], inputPins, 1024 * 1024, retainedPins)));
                if (layout.BindingValueFormat != SkirmishBindingValueFormat.CanonicalBinaryV2)
                    throw new InvalidDataException("Canonical binary layout input required for original setup replay");
                var source = values.ByPath<OperationMapDefinition>((string)request["campaign_source_path"]);
                bool measured = layout.WorldBinding.HasMeasuredDeployments || layout.WorldBinding.BindAllConfigurations;
                if (measured && !SkirmishMeasuredLayoutBinder.MatchesExactCompilerSelectedMeasuredLayout(layout,
                    source, (string)result["layout_fingerprint"]))
                    throw new InvalidDataException("Exact compiler-selected measured layout/source binding required");
                definition = SourceTypedMissionConfiguration.ConfigureDeclared(values, (JObject)request["profiles"], layout, source,
                    (string)request["catalog_id"]);
                var binding = values.LogicalBinding(ParseObject(ReadDescriptorBytes(request["binding"], inputPins, 2 * 1024 * 1024, retainedPins)));
                if (binding.ValueFormat != layout.BindingValueFormat) throw new InvalidDataException("Logical binding value format differs from selected layout");
                definition.BindMissionLogicalBinding(binding);
                bool declaredDataOnly = SkirmishStructureInstanceBinding.AllowsSingleDeclaredRefinement(layout, binding.Variants);
                if (!measured && !declaredDataOnly)
                    throw new InvalidDataException("Declared compiler selection requires its actual one-layout logical binding");

                string matrixPath = (string)request["setup_matrix"]["path"];
                string matrixSha = (string)request["setup_matrix"]["sha256"];
                byte[] matrixBytes = ReadPinned(matrixPath, matrixSha, 4 * 1024 * 1024);
                List<SkirmishSetupMatrixRow> matrix;
                if (!SkirmishSetupMatrixTable.TryLoadFromText(Encoding.UTF8.GetString(matrixBytes), out matrix, out string matrixError))
                    throw new InvalidDataException("Original setup matrix rejected: " + matrixError);
                if (!Enum.TryParse((string)request["size"], out SkirmishSizeId size) || size == SkirmishSizeId.None ||
                    !Enum.TryParse((string)request["difficulty"], out SkirmishDifficultyId difficulty) || difficulty == SkirmishDifficultyId.None)
                    throw new InvalidDataException("Exact original compiler size/difficulty required");
                var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
                if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size, (int)request["seed"], manifest, matrix,
                    out var setup, out var reasons))
                    throw new InvalidDataException("Current original setup compiler rejected selected input: " +
                        string.Join(";", reasons.Select(reason => reason.Code + "/" + reason.Field + "/" + reason.Detail)));
                var resultSetup = result["resolved_setup"] as JObject;
                if (resultSetup == null || (uint?)resultSetup["setup_hash"] != setup.SetupHash ||
                    (string)result["layout_fingerprint"] != SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout) ||
                    (string)result["source_fingerprint"] != SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source, layout.BindingValueFormat) ||
                    !JToken.DeepEquals(resultSetup, ResolvedSetup(setup)))
                    throw new InvalidDataException("Current original compiler output differs from hash-pinned author result");
                selectedSetup = setup;
                hydratedAssetGuids = JArray.FromObject(values.HydratedAssetGuids);
            }
            finally
            {
#if UNITY_EDITOR
                if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
#endif
                values.Dispose();
            }
            return selectedSetup;
        }

        static byte[] ReadDescriptorBytes(JToken token, JObject inputPins, long maximum, IDictionary<string, string> retainedPins)
        {
            var descriptor = token as JObject;
            if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                descriptor["sha256"]?.Type != JTokenType.String || !Path.IsPathFullyQualified((string)descriptor["path"]))
                throw new InvalidDataException("Exact absolute typed compiler descriptor required");
            var pin = PinRecord(inputPins, (string)descriptor["path"], maximum, retainedPins);
            if ((string)pin["sha256"] != (string)descriptor["sha256"])
                throw new InvalidDataException("Typed compiler descriptor hash differs from result input pins");
            return ReadPinned((string)descriptor["path"], (string)descriptor["sha256"], maximum);
        }

        static JObject ResolvedSetup(SkirmishResolvedSetup setup)
        {
            return new JObject {
                ["setup_hash"] = setup.SetupHash, ["materials_each"] = setup.MaterialsEach,
                ["deadline_seconds"] = setup.DeadlineSeconds,
                ["enemy_policy"] = setup.EnemyPolicy == null ? null : new JObject {
                    ["response_min_seconds"] = setup.EnemyPolicy.ResponseMinSeconds,
                    ["response_max_seconds"] = setup.EnemyPolicy.ResponseMaxSeconds,
                    ["scout_min_seconds"] = setup.EnemyPolicy.ScoutMinSeconds,
                    ["scout_max_seconds"] = setup.EnemyPolicy.ScoutMaxSeconds,
                    ["opening_restraint_seconds"] = setup.EnemyPolicy.OpeningRestraintSeconds },
                ["forces"] = new JArray(setup.Forces.Select(f => new JObject { ["faction_id"] = f.FactionId,
                    ["role_id"] = f.RoleId, ["quantity"] = f.Quantity, ["supply_cost"] = f.SupplyCost })),
                ["structures"] = new JArray(setup.Structures.Select(s => new JObject { ["faction_id"] = s.FactionId,
                    ["structure_id"] = s.StructureId, ["spawn_anchor_id"] = s.SpawnAnchorId,
                    ["spawn_world_x"] = s.SpawnWorldX, ["spawn_world_z"] = s.SpawnWorldZ,
                    ["across_offset_metres"] = s.AcrossOffsetMetres, ["forward_offset_metres"] = s.ForwardOffsetMetres }))
            };
        }

        static JObject ParseObject(byte[] bytes) => JObject.Parse(Encoding.UTF8.GetString(bytes),
            new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });

        static JObject FindRequest(JObject pins, IDictionary<string, string> retainedPins)
        {
            var matches = new List<JObject>();
            foreach (var pin in pins.Properties())
            {
                if (!string.Equals(Path.GetExtension(pin.Name), ".json", StringComparison.OrdinalIgnoreCase)) continue;
                var record = pin.Value as JObject;
                if (record == null || record.Count != 2 || record["sha256"]?.Type != JTokenType.String ||
                    record["maximum_bytes"]?.Type != JTokenType.Integer) continue;
                long maximum = (long)record["maximum_bytes"];
                if (maximum < 1 || maximum > 1024 * 1024) continue;
                byte[] bytes = ReadPinned(pin.Name, (string)record["sha256"], maximum);
                JObject document;
                using (var text = new StringReader(Encoding.UTF8.GetString(bytes)))
                using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
                {
                    var token = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new InvalidDataException("Exactly one pinned compiler input JSON value required");
                    document = token as JObject;
                }
                if (document != null && (string)document["kind"] == "SkirmishMissionAuthoringPreflight")
                {
                    Retain(retainedPins, Path.GetFullPath(pin.Name), (string)record["sha256"]);
                    matches.Add(document);
                }
            }
            if (matches.Count != 1) throw new InvalidDataException("Exactly one hash-pinned original authoring request required");
            return matches[0];
        }

        static JObject PinDescriptor(JObject pins, JToken token, long maximum, IDictionary<string, string> retainedPins)
        {
            var descriptor = token as JObject;
            if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                descriptor["sha256"]?.Type != JTokenType.String || !Path.IsPathFullyQualified((string)descriptor["path"]))
                throw new InvalidDataException("Exact absolute compiler input descriptor required");
            var pin = PinRecord(pins, (string)descriptor["path"], maximum, retainedPins);
            if ((string)pin["sha256"] != (string)descriptor["sha256"])
                throw new InvalidDataException("Compiler request descriptor differs from original compiler input pin");
            return pin;
        }

        static JObject PinRecord(JObject pins, string path, long maximum, IDictionary<string, string> retainedPins)
        {
            path = Path.GetFullPath(path);
            var record = pins[path] as JObject;
            if (record == null || record.Count != 2 || record["sha256"]?.Type != JTokenType.String ||
                record["maximum_bytes"]?.Type != JTokenType.Integer || (long)record["maximum_bytes"] < 1 ||
                (long)record["maximum_bytes"] > maximum)
                throw new InvalidDataException("Exact bounded original compiler input pin required: " + path);
            string digest = (string)record["sha256"];
            ReadPinned(path, digest, (long)record["maximum_bytes"]);
            Retain(retainedPins, path, digest);
            return record;
        }

        static byte[] ReadPinned(string path, string digest, long maximum)
        {
            if (!Path.IsPathFullyQualified(path) || Path.GetFullPath(path) != path || !IsSha256(digest) ||
                AuthoredSourceConfigGraphMaterializer.HasSymlinkComponent(path))
                throw new InvalidDataException("Exact canonical nonsymlink absolute SHA256 pin required");
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 1 || info.Length > maximum || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bounded regular pinned compiler input required: " + path);
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.LongLength != info.Length || Hash(bytes) != digest) throw new InvalidDataException("Compiler input pin drift: " + path);
            return bytes;
        }

        static void ExactKeys(JObject value, params string[] keys)
        {
            if (value == null || value.Count != keys.Length || value.Properties().Any(p => !keys.Contains(p.Name)))
                throw new InvalidDataException("Exact compiler request/result schema required");
        }
        static bool SupportedCatalog(string value) => value != null && value.Length == 4 && value[0] == 'S' &&
            int.TryParse(value.Substring(1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int id) && id >= 1 && id <= 120;
        static bool IsSha256(string value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static void Retain(IDictionary<string, string> pins, string path, string digest)
        {
            if (pins.TryGetValue(path, out var prior) && prior != digest) throw new InvalidDataException("Conflicting retained compiler pin: " + path);
            pins[path] = digest;
        }
    }
}
