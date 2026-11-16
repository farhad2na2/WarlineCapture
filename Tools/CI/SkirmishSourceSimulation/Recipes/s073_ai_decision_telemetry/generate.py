#!/usr/bin/env python3
"""Generate an exact-anchor diagnostic copy of AIProductionSystem."""
from __future__ import annotations

import hashlib
import importlib.util
from pathlib import Path

SOURCE = "Assets/Game/Scripts/Systems/AIProductionSystem.cs"
OUTPUT = "AIProductionSystem.DecisionTelemetry.cs"
PARTIAL_SOURCE = "Assets/Game/Scripts/Systems/AIProductionSystem.ProductionRequests.cs"
PARTIAL_OUTPUT = "AIProductionSystem.ProductionRequests.DecisionTelemetry.cs"
RUNTIME_SOURCE = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyProductionRuntime.cs"
RUNTIME_OUTPUT = "SourceS073LegacyProductionRuntime.DecisionTelemetry.cs"
FACTION_RECIPE = "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/regenerate_faction_queue.py"
DELIVERY_RECIPE = "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/generate.py"

FACTION_INPUTS = [
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Runtime/Skirmish/SkirmishNativeProduction.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
]
FACTION_HELPERS = [
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/BuildingProductionQueueCompositionSystemHelper.timeline.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/FactionRequestKernel.source.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/SkirmishNativeProduction.typed.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/SourceFactionProductionDispatcher.cs.txt",
]
DELIVERY_INPUTS = [
    "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs",
]
DELIVERY_HELPERS = [
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/complete_heli_fragment.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/engine-binding-contract.json",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/ground-bridge.cs.template",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/helicopter-bridge.cs.template",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/native-method-contract.json",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/partial_generate.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/prepare_ground.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/prepare_heli.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/prepare_queue.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/queue-bridge.cs.template",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/helpers/source_extract.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/recipe.json",
]
GENERATOR_HELPERS = list(dict.fromkeys([FACTION_RECIPE, DELIVERY_RECIPE] + FACTION_HELPERS + DELIVERY_HELPERS))
GENERATOR_INPUTS = list(dict.fromkeys([SOURCE, PARTIAL_SOURCE, RUNTIME_SOURCE] + FACTION_INPUTS + DELIVERY_INPUTS))


def _replace_once(text: str, before: str, after: str, label: str) -> str:
    count = text.count(before)
    if count != 1:
        raise ValueError(f"Expected exactly one {label} source anchor; found {count}")
    return text.replace(before, after, 1)


