#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PortraitSpritePrefabRenderer
{
    private const int Size = 2048;
    private const string OutputDirectory = "Assets/Game/Art/UI/Portraits/Generated";
    private static readonly Color ChromaGreen = new(0f, 1f, 0f, 1f);

    public static void RenderUnitChrSoldierMale01()
    {
        RenderPrefabPortrait(
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01.prefab",
            OutputDirectory + "/Portrait_Unit_Chr_Soldier_Male_01_Prefab_ChromaGreen.png");
    }

    public static void RenderUnitChrSoldierMale01Alt01()
    {
        RenderPrefabPortrait(
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01_Alt_01.prefab",
            OutputDirectory + "/References/Characters/PortraitReference_Unit_Chr_Soldier_Male_01_Alt_01_ChromaGreen.png");
    }

    public static void RenderUnitChrSoldierMale01Alt02()
    {
        RenderPrefabPortrait(
            "Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_01_Alt_02.prefab",
            OutputDirectory + "/References/Characters/PortraitReference_Unit_Chr_Soldier_Male_01_Alt_02_ChromaGreen.png");
    }

    public static void RenderPrefabFromCommandLine()
    {
        string prefabPath = ReadCommandLineValue("-portraitPrefab");
        string outputPath = ReadCommandLineValue("-portraitOutput");
        if (string.IsNullOrWhiteSpace(prefabPath))
            throw new InvalidOperationException("Missing -portraitPrefab argument.");
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new InvalidOperationException("Missing -portraitOutput argument.");

        RenderPrefabPortrait(prefabPath, outputPath);
    }

    public static void RenderAllPortraitReferences()
    {
        RenderPortraitReferences("Assets/Game/Prefabs/Characters", "Unit_Chr_", "Characters");
        RenderPortraitReferences("Assets/Game/Prefabs/Vehicles", "Unit_Veh_", "Vehicles");
        RenderPortraitReferences("Assets/Game/Prefabs/Buildings", string.Empty, "Buildings");
    }

    public static void RenderCharacterPortraitReferences()
    {
        RenderPortraitReferences("Assets/Game/Prefabs/Characters", "Unit_Chr_", "Characters");
    }

    public static void RenderVehiclePortraitReferences()
    {
        RenderPortraitReferences("Assets/Game/Prefabs/Vehicles", "Unit_Veh_", "Vehicles");
    }

    public static void RenderBuildingPortraitReferences()
    {
        RenderPortraitReferences("Assets/Game/Prefabs/Buildings", string.Empty, "Buildings");
    }

    private static void RenderPortraitReferences(string assetFolder, string requiredNamePrefix, string outputSubfolder)
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetFolder });
        Array.Sort(prefabGuids, StringComparer.Ordinal);
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!ShouldRenderReferencePrefab(prefabPath, requiredNamePrefix))
                continue;

            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            string outputPath = $"{OutputDirectory}/References/{outputSubfolder}/PortraitReference_{prefabName}_ChromaGreen.png";
            RenderPrefabPortrait(prefabPath, outputPath);
        }
    }

    private static bool ShouldRenderReferencePrefab(string prefabPath, string requiredNamePrefix)
    {
        if (string.IsNullOrWhiteSpace(prefabPath) || prefabPath.Contains("/Destroyed/", StringComparison.OrdinalIgnoreCase) || prefabPath.Contains("/DestroyedVisuals/", StringComparison.OrdinalIgnoreCase))
            return false;

        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
        if (string.IsNullOrWhiteSpace(prefabName))
            return false;

        if (!string.IsNullOrWhiteSpace(requiredNamePrefix) && !prefabName.StartsWith(requiredNamePrefix, StringComparison.Ordinal))
            return false;

        if (prefabName is "Unit" or "Unit_Veh" or "Building" or "BuildingSelectionMarker" or "VehicleSelectionMarker" or "VehicleHealthBar")
            return false;

        return true;
    }

    private static string ReadCommandLineValue(string key)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], key, StringComparison.Ordinal))
                return args[i + 1];
        }

        return string.Empty;
    }

    private static void RenderPrefabPortrait(string prefabPath, string outputPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new InvalidOperationException("Portrait source prefab not found: " + prefabPath);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.82f, 0.8f, 0.76f, 1f);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
            throw new InvalidOperationException("Could not instantiate portrait source prefab: " + prefabPath);

        instance.name = prefab.name + "_PortraitSource";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        DisableNonPortraitRenderers(instance);
        ApplyConfiguredGpuAnimationPose(instance, 0.35f);

        Bounds bounds = CalculateRenderableBounds(instance);
        Vector3 offset = new(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        instance.transform.position += offset;
        bounds.center += offset;

        GameObject keyObject = new("Portrait Key Light");
        Light key = keyObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.1f;
        key.color = new Color(1f, 0.92f, 0.82f, 1f);
        key.shadows = LightShadows.None;
        keyObject.transform.rotation = Quaternion.Euler(34f, -32f, 0f);

        GameObject fillObject = new("Portrait Fill Light");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.35f;
        fill.color = new Color(0.62f, 0.72f, 1f, 1f);
        fill.shadows = LightShadows.None;
        fillObject.transform.rotation = Quaternion.Euler(28f, 140f, 0f);

        GameObject cameraObject = new("Portrait Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = ChromaGreen;
        camera.orthographic = true;
        camera.aspect = 1f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;

        float height = Mathf.Max(bounds.size.y, 0.01f);
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        camera.orthographicSize = Mathf.Max(height * 0.58f, width * 0.75f);

        Vector3 focus = new(bounds.center.x, bounds.min.y + height * 0.54f, bounds.center.z);
        Vector3 viewDirection = new Vector3(0.35f, -0.08f, 1f).normalized;
        camera.transform.position = focus + viewDirection * 7f;
        camera.transform.LookAt(focus);

        RenderTexture target = new(Size, Size, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };

        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();

            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            texture.Apply(false, false);
            NormalizeChromaGreen(texture);

            string fullOutputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath));
            File.WriteAllBytes(fullOutputPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(target);
        }

        AssetDatabase.Refresh();
        Debug.Log("[PortraitSpritePrefabRenderer] rendered prefab=" + prefabPath + " output=" + outputPath);
    }

    private static void DisableNonPortraitRenderers(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            string path = HierarchyPath(renderer.transform);
            if (path.Contains("/SelectionMarker/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/SelectionMarker", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/FactionMarker/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/FactionMarker", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/HealthBar/", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/HealthBar", StringComparison.OrdinalIgnoreCase))
            {
                renderer.enabled = false;
            }
        }
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

        if (!hasBounds)
            throw new InvalidOperationException("Portrait source has no enabled renderers: " + root.name);

        return bounds;
    }

    private static void NormalizeChromaGreen(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.g > 130 && pixel.g > pixel.r * 1.4f && pixel.g > pixel.b * 1.4f)
                pixels[i] = new Color32(0, 255, 0, pixel.a);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
    }

    private static void ApplyConfiguredGpuAnimationPose(GameObject instance, float animationTime)
    {
        if (instance == null)
            return;

        UnitGridAuthoring authoring = instance.GetComponent<UnitGridAuthoring>();
        MaterialAnimatorIndexAuthoring indexAuthoring = instance.GetComponentInChildren<MaterialAnimatorIndexAuthoring>(true);
        if (indexAuthoring == null || indexAuthoring.animator == null)
            return;

        MaterialAnimatorAuthoring animatorAuthoring = indexAuthoring.animator.GetComponent<MaterialAnimatorAuthoring>();
        if (animatorAuthoring == null || animatorAuthoring.animations == null || animatorAuthoring.animations.Count == 0)
            return;

        int animationIndex = indexAuthoring.animationIndex;
        if (animationIndex <= 0)
            animationIndex = ResolveConfiguredPreviewAnimationIndex(authoring, UnitAnimationKind.Idle, UnitAnimationKind.Aim, UnitAnimationKind.Walk);
        animationIndex = Mathf.Clamp(animationIndex, 0, animatorAuthoring.animations.Count - 1);
        MaterialAnimatorBake animation = animatorAuthoring.animations[animationIndex];
        int frameCount = Mathf.Max(1, animation.frames);
        int boneCount = Mathf.Max(1, animatorAuthoring.bonesCount);
        float frameFloat = animationTime * Mathf.Max(1, animation.fps) * Mathf.Max(1, animation.speed);
        int frame = Mathf.FloorToInt(frameFloat) % frameCount;
        int nextFrame = (frame + 1) % frameCount;
        float blend = frameFloat - Mathf.Floor(frameFloat);
        Vector4 renderPixel = new(animation.start + frame * boneCount, animation.start + nextFrame * boneCount, blend, 0f);

        MaterialPropertyBlock propertyBlock = new();
        int modelShownId = Shader.PropertyToID("_SnivelerModelShown");
        int renderPixelId = Shader.PropertyToID("_SnivelerRenderPixel");
        Renderer[] renderers = indexAuthoring.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
                continue;

            int materialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0;
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                renderer.GetPropertyBlock(propertyBlock, materialIndex);
                propertyBlock.SetFloat(modelShownId, 1f);
                propertyBlock.SetVector(renderPixelId, renderPixel);
                renderer.SetPropertyBlock(propertyBlock, materialIndex);
            }
        }
    }

    private static int ResolveConfiguredPreviewAnimationIndex(UnitGridAuthoring authoring, UnitAnimationKind first, UnitAnimationKind second, UnitAnimationKind third)
    {
        if (authoring != null && authoring.AnimationOrder != null)
        {
            if (TryResolveConfiguredPreviewAnimationIndex(authoring.AnimationOrder, first, out int index))
                return index;
            if (TryResolveConfiguredPreviewAnimationIndex(authoring.AnimationOrder, second, out index))
                return index;
            if (TryResolveConfiguredPreviewAnimationIndex(authoring.AnimationOrder, third, out index))
                return index;
        }

        return 0;
    }

    private static bool TryResolveConfiguredPreviewAnimationIndex(IReadOnlyList<UnitAnimationKind> animationOrder, UnitAnimationKind kind, out int animationIndex)
    {
        if (animationOrder != null)
        {
            for (int i = 0; i < animationOrder.Count; i++)
            {
                if (animationOrder[i] == kind)
                {
                    animationIndex = i;
                    return true;
                }
            }
        }

        animationIndex = 0;
        return false;
    }

    private static string HierarchyPath(Transform transform)
    {
        if (transform == null)
            return string.Empty;

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
#endif
