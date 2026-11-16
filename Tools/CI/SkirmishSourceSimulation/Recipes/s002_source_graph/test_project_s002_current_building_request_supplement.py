from __future__ import annotations

import importlib.util
import json
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[5]
SCRIPT = Path(__file__).with_name("project_s002_current_building_request_supplement.py")
SPEC = importlib.util.spec_from_file_location("s002_building_request_projector", SCRIPT)
projector = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(projector)

PROVIDER = ROOT / "Logs/SkirmishProduction/s002-current-provider-metadata-20261104-T1/two-seed-metadata.json"
PROVIDER_RECEIPT = ROOT / "T Logs/SkirmishProduction/s002-current-provider-metadata-20261104-T1/receipt.json"
PROVIDER_LOG = ROOT / "T Logs/SkirmishProduction/s002-current-provider-metadata-20261104-T1/editor.log"
DEFINITIONS = ROOT / "Logs/SkirmishProduction/s002-current-building-metadata-20261104-T1/building-definitions.json"
DEFINITIONS_RECEIPT = ROOT / "T Logs/SkirmishProduction/s002-current-building-metadata-20261104-T1/receipt.json"
DEFINITIONS_LOG = ROOT / "T Logs/SkirmishProduction/s002-current-building-metadata-20261104-T1/editor.log"


def captures():
    provider, _ = projector.read_json(PROVIDER)
    definitions, _ = projector.read_json(DEFINITIONS)
    provider_receipt, _ = projector.read_json(PROVIDER_RECEIPT)
    definitions_receipt, _ = projector.read_json(DEFINITIONS_RECEIPT)
    pins = projector.verify_source_pins(ROOT, provider["source_pins"], definitions["source_pins"],
                                        expected_counts=(provider_receipt["source_pins_checked"],
                                                         definitions_receipt["source_pins_checked"]))
    return provider, definitions, provider_receipt, definitions_receipt, pins


class S002CurrentBuildingRequestSupplementTests(unittest.TestCase):
    def test_projects_current_native_requestability_and_original_indices(self):
        provider, definitions, pr, dr, pins = captures()
        result = projector.project(provider, definitions, pr, dr, PROVIDER, DEFINITIONS, pins)
        self.assertEqual("CurrentSourceBuildingRequestMetadataSupplement", result["kind"])
        self.assertEqual(8, len(result["rows"]))
        self.assertEqual(list(definitions["requested_keys"]), [row["key"] for row in result["rows"]])
        self.assertEqual(list(range(7)), sorted(row["construction_catalog_index"] for row in result["rows"]
                                                if row["construction_catalog_index"] is not None))
        self.assertFalse(result["mission_ready"])
        self.assertFalse(result["full_mission_initialized"])
        self.assertFalse(result["native_visual_readiness"])

    def test_key_join_preserves_provider_indices_when_provider_order_differs(self):
        provider, definitions, pr, dr, pins = captures()
        result = projector.project(provider, definitions, pr, dr, PROVIDER, DEFINITIONS, pins)
        native = next(c for c in provider["contexts"] if c["seed"] == 104731)["metadata"]["buildings"]
        self.assertNotEqual([r["prefab"]["key"] for r in native], [r["key"] for r in definitions["definitions"]])
        self.assertEqual(0, next(r for r in result["rows"] if r["key"] == "Building_Barrack")["construction_catalog_index"])

    def test_rejects_identity_mismatch_even_when_key_is_same(self):
        provider, definitions, pr, dr, pins = captures()
        definitions["definitions"][0]["prefab"]["guid"] = "0" * 32
        with self.assertRaisesRegex(projector.ProjectionError, "identity differs"):
            projector.project(provider, definitions, pr, dr, PROVIDER, DEFINITIONS, pins)

    def test_rejects_provider_index_not_witnessed_by_actual_list(self):
        provider, definitions, pr, dr, pins = captures()
        provider["contexts"][0]["metadata"]["buildings"][0]["construction_index"] = 1
        with self.assertRaisesRegex(projector.ProjectionError, "provider order/identity mismatch"):
            projector.project(provider, definitions, pr, dr, PROVIDER, DEFINITIONS, pins)

    def test_rejects_missing_original_requestability_instead_of_defaulting(self):
        provider, definitions, pr, dr, pins = captures()
        row = provider["contexts"][0]["metadata"]["buildings"][0]
        row["configured_entry"]["fields"]["CanRequest"]["codec"] = "missing"
        with self.assertRaisesRegex(projector.ProjectionError, "requestability unsupported"):
            projector.project(provider, definitions, pr, dr, PROVIDER, DEFINITIONS, pins)

    def test_rejects_readiness_or_scope_promotion(self):
        provider, definitions, pr, dr, pins = captures()
        provider["full_mission_initialized"] = True
        with self.assertRaisesRegex(projector.ProjectionError, "must remain false"):
            projector.project(provider, definitions, pr, dr, PROVIDER, DEFINITIONS, pins)

    def test_capture_receipts_and_markers_bind_real_inputs(self):
        projector.verify_capture(PROVIDER_RECEIPT, PROVIDER_LOG, PROVIDER,
                                 "OriginalCompilerProvidersAndMetadataFactoriesOnly",
                                 projector.PROVIDER_MARKER, "NativeTwoSeedMetadataQualification")
        projector.verify_capture(DEFINITIONS_RECEIPT, DEFINITIONS_LOG, DEFINITIONS,
                                 "OriginalConfiguredPrefabMetadataFactory_NotMissionState",
                                 projector.DEFINITIONS_MARKER, "AuthoredSkirmishBuildingDefinitions")


if __name__ == "__main__":
    unittest.main()
