using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using UnityEngine;

namespace Game.UI.Skirmish
{
    public sealed class SkirmishLibraryViewModel : ISkirmishLibraryViewModel
    {
        public const string ScenarioPrefsKey = "Warline.Skirmish.LastScenarioId";
        public const string LevelPrefsKey = "Warline.Skirmish.Library.Level";
        public const string MapPrefsKey = "Warline.Skirmish.Library.Map";
        public const string ObjectivePrefsKey = "Warline.Skirmish.Library.Objective";
        public const string LayoutPrefsKey = "Warline.Skirmish.Library.Layout";
        public const string DifficultyPrefsKey = "Warline.Skirmish.Library.Difficulty";
        public const int DevOverallPublishedCount = 120;

        private static readonly string[] MapOrder = { "DB", "CC", "MP", "IB", "AP" };
        private static readonly string[] ObjectiveOrder = { "BA", "FC", "BT", "CE" };
        private static readonly string[] ArmyOrder = { "G", "G", "A", "A", "C", "C" };
        private static readonly string[] StartOrder = { "F", "E", "F", "E", "F", "E" };
        private static readonly SkirmishDifficultyId[] AllDifficulties =
        {
            SkirmishDifficultyId.Recruit,
            SkirmishDifficultyId.Regular,
            SkirmishDifficultyId.Veteran,
            SkirmishDifficultyId.Commander
        };

        private readonly List<SkirmishBattleCatalogEntry> _catalog = new();
        private readonly SkirmishMedalLedger _ledger;
        private readonly Func<string, ContentAccessState> _access;
        private readonly Func<int, int> _randomIndex;
        private readonly ISkirmishLibraryPrefs _prefs;
        private readonly Func<SkirmishMedalSaveData[]> _medalSource;
        private readonly bool _showPlanned;
        private int _suspend;
        private int _interactionDepth;

        private SkirmishLibraryLevel _level = SkirmishLibraryLevel.Theater;
        private SkirmishBattlefieldLayout _layout = SkirmishBattlefieldLayout.Map;
        private string _selectedMapId = string.Empty;
        private string _selectedObjectiveId = "BA";
        private string _selectedScenarioId = string.Empty;
        private SkirmishDifficultyId _difficulty = SkirmishDifficultyId.Regular;
        private SkirmishLibraryFilter _filter;

        public SkirmishLibraryViewModel(
            IReadOnlyList<SkirmishBattleCatalogEntry> catalog,
            SkirmishMedalLedger ledger,
            Func<string, ContentAccessState> access,
            Func<int, int> randomIndex = null,
            bool showPlanned = false,
            ISkirmishLibraryPrefs prefs = null,
            Func<SkirmishMedalSaveData[]> medalSource = null)
        {
            if (catalog != null)
            {
                for (int i = 0; i < catalog.Count; i++)
                    _catalog.Add(catalog[i]);
            }

            _ledger = ledger ?? new SkirmishMedalLedger(null, _catalog);
            _access = access ?? (_ => ContentAccessState.Allowed);
            _randomIndex = randomIndex;
            _showPlanned = showPlanned;
            _prefs = prefs ?? new PlayerPrefsLibraryStore();
            _medalSource = medalSource;
            _filter = default;
            EnsureSelectedMap();
        }

        public event Action Changed;
        public event Action<SkirmishLibraryInteraction> Interaction;

        public static SkirmishMedalSaveData[] LoadProfileMedals()
        {
            try
            {
                return SaveService.CreateDefault().LoadProfile().skirmishMedals ?? Array.Empty<SkirmishMedalSaveData>();
            }
            catch (Exception)
            {
                return Array.Empty<SkirmishMedalSaveData>();
            }
        }

        public SkirmishLibraryLevel Level => _level;
        public SkirmishLibraryFilter Filter => _filter;
        public SkirmishTheaterState Theater => BuildTheater();
        public SkirmishBattlefieldState Battlefield => BuildBattlefield();
        public SkirmishBriefingState Briefing => BuildBriefing();

        public static SkirmishLibraryViewModel CreateDefault(IReadOnlyList<SkirmishBattleCatalogEntry> catalog)
        {
            SkirmishMedalLedger ledger;
            try
            {
                PlayerProfileSaveData profile = SaveService.CreateDefault().LoadProfile();
                ledger = new SkirmishMedalLedger(profile.skirmishMedals, catalog);
            }
            catch (Exception)
            {
                ledger = new SkirmishMedalLedger(null, catalog);
            }

            var model = new SkirmishLibraryViewModel(
                catalog,
                ledger,
                canonicalId => ContentAccessRuntime.Evaluate(canonicalId),
                showPlanned: ShowPlannedCards,
                medalSource: LoadProfileMedals);
            model.RestoreFromPrefs();
            return model;
        }

