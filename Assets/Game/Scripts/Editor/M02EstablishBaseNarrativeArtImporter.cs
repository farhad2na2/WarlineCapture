using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class M02EstablishBaseNarrativeArtImporter
    {
        private static bool configured;

        public const string PanelRoot =
            "Assets/Game/Art/Narrative/M02EstablishBase/Provisional";
        public const string BriefPanelPath = PanelRoot + "/M02-P01-Brief.png";
        public const string CommsPanelPath = PanelRoot + "/M02-P02-Comms.png";
        public const string DebriefPanelPath = PanelRoot + "/M02-P03-Debrief.png";

        [MenuItem("Game/Campaign/M02/Configure Provisional Narrative Art")]
        public static void ConfigureProvisionalArtImports()
        {
            if (configured && PanelsAreAvailable())
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { PanelRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] expected = { BriefPanelPath, CommsPanelPath, DebriefPanelPath };
            if (!paths.SequenceEqual(expected, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"M02 provisional panel set mismatch. Expected [{string.Join(", ", expected)}], " +
                    $"found [{string.Join(", ", paths)}].");
            }

            foreach (string path in paths)
                ConfigureTexture(path);
            AssetDatabase.SaveAssets();
            configured = true;
            Debug.Log("[M02EstablishBaseNarrativeArtImporter] result=Passed panels=3 provisional=1");
        }

        private static bool PanelsAreAvailable() =>
            AssetDatabase.LoadAssetAtPath<Sprite>(BriefPanelPath) != null &&
            AssetDatabase.LoadAssetAtPath<Sprite>(CommsPanelPath) != null &&
            AssetDatabase.LoadAssetAtPath<Sprite>(DebriefPanelPath) != null;

        private static void ConfigureTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Texture importer not found for {path}.");

            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           Math.Abs(importer.spritePixelsPerUnit - 100f) > 0.001f ||
                           !importer.sRGBTexture ||
                           importer.alphaSource != TextureImporterAlphaSource.None ||
                           importer.alphaIsTransparency || importer.mipmapEnabled ||
                           importer.streamingMipmaps || importer.isReadable ||
                           importer.npotScale != TextureImporterNPOTScale.None ||
                           importer.wrapMode != TextureWrapMode.Clamp ||
                           importer.filterMode != FilterMode.Bilinear ||
                           importer.textureCompression != TextureImporterCompression.CompressedHQ ||
                           importer.maxTextureSize != 4096;

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
            changed |= ConfigurePlatform(importer, "Android", TextureImporterFormat.ASTC_6x6);
            changed |= ConfigurePlatform(importer, "iPhone", TextureImporterFormat.ASTC_6x6);

            if (changed)
                importer.SaveAndReimport();
        }

        private static bool ConfigurePlatform(
            TextureImporter importer,
            string platform,
            TextureImporterFormat format)
        {
            TextureImporterPlatformSettings current = importer.GetPlatformTextureSettings(platform);
            bool changed = !current.overridden || current.maxTextureSize != 4096 ||
                           current.resizeAlgorithm != TextureResizeAlgorithm.Mitchell ||
                           current.format != format ||
                           current.textureCompression != TextureImporterCompression.CompressedHQ ||
                           current.compressionQuality != 100;
            if (!changed)
                return false;

            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = 4096,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                format = format,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100
            });
            return true;
        }
    }
}
