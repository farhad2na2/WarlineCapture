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
        private const float LibraryGridCardGap = 6f; // v37 approved-3 framed cells (was 3)
        private const float LibraryGridRightPad = 6f;
        private const float LibraryGridOriginX = 112f; // v37 approved-3 row tiles (was 84)
        private const float LibraryGridRowGap = 6f; // v37 approved-3 framed cells (was 2)
        private const float LibraryGridTitleHeight = 28f;
        private const float LibraryGridSubtitleHeight = 44f;
        private const float LibraryGridMedalSize = 14f;
        private const float LibraryGridTextGap = 2f;
        private const float LibraryGridMedalGap = 1f;
        private const float LibraryGridCardBottom = 4f;
        private const float LibraryGridContentTop = 2f;
        private const float LibraryGridContentBottom = 6f;
        private const float LibraryGridPreviewFloor = 0.40f;
        private const float LibraryGridRoomyCardWidth = 230f;
        private const float LibraryGridRoomyTextWidth = 210f;
        private const float LibraryGridRoomyTitleHeight = 24f;
        private const float LibraryGridRoomySubtitleHeight = 24f;
        private const float LibraryGridRoomyHeaderHeight = 40f;
        private const float LibraryPinSubtitleHeight = 36f;
        private const int LibraryGridLogicalColumns = 6;
        // Cover focal is the midpoint of the district anchors (y 0.15–0.78) on the 16:9 painting.
        private const float TheatreFocalX = 0.50f;
        private const float TheatreFocalY = 0.465f;
        private const float TheatreSafeLeft = 28f;
        private const float TheatreSafeTop = 28f;
        private const float TheatreSafeRight = 28f;
        private const float TheatreSafeBottom = 40f;
        private const float ZoneCardWidth = 320f;
        private const float ZoneCardHeight = 104f;
        private const float ZoneThumbSize = 96f;
        private const float ZonePinSize = 34f;
        private const float ZoneCueWidth = 160f;
        private const float ZoneCueHeight = 28f;
        private const float ZoneStackGap = 6f;
        private const float ZoneHexPad = 6f;
        private const float ZoneCardGap = 12f;
        // Above the 12px capture check. A 20:9 notch scales the frame by 0.91, and 14 × 0.91 still clears 12px.
        private const float ZoneSeparateGap = 14f;
        private const float ZoneFrameInset = 12f;

        /// <summary>
        /// One theatre district after the painting's cover transform. Polygon points are screen pixels
        /// in the map clip (y down) and stay on the painting's uniform scale.
        /// </summary>
        private struct TheatreZoneSlot
        {
            public string MapId;
            public Color Zone;
            public bool Selected;
            public int Won;
            public int Playable;
            public Vector2 Anchor;
            public Vector2 CardAnchor;
            public Rect Pin;
            public Rect Cue;
            public Rect Card;
            public Vector2[] Hex;
        }

        private struct GridCardSlots
        {
            public float Preview;
            public float Title;
            public float Subtitle;
            public float Medal;
        }

        private string _armyFilter = string.Empty;
        private string _startFilter = string.Empty;
        private bool _missingMedalOnly;
        private bool _filtersOpen;
        private Button _libraryAdvancedButton;
        private static Sprite _libraryMedallion;
        private static Sprite _zoneMedallion;
        private static Sprite _zoneGlow;
        private static Sprite _zoneRibbon;
        private static Sprite _zoneStroke;
        private static Sprite _zoneSolid;
        private static Sprite _zoneCap;
        private static Sprite _zoneRing;
        private static Sprite _zoneDisc;
        private static Sprite _solidSprite;
        private static Sprite _softShadow;
        private float _sideWonRatio;
        private string _recommendedScenarioId = string.Empty;
        private bool _advancedOpen;
        private bool _librarySearchApplied;
        private bool _libraryChromeReady;
        private bool _libraryPresenting;
        private bool _refittingGrid;

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
            _medalRow = AdoptBriefingWidget(rules, "MedalRow");
            _difficultyRow = AdoptBriefingWidget(rules, "MedalDifficultySelector");
            _recommendedPanel = AdoptBriefingWidget(rules, "RecommendedNext");
            EnsureMedalSlots(_medalRow);
            EnsureDifficultyButtons(_difficultyRow);
            EnsureRecommended(_recommendedPanel);
            RemoveRetiredFooterUtility(composition, "LibraryFavouritesButton");
            _libraryAdvancedButton = EnsureFooterUtility(composition, "LibraryAdvancedButton");
            EnsureStoreButton(rules);
            SetActive(FindModeToggle(), false);
            EnsureFilterPanel(_libraryFilters);
            EnsureLibraryPass(composition, preview, rules);
            if (missionSizeSegmented != null && rules.Find("BriefingColumn") is RectTransform briefingColumn)
                missionSizeSegmented.transform.SetParent(briefingColumn, false);
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
                case "saga":
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
            if (!baseAssaultPreset || _libraryPresenting)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            _libraryPresenting = true;
            try
            {
                // Chrome creation changes rects synchronously. Defer layout callbacks
                // until the district cards and zoom cue have both been installed.
                EnsureBattleLibraryChrome();
                if (_libraryPins == null && _libraryRegions == null)
                    return;
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
                bool grid = false; // Saga is the only battlefield presentation; legacy widgets remain for compatibility.
                SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary"), false);
                SetActive(transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryCount"), false);
                SetActive(battleLibraryMapTabs, false);
                HideMapCollectionTabs();
                SetActive(_libraryRegions, !battlefield);
                // The detail panel is reparented to the plate when seated. It must
                // follow the level too, otherwise its labels leak around the road.
                SetActive(FindTheatreDetailPanel(transform), !battlefield);
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
                if (!battlefield)
                    PresentRegions(theater, fa);
                SetActive(FindModeToggle(), false);
                if (battlefield && !grid)
                {
                    PresentObjectiveColumn(field, fa);
                    PresentSaga(field, fa);
                }
                if (grid)
                    PresentGrid(field, fa);
                PresentBriefingChrome(theater, field, briefing, fa, battlefield);
                string pinMap = string.IsNullOrEmpty(field.MapId) ? theater.SelectedMapId : field.MapId;
                bool hasPin = TryFindTheaterPin(theater, pinMap, out SkirmishTheaterPinState selectedPin);
                PresentProgress(theater, fa);
                PresentTheatreSummary();
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
                ApplyBriefingColumnState();
                PublishMedalCounts(theater);
                ApplySagaStyle();
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
            RectTransform clip = map.rectTransform.parent as RectTransform;
            if (!TryFrameSize(clip, out float plateWidth, out float plateHeight))
            {
                plateWidth = 1064f;
                plateHeight = 559f;
            }

            if (!battlefield)
                PresentTheatrePlate(map, plateWidth, plateHeight);
            else
                PresentBattlefieldPlate(map, field, plateWidth, plateHeight);
            AspectRatioFitter fitter = map.GetComponent<AspectRatioFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }


        /// <summary>
        /// Cover-crops the theatre painting. The window is the same one the district overlays use,
        /// so a wide frame crops top and bottom around the district anchors instead of letterboxing.
        /// </summary>


        /// <summary>
        /// Keeps the visible part of a district polygon inside the map frame. Vertices stay on the
        /// cover-fitted painting; only the cropped edge is cut, so the shape is not slid off the art.
        /// </summary>


        /// <summary>
        /// Sits each card on the legal cell nearest its anchor. Legal means clear of every hex,
        /// clear of every pin, and at least <see cref="ZoneSeparateGap"/> from cards already seated.
        /// Industrial Basin and Airfield Plains seat first so they keep the ground beside their landmarks.
        /// </summary>


        /// <summary>
        /// Pushes the closest pair until every card pair has the capture clearance
        /// (the separating axis, or the smaller gap when both axes separate) of at least
        /// <see cref="ZoneSeparateGap"/>. A move is kept only when it reduces that set
        /// without covering another pin.
        /// </summary>


        /// <summary>mode 0 splits the deficit, 1 moves only the lesser card, 2 moves only the greater card.</summary>

        /// <summary>
        /// Overlaps the short axis by 1px when the other axis is already clear, so the capture
        /// clearance becomes that clear axis. One pixel stays inside the 2px overlap slop.
        /// </summary>


        private static string StripSpriteTags(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf("<sprite", StringComparison.OrdinalIgnoreCase) < 0)
                return value;
            var builder = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (i + 7 <= value.Length && string.Compare(value, i, "<sprite", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    int end = value.IndexOf('>', i);
                    if (end < 0)
                        break;
                    i = end;
                    continue;
                }

                builder.Append(value[i]);
            }

            return builder.ToString();
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
                plateRect.offsetMin = new Vector2(theater ? 0f : 8f, 8f);
                plateRect.offsetMax = new Vector2(theater ? 0f : -8f, theater ? -8f : -56f);
                MaskRect(plateRect);
            }

            if (_overallStrip != null && preview is RectTransform previewRect)
            {
                _overallStrip.SetParent(previewRect, false);
                _overallStrip.anchorMin = new Vector2(0f, 0f);
                _overallStrip.anchorMax = new Vector2(1f, 0f);
                _overallStrip.pivot = new Vector2(0.5f, 0f);
                _overallStrip.anchoredPosition = new Vector2(0f, 8f);
                _overallStrip.sizeDelta = new Vector2(-16f, 88f);
            }

            if (_libraryPins is RectTransform pinRect)
            {
                pinRect.anchorMin = Vector2.zero;
                pinRect.anchorMax = Vector2.one;
                pinRect.offsetMin = new Vector2(PinRailReserve, 8f);
                pinRect.offsetMax = new Vector2(-8f, -8f);
            }

            if (battleLibrarySearch != null && theater)
                battleLibrarySearch.gameObject.SetActive(false);
            if (battleLibrarySearch != null && !theater)
            {
                battleLibrarySearch.gameObject.SetActive(true);
                RectTransform search = battleLibrarySearch.GetComponent<RectTransform>();
                if (preview != null && search.parent != preview)
                    search.SetParent(preview, false);
                RectMask2D searchMask = search.GetComponent<RectMask2D>();
                if (searchMask != null)
                    searchMask.enabled = false;
                PlaceTopLeft(search, 12f, 8f, Mathf.Max(240f,(preview as RectTransform).rect.width-24f), 44f);
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
                mapNameText.gameObject.SetActive(false);
                if (!theater)
                {
                    PlaceTopLeft(mapNameText.rectTransform, 12f, 6f, 680f, 40f);
                    mapNameText.textWrappingMode = TextWrappingModes.NoWrap;
                    mapNameText.overflowMode = TextOverflowModes.Overflow;
                }
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
                grid.offsetMax = new Vector2(-8f, -GridTopOffset());
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


        private void MirrorLibraryPanels()
        {
            ApplyLibraryFrame(LibraryBriefingOnLeft());
        }

        /// <param name="briefingOnLeftOverride">Null keeps the default side (left in LTR, right in RTL).</param>
        /// <summary>v46: px the 4:3 Theatre briefing column gives the map so P2's detail reserve holds.</summary>
        private const float TheatreFourByThreeColumnNarrow = 40f;

        /// <summary>v46: the 4:3 Theatre briefing column never drops below this (medal tiles stay >= 96 px).</summary>
        private const float TheatreBriefingMinWidth = 300f;

        /// <summary>v46: Game PM floor, the 4:3 Theatre map keeps at least this share of the safe width.</summary>
        private const float TheatreMapMinShare = 0.60f;

        private void ApplyLibraryFrame(bool? briefingOnLeftOverride = null)
        {
            RectTransform composition = transform.Find("SkirmishSetupComposition") as RectTransform;
            var map = transform.Find("SkirmishSetupComposition/OperationPreview") as RectTransform;
            var briefing = transform.Find("SkirmishSetupComposition/BaseAssaultRules") as RectTransform;
            if (composition == null || map == null || briefing == null)
                return;
            FillComposition(composition);
            bool briefingOnLeft = briefingOnLeftOverride ?? !UiShellRuntimeGateway.Localization.IsRightToLeft;
            bool theater = LibraryLevel != SkirmishLibraryLevel.Battlefield;
            SkirmishLibraryFrame.Resolve(
                composition.rect.width,
                composition.rect.height,
                briefingOnLeft,
                out float mapX,
                out float mapWidth,
                out float briefingX,
                out float briefingWidth,
                out float panelY,
                out float panelHeight,
                theater);
            // v46 (Game PM, approved-1 keeps the district panel at 4:3): P2's seating only reserves the trailing detail
            // column when every portrait seats (TheatreSeatGeometry.DetailReserveOn); the 4:3 plate was 1039.6 px, just
            // under it. The briefing column gives the map 40 px (plate ~1080, where the reserve holds for the default
            // district in en and fa). P2's capture check now reads the live split ([MapPanelWidth]); the map stays at
            // or above 60% of the safe width.
            float theatreNarrow = 0f;
            float safeWidth = composition.rect.width;
            if (theater && composition.rect.height > 1f && safeWidth / composition.rect.height < 1.6f)
            {
                theatreNarrow = Mathf.Min(TheatreFourByThreeColumnNarrow, Mathf.Max(0f, briefingWidth - TheatreBriefingMinWidth));
                if (mapWidth + theatreNarrow < TheatreMapMinShare * safeWidth)
                    theatreNarrow = Mathf.Max(theatreNarrow, TheatreMapMinShare * safeWidth - mapWidth);
                briefingWidth -= theatreNarrow;
                mapWidth += theatreNarrow;
                if (briefingOnLeft)
                    mapX -= theatreNarrow;
                else
                    briefingX += theatreNarrow;
            }

            SkirmishLibraryFrame.CloseTheatreSeam(
                theater,
                briefingOnLeft,
                ref mapX,
                ref mapWidth,
                ref briefingX,
                briefingWidth);
            PlaceTopLeft(map, mapX, panelY, mapWidth, panelHeight);
            PlaceTopLeft(briefing, briefingX, panelY, briefingWidth, panelHeight);
            if (theater)
                Debug.Log("[TheatreColumns] composition=" + composition.rect.width.ToString("0") + "x" + composition.rect.height.ToString("0") +
                    " briefing=" + briefingWidth.ToString("0.0") + " map=" + mapWidth.ToString("0.0") +
                    " narrow=" + theatreNarrow.ToString("0") + " mapShare=" + (composition.rect.width > 1f ? mapWidth / composition.rect.width : 0f).ToString("0.000") + " side=" + (briefingOnLeft ? "left" : "right"));
            EnsureOpaquePanel(map);
            EnsureOpaquePanel(briefing);
            RectTransform mapShadow = EnsureSoftShadow(map, 8f, 14f);
            RectTransform briefingShadow = EnsureSoftShadow(briefing, briefingOnLeft ? 16f : -16f, 14f);
            if (!theater && briefingShadow != null && briefingShadow.GetSiblingIndex() < map.GetSiblingIndex())
            {
                briefingShadow.SetSiblingIndex(map.GetSiblingIndex() + 1);
                briefing.SetSiblingIndex(briefingShadow.GetSiblingIndex() + 1);
            }
            if (mapShadow != null)
                mapShadow.SetSiblingIndex(map.GetSiblingIndex());
            FitLibrarySearch();
            SealTheatreMapEdge(map, briefing, theater);
        }

        /// <summary>
        /// The EN 16:9 dark strip is the 14px gap plus the briefing drop-shadow feather and the map border.
        /// Theatre closes that gap, drops the border and the readability veil, and keeps shadows behind the map.
        /// </summary>


        private static void FillComposition(RectTransform composition)
        {
            if (composition == null)
                return;
            SkirmishLibraryFrame.FitComposition(composition);
            composition.GetComponent<SkirmishLibrarySafeFrame>()?.RefreshLiveInset();
            Canvas.ForceUpdateCanvases();
        }


        private float ReferenceSubtitleFloor()
        {
            float scale = 1f;
            MainMenuV3SectionLayoutView layout = GetComponent<MainMenuV3SectionLayoutView>()
                ?? GetComponentInParent<MainMenuV3SectionLayoutView>()
                ?? GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout != null && layout.LastAppliedScale > 0.05f)
                scale = layout.LastAppliedScale;
            return Mathf.Max(22f, 17f / scale);
        }

        private RectTransform EnsureSoftShadow(RectTransform panel, float offsetX, float offsetY)
        {
            if (panel == null || panel.parent == null)
                return null;
            string name = panel.name + "DropShadow";
            var shadow = panel.parent.Find(name) as RectTransform;
            Image image = shadow != null ? shadow.GetComponent<Image>() : null;
            if (shadow == null)
            {
                image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                shadow = image.rectTransform;
                shadow.SetParent(panel.parent, false);
            }

            if (image == null)
                image = shadow.gameObject.AddComponent<Image>();
            image.sprite = SoftShadowSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0f, 0f, 0f, 0.62f);
            image.raycastTarget = false;
            image.enabled = true;
            float panelTop = -panel.anchoredPosition.y;
            PlaceTopLeft(shadow, panel.anchoredPosition.x + offsetX - 12f, panelTop + offsetY - 8f, panel.sizeDelta.x + 32f, panel.sizeDelta.y + 36f);
            int panelIndex = panel.GetSiblingIndex();
            if (shadow.GetSiblingIndex() > panelIndex)
                shadow.SetSiblingIndex(panelIndex);
            return shadow;
        }

        private static Sprite SoftShadowSprite()
        {
            if (_softShadow != null)
                return _softShadow;
            const int size = 64;
            const int inset = 18;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x < inset ? inset - x : x >= size - inset ? x - (size - inset - 1) : 0f;
                    float dy = y < inset ? inset - y : y >= size - inset ? y - (size - inset - 1) : 0f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = distance <= 0f ? 1f : Mathf.Clamp01(1f - distance / inset);
                    alpha *= alpha;
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _softShadow = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(inset, inset, inset, inset));
            _softShadow.hideFlags = HideFlags.HideAndDontSave;
            return _softShadow;
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
            // The plate now fills the old 128px band. Keep the hint on the bottom edge ZoomHintRect clears.
            const float clipBottom = 8f;
            rect.anchoredPosition = new Vector2(36f, clipBottom + 10f);
            rect.sizeDelta = new Vector2(-36f, 36f);
            rect.SetAsLastSibling();
            hint.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            hint.isRightToLeftText = fa;
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            hint.overflowMode = TextOverflowModes.Overflow;
            // Autosize with Overflow was drawing the Farsi line at 30px and clipping بزرگنمایی.
            // Lock 22, the library floor, on one line. The rect is the preview width.
            hint.enableAutoSizing = false;
            hint.fontSizeMin = LibraryFontFloor;
            hint.fontSizeMax = LibraryFontFloor;
            hint.fontSize = LibraryFontFloor;
            SetCopy(hint, fa ? "برای بزرگنمایی لمس یا جمع کن" : "Tap or pinch to zoom", LibraryFontFloor, LibraryFontFloor);
            hint.overflowMode = TextOverflowModes.Overflow;
            hint.enableAutoSizing = false;
            hint.fontSizeMin = LibraryFontFloor;
            hint.fontSizeMax = LibraryFontFloor;
            hint.fontSize = LibraryFontFloor;
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
                if (text == null || !text.gameObject.activeInHierarchy || RelaxesLibraryFloor(text))
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
            Sprite sprite = null;
            if (resourcePath.StartsWith(SkirmishBattleLibraryArt.ResourceRoot + "medal_", StringComparison.Ordinal) && !resourcePath.EndsWith("medal_empty", StringComparison.Ordinal))
                SkirmishLibrarySpriteRegistry.Load()?.TryGetSprite("skirmish.shared.medallion", out sprite);
            if (sprite == null) sprite = Resources.Load<Sprite>(resourcePath);
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
            image.color = tint * SagaMedalTint(resourcePath);
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


        private static Texture2D LoadScenarioPreview(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOf("skirmish.preview.", StringComparison.Ordinal) < 0)
                return null;
            Texture2D texture = SkirmishLibraryPreviewAssets.Load(key);
            if (texture == null || texture.name == "SkirmishLibraryPlaceholder")
                return null;
            return texture;
        }

        private static void PlaceCoverPreview(RawImage image, float width, float height, Texture2D texture, bool dimmed)
        {
            if (image == null)
                return;
            image.raycastTarget = false;
            PlaceTopLeft(image.rectTransform, 0f, 0f, width, height);
            if (texture == null || width < 2f || height < 2f || texture.width < 1 || texture.height < 1)
            {
                image.texture = null;
                image.uvRect = new Rect(0f, 0f, 1f, 1f);
                image.enabled = false;
                return;
            }

            image.enabled = true;
            image.texture = texture;
            image.color = dimmed ? new Color(0.62f, 0.56f, 0.48f, 1f) : Color.white;
            float frameAspect = width / height;
            float textureAspect = texture.width / (float)texture.height;
            if (frameAspect >= textureAspect)
            {
                float visibleHeight = textureAspect / frameAspect;
                image.uvRect = new Rect(0f, (1f - visibleHeight) * 0.5f, 1f, visibleHeight);
            }
            else
            {
                float visibleWidth = frameAspect / textureAspect;
                image.uvRect = new Rect((1f - visibleWidth) * 0.5f, 0f, visibleWidth, 1f);
            }
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
            AttachDisabledTap(button, UIAudioEventKind.ButtonDisabledTap); // v41 audit: disabled taps are heard
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
            if (text.name != "Subtitle" && text.name != "Value" && !RelaxesLibraryFloor(text) &&
                !SkirmishBattleLibraryArt.IsComingSoon(value))
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


        private void ApplyBriefingColumnState()
        {
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            RectTransform column = rules != null ? rules.Find("BriefingColumn") as RectTransform : null;
            if (column == null)
                return;
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            string descriptionName = scenarioDescription != null ? scenarioDescription.gameObject.name : "SeedHelp";
            if (battlefield)
                SetActive(FindModeToggle(), false);
            Transform medalKeep = ChooseMedalRow(column);
            if (medalKeep is RectTransform keptMedal)
                _medalRow = keptMedal;
            var order = battlefield
                ? new[] { "Title", "BriefingSubtitle", "BriefingPreview", descriptionName, "BriefingStats", "ArmySize", "MedalRow", "MedalDifficultySelector" }
                : new[] { "TheatreSummary", "Title", "BattlesWonTrack", "RecommendedNext" };
            if (battlefield) BindBattlefieldSeed(UiShellRuntimeGateway.Localization.IsRightToLeft);
            float descriptionHeight = battlefield ? MeasureSagaBriefingDescription(column.rect.width) : 0f;
            float previewHeight = Mathf.Clamp(column.rect.height - 520f - descriptionHeight, 80f, 188f);
            var heights = battlefield
                ? new[] { 44f, 32f, previewHeight, descriptionHeight, 84f, 52f, 104f, 52f } /* v34: taller MAP/GRID toggle row (approved-2), description 96->80 */ // Game PM approved-2 order: MAP/GRID toggle, heading, subtitle, preview; P1 v25 tiles + difficulty (52)
                : new[] { TheatreSummaryHeight, 40f, 84f, RecommendedCompactHeight }; // v41b compact Recommended next row (was 272)
            for (int i = 0; i < column.childCount; i++)
            {
                Transform child = column.GetChild(i);
                bool allowed = IsBriefingColumnChild(child, medalKeep, battlefield, descriptionName);
                child.gameObject.SetActive(allowed);
                LayoutElement element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.ignoreLayout = !allowed;
                if (!allowed)
                {
                    element.preferredHeight = 0f;
                    element.minHeight = 0f;
                    element.flexibleHeight = 0f;
                }
            }

            if (!battlefield)
            {
                if (scenarioDescription != null)
                {
                    scenarioDescription.gameObject.SetActive(false);
                    LayoutElement description = scenarioDescription.GetComponent<LayoutElement>() ?? scenarioDescription.gameObject.AddComponent<LayoutElement>();
                    description.ignoreLayout = true;
                    description.preferredHeight = 0f;
                    description.minHeight = 0f;
                    description.flexibleHeight = 0f;
                }

                if (medalKeep != null)
                {
                    medalKeep.gameObject.SetActive(false);
                    LayoutElement medal = medalKeep.GetComponent<LayoutElement>() ?? medalKeep.gameObject.AddComponent<LayoutElement>();
                    medal.ignoreLayout = true;
                    medal.preferredHeight = 0f;
                    medal.minHeight = 0f;
                    medal.flexibleHeight = 0f;
                }
            }

            int index = 0;
            for (int nameIndex = 0; nameIndex < order.Length; nameIndex++)
            {
                Transform child = null;
                for (int i = 0; i < column.childCount; i++)
                {
                    Transform candidate = column.GetChild(i);
                    if (candidate.name != order[nameIndex] || !candidate.gameObject.activeSelf)
                        continue;
                    if (candidate.name == "MedalRow" && candidate != medalKeep)
                        continue;
                    child = candidate;
                    break;
                }

                // v43 (Game PM): on the Theatre the summary plate (battles won + medal tiles) takes the column's spare
                // height and its tiles scale into it; Recommended next stays a compact row at the column bottom.
                bool absorb = !battlefield && order[nameIndex] == "TheatreSummary";
                index = StackColumnChild(child as RectTransform, index, heights[nameIndex], child != null, absorb);
            }

            if (battlefield && scenarioDescription != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                // Copy and binding bounds were finalized before measuring this row.
                // Rebinding here restores the old larger font after the layout pass.
                scenarioDescription.gameObject.SetActive(true);
                scenarioDescription.margin = new Vector4(24f, 8f, 24f, 8f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Overflow;
                scenarioDescription.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                scenarioDescription.isRightToLeftText = fa;
                scenarioDescription.maxVisibleLines = int.MaxValue;
                scenarioDescription.enableAutoSizing = false;
                scenarioDescription.fontSize = scenarioDescription.fontSizeMin = scenarioDescription.fontSizeMax = fa ? 22f : 20f;
            }

            VerticalLayoutGroup group = column.GetComponent<VerticalLayoutGroup>();
            if (group != null)
            {
                group.enabled = true;
                group.padding = new RectOffset(8, 8, 4, 6);
                group.spacing = 6f;
                group.childControlWidth = true;
                group.childControlHeight = true;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
            FitSagaBriefingOverflow((RectTransform)rules, column, battlefield);
            if (battlefield) FitBriefingPicture();
            if (scenarioDescription != null)
                scenarioDescription.ForceMeshUpdate();
            ArrangeMedalGrid(_medalRow, UiShellRuntimeGateway.Localization.IsRightToLeft);
            PlaceSideBattlesCopy();
            PaintSideProgress();
            if (!battlefield)
                FitRecommendedPreview();
        }


        private static bool RelaxesLibraryFloor(TMP_Text text)
        {
            if (text == null)
                return false;
            if (text.name == "Subtitle" || text.name == "PendingLabel" || text.name == "ZoomCue" ||
                text.name == "ZoneName" || text.name == "ZoneCount" ||
                text.name == "ZoneTabName" || text.name == "ZoneTabStatus" ||
                text.name.StartsWith("Column_", StringComparison.Ordinal))
                return true;
            if (text.name.StartsWith("RowCount_", StringComparison.Ordinal) &&
                SkirmishBattleLibraryArt.IsComingSoon(text.text))
                return true;
            string parent = text.transform.parent != null ? text.transform.parent.name : string.Empty;
            return text.name == "Caption" && parent.StartsWith("Slot", StringComparison.Ordinal);
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

        /// <summary>
        /// v41 audit: sound for taps on a non-interactable library button only (the enabled click stays silent here
        /// because the view model already raises its own interaction sound).
        /// </summary>
        private static void AttachDisabledTap(Button button, UIAudioEventKind disabledKind)
        {
            if (button == null)
                return;
            UIButtonAudioEventView audio = button.GetComponent<UIButtonAudioEventView>();
            if (audio == null)
                audio = button.gameObject.AddComponent<UIButtonAudioEventView>();
            audio.Configure(UIAudioEventKind.None, disabledKind);
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
            bool theater = LibraryLevel != SkirmishLibraryLevel.Battlefield;
            SealTheatreMapEdge(preview as RectTransform, rules as RectTransform, theater);
        }

        private float LibraryLabelColumnWidth()
        {
            float type = ReferenceSubtitleFloor();
            return Mathf.Max(LibraryGridOriginX, LibraryGridOriginX * type / 22f);
        }

        private void FitLibrarySearch()
        {
            bool theater = LibraryLevel != SkirmishLibraryLevel.Battlefield;
            if (theater)
            {
                if (battleLibrarySearch != null)
                    battleLibrarySearch.gameObject.SetActive(false);
                if (mapNameText != null)
                    mapNameText.gameObject.SetActive(false);
                return;
            }

            if (battleLibrarySearch == null)
                return;
            battleLibrarySearch.gameObject.SetActive(true);
            if (mapNameText != null)
                mapNameText.gameObject.SetActive(false);
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
            if (!PlaceGridSearch(search, searchWidth))
                PlaceTopLeft(search, 12f, 8f, Mathf.Max(140f,width-24f), 44f);
            FrameSearchField(search);
            search.SetAsLastSibling();
            if (mapNameText != null && !IsGridLibrary())
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
            border.color = new Color(0.23f, 0.29f, 0.31f, 1f);
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
            Texture2D texture = battlefield
                ? SkirmishLibraryPreviewAssets.Load("skirmish.blur." + mapId)
                : SkirmishLibraryPreviewAssets.Load(SkirmishLibraryPreviewAssets.TheatreBaseKey);
            if (texture == null && battlefield)
                texture = SkirmishLibraryPreviewAssets.Load("skirmish.plate." + mapId);
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
            graphic.Configure(new Color(0.05f, 0.08f, 0.09f, 0.94f), new Color(0.02f, 0.04f, 0.05f, 0.94f), new Color(0.23f, 0.29f, 0.31f, 1f), 1.5f);
        }


        private static string MapDisplayTitle(string mapId, bool fa)
        {
            SkirmishBattleLibraryArt.Copy(string.IsNullOrEmpty(mapId) ? "DB" : mapId, fa, out string title, out _);
            return title;
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
            float compositionHeight = compositionRect != null && compositionRect.rect.height > 200f
                ? compositionRect.rect.height
                : 941f;
            // v39b: a compact pill over the bottom of the briefing preview picture (the selected battle's own image),
            // clear of the header, every battle card/tile and the footer buttons. Fallback when no preview is laid out:
            // a small pill in the footer band's top edge, centred.
            float bannerHeight = 40f;
            float bannerWidth = 300f;
            float bannerX = (compositionWidth - bannerWidth) * 0.5f;
            float bannerY = compositionHeight - 112f - bannerHeight - 4f;
            RectTransform preview = _briefingPreview != null && _briefingPreview.gameObject.activeInHierarchy
                ? _briefingPreview.rectTransform
                : null;
            if (compositionRect != null && preview != null && preview.rect.width > 120f && preview.rect.height > 60f)
            {
                var corners = new Vector3[4];
                preview.GetWorldCorners(corners);
                Vector3 topLeft = compositionRect.InverseTransformPoint(corners[1]);
                Vector3 bottomRight = compositionRect.InverseTransformPoint(corners[3]);
                Rect area = compositionRect.rect;
                float left = topLeft.x - area.xMin;
                float top = area.yMax - topLeft.y;
                float w = bottomRight.x - topLeft.x;
                float h = topLeft.y - bottomRight.y;
                bannerWidth = Mathf.Min(300f, Mathf.Max(120f, w - 16f));
                bannerHeight = Mathf.Min(40f, Mathf.Max(28f, h * 0.3f));
                bannerX = left + (w - bannerWidth) * 0.5f;
                bannerY = top + h - bannerHeight - 8f;
            }

            PlaceTopLeft(plate, bannerX, bannerY, bannerWidth, bannerHeight);
            plate.gameObject.SetActive(true);
            if (plateImage != null)
                plateImage.color = new Color(0.07f, 0.08f, 0.09f, 0.88f);
            RaiseLaunchTransition();
            TMP_Text text = plate.Find("LaunchLabel")?.GetComponent<TMP_Text>();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (text != null)
            {
                // v39b: the FA toast rendered blank: the raw (unshaped) Farsi string was set with isRightToLeftText=false,
                // and the Farsi TMP atlas only bakes presentation forms, so no glyph drew. The label now opts out of the
                // localisation binding/refit pass, takes the live footer font, and gets shaped RTL text directly.
                V3LocalizedTextBindingView binding = text.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                {
                    if (Application.isPlaying)
                        Destroy(binding);
                    else
                        DestroyImmediate(binding);
                }
                LibraryFixedTextSize held = text.GetComponent<LibraryFixedTextSize>() ?? text.gameObject.AddComponent<LibraryFixedTextSize>();
                TMP_Text fontSource = launchButton != null ? launchButton.GetComponentInChildren<TMP_Text>(true) : null;
                if (fontSource != null && fontSource.font != null)
                    text.font = fontSource.font;
                Stretch(text.rectTransform, 10f, 10f, 4f, 4f);
                string copy = fa ? "در حال اعزام " + scenarioId : "LAUNCHING " + scenarioId;
                text.text = fa ? V3LocalizedTextBindingView.ShapeForRendering(copy) : copy;
                text.enableAutoSizing = false;
                text.fontSize = Mathf.Min(20f, bannerHeight * 0.5f);
                held.Size = text.fontSize;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.alignment = TextAlignmentOptions.Center;
                text.isRightToLeftText = fa;
                text.color = new Color(0.96f, 0.93f, 0.84f, 1f);
                text.alpha = 1f;
                text.gameObject.SetActive(true);
                text.enabled = true;
                text.transform.SetAsLastSibling();
                text.ForceMeshUpdate();
                Debug.Log("[LaunchToast] locale=" + (fa ? "fa-IR" : "en") + " text=" + scenarioId +
                    " chars=" + text.textInfo.characterCount + " rect=" + bannerWidth.ToString("0") + "x" + bannerHeight.ToString("0"));
            }
        }

        private void RaiseLaunchTransition()
        {
            Transform transition = transform.Find("SkirmishSetupComposition/LaunchTransition");
            if (transition != null && transition.gameObject.activeSelf)
                transition.SetAsLastSibling();
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
                if (text.name == "StopNumber") { text.isRightToLeftText = false; continue; }
                string parentName = text.transform.parent.name;
                bool subtitle = text.name == "Subtitle";
                bool zoneTab = text.name == "Label" && parentName.StartsWith("BattlefieldTab_", StringComparison.Ordinal);
                bool gridRow = text.name.StartsWith("Row_", StringComparison.Ordinal);
                bool columnHeader = text.name.StartsWith("Column_", StringComparison.Ordinal);
                bool statCaption = text.name == "Caption" &&
                    (parentName == "Objective" || parentName == "Time" || parentName == "Size" || parentName == "Army" || parentName == "Start");
                bool medalCaption = text.name == "Caption" && parentName.StartsWith("Slot", StringComparison.Ordinal);
                bool stripCaption = text.name == "Caption" && parentName.StartsWith("Medal", StringComparison.Ordinal);
                bool difficulty = parentName.StartsWith("DifficultyChoice_", StringComparison.Ordinal);
                bool zoneName = text.name == "ZoneName";
                bool recommended = parentName == "RecommendedNext";
                Button footerOwner = text.transform.parent.GetComponent<Button>();
                bool footer = footerOwner != null && (
                    footerOwner == resetButton ||
                    footerOwner == launchButton ||
                    footerOwner == randomizeSeedButton ||
                    footerOwner == _libraryAdvancedButton ||
                    footerOwner.name == "BackButton");
                bool cardTitle = text.name == "Title" &&
                    (text.transform.parent.GetComponent<Button>() != null || parentName == "EmptyCell");
                if (text.name == "PendingLabel")
                {
                    float host = text.transform.parent is RectTransform pendingHost ? pendingHost.sizeDelta.x : 0f;
                    bool roomyPending = host >= LibraryGridRoomyCardWidth;
                    float pendingMin = roomyPending ? 12f : 9f;
                    float pendingMax = roomyPending ? 14f : 12f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.isRightToLeftText = fa;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.enableAutoSizing = true;
                    text.fontSizeMin = pendingMin;
                    text.fontSizeMax = pendingMax;
                    if (text.fontSize < pendingMin)
                        text.fontSize = pendingMin;
                    text.maxVisibleLines = 1;
                    V3LocalizedTextBindingView pendingBinding = text.GetComponent<V3LocalizedTextBindingView>();
                    if (pendingBinding != null)
                        pendingBinding.ConfigureFontBounds(pendingMin, pendingMax);
                }
                else if (subtitle)
                {
                    float width = text.rectTransform != null ? text.rectTransform.sizeDelta.x : 0f;
                    bool gridCard = IsGridLibraryCard(text);
                    bool roomy = gridCard && width >= LibraryGridRoomyTextWidth;
                    bool wrappedGrid = gridCard && !roomy;
                    float floor = wrappedGrid || roomy ? 14f : ReferenceSubtitleFloor();
                    float max = wrappedGrid || roomy ? 16f : Mathf.Max(20f, floor);
                    RelaxLibraryText(text, floor, max, wrappedGrid);
                    text.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                    text.isRightToLeftText = fa && ContainsArabicScript(text.text);
                    if (wrappedGrid)
                    {
                        text.textWrappingMode = TextWrappingModes.Normal;
                        text.overflowMode = TextOverflowModes.Ellipsis;
                        text.maxVisibleLines = 2;
                    }
                    else
                    {
                        text.textWrappingMode = TextWrappingModes.NoWrap;
                        text.overflowMode = TextOverflowModes.Overflow;
                        text.maxVisibleLines = 1;
                    }

                    V3LocalizedTextBindingView subtitleBinding = text.GetComponent<V3LocalizedTextBindingView>();
                    if (subtitleBinding != null)
                        subtitleBinding.ConfigureFontBounds(floor, max);
                }
                else if (cardTitle)
                {
                    float titleBox = text.rectTransform != null ? text.rectTransform.sizeDelta.y : 0f;
                    if (titleBox >= 24f && titleBox < 32f)
                    {
                        text.enableAutoSizing = true;
                        text.fontSizeMin = 15f;
                        text.fontSizeMax = 18f;
                        if (text.fontSize < 15f)
                            text.fontSize = 15f;
                        V3LocalizedTextBindingView compactTitle = text.GetComponent<V3LocalizedTextBindingView>();
                        if (compactTitle != null)
                            compactTitle.ConfigureFontBounds(15f, 18f);
                    }
                    else if (titleBox >= 32f && titleBox <= 42f)
                    {
                        text.enableAutoSizing = true;
                        text.fontSizeMin = 20f;
                        text.fontSizeMax = 22f;
                        if (text.fontSize < 20f)
                            text.fontSize = 20f;
                        V3LocalizedTextBindingView titleBinding = text.GetComponent<V3LocalizedTextBindingView>();
                        if (titleBinding != null)
                            titleBinding.ConfigureFontBounds(20f, 22f);
                    }

                    text.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    text.isRightToLeftText = fa && ContainsArabicScript(text.text);
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Overflow;
                }
                else if (zoneTab || gridRow)
                    RelaxLibraryText(text, 11f, 16f, true);
                else if (statCaption)
                {
                    RelaxLibraryText(text, 11f, 16f, true);
                    text.overflowMode = TextOverflowModes.Overflow;
                }
                else if (columnHeader)
                {
                    ShowColumnHeader(text, fa);
                }
                else if (medalCaption || stripCaption)
                {
                    RelaxLibraryText(text, 10f, 15f, false);
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.maxVisibleLines = 1;
                }
                else if (difficulty)
                    RelaxLibraryText(text, 12f, 18f, true);
                else if (text.name == "ZoomCue")
                {
                    // FitZoomCue locks 16–26. A 12px floor here is what Farsi autosize
                    // settles on once the cue is narrower than the longest word.
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.maxVisibleLines = 2;
                    text.isRightToLeftText = fa;
                }
                else if (text.name == "ZoneCount")
                {
                    bool pending = SkirmishBattleLibraryArt.IsRenderedComingSoon(text.text);
                    RelaxLibraryText(text, pending ? 13f : 11f, pending ? 16f : 14f, false);
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.isRightToLeftText = pending && fa;
                }
                else if (zoneName)
                {
                    RelaxLibraryText(text, 13f, 16f, false);
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.maxVisibleLines = 1;
                    text.isRightToLeftText = fa && ContainsArabicScript(text.text);
                }
                else if (recommended)
                {
                    // v42: Recommended next copy is fitted and pinned by FitRecommendedText (largest size that fits,
                    // >= 14px body, two lines); the old 18-24 / 3-line relax here pushed it past its box.
                    if (text.name == "Heading" || text.name == "Body")
                        FitRecommendedText(fa);
                }
                else if (footer && !_refittingGrid)
                {
                    bool zoomDistrict = text.text != null && text.text.IndexOf("ZOOM IN", StringComparison.Ordinal) >= 0;
                    if (!zoomDistrict)
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

            LockZoneTabs();
            FitZoomCue();
            if (scenarioDescription != null)
            {
                scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
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
            const float size = 24f;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
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


        private static Sprite SolidSprite()
        {
            if (_solidSprite != null)
                return _solidSprite;
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _solidSprite.hideFlags = HideFlags.HideAndDontSave;
            return _solidSprite;
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
            {
                if (target is GameObject retired) { retired.SetActive(false); retired.transform.SetParent(null, false); }
                UnityEngine.Object.Destroy(target);
            }
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
