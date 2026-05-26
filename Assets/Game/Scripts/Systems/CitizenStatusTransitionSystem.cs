internal sealed class CitizenStatusTransitionSystem
{
    public delegate CitizenRecordComponent StoreCitizenAction(CitizenRecordComponent citizen);

    public void SetCitizenStatus(
        ref CitizenRecordComponent citizen,
        CitizenStatus status,
        int targetBuildingId,
        float stateDurationSeconds,
        float now)
    {
        citizen.Status = status;
        citizen.CurrentTargetBuildingId = targetBuildingId != 0 ? targetBuildingId : citizen.HomeBuildingId;
        citizen.StateStartedAt = now;
        citizen.StateEndsAt = stateDurationSeconds > 0f ? now + stateDurationSeconds : 0f;
        citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
    }

    public bool IsTravelStatus(CitizenStatus status)
    {
        return status == CitizenStatus.GoingToWork ||
               status == CitizenStatus.GoingToShop ||
               status == CitizenStatus.GoingToCityHall ||
               status == CitizenStatus.GoingForWalk ||
               status == CitizenStatus.ReturningHome ||
               status == CitizenStatus.Fleeing ||
               status == CitizenStatus.RefugeeSeekingShelter ||
               status == CitizenStatus.RelocatingToNewHouse;
    }

    public bool ShouldUseTravelStatus(
        CitizenPopulationStateSystem state,
        CitizenRecordComponent citizen,
        CitizenStatus desiredStatus,
        int desiredTargetBuildingId)
    {
        if (!state.VisibleCitizensById.ContainsKey(citizen.CitizenId))
            return false;

        CitizenStatus settledStatus = GetSettledStatus(citizen.Status);
        return settledStatus != desiredStatus || citizen.CurrentTargetBuildingId != desiredTargetBuildingId;
    }

    public CitizenStatus GetTravelStatusForDesiredStatus(CitizenStatus desiredStatus)
    {
        return desiredStatus switch
        {
            CitizenStatus.AtWork => CitizenStatus.GoingToWork,
            CitizenStatus.AtShop => CitizenStatus.GoingToShop,
            CitizenStatus.GoingToCityHall => CitizenStatus.GoingToCityHall,
            CitizenStatus.AtHome => CitizenStatus.ReturningHome,
            CitizenStatus.AtRefugeeTent => CitizenStatus.RefugeeSeekingShelter,
            CitizenStatus.GoingForWalk => CitizenStatus.GoingForWalk,
            CitizenStatus.Fleeing => CitizenStatus.Fleeing,
            _ => desiredStatus
        };
    }

    public CitizenStatus GetSettledStatus(CitizenStatus status)
    {
        return status switch
        {
            CitizenStatus.GoingToWork => CitizenStatus.AtWork,
            CitizenStatus.GoingToShop => CitizenStatus.AtShop,
            CitizenStatus.ReturningHome => CitizenStatus.AtHome,
            CitizenStatus.Fleeing => CitizenStatus.AtHome,
            CitizenStatus.RefugeeSeekingShelter => CitizenStatus.AtRefugeeTent,
            CitizenStatus.RelocatingToNewHouse => CitizenStatus.AtHome,
            _ => status
        };
    }

    public bool TrySetCitizenStatus(
        CitizenPopulationStateSystem state,
        int citizenId,
        CitizenStatus status,
        int targetBuildingId,
        float stateDurationSeconds,
        float now,
        StoreCitizenAction storeCitizen)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        SetCitizenStatus(ref citizen, status, targetBuildingId, stateDurationSeconds, now);
        storeCitizen(citizen);
        return true;
    }

    public bool TryResolveCitizenArrival(
        CitizenPopulationStateSystem state,
        int citizenId,
        float now,
        StoreCitizenAction storeCitizen)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        CitizenStatus settledStatus = GetSettledStatus(citizen.Status);
        if (settledStatus == citizen.Status)
            return false;

        SetCitizenStatus(ref citizen, settledStatus, citizen.CurrentTargetBuildingId, 0f, now);
        storeCitizen(citizen);
        return true;
    }

    public bool TryMarkCitizenDead(
        CitizenPopulationStateSystem state,
        int citizenId,
        string reason,
        float now,
        StoreCitizenAction storeCitizen)
    {
        _ = reason;
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return false;

        SetCitizenStatus(ref citizen, CitizenStatus.Dead, citizen.CurrentTargetBuildingId, 0f, now);
        storeCitizen(citizen);
        return true;
    }
}
