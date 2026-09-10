using System.Collections.Generic;
using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class UIShellEcsPresentationSystem
    {
        private readonly List<MissionDefenseHudView> defenseViews = new();
        private readonly List<MissionExtractionHudView> extractionViews = new();
        private readonly List<CampaignMissionScreenBinderView> campaignViews = new();
        private UIShellContentView missionContent;
        private int missionContentVersion = -1;
        private float nextMissionProjectionRefresh;

        private void RefreshBoundMissionViews()
        {
            var content = shellView != null ? shellView.ContentSystem : null;
            if(content != missionContent || (content != null && content.ContentVersion != missionContentVersion))
            {
                missionContent = content;
                missionContentVersion = content != null ? content.ContentVersion : -1;
                defenseViews.Clear(); extractionViews.Clear(); campaignViews.Clear();
                if(content != null)
                {
                    CollectMissionViews(content, UIShellRegionId.HeaderRegion);
                    CollectMissionViews(content, UIShellRegionId.MiddleRegion);
                    CollectMissionViews(content, UIShellRegionId.LeftRegion);
                    CollectMissionViews(content, UIShellRegionId.RightRegion);
                    CollectMissionViews(content, UIShellRegionId.FooterRegion);
                    CollectMissionViews(content, UIShellRegionId.PopupLayer);
                }
                nextMissionProjectionRefresh = 0;
            }
            foreach(var view in defenseViews)
                if(view != null && view.isActiveAndEnabled) view.RefreshCameraTour();
            if(Time.unscaledTime < nextMissionProjectionRefresh) return;
            nextMissionProjectionRefresh = Time.unscaledTime + .25f;
            foreach(var view in defenseViews)
                if(view != null && view.isActiveAndEnabled) view.RefreshPresentation();
            foreach(var view in extractionViews)
                if(view != null && view.isActiveAndEnabled) view.RefreshPresentation();
            foreach(var view in campaignViews)
                if(view != null && view.isActiveAndEnabled) view.RefreshProjection();
        }

        private void CollectMissionViews(UIShellContentView content, UIShellRegionId region)
        {
            if(!content.TryGetRegionContentRoot(region, out var root) || root == null) return;
            // Discovery is bounded to explicitly owned region roots and only runs on a mount/version change.
            defenseViews.AddRange(root.GetComponentsInChildren<MissionDefenseHudView>(true));
            extractionViews.AddRange(root.GetComponentsInChildren<MissionExtractionHudView>(true));
            campaignViews.AddRange(root.GetComponentsInChildren<CampaignMissionScreenBinderView>(true));
        }
    }
}
