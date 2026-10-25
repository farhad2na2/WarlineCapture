using System;using Game.Components;using Game.Configs;using Game.Simulation;using Unity.Entities;using Unity.Mathematics;
namespace Game.Runtime {
internal sealed class SourceAriaObjectiveApproach {
 readonly EntityManager em;readonly Entity session;readonly SourcePublicPlayerContactOwner contacts;readonly int sourceS002ScenarioIndex;readonly AriaGroundApproachKind approachKind;bool cityFlankReached;
 internal SourceAriaObjectiveApproach(EntityManager em,Entity session,SourcePublicPlayerContactOwner contacts,int sourceS002ScenarioIndex){if(sourceS002ScenarioIndex<0)throw new ArgumentOutOfRangeException(nameof(sourceS002ScenarioIndex));this.sourceS002ScenarioIndex=sourceS002ScenarioIndex;approachKind=sourceS002ScenarioIndex==SkirmishPresetConfig.CityCrossroadsScenarioIndex?AriaGroundApproachKind.CityCrossroads:sourceS002ScenarioIndex==SkirmishPresetConfig.IndustrialBasinScenarioIndex?AriaGroundApproachKind.IndustrialBasin:AriaGroundApproachKind.Ordinary;this.em=em??throw new ArgumentNullException(nameof(em));this.session=session;this.contacts=contacts??throw new ArgumentNullException(nameof(contacts));}
 internal void OnAriaInactive(){cityFlankReached=false;}
 internal SourceAriaPublicApproach? Read(){
  var s=em.GetComponentData<SkirmishExpandedSessionComponent>(session);if(s.CatalogId.ToString()!="S002"||s.ObjectiveKind!=Game.Skirmish.Contracts.SkirmishObjectiveKind.BaseAssault)throw new NotSupportedException("Only source S002 base approach");
  var match=em.GetComponentData<SkirmishMatchState>(session);if(match.ScenarioIndex!=sourceS002ScenarioIndex)throw new NotSupportedException("S002 original source preset identity required; City/flank and other profiles remain native");
  var facts=SkirmishPublicObjectiveProjection.Read(em,match);if(!facts.EnemyObjectiveAlive)return null;if(!facts.PlayerPositionKnown)throw new NotSupportedException("Actual public player objective position required");
  var rows=contacts.ReadCurrent();var distances=AriaGroundApproachPolicy.Observe(facts.EnemyPosition,rows,SkirmishBaseAssaultHudProjection.ReadEnemyBase(em,session).DisplayedCurrent,SkirmishArmySelectionService.SelectedLivingCount(em,session));
  AriaGroundPosition p;
  if(approachKind==AriaGroundApproachKind.CityCrossroads){var city=AriaGroundApproachPolicy.City(facts.EnemyPosition,facts.PlayerPosition,rows,distances.AssaultAtBase,ref cityFlankReached);if(!city.AdvancePreferred)return null;p=city.Goal;}
  else{bool basin=approachKind==AriaGroundApproachKind.IndustrialBasin;if(!AriaGroundApproachPolicy.AdvancePreferred(basin,distances))return null;p=AriaGroundApproachPolicy.Approach(basin,facts.EnemyPosition,facts.PlayerPosition,distances);}
  var position=new float3(p.X,p.Y,p.Z);
  using var grids=em.CreateEntityQuery(typeof(GridConfig));if(grids.CalculateEntityCount()!=1)throw new NotSupportedException("Actual unique source grid required");var grid=grids.GetSingleton<GridConfig>();return new SourceAriaPublicApproach(GridUtils.WorldToCell(grid,position),position);
 }
}
}
