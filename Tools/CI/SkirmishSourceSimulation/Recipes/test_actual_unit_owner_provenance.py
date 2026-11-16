#!/usr/bin/env python3
"""Strict direct unit-owner provenance controls."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).with_name("project_source_only_focused_prefab_domain.py")


def load_projector():
    spec = importlib.util.spec_from_file_location("actual_unit_owner_projector_test", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class ActualUnitOwnerProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.projector = load_projector()
        self.temp = tempfile.TemporaryDirectory(prefix="actual-unit-owner-")
        self.root = Path(self.temp.name)
        self.result_path = self.root / "author-result.json"
        self.request_path = self.root / "physical-request.json"
        setup = 472937146
        layout = "a" * 64
        compiled_layout = "d" * 64
        source = "b" * 64
        result = {
            "kind": "SkirmishMissionAuthoringPreflightResult", "status": "ConfigurationCompiled",
            "compiler_accepted": True, "native_authoring_inputs_returned": True,
            "catalog_id": "S050", "seed": 104779, "size": "Standard", "difficulty": "Regular",
            "operation_map_id": "S050_Map", "source_content_hash": source,
            "layout_fingerprint": compiled_layout, "resolved_setup": {"setup_hash": setup},
            "world_allocated": False, "source_fit_verified": False, "mission_ready": False,
            "native_acceptance": False, "catalog_mutated": False, "native_playable": False,
            "validation_passed": False, "partial_authoring_capture_for_missing_binding": False,
            "compiler_reasons": [], "authoring_capture_reasons": [],
        }
        self.result_path.write_text(json.dumps(result))
        result_ref = {"path": str(self.result_path.resolve()), "sha256": self.sha(self.result_path)}
        self.request_path.write_text(json.dumps({
            "kind": "PinnedCampaignPhysicalInputExport", "source_fit_certified": False,
            "mission_ready": False, "operation_map_id": "S050_Map", "content_hash": source,
            "selection": result_ref,
        }))
        request_ref = {"path": str(self.request_path.resolve()), "sha256": self.sha(self.request_path)}
        self.context = {"target_context": {
            "catalog_id": "S050", "definition_id": "skirmish.s050", "content_version": 2,
            "seed": 104779, "size": "Standard", "difficulty": "Regular",
            "setup_hash": setup, "layout_binding_fingerprint": layout, "operation_map_id": "S050_Map",
            "source_content_hash": source,
        }}
        self.keys = [f"unit-{i:02d}" for i in range(16)]
        self.units = {
            "kind": "AuthoredSkirmishUnitProductionMetadata",
            "authority": "OriginalConfiguredAuthoringFactories_NotMissionState",
            "catalog_id": "S050", "seed": 104779, "setup_hash": setup,
            "full_mission_initialized": False, "native_visual_readiness": False,
            "baked_actor_footprint_substituted": False, "ui_catalog_runtime_policy_sampled": False,
            "runtime_camera_basis_exported": False,
            "compiler_selection": {
                "result": result_ref, "physical_input_request": request_ref,
                "catalog_id": "S050", "seed": 104779, "size": "Standard", "difficulty": "Regular",
                "setup_hash": setup, "operation_map_id": "S050_Map",
                "source_content_hash": "skirmish.s050.v2/enemy-policy-v1/" + "c" * 64,
                "layout_fingerprint": layout, "status": "ConfigurationCompiled",
                "runtime_admission": False, "source_fit_certified": False,
                "mission_ready": False, "native_acceptance": False, "world_allocated": False,
            },
            "source_pins": {result_ref["path"]: result_ref["sha256"],
                             request_ref["path"]: request_ref["sha256"]},
            "units": [],
        }
        self.identity_rows = []
        for i, key in enumerate(self.keys):
            path = f"Assets/Game/Prefabs/Units/{key}.prefab"
            guid = f"{i + 1:032x}"
            local_id = 1000 + i
            ref = {"codec": "source-asset-reference-v1", "type": "UnityEngine.GameObject",
                   "key": key, "path": path, "guid": guid, "local_id": local_id}
            self.units["units"].append({
                "index": i, "key": key, "prefab": ref,
                "has_definition_metadata": True, "has_production_metadata": True,
                "authoring_catalog_properties": {"fields": {"CanRequest": {"codec": "bool-v1", "value": True}}},
            })
            self.identity_rows.append({"key": key, "path": path, "guid": guid, "local_id": local_id})
            absolute = str((self.projector.ROOT / path).resolve())
            self.units["source_pins"][absolute] = "e" * 64
            self.units["source_pins"][absolute + ".meta"] = "f" * 64
        self.identity = {
            "kind": "AuthoredUnitPrefabIdentityReadback", "catalog_id": "S050", "seed": 104779,
            "setup_hash": setup, "metadata_sha256": "c" * 64, "units": 16,
            "runtime_templates_verified": False, "mission_ready": False,
            "identities": self.identity_rows,
        }

    def tearDown(self):
        self.temp.cleanup()

    @staticmethod
    def sha(path):
        return hashlib.sha256(Path(path).read_bytes()).hexdigest()

    def validate(self):
        return self.projector.validate_actual_owner_unit_metadata(
            self.units, self.identity, "c" * 64, json.loads(self.result_path.read_bytes()),
            self.result_path, json.loads(self.request_path.read_bytes()), self.request_path,
            self.context, self.keys)

    def test_exact_compiler_selected_owner_positive(self):
        proof = self.validate()
        self.assertEqual(proof["mode"], "OriginalConfiguredAuthoringFactories_CompilerSelected")
        self.assertEqual(proof["requestable_unit_count"], 16)
        self.assertEqual(proof["logical_layout_binding_fingerprint"], "a" * 64)
        self.assertEqual(proof["compiled_layout_fingerprint"], "d" * 64)
        self.assertTrue(proof["compiler_source_policy_hash"].startswith("skirmish.s050.v2/enemy-policy-v1/"))
        self.assertEqual(proof["physical_map_content_hash"], "b" * 64)

    def test_rejects_false_requestable_property(self):
        self.units["units"][3]["authoring_catalog_properties"]["fields"]["CanRequest"]["value"] = False
        with self.assertRaisesRegex(ValueError, "CanRequest=true"):
            self.validate()

    def test_rejects_identity_bytes_or_selection_mismatch(self):
        self.identity["metadata_sha256"] = "d" * 64
        with self.assertRaisesRegex(ValueError, "identity readback"):
            self.validate()
        self.identity["metadata_sha256"] = "c" * 64
        self.units["compiler_selection"]["result"]["sha256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "current compiler result"):
            self.validate()

    def test_rejects_promoted_readiness_and_unpinned_prefab_meta(self):
        self.units["mission_ready"] = True
        with self.assertRaisesRegex(ValueError, "scope flags"):
            self.validate()
        self.units["mission_ready"] = False
        ref = self.units["units"][0]["prefab"]
        absolute = str((self.projector.ROOT / ref["path"]).resolve())
        del self.units["source_pins"][absolute + ".meta"]
        with self.assertRaisesRegex(ValueError, "prefab and .meta"):
            self.validate()

    def test_rejects_logical_layout_binding_mismatch_and_wrong_capture_key_arg(self):
        self.context["target_context"]["layout_binding_fingerprint"] = "e" * 64
        with self.assertRaisesRegex(ValueError, "compiler context"):
            self.validate()
        log = "-skirmishFocusedPrefabKeys\n/expected/keys.json\n-skirmishFocusedPrefabKeysSha\n" + "f" * 64
        self.assertTrue(self.projector.logged_argument_matches(log, "-skirmishFocusedPrefabKeys", "/expected/keys.json"))
        self.assertFalse(self.projector.logged_argument_matches(log, "-skirmishFocusedPrefabKeys", "/wrong/keys.json"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
