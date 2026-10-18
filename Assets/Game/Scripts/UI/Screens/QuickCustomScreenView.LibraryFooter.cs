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
                    SetCopy(label, fa ? "پیشرفته" : "ADVANCED", 20f, 28f);
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

        private Button backButtonRef() => transform.Find("SkirmishSetupComposition/BackButton")?.GetComponent<Button>();

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
            BindFooter(_libraryAdvancedButton, OnLibraryAdvanced, fa ? "پیشرفته" : "ADVANCED");
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
            string verb = fa ? "نزدیک‌تر" : "ZOOM IN";
            if (_libraryViewModel == null)
                return verb;
            SkirmishTheaterState theater = _libraryViewModel.Theater;
            if (!TryFindTheaterPin(theater, theater.SelectedMapId, out SkirmishTheaterPinState pin))
                return verb;
            string map = MapDisplayTitle(pin.MapId, fa);
            if (string.IsNullOrEmpty(map))
                return verb;
            return MixedText(verb + " · " + (fa ? map : map.ToUpperInvariant()), fa);
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
            gradient.Configure(top, bottom, new Color(0.93f, 0.78f, 0.38f, 1f), 3f);
            SkirmishLibraryButtonStateView state = button.GetComponent<SkirmishLibraryButtonStateView>();
            if (state == null)
                state = button.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
            state.Bind(gradient);
            state.ApplyPalette(top, bottom, new Color(0.93f, 0.78f, 0.38f, 1f), button.interactable, false);
        }
    }
}
