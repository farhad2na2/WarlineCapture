using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed class MissionExtractionHudView:MonoBehaviour
    {
        [SerializeField]private GameObject actions;
        [SerializeField]private Button guide,team,landing,departure;
        [SerializeField]private V3LocalizedTextBindingView status,aboard,carrier,secure,remaining;
        private string lastStatus;
        private int lastAboard=-1,lastCarrier=-1,lastSecure=-1,lastRemaining=-1;
        private bool lastContested,lastCleared;
        private void OnEnable(){guide.onClick.AddListener(Guide);team.onClick.AddListener(Team);landing.onClick.AddListener(Landing);departure.onClick.AddListener(Departure);UiShellRuntimeGateway.Localization.LocaleChanged+=RefreshLocale;RefreshPresentation();}
        private void OnDisable(){guide.onClick.RemoveListener(Guide);team.onClick.RemoveListener(Team);landing.onClick.RemoveListener(Landing);departure.onClick.RemoveListener(Departure);UiShellRuntimeGateway.Localization.LocaleChanged-=RefreshLocale;}
        private void RefreshLocale(){lastStatus=null;RefreshPresentation();}

        public void RefreshPresentation()
        {
            bool active=UiShellRuntimeGateway.TryReadMissionExtraction(out var model);if(actions.activeSelf!=active)actions.SetActive(active);if(!active)return;
            if(lastStatus!=null && lastAboard==model.Aboard && lastCarrier==model.CarrierLeg && lastSecure==model.SecureSeconds && lastRemaining==model.RemainingSeconds && lastContested==model.Contested && lastCleared==model.Cleared)return;
            lastAboard=model.Aboard;lastCarrier=model.CarrierLeg;lastSecure=model.SecureSeconds;lastRemaining=model.RemainingSeconds;lastContested=model.Contested;lastCleared=model.Cleared;
            aboard.SetLocalizedValue(model.Aboard+" / 4");carrier.SetLocalizedValue(model.CarrierLeg+" / 4");secure.SetLocalizedValue(model.SecureSeconds+" / 20");
            int seconds=Mathf.Max(0,model.RemainingSeconds);
            remaining.SetLocalizedValue((seconds/60).ToString("00")+":"+(seconds%60).ToString("00"));
            remaining.GetComponent<TMPro.TMP_Text>().color=seconds<=60?new Color32(255,112,60,255):new Color32(255,196,67,255);
            string key=model.Contested?"mission.m04.hud.contested":model.Cleared?"mission.m04.hud.cleared":"mission.m04.hud.route";
            lastStatus=UiShellRuntimeGateway.Localization.Get(key);status.SetLocalizedValue(lastStatus);
        }
        private void Guide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void Team()=>UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusTeam);
        private void Landing()=>UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusLanding);
        private void Departure()=>UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusDeparture);
    }
}
