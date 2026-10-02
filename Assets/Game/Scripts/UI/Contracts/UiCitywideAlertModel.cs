using UnityEngine;
namespace Game.UI.Contracts
{
 public readonly struct UiCitywideAlertModel
 {
  public readonly int Stage,ClinicRecoverySeconds,UtilityRecoverySeconds,RemainingSeconds,HostilesCleared;
  public readonly bool ClinicRecovered,UtilityRecovered,ReinforcementReady,ClinicObstructed,UtilityObstructed;
  public readonly Vector3 Clinic,Utility,Coverage,Perimeter,ClinicRecovery,UtilityRecovery;
  public UiCitywideAlertModel(int stage,int clinicSeconds,int utilitySeconds,int remaining,int cleared,bool clinicRecovered,bool utilityRecovered,bool reinforcementReady,bool clinicObstructed,bool utilityObstructed,Vector3 clinic,Vector3 utility,Vector3 coverage,Vector3 perimeter,Vector3 clinicRecovery,Vector3 utilityRecovery)
  {Stage=stage;ClinicRecoverySeconds=clinicSeconds;UtilityRecoverySeconds=utilitySeconds;RemainingSeconds=remaining;HostilesCleared=cleared;ClinicRecovered=clinicRecovered;UtilityRecovered=utilityRecovered;ReinforcementReady=reinforcementReady;ClinicObstructed=clinicObstructed;UtilityObstructed=utilityObstructed;Clinic=clinic;Utility=utility;Coverage=coverage;Perimeter=perimeter;ClinicRecovery=clinicRecovery;UtilityRecovery=utilityRecovery;}
 }
 public interface IUiCitywideAlertGateway {bool TryReadCitywideAlert(out UiCitywideAlertModel model);}
}
