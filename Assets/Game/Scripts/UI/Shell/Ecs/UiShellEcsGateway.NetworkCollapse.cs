using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiNetworkCollapseGateway, IUiNetworkCollapseResultGateway
    {
        private static bool TryNetworkCollapse(out EntityManager em,out Entity root,out CampaignMissionNetworkCollapseState m)
        {m=default;if(!TryGetMissionRoot(out em,out root)||!em.HasComponent<CampaignMissionNetworkCollapseState>(root))return false;var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);m=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);return runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)&&runtime.Phase==MissionPhaseKind.Engage&&runtime.Outcome==MissionOutcomeKind.None&&CampaignMissionNetworkCollapseRuleUtility.Matches(in m,in runtime);}
        public bool TryReadNetworkCollapse(out UiNetworkCollapseModel model)
        {model=default;if(!TryNetworkCollapse(out var em,out var root,out var m))return false;var f=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);model=new UiNetworkCollapseModel(CampaignMissionNetworkCollapseRuleUtility.Stage(in m),f.NetworkNodesVerified,f.NetworkNodesDisabled,f.HostileDefeatedCount,m.VerificationMilliseconds/1000,m.RecoveryMilliseconds/1000,m.ExtractionMilliseconds/1000,math.max(0,(900000-m.ElapsedMilliseconds+999)/1000),m.AuditRecovered!=0,m.EngineerAboard!=0,m.Extracted!=0,m.ReconOne,m.ReconTwo,m.ReconThree,m.AuditGate,m.Extraction);return true;}
        public bool TryReadNetworkCollapseResult(out UiNetworkCollapseResultModel model)
        {model=default;if(!TryGetMissionRoot(out var em,out var root))return false;var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);if(!runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)||runtime.Outcome==MissionOutcomeKind.None)return false;var f=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);model=new UiNetworkCollapseResultModel(f.NetworkNodesVerified==3&&f.NetworkNodesDisabled==3,f.NetworkAuditRecovered!=0,f.NetworkExtracted!=0,f.CivilianLossCount);return true;}
        private static bool ResolveNetworkCollapseTarget(EntityManager em,Entity root,in CampaignMissionGuidanceProjectionComponent g,out UiMissionTutorialTarget target)
        {
            target=default;if(!em.HasComponent<CampaignMissionNetworkCollapseState>(root)||!em.Exists(g.SourceEntity)||!em.HasComponent<LocalTransform>(g.SourceEntity))return false;var m=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);float3 position=em.GetComponentData<LocalTransform>(g.SourceEntity).Position,min=position,max=position;bool selected=em.HasComponent<SelectedUnitTag>(g.SourceEntity),moving=IsTutorialActorMoving(em,g.SourceEntity);int count=1;
            bool escort=false;foreach(var member in em.GetBuffer<CampaignMissionNetworkCollapseMember>(root,true))if(member.Entity==g.SourceEntity&&member.Kind==NetworkCollapseMemberKind.Escort)escort=true;
            if(escort){position=default;min=new float3(float.MaxValue);max=new float3(float.MinValue);selected=true;moving=false;count=0;foreach(var member in em.GetBuffer<CampaignMissionNetworkCollapseMember>(root,true)){if(member.Kind!=NetworkCollapseMemberKind.Escort||member.Dead!=0||!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;var p=em.GetComponentData<LocalTransform>(member.Entity).Position;position+=p;min=math.min(min,p);max=math.max(max,p);count++;selected&=em.HasComponent<SelectedUnitTag>(member.Entity);moving|=IsTutorialActorMoving(em,member.Entity);}if(count==0)return false;position/=count;}
            int stage=g.GuidanceId-70000;bool watch=g.RecommendationKind==AssistantRecommendationKind.Explain||stage is 1 or 3 or 5&&m.VerificationMilliseconds>0||stage==7&&m.RecoveryMilliseconds>0;
            var action=watch?UiTutorialBattleAction.Watch:g.RecommendationKind==AssistantRecommendationKind.Attack?UiTutorialBattleAction.Attack:g.RecommendationKind==AssistantRecommendationKind.Logistics?UiTutorialBattleAction.None:UiTutorialBattleAction.Move;
            target=new UiMissionTutorialTarget(position,g.WorldPosition,!watch&&!selected,moving,count,action,executingAttack:action==UiTutorialBattleAction.Attack&&IsTutorialAttackInProgress(em,g.SourceEntity,g.TargetEntity),areaRadius:stage==8?8:6,dragSelection:count>1,selectionMin:min-new float3(3,0,3),selectionMax:max+new float3(3,3,3));return true;
        }
    }
}
