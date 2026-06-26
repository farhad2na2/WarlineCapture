using UnityEngine;
using Unity.Entities;

internal sealed class CitizenHouseholdRegistrationCompositionSystemHelper
{
    public delegate CitizenHouseholdRecordComponent StoreHouseholdAction(CitizenHouseholdRecordComponent household);
    public delegate CitizenRecordComponent StoreCitizenAction(CitizenRecordComponent citizen);
    public delegate bool TryRehouseDisplacedHouseholdAction(int newHomeBuildingId);
    public delegate void DisplaceHouseholdAction(CitizenHouseholdRecordComponent household, string reason);
    public delegate float EstimateTravelSecondsAction(CitizenRecordComponent citizen, int targetBuildingId);

    public static void SyncRemovedHouses(
        CitizenHouseholdRegistrationCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        DisplaceHouseholdAction displaceHousehold)
    {
        if (system != null)
        {
            system.SyncRemovedHouses(state, buildingReadSystem, displaceHousehold);
            return;
        }

        SyncRemovedHousesState(state, buildingReadSystem, displaceHousehold);
    }

    public void SyncRemovedHouses(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        DisplaceHouseholdAction displaceHousehold)
    {
        SyncRemovedHousesState(state, buildingReadSystem, displaceHousehold);
    }

    public static void RegisterNewHouses(
        CitizenHouseholdRegistrationCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        TryRehouseDisplacedHouseholdAction tryRehouseDisplacedHousehold,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen)
    {
        if (system != null)
        {
            system.RegisterNewHouses(
                state,
                buildingReadSystem,
                tryRehouseDisplacedHousehold,
                storeHousehold,
                storeCitizen);
            return;
        }

        RegisterNewHousesState(
            state,
            buildingReadSystem,
            tryRehouseDisplacedHousehold,
            storeHousehold,
            storeCitizen);
    }

    public void RegisterNewHouses(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        TryRehouseDisplacedHouseholdAction tryRehouseDisplacedHousehold,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen)
    {
        RegisterNewHousesState(
            state,
            buildingReadSystem,
            tryRehouseDisplacedHousehold,
            storeHousehold,
            storeCitizen);
    }

    public static bool TryRehouseDisplacedHousehold(
        CitizenHouseholdRegistrationCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int newHomeBuildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        EstimateTravelSecondsAction estimateTravelSeconds)
    {
        return system != null
            ? system.TryRehouseDisplacedHousehold(
                state,
                buildingReadSystem,
                newHomeBuildingId,
                storeHousehold,
                storeCitizen,
                estimateTravelSeconds)
            : TryRehouseDisplacedHouseholdState(
                state,
                buildingReadSystem,
                newHomeBuildingId,
                storeHousehold,
                storeCitizen,
                estimateTravelSeconds);
    }

    public bool TryRehouseDisplacedHousehold(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        int newHomeBuildingId,
        StoreHouseholdAction storeHousehold,
        StoreCitizenAction storeCitizen,
        EstimateTravelSecondsAction estimateTravelSeconds)
    {
        return TryRehouseDisplacedHouseholdState(
            state,
            buildingReadSystem,
            newHomeBuildingId,
            storeHousehold,
            storeCitizen,
            estimateTravelSeconds);
    }

    public static int CountLivingHouseholdMembers(
        CitizenHouseholdRegistrationCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenHouseholdRecordComponent household)
    {
        return system != null
            ? system.CountLivingHouseholdMembers(state, household)
            : CountLivingHouseholdMembersState(state, household);
    }

    public int CountLivingHouseholdMembers(CitizenPopulationStateCompositionSystemHelper state, CitizenHouseholdRecordComponent household)
    {
        return CountLivingHouseholdMembersState(state, household);
    }

    public static int CountLivingHouseholdRefugees(
        CitizenHouseholdRegistrationCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenHouseholdRecordComponent household)
    {
        return system != null
            ? system.CountLivingHouseholdRefugees(state, household)
            : CountLivingHouseholdRefugeesState(state, household);
    }

    public int CountLivingHouseholdRefugees(CitizenPopulationStateCompositionSystemHelper state, CitizenHouseholdRecordComponent household)
    {
        return CountLivingHouseholdRefugeesState(state, household);
    }

    public static bool IsCitizenAlive(
        CitizenHouseholdRegistrationCompositionSystemHelper system,
        CitizenPopulationStateCompositionSystemHelper state,
        int citizenId)
    {
        return system != null
            ? system.IsCitizenAlive(state, citizenId)
            : IsCitizenAliveState(state, citizenId);
    }

