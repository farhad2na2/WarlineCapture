#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureModularRoad4TileSetBuilder
{
    private const float PixelsPerUnit = 100f;
    private const float TileStepX = 6.2f;
    private const float TileStepY = 4.8f;
    private const float SpriteCanvasCenterPx = 627f;
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/ModularRoad4";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/ModularRoad4TileSetPreview.unity";

    private static readonly TileDefinition[] Road4Tiles =
    {
        new(
            "CityBlockRoad4_BasePads",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoad4_D_DiamondIsoPadded/city_block_road4_diamond_iso_padded_d.png",
            new Vector2(626.5f, 624.5f)),
        new(
            "CityBlockRoad4_Park",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoad4_E_Variants/city_block_road4_park_e.png",
            new Vector2(626.0f, 605.5f)),
        new(
            "CityBlockRoad4_Airport",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoad4_E_Variants/city_block_road4_airport_e.png",
            new Vector2(628.5f, 613.0f)),
        new(
            "CityBlockRoad4_LargePad",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoad4_E_Variants/city_block_road4_large_pad_e.png",
            new Vector2(628.5f, 626.5f)),
        new(
            "CityBlockRoad4_MixedLots",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoad4_E_Variants/city_block_road4_mixed_lots_e.png",
            new Vector2(626.0f, 601.5f)),
        new(
            "CityBlockRoad4_ParkLake",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoadSockets_F_Expanded/road4_park_lake_f.png",
            new Vector2(628.0f, 607.5f)),
        new(
            "CityBlockRoad4_CanalDistrict",
            "Assets/Game/Art/Generated/IsometricMaps/ModularIsoTiles/CityBlockRoadSockets_F_Expanded/road4_canal_district_f.png",
            new Vector2(628.0f, 607.5f)),
    };

    [MenuItem("WarlineCapture/Design/Build Modular ROAD4 Iso Tile Set")]
    public static void BuildModularRoad4TileSet()
    {
        Directory.CreateDirectory(ProjectPath(PrefabRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));

        foreach (TileDefinition tile in Road4Tiles)
        {
            EnsureSpriteImport(tile.SpritePath);
        }

        AssetDatabase.Refresh();

        foreach (TileDefinition tile in Road4Tiles)
        {
            SaveTilePrefab(tile);
        }

        BuildPreviewScene();
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_MODULAR_ROAD4_TILE_SET_BUILT tiles={Road4Tiles.Length} scene={ScenePath}");
    }

    private static void SaveTilePrefab(TileDefinition tile)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(tile.SpritePath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_MODULAR_ROAD4_TILE_MISSING sprite={tile.SpritePath}");
            return;
        }

        GameObject root = CreateTileObject(tile.Name, sprite, tile.SpriteOffsetUnits, 0);
        string prefabPath = $"{PrefabRoot}/{tile.Name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void BuildPreviewScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("ModularRoad4TileSetPreview");
        Bounds combinedBounds = new(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        int[,] layout =
        {
            { 0, 1, 2 },
            { 3, 4, 5 },
            { 6, 0, 1 },
        };

        for (int row = 0; row < layout.GetLength(0); row++)
        {
            for (int col = 0; col < layout.GetLength(1); col++)
            {
                TileDefinition tile = Road4Tiles[layout[row, col]];
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(tile.SpritePath);
                if (sprite == null)
                {
                    continue;
                }

                Vector3 position = IsoGridPosition(col, row);
                GameObject tileObject = CreateTileObject($"{tile.Name}_{row}_{col}", sprite, tile.SpriteOffsetUnits, row * 10 + col);
                tileObject.transform.SetParent(root.transform, false);
                tileObject.transform.position = position;

                Bounds bounds = tileObject.GetComponentInChildren<SpriteRenderer>().bounds;
                if (!hasBounds)
                {
                    combinedBounds = bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(bounds);
                }
            }
        }

        CreateSocketLegend(root.transform);
        CreatePreviewCamera(combinedBounds);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
    }

    private static GameObject CreateTileObject(string name, Sprite sprite, Vector2 spriteOffsetUnits, int sortingOrder)
    {
        GameObject root = new(name);
        GameObject spriteObject = new("Sprite");
        spriteObject.transform.SetParent(root.transform, false);
        spriteObject.transform.localPosition = new Vector3(spriteOffsetUnits.x, spriteOffsetUnits.y, 0f);

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;

        ModularIsoTileMetadata metadata = root.AddComponent<ModularIsoTileMetadata>();
        metadata.SocketUpperLeft = true;
        metadata.SocketUpperRight = true;
        metadata.SocketLowerLeft = true;
        metadata.SocketLowerRight = true;
        metadata.SourceSpritePath = AssetDatabase.GetAssetPath(sprite);

        return root;
    }

    private static Vector3 IsoGridPosition(int col, int row)
    {
        return new Vector3((col - row) * TileStepX, -(col + row) * TileStepY, 0f);
    }

    private static void CreatePreviewCamera(Bounds bounds)
    {
        GameObject cameraObject = new("ModularRoad4TileSetPreview_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.orthographicSize = Mathf.Max(7f, bounds.extents.y * 1.12f);
    }

    private static void CreateSocketLegend(Transform parent)
    {
        GameObject legend = new("SocketLegend_ROAD4_AllTiles");
        legend.transform.SetParent(parent, false);
        ModularIsoTileMetadata metadata = legend.AddComponent<ModularIsoTileMetadata>();
        metadata.SocketUpperLeft = true;
        metadata.SocketUpperRight = true;
        metadata.SocketLowerLeft = true;
        metadata.SocketLowerRight = true;
        metadata.SourceSpritePath = "ROAD4: all four diamond-edge road sockets are enabled and intended to connect.";
    }

    private static void EnsureSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_MODULAR_ROAD4_IMPORTER_MISSING path={assetPath}");
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

        if (Math.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 0.001f)
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

    private static bool SetAlphaIsTransparency(TextureImporter importer, bool alphaIsTransparency)
    {
        if (importer.alphaIsTransparency == alphaIsTransparency)
        {
            return false;
        }

        importer.alphaIsTransparency = alphaIsTransparency;
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

    private readonly struct TileDefinition
    {
        public TileDefinition(string name, string spritePath, Vector2 visibleCenterPx)
        {
            Name = name;
            SpritePath = spritePath;
            SpriteOffsetUnits = new Vector2(
                (SpriteCanvasCenterPx - visibleCenterPx.x) / PixelsPerUnit,
                (visibleCenterPx.y - SpriteCanvasCenterPx) / PixelsPerUnit);
        }

        public string Name { get; }
        public string SpritePath { get; }
        public Vector2 SpriteOffsetUnits { get; }
    }
}
#endif
