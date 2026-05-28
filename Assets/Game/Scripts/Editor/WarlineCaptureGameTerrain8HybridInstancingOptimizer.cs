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

public static class WarlineCaptureGameTerrain8HybridInstancingOptimizer
{
    private const string SourceScenePath = "Assets/Game/Scenes/Game_Terrain4.unity";
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain8.unity";
    private const string SourceFoundationName = "ExpandedIsland_SourceGameTerrain3PrefabsOnly";
    private const string GroundChunkRootName = "GameTerrain8GroundChunks_256m";
    private const string MeshAssetRoot = "Assets/Game/GeneratedTerrainOptimized/Game_Terrain8";
    private const string GroundChunkAssetRoot = MeshAssetRoot + "/GroundChunks";
    private const string InstancedMaterialRoot = MeshAssetRoot + "/InstancedMaterials";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain8_HybridInstancing";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain8_HybridInstancing";
    private const string SourceTopDownCapture = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_topdown_proof.png";
    private const string SourceGameplayCapture = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_playable_angle_proof.png";
    private const string SummaryJsonPath = DataRoot + "/game_terrain8_hybrid_instancing_summary.json";
    private const string ReportPath = "Design/AgentReports/2026-05-27_gameplay_game-terrain8_hybrid_instancing_optimizer.md";
    private const string TaskPath = "Design/AgentTasks/game_terrain8_hybrid_instancing_optimization_steps.md";
    private const float ChunkSize = 256f;
    private const float NormalCameraHeight = 34f;
    private const float NormalCameraPitch = 40f;
    private const float NormalCameraFieldOfView = 36f;
    private const float BuildCameraHeight = 90f;
    private const float BuildCameraPitch = 64f;
    private const float BuildCameraFieldOfView = 52f;
    private const float WideAspect = 21f / 9f;

    private static readonly string[] DressingGroupNames =
    {
        "Generated_Mountains",
        "Generated_Mountains_Dirt",
        "Generated_Trees_Playable",
        "Generated_Trees_Dirt",
        "Generated_Trees_BlockerBelt",
        "Generated_Bushes_Playable",
        "Generated_Bushes_BlockerBelt",
        "Generated_Rocks"
    };

    [MenuItem("WarlineCapture/Design/Game Terrain8/Build Hybrid Instanced Terrain")]
    public static void BuildHybridInstancedTerrain()
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        EnsureAssetFolder(MeshAssetRoot);
        ClearAssetFolder(MeshAssetRoot);
        EnsureAssetFolder(GroundChunkAssetRoot);
        EnsureAssetFolder(InstancedMaterialRoot);

        if (File.Exists(ProjectPath(TargetScenePath)))
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            if (!File.Exists(ProjectPath(TargetScenePath)))
                throw new FileNotFoundException("Could not copy Game_Terrain4 scene for Game_Terrain8.", SourceScenePath);
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        ConfigureRenderSettingsForOptimizedTerrain();

        GameObject island = GameObject.Find("Island") ?? FindRootWithMostRenderers(scene);
        if (island == null)
            throw new InvalidOperationException("Game_Terrain8 optimizer could not find an Island root or renderable scene root.");

        Transform foundation = island.transform.Find(SourceFoundationName);
        if (foundation == null)
            throw new InvalidOperationException("Game_Terrain8 optimizer could not find the source foundation child: " + SourceFoundationName);

        List<MeshRenderer> sourceIslandRenderers = CollectRenderableMeshRenderers(island.transform);
        List<MeshRenderer> sourceFoundationRenderers = CollectRenderableMeshRenderers(foundation);
        List<MeshRenderer> sourceDressingRenderers = CollectDressingRenderers(island.transform, foundation);
        OptimizationStats sourceIslandStats = Measure(sourceIslandRenderers);
        OptimizationStats sourceFoundationStats = Measure(sourceFoundationRenderers);
        OptimizationStats sourceDressingStats = Measure(sourceDressingRenderers);

        Dictionary<string, bool> sourceImporterReadability = PrepareSourceMeshesForCombination(foundation);
        List<ChunkBucket> groundBuckets = null;
        OptimizationStats groundStats = default;
        long groundMeshBytes = 0;

