from __future__ import annotations

import importlib.util
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
GENERATOR_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s073_ai_decision_telemetry/generate.py"
MANIFEST_PATH = ROOT / "Tools/CI/make_s073_ai_production_decision_manifest.py"


def load(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class S073DecisionTelemetryTests(unittest.TestCase):
    def test_recipe_generates_single_record_after_original_selector_without_source_mutation(self):
        recipe = load("s073_decision_recipe", GENERATOR_PATH)
        owner = ROOT / "Assets/Game/Scripts/Systems/AIProductionSystem.cs"
        original = owner.read_bytes()
        with tempfile.TemporaryDirectory() as temporary:
            detail = recipe.generate(ROOT, Path(temporary))
            generated = (Path(temporary) / recipe.OUTPUT).read_text(encoding="utf-8")
            partial = (Path(temporary) / recipe.PARTIAL_OUTPUT).read_text(encoding="utf-8")
            runtime = (Path(temporary) / recipe.RUNTIME_OUTPUT).read_text(encoding="utf-8")
            kernel = (Path(temporary) / "FactionRequestKernel.source.cs").read_text(encoding="utf-8")
            delivery = (Path(temporary) / "SourceProductionDeliveryOwner.cs").read_text(encoding="utf-8")
        self.assertEqual(owner.read_bytes(), original)
        self.assertEqual(len(detail["source_pins"][recipe.SOURCE]), 64)
        self.assertTrue(detail["experimental_only"])
        self.assertIn("ProductionDecision decision = SelectProductionDecision", generated)
        self.assertIn("Record(now, planEntity.Index, planEntity.Version, plan.FactionId, decision.Result.ToString(), telemetryReason", generated)
        self.assertIn("decision.CapacitySkippedCandidates++", generated)
        self.assertIn("decision.TargetReachedCandidates++", generated)
        self.assertIn("decision.QueueLimitCandidates++", generated)
        self.assertIn("decision.ResourceResult = FactionConstructionResourceUtilitySystemHelper.Evaluate", generated)
        self.assertEqual(generated.count("SourceS073AIProductionDecisionTelemetry.Record("), 2)
        self.assertEqual(generated.count("SourceS073AIProductionDecisionTelemetry.RecordIntervalGate("), 1)
        self.assertIn('RecordLifecycle("try_spend_return"', generated)
        self.assertIn('RecordLifecycle("request_enqueued"', partial)
        self.assertEqual(partial.count("RecordLifecycle(\"request_enqueued\""), 1)
        self.assertIn("BuildingFactionUnitProductionRequest appendedRequest = requests[requests.Length - 1]", partial)
        self.assertNotIn("BuildingFactionUnitProductionRequest request = requests[requests.Length - 1]", partial)
        self.assertIn("FactionConstructionResourceMutationResult spendResult = FactionConstructionResourceUtilitySystemHelper.TrySpend", generated)
        self.assertIn("decision.FuelCheckEvaluated == 0 ? \"NotEvaluated\"", generated)
        self.assertIn("decision.FuelCheckEvaluated != 0 && decision.FuelCheckRequired != 0 ? decision.FuelSupported != 0 : (bool?)null", generated)
        self.assertIn("decision.FuelCheckEvaluated = 1", generated)
        self.assertIn("bool? fuelSupported = null", (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073AIProductionDecisionTelemetry.cs").read_text())
        self.assertIn("FuelCheckRequired == 0 ? \"NotRequired\"", generated)
        self.assertNotIn("requestCountBeforeSpend", generated)
        self.assertNotIn("requestCountAfterSpend", generated)
        self.assertIn("queueCapacityLimit:MaxQueued", delivery)
        self.assertIn("FactionConstructionResourceMutationResult refundResult =", partial)
        self.assertIn('RecordLifecycle("receipt_removed"', partial)
        self.assertIn('RecordConsumerSnapshot(em, boundary, now)', runtime)
        self.assertIn('"PopulationQueuePolicyRejected"', kernel)
        self.assertIn('"ProductionDeliveryOwnerRejected"', kernel)
        self.assertIn('"NoProductionSpawnSlotReservation"', delivery)
        self.assertIn('"NativeProductionReceiptReservationRejected"', delivery)
        self.assertIn('"delivery_queue_accepted"', delivery)
        self.assertTrue({recipe.OUTPUT, recipe.PARTIAL_OUTPUT, recipe.RUNTIME_OUTPUT,
                         "FactionRequestKernel.source.cs", "SourceProductionDeliveryOwner.cs"}.issubset(detail["outputs"]))
        self.assertIn("plan_entity_version = planEntityVersion", (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073AIProductionDecisionTelemetry.cs").read_text())
        self.assertIn("FileMode.CreateNew", (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073AIProductionDecisionTelemetry.cs").read_text())
        self.assertIn("interval_gate_updates_since_previous_decision = intervalGateUpdates",
                      (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073AIProductionDecisionTelemetry.cs").read_text())
        self.assertLess(generated.index("ProductionDecision decision = SelectProductionDecision"),
                        generated.index("SourceS073AIProductionDecisionTelemetry.Record(now, planEntity.Index, planEntity.Version, plan.FactionId, decision.Result"))

    def test_s073_producer_identity_join_is_full_tuple_and_fails_closed(self):
        adapter = (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyProductionRuntime.cs").read_text()
        metadata_path = ROOT / "Design/AgentReports/SkirmishProduction/S073/TransportMetadataReadback20261101/s073-unit-production-metadata.json"
        joined_definitions_path = ROOT / "Logs/SkirmishProduction/s073-shared-grid-frame-binding-20261102-r4/diagnostic-bundle/building-definitions-current-source-joined.json"
        initial_spawn_path = ROOT / "Logs/SkirmishProduction/s073-shared-grid-frame-binding-20261102-r4/diagnostic-bundle/legacy-initial-spawn-data-current-source-joined.json"
        metadata = json.loads(metadata_path.read_text())
        definitions = json.loads(joined_definitions_path.read_text())
        initial_spawn = json.loads(initial_spawn_path.read_text())
        diagnostic_bundle_path = ROOT / "Logs/SkirmishProduction/s073-shared-grid-frame-binding-20261102-r4/diagnostic-bundle/s073-current-source-diagnostic-bundle.json"
        diagnostic_bundle = json.loads(diagnostic_bundle_path.read_text())
        for descriptor_name, evidence_path in (("building_definitions", joined_definitions_path),
                                               ("legacy_config", initial_spawn_path)):
            descriptor = diagnostic_bundle[descriptor_name]
            self.assertEqual(Path(descriptor["path"]), evidence_path)
            self.assertEqual(hashlib.sha256(evidence_path.read_bytes()).hexdigest(), descriptor["sha256"])
        unit = next(row for row in metadata["units"] if row["key"] == "Unit_Chr_Soldier_Male_02_Alt_04")["prefab"]
        barrack = next(row for row in definitions["definitions"] if row["key"] == "Building_Barrack")
        slot = barrack["fields"]["ProductionSlots"]["items"][0]["fields"]["SpawnUnitPrefab"]
        self.assertEqual((slot["key"], slot["path"], slot["guid"], slot["local_id"]),
                         (unit["key"], unit["path"], unit["guid"], unit["local_id"]))
        self.assertEqual(definitions["source_pins"]["Assets/Game/Prefabs/Buildings/Building_Barrack.prefab"],
                         hashlib.sha256((ROOT / "Assets/Game/Prefabs/Buildings/Building_Barrack.prefab").read_bytes()).hexdigest())
        initial_forces_path = ROOT / "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
        self.assertEqual(initial_spawn["source_artifacts"]["configuration_asset_sha256"]["Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"],
                         hashlib.sha256(initial_forces_path.read_bytes()).hexdigest())
        factions = initial_spawn["initial_spawn"]["Factions"]
        self.assertTrue(any(row["FactionId"] == 2 and any(building["LookupKey"] == "building_barrack"
                                                            for building in row["Buildings"]) for row in factions))

        identity = ("Unit_Chr_Soldier_Male_02_Alt_04", unit["path"], unit["guid"], unit["local_id"])
        def exact_identity(candidate):
            return candidate == identity
        self.assertTrue(exact_identity(identity))
        self.assertFalse(exact_identity((identity[0], "Assets/Game/Prefabs/Other.prefab", identity[2], identity[3])))
        self.assertFalse(exact_identity((identity[0], identity[1], "0" * 32, identity[3])))
        self.assertFalse(exact_identity((identity[0], identity[1], identity[2], identity[3] + 1)))
        self.assertFalse(exact_identity(("Other_Unit", identity[1], identity[2], identity[3])))

        for comparison in ["a.name == b.name", "a.SourceAssetPath == b.SourceAssetPath",
                           "a.SourceAssetGuid == b.SourceAssetGuid", "a.SourceAssetLocalId == b.SourceAssetLocalId"]:
            self.assertIn(comparison, adapter)
        self.assertIn("SameAsset(definition, (JObject)row[\"prefab\"])", adapter)
        self.assertIn("CanonicalizeProductionPrefab(slot.SpawnUnitPrefab, canonicalPrefabsBySourceKey)", adapter)
        self.assertIn("throw new InvalidDataException(\"S073 production source key resolves to conflicting asset identity:", adapter)
        self.assertIn("if (sourcePrefab == null || !canonicalPrefabsBySourceKey.TryGetValue(sourcePrefab.name", adapter)
        self.assertIn("return sourcePrefab;", adapter)  # nonconfigured producer slots keep their own source token
        self.assertIn("production.ConfigureUnitProductionMetadataResolver", adapter)
        self.assertIn("new SourceUnitPrefab(name, template, prefabPath, guid, fileId)",
                      (ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S073NativeConfiguredUnits.cs").read_text())

    def test_manifest_composes_with_resource_census_and_reclassifies_only_owner_copy(self):
        manifest_helper = load("s073_decision_manifest", MANIFEST_PATH)
        base = json.loads((ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json").read_text())
        base = load("s073_census_manifest", ROOT / "Tools/CI/make_s073_resource_census_manifest.py").derive(base)
        derived = manifest_helper.derive(base)
        owner = [row for row in derived["sources"] if row.get("id") == "S073AI_AIProductionSystem"]
        self.assertEqual(len(owner), 1)
        self.assertEqual(owner[0]["kind"], "current_original")
        self.assertIn("Assets/Game/Scripts/Systems/AIProductionSystem.cs", owner[0]["generator_inputs"])
        self.assertIn("Assets/Game/Scripts/Systems/AIProductionSystem.ProductionRequests.cs", owner[0]["generator_inputs"])
        self.assertTrue(any(row.get("id") == "292F8_SourceS073ResourceEconomyRuntimeCensus.cs"
                            for row in derived["sources"]))
        self.assertTrue(any(row.get("id") == "SourceS073AIProductionDecisionTelemetry"
                            for row in derived["sources"]))
        self.assertTrue(all(next(row for row in derived["sources"] if row.get("id") == identity)["kind"] == "current_original"
                            for identity in ["S073AI_AIProductionSystemProductionRequests",
                                             "SourceS073LegacyProductionRuntime.cs",
                                             "505_FactionRequestKernel.source.cs",
                                             "467_SourceProductionDeliveryOwner.cs"]))


if __name__ == "__main__":
    unittest.main()
