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
        [SerializeField] private GameObject openingHint;
        [SerializeField] private GameObject actions;
        [SerializeField] private MissionHudTouchLayoutView touchLayout;
        [SerializeField] private Button guideButton,warningButton,skipButton,supportButton;
        [SerializeField] private Button returnCameraButton;
        [SerializeField] private V3LocalizedTextBindingView status,supportLabel;
        [SerializeField] private Image supportIcon;
        [SerializeField] private Sprite radarIcon;
        private Sprite originalIcon;
        private string originalLabel,lastLocale;
        private bool showingDefense;
        private int lastCharges=-1,lastCooldown=-1;
        public bool IsCameraTourControl(Selectable control)=>control==skipCameraTourButton;
        private void Awake()
        {originalIcon=supportIcon!=null ? supportIcon.sprite : null; originalLabel=supportLabel!=null ? supportLabel.EnglishFallback : "";}
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
                if(openingHint!=null) openingHint.SetActive(touring && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) && restrictions.MissionId=="saga.ch01.m03.radar_warning");
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
            bool touring=UiShellRuntimeGateway.TryReadMissionCameraTour() && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restriction) && restriction.MissionId=="saga.ch01.m03.radar_warning";
            if(touchLayout!=null) touchLayout.Apply(active || touring);
            if(showingDefense!=active || lastLocale!=UiShellRuntimeGateway.Localization.CurrentLocaleCode)
            {
                if(supportLabel!=null) supportLabel.SetLocalizedValue(active ? UiShellRuntimeGateway.Localization.Get("mission.m03.ping.label","Radar Ping") : UiShellRuntimeGateway.Localization.Get("",originalLabel));
                if(supportIcon!=null) supportIcon.sprite=active ? radarIcon : originalIcon;
                showingDefense=active;
            }
            if(returnCameraButton!=null) returnCameraButton.gameObject.SetActive(active && model.CanReturnCamera);
            if(status!=null) status.gameObject.SetActive(active);
            if(!active) {lastLocale=UiShellRuntimeGateway.Localization.CurrentLocaleCode; return;}
            if(supportButton!=null) supportButton.interactable=model.CanPing;
            if(warningButton!=null) warningButton.interactable=model.HasWarning;
            if(skipButton!=null) skipButton.gameObject.SetActive(model.GuidanceId is 45004 or 45007 or 45008 or 45009);
            if(status!=null && (lastCharges!=model.Charges || lastCooldown!=model.CooldownSeconds || lastLocale!=UiShellRuntimeGateway.Localization.CurrentLocaleCode))
            {
                string pingStatus=model.CooldownSeconds>0 ? (model.CooldownSeconds/60)+":"+(model.CooldownSeconds%60).ToString("00") : model.Charges.ToString();
                status.SetLocalizedValue(pingStatus);lastCharges=model.Charges;lastCooldown=model.CooldownSeconds;
            }
            lastLocale=UiShellRuntimeGateway.Localization.CurrentLocaleCode;
        }
        private void OpenGuide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void OpenWarning()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenWarning);
        private void Skip()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipOptional);
        private void SkipCameraTour()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipCameraTour);
        private void ReturnCamera()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ReturnCamera);
    }
}
