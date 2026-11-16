#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        // Asset-only capture; unresolved VFX remain in the ledger and never grant replay readiness.
        public static void RunS004MissingPrefabReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(name); return args[i + 1]; }
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS004MissingPrefabData"));
            string keyPath = Path.Combine(root, "T Logs/SkirmishProduction/s004-missing-four-focused-keys-20261103-T1/keys.json");
            string inventoryPath = Path.Combine(root, "T Logs/SkirmishProduction/s004-missing-four-focused-keys-20261103-T1/receipt.json");
            const string keyHash = "8bce22a0465b8bdfcfa3f12029c7ead7b9fc37109da7ba63112a00ed003c97b0";
            const string inventoryHash = "d4948d2557e631678cef72cd29c595e7d73f5caff0562921e1fbff78e02fc74c";
            if (HashFile(keyPath) != keyHash || HashFile(inventoryPath) != inventoryHash) throw new InvalidDataException("Exact retained source inventory required");
            string[] keys = SkirmishFocusedPrefabKeySet.Read(keyPath, keyHash);
            var inventory = JObject.Parse(File.ReadAllText(inventoryPath));
            var rows = inventory["source_assets"] as JArray;
            if (keys.Length != 4 || rows == null || rows.Count != keys.Length || keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
                throw new InvalidDataException("Four distinct inventoried source keys required");
            var pins = SourcePins(SourceScene);
            pins[keyPath] = keyHash; pins[inventoryPath] = inventoryHash;
            for (int i = 0; i < keys.Length; i++)
            {
                var row = rows[i]; string path = (string)row["path"];
                if ((string)row["key"] != keys[i] || !Path.IsPathRooted(path) || !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidDataException("Exact source inventory order and path required");
                string relative = path.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(relative);
                if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId) ||
                    guid != (string)row["guid"] || localId != (long)row["local_id"] || HashFile(path) != (string)row["sha256"] ||
                    HashFile(path + ".meta") != (string)row["meta_sha256"])
                    throw new InvalidDataException("Source prefab identity or bytes changed: " + keys[i]);
                pins[path] = (string)row["sha256"]; pins[path + ".meta"] = (string)row["meta_sha256"];
            }
            VerifyPins(pins);
            var defaultWorld = World.DefaultGameObjectInjectionWorld; int worldCount = World.All.Count;
            var unsupported = new JArray(); JObject result;
            using (var world = new World("S004MissingPrefabCaptureOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems) if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal)) throw new InvalidOperationException("Gameplay system forbidden");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)), new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update(); if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene)) throw new InvalidOperationException("Registry source scene not loaded");
                var roots = OpeningPrefabRoots(world.EntityManager, keys); var ordered = new JObject();
                foreach (string key in keys) { string exact = roots.Keys.Single(k => StringComparer.OrdinalIgnoreCase.Equals(k, key)); if (exact != key) throw new InvalidDataException("Prefab key case changed"); ordered[key] = roots[exact]; }
                var entities = CaptureTemplateClosure(world.EntityManager, keys.Select(k => roots[k]), unsupported);
                result = new JObject { ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishPrefabReferenceData", ["authority"] = "ActualSourceAssetsAndNativeBakers_NotMissionState", ["source_scenes"] = new JArray(SourceScene), ["source_pins"] = JObject.FromObject(pins), ["unity_version"] = Application.unityVersion, ["template_namespace_id"] = Guid.NewGuid().ToString("N"), ["requested_prefab_keys"] = new JArray(keys), ["requested_prefab_keys_sha256"] = keyHash, ["source_key_inventory_sha256"] = inventoryHash, ["opening_prefabs"] = ordered, ["entities"] = entities, ["unsupported_values"] = unsupported, ["unsupported_values_preserved_verbatim"] = true, ["diagnostic_status"] = "CollectedSourceOnly", ["replay_ready"] = false, ["fresh_world_allocator_exported"] = false, ["full_mission_initialized"] = false, ["native_visual_readiness"] = false, ["source_fit_certified"] = false, ["mission_ready"] = false, ["geometry_exported"] = false };
            }
            if (!ReferenceEquals(defaultWorld, World.DefaultGameObjectInjectionWorld) || World.All.Count != worldCount) throw new InvalidOperationException("World cleanup mismatch");
            VerifyPins(pins);
            Directory.CreateDirectory(output); string file = Path.Combine(output, "prefab-reference-data.json");
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Capture too large");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(bytes, 0, bytes.Length);
            Debug.Log("[S004MissingPrefabReference] result=Collected roots=" + keys.Length + " entities=" + ((JArray)result["entities"]).Count + " unsupported=" + unsupported.Count + " world=0 readiness=0");
        }
    }
}
#endif
