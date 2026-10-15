#if UNITY_EDITOR
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Paints one warm theatre plate from the existing Campaign plates. Each pixel
    /// keeps a single plate, then a brown-to-sand ramp, a thick region border and
    /// a vignette. It does not author new terrain.
    /// </summary>
    public static class SkirmishLibraryZoneComposer
    {
        public const int ZoneWidth = 960;
        public const int ZoneHeight = 540;
        public const int BlurWidth = 160;
        public const int BlurHeight = 90;
        private const float Warp = 0.12f;
        private const int BorderBand = 16;
        private const int DarkStroke = 5;

        public static void Generate()
        {
            string zoneFolder = SkirmishLibraryPreviewAssets.Root + "/Zones";
            string blurFolder = SkirmishLibraryPreviewAssets.Root + "/Blurs";
            EnsureFolder(zoneFolder);
            EnsureFolder(blurFolder);

            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            Rect[] regions = SkirmishBattleLibraryArt.TheaterRegions;
            var plates = new Texture2D[mapIds.Length];
            var pixels = new Color32[mapIds.Length][];
            var sizes = new Vector2Int[mapIds.Length];
            try
            {
                for (int i = 0; i < mapIds.Length; i++)
                {
                    plates[i] = LoadPlate(mapIds[i]);
                    pixels[i] = plates[i].GetPixels32();
                    sizes[i] = new Vector2Int(plates[i].width, plates[i].height);
                }

                Color32[] composite = Compose(regions, pixels, sizes, out byte[] owner);
                WriteSprite(zoneFolder + "/skirmish.zone.theater.png", composite, ZoneWidth, ZoneHeight);
                for (int i = 0; i < mapIds.Length; i++)
                {
                    WriteZoneWindow(zoneFolder, mapIds[i], (byte)i, owner, composite);
                    WriteBlur(blurFolder, "skirmish.blur." + mapIds[i], pixels[i], sizes[i]);
                }

                WriteTheaterBlur(blurFolder, composite);
            }
            finally
            {
                for (int i = 0; i < plates.Length; i++)
                {
                    if (plates[i] != null)
                        Object.DestroyImmediate(plates[i]);
                }
            }
        }

        private static Texture2D LoadPlate(string mapId)
        {
            string assetPath = SkirmishLibraryPreviewAssets.PlatePath(mapId);
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolute = Path.Combine(projectRoot ?? string.Empty, assetPath);
            if (!File.Exists(absolute))
                throw new FileNotFoundException("Theatre zone plate is missing: " + assetPath);
            byte[] bytes = File.ReadAllBytes(absolute);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!UnityEngine.ImageConversion.LoadImage(texture, bytes))
                throw new InvalidDataException("Theatre zone plate could not be read: " + assetPath);
            return texture;
        }

        private static Color32[] Compose(Rect[] regions, Color32[][] plates, Vector2Int[] sizes, out byte[] owner)
        {
            int count = ZoneWidth * ZoneHeight;
            var pixels = new Color32[count];
            owner = new byte[count];
            for (int y = 0; y < ZoneHeight; y++)
            {
                float vTop = 1f - (y + 0.5f) / ZoneHeight;
                for (int x = 0; x < ZoneWidth; x++)
                {
                    float u = (x + 0.5f) / ZoneWidth;
                    int best = 0;
                    float bestInset = -1000f;
                    for (int i = 0; i < regions.Length; i++)
                    {
                        float inset = RegionInset(regions[i], u, vTop, i);
                        if (inset > bestInset)
                        {
                            bestInset = inset;
                            best = i;
                        }
                    }

                    int index = y * ZoneWidth + x;
                    owner[index] = (byte)best;
                    Rect region = regions[best];
                    float pu = region.width > 0.001f ? Mathf.Clamp01((u - region.x) / region.width) : 0.5f;
                    float pv = region.height > 0.001f ? Mathf.Clamp01(1f - (vTop - region.y) / region.height) : 0.5f;
                    pixels[index] = Grade(Sample(plates[best], sizes[best], pu, pv));
                }
            }

            pixels = Bilateral(pixels, ZoneWidth, ZoneHeight, 4, 2.6f, 0.18f);
            PaintThickBorders(pixels, owner);
            Vignette(pixels);
            return pixels;
        }

        private static string IndexMap(int index)
        {
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            return index >= 0 && index < mapIds.Length ? mapIds[index] : "DB";
        }

        private static float RegionInset(Rect region, float u, float vTop, int index)
        {
            if (region.width <= 0.001f || region.height <= 0.001f)
                return -1f;
            float wobble = RegionWobble(u, vTop, index);
            float left = u - (region.x + wobble);
            float right = region.xMax - u + wobble * 0.85f;
            float top = vTop - (region.y + wobble * 0.9f);
            float bottom = region.yMax - vTop + wobble * 0.75f;
            return Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
        }

        private static float RegionWobble(float u, float vTop, int index)
        {
            float originU = u + index * 0.41f;
            float originV = vTop + index * 0.27f;
            float coarse = BoundaryNoise(originU, originV, 8f) * 0.62f;
            float fine = BoundaryNoise(originU + index * 1.7f, originV + index * 0.9f, 17f) * 0.38f;
            return (coarse + fine) * Warp;
        }

        private static float BoundaryNoise(float u, float v, float scale)
        {
            float x = u * scale;
            float y = v * scale;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = x - x0;
            float ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float n00 = Hash01(x0, y0);
            float n10 = Hash01(x0 + 1, y0);
            float n01 = Hash01(x0, y0 + 1);
            float n11 = Hash01(x0 + 1, y0 + 1);
            float mixed = Mathf.Lerp(Mathf.Lerp(n00, n10, tx), Mathf.Lerp(n01, n11, tx), ty);
            return mixed * 2f - 1f;
        }

        private static float Hash01(int x, int y)
        {
            int n = x * 374761393 + y * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            n ^= n >> 16;
            return (n & 1023) / 1023f;
        }

        private static void PaintThickBorders(Color32[] pixels, byte[] owner)
        {
            int count = ZoneWidth * ZoneHeight;
            var distance = new int[count];
            for (int i = 0; i < count; i++)
                distance[i] = 99;
            for (int y = 0; y < ZoneHeight; y++)
            {
                for (int x = 0; x < ZoneWidth; x++)
                {
                    int index = y * ZoneWidth + x;
                    byte mine = owner[index];
                    bool edge = (x > 0 && owner[index - 1] != mine)
                        || (x + 1 < ZoneWidth && owner[index + 1] != mine)
                        || (y > 0 && owner[index - ZoneWidth] != mine)
                        || (y + 1 < ZoneHeight && owner[index + ZoneWidth] != mine);
                    if (edge)
                        distance[index] = 0;
                }
            }

            for (int y = 0; y < ZoneHeight; y++)
            {
                for (int x = 0; x < ZoneWidth; x++)
                {
                    int index = y * ZoneWidth + x;
                    int next = distance[index];
                    if (y > 0)
                        next = Mathf.Min(next, distance[index - ZoneWidth] + 1);
                    if (x > 0)
                        next = Mathf.Min(next, distance[index - 1] + 1);
                    if (y > 0 && x > 0)
                        next = Mathf.Min(next, distance[index - ZoneWidth - 1] + 1);
                    if (y > 0 && x + 1 < ZoneWidth)
                        next = Mathf.Min(next, distance[index - ZoneWidth + 1] + 1);
                    distance[index] = next;
                }
            }

            for (int y = ZoneHeight - 1; y >= 0; y--)
            {
                for (int x = ZoneWidth - 1; x >= 0; x--)
                {
                    int index = y * ZoneWidth + x;
                    int next = distance[index];
                    if (y + 1 < ZoneHeight)
                        next = Mathf.Min(next, distance[index + ZoneWidth] + 1);
                    if (x + 1 < ZoneWidth)
                        next = Mathf.Min(next, distance[index + 1] + 1);
                    if (y + 1 < ZoneHeight && x + 1 < ZoneWidth)
                        next = Mathf.Min(next, distance[index + ZoneWidth + 1] + 1);
                    if (y + 1 < ZoneHeight && x > 0)
                        next = Mathf.Min(next, distance[index + ZoneWidth - 1] + 1);
                    distance[index] = next;
                }
            }

            var inkDark = new Color32(14, 8, 5, 255);
            for (int i = 0; i < count; i++)
            {
                int nearest = distance[i];
                if (nearest > BorderBand)
                    continue;
                if (nearest <= DarkStroke)
                {
                    pixels[i] = inkDark;
                    continue;
                }

                Color32 ink = (Color32)ZoneColor(IndexMap(owner[i]));
                Color32 under = pixels[i];
                pixels[i] = new Color32(
                    (byte)Mathf.Lerp(under.r, ink.r, 0.94f),
                    (byte)Mathf.Lerp(under.g, ink.g, 0.94f),
                    (byte)Mathf.Lerp(under.b, ink.b, 0.94f),
                    255);
            }
        }

        private static void Vignette(Color32[] pixels)
        {
            for (int y = 0; y < ZoneHeight; y++)
            {
                float dy = ((y + 0.5f) / ZoneHeight - 0.5f) * 1.28f;
                for (int x = 0; x < ZoneWidth; x++)
                {
                    float dx = ((x + 0.5f) / ZoneWidth - 0.5f) * 1.1f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy) / 0.70f;
                    float shade = Mathf.Clamp01(radius);
                    shade = Mathf.Pow(shade, 1.5f);
                    float gain = 1f - 0.62f * shade;
                    int index = y * ZoneWidth + x;
                    Color32 sample = pixels[index];
                    pixels[index] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(sample.r * gain), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(sample.g * gain), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(sample.b * gain), 0, 255),
                        255);
                }
            }
        }

        private static Color32[] Bilateral(Color32[] source, int width, int height, int radius, float sigmaSpace, float sigmaRange)
        {
            int count = width * height;
            var luminance = new float[count];
            for (int i = 0; i < count; i++)
            {
                Color32 sample = source[i];
                luminance[i] = (sample.r * 0.30f + sample.g * 0.50f + sample.b * 0.20f) / 255f;
            }

            var dest = new Color32[count];
            float rangeScale = 1f / (2f * sigmaRange * sigmaRange);
            float spaceScale = 1f / (2f * sigmaSpace * sigmaSpace);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    float weight = 0f;
                    float red = 0f;
                    float green = 0f;
                    float blue = 0f;
                    float center = luminance[index];
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int sy = Mathf.Clamp(y + ky, 0, height - 1);
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int sx = Mathf.Clamp(x + kx, 0, width - 1);
                            int sampleIndex = sy * width + sx;
                            float delta = center - luminance[sampleIndex];
                            float sampleWeight = Mathf.Exp(-(kx * kx + ky * ky) * spaceScale - delta * delta * rangeScale);
                            Color32 sample = source[sampleIndex];
                            red += sample.r * sampleWeight;
                            green += sample.g * sampleWeight;
                            blue += sample.b * sampleWeight;
                            weight += sampleWeight;
                        }
                    }

                    float inverse = weight > 0.001f ? 1f / weight : 0f;
                    dest[index] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(red * inverse), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(green * inverse), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(blue * inverse), 0, 255),
                        255);
                }
            }

            return dest;
        }

        private static void WriteZoneWindow(string folder, string mapId, byte regionIndex, byte[] owner, Color32[] composite)
        {
            var pixels = new Color32[ZoneWidth * ZoneHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                if (owner == null || i >= owner.Length || owner[i] != regionIndex)
                    continue;
                Color32 sample = composite[i];
                sample.a = 255;
                pixels[i] = sample;
            }

            WriteSprite(folder + "/skirmish.zone." + mapId + ".png", pixels, ZoneWidth, ZoneHeight);
        }

        private static Color32 Grade(Color32 sample)
        {
            float luminance = (sample.r * 0.30f + sample.g * 0.52f + sample.b * 0.18f) / 255f;
            luminance = Mathf.Clamp01((luminance - 0.40f) * 1.5f + 0.45f);
            const float steps = 5f;
            float poster = Mathf.Round(luminance * steps) / steps;
            float t = Mathf.Clamp01(poster * 0.9f + luminance * 0.1f);
            Color32 dark = new Color32(34, 15, 7, 255);
            Color32 amber = new Color32(168, 86, 24, 255);
            Color32 sand = new Color32(224, 202, 154, 255);
            if (t < 0.5f)
                return LerpColor(dark, amber, t * 2f);
            return LerpColor(amber, sand, (t - 0.5f) * 2f);
        }

        private static Color32 LerpColor(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.Lerp(a.r, b.r, t),
                (byte)Mathf.Lerp(a.g, b.g, t),
                (byte)Mathf.Lerp(a.b, b.b, t),
                255);
        }

        private static void WriteBlur(string folder, string fileName, Color32[] plate, Vector2Int plateSize)
        {
            var pixels = new Color32[BlurWidth * BlurHeight];
            for (int y = 0; y < BlurHeight; y++)
            {
                float v = (y + 0.5f) / BlurHeight;
                for (int x = 0; x < BlurWidth; x++)
                {
                    float u = (x + 0.5f) / BlurWidth;
                    pixels[y * BlurWidth + x] = Grade(Darken(Sample(plate, plateSize, u, v), 0.55f));
                }
            }

            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 3);
            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 2);
            WriteSprite(folder + "/" + fileName + ".png", pixels, BlurWidth, BlurHeight);
        }

        private static void WriteTheaterBlur(string folder, Color32[] composite)
        {
            var pixels = new Color32[BlurWidth * BlurHeight];
            for (int y = 0; y < BlurHeight; y++)
            {
                int sourceY = Mathf.Clamp(Mathf.RoundToInt((y + 0.5f) / BlurHeight * (ZoneHeight - 1)), 0, ZoneHeight - 1);
                for (int x = 0; x < BlurWidth; x++)
                {
                    int sourceX = Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) / BlurWidth * (ZoneWidth - 1)), 0, ZoneWidth - 1);
                    pixels[y * BlurWidth + x] = Darken(composite[sourceY * ZoneWidth + sourceX], 0.62f);
                }
            }

            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 2);
            WriteSprite(folder + "/skirmish.blur.theater.png", pixels, BlurWidth, BlurHeight);
        }

        private static Color32 Sample(Color32[] plate, Vector2Int size, float u, float v)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (size.x - 1)), 0, size.x - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(v * (size.y - 1)), 0, size.y - 1);
            return plate[y * size.x + x];
        }

        private static Color32 Darken(Color32 sample, float gain)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(sample.r * gain), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(sample.g * gain), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(sample.b * gain), 0, 255),
                255);
        }

        private static Color32[] BoxBlur(Color32[] source, int width, int height, int radius)
        {
            var dest = new Color32[source.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    int count = 0;
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int sy = Mathf.Clamp(y + ky, 0, height - 1);
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int sx = Mathf.Clamp(x + kx, 0, width - 1);
                            Color32 sample = source[sy * width + sx];
                            r += sample.r;
                            g += sample.g;
                            b += sample.b;
                            count++;
                        }
                    }

                    dest[y * width + x] = new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
                }
            }

            return dest;
        }

        private static Color ZoneColor(string mapId)
        {
            switch (mapId)
            {
                case "CC": return new Color(0.25f, 0.78f, 0.82f, 1f);
                case "MP": return new Color(0.42f, 0.78f, 0.38f, 1f);
                case "IB": return new Color(0.90f, 0.48f, 0.18f, 1f);
                case "AP": return new Color(0.32f, 0.58f, 0.92f, 1f);
                default: return new Color(0.93f, 0.68f, 0.22f, 1f);
            }
        }

        private static void WriteSprite(string assetPath, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            string absolute = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
            if (SameBytes(absolute, png))
                return;
            File.WriteAllBytes(absolute, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Theatre sprite importer is missing: " + assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static bool SameBytes(string absolute, byte[] next)
        {
            if (next == null || !File.Exists(absolute))
                return false;
            byte[] current = File.ReadAllBytes(absolute);
            if (current.Length != next.Length)
                return false;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != next[i])
                    return false;
            }

            return true;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;
            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
