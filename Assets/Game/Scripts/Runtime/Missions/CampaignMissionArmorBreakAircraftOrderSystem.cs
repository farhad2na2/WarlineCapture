using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitTargetOrderSystem))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct CampaignMissionArmorBreakAircraftOrderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var em=state.EntityManager;
            foreach(var (runtimeRef,missionRef,root) in SystemAPI.Query<RefRO<CampaignMissionRuntimeComponent>,RefRW<CampaignMissionArmorBreakState>>().WithEntityAccess())
            {
                var runtime=runtimeRef.ValueRO;var mission=missionRef.ValueRO;
                if(runtime.MissionId.ToString()!=CampaignMissionSequence.ArmorBreak || runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None ||
                    !CampaignMissionArmorBreakRuleUtility.Matches(in mission,in runtime) || mission.Ready==0 || mission.AircraftAttackOrdered!=0 ||
                    !em.HasBuffer<CampaignMissionArmorBreakMember>(root) || !em.Exists(mission.Aircraft) ||
                    !em.HasComponent<EngageTarget>(mission.Aircraft) || !em.HasComponent<UnitAttackTraceComponent>(mission.Aircraft) ||
                    !em.HasComponent<UnitAttack>(mission.Aircraft) || !em.HasComponent<UnitHealth>(mission.Aircraft) || em.GetComponentData<UnitHealth>(mission.Aircraft).Current<=0 ||
                    !em.HasComponent<CampaignMissionUnitRoleComponent>(mission.Aircraft))continue;
                var actorRole=em.GetComponentData<CampaignMissionUnitRoleComponent>(mission.Aircraft);
                if(!actorRole.SessionToken.Equals(runtime.SessionToken) || actorRole.MissionRoleId.ToString()!="role.friendly.aircraft")continue;
                var order=em.GetComponentData<EngageTarget>(mission.Aircraft);
                if(order.IsCommanded==0 || !em.Exists(order.Target) || !em.HasComponent<UnitHealth>(order.Target) || em.GetComponentData<UnitHealth>(order.Target).Current<=0 ||
                    !em.HasComponent<Faction>(order.Target) || !FactionIdentity.IsHostileToPlayer(em.GetComponentData<Faction>(order.Target).Id) ||
                    !em.HasComponent<CampaignMissionUnitRoleComponent>(order.Target) || em.HasComponent<CampaignMissionCombatSuppressedTag>(order.Target))continue;
                var targetRole=em.GetComponentData<CampaignMissionUnitRoleComponent>(order.Target);
                if(!targetRole.SessionToken.Equals(runtime.SessionToken))continue;
                bool originalActor=false,originalTarget=false;
                foreach(var member in em.GetBuffer<CampaignMissionArmorBreakMember>(root,true))
                {
                    if(member.Entity==mission.Aircraft && member.Kind==ArmorBreakMemberKind.Aircraft && member.Dead==0)originalActor=true;
                    if(member.Entity==order.Target && member.Dead==0 && member.Kind is ArmorBreakMemberKind.HostileArmor or ArmorBreakMemberKind.HostileCommand)originalTarget=true;
                }
                if(!originalActor || !originalTarget)continue;
                // Snapshot before combat. A first shot which destroys the target is still
                // evidence of the accepted normal Attack order in this same attempt.
                mission.AircraftAttackOrdered=1;
                mission.AircraftShotBaseline=em.GetComponentData<UnitAttackTraceComponent>(mission.Aircraft).ShotCounter;
                missionRef.ValueRW=mission;
            }
        }
        public static bool HasObservedShot(EntityManager em,in CampaignMissionArmorBreakState mission) =>
            mission.AircraftAttackOrdered!=0 && em.Exists(mission.Aircraft) && em.HasComponent<UnitAttackTraceComponent>(mission.Aircraft) &&
            em.GetComponentData<UnitAttackTraceComponent>(mission.Aircraft).ShotCounter>mission.AircraftShotBaseline;
    }
}