        public static bool ShowPlannedCards
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        public bool SelectMapCollection(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                _level = SkirmishLibraryLevel.Theater;
                EnsureSelectedMap();
                Persist();
                Notify();
                Raise(SkirmishLibraryInteractionKind.Selected, "theater", UIAudioEventKind.ScreenBack);
                return true;
            }

            string canonical = CanonicalMap(mapId);
            if (canonical == null || !IsMapVisible(canonical))
                return false;
            _selectedMapId = canonical;
            _level = SkirmishLibraryLevel.Battlefield;
            if (!HasObjectiveOnMap(canonical, _selectedObjectiveId))
                _selectedObjectiveId = FirstObjectiveOnMap(canonical);
            Persist();
            Notify();
            Raise(SkirmishLibraryInteractionKind.Selected, canonical, UIAudioEventKind.TabSelect);
            return true;
        }

        public bool SelectObjective(string objectiveId)
        {
            string canonical = CanonicalObjective(objectiveId);
            if (canonical == null)
                return false;
            if (_level != SkirmishLibraryLevel.Battlefield)
            {
                EnsureSelectedMap();
                if (string.IsNullOrEmpty(_selectedMapId) || !SelectMapCollection(_selectedMapId))
                    return false;
            }

            _selectedObjectiveId = canonical;
            if (!SelectedScenarioMatchesObjective())
                _selectedScenarioId = FirstScenarioOnObjective(_selectedMapId, canonical);
            Persist();
            Notify();
            Raise(SkirmishLibraryInteractionKind.Selected, canonical, UIAudioEventKind.TabSelect);
            return true;
        }

        public void SetLayout(SkirmishBattlefieldLayout layout)
        {
            _layout = layout;
            Persist();
            Notify();
            Raise(SkirmishLibraryInteractionKind.Clicked, layout.ToString(), UIAudioEventKind.TabSelect);
        }

        public bool SelectScenarioId(string scenarioId)
        {
            if (!TryFind(scenarioId, out SkirmishBattleCatalogEntry entry))
                return false;
            if (!_showPlanned && !entry.IsPlayable)
                return false;
            if (!IsMapVisible(entry.MapId) && !entry.IsPlayable)
                return false;
            _selectedScenarioId = entry.ScenarioId;
            _selectedMapId = CanonicalMap(entry.MapId) ?? entry.MapId;
            _selectedObjectiveId = CanonicalObjective(entry.ObjectiveId) ?? entry.ObjectiveId;
            _level = SkirmishLibraryLevel.Battlefield;
            Persist();
            Notify();
            ResolveAccess(entry, out SkirmishCardAccess access, out _, out _);
            Raise(
                SkirmishLibraryInteractionKind.Selected,
                entry.ScenarioId,
                access == SkirmishCardAccess.Playable
                    ? UIAudioEventKind.CardSelect
                    : UIAudioEventKind.CardLocked);
            return true;
        }

        public void SelectDifficulty(SkirmishDifficultyId difficulty)
        {
            if (difficulty < SkirmishDifficultyId.Recruit || difficulty > SkirmishDifficultyId.Commander)
                return;
            _difficulty = difficulty;
            Persist();
            Notify();
            Raise(SkirmishLibraryInteractionKind.Selected, difficulty.ToString(), UIAudioEventKind.TabSelect);
        }

        public void ZoomIn()
        {
            if (_level == SkirmishLibraryLevel.Battlefield)
                return;
            _interactionDepth++;
            try
            {
                EnsureSelectedMap();
                if (string.IsNullOrEmpty(_selectedMapId))
                {
                    string recommended = FindRecommended();
                    string map = MapOf(recommended);
                    if (!string.IsNullOrEmpty(map))
                        _selectedMapId = map;
                }

                if (!string.IsNullOrEmpty(_selectedMapId))
                    SelectMapCollection(_selectedMapId);
            }
            finally
            {
                _interactionDepth--;
            }

            if (_level == SkirmishLibraryLevel.Battlefield)
                Raise(SkirmishLibraryInteractionKind.Clicked, _selectedMapId, UIAudioEventKind.ScreenForward);
        }

        public void BackToTheater()
        {
            _level = SkirmishLibraryLevel.Theater;
            EnsureSelectedMap();
            Persist();
            Notify();
            Raise(SkirmishLibraryInteractionKind.Clicked, "theater", UIAudioEventKind.ScreenBack);
        }

        public string QuickBattle()
        {
            List<SkirmishBattleCatalogEntry> owned = OwnedPlayable();
            if (owned.Count == 0)
                return string.Empty;
            int index = _randomIndex != null ? _randomIndex(owned.Count) : UnityEngine.Random.Range(0, owned.Count);
            if (index < 0 || index >= owned.Count)
                index = 0;
            SkirmishBattleCatalogEntry entry = owned[index];
            _interactionDepth++;
            try
            {
                SelectScenarioId(entry.ScenarioId);
            }
            finally
            {
                _interactionDepth--;
            }

            Raise(SkirmishLibraryInteractionKind.Clicked, entry.ScenarioId, UIAudioEventKind.ButtonPrimaryClick);
            return entry.ScenarioId;
        }

