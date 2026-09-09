using Game.Missions.Contracts;
using UnityEditor;

namespace Game.Editor
{
    public static partial class M03RadarWarningConfigBuilder
    {
        private static void PopulateGuidance(SerializedProperty defense)
        {
            string[] names={"read_warning","inspect_route","choose_defense","build_option","position_squads","hold","stop","refresh","reinforce","priority","adapt","result"};
            MissionGuidanceActionKind[] actions={MissionGuidanceActionKind.ReadWarning,MissionGuidanceActionKind.FocusWarning,
                MissionGuidanceActionKind.Explain,MissionGuidanceActionKind.OpenBuild,MissionGuidanceActionKind.Move,
                MissionGuidanceActionKind.Hold,MissionGuidanceActionKind.Stop,MissionGuidanceActionKind.RadarPing,
                MissionGuidanceActionKind.OpenProduction,MissionGuidanceActionKind.InspectContact,
                MissionGuidanceActionKind.FocusWarning,MissionGuidanceActionKind.ReviewResult};
            MissionGuidanceCompletionKind[] completion={MissionGuidanceCompletionKind.WarningRead,MissionGuidanceCompletionKind.RouteInspected,
                MissionGuidanceCompletionKind.Acknowledged,MissionGuidanceCompletionKind.DefenseBuilt,MissionGuidanceCompletionKind.SquadPositioned,
                MissionGuidanceCompletionKind.Holding,MissionGuidanceCompletionKind.StopAccepted,MissionGuidanceCompletionKind.PingAccepted,
                MissionGuidanceCompletionKind.ReinforcementProduced,MissionGuidanceCompletionKind.HostileDefeated,
                MissionGuidanceCompletionKind.MainElementActivated,MissionGuidanceCompletionKind.ResultSettled};
            Array(defense.FindPropertyRelative("guidanceSteps"),12,(step,i)=>
            {
                Set(step,"stepId","tutorial.ch01.m03."+(i+1).ToString("00")+"."+names[i]);
                Set(step,"titleKey","mission.m03.tutorial."+(i+1)+".title");
                Set(step,"bodyKey","mission.m03.tutorial."+(i+1)+".body");
                Set(step,"action",(int)actions[i]); Set(step,"completion",(int)completion[i]);
                Set(step,"optional",i is 3 or 6 or 7 or 8);
            });
        }
    }
}
