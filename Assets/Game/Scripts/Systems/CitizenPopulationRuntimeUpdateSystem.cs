using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class CitizenPopulationRuntimeUpdateSystem
{
    private CitizenPopulationCompositionSystem.Result _systems;

    public void Bind(CitizenPopulationCompositionSystem.Result systems)
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

        _systems.LifecycleSystem.Update(
            _systems.BuildingReadSystem,
            _systems.EcsProjection,
            _systems.DangerSystem,
            _systems.DiagnosticSystem,
            _systems.State,
            UpdateLogicalCitizenPopulation,
            SyncVisibleCitizens,
            RecalculateTotalsForLifecycle,
            _systems.UnitPathfindingPendingStateReader.HasPendingPathJob,
            Time.time);
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
        if (!_systems.StatusTransitionSystem.TryMarkCitizenDead(_systems.State, citizenId, reason, Time.time, StoreCitizen))
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
        _systems.ReadModel.Refresh(_systems.TotalsSystem, _systems.State, _systems.EcsProjection, syncSummaryEntity);
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
        _systems.HouseholdRegistrationSystem.SyncRemovedHouses(
            _systems.State,
            _systems.BuildingReadSystem,
            (household, reason) => _systems.RefugeeSystem.DisplaceHousehold(
                _systems.State,
                _systems.BuildingReadSystem,
                _systems.HouseholdRegistrationSystem,
                household,
                reason,
                StoreHousehold,
                StoreCitizen,
                (CitizenHouseholdRecordComponent household, out Vector3 worldPosition) => _systems.TravelSystem.TryGetHouseholdReferenceWorldPosition(_systems.State, _systems.EcsProjection, _systems.BuildingReadSystem, _systems.StatusTransitionSystem, household, out worldPosition),
                (CitizenRecordComponent citizen, int targetBuildingId) => _systems.TravelSystem.EstimateTravelSeconds(_systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId),
                HandleCitizenDeath));
        _systems.HouseholdRegistrationSystem.RegisterNewHouses(
            _systems.State,
            _systems.BuildingReadSystem,
            newHomeBuildingId => _systems.HouseholdRegistrationSystem.TryRehouseDisplacedHousehold(
                _systems.State,
                _systems.BuildingReadSystem,
                newHomeBuildingId,
                StoreHousehold,
                StoreCitizen,
                (CitizenRecordComponent citizen, int targetBuildingId) => _systems.TravelSystem.EstimateTravelSeconds(_systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId)),
            StoreHousehold,
            StoreCitizen);
        _systems.RefugeeSystem.UpdateRefugeeTentState(
            _systems.State,
            _systems.BuildingReadSystem,
            _systems.HouseholdRegistrationSystem,
            StoreHousehold,
            StoreCitizen,
            (CitizenHouseholdRecordComponent household, out Vector3 worldPosition) => _systems.TravelSystem.TryGetHouseholdReferenceWorldPosition(_systems.State, _systems.EcsProjection, _systems.BuildingReadSystem, _systems.StatusTransitionSystem, household, out worldPosition),
            (CitizenRecordComponent citizen, int targetBuildingId) => _systems.TravelSystem.EstimateTravelSeconds(_systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId),
            HandleCitizenDeath);
        UpdateDeferredCitizenTravel();
        UpdateCitizenSchedules();
        _systems.RefugeeSystem.UpdateRefugeeUpkeep(
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

            if (_systems.DangerSystem.TryGetDangerFleeTarget(_systems.BuildingReadSystem, citizen, out int fleeTargetBuildingId))
            {
                if (citizen.Status != CitizenStatus.Fleeing || citizen.CurrentTargetBuildingId != fleeTargetBuildingId)
                {
                    _systems.StatusTransitionSystem.SetCitizenStatus(ref citizen, CitizenStatus.Fleeing, fleeTargetBuildingId, 0f, Time.time);
                    StoreCitizen(citizen);
                }
                continue;
            }

            if (citizen.Status == CitizenStatus.Fleeing)
                continue;

            CitizenStatus desiredStatus = _systems.ScheduleSystem.GetScheduledStatus(_systems.State, _systems.DayNightSystem, citizen);
            int desiredTargetBuildingId = _systems.ScheduleSystem.GetScheduledTargetBuildingId(_systems.State, _systems.DayNightSystem, citizen, desiredStatus);
            CitizenStatus nextStatus = _systems.StatusTransitionSystem.ShouldUseTravelStatus(_systems.State, citizen, desiredStatus, desiredTargetBuildingId)
                ? _systems.StatusTransitionSystem.GetTravelStatusForDesiredStatus(desiredStatus)
                : desiredStatus;

            if (citizen.Status == nextStatus && citizen.CurrentTargetBuildingId == desiredTargetBuildingId)
                continue;

            float stateDurationSeconds = _systems.StatusTransitionSystem.IsTravelStatus(nextStatus)
                ? _systems.TravelSystem.EstimateTravelSeconds(_systems.State, _systems.BuildingReadSystem, citizen, desiredTargetBuildingId)
                : 0f;
            _systems.StatusTransitionSystem.SetCitizenStatus(ref citizen, nextStatus, desiredTargetBuildingId, stateDurationSeconds, Time.time);
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
            if (!_systems.StatusTransitionSystem.IsTravelStatus(citizen.Status))
                continue;
            if (citizen.StateEndsAt <= 0f || Time.time < citizen.StateEndsAt)
                continue;

            _systems.StatusTransitionSystem.TryResolveCitizenArrival(_systems.State, citizenId, Time.time, StoreCitizen);
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
            _systems.TravelSystem,
            _systems.WorldCamera,
            HasCitizenData(),
            Time.time,
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

            _systems.RefugeeSystem.DisplaceHousehold(
                _systems.State,
                _systems.BuildingReadSystem,
                _systems.HouseholdRegistrationSystem,
                household,
                homeExists ? "home-destroyed" : "home-missing",
                StoreHousehold,
                StoreCitizen,
                (CitizenHouseholdRecordComponent household, out Vector3 worldPosition) => _systems.TravelSystem.TryGetHouseholdReferenceWorldPosition(_systems.State, _systems.EcsProjection, _systems.BuildingReadSystem, _systems.StatusTransitionSystem, household, out worldPosition),
                (CitizenRecordComponent citizen, int targetBuildingId) => _systems.TravelSystem.EstimateTravelSeconds(_systems.State, _systems.BuildingReadSystem, citizen, targetBuildingId),
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
        return _systems.TotalsSystem.HasCitizenData(_systems.State);
    }

    private bool HasHouseholdData()
    {
        return _systems.TotalsSystem.HasHouseholdData(_systems.State, _systems.EcsProjection);
    }
}
