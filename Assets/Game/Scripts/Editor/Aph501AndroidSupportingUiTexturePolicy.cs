#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public sealed class Aph501AndroidSupportingUiTexturePolicy : AssetPostprocessor
    {
        internal const string MatchHudRoot = "Assets/Game/Art/UI/Generated/MatchHUD";
        internal const string MainMenuV15CRoot =
            "Assets/Game/Art/UI/Generated/MainMenuV15C/LayeredOneGo";
        internal const string AndroidPlatform = "Android";
        internal const int AndroidMaxTextureSize = 2048;
        internal const int AndroidCompressionQuality = 100;

        private static readonly string[] Roots = { MatchHudRoot, MainMenuV15CRoot };

        [MenuItem("Game/Performance/Apply Android Supporting UI Texture Policy")]
        public static void ApplyToTrackedSprites()
        {
            string[] paths = FindTrackedSpritePaths();
            if (paths.Length == 0)
            {
                throw new InvalidOperationException(
                    "[APH-501 SupportingUiTexturePolicy] No tracked sprite textures were found.");
            }

            int changedCount = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    throw new InvalidOperationException(
                        $"[APH-501 SupportingUiTexturePolicy] Expected a sprite texture importer at '{path}'.");
                }

                if (!ApplyTrackedPolicy(importer))
                    continue;

                changedCount++;
                importer.SaveAndReimport();
            }

            Debug.Log(
                $"[APH-501 SupportingUiTexturePolicy] Verified {paths.Length} sprites; " +
                $"reimported {changedCount} with mipless Android ASTC 6x6 high-quality compression.");
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
            if (!normalizedPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("/../", StringComparison.Ordinal) ||
                normalizedPath.Contains("/./", StringComparison.Ordinal))
            {
                return false;
            }

            for (int i = 0; i < Roots.Length; i++)
            {
                if (normalizedPath.StartsWith(Roots[i] + "/", StringComparison.Ordinal))
                    return true;
            }

            return false;
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
                settings.format != TextureImporterFormat.ASTC_6x6 ||
                settings.textureCompression != TextureImporterCompression.CompressedHQ ||
                settings.compressionQuality != AndroidCompressionQuality;

            if (!androidChanged)
                return changed;

            settings.name = AndroidPlatform;
            settings.overridden = true;
            settings.maxTextureSize = AndroidMaxTextureSize;
            settings.format = TextureImporterFormat.ASTC_6x6;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = AndroidCompressionQuality;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        private static string[] FindTrackedSpritePaths()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", Roots);
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
