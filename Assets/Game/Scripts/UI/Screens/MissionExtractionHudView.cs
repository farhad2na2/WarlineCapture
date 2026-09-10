using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed class MissionExtractionHudView:MonoBehaviour
    {
        [SerializeField]private GameObject actions;
        [SerializeField]private Button guide,team,landing,departure;
        [SerializeField]private V3LocalizedTextBindingView status;
        private string lastStatus;
        private void OnEnable(){guide.onClick.AddListener(Guide);team.onClick.AddListener(Team);landing.onClick.AddListener(Landing);departure.onClick.AddListener(Departure);UiShellRuntimeGateway.Localization.LocaleChanged+=RefreshLocale;RefreshPresentation();}
        private void OnDisable(){guide.onClick.RemoveListener(Guide);team.onClick.RemoveListener(Team);landing.onClick.RemoveListener(Landing);departure.onClick.RemoveListener(Departure);UiShellRuntimeGateway.Localization.LocaleChanged-=RefreshLocale;}
        private void RefreshLocale(){lastStatus=null;RefreshPresentation();}

        public void RefreshPresentation()
        {
            bool active=UiShellRuntimeGateway.TryReadMissionExtraction(out var model);if(actions.activeSelf!=active)actions.SetActive(active);if(!active)return;
            string text=UiShellRuntimeGateway.Localization.Format("mission.m04.hud.status","Aboard {0}/4 · APC leg {1}/4 · Clear {2}/20s · {3}s left",model.Aboard,model.CarrierLeg,model.SecureSeconds,model.RemainingSeconds);
            if(model.Contested)text+="\n"+UiShellRuntimeGateway.Localization.Get("mission.m04.hud.contested");else if(model.Cleared)text+="\n"+UiShellRuntimeGateway.Localization.Get("mission.m04.hud.cleared");
            if(text!=lastStatus){status.SetLocalizedValue(text);lastStatus=text;}
        }
        private void Guide()=>UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.OpenGuide);
        private void Team()=>UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusTeam);
        private void Landing()=>UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusLanding);
        private void Departure()=>UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.FocusDeparture);
    }
}
