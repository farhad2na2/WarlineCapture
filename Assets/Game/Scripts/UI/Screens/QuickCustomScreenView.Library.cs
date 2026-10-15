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
        private const float LibraryFontFloor = 22f;
        private const float LibraryPinWidth = 252f;
        private const float LibraryPinHeight = 168f;
        private const float LibraryGridCardWidth = 146f;
        private const float LibraryGridCardHeight = 108f;
        private const float LibraryGridOriginX = 92f;
        private const float LibraryGridStride = 154f;

        private string _armyFilter = string.Empty;
        private string _startFilter = string.Empty;
        private bool _missingMedalOnly;
        private bool _filtersOpen;
        private bool _favouritesLit;
        private Button _libraryFavouritesButton;
        private Button _libraryAdvancedButton;
        private static Sprite _libraryMedallion;
        private static Sprite _zoneGlow;
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
            _libraryFavouritesButton = EnsureFooterUtility(composition, "LibraryFavouritesButton");
            _libraryAdvancedButton = EnsureFooterUtility(composition, "LibraryAdvancedButton");
            EnsureStoreButton(rules);
            EnsureModeToggle(preview);
            EnsureFilterPanel(_libraryFilters);
            EnsureLibraryPass(composition, preview, rules);
            PlaceLibraryChrome();
            _libraryChromeReady = true;
        }

        public void LockBattleLibraryMedals()
        {
            LockMedalArtwork();
        }

        public void FocusTheaterOnMap(string mapId)
        {
            if (!baseAssaultPreset || string.IsNullOrEmpty(mapId))
                return;
            EnsureLibraryViewModel();
            _mapFilter = mapId;
            _libraryViewModel.SelectMapCollection(mapId);
            _libraryViewModel.BackToTheater();
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
                    _libraryViewModel.SelectScenarioId("S001");
                    break;
                case "grid":
                    _libraryViewModel.SelectMapCollection(string.IsNullOrEmpty(LibraryMapId) ? "DB" : LibraryMapId);
                    _libraryViewModel.ZoomIn();
                    _libraryViewModel.SetLayout(SkirmishBattlefieldLayout.Grid);
                    _libraryViewModel.SelectScenarioId("S001");
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
                SetActive(_progressTrack, !battlefield);
                SetActive(_statRow, battlefield);
                SetActive(_medalGrid, false);
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
                PresentWallet();
                PresentScreenHeading(theater, field, fa, battlefield);
                ApplyLibraryDepth();
                ComposeBriefingColumn(battlefield);
                PresentZoomHint(fa, battlefield);
                EnforceLibraryFontFloor();
                FinishLibraryText();
                RemoveBlankPanelImages();
                PlaceLibraryHeader();
                MirrorLibraryPanels();
                ClampStrayMedalBars();
                RaiseLaunchTransition();
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
            if (!battlefield)
            {
                Texture2D collage = Resources.Load<Texture2D>(SkirmishBattleLibraryArt.TheaterPlate);
                map.texture = collage != null ? collage : SkirmishLibraryPreviewAssets.Placeholder();
                map.color = Color.white;
            }
            else
            {
                string mapId = string.IsNullOrEmpty(field.MapId) ? LibraryMapId : field.MapId;
                string key = TryFindTheaterPin(_libraryViewModel.Theater, mapId, out SkirmishTheaterPinState pin)
                    ? pin.PlatePreviewKey
                    : null;
                RectTransform clip = map.rectTransform.parent as RectTransform;
                if (!TryFrameSize(clip, out float plateWidth, out float plateHeight))
                {
                    plateWidth = 1064f;
                    plateHeight = 559f;
                }

                ApplyBoundPreview(map, key, plateWidth, plateHeight);
            }
            if (!battlefield)
                map.uvRect = new Rect(0f, 0f, 1f, 1f);
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
            RawImage map = transform.Find("SkirmishSetupComposition/OperationPreview/MapPreviewClip/MapPreview")
                ?.GetComponent<RawImage>();
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            if (battlefield || theater.Pins == null)
            {
                if (map != null)
                    map.enabled = true;
                Transform previewRoot = map != null && map.transform.parent != null ? map.transform.parent.parent : null;
                SetActive(previewRoot != null ? previewRoot.Find("TheaterLabels") : null, false);
                return;
            }

            var ground = new GameObject("TheaterGround", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            ground.transform.SetParent(_libraryRegions, false);
            Stretch(ground.rectTransform);
            ground.color = new Color(0.07f, 0.07f, 0.06f, 1f);
            ground.raycastTarget = false;

            var zones = new GameObject("TheaterZones", typeof(RectTransform)).GetComponent<RectTransform>();
            zones.SetParent(_libraryRegions, false);
            Stretch(zones);
            Texture2D painted = SkirmishLibraryPreviewAssets.Load("skirmish.zone.theater");
            bool anyZone = painted != null;
            if (painted != null)
            {
                var raw = new GameObject("TheaterPaint", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                raw.transform.SetParent(zones, false);
                Stretch(raw.rectTransform);
                raw.texture = painted;
                raw.color = Color.white;
                raw.raycastTarget = false;
                RectTransform plate = map != null ? map.rectTransform.parent as RectTransform : null;
                float plateWidth = plate != null && plate.rect.width > 2f ? plate.rect.width : 1024f;
                float plateHeight = plate != null && plate.rect.height > 2f ? plate.rect.height : 540f;
                CoverCrop(raw, plateWidth, plateHeight);
            }
            else
            {
                for (int i = 0; i < theater.Pins.Length; i++)
                {
                    SkirmishTheaterPinState pin = theater.Pins[i];
                    Texture2D feather = SkirmishLibraryPreviewAssets.Load("skirmish.zone." + pin.MapId);
                    if (feather == null)
                        continue;
                    anyZone = true;
                    var raw = new GameObject("Zone_" + pin.MapId, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                    raw.transform.SetParent(zones, false);
                    Stretch(raw.rectTransform);
                    raw.texture = feather;
                    raw.color = Color.white;
                    raw.raycastTarget = false;
                    RectTransform plate = map != null ? map.rectTransform.parent as RectTransform : null;
                    float plateWidth = plate != null && plate.rect.width > 2f ? plate.rect.width : 1024f;
                    float plateHeight = plate != null && plate.rect.height > 2f ? plate.rect.height : 540f;
                    CoverCrop(raw, plateWidth, plateHeight);
                }
            }

            if (map != null)
                map.enabled = !anyZone;

            var hits = new GameObject("TheaterHits", typeof(RectTransform)).GetComponent<RectTransform>();
            hits.SetParent(_libraryRegions, false);
            Stretch(hits);
            RectTransform plateRect = map != null ? map.rectTransform.parent as RectTransform : null;
            RectTransform labels = EnsureTheaterLabelHost(plateRect);
            ClearChildren(labels);
            for (int i = 0; i < SkirmishBattleLibraryArt.MapIds.Length; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                if (!TryRegion(mapId, out Rect region, out Vector2 centre))
                    continue;
                bool selected = string.Equals(mapId, theater.SelectedMapId, StringComparison.OrdinalIgnoreCase);
                Color zone = RegionZoneColor(mapId);
                int won = 0;
                int published = 0;
                if (theater.Pins != null)
                {
                    for (int pinIndex = 0; pinIndex < theater.Pins.Length; pinIndex++)
                    {
                        if (!string.Equals(theater.Pins[pinIndex].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        won = theater.Pins[pinIndex].WonCount;
                        published = theater.Pins[pinIndex].PublishedCount;
                        break;
                    }
                }

                var button = CreateHitButton(hits, SkirmishLibraryControlNames.TheaterPin(mapId));
                PlaceRegion(button.GetComponent<RectTransform>(), region);
                if (button.targetGraphic is Image hit)
                    hit.color = new Color(0f, 0f, 0f, 0f);
                string captured = mapId;
                button.onClick.AddListener(() => SelectTheaterRegion(captured));
                _regionButtons.Add(button);

                float pinSize = selected ? 58f : 40f;
                if (selected)
                {
                    var glow = new GameObject("ZoneGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    glow.transform.SetParent(labels, false);
                    glow.sprite = ZoneGlowSprite();
                    glow.preserveAspect = true;
                    glow.color = new Color(zone.r, zone.g, zone.b, 0.92f);
                    glow.raycastTarget = false;
                    PlaceLabelCentre(glow.rectTransform, centre + new Vector2(0f, -0.012f), 92f, 92f);
                }

                var pinMark = new GameObject("Pin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                pinMark.transform.SetParent(labels, false);
                pinMark.sprite = LibraryMedallion();
                pinMark.preserveAspect = true;
                pinMark.color = zone;
                pinMark.raycastTarget = false;
                PlaceLabelCentre(pinMark.rectTransform, centre + (selected ? new Vector2(0f, -0.018f) : Vector2.zero), pinSize, pinSize);
                if (selected)
                    pinMark.transform.SetAsLastSibling();
                var backing = new GameObject("ZonePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                backing.transform.SetParent(labels, false);
                backing.color = new Color(0.02f, 0.03f, 0.04f, 0.92f);
                backing.raycastTarget = false;
                PlaceLabelCentre(backing.rectTransform, centre + new Vector2(0f, 0.09f), 300f, 72f);
                var name = CreateLabel(labels, "ZoneName", 18f);
                PlaceLabelCentre(name.rectTransform, centre + new Vector2(0f, 0.075f), 284f, 36f);
                name.alignment = TextAlignmentOptions.Center;
                name.isRightToLeftText = fa;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Overflow;
                name.fontStyle = FontStyles.Bold;
                name.outlineWidth = 0f;
                name.color = new Color(1f, 0.98f, 0.92f, 1f);
                SetCopy(name, MapDisplayTitle(mapId, fa), 16f, 26f);
                name.alignment = TextAlignmentOptions.Center;
                name.isRightToLeftText = fa;
                name.overflowMode = TextOverflowModes.Overflow;
                name.fontStyle = FontStyles.Bold;
                name.outlineWidth = 0f;
                name.color = selected ? Color.white : new Color(1f, 0.98f, 0.92f, 1f);
                var count = CreateLabel(labels, "ZoneCount", 16f);
                PlaceLabelCentre(count.rectTransform, centre + new Vector2(0f, 0.145f), 160f, 28f);
                count.alignment = TextAlignmentOptions.Center;
                count.isRightToLeftText = false;
                count.fontStyle = FontStyles.Bold;
                count.outlineWidth = 0f;
                count.color = Color.white;
                count.overflowMode = TextOverflowModes.Overflow;
                SetCopy(count, won + "/" + published, 16f, 24f);
                count.isRightToLeftText = false;
                count.overflowMode = TextOverflowModes.Overflow;
                count.fontStyle = FontStyles.Bold;
                count.outlineWidth = 0f;
                count.color = Color.white;
            }
        }

        private RectTransform EnsureTheaterLabelHost(RectTransform plate)
        {
            Transform preview = plate != null ? plate.parent : _libraryRegions;
            if (preview == null)
                return _libraryRegions;
            Transform existing = preview.Find("TheaterLabels");
            RectTransform host = existing as RectTransform;
            if (host == null)
            {
                host = new GameObject("TheaterLabels", typeof(RectTransform)).GetComponent<RectTransform>();
                host.SetParent(preview, false);
            }

            if (plate != null)
            {
                host.anchorMin = plate.anchorMin;
                host.anchorMax = plate.anchorMax;
                host.pivot = plate.pivot;
                host.offsetMin = plate.offsetMin;
                host.offsetMax = plate.offsetMax;
            }
            else
            {
                Stretch(host);
            }

            host.gameObject.SetActive(true);
            host.SetAsLastSibling();
            return host;
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
                    float columnX = LibraryGridOriginX + column * LibraryGridStride;
                    var headerIcon = new GameObject("ColumnIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    headerIcon.transform.SetParent(_libraryGrid, false);
                    PlaceTopLeft(headerIcon.rectTransform, columnX + 2f, 56f, 16f, 16f);
                    ShowIcon(headerIcon, SkirmishBattleLibraryArt.ArmyIcon(armies[armyIndex]), Color.white);
                    SkirmishBattleLibraryArt.ProfileCopy(armies[armyIndex], fa, out string armyLabel);
                    SkirmishBattleLibraryArt.ProfileCopy(starts[startIndex], fa, out string startLabel);
                    var header = CreateLabel(_libraryGrid, "Column_" + column, 13f);
                    PlaceTopLeft(header.rectTransform, columnX + 18f, 54f, LibraryGridCardWidth - 20f, 42f);
                    header.alignment = TextAlignmentOptions.Center;
                    header.textWrappingMode = TextWrappingModes.Normal;
                    header.overflowMode = TextOverflowModes.Ellipsis;
                    SetCopy(header, armyLabel + " " + startLabel, 12f, 15f);
                    header.textWrappingMode = TextWrappingModes.Normal;
                    header.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            int rows = field.Tabs.Length;
            for (int row = 0; row < rows; row++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[row];
                float y = 100f + row * (LibraryGridCardHeight + 8f);
                var rowIcon = new GameObject("RowIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                rowIcon.transform.SetParent(_libraryGrid, false);
                PlaceTopLeft(rowIcon.rectTransform, 8f, y + 8f, 36f, 36f);
                ShowIcon(rowIcon, SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId), Color.white);
                SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string rowName);
                var rowLabel = CreateLabel(_libraryGrid, "Row_" + objective.ObjectiveId, 14f);
                PlaceTopLeft(rowLabel.rectTransform, 2f, y + 36f, LibraryGridOriginX - 8f, 40f);
                rowLabel.alignment = TextAlignmentOptions.Center;
                rowLabel.textWrappingMode = TextWrappingModes.Normal;
                rowLabel.overflowMode = TextOverflowModes.Ellipsis;
                SetCopy(rowLabel, rowName, 13f, 16f);
                var rowCount = CreateLabel(_libraryGrid, "RowCount_" + objective.ObjectiveId, 13f);
                PlaceTopLeft(rowCount.rectTransform, 4f, y + 62f, LibraryGridOriginX - 10f, 18f);
                rowCount.alignment = TextAlignmentOptions.Center;
                rowCount.isRightToLeftText = false;
                rowCount.textWrappingMode = TextWrappingModes.NoWrap;
                SetCopy(rowCount, objective.WonCount + "/" + objective.PublishedCount, 16f, 20f);
                for (int column = 0; column < 6; column++)
                {
                    int index = row * 6 + column;
                    if (index >= field.GridCards.Length)
                        break;
                    SkirmishBattleCardState card = field.GridCards[index];
                    RectTransform widget = IsEmptyCard(card)
                        ? CreateEmptyGridCell(_libraryGrid, objective.ObjectiveId, armies[column / 2], starts[column % 2], card.PreviewSpriteKey)
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
            int tabCount = SkirmishBattleLibraryArt.MapIds.Length;
            float span = _libraryGrid.rect.width > 240f ? _libraryGrid.rect.width - 16f : 1008f;
            const float tabGap = 10f;
            float tabWidth = tabCount > 0 ? (span - tabGap * (tabCount - 1)) / tabCount : 160f;
            for (int i = 0; i < tabCount; i++)
            {
                string mapId = SkirmishBattleLibraryArt.MapIds[i];
                var button = CreateChromeButton(_libraryGrid, "BattlefieldTab_" + mapId);
                PlaceTopLeft(button.GetComponent<RectTransform>(), 8f + i * (tabWidth + tabGap), 2f, tabWidth, 52f);
                TintButton(button, string.Equals(mapId, selectedMapId, StringComparison.OrdinalIgnoreCase), true);
                var label = CreateLabel(button.transform, "Label", 16f);
                Stretch(label.rectTransform, 6f, 6f, 2f, 2f);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Ellipsis;
                SetCopy(label, MapDisplayTitle(mapId, fa), 12f, 18f);
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.textWrappingMode = TextWrappingModes.Normal;
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
            float footer = pin ? 72f : 56f;
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
            PlaceFittedPreview(preview, previewKey, 3f, 3f, width - 6f, height - 3f - footer);
            preview.transform.SetAsFirstSibling();
            var scrim = new GameObject("FooterScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            scrim.transform.SetParent(button.transform, false);
            scrim.rectTransform.anchorMin = new Vector2(0f, 0f);
            scrim.rectTransform.anchorMax = new Vector2(1f, 0f);
            scrim.rectTransform.pivot = new Vector2(0.5f, 0f);
            scrim.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            scrim.rectTransform.sizeDelta = new Vector2(-6f, footer - 4f);
            scrim.color = new Color(0.04f, 0.06f, 0.07f, 0.9f);
            scrim.raycastTarget = false;
            var badge = new GameObject("ScenarioBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            badge.transform.SetParent(button.transform, false);
            PlaceTopLeft(badge.rectTransform, 4f, 4f, 58f, 22f);
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
            var title = CreateLabel(button.transform, "Title", 15f);
            PlaceTopLeft(title.rectTransform, 6f, height - footer + 1f, width - 12f, 20f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            SetCopy(title, objectiveLabel, 13f, 16f);
            var subtitle = CreateLabel(button.transform, "Subtitle", 12f);
            float subtitleTop = height - footer + (pin ? 20f : 16f);
            float subtitleHeight = pin ? 30f : 24f;
            float subtitleBottom = subtitleTop + subtitleHeight;
            if (subtitleBottom > height - 4f)
                subtitleHeight = Mathf.Max(18f, height - 4f - subtitleTop);
            PlaceTopLeft(subtitle.rectTransform, 6f, subtitleTop, width - 12f, subtitleHeight);
            subtitle.alignment = TextAlignmentOptions.TopLeft;
            subtitle.textWrappingMode = TextWrappingModes.Normal;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 11f;
            subtitle.fontSizeMax = 14f;
            subtitle.color = new Color(0.86f, 0.9f, 0.88f, 1f);
            string subtitleCopy = string.IsNullOrEmpty(startLabel) ? armyLabel : armyLabel + " · " + startLabel;
            SetCopy(subtitle, subtitleCopy, 11f, 14f);
            subtitle.textWrappingMode = TextWrappingModes.Normal;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;
            if (card.IsRecommended)
            {
                var recommended = CreateLabel(button.transform, "Recommended", 12f);
                PlaceTopLeft(recommended.rectTransform, 4f, 2f, width - 8f, 16f);
                recommended.alignment = TextAlignmentOptions.MidlineLeft;
                recommended.textWrappingMode = TextWrappingModes.NoWrap;
                recommended.color = new Color(0.95f, 0.82f, 0.28f, 1f);
                SetCopy(recommended, fa ? "پیشنهاد" : "NEXT", 11f, 13f);
            }
            float medal = pin ? 22f : 18f;
            AddMedalIcons(button.transform, card.Medals, 4f, height - footer - medal - 2f, medal);
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

        private RectTransform CreateEmptyGridCell(Transform parent, string objectiveId, string army, string start, string previewKey)
        {
            var rect = new GameObject("EmptyCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var plate = rect.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.07f, 0.08f, 0.92f);
            plate.raycastTarget = false;
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(rect, false);
            PlaceFittedPreview(preview, ScenarioPreviewKey(previewKey), 3f, 3f, LibraryGridCardWidth - 6f, LibraryGridCardHeight - 3f - 22f);
            preview.transform.SetAsFirstSibling();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out _, out string objectiveLabel);
            SkirmishBattleLibraryArt.ProfileCopy(army, fa, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(start, fa, out string startLabel);
            string detail = string.IsNullOrEmpty(startLabel) ? armyLabel : armyLabel + " · " + startLabel;
            string line = string.IsNullOrEmpty(detail) ? objectiveLabel : objectiveLabel + " · " + detail;
            var title = CreateLabel(rect, "Title", 13f);
            PlaceTopLeft(title.rectTransform, 4f, 2f, 138f, 18f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            KeepSingleLine(title, 11f, 14f);
            SetCopy(title, line, 11f, 14f);
            KeepSingleLine(title, 11f, 14f);
            AddMedalIcons(rect, SkirmishMedalMask.None, 4f, LibraryGridCardHeight - 20f, 14f);
            return rect;
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
                ShowIcon(image, SkirmishBattleLibraryArt.Medal(true, difficulties[i]), earned ? Color.white : new Color(1f, 1f, 1f, 0.78f));
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
                SetCopy(heading, fa ? "پیشنهاد بعدی" : "RECOMMENDED NEXT");
                KeepSingleLine(heading, 13f, 16f);
                string recommendation = FocusedRecommendation(theater, field);
                SetCopy(body, string.IsNullOrEmpty(recommendation)
                    ? (fa ? "هنوز پیشنهادی نیست." : "No recommendation yet.")
                    : RecommendationLabel(recommendation, fa));
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

        private void PresentBattlefieldSummary(bool hasPin, SkirmishTheaterPinState pin, bool fa, bool battlefield)
        {
            if (_progressTrack == null)
                return;
            if (battlefield || !hasPin)
            {
                _progressTrack.gameObject.SetActive(false);
                return;
            }

            _progressTrack.gameObject.SetActive(true);
            TMP_Text caption = _progressTrack.Find("Caption")?.GetComponent<TMP_Text>();
            TMP_Text count = _progressTrack.Find("Count")?.GetComponent<TMP_Text>();
            SetCopy(caption, fa ? "نبردهای برده" : "BATTLES WON", 14f, 22f);
            SetCopy(count, pin.WonCount + " / " + pin.PublishedCount, 14f, 22f);
            float trackWidth = _progressTrack.rect.width > 80f ? _progressTrack.rect.width : 520f;
            const float countWidth = 132f;
            if (fa)
            {
                if (count != null)
                {
                    count.isRightToLeftText = false;
                    count.alignment = TextAlignmentOptions.MidlineLeft;
                    count.overflowMode = TextOverflowModes.Overflow;
                    PlaceTopLeft(count.rectTransform, 0f, 0f, countWidth, 30f);
                }

                if (caption != null)
                {
                    caption.alignment = TextAlignmentOptions.MidlineRight;
                    caption.isRightToLeftText = true;
                    caption.overflowMode = TextOverflowModes.Overflow;
                    caption.textWrappingMode = TextWrappingModes.NoWrap;
                    PlaceTopLeft(caption.rectTransform, countWidth + 12f, 0f, Mathf.Max(80f, trackWidth - countWidth - 16f), 34f);
                }
            }
            else
            {
                if (caption != null)
                {
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                    caption.isRightToLeftText = false;
                    caption.overflowMode = TextOverflowModes.Ellipsis;
                    caption.textWrappingMode = TextWrappingModes.NoWrap;
                    PlaceTopLeft(caption.rectTransform, 0f, 0f, Mathf.Max(80f, trackWidth - countWidth - 12f), 30f);
                }

                if (count != null)
                {
                    count.isRightToLeftText = false;
                    count.alignment = TextAlignmentOptions.MidlineRight;
                    count.overflowMode = TextOverflowModes.Overflow;
                    PlaceTopLeft(count.rectTransform, Mathf.Max(0f, trackWidth - countWidth), 0f, countWidth, 30f);
                }
            }

            RectTransform bar = EnsureThinProgressBar(_progressTrack, 8f);
            float ratio = pin.PublishedCount > 0 ? Mathf.Clamp01(pin.WonCount / (float)pin.PublishedCount) : 0f;
            if (bar != null && _progressFill != null && _progressFill.transform.parent == bar)
            {
                float barWidth = bar.sizeDelta.x > 2f ? bar.sizeDelta.x : 520f;
                PlaceTopLeft(_progressFill.rectTransform, 0f, 0f, Mathf.Max(6f, barWidth * Mathf.Max(0.04f, ratio)), 8f);
                _progressFill.rectTransform.SetParent(bar, false);
            }
        }

        private RectTransform EnsureThinProgressBar(RectTransform track, float height)
        {
            if (track == null)
                return null;
            for (int i = track.childCount - 1; i >= 0; i--)
            {
                Transform child = track.GetChild(i);
                if (child.name != "Bar")
                    continue;
                if (track.Find("Bar") != child)
                    DestroyUi(child.gameObject);
            }

            RectTransform bar = track.Find("Bar") as RectTransform;
            if (bar == null)
            {
                var created = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                created.transform.SetParent(track, false);
                created.color = new Color(0.08f, 0.1f, 0.12f, 1f);
                created.raycastTarget = false;
                bar = created.rectTransform;
            }

            Image trackImage = track.GetComponent<Image>();
            if (trackImage != null)
                trackImage.enabled = false;
            float width = track.rect.width > 40f ? track.rect.width : 520f;
            PlaceTopLeft(bar, 0f, 36f, width, height);
            if (bar.GetComponent<RectMask2D>() == null)
                bar.gameObject.AddComponent<RectMask2D>();
            Image barImage = bar.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.color = new Color(0.08f, 0.1f, 0.12f, 1f);
                barImage.raycastTarget = false;
            }

            if (_progressFill == null || _progressFill.transform.parent != bar)
            {
                Transform existing = bar.Find("Fill");
                if (existing != null)
                    DestroyUi(existing.gameObject);
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                fill.transform.SetParent(bar, false);
                fill.color = new Color(0.91f, 0.72f, 0.22f, 1f);
                fill.raycastTarget = false;
                _progressFill = fill;
            }

            for (int i = track.childCount - 1; i >= 0; i--)
            {
                Transform child = track.GetChild(i);
                if (child.name == "Fill")
                    DestroyUi(child.gameObject);
            }

            return bar;
        }

        private void PresentMedalRow(SkirmishBriefingState briefing, bool hasPin, SkirmishTheaterPinState pin, bool fa, bool battlefield)
        {
            if (_medalRow == null)
                return;
            HorizontalLayoutGroup medalLayout = _medalRow.GetComponent<HorizontalLayoutGroup>();
            if (medalLayout != null)
                medalLayout.enabled = false;
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
                    ShowIcon(image, SkirmishBattleLibraryArt.Medal(true, difficulties[i]), earned ? Color.white : new Color(1f, 1f, 1f, 0.78f));
                TMP_Text count = slot.Find("Count")?.GetComponent<TMP_Text>();
                SetCopy(count, battlefield ? string.Empty : counts[i].ToString(), 14f, 18f);
                KeepSingleLine(count, 12f, 16f);
                if (count != null)
                    count.isRightToLeftText = false;
                SkirmishBattleLibraryArt.DifficultyCopy(difficulties[i], fa, out string label);
                TMP_Text caption = slot.Find("Caption")?.GetComponent<TMP_Text>();
                SetCopy(caption, label, LibraryFontFloor, 26f);
                if (caption != null)
                {
                    caption.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    caption.isRightToLeftText = fa;
                    caption.textWrappingMode = TextWrappingModes.NoWrap;
                    caption.overflowMode = TextOverflowModes.Overflow;
                    caption.enableAutoSizing = true;
                    caption.fontSizeMin = LibraryFontFloor;
                    caption.fontSizeMax = 26f;
                }

                if (slot is RectTransform slotRect)
                    PlaceMedalSlot(slotRect, i, fa);
            }
        }

        private void PlaceMedalSlot(RectTransform slot, int index, bool fa)
        {
            if (slot == null)
                return;
            StopExpanding(slot);
            if (slot.parent is RectTransform parent)
                StopExpanding(parent);
            float width = MedalRowWidth();
            float slotWidth = (width - 8f) * 0.5f;
            const float slotHeight = 46f;
            const float iconSize = 24f;
            const float countWidth = 32f;
            const float gap = 4f;
            PlaceTopLeft(slot, (index % 2) * (slotWidth + 8f), (index / 2) * (slotHeight + 6f), slotWidth, slotHeight);
            ReleaseFromLayout(slot);
            float iconX = fa ? slotWidth - iconSize - gap : gap;
            float countX = fa ? gap : slotWidth - countWidth - gap;
            float captionX = fa ? gap + countWidth + gap : gap + iconSize + gap;
            float captionLimit = fa ? iconX : countX;
            float captionWidth = Mathf.Max(0f, captionLimit - captionX - gap);
            if (slot.Find("Icon") is RectTransform icon)
            {
                PlaceTopLeft(icon, iconX, 11f, iconSize, iconSize);
                PinSquare(icon, iconSize);
                BindMedalIcon(icon.GetComponent<Image>(), index);
            }

            if (slot.Find("Caption") is RectTransform caption)
            {
                PlaceTopLeft(caption, captionX, 4f, captionWidth, 38f);
                ReleaseFromLayout(caption);
                TMP_Text captionText = caption.GetComponent<TMP_Text>();
                if (captionText != null)
                {
                    captionText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    captionText.isRightToLeftText = fa;
                    captionText.overflowMode = TextOverflowModes.Overflow;
                    captionText.textWrappingMode = TextWrappingModes.Normal;
                    captionText.enableAutoSizing = true;
                    captionText.fontSizeMin = 11f;
                    captionText.fontSizeMax = 16f;
                }
            }

            if (slot.Find("Count") is RectTransform count)
            {
                PlaceTopLeft(count, countX, 8f, countWidth, 30f);
                ReleaseFromLayout(count);
                TMP_Text countText = count.GetComponent<TMP_Text>();
                if (countText != null)
                {
                    countText.isRightToLeftText = false;
                    countText.alignment = TextAlignmentOptions.Center;
                    countText.overflowMode = TextOverflowModes.Overflow;
                }
            }
        }

        private void LockMedalArtwork()
        {
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (_medalRow != null)
            {
                StopExpanding(_medalRow);
                float rowWidth = MedalRowWidth();
                float rowY = _medalRow.anchoredPosition.y < -0.5f ? -_medalRow.anchoredPosition.y : 6f;
                float rowHeight = _medalRow.sizeDelta.y > 48f ? _medalRow.sizeDelta.y : 104f;
                PlaceTopLeft(_medalRow, 6f, rowY, rowWidth, rowHeight);
                ReleaseFromLayout(_medalRow);
                for (int i = 0; i < 4; i++)
                {
                    if (_medalRow.Find("Slot" + i) is RectTransform slot)
                        PlaceMedalSlot(slot, i, fa);
                }
            }

            LayoutTheatreMedals(fa);
        }

        private float MedalRowWidth()
        {
            RectTransform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn") as RectTransform;
            RectTransform briefing = column != null ? column.parent as RectTransform : null;
            float panel = PointSpan(briefing);
            if (panel >= 200f)
                return Mathf.Max(160f, panel - 26f);

            if (_medalRow != null)
            {
                float rowWidth = PointSpan(_medalRow);
                if (rowWidth >= 160f)
                    return rowWidth;
            }

            return 548f;
        }

        private static float PointSpan(RectTransform rect)
        {
            if (rect == null)
                return 0f;
            bool point = Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x) < 0.01f;
            if (point && rect.sizeDelta.x >= 160f)
                return rect.sizeDelta.x;
            if (rect.rect.width >= 160f)
                return rect.rect.width;
            if (rect.sizeDelta.x >= 160f)
                return rect.sizeDelta.x;
            return 0f;
        }

        private static void BindMedalIcon(Image image, int index)
        {
            if (image == null)
                return;
            Color tint = image.color.a > 0.2f ? image.color : Color.white;
            ShowIcon(image, SkirmishBattleLibraryArt.Medal(true, MedalTier(index)), tint);
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            if (image.sprite == null)
            {
                image.enabled = false;
                image.gameObject.SetActive(false);
            }
        }

        private static SkirmishDifficultyId MedalTier(int index)
        {
            switch (index)
            {
                case 1: return SkirmishDifficultyId.Regular;
                case 2: return SkirmishDifficultyId.Veteran;
                case 3: return SkirmishDifficultyId.Commander;
                default: return SkirmishDifficultyId.Recruit;
            }
        }

        private static void StopExpanding(RectTransform rect)
        {
            if (rect == null)
                return;
            HorizontalLayoutGroup group = rect.GetComponent<HorizontalLayoutGroup>();
            if (group == null)
                return;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            group.enabled = false;
        }

        private static void PinSquare(RectTransform rect, float size)
        {
            if (rect == null)
                return;
            LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            element.minWidth = size;
            element.minHeight = size;
            element.preferredWidth = size;
            element.preferredHeight = size;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
        }

        private static void ReleaseFromLayout(RectTransform rect)
        {
            if (rect == null)
                return;
            LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
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

        private void PresentScreenHeading(SkirmishTheaterState theater, SkirmishBattlefieldState field, bool fa, bool battlefield)
        {
            TMP_Text screenTitle = transform.Find("SkirmishSetupComposition/ScreenTitlePanel/ScreenTitle")?.GetComponent<TMP_Text>();
            string headingMap = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
            bool hasHeading = TryFindTheaterPin(theater, headingMap, out SkirmishTheaterPinState headingPin);
            string mapTitle = hasHeading ? MapDisplayTitle(headingPin.MapId, fa) : (fa ? "صحنه نبرد" : "THEATRE");
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
            if (composition is not RectTransform compositionRect)
                return;
            PlaceFooterPlate(compositionRect);
            Button back = composition.Find("BackButton")?.GetComponent<Button>();
            float compositionHeight = compositionRect.rect.height;
            float compositionWidth = compositionRect.rect.width;
            if (compositionHeight < 200f)
                compositionHeight = 941f;
            if (compositionWidth < 200f)
                compositionWidth = 1672f;
            float height = 84f;
            float y = compositionHeight - 104f;
            float gap = 6f;
            float side = 10f;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            Button[] commands =
            {
                back,
                resetButton,
                _libraryFavouritesButton,
                _libraryAdvancedButton,
                randomizeSeedButton,
                launchButton
            };
            int commandCount = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                if (commands[i] != null)
                    commandCount++;
            }

            if (commandCount == 0)
                return;
            float buttonWidth = (compositionWidth - side * 2f - gap * (commandCount - 1)) / commandCount;
            if (buttonWidth < 72f)
            {
                gap = 4f;
                buttonWidth = (compositionWidth - side * 2f - gap * (commandCount - 1)) / commandCount;
            }

            float x = side;
            for (int i = 0; i < commands.Length; i++)
            {
                Button button = commands[i];
                if (button == null)
                    continue;
                PlaceTopLeft(button.GetComponent<RectTransform>(), x, y, buttonWidth, height);
                HideFooterChevrons(button);
                HideDiceIcon(button);
                ClearFooterIcon(button);
                if (button == back)
                {
                    TMP_Text backLabel = button.GetComponentInChildren<TMP_Text>(true);
                    SetCopy(backLabel, fa ? "بازگشت" : "BACK", 14f, 20f);
                }

                FitFooterLabel(button, fa, buttonWidth);
                TMP_Text footerLabel = button.GetComponentInChildren<TMP_Text>(true);
                if (footerLabel != null)
                    footerLabel.isRightToLeftText = ContainsArabicScript(footerLabel.text);
                if (button == back)
                    AttachAudio(back, UIAudioEventKind.ScreenBack);
                x += buttonWidth + gap;
            }
        }

        private static void FitFooterLabel(Button button, bool fa, float buttonWidth)
        {
            if (button == null)
                return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                return;
            Stretch(label.rectTransform, 8f, 8f, 6f, 6f);
            label.alignment = TextAlignmentOptions.Center;
            if (fa)
                label.isRightToLeftText = ContainsArabicScript(label.text);
            label.textWrappingMode = buttonWidth < 150f ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 20f;
            label.margin = new Vector4(2f, 1f, 2f, 1f);
        }

        private static void HideFooterChevrons(Button button)
        {
            if (button == null)
                return;
            Transform[] children = button.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null || children[i] == button.transform)
                    continue;
                if (children[i].name.IndexOf("Chevron", StringComparison.Ordinal) < 0)
                    continue;
                children[i].gameObject.SetActive(false);
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text != null &&
                (label.text.IndexOf('›') >= 0 || label.text.IndexOf('‹') >= 0))
            {
                label.text = label.text.Replace("›", string.Empty).Replace("‹", string.Empty).Trim();
            }
        }

        private void PresentWallet()
        {
            MainMenuAccountHeaderView header = GetComponentInChildren<MainMenuAccountHeaderView>(true);
            if (header == null)
                return;
            int credits = 0;
            bool shown = SkirmishLibraryWallet.ReadCredits != null && SkirmishLibraryWallet.ReadCredits(out credits);
            header.enabled = false;
            if (!shown || header.Value == null || HasCommanderIdentity())
                return;
            header.gameObject.SetActive(true);
            header.Value.isRightToLeftText = false;
            header.Value.text = credits.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
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

            BindFooter(resetButton, OnLibraryFilters, fa ? "پالایه" : "FILTERS");
            BindFooter(_libraryFavouritesButton, OnLibraryFavourites, fa ? "برگزیده‌ها" : "FAVOURITES");
            BindFooter(_libraryAdvancedButton, OnLibraryAdvanced, fa ? "پیشرفته" : "ADVANCED");
            if (battlefield)
            {
                BindFooter(randomizeSeedButton, OnLibraryTheater, fa ? "نقشه صحنه" : "THEATRE MAP");
                BindFooter(launchButton, LaunchMatch, fa ? "شروع نبرد" : "START BATTLE");
            }
            else
            {
                BindFooter(randomizeSeedButton, OnLibraryZoom, fa ? "نزدیک‌تر" : "ZOOM IN");
                BindFooter(launchButton, OnLibraryQuickBattle, fa ? "نبرد سریع" : "QUICK BATTLE");
            }

            ClearFooterIcon(resetButton);
            ClearFooterIcon(_libraryFavouritesButton);
            ClearFooterIcon(_libraryAdvancedButton);
            ClearFooterIcon(randomizeSeedButton);
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
            button.onClick.RemoveListener(OnLibraryFavourites);
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

            bool localSound = action == OnLibraryFilters || action == OnLibraryAdvanced || action == OnLibraryFavourites;
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

        private void OnLibraryFavourites()
        {
            _favouritesLit = !_favouritesLit;
            TintButton(_libraryFavouritesButton, _favouritesLit, true);
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
            if (row == null)
                return;
            StopExpanding(row);
            if (row.childCount == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    var slot = new GameObject("Slot" + i, typeof(RectTransform)).GetComponent<RectTransform>();
                    slot.SetParent(row, false);
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    icon.transform.SetParent(slot, false);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    ShowIcon(icon, SkirmishBattleLibraryArt.Medal(true, MedalTier(i)), Color.white);
                    var caption = CreateBoundLabel(slot, "Caption", "ui.skirmish.library_medal_" + i, "MEDAL");
                    caption.alignment = TextAlignmentOptions.MidlineLeft;
                    caption.overflowMode = TextOverflowModes.Overflow;
                    caption.textWrappingMode = TextWrappingModes.Normal;
                    var count = CreateBoundLabel(slot, "Count", "ui.skirmish.library_medal_count", "0");
                    count.alignment = TextAlignmentOptions.Center;
                    count.isRightToLeftText = false;
                    count.overflowMode = TextOverflowModes.Overflow;
                }
            }

            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            for (int i = 0; i < 4; i++)
            {
                Transform slot = row.Find("Slot" + i);
                if (slot == null)
                    continue;
                StopExpanding(slot as RectTransform);
                if (slot.Find("Icon") is RectTransform icon)
                {
                    PinSquare(icon, 24f);
                    BindMedalIcon(icon.GetComponent<Image>(), i);
                }

                if (slot is RectTransform slotRect)
                    PlaceMedalSlot(slotRect, i, fa);
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
            if (panel == null)
                return;
            var group = panel.GetComponent<VerticalLayoutGroup>() ?? panel.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(8, 8, 2, 2);
            group.spacing = 2f;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            if (panel.Find("Heading") == null)
            {
                var heading = CreateBoundLabel(panel, "Heading", "ui.skirmish.library_recommended", "RECOMMENDED NEXT");
                heading.alignment = TextAlignmentOptions.MidlineLeft;
                var body = CreateBoundLabel(panel, "Body", "ui.skirmish.library_recommended_empty", "No recommendation yet.");
                body.alignment = TextAlignmentOptions.TopLeft;
            }

            FitLayout(panel.Find("Heading") as RectTransform, 0f, 22f, 1f);
            FitLayout(panel.Find("Body") as RectTransform, 0f, 36f, 1f);
            TMP_Text headingText = panel.Find("Heading")?.GetComponent<TMP_Text>();
            TMP_Text bodyText = panel.Find("Body")?.GetComponent<TMP_Text>();
            KeepSingleLine(headingText, 13f, 16f);
            if (bodyText != null)
            {
                bodyText.textWrappingMode = TextWrappingModes.Normal;
                bodyText.overflowMode = TextOverflowModes.Truncate;
                bodyText.enableAutoSizing = true;
                bodyText.fontSizeMin = 13f;
                bodyText.fontSizeMax = 16f;
            }
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
            ReserveFooterBand(transform.Find("SkirmishSetupComposition"));
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
                plateRect.offsetMin = new Vector2(8f, theater ? 128f : 8f);
                plateRect.offsetMax = new Vector2(-8f, -112f);
                MaskRect(plateRect);
            }

            if (_overallStrip != null && preview is RectTransform previewRect)
            {
                _overallStrip.SetParent(previewRect, false);
                _overallStrip.anchorMin = new Vector2(0f, 0f);
                _overallStrip.anchorMax = new Vector2(1f, 0f);
                _overallStrip.pivot = new Vector2(0.5f, 0f);
                _overallStrip.anchoredPosition = new Vector2(0f, 8f);
                _overallStrip.sizeDelta = new Vector2(-16f, 76f);
            }

            if (_libraryPins is RectTransform pinRect)
            {
                pinRect.anchorMin = Vector2.zero;
                pinRect.anchorMax = Vector2.one;
                pinRect.offsetMin = new Vector2(132f, 8f);
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
                PlaceTopLeft(search, 12f, 56f, 700f, 44f);
                FrameSearchField(search);
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
                PlaceTopLeft(mapNameText.rectTransform, 12f, 6f, 680f, 40f);
                mapNameText.textWrappingMode = TextWrappingModes.NoWrap;
                mapNameText.overflowMode = TextOverflowModes.Overflow;
            }

            if (preview != null)
            {
                SetActive(preview.Find("ObjectiveRow"), false);
                SetActive(preview.Find("Metrics"), false);
                SetActive(preview.Find("TitleRule"), false);
                SetActive(preview.Find("SeedMetric"), false);
                SetActive(preview.Find("BattleLibraryCount"), false);
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
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            PlaceBriefingLayout(rules as RectTransform, battlefield);
            if (_libraryFilters is RectTransform filters)
                PlaceTopLeft(filters, 24f, 180f, 460f, 280f);
            if (_libraryGrid is RectTransform grid)
            {
                grid.anchorMin = new Vector2(0f, 0f);
                grid.anchorMax = new Vector2(1f, 1f);
                grid.offsetMin = new Vector2(8f, 8f);
                grid.offsetMax = new Vector2(-8f, -112f);
                if (grid.GetComponent<V3GradientGraphic>() == null)
                    TintPanel(grid.gameObject);
            }

            MirrorLibraryPanels();
            if (rules != null && rules.Find("RulesScroll") is RectTransform scroll)
            {
                var ignore = scroll.GetComponent<LayoutElement>() ?? scroll.gameObject.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
                scroll.gameObject.SetActive(_advancedOpen);
                if (_advancedOpen)
                {
                    scroll.anchorMin = new Vector2(0f, 0f);
                    scroll.anchorMax = new Vector2(1f, 0f);
                    scroll.pivot = new Vector2(0.5f, 0f);
                    scroll.anchoredPosition = new Vector2(0f, 8f);
                    scroll.sizeDelta = new Vector2(-16f, 180f);
                }
            }
        }

        private void PlaceBriefingLayout(RectTransform rules, bool battlefield)
        {
            if (rules == null)
                return;
            RectTransform column = FindOrCreate(rules, "BriefingColumn");
            float bottom = _advancedOpen ? 196f : 8f;
            Stretch(column, 8f, 8f, 6f, bottom);
            VerticalLayoutGroup group = column.GetComponent<VerticalLayoutGroup>();
            if (group == null)
                group = column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.enabled = false;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            Transform title = rules.Find("Title") ?? column.Find("Title");
            if (title is RectTransform titleRect)
            {
                titleRect.SetParent(column, false);
                FitLayout(titleRect, 0f, 30f, 0f);
                TMP_Text titleText = titleRect.GetComponent<TMP_Text>();
                KeepSingleLine(titleText, 16f, 26f);
            }

            if (scenarioDescription != null)
            {
                scenarioDescription.transform.SetParent(column, false);
                var purposeBinding = scenarioDescription.GetComponent<V3LocalizedTextBindingView>()
                    ?? scenarioDescription.gameObject.AddComponent<V3LocalizedTextBindingView>();
                if (string.IsNullOrEmpty(purposeBinding.LocalizationKey) ||
                    !purposeBinding.LocalizationKey.StartsWith("ui.skirmish.", StringComparison.Ordinal))
                    purposeBinding.Configure("ui.skirmish.library_purpose", "Same map. Seed changes AI choices.");
                FitLayout(scenarioDescription.rectTransform, 0f, 64f, 0f);
                scenarioDescription.margin = new Vector4(4f, 2f, 4f, 2f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Truncate;
                scenarioDescription.alignment = TextAlignmentOptions.TopLeft;
            }

            if (_recommendedPanel != null)
            {
                _recommendedPanel.SetParent(column, false);
                FitLayout(_recommendedPanel, 0f, 68f, 0f);
            }

            if (_briefingPreview != null)
            {
                _briefingPreview.transform.SetParent(column, false);
                var previewLayout = FitLayout(_briefingPreview.rectTransform, 0f, battlefield ? 200f : 0f, 0f);
                previewLayout.minHeight = battlefield ? 180f : 0f;
                previewLayout.preferredHeight = battlefield ? 200f : 0f;
                previewLayout.flexibleHeight = 0f;
                previewLayout.ignoreLayout = !battlefield;
            }

            if (_statRow != null)
            {
                _statRow.SetParent(column, false);
                FitLayout(_statRow, 0f, 76f, 0f);
            }

            if (_medalRow != null)
            {
                _medalRow.SetParent(column, false);
                FitLayout(_medalRow, 0f, 44f, 0f);
            }

            if (_difficultyRow != null)
            {
                _difficultyRow.SetParent(column, false);
                FitLayout(_difficultyRow, 0f, 92f, 0f);
            }

            if (_medalGrid != null)
            {
                _medalGrid.SetParent(column, false);
                FitLayout(_medalGrid, 0f, 96f, 0f);
            }

            if (_libraryStoreButton != null)
            {
                RectTransform store = _libraryStoreButton.GetComponent<RectTransform>();
                store.SetParent(rules, false);
                var ignore = store.GetComponent<LayoutElement>() ?? store.gameObject.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
                float rulesWidth = rules.rect.width > 200f ? rules.rect.width : rules.sizeDelta.x;
                if (rulesWidth < 200f)
                    rulesWidth = 550f;
                PlaceTopLeft(store, rulesWidth - 226f, 4f, 210f, 30f);
            }

            if (_progressTrack != null && LibraryLevel == SkirmishLibraryLevel.Battlefield)
                _progressTrack.gameObject.SetActive(false);
            ComposeBriefingColumn(battlefield);
        }

        private void ComposeBriefingColumn(bool battlefield)
        {
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            RectTransform column = rules != null ? rules.Find("BriefingColumn") as RectTransform : null;
            if (column == null)
                return;
            VerticalLayoutGroup group = column.GetComponent<VerticalLayoutGroup>();
            if (group != null)
                group.enabled = false;
            float width = column.rect.width > 240f ? column.rect.width - 12f : 558f;
            float y = 6f;
            if (battlefield)
                y = StackBriefing(_briefingPreview != null ? _briefingPreview.rectTransform : null, y, width, 188f, _briefingPreview != null && _briefingPreview.gameObject.activeSelf);
            Transform title = rules.Find("BriefingColumn/Title") ?? rules.Find("Title");
            y = StackBriefing(title as RectTransform, y, width, 40f, title != null);
            y = StackBriefing(scenarioDescription != null ? scenarioDescription.rectTransform : null, y, width, battlefield ? 64f : 84f, scenarioDescription != null);
            if (!battlefield)
                y = StackBriefing(_progressTrack, y, width, 58f, _progressTrack != null && _progressTrack.gameObject.activeSelf);
            y = StackBriefing(_statRow, y, width, 84f, battlefield && _statRow != null && _statRow.gameObject.activeSelf);
            y = StackBriefing(_medalRow, y, width, 104f, _medalRow != null && _medalRow.gameObject.activeSelf);
            y = StackBriefing(_difficultyRow, y, width, 116f, battlefield && _difficultyRow != null && _difficultyRow.gameObject.activeSelf);
            StackBriefing(_recommendedPanel, y, width, 124f, !battlefield && _recommendedPanel != null && _recommendedPanel.gameObject.activeSelf);
            if (_medalGrid != null)
                _medalGrid.gameObject.SetActive(false);
            if (scenarioDescription != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                RectTransform descriptionRect = scenarioDescription.rectTransform;
                float shownY = -descriptionRect.anchoredPosition.y;
                float shownHeight = descriptionRect.sizeDelta.y > 8f ? descriptionRect.sizeDelta.y : 84f;
                float shownWidth = descriptionRect.sizeDelta.x > 80f ? descriptionRect.sizeDelta.x - 28f : Mathf.Max(80f, width - 36f);
                float leading = fa ? 28f : 16f;
                PlaceTopLeft(descriptionRect, fa ? 12f : 18f, shownY, Mathf.Max(80f, shownWidth - (fa ? 8f : 0f)), shownHeight);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = fa ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
                scenarioDescription.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                scenarioDescription.isRightToLeftText = fa;
                scenarioDescription.margin = new Vector4(fa ? 12f : leading, 6f, fa ? leading : 12f, 6f);
                scenarioDescription.fontSizeMin = 15f;
                scenarioDescription.fontSizeMax = 24f;
                ReleaseTextClip(column);
                ReleaseTextClip(rules);
            }

            TMP_Text titleText = title != null ? title.GetComponent<TMP_Text>() : null;
            if (titleText != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                titleText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                titleText.isRightToLeftText = fa;
                titleText.overflowMode = TextOverflowModes.Overflow;
            }

            FitBriefingPicture();
        }

        private static void ReleaseTextClip(Transform root)
        {
            if (root == null)
                return;
            RectMask2D mask = root.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
        }

        private static float StackBriefing(RectTransform rect, float y, float width, float height, bool show)
        {
            if (rect == null)
                return y;
            if (!show)
            {
                rect.gameObject.SetActive(false);
                return y;
            }

            rect.gameObject.SetActive(true);
            LayoutElement element = rect.GetComponent<LayoutElement>();
            if (element != null)
                element.ignoreLayout = true;
            PlaceTopLeft(rect, 6f, y, width, height);
            return y + height + 8f;
        }

        private void MirrorLibraryPanels()
        {
            ApplyLibraryFrame();
        }

        private void ApplyLibraryFrame()
        {
            RectTransform composition = transform.Find("SkirmishSetupComposition") as RectTransform;
            var map = transform.Find("SkirmishSetupComposition/OperationPreview") as RectTransform;
            var briefing = transform.Find("SkirmishSetupComposition/BaseAssaultRules") as RectTransform;
            if (composition == null || map == null || briefing == null)
                return;
            FillComposition(composition);
            bool briefingOnLeft = !UiShellRuntimeGateway.Localization.IsRightToLeft;
            SkirmishLibraryFrame.Resolve(
                composition.rect.width,
                composition.rect.height,
                briefingOnLeft,
                out float mapX,
                out float mapWidth,
                out float briefingX,
                out float briefingWidth,
                out float panelY,
                out float panelHeight);
            PlaceTopLeft(map, mapX, panelY, mapWidth, panelHeight);
            PlaceTopLeft(briefing, briefingX, panelY, briefingWidth, panelHeight);
            EnsureOpaquePanel(map);
            EnsureOpaquePanel(briefing);
            FitLibrarySearch();
        }

        private static void FillComposition(RectTransform composition)
        {
            if (composition == null)
                return;
            composition.anchorMin = Vector2.zero;
            composition.anchorMax = Vector2.one;
            composition.pivot = new Vector2(0.5f, 0.5f);
            composition.offsetMin = Vector2.zero;
            composition.offsetMax = Vector2.zero;
            composition.localScale = Vector3.one;
            composition.localRotation = Quaternion.identity;
            composition.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }

        private static void EnsureOpaquePanel(RectTransform panel)
        {
            if (panel == null)
                return;
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = true;
                Color color = image.color;
                if (color.a < 0.98f || SkirmishLibraryBlankImage.IsLightNeutral(color))
                    image.color = new Color(0.04f, 0.06f, 0.07f, 1f);
            }

            Transform fill = panel.Find("PanelFill");
            Image fillImage = fill != null ? fill.GetComponent<Image>() : null;
            if (fillImage == null)
            {
                fillImage = new GameObject("PanelFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                fillImage.transform.SetParent(panel, false);
            }

            fillImage.transform.SetAsFirstSibling();
            fillImage.color = new Color(0.04f, 0.06f, 0.07f, 1f);
            fillImage.raycastTarget = false;
            Stretch(fillImage.rectTransform);
        }

        private void PresentZoomHint(bool fa, bool battlefield)
        {
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            if (preview == null)
                return;
            TMP_Text hint = preview.Find("ZoomHint")?.GetComponent<TMP_Text>();
            if (hint == null)
                hint = CreateLabel(preview, "ZoomHint", LibraryFontFloor);
            hint.gameObject.SetActive(!battlefield);
            if (battlefield)
                return;
            RectTransform rect = hint.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(16f, 80f);
            rect.sizeDelta = new Vector2(-32f, 40f);
            hint.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            hint.isRightToLeftText = fa;
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            hint.overflowMode = TextOverflowModes.Overflow;
            hint.enableAutoSizing = true;
            hint.fontSizeMin = LibraryFontFloor;
            hint.fontSizeMax = 26f;
            SetCopy(hint, fa ? "برای بزرگنمایی لمس یا جمع کن" : "Tap or pinch to zoom", LibraryFontFloor, 26f);
            hint.overflowMode = TextOverflowModes.Overflow;
            hint.fontSizeMin = LibraryFontFloor;
            hint.isRightToLeftText = fa;
        }

        private void EnforceLibraryFontFloor()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition == null)
                return;
            TMP_Text[] texts = composition.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy)
                    continue;
                text.enableAutoSizing = true;
                if (text.fontSizeMax < LibraryFontFloor)
                    text.fontSizeMax = LibraryFontFloor;
                if (text.fontSizeMin < LibraryFontFloor)
                    text.fontSizeMin = LibraryFontFloor;
                if (text.fontSize < LibraryFontFloor)
                    text.fontSize = LibraryFontFloor;
            }
        }

        private void RemoveBlankPanelImages()
        {
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            HideBlankGraphics(rules);
            HideBlankGraphics(preview);
            if (_briefingPreview != null)
            {
                _briefingPreview.texture = null;
                _briefingPreview.enabled = false;
                _briefingPreview.color = new Color(0.05f, 0.07f, 0.08f, 1f);
            }

            Transform scroll = rules != null ? rules.Find("RulesScroll") : null;
            if (scroll != null && !_advancedOpen)
                scroll.gameObject.SetActive(false);
            HideOverlappingBlank(rules);
        }

        private static void HideBlankGraphics(Transform root)
        {
            if (root == null)
                return;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (SkirmishLibraryBlankImage.IsBlank(images[i]))
                    images[i].enabled = false;
            }

            RawImage[] rawImages = root.GetComponentsInChildren<RawImage>(true);
            for (int i = 0; i < rawImages.Length; i++)
            {
                if (rawImages[i] != null && rawImages[i].name == "Picture")
                    continue;
                if (SkirmishLibraryBlankImage.IsBlank(rawImages[i]))
                    rawImages[i].enabled = false;
            }
        }

        private void HideOverlappingBlank(Transform rules)
        {
            RectTransform army = _statRow != null ? _statRow.Find("Army") as RectTransform : null;
            if (army == null || _medalRow == null || !army.gameObject.activeInHierarchy || !_medalRow.gameObject.activeInHierarchy)
                return;
            Vector3 armyCenter = army.TransformPoint(army.rect.center);
            Vector3 medalCenter = _medalRow.TransformPoint(_medalRow.rect.center);
            Graphic[] graphics = rules.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic is TMP_Text || graphic is V3GradientGraphic || !graphic.enabled)
                    continue;
                if (graphic.name == "Picture" || graphic.name == "DepthFrame")
                    continue;
                RectTransform rect = graphic.rectTransform;
                if (!SkirmishLibraryBlankImage.IsLarge(rect))
                    continue;
                if (!rect.rect.Contains(rect.InverseTransformPoint(armyCenter)) ||
                    !rect.rect.Contains(rect.InverseTransformPoint(medalCenter)))
                    continue;
                bool emptyRaw = graphic is RawImage raw && raw.texture == null;
                bool emptyImage = graphic is Image image && image.sprite == null;
                if (emptyRaw || emptyImage || SkirmishLibraryBlankImage.IsLightNeutral(graphic.color))
                    graphic.enabled = false;
            }
        }

        private static void AddSelectionGlow(RectTransform card)
        {
            if (card == null || card.Find("SelectionGlowTop") != null)
                return;
            Color glow = new Color(1f, 0.82f, 0.28f, 1f);
            AddGlowEdge(card, "SelectionGlowTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(8f, 10f), new Vector2(0f, 4f), glow);
            AddGlowEdge(card, "SelectionGlowBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(8f, 10f), new Vector2(0f, -4f), glow);
            AddGlowEdge(card, "SelectionGlowLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(10f, 8f), new Vector2(-4f, 0f), glow);
            AddGlowEdge(card, "SelectionGlowRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(10f, 8f), new Vector2(4f, 0f), glow);
        }

        private static void AddGlowEdge(RectTransform card, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(card, false);
            image.color = color;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private void RebuildBriefingColumn()
        {
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            if (column is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private static LayoutElement FitLayout(RectTransform rect, float preferredWidth, float preferredHeight, float flexibleWidth)
        {
            if (rect == null)
                return null;
            var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.minHeight = preferredHeight > 0f ? preferredHeight : 0f;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = 0f;
            return element;
        }

        private static void KeepSingleLine(TMP_Text text, float min, float max)
        {
            if (text == null)
                return;
            min = Mathf.Max(LibraryFontFloor, min);
            max = Mathf.Max(min, max);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            if (text.fontSize < min)
                text.fontSize = min;
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

        private string FocusedRecommendation(SkirmishTheaterState theater, SkirmishBattlefieldState field)
        {
            string focused = theater.SelectedMapId;
            if (!string.IsNullOrEmpty(theater.RecommendedScenarioId) &&
                TryFindEntry(theater.RecommendedScenarioId, out SkirmishBattleCatalogEntry recommended) &&
                string.Equals(recommended.MapId, focused, StringComparison.OrdinalIgnoreCase))
                return theater.RecommendedScenarioId;

            if (field.GridCards != null)
            {
                for (int i = 0; i < field.GridCards.Length; i++)
                {
                    SkirmishBattleCardState card = field.GridCards[i];
                    if (IsEmptyCard(card) || card.Access != SkirmishCardAccess.Playable)
                        continue;
                    if ((card.Medals & SkirmishMedalMask.Regular) == 0)
                        return card.ScenarioId;
                }
            }

            return string.Empty;
        }

        private string RecommendationLabel(string scenarioId, bool fa)
        {
            if (!TryFindEntry(scenarioId, out SkirmishBattleCatalogEntry entry))
                return scenarioId ?? string.Empty;
            SkirmishBattleLibraryArt.ObjectiveCopy(entry.ObjectiveId, fa, out _, out string objective);
            SkirmishBattleLibraryArt.ProfileCopy(entry.ArmyProfile, fa, out string army);
            SkirmishBattleLibraryArt.ProfileCopy(entry.StartProfile, fa, out string start);
            string detail = string.IsNullOrEmpty(start) ? army : army + " · " + start;
            return scenarioId + " · " + objective + "\n" + detail;
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

        private static bool IsGeneratedLibraryCopy(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true;
            string trimmed = value.Trim();
            if (trimmed.StartsWith("ui.", StringComparison.Ordinal) || trimmed.StartsWith("(ui.", StringComparison.Ordinal))
                return true;
            if (trimmed.IndexOf("(ui.skirmish.", StringComparison.Ordinal) >= 0)
                return true;
            return trimmed.StartsWith("Briefing for ", StringComparison.Ordinal)
                || trimmed.StartsWith("Objective for ", StringComparison.Ordinal)
                || trimmed.StartsWith("توجیه ", StringComparison.Ordinal);
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

        private static readonly Dictionary<string, Sprite> LibraryIconCache = new Dictionary<string, Sprite>();

        private static Sprite LoadLibraryIcon(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return null;
            if (LibraryIconCache.TryGetValue(resourcePath, out Sprite cached) && cached != null)
                return cached;
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null && texture.isReadable)
                {
                    sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            if (sprite != null)
                LibraryIconCache[resourcePath] = sprite;
            return sprite;
        }

        private static void ShowIcon(Image image, string resourcePath, Color tint)
        {
            if (image == null)
                return;
            Sprite sprite = LoadLibraryIcon(resourcePath);
            image.sprite = sprite;
            image.enabled = sprite != null;
            if (sprite == null)
                image.gameObject.SetActive(false);
            else if (!image.gameObject.activeSelf)
                image.gameObject.SetActive(true);
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            image.color = tint;
            image.raycastTarget = false;
        }

        private static string ScenarioPreviewKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOf("skirmish.preview.", StringComparison.Ordinal) < 0)
                return null;
            return key;
        }

        private static void PlaceFittedPreview(RawImage image, string key, float x, float y, float frameWidth, float frameHeight)
        {
            if (image == null)
                return;
            image.raycastTarget = false;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            Texture2D texture = string.IsNullOrEmpty(key) ? null : SkirmishLibraryPreviewAssets.Load(key);
            if (texture == null && !string.IsNullOrEmpty(key))
                texture = SkirmishLibraryPreviewAssets.Placeholder();
            if (texture == null || frameWidth < 2f || frameHeight < 2f)
            {
                image.texture = null;
                image.enabled = false;
                return;
            }

            image.enabled = true;
            image.texture = texture;
            image.color = Color.white;
            float aspect = texture.width / (float)Mathf.Max(1, texture.height);
            float fittedWidth = frameWidth;
            float fittedHeight = fittedWidth / aspect;
            if (fittedHeight > frameHeight)
            {
                fittedHeight = frameHeight;
                fittedWidth = fittedHeight * aspect;
            }

            float originX = x + (frameWidth - fittedWidth) * 0.5f;
            float originY = y + (frameHeight - fittedHeight) * 0.5f;
            PlaceTopLeft(image.rectTransform, originX, originY, fittedWidth, fittedHeight);
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

        private static void ApplyBoundPreview(RawImage image, string key, float frameWidth, float frameHeight)
        {
            if (image == null)
                return;
            image.texture = SkirmishLibraryPreviewAssets.LoadOrPlaceholder(key);
            image.color = Color.white;
            image.raycastTarget = false;
            CoverCrop(image, frameWidth, frameHeight);
        }

        private static void CoverCrop(RawImage image, float frameWidth, float frameHeight)
        {
            float sourceWidth = image.texture != null && image.texture.width > 0
                ? image.texture.width
                : SkirmishLibraryPreviewAssets.Width;
            float sourceHeight = image.texture != null && image.texture.height > 0
                ? image.texture.height
                : SkirmishLibraryPreviewAssets.Height;
            float sourceAspect = sourceWidth / sourceHeight;
            if (frameWidth < 2f || frameHeight < 2f)
            {
                image.uvRect = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            float frameAspect = frameWidth / frameHeight;
            if (frameAspect >= sourceAspect)
            {
                float visibleHeight = sourceAspect / frameAspect;
                image.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }
            else
            {
                float visibleWidth = frameAspect / sourceAspect;
                image.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }
        }

        private static bool TryFrameSize(RectTransform rect, out float width, out float height)
        {
            width = rect != null ? rect.rect.width : 0f;
            height = rect != null ? rect.rect.height : 0f;
            if (width > 2f && height > 2f)
                return true;
            width = rect != null ? rect.sizeDelta.x : 0f;
            height = rect != null ? rect.sizeDelta.y : 0f;
            return width > 2f && height > 2f;
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
            size = Mathf.Max(LibraryFontFloor, size);
            text.font = _libraryLabelFont != null ? _libraryLabelFont : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontSizeMin = LibraryFontFloor;
            text.fontSizeMax = Mathf.Max(size, LibraryFontFloor);
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
            if (text.name != "Subtitle" && text.name != "Value" && !text.name.StartsWith("Column_", StringComparison.Ordinal))
                min = Mathf.Max(LibraryFontFloor, min);
            max = Mathf.Max(min, max);
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
            shadow.anchoredPosition = widget.anchoredPosition + new Vector2(10f, -12f);
            shadow.sizeDelta = widget.sizeDelta + new Vector2(16f, 16f);
            shadow.SetSiblingIndex(widget.GetSiblingIndex());
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0.1f, 0.04f, 0.01f, 0.78f);
            image.raycastTarget = false;
        }

        private void ApplyLibraryDepth()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            EnsureDepthPlate(composition);
            Transform preview = composition != null ? composition.Find("OperationPreview") : null;
            Transform plate = preview != null ? preview.Find("MapPreviewClip") : null;
            Transform rules = composition != null ? composition.Find("BaseAssaultRules") : null;
            EnsurePanelShadow(preview as RectTransform);
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

            ClampOrnamentCorners(preview as RectTransform);
            ClampOrnamentCorners(rules as RectTransform);
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
            if (_libraryFavouritesButton != null)
                _libraryFavouritesButton.transform.SetAsLastSibling();
            if (_libraryAdvancedButton != null)
                _libraryAdvancedButton.transform.SetAsLastSibling();
            if (randomizeSeedButton != null)
                randomizeSeedButton.transform.SetAsLastSibling();
            if (launchButton != null)
                launchButton.transform.SetAsLastSibling();
            Transform launchTransition = composition != null ? composition.Find("LaunchTransition") : null;
            if (launchTransition != null && launchTransition.gameObject.activeSelf)
                launchTransition.SetAsLastSibling();
            RaiseDepth(composition, "SafeAreaEdgeLeft");
            RaiseDepth(composition, "SafeAreaEdgeRight");
            RaiseDepth(composition, "SafeAreaEdgeTop");
            RaiseDepth(composition, "SafeAreaEdgeBottom");
            RaiseDepth(composition, "SafeAreaNotch");
            FitLibrarySearch();
        }

        private void FitLibrarySearch()
        {
            if (battleLibrarySearch == null)
                return;
            RectTransform search = battleLibrarySearch.GetComponent<RectTransform>();
            if (search == null)
                return;
            RectTransform preview = search.parent as RectTransform;
            if (preview == null)
                return;
            float width = preview.rect.width;
            if (width < 80f)
                width = preview.sizeDelta.x;
            Transform toggle = preview.Find("LibraryModeToggle");
            bool reserveToggle = toggle != null && toggle.gameObject.activeSelf;
            float reserved = reserveToggle ? 268f : 12f;
            float searchWidth = Mathf.Clamp(width - 24f - reserved, 140f, 820f);
            PlaceTopLeft(search, 12f, 56f, searchWidth, 44f);
            FrameSearchField(search);
            search.SetAsLastSibling();
            if (mapNameText != null)
            {
                PlaceTopLeft(mapNameText.rectTransform, 12f, 6f, Mathf.Max(160f, searchWidth), 40f);
                mapNameText.transform.SetAsLastSibling();
            }
            if (reserveToggle)
                toggle.SetAsLastSibling();
            ClearSearchOverlaps(preview, search);
        }

        private static void FrameSearchField(RectTransform search)
        {
            if (search == null)
                return;
            Image border = search.GetComponent<Image>();
            if (border == null)
                border = search.gameObject.AddComponent<Image>();
            border.enabled = true;
            border.color = new Color(0.55f, 0.42f, 0.18f, 1f);
            border.raycastTarget = true;
            Transform existing = search.Find("SearchFill");
            Image fill = existing != null ? existing.GetComponent<Image>() : null;
            if (fill == null)
            {
                fill = new GameObject("SearchFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                fill.transform.SetParent(search, false);
            }

            Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);
            fill.color = new Color(0.05f, 0.07f, 0.08f, 1f);
            fill.raycastTarget = false;
            fill.transform.SetAsFirstSibling();
        }

        private static void ClearSearchOverlaps(RectTransform preview, RectTransform search)
        {
            if (preview == null || search == null)
                return;
            Vector3[] searchCorners = new Vector3[4];
            search.GetWorldCorners(searchCorners);
            Rect searchBounds = WorldBounds(searchCorners);
            Graphic[] graphics = preview.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || !graphic.enabled || graphic is TMP_Text)
                    continue;
                if (graphic.transform == search || graphic.transform.IsChildOf(search))
                    continue;
                if (IsUnderNamed(graphic.transform, "MapPreviewClip") ||
                    IsUnderNamed(graphic.transform, "LibraryGrid") ||
                    IsUnderNamed(graphic.transform, "LibraryPins") ||
                    IsUnderNamed(graphic.transform, "TheaterLabels") ||
                    IsUnderNamed(graphic.transform, "LibraryModeToggle"))
                    continue;
                string name = graphic.name;
                if (name == "PanelFill" || name == "OperationName")
                    continue;
                Rect bounds = GraphicBounds(graphic);
                if (!searchBounds.Overlaps(bounds))
                    continue;
                bool thin = bounds.width < 18f || bounds.height < 18f;
                bool frame = name.IndexOf("Rule", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Frame", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Shadow", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Glow", StringComparison.Ordinal) >= 0 ||
                    name.IndexOf("Edge", StringComparison.Ordinal) >= 0 ||
                    name == "SeedMetric";
                bool translucentPanel = graphic.color.a < 0.9f && graphic.color.a > 0.05f &&
                    bounds.width > 40f && bounds.height > 8f;
                if (thin || frame || translucentPanel)
                    graphic.enabled = false;
            }
        }

        private static bool IsUnderNamed(Transform transform, string ancestor)
        {
            Transform cursor = transform;
            while (cursor != null)
            {
                if (cursor.name == ancestor)
                    return true;
                cursor = cursor.parent;
            }

            return false;
        }

        private static Rect GraphicBounds(Graphic graphic)
        {
            Vector3[] corners = new Vector3[4];
            graphic.rectTransform.GetWorldCorners(corners);
            return WorldBounds(corners);
        }

        private static Rect WorldBounds(Vector3[] corners)
        {
            float minX = corners[0].x;
            float maxX = corners[0].x;
            float minY = corners[0].y;
            float maxY = corners[0].y;
            for (int i = 1; i < corners.Length; i++)
            {
                minX = Mathf.Min(minX, corners[i].x);
                maxX = Mathf.Max(maxX, corners[i].x);
                minY = Mathf.Min(minY, corners[i].y);
                maxY = Mathf.Max(maxY, corners[i].y);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private static void RaiseDepth(Transform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
                child.SetAsLastSibling();
        }

        private void EnsureDepthPlate(Transform composition)
        {
            if (composition == null)
                return;
            Transform screen = composition.parent != null ? composition.parent : composition;
            RawImage image = screen.Find("LibraryDepthPlate")?.GetComponent<RawImage>()
                ?? composition.Find("LibraryDepthPlate")?.GetComponent<RawImage>();
            if (image == null)
            {
                image = new GameObject("LibraryDepthPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                image.transform.SetParent(screen, false);
            }
            image.transform.SetParent(screen, false);
            image.transform.SetAsFirstSibling();
            Stretch(image.rectTransform);
            Transform canvasBlack = screen.Find("CanvasBlack");
            if (canvasBlack != null)
                canvasBlack.gameObject.SetActive(false);
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            string mapId = LibraryMapId;
            string key = battlefield ? "skirmish.blur." + mapId : "skirmish.blur.theater";
            Texture2D texture = SkirmishLibraryPreviewAssets.Load(key);
            if (texture == null && battlefield)
                texture = SkirmishLibraryPreviewAssets.Load("skirmish.plate." + mapId);
            if (texture == null)
                texture = SkirmishLibraryPreviewAssets.Load("skirmish.zone.theater");
            if (texture == null)
                texture = SkirmishLibraryPreviewAssets.Load("skirmish.blur.theater");
            image.texture = texture != null ? texture : SkirmishLibraryPreviewAssets.Placeholder();
            image.color = new Color(0.16f, 0.15f, 0.14f, 1f);
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            image.raycastTarget = false;
        }

        private static void EnsurePanelShadow(RectTransform panel)
        {
            if (panel == null || panel.parent == null)
                return;
            Transform glow = panel.parent.Find(panel.name + "Glow");
            if (glow != null)
                DestroyUi(glow.gameObject);

            RectTransform shadow = panel.parent.Find(panel.name + "Shadow") as RectTransform;
            if (shadow == null)
            {
                shadow = new GameObject(panel.name + "Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
                shadow.SetParent(panel.parent, false);
            }

            shadow.anchorMin = panel.anchorMin;
            shadow.anchorMax = panel.anchorMax;
            shadow.pivot = panel.pivot;
            shadow.anchoredPosition = panel.anchoredPosition + new Vector2(18f, -24f);
            shadow.sizeDelta = panel.sizeDelta + new Vector2(40f, 36f);
            PlaceBehind(shadow, panel);
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = false;
        }

        private static void PlaceBehind(RectTransform back, RectTransform front)
        {
            if (back == null || front == null || back.parent != front.parent)
                return;
            int frontIndex = front.GetSiblingIndex();
            int backIndex = back.GetSiblingIndex();
            int insert = backIndex < frontIndex ? frontIndex - 1 : frontIndex;
            back.SetSiblingIndex(Mathf.Max(0, insert));
        }

        private static void EnsurePanelFrame(RectTransform panel)
        {
            if (panel == null)
                return;
            Transform existing = panel.Find("DepthFrame");
            RectTransform frame = existing as RectTransform;
            if (frame == null)
            {
                frame = new GameObject("DepthFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic)).GetComponent<RectTransform>();
                frame.SetParent(panel, false);
            }

            Stretch(frame, 8f, 8f, 8f, 8f);
            frame.SetAsFirstSibling();
            V3GradientGraphic graphic = frame.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(new Color(0.05f, 0.08f, 0.09f, 0.94f), new Color(0.02f, 0.04f, 0.05f, 0.94f), new Color(0.62f, 0.54f, 0.32f, 0.85f), 1.5f);
        }

        private void EnsureFooterPlate(Transform composition)
        {
            if (composition == null || composition.Find("LibraryFooterPlate") != null)
                return;
            var plate = new GameObject("LibraryFooterPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(V3GradientGraphic)).GetComponent<RectTransform>();
            plate.SetParent(composition, false);
            V3GradientGraphic graphic = plate.GetComponent<V3GradientGraphic>();
            graphic.raycastTarget = false;
            graphic.Configure(new Color(0.04f, 0.07f, 0.08f, 0.96f), new Color(0.02f, 0.03f, 0.04f, 0.96f), new Color(0.93f, 0.78f, 0.38f, 1f), 2f);
            var shadow = new GameObject("LibraryFooterShadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            shadow.SetParent(composition, false);
            shadow.SetSiblingIndex(plate.GetSiblingIndex());
            Image image = shadow.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            image.raycastTarget = false;
        }

        private void PlaceFooterPlate(RectTransform composition)
        {
            if (composition == null)
                return;
            EnsureFooterPlate(composition);
            PinFooterBand(composition.Find("LibraryFooterShadow") as RectTransform, 18f, 6f, 112f);
            PinFooterBand(composition.Find("LibraryFooterPlate") as RectTransform, 16f, 10f, 108f);
            HideStrayChevrons(composition);
        }

        private static void PinFooterBand(RectTransform rect, float inset, float bottom, float tall)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(-inset * 2f, tall);
        }

        private static void ReserveFooterBand(Transform composition)
        {
            if (composition is not RectTransform compositionRect)
                return;
            float height = compositionRect.rect.height;
            if (height < 200f)
                return;
            const float footerBand = 136f;
            float width = compositionRect.rect.width;
            ShrinkAboveFooter(composition.Find("OperationPreview") as RectTransform, height, width, footerBand);
            ShrinkAboveFooter(composition.Find("BaseAssaultRules") as RectTransform, height, width, footerBand);
        }

        private static void ShrinkAboveFooter(RectTransform rect, float compositionHeight, float compositionWidth, float footerBand)
        {
            if (rect == null)
                return;
            float top = -rect.anchoredPosition.y;
            float left = rect.anchoredPosition.x;
            float maxHeight = compositionHeight - footerBand - top;
            float maxWidth = compositionWidth - 14f - left;
            float nextWidth = rect.sizeDelta.x;
            float nextHeight = rect.sizeDelta.y;
            if (maxHeight > 120f && nextHeight > maxHeight)
                nextHeight = maxHeight;
            if (maxWidth > 120f && nextWidth > maxWidth)
                nextWidth = maxWidth;
            rect.sizeDelta = new Vector2(nextWidth, nextHeight);
        }

        private static string MapDisplayTitle(string mapId, bool fa)
        {
            SkirmishBattleLibraryArt.Copy(string.IsNullOrEmpty(mapId) ? "DB" : mapId, fa, out string title, out _);
            return title;
        }

        private static Color RegionZoneColor(string mapId)
        {
            switch ((mapId ?? string.Empty).ToUpperInvariant())
            {
                case "CC": return new Color(0.25f, 0.78f, 0.82f, 1f);
                case "MP": return new Color(0.42f, 0.78f, 0.38f, 1f);
                case "IB": return new Color(0.90f, 0.48f, 0.18f, 1f);
                case "AP": return new Color(0.32f, 0.58f, 0.92f, 1f);
                default: return new Color(0.93f, 0.68f, 0.22f, 1f);
            }
        }

        private static void HideStrayChevrons(Transform root)
        {
            if (root == null)
                return;
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null || children[i].name.IndexOf("Chevron", StringComparison.Ordinal) < 0)
                    continue;
                children[i].gameObject.SetActive(false);
            }
        }

        private Button EnsureFooterUtility(Transform parent, string name)
        {
            if (parent == null)
                return null;
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<Button>();
            Button button = CreateChromeButton(parent, name);
            var label = CreateLabel(button.transform, "Label", 18f);
            Stretch(label.rectTransform, 48f, 12f, 8f, 8f);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private void PresentRecommendedPreview(string scenarioId)
        {
            if (_recommendedPanel == null)
                return;
            VerticalLayoutGroup recommendedGroup = _recommendedPanel.GetComponent<VerticalLayoutGroup>();
            if (recommendedGroup != null)
                recommendedGroup.enabled = false;
            Image[] images = _recommendedPanel.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].name != "PreviewFrame" && SkirmishLibraryBlankImage.IsBlank(images[i]))
                    images[i].enabled = false;
            }

            RawImage preview = _recommendedPanel.Find("Preview")?.GetComponent<RawImage>();
            string key = string.IsNullOrEmpty(scenarioId) ? null : "skirmish.preview." + scenarioId;
            Texture2D texture = string.IsNullOrEmpty(key) ? null : SkirmishLibraryPreviewAssets.Load(key);
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float panelWidth = _recommendedPanel.rect.width > 80f ? _recommendedPanel.rect.width : _recommendedPanel.sizeDelta.x;
            if (panelWidth < 80f)
                panelWidth = 360f;
            bool showPreview = texture != null;
            if (!showPreview)
            {
                if (preview != null)
                    preview.gameObject.SetActive(false);
            }
            else
            {
                if (preview == null)
                {
                    preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                    preview.transform.SetParent(_recommendedPanel, false);
                }

                preview.gameObject.SetActive(true);
                preview.texture = texture;
                preview.color = Color.white;
                preview.raycastTarget = false;
                preview.enabled = true;
                float previewX = fa ? Mathf.Max(8f, panelWidth - 128f) : 8f;
                PlaceTopLeft(preview.rectTransform, previewX, 36f, 112f, 64f);
                CoverCrop(preview, 112f, 64f);
            }

            float textX = showPreview && !fa ? 128f : 8f;
            float textWidth = showPreview ? Mathf.Max(80f, panelWidth - 144f) : Mathf.Max(80f, panelWidth - 16f);
            if (_recommendedPanel.Find("Heading") is RectTransform heading)
            {
                PlaceTopLeft(heading, textX, 4f, textWidth, 28f);
                TMP_Text headingText = heading.GetComponent<TMP_Text>();
                if (headingText != null)
                {
                    headingText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    headingText.isRightToLeftText = fa;
                    headingText.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            if (_recommendedPanel.Find("Body") is RectTransform body)
            {
                PlaceTopLeft(body, textX, 34f, textWidth, 80f);
                TMP_Text bodyText = body.GetComponent<TMP_Text>();
                if (bodyText != null)
                {
                    bodyText.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                    bodyText.isRightToLeftText = fa;
                    bodyText.textWrappingMode = TextWrappingModes.Normal;
                    bodyText.overflowMode = TextOverflowModes.Ellipsis;
                }
            }
        }

        private void PresentLaunchTransition(string scenarioId)
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition == null)
                return;
            Transform existing = composition.Find("LaunchTransition");
            RectTransform plate = existing as RectTransform;
            if (plate == null)
            {
                var graphic = new GameObject("LaunchTransition", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                graphic.transform.SetParent(composition, false);
                graphic.color = new Color(0.07f, 0.08f, 0.09f, 0.96f);
                graphic.raycastTarget = false;
                plate = graphic.rectTransform;
                var label = CreateLabel(plate, "LaunchLabel", 22f);
                Stretch(label.rectTransform, 16f, 12f, 8f, 8f);
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            }

            Image plateImage = plate.GetComponent<Image>();
            if (plateImage != null)
                plateImage.color = new Color(0.07f, 0.08f, 0.09f, 0.96f);
            RectTransform compositionRect = composition as RectTransform;
            float compositionWidth = compositionRect != null && compositionRect.rect.width > 200f
                ? compositionRect.rect.width
                : 1672f;
            float bannerWidth = Mathf.Clamp(compositionWidth * 0.28f, 280f, 440f);
            PlaceTopLeft(plate, (compositionWidth - bannerWidth) * 0.5f, 8f, bannerWidth, 52f);
            plate.gameObject.SetActive(true);
            RaiseLaunchTransition();
            TMP_Text text = plate.Find("LaunchLabel")?.GetComponent<TMP_Text>();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            SetCopy(text, fa ? "در حال اعزام " + scenarioId : "LAUNCHING " + scenarioId, 16f, 24f);
            if (text != null)
            {
                Stretch(text.rectTransform, 16f, 12f, 8f, 8f);
                text.alignment = TextAlignmentOptions.Center;
                text.isRightToLeftText = false;
                text.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void RaiseLaunchTransition()
        {
            Transform transition = transform.Find("SkirmishSetupComposition/LaunchTransition");
            if (transition != null && transition.gameObject.activeSelf)
                transition.SetAsLastSibling();
        }

        private void PlaceLibraryHeader()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition is not RectTransform compositionRect)
                return;
            float width = compositionRect.rect.width;
            if (width < 200f)
                width = compositionRect.sizeDelta.x > 200f ? compositionRect.sizeDelta.x : 1672f;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float settingsWidth = 72f;
            bool showChip = HasCommanderIdentity();
            float chipWidth = showChip ? Mathf.Clamp(width * 0.18f, 210f, 280f) : 0f;
            float logoWidth = Mathf.Clamp(width * 0.16f, 160f, 280f);
            const float gap = 8f;
            float settingsX = width - 12f - settingsWidth;
            float chipX = settingsX - gap - chipWidth;
            float titleX = 12f + logoWidth + gap;
            float titleWidth = Mathf.Max(140f, chipX - gap - titleX);
            if (composition.Find("WarlineLogo") is RectTransform logo)
            {
                logo.gameObject.SetActive(true);
                PlaceTopLeft(logo, 12f, 8f, logoWidth, 80f);
            }

            if (composition.Find("ScreenTitlePanel") is RectTransform title)
            {
                PlaceTopLeft(title, titleX, 10f, titleWidth, 78f);
                if (title.Find("ScreenTitle") is RectTransform screenTitle)
                {
                    screenTitle.anchorMin = Vector2.zero;
                    screenTitle.anchorMax = Vector2.one;
                    screenTitle.offsetMin = new Vector2(fa ? 8f : 52f, 4f);
                    screenTitle.offsetMax = new Vector2(-8f, -4f);
                    TMP_Text screenTitleText = screenTitle.GetComponent<TMP_Text>();
                    if (screenTitleText != null)
                    {
                        screenTitleText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                        screenTitleText.isRightToLeftText = fa;
                        screenTitleText.textWrappingMode = TextWrappingModes.NoWrap;
                        screenTitleText.overflowMode = TextOverflowModes.Ellipsis;
                        screenTitleText.enableAutoSizing = true;
                        screenTitleText.fontSizeMin = 16f;
                        screenTitleText.fontSizeMax = 32f;
                    }
                }
            }

            Transform settings = FindDeep(transform, "SettingsButton");
            if (settings is RectTransform settingsRect)
            {
                if (settings.parent != composition)
                    settings.SetParent(composition, false);
                settings.gameObject.SetActive(true);
                PlaceTopLeft(settingsRect, settingsX, 12f, settingsWidth, 72f);
            }

            MainMenuAccountHeaderView header = GetComponentInChildren<MainMenuAccountHeaderView>(true);
            if (header == null)
                return;
            header.enabled = false;
            header.gameObject.SetActive(showChip);
            if (!showChip)
                return;
            if (header.transform is RectTransform chip)
            {
                if (chip.parent != composition)
                    chip.SetParent(composition, false);
                PlaceTopLeft(chip, chipX, 12f, chipWidth, 72f);
                Image chipImage = chip.GetComponent<Image>();
                if (chipImage != null)
                {
                    chipImage.enabled = true;
                    if (chipImage.color.a < 0.98f || SkirmishLibraryBlankImage.IsLightNeutral(chipImage.color))
                        chipImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                }
            }

            if (header.transform.Find("Icon") is RectTransform icon)
                PlaceTopLeft(icon, 8f, 16f, 40f, 40f);
            BindCommanderChip(header, fa, chipWidth);
        }

        private static bool HasCommanderIdentity()
        {
            if (!UiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile))
                return false;
            return !string.IsNullOrWhiteSpace(profile.Name);
        }

        private void BindCommanderChip(MainMenuAccountHeaderView header, bool fa, float chipWidth)
        {
            if (header == null || !UiShellRuntimeGateway.TryReadCommanderProfile(out UiShellCommanderProfileModel profile))
                return;
            string name = profile.Name ?? string.Empty;
            if (string.Equals(name, "Commander", StringComparison.OrdinalIgnoreCase) && fa)
                name = "فرمانده";
            Transform label = header.transform.Find("Label");
            if (label != null)
                label.gameObject.SetActive(true);
            if (label is RectTransform labelRect)
                PlaceTopLeft(labelRect, 54f, 6f, Mathf.Max(72f, chipWidth - 66f), 26f);
            TMP_Text caption = label != null ? label.GetComponent<TMP_Text>() : null;
            if (caption != null)
            {
                SetCopy(caption, name, 13f, 18f);
                caption.alignment = ContainsArabicScript(name) ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                caption.isRightToLeftText = ContainsArabicScript(caption.text);
                caption.overflowMode = TextOverflowModes.Ellipsis;
                caption.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            }

            if (header.Value == null)
                return;
            PlaceTopLeft(header.Value.rectTransform, 54f, 32f, Mathf.Max(72f, chipWidth - 66f), 32f);
            string level = "Lv " + profile.Level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            SetCopy(header.Value, level, 14f, 20f);
            header.Value.isRightToLeftText = false;
            header.Value.alignment = TextAlignmentOptions.MidlineLeft;
            header.Value.overflowMode = TextOverflowModes.Overflow;
            header.Value.text = level;
        }

        private void FinishLibraryText()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition == null)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            TMP_Text[] texts = composition.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || text.transform.parent == null)
                    continue;
                string parentName = text.transform.parent.name;
                bool subtitle = text.name == "Subtitle";
                bool zoneTab = text.name == "Label" && parentName.StartsWith("BattlefieldTab_", StringComparison.Ordinal);
                bool zoneRail = text.name == "Label" && parentName.StartsWith("ObjectiveTab_", StringComparison.Ordinal);
                bool gridRow = text.name.StartsWith("Row_", StringComparison.Ordinal);
                bool columnHeader = text.name.StartsWith("Column_", StringComparison.Ordinal);
                bool statCaption = text.name == "Caption" &&
                    (parentName == "Objective" || parentName == "Time" || parentName == "Size" || parentName == "Army" || parentName == "Start");
                bool medalCaption = text.name == "Caption" && parentName.StartsWith("Slot", StringComparison.Ordinal);
                bool stripCaption = text.name == "Caption" && parentName.StartsWith("Medal", StringComparison.Ordinal);
                bool difficulty = parentName.StartsWith("DifficultyChoice_", StringComparison.Ordinal);
                bool zoneName = (text.name == "ZoneName" || text.name == "ZoneCount") && parentName == "TheaterLabels";
                bool recommended = parentName == "RecommendedNext";
                Button footerOwner = text.transform.parent.GetComponent<Button>();
                bool footer = footerOwner != null && (
                    footerOwner == resetButton ||
                    footerOwner == launchButton ||
                    footerOwner == randomizeSeedButton ||
                    footerOwner == _libraryFavouritesButton ||
                    footerOwner == _libraryAdvancedButton ||
                    footerOwner.name == "BackButton");
                if (subtitle)
                    RelaxLibraryText(text, 11f, 14f, true);
                else if (zoneTab || gridRow)
                    RelaxLibraryText(text, 11f, 16f, true);
                else if (zoneRail)
                {
                    RelaxLibraryText(text, 11f, 16f, true);
                    text.overflowMode = TextOverflowModes.Overflow;
                }
                else if (statCaption)
                {
                    RelaxLibraryText(text, 11f, 16f, true);
                    text.overflowMode = TextOverflowModes.Overflow;
                }
                else if (columnHeader)
                {
                    RelaxLibraryText(text, 12f, 15f, true);
                    text.alignment = TextAlignmentOptions.Center;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                }
                else if (medalCaption || stripCaption)
                {
                    RelaxLibraryText(text, 11f, 15f, true);
                    text.overflowMode = TextOverflowModes.Overflow;
                }
                else if (difficulty)
                    RelaxLibraryText(text, 12f, 18f, true);
                else if (zoneName)
                    RelaxLibraryText(text, 14f, 26f, false);
                else if (recommended)
                    RelaxLibraryText(text, 12f, 18f, text.name == "Body");
                else if (footer)
                {
                    RelaxLibraryText(text, 11f, 20f, text.rectTransform.rect.width < 150f);
                    text.alignment = TextAlignmentOptions.Center;
                    text.isRightToLeftText = ContainsArabicScript(text.text);
                }
                else if (text.name == "Value" &&
                    (parentName == "Objective" || parentName == "Army" || parentName == "Start" || parentName == "Time" || parentName == "Size"))
                {
                    RelaxLibraryText(text, 13f, 20f, true);
                    text.overflowMode = TextOverflowModes.Overflow;
                }
                else if (text.name == "Count" && parentName.StartsWith("ObjectiveTab_", StringComparison.Ordinal))
                {
                    text.isRightToLeftText = false;
                    text.overflowMode = TextOverflowModes.Overflow;
                }
            }

            if (scenarioDescription != null)
            {
                scenarioDescription.overflowMode = fa ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
                scenarioDescription.margin = fa ? new Vector4(16f, 6f, 28f, 6f) : new Vector4(16f, 6f, 14f, 6f);
                scenarioDescription.fontSizeMin = 15f;
            }
        }

        private static void RelaxLibraryText(TMP_Text text, float min, float max, bool wrap)
        {
            if (text == null)
                return;
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = Mathf.Max(min, max);
            if (text.fontSize < min)
                text.fontSize = min;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static bool ContainsArabicScript(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char letter = value[i];
                if (letter >= '\u0600' && letter <= '\u06FF')
                    return true;
                if (letter >= '\u0750' && letter <= '\u077F')
                    return true;
                if (letter >= '\uFB50' && letter <= '\uFDFF')
                    return true;
                if (letter >= '\uFE70' && letter <= '\uFEFF')
                    return true;
            }

            return false;
        }

        private void ClampStrayMedalBars()
        {
            Transform composition = transform.Find("SkirmishSetupComposition");
            if (composition == null)
                return;
            Image[] images = composition.GetComponentsInChildren<Image>(true);
            TMP_Text[] texts = composition.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!SkirmishLibraryBlankImage.IsNarrowBar(image))
                    continue;
                if (SkirmishLibraryBlankImage.IsMedalArt(image))
                {
                    SquareMedal(image);
                    continue;
                }

                if (!SkirmishLibraryBlankImage.IsGreyTint(image))
                    continue;
                if (OverlapsVisibleText(image.rectTransform, texts))
                    image.enabled = false;
            }
        }

        private static void SquareMedal(Image image)
        {
            if (image == null)
                return;
            RectTransform rect = image.rectTransform;
            RectTransform parent = rect.parent as RectTransform;
            const float size = 24f;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            if (parent == null)
            {
                rect.sizeDelta = new Vector2(size, size);
                return;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 topLeft = parent.InverseTransformPoint(corners[1]);
            Rect parentRect = parent.rect;
            float x = topLeft.x - parentRect.xMin;
            float y = parentRect.yMax - topLeft.y;
            PlaceTopLeft(rect, Mathf.Max(0f, x), Mathf.Max(0f, y), size, size);
        }

        private static bool OverlapsVisibleText(RectTransform rect, TMP_Text[] texts)
        {
            if (rect == null || texts == null)
                return false;
            for (int i = 0; i < texts.Length; i++)
            {
                if (SkirmishLibraryBlankImage.OverlapsText(rect, texts[i]))
                    return true;
            }

            return false;
        }

        private static Sprite LibraryMedallion()
        {
            if (_libraryMedallion != null)
                return _libraryMedallion;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    if (distance > 1f)
                        continue;
                    bool ring = distance > 0.72f;
                    bool core = distance < 0.34f;
                    pixels[y * size + x] = ring
                        ? new Color32(255, 236, 196, 255)
                        : core
                            ? new Color32(255, 255, 255, 255)
                            : new Color32(255, 214, 120, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _libraryMedallion = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _libraryMedallion.hideFlags = HideFlags.HideAndDontSave;
            return _libraryMedallion;
        }

        private static Sprite ZoneGlowSprite()
        {
            if (_zoneGlow != null)
                return _zoneGlow;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    if (distance > 1f)
                        continue;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) / 0.22f);
                    float halo = Mathf.Clamp01(1f - distance) * 0.35f;
                    byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(ring, halo) * 255f), 0, 255);
                    pixels[y * size + x] = new Color32(255, 236, 180, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneGlow = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneGlow.hideFlags = HideFlags.HideAndDontSave;
            return _zoneGlow;
        }

        private static void ClearFooterIcon(Button button)
        {
            if (button == null)
                return;
            Transform existing = button.transform.Find("LibraryIcon");
            if (existing != null)
                DestroyUi(existing.gameObject);
        }

        private static void HideDiceIcon(Button button)
        {
            if (button == null)
                return;
            Transform icon = button.transform.Find("Icon");
            if (icon != null && icon.name != "LibraryIcon")
                icon.gameObject.SetActive(false);
            Transform[] children = button.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] == null)
                    continue;
                if (children[i].name.StartsWith("Dot_", StringComparison.Ordinal) || children[i].name == "DiceIcon")
                    children[i].gameObject.SetActive(false);
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
