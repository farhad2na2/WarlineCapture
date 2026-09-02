using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum CommandFeedCategory : byte
    {
        All,
        Operations,
        Aria,
        Alerts,
        Rewards
    }

    [DisallowMultipleComponent]
    public sealed class CommandFeedScreenView : UIScreenView
    {
        [SerializeField] private TMP_Text creditsValue;
        [SerializeField] private TMP_Text commandValue;
        [SerializeField] private TMP_Text liveStatusLabel;
        [SerializeField] private Button[] filterButtons;
        [SerializeField] private V3GradientGraphic[] filterGradients;
        [SerializeField] private RectTransform[] feedRows;
        [SerializeField] private CommandFeedCategory[] feedRowCategories;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text pauseLabel;
        [SerializeField] private Button searchButton;

        private UnityAction[] _filterActions = Array.Empty<UnityAction>();
        private bool _wired;
        private bool _paused;
        private bool _searchActive;
        private int _selectedFilter;

        public Button[] FilterButtons => filterButtons;
        public RectTransform[] FeedRows => feedRows;
        public Button PauseButton => pauseButton;
        public Button SearchButton => searchButton;
        public bool IsPaused => _paused;
        public bool IsSearchActive => _searchActive;

        public void Configure(
            TMP_Text configuredCredits,
            TMP_Text configuredCommand,
            TMP_Text configuredLiveStatus,
            Button[] configuredFilters,
            V3GradientGraphic[] configuredFilterGradients,
            RectTransform[] configuredRows,
            CommandFeedCategory[] configuredCategories,
            Button configuredPause,
            TMP_Text configuredPauseLabel,
            Button configuredSearch)
        {
            creditsValue = configuredCredits;
            commandValue = configuredCommand;
            liveStatusLabel = configuredLiveStatus;
            filterButtons = configuredFilters;
            filterGradients = configuredFilterGradients;
            feedRows = configuredRows;
            feedRowCategories = configuredCategories;
            pauseButton = configuredPause;
            pauseLabel = configuredPauseLabel;
            searchButton = configuredSearch;
        }

        private void Awake()
        {
            SetRouteForTests(UIRoute.CommandFeed);
            WireButtons();
            Refresh();
        }

        private void OnEnable()
        {
            WireButtons();
            Refresh();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < Mathf.Min(filterButtons?.Length ?? 0, _filterActions.Length); i++)
                filterButtons[i]?.onClick.RemoveListener(_filterActions[i]);
            pauseButton?.onClick.RemoveListener(TogglePause);
            searchButton?.onClick.RemoveListener(ToggleSearch);
        }

        public void Refresh()
        {
            RefreshResources();
            RefreshFilters();
        }

        public void SelectFilter(int index)
        {
            _selectedFilter = Mathf.Clamp(index, 0, Math.Max(0, (filterButtons?.Length ?? 1) - 1));
            _searchActive = false;
            RefreshFilters();
        }

        public void TogglePause()
        {
            _paused = !_paused;
            RefreshStatus();
        }

        public void ToggleSearch()
        {
            _searchActive = !_searchActive;
            RefreshFilters();
        }

        private void WireButtons()
        {
            if (_wired)
                return;
            _wired = true;
            _filterActions = new UnityAction[filterButtons?.Length ?? 0];
            for (int i = 0; i < _filterActions.Length; i++)
            {
                int index = i;
                _filterActions[i] = () => SelectFilter(index);
                filterButtons[i]?.onClick.AddListener(_filterActions[i]);
            }
            pauseButton?.onClick.AddListener(TogglePause);
            searchButton?.onClick.AddListener(ToggleSearch);
        }

        private void RefreshResources()
        {
            if (!UiShellRuntimeGateway.TryReadMainMenuResources(out UiShellMainMenuResourcesModel resources))
                return;
            if (creditsValue != null && !string.IsNullOrWhiteSpace(resources.CreditsText))
                creditsValue.text = resources.CreditsText;
            if (commandValue != null && !string.IsNullOrWhiteSpace(resources.CommandText))
                commandValue.text = resources.CommandText;
        }

        private void RefreshFilters()
        {
            for (int i = 0; i < (filterGradients?.Length ?? 0); i++)
            {
                bool selected = i == _selectedFilter && !_searchActive;
                filterGradients[i]?.ConfigureCorners(
                    selected ? new Color32(28, 139, 219, 255) : new Color32(30, 42, 46, 255),
                    selected ? new Color32(5, 91, 159, 255) : new Color32(18, 29, 33, 255),
                    selected ? new Color32(2, 47, 83, 255) : new Color32(5, 12, 15, 255),
                    selected ? new Color32(4, 65, 116, 255) : new Color32(7, 16, 19, 255),
                    selected ? new Color32(0, 184, 235, 255) : new Color32(65, 78, 83, 255),
                    3f);
            }

            int visibleIndex = 0;
            for (int i = 0; i < (feedRows?.Length ?? 0); i++)
            {
                bool visible;
                if (_searchActive)
                    visible = i == 1 || i == 4;
                else
                {
                    CommandFeedCategory selected = (CommandFeedCategory)_selectedFilter;
                    visible = selected == CommandFeedCategory.All ||
                              (i < (feedRowCategories?.Length ?? 0) && feedRowCategories[i] == selected);
                }

                RectTransform row = feedRows[i];
                if (row == null)
                    continue;
                row.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                row.anchoredPosition = new Vector2(row.anchoredPosition.x, -visibleIndex * 143f);
                visibleIndex++;
            }
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (pauseLabel != null)
                pauseLabel.text = _paused ? ">" : "||";
            if (liveStatusLabel == null)
                return;
            liveStatusLabel.text = _paused
                ? "PAUSED"
                : _searchActive
                    ? "SEARCH: HOSTILE + INTEL"
                    : "●  UPDATING...";
            liveStatusLabel.color = _paused
                ? new Color32(250, 174, 0, 255)
                : _searchActive
                    ? new Color32(0, 184, 235, 255)
                    : new Color32(112, 205, 44, 255);
        }
    }
}
