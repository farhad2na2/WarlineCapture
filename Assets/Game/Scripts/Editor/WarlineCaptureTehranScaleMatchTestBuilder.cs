#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureTehranScaleMatchTestBuilder
{
    private const float PixelsPerUnit = 100f;
    private const string ArtRoot = "Assets/Game/Art/Generated/IsometricMaps/TehranStrategicMap_A/ScaleMatchTest";
    private const string MapPath = ArtRoot + "/Sources/tehran_clean_strategy_area_a.png";
    private const string SpriteRoot = ArtRoot + "/Sprites";
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/TehranStrategicMap_A/ScaleMatchTest";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/TehranScaleMatchTest_A.unity";

    private readonly struct EntitySpec
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly Vector2 PixelPosition;
        public readonly float Scale;
        public readonly int SortingOrder;

        public EntitySpec(string name, string assetPath, Vector2 pixelPosition, float scale, int sortingOrder)
        {
            Name = name;
            AssetPath = assetPath;
            PixelPosition = pixelPosition;
            Scale = scale;
            SortingOrder = sortingOrder;
        }
    }

    private static readonly EntitySpec[] Entities =
    {
        new("ScaleMatch_CommandBuilding", SpriteRoot + "/command_building.png", new Vector2(560f, 560f), 0.34f, 20),
        new("ScaleMatch_TentCluster", SpriteRoot + "/tent_cluster.png", new Vector2(420f, 340f), 0.34f, 21),
        new("ScaleMatch_VehicleGarage", SpriteRoot + "/vehicle_garage.png", new Vector2(1035f, 565f), 0.32f, 22),
        new("ScaleMatch_FuelModule", SpriteRoot + "/fuel_module.png", new Vector2(1190f, 330f), 0.32f, 23),
        new("ScaleMatch_TankGroup", SpriteRoot + "/tank_group.png", new Vector2(890f, 650f), 0.23f, 24),
        new("ScaleMatch_ApcGroup", SpriteRoot + "/apc_group.png", new Vector2(810f, 570f), 0.22f, 25),
        new("ScaleMatch_InfantrySquad", SpriteRoot + "/infantry_squad.png", new Vector2(700f, 500f), 0.18f, 26),
        new("ScaleMatch_Helicopter", SpriteRoot + "/helicopter.png", new Vector2(1135f, 710f), 0.22f, 27),
    };

    [MenuItem("WarlineCapture/Design/Build Tehran Scale Match Test A")]
    public static void BuildTehranScaleMatchTestA()
    {
        AssetDatabase.Refresh();
        EnsureSpriteImport(MapPath, false, 2048);
        foreach (EntitySpec entity in Entities)
        {
            EnsureSpriteImport(entity.AssetPath, true, 1024);
        }

        AssetDatabase.Refresh();

        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapPath);
        if (mapSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_TEHRAN_SCALE_MATCH_TEST_A_MISSING_MAP path={MapPath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(PrefabRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        SaveEntityPrefabs();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(mapSprite);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_TEHRAN_SCALE_MATCH_TEST_A_BUILT scene={ScenePath} entities={Entities.Length}");
    }

    private static void BuildScene(Sprite mapSprite)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("TehranScaleMatchTest_A");

        GameObject mapObject = CreateSpriteObject("CleanStrategyMap_Background", mapSprite, 0);
        mapObject.transform.SetParent(root.transform, false);

        GameObject entityRoot = new("SeparateGeneratedEntities");
        entityRoot.transform.SetParent(root.transform, false);

        foreach (EntitySpec entity in Entities)
        {
            GameObject entityObject = CreateEntityObject(entity, mapSprite);
            if (entityObject == null)
            {
                continue;
            }

            entityObject.transform.SetParent(entityRoot.transform, false);
        }

        BuildCameras(mapSprite);
        Selection.activeObject = root;
    }

    private static GameObject CreateEntityObject(EntitySpec entity, Sprite mapSprite)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entity.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_TEHRAN_SCALE_MATCH_TEST_A_MISSING_ENTITY name={entity.Name} path={entity.AssetPath}");
            return null;
        }

        GameObject entityObject = CreateSpriteObject(entity.Name, sprite, entity.SortingOrder);
        entityObject.transform.localPosition = PixelToWorld(entity.PixelPosition, mapSprite);
        entityObject.transform.localScale = new Vector3(entity.Scale, entity.Scale, 1f);
        entityObject.AddComponent<BoxCollider2D>();
        return entityObject;
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

    private static Vector3 PixelToWorld(Vector2 pixelPosition, Sprite mapSprite)
    {
        float x = (pixelPosition.x - mapSprite.rect.width * 0.5f) / PixelsPerUnit;
        float y = (mapSprite.rect.height * 0.5f - pixelPosition.y) / PixelsPerUnit;
        return new Vector3(x, y, -0.05f);
    }

    private static void SaveEntityPrefabs()
    {
        foreach (EntitySpec entity in Entities)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entity.AssetPath);
            if (sprite == null)
            {
                continue;
            }

            GameObject prefab = CreateSpriteObject(entity.Name, sprite, entity.SortingOrder);
            prefab.transform.localScale = new Vector3(entity.Scale, entity.Scale, 1f);
            prefab.AddComponent<BoxCollider2D>();
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/" + entity.Name + ".prefab");
            Object.DestroyImmediate(prefab);
        }
    }

    private static void BuildCameras(Sprite mapSprite)
    {
        Camera fullMapCamera = CreateCamera(
            "TehranScaleMatchTest_A_FullMapCamera",
            new Vector3(0f, 0f, -10f),
            mapSprite.rect.height / PixelsPerUnit * 0.52f,
            true);
        Camera.SetupCurrent(fullMapCamera);

        CreateCamera(
            "TehranScaleMatchTest_A_CloseReviewCamera",
            PixelToWorld(new Vector2(835f, 560f), mapSprite) + new Vector3(0f, 0f, -9.95f),
            2.85f,
            false);
    }

    private static Camera CreateCamera(string name, Vector3 position, float orthographicSize, bool enabled)
    {
        GameObject cameraObject = new(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = enabled;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.035f, 0.03f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static void EnsureSpriteImport(string assetPath, bool alpha, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_TEHRAN_SCALE_MATCH_TEST_A_IMPORTER_MISSING path={assetPath}");
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
