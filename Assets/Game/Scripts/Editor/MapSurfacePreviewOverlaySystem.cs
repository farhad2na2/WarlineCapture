#if UNITY_EDITOR
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapSurfacePreviewOverlaySystem
{
    private static readonly MapSurfaceEditorOverlaySystem OverlaySystem = new();

    private static BlobAssetReference<MapSurfaceBlob> previewBlob;
    private static MapSurfaceComponent previewSurface;
    private static GridConfig previewGrid;
    private static MapSurfaceEditorOverlaySystem.OverlayMode previewMode;
    private static int previewStride = 16;
    private static string previewLabel = string.Empty;

    public static bool HasPreview => previewBlob.IsCreated;

    static MapSurfacePreviewOverlaySystem()
    {
        SceneView.duringSceneGui += DrawPreview;
        AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
        EditorApplication.quitting += ClearPreview;
    }

    public static void ShowPreview(
        MapSurfaceBakeRequest request,
        BlobAssetReference<MapSurfaceBlob> surfaceBlob,
        MapSurfaceEditorOverlaySystem.OverlayMode mode,
        int cellStride,
        string label)
    {
        ClearPreview();
        if (!surfaceBlob.IsCreated)
            return;

        previewBlob = surfaceBlob;
        previewMode = mode;
        previewStride = math.max(1, cellStride);
        previewLabel = label ?? string.Empty;
        previewSurface = new MapSurfaceComponent
        {
            SurfaceBlob = previewBlob,
            GridOrigin = request.GridOrigin,
            CellSize = request.CellSize,
            Dimensions = request.Dimensions,
            HasSurfaceData = 1,
            HasLayeredCells = 0,
            HasRoadSurfaces = 1,
            HasBridgeSurfaces = 1
        };
        previewGrid = new GridConfig
        {
            Origin = request.GridOrigin,
            CellSize = request.CellSize,
            Width = request.Dimensions.x,
            Height = request.Dimensions.y
        };
        SceneView.RepaintAll();
    }

    public static void ClearPreview()
    {
        if (previewBlob.IsCreated)
            previewBlob.Dispose();

        previewBlob = default;
        previewSurface = default;
        previewGrid = default;
        previewLabel = string.Empty;
        SceneView.RepaintAll();
    }

    private static void DrawPreview(SceneView sceneView)
    {
        if (!previewBlob.IsCreated)
            return;

        OverlaySystem.DrawOverlay(previewSurface, previewGrid, previewMode, previewStride);
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 360f, 76f), EditorStyles.helpBox);
        GUILayout.Label($"Map Surface Preview: {previewMode}", EditorStyles.boldLabel);
        GUILayout.Label($"{previewLabel} stride={previewStride} no asset saved");
        if (GUILayout.Button("Clear Map Surface Preview"))
            ClearPreview();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
#endif
