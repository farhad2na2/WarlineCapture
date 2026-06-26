using UnityEngine;

internal sealed class CitizenRefugeeCompositionSystemHelper
{
    public delegate CitizenHouseholdRecordComponent StoreHouseholdAction(CitizenHouseholdRecordComponent household);
    public delegate CitizenRecordComponent StoreCitizenAction(CitizenRecordComponent citizen);
    public delegate bool TryGetHouseholdReferenceWorldPositionAction(CitizenHouseholdRecordComponent household, out Vector3 worldPosition);
    public delegate float EstimateTravelSecondsAction(CitizenRecordComponent citizen, int targetBuildingId);
    public delegate bool MarkCitizenDeadAction(int citizenId, string reason);

    public struct State
    {
        public int LastRefugeeUpkeepChargedDay;
    }

    private int _lastRefugeeUpkeepChargedDay;

    public static void Reset(CitizenRefugeeCompositionSystemHelper system, ref State refugeeState)
    {
        if (system != null)
        {
            system.Reset();
            return;
        }

        ResetState(ref refugeeState);
    }

    public void Reset()
    {
        _lastRefugeeUpkeepChargedDay = 0;
    }

    public static void NotifyHomeBuildingDestroyed(
        CitizenRefugeeCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        int buildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (system != null)
        {
            system.NotifyHomeBuildingDestroyed(
                state,
                buildingReadSystem,
                householdRegistrationSystem,
                buildingId,
                storeHousehold,
                storeCitizen,
                tryGetHouseholdReferenceWorldPosition,
                estimateTravelSeconds,
                markCitizenDead);
            return;
        }

        NotifyHomeBuildingDestroyedState(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            buildingId,
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public void NotifyHomeBuildingDestroyed(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        int buildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        NotifyHomeBuildingDestroyedState(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            buildingId,
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public static void UpdateRefugeeTentState(
        CitizenRefugeeCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (system != null)
        {
            system.UpdateRefugeeTentState(
                state,
                buildingReadSystem,
                householdRegistrationSystem,
                storeHousehold,
                storeCitizen,
                tryGetHouseholdReferenceWorldPosition,
                estimateTravelSeconds,
                markCitizenDead);
            return;
        }

        UpdateRefugeeTentStateState(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public void UpdateRefugeeTentState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        UpdateRefugeeTentStateState(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public static void DisplaceHousehold(
        CitizenRefugeeCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenHouseholdRecordComponent household,
        string reason,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (system != null)
        {
            system.DisplaceHousehold(
                state,
                buildingReadSystem,
                householdRegistrationSystem,
                household,
                reason,
                storeHousehold,
                storeCitizen,
                tryGetHouseholdReferenceWorldPosition,
                estimateTravelSeconds,
                markCitizenDead);
            return;
        }

        DisplaceHouseholdState(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            household,
            reason,
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public void DisplaceHousehold(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenHouseholdRecordComponent household,
        string reason,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        DisplaceHouseholdState(
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            household,
            reason,
            storeHousehold,
            storeCitizen,
            tryGetHouseholdReferenceWorldPosition,
            estimateTravelSeconds,
            markCitizenDead);
    }

    public static void ReleaseHouseholdRefugeeAssignment(
        CitizenRefugeeCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        int householdId,
        StoreHouseholdAction storeHousehold)
    {
        if (system != null)
        {
            system.ReleaseHouseholdRefugeeAssignment(state, householdId, storeHousehold);
            return;
        }

        ReleaseHouseholdRefugeeAssignmentState(state, householdId, storeHousehold);
    }

    public void ReleaseHouseholdRefugeeAssignment(
        CitizenPopulationStateCompositionSystemHelper state,
        int householdId,
        StoreHouseholdAction storeHousehold)
    {
        ReleaseHouseholdRefugeeAssignmentState(state, householdId, storeHousehold);
    }

    public static int GetAssignedRefugeeOccupancy(
        CitizenRefugeeCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        int refugeeTentBuildingId)
    {
        return system != null
            ? system.GetAssignedRefugeeOccupancy(state, householdRegistrationSystem, refugeeTentBuildingId)
            : GetAssignedRefugeeOccupancyState(state, householdRegistrationSystem, refugeeTentBuildingId);
    }

    public int GetAssignedRefugeeOccupancy(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        int refugeeTentBuildingId)
    {
        return GetAssignedRefugeeOccupancyState(state, householdRegistrationSystem, refugeeTentBuildingId);
    }

    public static void UpdateRefugeeUpkeep(
        CitizenRefugeeCompositionSystemHelper system,
        ref State refugeeState,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenResourceSystem citizenResourceSystem,
        CitizenResourceSystem.Context citizenResourceContext,
        DayNightSystem dayNightSystem,
        MarkCitizenDeadAction markCitizenDead,
        StoreHouseholdAction storeHousehold)
    {
        if (system != null)
        {
            system.UpdateRefugeeUpkeep(
                state,
                buildingReadSystem,
                householdRegistrationSystem,
                citizenResourceSystem,
                citizenResourceContext,
                dayNightSystem,
                markCitizenDead,
                storeHousehold);
            return;
        }

        UpdateRefugeeUpkeepState(
            ref refugeeState,
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            citizenResourceSystem,
            citizenResourceContext,
            dayNightSystem,
            markCitizenDead,
            storeHousehold);
    }

    public void UpdateRefugeeUpkeep(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenResourceSystem citizenResourceSystem,
        CitizenResourceSystem.Context citizenResourceContext,
        DayNightSystem dayNightSystem,
        MarkCitizenDeadAction markCitizenDead,
        StoreHouseholdAction storeHousehold)
    {
        State refugeeState = new()
        {
            LastRefugeeUpkeepChargedDay = _lastRefugeeUpkeepChargedDay
        };
        UpdateRefugeeUpkeepState(
            ref refugeeState,
            state,
            buildingReadSystem,
            householdRegistrationSystem,
            citizenResourceSystem,
            citizenResourceContext,
            dayNightSystem,
            markCitizenDead,
            storeHousehold);
        _lastRefugeeUpkeepChargedDay = refugeeState.LastRefugeeUpkeepChargedDay;
    }

    public static bool TryFindHouseholdByHomeBuildingId(
        CitizenRefugeeCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        int buildingId,
        out CitizenHouseholdRecordComponent household)
    {
        return system != null
            ? system.TryFindHouseholdByHomeBuildingId(state, buildingId, out household)
            : TryFindHouseholdByHomeBuildingIdState(state, buildingId, out household);
    }

    public bool TryFindHouseholdByHomeBuildingId(
        CitizenPopulationStateCompositionSystemHelper state,
        int buildingId,
        out CitizenHouseholdRecordComponent household)
    {
        return TryFindHouseholdByHomeBuildingIdState(state, buildingId, out household);
    }

    private static void ResetState(ref State refugeeState)
    {
        refugeeState.LastRefugeeUpkeepChargedDay = 0;
    }

    private static void NotifyHomeBuildingDestroyedState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        int buildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (!TryFindHouseholdByHomeBuildingIdState(state, buildingId, out CitizenHouseholdRecordComponent household))
            return;
        if (household.IsDisplaced != 0)
            return;

        DisplaceHouseholdState(
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

    private static void UpdateRefugeeTentStateState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (!CitizenHouseholdRegistrationCompositionSystemHelper.HasHouseholdData(householdRegistrationSystem, state))
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
            ReleaseHouseholdRefugeeAssignmentState(state, household.HouseholdId, storeHousehold);
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

    private static void DisplaceHouseholdState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
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

    private static void ReleaseHouseholdRefugeeAssignmentState(
        CitizenPopulationStateCompositionSystemHelper state,
        int householdId,
        StoreHouseholdAction storeHousehold)
    {
        if (!state.TryGetHousehold(householdId, out CitizenHouseholdRecordComponent household))
            return;

        household.RefugeeTentBuildingId = 0;
        storeHousehold(household);
    }

    private static int GetAssignedRefugeeOccupancyState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
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

            occupied += CitizenHouseholdRegistrationCompositionSystemHelper.CountLivingHouseholdRefugees(householdRegistrationSystem, state, household);
        }

        return occupied;
    }

    private static void UpdateRefugeeUpkeepState(
        ref State refugeeState,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenResourceSystem citizenResourceSystem,
        CitizenResourceSystem.Context citizenResourceContext,
        DayNightSystem dayNightSystem,
        MarkCitizenDeadAction markCitizenDead,
        StoreHouseholdAction storeHousehold)
    {
        if (dayNightSystem == null ||
            !CitizenResourceSystem.IsConfigured(citizenResourceSystem, citizenResourceContext) ||
            !buildingReadSystem.HasRuntimeBuildingQuery())
        {
            return;
        }

        int currentDay = Mathf.Max(1, dayNightSystem.DayCount);
        if (currentDay == refugeeState.LastRefugeeUpkeepChargedDay)
            return;

        refugeeState.LastRefugeeUpkeepChargedDay = currentDay;

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

            int householdRefugees = CitizenHouseholdRegistrationCompositionSystemHelper.CountLivingHouseholdRefugees(householdRegistrationSystem, state, household);
            if (householdRefugees <= 0)
                continue;

            refugeeCitizens += householdRefugees;
            totalCost += householdRefugees * upkeepPerCitizenPerDay;
        }

        if (refugeeCitizens <= 0 || totalCost <= 0)
            return;

        if (CitizenResourceSystem.TrySpendDollars(citizenResourceSystem, citizenResourceContext, totalCost))
            return;

        state.PopulateHouseholdIds();

        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;

            int householdRefugees = CitizenHouseholdRegistrationCompositionSystemHelper.CountLivingHouseholdRefugees(householdRegistrationSystem, state, household);
            if (householdRefugees <= 0)
                continue;

            markCitizenDead(household.MaleCitizenId, "refugee-upkeep-unpaid");
            markCitizenDead(household.FemaleCitizenId, "refugee-upkeep-unpaid");
            ReleaseHouseholdRefugeeAssignmentState(state, household.HouseholdId, storeHousehold);
        }
    }

    private static bool TryFindHouseholdByHomeBuildingIdState(
        CitizenPopulationStateCompositionSystemHelper state,
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

    private static int FindNearestAvailableRefugeeTent(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenHouseholdRecordComponent household,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition)
    {
        if (!buildingReadSystem.HasRuntimeBuildingQuery() || buildingReadSystem.RefugeeTentBuildingIds.Count == 0)
            return 0;

        if (!tryGetHouseholdReferenceWorldPosition(household, out Vector3 originPosition))
            return 0;

        int requiredSlots = Mathf.Max(1, CitizenHouseholdRegistrationCompositionSystemHelper.CountLivingHouseholdMembers(householdRegistrationSystem, state, household));
        int bestBuildingId = 0;
        float bestDistanceSq = float.MaxValue;
        for (int i = 0; i < buildingReadSystem.RefugeeTentBuildingIds.Count; i++)
        {
            int candidateBuildingId = buildingReadSystem.RefugeeTentBuildingIds[i];
            if (!buildingReadSystem.TryGetRuntimeBuildingRefugeeSettings(candidateBuildingId, out int refugeeCapacity, out _))
                continue;
            if (refugeeCapacity <= 0)
                continue;

            int occupiedSlots = GetAssignedRefugeeOccupancyState(state, householdRegistrationSystem, candidateBuildingId);
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

    private static void MoveCitizenToRefugeeState(
        CitizenPopulationStateCompositionSystemHelper state,
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
        citizen.StateStartedAt = UnityEngine.Time.time;
        citizen.StateEndsAt = stateDurationSeconds > 0f ? UnityEngine.Time.time + stateDurationSeconds : 0f;
        citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
    }
}
