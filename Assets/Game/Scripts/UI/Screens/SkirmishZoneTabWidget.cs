using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// Battlefield zone tabs on the approved-3 grid.
    /// The mockup uses equal-width bars, a warm idle fill, and a lighter selected bar with a gold edge.
    /// Height stays under the 64px trial: that band left the card preview on the 40% floor.
    /// </summary>
    public static class SkirmishZoneTabWidget
    {
        public const float Height = 52f;
        public const float Gap = 14f;
        public const float OriginX = 12f;
        public const float OriginY = 2f;

        public static float BandBottom => OriginY + Height;

        public static float Width(float gridWidth, int tabCount)
        {
            float span = gridWidth > 240f ? gridWidth - OriginX * 2f : 1008f;
            if (tabCount <= 0)
                return 160f;
            return (span - Gap * (tabCount - 1)) / tabCount;
        }

        public static float Left(int index, float gridWidth, int tabCount)
        {
            return OriginX + index * (Width(gridWidth, tabCount) + Gap);
        }

        public static void Apply(Button button, bool selected)
        {
            if (button == null)
                return;
            SkirmishLibraryButtonStateView state = button.GetComponent<SkirmishLibraryButtonStateView>();
            if (state == null && button.targetGraphic is V3GradientGraphic graphic)
            {
                state = button.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
                state.Bind(graphic);
            }

            if (state == null)
                return;
            if (selected)
            {
                state.ApplyPalette(
                    new Color(0.90f, 0.78f, 0.56f, 1f),
                    new Color(0.68f, 0.54f, 0.36f, 1f),
                    new Color(0.95f, 0.74f, 0.22f, 1f),
                    true,
                    true);
                return;
            }

            state.ApplyPalette(
                new Color(0.58f, 0.52f, 0.46f, 1f),
                new Color(0.36f, 0.31f, 0.27f, 1f),
                new Color(0.40f, 0.34f, 0.28f, 1f),
                true,
                false);
        }

        public const float LabelInset = 4f;
        /// <summary>Name line inside the 52px tab. Two lines stay inside <see cref="Height"/> so the grid preview keeps its 40% floor.</summary>
        public const float NameBand = 32f;
        public const float StatusBand = 20f;
        public const float NameFontFloor = 18f;
        public const float StatusFont = 14f;

        public static void StyleLabel(TMP_Text label)
        {
            if (label == null)
                return;
            label.color = new Color(0.14f, 0.09f, 0.05f, 1f);
        }

        public static string StatusCopy(bool fa, bool pending, int won, int published)
        {
            if (pending)
                return SkirmishBattleLibraryArt.ComingSoon(fa);
            return won.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "/"
                + published.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Keeps the district name on its own line at 18px or larger. A 4:3 section is scaled
        /// below 1, so the local size rises until the drawn size is 18 when the tab is wide enough.
        /// Tracking tightens before the size drops, and the size never drops below 18.
        /// </summary>
        public static void FitName(TMP_Text name)
        {
            if (name == null || name.rectTransform == null)
                return;
            RectTransform rect = name.rectTransform;
            float boxW = Mathf.Max(rect.rect.width, rect.sizeDelta.x);
            float boxH = Mathf.Max(rect.rect.height, rect.sizeDelta.y);
            if (boxW < 8f)
                boxW = 120f;
            if (boxH < 8f)
                boxH = NameBand;
            SyncBox(rect, boxW, boxH);
            float scale = Mathf.Max(0.01f, rect.lossyScale.y);
            float local = Mathf.Max(NameFontFloor, NameFontFloor / scale);
            // Line spacing can pull a 1.65em box into the 32px band only so far. Past this the
            // glyphs themselves are taller than the band, so a broken scale must not chase it.
            float cap = Mathf.Max(NameFontFloor, (boxH - 1f) / 1.17f);
            local = Mathf.Min(local, cap);
            float[] tracks = { 0f, -8f, -16f, -24f, -36f, -48f, -72f, -108f };
            bool fitted = false;
            float size = local;
            while (true)
            {
                float spacing = LineSpacingFor(size, boxH);
                for (int i = 0; i < tracks.Length; i++)
                {
                    ApplyLine(name, size, spacing, tracks[i]);
                    name.ForceMeshUpdate();
                    if (LineFits(name, boxW, boxH))
                    {
                        fitted = true;
                        break;
                    }
                }

                if (fitted || size <= NameFontFloor + 0.01f)
                    break;
                size = Mathf.Max(NameFontFloor, size - 1f);
            }

            V3LocalizedTextBindingView binding = name.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(name.fontSize, name.fontSize);
        }

        public static void FitStatus(TMP_Text status)
        {
            if (status == null)
                return;
            ApplyLine(status, StatusFont, LineSpacingFor(StatusFont, StatusBand), 0f);
            status.alignment = TextAlignmentOptions.Center;
            V3LocalizedTextBindingView binding = status.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(StatusFont, StatusFont);
        }

        public static void Log(TMP_Text name, string mapId, string title)
        {
            if (name == null)
                return;
            bool visible = NameIsVisible(name, title, out bool truncated, out float drawn, out float local);
            string ratio = string.IsNullOrEmpty(TheatreSeatGeometry.ShotRatio) ? "unset" : TheatreSeatGeometry.ShotRatio;
            string locale = string.IsNullOrEmpty(TheatreSeatGeometry.ShotLocale) ? "en" : TheatreSeatGeometry.ShotLocale;
            Debug.Log("[ZoneTab] map=" + (mapId ?? string.Empty)
                + " locale=" + locale
                + " ratio=" + ratio
                + " nameVisible=" + (visible ? "1" : "0")
                + " fontSize=" + drawn.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " local=" + local.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)
                + " truncated=" + (truncated ? "1" : "0"));
        }

        public static bool NameIsVisible(TMP_Text name, string title, out bool truncated, out float drawn, out float local)
        {
            truncated = true;
            drawn = 0f;
            local = 0f;
            if (name == null)
                return false;
            RectTransform rect = name.rectTransform;
            float boxW = 0f;
            float boxH = NameBand;
            if (rect != null)
            {
                boxW = Mathf.Max(rect.rect.width, rect.sizeDelta.x);
                boxH = Mathf.Max(rect.rect.height, rect.sizeDelta.y);
                if (boxH < 8f)
                    boxH = NameBand;
                SyncBox(rect, boxW > 8f ? boxW : rect.sizeDelta.x, boxH);
            }

            name.ForceMeshUpdate();
            local = name.fontSize;
            float scale = rect != null ? Mathf.Max(0.01f, rect.lossyScale.y) : 1f;
            drawn = local * scale;
            bool wide = boxW > 8f && LineWidth(name) > boxW + 2f;
            bool tall = false;
            TMP_TextInfo info = name.textInfo;
            if (info != null && info.lineCount > 0 && info.lineInfo != null && info.lineInfo.Length > 0)
                tall = info.lineInfo[0].lineHeight > boxH + 2f;
            bool covers = SkirmishBattleLibraryArt.SameRenderedText(name.text, title);
            truncated = !covers || wide || tall || name.isTextTruncated;
            return covers && !wide && !tall && !name.isTextTruncated;
        }

        private static void ApplyLine(TMP_Text text, float size, float spacing, float tracking)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.fontSizeMin = size;
            text.fontSizeMax = size;
            text.lineSpacing = spacing;
            text.characterSpacing = tracking;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 1;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static float LineSpacingFor(float size, float band)
        {
            float natural = size * 1.65f;
            if (natural <= band - 1f)
                return 0f;
            return Mathf.Max(-48f, (band - 1f - natural) / Mathf.Max(0.01f, size * 0.01f));
        }

        private static void SyncBox(RectTransform rect, float boxW, float boxH)
        {
            if (Mathf.Abs(rect.rect.width - boxW) <= 0.75f && Mathf.Abs(rect.rect.height - boxH) <= 0.75f)
                return;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, boxW);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, boxH);
        }

        private static bool LineFits(TMP_Text text, float boxW, float boxH)
        {
            if (text == null)
                return false;
            float width = LineWidth(text);
            if (width > boxW + 1.5f)
                return false;
            TMP_TextInfo info = text.textInfo;
            if (info != null && info.lineCount > 0 && info.lineInfo != null && info.lineInfo.Length > 0)
            {
                if (info.lineInfo[0].lineHeight > boxH + 1.5f)
                    return false;
            }

            RectTransform rect = text.rectTransform;
            bool stale = rect != null && (rect.rect.width + 0.75f < boxW || rect.rect.height + 0.75f < boxH);
            if (!stale && (text.isTextTruncated || text.isTextOverflowing))
                return false;
            return true;
        }

        private static float LineWidth(TMP_Text text)
        {
            if (text == null)
                return 0f;
            TMP_TextInfo info = text.textInfo;
            if (info != null && info.lineCount > 0 && info.lineInfo != null && info.lineInfo.Length > 0 && info.lineInfo[0].length > 0.5f)
                return info.lineInfo[0].length;
            float preferred = text.preferredWidth;
            if (preferred < 0.5f && !string.IsNullOrEmpty(text.text))
                preferred = Mathf.Max(preferred, text.GetPreferredValues(text.text).x);
            return preferred;
        }
    }
}
