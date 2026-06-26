using Unity.Entities;

internal sealed class CitizenPopulationDebugDiagnosticsSystemHelper
{
    public delegate bool KillCitizenAction(int citizenId, string reason);

    public bool TryGetCitizenDebugSnapshot(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        int citizenId,
        out string snapshot)
    {
        snapshot = string.Empty;
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        if (ecsProjection.HasWorld &&
            citizen.CitizenEntity != Entity.Null &&
            ecsProjection.EntityManager.Exists(citizen.CitizenEntity) &&
            ecsProjection.EntityManager.HasComponent<CitizenIdentity>(citizen.CitizenEntity) &&
            ecsProjection.EntityManager.HasComponent<CitizenHouseholdRef>(citizen.CitizenEntity) &&
            ecsProjection.EntityManager.HasComponent<CitizenHomeTarget>(citizen.CitizenEntity) &&
            ecsProjection.EntityManager.HasComponent<CitizenAssignmentsComponent>(citizen.CitizenEntity) &&
            ecsProjection.EntityManager.HasComponent<CitizenTimersComponent>(citizen.CitizenEntity))
        {
            CitizenIdentity identity = ecsProjection.EntityManager.GetComponentData<CitizenIdentity>(citizen.CitizenEntity);
            CitizenHouseholdRef householdRef = ecsProjection.EntityManager.GetComponentData<CitizenHouseholdRef>(citizen.CitizenEntity);
            CitizenHomeTarget homeTarget = ecsProjection.EntityManager.GetComponentData<CitizenHomeTarget>(citizen.CitizenEntity);
            CitizenAssignmentsComponent assignments = ecsProjection.EntityManager.GetComponentData<CitizenAssignmentsComponent>(citizen.CitizenEntity);
            CitizenTimersComponent timers = ecsProjection.EntityManager.GetComponentData<CitizenTimersComponent>(citizen.CitizenEntity);

            snapshot =
                $"citizen={identity.CitizenId} household={householdRef.HouseholdId} gender={(CitizenGender)identity.Gender} " +
                $"life={(CitizenLifeState)identity.LifeState} status={(CitizenStatus)identity.Status} home={homeTarget.HomeBuildingId} " +
                $"work={assignments.WorkBuildingId} shop={assignments.PreferredShopBuildingId} lunchShop={assignments.LunchShopBuildingId} walk={assignments.PreferredWalkBuildingId} cityHall={assignments.PreferredCityHallBuildingId} " +
                $"target={homeTarget.CurrentTargetBuildingId} " +
                $"stateStartedAt={timers.StateStartedAt:0.00} stateEndsAt={timers.StateEndsAt:0.00} ecs=1";
            return true;
        }

        snapshot =
            $"citizen={citizen.CitizenId} household={citizen.HouseholdId} gender={citizen.Gender} " +
            $"life={citizen.LifeState} status={citizen.Status} home={citizen.HomeBuildingId} " +
            $"work={citizen.WorkBuildingId} shop={citizen.PreferredShopBuildingId} lunchShop={citizen.LunchShopBuildingId} walk={citizen.PreferredWalkBuildingId} cityHall={citizen.PreferredCityHallBuildingId} " +
            $"target={citizen.CurrentTargetBuildingId} " +
            $"stateStartedAt={citizen.StateStartedAt:0.00} stateEndsAt={citizen.StateEndsAt:0.00} ecs=0";
        return true;
    }

    public bool TrySetCitizenStatusForDebug(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenStatusTransitionSystem statusTransitionSystem,
        int citizenId,
        CitizenStatus status,
        int targetBuildingId,
        float stateDurationSeconds,
        float now,
        CitizenStatusTransitionSystem.StoreCitizenAction storeCitizen)
    {
        if (!state.TryGetCitizen(citizenId, out _))
            return false;

        return CitizenStatusTransitionSystem.TrySetCitizenStatus(
            statusTransitionSystem,
            state,
            citizenId,
            status,
            targetBuildingId,
            stateDurationSeconds,
            now,
            storeCitizen);
    }

    public bool TryKillCitizenForDebug(int citizenId, KillCitizenAction killCitizen)
    {
        return killCitizen != null && killCitizen(citizenId, "debug");
    }
}
