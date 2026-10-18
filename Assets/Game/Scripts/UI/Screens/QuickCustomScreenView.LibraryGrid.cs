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
            float headerTop = SkirmishZoneTabWidget.BandBottom + 2f;
            float headerHeight = roomy ? LibraryGridRoomyHeaderHeight : 34f;
            float scrollTop = headerTop + headerHeight + 4f;
            float viewport = Mathf.Max(120f, gridHeight - scrollTop);
            // The text block under the picture used to consume the height the tabs gave up,
            // so a 155px card kept a 62px preview. Shorter title and subtitle leave the
            // picture at about half the card while the four rows still fit a wide frame.
            float titleHeight = roomy ? LibraryGridRoomyTitleHeight : 20f;
            float subtitleHeight = roomy ? LibraryGridRoomySubtitleHeight : 30f;
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
                    var header = CreateLabel(_libraryGrid, "Column_" + column, fa ? 20f : 16f);
                    PlaceTopLeft(header.rectTransform, labelX, headerTop, labelWidth, headerHeight);
                    header.alignment = TextAlignmentOptions.Center;
                    header.isRightToLeftText = fa;
                    header.color = new Color(0.96f, 0.93f, 0.82f, 1f);
                    string headerCopy = fa || roomy ? armyLabel + " · " + startLabel : armyLabel + "\n" + startLabel;
                    SetCopy(header, headerCopy, roomy ? 16f : fa ? 16f : 14f, roomy ? 24f : fa ? 20f : 17f);
                    ShowColumnHeader(header, fa);
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
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    if (index >= field.GridCards.Length)
                        break;
                    float x = LibraryLabelColumnWidth() + column * (cardWidth + LibraryGridCardGap);
                    SkirmishBattleCardState card = field.GridCards[index];
                    RectTransform widget = IsEmptyCard(card)
                        ? CreateEmptyGridCell(zone, objective.ObjectiveId, armies[column / 2], starts[column % 2], card.PreviewSpriteKey, card.MapId, slots, cardWidth)
                        : CreateBattleCard(zone, card, false, cardWidth, cardHeight, slots);
                    PlaceTopLeft(widget, x, 0f, cardWidth, cardHeight);
                    AddSiblingShadow(widget);
                    if (!IsEmptyCard(card))
                        ApplyGridCardAccess(widget, card);
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
            PlaceTopLeft(rowIcon.rectTransform, 2f, y + 4f, 24f, 24f);
            ShowIcon(rowIcon, SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId), Color.white);
            SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string rowName);
            var rowLabel = CreateLabel(parent, "Row_" + objective.ObjectiveId, 13f);
            PlaceTopLeft(rowLabel.rectTransform, 1f, y + 26f, LibraryLabelColumnWidth() - 6f, 28f);
            rowLabel.alignment = TextAlignmentOptions.Center;
            rowLabel.textWrappingMode = TextWrappingModes.Normal;
            rowLabel.overflowMode = TextOverflowModes.Ellipsis;
            SetCopy(rowLabel, rowName, 13f, 16f);
            var rowCount = CreateLabel(parent, "RowCount_" + objective.ObjectiveId, 13f);
            bool pending = objective.PublishedCount <= 0;
            if (pending)
            {
                PlaceTopLeft(rowCount.rectTransform, 1f, y + 50f, LibraryLabelColumnWidth() - 4f, 42f);
                rowCount.textWrappingMode = TextWrappingModes.Normal;
                rowCount.maxVisibleLines = 2;
            }
            else
            {
                PlaceTopLeft(rowCount.rectTransform, 2f, y + 56f, LibraryLabelColumnWidth() - 8f, 16f);
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
                float inner = Mathf.Max(48f, tabWidth - SkirmishZoneTabWidget.LabelInset * 2f);
                var name = CreateLabel(button.transform, "ZoneTabName", SkirmishZoneTabWidget.NameFontFloor);
                name.alignment = TextAlignmentOptions.Center;
                string title = MapDisplayTitle(mapId, fa);
                SetCopy(name, title, SkirmishZoneTabWidget.NameFontFloor, SkirmishZoneTabWidget.NameFontFloor);
                SkirmishZoneTabWidget.StyleLabel(name);
                bool pending = true;
                int won = 0;
                int published = 0;
                if (_libraryViewModel != null &&
                    TryFindTheaterPin(_libraryViewModel.Theater, mapId, out SkirmishTheaterPinState pin))
                {
                    won = pin.WonCount;
                    published = pin.PublishedCount;
                    pending = published <= 0;
                }

                var status = CreateLabel(button.transform, "ZoneTabStatus", SkirmishZoneTabWidget.StatusFont);
                status.alignment = TextAlignmentOptions.Center;
                string statusCopy = SkirmishZoneTabWidget.StatusCopy(fa, pending, won, published);
                SetCopy(status, statusCopy, SkirmishZoneTabWidget.StatusFont, SkirmishZoneTabWidget.StatusFont);
                status.isRightToLeftText = pending && fa;
                SkirmishZoneTabWidget.StyleLabel(status);
                SkirmishZoneTabWidget.FitStatus(status);
                float countWidth = SkirmishZoneTabWidget.CountStripWidth(status, inner);
                float nameWidth = Mathf.Max(8f, inner - countWidth);
                float nameX = fa ? SkirmishZoneTabWidget.LabelInset + countWidth : SkirmishZoneTabWidget.LabelInset;
                float countX = fa ? SkirmishZoneTabWidget.LabelInset : SkirmishZoneTabWidget.LabelInset + nameWidth;
                float statusY = (SkirmishZoneTabWidget.NameBand - SkirmishZoneTabWidget.StatusBand) * 0.5f;
                PlaceTopLeft(name.rectTransform, nameX, 0f, nameWidth, SkirmishZoneTabWidget.NameBand);
                PlaceTopLeft(status.rectTransform, countX, statusY, countWidth, SkirmishZoneTabWidget.StatusBand);
                KeepAuthoredSize(name.rectTransform);
                KeepAuthoredSize(status.rectTransform);
                SkirmishZoneTabWidget.FitName(name);
                string captured = mapId;
                button.onClick.AddListener(() => SelectGridBattlefield(captured));
            }
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

        private void SelectGridBattlefield(string mapId)
        {
            EnsureLibraryViewModel();
            bool sameBattlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield &&
                string.Equals(_libraryViewModel.Battlefield.MapId, mapId, StringComparison.OrdinalIgnoreCase);
            if (!sameBattlefield)
                _libraryViewModel.SelectMapCollection(mapId);
            if (_libraryViewModel.Battlefield.Layout != SkirmishBattlefieldLayout.Grid)
                _libraryViewModel.SetLayout(SkirmishBattlefieldLayout.Grid);
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
            StyleBattleCard(rect, false, false, false);
            return rect;
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
                AttachAudio(button, UIAudioEventKind.CardLocked);
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
    }
}
