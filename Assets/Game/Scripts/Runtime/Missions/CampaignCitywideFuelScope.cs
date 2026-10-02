using Game.Components;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public static class CampaignCitywideFuelScope
    {
        public static bool TryGet(EntityManager em,out Entity clinic,out Entity utility)
        {
            clinic=utility=Entity.Null;using var query=em.CreateEntityQuery(typeof(CampaignMissionRuntimeComponent));
            if (query.CalculateEntityCount()!=1) return false;
            var root=query.GetSingletonEntity();var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (runtime.Phase==MissionPhaseKind.None || runtime.MissionId.ToString()!=CampaignMissionSequence.CitywideAlert) return false;
            if (em.HasComponent<CampaignMissionCitywideAlertState>(root))
            {
                var mission=em.GetComponentData<CampaignMissionCitywideAlertState>(root);
                if (CampaignMissionCitywideAlertRuleUtility.Matches(in mission,in runtime)) { clinic=mission.ClinicReserve;utility=mission.UtilityReserve; }
            }
            return true;
        }
        public static bool IsStorage(EntityManager em,Entity candidate) => TryGet(em,out var clinic,out var utility)&&(candidate==clinic||candidate==utility)&&candidate!=Entity.Null&&em.Exists(candidate)&&em.HasComponent<UnitHealth>(candidate)&&em.GetComponentData<UnitHealth>(candidate).Current>0;
        public static float Available(EntityManager em,Entity storage)
        {
            if (!em.Exists(storage)||!em.HasComponent<BuildingResourceStorageComponent>(storage)||!em.HasComponent<UnitHealth>(storage)||em.GetComponentData<UnitHealth>(storage).Current<=0) return 0;
            return SupportFuelTransactionSystem.Usable(em.GetComponentData<BuildingResourceStorageComponent>(storage),1);
        }
        public static float Total(EntityManager em)
        {
            return TryGet(em,out var clinic,out var utility)?Available(em,clinic)+(utility==clinic?0:Available(em,utility)):0;
        }
        public static void Drain(EntityManager em,float requested)
        {
            if (!TryGet(em,out var clinic,out var utility)) return;
            float amount=math.max(0,requested);amount-=DrainOne(em,clinic,amount);if (utility!=clinic) DrainOne(em,utility,amount);
        }
        private static float DrainOne(EntityManager em,Entity entity,float amount)
        {
            float used=math.min(Available(em,entity),amount);if (used<=0) return 0;
            var storage=em.GetComponentData<BuildingResourceStorageComponent>(entity);storage.StoredFuelBarrels-=used;storage.Version++;em.SetComponentData(entity,storage);return used;
        }
    }
}
