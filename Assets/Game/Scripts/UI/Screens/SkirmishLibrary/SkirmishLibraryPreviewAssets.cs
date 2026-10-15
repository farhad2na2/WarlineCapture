namespace Game.UI.Runtime
{
    public static class SkirmishLibraryPreviewAssets
    {
        public const int Width = 512;
        public const int Height = 288;
        public const string Root = "Assets/Game/Art/SkirmishLibrary";
        private static UnityEngine.Texture2D _placeholder;

        public static string PreviewPath(string scenarioId) =>
            Root + "/Previews/skirmish.preview." + scenarioId + ".png";

        public static string PlatePath(string mapId) =>
            Root + "/Plates/skirmish.plate." + mapId + ".png";

        public static string ResolvePreviewKey(string scenarioId, bool playable, string mapId)
        {
            if (playable && !string.IsNullOrEmpty(scenarioId))
            {
                string path = PreviewPath(scenarioId);
                if (HasAsset(path))
                    return path;
                return "skirmish.preview." + scenarioId;
            }

            return ResolvePlateKey(mapId);
        }

        public static string ResolvePlateKey(string mapId)
        {
            string id = mapId ?? string.Empty;
            string path = PlatePath(id);
            return HasAsset(path) ? path : "skirmish.plate." + id;
        }

        public static bool HasAsset(string assetPath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assetPath))
                return false;
            string root = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
            return !string.IsNullOrEmpty(root) && System.IO.File.Exists(System.IO.Path.Combine(root, assetPath));
#else
            return false;
#endif
        }

        public static string CanonicalPath(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (key.StartsWith(Root + "/", System.StringComparison.Ordinal))
                return key;
            const string preview = "skirmish.preview.";
            const string plate = "skirmish.plate.";
            if (key.StartsWith(preview, System.StringComparison.Ordinal))
                return PreviewPath(key.Substring(preview.Length));
            if (key.StartsWith(plate, System.StringComparison.Ordinal))
                return PlatePath(key.Substring(plate.Length));
            return null;
        }

        public static UnityEngine.Texture2D LoadOrPlaceholder(string key)
        {
            UnityEngine.Texture2D loaded = Load(key);
            return loaded != null ? loaded : Placeholder();
        }

        public static UnityEngine.Texture2D Load(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            SkirmishLibrarySpriteRegistry registry = SkirmishLibrarySpriteRegistry.Load();
            if (registry != null && registry.TryGetTexture(key, out UnityEngine.Texture2D texture) && texture != null)
                return texture;
#if UNITY_EDITOR
            return ReadGeneratedTexture(key);
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private static readonly System.Collections.Generic.Dictionary<string, UnityEngine.Texture2D> GeneratedTextures =
            new System.Collections.Generic.Dictionary<string, UnityEngine.Texture2D>(System.StringComparer.Ordinal);

        /// <summary>
        /// Zone and blur plates are regenerated beside the Campaign plates and are not registry source.
        /// The editor reads those PNGs directly so a Build does not have to rewrite the sprite registry.
        /// </summary>
        private static UnityEngine.Texture2D ReadGeneratedTexture(string key)
        {
            if (GeneratedTextures.TryGetValue(key, out UnityEngine.Texture2D cached) && cached != null)
                return cached;
            string assetPath = GeneratedPath(key);
            if (string.IsNullOrEmpty(assetPath) || !HasAsset(assetPath))
                return null;
            string root = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
            byte[] bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(root ?? string.Empty, assetPath));
            var texture = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            texture.name = key;
            texture.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            GeneratedTextures[key] = texture;
            return texture;
        }

        private static string GeneratedPath(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (key.StartsWith(Root + "/Zones/", System.StringComparison.Ordinal) ||
                key.StartsWith(Root + "/Blurs/", System.StringComparison.Ordinal))
                return key;
            if (key.StartsWith("skirmish.zone.", System.StringComparison.Ordinal))
                return Root + "/Zones/" + key + ".png";
            if (key.StartsWith("skirmish.blur.", System.StringComparison.Ordinal))
                return Root + "/Blurs/" + key + ".png";
            return null;
        }
#endif

        public static UnityEngine.Texture2D Placeholder()
        {
            if (_placeholder != null)
                return _placeholder;

            var texture = new UnityEngine.Texture2D(Width, Height, UnityEngine.TextureFormat.RGBA32, false);
            var pixels = new UnityEngine.Color32[Width * Height];
            var edge = new UnityEngine.Color32(36, 42, 46, 255);
            var band = new UnityEngine.Color32(22, 26, 30, 255);
            var fill = new UnityEngine.Color32(12, 14, 16, 255);
            const int border = 8;
            const int bandWidth = 18;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    bool outer = x < border || y < border || x >= Width - border || y >= Height - border;
                    bool innerBand = x < bandWidth || y < bandWidth || x >= Width - bandWidth || y >= Height - bandWidth;
                    pixels[y * Width + x] = outer ? edge : innerBand ? band : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.name = "SkirmishLibraryPlaceholder";
            texture.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            _placeholder = texture;
            return texture;
        }
    }
}
