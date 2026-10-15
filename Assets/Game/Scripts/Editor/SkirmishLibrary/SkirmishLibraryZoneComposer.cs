#if UNITY_EDITOR
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Paints one warm theatre plate from the existing Campaign plates, then writes
    /// feathered windows of that same image. It does not author new terrain.
    /// </summary>
    public static class SkirmishLibraryZoneComposer
    {
        public const int ZoneWidth = 960;
        public const int ZoneHeight = 540;
        public const int BlurWidth = 160;
        public const int BlurHeight = 90;
        private const float Feather = 0.085f;
        private const float Border = 0.010f;

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

                Color32[] composite = Compose(regions, pixels, sizes);
                WriteSprite(zoneFolder + "/skirmish.zone.theater.png", composite, ZoneWidth, ZoneHeight);
                for (int i = 0; i < mapIds.Length; i++)
                {
                    WriteZoneWindow(zoneFolder, mapIds[i], regions[i], composite, ZoneColor(mapIds[i]));
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

        private static Color32[] Compose(Rect[] regions, Color32[][] plates, Vector2Int[] sizes)
        {
            var pixels = new Color32[ZoneWidth * ZoneHeight];
            for (int y = 0; y < ZoneHeight; y++)
            {
                float vTop = 1f - (y + 0.5f) / ZoneHeight;
                for (int x = 0; x < ZoneWidth; x++)
                {
                    float u = (x + 0.5f) / ZoneWidth;
                    float red = 0f;
                    float green = 0f;
                    float blue = 0f;
                    float weight = 0f;
                    for (int i = 0; i < regions.Length; i++)
                    {
                        float influence = Influence(regions[i], u, vTop);
                        if (influence <= 0f)
                            continue;
                        float pu = regions[i].width > 0.001f ? Mathf.Clamp01((u - regions[i].x) / regions[i].width) : 0.5f;
                        float pv = regions[i].height > 0.001f ? Mathf.Clamp01(1f - (vTop - regions[i].y) / regions[i].height) : 0.5f;
                        Color32 sample = Sample(plates[i], sizes[i], pu, pv);
                        red += sample.r * influence;
                        green += sample.g * influence;
                        blue += sample.b * influence;
                        weight += influence;
                    }

                    if (weight <= 0.001f)
                    {
                        pixels[y * ZoneWidth + x] = new Color32(92, 48, 18, 255);
                        continue;
                    }

                    var mixed = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(red / weight), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(green / weight), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(blue / weight), 0, 255),
                        255);
                    pixels[y * ZoneWidth + x] = Grade(mixed);
                }
            }

            for (int i = 0; i < regions.Length; i++)
                Stroke(pixels, regions[i], ZoneColor(IndexMap(i)));
            return pixels;
        }

        private static string IndexMap(int index)
        {
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            return index >= 0 && index < mapIds.Length ? mapIds[index] : "DB";
        }

        private static float Influence(Rect region, float u, float vTop)
        {
            float left = u - region.x;
            float right = region.xMax - u;
            float top = vTop - region.y;
            float bottom = region.yMax - vTop;
            float inset = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
            if (region.width <= 0.001f || region.height <= 0.001f)
                return 0f;
            if (inset >= Feather)
                return 1f;
            if (inset <= -Feather)
                return 0f;
            return Mathf.Clamp01((inset + Feather) / (Feather * 2f));
        }

        private static void Stroke(Color32[] pixels, Rect region, Color color)
        {
            var ink = (Color32)color;
            for (int y = 0; y < ZoneHeight; y++)
            {
                float vTop = 1f - (y + 0.5f) / ZoneHeight;
                for (int x = 0; x < ZoneWidth; x++)
                {
                    float u = (x + 0.5f) / ZoneWidth;
                    float left = u - region.x;
                    float right = region.xMax - u;
                    float top = vTop - region.y;
                    float bottom = region.yMax - vTop;
                    float inset = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
                    if (inset < 0f || inset > Border)
                        continue;
                    int index = y * ZoneWidth + x;
                    float edge = Mathf.Clamp01(inset / Border);
                    Color32 under = pixels[index];
                    pixels[index] = new Color32(
                        (byte)Mathf.Lerp(ink.r, under.r, edge),
                        (byte)Mathf.Lerp(ink.g, under.g, edge),
                        (byte)Mathf.Lerp(ink.b, under.b, edge),
                        255);
                }
            }
        }

        private static void WriteZoneWindow(string folder, string mapId, Rect region, Color32[] composite, Color border)
        {
            var pixels = new Color32[ZoneWidth * ZoneHeight];
            var border32 = (Color32)border;
            for (int y = 0; y < ZoneHeight; y++)
            {
                float vTop = 1f - (y + 0.5f) / ZoneHeight;
                for (int x = 0; x < ZoneWidth; x++)
                {
                    float u = (x + 0.5f) / ZoneWidth;
                    float left = u - region.x;
                    float right = region.xMax - u;
                    float top = vTop - region.y;
                    float bottom = region.yMax - vTop;
                    float inset = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
                    if (inset <= -Feather || region.width <= 0.001f || region.height <= 0.001f)
                        continue;
                    float alpha = Mathf.Clamp01((inset + Feather * 0.35f) / Feather);
                    Color32 sample = composite[y * ZoneWidth + x];
                    if (inset >= 0f && inset < Border)
                    {
                        float edge = Mathf.Clamp01(inset / Border);
                        sample.r = (byte)Mathf.Lerp(border32.r, sample.r, edge);
                        sample.g = (byte)Mathf.Lerp(border32.g, sample.g, edge);
                        sample.b = (byte)Mathf.Lerp(border32.b, sample.b, edge);
                    }

                    sample.a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * ZoneWidth + x] = sample;
                }
            }

            WriteSprite(folder + "/skirmish.zone." + mapId + ".png", pixels, ZoneWidth, ZoneHeight);
        }

        private static Color32 Grade(Color32 sample)
        {
            float luminance = (sample.r * 0.30f + sample.g * 0.52f + sample.b * 0.18f) / 255f;
            float red = Mathf.Clamp01(luminance * 1.05f + 0.28f);
            float green = Mathf.Clamp01(luminance * 0.62f + 0.12f);
            float blue = Mathf.Clamp01(luminance * 0.22f + 0.03f);
            return new Color32(
                (byte)Mathf.RoundToInt(red * 255f),
                (byte)Mathf.RoundToInt(green * 255f),
                (byte)Mathf.RoundToInt(blue * 255f),
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
