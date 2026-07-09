using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class ResourceExchangePopupLayoutCaptureValidation
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab";
        private const string OutputDirectory = "Design/AgentReports/Captures/ResourceExchange";
        private const string ReportPath = OutputDirectory + "/POP12_ResourceExchange_layout_capture_report.md";
        private const float PanelWidth = 1640f;
        private const float PanelHeight = 916f;
        private const float BoundsPadding = 12f;
        private const int MinimumBrightPixels = 12000;

        private static readonly CaptureSpec[] CaptureSpecs =
        {
            new("16x9", 1920, 1080, "POP12_ResourceExchange_16x9_1920x1080.png"),
            new("20x9", 2400, 1080, "POP12_ResourceExchange_20x9_2400x1080.png")
        };

        [MenuItem("Game/UI/Validate Resource Exchange Popup Layout Captures")]
        public static void Run()
        {
            try
            {
                List<CaptureResult> results = CaptureAll();
                WriteReport(results);
                Debug.Log($"[ResourceExchangePopupLayoutCaptureValidation] result=Passed captures={results.Count} report={ReportPath}");
                Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ResourceExchangePopupLayoutCaptureValidation] result=Failed\n{exception}");
                Exit(1);
            }
        }

        private static List<CaptureResult> CaptureAll()
        {
            Directory.CreateDirectory(OutputDirectory);
            var results = new List<CaptureResult>(CaptureSpecs.Length);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(prefab != null, $"Missing Resource Exchange popup prefab at {PrefabPath}.");

            for (int i = 0; i < CaptureSpecs.Length; i++)
                results.Add(Capture(prefab, CaptureSpecs[i]));

            return results;
        }

        private static CaptureResult Capture(GameObject prefab, CaptureSpec spec)
        {
            Camera camera = null;
            Canvas canvas = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            GameObject instance = null;
            try
            {
                camera = CreateCamera(spec.Width, spec.Height);
                canvas = CreateCanvas(camera, spec.Width, spec.Height);
                instance = Object.Instantiate(prefab, canvas.transform);
                instance.name = "POP12_ResourceExchangePopup_Capture";
                RectTransform root = RequireRect(instance.transform, "POP12_ResourceExchangePopup_Capture");
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
                root.localScale = Vector3.one;
                instance.SetActive(true);

                ResourceExchangePopupView view = instance.GetComponent<ResourceExchangePopupView>();
                Require(view != null, "Capture instance is missing ResourceExchangePopupView.");
                view.Show();

                Canvas.ForceUpdateCanvases();
                ValidateRectLayout(instance.transform, spec);
                ValidateActiveText(instance.transform, spec);

                string path = Path.Combine(OutputDirectory, spec.FileName);
                texture = Render(camera, spec, path, out renderTexture);
                int brightPixels = CountBrightPixels(texture);
                Require(
                    brightPixels >= MinimumBrightPixels,
                    $"{spec.Label} capture appears blank or too dark: brightPixels={brightPixels}.");

                return new CaptureResult(spec.Label, spec.Width, spec.Height, path, brightPixels);
            }
            finally
            {
                if (texture != null)
                    Object.DestroyImmediate(texture);
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (instance != null)
                    Object.DestroyImmediate(instance);
                if (canvas != null)
                    Object.DestroyImmediate(canvas.gameObject);
                if (camera != null)
                    Object.DestroyImmediate(camera.gameObject);
            }
        }

        private static Camera CreateCamera(int width, int height)
        {
            var cameraObject = new GameObject("ResourceExchangePopupLayoutCaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.017f, 0.018f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.aspect = width / (float)height;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            return camera;
        }

        private static Canvas CreateCanvas(Camera camera, int width, int height)
        {
            var canvasObject = new GameObject("ResourceExchangePopupLayoutCaptureCanvas", typeof(RectTransform));
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 1f;

            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.position = Vector3.zero;
            rect.rotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return canvas;
        }

        private static Texture2D Render(Camera camera, CaptureSpec spec, string path, out RenderTexture renderTexture)
        {
            renderTexture = new RenderTexture(spec.Width, spec.Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                name = "ResourceExchangePopupLayoutCapture"
            };
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();

                var texture = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, spec.Width, spec.Height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }
        }

        private static void ValidateRectLayout(Transform root, CaptureSpec spec)
        {
            RectTransform panel = RequireRect(root.Find("ResourceExchangeRoot"), "ResourceExchangeRoot");
            Vector2 panelSize = panel.rect.size;
            Require(Mathf.Abs(panelSize.x - PanelWidth) <= 1f, $"{spec.Label} panel width drifted: {panelSize.x}.");
            Require(Mathf.Abs(panelSize.y - PanelHeight) <= 1f, $"{spec.Label} panel height drifted: {panelSize.y}.");
            RequireInsideCapture(panel, spec, "ResourceExchangeRoot");

            RequireInsideCapture(RequireRect(root.Find("ResourceExchangeRoot/Header"), "Header"), spec, "Header");
            RequireInsideCapture(RequireRect(root.Find("ResourceExchangeRoot/RecipeCards"), "RecipeCards"), spec, "RecipeCards");
            RequireInsideCapture(RequireRect(root.Find("ResourceExchangeRoot/DetailPanel"), "DetailPanel"), spec, "DetailPanel");
            RequireInsideCapture(RequireRect(root.Find("ResourceExchangeRoot/ExchangeQueuePanel"), "ExchangeQueuePanel"), spec, "ExchangeQueuePanel");
            RequireInsideCapture(RequireRect(root.Find("ResourceExchangeRoot/InstructionRail"), "InstructionRail"), spec, "InstructionRail");
            RequireAbove(
                RequireRect(root.Find("ResourceExchangeRoot/DetailPanel/RequirementsValue"), "RequirementsValue"),
                RequireRect(root.Find("ResourceExchangeRoot/DetailPanel/ConfirmButton"), "ConfirmButton"),
                spec,
                "RequirementsValue",
                "ConfirmButton");
            RequireAbove(
                RequireRect(root.Find("ResourceExchangeRoot/DetailPanel/ConfirmButton"), "ConfirmButton"),
                RequireRect(root.Find("ResourceExchangeRoot/DetailPanel/Instruction"), "Instruction"),
                spec,
                "ConfirmButton",
                "Instruction");
        }

        private static void ValidateActiveText(Transform root, CaptureSpec spec)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            Require(texts.Length > 40, $"{spec.Label} expected live TMP labels in POP-12.");

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy)
                    continue;

                RectTransform rect = text.rectTransform;
                RequireInsideCapture(rect, spec, text.name);
                text.ForceMeshUpdate();
                Require(!text.isTextOverflowing, $"{spec.Label} text overflows: {GetPath(text.transform)} value='{text.text}'.");
            }
        }

        private static void RequireInsideCapture(RectTransform rect, CaptureSpec spec, string label)
        {
            WorldRect worldRect = GetWorldRect(rect);

            float halfWidth = spec.Width * 0.5f;
            float halfHeight = spec.Height * 0.5f;
            Require(worldRect.MinX >= -halfWidth + BoundsPadding, $"{spec.Label} {label} left bound is outside capture: {worldRect.MinX.ToString(CultureInfo.InvariantCulture)}.");
            Require(worldRect.MaxX <= halfWidth - BoundsPadding, $"{spec.Label} {label} right bound is outside capture: {worldRect.MaxX.ToString(CultureInfo.InvariantCulture)}.");
            Require(worldRect.MinY >= -halfHeight + BoundsPadding, $"{spec.Label} {label} bottom bound is outside capture: {worldRect.MinY.ToString(CultureInfo.InvariantCulture)}.");
            Require(worldRect.MaxY <= halfHeight - BoundsPadding, $"{spec.Label} {label} top bound is outside capture: {worldRect.MaxY.ToString(CultureInfo.InvariantCulture)}.");
        }

        private static void RequireAbove(RectTransform upper, RectTransform lower, CaptureSpec spec, string upperLabel, string lowerLabel)
        {
            WorldRect upperRect = GetWorldRect(upper);
            WorldRect lowerRect = GetWorldRect(lower);
            Require(
                upperRect.MinY >= lowerRect.MaxY + 4f,
                $"{spec.Label} {upperLabel} overlaps {lowerLabel}: upperMinY={upperRect.MinY.ToString(CultureInfo.InvariantCulture)} lowerMaxY={lowerRect.MaxY.ToString(CultureInfo.InvariantCulture)}.");
        }

        private static WorldRect GetWorldRect(RectTransform rect)
        {
            rect.GetWorldCorners(Corners);
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < Corners.Length; i++)
            {
                Vector3 corner = Corners[i];
                minX = Mathf.Min(minX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxX = Mathf.Max(maxX, corner.x);
                maxY = Mathf.Max(maxY, corner.y);
            }

            return new WorldRect(minX, minY, maxX, maxY);
        }

        private static int CountBrightPixels(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int count = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                Color32 pixel = pixels[i];
                if (pixel.a > 8 && (pixel.r > 70 || pixel.g > 70 || pixel.b > 70))
                    count++;
            }

            return count;
        }

        private static void WriteReport(IReadOnlyList<CaptureResult> results)
        {
            using var writer = new StreamWriter(ReportPath, false);
            writer.WriteLine("# POP-12 Resource Exchange Layout Capture Validation");
            writer.WriteLine();
            writer.WriteLine("Generated by `ResourceExchangePopupLayoutCaptureValidation.Run`.");
            writer.WriteLine();
            writer.WriteLine("| Aspect | Resolution | Capture | Bright pixel samples |");
            writer.WriteLine("|---|---:|---|---:|");
            for (int i = 0; i < results.Count; i++)
            {
                CaptureResult result = results[i];
                writer.WriteLine(
                    $"| {result.Label} | {result.Width}x{result.Height} | `{result.Path}` | {result.BrightPixels.ToString(CultureInfo.InvariantCulture)} |");
            }

            writer.WriteLine();
            writer.WriteLine("Validation checks:");
            writer.WriteLine("- Captures are rendered from the live `POP12_ResourceExchangePopup` Canvas prefab, not from the target-lock PNG.");
            writer.WriteLine("- 16:9 and 20:9 captures are nonblank and saved at exact requested resolutions.");
            writer.WriteLine("- Popup, header, recipe cards, detail panel, queue panel, and instruction rail remain inside the viewport.");
            writer.WriteLine("- The modal panel keeps its fixed 1640x916 layout size on both aspects.");
            writer.WriteLine("- Active TMP text is checked for overflow.");
        }

        private static RectTransform RequireRect(Transform transform, string label)
        {
            Require(transform != null, $"Missing required layout object {label}.");
            RectTransform rect = transform as RectTransform;
            Require(rect != null, $"{label} must be a RectTransform.");
            return rect;
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }

        private static readonly Vector3[] Corners = new Vector3[4];

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly int Width;
            public readonly int Height;
            public readonly string FileName;

            public CaptureSpec(string label, int width, int height, string fileName)
            {
                Label = label;
                Width = width;
                Height = height;
                FileName = fileName;
            }
        }

        private readonly struct CaptureResult
        {
            public readonly string Label;
            public readonly int Width;
            public readonly int Height;
            public readonly string Path;
            public readonly int BrightPixels;

            public CaptureResult(string label, int width, int height, string path, int brightPixels)
            {
                Label = label;
                Width = width;
                Height = height;
                Path = path;
                BrightPixels = brightPixels;
            }
        }

        private readonly struct WorldRect
        {
            public readonly float MinX;
            public readonly float MinY;
            public readonly float MaxX;
            public readonly float MaxY;

            public WorldRect(float minX, float minY, float maxX, float maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }
        }
    }
}
