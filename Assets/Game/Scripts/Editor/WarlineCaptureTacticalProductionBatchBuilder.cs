#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureTacticalProductionBatchBuilder
{
    private const float GroundPixelsPerUnit = 600f;
    private const float EntityPixelsPerUnit = 100f;
    private const float CloseCameraOrthographicSize = 0.597f;
    private const string ArtRoot = "Assets/Game/Art/Generated/IsometricMaps";
    private const string BatchRoot = ArtRoot + "/TacticalProductionBatch_A";
    private const string ExistingGroundPath = ArtRoot + "/TacticalGroundQualityTest_A/tactical_ground_quality_test_close_pot_a.png";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalProductionBatch_A.unity";

    private readonly struct MapSpec
    {
        public readonly string Name;
        public readonly string Path;
        public readonly Vector3 Position;

        public MapSpec(string name, string path, Vector3 position)
        {
            Name = name;
            Path = path;
            Position = position;
        }
    }

    private readonly struct EntitySpec
    {
        public readonly string Name;
        public readonly string Path;
        public readonly Vector3 Position;
        public readonly float Scale;
        public readonly int SortingOrder;

        public EntitySpec(string name, string path, Vector3 position, float scale, int sortingOrder)
        {
            Name = name;
            Path = path;
            Position = position;
            Scale = scale;
            SortingOrder = sortingOrder;
        }
    }

    private static readonly MapSpec[] Maps =
    {
        new("Accepted_CloseGround_A", ExistingGroundPath, new Vector3(-4.2f, 0f, 0f)),
        new("Batch_CloseGround_B", BatchRoot + "/Ground/tactical_ground_batch_a_variant_b_pot.png", Vector3.zero),
        new("Batch_CloseGround_C", BatchRoot + "/Ground/tactical_ground_batch_a_variant_c_pot.png", new Vector3(4.2f, 0f, 0f)),
    };

    private static readonly EntitySpec[] Entities =
    {
        new("Batch_CommandBuilding", BatchRoot + "/Sprites/command_building.png", new Vector3(-0.92f, 0.18f, -0.05f), 0.14f, 20),
        new("Batch_TentCluster", BatchRoot + "/Sprites/tent_cluster.png", new Vector3(-0.35f, 0.36f, -0.05f), 0.13f, 21),
        new("Batch_FuelRefineryModule", BatchRoot + "/Sprites/fuel_refinery_module.png", new Vector3(0.60f, 0.12f, -0.05f), 0.30f, 22),
        new("Batch_BattleTank", BatchRoot + "/Sprites/battle_tank.png", new Vector3(-0.15f, -0.12f, -0.05f), 0.085f, 23),
        new("Batch_Apc", BatchRoot + "/Sprites/apc.png", new Vector3(0.35f, -0.35f, -0.05f), 0.095f, 24),
        new("Batch_InfantrySquad", BatchRoot + "/Sprites/infantry_squad.png", new Vector3(-0.62f, -0.27f, -0.05f), 0.10f, 25),
    };

    [MenuItem("WarlineCapture/Design/Build Tactical Production Batch A")]
    public static void BuildTacticalProductionBatchA()
    {
        AssetDatabase.Refresh();
        foreach (MapSpec map in Maps)
        {
            EnsureSpriteImport(map.Path, false, GroundPixelsPerUnit, 2048, TextureImporterCompression.CompressedHQ);
        }

        foreach (EntitySpec entity in Entities)
        {
            EnsureSpriteImport(entity.Path, true, EntityPixelsPerUnit, 1024, TextureImporterCompression.Uncompressed);
        }

        AssetDatabase.Refresh();
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"WARLINECAPTURE_TACTICAL_PRODUCTION_BATCH_A_BUILT scene={ScenePath} maps={Maps.Length} entities={Entities.Length}");
    }

    private static void BuildScene()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("TacticalProductionBatch_A");
        foreach (MapSpec map in Maps)
        {
            GameObject mapRoot = new(map.Name);
            mapRoot.transform.SetParent(root.transform, false);
            mapRoot.transform.localPosition = map.Position;

            Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(map.Path);
            if (groundSprite == null)
            {
                Debug.LogError($"WARLINECAPTURE_TACTICAL_PRODUCTION_BATCH_A_MISSING_MAP name={map.Name} path={map.Path}");
                continue;
            }

            GameObject ground = CreateSpriteObject("Ground_POT", groundSprite, 0);
            ground.transform.SetParent(mapRoot.transform, false);

            GameObject entityRoot = new("SeparateGeneratedEntities");
            entityRoot.transform.SetParent(mapRoot.transform, false);
            foreach (EntitySpec entity in Entities)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entity.Path);
                if (sprite == null)
                {
                    Debug.LogError($"WARLINECAPTURE_TACTICAL_PRODUCTION_BATCH_A_MISSING_ENTITY name={entity.Name} path={entity.Path}");
                    continue;
                }

                GameObject entityObject = CreateSpriteObject(entity.Name, sprite, entity.SortingOrder);
                entityObject.transform.SetParent(entityRoot.transform, false);
                entityObject.transform.localPosition = entity.Position;
                entityObject.transform.localScale = new Vector3(entity.Scale, entity.Scale, 1f);
                entityObject.AddComponent<BoxCollider2D>();
            }
        }

        CreateCamera("TacticalProductionBatch_A_ActiveCloseCamera_MapB", new Vector3(0f, 0f, -10f), CloseCameraOrthographicSize, true);
        CreateCamera("TacticalProductionBatch_A_CloseCamera_MapA", new Vector3(Maps[0].Position.x, 0f, -10f), CloseCameraOrthographicSize, false);
        CreateCamera("TacticalProductionBatch_A_CloseCamera_MapC", new Vector3(Maps[2].Position.x, 0f, -10f), CloseCameraOrthographicSize, false);
        CreateCamera("TacticalProductionBatch_A_FullReviewCamera", new Vector3(0f, 0f, -10f), 1.65f, false);
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
            Debug.LogError($"WARLINECAPTURE_TACTICAL_PRODUCTION_BATCH_A_IMPORTER_MISSING path={assetPath}");
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
            return false;

        importer.textureType = textureType;
        return true;
    }

    private static bool SetSpriteImportMode(TextureImporter importer, SpriteImportMode importMode)
    {
        if (importer.spriteImportMode == importMode)
            return false;

        importer.spriteImportMode = importMode;
        return true;
    }

    private static bool SetAlphaSource(TextureImporter importer, TextureImporterAlphaSource alphaSource)
    {
        if (importer.alphaSource == alphaSource)
            return false;

        importer.alphaSource = alphaSource;
        return true;
    }

    private static bool SetAlphaIsTransparency(TextureImporter importer, bool value)
    {
        if (importer.alphaIsTransparency == value)
            return false;

        importer.alphaIsTransparency = value;
        return true;
    }

    private static bool SetMipmapEnabled(TextureImporter importer, bool value)
    {
        if (importer.mipmapEnabled == value)
            return false;

        importer.mipmapEnabled = value;
        return true;
    }

    private static bool SetSrgbTexture(TextureImporter importer, bool value)
    {
        if (importer.sRGBTexture == value)
            return false;

        importer.sRGBTexture = value;
        return true;
    }

    private static bool SetFilterMode(TextureImporter importer, FilterMode value)
    {
        if (importer.filterMode == value)
            return false;

        importer.filterMode = value;
        return true;
    }

    private static bool SetTextureCompression(TextureImporter importer, TextureImporterCompression value)
    {
        if (importer.textureCompression == value)
            return false;

        importer.textureCompression = value;
        return true;
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
    }
}
#endif
