using UnityEngine;
namespace Game.UI.Runtime
{
 public sealed class CitywideAlertMarkersView:MonoBehaviour
 {
  private GameObject root;private TacticalGroundMarkerView clinic,utility,coverage,perimeter;
  private void LateUpdate()
  {
   if(!UiShellRuntimeGateway.TryReadCitywideAlert(out var m)||UiShellRuntimeGateway.TryReadMissionCameraTour()){if(root!=null)root.SetActive(false);return;}
   if(root==null){root=new GameObject("Citywide service objectives");clinic=Create("Clinic gate");utility=Create("Utility gate");coverage=Create("Air defense");perimeter=Create("Reinforcement perimeter");}
   root.SetActive(true);var cyan=new Color32(0,198,235,255);var amber=new Color32(255,191,48,255);var green=new Color32(94,224,112,255);
   clinic.Configure(m.ClinicRecovery,4,m.ClinicRecovered?green:amber,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.marker.clinic"),TacticalGroundMarkerView.Symbol.People,true);
   utility.Configure(m.UtilityRecovery,4,m.UtilityRecovered?green:amber,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.marker.utility"),TacticalGroundMarkerView.Symbol.People,true);
   coverage.gameObject.SetActive(m.Stage<=2);coverage.Configure(m.Coverage,6,cyan,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.marker.coverage"),TacticalGroundMarkerView.Symbol.Clock,true);
   perimeter.gameObject.SetActive(m.Stage>=7);perimeter.Configure(m.Perimeter,5,m.ReinforcementReady?green:cyan,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.marker.perimeter"),TacticalGroundMarkerView.Symbol.People,true);
  }
  private TacticalGroundMarkerView Create(string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);return go.AddComponent<TacticalGroundMarkerView>();}
  private void OnDisable(){if(root!=null)root.SetActive(false);}private void OnDestroy(){if(root!=null)Destroy(root);}
 }
}
