#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Reads current imported provider assets only. It creates no scene instance, World, or baked entity.
    public static class S002CurrentForceUnitMetadataObserver
    {
        private const string Marker = "[S002CurrentForceUnitMetadataObserver] result=Passed scope=ImportedProviderMetadataOnly";
        private const string MetadataOwnerType = "Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper";
        private const string MetadataOwnerMethod = "TryGetMetadata";
        private const string Scope = "ImportedProviderMetadataOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderAdmission_NoReadiness";

        public static void Run()
        {
            string output = null;
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string[] args = Environment.GetCommandLineArgs();
                output = Path.GetFullPath(Arg(args, "-s002-current-force-metadata-output"));
                string pinsPath = Path.GetFullPath(Arg(args, "-s002-current-force-metadata-pins"));
                RequireUnder(root, output, "Observer output");
                RequireUnder(root, pinsPath, "Pin manifest");

                JObject manifest = JObject.Parse(File.ReadAllText(pinsPath));
                if ((string)manifest["kind"] != "S002CurrentForceUnitMetadataPinManifest" ||
                    (string)manifest["scope"] != "ImportedProviderMetadataOnly" ||
                    (string)manifest["t5_projection_sha256"] != (string)manifest["source_pins"]?[(string)manifest["t5_projection_path"]])
                    throw new InvalidDataException("Exact pinned T5 force projection manifest required.");
                JObject expectedPins = manifest["source_pins"] as JObject ??
                    throw new InvalidDataException("T5 and current owner source pins required.");
                JArray providers = manifest["providers"] as JArray ??
                    throw new InvalidDataException("Exact T5 force provider candidate rows required.");
                if (providers.Count != 8)
                    throw new InvalidDataException("Exactly eight distinct T5 current force provider keys required.");
                JObject before = HashPins(root, expectedPins);
                MethodInfo metadataOwner = ResolveMetadataOwner();

                var rows = new JArray();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var globalIssues = new List<string>();
                foreach (JObject provider in providers.OfType<JObject>())
                {
                    JObject row;
                    try { row = ObserveProvider(provider, metadataOwner, globalIssues); }
                    catch (Exception error)
                    {
                        string key = (string)provider["key"] ?? "<missing-key>";
                        globalIssues.Add(key + ": observer error: " + error.Message);
                        row = new JObject { ["key"] = key, ["status"] = "Failed",
                            ["issues"] = new JArray("observer error: " + error.Message) };
                    }
                    string rowKey = (string)provider["key"];
                    if (string.IsNullOrWhiteSpace(rowKey) || !seen.Add(rowKey))
                        globalIssues.Add("T5 provider key is missing or duplicated.");
                    rows.Add(row);
                }
                if (seen.Count != 8) globalIssues.Add("All eight distinct T5 provider keys are required.");

                JObject after = HashPins(root, expectedPins);
                if (!JToken.DeepEquals(before, after))
                    globalIssues.Add("Pinned source changed during importer readback.");
                bool passed = globalIssues.Count == 0 && rows.All(row => (string)row["status"] == "Passed");
                var result = new JObject
                {
                    ["kind"] = "S002CurrentForceUnitMetadataObservation",
                    ["status"] = passed ? "Passed" : "Failed",
                    ["scope"] = Scope,
                    ["unity_version"] = Application.unityVersion,
                    ["t5_projection_path"] = (string)manifest["t5_projection_path"],
                    ["t5_projection_sha256"] = (string)manifest["t5_projection_sha256"],
                    ["provider_count"] = rows.Count,
                    ["providers"] = rows,
                    ["issues"] = new JArray(globalIssues),
                    ["source_pins_before"] = before,
                    ["source_pins_after"] = after,
                    ["observer_loaded_or_modified_scene"] = false,
                    ["observer_created_world"] = false,
                    ["observer_invoked_baker"] = false,
                    ["provider_admission_performed"] = false,
                    ["native_capture_used"] = false,
                    ["source_fit_verified"] = false,
                    ["mission_ready"] = false
                };
                using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream))
                    writer.Write(result.ToString(Formatting.Indented) + "\n");
                if (passed)
                {
                    Debug.Log(Marker);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[S002CurrentForceUnitMetadataObserver] result=Failed " + String.Join(" | ", globalIssues));
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S002CurrentForceUnitMetadataObserver] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static JObject ObserveProvider(JObject provider, MethodInfo metadataOwner, List<string> globalIssues)
        {
            string key = RequiredString(provider, "key");
            string path = RequiredString(provider, "path");
            string expectedGuid = RequiredString(provider, "guid");
            long expectedRootId = RequiredLong(provider, "file_id");
            var candidates = provider["config_candidates"] as JArray ??
                throw new InvalidDataException("Pinned T5 config candidate list missing for " + key);
            var bindings = provider["owner_bindings"] as JArray ??
                throw new InvalidDataException("Pinned T5 owner binding list missing for " + key);
            var issues = new List<string>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                issues.Add("AssetDatabase returned null provider prefab.");
                return FailedProvider(key, path, issues);
            }
            string actualPath = AssetDatabase.GetAssetPath(prefab);
            bool rootIdentityFound = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string rootGuid, out long rootId);
            string assetGuid = AssetDatabase.AssetPathToGUID(path);
            if (actualPath != path) issues.Add("Imported provider asset path mismatch.");
            if (!rootIdentityFound || rootGuid != expectedGuid || assetGuid != expectedGuid)
                issues.Add("Imported provider GUID differs from the exact T5 reference.");
            if (!rootIdentityFound || rootId != expectedRootId)
                issues.Add("Imported provider root local ID differs from the T5 referenced local ID.");
            if (!string.Equals(prefab.name, key, StringComparison.Ordinal))
                issues.Add("Imported provider root name differs from the compiled runtime key.");

            UnitGridAuthoring[] authorings = prefab.GetComponents<UnitGridAuthoring>();
            if (authorings == null || authorings.Length != 1)
                issues.Add("Exactly one root UnitGridAuthoring component is required by the original metadata owner.");
            UnitGridAuthoring authoring = authorings != null && authorings.Length == 1 ? authorings[0] : null;
            JObject componentIdentity = DescribeIdentity(authoring);
            if (authoring == null) return FailedProvider(key, path, issues, prefab, rootGuid, rootId, componentIdentity);

            var serialized = new SerializedObject(authoring);
            serialized.Update();
            SerializedProperty configProperty = serialized.FindProperty("config");
            if (configProperty == null)
                issues.Add("Root UnitGridAuthoring.config serialized property is unavailable.");
            UnityEngine.Object configObject = configProperty == null ? null : configProperty.objectReferenceValue;
            JObject configIdentity = DescribeIdentity(configObject);
            if (configObject == null)
                issues.Add("Root UnitGridAuthoring effective config reference is null.");
            UnitGridAuthoringConfig unitConfig = configObject as UnitGridAuthoringConfig;
            if (unitConfig == null)
                issues.Add("Root UnitGridAuthoring config is not a UnitGridAuthoringConfig.");

            bool candidateMatch = false;
            if (configIdentity != null)
            {
                string configPath = (string)configIdentity["path"];
                string configGuid = (string)configIdentity["guid"];
                string configLocalId = (string)configIdentity["local_id"];
                foreach (JObject candidate in candidates.OfType<JObject>())
                {
                    if ((string)candidate["path"] == configPath && (string)candidate["guid"] == configGuid &&
                        (string)candidate["local_id"] == configLocalId)
                    { candidateMatch = true; break; }
                }
                if (!candidateMatch)
                    issues.Add("Effective config identity is not one of the exact T5-pinned candidate assets.");
            }

            JObject overrides = ReadConfigOverrideRows(prefab, bindings, issues);
            foreach (var overrideIssue in (JArray)overrides["issues"])
                issues.Add("Config override: " + (string)overrideIssue);
            JObject ownerMetadata = null;
            if (metadataOwner != null)
            {
                object[] call = { prefab, null };
                bool returned = (bool)metadataOwner.Invoke(null, call);
                if (!returned || call[1] == null)
                    issues.Add("Original TryGetMetadata owner returned false/null for the imported root prefab.");
                else
                {
                    var metadata = (BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata)call[1];
                    JObject transportIdentity = DescribeIdentity(metadata.ProductionTransportPrefab);
                    JObject configTransportIdentity = unitConfig == null
                        ? null : DescribeIdentity(unitConfig.ProductionTransportPrefab);
                    bool transportMatches = IdentityEquals(transportIdentity, configTransportIdentity);
                    if (!transportMatches)
                        issues.Add("Original owner transport reference differs from the effective pinned config transport reference.");
                    ownerMetadata = new JObject
                    {
                        ["production_duration_seconds"] = Float(metadata.ProductionDurationSeconds),
                        ["production_transport_prefab"] = transportIdentity,
                        ["production_transport_prefab_is_null"] = metadata.ProductionTransportPrefab == null,
                        ["is_air_unit"] = metadata.IsAirUnit,
                        ["production_transport_arrival_seconds"] = Float(metadata.ProductionTransportArrivalSeconds),
                        ["production_transport_hold_for_next_ready_seconds"] = Float(metadata.ProductionTransportHoldForNextReadySeconds),
                        ["production_transport_max_concurrent"] = metadata.ProductionTransportMaxConcurrent,
                        ["production_transport_requires_airport_runway"] = metadata.ProductionTransportRequiresAirportRunway,
                        ["production_transport_uses_runway_landing"] = metadata.ProductionTransportUsesRunwayLanding,
                        ["footprint_cells"] = new JObject { ["x"] = metadata.FootprintCells.x, ["y"] = metadata.FootprintCells.y },
                        ["transport_reference_matches_config"] = transportMatches
                    };
                }
            }
            else issues.Add("Original production metadata owner method is unavailable.");

            return new JObject
            {
                ["key"] = key,
                ["status"] = issues.Count == 0 ? "Passed" : "Failed",
                ["asset_path"] = actualPath,
                ["expected_asset_guid"] = expectedGuid,
                ["actual_asset_identity"] = DescribeIdentity(prefab),
                ["expected_root_local_id"] = expectedRootId.ToString(CultureInfo.InvariantCulture),
                ["prefab_asset_type"] = PrefabUtility.GetPrefabAssetType(prefab).ToString(),
                ["root_name"] = prefab.name,
                ["unit_grid_authoring_component_count"] = authorings.Length,
                ["unit_grid_authoring_type"] = authoring.GetType().FullName,
                ["unit_grid_authoring_identity"] = componentIdentity,
                ["effective_config_identity"] = configIdentity,
                ["effective_config_candidate_match"] = candidateMatch,
                ["config_override_rows"] = overrides["rows"],
                ["config_override_issues"] = overrides["issues"],
                ["original_production_metadata_owner"] = MetadataOwnerType + "." + MetadataOwnerMethod,
                ["production_metadata"] = ownerMetadata,
                ["issues"] = new JArray(issues)
            };
        }

        private static JObject ReadConfigOverrideRows(GameObject prefab, JArray expectedBindings,
            List<string> providerIssues)
        {
            var rows = new JArray();
            var issues = new JArray();
            var expectedBindingCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (JObject binding in expectedBindings.OfType<JObject>())
            {
                string bindingKey = BindingKey(
                    RequiredString(binding, "owner_path"), RequiredString(binding, "owner_guid"),
                    RequiredString(binding, "owner_local_id"), RequiredString(binding, "config_path"),
                    RequiredString(binding, "config_guid"), RequiredString(binding, "config_local_id"));
                if (expectedBindingCounts.ContainsKey(bindingKey))
                    issues.Add("T5 contains duplicate config owner bindings.");
                else expectedBindingCounts.Add(bindingKey, 0);
            }
            if (expectedBindingCounts.Count == 0)
                issues.Add("T5 config owner binding set is empty.");
            PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(prefab);
            if (modifications == null)
            {
                issues.Add("Unity returned null prefab property modifications.");
                providerIssues.Add("Prefab config override rows could not be inspected.");
                return new JObject { ["rows"] = rows, ["issues"] = issues };
            }
            foreach (var modification in modifications)
            {
                if (modification == null || modification.propertyPath != "config") continue;
                JObject target = DescribeIdentity(modification.target);
                JObject config = DescribeIdentity(modification.objectReference);
                bool bindingMatch = false;
                if (target != null && config != null)
                {
                    foreach (JObject binding in expectedBindings.OfType<JObject>())
                    {
                        if ((string)binding["owner_path"] == (string)target["path"] &&
                            (string)binding["owner_guid"] == (string)target["guid"] &&
                            (string)binding["owner_local_id"] == (string)target["local_id"] &&
                            (string)binding["config_path"] == (string)config["path"] &&
                            (string)binding["config_guid"] == (string)config["guid"] &&
                            (string)binding["config_local_id"] == (string)config["local_id"])
                        { bindingMatch = true; break; }
                    }
                }
                if (!bindingMatch)
                {
                    issues.Add("Imported config override target/reference pair does not match a T5 source binding.");
                    providerIssues.Add("Imported config override target/reference pair differs from T5 source binding.");
                }
                else
                {
                    string key = BindingKey((string)target["path"], (string)target["guid"],
                        (string)target["local_id"], (string)config["path"],
                        (string)config["guid"], (string)config["local_id"]);
                    expectedBindingCounts[key]++;
                }
                rows.Add(new JObject
                {
                    ["property_path"] = modification.propertyPath,
                    ["target_identity"] = target,
                    ["target_type"] = modification.target == null ? null : modification.target.GetType().FullName,
                    ["config_identity"] = config,
                    ["binding_match"] = bindingMatch,
                    ["serialized_value"] = modification.value
                });
            }
            if (rows.Count != expectedBindings.Count)
            {
                issues.Add("Imported config override row count differs from T5 binding count.");
                providerIssues.Add("Imported config override row count differs from T5 binding count.");
            }
            foreach (var entry in expectedBindingCounts)
            {
                if (entry.Value != 1)
                {
                    issues.Add("T5 config owner binding is missing or duplicated in imported override rows.");
                    providerIssues.Add("T5 config owner binding is missing or duplicated in imported override rows.");
                }
            }
            return new JObject { ["rows"] = rows, ["issues"] = issues };
        }

        private static string BindingKey(params string[] fields) => JsonConvert.SerializeObject(fields);

        private static MethodInfo ResolveMetadataOwner()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "Game.Composition");
            if (assembly == null) throw new InvalidOperationException("Original Game.Composition assembly is not loaded.");
            Type owner = assembly.GetType(MetadataOwnerType, throwOnError: true);
            MethodInfo method = owner.GetMethod(MetadataOwnerMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var expected = new[] { typeof(GameObject), typeof(BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata).MakeByRefType() };
            if (method == null || method.ReturnType != typeof(bool) ||
                !method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(expected))
                throw new MissingMethodException(MetadataOwnerType, MetadataOwnerMethod);
            return method;
        }

        private static JObject DescribeIdentity(UnityEngine.Object value)
        {
            if (value == null) return null;
            bool hasIdentity = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId);
            return new JObject
            {
                ["path"] = AssetDatabase.GetAssetPath(value),
                ["guid"] = hasIdentity ? guid : null,
                ["local_id"] = hasIdentity ? localId.ToString(CultureInfo.InvariantCulture) : null,
                ["type"] = value.GetType().FullName,
                ["identity_resolved"] = hasIdentity
            };
        }

        private static bool IdentityEquals(JObject left, JObject right) =>
            left == null ? right == null : right != null &&
            (string)left["path"] == (string)right["path"] &&
            (string)left["guid"] == (string)right["guid"] &&
            (string)left["local_id"] == (string)right["local_id"];

        private static JToken Float(float value) => !float.IsNaN(value) && !float.IsInfinity(value)
            ? new JValue(value)
            : new JValue(value.ToString("R", CultureInfo.InvariantCulture));

        private static JObject FailedProvider(string key, string path, List<string> issues,
            GameObject prefab = null, string rootGuid = null, long rootId = 0, JObject componentIdentity = null)
        {
            return new JObject
            {
                ["key"] = key,
                ["status"] = "Failed",
                ["asset_path"] = path,
                ["actual_asset_identity"] = DescribeIdentity(prefab),
                ["actual_asset_guid_from_api"] = rootGuid,
                ["actual_asset_local_id_from_api"] = rootId > 0 ? rootId.ToString(CultureInfo.InvariantCulture) : null,
                ["unit_grid_authoring_identity"] = componentIdentity,
                ["issues"] = new JArray(issues)
            };
        }

        private static JObject HashPins(string root, JObject expected)
        {
            var result = new JObject();
            foreach (var property in expected.Properties())
            {
                string relative = property.Name.Replace('\\', '/');
                if (Path.IsPathRooted(relative) || relative.Split('/').Any(part => part == ".."))
                    throw new InvalidDataException("Relative source pin path required.");
                string path = Path.GetFullPath(Path.Combine(root, relative));
                RequireUnder(root, path, "Source pin");
                string actual = HashFile(path);
                if (actual != (string)property.Value)
                    throw new InvalidDataException("Pinned source changed: " + relative);
                result[relative] = actual;
            }
            return result;
        }

        private static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static string RequiredString(JObject row, string name)
        {
            string value = (string)row[name];
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("T5 provider " + name + " required.");
            return value;
        }

        private static long RequiredLong(JObject row, string name)
        {
            long? value = (long?)row[name];
            if (!value.HasValue || value.Value <= 0) throw new InvalidDataException("T5 provider " + name + " required.");
            return value.Value;
        }

        private static void RequireUnder(string root, string path, string label)
        {
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(path) || !Path.GetFullPath(path).StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException(label + " must remain inside the project.");
        }

        private static string Arg(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length) throw new InvalidDataException("Missing argument " + name);
            return args[index + 1];
        }
    }
}
#endif
