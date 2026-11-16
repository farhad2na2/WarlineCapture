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
        private static readonly string[] S073ProductionRoots =
        {
            "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy", "Unit_Veh_APC_Slow",
            "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air", "Unit_Veh_Missle_Launcher_Ground",
            "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA", "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker",
            "Unit_Veh_Truck_Tray"
        };

        private static readonly System.Collections.Generic.Dictionary<string, string[]> S073PresentationOnlyFields =
            new System.Collections.Generic.Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Game.Components.UnitAttackImpactVfxReference"] = new[] { "Prefab" },
                ["Game.Components.UnitMuzzleFlashVfxReference"] = new[] { "Prefab" },
                ["Game.Components.GroundMissileLauncherVfxReferenceComponent"] =
                    new[] { "LauncherBackfirePrefab", "RocketTrailPrefab", "ImpactExplosionPrefab", "ImpactSmokePrefab" },
                ["Game.Components.AirMissileLauncherVfxReferenceComponent"] =
                    new[] { "MissileVisualPrefab", "LaunchFlashPrefab", "LaunchSmokePrefab", "MissileTrailPrefab",
                        "AirburstExplosionPrefab", "AirTargetImpactPrefab", "InterceptExplosionPrefab" }
            };

        // Native S073 production template closure only. Unsupported values are
        // retained verbatim; accepted unresolved values match the existing
        // importer's exact presentation-only VFX field allowlist.
        public static void RunS073ProductionPrefabReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle owned Editor required");

            string Arg(string name)
            {
                string[] args = Environment.GetCommandLineArgs();
                int at = Array.IndexOf(args, name);
                if (at < 0 || at + 1 >= args.Length)
                    throw new ArgumentException("Required argument " + name);
                return args[at + 1];
            }

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS073ProductionPrefabReferenceData"));
            string keyPath = Path.GetFullPath(Arg("-skirmishS073ProductionPrefabKeys"));
            string keySha = Arg("-skirmishS073ProductionPrefabKeysSha");
            string file = Path.Combine(output, "prefab-reference-data.json");
            if (File.Exists(file)) throw new InvalidOperationException("Fresh S073 production prefab output required");
            string[] keys = SkirmishFocusedPrefabKeySet.Read(keyPath, keySha);
            if (keys.Length != S073ProductionRoots.Length || S073ProductionRoots.Any(key => !keys.Contains(key, StringComparer.Ordinal)))
                throw new InvalidDataException("Exact twelve-root S073 production closure required; every root must be a source-derived legacy production key");
            if (!File.Exists(SourceScene)) throw new InvalidOperationException("Original registry source scene missing");

            var pins = SourcePins(SourceScene);
            pins[keyPath] = keySha;
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var missing = new JArray();
            JObject result;
            string namespaceId = Guid.NewGuid().ToString("N");
            using (var world = new World("S073ProductionPrefabReferenceOnly", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during S073 production prefab export");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged,
                    new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Original registry source scene did not finish streaming");

                var em = world.EntityManager;
                var roots = OpeningPrefabRoots(em, keys);
                var ordered = new JObject();
                foreach (string key in keys)
                {
                    string canonical = roots.Keys.Single(k => StringComparer.OrdinalIgnoreCase.Equals(k, key));
                    if (canonical != key)
                        throw new InvalidOperationException("Exact source prefab key spelling required: " + key + " actual=" + canonical);
                    ordered[key] = roots[canonical];
                }
                var entities = CaptureTemplateClosure(em, keys.Select(k => roots[k]), missing);
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
                    ["unsupported_values"] = missing,
                    ["replay_ready"] = missing.Count == 0,
                    ["fresh_world_allocator_exported"] = false,
                    ["full_mission_initialized"] = false,
                    ["native_visual_readiness"] = false,
                    ["geometry_exported"] = false,
                    ["consumer_rule"] = "S073 production template data only. Retain every unsupported row; only the exact unconsumed UnityObjectRef<GameObject> presentation fields accepted by GeneratedReferenceTemplateImporter are permitted. Consumer must validate the full compiled type/read-domain and entity closure; no mission state, physical geometry, or readiness is exported."
                };
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("Source changed: " + pin.Key);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidOperationException("Bounded S073 production prefab output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);

            int unconsumedPresentation = 0;
            if (missing.Any(row => row is not JObject))
                throw new InvalidOperationException("S073 production prefab unsupported-values rows must be exact objects; diagnostic artifact retained");
            foreach (JObject row in missing)
            {
                var path = ((string)row["path"])?.Split('/');
                string[] entityParts = path != null && path.Length == 3 ? path[0].Split(':') : Array.Empty<string>();
                bool canonicalEntity = entityParts.Length == 2 &&
                    int.TryParse(entityParts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int entityIndex) &&
                    int.TryParse(entityParts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int entityVersion) &&
                    entityIndex >= 0 && entityVersion >= 0 && ((entityIndex == 0) == (entityVersion == 0)) &&
                    path[0] == entityIndex.ToString(CultureInfo.InvariantCulture) + ":" + entityVersion.ToString(CultureInfo.InvariantCulture);
                bool allowedPath = path != null && path.Length == 3 && canonicalEntity &&
                    S073PresentationOnlyFields.TryGetValue(path[1], out string[] fields) && fields.Contains(path[2], StringComparer.Ordinal);
                bool allowedValue = (string)row["reason"] == "unity_object_ref_requires_bound_source_identity" &&
                    ((string)row["type"] ?? "").StartsWith(
                        "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,", StringComparison.Ordinal);
                if (!allowedPath || !allowedValue)
                    throw new InvalidOperationException("S073 production prefab contains unsupported non-presentation value; diagnostic artifact retained");
                unconsumedPresentation++;
            }

            Debug.Log("[SkirmishS073ProductionPrefabReferenceData] result=Passed prefabs=" + keys.Length +
                " entities=" + ((JArray)result["entities"]).Count +
                " unsupportedUnconsumedPresentation=" + unconsumedPresentation +
                " replayReady=" + (bool)result["replay_ready"] + " fullMission=0");
        }
    }
}
#endif
