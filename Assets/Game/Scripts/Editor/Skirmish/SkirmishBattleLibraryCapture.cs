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

        internal static void ResetZoneCountLedger()
        {
            ZonePlayableCounts.Clear();
        }

        private static void Capture(bool quit)
        {
            ResetZoneCountLedger();
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
                RectTransform outline = RequireTheaterOverlay(regions, clip, "ZoneOutline_" + mapIds[i], "outline", mapIds[i], locale);
                RejectZonePolygon(outline, clip, mapIds[i], locale);
                RequireTheaterOverlay(regions, clip, "ZonePin_" + mapIds[i], "pin", mapIds[i], locale);
                RectTransform card = RequireTheaterOverlay(regions, clip, "ZoneCard_" + mapIds[i], "zone card", mapIds[i], locale);
                cards[i] = card;
                names[i] = card.Find("ZoneName") as RectTransform;
                RejectZoneCardContent(card, pins, mapIds[i], locale);
                RejectSelectedZoneMatchesGridRows(view, pins, mapIds[i], locale);
            }

            RejectZoneProgressMatchesCard(root, view, locale);
            RejectStripMatchesZoneTotals(root, view, locale);

            RectTransform cue = null;
            Transform cueNode = FindNamed(regions, "ZoomCue");
            if (cueNode != null && cueNode.gameObject.activeInHierarchy)
            {
                cue = RequireTheaterOverlay(regions, clip, "ZoomCue", "zoom hint", "selected", locale);
                TMP_Text cueText = cue.GetComponent<TMP_Text>();
                if (cueText != null)
                    RejectReadableLine(cueText, "zoom hint", "selected", locale);
                RejectCueClearOfPins(regions, cue, locale);
            }

            RejectTheaterOverlap(cards, names, cue, locale);
            RejectZoneCardSpacing(cards, clip, locale);
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

            if (polygon.color.a < 0.14f || polygon.color.a > 0.24f)
            {
                throw new InvalidOperationException(
                    "Theatre polygon fill for " + mapId + " is " + polygon.color.a.ToString("0.00") +
                    " locale=" + locale + ". Expected about 0.18.");
            }

            bool stroke = false;
            bool glow = false;
            for (int i = 0; i < outline.childCount; i++)
            {
                Transform child = outline.GetChild(i);
                if (child == null || child.name != "HexEdge" || child is not RectTransform edge || !edge.gameObject.activeInHierarchy)
                    continue;
                float thickness = edge.sizeDelta.y;
                if (thickness >= 3f && thickness <= 4.5f)
                    stroke = true;
                if (thickness >= 8f)
                    glow = true;
            }

            if (!stroke || !glow)
            {
                throw new InvalidOperationException(
                    "Theatre polygon for " + mapId + " is missing its hex stroke or glow locale=" + locale + ".");
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
                throw new InvalidOperationException(
                    "Theatre " + kind + " for " + mapId + " is outside the map locale=" + locale +
                    " local=" + local.x.ToString("0.0") + "," + local.y.ToString("0.0"));
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
            if (size.x < 280f || size.x > 320f || size.y < 96f || size.y > 110f)
            {
                throw new InvalidOperationException(
                    "Theatre zone card for " + mapId + " is " + size.x.ToString("0") + "x" + size.y.ToString("0") +
                    " locale=" + locale + ". Expected about 280-320 by 96-110 at 1080p.");
            }

            TMP_Text name = card.Find("ZoneName")?.GetComponent<TMP_Text>();
            if (name == null)
                throw new InvalidOperationException("Theatre zone card has no name for " + mapId + " locale=" + locale + ".");
            RejectReadableLine(name, "zone name", mapId, locale);

            RawImage thumb = card.Find("Thumbnail")?.GetComponent<RawImage>();
            if (thumb == null || thumb.texture == null || thumb.color.r < 0.95f || thumb.color.g < 0.95f || thumb.color.b < 0.95f)
                throw new InvalidOperationException("Theatre zone thumbnail is missing or tinted for " + mapId + " locale=" + locale + ".");

            Image fill = card.Find("ProgressRing/ProgressFill")?.GetComponent<Image>();
            TMP_Text count = card.Find("ProgressRing/ZoneCount")?.GetComponent<TMP_Text>();
            if (fill == null || count == null)
                throw new InvalidOperationException("Theatre zone card is missing its progress arc for " + mapId + " locale=" + locale + ".");
            if (fill.type != Image.Type.Filled || fill.fillMethod != Image.FillMethod.Radial360)
                throw new InvalidOperationException("Theatre progress for " + mapId + " is not a circular arc locale=" + locale + ".");

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
            string shown = StripBidiMarks(count.text).Trim();
            bool matches = pending
                ? SkirmishBattleLibraryArt.IsComingSoon(shown)
                : shown == expected;
            if (!matches)
            {
                throw new InvalidOperationException(
                    "Theatre zone count for " + mapId + " is '" + count.text + "' locale=" + locale +
                    ". Expected " + expected + " (medals won / playable scenarios).");
            }

            RejectReadableLine(count, "zone count", mapId, locale);
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
            string shown = StripBidiMarks(count.text).Trim();
            bool fa = locale != null && locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase);
            if (SkirmishBattleLibraryArt.IsComingSoon(shown))
                shown = SkirmishBattleLibraryArt.ComingSoon(fa);
            else
                shown = shown.Replace(" ", string.Empty);
            RememberZoneCount(locale, mapId, shown, "zone progress");
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
            if (strip == null || !strip.gameObject.activeInHierarchy)
                return;
            TMP_Text count = strip.Find("Count")?.GetComponent<TMP_Text>();
            string expected = theater.OverallWonCount + " / " + theater.OverallPublishedCount;
            if (count == null || StripBidiMarks(count.text) != expected)
            {
                throw new InvalidOperationException(
                    "Battles-won strip reads '" + (count != null ? count.text : string.Empty) +
                    "' locale=" + locale + ". Expected " + expected + ".");
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
            if (SkirmishBattleLibraryArt.IsComingSoon(cleaned))
                return true;
            cleaned = cleaned.Replace(" ", string.Empty);
            int slash = cleaned.IndexOf('/');
            if (slash <= 0 || slash >= cleaned.Length - 1)
                return false;
            return int.TryParse(cleaned.Substring(0, slash), out won) &&
                int.TryParse(cleaned.Substring(slash + 1), out published);
        }

        private static void RejectReadableLine(TMP_Text text, string kind, string mapId, string locale)
        {
            text.ForceMeshUpdate();
            if (text.isTextTruncated || text.isTextOverflowing)
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
            if (text.preferredWidth > width + 2f)
            {
                throw new InvalidOperationException(
                    "Theatre " + kind + " draws past its box for " + mapId + " locale=" + locale +
                    " preferred=" + text.preferredWidth.ToString("0.0") + " width=" + width.ToString("0.0") +
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
                if (cards[i] != null && WorldOverlap(cue, cards[i]))
                {
                    throw new InvalidOperationException(
                        "Theatre zoom hint overlaps a zone card locale=" + locale + " card=" + cards[i].name + ".");
                }
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
            RejectBriefingStack(root, state);
        }

        private static void RejectBriefingStack(Transform root, string state)
        {
            if (root == null || root.name != "BaseAssaultRules")
                return;
            var column = root.Find("BriefingColumn") as RectTransform;
            if (column == null)
                return;
            var active = new List<string>();
            int medalRows = 0;
            for (int i = 0; i < column.childCount; i++)
            {
                Transform child = column.GetChild(i);
                if (child == null || !child.gameObject.activeInHierarchy)
                    continue;
                active.Add(child.name);
                if (child.name == "MedalRow")
                    medalRows++;
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

                if (medalRows != 1)
                {
                    throw new InvalidOperationException(
                        "Briefing column expected one MedalRow in state " + state + " and found " + medalRows + ": " + listed);
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
            SkirmishLibraryFrame.Resolve(
                width,
                height,
                !fa,
                out float mapX,
                out float mapWidth,
                out float briefingX,
                out float briefingWidth,
                out float panelY,
                out float panelHeight);
            SkirmishLibraryFrame.CloseTheatreSeam(theatre, !fa, ref mapX, ref mapWidth, ref briefingX, briefingWidth);
            ExpectPanel(map, "map", mapX, -panelY, mapWidth, panelHeight);
            ExpectPanel(briefing, "briefing", briefingX, -panelY, briefingWidth, panelHeight);
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
