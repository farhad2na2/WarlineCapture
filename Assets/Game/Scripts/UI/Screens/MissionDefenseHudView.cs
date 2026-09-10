using Game.UI.Contracts;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed class MissionDefenseHudView : MonoBehaviour
    {
        private static readonly ProfilerMarker RefreshMarker = new("MissionDefenseHudView.Refresh");
        [SerializeField] private Button skipCameraTourButton;
        private bool cameraPreferencesApplied;
        [SerializeField] private GameObject actions;
        [SerializeField] private Button guideButton,warningButton,skipButton,supportButton;
        [SerializeField] private Button returnCameraButton;
        [SerializeField] private V3LocalizedTextBindingView status,supportLabel;
        [SerializeField] private Image supportIcon;
        [SerializeField] private Sprite radarIcon;
        private Sprite originalIcon;
        private string originalLabel,lastStatus,lastLocale;
        private bool showingDefense;
        private Color warningDefaultColor;
        private bool warningAttention;
        public bool IsCameraTourControl(Selectable control)=>control==skipCameraTourButton;
        private void Awake()
        {if(warningButton!=null && warningButton.targetGraphic!=null) warningDefaultColor=warningButton.targetGraphic.color; originalIcon=supportIcon!=null ? supportIcon.sprite : null; originalLabel=supportLabel!=null ? supportLabel.EnglishFallback : "";}
        private void OnEnable()
        {
            cameraPreferencesApplied=false;
            if(skipCameraTourButton!=null) skipCameraTourButton.onClick.AddListener(SkipCameraTour);
            UiShellRuntimeGateway.Localization.LocaleChanged += Refresh;
            if(guideButton!=null) guideButton.onClick.AddListener(OpenGuide);
            if(returnCameraButton!=null) returnCameraButton.onClick.AddListener(ReturnCamera);
            if(warningButton!=null) warningButton.onClick.AddListener(OpenWarning);
            if(skipButton!=null) skipButton.onClick.AddListener(Skip); Refresh();
        }
        private void OnDisable()
        {
            if(skipCameraTourButton!=null) skipCameraTourButton.onClick.RemoveListener(SkipCameraTour);
            UiShellRuntimeGateway.Localization.LocaleChanged -= Refresh;
            if(guideButton!=null) guideButton.onClick.RemoveListener(OpenGuide);
            if(returnCameraButton!=null) returnCameraButton.onClick.RemoveListener(ReturnCamera);
            if(warningButton!=null) warningButton.onClick.RemoveListener(OpenWarning);
            if(skipButton!=null) skipButton.onClick.RemoveListener(Skip);
        }

        public void RefreshCameraTour()
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
        public void RefreshPresentation() => Refresh();
        private void Refresh()
        {
            using var marker = RefreshMarker.Auto();
            bool active=UiShellRuntimeGateway.TryReadMissionDefense(out var model);
            if(actions!=null) actions.SetActive(active);
            if(showingDefense!=active || lastLocale!=UiShellRuntimeGateway.Localization.CurrentLocaleCode)
            {
                if(supportLabel!=null) supportLabel.SetLocalizedValue(active ? UiShellRuntimeGateway.Localization.Get("mission.m03.ping.label","Radar Ping") : UiShellRuntimeGateway.Localization.Get("",originalLabel));
                if(supportIcon!=null) supportIcon.sprite=active ? radarIcon : originalIcon;
                showingDefense=active;
            }
            bool attention=active && model.WarningNeedsAttention;
            if(warningAttention!=attention)
            {
                warningAttention=attention;
                if(warningButton!=null && warningButton.targetGraphic!=null)
                    warningButton.targetGraphic.color=attention ? new Color(.62f,.32f,.04f,1f) : warningDefaultColor;
            }
            if(!active) {lastLocale=UiShellRuntimeGateway.Localization.CurrentLocaleCode; return;}
            if(supportButton!=null) supportButton.interactable=model.CanPing;
            if(warningButton!=null) warningButton.interactable=model.HasWarning;
            if(returnCameraButton!=null) returnCameraButton.gameObject.SetActive(model.CanReturnCamera);
            if(skipButton!=null) skipButton.gameObject.SetActive(model.GuidanceId is 45004 or 45007 or 45008 or 45009);
            if(lastStatus!=model.PingText || lastLocale!=UiShellRuntimeGateway.Localization.CurrentLocaleCode)
            {if(status!=null) status.SetLocalizedValue(model.PingText); lastStatus=model.PingText;}
            lastLocale=UiShellRuntimeGateway.Localization.CurrentLocaleCode;
        }
        private void OpenGuide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void OpenWarning()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenWarning);
        private void Skip()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipOptional);
        private void SkipCameraTour()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipCameraTour);
        private void ReturnCamera()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ReturnCamera);
    }
}
