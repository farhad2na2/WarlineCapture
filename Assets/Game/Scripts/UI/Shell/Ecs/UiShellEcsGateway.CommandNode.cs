using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiCommandNodeGateway,IUiCommandNodeResultGateway
    {
        private static bool TryCommandNode(out EntityManager em,out Entity root,out CampaignMissionCommandNodeState state)
        {
            state=default;
            if(!TryGetMissionRoot(out em,out root)||!em.HasComponent<CampaignMissionCommandNodeState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);state=em.GetComponentData<CampaignMissionCommandNodeState>(root);
            return runtime.MissionId.Equals(CampaignMissionSequence.CommandNode)&&runtime.Phase==MissionPhaseKind.Engage&&runtime.Outcome==MissionOutcomeKind.None&&CampaignMissionCommandNodeRuleUtility.Matches(in state,in runtime);
        }
        private static int CommandHold(in CampaignMissionCommandNodeState s,int stage)=>stage switch
        {2=>s.IsolationMilliseconds,4=>s.BreachMilliseconds,6=>s.AuditMilliseconds,7=>s.ReleaseMilliseconds,8=>s.ReceivingMilliseconds,_=>0};
        public bool TryReadCommandNode(out UiCommandNodeModel model)
        {
            model=default;if(!TryCommandNode(out var em,out var root,out var m))return false;
            int stage=CampaignMissionCommandNodeRuleUtility.Stage(in m);
            using var surfaceQuery=em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            var markerContext=surfaceQuery.CalculateEntityCount()==1?new MapSurfaceSampler.Context(surfaceQuery.GetSingleton<MapSurfaceComponent>()):default;
            model=new UiCommandNodeModel(stage,CommandHold(in m,stage)/1000,math.max(0,(900000-m.ElapsedMilliseconds+999)/1000),em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).HostileDefeatedCount,(m.ClinicIsolated!=0?1:0)+(m.UtilityIsolated!=0?1:0),
                m.ClinicIsolated!=0,m.UtilityIsolated!=0,m.NodeDisabled!=0,m.Breached!=0,m.QassemDefeated!=0,m.AuditPreserved!=0,m.ReleaseOrdered!=0,m.AuditReleased!=0,m.SpecialistsSafe!=0,CommandMarkerGround(markerContext,m.IsolationClinic),CommandMarkerGround(markerContext,m.IsolationUtility),CommandMarkerGround(markerContext,m.BreachGate),CommandMarkerGround(markerContext,m.CoreAudit),CommandMarkerGround(markerContext,m.AuditRelease),CommandMarkerGround(markerContext,m.SafeReceiving),coverReady:m.CoverReady!=0,coverGate:CommandMarkerGround(markerContext,m.BreachGate+new float3(0,0,20)));return true;
        }
        private static readonly MapSurfaceSampler commandMarkerSampler=new();
        private static float3 CommandMarkerGround(MapSurfaceSampler.Context context,float3 position)
        {
            if(commandMarkerSampler.TrySampleBilinearHeight(context,position,out float height))position.y=height;
            return position;
        }
        public bool TryReadCommandNodeResult(out UiCommandNodeResultModel model)
        {
            model=default;if(!TryGetMissionRoot(out var em,out var root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);if(!runtime.MissionId.Equals(CampaignMissionSequence.CommandNode)||runtime.Outcome==MissionOutcomeKind.None)return false;
            var f=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            model=new UiCommandNodeResultModel(f.CommandNodeNetworkSeparated!=0,f.CommandNodeAuditReleased!=0,f.CommandNodeSpecialistsSafe!=0);return true;
        }
        private static bool ResolveCommandNodeTarget(EntityManager em,Entity root,in CampaignMissionGuidanceProjectionComponent g,out UiMissionTutorialTarget target)
        {
            target=default;if(!em.HasComponent<CampaignMissionCommandNodeState>(root)||!em.Exists(g.SourceEntity)||!em.HasComponent<LocalTransform>(g.SourceEntity))return false;
            var m=em.GetComponentData<CampaignMissionCommandNodeState>(root);int stage=g.GuidanceId-72000;
            float3 position=em.GetComponentData<LocalTransform>(g.SourceEntity).Position,min=position,max=position;
            bool selected=em.HasComponent<SelectedUnitTag>(g.SourceEntity),moving=IsTutorialActorMoving(em,g.SourceEntity);int count=1;
            CommandNodeMemberKind? group=(stage is 1 or 3 or 5 || stage==4 && m.CoverReady==0)?CommandNodeMemberKind.Escort:stage is 6 or 8?CommandNodeMemberKind.Specialist:null;
            if(group.HasValue)
            {
                position=default;min=new float3(float.MaxValue);max=new float3(float.MinValue);selected=true;moving=false;count=0;
                foreach(var member in em.GetBuffer<CampaignMissionCommandNodeMember>(root,true))
                {if(member.Kind!=group.Value||member.Dead!=0||!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
                    var p=em.GetComponentData<LocalTransform>(member.Entity).Position;position+=p;min=math.min(min,p);max=math.max(max,p);count++;
                    selected&=em.HasComponent<SelectedUnitTag>(member.Entity);moving|=IsTutorialActorMoving(em,member.Entity);}
                if(count==0)return false;position/=count;
            }
            bool watch=g.RecommendationKind==AssistantRecommendationKind.Explain||CommandHold(in m,stage)>0;
            var action=watch?UiTutorialBattleAction.Watch:g.RecommendationKind==AssistantRecommendationKind.Attack?UiTutorialBattleAction.Attack:UiTutorialBattleAction.Move;
            target=new UiMissionTutorialTarget(position,g.WorldPosition,!watch&&!selected,moving,count,action,executingAttack:action==UiTutorialBattleAction.Attack&&IsTutorialAttackInProgress(em,g.SourceEntity,g.TargetEntity),areaRadius:6,dragSelection:count>1,selectionMin:min-new float3(3,0,3),selectionMax:max+new float3(3,3,3));return true;
        }
        private static (int Stage,int Hold,int Isolation) cachedCommandNodeStatusStamp;
        private static (int Stage,int Hold,int Isolation) ReadCommandNodeStatusStamp()
        {if(!TryCommandNode(out _,out _,out var m))return(-1,0,0);int stage=CampaignMissionCommandNodeRuleUtility.Stage(in m);return(stage,CommandHold(in m,stage)/1000,(m.ClinicIsolated!=0?1:0)+(m.UtilityIsolated!=0?1:0));}
        private static string AppendCommandNodeStatus(string text)
        {var s=ReadCommandNodeStatusStamp();if(s.Stage<0)return text;if(s.Stage==2)text+="\n"+GameText.Format("mission.command_node.hud.isolation","Services isolated: {0}/2",s.Isolation);if(s.Stage is 2 or 4 or 6 or 7 or 8)text+="\n"+GameText.Format("mission.command_node.hud.hold","Hold position: {0}/6 s",s.Hold);return text;}
    }
}
