#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class MapSurfaceDebugCaptureSystem
{
    private const string CapturePath = "Design/AgentReports/map_surface_debug_capture.md";

    [MenuItem("Game/Map Surface/Capture Selected Surface Summary")]
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
        builder.AppendLine($"Compact Payload Bytes: {data.CompressedPayloadBytes}");
        builder.AppendLine($"Uncompressed Payload Bytes: {data.UncompressedPayloadBytes}");
        AppendBakeGroups(builder, authoring);
        return builder.ToString();
    }

    private static void AppendBakeGroups(StringBuilder builder, MapSurfaceAuthoring authoring)
    {
        MapBakeGroupAuthoring[] groups = authoring.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
        builder.AppendLine($"Bake Groups: {groups.Length}");
        for (int i = 0; i < groups.Length; i++)
        {
            MapBakeGroupAuthoring group = groups[i];
            if (group == null)
                continue;

            builder.AppendLine($"- {group.name}: {group.Role}");
        }
    }
}
#endif
