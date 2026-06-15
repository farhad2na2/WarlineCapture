using Unity.Entities;
using UnityEngine;

internal sealed partial class CitizenRefugeeSystem : SystemBase
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

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public static void Reset(CitizenRefugeeSystem system, ref State refugeeState)
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
        CitizenRefugeeSystem system,
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
        CitizenRefugeeSystem system,
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
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
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
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
        CitizenRefugeeSystem system,
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
        CitizenRefugeeSystem system,
        CitizenPopulationStateSystem state,
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
        CitizenPopulationStateSystem state,
        int householdId,
        StoreHouseholdAction storeHousehold)
    {
        ReleaseHouseholdRefugeeAssignmentState(state, householdId, storeHousehold);
    }

    public static int GetAssignedRefugeeOccupancy(
        CitizenRefugeeSystem system,
        CitizenPopulationStateSystem state,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        int refugeeTentBuildingId)
    {
        return system != null
            ? system.GetAssignedRefugeeOccupancy(state, householdRegistrationSystem, refugeeTentBuildingId)
            : GetAssignedRefugeeOccupancyState(state, householdRegistrationSystem, refugeeTentBuildingId);
    }

    public int GetAssignedRefugeeOccupancy(
        CitizenPopulationStateSystem state,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        int refugeeTentBuildingId)
    {
        return GetAssignedRefugeeOccupancyState(state, householdRegistrationSystem, refugeeTentBuildingId);
    }

    public static void UpdateRefugeeUpkeep(
        CitizenRefugeeSystem system,
        ref State refugeeState,
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
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
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
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
        CitizenRefugeeSystem system,
        CitizenPopulationStateSystem state,
        int buildingId,
        out CitizenHouseholdRecordComponent household)
    {
        return system != null
            ? system.TryFindHouseholdByHomeBuildingId(state, buildingId, out household)
            : TryFindHouseholdByHomeBuildingIdState(state, buildingId, out household);
    }

    public bool TryFindHouseholdByHomeBuildingId(
        CitizenPopulationStateSystem state,
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
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        CitizenHouseholdRegistrationSystem householdRegistrationSystem,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        TryGetHouseholdReferenceWorldPositionAction tryGetHouseholdReferenceWorldPosition,
        EstimateTravelSecondsAction estimateTravelSeconds,
        MarkCitizenDeadAction markCitizenDead)
    {
        if (!CitizenHouseholdRegistrationSystem.HasHouseholdData(householdRegistrationSystem, state))
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

    private static void ReleaseHouseholdRefugeeAssignmentState(
        CitizenPopulationStateSystem state,
        int householdId,
        StoreHouseholdAction storeHousehold)
    {
        if (!state.TryGetHousehold(householdId, out CitizenHouseholdRecordComponent household))
            return;

        household.RefugeeTentBuildingId = 0;
        storeHousehold(household);
    }

    private static int GetAssignedRefugeeOccupancyState(
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

            occupied += CitizenHouseholdRegistrationSystem.CountLivingHouseholdRefugees(householdRegistrationSystem, state, household);
        }

        return occupied;
    }

    private static void UpdateRefugeeUpkeepState(
        ref State refugeeState,
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

            int householdRefugees = CitizenHouseholdRegistrationSystem.CountLivingHouseholdRefugees(householdRegistrationSystem, state, household);
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

            int householdRefugees = CitizenHouseholdRegistrationSystem.CountLivingHouseholdRefugees(householdRegistrationSystem, state, household);
            if (householdRefugees <= 0)
                continue;

            markCitizenDead(household.MaleCitizenId, "refugee-upkeep-unpaid");
            markCitizenDead(household.FemaleCitizenId, "refugee-upkeep-unpaid");
            ReleaseHouseholdRefugeeAssignmentState(state, household.HouseholdId, storeHousehold);
        }
    }

    private static bool TryFindHouseholdByHomeBuildingIdState(
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

    private static int FindNearestAvailableRefugeeTent(
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

        int requiredSlots = Mathf.Max(1, CitizenHouseholdRegistrationSystem.CountLivingHouseholdMembers(householdRegistrationSystem, state, household));
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
        citizen.StateStartedAt = UnityEngine.Time.time;
        citizen.StateEndsAt = stateDurationSeconds > 0f ? UnityEngine.Time.time + stateDurationSeconds : 0f;
        citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
    }
}
