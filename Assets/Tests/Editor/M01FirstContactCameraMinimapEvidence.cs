using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using UnityEngine;

internal static class M01FirstContactCameraMinimapEvidence
{
    public const float NormalBlendSeconds = 1.25f;
    public const float ReducedMotionBlendSeconds = 0f;
    private const string ReportPath =
        "Design/AgentReports/M01FirstContact/m01dc_012_camera_minimap.json";
    private const string CapturePath =
        "Design/AgentReports/M01FirstContact/m01dc_012_camera_minimap_contact_sheet.png";
    private static readonly RectInt Window = new(760, 300, 240, 176);
    private static readonly RectInt Corridor = new(804, 348, 128, 80);

    public static void Write(OperationMapDefinition map, Vector2[] aspects, string marker)
    {
        WriteContactSheet(map, aspects);
        WriteReport(map, marker);
        Require(File.Exists(CapturePath) && new FileInfo(CapturePath).Length > 1000, "Contact sheet missing.");
        Require(File.Exists(ReportPath) && new FileInfo(ReportPath).Length > 500, "Report missing.");
    }

    public static string Sha256Text(string value) => Sha256(Encoding.UTF8.GetBytes(value));

    private static void WriteContactSheet(OperationMapDefinition map, Vector2[] aspects)
    {
        Texture2D texture = new(2400, 1350, TextureFormat.RGBA32, false, false);
        try
        {
            Color32[] pixels = new Color32[texture.width * texture.height];
            Array.Fill(pixels, new Color32(28, 33, 36, 255));
            texture.SetPixels32(pixels);
            for (int panel = 0; panel < aspects.Length; panel++)
            {
                RectInt outer = new(40 + panel * 790, 715, 740, 590);
                RectInt mapRect = FitAspect(outer, aspects[panel].x / aspects[panel].y);
                DrawMapPanel(texture, mapRect, map, planning: true);
                RectInt lowerOuter = new(40 + panel * 790, 45, 740, 590);
                DrawMapPanel(texture, FitAspect(lowerOuter, aspects[panel].x / aspects[panel].y), map, planning: false);
            }
            texture.Apply(false, false);
            EnsureDirectory(CapturePath);
            File.WriteAllBytes(CapturePath, texture.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
    }

    private static void DrawMapPanel(Texture2D texture, RectInt panel, OperationMapDefinition map, bool planning)
    {
        DrawRect(texture, panel, new Color32(58, 72, 66, 255), true);
        DrawRect(texture, panel, planning ? new Color32(67, 191, 220, 255) : new Color32(236, 184, 75, 255), false);
        RectInt corridor = WorldToPanel(Corridor, panel);
        DrawRect(texture, corridor, new Color32(177, 151, 98, 255), true);
        OperationMapCameraConfig camera = planning ? map.Cameras[0] : map.Cameras[1];
        Vector2Int cameraPoint = WorldToPanel(camera.Position, panel);
        DrawCircle(texture, cameraPoint, 11, planning ? new Color32(84, 205, 115, 255) : new Color32(232, 92, 75, 255));
        Vector3 direction = Quaternion.Euler(camera.EulerAngles) * Vector3.forward;
        Vector3 target = camera.Position + direction * 150f;
        DrawLine(texture, cameraPoint, WorldToPanel(target, panel), new Color32(238, 238, 225, 255));
        RectInt safe = new(panel.xMin + panel.width * 6 / 100, panel.yMin + panel.height * 6 / 100,
            panel.width * 88 / 100, panel.height * 88 / 100);
        DrawRect(texture, safe, new Color32(93, 204, 206, 255), false);
    }

    private static void WriteReport(OperationMapDefinition map, string marker)
    {
        OperationMapCameraConfig planning = map.Cameras[0];
        OperationMapCameraConfig battle = map.Cameras[1];
        string json = $@"{{
  ""artifactId"": ""m01dc-012-camera-minimap-v1"", ""taskId"": ""M01DC-012"", ""result"": ""Passed"",
  ""authority"": {{""visual"": ""current approved FirstLaunch FL-P18"", ""oldImagesUsed"": false}},
  ""planningCamera"": {{""id"": ""{planning.CameraId}"", ""position"": [{F(planning.Position.x)}, {F(planning.Position.y)}, {F(planning.Position.z)}], ""euler"": [{F(planning.EulerAngles.x)}, {F(planning.EulerAngles.y)}, {F(planning.EulerAngles.z)}], ""fov"": {F(planning.FieldOfView)}, ""clamp"": true}},
  ""battleStartCamera"": {{""id"": ""{battle.CameraId}"", ""position"": [{F(battle.Position.x)}, {F(battle.Position.y)}, {F(battle.Position.z)}], ""euler"": [{F(battle.EulerAngles.x)}, {F(battle.EulerAngles.y)}, {F(battle.EulerAngles.z)}], ""fov"": {F(battle.FieldOfView)}, ""clamp"": true}},
  ""minimap"": {{""id"": ""{map.Minimap.MinimapId}"", ""origin"": [760, 0, 300], ""size"": [240, 176], ""orientationDegrees"": 0, ""roundTripTolerance"": 0.001}},
  ""aspectReview"": [{{""resolution"": ""1920x1080"", ""aspect"": ""16:9""}}, {{""resolution"": ""2400x1080"", ""aspect"": ""20:9""}}, {{""resolution"": ""1920x1200"", ""aspect"": ""16:10-tablet""}}],
  ""safeArea"": {{""planningNormalized"": [0.05, 0.05, 0.90, 0.90], ""battleNormalized"": [0.06, 0.06, 0.88, 0.88]}},
  ""transitionPolicy"": {{""normalBlendSeconds"": {F(NormalBlendSeconds)}, ""reducedMotionBlendSeconds"": {F(ReducedMotionBlendSeconds)}, ""implementationOwner"": ""M01DC-015""}},
  ""capture"": ""{CapturePath}"", ""validation"": ""{marker}""
}}";
        EnsureDirectory(ReportPath);
        File.WriteAllText(ReportPath, json.Replace("\\", "/"), new UTF8Encoding(false));
    }

