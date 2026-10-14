using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private const float LibraryPinWidth = 176f;
        private const float LibraryPinHeight = 132f;
        private const float LibraryGridCardWidth = 136f;
        private const float LibraryGridCardHeight = 104f;
        private const float LibraryGridOriginX = 116f;
        private const float LibraryGridStride = 142f;

        private SkirmishLibraryLevel _libraryLevel = SkirmishLibraryLevel.Theater;
        private SkirmishLibraryPlateMode _plateMode = SkirmishLibraryPlateMode.Map;
        private string _libraryMap = "DB";
        private string _libraryObjective = "BA";
        private SkirmishDifficultyId _medalDifficulty = SkirmishDifficultyId.Regular;
        private string _armyFilter = string.Empty;
        private string _startFilter = string.Empty;
        private bool _missingMedalOnly;
        private bool _filtersOpen;
        private bool _advancedOpen;
        private bool _libraryMemoryLoaded;
        private bool _librarySearchApplied;
        private bool _libraryChromeReady;
        private ISkirmishBattleLibraryProjection _libraryProjection;

        private RectTransform _libraryRegions;
        private RectTransform _libraryPins;
        private RectTransform _libraryGrid;
        private RectTransform _libraryFilters;
        private RectTransform _medalRow;
        private RectTransform _difficultyRow;
        private RectTransform _recommendedPanel;
        private Button _libraryStoreButton;
        private readonly List<Button> _objectiveTabButtons = new List<Button>();
        private readonly List<Button> _regionButtons = new List<Button>();

        public SkirmishDifficultyId SelectedLibraryDifficulty => _medalDifficulty;

        public void EnsureBattleLibraryChrome()
        {
            if (_libraryChromeReady && _libraryPins != null)
                return;

            Transform composition = transform.Find("SkirmishSetupComposition");
            Transform preview = composition != null ? composition.Find("OperationPreview") : null;
            Transform rules = composition != null ? composition.Find("BaseAssaultRules") : null;
            if (preview == null || rules == null)
                return;

            if (composition.GetComponent<SkirmishLibrarySafeFrame>() == null)
                composition.gameObject.AddComponent<SkirmishLibrarySafeFrame>();

            SetActive(preview.Find("BattleLibrary"), false);
            Transform plate = preview.Find("MapPreviewClip");
            if (plate != null)
            {
                _libraryRegions = FindOrCreate(plate, "LibraryRegions");
                _libraryPins = FindOrCreate(plate, "LibraryPins");
                Stretch(_libraryRegions);
                Stretch(_libraryPins);
            }

            _libraryGrid = FindOrCreate(preview, "LibraryGrid");
            _libraryFilters = FindOrCreate(composition, "LibraryFilters");
            _medalRow = FindOrCreate(rules, "MedalRow");
            _difficultyRow = FindOrCreate(rules, "MedalDifficultySelector");
            _recommendedPanel = FindOrCreate(rules, "RecommendedNext");
            EnsureMedalSlots(_medalRow);
            EnsureDifficultyButtons(_difficultyRow);
            EnsureRecommended(_recommendedPanel);
            EnsureStoreButton(rules);
            EnsureModeToggle(preview);
            EnsureFilterPanel(_libraryFilters);
            PlaceLibraryChrome();
            _libraryChromeReady = true;
        }

        public void PresentLibraryCapture(string state)
        {
            if (!baseAssaultPreset)
                return;
            switch ((state ?? string.Empty).ToLowerInvariant())
            {
                case "map":
                    _libraryLevel = SkirmishLibraryLevel.Battlefield;
                    _plateMode = SkirmishLibraryPlateMode.Map;
                    if (string.IsNullOrEmpty(_libraryMap))
                        _libraryMap = "DB";
                    break;
                case "grid":
                    _libraryLevel = SkirmishLibraryLevel.Battlefield;
                    _plateMode = SkirmishLibraryPlateMode.Grid;
                    if (string.IsNullOrEmpty(_libraryMap))
                        _libraryMap = "DB";
                    break;
                default:
                    _libraryLevel = SkirmishLibraryLevel.Theater;
                    _plateMode = SkirmishLibraryPlateMode.Map;
                    break;
            }

            _filtersOpen = false;
            EnsureLibraryCards();
            PresentScenarioChoices();
        }

        private void LoadLibraryMemory()
        {
            if (_libraryMemoryLoaded)
                return;
            _libraryMemoryLoaded = true;
            int level = PlayerPrefs.GetInt(SkirmishLibrarySessionKeys.Level, (int)SkirmishLibraryLevel.Theater);
            int plate = PlayerPrefs.GetInt(SkirmishLibrarySessionKeys.Plate, (int)SkirmishLibraryPlateMode.Map);
            _libraryLevel = level == (int)SkirmishLibraryLevel.Battlefield
                ? SkirmishLibraryLevel.Battlefield
                : SkirmishLibraryLevel.Theater;
            _plateMode = plate == (int)SkirmishLibraryPlateMode.Grid
                ? SkirmishLibraryPlateMode.Grid
                : SkirmishLibraryPlateMode.Map;
            _libraryMap = PlayerPrefs.GetString(SkirmishLibrarySessionKeys.Map, "DB");
            _libraryObjective = PlayerPrefs.GetString(SkirmishLibrarySessionKeys.Objective, "BA");
            if (!IsLibraryMap(_libraryMap))
                _libraryMap = "DB";
            int difficulty = PlayerPrefs.GetInt(SkirmishLibrarySessionKeys.Difficulty, (int)SkirmishDifficultyId.Regular);
            _medalDifficulty = difficulty >= (int)SkirmishDifficultyId.Recruit && difficulty <= (int)SkirmishDifficultyId.Commander
                ? (SkirmishDifficultyId)difficulty
                : SkirmishDifficultyId.Regular;
        }

        private void SaveLibraryMemory()
        {
            if (!Application.isPlaying)
                return;
            PlayerPrefs.SetInt(SkirmishLibrarySessionKeys.Level, (int)_libraryLevel);
            PlayerPrefs.SetInt(SkirmishLibrarySessionKeys.Plate, (int)_plateMode);
            PlayerPrefs.SetString(SkirmishLibrarySessionKeys.Map, _libraryMap ?? "DB");
            PlayerPrefs.SetString(SkirmishLibrarySessionKeys.Objective, _libraryObjective ?? "BA");
            PlayerPrefs.SetInt(SkirmishLibrarySessionKeys.Difficulty, (int)_medalDifficulty);
            PlayerPrefs.Save();
        }

        private void RememberLibrarySelection(SkirmishBattleCatalogEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.MapId) && IsLibraryMap(entry.MapId))
                _libraryMap = entry.MapId;
            if (!string.IsNullOrEmpty(entry.ObjectiveId))
                _libraryObjective = entry.ObjectiveId;
            _libraryLevel = SkirmishLibraryLevel.Battlefield;
            _mapFilter = _libraryMap;
            SaveLibraryMemory();
        }

        private void PresentBattleLibrary()
        {
            if (!baseAssaultPreset)
                return;
            EnsureBattleLibraryChrome();
            if (_libraryPins == null && _libraryRegions == null)
                return;

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            ApplyLargeTextScale();
            PlaceLibraryChrome();
            SkirmishLibrarySnapshot snapshot = ProjectLibrary(fa);
            if (TryJumpToSearchHit())
                return;

            bool battlefield = _libraryLevel == SkirmishLibraryLevel.Battlefield;
            SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary"), false);
            SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryCount"), false);
            SetActive(battleLibraryMapTabs, battlefield);
            HideMapCollectionTabs();
            SetActive(_libraryRegions, !battlefield);
            SetActive(_libraryPins, battlefield && _plateMode == SkirmishLibraryPlateMode.Map);
            SetActive(_libraryGrid, battlefield && _plateMode == SkirmishLibraryPlateMode.Grid);
            SetActive(_libraryFilters, _filtersOpen);
            SetActive(_recommendedPanel, !battlefield);
            SetActive(_difficultyRow, battlefield);
            SetActive(_medalRow, true);
            PresentPlateTexture(battlefield);
            PresentRegions(snapshot, fa);
            PresentObjectiveTabs(snapshot, fa);
            PresentModeToggle(fa, battlefield);
            if (battlefield && _plateMode == SkirmishLibraryPlateMode.Map)
                PresentPins(snapshot, fa);
            if (battlefield && _plateMode == SkirmishLibraryPlateMode.Grid)
                PresentGrid(snapshot, fa);
            PresentBriefingChrome(snapshot, fa, battlefield);
            PresentFilterLabels(fa);
            PresentLibraryFooter(fa, battlefield);
            PresentScreenHeading(snapshot, fa, battlefield);
            SaveLibraryMemory();
        }

        private bool TryJumpToSearchHit()
        {
            if (_librarySearchApplied || string.IsNullOrEmpty(_searchFilter))
                return false;
            string focus = FirstSearchHit();
            if (string.IsNullOrEmpty(focus))
            {
                _librarySearchApplied = true;
                return false;
            }
            if (string.Equals(focus, _selectedScenarioId, StringComparison.Ordinal))
            {
                _librarySearchApplied = true;
                return false;
            }
            if (!TryFindEntry(focus, out SkirmishBattleCatalogEntry entry))
                return false;
            _librarySearchApplied = true;
            _selectedScenarioId = entry.ScenarioId;
            if (entry.IsPlayable)
                _config.ScenarioIndex = entry.PlayableScenarioIndex;
            if (IsLibraryMap(entry.MapId))
            {
                _libraryMap = entry.MapId;
                _mapFilter = entry.MapId;
            }
            if (!string.IsNullOrEmpty(entry.ObjectiveId))
                _libraryObjective = entry.ObjectiveId;
            _libraryLevel = SkirmishLibraryLevel.Battlefield;
            PresentScenarioChoices();
            return true;
        }

        private string FirstSearchHit()
        {
            for (int i = 0; i < _catalogEntries.Count; i++)
            {
                if (EntryMatchesSearch(_catalogEntries[i], _searchFilter))
                    return _catalogEntries[i].ScenarioId;
            }

            return string.Empty;
        }

        private SkirmishLibrarySnapshot ProjectLibrary(bool fa)
        {
            if (_libraryProjection == null)
            {
                _libraryProjection = new SkirmishBattleLibraryCatalogAdapter(
                    _catalogEntries,
                    SkirmishBattleLibraryServices.Medals,
                    SkirmishBattleLibraryServices.Access);
            }

            var query = new SkirmishLibraryQuery
            {
                RightToLeft = fa,
                DevelopmentBuild = Debug.isDebugBuild,
                SelectedCatalogId = _selectedScenarioId,
                MapId = _libraryMap,
                ObjectiveId = _libraryLevel == SkirmishLibraryLevel.Battlefield && _plateMode == SkirmishLibraryPlateMode.Map
                    ? _libraryObjective
                    : string.Empty,
                Search = _searchFilter,
                ArmyFilter = _armyFilter,
                StartFilter = _startFilter,
                MissingMedalOnly = _missingMedalOnly,
                MedalDifficulty = _medalDifficulty
            };
            SkirmishLibrarySnapshot snapshot = _libraryProjection.Project(query);
            if (SkirmishBattleLibraryServices.Advisor != null &&
                SkirmishBattleLibraryServices.Advisor.TryRecommendNext(query, snapshot.Cards, out string recommended) &&
                !string.IsNullOrEmpty(recommended))
            {
                snapshot.RecommendedCatalogId = recommended;
                snapshot.RecommendedTitle = recommended;
                for (int i = 0; i < snapshot.Cards.Length; i++)
                {
                    if (!string.Equals(snapshot.Cards[i].CatalogId, recommended, StringComparison.Ordinal))
                        continue;
                    snapshot.RecommendedTitle = snapshot.Cards[i].Title;
                    break;
                }
            }

            return snapshot;
        }

        private void PresentPlateTexture(bool battlefield)
        {
            RawImage map = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip/MapPreview")
                ?.GetComponent<RawImage>();
            if (map == null)
                return;
            string key = battlefield
                ? SkirmishBattleLibraryArt.Plate(_libraryMap)
                : SkirmishBattleLibraryArt.TheaterPlate;
            Texture2D texture = Resources.Load<Texture2D>(key);
            if (texture != null)
                map.texture = texture;
            map.color = Color.white;
        }

        private void PresentRegions(SkirmishLibrarySnapshot snapshot, bool fa)
        {
            if (_libraryRegions == null)
                return;
            ClearChildren(_libraryRegions);
            _regionButtons.Clear();
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                Rect region = SkirmishBattleLibraryArt.TheaterRegions[i];
                SkirmishLibraryBattlefieldModel field = FindBattlefield(snapshot, mapId);
                bool selected = string.Equals(mapId, _libraryMap, StringComparison.OrdinalIgnoreCase);
                var button = CreateChromeButton(_libraryRegions, "Region_" + mapId);
                PlaceRegion(button.GetComponent<RectTransform>(), region);
                TintButton(button, selected, true);
                string captured = mapId;
                button.onClick.AddListener(() => SelectTheaterRegion(captured));
                string title = field != null ? field.Title : mapId;
                int won = field != null ? field.Won : 0;
                int published = field != null ? field.Published : 0;
                var label = CreateLabel(button.transform, "Label", 18f);
                Stretch(label.rectTransform, 10f, 10f, 8f, 8f);
                label.alignment = TextAlignmentOptions.Bottom;
                SetCopy(label, title + "\n" + won + "/" + published);
                _regionButtons.Add(button);
            }
        }

        private void PresentObjectiveTabs(SkirmishLibrarySnapshot snapshot, bool fa)
        {
            if (battleLibraryMapTabs == null)
                return;
            for (int i = 0; i < _objectiveTabButtons.Count; i++)
            {
                if (_objectiveTabButtons[i] != null)
                    DestroyUi(_objectiveTabButtons[i].gameObject);
            }
            _objectiveTabButtons.Clear();
            if (_libraryLevel != SkirmishLibraryLevel.Battlefield || snapshot.Objectives == null)
                return;
            for (int i = 0; i < snapshot.Objectives.Length; i++)
            {
                SkirmishLibraryObjectiveModel objective = snapshot.Objectives[i];
                var button = CreateChromeButton(battleLibraryMapTabs, "ObjectiveTab_" + objective.ObjectiveId);
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 72f;
                layout.minHeight = 64f;
                layout.flexibleWidth = 1f;
                bool selected = string.Equals(objective.ObjectiveId, _libraryObjective, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, false);
                string captured = objective.ObjectiveId;
                button.onClick.AddListener(() => SelectLibraryObjective(captured));
                var label = CreateLabel(button.transform, "Label", 18f);
                Stretch(label.rectTransform, 6f, 6f, 4f, 4f);
                SetCopy(label, objective.ShortLabel + "\n" + objective.Won + "/" + objective.Published);
                _objectiveTabButtons.Add(button);
            }
        }

        private void PresentPins(SkirmishLibrarySnapshot snapshot, bool fa)
        {
            ClearChildren(_libraryPins);
            if (snapshot.Cards == null)
                return;
            for (int i = 0; i < snapshot.Cards.Length; i++)
                CreateBattleCard(_libraryPins, snapshot.Cards[i], true);
        }

        private void PresentGrid(SkirmishLibrarySnapshot snapshot, bool fa)
        {
            ClearChildren(_libraryGrid);
            if (snapshot.Cards == null || snapshot.Objectives == null)
                return;
            string[] armies = { "G", "A", "C" };
            string[] starts = { "F", "E" };
            for (int armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                for (int startIndex = 0; startIndex < starts.Length; startIndex++)
                {
                    int column = armyIndex * 2 + startIndex;
                    SkirmishBattleLibraryArt.ProfileCopy(armies[armyIndex], fa, out string armyLabel);
                    SkirmishBattleLibraryArt.ProfileCopy(starts[startIndex], fa, out string startLabel);
                    var header = CreateLabel(_libraryGrid, "Column_" + column, 13f);
                    PlaceTopLeft(header.rectTransform, LibraryGridOriginX + column * LibraryGridStride, 0f, LibraryGridCardWidth, 36f);
                    SetCopy(header, armyLabel + "\n" + startLabel);
                }
            }

            for (int row = 0; row < snapshot.Objectives.Length; row++)
            {
                SkirmishLibraryObjectiveModel objective = snapshot.Objectives[row];
                float y = 40f + row * (LibraryGridCardHeight + 8f);
                var rowLabel = CreateLabel(_libraryGrid, "Row_" + objective.ObjectiveId, 16f);
                PlaceTopLeft(rowLabel.rectTransform, 4f, y, LibraryGridOriginX - 8f, LibraryGridCardHeight);
                SetCopy(rowLabel, objective.ShortLabel);
                var used = new int[6];
                for (int i = 0; i < snapshot.Cards.Length; i++)
                {
                    SkirmishLibraryCardModel card = snapshot.Cards[i];
                    if (!string.Equals(card.ObjectiveId, objective.ObjectiveId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    int column = GridColumn(card.ArmyProfile, card.StartProfile);
                    if (column < 0)
                        continue;
                    int stack = used[column]++;
                    var widget = CreateBattleCard(_libraryGrid, card, false, LibraryGridCardWidth, LibraryGridCardHeight);
                    PlaceTopLeft(
                        widget,
                        LibraryGridOriginX + column * LibraryGridStride + stack * 10f,
                        y,
                        LibraryGridCardWidth,
                        LibraryGridCardHeight);
                }
            }
        }

        private static int GridColumn(string army, string start)
        {
            int armyIndex = army == "A" ? 1 : army == "C" ? 2 : army == "G" ? 0 : -1;
            int startIndex = start == "E" ? 1 : start == "F" ? 0 : -1;
            if (armyIndex < 0 || startIndex < 0)
                return -1;
            return armyIndex * 2 + startIndex;
        }

        private RectTransform CreateBattleCard(Transform parent, SkirmishLibraryCardModel card, bool pin, float width = LibraryPinWidth, float height = LibraryPinHeight)
        {
            var button = CreateChromeButton(parent, "BattleCard_" + card.CatalogId);
            var rect = button.GetComponent<RectTransform>();
            if (pin)
            {
                SkirmishBattlefieldPinLayout.TryGet(card.MapId, card.ObjectiveId, card.ArmyProfile, card.StartProfile, out Vector2 centre);
                rect.anchorMin = rect.anchorMax = new Vector2(centre.x, 1f - centre.y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = Vector2.zero;
            }

            bool selected = string.Equals(card.CatalogId, _selectedScenarioId, StringComparison.Ordinal);
            TintButton(button, selected, card.Access == SkirmishLibraryCardAccess.OwnedPlayable);
            if (card.Access == SkirmishLibraryCardAccess.Planned || card.Access == SkirmishLibraryCardAccess.Unavailable)
                Dim(button);
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(button.transform, false);
            Stretch(preview.rectTransform, 4f, 4f, 4f, 36f);
            preview.texture = Resources.Load<Texture2D>(card.PreviewKey);
            preview.color = preview.texture != null ? Color.white : new Color(0.12f, 0.16f, 0.18f, 1f);
            preview.raycastTarget = false;
            var title = CreateLabel(button.transform, "Title", 16f);
            PlaceTopLeft(title.rectTransform, 8f, height - 34f, width - 16f, 28f);
            title.alignment = TextAlignmentOptions.BottomLeft;
            SetCopy(title, card.ArmyLabel + "  " + card.StartLabel);
            AddMedalIcons(button.transform, card, 8f, 6f);
            if (card.Access == SkirmishLibraryCardAccess.NotOwned)
            {
                var lockLabel = CreateLabel(button.transform, "Lock", 14f);
                PlaceTopLeft(lockLabel.rectTransform, 8f, 36f, width - 16f, 24f);
                SetCopy(lockLabel, UiShellRuntimeGateway.Localization.IsRightToLeft ? "قفل" : "LOCK");
            }

            string captured = card.CatalogId;
            button.onClick.AddListener(() => OnLibraryCard(captured));
            return rect;
        }

        private void AddMedalIcons(Transform parent, SkirmishLibraryCardModel card, float x, float y)
        {
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            for (int i = 0; i < difficulties.Length; i++)
            {
                var image = new GameObject("Medal" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(parent, false);
                PlaceTopLeft(image.rectTransform, x + i * 28f, y, 24f, 24f);
                image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(card.HasMedal(difficulties[i]), difficulties[i]));
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        private void PresentBriefingChrome(SkirmishLibrarySnapshot snapshot, bool fa, bool battlefield)
        {
            SkirmishLibraryCardModel card = snapshot.HasSelection ? snapshot.Selected : null;
            SkirmishLibraryBattlefieldModel field = FindBattlefield(snapshot, _libraryMap);
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            TMP_Text title = rules != null ? rules.GetComponentInChildren<TMP_Text>(true) : null;
            // The authored Title is the first labelled rule. Prefer the named object.
            Transform titleTransform = FindDeep(rules, "Title");
            if (titleTransform != null)
                title = titleTransform.GetComponent<TMP_Text>();
            if (battlefield && card != null)
                SetCopy(title, card.ArmyLabel + " · " + card.StartLabel);
            else if (field != null)
                SetCopy(title, field.Title);

            if (_recommendedPanel != null)
            {
                TMP_Text heading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
                TMP_Text body = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
                SetCopy(heading, fa ? "پیشنهاد بعدی" : "RECOMMENDED NEXT");
                if (string.IsNullOrEmpty(snapshot.RecommendedCatalogId))
                    SetCopy(body, fa ? "هنوز پیشنهادی نیست." : "No recommendation yet.");
                else
                    SetCopy(body, snapshot.RecommendedTitle);
            }

            PresentMedalRow(card, field, fa, battlefield);
            PresentDifficulty(fa);
            bool notOwned = card != null && card.Access == SkirmishLibraryCardAccess.NotOwned;
            if (_libraryStoreButton != null)
                _libraryStoreButton.gameObject.SetActive(notOwned);
            SetActive(FindDeep(rules, "Opponent"), _advancedOpen);
            SetActive(FindDeep(rules, "Objective"), _advancedOpen);
            SetActive(FindDeep(rules, "Roster"), _advancedOpen);
            SetActive(FindDeep(rules, "Economy"), _advancedOpen);
            SetActive(FindDeep(rules, "Intel"), _advancedOpen);
            SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/SeedMetric"), _advancedOpen);

            if (scenarioDescription != null && card != null)
            {
                string reason = AccessCopy(card, fa);
                string purpose = string.IsNullOrEmpty(reason) ? card.Purpose : reason;
                SetCopy(scenarioDescription, purpose);
            }
            else if (scenarioDescription != null && field != null && !battlefield)
            {
                SetCopy(scenarioDescription, field.Geography);
            }

            if (launchButton != null && !_savedMatchAvailable)
            {
                bool canStart = battlefield && card != null && card.Access == SkirmishLibraryCardAccess.OwnedPlayable;
                launchButton.interactable = canStart || !battlefield;
            }
        }

        private void PresentMedalRow(SkirmishLibraryCardModel card, SkirmishLibraryBattlefieldModel field, bool fa, bool battlefield)
        {
            if (_medalRow == null)
                return;
            var difficulties = new[]
            {
                SkirmishDifficultyId.Recruit,
                SkirmishDifficultyId.Regular,
                SkirmishDifficultyId.Veteran,
                SkirmishDifficultyId.Commander
            };
            int[] totals = field == null
                ? new[] { 0, 0, 0, 0 }
                : new[] { field.MedalRecruit, field.MedalRegular, field.MedalVeteran, field.MedalCommander };
            for (int i = 0; i < difficulties.Length; i++)
            {
                Transform slot = _medalRow.Find("Slot" + i);
                if (slot == null)
                    continue;
                bool earned = card != null && card.HasMedal(difficulties[i]);
                var image = slot.Find("Icon")?.GetComponent<Image>();
                if (image == null)
                    image = slot.GetComponentInChildren<Image>(true);
                if (image != null)
                {
                    image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(battlefield && earned, difficulties[i]));
                    if (!battlefield)
                        image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(totals[i] > 0, difficulties[i]));
                }
                SetCopy(slot.Find("Count")?.GetComponent<TMP_Text>(), battlefield ? string.Empty : totals[i].ToString());
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                SetCopy(slot.Find("Caption")?.GetComponent<TMP_Text>(), label);
            }
        }

        private void PresentDifficulty(bool fa)
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
            for (int i = 0; i < difficulties.Length; i++)
            {
                Transform slot = _difficultyRow.Find("DifficultyChoice_" + difficulties[i]);
                if (slot == null)
                    continue;
                var button = slot.GetComponent<Button>();
                TintButton(button, difficulties[i] == _medalDifficulty, true);
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                SetCopy(slot.GetComponentInChildren<TMP_Text>(true), label);
            }
        }

        private void PresentFilterLabels(bool fa)
        {
            if (_libraryFilters == null)
                return;
            SetFilterLabel(_libraryFilters.Find("ArmyGround"), fa ? "زمینی" : "GROUND");
            SetFilterLabel(_libraryFilters.Find("ArmyAir"), fa ? "هوایی" : "AIR");
            SetFilterLabel(_libraryFilters.Find("ArmyCombined"), fa ? "ترکیبی" : "COMBINED");
            SetFilterLabel(_libraryFilters.Find("StartField"), fa ? "میدانی" : "FIELD");
            SetFilterLabel(_libraryFilters.Find("StartEstablished"), fa ? "آماده" : "ESTABLISHED");
            SetFilterLabel(_libraryFilters.Find("MissingMedal"), fa ? "بدون مدال" : "MISSING MEDAL");
        }

        private void SetFilterLabel(Transform button, string label)
        {
            if (button == null)
                return;
            SetCopy(button.Find("Label")?.GetComponent<TMP_Text>(), label);
        }

        private void PresentModeToggle(bool fa, bool battlefield)
        {
            Transform toggle = transform.Find("SkirmishSetupComposition/OperationPreview/LibraryModeToggle");
            if (toggle == null)
                return;
            toggle.gameObject.SetActive(battlefield);
            SetCopy(toggle.Find("MapChoice/Label")?.GetComponent<TMP_Text>(), fa ? "نقشه" : "MAP");
            SetCopy(toggle.Find("GridChoice/Label")?.GetComponent<TMP_Text>(), fa ? "جدول" : "GRID");
            TintButton(toggle.Find("MapChoice")?.GetComponent<Button>(), _plateMode == SkirmishLibraryPlateMode.Map, false);
            TintButton(toggle.Find("GridChoice")?.GetComponent<Button>(), _plateMode == SkirmishLibraryPlateMode.Grid, false);
        }

        private void PresentScreenHeading(SkirmishLibrarySnapshot snapshot, bool fa, bool battlefield)
        {
            SetCopy(transform.Find("SkirmishSetupComposition/ScreenTitlePanel/ScreenTitle")?.GetComponent<TMP_Text>(),
                fa ? "کتابخانه نبرد" : "BATTLE LIBRARY");
            SkirmishLibraryBattlefieldModel field = FindBattlefield(snapshot, _libraryMap);
            string heading = !battlefield
                ? (fa ? "صحنه نبرد" : "THEATRE")
                : field != null ? field.Title : _libraryMap;
            if (battlefield && field != null)
                heading = field.Title + "  " + field.Won + "/" + field.Published;
            SetCopy(mapNameText, heading);
        }

        private void PresentLibraryFooter(bool fa, bool battlefield)
        {
            if (_savedMatchAvailable)
            {
                BindFooter(resetButton, ResetToDefaults, null);
                BindFooter(launchButton, LaunchMatch, null);
                BindFooter(randomizeSeedButton, RandomizeSeed, null);
                return;
            }

            if (battlefield)
            {
                BindFooter(resetButton, OnLibraryAdvanced, fa ? "پیشرفته" : "ADVANCED");
                BindFooter(randomizeSeedButton, OnLibraryTheater, fa ? "نقشه صحنه" : "THEATRE MAP");
                BindFooter(launchButton, LaunchMatch, fa ? "شروع نبرد" : "START BATTLE");
            }
            else
            {
                BindFooter(resetButton, OnLibraryFilters, fa ? "پالایه" : "FILTERS");
                BindFooter(randomizeSeedButton, OnLibraryZoom, fa ? "نزدیک‌تر" : "ZOOM IN");
                BindFooter(launchButton, OnLibraryQuickBattle, fa ? "نبرد سریع" : "QUICK BATTLE");
            }
        }

        private void BindFooter(Button button, UnityEngine.Events.UnityAction action, string label)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(ResetToDefaults);
            button.onClick.RemoveListener(RandomizeSeed);
            button.onClick.RemoveListener(LaunchMatch);
            button.onClick.RemoveListener(OnLibraryFilters);
            button.onClick.RemoveListener(OnLibraryAdvanced);
            button.onClick.RemoveListener(OnLibraryZoom);
            button.onClick.RemoveListener(OnLibraryTheater);
            button.onClick.RemoveListener(OnLibraryQuickBattle);
            button.onClick.AddListener(action);
            if (!string.IsNullOrEmpty(label))
                SetCopy(button.GetComponentInChildren<TMP_Text>(true), label);
        }

        private void OnLibraryFilters()
        {
            _filtersOpen = !_filtersOpen;
            PresentBattleLibrary();
        }

        private void OnLibraryAdvanced()
        {
            _advancedOpen = !_advancedOpen;
            PresentBattleLibrary();
        }

        private void OnLibraryZoom()
        {
            _libraryLevel = SkirmishLibraryLevel.Battlefield;
            _plateMode = SkirmishLibraryPlateMode.Map;
            _mapFilter = _libraryMap;
            PresentScenarioChoices();
        }

        private void OnLibraryTheater()
        {
            _libraryLevel = SkirmishLibraryLevel.Theater;
            _plateMode = SkirmishLibraryPlateMode.Map;
            PresentScenarioChoices();
        }

        private void OnLibraryQuickBattle()
        {
            var query = new SkirmishLibraryQuery
            {
                RightToLeft = UiShellRuntimeGateway.Localization.IsRightToLeft,
                DevelopmentBuild = Debug.isDebugBuild,
                SelectedCatalogId = _selectedScenarioId,
                MapId = _libraryMap,
                MedalDifficulty = _medalDifficulty
            };
            if (SkirmishBattleLibraryServices.Advisor != null &&
                SkirmishBattleLibraryServices.Advisor.TryQuickBattle(query, _config.MapSeed, out string catalogId) &&
                !string.IsNullOrEmpty(catalogId))
            {
                SelectScenarioId(catalogId);
                return;
            }

            _libraryLevel = SkirmishLibraryLevel.Battlefield;
            _plateMode = SkirmishLibraryPlateMode.Map;
            _mapFilter = _libraryMap;
            PresentScenarioChoices();
        }

        private void OnLibraryCard(string catalogId)
        {
            if (!TryFindEntry(catalogId, out SkirmishBattleCatalogEntry entry))
                return;
            if (!entry.IsPlayable)
            {
                _selectedScenarioId = entry.ScenarioId;
                if (IsLibraryMap(entry.MapId))
                    _libraryMap = entry.MapId;
                if (!string.IsNullOrEmpty(entry.ObjectiveId))
                    _libraryObjective = entry.ObjectiveId;
                PresentScenarioChoices();
                return;
            }

            SelectScenarioId(catalogId);
        }

        private void SelectTheaterRegion(string mapId)
        {
            _libraryMap = mapId;
            _libraryLevel = SkirmishLibraryLevel.Theater;
            _mapFilter = mapId;
            PresentScenarioChoices();
        }

        private void SelectLibraryObjective(string objectiveId)
        {
            _libraryObjective = objectiveId;
            _plateMode = SkirmishLibraryPlateMode.Map;
            PresentScenarioChoices();
        }

        private void OnLibraryMode(SkirmishLibraryPlateMode mode)
        {
            _plateMode = mode;
            _libraryLevel = SkirmishLibraryLevel.Battlefield;
            PresentScenarioChoices();
        }

        private void OnLibraryDifficulty(SkirmishDifficultyId difficulty)
        {
            _medalDifficulty = difficulty;
            SaveLibraryMemory();
            PresentBattleLibrary();
        }

        private void OnLibraryStore() => OpenLibraryStore();

        private void OpenLibraryStore()
        {
            UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute, UIRoute.CommandExchange, true);
        }

        private void OnFilterArmy(string code)
        {
            _armyFilter = string.Equals(_armyFilter, code, StringComparison.Ordinal) ? string.Empty : code;
            PresentBattleLibrary();
        }

        private void OnFilterStart(string code)
        {
            _startFilter = string.Equals(_startFilter, code, StringComparison.Ordinal) ? string.Empty : code;
            PresentBattleLibrary();
        }

        private void OnFilterMissing()
        {
            _missingMedalOnly = !_missingMedalOnly;
            PresentBattleLibrary();
        }

        private void EnsureMedalSlots(RectTransform row)
        {
            if (row == null || row.childCount > 0)
                return;
            for (int i = 0; i < 4; i++)
            {
                var slot = new GameObject("Slot" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                slot.SetParent(row, false);
                PlaceTopLeft(slot, 16f + i * 132f, 8f, 120f, 72f);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(slot, false);
                PlaceTopLeft(icon.rectTransform, 0f, 8f, 36f, 36f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var caption = CreateBoundLabel(slot, "Caption", "ui.skirmish.library_medal_" + i, "MEDAL");
                PlaceTopLeft(caption.rectTransform, 40f, 8f, 76f, 28f);
                var count = CreateBoundLabel(slot, "Count", "ui.skirmish.library_medal_count", "0");
                PlaceTopLeft(count.rectTransform, 40f, 36f, 76f, 28f);
            }
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
                PlaceTopLeft(button.GetComponent<RectTransform>(), 12f + i * 140f, 4f, 132f, 48f);
                var label = CreateBoundLabel(button.transform, "Label", "ui.skirmish.library_difficulty_" + (int)difficulty, difficulty.ToString());
                Stretch(label.rectTransform, 4f, 4f, 4f, 4f);
                button.onClick.AddListener(() => OnLibraryDifficulty(difficulty));
            }
        }

        private void EnsureRecommended(RectTransform panel)
        {
            if (panel == null || panel.Find("Heading") != null)
                return;
            var heading = CreateBoundLabel(panel, "Heading", "ui.skirmish.library_recommended", "RECOMMENDED NEXT");
            PlaceTopLeft(heading.rectTransform, 12f, 4f, 520f, 28f);
            var body = CreateBoundLabel(panel, "Body", "ui.skirmish.library_recommended_empty", "No recommendation yet.");
            PlaceTopLeft(body.rectTransform, 12f, 34f, 520f, 48f);
            body.textWrappingMode = TextWrappingModes.Normal;
        }

        private void EnsureStoreButton(Transform rules)
        {
            if (rules == null || rules.Find("LibraryStoreButton") != null)
            {
                _libraryStoreButton = rules != null ? rules.Find("LibraryStoreButton")?.GetComponent<Button>() : null;
                return;
            }

            _libraryStoreButton = CreateChromeButton(rules, "LibraryStoreButton");
            PlaceTopLeft(_libraryStoreButton.GetComponent<RectTransform>(), 16f, 620f, 240f, 52f);
            var label = CreateBoundLabel(_libraryStoreButton.transform, "Label", "ui.skirmish.library_store", "OPEN STORE");
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            _libraryStoreButton.onClick.AddListener(OnLibraryStore);
        }

        private void EnsureModeToggle(Transform preview)
        {
            if (preview == null || preview.Find("LibraryModeToggle") != null)
                return;
            var toggle = FindOrCreate(preview, "LibraryModeToggle");
            var map = CreateChromeButton(toggle, "MapChoice");
            var grid = CreateChromeButton(toggle, "GridChoice");
            PlaceTopLeft(map.GetComponent<RectTransform>(), 0f, 0f, 120f, 56f);
            PlaceTopLeft(grid.GetComponent<RectTransform>(), 128f, 0f, 120f, 56f);
            var mapLabel = CreateLabel(map.transform, "Label", 18f);
            var gridLabel = CreateLabel(grid.transform, "Label", 18f);
            Stretch(mapLabel.rectTransform);
            Stretch(gridLabel.rectTransform);
            map.onClick.AddListener(() => OnLibraryMode(SkirmishLibraryPlateMode.Map));
            grid.onClick.AddListener(() => OnLibraryMode(SkirmishLibraryPlateMode.Grid));
        }

        private void EnsureFilterPanel(RectTransform panel)
        {
            if (panel == null || panel.Find("ArmyGround") != null)
                return;
            TintPanel(panel.gameObject);
            CreateFilterButton(panel, "ArmyGround", "G", 12f, 64f);
            CreateFilterButton(panel, "ArmyAir", "A", 150f, 64f);
            CreateFilterButton(panel, "ArmyCombined", "C", 288f, 64f);
            CreateFilterButton(panel, "StartField", "F", 12f, 128f);
            CreateFilterButton(panel, "StartEstablished", "E", 150f, 128f);
            var missing = CreateChromeButton(panel, "MissingMedal");
            PlaceTopLeft(missing.GetComponent<RectTransform>(), 12f, 192f, 280f, 52f);
            var label = CreateBoundLabel(missing.transform, "Label", "ui.skirmish.library_missing_medal", "MISSING MEDAL");
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            missing.onClick.AddListener(OnFilterMissing);
        }

        private void CreateFilterButton(RectTransform panel, string name, string code, float x, float y)
        {
            var button = CreateChromeButton(panel, name);
            PlaceTopLeft(button.GetComponent<RectTransform>(), x, y, 130f, 52f);
            bool army = code == "G" || code == "A" || code == "C";
            SkirmishBattleLibraryArt.ProfileCopy(code, false, out string english);
            string key = army
                ? "ui.skirmish.library_army_" + code.ToLowerInvariant()
                : "ui.skirmish.library_start_" + code.ToLowerInvariant();
            var label = CreateBoundLabel(button.transform, "Label", key, english);
            Stretch(label.rectTransform, 4f, 4f, 4f, 4f);
            string captured = code;
            if (army)
                button.onClick.AddListener(() => OnFilterArmy(captured));
            else
                button.onClick.AddListener(() => OnFilterStart(captured));
        }

        private void PlaceLibraryChrome()
        {
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            Transform plate = preview != null ? preview.Find("MapPreviewClip") : null;
            if (plate is RectTransform plateRect)
            {
                plateRect.anchorMin = new Vector2(0f, 0f);
                plateRect.anchorMax = new Vector2(1f, 1f);
                plateRect.pivot = new Vector2(0f, 1f);
                plateRect.offsetMin = new Vector2(12f, 12f);
                plateRect.offsetMax = new Vector2(-12f, -150f);
            }

            if (battleLibrarySearch != null)
            {
                RectTransform search = battleLibrarySearch.GetComponent<RectTransform>();
                search.anchorMin = search.anchorMax = new Vector2(1f, 1f);
                search.pivot = new Vector2(1f, 1f);
                search.anchoredPosition = new Vector2(-12f, -8f);
                search.sizeDelta = new Vector2(280f, 48f);
            }

            if (battleLibraryMapTabs != null)
            {
                var tabs = battleLibraryMapTabs;
                tabs.anchorMin = new Vector2(0f, 1f);
                tabs.anchorMax = new Vector2(1f, 1f);
                tabs.pivot = new Vector2(0f, 1f);
                tabs.anchoredPosition = new Vector2(12f, -58f);
                tabs.sizeDelta = new Vector2(-280f, 72f);
            }

            Transform toggle = preview != null ? preview.Find("LibraryModeToggle") : null;
            if (toggle is RectTransform toggleRect)
            {
                toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 1f);
                toggleRect.pivot = new Vector2(1f, 1f);
                toggleRect.anchoredPosition = new Vector2(-12f, -64f);
                toggleRect.sizeDelta = new Vector2(250f, 56f);
            }
            if (_medalRow != null)
                PlaceTopLeft(_medalRow, 8f, 56f, 560f, 84f);
            if (_difficultyRow != null)
                PlaceTopLeft(_difficultyRow, 8f, 146f, 560f, 56f);
            if (_recommendedPanel != null)
                PlaceTopLeft(_recommendedPanel, 8f, 210f, 560f, 88f);
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            if (scenarioDescription != null && rules != null)
            {
                scenarioDescription.transform.SetParent(rules, false);
                var purposeBinding = scenarioDescription.GetComponent<V3LocalizedTextBindingView>()
                    ?? scenarioDescription.gameObject.AddComponent<V3LocalizedTextBindingView>();
                if (string.IsNullOrEmpty(purposeBinding.LocalizationKey) ||
                    !purposeBinding.LocalizationKey.StartsWith("ui.skirmish.", StringComparison.Ordinal))
                    purposeBinding.Configure("ui.skirmish.library_purpose", "Same map. Seed changes AI choices.");
                float purposeTop = _libraryLevel == SkirmishLibraryLevel.Battlefield ? 214f : 308f;
                PlaceTopLeft(scenarioDescription.rectTransform, 16f, purposeTop, 540f, 72f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Overflow;
            }
            if (_libraryFilters is RectTransform filters)
                PlaceTopLeft(filters, 24f, 180f, 460f, 280f);
            if (_libraryGrid is RectTransform grid)
            {
                grid.anchorMin = new Vector2(0f, 0f);
                grid.anchorMax = new Vector2(1f, 1f);
                grid.offsetMin = new Vector2(8f, 8f);
                grid.offsetMax = new Vector2(-8f, -150f);
                if (grid.GetComponent<V3GradientGraphic>() == null)
                    TintPanel(grid.gameObject);
            }

            if (rules != null && rules.Find("RulesScroll") is RectTransform scroll)
            {
                float top = _libraryLevel == SkirmishLibraryLevel.Battlefield ? 292f : 388f;
                scroll.anchorMin = Vector2.zero;
                scroll.anchorMax = Vector2.one;
                scroll.offsetMin = new Vector2(8f, 8f);
                scroll.offsetMax = new Vector2(-8f, -top);
            }
        }

        private void HideMapCollectionTabs()
        {
            if (battleLibraryMapTabs == null)
                return;
            for (int i = 0; i < battleLibraryMapTabs.childCount; i++)
            {
                Transform child = battleLibraryMapTabs.GetChild(i);
                if (child.name.StartsWith("MapTab_", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }
        }

        private void ApplyLargeTextScale()
        {
            bool large = SettingsService.Load().Accessibility.LargeText;
            float scale = large ? 1.15f : 1f;
            var bindings = GetComponentsInChildren<V3LocalizedTextBindingView>(true);
            for (int i = 0; i < bindings.Length; i++)
            {
                if (bindings[i] == null)
                    continue;
                if (bindings[i].transform.name == "Title" || bindings[i].transform.parent != null &&
                    bindings[i].transform.parent.name.StartsWith("BattleCard_", StringComparison.Ordinal))
                    bindings[i].SetPresentationScale(scale);
            }
        }

        private static string AccessCopy(SkirmishLibraryCardModel card, bool fa)
        {
            if (!string.IsNullOrEmpty(card.UnavailableReason))
                return card.UnavailableReason;
            switch (card.Access)
            {
                case SkirmishLibraryCardAccess.NotOwned:
                    return fa
                        ? "این نبرد در بسته اصلی است. برای شروع، آن را از فروشگاه بگیر."
                        : "This battle is in the core package. Open the store to add it.";
                case SkirmishLibraryCardAccess.Unavailable:
                    return fa
                        ? "این نبرد الان شروع نمی‌شود."
                        : "This battle cannot be started.";
                case SkirmishLibraryCardAccess.Planned:
                    return fa
                        ? "این نبرد هنوز در حال ساخت است."
                        : "This battle is still in development.";
                default:
                    return string.Empty;
            }
        }

        private static SkirmishLibraryBattlefieldModel FindBattlefield(SkirmishLibrarySnapshot snapshot, string mapId)
        {
            if (snapshot?.Battlefields == null)
                return null;
            for (int i = 0; i < snapshot.Battlefields.Length; i++)
            {
                if (string.Equals(snapshot.Battlefields[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    return snapshot.Battlefields[i];
            }

            return null;
        }

        private static bool IsLibraryMap(string mapId)
        {
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                if (string.Equals(SkirmishBattleLibraryArt.MapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private Button CreateChromeButton(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic), typeof(Button))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var graphic = rect.GetComponent<V3GradientGraphic>();
            graphic.Configure(new Color(0.09f, 0.13f, 0.15f, 0.92f), new Color(0.02f, 0.05f, 0.06f, 0.92f), new Color(0.26f, 0.32f, 0.34f, 1f), 2f);
            var button = rect.GetComponent<Button>();
            button.targetGraphic = graphic;
            return button;
        }

        private void TintPanel(GameObject target)
        {
            var graphic = target.GetComponent<V3GradientGraphic>() ?? target.AddComponent<V3GradientGraphic>();
            graphic.Configure(new Color(0.05f, 0.08f, 0.09f, 0.96f), new Color(0.02f, 0.04f, 0.05f, 0.96f), new Color(0.26f, 0.55f, 0.62f, 1f), 2f);
        }

        private static void TintButton(Button button, bool selected, bool playable)
        {
            V3GradientGraphic gradient = button != null ? button.targetGraphic as V3GradientGraphic : null;
            if (gradient == null)
                return;
            if (!playable && !selected)
            {
                gradient.Configure(new Color(0.08f, 0.08f, 0.09f, 0.9f), new Color(0.03f, 0.03f, 0.04f, 0.9f), new Color(0.22f, 0.22f, 0.24f, 1f), 2f);
                return;
            }

            gradient.Configure(
                selected ? new Color(0.15f, 0.28f, 0.12f, 0.95f) : new Color(0.07f, 0.12f, 0.14f, 0.55f),
                selected ? new Color(0.05f, 0.12f, 0.06f, 0.95f) : new Color(0.02f, 0.05f, 0.06f, 0.55f),
                selected ? new Color(0.95f, 0.72f, 0.15f, 1f) : new Color(0.0f, 0.72f, 0.9f, 0.85f),
                selected ? 3f : 2f);
        }

        private static void Dim(Button button)
        {
            V3GradientGraphic gradient = button != null ? button.targetGraphic as V3GradientGraphic : null;
            if (gradient != null)
                gradient.Configure(new Color(0.08f, 0.08f, 0.08f, 0.78f), new Color(0.02f, 0.02f, 0.02f, 0.78f), new Color(0.25f, 0.25f, 0.25f, 1f), 2f);
        }

        private TMP_Text CreateLabel(Transform parent, string name, float size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.font = _libraryLabelFont != null ? _libraryLabelFont : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontSizeMin = 12f;
            text.fontSizeMax = Mathf.Max(size, 18f);
            text.enableAutoSizing = true;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.96f, 0.96f, 0.94f, 1f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private TMP_Text CreateBoundLabel(Transform parent, string name, string key, string english)
        {
            TMP_Text text = CreateLabel(parent, name, 16f);
            var binding = text.gameObject.AddComponent<V3LocalizedTextBindingView>();
            binding.Configure(key, english);
            binding.ConfigureFontBounds(12f, 22f);
            return text;
        }

        private void SetCopy(TMP_Text text, string value)
        {
            if (text == null)
                return;
            var binding = text.GetComponent<V3LocalizedTextBindingView>() ?? text.gameObject.AddComponent<V3LocalizedTextBindingView>();
            if (binding.GetComponent<V3LocalizedTextBindingView>() != null && string.IsNullOrEmpty(binding.LocalizationKey))
                binding.Configure("ui.skirmish.library_runtime", "BATTLE LIBRARY");
            binding.ConfigureFontBounds(12f, 22f);
            bool large = false;
            try
            {
                large = SettingsService.Load().Accessibility.LargeText;
            }
            catch (Exception)
            {
                large = false;
            }

            if (large)
                binding.SetPresentationScale(1.15f);
            UiLocalizedText.Set(text, value ?? string.Empty);
        }

        private RectTransform FindOrCreate(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform found)
                return found;
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void PlaceRegion(RectTransform rect, Rect region)
        {
            rect.anchorMin = new Vector2(region.x, 1f - (region.y + region.height));
            rect.anchorMax = new Vector2(region.x + region.width, 1f - region.y);
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyUi(parent.GetChild(i).gameObject);
        }

        private static void DestroyUi(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
