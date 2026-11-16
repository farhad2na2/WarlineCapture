using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Components;
using Newtonsoft.Json;
using Unity.Entities;

// Passive diagnostic sink used only by the generated S073 source-owner copy.
// An absent opt-in path makes every call a no-op.
internal static class SourceS073AIProductionDecisionTelemetry
{
    const string OptIn = "WARLINE_S073_AI_PRODUCTION_DECISION_TELEMETRY_FILE";
    static string _activePath;
    static readonly Dictionary<string, int> IntervalGateCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    static readonly HashSet<string> EmptyPlanRecorded = new HashSet<string>(StringComparer.Ordinal);
    static readonly HashSet<string> ConsumerStatesRecorded = new HashSet<string>(StringComparer.Ordinal);
    static readonly Dictionary<string, int> RefundAttempts = new Dictionary<string, int>(StringComparer.Ordinal);
    static readonly List<object> PendingRows = new List<object>();
    static string _writeFailure;

    static bool Enabled => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(OptIn));

    internal static void RecordIntervalGate(int planEntityIndex, int planEntityVersion)
    {
        if (!Enabled) return;
        string key = PlanKey(planEntityIndex, planEntityVersion);
        IntervalGateCounts.TryGetValue(key, out int count);
        IntervalGateCounts[key] = count == int.MaxValue ? count : count + 1;
    }

    internal static void RecordLifecycle(
        string phase, float time, int boundaryIndex = -1, int boundaryVersion = 0,
        int planIndex = -1, int planVersion = 0, int factionId = -1, int requestId = -1,
        string unitId = "", int status = -1, int resultCode = -1, string failureCause = "",
        int requestOwnerIndex = -1, int requestOwnerVersion = 0, string requestAttempt = "",
        int requestCountBefore = -1, int requestCountAfter = -1,
        int moneyBefore = -1, int moneyAfter = -1, int materialsBefore = -1, int materialsAfter = -1,
        string resourceResult = "NotObserved", int resourcesReserved = -1,
        int creditsCost = -1, int materialsCost = -1, string producer = "",
        int queueCount = -1, int producedCount = -1, int producerBuildingId = -1,
        int productionIndex = -1, string unitPrefab = "", int queueCapacityLimit = -1, int refundAttemptNumber = -1)
    {
        if (!Enabled) return;
        PendingRows.Add(new
        {
            kind = "S073AIProductionLifecycle", schema_version = 1, phase,
            time_seconds = time.ToString("R", CultureInfo.InvariantCulture),
            boundary_entity_index = boundaryIndex, boundary_entity_version = boundaryVersion,
            plan_entity_index = planIndex, plan_entity_version = planVersion, faction_id = factionId,
            request_id = requestId, unit_id = unitId ?? string.Empty,
            request_status = status, request_status_label = StatusLabel(status),
            result_code = resultCode, result_code_label = ResultCodeLabel(resultCode), failure_cause = failureCause,
            request_owner_entity_index = requestOwnerIndex, request_owner_entity_version = requestOwnerVersion,
            request_attempt = requestAttempt ?? string.Empty,
            refund_attempt_number = refundAttemptNumber,
            request_count_before = requestCountBefore, request_count_after = requestCountAfter,
            money_before = moneyBefore, money_after = moneyAfter,
            tactical_materials_before = materialsBefore, tactical_materials_after = materialsAfter,
            resource_result = resourceResult, resources_reserved = resourcesReserved,
            credits_cost = creditsCost, materials_cost = materialsCost, producer,
            queue_count = queueCount, produced_count = producedCount, queue_capacity_limit = queueCapacityLimit,
            producer_building_id = producerBuildingId, production_index = productionIndex,
            unit_prefab = unitPrefab ?? string.Empty,
            telemetry_source = "Generated source-only copies of original AIProductionSystem and actual S073 request/queue owners"
        });
    }

    internal static void RecordConsumerSnapshot(EntityManager em, Entity boundary, float time)
    {
        if (!Enabled ||
            !em.Exists(boundary) || !em.HasBuffer<BuildingFactionUnitProductionRequest>(boundary)) return;
        DynamicBuffer<BuildingFactionUnitProductionRequest> requests =
            em.GetBuffer<BuildingFactionUnitProductionRequest>(boundary, true);
        for (int i = 0; i < requests.Length; i++)
        {
            BuildingFactionUnitProductionRequest request = requests[i];
            if (request.Status == BuildingFactionUnitProductionRequest.Pending) continue;
            string key = ReceiptKey(boundary.Index, boundary.Version, request.RequestId) + ":" +
                request.Status.ToString(CultureInfo.InvariantCulture) + ":" + request.ResultCode.ToString(CultureInfo.InvariantCulture);
            if (!ConsumerStatesRecorded.Add(key)) continue;
            RecordLifecycle("consumer_terminal_receipt", time, boundary.Index, boundary.Version,
                factionId: request.FactionId, requestId: request.RequestId, unitId: request.UnitId.ToString(),
                status: request.Status, resultCode: request.ResultCode, resourcesReserved: request.ResourcesReserved,
                requestOwnerIndex: request.RequestOwner == Entity.Null ? -1 : request.RequestOwner.Index,
                requestOwnerVersion: request.RequestOwner == Entity.Null ? 0 : request.RequestOwner.Version,
                requestAttempt: request.RequestAttempt.ToString(),
                creditsCost: request.Cost, materialsCost: request.MaterialsCost,
                producer: request.ProducerDisplayName.ToString(), queueCount: request.QueueCount,
                producedCount: request.ProducedCount, requestCountAfter: requests.Length);
        }
    }

    internal static void RecordRefundAttempt(
        float time, int boundaryIndex, int boundaryVersion, BuildingFactionUnitProductionRequest request,
        string result, int moneyBefore, int moneyAfter, int materialsBefore, int materialsAfter)
    {
        if (!Enabled) return;
        string key = ReceiptKey(boundaryIndex, boundaryVersion, request.RequestId);
        RefundAttempts.TryGetValue(key, out int attempts);
        attempts = attempts == int.MaxValue ? attempts : attempts + 1;
        RefundAttempts[key] = attempts;
        RecordLifecycle("refund_attempt", time, boundaryIndex, boundaryVersion,
                factionId: request.FactionId, requestId: request.RequestId, unitId: request.UnitId.ToString(),
                status: request.Status, resultCode: request.ResultCode, failureCause: result,
                requestOwnerIndex: request.RequestOwner == Entity.Null ? -1 : request.RequestOwner.Index,
                requestOwnerVersion: request.RequestOwner == Entity.Null ? 0 : request.RequestOwner.Version,
                requestAttempt: request.RequestAttempt.ToString(),
                moneyBefore: moneyBefore, moneyAfter: moneyAfter, materialsBefore: materialsBefore,
                materialsAfter: materialsAfter, resourceResult: result, resourcesReserved: request.ResourcesReserved,
                creditsCost: request.Cost, materialsCost: request.MaterialsCost,
                producer: request.ProducerDisplayName.ToString(), queueCount: request.QueueCount,
                producedCount: request.ProducedCount, refundAttemptNumber: attempts);
        if (string.Equals(result, "Applied", StringComparison.Ordinal))
        {
            RefundAttempts.Remove(key);
        }
    }

    internal static void Record(
        float time, int planEntityIndex, int planEntityVersion, byte factionId,
        string decision, string reason, int candidateIndex, string unitId,
        int candidateAttempts, int capacitySkippedCandidates, int targetReachedCandidates,
        int queueLimitCandidates, int emptyUnitIdCandidates,
        int producedCount, int queuedCount, int money, int materials, int creditsCost, int materialsCost,
        string resourceResult, float intervalSeconds, float elapsedSinceProduction,
        string fuelSupport = "NotEvaluated", bool? fuelSupported = null,
        int targetProducedUnits = 0, int maxQueuedUnits = 0)
    {
        if (!Enabled) return;
        string planKey = PlanKey(planEntityIndex, planEntityVersion);
        IntervalGateCounts.TryGetValue(planKey, out int intervalGateUpdates);
        IntervalGateCounts.Remove(planKey);
        if (reason == "NoPlanEntries" && !EmptyPlanRecorded.Add(planKey)) return;

        var row = new
        {
            kind = "S073AIProductionDecision",
            schema_version = 1,
            decision_time_seconds = time.ToString("R", CultureInfo.InvariantCulture),
            plan_entity_index = planEntityIndex,
            plan_entity_version = planEntityVersion,
            faction_id = factionId,
            decision_enum = decision,
            reason,
            candidate_index = candidateIndex,
            unit_id = unitId ?? string.Empty,
            candidate_attempts = candidateAttempts,
            capacity_skipped_candidates = capacitySkippedCandidates,
            target_reached_candidates = targetReachedCandidates,
            queue_limit_candidates = queueLimitCandidates,
            empty_unit_id_candidates = emptyUnitIdCandidates,
            produced_count = producedCount,
            queued_count = queuedCount,
            interval_gate_updates_since_previous_decision = intervalGateUpdates,
            target_produced_units = targetProducedUnits,
            max_queued_units = maxQueuedUnits,
            money,
            tactical_materials = materials,
            credits_cost = creditsCost,
            materials_cost = materialsCost,
            resource_evaluation = resourceResult,
            fuel_support = fuelSupport,
            fuel_support_check_passed = fuelSupported,
            interval_seconds = intervalSeconds.ToString("R", CultureInfo.InvariantCulture),
            seconds_since_last_production = elapsedSinceProduction.ToString("R", CultureInfo.InvariantCulture),
            telemetry_source = "Generated exact-anchor copy of Assets/Game/Scripts/Systems/AIProductionSystem.cs"
        };
        PendingRows.Add(row);
    }

    internal static void Flush()
    {
        string path = Environment.GetEnvironmentVariable(OptIn);
        if (string.IsNullOrWhiteSpace(path) || PendingRows.Count == 0 || _writeFailure != null) return;
        path = Path.GetFullPath(path);
        try
        {
            EnsureOutput(path);
            foreach (object row in PendingRows)
            {
                string json = JsonConvert.SerializeObject(row, Formatting.None) + "\n";
                using var stream = new FileStream(path, _activePath == path ? FileMode.Append : FileMode.CreateNew,
                    FileAccess.Write, FileShare.Read);
                _activePath = path;
                using var writer = new StreamWriter(stream);
                writer.Write(json);
            }
            PendingRows.Clear();
        }
        catch (Exception error)
        {
            _writeFailure = error.GetType().Name + ": " + error.Message;
        }
    }

    internal static void ThrowIfFailed()
    {
        if (_writeFailure != null)
            throw new IOException("S073 AI production telemetry write failed after the owner step: " + _writeFailure);
    }

    static void EnsureOutput(string path)
    {
        if (_activePath != null)
        {
            if (!string.Equals(_activePath, path, StringComparison.Ordinal))
                throw new IOException("S073 AI production decision telemetry output path changed during one source run.");
            return;
        }
        if (File.Exists(path))
            throw new IOException("S073 AI production decision telemetry requires a fresh output file: " + path);
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
    }

    static string PlanKey(int index, int version) => index.ToString(CultureInfo.InvariantCulture) + ":" +
        version.ToString(CultureInfo.InvariantCulture);

    static string ReceiptKey(int boundaryIndex, int boundaryVersion, int requestId) =>
        PlanKey(boundaryIndex, boundaryVersion) + ":" + requestId.ToString(CultureInfo.InvariantCulture);

    static string StatusLabel(int value) => value == BuildingFactionUnitProductionRequest.Pending ? "Pending" :
        value == BuildingFactionUnitProductionRequest.Succeeded ? "Succeeded" :
        value == BuildingFactionUnitProductionRequest.Failed ? "Failed" : "Unknown";

    static string ResultCodeLabel(int value) => value == 0 ? "None" :
        value == BuildingFactionUnitProductionRequest.MissingUnitConfig ? "MissingUnitConfig" :
        value == BuildingFactionUnitProductionRequest.MissingProducerBuilding ? "MissingProducerBuilding" :
        value == BuildingFactionUnitProductionRequest.ProducerUnavailable ? "ProducerUnavailable" : "Unknown";
}
