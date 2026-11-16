#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
        public static void RunS073FocusedPrefabReferenceData()
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

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS073PrefabReferenceData"));
            string auditPath = Path.GetFullPath(Arg("-skirmishS073AssetAudit"));
            string auditSha = Arg("-skirmishS073AssetAuditSha");
            string file = Path.Combine(output, "prefab-reference-data.json");
            if (File.Exists(file)) throw new InvalidOperationException("Fresh S073 prefab reference output required");
            if (!Path.IsPathRooted(auditPath) || !File.Exists(auditPath) ||
                auditSha.Length != 64 || auditSha.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')) ||
                SkirmishOfflineDataExporter.HashFile(auditPath) != auditSha)
                throw new InvalidOperationException("Exact hash-pinned S073 asset audit required");

            JObject audit = JObject.Parse(File.ReadAllText(auditPath), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            if ((int?)audit["schema_version"] != 1 ||
                (string)audit["kind"] != "S073LegacySourceAssetReferenceAudit" ||
                (string)audit["authority"] != "Current Unity YAML references resolved by exact current .meta GUIDs; data only" ||
                (string)audit["mission"] != "S073" ||
                audit["configuration_roots"] is not JArray configRoots ||
                audit["references"] is not JArray references)
                throw new InvalidDataException("Exact S073 source asset audit required");

            string initialForcesPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset";
            string unitRegistryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
            var configPaths = new List<string>();
            foreach (JObject root in configRoots.OfType<JObject>())
            {
                string path = (string)root["path"], sha = (string)root["sha256"];
                if (path == null || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    sha == null || sha.Length != 64 || SkirmishOfflineDataExporter.HashFile(path) != sha)
                    throw new InvalidDataException("Current bounded S073 config-root pin required");
                configPaths.Add(path);
            }
            if (!configPaths.Contains(initialForcesPath, StringComparer.Ordinal) ||
                !configPaths.Contains(unitRegistryPath, StringComparer.Ordinal))
                throw new InvalidDataException("Current InitialForces and UnitRegistry source pins required");

            foreach (JObject reference in references.OfType<JObject>())
            {
                string path = (string)reference["asset_path"], sha = (string)reference["asset_sha256"];
                string metaPath = (string)reference["meta_path"], metaSha = (string)reference["meta_sha256"];
                if ((bool?)reference["exists"] != true || path == null || sha == null ||
                    metaPath == null || metaSha == null || SkirmishOfflineDataExporter.HashFile(path) != sha ||
                    SkirmishOfflineDataExporter.HashFile(metaPath) != metaSha)
                    throw new InvalidDataException("Current complete S073 direct-reference inventory required");
            }

            bool IsRequiredUnitUse(JObject use)
            {
                string source = (string)use["source_path"];
                string field = (string)use["field_path"];
                return source == initialForcesPath && field.Contains(".units[") && field.EndsWith(".prefab", StringComparison.Ordinal) ||
                    source == unitRegistryPath && field.Contains(".unitSpawnPrefabs[");
            }

            JObject[] rows = references.OfType<JObject>()
                .Where(row => row["uses"] is JArray uses && uses.OfType<JObject>().Any(IsRequiredUnitUse))
                .Where(row => ((string)row["asset_path"])?.EndsWith(".prefab", StringComparison.Ordinal) == true)
                .OrderBy(row => (string)row["guid"], StringComparer.Ordinal)
                .ToArray();
            if (rows.Length != 4 || rows.Select(row => (string)row["guid"]).Distinct(StringComparer.Ordinal).Count() != 4)
                throw new InvalidDataException("Exact four S073 unit prefab assets required");

            var sourcePins = SourcePins(new[] { SourceScene }.Concat(configPaths).ToArray());
            sourcePins[auditPath] = auditSha;
            var requested = new List<(string key, string guid, long localId, string path)>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JObject row in rows)
            {
                string guid = (string)row["guid"];
                string path = (string)row["asset_path"];
                string assetSha = (string)row["asset_sha256"];
                string metaPath = (string)row["meta_path"];
                string metaSha = (string)row["meta_sha256"];
                long[] ids = (row["file_ids"] as JArray)?.Values<long>().Distinct().ToArray() ?? Array.Empty<long>();
                if (guid == null || guid.Length != 32 || path == null || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    !path.EndsWith(".prefab", StringComparison.Ordinal) || ids.Length != 1 ||
                    AssetDatabase.AssetPathToGUID(path) != guid ||
                    SkirmishOfflineDataExporter.HashFile(path) != assetSha ||
                    SkirmishOfflineDataExporter.HashFile(metaPath) != metaSha)
                    throw new InvalidDataException("Exact current S073 prefab asset identity required: " + path);

                string[] metaGuids = File.ReadAllLines(metaPath)
                    .Where(line => line.StartsWith("guid: ", StringComparison.Ordinal))
                    .Select(line => line.Substring("guid: ".Length).Trim())
                    .ToArray();
                if (metaGuids.Length != 1 || metaGuids[0] != guid)
                    throw new InvalidDataException("Prefab .meta GUID differs from source reference: " + path);

                UnityEngine.Object assetObject = AssetDatabase.LoadAllAssetsAtPath(path)
                    .SingleOrDefault(asset =>
                        asset is GameObject &&
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string objectGuid, out long localId) &&
                        objectGuid == guid && localId == ids[0]);
                if (!(assetObject is GameObject gameObject) || string.IsNullOrWhiteSpace(gameObject.name) || !keys.Add(gameObject.name))
                    throw new InvalidDataException("Exact unique S073 prefab GameObject local identity/name required: " + path);

                requested.Add((gameObject.name, guid, ids[0], path));
                sourcePins[path] = assetSha;
                sourcePins[metaPath] = metaSha;
            }

            var requestedKeys = requested.Select(row => row.key).ToArray();
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var missing = new JArray();
            JObject result;
            string namespaceId = Guid.NewGuid().ToString("N");
            using (var world = new World("S073FocusedAuthoredPrefabReferenceOnly", WorldFlags.Game))
            {
                var streaming = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in streaming)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during S073 prefab export");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, streaming);
                var scene = SceneSystem.LoadSceneAsync(world.Unmanaged,
                    new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(SourceScene)),
                    new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                world.Update();
                if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                    throw new InvalidOperationException("Original registry source scene did not finish streaming");

                EntityManager em = world.EntityManager;
                using var query = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<Prefab>(), ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                    Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities
                });
                using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                var roots = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);
                foreach (Entity entity in entities)
                {
                    string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                    if (!keys.Contains(key)) continue;
                    if (!roots.TryAdd(key, entity))
                        throw new InvalidOperationException("Duplicate exact S073 source prefab root key: " + key);
                }
                if (roots.Count != requestedKeys.Length)
                    throw new InvalidOperationException("S073 baked root set incomplete; missing=" +
                        string.Join(",", requestedKeys.Where(key => !roots.ContainsKey(key))));

                foreach (var row in requested)
                    if (!StringComparer.Ordinal.Equals(row.key, em.GetComponentData<UnitSourcePrefabKey>(roots[row.key]).Value.ToString()))
                        throw new InvalidOperationException("S073 source root key differs from exact GameObject name: " + row.path);

                JArray capturedEntities = CaptureTemplateClosure(em, requested.Select(row => EntityId(roots[row.key])), missing);
                result = new JObject
                {
                    ["schema_version"] = 1,
                    ["kind"] = "AuthoredSkirmishPrefabReferenceData",
                    ["authority"] = "ActualSourceAssetsAndNativeBakers_NotMissionState",
                    ["source_scenes"] = new JArray(SourceScene),
                    ["source_pins"] = JObject.FromObject(sourcePins),
                    ["unity_version"] = Application.unityVersion,
                    ["template_namespace_id"] = namespaceId,
                    ["requested_prefab_keys"] = new JArray(requestedKeys),
                    ["requested_prefab_keys_sha256"] = auditSha,
                    ["opening_prefabs"] = JObject.FromObject(requested.ToDictionary(row => row.key,
                        row => EntityId(roots[row.key]), StringComparer.Ordinal)),
                    ["entities"] = capturedEntities,
                    ["unsupported_values"] = missing,
                    ["replay_ready"] = missing.Count == 0,
                    ["fresh_world_allocator_exported"] = false,
                    ["full_mission_initialized"] = false,
                    ["native_visual_readiness"] = false,
                    ["geometry_exported"] = false,
                    ["consumer_rule"] = "One captured Entity namespace only. Original Game.Components/Unity.Transforms/linked-template closure codecs. Requires exact S073 InitialForces and UnitRegistry GUID/local-ID/name mapping and unique UnitSourcePrefabKey roots. No mission state or physical map geometry is exported."
                };
            }

            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed");
            foreach (var pin in sourcePins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("S073 prefab source changed: " + pin.Key);

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidOperationException("Bounded S073 prefab reference output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);
            int unconsumedPresentation = 0;
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
                    path[2] == "Prefab" &&
                    (path[1] == "Game.Components.UnitAttackImpactVfxReference" ||
                     path[1] == "Game.Components.UnitMuzzleFlashVfxReference");
                bool allowedValue = (string)row["reason"] == "unity_object_ref_requires_bound_source_identity" &&
                    ((string)row["type"] ?? "").StartsWith(
                        "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,", StringComparison.Ordinal);
                if (!allowedPath || !allowedValue)
                    throw new InvalidOperationException("S073 prefab reference contains unsupported simulation value; diagnostic artifact retained");
                unconsumedPresentation++;
            }
            Debug.Log("[SkirmishS073FocusedPrefabReferenceData] result=Passed prefabs=" + requestedKeys.Length +
                " entities=" + ((JArray)result["entities"]).Count +
                " unsupportedUnconsumedPresentation=" + unconsumedPresentation +
                " replayReady=" + (bool)result["replay_ready"] + " fullMission=0");
        }

        public static void RunS073LegacyPrefabNameReadback()
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

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(Arg("-skirmishS073PrefabNameReadback"));
            string auditPath = Path.GetFullPath(Arg("-skirmishS073AssetAudit"));
            string auditSha = Arg("-skirmishS073AssetAuditSha");
            string prefabPath = Path.GetFullPath(Arg("-skirmishS073PrefabReferenceData"));
            string prefabSha = Arg("-skirmishS073PrefabReferenceDataSha");
            string resultPath = Path.Combine(output, "source-object-names.json");
            if (File.Exists(resultPath) || !Path.IsPathRooted(auditPath) || !File.Exists(auditPath) ||
                auditSha.Length != 64 || SkirmishOfflineDataExporter.HashFile(auditPath) != auditSha ||
                !Path.IsPathRooted(prefabPath) || !File.Exists(prefabPath) || prefabSha.Length != 64 ||
                SkirmishOfflineDataExporter.HashFile(prefabPath) != prefabSha)
                throw new InvalidDataException("Fresh exact S073 source audit and prefab capture required");

            JObject audit = JObject.Parse(File.ReadAllText(auditPath), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            JObject prefab = JObject.Parse(File.ReadAllText(prefabPath), new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            if ((int?)audit["schema_version"] != 1 || (string)audit["kind"] != "S073LegacySourceAssetReferenceAudit" ||
                (string)audit["mission"] != "S073" || audit["configuration_roots"] is not JArray configRoots ||
                audit["references"] is not JArray references || (int?)prefab["schema_version"] != 1 ||
                (string)prefab["kind"] != "AuthoredSkirmishPrefabReferenceData" ||
                (string)prefab["authority"] != "ActualSourceAssetsAndNativeBakers_NotMissionState" ||
                prefab["opening_prefabs"] is not JObject capturedRoots)
                throw new InvalidDataException("Exact S073 source-name inputs required");

            const string initialForcesPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset";
            const string unitRegistryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
            var configPins = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (JObject row in configRoots.OfType<JObject>())
            {
                string path = (string)row["path"], sha = (string)row["sha256"];
                if (path == null || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                    sha == null || sha.Length != 64 || SkirmishOfflineDataExporter.HashFile(path) != sha)
                    throw new InvalidDataException("Current exact S073 config pin required");
                configPins.Add(path, sha);
            }
            if (!configPins.ContainsKey(initialForcesPath) || !configPins.ContainsKey(unitRegistryPath))
                throw new InvalidDataException("Pinned S073 InitialForces and UnitRegistry required");

            bool IsRequiredUse(JObject use)
            {
                string source = (string)use["source_path"];
                string field = (string)use["field_path"];
                return source == initialForcesPath &&
                       ((field.Contains(".units[") && field.EndsWith(".prefab", StringComparison.Ordinal)) ||
                        (field.Contains(".buildings[") && field.EndsWith(".prefab", StringComparison.Ordinal)) ||
                        field == "MonoBehaviour.baseWallPrefab" || field == "MonoBehaviour.baseGatePrefab" ||
                        field == "MonoBehaviour.baseCoreBuildingPrefab") ||
                       source == unitRegistryPath && field.Contains(".unitSpawnPrefabs[");
            }

            JObject[] selected = references.OfType<JObject>()
                .Where(row => row["uses"] is JArray uses && uses.OfType<JObject>().Any(IsRequiredUse))
                .Where(row => ((string)row["asset_path"])?.EndsWith(".prefab", StringComparison.Ordinal) == true)
                .OrderBy(row => (string)row["guid"], StringComparer.Ordinal)
                .ToArray();
            if (selected.Length == 0 || selected.Length > 64 ||
                selected.Any(row => row["file_ids"] is not JArray ids || ids.Count == 0 || ids.Count > 16))
                throw new InvalidDataException("Bounded referenced S073 prefab objects required");

            var pins = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [auditPath] = auditSha,
                [prefabPath] = prefabSha
            };
            foreach (var pin in configPins) pins[pin.Key] = pin.Value;
            var entries = new JArray();
            var seenObjects = new HashSet<string>(StringComparer.Ordinal);
            var capturedUnitKeys = new HashSet<string>(capturedRoots.Properties().Select(property => property.Name), StringComparer.Ordinal);
            int unitObjectCount = 0;
            foreach (JObject row in selected)
            {
                string guid = (string)row["guid"], assetPath = (string)row["asset_path"];
                string assetSha = (string)row["asset_sha256"], metaPath = (string)row["meta_path"];
                string metaSha = (string)row["meta_sha256"];
                if (guid == null || guid.Length != 32 || assetPath == null || !assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                    assetSha == null || metaPath == null || metaSha == null ||
                    SkirmishOfflineDataExporter.HashFile(assetPath) != assetSha || SkirmishOfflineDataExporter.HashFile(metaPath) != metaSha ||
                    AssetDatabase.AssetPathToGUID(assetPath) != guid)
                    throw new InvalidDataException("Current exact S073 prefab source identity required");

                foreach (long localId in ((JArray)row["file_ids"]).Values<long>().Distinct())
                {
                    string objectIdentity = guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
                    if (!seenObjects.Add(objectIdentity))
                        throw new InvalidDataException("Unique S073 prefab GUID/local-ID required");
                    UnityEngine.Object[] matching = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                        .Where(asset => asset is GameObject &&
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string objectGuid, out long objectLocalId) &&
                            objectGuid == guid && objectLocalId == localId)
                        .ToArray();
                    if (matching.Length != 1 || !(matching[0] is GameObject gameObject) || string.IsNullOrWhiteSpace(gameObject.name))
                        throw new InvalidDataException("Exact S073 referenced GameObject name unresolved: " + objectIdentity);

                    JObject[] uses = ((JArray)row["uses"]).OfType<JObject>()
                        .Where(IsRequiredUse)
                        .Where(use => (long?)use["file_id"] == localId)
                        .ToArray();
                    if (uses.Length == 0)
                        throw new InvalidDataException("Exact S073 prefab GUID/local-ID use relation required: " + objectIdentity);
                    bool unitUse = uses.Any(use => ((string)use["field_path"]).Contains(".units[") ||
                        ((string)use["field_path"]).Contains(".unitSpawnPrefabs["));
                    if (unitUse)
                    {
                        if (!capturedUnitKeys.Contains(gameObject.name))
                            throw new InvalidDataException("S073 source unit name missing from exact captured root namespace: " + gameObject.name);
                        unitObjectCount++;
                    }
                    entries.Add(new JObject
                    {
                        ["guid"] = guid,
                        ["file_id"] = localId,
                        ["asset_path"] = assetPath,
                        ["asset_sha256"] = assetSha,
                        ["meta_path"] = metaPath,
                        ["meta_sha256"] = metaSha,
                        ["game_object_name"] = gameObject.name,
                        ["unit_source_key"] = unitUse ? gameObject.name : null,
                        ["building_lookup_key"] = uses.Any(use => ((string)use["field_path"]).Contains(".buildings[") ||
                            ((string)use["field_path"]).Contains("baseWallPrefab") ||
                            ((string)use["field_path"]).Contains("baseGatePrefab") ||
                            ((string)use["field_path"]).Contains("baseCoreBuildingPrefab"))
                            ? gameObject.name.Trim().ToLowerInvariant() : null,
                        ["uses"] = JArray.FromObject(uses.Select(use => new
                        {
                            source_path = (string)use["source_path"],
                            field_path = (string)use["field_path"],
                            file_id = (long)use["file_id"]
                        }))
                    });
                    pins[assetPath] = assetSha;
                    pins[metaPath] = metaSha;
                }
            }
            if (unitObjectCount != 4 || entries.Count < 7 || entries.Count > 32)
                throw new InvalidDataException("Expected complete S073 unit and building source-name set");

            foreach (var pin in pins)
                if (!Path.IsPathRooted(pin.Key) && SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidDataException("S073 source name input changed: " + pin.Key);

            JObject result = new JObject
            {
                ["schema_version"] = 1,
                ["kind"] = "S073LegacyPrefabSourceNameReadback",
                ["authority"] = "AssetDatabase exact GUID/local-fileID GameObject names; source data only",
                ["mission"] = "S073",
                ["source_readback_sha256"] = (string)audit["readback_artifact_sha256"],
                ["prefab_capture_sha256"] = prefabSha,
                ["unity_version"] = Application.unityVersion,
                ["source_pins"] = JObject.FromObject(pins),
                ["objects"] = entries,
                ["unity_world_allocated"] = false,
                ["mission_world_allocated"] = false,
                ["mission_initialized"] = false,
                ["native_visual_readiness"] = false
            };
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(result.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 2 * 1024 * 1024) throw new InvalidDataException("Bounded S073 source-name output required");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(resultPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);
            Debug.Log("[SkirmishS073LegacyPrefabNameReadback] result=Passed objects=" + entries.Count +
                " unitObjects=" + unitObjectCount + " unityWorld=0 mission=0");
        }
    }
}
#endif
