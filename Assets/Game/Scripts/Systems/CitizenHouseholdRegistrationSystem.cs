using UnityEngine;

internal sealed class CitizenHouseholdRegistrationSystem
{
    public delegate CitizenHouseholdRecordComponent StoreHouseholdAction(CitizenHouseholdRecordComponent household);
    public delegate CitizenRecordComponent StoreCitizenAction(CitizenRecordComponent citizen);
    public delegate bool TryRehouseDisplacedHouseholdAction(int newHomeBuildingId);
    public delegate void DisplaceHouseholdAction(CitizenHouseholdRecordComponent household, string reason);
    public delegate float EstimateTravelSecondsAction(CitizenRecordComponent citizen, int targetBuildingId);

    public void SyncRemovedHouses(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        DisplaceHouseholdAction displaceHousehold)
    {
        if (state.HouseholdIdsByHomeBuildingId.Count == 0)
            return;

        state.ScratchRemovedBuildingIds.Clear();
        foreach (System.Collections.Generic.KeyValuePair<int, int> pair in state.HouseholdIdsByHomeBuildingId)
        {
            if (buildingReadSystem.IsRuntimeHouseBuilding(pair.Key))
                continue;

            state.ScratchRemovedBuildingIds.Add(pair.Key);
        }

        if (state.ScratchRemovedBuildingIds.Count == 0)
            return;

        for (int i = 0; i < state.ScratchRemovedBuildingIds.Count; i++)
        {
            int buildingId = state.ScratchRemovedBuildingIds[i];
            if (!state.HouseholdIdsByHomeBuildingId.TryGetValue(buildingId, out int householdId) ||
                !state.TryGetHousehold(householdId, out CitizenHouseholdRecordComponent household))
                continue;

            if (household.IsDisplaced == 0)
                displaceHousehold(household, "home-missing");

            state.RemoveHomeMapping(buildingId);
        }
    }

    public void RegisterNewHouses(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        TryRehouseDisplacedHouseholdAction tryRehouseDisplacedHousehold,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen)
    {
        for (int i = 0; i < buildingReadSystem.HouseBuildingIds.Count; i++)
        {
            int homeBuildingId = buildingReadSystem.HouseBuildingIds[i];
            if (state.HouseholdIdsByHomeBuildingId.ContainsKey(homeBuildingId))
                continue;

            if (tryRehouseDisplacedHousehold(homeBuildingId))
                continue;

            int householdId = state.AllocateHouseholdId();
            int maleCitizenId = state.AllocateCitizenId();
            int femaleCitizenId = state.AllocateCitizenId();

            CitizenHouseholdRecordComponent household = new CitizenHouseholdRecordComponent
            {
                HouseholdId = householdId,
                HouseholdEntity = Unity.Entities.Entity.Null,
                HomeBuildingId = homeBuildingId,
                MaleCitizenId = maleCitizenId,
                FemaleCitizenId = femaleCitizenId,
                RefugeeTentBuildingId = 0,
                IsDisplaced = 0
            };

            household = storeHousehold(household);
            int assignedWorkBuildingId = buildingReadSystem.FindNearestBuilding(homeBuildingId, buildingReadSystem.ShopBuildingIds);
            int assignedLunchShopBuildingId = buildingReadSystem.FindNearestBuilding(homeBuildingId, buildingReadSystem.ShopBuildingIds, assignedWorkBuildingId);
            if (assignedLunchShopBuildingId == 0)
                assignedLunchShopBuildingId = assignedWorkBuildingId;
            int assignedCityHallBuildingId = buildingReadSystem.FindNearestBuilding(homeBuildingId, buildingReadSystem.CityHallBuildingIds);
            int assignedWalkBuildingId = assignedCityHallBuildingId != 0 ? assignedCityHallBuildingId : assignedWorkBuildingId;
            storeCitizen(new CitizenRecordComponent
            {
                CitizenId = maleCitizenId,
                CitizenEntity = Unity.Entities.Entity.Null,
                HouseholdId = householdId,
                HomeBuildingId = homeBuildingId,
                WorkBuildingId = assignedWorkBuildingId,
                PreferredShopBuildingId = assignedWorkBuildingId,
                LunchShopBuildingId = assignedLunchShopBuildingId,
                PreferredWalkBuildingId = assignedWalkBuildingId,
                PreferredCityHallBuildingId = assignedCityHallBuildingId,
                CurrentTargetBuildingId = homeBuildingId,
                Gender = CitizenGender.Male,
                LifeState = CitizenLifeState.Alive,
                Status = CitizenStatus.AtHome,
                StateStartedAt = Time.time,
                StateEndsAt = 0f
            });
            storeCitizen(new CitizenRecordComponent
            {
                CitizenId = femaleCitizenId,
                CitizenEntity = Unity.Entities.Entity.Null,
                HouseholdId = householdId,
                HomeBuildingId = homeBuildingId,
                WorkBuildingId = 0,
                PreferredShopBuildingId = assignedWorkBuildingId,
                LunchShopBuildingId = 0,
                PreferredWalkBuildingId = assignedWalkBuildingId,
                PreferredCityHallBuildingId = assignedCityHallBuildingId,
                CurrentTargetBuildingId = homeBuildingId,
                Gender = CitizenGender.Female,
                LifeState = CitizenLifeState.Alive,
                Status = CitizenStatus.AtHome,
                StateStartedAt = Time.time,
                StateEndsAt = 0f
            });
        }
    }

