using UnityEngine;

internal sealed class CitizenPopulationRuntimeUpdateSystem
{
    private CitizenPopulationCompositionSystemHelper.Result _systems;

    public void Bind(CitizenPopulationCompositionSystemHelper.Result systems)
    {
        _systems = systems;
    }

    public void Reset()
    {
        _systems = null;
    }

    public void Update()
    {
        if (_systems == null)
            return;
        if (!_systems.PopulationEnabled)
            return;

        CitizenPopulationLifecycleSystem.Update(
            _systems.LifecycleSystem,
            ref _systems.LifecycleState,
            _systems.BuildingReadSystem,
            _systems.EcsProjection,
            _systems.DangerSystem,
            _systems.DiagnosticSystem,
            _systems.State,
            UpdateLogicalCitizenPopulation,
            SyncVisibleCitizens,
            RecalculateTotalsForLifecycle,
            _systems.UnitPathfindingPendingStateReader.HasPendingPathJob,
            UnityEngine.Time.time);
    }

    public CitizenHouseholdRecordComponent StoreHousehold(CitizenHouseholdRecordComponent household)
    {
        _systems.EcsProjection.EnsureHouseholdEntity(ref household);
        household = _systems.State.StoreHousehold(household);
        _systems.EcsProjection.SyncHouseholdEntity(household);
        return household;
    }

    public CitizenRecordComponent StoreCitizen(CitizenRecordComponent citizen)
    {
        _systems.EcsProjection.EnsureCitizenEntity(ref citizen);
        citizen = _systems.State.StoreCitizen(citizen);
        _systems.EcsProjection.SyncCitizenEntity(_systems.State, citizen);
        return citizen;
    }

    public bool HandleCitizenDeath(int citizenId, string reason)
    {
        if (!CitizenStatusTransitionSystem.TryMarkCitizenDead(
                _systems.StatusTransitionSystem,
                _systems.State,
                citizenId,
                reason,
                UnityEngine.Time.time,
                StoreCitizen))
            return false;

        _systems.VisibleUnitSystem.RemoveVisibleCitizen(_systems.State, _systems.EcsProjection, citizenId);
        RecalculateTotals();
        return true;
    }

    private bool TryGetCitizenRecord(int citizenId, out CitizenRecordComponent citizen)
    {
        return _systems.State.TryGetCitizen(citizenId, out citizen);
    }

    private bool TryGetHouseholdRecord(int householdId, out CitizenHouseholdRecordComponent household)
    {
        return _systems.State.TryGetHousehold(householdId, out household);
    }

    private bool TryGetRuntimeBuildingDestroyedState(int buildingId, out bool isDestroyed)
    {
        isDestroyed = false;
        return _systems.BuildingReadSystem.TryGetRuntimeBuildingDestroyedState(buildingId, out isDestroyed);
    }

    private void RecalculateTotals()
    {
        RecalculateTotalsFromRecords(syncSummaryEntity: true);
    }

    private void RecalculateTotalsFromRecords(bool syncSummaryEntity)
    {
        CitizenPopulationReadModelSystem.Refresh(
            _systems.ReadModel,
            ref _systems.ReadModelState,
            _systems.TotalsSystem,
            _systems.State,
            _systems.EcsProjection,
            syncSummaryEntity);
    }

    private void RecalculateTotalsForLifecycle(bool syncSummaryEntity)
    {
        if (syncSummaryEntity)
            RecalculateTotals();
        else
            RecalculateTotalsFromRecords(syncSummaryEntity: false);
    }

