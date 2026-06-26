using UnityEngine;

public sealed class CitizenPopulationEventCompositionSystemHelper
{
    private CitizenPopulationStateSystem _state;
    private CitizenBuildingReadCompositionSystemHelper _buildingReadSystem;
    private CitizenHouseholdRegistrationCompositionSystemHelper _householdRegistrationSystem;
    private CitizenRefugeeSystem _refugeeSystem;
    private CitizenTravelSystem _travelSystem;
    private CitizenPopulationEcsProjectionCompositionSystemHelper _ecsProjection;
    private CitizenStatusTransitionSystem _statusTransitionSystem;
    private CitizenRefugeeSystem.StoreHouseholdAction _storeHousehold;
    private CitizenRefugeeSystem.StoreCitizenAction _storeCitizen;
    private CitizenRefugeeSystem.TryGetHouseholdReferenceWorldPositionAction _tryGetHouseholdReferenceWorldPosition;
    private CitizenRefugeeSystem.EstimateTravelSecondsAction _estimateTravelSeconds;
    private CitizenRefugeeSystem.MarkCitizenDeadAction _markCitizenDead;

    internal void Init(
        CitizenPopulationStateSystem state,
        CitizenBuildingReadCompositionSystemHelper buildingReadSystem,
        CitizenHouseholdRegistrationCompositionSystemHelper householdRegistrationSystem,
        CitizenRefugeeSystem refugeeSystem,
        CitizenTravelSystem travelSystem,
        CitizenPopulationEcsProjectionCompositionSystemHelper ecsProjection,
        CitizenStatusTransitionSystem statusTransitionSystem,
        CitizenRefugeeSystem.StoreHouseholdAction storeHousehold,
        CitizenRefugeeSystem.StoreCitizenAction storeCitizen,
        CitizenRefugeeSystem.MarkCitizenDeadAction markCitizenDead)
    {
        _state = state;
        _buildingReadSystem = buildingReadSystem;
        _householdRegistrationSystem = householdRegistrationSystem;
        _refugeeSystem = refugeeSystem;
        _travelSystem = travelSystem;
        _ecsProjection = ecsProjection;
        _statusTransitionSystem = statusTransitionSystem;
        _storeHousehold = storeHousehold;
        _storeCitizen = storeCitizen;
        _tryGetHouseholdReferenceWorldPosition = TryGetHouseholdReferenceWorldPosition;
        _estimateTravelSeconds = EstimateTravelSeconds;
        _markCitizenDead = markCitizenDead;
    }

    public void Reset()
    {
        _state = null;
        _buildingReadSystem = null;
        _householdRegistrationSystem = null;
        _refugeeSystem = null;
        _travelSystem = null;
        _ecsProjection = null;
        _statusTransitionSystem = null;
        _storeHousehold = null;
        _storeCitizen = null;
        _tryGetHouseholdReferenceWorldPosition = null;
        _estimateTravelSeconds = null;
        _markCitizenDead = null;
    }

    public void NotifyVisibleCitizenDestroyed(int citizenId)
    {
        if (_state == null || !_state.VisibleCitizensById.ContainsKey(citizenId))
            return;

        _markCitizenDead?.Invoke(citizenId, "visual-destroyed");
    }

    public void NotifyHomeBuildingDestroyed(int buildingId)
    {
        if (_state == null)
            return;

        CitizenRefugeeSystem.NotifyHomeBuildingDestroyed(
            _refugeeSystem,
            _state,
            _buildingReadSystem,
            _householdRegistrationSystem,
            buildingId,
            _storeHousehold,
            _storeCitizen,
            _tryGetHouseholdReferenceWorldPosition,
            _estimateTravelSeconds,
            _markCitizenDead);
    }

    private bool TryGetHouseholdReferenceWorldPosition(CitizenHouseholdRecordComponent household, out Vector3 worldPosition)
    {
        return CitizenTravelSystem.TryGetHouseholdReferenceWorldPosition(
            _travelSystem,
            _state,
            _ecsProjection,
            _buildingReadSystem,
            _statusTransitionSystem,
            household,
            out worldPosition);
    }

    private float EstimateTravelSeconds(CitizenRecordComponent citizen, int targetBuildingId)
    {
        return CitizenTravelSystem.EstimateTravelSeconds(_travelSystem, _state, _buildingReadSystem, citizen, targetBuildingId);
    }
}
