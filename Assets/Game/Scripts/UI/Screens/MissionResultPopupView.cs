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

        [Header("V3 outcome presentation")]
        [SerializeField] private TMP_Text missionIdentityText;
        [SerializeField] private TMP_Text missionStatusText;
        [SerializeField] private TMP_Text starCountText;
        [SerializeField] private TMP_Text civilianLostText;
        [SerializeField] private TMP_Text objectivePatrolStatusText;
        [SerializeField] private TMP_Text objectiveSquadStatusText;
        [SerializeField] private TMP_Text objectiveCivilianStatusText;
        [SerializeField] private TMP_Text[] outcomeAccentTexts;
        [SerializeField] private Image[] outcomeAccentImages;
        [SerializeField] private TMP_Text[] outcomeSecondaryAccentTexts;
        [SerializeField] private Image[] outcomeSecondaryAccentImages;
        [SerializeField] private V3StarGraphic[] outcomeSecondaryAccentStars;
        [SerializeField] private Image outcomeEmblemImage;
        [SerializeField] private Image timerIconImage;
        [SerializeField] private Image footerAccentImage;
        [SerializeField] private Image backgroundTintImage;
        [SerializeField] private GameObject victoryRewardIconsRoot;
        [SerializeField] private Sprite victoryEmblemSprite;
        [SerializeField] private Sprite lossEmblemSprite;
        [SerializeField] private V3GradientGraphic emblemGradient;
        [SerializeField] private V3GradientGraphic primaryActionGradient;
        [SerializeField] private V3GradientGraphic retryActionGradient;

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
            SetText(missionIdentityText, BuildMissionIdentity(model));
            SetText(summaryText, model.SummaryBody);
            SetText(elapsedText, model.ElapsedText);
            SetText(squadLossText, model.SquadLossText);
            SetText(enemiesDefeatedText, model.EnemiesDefeatedText);
            SetText(rewardsText, BuildRewardDisplay(model.RewardsText));
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
            ApplyV3Outcome(model);
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

        public void ConfigureV3(
            TMP_Text missionIdentity,
            TMP_Text missionStatus,
            TMP_Text starCount,
            TMP_Text civilianLost,
            TMP_Text patrolStatus,
            TMP_Text squadStatus,
            TMP_Text civilianStatus,
            TMP_Text[] accentTexts,
            Image[] accentImages,
            TMP_Text[] secondaryAccentTexts,
            Image[] secondaryAccentImages,
            V3StarGraphic[] secondaryAccentStars,
            Image emblemImage,
            Image timerIcon,
            Image footerAccent,
            Image backdropTint,
            GameObject rewardIconsRoot,
            Sprite victoryEmblem,
            Sprite lossEmblem,
            V3GradientGraphic emblemPanel,
            V3GradientGraphic primaryGradient,
            V3GradientGraphic retryGradient)
        {
            missionIdentityText = missionIdentity;
            missionStatusText = missionStatus;
            starCountText = starCount;
            civilianLostText = civilianLost;
            objectivePatrolStatusText = patrolStatus;
            objectiveSquadStatusText = squadStatus;
            objectiveCivilianStatusText = civilianStatus;
            outcomeAccentTexts = accentTexts;
            outcomeAccentImages = accentImages;
            outcomeSecondaryAccentTexts = secondaryAccentTexts;
            outcomeSecondaryAccentImages = secondaryAccentImages;
            outcomeSecondaryAccentStars = secondaryAccentStars;
            outcomeEmblemImage = emblemImage;
            timerIconImage = timerIcon;
            footerAccentImage = footerAccent;
            backgroundTintImage = backdropTint;
            victoryRewardIconsRoot = rewardIconsRoot;
            victoryEmblemSprite = victoryEmblem;
            lossEmblemSprite = lossEmblem;
            emblemGradient = emblemPanel;
            primaryActionGradient = primaryGradient;
            retryActionGradient = retryGradient;
        }
