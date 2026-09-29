using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    /// <summary>The authored finite reserve, never inherited demo-map stock.</summary>
    public static class CampaignSplitFrontFuelScope
    {
        public static bool TryGet(EntityManager em, out Entity reserve, out CampaignMissionRuntimeComponent runtime)
        {
            reserve=Entity.Null;runtime=default;
            using var missions=em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            if(missions.CalculateEntityCount()!=1)return false;
            var root=missions.GetSingletonEntity();runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(runtime.Phase==MissionPhaseKind.None || runtime.MissionId.ToString()!=CampaignMissionSequence.SplitFront)return false;
            if(em.HasComponent<CampaignMissionSplitFrontFuelState>(root))
            {
                var state=em.GetComponentData<CampaignMissionSplitFrontFuelState>(root);
                if(state.SessionToken.Equals(runtime.SessionToken)&&state.AttemptOrdinal==runtime.AttemptOrdinal&&state.Initialized!=0)
                    reserve=state.FuelReserve;
            }
            return true;
        }
        public static float Usable(EntityManager em, Entity reserve)
        {
            if(!em.Exists(reserve)||!em.HasComponent<BuildingResourceStorageComponent>(reserve)||!em.HasComponent<UnitHealth>(reserve)||em.GetComponentData<UnitHealth>(reserve).Current<=0)return 0;
            var storage=em.GetComponentData<BuildingResourceStorageComponent>(reserve);
            return math.max(0,storage.StoredFuelBarrels-storage.ReservedFuelOutboundBarrels-storage.CivilianFuelReserveBarrels);
        }
    }
}
