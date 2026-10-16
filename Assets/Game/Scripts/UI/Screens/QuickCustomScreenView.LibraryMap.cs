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
        private void PresentBattlefieldPlate(RawImage map, SkirmishBattlefieldState field, float plateWidth, float plateHeight)
        {
            ApplyMapPlate(map, field, plateWidth, plateHeight);
        }

        private void PresentObjectiveTabs(SkirmishBattlefieldState field, bool fa)
        {
            if (battleLibraryMapTabs == null)
                return;
            for (int i = 0; i < _objectiveTabButtons.Count; i++)
            {
                if (_objectiveTabButtons[i] != null)
                    DestroyUi(_objectiveTabButtons[i].gameObject);
            }
            _objectiveTabButtons.Clear();
            if (field.Tabs == null)
                return;
            for (int i = 0; i < field.Tabs.Length; i++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[i];
                var button = CreateChromeButton(battleLibraryMapTabs, SkirmishLibraryControlNames.ObjectiveTab(objective.ObjectiveId));
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 72f;
                layout.minHeight = 64f;
                layout.flexibleWidth = 1f;
                bool selected = string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, true);
                string captured = objective.ObjectiveId;
                button.onClick.AddListener(() => SelectLibraryObjective(captured));
                var label = CreateLabel(button.transform, "Label", 18f);
                Stretch(label.rectTransform, 6f, 6f, 4f, 4f);
                SetCopy(label, LibraryText(objective.TitleLocKey, fa) + "\n" + objective.WonCount + "/" + objective.PublishedCount);
                _objectiveTabButtons.Add(button);
            }
        }

        private void PresentPins(SkirmishBattlefieldState field)
        {
            ClearChildren(_libraryPins);
            if (field.MapCards == null)
                return;
            for (int i = 0; i < field.MapCards.Length; i++)
            {
                if (!IsEmptyCard(field.MapCards[i]))
                    CreateBattleCard(_libraryPins, field.MapCards[i], true);
            }
        }

        private TMP_FontAsset _libraryWidgetFont;

        /// <summary>Widget context for the standalone battle-library widgets, sized to the live safe frame.</summary>
        private SkirmishWidgetContext LibraryWidgetContext(SkirmishBattleCardState card)
        {
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            Canvas canvas = _libraryPins != null ? _libraryPins.GetComponentInParent<Canvas>() : null;
            RectTransform canvasRect = canvas != null && canvas.rootCanvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            Vector2 size = canvasRect != null && canvasRect.rect.width > 10f ? canvasRect.rect.size : new Vector2(1920f, 1080f);
            SkirmishWidgetContext context = SkirmishWidgetContext.ForFrame(size, new Rect(Vector2.zero, size), fa);
            // The library canvas works in a 1080-tall reference space; keep widget pixels in that space.
            context.Scale = 1f;
            context.Font = LibraryWidgetFont();
            context.Localize = key => GameLocalization.Get(key, string.Empty);
            context.ApplyText = (text, value, min, max) => SetCopy(text, value, min, max);
            context.ZoneTitle = mapId => MapDisplayTitle(mapId, context.RightToLeft);
            string scenarioId = card.ScenarioId;
            string mapId = card.MapId;
            context.LoadPreview = key => ResolveGridPreview(key, scenarioId, mapId, out _);
            return context;
        }

        private TMP_FontAsset LibraryWidgetFont()
        {
            if (_libraryWidgetFont != null)
                return _libraryWidgetFont;
            Transform host = _libraryPins != null ? _libraryPins : transform;
            TMP_Text probe = CreateLabel(host, "FontProbe", 16f);
            _libraryWidgetFont = probe.font;
            DestroyImmediate(probe.gameObject);
            return _libraryWidgetFont;
        }

        private RectTransform CreateBattleCard(Transform parent, SkirmishBattleCardState card, bool pin, float width = LibraryPinWidth, float height = LibraryPinHeight, GridCardSlots gridSlots = default)
        {
            if (pin)
                return CreateMapPinWidget(parent, card);
            var button = CreateChromeButton(parent, SkirmishLibraryControlNames.BattleCard(card.ScenarioId));
            var rect = button.GetComponent<RectTransform>();
            if (pin && SkirmishBattlefieldPinLayout.TryGet(card.PinId, out Vector2 centre))
            {
                rect.anchorMin = rect.anchorMax = new Vector2(centre.x, 1f - centre.y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = Vector2.zero;
            }

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float titleHeight = pin ? 22f : gridSlots.Title > 1f ? gridSlots.Title : LibraryGridTitleHeight;
            float subtitleHeight = pin ? LibraryPinSubtitleHeight : gridSlots.Subtitle > 1f ? gridSlots.Subtitle : LibraryGridSubtitleHeight;
            float gridPreview = gridSlots.Preview;
            if (gridPreview <= 1f)
                ResolveGridCard(740f, 4, out _, out gridPreview);
            float previewHeight = pin
                ? Mathf.Max(48f, height - (titleHeight + subtitleHeight + 22f + 14f))
                : gridPreview;
            bool playable = card.Access == SkirmishCardAccess.Playable;
            TintButton(button, card.IsSelected, playable, true);
            button.interactable = !string.IsNullOrEmpty(card.ScenarioId);
            if (card.IsSelected)
                AddSelectionGlow(rect);
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(button.transform, false);
            string previewKey = ScenarioPreviewKey(card.PreviewSpriteKey);
            if (string.IsNullOrEmpty(previewKey) && !string.IsNullOrEmpty(card.ScenarioId))
                previewKey = "skirmish.preview." + card.ScenarioId;
            if (pin)
                ShowPinPreview(preview, previewKey, card.MapId, 3f, 3f, width - 6f, previewHeight);
            else
            {
                Texture2D shown = ResolveGridPreview(card.PreviewSpriteKey, card.ScenarioId, card.MapId, out bool dimmed);
                PlaceCoverPreview(preview, width, previewHeight, shown, dimmed);
                MarkPendingPreview(button.transform, fa, dimmed, width, previewHeight);
            }
            preview.transform.SetAsFirstSibling();
            var scrim = new GameObject("FooterScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            scrim.transform.SetParent(button.transform, false);
            scrim.rectTransform.anchorMin = new Vector2(0f, 0f);
            scrim.rectTransform.anchorMax = new Vector2(1f, 0f);
            scrim.rectTransform.pivot = new Vector2(0.5f, 0f);
            scrim.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            scrim.rectTransform.sizeDelta = new Vector2(-6f, height - previewHeight - 4f);
            scrim.color = new Color(0.04f, 0.06f, 0.07f, 0.94f);
            scrim.raycastTarget = false;
            var badge = new GameObject("ScenarioBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            badge.transform.SetParent(button.transform, false);
            float badgeX = fa ? width - 62f : 4f;
            PlaceTopLeft(badge.rectTransform, badgeX, 4f, 58f, 22f);
            badge.color = new Color(0.05f, 0.07f, 0.08f, 0.88f);
            badge.raycastTarget = false;
            var idLabel = CreateLabel(badge.transform, "ScenarioId", 14f);
            Stretch(idLabel.rectTransform, 2f, 2f, 1f, 1f);
            idLabel.alignment = TextAlignmentOptions.Center;
            idLabel.textWrappingMode = TextWrappingModes.NoWrap;
            idLabel.overflowMode = TextOverflowModes.Overflow;
            idLabel.color = new Color(1f, 0.94f, 0.72f, 1f);
            idLabel.isRightToLeftText = false;
            SetCopy(idLabel, card.ScenarioId, 13f, 16f);
            SkirmishBattleLibraryArt.ObjectiveCopy(card.ObjectiveId, fa, out _, out string objectiveLabel);
            SkirmishBattleLibraryArt.ProfileCopy(card.ArmyProfile, fa, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(card.StartProfile, fa, out string startLabel);
            float textGap = pin ? 4f : LibraryGridTextGap;
            float textY = previewHeight + textGap;
            var title = CreateLabel(button.transform, "Title", 15f);
            PlaceTopLeft(title.rectTransform, 4f, textY, width - 8f, titleHeight);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            title.isRightToLeftText = fa;
            SetCopy(title, objectiveLabel, pin ? 14f : 15f, pin ? 18f : 18f);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.isRightToLeftText = fa && ContainsArabicScript(objectiveLabel);
            var subtitle = CreateLabel(button.transform, "Subtitle", 16f);
            PlaceTopLeft(subtitle.rectTransform, 4f, textY + titleHeight, width - 8f, subtitleHeight);
            string subtitleCopy = string.IsNullOrEmpty(startLabel) ? armyLabel : armyLabel + " · " + startLabel;
            ShowCardSubtitle(subtitle, subtitleCopy, fa, pin);
            if (card.IsRecommended)
            {
                var recommended = CreateLabel(button.transform, "Recommended", 12f);
                PlaceTopLeft(recommended.rectTransform, 4f, 2f, width - 8f, 16f);
                recommended.alignment = TextAlignmentOptions.MidlineLeft;
                recommended.textWrappingMode = TextWrappingModes.NoWrap;
                recommended.color = new Color(0.95f, 0.82f, 0.28f, 1f);
                SetCopy(recommended, fa ? "پیشنهاد" : "NEXT", 11f, 13f);
            }

            float medalSize = pin ? 18f : gridSlots.Medal > 1f ? gridSlots.Medal : LibraryGridMedalSize;
            float medalRun = 4f * (medalSize + 2f);
            float medalX = fa && pin ? width - 8f - medalRun : 4f;
            float medalGap = pin ? 2f : LibraryGridMedalGap;
            AddMedalIcons(button.transform, card.Medals, medalX, textY + titleHeight + subtitleHeight + medalGap, medalSize);
            if (card.Access == SkirmishCardAccess.NotOwned || card.Access == SkirmishCardAccess.Unavailable)
            {
                var lockLabel = CreateLabel(button.transform, "Lock", 13f);
                PlaceTopLeft(lockLabel.rectTransform, fa && pin ? 6f : width - 52f, textY, 46f, titleHeight);
                lockLabel.alignment = TextAlignmentOptions.Center;
                lockLabel.textWrappingMode = TextWrappingModes.NoWrap;
                SetCopy(lockLabel, fa ? "قفل" : "LOCK", 12f, 14f);
            }

            string captured = card.ScenarioId;
            button.onClick.AddListener(() => OnLibraryCard(captured));
            if (pin)
                AddSiblingShadow(rect);
            StyleBattleCard(button.transform, card.IsSelected, playable, pin);
            return rect;
        }

        private RectTransform CreateMapPinWidget(Transform parent, SkirmishBattleCardState card)
        {
            SkirmishWidgetContext context = LibraryWidgetContext(card);
            bool laid = SkirmishBattlefieldPinLayout.TryGet(card.PinId, out Vector2 centre);
            Rect slot = default;
            bool packed = laid && TryPackedPinSlot(parent as RectTransform, centre, out slot);
            if (packed)
            {
                // approved-2: two pins per row, three rows, sized from the plate rather than a fixed reference.
                float narrow = context.Aspect == SkirmishWidgetAspect.Narrow ? 0.82f : 1f;
                context.Scale = slot.width / (SkirmishBattleCardWidget.PinReferenceSize.x * narrow);
            }

            RectTransform rect = SkirmishBattleCardWidget.BuildMapPin(parent, card, context, OnLibraryCard);
            if (packed)
            {
                PlaceTopLeft(rect, slot.x, slot.y, slot.width, slot.height);
            }
            else if (laid)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(centre.x, 1f - centre.y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            AddSiblingShadow(rect);
            return rect;
        }

        private void AddMedalIcons(Transform parent, SkirmishMedalMask medals, float x, float y, float size = 24f)
        {
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            var backing = new GameObject("MedalBacking", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            backing.transform.SetParent(parent, false);
            PlaceTopLeft(backing.rectTransform, x - 1f, y - 1f, difficulties.Length * (size + 2f) + 2f, size + 2f);
            backing.color = new Color(0.03f, 0.05f, 0.06f, 0.82f);
            backing.raycastTarget = false;
            for (int i = 0; i < difficulties.Length; i++)
            {
                var image = new GameObject("Medal" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(parent, false);
                PlaceTopLeft(image.rectTransform, x + i * (size + 2f), y, size, size);
                bool earned = MedalEarned(medals, difficulties[i]);
                PaintDifficultyMedal(image, difficulties[i], earned);
            }
        }

        private void PresentModeToggle(bool fa, bool battlefield)
        {
            Transform toggle = transform.Find("SkirmishSetupComposition/OperationPreview/LibraryModeToggle");
            if (toggle == null)
                return;
            toggle.gameObject.SetActive(battlefield);
            Button mapChoice = LayoutChoice(toggle, SkirmishLibraryControlNames.LayoutMap, "MapChoice");
            Button gridChoice = LayoutChoice(toggle, SkirmishLibraryControlNames.LayoutGrid, "GridChoice");
            SetCopy(mapChoice != null ? mapChoice.transform.Find("Label")?.GetComponent<TMP_Text>() : null, fa ? "نقشه" : "MAP");
            SetCopy(gridChoice != null ? gridChoice.transform.Find("Label")?.GetComponent<TMP_Text>() : null, fa ? "جدول" : "GRID");
            TintButton(mapChoice, LibraryLayout == SkirmishBattlefieldLayout.Map, true);
            TintButton(gridChoice, LibraryLayout == SkirmishBattlefieldLayout.Grid, true);
            BindLayoutChoice(mapChoice, SkirmishBattlefieldLayout.Map);
            BindLayoutChoice(gridChoice, SkirmishBattlefieldLayout.Grid);
        }

        private Button LayoutChoice(Transform toggle, string controlName, string legacyName)
        {
            Transform found = toggle.Find(controlName);
            if (found == null)
                found = toggle.Find(legacyName);
            if (found == null)
                return null;
            Transform alias = found.Find(controlName);
            if (alias != null)
                DestroyUi(alias.gameObject);
            found.name = controlName;
            return found.GetComponent<Button>();
        }

        private void BindLayoutChoice(Button button, SkirmishBattlefieldLayout layout)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                EnsureLibraryViewModel();
                _libraryViewModel.SetLayout(layout);
                PresentScenarioChoices();
            });
        }

        private void SelectLibraryObjective(string objectiveId)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectObjective(objectiveId);
            PresentScenarioChoices();
        }

        private void EnsureModeToggle(Transform preview)
        {
            if (preview == null || preview.Find("LibraryModeToggle") != null)
                return;
            var toggle = FindOrCreate(preview, "LibraryModeToggle");
            var map = CreateChromeButton(toggle, SkirmishLibraryControlNames.LayoutMap);
            var grid = CreateChromeButton(toggle, SkirmishLibraryControlNames.LayoutGrid);
            PlaceTopLeft(map.GetComponent<RectTransform>(), 0f, 0f, 120f, 56f);
            PlaceTopLeft(grid.GetComponent<RectTransform>(), 128f, 0f, 120f, 56f);
            var mapLabel = CreateLabel(map.transform, "Label", 18f);
            var gridLabel = CreateLabel(grid.transform, "Label", 18f);
            Stretch(mapLabel.rectTransform);
            Stretch(gridLabel.rectTransform);
            BindLayoutChoice(map, SkirmishBattlefieldLayout.Map);
            BindLayoutChoice(grid, SkirmishBattlefieldLayout.Grid);
        }

        private void PresentObjectiveColumn(SkirmishBattlefieldState field, bool fa)
        {
            if (_objectiveColumn == null)
                return;
            ClearChildren(_objectiveColumn);
            const float listWidth = 196f;
            const float rowHeight = 92f;
            PlaceTopLeft(_objectiveColumn, 8f, 8f, listWidth, 4f * (rowHeight + 8f));
            if (field.Tabs == null)
                return;
            for (int i = 0; i < field.Tabs.Length; i++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[i];
                var button = CreateChromeButton(_objectiveColumn, SkirmishLibraryControlNames.ObjectiveTab(objective.ObjectiveId));
                PlaceTopLeft(button.GetComponent<RectTransform>(), 0f, i * (rowHeight + 8f), listWidth, rowHeight);
                bool selected = string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, true);
                // approved-2 labelled list: medallion tile on the leading edge, name and count stacked beside it.
                const float tile = 56f;
                float tileX = fa ? listWidth - 6f - tile : 6f;
                var medallion = new GameObject("Medallion", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                medallion.transform.SetParent(button.transform, false);
                PlaceTopLeft(medallion.rectTransform, tileX, (rowHeight - tile) * 0.5f, tile, tile);
                medallion.sprite = ZoneMedallionSprite();
                medallion.preserveAspect = true;
                medallion.raycastTarget = false;
                medallion.color = selected ? new Color(1f, 0.96f, 0.82f, 1f) : Color.white;
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button.transform, false);
                PlaceTopLeft(icon.rectTransform, tileX + 12f, (rowHeight - 32f) * 0.5f, 32f, 32f);
                icon.preserveAspect = true;
                ShowIcon(icon, SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId), Color.white);
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string shortLabel);
                var label = CreateLabel(button.transform, "Label", 22f);
                var count = CreateLabel(button.transform, "Count", 22f);
                float textX = fa ? 6f : 6f + tile + 6f;
                float textWidth = listWidth - tile - 18f;
                PlaceTopLeft(label.rectTransform, textX, 6f, textWidth, 52f);
                PlaceTopLeft(count.rectTransform, textX, 58f, textWidth, 28f);
                label.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                label.isRightToLeftText = fa;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                label.maxVisibleLines = 2;
                SetCopy(label, UpperLatin(shortLabel), LibraryFontFloor, 26f);
                label.color = selected ? new Color(0.98f, 0.84f, 0.42f, 1f) : new Color(0.96f, 0.93f, 0.84f, 1f);
                count.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.overflowMode = TextOverflowModes.Overflow;
                bool comingSoon = objective.PublishedCount <= 0;
                string ratio = objective.WonCount + "/" + objective.PublishedCount;
                string countCopy = comingSoon ? SkirmishBattleLibraryArt.ComingSoon(fa) : fa ? SkirmishWidgetContext.ToPersianDigits(ratio) : ratio;
                // FinishLibraryText forces ObjectiveTab_/Count to LTR (it expects digits), which reversed "به زودی".
                if (comingSoon)
                    count.gameObject.name = "ComingSoon";
                count.isRightToLeftText = comingSoon && fa;
                count.overflowMode = TextOverflowModes.Overflow;
                SetCopy(count, countCopy, comingSoon ? 14f : LibraryFontFloor, 24f);
                if (comingSoon)
                {
                    count.enableAutoSizing = true;
                    count.fontSizeMin = 12f;
                    count.fontSizeMax = 18f;
                    V3LocalizedTextBindingView countBinding = count.GetComponent<V3LocalizedTextBindingView>();
                    if (countBinding != null)
                        countBinding.ConfigureFontBounds(12f, 18f);
                }
                if (!comingSoon)
                    count.isRightToLeftText = false;
                label.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                count.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                string captured = objective.ObjectiveId;
                button.onClick.AddListener(() => SelectLibraryObjective(captured));
            }
        }

        private const float PinGutter = 14f;
        private const float PinRailReserve = 196f + 8f + 14f;

        /// <summary>
        /// Lays the six pins out as approved-2 does: two columns by three rows (S001/S002 on top, S005/S006 at the bottom),
        /// with a light zigzag. The ordinal comes from the shared pin layout (left/centre/right, top/bottom), so ordering stays
        /// with P2's layout. Pin height is bounded by three rows in the plate, which is approved-2's proportion (about a quarter).
        /// </summary>
        private static readonly float[] PinZigzagLeft = { 0f, -0.03f, -0.04f };
        private static readonly float[] PinZigzagRight = { 0f, 0.04f, -0.02f };

        private static bool TryPackedPinSlot(RectTransform parent, Vector2 centre, out Rect slot)
        {
            slot = default;
            if (parent == null)
                return false;
            float width = parent.rect.width;
            float height = parent.rect.height;
            if (width < 400f || height < 260f)
                return false;
            int legacyColumn = centre.x < 0.42f ? 0 : centre.x < 0.68f ? 1 : 2;
            int legacyRow = centre.y < 0.5f ? 0 : 1;
            int ordinal = legacyRow * 3 + legacyColumn;
            int column = ordinal % 2;
            int row = ordinal / 2;
            float left = PinRailReserve;
            float availWidth = width - left - 12f;
            float availHeight = height - 24f;
            float aspect = SkirmishBattleCardWidget.PinReferenceSize.y / SkirmishBattleCardWidget.PinReferenceSize.x;
            float pinHeight = (availHeight - PinGutter * 2f) / 3f;
            float pinWidth = pinHeight / aspect;
            float maxWidth = Mathf.Min(availWidth * 0.42f, 360f);
            if (pinWidth > maxWidth)
            {
                pinWidth = maxWidth;
                pinHeight = pinWidth * aspect;
            }

            float blockHeight = pinHeight * 3f + PinGutter * 2f;
            float y0 = 12f + (availHeight - blockHeight) * 0.5f;
            float centreX = column == 0
                ? 0.27f + PinZigzagLeft[row]
                : 0.73f + PinZigzagRight[row];
            float x = left + availWidth * centreX - pinWidth * 0.5f;
            x = Mathf.Clamp(x, left, left + availWidth - pinWidth);
            slot = new Rect(x, y0 + row * (pinHeight + PinGutter), pinWidth, pinHeight);
            return true;
        }
    }
}
