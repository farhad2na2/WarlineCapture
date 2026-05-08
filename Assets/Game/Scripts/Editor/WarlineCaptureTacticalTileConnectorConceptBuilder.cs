#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarlineCaptureTacticalTileConnectorConceptBuilder
{
    private const float PixelsPerUnit = 300f;
    private const string ConceptPath = "Assets/Game/Art/Generated/IsometricMaps/TacticalTileConnectorConcept_A/tactical_tile_connector_concept_a.png";
    private const string ScenePath = "Assets/Game/Scenes/DesignTargets/FinalMaps/TacticalTileConnectorConcept_A.unity";

    [MenuItem("WarlineCapture/Design/Build Tactical Tile Connector Concept A")]
    public static void BuildTacticalTileConnectorConceptA()
    {
        AssetDatabase.Refresh();
        EnsureSpriteImport();
        AssetDatabase.Refresh();

        Sprite conceptSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ConceptPath);
        if (conceptSprite == null)
        {
            Debug.LogError($"WARLINECAPTURE_TACTICAL_TILE_CONNECTOR_CONCEPT_MISSING path={ConceptPath}");
            return;
        }

        Directory.CreateDirectory(ProjectPath(Path.GetDirectoryName(ScenePath)));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.skybox = null;

        GameObject root = new("TacticalTileConnectorConcept_A");
        GameObject board = new("GeneratedConnectorConcept_Board");
        board.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = board.AddComponent<SpriteRenderer>();
        renderer.sprite = conceptSprite;
        renderer.sortingOrder = 0;

        float fullCameraSize = conceptSprite.rect.height / PixelsPerUnit * 0.52f;
        CreateCamera("TacticalTileConnectorConcept_A_FullCamera", new Vector3(0f, 0f, -10f), fullCameraSize, true);
        CreateCamera("TacticalTileConnectorConcept_A_CloseConnectorCamera", new Vector3(0.15f, -0.08f, -10f), 0.82f, false);

        Selection.activeObject = root;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"WARLINECAPTURE_TACTICAL_TILE_CONNECTOR_CONCEPT_BUILT scene={ScenePath} asset={ConceptPath}");
    }

    private static Camera CreateCamera(string name, Vector3 position, float orthographicSize, bool enabled)
    {
        GameObject cameraObject = new(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = enabled;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.052f, 0.047f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.identity;
        return camera;
    }

    private static void EnsureSpriteImport()
    {
        TextureImporter importer = AssetImporter.GetAtPath(ConceptPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"WARLINECAPTURE_TACTICAL_TILE_CONNECTOR_CONCEPT_IMPORTER_MISSING path={ConceptPath}");
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