        public void ApplyFilter(SkirmishLibraryFilter filter)
        {
            _filter = filter;
            bool jumped;
            _interactionDepth++;
            try
            {
                jumped = HasConstraint(filter) &&
                         TryFirstMatch(filter, out SkirmishBattleCatalogEntry hit) &&
                         SelectScenarioId(hit.ScenarioId);
            }
            finally
            {
                _interactionDepth--;
            }

            if (!jumped)
                Notify();
            Raise(SkirmishLibraryInteractionKind.Clicked, filter.SearchText ?? string.Empty, UIAudioEventKind.ButtonSecondaryClick);
        }

        public bool Launch()
        {
            SkirmishBriefingState briefing = BuildBriefing();
            if (!briefing.CanLaunch)
                return false;
            SkirmishLibraryLaunchContext.Difficulty = briefing.SelectedDifficulty;
            Raise(SkirmishLibraryInteractionKind.Clicked, briefing.ScenarioId, UIAudioEventKind.ButtonPrimaryClick);
            return true;
        }

        public void RestoreFromPrefs()
        {
            _suspend++;
            try
            {
                string level = _prefs.Get(LevelPrefsKey, string.Empty);
                string map = _prefs.Get(MapPrefsKey, string.Empty);
                string objective = _prefs.Get(ObjectivePrefsKey, string.Empty);
                string layout = _prefs.Get(LayoutPrefsKey, string.Empty);
                string scenario = _prefs.Get(ScenarioPrefsKey, string.Empty);
                string difficulty = _prefs.Get(DifficultyPrefsKey, string.Empty);
                if (byte.TryParse(difficulty, out byte raw) &&
                    raw >= (byte)SkirmishDifficultyId.Recruit &&
                    raw <= (byte)SkirmishDifficultyId.Commander)
                    _difficulty = (SkirmishDifficultyId)raw;
                if (Enum.TryParse(layout, out SkirmishBattlefieldLayout parsedLayout))
                    _layout = parsedLayout;
                if (!string.IsNullOrEmpty(scenario) && TryFind(scenario, out _))
                    SelectScenarioId(scenario);
                else if (!string.IsNullOrEmpty(map))
                    SelectMapCollection(map);
                if (!string.IsNullOrEmpty(objective))
                    SelectObjective(objective);
                bool savedBattlefield = string.Equals(
                    level, nameof(SkirmishLibraryLevel.Battlefield), StringComparison.Ordinal);
                if (!savedBattlefield)
                    BackToTheater();
                else if (!string.IsNullOrEmpty(_selectedMapId))
                    SelectMapCollection(_selectedMapId);
            }
            finally
            {
                _suspend--;
            }

            Notify();
        }

        public void SaveToPrefs()
        {
            _prefs.Set(LevelPrefsKey, _level.ToString());
            _prefs.Set(MapPrefsKey, _selectedMapId ?? string.Empty);
            _prefs.Set(ObjectivePrefsKey, _selectedObjectiveId ?? string.Empty);
            _prefs.Set(LayoutPrefsKey, _layout.ToString());
            _prefs.Set(ScenarioPrefsKey, _selectedScenarioId ?? string.Empty);
            _prefs.Set(DifficultyPrefsKey, ((byte)_difficulty).ToString());
            _prefs.Save();
        }

