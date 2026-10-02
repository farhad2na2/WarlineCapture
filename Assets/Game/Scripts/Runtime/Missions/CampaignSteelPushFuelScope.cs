using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>The authored finite reserve, never inherited demo-map stock.</summary>
    public static class CampaignSteelPushFuelScope
    {
        public static bool TryGet(EntityManager em, out Entity reserve, out CampaignMissionRuntimeComponent runtime)
        {
            reserve=Entity.Null;runtime=default;
            using var missions=em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            if(missions.CalculateEntityCount()!=1)return false;
            var root=missions.GetSingletonEntity();runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(runtime.Phase==MissionPhaseKind.None)return false;
            if(runtime.MissionId.ToString()==CampaignMissionSequence.NetworkCollapse)
            { if(em.HasComponent<CampaignMissionNetworkCollapseState>(root)){var mission=em.GetComponentData<CampaignMissionNetworkCollapseState>(root);if(CampaignMissionNetworkCollapseRuleUtility.Matches(in mission,in runtime))reserve=mission.Reserve;}return true; }
            if(runtime.MissionId.ToString()==CampaignMissionSequence.TrustUnderFire)
            {
                if(em.HasComponent<CampaignMissionTrustUnderFireState>(root))
                { var trust=em.GetComponentData<CampaignMissionTrustUnderFireState>(root); if(CampaignMissionTrustUnderFireRuleUtility.Matches(in trust,in runtime))reserve=trust.Reserve; }
                return true;
            }
            if(runtime.MissionId.ToString()==CampaignMissionSequence.CitywideAlert)
            { CampaignCitywideFuelScope.TryGet(em,out reserve,out _);return true; }
            if(runtime.MissionId.ToString()==CampaignMissionSequence.ArmorBreak)
            {
                if(em.HasComponent<CampaignMissionArmorBreakState>(root))
                {
                    var armor=em.GetComponentData<CampaignMissionArmorBreakState>(root);
                    if(CampaignMissionArmorBreakRuleUtility.Matches(in armor,in runtime))reserve=armor.FuelReserve;
                }
                return true;
            }
            if(runtime.MissionId.ToString()!=CampaignMissionSequence.SteelPush)return false;
            if(em.HasComponent<CampaignMissionSteelPushState>(root))
            {
                var state=em.GetComponentData<CampaignMissionSteelPushState>(root);
                if(state.SessionToken.Equals(runtime.SessionToken)&&state.AttemptOrdinal==runtime.AttemptOrdinal&&state.Initialized!=0)
                    reserve=state.FuelReserve;
            }
            return true;
        }
        public static float Usable(EntityManager em, Entity reserve)
        {
            if(CampaignCitywideFuelScope.TryGet(em,out _,out _))return CampaignCitywideFuelScope.Total(em);
            if(!em.Exists(reserve)||!em.HasComponent<BuildingResourceStorageComponent>(reserve))return 0;
            var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve);
            return math.max(0,storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels-storage.CivilianFuelReserveBarrels);
        }
        public static void Drain(EntityManager em, Entity reserve, float requested)
        {
            if(CampaignCitywideFuelScope.TryGet(em,out _,out _)){CampaignCitywideFuelScope.Drain(em,requested);return;}
            float amount=math.min(Usable(em,reserve),math.max(0,requested));
            if(amount<=0)return;
            var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve);
            storage.StoredFuelBarrels-=amount;storage.Version++;em.SetComponentData(reserve,storage);
        }
    }
}
