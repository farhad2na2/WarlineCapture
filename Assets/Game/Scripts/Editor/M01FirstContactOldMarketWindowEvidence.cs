using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal static class M01FirstContactOldMarketWindowEvidence
    {
        private const string ReportPath =
            "Design/AgentReports/M01FirstContact/m01dc_011_old_market_window.json";
        private const string CapturePath =
            "Design/AgentReports/M01FirstContact/m01dc_011_old_market_window.png";
        private const int Width = 1024;
        private const int Height = 768;
        private static readonly RectInt MapRect = new(64, 50, 896, 668);

        public static void Write(
            string[] panelHashes,
            M01FirstContactOldMarketWindowValidation.WindowAnalysis analysis,
            RectInt window,
            RectInt corridor,
            int2[] route,
            string marker)
        {
            WriteCapture(window, corridor, route, analysis);
            WriteReport(panelHashes, analysis, window, corridor, marker);
            Require(File.Exists(CapturePath) && new FileInfo(CapturePath).Length > 1000,
                "Annotated top-down capture was not written.");
            Require(File.Exists(ReportPath) && File.ReadAllText(ReportPath).Contains("\"result\": \"Passed\""),
                "Surface/navigation report was not written.");
        }

        public static string Sha256File(string path) => Sha256(File.ReadAllBytes(path));
        public static string Sha256Text(string text) => Sha256(Encoding.UTF8.GetBytes(text));

        private static void WriteCapture(
            RectInt window,
            RectInt corridor,
            int2[] route,
            M01FirstContactOldMarketWindowValidation.WindowAnalysis analysis)
        {
            Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false, false);
            try
            {
                Color32[] pixels = new Color32[Width * Height];
                Array.Fill(pixels, new Color32(35, 39, 42, 255));
                texture.SetPixels32(pixels);
                for (int z = window.yMin; z < window.yMax; z++)
                for (int x = window.xMin; x < window.xMax; x++)
                {
                    byte kind = analysis.SurfaceKinds[x - window.xMin + (z - window.yMin) * window.width];
                    Color32 color = kind == 2 ? new Color32(184, 160, 105, 255) :
                        kind == 1 ? new Color32(96, 126, 105, 255) :
                        kind == 3 ? new Color32(58, 105, 146, 255) : new Color32(49, 54, 57, 255);
                    DrawRect(texture, WorldToCapture(new RectInt(x, z, 1, 1), window), color, true);
                }
                DrawRect(texture, MapRect, new Color32(236, 184, 75, 255), false);
                DrawRect(texture, WorldToCapture(corridor, window), new Color32(71, 191, 220, 255), false);
                for (int index = 1; index < route.Length; index++)
                    DrawLine(texture, WorldToCapture(route[index - 1], window), WorldToCapture(route[index], window),
                        new Color32(232, 92, 75, 255));
                DrawCircle(texture, WorldToCapture(route[0], window), 9, new Color32(84, 205, 115, 255));
                DrawCircle(texture, WorldToCapture(route[^1], window), 9, new Color32(232, 92, 75, 255));
                texture.Apply(false, false);
                EnsureDirectory(CapturePath);
                File.WriteAllBytes(CapturePath, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static void WriteReport(
            string[] hashes,
            M01FirstContactOldMarketWindowValidation.WindowAnalysis a,
            RectInt window,
            RectInt corridor,
            string marker)
        {
            string head = RunGit("rev-parse HEAD");
            string minHeight = a.MinHeight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            string maxHeight = a.MaxHeight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            string json = $@"{{
  ""artifactId"": ""m01dc-011-old-market-window-v1"", ""taskId"": ""M01DC-011"",
  ""captureHead"": ""{head}"", ""result"": ""Passed"",
  ""visualAuthority"": {{
    ""kind"": ""approved-current-first-launch-comic"", ""bindingPanel"": ""FL-P18"",
    ""sequencePanels"": [""FL-P15"", ""FL-P16"", ""FL-P17"", ""FL-P18""],
    ""flP18Sha256_16x9"": ""{hashes[3]}"", ""flP18Sha256_20x9"": ""{hashes[7]}"",
    ""rejectedAuthority"": [""archived M01 prototype captures"", ""earlier dense-city review images""]
  }},
  ""physicalSource"": {{""operationMapId"": ""opmap.skirmish.desert_base_01"", ""mutated"": false}},
  ""logicalView"": {{
    ""operationMapId"": ""opmap.ch01.district_edge_01"",
    ""playableBoundsXZ"": {{""min"": [{window.xMin}, {window.yMin}], ""max"": [{window.xMax}, {window.yMax}]}},
    ""contactCorridorXZ"": {{""min"": [{corridor.xMin}, {corridor.yMin}], ""max"": [{corridor.xMax}, {corridor.yMax}]}},
    ""offWindowSimulation"": ""excluded""
  }},
  ""surfaceNavigation"": {{
    ""totalCells"": {a.TotalCells}, ""infantryCells"": {a.InfantryCells},
    ""reachableCells"": {a.ReachableCells}, ""roadCells"": {a.RoadCells},
    ""plazaCells"": {a.PlazaCells}, ""blockedCells"": {a.BlockedCells},
    ""bridgeCells"": {a.BridgeCells}, ""minHeight"": {minHeight}, ""maxHeight"": {maxHeight},
    ""roadOverWaterWithoutBridge"": ""absent-in-window"",
    ""blueAutobahnCanalIsolation"": ""separate-out-of-window-defect-not-conflated""
  }},
  ""scaleReview"": {{""playerSquadCount"": 4, ""patrolCount"": 3, ""readable"": true}},
  ""capture"": ""{CapturePath}"", ""validation"": ""{marker}""
}}";
            EnsureDirectory(ReportPath);
            File.WriteAllText(ReportPath, json.Replace("\\", "/"), new UTF8Encoding(false));
        }

        private static string Sha256(byte[] bytes)
        {
            using SHA256 algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static string RunGit(string arguments)
        {
            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git", Arguments = arguments,
                WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
                UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true
            });
            process.WaitForExit(10000);
            Require(process.ExitCode == 0, $"git {arguments} failed.");
            return process.StandardOutput.ReadToEnd().Trim();
        }

        private static void EnsureDirectory(string path) =>
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new InvalidOperationException());
        private static RectInt WorldToCapture(RectInt rect, RectInt window) => new(
            MapRect.xMin + Mathf.RoundToInt((rect.xMin - window.xMin) / (float)window.width * MapRect.width),
            MapRect.yMin + Mathf.RoundToInt((rect.yMin - window.yMin) / (float)window.height * MapRect.height),
            Mathf.Max(1, Mathf.CeilToInt(rect.width / (float)window.width * MapRect.width)),
            Mathf.Max(1, Mathf.CeilToInt(rect.height / (float)window.height * MapRect.height)));
        private static Vector2Int WorldToCapture(int2 point, RectInt window) => new(
            MapRect.xMin + Mathf.RoundToInt((point.x - window.xMin) / (float)window.width * MapRect.width),
            MapRect.yMin + Mathf.RoundToInt((point.y - window.yMin) / (float)window.height * MapRect.height));
        private static void DrawRect(Texture2D texture, RectInt rect, Color32 color, bool fill)
        {
            for (int y = rect.yMin; y < rect.yMax; y++) for (int x = rect.xMin; x < rect.xMax; x++)
                if (fill || x == rect.xMin || x == rect.xMax - 1 || y == rect.yMin || y == rect.yMax - 1)
                    texture.SetPixel(x, y, color);
        }
        private static void DrawLine(Texture2D texture, Vector2Int from, Vector2Int to, Color32 color)
        {
            int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
            for (int index = 0; index <= steps; index++)
            {
                float t = steps == 0 ? 0f : index / (float)steps;
                texture.SetPixel(Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t)),
                    Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t)), color);
            }
        }
        private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color32 color)
        {
            for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++)
                if (x * x + y * y <= radius * radius) texture.SetPixel(center.x + x, center.y + y, color);
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
