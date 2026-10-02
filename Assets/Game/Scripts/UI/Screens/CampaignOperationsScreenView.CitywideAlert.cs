using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        private void ApplyCitywideAlertCard()
        {
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            Set(selectedChapterLabel,fa?"فرماندهی سراسری شهر":"CITYWIDE COMMAND");
            for(int i=0;i<(chapterMissionNames?.Length??0);i++)Set(chapterMissionNames[i],FutureMissionComicCatalog.Find(5,i+1)?.Title(fa)??string.Empty);
            Set(missionNumber,"CH05 · M01");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.objective.clinic"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH05M01_CitywideAlert");
        }
    }
}
