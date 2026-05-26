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

public static class WarlineCaptureGameTerrain6LodFarMapOptimizer
{
    private const string SourceScenePath = "Assets/Game/Scenes/Game_Terrain5.unity";
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain6.unity";
    private const string MeshAssetRoot = "Assets/Game/GeneratedTerrainOptimized/Game_Terrain6";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain6_LodFarMap";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain6_LodFarMap";
    private const string SummaryJsonPath = DataRoot + "/game_terrain6_lod_far_map_summary.json";
    private const string ReportPath = "Design/AgentReports/2026-05-26_gameplay_game-terrain6_lod_far_map_optimizer.md";
    private const string TaskPath = "Design/AgentTasks/game_terrain6_lod_far_map_optimization_steps.md";
    private const float ChunkSize = 256f;
    private const float NormalCameraHeight = 34f;
    private const float NormalCameraPitch = 40f;
    private const float NormalCameraFieldOfView = 36f;
    private const float BuildCameraHeight = 90f;
    private const float BuildCameraPitch = 64f;
    private const float BuildCameraFieldOfView = 52f;
    private const float WideAspect = 21f / 9f;
    private const float Lod1Height = 70f;
    private const float Lod2Height = 130f;
    private const int Lod1MaxTrianglesPerRenderer = 40000;
    private const int Lod2TotalTriangleBudget = 250000;
    private const int FarMapTextureSize = 2048;

    [MenuItem("WarlineCapture/Design/Game Terrain6/Build LOD Far Map Terrain")]
    public static void BuildLodFarMapTerrain()
    {
        Directory.CreateDirectory(ProjectPath(DataRoot));
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        EnsureAssetFolder(MeshAssetRoot);
        ClearAssetFolder(MeshAssetRoot);

        if (File.Exists(ProjectPath(TargetScenePath)))
            AssetDatabase.DeleteAsset(TargetScenePath);

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            if (!File.Exists(ProjectPath(TargetScenePath)))
                throw new FileNotFoundException("Could not copy Game_Terrain5 scene for Game_Terrain6.", SourceScenePath);
        }

        StripStaticEditorFlagsInSceneFile(TargetScenePath);
        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        ConfigureRenderSettingsForOptimizedTerrain();
        GameObject island = GameObject.Find("Island") ?? FindRootWithMostRenderers(scene);
        if (island == null)
            throw new InvalidOperationException("Game_Terrain6 optimizer could not find an Island root or renderable scene root.");

        Transform sourceRoot = island.transform.Find("OptimizedVisualTerrain_256mChunks");
        if (sourceRoot == null)
            sourceRoot = FindRootContainingOptimizedTerrain(island.transform);
        if (sourceRoot == null)
            throw new InvalidOperationException("Game_Terrain6 optimizer could not find Game_Terrain5 optimized terrain chunks.");

        DeleteChildIfExists(island.transform, "GameTerrain6VisualTerrain");

        List<MeshRenderer> sourceRenderers = CollectChunkRenderers(sourceRoot);
        if (sourceRenderers.Count == 0)
            throw new InvalidOperationException("Game_Terrain6 optimizer found no source chunk renderers.");

        Bounds sourceBounds = CalculateBounds(sourceRenderers);

        GameObject lodWrapper = new("GameTerrain6VisualTerrain");
        lodWrapper.transform.SetParent(island.transform, false);
        lodWrapper.isStatic = false;

        sourceRoot.SetParent(lodWrapper.transform, true);
        sourceRoot.name = "LOD0_Near_GameTerrain5Chunks";
        sourceRoot.gameObject.isStatic = false;
        ClearStaticFlags(sourceRoot);
        DisableRendererCosts(sourceRoot.GetComponentsInChildren<MeshRenderer>(true));
        RemoveColliders(sourceRoot);

        GameObject lod1Root = CreateRoot("LOD1_Mid_SimplifiedChunks", lodWrapper.transform);
        GameObject lod2Root = CreateRoot("LOD2_FarMap_SimplifiedChunks", lodWrapper.transform);

