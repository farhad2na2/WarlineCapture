using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        UiAircraftServiceModel presentedAircraftService;
        int aircraftServiceFrame;
        void RefreshAircraftServiceContext()
        {
            presentedAircraftService=null;aircraftServiceFrame=0;
            if(currentOrderText==null||!HasWheelSelection||!currentOrderText.gameObject.activeInHierarchy||
                !UiShellRuntimeGateway.TryReadSelectedAircraftService(out var model)||model==null||!model.Visible)return;
            // Reuses the existing selected-unit order text, typography and layout.
            // It is normal player information, not a hidden ARIA telemetry channel.
            SetText(currentOrderText,model.Status);
            presentedAircraftService=model;aircraftServiceFrame=Time.frameCount;
        }
        public bool TryReadPresentedAircraftService(int frame,out UiAircraftServiceModel model)
        {
            model=presentedAircraftService;
            return frame==aircraftServiceFrame&&model!=null&&model.Visible&&HasWheelSelection&&
                currentOrderText!=null&&currentOrderText.gameObject.activeInHierarchy;
        }
    }
}
