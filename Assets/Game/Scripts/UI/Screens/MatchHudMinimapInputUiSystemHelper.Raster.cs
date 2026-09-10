using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudMinimapInputUiSystemHelper
    {
        private void CaptureMap(MatchHudMinimapProjectionGrid grid, int cullingMask)
        {
            MatchHudMinimapProjectionUiSystemHelper.ConfigureCaptureCamera(_captureCamera, grid, cullingMask);

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = _captureCamera.targetTexture;
            bool previousFog = RenderSettings.fog;
            try
            {
                RenderSettings.fog = false;
                _captureCamera.targetTexture = _renderTexture;
                _captureCamera.Render();
            }
            finally
            {
                RenderSettings.fog = previousFog;
                RenderTexture.active = previousActive;
                _captureCamera.targetTexture = previousTarget;
            }
        }

        private bool IsFlatCapture()
        {
            ReadRenderTextureInto(_readbackTexture);

            Color32[] pixels = _readbackTexture.GetPixels32();
            if (pixels == null || pixels.Length == 0)
                return true;

            int step = Mathf.Max(1, pixels.Length / 4096);
            int min = 255;
            int max = 0;
            double sum = 0d;
            double sumSquares = 0d;
            int samples = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                int luminance = (pixel.r + pixel.g + pixel.b) / 3;
                min = Mathf.Min(min, luminance);
                max = Mathf.Max(max, luminance);
                sum += luminance;
                sumSquares += luminance * luminance;
                samples++;
            }

            if (samples == 0)
                return true;

            float average = (float)(sum / samples);
            float variance = Mathf.Max(0f, (float)(sumSquares / samples) - average * average);
            float stdDev = Mathf.Sqrt(variance);
            int luminanceRange = max - min;
            return average < 48f ||
                   average > 247f ||
                   (luminanceRange < 3 && stdDev < 1f);
        }

        private void ReadRenderTextureInto(Texture2D texture)
        {
            if (texture == null || _renderTexture == null)
                return;

            RenderTexture previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = _renderTexture;
                texture.ReadPixels(new Rect(0, 0, CaptureResolution, CaptureResolution), 0, 0, false);
                texture.Apply(false);
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private void DrawRasterMap(
            MatchHudMinimapProjectionGrid requestedGrid,
            out MatchHudMinimapProjectionGrid renderedGrid,
            bool allowExpandedFallback)
        {
            renderedGrid = requestedGrid;
            if (_rasterPixels == null || _rasterPixels.Length != CaptureResolution * CaptureResolution)
                _rasterPixels = new Color32[CaptureResolution * CaptureResolution];

            int candidateCount = FillRasterProjectionCandidates(requestedGrid, allowExpandedFallback);
            for (int i = 0; i < candidateCount; i++)
            {
                MatchHudMinimapProjectionGrid candidate = _rasterProjectionCandidates[i];
                DrawRasterBase(_rasterPixels);
                int featureCount = DrawRasterSurfaceFeatures(candidate, _rasterPixels);
                featureCount += DrawRasterRoads(candidate, _rasterPixels);
                renderedGrid = candidate;
                if (featureCount >= MinRasterFeatureCount || i == candidateCount - 1)
                    break;
            }

            _rasterTexture.SetPixels32(_rasterPixels);
            _rasterTexture.Apply(false, false);
        }

        private int FillRasterProjectionCandidates(
            MatchHudMinimapProjectionGrid requestedGrid,
            bool allowExpandedFallback)
        {
            if (!allowExpandedFallback)
            {
                _rasterProjectionCandidates[0] = requestedGrid;
                return 1;
            }

            MatchHudMinimapProjectionGrid fullGrid = TryGetGrid(out MatchHudMinimapGridModel grid)
                ? MatchHudMinimapProjectionGrid.FromGridModel(grid)
                : requestedGrid;
            Vector3 center = requestedGrid.Origin + new Vector3(requestedGrid.Width * 0.5f, 0f, requestedGrid.Height * 0.5f);
            float aspect = requestedGrid.Width / Mathf.Max(0.001f, requestedGrid.Height);
            _rasterProjectionCandidates[0] = requestedGrid;
            _rasterProjectionCandidates[1] = CreateExpandedRasterGrid(center, requestedGrid.Width * 2f, requestedGrid.Height * 2f, aspect, fullGrid);
            _rasterProjectionCandidates[2] = CreateExpandedRasterGrid(center, requestedGrid.Width * 4f, requestedGrid.Height * 4f, aspect, fullGrid);
            _rasterProjectionCandidates[3] = fullGrid;
            return 4;
        }

        private static MatchHudMinimapProjectionGrid CreateExpandedRasterGrid(
            Vector3 center,
            float width,
            float height,
            float aspect,
            MatchHudMinimapProjectionGrid fullGrid)
        {
            height = Mathf.Min(Mathf.Max(1f, height), fullGrid.Height);
            width = Mathf.Min(Mathf.Max(height * aspect, width), fullGrid.Width);
            return new MatchHudMinimapProjectionGrid(
                new Vector3(center.x - width * 0.5f, fullGrid.Origin.y, center.z - height * 0.5f),
                width,
                height);
        }

        private void DrawRasterBase(Color32[] pixels)
        {
            Color32[] basePixels = GetOrCreateRasterBasePixels();
            Array.Copy(basePixels, pixels, basePixels.Length);
        }

        private Color32[] GetOrCreateRasterBasePixels()
        {
            if (_rasterBasePixels != null && _rasterBasePixels.Length == CaptureResolution * CaptureResolution)
                return _rasterBasePixels;

            Color32[] pixels = new Color32[CaptureResolution * CaptureResolution];
            for (int y = 0; y < CaptureResolution; y++)
            {
                for (int x = 0; x < CaptureResolution; x++)
                {
                    float vertical = y / (float)(CaptureResolution - 1);
                    int ripple = ((x * 17 + y * 31) & 15) - 8;
                    int ridge = Mathf.RoundToInt(Mathf.Sin((x * 0.025f) + (y * 0.013f)) * 7f);
                    pixels[x + y * CaptureResolution] = LerpColor(RasterBackgroundA, RasterBackgroundB, vertical, ripple + ridge);
                }
            }

            DrawRasterGrid(pixels);
            _rasterBasePixels = pixels;
            return _rasterBasePixels;
        }

        private static Color32 LerpColor(Color32 a, Color32 b, float t, int offset)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)) + offset, 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)) + offset, 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)) + offset, 0, 255),
                255);
        }

        private static void DrawRasterGrid(Color32[] pixels)
        {
            const int step = 32;
            for (int x = 0; x < CaptureResolution; x += step)
                for (int y = 0; y < CaptureResolution; y++)
                    BlendRasterPixel(pixels, x, y, RasterGrid, 0.28f);

            for (int y = 0; y < CaptureResolution; y += step)
                for (int x = 0; x < CaptureResolution; x++)
                    BlendRasterPixel(pixels, x, y, RasterGrid, 0.28f);
        }

        private int DrawRasterRoads(MatchHudMinimapProjectionGrid projectionGrid, Color32[] pixels)
        {
            _roadScratch.Clear();
            _minimapDataSource?.GetRoadCells(ToAreaModel(projectionGrid), _roadScratch);
            if (_roadScratch.Count == 0)
                return 0;

            int featureCount = 0;
            for (int i = 0; i < _roadScratch.Count; i++)
            {
                MatchHudMinimapRoadCellModel road = _roadScratch[i];
                if (!MatchHudMinimapProjectionUiSystemHelper.TryWorldToNormalized(projectionGrid, road.WorldPosition, out Vector2 normalized) ||
                    normalized.x < 0f ||
                    normalized.x > 1f ||
                    normalized.y < 0f ||
                    normalized.y > 1f)
                {
                    continue;
                }

                int pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (CaptureResolution - 1)), 0, CaptureResolution - 1);
                int pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (CaptureResolution - 1)), 0, CaptureResolution - 1);
                int roadRadius = Mathf.Clamp(Mathf.RoundToInt((CaptureResolution / Mathf.Max(1f, projectionGrid.Height)) * road.CellSize * 1.3f), 1, 4);
                DrawRasterDot(pixels, pixelX, pixelY, ResolveRoadRasterColor(road.Kind), roadRadius);
                featureCount++;
            }

            return featureCount;
        }

        private int DrawRasterSurfaceFeatures(MatchHudMinimapProjectionGrid projectionGrid, Color32[] pixels)
        {
            _surfaceScratch.Clear();
            _minimapDataSource?.GetSurfaceFeatures(ToAreaModel(projectionGrid), _surfaceScratch);
            if (_surfaceScratch.Count == 0)
                return 0;

            int featureCount = 0;
            for (int i = 0; i < _surfaceScratch.Count; i++)
            {
                MatchHudMinimapSurfaceFeatureModel feature = _surfaceScratch[i];
                Color32 color = ResolveSurfaceRasterColor(feature.Kind);
                if (feature.FillArea)
                {
                    DrawRasterAreaFeature(projectionGrid, feature, color, pixels);
                    featureCount++;
                    continue;
                }

                if (!MatchHudMinimapProjectionUiSystemHelper.TryWorldToNormalized(projectionGrid, feature.Center, out Vector2 normalized) ||
                    normalized.x < 0f ||
                    normalized.x > 1f ||
                    normalized.y < 0f ||
                    normalized.y > 1f)
                {
                    continue;
                }

                int pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (CaptureResolution - 1)), 0, CaptureResolution - 1);
                int pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (CaptureResolution - 1)), 0, CaptureResolution - 1);
                int radius = Mathf.Clamp(Mathf.RoundToInt((CaptureResolution / Mathf.Max(1f, projectionGrid.Height)) * feature.CellSize * 1.2f), 1, 3);
                DrawRasterDot(pixels, pixelX, pixelY, color, radius);
                featureCount++;
            }

            return featureCount;
        }

        private static MatchHudMinimapAreaModel ToAreaModel(MatchHudMinimapProjectionGrid projectionGrid)
        {
            return new MatchHudMinimapAreaModel(projectionGrid.Origin, projectionGrid.Width, projectionGrid.Height);
        }

        private static void DrawRasterAreaFeature(
            MatchHudMinimapProjectionGrid projectionGrid,
            MatchHudMinimapSurfaceFeatureModel feature,
            Color32 color,
            Color32[] pixels)
        {
            float minX = feature.Center.x - feature.HalfExtents.x;
            float maxX = feature.Center.x + feature.HalfExtents.x;
            float minZ = feature.Center.z - feature.HalfExtents.y;
            float maxZ = feature.Center.z + feature.HalfExtents.y;
            Rect projectionRect = new(projectionGrid.Origin.x, projectionGrid.Origin.z, projectionGrid.Width, projectionGrid.Height);
            Rect featureRect = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
            if (!projectionRect.Overlaps(featureRect))
                return;

            int pixelMinX = Mathf.Clamp(Mathf.FloorToInt(((minX - projectionGrid.Origin.x) / projectionGrid.Width) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int pixelMaxX = Mathf.Clamp(Mathf.CeilToInt(((maxX - projectionGrid.Origin.x) / projectionGrid.Width) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int pixelMinY = Mathf.Clamp(Mathf.FloorToInt(((minZ - projectionGrid.Origin.z) / projectionGrid.Height) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            int pixelMaxY = Mathf.Clamp(Mathf.CeilToInt(((maxZ - projectionGrid.Origin.z) / projectionGrid.Height) * (CaptureResolution - 1)), 0, CaptureResolution - 1);
            for (int y = pixelMinY; y <= pixelMaxY; y++)
            {
                for (int x = pixelMinX; x <= pixelMaxX; x++)
                    BlendRasterPixel(pixels, x, y, color, 0.58f);
            }
        }

        private static Color32 ResolveRoadRasterColor(MatchHudMinimapRoadKind kind)
        {
            return kind switch
            {
                MatchHudMinimapRoadKind.DirtRoad => RasterDirtRoad,
                MatchHudMinimapRoadKind.Sidewalk => RasterSidewalk,
                _ => RasterRoad
            };
        }

        private static Color32 ResolveSurfaceRasterColor(MatchHudMinimapSurfaceFeatureKind kind)
        {
            return kind switch
            {
                MatchHudMinimapSurfaceFeatureKind.Blocked => RasterBlocked,
                MatchHudMinimapSurfaceFeatureKind.Bridge => RasterBridge,
                MatchHudMinimapSurfaceFeatureKind.Ramp => RasterRamp,
                MatchHudMinimapSurfaceFeatureKind.Highway => RasterSidewalk,
                MatchHudMinimapSurfaceFeatureKind.DirtRoad => RasterDirtRoad,
                MatchHudMinimapSurfaceFeatureKind.Plaza => RasterPlaza,
                _ => RasterRoad
            };
        }

        private static void DrawRasterDot(Color32[] pixels, int centerX, int centerY, Color32 color, int radius)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                        BlendRasterPixel(pixels, centerX + x, centerY + y, color, 0.9f);
                }
            }
        }

        private static void BlendRasterPixel(Color32[] pixels, int x, int y, Color32 color, float amount)
        {
            if ((uint)x >= CaptureResolution || (uint)y >= CaptureResolution)
                return;

            int index = x + y * CaptureResolution;
            Color32 current = pixels[index];
            pixels[index] = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current.r, color.r, amount)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current.g, color.g, amount)), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(current.b, color.b, amount)), 0, 255),
                255);
        }

    }
}