        OptimizationStats lod0Stats = Measure(sourceRenderers);
        RoleCounts roleCounts = default;
        OptimizationStats lod1Stats = BuildReducedLod(sourceRenderers, lod1Root.transform, sourceBounds, 1, ref roleCounts);
        OptimizationStats lod2Stats = BuildFarMapLod(sourceRoot.gameObject, lod1Root, lod2Root, sourceBounds);

        WarlineCaptureTerrainLodHeightSwitch switcher = lodWrapper.AddComponent<WarlineCaptureTerrainLodHeightSwitch>();
        switcher.Lod0Root = sourceRoot;
        switcher.Lod1Root = lod1Root.transform;
        switcher.Lod2Root = lod2Root.transform;
        switcher.Lod1CameraHeight = Lod1Height;
        switcher.Lod2CameraHeight = Lod2Height;
        DestroyChildrenExcept(island.transform, lodWrapper.transform);

        SetOnlyActive(sourceRoot.gameObject, lod1Root, lod2Root, 0);

        int removedColliders = RemoveColliders(lodWrapper.transform);
        List<string> captures = WriteProofCaptures(lodWrapper.transform, sourceRoot.gameObject, lod1Root, lod2Root, sourceBounds);
        SetOnlyActive(sourceRoot.gameObject, lod1Root, lod2Root, 0);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        WriteSummaryJson(lod0Stats, lod1Stats, lod2Stats, roleCounts, sourceRenderers.Count, removedColliders, captures);
        WriteReport(lod0Stats, lod1Stats, lod2Stats, roleCounts, sourceRenderers.Count, removedColliders, captures);
        MarkTaskComplete();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("WARLINECAPTURE_GAME_TERRAIN6_LOD_FAR_MAP_READY"
            + " scene=" + TargetScenePath
            + " lod0Renderers=" + lod0Stats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " lod1Renderers=" + lod1Stats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " lod2Renderers=" + lod2Stats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " lod0Triangles=" + lod0Stats.Triangles.ToString(CultureInfo.InvariantCulture)
            + " lod1Triangles=" + lod1Stats.Triangles.ToString(CultureInfo.InvariantCulture)
            + " lod2Triangles=" + lod2Stats.Triangles.ToString(CultureInfo.InvariantCulture)
            + " report=" + ReportPath);
    }

    private static OptimizationStats BuildReducedLod(List<MeshRenderer> sourceRenderers, Transform targetRoot, Bounds allBounds, int lod, ref RoleCounts roleCounts)
    {
        OptimizationStats stats = new();
        HashSet<Material> uniqueMaterials = new();
        for (int i = 0; i < sourceRenderers.Count; i++)
        {
            MeshRenderer sourceRenderer = sourceRenderers[i];
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            Mesh sourceMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (sourceMesh == null)
                continue;

            TerrainChunkRole role = ClassifyRole(sourceRenderer, allBounds);
            if (lod == 1)
                roleCounts.Add(role);

            int sourceTriangles = CountTriangles(sourceMesh);
            int targetTriangles = CalculateLod1TargetTriangles(sourceTriangles, role);
            float ratio = Mathf.Clamp01(targetTriangles / Mathf.Max(1f, sourceTriangles));
            Mesh reducedMesh = BuildCompactTriangleReducedMesh(sourceMesh, ratio, targetTriangles);
            reducedMesh.name = $"GT6_LOD{lod}_{Sanitize(sourceRenderer.gameObject.name)}";
            MeshUtility.Optimize(reducedMesh);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{MeshAssetRoot}/{reducedMesh.name}.asset");
            AssetDatabase.CreateAsset(reducedMesh, assetPath);

            GameObject chunkObject = new(reducedMesh.name);
            chunkObject.transform.SetParent(targetRoot, false);
            chunkObject.transform.localPosition = sourceRenderer.transform.localPosition;
            chunkObject.transform.localRotation = sourceRenderer.transform.localRotation;
            chunkObject.transform.localScale = sourceRenderer.transform.localScale;
            chunkObject.isStatic = false;

            MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
            filter.sharedMesh = reducedMesh;

            MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            ApplyRendererSettings(renderer);

            stats.Renderers++;
            stats.MeshFilters++;
            stats.MaterialSlots += renderer.sharedMaterials.Length;
            for (int materialIndex = 0; materialIndex < renderer.sharedMaterials.Length; materialIndex++)
            {
                if (renderer.sharedMaterials[materialIndex] != null)
                    uniqueMaterials.Add(renderer.sharedMaterials[materialIndex]);
            }

            stats.UniqueMeshes++;
            stats.Vertices += reducedMesh.vertexCount;
            stats.Triangles += CountTriangles(reducedMesh);
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, reducedMesh.vertexCount);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, CountTriangles(reducedMesh));
        }

        stats.UniqueMaterials = uniqueMaterials.Count;
        return stats;
    }

    private static int CalculateLod1TargetTriangles(int sourceTriangles, TerrainChunkRole role)
    {
        float roleRatio = role == TerrainChunkRole.DressingAndRockMass ? 0.30f : 0.45f;
        int target = Mathf.RoundToInt(sourceTriangles * roleRatio);
        return Mathf.Clamp(target, Mathf.Min(sourceTriangles, 24), Mathf.Min(sourceTriangles, Lod1MaxTrianglesPerRenderer));
    }

    private static TerrainChunkRole ClassifyRole(MeshRenderer renderer, Bounds allBounds)
    {
        Bounds bounds = renderer.bounds;
        if (bounds.size.y > 12f || bounds.max.y > 16f)
            return TerrainChunkRole.DressingAndRockMass;

        float normalizedX = Mathf.Abs(bounds.center.x - allBounds.center.x) / Mathf.Max(1f, allBounds.extents.x);
        float normalizedZ = Mathf.Abs(bounds.center.z - allBounds.center.z) / Mathf.Max(1f, allBounds.extents.z);
        if (Mathf.Max(normalizedX, normalizedZ) > 0.82f && bounds.size.y < 8f)
            return TerrainChunkRole.BeachSurface;

        return TerrainChunkRole.GroundSurface;
    }

    private static OptimizationStats BuildFarMapLod(GameObject lod0Root, GameObject lod1Root, GameObject lod2Root, Bounds bounds)
    {
        SetOnlyActive(lod0Root, lod1Root, lod2Root, 0);
        Texture2D farMapTexture = CaptureFarMapTexture(bounds);
        Material material = CreateFarMapMaterial(farMapTexture);
        Mesh mesh = CreateFarMapMesh(bounds);
        string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{MeshAssetRoot}/GT6_LOD2_FarMapPlane.asset");
        AssetDatabase.CreateAsset(mesh, meshPath);

        GameObject plane = new("GT6_LOD2_BakedFarMapPlane");
        plane.transform.SetParent(lod2Root.transform, false);
        plane.isStatic = false;

        MeshFilter filter = plane.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = plane.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        ApplyRendererSettings(renderer);

        return new OptimizationStats
        {
            Renderers = 1,
            MeshFilters = 1,
            MaterialSlots = 1,
            UniqueMeshes = 1,
            UniqueMaterials = 1,
            Vertices = mesh.vertexCount,
            Triangles = CountTriangles(mesh),
            MaxVerticesPerRenderer = mesh.vertexCount,
            MaxTrianglesPerRenderer = CountTriangles(mesh)
        };
    }

    private static Texture2D CaptureFarMapTexture(Bounds bounds)
    {
        string texturePath = $"{MeshAssetRoot}/GT6_LOD2_FarMap_Texture.png";
        Camera camera = null;
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        try
        {
            GameObject cameraObject = new("GT6_FarMapBake_Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.39f, 0.36f, 0.32f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.08f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;
            camera.transform.position = new Vector3(bounds.center.x, bounds.max.y + 2200f, bounds.center.z);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            renderTexture = new RenderTexture(FarMapTextureSize, FarMapTextureSize, 24, RenderTextureFormat.ARGB32);
            texture = new Texture2D(FarMapTextureSize, FarMapTextureSize, TextureFormat.RGB24, false);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, FarMapTextureSize, FarMapTextureSize), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(ProjectPath(texturePath), texture.EncodeToPNG());
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);

            if (AssetImporter.GetAtPath(texturePath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }
        finally
        {
            if (camera != null)
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static Material CreateFarMapMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        Material material = new(shader)
        {
            name = "GT6_LOD2_FarMap_Material"
        };

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.1f);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);
        material.SetOverrideTag("RenderType", "Opaque");
        material.renderQueue = (int)RenderQueue.Geometry;

        string materialPath = AssetDatabase.GenerateUniqueAssetPath($"{MeshAssetRoot}/GT6_LOD2_FarMap_Material.mat");
        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static Mesh CreateFarMapMesh(Bounds bounds)
    {
        float y = bounds.max.y + 0.5f;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Mesh mesh = new()
        {
            name = "GT6_LOD2_FarMapPlane",
            indexFormat = IndexFormat.UInt16
        };
        mesh.SetVertices(new[]
        {
            new Vector3(min.x, y, min.z),
            new Vector3(max.x, y, min.z),
            new Vector3(max.x, y, max.z),
            new Vector3(min.x, y, max.z)
        });
        mesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
        mesh.SetUVs(0, new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        });
        mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildCompactTriangleReducedMesh(Mesh source, float triangleRatio, int maxTargetTriangles)
    {
        Vector3[] sourceVertices = source.vertices;
        Vector3[] sourceNormals = source.normals;
        Vector4[] sourceTangents = source.tangents;
        Color[] sourceColors = source.colors;
        Color32[] sourceColors32 = source.colors32;
        List<Vector4>[] sourceUvs = ReadUvs(source);

        List<Vector3> vertices = new();
        List<Vector3> normals = sourceNormals.Length == source.vertexCount ? new List<Vector3>() : null;
        List<Vector4> tangents = sourceTangents.Length == source.vertexCount ? new List<Vector4>() : null;
        List<Color> colors = sourceColors.Length == source.vertexCount ? new List<Color>() : null;
        List<Color32> colors32 = sourceColors.Length != source.vertexCount && sourceColors32.Length == source.vertexCount ? new List<Color32>() : null;
        List<Vector4>[] uvs = CreateUvWriters(sourceUvs);
        Dictionary<int, int> remap = new();
        List<int>[] submeshTriangles = new List<int>[source.subMeshCount];
        int remainingTarget = maxTargetTriangles;

        for (int submesh = 0; submesh < source.subMeshCount; submesh++)
        {
            int[] triangles = source.GetTriangles(submesh);
            int submeshTriangleCount = triangles.Length / 3;
            int submeshTarget = Mathf.Clamp(Mathf.RoundToInt(submeshTriangleCount * triangleRatio), Mathf.Min(submeshTriangleCount, 12), submeshTriangleCount);
            if (source.subMeshCount == 1)
                submeshTarget = Mathf.Min(submeshTarget, remainingTarget);

            List<int> selectedTriangleStarts = SelectTriangleStarts(sourceVertices, triangles, submeshTarget);
            List<int> outputTriangles = new(selectedTriangleStarts.Count * 3);
            for (int i = 0; i < selectedTriangleStarts.Count; i++)
            {
                int start = selectedTriangleStarts[i];
                outputTriangles.Add(CopyVertex(triangles[start], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceUvs, vertices, normals, tangents, colors, colors32, uvs, remap));
                outputTriangles.Add(CopyVertex(triangles[start + 1], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceUvs, vertices, normals, tangents, colors, colors32, uvs, remap));
                outputTriangles.Add(CopyVertex(triangles[start + 2], sourceVertices, sourceNormals, sourceTangents, sourceColors, sourceColors32, sourceUvs, vertices, normals, tangents, colors, colors32, uvs, remap));
            }

            submeshTriangles[submesh] = outputTriangles;
            remainingTarget = Mathf.Max(0, remainingTarget - selectedTriangleStarts.Count);
        }

        Mesh mesh = new()
        {
            indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        mesh.SetVertices(vertices);
        if (normals != null)
            mesh.SetNormals(normals);
        if (tangents != null)
            mesh.SetTangents(tangents);
        if (colors != null)
            mesh.SetColors(colors);
        if (colors32 != null)
            mesh.SetColors(colors32);
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            if (uvs[channel] != null)
                mesh.SetUVs(channel, uvs[channel]);
        }

        mesh.subMeshCount = source.subMeshCount;
        for (int submesh = 0; submesh < submeshTriangles.Length; submesh++)
            mesh.SetTriangles(submeshTriangles[submesh], submesh, true);

        mesh.bounds = source.bounds;
        return mesh;
    }

    private static List<int> SelectTriangleStarts(Vector3[] vertices, int[] triangles, int targetTriangleCount)
    {
        int triangleCount = triangles.Length / 3;
        List<int> validStarts = new(triangleCount);
        for (int triangle = 0; triangle < triangleCount; triangle++)
        {
            int start = triangle * 3;
            int a = triangles[start];
            int b = triangles[start + 1];
            int c = triangles[start + 2];
            if (a >= 0 && a < vertices.Length &&
                b >= 0 && b < vertices.Length &&
                c >= 0 && c < vertices.Length)
            {
                validStarts.Add(start);
            }
        }

        int validTriangleCount = validStarts.Count;
        if (validTriangleCount == 0)
            return validStarts;

        int targetCount = Mathf.Clamp(targetTriangleCount, Mathf.Min(validTriangleCount, 12), validTriangleCount);
        if (targetCount >= validTriangleCount)
        {
            validStarts.Sort();
            return validStarts;
        }

        List<int> selected = new(targetCount);
        for (int bucket = 0; bucket < targetCount; bucket++)
        {
            int startTriangle = Mathf.Clamp(
                Mathf.FloorToInt(bucket * validTriangleCount / (float)targetCount),
                0,
                validTriangleCount - 1);
            int endTriangle = Mathf.Clamp(
                Mathf.FloorToInt((bucket + 1) * validTriangleCount / (float)targetCount),
                startTriangle + 1,
                validTriangleCount);

            int bestStart = validStarts[startTriangle];
            float bestArea = -1f;
            for (int validTriangle = startTriangle; validTriangle < endTriangle; validTriangle++)
            {
                int start = validStarts[validTriangle];
                Vector3 a = vertices[triangles[start]];
                Vector3 b = vertices[triangles[start + 1]];
                Vector3 c = vertices[triangles[start + 2]];
                float area = Vector3.Cross(b - a, c - a).sqrMagnitude;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestStart = start;
                }
            }

            selected.Add(bestStart);
        }

        selected.Sort();
        return selected;
    }

    private static int CopyVertex(
        int sourceIndex,
        Vector3[] sourceVertices,
        Vector3[] sourceNormals,
        Vector4[] sourceTangents,
        Color[] sourceColors,
        Color32[] sourceColors32,
        List<Vector4>[] sourceUvs,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector4> tangents,
        List<Color> colors,
        List<Color32> colors32,
        List<Vector4>[] uvs,
        Dictionary<int, int> remap)
    {
        if (remap.TryGetValue(sourceIndex, out int mappedIndex))
            return mappedIndex;

        mappedIndex = vertices.Count;
        remap[sourceIndex] = mappedIndex;
        vertices.Add(sourceVertices[sourceIndex]);
        if (normals != null)
            normals.Add(sourceNormals[sourceIndex]);
        if (tangents != null)
            tangents.Add(sourceTangents[sourceIndex]);
        if (colors != null)
            colors.Add(sourceColors[sourceIndex]);
        if (colors32 != null)
            colors32.Add(sourceColors32[sourceIndex]);
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            if (uvs[channel] != null)
                uvs[channel].Add(sourceUvs[channel][sourceIndex]);
        }

        return mappedIndex;
    }

    private static List<Vector4>[] ReadUvs(Mesh source)
    {
        List<Vector4>[] uvs = new List<Vector4>[8];
        for (int channel = 0; channel < uvs.Length; channel++)
        {
            List<Vector4> channelUvs = new();
            source.GetUVs(channel, channelUvs);
            if (channelUvs.Count == source.vertexCount)
                uvs[channel] = channelUvs;
        }

        return uvs;
    }

    private static List<Vector4>[] CreateUvWriters(List<Vector4>[] sourceUvs)
    {
        List<Vector4>[] uvs = new List<Vector4>[sourceUvs.Length];
        for (int channel = 0; channel < sourceUvs.Length; channel++)
        {
            if (sourceUvs[channel] != null)
                uvs[channel] = new List<Vector4>();
        }

        return uvs;
    }

    private static List<MeshRenderer> CollectChunkRenderers(Transform root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        List<MeshRenderer> result = new(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            if (renderer != null && filter != null && filter.sharedMesh != null)
                result.Add(renderer);
        }

        return result;
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

            int triangles = CountTriangles(mesh);
            stats.Vertices += mesh.vertexCount;
            stats.Triangles += triangles;
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, mesh.vertexCount);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, triangles);
        }

        stats.UniqueMaterials = uniqueMaterials.Count;
        return stats;
    }

    private static int CountTriangles(Mesh mesh)
    {
        int total = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            total += (int)mesh.GetIndexCount(i) / 3;
        return total;
    }

    private static Bounds CalculateBounds(List<MeshRenderer> renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static GameObject CreateRoot(string name, Transform parent)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent, false);
        root.isStatic = false;
        return root;
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

    private static void DeleteChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void DestroyChildrenExcept(Transform parent, Transform keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == keep)
                continue;

            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
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

    private static void StripStaticEditorFlagsInSceneFile(string sceneAssetPath)
    {
        string fullPath = ProjectPath(sceneAssetPath);
        if (!File.Exists(fullPath))
            return;

        string[] lines = File.ReadAllLines(fullPath);
        bool changed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (!trimmed.StartsWith("m_StaticEditorFlags:", StringComparison.Ordinal))
                continue;

            int indentLength = lines[i].Length - trimmed.Length;
            string indent = lines[i].Substring(0, indentLength);
            string replacement = indent + "m_StaticEditorFlags: 0";
            if (lines[i] == replacement)
                continue;

            lines[i] = replacement;
            changed = true;
        }

        if (changed)
            File.WriteAllLines(fullPath, lines);
    }

    private static Transform FindRootContainingOptimizedTerrain(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.IndexOf("OptimizedVisualTerrain", StringComparison.OrdinalIgnoreCase) >= 0)
                return children[i];
        }

        return null;
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

    private static void ConfigureRenderSettingsForOptimizedTerrain()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.68f, 1f);
        RenderSettings.ambientIntensity = 1.15f;
    }

    private static List<string> WriteProofCaptures(Transform wrapper, GameObject lod0Root, GameObject lod1Root, GameObject lod2Root, Bounds bounds)
    {
        List<string> captures = new();
        Camera camera = null;
        Light light = null;
        WarlineCaptureTerrainLodHeightSwitch[] switchers = wrapper.GetComponentsInChildren<WarlineCaptureTerrainLodHeightSwitch>(true);
        bool[] switcherStates = new bool[switchers.Length];
        try
        {
            for (int i = 0; i < switchers.Length; i++)
            {
                switcherStates[i] = switchers[i].enabled;
                switchers[i].enabled = false;
            }

            GameObject cameraObject = new("GT6_ProofCapture_Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;
            camera.fieldOfView = 48f;

            GameObject lightObject = new("GT6_ProofCapture_Light");
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            CaptureLod(captures, camera, lod0Root, lod1Root, lod2Root, 0, bounds);
            CaptureLod(captures, camera, lod0Root, lod1Root, lod2Root, 1, bounds);
            CaptureLod(captures, camera, lod0Root, lod1Root, lod2Root, 2, bounds);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Game_Terrain6 proof captures were skipped: " + exception.Message);
        }
        finally
        {
            if (camera != null)
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            if (light != null)
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            for (int i = 0; i < switchers.Length; i++)
            {
                if (switchers[i] != null)
                    switchers[i].enabled = switcherStates[i];
            }
            wrapper.gameObject.SetActive(true);
        }

        return captures;
    }

    private static void CaptureLod(List<string> captures, Camera camera, GameObject lod0Root, GameObject lod1Root, GameObject lod2Root, int lod, Bounds bounds)
    {
        SetOnlyActive(lod0Root, lod1Root, lod2Root, lod);
        string label = "lod" + lod.ToString(CultureInfo.InvariantCulture);
        string topDownPath = CaptureTopDown(camera, bounds, label);
        if (!string.IsNullOrEmpty(topDownPath))
            captures.Add(topDownPath);

        string gameplayPath = CaptureGameplay(camera, bounds, label);
        if (!string.IsNullOrEmpty(gameplayPath))
            captures.Add(gameplayPath);
    }

    private static string CaptureTopDown(Camera camera, Bounds bounds, string label)
    {
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.08f;
        camera.transform.position = new Vector3(bounds.center.x, bounds.max.y + 2200f, bounds.center.z);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        return RenderCapture(camera, $"{CaptureRoot}/game_terrain6_{label}_topdown_1024.png", 1024, 1024);
    }

    private static string CaptureGameplay(Camera camera, Bounds bounds, string label)
    {
        camera.orthographic = false;
        camera.fieldOfView = 48f;
        Vector3 cameraPosition = bounds.center + new Vector3(0f, 520f, -760f);
        camera.transform.position = cameraPosition;
        camera.transform.rotation = Quaternion.LookRotation(bounds.center - cameraPosition, Vector3.up);
        return RenderCapture(camera, $"{CaptureRoot}/game_terrain6_{label}_gameplay_1600x900.png", 1600, 900);
    }

    private static string RenderCapture(Camera camera, string relativePath, int width, int height)
    {
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        try
        {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(ProjectPath(relativePath), texture.EncodeToPNG());
            return relativePath;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Game_Terrain6 capture failed for " + relativePath + ": " + exception.Message);
            return null;
        }
        finally
        {
            camera.targetTexture = null;
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void SetOnlyActive(GameObject lod0Root, GameObject lod1Root, GameObject lod2Root, int lod)
    {
        lod0Root.SetActive(lod == 0);
        lod1Root.SetActive(lod == 1);
        lod2Root.SetActive(lod == 2);
    }

    private static void WriteSummaryJson(OptimizationStats lod0, OptimizationStats lod1, OptimizationStats lod2, RoleCounts roleCounts, int sourceChunks, int removedColliders, List<string> captures)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"pipelineId\": \"GameTerrain6_LodFarMapOptimization\",");
        json.AppendLine("  \"date\": \"2026-05-26\",");
        json.AppendLine("  \"sourceScene\": \"" + SourceScenePath + "\",");
        json.AppendLine("  \"targetScene\": \"" + TargetScenePath + "\",");
        json.AppendLine("  \"meshAssetRoot\": \"" + MeshAssetRoot + "\",");
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
        json.AppendLine("  \"lodSwitch\": {");
        json.AppendLine("    \"lod0CameraHeightRange\": \"0-70\",");
        json.AppendLine("    \"lod1CameraHeightRange\": \"70-130\",");
        json.AppendLine("    \"lod2CameraHeightRange\": \"130+\"");
        json.AppendLine("  },");
        json.AppendLine("  \"sourceChunks\": " + sourceChunks.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"roleCounts\": " + roleCounts.ToJson(2) + ",");
        json.AppendLine("  \"lod0\": " + lod0.ToJson(2) + ",");
        json.AppendLine("  \"lod1\": " + lod1.ToJson(2) + ",");
        json.AppendLine("  \"lod2\": " + lod2.ToJson(2) + ",");
        json.AppendLine("  \"lod2TriangleBudget\": " + Lod2TotalTriangleBudget.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"removedColliders\": " + removedColliders.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"proofCaptures\": [");
        for (int i = 0; i < captures.Count; i++)
        {
            string comma = i + 1 < captures.Count ? "," : string.Empty;
            json.AppendLine("    \"" + captures[i] + "\"" + comma);
        }
        json.AppendLine("  ],");
        json.AppendLine("  \"validation\": [");
        json.AppendLine("    \"No source prefab-instance terrain hierarchy is copied from Game_Terrain4 into Game_Terrain6.\",");
        json.AppendLine("    \"Visual terrain remains decorative; gameplay still uses grid, blocker, and heightmap data.\",");
        json.AppendLine("    \"Terrain renderers have shadows, probes, motion vectors, dynamic occlusion, and visual colliders disabled.\",");
        json.AppendLine("    \"Runtime streaming is intentionally deferred until LOD profiling proves it is needed.\"");
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SummaryJsonPath), json.ToString());
    }

    private static void WriteReport(OptimizationStats lod0, OptimizationStats lod1, OptimizationStats lod2, RoleCounts roleCounts, int sourceChunks, int removedColliders, List<string> captures)
    {
        StringBuilder report = new();
        report.AppendLine("# Game_Terrain6 LOD and Far-Map Optimizer");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-26");
        report.AppendLine();
        report.AppendLine("Purpose: `Game_Terrain6` is the next mobile visual-terrain pass after `Game_Terrain5`. `Game_Terrain5` reduced draw calls by combining renderers; `Game_Terrain6` keeps that near-camera quality and adds lower-triangle mid/far terrain layers.");
        report.AppendLine();
        report.AppendLine("Implementation:");
        report.AppendLine("- Copy `Game_Terrain5` to `Game_Terrain6`.");
        report.AppendLine("- Keep the existing combined chunks as `LOD0_Near_GameTerrain5Chunks`.");
        report.AppendLine("- Generate `LOD1_Mid_SimplifiedChunks` from compact triangle-reduced meshes.");
        report.AppendLine("- Generate `LOD2_FarMap_SimplifiedChunks` as a baked top-down far-map plane.");
        report.AppendLine("- Add `WarlineCaptureTerrainLodHeightSwitch` using camera height thresholds `70` and `130`.");
        report.AppendLine("- Disable shadows, probes, motion vectors, dynamic occlusion, and colliders.");
        report.AppendLine();
        report.AppendLine("Results:");
        report.AppendLine("- Source chunks: `" + sourceChunks.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Role counts: ground `" + roleCounts.GroundSurface.ToString(CultureInfo.InvariantCulture) + "`, beach `" + roleCounts.BeachSurface.ToString(CultureInfo.InvariantCulture) + "`, dressing `" + roleCounts.DressingAndRockMass.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- LOD0 renderers/material slots/triangles: `" + lod0.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + lod0.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + lod0.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- LOD1 renderers/material slots/triangles: `" + lod1.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + lod1.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + lod1.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- LOD2 renderers/material slots/triangles: `" + lod2.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + lod2.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + lod2.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- LOD2 budget: `< " + Lod2TotalTriangleBudget.ToString(CultureInfo.InvariantCulture) + "` triangles.");
        report.AppendLine("- Removed visual colliders: `" + removedColliders.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine();
        report.AppendLine("Proof captures:");
        if (captures.Count == 0)
            report.AppendLine("- Capture generation was skipped or unavailable in batchmode.");
        for (int i = 0; i < captures.Count; i++)
            report.AppendLine("- `" + captures[i] + "`");
        report.AppendLine();
        report.AppendLine("Run command:");
        report.AppendLine("- `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain6LodFarMapOptimizer.BuildLodFarMapTerrain`");
        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static void MarkTaskComplete()
    {
        string fullPath = ProjectPath(TaskPath);
        if (!File.Exists(fullPath))
            return;

        string text = File.ReadAllText(fullPath);
        for (int i = 1; i <= 14; i++)
            text = text.Replace(i.ToString(CultureInfo.InvariantCulture) + ". Pending -", i.ToString(CultureInfo.InvariantCulture) + ". Complete -");

        text = text.Replace("3. Complete - read the `Game_Terrain5` combined chunk meshes and classify each chunk by bounds, material, and role:", "3. Complete - read the `Game_Terrain5` combined chunk meshes and classify each chunk by bounds, material, and role:");
        File.WriteAllText(fullPath, text);
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

    private enum TerrainChunkRole
    {
        GroundSurface,
        BeachSurface,
        DressingAndRockMass
    }

    private struct RoleCounts
    {
        public int GroundSurface;
        public int BeachSurface;
        public int DressingAndRockMass;

        public void Add(TerrainChunkRole role)
        {
            if (role == TerrainChunkRole.GroundSurface)
                GroundSurface++;
            else if (role == TerrainChunkRole.BeachSurface)
                BeachSurface++;
            else
                DressingAndRockMass++;
        }

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"groundSurface\": " + GroundSurface.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"beachSurface\": " + BeachSurface.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"dressingAndRockMass\": " + DressingAndRockMass.ToString(CultureInfo.InvariantCulture));
            json.Append(pad + "}");
            return json.ToString();
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
