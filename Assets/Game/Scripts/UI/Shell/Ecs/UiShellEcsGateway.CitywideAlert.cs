using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.UI.Shell.Ecs
{
 public sealed partial class UiShellEcsGateway : IUiCitywideAlertGateway
 {
  private static bool TryCitywideAlert(out EntityManager em,out Entity root,out CampaignMissionCitywideAlertState mission)
  {
   mission=default;if(!TryGetMissionRoot(out em,out root)||!em.HasComponent<CampaignMissionCitywideAlertState>(root))return false;
   var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);mission=em.GetComponentData<CampaignMissionCitywideAlertState>(root);
   return runtime.MissionId.Equals(CampaignMissionSequence.CitywideAlert)&&runtime.Phase==MissionPhaseKind.Engage&&runtime.Outcome==MissionOutcomeKind.None&&CampaignMissionCitywideAlertRuleUtility.Matches(in mission,in runtime);
  }
  public bool TryReadCitywideAlert(out UiCitywideAlertModel model)
  {
   model=default;if(!TryCitywideAlert(out var em,out var root,out var m))return false;
   var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
   model=new UiCitywideAlertModel(CampaignMissionCitywideAlertRuleUtility.Stage(in m),m.ClinicRecoveryMilliseconds/1000,m.UtilityRecoveryMilliseconds/1000,math.max(0,(CampaignMissionCitywideAlertRuleUtility.DeadlineMilliseconds-m.ElapsedMilliseconds+999)/1000),facts.HostileDefeatedCount,m.ClinicRecovered!=0,m.UtilityRecovered!=0,m.ReinforcementReady!=0,m.ClinicObstructed!=0,m.UtilityObstructed!=0,m.ClinicCenter,m.UtilityCenter,m.CoverageCenter,m.PerimeterCenter,m.ClinicRecoveryCenter,m.UtilityRecoveryCenter);return true;
  }
  private static bool ResolveCitywideAlertTarget(EntityManager em,Entity root,in CampaignMissionGuidanceProjectionComponent g,out UiMissionTutorialTarget target)
  {
   target=default;if(!em.HasComponent<CampaignMissionCitywideAlertState>(root)||!em.Exists(g.SourceEntity)||!em.HasComponent<LocalTransform>(g.SourceEntity))return false;
   var m=em.GetComponentData<CampaignMissionCitywideAlertState>(root);int stage=g.GuidanceId-68000;
   float3 selection=em.GetComponentData<LocalTransform>(g.SourceEntity).Position,min=selection,max=selection;
   bool selected=em.HasComponent<SelectedUnitTag>(g.SourceEntity),moving=IsTutorialActorMoving(em,g.SourceEntity);int required=1;
   if(stage is 3 or 5 && g.SourceEntity!=m.AirDefense)
   {
    selected=true;moving=false;selection=default;min=new float3(float.MaxValue);max=new float3(float.MinValue);required=0;
    var kind=stage==3?CitywideAlertMemberKind.ClinicResponse:CitywideAlertMemberKind.UtilityResponse;
    foreach(var member in em.GetBuffer<CampaignMissionCitywideAlertMember>(root,true))
     if(member.Entity==g.SourceEntity&&member.Kind is CitywideAlertMemberKind.ClinicResponse or CitywideAlertMemberKind.UtilityResponse){kind=member.Kind;break;}
    foreach(var member in em.GetBuffer<CampaignMissionCitywideAlertMember>(root,true))
    {if(member.Dead!=0||member.Kind!=kind||!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
     var p=em.GetComponentData<LocalTransform>(member.Entity).Position;selection+=p;min=math.min(min,p);max=math.max(max,p);required++;selected&=em.HasComponent<SelectedUnitTag>(member.Entity);moving|=IsTutorialActorMoving(em,member.Entity);}
    if(required==0)return false;selection/=required;
   }
   bool watching=stage==8||stage is 4 or 6&&(stage==4?m.ClinicRecoveryMilliseconds:m.UtilityRecoveryMilliseconds)>0||g.RecommendationKind==AssistantRecommendationKind.Explain;
   var action=stage==1?UiTutorialBattleAction.None:watching?UiTutorialBattleAction.Watch:g.RecommendationKind==AssistantRecommendationKind.Attack?UiTutorialBattleAction.Attack:UiTutorialBattleAction.Move;
   target=new UiMissionTutorialTarget(selection,g.WorldPosition,stage!=1&&!watching&&!selected,moving,required,action,executingAttack:action==UiTutorialBattleAction.Attack&&IsTutorialAttackInProgress(em,g.SourceEntity,g.TargetEntity),areaRadius:stage is 4 or 6?6:stage==7?m.PerimeterRadius:m.CoverageRadius,dragSelection:required>1,selectionMin:min-new float3(3,0,3),selectionMax:max+new float3(3,3,3));return true;
  }
 }
}
