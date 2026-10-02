using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Mathematics;
namespace Game.UI.Shell.Ecs
{
 public sealed partial class UiShellEcsGateway
 {
  private static (int Stage,int Clinic,int Utility,int Stable,int ClinicOutage,int UtilityOutage) cachedCitywideAlertStatusStamp;
  private static (int Stage,int Clinic,int Utility,int Stable,int ClinicOutage,int UtilityOutage) ReadCitywideAlertStatusStamp()
  {
   if(!TryCitywideAlert(out _,out _,out var m))return(-1,0,0,0,0,0);
   return(CampaignMissionCitywideAlertRuleUtility.Stage(in m),m.ClinicRecoveryMilliseconds/1000,m.UtilityRecoveryMilliseconds/1000,m.StableMilliseconds/1000,m.ClinicObstructed!=0?math.max(0,(60000-m.ClinicObstructionMilliseconds+999)/1000):-1,m.UtilityObstructed!=0?math.max(0,(60000-m.UtilityObstructionMilliseconds+999)/1000):-1);
  }
  private static string AppendCitywideAlertStatus(string text)
  {
   var s=ReadCitywideAlertStatusStamp();if(s.Stage<0)return text;
   if(s.Stage==4)text+="\n"+GameText.Format("mission.citywide_alert.hud.clinic_recovery","Clinic recovery: {0}/6 s",s.Clinic);
   if(s.Stage==6)text+="\n"+GameText.Format("mission.citywide_alert.hud.utility_recovery","Utility recovery: {0}/6 s",s.Utility);
   if(s.Stage==8)text+="\n"+GameText.Format("mission.citywide_alert.hud.stable","Stable perimeter: {0}/6 s",s.Stable);
   if(s.ClinicOutage>=0)text+="\n"+GameText.Format("mission.citywide_alert.hud.clinic_outage","Clinic service blocked · recover within {0}s",s.ClinicOutage);
   if(s.UtilityOutage>=0)text+="\n"+GameText.Format("mission.citywide_alert.hud.utility_outage","Utility service blocked · recover within {0}s",s.UtilityOutage);
   return text;
  }
 }
}
