#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AirMissileLauncherVisualProofCapture
{
    private const string AirLauncherPrefabPath = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Missle_Launcher_Air.prefab";
    private const string OutputPath = "/private/tmp/warline_air_missile_launcher_visual_proof.png";
    private const int CaptureSize = 1024;

    public static void Run()
    {
        try
        {
            string output = CaptureProof();
            Debug.Log($"[AirMissileLauncherVisualProof] PASS output={output}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AirMissileLauncherVisualProof] FAIL {ex}");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }

    private static string CaptureProof()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AirLauncherPrefabPath);
        Require(prefab != null, $"Missing prefab at {AirLauncherPrefabPath}.");

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.74f, 0.78f, 0.82f, 1f);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Require(instance != null, $"Could not instantiate {AirLauncherPrefabPath}.");
        instance.name = prefab.name + "_AirMissileVisualProof";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        PoseLauncherVisuals(instance);
        Bounds bounds = CalculateRenderableBounds(instance);
        Require(bounds.size.sqrMagnitude > 0.001f, "Air launcher proof instance has no renderable bounds.");
        CenterInstance(instance, bounds);
        bounds = CalculateRenderableBounds(instance);

        GameObject keyObject = new("Air Missile Proof Key Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.05f;
        key.color = new Color(1f, 0.93f, 0.82f, 1f);
        key.shadows = LightShadows.None;
        keyObject.transform.rotation = Quaternion.Euler(48f, -38f, 0f);

        GameObject fillObject = new("Air Missile Proof Fill Light");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.45f;
        fill.color = new Color(0.65f, 0.76f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(28f, 130f, 0f);

        GameObject cameraObject = new("Air Missile Proof Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.07f, 1f);
        camera.orthographic = true;
        camera.aspect = 1f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 250f;
        camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.58f;

        Vector3 focus = bounds.center + Vector3.up * Mathf.Max(0.2f, bounds.size.y * 0.08f);
        Vector3 viewDirection = new Vector3(0.7f, 0.48f, 0.7f).normalized;
        camera.transform.position = focus + viewDirection * Mathf.Max(18f, bounds.extents.magnitude * 2.3f);
        camera.transform.LookAt(focus);

        RenderTexture target = new(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            name = "AirMissileLauncherVisualProof"
        };
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();

            texture = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0);
            texture.Apply(false, false);
            Require(HasVisibleModelPixels(texture, camera.backgroundColor), "Air launcher proof capture is blank.");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            return OutputPath;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (texture != null)
                Object.DestroyImmediate(texture);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }

    private static void PoseLauncherVisuals(GameObject instance)
    {
        UnitGridAuthoring authoring = instance.GetComponent<UnitGridAuthoring>();
        Require(authoring != null, "Air launcher proof instance is missing UnitGridAuthoring.");

        SerializedObject serialized = new(authoring);
        Transform turret = GetReference<Transform>(serialized, "airMissileLauncherTurret");
        Require(turret != null, "Air launcher turret reference is not serialized.");
        turret.localRotation *= Quaternion.Euler(0f, 38f, 0f);

        SerializedProperty missiles = serialized.FindProperty("airMissileLauncherMissiles");
        Require(missiles != null && missiles.arraySize > 0, "Air launcher missile slot array is not serialized.");
        Transform missile = missiles.GetArrayElementAtIndex(0).objectReferenceValue as Transform;
        Require(missile != null, "Air launcher first missile slot is not serialized.");
        missile.SetParent(instance.transform, true);
        Vector3 launchDirection = (turret.forward + turret.right * 0.35f + Vector3.up * 0.42f).normalized;
        missile.position = turret.position + turret.forward * 8.2f + Vector3.up * 5.4f + turret.right * 2.2f;
        missile.rotation = Quaternion.LookRotation(launchDirection, Vector3.up);
        missile.localScale *= 1.12f;
    }

    private static void CenterInstance(GameObject instance, Bounds bounds)
    {
        Vector3 offset = new(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        instance.transform.position += offset;
    }

    private static Bounds CalculateRenderableBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
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

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static bool HasVisibleModelPixels(Texture2D texture, Color background)
    {
        Color32[] pixels = texture.GetPixels32();
        Color32 bg = background;
        int differingPixels = 0;
        for (int i = 0; i < pixels.Length; i += 8)
        {
            Color32 pixel = pixels[i];
            int diff = Mathf.Abs(pixel.r - bg.r) + Mathf.Abs(pixel.g - bg.g) + Mathf.Abs(pixel.b - bg.b);
            if (diff <= 18)
                continue;

            differingPixels++;
            if (differingPixels > 1200)
                return true;
        }

        return false;
    }

    private static T GetReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Require(property != null, $"Missing serialized property {propertyName}.");
        return property.objectReferenceValue as T;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
#endif