def _sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _load_recipe(repo: Path, relative: str, module_name: str):
    path = repo / relative
    spec = importlib.util.spec_from_file_location(module_name, path)
    if spec is None or spec.loader is None:
        raise ValueError("Unable to load exact current source recipe: " + relative)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def generate(repo: Path, output: Path) -> dict:
    source_path = repo / SOURCE
    original = source_path.read_text(encoding="utf-8")
    text = original

    text = _replace_once(text,
        "                    ArchetypeChunk chunk = planChunks[chunkIndex];\n"
        "                    NativeArray<AIProductionPlan> plans = chunk.GetNativeArray(ref _aiProductionPlanType);",
        "                    ArchetypeChunk chunk = planChunks[chunkIndex];\n"
        "                    NativeArray<Entity> planEntities = chunk.GetNativeArray(_entityType);\n"
        "                    NativeArray<AIProductionPlan> plans = chunk.GetNativeArray(ref _aiProductionPlanType);",
        "plan identity capture")

    text = _replace_once(text,
        "                        ProcessProductionPlan(\n"
        "                            ref state,\n"
        "                            boundaryEntity,\n"
        "                            ref plan,",
        "                        ProcessProductionPlan(\n"
        "                            ref state,\n"
        "                            boundaryEntity,\n"
        "                            planEntities[planIndex],\n"
        "                            ref plan,",
        "plan identity forwarding")

    text = _replace_once(text,
        "            Entity boundaryEntity,\n"
        "            ref AIProductionPlan plan,",
        "            Entity boundaryEntity,\n"
        "            Entity planEntity,\n"
        "            ref AIProductionPlan plan,",
        "plan identity parameter")

    text = _replace_once(text,
        "            public int Cost;\n        }",
        "            public int Cost;\n"
        "            public int CandidateAttempts;\n"
        "            public int CapacitySkippedCandidates;\n"
        "            public int TargetReachedCandidates;\n"
        "            public int QueueLimitCandidates;\n"
        "            public int EmptyUnitIdCandidates;\n"
        "            public int ProducedCount;\n"
        "            public int QueuedCount;\n"
        "            public FactionConstructionResourceMutationResult ResourceResult;\n"
        "            public byte FuelSupported;\n            public byte FuelCheckRequired;\n            public byte FuelCheckEvaluated;\n        }",
        "decision payload extension")

    text = _replace_once(text,
        "            float interval = math.max(0.1f, plan.UnitProductionIntervalSeconds);\n"
        "            if (now - plan.LastProductionTime < interval)\n"
        "                return;",
        "            float interval = math.max(0.1f, plan.UnitProductionIntervalSeconds);\n"
        "            if (now - plan.LastProductionTime < interval)\n"
        "            {\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordIntervalGate(planEntity.Index, planEntity.Version);\n"
        "                return;\n"
        "            }",
        "interval gate")

    text = _replace_once(text,
        "            if (entries.Length == 0)\n            {\n                LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);\n                return;\n            }",
        "            if (entries.Length == 0)\n            {\n"
        "                global::SourceS073AIProductionDecisionTelemetry.Record(now, planEntity.Index, planEntity.Version, plan.FactionId, \"None\", \"NoPlanEntries\", -1, string.Empty, 0, 0, 0, 0, 0, 0, 0, economy.Money, materials.Current, 0, 0, \"NotEvaluated\", interval, now - plan.LastProductionTime);\n"
        "                LogNoPlanIfNeeded(ref state, ref plan, now, shouldLog);\n                return;\n            }",
        "empty plan gate")

    text = _replace_once(text,
        "            ProductionDecision decision = SelectProductionDecision(ref state, boundaryEntity, entries, plan, economy, materials);\n"
        "            bool handledDecision = decision.Result != ProductionDecisionResult.None;",
        "            ProductionDecision decision = SelectProductionDecision(ref state, boundaryEntity, entries, plan, economy, materials);\n"
        "            string telemetryReason = decision.Result == ProductionDecisionResult.None\n"
        "                ? decision.EmptyUnitIdCandidates == decision.CandidateAttempts && decision.CandidateAttempts > 0 ? \"EmptyUnitIds\"\n"
        "                    : decision.CapacitySkippedCandidates == decision.CandidateAttempts && decision.CandidateAttempts > 0 ? \"CapacityReached\" : \"NoEligibleCandidate\"\n"
        "                : decision.Result.ToString();\n"
        "            global::SourceS073AIProductionDecisionTelemetry.Record(now, planEntity.Index, planEntity.Version, plan.FactionId, decision.Result.ToString(), telemetryReason, decision.EntryIndex, decision.UnitId.ToString(), decision.CandidateAttempts, decision.CapacitySkippedCandidates, decision.TargetReachedCandidates, decision.QueueLimitCandidates, decision.EmptyUnitIdCandidates, decision.ProducedCount, decision.QueuedCount, economy.Money, materials.Current, decision.Cost, decision.Unit.Price, decision.Result == ProductionDecisionResult.InsufficientFunds || decision.Result == ProductionDecisionResult.Request ? decision.ResourceResult.ToString() : \"NotEvaluated\", interval, now - plan.LastProductionTime, decision.FuelCheckEvaluated == 0 ? \"NotEvaluated\" : decision.FuelCheckRequired == 0 ? \"NotRequired\" : decision.Result == ProductionDecisionResult.InsufficientFuel ? \"Unavailable\" : decision.FuelSupported != 0 ? \"Available\" : \"NotEvaluated\", decision.FuelCheckEvaluated != 0 && decision.FuelCheckRequired != 0 ? decision.FuelSupported != 0 : (bool?)null, plan.TargetProducedUnits, plan.MaxQueuedUnits);\n"
        "            bool handledDecision = decision.Result != ProductionDecisionResult.None;",
        "decision-time record")

    text = _replace_once(text,
        "                int candidateIndex = PositiveModulo(plan.NextUnitIndex + attempt, entries.Length);\n"
        "                FixedString64Bytes unitId = entries[candidateIndex].UnitId;\n"
        "                if (unitId.Length == 0)\n                    continue;",
        "                decision.CandidateAttempts++;\n"
        "                int candidateIndex = PositiveModulo(plan.NextUnitIndex + attempt, entries.Length);\n"
        "                FixedString64Bytes unitId = entries[candidateIndex].UnitId;\n"
        "                if (unitId.Length == 0)\n                {\n                    decision.EmptyUnitIdCandidates++;\n                    continue;\n                }",
        "candidate attempt and empty id")

    text = _replace_once(text,
        "                if (producedCount + queuedCount >= targetProducedUnits ||\n"
        "                    queuedCount >= maxQueuedUnits)\n                {\n                    continue;\n                }",
        "                if (producedCount + queuedCount >= targetProducedUnits ||\n"
        "                    queuedCount >= maxQueuedUnits)\n                {\n"
        "                    decision.CapacitySkippedCandidates++;\n"
        "                    if (producedCount + queuedCount >= targetProducedUnits) decision.TargetReachedCandidates++;\n"
        "                    if (queuedCount >= maxQueuedUnits) decision.QueueLimitCandidates++;\n"
        "                    decision.ProducedCount = producedCount;\n"
        "                    decision.QueuedCount = queuedCount;\n"
        "                    continue;\n                }",
        "capacity skip")

    text = _replace_once(text,
        "                decision.EntryIndex = candidateIndex;\n                decision.UnitId = unitId;",
        "                decision.EntryIndex = candidateIndex;\n                decision.UnitId = unitId;\n"
        "                decision.ProducedCount = producedCount;\n"
        "                decision.QueuedCount = queuedCount;",
        "selected candidate counts")

    text = _replace_once(text,
        "                if (!HasFuelSupportForProduction(unit, plan.FactionId, factionSummaries, usableFuelSummaries))\n                {\n                    decision.Result = ProductionDecisionResult.InsufficientFuel;",
        "                decision.FuelCheckRequired = unit.IsVehicle != 0 ? (byte)1 : (byte)0;\n"
        "                bool fuelSupportAvailable = HasFuelSupportForProduction(unit, plan.FactionId, factionSummaries, usableFuelSummaries);\n"
        "                decision.FuelCheckEvaluated = 1;\n"
        "                if (!fuelSupportAvailable)\n                {\n                    decision.FuelSupported = 0;\n                    decision.Result = ProductionDecisionResult.InsufficientFuel;",
        "fuel failure")
    text = _replace_once(text,
        "                if (FactionConstructionResourceUtilitySystemHelper.Evaluate(economy, materials, cost, unit.Price) !=\n                    FactionConstructionResourceMutationResult.Applied)\n                {\n                    decision.Result = ProductionDecisionResult.InsufficientFunds;",
        "                decision.FuelSupported = 1;\n"
        "                decision.ResourceResult = FactionConstructionResourceUtilitySystemHelper.Evaluate(economy, materials, cost, unit.Price);\n"
        "                if (decision.ResourceResult != FactionConstructionResourceMutationResult.Applied)\n                {\n                    decision.Result = ProductionDecisionResult.InsufficientFunds;",
        "resource evaluation")

    text = _replace_once(text,
        "                        ProcessCompletedProductionRequests(ref state, boundaryEntity, ref economy, ref materials, shouldLog);",
        "                        ProcessCompletedProductionRequests(ref state, boundaryEntity, ref economy, ref materials, now, shouldLog);",
        "completed receipt clock forwarding")

    text = _replace_once(text,
        "                    if (FactionConstructionResourceUtilitySystemHelper.TrySpend(\n"
        "                            ref economy, ref materials, decision.Cost, decision.Unit.Price) !=\n"
        "                        FactionConstructionResourceMutationResult.Applied)\n"
        "                        break;\n"
        "                    EnqueueProductionRequest(ref state, boundaryEntity, plan.FactionId, decision.UnitId, decision.Cost, decision.Unit.Price);",
        "                    int moneyBeforeSpend = economy.Money;\n"
        "                    int materialsBeforeSpend = materials.Current;\n"
        "                    FactionConstructionResourceMutationResult spendResult = FactionConstructionResourceUtilitySystemHelper.TrySpend(\n"
        "                        ref economy, ref materials, decision.Cost, decision.Unit.Price);\n"
        "                    global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"try_spend_return\", now, boundaryEntity.Index, boundaryEntity.Version, planEntity.Index, planEntity.Version, plan.FactionId, unitId: decision.UnitId.ToString(), moneyBefore: moneyBeforeSpend, moneyAfter: economy.Money, materialsBefore: materialsBeforeSpend, materialsAfter: materials.Current, resourceResult: spendResult.ToString(), creditsCost: decision.Cost, materialsCost: decision.Unit.Price);\n"
        "                    if (spendResult != FactionConstructionResourceMutationResult.Applied)\n"
        "                        break;\n"
        "                    EnqueueProductionRequest(ref state, boundaryEntity, plan.FactionId, decision.UnitId, decision.Cost, decision.Unit.Price, now, planEntity);",
        "spend result and request enqueue")

    generated = output / OUTPUT
    generated.parent.mkdir(parents=True, exist_ok=True)
    generated.write_text(text, encoding="utf-8")

    return _generate_lifecycle_sources(repo, output, text)


