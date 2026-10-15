#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Renders the SCN-13 battle library to PNG for Theatre, Map and Grid at 16:9, 20:9 and 4:3,
    /// in English and Persian. This is an Editor capture, not player-readiness evidence.
    /// </summary>
    public static class SkirmishBattleLibraryCapture
    {
        public const string OutputDirectory = "Design/AgentReports/SkirmishBattleLibrary/2026-10-14";
        public const string PassMarker = "[SkirmishBattleLibraryCapture] result=Passed";

        private static readonly CaptureSize[] Sizes =
        {
            new CaptureSize("16x9", 1920, 1080),
            new CaptureSize("20x9", 2400, 1080),
            new CaptureSize("4x3", 1440, 1080)
        };

        private static readonly string[] Locales = { "en", "fa-IR" };
        private static readonly string[] States = { "theater", "map", "grid" };

        [MenuItem("Game/UI/V3/Capture Skirmish Battle Library")]
        public static void CaptureMenu()
        {
            Capture(false);
        }

        public static void RunFocusedValidation()
        {
            Capture(true);
        }

        private static void Capture(bool quit)
        {
            string previousLocale = GameLocalization.CurrentLocaleCode;
            var shots = new List<CaptureIdentity>();
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
                if (catalog == null)
                    throw new FileNotFoundException("Missing V3 localisation catalog.", V3UiLocalizationCatalogBuilder.CatalogPath);
                GameLocalization.Initialize(catalog, "en", false);
                UiShellRuntimeGateway.BindLocalization(new CaptureLocalization());

                SkirmishSetupPrefabBuilder.Build();
                SkirmishSetupPrefabBuilder.RequireFreshPrefab();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkirmishSetupPrefabBuilder.PrefabPath);
                if (prefab == null)
                    throw new FileNotFoundException("Missing SCN-13 prefab.");

                string folder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory));
                Directory.CreateDirectory(folder);
                int written = 0;
                for (int localeIndex = 0; localeIndex < Locales.Length; localeIndex++)
                {
                    GameLocalization.SetLocale(Locales[localeIndex], false);
                    for (int sizeIndex = 0; sizeIndex < Sizes.Length; sizeIndex++)
                    {
                        for (int stateIndex = 0; stateIndex < States.Length; stateIndex++)
                        {
                            string path = Path.Combine(
                                folder,
                                States[stateIndex] + "-" + Sizes[sizeIndex].Name + "-" + Locales[localeIndex] + ".png");
                            Render(prefab, Sizes[sizeIndex], States[stateIndex], path, false);
                            AcceptShot(path, States[stateIndex], Locales[localeIndex], shots);
                            written++;
                        }
                    }
                }

                var notchSize = new CaptureSize("16x9", 1920, 1080);
                for (int localeIndex = 0; localeIndex < Locales.Length; localeIndex++)
                {
                    GameLocalization.SetLocale(Locales[localeIndex], false);
                    for (int stateIndex = 0; stateIndex < States.Length; stateIndex++)
                    {
                        string path = Path.Combine(
                            folder,
                            States[stateIndex] + "-16x9-" + Locales[localeIndex] + "-notch.png");
                        Render(prefab, notchSize, States[stateIndex], path, true);
                        AcceptShot(path, States[stateIndex], Locales[localeIndex], shots);
                        written++;
                    }
                }

                if (written != 24)
                    throw new InvalidOperationException("Expected 24 captures, wrote " + written + ".");
                RejectDuplicateCaptures(shots);
                Debug.Log(PassMarker + " files=" + written + " folder=" + OutputDirectory);
                if (quit)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishBattleLibraryCapture] result=Failed\n" + exception);
                if (quit)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousLocale))
                    GameLocalization.SetLocale(previousLocale, false);
            }
        }

        private static void Render(GameObject prefab, CaptureSize size, string state, string path, bool notch)
        {
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            RenderTexture target = null;
            try
            {
                cameraObject = new GameObject("SkirmishLibraryCaptureCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = size.Height * 0.5f;
                camera.aspect = size.Width / (float)size.Height;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.02f, 0.05f, 0.06f, 1f);
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 200f;
                camera.transform.position = new Vector3(0f, 0f, -100f);

                canvasObject = new GameObject("SkirmishLibraryCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.pivot = new Vector2(0.5f, 0.5f);
                canvasRect.position = Vector3.zero;
                canvasRect.sizeDelta = new Vector2(size.Width, size.Height);
                canvasRect.localScale = Vector3.one;
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;

                var instance = UnityEngine.Object.Instantiate(prefab, canvasRect);
                var root = instance.GetComponent<RectTransform>();
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
                root.localScale = Vector3.one;
                root.localPosition = Vector3.zero;

                var view = instance.GetComponent<QuickCustomScreenView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("baseAssaultPreset").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                view.Bind(default);
                view.ApplyLibraryCaptureDemo(difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty);
                var safeFrame = instance.GetComponentInChildren<SkirmishLibrarySafeFrame>(true);
                if (notch && safeFrame != null)
                    safeFrame.SetSimulatedInsets(size.Width, size.Height, 36f, 72f, 36f, 28f);
                else
                    safeFrame?.ClearSimulatedInsets();
                instance.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
                view.PresentLibraryCapture(state);
                RejectUnresolvedSprites(view.Library);
                Canvas.ForceUpdateCanvases();
                instance.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
                RejectCollapsedLayout(instance);
                foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                    text.ForceMeshUpdate();
                RejectMissingGlyphs(instance);
                RejectUntranslatedFa(instance, path);
                RejectExtraLibraryRoots();
                IsolateCaptureLayer(canvasObject, camera);

                target = new RenderTexture(size.Width, size.Height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(size.Width, size.Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0f, 0f, size.Width, size.Height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(path, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }

                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        internal static void AcceptShot(string path, string state, string locale, List<CaptureIdentity> shots)
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 2048)
                throw new InvalidOperationException("Capture was empty: " + path);
            RejectFlatCapture(path);
            RejectOverlayTint(path);
            shots.Add(new CaptureIdentity(path, state, locale));
        }

        internal static void RejectExtraLibraryRoots()
        {
            QuickCustomScreenView[] views = UnityEngine.Object.FindObjectsByType<QuickCustomScreenView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null || EditorUtility.IsPersistent(views[i]))
                    continue;
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException("Expected one SCN-13 root at render, found " + count + ".");
        }

        internal static void IsolateCaptureLayer(GameObject canvas, Camera camera)
        {
            const int layer = 31;
            SetCaptureLayer(canvas, layer);
            if (camera != null)
                camera.cullingMask = 1 << layer;
        }

        private static void SetCaptureLayer(GameObject root, int layer)
        {
            if (root == null)
                return;
            root.layer = layer;
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetCaptureLayer(transform.GetChild(i).gameObject, layer);
        }

        internal static void RejectDuplicateCaptures(List<CaptureIdentity> shots)
        {
            if (shots == null)
                return;
            var seen = new Dictionary<string, CaptureIdentity>(StringComparer.Ordinal);
            for (int i = 0; i < shots.Count; i++)
            {
                CaptureIdentity shot = shots[i];
                string hash = HashFile(shot.Path);
                if (!seen.TryGetValue(hash, out CaptureIdentity previous))
                {
                    seen.Add(hash, shot);
                    continue;
                }

                bool sameState = string.Equals(previous.State, shot.State, StringComparison.Ordinal);
                bool sameLocale = string.Equals(previous.Locale, shot.Locale, StringComparison.OrdinalIgnoreCase);
                if (sameState && sameLocale)
                    continue;
                throw new InvalidOperationException(
                    "Capture files are identical across different states or locales: " +
                    Path.GetFileName(previous.Path) + " and " + Path.GetFileName(shot.Path) +
                    " hash=" + hash.Substring(0, 8));
            }
        }

        private static string HashFile(string path)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(File.ReadAllBytes(path));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        internal readonly struct CaptureIdentity
        {
            public readonly string Path;
            public readonly string State;
            public readonly string Locale;

            public CaptureIdentity(string path, string state, string locale)
            {
                Path = path;
                State = state ?? string.Empty;
                Locale = locale ?? string.Empty;
            }
        }

        internal static void RejectMissingGlyphs(GameObject root)
        {
            if (root == null || !GameLocalization.IsRightToLeft)
                return;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy)
                    continue;
                string value = text.text;
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                TMP_FontAsset font = text.font;
                if (font == null)
                    throw new InvalidOperationException("FA text has no TMP font on " + text.name + ".");
                uint[] missing;
                bool present = font.HasCharacters(value, out missing, true, false);
                if (!present || (missing != null && missing.Length > 0))
                {
                    string shown = value.Length > 48 ? value.Substring(0, 48) : value;
                    int code = missing != null && missing.Length > 0 ? (int)missing[0] : 0;
                    throw new InvalidOperationException(
                        "FA text is missing a glyph on " + text.name + " U+" + code.ToString("X4") + " text=" + shown);
                }
            }
        }

        internal static void RejectUntranslatedFa(GameObject root, string path)
        {
            if (root == null || !GameLocalization.IsRightToLeft)
                return;
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrEmpty(text.text))
                    continue;
                string value = text.text;
                for (int c = 0; c < value.Length; c++)
                {
                    if (IsArabicScript(value[c]))
                        return;
                }
            }

            throw new InvalidOperationException("FA shot has no Arabic-script glyphs: " + path);
        }

        private static bool IsArabicScript(char value)
        {
            int code = value;
            return (code >= 0x0600 && code <= 0x06FF)
                || (code >= 0x0750 && code <= 0x077F)
                || (code >= 0x08A0 && code <= 0x08FF)
                || (code >= 0xFB50 && code <= 0xFDFF)
                || (code >= 0xFE70 && code <= 0xFEFF);
        }

        internal static void RejectCollapsedLayout(GameObject instance)
        {
            Transform composition = instance != null ? instance.transform.Find("SkirmishSetupComposition") : null;
            if (composition == null)
                throw new InvalidOperationException("SCN-13 composition is missing.");
            var layout = composition.GetComponent<MainMenuV3SectionLayoutView>();
            if (layout == null)
                throw new InvalidOperationException("SCN-13 section layout is missing.");
            if (layout.UsesMenuFrame)
                throw new InvalidOperationException("SCN-13 menu frame overrides the library panels.");
            layout.RefreshLayout();
            Canvas.ForceUpdateCanvases();

            var map = composition.Find("OperationPreview") as RectTransform;
            var briefing = composition.Find("BaseAssaultRules") as RectTransform;
            float extra = layout.LastAppliedExtraWidth;
            ExpectPanel(
                map,
                "map",
                SkirmishSetupPrefabBuilder.MapPanelX,
                -SkirmishSetupPrefabBuilder.MapPanelY,
                SkirmishSetupPrefabBuilder.MapPanelWidth + extra,
                SkirmishSetupPrefabBuilder.MapPanelHeight);
            ExpectPanel(
                briefing,
                "briefing",
                SkirmishSetupPrefabBuilder.BriefingPanelX + extra,
                -SkirmishSetupPrefabBuilder.BriefingPanelY,
                SkirmishSetupPrefabBuilder.BriefingPanelWidth,
                SkirmishSetupPrefabBuilder.BriefingPanelHeight);
            RequireFilled(map, "map");
            RequireFilled(briefing, "briefing");
            var plate = composition.Find("OperationPreview/MapPreviewClip") as RectTransform;
            var column = composition.Find("BaseAssaultRules/BriefingColumn") as RectTransform;
            if (column != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(column);
            RequireFilled(plate, "map plate");
            RequireFilled(column, "briefing column");
        }

        private static void ExpectPanel(RectTransform rect, string label, float x, float y, float width, float height)
        {
            if (rect == null)
                throw new InvalidOperationException("Panel is missing: " + label);
            const float tolerance = 2f;
            bool placed =
                Mathf.Abs(rect.anchoredPosition.x - x) <= tolerance &&
                Mathf.Abs(rect.anchoredPosition.y - y) <= tolerance &&
                Mathf.Abs(rect.sizeDelta.x - width) <= tolerance &&
                Mathf.Abs(rect.sizeDelta.y - height) <= tolerance;
            if (placed)
                return;
            string actual = rect.anchoredPosition.x.ToString("0.#") + "," + rect.anchoredPosition.y.ToString("0.#") +
                " " + rect.sizeDelta.x.ToString("0.#") + "x" + rect.sizeDelta.y.ToString("0.#");
            string expected = x.ToString("0.#") + "," + y.ToString("0.#") + " " + width.ToString("0.#") + "x" + height.ToString("0.#");
            if (label == "map" && Mathf.Abs(rect.sizeDelta.x - width) > tolerance)
                throw new InvalidOperationException("Map panel width is " + rect.sizeDelta.x.ToString("0.#") + "; expected " + width.ToString("0.#") + ".");
            string title = label == "briefing" ? "Briefing panel is not at its expected rect." : "Map panel is not at its expected rect.";
            throw new InvalidOperationException(title + " rect=" + actual + " expected=" + expected);
        }

        private static void RequireFilled(RectTransform rect, string label)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Panel is missing: " + label);
            if (rect.rect.width < 200f || rect.rect.height < 160f)
                throw new InvalidOperationException(
                    "Panel is collapsed: " + label + " size=" + rect.rect.width.ToString("0.#") + "x" + rect.rect.height.ToString("0.#"));
        }

        internal static void RejectUnresolvedSprites(ISkirmishLibraryViewModel library)
        {
            if (library == null)
                throw new InvalidOperationException("Sprite check has no library.");
            SkirmishTheaterPinState[] pins = library.Theater.Pins;
            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                    RequireResolvedSprite(pins[i].PlatePreviewKey);
            }

            SkirmishBattlefieldState field = library.Battlefield;
            RequireCardSprites(field.MapCards);
            RequireCardSprites(field.GridCards);
            RequireFolderSprites(SkirmishLibraryPreviewAssets.Root + "/Previews");
            RequireFolderSprites(SkirmishLibraryPreviewAssets.Root + "/Plates");
        }

        private static void RequireCardSprites(SkirmishBattleCardState[] cards)
        {
            if (cards == null)
                return;
            for (int i = 0; i < cards.Length; i++)
                RequireResolvedSprite(cards[i].PreviewSpriteKey);
        }

        private static void RequireResolvedSprite(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            if (SkirmishLibraryPreviewAssets.Load(key) == null)
                throw new InvalidOperationException("Sprite key is set but resolved to null: " + key);
        }

        private static void RequireFolderSprites(string folder)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolute = Path.Combine(projectRoot ?? string.Empty, folder);
            if (!Directory.Exists(absolute))
                throw new InvalidOperationException("Sprite folder is missing: " + folder);
            string[] files = Directory.GetFiles(absolute, "*.png");
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = folder + "/" + Path.GetFileName(files[i]);
                string logical = Path.GetFileNameWithoutExtension(files[i]);
                RequireResolvedSprite(assetPath);
                RequireResolvedSprite(logical);
            }
        }

        internal static void RejectFlatCapture(string path)
        {
            var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!probe.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Capture could not be read: " + path);
                Color32[] pixels = probe.GetPixels32();
                if (pixels == null || pixels.Length < 64)
                    throw new InvalidOperationException("Capture had no pixels: " + path);
                int step = Math.Max(1, pixels.Length / 8000);
                int samples = 0;
                int minR = 255, minG = 255, minB = 255, maxR = 0, maxG = 0, maxB = 0;
                for (int i = 0; i < pixels.Length; i += step)
                {
                    Color32 pixel = pixels[i];
                    if (pixel.r < minR) minR = pixel.r;
                    if (pixel.g < minG) minG = pixel.g;
                    if (pixel.b < minB) minB = pixel.b;
                    if (pixel.r > maxR) maxR = pixel.r;
                    if (pixel.g > maxG) maxG = pixel.g;
                    if (pixel.b > maxB) maxB = pixel.b;
                    samples++;
                }

                int spread = (maxR - minR) + (maxG - minG) + (maxB - minB);
                if (samples < 32 || spread < 48)
                    throw new InvalidOperationException("Capture is blank or uniform: " + path + " spread=" + spread);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        internal static void RejectOverlayTint(string path)
        {
            var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!probe.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Capture could not be read: " + path);
                Color32[] pixels = probe.GetPixels32();
                int width = probe.width;
                int height = probe.height;
                if (pixels == null || width < 32 || height < 32)
                    throw new InvalidOperationException("Capture had no art region: " + path);
                RejectRegionTint(path, pixels, width, height, "map", 0.05f, 0.60f, 0.22f, 0.76f);
                RejectRegionTint(path, pixels, width, height, "briefing", 0.66f, 0.96f, 0.22f, 0.76f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private static void RejectRegionTint(
            string path,
            Color32[] pixels,
            int width,
            int height,
            string region,
            float x0,
            float x1,
            float yTop0,
            float yTop1)
        {
            int left = Mathf.Clamp(Mathf.FloorToInt(width * x0), 0, width - 1);
            int right = Mathf.Clamp(Mathf.CeilToInt(width * x1), left + 1, width);
            int top = Mathf.Clamp(Mathf.FloorToInt(height * yTop0), 0, height - 1);
            int bottom = Mathf.Clamp(Mathf.CeilToInt(height * yTop1), top + 1, height);
            int stepX = Math.Max(1, (right - left) / 48);
            int stepY = Math.Max(1, (bottom - top) / 48);
            int samples = 0;
            int nearMean = 0;
            int goldCast = 0;
            double sumR = 0, sumG = 0, sumB = 0, sumL = 0, sumL2 = 0;
            var sample = new System.Collections.Generic.List<Color32>(2500);
            for (int yTop = top; yTop < bottom; yTop += stepY)
            {
                int y = height - 1 - yTop;
                for (int x = left; x < right; x += stepX)
                {
                    Color32 pixel = pixels[y * width + x];
                    sample.Add(pixel);
                    sumR += pixel.r;
                    sumG += pixel.g;
                    sumB += pixel.b;
                    double luminance = pixel.r * 0.299 + pixel.g * 0.587 + pixel.b * 0.114;
                    sumL += luminance;
                    sumL2 += luminance * luminance;
                    if (pixel.r > pixel.b + 20 && pixel.g > pixel.b + 5)
                        goldCast++;
                    samples++;
                }
            }

            if (samples < 32)
                return;
            double meanR = sumR / samples;
            double meanG = sumG / samples;
            double meanB = sumB / samples;
            double meanL = sumL / samples;
            double variance = Math.Max(0, sumL2 / samples - meanL * meanL);
            double deviation = Math.Sqrt(variance);
            for (int i = 0; i < sample.Count; i++)
            {
                Color32 pixel = sample[i];
                if (Math.Abs(pixel.r - meanR) <= 18 && Math.Abs(pixel.g - meanG) <= 18 && Math.Abs(pixel.b - meanB) <= 18)
                    nearMean++;
            }

            double uniform = nearMean / (double)samples;
            double cast = goldCast / (double)samples;
            bool goldMean = meanR > 90 && meanR > meanB + 25 && meanG > meanB + 10 && meanL > 30;
            bool uniformGold = uniform > 0.82 && goldMean;
            bool castWash = cast > 0.88 && deviation < 26 && meanR > meanB + 18;
            if (uniformGold || castWash)
            {
                throw new InvalidOperationException(
                    "Panel art is a uniform overlay tint: " + path +
                    " region=" + region +
                    " uniform=" + uniform.ToString("0.00") +
                    " cast=" + cast.ToString("0.00") +
                    " deviation=" + deviation.ToString("0.0") +
                    " mean=" + (int)meanR + "," + (int)meanG + "," + (int)meanB);
            }
        }

        private struct CaptureSize
        {
            public readonly string Name;
            public readonly int Width;
            public readonly int Height;
            public CaptureSize(string name, int width, int height)
            {
                Name = name;
                Width = width;
                Height = height;
            }
        }

        private sealed class CaptureLocalization : IUiLocalization
        {
            public event Action LocaleChanged
            {
                add => GameLocalization.LocaleChanged += value;
                remove => GameLocalization.LocaleChanged -= value;
            }

            public string CurrentLocaleCode => GameLocalization.CurrentLocaleCode;
            public bool IsRightToLeft => GameLocalization.IsRightToLeft;
            public UnityEngine.Object CurrentFontAsset => GameLocalization.CurrentFontAsset;
            public int AvailableLocaleCount => 2;
            public string[] GetLocaleShortLabels() => GameLocalization.GetLocaleShortLabels();
            public string[] GetLocaleSwitchLabels() => GameLocalization.GetLocaleSwitchLabels();
            public int GetLocaleIndex(string code) => GameLocalization.GetLocaleIndex(code);
            public string GetLocaleCode(int index) => GameLocalization.GetLocaleCode(index);
            public bool SetLocale(string code, bool persist = true) => GameLocalization.SetLocale(code, persist);
            public string Get(string key, string fallback = "") => GameLocalization.Get(key, fallback);
            public bool TryGet(string key, out string value) => GameLocalization.TryGet(key, out value);
            public string Format(string key, string fallback, params object[] args) => string.Format(Get(key, fallback), args);
            public string GetBySource(string source) => GameLocalization.GetBySource(source);
            public bool TryGetBySource(string source, out string key, out string value) => GameLocalization.TryGetBySource(source, out key, out value);
            public bool TryGetSourceByLocalized(string value, out string key, out string source) =>
                GameLocalization.TryGetSourceByLocalized(value, out key, out source);
        }
    }
}
#endif
