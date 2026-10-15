#if UNITY_EDITOR
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Paints one warm theatre plate from the existing Campaign plates. Each pixel
    /// keeps a single plate, then five flat amber steps, a thin region border and
    /// a light vignette. It does not author new terrain.
    /// </summary>
    public static class SkirmishLibraryZoneComposer
    {
        public const int ZoneWidth = 960;
        public const int ZoneHeight = 540;
        public const int BlurWidth = 160;
        public const int BlurHeight = 90;
        private const float Warp = 0.065f;
        private const int BorderBand = 10;
        private const int DarkStroke = 3;

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
                }
            }

            owner = SmoothOwner(owner);
            owner = SmoothOwner(owner);
            for (int y = 0; y < ZoneHeight; y++)
            {
                float vTop = 1f - (y + 0.5f) / ZoneHeight;
                for (int x = 0; x < ZoneWidth; x++)
                {
                    int index = y * ZoneWidth + x;
                    int best = owner[index];
                    if (best < 0 || best >= regions.Length)
                        best = 0;
                    float u = (x + 0.5f) / ZoneWidth;
                    Rect region = regions[best];
                    float pu = region.width > 0.001f ? Mathf.Clamp01((u - region.x) / region.width) : 0.5f;
                    float pv = region.height > 0.001f ? Mathf.Clamp01(1f - (vTop - region.y) / region.height) : 0.5f;
                    pixels[index] = Sample(plates[best], sizes[best], pu, pv);
                }
            }

            pixels = FlattenPaint(pixels, ZoneWidth, ZoneHeight);
            Vignette(pixels);
            PaintThickBorders(pixels, owner);
            return pixels;
        }

        private static byte[] SmoothOwner(byte[] owner)
        {
            var next = new byte[owner.Length];
            var tally = new int[8];
            for (int y = 0; y < ZoneHeight; y++)
            {
                for (int x = 0; x < ZoneWidth; x++)
                {
                    for (int i = 0; i < tally.Length; i++)
                        tally[i] = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        int sy = y + ky;
                        if (sy < 0 || sy >= ZoneHeight)
                            continue;
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int sx = x + kx;
                            if (sx < 0 || sx >= ZoneWidth)
                                continue;
                            int id = owner[sy * ZoneWidth + sx];
                            if (id >= 0 && id < tally.Length)
                                tally[id]++;
                        }
                    }

                    int best = owner[y * ZoneWidth + x];
                    int bestCount = 0;
                    for (int i = 0; i < tally.Length; i++)
                    {
                        if (tally[i] > bestCount)
                        {
                            bestCount = tally[i];
                            best = i;
                        }
                    }

                    next[y * ZoneWidth + x] = (byte)best;
                }
            }

            return next;
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
            return BoundaryNoise(originU, originV, 5.5f) * Warp;
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
                    float gain = 1f - 0.12f * shade;
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

        private static Color32[] FlattenPaint(Color32[] source, int width, int height)
        {
            int smallWidth = Mathf.Max(1, width / 4);
            int smallHeight = Mathf.Max(1, height / 4);
            Color32[] small = DownscaleBox(source, width, height, smallWidth, smallHeight);
            for (int i = 0; i < small.Length; i++)
                small[i] = Grade(small[i]);
            Color32[] large = UpscaleBilinear(small, smallWidth, smallHeight, width, height);
            return BoxBlur(large, width, height, 1);
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
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) * smallHeight / height - 0.5f;
                int y0 = Mathf.Clamp(Mathf.FloorToInt(v), 0, smallHeight - 1);
                int y1 = Mathf.Min(y0 + 1, smallHeight - 1);
                float fy = Mathf.Clamp01(v - y0);
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) * smallWidth / width - 0.5f;
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, smallWidth - 1);
                    int x1 = Mathf.Min(x0 + 1, smallWidth - 1);
                    float fx = Mathf.Clamp01(u - x0);
                    Color32 c00 = source[y0 * smallWidth + x0];
                    Color32 c10 = source[y0 * smallWidth + x1];
                    Color32 c01 = source[y1 * smallWidth + x0];
                    Color32 c11 = source[y1 * smallWidth + x1];
                    dest[y * width + x] = new Color32(
                        LerpChannel(LerpChannel(c00.r, c10.r, fx), LerpChannel(c01.r, c11.r, fx), fy),
                        LerpChannel(LerpChannel(c00.g, c10.g, fx), LerpChannel(c01.g, c11.g, fx), fy),
                        LerpChannel(LerpChannel(c00.b, c10.b, fx), LerpChannel(c01.b, c11.b, fx), fy),
                        255);
                }
            }

            return dest;
        }

        private static byte LerpChannel(byte from, byte to, float t)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(from + (to - from) * t), 0, 255);
        }

        private static Color32[] Median(Color32[] source, int width, int height, int radius)
        {
            int window = (radius * 2 + 1) * (radius * 2 + 1);
            var reds = new byte[window];
            var greens = new byte[window];
            var blues = new byte[window];
            var dest = new Color32[source.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int count = 0;
                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        int sy = Mathf.Clamp(y + ky, 0, height - 1);
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int sx = Mathf.Clamp(x + kx, 0, width - 1);
                            Color32 sample = source[sy * width + sx];
                            reds[count] = sample.r;
                            greens[count] = sample.g;
                            blues[count] = sample.b;
                            count++;
                        }
                    }

                    dest[y * width + x] = new Color32(MedianChannel(reds, count), MedianChannel(greens, count), MedianChannel(blues, count), 255);
                }
            }

            return dest;
        }

        private static byte MedianChannel(byte[] values, int count)
        {
            for (int i = 1; i < count; i++)
            {
                byte value = values[i];
                int j = i - 1;
                while (j >= 0 && values[j] > value)
                {
                    values[j + 1] = values[j];
                    j--;
                }

                values[j + 1] = value;
            }

            return values[count / 2];
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
            int step = luminance < 0.38f ? 0 : luminance < 0.52f ? 1 : luminance < 0.64f ? 2 : luminance < 0.76f ? 3 : 4;
            return PosterStep(step);
        }

        private static Color32 PosterStep(int step)
        {
            switch (step)
            {
                case 0: return new Color32(90, 28, 10, 255);
                case 1: return new Color32(190, 90, 22, 255);
                case 2: return new Color32(220, 132, 32, 255);
                case 3: return new Color32(236, 168, 64, 255);
                default: return new Color32(252, 214, 140, 255);
            }
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