    public bool IsCitizenAlive(CitizenPopulationStateCompositionSystemHelper state, int citizenId)
    {
        return IsCitizenAliveState(state, citizenId);
    }

    public static bool HasHouseholdData(CitizenHouseholdRegistrationCompositionSystemHelper system, CitizenPopulationStateCompositionSystemHelper state)
    {
        return system != null
            ? system.HasHouseholdData(state)
            : HasHouseholdDataState(state);
    }

    public bool HasHouseholdData(CitizenPopulationStateCompositionSystemHelper state)
    {
        return HasHouseholdDataState(state);
    }

    private static void SyncRemovedHousesState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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

    private static void RegisterNewHousesState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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
                StateStartedAt = UnityEngine.Time.time,
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
                StateStartedAt = UnityEngine.Time.time,
                StateEndsAt = 0f
            });
        }
    }

    private static bool TryRehouseDisplacedHouseholdState(
        CitizenPopulationStateCompositionSystemHelper state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
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

        RehouseCitizenState(state, household.MaleCitizenId, newHomeBuildingId, assignedWorkBuildingId, assignedWorkBuildingId, assignedLunchShopBuildingId, assignedWalkBuildingId, assignedCityHallBuildingId, storeCitizen, estimateTravelSeconds);
        RehouseCitizenState(state, household.FemaleCitizenId, newHomeBuildingId, 0, assignedWorkBuildingId, 0, assignedWalkBuildingId, assignedCityHallBuildingId, storeCitizen, estimateTravelSeconds);

        return true;
    }

    private static int CountLivingHouseholdMembersState(CitizenPopulationStateCompositionSystemHelper state, CitizenHouseholdRecordComponent household)
    {
        int count = 0;
        if (IsCitizenAliveState(state, household.MaleCitizenId))
            count++;
        if (IsCitizenAliveState(state, household.FemaleCitizenId))
            count++;
        return count;
    }

    private static int CountLivingHouseholdRefugeesState(CitizenPopulationStateCompositionSystemHelper state, CitizenHouseholdRecordComponent household)
    {
        int count = 0;
        if (IsCitizenRefugeeState(state, household.MaleCitizenId))
            count++;
        if (IsCitizenRefugeeState(state, household.FemaleCitizenId))
            count++;
        return count;
    }

    private static bool IsCitizenAliveState(CitizenPopulationStateCompositionSystemHelper state, int citizenId)
    {
        return state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen) && citizen.LifeState != CitizenLifeState.Dead;
    }

    private static bool HasHouseholdDataState(CitizenPopulationStateCompositionSystemHelper state)
    {
        return state.HouseholdCount > 0;
    }

    private static int FindDisplacedHouseholdForRehousing(CitizenPopulationStateCompositionSystemHelper state)
    {
        state.PopulateHouseholdIds();
        for (int i = 0; i < state.ScratchHouseholdIds.Count; i++)
        {
            if (!state.TryGetHousehold(state.ScratchHouseholdIds[i], out CitizenHouseholdRecordComponent household))
                continue;
            if (household.IsDisplaced == 0)
                continue;
            if (!IsCitizenAliveState(state, household.MaleCitizenId) && !IsCitizenAliveState(state, household.FemaleCitizenId))
                continue;
            if (!IsCitizenAwaitingRehousingState(state, household.MaleCitizenId) && !IsCitizenAwaitingRehousingState(state, household.FemaleCitizenId))
                continue;

            return household.HouseholdId;
        }

        return 0;
    }

    private static void RehouseCitizenState(
        CitizenPopulationStateCompositionSystemHelper state,
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

    private static bool IsCitizenRefugeeState(CitizenPopulationStateCompositionSystemHelper state, int citizenId)
    {
        if (!state.TryGetCitizen(citizenId, out CitizenRecordComponent citizen))
            return false;

        return citizen.LifeState != CitizenLifeState.Dead &&
               (citizen.Status == CitizenStatus.RefugeeSeekingShelter || citizen.Status == CitizenStatus.AtRefugeeTent);
    }

    private static bool IsCitizenAwaitingRehousingState(CitizenPopulationStateCompositionSystemHelper state, int citizenId)
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
        citizen.StateStartedAt = UnityEngine.Time.time;
        citizen.StateEndsAt = stateDurationSeconds > 0f ? UnityEngine.Time.time + stateDurationSeconds : 0f;
        citizen.LifeState = status == CitizenStatus.Dead ? CitizenLifeState.Dead : CitizenLifeState.Alive;
    }
}
