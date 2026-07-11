#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public sealed class Aph503TextureStreamingGuard : AssetPostprocessor
    {
        internal const string RuntimeImpostorFolder = "Assets/Game/Textures/Generated/Impostors/";

        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null || !importer.streamingMipmaps)
                return;

            TextureStreamingGuardDecision decision = Evaluate(assetPath, importer.textureType);
            if (!decision.MustDisableStreaming)
                return;

            importer.streamingMipmaps = false;
            Debug.LogWarning(
                $"[APH-503 TextureStreamingGuard] Disabled mip streaming for '{assetPath}'. " +
                $"Reason: {decision.Reason} Configure streaming only for measured world textures; " +
                "do not bypass this guard from an importer preset or asset generator.");
        }

        public static TextureStreamingGuardDecision Evaluate(
            string assetPath,
            TextureImporterType textureType)
        {
            if (!TryNormalizeTexturePath(assetPath, out string path, out string failureReason))
                return TextureStreamingGuardDecision.Protected(failureReason);

            if (path.StartsWith(RuntimeImpostorFolder, StringComparison.OrdinalIgnoreCase))
                return TextureStreamingGuardDecision.Allowed("runtime world-unit impostor atlas allowlist");

            if (textureType == TextureImporterType.Sprite)
                return TextureStreamingGuardDecision.Protected("sprite/UI textures require stable full-resolution presentation");

            if (ContainsPathSegment(path, "UI") || ContainsPathSegment(path, "UserInterface"))
                return TextureStreamingGuardDecision.Protected("UI texture path");

            if (ContainsPathSegment(path, "Fonts") || ContainsPathSegment(path, "Font") ||
                ContainsFileToken(path, "font") || ContainsFileToken(path, "sdf"))
                return TextureStreamingGuardDecision.Protected("font or font-atlas texture");

            if (ContainsPathSegment(path, "AnimationData") ||
                ContainsPathSegment(path, "AnimationTextures") ||
                ContainsFileToken(path, "animationtexture"))
                return TextureStreamingGuardDecision.Protected("animation-data texture");

            if (ContainsPathSegment(path, "SpriteAtlas") ||
                ContainsPathSegment(path, "SpriteAtlases") ||
                ContainsFileToken(path, "spriteatlas"))
                return TextureStreamingGuardDecision.Protected("sprite-atlas texture");

            if (ContainsPathSegment(path, "References") || ContainsPathSegment(path, "Reference") ||
                ContainsPathSegment(path, "Sources") || ContainsPathSegment(path, "Source") ||
                ContainsFileToken(path, "targetlock") || ContainsFileToken(path, "visuallock") ||
                ContainsFileToken(path, "mockup"))
                return TextureStreamingGuardDecision.Protected("generated or authored reference/source texture");

            return TextureStreamingGuardDecision.Allowed("not in an APH-503 protected texture class");
        }

        private static bool TryNormalizeTexturePath(
            string assetPath,
            out string normalizedPath,
            out string failureReason)
        {
            normalizedPath = string.Empty;
            failureReason = string.Empty;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                failureReason = "missing asset path; classification failed closed";
                return false;
            }

            normalizedPath = assetPath.Replace('\\', '/').Trim();
            if (!normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.Contains("../", StringComparison.Ordinal) ||
                normalizedPath.EndsWith("/", StringComparison.Ordinal) ||
                string.IsNullOrEmpty(Path.GetExtension(normalizedPath)))
            {
                failureReason = $"malformed or non-project texture path '{assetPath}'; classification failed closed";
                normalizedPath = string.Empty;
                return false;
            }

            return true;
        }

        private static bool ContainsPathSegment(string path, string segment)
        {
            return path.IndexOf($"/{segment}/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsFileToken(string path, string token)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            return fileName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public readonly struct TextureStreamingGuardDecision
    {
        private TextureStreamingGuardDecision(bool mustDisableStreaming, string reason)
        {
            MustDisableStreaming = mustDisableStreaming;
            Reason = reason;
        }

        public bool MustDisableStreaming { get; }

        public string Reason { get; }

        public static TextureStreamingGuardDecision Protected(string reason)
        {
            return new TextureStreamingGuardDecision(true, reason);
        }

        public static TextureStreamingGuardDecision Allowed(string reason)
        {
            return new TextureStreamingGuardDecision(false, reason);
        }
    }
}

#endif
