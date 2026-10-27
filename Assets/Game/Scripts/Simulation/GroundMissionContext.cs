using System;
using Game.Skirmish.Contracts;
namespace Game.Simulation {
// Concrete mission data, never a launch/publication/owner-admission certificate.
// Runtime factory binds compiled identities; this value never grants launch or rendering authority.
public readonly struct GroundMissionContext {
 public readonly string CatalogId,DefinitionId,SessionId,SourceApproachIdentity;
 public readonly int ContentVersion,PlayableScenarioIndex;
 public readonly uint SetupHash;
 public readonly string CanonicalMapId,SourcePresetResourceName;
 public readonly int SourcePresetLoadIndex,SourcePresetIndex;
 public readonly AriaS002Scope ObservationScope;
 public readonly SkirmishObjectiveKind ObjectiveKind;
 public readonly SkirmishArmyProfileId ArmyProfileId;
 public readonly SkirmishStartPackageId StartPackageId;
 public readonly SkirmishReadinessStage Readiness;
 public readonly AriaGroundApproachKind ApproachKind;
 public GroundMissionContext(string catalogId,string definitionId,int contentVersion,uint setupHash,string sessionId,int playableScenarioIndex,SkirmishObjectiveKind objectiveKind,SkirmishArmyProfileId armyProfileId,SkirmishStartPackageId startPackageId,SkirmishReadinessStage readiness,AriaGroundApproachKind approachKind,string sourceApproachIdentity){
  CanonicalMapId=null;SourcePresetResourceName=null;SourcePresetLoadIndex=-1;SourcePresetIndex=-1;ObservationScope=default;
  CatalogId=catalogId;DefinitionId=definitionId;ContentVersion=contentVersion;SetupHash=setupHash;SessionId=sessionId;PlayableScenarioIndex=playableScenarioIndex;ObjectiveKind=objectiveKind;ArmyProfileId=armyProfileId;StartPackageId=startPackageId;Readiness=readiness;ApproachKind=approachKind;SourceApproachIdentity=sourceApproachIdentity;
 }
 public GroundMissionContext(string catalogId,string definitionId,int contentVersion,uint setupHash,string sessionId,int playableScenarioIndex,SkirmishObjectiveKind objectiveKind,SkirmishArmyProfileId armyProfileId,SkirmishStartPackageId startPackageId,SkirmishReadinessStage readiness,AriaGroundApproachKind approachKind,string sourceApproachIdentity,string canonicalMapId,int sourcePresetLoadIndex,int sourcePresetIndex,string sourcePresetResourceName,AriaS002Scope observationScope)
  :this(catalogId,definitionId,contentVersion,setupHash,sessionId,playableScenarioIndex,objectiveKind,armyProfileId,startPackageId,readiness,approachKind,sourceApproachIdentity){
  CanonicalMapId=canonicalMapId;SourcePresetLoadIndex=sourcePresetLoadIndex;SourcePresetIndex=sourcePresetIndex;SourcePresetResourceName=sourcePresetResourceName;ObservationScope=observationScope;
 }
 public static bool SupportsOriginalGroundStart(SkirmishStartPackageId start,SkirmishReadinessStage readiness)=>
  (start==SkirmishStartPackageId.FieldBase&&readiness==SkirmishReadinessStage.Field)||
  (start==SkirmishStartPackageId.EstablishedBase&&readiness==SkirmishReadinessStage.Established);
 internal void RequireCompatibleObservation(in AriaS002Observation view){
  if(ObservationScope!=default&&view.Scope!=ObservationScope)throw new NotSupportedException("Compiled context observation transport differs");
  if(view.Scope!=AriaS002Scope.NativeRendered&&view.Scope!=AriaS002Scope.LogicalPublicData)throw new ArgumentException("Explicit source observation scope required");
  if(!new SkirmishCatalogId(CatalogId).IsValid||DefinitionId!=SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(CatalogId)).Value||ContentVersion<=0||string.IsNullOrEmpty(SessionId)||string.IsNullOrEmpty(SourceApproachIdentity)||PlayableScenarioIndex< -1)
   throw new ArgumentException("Concrete compiled mission identity data required");
  if(ObjectiveKind!=SkirmishObjectiveKind.BaseAssault||ArmyProfileId!=SkirmishArmyProfileId.GroundManeuver||!SupportsOriginalGroundStart(StartPackageId,Readiness)||
   (ApproachKind!=AriaGroundApproachKind.Ordinary&&ApproachKind!=AriaGroundApproachKind.CityCrossroads&&ApproachKind!=AriaGroundApproachKind.IndustrialBasin&&ApproachKind!=AriaGroundApproachKind.AuthoredLogicalGround)||view.SourceScenarioIndex!=PlayableScenarioIndex||!view.ExpandedSession||!view.ExpandedBaseAssault||view.AirProfile||view.CombinedArmsField)
   throw new NotSupportedException("Matching Ground/Field-or-Established/BaseAssault source observation required");
 }
}
}
