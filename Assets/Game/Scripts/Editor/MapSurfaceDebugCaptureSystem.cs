#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MapSurfaceDebugCaptureSystem
{
    private const string CapturePath = "Design/AgentReports/map_surface_debug_capture.md";

    [MenuItem("WarlineCapture/Map Surface/Capture Selected Surface Summary")]
    public static void CaptureSelectedSurfaceSummary()
    {
        MapSurfaceAuthoring authoring = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<MapSurfaceAuthoring>()
            : null;

        if (authoring == null || authoring.BakedSurfaceData == null)
        {
            Debug.LogWarning("[MapSurfaceCapture] Select a MapSurfaceAuthoring with baked surface data before capturing.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(CapturePath));
        File.WriteAllText(CapturePath, BuildSummary(authoring, authoring.BakedSurfaceData));
        AssetDatabase.Refresh();
        Debug.Log($"[MapSurfaceCapture] Wrote {CapturePath}");
    }

    internal static string BuildSummary(MapSurfaceAuthoring authoring, MapSurfaceDataAsset data)
    {
        var builder = new StringBuilder(512);
        builder.AppendLine("# Map Surface Debug Capture");
        builder.AppendLine();
        builder.AppendLine($"Authoring: {authoring.name}");
        builder.AppendLine($"Grid Origin: {data.GridOrigin}");
        builder.AppendLine($"Cell Size: {data.CellSize}");
        builder.AppendLine($"Dimensions: {data.Dimensions}");
        builder.AppendLine($"Generated Flat Equivalent: {data.GeneratedFlatEquivalent}");
        builder.AppendLine($"Surface Count: {data.SurfaceCount}");
        builder.AppendLine($"Connection Count: {data.ConnectionCount}");
        builder.AppendLine($"Terrain Root: {FormatTransformName(authoring.TerrainRoot)}");
        builder.AppendLine($"Road Root: {FormatTransformName(authoring.RoadRoot)}");
        builder.AppendLine($"Bridge Root: {FormatTransformName(authoring.BridgeRoot)}");
        builder.AppendLine($"Ramp Root: {FormatTransformName(authoring.RampRoot)}");
        return builder.ToString();
    }

    private static string FormatTransformName(Transform target)
    {
        return target != null ? target.name : "None";
    }
}
#endif
