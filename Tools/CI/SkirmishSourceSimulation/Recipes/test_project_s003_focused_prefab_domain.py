import copy
import importlib.util
from pathlib import Path
import unittest


SCRIPT = Path(__file__).with_name("project_s003_focused_prefab_domain.py")
SPEC = importlib.util.spec_from_file_location("s003_focused_prefab_domain", SCRIPT)
PROJECTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PROJECTOR)


def synthetic_capture():
    value = {field: None for field in PROJECTOR.FIELDS}
    value.update({
        "schema_version": 1,
        "kind": "AuthoredSkirmishPrefabReferenceData",
        "authority": "ActualSourceAssetsAndNativeBakers_NotMissionState",
        "mission_id": "S003",
        "source_scenes": ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"],
        "source_pins": {"Assets/example.asset": "a" * 64},
        "unity_version": "6000.5.2f1",
        "template_namespace_id": "a" * 32,
        "requested_prefab_keys": ["unit-%02d" % index for index in range(19)],
        "requested_prefab_keys_sha256": "b" * 64,
        "source_key_descriptor_receipt_sha256": "c" * 64,
        "native_metadata_capture_sha256": "d" * 64,
        "native_metadata_capture_receipt_sha256": "e" * 64,
        "compiler_seed": 104732,
        "compiler_setup_hash": 2394754492,
        "opening_prefabs": {"unit-%02d" % index: "%d:1" % (index + 1) for index in range(19)},
        "entities": [{"id": "%d:1" % index, "components": {}} for index in range(647)],
        "unsupported_values": [
            {"path": "%d:1/%s/%s" % (index + 1, component, field),
             "type": PROJECTOR.UNITY_GAMEOBJECT_REF,
             "reason": "unity_object_ref_requires_bound_source_identity"}
            for index in range(32)
            for component, fields in PROJECTOR.ALLOWED_VFX_FIELDS.items()
            for field in sorted(fields)
        ][:32],
        "unsupported_values_preserved_verbatim": True,
        "diagnostic_status": "CollectedSourceOnly",
        "replay_ready": False,
        "fresh_world_allocator_exported": False,
        "full_mission_initialized": False,
        "utility_world_created": True,
        "utility_world_disposed": True,
        "mission_world_created": False,
        "native_visual_readiness": False,
        "source_fit_certified": False,
        "native_context_qualified": False,
        "mission_ready": False,
        "geometry_exported": False,
        "consumer_rule": "diagnostic only",
    })
    return value


def synthetic_proof():
    desc = {"path": "/tmp/evidence.json", "sha256": "f" * 64}
    return {"schema_version": 1, "kind": PROJECTOR.PROOF_KIND, "source_graph": desc,
            "strict_capture_receipt": desc.copy(), "importer_control_receipt": desc.copy()}


class S003FocusedPrefabDomainTests(unittest.TestCase):
    def test_exact_consumer_419_projection_preserves_fields_and_scope(self):
        capture = synthetic_capture()
        domain = PROJECTOR.project_payload(capture, synthetic_proof(), "9" * 64)
        self.assertEqual([name for name in PROJECTOR.FIELDS if domain[name] != capture[name]], [])
        self.assertEqual(domain["source_reference_sha256"], "9" * 64)
        self.assertEqual(domain["derived_read_domain"], "ExactTypedPrefabClosureOnly")
        self.assertEqual(len(domain["opening_prefabs"]), 19)
        self.assertEqual(len(domain["entities"]), 647)
        self.assertEqual(len(domain["unsupported_values"]), 32)
        self.assertIs(domain["utility_world_created"], True)
        self.assertIs(domain["utility_world_disposed"], True)
        for key in ("replay_ready", "fresh_world_allocator_exported", "full_mission_initialized",
                    "mission_world_created", "native_visual_readiness", "source_fit_certified",
                    "native_context_qualified", "mission_ready", "geometry_exported"):
            self.assertIs(domain[key], False)

    def test_readiness_promotion_and_false_utility_world_are_rejected(self):
        capture = synthetic_capture()
        capture["mission_ready"] = True
        with self.assertRaisesRegex(ValueError, "false source-only scope"):
            PROJECTOR.project_payload(capture, synthetic_proof(), "9" * 64)
        capture = synthetic_capture()
        capture["utility_world_disposed"] = False
        with self.assertRaisesRegex(ValueError, "disposed utility World"):
            PROJECTOR.project_payload(capture, synthetic_proof(), "9" * 64)

    def test_incomplete_domain_or_proof_is_rejected(self):
        capture = synthetic_capture()
        capture["entities"].pop()
        with self.assertRaisesRegex(ValueError, "19-root/647-entity"):
            PROJECTOR.project_payload(capture, synthetic_proof(), "9" * 64)
        proof = synthetic_proof()
        proof["unbound_source"] = True
        with self.assertRaisesRegex(ValueError, "proof schema"):
            PROJECTOR.project_payload(synthetic_capture(), proof, "9" * 64)

    def test_non_vfx_or_duplicate_unsupported_value_is_rejected(self):
        capture = synthetic_capture()
        capture["unsupported_values"][0]["path"] = "1:1/Combat/Target"
        with self.assertRaisesRegex(ValueError, "exact unresolved VFX"):
            PROJECTOR.project_payload(capture, synthetic_proof(), "9" * 64)

    def test_projection_never_rewrites_native_selected_fields(self):
        capture = synthetic_capture()
        original = copy.deepcopy(capture)
        domain = PROJECTOR.project_payload(capture, synthetic_proof(), "9" * 64)
        self.assertEqual({name: domain[name] for name in PROJECTOR.FIELDS}, original)

    def test_workspace_importer_receipt_validation_is_optional_until_fresh_build_exists(self):
        receipt = PROJECTOR.ROOT / "T Logs/SkirmishProduction/s003-current-focused-template-source-host-controls-20261103-T4/receipt.json"
        source_build = PROJECTOR.ROOT / "T Logs/SkirmishProduction/s003-source-host-controls-build-current"
        if not receipt.is_file() or not source_build.is_dir():
            self.skipTest("Optional workspace-specific importer receipt and explicit current source build are absent")
        PROJECTOR.validate_inputs(PROJECTOR.ROOT, receipt, source_build)


if __name__ == "__main__":
    unittest.main()
