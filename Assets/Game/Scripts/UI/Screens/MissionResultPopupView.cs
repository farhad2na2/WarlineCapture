using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MissionResultPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text missionNameText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text elapsedText;
        [SerializeField] private TMP_Text squadLossText;
        [SerializeField] private TMP_Text enemiesDefeatedText;
        [SerializeField] private TMP_Text rewardsText;
        [SerializeField] private TMP_Text statisticsText;
        [SerializeField] private GameObject[] starRoots;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryButtonLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private TMP_Text retryButtonLabel;
        [SerializeField] private GameObject[] hiddenLegacyRoots;

        private Action primaryRequested;
        private Action retryRequested;

        private void OnEnable()
        {
            if (primaryButton != null) primaryButton.onClick.AddListener(OnPrimaryRequested);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryRequested);
        }

        private void OnDisable()
        {
            if (primaryButton != null) primaryButton.onClick.RemoveListener(OnPrimaryRequested);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryRequested);
        }

        public void Bind(Action primary, Action retry)
        {
            primaryRequested = primary;
            retryRequested = retry;
        }

        public void Apply(in UiMissionResultPopupModel model)
        {
            SetText(titleText, model.Title);
            SetText(missionNameText, model.Subtitle);
            SetText(summaryText, model.SummaryBody);
            SetText(elapsedText, model.ElapsedText);
            SetText(squadLossText, model.SquadLossText);
            SetText(enemiesDefeatedText, model.EnemiesDefeatedText);
            SetText(rewardsText, model.RewardsText);
            SetText(statisticsText,
                $"SQUAD LOSSES  {model.SquadLossText}     •     ENEMIES DEFEATED  {model.EnemiesDefeatedText}");
            SetText(primaryButtonLabel, model.PrimaryActionLabel);
            SetText(retryButtonLabel, model.PrimaryActionLabel);
            if (primaryButton != null)
            {
                primaryButton.gameObject.SetActive(!model.RetryVisible);
                primaryButton.interactable = model.PrimaryActionEnabled && !model.RetryVisible;
            }
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(model.RetryVisible);
                retryButton.interactable = model.RetryVisible;
            }
            if (starRoots != null)
                for (int index = 0; index < starRoots.Length; index++)
                    if (starRoots[index] != null) starRoots[index].SetActive(index < model.Stars);
            if (hiddenLegacyRoots != null)
                for (int index = 0; index < hiddenLegacyRoots.Length; index++)
                    if (hiddenLegacyRoots[index] != null) hiddenLegacyRoots[index].SetActive(false);
            Button focus = model.RetryVisible ? retryButton : primaryButton;
            if (focus != null && focus.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }

#if UNITY_EDITOR
        public void Configure(
            TMP_Text title, TMP_Text missionName, TMP_Text summary, TMP_Text elapsed,
            TMP_Text squadLoss, TMP_Text enemiesDefeated, TMP_Text rewards, TMP_Text statistics,
            GameObject[] stars, Button primary, TMP_Text primaryLabel, Button retry, TMP_Text retryLabel,
            GameObject[] legacyRoots)
        {
            titleText = title; missionNameText = missionName; summaryText = summary;
            elapsedText = elapsed; squadLossText = squadLoss; enemiesDefeatedText = enemiesDefeated;
            rewardsText = rewards; statisticsText = statistics; starRoots = stars; primaryButton = primary;
            primaryButtonLabel = primaryLabel; retryButton = retry; retryButtonLabel = retryLabel;
            hiddenLegacyRoots = legacyRoots;
        }
#endif

        private void OnPrimaryRequested() => primaryRequested?.Invoke();
        private void OnRetryRequested() => retryRequested?.Invoke();
        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value) target.text = value;
        }
    }
}
