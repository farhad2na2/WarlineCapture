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
                    // Locked/empty: dark plate. The art stays assigned (capture requires real art) but is not drawn;
                    // tint is final-white so no pending tag is expected.
                    preview.color = Color.white;
                    preview.enabled = false;
                    HideCellChild(cell, "PreviewPending");
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

        /// <summary>v37 P1 Grid tab band: 60px (SkirmishZoneTabWidget.Height stays 52; P2 agreed option b).</summary>
        private const float GridA3TabHeight = 60f;
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

            float strip = Mathf.Clamp(h * 0.4f, 20f, 26f);
            var scrim = new GameObject("TabScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            scrim.transform.SetParent(button.transform, false);
            scrim.transform.SetSiblingIndex(1);
            PlaceTopLeft(scrim.rectTransform, 2f, h - 2f - strip, width - 4f, strip);
            scrim.color = new Color(0.02f, 0.03f, 0.04f, 0.86f);
            scrim.raycastTarget = false;

            string countCopy = string.Empty;
            bool comingSoon = havePin && pin.PublishedCount <= 0;
            if (comingSoon)
                countCopy = SkirmishBattleLibraryArt.ComingSoon(fa); // capture rule: zero playable reads Coming soon, never 0/0
            else if (havePin)
            {
                string ratio = pin.WonCount + "/" + pin.PublishedCount;
                countCopy = fa ? SkirmishWidgetContext.ToPersianDigits(ratio) : ratio;
            }
            float countW = string.IsNullOrEmpty(countCopy) ? 0f : comingSoon ? Mathf.Clamp(width * 0.42f, 60f, 104f) : Mathf.Clamp(width * 0.26f, 34f, 52f);
            if (label != null)
            {
                label.transform.SetAsLastSibling();
                float nameW = Mathf.Max(30f, width - 12f - countW);
                PlaceTopLeft(label.rectTransform, fa ? 6f + countW : 6f, h - 2f - strip, nameW, strip);
                label.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.maxVisibleLines = 1;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = 15f;
                label.color = selected ? new Color(1f, 0.88f, 0.5f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
                V3LocalizedTextBindingView binding = label.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(10f, 15f);
            }

            if (countW > 0f)
            {
                var count = CreateLabel(button.transform, comingSoon ? "ComingSoon" : "TabCount", 14f);
                PlaceTopLeft(count.rectTransform, fa ? 4f : width - 4f - countW, h - 2f - strip, countW, strip);
                SetCopy(count, countCopy, comingSoon ? 9f : 11f, comingSoon ? 13f : 15f);
                count.alignment = fa ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.overflowMode = TextOverflowModes.Overflow;
                count.isRightToLeftText = comingSoon && fa;
                count.color = new Color(1f, 0.86f, 0.42f, 1f);
                count.raycastTarget = false;
            }

            if (selected)
                AddGridA3Frame(rect, width, h);
            Outline edge = button.GetComponent<Outline>();
            if (edge == null)
                edge = button.gameObject.AddComponent<Outline>();
            edge.effectColor = selected ? GridA3Gold : GridA3Edge;
            edge.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
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

            float iconSize = Mathf.Clamp(height * 0.6f, 16f, 26f);
            if (icon != null)
                PlaceTopLeft(icon.rectTransform, fa ? columnX + width - iconSize - 4f : columnX + 4f, top + (height - iconSize) * 0.5f, iconSize, iconSize);
            float textX = fa ? columnX + 2f : columnX + iconSize + 8f;
            float textW = Mathf.Max(40f, width - iconSize - 10f);
            PlaceTopLeft(header.rectTransform, textX, top, textW, height);
            header.color = new Color(0.96f, 0.93f, 0.82f, 1f);
            SetCopy(header, armyLabel + "\n" + startLabel, 11f, 16f);
            header.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            header.isRightToLeftText = fa;
            header.textWrappingMode = TextWrappingModes.NoWrap;
            header.overflowMode = TextOverflowModes.Overflow;
            header.maxVisibleLines = 2;
            header.enableAutoSizing = true;
            header.fontSizeMin = 11f;
            header.fontSizeMax = 16f;
            V3LocalizedTextBindingView binding = header.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(11f, 16f);
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

            float iconSize = Mathf.Clamp(rowHeight * 0.36f, 24f, 40f);
            float iconY = Mathf.Max(4f, rowHeight * 0.5f - iconSize - 2f);
            Transform medallion = zone.Find("RowTile");
            if (medallion != null)
                medallion.gameObject.SetActive(false);
            Transform icon = zone.Find("RowIcon");
            if (icon != null)
                PlaceTopLeft((RectTransform)icon, (w - iconSize) * 0.5f, iconY, iconSize, iconSize);
            float textY = iconY + iconSize + 2f;
            Transform name = zone.Find("Row_" + objective.ObjectiveId);
            if (name != null && name.TryGetComponent(out TMP_Text label))
            {
                PlaceTopLeft(label.rectTransform, 3f, textY, w - 6f, Mathf.Max(20f, rowHeight * 0.24f));
                label.color = selected ? new Color(1f, 0.88f, 0.5f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
                label.fontStyle = FontStyles.Bold;
                label.maxVisibleLines = 2;
                label.isRightToLeftText = fa;
            }
            Transform count = zone.Find("RowCount_" + objective.ObjectiveId);
            if (count != null)
                PlaceTopLeft((RectTransform)count, 3f, Mathf.Min(rowHeight - 20f, textY + Mathf.Max(20f, rowHeight * 0.24f)), w - 6f, 18f);
        }
    }
}
