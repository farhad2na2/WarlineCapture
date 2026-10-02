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
    public sealed partial class UiShellEcsGateway : IUiLastCorridorGateway,IUiLastCorridorResultGateway
    {
        private static bool TryLastCorridor(out EntityManager em,out Entity root,out CampaignMissionLastCorridorState state)
        {
            state=default;
            if(!TryGetMissionRoot(out em,out root)||!em.HasComponent<CampaignMissionLastCorridorState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);state=em.GetComponentData<CampaignMissionLastCorridorState>(root);
            return runtime.MissionId.Equals(CampaignMissionSequence.LastCorridor)&&runtime.Phase==MissionPhaseKind.Engage&&runtime.Outcome==MissionOutcomeKind.None&&CampaignMissionLastCorridorRuleUtility.Matches(in state,in runtime);
        }
        private static int CorridorHold(in CampaignMissionLastCorridorState state,int stage)=>stage switch
        {2=>state.RepairMilliseconds,3=>state.MedicineMilliseconds,4=>state.FuelMilliseconds,5=>state.ReinforcementMilliseconds,8=>state.KeyMilliseconds,_=>0};
        public bool TryReadLastCorridor(out UiLastCorridorModel model)
        {
            model=default;if(!TryLastCorridor(out var em,out var root,out var m))return false;
            int stage=CampaignMissionLastCorridorRuleUtility.Stage(in m);
            int delivered=(m.MedicineDelivered!=0?1:0)+(m.FuelDelivered!=0?1:0)+(m.ReinforcementsDelivered!=0?1:0)+(m.EngineerDelivered!=0?1:0)+(m.KeysDelivered!=0?1:0);
            model=new UiLastCorridorModel(stage,delivered,CorridorHold(in m,stage)/1000,math.max(0,(900000-m.ElapsedMilliseconds+999)/1000),em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).HostileDefeatedCount,stage==3?m.MedicineRouteStep:stage==4?m.FuelRouteStep:stage==8?m.KeyRouteStep:0,m.LinkRecovered!=0,m.MedicineDelivered!=0,m.FuelDelivered!=0,m.ReinforcementsDelivered!=0,m.EngineerAboard!=0,m.EngineerDelivered!=0,m.KeysDelivered!=0,m.RepairGate,m.MedicineGate,m.FuelGate,m.ReinforcementGate,m.KeyReceiver);return true;
        }
        public bool TryReadLastCorridorResult(out UiLastCorridorResultModel model)
        {
            model=default;if(!TryGetMissionRoot(out var em,out var root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(!runtime.MissionId.Equals(CampaignMissionSequence.LastCorridor)||runtime.Outcome==MissionOutcomeKind.None)return false;
            var f=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            model=new UiLastCorridorResultModel(f.CorridorLinkRecovered!=0,f.CorridorSuppliesDelivered==3&&f.CorridorEngineerDelivered!=0&&f.CorridorKeysDelivered!=0,f.CorridorEngineerDelivered!=0&&f.CorridorKeysDelivered!=0);return true;
        }
        private static bool ResolveLastCorridorTarget(EntityManager em,Entity root,in CampaignMissionGuidanceProjectionComponent g,out UiMissionTutorialTarget target)
        {
            target=default;if(!em.HasComponent<CampaignMissionLastCorridorState>(root)||!em.Exists(g.SourceEntity)||!em.HasComponent<LocalTransform>(g.SourceEntity))return false;
            var m=em.GetComponentData<CampaignMissionLastCorridorState>(root);int stage=g.GuidanceId-71000;
            float3 position=em.GetComponentData<LocalTransform>(g.SourceEntity).Position,min=position,max=position;
            bool selected=em.HasComponent<SelectedUnitTag>(g.SourceEntity),moving=IsTutorialActorMoving(em,g.SourceEntity);int count=1;
            if(stage is 1 or 5)
            {
                var kind=stage==1?LastCorridorMemberKind.Escort:LastCorridorMemberKind.Reinforcement;
                position=default;min=new float3(float.MaxValue);max=new float3(float.MinValue);selected=true;moving=false;count=0;
                foreach(var member in em.GetBuffer<CampaignMissionLastCorridorMember>(root,true))
                {if(member.Kind!=kind||member.Dead!=0||!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
                    var p=em.GetComponentData<LocalTransform>(member.Entity).Position;position+=p;min=math.min(min,p);max=math.max(max,p);count++;
                    selected&=em.HasComponent<SelectedUnitTag>(member.Entity);moving|=IsTutorialActorMoving(em,member.Entity);}
                if(count==0)return false;position/=count;
            }
            bool watch=g.RecommendationKind==AssistantRecommendationKind.Explain||CorridorHold(in m,stage)>0;
            var action=watch?UiTutorialBattleAction.Watch:g.RecommendationKind==AssistantRecommendationKind.Attack?UiTutorialBattleAction.Attack:g.RecommendationKind==AssistantRecommendationKind.Logistics?UiTutorialBattleAction.None:UiTutorialBattleAction.Move;
            target=new UiMissionTutorialTarget(position,g.WorldPosition,!watch&&!selected,moving,count,action,executingAttack:action==UiTutorialBattleAction.Attack&&IsTutorialAttackInProgress(em,g.SourceEntity,g.TargetEntity),areaRadius:6,dragSelection:count>1,selectionMin:min-new float3(3,0,3),selectionMax:max+new float3(3,3,3));return true;
        }
        private static (int Stage,int Hold,int Deliveries) cachedLastCorridorStatusStamp;
        private static (int Stage,int Hold,int Deliveries) ReadLastCorridorStatusStamp()
        {if(!TryLastCorridor(out _,out _,out var m))return(-1,0,0);int stage=CampaignMissionLastCorridorRuleUtility.Stage(in m);return(stage,CorridorHold(in m,stage)/1000,(m.MedicineDelivered!=0?1:0)+(m.FuelDelivered!=0?1:0)+(m.ReinforcementsDelivered!=0?1:0)+(m.EngineerDelivered!=0?1:0)+(m.KeysDelivered!=0?1:0));}
        private static string AppendLastCorridorStatus(string text)
        {var s=ReadLastCorridorStatusStamp();if(s.Stage<0)return text;text+="\n"+GameText.Format("mission.last_corridor.hud.delivered","Delivered: {0}/5",s.Deliveries);if(s.Stage is 2 or 3 or 4 or 5 or 8)text+="\n"+GameText.Format("mission.last_corridor.hud.hold","Hold position: {0}/6 s",s.Hold);return text;}
    }
}
