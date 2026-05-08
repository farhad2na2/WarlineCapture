#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureIslandAssetValidationBuilder
{
    private const float PixelsPerUnit = 100f;
    private const string BaseScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/IslandLayerPrototype_B.unity";
    private const string ValidationScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/IslandLayerPrototype_B_AssetValidation.unity";
    private const string SpriteRoot = "Assets/Game/Art/Generated/IsometricMaps/IslandLayerPrototype_B/ValidationSprites/Sprites";
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/IslandLayerPrototype_B/ValidationSprites";

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

    private static readonly SpriteSpec[] Samples =
    {
        new("Validation_CommandCenter_A", SpriteRoot + "/building_command_center_a.png", new Vector2(-10.35f, 8.35f), 0.42f, 40),
        new("Validation_AirportTerminal_A", SpriteRoot + "/building_airport_terminal_a.png", new Vector2(7.55f, 7.7f), 0.42f, 40),
        new("Validation_UtilityDepot_A", SpriteRoot + "/building_utility_depot_a.png", new Vector2(-6.75f, 6.55f), 0.38f, 40),
        new("Validation_CommandJeep_A", SpriteRoot + "/unit_command_jeep_a.png", new Vector2(-8.55f, 7.55f), 0.24f, 45),
        new("Validation_LightApc_A", SpriteRoot + "/unit_light_apc_a.png", new Vector2(8.95f, 6.55f), 0.23f, 45),
        new("Validation_InfantrySquad_A", SpriteRoot + "/unit_infantry_squad_a.png", new Vector2(-7.25f, 8.45f), 0.18f, 45),
    };

    [MenuItem("WarlineCapture/Design/Build Island Asset Validation Scene")]
    public static void BuildIslandAssetValidationScene()
    {
        AssetDatabase.Refresh();
        foreach (SpriteSpec sample in Samples)
        {
            EnsureSpriteImport(sample.AssetPath);
        }

        AssetDatabase.Refresh();
        Directory.CreateDirectory(ProjectPath(PrefabRoot));

        foreach (SpriteSpec sample in Samples)
        {
            SavePrefab(sample);
        }

        EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

        GameObject existing = GameObject.Find("GeneratedAssetValidationSamples_A");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject root = new("GeneratedAssetValidationSamples_A");
        foreach (SpriteSpec sample in Samples)
        {
            GameObject sampleObject = CreateSampleObject(sample);
            if (sampleObject == null)
            {
                continue;
            }

            sampleObject.transform.SetParent(root.transform, false);
            sampleObject.transform.localPosition = new Vector3(sample.Position.x, sample.Position.y, -0.05f);
            sampleObject.transform.localScale = new Vector3(sample.Scale, sample.Scale, 1f);
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = Object.FindAnyObjectByType<Camera>();
        }

        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 7.15f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 8.75f;
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ValidationScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_ISLAND_ASSET_VALIDATION_BUILT scene={ValidationScenePath} samples={Samples.Length}");
    }

    private static GameObject CreateSampleObject(SpriteSpec sample)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sample.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_ISLAND_ASSET_VALIDATION_MISSING_SPRITE name={sample.Name} path={sample.AssetPath}");
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
        GameObject prefab = CreateSampleObject(sample);
        if (prefab == null)
        {
            return;
        }

        prefab.transform.localScale = new Vector3(sample.Scale, sample.Scale, 1f);
        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/" + sample.Name + ".prefab");
        Object.DestroyImmediate(prefab);
    }

    private static void EnsureSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_ISLAND_ASSET_VALIDATION_IMPORTER_MISSING path={assetPath}");
            return;
        }

        bool changed = false;
        changed |= SetTextureType(importer, TextureImporterType.Sprite);
        changed |= SetSpriteImportMode(importer, SpriteImportMode.Single);
        changed |= SetAlphaSource(importer, TextureImporterAlphaSource.FromInput);
        changed |= SetAlphaIsTransparency(importer, true);
        changed |= SetMipmapEnabled(importer, false);
        changed |= SetSrgbTexture(importer, true);
        changed |= SetFilterMode(importer, FilterMode.Bilinear);
        changed |= SetTextureCompression(importer, TextureImporterCompression.CompressedHQ);

        if (importer.spritePixelsPerUnit != PixelsPerUnit)
        {
            importer.spritePixelsPerUnit = PixelsPerUnit;
            changed = true;
        }

        changed |= EnsurePlatformSettings(importer, "DefaultTexturePlatform", false, 1024, TextureImporterFormat.Automatic);
        changed |= EnsurePlatformSettings(importer, "Android", true, 1024, TextureImporterFormat.ASTC_6x6);

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
