#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureTehranStrategicMapBuilder
{
    private const float PixelsPerUnit = 100f;
    private const string ArtRoot = "Assets/Game/Art/Generated/IsometricMaps/TehranStrategicMap_A";
    private const string PrefabRoot = "Assets/Game/Prefabs/Generated/IsometricMaps/TehranStrategicMap_A";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/TehranStrategicMap_A.unity";

    private readonly struct ViewSpec
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly Vector2 Position;
        public readonly bool EnabledCamera;

        public ViewSpec(string name, string assetPath, Vector2 position, bool enabledCamera)
        {
            Name = name;
            AssetPath = assetPath;
            Position = position;
            EnabledCamera = enabledCamera;
        }
    }

    private static readonly ViewSpec Overview = new(
        "TehranOverview_A",
        ArtRoot + "/Map/tehran_large_strategic_map_a.png",
        new Vector2(0f, 0f),
        true);

    private static readonly ViewSpec[] AreaViews =
    {
        new("TehranAirportHeliArea_A", ArtRoot + "/AreaViews/tehran_airport_heli_area_a.png", new Vector2(-9.5f, -13f), false),
        new("TehranTentCampArea_A", ArtRoot + "/AreaViews/tehran_tent_camp_area_a.png", new Vector2(9.5f, -13f), false),
        new("TehranVehicleTankDepot_A", ArtRoot + "/AreaViews/tehran_vehicle_tank_depot_a.png", new Vector2(-9.5f, -24f), false),
        new("TehranOilRefineryArea_A", ArtRoot + "/AreaViews/tehran_oil_refinery_area_a.png", new Vector2(9.5f, -24f), false),
    };

    [MenuItem("WarlineCapture/Design/Build Tehran Strategic Map A")]
    public static void BuildTehranStrategicMapA()
    {
        AssetDatabase.Refresh();

        EnsureSpriteImport(Overview.AssetPath, 2048);
        foreach (ViewSpec area in AreaViews)
        {
            EnsureSpriteImport(area.AssetPath, 2048);
        }

        AssetDatabase.Refresh();

        Directory.CreateDirectory(ProjectPath(PrefabRoot));
        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));

        SavePrefab(Overview);
        foreach (ViewSpec area in AreaViews)
        {
            SavePrefab(area);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_TEHRAN_STRATEGIC_MAP_A_BUILT scene={ScenePath} overview={Overview.AssetPath} areaViews={AreaViews.Length}");
    }

    private static void BuildScene()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("TehranStrategicMap_A");

        GameObject overviewRoot = new("Overview");
        overviewRoot.transform.SetParent(root.transform, false);
        GameObject overview = CreateViewObject(Overview, 0);
        overview.transform.SetParent(overviewRoot.transform, false);
        overview.transform.localPosition = new Vector3(Overview.Position.x, Overview.Position.y, 0f);

        GameObject areaRoot = new("TacticalAreaViews");
        areaRoot.transform.SetParent(root.transform, false);
        foreach (ViewSpec area in AreaViews)
        {
            GameObject areaObject = CreateViewObject(area, 0);
            areaObject.transform.SetParent(areaRoot.transform, false);
            areaObject.transform.localPosition = new Vector3(area.Position.x, area.Position.y, 0f);
        }

        BuildCamera(Overview);
        foreach (ViewSpec area in AreaViews)
        {
            BuildCamera(area);
        }

        Selection.activeObject = root;
    }

    private static GameObject CreateViewObject(ViewSpec view, int sortingOrder)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(view.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_TEHRAN_STRATEGIC_MAP_A_MISSING_SPRITE name={view.Name} path={view.AssetPath}");
            return new GameObject(view.Name + "_Missing");
        }

        GameObject spriteObject = new(view.Name);
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.drawMode = SpriteDrawMode.Simple;
        return spriteObject;
    }

    private static void SavePrefab(ViewSpec view)
    {
        GameObject prefab = CreateViewObject(view, 0);
        PrefabUtility.SaveAsPrefabAsset(prefab, PrefabRoot + "/" + view.Name + ".prefab");
        Object.DestroyImmediate(prefab);
    }

    private static void BuildCamera(ViewSpec view)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(view.AssetPath);
        float orthographicSize = 4.9f;
        if (sprite != null)
        {
            orthographicSize = sprite.rect.height / PixelsPerUnit * 0.52f;
        }

        GameObject cameraObject = new(view.Name + "_Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = view.EnabledCamera;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.027f, 0.025f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(view.Position.x, view.Position.y, -10f);
        camera.transform.rotation = Quaternion.identity;

        if (view.EnabledCamera)
        {
            Camera.SetupCurrent(camera);
        }
    }

    private static void EnsureSpriteImport(string assetPath, int maxTextureSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_TEHRAN_STRATEGIC_MAP_A_IMPORTER_MISSING path={assetPath}");
            return;
        }

        bool changed = false;
        changed |= SetTextureType(importer, TextureImporterType.Sprite);
        changed |= SetSpriteImportMode(importer, SpriteImportMode.Single);
        changed |= SetAlphaSource(importer, TextureImporterAlphaSource.None);
        changed |= SetAlphaIsTransparency(importer, false);
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