        try
        {
            groundBuckets = BuildChunkBuckets(island.transform, sourceFoundationRenderers);
            GameObject groundRootObject = new(GroundChunkRootName);
            groundRootObject.transform.SetParent(island.transform, false);
            groundRootObject.isStatic = false;
            groundStats = BuildCombinedGroundChunks(groundRootObject.transform, groundBuckets, ref groundMeshBytes);
            UnityEngine.Object.DestroyImmediate(foundation.gameObject);
        }
        finally
        {
            RestoreSourceMeshReadability(sourceImporterReadability);
        }

        int removedNonessential = RemoveNonessentialSceneObjects(scene);
        int removedColliders = RemoveColliders(island.transform);
        int removedSwitchers = RemoveComponents<WarlineCaptureTerrainLodHeightSwitch>(island.transform);
        ClearStaticFlags(island.transform);
        DisableRendererCosts(island.transform.GetComponentsInChildren<MeshRenderer>(true));

        List<MeshRenderer> finalDressingRenderers = CollectDressingRenderers(island.transform, island.transform.Find(GroundChunkRootName));
        MaterialCopyStats materialCopyStats = ApplyInstancedMaterialCopies(finalDressingRenderers);
        InstancingStats instancingStats = MeasureInstancing(finalDressingRenderers);
        List<MeshRenderer> finalIslandRenderers = CollectRenderableMeshRenderers(island.transform);
        OptimizationStats finalIslandStats = Measure(finalIslandRenderers);
        List<string> captures = CopyAcceptedProofCaptures();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        WriteSummaryJson(sourceIslandStats, sourceFoundationStats, sourceDressingStats, groundStats, finalIslandStats, materialCopyStats, instancingStats, groundBuckets.Count, groundMeshBytes, removedColliders, removedSwitchers, removedNonessential, captures);
        WriteReport(sourceIslandStats, sourceFoundationStats, sourceDressingStats, groundStats, finalIslandStats, materialCopyStats, instancingStats, groundBuckets.Count, groundMeshBytes, removedColliders, removedSwitchers, removedNonessential, captures);
        MarkTaskComplete();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("WARLINECAPTURE_GAME_TERRAIN8_HYBRID_INSTANCING_READY"
            + " scene=" + TargetScenePath
            + " sourceRenderers=" + sourceIslandStats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " finalRenderers=" + finalIslandStats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " groundRenderers=" + groundStats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " dressingRenderers=" + instancingStats.TotalRenderers.ToString(CultureInfo.InvariantCulture)
            + " instancingEligibleRenderers=" + instancingStats.EligibleRenderers.ToString(CultureInfo.InvariantCulture)
            + " instancingGroups=" + instancingStats.RepeatedGroups.ToString(CultureInfo.InvariantCulture)
            + " groundMeshBytes=" + groundMeshBytes.ToString(CultureInfo.InvariantCulture)
            + " report=" + ReportPath);
    }

    private static List<MeshRenderer> CollectDressingRenderers(Transform island, Transform excludeRoot)
    {
        List<MeshRenderer> result = new();
        for (int i = 0; i < DressingGroupNames.Length; i++)
        {
            Transform group = island.Find(DressingGroupNames[i]);
            if (group == null || group == excludeRoot || (excludeRoot != null && group.IsChildOf(excludeRoot)))
                continue;
            result.AddRange(CollectRenderableMeshRenderers(group));
        }

        return result;
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
            if (renderer == null || !renderer.enabled || mesh == null || ShouldExclude(renderer.transform))
                continue;

            result.Add(renderer);
        }

        return result;
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
            Matrix4x4 localToRoot = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

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

    private static OptimizationStats BuildCombinedGroundChunks(Transform groundRoot, List<ChunkBucket> buckets, ref long groundMeshBytes)
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
                name = $"GT8_GroundChunk_{bucket.Chunk.x}_{bucket.Chunk.y}_{Sanitize(bucket.Material.name)}",
                indexFormat = IndexFormat.UInt32
            };
            mesh.CombineMeshes(bucket.CombineInstances.ToArray(), true, true, false);
            mesh.RecalculateBounds();
            MeshUtility.Optimize(mesh);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{GroundChunkAssetRoot}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(mesh, assetPath);
            groundMeshBytes += AssetFileSize(assetPath);

            GameObject chunkObject = new(mesh.name);
            chunkObject.transform.SetParent(groundRoot, false);
            chunkObject.isStatic = false;

            MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = bucket.Material;
            ApplyRendererSettings(renderer);

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

    private static MaterialCopyStats ApplyInstancedMaterialCopies(List<MeshRenderer> renderers)
    {
        MaterialCopyStats stats = new();
        Dictionary<Material, Material> copies = new();
        for (int rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
        {
            MeshRenderer renderer = renderers[rendererIndex];
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                if (!copies.TryGetValue(material, out Material copy))
                {
                    copy = CreateInstancedMaterialCopy(material, copies.Count);
                    copies.Add(material, copy);
                    stats.CopiedMaterials++;
                    if (copy.enableInstancing)
                        stats.InstancingEnabledMaterials++;
                }

                materials[materialIndex] = copy;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }

        return stats;
    }

    private static Material CreateInstancedMaterialCopy(Material source, int index)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{InstancedMaterialRoot}/GT8_Instanced_{index:000}_{Sanitize(source.name)}.mat");
        Material copy = null;
        if (!string.IsNullOrEmpty(sourcePath) && File.Exists(ProjectPath(sourcePath)) && AssetDatabase.CopyAsset(sourcePath, targetPath))
        {
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            copy = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        }

        if (copy == null)
        {
            copy = new Material(source)
            {
                name = "GT8_Instanced_" + Sanitize(source.name)
            };
            AssetDatabase.CreateAsset(copy, targetPath);
        }

        copy.enableInstancing = true;
        EditorUtility.SetDirty(copy);
        return copy;
    }

    private static InstancingStats MeasureInstancing(List<MeshRenderer> renderers)
    {
        Dictionary<string, int> groups = new(StringComparer.Ordinal);
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
            bool allInstancingEnabled = materials.Length > 0;
            StringBuilder key = new();
            key.Append(AssetDatabase.GetAssetPath(mesh));
            key.Append('|');
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.enableInstancing)
                    allInstancingEnabled = false;
                key.Append(material != null ? AssetDatabase.GetAssetPath(material) : "null");
                key.Append(';');
            }

            if (!allInstancingEnabled)
                continue;

            string groupKey = key.ToString();
            groups.TryGetValue(groupKey, out int count);
            groups[groupKey] = count + 1;
        }

        foreach (KeyValuePair<string, int> entry in groups)
        {
            if (entry.Value <= 1)
                continue;

            stats.RepeatedGroups++;
            stats.EligibleRenderers += entry.Value;
            stats.LargestGroupSize = Mathf.Max(stats.LargestGroupSize, entry.Value);
        }

        return stats;
    }

    private static OptimizationStats Measure(List<MeshRenderer> renderers)
    {
        OptimizationStats stats = new();
        HashSet<Mesh> uniqueMeshes = new();
        HashSet<Material> uniqueMaterials = new();
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
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

            if (uniqueMeshes.Add(mesh))
                stats.UniqueMeshes++;

            int vertices = mesh.vertexCount;
            int triangles = CountTriangles(mesh);
            stats.Vertices += vertices;
            stats.Triangles += triangles;
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, vertices);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, triangles);
        }

        stats.UniqueMaterials = uniqueMaterials.Count;
        return stats;
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

    private static int RemoveNonessentialSceneObjects(Scene scene)
    {
        int removed = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                Transform transform = transforms[i];
                if (transform == null || transform.name == GroundChunkRootName)
                    continue;

                string name = transform.name;
                bool nonessential = name.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("MapTarget", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Reserve", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Probe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ProofCapture", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Bake_Camera", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!nonessential)
                    continue;

                UnityEngine.Object.DestroyImmediate(transform.gameObject);
                removed++;
            }
        }

        return removed;
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

    private static void ConfigureRenderSettingsForOptimizedTerrain()
    {
        RenderSettings.fog = false;
    }

    private static void DisableRendererCosts(MeshRenderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
            ApplyRendererSettings(renderers[i]);
    }

    private static void ApplyRendererSettings(MeshRenderer renderer)
    {
        if (renderer == null)
            return;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        renderer.allowOcclusionWhenDynamic = false;
    }

    private static int RemoveColliders(Transform root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(colliders[i]);
        return colliders.Length;
    }

    private static int RemoveComponents<T>(Transform root) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = components.Length - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(components[i]);
        return components.Length;
    }

    private static void ClearStaticFlags(Transform root)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.isStatic = false;
            GameObjectUtility.SetStaticEditorFlags(transforms[i].gameObject, 0);
        }
    }

    private static int CountTriangles(Mesh mesh)
    {
        int total = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            total += (int)mesh.GetIndexCount(i) / 3;
        return total;
    }

    private static List<string> CopyAcceptedProofCaptures()
    {
        List<string> captures = new();
        CopyProofCapture(SourceTopDownCapture, $"{CaptureRoot}/game_terrain8_source_topdown_proof.png", captures);
        CopyProofCapture(SourceGameplayCapture, $"{CaptureRoot}/game_terrain8_source_playable_angle_proof.png", captures);
        return captures;
    }

    private static void CopyProofCapture(string sourceRelativePath, string targetRelativePath, List<string> captures)
    {
        string source = ProjectPath(sourceRelativePath);
        string target = ProjectPath(targetRelativePath);
        if (!File.Exists(source))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(target));
        File.Copy(source, target, true);
        captures.Add(targetRelativePath);
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

    private static long AssetFileSize(string assetPath)
    {
        string fullPath = ProjectPath(assetPath);
        return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L;
    }

    private static void WriteSummaryJson(OptimizationStats sourceIslandStats, OptimizationStats sourceFoundationStats, OptimizationStats sourceDressingStats, OptimizationStats groundStats, OptimizationStats finalIslandStats, MaterialCopyStats materialCopyStats, InstancingStats instancingStats, int groundBuckets, long groundMeshBytes, int removedColliders, int removedSwitchers, int removedNonessential, List<string> captures)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"pipelineId\": \"GameTerrain8_HybridGroundChunksGpuInstancing\",");
        json.AppendLine("  \"date\": \"2026-05-27\",");
        json.AppendLine("  \"sourceScene\": \"" + SourceScenePath + "\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"chunkSizeWorldUnits\": " + ChunkSize.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"cameraBasis\": {");
        json.AppendLine("    \"normalHeight\": " + NormalCameraHeight.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"normalPitch\": " + NormalCameraPitch.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"normalFieldOfView\": " + NormalCameraFieldOfView.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"buildHeight\": " + BuildCameraHeight.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"buildPitch\": " + BuildCameraPitch.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"buildFieldOfView\": " + BuildCameraFieldOfView.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("    \"wideAspect\": " + WideAspect.ToString(CultureInfo.InvariantCulture));
        json.AppendLine("  },");
        json.AppendLine("  \"sourceIsland\": " + sourceIslandStats.ToJson(2) + ",");
        json.AppendLine("  \"sourceFoundation\": " + sourceFoundationStats.ToJson(2) + ",");
        json.AppendLine("  \"sourceDressing\": " + sourceDressingStats.ToJson(2) + ",");
        json.AppendLine("  \"combinedGround\": " + groundStats.ToJson(2) + ",");
        json.AppendLine("  \"finalIsland\": " + finalIslandStats.ToJson(2) + ",");
        json.AppendLine("  \"groundChunkMaterialBuckets\": " + groundBuckets.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"groundMeshAssetDiskBytes\": " + groundMeshBytes.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"instancedMaterials\": " + materialCopyStats.ToJson(2) + ",");
        json.AppendLine("  \"instancing\": " + instancingStats.ToJson(2) + ",");
        json.AppendLine("  \"removedColliders\": " + removedColliders.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"removedLodSwitchers\": " + removedSwitchers.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"removedNonessentialObjects\": " + removedNonessential.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"proofCaptures\": [");
        for (int i = 0; i < captures.Count; i++)
        {
            string comma = i + 1 < captures.Count ? "," : string.Empty;
            json.AppendLine("    \"" + captures[i] + "\"" + comma);
        }
        json.AppendLine("  ],");
        json.AppendLine("  \"validation\": [");
        json.AppendLine("    \"Ground/foundation renderers are chunk-combined.\",");
        json.AppendLine("    \"Generated dressing groups remain as repeated mesh renderers.\",");
        json.AppendLine("    \"Dressing material copies have GPU instancing enabled.\",");
        json.AppendLine("    \"Static flags are cleared so dressing is not static-batched away from instancing.\",");
        json.AppendLine("    \"Visual colliders are removed; gameplay remains grid, blocker, and heightmap driven.\",");
        json.AppendLine("    \"No live LOD switching is introduced.\"");
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SummaryJsonPath), json.ToString());
    }

    private static void WriteReport(OptimizationStats sourceIslandStats, OptimizationStats sourceFoundationStats, OptimizationStats sourceDressingStats, OptimizationStats groundStats, OptimizationStats finalIslandStats, MaterialCopyStats materialCopyStats, InstancingStats instancingStats, int groundBuckets, long groundMeshBytes, int removedColliders, int removedSwitchers, int removedNonessential, List<string> captures)
    {
        StringBuilder report = new();
        report.AppendLine("# Game_Terrain8 Hybrid Ground-Chunk GPU Instancing Optimizer");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-27");
        report.AppendLine();
        report.AppendLine("Purpose: `Game_Terrain8` is a profiling candidate that combines only the ground/foundation into 256m chunks and preserves generated dressing as repeated GPU-instancing-friendly renderers.");
        report.AppendLine();
        report.AppendLine("Implementation:");
        report.AppendLine("- Copy `Game_Terrain4` to `Game_Terrain8`.");
        report.AppendLine("- Combine only `Island/" + SourceFoundationName + "` into `" + GroundChunkRootName + "`.");
        report.AppendLine("- Keep generated mountains, trees, bushes, and rocks as scene renderers.");
        report.AppendLine("- Assign material copies from `" + InstancedMaterialRoot + "` with GPU instancing enabled.");
        report.AppendLine("- Remove visual colliders and disable shadows, probes, motion vectors, and static flags.");
        report.AppendLine();
        report.AppendLine("Results:");
        report.AppendLine("- Source island renderers/material slots/triangles: `" + sourceIslandStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceIslandStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceIslandStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Source foundation renderers/material slots/triangles: `" + sourceFoundationStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceFoundationStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceFoundationStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Source dressing renderers/material slots/triangles: `" + sourceDressingStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceDressingStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceDressingStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Combined ground renderers/material slots/triangles: `" + groundStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + groundStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + groundStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Final island renderers/material slots/triangles: `" + finalIslandStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + finalIslandStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + finalIslandStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Ground chunk/material buckets: `" + groundBuckets.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Ground mesh asset disk bytes: `" + groundMeshBytes.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Instanced material copies/enabled: `" + materialCopyStats.CopiedMaterials.ToString(CultureInfo.InvariantCulture) + "` / `" + materialCopyStats.InstancingEnabledMaterials.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Dressing renderers eligible for repeated mesh/material instancing: `" + instancingStats.EligibleRenderers.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Repeated instancing groups: `" + instancingStats.RepeatedGroups.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Largest repeated instancing group: `" + instancingStats.LargestGroupSize.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Removed colliders / LOD switchers / nonessential objects: `" + removedColliders.ToString(CultureInfo.InvariantCulture) + "` / `" + removedSwitchers.ToString(CultureInfo.InvariantCulture) + "` / `" + removedNonessential.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine();
        report.AppendLine("Profiling note:");
        report.AppendLine("- This pass prepares Unity GPU-instancing conditions for dressing, but actual instanced draw calls must still be verified in Frame Debugger or Profiler on the target renderer/device.");
        report.AppendLine("- Compared with `Game_Terrain7`, this scene is expected to have more renderers but much less generated unique mesh data because trees, bushes, rocks, and mountains are not baked into unique chunk meshes.");
        report.AppendLine();
        report.AppendLine("Proof captures:");
        if (captures.Count == 0)
            report.AppendLine("- Capture copy was skipped or unavailable.");
        for (int i = 0; i < captures.Count; i++)
            report.AppendLine("- `" + captures[i] + "`");
        report.AppendLine();
        report.AppendLine("Run command:");
        report.AppendLine("- `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain8HybridInstancingOptimizer.BuildHybridInstancedTerrain`");
        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static void MarkTaskComplete()
    {
        string fullPath = ProjectPath(TaskPath);
        if (!File.Exists(fullPath))
            return;

        string text = File.ReadAllText(fullPath);
        for (int i = 1; i <= 10; i++)
            text = text.Replace(i.ToString(CultureInfo.InvariantCulture) + ". Pending -", i.ToString(CultureInfo.InvariantCulture) + ". Complete -");
        File.WriteAllText(fullPath, text);
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

    private struct MaterialCopyStats
    {
        public int CopiedMaterials;
        public int InstancingEnabledMaterials;

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"copiedMaterials\": " + CopiedMaterials.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"instancingEnabledMaterials\": " + InstancingEnabledMaterials.ToString(CultureInfo.InvariantCulture));
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
}
#endif
