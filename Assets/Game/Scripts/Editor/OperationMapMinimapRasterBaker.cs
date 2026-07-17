using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class OperationMapMinimapRasterBaker
    {
        public const int Resolution = 512;
        public const string OutputPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01/MinimapRaster.png";

        private const int AlgorithmVersion = 1;
        private static readonly Color32 BackgroundA = new(150, 136, 116, 255);
        private static readonly Color32 BackgroundB = new(126, 116, 101, 255);
        private static readonly Color32 GridColor = new(97, 109, 97, 255);
        private static readonly Color32 RoadColor = new(83, 75, 61, 255);
        private static readonly Color32 DirtRoadColor = new(112, 91, 62, 255);
        private static readonly Color32 SidewalkColor = new(150, 145, 130, 255);
        private static readonly Color32 BridgeColor = new(82, 103, 116, 255);
        private static readonly Color32 RampColor = new(132, 103, 58, 255);
        private static readonly Color32 PlazaColor = new(119, 112, 99, 255);
        private static readonly Color32 BlockedColor = new(86, 78, 70, 255);

        [MenuItem("Game/Operation Maps/Bake Selected Map Minimap Raster")]
        public static void Run()
        {
            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            MapSurfaceDataAsset surface = AssetDatabase.LoadAssetAtPath<MapSurfaceDataAsset>(
                OperationMapAddressablesLayoutBuilder.MapSurfacePath);
            if (definition == null || surface == null)
                throw new InvalidOperationException("Operation-map definition and map surface are required.");
            if (!definition.TryValidateMetadata(out string metadataError))
                throw new InvalidOperationException(metadataError);
            if (math.abs(definition.Minimap.OrientationDegrees) > 0.001f)
                throw new InvalidOperationException("Minimap raster baking currently requires zero-degree orientation.");
            if (!surface.TryCreateRuntimeBlobAsset(
                    Allocator.Temp,
                    out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
            {
                throw new InvalidOperationException("Map surface payload could not create a runtime blob.");
            }

            byte[] pngBytes;
            try
            {
                pngBytes = EncodePng(definition, surface, ref surfaceBlob.Value);
            }
            finally
            {
                surfaceBlob.Dispose();
            }

            string absolutePath = Path.GetFullPath(OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? throw new InvalidOperationException());
            bool wrote = !File.Exists(absolutePath) || !BytesEqual(File.ReadAllBytes(absolutePath), pngBytes);
            if (wrote)
                File.WriteAllBytes(absolutePath, pngBytes);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("Generated minimap raster did not import as a texture.");

            string expectedUserData = BuildImporterUserData(definition, surface);
            bool importerChanged = ConfigureImporter(importer, expectedUserData);
            if (importerChanged)
                importer.SaveAndReimport();

            Debug.Log(
                $"[OperationMapMinimapRasterBaker] map={definition.OperationMapId} " +
                $"bytes={pngBytes.Length} wrote={(wrote ? 1 : 0)} importerChanged={(importerChanged ? 1 : 0)}");
        }

        public static string BuildImporterUserData(
            OperationMapDefinition definition,
            MapSurfaceDataAsset surface)
        {
            if (definition == null || surface == null)
                return string.Empty;

            return $"operation-map-minimap-raster|{AlgorithmVersion}|{definition.OperationMapId}|" +
                   $"{definition.GeneratedMetadataHash}|{surface.ComputeRuntimeBlobHash()}";
        }

        private static byte[] EncodePng(
            OperationMapDefinition definition,
            MapSurfaceDataAsset surface,
            ref MapSurfaceBlob blob)
        {
            Color32[] pixels = new Color32[Resolution * Resolution];
            DrawBackground(pixels);
            DrawSurfaceFeatures(definition, surface, ref blob, pixels);

            Texture2D texture = new(Resolution, Resolution, TextureFormat.RGBA32, false, false)
            {
                name = "OperationMapMinimapRasterBake"
            };
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return ImageConversion.EncodeToPNG(texture);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void DrawBackground(Color32[] pixels)
        {
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    float vertical = y / (float)(Resolution - 1);
                    int ripple = ((x * 17 + y * 31) & 15) - 8;
                    int ridge = Mathf.RoundToInt(Mathf.Sin((x * 0.025f) + (y * 0.013f)) * 7f);
                    pixels[x + y * Resolution] = LerpColor(BackgroundA, BackgroundB, vertical, ripple + ridge);
                }
            }

            const int gridStep = 32;
            for (int coordinate = 0; coordinate < Resolution; coordinate += gridStep)
            {
                for (int offset = 0; offset < Resolution; offset++)
                {
                    BlendPixel(pixels, coordinate, offset, GridColor, 0.28f);
                    BlendPixel(pixels, offset, coordinate, GridColor, 0.28f);
                }
            }
        }

        private static void DrawSurfaceFeatures(
            OperationMapDefinition definition,
            MapSurfaceDataAsset surface,
            ref MapSurfaceBlob blob,
            Color32[] pixels)
        {
            Vector3 projectionOrigin = definition.Minimap.ProjectionOrigin;
            Vector2 projectionSize = definition.Minimap.ProjectionSize;
            for (int pixelY = 0; pixelY < Resolution; pixelY++)
            {
                float worldMinZ = projectionOrigin.z + projectionSize.y * (pixelY / (float)Resolution);
                float worldMaxZ = projectionOrigin.z + projectionSize.y * ((pixelY + 1f) / Resolution);
                int cellMinY = Mathf.Clamp(
                    Mathf.FloorToInt((worldMinZ - surface.GridOrigin.z) / surface.CellSize),
                    0,
                    surface.Dimensions.y - 1);
                int cellMaxY = Mathf.Clamp(
                    Mathf.CeilToInt((worldMaxZ - surface.GridOrigin.z) / surface.CellSize) - 1,
                    cellMinY,
                    surface.Dimensions.y - 1);

                for (int pixelX = 0; pixelX < Resolution; pixelX++)
                {
                    float worldMinX = projectionOrigin.x + projectionSize.x * (pixelX / (float)Resolution);
                    float worldMaxX = projectionOrigin.x + projectionSize.x * ((pixelX + 1f) / Resolution);
                    int cellMinX = Mathf.Clamp(
                        Mathf.FloorToInt((worldMinX - surface.GridOrigin.x) / surface.CellSize),
                        0,
                        surface.Dimensions.x - 1);
                    int cellMaxX = Mathf.Clamp(
                        Mathf.CeilToInt((worldMaxX - surface.GridOrigin.x) / surface.CellSize) - 1,
                        cellMinX,
                        surface.Dimensions.x - 1);

                    int bestPriority = 0;
                    Color32 bestColor = default;
                    for (int cellY = cellMinY; cellY <= cellMaxY; cellY++)
                    {
                        for (int cellX = cellMinX; cellX <= cellMaxX; cellX++)
                        {
                            if (!MapSurfaceBlobAccess.TryGetSurfaceRange(
                                    ref blob,
                                    new int2(cellX, cellY),
                                    out MapSurfaceCellSurfaceRange range))
                                continue;

                            for (int surfaceIndex = 0; surfaceIndex < range.SurfaceCount; surfaceIndex++)
                            {
                                if (!MapSurfaceBlobAccess.TryGetSurface(
                                        ref blob,
                                        range,
                                        surfaceIndex,
                                        out MapSurfaceSample sample) ||
                                    !TryResolveFeature(sample, out Color32 color, out int priority) ||
                                    priority <= bestPriority)
                                {
                                    continue;
                                }

                                bestPriority = priority;
                                bestColor = color;
                            }
                        }
                    }

                    if (bestPriority > 0)
                        BlendPixel(pixels, pixelX, pixelY, bestColor, 0.82f);
                }
            }
        }

        private static bool TryResolveFeature(
            MapSurfaceSample sample,
            out Color32 color,
            out int priority)
        {
            if (sample.SurfaceType == MapSurfaceType.Blocked)
                return SetFeature(BlockedColor, 7, out color, out priority);
            if ((sample.Flags & MapSurfaceFlags.Bridge) != 0 ||
                sample.SurfaceType == MapSurfaceType.BridgeDeck)
                return SetFeature(BridgeColor, 6, out color, out priority);
            if ((sample.Flags & MapSurfaceFlags.Ramp) != 0 || sample.SurfaceType == MapSurfaceType.Ramp)
                return SetFeature(RampColor, 5, out color, out priority);
            if ((sample.Flags & MapSurfaceFlags.Highway) != 0 || sample.SurfaceType == MapSurfaceType.Highway)
                return SetFeature(SidewalkColor, 4, out color, out priority);
            if (sample.SurfaceType == MapSurfaceType.DirtRoad)
                return SetFeature(DirtRoadColor, 3, out color, out priority);
            if (sample.SurfaceType == MapSurfaceType.Road || (sample.Flags & MapSurfaceFlags.Road) != 0)
                return SetFeature(RoadColor, 3, out color, out priority);
            if (sample.SurfaceType == MapSurfaceType.Plaza)
                return SetFeature(PlazaColor, 2, out color, out priority);

            color = default;
            priority = 0;
            return false;
        }

        private static bool SetFeature(
            Color32 featureColor,
            int featurePriority,
            out Color32 color,
            out int priority)
        {
            color = featureColor;
            priority = featurePriority;
            return true;
        }

        private static Color32 LerpColor(Color32 a, Color32 b, float t, int offset)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)) + offset, 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)) + offset, 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)) + offset, 0, 255),
                255);
        }

        private static void BlendPixel(
            Color32[] pixels,
            int x,
            int y,
            Color32 color,
            float amount)
        {
            int index = x + y * Resolution;
            Color32 current = pixels[index];
            pixels[index] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(current.r, color.r, amount)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(current.g, color.g, amount)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(current.b, color.b, amount)),
                255);
        }

        private static bool ConfigureImporter(TextureImporter importer, string expectedUserData)
        {
            bool changed = importer.textureType != TextureImporterType.Default ||
                           !importer.sRGBTexture ||
                           importer.alphaSource != TextureImporterAlphaSource.None ||
                           importer.isReadable ||
                           importer.mipmapEnabled ||
                           importer.npotScale != TextureImporterNPOTScale.None ||
                           importer.wrapMode != TextureWrapMode.Clamp ||
                           importer.filterMode != FilterMode.Bilinear ||
                           importer.maxTextureSize != Resolution ||
                           importer.textureCompression != TextureImporterCompression.CompressedHQ ||
                           !string.Equals(importer.userData, expectedUserData, StringComparison.Ordinal);

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.isReadable = false;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = Resolution;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.userData = expectedUserData;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            changed |= !android.overridden ||
                       android.maxTextureSize != Resolution ||
                       android.format != TextureImporterFormat.ASTC_6x6 ||
                       android.compressionQuality != 100;
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = Resolution;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.compressionQuality = 100;
            android.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
            importer.SetPlatformTextureSettings(android);
            return changed;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }
    }
}
