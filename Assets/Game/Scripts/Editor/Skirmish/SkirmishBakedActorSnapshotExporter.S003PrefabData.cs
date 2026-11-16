#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string S003PrefabKeysRelative = "T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/prefab-keys.json";
        const string S003PrefabDescriptorReceiptRelative = "T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/receipt.json";
        const string S003MetadataRelative = "Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata.json";
        const string S003MetadataReceiptRelative = "Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata-receipt.json";
        const string S003GraphRelative = "Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json";
        const string S003PrefabKeysSha256 = "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b";
        const string S003PrefabDescriptorReceiptSha256 = "c676b2a726ea8134f72189d37e46ecb552e34eec8524b7b033c2464540eee181";
        const string S003MetadataSha256 = "2f857f965acd8743339ee73cf032ddf54e4ba7f07f278f4cde50d1e1876e91d3";
        const string S003MetadataReceiptSha256 = "aff49e834d5f2c74fe6b5a8c9666fdfeb297fb18b624ed81b5d5f4348db494f0";
        const string S003GraphSha256 = "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb";
        const string S003Run6ReceiptSha256 = "3d518a67a576bf223d66fb88adf181fc893bfb890200d902948d4a1a36b13609";
        const string S003Run6AuditSha256 = "52b5e97a494dc4adf5e5159e65fbe5921f57ad90b730012c5d0698f820abeda9";
        const string S003Run6GraphSha256 = "150cb503bb95dcdb1371e2c52c54764308b9bf1ad5e6305ae7ba3a68bf39cc5b";
        const long S003SetupHash = 2394754492L;

        // Original source/baker template closure only. Unsupported serialized values are retained
        // verbatim as diagnostics; their presence never produces replay or readiness authority.
        public static void RunS003FocusedPrefabReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int at = Array.IndexOf(args, name);
                if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Required argument " + name);
                return args[at + 1];
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS003FocusedPrefabReferenceData"));
            string keysPath = ExactInput(Arg("-skirmishS003FocusedPrefabKeys"), S003PrefabKeysRelative, projectRoot);
            string descriptorReceiptPath = ExactInput(Arg("-skirmishS003FocusedPrefabDescriptorReceipt"), S003PrefabDescriptorReceiptRelative, projectRoot);
            string metadataPath = ExactInput(Arg("-skirmishS003NativeMetadata"), S003MetadataRelative, projectRoot);
            string metadataReceiptPath = ExactInput(Arg("-skirmishS003NativeMetadataReceipt"), S003MetadataReceiptRelative, projectRoot);
            string keySha = Arg("-skirmishS003FocusedPrefabKeysSha");
            string outputFile = Path.Combine(output, "prefab-reference-data.json");
            if (File.Exists(outputFile)) throw new InvalidOperationException("Fresh S003 prefab reference output required");

            RequireHash(keySha == S003PrefabKeysSha256 && HashFile(keysPath) == S003PrefabKeysSha256,
                "Exact current S003 key descriptor required");
            RequireHash(HashFile(descriptorReceiptPath) == S003PrefabDescriptorReceiptSha256,
                "Exact current S003 key descriptor receipt required");
            RequireHash(HashFile(metadataPath) == S003MetadataSha256 && HashFile(metadataReceiptPath) == S003MetadataReceiptSha256,
                "Exact finalized current S003 native metadata capture and receipt required");

            JObject descriptorReceipt = ReadObject(descriptorReceiptPath);
            JObject metadata = ReadObject(metadataPath);
            JObject metadataReceipt = ReadObject(metadataReceiptPath);
            string[] keys = SkirmishFocusedPrefabKeySet.Read(keysPath, keySha);
            JObject context = metadata["context"] as JObject;
            JObject native = context?["metadata"] as JObject;
            JArray units = native?["units"] as JArray;
            string metadataDigest = MetadataDigest(native);
            RequireHash(keys.Length == 19 && units != null && units.Count == 19 &&
                (string)descriptorReceipt["kind"] == "S003FocusedPrefabKeyDescriptorReceipt" &&
                (string)descriptorReceipt["status"] == "PassedSourceOnly" &&
                (string)descriptorReceipt["descriptor"]?["sha256"] == keySha &&
                (int?)descriptorReceipt["descriptor"]?["key_count"] == 19 &&
                (string)descriptorReceipt["capture"]?["sha256"] == S003MetadataSha256 &&
                (string)descriptorReceipt["capture"]?["receipt_sha256"] == S003MetadataReceiptSha256 &&
                (string)descriptorReceipt["source_graph"]?["sha256"] == S003GraphSha256 &&
                (string)descriptorReceipt["run6_compiler_provenance"]?["receipt_sha256"] == S003Run6ReceiptSha256 &&
                (string)descriptorReceipt["run6_compiler_provenance"]?["post_run_audit_sha256"] == S003Run6AuditSha256 &&
                (string)descriptorReceipt["run6_compiler_provenance"]?["source_graph_sha256"] == S003Run6GraphSha256 &&
                (string)context?["catalog_id"] == "S003" && (string)context?["size"] == "Standard" &&
                (string)context?["difficulty"] == "Regular" && (int?)context?["seed"] == 104732 &&
                (long?)context?["setup_hash"] == S003SetupHash &&
                (string)context?["metadata_key_sha256"] == metadataDigest &&
                (string)descriptorReceipt["compiler_target"]?["metadata_key_sha256"] == metadataDigest,
                "Current capture, descriptor, graph, run-6 provenance, and compiler target must agree");
            RequireHash((string)metadata["kind"] == "NativeS003OriginalCompilerFactoryMetadataCapture" &&
                (string)metadata["scope"] == "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld" &&
                (bool?)metadata["utility_world_created"] == true && (bool?)metadata["utility_world_disposed"] == true &&
                (bool?)metadata["mission_world_created"] == false && (bool?)metadata["native_context_qualified"] == false &&
                (bool?)metadata["source_fit_certified"] == false && (bool?)metadata["mission_ready"] == false &&
                (bool?)descriptorReceipt["mission_world_created"] == false &&
                (bool?)descriptorReceipt["native_context_qualified"] == false &&
                (bool?)descriptorReceipt["source_fit_certified"] == false && (bool?)descriptorReceipt["mission_ready"] == false &&
                (string)metadataReceipt["kind"] == "S003OriginalCompilerFactoryMetadataCaptureReceipt" &&
                (string)metadataReceipt["validation_status"] == "Passed" && (int?)metadataReceipt["wrapper_exit_code"] == 0 &&
                (int?)metadataReceipt["required_marker_count"] == 1 &&
                (int?)metadataReceipt["seed"] == 104732 && (long?)metadataReceipt["setup_hash"] == S003SetupHash &&
                (string)metadataReceipt["capture_sha256"] == S003MetadataSha256 &&
                (string)metadataReceipt["graph_sha256"] == S003GraphSha256 &&
                (string)metadataReceipt["run6_receipt_sha256"] == S003Run6ReceiptSha256 &&
                (string)metadataReceipt["run6_post_run_audit_sha256"] == S003Run6AuditSha256 &&
                (string)metadataReceipt["run6_graph_sha256"] == S003Run6GraphSha256 &&
                (bool?)metadataReceipt["utility_world_created"] == true &&
                (bool?)metadataReceipt["utility_world_disposed"] == true &&
                (bool?)metadataReceipt["mission_world_created"] == false &&
                (bool?)metadataReceipt["mission_ready"] == false,
                "Source-only capture scope and utility World disclosure required");
            RequireHash(Path.GetFullPath((string)metadata["source_graph"]?["path"]) ==
                Path.GetFullPath(Path.Combine(projectRoot, S003GraphRelative)) &&
                (string)metadata["source_graph"]?["sha256"] == S003GraphSha256 &&
                Path.GetFullPath((string)descriptorReceipt["source_graph"]?["path"]) ==
                Path.GetFullPath(Path.Combine(projectRoot, S003GraphRelative)),
                "Exact current graph-r5 path required");

            for (int i = 0; i < keys.Length; i++)
            {
                JObject unit = units[i] as JObject;
                JObject prefab = unit?["prefab"] as JObject;
                JObject identity = (descriptorReceipt["prefab_identities"] as JArray)?[i] as JObject;
                string key = keys[i];
                RequireHash((int?)unit?["index"] == i && (bool?)unit?["has_definition_metadata"] == true &&
                    (bool?)unit?["has_production_metadata"] == true && (string)prefab?["key"] == key &&
                    (string)identity?["key"] == key && (int?)identity?["index"] == i &&
                    (string)identity?["guid"] == (string)prefab?["guid"] &&
                    (long?)identity?["local_id"] == (long?)prefab?["local_id"] &&
                    (string)identity?["path"] == (string)prefab?["path"],
                    "Ordered descriptor key identity differs from current metadata row " + i);
                string assetPath = (string)prefab["path"];
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                string assetGuid;
                long localId;
                if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out assetGuid, out localId) ||
                    !StringComparer.Ordinal.Equals(assetGuid, (string)prefab["guid"]) || localId != (long)prefab["local_id"])
                    throw new InvalidOperationException("Current Unity asset identity differs from descriptor: " + key);
            }
            if (!JToken.DeepEquals(descriptorReceipt["descriptor"]?["keys_in_metadata_order"], JArray.FromObject(keys)))
                throw new InvalidOperationException("Descriptor receipt ordered key list differs from key file");

            // Pin the complete prior capture domain plus the actual compiled project source closure.
            var pins = SourcePins(SourceScene);
            foreach (JProperty pin in ((JObject)metadata["source_pins"]).Properties())
            {
                string path = pin.Name;
                if (!Path.IsPathRooted(path)) path = Path.GetFullPath(Path.Combine(projectRoot, path));
                string sha = (string)pin.Value;
                if (pins.TryGetValue(path, out string current) && !StringComparer.Ordinal.Equals(current, sha))
                    throw new InvalidOperationException("Capture/current source pin disagreement: " + path);
                pins[path] = sha;
            }
            pins[keysPath] = S003PrefabKeysSha256;
            pins[descriptorReceiptPath] = S003PrefabDescriptorReceiptSha256;
            pins[metadataPath] = S003MetadataSha256;
            pins[metadataReceiptPath] = S003MetadataReceiptSha256;
            VerifyPins(pins);

            World originalDefault = World.DefaultGameObjectInjectionWorld;
            int initialWorldCount = World.All.Count;
            JArray unsupported = new JArray();
            JObject result;
            string templateNamespace = Guid.NewGuid().ToString("N");
            using (var world = new World("S003FocusedAuthoredPrefabReferenceOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during S003 prefab export");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                string sceneGuid = AssetDatabase.AssetPathToGUID(SourceScene);
                if (string.IsNullOrEmpty(sceneGuid)) throw new InvalidOperationException("Original source registry SubScene missing");
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(sceneGuid),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Original registry source scene did not finish streaming");
                EntityManager em = world.EntityManager;
                var roots = OpeningPrefabRoots(em, keys);
                var orderedRoots = new JObject();
                foreach (string key in keys)
                {
                    string canonical = roots.Keys.Single(k => StringComparer.OrdinalIgnoreCase.Equals(k, key));
                    if (!StringComparer.Ordinal.Equals(canonical, key))
                        throw new InvalidOperationException("Exact S003 source prefab key spelling required: " + key);
                    orderedRoots[key] = roots[canonical];
                }
                JArray entities = CaptureTemplateClosure(em, keys.Select(k => roots[k]), unsupported);
                result = new JObject
                {
                    ["schema_version"] = 1,
                    ["kind"] = "AuthoredSkirmishPrefabReferenceData",
                    ["authority"] = "ActualSourceAssetsAndNativeBakers_NotMissionState",
                    ["mission_id"] = "S003",
                    ["source_scenes"] = new JArray(SourceScene),
                    ["source_pins"] = JObject.FromObject(pins),
                    ["unity_version"] = Application.unityVersion,
                    ["template_namespace_id"] = templateNamespace,
                    ["requested_prefab_keys"] = new JArray(keys),
                    ["requested_prefab_keys_sha256"] = S003PrefabKeysSha256,
                    ["source_key_descriptor_receipt_sha256"] = S003PrefabDescriptorReceiptSha256,
                    ["native_metadata_capture_sha256"] = S003MetadataSha256,
                    ["native_metadata_capture_receipt_sha256"] = S003MetadataReceiptSha256,
                    ["compiler_seed"] = 104732,
                    ["compiler_setup_hash"] = S003SetupHash,
                    ["opening_prefabs"] = orderedRoots,
                    ["entities"] = entities,
                    ["unsupported_values"] = unsupported,
                    ["unsupported_values_preserved_verbatim"] = true,
                    ["diagnostic_status"] = "CollectedSourceOnly",
                    ["replay_ready"] = false,
                    ["fresh_world_allocator_exported"] = false,
                    ["full_mission_initialized"] = false,
                    ["utility_world_created"] = true,
                    ["utility_world_disposed"] = true,
                    ["mission_world_created"] = false,
                    ["native_visual_readiness"] = false,
                    ["source_fit_certified"] = false,
                    ["native_context_qualified"] = false,
                    ["mission_ready"] = false,
                    ["geometry_exported"] = false,
                    ["consumer_rule"] = "Diagnostic source-template closure only. Preserve every unsupported value exactly and reject any unsupported value required by a consumer. This artifact does not authorize import, replay, gameplay, source-fit, visual readiness, or mission readiness."
                };
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld) || World.All.Count != initialWorldCount)
                throw new InvalidOperationException("Default/gameplay World state changed during S003 source capture");
            VerifyPins(pins);
            byte[] bytes = Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Bounded S003 prefab reference output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);
            Debug.Log("[SkirmishS003FocusedPrefabReferenceData] result=Collected prefabs=" + keys.Length +
                " entities=" + ((JArray)result["entities"]).Count + " unsupported=" + unsupported.Count +
                " utilityWorld=True missionWorld=False replayReady=False missionReady=False");
        }

        static string ExactInput(string supplied, string relative, string projectRoot)
        {
            if (!Path.IsPathRooted(supplied)) throw new ArgumentException("Absolute pinned input path required: " + relative);
            string actual = Path.GetFullPath(supplied);
            string expected = Path.GetFullPath(Path.Combine(projectRoot, relative));
            if (!StringComparer.Ordinal.Equals(actual, expected)) throw new ArgumentException("Exact current pinned input path required: " + relative);
            return actual;
        }

        static JObject ReadObject(string path)
        {
            using (var reader = new JsonTextReader(new StringReader(File.ReadAllText(path))) { DateParseHandling = DateParseHandling.None })
                return JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        static void RequireHash(bool valid, string message)
        {
            if (!valid) throw new InvalidDataException(message);
        }

        static void VerifyPins(IDictionary<string, string> pins)
        {
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S003 prefab source changed: " + pin.Key);
        }

        static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
#endif
