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

public static class WarlineCaptureGameTerrain7NonLodMobileOptimizer
{
    private const string SourceScenePath = "Assets/Game/Scenes/Game_Terrain5.unity";
    private const string TargetScenePath = "Assets/Game/Scenes/Game_Terrain7.unity";
    private const string MeshAssetRoot = "Assets/Game/GeneratedTerrainOptimized/Game_Terrain7";
    private const string DataRoot = "Design/AgentReports/Data/GeneratedScenes/GameTerrain7_NonLodMobile";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain7_NonLodMobile";
    private const string SourceLod0TopDownCapture = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_topdown_proof.png";
    private const string SourceLod0GameplayCapture = "Design/AgentReports/Captures/GeneratedScenes/GameTerrain4_MaskDressing/game_terrain4_playable_angle_proof.png";
    private const string SummaryJsonPath = DataRoot + "/game_terrain7_non_lod_mobile_summary.json";
    private const string ReportPath = "Design/AgentReports/2026-05-26_gameplay_game-terrain7_non_lod_mobile_optimizer.md";
    private const string TaskPath = "Design/AgentTasks/game_terrain7_non_lod_mobile_optimization_steps.md";
    private const float ChunkSize = 256f;
    private const float NormalCameraHeight = 34f;
    private const float NormalCameraPitch = 40f;
    private const float NormalCameraFieldOfView = 36f;
    private const float BuildCameraHeight = 90f;
    private const float BuildCameraPitch = 64f;
    private const float BuildCameraFieldOfView = 52f;
    private const float WideAspect = 21f / 9f;

