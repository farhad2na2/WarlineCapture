using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {
        UiPublicBoardingModel presentedBoarding;
        int presentedBoardingFrame;
        void RefreshPublicBoardingHud()
        {
            presentedBoarding=null;presentedBoardingFrame=0;
            if(objectiveModel==null||!objectiveModel.Available||playerHealth==null||!playerHealth.gameObject.activeInHierarchy||
                !UiShellRuntimeGateway.TryReadPublicBoarding(out var model)||model==null||!model.Visible||model.Attempt!=objectiveModel.AttemptId)return;
            UiLocalizedText.Set(playerHealth,playerHealth.text+"\n"+model.Status);
            presentedBoarding=model;presentedBoardingFrame=Time.frameCount;
        }
        public bool TryReadPresentedBoarding(int frame,out UiPublicBoardingModel model)
        {model=presentedBoarding;return frame==presentedBoardingFrame&&model!=null&&playerHealth!=null&&playerHealth.gameObject.activeInHierarchy;}
        public bool PresentedBoardingFootprintFits(int frame,string publicId,Vector2[] points)
        {
            if(points==null||points.Length==0||!TryReadPublicObjectiveForAria(frame,out var shown)||!shown.Presented||objectiveModel==null||!objectiveModel.Available)return false;
            int matches=0;bool fit=false;
            for(int i=0;i<shown.Goal.NodeCount;i++)
            {var node=shown.Goal.Node(i);if(node.PublicId!=publicId||!node.PositionKnown)continue;matches++;fit=AllInsidePublicGoal(objectiveModel,node,points);}
            return matches==1&&fit;
        }
    }
}
