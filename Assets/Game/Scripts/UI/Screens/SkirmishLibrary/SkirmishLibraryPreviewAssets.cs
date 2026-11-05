using UnityEngine;

namespace Game.UI.Runtime
{
    public static class SkirmishLibraryPreviewAssets
    {
        public const int Width = 512;
        public const int Height = 288;
        public const string Root = "Assets/Game/Art/SkirmishLibrary";
        private static Texture2D _placeholder;

        public static string PreviewPath(string scenarioId) =>
            Root + "/Previews/skirmish.preview." + scenarioId + ".png";

        public static string PlatePath(string mapId) =>
            Root + "/Plates/skirmish.plate." + mapId + ".png";

        public const string TheatreBaseKey = "skirmish.theatre.base";

        public static string TheatreBasePath => Root + "/Theatre/" + TheatreBaseKey + ".png";

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
            const string zone = "skirmish.zone.";
            const string blur = "skirmish.blur.";
            if (key.StartsWith(zone, System.StringComparison.Ordinal))
                return Root + "/Zones/" + key + ".png";
            if (key.StartsWith(blur, System.StringComparison.Ordinal))
                return Root + "/Blurs/" + key + ".png";
            if (string.Equals(key, TheatreBaseKey, System.StringComparison.Ordinal))
                return TheatreBasePath;
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
            return null;
        }

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

        public static Color32 Grade(Color32 sample)
        {
            float luminance = (sample.r * 0.30f + sample.g * 0.52f + sample.b * 0.18f) / 255f;
            int step = luminance < 0.38f ? 0 : luminance < 0.52f ? 1 : luminance < 0.64f ? 2 : luminance < 0.76f ? 3 : 4;
            switch (step)
            {
                case 0: return new Color32(90, 28, 10, 255);
                case 1: return new Color32(190, 90, 22, 255);
                case 2: return new Color32(220, 132, 32, 255);
                case 3: return new Color32(236, 168, 64, 255);
                default: return new Color32(252, 214, 140, 255);
            }
        }

        public static Color32[] PosterizeFill(Color32[] source, int width, int height, int blurRadius)
        {
            if (source == null || width < 1 || height < 1 || source.Length < width * height)
                return source;
            int smallWidth = Mathf.Max(1, width / 4);
            int smallHeight = Mathf.Max(1, height / 4);
            Color32[] small = DownscaleBox(source, width, height, smallWidth, smallHeight);
            if (blurRadius > 0)
                small = BoxBlur(small, smallWidth, smallHeight, blurRadius);
            Color32[] full = UpscaleBilinear(small, smallWidth, smallHeight, width, height);
            full = BoxBlur(full, width, height, 1);
            for (int i = 0; i < full.Length; i++)
                full[i] = Grade(full[i]);
            return full;
        }

        private static Color32[] DownscaleBox(Color32[] source, int width, int height, int smallWidth, int smallHeight)
        {
            var dest = new Color32[smallWidth * smallHeight];
            for (int y = 0; y < smallHeight; y++)
            {
                int y0 = y * height / smallHeight;
                int y1 = Mathf.Max(y0 + 1, (y + 1) * height / smallHeight);
                for (int x = 0; x < smallWidth; x++)
                {
                    int x0 = x * width / smallWidth;
                    int x1 = Mathf.Max(x0 + 1, (x + 1) * width / smallWidth);
                    int red = 0;
                    int green = 0;
                    int blue = 0;
                    int count = 0;
                    for (int sy = y0; sy < y1; sy++)
                    {
                        for (int sx = x0; sx < x1; sx++)
                        {
                            Color32 sample = source[sy * width + sx];
                            red += sample.r;
                            green += sample.g;
                            blue += sample.b;
                            count++;
                        }
                    }

                    dest[y * smallWidth + x] = new Color32((byte)(red / count), (byte)(green / count), (byte)(blue / count), 255);
                }
            }

            return dest;
        }

        private static Color32[] UpscaleBilinear(Color32[] source, int smallWidth, int smallHeight, int width, int height)
        {
            var dest = new Color32[width * height];
            float xScale = smallWidth / (float)width;
            float yScale = smallHeight / (float)height;
            for (int y = 0; y < height; y++)
            {
                float sy = (y + 0.5f) * yScale - 0.5f;
                int y0 = Mathf.FloorToInt(sy);
                float fy = sy - y0;
                if (y0 < 0)
                {
                    y0 = 0;
                    fy = 0f;
                }

                int y1 = Mathf.Min(y0 + 1, smallHeight - 1);
                y0 = Mathf.Clamp(y0, 0, smallHeight - 1);
                for (int x = 0; x < width; x++)
                {
                    float sx = (x + 0.5f) * xScale - 0.5f;
                    int x0 = Mathf.FloorToInt(sx);
                    float fx = sx - x0;
                    if (x0 < 0)
                    {
                        x0 = 0;
                        fx = 0f;
                    }

                    int x1 = Mathf.Min(x0 + 1, smallWidth - 1);
                    x0 = Mathf.Clamp(x0, 0, smallWidth - 1);
                    Color32 top = Lerp(source[y0 * smallWidth + x0], source[y0 * smallWidth + x1], fx);
                    Color32 bottom = Lerp(source[y1 * smallWidth + x0], source[y1 * smallWidth + x1], fx);
                    dest[y * width + x] = Lerp(top, bottom, fy);
                }
            }

            return dest;
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)), 0, 255),
                255);
        }

        private static Color32[] BoxBlur(Color32[] source, int width, int height, int radius)
        {
            var dest = new Color32[source.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int red = 0;
                    int green = 0;
                    int blue = 0;
                    int count = 0;
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int sy = Mathf.Clamp(y + ky, 0, height - 1);
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int sx = Mathf.Clamp(x + kx, 0, width - 1);
                            Color32 sample = source[sy * width + sx];
                            red += sample.r;
                            green += sample.g;
                            blue += sample.b;
                            count++;
                        }
                    }

                    dest[y * width + x] = new Color32((byte)(red / count), (byte)(green / count), (byte)(blue / count), 255);
                }
            }

            return dest;
        }
    }
}
