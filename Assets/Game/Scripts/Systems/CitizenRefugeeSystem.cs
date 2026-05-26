using UnityEngine;

internal sealed class CitizenRefugeeSystem
{
    public delegate CitizenHouseholdRecordComponent StoreHouseholdAction(CitizenHouseholdRecordComponent household);
    public delegate CitizenRecordComponent StoreCitizenAction(CitizenRecordComponent citizen);
    public delegate bool TryGetHouseholdReferenceWorldPositionAction(CitizenHouseholdRecordComponent household, out Vector3 worldPosition);
    public delegate float EstimateTravelSecondsAction(CitizenRecordComponent citizen, int targetBuildingId);
    public delegate bool MarkCitizenDeadAction(int citizenId, string reason);

    private int _lastRefugeeUpkeepChargedDay;

    public void Reset()
    {
        _lastRefugeeUpkeepChargedDay = 0;
    }

    public void NotifyHomeBuildingDestroyed(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        int buildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (!TryFindHouseholdByHomeBuildingId(state, buildingId, out CitizenHouseholdRecordComponent household))
            return;
        if (household.IsDisplaced != 0)
            return;

        DisplaceHousehold(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            household,
            "home-destroyed",
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public void UpdateRefugeeTentState(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (!householdRegistrationSystem.HasHouseholdData(state))
            return;

        state.PopulateHouseholdIds();
        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;
            if (household.IsDisplaced == 0)
                continue;
            if (household.RefugeeTentBuildingId == 0)
                continue;

            bool tentExists = buildingReadSystem.TryGetRuntimeBuildingDestroyedState(household.RefugeeTentBuildingId, out bool isDestroyed);
            if (tentExists && !isDestroyed)
                continue;

            int previousTentBuildingId = household.RefugeeTentBuildingId;
            ReleaseHouseholdRefugeeAssignment(state, household.HouseholdId, storeHousehold);
            if (!state.TryGetHousehold(household.HouseholdId, out household))
                continue;

            int replacementTentBuildingId = FindNearestAvailableRefugeeTent(state, buildingReadSystem, householdRegistrationSystem, household, tryGetHouseholdReferenceWorldPosition);
            if (replacementTentBuildingId != 0 && replacementTentBuildingId != previousTentBuildingId)
            {
                household.RefugeeTentBuildingId = replacementTentBuildingId;
                household = storeHousehold(household);
                MoveCitizenToRefugeeState(state, household.MaleCitizenId, replacementTentBuildingId, "refugee-tent-lost", storeCitizen, estimateTravelSeconds);
                MoveCitizenToRefugeeState(state, household.FemaleCitizenId, replacementTentBuildingId, "refugee-tent-lost", storeCitizen, estimateTravelSeconds);
                continue;
            }

            markCitizenDead(household.MaleCitizenId, "refugee-tent-destroyed");
            markCitizenDead(household.FemaleCitizenId, "refugee-tent-destroyed");
        }
    }

    public void DisplaceHousehold(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        CitizenHouseholdRecordComponent household,
        string reason,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        int refugeeTentBuildingId = FindNearestAvailableRefugeeTent(state, buildingReadSystem, householdRegistrationSystem, household, tryGetHouseholdReferenceWorldPosition);
        household.IsDisplaced = 1;
        household.RefugeeTentBuildingId = refugeeTentBuildingId;
        household = storeHousehold(household);

        if (refugeeTentBuildingId != 0)
        {
            MoveCitizenToRefugeeState(state, household.MaleCitizenId, refugeeTentBuildingId, reason, storeCitizen, estimateTravelSeconds);
            MoveCitizenToRefugeeState(state, household.FemaleCitizenId, refugeeTentBuildingId, reason, storeCitizen, estimateTravelSeconds);
            return;
        }

        markCitizenDead(household.MaleCitizenId, $"{reason}-no-refugee");
        markCitizenDead(household.FemaleCitizenId, $"{reason}-no-refugee");
    }

    public void ReleaseHouseholdRefugeeAssignment(
        CitizenPopulationStateSystem state,
        int householdId,
        StoreHouseholdAction storeHousehold)
    {
        if (!state.TryGetHousehold(householdId, out CitizenHouseholdRecordComponent household))
            return;

        household.RefugeeTentBuildingId = 0;
        storeHousehold(household);
    }

    public int GetAssignedRefugeeOccupancy(
        CitizenPopulationStateSystem state,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        int refugeeTentBuildingId)
    {
        int occupied = 0;
        state.PopulateHouseholdIds();
        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;
            if (household.RefugeeTentBuildingId != refugeeTentBuildingId)
                continue;

            occupied += householdRegistrationSystem.CountLivingHouseholdRefugees(state, household);
        }

        return occupied;
    }

