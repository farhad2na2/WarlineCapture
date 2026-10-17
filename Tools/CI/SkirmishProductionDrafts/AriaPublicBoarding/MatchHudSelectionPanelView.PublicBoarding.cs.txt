using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MatchHudSelectionPanelView
    {
        UiPublicBoardingModel shownPublicBoarding;
        int shownPublicBoardingFrame;
        void RefreshPublicBoardingContext()
        {
            shownPublicBoarding=null;shownPublicBoardingFrame=0;
            if(currentOrderText==null||!HasWheelSelection||!currentOrderText.gameObject.activeInHierarchy||
                !UiShellRuntimeGateway.TryReadPublicBoarding(out var model)||model==null||!model.Visible)return;
            SetText(currentOrderText,model.Status);shownPublicBoarding=model;shownPublicBoardingFrame=Time.frameCount;
        }
        public bool TryReadPresentedPublicBoarding(int frame,out UiPublicBoardingModel model)
        {
            model=shownPublicBoarding;
            return frame==shownPublicBoardingFrame&&model!=null&&model.Visible&&HasWheelSelection&&currentOrderText!=null&&currentOrderText.gameObject.activeInHierarchy;
        }
    }
}
