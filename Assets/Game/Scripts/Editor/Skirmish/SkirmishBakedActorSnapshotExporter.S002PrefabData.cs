#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private const string S002FocusedPrefabKeysSha256 =
            "f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09";

        public static void RunS002FocusedPrefabReferenceData()
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

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(
                Arg("-skirmishS002FocusedPrefabReferenceData"));
            string keyPath = Path.GetFullPath(Arg("-skirmishFocusedPrefabKeys"));
            string keySha = Arg("-skirmishFocusedPrefabKeysSha");
            string file = Path.Combine(output, "prefab-reference-data.json");
            if (File.Exists(file))
                throw new InvalidOperationException("Fresh S002 prefab reference output required");
            if (!StringComparer.Ordinal.Equals(keySha, S002FocusedPrefabKeysSha256))
                throw new InvalidOperationException("Reviewed exact S002 12-prefab keyset hash required");

            string[] keys = SkirmishFocusedPrefabKeySet.Read(keyPath, keySha);
            if (keys.Length != 12 || !File.Exists(SourceScene))
                throw new InvalidDataException("Exact S002 12-prefab input set and current registry scene required");

            var pins = SourcePins(SourceScene);
            pins[keyPath] = keySha;
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var unsupported = new JArray();
            JObject result;
            string namespaceId = Guid.NewGuid().ToString("N");
            using (var world = new World("S002FocusedAuthoredPrefabReferenceOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during S002 prefab export");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged,
                    new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)),
                    new SceneSystem.LoadParameters
                    {
                        Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
                    });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Original registry source scene did not finish streaming");

                EntityManager em = world.EntityManager;
                var roots = OpeningPrefabRoots(em, keys);
                var ordered = new JObject();
                foreach (string key in keys)
                {
                    string canonical = roots.Keys.Single(k =>
                        StringComparer.OrdinalIgnoreCase.Equals(k, key));
                    if (!StringComparer.Ordinal.Equals(canonical, key))
                        throw new InvalidOperationException("Exact S002 source prefab key spelling required: " + key);
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
                    ["geometry_exported"] = false,
                    ["consumer_rule"] = "Exact S002 12-root typed source-template closure only. The two explicitly classified VFX UnityObjectRef fields are omitted presentation data; unsupported simulation values remain disallowed. No mission state, fresh allocator, geometry, replay, or visual readiness is exported."
                };
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed during S002 prefab export");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S002 prefab source changed: " + pin.Key);

            int presentationOnlyOmissions = 0;
            foreach (JObject row in unsupported.OfType<JObject>())
            {
                string[] path = ((string)row["path"])?.Split('/');
                string[] entityParts = path != null && path.Length == 3
                    ? path[0].Split(':') : Array.Empty<string>();
                bool canonicalEntity = entityParts.Length == 2 &&
                    int.TryParse(entityParts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int entityIndex) &&
                    int.TryParse(entityParts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int entityVersion) &&
                    entityIndex >= 0 && entityVersion >= 0 && ((entityIndex == 0) == (entityVersion == 0)) &&
                    path[0] == entityIndex.ToString(CultureInfo.InvariantCulture) + ":" +
                    entityVersion.ToString(CultureInfo.InvariantCulture);
                bool allowedPath = path != null && path.Length == 3 && canonicalEntity && path[2] == "Prefab" &&
                    (path[1] == "Game.Components.UnitAttackImpactVfxReference" ||
                     path[1] == "Game.Components.UnitMuzzleFlashVfxReference");
                bool allowedValue = (string)row["reason"] == "unity_object_ref_requires_bound_source_identity" &&
                    ((string)row["type"] ?? "").StartsWith(
                        "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,", StringComparison.Ordinal);
                if (!allowedPath || !allowedValue)
                    throw new InvalidOperationException(
                        "S002 prefab export contains an unsupported non-presentation value; artifact retained");
                presentationOnlyOmissions++;
            }
            if (presentationOnlyOmissions != 14 || ((JArray)result["entities"]).Count != 352 ||
                ((JObject)result["opening_prefabs"]).Count != 12)
                throw new InvalidDataException("Reviewed S002 capture shape changed; artifact retained without pass marker");

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024)
                throw new InvalidDataException("Bounded S002 prefab reference output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);

            Debug.Log("[SkirmishS002FocusedPrefabReferenceData] result=Passed prefabs=12 entities=352 " +
                "unsupportedUnconsumedPresentation=14 replayReady=False fullMission=0");
        }
    }
}
#endif
