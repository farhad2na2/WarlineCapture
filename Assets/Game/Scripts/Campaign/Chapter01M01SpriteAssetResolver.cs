using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Chapter01M01SpriteAssetResolver
{
    public const string ManifestPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_asset_manifest.asset";
    public const string ScaleContractPath = "Assets/Game/Data/TacticalMaps/Chapter01/chapter01_tactical_scale_contract.asset";

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    public static bool TryGetSprite(string spriteId, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(spriteId))
            return false;

        if (SpriteCache.TryGetValue(spriteId, out sprite))
            return sprite != null;

#if UNITY_EDITOR
        Chapter01TacticalAssetManifest manifest = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAssetManifest>(ManifestPath);
        if (manifest == null || !manifest.TryGetEntry(spriteId, out TacticalAssetManifestEntry entry))
            return false;

        if (!TryCreateSpriteFromPng(entry.PlannedPath, out sprite))
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.PlannedPath);
        SpriteCache[spriteId] = sprite;
        return sprite != null;
#else
        return false;
#endif
    }

    public static bool TryGetScale(string spriteId, out float scale)
    {
        scale = 1f;
#if UNITY_EDITOR
        Chapter01TacticalAssetManifest manifest = AssetDatabase.LoadAssetAtPath<Chapter01TacticalAssetManifest>(ManifestPath);
        Chapter01TacticalScaleContract scaleContract = AssetDatabase.LoadAssetAtPath<Chapter01TacticalScaleContract>(ScaleContractPath);
        if (manifest == null || scaleContract == null || !manifest.TryGetEntry(spriteId, out TacticalAssetManifestEntry entry) || !entry.UsesScaleRole)
            return false;

        scale = scaleContract.GetScale(entry.ScaleRole);
        return true;
#else
        return false;
#endif
    }

    public static void ClearCache()
    {
        SpriteCache.Clear();
    }

#if UNITY_EDITOR
    private static bool TryCreateSpriteFromPng(string assetPath, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(assetPath) || Path.GetExtension(assetPath).ToLowerInvariant() != ".png")
            return false;

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        if (!File.Exists(fullPath))
            return false;

        byte[] bytes = File.ReadAllBytes(fullPath);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(assetPath),
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        if (!ImageConversion.LoadImage(texture, bytes))
        {
            Object.DestroyImmediate(texture);
            return false;
        }

        float pixelsPerUnit = 100f;
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            pixelsPerUnit = importer.spritePixelsPerUnit;

        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            1u,
            SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return true;
    }
#endif
}
