#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
        private static readonly Dictionary<string, string> ZonePlayableCounts = new Dictionary<string, string>(StringComparer.Ordinal);

        [MenuItem("Game/UI/V3/Capture Skirmish Battle Library")]
        public static void CaptureMenu()
        {
            Capture(false);
        }

        public static void RunFocusedValidation()
        {
            Capture(true);
        }

        private static readonly List<string> TheatreViolations = new List<string>();

        internal static void ResetZoneCountLedger()
        {
            ZonePlayableCounts.Clear();
        }

        internal static void BeginTheatreReport()
        {
            TheatreViolations.Clear();
        }

        internal static void NoteTheatre(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            TheatreViolations.Add(TheatreSeatGeometry.Stamp(message));
        }

        internal static void FinishTheatreReport()
        {
            if (TheatreViolations.Count == 0)
                return;
            string report = "Theatre violations=" + TheatreViolations.Count + "\n" + string.Join("\n", TheatreViolations);
            TheatreViolations.Clear();
            throw new InvalidOperationException(report);
        }

        internal static string PendingTheatreSuffix()
        {
            if (TheatreViolations.Count == 0)
                return string.Empty;
            return "\n" + string.Join("\n", TheatreViolations);
        }

        private static void GuardTheatre(Action check)
        {
            if (check == null)
                return;
            try
            {
                check();
            }
            catch (InvalidOperationException exception)
            {
                NoteTheatre(exception.Message);
            }
        }

        private static void Capture(bool quit)
        {
            ResetZoneCountLedger();
            BeginTheatreReport();
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
                TheatreSeatGeometry.DumpDirectory = folder;
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

                var notchSize = new CaptureSize("20x9", 2400, 1080);
                for (int localeIndex = 0; localeIndex < Locales.Length; localeIndex++)
                {
                    GameLocalization.SetLocale(Locales[localeIndex], false);
                    for (int stateIndex = 0; stateIndex < States.Length; stateIndex++)
                    {
                        string path = Path.Combine(
                            folder,
                            States[stateIndex] + "-20x9-" + Locales[localeIndex] + "-notch.png");
                        Render(prefab, notchSize, States[stateIndex], path, true);
                        AcceptShot(path, States[stateIndex], Locales[localeIndex], shots);
                        written++;
                    }
                }

                if (written != 24)
                    throw new InvalidOperationException("Expected 24 captures, wrote " + written + ".");
                RejectDuplicateCaptures(shots);
                FinishTheatreReport();
                Debug.Log(PassMarker + " files=" + written + " folder=" + OutputDirectory);
                if (quit)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[SkirmishBattleLibraryCapture] result=Failed\n" + exception + PendingTheatreSuffix());
                if (quit)
                    EditorApplication.Exit(1);
                else
                    throw;
            }
            finally
            {
                TheatreSeatGeometry.DumpDirectory = null;
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
                // Bind presents the library, and that presentation fits the cue. The shot
                // locale has to be current first, or the opening Farsi 16:9 pass logs en.
                string ratio = string.IsNullOrEmpty(size.Name) ? "unset" : size.Name.Replace('x', ':');
                string shotLocale = GameLocalization.IsRightToLeft ? "fa" : "en";
                float insetLeft = notch ? 132f : 0f;
                float insetTop = notch ? 84f : 0f;
                TheatreSeatGeometry.SetShot(
                    ratio,
                    notch ? "notch" : "none",
                    shotLocale,
                    true,
                    size.Width,
                    size.Height,
                    insetLeft,
                    insetTop,
                    0f,
                    0f);
                view.Bind(default);
                TheatreSeatGeometry.SeatingReady = false;
                view.ApplyLibraryCaptureDemo(difficulty => SkirmishLibraryLaunchContext.Difficulty = difficulty);
                var safeFrame = instance.GetComponentInChildren<SkirmishLibrarySafeFrame>(true);
                safeFrame?.ClearSimulatedInsets();
                Canvas.ForceUpdateCanvases();
                instance.GetComponentInChildren<MainMenuV3SectionLayoutView>(true)?.RefreshLayout();
                Canvas.ForceUpdateCanvases();
                TheatreSeatGeometry.SeatingReady = true;
                view.PresentLibraryCapture(state);
                RejectUnresolvedSprites(view.Library);
                Canvas.ForceUpdateCanvases();
                RejectCollapsedLayout(instance);
                foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                    text.ForceMeshUpdate();
                RejectMissingGlyphs(instance);
                RejectUntranslatedFa(instance, path);
                RejectEmptyCardSubtitles(instance);
                RejectGridCardsOutside(instance);
                RejectBlankImages(instance, state);
                RejectExtraLibraryRoots();
                if (notch && safeFrame != null)
                {
                    safeFrame.SetSimulatedInsets(size.Width, size.Height, 132f, 84f, 0f, 0f);
                    safeFrame.CommitSimulatedInset();
                }
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
                if (string.Equals(state, "theater", StringComparison.OrdinalIgnoreCase))
                {
                    GuardTheatre(() => RejectDistrictPainting(image, canvasRect, instance));
                    GuardTheatre(() => RejectZoneNameContrast(image, canvasRect, instance));
                }
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
                TheatreSeatGeometry.SeatingReady = true;
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
                bool notchMismatch = IsNotchCapture(previous.Path) != IsNotchCapture(shot.Path);
                if (sameState && sameLocale && notchMismatch)
                {
                    throw new InvalidOperationException(
                        "Notch capture matches its plain counterpart: " +
                        Path.GetFileName(previous.Path) + " and " + Path.GetFileName(shot.Path) +
                        " hash=" + hash.Substring(0, 8));
                }

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

        internal static void RejectEmptyCardSubtitles(GameObject root)
        {
            if (root == null)
                return;
            string locale = GameLocalization.IsRightToLeft ? "fa" : "en";
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.name != "Subtitle" || !text.gameObject.activeInHierarchy)
                    continue;
                Transform parent = text.transform.parent;
                if (parent == null || !parent.gameObject.activeInHierarchy)
                    continue;
                bool card = parent.GetComponent<UnityEngine.UI.Button>() != null || parent.name == "EmptyCell";
                if (!card)
                    continue;
                string value = text.text;
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException(
                        "Visible card subtitle is empty locale=" + locale + " path=" + ImagePath(text.transform));
                text.ForceMeshUpdate();
                int visible = 0;
                TMP_TextInfo info = text.textInfo;
                if (info != null && info.characterInfo != null)
                {
                    int count = Mathf.Min(info.characterCount, info.characterInfo.Length);
                    for (int c = 0; c < count; c++)
                    {
                        if (info.characterInfo[c].isVisible)
                            visible++;
                    }
                }

                if (visible == 0)
                {
                    string shown = value.Length > 48 ? value.Substring(0, 48) : value;
                    throw new InvalidOperationException(
                        "Visible card subtitle draws no glyphs locale=" + locale + " path=" + ImagePath(text.transform) + " text=" + shown);
                }
            }
        }

        internal static void RejectGridCardsOutside(GameObject root)
        {
            if (root == null)
                return;
            RejectTheaterOverlays(root);
            Transform grid = FindNamed(root.transform, "LibraryGrid");
            var gridRect = grid as RectTransform;
            if (gridRect == null || !grid.gameObject.activeInHierarchy)
                return;
            Canvas.ForceUpdateCanvases();
            ScrollRect scroll = grid.GetComponent<ScrollRect>() ?? grid.GetComponentInChildren<ScrollRect>(true);
            RectTransform viewport = scroll != null && scroll.viewport != null ? scroll.viewport : gridRect;
            RectTransform content = scroll != null ? scroll.content : null;
            bool scrolls = content != null && content.rect.height > viewport.rect.height + 2f;
            bool wideFrame = IsWideLibraryFrame(grid);
            string locale = GameLocalization.IsRightToLeft ? "fa" : "en";
            RejectGridCardsOutside(grid, viewport, content, scrolls, wideFrame, locale);
            RejectZoneTabNames(grid, locale);
            RejectGridZoneRows(grid, locale);
            RejectVisibleGridRowTotals(root, locale);
        }

        private static bool IsWideLibraryFrame(Transform node)
        {
            Canvas canvas = node != null ? node.GetComponentInParent<Canvas>() : null;
            RectTransform rect = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (rect == null || rect.rect.height < 200f)
                return false;
            return rect.rect.width / rect.rect.height >= 1.6f;
        }

        private static void RejectZoneTabNames(Transform grid, string locale)
        {
            if (grid == null)
                return;
            bool fa = string.Equals(locale, "fa", StringComparison.OrdinalIgnoreCase);
            var tabs = new List<RectTransform>();
            for (int i = 0; i < grid.childCount; i++)
            {
                Transform tab = grid.GetChild(i);
                if (tab == null || !tab.gameObject.activeInHierarchy ||
                    !tab.name.StartsWith("BattlefieldTab_", StringComparison.Ordinal))
                    continue;
                if (tab is RectTransform tabRect)
                    tabs.Add(tabRect);
            }

            RejectZoneTabBand(grid, tabs, locale);

            int found = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                RectTransform tab = tabs[i];
                found++;
                string mapId = tab.name.Substring("BattlefieldTab_".Length);
                TMP_Text name = tab.Find("ZoneTabName")?.GetComponent<TMP_Text>();
                SkirmishBattleLibraryArt.Copy(mapId, fa, out string title, out _);
                if (name == null || !name.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(name.text) ||
                    !SkirmishBattleLibraryArt.SameRenderedText(name.text, title))
                {
                    throw new InvalidOperationException(
                        "Zone tab is missing its name locale=" + locale +
                        " map=" + mapId +
                        " text=" + (name != null ? name.text : string.Empty) + ".");
                }

                bool visible = SkirmishZoneTabWidget.NameIsVisible(name, title, out bool truncated, out _, out float local);
                if (!visible || truncated || local < SkirmishZoneTabWidget.NameFontFloor - 0.05f)
                {
                    throw new InvalidOperationException(
                        "Zone tab name is truncated locale=" + locale +
                        " map=" + mapId +
                        " font=" + local.ToString("0.0") +
                        " text=" + name.text + ".");
                }
            }

            if (found != SkirmishBattleLibraryArt.MapIds.Length)
            {
                throw new InvalidOperationException(
                    "Grid is missing a zone tab locale=" + locale + " tabs=" + found + ".");
            }
        }

        /// <summary>
        /// The tab strip may be 60px or 196px. What still has to hold is containment:
        /// every tab inside the band, no neighbour overlap, and the band plus each
        /// preview inside the grid while the preview stays at least 40% of its card.
        /// </summary>
        private static void RejectZoneTabBand(Transform grid, List<RectTransform> tabs, string locale)
        {
            var gridRect = grid as RectTransform;
            Rect gridBounds = gridRect != null ? WorldBounds(gridRect) : new Rect();
            Rect band = ZoneTabBand(grid, gridBounds);
            var report = new StringBuilder();
            string ratio = string.IsNullOrEmpty(TheatreSeatGeometry.ShotRatio) ? "unset" : TheatreSeatGeometry.ShotRatio;
            report.Append("[ZoneTabBand] locale=").Append(locale);
            report.Append(" ratio=").Append(ratio);
            report.Append(" band=").Append(FormatRect(band));
            report.Append(" tabs=").Append(tabs != null ? tabs.Count : 0);
            if (tabs != null)
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    Rect tabBounds = WorldBounds(tabs[i]);
                    report.Append(" ").Append(tabs[i].name).Append("=").Append(FormatRect(tabBounds));
                }
            }

            Debug.Log(report.ToString());
            const float slop = 2f;
            if (gridRect == null || band.height < 1f || !Contains(gridBounds, band, slop))
            {
                throw new InvalidOperationException(
                    "Zone tab band does not fit inside the card locale=" + locale +
                    " band=" + FormatRect(band) +
                    " card=" + FormatRect(gridBounds) + ".");
            }

            if (tabs == null)
                return;
            for (int i = 0; i < tabs.Count; i++)
            {
                Rect tabBounds = WorldBounds(tabs[i]);
                if (!Contains(band, tabBounds, slop))
                {
                    throw new InvalidOperationException(
                        "Zone tab is outside its band locale=" + locale +
                        " tab=" + tabs[i].name +
                        " tabRect=" + FormatRect(tabBounds) +
                        " band=" + FormatRect(band) + ".");
                }

                for (int other = i + 1; other < tabs.Count; other++)
                {
                    if (!WorldOverlap(tabs[i], tabs[other], 1f))
                        continue;
                    throw new InvalidOperationException(
                        "Zone tab overlaps a neighbour locale=" + locale +
                        " tab=" + tabs[i].name +
                        " other=" + tabs[other].name + ".");
                }
            }

            var rows = new List<Transform>();
            CollectZoneRows(grid, rows);
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                Transform row = rows[rowIndex];
                for (int childIndex = 0; childIndex < row.childCount; childIndex++)
                {
                    Transform child = row.GetChild(childIndex);
                    if (child == null || !child.gameObject.activeInHierarchy)
                        continue;
                    bool card = child.name == "EmptyCell" || child.name.StartsWith("BattleCard_", StringComparison.Ordinal);
                    if (!card || child is not RectTransform cardRect)
                        continue;
                    Transform preview = cardRect.Find("Preview");
                    var previewRect = preview as RectTransform;
                    if (previewRect == null)
                    {
                        throw new InvalidOperationException(
                            "Grid card has no preview locale=" + locale +
                            " path=" + ImagePath(cardRect));
                    }

                    float cardHeight = Span(cardRect, false);
                    float previewHeight = Span(previewRect, false);
                    if (cardHeight < 2f || previewHeight < cardHeight * 0.40f)
                    {
                        throw new InvalidOperationException(
                            "Grid preview is under 40% of the card locale=" + locale +
                            " path=" + ImagePath(cardRect) +
                            " preview=" + previewHeight.ToString("0.0") +
                            " card=" + cardHeight.ToString("0.0"));
                    }

                    Rect cardBounds = WorldBounds(cardRect);
                    Rect previewBounds = WorldBounds(previewRect);
                    if (!Contains(cardBounds, previewBounds, slop))
                    {
                        throw new InvalidOperationException(
                            "Grid preview does not fit inside the card locale=" + locale +
                            " path=" + ImagePath(cardRect) +
                            " preview=" + FormatRect(previewBounds) +
                            " card=" + FormatRect(cardBounds) + ".");
                    }

                    Rect scrollBounds = GridScrollBounds(grid, gridBounds);
                    bool previewShown = RectsOverlap(scrollBounds, previewBounds, 0f);
                    if (band.height + previewBounds.height > gridBounds.height + slop ||
                        (previewShown && RectsOverlap(band, previewBounds, 1f)))
                    {
                        throw new InvalidOperationException(
                            "Zone tab band plus preview does not fit inside the card locale=" + locale +
                            " path=" + ImagePath(cardRect) +
                            " band=" + FormatRect(band) +
                            " preview=" + FormatRect(previewBounds) +
                            " card=" + FormatRect(gridBounds) + ".");
                    }
                }
            }
        }

        private static Rect GridScrollBounds(Transform grid, Rect gridBounds)
        {
            if (grid == null)
                return gridBounds;
            for (int i = 0; i < grid.childCount; i++)
            {
                Transform child = grid.GetChild(i);
                if (child != null && child.name == "GridScroll" && child is RectTransform scroll)
                    return WorldBounds(scroll);
            }

            return gridBounds;
        }

        private static Rect ZoneTabBand(Transform grid, Rect gridBounds)
        {
            float seam = float.MinValue;
            bool seamFound = false;
            if (grid != null)
            {
                for (int i = 0; i < grid.childCount; i++)
                {
                    Transform child = grid.GetChild(i);
                    if (child == null || !child.gameObject.activeInHierarchy || child is not RectTransform rect)
                        continue;
                    bool header = child.name.StartsWith("Column_", StringComparison.Ordinal);
                    bool scroll = child.name == "GridScroll";
                    if (!header && !scroll)
                        continue;
                    float top = WorldBounds(rect).yMax;
                    if (!seamFound || top > seam)
                    {
                        seam = top;
                        seamFound = true;
                    }
                }
            }

            float bottom = seamFound ? Mathf.Min(seam, gridBounds.yMax) : gridBounds.yMin;
            if (bottom > gridBounds.yMax)
                bottom = gridBounds.yMax;
            return Rect.MinMaxRect(gridBounds.xMin, bottom, gridBounds.xMax, gridBounds.yMax);
        }

        private static string FormatRect(Rect rect)
        {
            return rect.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ","
                + rect.y.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ","
                + rect.width.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ","
                + rect.height.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void RejectGridZoneRows(Transform grid, string locale)
        {
            var rows = new List<Transform>();
            CollectZoneRows(grid, rows);
            if (rows.Count == 0)
                throw new InvalidOperationException("Grid has no zone rows locale=" + locale);
            var ids = new HashSet<string>();
            var tops = new HashSet<int>();
            for (int i = 0; i < rows.Count; i++)
            {
                Transform row = rows[i];
                string id = row.name.Length > "ZoneRow_".Length ? row.name.Substring("ZoneRow_".Length) : string.Empty;
                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                    throw new InvalidOperationException("Grid zone row is duplicated locale=" + locale + " row=" + row.name);
                var rowRect = row as RectTransform;
                if (rowRect != null && !tops.Add(Mathf.RoundToInt(rowRect.anchoredPosition.y)))
                    throw new InvalidOperationException("Grid zone rows share a line locale=" + locale + " row=" + id);
                int cards = 0;
                int labels = 0;
                float rowY = 0f;
                bool haveY = false;
                for (int childIndex = 0; childIndex < row.childCount; childIndex++)
                {
                    Transform child = row.GetChild(childIndex);
                    if (child == null || !child.gameObject.activeInHierarchy)
                        continue;
                    bool card = child.name == "EmptyCell" || child.name.StartsWith("BattleCard_", StringComparison.Ordinal);
                    if (card && child is RectTransform cardRect)
                    {
                        if (!haveY)
                        {
                            rowY = cardRect.anchoredPosition.y;
                            haveY = true;
                        }
                        else if (Mathf.Abs(cardRect.anchoredPosition.y - rowY) > 2f)
                        {
                            throw new InvalidOperationException(
                                "Zone row wraps onto another line locale=" + locale +
                                " row=" + id +
                                " path=" + ImagePath(child));
                        }

                        cards++;
                        RejectGridPreviewDominant(cardRect, locale, id);
                    }

                    if (child.name.StartsWith("Row_", StringComparison.Ordinal) &&
                        !child.name.StartsWith("RowCount_", StringComparison.Ordinal) &&
                        child.name != "RowIcon")
                        labels++;
                }

                if (cards != 6)
                {
                    throw new InvalidOperationException(
                        "Zone row does not contain its six cards on one row locale=" + locale +
                        " row=" + id +
                        " cards=" + cards);
                }

                if (labels != 1)
                {
                    throw new InvalidOperationException(
                        "Zone row label is not one line locale=" + locale +
                        " row=" + id +
                        " labels=" + labels);
                }
            }

            RejectOrphanGridCards(grid, false, locale);
        }

        private static void RejectGridPreviewDominant(RectTransform card, string locale, string zone)
        {
            Transform preview = card.Find("Preview");
            var previewRect = preview as RectTransform;
            if (previewRect == null)
            {
                throw new InvalidOperationException(
                    "Grid card has no preview locale=" + locale + " zone=" + zone + " path=" + ImagePath(card));
            }

            RawImage picture = preview.GetComponent<RawImage>();
            if (picture == null || picture.texture == null || picture.texture.name == "SkirmishLibraryPlaceholder")
            {
                throw new InvalidOperationException(
                    "Grid preview is not scenario or theatre plate art locale=" + locale +
                    " zone=" + zone +
                    " path=" + ImagePath(card));
            }

            Color tint = picture.color;
            bool finalArt = tint.r > 0.9f && tint.g > 0.9f && tint.b > 0.9f;
            RejectPendingPreviewTag(card, finalArt, locale, zone);
            RejectGridLockWrap(card, locale, zone);

            float cardWidth = Span(card, true);
            float cardHeight = Span(card, false);
            float previewWidth = Span(previewRect, true);
            float previewHeight = Span(previewRect, false);
            if (cardHeight < 2f || previewHeight < cardHeight * 0.40f)
            {
                throw new InvalidOperationException(
                    "Grid preview is under 40% of the card locale=" + locale +
                    " zone=" + zone +
                    " path=" + ImagePath(card) +
                    " preview=" + previewHeight.ToString("0.0") +
                    " card=" + cardHeight.ToString("0.0"));
            }

            if (previewWidth < cardWidth - 4f)
            {
                throw new InvalidOperationException(
                    "Grid preview is not the full card width locale=" + locale +
                    " zone=" + zone +
                    " path=" + ImagePath(card) +
                    " preview=" + previewWidth.ToString("0.0") +
                    " card=" + cardWidth.ToString("0.0"));
            }

            Vector3[] previewCorners = new Vector3[4];
            Vector3[] cardCorners = new Vector3[4];
            previewRect.GetWorldCorners(previewCorners);
            card.GetWorldCorners(cardCorners);
            float previewTop = previewCorners[1].y;
            float cardTop = cardCorners[1].y;
            for (int i = 0; i < 4; i++)
            {
                if (previewCorners[i].y > previewTop)
                    previewTop = previewCorners[i].y;
                if (cardCorners[i].y > cardTop)
                    cardTop = cardCorners[i].y;
            }

            float scale = Mathf.Max(0.01f, card.lossyScale.y);
            if (Mathf.Abs(cardTop - previewTop) > 3f * scale)
            {
                throw new InvalidOperationException(
                    "Grid preview is not at the top of the card locale=" + locale +
                    " zone=" + zone +
                    " path=" + ImagePath(card));
            }
        }

        /// <summary>World bounds of the drawn glyph quads (line metrics excluded: FA fonts report tall ascenders).</summary>
        private static bool TryInkBounds(TMP_Text text, out Rect ink)
        {
            ink = default;
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            if (info == null || info.characterInfo == null)
                return false;
            bool any = false;
            float minX = 0f, minY = 0f, maxX = 0f, maxY = 0f;
            int count = Mathf.Min(info.characterCount, info.characterInfo.Length);
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                    continue;
                Vector3 bl = text.rectTransform.TransformPoint(c.bottomLeft);
                Vector3 tr = text.rectTransform.TransformPoint(c.topRight);
                float x0 = Mathf.Min(bl.x, tr.x), x1 = Mathf.Max(bl.x, tr.x), y0 = Mathf.Min(bl.y, tr.y), y1 = Mathf.Max(bl.y, tr.y);
                if (!any) { minX = x0; maxX = x1; minY = y0; maxY = y1; any = true; }
                else { minX = Mathf.Min(minX, x0); maxX = Mathf.Max(maxX, x1); minY = Mathf.Min(minY, y0); maxY = Mathf.Max(maxY, y1); }
            }

            if (any)
                ink = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return any;
        }

        /// <summary>Theatre detail panel: no visible text may overlap another text, icon, preview or cabinet slot in the panel.</summary>
        private static void RejectTheatreDetailOverlap(Transform root, string locale)
        {
            Transform panel = root != null ? FindNamed(root, QuickCustomScreenView.TheatreDetailPanelName) : null;
            if (panel == null || !panel.gameObject.activeInHierarchy)
                return;
            TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(false);
            Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(false);
            foreach (TMP_Text text in texts)
            {
                if (text == null || !text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text))
                    continue;
                if (!TryInkBounds(text, out Rect ink))
                    continue;
                foreach (Graphic other in graphics)
                {
                    if (other == null || other == text || !other.isActiveAndEnabled || other.transform == panel)
                        continue;
                    if (other.transform.IsChildOf(text.transform) || text.transform.IsChildOf(other.transform))
                        continue;
                    if (other.name == "Rule")
                        continue;
                    Rect box;
                    if (other is TMP_Text otherText)
                    {
                        if (string.IsNullOrWhiteSpace(otherText.text) || !TryInkBounds(otherText, out box))
                            continue;
                    }
                    else
                    {
                        box = WorldBounds(other.rectTransform);
                    }

                    float w = Mathf.Min(ink.xMax, box.xMax) - Mathf.Max(ink.xMin, box.xMin);
                    float h = Mathf.Min(ink.yMax, box.yMax) - Mathf.Max(ink.yMin, box.yMin);
                    if (w > 2f && h > 2f)
                        TheatreViolations.Add(
                            "Theatre detail text overlaps another element locale=" + locale +
                            " text=" + ImagePath(text.transform) + " '" + text.text + "' other=" + ImagePath(other.transform) + ".");
                }
            }
        }

        /// <summary>Counts are won/playable; an empty zone reads "Coming soon". Any visible 0/0 is a regression.</summary>
        private static void RejectZeroOverZero(Transform root, string locale)
        {
            if (root == null)
                return;
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(false))
            {
                if (text == null || !text.isActiveAndEnabled || string.IsNullOrEmpty(text.text))
                    continue;
                string value = text.text.Replace("\u2066", string.Empty).Replace("\u2069", string.Empty).Replace("\u200f", string.Empty)
                    .Replace('\u06F0', '0').Replace('\u0660', '0').Replace(" ", string.Empty);
                if (System.Text.RegularExpressions.Regex.IsMatch(value, @"(^|[^0-9])0/0($|[^0-9])"))
                {
                    throw new InvalidOperationException(
                        "A zero-playable count shows 0/0 instead of Coming soon locale=" + locale +
                        " path=" + ImagePath(text.transform) + " text=" + text.text);
                }
            }
        }

        private static void RejectGridLockWrap(RectTransform card, string locale, string zone)
        {
            Transform locked = card.Find("Locked");
            if (locked == null || !locked.gameObject.activeInHierarchy)
                return;
            TMP_Text reason = locked.GetComponentInChildren<TMP_Text>(true);
            if (reason == null || string.IsNullOrWhiteSpace(reason.text))
                return;
            reason.ForceMeshUpdate();
            int lines = reason.textInfo != null ? reason.textInfo.lineCount : 0;
            if (reason.maxVisibleLines > 2 || lines > 2 || reason.isTextTruncated)
            {
                throw new InvalidOperationException(
                    "Grid lock text does not fit in two lines locale=" + locale +
                    " zone=" + zone +
                    " lines=" + lines +
                    " path=" + ImagePath(reason.transform) +
                    " text=" + reason.text);
            }
        }

        private static void RejectPendingPreviewTag(RectTransform card, bool finalArt, string locale, string zone)
        {
            Transform pending = card.Find("PreviewPending");
            bool shown = pending != null && pending.gameObject.activeInHierarchy;
            if (finalArt)
            {
                if (shown)
                {
                    throw new InvalidOperationException(
                        "Final scenario preview is marked pending locale=" + locale +
                        " zone=" + zone +
                        " path=" + ImagePath(card));
                }

                return;
            }

            TMP_Text label = shown ? pending.GetComponentInChildren<TMP_Text>(true) : null;
            if (!shown || label == null || !label.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(label.text))
            {
                throw new InvalidOperationException(
                    "Dimmed grid preview has no pending tag locale=" + locale +
                    " zone=" + zone +
                    " path=" + ImagePath(card));
            }

            label.ForceMeshUpdate();
            int visible = 0;
            TMP_TextInfo info = label.textInfo;
            if (info != null && info.characterInfo != null)
            {
                int count = Mathf.Min(info.characterCount, info.characterInfo.Length);
                for (int c = 0; c < count; c++)
                {
                    if (info.characterInfo[c].isVisible)
                        visible++;
                }
            }

            if (visible == 0)
            {
                throw new InvalidOperationException(
                    "Pending preview tag draws no glyphs locale=" + locale +
                    " zone=" + zone +
                    " path=" + ImagePath(label.transform));
            }
        }

        private static float Span(RectTransform rect, bool horizontal)
        {
            if (rect == null)
                return 0f;
            float live = horizontal ? rect.rect.width : rect.rect.height;
            float authored = horizontal ? rect.sizeDelta.x : rect.sizeDelta.y;
            return live > 2f ? live : authored;
        }

        private static void RejectOrphanGridCards(Transform parent, bool insideRow, string locale)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;
                bool card = child.name == "EmptyCell" || child.name.StartsWith("BattleCard_", StringComparison.Ordinal);
                if (card)
                {
                    if (!insideRow)
                    {
                        throw new InvalidOperationException(
                            "Grid card is outside its zone row locale=" + locale + " path=" + ImagePath(child));
                    }

                    continue;
                }

                bool row = child.name.StartsWith("ZoneRow_", StringComparison.Ordinal);
                RejectOrphanGridCards(child, insideRow || row, locale);
            }
        }

        private static void CollectZoneRows(Transform parent, List<Transform> rows)
        {
            if (parent == null)
                return;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;
                if (child.name.StartsWith("ZoneRow_", StringComparison.Ordinal))
                    rows.Add(child);
                else if (child.name.StartsWith("BattleCard_", StringComparison.Ordinal) || child.name == "EmptyCell")
                    continue;
                else
                    CollectZoneRows(child, rows);
            }
        }

        private static void RejectGridCardsOutside(Transform parent, RectTransform viewport, RectTransform content, bool scrolls, bool wideFrame, string locale)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;
                bool card = child.name == "EmptyCell" || child.name.StartsWith("BattleCard_", StringComparison.Ordinal);
                var cardRect = child as RectTransform;
                if (!card || cardRect == null)
                {
                    RejectGridCardsOutside(child, viewport, content, scrolls, wideFrame, locale);
                    continue;
                }

                RequireInside(viewport, cardRect, locale, "viewport", true);
                if (content != null)
                    RequireInside(content, cardRect, locale, "scroll content", false);
                if (!scrolls || wideFrame)
                    RequireInside(viewport, cardRect, locale, "viewport", false);
                Image[] images = child.GetComponentsInChildren<Image>(true);
                for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
                {
                    Image image = images[imageIndex];
                    if (image == null || !image.gameObject.activeInHierarchy || !image.name.StartsWith("Medal", StringComparison.Ordinal))
                        continue;
                    RequireInside(viewport, image.rectTransform, locale, "viewport", true);
                    if (content != null)
                        RequireInside(content, image.rectTransform, locale, "scroll content", false);
                    if (!scrolls || wideFrame)
                        RequireInside(viewport, image.rectTransform, locale, "viewport", false);
                }
            }
        }

        private static void RequireInside(RectTransform bounds, RectTransform item, string locale, string container, bool horizontalOnly)
        {
            const float slop = 2f;
            Vector3[] corners = new Vector3[4];
            item.GetWorldCorners(corners);
            Rect rect = bounds.rect;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = bounds.InverseTransformPoint(corners[i]);
                bool outsideX = local.x < rect.xMin - slop || local.x > rect.xMax + slop;
                bool outsideY = local.y < rect.yMin - slop || local.y > rect.yMax + slop;
                if (!outsideX && (horizontalOnly || !outsideY))
                    continue;
                throw new InvalidOperationException(
                    "Grid card is outside the " + container +
                    " locale=" + locale +
                    " gridWidth=" + rect.width.ToString("0.0") +
                    " gridHeight=" + rect.height.ToString("0.0") +
                    " path=" + ImagePath(item) +
                    " local=" + local.x.ToString("0.0") + "," + local.y.ToString("0.0"));
            }
        }

        private static void RejectTheaterOverlays(GameObject root)
        {
            Transform regions = FindNamed(root.transform, "LibraryRegions");
            if (regions == null || !regions.gameObject.activeInHierarchy)
                return;
            var clip = FindNamed(root.transform, "MapPreviewClip") as RectTransform;
            if (clip == null || !clip.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Map clip is missing for the theatre overlays.");
            Canvas.ForceUpdateCanvases();
            string locale = GameLocalization.IsRightToLeft ? "fa" : "en";
            QuickCustomScreenView view = root.GetComponent<QuickCustomScreenView>();
            SkirmishTheaterPinState[] pins = view != null && view.LibraryViewModel != null
                ? view.LibraryViewModel.Theater.Pins
                : null;
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            var cards = new RectTransform[mapIds.Length];
            var names = new RectTransform[mapIds.Length];
            for (int i = 0; i < mapIds.Length; i++)
            {
                int index = i;
                GuardTheatre(() =>
                {
                    RectTransform outline = RequireTheaterOverlay(regions, clip, "ZoneOutline_" + mapIds[index], "outline", mapIds[index], locale);
                    RejectZonePolygon(outline, clip, mapIds[index], locale);
                    RequireTheaterOverlay(regions, clip, "ZonePin_" + mapIds[index], "pin", mapIds[index], locale);
                    RectTransform card = RequireTheaterOverlay(regions, clip, "ZoneCard_" + mapIds[index], "zone card", mapIds[index], locale);
                    cards[index] = card;
                    names[index] = card != null ? card.Find("ZoneName") as RectTransform : null;
                    RejectZoneCardContent(card, pins, mapIds[index], locale);
                    RejectZoneLeader(regions, mapIds[index], locale);
                    RejectSelectedZoneMatchesGridRows(view, pins, mapIds[index], locale);
                });
            }

            GuardTheatre(() => RejectDistrictHalo(regions, mapIds, locale));
            GuardTheatre(() => RejectDistrictHexAspect(regions, mapIds, locale));
            GuardTheatre(() => RejectZoneProgressMatchesCard(root, view, locale));
            GuardTheatre(() => RejectStripMatchesZoneTotals(root, view, locale));
            GuardTheatre(() => RejectMirroredTheatreChrome(root, locale));
            GuardTheatre(() => RejectReserveOverlapsDetailSide(root, view, locale));

            RectTransform cue = null;
            Transform cueNode = FindNamed(regions, "ZoomCue");
            bool cueVisible = cueNode != null && cueNode.gameObject.activeInHierarchy
                && cueNode is RectTransform cueRect
                && (cueRect.rect.width >= 8f || cueRect.sizeDelta.x >= 8f)
                && (cueRect.rect.height >= 8f || cueRect.sizeDelta.y >= 8f);
            if (!cueVisible)
            {
                GuardTheatre(() =>
                {
                    throw new InvalidOperationException(
                        "Theatre zoom pill is missing locale=" + locale + ".");
                });
            }

            if (cueNode != null && cueNode.gameObject.activeInHierarchy)
            {
                GuardTheatre(() =>
                {
                    cue = RequireTheaterOverlay(regions, clip, "ZoomCue", "zoom hint", "selected", locale);
                    TMP_Text cueText = cue != null ? cue.GetComponent<TMP_Text>() : null;
                    if (cueText != null)
                    {
                        RejectReadableLine(cueText, "zoom hint", "selected", locale);
                        if (cueText.fontSize < 14f)
                        {
                            throw new InvalidOperationException(
                                "Theatre zoom hint text is under 14px locale=" + locale +
                                " font=" + cueText.fontSize.ToString("0.0") + ".");
                        }
                    }
                });
            }

            string selectedMapId = view != null && view.LibraryViewModel != null
                ? view.LibraryViewModel.Theater.SelectedMapId
                : string.Empty;
            GuardTheatre(() => AppendMeasuredTheatreViolations(view, regions, locale, mapIds));
            GuardTheatre(() => RejectZoomPillOverCards(regions, locale, selectedMapId));
            GuardTheatre(() => RejectZoomPillOverHexes(regions, locale, selectedMapId));
            GuardTheatre(() => RejectZoomCueOverSelectedName(regions, locale, selectedMapId));
            GuardTheatre(() => RejectLatinNameInsideCard(regions, locale));
            GuardTheatre(() => RejectStripMedalOverlap(root, locale));
        }

        private static void AppendMeasuredTheatreViolations(
            QuickCustomScreenView view,
            Transform regions,
            string locale,
            string[] mapIds)
        {
            if (regions == null || mapIds == null)
                return;
            RectTransform space = null;
            var hexes = new TheatreSeatGeometry.Vec2[mapIds.Length][];
            var cards = new TheatreSeatGeometry.Box[mapIds.Length];
            var pins = new TheatreSeatGeometry.Box[mapIds.Length];
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform outlineNode = FindNamed(regions, "ZoneOutline_" + mapIds[i]);
                if (outlineNode is RectTransform outline)
                {
                    space = outline.parent as RectTransform ?? space;
                    if (space != null)
                        hexes[i] = PlateShape(space, outline);
                }

                Transform cardNode = FindNamed(regions, "ZoneCard_" + mapIds[i]);
                if (cardNode is RectTransform card && space != null)
                    cards[i] = PlateBox(space, card);
                Transform pinNode = FindNamed(regions, "ZonePin_" + mapIds[i]);
                if (pinNode is RectTransform pin && space != null)
                    pins[i] = PlateBox(space, pin);
            }

            if (space == null)
                return;
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            string selected = view != null && view.LibraryViewModel != null
                ? view.LibraryViewModel.Theater.SelectedMapId
                : "DB";
            TheatreSeatGeometry.Box cue = default;
            bool hasCue = false;
            Transform cueNode = FindNamed(regions, "ZoomCue");
            if (cueNode is RectTransform cueRect && cueRect.gameObject.activeInHierarchy)
            {
                cue = PlateBox(space, cueRect);
                hasCue = true;
            }

            TheatreSeatGeometry.Box hint = default;
            bool hasHint = false;
            Transform hintNode = FindNamed(regions.root, "ZoomHint");
            if (hintNode is RectTransform hintRect && hintRect.gameObject.activeInHierarchy)
            {
                hint = PlateBox(space, hintRect);
                hasHint = true;
            }

            TheatreSeatGeometry.Box detail = default;
            Transform panel = FindNamed(regions, "TheatreDetailPanel");
            if (panel is RectTransform panelRect && panelRect.gameObject.activeInHierarchy)
                detail = PlateBox(space, panelRect);
            else if (TheatreSeatGeometry.TheatreDetailReserved)
                detail = TheatreSeatGeometry.DetailRect(space.rect.width, space.rect.height, fa, true);

            var violations = new List<string>();
            TheatreSeatGeometry.CollectViolations(
                locale,
                space.rect.width,
                space.rect.height,
                selected,
                hexes,
                cards,
                pins,
                cue,
                hasCue,
                detail,
                hint,
                hasHint,
                violations);
            for (int i = 0; i < violations.Count; i++)
                NoteTheatre(violations[i]);
        }

        private static TheatreSeatGeometry.Vec2[] PlateShape(RectTransform space, RectTransform outline)
        {
            V3PolygonGraphic graphic = outline != null ? outline.GetComponent<V3PolygonGraphic>() : null;
            int count = graphic != null ? graphic.PointCount : 0;
            var plate = new TheatreSeatGeometry.Vec2[count];
            for (int point = 0; point < count; point++)
            {
                Vector2 authored = graphic.LocalPoint(point);
                Vector3 world = outline.TransformPoint(new Vector3(authored.x, authored.y, 0f));
                Vector2 local = PlatePoint(space, world);
                plate[point] = new TheatreSeatGeometry.Vec2(local.x, local.y);
            }

            return plate;
        }

        private static void RejectZoneCardsInsideOrClear(Transform regions, string[] mapIds, string locale)
        {
            if (regions == null || mapIds == null)
                return;
            var shapes = new TheatreSeatGeometry.Vec2[mapIds.Length][];
            var cards = new TheatreSeatGeometry.Box[mapIds.Length];
            var present = new bool[mapIds.Length];
            RectTransform space = null;
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform cardNode = FindNamed(regions, "ZoneCard_" + mapIds[i]);
                Transform outlineNode = FindNamed(regions, "ZoneOutline_" + mapIds[i]);
                if (cardNode is not RectTransform card || outlineNode is not RectTransform outline)
                    continue;
                space = outline.parent as RectTransform ?? space;
                if (space == null)
                    continue;
                present[i] = true;
                cards[i] = PlateBox(space, card);
                V3PolygonGraphic graphic = outline.GetComponent<V3PolygonGraphic>();
                int count = graphic != null ? graphic.PointCount : 0;
                var plate = new TheatreSeatGeometry.Vec2[count];
                for (int point = 0; point < count; point++)
                {
                    Vector2 authored = graphic.LocalPoint(point);
                    Vector3 world = outline.TransformPoint(new Vector3(authored.x, authored.y, 0f));
                    Vector2 local = PlatePoint(space, world);
                    plate[point] = new TheatreSeatGeometry.Vec2(local.x, local.y);
                }

                shapes[i] = plate;
            }

            for (int i = 0; i < mapIds.Length; i++)
            {
                if (!present[i])
                    continue;
                if (TheatreSeatGeometry.SeatHolds(cards[i], shapes[i], shapes))
                    continue;
                throw new InvalidOperationException(
                    "Theatre zone card is outside its territory locale=" + locale +
                    " card=ZoneCard_" + mapIds[i] + ".");
            }
        }

        private static TheatreSeatGeometry.Box PlateBox(RectTransform space, RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 first = PlatePoint(space, corners[0]);
            float minX = first.x;
            float maxX = first.x;
            float minY = first.y;
            float maxY = first.y;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = PlatePoint(space, corners[i]);
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            return new TheatreSeatGeometry.Box(minX, minY, Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY));
        }

        private static Vector2 PlatePoint(RectTransform space, Vector3 world)
        {
            Vector3 local = space.InverseTransformPoint(world);
            Rect rect = space.rect;
            return new Vector2(local.x - rect.xMin, rect.yMax - local.y);
        }

        private static void RejectDetailPanelClear(
            Transform regions,
            RectTransform[] cards,
            RectTransform cue,
            string[] mapIds,
            string locale)
        {
            if (regions == null || !TryTheatreDetailWorld(regions, locale, out Rect detail))
                return;
            if (cards != null)
            {
                for (int i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null || !RectsOverlap(WorldBounds(cards[i]), detail, 0f))
                        continue;
                    throw new InvalidOperationException(
                        "Theatre zone card intersects the detail panel locale=" + locale +
                        " card=" + cards[i].name + ".");
                }
            }

            if (cue != null && RectsOverlap(WorldBounds(cue), detail, 0f))
            {
                throw new InvalidOperationException(
                    "Theatre zoom hint intersects the detail panel locale=" + locale + ".");
            }

            if (mapIds == null)
                return;
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform pinNode = FindNamed(regions, "ZonePin_" + mapIds[i]);
                Transform cardNode = FindNamed(regions, "ZoneCard_" + mapIds[i]);
                if (pinNode is not RectTransform pin || cardNode is not RectTransform card)
                    continue;
                Rect pinBounds = WorldBounds(pin);
                Rect cardBounds = WorldBounds(card);
                Vector2 from = pinBounds.center;
                Vector2 to = NearestPointOnRect(cardBounds, from);
                if (!SegmentHitsRect(from, to, detail))
                    continue;
                throw new InvalidOperationException(
                    "Theatre leader intersects the detail panel locale=" + locale +
                    " map=" + mapIds[i] + ".");
            }
        }

        private static bool TryTheatreDetailWorld(Transform regions, string locale, out Rect detail)
        {
            detail = default;
            if (regions is not RectTransform plate)
                return false;
            Transform panel = FindNamed(regions, "TheatreDetailPanel");
            if (panel is RectTransform panelRect && panelRect.gameObject.activeInHierarchy)
            {
                detail = WorldBounds(panelRect);
                return detail.width > 1f && detail.height > 1f;
            }

            float plateWidth = plate.rect.width;
            float plateHeight = plate.rect.height;
            if (plateWidth < 600f || plateHeight < 2f)
                return false;
            float reserve = Mathf.Clamp(plateWidth * 0.22f, 230f, 290f);
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            const float margin = 10f;
            float width = reserve - margin * 2f;
            float height = Mathf.Max(200f, plateHeight - margin * 2f);
            float x = fa ? margin : plateWidth - reserve + margin;
            float left = plate.rect.xMin + x;
            float top = plate.rect.yMax - margin;
            Vector3 c0 = plate.TransformPoint(new Vector3(left, top, 0f));
            Vector3 c1 = plate.TransformPoint(new Vector3(left + width, top, 0f));
            Vector3 c2 = plate.TransformPoint(new Vector3(left + width, top - height, 0f));
            Vector3 c3 = plate.TransformPoint(new Vector3(left, top - height, 0f));
            detail = Rect.MinMaxRect(
                Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x)),
                Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y)),
                Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x)),
                Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y)));
            return detail.width > 1f && detail.height > 1f;
        }

        private static bool CardRestsInside(Rect card, Vector2[] shape, float margin)
        {
            if (shape == null || shape.Length < 3)
                return false;
            const int steps = 4;
            Vector2[] corners =
            {
                new Vector2(card.xMin, card.yMin),
                new Vector2(card.xMax, card.yMin),
                new Vector2(card.xMax, card.yMax),
                new Vector2(card.xMin, card.yMax)
            };
            for (int edge = 0; edge < corners.Length; edge++)
            {
                Vector2 a = corners[edge];
                Vector2 b = corners[(edge + 1) % corners.Length];
                for (int step = 0; step < steps; step++)
                {
                    Vector2 point = Vector2.Lerp(a, b, step / (float)steps);
                    if (!PointInPolygon(point, shape) || DistanceToPolygonEdge(point, shape) < margin)
                        return false;
                }
            }

            return true;
        }

        private static float PerimeterGap(Rect card, Vector2[] shape)
        {
            if (shape == null || shape.Length < 3)
                return float.MaxValue;
            float best = float.MaxValue;
            Vector2[] corners =
            {
                new Vector2(card.xMin, card.yMin),
                new Vector2(card.xMax, card.yMin),
                new Vector2(card.xMax, card.yMax),
                new Vector2(card.xMin, card.yMax)
            };
            for (int edge = 0; edge < corners.Length; edge++)
            {
                Vector2 a = corners[edge];
                Vector2 b = corners[(edge + 1) % corners.Length];
                for (int step = 0; step <= 4; step++)
                {
                    Vector2 point = Vector2.Lerp(a, b, step / 4f);
                    best = Mathf.Min(best, DistanceToPolygonEdge(point, shape));
                }
            }

            return best;
        }

        private static bool WorldRectHitsPolygon(Rect rect, Vector2[] shape)
        {
            if (shape == null || shape.Length < 3)
                return false;
            Vector2[] corners =
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                if (PointInPolygon(corners[i], shape))
                    return true;
            }

            for (int i = 0; i < shape.Length; i++)
            {
                if (rect.Contains(shape[i]))
                    return true;
                Vector2 next = shape[(i + 1) % shape.Length];
                if (SegmentHitsRect(shape[i], next, rect))
                    return true;
            }

            return false;
        }

        private static Vector2 NearestPointOnRect(Rect rect, Vector2 point)
        {
            return new Vector2(
                Mathf.Clamp(point.x, rect.xMin, rect.xMax),
                Mathf.Clamp(point.y, rect.yMin, rect.yMax));
        }

        private static bool RectsOverlap(Rect left, Rect right, float slop)
        {
            return left.xMin < right.xMax - slop && right.xMin < left.xMax - slop &&
                left.yMin < right.yMax - slop && right.yMin < left.yMax - slop;
        }

        private static void RejectCueNearSelectedPin(QuickCustomScreenView view, Transform regions, RectTransform cue, string locale)
        {
            if (view == null || regions is not RectTransform space || cue == null || view.LibraryViewModel == null)
                return;
            string mapId = view.LibraryViewModel.Theater.SelectedMapId;
            if (string.IsNullOrEmpty(mapId))
                return;
            if (FindNamed(regions, "ZonePin_" + mapId) is not RectTransform pin)
                return;
            TheatreSeatGeometry.Box cueBox = PlateBox(space, cue);
            TheatreSeatGeometry.Box pinBox = PlateBox(space, pin);
            TheatreSeatGeometry.Box cardBox = default;
            if (FindNamed(regions, "ZoneCard_" + mapId) is RectTransform card)
                cardBox = PlateBox(space, card);
            float distance = TheatreSeatGeometry.CueAnchorDistance(cueBox, cardBox, pinBox);
            Vector2[] shape = FindNamed(regions, "ZoneOutline_" + mapId) is RectTransform outline
                ? PlateVectors(PlateShape(space, outline))
                : Array.Empty<Vector2>();
            var hex = new TheatreSeatGeometry.Vec2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                hex[i] = new TheatreSeatGeometry.Vec2(shape[i].x, shape[i].y);
            if (TheatreSeatGeometry.CueCountsAsNear(cueBox, cardBox, pinBox, hex))
                return;
            throw new InvalidOperationException(
                "Theatre zoom hint is far from the selected pin locale=" + locale +
                " map=" + mapId +
                " distance=" + distance.ToString("0") + ".");
        }

        private static void RejectZoomHintOffDistrictEdges(GameObject root, Transform regions, string[] mapIds, string locale)
        {
            if (root == null || regions == null || mapIds == null)
                return;
            Transform hintNode = FindNamed(root.transform, "ZoomHint");
            if (hintNode is not RectTransform hint || !hint.gameObject.activeInHierarchy)
                return;
            Rect hintBounds = WorldBounds(hint);
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform outlineNode = FindNamed(regions, "ZoneOutline_" + mapIds[i]);
                if (outlineNode is not RectTransform outline)
                    continue;
                Vector2[] shape = OutlineWorldPoints(outline);
                for (int p = 0; p < shape.Length; p++)
                {
                    Vector2 a = shape[p];
                    Vector2 b = shape[(p + 1) % shape.Length];
                    if (!SegmentHitsRect(a, b, hintBounds))
                        continue;
                    throw new InvalidOperationException(
                        "Theatre district edge crosses the zoom hint locale=" + locale +
                        " district=" + mapIds[i] + ".");
                }
            }
        }

        private static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect rect)
        {
            if (rect.Contains(a) || rect.Contains(b))
                return true;
            Vector2 c0 = new Vector2(rect.xMin, rect.yMin);
            Vector2 c1 = new Vector2(rect.xMax, rect.yMin);
            Vector2 c2 = new Vector2(rect.xMax, rect.yMax);
            Vector2 c3 = new Vector2(rect.xMin, rect.yMax);
            return SegmentsCross(a, b, c0, c1) || SegmentsCross(a, b, c1, c2)
                || SegmentsCross(a, b, c2, c3) || SegmentsCross(a, b, c3, c0);
        }

        private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float ab = Orient(a, b, c);
            float ab2 = Orient(a, b, d);
            float cd = Orient(c, d, a);
            float cd2 = Orient(c, d, b);
            return ab * ab2 < 0f && cd * cd2 < 0f;
        }

        private static float Orient(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        private static void RejectCueClearOfPins(Transform regions, RectTransform cue, string locale)
        {
            if (regions == null || cue == null)
                return;
            Transform[] nodes = regions.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                if (node == null || !node.name.StartsWith("ZonePin_", StringComparison.Ordinal) || !node.gameObject.activeInHierarchy)
                    continue;
                var pin = node as RectTransform;
                if (pin == null || !WorldOverlap(cue, pin, 0f))
                    continue;
                throw new InvalidOperationException(
                    "Theatre zoom hint intersects a pin locale=" + locale + " pin=" + pin.name + ".");
            }
        }

        private static void RejectZonePolygon(RectTransform outline, RectTransform clip, string mapId, string locale)
        {
            V3PolygonGraphic polygon = outline != null ? outline.GetComponent<V3PolygonGraphic>() : null;
            if (polygon == null || polygon.PointCount < 6 || polygon.FilledArea < 1200f)
            {
                throw new InvalidOperationException(
                    "Theatre polygon for " + mapId + " has no visible area locale=" + locale +
                    " points=" + (polygon != null ? polygon.PointCount.ToString() : "0") +
                    " area=" + (polygon != null ? polygon.FilledArea.ToString("0") : "0") + ".");
            }

            if (polygon.FilledArea < 40000f)
            {
                throw new InvalidOperationException(
                    "Theatre polygon for " + mapId + " is still a small hex locale=" + locale +
                    " area=" + polygon.FilledArea.ToString("0") + ". Expected a large district region.");
            }

            Color tint = polygon.color;
            float maxChannel = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b));
            float minChannel = Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b));
            float saturation = maxChannel <= 0.001f ? 0f : (maxChannel - minChannel) / maxChannel;
            float luminance = tint.r * 0.299f + tint.g * 0.587f + tint.b * 0.114f;
            if (saturation < 0.45f || luminance < 0.42f || luminance > 0.86f)
            {
                throw new InvalidOperationException(
                    "Theatre polygon tint for " + mapId + " is flat locale=" + locale +
                    " sat=" + saturation.ToString("0.00") +
                    " lum=" + luminance.ToString("0.00") +
                    ". Expected a deep saturated tint.");
            }

            for (int i = 0; i < outline.childCount; i++)
            {
                Transform child = outline.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy || child is not RectTransform edge)
                    continue;
                bool outlineStroke = child.name == "HexEdge" || child.name == "HexCap" || child.name == "HexCell";
                if (!outlineStroke)
                    continue;
                Image ink = edge.GetComponent<Image>();
                if (ink == null || !ink.enabled)
                    continue;
                float thickness = Mathf.Max(edge.sizeDelta.x, edge.sizeDelta.y);
                float strokeMax = Mathf.Max(ink.color.r, Mathf.Max(ink.color.g, ink.color.b));
                float strokeMin = Mathf.Min(ink.color.r, Mathf.Min(ink.color.g, ink.color.b));
                float strokeSaturation = strokeMax <= 0.001f ? 0f : (strokeMax - strokeMin) / strokeMax;
                if (thickness >= 3f && ink.color.a >= 0.75f && strokeSaturation >= 0.55f)
                {
                    throw new InvalidOperationException(
                        "Theatre polygon for " + mapId + " still draws a saturated hard hex outline locale=" + locale +
                        " stroke=" + child.name +
                        " sat=" + strokeSaturation.ToString("0.00") +
                        " alpha=" + ink.color.a.ToString("0.00") + ".");
                }
            }

            if (clip == null)
                return;
            Rect clipRect = clip.rect;
            Vector3[] corners = new Vector3[4];
            outline.GetWorldCorners(corners);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = clip.InverseTransformPoint(corners[i]);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }

            float width = Mathf.Min(maxX, clipRect.xMax) - Mathf.Max(minX, clipRect.xMin);
            float height = Mathf.Min(maxY, clipRect.yMax) - Mathf.Max(minY, clipRect.yMin);
            if (width <= 1f || height <= 1f)
            {
                throw new InvalidOperationException(
                    "Theatre polygon for " + mapId + " is outside the map locale=" + locale + ".");
            }
        }

        private static void RejectZoneCardSpacing(RectTransform[] cards, RectTransform clip, string locale)
        {
            if (cards == null)
                return;
            const float gap = 12f;
            const float inset = 12f;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                    continue;
                if (clip != null)
                {
                    Vector3[] corners = new Vector3[4];
                    cards[i].GetWorldCorners(corners);
                    Rect clipRect = clip.rect;
                    clipRect.xMin += inset;
                    clipRect.xMax -= inset;
                    clipRect.yMin += inset;
                    clipRect.yMax -= inset;
                    for (int c = 0; c < corners.Length; c++)
                    {
                        Vector3 local = clip.InverseTransformPoint(corners[c]);
                        if (local.x < clipRect.xMin || local.x > clipRect.xMax || local.y < clipRect.yMin || local.y > clipRect.yMax)
                        {
                            throw new InvalidOperationException(
                                "Theatre zone card is closer than 12px to the map edge locale=" + locale +
                                " card=" + cards[i].name +
                                " local=" + local.x.ToString("0.0") + "," + local.y.ToString("0.0"));
                        }
                    }
                }

                Rect left = WorldBounds(cards[i]);
                for (int j = i + 1; j < cards.Length; j++)
                {
                    if (cards[j] == null)
                        continue;
                    Rect right = WorldBounds(cards[j]);
                    float dx = SeparatingGap(left.xMin, left.xMax, right.xMin, right.xMax);
                    float dy = SeparatingGap(left.yMin, left.yMax, right.yMin, right.yMax);
                    float clearance = dx >= 0f && dy >= 0f
                        ? Mathf.Min(dx, dy)
                        : dx >= 0f ? dx : dy;
                    if (clearance >= gap)
                        continue;
                    throw new InvalidOperationException(
                        "Theatre zone cards are closer than 12px locale=" + locale +
                        " " + cards[i].name + " " + cards[j].name +
                        " gap=" + clearance.ToString("0.0") + ".");
                }
            }
        }

        private static float SeparatingGap(float aMin, float aMax, float bMin, float bMax)
        {
            if (aMax <= bMin)
                return bMin - aMax;
            if (bMax <= aMin)
                return aMin - bMax;
            return -1f;
        }

        private static RectTransform RequireTheaterOverlay(
            Transform regions,
            RectTransform clip,
            string name,
            string kind,
            string mapId,
            string locale)
        {
            Transform node = FindNamed(regions, name);
            if (node == null || !node.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Theatre " + kind + " is missing for " + mapId + " locale=" + locale + ".");
            Graphic[] graphics = node.GetComponentsInChildren<Graphic>(true);
            bool shown = false;
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy && graphic.color.a > 0.05f)
                {
                    shown = true;
                    break;
                }
            }

            if (!shown)
                throw new InvalidOperationException("Theatre " + kind + " for " + mapId + " has no visible graphic locale=" + locale + ".");
            var rect = node as RectTransform;
            if (rect == null)
                throw new InvalidOperationException("Theatre " + kind + " for " + mapId + " has no rect locale=" + locale + ".");
            const float slop = 2f;
            const float inset = 12f;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Rect clipRect = clip.rect;
            clipRect.xMin += inset;
            clipRect.xMax -= inset;
            clipRect.yMin += inset;
            clipRect.yMax -= inset;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = clip.InverseTransformPoint(corners[i]);
                bool outside = local.x < clipRect.xMin - slop || local.x > clipRect.xMax + slop ||
                    local.y < clipRect.yMin - slop || local.y > clipRect.yMax + slop;
                if (!outside)
                    continue;
                NoteTheatre(
                    "Theatre " + kind + " for " + mapId + " is outside the map locale=" + locale +
                    " local=" + local.x.ToString("0.0") + "," + local.y.ToString("0.0"));
                break;
            }

            return rect;
        }

        private static void RejectZoneCardContent(
            RectTransform card,
            SkirmishTheaterPinState[] pins,
            string mapId,
            string locale)
        {
            Vector2 size = card.sizeDelta;
            if (size.x < TheatreSeatGeometry.PortraitMinWidth || size.x > TheatreSeatGeometry.PortraitMaxWidth + TheatreSeatGeometry.PortraitMaxSlop
                || size.y < TheatreSeatGeometry.PortraitMinHeight || size.y > TheatreSeatGeometry.PortraitMaxHeight + TheatreSeatGeometry.PortraitMaxSlop)
            {
                throw new InvalidOperationException(
                    "Theatre zone card for " + mapId + " is " + size.x.ToString("0") + "x" + size.y.ToString("0") +
                    " locale=" + locale + ". Expected a portrait tile about 112-160 by 140-212.");
            }

            TMP_Text name = card.Find("ZoneName")?.GetComponent<TMP_Text>();
            if (name == null)
                throw new InvalidOperationException("Theatre zone card has no name for " + mapId + " locale=" + locale + ".");
            RejectReadableLine(name, "zone name", mapId, locale);

            RawImage thumb = card.Find("Thumbnail")?.GetComponent<RawImage>()
                ?? card.Find("ThumbFrame/Thumbnail")?.GetComponent<RawImage>();
            if (thumb == null || thumb.texture == null || thumb.color.r < 0.95f || thumb.color.g < 0.95f || thumb.color.b < 0.95f)
                throw new InvalidOperationException("Theatre zone thumbnail is missing or tinted for " + mapId + " locale=" + locale + ".");
            if (thumb.texture.name == "SkirmishLibraryPlaceholder")
            {
                throw new InvalidOperationException(
                    "Theatre zone thumbnail is the placeholder for " + mapId + " locale=" + locale + ".");
            }

            if (thumb.color.a < 0.99f)
            {
                throw new InvalidOperationException(
                    "Theatre zone plate is translucent for " + mapId + " locale=" + locale +
                    " alpha=" + thumb.color.a.ToString("0.00") + ".");
            }

            Image art = card.Find("ThumbFrame/CardArt")?.GetComponent<Image>()
                ?? card.Find("CardArt")?.GetComponent<Image>();
            string spriteName = art != null && art.sprite != null ? art.sprite.name : string.Empty;
            if (!IsDistrictPlateSpriteName(spriteName, mapId))
            {
                throw new InvalidOperationException(
                    "Theatre card art for " + mapId + " is not the district plate locale=" + locale +
                    " sprite=" + (string.IsNullOrEmpty(spriteName) ? "none" : spriteName) + ".");
            }

            if (art != null && (art.color.a < 0.99f || art.color.r < 0.95f || art.color.g < 0.95f || art.color.b < 0.95f))
            {
                throw new InvalidOperationException(
                    "Theatre card art for " + mapId + " is tinted locale=" + locale +
                    " tint=" + art.color.r.ToString("0.00") + "," + art.color.g.ToString("0.00") + "," + art.color.b.ToString("0.00") +
                    " alpha=" + art.color.a.ToString("0.00") + ".");
            }

            RejectBlankCardArt(card, art, thumb, locale, mapId);
            if (name.rectTransform != null && !Contains(WorldBounds(card), WorldBounds(name.rectTransform), 1f))
            {
                throw new InvalidOperationException(
                    "Theatre zone name draws outside its card locale=" + locale +
                    " card=ZoneCard_" + mapId + ".");
            }

            float nameShare = QuickCustomScreenView.NameStripShare(card);
            if (nameShare > 0.25f)
            {
                throw new InvalidOperationException(
                    "Theatre name strip covers more than 25% of the card locale=" + locale +
                    " card=ZoneCard_" + mapId +
                    " share=" + nameShare.ToString("0.00") + ".");
            }

            float contrast = QuickCustomScreenView.PlateLuminanceContrast(mapId);
            if (contrast < 0.07f)
            {
                throw new InvalidOperationException(
                    "Theatre plate luminance contrast is under the floor locale=" + locale +
                    " map=" + mapId +
                    " contrast=" + contrast.ToString("0.000") + ".");
            }

            Transform disc = card.Find("RingDisc");
            if (disc != null && disc.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    "Theatre card for " + mapId + " still draws the bullseye disc locale=" + locale + ".");
            }

            Image fill = card.Find("ProgressRing/ProgressFill")?.GetComponent<Image>();
            TMP_Text count = card.Find("ZoneCount")?.GetComponent<TMP_Text>()
                ?? card.Find("ProgressRing/ZoneCount")?.GetComponent<TMP_Text>();
            if (fill == null || count == null)
                throw new InvalidOperationException("Theatre zone card is missing its progress arc for " + mapId + " locale=" + locale + ".");
            if (fill.type != Image.Type.Filled || fill.fillMethod != Image.FillMethod.Radial360)
                throw new InvalidOperationException("Theatre progress for " + mapId + " is not a circular arc locale=" + locale + ".");
            RectTransform progress = card.Find("ProgressRing") as RectTransform;
            float share = ProgressAreaShare(card, progress);
            if (share > 0.25f && ProgressMarkIsCentred(card, progress))
            {
                throw new InvalidOperationException(
                    "Theatre progress arc for " + mapId + " is a centred ring covering " +
                    share.ToString("0.00") + " of the card locale=" + locale + ".");
            }

            SkirmishTheaterPinState pin = default;
            bool found = false;
            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    if (!string.Equals(pins[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    pin = pins[i];
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new InvalidOperationException("Theatre pin data is missing for " + mapId + " locale=" + locale + ".");

            bool pending = pin.PublishedCount <= 0;
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            string expected = pending
                ? SkirmishBattleLibraryArt.ComingSoon(fa)
                : pin.WonCount + "/" + pin.PublishedCount;
            count.ForceMeshUpdate();
            bool matches = SkirmishBattleLibraryArt.SameRenderedText(count.text, expected);
            if (!matches)
            {
                throw new InvalidOperationException(
                    "Theatre zone count for " + mapId + " is '" + count.text + "' locale=" + locale +
                    ". Expected " + expected + " (medals won / playable scenarios).");
            }

            RejectReadableLine(count, "zone count", mapId, locale);
            RejectStatusInsideCard(card, count, locale, mapId);
            RememberZoneCount(locale, mapId, expected, "zone card");
            if (pending)
            {
                if (fill.enabled || fill.fillAmount > 0.001f)
                {
                    throw new InvalidOperationException(
                        "Theatre progress arc for " + mapId + " is filled locale=" + locale +
                        " but the zone has no playable scenarios.");
                }

                return;
            }

            float ratio = pin.WonCount / (float)pin.PublishedCount;
            if (Mathf.Abs(fill.fillAmount - Mathf.Clamp01(ratio)) > 0.02f)
            {
                throw new InvalidOperationException(
                    "Theatre progress arc for " + mapId + " is " + fill.fillAmount.ToString("0.00") +
                    " locale=" + locale + ". Expected " + ratio.ToString("0.00") + ".");
            }
        }

        /// <summary>
        /// Badge area over the card. A centred mark above a quarter of the card is the
        /// old ring; a corner badge stays under that share.
        /// </summary>
        private static float ProgressAreaShare(RectTransform card, RectTransform ring)
        {
            if (card == null || ring == null)
                return 0f;
            float cardArea = card.sizeDelta.x * card.sizeDelta.y;
            if (cardArea < 1f)
                return 0f;
            return Mathf.Max(0f, ring.sizeDelta.x) * Mathf.Max(0f, ring.sizeDelta.y) / cardArea;
        }

        /// <summary>
        /// True when the progress centre sits in the middle half of the card on both axes.
        /// A corner badge is outside that box.
        /// </summary>
        private static bool ProgressMarkIsCentred(RectTransform card, RectTransform ring)
        {
            if (card == null || ring == null)
                return false;
            float cardW = card.sizeDelta.x;
            float cardH = card.sizeDelta.y;
            if (cardW < 1f || cardH < 1f)
                return false;
            float centreX = ring.anchoredPosition.x + ring.sizeDelta.x * 0.5f;
            float centreY = -ring.anchoredPosition.y + ring.sizeDelta.y * 0.5f;
            return centreX >= cardW * 0.25f && centreX <= cardW * 0.75f
                && centreY >= cardH * 0.25f && centreY <= cardH * 0.75f;
        }

        private static bool IsDistrictPlateSpriteName(string spriteName, string mapId)
        {
            if (string.IsNullOrEmpty(spriteName) || string.IsNullOrEmpty(mapId))
                return false;
            if (spriteName.IndexOf("placeholder", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (spriteName.IndexOf("bullseye", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (spriteName.IndexOf("disc", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return spriteName.IndexOf("skirmish.plate." + mapId, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// The painting has to occupy the middle of the card. A title parked on the
        /// bottom leaves an 8px art rect under the tile, and the card reads as a
        /// blank black frame even when the plate sprite is assigned.
        /// </summary>
        private static void RejectBlankCardArt(RectTransform card, Image art, RawImage thumb, string locale, string mapId)
        {
            RectTransform artRect = art != null ? art.rectTransform : null;
            float artH = 0f;
            if (artRect != null)
                artH = artRect.rect.height > 1f ? artRect.rect.height : artRect.sizeDelta.y;
            bool shown = art != null && art.enabled && art.gameObject.activeInHierarchy && art.sprite != null && artRect != null;
            Rect cardBounds = WorldBounds(card);
            Rect artBounds = artRect != null ? WorldBounds(artRect) : default;
            if (!shown || artH < 36f || artBounds.height < 36f || !Contains(cardBounds, artBounds, 2f))
            {
                throw new InvalidOperationException(
                    "Theatre card art area is blank locale=" + locale +
                    " card=ZoneCard_" + mapId +
                    " artH=" + artH.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".");
            }

            RectTransform thumbRect = thumb != null ? thumb.rectTransform : null;
            float thumbH = 0f;
            if (thumbRect != null)
                thumbH = thumbRect.rect.height > 1f ? thumbRect.rect.height : thumbRect.sizeDelta.y;
            Rect thumbBounds = thumbRect != null ? WorldBounds(thumbRect) : default;
            if (thumbRect == null || !thumb.enabled || thumbH < 36f || thumbBounds.height < 36f || !Contains(cardBounds, thumbBounds, 2f))
            {
                throw new InvalidOperationException(
                    "Theatre card art area is blank locale=" + locale +
                    " card=ZoneCard_" + mapId +
                    " thumbH=" + thumbH.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".");
            }
        }

        private static void RejectStatusInsideCard(RectTransform card, TMP_Text count, string locale, string mapId)
        {
            if (card == null)
                return;
            Rect plate = WorldBounds(card);
            const float slop = 1f;
            Transform ring = card.Find("ProgressRing");
            if (ring is RectTransform ringRect && !Contains(plate, WorldBounds(ringRect), slop))
            {
                throw new InvalidOperationException(
                    "Theatre zone status draws outside its card locale=" + locale +
                    " card=ZoneCard_" + mapId + ".");
            }

            if (count != null && count.rectTransform != null && !Contains(plate, WorldBounds(count.rectTransform), slop))
            {
                throw new InvalidOperationException(
                    "Theatre zone status draws outside its card locale=" + locale +
                    " card=ZoneCard_" + mapId + ".");
            }
        }

        private static bool Contains(Rect outer, Rect inner, float slop)
        {
            return inner.xMin >= outer.xMin - slop && inner.xMax <= outer.xMax + slop
                && inner.yMin >= outer.yMin - slop && inner.yMax <= outer.yMax + slop;
        }

        private static void RejectSelectedZoneMatchesGridRows(
            QuickCustomScreenView view,
            SkirmishTheaterPinState[] pins,
            string mapId,
            string locale)
        {
            if (view == null || view.LibraryViewModel == null || pins == null)
                return;
            SkirmishBattlefieldState field = view.LibraryViewModel.Battlefield;
            if (!string.Equals(field.MapId, mapId, StringComparison.OrdinalIgnoreCase) || field.Tabs == null)
                return;
            int won = 0;
            int published = 0;
            for (int i = 0; i < field.Tabs.Length; i++)
            {
                won += field.Tabs[i].WonCount;
                published += field.Tabs[i].PublishedCount;
            }

            SkirmishTheaterPinState pin = default;
            for (int i = 0; i < pins.Length; i++)
            {
                if (!string.Equals(pins[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                pin = pins[i];
                break;
            }

            if (won != pin.WonCount || published != pin.PublishedCount)
            {
                throw new InvalidOperationException(
                    "Theatre zone count for " + mapId + " is " + pin.WonCount + "/" + pin.PublishedCount +
                    " locale=" + locale + " but its grid rows sum to " + won + "/" + published + ".");
            }
        }

        private static void RejectZoneProgressMatchesCard(GameObject root, QuickCustomScreenView view, string locale)
        {
            if (view == null || view.LibraryViewModel == null)
                return;
            Transform track = FindNamed(root.transform, "BattlesWonTrack");
            if (track == null || !track.gameObject.activeInHierarchy)
                return;
            TMP_Text count = track.Find("Count")?.GetComponent<TMP_Text>();
            if (count == null)
                return;
            string mapId = view.LibraryViewModel.Theater.SelectedMapId ?? string.Empty;
            string raw = count.text ?? string.Empty;
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            string shown = SkirmishBattleLibraryArt.IsRenderedComingSoon(raw)
                ? SkirmishBattleLibraryArt.ComingSoon(fa)
                : StripBidiMarks(raw).Trim().Replace(" ", string.Empty);
            RememberZoneCount(locale, mapId, shown, "zone progress");
        }

        private static void RejectMirroredTheatreChrome(GameObject root, string locale)
        {
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            Transform strip = FindNamed(root.transform, "OverallProgressStrip");
            if (strip != null && strip.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    "Theatre still shows OverallProgressStrip locale=" + locale +
                    ". The battles-won strip is removed.");
            }

            if (FindNamed(root.transform, "MapPreviewClip") is RectTransform plate
                && FindNamed(root.transform, "TheatreDetailPanel") is RectTransform panel
                && panel.gameObject.activeInHierarchy)
            {
                float panelX = WorldBounds(panel).center.x;
                float plateX = WorldBounds(plate).center.x;
                if (fa ? panelX >= plateX : panelX <= plateX)
                {
                    throw new InvalidOperationException(
                        "Theatre reserve is on the leading edge locale=" + locale + ".");
                }

                if (FindNamed(root.transform, "ZoomCue") is RectTransform cue && cue.gameObject.activeInHierarchy)
                {
                    Rect cueBounds = WorldBounds(cue);
                    Rect panelBounds = WorldBounds(panel);
                    bool inOpen = fa
                        ? cueBounds.xMin + 1f >= panelBounds.xMax
                        : cueBounds.xMax - 1f <= panelBounds.xMin;
                    if (!inOpen)
                    {
                        throw new InvalidOperationException(
                            "Theatre zoom cue is outside the mirrored open viewport locale=" + locale + ".");
                    }
                }
            }
        }

        private static void RejectReserveOverlapsDetailSide(GameObject root, QuickCustomScreenView view, string locale)
        {
            if (view == null || !TheatreSeatGeometry.TheatreDetailReserved)
                return;
            if (!view.TryCopyTheatreDetailRect(out Rect detail))
                return;
            Rect reserve = view.SeatingReserveRect;
            if (reserve.width <= 1f)
            {
                throw new InvalidOperationException(
                    "Theatre reserve rect is empty while the detail column is reserved locale=" + locale + ".");
            }

            var clip = FindNamed(root.transform, "MapPreviewClip") as RectTransform;
            float plateWidth = clip != null && clip.rect.width > 2f
                ? clip.rect.width
                : Mathf.Max(reserve.xMax, detail.xMax) + 10f;
            bool reserveLeft = TheatreSeatGeometry.DetailOccupiesLeft(plateWidth, reserve.x, reserve.width);
            bool detailLeft = TheatreSeatGeometry.DetailOccupiesLeft(plateWidth, detail.x, detail.width);
            float overlap = Mathf.Min(reserve.xMax, detail.xMax) - Mathf.Max(reserve.xMin, detail.xMin);
            if (detail.width <= 1f || reserveLeft != detailLeft || overlap <= 1f)
            {
                throw new InvalidOperationException(
                    "Theatre reserve rect does not overlap TheatreDetailRect locale=" + locale +
                    " reserveX=" + reserve.x.ToString("0.0") +
                    " reserveW=" + reserve.width.ToString("0.0") +
                    " detailX=" + detail.x.ToString("0.0") +
                    " detailW=" + detail.width.ToString("0.0") + ".");
            }
        }

        private static void RejectStripMatchesZoneTotals(GameObject root, QuickCustomScreenView view, string locale)
        {
            if (view == null || view.LibraryViewModel == null)
                return;
            SkirmishTheaterState theater = view.LibraryViewModel.Theater;
            SkirmishTheaterPinState[] pins = theater.Pins;
            int won = 0;
            int published = 0;
            if (pins != null)
            {
                for (int i = 0; i < pins.Length; i++)
                {
                    won += pins[i].WonCount;
                    published += pins[i].PublishedCount;
                }
            }

            if (won != theater.OverallWonCount || published != theater.OverallPublishedCount)
            {
                throw new InvalidOperationException(
                    "Battles-won strip is " + theater.OverallWonCount + " / " + theater.OverallPublishedCount +
                    " locale=" + locale + " but the zone playable totals sum to " + won + " / " + published + ".");
            }

            Transform strip = FindNamed(root.transform, "OverallProgressStrip");
            if (strip != null && strip.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    "Battles-won strip is still in theatre locale=" + locale +
                    ". Medal counts belong on TheatreSummary.");
            }
        }

        private static void RejectVisibleGridRowTotals(GameObject root, string locale)
        {
            QuickCustomScreenView view = root != null ? root.GetComponent<QuickCustomScreenView>() : null;
            if (view == null || view.LibraryViewModel == null)
                return;
            string mapId = view.LibraryViewModel.Battlefield.MapId ?? string.Empty;
            Transform grid = FindNamed(root.transform, "LibraryGrid");
            if (grid == null)
                return;
            int won = 0;
            int published = 0;
            int rows = 0;
            TMP_Text[] texts = grid.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.name.StartsWith("RowCount_", StringComparison.Ordinal) || !text.gameObject.activeInHierarchy)
                    continue;
                if (!TryParseCount(text.text, out int rowWon, out int rowPublished))
                {
                    throw new InvalidOperationException(
                        "Grid row count is not won/playable locale=" + locale + " text=" + text.text + ".");
                }

                won += rowWon;
                published += rowPublished;
                rows++;
            }

            if (rows == 0)
                return;
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            string shown = published <= 0
                ? SkirmishBattleLibraryArt.ComingSoon(fa)
                : won + "/" + published;
            RememberZoneCount(locale, mapId, shown, "grid rows");
        }

        private static void RememberZoneCount(string locale, string mapId, string count, string source)
        {
            string key = (locale ?? string.Empty) + "|" + (mapId ?? string.Empty).ToUpperInvariant();
            if (ZonePlayableCounts.TryGetValue(key, out string prior) && prior != count)
            {
                throw new InvalidOperationException(
                    "Zone playable count for " + mapId + " is " + count + " from " + source +
                    " locale=" + locale + " but another surface shows " + prior + ".");
            }

            ZonePlayableCounts[key] = count;
        }

        private static bool TryParseCount(string text, out int won, out int published)
        {
            won = 0;
            published = 0;
            string cleaned = StripBidiMarks(text).Trim();
            if (SkirmishBattleLibraryArt.IsRenderedComingSoon(cleaned))
                return true;
            cleaned = cleaned.Replace(" ", string.Empty);
            int slash = cleaned.IndexOf('/');
            if (slash <= 0 || slash >= cleaned.Length - 1)
                return false;
            return int.TryParse(cleaned.Substring(0, slash), out won) &&
                int.TryParse(cleaned.Substring(slash + 1), out published);
        }

        private static bool ZoneNameCoversSource(TMP_Text text, string kind)
        {
            if (text == null || kind != "zone name")
                return true;
            string source = (text.text ?? string.Empty).Replace("\n", string.Empty);
            int expected = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (char.IsLetterOrDigit(source[i]))
                    expected++;
            }

            TMP_TextInfo info = text.textInfo;
            if (info == null || info.characterInfo == null)
                return expected == 0;
            int visible = 0;
            int count = Mathf.Min(info.characterCount, info.characterInfo.Length);
            for (int i = 0; i < count; i++)
            {
                if (!info.characterInfo[i].isVisible)
                    continue;
                char value = info.characterInfo[i].character;
                if (char.IsLetterOrDigit(value))
                    visible++;
            }

            return visible >= expected;
        }

        private static float LineWidth(TMP_Text text)
        {
            if (text == null)
                return 0f;
            TMP_TextInfo info = text.textInfo;
            if (info != null && info.lineCount > 1)
            {
                float widest = 0f;
                int lines = Mathf.Min(info.lineCount, info.lineInfo.Length);
                for (int i = 0; i < lines; i++)
                    widest = Mathf.Max(widest, info.lineInfo[i].length);
                if (widest > 0.5f)
                    return widest;
            }

            float preferred = text.preferredWidth;
            if (preferred < 0.5f && !string.IsNullOrEmpty(text.text))
            {
                Vector2 values = text.GetPreferredValues(text.text);
                preferred = Mathf.Max(preferred, values.x);
            }

            return preferred;
        }

        private static void RejectStripMedalOverlap(GameObject root, string locale)
        {
            if (root == null)
                return;
            Transform strip = FindNamed(root.transform, "OverallProgressStrip");
            if (strip == null || !strip.gameObject.activeInHierarchy)
                return;
            for (int i = 0; i < 4; i++)
            {
                Transform slot = strip.Find("Medal" + i);
                if (slot == null)
                    continue;
                var caption = slot.Find("Caption") as RectTransform;
                var count = slot.Find("Count") as RectTransform;
                if (caption == null || count == null)
                    continue;
                Rect label = WorldBounds(caption);
                Rect tally = WorldBounds(count);
                float dy = SeparatingGap(label.yMin, label.yMax, tally.yMin, tally.yMax);
                if (dy >= 0f)
                    continue;
                float gap = SeparatingGap(label.xMin, label.xMax, tally.xMin, tally.xMax);
                if (gap >= TheatreSeatGeometry.MedalGapMinimum)
                    continue;
                string tier = caption.GetComponent<TMP_Text>() != null ? caption.GetComponent<TMP_Text>().text : "Medal" + i;
                throw new InvalidOperationException(
                    "Theatre medal label overlaps its count locale=" + locale +
                    " tier=" + tier +
                    " gap=" + gap.ToString("0.0") + ".");
            }
        }

        private static void RejectReadableLine(TMP_Text text, string kind, string mapId, string locale)
        {
            text.ForceMeshUpdate();
            if (text.isTextTruncated || text.isTextOverflowing || !ZoneNameCoversSource(text, kind))
            {
                throw new InvalidOperationException(
                    "Theatre " + kind + " is truncated for " + mapId + " locale=" + locale + " text=" + text.text + ".");
            }

            if (text.overflowMode == TextOverflowModes.Ellipsis || text.overflowMode == TextOverflowModes.Truncate)
            {
                throw new InvalidOperationException(
                    "Theatre " + kind + " uses a clipping overflow for " + mapId + " locale=" + locale + ".");
            }

            float width = text.rectTransform != null ? text.rectTransform.rect.width : 0f;
            float preferred = LineWidth(text);
            if (preferred > width + 2f)
            {
                throw new InvalidOperationException(
                    "Theatre " + kind + " draws past its box for " + mapId + " locale=" + locale +
                    " preferred=" + preferred.ToString("0.0") + " width=" + width.ToString("0.0") +
                    " text=" + text.text + ".");
            }

            if (BreaksMidWord(text))
            {
                throw new InvalidOperationException(
                    "Theatre " + kind + " breaks a word for " + mapId + " locale=" + locale + " text=" + text.text + ".");
            }
        }

        private static bool BreaksMidWord(TMP_Text text)
        {
            TMP_TextInfo info = text.textInfo;
            if (info == null || info.lineCount <= 1)
                return false;
            string source = text.text ?? string.Empty;
            for (int line = 0; line < info.lineCount - 1; line++)
            {
                TMP_LineInfo row = info.lineInfo[line];
                TMP_LineInfo next = info.lineInfo[line + 1];
                if (row.characterCount <= 0 || next.characterCount <= 0)
                    continue;
                int last = row.lastCharacterIndex;
                int firstNext = next.firstCharacterIndex;
                if (last < 0 || firstNext < 0 || last >= info.characterCount || firstNext >= info.characterCount)
                    continue;
                int leftIndex = info.characterInfo[last].index;
                int rightIndex = info.characterInfo[firstNext].index;
                if (leftIndex < 0 || rightIndex <= leftIndex || leftIndex >= source.Length || rightIndex >= source.Length)
                    continue;
                if (IsWordChar(source[leftIndex]) && IsWordChar(source[rightIndex]))
                    return true;
            }

            return false;
        }

        private static bool IsWordChar(char value)
        {
            if (char.IsWhiteSpace(value) || value == '-' || value == '/' || value == '·')
                return false;
            return char.IsLetterOrDigit(value);
        }

        private static string StripBidiMarks(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value
                .Replace("\u200e", string.Empty)
                .Replace("\u200f", string.Empty)
                .Replace("\u202a", string.Empty)
                .Replace("\u202b", string.Empty)
                .Replace("\u202c", string.Empty)
                .Replace("\u061c", string.Empty);
        }

        private static void RejectZoneLeader(Transform regions, string mapId, string locale)
        {
            Transform node = FindNamed(regions, "ZoneLeader_" + mapId);
            if (node == null || !node.gameObject.activeInHierarchy || node is not RectTransform rect)
            {
                throw new InvalidOperationException(
                    "Theatre leader is missing for " + mapId + " locale=" + locale + ".");
            }

            Image line = rect.GetComponent<Image>();
            if (line == null || !line.enabled || line.color.a <= 0.4f || rect.sizeDelta.x < 4f)
            {
                throw new InvalidOperationException(
                    "Theatre leader for " + mapId + " is not a visible stroke locale=" + locale +
                    " length=" + rect.sizeDelta.x.ToString("0.0") + ".");
            }
        }

        private static void RejectDistrictHalo(
            Transform regions,
            string[] mapIds,
            string locale)
        {
            if (regions == null || mapIds == null)
                return;
            for (int h = 0; h < mapIds.Length; h++)
            {
                Transform outlineNode = FindNamed(regions, "ZoneOutline_" + mapIds[h]);
                if (outlineNode is not RectTransform outline)
                    continue;
                Transform haloNode = outline.Find("ZoneHalo");
                Image haloImage = haloNode != null ? haloNode.GetComponent<Image>() : null;
                if (haloNode is not RectTransform halo || haloImage == null || !haloImage.enabled || haloImage.sprite == null || haloImage.color.a < 0.45f)
                {
                    throw new InvalidOperationException(
                        "Theatre district glow for " + mapIds[h] + " is missing its soft halo locale=" + locale + ".");
                }

                float left = halo.anchoredPosition.x;
                float top = -halo.anchoredPosition.y;
                float right = left + halo.sizeDelta.x;
                float bottom = top + halo.sizeDelta.y;
                float width = outline.rect.width > 2f ? outline.rect.width : outline.sizeDelta.x;
                float height = outline.rect.height > 2f ? outline.rect.height : outline.sizeDelta.y;
                // The feather has to extend at least its own width past the region.
                // TerritoryFeather is 10, which is stricter than the old 6px test.
                float feather = TheatreSeatGeometry.TerritoryFeather;
                if (feather < 10f)
                    feather = 10f;
                if (left > -feather || top > -feather || right < width + feather || bottom < height + feather)
                {
                    throw new InvalidOperationException(
                        "Theatre district glow for " + mapIds[h] + " does not extend past the region locale=" + locale +
                        " halo=" + left.ToString("0") + "," + top.ToString("0") + " " + right.ToString("0") + "," + bottom.ToString("0") +
                        " outline=" + width.ToString("0") + "x" + height.ToString("0") + ".");
                }
            }

        }

        private static void RejectDistrictHexAspect(Transform regions, string[] mapIds, string locale)
        {
            if (regions == null || mapIds == null)
                return;
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform node = FindNamed(regions, "ZoneOutline_" + mapIds[i]);
                if (node is not RectTransform outline)
                    continue;
                V3PolygonGraphic polygon = outline.GetComponent<V3PolygonGraphic>();
                if (polygon == null || polygon.PointCount < 3)
                    continue;
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int p = 0; p < polygon.PointCount; p++)
                {
                    Vector2 local = polygon.LocalPoint(p);
                    minX = Mathf.Min(minX, local.x);
                    maxX = Mathf.Max(maxX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                }

                float height = Mathf.Max(1f, maxY - minY);
                float measured = (maxX - minX) / height;
                RectTransform frame = outline.parent as RectTransform;
                float frameW = 0f;
                float frameH = 0f;
                if (frame != null)
                {
                    frameW = frame.rect.width > 2f ? frame.rect.width : frame.sizeDelta.x;
                    frameH = frame.rect.height > 2f ? frame.rect.height : frame.sizeDelta.y;
                }

                if ((frameW < 2f || frameH < 2f) && regions is RectTransform plate)
                {
                    frameW = plate.rect.width > 2f ? plate.rect.width : plate.sizeDelta.x;
                    frameH = plate.rect.height > 2f ? plate.rect.height : plate.sizeDelta.y;
                }

                // The outline parent is the full clip. The authored ratio uses the open
                // viewport beside the reserve, matching CollectViolations.
                bool detailOpen = TheatreSeatGeometry.TheatreDetailReserved;
                bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
                Transform detailPanel = FindNamed(regions, "TheatreDetailPanel");
                if (detailPanel is RectTransform detailRect && detailRect.gameObject.activeInHierarchy && frame != null)
                {
                    TheatreSeatGeometry.Box detail = PlateBox(frame, detailRect);
                    if (detail.W > 1f)
                    {
                        detailOpen = true;
                        fa = detail.Center.X < frameW * 0.5f;
                    }
                }

                if (detailOpen && frameW > 2f && frameH > 2f)
                {
                    TheatreSeatGeometry.Box view = TheatreSeatGeometry.OpenViewport(frameW, frameH, fa, true);
                    if (view.W > 2f && view.H > 2f)
                    {
                        frameW = view.W;
                        frameH = view.H;
                    }
                }

                float expected = V35HexAspect(mapIds[i], frameW, frameH);
                if (expected < 0.2f)
                    continue;
                if (Mathf.Abs(measured / expected - 1f) <= 0.10f)
                    continue;
                throw new InvalidOperationException(
                    "Theatre district hex is stretched locale=" + locale +
                    " map=" + mapIds[i] +
                    " ratio=" + measured.ToString("0.00") +
                    " expected=" + expected.ToString("0.00") + ".");
            }
        }

        /// <summary>
        /// Width/height of the v35 flat hex for the frame passed in.
        /// That frame is the open viewport when the detail reserve is up.
        /// </summary>
        private static float V35HexAspect(string mapId, float frameWidth, float frameHeight)
        {
            if (frameWidth < 2f || frameHeight < 2f)
                return 0f;
            float aspect = frameWidth / frameHeight;
            float halfWidth;
            float halfHeight;
            float reference;
            if (aspect >= 2.4f)
            {
                reference = 1774f;
                switch (mapId)
                {
                    case "CC": halfWidth = 250f; halfHeight = 110f; break;
                    case "MP": halfWidth = 210f; halfHeight = 110f; break;
                    case "IB": halfWidth = 210f; halfHeight = 110f; break;
                    case "AP": halfWidth = 180f; halfHeight = 62f; break;
                    default: halfWidth = 250f; halfHeight = 110f; break;
                }
            }
            else if (aspect >= 1.9f)
            {
                reference = 1294f;
                switch (mapId)
                {
                    case "CC": halfWidth = 230f; halfHeight = 115f; break;
                    case "MP": halfWidth = 200f; halfHeight = 115f; break;
                    case "IB": halfWidth = 190f; halfHeight = 110f; break;
                    case "AP": halfWidth = 156f; halfHeight = 78f; break;
                    default: halfWidth = 250f; halfHeight = 120f; break;
                }
            }
            else
            {
                reference = 1039.6f;
                switch (mapId)
                {
                    case "CC": halfWidth = 200f; halfHeight = 105f; break;
                    case "MP": halfWidth = 170f; halfHeight = 100f; break;
                    case "IB": halfWidth = 165f; halfHeight = 100f; break;
                    case "AP": halfWidth = 155f; halfHeight = 100f; break;
                    default: halfWidth = 270f; halfHeight = 115f; break;
                }
            }

            float scaleX = frameWidth / reference;
            float scaleY = frameHeight / 620f;
            if (halfHeight * scaleY < 1f)
                return 0f;
            return (halfWidth * scaleX) / (halfHeight * scaleY);
        }

        private static void RejectDistrictsSeparate(Transform regions, string[] mapIds, string locale)
        {
            if (regions == null || mapIds == null)
                return;
            var shapes = new Vector2[mapIds.Length][];
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform node = FindNamed(regions, "ZoneOutline_" + mapIds[i]);
                shapes[i] = node is RectTransform outline ? OutlineWorldPoints(outline) : Array.Empty<Vector2>();
            }

            const float slop = 0.75f;
            for (int i = 0; i < shapes.Length; i++)
            {
                for (int j = i + 1; j < shapes.Length; j++)
                {
                    if (!PolygonsOverlap(shapes[i], shapes[j], slop))
                        continue;
                    throw new InvalidOperationException(
                        "Theatre districts overlap locale=" + locale +
                        " " + mapIds[i] + " " + mapIds[j] + ".");
                }
            }
        }

        private static void RejectDistrictPainting(Texture2D image, RectTransform canvas, GameObject root)
        {
            if (image == null || canvas == null || root == null)
                return;
            Transform regions = FindNamed(root.transform, "LibraryRegions");
            if (regions == null || !regions.gameObject.activeInHierarchy)
                return;
            Color32[] pixels = image.GetPixels32();
            int width = image.width;
            int height = image.height;
            if (pixels == null || pixels.Length < width * height)
                return;
            string locale = GameLocalization.IsRightToLeft ? "fa" : "en";
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            var blockers = new List<Rect>();
            CollectBlockers(regions, "ZoneCard_", blockers);
            CollectBlockers(regions, "ZonePin_", blockers);
            CollectBlockers(regions, "ZoomCue", blockers);
            Transform plate = FindNamed(regions, "CuePlate");
            if (plate is RectTransform plateRect && plateRect.gameObject.activeInHierarchy)
                blockers.Add(WorldBounds(plateRect));

            float worst = float.MaxValue;
            string worstId = mapIds.Length > 0 ? mapIds[0] : "zone";
            for (int i = 0; i < mapIds.Length; i++)
            {
                Transform node = FindNamed(regions, "ZoneOutline_" + mapIds[i]);
                if (node is not RectTransform outline)
                    continue;
                Vector2[] shape = OutlineWorldPoints(outline);
                float variance = InteriorLuminanceDeviation(pixels, width, height, canvas, shape, blockers, mapIds[i], locale);
                if (variance >= worst)
                    continue;
                worst = variance;
                worstId = mapIds[i];
            }

            const float floor = 8f;
            if (worst < floor)
            {
                throw new InvalidOperationException(
                    "Theatre district " + worstId + " hides the painted map locale=" + locale +
                    " variance=" + worst.ToString("0.0") + ". Expected the painting to stay visible inside the tint.");
            }
        }

        private static void RejectZoneNameContrast(Texture2D image, RectTransform canvas, GameObject root)
        {
            if (image == null || canvas == null || root == null)
                return;
            Transform regions = FindNamed(root.transform, "LibraryRegions");
            if (regions == null || !regions.gameObject.activeInHierarchy)
                return;
            Color32[] pixels = image.GetPixels32();
            int width = image.width;
            int height = image.height;
            if (pixels == null || pixels.Length < width * height)
                return;
            string locale = GameLocalization.IsRightToLeft ? "fa" : "en";
            const float minimum = 4.5f;
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (FindNamed(regions, "ZoneCard_" + mapIds[i]) is not RectTransform card || !card.gameObject.activeInHierarchy)
                    continue;
                if (card.Find("ZoneName") is not RectTransform nameRect || !nameRect.gameObject.activeInHierarchy)
                    continue;
                TMP_Text name = nameRect.GetComponent<TMP_Text>();
                if (name == null)
                    continue;
                Rect ink = NameInkWorldRect(name, nameRect);
                var ground = new List<float>();
                var inkLuma = new List<float>();
                SampleNameContrast(pixels, width, height, canvas, ink, ground, inkLuma);
                if (ground.Count < 8 || inkLuma.Count < 4)
                {
                    throw new InvalidOperationException(
                        "Theatre zone name contrast could not be sampled locale=" + locale +
                        " card=" + card.name + ".");
                }

                ground.Sort();
                inkLuma.Sort();
                float background = ground[ground.Count / 2];
                float glyph = inkLuma[inkLuma.Count / 2];
                float lighter = Mathf.Max(glyph, background);
                float darker = Mathf.Min(glyph, background);
                float ratio = (lighter + 0.05f) / (darker + 0.05f);
                if (ratio + 0.01f < minimum)
                {
                    throw new InvalidOperationException(
                        "Theatre zone name contrast is low locale=" + locale +
                        " card=" + card.name +
                        " ratio=" + ratio.ToString("0.00") +
                        ". Expected at least 4.5.");
                }
            }
        }

        private static void SampleNameContrast(
            Color32[] pixels,
            int width,
            int height,
            RectTransform canvas,
            Rect ink,
            List<float> ground,
            List<float> inkLuma)
        {
            Vector3 minLocal = canvas.InverseTransformPoint(new Vector3(ink.xMin, ink.yMin, canvas.position.z));
            Vector3 maxLocal = canvas.InverseTransformPoint(new Vector3(ink.xMax, ink.yMax, canvas.position.z));
            int x0 = Mathf.FloorToInt(Mathf.Min(minLocal.x, maxLocal.x) + width * 0.5f);
            int x1 = Mathf.CeilToInt(Mathf.Max(minLocal.x, maxLocal.x) + width * 0.5f);
            int y0 = Mathf.FloorToInt(Mathf.Min(minLocal.y, maxLocal.y) + height * 0.5f);
            int y1 = Mathf.CeilToInt(Mathf.Max(minLocal.y, maxLocal.y) + height * 0.5f);
            x0 = Mathf.Clamp(x0, 0, width - 1);
            x1 = Mathf.Clamp(x1, 0, width - 1);
            y0 = Mathf.Clamp(y0, 0, height - 1);
            y1 = Mathf.Clamp(y1, 0, height - 1);
            for (int y = y0; y <= y1; y += 2)
            {
                int row = y * width;
                for (int x = x0; x <= x1; x += 2)
                {
                    float luma = RelativeLuminance(pixels[row + x]);
                    if (luma >= 0.82f)
                        inkLuma.Add(luma);
                    else
                        ground.Add(luma);
                }
            }
        }

        private static float RelativeLuminance(Color32 pixel)
        {
            float r = SrgbChannel(pixel.r / 255f);
            float g = SrgbChannel(pixel.g / 255f);
            float b = SrgbChannel(pixel.b / 255f);
            return 0.2126f * r + 0.7152f * g + 0.0722f * b;
        }

        private static float SrgbChannel(float channel)
        {
            return channel <= 0.04045f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
        }

        private static Rect NameInkWorldRect(TMP_Text name, RectTransform fallback)
        {
            if (name != null)
            {
                name.ForceMeshUpdate();
                if (name.textInfo != null && name.textInfo.characterCount > 0)
                {
                    float minX = float.PositiveInfinity;
                    float minY = float.PositiveInfinity;
                    float maxX = float.NegativeInfinity;
                    float maxY = float.NegativeInfinity;
                    bool any = false;
                    int count = name.textInfo.characterCount;
                    for (int i = 0; i < count; i++)
                    {
                        TMP_CharacterInfo info = name.textInfo.characterInfo[i];
                        if (!info.isVisible)
                            continue;
                        any = true;
                        Vector3 bottomLeft = name.transform.TransformPoint(info.bottomLeft);
                        Vector3 topRight = name.transform.TransformPoint(info.topRight);
                        minX = Mathf.Min(minX, bottomLeft.x, topRight.x);
                        maxX = Mathf.Max(maxX, bottomLeft.x, topRight.x);
                        minY = Mathf.Min(minY, bottomLeft.y, topRight.y);
                        maxY = Mathf.Max(maxY, bottomLeft.y, topRight.y);
                    }

                    if (any)
                        return Rect.MinMaxRect(minX, minY, maxX, maxY);
                }
            }

            return fallback != null ? WorldBounds(fallback) : default;
        }

        private static void CollectBlockers(Transform regions, string prefix, List<Rect> blockers)
        {
            if (regions == null || blockers == null || string.IsNullOrEmpty(prefix))
                return;
            bool exact = prefix == "ZoomCue";
            Transform[] nodes = regions.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                if (node == null || !node.gameObject.activeInHierarchy || node is not RectTransform rect)
                    continue;
                bool match = exact
                    ? node.name == prefix
                    : node.name.StartsWith(prefix, StringComparison.Ordinal);
                if (!match)
                    continue;
                blockers.Add(WorldBounds(rect));
            }
        }

        private static float InteriorLuminanceDeviation(
            Color32[] pixels,
            int width,
            int height,
            RectTransform canvas,
            Vector2[] shape,
            List<Rect> blockers,
            string mapId,
            string locale)
        {
            if (shape == null || shape.Length < 3)
            {
                throw new InvalidOperationException(
                    "Theatre district " + mapId + " has no interior to sample locale=" + locale + ".");
            }
            float minX = shape[0].x;
            float maxX = shape[0].x;
            float minY = shape[0].y;
            float maxY = shape[0].y;
            for (int i = 1; i < shape.Length; i++)
            {
                minX = Mathf.Min(minX, shape[i].x);
                maxX = Mathf.Max(maxX, shape[i].x);
                minY = Mathf.Min(minY, shape[i].y);
                maxY = Mathf.Max(maxY, shape[i].y);
            }

            const float edgeInset = 26f;
            const float step = 18f;
            const int radius = 6;
            var samples = new List<float>();
            for (float y = minY + edgeInset; y <= maxY - edgeInset; y += step)
            {
                for (float x = minX + edgeInset; x <= maxX - edgeInset; x += step)
                {
                    var point = new Vector2(x, y);
                    if (!PointInPolygon(point, shape) || DistanceToPolygonEdge(point, shape) < edgeInset)
                        continue;
                    if (Blocked(point, blockers))
                        continue;
                    Vector3 local = canvas.InverseTransformPoint(new Vector3(point.x, point.y, canvas.position.z));
                    int px = Mathf.FloorToInt(local.x + width * 0.5f);
                    int py = Mathf.FloorToInt(local.y + height * 0.5f);
                    if (px < radius || py < radius || px >= width - radius || py >= height - radius)
                        continue;
                    samples.Add(NeighborhoodDeviation(pixels, width, px, py, radius));
                }
            }

            if (samples.Count < 8)
            {
                throw new InvalidOperationException(
                    "Theatre district " + mapId + " interior could not be sampled locale=" + locale +
                    " points=" + samples.Count + ".");
            }

            samples.Sort();
            return samples[samples.Count / 2];
        }

        private static float NeighborhoodDeviation(Color32[] pixels, int width, int px, int py, int radius)
        {
            double sum = 0d;
            double sumSq = 0d;
            int count = 0;
            for (int y = py - radius; y <= py + radius; y++)
            {
                int row = y * width;
                for (int x = px - radius; x <= px + radius; x++)
                {
                    Color32 pixel = pixels[row + x];
                    double luminance = pixel.r * 0.299 + pixel.g * 0.587 + pixel.b * 0.114;
                    sum += luminance;
                    sumSq += luminance * luminance;
                    count++;
                }
            }

            double mean = sum / count;
            return (float)Math.Sqrt(Math.Max(0d, sumSq / count - mean * mean));
        }

        private static bool Blocked(Vector2 point, List<Rect> blockers)
        {
            if (blockers == null)
                return false;
            for (int i = 0; i < blockers.Count; i++)
            {
                Rect rect = blockers[i];
                if (point.x >= rect.xMin - 2f && point.x <= rect.xMax + 2f &&
                    point.y >= rect.yMin - 2f && point.y <= rect.yMax + 2f)
                    return true;
            }

            return false;
        }

        private static Vector2[] OutlineWorldPoints(RectTransform outline)
        {
            V3PolygonGraphic polygon = outline != null ? outline.GetComponent<V3PolygonGraphic>() : null;
            if (polygon == null || polygon.PointCount < 3)
                return Array.Empty<Vector2>();
            var points = new Vector2[polygon.PointCount];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 local = polygon.LocalPoint(i);
                Vector3 world = outline.TransformPoint(new Vector3(local.x, local.y, 0f));
                points[i] = new Vector2(world.x, world.y);
            }

            return points;
        }

        private static bool PolygonsOverlap(Vector2[] left, Vector2[] right, float slop)
        {
            if (left == null || right == null || left.Length < 3 || right.Length < 3)
                return false;
            if (Separated(left, right, new Vector2(1f, 0f), slop) || Separated(left, right, new Vector2(0f, 1f), slop))
                return false;
            for (int pass = 0; pass < 2; pass++)
            {
                Vector2[] source = pass == 0 ? left : right;
                for (int i = 0; i < source.Length; i++)
                {
                    Vector2 edge = source[(i + 1) % source.Length] - source[i];
                    float length = edge.magnitude;
                    if (length < 0.001f)
                        continue;
                    var axis = new Vector2(-edge.y / length, edge.x / length);
                    if (Separated(left, right, axis, slop))
                        return false;
                }
            }

            return true;
        }

        private static bool Separated(Vector2[] left, Vector2[] right, Vector2 axis, float slop)
        {
            float aMin = float.MaxValue;
            float aMax = float.MinValue;
            for (int i = 0; i < left.Length; i++)
            {
                float dot = Vector2.Dot(left[i], axis);
                aMin = Mathf.Min(aMin, dot);
                aMax = Mathf.Max(aMax, dot);
            }

            float bMin = float.MaxValue;
            float bMax = float.MinValue;
            for (int i = 0; i < right.Length; i++)
            {
                float dot = Vector2.Dot(right[i], axis);
                bMin = Mathf.Min(bMin, dot);
                bMax = Mathf.Max(bMax, dot);
            }

            return aMax <= bMin + slop || bMax <= aMin + slop;
        }

        private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                if ((a.y > point.y) == (b.y > point.y))
                    continue;
                float x = (b.x - a.x) * (point.y - a.y) / ((b.y - a.y) + 0.0001f) + a.x;
                if (point.x < x)
                    inside = !inside;
            }

            return inside;
        }

        private static float DistanceToPolygonEdge(Vector2 point, Vector2[] polygon)
        {
            float best = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Length];
                Vector2 edge = b - a;
                float lengthSq = edge.sqrMagnitude;
                float t = lengthSq < 0.0001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - a, edge) / lengthSq);
                best = Mathf.Min(best, Vector2.Distance(point, a + edge * t));
            }

            return best;
        }

        private static void RejectTheaterOverlap(
            RectTransform[] cards,
            RectTransform[] names,
            RectTransform cue,
            string locale)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                for (int j = i + 1; j < cards.Length; j++)
                {
                    if (cards[i] == null || cards[j] == null)
                        continue;
                    if (WorldOverlap(cards[i], cards[j]))
                    {
                        throw new InvalidOperationException(
                            "Theatre zone cards overlap locale=" + locale +
                            " " + cards[i].name + " " + cards[j].name + ".");
                    }
                }
            }

            for (int i = 0; i < names.Length; i++)
            {
                for (int j = i + 1; j < names.Length; j++)
                {
                    if (names[i] == null || names[j] == null)
                        continue;
                    if (WorldOverlap(names[i], names[j]))
                    {
                        throw new InvalidOperationException(
                            "Theatre zone names overlap locale=" + locale +
                            " " + names[i].name + " " + names[j].name + ".");
                    }
                }
            }

            if (cue == null)
                return;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null || !WorldOverlap(cue, cards[i]))
                    continue;
                if (CueContainedInCard(cue, cards[i]))
                    continue;
                throw new InvalidOperationException(
                    "Theatre zoom hint overlaps a zone card locale=" + locale + " card=" + cards[i].name + ".");
            }

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != null && WorldOverlap(cue, names[i]))
                {
                    throw new InvalidOperationException(
                        "Theatre zoom hint overlaps a zone name locale=" + locale + " name=" + names[i].name + ".");
                }
            }
        }

        private static bool WorldOverlap(RectTransform a, RectTransform b)
        {
            return WorldOverlap(a, b, 2f);
        }

        private static bool WorldOverlap(RectTransform a, RectTransform b, float slop)
        {
            Rect left = WorldBounds(a);
            Rect right = WorldBounds(b);
            return left.xMin < right.xMax - slop && right.xMin < left.xMax - slop &&
                left.yMin < right.yMax - slop && right.yMin < left.yMax - slop;
        }

        private static Rect WorldBounds(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;
            for (int i = 1; i < corners.Length; i++)
            {
                minX = Mathf.Min(minX, corners[i].x);
                maxX = Mathf.Max(maxX, corners[i].x);
                minY = Mathf.Min(minY, corners[i].y);
                maxY = Mathf.Max(maxY, corners[i].y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
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

        private static bool IsNotchCapture(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                Path.GetFileName(path).IndexOf("-notch", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static void RejectBlankImages(GameObject instance)
        {
            RejectBlankImages(instance, null);
        }

        internal static void RejectBlankImages(GameObject instance, string state)
        {
            if (instance == null)
                return;
            Transform composition = instance.transform.Find("SkirmishSetupComposition");
            RejectBlankImages(composition != null ? composition.Find("BaseAssaultRules") : null, state);
            RejectBlankImages(composition != null ? composition.Find("OperationPreview") : null, null);
            if (!string.IsNullOrEmpty(state) &&
                string.Equals(state, "theater", StringComparison.OrdinalIgnoreCase))
                GuardTheatre(() => RejectTheatreMedalColumn(instance));
        }

        private static void RejectBlankImages(Transform root, string state)
        {
            if (root == null)
                return;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!SkirmishLibraryBlankImage.IsBlank(image))
                    continue;
                throw new InvalidOperationException(
                    "Visible image has no sprite and a default white or grey colour: " + ImagePath(image.transform));
            }

            RawImage[] rawImages = root.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < rawImages.Length; i++)
            {
                RawImage image = rawImages[i];
                if (!SkirmishLibraryBlankImage.IsBlank(image))
                    continue;
                throw new InvalidOperationException(
                    "Visible raw image has no texture and a default white or grey colour: " + ImagePath(image.transform));
            }

            LogBriefingImages(root);
            RejectBriefingOverlap(root);
            RejectNarrowGreyOverText(root);
            RejectMedalOverForeignText(root);
            RejectZeroOverZero(root, GameLocalization.IsRightToLeft ? "fa" : "en");
            RejectTheatreDetailOverlap(root, GameLocalization.IsRightToLeft ? "fa" : "en");
            RejectBriefingStack(root, state);
        }

        private static readonly string[] TheatreMedalNames =
        {
            "TheatreMedal_Recruit",
            "TheatreMedal_Regular",
            "TheatreMedal_Veteran",
            "TheatreMedal_Commander"
        };

        private static void RejectTheatreMedalColumn(GameObject root)
        {
            Transform strip = FindNamed(root != null ? root.transform : null, "OverallProgressStrip");
            if (strip != null && strip.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    "Theatre still shows OverallProgressStrip. The battles-won strip and its medal counts are removed. Tier counts belong on TheatreSummary.");
            }

            Transform column = FindNamed(root != null ? root.transform : null, "BriefingColumn");
            if (!TryReadTheatreSummaryCounts(column, out int[] shown)
                || !TryReadTheatreSummaryWon(column, out int wonShown, out int totalShown))
            {
                throw new InvalidOperationException("TheatreSummary missing until #68.");
            }

            Transform medalRow = column != null ? column.Find("MedalRow") : null;
            if (medalRow != null && medalRow.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException(
                    "Theatre shows MedalRow. The Theatre has no MedalRow. Tier counts belong on TheatreSummary.");
            }

            int[] expected = LedgerWholeTheatreTotals();
            for (int i = 0; i < expected.Length; i++)
            {
                if (shown[i] == expected[i])
                    continue;
                throw new InvalidOperationException(
                    "TheatreSummary " + TheatreMedalNames[i] + " TierCount reads '" + shown[i] +
                    "'. SkirmishMedalLedger whole-theatre total is " + expected[i] + ".");
            }

            int expectedWon = LedgerBattlesWon();
            QuickCustomScreenView view = root != null ? root.GetComponent<QuickCustomScreenView>() : null;
            int expectedTotal = view != null && view.LibraryViewModel != null
                ? view.LibraryViewModel.Theater.OverallPublishedCount
                : totalShown;
            if (wonShown != expectedWon || totalShown != expectedTotal)
            {
                throw new InvalidOperationException(
                    "TheatreSummary reads '" + wonShown + " / " + totalShown +
                    "'. SkirmishMedalLedger whole-theatre total is " + expectedWon + " / " + expectedTotal + ".");
            }
        }

        private static void RejectZoomPillOverCards(Transform regions, string locale, string selectedMapId)
        {
            if (regions == null)
                return;
            RectTransform pill = FindNamed(regions, "CuePlate") as RectTransform;
            if (pill == null || !pill.gameObject.activeInHierarchy)
                pill = FindNamed(regions, "ZoomCue") as RectTransform;
            if (pill == null || !pill.gameObject.activeInHierarchy)
                return;
            string selectedCard = string.IsNullOrEmpty(selectedMapId) ? string.Empty : "ZoneCard_" + selectedMapId;
            for (int i = 0; i < regions.childCount; i++)
            {
                Transform child = regions.GetChild(i);
                if (child == null || !child.name.StartsWith("ZoneCard_", StringComparison.Ordinal) || child is not RectTransform card)
                    continue;
                if (!card.gameObject.activeInHierarchy || !WorldOverlap(pill, card))
                    continue;
                if (selectedCard.Length > 0 && string.Equals(child.name, selectedCard, StringComparison.Ordinal))
                    continue;
                throw new InvalidOperationException(
                    "Theatre zoom pill overlaps a zone card locale=" + locale + " card=" + card.name + ".");
            }
        }

        private static void RejectZoomCueOverSelectedName(Transform regions, string locale, string selectedMapId)
        {
            if (regions == null || string.IsNullOrEmpty(selectedMapId))
                return;
            RectTransform pill = FindNamed(regions, "CuePlate") as RectTransform;
            if (pill == null || !pill.gameObject.activeInHierarchy)
                pill = FindNamed(regions, "ZoomCue") as RectTransform;
            if (pill == null || !pill.gameObject.activeInHierarchy)
                return;
            if (FindNamed(regions, "ZoneCard_" + selectedMapId) is not RectTransform card || !card.gameObject.activeInHierarchy)
                return;
            if (card.Find("ZoneName") is not RectTransform name || !name.gameObject.activeInHierarchy)
                return;
            TMP_Text nameText = name.GetComponent<TMP_Text>();
            Rect ink = NameInkWorldRect(nameText, name);
            Rect cue = WorldBounds(pill);
            if (ink.width < 1f || ink.height < 1f || cue.width < 1f || cue.height < 1f)
                return;
            // 6px around the ink on every side. A pill beside the title fails even when
            // the two rects do not share an x range.
            const float clearance = 6f;
            float gapX = AxisSeparation(cue.xMin, cue.xMax, ink.xMin, ink.xMax);
            float gapY = AxisSeparation(cue.yMin, cue.yMax, ink.yMin, ink.yMax);
            float gap = Mathf.Max(gapX, gapY);
            if (gap + 0.05f >= clearance)
                return;
            throw new InvalidOperationException(
                "Theatre zoom hint crowds the zone name locale=" + locale +
                " card=" + card.name +
                " gap=" + gap.ToString("0.0") +
                ". Expected at least 6px from the name ink.");
        }

        /// <summary>Positive when the spans are apart, zero when they overlap. The 2D gap is the greater axis.</summary>
        private static float AxisSeparation(float a0, float a1, float b0, float b1)
        {
            if (a1 <= b0)
                return b0 - a1;
            if (b1 <= a0)
                return a0 - b1;
            return 0f;
        }

        private static void RejectZoomPillOverHexes(Transform regions, string locale, string selectedMapId)
        {
            if (regions is not RectTransform space)
                return;
            var pills = new List<RectTransform>(2);
            if (FindNamed(regions, "CuePlate") is RectTransform plate && plate.gameObject.activeInHierarchy)
                pills.Add(plate);
            if (FindNamed(regions, "ZoomCue") is RectTransform cue && cue.gameObject.activeInHierarchy)
                pills.Add(cue);
            if (pills.Count == 0)
                return;
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            var shapes = new Vector2[mapIds.Length][];
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (FindNamed(regions, "ZoneOutline_" + mapIds[i]) is not RectTransform outline || !outline.gameObject.activeInHierarchy)
                    continue;
                shapes[i] = PlateVectors(PlateShape(space, outline));
            }

            RectTransform selected = string.IsNullOrEmpty(selectedMapId)
                ? null
                : FindNamed(regions, "ZoneCard_" + selectedMapId) as RectTransform;
            TheatreSeatGeometry.Box selectedBox = selected != null && selected.gameObject.activeInHierarchy
                ? PlateBox(space, selected)
                : default;
            // The visible hex is the fill plus a 26px edge. Inflating by 12 stopped short of that
            // stroke, so a pill on the tip of Industrial Basin did not register.
            float reach = TheatreSeatGeometry.CueHexStroke;
            for (int p = 0; p < pills.Count; p++)
            {
                TheatreSeatGeometry.Box box = PlateBox(space, pills[p]);
                bool contained = PlateBoxInside(box, selectedBox);
                if (contained)
                    continue;
                int selectedIndex = -1;
                for (int m = 0; m < mapIds.Length; m++)
                {
                    if (string.Equals(mapIds[m], selectedMapId, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = m;
                        break;
                    }
                }

                var inflated = new Rect(box.X - reach, box.Y - reach, box.W + reach * 2f, box.H + reach * 2f);
                if (inflated.width <= 1f || inflated.height <= 1f)
                    continue;
                for (int i = 0; i < mapIds.Length; i++)
                {
                    if (shapes[i] == null || shapes[i].Length < 3)
                        continue;
                    bool hits = i == selectedIndex
                        ? StrokeHitsRect(shapes[i], inflated)
                        : WorldRectHitsPolygon(inflated, shapes[i]);
                    if (!hits)
                        continue;
                    throw new InvalidOperationException(
                        "Theatre zoom pill overlaps a zone hex locale=" + locale + " district=" + mapIds[i] + ".");
                }
            }
        }

        private static bool StrokeHitsRect(Vector2[] shape, Rect rect)
        {
            if (shape == null || shape.Length < 3)
                return false;
            for (int i = 0; i < shape.Length; i++)
            {
                Vector2 next = shape[(i + 1) % shape.Length];
                if (SegmentHitsRect(shape[i], next, rect))
                    return true;
            }

            return false;
        }

        private static bool PlateBoxesOverlap(TheatreSeatGeometry.Box left, TheatreSeatGeometry.Box right)
        {
            return left.W > 1f && right.W > 1f
                && left.XMin < right.XMax && right.XMin < left.XMax
                && left.YMin < right.YMax && right.YMin < left.YMax;
        }

        private static bool PlateBoxInside(TheatreSeatGeometry.Box inner, TheatreSeatGeometry.Box outer)
        {
            const float slop = 1.5f;
            return inner.W > 1f && outer.W > 1f
                && inner.XMin >= outer.XMin - slop
                && inner.YMin >= outer.YMin - slop
                && inner.XMax <= outer.XMax + slop
                && inner.YMax <= outer.YMax + slop;
        }

        private static bool CueContainedInCard(RectTransform cue, RectTransform card)
        {
            if (cue == null || card == null)
                return false;
            Rect cueBounds = WorldBounds(cue);
            Rect cardBounds = WorldBounds(card);
            const float slop = 1.5f;
            return cueBounds.xMin >= cardBounds.xMin - slop
                && cueBounds.yMin >= cardBounds.yMin - slop
                && cueBounds.xMax <= cardBounds.xMax + slop
                && cueBounds.yMax <= cardBounds.yMax + slop;
        }

        /// <summary>
        /// Latin titles stay inside their card. A title that runs onto a neighbouring district is a violation.
        /// Farsi is already covered by the seat-geometry name box check.
        /// </summary>
        private static void RejectLatinNameInsideCard(Transform regions, string locale)
        {
            if (regions is not RectTransform space)
                return;
            string[] mapIds = SkirmishBattleLibraryArt.MapIds;
            var shapes = new Vector2[mapIds.Length][];
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (FindNamed(regions, "ZoneOutline_" + mapIds[i]) is not RectTransform outline || !outline.gameObject.activeInHierarchy)
                    continue;
                shapes[i] = PlateVectors(PlateShape(space, outline));
            }

            const float slop = 1f;
            for (int i = 0; i < mapIds.Length; i++)
            {
                if (FindNamed(regions, "ZoneCard_" + mapIds[i]) is not RectTransform card || !card.gameObject.activeInHierarchy)
                    continue;
                if (card.Find("ZoneName") is not RectTransform name || !name.gameObject.activeInHierarchy)
                    continue;
                TMP_Text text = name.GetComponent<TMP_Text>();
                if (text == null || ContainsArabicLetter(text.text))
                    continue;
                TheatreSeatGeometry.Box nameBox = PlateBox(space, name);
                TheatreSeatGeometry.Box cardBox = PlateBox(space, card);
                bool overhangs = nameBox.XMin < cardBox.XMin - slop || nameBox.XMax > cardBox.XMax + slop
                    || nameBox.YMin < cardBox.YMin - slop || nameBox.YMax > cardBox.YMax + slop;
                if (overhangs)
                {
                    throw new InvalidOperationException(
                        "Theatre zone name overhangs its card locale=" + locale + " card=" + card.name + ".");
                }

                var nameRect = new Rect(nameBox.X, nameBox.Y, nameBox.W, nameBox.H);
                for (int h = 0; h < mapIds.Length; h++)
                {
                    if (h == i || shapes[h] == null || shapes[h].Length < 3 || !WorldRectHitsPolygon(nameRect, shapes[h]))
                        continue;
                    throw new InvalidOperationException(
                        "Theatre zone name touches a zone hex locale=" + locale +
                        " card=" + card.name + " district=" + mapIds[h] + ".");
                }
            }
        }

        private static bool ContainsArabicLetter(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char letter = value[i];
                if (letter >= '\u0600' && letter <= '\u06FF')
                    return true;
            }

            return false;
        }

        private static Vector2[] PlateVectors(TheatreSeatGeometry.Vec2[] shape)
        {
            if (shape == null || shape.Length == 0)
                return Array.Empty<Vector2>();
            var points = new Vector2[shape.Length];
            for (int i = 0; i < shape.Length; i++)
                points[i] = new Vector2(shape[i].X, shape[i].Y);
            return points;
        }

        private static bool TryReadTheatreSummaryCounts(Transform column, out int[] counts)
        {
            counts = null;
            Transform summary = column != null ? column.Find("TheatreSummary") : null;
            if (summary == null || !summary.gameObject.activeInHierarchy)
                return false;
            var read = new int[TheatreMedalNames.Length];
            for (int i = 0; i < TheatreMedalNames.Length; i++)
            {
                Transform tile = summary.Find(TheatreMedalNames[i]);
                TMP_Text tier = tile != null ? tile.Find("TierCount")?.GetComponent<TMP_Text>() : null;
                if (tile == null || !tile.gameObject.activeInHierarchy || tier == null || !tier.gameObject.activeInHierarchy)
                    return false;
                if (!TryParseTierCount(tier.text, out read[i]))
                    return false;
            }

            counts = read;
            return true;
        }

        private static bool TryReadTheatreSummaryWon(Transform column, out int won, out int total)
        {
            won = 0;
            total = 0;
            Transform summary = column != null ? column.Find("TheatreSummary") : null;
            if (summary == null || !summary.gameObject.activeInHierarchy)
                return false;
            Transform track = summary.Find("ProgressTrack");
            if (track == null || !track.gameObject.activeInHierarchy || track.Find("Fill") == null)
                return false;
            TMP_Text line = summary.Find("Summary")?.GetComponent<TMP_Text>();
            if (line == null || !line.gameObject.activeInHierarchy)
                return false;
            return TryReadWonTotal(line.text, out won, out total);
        }

        private static bool TryReadWonTotal(string shown, out int won, out int total)
        {
            won = 0;
            total = 0;
            if (string.IsNullOrEmpty(shown))
                return false;
            string cleaned = StripBidiMarks(shown);
            int first = -1;
            int second = -1;
            int run = 0;
            bool inRun = false;
            for (int i = 0; i < cleaned.Length; i++)
            {
                char glyph = cleaned[i];
                if (glyph >= '۰' && glyph <= '۹')
                    glyph = (char)('0' + (glyph - '۰'));
                else if (glyph >= '٠' && glyph <= '٩')
                    glyph = (char)('0' + (glyph - '٠'));
                if (glyph < '0' || glyph > '9')
                {
                    if (inRun)
                    {
                        if (first < 0)
                            first = run;
                        else if (second < 0)
                            second = run;
                        inRun = false;
                        run = 0;
                    }

                    continue;
                }

                inRun = true;
                run = run * 10 + (glyph - '0');
            }

            if (inRun)
            {
                if (first < 0)
                    first = run;
                else if (second < 0)
                    second = run;
            }

            if (first < 0 || second < 0)
                return false;
            won = first;
            total = second;
            return true;
        }

        private static int[] LedgerWholeTheatreTotals()
        {
            SkirmishMedalDifficultyTotals ledger = CaptureLedger().TotalsByDifficulty();
            return new[] { ledger.Recruit, ledger.Regular, ledger.Veteran, ledger.Commander };
        }

        private static int LedgerBattlesWon()
        {
            return CaptureLedger().BattlesWon();
        }

        private static SkirmishMedalLedger CaptureLedger()
        {
            var grants = QuickCustomScreenView.CaptureMedalGrants;
            var rows = new SkirmishMedalSaveData[grants.Length];
            for (int i = 0; i < grants.Length; i++)
            {
                rows[i] = new SkirmishMedalSaveData
                {
                    catalogId = grants[i].CatalogId,
                    difficulty = (byte)grants[i].Difficulty,
                    contentVersion = 1,
                    sessionId = "capture"
                };
            }

            return new SkirmishMedalLedger(rows);
        }

        internal static bool TryParseTierCount(string shown, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(shown))
                return false;
            string cleaned = StripBidiMarks(shown).Trim();
            var digits = new char[cleaned.Length];
            int length = 0;
            for (int i = 0; i < cleaned.Length; i++)
            {
                char glyph = cleaned[i];
                if (glyph >= '۰' && glyph <= '۹')
                    glyph = (char)('0' + (glyph - '۰'));
                else if (glyph >= '٠' && glyph <= '٩')
                    glyph = (char)('0' + (glyph - '٠'));
                if (glyph < '0' || glyph > '9')
                    continue;
                digits[length++] = glyph;
            }

            if (length == 0)
                return false;
            return int.TryParse(new string(digits, 0, length), out value);
        }

        private static void RejectBriefingStack(Transform root, string state)
        {
            if (root == null || root.name != "BaseAssaultRules")
                return;
            var column = root.Find("BriefingColumn") as RectTransform;
            if (column == null)
                return;
            var active = new List<string>();
            for (int i = 0; i < column.childCount; i++)
            {
                Transform child = column.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;
                active.Add(child.name);
            }

            string listed = string.Join(",", active);
            Debug.Log("[SkirmishBattleLibraryCapture] briefing-active state=" + (state ?? "unspecified") + " children=" + listed);
            if (!string.IsNullOrEmpty(state))
            {
                bool theatre = string.Equals(state, "theater", StringComparison.OrdinalIgnoreCase);
                string[] forbidden = theatre
                    ? new[] { "BriefingPreview", "BriefingStats", "MedalDifficultySelector" }
                    : new[] { "BattlesWonTrack", "RecommendedNext" };
                for (int i = 0; i < forbidden.Length; i++)
                {
                    if (active.Contains(forbidden[i]))
                    {
                        throw new InvalidOperationException(
                            "Briefing column has " + forbidden[i] + " active in state " + state + ": " + listed);
                    }
                }

                if (!theatre)
                {
                    Transform seed = column.Find("SeedHelp");
                    TMP_Text seedText = seed != null ? seed.GetComponent<TMP_Text>() : null;
                    string seedValue = seedText != null ? seedText.text : string.Empty;
                    bool hidden = seedText != null && seedText.maxVisibleLines <= 0;
                    if (seed == null || !seed.gameObject.activeInHierarchy || hidden || string.IsNullOrWhiteSpace(seedValue))
                    {
                        throw new InvalidOperationException(
                            "Battlefield SeedHelp text is empty in state " + state + ": " + listed);
                    }
                }
            }

            var report = new System.Text.StringBuilder();
            report.Append("[SkirmishBattleLibraryCapture] briefing-stack state=")
                .Append(state ?? "unspecified")
                .Append(" column=")
                .Append(column.rect.width.ToString("0"))
                .Append("x")
                .Append(column.rect.height.ToString("0"));
            float previousBottom = float.PositiveInfinity;
            string previousName = string.Empty;
            for (int i = 0; i < column.childCount; i++)
            {
                if (column.GetChild(i) is not RectTransform child || !child.gameObject.activeInHierarchy)
                    continue;
                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout)
                    continue;
                Vector3 min = column.InverseTransformPoint(child.TransformPoint(child.rect.min));
                Vector3 max = column.InverseTransformPoint(child.TransformPoint(child.rect.max));
                float top = Mathf.Max(min.y, max.y);
                float bottom = Mathf.Min(min.y, max.y);
                float height = Mathf.Abs(max.y - min.y);
                report.Append(" | ").Append(i).Append(':').Append(child.name)
                    .Append(" h=").Append(height.ToString("0"))
                    .Append(" pref=").Append(element != null ? element.preferredHeight.ToString("0") : "-");
                if (top > previousBottom + 1f)
                {
                    throw new InvalidOperationException(
                        "Briefing column entries overlap: " + previousName + " and " + child.name + " " + report);
                }

                if (bottom < column.rect.yMin - 2f)
                {
                    throw new InvalidOperationException(
                        "Briefing column stack passes the bottom of the column: " + child.name + " " + report);
                }

                previousBottom = bottom;
                previousName = child.name;
            }

            Debug.Log(report.ToString());
        }

        private static void LogBriefingImages(Transform root)
        {
            if (root == null || root.name != "BaseAssaultRules")
                return;
            Transform column = root.Find("BriefingColumn");
            if (column == null)
                return;
            var report = new System.Text.StringBuilder();
            report.Append("[SkirmishBattleLibraryCapture] briefing-images");
            Image[] images = column.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.gameObject.activeInHierarchy || !image.enabled)
                    continue;
                Rect rect = image.rectTransform.rect;
                string sprite = image.sprite != null ? image.sprite.name : "none";
                report.Append(" | ").Append(ImagePath(image.transform));
                report.Append(" parent=").Append(image.transform.parent != null ? image.transform.parent.name : "none");
                report.Append(" rect=").Append(rect.x.ToString("0")).Append(",").Append(rect.y.ToString("0"));
                report.Append(",").Append(rect.width.ToString("0")).Append("x").Append(rect.height.ToString("0"));
                report.Append(" sprite=").Append(sprite);
            }

            TMP_Text[] texts = column.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy)
                    continue;
                string value = text.text ?? string.Empty;
                bool spriteTag = value.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) >= 0;
                bool foreign = text.name != "Caption";
                if (!spriteTag && text.spriteAsset == null && !foreign)
                    continue;
                if (text.name == "Caption" || text.name == "Count" || text.name == "Label")
                {
                    if (!spriteTag && text.spriteAsset == null)
                        continue;
                }

                report.Append(" | text=").Append(ImagePath(text.transform));
                report.Append(" spriteAsset=").Append(text.spriteAsset != null ? text.spriteAsset.name : "none");
                report.Append(" spriteTag=").Append(spriteTag ? "yes" : "no");
                if (value.IndexOf("approach", StringComparison.OrdinalIgnoreCase) >= 0 || spriteTag)
                    report.Append(" body=").Append(value.Replace("\n", " "));
            }

            Debug.Log(report.ToString());
        }

        private static void RejectMedalOverForeignText(Transform root)
        {
            if (root == null)
                return;
            Transform column = root.Find("BriefingColumn");
            Transform strip = root.Find("OverallProgressStrip");
            if (column != null)
                RejectMedalOverForeignTextIn(column);
            if (strip != null)
                RejectMedalOverForeignTextIn(strip);
        }

        private static void RejectMedalOverForeignTextIn(Transform scope)
        {
            if (scope == null)
                return;
            Image[] images = scope.GetComponentsInChildren<Image>(true);
            TMP_Text[] texts = scope.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !image.gameObject.activeInHierarchy || !image.enabled)
                    continue;
                if (!SkirmishLibraryBlankImage.IsMedalArt(image))
                    continue;
                if (image.sprite == null)
                    continue;
                string spriteName = image.sprite.name ?? string.Empty;
                bool tier = spriteName.IndexOf("medal", StringComparison.OrdinalIgnoreCase) >= 0;
                bool slotIcon = image.name == "Icon" && image.transform.parent != null &&
                    (image.transform.parent.name.StartsWith("Slot", StringComparison.Ordinal) ||
                     image.transform.parent.name.StartsWith("Medal", StringComparison.Ordinal));
                if (!tier && !slotIcon)
                    continue;
                for (int t = 0; t < texts.Length; t++)
                {
                    TMP_Text text = texts[t];
                    if (text == null || !text.gameObject.activeInHierarchy || !text.enabled)
                        continue;
                    if (string.IsNullOrEmpty(text.text))
                        continue;
                    bool ownLabel = text.name == "Caption" && text.transform.parent == image.transform.parent;
                    if (ownLabel)
                        continue;
                    if (!SkirmishLibraryBlankImage.OverlapsText(image.rectTransform, text))
                        continue;
                    throw new InvalidOperationException(
                        "A medal sprite covers text: " + ImagePath(image.transform) +
                        " text=" + ImagePath(text.transform));
                }
            }
        }

        private static void RejectNarrowGreyOverText(Transform root)
        {
            if (root == null)
                return;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!SkirmishLibraryBlankImage.IsNarrowBar(image))
                    continue;
                bool medal = SkirmishLibraryBlankImage.IsMedalArt(image);
                bool grey = SkirmishLibraryBlankImage.IsGreyTint(image);
                if (!medal && !grey)
                    continue;
                bool overText = false;
                for (int t = 0; t < texts.Length && !overText; t++)
                    overText = SkirmishLibraryBlankImage.OverlapsText(image.rectTransform, texts[t]);
                if (!overText)
                    continue;
                throw new InvalidOperationException(
                    "A narrow grey image covers text: " + ImagePath(image.transform));
            }
        }

        private static void RejectBriefingOverlap(Transform root)
        {
            if (root == null || root.name != "BaseAssaultRules")
                return;
            RectTransform army = root.Find("BriefingColumn/BriefingStats/Army") as RectTransform;
            RectTransform medals = root.Find("BriefingColumn/MedalRow") as RectTransform;
            if (army == null || medals == null || !army.gameObject.activeInHierarchy || !medals.gameObject.activeInHierarchy)
                return;
            Vector3 armyCenter = army.TransformPoint(army.rect.center);
            Vector3 medalCenter = medals.TransformPoint(medals.rect.center);
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic is TMP_Text || !graphic.enabled || !graphic.gameObject.activeInHierarchy)
                    continue;
                if (graphic.name == "Picture" || graphic.name == "DepthFrame")
                    continue;
                RectTransform rect = graphic.rectTransform;
                if (!SkirmishLibraryBlankImage.IsLarge(rect))
                    continue;
                if (!ContainsWorldPoint(rect, armyCenter) || !ContainsWorldPoint(rect, medalCenter))
                    continue;
                bool light = SkirmishLibraryBlankImage.IsLightNeutral(graphic.color);
                bool emptyRaw = graphic is RawImage raw && raw.texture == null;
                bool emptyImage = graphic is Image image && image.sprite == null && light;
                if (!emptyRaw && !emptyImage && !light)
                    continue;
                if (graphic is V3GradientGraphic)
                    continue;
                throw new InvalidOperationException(
                    "A blank graphic covers the army tile and the medal row: " + ImagePath(graphic.transform));
            }
        }

        private static bool ContainsWorldPoint(RectTransform rect, Vector3 worldPoint)
        {
            if (rect == null)
                return false;
            Vector3 local = rect.InverseTransformPoint(worldPoint);
            return rect.rect.Contains(local);
        }

        private static string ImagePath(Transform transform)
        {
            if (transform == null)
                return "image";
            string path = transform.name;
            Transform cursor = transform.parent;
            while (cursor != null)
            {
                path = cursor.name + "/" + path;
                cursor = cursor.parent;
            }

            return path;
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
            bool fa = GameLocalization.IsRightToLeft;
            RectTransform compositionRect = composition as RectTransform;
            float width = compositionRect != null ? compositionRect.rect.width : 0f;
            float height = compositionRect != null ? compositionRect.rect.height : 0f;
            QuickCustomScreenView view = instance.GetComponent<QuickCustomScreenView>();
            bool theatre = view != null && view.Library != null && view.Library.Level != SkirmishLibraryLevel.Battlefield;
            bool briefingOnLeft = theatre ? !fa : fa;
            SkirmishLibraryFrame.Resolve(
                width,
                height,
                briefingOnLeft,
                out float seamMapX,
                out float seamMapWidth,
                out float seamBriefingX,
                out float seamBriefingWidth,
                out float _,
                out float _);
            SkirmishLibraryFrame.CloseTheatreSeam(
                theatre,
                briefingOnLeft,
                ref seamMapX,
                ref seamMapWidth,
                ref seamBriefingX,
                seamBriefingWidth);
            MeasureSideColumns(compositionRect, map, out float leftWidth, out float rightWidth);
            float expectedWidth = TheatreSeatGeometry.ExpectedMapPanelWidth(width, leftWidth, rightWidth, theatre);
            int sideCount = (leftWidth > 1f ? 1 : 0) + (rightWidth > 1f ? 1 : 0);
            float gutters = TheatreSeatGeometry.MapPanelGutters(sideCount, theatre);
            float floor = TheatreSeatGeometry.MapPanelWidthFloor(width);
            bool fourByThree = TheatreSeatGeometry.MapPanelWidthFourByThree(width, height);
            float actualWidth = map != null ? PlacedSpan(map) : 0f;
            float briefingSpan = briefing != null ? PlacedSpan(briefing) : 0f;
            TheatreSeatGeometry.ExpectedLivePanels(
                width,
                height,
                briefingSpan,
                expectedWidth,
                theatre,
                briefingOnLeft,
                out float mapX,
                out float briefingX,
                out float panelY,
                out float panelHeight);
            if (!string.Equals(TheatreSeatGeometry.ShotNotch, "notch", StringComparison.Ordinal))
            {
                if (theatre)
                {
                    Debug.Log(TheatreSeatGeometry.FormatMapPanelWidth(
                        TheatreSeatGeometry.ShotRatio,
                        fa ? "fa" : "en",
                        actualWidth,
                        expectedWidth,
                        leftWidth,
                        rightWidth,
                        gutters,
                        floor));
                }

                float actualMapX = map != null ? map.anchoredPosition.x : 0f;
                float actualBriefingX = briefing != null ? briefing.anchoredPosition.x : 0f;
                Debug.Log("[MapPanelRect] ratio=" + TheatreSeatGeometry.ShotRatio
                    + " locale=" + (fa ? "fa" : "en")
                    + " mapX=" + actualMapX.ToString("0.0", CultureInfo.InvariantCulture)
                    + " expected=" + mapX.ToString("0.0", CultureInfo.InvariantCulture)
                    + " briefingX=" + actualBriefingX.ToString("0.0", CultureInfo.InvariantCulture)
                    + " expected=" + briefingX.ToString("0.0", CultureInfo.InvariantCulture)
                    + " briefingW=" + briefingSpan.ToString("0.0", CultureInfo.InvariantCulture)
                    + " mapW=" + actualWidth.ToString("0.0", CultureInfo.InvariantCulture)
                    + " theatre=" + (theatre ? "1" : "0")
                    + " briefingOnLeft=" + (briefingOnLeft ? "1" : "0")
                    + " frameMapX=" + seamMapX.ToString("0.0", CultureInfo.InvariantCulture)
                    + " frameMapW=" + seamMapWidth.ToString("0.0", CultureInfo.InvariantCulture)
                    + " frameBriefingX=" + seamBriefingX.ToString("0.0", CultureInfo.InvariantCulture)
                    + " frameBriefingW=" + seamBriefingWidth.ToString("0.0", CultureInfo.InvariantCulture));
            }

            if (map != null && !TheatreSeatGeometry.MapPanelWidthHolds(
                    actualWidth, expectedWidth, floor, theatre && fourByThree, out string widthFailure))
                throw new InvalidOperationException(widthFailure);
            ExpectPanel(map, "map", mapX, -panelY, expectedWidth, panelHeight);
            ExpectPanel(briefing, "briefing", briefingX, -panelY, briefingSpan, panelHeight);
            if (theatre && TheatreSeatGeometry.ZoomCueAttemptFailed)
            {
                string cueFailure = TheatreSeatGeometry.FormatZoomCueAttempt(
                    TheatreSeatGeometry.ShotRatio,
                    fa ? "fa" : "en",
                    actualWidth,
                    panelHeight);
                Debug.Log(cueFailure);
                throw new InvalidOperationException(cueFailure);
            }
            RequireFilled(map, "map");
            RequireFilled(briefing, "briefing");
            var plate = composition.Find("OperationPreview/MapPreviewClip") as RectTransform;
            var column = composition.Find("BaseAssaultRules/BriefingColumn") as RectTransform;
            if (column != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(column);
            RequireFilled(plate, "map plate");
            RequireFilled(column, "briefing column");
        }

        /// <summary>
        /// Left and right column widths from the placed panels. The briefing is one side.
        /// A second tall panel on the other side is the detail column. An absent side is zero.
        /// </summary>
        private static void MeasureSideColumns(RectTransform composition, RectTransform map, out float left, out float right)
        {
            left = 0f;
            right = 0f;
            if (composition == null || map == null)
                return;
            float mapWidth = PlacedSpan(map);
            float mapLeft = map.anchoredPosition.x;
            float mapCentre = mapLeft + mapWidth * 0.5f;
            for (int i = 0; i < composition.childCount; i++)
            {
                var child = composition.GetChild(i) as RectTransform;
                if (child == null || child == map || !child.gameObject.activeInHierarchy)
                    continue;
                string name = child.name ?? string.Empty;
                if (name.IndexOf("Shadow", StringComparison.Ordinal) >= 0 || name == "LibraryFilters")
                    continue;
                float span = PlacedSpan(child);
                float height = child.rect.height > 1f ? child.rect.height : child.sizeDelta.y;
                bool briefing = name == "BaseAssaultRules";
                bool detail = name.IndexOf("Detail", StringComparison.Ordinal) >= 0
                    || name.IndexOf("Objective", StringComparison.Ordinal) >= 0
                    || name.IndexOf("District", StringComparison.Ordinal) >= 0;
                if (!briefing && !detail)
                    continue;
                if (!briefing && (span < 160f || height < 400f))
                    continue;
                if (span < 1f)
                    continue;
                float centre = child.anchoredPosition.x + span * 0.5f;
                if (centre < mapCentre)
                    left = Mathf.Max(left, span);
                else
                    right = Mathf.Max(right, span);
            }
        }

        private static float PlacedSpan(RectTransform rect)
        {
            if (rect == null)
                return 0f;
            bool point = Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x) < 0.01f;
            if (point && rect.sizeDelta.x > 1f)
                return rect.sizeDelta.x;
            if (rect.rect.width > 1f)
                return rect.rect.width;
            return Mathf.Max(0f, rect.sizeDelta.x);
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
