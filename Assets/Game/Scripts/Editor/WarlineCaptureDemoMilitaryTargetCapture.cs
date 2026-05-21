#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class WarlineCaptureDemoMilitaryTargetCapture
{
    private const string DemoScenePath = "Assets/Game/Scenes/Demo.unity";
    private const string CaptureRoot = "Design/AgentReports/Captures/GeneratedScenes/GC06_DemoMilitaryTargetReference";

    public static void CaptureDemoMilitaryTarget()
    {
        Directory.CreateDirectory(ProjectPath(CaptureRoot));
        Scene scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        BuildLighting();

        Camera overview = CameraObject("Camera_DemoMilitary_TargetStyleOverview");
        overview.transform.position = new Vector3(-120f, 310f, -210f);
        overview.transform.LookAt(new Vector3(38f, 0f, 105f));
        overview.fieldOfView = 37f;
        Render(overview, ProjectPath(CaptureRoot + "/demo_military_target_style_overview_1920x1080.png"));

        Camera baseClose = CameraObject("Camera_DemoMilitary_BaseClose");
        baseClose.transform.position = new Vector3(-70f, 210f, -85f);
        baseClose.transform.LookAt(new Vector3(46f, 0f, 125f));
        baseClose.fieldOfView = 34f;
        Render(baseClose, ProjectPath(CaptureRoot + "/demo_military_base_close_1920x1080.png"));

        Camera runway = CameraObject("Camera_DemoMilitary_RunwayClose");
        runway.transform.position = new Vector3(-55f, 230f, 45f);
        runway.transform.LookAt(new Vector3(86f, 0f, 245f));
        runway.fieldOfView = 34f;
        Render(runway, ProjectPath(CaptureRoot + "/demo_military_runway_close_1920x1080.png"));

        Debug.Log($"WARLINECAPTURE_DEMO_MILITARY_TARGET_CAPTURED scene={scene.path} captureRoot={CaptureRoot}");
        EditorApplication.Exit(0);
    }

    private static void BuildLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.66f, 0.59f, 0.49f, 1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.55f, 0.47f, 0.36f, 1f);
        RenderSettings.fogDensity = 0.0011f;

        GameObject lightObject = new("DemoMilitaryCapture_KeyLight");
        Light key = lightObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.85f;
        key.color = new Color(1f, 0.88f, 0.66f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.58f;
        key.transform.rotation = Quaternion.Euler(50f, -42f, 0f);
    }

    private static Camera CameraObject(string name)
    {
        GameObject cameraObject = new(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.42f, 0.35f, 0.25f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 4000f;
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return camera;
    }

    private static void Render(Camera camera, string path)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
        camera.targetTexture = texture;
        RenderTexture.active = texture;
        GL.Clear(true, true, camera.backgroundColor);
        camera.Render();
        Texture2D image = new(1920, 1080, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        Object.DestroyImmediate(image);
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        texture.Release();
        Object.DestroyImmediate(texture);
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
    }
}
#endif
