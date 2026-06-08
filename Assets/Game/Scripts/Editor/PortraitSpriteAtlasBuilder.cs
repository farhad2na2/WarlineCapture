using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class PortraitSpriteAtlasBuilder
{
    private const string PortraitRoot = "Assets/Game/Art/UI/Portraits/Generated";
    private const string SecondaryPortraitRoot = "Assets/Game/Art/UI/Portraits/Secondary";
    private const string AtlasRoot = "Assets/Game/Art/UI/Portraits/Atlases";

    [MenuItem("Game/UI/Rebuild Portrait Sprite Atlases")]
    public static void RebuildPortraitSpriteAtlases()
    {
        Directory.CreateDirectory(AtlasRoot);

        BuildAtlas(
            $"{AtlasRoot}/Portraits_Characters.spriteatlas",
            "Portraits_Characters",
            FindPortraitTextures("Portrait_Unit_Chr_"));

        BuildAtlas(
            $"{AtlasRoot}/Portraits_Vehicles.spriteatlas",
            "Portraits_Vehicles",
            FindPortraitTextures("Portrait_Unit_Veh_"));

        BuildAtlas(
            $"{AtlasRoot}/Portraits_Buildings.spriteatlas",
            "Portraits_Buildings",
            FindPortraitTextures("Portrait_Building_"));

        BuildAtlas(
            $"{AtlasRoot}/Portraits_Secondary.spriteatlas",
            "Portraits_Secondary",
            FindPortraitTextures(SecondaryPortraitRoot, "Portrait_"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PortraitSpriteAtlasBuilder] Portrait sprite atlases rebuilt.");
    }

    private static Object[] FindPortraitTextures(string prefix)
    {
        return FindPortraitTextures(PortraitRoot, prefix);
    }

    private static Object[] FindPortraitTextures(string root, string prefix)
    {
        var textures = new List<Object>();
        foreach (string path in Directory.GetFiles(root, $"{prefix}*.png", SearchOption.TopDirectoryOnly))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                textures.Add(texture);
        }

        textures.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
        return textures.ToArray();
    }

    private static void BuildAtlas(string atlasPath, string tag, Object[] packables)
    {
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        Object[] existingPackables = SpriteAtlasExtensions.GetPackables(atlas);
        if (existingPackables.Length > 0)
            SpriteAtlasExtensions.Remove(atlas, existingPackables);

        if (packables.Length > 0)
            SpriteAtlasExtensions.Add(atlas, packables);

        var packingSettings = new SpriteAtlasPackingSettings
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 4
        };
        SpriteAtlasExtensions.SetPackingSettings(atlas, packingSettings);

        var textureSettings = new SpriteAtlasTextureSettings
        {
            filterMode = FilterMode.Bilinear,
            generateMipMaps = false,
            readable = false,
            sRGB = true
        };
        SpriteAtlasExtensions.SetTextureSettings(atlas, textureSettings);

        SetPlatformSettings(atlas, "DefaultTexturePlatform", 4096, TextureImporterFormat.Automatic);
        SetPlatformSettings(atlas, "Android", 4096, TextureImporterFormat.ASTC_6x6);
        SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
        atlas.name = tag;
        EditorUtility.SetDirty(atlas);
    }

    private static void SetPlatformSettings(SpriteAtlas atlas, string platformName, int maxTextureSize, TextureImporterFormat format)
    {
        var platformSettings = new TextureImporterPlatformSettings
        {
            name = platformName,
            overridden = platformName != "DefaultTexturePlatform",
            maxTextureSize = maxTextureSize,
            format = format,
            compressionQuality = 50
        };
        SpriteAtlasExtensions.SetPlatformSettings(atlas, platformSettings);
    }
}
