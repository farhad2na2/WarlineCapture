using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    /// <summary>
    /// v40 (P1) approved-1 Theatre right panel: the selected district's battles as thumbnails with their real medal state,
    /// access (lock) and objective-type icon. Data comes only from the view model: <see cref="SkirmishLibraryViewModel.Battlefield"/>
    /// is built for the selected map (the same map as <see cref="SkirmishTheaterState.SelectedMapId"/>), and each
    /// <see cref="SkirmishBattleCardState"/> carries ScenarioId, ObjectiveId, PreviewSpriteKey, Medals (per-tier mask) and Access.
    /// Icons are the existing Resources/SkirmishBattleLibrary set (medal_*, icon_objective_*, icon_lock).
    /// Grid slots with no catalog entry (no ScenarioId) are not drawn: there is nothing real to show for them.
    /// Called once from PresentTheatreDetailPanel; it takes the space between the panel header and the medal row.
    /// </summary>
    public sealed partial class QuickCustomScreenView
    {
        private const string TheatrePanelBattlesName = "TheatreBattles";
        private const float TheatrePanelSideMinW = 64f;
        private const int TheatrePanelSideMaxColumns = 4;
        private const float TheatrePanelMinThumb = 28f;
        private const float TheatrePanelGap = 4f;
        private const int TheatreCompactColumns = 3;
        private const float TheatreCompactSummaryH = 22f;
        private const float TheatreCompactMinThumb = 20f;
        private const float TheatreCompactMinText = 14f;

        private void BuildTheatrePanel(SkirmishTheaterPinState selectedZone, RectTransform parent)
        {
            if (parent == null || _libraryViewModel == null)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (!IsTheatreBottomSheet(parent))
            {
                PlaceTheatreLowerBlock(parent); // v48: objective block sized from its measured need, bottom-anchored
                LayoutTheatreObjectives(parent, fa);
            }

            Transform old = parent.Find(TheatrePanelBattlesName);
            if (old != null)
                DestroyUi(old.gameObject);

            SkirmishBattlefieldState field = _libraryViewModel.Battlefield;
            if (!string.Equals(field.MapId, selectedZone.MapId, StringComparison.OrdinalIgnoreCase) || field.GridCards == null)
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " skipped=map-mismatch field=" + field.MapId);
                return;
            }

            var battles = new List<SkirmishBattleCardState>();
            for (int i = 0; i < field.GridCards.Length; i++)
            {
                if (!IsEmptyCard(field.GridCards[i]))
                    battles.Add(field.GridCards[i]);
            }

            bool sheet = IsTheatreBottomSheet(parent);
            if (sheet)
            {
                BuildTheatreSheetStrip(selectedZone, parent, battles, fa);
                return;
            }

            if (!TryTheatrePanelArea(parent, out Rect area, out Transform blurb))
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " battles=" + battles.Count + " skipped=no-area");
                return;
            }

            if (battles.Count == 0)
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " battles=0 skipped=no-battles");
                return;
            }

            // v41b (Game PM v90 review): approved-1 grid of readable thumbnails. As many columns as keep a thumbnail
            // >= 64px wide (3-4 in the 16:9 side panel), as many rows as fit; when battles remain the last slot is "+N".
            int columns = Mathf.Clamp(Mathf.FloorToInt((area.width + TheatrePanelGap) / (TheatrePanelSideMinW + TheatrePanelGap)), 1, TheatrePanelSideMaxColumns);
            float thumbW = (area.width - TheatrePanelGap * (columns - 1)) / columns;
            float thumbH = thumbW * 0.75f;
            int rows = Mathf.FloorToInt((area.height + TheatrePanelGap) / (thumbH + TheatrePanelGap));
            if (thumbW < TheatrePanelSideMinW || rows < 1)
            {
                BuildTheatreCompact(selectedZone, parent, area, blurb, battles, fa);
                return;
            }

            rows = Mathf.Min(rows, Mathf.CeilToInt(battles.Count / (float)columns));
            rows = Mathf.Min(rows, TheatreSideMaxRows); // v48 (Game PM): 3x3 at 16:9 and wider, 2x3 at 4:3
            // v48: no empty band under the grid. The height left by the row cap goes first to the tiles (taller, up to
            // square; the preview is cover-cropped), then to the medal/objective block, which moves up to meet the grid.
            float band = area.height - (rows * thumbH + (rows - 1) * TheatrePanelGap);
            if (band > 0.5f)
                thumbH = Mathf.Min(thumbW, thumbH + band / rows);
            band = area.height - (rows * thumbH + (rows - 1) * TheatrePanelGap);
            float lift = band > 0.5f ? Mathf.Min(band, ObjectiveSpreadMax * ObjectiveRows) : 0f;
            if (lift > 0.5f)
            {
                ShiftTheatreLowerBlock(parent, -lift);
                LayoutTheatreObjectives(parent, fa);
            }

            int slots = rows * columns;
            int shown = Mathf.Min(battles.Count, slots);
            int more = battles.Count - shown;
            if (more > 0)
            {
                shown--;
                more++;
            }

            // The zone blurb sat in this space; the battle list replaces it (it is still the zone's own copy elsewhere).
            if (blurb != null)
                blurb.gameObject.SetActive(false);

            var holder = new GameObject(TheatrePanelBattlesName, typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            PlaceTopLeft(holder, area.x, area.y, area.width, area.height);

            int playable = 0;
            int won = 0;
            int medals = 0;
            for (int i = 0; i < battles.Count; i++)
            {
                if (battles[i].Access == SkirmishCardAccess.Playable)
                    playable++;
                if (battles[i].Medals != SkirmishMedalMask.None)
                    won++;
                medals += MedalCount(battles[i].Medals);
            }

            for (int i = 0; i < shown; i++)
            {
                int column = i % columns;
                int visual = fa ? columns - 1 - column : column;
                AddTheatreThumb(holder, battles[i], visual * (thumbW + TheatrePanelGap), (i / columns) * (thumbH + TheatrePanelGap), thumbW, thumbH, fa);
            }

            if (more > 0)
            {
                int column = shown % columns;
                int visual = fa ? columns - 1 - column : column;
                TMP_Text rest = CreateLabel(holder, "MoreBattles", 18f);
                PlaceTopLeft(rest.rectTransform, visual * (thumbW + TheatrePanelGap), (shown / columns) * (thumbH + TheatrePanelGap), thumbW, thumbH);
                string moreCopy = "+" + more;
                SetCopy(rest, fa ? SkirmishWidgetContext.ToPersianDigits(moreCopy) : moreCopy, TheatreCompactMinText, 18f);
                rest.textWrappingMode = TextWrappingModes.NoWrap;
                FitReadableLine(rest, thumbW, TheatreCompactMinText);
                WireTheatreMoreTile(rest, selectedZone.MapId, fa);
            }

            VerifyTheatrePanelCounts(holder, selectedZone.MapId, battles.Count, "side");
            LogTheatreGrid(selectedZone.MapId, "side", columns, rows, shown, battles.Count, Mathf.Max(0, more), fa,
                " thumb=" + thumbW.ToString("0.0") + "x" + thumbH.ToString("0.0") + " lift=" + lift.ToString("0.0") +
                " band=" + Mathf.Max(0f, band - lift).ToString("0.0"));
            Debug.Log("[TheatrePanel] map=" + selectedZone.MapId +
                " battles=" + battles.Count +
                " visible=" + shown +
                " more=" + Mathf.Max(0, more) +
                " columns=" + columns + " rows=" + rows +
                " playable=" + playable +
                " won=" + won +
                " medals=" + medals +
                " pinWon=" + selectedZone.WonCount + "/" + selectedZone.PublishedCount +
                " thumb=" + thumbW.ToString("0.0") + "x" + thumbH.ToString("0.0") +
                " layout=side rect=" + TheatrePanelRectCopy(parent, area) +
                " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>
        /// v41 compact layout for small side areas (e.g. 241x251) where six columns would fall under the 28px thumb floor:
        /// a one-line won/open summary (14px minimum, medal icon) and then as many 3-column thumbnails as fit; the last
        /// slot becomes a "+N" count when battles remain. Every number is from the same real GridCards list.
        /// </summary>
        private void BuildTheatreCompact(SkirmishTheaterPinState zone, RectTransform parent, Rect area, Transform blurb,
            List<SkirmishBattleCardState> battles, bool fa)
        {
            int playable = 0;
            int won = 0;
            int medals = 0;
            SkirmishMedalMask best = SkirmishMedalMask.None;
            for (int i = 0; i < battles.Count; i++)
            {
                if (battles[i].Access == SkirmishCardAccess.Playable)
                    playable++;
                if (battles[i].Medals != SkirmishMedalMask.None)
                    won++;
                medals += MedalCount(battles[i].Medals);
                best |= battles[i].Medals;
            }

            if (blurb != null)
                blurb.gameObject.SetActive(false);
            var holder = new GameObject(TheatrePanelBattlesName, typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(parent, false);
            PlaceTopLeft(holder, area.x, area.y, area.width, area.height);

            const float iconSize = 18f;
            var medalIcon = new GameObject("SummaryMedal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            medalIcon.transform.SetParent(holder, false);
            medalIcon.raycastTarget = false;
            medalIcon.preserveAspect = true;
            PlaceTopLeft(medalIcon.rectTransform, fa ? area.width - iconSize : 0f, (TheatreCompactSummaryH - iconSize) * 0.5f, iconSize, iconSize);
            ShowIcon(medalIcon, HighestMedalIcon(best), Color.white);

            string wonCopy = won + "/" + battles.Count;
            string openCopy = playable.ToString();
            string summaryCopy = fa
                ? "پیروزی " + SkirmishWidgetContext.ToPersianDigits(wonCopy) + " · باز " + SkirmishWidgetContext.ToPersianDigits(openCopy)
                : "WON " + wonCopy + " · OPEN " + openCopy;
            float textW = Mathf.Max(30f, area.width - iconSize - 4f);
            TMP_Text summary = CreateLabel(holder, "Summary", 16f);
            PlaceTopLeft(summary.rectTransform, fa ? 0f : iconSize + 4f, 0f, textW, TheatreCompactSummaryH);
            SetCopy(summary, summaryCopy, TheatreCompactMinText, 16f);
            summary.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            summary.isRightToLeftText = fa;
            summary.textWrappingMode = TextWrappingModes.NoWrap;
            summary.overflowMode = TextOverflowModes.Overflow;
            FitReadableLine(summary, textW, TheatreCompactMinText);

            float top = TheatreCompactSummaryH + TheatrePanelGap;
            float left = area.height - top;
            float thumbW = (area.width - TheatrePanelGap * (TheatreCompactColumns - 1)) / TheatreCompactColumns;
            float thumbH = Mathf.Min(thumbW * 0.75f, left);
            int rows = thumbH >= TheatreCompactMinThumb ? Mathf.Max(1, Mathf.FloorToInt((left + TheatrePanelGap) / (thumbH + TheatrePanelGap))) : 0;
            int slots = rows * TheatreCompactColumns;
            int shown = Mathf.Min(battles.Count, slots);
            int more = battles.Count - shown;
            if (more > 0 && shown > 0)
            {
                shown--;
                more++;
            }

            for (int i = 0; i < shown; i++)
            {
                int column = i % TheatreCompactColumns;
                int visual = fa ? TheatreCompactColumns - 1 - column : column;
                AddTheatreThumb(holder, battles[i], visual * (thumbW + TheatrePanelGap), top + (i / TheatreCompactColumns) * (thumbH + TheatrePanelGap), thumbW, thumbH, fa);
            }

            if (more > 0 && slots > 0)
            {
                int column = shown % TheatreCompactColumns;
                int visual = fa ? TheatreCompactColumns - 1 - column : column;
                TMP_Text rest = CreateLabel(holder, "MoreBattles", 16f);
                PlaceTopLeft(rest.rectTransform, visual * (thumbW + TheatrePanelGap), top + (shown / TheatreCompactColumns) * (thumbH + TheatrePanelGap), thumbW, thumbH);
                string moreCopy = "+" + more;
                SetCopy(rest, fa ? SkirmishWidgetContext.ToPersianDigits(moreCopy) : moreCopy, TheatreCompactMinText, 16f);
                rest.textWrappingMode = TextWrappingModes.NoWrap;
                FitReadableLine(rest, thumbW, TheatreCompactMinText);
                WireTheatreMoreTile(rest, zone.MapId, fa);
            }

            VerifyTheatrePanelCounts(holder, zone.MapId, battles.Count, "compact");
            LogTheatreGrid(zone.MapId, "compact", TheatreCompactColumns, rows, shown, battles.Count, Mathf.Max(0, more), fa);
            Debug.Log("[TheatrePanel] map=" + zone.MapId +
                " battles=" + battles.Count +
                " visible=" + shown +
                " more=" + Mathf.Max(0, more) +
                " playable=" + playable +
                " won=" + won +
                " medals=" + medals +
                " pinWon=" + zone.WonCount + "/" + zone.PublishedCount +
                " thumb=" + (rows > 0 ? thumbW.ToString("0.0") + "x" + thumbH.ToString("0.0") : "none") +
                " summaryFont=" + summary.fontSize.ToString("0.0") +
                " layout=compact rect=" + TheatrePanelRectCopy(parent, area) +
                " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>The 4:3 detail panel is a wide, short bottom sheet (PresentTheatreDetailPanel's bottomSheet branch).</summary>
        private static bool IsTheatreBottomSheet(RectTransform panel)
        {
            Vector2 size = panel.sizeDelta;
            return size.y > 1f && size.y <= 260f && size.x >= size.y * 2.2f;
        }

        /// <summary>
        /// 4:3 bottom sheet: the objective-type block (2 x 2 over the sheet's middle+trail columns) is re-flowed into one row
        /// of four at the top of that column, and the district's battles run below it as a single horizontally scrolling
        /// row inside the sheet. Everything stays inside the sheet rect, which PresentTheatreDetailPanel already keeps clear
        /// of the territories and the zoom pill.
        /// </summary>
        private void BuildTheatreSheetStrip(SkirmishTheaterPinState zone, RectTransform sheet, List<SkirmishBattleCardState> battles, bool fa)
        {
            RectTransform heading = sheet.Find("ObjectiveHeading") as RectTransform;
            if (heading == null || battles.Count == 0)
            {
                Debug.Log("[TheatrePanel] map=" + zone.MapId + " battles=" + battles.Count + " layout=sheet skipped=" + (heading == null ? "no-objectives" : "no-battles"));
                return;
            }

            const float pad = 12f;
            float colX = heading.anchoredPosition.x;
            float colW = heading.sizeDelta.x;
            float headingY = -heading.anchoredPosition.y;
            float sheetH = sheet.sizeDelta.y;

            // Objective types: one row of four (icon + caption), mirrored in FA. v47: below the heading's measured ink
            // (one line, fitted to its box), not a fixed offset from its rect.
            TMP_Text headingText = heading.GetComponent<TMP_Text>();
            if (headingText != null)
                FitDetailLine(headingText, 16f, 24f);
            float rowTop = Mathf.Max(headingY + heading.sizeDelta.y, TextInkBottom(sheet, headingText)) + ObjectiveHeadingGap;
            const float iconSize = 22f;
            const float captionH = 22f;
            float cellW = colW / 4f;
            string[] objectives = SkirmishBattleLibraryArt.ObjectiveIds;
            var captions = new List<RectTransform>();
            for (int i = 0; i < sheet.childCount; i++)
            {
                if (sheet.GetChild(i) is RectTransform c && c.name == "Subtitle")
                    captions.Add(c);
            }

            for (int i = 0; i < objectives.Length; i++)
            {
                int visual = fa ? objectives.Length - 1 - i : i;
                float cellX = colX + visual * cellW;
                if (sheet.Find("ObjectiveType_" + objectives[i]) is RectTransform icon)
                    PlaceTopLeft(icon, fa ? cellX + cellW - iconSize - 4f : cellX + 4f, rowTop, iconSize, iconSize);
                if (i < captions.Count)
                {
                    float capX = fa ? cellX + 2f : cellX + iconSize + 8f;
                    PlaceTopLeft(captions[i], capX, rowTop, Mathf.Max(30f, cellW - iconSize - 10f), captionH);
                }
            }

            float stripTop = rowTop + Mathf.Max(iconSize, captionH) + 6f;
            float stripH = sheetH - pad - stripTop;
            if (stripH < TheatrePanelMinThumb)
            {
                Debug.Log("[TheatrePanel] map=" + zone.MapId + " battles=" + battles.Count + " layout=sheet skipped=too-small strip=" + stripH.ToString("0"));
                return;
            }

            float thumbH = Mathf.Min(stripH, 64f);
            float thumbW = thumbH * 4f / 3f;
            var area = new Rect(colX, stripTop, colW, thumbH);

            // Masked viewport + ScrollRect so more battles than fit scroll sideways instead of spilling out of the sheet.
            var viewport = new GameObject(TheatrePanelBattlesName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)).GetComponent<RectTransform>();
            viewport.SetParent(sheet, false);
            PlaceTopLeft(viewport, area.x, area.y, area.width, area.height);
            Image hit = viewport.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0.01f);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            float contentW = battles.Count * thumbW + Mathf.Max(0, battles.Count - 1) * TheatrePanelGap;
            PlaceTopLeft(content, fa ? Mathf.Min(0f, area.width - contentW) : 0f, 0f, contentW, thumbH);
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            int playable = 0, won = 0, medals = 0;
            for (int i = 0; i < battles.Count; i++)
            {
                int visual = fa ? battles.Count - 1 - i : i;
                SkirmishBattleCardState card = battles[i];
                if (card.Access == SkirmishCardAccess.Playable) playable++;
                if (card.Medals != SkirmishMedalMask.None) won++;
                medals += MedalCount(card.Medals);
                AddTheatreThumb(content, card, visual * (thumbW + TheatrePanelGap), 0f, thumbW, thumbH, fa);
            }

            int visible = Mathf.Min(battles.Count, Mathf.FloorToInt((area.width + TheatrePanelGap) / (thumbW + TheatrePanelGap)));
            // Sheet: every battle is a real tile in one scrolling row, no "+N" tile.
            LogTheatreGrid(zone.MapId, "sheet", battles.Count, 1, battles.Count, battles.Count, 0, fa); // all drawn; ~visible in view logged by [TheatrePanel]
            Debug.Log("[TheatrePanel] map=" + zone.MapId +
                " battles=" + battles.Count +
                " visible=" + visible +
                " playable=" + playable +
                " won=" + won +
                " medals=" + medals +
                " pinWon=" + zone.WonCount + "/" + zone.PublishedCount +
                " thumb=" + thumbW.ToString("0.0") + "x" + thumbH.ToString("0.0") +
                " layout=sheet rect=" + TheatrePanelRectCopy(sheet, area) +
                " sheet=" + TheatrePanelRectCopy(sheet, new Rect(0f, 0f, sheet.sizeDelta.x, sheetH)) +
                " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>Panel-local rect converted to the panel parent's (plate) top-left coordinates, "x,y,w,h".</summary>
        /// <summary>
        /// v42: the drawn panel must account for every real battle exactly once - thumbnails actually built plus the
        /// number printed on the "+N" tile equal the district's battle count. Logged as [TheatrePanelCheck]; a mismatch
        /// is an error so the capture log flags it.
        /// </summary>
        private static void VerifyTheatrePanelCounts(RectTransform holder, string mapId, int battleCount, string layout)
        {
            if (holder == null)
                return;
            int drawn = 0;
            int labelled = 0;
            int labelledReversed = 0;
            for (int i = 0; i < holder.childCount; i++)
            {
                Transform child = holder.GetChild(i);
                if (child.name.StartsWith("TheatreBattle_", StringComparison.Ordinal))
                    drawn++;
                else if (child.name == "MoreBattles")
                    labelled = ParseMoreCount(child.GetComponent<TMP_Text>(), out labelledReversed);
            }

            // RTL rendering can store the Persian digits in visual order, so the reversed reading also counts.
            if (drawn + labelled != battleCount && drawn + labelledReversed == battleCount)
                labelled = labelledReversed;
            bool ok = drawn + labelled == battleCount;
            string line = "[TheatrePanelCheck] map=" + mapId + " layout=" + layout + " battles=" + battleCount +
                " drawn=" + drawn + " plusN=" + labelled + " hidden=" + (battleCount - drawn) + " ok=" + (ok ? "1" : "0");
            if (ok)
                Debug.Log(line);
            else
                Debug.LogError(line);
        }

        private static int ParseMoreCount(TMP_Text label, out int reversed)
        {
            reversed = 0;
            if (label == null || string.IsNullOrEmpty(label.text))
                return 0;
            int value = 0;
            int scale = 1;
            foreach (char c in label.text)
            {
                int digit = c >= '0' && c <= '9' ? c - '0'
                    : c >= '\u06F0' && c <= '\u06F9' ? c - '\u06F0'
                    : c >= '\u0660' && c <= '\u0669' ? c - '\u0660'
                    : -1;
                if (digit < 0)
                    continue;
                value = value * 10 + digit;
                reversed += digit * scale;
                scale *= 10;
            }

            return value;
        }

        private static string TheatrePanelRectCopy(RectTransform panel, Rect local)
        {
            float ox = panel.anchoredPosition.x;
            float oy = -panel.anchoredPosition.y;
            return (ox + local.x).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "," +
                (oy + local.y).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "," +
                local.width.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "," +
                local.height.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void AddTheatreThumb(RectTransform parent, SkirmishBattleCardState card, float x, float y, float thumbW, float thumbH, bool fa)
        {
            bool open = card.Access == SkirmishCardAccess.Playable;
                var thumb = new GameObject("TheatreBattle_" + card.ScenarioId, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                thumb.transform.SetParent(parent, false);
                thumb.raycastTarget = false; // v48: set by WireTheatreBattleTile (PlaceCoverPreview resets it); badges stay off
                Texture2D preview = ResolveGridPreview(card.PreviewSpriteKey, card.ScenarioId, card.MapId, out bool dimmed);
                PlaceCoverPreview(thumb, thumbW, thumbH, preview, dimmed || !open);
                PlaceTopLeft(thumb.rectTransform, x, y, thumbW, thumbH);
                if (!open)
                    thumb.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                Outline edge = thumb.gameObject.AddComponent<Outline>();
                edge.effectColor = card.IsSelected ? new Color(0.95f, 0.75f, 0.3f, 1f) : new Color(0.35f, 0.3f, 0.2f, 0.9f);
                edge.effectDistance = new Vector2(1f, -1f);

                float badge = Mathf.Clamp(thumbH * 0.38f, 10f, 18f);
                var objective = new GameObject("Objective", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                objective.transform.SetParent(thumb.transform, false);
                objective.raycastTarget = false;
                objective.preserveAspect = true;
                PlaceTopLeft(objective.rectTransform, fa ? thumbW - badge - 1f : 1f, 1f, badge, badge);
                ShowIcon(objective, SkirmishBattleLibraryArt.ObjectiveIcon(card.ObjectiveId), Color.white);

                var medal = new GameObject("Medal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                medal.transform.SetParent(thumb.transform, false);
                medal.raycastTarget = false;
                medal.preserveAspect = true;
                PlaceTopLeft(medal.rectTransform, fa ? 1f : thumbW - badge - 1f, thumbH - badge - 1f, badge, badge);
                ShowIcon(medal, HighestMedalIcon(card.Medals), Color.white);

                if (!open)
                {
                    var lockIcon = new GameObject("Lock", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    lockIcon.transform.SetParent(thumb.transform, false);
                    lockIcon.raycastTarget = false;
                    lockIcon.preserveAspect = true;
                    float lockSize = Mathf.Clamp(thumbH * 0.45f, 10f, 22f);
                    PlaceTopLeft(lockIcon.rectTransform, (thumbW - lockSize) * 0.5f, (thumbH - lockSize) * 0.5f, lockSize, lockSize);
                    ShowIcon(lockIcon, SkirmishBattleLibraryArt.ResourceRoot + "icon_lock", Color.white);
                }

            WireTheatreBattleTile(thumb, card, open, fa);
        }

        /// <summary>
        /// v47c (Game PM): Theatre right-panel tiles are tappable in every layout (side, compact, sheet), en and fa.
        /// Visuals are the stock uGUI Button ColorTint on the tile image, the same pressed/highlighted treatment as the
        /// Grid/Map battle cards (SkirmishBattleCardWidget.CardRoot). A playable tile zooms into its district's Map view
        /// and selects the battle through the map pin's own path (OnLibraryCard, Library.cs), which raises the view
        /// model's CardSelect sound. A locked tile keeps the pressed state, plays CardLocked (the disabled-tap sound
        /// ApplyGridCardAccess gives locked Grid cards) through AttachAudio, and does not navigate.
        /// </summary>
        private void WireTheatreBattleTile(RawImage thumb, SkirmishBattleCardState card, bool open, bool fa)
        {
            if (thumb == null || string.IsNullOrEmpty(card.ScenarioId))
                return;
            // v48 (Flow tap proof): PlaceCoverPreview turns raycastTarget off on the cover image after AddTheatreThumb set it,
            // so the v47c tile could not take a real pointer. The Button owns the tap target, set last.
            thumb.raycastTarget = true;
            Button button = thumb.gameObject.AddComponent<Button>();
            button.targetGraphic = thumb;
            button.transition = Selectable.Transition.ColorTint;
            button.interactable = true; // locked tiles still show the pressed state; the click only plays the locked tap
            string scenarioId = card.ScenarioId;
            string mapId = card.MapId;
            if (open)
            {
                AttachDisabledTap(button, UIAudioEventKind.CardLocked); // enabled click stays silent: the view model sounds it
                button.onClick.AddListener(() => OnTheatreTileBattle(mapId, scenarioId, fa));
            }
            else
            {
                AttachAudio(button, UIAudioEventKind.CardLocked);
                button.onClick.AddListener(() => LogTheatreGridTap("locked:" + scenarioId, mapId, fa));
            }
        }

        /// <summary>v47c: the "+N" tile opens the Grid for that district (SelectGridBattlefield, LibraryGrid.cs).</summary>
        private void WireTheatreMoreTile(TMP_Text rest, string mapId, bool fa)
        {
            if (rest == null || string.IsNullOrEmpty(mapId))
                return;
            rest.raycastTarget = true;
            Button button = rest.gameObject.AddComponent<Button>();
            button.targetGraphic = rest;
            button.transition = Selectable.Transition.ColorTint;
            AttachDisabledTap(button, UIAudioEventKind.ButtonDisabledTap); // enabled click: SelectGridBattlefield's TabSelect
            string captured = mapId;
            button.onClick.AddListener(() =>
            {
                LogTheatreGridTap("grid:" + captured, captured, fa);
                SelectGridBattlefield(captured);
            });
        }

        /// <summary>
        /// v47c: same end state as tapping the battle's pin on that district's Map: Battlefield level, Map layout, the
        /// district selected, then the pin's own OnLibraryCard (SelectScenarioId: view-model selection, config, Bind).
        /// Only the pin tap's sound (CardSelect) is heard; the zoom/layout steps are muted as in SelectTheaterRegion.
        /// </summary>
        private void OnTheatreTileBattle(string mapId, string scenarioId, bool fa)
        {
            LogTheatreGridTap("battle:" + scenarioId, mapId, fa);
            EnsureLibraryViewModel();
            bool wired = _libraryNotificationsWired;
            if (wired)
                _libraryViewModel.Interaction -= OnLibraryInteraction;
            try
            {
                if (_libraryViewModel.Battlefield.Layout != SkirmishBattlefieldLayout.Map)
                    _libraryViewModel.SetLayout(SkirmishBattlefieldLayout.Map);
                if (!string.IsNullOrEmpty(mapId))
                    _libraryViewModel.SelectMapCollection(mapId);
            }
            finally
            {
                if (wired)
                    _libraryViewModel.Interaction += OnLibraryInteraction;
            }

            OnLibraryCard(scenarioId);
        }

        private void LogTheatreGridTap(string target, string mapId, bool fa)
        {
            Debug.Log("[TheatreGridTap] target=" + target + " district=" + (mapId ?? string.Empty) +
                " locale=" + (fa ? "fa-IR" : "en") + " ratio=" + TheatreRatioLabel());
        }

        /// <summary>
        /// v47b (Game PM C: 2x3 at 4:3, 3x3 at 16:9+): grid proof for the capture logs. visible = real battle tiles drawn
        /// (the "+N" tile takes the last slot when battles remain), overflow = total - visible, ratio from the live
        /// composition size.
        /// </summary>
        private void LogTheatreGrid(string mapId, string layout, int columns, int rows, int visible, int total, int overflow, bool fa, string extra = "")
        {
            Debug.Log("[TheatreGrid] map=" + mapId + " layout=" + layout + " columns=" + columns + " rows=" + rows +
                " visible=" + visible + " total=" + total + " overflow=" + overflow +
                " slots=" + (columns * rows) + " plusTile=" + (overflow > 0 && layout != "sheet" ? "1" : "0") +
                " ok=" + (visible + overflow == total ? "1" : "0") +
                " locale=" + (fa ? "fa-IR" : "en") + " ratio=" + TheatreRatioLabel() + (extra ?? string.Empty));
        }

        /// <summary>v47b: "4:3" / "16:9" / "20:9" (else W:H rounded) from the composition rect.</summary>
        private string TheatreRatioLabel()
        {
            var composition = transform.Find("SkirmishSetupComposition") as RectTransform;
            if (composition == null || composition.rect.height < 1f)
                return "unknown";
            float aspect = composition.rect.width / composition.rect.height;
            if (Mathf.Abs(aspect - 4f / 3f) < 0.06f) return "4:3";
            if (Mathf.Abs(aspect - 16f / 9f) < 0.06f) return "16:9";
            if (Mathf.Abs(aspect - 20f / 9f) < 0.06f) return "20:9";
            return composition.rect.width.ToString("0") + ":" + composition.rect.height.ToString("0");
        }

        /// <summary>v47: clear space between the OBJECTIVE TYPE heading's ink and the objective rows.</summary>
        private const float ObjectiveHeadingGap = 4f;

        /// <summary>
        /// v47 (P2 v96 RejectTheatreDetailOverlap, 4:3 en): the heading is made by CreateLabel with wrapping on and the
        /// rows sat at a fixed 32 px under a 30 px box. In the 4:3 side panel (~173 px inner) "OBJECTIVE TYPE" at the
        /// 22 px floor wraps to two lines and its ink runs into ObjectiveType_BA/FC whenever the panel is rebuilt after
        /// FitTheatreDetailHeadings. The heading is fitted to one line in its box (same rule as FitTheatreDetailHeadings,
        /// so the later pass keeps the size) and the 2 x 2 block is laid out below its measured ink plus a gap, in the
        /// space left down to the panel's bottom pad. Columns (and the FA mirror) are kept from DetailObjectives.
        /// </summary>
        private void LayoutTheatreObjectives(RectTransform panel, bool fa)
        {
            TMP_Text heading = panel != null ? panel.Find("ObjectiveHeading")?.GetComponent<TMP_Text>() : null;
            if (heading == null)
                return;
            RectTransform headingRect = heading.rectTransform;
            FitDetailLine(heading, 16f, 24f);
            float boxBottom = -headingRect.anchoredPosition.y + headingRect.sizeDelta.y;
            float inkBottom = TextInkBottom(panel, heading);
            float rowsTop = Mathf.Max(boxBottom, inkBottom) + ObjectiveHeadingGap;

            string[] ids = SkirmishBattleLibraryArt.ObjectiveIds;
            var icons = new List<RectTransform>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
                icons.Add(panel.Find("ObjectiveType_" + ids[i]) as RectTransform);
            var captions = new List<RectTransform>(ids.Length);
            int headingIndex = headingRect.GetSiblingIndex();
            for (int i = headingIndex + 1; i < panel.childCount && captions.Count < ids.Length; i++)
            {
                if (panel.GetChild(i) is RectTransform child && child.name == "Subtitle")
                    captions.Add(child);
            }

            float blockBottom = 0f;
            float oldIcon = 0f;
            float captionH = 26f;
            for (int i = 0; i < ids.Length; i++)
            {
                if (icons[i] != null)
                {
                    blockBottom = Mathf.Max(blockBottom, -icons[i].anchoredPosition.y + icons[i].sizeDelta.y);
                    oldIcon = Mathf.Max(oldIcon, icons[i].sizeDelta.y);
                }

                if (i < captions.Count)
                {
                    blockBottom = Mathf.Max(blockBottom, -captions[i].anchoredPosition.y + captions[i].sizeDelta.y);
                    captionH = captions[i].sizeDelta.y;
                }
            }

            // v47d (v97: fa 'پایگاه' under BA ran into the BT icon on the row below at every ratio): rows are sized from
            // the captions' measured ink, not their 26 px box.
            // v49 (Game PM: fa captions sat ~2x further below their icons than en): Noto Arabic's line box (the 1.65 box
            // FitArabicLineBox assumes) puts the first line's ink much lower under a TopRight rect top than the Latin
            // font does under TopLeft. Each caption is therefore placed by its own measured ink top, at the size it
            // will be drawn at, so icon bottom -> caption ink top is ObjectiveCaptionInkGap in both locales.
            var captionTexts = new List<TMP_Text>(captions.Count);
            var captionInkTop = new List<float>(captions.Count);
            float captionReach = 0f;       // largest font size the captions can be drawn at
            float inkBelowIcon = captionH; // deepest caption ink below its icon's bottom
            float captionSize = ObjectiveCaptionSize();
            for (int i = 0; i < captions.Count; i++)
            {
                TMP_Text text = captions[i].GetComponent<TMP_Text>();
                PinObjectiveCaption(text, captionSize);
                captionTexts.Add(text);
                float inkTop = 0f;
                if (CaptionInkAtReach(text, out float top, out float bottom, out float reach))
                {
                    // v47e: ink measured at the largest size this caption can reach (the shared text pass lifts every
                    // Subtitle to the library floor after a panel rebuild), whatever pass comes last.
                    captionReach = Mathf.Max(captionReach, reach);
                    inkTop = top;
                    inkBelowIcon = Mathf.Max(inkBelowIcon, ObjectiveCaptionInkGap + bottom - top);
                }

                captionInkTop.Add(inkTop);
            }

            float needBelowIcon = inkBelowIcon + ObjectiveHeadingGap;
            float panelH = panel.rect.height > 1f ? panel.rect.height : panel.sizeDelta.y;
            float limit = panelH - ObjectiveBottomPad;
            int rows = Mathf.Max(1, (ids.Length + 1) / 2);
            // v48 (Game PM: fa icons match en): the icon is en's 28 px in both locales. The block was given its measured
            // height by PlaceTheatreLowerBlock (plus any height the grid left, up to ObjectiveSpreadMax a row), so the
            // pitch is what the live layout leaves, never less than need.
            float icon = ObjectiveIconSize;
            float rowNeed = Mathf.Max(DetailObjectiveRowHeight, icon + needBelowIcon);
            float pitch = Mathf.Min((limit - rowsTop) / rows, rowNeed + ObjectiveSpreadMax);
            if (pitch < icon + needBelowIcon)
                icon = Mathf.Max(16f, pitch - needBelowIcon); // only when the panel itself is too short; fits=0 below
            for (int i = 0; i < ids.Length; i++)
            {
                float top = rowsTop + (i / 2) * pitch;
                RectTransform iconRect = icons[i];
                if (iconRect != null)
                {
                    float oldX = iconRect.anchoredPosition.x;
                    float x = fa ? oldX + iconRect.sizeDelta.x - icon : oldX;
                    PlaceTopLeft(iconRect, x, top, icon, icon);
                }

                if (i < captions.Count)
                {
                    // Rect top so that the ink top lands ObjectiveCaptionInkGap under the icon (ink may rise above the rect).
                    float boxTop = top + icon + ObjectiveCaptionInkGap - captionInkTop[i];
                    PlaceTopLeft(captions[i], captions[i].anchoredPosition.x, boxTop, captions[i].sizeDelta.x, captionH);
                }
            }

            // Re-measure where the ink actually landed: every caption must clear the next row's icons and the panel pad.
            float worstClear = float.MaxValue;
            for (int i = 0; i < captionTexts.Count; i++)
            {
                if (captionTexts[i] == null || string.IsNullOrWhiteSpace(captionTexts[i].text))
                    continue;
                float ink = TextInkBottom(panel, captionTexts[i]);
                float next = (i / 2) + 1 < rows ? rowsTop + ((i / 2) + 1) * pitch : panelH - 4f;
                float now = Mathf.Max(1f, captionTexts[i].fontSize);
                float top = -captions[i].anchoredPosition.y;
                if (captionReach > now)
                    ink = top + (ink - top) * captionReach / now; // clearance at the reachable size, not today's
                worstClear = Mathf.Min(worstClear, next - ink);
            }

            if (worstClear == float.MaxValue)
                worstClear = 0f;
            bool fits = worstClear >= ObjectiveHeadingGap - 0.5f && icon >= ObjectiveIconSize - 0.01f;
            string line = "[TheatreObjectives] headingFont=" + heading.fontSize.ToString("0.0") +
                " box=" + headingRect.sizeDelta.x.ToString("0") + "x" + headingRect.sizeDelta.y.ToString("0") +
                " inkBottom=" + inkBottom.ToString("0.0") + " boxBottom=" + boxBottom.ToString("0.0") +
                " rowsTop=" + rowsTop.ToString("0.0") + " pitch=" + pitch.ToString("0.0") + " icon=" + icon.ToString("0.0") +
                " captionFont=" + (captionTexts.Count > 0 && captionTexts[0] != null ? captionTexts[0].fontSize.ToString("0.0") : "-") +
                " captionInk=" + ObjectiveCaptionInkGap.ToString("0.0") + "/" + inkBelowIcon.ToString("0.0") +
                " captionReach=" + captionReach.ToString("0.0") + " captionBox=" + captionH.ToString("0") + " captionClear=" + worstClear.ToString("0.0") +
                " panelH=" + panelH.ToString("0") + " fits=" + (fits ? "1" : "0") + " locale=" + (fa ? "fa-IR" : "en");
            if (fits)
                Debug.Log(line);
            else
                Debug.LogWarning(line);
            LogTheatreCaptionGaps(ids, icons, captionTexts, fa);
        }

        /// <summary>
        /// v50 (v98: fa-IR captionFont=12.3 against en 20-22): DetailObjectives creates the captions in a 26 px box with
        /// SetCopy 14-20 and autosize, and in fa V3LocalizedTextBindingView.FitArabicLineBox caps an Arabic line at
        /// box/1.65 (15.8) with a 12 px minimum, so the fa autosize settled at 12.3 while en grew to 20 and the shared
        /// pass lifted it to the 22 pt floor. Both locales are now pinned to the library caption floor (the size the
        /// ink was already measured at, CaptionInkAtReach) with LibraryFixedTextSize, which re-asserts it every frame.
        /// </summary>
        private float ObjectiveCaptionSize()
        {
            return Mathf.Max(LibraryFontFloor, ReferenceSubtitleFloor());
        }

        private static void PinObjectiveCaption(TMP_Text text, float size)
        {
            if (text == null || size <= 0f)
                return;
            LibraryFixedTextSize held = text.GetComponent<LibraryFixedTextSize>() ?? text.gameObject.AddComponent<LibraryFixedTextSize>();
            held.Size = size;
            held.Apply();
        }

        /// <summary>v49: icon rect bottom -> caption glyph ink top, the same target in en and fa at every ratio.</summary>
        private const float ObjectiveCaptionInkGap = 6f;

        /// <summary>v49: en's measured gaps by "ratio/objective", so the fa pass of the same ratio logs its delta to en.</summary>
        private static readonly Dictionary<string, float> TheatreEnCaptionGaps = new Dictionary<string, float>();

        /// <summary>
        /// v49: logs [TheatreCaptionGap] obj= gap= enGap= delta= locale= ratio= for each objective. gap is measured from the
        /// placed icon rect bottom to the caption's glyph ink top at the size the caption will be drawn at. In fa, enGap
        /// is en's measured gap for the same ratio when en has been laid out in this session (src=en), else the shared
        /// target (src=target). |delta| > 2 logs a warning (ok=0); the Flow step asserts it on the live panel.
        /// </summary>
        private void LogTheatreCaptionGaps(string[] ids, List<RectTransform> icons, List<TMP_Text> captions, bool fa)
        {
            string ratio = TheatreRatioLabel();
            string locale = fa ? "fa-IR" : "en";
            for (int i = 0; i < ids.Length && i < captions.Count; i++)
            {
                RectTransform iconRect = icons[i];
                TMP_Text text = captions[i];
                if (iconRect == null || text == null || !CaptionInkAtReach(text, out float inkTop, out _, out _))
                    continue;
                float iconBottom = -iconRect.anchoredPosition.y + iconRect.sizeDelta.y;
                float gap = (-text.rectTransform.anchoredPosition.y + inkTop) - iconBottom;
                string key = ratio + "/" + ids[i];
                string src = "en";
                float enGap = gap;
                if (!fa)
                    TheatreEnCaptionGaps[key] = gap;
                else if (!TheatreEnCaptionGaps.TryGetValue(key, out enGap))
                {
                    enGap = ObjectiveCaptionInkGap;
                    src = "target";
                }

                float delta = gap - enGap;
                bool ok = Mathf.Abs(delta) <= 2f;
                string line = "[TheatreCaptionGap] obj=" + ids[i] + " gap=" + gap.ToString("0.0") + " enGap=" + enGap.ToString("0.0") +
                    " delta=" + delta.ToString("0.0") + " locale=" + locale + " ratio=" + ratio + " src=" + src + " ok=" + (ok ? "1" : "0");
                if (ok)
                    Debug.Log(line);
                else
                    Debug.LogWarning(line);
            }
        }

        /// <summary>
        /// v49: a caption's glyph ink top and bottom below its rect top (top may be negative when ink rises above it),
        /// scaled to the largest size the caption can reach (the shared text pass lifts Subtitles to the library floor;
        /// captions are top-aligned, so ink offsets scale with the size).
        /// </summary>
        private bool CaptionInkAtReach(TMP_Text text, out float inkTop, out float inkBottom, out float reach)
        {
            inkTop = 0f;
            inkBottom = 0f;
            reach = 0f;
            if (!TextInkExtent(text, out _, out float depth, out float drop))
                return false;
            float now = Mathf.Max(1f, text.fontSize);
            reach = Mathf.Max(now, Mathf.Max(LibraryFontFloor, ReferenceSubtitleFloor()));
            if (text.enableAutoSizing)
                reach = Mathf.Max(reach, text.fontSizeMax);
            float scale = reach / now;
            inkTop = drop * scale;
            inkBottom = depth * scale;
            return true;
        }

        private const float ObjectiveIconSize = 28f;   // v48: en's objective icon; fa matches it
        private const float ObjectiveSpreadMax = 12f;  // v48: extra pitch a row may take from the grid's leftover band
        private const float ObjectiveBottomPad = 12f;
        private const int ObjectiveRows = 2;
        private const int TheatreSideMaxRows = 3;

        /// <summary>
        /// v48: the side panel's lower block (medal counts + OBJECTIVE TYPE heading + 2x2 rows) is bottom-anchored with
        /// the height it measurably needs: heading box/ink + gap + 2 rows of max(66, 28 icon + caption offset + caption
        /// ink at its reachable size + gap). fa's deeper Noto ink makes its block ~12 px taller than en's; the grid area
        /// above (TryTheatrePanelArea) shrinks to match. Computed from relative offsets, so it is idempotent.
        /// </summary>
        private void PlaceTheatreLowerBlock(RectTransform panel)
        {
            TMP_Text heading = panel != null ? panel.Find("ObjectiveHeading")?.GetComponent<TMP_Text>() : null;
            if (heading == null)
                return;
            RectTransform headingRect = heading.rectTransform;
            FitDetailLine(heading, 16f, 24f);
            float headingTop = -headingRect.anchoredPosition.y;
            float headingDepth = Mathf.Max(headingRect.sizeDelta.y, TextInkBottom(panel, heading) - headingTop) + ObjectiveHeadingGap;
            float inkBelowIcon = MeasureObjectiveCaptions(panel, headingRect);
            float rowNeed = Mathf.Max(DetailObjectiveRowHeight, ObjectiveIconSize + inkBelowIcon + ObjectiveHeadingGap);
            float panelH = panel.rect.height > 1f ? panel.rect.height : panel.sizeDelta.y;
            float target = panelH - ObjectiveBottomPad - ObjectiveRows * rowNeed - headingDepth;
            float delta = target - headingTop;
            if (Mathf.Abs(delta) > 0.25f)
                ShiftTheatreLowerBlock(panel, delta);
        }

        /// <summary>v48: moves the medal row and the objective block (not the header or the grid) by delta px (down +).</summary>
        private static void ShiftTheatreLowerBlock(RectTransform panel, float delta)
        {
            if (panel == null)
                return;
            float lowerTop = float.MaxValue;
            for (int i = 0; i < panel.childCount; i++)
            {
                if (panel.GetChild(i) is RectTransform child &&
                    (child.name.StartsWith("ZoneMedal_", StringComparison.Ordinal) || child.name == "ObjectiveHeading"))
                    lowerTop = Mathf.Min(lowerTop, -child.anchoredPosition.y);
            }

            if (lowerTop == float.MaxValue)
                return;
            for (int i = 0; i < panel.childCount; i++)
            {
                if (panel.GetChild(i) is not RectTransform child)
                    continue;
                string n = child.name;
                bool member = n.StartsWith("ZoneMedal_", StringComparison.Ordinal) || n == "ObjectiveHeading" ||
                    n.StartsWith("ObjectiveType_", StringComparison.Ordinal) ||
                    ((n == "Value" || n == "Subtitle") && -child.anchoredPosition.y >= lowerTop - 8f);
                if (member)
                    child.anchoredPosition = new Vector2(child.anchoredPosition.x, child.anchoredPosition.y - delta);
            }
        }

        /// <summary>v49: deepest caption ink below its icon when its ink top sits ObjectiveCaptionInkGap under it (as LayoutTheatreObjectives).</summary>
        private float MeasureObjectiveCaptions(RectTransform panel, RectTransform headingRect)
        {
            float below = 26f;
            int found = 0;
            for (int i = headingRect.GetSiblingIndex() + 1; i < panel.childCount && found < SkirmishBattleLibraryArt.ObjectiveIds.Length; i++)
            {
                if (!(panel.GetChild(i) is RectTransform child) || child.name != "Subtitle")
                    continue;
                found++;
                below = Mathf.Max(below, child.sizeDelta.y);
                if (CaptionInkAtReach(child.GetComponent<TMP_Text>(), out float top, out float bottom, out _))
                    below = Mathf.Max(below, ObjectiveCaptionInkGap + bottom - top);
            }

            return below;
        }

        /// <summary>
        /// v47d: a label's ink relative to its own rect top: rise = ink above the top edge (>= 0), depth = lowest glyph
        /// quad below the top edge. Glyph quads include TMP's descender dots, which is what the overlap check reads.
        /// </summary>
        private static bool TextInkExtent(TMP_Text text, out float rise, out float depth) => TextInkExtent(text, out rise, out depth, out _);

        /// <summary>v49: as above, plus drop = the highest glyph quad's top below the rect top (negative when it rises above).</summary>
        private static bool TextInkExtent(TMP_Text text, out float rise, out float depth, out float drop)
        {
            rise = 0f;
            depth = 0f;
            drop = float.MaxValue;
            if (text == null || string.IsNullOrWhiteSpace(text.text) || !text.gameObject.activeInHierarchy)
                return false;
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            int count = info != null && info.characterInfo != null ? Mathf.Min(info.characterCount, info.characterInfo.Length) : 0;
            float top = text.rectTransform.rect.yMax;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                    continue;
                any = true;
                rise = Mathf.Max(rise, c.topRight.y - top);
                depth = Mathf.Max(depth, top - c.bottomLeft.y);
                drop = Mathf.Min(drop, top - c.topRight.y);
            }

            if (!any)
                drop = 0f;
            return any;
        }

        /// <summary>v47: depth from <paramref name="panel"/>'s top of the lowest visible glyph quad of <paramref name="text"/>.</summary>
        private static float TextInkBottom(RectTransform panel, TMP_Text text)
        {
            if (panel == null || text == null)
                return 0f;
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            float bottom = 0f;
            int count = info != null && info.characterInfo != null ? Mathf.Min(info.characterCount, info.characterInfo.Length) : 0;
            for (int i = 0; i < count; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                    continue;
                Vector3 local = panel.InverseTransformPoint(text.rectTransform.TransformPoint(c.bottomLeft));
                bottom = Mathf.Max(bottom, panel.rect.yMax - local.y);
            }

            return bottom;
        }

        /// <summary>v47: re-run after FitTheatreDetailHeadings (PlaceLibraryHeader) so the rows follow the final heading size.</summary>
        private void ReflowTheatreObjectives()
        {
            if (_libraryRegions == null || !(_libraryRegions.Find(TheatreDetailPanelName) is RectTransform panel))
                return;
            if (IsTheatreBottomSheet(panel))
                return;
            LayoutTheatreObjectives(panel, UiShellRuntimeGateway.Localization.IsRightToLeft);
        }

        /// <summary>Space between the header block (Title/Stats/Rule/Preview) and the medal row or objective heading.</summary>
        private static bool TryTheatrePanelArea(RectTransform parent, out Rect area, out Transform blurb)
        {
            area = Rect.zero;
            blurb = null;
            float top = 0f;
            float bottom = float.MaxValue;
            float left = float.MaxValue;
            float right = 0f;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is not RectTransform child || !child.gameObject.activeSelf)
                    continue;
                float y = -child.anchoredPosition.y;
                float h = child.sizeDelta.y;
                string n = child.name;
                if (n == "ZoneBlurb")
                {
                    blurb = child;
                    continue;
                }

                if (n == "Title" || n == "Stats" || n == "Rule" || n == "Preview")
                {
                    top = Mathf.Max(top, y + h);
                    left = Mathf.Min(left, child.anchoredPosition.x);
                    right = Mathf.Max(right, child.anchoredPosition.x + child.sizeDelta.x);
                }
                else if (n.StartsWith("ZoneMedal_", StringComparison.Ordinal) || n == "ObjectiveHeading")
                    bottom = Mathf.Min(bottom, y);
            }

            if (bottom == float.MaxValue || right <= left)
                return false;
            float start = top + 8f;
            float end = bottom - 18f; // medal icons are placed a little under their row top
            if (end - start < TheatreCompactSummaryH)
                return false;
            area = new Rect(left, start, right - left, end - start);
            return true;
        }

        private static string HighestMedalIcon(SkirmishMedalMask mask)
        {
            if ((mask & SkirmishMedalMask.Commander) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Commander);
            if ((mask & SkirmishMedalMask.Veteran) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Veteran);
            if ((mask & SkirmishMedalMask.Regular) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Regular);
            if ((mask & SkirmishMedalMask.Recruit) != 0) return SkirmishBattleLibraryArt.Medal(true, SkirmishDifficultyId.Recruit);
            return SkirmishBattleLibraryArt.Medal(false, SkirmishDifficultyId.Recruit);
        }

        private static int MedalCount(SkirmishMedalMask mask)
        {
            int count = 0;
            for (int bits = (int)mask; bits != 0; bits &= bits - 1)
                count++;
            return count;
        }
    }
}
