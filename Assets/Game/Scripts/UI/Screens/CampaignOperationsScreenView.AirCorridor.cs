using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        private void ApplyAirCorridorCard()
        {
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            Set(selectedChapterLabel,fa?"هوا و زره":"AIR AND ARMOR");
            for(int i=0;i<(chapterMissionNames?.Length??0);i++)Set(chapterMissionNames[i],FutureMissionComicCatalog.Find(4,i+1)?.Title(fa)??string.Empty);
            Set(missionNumber,"CH04 · M01");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.air_corridor.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.air_corridor.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.air_corridor.objective.aircraft"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.air_corridor.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH04M01_AirCorridor");
        }
    }
}
