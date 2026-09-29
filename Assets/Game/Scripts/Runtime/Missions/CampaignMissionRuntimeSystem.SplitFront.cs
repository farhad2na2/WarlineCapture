using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
namespace Game.Runtime
{
    public partial struct CampaignMissionRuntimeSystem
    {
        private static void ProjectSplitFrontProtection(EntityManager em,Entity root,in CampaignMissionRuntimeComponent runtime,ref CampaignMissionAttemptFactsComponent facts)
        {
            var mission=em.HasComponent<CampaignMissionSplitFrontState>(root)?em.GetComponentData<CampaignMissionSplitFrontState>(root):default;
            if(!mission.SessionToken.Equals(runtime.SessionToken)||mission.AttemptOrdinal!=runtime.AttemptOrdinal)
                mission=new CampaignMissionSplitFrontState{SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal};
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
            {
                if(member.Entity==mission.Battery&&member.Defeated!=0)mission.BatteryDestroyed=1;
                if(em.Exists(member.Entity)&&em.HasComponent<SplitFrontLauncherCommandState>(member.Entity))mission.Launcher=member.Entity;
                if(em.Exists(member.Entity)&&em.HasComponent<CampaignMissionUnitRoleComponent>(member.Entity)&&em.GetComponentData<CampaignMissionUnitRoleComponent>(member.Entity).MissionRoleId.Equals(new FixedString64Bytes("role.hostile.battery")))
                {mission.Battery=member.Entity;if(member.Defeated!=0)mission.BatteryDestroyed=1;}
            }
            var defense=em.GetComponentData<CampaignMissionDefenseStateComponent>(root);
            using var boundary=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
            if(boundary.CalculateEntityCount()==1&&defense.InitialProducerRequestId!=0)
            {
                int id=0;foreach(var request in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary.GetSingletonEntity(),true))if(request.RequestId==defense.InitialProducerRequestId&&request.Status==BuildingRuntimeSpawnRequest.Succeeded)id=request.BuildingRuntimeId;
                using var buildings=em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth));using var entities=buildings.ToEntityArray(Allocator.Temp);
                foreach(var entity in entities)if(em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId==id&&id!=0){mission.Base=entity;mission.Initialized=1;}
            }
            if(mission.Initialized!=0)
            {
                if(!em.Exists(mission.Base)||!em.HasComponent<UnitHealth>(mission.Base)){facts.ForwardPostDestroyed=1;}
                else{var health=em.GetComponentData<UnitHealth>(mission.Base);facts.ForwardPostBound=1;if(health.Current<health.Max)facts.ForwardPostDamaged=1;if(health.Current<=0)facts.ForwardPostDestroyed=1;}
            }
            if(em.Exists(mission.Launcher)&&em.HasComponent<SplitFrontLauncherCommandState>(mission.Launcher))
            {var consent=em.GetComponentData<SplitFrontLauncherCommandState>(mission.Launcher);mission.AttackOrders=consent.AttackOrders;mission.StoppedOrders=consent.Stops;mission.LaunchedShots=consent.Launches;}
            if(facts.CivilianLossCount>0||facts.ElapsedMilliseconds>=240000)facts.CoreBreached=1;
            if(em.HasComponent<CampaignMissionSplitFrontState>(root))em.SetComponentData(root,mission);else em.AddComponentData(root,mission);
        }
    }
}
