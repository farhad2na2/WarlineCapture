using System;
using Game.Skirmish.Contracts;
namespace Game.Simulation {
// Concrete mission data, never a launch/publication/owner-admission certificate.
// A future Runtime factory must validate compiled source identity and bound capabilities.
public readonly struct GroundMissionContext {
 public readonly string CatalogId,DefinitionId,SessionId,SourceApproachIdentity;
 public readonly int ContentVersion,PlayableScenarioIndex;
 public readonly uint SetupHash;
 public readonly SkirmishObjectiveKind ObjectiveKind;
 public readonly SkirmishArmyProfileId ArmyProfileId;
 public readonly SkirmishStartPackageId StartPackageId;
 public readonly SkirmishReadinessStage Readiness;
 public readonly AriaGroundApproachKind ApproachKind;
 public GroundMissionContext(string catalogId,string definitionId,int contentVersion,uint setupHash,string sessionId,int playableScenarioIndex,SkirmishObjectiveKind objectiveKind,SkirmishArmyProfileId armyProfileId,SkirmishStartPackageId startPackageId,SkirmishReadinessStage readiness,AriaGroundApproachKind approachKind,string sourceApproachIdentity){
  CatalogId=catalogId;DefinitionId=definitionId;ContentVersion=contentVersion;SetupHash=setupHash;SessionId=sessionId;PlayableScenarioIndex=playableScenarioIndex;ObjectiveKind=objectiveKind;ArmyProfileId=armyProfileId;StartPackageId=startPackageId;Readiness=readiness;ApproachKind=approachKind;SourceApproachIdentity=sourceApproachIdentity;
 }
 internal void RequireCompatibleObservation(in AriaS002Observation view){
  if(view.Scope!=AriaS002Scope.NativeRendered&&view.Scope!=AriaS002Scope.LogicalPublicData)throw new ArgumentException("Explicit source observation scope required");
  if(!new SkirmishCatalogId(CatalogId).IsValid||DefinitionId!=SkirmishDefinitionId.FromCatalog(new SkirmishCatalogId(CatalogId)).Value||ContentVersion<=0||string.IsNullOrEmpty(SessionId)||string.IsNullOrEmpty(SourceApproachIdentity)||PlayableScenarioIndex< -1)
   throw new ArgumentException("Concrete compiled mission identity data required");
  if(ObjectiveKind!=SkirmishObjectiveKind.BaseAssault||ArmyProfileId!=SkirmishArmyProfileId.GroundManeuver||StartPackageId!=SkirmishStartPackageId.EstablishedBase||Readiness!=SkirmishReadinessStage.Established||
   (ApproachKind!=AriaGroundApproachKind.Ordinary&&ApproachKind!=AriaGroundApproachKind.CityCrossroads&&ApproachKind!=AriaGroundApproachKind.IndustrialBasin)||view.SourceScenarioIndex!=PlayableScenarioIndex||!view.ExpandedSession||!view.ExpandedBaseAssault||view.AirProfile||view.CombinedArmsField)
   throw new NotSupportedException("Matching Ground/Established/BaseAssault source observation required");
 }
}
}
