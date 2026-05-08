#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureTacticalGroundQualityTestBuilder
{
    private const float GroundPixelsPerUnit = 600f;
    private const float CloseCameraOrthographicSize = 0.597f;
    private const float EntityPixelsPerUnit = 100f;
    private const string ArtRoot = "Assets/Game/Art/Generated/IsometricMaps";
    private const string GroundPath = ArtRoot + "/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a.png";
    private const string EntityRoot = ArtRoot + "/TehranStrategicMap_A/ScaleMatchTest/Sprites";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalGroundQualityTest_A.unity";

    private readonly struct EntitySpec
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly Vector3 Position;
        public readonly float Scale;
        public readonly int SortingOrder;

        public EntitySpec(string name, string assetPath, Vector3 position, float scale, int sortingOrder)
        {
            Name = name;
            AssetPath = assetPath;
            Position = position;
            Scale = scale;
            SortingOrder = sortingOrder;
        }
    }

    private static readonly EntitySpec[] Entities =
    {
        new("QualityTest_CommandBuilding", EntityRoot + "/command_building.png", new Vector3(-0.94f, 0.16f, -0.05f), 0.20f, 20),
        new("QualityTest_TentCluster", EntityRoot + "/tent_cluster.png", new Vector3(-0.34f, 0.33f, -0.05f), 0.19f, 21),
        new("QualityTest_VehicleGarage", EntityRoot + "/vehicle_garage.png", new Vector3(0.86f, -0.20f, -0.05f), 0.18f, 22),
        new("QualityTest_FuelModule", EntityRoot + "/fuel_module.png", new Vector3(0.48f, 0.15f, -0.05f), 0.17f, 23),
        new("QualityTest_TankGroup", EntityRoot + "/tank_group.png", new Vector3(-0.12f, -0.10f, -0.05f), 0.20f, 24),
        new("QualityTest_ApcGroup", EntityRoot + "/apc_group.png", new Vector3(0.32f, -0.34f, -0.05f), 0.20f, 25),
        new("QualityTest_InfantrySquad", EntityRoot + "/infantry_squad.png", new Vector3(-0.58f, -0.24f, -0.05f), 0.25f, 26),
        new("QualityTest_Helicopter", EntityRoot + "/helicopter.png", new Vector3(1.02f, 0.34f, -0.05f), 0.18f, 27),
    };

    [MenuItem("WarlineCapture/Design/Build Tactical Ground Quality Test A")]
    public static void BuildTacticalGroundQualityTestA()
    {
        AssetDatabase.Refresh();
        EnsureSpriteImport(GroundPath, false, GroundPixelsPerUnit, 2048, TextureImporterCompression.CompressedHQ);
        foreach (EntitySpec entity in Entities)
        {
            EnsureSpriteImport(entity.AssetPath, true, EntityPixelsPerUnit, 1024, TextureImporterCompression.Uncompressed);
        }

        AssetDatabase.Refresh();

        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GroundPath);
        if (groundSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_TACTICAL_GROUND_QUALITY_TEST_MISSING_GROUND path={GroundPath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(groundSprite);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_TACTICAL_GROUND_QUALITY_TEST_BUILT scene={ScenePath} ground={GroundPath} entities={Entities.Length}");
    }

    private static void BuildScene(Sprite groundSprite)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("TacticalGroundQualityTest_A");
        GameObject ground = CreateSpriteObject("NativeCloseupTacticalGround_Background", groundSprite, 0);
        ground.transform.SetParent(root.transform, false);

        GameObject entityRoot = new("SeparateGeneratedEntities_ForQualityComparison");
        entityRoot.transform.SetParent(root.transform, false);

        foreach (EntitySpec entity in Entities)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entity.AssetPath);
            if (sprite == null)
            {
                Debug.LogError($"WARLINECAPTURE_TACTICAL_GROUND_QUALITY_TEST_MISSING_ENTITY name={entity.Name} path={entity.AssetPath}");
                continue;
            }

            GameObject entityObject = CreateSpriteObject(entity.Name, sprite, entity.SortingOrder);
            entityObject.transform.SetParent(entityRoot.transform, false);
            entityObject.transform.localPosition = entity.Position;
            entityObject.transform.localScale = new Vector3(entity.Scale, entity.Scale, 1f);
            entityObject.AddComponent<BoxCollider2D>();
        }

        CreateCamera("TacticalGroundQualityTest_A_CloseCamera_CloseScale", new Vector3(0f, 0f, -10f), CloseCameraOrthographicSize, true);
        CreateCamera("TacticalGroundQualityTest_A_FullTileCamera", new Vector3(0f, 0f, -10f), groundSprite.rect.height / GroundPixelsPerUnit * 0.52f, false);
        Selection.activeObject = root;
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, int sortingOrder)
    {
        GameObject spriteObject = new(name);
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;
        return spriteObject;
    }

    private static Camera CreateCamera(string name, Vector3 position, float orthographicSize, bool enabled)
    {
        GameObject cameraObject = new(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = enabled;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.041f, 0.036f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static void EnsureSpriteImport(string assetPath, bool alpha, float pixelsPerUnit, int maxTextureSize, TextureImporterCompression compression)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_TACTICAL_GROUND_QUALITY_TEST_IMPORTER_MISSING path={assetPath}");
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
        changed |= SetTextureCompression(importer, compression);

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
        {
            importer.spritePixelsPerUnit = pixelsPerUnit;
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

    private static bool SetAlphaIsTransparency(TextureImporter importer, bool value)
    {
        if (importer.alphaIsTransparency == value)
        {
            return false;
        }

        importer.alphaIsTransparency = value;
        return true;
    }

    private static bool SetMipmapEnabled(TextureImporter importer, bool value)
    {
        if (importer.mipmapEnabled == value)
        {
            return false;
        }

        importer.mipmapEnabled = value;
        return true;
    }

    private static bool SetSrgbTexture(TextureImporter importer, bool value)
    {
        if (importer.sRGBTexture == value)
        {
            return false;
        }

        importer.sRGBTexture = value;
        return true;
    }

    private static bool SetFilterMode(TextureImporter importer, FilterMode value)
    {
        if (importer.filterMode == value)
        {
            return false;
        }

        importer.filterMode = value;
        return true;
    }

    private static bool SetTextureCompression(TextureImporter importer, TextureImporterCompression value)
    {
        if (importer.textureCompression == value)
        {
            return false;
        }

        importer.textureCompression = value;
        return true;
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }
}
#endif