#endif

        private void OnPrimaryRequested() => primaryRequested?.Invoke();
        private void OnRetryRequested() => retryRequested?.Invoke();

        private void ApplyV3Outcome(in UiMissionResultPopupModel model)
        {
            bool victory = model.Outcome == UiMissionResultOutcome.Victory;
            Color accent = victory
                ? new Color32(102, 190, 45, 255)
                : new Color32(232, 58, 31, 255);
            Color actionTop = victory
                ? new Color32(79, 153, 45, 255)
                : new Color32(190, 48, 27, 255);
            Color actionBottom = victory
                ? new Color32(18, 70, 24, 255)
                : new Color32(76, 13, 10, 255);

            if (titleText != null)
                titleText.color = accent;
            SetText(missionStatusText, victory ? "MISSION COMPLETE" : "COMMAND SQUAD LOST");
            SetText(starCountText, $"{model.Stars} / 3 STARS");
            SetText(civilianLostText, "0");
            SetText(objectivePatrolStatusText, victory ? "COMPLETE" : "FAILED");
            SetText(objectiveSquadStatusText, victory ? "COMPLETE" : "FAILED");
            SetText(objectiveCivilianStatusText, victory ? "STABLE" : "AT RISK");
            if (missionStatusText != null)
                missionStatusText.color = accent;
            if (starCountText != null)
                starCountText.color = victory ? (Color)new Color32(246, 177, 22, 255) : accent;
            if (elapsedText != null)
                elapsedText.color = victory ? (Color)new Color32(28, 123, 194, 255) : accent;

            if (outcomeAccentTexts != null)
                for (int index = 0; index < outcomeAccentTexts.Length; index++)
                    if (outcomeAccentTexts[index] != null)
                        outcomeAccentTexts[index].color = accent;
            if (objectiveCivilianStatusText != null && !victory)
                objectiveCivilianStatusText.color = new Color32(242, 140, 20, 255);
            if (outcomeAccentImages != null)
                for (int index = 0; index < outcomeAccentImages.Length; index++)
                    if (outcomeAccentImages[index] != null)
                        outcomeAccentImages[index].color = accent;
            Color secondaryAccent = victory ? (Color)new Color32(28, 123, 194, 255) : accent;
            if (outcomeSecondaryAccentTexts != null)
                for (int index = 0; index < outcomeSecondaryAccentTexts.Length; index++)
                    if (outcomeSecondaryAccentTexts[index] != null)
                        outcomeSecondaryAccentTexts[index].color = secondaryAccent;
            if (outcomeSecondaryAccentImages != null)
                for (int index = 0; index < outcomeSecondaryAccentImages.Length; index++)
                    if (outcomeSecondaryAccentImages[index] != null)
                        outcomeSecondaryAccentImages[index].color = secondaryAccent;
            if (outcomeSecondaryAccentStars != null)
                for (int index = 0; index < outcomeSecondaryAccentStars.Length; index++)
                    outcomeSecondaryAccentStars[index]?.SetState(
                        secondaryAccent, false, new Color32(5, 13, 16, 255));
            if (timerIconImage != null)
                timerIconImage.color = secondaryAccent;
            if (footerAccentImage != null)
                footerAccentImage.color = accent;
            if (outcomeEmblemImage != null)
            {
                outcomeEmblemImage.sprite = victory ? victoryEmblemSprite : lossEmblemSprite;
                outcomeEmblemImage.color = accent;
                outcomeEmblemImage.preserveAspect = true;
            }
            if (backgroundTintImage != null)
                backgroundTintImage.color = victory
                    ? new Color(0.12f, 0.09f, 0.02f, 0.08f)
                    : new Color(0.015f, 0.025f, 0.032f, 0.64f);
            if (victoryRewardIconsRoot != null)
                victoryRewardIconsRoot.SetActive(victory);
            if (rewardsText != null)
            {
                rewardsText.color = victory
                    ? new Color32(246, 174, 23, 255)
                    : new Color32(129, 132, 132, 255);
                rewardsText.alignment = victory
                    ? TextAlignmentOptions.MidlineRight
                    : TextAlignmentOptions.Center;
                RectTransform rewardRect = rewardsText.rectTransform;
                rewardRect.anchorMin = rewardRect.anchorMax = new Vector2(0f, 1f);
                rewardRect.pivot = new Vector2(0f, 1f);
                rewardRect.anchoredPosition = victory ? new Vector2(86f, -72f) : new Vector2(3f, -70f);
                rewardRect.sizeDelta = victory ? new Vector2(354f, 97f) : new Vector2(469f, 100f);
            }
            emblemGradient?.SetGradient(
                victory ? new Color32(29, 76, 20, 255) : new Color32(91, 19, 14, 255),
                victory ? new Color32(8, 29, 9, 255) : new Color32(31, 5, 5, 255));
            emblemGradient?.SetBorder(accent, 3f);
            primaryActionGradient?.SetGradient(actionTop, actionBottom);
            primaryActionGradient?.SetBorder(accent, 3f);
            retryActionGradient?.SetGradient(actionTop, actionBottom);
            retryActionGradient?.SetBorder(accent, 3f);

            if (starRoots != null)
            {
                for (int index = 0; index < starRoots.Length; index++)
                {
                    GameObject starRoot = starRoots[index];
                    if (starRoot == null)
                        continue;
                    starRoot.SetActive(true);
                    bool filled = index < model.Stars;
                    Transform filledRoot = starRoot.transform.Find("StarFilled");
                    Transform outlineRoot = starRoot.transform.Find("StarOutline");
                    if (filledRoot != null)
                        filledRoot.gameObject.SetActive(filled);
                    if (outlineRoot != null)
                    {
                        outlineRoot.gameObject.SetActive(!filled);
                        outlineRoot.GetComponent<V3StarGraphic>()?.SetState(
                            accent, true, new Color32(5, 13, 16, 255));
                    }
                    TMP_Text star = starRoot.GetComponentInChildren<TMP_Text>(true);
                    if (star != null)
                    {
                        star.text = filled ? "★" : "☆";
                        star.color = filled ? (Color)new Color32(246, 177, 22, 255) : accent;
                    }
                }
            }
        }

        private static string BuildMissionIdentity(in UiMissionResultPopupModel model)
        {
            string subtitle = model.Subtitle ?? string.Empty;
            string[] parts = subtitle.Split(new[] { " • " }, StringSplitOptions.None);
            if (parts.Length < 2)
                return subtitle;
            string missionNumber = model.MissionId.Contains("m02") ? "M02" : "M01";
            return $"{missionNumber} {parts[0]}\n{parts[1]}";
        }

        private static string BuildRewardDisplay(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.IndexOf("NO REWARD", StringComparison.OrdinalIgnoreCase) >= 0)
                return "NO REWARD";

            string[] rewards = value.Split(new[] { "  ·  ", "  •  " }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < rewards.Length; index++)
            {
                string reward = rewards[index].Trim();
                int separator = reward.IndexOf(' ');
                if (separator <= 0 || !char.IsDigit(reward[0]))
                    continue;
                string amount = reward.Substring(0, separator);
                string label = reward.Substring(separator + 1).Trim();
                rewards[index] = $"{label}     +{amount}";
            }
            return string.Join("\n", rewards);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value) target.text = value;
        }
    }
}
