#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string MeshDatabase = "Assets/Game/GeneratedOperationMapEntityPresentationCandidate/VirtualizedPresentation/OperationMapRenderDatabaseBakeConfig.asset";
        const string MapDefinition = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset";
        static readonly string[] OpeningKeys = {
            "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Chr_Soldier_Male_01", "Unit_Chr_Ghillie_Male_01",
            "Unit_Veh_Light_Armored_Car", "Unit_Veh_APC_Slow", "Unit_Veh_Tank_USA",
            "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray" };

        // One-time ASSET reference data, not a player build or a mission run.
        // Subsequent gameplay-code fixes run against these values in the C# host.
        public static void RunReferenceData()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle owned Editor required");
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishReferenceData");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("New -skirmishReferenceData directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string[] scenes = new[] { SourceScene,
                AssetDatabase.GUIDToAssetPath("c00140f2e94a04c3084c8dcb0c18cbd0"),
                AssetDatabase.GUIDToAssetPath("d755eb43b9cb04aa292e471ace113ecf") }.Distinct().ToArray();
            if (scenes.Any(string.IsNullOrEmpty) || scenes.Any(p => !File.Exists(p)))
                throw new InvalidOperationException("Coded Campaign/registry source scene missing");
            var pins = SourcePins(scenes.Concat(new[] { MeshDatabase, MapDefinition }).ToArray());
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            var missing = new JArray();
            JObject result;
            Directory.CreateDirectory(output);
            using (var world = new World("SkirmishAuthoredReferenceData", WorldFlags.Game))
            {
                var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Streaming, true);
                foreach (var system in systems)
                    if (TypeManager.GetSystemName(system).ToString().StartsWith("Game.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Gameplay system cannot run during an asset export");
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
                foreach (string source in scenes)
                {
                    var scene = SceneSystem.LoadSceneAsync(world.Unmanaged,
                        new Unity.Entities.Hash128(AssetDatabase.AssetPathToGUID(source)),
                        new SceneSystem.LoadParameters { Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn });
                    world.Update();
                    if (!SceneSystem.IsSceneLoaded(world.Unmanaged, scene))
                        throw new InvalidOperationException("Source scene did not finish streaming: " + source);
                }
                var em = world.EntityManager;
                var roots = OpeningPrefabRoots(em);
                var entities = CaptureTemplateClosure(em, roots.Values, missing);
                var meshes = CaptureMeshAuthority();
                var bounds = new NativeList<Bounds>(Allocator.Temp);
                var reader = Game.Rendering.RenderBoundsPresentation.Create();
                var authoredBounds = new JArray();
                int count;
                try
                {
                    count = reader.CountAuthoredBuildingGeometry(em);
                    reader.CollectAuthoredBuildingBounds(em, ref bounds);
                    if (count != bounds.Length) throw new InvalidOperationException("Authored bounds query changed during capture");
                    foreach (var value in bounds) authoredBounds.Add(new JObject { ["center"] = Vector(value.center), ["extents"] = Vector(value.extents) });
                }
                finally { bounds.Dispose(); }
                var sidewalks = new JArray();
                using var sidewalkQuery = em.CreateEntityQuery(typeof(OperationMapSidewalkFootprint));
                using var owners = sidewalkQuery.ToEntityArray(Allocator.Temp);
                foreach (Entity owner in owners)
                {
                    var values = em.GetBuffer<OperationMapSidewalkFootprint>(owner, true);
                    if (sidewalks.Count > 1000000 - values.Length) throw new InvalidOperationException("Sidewalk data bound exceeded");
                    foreach (var value in values) sidewalks.Add(new JObject {
                        ["world_to_local"] = Matrix(value.WorldToLocal), ["local_min"] = Vector(value.LocalMin),
                        ["local_max"] = Vector(value.LocalMax), ["world_min"] = Vector(value.WorldMin), ["world_max"] = Vector(value.WorldMax) });
                }
                var waterRows = new JArray();
                using var waterQuery = em.CreateEntityQuery(typeof(RenderMeshArray), typeof(MaterialMeshInfo), typeof(LocalToWorld));
                int waterQueryCount = waterQuery.CalculateEntityCount();
                var water = new NativeList<Game.Rendering.Contracts.RenderSurfaceFootprint>(Allocator.Temp);
                try
                {
                    reader.CollectWaterFootprints(em, ref water);
                    foreach (var value in water) waterRows.Add(new JObject {
                        ["world_to_local"] = Matrix((float4x4)value.WorldToLocal),
                        ["local_bounds_center"] = Vector(value.LocalBounds.center), ["local_bounds_extents"] = Vector(value.LocalBounds.extents),
                        ["world_bounds_center"] = Vector(value.WorldBounds.center), ["world_bounds_extents"] = Vector(value.WorldBounds.extents) });
                }
                finally { water.Dispose(); }
                var staticBlockers = CaptureAuthoredNavigationBlockers(em, missing);
                result = new JObject {
                    ["schema_version"] = 1, ["kind"] = "AuthoredSkirmishReferenceData",
                    ["authority"] = "ActualSourceAssetsAndNativeBakers_NotMissionState",
                    ["source_scenes"] = new JArray(scenes), ["source_pins"] = JObject.FromObject(pins),
                    ["unity_version"] = Application.unityVersion, ["opening_prefabs"] = JObject.FromObject(roots),
                    ["entities"] = entities, ["unsupported_values"] = missing,
                    ["mesh_database"] = MeshDatabase, ["mesh_database_sha256"] = SkirmishOfflineDataExporter.HashFile(MeshDatabase),
                    ["meshes"] = meshes, ["authored_bounds_count"] = count, ["authored_bounds"] = authoredBounds,
                    ["sidewalk_owner_count"] = owners.Length, ["sidewalks"] = sidewalks,
                    ["water_query_count"] = waterQueryCount, ["water_footprint_count"] = waterRows.Count, ["water_footprints"] = waterRows,
                    ["navigation_static_blocker_count"] = staticBlockers.Count, ["navigation_static_blockers"] = staticBlockers,
                    ["fresh_world_allocator_exported"] = false, ["full_mission_initialized"] = false,
                    ["native_visual_readiness"] = false,
                    ["consumer_rule"] = "Compile-owned type/read-domain allowlist; reject unsupported REQUIRED values; remap source Entity IDs into fresh template identities" };
            }
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld)) throw new InvalidOperationException("Default World changed");
            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source changed: " + pin.Key);
            File.WriteAllText(Path.Combine(output, "reference-data.json"), result.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
            Debug.Log("[SkirmishAuthoredReferenceData] result=Passed prefabs=" + OpeningKeys.Length +
                " entities=" + ((JArray)result["entities"]).Count + " meshes=" + ((JArray)result["meshes"]).Count +
                " sidewalks=" + ((JArray)result["sidewalks"]).Count +
                " navigationBlockers=" + ((JArray)result["navigation_static_blockers"]).Count + " fullMission=0");
        }

        // Match the native StaticGridBlockerUpdateSystem new-blocker query. A
        // placement exclusion mask is not an authoritative navigation footprint.
        // These are authored Baker values before gameplay or a private allocator runs.
        static JArray CaptureAuthoredNavigationBlockers(EntityManager em, JArray missing)
        {
            using var query = em.CreateEntityQuery(typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize));
            using var entities = query.ToEntityArray(Allocator.Temp);
            if (entities.Length > 100000) throw new InvalidOperationException("Authored navigation blocker domain exceeded");
            var selected = new HashSet<Type> {
                typeof(StaticGridBlocker), typeof(UnitGrid), typeof(GridBlockerSize),
                typeof(FriendlyPassGridBlocker), typeof(OperationMapBuildingComponent), typeof(RuntimeBuildingCombatInfo),
                typeof(StaticGridBlockerSurfaceOpeningComponent), typeof(LocalTransform), typeof(UnitHealth), typeof(Faction) };
            var rows = new JArray();
            foreach (Entity entity in entities)
            {
                var values = new JObject(); using var types = em.GetComponentTypes(entity);
                foreach (var component in types)
                {
                    Type type = component.GetManagedType();
                    if (!selected.Contains(type)) continue;
                    if (TypeManager.GetTypeInfo(component.TypeIndex).Category == TypeManager.TypeCategory.BufferData)
                        throw new InvalidOperationException("Unexpected buffer in authored blocker scalar domain: " + type.FullName);
                    JToken value = component.TypeIndex.IsZeroSized ? new JObject { ["codec"] = "tag-v1" } :
                        S002InitialWorldEntityExport.CaptureValue(em.Debug.GetComponentBoxed(entity, component),
                            EntityId(entity) + "/" + type.FullName, missing);
                    values[type.FullName] = new JObject { ["present"] = true, ["buffer"] = false,
                        ["enabled"] = component.IsEnableable ? (JToken)em.IsComponentEnabled(entity, component) : JValue.CreateNull(), ["value"] = value };
                }
                rows.Add(new JObject { ["id"] = EntityId(entity), ["entity_enabled"] = em.IsEnabled(entity), ["components"] = values });
            }
            return rows;
        }

        static Dictionary<string, string> OpeningPrefabRoots(EntityManager em)
        {
            var wanted = new HashSet<string>(OpeningKeys, StringComparer.OrdinalIgnoreCase);
            var roots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using var query = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            using var owners = query.ToEntityArray(Allocator.Temp);
            foreach (Entity owner in owners)
                foreach (var entry in em.GetBuffer<UnitPrefabRegistryEntry>(owner, true))
                {
                    if (entry.Prefab == Entity.Null || !em.Exists(entry.Prefab) || !em.HasComponent<UnitSourcePrefabKey>(entry.Prefab))
                        throw new InvalidOperationException("Invalid actual source registry entry");
                    string key = em.GetComponentData<UnitSourcePrefabKey>(entry.Prefab).Value.ToString();
                    if (wanted.Contains(key))
                    {
                        if (!em.HasComponent<Prefab>(entry.Prefab)) throw new InvalidOperationException("Actual template is not a prefab");
                        roots[key] = EntityId(entry.Prefab); // Same last-observed-entry rule as native lookup.
                    }
                }
            if (roots.Count != wanted.Count) throw new InvalidOperationException("Required actual opening prefab missing");
            return roots;
        }

        static JArray CaptureTemplateClosure(EntityManager em, IEnumerable<string> roots, JArray missing)
        {
            var queue = new Queue<Entity>(roots.Select(ParseEntity));
            var seen = new HashSet<Entity>(); var rows = new JArray();
            while (queue.Count != 0)
            {
                Entity entity = queue.Dequeue();
                if (entity == Entity.Null || !seen.Add(entity)) continue;
                if (seen.Count > 32768 || !em.Exists(entity)) throw new InvalidOperationException("Invalid/unbounded template Entity reference closure");
                var data = new JObject(); using var types = em.GetComponentTypes(entity);
                foreach (var component in types)
                {
                    Type type = component.GetManagedType();
                    bool include = type.Namespace == "Game.Components" || type.Namespace == "Unity.Transforms" ||
                        type == typeof(LinkedEntityGroup) || type == typeof(Prefab) || type == typeof(Disabled) || type == typeof(Simulate);
                    if (!include) continue; // Visual engine objects are not mission simulation data.
                    var info = TypeManager.GetTypeInfo(component.TypeIndex);
                    JToken value;
                    if (info.Category == TypeManager.TypeCategory.BufferData)
                        value = (JToken)typeof(SkirmishBakedActorSnapshotExporter).GetMethod(nameof(CaptureTypedBuffer), BindingFlags.Static | BindingFlags.NonPublic)
                            .MakeGenericMethod(type).Invoke(null, new object[] { em, entity, missing });
                    else if (component.TypeIndex.IsZeroSized) value = new JObject { ["codec"] = "tag-v1" };
                    else value = S002InitialWorldEntityExport.CaptureValue(em.Debug.GetComponentBoxed(entity, component), EntityId(entity) + "/" + type.FullName, missing);
                    data[type.FullName] = new JObject { ["present"] = true, ["buffer"] = info.Category == TypeManager.TypeCategory.BufferData,
                        ["enabled"] = component.IsEnableable ? (JToken)em.IsComponentEnabled(entity, component) : JValue.CreateNull(), ["value"] = value };
                    var descendants = value is JContainer container ? container.DescendantsAndSelf() : Enumerable.Empty<JToken>();
                    foreach (var reference in descendants.OfType<JObject>().Where(v => (string)v["codec"] == "entity-v1"))
                        queue.Enqueue(ParseEntity((string)reference["id"]));
                }
                rows.Add(new JObject { ["id"] = EntityId(entity), ["entity_enabled"] = em.IsEnabled(entity), ["components"] = data });
            }
            return rows;
        }

        static JToken CaptureTypedBuffer<T>(EntityManager em, Entity entity, JArray missing) where T : unmanaged, IBufferElementData
        {
            var buffer = em.GetBuffer<T>(entity, true);
            if (buffer.Length > 65536) throw new InvalidOperationException("Template buffer bound exceeded");
            var values = new JArray();
            for (int i = 0; i < buffer.Length; i++) values.Add(S002InitialWorldEntityExport.CaptureValue(buffer[i], EntityId(entity) + "/" + typeof(T).FullName + "/" + i, missing));
            return new JObject { ["codec"] = "template-buffer-values-v1", ["element_type"] = typeof(T).FullName, ["items"] = values };
        }

        static JArray CaptureMeshAuthority()
        {
            var database = AssetDatabase.LoadAssetAtPath<OperationMapRenderDatabaseBakeConfig>(MeshDatabase);
            if (database == null || database.Meshes.Count != 447) throw new InvalidOperationException("Expected current447-row Campaign mesh authority");
            var meshes = new JArray();
            for (int i = 0; i < database.Meshes.Count; i++)
            {
                Mesh mesh = database.Meshes[i].Mesh;
                var row = new JObject { ["index"] = i, ["meshPresent"] = mesh != null };
                if (mesh != null)
                {
                    bool resolved = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId);
                    row["assetGuid"] = resolved ? guid : null; row["localId"] = resolved ? (JToken)localId : JValue.CreateNull();
                    row["name"] = mesh.name; row["localBoundsCenter"] = Vector(mesh.bounds.center); row["localBoundsExtents"] = Vector(mesh.bounds.extents);
                    row["assetPath"] = AssetDatabase.GetAssetPath(mesh);
                }
                meshes.Add(row);
            }
            return meshes;
        }
        static string EntityId(Entity value) => value.Index.ToString(CultureInfo.InvariantCulture) + ":" + value.Version.ToString(CultureInfo.InvariantCulture);
        static Entity ParseEntity(string value) { var parts = value.Split(':'); return new Entity { Index = int.Parse(parts[0], CultureInfo.InvariantCulture), Version = int.Parse(parts[1], CultureInfo.InvariantCulture) }; }
        static JArray Vector(Vector3 value) => new JArray(value.x, value.y, value.z);
        static JArray Vector(float3 value) => new JArray(value.x, value.y, value.z);
        static JArray Matrix(float4x4 m) => new JArray(m.c0.x,m.c0.y,m.c0.z,m.c0.w,m.c1.x,m.c1.y,m.c1.z,m.c1.w,m.c2.x,m.c2.y,m.c2.z,m.c2.w,m.c3.x,m.c3.y,m.c3.z,m.c3.w);
    }
}
#endif
