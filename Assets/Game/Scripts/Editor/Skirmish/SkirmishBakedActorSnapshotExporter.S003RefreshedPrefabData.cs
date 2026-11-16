#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        // Data-only capture: unsupported values remain in the output and never confer replay readiness.
        public static void RunS003RefreshedPrefabReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(name); return args[i + 1]; }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS003RefreshedPrefabData"));
            string keysPath = Path.GetFullPath(Path.Combine(root, "T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/prefab-keys.json"));
            string keyReceiptPath = Path.GetFullPath(Path.Combine(root, "T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/receipt.json"));
            string metadataPath = Path.GetFullPath(Path.Combine(root, "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"));
            string metadataReceiptPath = Path.GetFullPath(Path.Combine(root, "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"));
            const string keySha = "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b";
            const string keyReceiptSha = "6209d48463d318af7212716ce36ff5381d0ca5ef89655a84ec1424f01b72e8ff";
            const string historicalMetadataSha = "7d039dd9a09fdff34e88682be17ce648b14dc926637915ec5748b243d1de2433";
            const string historicalMetadataReceiptSha = "6ee96300ba60bd918da52ee58d7e67aa50267bee773df6469776b689560f70a7";
            string historicalMetadataPath=metadataPath;
            bool independentlyAudited=Array.IndexOf(args,"-skirmishS003CurrentMetadata")>=0;
            string metadataSha=historicalMetadataSha,metadataReceiptSha=historicalMetadataReceiptSha;
            if(independentlyAudited)
            {
                metadataPath=Path.GetFullPath(Arg("-skirmishS003CurrentMetadata"));
                metadataSha=Arg("-skirmishS003CurrentMetadataSha");
                metadataReceiptPath=Path.GetFullPath(Arg("-skirmishS003CurrentMetadataAudit"));
                metadataReceiptSha=Arg("-skirmishS003CurrentMetadataAuditSha");
                if(HashFile(historicalMetadataPath)!=historicalMetadataSha)
                    throw new InvalidDataException("Historical key authority changed");
            }
            if (HashFile(keysPath) != keySha || HashFile(keyReceiptPath) != keyReceiptSha || HashFile(metadataPath) != metadataSha || HashFile(metadataReceiptPath) != metadataReceiptSha)
                throw new InvalidDataException("Exact checked S003 factory evidence required");
            var keyReceipt = ReadObject(keyReceiptPath); var metadata = ReadObject(metadataPath); var metadataReceipt = ReadObject(metadataReceiptPath);
            string[] keys = SkirmishFocusedPrefabKeySet.Read(keysPath, keySha);
            var units = metadata["context"]?["metadata"]?["units"] as JArray; var identities = keyReceipt["ordered_prefab_identities"] as JArray;
            bool receiptValid=independentlyAudited
                ? (string)metadataReceipt["status"]=="Passed" &&
                  (string)metadataReceipt["scope"]=="CurrentDataOnlyCapture_IndependentAudit_NotHistoricalFinalizerQualification" &&
                  (string)metadataReceipt["capture_sha256"]==metadataSha && (int?)metadataReceipt["wrapper_exit_observed"]==0 &&
                  (int?)metadataReceipt["marker_count"]==1 && (metadataReceipt["source_pin_mismatches"] as JArray)?.Count==0 &&
                  (bool?)metadataReceipt["native_context_qualified"]==false && (bool?)metadataReceipt["mission_ready"]==false &&
                  JToken.DeepEquals(metadata["context"],ReadObject(historicalMetadataPath)["context"])
                : (string)metadataReceipt["status"]=="PassedDataOnlyCapture" && (string)metadataReceipt["capture"]?["sha256"]==metadataSha;
            if (keys.Length != 19 || units == null || units.Count != keys.Length || identities == null || identities.Count != keys.Length ||
                (string)keyReceipt["status"] != "PassedSourceOnly" || !receiptValid ||
                (string)keyReceipt["capture"]?["sha256"] != historicalMetadataSha ||
                (int?)metadata["context"]?["seed"] != 104732 || (long?)metadata["context"]?["setup_hash"] != 2394754492L ||
                (bool?)metadata["mission_ready"] != false || (bool?)metadata["source_fit_certified"] != false)
                throw new InvalidDataException("Exact source-only S003 factory target required");
            for (int i = 0; i < keys.Length; i++)
            {
                var prefab = units[i]?["prefab"]; var identity = identities[i];
                if ((int?)units[i]?["index"] != i || (string)prefab?["key"] != keys[i] || (string)identity?["key"] != keys[i] ||
                    (string)identity?["guid"] != (string)prefab?["guid"] || (long?)identity?["local_id"] != (long?)prefab?["local_id"] ||
                    (string)identity?["path"] != (string)prefab?["path"])
                    throw new InvalidDataException("Native prefab identity mismatch: " + i);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>((string)prefab["path"]);
                if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId) ||
                    guid != (string)prefab["guid"] || localId != (long)prefab["local_id"])
                    throw new InvalidDataException("Current AssetDatabase prefab identity mismatch: " + keys[i]);
            }
            var pins = SourcePins(SourceScene);
            foreach (JProperty pin in ((JObject)metadata["source_pins"]).Properties())
            {
                string path = Path.IsPathRooted(pin.Name) ? pin.Name : Path.GetFullPath(Path.Combine(root, pin.Name));
                string hash = (string)pin.Value;
                if (pins.TryGetValue(path, out string actual) && actual != hash) throw new InvalidDataException("Source pin conflict: " + path);
                pins[path] = hash;
            }
            pins[keysPath] = keySha; pins[keyReceiptPath] = keyReceiptSha; pins[metadataPath] = metadataSha; pins[metadataReceiptPath] = metadataReceiptSha;
            if(independentlyAudited) pins[historicalMetadataPath]=historicalMetadataSha;
            VerifyPins(pins);
            var original = World.DefaultGameObjectInjectionWorld; int count = World.All.Count; var unsupported = new JArray(); JObject result;
            using (var world = new World("S003RefreshedPrefabCaptureOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems) if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal)) throw new InvalidOperationException("Gameplay system forbidden");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)), new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update(); if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene)) throw new InvalidOperationException("Source scene not loaded");
                var roots = OpeningPrefabRoots(world.EntityManager, keys); var ordered = new JObject();
                foreach (string key in keys) { string exact = roots.Keys.Single(k => StringComparer.OrdinalIgnoreCase.Equals(k, key)); if (exact != key) throw new InvalidDataException("Prefab key casing changed"); ordered[key] = roots[exact]; }
                var entities = CaptureTemplateClosure(world.EntityManager, keys.Select(k => roots[k]), unsupported);
                result = new JObject { ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishPrefabReferenceData", ["authority"] = "ActualSourceAssetsAndNativeBakers_NotMissionState", ["mission_id"] = "S003", ["source_scenes"] = new JArray(SourceScene), ["source_pins"] = JObject.FromObject(pins), ["unity_version"] = Application.unityVersion, ["template_namespace_id"] = Guid.NewGuid().ToString("N"), ["requested_prefab_keys"] = new JArray(keys), ["requested_prefab_keys_sha256"] = keySha, ["source_key_descriptor_receipt_sha256"] = keyReceiptSha, ["native_metadata_capture_sha256"] = metadataSha, ["native_metadata_capture_receipt_sha256"] = metadataReceiptSha, ["compiler_seed"] = 104732, ["compiler_setup_hash"] = 2394754492L, ["opening_prefabs"] = ordered, ["entities"] = entities, ["unsupported_values"] = unsupported, ["unsupported_values_preserved_verbatim"] = true, ["diagnostic_status"] = "CollectedSourceOnly", ["replay_ready"] = false, ["fresh_world_allocator_exported"] = false, ["full_mission_initialized"] = false, ["utility_world_created"] = true, ["utility_world_disposed"] = true, ["mission_world_created"] = false, ["native_visual_readiness"] = false, ["source_fit_certified"] = false, ["native_context_qualified"] = false, ["mission_ready"] = false, ["geometry_exported"] = false };
            }
            if(independentlyAudited) {
                result["native_metadata_capture_receipt_sha256"]=null;
                result["independent_metadata_audit_sha256"]=metadataReceiptSha;
                result["key_metadata_epoch"]="HistoricalKeys_CurrentContextComparedExactly";
                result["historical_key_metadata_sha256"]=historicalMetadataSha;
            }
            if (!ReferenceEquals(original, World.DefaultGameObjectInjectionWorld) || World.All.Count != count) throw new InvalidOperationException("World cleanup mismatch");
            VerifyPins(pins); Directory.CreateDirectory(output); string file = Path.Combine(output, "prefab-reference-data.json");
            byte[] bytes = Encoding.UTF8.GetBytes(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Capture too large");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(bytes, 0, bytes.Length);
            Debug.Log("[S003RefreshedPrefabReference] result=Collected roots=" + keys.Length + " entities=" + ((JArray)result["entities"]).Count + " unsupported=" + unsupported.Count + " world=0 readiness=0");
        }
    }
}
#endif
