#!/usr/bin/env python3
"""Positive and negative fixture controls for generic focused prefab projection."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction"
SCRIPT = Path(__file__).with_name("project_source_only_focused_prefab_domain.py")
RAW = ROOT / "Logs/SkirmishProduction/s050-focused-prefab-capture-20261115-r1/prefab-reference-data.json"
WRAPPER = BASE / "s050-focused-prefab-projection-20261115-r2/capture-wrapper.log"
INPUTS = {
    "key_path": BASE / "s050-focused-prefab-capture-inputs-20261115-r1/prefab-keys.json",
    "authoring_result_path": BASE / "s050-general-authoring-run-20261107-r1/authoring-result.json",
    "context_path": BASE / "s050-source-factory-context-probe-20261107-r1/source-factory-context.json",
    "qualification_path": BASE / "s050-source-qualification-20261107-r1/qualification.json",
    "graph_path": BASE / "s050-source-grid-graph-extension-20261107-r1/source-config-graph.json",
    "building_path": BASE / "s050-derived-shared-factory-projections-20261107-r1/building-definitions.json",
    "unit_path": BASE / "s050-derived-shared-factory-projections-20261107-r1/unit-production-metadata.json",
    "request_path": BASE / "s050-focused-prefab-capture-inputs-20261115-r1/capture-request.json",
}


def load_projector():
    spec = importlib.util.spec_from_file_location("focused_prefab_projector_test", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class SourceOnlyFocusedPrefabProjectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.projector = load_projector()
        cls.raw = json.loads(RAW.read_bytes())

    def test_exact_importer_vfx_allowlist_positive(self):
        fields, components = self.projector.validate_unsupported(self.raw)
        self.assertEqual(sum(fields.values()), 23)
        self.assertEqual(components, {
            "Game.Components.UnitAttackImpactVfxReference": 10,
            "Game.Components.UnitMuzzleFlashVfxReference": 9,
            "Game.Components.GroundMissileLauncherVfxReferenceComponent": 1,
        })

    def test_rejects_non_presentation_component(self):
        raw = copy.deepcopy(self.raw)
        raw["unsupported_values"][0]["path"] = raw["unsupported_values"][0]["path"].replace(
            "Game.Components.UnitAttackImpactVfxReference", "Game.Components.UnitGridAuthoring")
        with self.assertRaisesRegex(ValueError, "allowlist"):
            self.projector.validate_unsupported(raw)

    def test_rejects_wrong_reason_type_and_component_field_bytes(self):
        for field, value in (("reason", "missing_source"), ("type", "UnityEngine.GameObject")):
            raw = copy.deepcopy(self.raw)
            raw["unsupported_values"][0][field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.projector.validate_unsupported(raw)
        raw = copy.deepcopy(self.raw)
        row = raw["unsupported_values"][0]
        eid, component_name, field_name = row["path"].split("/")
        raw["entities"] = [e for e in raw["entities"] if e["id"] != eid]
        with self.assertRaisesRegex(ValueError, "Unsupported row"):
            self.projector.validate_unsupported(raw)

    def test_rechecks_source_pin_bytes(self):
        key = INPUTS["key_path"].resolve()
        pin = {str(key): hashlib.sha256(key.read_bytes()).hexdigest()}
        self.projector.verify_source_pins(pin, ROOT)
        pin[str(key)] = "0" * 64
        with self.assertRaisesRegex(ValueError, "source pin changed"):
            self.projector.verify_source_pins(pin, ROOT)

    def test_positive_fixture_projection_preserves_all_nonclaims(self):
        raw = copy.deepcopy(self.raw)
        key = INPUTS["key_path"].resolve()
        key_sha = hashlib.sha256(key.read_bytes()).hexdigest()
        raw["source_pins"] = {str(key): key_sha}
        with tempfile.TemporaryDirectory(prefix="focused-prefab-projection-", dir=BASE) as temp:
            temp = Path(temp)
            raw_path = temp / "fixture-raw-capture.json"
            raw_path.write_text(json.dumps(raw, indent=2) + "\n")
            output = temp / "projection"
            self.projector.run(output, raw_path, WRAPPER, **INPUTS)
            receipt = json.loads((output / "receipt.json").read_bytes())
            domain = json.loads((output / "focused-prefab-domain.json").read_bytes())
            proof = json.loads((output / "focused-prefab-capture-proof.json").read_bytes())
            self.assertEqual(receipt["status"], "ProjectedSourceOnlyFromFailedRawCapture")
            self.assertTrue(receipt["raw_capture_failure_preserved"])
            self.assertFalse(domain["replay_ready"])
            self.assertEqual(domain["raw_capture_status"], "Failed_UnconsumedPresentationUnityObjectRefs")
            self.assertFalse(proof["raw_capture_pass_marker_present"])
            self.assertFalse(proof["runtime_import_attempted"])
            self.assertFalse(proof["runtime_admission"])
            self.assertFalse(proof["source_fit_certified"])
            self.assertFalse(proof["mission_ready"])
            self.assertNotIn("unit_owner_evidence", proof)

    def test_optional_direct_owner_evidence_is_omitted_when_absent_and_preserved_when_present(self):
        proof = self.projector.attach_unit_owner_evidence({"kind": "proof"}, None)
        self.assertNotIn("unit_owner_evidence", proof)
        evidence = {"mode": "OriginalConfiguredAuthoringFactories_CompilerSelected", "source_pin_count": 17}
        proof = self.projector.attach_unit_owner_evidence({"kind": "proof"}, evidence)
        self.assertEqual(proof["unit_owner_evidence"], evidence)


if __name__ == "__main__":
    unittest.main(verbosity=2)
