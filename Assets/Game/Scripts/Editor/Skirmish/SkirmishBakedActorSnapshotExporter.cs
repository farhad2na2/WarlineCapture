#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using Unity.Transforms;

namespace Game.Editor
{
    // A read-only source-template snapshot, never a replacement for mission startup.
    public static class SkirmishBakedActorSnapshotExporter
    {
        public const string SourceScene = "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity";
        public const string PassMarker = "[SkirmishBakedActorSnapshot] result=Passed";
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, "-skirmishActorSnapshot");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit new -skirmishActorSnapshot directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            var pins = SourcePins(); var originalDefault = World.DefaultGameObjectInjectionWorld;
            JObject report;
            using (var world = new World("OfflineBakedActorSnapshot", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system in streaming-only snapshot");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                string guid = AssetDatabase.AssetPathToGUID(SourceScene);
                if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Exact registry SubScene absent");
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged, new Unity.Entities.Hash128(guid),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene)) throw new InvalidOperationException("Registry source not fully streamed");
                report = Capture(world.EntityManager);
                report["source_scene"] = SourceScene; report["source_scene_guid"] = guid;
                report["source_pins"] = JObject.FromObject(pins);
            }
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld)) throw new InvalidOperationException("Default World changed");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source changed: " + pin.Key);
            // Persist only after disposal and invariance checks have passed.
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "baked-actors.json"), report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log(PassMarker + " entries=" + ((JArray)report["registry_entries"]).Count + " liveMissionReady=0");
        }
        static SortedDictionary<string, string> SourcePins()
        {
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string path in AssetDatabase.GetDependencies(SourceScene, true))
            { paths.Add(path); if (File.Exists(path + ".meta")) paths.Add(path + ".meta"); }
            foreach (string path in Directory.GetFiles("Assets/Game/Scripts", "*", SearchOption.AllDirectories))
                if (new[] { ".cs", ".asmdef", ".asmref" }.Contains(Path.GetExtension(path)))
                { paths.Add(path); if (File.Exists(path + ".meta")) paths.Add(path + ".meta"); }
            foreach (string path in Directory.GetFiles("ProjectSettings", "*", SearchOption.AllDirectories)) paths.Add(path);
            foreach (string path in new[] { "Packages/manifest.json", "Packages/packages-lock.json",
                "Assets/Tests/Editor/SkirmishBakedActorSnapshotTests.cs", "Assets/Tests/Editor/SkirmishBakedActorSnapshotTests.cs.meta" }) paths.Add(path);
            var pins = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (string path in paths)
            {
                string physical = path;
                if (path.StartsWith("Packages/", StringComparison.Ordinal) && !path.EndsWith("manifest.json", StringComparison.Ordinal) && !path.EndsWith("packages-lock.json", StringComparison.Ordinal))
                {
                    var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
                    if (info == null || !path.StartsWith("Packages/" + info.name + "/", StringComparison.Ordinal))
                        throw new InvalidOperationException("Exact installed package identity unavailable: " + path);
                    var locked = JObject.Parse(File.ReadAllText("Packages/packages-lock.json"))["dependencies"]?[info.name];
                    var packageJson = Path.Combine(info.resolvedPath, "package.json");
                    var actual = JObject.Parse(File.ReadAllText(packageJson));
                    bool embedded = (string)locked?["source"] == "embedded" &&
                        (string)locked?["version"] == "file:" + info.name &&
                        info.source == UnityEditor.PackageManager.PackageSource.Embedded &&
                        Path.GetFullPath(info.resolvedPath).TrimEnd(Path.DirectorySeparatorChar) == Path.GetFullPath("Packages/" + info.name).TrimEnd(Path.DirectorySeparatorChar);
                    bool versioned = (string)locked?["source"] != "embedded" && (string)locked?["version"] == info.version;
                    if ((string)actual["name"] != info.name || (string)actual["version"] != info.version || !(embedded || versioned))
                        throw new InvalidOperationException("Installed package differs from exact lock identity: " + path);
                    physical = Path.Combine(info.resolvedPath, path.Substring(("Packages/" + info.name + "/").Length));
                    pins[packageJson] = SkirmishOfflineDataExporter.HashFile(packageJson);
                }
                var pin = SkirmishOfflineDataExporter.ReadPin(physical);
                if (pin.status != "complete") throw new InvalidOperationException("Required baked-source payload unavailable: " + path + " " + pin.status);
                pins[physical] = pin.sha256;
            }
            return pins;
        }
        public static JObject Capture(EntityManager em)
        {
            var rows = new JArray(); var last = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            using var query = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            using var owners = query.ToEntityArray(Allocator.Temp);
            for (int ownerIndex = 0; ownerIndex < owners.Length; ownerIndex++)
            {
                var entries = em.GetBuffer<UnitPrefabRegistryEntry>(owners[ownerIndex], true);
                for (int i = 0; i < entries.Length; i++)
                {
                    var prefab = entries[i].Prefab;
                    if (prefab == Entity.Null || !em.Exists(prefab) || !em.HasComponent<UnitSourcePrefabKey>(prefab))
                        throw new InvalidOperationException("Incomplete actual registry entry");
                    string key = em.GetComponentData<UnitSourcePrefabKey>(prefab).Value.ToString();
                    if (key.Length == 0) throw new InvalidOperationException("Empty actual registry key");
                    if (!em.HasComponent<Prefab>(prefab)) throw new InvalidOperationException("Registry entry is not a baked Prefab");
                    var row = CaptureActor(em, prefab);
                    row["key"] = key; row["owner_query_index"] = ownerIndex; row["buffer_index"] = i;
                    row["selected_by_native_lookup"] = true;
                    if (last.TryGetValue(key, out int previous)) rows[previous]["selected_by_native_lookup"] = false;
                    last[key] = rows.Count; rows.Add(row);
                }
            }
            if (rows.Count == 0) throw new InvalidOperationException("Actual baked registry empty");
            return new JObject { ["schema_version"] = 1, ["scope"] = "ActualBakedRegistryTemplates_NotMissionStartup",
                ["lookup_comparer"] = "OrdinalIgnoreCase_LastObservedEntryWins", ["registry_entries"] = rows,
                ["full_mission_initialized"] = false, ["random_states_captured_not_reconstructed"] = true };
        }
        public static JObject CaptureActor(EntityManager em, Entity actor)
        {
            using var types = em.GetComponentTypes(actor);
            var row = new JObject { ["entity_index"] = actor.Index, ["entity_version"] = actor.Version,
                ["entity_enabled"] = em.IsEnabled(actor), ["component_types"] = new JArray(types.Select(t => t.GetManagedType().FullName).OrderBy(x => x, StringComparer.Ordinal)) };
            var data = new JObject(); row["components"] = data;
            var enabled = new JObject(); row["enableable_components"] = enabled;
            foreach (var type in types)
                if (type.IsEnableable) enabled[type.GetManagedType().FullName] = em.IsComponentEnabled(actor, type);
            Value<UnitGrid>(em, actor, data); Value<UnitFootprint>(em, actor, data); Value<UnitMove>(em, actor, data);
            Value<UnitMovementBehavior>(em, actor, data); Value<UnitVehicleMovement>(em, actor, data);
            Value<UnitVehicleKinematics>(em, actor, data); Value<UnitSurfaceComponent>(em, actor, data);
            Value<UnitAirMovement>(em, actor, data); Value<UnitAirComponent>(em, actor, data);
            Value<UnitCombat>(em, actor, data); Value<UnitAttack>(em, actor, data); Value<UnitHealth>(em, actor, data);
            Value<UnitAttackCooldownComponent>(em, actor, data); Value<UnitAnimationSettings>(em, actor, data);
            Value<UnitIdleWanderComponent>(em, actor, data); Value<UnitFuelConsumption>(em, actor, data);
            Value<AirMissileLauncherComponent>(em, actor, data); Value<GroundMissileLauncherComponent>(em, actor, data);
            Value<UnitAttackAnimationComponent>(em, actor, data); Value<UnitGroundOffsetComponent>(em, actor, data);
            Buffer<UnitAnimationOrderEntry>(em, actor, data); Buffer<UnitAnimationGroundOffset>(em, actor, data);
            Value<LocalTransform>(em, actor, data);
            row["unexported_component_values"] = new JArray(types.Where(t => !data.ContainsKey(t.GetManagedType().Name))
                .Select(t => t.GetManagedType().FullName).OrderBy(x => x, StringComparer.Ordinal));
            row["replay_ready"] = false;
            return row;
        }
        static void Buffer<T>(EntityManager em, Entity actor, JObject data) where T : unmanaged, IBufferElementData
        {
            if (!em.HasBuffer<T>(actor)) { data[typeof(T).Name] = JValue.CreateNull(); return; }
            var values = em.GetBuffer<T>(actor, true);
            if (values.Length > 65536) throw new InvalidOperationException("Bounded native animation buffer required");
            var rows = new JArray();
            foreach (var value in values)
            {
                string encoded = JsonUtility.ToJson(value);
                if (JsonUtility.ToJson(JsonUtility.FromJson<T>(encoded)) != encoded)
                    throw new InvalidOperationException("Buffer serialization roundtrip differs: " + typeof(T).FullName);
                rows.Add(JObject.Parse(encoded));
            }
            data[typeof(T).Name] = rows;
        }
        static void Value<T>(EntityManager em, Entity actor, JObject data) where T : unmanaged, IComponentData
        {
            // Null means component absent; it is not a default instance.
            if (!em.HasComponent<T>(actor)) { data[typeof(T).Name] = JValue.CreateNull(); return; }
            string encoded = JsonUtility.ToJson(em.GetComponentData<T>(actor));
            if (JsonUtility.ToJson(JsonUtility.FromJson<T>(encoded)) != encoded)
                throw new InvalidOperationException("Component serialization roundtrip differs: " + typeof(T).FullName);
            data[typeof(T).Name] = JObject.Parse(encoded);
        }
    }
}
#endif
