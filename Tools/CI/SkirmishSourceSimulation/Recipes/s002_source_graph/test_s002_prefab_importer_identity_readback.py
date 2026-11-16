from __future__ import annotations

import importlib.util
from pathlib import Path
import unittest

RUNNER = __import__("pathlib").Path(__file__).with_name("run_s002_prefab_importer_identity_readback.py")
SPEC = importlib.util.spec_from_file_location("s002_prefab_importer_identity_readback", RUNNER)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC is not None and SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def valid_evidence() -> dict:
    return {"kind": "S002PrefabConfigTargetPartialSourceEvidence", "status": "PartialUnresolved",
            "prefab_path": MODULE.PREFAB, "unity_importer_semantics": False,
            "complete_provider_join": False,
            "rows": [dict(row, target_guid_hit_count=1) for row in MODULE.EXPECTED_ROWS]}


def valid_observation() -> dict:
    rows = [dict(row, target_type="UnityResolvedType", config_type="UnityResolvedConfigType")
            for row in MODULE.EXPECTED_OBSERVATION_ROWS]
    return {"kind": "S002PrefabImporterIdentityObservation", "status": "Passed",
            "scope": "ImporterObjectIdentityOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderJoin_NoReadiness",
            "config_overrides": rows, "identity_issues": [],
            "observer_loaded_or_modified_scene": False, "observer_created_world": False,
            "observer_invoked_baker": False, "provider_join_complete": False, "mission_ready": False}


class PrefabImporterIdentityReadbackControls(unittest.TestCase):
    def test_invocation_uses_rtk_proxy_and_preserves_checked_wrapper(self):
        command = MODULE.wrapper_command(Path("/opt/rtk"), 180, Path("/tmp/unity.log"),
                                         Path("/tmp/observation.json"), Path("/tmp/pins.json"))
        self.assertEqual(command[:3], ["/opt/rtk", "proxy", str(MODULE.WRAPPER)])
        self.assertIn("--timeout", command)
        self.assertIn("-executeMethod", command)

    def test_accepts_both_distinct_exact_override_targets(self):
        MODULE.validate_evidence(valid_evidence())

    def test_rejects_missing_second_real_override(self):
        evidence = valid_evidence()
        evidence["rows"].pop()
        with self.assertRaisesRegex(ValueError, "Exactly two"):
            MODULE.validate_evidence(evidence)

    def test_rejects_duplicate_target_instead_of_treating_two_rows_as_complete(self):
        evidence = valid_evidence()
        evidence["rows"][1]["target_guid"] = evidence["rows"][0]["target_guid"]
        evidence["rows"][1]["target_file_id"] = evidence["rows"][0]["target_file_id"]
        evidence["rows"][1]["target_prefab_path"] = evidence["rows"][0]["target_prefab_path"]
        with self.assertRaisesRegex(ValueError, "target_guid"):
            MODULE.validate_evidence(evidence)

    def test_rejects_wrong_immediate_base_file_id(self):
        evidence = valid_evidence()
        evidence["rows"][1]["target_file_id"] += 1
        with self.assertRaisesRegex(ValueError, "target_file_id"):
            MODULE.validate_evidence(evidence)

    def test_rejects_changed_config_identity(self):
        evidence = valid_evidence()
        evidence["rows"][0]["config_file_id"] += 1
        with self.assertRaisesRegex(ValueError, "config_file_id"):
            MODULE.validate_evidence(evidence)

    def test_finalizer_accepts_both_importer_resolved_rows(self):
        MODULE.validate_observation(valid_observation())

    def test_finalizer_rejects_importer_resolving_only_one_target(self):
        observation = valid_observation()
        observation["config_overrides"].pop()
        with self.assertRaisesRegex(ValueError, "Exactly two"):
            MODULE.validate_observation(observation)

    def test_finalizer_rejects_changed_imported_object_reference(self):
        observation = valid_observation()
        observation["config_overrides"][1]["config_local_id"] = "11400001"
        with self.assertRaisesRegex(ValueError, "differ from both"):
            MODULE.validate_observation(observation)


if __name__ == "__main__":
    unittest.main()
