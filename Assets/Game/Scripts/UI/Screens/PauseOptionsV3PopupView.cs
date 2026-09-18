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
            UiShellRuntimeGateway.Localization.LocaleChanged += RefreshLiveText;
        }

        private void OnDisable()
        {
            UiShellRuntimeGateway.Localization.LocaleChanged -= RefreshLiveText;
            Unbind();
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
            if(UiShellRuntimeGateway.TryReadSkirmish(out _))
            {
                Object.FindAnyObjectByType<SkirmishMatchView>()?.Confirm(UiSkirmishAction.Restart);
                return;
            }
            SetActive(helpPanel, false);
            SetActive(restartConfirmation, true);
            SetText(restartStatusText, "RESTART THE CURRENT MISSION FROM THE BEGINNING?");
        }

        private void ShowHelp()
        {
            if(UiShellRuntimeGateway.TryReadMissionDefense(out _) && UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide)) return;
            if (helpPanel != null)
            {
                SetText(helpPanel.transform.Find("SELECTHelpRow/Body")?.GetComponent<TMP_Text>(), "Tap a squad card to select its units.");
                SetText(helpPanel.transform.Find("COMMAND WHEELHelpRow/Body")?.GetComponent<TMP_Text>(), "Tap Commands below the selected unit portrait.");
            }
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

        private bool showingSkirmish;
        private string campaignExitLabel;
        private Transform campaignStatus;
        private GameObject skirmishStatus;
        private TMP_Text skirmishObjective, skirmishPlayerBase, skirmishEnemyBase, skirmishInfantry, skirmishSaveNote;
        private TMP_Text skirmishPlayerHealth, skirmishEnemyHealth, skirmishInfantryCount;

        private void ShowSkirmishStatus(UiSkirmishModel model)
        {
            if (campaignStatus == null && objectiveText != null)
                campaignStatus = objectiveText.transform.parent.parent;
            if (campaignStatus == null) return;
            if (skirmishStatus == null)
            {
                skirmishStatus = new GameObject("SkirmishStatusColumn", typeof(RectTransform), typeof(V3GradientGraphic));
                var rect = (RectTransform)skirmishStatus.transform;
                var source = (RectTransform)campaignStatus;
                rect.SetParent(source.parent, false);
                rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax;
                rect.pivot = source.pivot; rect.sizeDelta = source.sizeDelta; rect.anchoredPosition = source.anchoredPosition;
                skirmishStatus.GetComponent<V3GradientGraphic>().Configure(new Color(.10f,.14f,.15f), new Color(.025f,.07f,.08f), new Color(.4f,.5f,.52f), 2);
                var layout = skirmishStatus.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(14,14,14,14); layout.spacing = 12;
                layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
                skirmishObjective = StatusLabel(120, 19);
                skirmishPlayerBase = StatusPair(70, 22, out skirmishPlayerHealth);
                skirmishEnemyBase = StatusPair(70, 22, out skirmishEnemyHealth);
                skirmishInfantry = StatusPair(70, 20, out skirmishInfantryCount);
                skirmishSaveNote = StatusLabel(78, 17);
            }
            campaignStatus.gameObject.SetActive(false);
            skirmishStatus.SetActive(true);
            SetText(skirmishObjective, model.Objective);
            SetBaseStatus(skirmishPlayerBase, skirmishPlayerHealth, model.PlayerBase);
            SetBaseStatus(skirmishEnemyBase, skirmishEnemyHealth, model.EnemyBase);
            SetText(skirmishInfantry, UiShellRuntimeGateway.Localization.Get("ui.skirmish.infantry", "Infantry"));
            SetText(skirmishInfantryCount, model.Infantry);
            SetText(skirmishSaveNote, UiShellRuntimeGateway.Localization.Get("ui.skirmish.no_resume", "Leaving ends this match. Your setup is saved for a new match."));
        }

        private static void SetBaseStatus(TMP_Text heading, TMP_Text value, string source)
        {
            var lines = (source ?? string.Empty).Split('\n');
            SetText(heading, lines[0]);
            SetText(value, lines.Length > 1 ? lines[1] : string.Empty);
        }

        private TMP_Text StatusPair(float height, float size, out TMP_Text value)
        {
            var heading = StatusLabel(height, size);
            heading.alignment = TextAlignmentOptions.Top;
            var go = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(heading.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(0, height * .5f); rect.anchoredPosition = Vector2.zero;
            value = go.GetComponent<TMP_Text>(); value.font = objectiveText.font; value.fontSize = size;
            value.color = Color.white; value.alignment = TextAlignmentOptions.Center; value.raycastTarget = false;
            return heading;
        }

        private TMP_Text StatusLabel(float height, float size)
        {
            var go = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(skirmishStatus.transform, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;
            var label = go.GetComponent<TMP_Text>();
            label.font = objectiveText.font; label.fontSize = size; label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center; label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false;
            return label;
        }

        private void RefreshLiveText()
        {
            if(UiShellRuntimeGateway.TryReadSkirmish(out var skirmish))
            {
                if(!showingSkirmish && exitButton!=null)campaignExitLabel=exitButton.GetComponentInChildren<TMP_Text>()?.text;
                showingSkirmish=true;
                UiLocalizedText.Set(missionText,UiShellRuntimeGateway.Localization.Get("ui.skirmish.base_assault","BASE ASSAULT"));
                SetText(currentTimeText,skirmish.Clock);
                SetText(objectiveText,skirmish.Objective);
                ShowSkirmishStatus(skirmish);
                if(exitButton!=null)UiLocalizedText.Set(exitButton.GetComponentInChildren<TMP_Text>(),UiShellRuntimeGateway.Localization.Get("ui.skirmish.surrender","SURRENDER"));
                if(restartButton!=null)SetText(restartButton.GetComponentInChildren<TMP_Text>(),"RESTART MATCH");
                if(civilianRiskText!=null)civilianRiskText.transform.parent.gameObject.SetActive(false);
                return;
            }
            if(showingSkirmish)
            {
                if (campaignStatus != null) campaignStatus.gameObject.SetActive(true);
                if (skirmishStatus != null) skirmishStatus.SetActive(false);
                if(civilianRiskText!=null)civilianRiskText.transform.parent.gameObject.SetActive(true);
                if(exitButton!=null)UiLocalizedText.Set(exitButton.GetComponentInChildren<TMP_Text>(),campaignExitLabel);
                if(restartButton!=null)SetText(restartButton.GetComponentInChildren<TMP_Text>(),"RESTART MISSION");
                showingSkirmish=false;
            }
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
            UiLocalizedText.Set(target, value);
        }
    }
}
