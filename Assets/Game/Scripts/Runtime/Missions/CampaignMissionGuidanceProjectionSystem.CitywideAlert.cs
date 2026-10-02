using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
 public partial struct CampaignMissionGuidanceProjectionSystem
 {
  private static readonly FixedString64Bytes CitywideGuidanceMissionId="saga.ch05.m01.citywide_alert";
  private bool TryUpdateCitywideAlertGuidance(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
  {
   if(!runtime.MissionId.Equals(CitywideGuidanceMissionId))return false;var em=system.EntityManager;
   if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionCitywideAlertState>(root)){ClearDefenseGuidance(em,root,in current);return true;}
   var m=em.GetComponentData<CampaignMissionCitywideAlertState>(root);
   if(!CampaignMissionCitywideAlertRuleUtility.Matches(in m,in runtime)){ClearDefenseGuidance(em,root,in current);return true;}
   int step=CampaignMissionCitywideAlertRuleUtility.Stage(in m);
   Entity actor=step==1?m.ClinicProducer:step==2?m.AirDefense:step==4?m.ClinicEngineer:step==6?m.UtilityEngineer:step==7?m.Reinforcement:m.ClinicEngineer;
   Entity hostile=Entity.Null;float3 destination=step==1?m.ClinicDefense:step==2?m.CoverageCenter:step==3?m.ClinicDefense:step==4?m.ClinicRecoveryCenter:step==5?m.UtilityDefense:step==6?m.UtilityRecoveryCenter:m.PerimeterCenter;
   if(step is 3 or 5)
   {
    actor=Entity.Null;var response=step==3?CitywideAlertMemberKind.ClinicResponse:CitywideAlertMemberKind.UtilityResponse;var threat=step==3?CitywideAlertMemberKind.ClinicHostile:CitywideAlertMemberKind.UtilityHostile;
    actor=ResolveCitywideResponseActor(em,root,response);
    foreach(var member in em.GetBuffer<CampaignMissionCitywideAlertMember>(root,true))
    {if(member.Dead!=0||!em.Exists(member.Entity))continue;if(hostile==Entity.Null&&member.Kind==threat&&!em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity)&&em.HasComponent<LocalTransform>(member.Entity))hostile=member.Entity;}
   }
   if(actor==Entity.Null||!em.Exists(actor)||!em.HasComponent<LocalTransform>(actor)){ClearDefenseGuidance(em,root,in current);return true;}
   var kind=step is 1 or 8?AssistantRecommendationKind.Explain:AssistantRecommendationKind.Move;
   if(step is 3 or 5 && hostile==Entity.Null)kind=AssistantRecommendationKind.Explain;
   if(hostile!=Entity.Null)
   {destination=em.GetComponentData<LocalTransform>(hostile).Position;kind=AssistantRecommendationKind.Attack;}
   if(step==5&&m.UtilityThreatsCleared!=0&&m.AirCleared==0){actor=m.AirDefense;destination=m.CoverageCenter;kind=AssistantRecommendationKind.Explain;}
   FixedString64Bytes title="mission.citywide_alert.tutorial.";title.Append(step);title.Append(ExtractionTitleSuffix);
   FixedString128Bytes body="mission.citywide_alert.tutorial.";body.Append(step);body.Append(ExtractionBodySuffix);
   var next=new CampaignMissionGuidanceProjectionComponent{GuidanceId=68000+step,Version=Next(current.Version),MissionSourceVersion=runtime.Version,Prompt=(CampaignMissionGuidancePromptKind)(100+step),GuidanceMode=NarrativeGuidanceMode.Full,Active=1,RecommendationKind=kind,TargetKind=AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=hostile,WorldPosition=destination,HasWorldPosition=1,Title=title,Body=body,ActionLabel=RadarAct,CanShow=1,CanExecute=0,Priority=AssistantMessagePriority.High,SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
   em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear();if(!ProjectionEquals(in current,in next)||!current.Body.Equals(next.Body))em.SetComponentData(root,next);return true;
  }
  internal static Entity ResolveCitywideResponseActor(EntityManager em,Entity root,CitywideAlertMemberKind preferred)
  {
   if(!em.HasBuffer<CampaignMissionCitywideAlertMember>(root))return Entity.Null;
   var roster=em.GetBuffer<CampaignMissionCitywideAlertMember>(root,true);
   var alternate=preferred==CitywideAlertMemberKind.ClinicResponse?CitywideAlertMemberKind.UtilityResponse:CitywideAlertMemberKind.ClinicResponse;
   for(int pass=0;pass<2;pass++)foreach(var member in roster)
   {
    if(member.Kind!=(pass==0?preferred:alternate)||member.Dead!=0||!em.Exists(member.Entity)||!em.HasComponent<UnitHealth>(member.Entity)||
       !em.HasComponent<UnitCombat>(member.Entity)||!em.HasComponent<UnitAttack>(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
    var health=em.GetComponentData<UnitHealth>(member.Entity);var weapon=em.GetComponentData<UnitAttack>(member.Entity);
    if(health.Max>0&&health.Current>0&&em.GetComponentData<UnitCombat>(member.Entity).CanAttack!=0&&weapon.Damage>0&&weapon.Range>0)return member.Entity;
   }
   return Entity.Null;
  }
 }
}