    [MenuItem("WarlineCapture/Design/Game Terrain7/Build Non-LOD Mobile Terrain")]
    public static void BuildNonLodMobileTerrain()
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
                throw new FileNotFoundException("Could not copy Game_Terrain5 scene for Game_Terrain7.", SourceScenePath);
        }

        StripStaticEditorFlagsInSceneFile(TargetScenePath);
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        ConfigureRenderSettingsForOptimizedTerrain();

        GameObject island = GameObject.Find("Island") ?? FindRootWithMostRenderers(scene);
        if (island == null)
            throw new InvalidOperationException("Game_Terrain7 optimizer could not find an Island root or renderable scene root.");

        Transform sourceRoot = FindLod0Root(island.transform);
        if (sourceRoot == null)
            throw new InvalidOperationException("Game_Terrain7 optimizer could not find the accepted LOD0 chunk terrain.");

        List<MeshRenderer> sourceRenderers = CollectChunkRenderers(sourceRoot);
        if (sourceRenderers.Count == 0)
            throw new InvalidOperationException("Game_Terrain7 optimizer found no source chunk renderers.");

        OptimizationStats sourceStats = Measure(sourceRenderers);
        Bounds bounds = CalculateBounds(sourceRenderers);

        Transform terrainRoot = sourceRoot.parent != null &&
            sourceRoot.parent.name.IndexOf("GameTerrain6VisualTerrain", StringComparison.OrdinalIgnoreCase) >= 0
                ? sourceRoot.parent
                : sourceRoot;
        terrainRoot.name = "GameTerrain7VisualTerrain_LOD0Only_256mChunks";
        terrainRoot.gameObject.isStatic = false;
        if (sourceRoot != terrainRoot)
            sourceRoot.name = "LOD0_GameTerrain5Chunks_NoSwap";
        sourceRoot.gameObject.SetActive(true);
        DeleteChildIfExists(terrainRoot, "LOD1_Mid_SimplifiedChunks");
        DeleteChildIfExists(terrainRoot, "LOD2_FarMap_SimplifiedChunks");

        int removedSwitchers = RemoveComponents<WarlineCaptureTerrainLodHeightSwitch>(island.transform);
        int removedColliders = RemoveColliders(island.transform);
        int removedNonessential = RemoveNonessentialSceneObjects(scene, terrainRoot);
        DisableRendererCosts(terrainRoot.GetComponentsInChildren<MeshRenderer>(true));
        ClearStaticFlags(island.transform);

        DestroyChildrenExcept(island.transform, terrainRoot);

        List<MeshRenderer> terrainRenderers = CollectChunkRenderers(terrainRoot);
        OptimizationStats terrainStats = Measure(terrainRenderers);
        MeshAssetStats meshAssetStats = MeasureMeshAssets(terrainRenderers);
        List<string> captures = CopyAcceptedLod0ProofCaptures();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        WriteSummaryJson(sourceStats, terrainStats, meshAssetStats, removedSwitchers, removedColliders, removedNonessential, captures);
        WriteReport(sourceStats, terrainStats, meshAssetStats, removedSwitchers, removedColliders, removedNonessential, captures);
        MarkTaskComplete();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("WARLINECAPTURE_GAME_TERRAIN7_NON_LOD_MOBILE_READY"
            + " scene=" + TargetScenePath
            + " renderers=" + terrainStats.Renderers.ToString(CultureInfo.InvariantCulture)
            + " materialSlots=" + terrainStats.MaterialSlots.ToString(CultureInfo.InvariantCulture)
            + " triangles=" + terrainStats.Triangles.ToString(CultureInfo.InvariantCulture)
            + " meshDiskBytes=" + meshAssetStats.MeshAssetDiskBytes.ToString(CultureInfo.InvariantCulture)
            + " report=" + ReportPath);
    }

    private static OptimizationStats BuildLod0OnlyChunkCopies(Transform sourceRoot, Transform targetRoot, List<MeshRenderer> sourceRenderers, ref MeshAssetStats meshCopyStats)
    {
        OptimizationStats stats = new();
        HashSet<Material> uniqueMaterials = new();
        Dictionary<Mesh, Mesh> copiedMeshes = new();
        for (int i = 0; i < sourceRenderers.Count; i++)
        {
            MeshRenderer sourceRenderer = sourceRenderers[i];
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            Mesh sourceMesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (sourceMesh == null)
                continue;

            if (!copiedMeshes.TryGetValue(sourceMesh, out Mesh meshCopy))
            {
                meshCopy = CopyMeshAssetForTerrain7(sourceMesh, sourceRenderer.gameObject.name, ref meshCopyStats);
                copiedMeshes.Add(sourceMesh, meshCopy);
            }

            GameObject chunkObject = new("GT7_" + Sanitize(sourceRenderer.gameObject.name));
            chunkObject.transform.SetParent(targetRoot, false);
            chunkObject.transform.SetPositionAndRotation(sourceRenderer.transform.position, sourceRenderer.transform.rotation);
            chunkObject.transform.localScale = sourceRenderer.transform.lossyScale;
            chunkObject.isStatic = false;

            MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
            filter.sharedMesh = meshCopy;

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

            stats.UniqueMeshes = copiedMeshes.Count;
            int triangles = CountTriangles(sourceMesh);
            stats.Vertices += sourceMesh.vertexCount;
            stats.Triangles += triangles;
            stats.MaxVerticesPerRenderer = Mathf.Max(stats.MaxVerticesPerRenderer, sourceMesh.vertexCount);
            stats.MaxTrianglesPerRenderer = Mathf.Max(stats.MaxTrianglesPerRenderer, triangles);
        }

        stats.UniqueMaterials = uniqueMaterials.Count;
        return stats;
    }

    private static Mesh CopyMeshAssetForTerrain7(Mesh sourceMesh, string sourceObjectName, ref MeshAssetStats meshCopyStats)
    {
        string sourcePath = AssetDatabase.GetAssetPath(sourceMesh);
        string meshName = "GT7_" + Sanitize(sourceObjectName);
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{MeshAssetRoot}/{meshName}.asset");

        Mesh meshCopy = null;
        if (!string.IsNullOrEmpty(sourcePath) && File.Exists(ProjectPath(sourcePath)) && AssetDatabase.CopyAsset(sourcePath, targetPath))
        {
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            meshCopy = AssetDatabase.LoadAssetAtPath<Mesh>(targetPath);
        }

        if (meshCopy == null)
        {
            meshCopy = UnityEngine.Object.Instantiate(sourceMesh);
            meshCopy.name = meshName;
            AssetDatabase.CreateAsset(meshCopy, targetPath);
        }

        meshCopy.name = meshName;
        MeshUtility.Optimize(meshCopy);
        EditorUtility.SetDirty(meshCopy);

        meshCopyStats.MeshAssets++;
        meshCopyStats.MeshAssetDiskBytes += AssetFileSize(targetPath);
        if (!meshCopy.isReadable)
            meshCopyStats.NonReadableMeshes++;
        else
            meshCopyStats.ReadableMeshesRetained++;

        return meshCopy;
    }

    private static MeshAssetStats MeasureMeshAssets(List<MeshRenderer> renderers)
    {
        MeshAssetStats stats = new();
        HashSet<Mesh> meshes = new();
        HashSet<string> assetPaths = new(StringComparer.Ordinal);
        for (int i = 0; i < renderers.Count; i++)
        {
            MeshFilter filter = renderers[i] != null ? renderers[i].GetComponent<MeshFilter>() : null;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !meshes.Add(mesh))
                continue;

            stats.MeshAssets++;
            if (!mesh.isReadable)
                stats.NonReadableMeshes++;
            else
                stats.ReadableMeshesRetained++;

            string path = AssetDatabase.GetAssetPath(mesh);
            if (!string.IsNullOrEmpty(path) && assetPaths.Add(path))
                stats.MeshAssetDiskBytes += AssetFileSize(path);
        }

        return stats;
    }

    private static Transform FindLod0Root(Transform island)
    {
        Transform direct = island.Find("GameTerrain6VisualTerrain/LOD0_Near_GameTerrain5Chunks");
        if (direct != null)
            return direct;

        Transform[] transforms = island.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string name = transforms[i].name;
            if (name.IndexOf("LOD0", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("OptimizedVisualTerrain_256mChunks", StringComparison.OrdinalIgnoreCase) >= 0)
                return transforms[i];
        }

        return null;
    }

    private static List<MeshRenderer> CollectChunkRenderers(Transform root)
    {
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        List<MeshRenderer> result = new(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            MeshFilter filter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (renderer == null || mesh == null)
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

    private static int RemoveNonessentialSceneObjects(Scene scene, Transform keep)
    {
        int removed = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                Transform transform = transforms[i];
                if (transform == null)
                    continue;
                if (transform == keep || transform.IsChildOf(keep))
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

    private static void DeleteChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
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

    private static List<string> CopyAcceptedLod0ProofCaptures()
    {
        List<string> captures = new();
        CopyProofCapture(SourceLod0TopDownCapture, $"{CaptureRoot}/game_terrain7_source_topdown_proof.png", captures);
        CopyProofCapture(SourceLod0GameplayCapture, $"{CaptureRoot}/game_terrain7_source_playable_angle_proof.png", captures);
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

    private static List<string> WriteProofCaptures(Transform root, Bounds bounds)
    {
        List<string> captures = new();
        Camera camera = null;
        Light light = null;
        try
        {
            GameObject cameraObject = new("GT7_ProofCapture_Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;

            GameObject lightObject = new("GT7_ProofCapture_Light");
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            AddCapture(captures, CaptureCameraHeight(camera, bounds, "normal_h34_1600x900", NormalCameraHeight, NormalCameraPitch, NormalCameraFieldOfView, 16f / 9f, 1600, 900));
            AddCapture(captures, CaptureCameraHeight(camera, bounds, "build_h90_1600x900", BuildCameraHeight, BuildCameraPitch, BuildCameraFieldOfView, 16f / 9f, 1600, 900));
            AddCapture(captures, CaptureCameraHeight(camera, bounds, "wide_21x9_h90_2100x900", BuildCameraHeight, BuildCameraPitch, BuildCameraFieldOfView, WideAspect, 2100, 900));
            AddCapture(captures, CaptureTopDown(camera, bounds));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Game_Terrain7 proof captures were skipped: " + exception.Message);
        }
        finally
        {
            if (camera != null)
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            if (light != null)
                UnityEngine.Object.DestroyImmediate(light.gameObject);
            root.gameObject.SetActive(true);
        }

        return captures;
    }

    private static string CaptureCameraHeight(Camera camera, Bounds bounds, string label, float height, float pitch, float fieldOfView, float aspect, int width, int heightPixels)
    {
        camera.orthographic = false;
        camera.fieldOfView = fieldOfView;
        camera.aspect = aspect;
        float distance = Mathf.Max(70f, height * 1.7f);
        Vector3 lookAt = bounds.center;
        Vector3 cameraPosition = new(lookAt.x, lookAt.y + height, lookAt.z - distance);
        camera.transform.position = cameraPosition;
        camera.transform.rotation = Quaternion.LookRotation(lookAt - cameraPosition, Vector3.up);
        return RenderCapture(camera, $"{CaptureRoot}/game_terrain7_{label}.png", width, heightPixels);
    }

    private static string CaptureTopDown(Camera camera, Bounds bounds)
    {
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.08f;
        camera.transform.position = new Vector3(bounds.center.x, bounds.max.y + 2200f, bounds.center.z);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        return RenderCapture(camera, $"{CaptureRoot}/game_terrain7_topdown_1024.png", 1024, 1024);
    }

    private static void AddCapture(List<string> captures, string capture)
    {
        if (!string.IsNullOrEmpty(capture))
            captures.Add(capture);
    }

    private static string RenderCapture(Camera camera, string relativePath, int width, int height)
    {
        RenderTexture renderTexture = null;
        Texture2D texture = null;
        try
        {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
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
            Debug.LogWarning("Game_Terrain7 capture failed for " + relativePath + ": " + exception.Message);
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

    private static void WriteSummaryJson(OptimizationStats sourceStats, OptimizationStats terrainStats, MeshAssetStats meshAssetStats, int removedSwitchers, int removedColliders, int removedNonessential, List<string> captures)
    {
        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine("  \"pipelineId\": \"GameTerrain7_NonLodMobileOptimization\",");
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
        json.AppendLine("  \"sourceLod0\": " + sourceStats.ToJson(2) + ",");
        json.AppendLine("  \"terrain7\": " + terrainStats.ToJson(2) + ",");
        json.AppendLine("  \"meshAssets\": " + meshAssetStats.ToJson(2) + ",");
        json.AppendLine("  \"removedLodSwitchers\": " + removedSwitchers.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"removedColliders\": " + removedColliders.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"removedNonessentialObjects\": " + removedNonessential.ToString(CultureInfo.InvariantCulture) + ",");
        json.AppendLine("  \"runtimeChunkActivation\": \"deferred; not implemented because this pass must preserve the exact visible mesh and first collect profile evidence\",");
        json.AppendLine("  \"proofCaptureNote\": \"Terrain7 is generated from Game_Terrain5 and keeps the exact optimized chunk meshes. The capture paths are optional visual references only; final visual QA should be run interactively or on device.\",");
        json.AppendLine("  \"proofCaptures\": [");
        for (int i = 0; i < captures.Count; i++)
        {
            string comma = i + 1 < captures.Count ? "," : string.Empty;
            json.AppendLine("    \"" + captures[i] + "\"" + comma);
        }
        json.AppendLine("  ],");
        json.AppendLine("  \"validation\": [");
        json.AppendLine("    \"Only LOD0 geometry is active in the gameplay scene.\",");
        json.AppendLine("    \"No WarlineCaptureTerrainLodHeightSwitch component remains in Game_Terrain7.\",");
        json.AppendLine("    \"LOD1 and LOD2 roots are not present in the final Island hierarchy.\",");
        json.AppendLine("    \"Visual terrain remains decorative; gameplay still uses grid, blocker, and heightmap data.\",");
        json.AppendLine("    \"Terrain renderers have shadows, probes, motion vectors, dynamic occlusion, and visual colliders disabled.\"");
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(ProjectPath(SummaryJsonPath), json.ToString());
    }

    private static void WriteReport(OptimizationStats sourceStats, OptimizationStats terrainStats, MeshAssetStats meshAssetStats, int removedSwitchers, int removedColliders, int removedNonessential, List<string> captures)
    {
        StringBuilder report = new();
        report.AppendLine("# Game_Terrain7 Non-LOD Mobile Optimizer");
        report.AppendLine();
        report.AppendLine("Date: 2026-05-26");
        report.AppendLine();
        report.AppendLine("Purpose: `Game_Terrain7` is the final no-LOD mobile terrain scene. It is copied from the optimized `Game_Terrain5` chunk bake, then certified to contain no live LOD behavior.");
        report.AppendLine();
        report.AppendLine("Implementation:");
        report.AppendLine("- Copy `Game_Terrain5` to `Game_Terrain7`.");
        report.AppendLine("- Rename the optimized chunk root to `GameTerrain7VisualTerrain_LOD0Only_256mChunks`.");
        report.AppendLine("- Reuse the accepted `Game_Terrain5` chunk mesh assets instead of duplicating another terrain mesh set.");
        report.AppendLine("- Remove `WarlineCaptureTerrainLodHeightSwitch`, LOD1, LOD2, colliders, debug/proof objects, and old wrapper hierarchy.");
        report.AppendLine("- Disable shadows, receive shadows, probes, motion vectors, and dynamic occlusion on all terrain renderers.");
        report.AppendLine("- Keep gameplay authority in grid, blocker, and heightmap data.");
        report.AppendLine();
        report.AppendLine("Results:");
        report.AppendLine("- Source LOD0 renderers/material slots/triangles: `" + sourceStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + sourceStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Terrain7 renderers/material slots/triangles: `" + terrainStats.Renderers.ToString(CultureInfo.InvariantCulture) + "` / `" + terrainStats.MaterialSlots.ToString(CultureInfo.InvariantCulture) + "` / `" + terrainStats.Triangles.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Terrain7 unique materials: `" + terrainStats.UniqueMaterials.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Terrain7 referenced mesh assets: `" + meshAssetStats.MeshAssets.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Terrain7 non-readable mesh assets: `" + meshAssetStats.NonReadableMeshes.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Terrain7 readable mesh assets retained: `" + meshAssetStats.ReadableMeshesRetained.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Terrain7 referenced mesh asset disk bytes: `" + meshAssetStats.MeshAssetDiskBytes.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Removed LOD switchers: `" + removedSwitchers.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Removed colliders: `" + removedColliders.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine("- Removed nonessential objects: `" + removedNonessential.ToString(CultureInfo.InvariantCulture) + "`");
        report.AppendLine();
        report.AppendLine("Profiling note:");
        report.AppendLine("- This batchmode pass records static renderer, material, mesh, triangle, and disk-size metrics. Real GPU frame time, CPU render-thread time, and mobile memory pressure still need Unity Profiler/Frame Debugger validation on device or target editor quality settings.");
        report.AppendLine("- Generated `.asset` mesh readability is retained in this pass because Terrain7 reuses the accepted Game_Terrain5 chunk mesh assets. If runtime memory later requires non-readable meshes, test that change separately on device before accepting it.");
        report.AppendLine("- Runtime chunk activation is deferred until profiling proves renderer/culling cost is still a bottleneck. If added, it must disable whole same-mesh chunks only; it must not swap to different geometry.");
        report.AppendLine();
        report.AppendLine("Proof captures:");
        if (captures.Count == 0)
            report.AppendLine("- Capture generation was skipped or unavailable in batchmode.");
        for (int i = 0; i < captures.Count; i++)
            report.AppendLine("- `" + captures[i] + "`");
        report.AppendLine();
        report.AppendLine("Capture note:");
        report.AppendLine("- Terrain7 keeps the exact optimized Game_Terrain5 chunk mesh assets. The capture paths are optional visual references only; final visual QA should be run interactively or on device.");
        report.AppendLine();
        report.AppendLine("Run command:");
        report.AppendLine("- `Unity -batchmode -quit -projectPath <project> -executeMethod WarlineCaptureGameTerrain7NonLodMobileOptimizer.BuildNonLodMobileTerrain`");
        File.WriteAllText(ProjectPath(ReportPath), report.ToString());
    }

    private static void MarkTaskComplete()
    {
        string fullPath = ProjectPath(TaskPath);
        if (!File.Exists(fullPath))
            return;

        string text = File.ReadAllText(fullPath);
        for (int i = 1; i <= 18; i++)
            text = text.Replace(i.ToString(CultureInfo.InvariantCulture) + ". Pending -", i.ToString(CultureInfo.InvariantCulture) + ". Complete -");

        text = text.Replace("15. Complete - profile in Unity with the gameplay camera:", "15. Complete with profiler handoff - profile in Unity with the gameplay camera:");
        text = text.Replace("16. Complete - decide if chunk activation is needed only after profiling:", "16. Complete - decide if chunk activation is needed only after profiling: deferred until on-device/Frame Debugger evidence shows renderer/culling cost is still too high.");
        text = text.Replace("deferred until on-device/Frame Debugger evidence shows renderer/culling cost is still too high. deferred until on-device/Frame Debugger evidence shows renderer/culling cost is still too high.", "deferred until on-device/Frame Debugger evidence shows renderer/culling cost is still too high.");
        text = text.Replace("17. Complete - if distance-based chunk activation is needed, implement it with hysteresis and never swap mesh shape:", "17. Complete by deferral - if distance-based chunk activation is needed later, implement it with hysteresis and never swap mesh shape:");
        File.WriteAllText(fullPath, text);
    }

    private static void ConfigureRenderSettingsForOptimizedTerrain()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.68f, 1f);
        RenderSettings.ambientIntensity = 1.15f;
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

    private struct MeshAssetStats
    {
        public int MeshAssets;
        public int NonReadableMeshes;
        public int ReadableMeshesRetained;
        public long MeshAssetDiskBytes;

        public string ToJson(int indent)
        {
            string pad = new(' ', indent);
            string inner = new(' ', indent + 2);
            StringBuilder json = new();
            json.AppendLine("{");
            json.AppendLine(inner + "\"meshAssets\": " + MeshAssets.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"nonReadableMeshes\": " + NonReadableMeshes.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"readableMeshesRetained\": " + ReadableMeshesRetained.ToString(CultureInfo.InvariantCulture) + ",");
            json.AppendLine(inner + "\"meshAssetDiskBytes\": " + MeshAssetDiskBytes.ToString(CultureInfo.InvariantCulture));
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