        private SkirmishTheaterState BuildTheater()
        {
            RefreshMedals();
            EnsureSelectedMap();
            string recommended = FindRecommended();
            string recommendedMap = MapOf(recommended);
            var pins = new List<SkirmishTheaterPinState>();
            for (int i = 0; i < MapOrder.Length; i++)
            {
                if (!IsMapVisible(MapOrder[i]))
                    continue;
                SkirmishMedalDifficultyTotals totals = TotalsForMap(MapOrder[i]);
                pins.Add(new SkirmishTheaterPinState
                {
                    MapId = MapOrder[i],
                    TitleLocKey = TheaterTitleKey(MapOrder[i]),
                    SubtitleLocKey = "ui.skirmish.theater." + MapOrder[i].ToLowerInvariant() + ".subtitle",
                    PlatePreviewKey = "skirmish.plate." + MapOrder[i],
                    PublishedCount = CountPublished(MapOrder[i], null),
                    WonCount = _ledger.CountFor(MapOrder[i]),
                    Medals = new SkirmishMedalTotals
                    {
                        Recruit = totals.Recruit,
                        Regular = totals.Regular,
                        Veteran = totals.Veteran,
                        Commander = totals.Commander
                    },
                    HasOwnedPlayable = HasOwnedPlayable(MapOrder[i]),
                    IsRecommended = !string.IsNullOrEmpty(recommendedMap) &&
                                    string.Equals(recommendedMap, MapOrder[i], StringComparison.OrdinalIgnoreCase)
                });
            }

            SkirmishMedalDifficultyTotals overallMedals = TotalsAcrossCatalog();
            int earned = overallMedals.Recruit + overallMedals.Regular + overallMedals.Veteran + overallMedals.Commander;
            int published = _showPlanned ? DevOverallPublishedCount : CountPublished(null, null);
            return new SkirmishTheaterState
            {
                Pins = pins.ToArray(),
                SelectedMapId = _selectedMapId ?? string.Empty,
                RecommendedScenarioId = recommended ?? string.Empty,
                OverallWonCount = CountWon(null),
                OverallPublishedCount = published,
                OverallMedalsEarned = earned,
                OverallMedalsPossible = published * 4,
                OverallMedalTotals = new SkirmishMedalTotals
                {
                    Recruit = overallMedals.Recruit,
                    Regular = overallMedals.Regular,
                    Veteran = overallMedals.Veteran,
                    Commander = overallMedals.Commander
                }
            };
        }

        private SkirmishBattlefieldState BuildBattlefield()
        {
            RefreshMedals();
            string mapId = _selectedMapId ?? string.Empty;
            var tabs = new SkirmishObjectiveTabState[ObjectiveOrder.Length];
            for (int i = 0; i < ObjectiveOrder.Length; i++)
            {
                tabs[i] = new SkirmishObjectiveTabState
                {
                    ObjectiveId = ObjectiveOrder[i],
                    TitleLocKey = "ui.skirmish.objective." + ObjectiveOrder[i].ToLowerInvariant(),
                    PublishedCount = CountPublished(mapId, ObjectiveOrder[i]),
                    WonCount = _ledger.CountFor(mapId, ObjectiveOrder[i])
                };
            }

            var grid = new SkirmishBattleCardState[ObjectiveOrder.Length * ArmyOrder.Length];
            for (int row = 0; row < ObjectiveOrder.Length; row++)
            {
                for (int column = 0; column < ArmyOrder.Length; column++)
                {
                    grid[(row * ArmyOrder.Length) + column] = BuildCard(
                        mapId, ObjectiveOrder[row], ArmyOrder[column], StartOrder[column], column);
                }
            }

            string objective = CanonicalObjective(_selectedObjectiveId) ?? "BA";
            var mapCards = new SkirmishBattleCardState[ArmyOrder.Length];
            int objectiveRow = ObjectiveRow(objective);
            for (int column = 0; column < ArmyOrder.Length; column++)
                mapCards[column] = grid[(objectiveRow * ArmyOrder.Length) + column];

            return new SkirmishBattlefieldState
            {
                MapId = mapId,
                Layout = _layout,
                Tabs = tabs,
                SelectedObjectiveId = objective,
                MapCards = mapCards,
                GridCards = grid
            };
        }

        private SkirmishBriefingState BuildBriefing()
        {
            RefreshMedals();
            string scenarioId = _selectedScenarioId;
            if (_level == SkirmishLibraryLevel.Theater)
                scenarioId = FindRecommended();
            if (!TryFind(scenarioId, out SkirmishBattleCatalogEntry entry))
            {
                return new SkirmishBriefingState
                {
                    AvailableDifficulties = Array.Empty<SkirmishDifficultyId>(),
                    SelectedDifficulty = _difficulty
                };
            }

            ResolveAccess(entry, out SkirmishCardAccess access, out string reason, out string storeProduct);
            bool published = entry.IsPlayable;
            return new SkirmishBriefingState
            {
                ScenarioId = entry.ScenarioId,
                TitleLocKey = TitleKey(entry),
                DescriptionLocKey = "ui.skirmish." + entry.ScenarioId.ToLowerInvariant() + ".description",
                ArmyLocKey = ArmyKey(entry.ArmyProfile),
                StartLocKey = StartKey(entry.StartProfile),
                PurposeLocKey = "ui.skirmish." + entry.ScenarioId.ToLowerInvariant() + ".purpose",
                Medals = MaskFor(entry.ScenarioId),
                AvailableDifficulties = published ? AllDifficulties : Array.Empty<SkirmishDifficultyId>(),
                SelectedDifficulty = _difficulty,
                Access = access,
                LockedReasonLocKey = reason ?? string.Empty,
                StoreRouteProductId = storeProduct ?? string.Empty,
                CanLaunch = _level == SkirmishLibraryLevel.Battlefield &&
                            access == SkirmishCardAccess.Playable &&
                            string.Equals(_selectedScenarioId, entry.ScenarioId, StringComparison.Ordinal) &&
                            IsMedalDifficulty(_difficulty)
            };
        }