def _generate_partial_copy(repo: Path, output: Path, source_pins: dict[str, str]) -> str:
    path = repo / PARTIAL_SOURCE
    original = path.read_text(encoding="utf-8")
    source_pins[PARTIAL_SOURCE] = _sha(path)
    text = _replace_once(original,
        "            ref FactionTacticalMaterialsComponent materials,\n"
        "            bool shouldLog)",
        "            ref FactionTacticalMaterialsComponent materials,\n"
        "            float now,\n"
        "            bool shouldLog)",
        "receipt time parameter")
    text = _replace_once(text,
        "        private void EnqueueProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, FixedString128Bytes unitId, int creditsCost, int materialsCost)",
        "        private void EnqueueProductionRequest(ref SystemState state, Entity boundaryEntity, byte factionId, FixedString128Bytes unitId, int creditsCost, int materialsCost, float now, Entity planEntity)",
        "enqueue event parameters")
    text = _replace_once(text,
        "            requests.Add(new BuildingFactionUnitProductionRequest\n            {",
        "            int requestCountBefore = requests.Length;\n            requests.Add(new BuildingFactionUnitProductionRequest\n            {",
        "existing request buffer count")
    text = _replace_once(text,
        "                ResourcesReserved = 1\n            });",
        "                ResourcesReserved = 1\n            });\n"
        "            BuildingFactionUnitProductionRequest appendedRequest = requests[requests.Length - 1];\n"
        "            global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"request_enqueued\", now, boundaryEntity.Index, boundaryEntity.Version, planEntity.Index, planEntity.Version, factionId, appendedRequest.RequestId, appendedRequest.UnitId.ToString(), appendedRequest.Status, appendedRequest.ResultCode, requestCountBefore: requestCountBefore, requestCountAfter: requests.Length, resourcesReserved: appendedRequest.ResourcesReserved, creditsCost: appendedRequest.Cost, materialsCost: appendedRequest.MaterialsCost);",
        "actual appended request observation")
    text = _replace_once(text,
        "                if (request.ResourcesReserved != 0)\n"
        "                {\n"
        "                    if (request.Status != BuildingFactionUnitProductionRequest.Succeeded &&\n"
        "                        FactionConstructionResourceUtilitySystemHelper.TryRollback(\n"
        "                            ref economy, ref materials, request.Cost, request.MaterialsCost) !=\n"
        "                        FactionConstructionResourceMutationResult.Applied)\n"
        "                        continue; // Keep the receipt until both resources can be restored.\n"
        "                }\n"
        "                else if (request.Status == BuildingFactionUnitProductionRequest.Succeeded)\n"
        "                    economy.Money = math.max(0, economy.Money - math.max(0, request.Cost));",
        "                if (request.ResourcesReserved != 0)\n"
        "                {\n"
        "                    if (request.Status != BuildingFactionUnitProductionRequest.Succeeded)\n"
        "                    {\n"
        "                        int moneyBeforeRefund = economy.Money;\n"
        "                        int materialsBeforeRefund = materials.Current;\n"
        "                        FactionConstructionResourceMutationResult refundResult =\n"
        "                            FactionConstructionResourceUtilitySystemHelper.TryRollback(\n"
        "                                ref economy, ref materials, request.Cost, request.MaterialsCost);\n"
        "                        global::SourceS073AIProductionDecisionTelemetry.RecordRefundAttempt(\n"
        "                            now, boundaryEntity.Index, boundaryEntity.Version, request, refundResult.ToString(),\n"
        "                            moneyBeforeRefund, economy.Money, materialsBeforeRefund, materials.Current);\n"
        "                        if (refundResult != FactionConstructionResourceMutationResult.Applied)\n"
        "                            continue; // Keep the receipt until both resources can be restored.\n"
        "                    }\n"
        "                }\n"
        "                else if (request.Status == BuildingFactionUnitProductionRequest.Succeeded)\n"
        "                {\n"
        "                    int moneyBeforeReceiptCharge = economy.Money;\n"
        "                    economy.Money = math.max(0, economy.Money - math.max(0, request.Cost));\n"
        "                    global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"successful_unreserved_receipt_charge\", now, boundaryEntity.Index, boundaryEntity.Version, factionId: request.FactionId, requestId: request.RequestId, unitId: request.UnitId.ToString(), status: request.Status, resultCode: request.ResultCode, moneyBefore: moneyBeforeReceiptCharge, moneyAfter: economy.Money, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);\n"
        "                }",
        "exact refund/settlement return")
    text = _replace_once(text,
        "                requests.RemoveAt(i);",
        "                int requestCountBeforeRemoval = requests.Length;\n"
        "                requests.RemoveAt(i);\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"receipt_removed\", now, boundaryEntity.Index, boundaryEntity.Version, factionId: request.FactionId, requestId: request.RequestId, unitId: request.UnitId.ToString(), status: request.Status, resultCode: request.ResultCode, requestCountBefore: requestCountBeforeRemoval, requestCountAfter: requests.Length, moneyBefore: economy.Money, moneyAfter: economy.Money, materialsBefore: materials.Current, materialsAfter: materials.Current, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost, producer: request.ProducerDisplayName.ToString(), queueCount: request.QueueCount, producedCount: request.ProducedCount, requestOwnerIndex: request.RequestOwner == Entity.Null ? -1 : request.RequestOwner.Index, requestOwnerVersion: request.RequestOwner == Entity.Null ? 0 : request.RequestOwner.Version, requestAttempt: request.RequestAttempt.ToString());",
        "receipt removal")
    (output / PARTIAL_OUTPUT).write_text(text, encoding="utf-8")
    return text