    public bool TryRehouseDisplacedHousehold(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadSystem buildingReadSystem,
        int newHomeBuildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        EstimateTravelSecondsAction estimateTravelSeconds)
    {
        int householdId = FindDisplacedHouseholdForRehousing(state);
        if (householdId == 0)
            return false;
        if (!state.TryGetHousehold(householdId, out CitizenHouseholdRecordComponent household))
            return false;

        int assignedWorkBuildingId = buildingReadSystem.FindNearestBuilding(newHomeBuildingId, buildingReadSystem.ShopBuildingIds);
        int assignedLunchShopBuildingId = buildingReadSystem.FindNearestBuilding(newHomeBuildingId, buildingReadSystem.ShopBuildingIds, assignedWorkBuildingId);
        if (assignedLunchShopBuildingId == 0)
            assignedLunchShopBuildingId = assignedWorkBuildingId;
        int assignedCityHallBuildingId = buildingReadSystem.FindNearestBuilding(newHomeBuildingId, buildingReadSystem.CityHallBuildingIds);
        int assignedWalkBuildingId = assignedCityHallBuildingId != 0 ? assignedCityHallBuildingId : assignedWorkBuildingId;

        household.HomeBuildingId = newHomeBuildingId;
        household.IsDisplaced = 0;
        household.RefugeeTentBuildingId = 0;
        household = storeHousehold(household);

        RehouseCitizen(state, household.MaleCitizenId, newHomeBuildingId, assignedWorkBuildingId, assignedWorkBuildingId, assignedLunchShopBuildingId, assignedWalkBuildingId, assignedCityHallBuildingId, storeCitizen, estimateTravelSeconds);
        RehouseCitizen(state, household.FemaleCitizenId, newHomeBuildingId, 0, assignedWorkBuildingId, 0, assignedWalkBuildingId, assignedCityHallBuildingId, storeCitizen, estimateTravelSeconds);

        return true;
    }

    public int CountLivingHouseholdMembers(CitizenPopulationStateSystem state, CitizenHouseholdRecordComponent household)
    {
        int count = 0;
        if (IsCitizenAlive(state, household.MaleCitizenId))
            count++;
        if (IsCitizenAlive(state, household.FemaleCitizenId))
            count++;
        return count;
    }

    public int CountLivingHouseholdRefugees(CitizenPopulationStateSystem state, CitizenHouseholdRecordComponent household)
    {
        int count = 0;
        if (IsCitizenRefugee(state, household.MaleCitizenId))
            count++;
        if (IsCitizenRefugee(state, household.FemaleCitizenId))
            count++;
        return count;
    }

    public bool IsCitizenAlive(CitizenPopulationStateSystem state, int citizenId)
    {
        return state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen) && citizen.LifeState != CitizenLifeState.Dead;
    }

    public bool HasHouseholdData(CitizenPopulationStateSystem state)
    {
        return state.HouseholdCount > 0;
    }

    private int FindDisplacedHouseholdForRehousing(CitizenPopulationStateSystem state)
    {
        state.PopulateHouseholdIds();
        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;
            if (household.IsDisplaced == 0)
                continue;
            if (!IsCitizenAlive(state, household.MaleCitizenId) && !IsCitizenAlive(state, household.FemaleCitizenId))
                continue;
            if (!IsCitizenAwaitingRehousing(state, household.MaleCitizenId) && !IsCitizenAwaitingRehousing(state, household.FemaleCitizenId))
                continue;

            return household.HouseholdId;
        }

        return 0;
    }

    private void RehouseCitizen(
        CitizenPopulationStateSystem state,
        int citizenId,
        int newHomeBuildingId,
        int workBuildingId,
        int preferredShopBuildingId,
        int lunchShopBuildingId,
        int preferredWalkBuildingId,
        int preferredCityHallBuildingId,
        StoreCitizenAction storeCitizen,
        EstimateTravelSecondsAction estimateTravelSeconds)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return;
        if (citizen.LifeState == CitizenLifeState.Dead)
            return;

        citizen.HomeBuildingId = newHomeBuildingId;
        citizen.WorkBuildingId = workBuildingId;
        citizen.PreferredShopBuildingId = preferredShopBuildingId;
        citizen.LunchShopBuildingId = lunchShopBuildingId;
        citizen.PreferredWalkBuildingId = preferredWalkBuildingId;
        citizen.PreferredCityHallBuildingId = preferredCityHallBuildingId;
        SetCitizenStatus(ref citizen, CitizenStatus.RelocatingToNewHouse, newHomeBuildingId, estimateTravelSeconds(citizen, newHomeBuildingId));
        storeCitizen(citizen);
    }

    private bool IsCitizenRefugee(CitizenPopulationStateSystem state, int citizenId)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        return citizen.LifeState != CitizenLifeState.Dead &&
               (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent);
    }

    private bool IsCitizenAwaitingRehousing(CitizenPopulationStateSystem state, int citizenId)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        return citizen.LifeState != CitizenLifeState.Dead &&
               (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent);
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
