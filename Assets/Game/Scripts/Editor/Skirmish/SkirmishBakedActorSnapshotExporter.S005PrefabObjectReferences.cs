#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        // Asset identity inspection only; retains the original unsupported ledger.
        public static void RunS005PrefabObjectReferences()
        {
            RunPrefabObjectReferences(false);
        }

        // The selected domain comes from a checked asset-only unit capture;
        // it is not a mission, gameplay or template-readiness assertion.
        public static void RunSelectedUnitPrefabObjectReferences()
        {
            RunPrefabObjectReferences(true);
        }

        private static void RunPrefabObjectReferences(bool selectedUnits)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            var args = Environment.GetCommandLineArgs();
            string Arg(string key) { int i = Array.IndexOf(args, key); if (i < 0 || i + 1 >= args.Length) throw new ArgumentException(key); return args[i + 1]; }
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-s005PrefabObjectsOutput"));
            string keyPath; string expectedKeyHash; string[] keys; string receiptPath=null,receiptHash=null,extraPath=null,extraHash=null;
            if(selectedUnits)
            {
                keyPath=Path.GetFullPath(Arg("-skirmishUnitMetadataInput"));
                receiptPath=Path.GetFullPath(Arg("-skirmishUnitMetadataReceipt"));
                expectedKeyHash=HashFile(keyPath);receiptHash=HashFile(receiptPath);
                var receipt=JObject.Parse(File.ReadAllText(receiptPath));var metadata=JObject.Parse(File.ReadAllText(keyPath));
                if((string)receipt["status"]!="PassedAssetMetadataOnly" || (int?)receipt["wrapper_exit"]!=0 ||
                    (string)receipt["metadata_sha256"]!=expectedKeyHash || (bool?)receipt["mission_ready"]!=false ||
                    (string)metadata["kind"]!="AuthoredSkirmishUnitProductionMetadata" ||
                    (string)metadata["catalog_id"]!=(string)receipt["catalog_id"] ||
                    (bool?)metadata["full_mission_initialized"]!=false || (bool?)metadata["native_visual_readiness"]!=false)
                    throw new InvalidDataException("Exact unqualified selected unit metadata capture required");
                var unitRows=metadata["units"] as JArray ?? throw new InvalidDataException("Selected unit rows required");
                keys=unitRows.OfType<JObject>().Select(row=>(string)row["key"]).ToArray();
                if(keys.Length<1 || keys.Length>128 || keys.Length!=unitRows.Count || keys.Length!=(int?)receipt["units"] ||
                    keys.Any(String.IsNullOrWhiteSpace) || keys.Distinct(StringComparer.Ordinal).Count()!=keys.Length)
                    throw new InvalidDataException("Bounded unique selected unit keys required");
                int extraAt=Array.IndexOf(args,"-skirmishAdditionalPrefabKeys");
                if(extraAt>=0)
                {
                    if(extraAt+1>=args.Length)throw new ArgumentException("Additional prefab key path required");
                    extraPath=Path.GetFullPath(args[extraAt+1]);extraHash=Arg("-skirmishAdditionalPrefabKeysSha256");
                    var extra=SkirmishFocusedPrefabKeySet.Read(extraPath,extraHash);
                    keys=keys.Concat(extra).Distinct(StringComparer.Ordinal).ToArray();
                    if(keys.Length>128)throw new InvalidDataException("Bounded combined prefab key domain required");
                }
                var capturedPins=metadata["source_pins"] as JObject ?? throw new InvalidDataException("Captured source pins required");
                foreach(var pin in capturedPins.Properties())
                    if(HashFile(pin.Name)!=(string)pin.Value)throw new InvalidDataException("Selected metadata source changed: "+pin.Name);
            }
            else
            {
                keyPath=Path.GetFullPath(Arg("-s005PrefabObjectsKeys"));
                expectedKeyHash="6fde3c76fa1abef80802ba120dd7c051d86aa85d010f8589e5e6bfab12e84e43";
                keys=SkirmishFocusedPrefabKeySet.Read(keyPath,expectedKeyHash);
                if(keys.Length!=16)throw new InvalidDataException("Exact S005 key domain required");
            }
            var pins = SourcePins(SourceScene); pins[keyPath] = expectedKeyHash;
            if(selectedUnits)pins[receiptPath]=receiptHash;
            if(extraPath!=null)pins[extraPath]=extraHash;
            const string captureSource = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S005PrefabObjectReferences.cs";
            pins[captureSource] = HashFile(captureSource);
            string captureAssemblyPath = typeof(SkirmishBakedActorSnapshotExporter).Assembly.Location;
            string entitiesAssemblyPath = typeof(Entity).Assembly.Location;
            string captureAssemblyHash = HashFile(captureAssemblyPath), entitiesAssemblyHash = HashFile(entitiesAssemblyPath);
            var before = World.DefaultGameObjectInjectionWorld; int worldCount = World.All.Count;
            var missing = new JArray(); var refs = new JArray(); JObject result;
            using (var world = new World("S005PrefabObjectReferenceInspection", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during prefab inspection");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene)) throw new InvalidOperationException("Source registry scene not loaded");
                var em = world.EntityManager; var roots = OpeningPrefabRoots(em, keys);
                var ordered = new JObject();
                foreach (string key in keys) { if (!roots.TryGetValue(key, out var root)) throw new InvalidDataException("Exact source prefab root missing: " + key); ordered[key] = root; }
                var entities = CaptureTemplateClosure(em, keys.Select(k => roots[k]), missing);
                foreach (JObject item in missing)
                {
                    string path = (string)item["path"]; string[] parts = path?.Split('/'); string[] id = parts?[0].Split(':');
                    if (parts == null || parts.Length != 3 || id == null || id.Length != 2 ||
                        (string)item["reason"] != "unity_object_ref_requires_bound_source_identity")
                        throw new InvalidDataException("Unexpected unsupported leaf: " + path);
                    var entity = new Entity { Index = Int32.Parse(id[0]), Version = Int32.Parse(id[1]) };
                    object component;
                    switch (parts[1])
                    {
                        case "Game.Components.UnitAttackImpactVfxReference": component = em.GetComponentData<UnitAttackImpactVfxReference>(entity); break;
                        case "Game.Components.UnitMuzzleFlashVfxReference": component = em.GetComponentData<UnitMuzzleFlashVfxReference>(entity); break;
                        case "Game.Components.GroundMissileLauncherVfxReferenceComponent": component = em.GetComponentData<GroundMissileLauncherVfxReferenceComponent>(entity); break;
                        case "Game.Components.AirMissileLauncherVfxReferenceComponent": component = em.GetComponentData<AirMissileLauncherVfxReferenceComponent>(entity); break;
                        default: throw new InvalidDataException("Unexpected component: " + parts[1]);
                    }
                    var field = component.GetType().GetField(parts[2], BindingFlags.Public | BindingFlags.Instance);
                    object boxed = field?.GetValue(component);
                    var value = boxed?.GetType().GetProperty("Value")?.GetValue(boxed) as UnityEngine.Object;
                    if (field == null || boxed == null) throw new InvalidDataException("Missing object reference field: " + path);
                    JToken identity = value == null ? new JObject { ["codec"] = "null-v1" } : SourceAsset(value);
                    if (value != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(value);
                        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath) || !File.Exists(assetPath + ".meta"))
                            throw new InvalidDataException("Unpinned referenced asset: " + path);
                        pins[assetPath] = HashFile(assetPath); pins[assetPath + ".meta"] = HashFile(assetPath + ".meta");
                    }
                    refs.Add(new JObject { ["path"] = path, ["identity"] = identity });
                }
                if ((!selectedUnits && missing.Count != 29) || missing.Count>4096 || refs.Count != missing.Count) throw new InvalidDataException("Unresolved source reference domain changed");
                result = new JObject { ["kind"] = selectedUnits ? "SelectedUnitPrefabObjectReferenceInspection" : "S005PrefabObjectReferenceInspection", ["requested_prefab_keys"] = new JArray(keys),
                    ["requested_prefab_keys_sha256"] = expectedKeyHash, ["template_namespace_id"] = Guid.NewGuid().ToString("N"),
                    ["opening_prefabs"] = ordered, ["entities"] = entities, ["unsupported_values"] = missing,
                    ["object_reference_identities"] = refs, ["source_pins"] = JObject.FromObject(pins),
                    ["capture_assembly"] = new JObject { ["path"] = captureAssemblyPath, ["sha256"] = captureAssemblyHash,
                        ["mvid"] = typeof(SkirmishBakedActorSnapshotExporter).Assembly.ManifestModule.ModuleVersionId.ToString() },
                    ["entities_assembly"] = new JObject { ["path"] = entitiesAssemblyPath, ["sha256"] = entitiesAssemblyHash,
                        ["mvid"] = typeof(Entity).Assembly.ManifestModule.ModuleVersionId.ToString() },
                    ["world_created_for_capture"] = true, ["mission_world_created"] = false,
                    ["replay_ready"] = false, ["native_visual_readiness"] = false, ["mission_ready"] = false };
                if(selectedUnits)
                {
                    result.Remove("requested_prefab_keys_sha256");
                    result["selected_unit_metadata_sha256"]=expectedKeyHash;
                    result["selected_unit_metadata_receipt_sha256"]=receiptHash;
                    if(extraPath!=null)result["additional_prefab_keys_sha256"]=extraHash;
                }
            }
            if (!ReferenceEquals(before, World.DefaultGameObjectInjectionWorld) || World.All.Count != worldCount)
                throw new InvalidOperationException("World ownership changed");
            VerifyPins(pins);
            if (HashFile(captureAssemblyPath) != captureAssemblyHash || HashFile(entitiesAssemblyPath) != entitiesAssemblyHash)
                throw new InvalidOperationException("Loaded capture or Entities assembly changed during inspection");
            Directory.CreateDirectory(output); string file = Path.Combine(output, selectedUnits ? "selected-unit-prefab-object-references.json" : "s005-prefab-object-references.json");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) writer.Write(result.ToString(Formatting.Indented) + "\n");
            if(selectedUnits)Debug.Log("[SelectedUnitPrefabObjectReferences] result=Collected keys="+keys.Length+" resolved="+refs.Count+" mission=0 replayReady=0 readiness=0");
            else Debug.Log("[S005PrefabObjectReferences] result=Collected keys=16 resolved=29 mission=0 replayReady=0 readiness=0");
        }
    }
}
#endif
