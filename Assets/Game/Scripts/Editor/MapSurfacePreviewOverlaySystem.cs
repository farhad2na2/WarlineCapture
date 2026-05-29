#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapSurfacePreviewOverlaySystem
{
    private const int MaxPreviewItems = 2048;

    private static PreviewItem[] previewItems = new PreviewItem[0];
    private static MapSurfaceEditorOverlaySystem.OverlayMode previewMode;
    private static string previewLabel = string.Empty;

    public static bool HasPreview => previewItems.Length > 0;

    static MapSurfacePreviewOverlaySystem()
    {
        SceneView.duringSceneGui += DrawPreview;
        AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
        EditorApplication.quitting += ClearPreview;
    }

    public static void ShowAuthoringPreview(
        MapSurfaceAuthoring authoring,
        MapSurfaceEditorOverlaySystem.OverlayMode mode)
    {
        ClearPreview();
        if (authoring == null)
            return;

        previewMode = mode;
        previewItems = CollectPreviewItems(authoring);
        previewLabel = $"{authoring.name}: {previewItems.Length} cached renderer bounds, no asset saved";
        SceneView.RepaintAll();
    }

    public static void ClearPreview()
    {
        previewItems = new PreviewItem[0];
        previewLabel = string.Empty;
        SceneView.RepaintAll();
    }

    private static PreviewItem[] CollectPreviewItems(MapSurfaceAuthoring authoring)
    {
        var items = new List<PreviewItem>(256);
        MapBakeGroupAuthoring[] groups = authoring.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
        for (int i = 0; i < groups.Length && items.Count < MaxPreviewItems; i++)
        {
            MapBakeGroupAuthoring group = groups[i];
            if (group == null)
                continue;

            Renderer[] renderers = group.GetComponentsInChildren<Renderer>(group.IncludeInactiveChildren);
            for (int rendererIndex = 0; rendererIndex < renderers.Length && items.Count < MaxPreviewItems; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.size.sqrMagnitude <= 0.0001f)
                    continue;

                items.Add(new PreviewItem(bounds, group.Role));
            }
        }

        return items.ToArray();
    }

    private static void DrawPreview(SceneView sceneView)
    {
        if (previewItems.Length == 0)
            return;

        for (int i = 0; i < previewItems.Length; i++)
            DrawPreviewItem(previewItems[i]);

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 420f, 76f), EditorStyles.helpBox);
        GUILayout.Label($"Map Surface Authoring Preview: {previewMode}", EditorStyles.boldLabel);
        GUILayout.Label(previewLabel);
        if (GUILayout.Button("Clear Map Surface Preview"))
            ClearPreview();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private static void DrawPreviewItem(PreviewItem item)
    {
        Color color = ResolveColor(item);
        Bounds bounds = item.Bounds;
        float y = bounds.max.y + 0.08f;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] verts =
        {
            new(min.x, y, min.z),
            new(min.x, y, max.z),
            new(max.x, y, max.z),
            new(max.x, y, min.z)
        };

        Handles.DrawSolidRectangleWithOutline(verts, color, new Color(color.r, color.g, color.b, 0.85f));
    }

    private static Color ResolveColor(PreviewItem item)
    {
        switch (previewMode)
        {
            case MapSurfaceEditorOverlaySystem.OverlayMode.Height:
                return Color.Lerp(new Color(0.05f, 0.25f, 0.9f, 0.18f), new Color(0.9f, 0.85f, 0.1f, 0.24f), Mathf.InverseLerp(0f, 35f, item.Bounds.max.y));
            case MapSurfaceEditorOverlaySystem.OverlayMode.Blocked:
                return item.Role == MapBakeGroupRole.Blocker
                    ? new Color(0.9f, 0.05f, 0.05f, 0.28f)
                    : new Color(0.05f, 0.65f, 0.15f, 0.1f);
            case MapSurfaceEditorOverlaySystem.OverlayMode.RoadBridgeRamp:
                return ResolveRoadColor(item.Role);
            case MapSurfaceEditorOverlaySystem.OverlayMode.Layer:
                return item.Role == MapBakeGroupRole.Bridge
                    ? new Color(0.1f, 0.55f, 1f, 0.25f)
                    : new Color(0.1f, 0.8f, 0.35f, 0.12f);
            case MapSurfaceEditorOverlaySystem.OverlayMode.Slope:
            default:
                return item.Role == MapBakeGroupRole.Blocker
                    ? new Color(0.9f, 0.15f, 0.05f, 0.22f)
                    : new Color(0.1f, 0.7f, 0.2f, 0.12f);
        }
    }

    private static Color ResolveRoadColor(MapBakeGroupRole role)
    {
        switch (role)
        {
            case MapBakeGroupRole.Road:
                return new Color(0.08f, 0.08f, 0.08f, 0.24f);
            case MapBakeGroupRole.Bridge:
                return new Color(0.1f, 0.55f, 1f, 0.28f);
            case MapBakeGroupRole.Ramp:
                return new Color(0.95f, 0.65f, 0.1f, 0.28f);
            case MapBakeGroupRole.Blocker:
                return new Color(0.9f, 0.05f, 0.05f, 0.16f);
            case MapBakeGroupRole.Terrain:
                return new Color(0.05f, 0.5f, 0.12f, 0.1f);
            default:
                return new Color(0.5f, 0.5f, 0.5f, 0.04f);
        }
    }

    private readonly struct PreviewItem
    {
        public readonly Bounds Bounds;
        public readonly MapBakeGroupRole Role;

        public PreviewItem(Bounds bounds, MapBakeGroupRole role)
        {
            Bounds = bounds;
            Role = role;
        }
    }
}
#endif
