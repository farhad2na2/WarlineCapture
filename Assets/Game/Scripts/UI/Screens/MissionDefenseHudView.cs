using Game.Configs;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class MissionDefenseHudView : MonoBehaviour
    {
        [SerializeField] private Button skipCameraTourButton;
        private bool cameraPreferencesApplied;
        [SerializeField] private GameObject actions;
        [SerializeField] private Button guideButton,warningButton,skipButton,supportButton;
        [SerializeField] private Button returnCameraButton;
        [SerializeField] private V3LocalizedTextBinding status,supportLabel;
        [SerializeField] private Image supportIcon;
        [SerializeField] private Sprite radarIcon;
        private Sprite originalIcon;
        private string originalLabel,lastStatus,lastLocale;
        private bool showingDefense;
        private Color warningDefaultColor;
        private bool warningAttention;
        private float nextRefresh;
        public bool IsCameraTourControl(Selectable control)=>control==skipCameraTourButton;
        private void Awake()
        {if(warningButton!=null && warningButton.image!=null) warningDefaultColor=warningButton.image.color; originalIcon=supportIcon!=null ? supportIcon.sprite : null; originalLabel=supportLabel!=null ? supportLabel.EnglishFallback : "";}
        private void OnEnable()
        {
            cameraPreferencesApplied=false;
            if(skipCameraTourButton!=null) skipCameraTourButton.onClick.AddListener(SkipCameraTour);
            GameLocalization.LocaleChanged += Refresh;
            if(guideButton!=null) guideButton.onClick.AddListener(OpenGuide);
            if(returnCameraButton!=null) returnCameraButton.onClick.AddListener(ReturnCamera);
            if(warningButton!=null) warningButton.onClick.AddListener(OpenWarning);
            if(skipButton!=null) skipButton.onClick.AddListener(Skip); nextRefresh=0; Refresh();
        }
        private void OnDisable()
        {
            if(skipCameraTourButton!=null) skipCameraTourButton.onClick.RemoveListener(SkipCameraTour);
            GameLocalization.LocaleChanged -= Refresh;
            if(guideButton!=null) guideButton.onClick.RemoveListener(OpenGuide);
            if(returnCameraButton!=null) returnCameraButton.onClick.RemoveListener(ReturnCamera);
            if(warningButton!=null) warningButton.onClick.RemoveListener(OpenWarning);
            if(skipButton!=null) skipButton.onClick.RemoveListener(Skip);
        }
        private void Update()
        {RefreshCameraTour(); if(Time.unscaledTime>=nextRefresh) {nextRefresh=Time.unscaledTime+.25f; Refresh();}}
        private void RefreshCameraTour()
        {
            if(skipCameraTourButton!=null)
            {
                bool touring=UiShellRuntimeGateway.TryReadMissionCameraTour();
                if(skipCameraTourButton.gameObject.activeSelf!=touring) skipCameraTourButton.gameObject.SetActive(touring);
                if(touring && !cameraPreferencesApplied)
                {
                    cameraPreferencesApplied=true;
                    if(SettingsService.LoadReducedMotionPreference()) UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ReduceCameraMotion);
                }
            }
        }
        private void Refresh()
        {
            bool active=UiShellRuntimeGateway.TryReadMissionDefense(out var model);
            if(actions!=null) actions.SetActive(active);
            if(showingDefense!=active || lastLocale!=GameLocalization.CurrentLocaleCode)
            {
                if(supportLabel!=null) supportLabel.SetLocalizedValue(active ? GameText.Get("mission.m03.ping.label","Radar Ping") : GameText.Get("",originalLabel));
                if(supportIcon!=null) supportIcon.sprite=active ? radarIcon : originalIcon;
                showingDefense=active;
            }
            bool attention=active && model.WarningNeedsAttention;
            if(warningAttention!=attention)
            {
                warningAttention=attention;
                if(warningButton!=null && warningButton.image!=null)
                    warningButton.image.color=attention ? new Color(.62f,.32f,.04f,1f) : warningDefaultColor;
            }
            if(!active) {lastLocale=GameLocalization.CurrentLocaleCode; return;}
            if(supportButton!=null) supportButton.interactable=model.CanPing;
            if(warningButton!=null) warningButton.interactable=model.HasWarning;
            if(returnCameraButton!=null) returnCameraButton.gameObject.SetActive(model.CanReturnCamera);
            if(skipButton!=null) skipButton.gameObject.SetActive(model.GuidanceId is 45004 or 45007 or 45008 or 45009);
            if(lastStatus!=model.PingText || lastLocale!=GameLocalization.CurrentLocaleCode)
            {if(status!=null) status.SetLocalizedValue(model.PingText); lastStatus=model.PingText;}
            lastLocale=GameLocalization.CurrentLocaleCode;
        }
        private void OpenGuide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void OpenWarning()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenWarning);
        private void Skip()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipOptional);
        private void SkipCameraTour()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipCameraTour);
        private void ReturnCamera()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ReturnCamera);
    }
}
