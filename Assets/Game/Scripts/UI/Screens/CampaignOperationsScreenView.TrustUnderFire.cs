using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        private void ApplyTrustUnderFireCard()
        {
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            Set(selectedChapterLabel,fa?"فرماندهی سراسری شهر":"CITYWIDE COMMAND");
            for(int i=0;i<(chapterMissionNames?.Length??0);i++)Set(chapterMissionNames[i],FutureMissionComicCatalog.Find(5,i+1)?.Title(fa)??string.Empty);
            Set(missionNumber,"CH05 · M02");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.trust_under_fire.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.trust_under_fire.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.trust_under_fire.objective.north"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.trust_under_fire.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH05M02_TrustUnderFire");
        }
    }
}
