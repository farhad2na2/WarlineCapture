using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        private void ApplySteelPush(in UiMissionBriefingModel model)
        {
            var l=UiShellRuntimeGateway.Localization;
            Set(screenSubtitle,l.IsRightToLeft?"فصل چهارم · هوا و زره":"CHAPTER IV - AIR AND ARMOR");Set(missionNumber,"M02");
            Set(operationCodename,l.Get("mission.steel_push.name"));Set(missionTitle,l.Get("mission.steel_push.name"));
            Set(missionSummary,l.Get("mission.steel_push.summary"));Set(locationLabel,l.Get("mission.steel_push.location"));Set(enemyIntelLabel,l.Get("mission.steel_push.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH04M02_SteelPush");
            string[] names={"column","fuel","relay"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],l.Get(i<3?"mission.steel_push.objective."+names[i]:"mission.steel_push.star.3"));
            SetAt(conditionLabels,0,l.Get("mission.steel_push.resources"));SetAt(conditionLabels,1,l.Get("mission.steel_push.forces"));SetAt(conditionLabels,2,l.Get("mission.steel_push.deadline"));
            SetAt(conditionNameLabels,0,l.IsRightToLeft?"ذخیرهٔ سوخت":"FUEL RESERVE");SetAt(conditionNameLabels,1,l.Get("mission.m03.label.forces"));SetAt(conditionNameLabels,2,l.IsRightToLeft?"مهلت دفاع":"DEFENSE DEADLINE");
            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Value")?.GetComponent<TMPro.TMP_Text>(),l.IsRightToLeft?"زیاد":"HIGH");
            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Label")?.GetComponent<TMPro.TMP_Text>(),l.IsRightToLeft?"خودروهای زرهی":"ARMORED VEHICLES");
            var threat=enemyIntel?.Find("Row_AIR_THREAT/Value")?.GetComponent<TMPro.TMP_Text>();Set(threat,l.IsRightToLeft?"هیچ":"NONE");if(threat!=null)threat.color=new Color32(160,195,65,255);
            var goals=enemyIntel!=null?enemyIntel.parent.Find("StarGoals"):null;for(int i=0;i<3;i++)Set(goals?.Find("Goal"+i)?.GetComponent<TMPro.TMP_Text>(),l.Get("mission.steel_push.star."+(i+1)));
        }
    }
}