    private void UpdateLogicalCitizenPopulation()
    {
        CitizenHouseholdRegistrationSystem.SyncRemovedHouses(
            _systems.HouseholdRegistrationSystem,
            _systems.State,
            _systems.BuildingReadSystem,
            (household, reason) => CitizenRefugeeSystem.DisplaceHousehold(
                _systems.RefugeeSystem,
                _systems.State,
                _systems.BuildingReadSystem,
                _systems.HouseholdRegistrationSystem,
                household,
                reason,
                StoreHousehold,
                StoreCitizen,
                (CitizenHouseholdRecordComponent household, out Vector3 worldPosition) => CitizenTravelSystem.TryGetHouseholdReferenceWorldPosition(_systems.TravelSystem, _systems.State, _systems.EcsProjection, _systems.BuildingReadSystem, _systems.StatusTransitionSystem, household, out worldPosition),
                (CitizenRecordComponent citizen, int targetBuildingId) => CitizenTravelSystem.EstimateTravelSeconds(_systems.TravelSystem, _systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId),
                HandleCitizenDeath));
        CitizenHouseholdRegistrationSystem.RegisterNewHouses(
            _systems.HouseholdRegistrationSystem,
            _systems.State,
            _systems.BuildingReadSystem,
            newHomeBuildingId => CitizenHouseholdRegistrationSystem.TryRehouseDisplacedHousehold(
                _systems.HouseholdRegistrationSystem,
                _systems.State,
                _systems.BuildingReadSystem,
                newHomeBuildingId,
                StoreHousehold,
                StoreCitizen,
                (CitizenRecordComponent citizen, int targetBuildingId) => CitizenTravelSystem.EstimateTravelSeconds(_systems.TravelSystem, _systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId)),
            StoreHousehold,
            StoreCitizen);
        CitizenRefugeeSystem.UpdateRefugeeTentState(
            _systems.RefugeeSystem,
            _systems.State,
            _systems.BuildingReadSystem,
            _systems.HouseholdRegistrationSystem,
            StoreHousehold,
            StoreCitizen,
            (CitizenHouseholdRecordComponent household, out Vector3 worldPosition) => CitizenTravelSystem.TryGetHouseholdReferenceWorldPosition(_systems.TravelSystem, _systems.State, _systems.EcsProjection, _systems.BuildingReadSystem, _systems.StatusTransitionSystem, household, out worldPosition),
            (CitizenRecordComponent citizen, int targetBuildingId) => CitizenTravelSystem.EstimateTravelSeconds(_systems.TravelSystem, _systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId),
            HandleCitizenDeath);
        UpdateDeferredCitizenTravel();
        UpdateCitizenSchedules();
        CitizenRefugeeSystem.UpdateRefugeeUpkeep(
            _systems.RefugeeSystem,
            ref _systems.RefugeeState,
            _systems.State,
            _systems.BuildingReadSystem,
            _systems.HouseholdRegistrationSystem,
            _systems.CitizenResourceSystem,
            _systems.CitizenResourceContext,
            _systems.DayNightSystem,
            HandleCitizenDeath,
            StoreHousehold);
    }

    private void UpdateCitizenSchedules()
    {
        if (_systems.DayNightSystem == null || !HasCitizenData())
            return;

        UpdateHouseholdDisplacementState();

        PopulateCitizenIdsFromEcs();

        for (int i = 0; i < _systems.State.ScratchCitizenIds.Count; i++)
        {
            int citizenId = _systems.State.ScratchCitizenIds[i];
            if (!TryGetCitizenRecord(citizenId, out CitizenRecordComponent citizen))
                continue;

            if (citizen.LifeState == CitizenLifeState.Dead)
                continue;

            if (citizen.Status == CitizenStatus.RefugeeSeekingShelter ||
                citizen.Status == CitizenStatus.RelocatingToNewHouse ||
                citizen.Status == CitizenStatus.LeavingWorld)
            {
                continue;
            }

            if (CitizenDangerSystem.TryGetDangerFleeTarget(_systems.DangerSystem, _systems.BuildingReadSystem, citizen, out int fleeTargetBuildingId))
            {
                if (citizen.Status != CitizenStatus.Fleeing || citizen.CurrentTargetBuildingId != fleeTargetBuildingId)
                {
                    CitizenStatusTransitionSystem.SetCitizenStatus(
                        _systems.StatusTransitionSystem,
                        ref citizen,
                        CitizenStatus.Fleeing,
                        fleeTargetBuildingId,
                        0f,
                        UnityEngine.Time.time);
                    StoreCitizen(citizen);
                }
                continue;
            }

            if (citizen.Status == CitizenStatus.Fleeing)
                continue;

            CitizenStatus desiredStatus = CitizenScheduleSystem.GetScheduledStatus(
                _systems.ScheduleSystem,
                _systems.State,
                _systems.DayNightSystem,
                citizen);
            int desiredTargetBuildingId = CitizenScheduleSystem.GetScheduledTargetBuildingId(
                _systems.ScheduleSystem,
                _systems.State,
                _systems.DayNightSystem,
                citizen,
                desiredStatus);
            CitizenStatus nextStatus = CitizenStatusTransitionSystem.ShouldUseTravelStatus(
                    _systems.StatusTransitionSystem,
                    _systems.State,
                    citizen,
                    desiredStatus,
                    desiredTargetBuildingId)
                ? CitizenStatusTransitionSystem.GetTravelStatusForDesiredStatus(_systems.StatusTransitionSystem, desiredStatus)
                : desiredStatus;

            if (citizen.Status == nextStatus && citizen.CurrentTargetBuildingId == desiredTargetBuildingId)
                continue;

            float stateDurationSeconds = CitizenStatusTransitionSystem.IsTravelStatus(_systems.StatusTransitionSystem, nextStatus)
                ? CitizenTravelSystem.EstimateTravelSeconds(_systems.TravelSystem, _systems.State, _systems.BuildingReadSystem, citizen, desiredTargetBuildingId)
                : 0f;
            CitizenStatusTransitionSystem.SetCitizenStatus(
                _systems.StatusTransitionSystem,
                ref citizen,
                nextStatus,
                desiredTargetBuildingId,
                stateDurationSeconds,
                UnityEngine.Time.time);
            StoreCitizen(citizen);
        }
    }

