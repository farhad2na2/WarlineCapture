using UnityEngine;
namespace Game.Editor.MapVariants
{
    public static partial class OtherMapSurfacePolish
    {
        public static void BuildCampaignRemainingVisuals()
        {
            BuildCampaignMissingDestinationDressing();
            BuildCampaignRunwayEnds();
            BuildCampaignLandscapeFinish();
            CaptureCampaignFixDetails();
            Game.Editor.CampaignRenderReconciliation20261011.Inspect();
            Debug.Log("[CampaignRemainingVisuals] result=Passed requiresNativeReview=true");
        }
    }
}
