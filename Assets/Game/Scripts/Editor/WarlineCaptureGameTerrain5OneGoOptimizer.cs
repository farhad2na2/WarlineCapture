#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class WarlineCaptureGameTerrain5OneGoOptimizer
{
    private const string SourceScenePath = "Assets/Game/Scenes/Game_Terrain4.unity";
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain5.unity";
    private const string MeshAssetRoot = "Assets/Game/GeneratedTerrainOptimized/Game_Terrain5";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain5_Optimization";
    private const string SummaryJsonPath = DataRoot + "/game_terrain5_optimization_summary.json";
    private const string ReportPath = "Design/AgentReports/2026-05-26_gameplay_game-terrain5_one_go_optimizer.md";
    private const float ChunkSize = 256f;
    private const float NormalCameraHeight = 34f;
    private const float NormalCameraPitch = 40f;
    private const float NormalCameraFieldOfView = 36f;
    private const float BuildCameraHeight = 90f;
    private const float BuildCameraPitch = 64f;
    private const float BuildCameraFieldOfView = 52f;
    private const float WideAspect = 21f / 9f;

    [MenuItem("WarlineCapture/Design/Game Terrain5/Build Optimized Shipping Terrain")]
    public static void BuildOptimizedShippingTerrain()
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        EnsureAssetFolder(MeshAssetRoot);
        ClearAssetFolder(MeshAssetRoot);

        if (File.Exists(ProjectPath(TargetScenePath)))
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            if (!File.Exists(ProjectPath(TargetScenePath)))
                throw new FileNotFoundException("Could not copy source terrain scene.", SourceScenePath);
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        GameObject island = GameObject.Find("Island") ?? FindRootWithMostRenderers(scene);
        if (island == null)
            throw new InvalidOperationException("Game_Terrain5 optimizer could not find an Island root or renderable scene root.");

        Dictionary<string, bool> sourceImporterReadability = PrepareSourceMeshesForCombination(island.transform);
        List<MeshRenderer> sourceRenderers = null;
        OptimizationStats before = default;
        List<ChunkBucket> buckets = null;
        OptimizationStats after = default;

        try
        {
            sourceRenderers = CollectOptimizableRenderers(island.transform);
            before = Measure(sourceRenderers);
            buckets = BuildChunkBuckets(island.transform, sourceRenderers);

            GameObject optimizedRoot = new("OptimizedVisualTerrain_256mChunks");
            optimizedRoot.transform.SetParent(island.transform, false);
            optimizedRoot.isStatic = true;

            after = BuildCombinedChunkMeshes(optimizedRoot.transform, buckets);
            DestroySourceChildrenExcept(island.transform, optimizedRoot.transform);
        }
        finally
        {
            RestoreSourceMeshReadability(sourceImporterReadability);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        WriteSummaryJson(before, after, buckets.Count);
        WriteReport(before, after, buckets.Count);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("WARLINECAPTURE_GAME_TERRAIN5_OPTIMIZED_READY"
            + " scene=" + TargetScenePath
            + " chunkSize=" + ChunkSize.ToString(CultureInfo.InvariantCulture)
            + " sourceRenderers=" + before.Renderers.ToString(CultureInfo.InvariantCulture)
            + " optimizedRenderers=" + after.Renderers.ToString(CultureInfo.InvariantCulture)
            + " sourceVertices=" + before.Vertices.ToString(CultureInfo.InvariantCulture)
            + " optimizedVertices=" + after.Vertices.ToString(CultureInfo.InvariantCulture)
            + " report=" + ReportPath);
    }

    public static void FullRegenerateAndOptimize()
    {
        WarlineCaptureGameTerrain4FullRegenerationPipeline.FullRegenerate();
        BuildOptimizedShippingTerrain();
        WarlineCaptureGameTerrain7NonLodMobileOptimizer.BuildNonLodMobileTerrain();
    }

    private static List<MeshRenderer> CollectOptimizableRenderers(Transform root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        List<MeshRenderer> result = new(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || renderer.gameObject.name.StartsWith("OptimizedVisualTerrain_", StringComparison.Ordinal))
                continue;
            if (ShouldExclude(renderer.transform))
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable)
            {
                if (mesh != null)
                    Debug.LogWarning($"Game_Terrain5 optimizer skipped still-unreadable mesh '{mesh.name}' on '{renderer.name}'.", renderer);
                continue;
            }

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

    private static List<ChunkBucket> BuildChunkBuckets(Transform root, List<MeshRenderer> renderers)
    {
        Dictionary<ChunkMaterialKey, ChunkBucket> bucketsByKey = new();
        Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;

        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter.sharedMesh;
            Material[] materials = renderer.sharedMaterials;
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Length);
            Vector3 center = renderer.bounds.center;
            Vector2Int chunk = new(
                Mathf.FloorToInt(center.x / ChunkSize),
                Mathf.FloorToInt(center.z / ChunkSize));
            Matrix4x4 localToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;

            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                Material material = materials[subMeshIndex];
                if (material == null)
                    continue;

                ChunkMaterialKey key = new(chunk, material);
                if (!bucketsByKey.TryGetValue(key, out ChunkBucket bucket))
                {
                    bucket = new ChunkBucket(chunk, material);
                    bucketsByKey.Add(key, bucket);
                }

                bucket.CombineInstances.Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = localToRoot
                });
            }
        }

        List<ChunkBucket> buckets = new(bucketsByKey.Values);
        buckets.Sort((a, b) =>
        {
            int z = a.Chunk.y.CompareTo(b.Chunk.y);
            if (z != 0)
                return z;
            int x = a.Chunk.x.CompareTo(b.Chunk.x);
            return x != 0 ? x : string.CompareOrdinal(a.Material.name, b.Material.name);
        });
        return buckets;
    }

    private static OptimizationStats BuildCombinedChunkMeshes(Transform optimizedRoot, List<ChunkBucket> buckets)
    {
        OptimizationStats stats = new();
        HashSet<Material> uniqueMaterials = new();
        for (int i = 0; i < buckets.Count; i++)
        {
            ChunkBucket bucket = buckets[i];
            if (bucket.CombineInstances.Count == 0)
                continue;

            Mesh mesh = new()
            {
                name = $"GT5_Chunk_{bucket.Chunk.x}_{bucket.Chunk.y}_{Sanitize(bucket.Material.name)}",
                indexFormat = IndexFormat.UInt32
            };
            mesh.CombineMeshes(bucket.CombineInstances.ToArray(), true, true, false);
            mesh.RecalculateBounds();
            MeshUtility.Optimize(mesh);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{MeshAssetRoot}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(mesh, assetPath);

            GameObject chunkObject = new(mesh.name);
            chunkObject.transform.SetParent(optimizedRoot, false);
            chunkObject.isStatic = true;

            MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = bucket.Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;

            stats.Renderers++;
            stats.MeshFilters++;
            stats.MaterialSlots++;
            uniqueMaterials.Add(bucket.Material);
            int vertices = mesh.vertexCount;
            int triangles = CountTriangles(mesh);
            stats.Vertices += vertices;
            stats.Triangles += triangles;
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, vertices);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, triangles);
        }

        stats.UniqueMeshes = stats.Renderers;
        stats.UniqueMaterials = uniqueMaterials.Count;
        return stats;
    }

    private static OptimizationStats Measure(List<MeshRenderer> renderers)
    {
        OptimizationStats stats = new();
        HashSet<Mesh> countedMeshes = new();
        HashSet<Material> uniqueMaterials = new();
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
                if (renderer.sharedMaterials[materialIndex] != null)
                    uniqueMaterials.Add(renderer.sharedMaterials[materialIndex]);
            }

            int vertices = mesh.vertexCount;
            int triangles = CountTriangles(mesh);
            stats.Vertices += vertices;
            stats.Triangles += triangles;
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, vertices);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, triangles);
            if (countedMeshes.Add(mesh))
                stats.UniqueMeshes++;
        }

        stats.UniqueMaterials = uniqueMaterials.Count;
        return stats;
    }

    private static int CountTriangles(Mesh mesh)
    {
        int triangles = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            triangles += (int)mesh.GetIndexCount(i) / 3;
        return triangles;
    }

    private static void DestroySourceChildrenExcept(Transform root, Transform keep)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == keep)
                continue;

            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static Dictionary<string, bool> PrepareSourceMeshesForCombination(Transform root)
    {
        Dictionary<string, bool> importerReadability = new(StringComparer.Ordinal);
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || ShouldExclude(renderer.transform))
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || mesh.isReadable)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(assetPath) || importerReadability.ContainsKey(assetPath))
                continue;

            if (AssetImporter.GetAtPath(assetPath) is not ModelImporter importer)
                continue;

            importerReadability.Add(assetPath, importer.isReadable);
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        return importerReadability;
    }

    private static void RestoreSourceMeshReadability(Dictionary<string, bool> importerReadability)
    {
        foreach (KeyValuePair<string, bool> entry in importerReadability)
        {
            if (AssetImporter.GetAtPath(entry.Key) is not ModelImporter importer)
                continue;
            if (importer.isReadable == entry.Value)
                continue;

            importer.isReadable = entry.Value;
            importer.SaveAndReimport();
        }
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

    private static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void ClearAssetFolder(string assetFolder)
    {
        string[] assets = AssetDatabase.FindAssets("", new[] { assetFolder });
        for (int i = 0; i < assets.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(assets[i]);
            if (!string.IsNullOrEmpty(path) && path.StartsWith(assetFolder + "/", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(path);
        }
    }

    private static void WriteSummaryJson(OptimizationStats before, OptimizationStats after, int buckets)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"pipelineId\": \"GameTerrain5_OneGoVisualOptimization\",");
        json.AppendLine("  \"date\": \"2026-05-26\",");
        json.AppendLine("  \"sourceScene\": \"" + SourceScenePath + "\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"chunkSizeWorldUnits\": " + ChunkSize.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"chunkReason\": \"Normal 21:9 camera footprint at height 34 is about 129x78 world units; 256m chunks keep normal gameplay in one to two chunks, build mode in about two chunks, and reduce renderer count for mobile.\",");
        json.AppendLine("  \"cameraBasis\": {");
        json.AppendLine("    \"normalHeight\": " + NormalCameraHeight.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"normalPitch\": " + NormalCameraPitch.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"normalFieldOfView\": " + NormalCameraFieldOfView.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"buildHeight\": " + BuildCameraHeight.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"buildPitch\": " + BuildCameraPitch.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"buildFieldOfView\": " + BuildCameraFieldOfView.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"wideAspect\": " + WideAspect.ToString(CultureInfo.InvariantCulture));
        json.AppendLine("  },");
        json.AppendLine("  \"source\": " + before.ToJson(2) + ",");
        json.AppendLine("  \"optimized\": " + after.ToJson(2) + ",");
        json.AppendLine("  \"combinedChunkMaterialBuckets\": " + buckets.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"shippingRules\": [");
        json.AppendLine("    \"Visual terrain has no gameplay authority; gameplay uses grid, blocker and heightmap data.\",");
        json.AppendLine("    \"No visual terrain colliders are kept in Game_Terrain5.\",");
        json.AppendLine("    \"Chunk meshes are combined by material and have shadows, probes and motion vectors disabled.\"");
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SummaryJsonPath), json.ToString());
    }

    private static void WriteReport(OptimizationStats before, OptimizationStats after, int buckets)
    {
        StringBuilder report = new();
        report.AppendLine("# Game_Terrain5 One-Go Visual Optimizer");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-26");
        report.AppendLine();
        report.AppendLine("Purpose: `Game_Terrain5` is the shipping visual-terrain scene. It is copied from `Game_Terrain4`, then optimized as decorative render-only terrain. Gameplay remains driven by grid, blocker, and heightmap data, not these meshes.");
        report.AppendLine();
        report.AppendLine("Chunk-size decision:");
        report.AppendLine("- Runtime config normal camera: height `34`, pitch `40`, FOV `36`.");
        report.AppendLine("- At 21:9, that sees roughly `129 x 78` world units.");
        report.AppendLine("- Build-mode camera: height `90`, pitch `64`, FOV `52`, about `295 x 155` world units at 21:9.");
        report.AppendLine("- Chosen chunk size: `256` world units. This avoids 128m over-fragmentation while still letting Unity cull large off-camera parts of the decorative island.");
        report.AppendLine();
        report.AppendLine("Optimization rules:");
        report.AppendLine("- Combine visual meshes by `256m chunk + material`.");
        report.AppendLine("- Delete the source prefab-instance visual hierarchy from `Game_Terrain5` after bake.");
        report.AppendLine("- Strip visual terrain colliders by not carrying source objects forward.");
        report.AppendLine("- Disable dynamic shadow casting, shadow receiving, reflection probes, light probes, and motion vectors on combined chunk renderers.");
        report.AppendLine("- Store generated chunk meshes in `" + MeshAssetRoot + "`.");
        report.AppendLine();
        report.AppendLine("Results:");
        report.AppendLine("- Source renderers: `" + before.Renderers.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Source material slots: `" + before.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Source unique meshes: `" + before.UniqueMeshes.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Optimized renderers: `" + after.Renderers.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Optimized material slots: `" + after.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Combined chunk/material buckets: `" + buckets.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Optimized mesh vertices: `" + after.Vertices.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Optimized mesh triangles: `" + after.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Max vertices in one optimized chunk renderer: `" + after.MaxVerticesPerRenderer.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Max triangles in one optimized chunk renderer: `" + after.MaxTrianglesPerRenderer.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine();
        report.AppendLine("Run commands:");
        report.AppendLine("- Optimize existing `Game_Terrain4`: `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.BuildOptimizedShippingTerrain`");
        report.AppendLine("- Full one-go workflow: `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain5OneGoOptimizer.FullRegenerateAndOptimize`");
        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static string Sanitize(string value)
    {
        StringBuilder builder = new();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        return builder.ToString();
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    }

    private readonly struct ChunkMaterialKey : IEquatable<ChunkMaterialKey>
    {
        private readonly Vector2Int _chunk;
        private readonly Material _material;

        public ChunkMaterialKey(Vector2Int chunk, Material material)
        {
            _chunk = chunk;
            _material = material;
        }

        public bool Equals(ChunkMaterialKey other) => _chunk == other._chunk && _material == other._material;
        public override bool Equals(object obj) => obj is ChunkMaterialKey other && Equals(other);
        public override int GetHashCode() => (_chunk.GetHashCode() * 397) ^ (_material != null ? _material.GetHashCode() : 0);
    }

    private sealed class ChunkBucket
    {
        public readonly Vector2Int Chunk;
        public readonly Material Material;
        public readonly List<CombineInstance> CombineInstances = new();

        public ChunkBucket(Vector2Int chunk, Material material)
        {
            Chunk = chunk;
            Material = material;
        }
    }

    private struct OptimizationStats
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
            json.AppendLine(inner + "\"maxTrianglesPerRenderer\": " + MaxTrianglesPerRenderer.ToString(CultureInfo.InvariantCulture));
            json.Append(pad + "}");
            return json.ToString();
        }
    }
}
#endif
