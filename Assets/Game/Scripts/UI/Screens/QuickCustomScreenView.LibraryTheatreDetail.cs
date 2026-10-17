using System;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        public const string TheatreDetailPanelName = "TheatreDetailPanel";

        /// <summary>Last placed Theatre detail panel rect in plate coordinates (top-left origin, y down). For overlap checks.</summary>
        public Rect TheatreDetailRect { get; private set; }

        /// <summary>Width the Theatre detail panel takes on the trailing edge of the plate; zone cards and cues stay outside it.</summary>
        private static float TheatreDetailReserve(float plateWidth)
        {
            if (plateWidth < 600f)
                return 0f;
            return Mathf.Clamp(plateWidth * 0.22f, 230f, 290f);
        }

        /// <summary>Shrinks the Theatre safe rect so cards and cues avoid the detail panel (trailing side: right in LTR, left in RTL).</summary>
        /// <summary>
        /// Panel mode. P2's seating records whether it reserved the trailing column; when it did not, the panel is a
        /// bottom sheet.
        /// </summary>
        private static bool TheatreDetailReservedLocal()
        {
            return TheatreSeatGeometry.TheatreDetailReserved;
        }

        /// <summary>approved-1 right panel: zone title, plate, battles/won line, medal cabinet and objective types.</summary>
        private void PresentTheatreDetailPanel(SkirmishTheaterState theater, bool fa, float plateWidth, float plateHeight)
        {
            if (_libraryRegions == null)
                return;
            Transform old = _libraryRegions.Find(TheatreDetailPanelName);
            if (old != null)
                DestroyUi(old.gameObject);
            float reserve = TheatreDetailReserve(plateWidth);
            if (reserve <= 0f || !TryFindTheaterPin(theater, theater.SelectedMapId, out SkirmishTheaterPinState pin))
            {
                TheatreDetailRect = Rect.zero;
                return;
            }

            bool bottomSheet = !TheatreDetailReservedLocal();
            const float margin = 10f;
            // Keep the panel inside what is actually drawn: the regions rect can run past the plate frame's mask and the
            // device safe area, which clipped the trailing edge in v7.
            VisibleRegionSpan(plateWidth, out float visibleMin, out float visibleMax);
            Rect frame;
            if (bottomSheet)
            {
                float sheetHeight = Mathf.Clamp(plateHeight * 0.34f, 150f, 210f);
                frame = new Rect(visibleMin + margin, plateHeight - margin - sheetHeight, visibleMax - visibleMin - margin * 2f, sheetHeight);
            }
            else
            {
                float width = Mathf.Min(reserve - margin * 2f, visibleMax - visibleMin - margin * 2f);
                float height = Mathf.Max(200f, plateHeight - margin * 2f);
                frame = new Rect(fa ? visibleMin + margin : visibleMax - margin - width, margin, width, height);
            }

            TheatreDetailRect = frame;
            var panel = new GameObject(TheatreDetailPanelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            panel.transform.SetParent(_libraryRegions, false);
            PlaceTopLeft(panel.rectTransform, frame.x, frame.y, frame.width, frame.height);
            panel.color = new Color(0.03f, 0.05f, 0.06f, 0.94f);
            panel.raycastTarget = true;
            panel.transform.SetAsLastSibling();
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.47f, 0.28f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = bottomSheet ? new Vector2(0f, 8f) : new Vector2(fa ? 8f : -8f, -10f);
            RectTransform root = panel.rectTransform;
            const float pad = 12f;

            if (bottomSheet)
            {
                // Bottom sheet: [title, stats, preview] | [medal cabinet 8 x 3] | [objective types 2 x 2], mirrored in RTL.
                float innerW = frame.width - pad * 2f;
                float innerH = frame.height - pad * 2f;
                float lead = innerW * 0.3f;
                float middle = innerW * 0.4f;
                float trail = innerW - lead - middle;
                float leadX = fa ? pad + middle + trail : pad;
                float middleX = pad + (fa ? trail : lead);
                float trailX = fa ? pad : pad + lead + middle;
                float y = pad;
                y = DetailHeader(root, pin, fa, leadX + 4f, y, lead - 12f);
                DetailPreview(root, pin, leadX + 4f, y, lead - 12f, Mathf.Max(40f, pad + innerH - y));
                DetailCabinet(root, pin, fa, middleX + 6f, pad, middle - 12f, innerH, 8, 3);
                DetailObjectives(root, fa, trailX + 6f, pad, trail - 12f, innerH);
                return;
            }

            {
                float inner = frame.width - pad * 2f;
                float y = pad;
                y = DetailHeader(root, pin, fa, pad, y, inner, frame.height);
                float objectivesHeight = 32f + 2f * DetailObjectiveRowHeight;
                float objectivesTop = frame.height - pad - objectivesHeight;
                float cabinetHeight = Mathf.Max(60f, Mathf.Min(objectivesTop - y - 10f, inner * 0.5f + 12f));
                y = DetailCabinet(root, pin, fa, pad, y, inner, cabinetHeight, 6, 3) + 10f;
                // The zone's own subtitle copy fills the gap between cabinet and objective types (no invented data).
                y = DetailBlurb(root, pin, fa, pad, y, inner, objectivesTop - y - 8f - DetailMedalRowHeight);
                if (objectivesTop - y >= DetailMedalRowHeight)
                    y = DetailMedalRow(root, pin, fa, pad, Mathf.Max(y, objectivesTop - DetailMedalRowHeight - 4f), inner);
                DetailObjectives(root, fa, pad, Mathf.Max(y, objectivesTop), inner, objectivesHeight);
            }
        }

        private const float DetailObjectiveRowHeight = 66f;

        /// <summary>
        /// Runs after the library text passes (which hold text at the 22 px floor): the panel title and the
        /// OBJECTIVE TYPE heading stay on one line and auto-size down to fit the narrow 4:3 panel.
        /// </summary>
        private void FitTheatreDetailHeadings()
        {
            Transform panel = _libraryRegions != null ? _libraryRegions.Find(TheatreDetailPanelName) : null;
            if (panel == null)
                return;
            FitDetailLine(panel.Find("Title")?.GetComponent<TMP_Text>(), 18f, 32f); // type scale up (Game PM)
            FitDetailLine(panel.Find("ObjectiveHeading")?.GetComponent<TMP_Text>(), 16f, 24f);
            for (int i = 0; i < panel.childCount; i++)
            {
                Transform child = panel.GetChild(i);
                if (child.name == "Subtitle")
                {
                    TMP_Text sub = child.GetComponent<TMP_Text>();
                    FitDetailLine(sub, 14f, 22f); // objective names, past the text pass's subtitle floor
                    if (sub != null)
                        sub.color = new Color(0.96f, 0.94f, 0.88f, 1f); // approved-1: white detail lines, not grey
                }
            }
        }

        private static void FitDetailLine(TMP_Text text, float min, float max)
        {
            if (text == null)
                return;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 1;
            text.enableAutoSizing = false;
            float width = text.rectTransform.rect.width;
            float size = max;
            text.fontSize = size;
            float natural = text.GetPreferredValues(text.text, 9999f, 100f).x;
            if (natural > width - 2f && natural > 1f)
                size = Mathf.Max(min, Mathf.Floor(max * (width - 2f) / natural));
            text.fontSize = size;
            text.fontSizeMin = size;
            text.fontSizeMax = size;
            V3LocalizedTextBindingView binding = text.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            text.ForceMeshUpdate();
        }

        /// <summary>Side panel header: title, preview, stats, rule. Returns the next y.</summary>
        private float DetailHeader(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width, float panelHeight)
        {
            y = DetailTitleAndStats(root, pin, fa, x, y, width, false);
            float previewHeight = Mathf.Clamp(width * 0.9f, 90f, Mathf.Max(90f, panelHeight * 0.4f)); // approved-1: large plate, grown to fill the panel
            DetailPreview(root, pin, x, y, width, previewHeight);
            y += previewHeight + 8f;
            y = DetailStats(root, pin, fa, x, y, width);
            var rule = new GameObject("Rule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            rule.transform.SetParent(root, false);
            PlaceTopLeft(rule.rectTransform, x, y, width, 1.5f);
            rule.color = new Color(0.55f, 0.47f, 0.28f, 0.6f);
            rule.raycastTarget = false;
            return y + 8f;
        }

        /// <summary>Bottom-sheet header: title and stats stacked. Returns the next y.</summary>
        private float DetailHeader(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width)
        {
            y = DetailTitleAndStats(root, pin, fa, x, y, width, true);
            return DetailStats(root, pin, fa, x, y, width);
        }

        private float DetailTitleAndStats(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width, bool compact)
        {
            TMP_Text title = CreateLabel(root, "Title", 26f);
            PlaceTopLeft(title.rectTransform, x, y, width, 38f);
            // FA line metrics run taller than the box; the next block starts 12 px below so the ink never meets the art.
            SetCopy(title, MapDisplayTitle(pin.MapId, fa), LibraryFontFloor, compact ? 24f : 28f);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.isRightToLeftText = fa;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            return y + 50f;
        }

        private const bool HideDetailStatsLine = true;

        private float DetailStats(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width)
        {
            // PM: the battles/time line is hidden entirely until the data exists (no stub).
            if (HideDetailStatsLine)
                return y;
            TMP_Text stats = CreateLabel(root, "Stats", 22f);
            PlaceTopLeft(stats.rectTransform, x, y, width, 30f);
            SetCopy(stats, TheatreBattlesWonCopy(pin.PublishedCount, pin.WonCount, fa), LibraryFontFloor, 22f);
            stats.fontStyle = FontStyles.Normal;
            stats.color = new Color(0.86f, 0.86f, 0.82f, 1f);
            stats.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            stats.isRightToLeftText = fa;
            return y + 34f;
        }

        private void DetailPreview(RectTransform root, SkirmishTheaterPinState pin, float x, float y, float width, float height)
        {
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(root, false);
            PlaceTopLeft(preview.rectTransform, x, y, width, height);
            preview.texture = ResolveGridPreview(pin.PlatePreviewKey, null, pin.MapId, out _);
            preview.raycastTarget = false;
            if (preview.texture != null)
                SkirmishBattleCardWidget.FitCover(preview, width, height);
        }

        private const float DetailMedalRowHeight = 84f; // 2x2: four cells collide in the narrow side panel

        /// <summary>Per-zone medal row: the zone's own ledger counts per tier (pin.Medals), icon + count.</summary>
        private float DetailMedalRow(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width)
        {
            SkirmishDifficultyId[] tiers = { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander };
            int[] counts = { pin.Medals.Recruit, pin.Medals.Regular, pin.Medals.Veteran, pin.Medals.Commander };
            float cellW = width / 2f;
            for (int i = 0; i < tiers.Length; i++)
            {
                int col = i % 2, row = i / 2;
                int visual = fa ? 1 - col : col;
                float cx = x + visual * cellW;
                float cy = y + row * 42f;
                var icon = new GameObject("ZoneMedal_" + tiers[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(root, false);
                PlaceTopLeft(icon.rectTransform, cx + 4f, cy + 5f, 32f, 32f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                ShowIcon(icon, SkirmishBattleLibraryArt.Medal(true, tiers[i]), Color.white);
                TMP_Text count = CreateLabel(root, "Value", 24f);
                PlaceTopLeft(count.rectTransform, cx + 40f, cy + 1f, Mathf.Max(16f, cellW - 46f), 40f);
                SetCopy(count, fa ? SkirmishWidgetContext.ToPersianDigits(counts[i].ToString()) : counts[i].ToString(), LibraryFontFloor, 26f);
                count.alignment = TextAlignmentOptions.MidlineLeft;
                count.isRightToLeftText = false;
                count.fontStyle = FontStyles.Bold;
                count.enableAutoSizing = false; // past the Arabic line-box cap for Persian digits
                count.fontSize = 26f;
                count.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            }
            return y + DetailMedalRowHeight;
        }

        private float DetailBlurb(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width, float room)
        {
            string copy = string.IsNullOrEmpty(pin.SubtitleLocKey) ? string.Empty : LibraryText(pin.SubtitleLocKey, fa);
            if (string.IsNullOrWhiteSpace(copy) || room < 34f)
                return y;
            TMP_Text blurb = CreateLabel(root, "ZoneBlurb", 22f);
            float height = Mathf.Min(room, 112f);
            PlaceTopLeft(blurb.rectTransform, x, y, width, height);
            SetCopy(blurb, copy, LibraryFontFloor, 22f);
            blurb.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            blurb.isRightToLeftText = fa;
            blurb.textWrappingMode = TextWrappingModes.Normal;
            blurb.overflowMode = TextOverflowModes.Ellipsis;
            blurb.maxVisibleLines = Mathf.Max(1, Mathf.FloorToInt(height / 28f));
            blurb.color = new Color(0.8f, 0.8f, 0.76f, 1f);
            return y + height + 6f;
        }

        /// <summary>Medal cabinet: earned medals first (commander → recruit), the rest empty. Returns the bottom y.</summary>
        private float DetailCabinet(RectTransform root, SkirmishTheaterPinState pin, bool fa, float x, float y, float width, float height, int columns, int rows)
        {
            const float gap = 5f;
            float cell = Mathf.Min((width - gap * (columns - 1)) / columns, (height - gap * (rows - 1)) / rows);
            float gridWidth = cell * columns + gap * (columns - 1);
            float gridX = x + (width - gridWidth) * 0.5f;
            SkirmishDifficultyId[] tiers = { SkirmishDifficultyId.Commander, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Recruit };
            int[] counts = { pin.Medals.Commander, pin.Medals.Veteran, pin.Medals.Regular, pin.Medals.Recruit };
            int slot = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int visual = fa ? columns - 1 - c : c;
                    var box = new GameObject("Cabinet_" + slot, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    box.transform.SetParent(root, false);
                    PlaceTopLeft(box.rectTransform, gridX + visual * (cell + gap), y + r * (cell + gap), cell, cell);
                    box.color = new Color(0.08f, 0.1f, 0.11f, 1f);
                    box.raycastTarget = false;
                    var edge = box.gameObject.AddComponent<Outline>();
                    edge.effectColor = new Color(0.3f, 0.32f, 0.33f, 0.9f);
                    edge.effectDistance = new Vector2(1f, -1f);
                    int remaining = slot;
                    for (int t = 0; t < tiers.Length; t++)
                    {
                        if (remaining < counts[t])
                        {
                            var medal = new GameObject("Medal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                            medal.transform.SetParent(box.transform, false);
                            PlaceTopLeft(medal.rectTransform, cell * 0.12f, cell * 0.12f, cell * 0.76f, cell * 0.76f);
                            medal.preserveAspect = true;
                            ShowIcon(medal, SkirmishBattleLibraryArt.Medal(true, tiers[t]), Color.white);
                            break;
                        }

                        remaining -= counts[t];
                    }

                    slot++;
                }
            }

            return y + rows * (cell + gap) - gap;
        }

        /// <summary>
        /// "OBJECTIVE TYPE" heading and a 2 x 2 block of icon + short name. The library text pass holds captions at the
        /// subtitle floor (~20 px), so each name gets half the panel width and its own row instead of four across.
        /// </summary>
        private void DetailObjectives(RectTransform root, bool fa, float x, float y, float width, float height)
        {
            TMP_Text heading = CreateLabel(root, "ObjectiveHeading", 22f);
            PlaceTopLeft(heading.rectTransform, x, y, width, 30f);
            SetCopy(heading, fa ? "نوع هدف" : "OBJECTIVE TYPE", LibraryFontFloor, 22f);
            heading.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            heading.isRightToLeftText = fa;
            heading.fontStyle = FontStyles.Bold;
            y += 32f;
            string[] objectives = SkirmishBattleLibraryArt.ObjectiveIds;
            float cellWidth = width * 0.5f;
            float rowHeight = Mathf.Min(DetailObjectiveRowHeight, Mathf.Max(48f, (height - 32f) * 0.5f));
            float iconSize = Mathf.Max(18f, Mathf.Min(32f, rowHeight - 38f)); // leaves 36 px under the icon for FA descenders
            for (int i = 0; i < objectives.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                int visual = fa ? 1 - column : column;
                float cellX = x + visual * cellWidth;
                float cellY = y + row * rowHeight;
                var icon = new GameObject("ObjectiveType_" + objectives[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(root, false);
                float iconX = fa ? cellX + cellWidth - 6f - iconSize : cellX + 6f;
                PlaceTopLeft(icon.rectTransform, iconX, cellY, iconSize, iconSize);
                icon.preserveAspect = true;
                ShowIcon(icon, SkirmishBattleLibraryArt.ObjectiveIcon(objectives[i]), Color.white);
                SkirmishBattleLibraryArt.ObjectiveCopy(objectives[i], fa, out _, out string shortName);
                TMP_Text caption = CreateLabel(root, "Subtitle", 18f);
                PlaceTopLeft(caption.rectTransform, cellX + 6f, cellY + iconSize + 2f, cellWidth - 12f, 26f);
                SetCopy(caption, shortName, 14f, 20f);
                caption.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                caption.isRightToLeftText = fa;
                caption.textWrappingMode = TextWrappingModes.NoWrap;
                caption.overflowMode = TextOverflowModes.Overflow;
                caption.color = new Color(0.82f, 0.82f, 0.78f, 1f);
            }
        }

        /// <summary>
        /// "1 battle · 1 won" / "24 battles · 9 won". Keys are looked up first so loc can override; FA keeps the noun singular
        /// after a numeral, as Persian grammar does, so both forms share one string.
        /// </summary>
        internal static string TheatreBattlesWonCopy(int battles, int won, bool fa)
        {
            string battlesKey = battles == 1 ? "ui.skirmish.theatre_battles_one" : "ui.skirmish.theatre_battles_other";
            string wonKey = "ui.skirmish.theatre_won";
            string battlesFallback = fa ? "{0} نبرد" : battles == 1 ? "{0} battle" : "{0} battles";
            string wonFallback = fa ? "{0} پیروزی" : "{0} won";
            string battlesFormat = LocalizedFormat(battlesKey, battlesFallback, fa);
            string wonFormat = LocalizedFormat(wonKey, wonFallback, fa);
            string battlesNumber = fa ? SkirmishWidgetContext.ToPersianDigits(battles.ToString()) : battles.ToString();
            string wonNumber = fa ? SkirmishWidgetContext.ToPersianDigits(won.ToString()) : won.ToString();
            // A middle dot reads as the Persian zero (٠) next to Persian digits, so FA separates with the Persian comma.
            return string.Format(battlesFormat, battlesNumber) + (fa ? "، " : " · ") + string.Format(wonFormat, wonNumber);
        }

        private static string LocalizedFormat(string key, string fallback, bool fa)
        {
            string value = null;
            try
            {
                value = GameLocalization.Get(key, string.Empty);
            }
            catch (Exception)
            {
                value = null;
            }

            return string.IsNullOrEmpty(value) || !value.Contains("{0}") ? fallback : value;
        }

        private void VisibleRegionSpan(float plateWidth, out float min, out float max)
        {
            min = 0f;
            max = plateWidth;
            RectTransform regions = _libraryRegions as RectTransform;
            if (regions == null)
                return;
            var corners = new Vector3[4];
            Rect local = regions.rect;
            for (Transform node = regions.parent; node != null; node = node.parent)
            {
                bool clips = node.GetComponent<RectMask2D>() != null || node.GetComponent<Mask>() != null;
                Canvas canvas = node.GetComponent<Canvas>();
                bool root = canvas != null && canvas.isRootCanvas;
                if (!clips && !root)
                    continue;
                ((RectTransform)node).GetWorldCorners(corners);
                float left = regions.InverseTransformPoint(corners[0]).x - local.xMin;
                float right = regions.InverseTransformPoint(corners[2]).x - local.xMin;
                min = Mathf.Max(min, left);
                max = Mathf.Min(max, right);
                if (root)
                {
                    Rect safe = Screen.safeArea;
                    if (Screen.width > 0 && canvas.worldCamera == null)
                    {
                        float scale = (right - left) / Screen.width;
                        min = Mathf.Max(min, left + safe.xMin * scale);
                        max = Mathf.Min(max, left + safe.xMax * scale);
                    }
                    break;
                }
            }

            if (max - min < 120f)
            {
                min = 0f;
                max = plateWidth;
            }
        }
    }
}