def _generate_runtime_copy(repo: Path, output: Path, source_pins: dict[str, str]) -> str:
    path = repo / RUNTIME_SOURCE
    original = path.read_text(encoding="utf-8")
    source_pins[RUNTIME_SOURCE] = _sha(path)
    text = _replace_once(original,
        "            dispatcher.ProcessLegacyS073Requests(now);\n"
        "            RequestProcessingUpdates++;",
        "            dispatcher.ProcessLegacyS073Requests(now);\n"
        "            RequestProcessingUpdates++;\n"
        "            delivery.Advance(now, delta);\n"
        "            DeliveryUpdates++;\n"
        "            global::SourceS073AIProductionDecisionTelemetry.RecordConsumerSnapshot(em, boundary, now);\n"
        "            global::SourceS073AIProductionDecisionTelemetry.Flush();\n"
        "            global::SourceS073AIProductionDecisionTelemetry.ThrowIfFailed();",
        "post-owner safe flush")
    (output / RUNTIME_OUTPUT).write_text(text, encoding="utf-8")
    return text


def _instrument_faction_kernel(text: str) -> str:
    # This is the original generated QueueFactionUnitProductionRequest body, with
    # one trace at each existing return. The policy call sites remain single-call.
    text = _replace_once(text,
        "            if (!GeneratedEnemyProductionRequests.RequestAttemptActive(entityManager, request))\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                return false;\n"
        "            }",
        "            if (!GeneratedEnemyProductionRequests.RequestAttemptActive(entityManager, request))\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: \"RequestAttemptInactive\", resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);\n"
        "                return false;\n"
        "            }",
        "request attempt gate")

    text = _replace_once(text,
        "            if (!TryResolveConfiguredUnit(context, unitId, unitIdIsNormalized, out GameObject unitPrefab, out string unitDisplayName, out int unitPrice, out bool canRequest) ||\n"
        "                unitPrefab == null ||\n"
        "                !canRequest)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.MissingUnitConfig;",
        "            bool configuredUnitResolved = TryResolveConfiguredUnit(context, unitId, unitIdIsNormalized, out GameObject unitPrefab, out string unitDisplayName, out int unitPrice, out bool canRequest);\n"
        "            bool configuredPrefabPresent = unitPrefab != null;\n"
        "            if (!configuredUnitResolved || !configuredPrefabPresent || !canRequest)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.MissingUnitConfig;\n"
        "                string configFailureCause = !configuredUnitResolved ? \"ConfiguredUnitUnresolved\" : !configuredPrefabPresent ? \"ConfiguredPrefabMissing\" : \"CanRequestFalse\";\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: configFailureCause, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);",
        "configured-unit failure cause")

    text = _replace_once(text,
        "            if (!TryFindFirstFactionProducerBuilding(context, factionId, unitPrefab, out int producerBuildingId, out int productionIndex, out string producerDisplayName))\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.MissingProducerBuilding;",
        "            if (!TryFindFirstFactionProducerBuilding(context, factionId, unitPrefab, out int producerBuildingId, out int productionIndex, out string producerDisplayName))\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.MissingProducerBuilding;\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: \"NoQueueableFactionProducer\", resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);",
        "producer selection failure cause")

    text = _replace_once(text,
        "            if (context.RuntimeBuildings == null ||\n"
        "                !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingEntity producerBuilding) ||\n"
        "                producerBuilding == null)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;",
        "            if (context.RuntimeBuildings == null ||\n"
        "                !context.RuntimeBuildings.TryGetValue(producerBuildingId, out RuntimeBuildingEntity producerBuilding) ||\n"
        "                producerBuilding == null)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: \"ProducerRuntimeEntityMissing\", producerBuildingId: producerBuildingId, productionIndex: productionIndex, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);",
        "runtime producer failure cause")

    text = _replace_once(text,
        "            if (!SourceCampPopulationPolicy.CanQueue(entityManager,context.RuntimeBuildings,producerBuilding,unitPrefab,productionIndex) ||\n"
        "                context.ProductionSystem == null ||\n"
        "                !context.ProductionSystem.TryQueuePlayerUnitFromBuilding(\n"
        "                    context.ProductionQueueContext,\n"
        "                    producerBuilding,\n"
        "                    productionIndex,\n"
        "                    unitPrefab,\n"
        "                    entityManager,\n"
        "                    now))\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                return false;\n"
        "            }",
        "            bool populationAllowsQueue = SourceCampPopulationPolicy.CanQueue(entityManager,context.RuntimeBuildings,producerBuilding,unitPrefab,productionIndex);\n"
        "            if (!populationAllowsQueue)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: \"PopulationQueuePolicyRejected\", producer: producerDisplayName, producerBuildingId: producerBuildingId, productionIndex: productionIndex, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);\n"
        "                return false;\n"
        "            }\n"
        "            if (context.ProductionSystem == null)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: \"ProductionQueueOwnerMissing\", producer: producerDisplayName, producerBuildingId: producerBuildingId, productionIndex: productionIndex, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);\n"
        "                return false;\n"
        "            }\n"
        "            bool queueOwnerAccepted = context.ProductionSystem.TryQueuePlayerUnitFromBuilding(\n"
        "                context.ProductionQueueContext, producerBuilding, productionIndex, unitPrefab, entityManager, now);\n"
        "            if (!queueOwnerAccepted)\n"
        "            {\n"
        "                request.ResultCode = BuildingFactionUnitProductionRequest.ProducerUnavailable;\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"consumer_rejected_request\", now, factionId: factionId, requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode, failureCause: \"ProductionDeliveryOwnerRejected\", producer: producerDisplayName, producerBuildingId: producerBuildingId, productionIndex: productionIndex, resourcesReserved: request.ResourcesReserved, creditsCost: request.Cost, materialsCost: request.MaterialsCost);\n"
        "                return false;\n"
        "            }",
        "population and queue-owner failure cause")

    identity_anchor = "requestId: request.RequestId, unitId: unitId, status: request.Status, resultCode: request.ResultCode,"
    identity_count = text.count(identity_anchor)
    if identity_count < 6:
        raise ValueError("Expected all source consumer failure records to carry exact request identity fields")
    text = text.replace(identity_anchor,
        identity_anchor + " requestOwnerIndex: request.RequestOwner == Entity.Null ? -1 : request.RequestOwner.Index, requestOwnerVersion: request.RequestOwner == Entity.Null ? 0 : request.RequestOwner.Version, requestAttempt: request.RequestAttempt.ToString(),")

    return text


