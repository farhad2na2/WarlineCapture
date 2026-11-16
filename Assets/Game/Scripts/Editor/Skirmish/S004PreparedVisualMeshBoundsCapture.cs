#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Asset-only readback of exact mesh identities referenced by the prepared-scene inventory.
    internal static class S004PreparedVisualMeshBoundsCapture
    {
        private const string InputKey = "WARLINE_S004_VISUAL_INVENTORY_INPUT";
        private const string OutputKey = "WARLINE_S004_MESH_BOUNDS_OUTPUT";

        public static void CaptureInventoryMeshBounds()
        {
            int exitCode = 1;
            string outputPath = null;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Capture requires Edit Mode.");
                string inputPath = RequiredAbsolute(InputKey);
                outputPath = RequiredFreshOutput();
                JObject inventory = JObject.Parse(File.ReadAllText(inputPath));
                if ((string)inventory["kind"] != "S004PreparedEntitiesVisualDependencyInventory" ||
                    (string)inventory["status"] != "PassedInventoryOnly")
                    throw new InvalidDataException("Unexpected input inventory kind or status.");
                var rows = new JArray();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                JObject prefabs = inventory["prefab_inventory"] as JObject;
                if (prefabs == null) throw new InvalidDataException("prefab_inventory is missing.");
                foreach (var prefabProperty in prefabs.Properties())
                {
                    string prefabGuid = prefabProperty.Name;
                    JObject prefab = prefabProperty.Value as JObject;
                    JArray roots = prefab?["source_local_roots"] as JArray;
                    if (roots == null) throw new InvalidDataException("source_local_roots missing for " + prefabGuid);
                    foreach (JObject root in roots)
                    {
                        JArray filters = root["mesh_filters"] as JArray;
                        if (filters == null) throw new InvalidDataException("mesh_filters missing for " + prefabGuid);
                        foreach (JObject filter in filters)
                        {
                            JObject meshRef = filter["mesh"] as JObject;
                            string guid = (string)meshRef?["guid"];
                            string localIdText = (string)meshRef?["file_id"];
                            long localId;
                            if (string.IsNullOrEmpty(guid) || !long.TryParse(localIdText, NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out localId))
                                throw new InvalidDataException("Invalid mesh GUID/local file ID in inventory.");
                            string key = guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
                            if (!seen.Add(key)) continue;
                            JObject measured = ResolveMesh(guid, localId, key);
                            measured["referencing_prefab_guid"] = prefabGuid;
                            measured["referencing_prefab_path"] = (string)prefab["prefab_path"];
                            measured["referencing_prefab_sha256"] = (string)prefab["asset_sha256"];
                            measured["source_root_file_id"] = (string)root["source_local_file_id"];
                            measured["mesh_filter_file_id"] = (string)filter["file_id"];
                            measured["game_object_file_id"] = (string)filter["game_object"];
                            rows.Add(measured);
                        }
                    }
                }
                if (rows.Count == 0) throw new InvalidDataException("Inventory contains no mesh references.");
                string inputSha = Sha256(File.ReadAllBytes(inputPath));
                var result = new JObject
                {
                    ["kind"] = "S004PreparedVisualMeshBoundsCapture",
                    ["schema_version"] = 1,
                    ["status"] = "Passed",
                    ["capture_mode"] = "AssetDatabaseExactGuidLocalId_NoSceneLoad_NoGameObjects_NoSimulation",
                    ["input_inventory_path"] = inputPath,
                    ["input_inventory_sha256"] = inputSha,
                    ["unique_mesh_count"] = rows.Count,
                    ["meshes"] = rows
                };
                WriteFresh(outputPath, result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
                Debug.Log("[S004PreparedVisualMeshBoundsCapture] result=Passed mesh_count=" + rows.Count + " output=" + outputPath);
                exitCode = 0;
            }
            catch (Exception e)
            {
                Debug.LogError("[S004PreparedVisualMeshBoundsCapture] result=Failed error=" + e.GetType().Name + ":" +
                    e.Message.Replace('\n', ' ').Replace('\r', ' '));
            }
            finally { EditorApplication.delayCall += () => EditorApplication.Exit(exitCode); }
        }

        private static JObject ResolveMesh(string guid, long localId, string key)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) throw new InvalidDataException("Mesh asset GUID unresolved: " + key);
            var matches = new List<Mesh>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                Mesh mesh = asset as Mesh;
                if (mesh == null) continue;
                string actualGuid; long actualId;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out actualGuid, out actualId) &&
                    actualGuid == guid && actualId == localId) matches.Add(mesh);
            }
            if (matches.Count != 1) throw new InvalidDataException("Expected exactly one Mesh for " + key + "; found " + matches.Count);
            Mesh resolved = matches[0];
            Bounds bounds = resolved.bounds;
            RequireFinite(bounds.center, key + " bounds center");
            RequireFinite(bounds.size, key + " bounds size");
            if (bounds.size.x < 0 || bounds.size.y < 0 || bounds.size.z < 0)
                throw new InvalidDataException("Negative bounds size for " + key);
            string verifiedGuid; long verifiedId;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(resolved, out verifiedGuid, out verifiedId) ||
                verifiedGuid != guid || verifiedId != localId)
                throw new InvalidDataException("Resolved mesh identity changed for " + key);
            return new JObject
            {
                ["guid"] = guid, ["local_file_id"] = localId,
                ["asset_path"] = path, ["mesh_name"] = resolved.name,
                ["asset_dependency_hash"] = AssetDatabase.GetAssetDependencyHash(path).ToString(),
                ["bounds_center"] = Vec(bounds.center), ["bounds_size"] = Vec(bounds.size),
                ["vertex_count"] = resolved.vertexCount, ["sub_mesh_count"] = resolved.subMeshCount
            };
        }

        private static void RequireFinite(Vector3 v, string label)
        {
            if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                float.IsNaN(v.z) || float.IsInfinity(v.z)) throw new InvalidDataException("Nonfinite " + label);
        }
        private static JArray Vec(Vector3 v) => new JArray(v.x, v.y, v.z);
        private static string RequiredAbsolute(string key)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value) || !Path.IsPathRooted(value)) throw new InvalidOperationException(key + " must be an absolute path.");
            return Path.GetFullPath(value);
        }
        private static string RequiredFreshOutput()
        {
            string path = RequiredAbsolute(OutputKey);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string evidenceRoot = Path.GetFullPath(Path.Combine(projectRoot, "Logs/SkirmishProduction")) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(evidenceRoot, StringComparison.Ordinal) || !Directory.Exists(Path.GetDirectoryName(path)) ||
                File.Exists(path) || Directory.Exists(path)) throw new InvalidOperationException("Fresh output under the project evidence directory required.");
            return path;
        }
        private static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes); var b = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash) b.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return b.ToString();
            }
        }
        private static void WriteFresh(string path, string contents)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(contents);
        }
    }
}
#endif
