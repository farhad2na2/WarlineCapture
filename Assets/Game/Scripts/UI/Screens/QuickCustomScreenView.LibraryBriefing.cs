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
                SetCopy(title, LibraryText(briefing.TitleLocKey, fa), 26f, 34f);
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
                SetCopy(scenarioDescription, string.IsNullOrEmpty(reason) ? purpose : purpose + "\n" + reason, 15f, 22f);
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
                    float buttonWidth = (rowWidth - 8f) * 0.5f;
                    const float buttonHeight = 50f;
                    PlaceTopLeft(choiceRect, (i % 2) * (buttonWidth + 8f), (i / 2) * (buttonHeight + 6f), buttonWidth, buttonHeight);
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
            float aspect = texture.width / (float)texture.height;
            float fittedWidth = slotWidth;
            float fittedHeight = fittedWidth / aspect;
            if (fittedHeight > slotHeight)
            {
                fittedHeight = slotHeight;
                fittedWidth = fittedHeight * aspect;
            }

            picture.uvRect = new Rect(0f, 0f, 1f, 1f);
            picture.color = Color.white;
            picture.raycastTarget = false;
            PlaceTopLeft(
                picture.rectTransform,
                (slotWidth - fittedWidth) * 0.5f,
                (slotHeight - fittedHeight) * 0.5f,
                fittedWidth,
                fittedHeight);
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
            SetCopy(scenarioDescription, copy, 15f, 22f);
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
        }

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
    }
}
