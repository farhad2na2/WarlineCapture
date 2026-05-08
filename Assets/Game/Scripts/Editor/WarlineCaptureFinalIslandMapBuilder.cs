#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureFinalIslandMapBuilder
{
    private const string MapSpritePath = "Assets/Game/Art/Generated/IsometricMaps/IslandMapTargets/FinalIsland_A/final_island_map_a.png";
    private const string PrefabPath = "Assets/Game/Prefabs/Generated/IsometricMaps/FinalIslandMap_A.prefab";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/FinalIslandMap_A.unity";
    private const float PixelsPerUnit = 100f;

    [MenuItem("WarlineCapture/Design/Build Final Island Map A")]
    public static void BuildFinalIslandMapA()
    {
        EnsureMapSpriteImport();
        AssetDatabase.Refresh();

        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapSpritePath);
        if (mapSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_FINAL_ISLAND_MAP_MISSING sprite={MapSpritePath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(PrefabPath)));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));

        GameObject prefabRoot = CreateMapRoot(mapSprite);
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject sceneRoot = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) as GameObject;
        if (sceneRoot != null)
        {
            sceneRoot.name = "FinalIslandMap_A";
            sceneRoot.transform.position = Vector3.zero;
        }

        Camera camera = CreateCamera(mapSprite);
        Selection.activeObject = sceneRoot;

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_FINAL_ISLAND_MAP_BUILT sprite={MapSpritePath} prefab={PrefabPath} scene={ScenePath} cameraOrtho={camera.orthographicSize:F2}");
    }

    private static GameObject CreateMapRoot(Sprite mapSprite)
    {
        GameObject root = new("FinalIslandMap_A");
        root.transform.position = Vector3.zero;

        GameObject mapObject = new("IslandMapSprite");
        mapObject.transform.SetParent(root.transform, false);
        mapObject.transform.position = Vector3.zero;

        SpriteRenderer renderer = mapObject.AddComponent<SpriteRenderer>();
        renderer.sprite = mapSprite;
        renderer.sortingOrder = 0;
        renderer.drawMode = SpriteDrawMode.Simple;

        GameObject boundsObject = new("GameplayBoundsGuide");
        boundsObject.transform.SetParent(root.transform, false);
        boundsObject.transform.position = new Vector3(0f, 0f, -0.02f);
        SpriteRenderer boundsRenderer = boundsObject.AddComponent<SpriteRenderer>();
        boundsRenderer.sprite = mapSprite;
        boundsRenderer.color = new Color(1f, 1f, 1f, 0.001f);
        boundsRenderer.sortingOrder = -1;

        return root;
    }

    private static Camera CreateCamera(Sprite mapSprite)
    {
        GameObject cameraObject = new("FinalIslandMap_A_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.03f, 0.04f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;

        float mapHeightUnits = mapSprite.rect.height / PixelsPerUnit;
        camera.orthographicSize = mapHeightUnits * 0.52f;
        Camera.SetupCurrent(camera);
        return camera;
    }

    private static void EnsureMapSpriteImport()
    {
        TextureImporter importer = AssetImporter.GetAtPath(MapSpritePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_FINAL_ISLAND_MAP_IMPORTER_MISSING path={MapSpritePath}");
            return;
        }

        bool changed = false;
        changed |= SetTextureType(importer, TextureImporterType.Sprite);
        changed |= SetSpriteImportMode(importer, SpriteImportMode.Single);
        changed |= SetAlphaSource(importer, TextureImporterAlphaSource.None);
        changed |= SetMipmapEnabled(importer, false);
        changed |= SetSrgbTexture(importer, true);
        changed |= SetFilterMode(importer, FilterMode.Bilinear);
        changed |= SetTextureCompression(importer, TextureImporterCompression.CompressedHQ);

        if (importer.spritePixelsPerUnit != PixelsPerUnit)
        {
            importer.spritePixelsPerUnit = PixelsPerUnit;
            changed = true;
        }

        changed |= EnsurePlatformSettings(importer, "DefaultTexturePlatform", false, 2048, TextureImporterFormat.Automatic);
        changed |= EnsurePlatformSettings(importer, "Android", true, 2048, TextureImporterFormat.ASTC_6x6);

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
