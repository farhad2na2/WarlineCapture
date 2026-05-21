using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WarlineCaptureScn02LayerCanvasBuilder
{
    private const string ManifestPath = "Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json";
    private const string LayoutPath = "Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json";
    private const string StandaloneImplementationRoot = "Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets";
    private const string ComponentPlateRoot = "Design/VisualLockLayered/SCN-02_MainMenu/component_plates_20260519/assets";
    private const string GeneratedRoot = "Assets/Game/Art/UI/Generated/MainMenu/ComponentCanvas";
    private const string PrefabPath = "Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_ComponentCanvasTest.prefab";
    private const string CapturePath3840 = "Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png";
    private const string BoldFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Bold SDF.asset";
    private const string LightFontPath = "Assets/Synty/InterfaceMilitaryCombatHUD/Fonts/Oxanium/Oxanium-Light SDF.asset";

    private static readonly HashSet<string> FixedSimpleLayerIds = new HashSet<string>
    {
        "screen_shell_frame",
        "brand_logo_panel_frame",
        "top_resource_bar_frame_full",
        "settings_button_frame",
        "settings_gear_icon",
        "main_menu_background_tactical_map",
        "deploy_command_button_frame",
        "left_nav_row_frame",
        "operation_warning_row_frame",
    };

    [MenuItem("WarlineCapture/Design/SCN-02/Build Component Canvas Test")]
    public static void BuildLayerCanvasTest()
    {
        LayerRequest request = LoadManifest();
        CleanGeneratedOutputRoot();
        EnsureBatch01Sprites(request);
        EnsureStandaloneImplementationSprites();
        GameObject root = BuildPrefabRoot(request);

        EnsureFolder("Assets/Game/Prefabs/UI", "Screens");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SCN-02 ComponentCanvas] Built {PrefabPath}");
    }

    [MenuItem("WarlineCapture/Design/SCN-02/Capture Component Canvas Test")]
    public static void CaptureLayerCanvasTest()
    {
        BuildLayerCanvasTest();
        CapturePrefab(PrefabPath, CapturePath3840, 3840, 2160, new Color(0.004f, 0.018f, 0.022f, 1f));
    }

    private static LayerRequest LoadManifest()
    {
        if (!File.Exists(ManifestPath))
            throw new FileNotFoundException($"SCN-02 layer request manifest missing: {ManifestPath}");

        LayerRequest request = JsonUtility.FromJson<LayerRequest>(File.ReadAllText(ManifestPath));
        if (request == null || request.assets == null || request.reference == null)
            throw new InvalidOperationException($"SCN-02 layer request manifest is invalid: {ManifestPath}");

        if (request.reference.targetCanvas == null || request.reference.targetCanvas.Length < 2)
            request.reference.targetCanvas = new[] { 3840, 2160 };

        return request;
    }

    private static void EnsureBatch01Sprites(LayerRequest request)
    {
        foreach (LayerAsset asset in request.assets)
        {
            string sourcePath = ResolveSourcePath(asset);
            if (string.IsNullOrEmpty(sourcePath))
                continue;

            string destinationPath = GetGeneratedAssetPath(asset);
            EnsureParentFolder(destinationPath);

            CopyPng(sourcePath, destinationPath);

            ImportSprite(destinationPath, asset);
        }
    }

    private static string ResolveSourcePath(LayerAsset asset)
    {
        string standalonePath = Path.Combine(StandaloneImplementationRoot, $"{asset.id}.png");
        return File.Exists(standalonePath) ? standalonePath : null;
    }

    private static bool IsOpaqueAsset(LayerAsset asset)
    {
        return asset.background == "opaque" || asset.type == "background" || asset.type == "reference-preview";
    }

    private static void CleanGeneratedOutputRoot()
    {
        if (Directory.Exists(GeneratedRoot))
        {
            FileUtil.DeleteFileOrDirectory(GeneratedRoot);
            FileUtil.DeleteFileOrDirectory($"{GeneratedRoot}.meta");
        }

        AssetDatabase.Refresh();
    }

    private static string GetGeneratedAssetPath(LayerAsset asset)
    {
        string folder = "Frames";
        if (asset.type == "background")
            folder = "Backgrounds";
        else if (asset.type == "reference-preview")
            folder = "References";
        else if (ContainsIgnoreCase(asset.type, "button"))
            folder = "Buttons";
        else if (asset.type == "icon")
            folder = "Icons";
        else if (ContainsIgnoreCase(asset.type, "overlay"))
            folder = "Overlays";
        else if (ContainsIgnoreCase(asset.type, "content"))
            folder = "Content";

        return $"{GeneratedRoot}/{folder}/{asset.id}.png";
    }

    private static string GetGeneratedCleanedAssetPath(string fileName)
    {
        return $"{GeneratedRoot}/Cleaned/{fileName}";
    }

    private static void EnsureStandaloneImplementationSprites()
    {
        CopyImplementationSpritesFromRoot(StandaloneImplementationRoot);
        CopyImplementationSpritesFromRoot(ComponentPlateRoot);
    }

    private static void CopyImplementationSpritesFromRoot(string sourceRoot)
    {
        if (!Directory.Exists(sourceRoot))
            return;

        foreach (string sourcePath in Directory.GetFiles(sourceRoot, "*.png"))
        {
            string fileName = Path.GetFileName(sourcePath);
            if (fileName == "full_visual_lock_preview.png")
                continue;

            string destinationPath = GetGeneratedCleanedAssetPath(fileName);
            EnsureParentFolder(destinationPath);
            CopyPng(sourcePath, destinationPath);

            string id = Path.GetFileNameWithoutExtension(fileName);
            LayerAsset layer = new LayerAsset
            {
                id = id,
                type = InferCleanedAssetType(id),
                background = IsCleanedOpaqueAsset(fileName) ? "opaque" : "transparent-or-chromakey"
            };
            ImportSprite(destinationPath, layer);
        }
    }

    private static string InferCleanedAssetType(string id)
    {
        if (ContainsIgnoreCase(id, "background") || id == "full_visual_lock_preview")
            return "background";
        if (ContainsIgnoreCase(id, "art") || ContainsIgnoreCase(id, "portrait"))
            return "content-image";
        if (ContainsIgnoreCase(id, "icon") || ContainsIgnoreCase(id, "gear") || ContainsIgnoreCase(id, "chevron"))
            return "icon";
        return "sliced-frame";
    }

    private static bool IsCleanedOpaqueAsset(string fileName)
    {
        return fileName == "full_visual_lock_preview.png"
            || fileName == "main_menu_background_tactical_map.png"
            || fileName == "commander_profile_portrait.png"
            || fileName == "commander_portrait_placeholder.png"
            || fileName == "mode_card_art_saga.png"
            || fileName == "mode_card_art_operation.png"
            || fileName == "mode_card_art_quick_custom.png";
    }

    private static bool ContainsIgnoreCase(string value, string token)
    {
        return value != null && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void CopyPng(string sourcePath, string destinationPath)
    {
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static void WriteChromaKeyAlphaPng(string sourcePath, string destinationPath, bool trimToContent)
    {
        Texture2D source = LoadTexture(sourcePath);
        Color32[] sourcePixels = source.GetPixels32();
        Color32[] alphaPixels = new Color32[sourcePixels.Length];

        int minX = source.width;
        int minY = source.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                int index = y * source.width + x;
                Color32 pixel = sourcePixels[index];
                byte alpha = ComputeChromaAlpha(pixel);
                if (alpha > 0 && IsChromaFringe(pixel))
                    alpha = 0;

                if (alpha == 0)
                {
                    alphaPixels[index] = new Color32(0, 0, 0, 0);
                    continue;
                }

                Color32 despilled = DespillGreen(pixel, alpha);
                despilled.a = alpha;
                alphaPixels[index] = despilled;

                if (alpha > 10)
                {
                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        Texture2D output;
        if (trimToContent && maxX >= minX && maxY >= minY)
        {
            const int padding = 6;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(source.width - 1, maxX + padding);
            maxY = Mathf.Min(source.height - 1, maxY + padding);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] trimmed = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                Array.Copy(alphaPixels, (minY + y) * source.width + minX, trimmed, y * width, width);
            }

            output.SetPixels32(trimmed);
        }
        else
        {
            output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            output.SetPixels32(alphaPixels);
        }

        output.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        File.WriteAllBytes(destinationPath, output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(source);
        UnityEngine.Object.DestroyImmediate(output);
    }

    private static Texture2D LoadTexture(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path)))
            throw new InvalidOperationException($"Could not load PNG: {path}");

        return texture;
    }

    private static byte ComputeChromaAlpha(Color32 pixel)
    {
        bool pureGreen = pixel.g > 205 && pixel.r < 70 && pixel.b < 70;
        if (pureGreen)
            return 0;

        float r = pixel.r / 255f;
        float g = pixel.g / 255f;
        float b = pixel.b / 255f;
        float keyDistance = Mathf.Sqrt(r * r + (g - 1f) * (g - 1f) + b * b);
        float alpha = Mathf.InverseLerp(0.13f, 0.33f, keyDistance);
        alpha = Mathf.Clamp01(alpha);
        return (byte)Mathf.RoundToInt(alpha * 255f);
    }

    private static bool IsChromaFringe(Color32 pixel)
    {
        bool greenDominant = pixel.g >= 70 && pixel.g > pixel.r * 1.45f && pixel.g > pixel.b * 1.25f;
        bool greenCyanEdge = pixel.g >= 55 && pixel.g > pixel.r * 1.2f && pixel.g > pixel.b * 0.85f;
        bool darkGreenEdge = pixel.r <= 35 && pixel.g >= 70 && pixel.g > pixel.r * 2.4f && pixel.g > pixel.b * 2.0f;
        bool turquoiseKeyHalo = pixel.r <= 35 && pixel.g >= 170 && pixel.b >= 70 && pixel.b <= 175;
        return greenDominant || greenCyanEdge || darkGreenEdge || turquoiseKeyHalo;
    }

    private static void CleanTopHeaderBorder(string destinationPath)
    {
        Texture2D source = LoadTexture(destinationPath);
        Color32[] original = source.GetPixels32();
        Color32[] cleaned = new Color32[original.Length];
        Array.Copy(original, cleaned, original.Length);

        for (int y = 0; y < source.height; y++)
        {
            for (int x = 0; x < source.width; x++)
            {
                int index = y * source.width + x;
                Color32 pixel = original[index];
                if (pixel.a == 0 || !IsHeaderRimSpill(pixel))
                    continue;

                if (HasTransparentNeighbor(original, source.width, source.height, x, y, 5))
                    cleaned[index] = new Color32(0, 0, 0, 0);
                else
                    cleaned[index] = NeutralizeGreenCyanRim(pixel);
            }
        }

        Texture2D output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        output.SetPixels32(cleaned);
        output.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        File.WriteAllBytes(destinationPath, output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(source);
        UnityEngine.Object.DestroyImmediate(output);
    }

    private static bool IsHeaderRimSpill(Color32 pixel)
    {
        bool greenCyan = pixel.r <= 55 && pixel.g >= 65 && pixel.b >= 55 && pixel.g + pixel.b >= 145;
        bool greenTint = pixel.r <= 75 && pixel.g >= 85 && pixel.g > pixel.r * 1.45f && pixel.b >= 45;
        return greenCyan || greenTint;
    }

    private static Color32 NeutralizeGreenCyanRim(Color32 pixel)
    {
        byte neutral = (byte)Mathf.Clamp(Mathf.RoundToInt((pixel.r + pixel.g + pixel.b) / 5.5f), 0, 72);
        return new Color32(neutral, neutral, neutral, pixel.a);
    }

    private static bool HasTransparentNeighbor(Color32[] pixels, int width, int height, int x, int y, int radius)
    {
        int minX = Mathf.Max(0, x - radius);
        int maxX = Mathf.Min(width - 1, x + radius);
        int minY = Mathf.Max(0, y - radius);
        int maxY = Mathf.Min(height - 1, y + radius);

        for (int sampleY = minY; sampleY <= maxY; sampleY++)
        {
            for (int sampleX = minX; sampleX <= maxX; sampleX++)
            {
                if (pixels[sampleY * width + sampleX].a < 8)
                    return true;
            }
        }

        return false;
    }

    private static Color32 DespillGreen(Color32 pixel, byte alpha)
    {
        if (alpha > 240)
            return pixel;

        byte maxRedBlue = Math.Max(pixel.r, pixel.b);
        if (pixel.g > maxRedBlue)
            pixel.g = (byte)Mathf.Lerp(maxRedBlue, pixel.g, alpha / 255f);

        return pixel;
    }

    private static void ImportSprite(string assetPath, LayerAsset layer)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = !IsOpaqueAsset(layer);
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;

        Vector4 border = CalculateSpriteBorder(assetPath, layer);
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static Vector4 CalculateSpriteBorder(string assetPath, LayerAsset layer)
    {
        if (layer.sliceBorder == null || layer.sliceBorder.Length < 4 || layer.targetRect == null || layer.targetRect.Length < 4)
            return Vector4.zero;

        Texture2D texture = LoadTexture(assetPath);
        try
        {
            float scaleX = texture.width / Mathf.Max(1f, layer.targetRect[2]);
            float scaleY = texture.height / Mathf.Max(1f, layer.targetRect[3]);
            float left = Mathf.Clamp(layer.sliceBorder[0] * scaleX, 0f, texture.width * 0.35f);
            float top = Mathf.Clamp(layer.sliceBorder[1] * scaleY, 0f, texture.height * 0.35f);
            float right = Mathf.Clamp(layer.sliceBorder[2] * scaleX, 0f, texture.width * 0.35f);
            float bottom = Mathf.Clamp(layer.sliceBorder[3] * scaleY, 0f, texture.height * 0.35f);
            return new Vector4(left, bottom, right, top);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static GameObject BuildPrefabRoot(LayerRequest request)
    {
        CanvasLayout layout = LoadLayoutOrNull();
        if (layout != null)
            return BuildLayoutPrefabRoot(layout, request.reference.targetCanvas[0], request.reference.targetCanvas[1]);

        int canvasWidth = request.reference.targetCanvas[0];
        int canvasHeight = request.reference.targetCanvas[1];

        GameObject root = CreateRectObject("Screen_MainMenu_ComponentCanvasTest", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);

        Image rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.004f, 0.018f, 0.022f, 1f);
        rootImage.raycastTarget = false;

        WarlineCaptureScreenController screenController = root.AddComponent<WarlineCaptureScreenController>();
        screenController.SetRouteForTests(WarlineCaptureRoute.MainMenu);

        GameObject artRoot = CreateRectObject("GeneratedLayerArtRoot", root.transform);
        StretchToParent(artRoot.GetComponent<RectTransform>());

        GameObject textRoot = CreateRectObject("LiveTextRoot", root.transform);
        StretchToParent(textRoot.GetComponent<RectTransform>());

        AddBackgroundTacticalMap(request, artRoot.transform);
        AddLayerIfAvailable(request, artRoot.transform, "screen_shell_frame", "ScreenShellFrame");
        AddCleanedLayer(artRoot.transform, "brand_logo_panel_frame.png", "BrandLogoPanelFrame", new[] { 16, 14, 820, 246 });
        AddCleanedLayer(artRoot.transform, "brand_logo_lockup.png", "BrandLogoLockup", new[] { 92, 48, 600, 152 }, preserveAspect: true);
        AddLayerIfAvailable(request, artRoot.transform, "top_resource_bar_frame_full", "TopResourceBarFrameFull");
        AddCleanedLayer(artRoot.transform, "icon_credits.png", "CreditsIcon", new[] { 1098, 62, 122, 122 });
        AddCleanedLayer(artRoot.transform, "icon_materials.png", "MaterialsIcon", new[] { 1888, 62, 122, 122 });
        AddCleanedLayer(artRoot.transform, "icon_command_authority.png", "AuthorityIcon", new[] { 2706, 54, 132, 142 });

        AddLayerIfAvailable(request, artRoot.transform, "commander_profile_panel_frame", "CommanderProfilePanelFrame");
        AddCoverLayer(artRoot.transform, "commander_profile_portrait.png", "CommanderProfilePortrait", FindLayer(request, "commander_profile_portrait")?.targetRect, Color.white);
        AddCleanedLayer(artRoot.transform, "profile_data_status_strip.png", "ProfileDataStatusStrip", new[] { 86, 866, 662, 94 });
        AddRepeatedNavRows(request, artRoot.transform);
        AddRepeatedNavDetails(request, artRoot.transform);
        AddModeCards(request, artRoot.transform);
        AddLayerIfAvailable(request, artRoot.transform, "deploy_command_button_frame", "DeployCommandButtonFrame");
        AddLayerIfAvailable(request, artRoot.transform, "deploy_command_chevrons", "DeployCommandChevrons");
        AddLayerIfAvailable(request, artRoot.transform, "settings_gear_icon", "SettingsGearIcon");

        AddLiveText(textRoot.transform);

        return root;
    }

    private static CanvasLayout LoadLayoutOrNull()
    {
        if (!File.Exists(LayoutPath))
            return null;

        CanvasLayout layout = JsonUtility.FromJson<CanvasLayout>(File.ReadAllText(LayoutPath));
        if (layout == null)
            throw new InvalidOperationException($"SCN-02 layout manifest is invalid: {LayoutPath}");

        return layout;
    }

    private static GameObject BuildLayoutPrefabRoot(CanvasLayout layout, int fallbackCanvasWidth, int fallbackCanvasHeight)
    {
        int canvasWidth = layout.canvas != null && layout.canvas.Length >= 2 ? layout.canvas[0] : fallbackCanvasWidth;
        int canvasHeight = layout.canvas != null && layout.canvas.Length >= 2 ? layout.canvas[1] : fallbackCanvasHeight;

        GameObject root = CreateRectObject("Screen_MainMenu_ComponentCanvasTest", null);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);

        Image rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.004f, 0.018f, 0.022f, 1f);
        rootImage.raycastTarget = false;

        WarlineCaptureScreenController screenController = root.AddComponent<WarlineCaptureScreenController>();
        screenController.SetRouteForTests(WarlineCaptureRoute.MainMenu);

        GameObject artRoot = CreateRectObject("ComponentPlateArtRoot", root.transform);
        StretchToParent(artRoot.GetComponent<RectTransform>());

        GameObject textRoot = CreateRectObject("ComponentLiveTextRoot", root.transform);
        StretchToParent(textRoot.GetComponent<RectTransform>());

        if (layout.images != null)
        {
            LayoutImage[] images = (LayoutImage[])layout.images.Clone();
            Array.Sort(images, (left, right) => left.z.CompareTo(right.z));
            foreach (LayoutImage image in images)
                AddLayoutImage(artRoot.transform, image);
        }

        if (layout.texts != null)
        {
            foreach (LayoutText text in layout.texts)
                AddLayoutText(textRoot.transform, text);
        }

        return root;
    }

    private static void AddLayoutImage(Transform parent, LayoutImage layer)
    {
        if (layer == null || string.IsNullOrEmpty(layer.file) || layer.rect == null || layer.rect.Length < 4)
            return;

        if (ContainsIgnoreCase(layer.file, "contact_sheet")
            || ContainsIgnoreCase(layer.file, "comparison")
            || ContainsIgnoreCase(layer.file, "screenshot")
            || ContainsIgnoreCase(layer.file, "target_slice")
            || ContainsIgnoreCase(layer.file, "full_visual_lock_preview"))
        {
            throw new InvalidOperationException($"Forbidden SCN-02 runtime layout image: {layer.file}");
        }

        string spritePath = GetGeneratedCleanedAssetPath(layer.file);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
            throw new FileNotFoundException($"SCN-02 layout sprite missing: {spritePath}");

        string objectName = string.IsNullOrEmpty(layer.name) ? Path.GetFileNameWithoutExtension(layer.file) : layer.name;
        Color tint = ToColor(layer.tint, Color.white);
        string fit = string.IsNullOrEmpty(layer.fit) ? "stretch" : layer.fit;

        if (string.Equals(fit, "cover", StringComparison.OrdinalIgnoreCase))
        {
            AddLayoutCoverImage(parent, objectName, sprite, layer.rect, tint);
            return;
        }

        GameObject layerObject = CreateRectObject(objectName, parent);
        ApplyTopLeftRect(layerObject.GetComponent<RectTransform>(), layer.rect);

        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        image.preserveAspect = string.Equals(fit, "contain", StringComparison.OrdinalIgnoreCase);
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
    }

    private static void AddLayoutCoverImage(Transform parent, string objectName, Sprite sprite, int[] rect, Color tint)
    {
        GameObject maskObject = CreateRectObject(objectName, parent);
        ApplyTopLeftRect(maskObject.GetComponent<RectTransform>(), rect);
        maskObject.AddComponent<RectMask2D>();

        GameObject imageObject = CreateRectObject($"{objectName}_Image", maskObject.transform);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();

        float rectAspect = rect[2] / Mathf.Max(1f, rect[3]);
        float spriteAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        float width = rect[2];
        float height = rect[3];
        if (spriteAspect > rectAspect)
            width = height * spriteAspect;
        else
            height = width / spriteAspect;

        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(width, height);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
    }

    private static void AddLayoutText(Transform parent, LayoutText layoutText)
    {
        if (layoutText == null || layoutText.rect == null || layoutText.rect.Length < 4)
            return;

        bool bold = string.Equals(layoutText.weight, "bold", StringComparison.OrdinalIgnoreCase);
        TextAlignmentOptions alignment = ParseTextAlignment(layoutText.alignment);
        Color color = ToColor(layoutText.color, new Color(0.82f, 0.84f, 0.86f, 1f));
        AddText(parent, layoutText.name, layoutText.text, layoutText.rect, layoutText.fontSize, alignment, bold, color);
    }

    private static TextAlignmentOptions ParseTextAlignment(string alignment)
    {
        if (string.Equals(alignment, "center", StringComparison.OrdinalIgnoreCase))
            return TextAlignmentOptions.Center;
        if (string.Equals(alignment, "right", StringComparison.OrdinalIgnoreCase))
            return TextAlignmentOptions.Right;
        return TextAlignmentOptions.Left;
    }

    private static Color ToColor(float[] values, Color fallback)
    {
        if (values == null || values.Length < 3)
            return fallback;

        float alpha = values.Length >= 4 ? values[3] : 1f;
        return new Color(values[0], values[1], values[2], alpha);
    }

    private static void AddLayerIfAvailable(LayerRequest request, Transform parent, string id, string objectName)
    {
        LayerAsset layer = FindLayer(request, id);
        if (layer == null)
            return;

        string path = GetGeneratedAssetPath(layer);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            return;

        GameObject layerObject = CreateRectObject(objectName, parent);
        RectTransform rect = layerObject.GetComponent<RectTransform>();
        ApplyTopLeftRect(rect, CompensateStandaloneFrameRect(layer.id, layer.targetRect));

        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = GetLayerTint(layer.id);
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.type = ShouldUseSlicedImage(layer) ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static void AddBackgroundTacticalMap(LayerRequest request, Transform parent)
    {
        LayerAsset layer = FindLayer(request, "main_menu_background_tactical_map");
        if (layer == null)
            return;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetGeneratedAssetPath(layer));
        if (sprite == null)
            return;

        GameObject maskObject = CreateRectObject("BackgroundTacticalMapMask", parent);
        ApplyTopLeftRect(maskObject.GetComponent<RectTransform>(), layer.targetRect);
        maskObject.AddComponent<RectMask2D>();

        GameObject imageObject = CreateRectObject("BackgroundTacticalMap", maskObject.transform);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(layer.targetRect[2], layer.targetRect[3]);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
    }

    private static void AddCleanedLayer(Transform parent, string fileName, string objectName, int[] rect, bool preserveAspect = false)
    {
        if (rect == null || rect.Length < 4)
            return;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetGeneratedCleanedAssetPath(fileName));
        if (sprite == null)
            return;

        GameObject layerObject = CreateRectObject(objectName, parent);
        ApplyTopLeftRect(layerObject.GetComponent<RectTransform>(), rect);

        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = GetCleanedLayerTint(fileName);
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
    }

    private static void AddManifestLayerAtRect(LayerRequest request, Transform parent, string id, string objectName, int[] rect)
    {
        LayerAsset layer = FindLayer(request, id);
        if (layer == null || rect == null || rect.Length < 4)
            return;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetGeneratedAssetPath(layer));
        if (sprite == null)
            return;

        GameObject layerObject = CreateRectObject(objectName, parent);
        ApplyTopLeftRect(layerObject.GetComponent<RectTransform>(), CompensateStandaloneFrameRect(id, rect));

        Image image = layerObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = GetLayerTint(layer.id);
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.type = ShouldUseSlicedImage(layer) ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static Color GetLayerTint(string layerId)
    {
        if (layerId == "screen_shell_frame")
            return new Color(0.55f, 0.72f, 0.76f, 0.58f);

        if (layerId == "brand_logo_panel_frame"
            || layerId == "commander_profile_panel_frame"
            || layerId == "profile_block_frame"
            || layerId == "left_nav_row_frame"
            || layerId == "mode_card_frame")
        {
            return new Color(0.78f, 0.86f, 0.90f, 1f);
        }

        if (layerId == "operation_warning_row_frame")
            return new Color(0.92f, 0.82f, 0.68f, 1f);

        if (layerId == "deploy_command_button_frame" || layerId == "deploy_command_chevrons")
            return new Color(0.95f, 0.86f, 0.70f, 1f);

        return Color.white;
    }

    private static Color GetCleanedLayerTint(string fileName)
    {
        if (fileName == "mode_card_frame.png"
            || fileName == "commander_profile_panel_frame.png"
            || fileName == "profile_block_frame.png"
            || fileName == "left_nav_row_frame.png"
            || fileName == "designed_unavailable_badge.png")
        {
            return new Color(0.78f, 0.86f, 0.90f, 1f);
        }

        if (fileName == "operation_warning_row_frame.png")
            return new Color(0.92f, 0.82f, 0.68f, 1f);

        if (fileName == "deploy_command_button_frame.png" || fileName == "deploy_command_chevrons.png")
            return new Color(0.95f, 0.86f, 0.70f, 1f);

        if (fileName.StartsWith("mode_card_header_emblem_", StringComparison.Ordinal)
            || fileName.StartsWith("card_footer_icon_", StringComparison.Ordinal))
        {
            return new Color(0.82f, 0.94f, 1f, 1f);
        }

        return Color.white;
    }

    private static void AddCoverLayer(Transform parent, string fileName, string objectName, int[] rect, Color tint)
    {
        if (rect == null || rect.Length < 4)
            return;

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetGeneratedCleanedAssetPath(fileName));
        if (sprite == null)
            return;

        GameObject maskObject = CreateRectObject(objectName, parent);
        ApplyTopLeftRect(maskObject.GetComponent<RectTransform>(), rect);
        maskObject.AddComponent<RectMask2D>();

        GameObject imageObject = CreateRectObject($"{objectName}_Image", maskObject.transform);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();

        float rectAspect = rect[2] / Mathf.Max(1f, rect[3]);
        float spriteAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        float width = rect[2];
        float height = rect[3];
        if (spriteAspect > rectAspect)
            width = height * spriteAspect;
        else
            height = width / spriteAspect;

        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = new Vector2(width, height);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = tint;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.type = Image.Type.Simple;
    }

    private static bool ShouldUseSlicedImage(LayerAsset layer)
    {
        if (FixedSimpleLayerIds.Contains(layer.id))
            return false;

        return layer.sliceBorder != null && layer.sliceBorder.Length >= 4;
    }

    private static int[] CompensateStandaloneFrameRect(string id, int[] rect)
    {
        if (rect == null || rect.Length < 4)
            return rect;

        int horizontal = 0;
        int vertical = 0;
        switch (id)
        {
            case "brand_logo_panel_frame":
                return rect;
            case "top_resource_bar_frame_full":
                horizontal = 24;
                vertical = 24;
                break;
            case "commander_profile_panel_frame":
                horizontal = 28;
                vertical = 34;
                break;
            case "left_nav_row_frame":
                horizontal = 34;
                vertical = 14;
                break;
            case "mode_card_frame":
                horizontal = 34;
                vertical = 38;
                break;
            case "operation_warning_row_frame":
                horizontal = 24;
                vertical = 22;
                break;
            case "deploy_command_button_frame":
                horizontal = 38;
                vertical = 60;
                break;
            default:
                return rect;
        }

        return new[]
        {
            rect[0] - horizontal,
            rect[1] - vertical,
            rect[2] + horizontal * 2,
            rect[3] + vertical * 2
        };
    }

    private static void AddRepeatedNavRows(LayerRequest request, Transform parent)
    {
        LayerAsset rowLayer = FindLayer(request, "left_nav_row_frame");
        if (rowLayer == null)
            return;

        string path = GetGeneratedAssetPath(rowLayer);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            return;

        string[] rowNames = { "Inbox", "Store", "Events", "Ranking", "CommandFeed" };
        for (int i = 0; i < rowNames.Length; i++)
        {
            int[] rowRect = (int[])rowLayer.targetRect.Clone();
            rowRect[1] += i * 170;
            AddManifestLayerAtRect(request, parent, "left_nav_row_frame", $"LeftNavRow_{rowNames[i]}", rowRect);
        }
    }

    private static void AddRepeatedNavDetails(LayerRequest request, Transform parent)
    {
        string[] iconIds =
        {
            "left_nav_icon_inbox",
            "left_nav_icon_store",
            "left_nav_icon_events",
            "left_nav_icon_ranking",
            "left_nav_icon_command_feed"
        };

        for (int i = 0; i < iconIds.Length; i++)
        {
            LayerAsset iconLayer = FindLayer(request, iconIds[i]);
            if (iconLayer != null)
                AddCleanedLayer(parent, $"{iconIds[i]}.png", iconIds[i], ShrinkRectCentered(iconLayer.targetRect, 82, 82));

            LayerAsset badgeLayer = FindLayer(request, "designed_unavailable_badge");
            if (badgeLayer != null)
            {
                int[] badgeRect = (int[])badgeLayer.targetRect.Clone();
                badgeRect[1] += i * 170;
                AddCleanedLayer(parent, "designed_unavailable_badge.png", $"DesignedUnavailableBadge_{i}", badgeRect);
            }

            int lockY = 1044 + i * 170;
            AddCleanedLayer(parent, "lock_badge_frame.png", $"LockBadgeFrame_{i}", new[] { 684, lockY, 76, 84 });
            AddCleanedLayer(parent, "lock_icon.png", $"LockIcon_{i}", new[] { 702, lockY + 18, 40, 44 }, preserveAspect: true);
        }
    }

    private static void AddModeCards(LayerRequest request, Transform parent)
    {
        AddModeCard(request, parent, "Saga", 0, "mode_card_art_saga.png", "mode_card_header_emblem_saga", "card_footer_icon_saga");
        AddModeCard(request, parent, "Operation", 900, "mode_card_art_operation.png", "mode_card_header_emblem_operation", "card_footer_icon_operation");
        AddModeCard(request, parent, "QuickCustom", 1846, "mode_card_art_quick_custom.png", "mode_card_header_emblem_quick_custom", "card_footer_icon_quick_custom");

        AddOperationWarnings(request, parent);
    }

    private static void AddModeCard(LayerRequest request, Transform parent, string name, int xOffset, string artFile, string headerIconId, string footerIconId)
    {
        string artId = name == "Saga" ? "mode_card_art_saga" : name == "Operation" ? "mode_card_art_operation" : "mode_card_art_quick_custom";
        LayerAsset artLayer = FindLayer(request, artId);
        if (artLayer != null)
            AddCoverLayer(parent, artFile, $"ModeCardArt_{name}", OffsetRect(artLayer.targetRect, CardOffsetX(name), -22), new Color(0.72f, 0.76f, 0.78f, 1f));

        LayerAsset frameLayer = FindLayer(request, "mode_card_frame");
        if (frameLayer != null)
        {
            int[] frameRect = (int[])frameLayer.targetRect.Clone();
            frameRect[0] += xOffset;
            AddManifestLayerAtRect(request, parent, "mode_card_frame", $"ModeCardFrame_{name}", frameRect);
        }

        LayerAsset headerIconLayer = FindLayer(request, headerIconId);
        if (headerIconLayer != null)
            AddCleanedLayer(parent, $"{headerIconId}.png", $"ModeCardHeaderIcon_{name}", OffsetRect(ShrinkRectCentered(headerIconLayer.targetRect, 104, 104), CardOffsetX(name), -18));

        LayerAsset footerIconLayer = FindLayer(request, footerIconId);
        if (footerIconLayer != null)
        {
            AddCleanedLayer(parent, "circular_badge_frame.png", $"ModeCardFooterBadgeFrame_{name}", OffsetRect(ShrinkRectCentered(footerIconLayer.targetRect, 142, 142), CardOffsetX(name), -6));
            AddCleanedLayer(parent, $"{footerIconId}.png", $"ModeCardFooterIcon_{name}", OffsetRect(ShrinkRectCentered(footerIconLayer.targetRect, 120, 120), CardOffsetX(name), -6));
        }
    }

    private static int CardOffsetX(string name)
    {
        if (name == "Operation")
            return -23;
        if (name == "QuickCustom")
            return -9;
        return 0;
    }

    private static void AddOperationWarnings(LayerRequest request, Transform parent)
    {
        LayerAsset rowLayer = FindLayer(request, "operation_warning_row_frame");
        LayerAsset iconLayer = FindLayer(request, "operation_warning_icon");
        if (rowLayer != null)
        {
            AddManifestLayerAtRect(request, parent, "operation_warning_row_frame", "OperationWarningRowPressure", OffsetRect(rowLayer.targetRect, -23, -10));
            int[] secondRow = OffsetRect(rowLayer.targetRect, -23, -10);
            secondRow[1] += 170;
            AddManifestLayerAtRect(request, parent, "operation_warning_row_frame", "OperationWarningRowRisk", secondRow);
        }

        if (iconLayer != null)
        {
            AddCleanedLayer(parent, "operation_warning_icon.png", "OperationWarningIconPressure", OffsetRect(iconLayer.targetRect, -23, -10));
            int[] secondIcon = OffsetRect(iconLayer.targetRect, -23, -10);
            secondIcon[1] += 170;
            AddCleanedLayer(parent, "operation_warning_icon.png", "OperationWarningIconRisk", secondIcon);
        }

        LayerAsset pressureMeter = FindLayer(request, "operation_pressure_meter_segments");
        if (pressureMeter != null)
            AddManifestLayerAtRect(request, parent, "operation_pressure_meter_segments", "OperationPressureMeterSegments", OffsetRect(pressureMeter.targetRect, -23, -10));

        LayerAsset riskMeter = FindLayer(request, "operation_risk_meter_segments");
        if (riskMeter != null)
            AddManifestLayerAtRect(request, parent, "operation_risk_meter_segments", "OperationRiskMeterSegments", OffsetRect(riskMeter.targetRect, -23, -10));
    }

    private static int[] ShrinkRectCentered(int[] rect, int width, int height)
    {
        if (rect == null || rect.Length < 4)
            return rect;

        return new[]
        {
            rect[0] + (rect[2] - width) / 2,
            rect[1] + (rect[3] - height) / 2,
            width,
            height
        };
    }

    private static int[] OffsetRect(int[] rect, int x, int y)
    {
        if (rect == null || rect.Length < 4)
            return rect;

        return new[] { rect[0] + x, rect[1] + y, rect[2], rect[3] };
    }

    private static void AddLiveText(Transform parent)
    {
        AddText(parent, "CreditsLabel", "Credits", new[] { 1270, 78, 220, 46 }, 34f, TextAlignmentOptions.Left, false, new Color(0.78f, 0.80f, 0.82f));
        AddText(parent, "CreditsValue", "187,540", new[] { 1270, 124, 300, 70 }, 56f, TextAlignmentOptions.Left, true, new Color(0.96f, 0.76f, 0.48f));
        AddText(parent, "MaterialsLabel", "Materials", new[] { 2056, 78, 250, 46 }, 34f, TextAlignmentOptions.Left, false, new Color(0.78f, 0.80f, 0.82f));
        AddText(parent, "MaterialsValue", "92,860", new[] { 2056, 124, 300, 70 }, 56f, TextAlignmentOptions.Left, true, new Color(0.68f, 0.84f, 0.96f));
        AddText(parent, "AuthorityLabel", "Command Authority", new[] { 2880, 78, 430, 46 }, 34f, TextAlignmentOptions.Left, false, new Color(0.78f, 0.80f, 0.82f));
        AddText(parent, "AuthorityValue", "2,715", new[] { 2880, 124, 260, 70 }, 56f, TextAlignmentOptions.Left, true, new Color(0.96f, 0.76f, 0.48f));

        AddText(parent, "CommanderProfileTitle", "Commander Profile", new[] { 86, 318, 400, 62 }, 40f, TextAlignmentOptions.Left, false, new Color(0.18f, 0.78f, 0.95f));
        AddText(parent, "CommanderProfilePending", "Profile data pending", new[] { 150, 920, 520, 50 }, 32f, TextAlignmentOptions.Center, false, new Color(0.72f, 0.75f, 0.78f));

        string[] navLabels = { "Inbox", "Store", "Events", "Ranking", "Command Feed" };
        for (int i = 0; i < navLabels.Length; i++)
        {
            int y = 1044 + i * 170;
            float navFontSize = navLabels[i] == "Command Feed" ? 36f : 42f;
            AddText(parent, $"LeftNav_{i}_Label", navLabels[i], new[] { 205, y, 310, 60 }, navFontSize, TextAlignmentOptions.Left, true, new Color(0.82f, 0.84f, 0.86f));
            AddText(parent, $"LeftNav_{i}_Unavailable", "Designed\nUnavailable", new[] { 455, y + 4, 245, 72 }, 23f, TextAlignmentOptions.Center, false, new Color(0.70f, 0.75f, 0.78f));
        }

        AddText(parent, "SagaTitle", "Saga Campaign", new[] { 1132, 392, 530, 84 }, 48f, TextAlignmentOptions.Left, true, new Color(0.82f, 0.82f, 0.80f));
        AddText(parent, "OperationTitle", "Persistent Operation", new[] { 2031, 392, 700, 84 }, 48f, TextAlignmentOptions.Left, true, new Color(0.82f, 0.82f, 0.80f));
        AddText(parent, "QuickCustomTitle", "Quick Custom Game", new[] { 2977, 392, 610, 84 }, 48f, TextAlignmentOptions.Left, true, new Color(0.82f, 0.82f, 0.80f));

        AddText(parent, "SagaDescription", "Play through the story arc\nand reclaim key districts.", new[] { 1164, 1500, 485, 120 }, 34f, TextAlignmentOptions.Left, false, new Color(0.72f, 0.76f, 0.78f));
        AddText(parent, "OperationDescription", "Maintain control and manage\ndistrict and city operations.", new[] { 2045, 1500, 560, 120 }, 34f, TextAlignmentOptions.Left, false, new Color(0.72f, 0.76f, 0.78f));
        AddText(parent, "QuickCustomDescription", "Set up a custom scenario\nand jump into battle.", new[] { 2991, 1500, 560, 120 }, 34f, TextAlignmentOptions.Left, false, new Color(0.72f, 0.76f, 0.78f));

        AddText(parent, "OperationPressureText", "District pressure rising", new[] { 1975, 1200, 470, 42 }, 30f, TextAlignmentOptions.Left, true, new Color(1.00f, 0.63f, 0.10f));
        AddText(parent, "OperationPressureHigh", "HIGH", new[] { 2489, 1270, 120, 42 }, 26f, TextAlignmentOptions.Right, true, new Color(1.00f, 0.63f, 0.10f));
        AddText(parent, "OperationRiskText", "City operation risk", new[] { 1975, 1370, 470, 42 }, 30f, TextAlignmentOptions.Left, true, new Color(1.00f, 0.63f, 0.10f));
        AddText(parent, "OperationRiskElevated", "ELEVATED", new[] { 2449, 1440, 160, 42 }, 26f, TextAlignmentOptions.Right, true, new Color(1.00f, 0.63f, 0.10f));

        AddText(parent, "DeployCommandLabel", "DEPLOY COMMAND", new[] { 2840, 1826, 640, 88 }, 62f, TextAlignmentOptions.Center, true, new Color(1.00f, 0.67f, 0.25f));
    }

    private static TMP_Text AddText(Transform parent, string name, string value, int[] rect, float fontSize, TextAlignmentOptions alignment, bool bold, Color color)
    {
        GameObject textObject = CreateRectObject(name, parent);
        ApplyTopLeftRect(textObject.GetComponent<RectTransform>(), rect);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(bold ? BoldFontPath : LightFontPath);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static LayerAsset FindLayer(LayerRequest request, string id)
    {
        foreach (LayerAsset asset in request.assets)
        {
            if (asset.id == id)
                return asset;
        }

        return null;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            gameObject.transform.SetParent(parent, false);

        return gameObject;
    }

    private static void CapturePrefab(string prefabPath, string outputPath, int width, int height, Color backgroundColor)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            throw new InvalidOperationException($"Cannot capture {prefabPath} while Unity is running with NullGfxDevice.");

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new FileNotFoundException($"UI prefab not found at {prefabPath}");

        EnsureParentFolder(outputPath);

        RenderTexture renderTexture = null;
        Texture2D screenshot = null;
        GameObject cameraObject = null;
        GameObject canvasObject = null;
        GameObject instance = null;
        RenderTexture previousActiveTexture = RenderTexture.active;

        try
        {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            renderTexture.Create();

            cameraObject = new GameObject("SCN02LayerCanvasCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.targetTexture = renderTexture;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            canvasObject = CreateRectObject("SCN02LayerCanvasCaptureRoot", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.sizeDelta = new Vector2(width, height);
            canvasTransform.localPosition = Vector3.zero;
            canvasTransform.localScale = Vector3.one;

            instance = UnityEngine.Object.Instantiate(prefab, canvasObject.transform);
            instance.name = prefab.name;
            instance.SetActive(true);
            RectTransform instanceTransform = (RectTransform)instance.transform;
            instanceTransform.anchorMin = Vector2.zero;
            instanceTransform.anchorMax = Vector2.one;
            instanceTransform.offsetMin = Vector2.zero;
            instanceTransform.offsetMax = Vector2.zero;
            instanceTransform.localScale = Vector3.one;

            Canvas.ForceUpdateCanvases();
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, backgroundColor);
            camera.Render();

            screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[SCN-02 ComponentCanvas] Captured {prefabPath} to {outputPath}");
        }
        finally
        {
            RenderTexture.active = previousActiveTexture;
            Camera camera = cameraObject == null ? null : cameraObject.GetComponent<Camera>();
            if (camera != null)
                camera.targetTexture = null;
            if (screenshot != null)
                UnityEngine.Object.DestroyImmediate(screenshot);
            if (renderTexture != null)
                UnityEngine.Object.DestroyImmediate(renderTexture);
            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance);
            if (canvasObject != null)
                UnityEngine.Object.DestroyImmediate(canvasObject);
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ApplyTopLeftRect(RectTransform rect, int[] topLeftRect)
    {
        if (topLeftRect == null || topLeftRect.Length < 4)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(topLeftRect[0], -topLeftRect[1]);
        rect.sizeDelta = new Vector2(topLeftRect[2], topLeftRect[3]);
    }

    private static void EnsureParentFolder(string assetPath)
    {
        string folder = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            EnsureFolder(current, parts[i]);
            current += "/" + parts[i];
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    [Serializable]
    private sealed class LayerRequest
    {
        public LayerReference reference;
        public LayerAsset[] assets;
    }

    [Serializable]
    private sealed class LayerReference
    {
        public int[] targetCanvas;
    }

    [Serializable]
    private sealed class LayerAsset
    {
        public string id;
        public string type;
        public string background;
        public int[] targetRect;
        public int[] sliceBorder;
    }

    [Serializable]
    private sealed class CanvasLayout
    {
        public string schema;
        public string screen;
        public int[] canvas;
        public LayoutImage[] images;
        public LayoutText[] texts;
    }

    [Serializable]
    private sealed class LayoutImage
    {
        public string name;
        public string file;
        public int[] rect;
        public string fit;
        public float[] tint;
        public int z;
    }

    [Serializable]
    private sealed class LayoutText
    {
        public string name;
        public string text;
        public int[] rect;
        public float fontSize;
        public string alignment;
        public string weight;
        public float[] color;
        public int z;
    }
}