        private SkirmishBattleCardState BuildCard(string mapId, string objectiveId, string army, string start, int pinIndex)
        {
            string pinId = mapId + "." + objectiveId + "." + pinIndex;
            if (!TryFindSlot(mapId, objectiveId, army, start, out SkirmishBattleCatalogEntry entry))
            {
                return new SkirmishBattleCardState
                {
                    MapId = mapId,
                    ObjectiveId = objectiveId,
                    ArmyProfile = army,
                    StartProfile = start,
                    PinId = pinId,
                    Access = SkirmishCardAccess.Planned,
                    LockedReasonLocKey = "ui.skirmish.locked.planned",
                    PreviewSpriteKey = "skirmish.plate." + mapId
                };
            }

            ResolveAccess(entry, out SkirmishCardAccess access, out string reason, out _);
            return new SkirmishBattleCardState
            {
                ScenarioId = entry.ScenarioId,
                MapId = entry.MapId,
                ObjectiveId = CanonicalObjective(entry.ObjectiveId) ?? entry.ObjectiveId,
                ArmyProfile = CanonicalArmy(entry.ArmyProfile) ?? entry.ArmyProfile,
                StartProfile = CanonicalStart(entry.StartProfile) ?? entry.StartProfile,
                TitleLocKey = TitleKey(entry),
                SubtitleLocKey = "ui.skirmish." + entry.ScenarioId.ToLowerInvariant() + ".subtitle",
                ArmyLocKey = ArmyKey(entry.ArmyProfile),
                StartLocKey = StartKey(entry.StartProfile),
                PreviewSpriteKey = entry.IsPlayable
                    ? "skirmish.preview." + entry.ScenarioId
                    : "skirmish.plate." + entry.MapId,
                PinId = pinId,
                Medals = MaskFor(entry.ScenarioId),
                Access = access,
                LockedReasonLocKey = reason ?? string.Empty,
                IsSelected = string.Equals(entry.ScenarioId, _selectedScenarioId, StringComparison.Ordinal),
                IsRecommended = string.Equals(entry.ScenarioId, FindRecommended(), StringComparison.Ordinal)
            };
        }

        private string FindRecommended()
        {
            int bestWins = int.MaxValue;
            string bestMap = null;
            for (int i = 0; i < MapOrder.Length; i++)
            {
                if (!IsMapVisible(MapOrder[i]) || !HasOwnedMissingRegular(MapOrder[i]))
                    continue;
                int wins = _ledger.CountFor(MapOrder[i]);
                if (wins < bestWins)
                {
                    bestWins = wins;
                    bestMap = MapOrder[i];
                }
            }

            if (!string.IsNullOrEmpty(bestMap))
                return FirstOwnedMissingRegular(bestMap);

            if (OwnedPlayable().Count > 0)
                return string.Empty;
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (!_catalog[i].IsPlayable || !IsShown(_catalog[i]))
                    continue;
                if (ResolveCardAccess(_catalog[i]) == SkirmishCardAccess.NotOwned)
                    return _catalog[i].ScenarioId;
            }

