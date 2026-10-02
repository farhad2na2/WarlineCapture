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
        private static readonly FixedString64Bytes NetworkGuidanceId=CampaignMissionSequence.NetworkCollapse,NetworkApproachSuffix=".approach",NetworkReconSuffix=".recon",NetworkBoardSuffix=".board";
        private bool TryUpdateNetworkCollapseGuidance(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(NetworkGuidanceId))return false;var em=system.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionNetworkCollapseState>(root)){ClearDefenseGuidance(em,root,in current);return true;}
            var m=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);if(!CampaignMissionNetworkCollapseRuleUtility.Matches(in m,in runtime)){ClearDefenseGuidance(em,root,in current);return true;}
            int stage=CampaignMissionNetworkCollapseRuleUtility.Stage(in m);Entity actor=m.Engineer,target=Entity.Null;float3 position=stage==1?m.ReconOne:stage==3?m.ReconTwo:stage==5?m.ReconThree:stage==7?m.AuditGate:m.Extraction;
            var kind=AssistantRecommendationKind.Move;bool combat=false;
            if(stage<=6)
            {
                var targetKind=(NetworkCollapseMemberKind)((int)(stage%2==0?NetworkCollapseMemberKind.NodeOne:NetworkCollapseMemberKind.GuardOne)+(stage-1)/2);
                foreach(var member in em.GetBuffer<CampaignMissionNetworkCollapseMember>(root,true))if(member.Kind==targetKind&&member.Dead==0&&em.Exists(member.Entity)&&!em.HasComponent<CampaignMissionCombatSuppressedTag>(member.Entity)){target=member.Entity;break;}
                if(target!=Entity.Null){actor=CampaignMissionRuntimeSystem.ResolveNetworkEscort(em,root);combat=true;}
            }
            if(stage==8){if(m.EngineerAboard==0){actor=m.Engineer;target=m.Carrier;kind=AssistantRecommendationKind.Logistics;}else {actor=m.Carrier;if(m.ExtractionMilliseconds>0)kind=AssistantRecommendationKind.Explain;}}
            if(actor==Entity.Null||!em.Exists(actor)||!em.HasComponent<LocalTransform>(actor)){ClearDefenseGuidance(em,root,in current);return true;}
            if(target!=Entity.Null&&em.Exists(target)&&em.HasComponent<LocalTransform>(target))
            {
                var goal=em.GetComponentData<LocalTransform>(target).Position;position=goal;
                if(combat&&em.HasComponent<UnitAttack>(actor)){var p=em.GetComponentData<LocalTransform>(actor).Position;float range=math.max(3,em.GetComponentData<UnitAttack>(actor).Range-8);float2 dir=math.normalizesafe(p.xz-goal.xz,new float2(-1,0));position=new float3(goal.x+dir.x*range,p.y,goal.z+dir.y*range);if(math.distancesq(p.xz,goal.xz)<=math.square(range+3)){position=goal;kind=AssistantRecommendationKind.Attack;}}
            }
            FixedString64Bytes title="mission.network_collapse.tutorial.";title.Append(stage);title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body="mission.network_collapse.tutorial.";body.Append(stage);body.Append(ExtractionBodySuffix);
            if(combat&&kind==AssistantRecommendationKind.Move)body.Append(NetworkApproachSuffix);
            else if(stage is 1 or 3 or 5&&!combat)body.Append(NetworkReconSuffix);
            if(stage==8&&kind==AssistantRecommendationKind.Logistics)body.Append(NetworkBoardSuffix);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=70000+stage,Version=Next(current.Version),MissionSourceVersion=runtime.Version,Prompt=(CampaignMissionGuidancePromptKind)(116+stage),GuidanceMode=NarrativeGuidanceMode.Full,Active=1,RecommendationKind=kind,TargetKind=AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=target,WorldPosition=position,HasWorldPosition=1,Title=title,Body=body,ActionLabel=RadarAct,CanShow=1,CanExecute=0,Priority=AssistantMessagePriority.High,SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            em.GetBuffer<CampaignMissionGuidanceAcknowledgementRequestElement>(root).Clear();if(!ProjectionEquals(in current,in next)||!current.Body.Equals(next.Body))em.SetComponentData(root,next);return true;
        }
    }
}
