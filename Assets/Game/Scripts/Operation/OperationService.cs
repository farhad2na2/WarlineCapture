using System;
using UnityEngine;

public sealed class OperationService
{
    private readonly OperationActionConfig[] _actionConfigs;
    private readonly OperationDistrictActionModifier[] _districtModifiers;
    private readonly OperationDistrictEventRule[] _eventRules;

    public OperationService()
        : this(OperationActionConfig.CreateDefaults(), OperationActionConfigSet.CreateDefaultDistrictModifiers(), OperationActionConfigSet.CreateDefaultEventRules())
    {
    }

    public OperationService(OperationActionConfig[] actionConfigs, OperationDistrictActionModifier[] districtModifiers = null, OperationDistrictEventRule[] eventRules = null)
    {
        _actionConfigs = actionConfigs == null || actionConfigs.Length == 0
            ? OperationActionConfig.CreateDefaults()
            : actionConfigs;
        _districtModifiers = districtModifiers ?? Array.Empty<OperationDistrictActionModifier>();
        _eventRules = eventRules ?? Array.Empty<OperationDistrictEventRule>();
    }

    public OperationSaveData CreateDefaultState()
    {
        return new OperationSaveData
        {
            operationDay = 1,
            districts = new[]
            {
                new DistrictStateData { districtId = "north_bridge", stability = 54, threat = 68, intel = 32, trust = 54, security = 32, infrastructure = 57, enemyInfluence = 68, heat = 42, civilianRisk = 52 },
                new DistrictStateData { districtId = "old_market", stability = 62, threat = 49, intel = 40, trust = 66, security = 51, infrastructure = 58, enemyInfluence = 49, heat = 35, civilianRisk = 44 },
                new DistrictStateData { districtId = "port_breach", stability = 38, threat = 82, intel = 24, trust = 36, security = 24, infrastructure = 41, enemyInfluence = 82, heat = 61, civilianRisk = 67 }
            },
            operationSupplies = 4,
            pendingEvents = Array.Empty<OperationEventData>(),
            intelEvidence = Array.Empty<OperationIntelEvidenceData>()
        };
    }

    public OperationActionResult ApplyAction(OperationSaveData state, OperationActionRequest request)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        DistrictStateData district = FindDistrict(state, request.DistrictId);
        OperationActionConfig config = FindActionConfig(request.ActionType);
        OperationDistrictActionModifier modifier = FindDistrictModifier(request.DistrictId, request.ActionType);
        OperationActionResult result = CreateActionResult(request, config, modifier, state.operationSupplies, state.operationDay);

        if (result.Applied)
        {
            district.stability = ClampMeter(district.stability + result.StabilityDelta);
            district.threat = ClampMeter(district.threat + result.ThreatDelta);
            district.intel = ClampMeter(district.intel + result.IntelDelta);
            district.trust = ClampMeter(district.trust + result.TrustDelta);
            district.security = ClampMeter(district.security + result.SecurityDelta);
            district.infrastructure = ClampMeter(district.infrastructure + result.InfrastructureDelta);
            district.enemyInfluence = ClampMeter(district.enemyInfluence + result.EnemyInfluenceDelta);
            district.heat = ClampMeter(district.heat + result.HeatDelta);
            district.civilianRisk = ClampMeter(district.civilianRisk + result.CivilianRiskDelta);
            state.operationSupplies = Mathf.Max(0, state.operationSupplies + result.SupplyDelta);
            state.completedActions = Mathf.Max(0, state.completedActions + 1);
        }

        AddPendingEvent(state, result.Event);
        if (result.Applied && request.ActionType == OperationActionType.Scan)
            AddIntelEvidence(state, district, result.Event);