            return string.Empty;
        }

        private bool HasOwnedMissingRegular(string mapId)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = _catalog[i];
                if (!entry.IsPlayable || !string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ResolveCardAccess(entry) != SkirmishCardAccess.Playable)
                    continue;
                if (!_ledger.Has(entry.ScenarioId, SkirmishDifficultyId.Regular))
                    return true;
            }

            return false;
        }

        private string FirstOwnedMissingRegular(string mapId)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = _catalog[i];
                if (!entry.IsPlayable || !string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ResolveCardAccess(entry) != SkirmishCardAccess.Playable)
                    continue;
                if (!_ledger.Has(entry.ScenarioId, SkirmishDifficultyId.Regular))
                    return entry.ScenarioId;
            }

            return string.Empty;
        }

        private List<SkirmishBattleCatalogEntry> OwnedPlayable()
        {
            var owned = new List<SkirmishBattleCatalogEntry>();
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (_catalog[i].IsPlayable && ResolveCardAccess(_catalog[i]) == SkirmishCardAccess.Playable)
                    owned.Add(_catalog[i]);
            }

            return owned;
        }

        private bool TryFirstMatch(SkirmishLibraryFilter filter, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (!IsShown(_catalog[i]) || !Matches(_catalog[i], filter))
                    continue;
                entry = _catalog[i];
                return true;
            }

            entry = default;
            return false;
        }

        private bool Matches(SkirmishBattleCatalogEntry entry, SkirmishLibraryFilter filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.ObjectiveId) &&
                !string.Equals(CanonicalObjective(entry.ObjectiveId), CanonicalObjective(filter.ObjectiveId), StringComparison.Ordinal))
                return false;
            if (!string.IsNullOrWhiteSpace(filter.ArmyProfile) &&
                !string.Equals(CanonicalArmy(entry.ArmyProfile), CanonicalArmy(filter.ArmyProfile), StringComparison.Ordinal))
                return false;
            if (!string.IsNullOrWhiteSpace(filter.StartProfile) &&
                !string.Equals(CanonicalStart(entry.StartProfile), CanonicalStart(filter.StartProfile), StringComparison.Ordinal))
                return false;
            if (filter.MedalMissing.HasValue && IsMedalDifficulty(filter.MedalMissing.Value) &&
                _ledger.Has(entry.ScenarioId, filter.MedalMissing.Value))
                return false;
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                bool hit = Contains(entry.ScenarioId, search) || Contains(entry.TitleEnglish, search) ||
                           Contains(entry.MapId, search) || Contains(entry.ObjectiveId, search) ||
                           Contains(entry.ArmyProfile, search) || Contains(entry.StartProfile, search) ||
                           Contains(entry.DescriptionEnglish, search);
                if (!hit)
                    return false;
            }

            return true;
        }

        private static bool HasConstraint(SkirmishLibraryFilter filter) =>
            !string.IsNullOrWhiteSpace(filter.SearchText) ||
            !string.IsNullOrWhiteSpace(filter.ObjectiveId) ||
            !string.IsNullOrWhiteSpace(filter.ArmyProfile) ||
            !string.IsNullOrWhiteSpace(filter.StartProfile) ||
            (filter.MedalMissing.HasValue && IsMedalDifficulty(filter.MedalMissing.Value));

        private void ResolveAccess(SkirmishBattleCatalogEntry entry, out SkirmishCardAccess access, out string reason, out string storeProduct)
        {
            reason = string.Empty;
            storeProduct = string.Empty;
            if (!entry.IsPlayable)
            {
                access = SkirmishCardAccess.Planned;
                reason = "ui.skirmish.locked.planned";
                return;
            }

            ContentAccessState state = _access("skirmish." + entry.ScenarioId.ToLowerInvariant());
            switch (state)
            {
                case ContentAccessState.Allowed:
                    access = SkirmishCardAccess.Playable;
                    break;
                case ContentAccessState.NotOwned:
                    access = SkirmishCardAccess.NotOwned;
                    reason = "ui.skirmish.locked.not_owned";
                    if (ContentAccessAuthority.TryMembership("skirmish." + entry.ScenarioId.ToLowerInvariant(), out MissionProductMembership membership))
                        storeProduct = membership.ProductId;
                    break;
                case ContentAccessState.ProgressionLocked:
                    access = SkirmishCardAccess.Unavailable;
                    reason = "ui.skirmish.locked.progression";
                    break;
                case ContentAccessState.InstalledContentMissing:
                    access = SkirmishCardAccess.Unavailable;
                    reason = "ui.skirmish.locked.missing";
                    break;
                case ContentAccessState.UnknownContent:
                    access = SkirmishCardAccess.Unavailable;
                    reason = "ui.skirmish.locked.unknown";
                    break;
                default:
                    access = SkirmishCardAccess.Unavailable;
                    reason = "ui.skirmish.locked.unavailable";
                    break;
            }
        }

        private SkirmishCardAccess ResolveCardAccess(SkirmishBattleCatalogEntry entry)
        {
            ResolveAccess(entry, out SkirmishCardAccess access, out _, out _);
            return access;
        }

        private SkirmishMedalMask MaskFor(string scenarioId)
        {
            SkirmishMedalMask mask = SkirmishMedalMask.None;
            if (_ledger.Has(scenarioId, SkirmishDifficultyId.Recruit))
                mask |= SkirmishMedalMask.Recruit;
            if (_ledger.Has(scenarioId, SkirmishDifficultyId.Regular))
                mask |= SkirmishMedalMask.Regular;
            if (_ledger.Has(scenarioId, SkirmishDifficultyId.Veteran))
                mask |= SkirmishMedalMask.Veteran;
            if (_ledger.Has(scenarioId, SkirmishDifficultyId.Commander))
                mask |= SkirmishMedalMask.Commander;
            return mask;
        }

        private SkirmishMedalDifficultyTotals TotalsForMap(string mapId)
        {
            var totals = new SkirmishMedalDifficultyTotals();
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (!string.Equals(_catalog[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (_ledger.Has(_catalog[i].ScenarioId, SkirmishDifficultyId.Recruit))
                    totals.Recruit++;
                if (_ledger.Has(_catalog[i].ScenarioId, SkirmishDifficultyId.Regular))
                    totals.Regular++;
                if (_ledger.Has(_catalog[i].ScenarioId, SkirmishDifficultyId.Veteran))
                    totals.Veteran++;
                if (_ledger.Has(_catalog[i].ScenarioId, SkirmishDifficultyId.Commander))
                    totals.Commander++;
            }

            return totals;
        }

        private int CountWon(string mapId)
        {
            int count = 0;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = _catalog[i];
                if (!string.IsNullOrEmpty(mapId) &&
                    !string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!seen.Add(entry.ScenarioId))
                    continue;
                if (_ledger.Has(entry.ScenarioId, SkirmishDifficultyId.Recruit) ||
                    _ledger.Has(entry.ScenarioId, SkirmishDifficultyId.Regular) ||
                    _ledger.Has(entry.ScenarioId, SkirmishDifficultyId.Veteran) ||
                    _ledger.Has(entry.ScenarioId, SkirmishDifficultyId.Commander))
                    count++;
            }

            return count;
        }

        private SkirmishMedalDifficultyTotals TotalsAcrossCatalog()
        {
            var totals = new SkirmishMedalDifficultyTotals();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _catalog.Count; i++)
            {
                string scenarioId = _catalog[i].ScenarioId;
                if (!seen.Add(scenarioId))
                    continue;
                if (_ledger.Has(scenarioId, SkirmishDifficultyId.Recruit))
                    totals.Recruit++;
                if (_ledger.Has(scenarioId, SkirmishDifficultyId.Regular))
                    totals.Regular++;
                if (_ledger.Has(scenarioId, SkirmishDifficultyId.Veteran))
                    totals.Veteran++;
                if (_ledger.Has(scenarioId, SkirmishDifficultyId.Commander))
                    totals.Commander++;
            }

            return totals;
        }

        private int CountPublished(string mapId, string objectiveId)
        {
            int count = 0;
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = _catalog[i];
                if (!entry.IsPlayable)
                    continue;
                if (!string.IsNullOrEmpty(mapId) && !string.Equals(entry.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(objectiveId) &&
                    !string.Equals(CanonicalObjective(entry.ObjectiveId), objectiveId, StringComparison.Ordinal))
                    continue;
                count++;
            }

            return count;
        }

        private bool HasOwnedPlayable(string mapId)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (!_catalog[i].IsPlayable || !string.Equals(_catalog[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (ResolveCardAccess(_catalog[i]) == SkirmishCardAccess.Playable)
                    return true;
            }

            return false;
        }

        private bool IsMapVisible(string mapId)
        {
            if (CanonicalMap(mapId) == null)
                return false;
            if (_showPlanned)
                return HasAnyEntry(mapId);
            return CountPublished(mapId, null) > 0;
        }

        private bool IsShown(SkirmishBattleCatalogEntry entry) =>
            entry.IsPlayable || _showPlanned;

        private bool HasAnyEntry(string mapId)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (string.Equals(_catalog[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool HasObjectiveOnMap(string mapId, string objectiveId)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (!string.Equals(_catalog[i].MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(CanonicalObjective(_catalog[i].ObjectiveId), objectiveId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private string FirstObjectiveOnMap(string mapId)
        {
            for (int i = 0; i < ObjectiveOrder.Length; i++)
            {
                if (HasObjectiveOnMap(mapId, ObjectiveOrder[i]))
                    return ObjectiveOrder[i];
            }

            return "BA";
        }

        private string FirstScenarioOnObjective(string mapId, string objectiveId)
        {
            for (int column = 0; column < ArmyOrder.Length; column++)
            {
                if (TryFindSlot(mapId, objectiveId, ArmyOrder[column], StartOrder[column], out SkirmishBattleCatalogEntry entry) &&
                    IsShown(entry))
                    return entry.ScenarioId;
            }

            return string.Empty;
        }

        private bool SelectedScenarioMatchesObjective() =>
            TryFind(_selectedScenarioId, out SkirmishBattleCatalogEntry entry) &&
            string.Equals(entry.MapId, _selectedMapId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(CanonicalObjective(entry.ObjectiveId), _selectedObjectiveId, StringComparison.Ordinal);

        private void EnsureSelectedMap()
        {
            if (!string.IsNullOrEmpty(_selectedMapId) && IsMapVisible(_selectedMapId))
                return;
            string recommended = FindRecommended();
            string map = MapOf(recommended);
            if (string.IsNullOrEmpty(map))
            {
                for (int i = 0; i < MapOrder.Length; i++)
                {
                    if (IsMapVisible(MapOrder[i]))
                    {
                        map = MapOrder[i];
                        break;
                    }
                }
            }

            _selectedMapId = map ?? string.Empty;
        }

        private string MapOf(string scenarioId) =>
            TryFind(scenarioId, out SkirmishBattleCatalogEntry entry) ? CanonicalMap(entry.MapId) ?? entry.MapId : string.Empty;

        private bool TryFind(string scenarioId, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                if (string.Equals(_catalog[i].ScenarioId, scenarioId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = _catalog[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private bool TryFindSlot(string mapId, string objectiveId, string army, string start, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < _catalog.Count; i++)
            {
                SkirmishBattleCatalogEntry candidate = _catalog[i];
                if (!string.Equals(candidate.MapId, mapId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(CanonicalObjective(candidate.ObjectiveId), objectiveId, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(CanonicalArmy(candidate.ArmyProfile), army, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(CanonicalStart(candidate.StartProfile), start, StringComparison.Ordinal))
                    continue;
                entry = candidate;
                return true;
            }

            entry = default;
            return false;
        }

        private static int ObjectiveRow(string objectiveId)
        {
            for (int i = 0; i < ObjectiveOrder.Length; i++)
            {
                if (ObjectiveOrder[i] == objectiveId)
                    return i;
            }

            return 0;
        }

        private static string CanonicalMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
                return null;
            for (int i = 0; i < MapOrder.Length; i++)
            {
                if (string.Equals(MapOrder[i], mapId, StringComparison.OrdinalIgnoreCase))
                    return MapOrder[i];
            }

            return null;
        }

        private static string CanonicalObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
                return null;
            for (int i = 0; i < ObjectiveOrder.Length; i++)
            {
                if (string.Equals(ObjectiveOrder[i], objectiveId, StringComparison.OrdinalIgnoreCase))
                    return ObjectiveOrder[i];
            }

            return objectiveId.ToUpperInvariant() switch
            {
                "BASEASSAULT" or "BASE_ASSAULT" => "BA",
                "FRONTLINECONTROL" or "FRONTLINE_CONTROL" => "FC",
                "BREAKTHROUGH" => "BT",
                "CONVOYESCORT" or "CONVOY_ESCORT" => "CE",
                _ => null
            };
        }

        private static string CanonicalArmy(string army)
        {
            if (string.IsNullOrEmpty(army))
                return null;
            string token = army.Trim();
            if (token.Equals("G", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Ground", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("GroundManeuver", StringComparison.OrdinalIgnoreCase))
                return "G";
            if (token.Equals("A", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Air", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("AirMobile", StringComparison.OrdinalIgnoreCase))
                return "A";
            if (token.Equals("C", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Combined", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("CombinedArms", StringComparison.OrdinalIgnoreCase))
                return "C";
            return null;
        }

        private static string CanonicalStart(string start)
        {
            if (string.IsNullOrEmpty(start))
                return null;
            string token = start.Trim();
            if (token.Equals("F", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Field", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("FieldBase", StringComparison.OrdinalIgnoreCase))
                return "F";
            if (token.Equals("E", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Established", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("EstablishedBase", StringComparison.OrdinalIgnoreCase))
                return "E";
            return null;
        }

        private static string TheaterTitleKey(string mapId) => mapId switch
        {
            "DB" => "ui.skirmish.base_assault_map",
            "CC" => "ui.skirmish.city_crossroads_map",
            "IB" => "ui.skirmish.industrial_basin_map",
            _ => "ui.skirmish.theater." + mapId.ToLowerInvariant() + ".title"
        };

        private static string TitleKey(SkirmishBattleCatalogEntry entry) =>
            string.IsNullOrEmpty(entry.TitleKey)
                ? "ui.skirmish." + entry.ScenarioId.ToLowerInvariant() + ".title"
                : entry.TitleKey;

        private static string ArmyKey(string army) =>
            "ui.skirmish.army." + (CanonicalArmy(army) ?? "g").ToLowerInvariant();

        private static string StartKey(string start) =>
            "ui.skirmish.start." + (CanonicalStart(start) ?? "f").ToLowerInvariant();

        private static bool IsMedalDifficulty(SkirmishDifficultyId difficulty) =>
            difficulty >= SkirmishDifficultyId.Recruit && difficulty <= SkirmishDifficultyId.Commander;

        private static bool Contains(string value, string search) =>
            !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        private void RefreshMedals()
        {
            if (_medalSource == null)
                return;
            _ledger.Replace(_medalSource());
        }

        private void Persist()
        {
            if (_suspend == 0)
                SaveToPrefs();
        }

        private void Notify()
        {
            if (_suspend == 0)
                Changed?.Invoke();
        }

        private void Raise(SkirmishLibraryInteractionKind kind, string targetId, UIAudioEventKind sound)
        {
            if (_suspend != 0 || _interactionDepth != 0)
                return;
            Interaction?.Invoke(new SkirmishLibraryInteraction(kind, targetId, sound));
        }

        private sealed class PlayerPrefsLibraryStore : ISkirmishLibraryPrefs
        {
            public string Get(string key, string fallback) => PlayerPrefs.GetString(key, fallback ?? string.Empty);
            public void Set(string key, string value) => PlayerPrefs.SetString(key, value ?? string.Empty);
            public void Save() => PlayerPrefs.Save();
        }
    }
}
