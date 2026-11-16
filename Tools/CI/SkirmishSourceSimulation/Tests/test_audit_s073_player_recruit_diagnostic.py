import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from audit_s073_player_recruit_diagnostic import audit


class PlayerRecruitAuditControl(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.result = {
            "mission_ready": False, "native_player_input_observed": False,
            "source_host_aria_policy_step": {"synthetic_player_recruit_diagnostic": {
                "scope": "ExplicitLogicalSourceCommand_NotNativeInput", "accepted": True,
                "result": "None", "native_input_observed": False, "mission_ready": False,
                "authored_credits_cost": 10000, "authored_materials_cost": 20, "resource_policy": 1,
                "credits_before": 0, "credits_after": 0, "materials_before": 220, "materials_after": 200,
                "pending_refundable_materials": 20, "pending_paid_quantity": 4, "unit_prefab": "rifle"}},
            "source_host_original_tactical_owner_scheduler_tick": {"original_production_runtime": {
                "source_delivery_events": [
                    {"kind": "queue_accepted", "producer_id": 1, "source_time_seconds": 0, "unit_prefab": "rifle"},
                    {"kind": "helicopter_arrived", "producer_id": 1},
                    *[{"kind": "unit_spawned_and_placed", "producer_id": 1, "unit_prefab": "rifle",
                       "spawn_succeeded": True, "placement_observed": True, "entity_index": i} for i in range(4)]
                ]}}}

    def tearDown(self):
        self.temp.cleanup()

    def run_audit(self):
        raw = json.dumps(self.result).encode()
        (self.root / "result.json").write_bytes(raw)
        receipt = {"mission": "S073", "seed": 104729, "timed_out": False, "process_exit": 2,
                   "validation_passed": False, "mission_ready": False,
                   "worker_result": {"sha256": hashlib.sha256(raw).hexdigest()}}
        (self.root / "run-receipt.json").write_text(json.dumps(receipt))
        audit(self.root, self.root / "audit.json")

    def test_valid(self):
        self.run_audit()
        self.assertTrue((self.root / "audit.json").exists())

    def test_rejects_wrong_balance(self):
        self.result["source_host_aria_policy_step"]["synthetic_player_recruit_diagnostic"]["materials_after"] = 220
        with self.assertRaises(ValueError):
            self.run_audit()

    def test_rejects_missing_placed_unit(self):
        self.result["source_host_original_tactical_owner_scheduler_tick"]["original_production_runtime"]["source_delivery_events"].pop()
        with self.assertRaises(ValueError):
            self.run_audit()

    def test_rejects_native_input_claim(self):
        self.result["native_player_input_observed"] = True
        with self.assertRaises(ValueError):
            self.run_audit()


if __name__ == "__main__":
    unittest.main()
