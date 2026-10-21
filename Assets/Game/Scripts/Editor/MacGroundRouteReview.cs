#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Game.Editor
{
    // Read-only observation of normal mission input. Never writes orders, transforms or outcomes.
    [InitializeOnLoad]
    public static class MacGroundRouteReview
    {
        const string Active = "Warline.MacGroundRouteReview.Active";
        const string OutputKey = "Warline.MacGroundRouteReview.Output";
        static double nextSample;
        static int captures;
        static MacGroundRouteReview()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.ExitingPlayMode) SessionState.SetBool(Active, false); };
        }
        internal static void Begin(string mission)
        {
            string output = Path.GetFullPath(Environment.GetEnvironmentVariable("WARLINE_GROUND_ROUTE_OUTPUT") ??
                "Logs/MacGroundContactAriaRoute20261021/" + mission + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output); SessionState.SetString(OutputKey, output);
            SessionState.SetBool(Active, true); captures = 0; nextSample = 0;
            Debug.Log("[MacGroundRouteReview] begin=" + mission + " output=" + output);
        }
        public static void RunCampaignM01()
        {
            Begin("campaign-m01"); CampaignTutorialEntryEditorProbe.RunM01Journey();
        }
        static void MeasureRenderedSupport(EntityManager em, float3 point, JObject row)
        {
            float highest = float.NegativeInfinity; string source = null;
            using var query = em.CreateEntityQuery(typeof(WorldRenderBounds), typeof(MaterialMeshInfo), typeof(LocalToWorld));
            using var renderers = query.ToEntityArray(Allocator.Temp);
            foreach (var renderer in renderers)
            {
                var bounds = em.GetComponentData<WorldRenderBounds>(renderer).Value;
                if (math.any(math.abs(point.xz - bounds.Center.xz) > bounds.Extents.xz + .001f) ||
                    !em.HasComponent<RenderMeshArray>(renderer) || !em.IsComponentEnabled<MaterialMeshInfo>(renderer)) continue;
                var array = em.GetSharedComponentManaged<RenderMeshArray>(renderer);
                var info = em.GetComponentData<MaterialMeshInfo>(renderer);
                if (info.HasMaterialMeshIndexRange)
                {
                    var range = info.MaterialMeshIndexRange;
                    for (int i = range.start; i < range.end; i++)
                    {
                        var index = array.MaterialMeshIndices[i];
                        SampleRoadDraw(array.MeshReferences[index.MeshIndex].Value, index.SubMeshIndex,
                            em.GetComponentData<LocalToWorld>(renderer).Value, point, ref highest, ref source);
                    }
                }
                else if (info.Mesh < 0)
                    SampleRoadDraw(array.GetMesh(info), info.SubMesh, em.GetComponentData<LocalToWorld>(renderer).Value,
                        point, ref highest, ref source);
            }
            row["rendered_support_measured"] = math.isfinite(highest);
            if (math.isfinite(highest)) { row["rendered_support_y"] = highest; row["rendered_support_mesh"] = source;
                row["rendered_minus_sample"] = highest - (float)row["sampled_height"]; }
        }
        static void SampleRoadDraw(Mesh mesh, int submesh, float4x4 matrix, float3 point, ref float highest, ref string source)
        {
            if (mesh == null) return;
            string name = mesh.name.ToLowerInvariant();
            if (!name.Contains("road") && !name.Contains("street") && !name.Contains("pavement") && !name.Contains("sidewalk")) return;
            using var data = MeshUtility.AcquireReadOnlyMeshData(mesh);
            using var vertices = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp);
            using var indices = new NativeArray<int>(data[0].GetSubMesh(submesh).indexCount, Allocator.Temp);
            data[0].GetVertices(vertices); data[0].GetIndices(indices, submesh, true);
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                float3 a = math.transform(matrix, (float3)vertices[indices[i]]);
                float3 b = math.transform(matrix, (float3)vertices[indices[i + 1]]);
                float3 c = math.transform(matrix, (float3)vertices[indices[i + 2]]);
                float3 normal = math.cross(b - a, c - a);
                if (math.abs(math.normalizesafe(normal).y) < .7f) continue;
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (math.abs(denominator) < .000001f) continue;
                float u = ((b.z - c.z) * (point.x - c.x) + (c.x - b.x) * (point.z - c.z)) / denominator;
                float v = ((c.z - a.z) * (point.x - c.x) + (a.x - c.x) * (point.z - c.z)) / denominator;
                if (u < -.00001f || v < -.00001f || u + v > 1.00001f) continue;
                float y = u * a.y + v * b.y + (1 - u - v) * c.y;
                if (y > highest && y < point.y + 2) { highest = y; source = AssetDatabase.GetAssetPath(mesh); }
            }
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup < nextSample) return;
            nextSample = EditorApplication.timeSinceStartup + 15;
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;
            try
            {
                var em = world.EntityManager; em.CompleteAllTrackedJobs();
                using var query = em.CreateEntityQuery(typeof(UnitSurfaceComponent), typeof(UnitSourcePrefabKey), typeof(LocalTransform));
                using var entities = query.ToEntityArray(Allocator.Temp);
                var rows = new JArray(); var selected = new HashSet<string>();
                foreach (var entity in entities)
                {
                    string key = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
                    int faction = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : -1;
                    if (!key.Contains("Chr_") || !selected.Add(faction + ":" + key) || rows.Count >= 12 || em.HasComponent<UnitTransportPassenger>(entity)) continue;
                    var transform = em.GetComponentData<LocalTransform>(entity); var surface = em.GetComponentData<UnitSurfaceComponent>(entity);
                    var candidates = new List<(string, Entity)>();
                    if (em.HasComponent<UnitDetailedVisualReference>(entity)) candidates.Add(("Detail", em.GetComponentData<UnitDetailedVisualReference>(entity).Root));
                    if (em.HasComponent<UnitModelInstanceReference>(entity)) candidates.Add(("Model", em.GetComponentData<UnitModelInstanceReference>(entity).Instance));
                    if (em.HasComponent<UnitMidLodInstanceReference>(entity)) candidates.Add(("Mid", em.GetComponentData<UnitMidLodInstanceReference>(entity).Instance));
                    if (em.HasComponent<UnitLowLodInstanceReference>(entity)) candidates.Add(("Low", em.GetComponentData<UnitLowLodInstanceReference>(entity).Instance));
                    candidates.Add(("Root", entity));
                    bool measured = false; SkirmishCameraPosedMeshReader.Fact[] meshes = null;
                    var errors = new JObject(); string measuredVisual = null;
                    foreach (var candidate in candidates)
                    {
                        if (candidate.Item2 == Entity.Null || !em.Exists(candidate.Item2)) continue;
                        if (SkirmishCameraPosedMeshReader.TryCollect(em, candidate.Item2, out meshes, out string error))
                        { measured = true; measuredVisual = candidate.Item1; break; }
                        errors[candidate.Item1] = error;
                    }
                    float feet = float.PositiveInfinity;
                    if (measured) foreach (var mesh in meshes) feet = Mathf.Min(feet, mesh.WorldBounds.min.y);
                    var row = new JObject { ["entity"] = entity.ToString(), ["prefab"] = key, ["faction"] = faction,
                        ["root_x"] = transform.Position.x, ["root_y"] = transform.Position.y, ["root_z"] = transform.Position.z,
                        ["scale"] = transform.Scale, ["sampled_height"] = surface.LastSampledHeight,
                        ["has_surface"] = surface.HasSurface, ["grounded"] = surface.IsGrounded,
                        ["measured_posed_mesh"] = measured, ["mesh_errors"] = errors, ["measured_visual"] = measuredVisual,
                        ["animation_index"] = em.HasComponent<UnitResolvedAnimationIndex>(entity) ? em.GetComponentData<UnitResolvedAnimationIndex>(entity).Value : -1 };
                    if (measured) { row["posed_mesh_min_y"] = feet; row["mesh_min_above_sample"] = feet - surface.LastSampledHeight; }
                    if (em.HasBuffer<UnitAnimationGroundOffset>(entity))
                    { var offsets = em.GetBuffer<UnitAnimationGroundOffset>(entity, true); var values = new JArray(); foreach (var v in offsets) values.Add(v.Value); row["baked_animation_offsets"] = values; }
                    using var dbQuery = em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));
                    if (dbQuery.CalculateEntityCount() == 1)
                    {
                        var database = dbQuery.GetSingleton<OperationMapRenderDatabaseComponent>();
                        if (database.Blob.IsCreated)
                        {
                            ref var db = ref database.Blob.Value; var nearby = new JObject();
                            string inventory = Path.Combine(SessionState.GetString(OutputKey, ""), "environment-layout.json");
                            if (!File.Exists(inventory))
                            {
                                var cells = new JObject();
                                for (int i = 0; i < db.Placements.Length; i++)
                                {
                                    ref var placement = ref db.Placements[i]; var point = placement.WorldMatrix.c3.xyz;
                                    string bin = Mathf.FloorToInt(point.x / 64) + "," + Mathf.FloorToInt(point.z / 64);
                                    var cell = cells[bin] as JObject;
                                    if (cell == null) { cell = new JObject(); cells[bin] = cell; }
                                    string category = placement.SemanticCategory.ToString(); cell[category] = ((int?)cell[category] ?? 0) + 1;
                                }
                                File.WriteAllText(inventory, new JObject { ["map"] = db.OperationMapId.ToString(),
                                    ["placements"] = db.Placements.Length, ["cell_metres"] = 64, ["cells"] = cells }.ToString());
                            }
                            for (int i = 0; i < db.Placements.Length; i++)
                            {
                                ref var placement = ref db.Placements[i]; float3 p = placement.WorldMatrix.c3.xyz;
                                if (math.distance(p.xz, transform.Position.xz) > 60) continue;
                                string category = placement.SemanticCategory.ToString(); nearby[category] = (int?)nearby[category] + 1 ?? 1;
                            }
                            row["environment_database"] = db.OperationMapId.ToString(); row["authored_placements_within_60m_by_category"] = nearby;
                        }
                    }
                    MeasureRenderedSupport(em, transform.Position, row);
                    rows.Add(row);
                }
                if (rows.Count == 0) return;
                string output = SessionState.GetString(OutputKey, ""); int sample = ++captures;
                File.AppendAllText(Path.Combine(output, "contact.jsonl"), new JObject { ["frame"] = Time.frameCount,
                    ["utc"] = DateTime.UtcNow.ToString("O"), ["camera"] = Camera.main == null ? null : Camera.main.transform.position.ToString("F3"),
                    ["actors"] = rows }.ToString(Newtonsoft.Json.Formatting.None) + "\n");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "normal-view-" + sample + ".png"));
                Debug.Log("[MacGroundRouteReview] observed=" + rows.Count + " capture=" + sample);
            }
            catch (Exception error)
            {
                SessionState.SetBool(Active, false); Debug.LogException(error);
                Debug.LogError("[MacGroundRouteReview] observation=Failed");
            }
        }
    }
    public static partial class SkirmishS002AriaRunHarness
    {
        public static void RunS002GroundRouteReview()
        {
            MacGroundRouteReview.Begin("s002-fa");
            RunEditorJourney("fa-IR", false, false, "S002", 104731);
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_INPUT_DIAGNOSTIC", "1");
        }
    }
}
#endif
