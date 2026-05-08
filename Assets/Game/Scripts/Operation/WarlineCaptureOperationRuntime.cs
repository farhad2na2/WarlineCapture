using System;
using UnityEngine;

public static class WarlineCaptureOperationRuntime
{
    private const string ActionConfigResourcePath = "Operation/OperationActionConfigSet";
    private static OperationService _service;
    private static SaveService _saveService;
    private static OperationSaveData _state;
    private static bool _useInMemoryStateForTests;

    public static OperationSaveData State
    {
        get
        {
            _state ??= LoadInitialState();
            return _state;
        }
    }

    public static string SelectedDistrictId { get; private set; } = "north_bridge";

    public static DistrictStateData SelectedDistrict => FindDistrict(SelectedDistrictId);

    public static void SelectDistrict(string districtId)
    {
        if (string.IsNullOrWhiteSpace(districtId))
            return;

        FindDistrict(districtId);
        SelectedDistrictId = districtId;
    }

    public static OperationActionResult ApplyAction(OperationActionType actionType)
    {
        OperationActionResult result = Service.ApplyAction(State, new OperationActionRequest(SelectedDistrictId, actionType));
        PersistState();
        return result;
    }

    public static void EndDay()
    {
        Service.EndDay(State);
        PersistState();
    }

    public static OperationIntelEvidenceData LatestEvidence(string districtId = null)
    {
        return OperationIntelArchive.Latest(State, districtId);
    }

    public static int UnreadEvidenceCount(string districtId = null)
    {
        return OperationIntelArchive.CountUnread(State, districtId);
    }

    public static bool MarkEvidenceRead(string evidenceId)
    {
        bool changed = OperationIntelArchive.MarkRead(State, evidenceId);
        if (changed)
            PersistState();

        return changed;
    }

    public static DistrictStateData FindDistrict(string districtId)
    {
        foreach (DistrictStateData district in State.districts)
        {
            if (district != null && district.districtId == districtId)
                return district;
        }

        throw new InvalidOperationException($"Unknown operation district '{districtId}'.");
    }

    public static void ResetForTests()
    {
        _state = null;
        _service = null;
        _saveService = null;
        _useInMemoryStateForTests = true;
        SelectedDistrictId = "north_bridge";
    }

    public static void SetSaveServiceForTests(SaveService saveService)
    {
        _saveService = saveService;
        _state = null;
        _service = null;
        _useInMemoryStateForTests = saveService == null;
        SelectedDistrictId = "north_bridge";
    }

    public static void ClearCachedStateForTests()
    {
        _state = null;
        SelectedDistrictId = "north_bridge";
    }

    private static OperationSaveData LoadInitialState()
    {
        if (_useInMemoryStateForTests)
            return Service.CreateDefaultState();

        OperationSaveData state = SaveServiceInstance.LoadOperation();
        if (!HasDistricts(state))
            state = Service.CreateDefaultState();

        state.operationDay = Math.Max(1, state.operationDay);
        if (state.operationSupplies <= 0)
            state.operationSupplies = 4;
        state.pendingEvents ??= Array.Empty<OperationEventData>();
        state.intelEvidence ??= Array.Empty<OperationIntelEvidenceData>();
        if (!HasDistrict(SelectedDistrictId, state))
            SelectedDistrictId = FirstDistrictId(state);

        return state;
    }

    private static OperationService Service
    {
        get
        {
            _service ??= CreateService();
            return _service;
        }
    }

    private static OperationService CreateService()
    {
        OperationActionConfigSet configSet = Resources.Load<OperationActionConfigSet>(ActionConfigResourcePath);
        return configSet != null
            ? new OperationService(configSet.GetActionConfigs(), configSet.GetDistrictModifiers(), configSet.GetEventRules())
            : new OperationService();
    }

    private static SaveService SaveServiceInstance
    {
        get
        {
            _saveService ??= SaveService.CreateDefault();
            return _saveService;
        }
    }

    private static void PersistState()
    {
        if (_useInMemoryStateForTests || _state == null)
            return;

        SaveServiceInstance.SaveOperation(_state);
    }

    private static bool HasDistricts(OperationSaveData state)
    {
        if (state?.districts == null)
            return false;

        foreach (DistrictStateData district in state.districts)
        {
            if (district != null && !string.IsNullOrWhiteSpace(district.districtId))
                return true;
        }

        return false;
    }

    private static bool HasDistrict(string districtId, OperationSaveData state)
    {
        if (string.IsNullOrWhiteSpace(districtId) || !HasDistricts(state))
            return false;

        foreach (DistrictStateData district in state.districts)
        {
            if (district != null && district.districtId == districtId)
                return true;
        }

        return false;
    }

    private static string FirstDistrictId(OperationSaveData state)
    {
        foreach (DistrictStateData district in state.districts)
        {
            if (district != null && !string.IsNullOrWhiteSpace(district.districtId))
                return district.districtId;
        }

        throw new InvalidOperationException("Operation state has no valid districts.");
    }
}
