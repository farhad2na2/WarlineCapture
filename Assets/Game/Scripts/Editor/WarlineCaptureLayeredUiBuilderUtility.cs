#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public readonly struct WarlineUiRect
{
    public WarlineUiRect(string name, RectInt rect)
    {
        Name = name;
        Rect = rect;
    }

    public string Name { get; }
    public RectInt Rect { get; }
}

public readonly struct WarlineUiImagePlacement
{
    public WarlineUiImagePlacement(RectInt fullRect, RectInt visibleRect)
    {
        FullRect = fullRect;
        VisibleRect = visibleRect;
    }

    public RectInt FullRect { get; }
    public RectInt VisibleRect { get; }
}

public static class WarlineCaptureLayeredUiBuilderUtility
{
    private const float VisibleCenterTolerance = 2f;
    private static readonly Dictionary<string, RectInt> s_VisibleBoundsCache = new();

    public static RectInt Inset(RectInt rect, int x, int y) => new(rect.x + x, rect.y + y, rect.width - x * 2, rect.height - y * 2);

    public static int[] ToArray(RectInt rect) => new[] { rect.x, rect.y, rect.width, rect.height };

    public static RectInt SourceToCanvasRect(RectInt canvasRect, int sourceWidth, int sourceHeight, RectInt sourceRect)
    {
        float scaleX = canvasRect.width / (float)sourceWidth;
        float scaleY = canvasRect.height / (float)sourceHeight;
        return new RectInt(
            canvasRect.x + Mathf.RoundToInt(sourceRect.x * scaleX),
            canvasRect.y + Mathf.RoundToInt(sourceRect.y * scaleY),
            Mathf.RoundToInt(sourceRect.width * scaleX),
            Mathf.RoundToInt(sourceRect.height * scaleY));
    }

    public static GameObject CreateRectObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        if (parent != null)
            gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    public static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    public static void ApplyTopLeftRect(RectTransform rect, int[] topLeftRect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(topLeftRect[0], -topLeftRect[1]);
        rect.sizeDelta = new Vector2(topLeftRect[2], topLeftRect[3]);
        rect.localScale = Vector3.one;
    }

    public static Sprite LoadSprite(string layerRoot, string spriteName)
    {
        string assetPath = $"{layerRoot}/{spriteName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
            throw new FileNotFoundException($"Missing layered sprite: {assetPath}");
        return sprite;
    }