def _instrument_delivery_owner(text: str) -> str:
    text = _replace_once(text,
        "if(!World.Buildings.TryGetValue(producerId,out var building)||building==null||!Recipes.TryGetValue(producerId,out var recipes))return false;",
        "if(!World.Buildings.TryGetValue(producerId,out var building)||building==null||!Recipes.TryGetValue(producerId,out var recipes)){global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_rejected\",now,failureCause:\"ProducerBuildingOrRecipeRegistryMissing\",producerBuildingId:producerId,productionIndex:productionIndex);return false;}",
        "delivery producer/recipe registry gate")
    text = _replace_once(text,
        "if(recipe==null)return false;ValidateRecipe(building,recipe);",
        "if(recipe==null){global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_rejected\",now,failureCause:\"NoRecipeForProductionIndex\",producerBuildingId:producerId,productionIndex:productionIndex);return false;}ValidateRecipe(building,recipe);",
        "delivery recipe index gate")
    text = _replace_once(text,
        "if(count>=MaxQueued)return false;}",
        "if(count>=MaxQueued){global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_rejected\",now,failureCause:\"GlobalPendingQueueLimitReached\",requestCountBefore:count,requestCountAfter:count,queueCapacityLimit:MaxQueued,producerBuildingId:producerId,productionIndex:productionIndex);return false;}}",
        "delivery queue capacity gate")
    text = _replace_once(text,
        "            if (building == null || spawnUnitPrefab == null)\n                return false;",
        "            if (building == null || spawnUnitPrefab == null)\n            {\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_rejected\", now, failureCause: building == null ? \"ProducerBuildingNull\" : \"SpawnUnitPrefabNull\", producerBuildingId: building?.Id ?? -1, productionIndex: productionIndex, unitPrefab: spawnUnitPrefab?.name);\n"
        "                return false;\n            }",
        "queue owner null input gate")
    text = _replace_once(text,
        "                if (reservedProductionSlotIndex < 0 && !allowUnreservedHelicopterHelipadSpawn)\n                    return false;",
        "                if (reservedProductionSlotIndex < 0 && !allowUnreservedHelicopterHelipadSpawn)\n                {\n"
        "                    global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_rejected\", now, failureCause: \"NoProductionSpawnSlotReservation\", producerBuildingId: building.Id, productionIndex: productionIndex, unitPrefab: spawnUnitPrefab.name);\n"
        "                    return false;\n                }",
        "production spawn slot gate")
    text = _replace_once(text,
        "            if (!SkirmishNativeProduction.TryReserve(entityManager, building, spawnUnitPrefab, out Entity receiptOwner, out uint receiptId))\n                return false;",
        "            if (!SkirmishNativeProduction.TryReserve(entityManager, building, spawnUnitPrefab, out Entity receiptOwner, out uint receiptId))\n            {\n"
        "                global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_rejected\", now, failureCause: \"NativeProductionReceiptReservationRejected\", producerBuildingId: building.Id, productionIndex: productionIndex, unitPrefab: spawnUnitPrefab.name);\n"
        "                return false;\n            }",
        "native queue receipt reservation gate")
    text = _replace_once(text,
        "pending=queuedProduction;RecordDeliveryEvent(\"queue_accepted\",now,building.Id,productionIndex,spawnUnitPrefab.name,new int2(-1,-1),default,false,Entity.Null,false);return true;",
        "pending=queuedProduction;RecordDeliveryEvent(\"queue_accepted\",now,building.Id,productionIndex,spawnUnitPrefab.name,new int2(-1,-1),default,false,Entity.Null,false);global::SourceS073AIProductionDecisionTelemetry.RecordLifecycle(\"delivery_queue_accepted\",now,producerBuildingId:building.Id,productionIndex:productionIndex,unitPrefab:spawnUnitPrefab.name,requestCountAfter:building.PendingProductions.Count,requestCountBefore:building.PendingProductions.Count-1);return true;",
        "delivery queue accepted boundary")
    return text


