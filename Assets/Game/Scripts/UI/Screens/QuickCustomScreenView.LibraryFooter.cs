using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private void PresentFilterLabels(bool fa)
        {
            if (_libraryFilters == null)
                return;
            SetFilterLabel(_libraryFilters.Find("ArmyGround"), fa ? "زمینی" : "GROUND");
            SetFilterLabel(_libraryFilters.Find("ArmyAir"), fa ? "هوایی" : "AIR");
            SetFilterLabel(_libraryFilters.Find("ArmyCombined"), fa ? "ترکیبی" : "COMBINED");
            SetFilterLabel(_libraryFilters.Find("StartField"), fa ? "میدانی" : "FIELD");
            SetFilterLabel(_libraryFilters.Find("StartEstablished"), fa ? "آماده" : "ESTABLISHED");
            SetFilterLabel(_libraryFilters.Find("MissingMedal"), fa ? "بدون مدال" : "MISSING MEDAL");
            // v41 audit: the active filters show the selected state (they toggle real SkirmishLibraryFilter fields).
            TintFilter("ArmyGround", _armyFilter == "G");
            TintFilter("ArmyAir", _armyFilter == "A");
            TintFilter("ArmyCombined", _armyFilter == "C");
            TintFilter("StartField", _startFilter == "F");
            TintFilter("StartEstablished", _startFilter == "E");
            TintFilter("MissingMedal", _missingMedalOnly);
        }

        private void TintFilter(string name, bool active)
        {
            Transform slot = _libraryFilters != null ? _libraryFilters.Find(name) : null;
            Button button = slot != null ? slot.GetComponent<Button>() : null;
            if (button != null)
                TintButton(button, active, true);
        }

        private void SetFilterLabel(Transform button, string label)
        {
            if (button == null)
                return;
            SetCopy(button.Find("Label")?.GetComponent<TMP_Text>(), label);
        }

        private void PlaceLibraryFooter()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition is not RectTransform compositionRect)
                return;
            PlaceFooterPlate(compositionRect);
            SyncGridPreviewOrnaments(); // v49b: no preview corner ornaments over the approved-3 Grid
            Button back = composition.Find("BackButton")?.GetComponent<Button>();
            float compositionHeight = compositionRect.rect.height;
            float compositionWidth = compositionRect.rect.width;
            if (compositionHeight < 200f)
                compositionHeight = 941f;
            if (compositionWidth < 200f)
                compositionWidth = 1672f;
            float height = 100f; // approved-1/2/3 footer weight
            float y = compositionHeight - 112f;
            float gap = 6f;
            float side = 10f;
            // v39: 4:3-class compositions get a wider safe margin so ADVANCED/BACK clear the screen edge; 16:9+ unchanged.
            if (compositionWidth / compositionHeight < 1.6f)
                side = 24f;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (!_savedMatchAvailable && back != null)
            {
                PlaceLevelFooter(back, compositionWidth, y, height, side, fa);
                return;
            }

            Button[] commands =
            {
                back,
                resetButton,
                _libraryAdvancedButton,
                randomizeSeedButton,
                launchButton
            };
            int commandCount = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                if (commands[i] != null)
                    commandCount++;
            }

            if (commandCount == 0)
                return;
            float buttonWidth = (compositionWidth - side * 2f - gap * (commandCount - 1)) / commandCount;
            if (buttonWidth < 72f)
            {
                gap = 4f;
                buttonWidth = (compositionWidth - side * 2f - gap * (commandCount - 1)) / commandCount;
            }

            float x = side;
            for (int i = 0; i < commands.Length; i++)
            {
                Button button = commands[i];
                if (button == null)
                    continue;
                PlaceTopLeft(button.GetComponent<RectTransform>(), x, y, buttonWidth, height);
                HideFooterChevrons(button);
                HideDiceIcon(button);
                ClearFooterIcon(button);
                if (button == back)
                {
                    TMP_Text backLabel = button.GetComponentInChildren<TMP_Text>(true);
                    SetCopy(backLabel, fa ? "بازگشت" : "BACK", 14f, 20f);
                }

                FitFooterLabel(button, fa, buttonWidth);
                if (button == randomizeSeedButton)
                    FitZoomInLabel(button, buttonWidth);
                TMP_Text footerLabel = button.GetComponentInChildren<TMP_Text>(true);
                if (footerLabel != null)
                    footerLabel.isRightToLeftText = ContainsArabicScript(footerLabel.text);
                if (button == back)
                    AttachAudio(back, UIAudioEventKind.ScreenBack);
                x += buttonWidth + gap;
            }
        }

        /// <summary>
        /// Four-button footer per level (Theatre: Back, Filters, Zoom In, Quick Battle; Battlefield: Back, Theatre Map,
        /// Start Battle, Advanced), laid out and coloured by <see cref="SkirmishFooterBarWidget"/> with its button sounds.
        /// The prefab buttons are reused so control names, aliases and actions stay intact; unused ones are hidden.
        /// </summary>
        private void PlaceLevelFooter(Button back, float compositionWidth, float y, float height, float side, bool fa)
        {
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            SkirmishLibraryLevel level = battlefield ? SkirmishLibraryLevel.Battlefield : SkirmishLibraryLevel.Theater;
            Button[] shown = battlefield
                ? new[] { back, randomizeSeedButton, launchButton, _libraryAdvancedButton }
                : new[] { back, resetButton, randomizeSeedButton, launchButton };
            Button[] all = { back, resetButton, _libraryAdvancedButton, randomizeSeedButton, launchButton };
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].gameObject.SetActive(Array.IndexOf(shown, all[i]) >= 0);
            }

            IReadOnlyList<SkirmishFooterBarWidget.ButtonSpec> specs = SkirmishFooterBarWidget.Buttons(level);
            // approved-1/2 + Game PM: Zoom In is the green primary on Theatre, Start Battle on Battlefield.
            _footerPrimary = battlefield ? launchButton : randomizeSeedButton;
            _zoomFitSize = 0f;
            float gap = 8f;
            Rect[] rects = SkirmishFooterBarWidget.Layout(level, compositionWidth - side * 2f, height, gap, fa);
            for (int i = 0; i < shown.Length && i < specs.Count; i++)
            {
                Button button = shown[i];
                if (button == null)
                    continue;
                PlaceTopLeft(button.GetComponent<RectTransform>(), side + rects[i].x, y, rects[i].width, height);
                HideFooterChevrons(button);
                HideDiceIcon(button);
                ClearFooterIcon(button);
                Color face = button == _footerPrimary ? new Color(0.24f, 0.58f, 0.18f, 1f) : specs[i].Color;
                if (button != _footerPrimary && face.g > face.r * 1.6f && face.g > face.b * 1.6f)
                    face = new Color(0.14f, 0.46f, 0.7f, 1f); // only one green primary per footer
                TintCommand(button, Color.Lerp(face, Color.white, 0.12f), Color.Lerp(face, Color.black, 0.25f));
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (button == back)
                    SetCopy(label, fa ? "بازگشت" : "BACK", 20f, 28f);
                else if (button == _libraryAdvancedButton)
                    SetCopy(label, fa ? "نبرد سریع" : "QUICK BATTLE", 20f, 28f);
                FitFooterLabel(button, fa, rects[i].width);
                if (label != null)
                {
                    label.fontSizeMax = Mathf.Max(label.fontSizeMax, 28f);
                    label.isRightToLeftText = ContainsArabicScript(label.text);
                }

                bool primary = button == _footerPrimary;
                if (label != null)
                {
                    // approved mockups: heavy footer type, the green primary largest.
                    label.fontStyle = FontStyles.Bold;
                    label.fontSizeMin = primary ? 28f : 24f; // type scale up (Game PM)
                    label.fontSizeMax = primary ? 44f : 34f;
                }

                if (button != back)
                    AddFooterGlyph(button, FooterGlyphFor(specs[i].Name), height, fa, label);
                if (!battlefield && button == randomizeSeedButton)
                    FitLevelZoomLabel(label, rects[i].width, height, fa);
                ClearButtonAudio(button);
                AttachAudio(button, specs[i].Sound);
                AddFooterDepth(button, face);
            }
        }

        private enum FooterGlyph { None, Funnel, Chevrons, Dice, Map, Gear, Clock, Squares, Shield, Pin }

        private static FooterGlyph FooterGlyphFor(string controlName)
        {
            switch (controlName)
            {
                case SkirmishLibraryControlNames.Filters: return FooterGlyph.Funnel;
                case SkirmishLibraryControlNames.ZoomIn: return FooterGlyph.Chevrons;
                case SkirmishLibraryControlNames.StartBattle: return FooterGlyph.Chevrons;
                case SkirmishLibraryControlNames.QuickBattle: return FooterGlyph.Dice;
                case SkirmishLibraryControlNames.TheaterMap: return FooterGlyph.Map;
                case SkirmishFooterBarWidget.AdvancedName: return FooterGlyph.Gear;
                default: return FooterGlyph.Gear;
            }
        }

        private static readonly Dictionary<FooterGlyph, Texture2D> FooterGlyphCache = new Dictionary<FooterGlyph, Texture2D>();

        /// <summary>Leading-side icon (right in RTL) drawn in code, so no new art assets are needed.</summary>
        private static void AddFooterGlyph(Button button, FooterGlyph glyph, float height, bool fa, TMP_Text label)
        {
            if (glyph == FooterGlyph.None)
                return;
            Texture2D texture = FooterGlyphTexture(glyph);
            var icon = new GameObject("LibraryIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            icon.transform.SetParent(button.transform, false);
            float size = Mathf.Round(height * 0.5f);
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(fa ? 1f : 0f, 0.5f);
            rect.pivot = new Vector2(fa ? 1f : 0f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(fa ? -18f : 18f, 0f);
            icon.texture = texture;
            icon.color = glyph == FooterGlyph.Chevrons ? new Color(1f, 0.78f, 0.25f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
            icon.raycastTarget = false;
            icon.uvRect = fa && glyph == FooterGlyph.Chevrons ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
            if (label != null)
            {
                float inset = 18f + size + 8f;
                label.margin = new Vector4(fa ? 2f : inset, 1f, fa ? inset : 2f, 1f);
            }
        }

        private static Texture2D FooterGlyphTexture(FooterGlyph glyph)
        {
            if (FooterGlyphCache.TryGetValue(glyph, out Texture2D cached) && cached != null)
                return cached;
            const int n = 64;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "FooterGlyph_" + glyph, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    float v = 1f - (y + 0.5f) / n; // v down from the top
                    float a = Mathf.Clamp01(GlyphCoverage(glyph, u, v));
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            FooterGlyphCache[glyph] = texture;
            return texture;
        }

        /// <summary>Soft-edged coverage for each glyph in unit space (u right, v down).</summary>
        private static float GlyphCoverage(FooterGlyph glyph, float u, float v)
        {
            const float edge = 0.03f;
            switch (glyph)
            {
                case FooterGlyph.Chevrons:
                {
                    float best = 0f;
                    for (int k = 0; k < 2; k++)
                    {
                        float cx = 0.3f + k * 0.34f;
                        float d = Mathf.Abs(u - (cx - 0.18f + 0.36f * (0.5f - Mathf.Abs(v - 0.5f)))) ;
                        float inside = v > 0.12f && v < 0.88f ? 1f : 0f;
                        best = Mathf.Max(best, inside * Mathf.Clamp01((0.09f - d) / edge));
                    }

                    return best;
                }
                case FooterGlyph.Funnel:
                {
                    if (v < 0.12f || v > 0.9f)
                        return 0f;
                    float half = v < 0.55f ? Mathf.Lerp(0.42f, 0.1f, (v - 0.12f) / 0.43f) : 0.1f;
                    return Mathf.Clamp01((half - Mathf.Abs(u - 0.5f)) / edge);
                }
                case FooterGlyph.Dice:
                {
                    float box = Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f));
                    float frame = Mathf.Clamp01((0.4f - box) / edge) * Mathf.Clamp01((box - 0.31f) / edge);
                    float pips = 0f;
                    Vector2[] dots = { new Vector2(0.36f, 0.36f), new Vector2(0.64f, 0.64f), new Vector2(0.5f, 0.5f), new Vector2(0.64f, 0.36f), new Vector2(0.36f, 0.64f) };
                    for (int i = 0; i < dots.Length; i++)
                        pips = Mathf.Max(pips, Mathf.Clamp01((0.07f - Vector2.Distance(new Vector2(u, v), dots[i])) / edge));
                    return Mathf.Max(frame, pips);
                }
                case FooterGlyph.Map:
                {
                    if (u < 0.1f || u > 0.9f)
                        return 0f;
                    int panel = Mathf.Clamp((int)((u - 0.1f) / (0.8f / 3f)), 0, 2);
                    float local = (u - 0.1f) / (0.8f / 3f) - panel;
                    float slope = (panel % 2 == 0 ? 0.08f : -0.08f) * local;
                    float top = 0.2f + slope;
                    float bottom = 0.8f + slope;
                    float body = Mathf.Clamp01((v - top) / edge) * Mathf.Clamp01((bottom - v) / edge);
                    float fold = Mathf.Clamp01((Mathf.Abs(local - 0f) - 0.04f) / 0.03f);
                    return body * (panel == 0 ? 1f : Mathf.Max(0.35f, fold));
                }
                case FooterGlyph.Gear:
                {
                    float dx = u - 0.5f, dy = v - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float teeth = Mathf.Cos(angle * 8f) > 0.3f ? 0.42f : 0.34f;
                    float outer = Mathf.Clamp01((teeth - r) / edge);
                    float hole = Mathf.Clamp01((r - 0.13f) / edge);
                    return outer * hole;
                }
                case FooterGlyph.Clock:
                {
                    float dx = u - 0.5f, dy = v - 0.5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.Clamp01((0.4f - r) / edge) * Mathf.Clamp01((r - 0.32f) / edge);
                    float minute = Mathf.Abs(dx) < 0.04f && dy < 0.02f && dy > -0.26f ? 1f : 0f;
                    float hour = Mathf.Abs(dy) < 0.04f && dx > -0.02f && dx < 0.18f ? 1f : 0f;
                    return Mathf.Max(ring, Mathf.Max(minute, hour));
                }
                case FooterGlyph.Squares:
                {
                    float best = 0f;
                    for (int k = 0; k < 4; k++)
                    {
                        float cx = k % 2 == 0 ? 0.32f : 0.68f, cy = k / 2 == 0 ? 0.32f : 0.68f;
                        float box = Mathf.Max(Mathf.Abs(u - cx), Mathf.Abs(v - cy));
                        best = Mathf.Max(best, Mathf.Clamp01((0.14f - box) / edge));
                    }
                    return best;
                }
                case FooterGlyph.Shield:
                {
                    if (v < 0.12f || v > 0.9f)
                        return 0f;
                    float half = v < 0.5f ? 0.36f : Mathf.Lerp(0.36f, 0.02f, (v - 0.5f) / 0.4f);
                    return Mathf.Clamp01((half - Mathf.Abs(u - 0.5f)) / edge);
                }
                case FooterGlyph.Pin:
                {
                    float dx = u - 0.5f, dy = v - 0.38f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float head = Mathf.Clamp01((0.26f - r) / edge) * Mathf.Clamp01((r - 0.1f) / edge);
                    float tail = v > 0.5f && v < 0.9f ? Mathf.Clamp01((Mathf.Lerp(0.2f, 0f, (v - 0.5f) / 0.4f) - Mathf.Abs(dx)) / edge) : 0f;
                    return Mathf.Max(head, tail);
                }
                default:
                    return 0f;
            }
        }

        /// <summary>Raised footer button: drop shadow, a lit top edge and a darker base lip (approved mockups).</summary>
        private static void AddFooterDepth(Button button, Color face)
        {
            if (button == null)
                return;
            Graphic target = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (target != null)
            {
                Shadow shadow = target.GetComponent<Shadow>() ?? target.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
                shadow.effectDistance = new Vector2(0f, -10f);
            }

            EnsureFooterEdge(button.transform, "FooterBevel", true, 5f, Color.Lerp(face, Color.white, 0.7f));
            EnsureFooterEdge(button.transform, "FooterLip", false, 9f, Color.Lerp(face, Color.black, 0.65f));
        }

        private static void EnsureFooterEdge(Transform parent, string name, bool top, float height, Color color)
        {
            Image edge = parent.Find(name)?.GetComponent<Image>();
            if (edge == null)
            {
                edge = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                edge.transform.SetParent(parent, false);
            }

            RectTransform rect = edge.rectTransform;
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            edge.color = color;
            edge.raycastTarget = false;
            edge.transform.SetAsFirstSibling();
            edge.transform.SetSiblingIndex(Mathf.Min(1, parent.childCount - 1));
        }

        /// <summary>
        /// Late re-pin of footer label sizes: the library text passes (EnforceLibraryFontFloor/FinishLibraryText) run
        /// after PlaceLibraryFooter and shrink the labels; this runs from PlaceLibraryHeader, after them.
        /// </summary>
        private void ReassertFooterLabels()
        {
            Button[] all = { backButtonRef(), resetButton, _libraryAdvancedButton, randomizeSeedButton, launchButton };
            for (int i = 0; i < all.Length; i++)
            {
                Button button = all[i];
                if (button == null || !button.gameObject.activeInHierarchy)
                    continue;
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null)
                    continue;
                bool primary = button == _footerPrimary; // Zoom In (Theatre) / Start Battle (Battlefield)
                float max = primary ? 44f : 34f;
                float min = primary ? 28f : 24f;
                if (button == randomizeSeedButton && _zoomFitSize > 0f)
                    min = max = _zoomFitSize; // v46b: keep the fitted Zoom In size (FitLevelZoomLabel)
                label.enableAutoSizing = true;
                label.fontSizeMin = min;
                label.fontSizeMax = max;
                label.fontStyle = FontStyles.Bold;
                label.overflowMode = TextOverflowModes.Overflow;
                V3LocalizedTextBindingView binding = label.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(min, max);
            }
        }

        private Button _footerPrimary;

        /// <summary>v46b: fitted Theatre Zoom In label size, 0 when the normal 28-44 primary range fits.</summary>
        private float _zoomFitSize;

        /// <summary>
        /// v46b: "ZOOM IN · INDUSTRIAL BASIN" overflowed the 4:3 primary button because the 28 px primary floor was
        /// re-applied after the fit. Same rule as P2's FitZoomInLabel (16 px command floor; break at the dot, else the
        /// district alone), measured with the label's own font on the laid-out width minus the glyph inset. fa-IR never
        /// breaks the line (RTL line order), it falls back to the district alone. Logs [ZoomLabel].
        /// </summary>
        private void FitLevelZoomLabel(TMP_Text label, float buttonWidth, float height, bool fa)
        {
            if (label == null)
                return;
            ZoomFooterParts(fa, out string verb, out string district);
            const float floor = 16f;
            const float primaryMin = 28f;
            const float primaryMax = 44f;
            // FitFooterLabel stretches the label 8 px either side; AddFooterGlyph sets the side margins.
            float inner = buttonWidth - 16f - label.margin.x - label.margin.z - 2f;
            float lineHeight = height - 12f - label.margin.y - label.margin.w;
            string full = MixedText(string.IsNullOrEmpty(district) ? verb : verb + " · " + district, fa);
            string form = "one";
            float size = LargestFooterFit(label, full, inner, lineHeight, floor, primaryMax, false);
            string chosen = full;
            if (size < primaryMin && !string.IsNullOrEmpty(district))
            {
                float split = fa ? 0f : LargestFooterFit(label, verb + "\n" + district, inner, lineHeight, floor, primaryMax, true);
                if (split > size && split >= floor)
                {
                    size = split;
                    chosen = verb + "\n" + district;
                    form = "split";
                }
                else if (size < floor)
                {
                    chosen = MixedText(district, fa);
                    size = Mathf.Max(floor, LargestFooterFit(label, chosen, inner, lineHeight, floor, primaryMax, false));
                    form = "district";
                }
            }

            if (size < floor)
                size = floor;
            bool fitsPrimary = size >= primaryMin && form == "one";
            _zoomFitSize = fitsPrimary ? 0f : size;
            SetCopy(label, chosen, fitsPrimary ? primaryMin : size, fitsPrimary ? primaryMax : size);
            V3LocalizedTextBindingView zoomBinding = label.GetComponent<V3LocalizedTextBindingView>();
            if (zoomBinding != null) // SetCopy lifts the min to the 22 px library floor; the command floor here is 16
                zoomBinding.ConfigureFontBounds(fitsPrimary ? primaryMin : size, fitsPrimary ? primaryMax : size);
            label.enableAutoSizing = fitsPrimary;
            label.fontSizeMin = fitsPrimary ? primaryMin : size;
            label.fontSizeMax = fitsPrimary ? primaryMax : size;
            label.fontSize = fitsPrimary ? Mathf.Min(primaryMax, size) : size;
            label.textWrappingMode = form == "split" ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.maxVisibleLines = form == "split" ? 2 : 1;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.isRightToLeftText = ContainsArabicScript(label.text);
            Debug.Log("[ZoomLabel] form=" + form + " font=" + size.ToString("0.0") + " inner=" + inner.ToString("0") +
                " button=" + buttonWidth.ToString("0") + " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>Largest size in [floor, max] (1 px steps) at which every line fits; floor - 1 when none does.</summary>
        private float LargestFooterFit(TMP_Text label, string copy, float inner, float lineHeight, float floor, float max, bool twoLines)
        {
            if (label == null || string.IsNullOrEmpty(copy) || inner < 8f)
                return floor - 1f;
            SetCopy(label, copy, floor, max);
            string shaped = label.text;
            string[] lines = shaped.Split('\n');
            bool autosize = label.enableAutoSizing;
            float held = label.fontSize;
            label.enableAutoSizing = false;
            float found = floor - 1f;
            for (float size = Mathf.Floor(max); size >= floor - 0.01f; size -= 1f)
            {
                label.fontSize = size;
                bool fits = true;
                float total = 0f;
                for (int i = 0; i < lines.Length && fits; i++)
                {
                    Vector2 natural = label.GetPreferredValues(lines[i], 9999f, 9999f);
                    fits = natural.x <= inner + 0.5f;
                    total += natural.y;
                }

                if (fits && (!twoLines || total <= lineHeight + 0.5f) && (twoLines || size * 1.2f <= lineHeight + 0.5f))
                {
                    found = size;
                    break;
                }
            }

            label.fontSize = held;
            label.enableAutoSizing = autosize;
            return found;
        }

        private Button backButtonRef() => transform.Find("SkirmishSetupComposition/BackButton")?.GetComponent<Button>();

        /// <summary>
        /// "ZOOM IN · INDUSTRIAL BASIN" is wider than a 4:3 footer button.
        /// Autosize stays at or above the 16px command floor. When one line does not
        /// fit there, the label breaks at the dot. The district name is the shorter form.
        /// </summary>
        private static void FitZoomInLabel(Button button, float buttonWidth)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null || string.IsNullOrEmpty(label.text) || label.text.IndexOf("ZOOM IN", StringComparison.Ordinal) < 0)
                return;
            const float floor = 16f;
            string source = label.text.Replace("\n", " ");
            int dot = source.IndexOf('·');
            float inner = buttonWidth - 16f;
            string district = dot > 0 ? source.Substring(dot + 1).Trim() : source;
            bool oneLine = MeasureFooterLine(source, floor) <= inner + 0.5f;
            bool split = !oneLine && dot > 0 && FooterLinesFit(
                source.Substring(0, dot).Trim() + "\n" + district, floor, inner);
            if (!oneLine && split)
                source = source.Substring(0, dot).Trim() + "\n" + district;
            else if (!oneLine)
                source = district;
            float max = floor;
            for (float size = 22f; size >= floor - 0.01f; size -= 1f)
            {
                if (!FooterLinesFit(source, size, inner))
                    continue;
                max = size;
                break;
            }

            label.enableAutoSizing = true;
            label.fontSizeMin = floor;
            label.fontSizeMax = max;
            label.fontSize = max;
            label.textWrappingMode = source.IndexOf('\n') >= 0 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.maxVisibleLines = source.IndexOf('\n') >= 0 ? 2 : 1;
            label.alignment = TextAlignmentOptions.Center;
            label.text = source;
        }

        private static bool FooterLinesFit(string source, float size, float inner)
        {
            if (string.IsNullOrEmpty(source) || inner < 8f)
                return false;
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (MeasureFooterLine(lines[i], size) > inner + 0.5f)
                    return false;
            }

            return true;
        }

        private static float MeasureFooterLine(string text, float size)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                float factor = text[i] <= 127 ? 0.62f : 0.95f;
                width += size * factor;
            }

            return width;
        }

        private static void FitFooterLabel(Button button, bool fa, float buttonWidth)
        {
            if (button == null)
                return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            label.alignment = TextAlignmentOptions.Center;
            if (fa)
                label.isRightToLeftText = ContainsArabicScript(label.text);
            label.textWrappingMode = buttonWidth < 150f ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 20f;
            label.margin = new Vector4(2f, 1f, 2f, 1f);
        }

        private static void HideFooterChevrons(Button button)
        {
            if (button == null)
                return;
            Transform[] children = button.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null || children[i] == button.transform)
                    continue;
                if (children[i].name.IndexOf("Chevron", StringComparison.Ordinal) < 0)
                    continue;
                children[i].gameObject.SetActive(false);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text != null &&
                (label.text.IndexOf('›') >= 0 || label.text.IndexOf('‹') >= 0))
            {
                label.text = label.text.Replace("›", string.Empty).Replace("‹", string.Empty).Trim();
            }
        }

        private void PresentLibraryFooter(bool fa, bool battlefield)
        {
            if (_savedMatchAvailable)
            {
                BindFooter(resetButton, ResetToDefaults, null);
                BindFooter(launchButton, LaunchMatch, null);
                BindFooter(randomizeSeedButton, RandomizeSeed, null);
                return;
            }

            BindFooter(resetButton, OnLibraryFilters, fa ? "پالایه" : "FILTERS");
            BindFooter(_libraryAdvancedButton, OnLibraryQuickBattle, fa ? "نبرد سریع" : "QUICK BATTLE");
            if (_libraryAdvancedButton != null) _libraryAdvancedButton.interactable = HasQuickBattleTarget();
            if (battlefield)
            {
                BindFooter(randomizeSeedButton, OnLibraryTheater, fa ? "نقشه صحنه" : "THEATRE MAP");
                BindFooter(launchButton, LaunchMatch, fa ? "شروع نبرد" : "START BATTLE");
                if (_quickBattleDisabledLaunch && launchButton != null)
                    launchButton.interactable = true; // v37 P1: undo the Theatre-only Quick Battle disabled state
                _quickBattleDisabledLaunch = false;
            }
            else
            {
                BindFooter(randomizeSeedButton, OnLibraryZoom, ZoomFooterLabel(fa)); // approved-1: "ZOOM IN · DESERT BASE"
                BindFooter(launchButton, OnLibraryQuickBattle, fa ? "نبرد سریع" : "QUICK BATTLE");
                // v37 P1: Quick Battle is live only when the ledger has an owned playable battle (disabled state otherwise).
                bool quickReady = HasQuickBattleTarget();
                if (launchButton != null)
                {
                    launchButton.interactable = quickReady;
                    _quickBattleDisabledLaunch = !quickReady;
                }
            }

            ClearFooterIcon(resetButton);
            ClearFooterIcon(_libraryAdvancedButton);
            ClearFooterIcon(randomizeSeedButton);
        }

        private string ZoomFooterLabel(bool fa)
        {
            ZoomFooterParts(fa, out string verb, out string district);
            if (string.IsNullOrEmpty(district))
                return verb;
            return MixedText(verb + " · " + district, fa);
        }

        /// <summary>The Zoom In verb and the selected district name (upper case in en), district empty when unknown.</summary>
        private void ZoomFooterParts(bool fa, out string verb, out string district)
        {
            verb = fa ? "نزدیک‌تر" : "ZOOM IN";
            district = string.Empty;
            if (_libraryViewModel == null)
                return;
            SkirmishTheaterState theater = _libraryViewModel.Theater;
            if (!TryFindTheaterPin(theater, theater.SelectedMapId, out SkirmishTheaterPinState pin))
                return;
            string map = MapDisplayTitle(pin.MapId, fa);
            if (!string.IsNullOrEmpty(map))
                district = fa ? map : map.ToUpperInvariant();
        }

        private void BindFooter(Button button, UnityEngine.Events.UnityAction action, string label)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(ResetToDefaults);
            button.onClick.RemoveListener(RandomizeSeed);
            button.onClick.RemoveListener(LaunchMatch);
            button.onClick.RemoveListener(OnLibraryFilters);
            button.onClick.RemoveListener(OnLibraryAdvanced);
            button.onClick.RemoveListener(OnLibraryZoom);
            button.onClick.RemoveListener(OnLibraryTheater);
            button.onClick.RemoveListener(OnLibraryQuickBattle);
            button.onClick.AddListener(action);
            if (!string.IsNullOrEmpty(label))
                SetCopy(button.GetComponentInChildren<TMP_Text>(true), label);
            if (action == OnLibraryZoom)
                AliasControl(button, SkirmishLibraryControlNames.ZoomIn);
            else if (action == OnLibraryQuickBattle)
                AliasControl(button, SkirmishLibraryControlNames.QuickBattle);
            else if (action == OnLibraryFilters)
                AliasControl(button, SkirmishLibraryControlNames.Filters);
            else if (action == OnLibraryTheater)
                AliasControl(button, SkirmishLibraryControlNames.TheaterMap);
            else if (action == LaunchMatch)
                AliasControl(button, SkirmishLibraryControlNames.StartBattle);

            bool localSound = action == OnLibraryFilters || action == OnLibraryAdvanced;
            if (localSound)
                AttachAudio(button, UIAudioEventKind.ButtonSecondaryClick);
            else
                AttachDisabledTap(button, UIAudioEventKind.ButtonDisabledTap); // v41 audit: enabled click is the view model's sound; disabled START/QUICK BATTLE taps are heard
        }

        private static void ClearButtonAudio(Button button)
        {
            if (button == null)
                return;
            UIButtonAudioEventView audio = button.GetComponent<UIButtonAudioEventView>();
            if (audio != null)
                DestroyUi(audio);
        }

        private void OnLibraryFilters()
        {
            _filtersOpen = !_filtersOpen;
            PresentBattleLibrary();
        }

        private void OnLibraryAdvanced()
        {
            _advancedOpen = !_advancedOpen;
            PresentBattleLibrary();
        }

        private void OnLibraryZoom()
        {
            EnsureLibraryViewModel();
            _libraryViewModel.ZoomIn();
            _mapFilter = LibraryMapId;
            PresentScenarioChoices();
        }

        private void OnLibraryTheater()
        {
            EnsureLibraryViewModel();
            _libraryViewModel.BackToTheater();
            PresentScenarioChoices();
        }

        private bool _quickBattleDisabledLaunch;

        private bool HasQuickBattleTarget()
        {
            EnsureLibraryViewModel();
            SkirmishTheaterPinState[] pins = _libraryViewModel != null ? _libraryViewModel.Theater.Pins : null;
            for (int i = 0; pins != null && i < pins.Length; i++)
            {
                if (pins[i].HasOwnedPlayable && pins[i].PublishedCount > 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// v37 P1 (Game PM no-stub rule): Quick Battle picks a random owned playable battle from the ledger
        /// (view model QuickBattle raises the primary-click sound), adopts it, then takes the real START BATTLE
        /// launch path (LaunchMatch -> view model Launch). With nothing playable the button is disabled, never a stub.
        /// </summary>
        private void OnLibraryQuickBattle()
        {
            EnsureLibraryViewModel();
            string catalogId = _libraryViewModel.QuickBattle();
            if (string.IsNullOrEmpty(catalogId))
            {
                UIAudioEventGateway.Raise(UIAudioEventKind.CardLocked);
                return;
            }

            // v38 fix: select exactly as a card tap does (SelectScenarioId also records the launch selection that
            // LaunchMatch checks; AdoptLibrarySelection did not, so LaunchMatch returned early and nothing launched).
            SelectScenarioId(catalogId);
            LibraryLaunchRequested = false;
            LaunchMatch();
            Debug.Log("[QuickBattle] scenario=" + catalogId + " selected=" + _selectedScenarioId + " launchRequested=" + LibraryLaunchRequested);
        }

        private void EnsureFilterPanel(RectTransform panel)
        {
            if (panel == null || panel.Find("ArmyGround") != null)
                return;
            TintPanel(panel.gameObject);
            CreateFilterButton(panel, "ArmyGround", "G", 12f, 64f);
            CreateFilterButton(panel, "ArmyAir", "A", 150f, 64f);
            CreateFilterButton(panel, "ArmyCombined", "C", 288f, 64f);
            CreateFilterButton(panel, "StartField", "F", 12f, 128f);
            CreateFilterButton(panel, "StartEstablished", "E", 150f, 128f);
            var missing = CreateChromeButton(panel, "MissingMedal");
            PlaceTopLeft(missing.GetComponent<RectTransform>(), 12f, 192f, 280f, 52f);
            var label = CreateBoundLabel(missing.transform, "Label", "ui.skirmish.library_missing_medal", "MISSING MEDAL");
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            missing.onClick.AddListener(OnFilterMissing);
        }

        private void CreateFilterButton(RectTransform panel, string name, string code, float x, float y)
        {
            var button = CreateChromeButton(panel, name);
            PlaceTopLeft(button.GetComponent<RectTransform>(), x, y, 130f, 52f);
            bool army = code == "G" || code == "A" || code == "C";
            SkirmishBattleLibraryArt.ProfileCopy(code, false, out string english);
            string key = army
                ? "ui.skirmish.library_army_" + code.ToLowerInvariant()
                : "ui.skirmish.library_start_" + code.ToLowerInvariant();
            var label = CreateBoundLabel(button.transform, "Label", key, english);
            Stretch(label.rectTransform, 4f, 4f, 4f, 4f);
            string captured = code;
            if (army)
                button.onClick.AddListener(() => OnFilterArmy(captured));
            else
                button.onClick.AddListener(() => OnFilterStart(captured));
        }

        private void EnsureFooterPlate(Transform composition)
        {
            if (composition == null || composition.Find("LibraryFooterPlate") != null)
                return;
            var plate = new GameObject("LibraryFooterPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic)).GetComponent<RectTransform>();
            plate.SetParent(composition, false);
            V3GradientGraphic graphic = plate.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(new Color(0.04f, 0.07f, 0.08f, 0.96f), new Color(0.02f, 0.03f, 0.04f, 0.96f), new Color(0.93f, 0.78f, 0.38f, 1f), 2f);
            var shadow = new GameObject("LibraryFooterShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            shadow.SetParent(composition, false);
            shadow.SetSiblingIndex(plate.GetSiblingIndex());
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            image.raycastTarget = false;
        }

        private void PlaceFooterPlate(RectTransform composition)
        {
            if (composition == null)
                return;
            EnsureFooterPlate(composition);
            PinFooterBand(composition.Find("LibraryFooterShadow") as RectTransform, 18f, 6f, 112f);
            PinFooterBand(composition.Find("LibraryFooterPlate") as RectTransform, 16f, 10f, 108f);
            HideStrayChevrons(composition);
        }

        private static void PinFooterBand(RectTransform rect, float inset, float bottom, float tall)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(-inset * 2f, tall);
        }

        private static void ReserveFooterBand(Transform composition)
        {
            if (composition is not RectTransform compositionRect)
                return;
            float height = compositionRect.rect.height;
            if (height < 200f)
                return;
            const float footerBand = 136f;
            float width = compositionRect.rect.width;
            ShrinkAboveFooter(composition.Find("OperationPreview") as RectTransform, height, width, footerBand);
            ShrinkAboveFooter(composition.Find("BaseAssaultRules") as RectTransform, height, width, footerBand);
        }

        private static void ShrinkAboveFooter(RectTransform rect, float compositionHeight, float compositionWidth, float footerBand)
        {
            if (rect == null)
                return;
            float top = -rect.anchoredPosition.y;
            float left = rect.anchoredPosition.x;
            float maxHeight = compositionHeight - footerBand - top;
            float maxWidth = compositionWidth - 14f - left;
            float nextWidth = rect.sizeDelta.x;
            float nextHeight = rect.sizeDelta.y;
            if (maxHeight > 120f && nextHeight > maxHeight)
                nextHeight = maxHeight;
            if (maxWidth > 120f && nextWidth > maxWidth)
                nextWidth = maxWidth;
            rect.sizeDelta = new Vector2(nextWidth, nextHeight);
        }

        /// <summary>
        /// v41 Game PM: FAVOURITES is hidden for this merge (no favourites data, no disabled stub). Older generated
        /// prefabs still carry the button, so it is removed at build time and the footer reflows without it.
        /// </summary>
        private void RemoveRetiredFooterUtility(Transform parent, string name)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            if (existing != null)
                DestroyUi(existing.gameObject);
        }

        private Button EnsureFooterUtility(Transform parent, string name)
        {
            if (parent == null)
                return null;
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<Button>();
            Button button = CreateChromeButton(parent, name);
            var label = CreateLabel(button.transform, "Label", 18f);
            Stretch(label.rectTransform, 48f, 12f, 8f, 8f);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static void ClearFooterIcon(Button button)
        {
            if (button == null)
                return;
            Transform existing = button.transform.Find("LibraryIcon");
            if (existing != null)
                DestroyUi(existing.gameObject);
        }

        private static void HideDiceIcon(Button button)
        {
            if (button == null)
                return;
            Transform icon = button.transform.Find("Icon");
            if (icon != null && icon.name != "LibraryIcon")
                icon.gameObject.SetActive(false);
            Transform[] children = button.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null)
                    continue;
                if (children[i].name.StartsWith("Dot_", StringComparison.Ordinal) || children[i].name == "DiceIcon")
                    children[i].gameObject.SetActive(false);
            }
        }

        private void StyleCommandButtons(bool battlefield)
        {
            TintCommand(transform.Find("SkirmishSetupComposition/BackButton")?.GetComponent<Button>()
                ?? FindDeep(transform, "BackButton")?.GetComponent<Button>(),
                new Color(0.16f, 0.48f, 0.78f, 1f), new Color(0.06f, 0.22f, 0.42f, 1f));
            if (_savedMatchAvailable)
                return;
            TintCommand(resetButton, new Color(0.16f, 0.4f, 0.62f, 1f), new Color(0.06f, 0.18f, 0.32f, 1f));
            TintCommand(_libraryAdvancedButton, new Color(0.22f, 0.32f, 0.36f, 1f), new Color(0.08f, 0.14f, 0.16f, 1f));
            TintCommand(randomizeSeedButton, new Color(0.14f, 0.46f, 0.7f, 1f), new Color(0.05f, 0.2f, 0.36f, 1f));
            TintCommand(launchButton, new Color(0.24f, 0.58f, 0.18f, 1f), new Color(0.08f, 0.28f, 0.08f, 1f));

            EnlargeButtonLabel(resetButton);
            EnlargeButtonLabel(_libraryAdvancedButton);
            EnlargeButtonLabel(randomizeSeedButton);
            EnlargeButtonLabel(launchButton);
        }

        private void EnlargeButtonLabel(Button button)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label == null)
                return;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 26f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        private static void TintCommand(Button button, Color top, Color bottom)
        {
            V3GradientGraphic gradient = button != null ? button.targetGraphic as V3GradientGraphic : null;
            if (gradient == null && button != null)
                gradient = button.GetComponent<V3GradientGraphic>();
            if (gradient == null)
                return;
            gradient.Configure(top, bottom, new Color(0.23f, 0.29f, 0.31f, 1f), 2f);
            SkirmishLibraryButtonStateView state = button.GetComponent<SkirmishLibraryButtonStateView>();
            if (state == null)
                state = button.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
            state.Bind(gradient);
            state.ApplyPalette(top, bottom, new Color(0.23f, 0.29f, 0.31f, 1f), button.interactable, false);
        }
    }
}
