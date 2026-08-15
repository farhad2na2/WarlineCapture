using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class MissionDayNightPolicyUtility
    {
        internal static void Apply(DayNightSystem dayNight, EntityManager entityManager)
        {
            if (dayNight == null)
                return;

            dayNight.SetRuntimeVisualsEnabled(ShouldEnableDayNightVisuals(entityManager));
        }

        internal static bool ShouldEnableDayNightVisuals(EntityManager entityManager)
        {
            using EntityQuery activeMapQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            if (activeMapQuery.CalculateEntityCount() == 1 &&
                !activeMapQuery.GetSingleton<ActiveOperationMapComponent>().MissionId.IsEmpty)
                return false;

            using EntityQuery missionQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>());
            if (missionQuery.CalculateEntityCount() != 1)
                return true;

            CampaignMissionRuntimeComponent runtime =
                missionQuery.GetSingleton<CampaignMissionRuntimeComponent>();
            return runtime.MissionId.IsEmpty || runtime.SessionToken.IsEmpty;
        }
    }
}
