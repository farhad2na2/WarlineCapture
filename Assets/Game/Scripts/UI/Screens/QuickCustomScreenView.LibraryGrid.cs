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
        private void PresentGrid(SkirmishBattlefieldState field, bool fa)
        {
            ApplyGridPlate(field);
            ClearChildren(_libraryGrid);
            if (field.GridCards == null || field.Tabs == null)
                return;
            MeasureLibraryGrid(out float gridWidth, out float gridHeight);
            int columns = LibraryGridLogicalColumns;
            int objectives = field.Tabs.Length;
            float cardWidth = GridCardWidth(gridWidth, columns, LibraryGridRightPad);
            bool roomy = cardWidth >= LibraryGridRoomyCardWidth;
            bool wideBands = IsWideLibraryFrame(_libraryGrid);
            // v38 P1: approved-3 tab and header heights on 16:9 / 20:9 (4:3 keeps the 60px tabs and 40px headers).
            float headerHeight = ResolveGridA3Bands(wideBands, gridHeight, roomy ? LibraryGridRoomyHeaderHeight : 40f, objectives);
            float headerTop = GridA3TabBandBottom + 2f; // v37 P1: image tabs (widget stays 52)
            float scrollTop = headerTop + headerHeight + 4f;
            float viewport = Mathf.Max(120f, gridHeight - scrollTop);
            // The text block under the picture used to consume the height the tabs gave up,
            // so a 155px card kept a 62px preview. Shorter title and subtitle leave the
            // picture at about half the card while the four rows still fit a wide frame.
            // v37 approved-3 (P1): cells carry no text block, so the art takes the whole cell (StyleApproved3Cell).
            float titleHeight = 2f;
            float subtitleHeight = 2f;
            bool stayInView = IsWideLibraryFrame(_libraryGrid);
            ResolveGridCard(viewport, objectives, titleHeight, subtitleHeight, stayInView, out float cardHeight, out float previewHeight);
            float stack = objectives * cardHeight + Mathf.Max(0, objectives - 1) * LibraryGridRowGap;
            float contentHeight = LibraryGridContentTop + stack + LibraryGridContentBottom;
            bool scrolls = contentHeight > viewport + 2f;
            if (scrolls)
                cardWidth = GridCardWidth(gridWidth, columns, LibraryGridRightPad + 16f);

            PresentBattlefieldTabs(field.MapId, fa);
            string[] armies = { "G", "A", "C" };
            string[] starts = { "F", "E" };
            for (int armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                for (int startIndex = 0; startIndex < starts.Length; startIndex++)
                {
                    int column = armyIndex * 2 + startIndex;
                    float columnX = LibraryLabelColumnWidth() + column * (cardWidth + LibraryGridCardGap);
                    float iconX = fa ? columnX + cardWidth - 20f : columnX + 2f;
                    var headerIcon = new GameObject("ColumnIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    headerIcon.transform.SetParent(_libraryGrid, false);
                    PlaceTopLeft(headerIcon.rectTransform, iconX, headerTop + (headerHeight - 18f) * 0.5f, 18f, 18f);
                    ShowIcon(headerIcon, SkirmishBattleLibraryArt.ArmyIcon(armies[armyIndex]), Color.white);
                    SkirmishBattleLibraryArt.ProfileCopy(armies[armyIndex], fa, out string armyLabel);
                    SkirmishBattleLibraryArt.ProfileCopy(starts[startIndex], fa, out string startLabel);
                    float labelX = fa ? columnX : columnX + 22f;
                    float labelWidth = Mathf.Max(48f, cardWidth - (fa ? 24f : 26f));
                    var header = CreateLabel(_libraryGrid, "Column_" + column, 22f);
                    PlaceTopLeft(header.rectTransform, labelX, headerTop, labelWidth, headerHeight);
                    header.alignment = TextAlignmentOptions.Center;
                    header.isRightToLeftText = fa;
                    header.color = new Color(0.96f, 0.93f, 0.82f, 1f);
                    string headerCopy = fa || roomy ? armyLabel + " · " + startLabel : armyLabel + "\n" + startLabel;
                    SetCopy(header, headerCopy, roomy ? 18f : 16f, roomy ? 26f : 22f);
                    ShowColumnHeader(header, fa);
                    StyleApproved3ColumnHeader(_libraryGrid, headerIcon, header, armyLabel, startLabel, columnX, headerTop, cardWidth, headerHeight, fa); // v37 P1
                }
            }

            RectTransform content = CreateGridScroll(scrollTop, out ScrollRect scroll);
            var slots = new GridCardSlots
            {
                Preview = previewHeight,
                Title = titleHeight,
                Subtitle = subtitleHeight,
                Medal = LibraryGridMedalSize
            };
            for (int row = 0; row < objectives; row++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[row];
                float y = LibraryGridContentTop + row * (cardHeight + LibraryGridRowGap);
                var zone = new GameObject("ZoneRow_" + objective.ObjectiveId, typeof(RectTransform)).GetComponent<RectTransform>();
                zone.SetParent(content, false);
                PlaceTopLeft(zone, 0f, y, gridWidth, cardHeight);
                PresentGridRowLabel(zone, objective, fa, 0f);
                StyleApproved3RowLabel(zone, objective, string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase), cardHeight, fa); // v37 P1
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    if (index >= field.GridCards.Length)
                        break;
                    float x = LibraryLabelColumnWidth() + column * (cardWidth + LibraryGridCardGap);
                    SkirmishBattleCardState card = field.GridCards[index];
                    // Card bodies come only from P2's LibraryCardArt path (CreateBattleCard / CreateEmptyGridCell).
                    RectTransform widget = IsEmptyCard(card)
                        ? CreateEmptyGridCell(zone, objective.ObjectiveId, armies[column / 2], starts[column % 2], card.PreviewSpriteKey, card.MapId, slots, cardWidth)
                        : CreateBattleCard(zone, card, false, cardWidth, cardHeight, slots);
                    PlaceTopLeft(widget, x, 0f, cardWidth, cardHeight);
                    AddSiblingShadow(widget);
                    if (!IsEmptyCard(card))
                        ApplyGridCardAccess(widget, card);
                    StyleApproved3Cell(widget, card, IsEmptyCard(card), cardWidth, cardHeight, fa); // v37 P1
                }
            }

            content.sizeDelta = new Vector2(0f, contentHeight);
            scroll.vertical = scrolls;
            scroll.horizontal = false;
            if (scrolls)
                scroll.verticalScrollbar = CreateGridScrollbar(scroll.transform as RectTransform);
            if (_libraryGrid != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_libraryGrid);
            Canvas.ForceUpdateCanvases();
            if (scrolls)
                scroll.verticalNormalizedPosition = IsWideLibraryFrame(_libraryGrid) ? 1f : 0f;
        }

        private void PresentGridRowLabel(Transform parent, SkirmishObjectiveTabState objective, bool fa, float y)
        {
            var rowIcon = new GameObject("RowIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            rowIcon.transform.SetParent(parent, false);
            // Icon tile (approved-3): a medallion plate behind the objective icon.
            var rowTile = new GameObject("RowTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            rowTile.transform.SetParent(parent, false);
            float tileSize = Mathf.Min(LibraryGridOriginX - 8f, 50f);
            PlaceTopLeft(rowTile.rectTransform, (LibraryGridOriginX - 4f - tileSize) * 0.5f, y + 2f, tileSize, tileSize);
            rowTile.sprite = ZoneMedallionSprite();
            rowTile.preserveAspect = true;
            rowTile.raycastTarget = false;
            rowIcon.transform.SetAsLastSibling();
            PlaceTopLeft(rowIcon.rectTransform, (LibraryGridOriginX - 4f - 30f) * 0.5f, y + 2f + (tileSize - 30f) * 0.5f, 30f, 30f);
            ShowIcon(rowIcon, SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId), Color.white);
            SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string rowName);
            var rowLabel = CreateLabel(parent, "Row_" + objective.ObjectiveId, 13f);
            PlaceTopLeft(rowLabel.rectTransform, 1f, y + 54f, LibraryLabelColumnWidth() - 6f, 24f);
            rowLabel.alignment = TextAlignmentOptions.Center;
            rowLabel.textWrappingMode = TextWrappingModes.Normal;
            rowLabel.overflowMode = TextOverflowModes.Ellipsis;
            SetCopy(rowLabel, rowName, 15f, 20f);
            var rowCount = CreateLabel(parent, "RowCount_" + objective.ObjectiveId, 13f);
            bool pending = objective.PublishedCount <= 0;
            if (pending)
            {
                PlaceTopLeft(rowCount.rectTransform, 1f, y + 76f, LibraryLabelColumnWidth() - 4f, 40f);
                rowCount.textWrappingMode = TextWrappingModes.Normal;
                rowCount.maxVisibleLines = 2;
            }
            else
            {
                PlaceTopLeft(rowCount.rectTransform, 2f, y + 78f, LibraryLabelColumnWidth() - 8f, 22f);
                rowCount.textWrappingMode = TextWrappingModes.NoWrap;
                rowCount.maxVisibleLines = 1;
            }

            rowCount.alignment = TextAlignmentOptions.Center;
            rowCount.isRightToLeftText = pending && fa;
            rowCount.overflowMode = TextOverflowModes.Overflow;
            SetCopy(
                rowCount,
                pending ? SkirmishBattleLibraryArt.ComingSoon(fa) : objective.WonCount + "/" + objective.PublishedCount,
                pending ? 12f : 16f,
                pending ? 14f : 20f);
        }

        private RectTransform CreateGridScroll(float top, out ScrollRect scroll)
        {
            var root = new GameObject("GridScroll", typeof(RectTransform), typeof(ScrollRect)).GetComponent<RectTransform>();
            root.SetParent(_libraryGrid, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 1f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = new Vector2(0f, -top);

            var viewportImage = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D)).GetComponent<Image>();
            viewportImage.transform.SetParent(root, false);
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            RectTransform viewport = viewportImage.rectTransform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 28f;
            return content;
        }

        private static Scrollbar CreateGridScrollbar(RectTransform scrollRoot)
        {
            var bar = new GameObject("GridScrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar)).GetComponent<Scrollbar>();
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.SetParent(scrollRoot, false);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(12f, -12f);
            rect.anchoredPosition = new Vector2(-4f, 0f);
            Image track = bar.GetComponent<Image>();
            track.color = new Color(0.10f, 0.08f, 0.06f, 0.92f);
            track.raycastTarget = true;
            var handleImage = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            handleImage.transform.SetParent(rect, false);
            handleImage.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            handleImage.raycastTarget = true;
            RectTransform handle = handleImage.rectTransform;
            handle.anchorMin = new Vector2(0f, 0f);
            handle.anchorMax = new Vector2(1f, 1f);
            handle.offsetMin = new Vector2(2f, 2f);
            handle.offsetMax = new Vector2(-2f, -2f);
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            return bar;
        }

        private static bool IsWideLibraryFrame(Transform node)
        {
            Canvas canvas = node != null ? node.GetComponentInParent<Canvas>() : null;
            RectTransform rect = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;
            if (rect == null || rect.rect.height < 200f)
                return true;
            return rect.rect.width / rect.rect.height >= 1.6f;
        }

        private void MeasureLibraryGrid(out float width, out float height)
        {
            if (_libraryGrid != null && (_libraryGrid.rect.width < 200f || _libraryGrid.rect.height < 200f))
                Canvas.ForceUpdateCanvases();
            width = _libraryGrid != null && _libraryGrid.rect.width > 200f ? _libraryGrid.rect.width : 1008f;
            height = _libraryGrid != null && _libraryGrid.rect.height > 200f ? _libraryGrid.rect.height : 740f;
        }

        private static void ResolveGridCard(float viewport, int rows, out float cardHeight, out float previewHeight)
        {
            ResolveGridCard(viewport, rows, LibraryGridTitleHeight, LibraryGridSubtitleHeight, false, out cardHeight, out previewHeight);
        }

        /// <summary>
        /// The capture rejects a preview under 40% of the card. Wide frames also keep
        /// every card inside the viewport, so the picture aims at half the card and
        /// the card does not grow past the rows that fit.
        /// </summary>
        private static void ResolveGridCard(
            float viewport,
            int rows,
            float titleHeight,
            float subtitleHeight,
            bool stayInView,
            out float cardHeight,
            out float previewHeight)
        {
            const float previewAim = 0.50f;
            float chrome = LibraryGridTextGap + titleHeight + subtitleHeight +
                LibraryGridMedalGap + LibraryGridMedalSize + LibraryGridCardBottom;
            int count = Mathf.Max(1, rows);
            float available = viewport - LibraryGridContentTop - LibraryGridContentBottom -
                Mathf.Max(0, count - 1) * LibraryGridRowGap;
            float fitted = available / count;
            float minCard = chrome / (1f - previewAim);
            cardHeight = fitted >= minCard ? fitted : minCard;
            if (stayInView && cardHeight > fitted + 0.5f)
                cardHeight = Mathf.Max(1f, fitted);
            previewHeight = Mathf.Max(1f, cardHeight - chrome);
        }

        private float GridCardWidth(float gridWidth, int columns, float rightPad)
        {
            float available = gridWidth - LibraryLabelColumnWidth() - rightPad;
            float gaps = LibraryGridCardGap * Mathf.Max(0, columns - 1);
            return columns > 0 ? Mathf.Max(1f, (available - gaps) / columns) : available;
        }

        private static void ShowColumnHeader(TMP_Text header, bool fa)
        {
            if (header == null)
                return;
            float width = header.rectTransform != null ? header.rectTransform.sizeDelta.x : 0f;
            bool roomy = width >= 220f;
            header.alignment = TextAlignmentOptions.Center;
            header.isRightToLeftText = fa;
            header.textWrappingMode = fa || roomy ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            header.overflowMode = TextOverflowModes.Overflow;
            header.enableAutoSizing = true;
            header.fontSizeMin = roomy ? 16f : fa ? 16f : 14f;
            header.fontSizeMax = roomy ? 24f : fa ? 20f : 17f;
            header.maxVisibleLines = fa || roomy ? 1 : 2;
            header.margin = new Vector4(1f, 0f, 1f, 0f);
            V3LocalizedTextBindingView binding = header.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(header.fontSizeMin, header.fontSizeMax);
        }

        private void ShowCardSubtitle(TMP_Text subtitle, string copy, bool fa, bool singleLine)
        {
            if (subtitle == null)
                return;
            float textWidth = subtitle.rectTransform != null ? subtitle.rectTransform.sizeDelta.x : 0f;
            bool roomy = !singleLine && textWidth >= LibraryGridRoomyTextWidth;
            float floor = singleLine ? ReferenceSubtitleFloor() : 14f;
            float max = singleLine ? Mathf.Max(20f, floor) : 16f;
            subtitle.color = new Color(0.9f, 0.93f, 0.9f, 1f);
            SetCopy(subtitle, copy, floor, max);
            subtitle.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            subtitle.isRightToLeftText = fa && ContainsArabicScript(copy);
            bool oneLine = singleLine || roomy;
            subtitle.textWrappingMode = oneLine ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            subtitle.overflowMode = oneLine ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = floor;
            subtitle.fontSizeMax = max;
            subtitle.maxVisibleLines = oneLine ? 1 : 2;
            V3LocalizedTextBindingView binding = subtitle.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(floor, max);
        }

        private void PresentBattlefieldTabs(string selectedMapId, bool fa)
        {
            if (_libraryGrid == null)
                return;
            int tabCount = SkirmishBattleLibraryArt.MapIds.Length;
            float gridWidth = _libraryGrid.rect.width;
            float tabWidth = SkirmishZoneTabWidget.Width(gridWidth, tabCount);
            for (int i = 0; i < tabCount; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                var button = CreateChromeButton(_libraryGrid, "BattlefieldTab_" + mapId);
                PlaceTopLeft(
                    button.GetComponent<RectTransform>(),
                    SkirmishZoneTabWidget.Left(i, gridWidth, tabCount),
                    SkirmishZoneTabWidget.OriginY,
                    tabWidth,
                    SkirmishZoneTabWidget.Height);
                SkirmishZoneTabWidget.Apply(button, string.Equals(mapId, selectedMapId, StringComparison.OrdinalIgnoreCase));
                // Merge #66 2e1321e52: P2's ZoneTabName (own line, 18px floor, [ZoneTab] log) inside P1's approved-3
                // name strip; the won/playable count stays P1's TabCount, so no separate ZoneTabStatus here.
                var name = CreateLabel(button.transform, "ZoneTabName", SkirmishZoneTabWidget.NameFontFloor);
                KeepAuthoredSize(name.rectTransform);
                name.alignment = TextAlignmentOptions.Center;
                SetCopy(name, MapDisplayTitle(mapId, fa), SkirmishZoneTabWidget.NameFontFloor, SkirmishZoneTabWidget.NameFontFloor);
                StyleApproved3BattlefieldTab(button, name, mapId, string.Equals(mapId, selectedMapId, StringComparison.OrdinalIgnoreCase),
                    SkirmishZoneTabWidget.Left(i, gridWidth, tabCount), tabWidth, fa); // v37 P1
                SkirmishZoneTabWidget.FitName(name);
                RestoreApproved3TabNameColour(name, string.Equals(mapId, selectedMapId, StringComparison.OrdinalIgnoreCase));
                string captured = mapId;
                button.onClick.AddListener(() => SelectGridBattlefield(captured));
            }
        }

        private RectTransform CreateGridCellWidget(Transform parent, SkirmishBattleCardState card, string objectiveId, string army, string start,
            float width, float height, bool fa)
        {
            if (IsEmptyCard(card))
            {
                card.ObjectiveId = objectiveId;
                card.ArmyProfile = army;
                card.StartProfile = start;
            }

            SkirmishWidgetContext context = LibraryWidgetContext(card);
            ResolveGridPreview(card.PreviewSpriteKey, card.ScenarioId, card.MapId, out bool dimmed);
            string pendingText = fa ? "پیش‌نمایش در راه" : "Preview pending";
            return SkirmishBattleCardWidget.BuildGridCell(parent, card, new Vector2(width, height), context, OnLibraryCard,
                !IsEmptyCard(card) && dimmed, pendingText);
        }

        private void LockZoneTabs()
        {
            if (_libraryGrid == null || !_libraryGrid.gameObject.activeInHierarchy)
                return;
            if (LibraryLevel != SkirmishLibraryLevel.Battlefield || LibraryLayout != SkirmishBattlefieldLayout.Grid)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            for (int i = 0; i < _libraryGrid.childCount; i++)
            {
                Transform tab = _libraryGrid.GetChild(i);
                if (tab == null || !tab.name.StartsWith("BattlefieldTab_", StringComparison.Ordinal))
                    continue;
                string mapId = tab.name.Substring("BattlefieldTab_".Length);
                TMP_Text name = tab.Find("ZoneTabName")?.GetComponent<TMP_Text>();
                TMP_Text status = tab.Find("ZoneTabStatus")?.GetComponent<TMP_Text>();
                if (name != null)
                {
                    KeepAuthoredSize(name.rectTransform);
                    SkirmishZoneTabWidget.FitName(name);
                    if (tab.Find("TabScrim") != null)
                        RestoreApproved3TabNameColour(name, string.Equals(mapId, _libraryViewModel?.Battlefield.MapId, StringComparison.OrdinalIgnoreCase)); // P1 approved-3: light name on the dark strip
                    else
                        SkirmishZoneTabWidget.StyleLabel(name);
                }

                if (status != null)
                {
                    KeepAuthoredSize(status.rectTransform);
                    SkirmishZoneTabWidget.FitStatus(status);
                    status.isRightToLeftText = fa && SkirmishBattleLibraryArt.IsRenderedComingSoon(status.text);
                    SkirmishZoneTabWidget.StyleLabel(status);
                }

                SkirmishZoneTabWidget.Log(name, mapId, MapDisplayTitle(mapId, fa));
            }
        }

        // P1 approved-3 tabs draw the name on a dark scrim, so P2's parchment StyleLabel colour would vanish there.
        private static void RestoreApproved3TabNameColour(TMP_Text name, bool selected)
        {
            if (name == null)
                return;
            name.color = selected ? new Color(1f, 0.88f, 0.5f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
        }

        private void SelectGridBattlefield(string mapId)
        {
            EnsureLibraryViewModel();
            bool sameBattlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield &&
                string.Equals(_libraryViewModel.Battlefield.MapId, mapId, StringComparison.OrdinalIgnoreCase);
            if (!sameBattlefield)
                _libraryViewModel.SelectMapCollection(mapId);
            bool gridAlready = _libraryViewModel.Battlefield.Layout == SkirmishBattlefieldLayout.Grid;
            if (!gridAlready)
                _libraryViewModel.SetLayout(SkirmishBattlefieldLayout.Grid);
            else if (sameBattlefield)
                UIAudioEventGateway.Raise(UIAudioEventKind.TabSelect); // v41 audit: re-tapping the selected tab was silent
            _mapFilter = mapId;
            PresentScenarioChoices();
        }

        private RectTransform CreateEmptyGridCell(Transform parent, string objectiveId, string army, string start, string previewKey, string mapId, GridCardSlots slots, float width)
        {
            var rect = new GameObject("EmptyCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var plate = rect.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.07f, 0.08f, 0.92f);
            plate.raycastTarget = false;
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(rect, false);
            float previewHeight = slots.Preview;
            if (previewHeight <= 1f)
                ResolveGridCard(740f, 4, out _, out previewHeight);
            float titleHeight = slots.Title > 1f ? slots.Title : LibraryGridTitleHeight;
            float subtitleHeight = slots.Subtitle > 1f ? slots.Subtitle : LibraryGridSubtitleHeight;
            float medalSize = slots.Medal > 1f ? slots.Medal : LibraryGridMedalSize;
            Texture2D gridPreview = ResolveGridPreview(previewKey, null, mapId, out bool dimmed);
            PlaceCoverPreview(preview, width, previewHeight, gridPreview, dimmed);
            preview.transform.SetAsFirstSibling();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            MarkPendingPreview(rect, fa, dimmed, width, previewHeight);
            SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out _, out string objectiveLabel);
            SkirmishBattleLibraryArt.ProfileCopy(army, fa, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(start, fa, out string startLabel);
            string detail = string.IsNullOrEmpty(startLabel) ? armyLabel : armyLabel + " · " + startLabel;
            float textY = previewHeight + LibraryGridTextGap;
            var title = CreateLabel(rect, "Title", 16f);
            PlaceTopLeft(title.rectTransform, 4f, textY, width - 8f, titleHeight);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            SetCopy(title, objectiveLabel, 15f, 18f);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.isRightToLeftText = fa && ContainsArabicScript(objectiveLabel);
            var subtitle = CreateLabel(rect, "Subtitle", 15f);
            PlaceTopLeft(subtitle.rectTransform, 4f, textY + titleHeight, width - 8f, subtitleHeight);
            ShowCardSubtitle(subtitle, detail, fa, false);
            AddMedalIcons(rect, SkirmishMedalMask.None, 4f, textY + titleHeight + subtitleHeight + LibraryGridMedalGap, medalSize);
            AddGridCornerMedal(rect, SkirmishMedalMask.None, width, previewHeight, fa);
            StyleBattleCard(rect, false, false, false);
            return rect;
        }

        /// <summary>approved-3: one medal circle in the preview's bottom corner (best earned tier, else a dim Recruit).
        /// Drawn inside the preview rect so #66 card/plate/seat geometry is unchanged.</summary>
        private void AddGridCornerMedal(Transform parent, SkirmishMedalMask medals, float width, float previewHeight, bool fa)
        {
            SkirmishDifficultyId[] order = { SkirmishDifficultyId.Commander, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Recruit };
            SkirmishDifficultyId shown = SkirmishDifficultyId.Recruit;
            bool earned = false;
            for (int i = 0; i < order.Length; i++)
            {
                if (MedalEarned(medals, order[i])) { shown = order[i]; earned = true; break; }
            }
            // medal sprite is ~86% of its rect: 30 px rect draws ~26 px (approved-3). Capped by preview height and cell width.
            float size = Mathf.Min(30f, previewHeight * 0.56f, width * 0.2f);
            if (size < 16f)
                return;
            float x = fa ? 4f : width - size - 4f;
            var image = new GameObject("CornerMedal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            PlaceTopLeft(image.rectTransform, x, previewHeight - size - 2f, size, size);
            PaintDifficultyMedal(image, shown, earned);
        }

        private void MarkPendingPreview(Transform card, bool fa, bool dimmed, float width, float previewHeight)
        {
            if (!dimmed || card == null || previewHeight < 24f)
                return;
            bool roomy = width >= LibraryGridRoomyCardWidth;
            float bar = roomy ? 28f : 20f;
            float textMin = roomy ? 12f : 9f;
            float textMax = roomy ? 14f : 12f;
            var tag = new GameObject("PreviewPending", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            tag.transform.SetParent(card, false);
            PlaceTopLeft(tag.rectTransform, 0f, Mathf.Max(0f, previewHeight - bar), width, bar);
            tag.color = new Color(0.10f, 0.07f, 0.04f, 0.92f);
            tag.raycastTarget = false;
            var label = CreateLabel(tag.transform, "PendingLabel", textMin);
            PlaceTopLeft(label.rectTransform, 4f, 2f, Mathf.Max(40f, width - 8f), Mathf.Max(16f, bar - 4f));
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.86f, 0.55f, 1f);
            label.isRightToLeftText = fa;
            SetCopy(label, fa ? "پیش‌نمایش در انتظار" : "Preview pending", textMin, textMax);
            label.alignment = TextAlignmentOptions.Center;
            label.isRightToLeftText = fa;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = textMin;
            label.fontSizeMax = textMax;
            label.fontSize = textMin;
            label.maxVisibleLines = 1;
            V3LocalizedTextBindingView binding = label.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(textMin, textMax);
            tag.transform.SetAsLastSibling();
        }

        private void AddNewTag(Transform parent, bool fa, float width)
        {
            var tag = new GameObject("NewTag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            tag.transform.SetParent(parent, false);
            tag.transform.SetAsLastSibling();
            PlaceTopLeft(tag.rectTransform, Mathf.Max(4f, width - 54f), 4f, 48f, 20f);
            Sprite badge = LoadLibraryIcon(SkirmishBattleLibraryArt.NewBadge);
            tag.sprite = badge;
            tag.preserveAspect = badge != null;
            tag.color = new Color(0.95f, 0.62f, 0.12f, 1f);
            tag.raycastTarget = false;
            var badgeLabel = CreateLabel(tag.transform, "NewLabel", 12f);
            Stretch(badgeLabel.rectTransform, 2f, 1f, 1f, 1f);
            badgeLabel.alignment = TextAlignmentOptions.Center;
            badgeLabel.color = new Color(0.12f, 0.07f, 0.02f, 1f);
            KeepSingleLine(badgeLabel, 11f, 13f);
            badgeLabel.overflowMode = TextOverflowModes.Overflow;
            SetCopy(badgeLabel, fa ? "نو" : "NEW", 11f, 14f);
            KeepSingleLine(badgeLabel, 11f, 14f);
            badgeLabel.overflowMode = TextOverflowModes.Overflow;
        }

        /// <summary>
        /// A missing scenario capture uses the approved battlefield plate at full strength.
        /// The dimmed "Preview pending" tag stays only when that plate is missing too.
        /// </summary>
        private static Texture2D ResolveGridPreview(string key, string scenarioId, string mapId, out bool dimmed)
        {
            dimmed = false;
            Texture2D scenario = LoadScenarioPreview(key);
            if (scenario == null)
                scenario = LoadScenarioPreview(string.IsNullOrEmpty(scenarioId) ? null : "skirmish.preview." + scenarioId);
            if (scenario != null)
                return scenario;
            string map = string.IsNullOrEmpty(mapId) ? "DB" : mapId;
            Texture2D plate = SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.ResolvePlateKey(map));
            if (plate != null)
                return plate;
            dimmed = true;
            Texture2D graded = SkirmishLibraryPreviewAssets.Load("skirmish.blur." + map);
            if (graded != null)
                return graded;
            return SkirmishLibraryPreviewAssets.Load("skirmish.zone." + map);
        }

        /// <summary>
        /// Playable and not-owned cards stay clickable. Not-owned opens the store.
        /// Planned and unavailable cards are disabled and play the locked tap.
        /// </summary>
        private void ApplyGridCardAccess(RectTransform widget, SkirmishBattleCardState card)
        {
            if (widget == null)
                return;
            Button button = widget.GetComponent<Button>();
            if (button == null)
                return;
            bool playable = card.Access == SkirmishCardAccess.Playable;
            bool storeRoute = card.Access == SkirmishCardAccess.NotOwned;
            button.interactable = playable || storeRoute || card.IsSelected;
            if (!button.interactable)
                AttachDisabledTap(button, UIAudioEventKind.CardLocked); // v41 audit: AttachAudio set the click sound, but a non-interactable button only plays its disabled-tap sound
        }

        private static bool IsGridLibraryCard(TMP_Text text)
        {
            Transform node = text != null ? text.transform.parent : null;
            while (node != null)
            {
                if (node.name.StartsWith("ZoneRow_", StringComparison.Ordinal))
                    return true;
                node = node.parent;
            }

            return false;
        }

        private bool IsGridLibrary() =>
            LibraryLevel == SkirmishLibraryLevel.Battlefield && LibraryLayout == SkirmishBattlefieldLayout.Grid;

        /// <summary>On the Grid the search shares the top row with the Map/Grid toggle, so the zone tabs own the band below it.</summary>
        private float GridTopOffset() => IsGridLibrary() ? 60f : 112f;

        private bool PlaceGridSearch(RectTransform search, float searchWidth)
        {
            if (!IsGridLibrary() || search == null)
                return false;
            PlaceTopLeft(search, 12f, 8f, searchWidth, 44f);
            if (mapNameText != null)
                mapNameText.gameObject.SetActive(false);
            if (_libraryGrid != null)
                _libraryGrid.offsetMax = new Vector2(_libraryGrid.offsetMax.x, -GridTopOffset());
            return true;
        }
    }
}
