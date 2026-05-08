using NUnit.Framework;
using System.IO;
using UnityEngine;

public sealed class OperationServiceTests
{
    [Test]
    public void CreateDefaultState_ProvidesServiceReadyDistricts()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();

        Assert.AreEqual(1, state.operationDay);
        Assert.AreEqual(4, state.operationSupplies);
        Assert.AreEqual(3, state.districts.Length);
        Assert.AreEqual("north_bridge", state.districts[0].districtId);
        Assert.AreEqual(54, state.districts[0].trust);
        Assert.AreEqual(32, state.districts[0].security);
        Assert.AreEqual(42, state.districts[0].heat);
    }

    [Test]
    public void ApplyAction_MutatesDistrictMetersAndReportsRaidRouting()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();

        OperationActionResult scan = service.ApplyAction(state, new OperationActionRequest("north_bridge", OperationActionType.Scan));
        OperationActionResult raid = service.ApplyAction(state, new OperationActionRequest("north_bridge", OperationActionType.Raid));

        Assert.AreEqual(12, scan.IntelDelta);
        Assert.AreEqual(-1, scan.SupplyDelta);
        Assert.AreEqual("Drone Scan", scan.Event.title);
        Assert.AreEqual("north_bridge", scan.Event.districtId);
        Assert.AreEqual(OperationEventCategory.Intel, scan.Event.category);
        Assert.AreEqual(OperationEventSeverity.Info, scan.Event.severity);
        Assert.AreEqual(1, scan.Event.operationDay);
        Assert.IsTrue(raid.StartsRaidMission);
        Assert.AreEqual(OperationEventCategory.Raid, raid.Event.category);
        Assert.AreEqual(OperationEventSeverity.Warning, raid.Event.severity);
        Assert.AreEqual(54, state.districts[0].threat);
        Assert.AreEqual(38, state.districts[0].intel);
        Assert.AreEqual(-4, raid.TrustDelta);
        Assert.AreEqual(8, raid.SecurityDelta);
        Assert.AreEqual(50, state.districts[0].trust);
        Assert.AreEqual(40, state.districts[0].security);
        Assert.AreEqual(51, state.districts[0].heat);
        Assert.AreEqual(58, state.districts[0].civilianRisk);
        Assert.AreEqual(1, state.operationSupplies);
        Assert.AreEqual(2, state.completedActions);
        Assert.AreEqual(2, state.pendingEvents.Length);
        Assert.AreEqual(1, state.intelEvidence.Length);
        Assert.AreEqual("north_bridge", state.intelEvidence[0].districtId);
        Assert.AreEqual(44, state.intelEvidence[0].confidence);
    }

    [Test]
    public void ApplyAction_UsesConfiguredActionValues()
    {
        var service = new OperationService(new[]
        {
            new OperationActionConfig(OperationActionType.Patrol, 9, -11, 4, 1, 0, false, "Custom Patrol", "Config-driven patrol.")
        });
        OperationSaveData state = service.CreateDefaultState();

        OperationActionResult result = service.ApplyAction(state, new OperationActionRequest("north_bridge", OperationActionType.Patrol));

        Assert.IsTrue(result.Applied);
        Assert.AreEqual(9, result.StabilityDelta);
        Assert.AreEqual(-11, result.ThreatDelta);
        Assert.AreEqual(4, result.IntelDelta);
        Assert.AreEqual(63, state.districts[0].stability);
        Assert.AreEqual(57, state.districts[0].threat);
        Assert.AreEqual(36, state.districts[0].intel);
        Assert.AreEqual(54, state.districts[0].trust);
        Assert.AreEqual(32, state.districts[0].security);
        Assert.AreEqual(3, state.operationSupplies);
        Assert.AreEqual("Custom Patrol", state.pendingEvents[0].title);
    }

    [Test]
    public void ApplyAction_UsesDistrictSpecificModifierValues()
    {
        var service = new OperationService(
            OperationActionConfig.CreateDefaults(),
            OperationActionConfigSet.CreateDefaultDistrictModifiers());
        OperationSaveData state = service.CreateDefaultState();

        OperationActionResult result = service.ApplyAction(state, new OperationActionRequest("old_market", OperationActionType.Aid));

        Assert.IsTrue(result.Applied);
        Assert.AreEqual(10, result.StabilityDelta);
        Assert.AreEqual(0, result.SupplyDelta);
        Assert.AreEqual(12, result.TrustDelta);
        Assert.AreEqual(6, result.InfrastructureDelta);
        Assert.AreEqual(-9, result.CivilianRiskDelta);
        Assert.AreEqual(72, state.districts[1].stability);
        Assert.AreEqual(78, state.districts[1].trust);
        Assert.AreEqual(64, state.districts[1].infrastructure);
        Assert.AreEqual(35, state.districts[1].civilianRisk);
        Assert.AreEqual(4, state.operationSupplies);
        Assert.AreEqual("Old Market Aid Distribution", result.Event.title);
        Assert.AreEqual("old_market", result.Event.districtId);
        Assert.AreEqual(OperationEventCategory.Aid, result.Event.category);
        Assert.AreEqual(OperationEventSeverity.Info, result.Event.severity);
    }

    [Test]
    public void OperationActionConfigSet_LoadsAuthoredDefaultsFromResources()
    {
        OperationActionConfigSet configSet = Resources.Load<OperationActionConfigSet>("Operation/OperationActionConfigSet");

        Assert.NotNull(configSet);
        OperationActionConfig[] configs = configSet.GetActionConfigs();

        Assert.AreEqual(7, configs.Length);
        Assert.AreEqual(OperationActionType.Patrol, configs[0].actionType);
        Assert.AreEqual(3, configs[0].stabilityDelta);
        Assert.AreEqual(OperationActionType.Raid, configs[3].actionType);
        Assert.AreEqual(2, configs[3].supplyCost);
        Assert.IsTrue(configs[3].startsRaidMission);
        Assert.AreEqual(8, configs[3].securityDelta);
        Assert.AreEqual(6, configs[3].civilianRiskDelta);
        Assert.AreEqual(OperationActionType.Repair, configs[4].actionType);
        Assert.AreEqual(12, configs[4].infrastructureDelta);
        Assert.AreEqual(OperationActionType.Evacuate, configs[5].actionType);
        Assert.AreEqual(-15, configs[5].civilianRiskDelta);
        Assert.AreEqual(OperationActionType.BuildOutpost, configs[6].actionType);
        Assert.AreEqual(14, configs[6].securityDelta);

        OperationDistrictActionModifier[] modifiers = configSet.GetDistrictModifiers();
        Assert.AreEqual(6, modifiers.Length);
        Assert.AreEqual("old_market", modifiers[0].districtId);
        Assert.AreEqual(OperationActionType.Aid, modifiers[0].actionType);
        Assert.AreEqual(4, modifiers[0].trustDelta);
        Assert.AreEqual(OperationActionType.BuildOutpost, modifiers[3].actionType);
        Assert.AreEqual(OperationActionType.Evacuate, modifiers[4].actionType);
        Assert.AreEqual(OperationActionType.Repair, modifiers[5].actionType);

        OperationDistrictEventRule[] rules = configSet.GetEventRules();
        Assert.AreEqual(3, rules.Length);
        Assert.AreEqual(OperationDistrictMetric.Heat, rules[0].metric);
        Assert.AreEqual(OperationEventCategory.Civilian, rules[1].category);
    }

    [Test]
    public void ApplyAction_SupportsExpandedOperationActions()
    {
        var service = new OperationService(
            OperationActionConfig.CreateDefaults(),
            OperationActionConfigSet.CreateDefaultDistrictModifiers());

        OperationSaveData repairState = service.CreateDefaultState();
        OperationActionResult repair = service.ApplyAction(repairState, new OperationActionRequest("port_breach", OperationActionType.Repair));
        Assert.IsTrue(repair.Applied);
        Assert.AreEqual(OperationEventCategory.Repair, repair.Event.category);
        Assert.AreEqual(17, repair.InfrastructureDelta);
        Assert.AreEqual(58, repairState.districts[2].infrastructure);
        Assert.AreEqual(58, repairState.districts[2].heat);
        Assert.AreEqual(60, repairState.districts[2].civilianRisk);

        OperationSaveData evacuationState = service.CreateDefaultState();
        OperationActionResult evacuation = service.ApplyAction(evacuationState, new OperationActionRequest("old_market", OperationActionType.Evacuate));
        Assert.IsTrue(evacuation.Applied);
        Assert.AreEqual(OperationEventCategory.Evacuation, evacuation.Event.category);
        Assert.AreEqual(OperationEventSeverity.Warning, evacuation.Event.severity);
        Assert.AreEqual(-20, evacuation.CivilianRiskDelta);
        Assert.AreEqual(58, evacuationState.districts[1].trust);
        Assert.AreEqual(24, evacuationState.districts[1].civilianRisk);

        OperationSaveData outpostState = service.CreateDefaultState();
        OperationActionResult outpost = service.ApplyAction(outpostState, new OperationActionRequest("north_bridge", OperationActionType.BuildOutpost));
        Assert.IsTrue(outpost.Applied);
        Assert.AreEqual(OperationEventCategory.Outpost, outpost.Event.category);
        Assert.AreEqual(OperationEventSeverity.Warning, outpost.Event.severity);
        Assert.AreEqual(18, outpost.SecurityDelta);
        Assert.AreEqual(50, outpostState.districts[0].security);
        Assert.AreEqual(57, outpostState.districts[0].enemyInfluence);
        Assert.AreEqual(2, outpostState.operationSupplies);
    }

    [Test]
    public void OperationEvents_IncludeTypedLedgerMetadata()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();
        state.operationSupplies = 1;

        OperationActionResult shortage = service.ApplyAction(state, new OperationActionRequest("north_bridge", OperationActionType.Raid));
        service.EndDay(state);

        Assert.AreEqual(OperationEventCategory.Supply, shortage.Event.category);
        Assert.AreEqual(OperationEventSeverity.Warning, shortage.Event.severity);
        Assert.AreEqual(OperationActionType.Raid, shortage.Event.actionType);
        Assert.IsTrue(shortage.Event.unread);

        OperationEventData dayEvent = state.pendingEvents[1];
        Assert.AreEqual(OperationEventCategory.Pressure, dayEvent.category);
        Assert.AreEqual(OperationEventSeverity.Warning, dayEvent.severity);
        Assert.AreEqual(2, dayEvent.operationDay);

        OperationEventData civilianAlert = state.pendingEvents[2];
        Assert.AreEqual(OperationEventCategory.Civilian, civilianAlert.category);
        Assert.AreEqual(OperationEventSeverity.Warning, civilianAlert.severity);
        Assert.AreEqual("PORT BREACH", civilianAlert.body.Contains("PORT BREACH") ? "PORT BREACH" : string.Empty);
        Assert.AreEqual("Civilian Risk", civilianAlert.sourceMetric);
        Assert.AreEqual(69, civilianAlert.metricValue);
    }

    [Test]
    public void EndDay_CreatesAuthoredDistrictAlertEventsForRiskThresholds()
    {
        var service = new OperationService(
            OperationActionConfig.CreateDefaults(),
            OperationActionConfigSet.CreateDefaultDistrictModifiers(),
            OperationActionConfigSet.CreateDefaultEventRules());
        OperationSaveData state = service.CreateDefaultState();

        service.EndDay(state);

        Assert.AreEqual(3, state.pendingEvents.Length);
        Assert.AreEqual("Daily Pressure Report", state.pendingEvents[0].title);
        Assert.AreEqual("Civilian Risk Elevated", state.pendingEvents[1].title);
        Assert.AreEqual("Enemy Influence Entrenched", state.pendingEvents[2].title);
        Assert.AreEqual("port_breach", state.pendingEvents[1].districtId);
        Assert.AreEqual(69, state.pendingEvents[1].metricValue);
        Assert.AreEqual(84, state.pendingEvents[2].metricValue);
        StringAssert.Contains("PORT BREACH civilian risk reached 69", state.pendingEvents[1].body);
    }

    [Test]
    public void ScanAction_CreatesIntelEvidenceArchiveEntry()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();

        OperationActionResult result = service.ApplyAction(state, new OperationActionRequest("port_breach", OperationActionType.Scan));

        Assert.IsTrue(result.Applied);
        Assert.AreEqual(1, state.intelEvidence.Length);
        OperationIntelEvidenceData evidence = state.intelEvidence[0];
        Assert.AreEqual("port_breach", evidence.districtId);
        Assert.AreEqual(result.Event.eventId, evidence.sourceEventId);
        Assert.AreEqual(40, evidence.confidence);
        Assert.AreEqual(1, evidence.operationDay);
        Assert.IsTrue(evidence.unread);
        StringAssert.Contains("PORT BREACH", evidence.title);
    }

    [Test]
    public void IntelArchive_FiltersLatestUnreadAndReadState()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();

        service.ApplyAction(state, new OperationActionRequest("north_bridge", OperationActionType.Scan));
        service.ApplyAction(state, new OperationActionRequest("port_breach", OperationActionType.Scan));

        Assert.AreEqual(2, OperationIntelArchive.Count(state));
        Assert.AreEqual(1, OperationIntelArchive.Count(state, "north_bridge"));
        Assert.AreEqual(2, OperationIntelArchive.CountUnread(state));
        Assert.AreEqual("port_breach", OperationIntelArchive.Latest(state).districtId);
        Assert.AreEqual("north_bridge", OperationIntelArchive.Latest(state, "north_bridge").districtId);
        Assert.AreEqual("north_bridge", OperationIntelArchive.At(state, 1).districtId);

        string latestEvidenceId = OperationIntelArchive.Latest(state).evidenceId;

        Assert.IsTrue(OperationIntelArchive.MarkRead(state, latestEvidenceId));
        Assert.IsFalse(OperationIntelArchive.MarkRead(state, latestEvidenceId));
        Assert.AreEqual(1, OperationIntelArchive.CountUnread(state));
        Assert.IsFalse(OperationIntelArchive.Latest(state).unread);
    }

    [Test]
    public void ApplyAction_BlocksSupplyGatedActionsWhenSuppliesAreTooLow()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();
        state.operationSupplies = 1;

        OperationActionResult result = service.ApplyAction(state, new OperationActionRequest("north_bridge", OperationActionType.Raid));

        Assert.IsFalse(result.Applied);
        Assert.IsFalse(result.StartsRaidMission);
        Assert.AreEqual("Insufficient operation supplies.", result.FailureReason);
        Assert.AreEqual(68, state.districts[0].threat);
        Assert.AreEqual(1, state.operationSupplies);
        Assert.AreEqual(0, state.completedActions);
        Assert.AreEqual("Supply Shortage", state.pendingEvents[0].title);
    }

    [Test]
    public void EndDay_IncrementsDayAndAppliesPassivePressure()
    {
        var service = new OperationService();
        OperationSaveData state = service.CreateDefaultState();

        service.EndDay(state);

        Assert.AreEqual(2, state.operationDay);
        Assert.AreEqual(5, state.operationSupplies);
        Assert.AreEqual(53, state.districts[0].stability);
        Assert.AreEqual(71, state.districts[0].threat);
        Assert.AreEqual(53, state.districts[0].trust);
        Assert.AreEqual(31, state.districts[0].security);
        Assert.AreEqual(44, state.districts[0].heat);
        Assert.AreEqual(54, state.districts[0].civilianRisk);
        Assert.AreEqual("Daily Pressure Report", state.pendingEvents[0].title);
        Assert.AreEqual(3, state.pendingEvents.Length);
    }

    [Test]
    public void OperationRuntime_PersistsActionAndEndDayThroughSaveService()
    {
        string saveRoot = Path.Combine(Path.GetTempPath(), "WarlineCaptureOperationRuntimeTests", System.Guid.NewGuid().ToString("N"));
        var saveService = new SaveService(new JsonSaveRepository(saveRoot));
        try
        {
            WarlineCaptureOperationRuntime.SetSaveServiceForTests(saveService);

            WarlineCaptureOperationRuntime.ApplyAction(OperationActionType.Scan);
            WarlineCaptureOperationRuntime.EndDay();

            WarlineCaptureOperationRuntime.ClearCachedStateForTests();

            OperationSaveData restored = WarlineCaptureOperationRuntime.State;

            Assert.AreEqual(2, restored.operationDay);
            Assert.AreEqual(4, restored.operationSupplies);
            Assert.AreEqual(1, restored.completedActions);
            Assert.AreEqual(44, restored.districts[0].intel);
            Assert.AreEqual(71, restored.districts[0].threat);
            Assert.AreEqual(53, restored.districts[0].trust);
            Assert.AreEqual(31, restored.districts[0].security);
            Assert.AreEqual(45, restored.districts[0].heat);
            Assert.AreEqual(4, restored.pendingEvents.Length);
            Assert.AreEqual("Enemy Influence", restored.pendingEvents[3].sourceMetric);
            Assert.AreEqual(1, restored.intelEvidence.Length);
            Assert.AreEqual(44, restored.intelEvidence[0].confidence);
        }
        finally
        {
            WarlineCaptureOperationRuntime.ResetForTests();
            if (Directory.Exists(saveRoot))
                Directory.Delete(saveRoot, true);
        }
    }

    [Test]
    public void OperationRuntime_NormalizesIncompleteSavedOperationState()
    {
        string saveRoot = Path.Combine(Path.GetTempPath(), "WarlineCaptureOperationRuntimeTests", System.Guid.NewGuid().ToString("N"));
        var saveService = new SaveService(new JsonSaveRepository(saveRoot));
        try
        {
            saveService.SaveOperation(new OperationSaveData
            {
                operationDay = 0,
                operationSupplies = 0,
                districts = new[] { new DistrictStateData { districtId = "custom_district", stability = 10, threat = 20, intel = 30 } }
            });

            WarlineCaptureOperationRuntime.SetSaveServiceForTests(saveService);

            OperationSaveData restored = WarlineCaptureOperationRuntime.State;

            Assert.AreEqual(1, restored.operationDay);
            Assert.AreEqual(4, restored.operationSupplies);
            Assert.AreEqual(10, restored.districts[0].trust);
            Assert.AreEqual(80, restored.districts[0].security);
            Assert.AreEqual(13, restored.districts[0].heat);
            Assert.AreEqual(55, restored.districts[0].civilianRisk);
            Assert.AreEqual("custom_district", WarlineCaptureOperationRuntime.SelectedDistrictId);
        }
        finally
        {
            WarlineCaptureOperationRuntime.ResetForTests();
            if (Directory.Exists(saveRoot))
                Directory.Delete(saveRoot, true);
        }
    }
}
