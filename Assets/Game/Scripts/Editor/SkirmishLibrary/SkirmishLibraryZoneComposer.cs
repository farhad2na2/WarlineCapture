#if UNITY_EDITOR
using System.IO;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Cuts one window per district from the painted theatre base. The base file is
    /// read only: this composer does not rewrite it, copy it to a theatre plate, or
    /// bake a darkened theatre blur. Map and Grid fallbacks still blur the Campaign plates.
    /// </summary>
    public static class SkirmishLibraryZoneComposer
    {
        public const int ComposerVersion = 3;
        public const int BlurWidth = 160;
        public const int BlurHeight = 90;
        private static int _rewritten;

        public static void Generate()
        {
            _rewritten = 0;
            string zoneFolder = SkirmishLibraryPreviewAssets.Root + "/Zones";
            string blurFolder = SkirmishLibraryPreviewAssets.Root + "/Blurs";
            EnsureFolder(zoneFolder);
            EnsureFolder(blurFolder);

            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            Rect[] regions = SkirmishBattleLibraryArt.TheaterRegions;
            Texture2D painted = null;
            var plates = new Texture2D[mapIds.Length];
            try
            {
                painted = LoadPng(SkirmishLibraryPreviewAssets.TheatreBasePath, "Theatre base");
                Color32[] theatre = painted.GetPixels32();
                int width = painted.width;
                int height = painted.height;
                byte[] owner = BuildOwner(regions, width, height);
                for (int i = 0; i < mapIds.Length; i++)
                {
                    WriteZoneWindow(zoneFolder, mapIds[i], (byte)i, owner, theatre, width, height);
                    plates[i] = LoadPng(SkirmishLibraryPreviewAssets.PlatePath(mapIds[i]), "Theatre zone plate");
                    WriteBlur(
                        blurFolder,
                        "skirmish.blur." + mapIds[i],
                        plates[i].GetPixels32(),
                        new Vector2Int(plates[i].width, plates[i].height));
                }

                Debug.Log("[SkirmishLibraryZoneComposer] version=" + ComposerVersion + " rewritten=" + _rewritten);
            }
            finally
            {
                if (painted != null)
                    Object.DestroyImmediate(painted);
                for (int i = 0; i < plates.Length; i++)
                {
                    if (plates[i] != null)
                        Object.DestroyImmediate(plates[i]);
                }
            }
        }

        private static Texture2D LoadPng(string assetPath, string label)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolute = Path.Combine(projectRoot ?? string.Empty, assetPath);
            if (!File.Exists(absolute))
                throw new FileNotFoundException(label + " is missing: " + assetPath);
            byte[] bytes = File.ReadAllBytes(absolute);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!UnityEngine.ImageConversion.LoadImage(texture, bytes))
                throw new InvalidDataException(label + " could not be read: " + assetPath);
            return texture;
        }

        private static byte[] BuildOwner(Rect[] regions, int width, int height)
        {
            var owner = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                float vTop = 1f - (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    int best = 0;
                    float bestInset = -1f;
                    for (int i = 0; i < regions.Length; i++)
                    {
                        float inset = RegionInset(regions[i], u, vTop);
                        if (inset > bestInset)
                        {
                            bestInset = inset;
                            best = i;
                        }
                    }

                    owner[y * width + x] = bestInset >= 0f ? (byte)best : (byte)255;
                }
            }

            return owner;
        }

        private static float RegionInset(Rect region, float u, float vTop)
        {
            if (region.width <= 0.001f || region.height <= 0.001f)
                return -1f;
            float left = u - region.x;
            float right = region.xMax - u;
            float top = vTop - region.y;
            float bottom = region.yMax - vTop;
            return Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
        }

        private static void WriteZoneWindow(
            string folder,
            string mapId,
            byte regionIndex,
            byte[] owner,
            Color32[] composite,
            int width,
            int height)
        {
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                if (owner == null || i >= owner.Length || owner[i] != regionIndex)
                    continue;
                Color32 sample = composite[i];
                sample.a = 255;
                pixels[i] = sample;
            }

            WriteSprite(folder + "/skirmish.zone." + mapId + ".png", pixels, width, height);
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
                    pixels[y * BlurWidth + x] = SkirmishLibraryPreviewAssets.Grade(Darken(Sample(plate, plateSize, u, v), 0.55f));
                }
            }

            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 3);
            pixels = BoxBlur(pixels, BlurWidth, BlurHeight, 2);
            WriteSprite(folder + "/" + fileName + ".png", pixels, BlurWidth, BlurHeight);
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

        private static void WriteSprite(string assetPath, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            string absolute = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
            TextureImporter current = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (StampMatches(current) && SameBytes(absolute, png))
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
            importer.userData = Stamp();
            importer.SaveAndReimport();
            _rewritten++;
        }

        private static string Stamp()
        {
            return "skirmish-zone-composer=" + ComposerVersion;
        }

        private static bool StampMatches(TextureImporter importer)
        {
            return importer != null && string.Equals(importer.userData, Stamp(), System.StringComparison.Ordinal);
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
