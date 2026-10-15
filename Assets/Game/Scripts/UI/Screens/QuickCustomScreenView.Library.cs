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
        private const float LibraryGridMinCardWidth = 168f;
        private const float LibraryGridCardGap = 8f;
        private const float LibraryGridRightPad = 8f;
        private const float LibraryGridOriginX = 92f;
        private const float LibraryGridRowGap = 4f;
        private const float LibraryGridCardHeight = 156f;
        private const float LibraryGridPreviewHeight = 48f;
        private const float LibraryGridTitleHeight = 34f;
        private const float LibraryGridSubtitleHeight = 48f;
        private const float LibraryGridMedalSize = 16f;
        private const float LibraryPinSubtitleHeight = 36f;
        private const int LibraryGridLogicalColumns = 6;

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
        private bool _favouritesLit;
        private Button _libraryFavouritesButton;
        private Button _libraryAdvancedButton;
        private static Sprite _libraryMedallion;
        private static Sprite _zoneMedallion;
        private static Sprite _zoneGlow;
        private static Sprite _zoneRibbon;
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
                OutlineSelectedZone(zones, theater.SelectedMapId, plateWidth, plateHeight);
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
                    bool zoneSelected = string.Equals(pin.MapId, theater.SelectedMapId, StringComparison.OrdinalIgnoreCase);
                    raw.color = zoneSelected ? new Color(1.25f, 1.05f, 0.72f, 1f) : new Color(0.62f, 0.5f, 0.38f, 1f);
                    raw.raycastTarget = false;
                    RectTransform plate = map != null ? map.rectTransform.parent as RectTransform : null;
                    float plateWidth = plate != null && plate.rect.width > 2f ? plate.rect.width : 1024f;
                    float plateHeight = plate != null && plate.rect.height > 2f ? plate.rect.height : 540f;
                    CoverCrop(raw, plateWidth, plateHeight);
                    if (zoneSelected)
                        StrokeSelectedZone(zones, raw, feather, plateWidth, plateHeight);
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

                float pinSize = selected ? 96f : 36f;
                Vector2 pinCentre = centre + (selected ? new Vector2(0f, -0.07f) : Vector2.zero);
                if (selected)
                {
                    var glow = new GameObject("ZoneGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                    glow.transform.SetParent(labels, false);
                    glow.sprite = ZoneGlowSprite();
                    glow.preserveAspect = true;
                    glow.color = new Color(1f, 0.86f, 0.28f, 0.95f);
                    glow.raycastTarget = false;
                    PlaceLabelCentre(glow.rectTransform, pinCentre, 220f, 220f);
                }

                var backing = new GameObject("ZonePlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                backing.transform.SetParent(labels, false);
                backing.sprite = ZoneRibbonSprite();
                backing.type = Image.Type.Sliced;
                backing.color = Color.white;
                backing.raycastTarget = false;
                PlaceLabelCentre(backing.rectTransform, centre + new Vector2(0f, 0.09f), 300f, 76f);
                var name = CreateLabel(labels, "ZoneName", 18f);
                PlaceLabelCentre(name.rectTransform, centre + new Vector2(0f, 0.072f), 260f, 30f);
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
                PlaceLabelCentre(count.rectTransform, centre + new Vector2(0f, 0.112f), 120f, 24f);
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
                var pinMark = new GameObject("Pin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                pinMark.transform.SetParent(labels, false);
                pinMark.sprite = LibraryMedallion();
                pinMark.preserveAspect = true;
                pinMark.color = selected ? new Color(1f, 0.93f, 0.45f, 1f) : zone;
                pinMark.raycastTarget = false;
                PlaceLabelCentre(pinMark.rectTransform, pinCentre, pinSize, pinSize);
                if (selected)
                    pinMark.transform.SetAsLastSibling();
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

        private static void OutlineSelectedZone(Transform parent, string mapId, float frameWidth, float frameHeight)
        {
            if (parent == null || string.IsNullOrEmpty(mapId))
                return;
            Texture2D feather = SkirmishLibraryPreviewAssets.Load("skirmish.zone." + mapId);
            if (feather == null)
                return;
            var marker = new GameObject("ZoneSelected", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            marker.transform.SetParent(parent, false);
            Stretch(marker.rectTransform);
            marker.texture = feather;
            marker.color = new Color(1.22f, 1.08f, 0.78f, 1f);
            marker.raycastTarget = false;
            CoverCrop(marker, frameWidth, frameHeight);
            StrokeSelectedZone(parent, marker, feather, frameWidth, frameHeight);
        }

        private static void StrokeSelectedZone(Transform parent, RawImage zone, Texture2D texture, float frameWidth, float frameHeight)
        {
            Vector2[] offsets =
            {
                new Vector2(12f, 0f), new Vector2(-12f, 0f), new Vector2(0f, 12f), new Vector2(0f, -12f),
                new Vector2(8f, 8f), new Vector2(-8f, 8f), new Vector2(8f, -8f), new Vector2(-8f, -8f)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                var stroke = new GameObject("ZoneStroke", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                stroke.transform.SetParent(parent, false);
                stroke.transform.SetSiblingIndex(zone.transform.GetSiblingIndex());
                Stretch(stroke.rectTransform);
                stroke.rectTransform.anchoredPosition = offsets[i];
                stroke.texture = texture;
                stroke.color = new Color(1f, 0.86f, 0.22f, 1f);
                stroke.raycastTarget = false;
                CoverCrop(stroke, frameWidth, frameHeight);
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
            MeasureLibraryGrid(out float gridWidth, out float gridHeight);
            int columns = GridColumnCount(gridWidth);
            float cardWidth = GridCardWidth(gridWidth, columns);
            int lines = Mathf.Max(1, (LibraryGridLogicalColumns + columns - 1) / columns);
            int objectives = field.Tabs.Length;
            int visualRows = objectives * lines;
            bool showHeaders = columns == LibraryGridLogicalColumns;
            float scrollTop = showHeaders ? 96f : 60f;
            float cardHeight = LibraryGridCardHeight;
            float previewHeight = LibraryGridPreviewHeight;
            float viewport = Mathf.Max(80f, gridHeight - scrollTop);
            float stack = visualRows * cardHeight + Mathf.Max(0, visualRows - 1) * LibraryGridRowGap;
            const float contentTop = 2f;
            const float contentBottom = 4f;
            float spare = viewport - (contentTop + stack + contentBottom);
            if (showHeaders && spare > 8f)
            {
                float grow = Mathf.Min(28f, spare / Mathf.Max(1, visualRows));
                previewHeight += grow;
                cardHeight += grow;
                stack = visualRows * cardHeight + Mathf.Max(0, visualRows - 1) * LibraryGridRowGap;
            }

            PresentBattlefieldTabs(field.MapId, fa);
            string[] armies = { "G", "A", "C" };
            string[] starts = { "F", "E" };
            if (showHeaders)
            {
                for (int armyIndex = 0; armyIndex < armies.Length; armyIndex++)
                {
                    for (int startIndex = 0; startIndex < starts.Length; startIndex++)
                    {
                        int column = armyIndex * 2 + startIndex;
                        float columnX = LibraryGridOriginX + column * (cardWidth + LibraryGridCardGap);
                        float iconX = fa ? columnX + cardWidth - 20f : columnX + 2f;
                        var headerIcon = new GameObject("ColumnIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                        headerIcon.transform.SetParent(_libraryGrid, false);
                        PlaceTopLeft(headerIcon.rectTransform, iconX, 66f, 16f, 16f);
                        ShowIcon(headerIcon, SkirmishBattleLibraryArt.ArmyIcon(armies[armyIndex]), Color.white);
                        SkirmishBattleLibraryArt.ProfileCopy(armies[armyIndex], fa, out string armyLabel);
                        SkirmishBattleLibraryArt.ProfileCopy(starts[startIndex], fa, out string startLabel);
                        float labelX = fa ? columnX + 2f : columnX + 20f;
                        var header = CreateLabel(_libraryGrid, "Column_" + column, 15f);
                        PlaceTopLeft(header.rectTransform, labelX, 58f, Mathf.Max(40f, cardWidth - 24f), 36f);
                        header.alignment = TextAlignmentOptions.Center;
                        header.isRightToLeftText = fa;
                        header.color = new Color(0.96f, 0.93f, 0.82f, 1f);
                        SetCopy(header, armyLabel + "\n" + startLabel, 13f, 16f);
                        ShowColumnHeader(header, fa);
                    }
                }
            }

            RectTransform content = CreateGridScroll(scrollTop, out ScrollRect scroll);
            var slots = new GridCardSlots
            {
                Preview = previewHeight,
                Title = LibraryGridTitleHeight,
                Subtitle = LibraryGridSubtitleHeight,
                Medal = LibraryGridMedalSize
            };
            var labelledRows = new HashSet<int>();
            for (int row = 0; row < objectives; row++)
            {
                SkirmishObjectiveTabState objective = field.Tabs[row];
                for (int column = 0; column < LibraryGridLogicalColumns; column++)
                {
                    int index = row * LibraryGridLogicalColumns + column;
                    if (index >= field.GridCards.Length)
                        break;
                    int line = column / columns;
                    int visualColumn = column % columns;
                    int visualRow = row * lines + line;
                    float y = contentTop + visualRow * (cardHeight + LibraryGridRowGap);
                    if (labelledRows.Add(visualRow))
                        PresentGridRowLabel(content, objective, fa, y);
                    float x = LibraryGridOriginX + visualColumn * (cardWidth + LibraryGridCardGap);
                    SkirmishBattleCardState card = field.GridCards[index];
                    RectTransform widget = IsEmptyCard(card)
                        ? CreateEmptyGridCell(content, objective.ObjectiveId, armies[column / 2], starts[column % 2], card.PreviewSpriteKey, slots, cardWidth)
                        : CreateBattleCard(content, card, false, cardWidth, cardHeight, slots);
                    PlaceTopLeft(widget, x, y, cardWidth, cardHeight);
                    AddSiblingShadow(widget);
                }
            }

            float contentHeight = contentTop + stack + contentBottom;
            content.sizeDelta = new Vector2(0f, contentHeight);
            bool scrolls = contentHeight > viewport + 2f;
            scroll.vertical = scrolls;
            scroll.horizontal = false;
            if (_libraryGrid != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_libraryGrid);
            Canvas.ForceUpdateCanvases();
            if (scrolls)
                scroll.verticalNormalizedPosition = 1f;
        }

        private void PresentGridRowLabel(Transform parent, SkirmishObjectiveTabState objective, bool fa, float y)
        {
            var rowIcon = new GameObject("RowIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            rowIcon.transform.SetParent(parent, false);
            PlaceTopLeft(rowIcon.rectTransform, 8f, y + 8f, 36f, 36f);
            ShowIcon(rowIcon, SkirmishBattleLibraryArt.ObjectiveIcon(objective.ObjectiveId), Color.white);
            SkirmishBattleLibraryArt.ObjectiveCopy(objective.ObjectiveId, fa, out _, out string rowName);
            var rowLabel = CreateLabel(parent, "Row_" + objective.ObjectiveId, 14f);
            PlaceTopLeft(rowLabel.rectTransform, 2f, y + 36f, LibraryGridOriginX - 8f, 40f);
            rowLabel.alignment = TextAlignmentOptions.Center;
            rowLabel.textWrappingMode = TextWrappingModes.Normal;
            rowLabel.overflowMode = TextOverflowModes.Ellipsis;
            SetCopy(rowLabel, rowName, 13f, 16f);
            var rowCount = CreateLabel(parent, "RowCount_" + objective.ObjectiveId, 13f);
            PlaceTopLeft(rowCount.rectTransform, 4f, y + 62f, LibraryGridOriginX - 10f, 18f);
            rowCount.alignment = TextAlignmentOptions.Center;
            rowCount.isRightToLeftText = false;
            rowCount.textWrappingMode = TextWrappingModes.NoWrap;
            SetCopy(rowCount, objective.WonCount + "/" + objective.PublishedCount, 16f, 20f);
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

        private void MeasureLibraryGrid(out float width, out float height)
        {
            if (_libraryGrid != null && (_libraryGrid.rect.width < 200f || _libraryGrid.rect.height < 200f))
                Canvas.ForceUpdateCanvases();
            width = _libraryGrid != null && _libraryGrid.rect.width > 200f ? _libraryGrid.rect.width : 1008f;
            height = _libraryGrid != null && _libraryGrid.rect.height > 200f ? _libraryGrid.rect.height : 740f;
        }

        private static int GridColumnCount(float gridWidth)
        {
            float available = gridWidth - LibraryGridOriginX - LibraryGridRightPad;
            float stride = LibraryGridMinCardWidth + LibraryGridCardGap;
            int columns = stride > 1f ? Mathf.FloorToInt((available + LibraryGridCardGap) / stride) : 1;
            return Mathf.Clamp(columns, 1, LibraryGridLogicalColumns);
        }

        private static float GridCardWidth(float gridWidth, int columns)
        {
            float available = gridWidth - LibraryGridOriginX - LibraryGridRightPad;
            float gaps = LibraryGridCardGap * Mathf.Max(0, columns - 1);
            return columns > 0 ? Mathf.Max(1f, (available - gaps) / columns) : LibraryGridMinCardWidth;
        }

        private static void ShowColumnHeader(TMP_Text header, bool fa)
        {
            if (header == null)
                return;
            header.alignment = TextAlignmentOptions.Center;
            header.isRightToLeftText = fa;
            header.textWrappingMode = TextWrappingModes.Normal;
            header.overflowMode = TextOverflowModes.Overflow;
            header.enableAutoSizing = true;
            header.fontSizeMin = 13f;
            header.fontSizeMax = 16f;
            header.maxVisibleLines = 2;
            header.margin = new Vector4(1f, 0f, 1f, 0f);
            V3LocalizedTextBindingView binding = header.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(13f, 16f);
        }

        private void ShowCardSubtitle(TMP_Text subtitle, string copy, bool fa, bool singleLine)
        {
            if (subtitle == null)
                return;
            float floor = ReferenceSubtitleFloor();
            float max = Mathf.Max(20f, floor);
            subtitle.color = new Color(0.9f, 0.93f, 0.9f, 1f);
            SetCopy(subtitle, copy, floor, max);
            subtitle.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            subtitle.isRightToLeftText = fa && ContainsArabicScript(copy);
            subtitle.textWrappingMode = singleLine ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            subtitle.overflowMode = singleLine ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = floor;
            subtitle.fontSizeMax = max;
            subtitle.maxVisibleLines = singleLine ? 1 : 2;
            V3LocalizedTextBindingView binding = subtitle.GetComponent<V3LocalizedTextBindingView>();
            if (binding != null)
                binding.ConfigureFontBounds(floor, max);
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

        private RectTransform CreateBattleCard(Transform parent, SkirmishBattleCardState card, bool pin, float width = LibraryPinWidth, float height = LibraryPinHeight, GridCardSlots gridSlots = default)
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
            float titleHeight = pin ? 22f : gridSlots.Title > 1f ? gridSlots.Title : LibraryGridTitleHeight;
            float subtitleHeight = pin ? LibraryPinSubtitleHeight : gridSlots.Subtitle > 1f ? gridSlots.Subtitle : LibraryGridSubtitleHeight;
            float gridPreview = gridSlots.Preview > 1f ? gridSlots.Preview : LibraryGridPreviewHeight;
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
            PlaceFittedPreview(preview, previewKey, 3f, 3f, width - 6f, previewHeight);
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
            float textY = previewHeight + 4f;
            var title = CreateLabel(button.transform, "Title", 15f);
            PlaceTopLeft(title.rectTransform, 6f, textY, width - 12f, titleHeight);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            title.isRightToLeftText = fa;
            SetCopy(title, objectiveLabel, pin ? 14f : 20f, pin ? 18f : 22f);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.isRightToLeftText = fa && ContainsArabicScript(objectiveLabel);
            var subtitle = CreateLabel(button.transform, "Subtitle", 16f);
            PlaceTopLeft(subtitle.rectTransform, 6f, textY + titleHeight, width - 12f, subtitleHeight);
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
            float medalX = fa && pin ? width - 8f - medalRun : 6f;
            AddMedalIcons(button.transform, card.Medals, medalX, textY + titleHeight + subtitleHeight + 2f, medalSize);
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
            return rect;
        }

        private RectTransform CreateEmptyGridCell(Transform parent, string objectiveId, string army, string start, string previewKey, GridCardSlots slots, float width)
        {
            var rect = new GameObject("EmptyCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var plate = rect.GetComponent<Image>();
            plate.color = new Color(0.05f, 0.07f, 0.08f, 0.92f);
            plate.raycastTarget = false;
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(rect, false);
            float previewHeight = slots.Preview > 1f ? slots.Preview : LibraryGridPreviewHeight;
            float titleHeight = slots.Title > 1f ? slots.Title : LibraryGridTitleHeight;
            float subtitleHeight = slots.Subtitle > 1f ? slots.Subtitle : LibraryGridSubtitleHeight;
            float medalSize = slots.Medal > 1f ? slots.Medal : LibraryGridMedalSize;
            PlaceFittedPreview(preview, ScenarioPreviewKey(previewKey), 3f, 3f, width - 6f, previewHeight);
            preview.transform.SetAsFirstSibling();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            SkirmishBattleLibraryArt.ObjectiveCopy(objectiveId, fa, out _, out string objectiveLabel);
            SkirmishBattleLibraryArt.ProfileCopy(army, fa, out string armyLabel);
            SkirmishBattleLibraryArt.ProfileCopy(start, fa, out string startLabel);
            string detail = string.IsNullOrEmpty(startLabel) ? armyLabel : armyLabel + " · " + startLabel;
            float textY = previewHeight + 4f;
            var title = CreateLabel(rect, "Title", 20f);
            PlaceTopLeft(title.rectTransform, 6f, textY, width - 12f, titleHeight);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            SetCopy(title, objectiveLabel, 20f, 22f);
            title.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            title.isRightToLeftText = fa && ContainsArabicScript(objectiveLabel);
            var subtitle = CreateLabel(rect, "Subtitle", 17f);
            PlaceTopLeft(subtitle.rectTransform, 6f, textY + titleHeight, width - 12f, subtitleHeight);
            ShowCardSubtitle(subtitle, detail, fa, false);
            AddMedalIcons(rect, SkirmishMedalMask.None, 6f, textY + titleHeight + subtitleHeight + 2f, medalSize);
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
            SetCopy(caption, LibraryText("ui.skirmish.library.zone_progress", fa), 14f, 18f);
            SetCopy(count, pin.WonCount + " / " + pin.PublishedCount, 14f, 20f);
            _sideWonRatio = pin.PublishedCount > 0 ? Mathf.Clamp01(pin.WonCount / (float)pin.PublishedCount) : 0f;
            PlaceSideBattlesCopy();
            EnsureThinProgressBar(_progressTrack, 16f);
            PaintSideProgress();
        }

        private void PlaceSideBattlesCopy()
        {
            if (_progressTrack == null || !_progressTrack.gameObject.activeInHierarchy)
                return;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float width = _progressTrack.rect.width;
            if (width < 120f)
                width = _progressTrack.sizeDelta.x > 120f ? _progressTrack.sizeDelta.x : 360f;
            float textWidth = Mathf.Max(96f, width - 16f);
            TMP_Text caption = _progressTrack.Find("Caption")?.GetComponent<TMP_Text>();
            TMP_Text count = _progressTrack.Find("Count")?.GetComponent<TMP_Text>();
            float labelSize = ColumnLabelSize();
            string battles = UpperLatin(LibraryText("ui.skirmish.library.zone_progress", fa));
            SetCopy(caption, battles, labelSize, labelSize);
            if (caption != null)
            {
                caption.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                caption.isRightToLeftText = fa;
                caption.overflowMode = TextOverflowModes.Overflow;
                caption.textWrappingMode = TextWrappingModes.NoWrap;
                caption.enableAutoSizing = false;
                caption.fontSize = labelSize;
                caption.fontSizeMin = labelSize;
                caption.fontSizeMax = labelSize;
                caption.maxVisibleLines = 1;
                V3LocalizedTextBindingView binding = caption.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(labelSize, labelSize);
                PlaceTopLeft(caption.rectTransform, 8f, 0f, textWidth, 36f);
            }

            if (count != null)
            {
                count.isRightToLeftText = false;
                count.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                count.overflowMode = TextOverflowModes.Overflow;
                count.textWrappingMode = TextWrappingModes.NoWrap;
                count.maxVisibleLines = 1;
                PlaceTopLeft(count.rectTransform, 8f, 38f, textWidth, 22f);
            }
        }

        private static float ColumnLabelSize()
        {
            return LibraryFontFloor;
        }

        private static string UpperLatin(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var builder = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char letter = value[i];
                builder.Append(letter <= '\u007f' ? char.ToUpperInvariant(letter) : letter);
            }

            return builder.ToString();
        }

        private void PaintSideProgress()
        {
            if (_progressTrack == null || !_progressTrack.gameObject.activeInHierarchy)
                return;
            V3GradientGraphic plate = _progressTrack.GetComponent<V3GradientGraphic>();
            if (plate != null)
                plate.enabled = false;
            RectTransform bar = _progressTrack.Find("Bar") as RectTransform;
            if (bar == null)
                bar = EnsureThinProgressBar(_progressTrack, 18f);
            if (bar == null || _progressFill == null)
                return;
            float trackWidth = _progressTrack.rect.width;
            if (trackWidth < 120f)
                trackWidth = 360f;
            float barWidth = Mathf.Min(270f, trackWidth - 16f);
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float barX = fa ? Mathf.Max(8f, trackWidth - 8f - barWidth) : 8f;
            PlaceTopLeft(bar, barX, 62f, barWidth, 16f);
            RectMask2D mask = bar.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
            Image barImage = bar.GetComponent<Image>();
            if (barImage != null)
            {
                barImage.sprite = SolidSprite();
                barImage.type = Image.Type.Simple;
                barImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                barImage.enabled = true;
                barImage.raycastTarget = false;
            }

            _progressFill.transform.SetParent(bar, false);
            _progressFill.sprite = SolidSprite();
            _progressFill.type = Image.Type.Simple;
            _progressFill.color = new Color(0.93f, 0.74f, 0.22f, 1f);
            _progressFill.enabled = true;
            _progressFill.raycastTarget = false;
            Outline outline = bar.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
            float ratio = Mathf.Clamp01(_sideWonRatio);
            TMP_Text count = _progressTrack.Find("Count")?.GetComponent<TMP_Text>();
            if (WonRatio(count != null ? count.text : null, out float parsed))
                ratio = parsed;
            float inner = barWidth - 6f;
            float fillWidth = ratio >= 0.999f ? inner : Mathf.Max(12f, inner * ratio);
            PlaceTopLeft(_progressFill.rectTransform, 2f, 2f, fillWidth, 12f);
            _progressFill.transform.SetAsLastSibling();
        }

        private static bool WonRatio(string value, out float ratio)
        {
            ratio = 0f;
            if (string.IsNullOrEmpty(value))
                return false;
            int slash = value.IndexOf('/');
            if (slash <= 0 || slash >= value.Length - 1)
                return false;
            if (!int.TryParse(value.Substring(0, slash).Trim(), out int won))
                return false;
            if (!int.TryParse(value.Substring(slash + 1).Trim(), out int published) || published <= 0)
                return false;
            ratio = Mathf.Clamp01(won / (float)published);
            return true;
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
                barImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                barImage.raycastTarget = false;
            }

            Outline rim = bar.GetComponent<Outline>() ?? bar.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.93f, 0.78f, 0.38f, 1f);
            rim.effectDistance = new Vector2(1.5f, -1.5f);

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
            LayoutMedalEntry(slot, fa, 24f, 36f);
            BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), index);
        }

        private void LockMedalArtwork()
        {
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            HoldDescriptionClear();
            ArrangeMedalGrid(_medalRow, fa);
            LayoutTheatreMedals(fa);
            PaintSideProgress();
            PurgeStrayMedalImages();
        }

        private void ArrangeMedalGrid(RectTransform row, bool fa)
        {
            if (row == null)
                return;
            StopExpanding(row);
            GridLayoutGroup grid = row.GetComponent<GridLayoutGroup>();
            if (grid != null)
                grid.enabled = false;
            float width = row.rect.width > 80f ? row.rect.width : MedalRowWidth();
            float cell = Mathf.Min(220f, Mathf.Max(148f, (width - 8f) * 0.5f));
            if (cell * 2f + 8f > width)
                cell = Mathf.Max(120f, (width - 8f) * 0.5f);
            for (int i = 0; i < 4; i++)
            {
                if (row.Find("Slot" + i) is not RectTransform slot)
                    continue;
                int column = i % 2;
                if (fa)
                    column = 1 - column;
                int line = i / 2;
                PlaceTopLeft(slot, column * (cell + 8f), 4f + line * 50f, cell, 46f);
                LayoutElement element = slot.GetComponent<LayoutElement>();
                if (element != null)
                    element.ignoreLayout = false;
                DropExtraMedalImages(slot);
                LayoutMedalEntry(slot, fa, 24f, 36f);
                BindMedalIcon(slot.Find("Icon")?.GetComponent<Image>(), i);
            }
        }

        private void HoldDescriptionClear()
        {
            if (scenarioDescription == null)
                return;
            scenarioDescription.spriteAsset = null;
            string source = scenarioDescription.text ?? string.Empty;
            string stripped = StripSpriteTags(source);
            if (!string.Equals(stripped, source, StringComparison.Ordinal))
                scenarioDescription.text = stripped;
            scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
            scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
            if (scenarioDescription.GetComponent<RectMask2D>() == null)
                scenarioDescription.gameObject.AddComponent<RectMask2D>();
            Image[] images = scenarioDescription.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                    DestroyUi(images[i].gameObject);
            }
        }

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

        private void PurgeStrayMedalImages()
        {
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            if (column == null)
                return;
            Image[] images = column.GetComponentsInChildren<Image>(true);
            var stray = new List<GameObject>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || !SkirmishLibraryBlankImage.IsMedalArt(image))
                    continue;
                Transform parent = image.transform.parent;
                bool kept = image.name == "Icon" && parent != null && parent.name.StartsWith("Slot", StringComparison.Ordinal);
                if (!kept)
                    stray.Add(image.gameObject);
            }

            for (int i = 0; i < stray.Count; i++)
                DestroyUi(stray[i]);
        }

        private static void DropExtraMedalImages(RectTransform slot)
        {
            if (slot == null)
                return;
            var stray = new List<GameObject>();
            for (int i = 0; i < slot.childCount; i++)
            {
                Transform child = slot.GetChild(i);
                Image image = child.GetComponent<Image>();
                if (image == null || child.name == "Icon")
                    continue;
                if (SkirmishLibraryBlankImage.IsMedalArt(image))
                    stray.Add(child.gameObject);
            }

            for (int i = 0; i < stray.Count; i++)
                DestroyUi(stray[i]);
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

        private static void LayoutMedalEntry(RectTransform slot, bool fa, float iconSize, float countWidth)
        {
            if (slot == null)
                return;
            HorizontalLayoutGroup row = slot.GetComponent<HorizontalLayoutGroup>();
            if (row != null)
                row.enabled = false;
            GridLayoutGroup grid = slot.GetComponent<GridLayoutGroup>();
            if (grid != null)
                grid.enabled = false;
            float slotWidth = slot.sizeDelta.x > 40f ? slot.sizeDelta.x : slot.rect.width;
            if (slotWidth < 40f)
                slotWidth = 148f;
            const float gap = 8f;
            float icon = iconSize;
            float countBox = Mathf.Max(28f, countWidth);
            float labelWidth = slotWidth - (4f + icon + gap + gap + countBox + 4f);
            if (labelWidth < 64f)
            {
                icon = Mathf.Min(icon, 18f);
                countBox = 28f;
                labelWidth = Mathf.Max(48f, slotWidth - (4f + icon + gap + gap + countBox + 4f));
            }

            RectTransform iconRect = slot.Find("Icon") as RectTransform;
            RectTransform caption = slot.Find("Caption") as RectTransform;
            RectTransform count = slot.Find("Count") as RectTransform;
            float iconX = fa ? slotWidth - 4f - icon : 4f;
            float countX = fa ? 4f : slotWidth - 4f - countBox;
            float labelX = fa ? 4f + countBox + gap : 4f + icon + gap;
            PlaceTopLeft(iconRect, iconX, 12f, icon, icon);
            PlaceTopLeft(caption, labelX, 8f, labelWidth, 30f);
            PlaceTopLeft(count, countX, 12f, countBox, 22f);
            HoldFixed(iconRect, icon, icon, 0f);
            HoldFixed(caption, labelWidth, 30f, 0f);
            HoldFixed(count, countBox, 22f, 0f);
            Image image = iconRect != null ? iconRect.GetComponent<Image>() : null;
            if (image != null)
            {
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
            }

            TMP_Text captionText = caption != null ? caption.GetComponent<TMP_Text>() : null;
            if (captionText != null)
            {
                float fitted = Mathf.Clamp(labelWidth / 5.4f, 10f, 15f);
                captionText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                captionText.isRightToLeftText = fa;
                captionText.overflowMode = TextOverflowModes.Overflow;
                captionText.textWrappingMode = TextWrappingModes.NoWrap;
                captionText.enableAutoSizing = false;
                captionText.fontSize = fitted;
                captionText.fontSizeMin = fitted;
                captionText.fontSizeMax = fitted;
                captionText.maxVisibleLines = 1;
                captionText.margin = Vector4.zero;
                if (caption.GetComponent<RectMask2D>() == null)
                    caption.gameObject.AddComponent<RectMask2D>();
                V3LocalizedTextBindingView binding = captionText.GetComponent<V3LocalizedTextBindingView>();
                if (binding != null)
                    binding.ConfigureFontBounds(fitted, fitted);
            }

            TMP_Text countText = count != null ? count.GetComponent<TMP_Text>() : null;
            if (countText != null)
            {
                countText.isRightToLeftText = false;
                countText.alignment = TextAlignmentOptions.Center;
                countText.overflowMode = TextOverflowModes.Overflow;
                countText.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private static void HoldFixed(RectTransform rect, float width, float height, float flexibleWidth)
        {
            if (rect == null)
                return;
            LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = false;
            element.minWidth = width;
            element.preferredWidth = width;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
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
            ArrangeMedalGrid(row, fa);
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
                _overallStrip.sizeDelta = new Vector2(-16f, 88f);
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
            Stretch(column, 24f, 24f, 6f, bottom);
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
                FitLayout(scenarioDescription.rectTransform, 0f, 96f, 1f);
                scenarioDescription.margin = new Vector4(4f, 2f, 4f, 2f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Truncate;
                scenarioDescription.alignment = TextAlignmentOptions.TopLeft;
            }

            if (_recommendedPanel != null)
            {
                _recommendedPanel.SetParent(column, false);
                FitLayout(_recommendedPanel, 0f, 272f, 0f);
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

            if (_progressTrack != null)
            {
                _progressTrack.SetParent(column, false);
                FitLayout(_progressTrack, 0f, 84f, 1f);
            }

            if (_medalRow != null)
            {
                _medalRow.SetParent(column, false);
                FitLayout(_medalRow, 0f, 104f, 1f);
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
            if (_progressTrack != null && _progressTrack.parent != column)
                _progressTrack.SetParent(column, false);
            VerticalLayoutGroup group = column.GetComponent<VerticalLayoutGroup>() ?? column.gameObject.AddComponent<VerticalLayoutGroup>();
            group.enabled = true;
            group.padding = new RectOffset(8, 8, 4, 6);
            group.spacing = 6f;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childScaleWidth = false;
            group.childScaleHeight = false;
            int index = 0;
            Transform summary = column.Find("TheatreSummary");
            index = StackColumnChild(summary as RectTransform, index, 76f, !battlefield && summary != null);
            bool previewShown = battlefield && _briefingPreview != null && _briefingPreview.gameObject.activeSelf;
            index = StackColumnChild(_briefingPreview != null ? _briefingPreview.rectTransform : null, index, 188f, previewShown);
            Transform title = rules.Find("BriefingColumn/Title") ?? rules.Find("Title");
            index = StackColumnChild(title as RectTransform, index, 40f, title != null);
            index = StackColumnChild(scenarioDescription != null ? scenarioDescription.rectTransform : null, index, battlefield ? 96f : 80f, scenarioDescription != null);
            index = StackColumnChild(_progressTrack, index, 84f, !battlefield && _progressTrack != null);
            index = StackColumnChild(_statRow, index, 84f, battlefield && _statRow != null);
            index = StackColumnChild(_medalRow, index, 104f, _medalRow != null);
            index = StackColumnChild(_difficultyRow, index, 116f, battlefield && _difficultyRow != null);
            StackColumnChild(_recommendedPanel, index, 272f, !battlefield && _recommendedPanel != null);
            if (_medalGrid != null)
            {
                _medalGrid.gameObject.SetActive(false);
                LayoutElement grid = _medalGrid.GetComponent<LayoutElement>() ?? _medalGrid.gameObject.AddComponent<LayoutElement>();
                grid.ignoreLayout = true;
                grid.preferredHeight = 0f;
                grid.minHeight = 0f;
            }

            if (scenarioDescription != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                float leading = fa ? 28f : 16f;
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
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
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }

        private static void ReleaseTextClip(Transform root)
        {
            if (root == null)
                return;
            RectMask2D mask = root.GetComponent<RectMask2D>();
            if (mask != null)
                mask.enabled = false;
        }

        private static int StackColumnChild(RectTransform rect, int index, float height, bool show, bool expand = false)
        {
            if (rect == null)
                return index;
            rect.gameObject.SetActive(show);
            LayoutElement element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = !show;
            element.minWidth = 0f;
            element.preferredWidth = -1f;
            element.flexibleWidth = 1f;
            element.minHeight = show ? height : 0f;
            element.preferredHeight = show ? height : 0f;
            element.flexibleHeight = show && expand ? 1f : 0f;
            if (!show)
                return index;
            rect.SetSiblingIndex(index);
            return index + 1;
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
            RectTransform mapShadow = EnsureSoftShadow(map, 8f, 14f);
            RectTransform briefingShadow = EnsureSoftShadow(briefing, briefingOnLeft ? 16f : -16f, 14f);
            if (briefingShadow != null && briefingShadow.GetSiblingIndex() < map.GetSiblingIndex())
            {
                briefingShadow.SetSiblingIndex(map.GetSiblingIndex() + 1);
                briefing.SetSiblingIndex(briefingShadow.GetSiblingIndex() + 1);
            }
            if (mapShadow != null)
                mapShadow.SetSiblingIndex(map.GetSiblingIndex());
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

        private void PresentTheatreSummary()
        {
            Transform preview = transform.Find("SkirmishSetupComposition/OperationPreview");
            Transform column = transform.Find("SkirmishSetupComposition/BaseAssaultRules/BriefingColumn");
            Transform stray = preview != null ? preview.Find("TheatreSummary") : null;
            if (stray != null)
                DestroyUi(stray.gameObject);
            if (column == null)
                return;
            bool show = LibraryLevel != SkirmishLibraryLevel.Battlefield && _libraryViewModel != null;
            var plate = column.Find("TheatreSummary") as RectTransform;
            if (!show)
            {
                if (plate != null)
                    plate.gameObject.SetActive(false);
                return;
            }

            if (plate == null)
            {
                plate = new GameObject("TheatreSummary", typeof(RectTransform)).GetComponent<RectTransform>();
                plate.SetParent(column, false);
            }

            if (plate.Find("Heading") == null)
                CreateLabel(plate, "Heading", 22f);
            if (plate.Find("Summary") == null)
                CreateLabel(plate, "Summary", 36f);

            plate.gameObject.SetActive(true);
            int won = _libraryViewModel.Theater.OverallWonCount;
            int published = _libraryViewModel.Theater.OverallPublishedCount;
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            const float ornamentInset = 32f;
            TMP_Text heading = plate.Find("Heading")?.GetComponent<TMP_Text>();
            if (heading != null)
            {
                PlaceSummaryLine(heading.rectTransform, 0f, 28f, fa, ornamentInset);
                string battles = UpperLatin(LibraryText("ui.skirmish.library.battles_won", fa));
                SetCopy(heading, battles, 18f, 22f);
                heading.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                heading.isRightToLeftText = fa;
                heading.textWrappingMode = TextWrappingModes.NoWrap;
                heading.overflowMode = TextOverflowModes.Overflow;
                heading.color = new Color(0.93f, 0.78f, 0.38f, 1f);
                heading.enableAutoSizing = true;
                heading.fontSizeMin = 18f;
                heading.fontSizeMax = 22f;
                heading.maxVisibleLines = 1;
            }

            TMP_Text summary = plate.Find("Summary")?.GetComponent<TMP_Text>();
            if (summary == null)
                return;
            PlaceSummaryLine(summary.rectTransform, 30f, 40f, fa, ornamentInset);
            SetCopy(summary, won + " / " + published, 28f, 36f);
            summary.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            summary.isRightToLeftText = false;
            summary.textWrappingMode = TextWrappingModes.NoWrap;
            summary.overflowMode = TextOverflowModes.Overflow;
            summary.color = new Color(0.98f, 0.91f, 0.64f, 1f);
            summary.enableAutoSizing = true;
            summary.fontSizeMin = 28f;
            summary.fontSizeMax = 36f;
            summary.maxVisibleLines = 1;
        }

        private static void PlaceSummaryLine(RectTransform rect, float y, float height, bool fa, float inset)
        {
            if (rect == null)
                return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(fa ? 8f : inset, -(y + height));
            rect.offsetMax = new Vector2(fa ? -inset : -8f, -y);
        }

        private float ReferenceSubtitleFloor()
        {
            float scale = 1f;
            MainMenuV3SectionLayoutView layout = GetComponent<MainMenuV3SectionLayoutView>()
                ?? GetComponentInParent<MainMenuV3SectionLayoutView>()
                ?? GetComponentInChildren<MainMenuV3SectionLayoutView>(true);
            if (layout != null && layout.LastAppliedScale > 0.05f)
                scale = layout.LastAppliedScale;
            return Mathf.Clamp(17f / scale, 17f, 22f);
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
            shadow.SetSiblingIndex(panel.GetSiblingIndex());
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
            float stripTop = 96f;
            if (preview.Find("OverallProgressStrip") is RectTransform strip)
            {
                float occupied = strip.rect.height > 8f ? strip.rect.height : strip.sizeDelta.y;
                if (occupied < 8f)
                    occupied = 88f;
                stripTop = strip.anchoredPosition.y + occupied;
            }

            rect.anchoredPosition = new Vector2(18f, stripTop + 40f);
            rect.sizeDelta = new Vector2(-36f, 36f);
            rect.SetAsLastSibling();
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
            if (text.name != "Subtitle" && text.name != "Value" && !RelaxesLibraryFloor(text))
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

        private void ApplyBriefingColumnState()
        {
            Transform rules = transform.Find("SkirmishSetupComposition/BaseAssaultRules");
            RectTransform column = rules != null ? rules.Find("BriefingColumn") as RectTransform : null;
            if (column == null)
                return;
            bool battlefield = LibraryLevel == SkirmishLibraryLevel.Battlefield;
            string descriptionName = scenarioDescription != null ? scenarioDescription.gameObject.name : "SeedHelp";
            Transform medalKeep = ChooseMedalRow(column);
            if (medalKeep is RectTransform keptMedal)
                _medalRow = keptMedal;
            var order = battlefield
                ? new[] { "BriefingPreview", "Title", descriptionName, "BriefingStats", "MedalRow", "MedalDifficultySelector" }
                : new[] { "TheatreSummary", "Title", descriptionName, "BattlesWonTrack", "MedalRow", "RecommendedNext" };
            var heights = battlefield
                ? new[] { 188f, 40f, 96f, 84f, 104f, 116f }
                : new[] { 76f, 40f, 80f, 84f, 104f, 272f };
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

                index = StackColumnChild(child as RectTransform, index, heights[nameIndex], child != null);
            }

            if (scenarioDescription != null)
            {
                bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
                if (battlefield)
                    BindBattlefieldSeed(fa);
                scenarioDescription.gameObject.SetActive(true);
                scenarioDescription.margin = new Vector4(24f, 8f, 24f, 8f);
                scenarioDescription.textWrappingMode = TextWrappingModes.Normal;
                scenarioDescription.overflowMode = TextOverflowModes.Ellipsis;
                scenarioDescription.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                scenarioDescription.isRightToLeftText = fa;
                scenarioDescription.maxVisibleLines = battlefield ? 4 : 3;
                scenarioDescription.enableAutoSizing = true;
                scenarioDescription.fontSizeMin = 15f;
                scenarioDescription.fontSizeMax = battlefield ? 22f : 20f;
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
            if (scenarioDescription != null)
                scenarioDescription.ForceMeshUpdate();
            ArrangeMedalGrid(_medalRow, UiShellRuntimeGateway.Localization.IsRightToLeft);
            PlaceSideBattlesCopy();
            PaintSideProgress();
            if (!battlefield)
                FitRecommendedPreview();
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

        private static bool RelaxesLibraryFloor(TMP_Text text)
        {
            if (text == null)
                return false;
            if (text.name == "Subtitle" || text.name.StartsWith("Column_", StringComparison.Ordinal))
                return true;
            string parent = text.transform.parent != null ? text.transform.parent.name : string.Empty;
            return text.name == "Caption" && parent.StartsWith("Slot", StringComparison.Ordinal);
        }

        private bool IsBriefingColumnChild(Transform child, Transform medalKeep, bool battlefield, string descriptionName)
        {
            if (child == null)
                return false;
            if (child.name == "MedalRow")
                return child == medalKeep;
            if (child.name == "Title")
                return true;
            if (scenarioDescription != null && child == scenarioDescription.transform)
                return true;
            if (child.name == descriptionName && scenarioDescription != null && child != scenarioDescription.transform)
                return false;
            if (battlefield)
            {
                if (child.name == "BriefingPreview")
                    return _briefingPreview == null || child == _briefingPreview.transform;
                if (child.name == "BriefingStats")
                    return _statRow == null || child == _statRow;
                if (child.name == "MedalDifficultySelector")
                    return _difficultyRow == null || child == _difficultyRow;
                return false;
            }

            if (child.name == "TheatreSummary")
                return true;
            if (child.name == "BattlesWonTrack")
                return _progressTrack == null || child == _progressTrack;
            if (child.name == "RecommendedNext")
                return _recommendedPanel == null || child == _recommendedPanel;
            return false;
        }

        private Transform ChooseMedalRow(Transform column)
        {
            Transform best = null;
            int bestScore = -1;
            for (int i = 0; i < column.childCount; i++)
            {
                Transform child = column.GetChild(i);
                if (child.name != "MedalRow")
                    continue;
                int score = 0;
                if (child.Find("Slot0") != null)
                    score += 2;
                if (child.Find("Slot0/Caption") != null)
                    score += 2;
                if (child == _medalRow)
                    score += 1;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = child;
                }
            }

            return best;
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
            if (texture == null && !string.IsNullOrEmpty(key))
                texture = SkirmishLibraryPreviewAssets.Placeholder();
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            float panelWidth = _recommendedPanel.rect.width > 80f ? _recommendedPanel.rect.width : _recommendedPanel.sizeDelta.x;
            if (panelWidth < 80f)
                panelWidth = 360f;
            float textWidth = Mathf.Max(140f, panelWidth - 16f);
            const float headingHeight = 32f;
            const float captionHeight = 64f;
            const float previewCap = 160f;
            const float frameHeight = 168f;
            float previewY = headingHeight + captionHeight + 6f;
            bool showPreview = texture != null;
            Image frame = _recommendedPanel.Find("PreviewFrame")?.GetComponent<Image>();
            if (frame == null)
            {
                frame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                frame.transform.SetParent(_recommendedPanel, false);
                frame.raycastTarget = false;
            }

            frame.sprite = SolidSprite();
            frame.type = Image.Type.Simple;
            frame.color = new Color(0.93f, 0.72f, 0.28f, 1f);
            frame.enabled = showPreview;
            frame.gameObject.SetActive(showPreview);
            if (showPreview)
                PlaceTopLeft(frame.rectTransform, 8f, previewY, textWidth, frameHeight);
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
                PlaceTopLeft(preview.rectTransform, 12f, previewY + 4f, Mathf.Max(80f, textWidth - 8f), previewCap);
                CoverCrop(preview, Mathf.Max(80f, textWidth - 8f), previewCap);
                preview.transform.SetSiblingIndex(frame.transform.GetSiblingIndex() + 1);
            }

            if (_recommendedPanel.Find("Heading") is RectTransform heading)
            {
                PlaceTopLeft(heading, 8f, 0f, textWidth, headingHeight);
                heading.SetAsLastSibling();
                TMP_Text headingText = heading.GetComponent<TMP_Text>();
                if (headingText != null)
                {
                    headingText.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                    headingText.isRightToLeftText = fa;
                    headingText.textWrappingMode = TextWrappingModes.NoWrap;
                    headingText.overflowMode = TextOverflowModes.Overflow;
                    headingText.enableAutoSizing = true;
                    headingText.fontSizeMin = 20f;
                    headingText.fontSizeMax = 26f;
                    headingText.maxVisibleLines = 1;
                }
            }

            if (_recommendedPanel.Find("Body") is RectTransform body)
            {
                PlaceTopLeft(body, 8f, headingHeight, textWidth, captionHeight);
                body.SetAsLastSibling();
                TMP_Text bodyText = body.GetComponent<TMP_Text>();
                if (bodyText != null)
                {
                    bodyText.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                    bodyText.isRightToLeftText = fa;
                    bodyText.textWrappingMode = TextWrappingModes.Normal;
                    bodyText.overflowMode = TextOverflowModes.Overflow;
                    bodyText.enableAutoSizing = true;
                    bodyText.fontSizeMin = 18f;
                    bodyText.fontSizeMax = 24f;
                    bodyText.maxVisibleLines = 3;
                    if (!string.IsNullOrWhiteSpace(bodyText.text))
                        bodyText.text = UpperLatin(bodyText.text);
                }
            }
        }

        private void FitRecommendedPreview()
        {
            if (_recommendedPanel == null || !_recommendedPanel.gameObject.activeInHierarchy)
                return;
            if (string.IsNullOrEmpty(_recommendedScenarioId) && _libraryViewModel != null)
                _recommendedScenarioId = FocusedRecommendation(_libraryViewModel.Theater, _libraryViewModel.Battlefield);
            PresentRecommendedPreview(_recommendedScenarioId);
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
            float chipWidth = showChip ? Mathf.Clamp(width * 0.22f, 240f, 360f) : 0f;
            float logoWidth = Mathf.Clamp(width * 0.24f, 220f, 400f);
            const float logoHeight = 96f;
            const float logoX = 18f;
            const float gap = 8f;
            float settingsX = width - 12f - settingsWidth;
            float chipX = settingsX - gap - chipWidth;
            float titleX = logoX + logoWidth + gap;
            float titleWidth = Mathf.Max(140f, chipX - gap - titleX);
            if (composition.Find("WarlineLogo") is RectTransform logo)
                FitBrandLogo(logo, logoX, 6f, logoWidth, logoHeight);

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
                PlaceTopLeft(chip, chipX, 10f, chipWidth, 88f);
                Image chipImage = chip.GetComponent<Image>();
                if (chipImage != null)
                {
                    chipImage.enabled = true;
                    if (chipImage.color.a < 0.98f || SkirmishLibraryBlankImage.IsLightNeutral(chipImage.color))
                        chipImage.color = new Color(0.05f, 0.07f, 0.08f, 1f);
                }
            }

            if (header.transform.Find("Icon") is RectTransform icon)
                PlaceTopLeft(icon, 10f, 16f, 56f, 56f);
            BindCommanderChip(header, fa, chipWidth);
        }

        private static void FitBrandLogo(RectTransform logo, float x, float y, float width, float height)
        {
            if (logo == null)
                return;
            logo.gameObject.SetActive(true);
            PlaceTopLeft(logo, x, y, width, height);
            if (logo.childCount == 0)
                return;
            RectTransform mark = logo.GetChild(0) as RectTransform;
            if (mark == null)
                return;
            float nativeWidth = mark.sizeDelta.x > 64f ? mark.sizeDelta.x : 513f;
            float nativeHeight = mark.sizeDelta.y > 32f ? mark.sizeDelta.y : 137f;
            float scale = Mathf.Min(
                Mathf.Max(1f, width - 16f) / nativeWidth,
                Mathf.Max(1f, height - 12f) / nativeHeight);
            mark.anchorMin = new Vector2(0.5f, 0.5f);
            mark.anchorMax = new Vector2(0.5f, 0.5f);
            mark.pivot = new Vector2(0.5f, 0.5f);
            mark.sizeDelta = new Vector2(nativeWidth, nativeHeight);
            mark.localScale = new Vector3(scale, scale, 1f);
            mark.anchoredPosition = Vector2.zero;
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
                PlaceTopLeft(labelRect, 74f, 8f, Mathf.Max(96f, chipWidth - 88f), 34f);
            TMP_Text caption = label != null ? label.GetComponent<TMP_Text>() : null;
            if (caption != null)
            {
                SetCopy(caption, name, 16f, 22f);
                caption.alignment = ContainsArabicScript(name) ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                caption.isRightToLeftText = ContainsArabicScript(caption.text);
                caption.overflowMode = TextOverflowModes.Ellipsis;
                caption.color = new Color(0.93f, 0.78f, 0.38f, 1f);
            }

            if (header.Value == null)
                return;
            PlaceTopLeft(header.Value.rectTransform, 74f, 44f, Mathf.Max(96f, chipWidth - 88f), 36f);
            string level = "Lv " + profile.Level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            SetCopy(header.Value, level, 18f, 28f);
            header.Value.isRightToLeftText = false;
            header.Value.alignment = fa ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
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
                bool cardTitle = text.name == "Title" &&
                    (text.transform.parent.GetComponent<Button>() != null || parentName == "EmptyCell");
                if (subtitle)
                {
                    float floor = ReferenceSubtitleFloor();
                    float box = text.rectTransform != null ? text.rectTransform.sizeDelta.y : 0f;
                    bool singleLine = box > 0f && box < 40f;
                    RelaxLibraryText(text, floor, Mathf.Max(20f, floor), !singleLine);
                    text.alignment = fa ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
                    text.isRightToLeftText = fa && ContainsArabicScript(text.text);
                    if (singleLine)
                    {
                        text.textWrappingMode = TextWrappingModes.NoWrap;
                        text.overflowMode = TextOverflowModes.Overflow;
                        text.maxVisibleLines = 1;
                    }
                    else
                    {
                        text.textWrappingMode = TextWrappingModes.Normal;
                        text.overflowMode = TextOverflowModes.Ellipsis;
                        text.maxVisibleLines = 2;
                    }

                    V3LocalizedTextBindingView subtitleBinding = text.GetComponent<V3LocalizedTextBindingView>();
                    if (subtitleBinding != null)
                        subtitleBinding.ConfigureFontBounds(floor, Mathf.Max(20f, floor));
                }
                else if (cardTitle)
                {
                    float titleBox = text.rectTransform != null ? text.rectTransform.sizeDelta.y : 0f;
                    if (titleBox >= 30f && titleBox <= 42f)
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
                else if (zoneName)
                    RelaxLibraryText(text, 14f, 26f, false);
                else if (recommended)
                {
                    bool bodyCopy = text.name == "Body";
                    RelaxLibraryText(text, bodyCopy ? 18f : 20f, bodyCopy ? 24f : 26f, bodyCopy);
                    text.overflowMode = TextOverflowModes.Overflow;
                    text.maxVisibleLines = bodyCopy ? 3 : 1;
                }
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

        private static Sprite ZoneMedallionSprite()
        {
            if (_zoneMedallion != null)
                return _zoneMedallion;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var fill = new Color32(18, 22, 26, 255);
            var gold = new Color32(236, 196, 96, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    if (distance > 1f)
                        continue;
                    pixels[y * size + x] = distance > 0.82f ? gold : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneMedallion = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _zoneMedallion.hideFlags = HideFlags.HideAndDontSave;
            return _zoneMedallion;
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

        private static Sprite ZoneRibbonSprite()
        {
            if (_zoneRibbon != null)
                return _zoneRibbon;
            const int width = 256;
            const int height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var fill = new Color32(8, 12, 14, 235);
            var gold = new Color32(236, 196, 96, 255);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int inset = 0;
                    if (x < 22)
                        inset = 22 - x;
                    else if (x >= width - 22)
                        inset = x - (width - 23);
                    int top = 4 + inset / 3;
                    int bottom = height - 5 - inset / 3;
                    if (y < top || y > bottom)
                        continue;
                    bool rim = y <= top + 2 || y >= bottom - 2 || x <= 2 || x >= width - 3;
                    pixels[y * width + x] = rim ? gold : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            _zoneRibbon = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(28f, 8f, 28f, 8f),
                false);
            _zoneRibbon.hideFlags = HideFlags.HideAndDontSave;
            return _zoneRibbon;
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
