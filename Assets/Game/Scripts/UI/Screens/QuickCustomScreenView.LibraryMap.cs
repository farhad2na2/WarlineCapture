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
                // Zones with no playable battles read "Coming soon" (localised) instead of 0/0, fitted inside the tab.
                bool tabPending = objective.PublishedCount <= 0;
                string tabRatio = objective.WonCount + "/" + objective.PublishedCount;
                string tabCount = tabPending ? SkirmishBattleLibraryArt.ComingSoon(fa) : fa ? SkirmishWidgetContext.ToPersianDigits(tabRatio) : tabRatio;
                SetCopy(label, LibraryText(objective.TitleLocKey, fa) + "\n" + tabCount, 12f, 18f);
                label.isRightToLeftText = fa; // Persian copy shapes RTL; digits are already Persian so nothing reverses
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 18f;
                _objectiveTabButtons.Add(button);
            }
        }

        private void PresentPins(SkirmishBattlefieldState field)
        {
            ClearChildren(_libraryPins);
            if (field.MapCards == null)
                return;
            _pinWideFrame = IsWideLibraryFrame(_libraryPins); // v38: 16:9 and wider enforce the 24px clear row gap in both layouts
            var routePins = new System.Collections.Generic.List<RectTransform>();
            for (int i = 0; i < field.MapCards.Length; i++)
            {
                if (IsEmptyCard(field.MapCards[i]))
                    continue;
                RectTransform pinRect = CreateBattleCard(_libraryPins, field.MapCards[i], true);
                LightenPlannedShade(pinRect, field.MapCards[i].Access == SkirmishCardAccess.Playable);
                if (pinRect != null && pinRect.anchorMin == pinRect.anchorMax)
                    routePins.Add(pinRect);
            }

            AddPinRoutes(routePins);
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
            // Card bodies (frame, title, type line, medal slots) come only from P2's LibraryCardArt path below;
            // P1 keeps only the approved-2 packed pin size and placement (the slot), set before P2 lays out the body.
            if (pin && SkirmishBattlefieldPinLayout.TryGet(card.PinId, out Vector2 sizeCentre) &&
                TryPackedPinSlot(parent as RectTransform, sizeCentre, out Rect sizeSlot))
            {
                width = sizeSlot.width;
                height = sizeSlot.height;
            }
            var button = CreateChromeButton(parent, SkirmishLibraryControlNames.BattleCard(card.ScenarioId));
            var rect = button.GetComponent<RectTransform>();
            if (pin && SkirmishBattlefieldPinLayout.TryGet(card.PinId, out Vector2 centre))
            {
                rect.anchorMin = rect.anchorMax = new Vector2(centre.x, 1f - centre.y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = Vector2.zero;
                if (TryPackedPinSlot(parent as RectTransform, centre, out Rect packedSlot))
                {
                    // approved-2: two pins per row, three rows; keep P2's card size, centred in the packed slot.
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(packedSlot.center.x, -packedSlot.center.y);
                }
            }

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float titleHeight = pin ? Mathf.Clamp(height * 0.11f, 22f, 30f) : gridSlots.Title > 1f ? gridSlots.Title : LibraryGridTitleHeight;
            float subtitleHeight = pin ? Mathf.Clamp(height * 0.15f, LibraryPinSubtitleHeight, 44f) : gridSlots.Subtitle > 1f ? gridSlots.Subtitle : LibraryGridSubtitleHeight;
            float gridPreview = gridSlots.Preview;
            if (gridPreview <= 1f)
                ResolveGridCard(740f, 4, out _, out gridPreview);
            float previewHeight = pin
                ? Mathf.Max(48f, height - (titleHeight + subtitleHeight + PinMedalSize(height) + 4f + 14f))
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
            if (pin && TryShowUnitArt(preview, card.ArmyProfile, 3f, 3f, width - 6f, previewHeight))
            {
                // approved-2: Design's painted unit art is the card's main image; the aerial preview is the fallback.
            }
            else if (pin)
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
            SetCopy(title, objectiveLabel, pin ? Mathf.Clamp(height * 0.075f, 14f, 20f) : 15f, pin ? Mathf.Clamp(height * 0.095f, 18f, 26f) : 18f); // v36: pin title scales with card height
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

            float medalSize = pin ? PinMedalSize(height) : gridSlots.Medal > 1f ? gridSlots.Medal : LibraryGridMedalSize;
            float medalRun = 4f * (medalSize + 2f);
            float medalX = fa && pin ? width - 8f - medalRun : 4f;
            float medalGap = pin ? 2f : LibraryGridMedalGap;
            AddMedalIcons(button.transform, card.Medals, medalX, textY + titleHeight + subtitleHeight + medalGap, medalSize);
            if (!pin)
                AddGridCornerMedal(button.transform, card.Medals, width, previewHeight, fa);
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
            Transform toggle = FindModeToggle();
            if (toggle == null)
                return;
            toggle.gameObject.SetActive(battlefield);
            LayoutModeToggleButtons(toggle);
            Button mapChoice = LayoutChoice(toggle, SkirmishLibraryControlNames.LayoutMap, "MapChoice");
            Button gridChoice = LayoutChoice(toggle, SkirmishLibraryControlNames.LayoutGrid, "GridChoice");
            SetCopy(mapChoice != null ? mapChoice.transform.Find("Label")?.GetComponent<TMP_Text>() : null, fa ? "نقشه" : "MAP");
            SetCopy(gridChoice != null ? gridChoice.transform.Find("Label")?.GetComponent<TMP_Text>() : null, fa ? "جدول" : "GRID");
            TintButton(mapChoice, LibraryLayout == SkirmishBattlefieldLayout.Map, true);
            TintButton(gridChoice, LibraryLayout == SkirmishBattlefieldLayout.Grid, true);
            BindLayoutChoice(mapChoice, SkirmishBattlefieldLayout.Map);
            BindLayoutChoice(gridChoice, SkirmishBattlefieldLayout.Grid);
        }

        private Transform FindModeToggle()
        {
            Transform inColumn = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn/LibraryModeToggle");
            return inColumn != null ? inColumn : transform.Find("SkirmishSetupComposition/OperationPreview/LibraryModeToggle");
        }

        /// <summary>approved-2: the MAP/GRID toggle moves from the battlefield plate to the top row of the right panel.</summary>
        private void AdoptModeToggle(RectTransform column)
        {
            Transform toggle = FindModeToggle();
            if (toggle is not RectTransform rect || column == null)
                return;
            if (rect.parent != column)
                rect.SetParent(column, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y); // v34: stretch exactly to the column (plate sizeDelta made GRID overflow)
            LayoutModeToggleButtons(rect);
        }

        private static void LayoutModeToggleButtons(Transform toggle)
        {
            if (toggle is not RectTransform rect)
                return;
            float width = rect.rect.width > 120f ? rect.rect.width : 360f;
            if (rect.parent is RectTransform owner && owner.name == "BriefingColumn" && owner.rect.width > 120f)
                width = Mathf.Min(width, owner.rect.width);
            float half = (width - 8f) * 0.5f;
            var map = toggle.Find(SkirmishLibraryControlNames.LayoutMap) as RectTransform;
            var grid = toggle.Find(SkirmishLibraryControlNames.LayoutGrid) as RectTransform;
            PlaceTopLeft(map, 0f, 2f, half, 58f);
            PlaceTopLeft(grid, half + 8f, 2f, half, 58f);
            foreach (var choice in new[] { map, grid })
            {
                var label = choice != null ? choice.Find("Label")?.GetComponent<TMP_Text>() : null;
                if (label == null)
                    continue;
                label.enableAutoSizing = false;
                label.fontSize = 26f; // v34: approved-2 toggle text size
                label.fontStyle |= FontStyles.Bold;
            }
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
            if (preview == null || FindModeToggle() != null)
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
            const float listWidth = ObjectiveListWidth;
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
                StyleZoneTab(button, selected); // v33 approved-2/3: gold-framed dark-gold selected, dark idle
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
                SetCopy(label, UpperLatin(shortLabel), LibraryFontFloor, 28f);
                label.enableAutoSizing = false; // larger rail labels (Game PM); fixed so the Arabic line-box fit can't shrink them
                label.fontSize = 24f;
                label.fontStyle = FontStyles.Bold;
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

        private static void StyleZoneTab(Button button, bool selected)
        {
            Image face = button != null ? button.GetComponent<Image>() : null;
            if (face == null)
                return;
            face.color = selected ? new Color(0.34f, 0.25f, 0.07f, 0.96f) : new Color(0.05f, 0.07f, 0.08f, 0.92f);
            Outline edge = button.GetComponent<Outline>();
            if (edge == null)
                edge = button.gameObject.AddComponent<Outline>();
            edge.effectColor = selected ? new Color(0.95f, 0.78f, 0.32f, 1f) : new Color(0.55f, 0.5f, 0.38f, 0.45f);
            edge.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
        }

        private const float LibraryPinMedalSize = 36f; // v37: approved-2 medal circles (~36px at 16:9+; was 30)

        /// <summary>Medal circle size for a pin of this height: 30px, shrinking on short cards (4:3) so the art keeps room.</summary>
        private static float PinMedalSize(float pinHeight) => Mathf.Clamp(pinHeight * 0.185f, 20f, LibraryPinMedalSize);
        private const float PinGutter = 14f;
        private const float PinWideRowGap = 24f; // v37: min clear gap between staggered rows at 16:9 and wider
        /// <summary>v38: the selection glow reaches 4px past the card edge and the frame outline 1px, so the slot pitch adds
        /// that on both cards to keep a clear 24px between the visible frames.</summary>
        private const float PinFrameOverhang = 5f;
        private static bool _pinWideFrame;
        private static float PinRowPitchGap(float gutter) => _pinWideFrame ? Mathf.Max(gutter, PinWideRowGap + 2f * PinFrameOverhang) : gutter;
        private const float ObjectiveListWidth = 224f;
        private const float PinRailReserve = ObjectiveListWidth + 8f + 14f;

        /// <summary>The slot shade behind a pin: Planned (not playable) pins sit on a lighter shade so they read as pending, not sunk.</summary>
        private static void LightenPlannedShade(RectTransform widget, bool playable)
        {
            if (widget == null || widget.parent == null)
                return;
            int index = widget.GetSiblingIndex();
            for (int i = index - 1; i >= 0 && i >= index - 2; i--)
            {
                Transform sibling = widget.parent.GetChild(i);
                if (sibling.name != "DepthShadow")
                    continue;
                Image shade = sibling.GetComponent<Image>();
                if (shade != null)
                    shade.color = playable ? new Color(0.1f, 0.04f, 0.01f, 0.6f) : new Color(0.1f, 0.06f, 0.03f, 0.3f);
                return;
            }
        }

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
            int ordinal = legacyRow * 3 + legacyColumn; // battle order S001..S006
            return TryStaggeredPinSlot(width, height, ordinal, out slot);
        }

        private const float PinMaxWidth = 440f;
        private const float PinStaggerFraction = 0.32f;

        /// <summary>
        /// v36 approved-2 staggered layout sized from the live map area (the objective rail is reserved on the leading side;
        /// FA inherits the plate's mirroring). Two candidate layouts are measured and the one giving the larger card wins:
        /// 2 columns x 3 rows with alternate rows shifted sideways (tall areas, 4:3), or 3 columns x 2 rows with alternate
        /// columns dropped (wide areas, 16:9 / 20:9). The stagger is added to the block size, so cards can never overlap
        /// and always stay inside the area.
        /// </summary>
        private static bool TryStaggeredPinSlot(float width, float height, int ordinal, out Rect slot)
        {
            slot = default;
            float left = PinRailReserve;
            const float top = 12f;
            float availWidth = width - left - 12f;
            float availHeight = height - top * 2f;
            // v39: 4:3-class map areas (FA mirroring put S002/S004/S006 ~15px from the frame) keep a 16px extra safe
            // margin on both sides of the centred block; 16:9 and 20:9 areas are wider than 1.45 and stay unchanged.
            if (height > 1f && width / height < 1.45f)
                availWidth -= 32f;
            if (availWidth < 200f || availHeight < 200f)
                return false;
            float aspect = SkirmishBattleCardWidget.PinReferenceSize.y / SkirmishBattleCardWidget.PinReferenceSize.x;
            float g = PinGutter;
            float k = PinStaggerFraction;

            // v38: the 2x3 branch (A) is the one 16:9 picks; it used the 14px gutter between rows, so S003/S004 touched the
            // S001/S002 frames. Both branches now take the row gap from PinRowPitchGap (>= 24px clear at 16:9 and wider).
            float rg = PinRowPitchGap(g);
            // A: 2 cols x 3 rows, odd rows shifted right by k*w.  width = (2+k)w + g, height = 3h + 2rg.
            float wA = Mathf.Min((availWidth - g) / (2f + k), (availHeight - 2f * rg) / 3f / aspect, PinMaxWidth);
            // B: 3 cols x 2 rows, odd columns dropped by k*h.  width = 3w + 2g, height = (2+k)h + rg (cards shrink to fit).
            float wB = Mathf.Min((availWidth - 2f * g) / 3f, (availHeight - rg) / (2f + k) / aspect, PinMaxWidth);
            bool wide = wB > wA;
            float w = Mathf.Max(80f, wide ? wB : wA);
            float h = w * aspect;
            int column;
            int row;
            float x;
            float y;
            if (wide)
            {
                column = ordinal % 3;
                row = ordinal / 3;
                float blockW = 3f * w + 2f * g;
                float blockH = (2f + k) * h + rg;
                float x0 = left + (availWidth - blockW) * 0.5f;
                float y0 = top + (availHeight - blockH) * 0.5f;
                x = x0 + column * (w + g);
                y = y0 + row * (h + rg) + (column % 2 == 1 ? k * h : 0f);
            }
            else
            {
                column = ordinal % 2;
                row = ordinal / 2;
                // v38: spare width widens the column gutter (up to 56px) so the route between the two columns shows.
                float ga = Mathf.Clamp(availWidth - (2f + k) * w, g, 56f);
                float blockW = (2f + k) * w + ga;
                float blockH = 3f * h + 2f * rg;
                float x0 = left + (availWidth - blockW) * 0.5f;
                float y0 = top + (availHeight - blockH) * 0.5f;
                x = x0 + column * (w + ga) + (row % 2 == 1 ? k * w : 0f);
                y = y0 + row * (h + rg);
            }

            slot = new Rect(x, y, w, h);
            return true;
        }

        /// <summary>
        /// approved-2 route lines joining the pins in battle order: a 4px bright gold core over a 7px dark outline.
        /// v38 fix (v37 capture showed none): the lines were the first siblings, under every DepthShadow, and ran centre to
        /// centre, so the only visible part (the 14px gutter) was covered by the neighbouring card's 18px shadow overhang.
        /// Now every DepthShadow goes to the bottom, the routes sit above all shadows and below all cards, and each segment
        /// is clipped to the open sand between the two card frames.
        /// </summary>
        private void AddPinRoutes(System.Collections.Generic.List<RectTransform> pins)
        {
            if (_libraryPins == null || pins == null || pins.Count < 2)
                return;
            var shadowList = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < _libraryPins.childCount; i++)
            {
                Transform child = _libraryPins.GetChild(i);
                if (child.name == "DepthShadow")
                    shadowList.Add(child);
            }

            for (int i = 0; i < shadowList.Count; i++)
                shadowList[i].SetSiblingIndex(i);
            int shadows = shadowList.Count;
            int insertAt = shadows;
            insertAt = AddPinRouteLayer(pins, "PinRouteOutline", 7f, new Color(0.12f, 0.07f, 0.02f, 0.9f), insertAt);
            AddPinRouteLayer(pins, "PinRoute", 4f, new Color(1f, 0.86f, 0.38f, 1f), insertAt);
        }

        private int AddPinRouteLayer(System.Collections.Generic.List<RectTransform> pins, string name, float thickness, Color colour, int insertAt)
        {
            for (int i = 1; i < pins.Count; i++)
            {
                RectTransform a = pins[i - 1];
                RectTransform b = pins[i];
                if (a == null || b == null)
                    continue;
                Vector2 pa = a.anchoredPosition;
                Vector2 pb = b.anchoredPosition;
                Vector2 d = pb - pa;
                if (d.magnitude < 1f)
                    continue;
                // Clip the centre line to the gap between the two frames (2px into each card so the ends tuck under).
                float ta = ExitFraction(d, a.sizeDelta * 0.5f) - 2f / d.magnitude;
                float tb = 1f - ExitFraction(-d, b.sizeDelta * 0.5f) + 2f / d.magnitude;
                if (tb - ta < 0.5f / d.magnitude)
                    continue; // the cards touch along this line: nothing visible to draw
                Vector2 start = pa + d * ta;
                Vector2 end = pa + d * tb;
                Vector2 seg = end - start;
                var line = new GameObject(name + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                line.transform.SetParent(_libraryPins, false);
                line.raycastTarget = false;
                line.color = colour;
                RectTransform r = line.rectTransform;
                r.anchorMin = a.anchorMin;
                r.anchorMax = a.anchorMax;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(seg.magnitude, thickness);
                r.anchoredPosition = (start + end) * 0.5f;
                r.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(seg.y, seg.x) * Mathf.Rad2Deg);
                line.transform.SetSiblingIndex(Mathf.Min(insertAt, _libraryPins.childCount - 1));
                insertAt++;
            }

            return insertAt;
        }

        /// <summary>Fraction of d at which a ray from a rect centre leaves a rect of the given half size.</summary>
        private static float ExitFraction(Vector2 d, Vector2 half)
        {
            float tx = Mathf.Abs(d.x) > 0.001f ? half.x / Mathf.Abs(d.x) : float.MaxValue;
            float ty = Mathf.Abs(d.y) > 0.001f ? half.y / Mathf.Abs(d.y) : float.MaxValue;
            return Mathf.Min(tx, ty);
        }
    }
}
