#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapSurfacePreviewProofCapture
{
    private const string MapPrefabPath = "Assets/Game/Prefabs/Maps/Map.prefab";
    private const string MatchScenePath = "Assets/Game/Scenes/Match.unity";
    private const string WalkableOutputPath = "Design/AgentReports/map_surface_walkable_preview.png";
    private const string HeightOutputPath = "Design/AgentReports/map_surface_walkable_height_preview.png";
    private const string BlockedOutputPath = "Design/AgentReports/map_surface_blocked_preview.png";
    private const string TentCampWalkableOutputPath = "Design/AgentReports/map_surface_tent_camp_walkable_preview.png";
    private const string MatchRightWalkableOutputPath = "Design/AgentReports/map_surface_match_right_walkable_preview.png";
    private const int Width = 1920;
    private const int Height = 1080;
    private static readonly Bounds TentCampBounds = new(
        new Vector3(890f, 5f, 115f),
        new Vector3(140f, 40f, 105f));
    private static readonly Bounds MatchRightAreaBounds = new(
        new Vector3(1625f, 5f, 385f),
        new Vector3(900f, 80f, 900f));

    public static void CaptureWalkableHeightPreview()
    {
        CapturePreview(MapSurfaceEditorOverlaySystem.OverlayMode.Height, HeightOutputPath);
    }

    public static void CaptureWalkablePreview()
    {
        CapturePreview(MapSurfaceEditorOverlaySystem.OverlayMode.Walkable, WalkableOutputPath);
    }

    public static void CaptureBlockedPreview()
    {
        CapturePreview(MapSurfaceEditorOverlaySystem.OverlayMode.Blocked, BlockedOutputPath);
    }

    public static void CaptureTentCampWalkablePreview()
    {
        CapturePreview(MapSurfaceEditorOverlaySystem.OverlayMode.Walkable, TentCampWalkableOutputPath, TentCampBounds);
    }

    public static void CaptureMatchRightWalkablePreview()
    {
        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
        try
        {
            MapSurfaceAuthoring authoring = FindAuthoring(scene);
            CapturePreviewFromAuthoring(
                authoring,
                MapSurfaceEditorOverlaySystem.OverlayMode.Walkable,
                MatchRightWalkableOutputPath,
                MatchRightAreaBounds);
        }
        finally
        {
            if (previousScene.IsValid() &&
                !string.IsNullOrEmpty(previousScene.path) &&
                previousScene.path != scene.path)
            {
                EditorSceneManager.OpenScene(previousScene.path, OpenSceneMode.Single);
            }
        }
    }

    private static void CapturePreview(
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        string outputPath)
    {
        CapturePreview(mode, outputPath, null);
    }

    private static void CapturePreview(
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        string outputPath,
        Bounds? cropBounds)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(MapPrefabPath);
        try
        {
            MapSurfaceAuthoring authoring = prefabRoot.GetComponent<MapSurfaceAuthoring>();
            if (authoring == null)
                authoring = prefabRoot.GetComponentInChildren<MapSurfaceAuthoring>(true);

            if (authoring == null)
                throw new MissingReferenceException($"No MapSurfaceAuthoring found in {MapPrefabPath}.");

            CapturePreviewFromAuthoring(authoring, mode, outputPath, cropBounds);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void CapturePreviewFromAuthoring(
        MapSurfaceAuthoring authoring,
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        string outputPath,
        Bounds? cropBounds)
    {
        MapSurfacePreviewOverlaySystem.PreviewMeshItem[] meshes =
            MapSurfacePreviewOverlaySystem.BuildPreviewMeshes(authoring, mode);

        MapSurfacePreviewOverlaySystem.CalculateHeightRange(meshes, out float minHeight, out float maxHeight);
        Texture2D texture = RenderTopDown(meshes, mode, minHeight, maxHeight, cropBounds);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        string cropSummary = cropBounds.HasValue ? $" crop={cropBounds.Value.center}/{cropBounds.Value.size}" : string.Empty;
        Debug.Log($"[MapSurfacePreviewProof] Wrote {outputPath} mode={mode} meshes={meshes.Length} blockers={CountRole(meshes, MapBakeGroupRole.Blocker)} height={minHeight:0.##}-{maxHeight:0.##}{cropSummary}");
    }

    private static MapSurfaceAuthoring FindAuthoring(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            MapSurfaceAuthoring authoring = roots[i].GetComponentInChildren<MapSurfaceAuthoring>(true);
            if (authoring != null)
                return authoring;
        }

        throw new MissingReferenceException($"No MapSurfaceAuthoring found in {scene.path}.");
    }

    private static Texture2D RenderTopDown(
        MapSurfacePreviewOverlaySystem.PreviewMeshItem[] meshes,
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        float minHeight,
        float maxHeight,
        Bounds? cropBounds)
    {
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        Color32 background = new(20, 24, 28, 255);
        Color32[] pixels = new Color32[Width * Height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = background;

        if (meshes == null || meshes.Length == 0)
        {
            texture.SetPixels32(pixels);
            texture.Apply(false);
            return texture;
        }

        Bounds bounds = cropBounds ?? meshes[0].Bounds;
        if (!cropBounds.HasValue)
        {
            for (int i = 1; i < meshes.Length; i++)
                bounds.Encapsulate(meshes[i].Bounds);
        }

        float scale = Mathf.Min((Width - 80f) / Mathf.Max(1f, bounds.size.x), (Height - 80f) / Mathf.Max(1f, bounds.size.z));
        Vector2 offset = new(
            (Width - bounds.size.x * scale) * 0.5f - bounds.min.x * scale,
            (Height - bounds.size.z * scale) * 0.5f - bounds.min.z * scale);

        for (int i = 0; i < meshes.Length; i++)
            RasterizeMesh(meshes[i], mode, minHeight, maxHeight, scale, offset, cropBounds, pixels);

        texture.SetPixels32(pixels);
        texture.Apply(false);
        return texture;
    }

    private static void RasterizeMesh(
        MapSurfacePreviewOverlaySystem.PreviewMeshItem item,
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        float minHeight,
        float maxHeight,
        float scale,
        Vector2 offset,
        Bounds? cropBounds,
        Color32[] pixels)
    {
        Mesh mesh = item.Mesh;
        if (mesh == null)
            return;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        if (vertices == null || triangles == null || vertices.Length == 0 || triangles.Length < 3)
            return;

        Matrix4x4 matrix = MapSurfacePreviewOverlaySystem.ResolveDrawMatrixForCapture(item, mode);
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            Vector3 a = matrix.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 b = matrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 c = matrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);
            if (cropBounds.HasValue && !IntersectsTriangleXZ(cropBounds.Value, a, b, c))
                continue;

            DrawTriangle(a, b, c, item, mode, minHeight, maxHeight, scale, offset, pixels);
        }
    }

    private static bool IntersectsTriangleXZ(Bounds bounds, Vector3 a, Vector3 b, Vector3 c)
    {
        float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
        float maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
        return minX <= bounds.max.x &&
               maxX >= bounds.min.x &&
               minZ <= bounds.max.z &&
               maxZ >= bounds.min.z;
    }

    private static void DrawTriangle(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        MapSurfacePreviewOverlaySystem.PreviewMeshItem item,
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        float minHeight,
        float maxHeight,
        float scale,
        Vector2 offset,
        Color32[] pixels)
    {
        Vector2 pa = Project(a, scale, offset);
        Vector2 pb = Project(b, scale, offset);
        Vector2 pc = Project(c, scale, offset);
        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pa.x, Mathf.Min(pb.x, pc.x))), 0, Width - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pa.x, Mathf.Max(pb.x, pc.x))), 0, Width - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pa.y, Mathf.Min(pb.y, pc.y))), 0, Height - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pa.y, Mathf.Max(pb.y, pc.y))), 0, Height - 1);
        float area = Edge(pa, pb, pc);
        if (Mathf.Abs(area) < 0.0001f)
            return;

        Color color = MapSurfacePreviewOverlaySystem.ResolveColorForCapture(item, mode, minHeight, maxHeight);
        Color32 color32 = color;
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float w0 = Edge(pb, pc, p);
                float w1 = Edge(pc, pa, p);
                float w2 = Edge(pa, pb, p);
                if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                    BlendPixel(pixels, x, y, color32);
            }
        }
    }

    private static Vector2 Project(Vector3 world, float scale, Vector2 offset)
    {
        return new Vector2(world.x * scale + offset.x, Height - (world.z * scale + offset.y));
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 c)
    {
        return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
    }

    private static void BlendPixel(Color32[] pixels, int x, int y, Color32 source)
    {
        int index = x + y * Width;
        Color32 destination = pixels[index];
        float alpha = source.a / 255f;
        pixels[index] = new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * alpha + destination.r * (1f - alpha)), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * alpha + destination.g * (1f - alpha)), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * alpha + destination.b * (1f - alpha)), 0, 255),
            255);
    }

    private static int CountRole(MapSurfacePreviewOverlaySystem.PreviewMeshItem[] meshes, MapBakeGroupRole role)
    {
        int count = 0;
        if (meshes == null)
            return 0;

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i].Role == role)
                count++;
        }

        return count;
    }
}
#endif
