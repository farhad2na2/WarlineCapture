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
        private void PresentBriefingChrome(SkirmishTheaterState theater, SkirmishBattlefieldState field, SkirmishBriefingState briefing, bool fa, bool battlefield)
        {
            string pinMap = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
            bool hasPin = TryFindTheaterPin(theater, pinMap, out SkirmishTheaterPinState pin);
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            TMP_Text title = rules != null ? rules.GetComponentInChildren<TMP_Text>(true) : null;
            Transform titleTransform = FindDeep(rules, "Title");
            if (titleTransform != null)
                title = titleTransform.GetComponent<TMP_Text>();
            if (battlefield && !string.IsNullOrEmpty(briefing.ScenarioId))
            {
                // approved-2 heading: "S005 · BASE ASSAULT" in gold.
                string objectiveTitle = string.Empty;
                if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry headingEntry) && !string.IsNullOrEmpty(headingEntry.ObjectiveId))
                    SkirmishBattleLibraryArt.ObjectiveCopy(headingEntry.ObjectiveId, fa, out objectiveTitle, out _);
                string heading = string.IsNullOrEmpty(objectiveTitle)
                    ? LibraryText(briefing.TitleLocKey, fa)
                    : briefing.ScenarioId + " · " + UpperLatin(objectiveTitle);
                SetCopy(title, MixedText(heading, fa), 26f, 34f);
                if (title != null)
                    title.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            }
            else if (hasPin)
                SetCopy(title, MapDisplayTitle(pin.MapId, fa), 26f, 34f);
            KeepSingleLine(title, 26f, 34f);
            if (title != null)
            {
                title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                title.isRightToLeftText = fa;
                title.overflowMode = TextOverflowModes.Overflow;
            }

            PresentBattlefieldSummary(hasPin, pin, fa, battlefield);

            if (_recommendedPanel != null)
            {
                TMP_Text heading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
                TMP_Text body = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
                SetCopy(heading, fa ? "پیشنهاد بعدی" : "RECOMMENDED NEXT", 20f, 26f);
                KeepSingleLine(heading, 20f, 26f);
                string recommendation = FocusedRecommendation(theater, field);
                _recommendedScenarioId = recommendation ?? string.Empty;
                string recommendationCopy = string.IsNullOrEmpty(recommendation)
                    ? (fa ? "هنوز پیشنهادی نیست." : "NO RECOMMENDATION YET.")
                    : UpperLatin(RecommendationLabel(recommendation, fa));
                SetCopy(body, recommendationCopy, 18f, 24f);
                if (body != null)
                {
                    body.textWrappingMode = TextWrappingModes.Normal;
                    body.overflowMode = TextOverflowModes.Ellipsis;
                    body.enableAutoSizing = true;
                    body.fontSizeMin = 14f;
                    body.fontSizeMax = 18f;
                    body.margin = new Vector4(4f, 0f, 4f, 0f);
                }

                PresentRecommendedPreview(recommendation);
            }

            PresentMedalRow(briefing, hasPin, pin, fa, battlefield);
            PresentDifficulty(briefing, fa);
            bool notOwned = briefing.Access == SkirmishCardAccess.NotOwned;
            if (_libraryStoreButton != null)
                _libraryStoreButton.gameObject.SetActive(notOwned);
            SetActive(FindDeep(rules, "Opponent"), _advancedOpen);
            SetActive(FindDeep(rules, "Objective"), _advancedOpen);
            SetActive(FindDeep(rules, "Roster"), _advancedOpen);
            SetActive(FindDeep(rules, "Economy"), _advancedOpen);
            SetActive(FindDeep(rules, "Intel"), _advancedOpen);
            SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/SeedMetric"), _advancedOpen);

            SkirmishBattleCardState selectedCard = default;
            bool hasCard = false;
            if (field.MapCards != null)
            {
                for (int i = 0; i < field.MapCards.Length; i++)
                {
                    if (!IsEmptyCard(field.MapCards[i]) && field.MapCards[i].IsSelected)
                    {
                        selectedCard = field.MapCards[i];
                        hasCard = true;
                    }
                }
            }
            if (!hasCard && field.GridCards != null)
            {
                for (int i = 0; i < field.GridCards.Length; i++)
                {
                    if (!IsEmptyCard(field.GridCards[i]) && field.GridCards[i].IsSelected)
                    {
                        selectedCard = field.GridCards[i];
                        hasCard = true;
                    }
                }
            }
            if (battlefield)
                PresentStatTiles(briefing, hasCard, selectedCard, fa);
            if (scenarioDescription != null && !battlefield && hasPin)
            {
                SkirmishBattleLibraryArt.Copy(pin.MapId, fa, out _, out string geography);
                SetCopy(scenarioDescription, geography, LibraryFontFloor, 26f);
                scenarioDescription.color = new Color(0.98f, 0.95f, 0.84f, 1f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
                scenarioDescription.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                scenarioDescription.isRightToLeftText = fa;
                scenarioDescription.margin = new Vector4(16f, 2f, 16f, 2f);
            }
            else if (scenarioDescription != null && !string.IsNullOrEmpty(briefing.ScenarioId))
            {
                string purpose = MixedText(LibraryPurpose(briefing, fa), fa);
                string reason = LibraryReason(briefing, fa);
                SetCopy(scenarioDescription, string.IsNullOrEmpty(reason) ? purpose : purpose + "\n" + reason, 20f, 26f);
            }

            if (launchButton != null && !_savedMatchAvailable)
                launchButton.interactable = !battlefield || briefing.CanLaunch;
        }

        private void PresentDifficulty(SkirmishBriefingState briefing, bool fa)
        {
            if (_difficultyRow == null)
                return;
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            SkirmishDifficultyId selected = briefing.SelectedDifficulty;
            for (int i = 0; i < difficulties.Length; i++)
            {
                Transform slot = _difficultyRow.Find("DifficultyChoice_" + difficulties[i]);
                if (slot == null)
                    continue;
                var button = slot.GetComponent<Button>();
                bool offered = DifficultyOffered(briefing, difficulties[i]);
                TintButton(button, offered && difficulties[i] == selected, offered);
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                if (slot is RectTransform choiceRect)
                {
                    float rowWidth = _difficultyRow.rect.width > 200f ? _difficultyRow.rect.width : _difficultyRow.sizeDelta.x;
                    if (rowWidth < 200f || rowWidth > 760f)
                        rowWidth = 548f;
                    // approved-2/3: compact segmented control, one row of four, mirrored in FA.
                    float segment = rowWidth / 4f;
                    const float segmentHeight = 40f;
                    int visual = fa ? 3 - i : i;
                    PlaceTopLeft(choiceRect, visual * segment, 4f, segment - 2f, segmentHeight);
                    StyleDifficultySegment(button, offered && difficulties[i] == selected, offered);
                }
                TMP_Text choice = slot.GetComponentInChildren<TMP_Text>(true);
                if (choice != null)
                {
                    choice.fontSize = LibraryFontFloor;
                    choice.fontSizeMin = LibraryFontFloor;
                    choice.fontSizeMax = 28f;
                    choice.textWrappingMode = TextWrappingModes.NoWrap;
                    choice.overflowMode = TextOverflowModes.Overflow;
                    choice.enableAutoSizing = true;
                    choice.alignment = fa ? TextAlignmentOptions.Center : TextAlignmentOptions.Center;
                    choice.isRightToLeftText = fa;
                }
                SetCopy(choice, label, LibraryFontFloor, 28f);
                if (choice != null)
                {
                    choice.overflowMode = TextOverflowModes.Overflow;
                    choice.fontSizeMin = LibraryFontFloor;
                }
            }
        }

        /// <summary>
        /// approved-2/3 selected panel type scale. Runs from PlaceLibraryHeader, after the late text passes and
        /// V3LocalizedTextBindingView's FitArabicLineBox, so the sizes hold in FA too.
        /// </summary>
        private void ReassertBriefingPanel()
        {
            if (LibraryLevel != SkirmishLibraryLevel.Battlefield)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            Transform titleTransform = FindDeep(rules, "Title");
            TMP_Text title = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
            if (title != null && title.gameObject.activeInHierarchy)
            {
                title.fontStyle = FontStyles.Bold;
                title.color = new Color(0.96f, 0.64f, 0.2f, 1f); // approved-2 orange heading
                FitDetailLine(title, 22f, 34f);
            }

            Transform column = title != null ? title.transform.parent : null;
            TMP_Text subtitle = column != null ? column.Find("BriefingSubtitle")?.GetComponent<TMP_Text>() : null;
            if (subtitle != null && subtitle.gameObject.activeInHierarchy)
            {
                subtitle.fontStyle = FontStyles.Bold;
                subtitle.color = new Color(0.98f, 0.96f, 0.9f, 1f);
                FitDetailLine(subtitle, 18f, 28f);
            }

            TMP_FontAsset latin = title != null ? title.font : null;
            var labels = new List<TMP_Text>(4);
            if (_difficultyRow != null)
            {
                for (int i = 0; i < _difficultyRow.childCount; i++)
                {
                    Transform slot = _difficultyRow.GetChild(i);
                    if (!slot.name.StartsWith("DifficultyChoice_", StringComparison.Ordinal))
                        continue;
                    labels.Add(slot.GetComponentInChildren<TMP_Text>(true));
                }
            }

            // Segment labels: one shared size (smallest fit), held fixed so the Grid refit text pass cannot clip them.
            float segShared = fa ? 20f : 18f;
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i] == null)
                    continue;
                ReassertPanelLabel(labels[i], latin, fa, 11f, fa ? 20f : 18f);
                segShared = Mathf.Min(segShared, labels[i].fontSize);
            }

            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i] == null)
                    continue;
                labels[i].margin = new Vector4(2f, 0f, 2f, 0f);
                LibraryFixedTextSize hold = labels[i].GetComponent<LibraryFixedTextSize>() ?? labels[i].gameObject.AddComponent<LibraryFixedTextSize>();
                hold.Size = segShared;
                hold.Apply();
                labels[i].textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private static void ReassertPanelLabel(TMP_Text text, TMP_FontAsset latin, bool fa, float min, float max)
        {
            if (text == null)
                return;
            // EN: same display face as the heading (no Arial fallback look); FA keeps the locale font.
            if (!fa && latin != null && !ContainsArabicScript(text.text))
                text.font = latin;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            FitDetailLine(text, min, max);
        }

        private static void StyleDifficultySegment(Button button, bool selected, bool offered)
        {
            if (button == null)
                return;
            // approved-2 MAP/GRID toggle colours: gold fill + gold edge when selected, dark slate otherwise.
            Color top = selected ? new Color(0.58f, 0.44f, 0.14f, 1f) : new Color(0.07f, 0.1f, 0.11f, 0.96f);
            Color bottom = selected ? new Color(0.36f, 0.26f, 0.06f, 1f) : new Color(0.03f, 0.05f, 0.06f, 0.96f);
            if (!offered)
            {
                top.a *= 0.5f;
                bottom.a *= 0.5f;
            }
            TintCommand(button, top, bottom);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.color = selected ? new Color(1f, 0.96f, 0.86f, 1f) : new Color(0.82f, 0.82f, 0.78f, offered ? 1f : 0.5f);
        }

        private void OnLibraryDifficulty(SkirmishDifficultyId difficulty)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectDifficulty(difficulty);
            PresentBattleLibrary();
        }

        private void EnsureDifficultyButtons(RectTransform row)
        {
            if (row == null || row.childCount > 0)
                return;
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            for (int i = 0; i < difficulties.Length; i++)
            {
                SkirmishDifficultyId difficulty = difficulties[i];
                var button = CreateChromeButton(row, "DifficultyChoice_" + difficulty);
                PlaceTopLeft(button.GetComponent<RectTransform>(), 4f + i * 130f, 4f, 124f, 56f);
                var label = CreateBoundLabel(button.transform, "Label", "ui.skirmish.library_difficulty_" + (int)difficulty, difficulty.ToString());
                Stretch(label.rectTransform, 4f, 4f, 4f, 4f);
                button.onClick.AddListener(() => OnLibraryDifficulty(difficulty));
            }
        }

        private string LibraryReason(SkirmishBriefingState briefing, bool fa)
        {
            if (string.IsNullOrEmpty(briefing.LockedReasonLocKey) ||
                briefing.LockedReasonLocKey.EndsWith("locked.planned", StringComparison.Ordinal))
                return string.Empty;
            string reason = MixedText(LibraryText(briefing.LockedReasonLocKey, fa), fa);
            return IsSuppressedLibraryCopy(reason) ? string.Empty : reason;
        }

        private string LibraryPurpose(SkirmishBriefingState briefing, bool fa)
        {
            if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry entry))
            {
                string authored = fa && !string.IsNullOrEmpty(entry.DescriptionFarsi)
                    ? entry.DescriptionFarsi
                    : entry.DescriptionEnglish;
                if (!IsGeneratedLibraryCopy(authored))
                    return authored;
            }

            string described = LibraryText(briefing.DescriptionLocKey, fa);
            if (!IsGeneratedLibraryCopy(described))
                return described;

            string objectiveId = string.Empty;
            if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry known))
                objectiveId = known.ObjectiveId;
            SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out string purpose, out _);
            return purpose ?? string.Empty;
        }

        private void FitBriefingPicture()
        {
            if (_briefingPreview == null || !_briefingPreview.gameObject.activeInHierarchy)
                return;
            RawImage picture = _briefingPreview.transform.Find("Picture")?.GetComponent<RawImage>();
            Texture texture = picture != null ? picture.texture : null;
            if (picture == null || texture == null || texture.width < 1 || texture.height < 1)
                return;
            Rect slot = _briefingPreview.rectTransform.rect;
            float slotWidth = slot.width > 2f ? slot.width : _briefingPreview.rectTransform.sizeDelta.x;
            float slotHeight = slot.height > 2f ? slot.height : _briefingPreview.rectTransform.sizeDelta.y;
            if (slotWidth < 2f)
                slotWidth = 540f;
            if (slotHeight < 2f)
                slotHeight = 200f;
            // approved-2/3: the battle art is a full-bleed plate across the panel, cropped to cover.
            picture.color = Color.white;
            picture.raycastTarget = false;
            PlaceTopLeft(picture.rectTransform, 0f, 0f, slotWidth, slotHeight);
            SkirmishBattleCardWidget.FitCover(picture, slotWidth, slotHeight);
        }

        private string BriefingPreviewKey(SkirmishBriefingState briefing)
        {
            string scenarioId = briefing.ScenarioId;
            if (_libraryViewModel != null)
            {
                SkirmishBattlefieldState field = _libraryViewModel.Battlefield;
                string fromCard = PreviewKeyOn(field.MapCards, scenarioId);
                if (!string.IsNullOrEmpty(fromCard))
                    return fromCard;
                fromCard = PreviewKeyOn(field.GridCards, scenarioId);
                if (!string.IsNullOrEmpty(fromCard))
                    return fromCard;
                if (!string.IsNullOrEmpty(scenarioId))
                    return SkirmishLibraryPreviewAssets.ResolvePreviewKey(scenarioId, true, field.MapId);
            }

            return string.IsNullOrEmpty(scenarioId) ? null : "skirmish.preview." + scenarioId;
        }

        private static string PreviewKeyOn(SkirmishBattleCardState[] cards, string scenarioId)
        {
            if (cards == null || string.IsNullOrEmpty(scenarioId))
                return null;
            for (int i = 0; i < cards.Length; i++)
            {
                if (string.Equals(cards[i].ScenarioId, scenarioId, StringComparison.Ordinal))
                    return cards[i].PreviewSpriteKey;
            }

            return null;
        }

        private RectTransform AdoptBriefingWidget(Transform rules, string name)
        {
            Transform column = rules != null ? rules.Find("BriefingColumn") : null;
            RectTransform adopted = null;
            if (column != null)
            {
                for (int i = 0; i < column.childCount; i++)
                {
                    Transform child = column.GetChild(i);
                    if (child.name != name)
                        continue;
                    if (adopted == null)
                        adopted = child as RectTransform;
                    else
                        child.gameObject.SetActive(false);
                }
            }

            if (adopted == null && rules != null && rules.Find(name) is RectTransform direct)
                adopted = direct;
            if (adopted != null)
                return adopted;
            return FindOrCreate(column != null ? column : rules, name);
        }

        private void BindBattlefieldSeed(bool fa)
        {
            if (scenarioDescription == null)
                return;
            SkirmishBriefingState briefing = _libraryViewModel != null ? _libraryViewModel.Briefing : default;
            if (string.IsNullOrEmpty(briefing.ScenarioId))
                return;
            string purpose = MixedText(LibraryPurpose(briefing, fa), fa);
            string reason = LibraryReason(briefing, fa);
            string copy = string.IsNullOrEmpty(reason) ? purpose : purpose + "\n" + reason;
            if (string.IsNullOrWhiteSpace(copy))
                return;
            SetCopy(scenarioDescription, copy, 20f, 26f);
        }

        private void EnsureStatTile(RectTransform row, string name, float x, string captionText)
        {
            if (row.Find(name) != null)
                return;
            var tile = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            tile.SetParent(row, false);
            PlaceTopLeft(tile, x, 0f, 124f, 72f);
            TintPanel(tile.gameObject);
            var caption = CreateBoundLabel(tile, "Caption", "ui.skirmish.library_stat_" + name.ToLowerInvariant(), captionText);
            PlaceTopLeft(caption.rectTransform, 6f, 4f, 112f, 22f);
            caption.fontSize = 14f;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            var value = CreateBoundLabel(tile, "Value", "ui.skirmish.library_stat_" + name.ToLowerInvariant() + "_value", "—");
            PlaceTopLeft(value.rectTransform, 6f, 28f, 112f, 36f);
            value.fontSize = 18f;
            value.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void HideLegacyStat(RectTransform row, string name)
        {
            Transform tile = row != null ? row.Find(name) : null;
            if (tile != null)
                tile.gameObject.SetActive(false);
        }

        private void PresentStatTiles(SkirmishBriefingState briefing, bool hasCard, SkirmishBattleCardState card, bool fa)
        {
            if (_statRow == null)
                return;
            HideLegacyStat(_statRow, "Forces");
            HideLegacyStat(_statRow, "Credits");
            HideLegacyStat(_statRow, "SourcedFacts");
            HideLegacyStat(_statRow, "FactsFrame");
            EnsureStatTile(_statRow, "Time", 0f, fa ? "زمان" : "TIME");
            EnsureStatTile(_statRow, "Size", 0f, fa ? "اندازه" : "SIZE");
            EnsureStatTile(_statRow, "Objective", 0f, fa ? "هدف" : "OBJECTIVE");
            EnsureStatTile(_statRow, "Army", 0f, fa ? "ارتش" : "ARMY");
            EnsureStatTile(_statRow, "Start", 0f, fa ? "آغاز" : "START");

            PresentFlaggedTile("Time", briefing.ShowTime, fa ? "زمان" : "TIME", FormatClock(briefing.DeadlineSeconds), true);
            PresentFlaggedTile("Size", briefing.ShowSize, fa ? "اندازه" : "SIZE", LibraryText(briefing.SizeLocKey, fa), false);
            // approved-2/3: three tiles (Objective, Army, Start). Time/Size fold into the subtitle line when present.
            string timeCopy = briefing.ShowTime ? FormatClock(briefing.DeadlineSeconds) : string.Empty;
            string sizeCopy = briefing.ShowSize ? LibraryText(briefing.SizeLocKey, fa) : string.Empty;
            SetActive(_statRow.Find("Time"), false);
            SetActive(_statRow.Find("Size"), false);

            string army = hasCard ? card.ArmyProfile : string.Empty;
            string start = hasCard ? card.StartProfile : string.Empty;
            string objectiveId = hasCard ? card.ObjectiveId : string.Empty;
            if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry entry))
            {
                if (string.IsNullOrEmpty(army))
                    army = entry.ArmyProfile;
                if (string.IsNullOrEmpty(start))
                    start = entry.StartProfile;
                if (string.IsNullOrEmpty(objectiveId))
                    objectiveId = entry.ObjectiveId;
            }

            string objective = string.Empty;
            if (!string.IsNullOrEmpty(objectiveId))
                SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out objective, out _);
            SkirmishBattleLibraryArt.ProfileCopy(army, fa, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(start, fa, out string startLabel);
            BindStatTile(_statRow.Find("Objective"), fa ? "هدف" : "OBJECTIVE", objective, false);
            BindStatTile(_statRow.Find("Army"), fa ? "ارتش" : "ARMY", armyLabel, false);
            BindStatTile(_statRow.Find("Start"), fa ? "آغاز" : "START", startLabel, false);
            LayoutVisibleStatTiles();
            // approved-2: icon tiles at mockup weight (icon over value, no caption). Only real values are shown.
            DecorateIconTile(_statRow.Find("Time"), FooterGlyph.Clock, null, fa);
            DecorateIconTile(_statRow.Find("Size"), FooterGlyph.Squares, null, fa);
            DecorateIconTile(_statRow.Find("Objective"), FooterGlyph.None, string.IsNullOrEmpty(objectiveId) ? null : SkirmishBattleLibraryArt.ObjectiveIcon(objectiveId), fa);
            DecorateIconTile(_statRow.Find("Army"), FooterGlyph.Shield, null, fa);
            DecorateIconTile(_statRow.Find("Start"), FooterGlyph.Pin, null, fa);
            string extra = string.IsNullOrEmpty(timeCopy) ? sizeCopy : string.IsNullOrEmpty(sizeCopy) ? timeCopy : timeCopy + " · " + sizeCopy;
            PresentBriefingSubtitle(armyLabel, string.IsNullOrEmpty(extra) ? startLabel : (string.IsNullOrEmpty(startLabel) ? extra : startLabel + " · " + extra), fa);
        }

        private void DecorateIconTile(Transform tile, FooterGlyph glyph, string sprite, bool fa)
        {
            if (tile is not RectTransform rect || !tile.gameObject.activeSelf)
                return;
            float width = rect.sizeDelta.x > 20f ? rect.sizeDelta.x : Mathf.Max(60f, rect.rect.width);
            const float icon = 38f; // approved-2/3: icon carries the tile
            Transform caption = tile.Find("Caption");
            if (caption != null)
                caption.gameObject.SetActive(false);
            Transform oldRaw = tile.Find("TileGlyph");
            Transform oldImg = tile.Find("TileArt");
            if (!string.IsNullOrEmpty(sprite))
            {
                if (oldRaw != null)
                    oldRaw.gameObject.SetActive(false);
                Image art = oldImg != null ? oldImg.GetComponent<Image>() : null;
                if (art == null)
                {
                    art = new GameObject("TileArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    art.transform.SetParent(tile, false);
                }
                art.gameObject.SetActive(true);
                PlaceTopLeft(art.rectTransform, (width - icon) * 0.5f, 5f, icon, icon);
                art.preserveAspect = true;
                art.raycastTarget = false;
                ShowIcon(art, sprite, new Color(0.96f, 0.93f, 0.84f, 1f));
            }
            else if (glyph != FooterGlyph.None)
            {
                if (oldImg != null)
                    oldImg.gameObject.SetActive(false);
                RawImage raw = oldRaw != null ? oldRaw.GetComponent<RawImage>() : null;
                if (raw == null)
                {
                    raw = new GameObject("TileGlyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                    raw.transform.SetParent(tile, false);
                }
                raw.gameObject.SetActive(true);
                PlaceTopLeft(raw.rectTransform, (width - icon) * 0.5f, 5f, icon, icon);
                raw.texture = FooterGlyphTexture(glyph);
                raw.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                raw.raycastTarget = false;
            }

            TMP_Text value = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (value != null)
            {
                PlaceTopLeft(value.rectTransform, 4f, 45f, width - 8f, 36f);
                value.alignment = TextAlignmentOptions.Center;
                value.textWrappingMode = TextWrappingModes.Normal;
                value.overflowMode = TextOverflowModes.Overflow;
                value.enableAutoSizing = true;
                value.fontSizeMin = 15f;
                value.fontSizeMax = 22f;
                value.maxVisibleLines = 2;
                value.fontStyle = FontStyles.Bold;
                value.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                V3LocalizedTextBindingView binding = value.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(15f, 22f);
            }
        }

        /// <summary>approved-2 subtitle under the gold heading: army · start from the catalog (e.g. COMBINED ARMS · FIELD BASE).</summary>
        private void PresentBriefingSubtitle(string army, string start, bool fa)
        {
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            if (column == null)
                return;
            Transform existing = column.Find("BriefingSubtitle");
            TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : CreateLabel(column, "BriefingSubtitle", 22f);
            string copy = string.IsNullOrEmpty(army) ? start : string.IsNullOrEmpty(start) ? army : army + " · " + start;
            _briefingSubtitleShown = !string.IsNullOrWhiteSpace(copy);
            text.gameObject.SetActive(_briefingSubtitleShown);
            if (!_briefingSubtitleShown)
                return;
            SetCopy(text, UpperLatin(copy), 20f, 28f);
            text.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            text.isRightToLeftText = fa;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontStyle = FontStyles.Bold;
            text.margin = new Vector4(16f, 0f, 16f, 0f);
            text.color = new Color(0.96f, 0.93f, 0.84f, 1f);
        }

        private bool _briefingSubtitleShown;

        private void PresentFlaggedTile(string name, bool show, string caption, string value, bool latinValue)
        {
            Transform tile = _statRow != null ? _statRow.Find(name) : null;
            if (tile == null)
                return;
            tile.gameObject.SetActive(show);
            if (!show)
                return;
            SetTile(tile, caption, value ?? string.Empty);
            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (valueText != null && latinValue)
                valueText.isRightToLeftText = false;
        }

        private void BindStatTile(Transform tile, string caption, string value, bool latinValue)
        {
            if (tile == null)
                return;
            if (string.IsNullOrEmpty(value))
            {
                tile.gameObject.SetActive(false);
                return;
            }

            tile.gameObject.SetActive(true);
            SetTile(tile, caption, value);
            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (valueText != null && latinValue)
                valueText.isRightToLeftText = false;
        }

        private void LayoutVisibleStatTiles()
        {
            if (_statRow == null)
                return;
            string[] order = { "Time", "Size", "Objective", "Army", "Start" };
            int count = 0;
            for (int i = 0; i < order.Length; i++)
            {
                Transform tile = _statRow.Find(order[i]);
                if (tile != null && tile.gameObject.activeSelf)
                    count++;
            }

            if (count == 0)
                return;
            float width = _statRow.rect.width > 200f ? _statRow.rect.width : _statRow.sizeDelta.x;
            if (width < 200f)
                width = 518f;
            const float gap = 8f;
            float tileWidth = (width - gap * (count - 1)) / count;
            float x = 0f;
            for (int i = 0; i < order.Length; i++)
            {
                Transform tile = _statRow.Find(order[i]);
                if (tile == null || !tile.gameObject.activeSelf)
                    continue;
                PlaceTruthfulTile(tile, x, tileWidth);
                x += tileWidth + gap;
            }
        }

        private static string FormatClock(int seconds)
        {
            int minutes = seconds / 60;
            int remainder = seconds % 60;
            if (remainder < 0)
                remainder = -remainder;
            return minutes.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ":"
                + remainder.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void PlaceTruthfulTile(Transform tile, float x, float width)
        {
            if (tile == null)
                return;
            tile.gameObject.SetActive(true);
            if (tile is RectTransform rect)
                PlaceTopLeft(rect, x, 0f, width, 84f);
            float inner = Mathf.Max(20f, width - 8f);
            TMP_Text captionText = tile.Find("Caption")?.GetComponent<TMP_Text>();
            if (captionText != null)
            {
                PlaceTopLeft(captionText.rectTransform, 4f, 2f, inner, 26f);
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.margin = new Vector4(2f, 0f, 2f, 0f);
                captionText.fontSizeMin = 12f;
                captionText.fontSizeMax = 16f;
            }

            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            if (valueText != null)
            {
                PlaceTopLeft(valueText.rectTransform, 4f, 28f, inner, 48f);
                valueText.textWrappingMode = TextWrappingModes.Normal;
                valueText.overflowMode = TextOverflowModes.Overflow;
                valueText.margin = new Vector4(2f, 0f, 2f, 0f);
                valueText.enableAutoSizing = true;
                valueText.fontSizeMin = 13f;
                valueText.fontSizeMax = 20f;
            }

            RectMask2D mask = tile.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
        }

        private static int CountMedals(SkirmishMedalMask mask)
        {
            int count = 0;
            if ((mask & SkirmishMedalMask.Recruit) != 0) count++;
            if ((mask & SkirmishMedalMask.Regular) != 0) count++;
            if ((mask & SkirmishMedalMask.Veteran) != 0) count++;
            if ((mask & SkirmishMedalMask.Commander) != 0) count++;
            return count;
        }

        private void SetTile(Transform tile, string caption, string value)
        {
            if (tile == null)
                return;
            TMP_Text captionText = tile.Find("Caption")?.GetComponent<TMP_Text>();
            TMP_Text valueText = tile.Find("Value")?.GetComponent<TMP_Text>();
            SetCopy(captionText, caption, LibraryFontFloor, 24f);
            SetCopy(valueText, value, LibraryFontFloor, 28f);
            if (captionText != null)
            {
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.overflowMode = TextOverflowModes.Overflow;
            }

            if (valueText != null)
            {
                valueText.textWrappingMode = TextWrappingModes.NoWrap;
                valueText.overflowMode = TextOverflowModes.Overflow;
                float width = tile is RectTransform row && row.sizeDelta.x > 20f ? row.sizeDelta.x - 12f : 80f;
                PlaceTopLeft(valueText.rectTransform, 6f, 26f, width, 34f);
            }
        }

        private void PresentBriefingPreview(SkirmishBriefingState briefing, bool hasPin, SkirmishTheaterPinState pin, bool battlefield)
        {
            if (_briefingPreview == null)
                return;
            string key = battlefield ? ScenarioPreviewKey(BriefingPreviewKey(briefing)) : null;
            if (battlefield && string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(briefing.ScenarioId))
                key = "skirmish.preview." + briefing.ScenarioId;
            Texture2D texture = string.IsNullOrEmpty(key) ? null : SkirmishLibraryPreviewAssets.Load(key);
            if (texture == null && battlefield && !string.IsNullOrEmpty(key))
                texture = SkirmishLibraryPreviewAssets.Placeholder();
            bool show = battlefield && texture != null;
            _briefingPreview.texture = null;
            _briefingPreview.enabled = false;
            _briefingPreview.color = new Color(0.05f, 0.07f, 0.08f, 1f);
            _briefingPreview.gameObject.SetActive(show);
            LayoutElement element = _briefingPreview.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.ignoreLayout = !show;
                element.minHeight = show ? 180f : 0f;
                element.preferredHeight = show ? 200f : 0f;
                element.flexibleHeight = 0f;
            }

            RawImage picture = _briefingPreview.transform.Find("Picture")?.GetComponent<RawImage>();
            if (picture == null)
            {
                picture = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                picture.transform.SetParent(_briefingPreview.transform, false);
            }

            picture.texture = show ? texture : null;
            picture.enabled = show;
            picture.color = Color.white;
            picture.uvRect = new Rect(0f, 0f, 1f, 1f);
            picture.raycastTarget = false;
        }

        private void PresentTheaterDetail(bool fa)
        {
            if (_medalGrid != null)
            {
                ClearChildren(_medalGrid);
                _medalGrid.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Map and Grid put the briefing on the trailing side per approved-2/3 (right in LTR, left in RTL).
        /// The Theatre keeps the frame default, where the briefing column is approved-1's left column.
        /// </summary>
        private bool? LibraryBriefingOnLeft()
        {
            if (LibraryLevel != SkirmishLibraryLevel.Battlefield)
                return null;
            return UiShellRuntimeGateway.Localization.IsRightToLeft;
        }
    }
}

namespace Game.UI.Runtime
{
    /// <summary>
    /// Holds a fixed TMP size with autosize off every frame. Late library text passes (FinishLibraryText after the
    /// Grid refit) re-enable autosize on medal captions; this keeps the shared caption size the panel computed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LibraryFixedTextSize : MonoBehaviour
    {
        public float Size;

        private void LateUpdate()
        {
            Apply();
        }

        public void Apply()
        {
            var text = GetComponent<TMP_Text>();
            if (text == null || Size <= 0f)
                return;
            text.textWrappingMode = TextWrappingModes.NoWrap; // refit text pass can turn wrapping back on
            text.maxVisibleLines = 1;
            if (text.enableAutoSizing || !Mathf.Approximately(text.fontSize, Size))
            {
                text.enableAutoSizing = false;
                text.fontSize = text.fontSizeMin = text.fontSizeMax = Size;
                text.overflowMode = TextOverflowModes.Overflow;
                var binding = GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(Size, Size);
            }
        }
    }
}