    public static Image AddImage(Transform parent, string layerRoot, string name, string spriteName, RectInt rect, bool preserveAspect, Color color)
    {
        Sprite sprite = LoadSprite(layerRoot, spriteName);
        GameObject gameObject = CreateRectObject(name, parent);
        ApplyTopLeftRect(gameObject.GetComponent<RectTransform>(), ToArray(rect));
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    public static Image AddSlicedImage(Transform parent, string layerRoot, string name, string spriteName, RectInt rect, Vector4 spriteBorder, Color color)
    {
        EnsureSpriteBorder($"{layerRoot}/{spriteName}", spriteBorder);
        Sprite sprite = LoadSprite(layerRoot, spriteName);
        GameObject gameObject = CreateRectObject(name, parent);
        ApplyTopLeftRect(gameObject.GetComponent<RectTransform>(), ToArray(rect));
        Image image = gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
    }

    public static Image AddFittedImage(Transform parent, string layerRoot, string name, string spriteName, RectInt slot, int maxWidth, int maxHeight, Color color)
    {
        WarlineUiImagePlacement placement = VisibleFittedPlacement(layerRoot, spriteName, slot, maxWidth, maxHeight);
        ValidateVisiblePlacement(name, slot, placement);
        return AddImage(parent, layerRoot, name, spriteName, placement.FullRect, true, color);
    }

    public static void AddCoverImage(Transform parent, string layerRoot, string name, string spriteName, RectInt slot, Color color)
    {
        Sprite sprite = LoadSprite(layerRoot, spriteName);
        GameObject maskObject = CreateRectObject($"{name}_Viewport", parent);
        ApplyTopLeftRect(maskObject.GetComponent<RectTransform>(), ToArray(slot));
        maskObject.AddComponent<RectMask2D>();

        float sourceW = Mathf.Max(1f, sprite.rect.width);
        float sourceH = Mathf.Max(1f, sprite.rect.height);
        float scale = Mathf.Max(slot.width / sourceW, slot.height / sourceH);
        int width = Mathf.CeilToInt(sourceW * scale);
        int height = Mathf.CeilToInt(sourceH * scale);

        GameObject imageObject = CreateRectObject(name, maskObject.transform);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    public static TMP_Text AddText(Transform parent, string name, string value, RectInt rect, float size, TextAlignmentOptions alignment, Color color, bool wordWrap = false)
    {
        GameObject gameObject = CreateRectObject(name, parent);
        ApplyTopLeftRect(gameObject.GetComponent<RectTransform>(), ToArray(rect));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = wordWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    public static WarlineUiImagePlacement VisibleFittedPlacement(string layerRoot, string spriteName, RectInt slot, int maxWidth, int maxHeight)
    {
        Sprite sprite = LoadSprite(layerRoot, spriteName);
        float sourceWidth = Mathf.Max(1f, sprite.rect.width);
        float sourceHeight = Mathf.Max(1f, sprite.rect.height);
        RectInt visibleSource = GetVisibleAlphaBounds(layerRoot, spriteName, sprite);
        float scale = Mathf.Min(
            Mathf.Min(maxWidth, slot.width) / sourceWidth,
            Mathf.Min(maxHeight, slot.height) / sourceHeight);

        int fittedWidth = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
        int fittedHeight = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));

        float sourceCenterX = sourceWidth * 0.5f;
        float sourceCenterY = sourceHeight * 0.5f;
        float visibleCenterX = visibleSource.x + visibleSource.width * 0.5f;
        float visibleCenterY = visibleSource.y + visibleSource.height * 0.5f;
        float visibleOffsetX = (visibleCenterX - sourceCenterX) * scale;
        float visibleOffsetY = (visibleCenterY - sourceCenterY) * scale;

        float fullCenterX = slot.center.x - visibleOffsetX;
        float fullCenterY = slot.center.y + visibleOffsetY;
        RectInt fullRect = new(
            Mathf.RoundToInt(fullCenterX - fittedWidth * 0.5f),
            Mathf.RoundToInt(fullCenterY - fittedHeight * 0.5f),
            fittedWidth,
            fittedHeight);
        RectInt visibleRect = new(
            fullRect.x + Mathf.RoundToInt(visibleSource.x * scale),
            fullRect.y + fittedHeight - Mathf.RoundToInt((visibleSource.y + visibleSource.height) * scale),
            Mathf.Max(1, Mathf.RoundToInt(visibleSource.width * scale)),
            Mathf.Max(1, Mathf.RoundToInt(visibleSource.height * scale)));
        return new WarlineUiImagePlacement(fullRect, visibleRect);
    }

    public static void ValidateSectionContent(string sectionName, RectInt safeRect, params WarlineUiRect[] items)
    {
        List<string> failures = new();
        foreach (WarlineUiRect item in items)
        {
            if (!Contains(safeRect, item.Rect))
                failures.Add($"{item.Name} rect={item.Rect} is outside safe={safeRect}");
        }

        for (int i = 0; i < items.Length; i++)
        {
            for (int j = i + 1; j < items.Length; j++)
            {
                if (Intersects(items[i].Rect, items[j].Rect))
                    failures.Add($"{items[i].Name} rect={items[i].Rect} overlaps {items[j].Name} rect={items[j].Rect}");
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException($"Layered UI layout invalid in {sectionName}: {string.Join("; ", failures)}");
    }

    public static void ValidateMajorPanels(params WarlineUiRect[] panels)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            for (int j = i + 1; j < panels.Length; j++)
            {
                if (Intersects(panels[i].Rect, panels[j].Rect))
                    throw new InvalidOperationException($"Layered UI major panel overlap: {panels[i].Name} {panels[i].Rect} overlaps {panels[j].Name} {panels[j].Rect}");
            }
        }
    }

    public static void AddHitZone(Transform parent, string name, RectInt rect)
    {
        GameObject zone = CreateRectObject(name, parent);
        ApplyTopLeftRect(zone.GetComponent<RectTransform>(), ToArray(rect));
        Image image = zone.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        Button button = zone.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
        colors.highlightedColor = new Color(1f, 0.78f, 0.25f, 0.12f);
        colors.pressedColor = new Color(1f, 0.62f, 0.12f, 0.20f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    public static void EnsureLayerSpriteImports(string layerRoot)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { layerRoot });
        if (guids.Length == 0)
            throw new FileNotFoundException($"No layer textures found under {layerRoot}");

        foreach (string guid in guids)
            EnsureSpriteImport(AssetDatabase.GUIDToAssetPath(guid));
    }

    public static void EnsureParentFolder(string path)
    {
        string folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);
    }

