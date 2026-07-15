#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public sealed class Aph501AndroidMainMenuTexturePolicy : AssetPostprocessor
    {
        internal const string SpriteFolder =
            "Assets/Game/Art/UI/Generated/MainMenuBrightCommand/Sprites";
        internal const string AndroidPlatform = "Android";
        internal const int AndroidMaxTextureSize = 2048;
        internal const int AndroidCompressionQuality = 100;

        [MenuItem("Warline Capture/Performance/Apply Android Main Menu Texture Policy")]
        public static void ApplyToTrackedSprites()
        {
            string[] paths = FindTrackedSpritePaths();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException(
                    $"[APH-501 MainMenuTexturePolicy] No textures found in '{SpriteFolder}'.");
            }

            int changedCount = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    throw new InvalidOperationException(
                        $"[APH-501 MainMenuTexturePolicy] Expected a sprite texture importer at '{path}'.");
                }

                if (!ApplyTrackedPolicy(importer))
                    continue;

                changedCount++;
                importer.SaveAndReimport();
            }

            Debug.Log(
                $"[APH-501 MainMenuTexturePolicy] Verified {paths.Length} sprites; " +
                $"reimported {changedCount} with Android ASTC 4x4 high-quality compression.");
        }

        private void OnPreprocessTexture()
        {
            if (!IsTrackedSpritePath(assetPath))
                return;

            TextureImporter importer = assetImporter as TextureImporter;
            if (importer != null)
                ApplyTrackedPolicy(importer);
        }

        internal static bool IsTrackedSpritePath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalizedPath = assetPath.Replace('\\', '/').Trim();
            if (!string.Equals(
                    Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/'),
                    SpriteFolder,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(
                Path.GetExtension(normalizedPath),
                ".png",
                StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ApplyTrackedPolicy(TextureImporter importer)
        {
            bool changed = importer.mipmapEnabled;
            importer.mipmapEnabled = false;

            TextureImporterPlatformSettings settings =
                importer.GetPlatformTextureSettings(AndroidPlatform);
            bool androidChanged =
                !settings.overridden ||
                settings.maxTextureSize != AndroidMaxTextureSize ||
                settings.format != TextureImporterFormat.ASTC_4x4 ||
                settings.textureCompression != TextureImporterCompression.CompressedHQ ||
                settings.compressionQuality != AndroidCompressionQuality;

            if (!androidChanged)
                return changed;

            settings.name = AndroidPlatform;
            settings.overridden = true;
            settings.maxTextureSize = AndroidMaxTextureSize;
            settings.format = TextureImporterFormat.ASTC_4x4;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = AndroidCompressionQuality;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        private static string[] FindTrackedSpritePaths()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder });
            var paths = new List<string>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsTrackedSpritePath(path))
                    paths.Add(path);
            }

            paths.Sort(StringComparer.Ordinal);
            return paths.ToArray();
        }
    }
}

#endif
