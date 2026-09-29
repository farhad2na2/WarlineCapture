using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        private void ApplySplitFront(in UiMissionBriefingModel model)
        {
            var l=UiShellRuntimeGateway.Localization;
            Set(screenSubtitle,l.IsRightToLeft?"فصل چهارم · هوا و زره":"CHAPTER IV - AIR AND ARMOR");Set(missionNumber,"M03");
            Set(operationCodename,l.Get("mission.split_front.name"));Set(missionTitle,l.Get("mission.split_front.name"));
            Set(missionSummary,l.Get("mission.split_front.summary"));Set(locationLabel,l.Get("mission.split_front.location"));Set(enemyIntelLabel,l.Get("mission.split_front.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH04M03_SplitFront");
            string[] names={"battery","base","civilians"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],l.Get(i<3?"mission.split_front.objective."+names[i]:"mission.split_front.star.3"));
            SetAt(conditionLabels,0,l.Get("mission.split_front.resources"));SetAt(conditionLabels,1,l.Get("mission.split_front.forces"));SetAt(conditionLabels,2,l.Get("mission.split_front.deadline"));
            SetAt(conditionNameLabels,0,l.IsRightToLeft?"ایمنی پرتابگر":"LAUNCHER SAFETY");SetAt(conditionNameLabels,1,l.Get("mission.m03.label.forces"));SetAt(conditionNameLabels,2,l.IsRightToLeft?"پشتیبانی":"SUPPORT");
            SetAt(rewardLabels,2,l.IsRightToLeft?"حملهٔ دقیق":"PRECISION STRIKE");
            SetAt(rewardValues,2,l.Get("mission.reward.unlock","UNLOCK"));
            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Value")?.GetComponent<TMPro.TMP_Text>(),l.IsRightToLeft?"زیاد":"HIGH");
            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Label")?.GetComponent<TMPro.TMP_Text>(),l.IsRightToLeft?"آتشبار و انحراف":"BATTERY + DIVERSION");
            var threat=enemyIntel?.Find("Row_AIR_THREAT/Value")?.GetComponent<TMPro.TMP_Text>();Set(threat,l.IsRightToLeft?"هیچ":"NONE");if(threat!=null)threat.color=new Color32(160,195,65,255);
            var goals=enemyIntel!=null?enemyIntel.parent.Find("StarGoals"):null;for(int i=0;i<3;i++)Set(goals?.Find("Goal"+i)?.GetComponent<TMPro.TMP_Text>(),l.Get("mission.split_front.star."+(i+1)));
        }
    }
}
