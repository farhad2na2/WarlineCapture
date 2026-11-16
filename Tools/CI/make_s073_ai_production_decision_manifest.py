#!/usr/bin/env python3
"""Derive a narrow, experimental S073 AI production decision trace manifest."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/s073_ai_decision_telemetry/generate.py"
SOURCE = "Assets/Game/Scripts/Systems/AIProductionSystem.cs"
OUTPUT = "S073AI_AIProductionSystem.DecisionTelemetry.cs"
GENERATOR_OUTPUT = "AIProductionSystem.DecisionTelemetry.cs"
HELPER = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073AIProductionDecisionTelemetry.cs"
SOURCE_ID = "S073AI_AIProductionSystem"
HELPER_ID = "SourceS073AIProductionDecisionTelemetry"
RECIPE_OUTPUTS = {
    SOURCE_ID: "AIProductionSystem.DecisionTelemetry.cs",
    "S073AI_AIProductionSystemProductionRequests": "AIProductionSystem.ProductionRequests.DecisionTelemetry.cs",
    "SourceS073LegacyProductionRuntime.cs": "SourceS073LegacyProductionRuntime.DecisionTelemetry.cs",
    "505_FactionRequestKernel.source.cs": "FactionRequestKernel.source.cs",
    "467_SourceProductionDeliveryOwner.cs": "SourceProductionDeliveryOwner.cs",
}

GENERATOR_INPUTS = [
    "Assets/Game/Scripts/Systems/AIProductionSystem.cs",
    "Assets/Game/Scripts/Systems/AIProductionSystem.ProductionRequests.cs",
    "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyProductionRuntime.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Runtime/Skirmish/SkirmishNativeProduction.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
    "Assets/Game/Scripts/Configs/BuildingProductionRecipe.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionSlotUtilitySystemHelper.cs",
]

GENERATOR_HELPERS = [
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/regenerate_faction_queue.py",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/BuildingProductionQueueCompositionSystemHelper.timeline.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/FactionRequestKernel.source.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/SkirmishNativeProduction.typed.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/faction_queue/templates/SourceFactionProductionDispatcher.cs.txt",
    "Tools/CI/SkirmishSourceSimulation/Recipes/production_delivery/generate.py",
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


def derive(base: dict) -> dict:
    if base.get("profile", {}).get("mission") != "S002":
        raise ValueError("Expected the current shared source manifest base profile.")
    variants = base.get("source_profile_variants", [])
    if not any(row.get("mission") == "S073" and row.get("runtime_backend") == "LegacyS073OwnerV1"
               for row in variants):
        raise ValueError("Base manifest has no admitted LegacyS073OwnerV1 source profile.")
    result = json.loads(json.dumps(base))
    rows = result.get("sources", [])
    for identity, generated_name in RECIPE_OUTPUTS.items():
        matching = [row for row in rows if row.get("id") == identity]
        if len(matching) != 1:
            raise ValueError("Exact current source row required for generated telemetry owner: " + identity)
        row = matching[0]
        if identity == SOURCE_ID and (row.get("kind") != "native_direct" or row.get("path") != SOURCE):
            raise ValueError("Exact current native AIProductionSystem source row required.")
        if identity == "S073AI_AIProductionSystemProductionRequests" and (
                row.get("kind") != "native_direct" or row.get("path") != "Assets/Game/Scripts/Systems/AIProductionSystem.ProductionRequests.cs"):
            raise ValueError("Exact current AIProductionSystem receipt owner row required.")
        if identity == "SourceS073LegacyProductionRuntime.cs" and (
                row.get("kind") != "native_direct" or row.get("path") != "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyProductionRuntime.cs"):
            raise ValueError("Exact current retained S073 request dispatcher adapter row required.")
        if identity == "505_FactionRequestKernel.source.cs" and row.get("kind") != "current_original":
            raise ValueError("Exact generated original faction request kernel row required.")
        if identity == "467_SourceProductionDeliveryOwner.cs" and row.get("kind") != "current_original":
            raise ValueError("Exact generated original production delivery owner row required.")
        row.update({
            "kind": "current_original",
            "generator": GENERATOR,
            "generator_inputs": list(GENERATOR_INPUTS),
            "generator_helpers": list(GENERATOR_HELPERS),
            "generator_output": generated_name,
            "output": row.get("output") or generated_name,
            "scope": "Experimental opt-in telemetry in exact-anchor generated copies of current original S073 production decision, receipt, request consumer and queue owner bodies; no default or shipping source mutation.",
        })
        row.pop("path", None)
        row.pop("role", None)
        row.pop("contains_original_gameplay", None)
    if any(row.get("id") == HELPER_ID or row.get("path") == HELPER for row in rows):
        raise ValueError("S073 decision telemetry sink already exists in the input manifest.")
    rows.append({
        "id": HELPER_ID,
        "kind": "finite_adapter",
        "path": HELPER,
        "role": "host_glue",
        "contains_original_gameplay": False,
        "require_declarations": ["internal static class SourceS073AIProductionDecisionTelemetry"],
        "scope": "Opt-in JSONL sink for generated original AIProductionSystem decision records. Requires a fresh WARLINE_S073_AI_PRODUCTION_DECISION_TELEMETRY_FILE path; no ECS reads or writes.",
    })
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", type=Path, required=True, help="Current base or resource-census manifest")
    parser.add_argument("--output", type=Path, required=True, help="Fresh derived manifest output path")
    args = parser.parse_args()
    if args.output.exists():
        parser.error("Output must be fresh: " + str(args.output))
    if not (ROOT / GENERATOR).is_file() or not (ROOT / HELPER).is_file():
        parser.error("Generated owner recipe or telemetry sink is missing")
    manifest = derive(json.loads(args.base.read_text(encoding="utf-8")))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    payload = json.dumps(manifest, indent=2, ensure_ascii=False) + "\n"
    args.output.write_text(payload, encoding="utf-8")
    print(json.dumps({
        "status": "Derived",
        "base_manifest": str(args.base.resolve()),
        "output_manifest": str(args.output.resolve()),
        "generated_original_owner_copy": OUTPUT,
        "opt_in_environment_variable": "WARLINE_S073_AI_PRODUCTION_DECISION_TELEMETRY_FILE",
        "scope": "Experimental S073 diagnostic source host only; source provenance remains linked to the exact current AIProductionSystem bytes.",
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
