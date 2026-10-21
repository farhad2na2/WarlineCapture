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
    /// <summary>
    /// P1 (v37) approved-3 Grid pass, applied after the shared Grid build so #66's card body code
    /// (LibraryCardArt / CreateBattleCard) and the zone-tab widget stay untouched.
    /// Cells: full-bleed art at full contrast, a small S-number badge, one tier medal in the trailing corner;
    /// locked and empty cells are dark plates with a grey medal; the selected cell gets a heavy gold frame.
    /// Column headers and row labels sit on dark framed plates like approved-3.
    /// </summary>
    public sealed partial class QuickCustomScreenView
    {
        private const float GridA3Inset = 2f;
        private const float GridA3FrameSelected = 3f;
        private static readonly Color GridA3Plate = new Color(0.05f, 0.07f, 0.08f, 0.96f);
        private static readonly Color GridA3Edge = new Color(0.42f, 0.38f, 0.30f, 0.85f);
        private static readonly Color GridA3Gold = new Color(1f, 0.82f, 0.30f, 1f);

        /// <summary>Gap 1, 6, 7, 8: restyle one Grid cell built by the shared path.</summary>
        private void StyleApproved3Cell(RectTransform cell, SkirmishBattleCardState card, bool empty, float width, float height, bool fa)
        {
            if (cell == null || width < 8f || height < 8f)
                return;
            bool playable = !empty && card.Access == SkirmishCardAccess.Playable;
            bool selected = !empty && card.IsSelected;

            // Drop the per-cell text: row and column headers already carry objective, force and base.
            HideCellChild(cell, "Title");
            HideCellChild(cell, "Subtitle");
            HideCellChild(cell, "FooterScrim");
            HideCellChild(cell, "MedalBacking");
            HideCellChild(cell, "Recommended");
            for (int i = 0; i < 4; i++)
                HideCellChild(cell, "Medal" + i);

            if (cell.TryGetComponent(out Image plate))
                plate.color = GridA3Plate;

            // Full-bleed art at full contrast for playable cells; locked/empty cells read as dark plates (approved-3).
            Transform previewNode = cell.Find("Preview");
            RawImage preview = previewNode != null ? previewNode.GetComponent<RawImage>() : null;
            if (preview != null)
            {
                if (playable)
                {
                    // Capture rules (SkirmishBattleLibraryCapture.RejectGridPreviewDominant / RejectPendingPreviewTag):
                    // full width (inset <= 2 each side), top-aligned, >= 40% of the card; a dimmed tint keeps its pending tag.
                    Texture2D texture = ResolveGridPreview(card.PreviewSpriteKey, card.ScenarioId, card.MapId, out bool dimmed);
                    PlaceCoverPreview(preview, width - GridA3Inset * 2f, height - GridA3Inset * 2f, texture, dimmed);
                    PlaceTopLeft(preview.rectTransform, GridA3Inset, GridA3Inset, width - GridA3Inset * 2f, height - GridA3Inset * 2f);
                    if (!dimmed)
                        HideCellChild(cell, "PreviewPending");
                }
                else
                {
                    // v38 locked/unplayed (S007 on, and empty slots): the real art the shared path resolved (scenario preview,
                    // else the zone's district plate via ResolveGridPreview) is drawn full-bleed and dimmed by a separate dark
                    // scrim. The RawImage tint stays final-white, so RejectPendingPreviewTag expects no pending tag, and the
                    // preview still fills the cell (RejectGridPreviewDominant / 40% floor).
                    Texture2D texture = preview.texture as Texture2D;
                    if (texture == null || texture.name == "SkirmishLibraryPlaceholder")
                        texture = ResolveGridPreview(empty ? null : card.PreviewSpriteKey, empty ? null : card.ScenarioId, card.MapId, out _);
                    PlaceCoverPreview(preview, width - GridA3Inset * 2f, height - GridA3Inset * 2f, texture, false);
                    PlaceTopLeft(preview.rectTransform, GridA3Inset, GridA3Inset, width - GridA3Inset * 2f, height - GridA3Inset * 2f);
                    preview.color = Color.white;
                    HideCellChild(cell, "PreviewPending");
                    AddGridA3LockOverlay(cell, preview.transform.GetSiblingIndex() + 1, width, height);
                }
                LogGridA3PreviewShare(preview.rectTransform.rect.height > 0f ? preview.rectTransform.sizeDelta.y : 0f, height, fa);
            }

            // Small S-number badge (top leading corner); none on empty cells.
            Transform badgeNode = cell.Find("ScenarioBadge");
            if (badgeNode != null)
            {
                if (empty)
                    badgeNode.gameObject.SetActive(false);
                else
                {
                    float badgeW = Mathf.Clamp(width * 0.34f, 36f, 52f);
                    float badgeH = Mathf.Clamp(height * 0.16f, 16f, 20f);
                    PlaceTopLeft((RectTransform)badgeNode, fa ? width - badgeW - 3f : 3f, 3f, badgeW, badgeH);
                    if (badgeNode.TryGetComponent(out Image badge))
                        badge.color = new Color(0.02f, 0.03f, 0.04f, playable ? 0.82f : 0.6f);
                    Transform id = badgeNode.Find("ScenarioId");
                    if (id != null && id.TryGetComponent(out TMP_Text idLabel))
                    {
                        SetCopy(idLabel, card.ScenarioId, 10f, 13f);
                        idLabel.color = playable ? new Color(1f, 0.94f, 0.72f, 1f) : new Color(0.7f, 0.68f, 0.62f, 1f);
                    }
                }
            }

            // One clear tier circle in the trailing bottom corner (best earned, else grey).
            HideCellChild(cell, "CornerMedal");
            float medal = Mathf.Clamp(height * 0.26f, 18f, 30f);
            medal = Mathf.Min(medal, width * 0.3f);
            SkirmishDifficultyId[] order = { SkirmishDifficultyId.Commander, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Recruit };
            SkirmishDifficultyId shown = SkirmishDifficultyId.Recruit;
            bool earned = false;
            for (int i = 0; i < order.Length && !empty; i++)
            {
                if (MedalEarned(card.Medals, order[i])) { shown = order[i]; earned = true; break; }
            }
            var corner = new GameObject("CornerMedalA3", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            corner.transform.SetParent(cell, false);
            PlaceTopLeft(corner.rectTransform, fa ? 4f : width - medal - 4f, height - medal - 4f, medal, medal);
            PaintDifficultyMedal(corner, shown, earned);
            corner.raycastTarget = false;
            corner.transform.SetAsLastSibling();

            // Framed cell: a thin edge on every cell, a heavy gold frame on the selected one.
            Outline edge = cell.GetComponent<Outline>();
            if (edge == null)
                edge = cell.gameObject.AddComponent<Outline>();
            edge.effectColor = selected ? GridA3Gold : GridA3Edge;
            edge.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            if (selected)
                AddGridA3Frame(cell, width, height);
        }

        private static readonly Color GridA3LockScrim = new Color(0.02f, 0.03f, 0.04f, 0.64f);

        /// <summary>v38: dark scrim over the locked art (just above the preview, below badge and medal) and a centred lock icon.</summary>
        private static void AddGridA3LockOverlay(RectTransform cell, int siblingIndex, float width, float height)
        {
            var scrim = new GameObject("LockScrimA3", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            scrim.transform.SetParent(cell, false);
            scrim.transform.SetSiblingIndex(Mathf.Min(siblingIndex, cell.childCount - 1));
            PlaceTopLeft(scrim.rectTransform, GridA3Inset, GridA3Inset, width - GridA3Inset * 2f, height - GridA3Inset * 2f);
            scrim.color = GridA3LockScrim;
            scrim.raycastTarget = false;

            float size = Mathf.Clamp(Mathf.Min(width, height) * 0.3f, 18f, 44f);
            var icon = new GameObject("LockIconA3", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(cell, false);
            PlaceTopLeft(icon.rectTransform, (width - size) * 0.5f, (height - size) * 0.5f, size, size);
            ShowIcon(icon, GridA3LockIcon, new Color(0.86f, 0.86f, 0.84f, 0.92f));
            icon.raycastTarget = false;
            icon.transform.SetAsLastSibling();
        }

        /// <summary>Existing V3 equipment lock art, copied into the library resources (Resources/SkirmishBattleLibrary/icon_lock.png).</summary>
        private const string GridA3LockIcon = SkirmishBattleLibraryArt.ResourceRoot + "icon_lock";

        private static bool _gridA3ShareLogged;

        /// <summary>Logs the preview share of a Grid cell once per Grid build (Game PM: keep >= 40% with the 60px tab band).</summary>
        private static void LogGridA3PreviewShare(float previewHeight, float cardHeight, bool fa)
        {
            if (_gridA3ShareLogged || cardHeight < 1f)
                return;
            _gridA3ShareLogged = true;
            Debug.Log("[GridA3] previewShare=" + (previewHeight / cardHeight).ToString("0.000") +
                " preview=" + previewHeight.ToString("0.0") + " card=" + cardHeight.ToString("0.0") +
                " tabBand=" + GridA3TabHeight.ToString("0") + " locale=" + (fa ? "fa-IR" : "en"));
        }

        /// <summary>
        /// Grid tab band height for this build. v37: 60px (SkirmishZoneTabWidget.Height stays 52; P2 agreed option b).
        /// v38: on 16:9 and wider it follows approved-3 (tab strip ~18% and column headers ~7.6% of the 1080 frame),
        /// shrinking if four rows would drop under GridA3MinRow; 4:3 keeps 60/40.
        /// </summary>
        private static float GridA3TabHeight = 60f;
        private const float GridA3TabWide = 196f;
        private const float GridA3HeaderWide = 82f;
        private const float GridA3MinRow = 112f;
        private const float GridA3CompactHeader = 50f;
        private const float GridA3MinReadable = 14f;

        /// <summary>v38: sets this build's tab band and returns the column header height (approved-3 proportions on wide frames).</summary>
        private static float ResolveGridA3Bands(bool wide, float gridHeight, float defaultHeader, int rows)
        {
            if (!wide)
            {
                GridA3TabHeight = 60f;
                return Mathf.Max(defaultHeader, GridA3CompactHeader); // v40: 4:3 two 14px lines (FA line box ~23px)
            }

            float header = GridA3HeaderWide;
            float tab = GridA3TabWide;
            int count = Mathf.Max(1, rows);
            float rowsNeed = count * GridA3MinRow + (count - 1) * LibraryGridRowGap + LibraryGridContentTop + LibraryGridContentBottom;
            float spare = gridHeight - SkirmishZoneTabWidget.OriginY - 2f - header - 4f - rowsNeed;
            if (spare < tab)
                tab = Mathf.Max(60f, spare);
            if (tab <= 60f && spare < 60f)
                header = Mathf.Max(defaultHeader, header - (60f - spare));
            GridA3TabHeight = tab;
            return header;
        }
        private static float GridA3TabBandBottom => SkirmishZoneTabWidget.OriginY + GridA3TabHeight;

        /// <summary>
        /// Gap 5: battlefield image tile inside the existing tab button with the name and the ledger count
        /// (won / published playable, from the Theatre pin state), and a heavy gold frame on the selected tab.
        /// </summary>
        private void StyleApproved3BattlefieldTab(Button button, TMP_Text label, string mapId, bool selected, float left, float width, bool fa)
        {
            if (button == null)
                return;
            _gridA3ShareLogged = false;
            var rect = button.GetComponent<RectTransform>();
            float h = GridA3TabHeight;
            PlaceTopLeft(rect, left, SkirmishZoneTabWidget.OriginY, width, h);

            SkirmishTheaterPinState pin = default;
            bool havePin = false;
            if (_libraryViewModel != null)
            {
                SkirmishTheaterPinState[] pins = _libraryViewModel.Theater.Pins;
                for (int i = 0; pins != null && i < pins.Length; i++)
                {
                    if (string.Equals(pins[i].MapId, mapId, StringComparison.OrdinalIgnoreCase)) { pin = pins[i]; havePin = true; break; }
                }
            }

            var image = new GameObject("TabArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(button.transform, false);
            image.transform.SetAsFirstSibling();
            image.transform.SetSiblingIndex(0);
            Texture2D plate = ResolveGridPreview(havePin ? pin.PlatePreviewKey : null, null, mapId, out bool dimmed);
            PlaceCoverPreview(image, width - 4f, h - 4f, plate, dimmed);
            PlaceTopLeft(image.rectTransform, 2f, 2f, width - 4f, h - 4f);
            image.color = selected || dimmed ? image.color : new Color(0.82f, 0.82f, 0.82f, 1f);

            float strip = Mathf.Clamp(h * 0.24f, 20f, 46f); // v38: approved-3 name strip (~24% of the tab)
            // v39: short (4:3) tabs stack the count above the name so the name gets the full tab width and a band
            // tall enough for the 18px floor; wide tabs keep name + count side by side in one strip.
            bool comingSoon = havePin && pin.PublishedCount <= 0;
            // v49b (v49 16:9/20:9 Grid: "MOUNTAINPASS Coming soon", the name squeezed into the strip beside the wide
            // status): narrow Coming-soon tabs use the stacked layout too, so the name gets the full tab width.
            bool stacked = h < 120f || (comingSoon && width < GridA3InlineComingSoonMinWidth);
            if (stacked)
                strip = Mathf.Min(h - 20f, Mathf.Max(strip, 40f)); // FA line box at 18px needs ~40px; badge keeps the top 18px
            float nameMax = h >= 120f ? 22f : 15f;
            var scrim = new GameObject("TabScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            scrim.transform.SetParent(button.transform, false);
            scrim.transform.SetSiblingIndex(1);
            PlaceTopLeft(scrim.rectTransform, 2f, h - 2f - strip, width - 4f, strip);
            scrim.color = new Color(0.02f, 0.03f, 0.04f, 0.86f);
            scrim.raycastTarget = false;

            string countCopy = string.Empty;
            if (comingSoon)
                countCopy = SkirmishBattleLibraryArt.ComingSoon(fa); // capture rule: zero playable reads Coming soon, never 0/0
            else if (havePin)
            {
                string ratio = pin.WonCount + "/" + pin.PublishedCount;
                countCopy = fa ? SkirmishWidgetContext.ToPersianDigits(ratio) : ratio;
            }
            float countW = string.IsNullOrEmpty(countCopy) ? 0f : stacked ? Mathf.Clamp(width * (comingSoon ? 0.62f : 0.34f), 34f, width - 8f) : comingSoon ? Mathf.Clamp(width * 0.42f, 60f, 104f) : Mathf.Clamp(width * 0.26f, 34f, h >= 120f ? 64f : 52f);
            if (label != null)
            {
                label.transform.SetAsLastSibling();
                float nameW = stacked ? Mathf.Max(30f, width - 8f) : Mathf.Max(30f, width - 12f - countW);
                PlaceTopLeft(label.rectTransform, stacked ? 4f : fa ? 6f + countW : 6f, h - 2f - strip, nameW, strip);
                label.alignment = stacked ? TextAlignmentOptions.Center : fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.maxVisibleLines = 1;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = nameMax;
                label.color = selected ? new Color(1f, 0.88f, 0.5f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
                V3LocalizedTextBindingView binding = label.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(10f, nameMax);
            }

            if (countW > 0f)
            {
                var count = CreateLabel(button.transform, comingSoon ? "ComingSoon" : "TabCount", 14f);
                if (stacked)
                {
                    // Count badge in the top corner, above the name strip, on its own small scrim over the art.
                    float badgeH = Mathf.Clamp(h - strip - 2f, 16f, 20f); // v40: room for a 14px count
                    float bx = fa ? 4f : width - 4f - countW;
                    var badge = new GameObject("TabCountScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    badge.transform.SetParent(button.transform, false);
                    PlaceTopLeft(badge.rectTransform, bx, 0f, countW, badgeH);
                    badge.color = new Color(0.02f, 0.03f, 0.04f, 0.82f);
                    badge.raycastTarget = false;
                    count.transform.SetAsLastSibling();
                    PlaceTopLeft(count.rectTransform, bx + 2f, 0f, countW - 4f, badgeH);
                }
                else
                    PlaceTopLeft(count.rectTransform, fa ? 4f : width - 4f - countW, h - 2f - strip, countW, strip);
                SetCopy(count, countCopy, comingSoon ? 9f : 11f, comingSoon ? (h >= 120f ? 16f : 13f) : nameMax);
                count.alignment = stacked ? TextAlignmentOptions.Center : fa ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.overflowMode = TextOverflowModes.Overflow;
                count.isRightToLeftText = comingSoon && fa;
                count.color = new Color(1f, 0.86f, 0.42f, 1f);
                count.raycastTarget = false;
                if (stacked)
                {
                    // v40: 4:3 badge text was 9-13px. Hold it at 14px; tighten tracking rather than shrink or truncate.
                    FitReadableLine(count, countW - 4f, GridA3MinReadable);
                    Debug.Log("[GridA3] tabCount map=" + mapId + " font=" + count.fontSize.ToString("0.0") +
                        " width=" + (countW - 4f).ToString("0.0") + " locale=" + (fa ? "fa-IR" : "en"));
                }
            }

            if (selected)
                AddGridA3Frame(rect, width, h);
            Outline edge = button.GetComponent<Outline>();
            if (edge == null)
                edge = button.gameObject.AddComponent<Outline>();
            edge.effectColor = selected ? GridA3Gold : GridA3Edge;
            edge.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
        }

        /// <summary>v49b: below this tab width a Coming-soon tab stacks its status above the name (inline left ~130 px for it).</summary>
        private const float GridA3InlineComingSoonMinWidth = 300f;

        /// <summary>
        /// v49b (approved-3: the Grid is full-bleed with no panel ornaments): the OperationPreview's gold corner
        /// ornaments are raised above the grid by the depth pass and sat on the CONVOY row header ("Coming soon") and on
        /// the last cell's medal. Hidden while the Grid shows, back for the Map. Called every pass from PlaceLibraryFooter.
        /// </summary>
        private void SyncGridPreviewOrnaments()
        {
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            if (preview == null)
                return;
            bool grid = LibraryLevel == SkirmishLibraryLevel.Battlefield && LibraryLayout == SkirmishBattlefieldLayout.Grid;
            for (int i = 0; i < 4; i++)
            {
                Transform corner = preview.Find("OrnamentCorner" + i);
                if (corner != null && corner.gameObject.activeSelf == grid)
                    corner.gameObject.SetActive(!grid);
            }
        }

        private static void AddGridA3Frame(RectTransform cell, float width, float height)
        {
            float t = GridA3FrameSelected;
            AddGridA3Bar(cell, "A3SelectTop", 0f, 0f, width, t);
            AddGridA3Bar(cell, "A3SelectBottom", 0f, height - t, width, t);
            AddGridA3Bar(cell, "A3SelectLeft", 0f, 0f, t, height);
            AddGridA3Bar(cell, "A3SelectRight", width - t, 0f, t, height);
        }

        private static void AddGridA3Bar(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            bar.transform.SetParent(parent, false);
            bar.color = GridA3Gold;
            bar.raycastTarget = false;
            PlaceTopLeft(bar.rectTransform, x, y, w, h);
            bar.transform.SetAsLastSibling();
        }

        private static void HideCellChild(Transform cell, string name)
        {
            Transform child = cell != null ? cell.Find(name) : null;
            if (child != null)
                child.gameObject.SetActive(false);
        }

        /// <summary>Gap 3: dark header plate per column, icon on the leading side, two-line "FORCE / BASE" label.</summary>
        private void StyleApproved3ColumnHeader(Transform parent, Image icon, TMP_Text header, string armyLabel, string startLabel,
            float columnX, float top, float width, float height, bool fa)
        {
            if (parent == null || header == null)
                return;
            var plate = new GameObject("ColumnPlateA3", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            plate.transform.SetParent(parent, false);
            PlaceTopLeft(plate.rectTransform, columnX, top, width, height);
            plate.color = GridA3Plate;
            plate.raycastTarget = false;
            Outline edge = plate.gameObject.AddComponent<Outline>();
            edge.effectColor = GridA3Edge;
            edge.effectDistance = new Vector2(1f, -1f);
            plate.transform.SetSiblingIndex(Mathf.Max(0, (icon != null ? icon.transform.GetSiblingIndex() : header.transform.GetSiblingIndex())));

            if (height < 64f)
            {
                StyleCompactColumnHeader(parent, icon, header, armyLabel, startLabel, columnX, top, width, height, fa);
                return;
            }

            float iconSize = Mathf.Clamp(height * 0.5f, 16f, 38f); // v38: grows with the approved-3 header height
            if (icon != null)
                PlaceTopLeft(icon.rectTransform, fa ? columnX + width - iconSize - 4f : columnX + 4f, top + (height - iconSize) * 0.5f, iconSize, iconSize);
            float textX = fa ? columnX + 2f : columnX + iconSize + 8f;
            float textW = Mathf.Max(40f, width - iconSize - 10f);
            PlaceTopLeft(header.rectTransform, textX, top, textW, height);
            header.color = new Color(0.96f, 0.93f, 0.82f, 1f);
            float headerMax = height >= 64f ? 21f : 16f;
            SetCopy(header, armyLabel + "\n" + startLabel, 11f, headerMax);
            header.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            header.isRightToLeftText = fa;
            header.textWrappingMode = TextWrappingModes.NoWrap;
            header.overflowMode = TextOverflowModes.Overflow;
            header.maxVisibleLines = 2;
            header.enableAutoSizing = true;
            header.fontSizeMin = 11f;
            header.fontSizeMax = headerMax;
            V3LocalizedTextBindingView binding = header.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(11f, headerMax);
        }

        /// <summary>
        /// v40 4:3 header: the 40px plate squeezed "FORCE / BASE" beside a 20px icon to 11px and clipped the FA second
        /// line. Now a 16px icon sits in the leading corner beside the force line, and the base line takes the full
        /// column width; both lines hold 14px or more (tracking tightens before anything shrinks).
        /// </summary>
        private void StyleCompactColumnHeader(Transform parent, Image icon, TMP_Text header, string armyLabel, string startLabel,
            float columnX, float top, float width, float height, bool fa)
        {
            const float iconSize = 16f;
            float line = height * 0.5f;
            if (icon != null)
                PlaceTopLeft(icon.rectTransform, fa ? columnX + width - iconSize - 3f : columnX + 3f, top + (line - iconSize) * 0.5f, iconSize, iconSize);
            float firstX = fa ? columnX + 3f : columnX + iconSize + 5f;
            float firstW = Mathf.Max(30f, width - iconSize - 8f);
            PlaceTopLeft(header.rectTransform, firstX, top, firstW, line);
            SetCopy(header, armyLabel, GridA3MinReadable, 16f);
            StyleCompactHeaderLine(header, fa);
            FitReadableLine(header, firstW, GridA3MinReadable);

            string startName = "HeaderStart_" + header.name.Substring(Mathf.Min(header.name.Length, "Column_".Length));
            Transform existing = parent.Find(startName);
            TMP_Text start = existing != null ? existing.GetComponent<TMP_Text>() : CreateLabel(parent, startName, GridA3MinReadable);
            float secondW = Mathf.Max(30f, width - 6f);
            PlaceTopLeft(start.rectTransform, columnX + 3f, top + line, secondW, height - line);
            SetCopy(start, startLabel, GridA3MinReadable, 16f);
            StyleCompactHeaderLine(start, fa);
            start.alignment = TextAlignmentOptions.Center;
            FitReadableLine(start, secondW, GridA3MinReadable);
            Debug.Log("[GridA3] header col=" + header.name + " force=" + header.fontSize.ToString("0.0") +
                " base=" + start.fontSize.ToString("0.0") + " width=" + width.ToString("0.0") +
                " height=" + height.ToString("0.0") + " locale=" + (fa ? "fa-IR" : "en"));
        }

        private static void StyleCompactHeaderLine(TMP_Text text, bool fa)
        {
            text.color = new Color(0.96f, 0.93f, 0.82f, 1f);
            text.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            text.isRightToLeftText = fa;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.maxVisibleLines = 1;
            text.raycastTarget = false;
        }

        /// <summary>
        /// Single line at a fixed readable size: starts at min(16, current max), tightens tracking to -12 before dropping
        /// toward the floor, never below <paramref name="floor"/>, and pins the size against the refit pass.
        /// </summary>
        private static void FitReadableLine(TMP_Text text, float boxWidth, float floor)
        {
            if (text == null)
                return;
            float size = Mathf.Max(floor, Mathf.Min(16f, text.fontSizeMax > 0f ? text.fontSizeMax : 16f));
            float[] tracks = { 0f, -4f, -8f, -12f };
            bool fits = false;
            while (!fits)
            {
                for (int i = 0; i < tracks.Length && !fits; i++)
                {
                    text.enableAutoSizing = false;
                    text.fontSize = text.fontSizeMin = text.fontSizeMax = size;
                    text.characterSpacing = tracks[i];
                    fits = boxWidth <= 1f || text.GetPreferredValues(text.text, 9999f, 100f).x <= boxWidth + 0.5f;
                }

                if (fits || size <= floor + 0.01f)
                    break;
                size = Mathf.Max(floor, size - 1f);
            }

            V3LocalizedTextBindingView binding = text.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(size, size);
            LibraryFixedTextSize held = text.GetComponent<LibraryFixedTextSize>() ?? text.gameObject.AddComponent<LibraryFixedTextSize>();
            held.Size = size;
        }

        /// <summary>Gap 4: framed row tile the full row height; gold fill on the selected objective; larger icon and name.</summary>
        private void StyleApproved3RowLabel(RectTransform zone, SkirmishObjectiveTabState objective, bool selected, float rowHeight, bool fa)
        {
            if (zone == null)
                return;
            float w = LibraryLabelColumnWidth() - 4f;
            var tile = new GameObject("RowPlateA3", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            tile.transform.SetParent(zone, false);
            tile.transform.SetAsFirstSibling();
            PlaceTopLeft(tile.rectTransform, 0f, 0f, w, rowHeight);
            tile.color = selected ? new Color(0.36f, 0.26f, 0.07f, 0.96f) : GridA3Plate;
            tile.raycastTarget = false;
            Outline edge = tile.gameObject.AddComponent<Outline>();
            edge.effectColor = selected ? GridA3Gold : GridA3Edge;
            edge.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            Transform medallion = zone.Find("RowTile");
            if (medallion != null)
                medallion.gameObject.SetActive(false);

            // v49d (Game PM, approved-3): the row tile names the objective in full ("FRONTLINE CONTROL") on up to two
            // lines at 14 px or more, from the catalog (objective.TitleLocKey, SkirmishLibraryUiStrings.json), and the
            // status ("Coming soon" / won-of-playable) sits in a small badge at the tile's foot. The name is sized here
            // (16 -> 14, tracking 0 -> -12) so the later text pass, which autosizes Row_ labels 11-16 with wrapping,
            // still finds a fit at that size or larger.
            float innerW = w - 8f;
            Transform name = zone.Find("Row_" + objective.ObjectiveId);
            TMP_Text label = name != null ? name.GetComponent<TMP_Text>() : null;
            Transform countNode = zone.Find("RowCount_" + objective.ObjectiveId);
            TMP_Text count = countNode != null ? countNode.GetComponent<TMP_Text>() : null;
            bool badge = count != null && !string.IsNullOrWhiteSpace(count.text);
            float badgeH = badge ? GridRowBadgeHeight : 0f;
            string fullName = LibraryText(objective.TitleLocKey, fa);
            if (string.IsNullOrWhiteSpace(fullName))
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out fullName, out _);
            float size = GridRowNameMin;
            int lines = 0;
            bool fits = false;
            float nameH = 0f;
            if (label != null)
            {
                SetCopy(label, fullName, GridRowNameMin, GridRowNameMax);
                label.fontStyle = fa ? FontStyles.Bold : FontStyles.Bold | FontStyles.UpperCase;
                label.isRightToLeftText = fa;
                label.alignment = TextAlignmentOptions.Top;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                label.maxVisibleLines = GridRowNameMaxLines;
                fits = FitGridRowName(label, innerW, fa, out size, out lines);
                // v50 (Game PM): never below 14 px; FinishLibraryText's Row_ relax (11-16, Library.cs) is undone by
                // LibraryFixedTextSize, which LibraryPass re-applies straight after the Grid refit's text pass.
                LibraryFixedTextSize held = label.GetComponent<LibraryFixedTextSize>() ?? label.gameObject.AddComponent<LibraryFixedTextSize>();
                held.MaxLines = Mathf.Max(GridRowNameLines, lines);
                held.Size = size;
                held.Apply();
                nameH = Mathf.Max(1, lines) * size * (fa ? GridRowFaLine : GridRowEnLine);
                label.color = selected ? new Color(1f, 0.88f, 0.5f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
            }

            const float pad = 4f;
            const float gap = 3f;
            float iconSize = Mathf.Clamp(rowHeight - 2f * pad - nameH - badgeH - 2f * gap, 18f, 40f);
            float stack = iconSize + gap + nameH + (badge ? gap + badgeH : 0f);
            float top = Mathf.Max(pad, (rowHeight - stack) * 0.5f);
            Transform icon = zone.Find("RowIcon");
            if (icon != null)
                PlaceTopLeft((RectTransform)icon, (w - iconSize) * 0.5f, top, iconSize, iconSize);
            float nameY = top + iconSize + gap;
            if (label != null)
            {
                // Two full lines of room (the pass may pick a size up to 16), never past the badge.
                float room = Mathf.Max(nameH, GridRowNameLines * GridRowNameMax * (fa ? GridRowFaLine : GridRowEnLine));
                room = Mathf.Min(room, rowHeight - pad - badgeH - (badge ? gap : 0f) - nameY);
                PlaceTopLeft(label.rectTransform, 4f, nameY, innerW, Mathf.Max(nameH, room));
            }

            if (badge)
            {
                float badgeY = Mathf.Min(rowHeight - pad - badgeH, nameY + nameH + gap);
                float badgeW = Mathf.Min(innerW, count.GetPreferredValues(count.text, 9999f, badgeH).x + 14f);
                float badgeX = (w - badgeW) * 0.5f;
                Image plate = null;
                Transform plateNode = zone.Find("RowCountBadge_" + objective.ObjectiveId);
                if (plateNode != null)
                    plate = plateNode.GetComponent<Image>();
                if (plate == null)
                {
                    plate = new GameObject("RowCountBadge_" + objective.ObjectiveId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    plate.transform.SetParent(zone, false);
                    plate.raycastTarget = false;
                    Outline rim = plate.gameObject.AddComponent<Outline>();
                    rim.effectColor = GridA3Edge;
                    rim.effectDistance = new Vector2(1f, -1f);
                }

                plate.color = new Color(0.02f, 0.03f, 0.04f, 0.9f);
                PlaceTopLeft(plate.rectTransform, badgeX, badgeY, badgeW, badgeH);
                plate.transform.SetSiblingIndex(Mathf.Max(0, count.transform.GetSiblingIndex()));
                PlaceTopLeft(count.rectTransform, badgeX + 2f, badgeY, badgeW - 4f, badgeH);
                count.alignment = TextAlignmentOptions.Center;
                count.color = new Color(1f, 0.86f, 0.42f, 1f);
            }

            Debug.Log("[GridRowHeader] ratio=" + TheatreRatioLabel() + " locale=" + (fa ? "fa-IR" : "en") + " obj=" + objective.ObjectiveId +
                " name=" + fullName + " lines=" + lines + " size=" + size.ToString("0.0") + " box=" + innerW.ToString("0") +
                " badge=" + (badge ? count.text : "-") + " fits=" + (fits ? "1" : "0"));
        }

        private const float GridRowNameMin = 14f;
        private const float GridRowNameMax = 16f;
        private const float GridRowBadgeHeight = 20f;
        private const int GridRowNameLines = 2;     // word-wrapped full name
        private const int GridRowNameMaxLines = 3;  // v50: a word wider than the tile wraps inside it at 14 px
        private const float GridRowEnLine = 1.2f;
        private const float GridRowFaLine = 1.65f; // Noto Arabic line box (V3LocalizedTextBindingView.FitArabicLineBox)

        /// <summary>
        /// v49d: largest size (16 down to 14) and tracking (0 down to -12) at which the name wraps to at most two lines
        /// with no word wider than the box. Leaves the label at that size with autosizing off; false when even 14 px at
        /// -12 tracking does not fit (logged as fits=0).
        /// </summary>
        private static bool FitGridRowName(TMP_Text label, float boxWidth, bool fa, out float size, out int lines)
        {
            float[] tracks = { 0f, -3f, -6f, -9f, -12f };
            string[] words = (label.text ?? string.Empty).Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            label.enableAutoSizing = false;
            for (float s = GridRowNameMax; s >= GridRowNameMin - 0.01f; s -= 1f)
            {
                for (int t = 0; t < tracks.Length; t++)
                {
                    label.fontSize = label.fontSizeMin = label.fontSizeMax = s;
                    label.characterSpacing = tracks[t];
                    bool wordsFit = true;
                    for (int i = 0; i < words.Length && wordsFit; i++)
                        wordsFit = label.GetPreferredValues(words[i], 9999f, 100f).x <= boxWidth + 0.5f;
                    if (!wordsFit)
                        continue;
                    label.rectTransform.sizeDelta = new Vector2(boxWidth, label.rectTransform.sizeDelta.y);
                    label.ForceMeshUpdate();
                    lines = label.textInfo != null ? label.textInfo.lineCount : 0;
                    if (lines <= GridRowNameLines)
                    {
                        size = s;
                        return true;
                    }
                }
            }

            // v50 (Game PM): no size below 14. A word that is wider than the tile even at -12 tracking (BREAKTHROUGH in a
            // narrow 4:3 header) wraps inside the tile at 14 px (TMP breaks an over-long word across lines); it fits when
            // it takes no more than GridRowNameMaxLines lines.
            size = GridRowNameMin;
            label.fontSize = label.fontSizeMin = label.fontSizeMax = size;
            label.characterSpacing = tracks[tracks.Length - 1];
            label.textWrappingMode = TextWrappingModes.Normal;
            label.maxVisibleLines = GridRowNameMaxLines;
            label.rectTransform.sizeDelta = new Vector2(boxWidth, label.rectTransform.sizeDelta.y);
            label.ForceMeshUpdate();
            lines = label.textInfo != null ? label.textInfo.lineCount : 0;
            return lines > 0 && lines <= GridRowNameMaxLines;
        }
    }
}