def _generate_lifecycle_sources(repo: Path, output: Path, ai_owner: str) -> dict:
    source_pins: dict[str, str] = {SOURCE: _sha(repo / SOURCE)}
    generated_outputs: dict[str, str] = {OUTPUT: ai_owner}

    partial = _generate_partial_copy(repo, output, source_pins)
    runtime = _generate_runtime_copy(repo, output, source_pins)
    generated_outputs[PARTIAL_OUTPUT] = partial
    generated_outputs[RUNTIME_OUTPUT] = runtime

    faction_recipe = _load_recipe(repo, FACTION_RECIPE, "s073_ai_decision_faction_recipe")
    faction_detail = faction_recipe.generate(repo, output)
    kernel_path = output / "FactionRequestKernel.source.cs"
    kernel = _instrument_faction_kernel(kernel_path.read_text(encoding="utf-8"))
    kernel_path.write_text(kernel, encoding="utf-8")
    generated_outputs["FactionRequestKernel.source.cs"] = kernel

    delivery_recipe = _load_recipe(repo, DELIVERY_RECIPE, "s073_ai_decision_delivery_recipe")
    delivery_detail = delivery_recipe.generate(repo, output)
    delivery_path = output / "SourceProductionDeliveryOwner.cs"
    delivery_owner = _instrument_delivery_owner(delivery_path.read_text(encoding="utf-8"))
    delivery_path.write_text(delivery_owner, encoding="utf-8")
    generated_outputs["SourceProductionDeliveryOwner.cs"] = delivery_owner

    for path, digest in faction_detail.get("source_inputs", {}).items():
        source_pins[path] = digest
    for item in delivery_detail.get("inputs", []):
        source_pins[item["path"]] = item["raw_sha256"]
    for relative in GENERATOR_HELPERS:
        path = repo / relative
        source_pins[str(path.resolve())] = _sha(path)
    outputs = {name: hashlib.sha256(content.encode("utf-8")).hexdigest()
               for name, content in generated_outputs.items()}
    return {
        "source_pins": source_pins,
        "outputs": outputs,
        "experimental_only": True,
        "transform": "Generated exact-anchor S073 diagnostic copies observe the original selector, one actual TrySpend return, the actual request append, the live consumer terminal receipt, exact queue-owner failure branch, and the original rollback/removal result. No selector/resource re-evaluation, state injection, or default source mutation.",
    }
