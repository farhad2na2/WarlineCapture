#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Narrow importer readback for the two serialized config overrides on one pinned prefab variant.
    // It does not load a scene, initialize a World, or run a Baker.
    public static class S002PrefabImporterIdentityObserver
    {
        private const string PrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab";
        private const string ConfigPath = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset";
        private const string UnitPrefabPath = "Assets/Game/Prefabs/Characters/Unit.prefab";
        private const string VehiclePrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab";
        private const string Marker = "[S002PrefabImporterIdentityObserver] result=Passed scope=ImporterObjectIdentityOnly";

        private sealed class ExpectedOverride
        {
            public readonly string TargetGuid;
            public readonly long TargetLocalId;
            public readonly string TargetPath;
            public ExpectedOverride(string guid, long localId, string path)
            { TargetGuid = guid; TargetLocalId = localId; TargetPath = path; }
        }

        private static readonly ExpectedOverride[] Expected =
        {
            new ExpectedOverride("5680e7910ed3741beba6f07116b6b03c", 106347591433935587L, UnitPrefabPath),
            new ExpectedOverride("9ce6998c034284d98a89b5badc6e2f24", 391336776749024018L, VehiclePrefabPath)
        };

        public static void Run()
        {
            string output = null;
            try
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string[] args = Environment.GetCommandLineArgs();
                output = Arg(args, "-s002-prefab-importer-output");
                string pinFile = Arg(args, "-s002-prefab-importer-pins");
                if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(pinFile))
                    throw new InvalidDataException("Explicit observer output and source pin manifest required.");
                output = Path.GetFullPath(output);
                pinFile = Path.GetFullPath(pinFile);
                RequireUnder(root, output, "Observer output");
                RequireUnder(root, pinFile, "Pin manifest");

                JObject manifest = JObject.Parse(File.ReadAllText(pinFile));
                if ((string)manifest["kind"] != "S002PrefabImporterIdentityPinManifest" ||
                    (string)manifest["scope"] != "ImporterObjectIdentityOnly")
                    throw new InvalidDataException("Exact importer identity pin manifest required.");
                JObject expectedPins = manifest["source_pins"] as JObject ??
                    throw new InvalidDataException("Source pin mapping required.");
                JObject before = HashPins(root, expectedPins);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null || PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.Variant)
                    throw new InvalidDataException("Exact imported variant prefab required.");
                JObject prefabIdentity = AssetIdentity(prefab, PrefabPath);
                var modifications = PrefabUtility.GetPropertyModifications(prefab);
                if (modifications == null)
                    throw new InvalidDataException("Unity returned null prefab property modifications.");

                var rows = new JArray();
                var seen = new HashSet<int>();
                var issues = new List<string>();
                foreach (var modification in modifications)
                {
                    if (modification == null || modification.propertyPath != "config") continue;
                    string targetGuid = null;
                    long targetId = 0;
                    bool targetIdentityFound = modification.target != null &&
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(modification.target, out targetGuid, out targetId);
                    string targetPath = modification.target == null ? null : AssetDatabase.GetAssetPath(modification.target);
                    int index = targetIdentityFound
                        ? Array.FindIndex(Expected, item => item.TargetGuid == targetGuid && item.TargetLocalId == targetId)
                        : -1;
                    if (!targetIdentityFound) issues.Add("Unity could not resolve a config override target identity.");
                    else if (index < 0) issues.Add("Unexpected config override target identity.");
                    else if (!seen.Add(index)) issues.Add("Duplicate config override target identity.");
                    if (index >= 0 && targetPath != Expected[index].TargetPath)
                        issues.Add("Config override target path does not match its pinned object identity.");
                    JObject configIdentity = DescribeIdentity(modification.objectReference);
                    if (configIdentity == null || (string)configIdentity["path"] != ConfigPath ||
                        (string)configIdentity["guid"] != "b0ffc9714da994462a1e48679b4863de" ||
                        (string)configIdentity["local_id"] != "11400000")
                        issues.Add("Config override object identity did not match the current pinned config asset.");
                    rows.Add(new JObject
                    {
                        ["property_path"] = modification.propertyPath,
                        ["target_guid"] = targetGuid,
                        ["target_local_id"] = targetIdentityFound ? targetId.ToString(CultureInfo.InvariantCulture) : null,
                        ["target_path"] = targetPath,
                        ["target_type"] = modification.target == null ? null : modification.target.GetType().FullName,
                        ["target_name"] = modification.target == null ? null : modification.target.name,
                        ["config_guid"] = configIdentity?["guid"],
                        ["config_local_id"] = configIdentity?["local_id"],
                        ["config_type"] = configIdentity?["type"],
                        ["config_path"] = configIdentity?["path"],
                        ["serialized_value"] = modification.value
                    });
                }
                if (seen.Count != Expected.Length)
                    issues.Add("Both exact serialized config override target identities are required.");
                var after = HashPins(root, expectedPins);
                if (!JToken.DeepEquals(before, after))
                    throw new InvalidDataException("Pinned source changed during AssetDatabase readback.");

                var result = new JObject
                {
                    ["kind"] = "S002PrefabImporterIdentityObservation",
                    ["status"] = issues.Count == 0 ? "Passed" : "Failed",
                    ["scope"] = "ImporterObjectIdentityOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderJoin_NoReadiness",
                    ["unity_version"] = Application.unityVersion,
                    ["prefab_path"] = PrefabPath,
                    ["prefab_asset"] = prefabIdentity,
                    ["prefab_asset_type"] = PrefabUtility.GetPrefabAssetType(prefab).ToString(),
                    ["expected_config_override_count"] = Expected.Length,
                    ["config_overrides"] = rows,
                    ["identity_issues"] = new JArray(issues),
                    ["source_pins_before"] = before,
                    ["source_pins_after"] = after,
                    ["observer_loaded_or_modified_scene"] = false,
                    ["observer_created_world"] = false,
                    ["observer_invoked_baker"] = false,
                    ["provider_join_complete"] = false,
                    ["mission_ready"] = false
                };
                using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream))
                    writer.Write(result.ToString(Formatting.Indented) + "\n");
                if (issues.Count == 0)
                {
                    Debug.Log(Marker);
                    EditorApplication.Exit(0);
                }
                else
                {
                    Debug.LogError("[S002PrefabImporterIdentityObserver] result=Failed identity_issues=" + String.Join(" | ", issues));
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Debug.LogError("[S002PrefabImporterIdentityObserver] result=Failed " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        private static JObject AssetIdentity(UnityEngine.Object value, string expectedPath)
        {
            if (value == null || AssetDatabase.GetAssetPath(value) != expectedPath ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId))
                throw new InvalidDataException("Imported asset identity did not match expected path: " + expectedPath);
            if (expectedPath == ConfigPath && !(value is UnitGridAuthoringConfig))
                throw new InvalidDataException("Expected the exact current UnitGridAuthoringConfig asset type.");
            if (guid != AssetDatabase.AssetPathToGUID(expectedPath) || localId <= 0)
                throw new InvalidDataException("Imported asset GUID/local ID mismatch: " + expectedPath);
            if (expectedPath == ConfigPath && (guid != "b0ffc9714da994462a1e48679b4863de" || localId != 11400000L))
                throw new InvalidDataException("Current variant config identity mismatch.");
            return new JObject { ["path"] = expectedPath, ["guid"] = guid,
                ["local_id"] = localId.ToString(CultureInfo.InvariantCulture) };
        }

        private static JObject DescribeIdentity(UnityEngine.Object value)
        {
            if (value == null) return null;
            string path = AssetDatabase.GetAssetPath(value);
            bool found = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localId);
            return new JObject { ["path"] = path, ["type"] = value.GetType().FullName,
                ["guid"] = found ? guid : null,
                ["local_id"] = found ? localId.ToString(CultureInfo.InvariantCulture) : null };
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
                string wanted = (string)property.Value;
                if (actual != wanted) throw new InvalidDataException("Source pin changed: " + relative);
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

        private static void RequireUnder(string root, string path, string label)
        {
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidDataException(label + " must remain inside the project.");
        }

        private static string Arg(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length) return null;
            return args[index + 1];
        }
    }
}
#endif
