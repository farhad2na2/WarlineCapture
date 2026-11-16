#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Source-only preview survey of exact S073 render triangles against the pinned baked surface.
    /// This does not enter Play mode, create a World, edit source assets, or establish runtime readiness.
    /// </summary>
    public static class S073SourceVisualGeometryPreviewSurvey
    {
        const string OutputEnvironmentVariable = "WARLINE_S073_SOURCE_VISUAL_GEOMETRY_OUTPUT";
        const string MapPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset";
        const string ForcesPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset";
        const string ManifestPath = "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/StaticMapPresentationManifest.asset";
        const string SurveyPath = "Assets/Game/Scripts/Editor/Skirmish/S073SourceVisualGeometryPreviewSurvey.cs";
        const string SceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        const string ScenePath = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        const float MaxBuildingSlopeDegrees = 8f;
        const float MaxFoundationHeightDelta = 0.5f;
        const float MaxFoundationElevation = 0.75f;
        const float VisualObstructionTolerance = 0.5f;
        const int GridPointSamplesPerAxis = 3;

        struct CellSample
        {
            public bool HasSurface;
            public MapSurfaceSample Surface;
            public float RenderTop;
            public byte GeometryKinds;
            public byte UnknownKinds;
        }

        sealed class GeometryRecord
        {
            public string Name;
            public string SourceGlobalObjectId;
            public string MeshGuid;
            public long MeshLocalId;
            public string Kind;
            public int TriangleCount;
            public int DegenerateProjectedTriangleCount;
            public int RasterizedCellCount;
            public int UnknownCellCount;
            public float MaxHeightAboveBakedSurface;
            public JArray TransformChain;
        }

        [MenuItem("Tools/Warline/Skirmish/Record S073 Source Visual Geometry Preview")]
        public static void RunS073SourceVisualGeometryPreviewSurvey()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Source geometry survey requires Edit mode.");
            string outputPath = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new InvalidOperationException("Set " + OutputEnvironmentVariable + " to a fresh /private/tmp output path.");
            outputPath = Path.GetFullPath(outputPath);
            if (!outputPath.StartsWith("/private/tmp/", StringComparison.Ordinal) || File.Exists(outputPath))
                throw new InvalidOperationException("Output must be a fresh create-new file under /private/tmp.");

            string activeSceneBefore = SceneManager.GetActiveScene().path;
            Scene preview = default;
            try
            {
                OperationMapDefinition map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapPath);
                Require(map != null && map.OperationMapId == "opmap.skirmish.industrial_basin", "Exact S073 map definition unavailable.");
                Require(map.SourceBinding.IsConfigured && map.SourceBinding.SourceOperationMapId == "opmap.skirmish.desert_base_01" &&
                    map.SourceBinding.SourceIdentityHash == "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900",
                    "S073 physical source identity changed.");
                Require(map.NavigationMetadata.AuthoredSubSceneGuid == SceneGuid, "S073 authored subscene GUID changed.");
                string resolvedScenePath = AssetDatabase.GUIDToAssetPath(map.NavigationMetadata.AuthoredSubSceneGuid);
                Require(resolvedScenePath == ScenePath && AssetDatabase.AssetPathToGUID(ScenePath) == SceneGuid,
                    "Exact S073 authored subscene path/GUID mismatch.");

                string gridPath = AssetDatabase.GUIDToAssetPath(map.GridMetadata.AssetGuid);
                Require(!string.IsNullOrEmpty(gridPath) && HashFile(gridPath) == map.GridMetadata.ContentHash,
                    "Pinned grid asset path/content hash differs from S073 metadata.");
                string surfacePath = AssetDatabase.GUIDToAssetPath(map.SurfaceMetadata.AssetGuid);
                Require(!string.IsNullOrEmpty(surfacePath), "Pinned map surface GUID did not resolve.");
                Require(HashFile(surfacePath) == map.SurfaceMetadata.ContentHash, "Map surface content hash differs from S073 metadata.");
                var surfaceAsset = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(surfacePath);
                Require(surfaceAsset != null && surfaceAsset.Dimensions == map.GridMetadata.Dimensions &&
                    surfaceAsset.CellSize == map.GridMetadata.CellSize && surfaceAsset.GridOrigin == map.GridMetadata.Origin,
                    "Pinned surface frame differs from S073 grid frame.");
                Require(surfaceAsset.ComputeRuntimeBlobHash().ToString() == map.SurfaceMetadata.RuntimeBlobHash,
                    "Pinned surface runtime blob hash differs from S073 metadata.");
                Require(surfaceAsset.TryCreateRuntimeBlobAsset(Allocator.Temp, out BlobAssetReference<MapSurfaceBlob> surfaceBlob),
                    "Pinned map surface payload did not decode.");
                try
                {
                    preview = EditorSceneManager.OpenPreviewScene(ScenePath);
                    Require(preview.IsValid() && EditorSceneManager.IsPreviewScene(preview), "Unity did not open the exact scene as a preview scene.");

                    Vector3 gridOrigin = map.GridMetadata.Origin;
                    float cellSize = map.GridMetadata.CellSize;
                    int width = map.GridMetadata.Dimensions.x;
                    int height = map.GridMetadata.Dimensions.y;
                    int xMin = Mathf.Max(0, Mathf.CeilToInt((map.Bounds.PlayableMin.x - gridOrigin.x) / cellSize));
                    int xMax = Mathf.Min(width, Mathf.FloorToInt((map.Bounds.PlayableMax.x - gridOrigin.x) / cellSize));
                    int zMin = Mathf.Max(0, Mathf.CeilToInt((map.Bounds.PlayableMin.z - gridOrigin.z) / cellSize));
                    int zMax = Mathf.Min(height, Mathf.FloorToInt((map.Bounds.PlayableMax.z - gridOrigin.z) / cellSize));
                    Require(xMax > xMin && zMax > zMin, "Playable sample region is empty.");
                    int regionWidth = xMax - xMin;
                    int regionHeight = zMax - zMin;
                    var cells = new CellSample[regionHeight * regionWidth];
                    CaptureBakedSurface(ref surfaceBlob.Value, cells, xMin, zMin, regionWidth, regionHeight);

                    var geometryRows = new JArray();
                    var records = new List<GeometryRecord>();
                    foreach (GameObject root in preview.GetRootGameObjects())
                    {
                        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                                !BoundsIntersectsRegion(renderer.bounds, gridOrigin, cellSize, xMin, xMax, zMin, zMax)) continue;
                            string kind = GeometryKind(renderer.gameObject.name);
                            if (kind == null) continue;
                            var record = CaptureMesh(renderer, kind, cells, gridOrigin, cellSize, xMin, zMin, regionWidth, regionHeight);
                            if (record != null) records.Add(record);
                        }
                    }
                    Require(records.Any(r => r.Name.IndexOf("SM_Env_Mountain_03", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        r.MeshGuid == "6bfb7e568887f6f44b40fe4cd636dcd9" && !string.IsNullOrWhiteSpace(r.SourceGlobalObjectId)),
                        "Exact c001 Mountain_03 identity/mesh was not surveyed.");
                    Require(records.Any(r => r.Name.IndexOf("SM_Env_Mountain_05", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        r.MeshGuid == "2556ee89c91ec3644b76d957423fe598" && !string.IsNullOrWhiteSpace(r.SourceGlobalObjectId)),
                        "Exact c001 Mountain_05 identity/mesh was not surveyed.");

                    foreach (GeometryRecord record in records)
                    {
                        geometryRows.Add(new JObject
                        {
                            ["name"] = record.Name,
                            ["kind"] = record.Kind,
                            ["source_global_object_id"] = record.SourceGlobalObjectId,
                            ["mesh_guid"] = record.MeshGuid,
                            ["mesh_local_id"] = record.MeshLocalId,
                            ["transform_chain_child_to_root"] = record.TransformChain,
                            ["triangle_count"] = record.TriangleCount,
                            ["degenerate_projected_triangles"] = record.DegenerateProjectedTriangleCount,
                            ["rasterized_cells"] = record.RasterizedCellCount,
                            ["unknown_cells_fail_closed"] = record.UnknownCellCount,
                            ["max_triangle_height_above_baked_surface_m"] = FiniteOrNull(record.MaxHeightAboveBakedSurface)
                        });
                    }

                    JObject currentBuildings = CaptureFactionBuildings(ref surfaceBlob.Value, map, cells, xMin, zMin, regionWidth, regionHeight);
                    JArray cellRows = SerializeCells(cells, xMin, zMin, regionWidth, regionHeight, gridOrigin, cellSize);
                    string sceneHash = HashFile(ScenePath);
                    string mapHash = HashFile(MapPath);
                    string manifestHash = HashFile(ManifestPath);
                    string output = new JObject
                    {
                        ["kind"] = "S073SourceVisualGeometryPreviewSurvey",
                        ["schema_version"] = 1,
                        ["scope"] = "ExactAuthoredSubScenePreviewTrianglesVsPinnedBakedSurface_NotRuntimePlacement_NotLiveScene_NotReadiness",
                        ["unity_version"] = Application.unityVersion,
                        ["play_mode"] = false,
                        ["active_scene_path_before"] = activeSceneBefore,
                        ["active_scene_path_after_sampling"] = SceneManager.GetActiveScene().path,
                        ["preview_scene_close_pending"] = true,
                        ["source_pins"] = new JObject
                        {
                            [MapPath] = mapHash,
                            [ScenePath] = sceneHash,
                            [ManifestPath] = manifestHash,
                            [ForcesPath] = HashFile(ForcesPath),
                            [gridPath] = HashFile(gridPath),
                            [surfacePath] = HashFile(surfacePath),
                            [SurveyPath] = HashFile(SurveyPath)
                        },
                        ["map"] = new JObject
                        {
                            ["operation_map_id"] = map.OperationMapId,
                            ["source_identity_hash"] = map.SourceIdentityHash,
                            ["content_hash"] = map.ContentHash,
                            ["generated_metadata_hash"] = map.GeneratedMetadataHash,
                            ["authored_subscene_guid"] = map.NavigationMetadata.AuthoredSubSceneGuid,
                            ["authored_subscene_path"] = resolvedScenePath,
                        ["physical_source_operation_map_id"] = map.SourceBinding.SourceOperationMapId,
                        ["physical_source_identity_hash"] = map.SourceBinding.SourceIdentityHash,
                        ["physical_source_content_hash"] = map.SourceBinding.SourceContentHash,
                            ["grid_origin"] = Vector3Json(gridOrigin),
                            ["grid_dimensions"] = new JArray(width, height),
                            ["cell_size_m"] = cellSize,
                            ["playable_bounds"] = new JObject { ["min"] = Vector3Json(map.Bounds.PlayableMin), ["max"] = Vector3Json(map.Bounds.PlayableMax) }
                        },
                        ["surface"] = new JObject
                        {
                            ["asset_path"] = surfacePath,
                            ["asset_guid"] = map.SurfaceMetadata.AssetGuid,
                            ["content_hash"] = map.SurfaceMetadata.ContentHash,
                            ["runtime_blob_hash"] = map.SurfaceMetadata.RuntimeBlobHash,
                            ["payload_version"] = surfaceAsset.PayloadVersion,
                            ["payload_encoding"] = surfaceAsset.PayloadEncoding,
                            ["surface_count"] = surfaceAsset.SurfaceCount
                        },
                        ["sampling"] = new JObject
                        {
                            ["cell_region"] = new JArray(xMin, zMin, xMax, zMax),
                            ["mesh_sample_points_per_cell"] = 9,
                            ["subcell_offsets"] = new JArray(0.25f, 0.5f, 0.75f),
                            ["triangle_projection"] = "World-space MeshFilter triangles; barycentric height at nine points per 1m cell; maximum rendered terrain height compared with primary baked surface sample.",
                            ["visual_obstruction_threshold_m"] = VisualObstructionTolerance,
                            ["unknown_or_unreadable_meshes"] = "Fail closed for their renderer AABB cells; listed per geometry row."
                        },
                        ["geometry"] = geometryRows,
                        ["faction_1_buildings"] = currentBuildings["faction_1"],
                        ["faction_2_protected_buildings"] = currentBuildings["faction_2"],
                        ["candidate_cell_mask"] = cellRows,
                        ["limitations"] = new JArray(
                            "This is a source asset preview, not a loaded native S073 runtime world.",
                            "The cell mask samples triangle height at nine points per grid cell; it is not a full triangle-cell intersection proof.",
                            "MapSurface road flags are recorded, but the runtime GridRoad owner and occupancy are not reconstructed here.",
                            "A clear mask cell is a candidate for native validation, not placement or mission acceptance.")
                    }.ToString(Formatting.Indented) + "\n";
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    using (var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream)) writer.Write(output);
                    Debug.Log("[S073SourceVisualGeometryPreview] result=Recorded scene=" + ScenePath + " cells=" + cells.Length + " meshes=" + records.Count + " path=" + outputPath);
                }
                finally
                {
                    if (preview.IsValid() && EditorSceneManager.IsPreviewScene(preview))
                        EditorSceneManager.ClosePreviewScene(preview);
                    surfaceBlob.Dispose();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[S073SourceVisualGeometryPreview] result=Failed scope=source-preview-only error=" + exception);
                throw;
            }
        }

        static void CaptureBakedSurface(ref MapSurfaceBlob blob, CellSample[] cells, int xMin, int zMin, int width, int height)
        {
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                if (MapSurfaceBlobAccess.TryGetPrimarySurface(ref blob, new int2(xMin + x, zMin + z), out MapSurfaceSample sample))
                {
                    cells[index].HasSurface = true;
                    cells[index].Surface = sample;
                    cells[index].RenderTop = float.NegativeInfinity;
                }
                else cells[index].RenderTop = float.NegativeInfinity;
            }
        }

        static GeometryRecord CaptureMesh(MeshRenderer renderer, string kind, CellSample[] cells, Vector3 origin,
            float cellSize, int xMin, int zMin, int regionWidth, int regionHeight)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            var identity = renderer.GetComponent<OperationMapEntityPresentationIdentityAuthoring>();
            var record = new GeometryRecord
            {
                Name = renderer.gameObject.name,
                Kind = kind,
                SourceGlobalObjectId = identity != null ? identity.SourceGlobalObjectId : null,
                MeshGuid = null,
                MeshLocalId = 0,
                TransformChain = TransformChainJson(renderer.transform),
                MaxHeightAboveBakedSurface = float.NegativeInfinity
            };
            if (mesh == null)
            {
                MarkUnknownBounds(renderer.bounds, kind, cells, origin, cellSize, xMin, zMin, regionWidth, regionHeight);
                record.UnknownCellCount = CountUnknownForBounds(renderer.bounds, cells, origin, cellSize, xMin, zMin, regionWidth, regionHeight);
                return record;
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out record.MeshGuid, out record.MeshLocalId);
            try
            {
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                record.TriangleCount = triangles.Length / 3;
                var touched = new Dictionary<int, float>();
                Transform transform = renderer.transform;
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    Vector3 a = transform.TransformPoint(vertices[triangles[i]]);
                    Vector3 b = transform.TransformPoint(vertices[triangles[i + 1]]);
                    Vector3 c = transform.TransformPoint(vertices[triangles[i + 2]]);
                    float area = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
                    if (Mathf.Abs(area) < 0.000001f)
                    {
                        record.DegenerateProjectedTriangleCount++;
                        MarkDegenerateProjection(a, b, c, kind, cells, origin, cellSize, xMin, zMin, regionWidth, regionHeight);
                        continue;
                    }
                    int tx0 = Mathf.Max(xMin, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - origin.x) / cellSize));
                    int tx1 = Mathf.Min(xMin + regionWidth - 1, Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - origin.x) / cellSize));
                    int tz0 = Mathf.Max(zMin, Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - origin.z) / cellSize));
                    int tz1 = Mathf.Min(zMin + regionHeight - 1, Mathf.FloorToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - origin.z) / cellSize));
                    for (int gz = tz0; gz <= tz1; gz++)
                    for (int gx = tx0; gx <= tx1; gx++)
                    for (int oz = 1; oz <= GridPointSamplesPerAxis; oz++)
                    for (int ox = 1; ox <= GridPointSamplesPerAxis; ox++)
                    {
                        float px = origin.x + (gx + ox / 4f) * cellSize;
                        float pz = origin.z + (gz + oz / 4f) * cellSize;
                        if (!TryTriangleHeight(a, b, c, px, pz, area, out float y)) continue;
                        int index = (gz - zMin) * regionWidth + gx - xMin;
                        if (!touched.TryGetValue(index, out float old) || y > old) touched[index] = y;
                    }
                }
                record.RasterizedCellCount = touched.Count;
                foreach (var pair in touched)
                {
                    CellSample cell = cells[pair.Key];
                    if (pair.Value > cell.RenderTop) cell.RenderTop = pair.Value;
                    cell.GeometryKinds |= KindBit(kind);
                    if (cell.HasSurface)
                        record.MaxHeightAboveBakedSurface = Mathf.Max(record.MaxHeightAboveBakedSurface, pair.Value - cell.Surface.Height);
                    cells[pair.Key] = cell;
                }
            }
            catch (Exception)
            {
                MarkUnknownBounds(renderer.bounds, kind, cells, origin, cellSize, xMin, zMin, regionWidth, regionHeight);
                record.UnknownCellCount = CountUnknownForBounds(renderer.bounds, cells, origin, cellSize, xMin, zMin, regionWidth, regionHeight);
            }
            return record;
        }

        static bool TryTriangleHeight(Vector3 a, Vector3 b, Vector3 c, float x, float z, float area, out float y)
        {
            float wa = ((b.x - x) * (c.z - z) - (b.z - z) * (c.x - x)) / area;
            float wb = ((c.x - x) * (a.z - z) - (c.z - z) * (a.x - x)) / area;
            float wc = 1f - wa - wb;
            y = wa * a.y + wb * b.y + wc * c.y;
            return wa >= -0.0001f && wb >= -0.0001f && wc >= -0.0001f;
        }

        static void MarkDegenerateProjection(Vector3 a, Vector3 b, Vector3 c, string kind, CellSample[] cells,
            Vector3 origin, float cellSize, int xMin, int zMin, int regionWidth, int regionHeight)
        {
            Vector3 p = a, q = b;
            float ab = (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);
            float ac = (a.x - c.x) * (a.x - c.x) + (a.z - c.z) * (a.z - c.z);
            float bc = (b.x - c.x) * (b.x - c.x) + (b.z - c.z) * (b.z - c.z);
            if (ac > ab && ac >= bc) { p = a; q = c; }
            else if (bc > ab && bc > ac) { p = b; q = c; }
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(q.x, q.z)) / Mathf.Max(0.01f, cellSize * 0.25f)));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int gx = Mathf.FloorToInt((Mathf.Lerp(p.x, q.x, t) - origin.x) / cellSize);
                int gz = Mathf.FloorToInt((Mathf.Lerp(p.z, q.z, t) - origin.z) / cellSize);
                if (gx < xMin || gz < zMin || gx >= xMin + regionWidth || gz >= zMin + regionHeight) continue;
                int index = (gz - zMin) * regionWidth + gx - xMin;
                cells[index].UnknownKinds |= KindBit(kind);
            }
        }

        static JObject CaptureFactionBuildings(ref MapSurfaceBlob blob, OperationMapDefinition map, CellSample[] cells,
            int xMin, int zMin, int regionWidth, int regionHeight)
        {
            var result = new JObject { ["faction_1"] = new JArray(), ["faction_2"] = new JArray() };
            var forces = AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(ForcesPath);
            Require(forces != null, "Exact InitialForces config unavailable.");
            foreach (var faction in forces.Factions)
            {
                JArray rows = faction.FactionId == 1 ? (JArray)result["faction_1"] : (JArray)result["faction_2"];
                foreach (var entry in faction.Buildings)
                {
                    GameObject prefab = entry.Prefab;
                    var authoring = prefab != null ? prefab.GetComponentInChildren<BuildingDefinitionAuthoring>(true) : null;
                    Require(authoring != null, "Building definition authoring missing: " + (prefab != null ? prefab.name : "null"));
                    Vector2Int footprint = authoring.ConfiguredFootprintCells;
                    Vector2Int origin = faction.SpawnCell + entry.OriginOffset;
                    float minHeight = float.PositiveInfinity, maxHeight = float.NegativeInfinity, maxSlope = 0f, maxVisualDelta = float.NegativeInfinity;
                    bool levelPass = true, geometryUnknown = false, visualIntersection = false;
                    var geometryKinds = new HashSet<string>();
                    for (int z = 0; z < footprint.y; z++)
                    for (int x = 0; x < footprint.x; x++)
                    {
                        int gx = origin.x + x, gz = origin.y + z;
                        if (gx < xMin || gz < zMin || gx >= xMin + regionWidth || gz >= zMin + regionHeight)
                        { levelPass = false; continue; }
                        CellSample cell = cells[(gz - zMin) * regionWidth + gx - xMin];
                        if (!cell.HasSurface) { levelPass = false; continue; }
                        MapSurfaceSample sample = cell.Surface;
                        minHeight = Mathf.Min(minHeight, sample.Height); maxHeight = Mathf.Max(maxHeight, sample.Height);
                        maxSlope = Mathf.Max(maxSlope, sample.SlopeDegrees);
                        if (sample.SlopeDegrees > MaxBuildingSlopeDegrees) levelPass = false;
                        if (cell.GeometryKinds != 0 && float.IsFinite(cell.RenderTop))
                        {
                            float delta = cell.RenderTop - sample.Height;
                            maxVisualDelta = Mathf.Max(maxVisualDelta, delta);
                            if (delta > VisualObstructionTolerance) visualIntersection = true;
                            if ((cell.GeometryKinds & 1) != 0) geometryKinds.Add("mountain");
                            if ((cell.GeometryKinds & 2) != 0) geometryKinds.Add("sand_dunes");
                            if ((cell.GeometryKinds & 4) != 0) geometryKinds.Add("ground_hill");
                            if ((cell.GeometryKinds & 8) != 0) geometryKinds.Add("ground_mesh");
                        }
                        if (cell.UnknownKinds != 0) { geometryUnknown = true; visualIntersection = true; }
                    }
                    float deltaHeight = maxHeight - minHeight;
                    if (!float.IsFinite(minHeight) || deltaHeight > MaxFoundationHeightDelta || maxHeight - map.GridMetadata.Origin.y > MaxFoundationElevation)
                        levelPass = false;
                    string prefabPath = AssetDatabase.GetAssetPath(prefab);
                    rows.Add(new JObject
                    {
                        ["name"] = prefab.name,
                        ["faction_id"] = faction.FactionId,
                        ["origin_cell"] = new JArray(origin.x, origin.y),
                        ["footprint_cells"] = new JArray(footprint.x, footprint.y),
                        ["prefab_path"] = prefabPath,
                        ["prefab_sha256"] = HashFile(prefabPath),
                        ["baked_surface_level_predicate_pass"] = levelPass,
                        ["height_delta_m"] = FiniteOrNull(deltaHeight),
                        ["max_slope_degrees"] = maxSlope,
                        ["max_visual_mesh_height_above_surface_m"] = FiniteOrNull(maxVisualDelta),
                        ["visual_mesh_above_surface_over_tolerance"] = visualIntersection,
                        ["unreadable_mesh_geometry_fail_closed"] = geometryUnknown,
                        ["geometry_kinds"] = new JArray(geometryKinds)
                    });
                }
            }
            return result;
        }

        static JArray SerializeCells(CellSample[] cells, int xMin, int zMin, int width, int height, Vector3 origin, float cellSize)
        {
            var rows = new JArray();
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                CellSample cell = cells[z * width + x];
                JToken sample = cell.HasSurface ? (JToken)new JObject
                {
                    ["height_m"] = cell.Surface.Height,
                    ["slope_degrees"] = cell.Surface.SlopeDegrees,
                    ["surface_type"] = (byte)cell.Surface.SurfaceType,
                    ["movement_mask"] = (ushort)cell.Surface.MovementMask,
                    ["flags"] = (ushort)cell.Surface.Flags,
                    ["layer_id"] = cell.Surface.LayerId
                } : JValue.CreateNull();
                rows.Add(new JObject
                {
                    ["cell"] = new JArray(xMin + x, zMin + z),
                    ["surface"] = sample,
                    ["max_render_triangle_y_m"] = FiniteOrNull(cell.RenderTop),
                    ["render_height_above_surface_m"] = cell.HasSurface && float.IsFinite(cell.RenderTop)
                        ? (JToken)new JValue(cell.RenderTop - cell.Surface.Height) : JValue.CreateNull(),
                    ["geometry_kind_mask"] = cell.GeometryKinds,
                    ["unknown_geometry_kind_mask_fail_closed"] = cell.UnknownKinds,
                    ["building_placement_surface_bit"] = cell.HasSurface && (cell.Surface.MovementMask & MapSurfaceMovementMask.BuildingPlacement) != 0,
                    ["road_surface_flag_or_type"] = cell.HasSurface && ((cell.Surface.Flags & MapSurfaceFlags.Road) != 0 ||
                        cell.Surface.SurfaceType == MapSurfaceType.Road || cell.Surface.SurfaceType == MapSurfaceType.DirtRoad ||
                        cell.Surface.SurfaceType == MapSurfaceType.BridgeDeck || cell.Surface.SurfaceType == MapSurfaceType.Highway ||
                        cell.Surface.SurfaceType == MapSurfaceType.Ramp),
                    ["slope_pass_8deg"] = cell.HasSurface && cell.Surface.SlopeDegrees <= MaxBuildingSlopeDegrees,
                    ["visual_mesh_above_surface_over_tolerance"] = cell.HasSurface && float.IsFinite(cell.RenderTop) &&
                        cell.RenderTop - cell.Surface.Height > VisualObstructionTolerance
                });
            }
            return rows;
        }

        static void MarkUnknownBounds(Bounds bounds, string kind, CellSample[] cells, Vector3 origin, float cellSize,
            int xMin, int zMin, int regionWidth, int regionHeight)
        {
            int x0 = Mathf.Max(xMin, Mathf.FloorToInt((bounds.min.x - origin.x) / cellSize));
            int x1 = Mathf.Min(xMin + regionWidth - 1, Mathf.FloorToInt((bounds.max.x - origin.x) / cellSize));
            int z0 = Mathf.Max(zMin, Mathf.FloorToInt((bounds.min.z - origin.z) / cellSize));
            int z1 = Mathf.Min(zMin + regionHeight - 1, Mathf.FloorToInt((bounds.max.z - origin.z) / cellSize));
            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                int index = (z - zMin) * regionWidth + x - xMin;
                cells[index].UnknownKinds |= KindBit(kind);
            }
        }

        static int CountUnknownForBounds(Bounds bounds, CellSample[] cells, Vector3 origin, float cellSize,
            int xMin, int zMin, int regionWidth, int regionHeight)
        {
            int count = 0;
            int x0 = Mathf.Max(xMin, Mathf.FloorToInt((bounds.min.x - origin.x) / cellSize));
            int x1 = Mathf.Min(xMin + regionWidth - 1, Mathf.FloorToInt((bounds.max.x - origin.x) / cellSize));
            int z0 = Mathf.Max(zMin, Mathf.FloorToInt((bounds.min.z - origin.z) / cellSize));
            int z1 = Mathf.Min(zMin + regionHeight - 1, Mathf.FloorToInt((bounds.max.z - origin.z) / cellSize));
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++) count++;
            return count;
        }

        static bool BoundsIntersectsRegion(Bounds bounds, Vector3 origin, float cellSize, int xMin, int xMax, int zMin, int zMax)
        {
            float minX = origin.x + xMin * cellSize, maxX = origin.x + xMax * cellSize;
            float minZ = origin.z + zMin * cellSize, maxZ = origin.z + zMax * cellSize;
            return bounds.max.x >= minX && bounds.min.x <= maxX && bounds.max.z >= minZ && bounds.min.z <= maxZ;
        }

        static string GeometryKind(string name)
        {
            if (name.IndexOf("Mountain", StringComparison.OrdinalIgnoreCase) >= 0) return "mountain";
            if (name.IndexOf("SandDune", StringComparison.OrdinalIgnoreCase) >= 0) return "sand_dunes";
            if (name.IndexOf("Ground_Hill", StringComparison.OrdinalIgnoreCase) >= 0) return "ground_hill";
            if (name.IndexOf("Ground_Round", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Ground_Square", StringComparison.OrdinalIgnoreCase) >= 0) return "ground_mesh";
            return null;
        }

        static byte KindBit(string kind) => kind == "mountain" ? (byte)1 : kind == "sand_dunes" ? (byte)2 : kind == "ground_hill" ? (byte)4 : (byte)8;
        static JArray TransformChainJson(Transform transform)
        {
            var chain = new JArray();
            for (Transform current = transform; current != null; current = current.parent)
                chain.Add(new JObject
                {
                    ["name"] = current.name,
                    ["local_position"] = Vector3Json(current.localPosition),
                    ["local_rotation_xyzw"] = new JArray(current.localRotation.x, current.localRotation.y, current.localRotation.z, current.localRotation.w),
                    ["local_scale"] = Vector3Json(current.localScale),
                    ["world_position"] = Vector3Json(current.position),
                    ["world_lossy_scale"] = Vector3Json(current.lossyScale)
                });
            return chain;
        }
        static JArray Vector3Json(Vector3 value) => new JArray(value.x, value.y, value.z);
        static JToken FiniteOrNull(float value) => float.IsFinite(value) ? (JToken)new JValue(value) : JValue.CreateNull();
        static string HashFile(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
