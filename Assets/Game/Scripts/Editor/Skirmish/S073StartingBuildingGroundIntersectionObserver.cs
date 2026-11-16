#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Opt-in passive measurement of the S073 faction-1 startup buildings against
    /// currently loaded authored map-ground render triangles. No gameplay writes.
    /// </summary>
    [InitializeOnLoad]
    internal static class S073StartingBuildingGroundIntersectionObserver
    {
        const string OutputKey = "WARLINE_S073_BUILDING_GROUND_INTERSECTION_OUTPUT";
        const string MapId = "opmap.skirmish.industrial_basin";
        const string SourceMapId = "opmap.skirmish.desert_base_01";
        const string SourceIdentity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string SourceContent = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string SceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        const string BoundScene = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        const string ConfigPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
        const int ExpectedBuildingCount = 10;
        const int ExpectedFactionBuildings = 5;
        const int MaximumFootprintSamplesPerBuilding = 4096;
        const float Epsilon = 0.00001f;
        static bool done;
        static World observedWorld;
        static string observedSession;

        static S073StartingBuildingGroundIntersectionObserver()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey)))
                EditorApplication.update += Observe;
        }

        static void Observe()
        {
            if (done || !EditorApplication.isPlaying) return;
            try
            {
                ValidateTriangleMathControls();
                World world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated) return;
                EntityManager em = world.EntityManager;
                using var matches = em.CreateEntityQuery(typeof(SkirmishMatchState));
                if (matches.CalculateEntityCount() != 1) return;
                SkirmishMatchState match = matches.GetSingleton<SkirmishMatchState>();
                if (match.ScenarioIndex != 3 || match.Seed != 104729 || match.Phase != SkirmishPhase.Playing) return;
                if (observedWorld == null) { observedWorld = world; observedSession = match.SessionId.ToString(); }
                Require(ReferenceEquals(world, observedWorld) && match.SessionId.ToString() == observedSession,
                    "S073 world/session changed during observation");

                using var maps = em.CreateEntityQuery(typeof(ActiveOperationMapComponent), typeof(OperationMapMetadataComponent));
                using var grids = em.CreateEntityQuery(typeof(GridConfig));
                using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
                int mapCount = maps.CalculateEntityCount(), gridCount = grids.CalculateEntityCount(), surfaceCount = surfaces.CalculateEntityCount();
                using var actorsQuery = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<Faction>(),
                        ComponentType.ReadOnly<UnitSourcePrefabKey>(), ComponentType.ReadOnly<LocalTransform>() },
                    None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>() }
                });
                int actorCount = actorsQuery.CalculateEntityCount();
                Require(mapCount == 1 && gridCount == 1 && surfaceCount == 1,
                    $"Unique map/grid/surface required; query_counts maps={mapCount}, grids={gridCount}, surfaces={surfaceCount}, runtime_building_candidates={actorCount}");

                Entity map = maps.GetSingletonEntity();
                ActiveOperationMapComponent active = em.GetComponentData<ActiveOperationMapComponent>(map);
                OperationMapMetadataComponent metadata = em.GetComponentData<OperationMapMetadataComponent>(map);
                Require(active.OperationMapId.ToString() == MapId && metadata.Blob.IsCreated && metadata.PhysicalSourceValidated != 0 &&
                    metadata.Blob.Value.SourceOperationMapId.ToString() == SourceMapId &&
                    metadata.Blob.Value.SourceIdentityHash.ToString() == SourceIdentity &&
                    metadata.Blob.Value.SourceContentHash.ToString() == SourceContent &&
                    metadata.Blob.Value.Navigation.AuthoredSubSceneGuid.ToString() == SceneGuid,
                    "Pinned validated S073 Campaign map source required");
                Entity gridEntity = grids.GetSingletonEntity();
                GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                MapSurfaceComponent surface = surfaces.GetSingleton<MapSurfaceComponent>();
                Require(grid.Width == 2048 && grid.Height == 1024 && grid.CellSize == 1f &&
                    grid.Origin.x == 0f && grid.Origin.y == 0f && grid.Origin.z == 0f &&
                    surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated,
                    "Exact S073 gameplay grid and baked surface required");

                SkirmishPresetConfig preset = SkirmishPresetResolver.Load(em);
                Require(preset.buildingPlacement != null && preset.buildingPlacement.InitialUnitsConfig != null,
                    "Selected S073 preset has no authored initial building rows");
                var expected = new List<ExpectedBuilding>();
                foreach (var faction in preset.buildingPlacement.InitialUnitsConfig.Factions)
                {
                    if (faction == null || faction.FactionId is not (1 or 2)) continue;
                    foreach (var entry in faction.Buildings)
                    {
                        Require(entry != null && entry.Prefab != null, "S073 authored building row is incomplete");
                        expected.Add(new ExpectedBuilding(faction.FactionId, entry.Prefab.name,
                            new int2(faction.SpawnCell.x + entry.OriginOffset.x, faction.SpawnCell.y + entry.OriginOffset.y)));
                    }
                }
                int faction1Expected = expected.Count(value => value.FactionId == 1);
                int faction2Expected = expected.Count(value => value.FactionId == 2);
                Require(expected.Count == ExpectedBuildingCount && faction1Expected == ExpectedFactionBuildings && faction2Expected == ExpectedFactionBuildings,
                    $"Preset building rows mismatch; total={expected.Count}, faction1={faction1Expected}, faction2={faction2Expected}");

                Scene boundScene = SceneManager.GetSceneByPath(BoundScene);

                JObject beforePins = HashPins();
                long entityOrderBefore = em.EntityOrderVersion;
                var links = Resources.FindObjectsOfTypeAll<RuntimeBuildingEntityLink>()
                    .Where(link => link != null && link.gameObject.scene.IsValid() && link.gameObject.scene.isLoaded && link.gameObject.activeInHierarchy)
                    .ToArray();
                using var actorEntities = actorsQuery.ToEntityArray(Allocator.Temp);
                var resolvedStarting = new List<StartingBuilding>();
                var usedActors = new HashSet<Entity>();
                for (int i = 0; i < expected.Count; i++)
                {
                    ExpectedBuilding row = expected[i];
                    var matchesForRow = new List<Entity>();
                    for (int j = 0; j < actorEntities.Length; j++)
                    {
                        Entity candidate = actorEntities[j];
                        RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(candidate);
                        Faction faction = em.GetComponentData<Faction>(candidate);
                        string key = em.GetComponentData<UnitSourcePrefabKey>(candidate).Value.ToString();
                        if (faction.Id == row.FactionId && info.OwnerFactionId == row.FactionId &&
                            key.Contains(row.PrefabName, StringComparison.Ordinal) && math.all(info.OriginCell == row.ExpectedOrigin))
                            matchesForRow.Add(candidate);
                    }
                    Require(matchesForRow.Count == 1,
                        $"Preset row must match one unique runtime building; query_counts maps={mapCount}, grids={gridCount}, surfaces={surfaceCount}, runtime_building_candidates={actorCount}; faction={row.FactionId}, prefab={row.PrefabName}, origin={row.ExpectedOrigin}, matches={matchesForRow.Count}");
                    Entity actor = matchesForRow[0];
                    Require(usedActors.Add(actor), "One runtime building matched multiple preset rows: " + row.PrefabName);
                    RuntimeBuildingCombatInfo actorInfo = em.GetComponentData<RuntimeBuildingCombatInfo>(actor);
                    Require(actorInfo.FootprintCells.x > 0 && actorInfo.FootprintCells.y > 0 &&
                        (long)actorInfo.FootprintCells.x * actorInfo.FootprintCells.y <= MaximumFootprintSamplesPerBuilding,
                        "Actual starting building footprint exceeds the complete sampling bound");
                    var rect = new Rect(grid.Origin.x + actorInfo.OriginCell.x * grid.CellSize,
                        grid.Origin.z + actorInfo.OriginCell.y * grid.CellSize,
                        actorInfo.FootprintCells.x * grid.CellSize, actorInfo.FootprintCells.y * grid.CellSize);
                    var actorLinks = links.Where(link => link.Entity == actor).ToArray();
                    resolvedStarting.Add(new StartingBuilding(row,
                        actor,
                        actorLinks.Length == 1 ? actorLinks[0].gameObject : null, rect));
                }
                List<GroundMesh> groundMeshes = ResolveLoadedTerrainMeshes(boundScene, resolvedStarting, out JArray sourceRows,
                    out bool terrainGeometryComplete, out JArray geometryFailures);
                var buildings = new JArray();
                for (int i = 0; i < resolvedStarting.Count; i++)
                    buildings.Add(CaptureBuilding(em, grid, surface, resolvedStarting[i], groundMeshes, terrainGeometryComplete, geometryFailures));

                Require(em.EntityOrderVersion == entityOrderBefore && JToken.DeepEquals(beforePins, HashPins()),
                    "Passive source/entity structural invariant changed");
                var result = new JObject
                {
                    ["kind"] = "S073StartingBuildingGroundIntersectionObservation", ["schema_version"] = 1,
                    ["scope"] = "SamePlayingSession_PresetRowsMatchedToAllTenActualRuntimeBuildings_Faction1SubsetFive_ActualLoadedAuthoredGroundTriangles_NotReadinessOrAcceptance",
                    ["unity_version"] = Application.unityVersion, ["world_name"] = world.Name,
                    ["world_sequence"] = world.SequenceNumber.ToString(), ["session_id"] = observedSession,
                    ["scenario_index"] = match.ScenarioIndex, ["seed"] = match.Seed, ["phase"] = match.Phase.ToString(),
                    ["operation_map_id"] = active.OperationMapId.ToString(), ["campaign_source_map_id"] = SourceMapId,
                    ["source_identity_hash"] = SourceIdentity, ["source_content_hash"] = SourceContent,
                    ["source_scene_guid"] = SceneGuid, ["bound_scene_path"] = BoundScene,
                    ["bound_scene_gameobjects_loaded"] = boundScene.IsValid() && boundScene.isLoaded,
                    ["expected_building_count"] = ExpectedBuildingCount, ["observed_building_count"] = buildings.Count,
                    ["query_counts"] = new JObject { ["active_map_metadata"] = mapCount, ["grid_config"] = gridCount,
                        ["map_surface"] = surfaceCount, ["runtime_building_candidates"] = actorCount },
                    ["faction1_expected_count"] = ExpectedFactionBuildings,
                    ["faction1_observed_count"] = resolvedStarting.Count(value => value.Expected.FactionId == 1),
                    ["faction2_expected_count"] = ExpectedFactionBuildings,
                    ["faction2_observed_count"] = resolvedStarting.Count(value => value.Expected.FactionId == 2),
                    ["terrain_source_count"] = sourceRows.Count, ["terrain_sources"] = sourceRows,
                    ["terrain_geometry_complete"] = terrainGeometryComplete, ["terrain_geometry_failures"] = geometryFailures,
                    ["buildings"] = buildings, ["all_mesh_contacts_complete"] = AllComplete(buildings),
                    ["mission_ready"] = false, ["source_fit_certified"] = false,
                    ["normal_input_playthrough"] = false, ["player_readiness"] = false,
                    ["source_pins_sha256"] = beforePins
                };
                WriteResult(result);
            }
            catch (Exception error) { FinishFailure(error); }
        }

        static JObject CaptureBuilding(EntityManager em, GridConfig grid, MapSurfaceComponent surface,
            StartingBuilding start, List<GroundMesh> groundMeshes, bool terrainGeometryComplete, JArray geometryFailures)
        {
            Entity actor = start.Actor;
            ExpectedBuilding expected = start.Expected;
            var row = new JObject
            {
                ["preset_faction_id"] = expected.FactionId, ["preset_prefab_name"] = expected.PrefabName,
                ["preset_expected_origin_cell"] = new JArray(expected.ExpectedOrigin.x, expected.ExpectedOrigin.y),
                ["faction_id"] = expected.FactionId
            };
            if (actor == Entity.Null || !em.Exists(actor)) { row["witness_status"] = "Unknown"; row["unavailable_reason"] = "RuntimeCombatEntityMissing"; return row; }
            row["actor_entity"] = new JArray(actor.Index, actor.Version);
            row["entity_position"] = Vec(em.GetComponentData<LocalTransform>(actor).Position);
            RuntimeBuildingCombatInfo info = em.GetComponentData<RuntimeBuildingCombatInfo>(actor);
            row["actor_origin_cell"] = new JArray(info.OriginCell.x, info.OriginCell.y);
            row["actor_footprint_cells"] = new JArray(info.FootprintCells.x, info.FootprintCells.y);
            GameObject root = start.Root;
            if (root == null) { row["witness_status"] = "Unknown"; row["unavailable_reason"] = "ActualVisualRootNotLoadedOrAmbiguous"; return row; }
            row["visual_root_name"] = root.name; row["visual_root_scene"] = root.scene.path;
            row["visual_root_global_id"] = GlobalObjectId.GetGlobalObjectIdSlow(root).ToString();
            row["visual_root_position"] = Vec(root.transform.position);
            row["visual_root_rotation"] = new JArray(root.transform.rotation.x, root.transform.rotation.y, root.transform.rotation.z, root.transform.rotation.w);
            row["visual_root_scale"] = Vec(root.transform.lossyScale);
            int2 origin = info.OriginCell; int2 size = info.FootprintCells;
            Rect footprint = start.Footprint;
            row["world_footprint"] = RectJson(footprint);
            var visualTriangles = ReadActualVisualTriangles(root, out JArray visualMeshes, out string visualFailure);
            row["visual_meshes"] = visualMeshes;
            if (visualFailure != null) { row["witness_status"] = "Unknown"; row["unavailable_reason"] = visualFailure; return row; }

            var samples = new JArray();
            int missingGround = 0, missingVisual = 0;
            for (int z = 0; z < size.y; z++)
            for (int x = 0; x < size.x; x++)
            {
                float wx = grid.Origin.x + (origin.x + x + .5f) * grid.CellSize;
                float wz = grid.Origin.z + (origin.y + z + .5f) * grid.CellSize;
                float baked = float.NaN;
                int2 cell = new(origin.x + x, origin.y + z);
                ref MapSurfaceBlob surfaceBlob = ref surface.SurfaceBlob.Value;
                if (MapSurfaceBlobAccess.TryGetPrimarySurface(ref surfaceBlob, cell, out MapSurfaceSample sample)) baked = sample.Height;
                bool hasGround = HighestGroundHit(groundMeshes, wx, wz, out float groundY, out string groundSource, out int groundTriangle);
                bool hasVisual = VerticalExtreme(visualTriangles, wx, wz, wantMinimum: true, out float visualY, out int visualTriangle);
                if (!hasGround) missingGround++;
                if (!hasVisual) missingVisual++;
                samples.Add(new JObject { ["x"] = wx, ["z"] = wz,
                    ["baked_surface_y"] = FiniteOrNull(baked), ["rendered_ground_hit"] = hasGround,
                    ["rendered_ground_y"] = hasGround ? (JToken)new JValue(groundY) : JValue.CreateNull(),
                    ["rendered_ground_source_id"] = groundSource, ["rendered_ground_triangle"] = hasGround ? (JToken)new JValue(groundTriangle) : JValue.CreateNull(),
                    ["lowest_building_mesh_y"] = hasVisual ? (JToken)new JValue(visualY) : JValue.CreateNull(),
                    ["building_triangle"] = hasVisual ? (JToken)new JValue(visualTriangle) : JValue.CreateNull(),
                    ["ground_minus_visual_bottom_m"] = hasGround && hasVisual ? (JToken)new JValue(groundY - visualY) : JValue.CreateNull(),
                    ["scope"] = "Exact loaded mesh triangles at footprint cell centers; positive delta means rendered ground is above lowest building triangle at this xz" });
            }
            row["samples"] = samples; row["missing_ground_sample_count"] = missingGround; row["missing_visual_sample_count"] = missingVisual;
            row["witness_status"] = missingGround == 0 && missingVisual == 0 && terrainGeometryComplete ? "Measured" : "Unknown";
            row["terrain_geometry_complete"] = terrainGeometryComplete;
            row["terrain_geometry_failures"] = geometryFailures.DeepClone();
            if (missingGround != 0 || missingVisual != 0) row["unavailable_reason"] = "OneOrMoreFootprintSamplesHadNoExactMeshIntersection";
            return row;
        }

        static List<GroundMesh> ResolveLoadedTerrainMeshes(Scene scene, List<StartingBuilding> buildings,
            out JArray sourceRows, out bool complete, out JArray failures)
        {
            sourceRows = new JArray(); failures = new JArray(); complete = true;
            var resolved = new List<GroundMesh>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                complete = false; failures.Add("BoundCampaignSceneNotLoaded"); return resolved;
            }
            var roots = new List<Transform>();
            var targetFootprints = buildings.Where(building => building.Actor != Entity.Null).Select(building => building.Footprint).ToList();
            var supportedNames = new HashSet<string>(StringComparer.Ordinal) { "Ground", "GroundHills", "Mountains", "Terrain" };
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            foreach (Transform transform in sceneRoot.GetComponentsInChildren<Transform>(true))
                if (supportedNames.Contains(transform.name)) roots.Add(transform);
            if (roots.Count == 0) { complete = false; failures.Add("NoGroundGroundHillsMountainsOrTerrainRootsInBoundScene"); }

            var rendererOwners = new HashSet<MeshRenderer>();
            foreach (Transform root in roots)
            {
                Terrain[] nativeTerrains = root.GetComponentsInChildren<Terrain>(true);
                foreach (Terrain terrain in nativeTerrains)
                {
                    complete = false;
                    failures.Add("UnsupportedUnityTerrainComponent:" + GlobalObjectId.GetGlobalObjectIdSlow(terrain).ToString());
                }
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true)) rendererOwners.Add(renderer);
            }
            foreach (MeshRenderer renderer in rendererOwners)
            {
                var sourceRow = new JObject { ["source_global_object_id"] = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString(),
                    ["source_path"] = HierarchyPath(renderer.transform, null), ["root_group"] = RootGroup(renderer.transform, roots),
                    ["mesh_name"] = renderer.GetComponent<MeshFilter>()?.sharedMesh?.name,
                    ["scene_path"] = renderer.gameObject.scene.path };
                MeshFilter filter = renderer.GetComponent<MeshFilter>(); Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                { sourceRow["status"] = "NotRendered"; sourceRows.Add(sourceRow); continue; }
                if (!AnyFootprintOverlap(renderer.bounds, buildings)) { sourceRow["status"] = "OutsideAllStartingFootprints"; sourceRows.Add(sourceRow); continue; }
                sourceRow["renderer_world_bounds"] = BoundsJson(renderer.bounds);
                sourceRow["renderer_transform"] = MatrixJson(renderer.transform.localToWorldMatrix);
                if (mesh == null)
                { sourceRow["status"] = "MeshMissing"; sourceRows.Add(sourceRow); complete = false; failures.Add((string)sourceRow["source_global_object_id"] + ":MeshMissing"); continue; }
                sourceRow["mesh_guid"] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mesh));
                if (!TryReadTriangles(mesh, renderer.transform.localToWorldMatrix, out List<Triangle> triangles, out string failure, targetFootprints))
                { sourceRow["status"] = "MeshUnreadable"; sourceRow["unavailable_reason"] = failure; sourceRows.Add(sourceRow); complete = false; failures.Add((string)sourceRow["source_global_object_id"] + ":" + failure); continue; }
                sourceRow["status"] = "LoadedTriangles"; sourceRow["triangle_count"] = triangles.Count;
                sourceRows.Add(sourceRow); resolved.Add(new GroundMesh((string)sourceRow["source_global_object_id"], renderer.bounds, triangles));
            }
            if (resolved.Count == 0) { complete = false; failures.Add("NoLoadedGroundTrianglesOverlapStartingFootprints"); }
            return resolved;
        }

        static string RootGroup(Transform value, List<Transform> roots)
        {
            for (Transform current = value; current != null; current = current.parent)
                if (roots.Contains(current)) return current.name;
            return "Unknown";
        }

        static bool AnyFootprintOverlap(Bounds bounds, List<StartingBuilding> buildings)
        {
            foreach (StartingBuilding building in buildings)
                if (building.Actor != Entity.Null && RectOverlap(building.Footprint,
                    new Rect(bounds.min.x, bounds.min.z, bounds.size.x, bounds.size.z))) return true;
            return false;
        }

        static bool RectOverlap(Rect a, Rect b) => a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;

        static List<Triangle> ReadActualVisualTriangles(GameObject root, out JArray meshRows, out string failure)
        {
            meshRows = new JArray(); failure = null; var triangles = new List<Triangle>();
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) { failure = "NoActiveMeshRendererOnActualRoot"; return triangles; }
            foreach (MeshRenderer renderer in renderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) { failure = "RendererMeshFilterMissing"; return triangles; }
                if (!TryReadTriangles(filter.sharedMesh, renderer.transform.localToWorldMatrix, out var part, out failure)) return triangles;
                triangles.AddRange(part);
                meshRows.Add(new JObject { ["renderer_path"] = HierarchyPath(renderer.transform, root.transform),
                    ["mesh_name"] = filter.sharedMesh.name, ["mesh_guid"] = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(filter.sharedMesh)),
                    ["renderer_world_bounds"] = BoundsJson(renderer.bounds), ["triangle_count"] = part.Count });
            }
            return triangles;
        }

        static bool TryReadTriangles(Mesh mesh, Matrix4x4 world, out List<Triangle> triangles, out string failure,
            List<Rect> clipFootprints = null)
        {
            triangles = new List<Triangle>(); failure = null;
            if (mesh == null || !mesh.isReadable) { failure = "MeshMissingOrNotReadable"; return false; }
            try
            {
                Vector3[] vertices = mesh.vertices;
                int index = 0;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    if (mesh.GetTopology(sub) != MeshTopology.Triangles) { failure = "NonTriangleSubmesh"; return false; }
                    int[] indices = mesh.GetTriangles(sub, true);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                        if ((uint)a >= vertices.Length || (uint)b >= vertices.Length || (uint)c >= vertices.Length) { failure = "MeshIndexOutOfRange"; return false; }
                        Vector3 pa = world.MultiplyPoint3x4(vertices[a]), pb = world.MultiplyPoint3x4(vertices[b]), pc = world.MultiplyPoint3x4(vertices[c]);
                        if (!Finite(pa) || !Finite(pb) || !Finite(pc)) { failure = "NonFiniteWorldVertex"; return false; }
                        if (clipFootprints != null && !TriangleOverlapsFootprints(pa, pb, pc, clipFootprints)) { index++; continue; }
                        triangles.Add(new Triangle(pa, pb, pc, index++));
                    }
                }
            }
            catch (Exception e) when (e is UnityException || e is InvalidOperationException || e is ArgumentException)
            { failure = "TriangleReadFailed:" + e.GetType().Name; return false; }
            if (triangles.Count == 0) { failure = "MeshHasNoTriangles"; return false; }
            return true;
        }

        static bool TriangleOverlapsFootprints(Vector3 a, Vector3 b, Vector3 c, List<Rect> footprints)
        {
            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x)), maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
            foreach (Rect rect in footprints)
                if (rect.xMin <= maxX && rect.xMax >= minX && rect.yMin <= maxZ && rect.yMax >= minZ) return true;
            return false;
        }

        static bool HighestGroundHit(List<GroundMesh> meshes, float x, float z, out float y, out string source, out int triangle)
        {
            y = float.NegativeInfinity; source = null; triangle = -1; bool found = false;
            foreach (GroundMesh mesh in meshes)
            {
                if (x < mesh.Bounds.min.x || x > mesh.Bounds.max.x || z < mesh.Bounds.min.z || z > mesh.Bounds.max.z) continue;
                foreach (Triangle tri in mesh.Triangles)
                    if (VerticalHit(tri, x, z, out float hit) && hit > y) { y = hit; source = mesh.SourceId; triangle = tri.Index; found = true; }
            }
            return found;
        }

        static bool VerticalExtreme(List<Triangle> triangles, float x, float z, bool wantMinimum, out float y, out int index)
        {
            y = wantMinimum ? float.PositiveInfinity : float.NegativeInfinity; index = -1;
            foreach (Triangle triangle in triangles)
                if (VerticalHit(triangle, x, z, out float hit) && (wantMinimum ? hit < y : hit > y)) { y = hit; index = triangle.Index; }
            return index >= 0;
        }

        static bool VerticalHit(Triangle t, float x, float z, out float y)
        {
            y = 0;
            float2 a = new(t.A.x, t.A.z), b = new(t.B.x, t.B.z), c = new(t.C.x, t.C.z), p = new(x, z);
            float den = Cross(b - a, c - a);
            if (math.abs(den) < Epsilon) return false;
            float u = Cross(b - p, c - p) / den, v = Cross(c - p, a - p) / den;
            if (u < -Epsilon || v < -Epsilon || u + v > 1f + Epsilon) return false;
            // Projected barycentric u/v are the weights of A/B respectively.
            y = u * t.A.y + v * t.B.y + (1f - u - v) * t.C.y;
            return math.isfinite(y);
        }

        static void ValidateTriangleMathControls()
        {
            var flat = new Triangle(new Vector3(0, 4, 0), new Vector3(1, 4, 0), new Vector3(0, 4, 1), 0);
            var sloped = new Triangle(new Vector3(0, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 0, 1), 1);
            var reversed = new Triangle(sloped.A, sloped.C, sloped.B, 2);
            if (!VerticalHit(flat, .25f, .25f, out float flatY) || Mathf.Abs(flatY - 4f) > .00001f ||
                !VerticalHit(sloped, .25f, .25f, out float slopeY) || Mathf.Abs(slopeY - .25f) > .00001f ||
                !VerticalHit(reversed, .25f, .25f, out float reverseY) || Mathf.Abs(reverseY - .25f) > .00001f ||
                !VerticalHit(sloped, .5f, 0f, out float edgeY) || Mathf.Abs(edgeY - .5f) > .00001f)
                throw new InvalidOperationException("World-space triangle contact barycentric control failed");
        }

        static bool AllComplete(JArray rows) => rows.Count == ExpectedBuildingCount && rows.All(token =>
            (string)token["witness_status"] == "Measured");
        static JObject HashPins()
        {
            var result = new JObject();
            foreach (string path in new[] { "Assets/Game/Scripts/Editor/Skirmish/S073StartingBuildingGroundIntersectionObserver.cs", ConfigPath,
                "Assets/Game/Resources/SkirmishIndustrialBasin.asset", "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs",
                "Assets/Game/Scripts/Systems/SkirmishPresetResolver.cs", "Assets/Game/Scripts/Systems/SkirmishLayoutAudit.cs",
                "Assets/Game/Scenes/Match.unity", "Assets/Game/Scripts/Components/InitialUnitsSpawnComponents.cs",
                "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs", "Assets/Game/Scripts/Components/RuntimeBuildingCombatComponents.cs",
                "Assets/Game/Scripts/Components/MapSurfaceComponents.cs", "Assets/Game/Scripts/Components/MapSurfaceBlobAccess.cs",
                "Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntityLink.cs", BoundScene, BoundScene + ".meta" })
            {
                string absolute = Path.GetFullPath(path);
                Require(File.Exists(absolute), "Pinned diagnostic input missing: " + path);
                using var sha = SHA256.Create(); result[path] = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(absolute))).Replace("-", "").ToLowerInvariant();
            }
            return result;
        }

        static void WriteResult(JObject result)
        {
            string path = Path.GetFullPath(Environment.GetEnvironmentVariable(OutputKey));
            Require(path.StartsWith("/private/tmp/", StringComparison.Ordinal) && !File.Exists(path), "Fresh /private/tmp output path required");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
            done = true; EditorApplication.update -= Observe;
                Debug.Log("[S073StartingBuildingGroundIntersection] result=Recorded scope=loaded_ground_triangle_contacts complete=" +
                result["all_mesh_contacts_complete"] + " path=" + path);
        }

        static void FinishFailure(Exception error)
        {
            done = true; EditorApplication.update -= Observe; Debug.LogException(error);
            Debug.LogError("[S073StartingBuildingGroundIntersection] result=Failed reason=" + error.Message);
        }

        static JObject Vec(float3 p) => new() { ["x"] = p.x, ["y"] = p.y, ["z"] = p.z };
        static JObject Vec(Vector3 p) => new() { ["x"] = p.x, ["y"] = p.y, ["z"] = p.z };
        static JToken FiniteOrNull(float v) => math.isfinite(v) ? new JValue(v) : JValue.CreateNull();
        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
        static float Cross(float2 a, float2 b) => a.x * b.y - a.y * b.x;
        static JObject RectJson(Rect r) => new() { ["min_x"] = r.xMin, ["min_z"] = r.yMin, ["max_x"] = r.xMax, ["max_z"] = r.yMax };
        static JObject BoundsJson(Bounds b) => new() { ["min"] = Vec(b.min), ["max"] = Vec(b.max), ["center"] = Vec(b.center), ["size"] = Vec(b.size) };
        static JArray MatrixJson(Matrix4x4 m) { var a = new JArray(); for (int i = 0; i < 16; i++) a.Add(m[i]); return a; }
        static string HierarchyPath(Transform value, Transform root) { var parts = new List<string>(); for (Transform t = value; t != null; t = t.parent) { parts.Add(t.name); if (t == root) break; } parts.Reverse(); return string.Join("/", parts); }
        static bool BoundsNear(Bounds a, Bounds b) => Vector3.Distance(a.center, b.center) <= .005f && Vector3.Distance(a.size, b.size) <= .01f;
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        readonly struct Triangle
        {
            public readonly Vector3 A, B, C; public readonly int Index;
            public Triangle(Vector3 a, Vector3 b, Vector3 c, int index) { A = a; B = b; C = c; Index = index; }
        }
        sealed class GroundMesh
        {
            public readonly string SourceId; public readonly Bounds Bounds; public readonly List<Triangle> Triangles;
            public GroundMesh(string id, Bounds bounds, List<Triangle> triangles) { SourceId = id; Bounds = bounds; Triangles = triangles; }
        }

        readonly struct StartingBuilding
        {
            public readonly ExpectedBuilding Expected;
            public readonly Entity Actor;
            public readonly GameObject Root;
            public readonly Rect Footprint;
            public StartingBuilding(ExpectedBuilding expected, Entity actor, GameObject root, Rect footprint)
            { Expected = expected; Actor = actor; Root = root; Footprint = footprint; }
        }
        readonly struct ExpectedBuilding
        {
            public readonly int FactionId;
            public readonly string PrefabName;
            public readonly int2 ExpectedOrigin;
            public ExpectedBuilding(int factionId, string prefabName, int2 expectedOrigin)
            { FactionId = factionId; PrefabName = prefabName; ExpectedOrigin = expectedOrigin; }
        }
    }
}
#endif
