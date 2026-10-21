#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
                    if (!key.Contains("Chr_") || !selected.Add(key) || rows.Count >= 12 || em.HasComponent<UnitTransportPassenger>(entity)) continue;
                    var transform = em.GetComponentData<LocalTransform>(entity); var surface = em.GetComponentData<UnitSurfaceComponent>(entity);
                    Entity visual = em.HasComponent<UnitDetailedVisualReference>(entity) ? em.GetComponentData<UnitDetailedVisualReference>(entity).Root : entity;
                    bool measured = SkirmishCameraPosedMeshReader.TryCollect(em, visual, out var meshes, out string error);
                    float feet = float.PositiveInfinity;
                    if (measured) foreach (var mesh in meshes) feet = Mathf.Min(feet, mesh.WorldBounds.min.y);
                    var row = new JObject { ["entity"] = entity.ToString(), ["prefab"] = key,
                        ["root_x"] = transform.Position.x, ["root_y"] = transform.Position.y, ["root_z"] = transform.Position.z,
                        ["scale"] = transform.Scale, ["sampled_height"] = surface.LastSampledHeight,
                        ["has_surface"] = surface.HasSurface, ["grounded"] = surface.IsGrounded,
                        ["measured_posed_mesh"] = measured, ["mesh_error"] = error,
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
                            for (int i = 0; i < db.Placements.Length; i++)
                            {
                                ref var placement = ref db.Placements[i]; float3 p = placement.WorldMatrix.c3.xyz;
                                if (math.distance(p.xz, transform.Position.xz) > 60) continue;
                                string category = placement.SemanticCategory.ToString(); nearby[category] = (int?)nearby[category] + 1 ?? 1;
                            }
                            row["environment_database"] = db.OperationMapId.ToString(); row["authored_placements_within_60m_by_category"] = nearby;
                        }
                    }
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
        }
    }
}
#endif
