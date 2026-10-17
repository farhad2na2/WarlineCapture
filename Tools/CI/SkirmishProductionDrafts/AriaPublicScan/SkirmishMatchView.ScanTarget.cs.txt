using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class SkirmishMatchView
    {
        public bool PresentedPublicScanCenterFits(int frame,string publicId,Vector2 center)
        {
            if(!TryReadPublicObjectiveForAria(frame,out var shown)||!shown.Presented||objectiveModel==null||!objectiveModel.Available||
                float.IsNaN(center.x)||float.IsInfinity(center.x)||float.IsNaN(center.y)||float.IsInfinity(center.y))return false;
            int matches=0;bool fits=false;
            for(int i=0;i<shown.Goal.NodeCount;i++)
            {
                var node=shown.Goal.Node(i);
                if(node.PublicId!=publicId||!node.PositionKnown)continue;matches++;
                fits=AllInsidePublicGoal(objectiveModel,node,new[]{center});
            }
            return matches==1&&fits;
        }
    }
}
