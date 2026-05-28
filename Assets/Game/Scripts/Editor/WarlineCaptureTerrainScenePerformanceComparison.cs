#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public static class WarlineCaptureTerrainScenePerformanceComparison
{
    private const string Terrain7ScenePath = "Assets/Game/Scenes/Game_Terrain7.unity";
    private const string Terrain8ScenePath = "Assets/Game/Scenes/Game_Terrain8.unity";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/TerrainOptimizationComparison";
    private const string SummaryJsonPath = DataRoot + "/terrain7_vs_terrain8_comparison.json";
    private const string ReportPath = "Design/AgentReports/2026-05-28_gameplay_terrain7-vs-terrain8-performance-comparison.md";
    private const int WarmupFrames = 8;
    private const int SampleFrames = 48;
    private const int RenderWidth = 1600;
    private const int RenderHeight = 900;
    private const float NormalCameraHeight = 34f;
    private const float NormalCameraPitch = 40f;
    private const float NormalCameraFieldOfView = 36f;
    private const float BuildCameraHeight = 90f;
    private const float BuildCameraPitch = 64f;
    private const float BuildCameraFieldOfView = 52f;

    [MenuItem("WarlineCapture/Design/Terrain/Compare Terrain7 And Terrain8 Performance")]
    public static void RunComparison()
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        SceneComparison terrain7 = MeasureScene("Game_Terrain7", Terrain7ScenePath);
        SceneComparison terrain8 = MeasureScene("Game_Terrain8", Terrain8ScenePath);
        WriteSummaryJson(terrain7, terrain8);
        WriteReport(terrain7, terrain8);
        AssetDatabase.Refresh();

        Debug.Log("WARLINECAPTURE_TERRAIN_PERFORMANCE_COMPARISON_READY"
            + " terrain7Renderers=" + terrain7.StaticStats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " terrain8Renderers=" + terrain8.StaticStats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " terrain7AvgMs=" + terrain7.RenderStats.AverageRenderMilliseconds.ToString("F3", CultureInfo.InvariantCulture)
            + " terrain8AvgMs=" + terrain8.RenderStats.AverageRenderMilliseconds.ToString("F3", CultureInfo.InvariantCulture)
            + " report=" + ReportPath);
    }

    private static SceneComparison MeasureScene(string label, string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RenderSettings.fog = false;
        GameObject island = GameObject.Find("Island") ?? FindRootWithMostRenderers(scene);
        if (island == null)
            throw new InvalidOperationException("Could not find Island or renderable root in " + scenePath);

        List<MeshRenderer> renderers = CollectRenderableMeshRenderers(island.transform);
        Bounds bounds = CalculateBounds(renderers);
        StaticStats staticStats = MeasureStaticStats(renderers, island.transform);
        InstancingStats instancingStats = MeasureInstancing(renderers);
        RenderRouteStats renderStats = MeasureRenderRoute(label, bounds);
        return new SceneComparison(label, scenePath, staticStats, instancingStats, renderStats);
    }

    private static RenderRouteStats MeasureRenderRoute(string label, Bounds bounds)
    {
        Camera camera = null;
        Light light = null;
        RenderTexture target = null;
        ProfilerRecorder drawCalls = StartRecorder(ProfilerCategory.Render, "Draw Calls Count");
        ProfilerRecorder batches = StartRecorder(ProfilerCategory.Render, "Batches Count");
        ProfilerRecorder setPass = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        ProfilerRecorder triangles = StartRecorder(ProfilerCategory.Render, "Triangles Count");
        ProfilerRecorder vertices = StartRecorder(ProfilerCategory.Render, "Vertices Count");

        List<double> renderMilliseconds = new(SampleFrames);
        long drawCallsTotal = 0;
        long batchesTotal = 0;
        long setPassTotal = 0;
        long trianglesTotal = 0;
        long verticesTotal = 0;
        int profilerSamples = 0;

        try
        {
            GameObject cameraObject = new(label + "_BenchmarkCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            GameObject lightObject = new(label + "_BenchmarkLight");
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            target = new RenderTexture(RenderWidth, RenderHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = label + "_BenchmarkRenderTexture",
                antiAliasing = 1
            };
            target.Create();
            camera.targetTexture = target;

            Stopwatch stopwatch = new();
            for (int frame = 0; frame < WarmupFrames + SampleFrames; frame++)
            {
                ConfigureCameraForFrame(camera, bounds, frame);
                stopwatch.Restart();
                camera.Render();
                stopwatch.Stop();

                if (frame < WarmupFrames)
                    continue;

                renderMilliseconds.Add(stopwatch.Elapsed.TotalMilliseconds);
                drawCallsTotal += ReadRecorder(drawCalls);
                batchesTotal += ReadRecorder(batches);
                setPassTotal += ReadRecorder(setPass);
                trianglesTotal += ReadRecorder(triangles);
                verticesTotal += ReadRecorder(vertices);
                profilerSamples++;
            }
        }
        finally
        {
            DisposeRecorder(ref drawCalls);
            DisposeRecorder(ref batches);
            DisposeRecorder(ref setPass);
            DisposeRecorder(ref triangles);
            DisposeRecorder(ref vertices);
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }

            if (camera != null)
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            if (light != null)
                UnityEngine.Object.DestroyImmediate(light.gameObject);
        }

        double averageMs = Average(renderMilliseconds);
        double p95Ms = Percentile(renderMilliseconds, 0.95d);
        double estimatedFps = averageMs > 0d ? 1000d / averageMs : 0d;
        return new RenderRouteStats(
            WarmupFrames,
            SampleFrames,
            RenderWidth,
            RenderHeight,
            averageMs,
            p95Ms,
            estimatedFps,
            AverageCounter(drawCallsTotal, profilerSamples),
            AverageCounter(batchesTotal, profilerSamples),
            AverageCounter(setPassTotal, profilerSamples),
            AverageCounter(trianglesTotal, profilerSamples),
            AverageCounter(verticesTotal, profilerSamples));
    }

    private static void ConfigureCameraForFrame(Camera camera, Bounds bounds, int frame)
    {
        bool buildHeight = frame % 4 == 3;
        float height = buildHeight ? BuildCameraHeight : NormalCameraHeight;
        float pitch = buildHeight ? BuildCameraPitch : NormalCameraPitch;
        float fov = buildHeight ? BuildCameraFieldOfView : NormalCameraFieldOfView;
        float orbit = (frame * 23f) * Mathf.Deg2Rad;
        float routeRadius = buildHeight ? 150f : 80f;
        Vector3 center = bounds.center;
        Vector3 lookAt = new(
            center.x + Mathf.Cos(orbit * 0.5f) * 90f,
            center.y,
            center.z + Mathf.Sin(orbit * 0.5f) * 90f);
        Vector3 offset = new(Mathf.Cos(orbit) * routeRadius, height, Mathf.Sin(orbit) * routeRadius);
        camera.fieldOfView = fov;
        camera.aspect = RenderWidth / (float)RenderHeight;
        camera.transform.position = lookAt + offset;
        camera.transform.rotation = Quaternion.Euler(pitch, Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg + 180f, 0f);
    }

    private static StaticStats MeasureStaticStats(List<MeshRenderer> renderers, Transform root)
    {
        StaticStats stats = new();
        HashSet<Mesh> uniqueMeshes = new();
        HashSet<Material> uniqueMaterials = new();
        HashSet<string> meshAssetPaths = new(StringComparer.Ordinal);
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
                continue;

            stats.Renderers++;
            stats.MeshFilters++;
            stats.MaterialSlots += renderer.sharedMaterials.Length;
            for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
            {
                Material material = renderer.sharedMaterials[materialIndex];
                if (material != null)
                    uniqueMaterials.Add(material);
            }

            if (uniqueMeshes.Add(mesh))
            {
                stats.UniqueMeshes++;
                string meshPath = AssetDatabase.GetAssetPath(mesh);
                if (!string.IsNullOrEmpty(meshPath) && meshAssetPaths.Add(meshPath))
                    stats.MeshAssetDiskBytes += AssetFileSize(meshPath);
            }

            int triangles = CountTriangles(mesh);
            stats.Vertices += mesh.vertexCount;
            stats.Triangles += triangles;
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, mesh.vertexCount);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, triangles);
        }

        stats.UniqueMaterials = uniqueMaterials.Count;
        stats.Colliders = root.GetComponentsInChildren<Collider>(true).Length;
        stats.LodSwitchers = root.GetComponentsInChildren<WarlineCaptureTerrainLodHeightSwitch>(true).Length;
        return stats;
    }

    private static InstancingStats MeasureInstancing(List<MeshRenderer> renderers)
    {
        Dictionary<string, int> repeatedGroups = new(StringComparer.Ordinal);
        InstancingStats stats = new();
        stats.TotalRenderers = renderers.Count;
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            bool instancingEnabled = materials.Length > 0;
            StringBuilder key = new();
            key.Append(AssetDatabase.GetAssetPath(mesh));
            key.Append('|');
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.enableInstancing)
                    instancingEnabled = false;
                key.Append(material != null ? AssetDatabase.GetAssetPath(material) : "null");
                key.Append(';');
            }

            if (!instancingEnabled)
                continue;

            string groupKey = key.ToString();
            repeatedGroups.TryGetValue(groupKey, out int count);
            repeatedGroups[groupKey] = count + 1;
        }

        foreach (KeyValuePair<string, int> entry in repeatedGroups)
        {
            if (entry.Value <= 1)
                continue;
            stats.RepeatedGroups++;
            stats.EligibleRenderers += entry.Value;
            stats.LargestGroupSize = Mathf.Max(stats.LargestGroupSize, entry.Value);
        }

        return stats;
    }

    private static List<MeshRenderer> CollectRenderableMeshRenderers(Transform root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        List<MeshRenderer> result = new(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (renderer == null || !renderer.enabled || mesh == null)
                continue;
            if (ShouldExclude(renderer.transform))
                continue;
            result.Add(renderer);
        }

        return result;
    }

    private static bool ShouldExclude(Transform transform)
    {
        for (Transform cursor = transform; cursor != null; cursor = cursor.parent)
        {
            string name = cursor.name;
            if (name.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("MapTarget", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Reserve", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Probe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static Bounds CalculateBounds(List<MeshRenderer> renderers)
    {
        if (renderers.Count == 0)
            return new Bounds(Vector3.zero, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static int CountTriangles(Mesh mesh)
    {
        int total = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            total += (int)mesh.GetIndexCount(i) / 3;
        return total;
    }

    private static ProfilerRecorder StartRecorder(ProfilerCategory category, string statName)
    {
        try
        {
            return ProfilerRecorder.StartNew(category, statName);
        }
        catch (Exception)
        {
            return default;
        }
    }

    private static long ReadRecorder(ProfilerRecorder recorder)
    {
        return recorder.Valid ? recorder.LastValue : 0L;
    }

    private static void DisposeRecorder(ref ProfilerRecorder recorder)
    {
        if (recorder.Valid)
            recorder.Dispose();
        recorder = default;
    }

    private static double Average(List<double> values)
    {
        if (values.Count == 0)
            return 0d;
        double total = 0d;
        for (int i = 0; i < values.Count; i++)
            total += values[i];
        return total / values.Count;
    }

    private static long AverageCounter(long total, int samples)
    {
        return samples > 0 ? total / samples : 0L;
    }

    private static double Percentile(List<double> values, double percentile)
    {
        if (values.Count == 0)
            return 0d;
        List<double> copy = new(values);
        copy.Sort();
        int index = Mathf.Clamp(Mathf.CeilToInt((float)(percentile * copy.Count)) - 1, 0, copy.Count - 1);
        return copy[index];
    }

    private static GameObject FindRootWithMostRenderers(Scene scene)
    {
        GameObject best = null;
        int bestCount = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            int count = roots[i].GetComponentsInChildren<MeshRenderer>(true).Length;
            if (count <= bestCount)
                continue;
            best = roots[i];
            bestCount = count;
        }

        return best;
    }

    private static long AssetFileSize(string assetPath)
    {
        string fullPath = ProjectPath(assetPath);
        return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L;
    }

    private static void WriteSummaryJson(SceneComparison terrain7, SceneComparison terrain8)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"pipelineId\": \"Terrain7VsTerrain8PerformanceComparison\",");
        json.AppendLine("  \"date\": \"2026-05-28\",");
        json.AppendLine("  \"environmentNote\": \"Editor offscreen Camera.Render benchmark. Use as same-machine comparison only; final FPS winner requires device profiling.\",");
        json.AppendLine("  \"warmupFrames\": " + WarmupFrames.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"sampleFrames\": " + SampleFrames.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"renderResolution\": \"" + RenderWidth.ToString(CultureInfo.InvariantCulture) + "x" + RenderHeight.ToString(CultureInfo.InvariantCulture) + "\",");
        json.AppendLine("  \"terrain7\": " + terrain7.ToJson(2) + ",");
        json.AppendLine("  \"terrain8\": " + terrain8.ToJson(2) + ",");
        json.AppendLine("  \"interpretation\": {");
        json.AppendLine("    \"lowerRenderMillisecondsWinsEditorRoute\": \"" + FasterLabel(terrain7, terrain8) + "\",");
        json.AppendLine("    \"lowerGeneratedMeshMemoryWins\": \"" + LowerMeshMemoryLabel(terrain7, terrain8) + "\",");
        json.AppendLine("    \"instancingPreparedScene\": \"" + (terrain8.InstancingStats.EligibleRenderers > terrain7.InstancingStats.EligibleRenderers ? terrain8.Label : terrain7.Label) + "\"");
        json.AppendLine("  }");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SummaryJsonPath), json.ToString());
    }

    private static void WriteReport(SceneComparison terrain7, SceneComparison terrain8)
    {
        StringBuilder report = new();
        report.AppendLine("# Terrain7 vs Terrain8 Performance Comparison");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-28");
        report.AppendLine();
        report.AppendLine("Purpose: compare the full chunk-combined `Game_Terrain7` path against the hybrid ground-chunk plus GPU-instancing-prepared `Game_Terrain8` path.");
        report.AppendLine();
        report.AppendLine("Method:");
        report.AppendLine("- Open each scene in the same editor process.");
        report.AppendLine("- Disable fog.");
        report.AppendLine("- Render the same offscreen camera route at `" + RenderWidth.ToString(CultureInfo.InvariantCulture) + "x" + RenderHeight.ToString(CultureInfo.InvariantCulture) + "`.");
        report.AppendLine("- Use `" + WarmupFrames.ToString(CultureInfo.InvariantCulture) + "` warmup frames and `" + SampleFrames.ToString(CultureInfo.InvariantCulture) + "` measured frames.");
        report.AppendLine("- Record static scene metrics, instancing eligibility, and editor offscreen render-call wall time.");
        report.AppendLine();
        report.AppendLine("Results:");
        AppendSceneResult(report, terrain7);
        AppendSceneResult(report, terrain8);
        report.AppendLine();
        report.AppendLine("Interpretation:");
        report.AppendLine("- Faster editor offscreen route: `" + FasterLabel(terrain7, terrain8) + "`.");
        report.AppendLine("- Lower generated mesh asset memory: `" + LowerMeshMemoryLabel(terrain7, terrain8) + "`.");
        report.AppendLine("- Better GPU-instancing setup: `" + (terrain8.InstancingStats.EligibleRenderers > terrain7.InstancingStats.EligibleRenderers ? terrain8.Label : terrain7.Label) + "`.");
        report.AppendLine();
        report.AppendLine("Caveat:");
        report.AppendLine("- This is not a final mobile FPS verdict. Unity GPU instancing should be confirmed in Frame Debugger or Profiler on the target device/player because editor offscreen rendering, batchmode, and mobile GPU drivers can produce different bottlenecks.");
        report.AppendLine();
        report.AppendLine("Output JSON:");
        report.AppendLine("- `" + SummaryJsonPath + "`");
        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static void AppendSceneResult(StringBuilder report, SceneComparison scene)
    {
        report.AppendLine();
        report.AppendLine("`" + scene.Label + "`:");
        report.AppendLine("- Renderers/material slots/triangles: `" + scene.StaticStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.StaticStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.StaticStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Unique meshes/materials: `" + scene.StaticStats.UniqueMeshes.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.StaticStats.UniqueMaterials.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Mesh asset disk bytes: `" + scene.StaticStats.MeshAssetDiskBytes.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Colliders / LOD switchers: `" + scene.StaticStats.Colliders.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.StaticStats.LodSwitchers.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Instancing-eligible renderers / repeated groups / largest group: `" + scene.InstancingStats.EligibleRenderers.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.InstancingStats.RepeatedGroups.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.InstancingStats.LargestGroupSize.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Average render ms / p95 render ms / estimated FPS: `" + scene.RenderStats.AverageRenderMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + "` / `" + scene.RenderStats.P95RenderMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + "` / `" + scene.RenderStats.EstimatedFps.ToString("F1", CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Profiler counters, avg draw/batch/setpass/tris/verts: `" + scene.RenderStats.AverageDrawCalls.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.RenderStats.AverageBatches.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.RenderStats.AverageSetPassCalls.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.RenderStats.AverageTriangles.ToString(CultureInfo.InvariantCulture) + "` / `" + scene.RenderStats.AverageVertices.ToString(CultureInfo.InvariantCulture) + "`");
    }

    private static string FasterLabel(SceneComparison a, SceneComparison b)
    {
        if (a.RenderStats.AverageRenderMilliseconds <= 0d && b.RenderStats.AverageRenderMilliseconds <= 0d)
            return "unknown";
        if (a.RenderStats.AverageRenderMilliseconds <= 0d)
            return b.Label;
        if (b.RenderStats.AverageRenderMilliseconds <= 0d)
            return a.Label;
        return a.RenderStats.AverageRenderMilliseconds <= b.RenderStats.AverageRenderMilliseconds ? a.Label : b.Label;
    }

    private static string LowerMeshMemoryLabel(SceneComparison a, SceneComparison b)
    {
        return a.StaticStats.MeshAssetDiskBytes <= b.StaticStats.MeshAssetDiskBytes ? a.Label : b.Label;
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    }

    private readonly struct SceneComparison
    {
        public readonly string Label;
        public readonly string ScenePath;
        public readonly StaticStats StaticStats;
        public readonly InstancingStats InstancingStats;
        public readonly RenderRouteStats RenderStats;

        public SceneComparison(string label, string scenePath, StaticStats staticStats, InstancingStats instancingStats, RenderRouteStats renderStats)
        {
            Label = label;
            ScenePath = scenePath;
            StaticStats = staticStats;
            InstancingStats = instancingStats;
            RenderStats = renderStats;
        }

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"scenePath\": \"" + ScenePath + "\",");
            json.AppendLine(inner + "\"staticStats\": " + StaticStats.ToJson(indent + 2) + ",");
            json.AppendLine(inner + "\"instancingStats\": " + InstancingStats.ToJson(indent + 2) + ",");
            json.AppendLine(inner + "\"renderStats\": " + RenderStats.ToJson(indent + 2));
            json.Append(pad + "}");
            return json.ToString();
        }
    }

    private struct StaticStats
    {
        public int Renderers;
        public int MeshFilters;
        public int MaterialSlots;
        public int UniqueMeshes;
        public int UniqueMaterials;
        public int Vertices;
        public int Triangles;
        public int MaxVerticesPerRenderer;
        public int MaxTrianglesPerRenderer;
        public int Colliders;
        public int LodSwitchers;
        public long MeshAssetDiskBytes;

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"renderers\": " + Renderers.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"meshFilters\": " + MeshFilters.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"materialSlots\": " + MaterialSlots.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"uniqueMeshes\": " + UniqueMeshes.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"uniqueMaterials\": " + UniqueMaterials.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"vertices\": " + Vertices.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"triangles\": " + Triangles.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"maxVerticesPerRenderer\": " + MaxVerticesPerRenderer.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"maxTrianglesPerRenderer\": " + MaxTrianglesPerRenderer.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"colliders\": " + Colliders.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"lodSwitchers\": " + LodSwitchers.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"meshAssetDiskBytes\": " + MeshAssetDiskBytes.ToString(CultureInfo.InvariantCulture));
            json.Append(pad + "}");
            return json.ToString();
        }
    }

    private struct InstancingStats
    {
        public int TotalRenderers;
        public int EligibleRenderers;
        public int RepeatedGroups;
        public int LargestGroupSize;

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"totalRenderers\": " + TotalRenderers.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"eligibleRenderers\": " + EligibleRenderers.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"repeatedGroups\": " + RepeatedGroups.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"largestGroupSize\": " + LargestGroupSize.ToString(CultureInfo.InvariantCulture));
            json.Append(pad + "}");
            return json.ToString();
        }
    }

    private readonly struct RenderRouteStats
    {
        public readonly int WarmupFrames;
        public readonly int SampleFrames;
        public readonly int Width;
        public readonly int Height;
        public readonly double AverageRenderMilliseconds;
        public readonly double P95RenderMilliseconds;
        public readonly double EstimatedFps;
        public readonly long AverageDrawCalls;
        public readonly long AverageBatches;
        public readonly long AverageSetPassCalls;
        public readonly long AverageTriangles;
        public readonly long AverageVertices;

        public RenderRouteStats(int warmupFrames, int sampleFrames, int width, int height, double averageRenderMilliseconds, double p95RenderMilliseconds, double estimatedFps, long averageDrawCalls, long averageBatches, long averageSetPassCalls, long averageTriangles, long averageVertices)
        {
            WarmupFrames = warmupFrames;
            SampleFrames = sampleFrames;
            Width = width;
            Height = height;
            AverageRenderMilliseconds = averageRenderMilliseconds;
            P95RenderMilliseconds = p95RenderMilliseconds;
            EstimatedFps = estimatedFps;
            AverageDrawCalls = averageDrawCalls;
            AverageBatches = averageBatches;
            AverageSetPassCalls = averageSetPassCalls;
            AverageTriangles = averageTriangles;
            AverageVertices = averageVertices;
        }

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"warmupFrames\": " + WarmupFrames.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"sampleFrames\": " + SampleFrames.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"width\": " + Width.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"height\": " + Height.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"averageRenderMilliseconds\": " + AverageRenderMilliseconds.ToString("F4", CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"p95RenderMilliseconds\": " + P95RenderMilliseconds.ToString("F4", CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"estimatedFps\": " + EstimatedFps.ToString("F2", CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"averageDrawCalls\": " + AverageDrawCalls.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"averageBatches\": " + AverageBatches.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"averageSetPassCalls\": " + AverageSetPassCalls.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"averageTriangles\": " + AverageTriangles.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"averageVertices\": " + AverageVertices.ToString(CultureInfo.InvariantCulture));
            json.Append(pad + "}");
            return json.ToString();
        }
    }
}
#endif
