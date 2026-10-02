using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiArmorBreakGateway
    {
        private static bool TryArmorBreak(out EntityManager em, out Entity root, out CampaignMissionArmorBreakState mission)
        {
            mission=default;
            if(!TryGetMissionRoot(out em,out root) || !em.HasComponent<CampaignMissionArmorBreakState>(root))return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);mission=em.GetComponentData<CampaignMissionArmorBreakState>(root);
            return runtime.MissionId.Equals(new Unity.Collections.FixedString64Bytes(CampaignMissionSequence.ArmorBreak)) && runtime.Phase==MissionPhaseKind.Engage && runtime.Outcome==MissionOutcomeKind.None &&
                CampaignMissionArmorBreakRuleUtility.Matches(in mission,in runtime);
        }
        public bool TryReadArmorBreak(out UiArmorBreakModel model)
        {
            model=default;if(!TryArmorBreak(out var em,out var root,out var mission))return false;
            float fuel=0;
            if(em.Exists(mission.FuelReserve) && em.HasComponent<BuildingResourceStorageComponent>(mission.FuelReserve))
            {var storage=em.GetComponentData<BuildingResourceStorageComponent>(mission.FuelReserve);fuel=math.max(0,storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels-storage.CivilianFuelReserveBarrels);}
            int cleared=0;
            foreach(var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root,true))
                if(member.Kind==ArmorBreakMemberKind.HostileArmor && member.Dead!=0)cleared++;
            model=new UiArmorBreakModel(CampaignMissionArmorBreakRuleUtility.Stage(in mission),mission.RecoveryMilliseconds/1000,
                math.max(0,(CampaignMissionArmorBreakRuleUtility.DeadlineMilliseconds-mission.ElapsedMilliseconds+999)/1000),fuel,
                mission.CommandDisabled!=0,mission.AuthorityRecovered!=0,mission.ReliefLost!=0,mission.CoverageCenter,mission.AuthorityCenter,mission.ReliefCenter,cleared);
            return true;
        }
        private static bool ResolveArmorBreakTarget(EntityManager em, Entity root, in CampaignMissionGuidanceProjectionComponent guidance, out UiMissionTutorialTarget target)
        {
            target=default;
            if(!em.HasComponent<CampaignMissionArmorBreakState>(root) || !em.Exists(guidance.SourceEntity) || !em.HasComponent<LocalTransform>(guidance.SourceEntity))return false;
            var mission=em.GetComponentData<CampaignMissionArmorBreakState>(root);
            int stage=guidance.GuidanceId-67000;
            float3 selection=em.GetComponentData<LocalTransform>(guidance.SourceEntity).Position,min=selection,max=selection;
            bool selected=em.HasComponent<SelectedUnitTag>(guidance.SourceEntity),moving=IsTutorialActorMoving(em,guidance.SourceEntity);
            int required=1;
            bool aircraftAssault=stage==6 && guidance.SourceEntity==mission.Aircraft;
            if(stage is 5 or 6 or 7 && !aircraftAssault)
            {
                selected=true;moving=false;selection=default;min=new float3(float.MaxValue);max=new float3(float.MinValue);required=0;
                foreach(var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root,true))
                {
                    if(member.Dead!=0 || member.Kind!=(stage==7?ArmorBreakMemberKind.Infantry:ArmorBreakMemberKind.Armor) || !em.Exists(member.Entity) || !em.HasComponent<LocalTransform>(member.Entity))continue;
                    if(stage==6 && !CampaignMissionGuidanceProjectionSystem.IsArmorBreakArmedActor(em,member.Entity))continue;
                    var position=em.GetComponentData<LocalTransform>(member.Entity).Position;
                    selection+=position;min=math.min(min,position);max=math.max(max,position);required++;
                    selected&=em.HasComponent<SelectedUnitTag>(member.Entity);moving|=IsTutorialActorMoving(em,member.Entity);
                }
                if(required==0)return false;selection/=required;
            }
            var action=stage==2 || stage==7 && mission.RecoveryMilliseconds>0?UiTutorialBattleAction.Watch:
                guidance.RecommendationKind==AssistantRecommendationKind.Attack?UiTutorialBattleAction.Attack:UiTutorialBattleAction.Move;
            bool attacking=action==UiTutorialBattleAction.Attack && IsTutorialAttackInProgress(em,guidance.SourceEntity,guidance.TargetEntity);
            if(stage==3 && em.HasComponent<GroundMissileLauncherStateComponent>(guidance.SourceEntity))
                attacking|=em.GetComponentData<GroundMissileLauncherStateComponent>(guidance.SourceEntity).Phase!=(byte)GroundMissileLauncherPhase.Idle;
            target=new UiMissionTutorialTarget(selection,guidance.WorldPosition,!selected,moving,required,action,
                executingAttack:attacking,areaRadius:stage==7?mission.AuthorityRadius:mission.CoverageRadius,
                dragSelection:required>1,selectionMin:min-new float3(3,0,3),selectionMax:max+new float3(3,3,3));
            return true;
        }
    }
}
