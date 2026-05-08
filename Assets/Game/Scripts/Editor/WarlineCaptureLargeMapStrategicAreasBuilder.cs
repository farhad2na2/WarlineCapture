#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureLargeMapStrategicAreasBuilder
{
    private const float PixelsPerUnit = 100f;
    private const string ArtRoot = "Assets/Game/Art/Generated/IsometricMaps/LargeMapStrategicAreas_A";
    private const string MapPath = ArtRoot + "/Map/large_tactical_island_map_a.png";
    private const string OverlayRoot = ArtRoot + "/AreaOverlays/Sprites";
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/LargeMapStrategicAreas_A";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/LargeMapStrategicAreas_A.unity";

    private readonly struct OverlaySpec
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly Vector2 PixelPosition;
        public readonly float Scale;
        public readonly int SortingOrder;

        public OverlaySpec(string name, string assetPath, Vector2 pixelPosition, float scale, int sortingOrder)
        {
            Name = name;
            AssetPath = assetPath;
            PixelPosition = pixelPosition;
            Scale = scale;
            SortingOrder = sortingOrder;
        }
    }

    private static readonly OverlaySpec[] Overlays =
    {
        new("StrategicTentCamp_A", OverlayRoot + "/soldier_tent_camp_a.png", new Vector2(405f, 294f), 0.36f, 20),
        new("StrategicVehicleTankDepot_A", OverlayRoot + "/vehicle_tank_depot_a.png", new Vector2(365f, 505f), 0.34f, 21),
        new("StrategicOilRefineryFuel_A", OverlayRoot + "/oil_refinery_fuel_a.png", new Vector2(1195f, 522f), 0.40f, 22),
        new("StrategicAirportHeliArea_A", OverlayRoot + "/airport_heli_area_a.png", new Vector2(1252f, 245f), 0.34f, 23),
    };

    [MenuItem("WarlineCapture/Design/Build Large Map Strategic Areas A")]
    public static void BuildLargeMapStrategicAreasA()
    {
        AssetDatabase.Refresh();
        EnsureSpriteImport(MapPath, false, 2048);
        foreach (OverlaySpec overlay in Overlays)
        {
            EnsureSpriteImport(overlay.AssetPath, true, 1024);
        }

        AssetDatabase.Refresh();

        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapPath);
        if (mapSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_LARGE_MAP_STRATEGIC_A_MISSING_MAP path={MapPath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(PrefabRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));

        SaveOverlayPrefabs();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(mapSprite);
        BuildCameras(mapSprite);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_LARGE_MAP_STRATEGIC_A_BUILT scene={ScenePath} overlays={Overlays.Length}");
    }

    private static void BuildScene(Sprite mapSprite)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("LargeMapStrategicAreas_A");

        GameObject mapObject = CreateSpriteObject("ApprovedLargeMap_A", mapSprite, 0);
        mapObject.transform.SetParent(root.transform, false);
        mapObject.transform.localPosition = Vector3.zero;

        GameObject overlayRoot = new("GeneratedStrategicAreaOverlays_A");
        overlayRoot.transform.SetParent(root.transform, false);

        foreach (OverlaySpec overlay in Overlays)
        {
            GameObject overlayObject = CreateOverlayObject(overlay, mapSprite);
            if (overlayObject == null)
            {
                continue;
            }

            overlayObject.transform.SetParent(overlayRoot.transform, false);
        }

        Selection.activeObject = root;
    }

    private static GameObject CreateOverlayObject(OverlaySpec overlay, Sprite mapSprite)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(overlay.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_LARGE_MAP_STRATEGIC_A_MISSING_OVERLAY name={overlay.Name} path={overlay.AssetPath}");
            return null;
        }

        GameObject overlayObject = CreateSpriteObject(overlay.Name, sprite, overlay.SortingOrder);
        overlayObject.transform.localPosition = PixelToWorld(overlay.PixelPosition, mapSprite);
        overlayObject.transform.localScale = new Vector3(overlay.Scale, overlay.Scale, 1f);
        return overlayObject;
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

    private static void SaveOverlayPrefabs()
    {
        foreach (OverlaySpec overlay in Overlays)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(overlay.AssetPath);
            if (sprite == null)
            {
                continue;
            }

            GameObject prefab = CreateSpriteObject(overlay.Name, sprite, overlay.SortingOrder);
            prefab.transform.localScale = new Vector3(overlay.Scale, overlay.Scale, 1f);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/" + overlay.Name + ".prefab");
            Object.DestroyImmediate(prefab);
        }
    }

    private static void BuildCameras(Sprite mapSprite)
    {
        Camera fullMapCamera = CreateCamera("LargeMapStrategicAreas_A_FullMapCamera", new Vector3(0f, 0f, -10f), mapSprite.rect.height / PixelsPerUnit * 0.52f);
        Camera.SetupCurrent(fullMapCamera);

        CreateCamera("LargeMapStrategicAreas_A_TentCampCamera", PixelToWorld(new Vector2(405f, 294f), mapSprite) + new Vector3(0f, 0f, -9.95f), 1.75f).enabled = false;
        CreateCamera("LargeMapStrategicAreas_A_VehicleDepotCamera", PixelToWorld(new Vector2(365f, 505f), mapSprite) + new Vector3(0f, 0f, -9.95f), 1.75f).enabled = false;
        CreateCamera("LargeMapStrategicAreas_A_RefineryCamera", PixelToWorld(new Vector2(1195f, 522f), mapSprite) + new Vector3(0f, 0f, -9.95f), 1.75f).enabled = false;
        CreateCamera("LargeMapStrategicAreas_A_AirportCamera", PixelToWorld(new Vector2(1252f, 245f), mapSprite) + new Vector3(0f, 0f, -9.95f), 1.75f).enabled = false;
    }

    private static Camera CreateCamera(string name, Vector3 position, float orthographicSize)
    {
        GameObject cameraObject = new(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.01f, 0.04f, 0.055f, 1f);
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
            Debug.LogError($"WARLINECAPTURE_LARGE_MAP_STRATEGIC_A_IMPORTER_MISSING path={assetPath}");
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
