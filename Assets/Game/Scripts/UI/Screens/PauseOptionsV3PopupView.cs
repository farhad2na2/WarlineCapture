using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PauseOptionsV3PopupView : MonoBehaviour
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject restartConfirmation;
        [SerializeField] private Button restartConfirmButton;
        [SerializeField] private Button restartCancelButton;
        [SerializeField] private TMP_Text restartStatusText;
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private Button helpCloseButton;
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private TMP_Text currentTimeText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text squadsAliveText;
        [SerializeField] private TMP_Text civilianRiskText;

        private float nextRefreshAt;

        public Button CloseButton => closeButton;
        public Button ResumeButton => resumeButton;
        public Button RestartButton => restartButton;
        public Button SettingsButton => settingsButton;
        public Button HelpButton => helpButton;
        public Button ExitButton => exitButton;
        public GameObject RestartConfirmation => restartConfirmation;
        public GameObject HelpPanel => helpPanel;

        private void OnEnable()
        {
            Bind();
            ShowDefault();
            RefreshLiveText();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshAt)
                return;
            nextRefreshAt = Time.unscaledTime + .25f;
            RefreshLiveText();
        }

        public void Configure(
            Button configuredClose,
            Button configuredResume,
            Button configuredRestart,
            Button configuredSettings,
            Button configuredHelp,
            Button configuredExit,
            GameObject configuredRestartConfirmation,
            Button configuredRestartConfirm,
            Button configuredRestartCancel,
            TMP_Text configuredRestartStatus,
            GameObject configuredHelpPanel,
            Button configuredHelpClose,
            TMP_Text configuredMission,
            TMP_Text configuredCurrentTime,
            TMP_Text configuredObjective,
            TMP_Text configuredSquadsAlive,
            TMP_Text configuredCivilianRisk)
        {
            closeButton = configuredClose;
            resumeButton = configuredResume;
            restartButton = configuredRestart;
            settingsButton = configuredSettings;
            helpButton = configuredHelp;
            exitButton = configuredExit;
            restartConfirmation = configuredRestartConfirmation;
            restartConfirmButton = configuredRestartConfirm;
            restartCancelButton = configuredRestartCancel;
            restartStatusText = configuredRestartStatus;
            helpPanel = configuredHelpPanel;
            helpCloseButton = configuredHelpClose;
            missionText = configuredMission;
            currentTimeText = configuredCurrentTime;
            objectiveText = configuredObjective;
            squadsAliveText = configuredSquadsAlive;
            civilianRiskText = configuredCivilianRisk;
        }

        public void ShowDefault()
        {
            SetActive(restartConfirmation, false);
            SetActive(helpPanel, false);
            SetText(restartStatusText, "RESTART THE CURRENT MISSION FROM THE BEGINNING?");
        }

        private void Bind()
        {
            Add(restartButton, ShowRestart);
            Add(helpButton, ShowHelp);
            Add(restartConfirmButton, ConfirmRestart);
            Add(restartCancelButton, ShowDefault);
            Add(helpCloseButton, ShowDefault);
        }

        private void Unbind()
        {
            Remove(restartButton, ShowRestart);
            Remove(helpButton, ShowHelp);
            Remove(restartConfirmButton, ConfirmRestart);
            Remove(restartCancelButton, ShowDefault);
            Remove(helpCloseButton, ShowDefault);
        }

        private void ShowRestart()
        {
            SetActive(helpPanel, false);
            SetActive(restartConfirmation, true);
            SetText(restartStatusText, "RESTART THE CURRENT MISSION FROM THE BEGINNING?");
        }

        private void ShowHelp()
        {
            SetActive(restartConfirmation, false);
            SetActive(helpPanel, true);
        }

        private void ConfirmRestart()
        {
            if (!UiShellRuntimeGateway.TryRestartCurrentMission())
            {
                SetText(restartStatusText, "RESTART IS NOT AVAILABLE FOR THIS MATCH STATE.");
                return;
            }

            UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);
        }

        private void RefreshLiveText()
        {
            if (UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel campaign) &&
                campaign.IsValid && !string.IsNullOrWhiteSpace(campaign.SelectedMission.DisplayName))
            {
                SetText(missionText, campaign.SelectedMission.DisplayName);
            }

            if (UiShellRuntimeGateway.TryReadMatchHudStatusSurfaces(out UiMatchHudStatusSurfacesModel status))
            {
                string elapsed = string.IsNullOrWhiteSpace(status.ElapsedText) ? "14:32" : status.ElapsedText;
                SetText(currentTimeText, "CURRENT TIME  " + elapsed);
                string objective = FirstNonEmpty(
                    status.Objective0.Text,
                    status.Objective1.Text,
                    status.Objective2.Text,
                    "Capture the Enemy HQ");
                SetText(objectiveText, objective);
            }

            if (UiShellRuntimeGateway.TryReadMatchHudHeader(out UiMatchHudHeaderModel header))
                SetText(civilianRiskText, NormalizeRisk(header.CivilianRiskText));

            if (UiShellRuntimeGateway.TryReadMatchHudSquadTray(out UiMatchHudSquadTrayModel tray))
            {
                int alive = 0;
                int total = 0;
                for (int index = 0; index < UiMatchHudSquadTrayModel.MaxCards; index++)
                {
                    UiMatchHudSquadTrayCardModel card = tray.GetCard(index);
                    if (!card.Visible)
                        continue;
                    total++;
                    if (card.Health01 > .001f)
                        alive++;
                }
                if (total > 0)
                    SetText(squadsAliveText, $"{alive} / {total}");
            }
        }

        private static string NormalizeRisk(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "MEDIUM";
            return value.Trim().ToUpperInvariant() switch
            {
                "MED" => "MEDIUM",
                "HI" => "HIGH",
                _ => value.Trim().ToUpperInvariant()
            };
        }

        private static string FirstNonEmpty(params string[] values)
        {
            for (int index = 0; index < values.Length; index++)
                if (!string.IsNullOrWhiteSpace(values[index]))
                    return values[index];
            return string.Empty;
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null && target.text != value)
                target.text = value;
        }
    }
}
