import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

MODULE = Path(__file__).with_name("finalize_s073_native_ai_snapshot.py")
spec = importlib.util.spec_from_file_location("native_ai_snapshot_receipt", MODULE)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class NativeAISnapshotReceiptTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.out = self.root / "capture"
        self.out.mkdir()
        (self.out / "editor.log").write_text(
            "[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup fixture\n"
        )
        pins = {}
        for i in range(20):
            path = f"Assets/Source/file-{i:02}.txt"
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            content = f"source-{i}".encode()
            target.write_bytes(content)
            pins[path] = hashlib.sha256(content).hexdigest()
        self.snapshot = {
            "kind": "S073NativeAIStartupSnapshot",
            "schema_version": 1,
            "scope": "OriginalS073InstrumentedStartupSnapshot_NotNormalInputOrReadiness",
            "scenario_index": 3,
            "seed": 104729,
            "phase": "Playing",
            "map_id": "opmap.skirmish.industrial_basin",
            "source_map_id": "opmap.skirmish.desert_base_01",
            "source_identity_hash": "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900",
            "gameplay_start_complete": True,
            "gameplay_start_failed": False,
            "normal_input_playthrough": False,
            "player_readiness": False,
            "session_id": "0123456789abcdef0123456789abcdef",
            "source_pins_sha256": pins,
            "faction_control_config_tag_count": 1,
            "faction_control_entries": [{"FactionId": 1, "control": "AI"}],
            "ai_build_plan_count": 1,
            "ai_build_plans": [{"FactionId": 1, "entries_ordered": []}],
            "pending_ai_settings_snapshot": {},
            "post_startup_spawn_resolver_observations": [],
        }
        self.write_snapshot()

    def tearDown(self):
        self.tmp.cleanup()

    def write_snapshot(self):
        (self.out / "snapshot.json").write_text(json.dumps(self.snapshot))

    def test_qualifies_synthetic_instrumented_snapshot_without_readiness_claim(self):
        receipt = module.qualify(self.root, self.out, 0)
        self.assertEqual(receipt["validation_status"], "Passed")
        self.assertEqual(receipt["source_pin_count"], 20)
        self.assertFalse(receipt["normal_input_playthrough"])
        self.assertFalse(receipt["mission_ready"])

    def test_wrapper_failure_and_missing_marker_fail_closed(self):
        with self.assertRaisesRegex(ValueError, "Successful checked wrapper"):
            module.qualify(self.root, self.out, 1)
        (self.out / "editor.log").write_text("unrelated successful log\n")
        with self.assertRaisesRegex(ValueError, "Successful checked wrapper"):
            module.qualify(self.root, self.out, 0)

    def test_stale_source_pin_fails_closed(self):
        (self.root / "Assets/Source/file-00.txt").write_text("changed")
        with self.assertRaisesRegex(ValueError, "Native AI source changed"):
            module.qualify(self.root, self.out, 0)

    def test_duplicate_control_faction_fails_closed(self):
        self.snapshot["faction_control_entries"].append({"FactionId": 1, "control": "AI"})
        self.write_snapshot()
        with self.assertRaisesRegex(ValueError, "Complete unique observed controls"):
            module.qualify(self.root, self.out, 0)

    def test_duplicate_plan_faction_fails_closed(self):
        self.snapshot["ai_build_plans"].append({"FactionId": 1, "entries_ordered": []})
        self.snapshot["ai_build_plan_count"] = 2
        self.write_snapshot()
        with self.assertRaisesRegex(ValueError, "Complete unique observed controls"):
            module.qualify(self.root, self.out, 0)

    def test_wrong_phase_fails_closed(self):
        self.snapshot["phase"] = "Loading"
        self.write_snapshot()
        with self.assertRaisesRegex(ValueError, "Exact instrumented S073 startup identity"):
            module.qualify(self.root, self.out, 0)


if __name__ == "__main__":
    unittest.main()
