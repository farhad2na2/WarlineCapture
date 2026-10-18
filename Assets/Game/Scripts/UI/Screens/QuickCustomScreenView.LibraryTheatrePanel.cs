using System;
using System.Collections.Generic;
using Game.Skirmish.Contracts;
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
        private const int TheatrePanelColumns = 6;
        private const float TheatrePanelMinThumb = 28f;
        private const float TheatrePanelGap = 4f;

        private void BuildTheatrePanel(SkirmishTheaterPinState selectedZone, RectTransform parent)
        {
            if (parent == null || _libraryViewModel == null)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
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

            int rowsNeeded = Mathf.Max(1, Mathf.CeilToInt(battles.Count / (float)TheatrePanelColumns));
            float thumbW = (area.width - TheatrePanelGap * (TheatrePanelColumns - 1)) / TheatrePanelColumns;
            float thumbH = Mathf.Min(thumbW * 0.75f, (area.height - TheatrePanelGap * (rowsNeeded - 1)) / rowsNeeded);
            if (battles.Count == 0 || thumbH < TheatrePanelMinThumb)
            {
                Debug.Log("[TheatrePanel] map=" + selectedZone.MapId + " battles=" + battles.Count +
                    " skipped=too-small area=" + area.width.ToString("0") + "x" + area.height.ToString("0"));
                return;
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
                SkirmishBattleCardState card = battles[i];
                int column = i % TheatrePanelColumns;
                int row = i / TheatrePanelColumns;
                int visual = fa ? TheatrePanelColumns - 1 - column : column;
                float x = visual * (thumbW + TheatrePanelGap);
                float y = row * (thumbH + TheatrePanelGap);
                bool open = card.Access == SkirmishCardAccess.Playable;
                if (open)
                    playable++;
                if (card.Medals != SkirmishMedalMask.None)
                    won++;
                medals += MedalCount(card.Medals);

                AddTheatreThumb(holder, card, x, y, thumbW, thumbH, fa);
            }

            Debug.Log("[TheatrePanel] map=" + selectedZone.MapId +
                " battles=" + battles.Count +
                " playable=" + playable +
                " won=" + won +
                " medals=" + medals +
                " pinWon=" + selectedZone.WonCount + "/" + selectedZone.PublishedCount +
                " thumb=" + thumbW.ToString("0.0") + "x" + thumbH.ToString("0.0") +
                " layout=side rect=" + TheatrePanelRectCopy(parent, area) +
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

            // Objective types: one row of four (icon + caption), mirrored in FA.
            float rowTop = headingY + heading.sizeDelta.y + 2f;
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
                thumb.raycastTarget = false;
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
            if (end - start < TheatrePanelMinThumb)
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
