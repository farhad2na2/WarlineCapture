#if UNITY_EDITOR
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WarlineCaptureDemoSceneCameraSweep
{
    private const string ScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string OutputRoot = "Design/AgentReports/Captures/DemoSceneCameraSweep";

    public static void Run()
    {
        Directory.CreateDirectory(OutputRoot);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Bounds bounds = new(new Vector3(55f, 25f, 180f), new Vector3(760f, 120f, 980f));
        Camera camera = FindOrCreateCamera();
        ConfigureTopDown(camera, bounds);

        CaptureZone(camera, "town_close", new Vector3(-20f, 25f, -70f), 120f);
        CaptureZone(camera, "military_base_close", new Vector3(40f, 25f, 135f), 130f);
        CaptureZone(camera, "base_gate_highway_close", new Vector3(0f, 25f, 80f), 105f);
        CaptureZone(camera, "industrial_vehicle_close", new Vector3(95f, 25f, 230f), 130f);
        CaptureZone(camera, "road_spine_close", new Vector3(-5f, 25f, 20f), 150f);

        File.WriteAllText(
            Path.Combine(OutputRoot, "sweep_manifest.txt"),
            $"scene={ScenePath}\n" +
            $"boundsCenter={Format(bounds.center)}\n" +
            $"boundsSize={Format(bounds.size)}\n" +
            "zones=town_close,military_base_close,base_gate_highway_close,industrial_vehicle_close,road_spine_close\n" +
            "Camera captures close top-down views of authored town/base clusters, not the full desert terrain.\n");

        Debug.Log($"WARLINECAPTURE_DEMO_CAMERA_SWEEP output={OutputRoot} boundsCenter={Format(bounds.center)} boundsSize={Format(bounds.size)}");
        EditorApplication.Exit(0);
    }

    private static void CaptureZone(Camera camera, string name, Vector3 center, float orthographicSize)
    {
        camera.transform.position = new Vector3(center.x, center.y + 180f, center.z);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.orthographicSize = orthographicSize;
        Render(camera, Path.Combine(OutputRoot, $"demo_topdown_{name}.png"), 1800, 1400);
    }

    private static Camera FindOrCreateCamera()
    {
        Camera camera = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include)
            .FirstOrDefault(c => c.name == "Main Camera");
        if (camera != null)
            return camera;

        GameObject cameraObject = new("WarlineCaptureDemoSweepCamera");
        return cameraObject.AddComponent<Camera>();
    }

    private static void ConfigureTopDown(Camera camera, Bounds bounds)
    {
        camera.enabled = true;
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = Mathf.Max(1000f, bounds.size.y + 500f);
        camera.cullingMask = ~0;
        camera.allowHDR = false;
        camera.allowMSAA = false;
    }

    private static void Render(Camera camera, string path, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(width, height, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
    }

    private static Bounds CalculateUsefulBounds(Renderer[] renderers)
    {
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || IsBackground(renderer))
                continue;

            Bounds rendererBounds = renderer.bounds;
            if (rendererBounds.size.x <= 0f || rendererBounds.size.z <= 0f)
                continue;

            if (!hasBounds)
            {
                bounds = rendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rendererBounds);
            }
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, new Vector3(100f, 50f, 100f));
    }

    private static Bounds CalculatePrefabDenseBounds()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (Transform transform in transforms)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject);
            if (source == null)
                continue;

            string path = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(path))
                continue;

            string role = Classify(path);
            if (role == "environment" || role == "building_detail" || role == "utility" || role == "prop")
                continue;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
            Bounds instanceBounds = CalculateUsefulBounds(renderers);
            if (instanceBounds.size == Vector3.zero || IsBackgroundBounds(instanceBounds))
                continue;

            if (!hasBounds)
            {
                bounds = instanceBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(instanceBounds);
            }
        }

        if (!hasBounds)
            return new Bounds(Vector3.zero, new Vector3(300f, 100f, 300f));

        bounds.Expand(new Vector3(70f, 0f, 70f));
        return bounds;
    }

    private static bool IsBackgroundBounds(Bounds bounds)
    {
        return bounds.center.y > 500f || bounds.size.x > 1000f || bounds.size.z > 1000f;
    }

    private static bool IsBackground(Renderer renderer)
    {
        string name = renderer.name.ToLowerInvariant();
        Bounds bounds = renderer.bounds;
        return name.Contains("sky") ||
            name.Contains("cloud") ||
            name.Contains("skydome") ||
            bounds.center.y > 500f ||
            bounds.size.x > 1200f ||
            bounds.size.z > 1200f;
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

    private static string Format(Vector3 value)
    {
        return $"({F(value.x)}, {F(value.y)}, {F(value.z)})";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
#endif
