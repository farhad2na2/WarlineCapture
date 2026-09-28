using Game.UI.Contracts;
using UnityEngine;

namespace Game.UI.Runtime
{
    public sealed partial class MissionBriefingScreenView
    {
        private void ApplyAirCorridor(in UiMissionBriefingModel model)
        {
            var l=UiShellRuntimeGateway.Localization;
            Set(screenSubtitle,l.IsRightToLeft?"فصل چهارم · هوا و زره":"CHAPTER IV - AIR AND ARMOR");Set(missionNumber,"M01");
            Set(operationCodename,l.Get("mission.air_corridor.name"));Set(missionTitle,l.Get("mission.air_corridor.name"));
            Set(missionSummary,l.Get("mission.air_corridor.summary"));Set(locationLabel,l.Get("mission.air_corridor.location"));Set(enemyIntelLabel,l.Get("mission.air_corridor.enemy_intel"));
            if(missionArtImage!=null)missionArtImage.texture=Resources.Load<Texture2D>("FutureMissionComics/CH04M01_AirCorridor");
            string[] names={"aircraft","radar","corridor"};for(int i=0;i<(objectiveLabels?.Length??0);i++)Set(objectiveLabels[i],l.Get(i<3?"mission.air_corridor.objective."+names[i]:"mission.air_corridor.star.3"));
            SetAt(conditionLabels,0,l.Get("mission.air_corridor.resources"));SetAt(conditionLabels,1,l.Get("mission.air_corridor.forces"));SetAt(conditionLabels,2,l.Get("mission.air_corridor.deadline"));
            SetAt(conditionNameLabels,0,l.IsRightToLeft?"پشتیبانی رادار":"RADAR SUPPORT");SetAt(conditionNameLabels,1,l.Get("mission.m03.label.forces"));SetAt(conditionNameLabels,2,l.IsRightToLeft?"مهلت دفاع":"DEFENSE DEADLINE");
            Set(enemyIntel?.Find("Row_LIGHT_VEHICLES/Value")?.GetComponent<TMPro.TMP_Text>(),l.IsRightToLeft?"هیچ":"NONE");
            var threat=enemyIntel?.Find("Row_AIR_THREAT/Value")?.GetComponent<TMPro.TMP_Text>();Set(threat,l.IsRightToLeft?"زیاد":"HIGH");if(threat!=null)threat.color=new Color32(255,75,40,255);
            var goals=enemyIntel!=null?enemyIntel.parent.Find("StarGoals"):null;for(int i=0;i<3;i++)Set(goals?.Find("Goal"+i)?.GetComponent<TMPro.TMP_Text>(),l.Get("mission.air_corridor.star."+(i+1)));
        }
    }
}
