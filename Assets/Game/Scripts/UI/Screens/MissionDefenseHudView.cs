using Game.UI.Contracts;
using Game.Tactical.Contracts;
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
        private uint lastScanResult;
        private BattleHudRuntimeFeedbackView feedback;
        [SerializeField] private GameObject openingHint;
        [SerializeField] private GameObject actions;
        [SerializeField] private MissionHudTouchLayoutView touchLayout;
        [SerializeField] private Button guideButton,warningButton,skipButton,supportButton;
        public Button GuideButton => guideButton;
        [SerializeField] private Button returnCameraButton;
        [SerializeField] private V3LocalizedTextBindingView status,supportLabel;
        [SerializeField] private Image supportIcon;
        [SerializeField] private Sprite radarIcon;
        public bool IsCameraTourControl(Selectable control)=>control==skipCameraTourButton;
        private void OnEnable()
        {
            cameraPreferencesApplied=false;
            if(returnCameraButton!=null) returnCameraButton.gameObject.SetActive(false);
            if(skipCameraTourButton!=null) skipCameraTourButton.gameObject.SetActive(false);
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
                if(openingHint!=null)
                {
                    bool visible=touring && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions) && restrictions.MissionId is "saga.ch01.m03.radar_warning" or "saga.ch01.m04.airlift" or "saga.ch01.m05.breach_assault";
                    openingHint.SetActive(visible);
                    if(visible && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out restrictions))
                        openingHint.GetComponent<V3LocalizedTextBindingView>()?.SetLocalizedValue(UiShellRuntimeGateway.Localization.Get((restrictions.MissionId=="saga.ch01.m05.breach_assault" ? "mission.m05" : restrictions.MissionId=="saga.ch01.m04.airlift" ? "mission.m04" : "mission.m03")+(restrictions.OpeningCinematic ? ".camera.opening" : ".camera.victory")));
                }
                if(skipCameraTourButton.gameObject.activeSelf) skipCameraTourButton.gameObject.SetActive(false);
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
            bool breach=(UiShellRuntimeGateway.IsBreachGuideContext() || UiShellRuntimeGateway.IsGridlockGuideContext()) && !UiShellRuntimeGateway.TryReadMissionCameraTour();
            if(actions!=null) actions.SetActive(active || breach);
            if(warningButton!=null) warningButton.gameObject.SetActive(active);
            if(skipButton!=null && !active) skipButton.gameObject.SetActive(false);
            bool touring=UiShellRuntimeGateway.TryReadMissionCameraTour() && UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restriction) && restriction.MissionId=="saga.ch01.m03.radar_warning";
            if(touchLayout!=null) touchLayout.Apply(active || touring || UiShellRuntimeGateway.TryReadMissionExtraction(out _));
            if(returnCameraButton!=null) returnCameraButton.gameObject.SetActive(false);
            if(status!=null) status.gameObject.SetActive(false);
            if(!active) {lastScanResult=0; return;}
            if(warningButton!=null) warningButton.interactable=model.HasWarning;
            if(warningButton!=null && model.ScanResultVersion!=lastScanResult && !string.IsNullOrEmpty(model.ScanFeedback))
            {
                lastScanResult=model.ScanResultVersion;
                if(feedback==null) feedback=transform.root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
                feedback?.ApplyTransientCommandFeedback(MatchHudCommandFeedbackModel.ShowTransient(model.ScanFeedback,CommandFeedbackSeverity.Ready,8f),Time.unscaledTime);
            }
            if(skipButton!=null) skipButton.gameObject.SetActive(!model.RequiresHoldResume && model.GuidanceId is 45004 or 45007 or 45008 or 45009);
        }
        private void OpenGuide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void OpenWarning()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenWarning);
        private void Skip()
        {
            var placement=transform.root.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
            if(placement!=null && placement.HasPendingPlacement) placement.CancelButton.onClick.Invoke();
            UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipOptional);
        }
        private void SkipCameraTour()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.SkipCameraTour);
        private void ReturnCamera()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.ReturnCamera);
    }
}
