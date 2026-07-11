using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class FirstLaunchNarrativeArtImporter
    {
        private const string PanelRoot = "Assets/Game/Art/Narrative/FirstLaunch/Panels";
        private const int ExpectedTextureCount = 44;

        [MenuItem("Game/Narrative/Configure Approved FirstLaunch Art Imports")]
        public static void ConfigureApprovedArtImports()
        {
            string[] assetPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { PanelRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (assetPaths.Length != ExpectedTextureCount)
            {
                throw new InvalidOperationException(
                    $"Expected {ExpectedTextureCount} approved FirstLaunch panel textures, found {assetPaths.Length}.");
            }

            foreach (string assetPath in assetPaths)
            {
                ConfigureTexture(assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Configured {assetPaths.Length} approved FirstLaunch narrative panel textures.");
        }

        private static void ConfigureTexture(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Texture importer not found for {assetPath}.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.streamingMipmaps = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 4096;

            SetPlatform(importer, "Android", TextureImporterFormat.ASTC_6x6);
            SetPlatform(importer, "iPhone", TextureImporterFormat.ASTC_6x6);
            importer.SaveAndReimport();
        }

        private static void SetPlatform(
            TextureImporter importer,
            string platform,
            TextureImporterFormat format)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = 4096,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                format = format,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
            });
        }
    }
}
