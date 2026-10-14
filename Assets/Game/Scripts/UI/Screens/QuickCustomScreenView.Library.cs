using System;
using System.Collections.Generic;
using Game.Configs;
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
        private const float LibraryPinWidth = 150f;
        private const float LibraryPinHeight = 104f;
        private const float LibraryGridCardWidth = 124f;
        private const float LibraryGridCardHeight = 84f;
        private const float LibraryGridOriginX = 86f;
        private const float LibraryGridStride = 130f;

        private string _armyFilter = string.Empty;
        private string _startFilter = string.Empty;
        private bool _missingMedalOnly;
        private bool _filtersOpen;
        private bool _advancedOpen;
        private bool _librarySearchApplied;
        private bool _libraryChromeReady;
        private bool _libraryPresenting;
        private ISkirmishLibraryViewModel _libraryViewModel;

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
            _libraryViewModel?.Briefing?.SelectedDifficulty ?? SkirmishDifficultyId.Regular;

        public ISkirmishLibraryViewModel LibraryViewModel => _libraryViewModel;

        public void UseLibraryViewModel(ISkirmishLibraryViewModel viewModel)
        {
            if (_libraryViewModel != null)
                _libraryViewModel.Changed -= OnLibraryViewModelChanged;
            EnsureLibraryCards();
            _libraryViewModel = viewModel ?? new SkirmishLibraryStubViewModel(_catalogEntries);
            _libraryViewModel.Changed += OnLibraryViewModelChanged;
            _libraryViewModel.RestoreFromPrefs();
        }

        private SkirmishLibraryLevel LibraryLevel =>
            _libraryViewModel != null ? _libraryViewModel.Level : SkirmishLibraryLevel.Theater;

        private SkirmishBattlefieldLayout LibraryLayout =>
            _libraryViewModel?.Battlefield?.Layout ?? SkirmishBattlefieldLayout.Map;

        private string LibraryMapId => _libraryViewModel?.Theater?.SelectedMapId ?? "DB";

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

        private void EnsureLibraryViewModel()
        {
            EnsureLibraryCards();
            if (_libraryViewModel == null)
            {
                _libraryViewModel = new SkirmishLibraryStubViewModel(_catalogEntries);
                _libraryViewModel.Changed += OnLibraryViewModelChanged;
                _libraryViewModel.RestoreFromPrefs();
                return;
            }

            if (_libraryViewModel is SkirmishLibraryStubViewModel stub && stub.EntryCount != _catalogEntries.Count)
                stub.SetEntries(_catalogEntries);
        }

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
            _libraryViewModel.ApplyFilter(new SkirmishLibraryFilter
            {
                SearchText = _searchFilter,
                ArmyProfile = _armyFilter,
                StartProfile = _startFilter,
                MedalMissing = _missingMedalOnly ? SelectedLibraryDifficulty : (SkirmishDifficultyId?)null
            });
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
                SetActive(_progressTrack, !battlefield);
                SetActive(_statRow, battlefield);
                SetActive(_medalGrid, battlefield);
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
                SkirmishTheaterPinState selectedPin = FindTheaterPin(theater, field != null ? field.MapId : theater?.SelectedMapId);
                PresentProgress(selectedPin, fa);
                PresentBriefingPreview(briefing, selectedPin, battlefield);
                if (battlefield)
                    PresentMedalGrid(field);
                PresentFilterLabels(fa);
                PresentLibraryFooter(fa, battlefield);
                StyleCommandButtons(battlefield);
                PresentScreenHeading(theater, field, fa, battlefield);
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
                key = SkirmishBattleLibraryArt.Plate(field != null ? field.MapId : LibraryMapId);
                SkirmishTheaterPinState pin = FindTheaterPin(_libraryViewModel.Theater, field != null ? field.MapId : LibraryMapId);
                if (pin != null && !string.IsNullOrEmpty(pin.PlatePreviewKey))
                    key = pin.PlatePreviewKey;
            }

            Texture2D texture = Resources.Load<Texture2D>(key);
            if (texture != null)
                map.texture = texture;
            map.color = Color.white;
        }

        private void PresentRegions(SkirmishTheaterState theater, bool fa)
        {
            if (_libraryRegions == null)
                return;
            ClearChildren(_libraryRegions);
            _regionButtons.Clear();
            if (theater?.Pins == null)
                return;
            for (int i = 0; i < theater.Pins.Count; i++)
            {
                SkirmishTheaterPinState pin = theater.Pins[i];
                if (!TryRegion(pin.MapId, out Rect region))
                    continue;
                bool selected = string.Equals(pin.MapId, theater.SelectedMapId, StringComparison.OrdinalIgnoreCase);
                var button = CreateChromeButton(_libraryRegions, "Region_" + pin.MapId);
                PlaceRegion(button.GetComponent<RectTransform>(), region);
                TintButton(button, selected, true);
                var graphic = button.targetGraphic as V3GradientGraphic;
                if (graphic != null && !selected)
                    graphic.Configure(new Color(0.02f, 0.05f, 0.07f, 0.18f), new Color(0.02f, 0.04f, 0.05f, 0.28f), new Color(0.91f, 0.77f, 0.38f, 0.9f), 3f);
                string captured = pin.MapId;
                button.onClick.AddListener(() => SelectTheaterRegion(captured));
                var label = CreateLabel(button.transform, "Label", 16f);
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0f);
                label.rectTransform.pivot = new Vector2(0.5f, 0f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                label.rectTransform.sizeDelta = new Vector2(-12f, 48f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                SetCopy(label, LibraryText(pin.TitleLocKey, fa), 14f, 20f);
                var count = CreateLabel(button.transform, "Count", 16f);
                count.rectTransform.anchorMin = new Vector2(0f, 1f);
                count.rectTransform.anchorMax = new Vector2(1f, 1f);
                count.rectTransform.pivot = new Vector2(0.5f, 1f);
                count.rectTransform.anchoredPosition = new Vector2(0f, -8f);
                count.rectTransform.sizeDelta = new Vector2(-12f, 28f);
                count.alignment = TextAlignmentOptions.Center;
                count.isRightToLeftText = false;
                SetCopy(count, pin.WonCount + "/" + pin.PublishedCount, 14f, 20f);
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
            if (field?.Tabs == null)
                return;
            for (int i = 0; i < field.Tabs.Count; i++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[i];
                var button = CreateChromeButton(battleLibraryMapTabs, "ObjectiveTab_" + objective.ObjectiveId);
                var layout = button.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 72f;
                layout.minHeight = 64f;
                layout.flexibleWidth = 1f;
                bool selected = string.Equals(objective.ObjectiveId, field.SelectedObjectiveId, StringComparison.OrdinalIgnoreCase);
                TintButton(button, selected, false);
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
            if (field?.MapCards == null)
                return;
            for (int i = 0; i < field.MapCards.Count; i++)
            {
                if (field.MapCards[i] != null)
                    CreateBattleCard(_libraryPins, field.MapCards[i], true);
            }
        }

        private void PresentGrid(SkirmishBattlefieldState field, bool fa)
        {
            ClearChildren(_libraryGrid);
            if (field?.GridCards == null || field.Tabs == null)
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
                    PlaceTopLeft(headerIcon.rectTransform, LibraryGridOriginX + column * LibraryGridStride + 36f, 52f, 28f, 28f);
                    headerIcon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ArmyIcon(armies[armyIndex]));
                    headerIcon.preserveAspect = true;
                    headerIcon.raycastTarget = false;
                    SkirmishBattleLibraryArt.ProfileCopy(starts[startIndex], fa, out string startLabel);
                    var header = CreateLabel(_libraryGrid, "Column_" + column, 14f);
                    PlaceTopLeft(header.rectTransform, LibraryGridOriginX + column * LibraryGridStride, 80f, LibraryGridCardWidth, 22f);
                    header.alignment = TextAlignmentOptions.Center;
                    SetCopy(header, startLabel, 12f, 16f);
                }
            }

            int rows = field.Tabs.Count;
            for (int row = 0; row < rows; row++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[row];
                float y = 108f + row * (LibraryGridCardHeight + 6f);
                var rowIcon = new GameObject("RowIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                rowIcon.transform.SetParent(_libraryGrid, false);
                PlaceTopLeft(rowIcon.rectTransform, 8f, y + 8f, 36f, 36f);
                rowIcon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId));
                rowIcon.preserveAspect = true;
                rowIcon.raycastTarget = false;
                var rowLabel = CreateLabel(_libraryGrid, "Row_" + objective.ObjectiveId, 14f);
                PlaceTopLeft(rowLabel.rectTransform, 4f, y + 46f, LibraryGridOriginX - 10f, 32f);
                rowLabel.alignment = TextAlignmentOptions.Center;
                rowLabel.textWrappingMode = TextWrappingModes.Normal;
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string shortLabel);
                SetCopy(rowLabel, shortLabel, 12f, 16f);
                for (int column = 0; column < 6; column++)
                {
                    int index = row * 6 + column;
                    if (index >= field.GridCards.Count)
                        break;
                    SkirmishBattleCardState card = field.GridCards[index];
                    if (card == null)
                        continue;
                    var widget = CreateBattleCard(_libraryGrid, card, false, LibraryGridCardWidth, LibraryGridCardHeight);
                    PlaceTopLeft(widget, LibraryGridOriginX + column * LibraryGridStride, y, LibraryGridCardWidth, LibraryGridCardHeight);
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
                float width = 150f;
                PlaceTopLeft(button.GetComponent<RectTransform>(), 8f + i * (width + 8f), 4f, width, 44f);
                TintButton(button, string.Equals(mapId, selectedMapId, StringComparison.OrdinalIgnoreCase), true);
                var label = CreateLabel(button.transform, "Label", 15f);
                Stretch(label.rectTransform, 6f, 4f, 4f, 4f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                SkirmishBattleLibraryArt.Copy(mapId, fa, out string title, out _);
                SetCopy(label, title, 12f, 16f);
                string captured = mapId;
                button.onClick.AddListener(() => SelectGridBattlefield(captured));
            }
        }

        private void SelectGridBattlefield(string mapId)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.SelectMapCollection(mapId);
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
            MaskRect(rect);
            TintButton(button, card.IsSelected, card.Access == SkirmishCardAccess.Playable);
            if (card.Access == SkirmishCardAccess.Planned || card.Access == SkirmishCardAccess.Unavailable)
                Dim(button);
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(button.transform, false);
            Stretch(preview.rectTransform, 3f, 3f, 3f, 34f);
            preview.texture = Resources.Load<Texture2D>(card.PreviewSpriteKey);
            preview.color = preview.texture != null ? Color.white : new Color(0.12f, 0.16f, 0.18f, 1f);
            preview.raycastTarget = false;
            var armyIcon = new GameObject("ArmyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            armyIcon.transform.SetParent(button.transform, false);
            PlaceTopLeft(armyIcon.rectTransform, 4f, 4f, 22f, 22f);
            armyIcon.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.ArmyIcon(card.ArmyProfile));
            armyIcon.preserveAspect = true;
            armyIcon.raycastTarget = false;
            var id = CreateLabel(button.transform, "ScenarioId", 13f);
            PlaceTopLeft(id.rectTransform, 28f, 2f, width - 34f, 20f);
            id.alignment = TextAlignmentOptions.MidlineLeft;
            id.isRightToLeftText = false;
            id.text = card.ScenarioId ?? string.Empty;
            var title = CreateLabel(button.transform, "Title", 14f);
            PlaceTopLeft(title.rectTransform, 4f, height - 32f, width - 8f, 28f);
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.overflowMode = TextOverflowModes.Overflow;
            SetCopy(title, LibraryText(card.ArmyLocKey, fa), 12f, 16f);
            AddMedalIcons(button.transform, card.Medals, 4f, height - 54f);
            if (card.Access == SkirmishCardAccess.Playable && card.Medals == SkirmishMedalMask.None)
            {
                var badge = new GameObject("NewBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                badge.transform.SetParent(button.transform, false);
                PlaceTopLeft(badge.rectTransform, width - 36f, 2f, 32f, 22f);
                badge.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.NewBadge);
                badge.preserveAspect = true;
                badge.raycastTarget = false;
                var badgeLabel = CreateLabel(button.transform, "NewLabel", 11f);
                PlaceTopLeft(badgeLabel.rectTransform, width - 36f, 2f, 32f, 18f);
                badgeLabel.alignment = TextAlignmentOptions.Center;
                SetCopy(badgeLabel, fa ? "نو" : "NEW", 10f, 12f);
            }
            if (card.Access == SkirmishCardAccess.NotOwned)
            {
                var lockLabel = CreateLabel(button.transform, "Lock", 13f);
                PlaceTopLeft(lockLabel.rectTransform, 4f, 28f, width - 8f, 20f);
                lockLabel.alignment = TextAlignmentOptions.Center;
                SetCopy(lockLabel, fa ? "قفل" : "LOCK", 11f, 14f);
            }

            string captured = card.ScenarioId;
            button.onClick.AddListener(() => OnLibraryCard(captured));
            return rect;
        }

        private void AddMedalIcons(Transform parent, SkirmishMedalMask medals, float x, float y)
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
                image.sprite = Resources.Load<Sprite>(SkirmishBattleLibraryArt.Medal(MedalEarned(medals, difficulties[i]), difficulties[i]));
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }

        private void PresentBriefingChrome(SkirmishTheaterState theater, SkirmishBattlefieldState field, SkirmishBriefingState briefing, bool fa, bool battlefield)
        {
            SkirmishTheaterPinState pin = FindTheaterPin(theater, field != null ? field.MapId : theater?.SelectedMapId);
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            TMP_Text title = rules != null ? rules.GetComponentInChildren<TMP_Text>(true) : null;
            Transform titleTransform = FindDeep(rules, "Title");
            if (titleTransform != null)
                title = titleTransform.GetComponent<TMP_Text>();
            if (battlefield && briefing != null)
                SetCopy(title, LibraryText(briefing.ArmyLocKey, fa) + " · " + LibraryText(briefing.StartLocKey, fa));
            else if (pin != null)
                SetCopy(title, LibraryText(pin.TitleLocKey, fa));

            if (_recommendedPanel != null)
            {
                TMP_Text heading = _recommendedPanel.Find("Heading")?.GetComponent<TMP_Text>();
                TMP_Text body = _recommendedPanel.Find("Body")?.GetComponent<TMP_Text>();
                SetCopy(heading, fa ? "پیشنهاد بعدی" : "RECOMMENDED NEXT");
                if (theater == null || string.IsNullOrEmpty(theater.RecommendedScenarioId))
                    SetCopy(body, fa ? "هنوز پیشنهادی نیست." : "No recommendation yet.");
                else
                    SetCopy(body, LibraryTitle(theater.RecommendedScenarioId, fa));
            }

            PresentMedalRow(briefing, pin, fa, battlefield);
            PresentDifficulty(briefing, fa);
            bool notOwned = briefing != null && briefing.Access == SkirmishCardAccess.NotOwned;
            if (_libraryStoreButton != null)
                _libraryStoreButton.gameObject.SetActive(notOwned);
            SetActive(FindDeep(rules, "Opponent"), _advancedOpen);
            SetActive(FindDeep(rules, "Objective"), _advancedOpen);
            SetActive(FindDeep(rules, "Roster"), _advancedOpen);
            SetActive(FindDeep(rules, "Economy"), _advancedOpen);
            SetActive(FindDeep(rules, "Intel"), _advancedOpen);
            SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/SeedMetric"), _advancedOpen);

            SkirmishBattleCardState selectedCard = null;
            if (field?.MapCards != null)
            {
                for (int i = 0; i < field.MapCards.Count; i++)
                {
                    if (field.MapCards[i] != null && field.MapCards[i].IsSelected)
                        selectedCard = field.MapCards[i];
                }
            }
            if (battlefield)
                PresentStatTiles(briefing, selectedCard, fa);
            if (scenarioDescription != null && !battlefield && pin != null)
                SetCopy(scenarioDescription, LibraryText(pin.SubtitleLocKey, fa), 15f, 22f);
            else if (scenarioDescription != null && briefing != null)
            {
                string purpose = MixedText(LibraryPurpose(briefing, fa), fa);
                string reason = MixedText(LibraryText(briefing.LockedReasonLocKey, fa), fa);
                SetCopy(scenarioDescription, string.IsNullOrEmpty(reason) ? purpose : purpose + "\n" + reason, 15f, 22f);
            }

            if (launchButton != null && !_savedMatchAvailable)
                launchButton.interactable = !battlefield || (briefing != null && briefing.CanLaunch);
        }

        private void PresentMedalRow(SkirmishBriefingState briefing, SkirmishTheaterPinState pin, bool fa, bool battlefield)
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
            SkirmishMedalTotals totals = pin != null ? pin.Medals : default;
            int[] counts = { totals.Recruit, totals.Regular, totals.Veteran, totals.Commander };
            SkirmishMedalMask mask = briefing != null ? briefing.Medals : SkirmishMedalMask.None;
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
            SkirmishDifficultyId selected = briefing != null ? briefing.SelectedDifficulty : SelectedLibraryDifficulty;
            for (int i = 0; i < difficulties.Length; i++)
            {
                Transform slot = _difficultyRow.Find("DifficultyChoice_" + difficulties[i]);
                if (slot == null)
                    continue;
                var button = slot.GetComponent<Button>();
                bool offered = briefing == null || DifficultyOffered(briefing, difficulties[i]);
                TintButton(button, offered && difficulties[i] == selected, offered);
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                TMP_Text choice = slot.GetComponentInChildren<TMP_Text>(true);
                if (choice != null)
                {
                    choice.fontSize = 18f;
                    choice.fontSizeMin = 14f;
                    choice.fontSizeMax = 22f;
                }
                SetCopy(choice, label, 14f, 22f);
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
            TintButton(toggle.Find("MapChoice")?.GetComponent<Button>(), LibraryLayout == SkirmishBattlefieldLayout.Map, false);
            TintButton(toggle.Find("GridChoice")?.GetComponent<Button>(), LibraryLayout == SkirmishBattlefieldLayout.Grid, false);
        }

        private void PresentScreenHeading(SkirmishTheaterState theater, SkirmishBattlefieldState field, bool fa, bool battlefield)
        {
            TMP_Text screenTitle = transform.Find("SkirmishSetupComposition/ScreenTitlePanel/ScreenTitle")?.GetComponent<TMP_Text>();
            SkirmishTheaterPinState headingPin = FindTheaterPin(theater, field != null ? field.MapId : theater?.SelectedMapId);
            string mapTitle = headingPin != null ? LibraryText(headingPin.TitleLocKey, fa) : (fa ? "صحنه نبرد" : "THEATRE");
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
            if (!string.IsNullOrEmpty(catalogId))
            {
                SelectScenarioId(catalogId);
                return;
            }

            _libraryViewModel.ZoomIn();
            _mapFilter = LibraryMapId;
            PresentScenarioChoices();
        }

        private void OnLibraryCard(string catalogId) => SelectScenarioId(catalogId);

        private void SelectTheaterRegion(string mapId)
        {
            EnsureLibraryViewModel();
            _libraryViewModel.BackToTheater();
            _libraryViewModel.SelectMapCollection(mapId);
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
                PlaceTopLeft(button.GetComponent<RectTransform>(), 8f + i * 138f, 4f, 130f, 56f);
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
            if (plate is RectTransform plateRect)
            {
                plateRect.anchorMin = new Vector2(0f, 0f);
                plateRect.anchorMax = new Vector2(1f, 1f);
                plateRect.pivot = new Vector2(0f, 1f);
                plateRect.offsetMin = new Vector2(8f, 68f);
                plateRect.offsetMax = new Vector2(-8f, -64f);
                MaskRect(plateRect);
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
                search.anchorMin = search.anchorMax = new Vector2(0f, 0f);
                search.pivot = new Vector2(0f, 0f);
                search.anchoredPosition = new Vector2(12f, 8f);
                search.sizeDelta = new Vector2(420f, 48f);
                foreach (TMP_Text fieldText in battleLibrarySearch.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (fieldText.rectTransform == search)
                        continue;
                    Stretch(fieldText.rectTransform, 12f, 8f, 6f, 6f);
                    fieldText.textWrappingMode = TextWrappingModes.NoWrap;
                    fieldText.overflowMode = TextOverflowModes.Ellipsis;
                    fieldText.fontSize = 18f;
                }
            }

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
            if (_medalRow != null)
                PlaceTopLeft(_medalRow, 8f, 8f, 560f, 96f);
            if (_difficultyRow != null)
                PlaceTopLeft(_difficultyRow, 8f, 556f, 560f, 60f);
            if (_libraryStoreButton != null)
                PlaceTopLeft(_libraryStoreButton.GetComponent<RectTransform>(), 16f, 624f, 280f, 48f);
            if (_recommendedPanel != null)
                PlaceTopLeft(_recommendedPanel, 8f, 176f, 560f, 96f);
            if (_progressTrack != null)
                PlaceTopLeft(_progressTrack, 16f, 112f, 540f, 52f);
            if (_briefingPreview != null)
                PlaceTopLeft(_briefingPreview.rectTransform, 16f, 108f, 540f, 112f);
            if (_statRow != null)
                PlaceTopLeft(_statRow, 16f, 478f, 540f, 72f);
            if (_medalGrid != null)
                PlaceTopLeft(_medalGrid, 16f, 314f, 540f, 156f);
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            if (scenarioDescription != null && rules != null)
            {
                scenarioDescription.transform.SetParent(rules, false);
                var purposeBinding = scenarioDescription.GetComponent<V3LocalizedTextBindingView>()
                    ?? scenarioDescription.gameObject.AddComponent<V3LocalizedTextBindingView>();
                if (string.IsNullOrEmpty(purposeBinding.LocalizationKey) ||
                    !purposeBinding.LocalizationKey.StartsWith("ui.skirmish.", StringComparison.Ordinal))
                    purposeBinding.Configure("ui.skirmish.library_purpose", "Same map. Seed changes AI choices.");
                float purposeTop = LibraryLevel == SkirmishLibraryLevel.Battlefield ? 224f : 284f;
                PlaceTopLeft(scenarioDescription.rectTransform, 16f, purposeTop, 540f, 86f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Overflow;
            }
            if (_libraryFilters is RectTransform filters)
                PlaceTopLeft(filters, 24f, 180f, 460f, 280f);
            if (_libraryGrid is RectTransform grid)
            {
                grid.anchorMin = new Vector2(0f, 0f);
                grid.anchorMax = new Vector2(1f, 1f);
                grid.offsetMin = new Vector2(8f, 68f);
                grid.offsetMax = new Vector2(-8f, -64f);
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

        private static string LibraryText(string key, bool fa)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            string localized = UiShellRuntimeGateway.Localization.Get(key, string.Empty);
            if (!string.IsNullOrEmpty(localized))
                return localized;
            return SkirmishBattleLibraryArt.Fallback(key, fa);
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
            if (briefing?.AvailableDifficulties == null)
                return true;
            for (int i = 0; i < briefing.AvailableDifficulties.Count; i++)
            {
                if (briefing.AvailableDifficulties[i] == difficulty)
                    return true;
            }

            return false;
        }

        private static SkirmishTheaterPinState FindTheaterPin(SkirmishTheaterState theater, string mapId)
        {
            if (theater?.Pins == null)
                return null;
            for (int i = 0; i < theater.Pins.Count; i++)
            {
                if (string.Equals(theater.Pins[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    return theater.Pins[i];
            }

            return null;
        }

        private static bool TryRegion(string mapId, out Rect region)
        {
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                if (!string.Equals(SkirmishBattleLibraryArt.MapIds[i], mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                region = SkirmishBattleLibraryArt.TheaterRegions[i];
                return true;
            }

            region = default;
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
