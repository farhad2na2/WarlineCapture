#if UNITY_EDITOR
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Builds the theatre as five feathered plate windows plus darkened blur plates.
    /// The screen composites one RawImage per zone. It does not author new terrain.
    /// </summary>
    public static class SkirmishLibraryZoneComposer
    {
        public const int ZoneWidth = 960;
        public const int ZoneHeight = 540;
        public const int BlurWidth = 160;
        public const int BlurHeight = 90;
        private const float Feather = 0.055f;
        private const float Border = 0.014f;

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
                    WriteZone(zoneFolder, mapIds[i], regions[i], pixels[i], sizes[i], ZoneColor(mapIds[i]));
                    WriteBlur(blurFolder, "skirmish.blur." + mapIds[i], pixels[i], sizes[i]);
                }

                WriteTheaterBlur(blurFolder, mapIds, regions, pixels, sizes);
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
            if (!texture.LoadImage(bytes))
                throw new InvalidDataException("Theatre zone plate could not be read: " + assetPath);
            return texture;
        }

        private static void WriteZone(string folder, string mapId, Rect region, Color32[] plate, Vector2Int plateSize, Color border)
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
                    if (inset <= 0f || region.width <= 0.001f || region.height <= 0.001f)
                        continue;
                    float alpha = Mathf.Clamp01(inset / Feather);
                    float pu = Mathf.Clamp01((u - region.x) / region.width);
                    float pv = Mathf.Clamp01(1f - (vTop - region.y) / region.height);
                    Color32 sample = Sample(plate, plateSize, pu, pv);
                    if (inset < Border)
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

        private static void WriteBlur(string folder, string fileName, Color32[] plate, Vector2Int plateSize)
        {
            var pixels = new Color32[BlurWidth * BlurHeight];
            for (int y = 0; y < BlurHeight; y++)
            {
                float v = (y + 0.5f) / BlurHeight;
                for (int x = 0; x < BlurWidth; x++)
                {
                    float u = (x + 0.5f) / BlurWidth;
                    Color32 sample = Sample(plate, plateSize, u, v);
                    pixels[y * BlurWidth + x] = Darken(sample, 0.42f);
                }
            }

            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 3);
            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 2);
            WriteSprite(folder + "/" + fileName + ".png", pixels, BlurWidth, BlurHeight);
        }

        private static void WriteTheaterBlur(
            string folder,
            string[] mapIds,
            Rect[] regions,
            Color32[][] plates,
            Vector2Int[] sizes)
        {
            var pixels = new Color32[BlurWidth * BlurHeight];
            for (int y = 0; y < BlurHeight; y++)
            {
                float vTop = 1f - (y + 0.5f) / BlurHeight;
                for (int x = 0; x < BlurWidth; x++)
                {
                    float u = (x + 0.5f) / BlurWidth;
                    int chosen = 0;
                    for (int i = 0; i < regions.Length; i++)
                    {
                        Rect region = regions[i];
                        if (u >= region.x && u <= region.xMax && vTop >= region.y && vTop <= region.yMax)
                            chosen = i;
                    }

                    Rect window = regions[chosen];
                    float pu = window.width > 0.001f ? Mathf.Clamp01((u - window.x) / window.width) : 0.5f;
                    float pv = window.height > 0.001f ? Mathf.Clamp01(1f - (vTop - window.y) / window.height) : 0.5f;
                    pixels[y * BlurWidth + x] = Darken(Sample(plates[chosen], sizes[chosen], pu, pv), 0.38f);
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
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Theatre sprite importer is missing: " + assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
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
