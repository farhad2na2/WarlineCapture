#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private const string S004FocusedPrefabKeysSha256 =
            "7bbcbe81369bf86844167355a6ce942c4be10ee3ff133660b90db4bc79214f58";

        private static readonly System.Collections.Generic.Dictionary<string, int> S004PresentationFields =
            new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Game.Components.UnitAttackImpactVfxReference/Prefab"] = 5,
                ["Game.Components.UnitMuzzleFlashVfxReference/Prefab"] = 4,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/MissileVisualPrefab"] = 1,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/LaunchFlashPrefab"] = 1,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/LaunchSmokePrefab"] = 1,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/MissileTrailPrefab"] = 1,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/AirburstExplosionPrefab"] = 1,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/AirTargetImpactPrefab"] = 1,
                ["Game.Components.AirMissileLauncherVfxReferenceComponent/InterceptExplosionPrefab"] = 1
            };

        // Captures the original streaming bake only. The retained unsupported ledger is diagnostic;
        // this artifact never authorizes replay, runtime import, visual readiness, or mission readiness.
        public static void RunS004FocusedPrefabReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");

            string[] args = Environment.GetCommandLineArgs();
            string Arg(string name)
            {
                int at = Array.IndexOf(args, name);
                if (at < 0 || at + 1 >= args.Length)
                    throw new ArgumentException("Required argument " + name);
                return args[at + 1];
            }

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS004FocusedPrefabReferenceData"));
            string keyPath = Path.GetFullPath(Arg("-skirmishS004FocusedPrefabKeys"));
            string keySha = Arg("-skirmishS004FocusedPrefabKeysSha");
            string file = Path.Combine(output, "prefab-reference-data.json");
            if (File.Exists(file)) throw new InvalidOperationException("Fresh S004 prefab reference output required");
            if (!StringComparer.Ordinal.Equals(keySha, S004FocusedPrefabKeysSha256) ||
                SkirmishOfflineDataExporter.HashFile(keyPath) != S004FocusedPrefabKeysSha256)
                throw new InvalidOperationException("Reviewed exact S004 compiled key descriptor hash required");

            string[] keys = SkirmishFocusedPrefabKeySet.Read(keyPath, keySha);
            if (keys.Length != 9 || keys.Distinct(StringComparer.Ordinal).Count() != 9 ||
                keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 9 || !File.Exists(SourceScene))
                throw new InvalidDataException("Exact nine unique S004 compiled source keys and registry scene required");

            var pins = SourcePins(SourceScene);
            pins[keyPath] = keySha;
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var unsupported = new JArray();
            JObject result;
            string namespaceId = Guid.NewGuid().ToString("N");
            using (var world = new World("S004FocusedAuthoredPrefabReferenceOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during S004 prefab export");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged,
                    new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Original registry source scene did not finish streaming");

                EntityManager em = world.EntityManager;
                var roots = OpeningPrefabRoots(em, keys);
                var ordered = new JObject();
                foreach (string key in keys)
                {
                    string canonical = roots.Keys.Single(k => StringComparer.OrdinalIgnoreCase.Equals(k, key));
                    if (!StringComparer.Ordinal.Equals(canonical, key))
                        throw new InvalidOperationException("Exact S004 source prefab key spelling required: " + key);
                    ordered[key] = roots[canonical];
                }
                JArray entities = CaptureTemplateClosure(em, keys.Select(k => roots[k]), unsupported);
                result = new JObject
                {
                    ["schema_version"] = 1,
                    ["kind"] = "AuthoredSkirmishPrefabReferenceData",
                    ["authority"] = "ActualSourceAssetsAndNativeBakers_NotMissionState",
                    ["source_scenes"] = new JArray(SourceScene),
                    ["source_pins"] = JObject.FromObject(pins),
                    ["unity_version"] = Application.unityVersion,
                    ["template_namespace_id"] = namespaceId,
                    ["requested_prefab_keys"] = new JArray(keys),
                    ["requested_prefab_keys_sha256"] = keySha,
                    ["opening_prefabs"] = ordered,
                    ["entities"] = entities,
                    ["unsupported_values"] = unsupported,
                    ["replay_ready"] = false,
                    ["fresh_world_allocator_exported"] = false,
                    ["full_mission_initialized"] = false,
                    ["native_visual_readiness"] = false,
                    ["mission_ready"] = false,
                    ["geometry_exported"] = false,
                    ["consumer_rule"] = "S004 source/baker-only structural and mechanics inspection. Preserve all 16 exact unsupported presentation UnityObjectRef<GameObject> leaves in the diagnostic ledger; these source identities are not resolved or imported, so impact, muzzle, and missile visuals are omitted from any source-only projection. No runtime import, replay, gameplay, geometry, native visual readiness, or mission readiness is authorized."
                };
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed during S004 prefab export");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S004 prefab source changed: " + pin.Key);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Bounded S004 prefab reference output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);

            var observed = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
            foreach (JObject row in unsupported.OfType<JObject>())
            {
                string[] path = ((string)row["path"])?.Split('/');
                string[] ids = path != null && path.Length == 3 ? path[0].Split(':') : Array.Empty<string>();
                bool canonical = ids.Length == 2 &&
                    int.TryParse(ids[0], NumberStyles.None, CultureInfo.InvariantCulture, out int index) &&
                    int.TryParse(ids[1], NumberStyles.None, CultureInfo.InvariantCulture, out int version) &&
                    index >= 0 && version >= 0 && ((index == 0) == (version == 0)) &&
                    path[0] == index.ToString(CultureInfo.InvariantCulture) + ":" + version.ToString(CultureInfo.InvariantCulture);
                string field = path != null && path.Length == 3 ? path[1] + "/" + path[2] : "";
                bool exactType = StringComparer.Ordinal.Equals((string)row["reason"], "unity_object_ref_requires_bound_source_identity") &&
                    StringComparer.Ordinal.Equals((string)row["type"],
                        "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]");
                if (!canonical || !exactType || !S004PresentationFields.ContainsKey(field))
                    throw new InvalidOperationException("S004 diagnostic contains unsupported non-reviewed value; artifact retained");
                observed.TryGetValue(field, out int count);
                observed[field] = count + 1;
            }
            if (unsupported.Count != 16 || S004PresentationFields.Any(p => !observed.TryGetValue(p.Key, out int count) || count != p.Value) ||
                observed.Count != S004PresentationFields.Count || ((JArray)result["entities"]).Count != 276 ||
                ((JObject)result["opening_prefabs"]).Count != 9)
                throw new InvalidDataException("Reviewed S004 capture shape changed; diagnostic artifact retained without pass marker");

            Debug.Log("[SkirmishS004ScopedPrefabReferenceData] result=PassedScoped prefabs=9 entities=276 " +
                "retainedPresentationRefs=16 replayReady=False nativeVisualReadiness=False missionReady=False fullMission=0");
        }
    }
}
#endif
