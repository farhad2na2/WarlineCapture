#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Verifies Unity's asset identity layer for graph nodes and serialized
    // GUID/fileID links. It does not assert field-value parity or gameplay fit.
    internal static class CompilerSelectedUnityAssetReadback
    {
        internal static JObject Validate(JObject request, JObject compilerResult, string projectRoot,
            IDictionary<string, string> retainedPins)
        {
            return Validate(request, compilerResult, projectRoot, retainedPins, null);
        }

        internal static JObject Validate(JObject request, JObject compilerResult, string projectRoot,
            IDictionary<string, string> retainedPins, JArray hydratedAssetGuids)
        {
            string root = Path.GetFullPath(projectRoot);
            foreach (string relative in new[] {
                "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.PinnedPhysicalInputExport.cs",
                "Assets/Game/Scripts/Editor/Skirmish/CompilerSelectedAuthoringEvidence.cs",
                "Assets/Game/Scripts/Editor/Skirmish/CompilerSelectedUnityAssetReadback.cs",
                "Assets/Game/Scripts/Editor/Skirmish/SourceTypedMissionConfiguration.cs",
                "Assets/Game/Scripts/Editor/Skirmish/AuthoredSourceConfigGraphMaterializer.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupMatrixTable.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishMissionLogicalBindingConfig.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishScenarioDefinitionConfig.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishMapLayoutConfig.cs",
                "Assets/Game/Scripts/Configs/Skirmish/SkirmishStructureInstanceBinding.cs",
                "Assets/Game/Scripts/Configs/OperationMapDefinition.cs"
            })
            {
                string path = Path.GetFullPath(Path.Combine(root, relative));
                if (AuthoredSourceConfigGraphMaterializer.HasSymlinkComponent(path))
                    throw new InvalidDataException("Symlink in current compiler owner source path: " + path);
                Retain(retainedPins, path, HashFile(path));
            }
            var descriptor = request["graph"] as JObject;
            if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                descriptor["sha256"]?.Type != JTokenType.String)
                throw new InvalidDataException("Exact selected authored graph descriptor required");
            byte[] bytes = ReadPinned((string)descriptor["path"], (string)descriptor["sha256"], 16 * 1024 * 1024);
            JObject graph;
            using (var text = new StringReader(Encoding.UTF8.GetString(bytes)))
            using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
            {
                graph = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new InvalidDataException("Exactly one selected authored graph document required");
            }

            var sourcePins = graph["source_pins"] as JObject;
            var nodes = graph["nodes"] as JArray;
            if (sourcePins == null || nodes == null || nodes.Count < 1 || nodes.Count > 64)
                throw new InvalidDataException("Bounded selected authored graph required");
            HashSet<string> hydrated = null;
            if (hydratedAssetGuids != null)
            {
                hydrated = new HashSet<string>(StringComparer.Ordinal);
                foreach (JToken value in hydratedAssetGuids)
                    if (value.Type != JTokenType.String || !hydrated.Add((string)value))
                        throw new InvalidDataException("Exact unique hydrated source GUID set required");
                if (hydrated.Count < 1 || hydrated.Any(guid => !nodes.OfType<JObject>().Any(node => (string)node["asset_guid"] == guid)))
                    throw new InvalidDataException("Hydrated source GUID set must join captured graph nodes");
            }
            var identities = new Dictionary<string, Tuple<string, long>>(StringComparer.Ordinal);
            var identityRows = new JArray();
            foreach (JObject node in nodes)
            {
                ExactNode(node);
                string guid = (string)node["asset_guid"], relative = (string)node["source_path"], digest = (string)node["source_sha256"];
                if (Path.IsPathRooted(relative) || !relative.StartsWith("Assets/", StringComparison.Ordinal) ||
                    relative.Replace('\\', '/').Split('/').Any(part => part == ".."))
                    throw new InvalidDataException("Project-relative selected asset source path required");
                string absolute = Path.GetFullPath(Path.Combine(root, relative));
                var sourcePin = sourcePins[absolute];
                if (sourcePin?.Type != JTokenType.String || (string)sourcePin != digest ||
                    AuthoredSourceConfigGraphMaterializer.HasSymlinkComponent(absolute) || HashFile(absolute) != digest)
                    throw new InvalidDataException("Exact graph node path/SHA must join the actual graph source pin: " + relative);
                if (AssetDatabase.AssetPathToGUID(relative) != guid)
                    throw new InvalidDataException("AssetDatabase GUID differs from captured graph node: " + relative);
                var asset = AssetDatabase.LoadMainAssetAtPath(relative);
                if (asset == null || EditorUtility.IsDirty(asset) || AssetDatabase.GetAssetPath(asset) != relative ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string actualGuid, out long localId) ||
                    actualGuid != guid || localId <= 0 || !identities.TryAdd(guid, Tuple.Create(relative, localId)))
                    throw new InvalidDataException("AssetDatabase main-object GUID/local ID differs from graph node: " + relative);
                Retain(retainedPins, absolute, digest);
                identityRows.Add(new JObject { ["asset_guid"] = guid, ["source_path"] = relative,
                    ["source_sha256"] = digest, ["actual_local_file_id"] = localId });
            }

            int references = 0;
            var referenceRows = new JArray();
            foreach (JObject node in nodes)
                if (hydrated == null || hydrated.Contains((string)node["asset_guid"]))
                    VerifySerializedReferences(node["fields"], identities, sourcePins, root,
                        (string)node["source_path"], "fields", ref references, referenceRows);
            return new JObject { ["kind"] = "UnityAssetDatabaseCompilerGraphIdentityReadback", ["status"] = "VerifiedAssetIdentityOnly",
                ["graph_nodes"] = nodes.Count, ["hydrated_graph_nodes"] = hydrated?.Count ?? nodes.Count,
                ["excluded_unhydrated_graph_nodes"] = hydrated == null ? new JArray() : JArray.FromObject(nodes.OfType<JObject>()
                    .Where(node => !hydrated.Contains((string)node["asset_guid"]))
                    .Select(node => new JObject { ["asset_guid"] = (string)node["asset_guid"], ["source_path"] = (string)node["source_path"] })),
                ["asset_database_guid_local_id_rows"] = identityRows,
                ["serialized_guid_file_id_references"] = references, ["serialized_reference_targets"] = referenceRows,
                ["asset_field_value_parity_proven"] = false,
                ["native_acceptance"] = false, ["source_fit_certified"] = false, ["mission_ready"] = false };
        }

        static void ExactNode(JObject node)
        {
            string[] fields = { "asset_guid", "source_type", "source_path", "source_sha256", "fields", "scalar_spellings" };
            if (node == null || node.Count != fields.Length || node.Properties().Any(p => !fields.Contains(p.Name)) ||
                node["fields"] is not JObject || node["scalar_spellings"] is not JObject)
                throw new InvalidDataException("Exact six-field serialized source node required");
        }

        static void VerifySerializedReferences(JToken token, IDictionary<string, Tuple<string, long>> identities,
            JObject sourcePins, string root, string ownerPath, string fieldPath, ref int references, JArray referenceRows)
        {
            if (token is JObject record && record["fileID"] != null)
            {
                if (record["fileID"].Type != JTokenType.Integer || record.Count > 3 ||
                    record.Properties().Any(p => p.Name != "fileID" && p.Name != "guid" && p.Name != "type"))
                    throw new InvalidDataException("Exact serialized Unity reference record required");
                long fileId = (long)record["fileID"];
                if (fileId == 0) return;
                string guid = (string)record["guid"];
                if (references >= 8192) throw new InvalidDataException("Serialized Unity reference count exceeds the bounded readback limit");
                var targetRow = ResolvePinnedSerializedTarget(guid, fileId, identities, sourcePins, root, ownerPath, fieldPath);
                references++;
                referenceRows.Add(targetRow);
                return;
            }
            if (token is JObject obj) foreach (var property in obj.Properties())
                VerifySerializedReferences(property.Value, identities, sourcePins, root, ownerPath,
                    fieldPath + "." + property.Name, ref references, referenceRows);
            else if (token is JArray array) for (int index = 0; index < array.Count; index++)
                VerifySerializedReferences(array[index], identities, sourcePins, root, ownerPath,
                    fieldPath + "[" + index + "]", ref references, referenceRows);
        }

        static JObject ResolvePinnedSerializedTarget(string guid, long fileId,
            IDictionary<string, Tuple<string, long>> identities, JObject sourcePins, string root,
            string ownerPath, string fieldPath)
        {
            if (string.IsNullOrEmpty(guid))
                throw new InvalidDataException("Serialized Unity reference has a null GUID: owner=" + ownerPath +
                    " field=" + fieldPath + " fileID=" + fileId);
            string relative = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(relative) || !relative.StartsWith("Assets/", StringComparison.Ordinal) ||
                relative.Replace('\\', '/').Split('/').Any(part => part == ".."))
                throw new InvalidDataException("Serialized Unity reference has no project asset: owner=" + ownerPath +
                    " field=" + fieldPath + " guid=" + (guid ?? "<null>") + " fileID=" + fileId);
            string absolute = Path.GetFullPath(Path.Combine(root, relative));
            string assetSha = (string)sourcePins[absolute], metaSha = (string)sourcePins[absolute + ".meta"];
            if (!IsSha(assetSha) || !IsSha(metaSha) || HashFile(absolute) != assetSha || HashFile(absolute + ".meta") != metaSha)
                throw new InvalidDataException("Serialized Unity reference targets an unpinned asset; both asset and .meta SHA pins are required: owner=" +
                    ownerPath + " field=" + fieldPath + " path=" + relative + " guid=" + guid + " fileID=" + fileId);
            if (AssetDatabase.AssetPathToGUID(relative) != guid)
                throw new InvalidDataException("Serialized Unity reference GUID/path mismatch: owner=" + ownerPath +
                    " field=" + fieldPath + " path=" + relative + " guid=" + guid + " fileID=" + fileId);
            UnityEngine.Object matched = null;
            foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(relative) ?? Array.Empty<UnityEngine.Object>())
                if (candidate != null && EditorUtility.IsPersistent(candidate) && !EditorUtility.IsDirty(candidate) &&
                    AssetDatabase.GetAssetPath(candidate) == relative &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out string candidateGuid, out long candidateId) &&
                    candidateGuid == guid && candidateId == fileId)
                {
                    if (matched != null)
                        throw new InvalidDataException("Serialized Unity reference has ambiguous loaded object identity: owner=" + ownerPath +
                            " field=" + fieldPath + " path=" + relative + " guid=" + guid + " fileID=" + fileId);
                    matched = candidate;
                }
            if (matched == null)
            {
                identities.TryGetValue(guid, out var captured);
                throw new InvalidDataException("Serialized Unity GUID/fileID has no clean persistent AssetDatabase object in the pinned source: owner=" +
                    ownerPath + " field=" + fieldPath + " path=" + relative + " guid=" + guid + " fileID=" + fileId +
                    (captured == null ? string.Empty : " capturedMainFileID=" + captured.Item2));
            }
            return new JObject { ["owner_path"] = ownerPath, ["field_path"] = fieldPath, ["path"] = relative,
                ["guid"] = guid, ["file_id"] = fileId, ["object_type"] = matched.GetType().FullName,
                ["is_captured_graph_node"] = identities.ContainsKey(guid), ["asset_sha256"] = assetSha, ["meta_sha256"] = metaSha };
        }

        static byte[] ReadPinned(string path, string digest, long maximum)
        {
            if (!Path.IsPathFullyQualified(path) || Path.GetFullPath(path) != path || !IsSha(digest) ||
                AuthoredSourceConfigGraphMaterializer.HasSymlinkComponent(path))
                throw new InvalidDataException("Exact canonical absolute asset graph pin required");
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 1 || info.Length > maximum || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Bounded regular selected graph input required: " + path);
            byte[] data = File.ReadAllBytes(path);
            if (data.LongLength != info.Length || Hash(data) != digest) throw new InvalidDataException("Selected graph pin drift: " + path);
            return data;
        }

        static string HashFile(string path) => Hash(File.ReadAllBytes(path));
        static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static bool IsSha(string value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
        static void Retain(IDictionary<string, string> pins, string path, string digest)
        {
            if (pins.TryGetValue(path, out var prior) && prior != digest) throw new InvalidDataException("Conflicting Unity asset pin: " + path);
            pins[path] = digest;
        }
    }
}
#endif
