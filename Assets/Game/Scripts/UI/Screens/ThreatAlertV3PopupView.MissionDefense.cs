using Game.UI.Contracts;
using TMPro;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class ThreatAlertV3PopupView
    {
        [SerializeField] private GameObject authoredExampleBody;
        [SerializeField] private TMP_Text missionWarningDetails;
        [SerializeField] private TMP_Text missionWarningTitle;
        [SerializeField] private TMP_Text jumpLabel;
        [SerializeField] private V3LocalizedTextBindingView detailsLocalization;
        [SerializeField] private UnityEngine.UI.Button missionGuideButton;
        [SerializeField] private RectTransform missionJumpIcon;
        private GameObject _boundLegacyThreatPanel;
        private float nextMissionRefresh;
        private string lastWarningText,lastWarningLocale;

        public void BindLegacyThreatPanel(GameObject panel)
        { RestoreLegacyThreatBanner(); _boundLegacyThreatPanel=panel; if(isActiveAndEnabled) SuppressLegacyThreatBanner(); }

        private void Update()
        {
            if(Time.unscaledTime<nextMissionRefresh) return;
            nextMissionRefresh=Time.unscaledTime+0.2f;
            if(!UiShellRuntimeGateway.TryReadMissionDefense(out var model)) return;
            if(missionGuideButton!=null) missionGuideButton.gameObject.SetActive(true);
            if(jumpToThreatButton!=null) ((RectTransform)jumpToThreatButton.transform).sizeDelta=new Vector2(450,84);
            if(missionJumpIcon!=null) missionJumpIcon.anchoredPosition=new Vector2(20,-14);
            if(jumpLabel!=null)
            {jumpLabel.rectTransform.anchoredPosition=new Vector2(85,-5); jumpLabel.rectTransform.sizeDelta=new Vector2(345,72); jumpLabel.fontSizeMax=27;}
            if(authoredExampleBody!=null) authoredExampleBody.SetActive(false);
            if(missionWarningDetails!=null) missionWarningDetails.gameObject.SetActive(true);
            string text=model.HasWarning ? model.WarningText : UiShellRuntimeGateway.Localization.Get("mission.m03.warning.resolved","Convoy element stopped");
            if(text!=lastWarningText || lastWarningLocale!=UiShellRuntimeGateway.Localization.CurrentLocaleCode)
            {
                if(detailsLocalization!=null) detailsLocalization.SetLocalizedValue(text);
                else if(missionWarningDetails!=null) missionWarningDetails.text=text;
                lastWarningText=text; lastWarningLocale=UiShellRuntimeGateway.Localization.CurrentLocaleCode;
            }
            if(jumpToThreatButton!=null) jumpToThreatButton.interactable=model.CanFocus;
        }
        private void OpenWarningGuide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void JumpToReport()
        {
            if(UiShellRuntimeGateway.TryReadMissionDefense(out _))
            {
                if(UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.FocusWarning)) Close();
                return;
            }
            ShowRoutePreview();
        }
    }
}