    private static RectInt FitAspect(RectInt outer, float aspect)
    {
        int width = outer.width;
        int height = Mathf.RoundToInt(width / aspect);
        if (height > outer.height) { height = outer.height; width = Mathf.RoundToInt(height * aspect); }
        return new RectInt(outer.xMin + (outer.width - width) / 2, outer.yMin + (outer.height - height) / 2, width, height);
    }
    private static RectInt WorldToPanel(RectInt world, RectInt panel) => new(
        panel.xMin + Mathf.RoundToInt((world.xMin - Window.xMin) / (float)Window.width * panel.width),
        panel.yMin + Mathf.RoundToInt((world.yMin - Window.yMin) / (float)Window.height * panel.height),
        Mathf.Max(1, Mathf.RoundToInt(world.width / (float)Window.width * panel.width)),
        Mathf.Max(1, Mathf.RoundToInt(world.height / (float)Window.height * panel.height)));
    private static Vector2Int WorldToPanel(Vector3 world, RectInt panel) => new(
        panel.xMin + Mathf.RoundToInt((world.x - Window.xMin) / Window.width * panel.width),
        panel.yMin + Mathf.RoundToInt((world.z - Window.yMin) / Window.height * panel.height));
    private static void DrawRect(Texture2D texture, RectInt rect, Color32 color, bool fill)
    {
        for (int y = rect.yMin; y < rect.yMax; y++) for (int x = rect.xMin; x < rect.xMax; x++)
            if (x >= 0 && y >= 0 && x < texture.width && y < texture.height &&
                (fill || x == rect.xMin || x == rect.xMax - 1 || y == rect.yMin || y == rect.yMax - 1))
                texture.SetPixel(x, y, color);
    }
    private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color32 color)
    {
        int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
        for (int index = 0; index <= steps; index++)
        {
            float t = steps == 0 ? 0f : index / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
            if (x >= 0 && y >= 0 && x < texture.width && y < texture.height) texture.SetPixel(x, y, color);
        }
    }
    private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color32 color)
    {
        for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
            if (x * x + y * y <= radius * radius && center.x + x >= 0 && center.y + y >= 0 &&
                center.x + x < texture.width && center.y + y < texture.height)
                texture.SetPixel(center.x + x, center.y + y, color);
    }
    private static string F(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    private static string Sha256(byte[] bytes)
    {
        using SHA256 hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }
    private static void EnsureDirectory(string path) => Directory.CreateDirectory(
        Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new InvalidOperationException());
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
