using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Skirmish;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private const float LibraryPinWidth = 196f;
        private const float LibraryPinHeight = 132f;
        private const float LibraryGridCardWidth = 146f;
        private const float LibraryGridCardHeight = 96f;
        private const float LibraryGridOriginX = 92f;
        private const float LibraryGridStride = 154f;

        private string _armyFilter = string.Empty;
        private string _startFilter = string.Empty;
        private bool _missingMedalOnly;
        private bool _filtersOpen;
        private bool _advancedOpen;
        private bool _librarySearchApplied;
        private bool _libraryChromeReady;
        private bool _libraryPresenting;

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

        public SkirmishDifficultyId SelectedLibraryDifficulty =>
            _libraryViewModel != null ? _libraryViewModel.Briefing.SelectedDifficulty : SkirmishDifficultyId.Regular;

        public ISkirmishLibraryViewModel LibraryViewModel => _libraryViewModel;

        public void UseLibraryViewModel(SkirmishLibraryViewModel viewModel)
        {
            UnwireLibraryNotifications();
            EnsureLibraryCards();
            if (viewModel == null)
            {
                _libraryViewModel = null;
                EnsureLibraryViewModel();
                return;
            }

            _libraryViewModel = viewModel;
            WireLibraryNotifications();
            _libraryViewModel.RestoreFromPrefs();
        }

        private bool _libraryNotificationsWired;

        private void WireLibraryNotifications()
        {
            if (_libraryViewModel == null || _libraryNotificationsWired)
                return;
            _libraryViewModel.Changed += OnLibraryViewModelChanged;
            _libraryViewModel.Interaction += OnLibraryInteraction;
            _libraryNotificationsWired = true;
        }

        private void UnwireLibraryNotifications()
        {
            if (_libraryViewModel == null || !_libraryNotificationsWired)
                return;
            _libraryViewModel.Changed -= OnLibraryViewModelChanged;
            _libraryViewModel.Interaction -= OnLibraryInteraction;
            _libraryNotificationsWired = false;
        }

        private static void OnLibraryInteraction(SkirmishLibraryInteraction interaction)
        {
            UIAudioEventGateway.Raise(interaction.Sound);
        }

        private SkirmishLibraryLevel LibraryLevel =>
            _libraryViewModel != null ? _libraryViewModel.Level : SkirmishLibraryLevel.Theater;

        private SkirmishBattlefieldLayout LibraryLayout =>
            _libraryViewModel != null ? _libraryViewModel.Battlefield.Layout : SkirmishBattlefieldLayout.Map;

        private string LibraryMapId
        {
            get
            {
                if (_libraryViewModel == null)
                    return "DB";
                string map = _libraryViewModel.Theater.SelectedMapId;
                return string.IsNullOrEmpty(map) ? "DB" : map;
            }
        }

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
            EnsureLibraryPass(composition, preview, rules);
            PlaceLibraryChrome();
            _libraryChromeReady = true;
        }

        public void PresentLibraryCapture(string state)
        {
            if (!baseAssaultPreset)
                return;
            EnsureLibraryViewModel();
            switch ((state ?? string.Empty).ToLowerInvariant())
            {
                case "map":
                    _libraryViewModel.SelectMapCollection(string.IsNullOrEmpty(LibraryMapId) ? "DB" : LibraryMapId);
                    _libraryViewModel.ZoomIn();
                    _libraryViewModel.SetLayout(SkirmishBattlefieldLayout.Map);
                    break;
                case "grid":
                    _libraryViewModel.SelectMapCollection(string.IsNullOrEmpty(LibraryMapId) ? "DB" : LibraryMapId);
                    _libraryViewModel.ZoomIn();
                    _libraryViewModel.SetLayout(SkirmishBattlefieldLayout.Grid);
                    break;
                default:
                    _libraryViewModel.BackToTheater();
                    break;
            }

            _filtersOpen = false;
            EnsureLibraryCards();
            PresentScenarioChoices();
        }

        private void LoadLibraryMemory() => EnsureLibraryViewModel();

        private void OnLibraryViewModelChanged()
        {
            if (_libraryPresenting || !baseAssaultPreset)
                return;
            PresentBattleLibrary();
        }

        private void RememberLibrarySelection(SkirmishBattleCatalogEntry entry)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectScenarioId(entry.ScenarioId);
            if (!string.IsNullOrEmpty(entry.MapId))
                _mapFilter = entry.MapId;
        }

        private void SyncLibraryFilter()
        {
            if (_libraryViewModel == null)
                return;
            SkirmishLibraryFilter next = new SkirmishLibraryFilter
            {
                SearchText = _searchFilter,
                ArmyProfile = _armyFilter,
                StartProfile = _startFilter,
                MedalMissing = _missingMedalOnly ? SelectedLibraryDifficulty : (SkirmishDifficultyId?)null
            };
            SkirmishLibraryFilter current = _libraryViewModel.Filter;
            if (string.Equals(current.SearchText ?? string.Empty, next.SearchText ?? string.Empty, StringComparison.Ordinal) &&
                string.Equals(current.ArmyProfile ?? string.Empty, next.ArmyProfile ?? string.Empty, StringComparison.Ordinal) &&
                string.Equals(current.StartProfile ?? string.Empty, next.StartProfile ?? string.Empty, StringComparison.Ordinal) &&
                current.MedalMissing == next.MedalMissing)
                return;
            _libraryViewModel.ApplyFilter(next);
        }

        private void PresentBattleLibrary()
        {
            if (!baseAssaultPreset)
                return;
            EnsureBattleLibraryChrome();
            if (_libraryPins == null && _libraryRegions == null)
                return;

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            _libraryPresenting = true;
            try
            {
                EnsureLibraryViewModel();
                SyncLibraryFilter();
                ApplyLargeTextScale();
                PlaceLibraryChrome();
                if (TryJumpToSearchHit())
                    return;

                SkirmishTheaterState theater = _libraryViewModel.Theater;
                SkirmishBattlefieldState field = _libraryViewModel.Battlefield;
                SkirmishBriefingState briefing = _libraryViewModel.Briefing;
                bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
                bool grid = battlefield && LibraryLayout == SkirmishBattlefieldLayout.Grid;
                SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary"), false);
                SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryCount"), false);
                SetActive(battleLibraryMapTabs, false);
                HideMapCollectionTabs();
                SetActive(_libraryRegions, !battlefield);
                SetActive(_libraryPins, battlefield && !grid);
                SetActive(_objectiveColumn, battlefield && !grid);
                SetActive(_libraryGrid, grid);
                SetActive(_libraryFilters, _filtersOpen);
                SetActive(_recommendedPanel, !battlefield);
                SetActive(_difficultyRow, battlefield);
                SetActive(_medalRow, true);
                SetActive(_progressTrack, false);
                SetActive(_statRow, battlefield);
                SetActive(_medalGrid, !battlefield);
                SetActive(_briefingPreview, battlefield);
                PresentPlateTexture(battlefield, field);
                PresentRegions(theater, fa);
                PresentModeToggle(fa, battlefield);
                if (battlefield && !grid)
                {
                    PresentObjectiveColumn(field, fa);
                    PresentPins(field);
                }
                if (grid)
                    PresentGrid(field, fa);
                PresentBriefingChrome(theater, field, briefing, fa, battlefield);
                string pinMap = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
                bool hasPin = TryFindTheaterPin(theater, pinMap, out SkirmishTheaterPinState selectedPin);
                PresentProgress(theater, fa);
                PresentBriefingPreview(briefing, hasPin, selectedPin, battlefield);
                if (!battlefield)
                    PresentTheaterDetail(fa);
                PresentFilterLabels(fa);
                PresentLibraryFooter(fa, battlefield);
                StyleCommandButtons(battlefield);
                PlaceLibraryFooter();
                PresentCaptureCredits();
                PresentScreenHeading(theater, field, fa, battlefield);
                ApplyLibraryDepth();
            }
            finally
            {
                _libraryPresenting = false;
            }
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
            EnsureLibraryViewModel();
            _libraryViewModel.SelectScenarioId(entry.ScenarioId);
            if (!string.IsNullOrEmpty(entry.MapId))
                _mapFilter = entry.MapId;
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

        private void PresentPlateTexture(bool battlefield, SkirmishBattlefieldState field)
        {
            RawImage map = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip/MapPreview")
                ?.GetComponent<RawImage>();
            if (map == null)
                return;
            string key = SkirmishBattleLibraryArt.TheaterPlate;
            if (battlefield)
            {
                string mapId = string.IsNullOrEmpty(field.MapId) ? LibraryMapId : field.MapId;
                key = SkirmishBattleLibraryArt.Plate(mapId);
                if (TryFindTheaterPin(_libraryViewModel.Theater, mapId, out SkirmishTheaterPinState pin) &&
                    !string.IsNullOrEmpty(pin.PlatePreviewKey))
                    key = LibraryArtKey(pin.PlatePreviewKey, mapId);
            }

            Texture2D texture = Resources.Load<Texture2D>(key);
            if (texture != null)
                map.texture = texture;
            map.color = Color.white;
            AspectRatioFitter fitter = map.GetComponent<AspectRatioFitter>();
            if (fitter != null)
                fitter.enabled = false;
            Stretch(map.rectTransform);
        }

        private void PresentRegions(SkirmishTheaterState theater, bool fa)
        {
            if (_libraryRegions == null)
                return;
            ClearChildren(_libraryRegions);
            _regionButtons.Clear();
            if (theater.Pins == null)
                return;
            for (int i = 0; i < theater.Pins.Length; i++)
            {
                SkirmishTheaterPinState pin = theater.Pins[i];
                if (!TryRegion(pin.MapId, out Rect region, out Vector2 centre))
                    continue;
                bool selected = string.Equals(pin.MapId, theater.SelectedMapId, StringComparison.OrdinalIgnoreCase);
                var button = CreateHitButton(_libraryRegions, SkirmishLibraryControlNames.TheaterPin(pin.MapId));
                PlaceRegion(button.GetComponent<RectTransform>(), region);
                var wash = button.targetGraphic as Image;
                if (wash != null)
                    wash.color = selected
                        ? new Color(0.95f, 0.78f, 0.25f, 0.10f)
                        : new Color(0f, 0f, 0f, 0f);
                string captured = pin.MapId;
                button.onClick.AddListener(() => SelectTheaterRegion(captured));
                var label = CreateLabel(_libraryRegions, "RegionLabel_" + pin.MapId, 18f);
                PlaceLabelCentre(label.rectTransform, centre, 220f, 28f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.color = selected ? new Color(1f, 0.9f, 0.55f, 1f) : Color.white;
                SetCopy(label, LibraryText(pin.TitleLocKey, fa), 15f, 20f);
                var count = CreateLabel(_libraryRegions, "RegionCount_" + pin.MapId, 16f);
                PlaceLabelCentre(count.rectTransform, centre + new Vector2(0f, 0.045f), 120f, 24f);
                count.alignment = TextAlignmentOptions.Center;
                count.isRightToLeftText = false;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                SetCopy(count, pin.WonCount + "/" + pin.PublishedCount, 15f, 18f);
                _regionButtons.Add(button);
            }
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
                var button = CreateChromeButton(battleLibraryMapTabs, "ObjectiveTab_" + objective.ObjectiveId);
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

        private void PresentGrid(SkirmishBattlefieldState field, bool fa)
        {
            ClearChildren(_libraryGrid);
            if (field.GridCards == null || field.Tabs == null)
                return;
            PresentBattlefieldTabs(field.MapId, fa);
            string[] armies = { "G", "A", "C" };
            string[] starts = { "F", "E" };
            for (int armyIndex = 0; armyIndex < armies.Length; armyIndex++)
            {
                for (int startIndex = 0; startIndex < starts.Length; startIndex++)
                {
                    int column = armyIndex * 2 + startIndex;
                    var headerIcon = new GameObject("ColumnIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    headerIcon.transform.SetParent(_libraryGrid, false);
                    PlaceTopLeft(headerIcon.rectTransform, LibraryGridOriginX + column * LibraryGridStride + 8f, 52f, 24f, 24f);
                    headerIcon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ArmyIcon(armies[armyIndex]));
                    headerIcon.preserveAspect = true;
                    headerIcon.raycastTarget = false;
                    SkirmishBattleLibraryArt.ProfileCopy(armies[armyIndex], fa, out string armyLabel);
                    SkirmishBattleLibraryArt.ProfileCopy(starts[startIndex], fa, out string startLabel);
                    var header = CreateLabel(_libraryGrid, "Column_" + column, 16f);
                    PlaceTopLeft(header.rectTransform, LibraryGridOriginX + column * LibraryGridStride, 78f, LibraryGridCardWidth, 36f);
                    header.alignment = TextAlignmentOptions.Center;
                    header.textWrappingMode = TextWrappingModes.Normal;
                    header.overflowMode = TextOverflowModes.Overflow;
                    SetCopy(header, armyLabel + " " + startLabel, 13f, 16f);
                }
            }

            int rows = field.Tabs.Length;
            for (int row = 0; row < rows; row++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[row];
                float y = 118f + row * (LibraryGridCardHeight + 8f);
                var rowIcon = new GameObject("RowIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                rowIcon.transform.SetParent(_libraryGrid, false);
                PlaceTopLeft(rowIcon.rectTransform, 8f, y + 8f, 36f, 36f);
                rowIcon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId));
                rowIcon.preserveAspect = true;
                rowIcon.raycastTarget = false;
                var rowLabel = CreateLabel(_libraryGrid, "Row_" + objective.ObjectiveId, 14f);
                PlaceTopLeft(rowLabel.rectTransform, 4f, y + 44f, LibraryGridOriginX - 10f, 44f);
                rowLabel.alignment = TextAlignmentOptions.Center;
                rowLabel.textWrappingMode = TextWrappingModes.Normal;
                rowLabel.overflowMode = TextOverflowModes.Overflow;
                SetCopy(
                    rowLabel,
                    LibraryText(objective.TitleLocKey, fa) + "\n" + objective.WonCount + "/" + objective.PublishedCount,
                    11f,
                    14f);
                for (int column = 0; column < 6; column++)
                {
                    int index = row * 6 + column;
                    if (index >= field.GridCards.Length)
                        break;
                    SkirmishBattleCardState card = field.GridCards[index];
                    RectTransform widget = IsEmptyCard(card)
                        ? CreateEmptyGridCell(_libraryGrid, field.MapId, objective.ObjectiveId, armies[column / 2], starts[column % 2])
                        : CreateBattleCard(_libraryGrid, card, false, LibraryGridCardWidth, LibraryGridCardHeight);
                    PlaceTopLeft(widget, LibraryGridOriginX + column * LibraryGridStride, y, LibraryGridCardWidth, LibraryGridCardHeight);
                    AddSiblingShadow(widget);
                }
            }
        }

        private void PresentBattlefieldTabs(string selectedMapId, bool fa)
        {
            if (_libraryGrid == null)
                return;
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                var button = CreateChromeButton(_libraryGrid, "BattlefieldTab_" + mapId);
                float width = 168f;
                PlaceTopLeft(button.GetComponent<RectTransform>(), 8f + i * (width + 6f), 4f, width, 46f);
                TintButton(button, string.Equals(mapId, selectedMapId, StringComparison.OrdinalIgnoreCase), true);
                var label = CreateLabel(button.transform, "Label", 16f);
                Stretch(label.rectTransform, 6f, 4f, 4f, 4f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                string title = mapId;
                if (_libraryViewModel != null &&
                    TryFindTheaterPin(_libraryViewModel.Theater, mapId, out SkirmishTheaterPinState mapPin))
                    title = LibraryText(mapPin.TitleLocKey, fa);
                SetCopy(label, title, 13f, 16f);
                string captured = mapId;
                button.onClick.AddListener(() => SelectGridBattlefield(captured));
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

        private RectTransform CreateBattleCard(Transform parent, SkirmishBattleCardState card, bool pin, float width = LibraryPinWidth, float height = LibraryPinHeight)
        {
            var button = CreateChromeButton(parent, "BattleCard_" + card.ScenarioId);
            var rect = button.GetComponent<RectTransform>();
            if (pin && SkirmishBattlefieldPinLayout.TryGet(card.PinId, out Vector2 centre))
            {
                rect.anchorMin = rect.anchorMax = new Vector2(centre.x, 1f - centre.y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = Vector2.zero;
            }

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float footer = pin ? 56f : 36f;
            MaskRect(rect);
            bool playable = card.Access == SkirmishCardAccess.Playable;
            TintButton(button, card.IsSelected, playable, true);
            button.interactable = !string.IsNullOrEmpty(card.ScenarioId);
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(button.transform, false);
            Stretch(preview.rectTransform, 3f, 3f, 3f, footer);
            preview.texture = Resources.Load<Texture2D>(CardPreviewResource(card));
            preview.color = preview.texture != null ? Color.white : new Color(0.12f, 0.16f, 0.18f, 1f);
            preview.raycastTarget = false;
            var armyIcon = new GameObject("ArmyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            armyIcon.transform.SetParent(button.transform, false);
            PlaceTopLeft(armyIcon.rectTransform, 4f, height - footer + 2f, 18f, 18f);
            armyIcon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ArmyIcon(card.ArmyProfile));
            armyIcon.preserveAspect = true;
            armyIcon.raycastTarget = false;
            var title = CreateLabel(button.transform, "Title", 15f);
            PlaceTopLeft(title.rectTransform, 24f, height - footer + 2f, width - 72f, 18f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            SetCopy(title, LibraryText(card.TitleLocKey, fa), 13f, 16f);
            if (card.IsRecommended)
            {
                var recommended = CreateLabel(button.transform, "Recommended", 12f);
                PlaceTopLeft(recommended.rectTransform, 4f, 2f, width - 8f, 16f);
                recommended.alignment = TextAlignmentOptions.MidlineLeft;
                recommended.textWrappingMode = TextWrappingModes.NoWrap;
                recommended.color = new Color(0.95f, 0.82f, 0.28f, 1f);
                SetCopy(recommended, fa ? "پیشنهاد" : "NEXT", 11f, 13f);
            }
            float medal = pin ? 20f : 16f;
            AddMedalIcons(button.transform, card.Medals, 4f, height - medal - 2f, medal);
            if (card.Access == SkirmishCardAccess.Playable && card.Medals == SkirmishMedalMask.None)
            {
                var badgeLabel = CreateLabel(button.transform, "NewLabel", 13f);
                PlaceTopLeft(badgeLabel.rectTransform, width - 46f, height - footer + 1f, 42f, 18f);
                badgeLabel.alignment = TextAlignmentOptions.Center;
                badgeLabel.color = new Color(0.95f, 0.82f, 0.28f, 1f);
                badgeLabel.textWrappingMode = TextWrappingModes.NoWrap;
                SetCopy(badgeLabel, fa ? "نو" : "NEW", 12f, 14f);
            }
            if (card.Access == SkirmishCardAccess.NotOwned || card.Access == SkirmishCardAccess.Unavailable)
            {
                var lockLabel = CreateLabel(button.transform, "Lock", 13f);
                PlaceTopLeft(lockLabel.rectTransform, width - 52f, height - footer + 1f, 48f, 18f);
                lockLabel.alignment = TextAlignmentOptions.Center;
                lockLabel.textWrappingMode = TextWrappingModes.NoWrap;
                SetCopy(lockLabel, fa ? "قفل" : "LOCK", 12f, 14f);
            }

            string captured = card.ScenarioId;
            button.onClick.AddListener(() => OnLibraryCard(captured));
            if (pin)
                AddSiblingShadow(rect);
            return rect;
        }

        private RectTransform CreateEmptyGridCell(Transform parent, string mapId, string objectiveId, string army, string start)
        {
            var rect = new GameObject("EmptyCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var plate = rect.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.07f, 0.08f, 0.92f);
            plate.raycastTarget = false;
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(rect, false);
            Stretch(preview.rectTransform, 3f, 3f, 3f, 22f);
            preview.texture = Resources.Load<Texture2D>(SkirmishBattleLibraryArt.CellPreview(mapId, objectiveId, army, start));
            preview.color = preview.texture != null ? new Color(0.45f, 0.45f, 0.45f, 1f) : new Color(0.1f, 0.12f, 0.13f, 1f);
            preview.raycastTarget = false;
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
            for (int i = 0; i < difficulties.Length; i++)
            {
                var image = new GameObject("Medal" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                image.transform.SetParent(parent, false);
                PlaceTopLeft(image.rectTransform, x + i * (size + 2f), y, size, size);
                image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(MedalEarned(medals, difficulties[i]), difficulties[i]));
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

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
                SetCopy(title, LibraryText(briefing.TitleLocKey, fa), 16f, 26f);
            else if (hasPin)
                SetCopy(title, LibraryText(pin.TitleLocKey, fa), 16f, 26f);
            if (title != null)
            {
                title.textWrappingMode = TextWrappingModes.Normal;
                title.overflowMode = TextOverflowModes.Overflow;
            }

            if (_recommendedPanel != null)
            {
                TMP_Text heading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
                TMP_Text body = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
                SetCopy(heading, fa ? "پیشنهاد بعدی" : "RECOMMENDED NEXT");
                if (string.IsNullOrEmpty(theater.RecommendedScenarioId))
                    SetCopy(body, fa ? "هنوز پیشنهادی نیست." : "No recommendation yet.");
                else if (string.Equals(briefing.ScenarioId, theater.RecommendedScenarioId, StringComparison.Ordinal) &&
                         !string.IsNullOrEmpty(briefing.TitleLocKey))
                    SetCopy(body, LibraryText(briefing.TitleLocKey, fa));
                else
                    SetCopy(body, LibraryTitle(theater.RecommendedScenarioId, fa));
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
                SetCopy(scenarioDescription, LibraryText(pin.SubtitleLocKey, fa), 15f, 22f);
            else if (scenarioDescription != null && !string.IsNullOrEmpty(briefing.ScenarioId))
            {
                string purpose = MixedText(LibraryPurpose(briefing, fa), fa);
                string reason = LibraryReason(briefing, fa);
                SetCopy(scenarioDescription, string.IsNullOrEmpty(reason) ? purpose : purpose + "\n" + reason, 15f, 22f);
            }

            if (launchButton != null && !_savedMatchAvailable)
                launchButton.interactable = !battlefield || briefing.CanLaunch;
        }

        private void PresentMedalRow(SkirmishBriefingState briefing, bool hasPin, SkirmishTheaterPinState pin, bool fa, bool battlefield)
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
            SkirmishMedalTotals totals = hasPin ? pin.Medals : default;
            int[] counts = { totals.Recruit, totals.Regular, totals.Veteran, totals.Commander };
            SkirmishMedalMask mask = briefing.Medals;
            for (int i = 0; i < difficulties.Length; i++)
            {
                Transform slot = _medalRow.Find("Slot" + i);
                if (slot == null)
                    continue;
                bool earned = battlefield && MedalEarned(mask, difficulties[i]);
                if (!battlefield)
                    earned = counts[i] > 0;
                var image = slot.Find("Icon")?.GetComponent<Image>();
                if (image == null)
                    image = slot.GetComponentInChildren<Image>(true);
                if (image != null)
                    image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(earned, difficulties[i]));
                SetCopy(slot.Find("Count")?.GetComponent<TMP_Text>(), battlefield ? string.Empty : counts[i].ToString(), 16f, 22f);
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                SetCopy(slot.Find("Caption")?.GetComponent<TMP_Text>(), label, 15f, 22f);
            }
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
                TMP_Text choice = slot.GetComponentInChildren<TMP_Text>(true);
                if (choice != null)
                {
                    choice.fontSize = 18f;
                    choice.fontSizeMin = 14f;
                    choice.fontSizeMax = 20f;
                    choice.textWrappingMode = TextWrappingModes.NoWrap;
                    choice.overflowMode = TextOverflowModes.Overflow;
                }
                SetCopy(choice, label, 14f, 20f);
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
            Button mapChoice = toggle.Find("MapChoice")?.GetComponent<Button>();
            Button gridChoice = toggle.Find("GridChoice")?.GetComponent<Button>();
            TintButton(mapChoice, LibraryLayout == SkirmishBattlefieldLayout.Map, true);
            TintButton(gridChoice, LibraryLayout == SkirmishBattlefieldLayout.Grid, true);
            AliasControl(mapChoice, SkirmishLibraryControlNames.LayoutMap);
            AliasControl(gridChoice, SkirmishLibraryControlNames.LayoutGrid);
        }

        private void PresentScreenHeading(SkirmishTheaterState theater, SkirmishBattlefieldState field, bool fa, bool battlefield)
        {
            TMP_Text screenTitle = transform.Find("SkirmishSetupComposition/ScreenTitlePanel/ScreenTitle")?.GetComponent<TMP_Text>();
            string headingMap = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
            bool hasHeading = TryFindTheaterPin(theater, headingMap, out SkirmishTheaterPinState headingPin);
            string mapTitle = hasHeading ? LibraryText(headingPin.TitleLocKey, fa) : (fa ? "صحنه نبرد" : "THEATRE");
            string route = !battlefield
                ? (fa ? "زد و خورد | نقشه صحنه" : "SKIRMISH | THEATRE MAP")
                : (fa ? "زد و خورد | " : "SKIRMISH | ") + mapTitle;
            SetCopy(screenTitle, MixedText(route, fa), 20f, 32f);
            if (screenTitle != null)
                screenTitle.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            SetCopy(mapNameText, mapTitle, 18f, 28f);
            if (mapNameText != null)
                mapNameText.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            if (screenTitle != null)
            {
                screenTitle.textWrappingMode = TextWrappingModes.NoWrap;
                screenTitle.overflowMode = TextOverflowModes.Overflow;
            }
        }

        private void PlaceLibraryFooter()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition == null)
                return;
            Button back = composition.Find("BackButton")?.GetComponent<Button>();
            float y = 818f;
            float height = 108f;
            float gap = 12f;
            float x = 14f;
            float backWidth = 260f;
            float middle = 400f;
            if (back != null)
            {
                PlaceTopLeft(back.GetComponent<RectTransform>(), x, y, backWidth, height);
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                SetCopy(back.GetComponentInChildren<TMP_Text>(true), fa ? "بازگشت" : "BACK", 16f, 28f);
                TMP_Text backLabel = back.GetComponentInChildren<TMP_Text>(true);
                if (backLabel != null)
                    backLabel.textWrappingMode = TextWrappingModes.NoWrap;
                AttachAudio(back, UIAudioEventKind.ScreenBack);
            }

            x += backWidth + gap;
            if (resetButton != null)
                PlaceTopLeft(resetButton.GetComponent<RectTransform>(), x, y, middle, height);
            x += middle + gap;
            if (randomizeSeedButton != null)
                PlaceTopLeft(randomizeSeedButton.GetComponent<RectTransform>(), x, y, middle, height);
            x += middle + gap;
            if (launchButton != null)
                PlaceTopLeft(launchButton.GetComponent<RectTransform>(), x, y, 1672f - 14f - x, height);
        }

        private void PresentCaptureCredits()
        {
            MainMenuAccountHeaderView header = GetComponentInChildren<MainMenuAccountHeaderView>(true);
            if (header == null)
                return;
            header.enabled = false;
            if (header.Value == null)
                return;
            header.Value.text = ReadProfileCredits();
            header.Value.isRightToLeftText = false;
            header.Value.textWrappingMode = TextWrappingModes.NoWrap;
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
            if (action == OnLibraryZoom)
                AliasControl(button, SkirmishLibraryControlNames.ZoomIn);
            else if (action == OnLibraryQuickBattle)
                AliasControl(button, SkirmishLibraryControlNames.QuickBattle);
            else if (action == OnLibraryFilters)
                AliasControl(button, SkirmishLibraryControlNames.Filters);
            else if (action == OnLibraryTheater)
                AliasControl(button, SkirmishLibraryControlNames.TheaterMap);
            else if (action == LaunchMatch)
                AliasControl(button, SkirmishLibraryControlNames.StartBattle);

            bool localSound = action == OnLibraryFilters || action == OnLibraryAdvanced;
            if (localSound)
                AttachAudio(button, UIAudioEventKind.ButtonSecondaryClick);
            else
                ClearButtonAudio(button);
        }

        private static void ClearButtonAudio(Button button)
        {
            if (button == null)
                return;
            UIButtonAudioEventView audio = button.GetComponent<UIButtonAudioEventView>();
            if (audio != null)
                DestroyUi(audio);
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
            EnsureLibraryViewModel();
            _libraryViewModel.ZoomIn();
            _mapFilter = LibraryMapId;
            PresentScenarioChoices();
        }

        private void OnLibraryTheater()
        {
            EnsureLibraryViewModel();
            _libraryViewModel.BackToTheater();
            PresentScenarioChoices();
        }

        private void OnLibraryQuickBattle()
        {
            EnsureLibraryViewModel();
            string catalogId = _libraryViewModel.QuickBattle();
            if (string.IsNullOrEmpty(catalogId))
            {
                _libraryViewModel.ZoomIn();
                _mapFilter = LibraryMapId;
                PresentScenarioChoices();
                return;
            }

            AdoptLibrarySelection(catalogId);
        }

        private void AdoptLibrarySelection(string scenarioId)
        {
            if (!TryFindEntry(scenarioId, out SkirmishBattleCatalogEntry entry))
            {
                PresentScenarioChoices();
                return;
            }

            _selectedScenarioId = entry.ScenarioId;
            if (!string.IsNullOrEmpty(entry.MapId))
                _mapFilter = entry.MapId;
            if (entry.IsPlayable)
            {
                _config.ScenarioIndex = entry.PlayableScenarioIndex;
                PlayerPrefs.SetString(LastScenarioIdPrefsKey, _selectedScenarioId);
                PlayerPrefs.Save();
            }

            Bind(_config);
            if (entry.IsPlayable)
                _configStore?.Apply(ReadConfigFromControls());
        }

        private void OnLibraryCard(string catalogId) => SelectScenarioId(catalogId);

        private void SelectTheaterRegion(string mapId)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectMapCollection(mapId);
            _libraryViewModel.BackToTheater();
            _mapFilter = mapId;
            PresentScenarioChoices();
        }

        private void SelectLibraryObjective(string objectiveId)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectObjective(objectiveId);
            PresentScenarioChoices();
        }

        private void OnLibraryMode(SkirmishBattlefieldLayout mode)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SetLayout(mode);
            PresentScenarioChoices();
        }

        private void OnLibraryDifficulty(SkirmishDifficultyId difficulty)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectDifficulty(difficulty);
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
                PlaceTopLeft(slot, 8f + i * 138f, 4f, 132f, 88f);
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(slot, false);
                PlaceTopLeft(icon.rectTransform, 4f, 8f, 40f, 40f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var caption = CreateBoundLabel(slot, "Caption", "ui.skirmish.library_medal_" + i, "MEDAL");
                PlaceTopLeft(caption.rectTransform, 46f, 8f, 82f, 36f);
                caption.alignment = TextAlignmentOptions.MidlineLeft;
                var count = CreateBoundLabel(slot, "Count", "ui.skirmish.library_medal_count", "0");
                PlaceTopLeft(count.rectTransform, 46f, 46f, 82f, 32f);
                count.alignment = TextAlignmentOptions.MidlineLeft;
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
                PlaceTopLeft(button.GetComponent<RectTransform>(), 4f + i * 130f, 4f, 124f, 56f);
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
            AttachAudio(_libraryStoreButton, UIAudioEventKind.ButtonPrimaryClick);
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
            map.onClick.AddListener(() => OnLibraryMode(SkirmishBattlefieldLayout.Map));
            grid.onClick.AddListener(() => OnLibraryMode(SkirmishBattlefieldLayout.Grid));
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
            RectTransform screenTitle = transform.Find("SkirmishSetupComposition/ScreenTitlePanel/ScreenTitle") as RectTransform;
            if (screenTitle != null)
                screenTitle.offsetMin = new Vector2(64f, screenTitle.offsetMin.y);
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            Transform plate = preview != null ? preview.Find("MapPreviewClip") : null;
            bool theater = LibraryLevel != SkirmishLibraryLevel.Battlefield;
            if (plate is RectTransform plateRect)
            {
                plateRect.anchorMin = new Vector2(0f, 0f);
                plateRect.anchorMax = new Vector2(1f, 1f);
                plateRect.pivot = new Vector2(0f, 1f);
                plateRect.offsetMin = new Vector2(8f, theater ? 84f : 8f);
                plateRect.offsetMax = new Vector2(-8f, -104f);
                MaskRect(plateRect);
            }

            if (_overallStrip != null && preview is RectTransform previewRect)
            {
                _overallStrip.SetParent(previewRect, false);
                _overallStrip.anchorMin = new Vector2(0f, 0f);
                _overallStrip.anchorMax = new Vector2(1f, 0f);
                _overallStrip.pivot = new Vector2(0.5f, 0f);
                _overallStrip.anchoredPosition = new Vector2(0f, 8f);
                _overallStrip.sizeDelta = new Vector2(-16f, 68f);
            }

            if (_libraryPins is RectTransform pinRect)
            {
                pinRect.anchorMin = Vector2.zero;
                pinRect.anchorMax = Vector2.one;
                pinRect.offsetMin = new Vector2(96f, 8f);
                pinRect.offsetMax = new Vector2(-8f, -8f);
            }

            if (battleLibrarySearch != null)
            {
                RectTransform search = battleLibrarySearch.GetComponent<RectTransform>();
                if (preview != null && search.parent != preview)
                    search.SetParent(preview, false);
                RectMask2D searchMask = search.GetComponent<RectMask2D>();
                if (searchMask != null)
                    searchMask.enabled = false;
                PlaceTopLeft(search, 12f, 52f, 700f, 48f);
                search.SetAsLastSibling();
                if (battleLibrarySearch.textViewport != null && battleLibrarySearch.textViewport != search)
                    Stretch(battleLibrarySearch.textViewport, 8f, 8f, 4f, 4f);
                foreach (TMP_Text fieldText in battleLibrarySearch.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (fieldText.rectTransform == search)
                        continue;
                    Stretch(fieldText.rectTransform, 12f, 10f, 4f, 4f);
                    fieldText.textWrappingMode = TextWrappingModes.NoWrap;
                    fieldText.overflowMode = TextOverflowModes.Overflow;
                    fieldText.fontSize = 18f;
                    fieldText.fontSizeMin = 15f;
                    fieldText.enableAutoSizing = true;
                }
            }

            if (mapNameText != null)
            {
                PlaceTopLeft(mapNameText.rectTransform, 12f, 4f, 680f, 44f);
                mapNameText.textWrappingMode = TextWrappingModes.NoWrap;
                mapNameText.overflowMode = TextOverflowModes.Ellipsis;
            }

            if (preview != null)
            {
                SetActive(preview.Find("ObjectiveRow"), false);
                SetActive(preview.Find("Metrics"), false);
                SetActive(preview.Find("TitleRule"), false);
            }

            Transform titlePanel = transform.Find("SkirmishSetupComposition/ScreenTitlePanel");
            if (titlePanel is RectTransform titlePanelRect)
                PlaceTopLeft(titlePanelRect, 14f, 13f, 980f, 100f);

            if (battleLibraryMapTabs != null)
            {
                var tabs = battleLibraryMapTabs;
                tabs.anchorMin = new Vector2(0f, 1f);
                tabs.anchorMax = new Vector2(1f, 1f);
                tabs.pivot = new Vector2(0f, 1f);
                tabs.anchoredPosition = new Vector2(12f, -8f);
                tabs.sizeDelta = new Vector2(0f, 0f);
                tabs.gameObject.SetActive(false);
            }

            Transform toggle = preview != null ? preview.Find("LibraryModeToggle") : null;
            if (toggle is RectTransform toggleRect)
            {
                toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 1f);
                toggleRect.pivot = new Vector2(1f, 1f);
                toggleRect.anchoredPosition = new Vector2(-12f, -8f);
                toggleRect.sizeDelta = new Vector2(250f, 48f);
            }
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            if (_medalRow != null)
                PlaceTopLeft(_medalRow, 12f, battlefield ? 336f : 168f, 526f, battlefield ? 72f : 88f);
            if (_difficultyRow != null)
                PlaceTopLeft(_difficultyRow, 8f, 568f, 534f, 64f);
            if (_libraryStoreButton != null)
                PlaceTopLeft(_libraryStoreButton.GetComponent<RectTransform>(), 16f, 476f, 250f, 44f);
            if (_recommendedPanel != null)
                PlaceTopLeft(_recommendedPanel, 12f, 268f, 526f, 80f);
            if (_progressTrack != null)
                _progressTrack.gameObject.SetActive(false);
            if (_briefingPreview != null)
                PlaceTopLeft(_briefingPreview.rectTransform, 16f, 140f, 518f, 100f);
            if (_statRow != null)
                PlaceTopLeft(_statRow, 12f, 248f, 526f, 76f);
            if (_medalGrid != null)
                PlaceTopLeft(_medalGrid, 12f, 416f, 526f, 96f);
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            Transform rulesTitle = rules != null ? rules.Find("Title") : null;
            if (rulesTitle is RectTransform titleRect)
                PlaceTopLeft(titleRect, 16f, 8f, 518f, 64f);
            if (scenarioDescription != null && rules != null)
            {
                scenarioDescription.transform.SetParent(rules, false);
                var purposeBinding = scenarioDescription.GetComponent<V3LocalizedTextBindingView>()
                    ?? scenarioDescription.gameObject.AddComponent<V3LocalizedTextBindingView>();
                if (string.IsNullOrEmpty(purposeBinding.LocalizationKey) ||
                    !purposeBinding.LocalizationKey.StartsWith("ui.skirmish.", StringComparison.Ordinal))
                    purposeBinding.Configure("ui.skirmish.library_purpose", "Same map. Seed changes AI choices.");
                PlaceTopLeft(scenarioDescription.rectTransform, 16f, 76f, 518f, 56f);
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
                grid.offsetMax = new Vector2(-8f, -104f);
                if (grid.GetComponent<V3GradientGraphic>() == null)
                    TintPanel(grid.gameObject);
            }

            if (rules != null && rules.Find("RulesScroll") is RectTransform scroll)
            {
                float top = LibraryLevel == SkirmishLibraryLevel.Battlefield ? 630f : 400f;
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

        private string LibraryText(string key, bool fa)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (key.EndsWith("locked.planned", StringComparison.Ordinal))
                return string.Empty;
            string localized = UiShellRuntimeGateway.Localization.Get(key, string.Empty);
            if (!IsSuppressedLibraryCopy(localized))
                return localized;
            string art = SkirmishBattleLibraryArt.Fallback(key, fa);
            if (!IsSuppressedLibraryCopy(art))
                return art;
            return ResolveCatalogCopy(key, fa);
        }

        private string LibraryReason(SkirmishBriefingState briefing, bool fa)
        {
            if (string.IsNullOrEmpty(briefing.LockedReasonLocKey) ||
                briefing.LockedReasonLocKey.EndsWith("locked.planned", StringComparison.Ordinal))
                return string.Empty;
            string reason = MixedText(LibraryText(briefing.LockedReasonLocKey, fa), fa);
            return IsSuppressedLibraryCopy(reason) ? string.Empty : reason;
        }

        private static bool IsSuppressedLibraryCopy(string value)
        {
            if (string.IsNullOrEmpty(value))
                return true;
            return value == "—"
                || value.IndexOf("in development", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("در حال ساخت", StringComparison.Ordinal) >= 0;
        }

        private string ResolveCatalogCopy(string key, bool fa)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (key == "ui.skirmish.base_assault_map" || key == "ui.skirmish.theater.db.title" || key == "ui.skirmish.theater.db.subtitle")
            {
                SkirmishBattleLibraryArt.Copy("DB", fa, out string title, out string place);
                return key.EndsWith(".subtitle", StringComparison.Ordinal) ? place : title;
            }
            if (key == "ui.skirmish.city_crossroads_map" || key.StartsWith("ui.skirmish.theater.cc.", StringComparison.Ordinal))
            {
                SkirmishBattleLibraryArt.Copy("CC", fa, out string title, out string place);
                return key.EndsWith(".subtitle", StringComparison.Ordinal) ? place : title;
            }
            if (key == "ui.skirmish.industrial_basin_map" || key.StartsWith("ui.skirmish.theater.ib.", StringComparison.Ordinal))
            {
                SkirmishBattleLibraryArt.Copy("IB", fa, out string title, out string place);
                return key.EndsWith(".subtitle", StringComparison.Ordinal) ? place : title;
            }
            if (key.StartsWith("ui.skirmish.theater.", StringComparison.Ordinal))
            {
                string body = key.Substring("ui.skirmish.theater.".Length);
                int dot = body.IndexOf('.');
                string mapId = (dot >= 0 ? body.Substring(0, dot) : body).ToUpperInvariant();
                SkirmishBattleLibraryArt.Copy(mapId, fa, out string title, out string place);
                return key.EndsWith(".subtitle", StringComparison.Ordinal) ? place : title;
            }
            if (key.StartsWith("ui.skirmish.objective.", StringComparison.Ordinal))
            {
                SkirmishBattleLibraryArt.ObjectiveCopy(key.Substring("ui.skirmish.objective.".Length).ToUpperInvariant(), fa, out _, out string shortLabel);
                return shortLabel;
            }
            if (key.StartsWith("ui.skirmish.army.", StringComparison.Ordinal) || key.StartsWith("ui.skirmish.start.", StringComparison.Ordinal))
            {
                SkirmishBattleLibraryArt.ProfileCopy(key.Substring(key.LastIndexOf('.') + 1).ToUpperInvariant(), fa, out string label);
                return label;
            }
            if (key.StartsWith("ui.skirmish.locked.", StringComparison.Ordinal))
                return string.Empty;
            if (key.StartsWith("ui.skirmish.s", StringComparison.Ordinal) && TryFindEntry(ScenarioIdFromLocKey(key), out SkirmishBattleCatalogEntry entry))
            {
                if (key.EndsWith(".title", StringComparison.Ordinal) && !string.IsNullOrEmpty(entry.TitleEnglish))
                    return entry.TitleEnglish;
                if (fa && !string.IsNullOrEmpty(entry.DescriptionFarsi))
                    return entry.DescriptionFarsi;
                return entry.DescriptionEnglish ?? string.Empty;
            }
            return string.Empty;
        }

        private static string ScenarioIdFromLocKey(string key)
        {
            const string prefix = "ui.skirmish.";
            if (string.IsNullOrEmpty(key) || !key.StartsWith(prefix, StringComparison.Ordinal))
                return string.Empty;
            string body = key.Substring(prefix.Length);
            int dot = body.IndexOf('.');
            return (dot >= 0 ? body.Substring(0, dot) : body).ToUpperInvariant();
        }

        private string LibraryTitle(string scenarioId, bool fa)
        {
            if (TryFindEntry(scenarioId, out SkirmishBattleCatalogEntry entry))
            {
                if (!string.IsNullOrEmpty(entry.TitleKey))
                {
                    string localized = LibraryText(entry.TitleKey, fa);
                    if (!string.IsNullOrEmpty(localized))
                        return localized;
                }

                if (!string.IsNullOrEmpty(entry.TitleEnglish))
                    return entry.TitleEnglish;
            }

            return scenarioId ?? string.Empty;
        }

        private string LibraryPurpose(SkirmishBriefingState briefing, bool fa)
        {
            string described = LibraryText(briefing.DescriptionLocKey, fa);
            if (!string.IsNullOrEmpty(described))
                return described;
            string localized = LibraryText(briefing.PurposeLocKey, fa);
            if (!string.IsNullOrEmpty(localized))
                return localized;
            if (TryFindEntry(briefing.ScenarioId, out SkirmishBattleCatalogEntry entry))
            {
                if (fa && !string.IsNullOrEmpty(entry.DescriptionFarsi))
                    return entry.DescriptionFarsi;
                if (!string.IsNullOrEmpty(entry.DescriptionEnglish))
                    return entry.DescriptionEnglish;
            }

            return string.Empty;
        }

        private static bool MedalEarned(SkirmishMedalMask mask, SkirmishDifficultyId difficulty)
        {
            switch (difficulty)
            {
                case SkirmishDifficultyId.Recruit: return (mask & SkirmishMedalMask.Recruit) != 0;
                case SkirmishDifficultyId.Regular: return (mask & SkirmishMedalMask.Regular) != 0;
                case SkirmishDifficultyId.Veteran: return (mask & SkirmishMedalMask.Veteran) != 0;
                case SkirmishDifficultyId.Commander: return (mask & SkirmishMedalMask.Commander) != 0;
                default: return false;
            }
        }

        private static bool DifficultyOffered(SkirmishBriefingState briefing, SkirmishDifficultyId difficulty)
        {
            if (briefing.AvailableDifficulties == null)
                return false;
            for (int i = 0; i < briefing.AvailableDifficulties.Length; i++)
            {
                if (briefing.AvailableDifficulties[i] == difficulty)
                    return true;
            }

            return false;
        }

        private static bool TryFindTheaterPin(SkirmishTheaterState theater, string mapId, out SkirmishTheaterPinState pin)
        {
            pin = default;
            if (theater.Pins == null || string.IsNullOrEmpty(mapId))
                return false;
            for (int i = 0; i < theater.Pins.Length; i++)
            {
                if (string.Equals(theater.Pins[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                {
                    pin = theater.Pins[i];
                    return true;
                }
            }

            return false;
        }

        private static bool IsEmptyCard(SkirmishBattleCardState card) => string.IsNullOrEmpty(card.ScenarioId);

        private static string CardPreviewResource(SkirmishBattleCardState card)
        {
            if (!string.IsNullOrEmpty(card.MapId) && !string.IsNullOrEmpty(card.ObjectiveId))
                return SkirmishBattleLibraryArt.CellPreview(card.MapId, card.ObjectiveId, card.ArmyProfile, card.StartProfile);
            return LibraryArtKey(card.PreviewSpriteKey, card.MapId);
        }

        private static string LibraryArtKey(string key, string mapId)
        {
            if (string.IsNullOrEmpty(key))
                return string.IsNullOrEmpty(mapId) ? SkirmishBattleLibraryArt.TheaterPlate : SkirmishBattleLibraryArt.Plate(mapId);
            const string plate = "skirmish.plate.";
            const string preview = "skirmish.preview.";
            if (key.StartsWith(plate, StringComparison.OrdinalIgnoreCase))
                return SkirmishBattleLibraryArt.Plate(key.Substring(plate.Length));
            if (key.StartsWith(preview, StringComparison.OrdinalIgnoreCase))
                return SkirmishBattleLibraryArt.PreviewKey(key.Substring(preview.Length), mapId);
            return key;
        }

        private static bool TryRegion(string mapId, out Rect region, out Vector2 centre)
        {
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                if (!string.Equals(SkirmishBattleLibraryArt.MapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                region = SkirmishBattleLibraryArt.TheaterRegions[i];
                centre = SkirmishBattleLibraryArt.TheaterLabelCentres[i];
                return true;
            }

            region = default;
            centre = new Vector2(0.5f, 0.5f);
            return false;
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
            var state = rect.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
            state.Bind(graphic);
            return button;
        }

        private void TintPanel(GameObject target)
        {
            var graphic = target.GetComponent<V3GradientGraphic>() ?? target.AddComponent<V3GradientGraphic>();
            graphic.Configure(new Color(0.05f, 0.08f, 0.09f, 0.96f), new Color(0.02f, 0.04f, 0.05f, 0.96f), new Color(0.26f, 0.55f, 0.62f, 1f), 2f);
        }

        private static void TintButton(Button button, bool selected, bool playable, bool opaque = false)
        {
            if (button == null)
                return;
            SkirmishLibraryButtonStateView state = button.GetComponent<SkirmishLibraryButtonStateView>();
            if (state == null && button.targetGraphic is V3GradientGraphic graphic)
            {
                state = button.gameObject.AddComponent<SkirmishLibraryButtonStateView>();
                state.Bind(graphic);
            }

            if (state != null)
            {
                state.Apply(selected, playable || selected, opaque);
                return;
            }

            V3GradientGraphic fallback = button.targetGraphic as V3GradientGraphic;
            if (fallback == null)
                return;
            float body = opaque ? 1f : 0.94f;
            fallback.Configure(
                selected ? new Color(0.15f, 0.28f, 0.12f, 1f) : new Color(0.07f, 0.12f, 0.14f, body),
                selected ? new Color(0.05f, 0.12f, 0.06f, 1f) : new Color(0.02f, 0.05f, 0.06f, body),
                selected ? new Color(0.95f, 0.72f, 0.15f, 1f) : new Color(0.93f, 0.78f, 0.38f, 1f),
                selected ? 3f : 2f);
        }

        private Button CreateHitButton(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button))
                .GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = rect.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;
            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void PlaceLabelCentre(RectTransform rect, Vector2 centreFromTopLeft, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(centreFromTopLeft.x, 1f - centreFromTopLeft.y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
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
            text.fontSizeMin = 15f;
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
            binding.ConfigureFontBounds(15f, 22f);
            return text;
        }

        private void SetCopy(TMP_Text text, string value)
        {
            SetCopy(text, value, 14f, 22f);
        }

        private void SetCopy(TMP_Text text, string value, float min, float max)
        {
            if (text == null)
                return;
            var binding = text.GetComponent<V3LocalizedTextBindingView>() ?? text.gameObject.AddComponent<V3LocalizedTextBindingView>();
            if (string.IsNullOrEmpty(binding.LocalizationKey))
                binding.Configure("ui.skirmish.library_runtime", "BATTLE LIBRARY");
            binding.ConfigureFontBounds(min, max);
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

        private static void AttachAudio(Button button, UIAudioEventKind kind)
        {
            if (button == null)
                return;
            UIButtonAudioEventView audio = button.GetComponent<UIButtonAudioEventView>();
            if (audio == null)
                audio = button.gameObject.AddComponent<UIButtonAudioEventView>();
            audio.Configure(kind);
        }

        private static void AliasControl(Component owner, string controlName)
        {
            if (owner == null || string.IsNullOrEmpty(controlName))
                return;
            if (owner.transform.Find(controlName) != null)
                return;
            var alias = new GameObject(controlName, typeof(RectTransform));
            alias.transform.SetParent(owner.transform, false);
        }

        private static void AddSiblingShadow(RectTransform widget)
        {
            if (widget == null || widget.parent == null)
                return;
            var shadow = new GameObject("DepthShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            shadow.SetParent(widget.parent, false);
            shadow.anchorMin = widget.anchorMin;
            shadow.anchorMax = widget.anchorMax;
            shadow.pivot = widget.pivot;
            shadow.sizeDelta = widget.sizeDelta;
            shadow.anchoredPosition = widget.anchoredPosition + new Vector2(10f, -10f);
            shadow.SetSiblingIndex(widget.GetSiblingIndex());
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f);
            image.raycastTarget = false;
        }

        private void ApplyLibraryDepth()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            Transform preview = composition != null ? composition.Find("OperationPreview") : null;
            Transform plate = preview != null ? preview.Find("MapPreviewClip") : null;
            Transform rules = composition != null ? composition.Find("BaseAssaultRules") : null;
            EnsurePanelShadow(plate as RectTransform);
            if (_libraryRegions != null)
                _libraryRegions.SetAsLastSibling();
            if (_libraryPins != null)
                _libraryPins.SetAsLastSibling();
            if (_libraryGrid != null)
                _libraryGrid.SetAsLastSibling();
            for (int i = 0; i < 4; i++)
            {
                RaiseDepth(preview, "OrnamentCorner" + i);
                RaiseDepth(rules, "OrnamentCorner" + i);
            }
            if (_overallStrip != null)
                _overallStrip.SetAsLastSibling();
            EnsurePanelShadow(rules as RectTransform);
            EnsurePanelFrame(rules as RectTransform);
            if (rules != null && preview != null)
                rules.SetSiblingIndex(preview.GetSiblingIndex() + 1);
            EnsureFooterPlate(composition);
            Transform footerShadow = composition != null ? composition.Find("LibraryFooterShadow") : null;
            Transform footerPlate = composition != null ? composition.Find("LibraryFooterPlate") : null;
            if (footerShadow != null)
                footerShadow.SetAsLastSibling();
            if (footerPlate != null)
                footerPlate.SetAsLastSibling();
            Transform back = composition != null ? composition.Find("BackButton") : null;
            if (back != null)
                back.SetAsLastSibling();
            if (resetButton != null)
                resetButton.transform.SetAsLastSibling();
            if (randomizeSeedButton != null)
                randomizeSeedButton.transform.SetAsLastSibling();
            if (launchButton != null)
                launchButton.transform.SetAsLastSibling();
            RaiseDepth(composition, "SafeAreaEdgeLeft");
            RaiseDepth(composition, "SafeAreaEdgeRight");
            RaiseDepth(composition, "SafeAreaEdgeTop");
            RaiseDepth(composition, "SafeAreaEdgeBottom");
            RaiseDepth(composition, "SafeAreaNotch");
        }

        private static void RaiseDepth(Transform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
                child.SetAsLastSibling();
        }

        private static void EnsurePanelShadow(RectTransform panel)
        {
            if (panel == null || panel.parent == null || panel.parent.Find(panel.name + "Shadow") != null)
                return;
            var shadow = new GameObject(panel.name + "Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            shadow.SetParent(panel.parent, false);
            shadow.anchorMin = panel.anchorMin;
            shadow.anchorMax = panel.anchorMax;
            shadow.pivot = panel.pivot;
            shadow.anchoredPosition = panel.anchoredPosition + new Vector2(12f, -14f);
            shadow.sizeDelta = panel.sizeDelta;
            shadow.SetSiblingIndex(panel.GetSiblingIndex());
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = false;
        }

        private static void EnsurePanelFrame(RectTransform panel)
        {
            if (panel == null || panel.Find("DepthFrame") != null)
                return;
            var frame = new GameObject("DepthFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic)).GetComponent<RectTransform>();
            frame.SetParent(panel, false);
            Stretch(frame);
            frame.SetAsFirstSibling();
            V3GradientGraphic graphic = frame.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(new Color(0.05f, 0.08f, 0.09f, 0.94f), new Color(0.02f, 0.04f, 0.05f, 0.94f), new Color(0.93f, 0.78f, 0.38f, 1f), 3f);
        }

        private void EnsureFooterPlate(Transform composition)
        {
            if (composition == null || composition.Find("LibraryFooterPlate") != null)
                return;
            var plate = new GameObject("LibraryFooterPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic)).GetComponent<RectTransform>();
            plate.SetParent(composition, false);
            PlaceTopLeft(plate, 8f, 808f, 1656f, 124f);
            V3GradientGraphic graphic = plate.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(new Color(0.04f, 0.07f, 0.08f, 0.96f), new Color(0.02f, 0.03f, 0.04f, 0.96f), new Color(0.93f, 0.78f, 0.38f, 1f), 3f);
            var shadow = new GameObject("LibraryFooterShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            shadow.SetParent(composition, false);
            PlaceTopLeft(shadow, 16f, 820f, 1656f, 124f);
            shadow.SetSiblingIndex(plate.GetSiblingIndex());
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            image.raycastTarget = false;
        }

        private static string ReadProfileCredits()
        {
            try
            {
                int credits = SaveService.CreateDefault().LoadProfile().credits;
                if (credits < 0)
                    credits = 0;
                return credits.ToString("N0", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return "0";
            }
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