    public static void AddEventSystem()
    {
        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    public static Camera AddSceneCamera(int canvasHeight)
    {
        GameObject cameraObject = new("UICamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = canvasHeight * 0.5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    public static void CapturePrefab(string prefabPath, string outputPath, int width, int height, int canvasWidth, int canvasHeight, Color backgroundColor)
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
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            renderTexture.Create();

            cameraObject = new GameObject("LayeredUiCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = canvasHeight * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.targetTexture = renderTexture;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            canvasObject = CreateRectObject("LayeredUiCaptureRoot", null);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            RectTransform canvasTransform = (RectTransform)canvasObject.transform;
            canvasTransform.sizeDelta = new Vector2(canvasWidth, canvasHeight);
            canvasTransform.localPosition = Vector3.zero;
            canvasTransform.localScale = Vector3.one;

            instance = UnityEngine.Object.Instantiate(prefab, canvasObject.transform);
            instance.name = prefab.name;
            RectTransform instanceTransform = (RectTransform)instance.transform;
            instanceTransform.anchorMin = new Vector2(0.5f, 0.5f);
            instanceTransform.anchorMax = new Vector2(0.5f, 0.5f);
            instanceTransform.pivot = new Vector2(0.5f, 0.5f);
            instanceTransform.anchoredPosition = Vector2.zero;
            instanceTransform.sizeDelta = new Vector2(canvasWidth, canvasHeight);
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

    private static void EnsureSpriteImport(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }

    private static void EnsureSpriteBorder(string assetPath, Vector4 spriteBorder)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.spriteBorder = spriteBorder;
        importer.SaveAndReimport();
    }

    private static RectInt GetVisibleAlphaBounds(string layerRoot, string spriteName, Sprite sprite)
    {
        string cacheKey = $"{layerRoot}/{spriteName}";
        if (s_VisibleBoundsCache.TryGetValue(cacheKey, out RectInt cached))
            return cached;

        Texture2D texture = sprite.texture;
        Rect source = sprite.rect;
        int startX = Mathf.RoundToInt(source.x);
        int startY = Mathf.RoundToInt(source.y);
        int width = Mathf.RoundToInt(source.width);
        int height = Mathf.RoundToInt(source.height);
        Color32[] pixels = texture.GetPixels32();
        int textureWidth = texture.width;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < height; y++)
        {
            int row = (startY + y) * textureWidth;
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[row + startX + x];
                if (pixel.a <= 8)
                    continue;

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        RectInt bounds = maxX < minX || maxY < minY
            ? new RectInt(0, 0, width, height)
            : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        s_VisibleBoundsCache[cacheKey] = bounds;
        return bounds;
    }

    private static void ValidateVisiblePlacement(string name, RectInt slot, WarlineUiImagePlacement placement)
    {
        List<string> failures = new();
        if (!Contains(slot, placement.VisibleRect))
            failures.Add($"visible={placement.VisibleRect} is outside slot={slot}");

        Vector2 slotCenter = slot.center;
        Vector2 visibleCenter = placement.VisibleRect.center;
        if (Mathf.Abs(slotCenter.x - visibleCenter.x) > VisibleCenterTolerance)
            failures.Add($"visible center x={visibleCenter.x:0.0} expected={slotCenter.x:0.0}");
        if (Mathf.Abs(slotCenter.y - visibleCenter.y) > VisibleCenterTolerance)
            failures.Add($"visible center y={visibleCenter.y:0.0} expected={slotCenter.y:0.0}");

        if (failures.Count > 0)
            throw new InvalidOperationException($"Layered UI image placement invalid for {name}: {string.Join("; ", failures)}");
    }

    private static bool Contains(RectInt outer, RectInt inner)
    {
        return inner.xMin >= outer.xMin
            && inner.yMin >= outer.yMin
            && inner.xMax <= outer.xMax
            && inner.yMax <= outer.yMax;
    }

    private static bool Intersects(RectInt left, RectInt right)
    {
        return left.xMin < right.xMax
            && left.xMax > right.xMin
            && left.yMin < right.yMax
            && left.yMax > right.yMin;
    }
}
#endif
