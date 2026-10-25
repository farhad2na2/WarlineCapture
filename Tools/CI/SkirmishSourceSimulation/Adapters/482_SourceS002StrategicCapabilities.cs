using System;using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Game.UI.Contracts;using Unity.Entities;
namespace Game.Runtime{
internal readonly struct SourceS002StrategicCapabilities{
 internal readonly bool ReadinessEligible,CanUpgradeReadiness,CanBuildAirPad,PadPresent,AirQueueOffered,ReadinessRead;
 internal readonly UiSkirmishReadinessModel Readiness;
 private SourceS002StrategicCapabilities(SkirmishAriaPublicView view,bool read,UiSkirmishReadinessModel readiness){ReadinessEligible=view.ReadinessEligible;CanBuildAirPad=view.CanBuildAirPad;PadPresent=view.PadPresent;AirQueueOffered=view.AirQueueOffered;ReadinessRead=read;Readiness=readiness;CanUpgradeReadiness=read&&readiness.CanUpgrade;}
 internal static SourceS002StrategicCapabilities Read(EntityManager em,Entity session,SkirmishArmyProfileConfig army){
  if(em==null||army==null||army.AllowsOffensiveAir)throw new NotSupportedException("Actual ground S002 army required; active air public read-domain not admitted");
  if(!em.HasComponent<SkirmishResolvedSetupRecord>(session)||em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup?.CatalogId!="S002")throw new NotSupportedException("Actual S002 setup required");
  // Byte-identical current original source. Ground AirProfile=false short-circuits
  // CanAffordBuilding before any configured-building read; unknown UI registry is irrelevant.
  var view=SkirmishAriaPublicProjection.FromSession(em,session,army);
  bool read=SourceS002ReadinessGateway.Read(em,session,out var readiness);
  return new SourceS002StrategicCapabilities(view,read,readiness);
 }
}
}
