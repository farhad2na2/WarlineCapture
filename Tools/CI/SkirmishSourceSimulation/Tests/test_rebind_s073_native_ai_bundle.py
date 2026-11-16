"""Fail-closed controls for S073 native AI bundle rebinding."""
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).with_name("rebind_s073_native_ai_bundle.py")
spec = importlib.util.spec_from_file_location("rebind_s073_native_ai_bundle", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class S073NativeAIBundleRebindTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.capture = self.root / "capture"
        self.capture.mkdir()
        (self.capture / "editor.log").write_text(
            "[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup fixture\n")
        pins = {}
        for i in range(20):
            relative = f"Assets/Source/file-{i:02}.txt"
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(f"source-{i}")
            pins[relative] = digest(path)
        snapshot = {
            "kind": "S073NativeAIStartupSnapshot", "schema_version": 1,
            "scope": "OriginalS073InstrumentedStartupSnapshot_NotNormalInputOrReadiness",
            "scenario_index": 3, "seed": 104729, "phase": "Playing",
            "map_id": "opmap.skirmish.industrial_basin", "source_map_id": "opmap.skirmish.desert_base_01",
            "source_identity_hash": "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900",
            "gameplay_start_complete": True, "gameplay_start_failed": False,
            "normal_input_playthrough": False, "player_readiness": False,
            "session_id": "0123456789abcdef0123456789abcdef", "source_pins_sha256": pins,
            "faction_control_config_tag_count": 1,
            "faction_control_entries": [{"FactionId": 1, "control": "AI"}],
            "ai_build_plan_count": 1, "ai_build_plans": [{"FactionId": 1, "entries_ordered": []}],
            "pending_ai_settings_snapshot": {}, "post_startup_spawn_resolver_observations": [],
        }
        (self.capture / "snapshot.json").write_text(json.dumps(snapshot))
        receipt = module.qualify(self.root, self.capture, 0)
        (self.capture / "snapshot-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
        graph_source = self.root / "Assets/Source/ai.asset"
        graph_source.write_text("authored-ai")
        graph = {"schema_version": 1, "authority": "AuthoredConfigAssets", "nodes": [
            {"source_path": "Assets/Source/ai.asset", "source_sha256": digest(graph_source)}],
            "source_pins": {str(graph_source.resolve()): digest(graph_source)},
            "compiled_setup": False, "world_initialized": False, "native_world_state_used": False,
            "scope": "Authored source readback only; no native or mission readiness"}
        self.graph = self.root / "ai-graph.json"
        self.graph.write_text(json.dumps(graph))
        self.existing = self.root / "existing-source.json"
        self.existing.write_text("kept source")
        self.bundle = self.root / "r15-bundle.json"
        self.bundle.write_text(json.dumps({"schema_version": 1, "kind": "S073LegacyStartupBundle",
            "mission": "S073", "seed": 104729,
            "prefab_capture": {"path": str(self.existing), "sha256": digest(self.existing)},
            "native_ai_snapshot_receipt": {"path": "/stale/receipt", "sha256": "0" * 64}}))
        self.source_bytes = self.bundle.read_bytes()

    def tearDown(self):
        self.temp.cleanup()

    def run_rebind(self):
        return module.rebind(self.root, self.bundle, self.capture, self.graph, self.root / "r16-bundle.json")

    def test_rebinds_receipt_and_graph_without_mutating_source_bundle(self):
        result = self.run_rebind()
        self.assertEqual(result["status"], "Passed")
        self.assertEqual(self.bundle.read_bytes(), self.source_bytes)
        output = json.loads((self.root / "r16-bundle.json").read_text())
        self.assertEqual(output["native_ai_snapshot_receipt"]["sha256"], digest(self.capture / "snapshot-receipt.json"))
        self.assertEqual(output["native_ai_source_graph"]["sha256"], digest(self.graph))
        self.assertEqual(output["prefab_capture"]["sha256"], digest(self.existing))

    def test_stale_retained_descriptor_rejected(self):
        self.existing.write_text("changed")
        with self.assertRaisesRegex(ValueError, "Existing bundle descriptor hash mismatch"):
            self.run_rebind()
        self.assertFalse((self.root / "r16-bundle.json").exists())

    def test_tampered_snapshot_or_log_rejected(self):
        (self.capture / "editor.log").write_text("tampered\n")
        with self.assertRaisesRegex(ValueError, "Successful checked wrapper|receipt does not match"):
            self.run_rebind()
        self.assertFalse((self.root / "r16-bundle.json").exists())

    def test_changed_native_source_pin_rejected(self):
        (self.root / "Assets/Source/file-00.txt").write_text("stale")
        with self.assertRaisesRegex(ValueError, "Native AI source changed"):
            self.run_rebind()
        self.assertFalse((self.root / "r16-bundle.json").exists())

    def test_absolute_graph_pin_outside_repo_rejected(self):
        outside = Path(self.temp.name).parent / (Path(self.temp.name).name + "-outside")
        outside.write_text("outside repo")
        graph = json.loads(self.graph.read_text())
        graph["source_pins"] = {str(outside.resolve()): digest(outside)}
        self.graph.write_text(json.dumps(graph))
        try:
            with self.assertRaisesRegex(ValueError, "AI source graph pin changed"):
                self.run_rebind()
            self.assertFalse((self.root / "r16-bundle.json").exists())
        finally:
            outside.unlink(missing_ok=True)

    def test_unqualified_receipt_rejected(self):
        receipt_path = self.capture / "snapshot-receipt.json"
        receipt = json.loads(receipt_path.read_text())
        receipt["mission_ready"] = True
        receipt_path.write_text(json.dumps(receipt))
        with self.assertRaisesRegex(ValueError, "receipt does not match"):
            self.run_rebind()
        self.assertFalse((self.root / "r16-bundle.json").exists())


if __name__ == "__main__":
    unittest.main()
