using System.Linq;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;
namespace Game.UI.Runtime
{
    public sealed partial class UIShellContentView
    {
        private System.Collections.IEnumerator RestoreInnerMenuAfterSettings()
        {
            // Settings shares the fullscreen inner-screen mount. Restore that screen once
            // the shell's hide transition has completed, without pushing another route.
            yield return null;
            while(UiShellRuntimeGateway.TryReadShellState(out var transitioning)&&transitioning.IsTransitionRunning)yield return null;
            if(UiShellRuntimeGateway.TryReadShellState(out var state)&&state.CurrentMode==UiShellMode.MainMenu&&state.ActiveRoute!=UIRoute.MainMenu&&state.ActiveRoute!=UIRoute.Settings)
                InstallMenuRouteBody(state.ActiveRoute);
        }
        public bool HasBackOverlay=>_settingsPopupInstance!=null||_pauseMenuPopupInstance!=null||_fullMapPopupInstance!=null||_missionFieldGuideInstance!=null||_threatAlertPopupInstance!=null||_supportPopupInstance!=null||_buildDrawerPopupInstance!=null||_resourceExchangeShellBinding.IsOpen;
        public bool SubmitScreenBack()
        {
            foreach(var id in new[]{UIShellRegionId.PopupLayer,UIShellRegionId.HeaderRegion,UIShellRegionId.MiddleRegion,UIShellRegionId.FooterRegion})
            {
                if(!TryGetRegionContentRoot(id,out var root))continue;
                var back=root.GetComponentsInChildren<UIShellRouteButtonView>().FirstOrDefault(v=>v.Intent==UiShellRouteIntent.BackMenuRoute&&v.GetComponent<Button>().IsInteractable()&&v.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha>.05f));
                if(back!=null){back.SubmitRouteRequest();return true;}
            }
            return false;
        }
        public bool HandleOverlayBack()
        {
            if(!HasBackOverlay)return _mainMenuPlayUi?.TryCloseMatchHudAssistantForBack()==true;
            // Cancel a nested confirmation before closing the popup below it.
            if(TryGetRegionContentRoot(UIShellRegionId.PopupLayer,out var root))
            {
                var cancel=root.GetComponentsInChildren<Button>().LastOrDefault(b=>(b.name is "CancelButton" or "Cancel")&&b.IsInteractable()&&b.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha>.05f));
                if(cancel!=null){cancel.onClick.Invoke();return true;}
            }
            if(_settingsPopupInstance!=null){CloseSettingsPopup();return true;}
            if(_pauseMenuPopupInstance!=null){UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);return true;}
            if(_fullMapPopupInstance!=null){CloseFullMapPopup();return true;}
            if(_missionFieldGuideInstance!=null){UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseGuide);return true;}
            if(_threatAlertPopupInstance!=null){UiShellRuntimeGateway.TryRequestMissionDefenseAction(UiMissionDefenseAction.CloseWarning);return true;}
            if(_supportPopupInstance!=null){UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseSupport);return true;}
            if(_buildDrawerPopupInstance!=null){CloseBuildDrawerPopup();UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.CloseBuildDrawer);return true;}
            if(root!=null&&root.GetComponentInChildren<ResourceExchangePopupView>()!=null){RequestCloseResourceExchangePopup();return true;}
            // Non-dismissable results/required narrative decisions keep their explicit actions.
            return true;
        }
    }
}
