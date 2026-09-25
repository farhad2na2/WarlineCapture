using System;
using System.Collections.Generic;
using Game.Configs;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class QuickCustomScreenView
    {
        private const string LastScenarioIdPrefsKey = "Warline.Skirmish.LastScenarioId";
        private const float LibraryCardHeight = 80f;
        private const float MapTabMinWidth = 120f;
        private const float MapTabHeight = 72f;
        private static readonly string[] MapCollectionIds = { "", "DB", "CC", "MP", "IB", "AP" };
        private static readonly string[] MapCollectionLabelsEn =
            { "ALL", "DESERT", "CITY", "PASS", "BASIN", "AIR" };
        private static readonly string[] MapCollectionLabelsFa =
            { "همه", "کویر", "شهر", "گردنه", "حوضه", "هوا" };

        [SerializeField] private Button[] scenarioButtons;
        [SerializeField] private RectTransform battleLibraryContent;
        [SerializeField] private RectTransform battleLibraryMapTabs;
        [SerializeField] private TMP_InputField battleLibrarySearch;
        [SerializeField] private TMP_Text scenarioDescription;
        [SerializeField] private TMP_Text battleLibraryCountLabel;

        private readonly List<SkirmishBattleCatalogEntry> _catalogEntries = new();
        private readonly List<SkirmishBattleCatalogEntry> _visibleEntries = new();
        private readonly List<Button> _libraryButtons = new();
        private readonly List<Button> _mapTabButtons = new();
        private string _selectedScenarioId;
        private string _mapFilter = string.Empty;
        private string _searchFilter = string.Empty;
        private TMP_FontAsset _libraryLabelFont;
        private bool _librarySearchWired;

        private void WireScenarioChoices()
        {
            if (string.IsNullOrEmpty(_selectedScenarioId))
                _selectedScenarioId = PlayerPrefs.GetString(LastScenarioIdPrefsKey, string.Empty);
            WireLibrarySearch();
            EnsureMapTabs();
            EnsureLibraryCards();
        }

        public void SelectScenario(int index)
        {
            if (!baseAssaultPreset || index < 0)
                return;
            EnsureLibraryCards();
            for (int i = 0; i < _catalogEntries.Count; i++)
            {
                if (_catalogEntries[i].IsPlayable && _catalogEntries[i].PlayableScenarioIndex == index)
                {
                    SelectScenarioId(_catalogEntries[i].ScenarioId);
                    return;
                }
            }
        }

        public void SelectScenarioId(string scenarioId)
        {
            if (!baseAssaultPreset || string.IsNullOrEmpty(scenarioId))
                return;
            EnsureLibraryCards();
            if (!TryFindEntry(scenarioId, out SkirmishBattleCatalogEntry entry))
                return;

            _selectedScenarioId = entry.ScenarioId;
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

        public void SelectMapCollection(string mapId)
        {
            _mapFilter = mapId ?? string.Empty;
            EnsureLibraryCards();
            PresentScenarioChoices();
        }

        private void PresentScenarioChoices()
        {
            EnsureMapTabs();
            EnsureLibraryCards();
            ResolveSelectedEntry(out SkirmishBattleCatalogEntry selected);
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            PresentMapTabs(fa);

            for (int i = 0; i < _libraryButtons.Count; i++)
            {
                Button button = _libraryButtons[i];
                if (button == null)
                    continue;
                if (i >= _visibleEntries.Count)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                button.gameObject.SetActive(true);
                SkirmishBattleCatalogEntry entry = _visibleEntries[i];
                bool isSelected = string.Equals(entry.ScenarioId, _selectedScenarioId, StringComparison.Ordinal);
                string status = entry.IsPlayable
                    ? (isSelected
                        ? (fa ? "آماده · انتخاب‌شده" : "READY · SELECTED")
                        : (fa ? "آماده · انتخاب" : "READY · SELECT"))
                    : SkirmishExpandedCopyProjection.IsExpandedS002(entry.ScenarioId)
                        ? (fa ? "در حال پیشرفت" : "IN PROGRESS")
                        : (fa ? "در حال ساخت" : "PLANNED");
                UiLocalizedText.Set(
                    button.GetComponentInChildren<TMP_Text>(),
                    entry.ScenarioId + "  " + ResolveCardTitle(entry) + "\n" + status);

                if (button.targetGraphic is V3GradientGraphic gradient)
                {
                    if (entry.IsPlayable)
                    {
                        gradient.Configure(
                            isSelected ? new Color(0f, .38f, .52f) : new Color(.12f, .17f, .18f),
                            new Color(.02f, .06f, .08f),
                            isSelected ? new Color(0f, .8f, 1f) : new Color(.3f, .36f, .38f),
                            isSelected ? 4f : 2f);
                    }
                    else
                    {
                        gradient.Configure(
                            isSelected ? new Color(.22f, .18f, .08f) : new Color(.1f, .1f, .11f),
                            new Color(.04f, .04f, .05f),
                            isSelected ? new Color(.85f, .65f, .2f) : new Color(.28f, .28f, .3f),
                            isSelected ? 3f : 1f);
                    }
                }
            }

            if (battleLibraryCountLabel != null)
            {
                UiLocalizedText.Set(
                    battleLibraryCountLabel,
                    fa
                        ? $"نمایش {_visibleEntries.Count} از {_catalogEntries.Count} نبرد"
                        : $"SHOWING {_visibleEntries.Count} OF {_catalogEntries.Count} BATTLES");
            }

            if (scenarioDescription != null)
            {
                if (SkirmishExpandedCopyProjection.IsExpandedS002(selected.ScenarioId) &&
                    SkirmishExpandedCopyProjection.TryResolveS002LibraryBriefing(
                        fa ? GameLocalization.PersianLocaleCode : GameLocalization.EnglishLocaleCode,
                        out SkirmishLibraryBriefing briefing))
                {
                    UiLocalizedText.Set(scenarioDescription, briefing.Brief);
                }
                else if (!selected.IsPlayable)
                {
                    UiLocalizedText.Set(
                        scenarioDescription,
                        fa
                            ? "این نبرد هنوز آماده نیست. یکی از نبردهای Ready را انتخاب کن."
                            : "This battle is still in development. Select a Ready battle to deploy.");
                }
                else
                {
                    UiLocalizedText.Set(
                        scenarioDescription,
                        fa
                            ? (string.IsNullOrEmpty(selected.DescriptionFarsi)
                                ? selected.DescriptionEnglish
                                : selected.DescriptionFarsi)
                            : selected.DescriptionEnglish);
                }
            }

            PresentCompiledBriefing(selected, fa);
            if (launchButton != null)
                launchButton.interactable = selected.IsPlayable;
        }

        private readonly Dictionary<TMP_Text, (string key, string fallback)> _authoredRuleCopy = new();
        private string _briefingCatalogId;
        private int _briefingSeed;
        private SkirmishResolvedSetup _briefingSetup;

        private void PresentCompiledBriefing(SkirmishBattleCatalogEntry selected, bool fa)
        {
            if (_briefingCatalogId != selected.ScenarioId || _briefingSeed != _config.MapSeed)
            {
                _briefingCatalogId = selected.ScenarioId;
                _briefingSeed = _config.MapSeed;
                _briefingSetup = null;
                if (selected.PlayableScenarioIndex == SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex ||
                    selected.PlayableScenarioIndex == SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex)
                    SkirmishSetupBriefing.TryLoad(selected.ScenarioId, _config.MapSeed, out _briefingSetup);
            }
            string[] names = { "Opponent", "Objective", "Roster", "Economy", "Intel" };
            var copy = _briefingSetup != null ? new SkirmishSetupBriefing(_briefingSetup, fa) : default;
            string[] values = { copy.Summary, copy.Objective, copy.Roster, copy.Economy, copy.Intel };
            for (int i = 0; i < names.Length; i++)
            {
                var label = transform.Find("SkirmishSetupComposition/BaseAssaultRules/" + names[i])?.GetComponent<TMP_Text>();
                if (label == null) continue;
                var binding = label.GetComponent<V3LocalizedTextBindingView>();
                if (binding == null) continue;
                if (!_authoredRuleCopy.ContainsKey(label))
                    _authoredRuleCopy.Add(label, (binding.LocalizationKey, binding.EnglishFallback));
                if (_briefingSetup != null) binding.SetLocalizedValue(values[i]);
                else
                {
                    var original = _authoredRuleCopy[label];
                    binding.Configure(original.key, original.fallback);
                    binding.ApplyLocalization();
                }
            }
        }

        private void WireLibrarySearch()
        {
            if (_librarySearchWired || battleLibrarySearch == null)
                return;
            battleLibrarySearch.onValueChanged.AddListener(OnLibrarySearchChanged);
            _librarySearchWired = true;
        }

        private void OnLibrarySearchChanged(string value)
        {
            _searchFilter = value ?? string.Empty;
            EnsureLibraryCards();
            PresentScenarioChoices();
        }

        private void EnsureMapTabs()
        {
            if (!baseAssaultPreset || battleLibraryMapTabs == null)
                return;
            if (_libraryLabelFont == null)
            {
                var title = transform.Find("SkirmishSetupComposition/OperationPreview/OperationName")
                    ?.GetComponent<TMP_Text>();
                _libraryLabelFont = title != null ? title.font : TMP_Settings.defaultFontAsset;
            }

            while (_mapTabButtons.Count > MapCollectionIds.Length)
            {
                int last = _mapTabButtons.Count - 1;
                Button button = _mapTabButtons[last];
                _mapTabButtons.RemoveAt(last);
                if (button != null)
                    Destroy(button.gameObject);
            }

            for (int i = 0; i < MapCollectionIds.Length; i++)
            {
                Button button;
                if (i < _mapTabButtons.Count && _mapTabButtons[i] != null)
                {
                    button = _mapTabButtons[i];
                }
                else
                {
                    string mapId = MapCollectionIds[i];
                    var rect = new GameObject(
                            string.IsNullOrEmpty(mapId) ? "MapTab_ALL" : "MapTab_" + mapId,
                            typeof(RectTransform),
                            typeof(CanvasRenderer),
                            typeof(V3GradientGraphic),
                            typeof(Button),
                            typeof(LayoutElement))
                        .GetComponent<RectTransform>();
                    rect.SetParent(battleLibraryMapTabs, false);

                    var graphic = rect.GetComponent<V3GradientGraphic>();
                    graphic.Configure(new Color(.12f, .17f, .18f), new Color(.02f, .06f, .08f), new Color(.3f, .36f, .38f), 2f);
                    button = rect.GetComponent<Button>();
                    button.targetGraphic = graphic;
                    string captured = mapId;
                    button.onClick.AddListener(() => SelectMapCollection(captured));

                    var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                        .GetComponent<TextMeshProUGUI>();
                    label.transform.SetParent(rect, false);
                    label.font = _libraryLabelFont;
                    label.alignment = TextAlignmentOptions.Center;
                    label.raycastTarget = false;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.overflowMode = TextOverflowModes.Ellipsis;
                    Stretch(label.rectTransform, 4f, 4f, 6f, 6f);

                    if (i < _mapTabButtons.Count)
                        _mapTabButtons[i] = button;
                    else
                        _mapTabButtons.Add(button);
                }

                var layoutElement = button.GetComponent<LayoutElement>() ??
                                    button.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = MapTabMinWidth;
                layoutElement.minWidth = MapTabMinWidth;
                layoutElement.preferredHeight = MapTabHeight;
                layoutElement.minHeight = MapTabHeight;
                layoutElement.flexibleWidth = 1f;
                layoutElement.flexibleHeight = 0f;

                var tabLabel = button.GetComponentInChildren<TMP_Text>();
                if (tabLabel != null)
                {
                    tabLabel.fontSize = 22f;
                    tabLabel.textWrappingMode = TextWrappingModes.NoWrap;
                    tabLabel.overflowMode = TextOverflowModes.Ellipsis;
                }
            }
        }

        private void PresentMapTabs(bool fa)
        {
            for (int i = 0; i < _mapTabButtons.Count && i < MapCollectionIds.Length; i++)
            {
                Button button = _mapTabButtons[i];
                if (button == null)
                    continue;
                bool selected = string.Equals(_mapFilter, MapCollectionIds[i], StringComparison.Ordinal);
                UiLocalizedText.Set(
                    button.GetComponentInChildren<TMP_Text>(),
                    fa ? MapCollectionLabelsFa[i] : MapCollectionLabelsEn[i]);
                if (button.targetGraphic is V3GradientGraphic gradient)
                {
                    gradient.Configure(
                        selected ? new Color(0f, .38f, .52f) : new Color(.12f, .17f, .18f),
                        new Color(.02f, .06f, .08f),
                        selected ? new Color(0f, .8f, 1f) : new Color(.3f, .36f, .38f),
                        selected ? 3f : 1f);
                }
            }
        }

        private void EnsureLibraryCards()
        {
            if (!baseAssaultPreset)
                return;

            ReloadCatalogEntries();
            RebuildVisibleEntries();

            if (battleLibraryContent == null)
                return;

            if (_libraryLabelFont == null)
            {
                var title = transform.Find("SkirmishSetupComposition/OperationPreview/OperationName")
                    ?.GetComponent<TMP_Text>();
                _libraryLabelFont = title != null ? title.font : TMP_Settings.defaultFontAsset;
            }

            int needed = Mathf.Max(_visibleEntries.Count, 1);
            while (_libraryButtons.Count > needed)
            {
                int last = _libraryButtons.Count - 1;
                Button button = _libraryButtons[last];
                _libraryButtons.RemoveAt(last);
                if (button != null)
                    Destroy(button.gameObject);
            }

            for (int i = 0; i < _visibleEntries.Count; i++)
            {
                if (i < _libraryButtons.Count && _libraryButtons[i] != null)
                {
                    // Rebind click to current visible id in case the filtered set changed.
                    string scenarioId = _visibleEntries[i].ScenarioId;
                    Button existing = _libraryButtons[i];
                    existing.onClick.RemoveAllListeners();
                    existing.onClick.AddListener(() => SelectScenarioId(scenarioId));
                    existing.gameObject.name = "ScenarioCard_" + scenarioId;
                    continue;
                }

                string newId = _visibleEntries[i].ScenarioId;
                var rect = new GameObject(
                        "ScenarioCard_" + newId,
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(V3GradientGraphic),
                        typeof(Button),
                        typeof(LayoutElement))
                    .GetComponent<RectTransform>();
                rect.SetParent(battleLibraryContent, false);
                var layoutElement = rect.GetComponent<LayoutElement>();
                layoutElement.preferredHeight = LibraryCardHeight;
                layoutElement.minHeight = LibraryCardHeight;
                layoutElement.flexibleWidth = 1f;

                var graphic = rect.GetComponent<V3GradientGraphic>();
                graphic.Configure(new Color(0f, .3f, .4f), Color.black, Color.cyan, 3f);
                var button = rect.GetComponent<Button>();
                button.targetGraphic = graphic;
                string captured = newId;
                button.onClick.AddListener(() => SelectScenarioId(captured));

                var label = new GameObject(
                        "Label",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(TextMeshProUGUI))
                    .GetComponent<TextMeshProUGUI>();
                label.transform.SetParent(rect, false);
                label.font = _libraryLabelFont;
                label.fontSize = 20f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
                label.textWrappingMode = TextWrappingModes.Normal;
                Stretch(label.rectTransform, 10f, 10f, 6f, 6f);

                if (i < _libraryButtons.Count)
                    _libraryButtons[i] = button;
                else
                    _libraryButtons.Add(button);
            }
        }

        private void ReloadCatalogEntries()
        {
            _catalogEntries.Clear();
            SkirmishBattleCatalogConfig catalog = SkirmishBattleCatalogConfig.Load();
            if (catalog != null)
            {
                for (int i = 0; i < catalog.Entries.Count; i++)
                {
                    SkirmishBattleCatalogEntry entry = catalog.Entries[i];
                    SkirmishExpandedCopyProjection.ApplyLibraryCopy(ref entry);
                    _catalogEntries.Add(entry);
                }
            }
            else
                SeedFallbackPlayables(_catalogEntries);
        }

        private void RebuildVisibleEntries()
        {
            _visibleEntries.Clear();
            string search = _searchFilter.Trim();
            for (int i = 0; i < _catalogEntries.Count; i++)
            {
                SkirmishBattleCatalogEntry entry = _catalogEntries[i];
                if (!string.IsNullOrEmpty(_mapFilter) &&
                    !string.Equals(entry.MapId, _mapFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.IsNullOrEmpty(search) && !EntryMatchesSearch(entry, search))
                    continue;
                _visibleEntries.Add(entry);
            }

            // Ready battles first so the two shipped missions stay immediately selectable.
            _visibleEntries.Sort(CompareCatalogEntriesForDisplay);
        }

        private static int CompareCatalogEntriesForDisplay(SkirmishBattleCatalogEntry a, SkirmishBattleCatalogEntry b)
        {
            if (a.IsPlayable != b.IsPlayable)
                return a.IsPlayable ? -1 : 1;
            if (a.IsPlayable && b.IsPlayable)
                return a.PlayableScenarioIndex.CompareTo(b.PlayableScenarioIndex);
            return string.CompareOrdinal(a.ScenarioId, b.ScenarioId);
        }

        private static bool EntryMatchesSearch(SkirmishBattleCatalogEntry entry, string search)
        {
            return (entry.ScenarioId?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (entry.TitleEnglish?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (entry.MapId?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (entry.ObjectiveId?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (entry.ArmyProfile?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                || (entry.StartProfile?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        private void ResolveSelectedEntry(out SkirmishBattleCatalogEntry selected)
        {
            if (!string.IsNullOrEmpty(_selectedScenarioId) &&
                TryFindEntry(_selectedScenarioId, out selected))
            {
                if (selected.IsPlayable)
                    _config.ScenarioIndex = selected.PlayableScenarioIndex;
                return;
            }

            if (TryFindByScenarioIndex(_config.ScenarioIndex, out selected))
            {
                _selectedScenarioId = selected.ScenarioId;
                return;
            }

            string remembered = PlayerPrefs.GetString(LastScenarioIdPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(remembered) && TryFindEntry(remembered, out selected))
            {
                _selectedScenarioId = selected.ScenarioId;
                if (selected.IsPlayable)
                    _config.ScenarioIndex = selected.PlayableScenarioIndex;
                return;
            }

            for (int i = 0; i < _catalogEntries.Count; i++)
            {
                if (!_catalogEntries[i].IsPlayable)
                    continue;
                selected = _catalogEntries[i];
                _selectedScenarioId = selected.ScenarioId;
                _config.ScenarioIndex = selected.PlayableScenarioIndex;
                return;
            }

            selected = default;
            _selectedScenarioId = SkirmishBattleCatalogConfig.DesertBaseScenarioId;
            _config.ScenarioIndex = 0;
        }

        // Kept for Bind() compatibility with the previous playable-only helper name.
        private void ResolveSelectedPlayable(out SkirmishBattleCatalogEntry selected) =>
            ResolveSelectedEntry(out selected);

        private bool TryFindEntry(string scenarioId, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < _catalogEntries.Count; i++)
            {
                if (string.Equals(_catalogEntries[i].ScenarioId, scenarioId, StringComparison.Ordinal))
                {
                    entry = _catalogEntries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private bool TryFindByScenarioIndex(int scenarioIndex, out SkirmishBattleCatalogEntry entry)
        {
            for (int i = 0; i < _catalogEntries.Count; i++)
            {
                if (_catalogEntries[i].IsPlayable && _catalogEntries[i].PlayableScenarioIndex == scenarioIndex)
                {
                    entry = _catalogEntries[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private bool TryFindPlayable(string scenarioId, out SkirmishBattleCatalogEntry entry) =>
            TryFindEntry(scenarioId, out entry) && entry.IsPlayable;

        private static string ResolveCardTitle(SkirmishBattleCatalogEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.TitleKey))
            {
                string localized = UiShellRuntimeGateway.Localization.Get(entry.TitleKey, entry.TitleEnglish);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }

            return string.IsNullOrEmpty(entry.TitleEnglish) ? entry.ScenarioId : entry.TitleEnglish;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SeedFallbackPlayables(List<SkirmishBattleCatalogEntry> destination)
        {
            destination.Add(new SkirmishBattleCatalogEntry
            {
                ScenarioId = SkirmishBattleCatalogConfig.DesertBaseScenarioId,
                MapId = "DB",
                ObjectiveId = "BA",
                ArmyProfile = "G",
                StartProfile = "F",
                TitleKey = "ui.skirmish.base_assault_map",
                TitleEnglish = "DESERT BASE",
                DescriptionEnglish =
                    "Advance from the western base. Fight through the city or approach around its edge.",
                DescriptionFarsi =
                    "از پایگاه غربی جلو برو؛ از وسط شهر یا مسیر کنار شهر به دشمن برس.",
                Status = SkirmishBattleCatalogStatus.Playable,
                PlayableScenarioIndex = SkirmishPresetConfig.DesertBaseScenarioIndex
            });
            destination.Add(new SkirmishBattleCatalogEntry
            {
                ScenarioId = SkirmishBattleCatalogConfig.CityCrossroadsScenarioId,
                MapId = "CC",
                ObjectiveId = "BA",
                ArmyProfile = "G",
                StartProfile = "F",
                TitleKey = "ui.skirmish.city_crossroads_map",
                TitleEnglish = "CITY CROSSROADS",
                DescriptionEnglish =
                    "Attack from the north toward the southern base. Use city streets or flank around the blocks.",
                DescriptionFarsi =
                    "از شمال شهر به پایگاه جنوبی حمله کن؛ از خیابون‌ها یا مسیرهای کناری جلو برو.",
                Status = SkirmishBattleCatalogStatus.Playable,
                PlayableScenarioIndex = SkirmishPresetConfig.CityCrossroadsScenarioIndex
            });
            destination.Add(new SkirmishBattleCatalogEntry
            {
                ScenarioId = SkirmishBattleCatalogConfig.IndustrialBasinScenarioId,
                MapId = "IB",
                ObjectiveId = "BA",
                ArmyProfile = "G",
                StartProfile = "F",
                TitleKey = "ui.skirmish.industrial_basin_map",
                TitleEnglish = "INDUSTRIAL BASIN",
                DescriptionEnglish =
                    "Advance from the northwest industrial yard toward the southeast base. Use the freight avenue or service ring.",
                DescriptionFarsi =
                    "از حیاط صنعتی شمال‌غربی به پایگاه جنوب‌شرقی حمله کن؛ از مسیر باربری یا حلقه خدماتی جلو برو.",
                Status = SkirmishBattleCatalogStatus.Playable,
                PlayableScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex
            });
        }
    }

    internal static class SkirmishBattleCatalogEntryExtensions
    {
        public static bool ScenarioIndexMatches(this SkirmishBattleCatalogEntry entry, int scenarioIndex) =>
            entry.IsPlayable && entry.PlayableScenarioIndex == scenarioIndex;
    }
}
