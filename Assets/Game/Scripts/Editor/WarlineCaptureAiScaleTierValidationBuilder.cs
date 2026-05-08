#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureAiScaleTierValidationBuilder
{
    private const float PixelsPerUnit = 100f;
    private const string BaseScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/IslandLayerPrototype_B.unity";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/IslandLayerPrototype_B_AiScaleTierValidation.unity";
    private const string StrategicSpriteRoot = "Assets/Game/Art/Generated/IsometricMaps/IslandLayerPrototype_B/ScaleTierValidation/StrategicSprites";
    private const string TacticalRoot = "Assets/Game/Art/Generated/IsometricMaps/IslandLayerPrototype_B/ScaleTierValidation/Tactical";
    private const string CloseSpriteRoot = "Assets/Game/Art/Generated/IsometricMaps/IslandLayerPrototype_B/ValidationSprites/Sprites";
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/IslandLayerPrototype_B/ScaleTierValidation";

    private readonly struct SpriteSpec
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly Vector2 Position;
        public readonly float Scale;
        public readonly int SortingOrder;

        public SpriteSpec(string name, string assetPath, Vector2 position, float scale, int sortingOrder)
        {
            Name = name;
            AssetPath = assetPath;
            Position = position;
            Scale = scale;
            SortingOrder = sortingOrder;
        }
    }

    private static readonly SpriteSpec[] StrategicSamples =
    {
        new("Strategic_HQ_A", StrategicSpriteRoot + "/strategic_hq_a.png", new Vector2(-10.55f, 8.35f), 0.24f, 40),
        new("Strategic_Radar_A", StrategicSpriteRoot + "/strategic_radar_a.png", new Vector2(-7.8f, 8.55f), 0.22f, 40),
        new("Strategic_Power_A", StrategicSpriteRoot + "/strategic_power_a.png", new Vector2(-6.35f, 6.65f), 0.22f, 40),
        new("Strategic_AirfieldService_A", StrategicSpriteRoot + "/strategic_airfield_service_a.png", new Vector2(7.35f, 7.55f), 0.24f, 40),
        new("Strategic_VehicleMarker_A", StrategicSpriteRoot + "/strategic_vehicle_platoon_marker_a.png", new Vector2(-8.6f, 7.25f), 0.16f, 45),
        new("Strategic_InfantryMarker_A", StrategicSpriteRoot + "/strategic_infantry_marker_a.png", new Vector2(-7.15f, 7.55f), 0.14f, 45),
        new("Strategic_HelicopterMarker_A", StrategicSpriteRoot + "/strategic_helicopter_marker_a.png", new Vector2(9.1f, 8.55f), 0.15f, 45),
        new("Strategic_EnemyMarker_A", StrategicSpriteRoot + "/strategic_enemy_marker_a.png", new Vector2(6.2f, 6.35f), 0.14f, 45),
    };

    private static readonly SpriteSpec[] TacticalSamples =
    {
        new("Tactical_CommandCenter_A", CloseSpriteRoot + "/building_command_center_a.png", new Vector2(-3.0f, 0.9f), 0.54f, 60),
        new("Tactical_UtilityDepot_A", CloseSpriteRoot + "/building_utility_depot_a.png", new Vector2(2.9f, 1.45f), 0.48f, 60),
        new("Tactical_CommandJeep_A", CloseSpriteRoot + "/unit_command_jeep_a.png", new Vector2(-0.9f, -1.25f), 0.28f, 65),
        new("Tactical_LightApc_A", CloseSpriteRoot + "/unit_light_apc_a.png", new Vector2(1.1f, -0.95f), 0.27f, 65),
        new("Tactical_InfantrySquad_A", CloseSpriteRoot + "/unit_infantry_squad_a.png", new Vector2(-0.15f, 0.1f), 0.22f, 65),
    };

    [MenuItem("WarlineCapture/Design/Build AI Scale Tier Validation Scene")]
    public static void BuildAiScaleTierValidationScene()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory(ProjectPath(PrefabRoot));

        foreach (SpriteSpec sample in StrategicSamples)
        {
            EnsureSpriteImport(sample.AssetPath, true, 1024);
        }

        foreach (SpriteSpec sample in TacticalSamples)
        {
            EnsureSpriteImport(sample.AssetPath, true, 1024);
        }

        string tacticalPatchPath = TacticalRoot + "/tactical_closeup_patch_a.png";
        EnsureSpriteImport(tacticalPatchPath, false, 2048);
        AssetDatabase.Refresh();

        foreach (SpriteSpec sample in StrategicSamples)
        {
            SavePrefab(sample);
        }

        EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

        DestroyIfExists("AiScaleTier_StrategicSamples_A");
        DestroyIfExists("AiScaleTier_TacticalCloseup_A");
        DestroyIfExists("AiScaleTier_StrategicCamera_A");
        DestroyIfExists("AiScaleTier_TacticalCamera_A");

        BuildStrategicSamples();
        BuildTacticalCloseup(tacticalPatchPath);
        BuildCameras();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_AI_SCALE_TIER_VALIDATION_BUILT scene={ScenePath} strategicSamples={StrategicSamples.Length} tacticalSamples={TacticalSamples.Length}");
    }

    private static void BuildStrategicSamples()
    {
        GameObject root = new("AiScaleTier_StrategicSamples_A");
        foreach (SpriteSpec sample in StrategicSamples)
        {
            GameObject sampleObject = CreateSpriteObject(sample);
            if (sampleObject == null)
            {
                continue;
            }

            sampleObject.transform.SetParent(root.transform, false);
            sampleObject.transform.localPosition = new Vector3(sample.Position.x, sample.Position.y, -0.05f);
            sampleObject.transform.localScale = new Vector3(sample.Scale, sample.Scale, 1f);
        }
    }

    private static void BuildTacticalCloseup(string tacticalPatchPath)
    {
        GameObject root = new("AiScaleTier_TacticalCloseup_A");
        root.transform.position = new Vector3(0f, -25f, 0f);

        Sprite patchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(tacticalPatchPath);
        if (patchSprite != null)
        {
            GameObject patch = new("Tactical_Closeup_Patch_A");
            patch.transform.SetParent(root.transform, false);
            patch.transform.localPosition = new Vector3(0f, 0f, 0f);
            patch.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

            SpriteRenderer patchRenderer = patch.AddComponent<SpriteRenderer>();
            patchRenderer.sprite = patchSprite;
            patchRenderer.sortingOrder = 50;
        }
        else
        {
            Debug.LogError($"WARLINECAPTURE_AI_SCALE_TIER_TACTICAL_PATCH_MISSING path={tacticalPatchPath}");
        }

        foreach (SpriteSpec sample in TacticalSamples)
        {
            GameObject sampleObject = CreateSpriteObject(sample);
            if (sampleObject == null)
            {
                continue;
            }

            sampleObject.transform.SetParent(root.transform, false);
            sampleObject.transform.localPosition = new Vector3(sample.Position.x, sample.Position.y, -0.05f);
            sampleObject.transform.localScale = new Vector3(sample.Scale, sample.Scale, 1f);
        }
    }

    private static void BuildCameras()
    {
        Camera existing = Camera.main;
        if (existing == null)
        {
            existing = Object.FindAnyObjectByType<Camera>();
        }

        if (existing != null)
        {
            existing.name = "AiScaleTier_StrategicCamera_A";
            existing.transform.position = new Vector3(0f, 7.15f, -10f);
            existing.transform.rotation = Quaternion.identity;
            existing.orthographic = true;
            existing.orthographicSize = 8.75f;
        }

        GameObject tacticalCameraObject = new("AiScaleTier_TacticalCamera_A");
        Camera tacticalCamera = tacticalCameraObject.AddComponent<Camera>();
        tacticalCamera.enabled = false;
        tacticalCamera.clearFlags = CameraClearFlags.SolidColor;
        tacticalCamera.backgroundColor = new Color(0.01f, 0.05f, 0.07f, 1f);
        tacticalCamera.orthographic = true;
        tacticalCamera.orthographicSize = 4.6f;
        tacticalCamera.nearClipPlane = 0.1f;
        tacticalCamera.farClipPlane = 100f;
        tacticalCamera.transform.position = new Vector3(0f, -25f, -10f);
        tacticalCamera.transform.rotation = Quaternion.identity;
    }

    private static GameObject CreateSpriteObject(SpriteSpec sample)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sample.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_AI_SCALE_TIER_MISSING_SPRITE name={sample.Name} path={sample.AssetPath}");
            return null;
        }

        GameObject sampleObject = new(sample.Name);
        SpriteRenderer renderer = sampleObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sample.SortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;
        return sampleObject;
    }

    private static void SavePrefab(SpriteSpec sample)
    {
        GameObject prefab = CreateSpriteObject(sample);
        if (prefab == null)
        {
            return;
        }

        prefab.transform.localScale = new Vector3(sample.Scale, sample.Scale, 1f);
        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/" + sample.Name + ".prefab");
        Object.DestroyImmediate(prefab);
    }

    private static void DestroyIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void EnsureSpriteImport(string assetPath, bool alpha, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_AI_SCALE_TIER_IMPORTER_MISSING path={assetPath}");
            return;
        }

        bool changed = false;
        changed |= SetTextureType(importer, TextureImporterType.Sprite);
        changed |= SetSpriteImportMode(importer, SpriteImportMode.Single);
        changed |= SetAlphaSource(importer, alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None);
        changed |= SetAlphaIsTransparency(importer, alpha);
        changed |= SetMipmapEnabled(importer, false);
        changed |= SetSrgbTexture(importer, true);
        changed |= SetFilterMode(importer, FilterMode.Bilinear);
        changed |= SetTextureCompression(importer, TextureImporterCompression.CompressedHQ);

        if (importer.spritePixelsPerUnit != PixelsPerUnit)
        {
            importer.spritePixelsPerUnit = PixelsPerUnit;
            changed = true;
        }

        changed |= EnsurePlatformSettings(importer, "DefaultTexturePlatform", false, maxTextureSize, TextureImporterFormat.Automatic);
        changed |= EnsurePlatformSettings(importer, "Android", true, maxTextureSize, TextureImporterFormat.ASTC_6x6);

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static bool EnsurePlatformSettings(TextureImporter importer, string platformName, bool overridden, int maxTextureSize, TextureImporterFormat format)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        bool changed = settings.overridden != overridden || settings.maxTextureSize != maxTextureSize || settings.format != format;
        if (!changed)
        {
            return false;
        }

        settings.overridden = overridden;
        settings.maxTextureSize = maxTextureSize;
        settings.format = format;
        importer.SetPlatformTextureSettings(settings);
        return true;
    }

    private static bool SetTextureType(TextureImporter importer, TextureImporterType textureType)
    {
        if (importer.textureType == textureType)
        {
            return false;
        }

        importer.textureType = textureType;
        return true;
    }

    private static bool SetSpriteImportMode(TextureImporter importer, SpriteImportMode importMode)
    {
        if (importer.spriteImportMode == importMode)
        {
            return false;
        }

        importer.spriteImportMode = importMode;
        return true;
    }

    private static bool SetAlphaSource(TextureImporter importer, TextureImporterAlphaSource alphaSource)
    {
        if (importer.alphaSource == alphaSource)
        {
            return false;
        }

        importer.alphaSource = alphaSource;
        return true;
    }

    private static bool SetAlphaIsTransparency(TextureImporter importer, bool enabled)
    {
        if (importer.alphaIsTransparency == enabled)
        {
            return false;
        }

        importer.alphaIsTransparency = enabled;
        return true;
    }

    private static bool SetMipmapEnabled(TextureImporter importer, bool enabled)
    {
        if (importer.mipmapEnabled == enabled)
        {
            return false;
        }

        importer.mipmapEnabled = enabled;
        return true;
    }

    private static bool SetSrgbTexture(TextureImporter importer, bool enabled)
    {
        if (importer.sRGBTexture == enabled)
        {
            return false;
        }

        importer.sRGBTexture = enabled;
        return true;
    }

    private static bool SetFilterMode(TextureImporter importer, FilterMode filterMode)
    {
        if (importer.filterMode == filterMode)
        {
            return false;
        }

        importer.filterMode = filterMode;
        return true;
    }

    private static bool SetTextureCompression(TextureImporter importer, TextureImporterCompression textureCompression)
    {
        if (importer.textureCompression == textureCompression)
        {
            return false;
        }

        importer.textureCompression = textureCompression;
        return true;
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
#endif