        return result;
    }

    public void EndDay(OperationSaveData state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        state.operationDay = Mathf.Max(1, state.operationDay + 1);
        if (state.districts == null)
            return;

        foreach (DistrictStateData district in state.districts)
        {
            if (district == null)
                continue;

            district.threat = ClampMeter(district.threat + 3);
            district.stability = ClampMeter(district.stability - 1);
            district.trust = ClampMeter(district.trust - 1);
            district.security = ClampMeter(district.security - 1);
            district.enemyInfluence = ClampMeter(district.enemyInfluence + 2);
            district.heat = ClampMeter(district.heat + 2);
            district.civilianRisk = ClampMeter(district.civilianRisk + 2);
        }

        state.operationSupplies = Mathf.Min(9, Mathf.Max(0, state.operationSupplies + 1));
        AddPendingEvent(state, new OperationEventData
        {
            eventId = $"operation.day.{state.operationDay}",
            category = OperationEventCategory.Pressure,
            severity = OperationEventSeverity.Warning,
            operationDay = state.operationDay,
            title = "Daily Pressure Report",
            body = "Enemy pressure increased across active districts. One operation supply recovered."
        });
        AddDistrictAlertEvents(state);
    }

    private static OperationActionResult CreateActionResult(
        OperationActionRequest request,
        OperationActionConfig config,
        OperationDistrictActionModifier modifier,
        int availableSupplies,
        int operationDay)
    {
        if (config.supplyCost > availableSupplies)
        {
            return new OperationActionResult(
                request.DistrictId,
                request.ActionType,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                "Insufficient operation supplies.",
                CreateEvent(
                    request,
                    "Supply Shortage",
                    $"{config.eventTitle} requires {config.supplyCost} operation supplies.",
                    OperationEventCategory.Supply,
                    OperationEventSeverity.Warning,
                    operationDay));
        }

        string title = !string.IsNullOrWhiteSpace(modifier?.eventTitle) ? modifier.eventTitle : config.eventTitle;
        string body = !string.IsNullOrWhiteSpace(modifier?.eventBody) ? modifier.eventBody : config.eventBody;
        OperationEventSeverity severity = modifier != null && modifier.eventSeverity != OperationEventSeverity.Info
            ? modifier.eventSeverity
            : config.eventSeverity;

        return new OperationActionResult(
            request.DistrictId,
            request.ActionType,
            config.stabilityDelta + (modifier?.stabilityDelta ?? 0),
            config.threatDelta + (modifier?.threatDelta ?? 0),
            config.intelDelta + (modifier?.intelDelta ?? 0),
            config.SupplyDelta + (modifier?.supplyReward ?? 0),
            config.trustDelta + (modifier?.trustDelta ?? 0),
            config.securityDelta + (modifier?.securityDelta ?? 0),
            config.infrastructureDelta + (modifier?.infrastructureDelta ?? 0),
            config.enemyInfluenceDelta + (modifier?.enemyInfluenceDelta ?? 0),
            config.heatDelta + (modifier?.heatDelta ?? 0),
            config.civilianRiskDelta + (modifier?.civilianRiskDelta ?? 0),
            config.startsRaidMission,
            CreateEvent(request, title, body, CategoryFor(request.ActionType), severity, operationDay));
    }

    private OperationActionConfig FindActionConfig(OperationActionType actionType)
    {
        foreach (OperationActionConfig config in _actionConfigs)
        {
            if (config != null && config.actionType == actionType)
                return config;
        }

        throw new InvalidOperationException($"Missing Operation action config for '{actionType}'.");
    }

    private OperationDistrictActionModifier FindDistrictModifier(string districtId, OperationActionType actionType)
    {
        foreach (OperationDistrictActionModifier modifier in _districtModifiers)
        {
            if (modifier != null && modifier.districtId == districtId && modifier.actionType == actionType)
                return modifier;
        }

        return null;
    }

    private static OperationEventData CreateEvent(
        OperationActionRequest request,
        string title,
        string body,
        OperationEventCategory category,
        OperationEventSeverity severity,
        int operationDay)
    {
        return new OperationEventData
        {
            eventId = $"operation.{request.ActionType.ToString().ToLowerInvariant()}.{request.DistrictId}",
            districtId = request.DistrictId,
            actionType = request.ActionType,
            category = category,
            severity = severity,
            operationDay = Mathf.Max(1, operationDay),
            title = title,
            body = body
        };
    }

    private static OperationEventCategory CategoryFor(OperationActionType actionType)
    {
        return actionType switch
        {
            OperationActionType.Patrol => OperationEventCategory.Patrol,
            OperationActionType.Scan => OperationEventCategory.Intel,
            OperationActionType.Aid => OperationEventCategory.Aid,
            OperationActionType.Raid => OperationEventCategory.Raid,
            OperationActionType.Repair => OperationEventCategory.Repair,
            OperationActionType.Evacuate => OperationEventCategory.Evacuation,
            OperationActionType.BuildOutpost => OperationEventCategory.Outpost,
            _ => OperationEventCategory.System
        };
    }

    private static void AddPendingEvent(OperationSaveData state, OperationEventData operationEvent)
    {
        if (state == null || operationEvent == null)
            return;

        state.pendingEvents ??= Array.Empty<OperationEventData>();
        var events = new OperationEventData[state.pendingEvents.Length + 1];
        Array.Copy(state.pendingEvents, events, state.pendingEvents.Length);
        events[^1] = operationEvent;
        state.pendingEvents = events;
    }

    private void AddDistrictAlertEvents(OperationSaveData state)
    {
        if (state?.districts == null || _eventRules.Length == 0)
            return;

        foreach (DistrictStateData district in state.districts)
        {
            if (district == null)
                continue;

            foreach (OperationDistrictEventRule rule in _eventRules)
            {
                if (rule == null || !rule.TryCreateEvent(district, state.operationDay, out OperationEventData alert))
                    continue;

                AddPendingEvent(state, alert);
            }
        }
    }

    private static void AddIntelEvidence(OperationSaveData state, DistrictStateData district, OperationEventData sourceEvent)
    {
        if (state == null || district == null || sourceEvent == null)
            return;

        state.intelEvidence ??= Array.Empty<OperationIntelEvidenceData>();
        int evidenceIndex = state.intelEvidence.Length + 1;
        var evidence = new OperationIntelEvidenceData
        {
            evidenceId = $"operation.evidence.{sourceEvent.operationDay}.{sourceEvent.districtId}.{evidenceIndex}",
            districtId = sourceEvent.districtId,
            sourceEventId = sourceEvent.eventId,
            title = $"{FormatDistrictName(sourceEvent.districtId)} Intel Sweep",
            body = $"Confidence raised to {district.intel}. {sourceEvent.body}",
            confidence = district.intel,
            operationDay = sourceEvent.operationDay
        };

        var evidenceRows = new OperationIntelEvidenceData[state.intelEvidence.Length + 1];
        Array.Copy(state.intelEvidence, evidenceRows, state.intelEvidence.Length);
        evidenceRows[^1] = evidence;
        state.intelEvidence = evidenceRows;
    }

    private static string FormatDistrictName(string districtId)
    {
        return string.IsNullOrWhiteSpace(districtId)
            ? "Unknown District"
            : districtId.Replace('_', ' ').ToUpperInvariant();
    }

    private static DistrictStateData FindDistrict(OperationSaveData state, string districtId)
    {
        if (state.districts == null)
            throw new InvalidOperationException("Operation state has no districts.");

        foreach (DistrictStateData district in state.districts)
        {
            if (district != null && district.districtId == districtId)
                return district;
        }

        throw new InvalidOperationException($"Unknown operation district '{districtId}'.");
    }

    private static int ClampMeter(int value)
    {
        return Mathf.Clamp(value, 0, 100);
    }
}
