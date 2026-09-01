using System;
using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class RankingV3View : UIScreenView
    {
        [SerializeField] private TMP_Text creditsValue;
        [SerializeField] private TMP_Text commandValue;
        [SerializeField] private Button[] categoryButtons;
        [SerializeField] private V3GradientGraphic[] categoryGradients;
        [SerializeField] private GameObject[] categoryBodies;
        [SerializeField] private Button viewRewardsButton;

        private UnityAction[] _categoryActions = Array.Empty<UnityAction>();
        private int _selectedCategory;

        public Button[] CategoryButtons => categoryButtons;
        public GameObject[] CategoryBodies => categoryBodies;
        public Button ViewRewardsButton => viewRewardsButton;

        public void Configure(
            TMP_Text configuredCredits,
            TMP_Text configuredCommand,
            Button[] configuredCategoryButtons,
            V3GradientGraphic[] configuredCategoryGradients,
            GameObject[] configuredCategoryBodies,
            Button configuredViewRewards)
        {
            creditsValue = configuredCredits;
            commandValue = configuredCommand;
            categoryButtons = configuredCategoryButtons;
            categoryGradients = configuredCategoryGradients;
            categoryBodies = configuredCategoryBodies;
            viewRewardsButton = configuredViewRewards;
        }

        private void Awake()
        {
            SetRouteForTests(UIRoute.Ranking);
            WireButtons();
            SelectCategory(0);
        }

        private void OnEnable()
        {
            RefreshResources();
            RefreshCategory();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < Mathf.Min(categoryButtons?.Length ?? 0, _categoryActions.Length); i++)
                categoryButtons[i]?.onClick.RemoveListener(_categoryActions[i]);
            viewRewardsButton?.onClick.RemoveListener(ShowSeasonRewards);
        }

        private void WireButtons()
        {
            _categoryActions = new UnityAction[categoryButtons?.Length ?? 0];
            for (int i = 0; i < _categoryActions.Length; i++)
            {
                int index = i;
                _categoryActions[i] = () => SelectCategory(index);
                categoryButtons[i]?.onClick.AddListener(_categoryActions[i]);
            }
            viewRewardsButton?.onClick.AddListener(ShowSeasonRewards);
        }

        private void ShowSeasonRewards() => SelectCategory(3);

        private void SelectCategory(int index)
        {
            _selectedCategory = Mathf.Clamp(index, 0, Math.Max(0, (categoryButtons?.Length ?? 1) - 1));
            RefreshCategory();
        }

        private void RefreshCategory()
        {
            for (int i = 0; i < (categoryGradients?.Length ?? 0); i++)
            {
                bool selected = i == _selectedCategory;
                categoryGradients[i]?.ConfigureCorners(
                    selected ? new Color32(26, 136, 216, 255) : new Color32(31, 42, 46, 255),
                    selected ? new Color32(7, 89, 158, 255) : new Color32(20, 30, 33, 255),
                    selected ? new Color32(2, 46, 83, 255) : new Color32(5, 12, 15, 255),
                    selected ? new Color32(4, 67, 116, 255) : new Color32(8, 17, 19, 255),
                    selected ? new Color32(0, 184, 235, 255) : new Color32(65, 78, 83, 255),
                    3f);
            }

            for (int i = 0; i < (categoryBodies?.Length ?? 0); i++)
                categoryBodies[i]?.SetActive(i == _selectedCategory);
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
    }
}