    private void UpdateDeferredCitizenTravel()
    {
        PopulateCitizenIdsFromEcs();
        for (int i = 0; i < _systems.State.ScratchCitizenIds.Count; i++)
        {
            int citizenId = _systems.State.ScratchCitizenIds[i];
            if (_systems.State.VisibleCitizensById.ContainsKey(citizenId))
                continue;
            if (!TryGetCitizenRecord(citizenId, out CitizenRecordComponent citizen))
                continue;
            if (citizen.LifeState == CitizenLifeState.Dead)
                continue;
            if (!CitizenStatusTransitionSystem.IsTravelStatus(_systems.StatusTransitionSystem, citizen.Status))
                continue;
            if (citizen.StateEndsAt <= 0f || UnityEngine.Time.time < citizen.StateEndsAt)
                continue;

            CitizenStatusTransitionSystem.TryResolveCitizenArrival(
                _systems.StatusTransitionSystem,
                _systems.State,
                citizenId,
                UnityEngine.Time.time,
                StoreCitizen);
        }
    }

    private void SyncVisibleCitizens()
    {
        _systems.VisibleUnitSystem.SyncVisibleCitizens(
            _systems.State,
            _systems.EcsProjection,
            _systems.BuildingReadSystem,
            _systems.StatusTransitionSystem,
            _systems.CitizenPrefabSystem,
            _systems.CitizenPrefabContext,
            _systems.PrefabSelectionSystem,
            _systems.PrefabSelectionState,
            _systems.TravelSystem,
            _systems.WorldCamera,
            HasCitizenData(),
            UnityEngine.Time.time,
            StoreCitizen,
            HandleCitizenDeath);
        if (_systems.EcsProjection.HasWorld)
            CitizenMovementCommandSystem.ProcessPendingRequests(_systems.EcsProjection.EntityManager);
    }

    private void UpdateHouseholdDisplacementState()
    {
        if (!HasHouseholdData() || !_systems.BuildingReadSystem.HasRuntimeBuildingQuery())
            return;

        PopulateHouseholdIdsFromEcs();

        for (int i = 0; i < _systems.State.ScratchHouseholdIds.Count; i++)
        {
            int householdId = _systems.State.ScratchHouseholdIds[i];
            if (!TryGetHouseholdRecord(householdId, out CitizenHouseholdRecordComponent household))
                continue;
            if (household.IsDisplaced != 0)
                continue;

            bool homeExists = TryGetRuntimeBuildingDestroyedState(household.HomeBuildingId, out bool isDestroyed);
            if (homeExists && !isDestroyed)
                continue;

            CitizenRefugeeSystem.DisplaceHousehold(
                _systems.RefugeeSystem,
                _systems.State,
                _systems.BuildingReadSystem,
                _systems.HouseholdRegistrationSystem,
                household,
                homeExists ? "home-destroyed" : "home-missing",
                StoreHousehold,
                StoreCitizen,
                (CitizenHouseholdRecordComponent household, out Vector3 worldPosition) => CitizenTravelSystem.TryGetHouseholdReferenceWorldPosition(_systems.TravelSystem, _systems.State, _systems.EcsProjection, _systems.BuildingReadSystem, _systems.StatusTransitionSystem, household, out worldPosition),
                (CitizenRecordComponent citizen, int targetBuildingId) => CitizenTravelSystem.EstimateTravelSeconds(_systems.TravelSystem, _systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId),
                HandleCitizenDeath);
        }
    }

    private void PopulateCitizenIdsFromEcs()
    {
        _systems.State.PopulateCitizenIds();
    }

    private void PopulateHouseholdIdsFromEcs()
    {
        _systems.State.PopulateHouseholdIds();
    }

    private bool HasCitizenData()
    {
        return CitizenPopulationTotalsSystem.HasCitizenData(_systems.TotalsSystem, _systems.State);
    }

    private bool HasHouseholdData()
    {
        return CitizenPopulationTotalsSystem.HasHouseholdData(_systems.TotalsSystem, _systems.State, _systems.EcsProjection);
    }
}