    public void UpdateRefugeeUpkeep(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        CitizenResourceSystem citizenResourceSystem,
        CitizenResourceSystem.Context citizenResourceContext,
        DayNightSystem dayNightSystem,
        MarkCitizenDeadAction markCitizenDead,
        StoreHouseholdAction storeHousehold)
    {
        if (dayNightSystem == null ||
            !citizenResourceSystem.IsConfigured(citizenResourceContext) ||
            !buildingReadSystem.HasRuntimeBuildingQuery())
        {
            return;
        }

        int currentDay = Mathf.Max(1, dayNightSystem.DayCount);
        if (currentDay == _lastRefugeeUpkeepChargedDay)
            return;

        _lastRefugeeUpkeepChargedDay = currentDay;

        int refugeeCitizens = 0;
        int totalCost = 0;
        state.PopulateHouseholdIds();
        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;
            if (household.RefugeeTentBuildingId == 0)
                continue;

            if (!buildingReadSystem.TryGetRuntimeBuildingRefugeeSettings(household.RefugeeTentBuildingId, out _, out int upkeepPerCitizenPerDay))
                continue;

            int householdRefugees = householdRegistrationSystem.CountLivingHouseholdRefugees(state, household);
            if (householdRefugees <= 0)
                continue;

            refugeeCitizens += householdRefugees;
            totalCost += householdRefugees * upkeepPerCitizenPerDay;
        }

        if (refugeeCitizens <= 0 || totalCost <= 0)
            return;

        if (citizenResourceSystem.TrySpendDollars(citizenResourceContext, totalCost))
            return;

        state.PopulateHouseholdIds();

        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;

            int householdRefugees = householdRegistrationSystem.CountLivingHouseholdRefugees(state, household);
            if (householdRefugees <= 0)
                continue;

            markCitizenDead(household.MaleCitizenId, "refugee-upkeep-unpaid");
            markCitizenDead(household.FemaleCitizenId, "refugee-upkeep-unpaid");
            ReleaseHouseholdRefugeeAssignment(state, household.HouseholdId, storeHousehold);
        }
    }

    public bool TryFindHouseholdByHomeBuildingId(
        CitizenPopulationStateSystem state,
        int buildingId,
        out CitizenHouseholdRecordComponent household)
    {
        household = default;

        if (state.HouseholdIdsByHomeBuildingId.TryGetValue(buildingId, out int mappedHouseholdId) &&
            state.TryGetHousehold(mappedHouseholdId, out household))
        {
            return true;
        }

        state.PopulateHouseholdIds();
        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out household))
                continue;
            if (household.HomeBuildingId != buildingId)
                continue;

            state.HouseholdIdsByHomeBuildingId[buildingId] = household.HouseholdId;
            return true;
        }

        household = default;
        return false;
    }

    private int FindNearestAvailableRefugeeTent(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        CitizenHouseholdRecordComponent household,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition)
    {
        if (!buildingReadSystem.HasRuntimeBuildingQuery() || buildingReadSystem.RefugeeTentBuildingIds.Count == 0)
            return 0;

        if (!tryGetHouseholdReferenceWorldPosition(household, out Vector3 originPosition))
            return 0;

        int requiredSlots = Mathf.Max(1, householdRegistrationSystem.CountLivingHouseholdMembers(state, household));
        int bestBuildingId = 0;
        float bestDistanceSq = float.MaxValue;
        for (int i = 0; i < buildingReadSystem.RefugeeTentBuildingIds.Count; i++)
        {
            int candidateBuildingId = buildingReadSystem.RefugeeTentBuildingIds[i];
            if (!buildingReadSystem.TryGetRuntimeBuildingRefugeeSettings(candidateBuildingId, out int refugeeCapacity, out _))
                continue;
            if (refugeeCapacity <= 0)
                continue;

            int occupiedSlots = GetAssignedRefugeeOccupancy(state, householdRegistrationSystem, candidateBuildingId);
            if (occupiedSlots + requiredSlots > refugeeCapacity)
                continue;
            if (!buildingReadSystem.TryGetRuntimeBuildingFocusWorldPosition(candidateBuildingId, out Vector3 candidatePosition))
                continue;

            float distanceSq = (candidatePosition - originPosition).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestBuildingId = candidateBuildingId;
        }

        return bestBuildingId;
    }

    private void MoveCitizenToRefugeeState(
        CitizenPopulationStateSystem state,
        int citizenId,
        int refugeeTentBuildingId,
        string reason,
        StoreCitizenAction storeCitizen,
        EstimateTravelSecondsAction estimateTravelSeconds)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return;

        SetCitizenStatus(ref citizen, CitizenStatus.RefugeeSeekingShelter, refugeeTentBuildingId, estimateTravelSeconds(citizen, refugeeTentBuildingId));
        storeCitizen(citizen);
    }

    private static void SetCitizenStatus(ref CitizenRecordComponent citizen, CitizenStatus status, int targetBuildingId, float stateDurationSeconds)
    {
        citizen.Status = status;
        citizen.CurrentTargetBuildingId = targetBuildingId != 0 ? targetBuildingId : citizen.HomeBuildingId;
        citizen.StateStartedAt = Time.time;
        citizen.StateEndsAt = stateDurationSeconds > 0f ? Time.time + stateDurationSeconds : 0f;
        citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
    }
}
