using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class CampaignOperationsScreenView
    {
        private void ApplyGroundedSignalCard()
        {
            bool fa=UiShellRuntimeGateway.Localization.IsRightToLeft;
            Set(selectedChapterLabel,fa?"هوا و زره":"AIR AND ARMOR");
            for(int i=0;i<(chapterMissionNames?.Length??0);i++)Set(chapterMissionNames[i],FutureMissionComicCatalog.Find(4,i+1)?.Title(fa)??string.Empty);
            Set(missionNumber,"CH04 · M04");Set(missionName,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.name"));
            Set(missionBriefingText,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.summary"));
            Set(primaryObjectiveText,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.objective.hardware"));
            Set(rewardSummaryText,UiShellRuntimeGateway.Localization.Get("mission.grounded_signal.reward.card"));
            if(missionPreviewImage!=null)missionPreviewImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH04M04_GroundedSignal");
        }
    }
}
