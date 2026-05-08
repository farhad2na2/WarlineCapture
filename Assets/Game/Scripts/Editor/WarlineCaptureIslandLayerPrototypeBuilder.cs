#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureIslandLayerPrototypeBuilder
{
    private const float PixelsPerUnit = 100f;
    private const string AssetRoot = "Assets/Game/Art/Generated/IsometricMaps/IslandLayerPrototype_B";
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/IslandLayerPrototype_B";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/IslandLayerPrototype_B.unity";
    private const string OceanPath = AssetRoot + "/Ocean/ocean_reference_style_seamless_4096_v2.png";
    private const string OceanMaterialPath = PrefabRoot + "/OceanReferenceStyle_B.mat";
    private const float OceanTextureRepeat = 4f;

    private readonly struct IslandSpec
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly string ShorelinePath;
        public readonly Vector2 Position;
        public readonly float Scale;
        public readonly int SortingOrder;

        public IslandSpec(string name, string assetPath, string shorelinePath, Vector2 position, float scale, int sortingOrder)
        {
            Name = name;
            AssetPath = assetPath;
            ShorelinePath = shorelinePath;
            Position = position;
            Scale = scale;
            SortingOrder = sortingOrder;
        }
    }

    private static readonly IslandSpec[] Islands =
    {
        new("CityIsland_B", AssetRoot + "/Islands/island_city_b.png", AssetRoot + "/Shoreline/island_city_b_coastal_water.png", new Vector2(-8.25f, 7.25f), 1f, 10),
        new("AirportIsland_B", AssetRoot + "/Islands/island_airport_b.png", AssetRoot + "/Shoreline/island_airport_b_coastal_water.png", new Vector2(8.25f, 7.25f), 1f, 11),
        new("CanalPortIsland_B", AssetRoot + "/Islands/island_canal_port_b.png", AssetRoot + "/Shoreline/island_canal_port_b_coastal_water.png", new Vector2(-8.25f, -7.75f), 1f, 12),
        new("ParkResortIsland_B", AssetRoot + "/Islands/island_park_resort_b.png", AssetRoot + "/Shoreline/island_park_resort_b_coastal_water.png", new Vector2(8.25f, -7.75f), 1f, 13),
    };

    [MenuItem("WarlineCapture/Design/Build Island Layer Prototype B")]
    public static void BuildIslandLayerPrototypeB()
    {
        AssetDatabase.Refresh();

        EnsureOceanTextureImport(OceanPath, 4096);
        foreach (IslandSpec island in Islands)
        {
            EnsureSpriteImport(island.AssetPath, true, 2048);
            EnsureSpriteImport(island.ShorelinePath, true, 2048);
        }

        AssetDatabase.Refresh();

        Texture2D oceanTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OceanPath);
        if (oceanTexture == null)
        {
            Debug.LogError($"WARLINECAPTURE_ISLAND_LAYER_MISSING_OCEAN_TEXTURE path={OceanPath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(PrefabRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));

        Material oceanMaterial = SaveOceanMaterial(oceanTexture);
        SaveOceanPrefab(oceanMaterial);
        foreach (IslandSpec island in Islands)
        {
            SaveIslandPrefab(island);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(oceanMaterial);
        Camera camera = CreateCamera();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_ISLAND_LAYER_PROTOTYPE_B_BUILT scene={ScenePath} ocean={OceanPath} islands={Islands.Length} cameraOrtho={camera.orthographicSize:F2}");
    }

    private static void BuildScene(Material oceanMaterial)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("IslandLayerPrototype_B");
        GameObject ocean = CreateOceanObject("OceanReferenceStyle_Tiled_B", oceanMaterial);
        ocean.transform.SetParent(root.transform, false);

        foreach (IslandSpec island in Islands)
        {
            Sprite shorelineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(island.ShorelinePath);
            if (shorelineSprite != null)
            {
                GameObject shorelineObject = CreateSpriteObject(island.Name + "_CoastalWater", shorelineSprite, island.SortingOrder - 5);
                shorelineObject.transform.SetParent(root.transform, false);
                shorelineObject.transform.localPosition = new Vector3(island.Position.x, island.Position.y, 0f);
                shorelineObject.transform.localScale = new Vector3(island.Scale, island.Scale, 1f);
            }
            else
            {
                Debug.LogWarning($"WARLINECAPTURE_ISLAND_LAYER_MISSING_SHORELINE name={island.Name} path={island.ShorelinePath}");
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(island.AssetPath);
            if (sprite == null)
            {
                Debug.LogError($"WARLINECAPTURE_ISLAND_LAYER_MISSING_ISLAND name={island.Name} path={island.AssetPath}");
                continue;
            }

            GameObject islandObject = CreateSpriteObject(island.Name, sprite, island.SortingOrder);
            islandObject.transform.SetParent(root.transform, false);
            islandObject.transform.localPosition = new Vector3(island.Position.x, island.Position.y, 0f);
            islandObject.transform.localScale = new Vector3(island.Scale, island.Scale, 1f);
        }

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

    private static Material SaveOceanMaterial(Texture2D oceanTexture)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(OceanMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, OceanMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.mainTexture = oceanTexture;
        material.mainTexture.wrapMode = TextureWrapMode.Repeat;
        material.mainTexture.filterMode = FilterMode.Bilinear;
        material.mainTextureScale = new Vector2(OceanTextureRepeat, OceanTextureRepeat);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void SaveOceanPrefab(Material oceanMaterial)
    {
        GameObject prefab = CreateOceanObject("OceanReferenceStyle_Tiled_B", oceanMaterial);
        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/OceanReferenceStyle_Tiled_B.prefab");
        Object.DestroyImmediate(prefab);
    }

    private static GameObject CreateOceanObject(string name, Material material)
    {
        const float width = 62f;
        const float height = 42f;
        const float tileX = 1f;
        const float tileY = 1f;

        GameObject ocean = new(name);
        ocean.transform.localPosition = new Vector3(0f, 0f, 0.15f);

        Mesh mesh = new()
        {
            name = "OceanReferenceStyle_TiledMesh_B",
            vertices = new[]
            {
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, -height * 0.5f, 0f),
                new Vector3(-width * 0.5f, height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f),
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(tileX, 0f),
                new Vector2(0f, tileY),
                new Vector2(tileX, tileY),
            },
            triangles = new[] { 0, 2, 1, 2, 3, 1 },
        };
        mesh.RecalculateBounds();

        MeshFilter meshFilter = ocean.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = ocean.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 0;
        return ocean;
    }

    private static void SaveIslandPrefab(IslandSpec island)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(island.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_ISLAND_LAYER_PREFAB_SKIPPED name={island.Name} path={island.AssetPath}");
            return;
        }

        GameObject prefab = new(island.Name);
        Sprite shorelineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(island.ShorelinePath);
        if (shorelineSprite != null)
        {
            GameObject shorelineObject = CreateSpriteObject(island.Name + "_CoastalWater", shorelineSprite, island.SortingOrder - 5);
            shorelineObject.transform.SetParent(prefab.transform, false);
        }

        GameObject islandObject = CreateSpriteObject(island.Name + "_Art", sprite, island.SortingOrder);
        islandObject.transform.SetParent(prefab.transform, false);

        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/" + island.Name + ".prefab");
        Object.DestroyImmediate(prefab);
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new("IslandLayerPrototype_B_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.05f, 0.07f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(0f, -0.25f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.orthographicSize = 18.25f;
        Camera.SetupCurrent(camera);
        return camera;
    }

    private static void EnsureSpriteImport(string assetPath, bool alpha, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_ISLAND_LAYER_IMPORTER_MISSING path={assetPath}");
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
        changed |= EnsurePlatformSettings(importer, "Android", true, maxTextureSize, alpha ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_6x6);

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static void EnsureOceanTextureImport(string assetPath, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_ISLAND_LAYER_IMPORTER_MISSING path={assetPath}");
            return;
        }

        bool changed = false;
        changed |= SetTextureType(importer, TextureImporterType.Default);
        changed |= SetAlphaSource(importer, TextureImporterAlphaSource.None);
        changed |= SetAlphaIsTransparency(importer, false);
        changed |= SetMipmapEnabled(importer, true);
        changed |= SetSrgbTexture(importer, true);
        changed |= SetFilterMode(importer, FilterMode.Bilinear);
        changed |= SetWrapMode(importer, TextureWrapMode.Repeat);
        changed |= SetTextureCompression(importer, TextureImporterCompression.CompressedHQ);
        changed |= EnsurePlatformSettings(importer, "DefaultTexturePlatform", false, maxTextureSize, TextureImporterFormat.Automatic);
        changed |= EnsurePlatformSettings(importer, "Android", true, maxTextureSize, TextureImporterFormat.ASTC_4x4);

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

    private static bool SetWrapMode(TextureImporter importer, TextureWrapMode wrapMode)
    {
        if (importer.wrapMode == wrapMode)
        {
            return false;
        }

        importer.wrapMode = wrapMode;
        return true;
    }

    private static string ProjectPath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
    }
}
#endif
