#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WarlineCaptureDemoSceneVisualAudit
{
    private const string ScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string OutputRoot = "Design/AgentReports/Captures/DemoSceneVisualAudit";

    public static void Run()
    {
        Directory.CreateDirectory(OutputRoot);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        Bounds contentBounds = CalculateContentBounds(renderers);
        CaptureTopDown(contentBounds);
        CapturePerspective(contentBounds);
        WriteInstanceMap(contentBounds);

        Debug.Log($"WARLINECAPTURE_DEMO_VISUAL_AUDIT output={OutputRoot} boundsCenter={Format(contentBounds.center)} boundsSize={Format(contentBounds.size)}");
        EditorApplication.Exit(0);
    }

    private static void CaptureTopDown(Bounds bounds)
    {
        GameObject cameraObject = new("WarlineCaptureDemoTopDownAuditCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.08f;
        camera.transform.position = new Vector3(bounds.center.x, bounds.max.y + 220f, bounds.center.z);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 2000f;
        Render(camera, Path.Combine(OutputRoot, "demo_topdown_2048.png"), 2048, 2048);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    private static void CapturePerspective(Bounds bounds)
    {
        Camera source = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
            .FirstOrDefault(c => c.name == "Main Camera") ??
            UnityEngine.Object.FindAnyObjectByType<Camera>();
        if (source != null)
            Render(source, Path.Combine(OutputRoot, "demo_existing_camera_1920x1080.png"), 1920, 1080);

        GameObject cameraObject = new("WarlineCaptureDemoIsoAuditCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 45f;
        Vector3 offset = new(bounds.size.x * 0.42f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.48f, -bounds.size.z * 0.42f);
        camera.transform.position = bounds.center + offset;
        camera.transform.LookAt(bounds.center);
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 4000f;
        Render(camera, Path.Combine(OutputRoot, "demo_isometric_overview_1920x1080.png"), 1920, 1080);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        RenderTexture previous = camera.targetTexture;
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = texture;
        camera.Render();
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = texture;
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = active;
        camera.targetTexture = previous;
        UnityEngine.Object.DestroyImmediate(image);
        texture.Release();
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void WriteInstanceMap(Bounds contentBounds)
    {
        Dictionary<string, int> roleCounts = new(StringComparer.Ordinal);
        List<InstanceInfo> instances = new();
        foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
            if (source == null)
                continue;

            string path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path))
                continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = CalculateContentBounds(renderers);
            if (bounds.size == Vector3.zero || IsHugeBackground(bounds))
                continue;

            string role = Classify(path);
            roleCounts[role] = roleCounts.TryGetValue(role, out int count) ? count + 1 : 1;
            instances.Add(new InstanceInfo
            {
                Name = transform.name,
                Path = path,
                Role = role,
                Position = transform.position,
                BoundsCenter = bounds.center,
                BoundsSize = bounds.size
            });
        }

        instances = instances
            .GroupBy(i => $"{i.Path}|{i.BoundsCenter.x:0.###}|{i.BoundsCenter.z:0.###}", StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(i => i.Role, StringComparer.Ordinal)
            .ThenBy(i => i.Path, StringComparer.Ordinal)
            .ToList();

        StringBuilder json = new();
        json.AppendLine("{");
        json.AppendLine($"  \"scene\": \"{ScenePath}\",");
        json.AppendLine($"  \"contentBoundsCenter\": {JsonVector(contentBounds.center)},");
        json.AppendLine($"  \"contentBoundsSize\": {JsonVector(contentBounds.size)},");
        json.AppendLine("  \"roleCounts\": {");
        int roleIndex = 0;
        foreach (KeyValuePair<string, int> pair in roleCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            json.Append($"    \"{pair.Key}\": {pair.Value}");
            json.AppendLine(roleIndex == roleCounts.Count - 1 ? string.Empty : ",");
            roleIndex++;
        }
        json.AppendLine("  },");
        json.AppendLine("  \"instances\": [");
        for (int i = 0; i < instances.Count; i++)
        {
            InstanceInfo instance = instances[i];
            json.AppendLine("    {");
            json.AppendLine($"      \"name\": {Json(instance.Name)},");
            json.AppendLine($"      \"path\": {Json(instance.Path)},");
            json.AppendLine($"      \"role\": {Json(instance.Role)},");
            json.AppendLine($"      \"position\": {JsonVector(instance.Position)},");
            json.AppendLine($"      \"boundsCenter\": {JsonVector(instance.BoundsCenter)},");
            json.AppendLine($"      \"boundsSize\": {JsonVector(instance.BoundsSize)}");
            json.AppendLine(i == instances.Count - 1 ? "    }" : "    },");
        }
        json.AppendLine("  ]");
        json.AppendLine("}");
        File.WriteAllText(Path.Combine(OutputRoot, "demo_instance_role_map.json"), json.ToString(), Encoding.UTF8);
    }

    private static Bounds CalculateContentBounds(Renderer[] renderers)
    {
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || IsHugeBackground(renderer.bounds) || IsBackgroundName(renderer.name))
                continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
    }

    private static bool IsHugeBackground(Bounds bounds)
    {
        return bounds.size.x > 1200f || bounds.size.y > 1200f || bounds.size.z > 1200f || bounds.center.y > 500f;
    }

    private static bool IsBackgroundName(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("sky") || lower.Contains("cloud") || lower.Contains("skydome");
    }

    private static string Classify(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("window") || lower.Contains("door") || lower.Contains("shutter") || lower.Contains("clothcover"))
            return "building_detail";
        if (lower.Contains("/vehicles/") || lower.Contains("_veh_"))
            return lower.Contains("destroyed") ? "vehicle_destroyed" : "vehicle";
        if (lower.Contains("/buildings/") || lower.Contains("_bld_"))
            return lower.Contains("destroyed") ? "building_destroyed" : "building";
        if (lower.Contains("road") || lower.Contains("sidewalk") || lower.Contains("runway"))
            return "road";
        if (lower.Contains("barrier") || lower.Contains("fence") || lower.Contains("sandbag") || lower.Contains("cover"))
            return "cover";
        if (lower.Contains("debris") || lower.Contains("rubble") || lower.Contains("rubbish") || lower.Contains("crater") || lower.Contains("blood") || lower.Contains("shell"))
            return "debris";
        if (lower.Contains("soldier") || lower.Contains("human_") || lower.Contains("/prefabs/weapons/"))
            return "soldier_or_weapon";
        if (lower.Contains("pipeline") || lower.Contains("fueltank") || lower.Contains("fuel") || lower.Contains("gaspump"))
            return "industrial";
        if (lower.Contains("powerline") || lower.Contains("powerpole") || lower.Contains("road_light"))
            return "utility";
        if (lower.Contains("/environment/") || lower.Contains("_env_"))
            return "environment";
        if (lower.Contains("/props/") || lower.Contains("_prop_") || lower.Contains("_item_"))
            return "prop";
        return "uncategorized";
    }

    private static string Json(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string JsonVector(Vector3 value)
    {
        return $"[{F(value.x)},{F(value.y)},{F(value.z)}]";
    }

    private static string Format(Vector3 value)
    {
        return $"({F(value.x)}, {F(value.y)}, {F(value.z)})";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private sealed class InstanceInfo
    {
        public string Name;
        public string Path;
        public string Role;
        public Vector3 Position;
        public Vector3 BoundsCenter;
        public Vector3 BoundsSize;
    }
}
#endif
